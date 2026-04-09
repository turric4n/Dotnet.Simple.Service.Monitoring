<div align="center">

# Dotnet.Simple.Service.Monitoring

**Enterprise-Grade Health Monitoring & Alerting for .NET**

[![.NET](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/actions/workflows/dotnet.yml/badge.svg)](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

Monitor **26 service types** across your infrastructure, alert through **24 transport channels**, and visualize everything in a **React-based real-time dashboard** — all driven by configuration.

[Quick Start](#-quick-start) · [Documentation](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/wiki) · [Examples](docs/examples/) · [Contributing](CONTRIBUTING.md)

</div>

---

## Overview

Dotnet.Simple.Service.Monitoring is a comprehensive health monitoring solution for .NET applications. Built on the .NET HealthChecks framework, it provides enterprise-grade monitoring, intelligent alerting, and a modern web dashboard — all with minimal code and configuration-first design.

## Key Features

### Comprehensive Service Monitoring (26 Types)

| Category | Service Types |
|----------|--------------|
| **Web & API** | HTTP/HTTPS, gRPC, TCP, Ping/ICMP, DNS, FTP, SMTP |
| **SQL Databases** | SQL Server, MySQL, PostgreSQL, Oracle, SQLite |
| **NoSQL & Cache** | Redis, Elasticsearch, MongoDB, CosmosDB, Memcached |
| **Message Brokers** | RabbitMQ, Kafka, Azure Service Bus, AWS SQS |
| **Infrastructure** | Docker, SSL Certificate, Hangfire |
| **Custom** | Custom `IHealthCheck` implementations, Request Interceptors |

### Intelligent Alerting (24 Transport Channels)

| Category | Transports |
|----------|-----------|
| **Chat & Messaging** | Slack, Telegram, Discord, Microsoft Teams, Google Chat, Mattermost |
| **Email** | SMTP with HTML/Markdown templates |
| **Incident Management** | PagerDuty, Opsgenie |
| **Metrics & Observability** | Prometheus, Datadog, AWS CloudWatch, Azure Application Insights, InfluxDB, Elasticsearch |
| **Streaming** | Kafka, Redis Pub/Sub, RabbitMQ |
| **Webhooks & API** | Generic Webhook, Custom Notification API, SignalR |
| **Local** | Console, File Log |

### Modern React Dashboard

- **Real-time updates** via SignalR WebSocket
- **Timeline visualization** with service name grouping and active services filtering
- **Dark mode** with system preference detection
- **Responsive design** — mobile, tablet, and desktop
- **Custom branding** — company logo and name
- **Built with**: React 18, TypeScript, Tailwind CSS, shadcn/ui, Zustand, TanStack Query

### Developer Experience

- **Configuration-first**: JSON or YAML — no code required for standard monitors
- **Hot reload**: Change configuration without restarting
- **3 storage backends**: InMemory, LiteDB, SQL Server
- **Extensible**: Add custom health checks and transport publishers
- **Docker ready**: Includes `docker-compose.yml` for local development

## Project Structure

```
Simple.Service.Monitoring/           # Core host application
Simple.Service.Monitoring.Library/   # Business logic, monitors, publishers, models
Simple.Service.Monitoring.Extensions/# Integration extension methods
Simple.Service.Monitoring.UI/        # React SPA dashboard (Webpack 5)
Simple.Service.Monitoring.UI.Extensions/ # UI middleware extensions
Simple.Service.Monitoring.Config.Generator/ # WinForms configuration tool
Simple.Service.Monitoring.Sample.API/# Example implementation
Simple.Service.Monitoring.Tests/     # Unit & integration tests
```

## Installation

```bash
dotnet add package Simple.Service.Monitoring.Extensions
dotnet add package Simple.Service.Monitoring.UI.Extensions
```

Or clone and reference directly:

```bash
git clone https://github.com/turric4n/Dotnet.Simple.Service.Monitoring.git
```

## Quick Start

### 1. Wire up in Program.cs / Startup.cs

```csharp
// ConfigureServices
services
    .AddServiceMonitoring(Configuration)
    .WithServiceMonitoringUi(services, Configuration)
    .WithApplicationSettings();

// Configure
app.UseServiceMonitoringUi(env);
app.UseEndpoints(endpoints => endpoints.MapServiceMonitoringUi());
```

### 2. Add configuration (appsettings.yml)

```yaml
MonitoringUi:
  CompanyName: "Acme Corp"
  HeaderLogoUrl: "https://acme.com/logo.png"
  DataRepositoryType: "LiteDb" # InMemory | LiteDb | Sql

Monitoring:
  Settings:
    ShowUI: true
    UseGlobalServiceName: "Production"
  HealthChecks:
    - Name: "Main API"
      ServiceType: Http
      EndpointOrHost: "https://api.acme.com/health"
      Port: 443
      MonitoringInterval: "00:01:00"
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
          HttpTimeoutMs: 5000
          HttpVerb: Get
          HttpCustomHeaders:         # v2.0 — custom headers
            Authorization: "Bearer <token>"
            X-Api-Key: "key-123"
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "OpsSlack"
          AlertEvery: "00:05:00"
          AlertOnServiceRecovered: true
          AlertByFailCount: 3
  SlackTransportSettings:
    - Name: "OpsSlack"
      Token: "xoxb-your-token"
      Channel: "#ops-alerts"
```

### 3. Run and open the dashboard

```bash
dotnet run
# Navigate to https://localhost:5001/monitoring
```

## Service Types Reference

### Web & API

| Type | Config Key | Required Fields | Notes |
|------|-----------|----------------|-------|
| HTTP/HTTPS | `Http` | `EndpointOrHost`, `Port` | Custom headers, expected status code, verb, timeout |
| gRPC | `Grpc` | `EndpointOrHost`, `Port` | gRPC health check protocol |
| TCP | `Tcp` | `EndpointOrHost`, `Port` | Raw TCP connection check |
| Ping/ICMP | `Ping` | `EndpointOrHost` | Network reachability |
| DNS | `Dns` | `EndpointOrHost` | DNS resolution check |
| FTP | `Ftp` | `EndpointOrHost`, `Port` | FTP server availability |
| SMTP | `Smtp` | `EndpointOrHost`, `Port` | Mail server availability |

### Databases

| Type | Config Key | Required Fields | Notes |
|------|-----------|----------------|-------|
| SQL Server | `MsSql` | `ConnectionString` | Custom SQL queries with result validation |
| MySQL | `MySql` | `ConnectionString` | Custom SQL queries with result validation |
| PostgreSQL | `PostgreSql` | `ConnectionString` | Custom SQL queries with result validation |
| Oracle | `Oracle` | `ConnectionString` | Custom SQL queries with result validation |
| SQLite | `Sqlite` | `ConnectionString` | Custom SQL queries with result validation |
| MongoDB | `MongoDb` | `ConnectionString` | Collection availability |
| CosmosDB | `CosmosDb` | `ConnectionString` | Azure Cosmos DB health |

### Cache & Search

| Type | Config Key | Required Fields | Notes |
|------|-----------|----------------|-------|
| Redis | `Redis` | `ConnectionString` | Connection + optional timeout |
| Elasticsearch | `ElasticSearch` | `EndpointOrHost` | Cluster health status |
| Memcached | `Memcached` | `EndpointOrHost`, `Port` | Memcached server check |

### Message Brokers

| Type | Config Key | Required Fields | Notes |
|------|-----------|----------------|-------|
| RabbitMQ | `Rmq` | `ConnectionString` | Broker connectivity |
| Kafka | `Kafka` | `EndpointOrHost` | Bootstrap server check |
| Azure Service Bus | `AzureServiceBus` | `ConnectionString` | Queue/topic health |
| AWS SQS | `AwsSqs` | `ConnectionString` | SQS queue health |

### Infrastructure

| Type | Config Key | Required Fields | Notes |
|------|-----------|----------------|-------|
| Docker | `Docker` | `EndpointOrHost` | Docker daemon health |
| SSL Certificate | `SslCertificate` | `EndpointOrHost` | Certificate expiry check |
| Hangfire | `Hangfire` | `ConnectionString` | Failed jobs & server count |

### Custom

| Type | Config Key | Required Fields | Notes |
|------|-----------|----------------|-------|
| Custom | `Custom` | `FullClassName` | Any `IHealthCheck` implementation |
| Interceptor | `Interceptor` | — | Application request monitoring |

### SQL Query Validation

SQL-based monitors (MsSql, MySql, PostgreSql, Oracle, Sqlite) support custom query validation:

```yaml
HealthCheckConditions:
  SqlBehaviour:
    Query: "SELECT COUNT(*) FROM Orders WHERE Status = 'Stuck'"
    ResultExpression: LessThan   # Equal | NotEqual | GreaterThan | LessThan
    SqlResultDataType: Int       # String | Int | Bool | DateTime
    ExpectedResult: 10
```

## Transport Channels Reference

### Chat & Messaging

<details>
<summary><strong>Slack</strong></summary>

```yaml
SlackTransportSettings:
  - Name: "OpsSlack"
    Token: "xoxb-your-slack-bot-token"
    Channel: "#ops-alerts"
    Username: "Health Monitor"
```
</details>

<details>
<summary><strong>Telegram</strong></summary>

```yaml
TelegramTransportSettings:
  - Name: "OpsTelegram"
    BotApiToken: "123456:ABC-DEF"
    ChatId: "-100123456789"
```

Supports HTML formatting with detailed failure/success reports (v2.0).
</details>

<details>
<summary><strong>Discord</strong></summary>

```yaml
DiscordTransportSettings:
  - Name: "OpsDiscord"
    WebhookUrl: "https://discord.com/api/webhooks/..."
```
</details>

<details>
<summary><strong>Microsoft Teams</strong></summary>

```yaml
TeamsTransportSettings:
  - Name: "OpsTeams"
    WebhookUrl: "https://outlook.office.com/webhook/..."
```
</details>

<details>
<summary><strong>Google Chat</strong></summary>

```yaml
GoogleChatTransportSettings:
  - Name: "OpsGoogleChat"
    WebhookUrl: "https://chat.googleapis.com/v1/spaces/..."
```
</details>

<details>
<summary><strong>Mattermost</strong></summary>

```yaml
MattermostTransportSettings:
  - Name: "OpsMattermost"
    WebhookUrl: "https://mattermost.company.com/hooks/..."
```
</details>

### Email

<details>
<summary><strong>Email (SMTP)</strong></summary>

```yaml
EmailTransportSettings:
  - Name: "OpsEmail"
    From: "monitoring@company.com"
    DisplayName: "Health Monitor"
    To: "devops@company.com"
    SmtpHost: "smtp.company.com"
    SmtpPort: 587
    Authentication: true
    Username: "monitoring@company.com"
    Password: "secure_password"
```

Supports HTML templates with detailed failure reports (v2.0).
</details>

### Incident Management

<details>
<summary><strong>PagerDuty</strong></summary>

```yaml
PagerDutyTransportSettings:
  - Name: "OpsPagerDuty"
    IntegrationKey: "your-integration-key"
```
</details>

<details>
<summary><strong>Opsgenie</strong></summary>

```yaml
OpsgenieTransportSettings:
  - Name: "OpsOpsgenie"
    ApiKey: "your-api-key"
```
</details>

### Metrics & Observability

<details>
<summary><strong>Prometheus</strong></summary>

```yaml
PrometheusTransportSettings:
  - Name: "OpsPrometheus"
    PushgatewayUrl: "https://pushgateway.company.com"
```
</details>

<details>
<summary><strong>Datadog</strong></summary>

```yaml
DatadogTransportSettings:
  - Name: "OpsDatadog"
    ApiKey: "your-datadog-api-key"
```
</details>

<details>
<summary><strong>AWS CloudWatch</strong></summary>

```yaml
CloudWatchTransportSettings:
  - Name: "OpsCloudWatch"
    Region: "us-east-1"
    Namespace: "HealthChecks"
```
</details>

<details>
<summary><strong>Azure Application Insights</strong></summary>

```yaml
AppInsightsTransportSettings:
  - Name: "OpsAppInsights"
    InstrumentationKey: "your-instrumentation-key"
```
</details>

<details>
<summary><strong>InfluxDB</strong></summary>

```yaml
InfluxDbTransportSettings:
  - Name: "OpsInflux"
    Host: "https://influx.company.com"
    Database: "health_metrics"
```
</details>

### Webhooks & Streaming

<details>
<summary><strong>Webhook</strong></summary>

```yaml
WebhookTransportSettings:
  - Name: "OpsWebhook"
    Url: "https://api.company.com/health-webhook"
    Headers:
      Authorization: "Bearer token"
```
</details>

<details>
<summary><strong>Custom Notification API</strong></summary>

```yaml
CustomNotificationTransportSettings:
  - Name: "OpsCustom"
    BaseEndpoint: "https://notifications.company.com/api"
    ApiKey: "your-api-key"
    ProjectName: "MyApp"
    Environment: "Production"
```
</details>

<details>
<summary><strong>SignalR</strong></summary>

```yaml
SignalRTransportSettings:
  - Name: "OpsSignalR"
    HubUrl: "https://app.company.com/monitoringhub"
    HubMethod: "ReceiveHealthAlert"
```
</details>

## Alert Behavior

Configure sophisticated alerting rules per health check:

```yaml
AlertBehaviour:
  - TransportMethod: Email
    TransportName: "PrimaryEmail"
    AlertOnce: false                  # false = repeated alerts
    AlertOnServiceRecovered: true     # notify on recovery
    AlertEvery: "00:05:00"           # alert frequency
    AlertByFailCount: 3              # trigger after N consecutive failures
    StartAlertingOn: "09:00:00"      # business hours only
    StopAlertingOn: "17:00:00"
    PublishAllResults: false
    IncludeEnvironment: true
    Timezone: "UTC"
```

| Property | Description | Default |
|----------|-------------|---------|
| `TransportMethod` | Transport type (`Email`, `Slack`, `Telegram`, `Webhook`, etc.) | — |
| `TransportName` | References a named transport config | — |
| `AlertOnce` | Single alert per failure episode | `false` |
| `AlertOnServiceRecovered` | Alert when service recovers | `true` |
| `AlertEvery` | Minimum interval between alerts | `00:05:00` |
| `AlertByFailCount` | Consecutive failures before alerting | `1` |
| `StartAlertingOn` / `StopAlertingOn` | Time-of-day alert window | `00:00:00` / `23:59:59` |
| `PublishAllResults` | Publish all results (not just failures) | `false` |
| `IncludeEnvironment` | Include environment in alert | `false` |
| `Timezone` | Timezone for time-based windows | `UTC` |

## Data Storage

```yaml
MonitoringUi:
  DataRepositoryType: "LiteDb"  # InMemory | LiteDb | Sql
  SqlConnectionString: "Server=localhost;Database=HealthChecks;..."
```

| Backend | Persistence | Dependencies | Best For |
|---------|------------|--------------|----------|
| **InMemory** | None | None | Development, testing |
| **LiteDb** | File-based | None | Small-medium deployments |
| **Sql** | SQL Server | SQL Server instance | Enterprise, multi-instance |

## Docker Support

A `docker-compose.yml` is included for local development with all supported services:

```bash
docker-compose up -d
```

Starts: SQL Server, MySQL, PostgreSQL, Redis, RabbitMQ, Elasticsearch, Hangfire SQL, and an HTTP test server.

### Running the Monitor in Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Simple.Service.Monitoring.dll"]
```

## Dashboard

Access the monitoring dashboard at `/monitoring`. Built with React 18, TypeScript, and Tailwind CSS.

**Features:**
- Real-time health status with SignalR live updates
- Interactive timeline visualization with configurable time ranges
- Service name grouping — aggregate checks across multiple machines
- Active services filter — hide inactive monitors
- Dark mode with system preference detection
- Mobile-responsive layout
- Company branding (logo + name)

## Use Cases

### Microservices Monitoring

```yaml
HealthChecks:
  - Name: "User API"
    ServiceType: Http
    EndpointOrHost: "https://user-api.company.com/health"
    AlertBehaviour:
      - TransportMethod: PagerDuty
        TransportName: "CriticalPD"
        AlertByFailCount: 2
    AdditionalTags: ["critical", "user-facing"]

  - Name: "Order Queue"
    ServiceType: Kafka
    EndpointOrHost: "kafka-broker:9092"
    AlertBehaviour:
      - TransportMethod: Slack
        TransportName: "OpsSlack"
    AdditionalTags: ["messaging"]
```

### Database Health with Business Logic

```yaml
HealthChecks:
  - Name: "Stuck Orders Alert"
    ServiceType: MsSql
    ConnectionString: "Server=db;Database=Orders;..."
    HealthCheckConditions:
      SqlBehaviour:
        Query: |
          SELECT COUNT(*) FROM Orders
          WHERE Status = 'Processing'
          AND CreatedDate < DATEADD(hour, -1, GETDATE())
        ResultExpression: LessThan
        SqlResultDataType: Int
        ExpectedResult: 50
    AlertBehaviour:
      - TransportMethod: Teams
        TransportName: "BusinessTeams"
```

### SSL Certificate Expiry

```yaml
HealthChecks:
  - Name: "API SSL Certificate"
    ServiceType: SslCertificate
    EndpointOrHost: "api.company.com"
    Port: 443
    AlertBehaviour:
      - TransportMethod: Email
        TransportName: "SecurityEmail"
        AlertEvery: "24:00:00"
```

### Multi-Environment Setup

```yaml
# appsettings.Production.yml
Monitoring:
  Settings:
    UseGlobalServiceName: "Production"
  HealthChecks:
    - Name: "Prod API"
      MonitoringInterval: "00:00:30"
      AlertBehaviour:
        - TransportMethod: PagerDuty
          AlertEvery: "00:01:00"

# appsettings.Development.yml
Monitoring:
  Settings:
    UseGlobalServiceName: "Development"
  HealthChecks:
    - Name: "Dev API"
      MonitoringInterval: "00:05:00"
      Alert: false
```

## Tools

| Tool | Description |
|------|-------------|
| **Config Generator** (`Simple.Service.Monitoring.Config.Generator`) | WinForms app for visual health check configuration with JSON/YAML export |
| **Sample API** (`Simple.Service.Monitoring.Sample.API`) | Reference implementation with custom health checks and transport configs |

## Configuration Reference

### Health Check Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `Name` | String | Yes | — | Unique identifier |
| `ServiceType` | Enum | Yes | — | One of [26 service types](#service-types-reference) |
| `EndpointOrHost` | String | Varies | — | Service endpoint or hostname |
| `ConnectionString` | String | Varies | — | Database/service connection string |
| `Port` | Integer | No | — | Service port |
| `MonitoringInterval` | TimeSpan | No | `00:01:00` | Check frequency |
| `FullClassName` | String | No | — | Custom `IHealthCheck` class (for `Custom` type) |
| `PublishChecks` | Boolean | No | `true` | Publish results to storage |
| `Alert` | Boolean | No | `false` | Enable alerting |
| `AlertBehaviour` | Array | No | — | Alert configurations |
| `AdditionalTags` | String[] | No | — | Tags for grouping/filtering |

### MonitoringUi Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CompanyName` | String | — | Displayed in dashboard header |
| `HeaderDescription` | String | — | Header subtitle |
| `HeaderLogoUrl` | String | — | Company logo URL |
| `DataRepositoryType` | Enum | `LiteDb` | `InMemory`, `LiteDb`, or `Sql` |
| `SqlConnectionString` | String | — | Required when using `Sql` storage |

## What's New in v2.0

- **HTTP Custom Headers** — attach `Authorization`, `X-Api-Key`, or any custom headers to HTTP health checks
- **Detailed Failure/Success Reports** — rich diagnostic info in alerts (response time, status code, error details)
- **Enhanced Telegram Formatting** — HTML templates with emoji indicators
- **15 New Service Monitors** — Kafka, gRPC, TCP, DNS, SSL Certificate, FTP, SMTP, Azure Service Bus, Memcached, Docker, AWS SQS, CosmosDB, MongoDB, Oracle, SQLite
- **17 New Transport Publishers** — Discord, Teams, Google Chat, Mattermost, PagerDuty, Opsgenie, Datadog, CloudWatch, App Insights, Prometheus, Kafka, Redis, RabbitMQ, Elasticsearch, Console, File Log
- **React SPA Dashboard** — complete rewrite with React 18, TypeScript, Tailwind CSS, shadcn/ui
- **Timeline Grouping** — group health checks by service name across machines
- **Active Services Filter** — hide inactive monitors from the timeline
- **Bug Fixes** — duration reporting accuracy, Redis concurrency safety, credential security

See [RELEASE_NOTES_v2.0.0.md](Simple.Service.Monitoring/RELEASE_NOTES_v2.0.0.md) for full details.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

```bash
git clone https://github.com/turric4n/Dotnet.Simple.Service.Monitoring.git
cd Dotnet.Simple.Service.Monitoring
dotnet restore Simple.Service.Monitoring/Simple.Service.Monitoring.sln
dotnet build Simple.Service.Monitoring/Simple.Service.Monitoring.sln
dotnet test Simple.Service.Monitoring.Tests/Simple.Service.Monitoring.Tests.csproj
```

## License

MIT — see [LICENSE](LICENSE) for details.

## Support

- [GitHub Issues](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/issues) — bug reports & feature requests
- [GitHub Discussions](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/discussions) — questions & ideas
- [Wiki](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/wiki) — full documentation

---

<div align="center">
Made with ❤️ for the .NET community by <a href="https://github.com/turric4n">Turrican</a>
</div>
