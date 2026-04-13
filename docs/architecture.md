# Architecture Overview

## System Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                      Your .NET Application                         │
├────────────────────────────────────────────────────────────────────┤
│         Kythr.Extensions                       │
│    ┌──────────────────────────────────────────────────────────┐    │
│    │            AddServiceMonitoring(Configuration)            │    │
│    │            WithServiceMonitoringUi(services, config)      │    │
│    └──────────────────────────────────────────────────────────┘    │
├────────────────────────────────────────────────────────────────────┤
│              Kythr.Library                      │
│    ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐     │
│    │  26 Service   │  │ 24 Transport │  │  Data Repository  │     │
│    │   Monitors    │  │  Publishers  │  │ (InMem/LiteDb/Sql)│     │
│    └──────────────┘  └──────────────┘  └───────────────────┘     │
├────────────────────────────────────────────────────────────────────┤
│    Kythr.UI         (React 18 SPA)             │
│    Kythr.UI.Extensions (Middleware)             │
│    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│    │  Dashboard   │  │  SignalR Hub │  │  REST API    │          │
│    │  (React SPA) │  │  (Real-time) │  │  (Data API)  │          │
│    └──────────────┘  └──────────────┘  └──────────────┘          │
└────────────────────────────────────────────────────────────────────┘
```

## Data Flow

1. **Configuration Loading** — `MonitorOptions` are bound from `appsettings.json/yml` at startup
2. **Health Check Registration** — `StandardStackMonitoring` registers all configured health checks with the .NET health check framework
3. **Periodic Execution** — Each health check runs at its configured `MonitoringInterval`
4. **Result Processing** — Results are evaluated against `HealthCheckConditions` and stored in the data repository
5. **Alert Evaluation** — `AlertBehaviour` rules determine whether to fire an alert (fail count, time window, deduplication)
6. **Transport Publishing** — Matching transport publishers deliver alerts to configured channels
7. **Dashboard Update** — SignalR hub pushes results to connected React dashboard clients in real-time

## Key Components

### StandardStackMonitoring

The orchestrator that wires up all configured monitors. It reads `MonitorOptions.HealthChecks` and creates the appropriate `ServiceMonitoringBase` subclass for each entry.

### ServiceMonitoringBase

Abstract base for all monitors. Each implementation (e.g., `HttpServiceMonitoring`, `MsSqlServiceMonitoring`) overrides `SetUpMonitoring()` to register the appropriate health check.

### Publisher Pipeline

Publishers are registered as `IHealthCheckPublisher` implementations. When a health check completes, the .NET framework calls all registered publishers. Each publisher checks the `AlertBehaviour` configuration to decide whether to send an alert.

### MonitoringHub (SignalR)

A SignalR hub that broadcasts health check results to connected dashboard clients. The `CallbackPublisher` bridges the health check pipeline to the SignalR hub.

### React Dashboard

A single-page application served via `EmbeddedFileProvider` middleware. Built with:
- **React 18** — UI framework
- **TypeScript** — type safety
- **Tailwind CSS + shadcn/ui** — styling and components
- **Zustand** — client state management
- **TanStack Query** — server state and caching
- **TanStack Table** — data tables
- **React Router v6** — client-side routing

## NuGet Package Dependency Graph

```
Your Application
├── Kythr.Extensions
│   └── Kythr.Library
│       ├── Microsoft.Extensions.Diagnostics.HealthChecks
│       └── AspNetCore.Diagnostics.HealthChecks.*
└── Kythr.UI.Extensions
    └── Kythr.UI (embedded React SPA)
```
