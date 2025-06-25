# Dotnet.Simple.Service.Monitoring

> Simplify Health Check Implementations with Easy Monitoring

## Overview

Dotnet.Simple.Service.Monitoring is a lightweight wrapper that simplifies the implementation of health checks in .NET applications. It leverages the .NET HealthChecks framework and the [AspNetCore.Diagnostics.HealthChecks](https://github.com/xabaril/AspNetCore.Diagnostics.HealthChecks) library by Xabaril.

With minimal configuration, you can monitor various services and receive alerts through multiple channels when issues arise.

## Features

- **Simple Configuration**: Define health checks through configuration files
- **Multiple Service Types**: Support for HTTP, ElasticSearch, SQL Server, RabbitMQ, Hangfire, and ICMP
- **Flexible Alerting**: Configure alerts based on specific conditions and timeframes
- **Multiple Transport Methods**: Publish alerts via Email, Slack, Telegram, and InfluxDB
- **Recovery Notifications**: Get notified when services recover

## Getting Started

### Installation

```bash
dotnet add package Dotnet.Simple.Service.Monitoring
```

### Basic Setup

In your `Startup.cs` or `Program.cs`:

```csharp
var monitoring = services.UseServiceMonitoring(Configuration)
  .UseSettings()
  .Build();
```

## Configuration

You can configure the monitoring in either JSON or YAML format.

### JSON Configuration (appSettings.json)

```json
{
  "Monitoring": {
  "HealthChecks": [
    {
    "Name": "Test",
    "ServiceType": "Http",
    "EndpointOrHost": "https://www.testendpoint.com/",
    "Port": 443,
    "HealthCheckConditions": {
      "HttpBehaviour": {
      "HttpExpectedResponseTimeMs": 300,
      "HttpExpectedCode": 200,
      "HttpVerb": "Get"
      },
      "ServiceReach": true,
      "ServiceConnectionEstablished": true
    },
    "PublishChecks": true,
    "Alert": true,
    "AlertBehaviour": [
      {
      "TransportMethod": "Email",
      "TransportName": "StandardEmailTransport",
      "AlertOnce": true,
      "AlertOnServiceRecovered": true,
      "StartAlertingOn": "",
      "StopAlertingOn": "",
      "AlertEvery": "00:00:05",
      "AlertOn": ""
      }
    ]
    }
  ],
  "EmailTransportSettings": [
    {
    "Name": "StandardEmailTransport",
    "From": "test@test.com",
    "To": "test@test.com",
    "SmtpHost": "test.test.com",
    "SmtpPort": 25,
    "Authentication": false,
    "Username": "",
    "Password": "",
    "Template": "Plain"
    }
  ]
  }
}
```

### YAML Configuration (appsettings.yml)

```yaml
Monitoring:
  HealthChecks:
  - Name: Test
  ServiceType: Http
  EndpointOrHost: https://www.testendpoint.com/
  Port: 443
  HealthCheckConditions:
    HttpBehaviour:
    HttpExpectedResponseTimeMs: 300
    HttpExpectedCode: 200
    HttpVerb: Get
    ServiceReach: true
    ServiceConnectionEstablished: true
  PublishChecks: true
  Alert: true
  AlertBehaviour:
  - TransportMethod: Email
    TransportName: StandardEmailTransport
    AlertOnce: true
    AlertOnServiceRecovered: true
    StartAlertingOn: ''
    StopAlertingOn: ''
    AlertEvery: '00:00:05'
    AlertOn: ''
  EmailTransportSettings:
  - Name: StandardEmailTransport
  From: test@test.com
  To: test@test.com
  SmtpHost: test.test.com
  SmtpPort: 25
  Authentication: false
  Username: ''
  Password: ''
  Template: Plain    
```

## Configuration Reference

### Health Checks Properties

| Property | Description | Type |
|----------|-------------|------|
| `Name` | Unique identifier for the health check | String |
| `ServiceType` | Type of service to monitor | Enum: `Http`, `ElasticSearch`, `MsSql`, `Rmq`, `Hangfire`, `Ping` |
| `EndpointOrHost` | URI or host address of the service | String |
| `Port` | Port number for the service | Integer |
| `HealthCheckConditions` | Specific conditions to check | Object |
| `PublishChecks` | Enable/disable check publishing | Boolean |
| `Alert` | Enable/disable alerts | Boolean |
| `AlertBehaviour` | Alert configuration | Array of Objects |

### Alert Behaviour Properties

| Property | Description | Type |
|----------|-------------|------|
| `TransportMethod` | Method to send alerts | Enum: `Email`, `Slack`, `Telegram`, `InfluxDb` |
| `TransportName` | Name of the defined transport | String |
| `AlertOnce` | Send only one notification on failure | Boolean |
| `AlertOnServiceRecovered` | Send notification when service recovers | Boolean |
| `StartAlertingOn` | Start alerts at specific time | DateTime |
| `StopAlertingOn` | Stop alerts at specific time | DateTime |
| `AlertEvery` | Time interval between alerts | TimeSpan |

### Transport Settings

#### Email Transport Properties

| Property | Description | Type |
|----------|-------------|------|
| `Name` | Unique identifier for the transport | String |
| `From` | Sender email address | String |
| `To` | Recipient email address | String |
| `SmtpHost` | SMTP server address | String |
| `SmtpPort` | SMTP server port | Integer |
| `Authentication` | Enable SMTP authentication | Boolean |
| `Username` | SMTP username (if authentication enabled) | String |
| `Password` | SMTP password (if authentication enabled) | String |
| `Template` | Email template type | String |

#### Slack, Telegram, and InfluxDB Transport Properties

Documentation for these transport methods is coming soon.

## License

MIT
