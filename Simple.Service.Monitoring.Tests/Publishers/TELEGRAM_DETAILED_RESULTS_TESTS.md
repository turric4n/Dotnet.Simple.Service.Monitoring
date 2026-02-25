# Telegram Detailed Results Test Cases - Guide

## Overview

Added 6 comprehensive test cases to validate the detailed failure/success reporting feature in Telegram alerts. These tests demonstrate real-world scenarios with multiple endpoints and verify proper formatting.

---

## New Test Cases

### 1. **Should_Display_Detailed_Failures_And_Successes** ?

**Category**: DetailedResults  
**Scenario**: Multi-endpoint HTTP check with mixed results (1 success, 2 failures)

**Purpose**: Validates the core feature - displaying both failures and successes

**Test Data**:
```csharp
Failures (2):
  • https://api2.example.com returned 503, expected 200
  • https://api3.example.com timed out after 5000ms

Successes (1):
  • https://api1.example.com returned expected status code 200
```

**Expected Telegram Message**:
```
? [Unhealthy] telegram_detailed_results

?? Detailed Results:

? Failed (2):
  • https://api2.example.com returned 503, expected 200
  • https://api3.example.com timed out after 5000ms

? Succeeded (1):
  • https://api1.example.com returned expected status code 200
```

**Validates**: Basic feature functionality

---

### 2. **Should_Display_All_Failures_When_Complete_Outage** ??

**Category**: DetailedResults  
**Scenario**: Complete service outage - all 3 endpoints failed

**Purpose**: Tests worst-case scenario with no successes

**Test Data**:
```csharp
Failures (3):
  • https://primary.example.com timed out after 5000ms
  • https://backup1.example.com returned 500, expected 200
  • https://backup2.example.com failed: Connection refused

Successes: (none)
```

**Expected Telegram Message**:
```
? [Unhealthy] telegram_complete_outage

Error Details: HTTP health check failed for 3 of 3 endpoints

?? Detailed Results:

? Failed (3):
  • https://primary.example.com timed out after 5000ms
  • https://backup1.example.com returned 500, expected 200
  • https://backup2.example.com failed: Connection refused
```

**Validates**: 
- Handles all-failure scenario
- No "Succeeded" section shown when none exist
- Critical situation clearly communicated

---

### 3. **Should_Display_Ping_Results_With_Multiple_Hosts** ??

**Category**: DetailedResults  
**Scenario**: Network ping check - 5 hosts, 3 responding, 2 timeout

**Purpose**: Tests Ping health check type with network diagnostics

**Test Data**:
```csharp
Failures (2):
  • 192.168.1.3 timed out after 1000ms
  • 192.168.1.5 returned status TimedOut

Successes (3):
  • 192.168.1.1 responded in 12ms
  • 192.168.1.2 responded in 15ms
  • 192.168.1.4 responded in 8ms
```

**Expected Telegram Message**:
```
? [Unhealthy] telegram_ping_multiple_hosts

Error Details: Ping failed for 2 of 5 hosts

?? Detailed Results:

? Failed (2):
  • 192.168.1.3 timed out after 1000ms
  • 192.168.1.5 returned status TimedOut

? Succeeded (3):
  • 192.168.1.1 responded in 12ms
  • 192.168.1.2 responded in 15ms
  • 192.168.1.4 responded in 8ms
```

**Validates**:
- Works with Ping health checks
- Network diagnostics with latency info
- Partial network failure visibility

---

### 4. **Should_Display_Only_Failures_When_No_Successes** ??

**Category**: DetailedResults  
**Scenario**: Only failures exist (complete failure, different from test #2)

**Purpose**: Ensures graceful handling when successes list is null/empty

**Test Data**:
```csharp
Failures (2):
  • https://down1.example.com connection refused
  • https://down2.example.com DNS resolution failed

Successes: null
```

**Expected Telegram Message**:
```
? [Unhealthy] telegram_only_failures

Error Details: All endpoints failed

?? Detailed Results:

? Failed (2):
  • https://down1.example.com connection refused
  • https://down2.example.com DNS resolution failed
```

**Validates**:
- Null safety for successes list
- Only failures section displayed
- No empty "Succeeded" section

---

### 5. **Should_Handle_Large_Number_Of_Endpoints** ??

**Category**: DetailedResults  
**Scenario**: Scalability test - 10 endpoints (3 failed, 7 succeeded)

**Purpose**: Tests formatting with many items, ensures message isn't too large

**Test Data**:
```csharp
Failures (3):
  • https://api1.example.com returned 500, expected 200
  • https://api2.example.com returned 500, expected 200
  • https://api3.example.com returned 500, expected 200

Successes (7):
  • https://api4.example.com returned expected status code 200
  • https://api5.example.com returned expected status code 200
  ... (through api10)
```

**Expected Telegram Message**:
```
? [Unhealthy] telegram_many_endpoints

Error Details: HTTP health check failed for 3 of 10 endpoints

?? Detailed Results:

? Failed (3):
  • https://api1.example.com returned 500, expected 200
  • https://api2.example.com returned 500, expected 200
  • https://api3.example.com returned 500, expected 200

? Succeeded (7):
  • https://api4.example.com returned expected status code 200
  • https://api5.example.com returned expected status code 200
  • https://api6.example.com returned expected status code 200
  • https://api7.example.com returned expected status code 200
  • https://api8.example.com returned expected status code 200
  • https://api9.example.com returned expected status code 200
  • https://api10.example.com returned expected status code 200
```

**Validates**:
- Scalability with many endpoints
- Message formatting with long lists
- Telegram message size limits handled
- Performance with larger data sets

---

### 6. **Should_Display_Degraded_Status_With_Partial_Failures** ??

**Category**: DetailedResults  
**Scenario**: Degraded status due to slow performance (not complete failure)

**Purpose**: Tests ?? Degraded status with performance issues

**Test Data**:
```csharp
Failures (1):
  • https://api2.example.com responded in 4500ms (threshold: 2000ms)

Successes (3):
  • https://api1.example.com responded in 120ms
  • https://api3.example.com responded in 85ms
  • https://api4.example.com responded in 95ms
```

**Expected Telegram Message**:
```
?? [Degraded] telegram_degraded_partial

Error Details: Service degraded: 1 of 4 endpoints slow

?? Detailed Results:

? Failed (1):
  • https://api2.example.com responded in 4500ms (threshold: 2000ms)

? Succeeded (3):
  • https://api1.example.com responded in 120ms
  • https://api3.example.com responded in 85ms
  • https://api4.example.com responded in 95ms
```

**Validates**:
- ?? Degraded emoji and status
- Performance issue reporting
- Detailed results work with Degraded status
- SLA/threshold monitoring

---

## Running the Tests

### All Detailed Results Tests

```bash
dotnet test --filter "Category=DetailedResults"
```

**Expected Output**:
```
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

### Individual Tests

```bash
# Mixed results (basic feature)
dotnet test --filter "Should_Display_Detailed_Failures_And_Successes"

# Complete outage
dotnet test --filter "Should_Display_All_Failures_When_Complete_Outage"

# Network diagnostics
dotnet test --filter "Should_Display_Ping_Results_With_Multiple_Hosts"

# Only failures
dotnet test --filter "Should_Display_Only_Failures_When_No_Successes"

# Scalability test
dotnet test --filter "Should_Handle_Large_Number_Of_Endpoints"

# Degraded status
dotnet test --filter "Should_Display_Degraded_Status_With_Partial_Failures"
```

---

## Test Coverage Matrix

| Scenario | Status | Failures | Successes | Service Type | Key Validation |
|----------|--------|----------|-----------|--------------|----------------|
| Mixed Results | Unhealthy | 2 | 1 | HTTP | Basic feature |
| Complete Outage | Unhealthy | 3 | 0 | HTTP | All failures |
| Network Issues | Unhealthy | 2 | 3 | Ping | Network diagnostics |
| Only Failures | Unhealthy | 2 | null | HTTP | Null safety |
| Many Endpoints | Unhealthy | 3 | 7 | HTTP | Scalability |
| Performance | Degraded | 1 | 3 | HTTP | Degraded status |

---

## What Each Test Validates

### Code Coverage

| Component | Validation |
|-----------|------------|
| **HealthCheckData.cs** | Data extraction from HealthReportEntry.Data |
| **TelegramAlertingPublisher.cs** | Formatting failures/successes |
| **Message Formatting** | HTML bold tags, bullet points |
| **Edge Cases** | Null lists, empty lists, large lists |
| **Status Emojis** | ? Unhealthy, ?? Degraded |

### Real-World Scenarios

| Test | Real-World Equivalent |
|------|----------------------|
| #1 - Mixed Results | Load balancer with one backend down |
| #2 - Complete Outage | Complete service failure |
| #3 - Ping Multiple | Network infrastructure monitoring |
| #4 - Only Failures | DNS/connection issues |
| #5 - Large Number | Microservices mesh monitoring |
| #6 - Degraded | Performance degradation under load |

---

## Prerequisites

### Before Running Tests

1. **Configure Bot Credentials** (lines 26-27 in test file):
```csharp
private const string BOT_API_TOKEN = "YOUR_BOT_TOKEN";
private const string CHAT_ID = "YOUR_CHAT_ID";
```

2. **Ensure Docker is Running** (for other tests in the suite)

3. **Valid Telegram Bot Setup**:
   - Create bot via @BotFather
   - Get bot token
   - Get chat ID (personal or group)

---

## Expected Telegram Messages

### Message Count by Test

| Test | Telegram Messages Sent |
|------|----------------------|
| #1 - Mixed Results | 1 message |
| #2 - Complete Outage | 1 message |
| #3 - Ping Multiple | 1 message |
| #4 - Only Failures | 1 message |
| #5 - Large Number | 1 message |
| #6 - Degraded | 1 message |
| **Total** | **6 messages** |

### Message Features to Verify

When you run the tests, check that each Telegram message includes:

? **Status emoji** (? for Unhealthy, ?? for Degraded)  
? **Service name** in bold  
? **?? Detailed Results** section  
? **? Failed (X)** section with bullet list  
? **? Succeeded (X)** section with bullet list (when applicable)  
? **Proper HTML formatting** (bold tags work)  
? **Accurate counts** matching the data  

---

## Integration with Existing Tests

### Test Suite Structure

```
TelegramPublisherIntegrationShould.cs
??? Failure Scenarios (4 tests)
??? Recovery Scenarios (3 tests)
??? Scheduling Tests (2 tests)
??? Complex Scenarios (1 test)
??? Detailed Results Tests (6 tests) ? NEW
??? Configuration Guide (1 test)

Total: 17 tests
```

### Running All Tests

```bash
# All Telegram publisher tests
dotnet test --filter "Category=TelegramPublisher"

# Expected: 16 tests pass (17 total - 1 is [Explicit] guide)
```

---

## Benefits of These Tests

### 1. **Feature Validation**
Proves the detailed results feature works end-to-end with real Telegram API.

### 2. **Regression Prevention**
Ensures future changes don't break the feature.

### 3. **Documentation**
Tests serve as executable examples of the feature.

### 4. **Edge Case Coverage**
Validates null handling, large lists, different statuses.

### 5. **Real-World Scenarios**
Tests actual monitoring use cases.

---

## Troubleshooting

### Test Fails: "Telegram bot credentials not configured"

**Solution**: Update `BOT_API_TOKEN` and `CHAT_ID` constants.

### Test Fails: Timeout

**Possible Causes**:
- Bot token invalid
- Chat ID incorrect
- Network issues
- Telegram API rate limiting

**Solution**: 
- Verify credentials
- Check network connectivity
- Add delays between tests if rate limited

### Message Not Received in Telegram

**Check**:
1. Bot added to chat/group
2. Chat ID is correct (negative for groups)
3. Bot has permission to send messages
4. Check Telegram API response in test output

---

## Future Enhancements

### Potential Additional Tests

1. **Message Size Limits**
   - Test with 50+ endpoints
   - Verify Telegram 4096 character limit handling

2. **Special Characters**
   - Test with URLs containing special chars
   - Test with unicode in error messages

3. **Custom Data Types**
   - Test with custom health check data
   - Test with nested objects

4. **Error Handling**
   - Test what happens if Data is malformed
   - Test with very long individual failure messages

---

## Quick Reference

### Run Specific Category
```bash
dotnet test --filter "Category=DetailedResults"
```

### Run Single Test
```bash
dotnet test --filter "Should_Display_Detailed_Failures_And_Successes"
```

### Run with Verbose Output
```bash
dotnet test --filter "Category=DetailedResults" --logger "console;verbosity=detailed"
```

### Run and Save Results
```bash
dotnet test --filter "Category=DetailedResults" --logger "trx;LogFileName=detailed-results-tests.trx"
```

---

## Success Criteria

### All 6 Tests Should:
- ? Pass without errors
- ? Send exactly 1 Telegram message each
- ? Display formatted failures/successes
- ? Show correct counts
- ? Use proper HTML formatting
- ? Complete within 2 seconds each

### Visual Verification in Telegram:
- ? Check messages in your Telegram chat
- ? Verify formatting renders correctly
- ? Confirm bullet points display properly
- ? Ensure bold text is bold
- ? Validate emojis appear correctly

---

## Conclusion

These 6 test cases provide comprehensive coverage of the detailed results feature:

| Coverage Type | Count |
|--------------|-------|
| Status Types | 2 (Unhealthy, Degraded) |
| Service Types | 2 (HTTP, Ping) |
| Edge Cases | 3 (null, empty, large) |
| Scenarios | 6 (mixed, outage, network, failures, scale, perf) |

**Total Validation**: End-to-end feature verification with real-world scenarios! ?

Run the tests and watch your Telegram fill with beautifully formatted diagnostic messages! ????
