# Transport Publishers Guide

This guide covers all 24 transport publisher types with configuration examples.

## Chat & Messaging

### Slack

```yaml
SlackTransportSettings:
  - Name: "OpsSlack"
    Token: "xoxb-your-slack-bot-token"
    Channel: "#ops-alerts"
    Username: "Health Monitor"
```

**AlertBehaviour reference:**
```yaml
AlertBehaviour:
  - TransportMethod: Slack
    TransportName: "OpsSlack"
```

### Telegram

```yaml
TelegramTransportSettings:
  - Name: "OpsTelegram"
    BotApiToken: "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11"
    ChatId: "-100123456789"
```

v2.0 features: HTML formatting, emoji status indicators, detailed failure/success reports.

### Discord

```yaml
DiscordTransportSettings:
  - Name: "OpsDiscord"
    WebhookUrl: "https://discord.com/api/webhooks/1234567890/abcdef"
```

### Microsoft Teams

```yaml
TeamsTransportSettings:
  - Name: "OpsTeams"
    WebhookUrl: "https://outlook.office.com/webhook/..."
```

### Google Chat

```yaml
GoogleChatTransportSettings:
  - Name: "OpsGoogleChat"
    WebhookUrl: "https://chat.googleapis.com/v1/spaces/SPACE_ID/messages?key=KEY&token=TOKEN"
```

### Mattermost

```yaml
MattermostTransportSettings:
  - Name: "OpsMattermost"
    WebhookUrl: "https://mattermost.company.com/hooks/xxx-xxx-xxx"
```

---

## Email

### SMTP Email

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

v2.0 features: HTML templates with detailed failure reports.

---

## Incident Management

### PagerDuty

```yaml
PagerDutyTransportSettings:
  - Name: "OpsPagerDuty"
    IntegrationKey: "your-pagerduty-integration-key"
```

Triggers incidents in PagerDuty when health checks fail and resolves them on recovery.

### Opsgenie

```yaml
OpsgenieTransportSettings:
  - Name: "OpsOpsgenie"
    ApiKey: "your-opsgenie-api-key"
```

Creates alerts in Opsgenie with appropriate priority levels.

---

## Metrics & Observability

### Prometheus

```yaml
PrometheusTransportSettings:
  - Name: "OpsPrometheus"
    PushgatewayUrl: "https://pushgateway.company.com"
```

Pushes health check metrics to a Prometheus Pushgateway for scraping by Prometheus.

### Datadog

```yaml
DatadogTransportSettings:
  - Name: "OpsDatadog"
    ApiKey: "your-datadog-api-key"
```

Sends health check events and metrics to Datadog.

### AWS CloudWatch

```yaml
CloudWatchTransportSettings:
  - Name: "OpsCloudWatch"
    Region: "us-east-1"
    Namespace: "HealthChecks"
```

Publishes custom metrics to AWS CloudWatch.

### Azure Application Insights

```yaml
AppInsightsTransportSettings:
  - Name: "OpsAppInsights"
    InstrumentationKey: "your-instrumentation-key"
```

Sends health check telemetry to Application Insights.

### InfluxDB

```yaml
InfluxDbTransportSettings:
  - Name: "OpsInflux"
    Host: "https://influx.company.com"
    Database: "health_metrics"
```

Stores time-series health check data in InfluxDB.

### Elasticsearch

Logs health check results to an Elasticsearch index.

---

## Streaming & Messaging

### Kafka Publisher

Publishes health check results to a Kafka topic for downstream consumers.

### Redis Pub/Sub

Publishes health check results via Redis pub/sub channels.

### RabbitMQ Publisher

Publishes health check results to a RabbitMQ exchange.

---

## Webhooks & API

### Generic Webhook

```yaml
WebhookTransportSettings:
  - Name: "OpsWebhook"
    Url: "https://api.company.com/health-webhook"
    Headers:
      Authorization: "Bearer your-token"
      Content-Type: "application/json"
```

Sends JSON-formatted health check results to any HTTP endpoint.

### Custom Notification API

```yaml
CustomNotificationTransportSettings:
  - Name: "OpsCustom"
    BaseEndpoint: "https://notifications.company.com/api"
    ApiKey: "your-api-key"
    ProjectName: "MyApp"
    Environment: "Production"
```

### SignalR

```yaml
SignalRTransportSettings:
  - Name: "OpsSignalR"
    HubUrl: "https://app.company.com/monitoringhub"
    HubMethod: "ReceiveHealthAlert"
```

Pushes real-time health updates to web clients via SignalR.

---

## Local

### Console

Outputs health check results to the application console/stdout. Useful for development and debugging.

### File Log

Writes health check results to a log file. Useful for environments without network-based transports.

---

## Multi-Channel Alerting

You can send the same health check result to multiple transports:

```yaml
HealthChecks:
  - Name: "Critical API"
    ServiceType: Http
    EndpointOrHost: "https://api.company.com/health"
    Alert: true
    AlertBehaviour:
      # Immediate PagerDuty for on-call
      - TransportMethod: PagerDuty
        TransportName: "CriticalPD"
        AlertByFailCount: 2
        AlertEvery: "00:01:00"
      
      # Slack for visibility
      - TransportMethod: Slack
        TransportName: "OpsSlack"
        AlertByFailCount: 1
        AlertEvery: "00:05:00"
      
      # Email for audit trail
      - TransportMethod: Email
        TransportName: "OpsEmail"
        AlertByFailCount: 3
        AlertEvery: "00:15:00"
        AlertOnServiceRecovered: true
      
      # Metrics for dashboards
      - TransportMethod: Prometheus
        TransportName: "OpsPrometheus"
        PublishAllResults: true
```
