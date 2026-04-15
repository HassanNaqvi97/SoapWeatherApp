using SoapWeatherApp.Models;
using SoapWeatherApp.Parsers;
using SoapWeatherApp.Services;

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
    using var httpClient = new HttpClient();
    var soapTransport = new SoapTransport(httpClient);

    var zipCodeApi = new ZipCodeWeatherApi(soapTransport);
    Coordinate coordinate = await zipCodeApi.GetCoordinatesByZipAsync(zipCode);

    Console.WriteLine($"Coordinates for {zipCode}: lat={coordinate.Latitude}, lon={coordinate.Longitude}");

    var forecastApi = new ForecastWeatherApi(soapTransport);
    var dwml = await forecastApi.GetForecastDwmlByDayAsync(coordinate, DateTime.UtcNow.Date, 3);

    Console.WriteLine("\nForecast (next 3 days):");
    Console.WriteLine(ForecastFormatter.BuildSimpleForecast(dwml, 3));
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to fetch weather data: {ex.Message}");
}
