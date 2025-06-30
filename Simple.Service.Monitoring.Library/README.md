# Simple.Service.Monitoring.Library

A lightweight, extensible .NET library for real-time service health monitoring and reporting.

## Features

- Health check abstraction for any .NET service
- Pluggable health check providers
- Real-time health status reporting
- Integration with ASP.NET Core and background services
- Extensible event/observer model
- Pluggable publishers for notifications (email, webhooks, etc.)
- Designed for cloud-native and on-premises scenarios

## Installation

Install via NuGet:

```
dotnet add package Simple.Service.Monitoring.Library
```

## Usage

### 1. Register Health Checks

In your `Program.cs` or `Startup.cs`:

```csharp
builder.Services.AddServiceMonitoring()
```

### 2. Implement Custom Health Checks (Optional)

Implement `IHealthCheckProvider` for your own checks:

```csharp
public class MyCustomHealthCheck : IHealthCheckProvider
{
    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Your health check logic
        return Task.FromResult(HealthCheckResult.Healthy("OK"));
    }
}
```

Register it:

```csharp
builder.Services.AddServiceMonitoring()
    .AddHealthCheck<MyCustomHealthCheck>("My Custom Check");
```

### 3. Observe Health Reports

Subscribe to health report events:

```csharp
public class MyObserver : IReportObserver
{
    public Task OnReportAsync(HealthReport report)
    {
        // Handle report
        return Task.CompletedTask;
    }
}

// Register observer
builder.Services.AddSingleton<IReportObserver, MyObserver>();
```

### 4. Add Publishers (Optional)

You can add publishers to send notifications (email, webhooks, etc.):


## License

MIT

## Repository

[https://github.com/turric4n/Dotnet.Simple.Service.Monitoring](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring)
