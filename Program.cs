using System.Globalization;
using System.Xml.Linq;

const string Endpoint = "https://graphical.weather.gov/xml/SOAP_server/ndfdXMLserver.php";
const string Namespace = "https://graphical.weather.gov/xml/DWMLgen/wsdl/ndfdXML.wsdl";

Console.WriteLine("=== SOAP Weather App (.NET) ===");
Console.Write("Enter a US ZIP code (example: 10001): ");
var zipCode = Console.ReadLine()?.Trim();

if (string.IsNullOrWhiteSpace(zipCode))
{
    Console.WriteLine("ZIP code is required.");
    return;
}

try
{
    using var http = new HttpClient();

    var (lat, lon) = await GetCoordinatesByZipAsync(http, zipCode);
    Console.WriteLine($"Coordinates for {zipCode}: lat={lat}, lon={lon}");

    var forecast = await GetForecastByDayAsync(http, lat, lon, DateTime.UtcNow.Date, 3);
    Console.WriteLine("\nForecast (next 3 days):");
    Console.WriteLine(forecast);
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to fetch weather data: {ex.Message}");
}

static async Task<(double Latitude, double Longitude)> GetCoordinatesByZipAsync(HttpClient http, string zipCode)
{
    var body = $"""
        <LatLonListZipCode xmlns="{Namespace}">
          <zipCodeList>{zipCode}</zipCodeList>
        </LatLonListZipCode>
        """;

    var responseXml = await SendSoapRequestAsync(http, body, "LatLonListZipCode");
    var result = ExtractResultValue(responseXml, "LatLonListZipCodeResult");

    // Example result format: "10001,NEW YORK,NY,40.748,-73.996"
    var first = result.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                      .FirstOrDefault();

    if (first is null)
    {
        throw new InvalidOperationException($"No coordinates were returned for ZIP code '{zipCode}'.");
    }

    var fields = first.Split(',', StringSplitOptions.TrimEntries);
    if (fields.Length < 5)
    {
        throw new InvalidOperationException($"Unexpected coordinate format: '{first}'.");
    }

    if (!double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
        !double.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
    {
        throw new InvalidOperationException($"Unable to parse latitude/longitude from '{first}'.");
    }

    return (lat, lon);
}

static async Task<string> GetForecastByDayAsync(HttpClient http, double latitude, double longitude, DateTime startDateUtc, int numDays)
{
    var body = $"""
        <NDFDgenByDay xmlns="{Namespace}">
          <latitude>{latitude.ToString(CultureInfo.InvariantCulture)}</latitude>
          <longitude>{longitude.ToString(CultureInfo.InvariantCulture)}</longitude>
          <startDate>{startDateUtc:yyyy-MM-dd}</startDate>
          <numDays>{numDays}</numDays>
          <Unit>e</Unit>
          <format>24 hourly</format>
        </NDFDgenByDay>
        """;

    var responseXml = await SendSoapRequestAsync(http, body, "NDFDgenByDay");
    var dwml = ExtractResultValue(responseXml, "NDFDgenByDayResult");

    return BuildSimpleForecast(dwml);
}

static async Task<string> SendSoapRequestAsync(HttpClient http, string operationBody, string soapAction)
{
    var envelope = $"""
        <?xml version="1.0" encoding="utf-8"?>
        <soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                       xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                       xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            {operationBody}
          </soap:Body>
        </soap:Envelope>
        """;

    using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
    request.Content = new StringContent(envelope);
    request.Content.Headers.ContentType = new("text/xml") { CharSet = "utf-8" };
    request.Headers.Add("SOAPAction", $"\"{Namespace}/{soapAction}\"");

    using var response = await http.SendAsync(request);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStringAsync();
}

static string ExtractResultValue(string soapXml, string resultElementName)
{
    var doc = XDocument.Parse(soapXml);

    var resultElement = doc.Descendants()
        .FirstOrDefault(x => x.Name.LocalName.Equals(resultElementName, StringComparison.Ordinal));

    if (resultElement is null)
    {
        throw new InvalidOperationException($"SOAP response did not contain '{resultElementName}'.");
    }

    return resultElement.Value;
}

static string BuildSimpleForecast(string dwmlXml)
{
    var doc = XDocument.Parse(dwmlXml);

    var days = doc.Descendants().Where(x => x.Name.LocalName == "start-valid-time").Take(3).ToList();
    var maxTemps = doc.Descendants().Where(x => x.Name.LocalName == "temperature" &&
                                                 (string?)x.Attribute("type") == "maximum")
                                  .Descendants().Where(x => x.Name.LocalName == "value").Take(3).ToList();
    var minTemps = doc.Descendants().Where(x => x.Name.LocalName == "temperature" &&
                                                 (string?)x.Attribute("type") == "minimum")
                                  .Descendants().Where(x => x.Name.LocalName == "value").Take(3).ToList();
    var summaries = doc.Descendants().Where(x => x.Name.LocalName == "weather-conditions")
                                     .Take(3)
                                     .Select(x => (string?)x.Attribute("weather-summary") ?? "N/A")
                                     .ToList();

    if (!days.Any())
    {
        return "No forecast details were returned by the SOAP API.";
    }

    var lines = new List<string>();
    for (var i = 0; i < days.Count; i++)
    {
        var date = DateTimeOffset.TryParse(days[i].Value, out var parsed)
            ? parsed.ToString("yyyy-MM-dd")
            : days[i].Value;

        var hi = i < maxTemps.Count ? maxTemps[i].Value : "N/A";
        var lo = i < minTemps.Count ? minTemps[i].Value : "N/A";
        var summary = i < summaries.Count ? summaries[i] : "N/A";

        lines.Add($"- {date}: High {hi}°F, Low {lo}°F, {summary}");
    }

    return string.Join(Environment.NewLine, lines);
}
