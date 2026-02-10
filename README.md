# Dotnet.Simple.Service.Monitoring

> Enterprise-Grade Health Monitoring and Alerting System for .NET Applications

## Overview

Dotnet.Simple.Service.Monitoring is a comprehensive health monitoring solution that simplifies the implementation of health checks in .NET applications. Built on top of the robust .NET HealthChecks framework with custom health check implementations, it provides enterprise-grade monitoring capabilities with minimal configuration.

The system offers real-time monitoring, intelligent alerting, and a beautiful web-based dashboard to keep track of your application's health across multiple environments.

## 🚀 Key Features

### **Comprehensive Service Monitoring**

- **11+ Service Types**: HTTP/HTTPS, SQL Server, MySQL, PostgreSQL, Redis, ElasticSearch, RabbitMQ, Hangfire, Ping/ICMP, Custom implementations, and Request Interceptors
- **Advanced Health Conditions**: Custom queries, response time thresholds, connection validation, and business logic checks
- **Flexible Scheduling**: Configurable monitoring intervals per service
- **Smart Tagging**: Organize services with custom tags for better management

### **Intelligent Alerting System**

- **7 Transport Methods**: Email (SMTP), Slack, Telegram, InfluxDB, Custom APIs, SignalR, and Webhooks
- **Conditional Alerting**: Time-based alerting windows, failure count thresholds, and recovery notifications
- **Alert Deduplication**: Prevent alert spam with configurable frequency controls
- **Multi-Channel Support**: Send alerts to multiple channels simultaneously

### **Modern Web Dashboard**

- **Real-time Updates**: Live status updates via SignalR
- **Timeline Visualization**: Historical health data with interactive charts
- **Dark Mode Support**: Modern UI with user preference persistence
- **Responsive Design**: Mobile-friendly dashboard for monitoring on-the-go
- **Company Branding**: Customizable logos and company information

### **Developer Experience**

- **Configuration-First Approach**: Define everything through JSON/YAML configuration
- **Hot Reload**: Configuration changes without application restart
- **Multiple Data Storage**: In-memory, LiteDB, or SQL Server backends
- **Extensible Architecture**: Easy to add custom health checks and transport methods

## 🏗️ Project Structure

This solution consists of several key components:

- **Simple.Service.Monitoring**: Core monitoring service and web dashboard
- **Simple.Service.Monitoring.Library**: Core business logic and models
- **Simple.Service.Monitoring.Extensions**: Extension methods for easy integration
- **Simple.Service.Monitoring.UI**: Modern web-based monitoring dashboard
- **Simple.Service.Monitoring.UI.Extensions**: UI-specific extension methods
- **Simple.Service.Monitoring.Config.Generator**: Windows Forms configuration tool
- **Simple.Service.Monitoring.Sample.API**: Example implementation

## 📦 Installation

### NuGet Package Installation

```bash
dotnet add package Simple.Service.Monitoring.Extensions
dotnet add package Simple.Service.Monitoring.UI.Extensions
```

### Manual Installation

1. Clone the repository:

```bash
git clone https://github.com/yourusername/Dotnet.Simple.Service.Monitoring.git
```

1. Add project references to your solution:

```bash
dotnet add reference Simple.Service.Monitoring.Extensions
dotnet add reference Simple.Service.Monitoring.UI.Extensions
```

## ⚡ Quick Start

### 1. Basic Setup

In your `Startup.cs` or `Program.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
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

### 2. Configuration

Add configuration to your `appsettings.yml` or `appsettings.json`:

```yaml
MonitoringUi:
  CompanyName: "Your Company"
  HeaderLogoUrl: "https://yourcompany.com/logo.png"
  DataRepositoryType: "LiteDb"

Monitoring:
  Settings:
    ShowUI: true
    UseGlobalServiceName: "Production Services"
  HealthChecks:
    - Name: "API Health Check"
      ServiceType: Http
      EndpointOrHost: "https://api.yourapp.com/health"
      Port: 443
      Alert: true
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "DevOpsEmail"
          AlertEvery: "00:05:00"
  EmailTransportSettings:
    - Name: "DevOpsEmail"
      From: "monitoring@yourcompany.com"
      To: "devops@yourcompany.com"
      SmtpHost: "smtp.yourcompany.com"
      SmtpPort: 587
      Authentication: true
```

### 3. Run Your Application

Navigate to `/monitoring` to access the dashboard.

## 📋 Comprehensive Configuration

The monitoring system supports both JSON and YAML configuration formats. Below are detailed examples showcasing all available options.

### Service Types

| Service Type | Description | Use Case |
|--------------|-------------|----------|
| `Http` | HTTP/HTTPS endpoint monitoring | Web APIs, websites, microservices |
| `MsSql` | SQL Server database monitoring | Database connectivity and custom queries |
| `MySql` | MySQL database monitoring | MySQL database health checks |
| `PostgreSql` | PostgreSQL database monitoring | PostgreSQL database health checks |
| `Redis` | Redis cache monitoring | Cache availability and performance |
| `ElasticSearch` | Elasticsearch cluster monitoring | Search and logging infrastructure |
| `Rmq` | RabbitMQ message queue monitoring | Message broker health |
| `Hangfire` | Hangfire background job monitoring | Background job processing |
| `Ping` | ICMP ping monitoring | Network connectivity |
| `Custom` | Custom health check implementations | Business logic validation |
| `Interceptor` | Request interception monitoring | Application request monitoring |

### Transport Methods

| Transport | Description | Configuration |
|-----------|-------------|---------------|
| `Email` | SMTP email notifications | SMTP server, credentials, templates |
| `Slack` | Slack channel messages | Bot token, channel, username |
| `Telegram` | Telegram bot messages | Bot API token, chat ID |
| `Influx` | InfluxDB metrics storage | Database host, database name |
| `CustomApi` | Custom API webhooks | API endpoint, authentication |
| `SignalR` | Real-time web notifications | Hub URL, method name |
| `Dummy` | No-op transport for testing | No configuration needed |

### Complete Configuration Examples

#### JSON Configuration (appsettings.json)

```json
{
  "MonitoringUi": {
    "CompanyName": "Your Company Name",
    "HeaderLogoUrl": "https://company.com/logo.png",
    "DataRepositoryType": "LiteDb"
  },
  "Monitoring": {
    "Settings": {
      "ShowUI": true,
      "UseGlobalServiceName": "Production Services"
    },
    "HealthChecks": [
      {
        "Name": "API Gateway",
        "ServiceType": "Http",
        "EndpointOrHost": "https://api.company.com/health",
        "Port": 443,
        "MonitoringInterval": "00:01:00",
        "HealthCheckConditions": {
          "HttpBehaviour": {
            "HttpExpectedCode": 200,
            "HttpTimeoutMs": 5000,
            "HttpVerb": "Get"
          },
          "ServiceReach": true,
          "ServiceConnectionEstablished": true
        },
        "Alert": true,
        "AlertBehaviour": [
          {
            "TransportMethod": "Email",
            "TransportName": "DevOpsEmail",
            "AlertEvery": "00:05:00",
            "AlertOnServiceRecovered": true,
            "AlertByFailCount": 3
          }
        ],
        "AdditionalTags": ["critical", "api"]
      },
      {
        "Name": "User Database",
        "ServiceType": "MsSql",
        "ConnectionString": "Server=db.company.com;Database=Users;Integrated Security=true;",
        "HealthCheckConditions": {
          "SqlBehaviour": {
            "Query": "SELECT COUNT(*) FROM Users WHERE LastLoginDate > DATEADD(day, -30, GETDATE())",
            "ResultExpression": "GreaterThan",
            "SqlResultDataType": "Int",
            "ExpectedResult": 100
          }
        },
        "Alert": true,
        "AlertBehaviour": [
          {
            "TransportMethod": "Slack",
            "TransportName": "DevOpsSlack",
            "AlertEvery": "00:10:00"
          }
        ]
      }
    ],
    "EmailTransportSettings": [
      {
        "Name": "DevOpsEmail",
        "From": "monitoring@company.com",
        "To": "devops@company.com",
        "SmtpHost": "smtp.company.com",
        "SmtpPort": 587,
        "Authentication": true,
        "Username": "monitoring@company.com",
        "Password": "secure_password"
      }
    ],
    "SlackTransportSettings": [
      {
        "Name": "DevOpsSlack",
        "Token": "xoxb-your-slack-bot-token",
        "Channel": "#devops-alerts",
        "Username": "Health Monitor"
      }
    ]
  }
}
```

#### YAML Configuration (appsettings.yml)

```yaml
MonitoringUi:
  CompanyName: "Your Company Name"
  HeaderLogoUrl: "https://company.com/logo.png"
  DataRepositoryType: "LiteDb" # Options: InMemory, LiteDb, Sql

Monitoring:
  Settings:
    ShowUI: true
    UseGlobalServiceName: "Production Services"
  
  HealthChecks:
    - Name: "API Gateway"
      ServiceType: Http
      EndpointOrHost: "https://api.company.com/health"
      Port: 443
      MonitoringInterval: "00:01:00"
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
          HttpTimeoutMs: 5000
          HttpVerb: Get # Options: Get, Post, Put, Delete
        ServiceReach: true
        ServiceConnectionEstablished: true
      Alert: true
      AlertBehaviour:
        - TransportMethod: Email # Options: Email, Slack, Telegram, Influx, CustomApi, SignalR
          TransportName: "DevOpsEmail"
          AlertEvery: "00:05:00"
          AlertOnServiceRecovered: true
          AlertByFailCount: 3
          StartAlertingOn: "00:00:00"
          StopAlertingOn: "23:59:59"
          IncludeEnvironment: true
          Timezone: "UTC"
      AdditionalTags:
        - "critical"
        - "api"
    
    - Name: "User Database"
      ServiceType: MsSql
      ConnectionString: "Server=db.company.com;Database=Users;Integrated Security=true;"
      MonitoringInterval: "00:02:00"
      HealthCheckConditions:
        SqlBehaviour:
          Query: "SELECT COUNT(*) FROM Users WHERE LastLoginDate > DATEADD(day, -30, GETDATE())"
          ResultExpression: GreaterThan # Options: Equal, NotEqual, GreaterThan, LessThan
          SqlResultDataType: Int # Options: String, Int, Bool, DateTime
          ExpectedResult: 100
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "DevOpsSlack"
          AlertEvery: "00:10:00"
      
    - Name: "PostgreSQL Database"
      ServiceType: PostgreSql
      ConnectionString: "Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=secret"
      MonitoringInterval: "00:02:00"
      HealthCheckConditions:
        SqlBehaviour:
          Query: "SELECT COUNT(*) FROM users WHERE active = true"
          ResultExpression: GreaterThan
          SqlResultDataType: Int
          ExpectedResult: 0
      Alert: true
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "DevOpsEmail"
      AdditionalTags:
        - "database"
        - "postgresql"
      
    - Name: "Redis Cache"
      ServiceType: Redis
      ConnectionString: "localhost:6379"
      HealthCheckConditions:
        RedisBehaviour:
          TimeOutMs: 3000
      Alert: true
      AlertBehaviour:
        - TransportMethod: Telegram
          TransportName: "DevOpsTelegram"
  
  EmailTransportSettings:
    - Name: "DevOpsEmail"
      From: "monitoring@company.com"
      DisplayName: "Health Monitoring"
      To: "devops@company.com"
      SmtpHost: "smtp.company.com"
      SmtpPort: 587
      Authentication: true
      Username: "monitoring@company.com"
      Password: "secure_password"
  
  SlackTransportSettings:
    - Name: "DevOpsSlack"
      Token: "xoxb-your-slack-bot-token"
      Channel: "#devops-alerts"
      Username: "Health Monitor"
  
  TelegramTransportSettings:
    - Name: "DevOpsTelegram"
      BotApiToken: "00000:000000"
      ChatId: "-000000"
```

## 🔧 Advanced Configuration

### Health Check Conditions

Each service type supports specific health check conditions:

#### HTTP/HTTPS Services

```yaml
HealthCheckConditions:
  HttpBehaviour:
    HttpExpectedCode: 200        # Expected HTTP status code
    HttpTimeoutMs: 5000          # Request timeout in milliseconds
    HttpVerb: Get                # HTTP method (Get, Post, Put, Delete)
  ServiceReach: true             # Verify service is reachable
  ServiceConnectionEstablished: true  # Verify connection can be established
```

#### SQL Database Services

```yaml
HealthCheckConditions:
  SqlBehaviour:
    Query: "SELECT COUNT(*) FROM Users"
    ResultExpression: GreaterThan    # Equal, NotEqual, GreaterThan, LessThan
    SqlResultDataType: Int           # String, Int, Bool, DateTime
    ExpectedResult: 0
```

#### Redis Services

```yaml
HealthCheckConditions:
  RedisBehaviour:
    TimeOutMs: 3000              # Connection timeout in milliseconds
```

#### Hangfire Services

```yaml
HealthCheckConditions:
  HangfireBehaviour:
    MaximumJobsFailed: 10        # Maximum failed jobs threshold
    MinimumAvailableServers: 1   # Minimum required servers
```

### Alert Behavior Configuration

Configure sophisticated alerting behavior:

```yaml
AlertBehaviour:
  - TransportMethod: Email
    TransportName: "PrimaryEmail"
    AlertOnce: false                    # Send repeated alerts
    AlertOnServiceRecovered: true       # Alert when service recovers
    AlertEvery: "00:05:00"             # Alert frequency
    AlertByFailCount: 3                 # Alert after N consecutive failures
    StartAlertingOn: "09:00:00"        # Start alerting at specific time
    StopAlertingOn: "17:00:00"         # Stop alerting at specific time
    PublishAllResults: false            # Publish all check results
    IncludeEnvironment: true            # Include environment info
    Timezone: "UTC"                     # Timezone for time-based alerts
```

### Data Storage Options

Choose your preferred data storage backend:

```yaml
MonitoringUi:
  DataRepositoryType: "LiteDb"  # Options: InMemory, LiteDb, Sql
  SqlConnectionString: "Server=localhost;Database=HealthChecks;Integrated Security=true;"
```

- **InMemory**: Fast, no persistence, suitable for development
- **LiteDb**: File-based, no external dependencies, great for small deployments
- **Sql**: Full SQL Server support, suitable for enterprise deployments

## 📊 Configuration Reference

### Core Properties

#### MonitoringUi Settings

| Property | Description | Type | Default |
|----------|-------------|------|---------|
| `CompanyName` | Company name displayed in UI | String | - |
| `HeaderDescription` | Header description text | String | - |
| `HeaderLogoUrl` | URL to company logo | String | - |
| `DataRepositoryType` | Data storage backend | Enum | `LiteDb` |
| `SqlConnectionString` | SQL connection (if using Sql storage) | String | - |

#### Monitoring Settings

| Property | Description | Type | Default |
|----------|-------------|------|---------|
| `ShowUI` | Enable web dashboard | Boolean | `true` |
| `UseGlobalServiceName` | Global service identifier | String | - |

#### Health Check Properties

| Property | Description | Type | Required |
|----------|-------------|------|----------|
| `Name` | Unique health check identifier | String | ✅ |
| `ServiceType` | Type of service to monitor | Enum | ✅ |
| `EndpointOrHost` | Service endpoint or hostname | String | ✅* |
| `ConnectionString` | Database/service connection string | String | ✅* |
| `Port` | Service port number | Integer | - |
| `MonitoringInterval` | Check frequency | TimeSpan | `00:01:00` |
| `FullClassName` | Custom health check class | String | - |
| `PublishChecks` | Publish results to storage | Boolean | `true` |
| `Alert` | Enable alerting | Boolean | `false` |
| `AlertBehaviour` | Alert configurations | Array | - |
| `AdditionalTags` | Custom tags for organization | String[] | - |
| `ExcludedInterceptionNames` | Excluded interceptor names | String[] | - |

*Required depending on ServiceType

### Alert Behavior Properties

| Property | Description | Type | Default |
|----------|-------------|------|---------|
| `TransportMethod` | Alert transport type | Enum | - |
| `TransportName` | Reference to transport config | String | - |
| `AlertOnce` | Send single alert per failure | Boolean | `false` |
| `AlertOnServiceRecovered` | Alert on recovery | Boolean | `true` |
| `AlertEvery` | Alert frequency | TimeSpan | `00:05:00` |
| `AlertByFailCount` | Alert after N failures | Integer | `1` |
| `StartAlertingOn` | Start time for alerts | TimeSpan | `00:00:00` |
| `StopAlertingOn` | Stop time for alerts | TimeSpan | `23:59:59` |
| `PublishAllResults` | Publish all check results | Boolean | `false` |
| `IncludeEnvironment` | Include environment info | Boolean | `false` |
| `Timezone` | Timezone for time-based alerts | String | `UTC` |

### Transport Settings

#### Email Transport

| Property | Description | Type | Required |
|----------|-------------|------|----------|
| `Name` | Transport identifier | String | ✅ |
| `From` | Sender email address | String | ✅ |
| `DisplayName` | Sender display name | String | - |
| `To` | Recipient email address | String | ✅ |
| `SmtpHost` | SMTP server hostname | String | ✅ |
| `SmtpPort` | SMTP server port | Integer | ✅ |
| `Authentication` | Enable SMTP authentication | Boolean | `false` |
| `Username` | SMTP username | String | - |
| `Password` | SMTP password | String | - |
| `Template` | Email template type | String | `Plain` |

#### Slack Transport

| Property | Description | Type | Required |
|----------|-------------|------|----------|
| `Name` | Transport identifier | String | ✅ |
| `Token` | Slack bot token | String | ✅ |
| `Channel` | Slack channel name | String | ✅ |
| `Username` | Bot display name | String | - |

#### Telegram Transport

| Property | Description | Type | Required |
|----------|-------------|------|----------|
| `Name` | Transport identifier | String | ✅ |
| `BotApiToken` | Telegram bot API token | String | ✅ |
| `ChatId` | Telegram chat ID | String | ✅ |

**Telegram Alert Example:**

![Telegram Alert Screenshot](Telegram.png)

#### InfluxDB Transport

| Property | Description | Type | Required |
|----------|-------------|------|----------|
| `Name` | Transport identifier | String | ✅ |
| `Host` | InfluxDB server URL | String | ✅ |
| `Database` | Database name | String | ✅ |

#### Custom API Transport

| Property | Description | Type | Required |
|----------|-------------|------|----------|
| `Name` | Transport identifier | String | ✅ |
| `BaseEndpoint` | API base URL | String | ✅ |
| `ApiKey` | API authentication key | String | ✅ |
| `ProjectName` | Project identifier | String | - |
| `Environment` | Environment name | String | - |

#### SignalR Transport

| Property | Description | Type | Required |
|----------|-------------|------|----------|
| `Name` | Transport identifier | String | ✅ |
| `HubUrl` | SignalR hub URL | String | ✅ |
| `HubMethod` | Hub method name | String | `ReceiveHealthAlert` |

## 🎯 Use Cases and Examples

### Microservices Architecture

Monitor multiple microservices with different alert priorities:

```yaml
HealthChecks:
  # Critical user-facing API
  - Name: "User API"
    ServiceType: Http
    EndpointOrHost: "https://user-api.company.com/health"
    Alert: true
    AlertBehaviour:
      - TransportMethod: Slack
        TransportName: "CriticalAlerts"
        AlertEvery: "00:01:00"
    AdditionalTags: ["critical", "user-facing"]
  
  # Internal service with less critical monitoring
  - Name: "Internal Cache"
    ServiceType: Redis
    ConnectionString: "internal-redis:6379"
    Alert: true
    AlertBehaviour:
      - TransportMethod: Email
        TransportName: "DevOpsEmail"
        AlertEvery: "00:15:00"
    AdditionalTags: ["internal", "cache"]
```

### Database Monitoring with Custom Queries

Monitor database health with business logic validation:

#### SQL Server Example

```yaml
HealthChecks:
  - Name: "Order Processing Health"
    ServiceType: MsSql
    ConnectionString: "Server=db.company.com;Database=Orders;..."
    HealthCheckConditions:
      SqlBehaviour:
        Query: |
          SELECT COUNT(*) 
          FROM Orders 
          WHERE CreatedDate > DATEADD(hour, -1, GETDATE()) 
          AND Status = 'Processing'
        ResultExpression: LessThan
        SqlResultDataType: Int
        ExpectedResult: 100  # Alert if more than 100 orders stuck processing
    Alert: true
    AlertBehaviour:
      - TransportMethod: Telegram
        TransportName: "BusinessAlerts"
```

#### PostgreSQL Example

```yaml
HealthChecks:
  - Name: "PostgreSQL User Activity"
    ServiceType: PostgreSql
    ConnectionString: "Host=postgres.company.com;Port=5432;Database=app;Username=monitor;Password=secret;SSL Mode=Require"
    MonitoringInterval: "00:02:00"
    HealthCheckConditions:
      SqlBehaviour:
        Query: |
          SELECT COUNT(*) 
          FROM active_sessions 
          WHERE last_activity > NOW() - INTERVAL '5 minutes'
        ResultExpression: GreaterThan
        SqlResultDataType: Int
        ExpectedResult: 0
    Alert: true
    AlertBehaviour:
      - TransportMethod: Email
        TransportName: "DevOpsEmail"
    AdditionalTags:
      - "database"
      - "postgresql"
```

**PostgreSQL Connection String Options:**
- `Host` - Server hostname or IP
- `Port` - Server port (default: 5432)
- `Database` - Database name
- `Username` - Database user
- `Password` - User password
- `SSL Mode` - SSL connection mode (Disable, Allow, Prefer, Require)
- `Timeout` - Connection timeout in seconds
- `Pooling` - Enable/disable connection pooling (default: true)

#### MySQL Example

```yaml
HealthChecks:
  - Name: "MySQL Replication Lag"
    ServiceType: MySql
    ConnectionString: "Server=mysql.company.com;Database=app;Uid=monitor;Pwd=secret;"
    HealthCheckConditions:
      SqlBehaviour:
        Query: "SHOW SLAVE STATUS"
        ResultExpression: Equal
        SqlResultDataType: Int
        ExpectedResult: 0
    Alert: true
```

### Environment-Specific Configuration

Configure different monitoring for different environments:

```yaml
# Production - High frequency monitoring
Monitoring:
  Settings:
    UseGlobalServiceName: "Production Services"
  HealthChecks:
    - Name: "Production API"
      MonitoringInterval: "00:00:30"  # 30 seconds
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          AlertEvery: "00:01:00"      # Immediate alerts

---
# Development - Relaxed monitoring  
Monitoring:
  Settings:
    UseGlobalServiceName: "Development Services"
  HealthChecks:
    - Name: "Dev API"
      MonitoringInterval: "00:05:00"  # 5 minutes
      Alert: false                    # No alerts in dev
```

## 🛠️ Tools and Utilities

### Configuration Generator

The solution includes a Windows Forms application for visual configuration management:

- **Location**: `Simple.Service.Monitoring.Config.Generator`
- **Features**:
  - Visual health check configuration
  - Real-time validation
  - Export to JSON/YAML
  - Import existing configurations

### Sample Implementation

Reference the sample API project for implementation examples:

- **Location**: `Simple.Service.Monitoring.Sample.API`
- **Features**:
  - Custom health check implementations
  - Multiple transport configurations
  - Real-world monitoring scenarios

## 🔍 Dashboard Features

### Real-time Monitoring Dashboard

Access the web dashboard at `/monitoring` (configurable) to view:

- **Live Status**: Real-time health status with automatic updates
- **Timeline View**: Historical health data visualization
- **Service Groups**: Organized view by tags and categories
- **Alert History**: Complete audit trail of all alerts
- **Performance Metrics**: Response times and availability statistics

### Dark Mode Support

The dashboard includes a modern dark mode with:

- Automatic system preference detection
- Manual toggle option
- Persistent user preferences
- Consistent theming across all components

### Mobile Responsiveness

Fully responsive design supporting:

- Mobile phones and tablets
- Touch-friendly interactions
- Optimized layouts for different screen sizes
- Offline-capable PWA features

## 🚀 Performance and Scalability

### Lightweight Architecture

- **Minimal Dependencies**: Built on standard .NET health check framework
- **Efficient Polling**: Configurable intervals prevent resource waste
- **Async Operations**: Non-blocking health check execution
- **Memory Optimized**: LiteDB option for minimal memory footprint

### Enterprise Ready

- **Multiple Storage Backends**: Scale from single instance to enterprise
- **Load Balancer Support**: Health checks work behind load balancers
- **Container Ready**: Docker and Kubernetes compatible
- **Cloud Native**: Works with Azure, AWS, and other cloud platforms

## 🤝 Contributing

We welcome contributions! Please see our contributing guidelines:

1. Fork the repository
1. Create a feature branch (`git checkout -b feature/amazing-feature`)
1. Commit your changes (`git commit -m 'Add some amazing feature'`)
1. Push to the branch (`git push origin feature/amazing-feature`)
1. Open a Pull Request

### Development Setup

1. Clone the repository
1. Open in Visual Studio 2022 or VS Code
1. Restore NuGet packages: `dotnet restore`
1. Build solution: `dotnet build`
1. Run tests: `dotnet test`

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built on top of Microsoft.Extensions.Diagnostics.HealthChecks framework
- Inspired by enterprise monitoring needs
- Community contributions and feedback

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/issues)
- **Discussions**: [GitHub Discussions](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/discussions)
- **Documentation**: [Wiki](https://github.com/turric4n/Dotnet.Simple.Service.Monitoring/wiki)

---

Made with ❤️ for the .NET community
