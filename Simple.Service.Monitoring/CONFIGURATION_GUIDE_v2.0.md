# Configuration Guide - Version 2.0

## Overview

This guide covers all configuration options for Dotnet.Simple.Service.Monitoring v2.0, including new features like HTTP custom headers and detailed alert results.

---

## Table of Contents

1. [Basic Configuration](#basic-configuration)
2. [HTTP Health Checks](#http-health-checks)
3. [Custom HTTP Headers](#custom-http-headers-new)
4. [Database Monitoring](#database-monitoring)
5. [Alert Configuration](#alert-configuration)
6. [Transport Settings](#transport-settings)
7. [Best Practices](#best-practices)
8. [Migration from v1.x](#migration-from-v1x)

---

## Basic Configuration

### Configuration File Structure

```yaml
MonitorOptions:
  Settings:
    UseGlobalServiceName: "MyApplication"
  
  HealthChecks:
    - Name: "Service Name"
      ServiceType: Http
      # ... health check configuration
  
  EmailTransportSettings:
    - Name: "EmailTransport"
      # ... email settings
  
  # ... other transport settings
```

---

## HTTP Health Checks

### Basic HTTP Monitoring

```yaml
HealthChecks:
  - Name: "Basic HTTP Check"
    ServiceType: Http
    EndpointOrHost: "https://api.example.com/health"
    Alert: true
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpTimeoutMs: 5000
        HttpVerb: Get
```

### Multiple Endpoints (Load Balancer)

```yaml
HealthChecks:
  - Name: "API Cluster"
    ServiceType: Http
    EndpointOrHost: "https://api1.example.com,https://api2.example.com,https://api3.example.com"
    Alert: true
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpTimeoutMs: 5000
        HttpVerb: Get
```

**Result**: Shows which endpoints failed:
```
❌ Failed (1):
  • https://api2.example.com returned 503

✅ Succeeded (2):
  • https://api1.example.com returned 200
  • https://api3.example.com returned 200
```

---

## Custom HTTP Headers (NEW)

### Bearer Token Authentication

```yaml
HealthChecks:
  - Name: "Secured API"
    ServiceType: Http
    EndpointOrHost: "https://api.example.com/secure/health"
    Alert: true
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpTimeoutMs: 5000
        HttpVerb: Get
        CustomHttpHeaders:
          Authorization: "Bearer ${API_TOKEN}"  # Use environment variable
          X-API-Version: "v2"
```

### API Key Authentication

```yaml
HealthChecks:
  - Name: "Third-Party API"
    ServiceType: Http
    EndpointOrHost: "https://api.external.com/health"
    Alert: true
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          X-API-Key: "${API_KEY}"
          X-API-Secret: "${API_SECRET}"
          Accept: "application/json"
```

### Request Tracking Headers

```yaml
HealthChecks:
  - Name: "Microservice"
    ServiceType: Http
    EndpointOrHost: "https://microservice.example.com/health"
    Alert: true
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          X-Request-ID: "health-check-001"
          X-Correlation-ID: "monitoring-service"
          X-Source: "HealthMonitor"
```

### Custom User-Agent

```yaml
HealthChecks:
  - Name: "Web Service"
    ServiceType: Http
    EndpointOrHost: "https://www.example.com/health"
    Alert: true
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          User-Agent: "MyCompany-HealthMonitor/2.0"
```

### Multiple Endpoints with Shared Headers

```yaml
HealthChecks:
  - Name: "API Cluster with Auth"
    ServiceType: Http
    EndpointOrHost: "https://api1.example.com,https://api2.example.com,https://api3.example.com"
    Alert: true
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          Authorization: "Bearer ${SHARED_TOKEN}"
          X-Environment: "Production"
```

---

## Database Monitoring

### SQL Server

```yaml
HealthChecks:
  - Name: "Primary Database"
    ServiceType: MsSql
    ConnectionString: "Server=localhost;Database=MyDB;User Id=sa;Password=${DB_PASSWORD};"
    Alert: true
    HealthCheckConditions:
      ServiceConnectionEstablished: true
      SqlBehaviour:
        Query: "SELECT COUNT(*) FROM Users WHERE IsActive = 1"
        SqlResultDataType: Integer
        ResultExpression: GreaterThan
        ExpectedResult: 0
```

### MySQL

```yaml
HealthChecks:
  - Name: "MySQL Database"
    ServiceType: MySql
    ConnectionString: "Server=localhost;Database=mydb;Uid=root;Pwd=${MYSQL_PASSWORD};"
    Alert: true
    HealthCheckConditions:
      ServiceConnectionEstablished: true
      SqlBehaviour:
        Query: "SELECT VERSION()"
        SqlResultDataType: String
        ResultExpression: Contains
        ExpectedResult: "8.0"
```

### PostgreSQL

```yaml
HealthChecks:
  - Name: "PostgreSQL Database"
    ServiceType: PostgreSql
    ConnectionString: "Host=localhost;Database=postgres;Username=postgres;Password=${PG_PASSWORD}"
    Alert: true
    HealthCheckConditions:
      ServiceConnectionEstablished: true
```

---

## Alert Configuration

### Basic Alert Setup

```yaml
AlertBehaviour:
  - TransportMethod: Email
    TransportName: "PrimaryEmail"
    AlertByFailCount: 3  # Alert after 3 consecutive failures
    AlertOnServiceRecovered: true
    AlertEvery: "00:05:00"  # Alert every 5 minutes
```

### Multiple Transports

```yaml
AlertBehaviour:
  - TransportMethod: Email
    TransportName: "OpsTeam"
    AlertByFailCount: 2
    AlertOnServiceRecovered: true
  
  - TransportMethod: Slack
    TransportName: "DevOpsChannel"
    AlertOnce: true
    AlertOnServiceRecovered: true
  
  - TransportMethod: Telegram
    TransportName: "CriticalAlerts"
    AlertByFailCount: 1
    AlertOnServiceRecovered: true
```

### Alert Scheduling

```yaml
AlertBehaviour:
  - TransportMethod: Email
    TransportName: "BusinessHours"
    AlertByFailCount: 2
    AlertOnServiceRecovered: true
    UsePeriodicAlerting: true
    StartAlertingOn: "08:00:00"  # 8 AM
    StopAlertingOn: "18:00:00"   # 6 PM
    Timezone: "America/New_York"
```

### Alert Once Per Episode

```yaml
AlertBehaviour:
  - TransportMethod: Email
    TransportName: "OpsTeam"
    AlertOnce: true  # Alert only on first failure
    AlertOnServiceRecovered: true  # Alert when recovered
    AlertEvery: "00:10:00"
```

---

## Transport Settings

### Email (SMTP)

```yaml
EmailTransportSettings:
  - Name: "PrimaryEmail"
    From: "monitoring@example.com"
    DisplayName: "Service Monitoring"
    To: "devops@example.com"
    SmtpHost: "smtp.gmail.com"
    SmtpPort: 587
    Authentication: true
    Username: "monitoring@example.com"
    Password: "${SMTP_PASSWORD}"
```

### Slack

```yaml
SlackTransportSettings:
  - Name: "DevOpsChannel"
    Token: "${SLACK_BOT_TOKEN}"
    Channel: "#devops-alerts"
    Username: "Monitoring Bot"
```

### Telegram

```yaml
TelegramTransportSettings:
  - Name: "CriticalAlerts"
    BotApiToken: "${TELEGRAM_BOT_TOKEN}"
    ChatId: "${TELEGRAM_CHAT_ID}"
```

**Security Note**: Use environment variables for all tokens!

```powershell
# Set environment variables
$env:TELEGRAM_BOT_TOKEN="123456:ABC-DEF..."
$env:TELEGRAM_CHAT_ID="-1001234567890"
```

### InfluxDB (Metrics)

```yaml
InfluxDbTransportSettings:
  - Name: "MetricsDB"
    Host: "http://localhost:8086"
    Database: "monitoring"
```

### SignalR (Real-time)

```yaml
SignalRTransportSettings:
  - Name: "MonitoringHub"
    HubUrl: "/hubs/monitoring"
    HubMethod: "ReceiveHealthAlert"
```

### Webhook

```yaml
WebhookTransportSettings:
  - Name: "CustomWebhook"
    WebhookUrl: "https://webhook.example.com/alerts"
    HttpVerb: Post
```

---

## Best Practices

### Security

**✅ DO:**
- Use environment variables for secrets
- Rotate tokens regularly
- Use HTTPS for all endpoints
- Implement token refresh for long-running monitors

**❌ DON'T:**
- Hardcode tokens in YAML files
- Commit secrets to Git
- Use production credentials for testing
- Share credentials across environments

### Reliability

**✅ DO:**
- Use `AlertByFailCount` to avoid false positives
- Set appropriate timeouts for each service
- Configure recovery alerts
- Use multiple alert channels for critical services

**❌ DON'T:**
- Set timeouts too low (causes false positives)
- Alert on every single failure
- Ignore recovery notifications
- Use same transport for all services

### Performance

**✅ DO:**
- Monitor multiple endpoints in single check
- Use PublishAllResults for metrics (InfluxDB)
- Set realistic health check intervals
- Tag health checks for easy filtering

**❌ DON'T:**
- Create separate checks for each endpoint
- Poll health endpoints too frequently
- Use very long timeouts
- Over-complicate health check queries

### Custom Headers

**✅ DO:**
- Use environment variables for tokens
- Add correlation IDs for tracing
- Set User-Agent for identification
- Use shared headers for load-balanced endpoints

**❌ DON'T:**
- Hardcode Bearer tokens
- Include secrets in header values (use refs)
- Set unnecessary headers
- Forget to rotate authentication tokens

---

## Migration from v1.x

### No Breaking Changes!

Version 2.0 is **100% backward compatible**. Existing configurations work unchanged.

### New Features to Adopt

#### 1. Add Custom Headers to HTTP Checks

**Before (v1.x)**:
```yaml
HealthCheckConditions:
  HttpBehaviour:
    HttpExpectedCode: 200
    HttpVerb: Get
```

**After (v2.0)** - Add authentication:
```yaml
HealthCheckConditions:
  HttpBehaviour:
    HttpExpectedCode: 200
    HttpVerb: Get
    CustomHttpHeaders:
      Authorization: "Bearer ${TOKEN}"
```

#### 2. Detailed Results Work Automatically

No configuration changes needed! Multi-endpoint checks now automatically show:

```
❌ Failed (2):
  • api2.example.com: 503 error
  • api3.example.com: timeout

✅ Succeeded (1):
  • api1.example.com: 200 OK
```

#### 3. Update Telegram Credentials

**Recommended** - Move to environment variables:

**Before**:
```yaml
TelegramTransportSettings:
  - Name: "Alerts"
    BotApiToken: "123456:ABC-DEF..."  # Hardcoded
    ChatId: "-1001234567890"
```

**After**:
```yaml
TelegramTransportSettings:
  - Name: "Alerts"
    BotApiToken: "${TELEGRAM_BOT_TOKEN}"  # Secure
    ChatId: "${TELEGRAM_CHAT_ID}"
```

---

## Complete Examples

### Minimal Configuration

```yaml
MonitorOptions:
  HealthChecks:
    - Name: "Simple Check"
      ServiceType: Http
      EndpointOrHost: "https://api.example.com/health"
      Alert: false  # No alerts, just monitoring
```

### Production-Ready Configuration

```yaml
MonitorOptions:
  Settings:
    UseGlobalServiceName: "MyApp-Production"
  
  HealthChecks:
    # Secured API with authentication
    - Name: "Main API"
      ServiceType: Http
      EndpointOrHost: "https://api1.example.com,https://api2.example.com"
      Alert: true
      AdditionalTags:
        - "production"
        - "critical"
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
          HttpTimeoutMs: 5000
          HttpVerb: Get
          CustomHttpHeaders:
            Authorization: "Bearer ${API_TOKEN}"
            X-Environment: "Production"
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "OpsTeam"
          AlertByFailCount: 3
          AlertOnServiceRecovered: true
          AlertEvery: "00:05:00"
        - TransportMethod: Slack
          TransportName: "CriticalAlerts"
          AlertByFailCount: 2
          AlertOnServiceRecovered: true
        - TransportMethod: Influx
          TransportName: "Metrics"
          PublishAllResults: true
    
    # Database monitoring
    - Name: "Primary Database"
      ServiceType: MsSql
      ConnectionString: "${DB_CONNECTION_STRING}"
      Alert: true
      AdditionalTags:
        - "database"
        - "critical"
      HealthCheckConditions:
        ServiceConnectionEstablished: true
        SqlBehaviour:
          Query: "SELECT COUNT(*) FROM ActiveSessions"
          SqlResultDataType: Integer
          ResultExpression: LessThan
          ExpectedResult: 1000
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "DBATeam"
          AlertByFailCount: 2
          AlertOnServiceRecovered: true
  
  EmailTransportSettings:
    - Name: "OpsTeam"
      From: "${SMTP_FROM}"
      To: "ops@example.com"
      SmtpHost: "${SMTP_HOST}"
      SmtpPort: 587
      Authentication: true
      Username: "${SMTP_USER}"
      Password: "${SMTP_PASSWORD}"
    
    - Name: "DBATeam"
      From: "${SMTP_FROM}"
      To: "dba@example.com"
      SmtpHost: "${SMTP_HOST}"
      SmtpPort: 587
      Authentication: true
      Username: "${SMTP_USER}"
      Password: "${SMTP_PASSWORD}"
  
  SlackTransportSettings:
    - Name: "CriticalAlerts"
      Token: "${SLACK_BOT_TOKEN}"
      Channel: "#critical-alerts"
      Username: "Production Monitor"
  
  InfluxDbTransportSettings:
    - Name: "Metrics"
      Host: "http://influx.example.com:8086"
      Database: "production_monitoring"
```

---

## Environment Variables

### Setting Environment Variables

**Windows (PowerShell)**:
```powershell
$env:API_TOKEN="your-token-here"
$env:TELEGRAM_BOT_TOKEN="123456:ABC-DEF..."
$env:TELEGRAM_CHAT_ID="-1001234567890"
$env:DB_CONNECTION_STRING="Server=..."
```

**Windows (Permanent)**:
```powershell
[Environment]::SetEnvironmentVariable("API_TOKEN", "your-token", "User")
```

**Linux/Mac**:
```bash
export API_TOKEN="your-token-here"
export TELEGRAM_BOT_TOKEN="123456:ABC-DEF..."
export TELEGRAM_CHAT_ID="-1001234567890"
```

**Docker**:
```yaml
services:
  monitoring:
    environment:
      - API_TOKEN=your-token
      - TELEGRAM_BOT_TOKEN=123456:ABC-DEF...
      - TELEGRAM_CHAT_ID=-1001234567890
```

**Kubernetes**:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: monitoring-secrets
type: Opaque
stringData:
  API_TOKEN: "your-token"
  TELEGRAM_BOT_TOKEN: "123456:ABC-DEF..."
  TELEGRAM_CHAT_ID: "-1001234567890"
```

---

## Troubleshooting

### HTTP Custom Headers Not Working

**Issue**: Headers not being sent

**Solutions**:
1. Check YAML syntax (proper indentation)
2. Verify header names (case-sensitive)
3. Ensure tokens are loaded from environment
4. Test with curl to verify endpoint requires headers

### Detailed Results Not Showing

**Issue**: Alerts don't show failure/success lists

**Solutions**:
1. Ensure using comma-separated endpoints
2. Check that health check is actually failing
3. Verify alert transport is updated (v2.0)
4. Review alert message format

### Authentication Failures

**Issue**: Getting 401/403 errors

**Solutions**:
1. Verify token format (Bearer vs API Key)
2. Check token expiration
3. Test token manually with curl/Postman
4. Ensure environment variables are set
5. Verify token permissions

---

## Reference

- **Full Demo**: See `appsettings.demo.yml`
- **HTTP Headers Guide**: [HTTP_CUSTOM_HEADERS_FEATURE.md](./Simple.Service.Monitoring.Library/Monitoring/Implementations/HTTP_CUSTOM_HEADERS_FEATURE.md)
- **Transport Guide**: [TRANSPORT_PUBLISHERS_DETAILED_RESULTS.md](./Simple.Service.Monitoring.Library/Monitoring/Implementations/Publishers/TRANSPORT_PUBLISHERS_DETAILED_RESULTS.md)
- **Release Notes**: [RELEASE_NOTES_v2.0.0.md](./RELEASE_NOTES_v2.0.0.md)
- **Quick Reference**: [QUICK_REFERENCE_v2.0.0.md](./QUICK_REFERENCE_v2.0.0.md)

---

## Summary

Version 2.0 Configuration Features:

✅ **CustomHttpHeaders** - Add authentication and tracking headers  
✅ **Detailed Results** - Automatic failure/success lists  
✅ **Enhanced Alerts** - Better formatted notifications  
✅ **Environment Variables** - Secure credential management  
✅ **Multiple Endpoints** - Load balancer monitoring  
✅ **Backward Compatible** - No breaking changes  

**Start using v2.0 features today!** 🚀
