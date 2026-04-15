using System.Globalization;
using SoapWeatherApp.Models;

namespace SoapWeatherApp.Services;

public sealed class ForecastWeatherApi
{
    private readonly SoapTransport _soapTransport;

    public ForecastWeatherApi(SoapTransport soapTransport)
    {
        _soapTransport = soapTransport;
    }

    public async Task<string> GetForecastDwmlByDayAsync(Coordinate coordinate, DateTime startDateUtc, int numDays)
    {
        var body = $"""
            <NDFDgenByDay xmlns="{SoapTransport.Namespace}">
              <latitude>{coordinate.Latitude.ToString(CultureInfo.InvariantCulture)}</latitude>
              <longitude>{coordinate.Longitude.ToString(CultureInfo.InvariantCulture)}</longitude>
              <startDate>{startDateUtc:yyyy-MM-dd}</startDate>
              <numDays>{numDays}</numDays>
              <Unit>e</Unit>
              <format>24 hourly</format>
            </NDFDgenByDay>
            """;

        var responseXml = await _soapTransport.SendSoapRequestAsync(body, "NDFDgenByDay");
        return SoapTransport.ExtractResultValue(responseXml, "NDFDgenByDayResult");
    }
}
