# Service Monitors Guide

This guide covers all 26 service monitor types with configuration examples.

## Web & API Monitors

### HTTP/HTTPS (`Http`)

Monitor any HTTP/HTTPS endpoint with custom headers, expected status codes, and timeouts.

```yaml
- Name: "API Gateway"
  ServiceType: Http
  EndpointOrHost: "https://api.company.com/health"
  Port: 443
  MonitoringInterval: "00:01:00"
  HealthCheckConditions:
    HttpBehaviour:
      HttpExpectedCode: 200
      HttpTimeoutMs: 5000
      HttpVerb: Get                # Get | Post | Put | Delete
      HttpCustomHeaders:           # v2.0 feature
        Authorization: "Bearer eyJhbGc..."
        X-Api-Key: "key-123"
        X-Request-Id: "monitoring"
    ServiceReach: true
    ServiceConnectionEstablished: true
```

### gRPC (`Grpc`)

Check gRPC service health using the standard gRPC health check protocol.

```yaml
- Name: "gRPC Order Service"
  ServiceType: Grpc
  EndpointOrHost: "https://grpc.company.com"
  Port: 50051
```

### TCP (`Tcp`)

Verify raw TCP connectivity to any host and port.

```yaml
- Name: "Custom TCP Service"
  ServiceType: Tcp
  EndpointOrHost: "tcp-service.company.com"
  Port: 9090
```

### Ping / ICMP (`Ping`)

Network reachability check using ICMP ping.

```yaml
- Name: "Gateway Ping"
  ServiceType: Ping
  EndpointOrHost: "192.168.1.1"
```

### DNS (`Dns`)

Verify DNS resolution for a given hostname.

```yaml
- Name: "DNS Resolution"
  ServiceType: Dns
  EndpointOrHost: "api.company.com"
```

### FTP (`Ftp`)

Check FTP server availability.

```yaml
- Name: "File Server"
  ServiceType: Ftp
  EndpointOrHost: "ftp.company.com"
  Port: 21
```

### SMTP (`Smtp`)

Verify SMTP/mail server availability.

```yaml
- Name: "Mail Server"
  ServiceType: Smtp
  EndpointOrHost: "smtp.company.com"
  Port: 587
```

---

## SQL Database Monitors

All SQL monitors support custom query validation with `SqlBehaviour`:

| Property | Description |
|----------|-------------|
| `Query` | SQL query to execute |
| `ResultExpression` | `Equal`, `NotEqual`, `GreaterThan`, `LessThan` |
| `SqlResultDataType` | `String`, `Int`, `Bool`, `DateTime` |
| `ExpectedResult` | The value to compare against |

### SQL Server (`MsSql`)

```yaml
- Name: "User Database"
  ServiceType: MsSql
  ConnectionString: "Server=db.company.com;Database=Users;Integrated Security=true;"
  HealthCheckConditions:
    SqlBehaviour:
      Query: "SELECT COUNT(*) FROM Users WHERE Active = 1"
      ResultExpression: GreaterThan
      SqlResultDataType: Int
      ExpectedResult: 0
```

### MySQL (`MySql`)

```yaml
- Name: "Orders MySQL"
  ServiceType: MySql
  ConnectionString: "Server=mysql.company.com;Database=orders;Uid=monitor;Pwd=secret;"
  HealthCheckConditions:
    SqlBehaviour:
      Query: "SELECT COUNT(*) FROM orders WHERE status = 'pending'"
      ResultExpression: LessThan
      SqlResultDataType: Int
      ExpectedResult: 1000
```

### PostgreSQL (`PostgreSql`)

```yaml
- Name: "Analytics PostgreSQL"
  ServiceType: PostgreSql
  ConnectionString: "Host=pg.company.com;Port=5432;Database=analytics;Username=monitor;Password=secret;"
  HealthCheckConditions:
    SqlBehaviour:
      Query: "SELECT COUNT(*) FROM active_sessions"
      ResultExpression: GreaterThan
      SqlResultDataType: Int
      ExpectedResult: 0
```

### Oracle (`Oracle`)

```yaml
- Name: "Legacy Oracle DB"
  ServiceType: Oracle
  ConnectionString: "Data Source=oracle.company.com:1521/ORCL;User Id=monitor;Password=secret;"
```

### SQLite (`Sqlite`)

```yaml
- Name: "Local SQLite"
  ServiceType: Sqlite
  ConnectionString: "Data Source=app.db;"
```

---

## NoSQL & Cache Monitors

### Redis (`Redis`)

```yaml
- Name: "Session Cache"
  ServiceType: Redis
  ConnectionString: "redis.company.com:6379,password=secret"
  HealthCheckConditions:
    RedisBehaviour:
      TimeOutMs: 3000
```

### Elasticsearch (`ElasticSearch`)

```yaml
- Name: "Search Cluster"
  ServiceType: ElasticSearch
  EndpointOrHost: "https://es.company.com"
  Port: 9200
```

### MongoDB (`MongoDb`)

```yaml
- Name: "Document Store"
  ServiceType: MongoDb
  ConnectionString: "mongodb://mongo.company.com:27017/mydb"
```

### CosmosDB (`CosmosDb`)

```yaml
- Name: "Azure CosmosDB"
  ServiceType: CosmosDb
  ConnectionString: "AccountEndpoint=https://myaccount.documents.azure.com:443/;AccountKey=..."
```

### Memcached (`Memcached`)

```yaml
- Name: "Memcached Cluster"
  ServiceType: Memcached
  EndpointOrHost: "memcached.company.com"
  Port: 11211
```

---

## Message Broker Monitors

### RabbitMQ (`Rmq`)

```yaml
- Name: "Message Broker"
  ServiceType: Rmq
  ConnectionString: "amqp://user:password@rabbitmq.company.com:5672/"
```

### Kafka (`Kafka`)

```yaml
- Name: "Event Stream"
  ServiceType: Kafka
  EndpointOrHost: "kafka-broker.company.com:9092"
```

### Azure Service Bus (`AzureServiceBus`)

```yaml
- Name: "Azure Service Bus"
  ServiceType: AzureServiceBus
  ConnectionString: "Endpoint=sb://mybus.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=..."
```

### AWS SQS (`AwsSqs`)

```yaml
- Name: "AWS SQS Queue"
  ServiceType: AwsSqs
  ConnectionString: "https://sqs.us-east-1.amazonaws.com/123456789/my-queue"
```

---

## Infrastructure Monitors

### Docker (`Docker`)

```yaml
- Name: "Docker Daemon"
  ServiceType: Docker
  EndpointOrHost: "unix:///var/run/docker.sock"
```

### SSL Certificate (`SslCertificate`)

Monitor certificate expiry for any HTTPS endpoint.

```yaml
- Name: "API SSL Cert"
  ServiceType: SslCertificate
  EndpointOrHost: "api.company.com"
  Port: 443
```

### Hangfire (`Hangfire`)

Monitor Hangfire background job processing health.

```yaml
- Name: "Background Jobs"
  ServiceType: Hangfire
  ConnectionString: "Server=db.company.com;Database=Hangfire;..."
  HealthCheckConditions:
    HangfireBehaviour:
      MaximumJobsFailed: 10
      MinimumAvailableServers: 1
```

---

## Custom Monitors

### Custom Health Check (`Custom`)

Use any class implementing `IHealthCheck`:

```yaml
- Name: "Business Logic Check"
  ServiceType: Custom
  FullClassName: "MyApp.Health.OrderProcessingHealthCheck, MyApp"
```

### Request Interceptor (`Interceptor`)

Monitor application request patterns:

```yaml
- Name: "Request Monitor"
  ServiceType: Interceptor
  ExcludedInterceptionNames:
    - "health"
    - "metrics"
```
