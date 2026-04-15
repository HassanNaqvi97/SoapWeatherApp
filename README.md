# SoapWeatherApp

Simple .NET console app that calls a NOAA SOAP weather service.

## What it does
- Accepts a US ZIP code.
- Calls SOAP operation `LatLonListZipCode` to resolve latitude/longitude.
- Calls SOAP operation `NDFDgenByDay` to fetch a 3-day forecast.
- Prints a friendly summary in the terminal.


## Project structure
- `Program.cs`: app entrypoint and orchestration.
- `Services/ZipCodeWeatherApi.cs`: ZIP lookup SOAP API.
- `Services/ForecastWeatherApi.cs`: forecast SOAP API.
- `Services/SoapTransport.cs`: shared SOAP transport + SOAP result extraction.
- `Parsers/ForecastFormatter.cs`: DWML parsing and display formatting.
- `Models/Coordinate.cs`: coordinate model.

## Run (.NET)
```bash
dotnet run
```

You will be prompted for a ZIP code (e.g. `10001`).

## Test the SOAP API with Postman
Yes — you can test this API in Postman.

### Option A: Import ready-made collection
1. Open Postman.
2. Import `postman/SoapWeatherApp.postman_collection.json`.
3. Run request **1) LatLonListZipCode** with `zip_code` set (e.g. `10001`).
4. Copy returned latitude/longitude into collection variables.
5. Run request **2) NDFDgenByDay**.

### Option B: Manual request setup
- Method: `POST`
- URL: `https://graphical.weather.gov/xml/SOAP_server/ndfdXMLserver.php`
- Headers:
  - `Content-Type: text/xml; charset=utf-8`
  - `SOAPAction: "https://graphical.weather.gov/xml/DWMLgen/wsdl/ndfdXML.wsdl/LatLonListZipCode"`
- Body (raw XML):

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
               xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <LatLonListZipCode xmlns="https://graphical.weather.gov/xml/DWMLgen/wsdl/ndfdXML.wsdl">
      <zipCodeList>10001</zipCodeList>
    </LatLonListZipCode>
  </soap:Body>
</soap:Envelope>
```

Then use the same URL with SOAPAction `.../NDFDgenByDay` and the `NDFDgenByDay` SOAP body to retrieve forecast XML.
