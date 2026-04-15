using System.Xml.Linq;

namespace SoapWeatherApp.Services;

public sealed class SoapTransport
{
    public const string Endpoint = "https://graphical.weather.gov/xml/SOAP_server/ndfdXMLserver.php";
    public const string Namespace = "https://graphical.weather.gov/xml/DWMLgen/wsdl/ndfdXML.wsdl";

    private readonly HttpClient _httpClient;

    public SoapTransport(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> SendSoapRequestAsync(string operationBody, string soapAction)
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

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public static string ExtractResultValue(string soapXml, string resultElementName)
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
}
