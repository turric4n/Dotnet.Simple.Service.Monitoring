# Dashboard Guide

## Accessing the Dashboard

After configuring and starting your application, navigate to:

```
https://your-app/monitoring
```

## Features

### Real-Time Status

The dashboard connects to the server via SignalR WebSocket and receives health check results in real-time. No manual refresh needed — status cards update automatically as checks complete.

Status indicators:
- **Healthy** (green) — service is operating normally
- **Degraded** (yellow) — service is responding but with issues
- **Unhealthy** (red) — service is down or failing checks
- **Unknown** (gray) — no data yet or service is inactive

### Timeline Visualization

The timeline view shows historical health check data over configurable time ranges. Each service has a horizontal bar showing health status over time.

**Controls:**
- **Time Range** — select how far back to display (1h, 6h, 24h, 7d, etc.)
- **Grouping Mode** — toggle between individual checks and service name grouping
- **Active Only** — filter to show only services with recent activity
- **Active Threshold** — configure what "recent" means (e.g., last 30 minutes)

### Service Name Grouping

When multiple instances of the same service run across different machines, the grouping feature combines their results under a single service name. This is useful for:
- Load-balanced APIs where each instance reports independently
- Microservices running on multiple Kubernetes pods
- Multi-region deployments

### Dark Mode

The dashboard supports dark mode with:
- **System detection** — automatically matches your OS theme preference
- **Manual toggle** — switch between light and dark at any time
- **Persistence** — your preference is saved in the browser

### Responsive Design

The dashboard adapts to all screen sizes:
- **Desktop** — full layout with sidebar, timeline, and detail panels
- **Tablet** — condensed layout with collapsible panels
- **Mobile** — stacked layout optimized for touch

## Configuration

### Company Branding

```yaml
MonitoringUi:
  CompanyName: "Your Company"
  HeaderDescription: "Service Health Dashboard"
  HeaderLogoUrl: "https://your-cdn.com/logo.png"
```

### Data Repository

The dashboard needs a data repository to store historical health check results:

```yaml
MonitoringUi:
  DataRepositoryType: "LiteDb"  # InMemory | LiteDb | Sql
```

| Option | Persistence | Notes |
|--------|------------|-------|
| `InMemory` | Lost on restart | Best for development |
| `LiteDb` | File-based | Default; no external dependencies |
| `Sql` | SQL Server | Requires `SqlConnectionString`; best for multi-instance |

### Restricting Access

The dashboard endpoint does not include built-in authentication. Protect it using your application's auth middleware:

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapServiceMonitoringUi()
        .RequireAuthorization("AdminPolicy");
});
```

Or restrict by network/reverse proxy in production environments.

## Technology Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | React 18 |
| Language | TypeScript (strict mode) |
| Styling | Tailwind CSS v3 + shadcn/ui |
| State Management | Zustand |
| Server State | TanStack Query v5 |
| Data Tables | TanStack Table v8 |
| Routing | React Router v6 |
| Build Tool | Webpack 5 |
| Real-time | SignalR (WebSocket) |
