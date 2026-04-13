# Introduction

## What is Kythr?

**Kythr** is an enterprise-grade health monitoring and alerting system for .NET applications. It provides **26 service monitors**, **24 transport publishers**, a **React-based real-time dashboard**, and intelligent alerting — all driven by configuration (JSON/YAML).

## Key Features

### Comprehensive Monitoring (26 Service Types)

| Category | Types |
|----------|-------|
| **Web & API** | HTTP/HTTPS, gRPC, TCP, Ping/ICMP, DNS, FTP, SMTP |
| **SQL Databases** | SQL Server, MySQL, PostgreSQL, Oracle, SQLite |
| **NoSQL & Cache** | Redis, Elasticsearch, MongoDB, CosmosDB, Memcached |
| **Message Brokers** | RabbitMQ, Kafka, Azure Service Bus, AWS SQS |
| **Infrastructure** | Docker, SSL Certificate, Hangfire |
| **Custom** | Any `IHealthCheck` implementation, Request Interceptors |

### Intelligent Alerting (24 Transport Channels)

| Category | Transports |
|----------|-----------|
| **Chat** | Slack, Telegram, Discord, Microsoft Teams, Google Chat, Mattermost |
| **Email** | SMTP with HTML templates |
| **Incident** | PagerDuty, Opsgenie |
| **Metrics** | Prometheus, Datadog, CloudWatch, App Insights, InfluxDB, Elasticsearch |
| **Streaming** | Kafka, Redis Pub/Sub, RabbitMQ |
| **Webhooks** | Generic Webhook, Custom API, SignalR |
| **Local** | Console, File Log |

### Modern React Dashboard

- Real-time updates via SignalR WebSocket
- Timeline visualization with service name grouping
- Active services filter
- Dark mode with system preference detection
- Mobile-responsive design
- Custom branding (company logo + name)
- Built with React 18, TypeScript, Tailwind CSS, shadcn/ui

### Developer-Friendly

- **Configuration-first**: JSON or YAML — no code required
- **Hot reload**: Change configuration without restarting
- **3 storage backends**: InMemory, LiteDB, SQL Server
- **Extensible**: Custom health checks and transport publishers
- **NuGet packages**: Simple integration
- **Docker ready**: `docker-compose.yml` included

## Architecture

Built on the .NET HealthChecks framework, leveraging [AspNetCore.Diagnostics.HealthChecks](https://github.com/xabaril/AspNetCore.Diagnostics.HealthChecks).

```
┌────────────────────────────────────────────────────────────┐
│                   Your .NET Application                     │
├────────────────────────────────────────────────────────────┤
│  Kythr.Extensions (Integration Layer)   │
├────────────────────────────────────────────────────────────┤
│         Kythr.Library (Core)            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │  26 Service  │  │ 24 Transport │  │   Storage    │    │
│  │   Monitors   │  │  Publishers  │  │  Repository  │    │
│  └──────────────┘  └──────────────┘  └──────────────┘    │
├────────────────────────────────────────────────────────────┤
│  Kythr.UI (React SPA Dashboard)        │
└────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Health Checks** run at configured intervals
2. **Results** are evaluated and stored in the data repository
3. **Alert rules** determine whether to fire (fail count, time window, deduplication)
4. **Transport publishers** deliver alerts to configured channels
5. **Dashboard** displays real-time status via SignalR

## Getting Started

Ready to start? See the [Quick Start Guide](Quick-Start-Guide.md) to be up and running in minutes.

## Questions?

- Check the [FAQ](FAQ.md)
- [Open an issue](https://github.com/turric4n/Dotnet.Kythr/issues) on GitHub
- Start a [Discussion](https://github.com/turric4n/Dotnet.Kythr/discussions)
