# YAddress Web API .NET Client
Makes calls into YAddress Web API for postal address correction, validation, 
standardization and geocoding.

Add it to your project as a NuGet package "YAddressWebApiClient".
``` 
PM> Install-Package YAddressWebApiClient
```

Find more about YAddress Web API at http://www.yaddress.net/WebApi

## How To Use
```csharp
// Instantiate client
YAddress.Client client = new YAddress.Client();

// Process address
YAddress.Client.Address adr = 
    client.ProcessAddress("506 Fourth Avenue Unit 1", "Asbury Prk, NJ", null);

// Print out the results
Console.WriteLine("ErrorCode: " + adr.ErrorCode);
Console.WriteLine("ErrorMessage: " + adr.ErrorMessage);
Console.WriteLine("AddressLine1: " + adr.AddressLine1);
Console.WriteLine("AddressLine2: " + adr.AddressLine2);
Console.WriteLine("Latitude: " + adr.Latitude);
Console.WriteLine("Longitude: " + adr.Longitude);
```
