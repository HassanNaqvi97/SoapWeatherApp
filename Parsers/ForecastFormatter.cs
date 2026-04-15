using System.Xml.Linq;

namespace SoapWeatherApp.Parsers;

public static class ForecastFormatter
{
    public static string BuildSimpleForecast(string dwmlXml, int maxDays)
    {
        var doc = XDocument.Parse(dwmlXml);

        var days = doc.Descendants().Where(x => x.Name.LocalName == "start-valid-time").Take(maxDays).ToList();
        var maxTemps = doc.Descendants().Where(x => x.Name.LocalName == "temperature" &&
                                                     (string?)x.Attribute("type") == "maximum")
                                      .Descendants().Where(x => x.Name.LocalName == "value").Take(maxDays).ToList();
        var minTemps = doc.Descendants().Where(x => x.Name.LocalName == "temperature" &&
                                                     (string?)x.Attribute("type") == "minimum")
                                      .Descendants().Where(x => x.Name.LocalName == "value").Take(maxDays).ToList();
        var summaries = doc.Descendants().Where(x => x.Name.LocalName == "weather-conditions")
                                         .Take(maxDays)
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
}
