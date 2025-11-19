# Quick Start Guide

Get up and running with Simple Service Monitoring in just a few minutes!

## Prerequisites

- .NET 8.0 SDK or later
- ASP.NET Core application
- Visual Studio 2022, VS Code, or Rider

## Step 1: Install NuGet Packages

Add the required NuGet packages to your project:

```bash
dotnet add package Simple.Service.Monitoring.Extensions
dotnet add package Simple.Service.Monitoring.UI.Extensions
```

## Step 2: Update Program.cs

### For .NET 8+ (Minimal API)

```csharp
using Simple.Service.Monitoring.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Service Monitoring
builder.Services
    .AddServiceMonitoring(builder.Configuration)
    .WithServiceMonitoringUi(builder.Services, builder.Configuration)
    .WithApplicationSettings();

var app = builder.Build();

// Use Service Monitoring UI
app.UseServiceMonitoringUi(app.Environment);

app.UseEndpoints(endpoints =>
{
    endpoints.MapServiceMonitoringUi();
});

app.Run();
```

### For .NET 6/7 (Startup.cs)

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add Service Monitoring
    services
        .AddServiceMonitoring(Configuration)
        .WithServiceMonitoringUi(services, Configuration)
        .WithApplicationSettings();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseServiceMonitoringUi(env);
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapServiceMonitoringUi();
    });
}
```

## Step 3: Add Configuration

Create or update your `appsettings.yml` file:

```yaml
MonitoringUi:
  CompanyName: "My Company"
  DataRepositoryType: "LiteDb"

Monitoring:
  Settings:
    ShowUI: true
    UseGlobalServiceName: "My Application"
  
  HealthChecks:
    - Name: "Application Health"
      ServiceType: Http
      EndpointOrHost: "https://localhost:5001/health"
      Port: 5001
      MonitoringInterval: "00:01:00"
      Alert: true
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "AlertEmail"
          AlertEvery: "00:05:00"
  
  EmailTransportSettings:
    - Name: "AlertEmail"
      From: "monitoring@mycompany.com"
      To: "devops@mycompany.com"
      SmtpHost: "smtp.gmail.com"
      SmtpPort: 587
      Authentication: true
      Username: "monitoring@mycompany.com"
      Password: "your-app-specific-password"
```

Or in JSON (`appsettings.json`):

```json
{
  "MonitoringUi": {
    "CompanyName": "My Company",
    "DataRepositoryType": "LiteDb"
  },
  "Monitoring": {
    "Settings": {
      "ShowUI": true,
      "UseGlobalServiceName": "My Application"
    },
    "HealthChecks": [
      {
        "Name": "Application Health",
        "ServiceType": "Http",
        "EndpointOrHost": "https://localhost:5001/health",
        "Port": 5001,
        "MonitoringInterval": "00:01:00",
        "Alert": true,
        "AlertBehaviour": [
          {
            "TransportMethod": "Email",
            "TransportName": "AlertEmail",
            "AlertEvery": "00:05:00"
          }
        ]
      }
    ],
    "EmailTransportSettings": [
      {
        "Name": "AlertEmail",
        "From": "monitoring@mycompany.com",
        "To": "devops@mycompany.com",
        "SmtpHost": "smtp.gmail.com",
        "SmtpPort": 587,
        "Authentication": true,
        "Username": "monitoring@mycompany.com",
        "Password": "your-app-specific-password"
      }
    ]
  }
}
```

## Step 4: Run Your Application

Start your application:

```bash
dotnet run
```

## Step 5: Access the Dashboard

Open your browser and navigate to:

```
https://localhost:5001/monitoring
```

You should see the monitoring dashboard with your configured health checks!

## What You'll See

### Dashboard Features

1. **Real-time Status**: Live health status of all configured services
2. **Timeline View**: Historical health data visualization
3. **Status Page**: Service uptime and availability statistics
4. **Alert History**: Complete log of all alerts sent

### Initial Setup

The system will:

- Start monitoring your configured endpoints immediately
- Store health check results in LiteDB (file-based database)
- Send alerts when services become unhealthy
- Update the dashboard in real-time via SignalR

## Common Customizations

### 1. Change Monitoring Interval

```yaml
MonitoringInterval: "00:00:30"  # Check every 30 seconds
```

### 2. Add More Services

```yaml
HealthChecks:
  - Name: "Database"
    ServiceType: MsSql
    ConnectionString: "Server=localhost;Database=MyDb;..."
  
  - Name: "Redis Cache"
    ServiceType: Redis
    ConnectionString: "localhost:6379"
```

### 3. Configure Multiple Alert Channels

```yaml
AlertBehaviour:
  - TransportMethod: Email
    TransportName: "AlertEmail"
  - TransportMethod: Slack
    TransportName: "DevOpsSlack"
```

### 4. Enable Dark Mode

Dark mode is automatically enabled based on system preferences. Users can toggle it manually in the dashboard.

## Troubleshooting

### Dashboard Not Showing

Ensure you've called both:

```csharp
app.UseServiceMonitoringUi(app.Environment);
endpoints.MapServiceMonitoringUi();
```

### Alerts Not Sending

Check your transport settings:

- SMTP credentials are correct
- Firewall allows outbound connections
- Transport name matches in AlertBehaviour

### Health Checks Not Running

Verify:

- Configuration is in `appsettings.yml` or `appsettings.json`
- ServiceType is correct
- Monitoring interval is valid TimeSpan format

## Next Steps

Now that you have the basics working, explore:

- [Configuration Guide](Configuration-Guide.md) - Learn all configuration options
- [Service Types](Service-Types.md) - Explore all service monitoring types
- [Alert Configuration](Alert-Configuration.md) - Advanced alerting patterns
- [Custom Health Checks](Custom-Health-Checks.md) - Create custom monitors

## Need Help?

- Review [Common Issues](Common-Issues.md)
- Check the [FAQ](FAQ.md)
- [Open an issue](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/issues) on GitHub

---

**Congratulations! 🎉** You now have a fully functional health monitoring system running in your application!
