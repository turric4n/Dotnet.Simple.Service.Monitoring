# Migration Guide: v1.x → v2.0

## Overview

v2.0 is **fully backward compatible** with v1.x configurations. All existing health checks and transport settings continue to work unchanged. v2.0 adds new capabilities that you can adopt incrementally.

## What's New

| Feature | v1.x | v2.0 |
|---------|------|------|
| Service monitors | 11 | 26 |
| Transport publishers | 7 | 24 |
| HTTP custom headers | ❌ | ✅ |
| Detailed failure reports | ❌ | ✅ |
| Telegram HTML formatting | ❌ | ✅ |
| Dashboard technology | Vanilla TypeScript | React 18 SPA |
| Timeline grouping | ❌ | ✅ |
| Active services filter | ❌ | ✅ |

## Step-by-Step Migration

### 1. Update Packages

```bash
dotnet add package Simple.Service.Monitoring.Extensions --version 2.0.0
dotnet add package Simple.Service.Monitoring.UI.Extensions --version 2.0.0
```

### 2. Rebuild the UI (if using from source)

```bash
cd Simple.Service.Monitoring.UI
npm install
npm run build
```

### 3. (Optional) Adopt New Features

#### HTTP Custom Headers

Add `HttpCustomHeaders` to any existing HTTP health check:

```yaml
# Before (v1.x)
HealthCheckConditions:
  HttpBehaviour:
    HttpExpectedCode: 200
    HttpVerb: Get

# After (v2.0) — add headers
HealthCheckConditions:
  HttpBehaviour:
    HttpExpectedCode: 200
    HttpVerb: Get
    HttpCustomHeaders:
      Authorization: "Bearer <token>"
      X-Api-Key: "key-123"
```

#### New Service Monitors

Add any of the 15 new monitor types to your `HealthChecks` array:

```yaml
HealthChecks:
  # ... existing checks ...
  
  # New in v2.0
  - Name: "Kafka Broker"
    ServiceType: Kafka
    EndpointOrHost: "kafka:9092"
  
  - Name: "gRPC Service"
    ServiceType: Grpc
    EndpointOrHost: "https://grpc-service:50051"
  
  - Name: "SSL Certificate"
    ServiceType: SslCertificate
    EndpointOrHost: "api.company.com"
    Port: 443
```

#### New Transport Publishers

Add any of the 17 new transport types. Example — adding Discord alongside existing Slack:

```yaml
# Existing
SlackTransportSettings:
  - Name: "OpsSlack"
    Token: "xoxb-token"
    Channel: "#alerts"

# New in v2.0
DiscordTransportSettings:
  - Name: "OpsDiscord"
    WebhookUrl: "https://discord.com/api/webhooks/..."

# Use both in alert behavior
AlertBehaviour:
  - TransportMethod: Slack
    TransportName: "OpsSlack"
  - TransportMethod: Discord
    TransportName: "OpsDiscord"
```

### 4. (Optional) Verify

Run the test suite to confirm everything works:

```bash
dotnet test Simple.Service.Monitoring.Tests/Simple.Service.Monitoring.Tests.csproj
```

## Breaking Changes

**None.** v2.0 is fully backward compatible.

## Known Differences

- The dashboard UI has been completely rewritten in React. Visual appearance and interaction patterns have changed, but all functionality is preserved and enhanced.
- Duration values in alerts now use `TotalMilliseconds` instead of `Milliseconds` — this is a bug fix that improves accuracy for durations > 1 second.
