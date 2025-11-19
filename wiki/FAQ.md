# Frequently Asked Questions (FAQ)

## General Questions

### What is Simple Service Monitoring?

Simple Service Monitoring is an enterprise-grade health monitoring and alerting system for .NET applications. It provides real-time monitoring, intelligent alerting, and a web-based dashboard for tracking the health of your services.

### Is it free to use?

Yes! Simple Service Monitoring is open source and licensed under the MIT License. You can use it freely in both commercial and non-commercial projects.

### What .NET versions are supported?

The system supports .NET 6.0, .NET 7.0, and .NET 8.0+. It's built on ASP.NET Core and leverages the standard .NET health checks framework.

### Do I need external dependencies?

No! You can use the LiteDB storage option which requires no external database. For enterprise deployments, SQL Server storage is also available.

---

## Installation & Setup

### How do I install it?

Install via NuGet:

```bash
dotnet add package Simple.Service.Monitoring.Extensions
dotnet add package Simple.Service.Monitoring.UI.Extensions
```

See the [Quick Start Guide](Quick-Start-Guide.md) for complete setup instructions.

### Can I use it without the UI?

Yes! You can use just the monitoring and alerting features without the web dashboard:

```csharp
services.AddServiceMonitoring(configuration);
// Don't call WithServiceMonitoringUi()
```

### Do I need to modify my existing code?

Minimal changes are required. You mainly need to:
1. Install NuGet packages
2. Add service registration in `Program.cs`
3. Configure health checks in `appsettings.yml`

---

## Configuration

### Can I use JSON instead of YAML?

Yes! Both `appsettings.json` and `appsettings.yml` are supported. Use whichever format you prefer.

### How do I reload configuration without restarting?

Configuration hot-reload is built-in when using standard ASP.NET Core configuration. Changes to `appsettings.yml` or `appsettings.json` are automatically detected.

### Where should I store sensitive configuration?

Use environment variables, Azure Key Vault, or ASP.NET Core User Secrets for sensitive data:

```yaml
EmailTransportSettings:
  - Name: "AlertEmail"
    Password: "${EMAIL_PASSWORD}"  # From environment variable
```

### Can I monitor services in different environments?

Yes! Use environment-specific configuration files:

- `appsettings.Development.yml`
- `appsettings.Staging.yml`
- `appsettings.Production.yml`

---

## Monitoring

### How often are health checks performed?

You configure the monitoring interval per health check:

```yaml
MonitoringInterval: "00:01:00"  # Every 1 minute
```

Default is 1 minute if not specified.

### What happens if a health check times out?

The health check will be marked as `Unhealthy` and alerts will be triggered according to your alert configuration.

### Can I monitor external services?

Yes! HTTP/HTTPS monitoring works with any accessible endpoint, whether internal or external.

### Does it work behind a load balancer?

Yes! The system works correctly behind load balancers. Each instance maintains its own health check state.

---

## Alerting

### Why am I not receiving alerts?

Check:

1. **Alert is enabled**: `Alert: true` in health check configuration
2. **Transport settings are correct**: SMTP credentials, API tokens, etc.
3. **Transport name matches**: `TransportName` in `AlertBehaviour` matches transport configuration
4. **Firewall allows outbound connections**: SMTP, HTTP, etc.
5. **Alert frequency not throttling**: Check `AlertEvery` setting

### How do I prevent alert spam?

Use these settings:

```yaml
AlertBehaviour:
  - TransportMethod: Email
    AlertEvery: "00:15:00"        # Don't alert more than every 15 minutes
    AlertByFailCount: 3            # Only alert after 3 consecutive failures
    AlertOnce: true                # Send only one alert per incident
```

### Can I send alerts to multiple channels?

Yes! Configure multiple alert behaviours:

```yaml
AlertBehaviour:
  - TransportMethod: Email
    TransportName: "DevOpsEmail"
  - TransportMethod: Slack
    TransportName: "DevOpsSlack"
  - TransportMethod: Telegram
    TransportName: "DevOpsTelegram"
```

### How do I test alert configuration?

The easiest way is to:
1. Configure a health check with a very short timeout
2. Point it at an unreachable endpoint
3. Wait for the alert to trigger

### Can I schedule alerts for specific times?

Yes! Use time-based alerting:

```yaml
AlertBehaviour:
  - TransportMethod: Email
    StartAlertingOn: "09:00:00"  # Start at 9 AM
    StopAlertingOn: "17:00:00"   # Stop at 5 PM
    Timezone: "Eastern Standard Time"
```

---

## Dashboard & UI

### How do I access the dashboard?

By default, navigate to `/monitoring` in your application:

```
https://your-app.com/monitoring
```

### Can I customize the dashboard URL?

The URL path can be configured when mapping endpoints (requires code modification).

### Does the dashboard work on mobile?

Yes! The dashboard is fully responsive and works on mobile devices, tablets, and desktops.

### How do I enable dark mode?

Dark mode automatically detects system preferences. Users can also toggle it manually using the moon icon in the dashboard.

### Can I add my company logo?

Yes! Configure in `appsettings.yml`:

```yaml
MonitoringUi:
  CompanyName: "Your Company"
  HeaderLogoUrl: "https://your-company.com/logo.png"
```

---

## Data Storage

### Which storage option should I use?

| Option | Best For |
|--------|---------|
| **InMemory** | Development, testing |
| **LiteDB** | Small to medium deployments, single instance |
| **SQL Server** | Enterprise deployments, multiple instances |

### Does LiteDB require installation?

No! LiteDB is a file-based database included via NuGet. No separate installation needed.

### Where is the LiteDB file stored?

By default, it's stored in your application directory. You can configure the path via connection string.

### Can I query the historical data?

Yes! Historical data is accessible through the dashboard's timeline view and can be queried programmatically through the repository interfaces.

### How long is data retained?

Data retention depends on your storage backend and cleanup policies. You can implement custom retention policies by accessing the repository.

---

## Custom Health Checks

### How do I create a custom health check?

Implement `IHealthCheck`:

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Your logic here
        return HealthCheckResult.Healthy();
    }
}
```

Then configure it:

```yaml
HealthChecks:
  - Name: "Custom Check"
    ServiceType: Custom
    FullClassName: "MyApp.Health.CustomHealthCheck"
```

See [Custom Health Checks](Custom-Health-Checks.md) for details.

### Can I inject dependencies into custom health checks?

Yes! Use constructor injection:

```csharp
public class CustomHealthCheck : IHealthCheck
{
    private readonly IMyService _myService;

    public CustomHealthCheck(IMyService myService)
    {
        _myService = myService;
    }

    // ... implementation
}
```

---

## Performance

### What's the performance impact?

Minimal! Health checks run asynchronously and don't block your application. Impact depends on:
- Number of health checks
- Monitoring intervals
- Complexity of custom checks

### Can it scale to hundreds of services?

Yes! The system is designed for enterprise scale. Consider:
- Using SQL Server storage for multiple instances
- Adjusting monitoring intervals based on priority
- Implementing custom health checks efficiently

### Does it affect application startup time?

Minimal impact. Health checks are registered during DI container setup but don't run until after the application starts.

---

## Troubleshooting

### Dashboard shows "No data available"

Check:

1. Health checks are configured correctly
2. Monitoring service is running (check logs)
3. Storage backend is accessible
4. Browser console for JavaScript errors

### Health checks aren't running

Verify:

1. `AddServiceMonitoring()` is called in `Program.cs`
2. Configuration file is loaded correctly
3. Application logs for errors
4. `MonitoringInterval` is valid TimeSpan format

### SignalR real-time updates not working

Ensure:

1. SignalR middleware is configured
2. WebSocket support is enabled
3. Firewall/proxy allows WebSocket connections
4. Check browser console for connection errors

### "Status page component not rendering data correctly"

This was a known issue that has been fixed. Ensure you have the latest version and see [Status Page Component](Status-Page-Component.md) for troubleshooting steps.

---

## Integration

### Can I use it with Docker?

Yes! Works great in Docker containers. See [Docker Deployment](Docker-Deployment.md).

### Does it work with Kubernetes?

Yes! Perfect for Kubernetes deployments. See [Kubernetes Deployment](Kubernetes-Deployment.md).

### Can I integrate with Azure Application Insights?

Yes! You can implement a custom transport that publishes to Application Insights.

### Does it work with multiple instances/load balancers?

Yes! Each instance maintains its own health check state. Use SQL Server storage for centralized data when running multiple instances.

---

## Security

### Is the dashboard password protected?

The dashboard respects your application's authentication. Add authentication middleware before the monitoring middleware.

### Can I restrict access to specific users?

Yes! Use ASP.NET Core authorization:

```csharp
app.MapServiceMonitoringUi()
   .RequireAuthorization("MonitoringPolicy");
```

### How secure are alert transports?

- Email: Supports TLS/SSL
- Slack: Uses HTTPS with API tokens
- Telegram: Uses HTTPS with bot tokens
- All credentials should be stored securely

---

## Contributing

### How can I contribute?

See the [Contributing Guide](Contributing-Guide.md) for details on:
- Reporting bugs
- Suggesting features
- Submitting pull requests
- Code standards

### I found a bug, what should I do?

[Open an issue](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/issues) on GitHub with:
- Description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details

---

## Still Have Questions?

- Check [Common Issues](Common-Issues.md)
- Review [Troubleshooting](Common-Issues.md)
- [Open a discussion](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/discussions) on GitHub
- [Report an issue](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/issues)
