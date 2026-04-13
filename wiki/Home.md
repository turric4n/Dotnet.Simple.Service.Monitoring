# Kythr - Wiki

Welcome to the **Kythr** wiki! This comprehensive guide covers the full v2.0 feature set: **26 service monitors**, **24 transport publishers**, and a **React-based real-time dashboard**.

## 📚 Table of Contents

### Getting Started
- [Introduction](Introduction.md) — Overview and key features
- [Quick Start Guide](Quick-Start-Guide.md) — Get up and running in minutes

### Configuration
- [Service Types](Service-Types.md) — All 26 supported service monitoring types
- [Transport Methods](Transport-Methods.md) — All 24 alert transport channels
- [Alert Configuration](Alert-Configuration.md) — Intelligent alerting rules

### Dashboard & UI
- [Status Page Component](Status-Page-Component.md) — Timeline visualization and status indicators

### Troubleshooting
- [FAQ](FAQ.md) — Frequently asked questions

### Extended Documentation

For detailed guides, see the [docs/](https://github.com/turric4n/Kythr/tree/master/docs) directory:

| Guide | Description |
|-------|-------------|
| [Architecture](https://github.com/turric4n/Kythr/blob/master/docs/architecture.md) | System architecture, data flow, component overview |
| [Service Monitors](https://github.com/turric4n/Kythr/blob/master/docs/service-monitors.md) | All 26 monitor types with full config examples |
| [Transport Publishers](https://github.com/turric4n/Kythr/blob/master/docs/transport-publishers.md) | All 24 transport channels with config examples |
| [Dashboard](https://github.com/turric4n/Kythr/blob/master/docs/dashboard.md) | React SPA dashboard features and configuration |
| [Deployment](https://github.com/turric4n/Kythr/blob/master/docs/deployment.md) | Docker, Kubernetes, Azure, IIS deployment |
| [Migration v1→v2](https://github.com/turric4n/Kythr/blob/master/docs/migration-v2.md) | Upgrade guide from v1.x to v2.0 |
| [Example Configs](https://github.com/turric4n/Kythr/blob/master/docs/examples/configurations.md) | Ready-to-use configuration examples |

## 🚀 Quick Links

- **[Quick Start Guide](Quick-Start-Guide.md)** — New to the project? Start here!
- **[Example Configurations](https://github.com/turric4n/Kythr/blob/master/docs/examples/configurations.md)** — Copy-paste ready configurations
- **[FAQ](FAQ.md)** — Common questions answered
- **[Contributing](https://github.com/turric4n/Kythr/blob/master/CONTRIBUTING.md)** — How to contribute

## 📦 Project Information

- **Repository**: [GitHub](https://github.com/turric4n/Kythr)
- **License**: MIT
- **Author**: Turrican (Enrique Fuentes)
- **Version**: 2.0.0

## 🤝 Community

- [Report Issues](https://github.com/turric4n/Kythr/issues)
- [Request Features](https://github.com/turric4n/Kythr/issues/new)
- [Discussions](https://github.com/turric4n/Kythr/discussions)

## 📝 v2.0 Highlights

- **15 new service monitors**: Kafka, gRPC, TCP, DNS, SSL Certificate, FTP, SMTP, Azure Service Bus, Memcached, Docker, AWS SQS, CosmosDB, MongoDB, Oracle, SQLite
- **17 new transport publishers**: Discord, Teams, Google Chat, Mattermost, PagerDuty, Opsgenie, Datadog, CloudWatch, App Insights, Prometheus, Kafka, Redis, RabbitMQ, Elasticsearch, Console, File Log
- **React SPA dashboard**: React 18 + TypeScript + Tailwind CSS + shadcn/ui
- **HTTP custom headers**, **detailed failure reports**, **timeline grouping**, **active services filter**
- TypeScript debugging integration
- Improved webpack configuration
- Extended configuration options

---

**Need help?** Check the [FAQ](FAQ.md) or [create an issue](https://github.com/turric4n/Kythr/issues).
