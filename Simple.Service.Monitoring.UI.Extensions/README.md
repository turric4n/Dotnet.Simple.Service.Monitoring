# Simple Service Monitoring UI Extensions

Real-time monitoring dashboard UI extensions for the Simple Service Monitoring library. This package provides a web-based interface for monitoring the health and performance of your .NET services.

## Features

- Real-time monitoring dashboard for service health metrics
- SignalR-based live updates for instant feedback
- Embedded static assets with optimized caching
- Seamless integration with ASP.NET Core applications
- Compatible with .NET 8 and .NET Standard 2.1 projects

## Installation

Install the package via NuGet:

```bash
dotnet add package Simple.Service.Monitoring.UI.Extensions
```

## Usage

### Step 1: Configure Services

In your `Program.cs` or `Startup.cs`:

```csharp
// Add service monitoring with UI extensions
builder.Services.AddServiceMonitoring()
	.WithStandardMetrics()
	.WithServiceMonitoringUi(builder.Services);
```

### Step 2: Configure the Application

In the middleware pipeline configuration:

```csharp
// Add the monitoring UI middleware
app.UseServiceMonitoringUi();
```

## Access the Dashboard

Once configured, access the monitoring dashboard at:

```
https://your-application-url/MonitoringDashboard
```

The dashboard provides real-time insights into your service's health, performance metrics, and operational status.

## API Reference

### WithServiceMonitoringUi

```csharp
IServiceMonitoringBuilder WithServiceMonitoringUi(IServiceCollection services)
```

Registers UI-related services including SignalR and the monitoring data service.

### UseServiceMonitoringUi

```csharp
IApplicationBuilder UseServiceMonitoringUi(this IApplicationBuilder app)
```

Sets up static file handling with appropriate caching for UI assets and configures the necessary endpoints for the monitoring dashboard.



