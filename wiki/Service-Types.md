# Service Types

Simple Service Monitoring supports 10+ different service types for comprehensive monitoring of your infrastructure.

## HTTP/HTTPS Services

Monitor web endpoints, APIs, and web applications.

### Configuration

```yaml
HealthChecks:
  - Name: "API Endpoint"
    ServiceType: Http
    EndpointOrHost: "https://api.example.com/health"
    Port: 443
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpTimeoutMs: 5000
        HttpVerb: Get
      ServiceReach: true
      ServiceConnectionEstablished: true
```

### Properties

| Property | Description | Example |
|----------|-------------|---------|
| `EndpointOrHost` | Full URL or hostname | `https://api.example.com/health` |
| `Port` | Port number | `443`, `80`, `8080` |
| `HttpExpectedCode` | Expected HTTP status code | `200`, `204`, `301` |
| `HttpTimeoutMs` | Request timeout in milliseconds | `5000` |
| `HttpVerb` | HTTP method | `Get`, `Post`, `Put`, `Delete` |

### Use Cases

- REST API health checks
- Website availability monitoring
- Microservice endpoint monitoring
- Load balancer health endpoints

---

## SQL Server (MsSql)

Monitor Microsoft SQL Server databases with optional custom queries.

### Basic Connection Check

```yaml
HealthChecks:
  - Name: "User Database"
    ServiceType: MsSql
    ConnectionString: "Server=localhost;Database=Users;Integrated Security=true;"
```

### Advanced Query Validation

```yaml
HealthChecks:
  - Name: "Order Processing Check"
    ServiceType: MsSql
    ConnectionString: "Server=localhost;Database=Orders;..."
    HealthCheckConditions:
      SqlBehaviour:
        Query: "SELECT COUNT(*) FROM Orders WHERE Status = 'Pending' AND CreatedDate < DATEADD(hour, -1, GETDATE())"
        ResultExpression: LessThan
        SqlResultDataType: Int
        ExpectedResult: 10
```

### Query Result Expressions

| Expression | Description | Example |
|------------|-------------|---------|
| `Equal` | Result must equal expected value | `COUNT(*) = 0` |
| `NotEqual` | Result must not equal expected value | `COUNT(*) != 0` |
| `GreaterThan` | Result must be greater than expected | `COUNT(*) > 100` |
| `LessThan` | Result must be less than expected | `COUNT(*) < 5` |

### Result Data Types

- `Int` - Integer values
- `String` - Text values
- `Bool` - Boolean values (true/false)
- `DateTime` - Date and time values

---

## MySQL

Monitor MySQL databases with the same features as SQL Server.

### Configuration

```yaml
HealthChecks:
  - Name: "MySQL Database"
    ServiceType: MySql
    ConnectionString: "Server=localhost;Database=mydb;Uid=root;Pwd=password;"
    HealthCheckConditions:
      SqlBehaviour:
        Query: "SELECT COUNT(*) FROM users WHERE active = 1"
        ResultExpression: GreaterThan
        SqlResultDataType: Int
        ExpectedResult: 0
```

---

## Redis

Monitor Redis cache servers for availability and performance.

### Configuration

```yaml
HealthChecks:
  - Name: "Session Cache"
    ServiceType: Redis
    ConnectionString: "localhost:6379,password=mypassword"
    HealthCheckConditions:
      RedisBehaviour:
        TimeOutMs: 3000
```

### Connection String Examples

```yaml
# Local Redis
ConnectionString: "localhost:6379"

# Redis with password
ConnectionString: "localhost:6379,password=secretpassword"

# Redis Sentinel
ConnectionString: "sentinel1:26379,sentinel2:26379,serviceName=mymaster"

# Redis Cluster
ConnectionString: "node1:6379,node2:6379,node3:6379"
```

---

## Elasticsearch

Monitor Elasticsearch clusters for health and availability.

### Configuration

```yaml
HealthChecks:
  - Name: "Search Cluster"
    ServiceType: ElasticSearch
    EndpointOrHost: "http://localhost:9200"
    HealthCheckConditions:
      ElasticBehaviour:
        TimeOutMs: 5000
```

---

## RabbitMQ (Rmq)

Monitor RabbitMQ message brokers.

### Configuration

```yaml
HealthChecks:
  - Name: "Message Queue"
    ServiceType: Rmq
    ConnectionString: "amqp://guest:guest@localhost:5672/"
```

### Connection String Format

```
amqp://username:password@hostname:port/virtualhost
```

---

## Hangfire

Monitor Hangfire background job processing.

### Configuration

```yaml
HealthChecks:
  - Name: "Background Jobs"
    ServiceType: Hangfire
    ConnectionString: "Server=localhost;Database=Hangfire;..."
    HealthCheckConditions:
      HangfireBehaviour:
        MaximumJobsFailed: 10
        MinimumAvailableServers: 1
```

### Properties

| Property | Description | Default |
|----------|-------------|---------|
| `MaximumJobsFailed` | Maximum allowed failed jobs | No limit |
| `MinimumAvailableServers` | Minimum required servers | 1 |

---

## Ping/ICMP

Monitor network connectivity using ICMP ping.

### Configuration

```yaml
HealthChecks:
  - Name: "Gateway Ping"
    ServiceType: Ping
    EndpointOrHost: "192.168.1.1"
    HealthCheckConditions:
      PingBehaviour:
        TimeOutMs: 2000
```

---

## Custom Health Checks

Implement custom business logic health checks.

### Configuration

```yaml
HealthChecks:
  - Name: "Custom Business Logic"
    ServiceType: Custom
    FullClassName: "MyApp.Health.CustomHealthCheck"
```

### Implementation

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MyApp.Health
{
    public class CustomHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Your custom logic here
                bool isHealthy = await CheckYourBusinessLogic();
                
                if (isHealthy)
                {
                    return HealthCheckResult.Healthy("Everything is OK");
                }
                else
                {
                    return HealthCheckResult.Degraded("Partial failure");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Failed", ex);
            }
        }
        
        private async Task<bool> CheckYourBusinessLogic()
        {
            // Implement your custom health check logic
            return true;
        }
    }
}
```

See [Custom Health Checks](Custom-Health-Checks.md) for detailed implementation guide.

---

## Request Interceptor

Monitor application requests and track failures.

### Configuration

```yaml
HealthChecks:
  - Name: "Application Requests"
    ServiceType: Interceptor
    HealthCheckConditions:
      InterceptorBehaviour:
        MaximumFailedRequests: 5
        TimeWindowMinutes: 5
    ExcludedInterceptionNames:
      - "health-check"
      - "metrics"
```

### How It Works

1. Intercepts HTTP requests in your application
2. Tracks failed requests (4xx, 5xx status codes)
3. Triggers unhealthy status if failures exceed threshold within time window

### Use Cases

- Monitor application error rates
- Detect sudden spikes in failures
- Track specific endpoint failures

---

## Monitoring Intervals

All service types support configurable monitoring intervals:

```yaml
MonitoringInterval: "00:01:00"  # 1 minute
MonitoringInterval: "00:00:30"  # 30 seconds
MonitoringInterval: "00:05:00"  # 5 minutes
```

### Recommended Intervals

| Service Type | Recommended Interval | Reason |
|--------------|---------------------|---------|
| HTTP/HTTPS | 30s - 1m | Balance between responsiveness and load |
| Database | 1m - 2m | Reduce database connection overhead |
| Redis | 30s - 1m | Cache should respond quickly |
| Ping | 10s - 30s | Network connectivity |
| Custom | Varies | Based on operation cost |

---

## Common Patterns

### Multi-Environment Monitoring

```yaml
# Production - High frequency
HealthChecks:
  - Name: "Production API"
    ServiceType: Http
    EndpointOrHost: "https://api.prod.company.com"
    MonitoringInterval: "00:00:30"
    
# Staging - Normal frequency  
  - Name: "Staging API"
    ServiceType: Http
    EndpointOrHost: "https://api.staging.company.com"
    MonitoringInterval: "00:02:00"
```

### Database Read/Write Validation

```yaml
# Read check
- Name: "Database Read"
  ServiceType: MsSql
  ConnectionString: "..."
  HealthCheckConditions:
    SqlBehaviour:
      Query: "SELECT 1"

# Write check with temp table
- Name: "Database Write"
  ServiceType: MsSql
  ConnectionString: "..."
  HealthCheckConditions:
    SqlBehaviour:
      Query: "CREATE TABLE #temp (id INT); DROP TABLE #temp; SELECT 1"
```

### Cascading Dependencies

```yaml
# Database must be healthy for API to work
- Name: "Database"
  ServiceType: MsSql
  ConnectionString: "..."
  
- Name: "API (depends on Database)"
  ServiceType: Http
  EndpointOrHost: "https://api.company.com/health"
```

---

## Best Practices

### 1. Use Appropriate Timeouts

```yaml
# Too aggressive - may cause false positives
HttpTimeoutMs: 1000  # ❌

# Reasonable timeout
HttpTimeoutMs: 5000  # ✅
```

### 2. Avoid Expensive Operations

```yaml
# Expensive query - avoid
Query: "SELECT * FROM LargeTable"  # ❌

# Lightweight query
Query: "SELECT COUNT(*) FROM Orders WHERE Status = 'Active'"  # ✅
```

### 3. Tag Services Appropriately

```yaml
AdditionalTags:
  - "production"
  - "critical"
  - "user-facing"
```

### 4. Use Custom Queries for Business Logic

```yaml
# Check for stale data
Query: "SELECT COUNT(*) FROM DataFeed WHERE LastUpdate < DATEADD(hour, -2, GETDATE())"
ResultExpression: Equal
ExpectedResult: 0
```

---

## Next Steps

- [Configuration Guide](Configuration-Guide.md) - Complete configuration reference
- [Alert Configuration](Alert-Configuration.md) - Set up intelligent alerting
- [Custom Health Checks](Custom-Health-Checks.md) - Implement custom monitors
- [Example Configurations](Example-Configurations.md) - Ready-to-use examples
