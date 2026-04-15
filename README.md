# SoapWeatherApp

Simple .NET console app that calls a NOAA SOAP weather service.

## What it does
- Accepts a US ZIP code.
- Calls SOAP operation `LatLonListZipCode` to resolve latitude/longitude.
- Calls SOAP operation `NDFDgenByDay` to fetch a 3-day forecast.
- Prints a friendly summary in the terminal.

## Run
```bash
dotnet run
```

You will be prompted for a ZIP code (e.g. `10001`).
