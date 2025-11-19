# Introduction

## What is Simple Service Monitoring?

**Simple Service Monitoring** is a comprehensive, enterprise-grade health monitoring and alerting system built for .NET applications. It provides real-time monitoring capabilities, intelligent alerting, and a beautiful web-based dashboard to help you maintain the health and reliability of your services.

## Key Features

### 🎯 Comprehensive Monitoring

Monitor 10+ different service types including:

- HTTP/HTTPS endpoints
- SQL Server databases
- MySQL databases  
- Redis cache servers
- Elasticsearch clusters
- RabbitMQ message queues
- Hangfire background jobs
- Network connectivity (Ping/ICMP)
- Custom health check implementations
- Request interceptors for application monitoring

### 🔔 Intelligent Alerting

The alerting system provides:

- **7 Transport Methods**: Email, Slack, Telegram, InfluxDB, Custom APIs, SignalR, Webhooks
- **Conditional Alerts**: Time-based windows, failure count thresholds
- **Recovery Notifications**: Get notified when services recover
- **Alert Deduplication**: Prevent alert fatigue with configurable frequency controls
- **Multi-Channel Support**: Send the same alert to multiple channels

### 📊 Modern Web Dashboard

Access a feature-rich dashboard featuring:

- Real-time status updates via SignalR
- Timeline visualization of historical health data
- Interactive status page component
- Dark mode support
- Mobile-responsive design
- Custom branding with company logos
- Service grouping and tagging

### ⚙️ Developer-Friendly

Built with developers in mind:

- **Configuration-first approach**: Define everything in JSON/YAML
- **Hot reload**: Update configuration without restarting
- **Multiple storage backends**: InMemory, LiteDB, SQL Server
- **Extensible architecture**: Easy to add custom checks and transports
- **NuGet packages**: Simple integration via NuGet
- **Sample implementations**: Learn from working examples

## Architecture

The system is built on the robust .NET HealthChecks framework, specifically leveraging [AspNetCore.Diagnostics.HealthChecks](https://github.com/xabaril/AspNetCore.Diagnostics.HealthChecks) by Xabaril.

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Your .NET Application                    │
├─────────────────────────────────────────────────────────────┤
│  Simple.Service.Monitoring.Extensions (Integration Layer)   │
├─────────────────────────────────────────────────────────────┤
│         Simple.Service.Monitoring.Library (Core)            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Service    │  │   Alert      │  │   Storage    │     │
│  │  Monitoring  │  │  Transports  │  │  Repository  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
├─────────────────────────────────────────────────────────────┤
│   Simple.Service.Monitoring.UI (Web Dashboard - Optional)   │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Health Checks** run at configured intervals
2. **Results** are analyzed and stored in the repository
3. **Alerts** are triggered based on configured conditions
4. **Transports** deliver alerts to configured channels
5. **Dashboard** displays real-time status via SignalR

## Use Cases

### Microservices Monitoring

Monitor the health of all your microservices from a single dashboard. Get immediate notifications when any service degrades or fails.

```yaml
HealthChecks:
  - Name: "User Service"
    ServiceType: Http
    EndpointOrHost: "https://user-service/health"
  - Name: "Order Service"
    ServiceType: Http
    EndpointOrHost: "https://order-service/health"
  - Name: "Payment Service"
    ServiceType: Http
    EndpointOrHost: "https://payment-service/health"
```

### Database Health Monitoring

Monitor your databases with custom business logic queries:

```yaml
HealthChecks:
  - Name: "Order Processing"
    ServiceType: MsSql
    ConnectionString: "..."
    HealthCheckConditions:
      SqlBehaviour:
        Query: "SELECT COUNT(*) FROM Orders WHERE Status = 'Stuck'"
        ResultExpression: LessThan
        ExpectedResult: 5  # Alert if more than 5 stuck orders
```

### Infrastructure Monitoring

Keep track of your infrastructure components:

```yaml
HealthChecks:
  - Name: "Redis Cache"
    ServiceType: Redis
    ConnectionString: "localhost:6379"
  - Name: "RabbitMQ"
    ServiceType: Rmq
    EndpointOrHost: "rabbitmq.company.com"
  - Name: "Elasticsearch"
    ServiceType: ElasticSearch
    EndpointOrHost: "elasticsearch.company.com"
```

## Why Choose Simple Service Monitoring?

### ✅ Production-Ready

- Battle-tested in enterprise environments
- Reliable alerting with duplicate prevention
- Comprehensive error handling
- Performance optimized

### ✅ Easy Integration

- NuGet package installation
- Minimal code changes required
- Configuration-driven setup
- Excellent documentation

### ✅ Flexible & Extensible

- Custom health check implementations
- Pluggable transport methods
- Multiple storage backends
- Open source and MIT licensed

### ✅ Cost-Effective

- No external dependencies required (LiteDB option)
- Self-hosted solution
- No per-service pricing
- Free and open source

## Getting Started

Ready to get started? Check out our [Quick Start Guide](Quick-Start-Guide.md) to be up and running in minutes!

## Next Steps

- [Installation Guide](Installation.md) - Install the necessary packages
- [Configuration Guide](Configuration-Guide.md) - Learn about all configuration options
- [Web Dashboard](Web-Dashboard.md) - Explore the monitoring dashboard
- [Example Configurations](Example-Configurations.md) - Copy-paste ready examples

## Questions?

- Check the [FAQ](FAQ.md)
- Review [Common Issues](Common-Issues.md)
- [Open an issue](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/issues) on GitHub
