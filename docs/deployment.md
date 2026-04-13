# Deployment Guide

## Docker

### Using the Included Dockerfile

The project ships with a `Dockerfile` in the `Kythr/` directory.

```bash
# Build the image
docker build -t kythr -f Kythr/Dockerfile .

# Run with a mounted config
docker run -d \
  -p 5000:80 \
  -v $(pwd)/appsettings.yml:/app/appsettings.yml:ro \
  --name monitoring \
  kythr
```

### Docker Compose (Local Development)

The repository includes a `docker-compose.yml` that starts all infrastructure services needed for integration testing:

```bash
docker-compose up -d
```

Services started:
| Service | Port | Purpose |
|---------|------|---------|
| SQL Server | 1433 | MsSql health checks |
| MySQL | 3306 | MySql health checks |
| PostgreSQL | 5432 | PostgreSql health checks |
| Redis | 6379 | Redis health checks |
| RabbitMQ | 5672, 15672 | Rmq health checks |
| Elasticsearch | 9200 | ElasticSearch health checks |
| Hangfire SQL | 1434 | Hangfire health checks |
| Nginx (HTTP) | 8080 | Http health checks |

### Production Docker Compose

```yaml
version: '3.8'

services:
  monitoring:
    build:
      context: .
      dockerfile: Kythr/Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./appsettings.Production.yml:/app/appsettings.yml:ro
      - monitoring-data:/app/data    # LiteDB persistence
    restart: unless-stopped

volumes:
  monitoring-data:
```

---

## Kubernetes

### Basic Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: service-monitoring
  labels:
    app: service-monitoring
spec:
  replicas: 1                        # Single instance recommended
  selector:
    matchLabels:
      app: service-monitoring
  template:
    metadata:
      labels:
        app: service-monitoring
    spec:
      containers:
        - name: monitoring
          image: your-registry/kythr:2.0.0
          ports:
            - containerPort: 80
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
          volumeMounts:
            - name: config
              mountPath: /app/appsettings.yml
              subPath: appsettings.yml
              readOnly: true
            - name: data
              mountPath: /app/data
          resources:
            requests:
              memory: "128Mi"
              cpu: "100m"
            limits:
              memory: "512Mi"
              cpu: "500m"
      volumes:
        - name: config
          configMap:
            name: monitoring-config
        - name: data
          persistentVolumeClaim:
            claimName: monitoring-data
---
apiVersion: v1
kind: Service
metadata:
  name: service-monitoring
spec:
  selector:
    app: service-monitoring
  ports:
    - port: 80
      targetPort: 80
  type: ClusterIP
```

### ConfigMap for Configuration

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: monitoring-config
data:
  appsettings.yml: |
    MonitoringUi:
      CompanyName: "Acme Corp"
      DataRepositoryType: "LiteDb"
    Monitoring:
      Settings:
        ShowUI: true
      HealthChecks:
        - Name: "Internal API"
          ServiceType: Http
          EndpointOrHost: "http://api-service.default.svc.cluster.local/health"
          Port: 80
```

### Secrets for Credentials

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: monitoring-secrets
type: Opaque
stringData:
  SMTP_PASSWORD: "your-smtp-password"
  SLACK_TOKEN: "xoxb-your-token"
  PAGERDUTY_KEY: "your-integration-key"
```

---

## Azure App Service

### Deploy via Azure CLI

```bash
# Create App Service
az webapp create \
  --resource-group myResourceGroup \
  --plan myAppServicePlan \
  --name my-service-monitoring \
  --runtime "DOTNET|10.0"

# Deploy code
az webapp deploy \
  --resource-group myResourceGroup \
  --name my-service-monitoring \
  --src-path ./publish.zip

# Configure settings
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name my-service-monitoring \
  --settings ASPNETCORE_ENVIRONMENT=Production
```

### Azure Key Vault for Secrets

Store transport credentials in Key Vault and reference them in configuration:

```json
{
  "EmailTransportSettings": [{
    "Password": "@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/smtp-password/)"
  }]
}
```

---

## IIS

1. Publish the application:
   ```bash
   dotnet publish Kythr/Kythr.csproj -c Release -o ./publish
   ```
2. Create an IIS site pointing to the `./publish` directory
3. Ensure the application pool uses `.NET CLR Version: No Managed Code` (runs as out-of-process)
4. Configure `appsettings.json` or `appsettings.yml` in the publish directory

---

## Environment Variables

All configuration values can be overridden via environment variables using the standard .NET configuration provider pattern:

```bash
# Override monitoring settings
export Monitoring__Settings__ShowUI=true
export Monitoring__Settings__UseGlobalServiceName="Production"

# Override transport credentials
export Monitoring__EmailTransportSettings__0__Password="secret"
export Monitoring__SlackTransportSettings__0__Token="xoxb-token"
```
