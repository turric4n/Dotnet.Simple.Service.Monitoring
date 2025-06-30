# Simple.Service.Monitoring.Extensions

A set of extension methods and helpers for the Simple.Service.Monitoring library, making it easier to integrate health checks, metrics, and monitoring into your .NET applications.

## Features

- Extension methods for quick service registration
- Helpers for common monitoring scenarios
- Integration with ASP.NET Core DI and middleware
- Designed for cloud-native and on-premises .NET solutions

## Installation

Install via NuGet: 

```
dotnet add package Simple.Service.Monitoring.Extensions
```

## Usage

### 1. Register Monitoring Extensions

In your `Program.cs` or `Startup.cs`:

```csharp
builder.Services.AddServiceMonitoring()
```

### 2. Use Extension Methods

You can use the provided extension methods to add custom health checks, metrics, or publishers as needed.

## License

MIT

## Repository

[https://github.com/turric4n/Dotnet.Simple.Service.Monitoring](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring)
