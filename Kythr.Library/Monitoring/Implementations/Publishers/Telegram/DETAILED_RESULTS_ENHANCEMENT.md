# Detailed Health Check Results in Telegram - Enhancement

## Overview

Enhanced Telegram notifications to display detailed failure and success information from `HealthCheckResult.Data` dictionary, providing comprehensive diagnostics for multi-endpoint health checks.

---

## Problem

When health checks fail (especially for HTTP checks with multiple endpoints or Ping checks with multiple hosts), the Telegram message only showed a generic error message without details about which specific endpoints failed or succeeded.

**Before**:
```
? [Unhealthy] API Health Check

?? Error Details: HTTP health check failed for 2 of 3 endpoints
```

**Missing Information**:
- Which endpoints failed?
- Which endpoints succeeded?
- What were the specific errors?

---

## Solution

### 1. Enhanced `HealthCheckData` Constructor

Modified to capture `HealthCheckResult.Data` dictionary and convert it to the `Tags` dictionary:

```csharp
public HealthCheckData(HealthReportEntry healthReportEntry, string name)
{
    // ...existing code...
    
    // Add data from HealthCheckResult.Data (e.g., Failures, Successes)
    if (healthReportEntry.Data != null)
    {
        foreach (var dataItem in healthReportEntry.Data)
        {
            if (dataItem.Value != null)
            {
                // Handle lists/arrays specially
                if (dataItem.Value is System.Collections.IEnumerable enumerable && 
                    !(dataItem.Value is string))
                {
                    var items = new List<string>();
                    foreach (var item in enumerable)
                    {
                        if (item != null)
                        {
                            items.Add(item.ToString());
                        }
                    }
                    
                    if (items.Any())
                    {
                        Tags[$"Data_{dataItem.Key}"] = string.Join("; ", items);
                    }
                }
                else
                {
                    Tags[$"Data_{dataItem.Key}"] = dataItem.Value.ToString();
                }
            }
        }
    }
}
```

**Key Features**:
- Converts `IReadOnlyDictionary<string, object>` to `Dictionary<string, string>`
- Handles collections (lists of failures/successes) by joining with semicolons
- Prefixes with `Data_` to avoid tag name conflicts
- Preserves all diagnostic information from health checks

### 2. Enhanced Telegram Message Formatting

Updated `SendTelegramMessage` to display detailed results:

```csharp
// Extract failures and successes from Tags
var failures = healthCheckData.Tags.GetValueOrDefault("Data_Failures");
var successes = healthCheckData.Tags.GetValueOrDefault("Data_Successes");

if (!string.IsNullOrEmpty(failures) || !string.IsNullOrEmpty(successes))
{
    body += $"\n?? <b>Detailed Results:</b>\n";
    
    if (!string.IsNullOrEmpty(failures))
    {
        var failureList = failures.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
        body += $"\n? <b>Failed ({failureList.Length}):</b>\n";
        foreach (var failure in failureList)
        {
            body += $"  • {failure}\n";
        }
    }
    
    if (!string.IsNullOrEmpty(successes))
    {
        var successList = successes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
        body += $"\n? <b>Succeeded ({successList.Length}):</b>\n";
        foreach (var success in successList)
        {
            body += $"  • {success}\n";
        }
    }
}
```

---

## Example Messages

### HTTP Health Check with Multiple Endpoints

**Scenario**: 3 endpoints monitored, 1 fails, 2 succeed

**Telegram Message**:
```
? [Unhealthy] <b>CDI API Cluster</b>

?? <b>Triggered On:</b> 2026-01-27 15:30:00
?? <b>Machine:</b> web-server-01
?? <b>Service Type:</b> Http
?? <b>Endpoint:</b> https://api1.example.com,https://api2.example.com,https://api3.example.com
?? <b>Duration:</b> 2505.43 ms
?? <b>Status:</b> Unhealthy

?? <b>Error Details:</b> HTTP health check failed for 1 of 3 endpoints

?? <b>Detailed Results:</b>

? <b>Failed (1):</b>
  • https://api2.example.com returned 503, expected 200

? <b>Succeeded (2):</b>
  • https://api1.example.com returned expected status code 200
  • https://api3.example.com returned expected status code 200

?? <b>Last updated:</b> 2026-01-27 15:30:00
```

### Ping Health Check with Multiple Hosts

**Scenario**: 5 hosts pinged, 2 fail, 3 succeed

**Telegram Message**:
```
? [Unhealthy] <b>Network Infrastructure</b>

?? <b>Triggered On:</b> 2026-01-27 15:35:00
?? <b>Machine:</b> monitor-server
?? <b>Service Type:</b> Ping
?? <b>Endpoint:</b> 192.168.1.1,192.168.1.2,192.168.1.3,192.168.1.4,192.168.1.5
?? <b>Duration:</b> 3240.18 ms
?? <b>Status:</b> Unhealthy

?? <b>Error Details:</b> Ping failed for 2 of 5 hosts

?? <b>Detailed Results:</b>

? <b>Failed (2):</b>
  • 192.168.1.3 timed out after 1000ms
  • 192.168.1.5 returned status TimedOut

? <b>Succeeded (3):</b>
  • 192.168.1.1 responded in 12ms
  • 192.168.1.2 responded in 15ms
  • 192.168.1.4 responded in 8ms

?? <b>Last updated:</b> 2026-01-27 15:35:00
```

### All Endpoints Failed

**Scenario**: Complete outage, all 3 endpoints down

**Telegram Message**:
```
? [Unhealthy] <b>Critical Service</b>

?? <b>Triggered On:</b> 2026-01-27 15:40:00
?? <b>Machine:</b> web-server-02
?? <b>Service Type:</b> Http
?? <b>Endpoint:</b> https://primary.example.com,https://backup1.example.com,https://backup2.example.com
?? <b>Duration:</b> 15023.67 ms
?? <b>Status:</b> Unhealthy

?? <b>Error Details:</b> HTTP health check failed for 3 of 3 endpoints

?? <b>Detailed Results:</b>

? <b>Failed (3):</b>
  • https://primary.example.com timed out after 5000ms
  • https://backup1.example.com returned 500, expected 200
  • https://backup2.example.com failed: Connection refused

?? <b>Last updated:</b> 2026-01-27 15:40:00
```

---

## Benefits

### 1. **Immediate Actionability**
Engineers can see exactly which endpoints/hosts are failing without checking logs or dashboards.

### 2. **Complete Context**
Messages include both failures AND successes, showing the scope of the problem:
- Partial outage vs. complete outage
- Which backup systems are working
- Network segmentation issues

### 3. **Root Cause Analysis**
Specific error messages help identify the issue:
- Timeout ? Network/performance problem
- Status code 500 ? Application error
- Connection refused ? Service not running

### 4. **No Log Diving Required**
All diagnostic information in one Telegram message - no need to SSH into servers or search logs.

---

## Technical Details

### Data Flow

```
HealthCheck
  ?
Creates HealthCheckResult with Data dictionary
  ?
HealthReportEntry created by ASP.NET Core
  ?
HealthCheckData constructor extracts Data
  ?
Converts to Tags dictionary with "Data_" prefix
  ?
TelegramAlertingPublisher formats and displays
  ?
Telegram message with detailed breakdowns
```

### Supported Data Types

| Source Type | Storage | Display |
|-------------|---------|---------|
| `List<string>` | Joined with `; ` | Bulleted list |
| `string[]` | Joined with `; ` | Bulleted list |
| `string` | As-is | Single value |
| `int`, `bool`, etc. | `.ToString()` | Single value |
| `IEnumerable<T>` | `.ToString()` items joined | Bulleted list |

### Health Checks Using This Feature

| Health Check Type | Data Included |
|------------------|---------------|
| HTTP | Failures, Successes |
| Ping | Failures, Successes |
| SQL (future) | Query results, errors |
| Redis (future) | Connection details |
| Custom | Any custom data |

---

## Configuration

No configuration changes required! This enhancement works automatically for all health checks that provide `Data` in their `HealthCheckResult`.

### How to Use in Custom Health Checks

```csharp
public class MyHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var failures = new List<string>();
        var successes = new List<string>();
        
        // ... perform checks ...
        
        if (failures.Any())
        {
            var data = new Dictionary<string, object>
            {
                { "Failures", failures },
                { "Successes", successes },
                { "CustomMetric", 42 } // Any additional data
            };
            
            return HealthCheckResult.Unhealthy(
                $"Check failed for {failures.Count} items",
                null,
                data); // ? Data will appear in Telegram!
        }
        
        return HealthCheckResult.Healthy("All checks passed");
    }
}
```

---

## Files Modified

1. **`HealthCheckData.cs`** 
   - Added Data dictionary extraction logic
   - Converts collections to semicolon-separated strings
   - Prefixes with `Data_` to avoid conflicts

2. **`TelegramAlertingPublisher.cs`**
   - Extracts `Data_Failures` and `Data_Successes`
   - Formats as bulleted lists
   - Excludes from additional tags section to avoid duplication

---

## Backward Compatibility

? **Fully backward compatible**:
- Health checks without `Data` work as before
- Existing messages show same info (no Data section added)
- New health checks automatically benefit from enhancement
- No configuration changes required

---

## Future Enhancements

### 1. Smart Formatting by Type

```csharp
// Detect data type and format appropriately
if (dataItem.Key == "QueryResult")
{
    // Format SQL query results as table
}
else if (dataItem.Key == "Metrics")
{
    // Format metrics with units
}
```

### 2. Truncation for Long Lists

```csharp
// If too many items, truncate and show count
if (failureList.Length > 10)
{
    // Show first 5
    // Show "... and 15 more"
}
```

### 3. Grouping by Error Type

```csharp
// Group failures by error type
var timeouts = failures.Where(f => f.Contains("timeout"));
var serverErrors = failures.Where(f => f.Contains("500"));
```

---

## Testing

### Test with Multiple HTTP Endpoints

```yaml
HealthChecks:
  - Name: "Multi-Endpoint Test"
    ServiceType: Http
    EndpointOrHost: "https://api1.example.com,https://api2.example.com,https://api3.example.com"
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedCode: 200
        HttpTimeoutMs: 5000
```

### Expected Telegram Output

When one endpoint fails, you'll see detailed breakdown with:
- ? Which endpoint failed and why
- ? Which endpoints succeeded
- Complete diagnostic information

---

## Comparison

### Before Enhancement

```
? [Unhealthy] API Cluster

?? Error Details: HTTP health check failed for 1 of 3 endpoints
```

**Questions This Raises**:
- Which endpoint failed?
- What was the error?
- Are the other 2 working?

### After Enhancement

```
? [Unhealthy] API Cluster

?? Error Details: HTTP health check failed for 1 of 3 endpoints

?? Detailed Results:

? Failed (1):
  • https://api2.example.com returned 503, expected 200

? Succeeded (2):
  • https://api1.example.com returned expected status code 200
  • https://api3.example.com returned expected status code 200
```

**All Questions Answered**! ?

---

## Conclusion

This enhancement transforms Telegram alerts from simple notifications into comprehensive diagnostic reports, enabling faster incident response and resolution without additional tooling.

**Key Achievements**:
- ? Detailed failure/success breakdown
- ? Zero configuration required
- ? Backward compatible
- ? Works with all health check types
- ? Extensible for custom data

**Impact**: Reduces mean time to resolution (MTTR) by providing complete diagnostic information in the initial alert! ??
