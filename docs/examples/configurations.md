# Example Configurations

Ready-to-use configuration examples for common monitoring scenarios.

## Minimal Setup — Single HTTP Monitor

```yaml
MonitoringUi:
  CompanyName: "My Company"
  DataRepositoryType: "InMemory"

Monitoring:
  Settings:
    ShowUI: true
  HealthChecks:
    - Name: "Main Website"
      ServiceType: Http
      EndpointOrHost: "https://www.mycompany.com"
      Port: 443
      MonitoringInterval: "00:01:00"
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
          HttpTimeoutMs: 10000
          HttpVerb: Get
```

---

## Full-Stack Web Application

Monitor the API, database, cache, and message queue of a typical web application.

```yaml
MonitoringUi:
  CompanyName: "Acme Corp"
  HeaderDescription: "Production Monitoring"
  DataRepositoryType: "LiteDb"

Monitoring:
  Settings:
    ShowUI: true
    UseGlobalServiceName: "Production"

  HealthChecks:
    # --- Web & API ---
    - Name: "Public API"
      ServiceType: Http
      EndpointOrHost: "https://api.acme.com/health"
      Port: 443
      MonitoringInterval: "00:00:30"
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
          HttpTimeoutMs: 5000
          HttpVerb: Get
          HttpCustomHeaders:
            X-Monitor: "true"
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "CriticalSlack"
          AlertByFailCount: 2
          AlertEvery: "00:02:00"
          AlertOnServiceRecovered: true
        - TransportMethod: PagerDuty
          TransportName: "OnCallPD"
          AlertByFailCount: 5
          AlertEvery: "00:01:00"
      AdditionalTags: ["critical", "api"]

    - Name: "Admin Portal"
      ServiceType: Http
      EndpointOrHost: "https://admin.acme.com/health"
      Port: 443
      MonitoringInterval: "00:02:00"
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
          HttpTimeoutMs: 10000
          HttpVerb: Get
      Alert: true
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "OpsEmail"
          AlertEvery: "00:15:00"
      AdditionalTags: ["internal", "admin"]

    # --- Databases ---
    - Name: "Primary SQL Server"
      ServiceType: MsSql
      ConnectionString: "Server=sql-primary.acme.com;Database=AppDb;User Id=monitor;Password=***;"
      MonitoringInterval: "00:01:00"
      HealthCheckConditions:
        SqlBehaviour:
          Query: "SELECT 1"
          ResultExpression: Equal
          SqlResultDataType: Int
          ExpectedResult: 1
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "CriticalSlack"
          AlertByFailCount: 1
      AdditionalTags: ["critical", "database"]

    - Name: "Order Processing Health"
      ServiceType: MsSql
      ConnectionString: "Server=sql-primary.acme.com;Database=AppDb;User Id=monitor;Password=***;"
      MonitoringInterval: "00:05:00"
      HealthCheckConditions:
        SqlBehaviour:
          Query: |
            SELECT COUNT(*) FROM Orders
            WHERE Status = 'Processing'
            AND CreatedDate < DATEADD(hour, -2, GETDATE())
          ResultExpression: LessThan
          SqlResultDataType: Int
          ExpectedResult: 50
      Alert: true
      AlertBehaviour:
        - TransportMethod: Teams
          TransportName: "BusinessTeams"
          AlertEvery: "00:30:00"
      AdditionalTags: ["business-logic", "orders"]

    # --- Cache ---
    - Name: "Redis Cache"
      ServiceType: Redis
      ConnectionString: "redis.acme.com:6379,password=***"
      MonitoringInterval: "00:01:00"
      HealthCheckConditions:
        RedisBehaviour:
          TimeOutMs: 3000
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "CriticalSlack"
          AlertByFailCount: 3
      AdditionalTags: ["critical", "cache"]

    # --- Message Broker ---
    - Name: "RabbitMQ"
      ServiceType: Rmq
      ConnectionString: "amqp://user:***@rabbitmq.acme.com:5672/"
      MonitoringInterval: "00:01:00"
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "OpsSlack"
      AdditionalTags: ["messaging"]

    # --- Infrastructure ---
    - Name: "SSL Certificate"
      ServiceType: SslCertificate
      EndpointOrHost: "api.acme.com"
      Port: 443
      MonitoringInterval: "06:00:00"
      Alert: true
      AlertBehaviour:
        - TransportMethod: Email
          TransportName: "SecurityEmail"
          AlertEvery: "24:00:00"
      AdditionalTags: ["security", "ssl"]

  # --- Transport Settings ---
  SlackTransportSettings:
    - Name: "CriticalSlack"
      Token: "xoxb-your-token"
      Channel: "#critical-alerts"
    - Name: "OpsSlack"
      Token: "xoxb-your-token"
      Channel: "#ops-alerts"

  EmailTransportSettings:
    - Name: "OpsEmail"
      From: "monitoring@acme.com"
      To: "ops@acme.com"
      SmtpHost: "smtp.acme.com"
      SmtpPort: 587
      Authentication: true
      Username: "monitoring@acme.com"
      Password: "***"
    - Name: "SecurityEmail"
      From: "monitoring@acme.com"
      To: "security@acme.com"
      SmtpHost: "smtp.acme.com"
      SmtpPort: 587
      Authentication: true
      Username: "monitoring@acme.com"
      Password: "***"

  PagerDutyTransportSettings:
    - Name: "OnCallPD"
      IntegrationKey: "your-pagerduty-key"

  TeamsTransportSettings:
    - Name: "BusinessTeams"
      WebhookUrl: "https://outlook.office.com/webhook/..."
```

---

## Microservices with Kafka & gRPC

```yaml
MonitoringUi:
  CompanyName: "MicroCorp"
  DataRepositoryType: "LiteDb"

Monitoring:
  Settings:
    ShowUI: true
    UseGlobalServiceName: "Microservices Cluster"

  HealthChecks:
    - Name: "User Service (gRPC)"
      ServiceType: Grpc
      EndpointOrHost: "https://user-svc.internal:50051"
      MonitoringInterval: "00:00:30"
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "MicroSlack"

    - Name: "Payment Service (HTTP)"
      ServiceType: Http
      EndpointOrHost: "https://payment-svc.internal/health"
      Port: 443
      MonitoringInterval: "00:00:30"
      HealthCheckConditions:
        HttpBehaviour:
          HttpExpectedCode: 200
          HttpTimeoutMs: 3000
          HttpVerb: Get
      Alert: true
      AlertBehaviour:
        - TransportMethod: PagerDuty
          TransportName: "PaymentPD"
          AlertByFailCount: 2

    - Name: "Kafka Broker"
      ServiceType: Kafka
      EndpointOrHost: "kafka-broker-1.internal:9092"
      MonitoringInterval: "00:01:00"
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "MicroSlack"

    - Name: "MongoDB (Orders)"
      ServiceType: MongoDb
      ConnectionString: "mongodb://mongo.internal:27017/orders"
      MonitoringInterval: "00:01:00"
      Alert: true
      AlertBehaviour:
        - TransportMethod: Slack
          TransportName: "MicroSlack"

  SlackTransportSettings:
    - Name: "MicroSlack"
      Token: "xoxb-your-token"
      Channel: "#microservices-alerts"

  PagerDutyTransportSettings:
    - Name: "PaymentPD"
      IntegrationKey: "your-key"
```

---

## Cloud Infrastructure (Azure + AWS)

```yaml
Monitoring:
  Settings:
    ShowUI: true
    UseGlobalServiceName: "Cloud Infrastructure"

  HealthChecks:
    - Name: "Azure Service Bus"
      ServiceType: AzureServiceBus
      ConnectionString: "Endpoint=sb://mybus.servicebus.windows.net/;SharedAccessKeyName=monitor;SharedAccessKey=***"
      MonitoringInterval: "00:02:00"
      Alert: true
      AlertBehaviour:
        - TransportMethod: AppInsights
          TransportName: "AzureAI"
          PublishAllResults: true

    - Name: "Azure CosmosDB"
      ServiceType: CosmosDb
      ConnectionString: "AccountEndpoint=https://mydb.documents.azure.com:443/;AccountKey=***"
      MonitoringInterval: "00:02:00"
      Alert: true
      AlertBehaviour:
        - TransportMethod: AppInsights
          TransportName: "AzureAI"

    - Name: "AWS SQS Queue"
      ServiceType: AwsSqs
      ConnectionString: "https://sqs.us-east-1.amazonaws.com/123456789/orders"
      MonitoringInterval: "00:02:00"
      Alert: true
      AlertBehaviour:
        - TransportMethod: CloudWatch
          TransportName: "AWSCW"
          PublishAllResults: true

  AppInsightsTransportSettings:
    - Name: "AzureAI"
      InstrumentationKey: "your-instrumentation-key"

  CloudWatchTransportSettings:
    - Name: "AWSCW"
      Region: "us-east-1"
      Namespace: "HealthChecks"
```

---

## Development Environment (No Alerts)

```yaml
MonitoringUi:
  CompanyName: "Dev Environment"
  DataRepositoryType: "InMemory"

Monitoring:
  Settings:
    ShowUI: true
    UseGlobalServiceName: "Development"

  HealthChecks:
    - Name: "Local API"
      ServiceType: Http
      EndpointOrHost: "https://localhost:5001/health"
      Port: 5001
      MonitoringInterval: "00:05:00"
      Alert: false

    - Name: "Local Redis"
      ServiceType: Redis
      ConnectionString: "localhost:6379"
      MonitoringInterval: "00:05:00"
      Alert: false

    - Name: "Local SQL"
      ServiceType: MsSql
      ConnectionString: "Server=(localdb)\\mssqllocaldb;Database=DevDb;Trusted_Connection=true;"
      MonitoringInterval: "00:05:00"
      Alert: false
```
