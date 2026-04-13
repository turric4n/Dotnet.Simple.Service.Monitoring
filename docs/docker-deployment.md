# Docker Deployment Guide

Deploy Kythr as a standalone Docker container with the full monitoring suite and dashboard UI.

## Overview

The Docker image packages the entire monitoring application — 26 service monitor types, 24 alert transport channels, and the React dashboard UI — into a single container. All configuration is parameterizable via:

- **YAML file** (mounted volume) — best for complex configurations
- **Environment variables** — best for secrets and simple overrides
- **Mix of both** — recommended: YAML for structure, env vars for secrets

**Configuration priority** (highest wins):
1. Environment variables
2. `appsettings.{Environment}.yml`
3. `appsettings.yml`
4. Defaults

---

## Building the Image

The application is compiled **outside Docker**. The Dockerfile only packages the pre-built output into a lean runtime image.

### Using the build script (recommended)

**Windows (PowerShell):**
```powershell
.\build-docker.ps1                          # build with :latest tag
.\build-docker.ps1 -Tag "2.0.0"            # build with version tag
.\build-docker.ps1 -Tag "2.0.0" -Push      # build and push to Docker Hub
```

**Linux / macOS / CI:**
```bash
chmod +x build-docker.sh
./build-docker.sh                           # build with :latest tag
./build-docker.sh --tag 2.0.0              # build with version tag
./build-docker.sh --tag 2.0.0 --push       # build and push to Docker Hub
```

### Manual build

```bash
# Step 1: Publish the application
dotnet publish Kythr/Kythr.csproj \
    -c Release -o ./publish /p:UseAppHost=false

# Step 2: Build the Docker image (context = publish folder)
docker build -t turric4n/kythr:latest \
    -f Kythr/Dockerfile ./publish
```

### Publishing to Docker Hub

```bash
docker login
docker push turric4n/kythr:latest
docker push turric4n/kythr:2.0.0
```

---

## Running the Container

### Quick start (defaults only)

```bash
docker run -p 5000:5000 turric4n/kythr:latest
```

The dashboard UI is available at `http://localhost:5000`.

### With mounted YAML configuration

```bash
# 1. Copy the template
cp Kythr/appsettings.docker.yml ./config/appsettings.yml

# 2. Edit config/appsettings.yml with your monitors and transports

# 3. Run with mounted config
docker run -p 5000:5000 \
    -v $(pwd)/config/appsettings.yml:/app/appsettings.yml:ro \
    -v monitoring-data:/app/data \
    turric4n/kythr:latest
```

### With environment variables only

```bash
docker run -p 5000:5000 \
    -e "MONITORINGUI_COMPANYNAME=ACME Corp" \
    -e "MONITORING_SETTINGS_USEGLOBALSERVICENAME=Production" \
    -e "MONITORING_HEALTHCHECKS_0_NAME=API Health" \
    -e "MONITORING_HEALTHCHECKS_0_SERVICETYPE=Http" \
    -e "MONITORING_HEALTHCHECKS_0_ENDPOINTORHOST=https://api.example.com/health" \
    -e "MONITORING_HEALTHCHECKS_0_ALERT=true" \
    -e "MONITORING_HEALTHCHECKS_0_HEALTHCHECKCONDITIONS_HTTPBEHAVIOUR_HTTPEXPECTEDCODE=200" \
    -e "MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTMETHOD=Console" \
    -e "MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTNAME=DefaultConsole" \
    -e "MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_ALERTONSERVICERECOVERED=true" \
    -e "MONITORING_CONSOLETRANSPORTSETTINGS_0_NAME=DefaultConsole" \
    -e "MONITORING_CONSOLETRANSPORTSETTINGS_0_USECOLORS=true" \
    turric4n/kythr:latest
```

### With Docker Compose (recommended)

```bash
# 1. Copy and customize the config
mkdir -p config
cp Kythr/appsettings.docker.yml config/appsettings.yml

# 2. Start the container
docker-compose -f docker-compose.monitoring.yml up -d

# 3. View logs
docker-compose -f docker-compose.monitoring.yml logs -f

# 4. Stop
docker-compose -f docker-compose.monitoring.yml down
```

### Recommended pattern: YAML + env var secrets

Use the YAML file for the full structure and override secrets via env vars:

```bash
docker run -p 5000:5000 \
    -v $(pwd)/config/appsettings.yml:/app/appsettings.yml:ro \
    -v monitoring-data:/app/data \
    -e "MONITORING_EMAILTRANSPORTSETTINGS_0_PASSWORD=real-smtp-password" \
    -e "MONITORING_SLACKTRANSPORTSETTINGS_0_TOKEN=xoxb-real-token" \
    -e "MONITORING_TELEGRAMTRANSPORTSETTINGS_0_BOTAPITOKEN=real-bot-token" \
    -e "MONITORING_PAGERDUTYTRANSPORTSETTINGS_0_ROUTINGKEY=real-key" \
    -e "MONITORING_HEALTHCHECKS_3_CONNECTIONSTRING=Server=prod-sql;Password=real-password" \
    turric4n/kythr:latest
```

---

## Environment Variable Reference

### Naming Convention

Use **UPPERCASE** with **single underscores** (`_`) as section separators. Arrays use **zero-based index**.

| YAML Path | Environment Variable |
|---|---|
| `MonitoringUi:CompanyName` | `MONITORINGUI_COMPANYNAME` |
| `MonitoringUi:DataRepositoryType` | `MONITORINGUI_DATAREPOSITORYTYPE` |
| `Monitoring:Settings:UseGlobalServiceName` | `MONITORING_SETTINGS_USEGLOBALSERVICENAME` |
| `Monitoring:HealthChecks[0]:Name` | `MONITORING_HEALTHCHECKS_0_NAME` |
| `Monitoring:HealthChecks[0]:ServiceType` | `MONITORING_HEALTHCHECKS_0_SERVICETYPE` |
| `Monitoring:HealthChecks[0]:EndpointOrHost` | `MONITORING_HEALTHCHECKS_0_ENDPOINTORHOST` |
| `Monitoring:HealthChecks[0]:ConnectionString` | `MONITORING_HEALTHCHECKS_0_CONNECTIONSTRING` |
| `Monitoring:HealthChecks[0]:Alert` | `MONITORING_HEALTHCHECKS_0_ALERT` |
| `Monitoring:HealthChecks[0]:HealthCheckConditions:HttpBehaviour:HttpExpectedCode` | `MONITORING_HEALTHCHECKS_0_HEALTHCHECKCONDITIONS_HTTPBEHAVIOUR_HTTPEXPECTEDCODE` |
| `Monitoring:HealthChecks[0]:AlertBehaviour[0]:TransportMethod` | `MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTMETHOD` |
| `Monitoring:HealthChecks[0]:AlertBehaviour[0]:TransportName` | `MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTNAME` |
| `Monitoring:EmailTransportSettings[0]:Name` | `MONITORING_EMAILTRANSPORTSETTINGS_0_NAME` |
| `Monitoring:EmailTransportSettings[0]:Password` | `MONITORING_EMAILTRANSPORTSETTINGS_0_PASSWORD` |
| `Monitoring:SlackTransportSettings[0]:Token` | `MONITORING_SLACKTRANSPORTSETTINGS_0_TOKEN` |

### MonitoringUi Options

| Variable | Type | Default | Description |
|---|---|---|---|
| `MONITORINGUI_COMPANYNAME` | string | | Company name in dashboard header |
| `MONITORINGUI_HEADERLOGOURL` | string | | Logo URL for dashboard header |
| `MONITORINGUI_DATAREPOSITORYTYPE` | enum | `LiteDb` | `InMemory`, `LiteDb`, or `Sql` |
| `MONITORINGUI_SQLCONNECTIONSTRING` | string | | SQL connection (when `DataRepositoryType=Sql`) |

### Service Types

| ServiceType | Required Fields | Behaviour Class |
|---|---|---|
| `Http` | `EndpointOrHost` | `HttpBehaviour` |
| `Ping` | `EndpointOrHost` | `PingBehaviour` |
| `Tcp` | `EndpointOrHost` | `TcpBehaviour` |
| `Dns` | `EndpointOrHost` | `DnsBehaviour` |
| `SslCertificate` | `EndpointOrHost` | `SslCertificateBehaviour` |
| `Ftp` | `EndpointOrHost` | `FtpBehaviour` |
| `Smtp` | `EndpointOrHost` | `SmtpBehaviour` |
| `MsSql` | `ConnectionString` | `SqlBehaviour` |
| `MySql` | `ConnectionString` | `SqlBehaviour` |
| `PostgreSql` | `ConnectionString` | `SqlBehaviour` |
| `Oracle` | `ConnectionString` | `SqlBehaviour` |
| `Sqlite` | `ConnectionString` | `SqlBehaviour` |
| `Redis` | `ConnectionString` | `RedisBehaviour` |
| `Memcached` | `EndpointOrHost` | `MemcachedBehaviour` |
| `MongoDb` | `ConnectionString` | `MongoDbBehaviour` |
| `CosmosDb` | `ConnectionString` | `CosmosDbBehaviour` |
| `Rmq` | `ConnectionString` | — |
| `Kafka` | `EndpointOrHost` | `KafkaBehaviour` |
| `AzureServiceBus` | `ConnectionString` | `AzureServiceBusBehaviour` |
| `AwsSqs` | — | `AwsSqsBehaviour` |
| `ElasticSearch` | `EndpointOrHost` | — |
| `Hangfire` | `ConnectionString` | `HangfireBehaviour` |
| `Grpc` | `EndpointOrHost` | `GrpcBehaviour` |
| `Docker` | — | `DockerBehaviour` |
| `Custom` | `FullClassName` | — |
| `Interceptor` | — | — |

### Alert Transport Methods

| TransportMethod | Settings Array | Key Fields |
|---|---|---|
| `Email` | `EmailTransportSettings` | `From`, `To`, `SmtpHost`, `SmtpPort`, `Username`, `Password` |
| `Slack` | `SlackTransportSettings` | `Token`, `Channel`, `Username` |
| `Telegram` | `TelegramTransportSettings` | `BotApiToken`, `ChatId` |
| `Influx` | `InfluxDbTransportSettings` | `Host`, `Database`, `Version`, `Token` |
| `CustomApi` | `CustomNotificationTransportSettings` | `BaseEndpoint`, `ApiKey` |
| `SignalR` | `SignalRTransportSettings` | `HubUrl`, `HubMethod` |
| `Teams` | `TeamsTransportSettings` | `WebhookUrl` |
| `Discord` | `DiscordTransportSettings` | `WebhookUrl`, `Username` |
| `PagerDuty` | `PagerDutyTransportSettings` | `RoutingKey`, `Severity` |
| `Opsgenie` | `OpsgenieTransportSettings` | `ApiKey`, `Priority` |
| `Datadog` | `DatadogTransportSettings` | `ApiKey`, `ApplicationKey`, `Site` |
| `Prometheus` | `PrometheusTransportSettings` | `PushgatewayUrl`, `JobName` |
| `CloudWatch` | `CloudWatchTransportSettings` | `Region`, `AccessKey`, `SecretKey` |
| `AppInsights` | `AppInsightsTransportSettings` | `ConnectionString`, `InstrumentationKey` |
| `Elasticsearch` | `ElasticsearchTransportSettings` | `Nodes[]`, `IndexPrefix` |
| `GoogleChat` | `GoogleChatTransportSettings` | `WebhookUrl` |
| `Mattermost` | `MattermostTransportSettings` | `WebhookUrl`, `Channel` |
| `Console` | `ConsoleTransportSettings` | `UseColors`, `OutputFormat` |
| `FileLog` | `FileTransportSettings` | `FilePath`, `MaxFileSizeBytes` |
| `RabbitMq` | `RmqTransportSettings` | `ConnectionString`, `Exchange`, `RoutingKey` |
| `KafkaTransport` | `KafkaTransportSettings` | `BootstrapServers`, `Topic` |
| `Webhook` | `WebhookTransportSettings` | `WebhookUrl`, `Headers` |
| `RedisTransport` | `RedisTransportSettings` | `Host`, `Port` |

---

## Data Persistence

The dashboard UI stores historical data. Storage backends:

| Backend | Config | Persistence | Notes |
|---|---|---|---|
| **LiteDb** (default) | `DataRepositoryType: LiteDb` | File at `/app/data/` | Mount a volume to persist across restarts |
| **InMemory** | `DataRepositoryType: InMemory` | None | Data lost on container restart |
| **SQL Server** | `DataRepositoryType: Sql` | Database | Set `SqlConnectionString` |

For LiteDb persistence, mount a Docker volume:

```bash
docker run -v monitoring-data:/app/data ...
```

Or in docker-compose:

```yaml
volumes:
  - monitoring-data:/app/data
```

---

## Example Configurations

### Minimal: Monitor two HTTP endpoints

```yaml
# config/appsettings.yml
Monitoring:
  Settings:
    UseGlobalServiceName: "Production"
  HealthChecks:
    - Name: "Main API"
      ServiceType: Http
      EndpointOrHost: "https://api.myapp.com/health"
      Alert: true
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
      AlertBehaviour:
        - TransportMethod: Console
          TransportName: "DefaultConsole"
          AlertOnServiceRecovered: true
    - Name: "Auth Service"
      ServiceType: Http
      EndpointOrHost: "https://auth.myapp.com/health"
      Alert: true
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
      AlertBehaviour:
        - TransportMethod: Console
          TransportName: "DefaultConsole"
          AlertOnServiceRecovered: true
  ConsoleTransportSettings:
    - Name: "DefaultConsole"
      UseColors: true

MonitoringUi:
  CompanyName: "My Company"
  DataRepositoryType: LiteDb
```

### Production: HTTP + SQL + Redis with Slack and Email alerts

```yaml
# config/appsettings.yml
Monitoring:
  Settings:
    UseGlobalServiceName: "Production Services"
  HealthChecks:
    - Name: "API Gateway"
      ServiceType: Http
      EndpointOrHost: "https://api.myapp.com/health"
      Alert: true
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
          HttpTimeoutMs: 5000
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "OpsEmail"
          AlertOnce: false
          AlertOnServiceRecovered: true
          AlertEvery: "00:05:00"
          AlertByFailCount: 3
        - TransportMethod: Slack
          TransportName: "OpsSlack"
          AlertOnce: true
          AlertOnServiceRecovered: true
    - Name: "Primary Database"
      ServiceType: MsSql
      ConnectionString: "Server=db.internal;Database=MyApp;User Id=monitor;Password=PLACEHOLDER;"
      Alert: true
      HealthCheckConditions:
        SqlBehaviour:
          Query: "SELECT 1"
          ResultExpression: Equal
          SqlResultDataType: Int
          ExpectedResult: 1
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "OpsEmail"
          AlertOnce: false
          AlertOnServiceRecovered: true
          AlertEvery: "00:10:00"
    - Name: "Redis Cache"
      ServiceType: Redis
      ConnectionString: "redis.internal:6379"
      Alert: true
      HealthCheckConditions:
        RedisBehaviour:
          TimeOutMs: 3000
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "OpsSlack"
          AlertOnce: true
          AlertOnServiceRecovered: true
  EmailTransportSettings:
    - Name: "OpsEmail"
      From: "monitoring@myapp.com"
      DisplayName: "Service Monitor"
      To: "ops@myapp.com"
      SmtpHost: "smtp.myapp.com"
      SmtpPort: 587
      Authentication: true
      Username: "monitoring@myapp.com"
      Password: "PLACEHOLDER"
  SlackTransportSettings:
    - Name: "OpsSlack"
      Token: "PLACEHOLDER"
      Channel: "#ops-alerts"
      Username: "Monitor Bot"

MonitoringUi:
  CompanyName: "My Company"
  DataRepositoryType: LiteDb
```

Then run with secrets injected via env vars:

```bash
docker run -p 5000:5000 \
    -v $(pwd)/config/appsettings.yml:/app/appsettings.yml:ro \
    -v monitoring-data:/app/data \
    -e "MONITORING_HEALTHCHECKS_1_CONNECTIONSTRING=Server=db.internal;Database=MyApp;User Id=monitor;Password=RealSecret123" \
    -e "MONITORING_EMAILTRANSPORTSETTINGS_0_PASSWORD=real-smtp-password" \
    -e "MONITORING_SLACKTRANSPORTSETTINGS_0_TOKEN=xoxb-real-slack-token" \
    turric4n/kythr:latest
```

---

## Ports

| Port | Protocol | Description |
|---|---|---|
| 5000 | HTTP | Dashboard UI + Health endpoints |

---

## Troubleshooting

**Container starts but no monitors run:**
- Check that `appsettings.yml` is mounted correctly: `docker exec <container> cat /app/appsettings.yml`
- Verify the `Monitoring` section exists and has `HealthChecks` entries

**Environment variables not taking effect:**
- Ensure you use double underscores (`__`) not dots (`.`) or colons (`:`)
- Array indices are zero-based: `__0__`, `__1__`, etc.
- Check variable names are case-sensitive on Linux

**LiteDb data lost on restart:**
- Mount a persistent volume: `-v monitoring-data:/app/data`

**Dashboard shows no data:**
- Verify `MonitoringUi__DataRepositoryType` is set to `LiteDb` (default) or `Sql`
- Check that `/app/data` is writable inside the container
