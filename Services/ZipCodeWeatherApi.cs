using System.Globalization;
using SoapWeatherApp.Models;

namespace SoapWeatherApp.Services;

public sealed class ZipCodeWeatherApi
{
    private readonly SoapTransport _soapTransport;

    public ZipCodeWeatherApi(SoapTransport soapTransport)
    {
        _soapTransport = soapTransport;
    }

    public async Task<Coordinate> GetCoordinatesByZipAsync(string zipCode)
    {
        var body = $"""
            <LatLonListZipCode xmlns="{SoapTransport.Namespace}">
              <zipCodeList>{zipCode}</zipCodeList>
            </LatLonListZipCode>
            """;

        var responseXml = await _soapTransport.SendSoapRequestAsync(body, "LatLonListZipCode");
        var result = SoapTransport.ExtractResultValue(responseXml, "LatLonListZipCodeResult");

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

        if (!double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            throw new InvalidOperationException($"Unable to parse latitude/longitude from '{first}'.");
        }

        return new Coordinate(latitude, longitude);
    }
}
