# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.0.0] - 2026-01

### Added

- **15 New Service Monitors**: Kafka, gRPC, TCP, DNS, SSL Certificate, FTP, SMTP, Azure Service Bus, Memcached, Docker, AWS SQS, CosmosDB, MongoDB, Oracle, SQLite
- **17 New Transport Publishers**: Discord, Microsoft Teams, Google Chat, Mattermost, PagerDuty, Opsgenie, Datadog, AWS CloudWatch, Azure Application Insights, Prometheus, Kafka, Redis Pub/Sub, RabbitMQ, Elasticsearch, Console, File Log
- **HTTP Custom Headers**: Attach `Authorization`, `X-Api-Key`, or any custom headers to HTTP health checks
- **Detailed Failure/Success Reports**: Rich diagnostic info in alerts (response time, status code, error details)
- **Enhanced Telegram Formatting**: HTML templates with emoji status indicators
- **React SPA Dashboard**: Complete rewrite with React 18, TypeScript, Tailwind CSS, shadcn/ui, Zustand, TanStack Query
- **Timeline Grouping**: Group health checks by service name across multiple machines
- **Active Services Filter**: Hide inactive monitors from the timeline view
- 19 new tests (HTTP headers, Telegram detailed reports, Redis concurrency)

### Fixed

- Duration reporting: `.Milliseconds` → `.TotalMilliseconds` for accurate timing
- Redis concurrency: `ObjectDisposedException` under concurrent access
- Telegram credential security: prevent accidental credential exposure

### Changed

- Dashboard technology: vanilla TypeScript → React 18 SPA
- Minimum target: .NET Standard 2.1 / .NET 6.0+ (tests require .NET 10)

## [1.0.17] - Previous

### Features

- 11 service monitors: HTTP, MsSql, MySql, PostgreSql, Redis, ElasticSearch, RabbitMQ, Hangfire, Ping, Custom, Interceptor
- 7 transport publishers: Email, Slack, Telegram, InfluxDB, Custom API, SignalR, Webhook
- Web dashboard with timeline visualization
- LiteDB and SQL Server storage backends
- Configuration via JSON/YAML
- Docker Compose for local development
