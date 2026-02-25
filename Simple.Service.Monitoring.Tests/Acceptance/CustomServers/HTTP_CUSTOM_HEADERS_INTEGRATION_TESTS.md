# HTTP Service Monitoring - Custom Headers Integration Tests

## Overview

Integration tests for HTTP health checks with custom headers feature. These tests verify that custom headers are properly sent with HTTP requests and work with various authentication schemes.

---

## Test Setup

### Prerequisites

1. **Test HTTP Server** - Uses Kestrel test server
2. **Custom Middleware** - Captures and validates headers
3. **Real HTTP Requests** - Not mocked, actual HTTP calls

---

## Test Scenarios

### 1. **Basic Custom Header**

Verifies a single custom header is sent correctly.

```csharp
[Test]
public async Task Should_Send_Custom_Header_With_Http_Request()
{
    // Arrange
    var customHeaders = new Dictionary<string, string>
    {
        { "X-API-Key", "test-api-key-123" }
    };
    
    // Start test server that validates headers
    using var testServer = CreateTestServer(request =>
    {
        request.Headers["X-API-Key"].Should().Be("test-api-key-123");
        return 200;
    });
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        customHeaders);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(), 
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
}
```

### 2. **Bearer Token Authentication**

Tests OAuth 2.0 Bearer token in Authorization header.

```csharp
[Test]
public async Task Should_Send_Bearer_Token_In_Authorization_Header()
{
    // Arrange
    var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";
    var customHeaders = new Dictionary<string, string>
    {
        { "Authorization", $"Bearer {token}" }
    };
    
    using var testServer = CreateTestServer(request =>
    {
        var authHeader = request.Headers["Authorization"].ToString();
        authHeader.Should().StartWith("Bearer ");
        authHeader.Should().Contain(token);
        return 200;
    });
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        customHeaders);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
}
```

### 3. **Multiple Custom Headers**

Validates multiple headers are all sent together.

```csharp
[Test]
public async Task Should_Send_Multiple_Custom_Headers()
{
    // Arrange
    var customHeaders = new Dictionary<string, string>
    {
        { "X-API-Key", "api-key-123" },
        { "X-Request-ID", "req-456" },
        { "X-Correlation-ID", "corr-789" },
        { "X-Environment", "Testing" }
    };
    
    using var testServer = CreateTestServer(request =>
    {
        request.Headers["X-API-Key"].Should().Be("api-key-123");
        request.Headers["X-Request-ID"].Should().Be("req-456");
        request.Headers["X-Correlation-ID"].Should().Be("corr-789");
        request.Headers["X-Environment"].Should().Be("Testing");
        return 200;
    });
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        customHeaders);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
}
```

### 4. **Custom User-Agent Override**

Tests that default User-Agent can be overridden.

```csharp
[Test]
public async Task Should_Override_Default_UserAgent_With_Custom_Header()
{
    // Arrange
    var customUserAgent = "MyCompany-HealthMonitor/2.0";
    var customHeaders = new Dictionary<string, string>
    {
        { "User-Agent", customUserAgent }
    };
    
    using var testServer = CreateTestServer(request =>
    {
        // Note: User-Agent might be in different header collection
        var userAgent = request.Headers["User-Agent"].ToString();
        userAgent.Should().Contain(customUserAgent);
        return 200;
    });
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        customHeaders);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
}
```

### 5. **Headers with Multiple Endpoints**

Verifies same headers are sent to all endpoints.

```csharp
[Test]
public async Task Should_Send_Custom_Headers_To_All_Endpoints()
{
    // Arrange
    var customHeaders = new Dictionary<string, string>
    {
        { "X-API-Key", "shared-key" }
    };
    
    var headersReceived = new List<string>();
    
    using var testServer1 = CreateTestServer(request =>
    {
        headersReceived.Add(request.Headers["X-API-Key"].ToString());
        return 200;
    });
    
    using var testServer2 = CreateTestServer(request =>
    {
        headersReceived.Add(request.Headers["X-API-Key"].ToString());
        return 200;
    });
    
    var endpoints = new List<Uri>
    {
        testServer1.BaseAddress,
        testServer2.BaseAddress
    };
    
    var healthCheck = new HttpHealthCheck(
        endpoints,
        TimeSpan.FromSeconds(5),
        200,
        HttpVerb.Get,
        customHeaders);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
    headersReceived.Should().HaveCount(2);
    headersReceived.Should().AllBe("shared-key");
}
```

### 6. **No Custom Headers (Backward Compatibility)**

Ensures health check works without custom headers.

```csharp
[Test]
public async Task Should_Work_Without_Custom_Headers()
{
    // Arrange
    using var testServer = CreateTestServer(request =>
    {
        // Just verify default User-Agent is present
        request.Headers["User-Agent"].Should().Contain("HealthChecks");
        return 200;
    });
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        null); // No custom headers
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
}
```

### 7. **Empty Headers Dictionary**

Tests empty dictionary doesn't cause issues.

```csharp
[Test]
public async Task Should_Work_With_Empty_Custom_Headers_Dictionary()
{
    // Arrange
    var customHeaders = new Dictionary<string, string>();
    
    using var testServer = CreateTestServer(request => 200);
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        customHeaders);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
}
```

### 8. **Headers with Failed Health Check**

Verifies headers are sent even when health check fails.

```csharp
[Test]
public async Task Should_Send_Custom_Headers_Even_When_Endpoint_Returns_Error()
{
    // Arrange
    var customHeaders = new Dictionary<string, string>
    {
        { "X-API-Key", "test-key" }
    };
    
    bool headerWasPresent = false;
    
    using var testServer = CreateTestServer(request =>
    {
        headerWasPresent = request.Headers["X-API-Key"] == "test-key";
        return 500; // Return error
    });
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        customHeaders);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Unhealthy);
    headerWasPresent.Should().BeTrue(
        "custom header should be sent even when endpoint returns error");
}
```

---

## Test Server Implementation

### Helper Methods

```csharp
private TestServer CreateTestServer(Func<HttpRequest, int> responseHandler)
{
    var webHostBuilder = new WebHostBuilder()
        .Configure(app =>
        {
            app.Run(async context =>
            {
                var statusCode = responseHandler(context.Request);
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync("OK");
            });
        });
    
    return new TestServer(webHostBuilder);
}

private HttpHealthCheck CreateHttpHealthCheck(
    string endpoint,
    Dictionary<string, string> customHeaders)
{
    return new HttpHealthCheck(
        new List<Uri> { new Uri(endpoint) },
        TimeSpan.FromSeconds(5),
        200,
        HttpVerb.Get,
        customHeaders ?? new Dictionary<string, string>());
}
```

---

## Running the Tests

### Run All Integration Tests

```bash
dotnet test --filter "Category=Integration&FullyQualifiedName~HttpCustomHeaders"
```

### Run Specific Test

```bash
dotnet test --filter "Should_Send_Custom_Header_With_Http_Request"
```

### Run with Verbose Output

```bash
dotnet test --filter "Category=HttpCustomHeaders" --logger "console;verbosity=detailed"
```

---

## Test Coverage

| Scenario | Coverage |
|----------|----------|
| Single Header | ✅ Covered |
| Multiple Headers | ✅ Covered |
| Bearer Token | ✅ Covered |
| API Key | ✅ Covered |
| User-Agent Override | ✅ Covered |
| Multiple Endpoints | ✅ Covered |
| Null Headers | ✅ Covered |
| Empty Dictionary | ✅ Covered |
| Failed Requests | ✅ Covered |
| Backward Compatibility | ✅ Covered |

---

## Security Testing

### Test with Sensitive Headers

```csharp
[Test]
public async Task Should_Handle_Sensitive_Headers_Securely()
{
    // Arrange
    var sensitiveToken = "super-secret-token-12345";
    var customHeaders = new Dictionary<string, string>
    {
        { "Authorization", $"Bearer {sensitiveToken}" }
    };
    
    using var testServer = CreateTestServer(request =>
    {
        // Validate token is received correctly
        request.Headers["Authorization"].Should().Contain(sensitiveToken);
        return 200;
    });
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        customHeaders);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
    
    // Verify sensitive data is not in result description
    result.Description.Should().NotContain(sensitiveToken,
        "sensitive data should not be exposed in health check results");
}
```

---

## Performance Testing

### Concurrent Requests with Headers

```csharp
[Test]
public async Task Should_Handle_Concurrent_Requests_With_Custom_Headers()
{
    // Arrange
    var customHeaders = new Dictionary<string, string>
    {
        { "X-API-Key", "concurrent-test-key" }
    };
    
    var requestCount = 0;
    using var testServer = CreateTestServer(request =>
    {
        Interlocked.Increment(ref requestCount);
        request.Headers["X-API-Key"].Should().Be("concurrent-test-key");
        return 200;
    });
    
    var healthCheck = CreateHttpHealthCheck(
        testServer.BaseAddress.ToString(),
        customHeaders);
    
    // Act - Execute 100 concurrent health checks
    var tasks = Enumerable.Range(0, 100)
        .Select(_ => healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            CancellationToken.None))
        .ToList();
    
    var results = await Task.WhenAll(tasks);
    
    // Assert
    results.Should().HaveCount(100);
    results.Should().AllSatisfy(r => r.Status.Should().Be(HealthStatus.Healthy));
    requestCount.Should().Be(100);
}
```

---

## Troubleshooting Tests

### Test Fails with "Connection Refused"

**Issue**: Test server not starting correctly

**Solution**:
```csharp
// Ensure TestServer is properly created
using var testServer = new TestServer(webHostBuilder);
var baseAddress = testServer.BaseAddress; // Verify address is set
```

### Headers Not Being Captured

**Issue**: Test middleware not seeing headers

**Solution**:
```csharp
// Check header name casing
request.Headers["X-Api-Key"] vs request.Headers["x-api-key"]
// HTTP headers are case-insensitive
```

### Test Server Timeout

**Issue**: Health check times out

**Solution**:
```csharp
// Increase timeout in test
var timeout = TimeSpan.FromSeconds(30); // Longer timeout for debugging
```

---

## Future Test Enhancements

### Potential Additions

1. **Header Value Validation**
   - Test with special characters
   - Test with very long values
   - Test with unicode characters

2. **Security Tests**
   - Verify headers over HTTPS
   - Test header injection prevention
   - Validate sensitive data masking

3. **Error Scenarios**
   - Invalid header names
   - Malformed header values
   - Headers exceeding size limits

4. **Performance Tests**
   - Measure overhead of custom headers
   - Test with very large number of headers
   - Benchmark header processing

---

## Related Documentation

- [HTTP Custom Headers Feature](./HTTP_CUSTOM_HEADERS_FEATURE.md)
- [Unit Tests](../../Tests/Monitors/HttpServiceMonitoringShould.cs)
- [Security Best Practices](../wiki/Security.md)

---

## Summary

These integration tests provide:

✅ **End-to-End Validation** - Real HTTP requests with actual servers  
✅ **Security Coverage** - Bearer tokens, API keys, sensitive data  
✅ **Concurrency Testing** - Multiple requests with headers  
✅ **Backward Compatibility** - Tests with and without headers  
✅ **Error Scenarios** - Failed requests still send headers  

**All tests use real HTTP infrastructure for maximum confidence!** 🎯
