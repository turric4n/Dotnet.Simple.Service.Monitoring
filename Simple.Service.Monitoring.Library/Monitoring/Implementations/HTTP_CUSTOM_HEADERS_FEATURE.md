# HTTP Service Monitoring - Custom Headers Feature

## Overview

The HTTP Service Monitoring now supports **custom HTTP headers** for health check requests. This allows you to:

- Add authentication headers (Bearer tokens, API keys)
- Set custom User-Agent strings  
- Include request tracking headers
- Add any custom headers required by your endpoints

---

## Configuration

### YAML Configuration

```yaml
HealthChecks:
  - Name: "API with Auth Header"
    ServiceType: Http
    EndpointOrHost: "https://api.example.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpTimeoutMs: 5000
        HttpVerb: Get
        CustomHttpHeaders:
          Authorization: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
          X-API-Key: "your-api-key-here"
          X-Request-ID: "health-check-001"
```

### C# Configuration

```csharp
var healthCheck = new ServiceHealthCheck
{
    Name = "API with Custom Headers",
    ServiceType = ServiceType.Http,
    EndpointOrHost = "https://api.example.com/health",
    HealthCheckConditions = new HealthCheckConditions
    {
        HttpBehaviour = new HttpBehaviour
        {
            HttpExpectedCode = 200,
            HttpTimeoutMs = 5000,
            HttpVerb = HttpVerb.Get,
            CustomHttpHeaders = new Dictionary<string, string>
            {
                { "Authorization", "Bearer your-token-here" },
                { "X-API-Key", "your-api-key" },
                { "X-Request-ID", "health-check-001" }
            }
        }
    }
};
```

---

## Common Use Cases

### 1. **Bearer Token Authentication**

```yaml
HealthChecks:
  - Name: "Secured API Endpoint"
    ServiceType: Http
    EndpointOrHost: "https://secure-api.example.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          Authorization: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Use Case**: Monitoring APIs that require OAuth 2.0 Bearer tokens

---

### 2. **API Key Authentication**

```yaml
HealthChecks:
  - Name: "Third-Party API"
    ServiceType: Http
    EndpointOrHost: "https://api.thirdparty.com/status"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          X-API-Key: "sk_live_abc123xyz789"
          X-API-Secret: "secret_key_here"
```

**Use Case**: Monitoring external services with API key authentication

---

### 3. **Custom User-Agent**

```yaml
HealthChecks:
  - Name: "Web Service"
    ServiceType: Http
    EndpointOrHost: "https://www.example.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          User-Agent: "MyCompany-HealthMonitor/1.0"
```

**Use Case**: Identifying health check traffic in server logs

---

### 4. **Request Tracking Headers**

```yaml
HealthChecks:
  - Name: "Microservice API"
    ServiceType: Http
    EndpointOrHost: "https://microservice.example.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          X-Request-ID: "health-check-service-001"
          X-Correlation-ID: "monitoring-${timestamp}"
          X-Source: "HealthCheckService"
```

**Use Case**: Distributed tracing and request correlation

---

### 5. **Content Negotiation**

```yaml
HealthChecks:
  - Name: "REST API"
    ServiceType: Http
    EndpointOrHost: "https://api.example.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          Accept: "application/json"
          Accept-Language: "en-US"
```

**Use Case**: Ensuring correct content type responses

---

### 6. **Multiple Endpoints with Same Headers**

```yaml
HealthChecks:
  - Name: "API Cluster"
    ServiceType: Http
    EndpointOrHost: "https://api1.example.com/health,https://api2.example.com/health,https://api3.example.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          Authorization: "Bearer shared-token"
          X-Environment: "Production"
```

**Use Case**: Load balancer health checks with shared authentication

---

## Security Best Practices

### ⚠️ **DO NOT Hardcode Secrets**

❌ **Bad** (Hardcoded in YAML):
```yaml
CustomHttpHeaders:
  Authorization: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

✅ **Good** (Environment Variables):
```yaml
CustomHttpHeaders:
  Authorization: "${API_BEARER_TOKEN}"
  X-API-Key: "${API_KEY}"
```

### Use Configuration Providers

```csharp
// In Startup.cs or Program.cs
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddAzureKeyVault(); // For Azure
builder.Configuration.AddUserSecrets(); // For development

// Headers will be resolved from configuration
var bearerToken = builder.Configuration["API_BEARER_TOKEN"];
```

### Rotate Tokens Regularly

- Use short-lived tokens when possible
- Implement token refresh mechanisms
- Store sensitive headers in secure vaults (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)

---

## Implementation Details

### How Headers Are Applied

The `HttpHealthCheck` applies custom headers to each request:

```csharp
using var request = new HttpRequestMessage(GetHttpMethod(_httpVerb), endpoint);
request.Headers.Add("User-Agent", "HealthChecks"); // Default

// Add custom headers
foreach (var header in _customHeaders)
{
    request.Headers.Add(header.Key, header.Value);
}

var response = await _httpClient.SendAsync(request, cts.Token);
```

### Header Validation

- Headers are validated by the underlying `HttpClient`
- Invalid header names/values will throw exceptions
- Common invalid characters: newlines, control characters

### Default Headers

The health check always includes:
```
User-Agent: HealthChecks
```

Custom headers can override this if needed.

---

## Examples

### Example 1: GitHub API Health Check

```yaml
HealthChecks:
  - Name: "GitHub API Status"
    ServiceType: Http
    EndpointOrHost: "https://api.github.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        HttpTimeoutMs: 10000
        CustomHttpHeaders:
          Accept: "application/vnd.github.v3+json"
          User-Agent: "MyApp-HealthMonitor/1.0"
```

### Example 2: Azure Function with Key

```yaml
HealthChecks:
  - Name: "Azure Function"
    ServiceType: Http
    EndpointOrHost: "https://myfunction.azurewebsites.net/api/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          x-functions-key: "${AZURE_FUNCTION_KEY}"
```

### Example 3: Kubernetes Ingress Health

```yaml
HealthChecks:
  - Name: "K8s Ingress"
    ServiceType: Http
    EndpointOrHost: "https://ingress.k8s.example.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Get
        CustomHttpHeaders:
          Host: "service.example.com"
          X-Forwarded-Proto: "https"
```

### Example 4: GraphQL Endpoint

```yaml
HealthChecks:
  - Name: "GraphQL API"
    ServiceType: Http
    EndpointOrHost: "https://graphql.example.com/health"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpVerb: Post
        CustomHttpHeaders:
          Content-Type: "application/json"
          Authorization: "Bearer ${GRAPHQL_TOKEN}"
```

---

## Troubleshooting

### Headers Not Being Sent

**Issue**: Custom headers don't appear in server logs

**Solution**:
1. Verify headers are in the `CustomHttpHeaders` dictionary
2. Check for typos in header names
3. Ensure configuration is being loaded correctly
4. Use a tool like Fiddler or Wireshark to inspect requests

### Authentication Fails

**Issue**: Getting 401 Unauthorized responses

**Solution**:
1. Verify token/key is valid and not expired
2. Check token format (e.g., "Bearer token" vs just "token")
3. Ensure token has necessary permissions
4. Test manually with curl/Postman first

### Invalid Header Error

**Issue**: `InvalidOperationException` when adding headers

**Solution**:
1. Check for invalid characters (newlines, control chars)
2. Some headers must be set on `HttpClient` not `HttpRequestMessage`
3. Content-Type and similar headers may need special handling

---

## Advanced Scenarios

### Dynamic Headers (Token Refresh)

For tokens that expire, consider implementing a custom health check:

```csharp
public class AuthenticatedHttpHealthCheck : IHealthCheck
{
    private readonly ITokenProvider _tokenProvider;
    
    public async Task<HealthCheckResult> CheckHealthAsync(...)
    {
        var token = await _tokenProvider.GetTokenAsync();
        
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Authorization", $"Bearer {token}");
        
        // ... rest of health check logic
    }
}
```

### Environment-Specific Headers

```yaml
# Development
CustomHttpHeaders:
  X-Environment: "Development"
  X-Debug: "true"

# Production
CustomHttpHeaders:
  X-Environment: "Production"
  X-Debug: "false"
```

---

## Testing

See `HttpServiceMonitoringShould.cs` for comprehensive unit tests including:

- `Should_Support_Custom_Headers_In_Http_Request`
- `Should_Apply_Multiple_Custom_Headers`
- `Should_Work_Without_Custom_Headers`
- `Should_Override_Default_UserAgent_With_Custom_Header`

---

## Configuration Reference

### HttpBehaviour Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `HttpExpectedCode` | `int` | Yes | - | Expected HTTP status code (e.g., 200) |
| `HttpTimeoutMs` | `int` | No | 30000 | Timeout in milliseconds |
| `HttpVerb` | `HttpVerb` | Yes | Get | HTTP method (Get, Post, Put, Delete) |
| `CustomHttpHeaders` | `Dictionary<string, string>` | No | `null` | Custom headers to include |

### CustomHttpHeaders Format

```csharp
Dictionary<string, string>
{
    { "Header-Name", "Header-Value" },
    { "Another-Header", "Another-Value" }
}
```

**Rules**:
- Header names are case-insensitive (HTTP standard)
- Values should not contain newlines or control characters
- Empty dictionary or `null` means no custom headers

---

## Related Documentation

- [HTTP Service Type Documentation](../wiki/Service-Types.md#httphttps-services)
- [Security Best Practices](../wiki/Security.md)
- [Configuration Guide](../wiki/Configuration.md)
- [Detailed Results Enhancement](./Publishers/TRANSPORT_PUBLISHERS_DETAILED_RESULTS.md)

---

## Changelog

### v2.0.0 (Current)
- ✅ Added `CustomHttpHeaders` support to `HttpBehaviour`
- ✅ Headers applied to all HTTP health check requests
- ✅ Supports multiple endpoints with shared headers
- ✅ Backward compatible (headers are optional)

### v1.x
- Basic HTTP health checks without custom headers

---

## Summary

The custom headers feature enables:

✅ **Authentication** - Bearer tokens, API keys, basic auth  
✅ **Request Tracking** - Correlation IDs, request IDs  
✅ **Content Negotiation** - Accept headers, content types  
✅ **Custom Identification** - User-Agent, application identifiers  
✅ **Environment Tagging** - Development, staging, production markers  

**All without modifying health check code!** 🎉
