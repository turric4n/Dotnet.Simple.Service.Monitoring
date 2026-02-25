# Running Redis Acceptance Tests

## Quick Start

### Prerequisites
1. **Docker Desktop** must be installed and running
2. **.NET 10 SDK** installed
3. No Redis installation required (uses Docker containers)

### Run All Redis Acceptance Tests
```bash
cd Simple.Service.Monitoring.Tests
dotnet test --filter "FullyQualifiedName~DockerRedisAcceptanceTests"
```

## Test Breakdown

### Original Functionality Tests (3 tests)
? Basic health monitoring  
? Timeout handling  
? Read/write operations  

### New Concurrency Tests (4 tests)
? 20 concurrent health checks  
? 30 rapid sequential checks  
? Multiple health check instances  
? 100 concurrent stress test  

## What Happens During Test Execution

1. **Container Startup** (~5-10 seconds)
   - Testcontainers pulls Redis image (first time only)
   - Starts Redis container on random port
   - Seeds test data

2. **Test Execution** (~30-60 seconds)
   - All 7 tests run sequentially
   - Each test validates different scenario
   - Concurrency tests verify no ObjectDisposedException

3. **Cleanup** (~2-3 seconds)
   - Container stops automatically
   - Resources are cleaned up
   - No manual cleanup needed

## Expected Output

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

  Simple.Service.Monitoring.Tests net10.0 Testing (XX,Xs)
  
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: ~1 min

Test summary: total: 7; failed: 0; succeeded: 7; skipped: 0
```

## Individual Test Execution

### Test 1: Basic Connection
```bash
dotnet test --filter "Should_Monitor_Redis_Connection_Successfully"
```
**Purpose**: Verify basic health check works  
**Duration**: ~5s  

### Test 2: Timeout Handling
```bash
dotnet test --filter "Should_Monitor_Redis_With_Short_Timeout"
```
**Purpose**: Ensure timeout configuration works  
**Duration**: ~3s  

### Test 3: Read/Write Operations
```bash
dotnet test --filter "Should_Verify_Redis_Can_Read_And_Write"
```
**Purpose**: Validate Redis operations before health check  
**Duration**: ~5s  

### Test 4: Concurrent Health Checks ?
```bash
dotnet test --filter "Should_Handle_Multiple_Concurrent_HealthChecks_Against_Real_Redis"
```
**Purpose**: Verify 20 concurrent checks succeed without ObjectDisposedException  
**Duration**: ~10s  
**Key Validation**: Race condition fix  

### Test 5: Rapid Sequential Checks
```bash
dotnet test --filter "Should_Handle_Rapid_Sequential_Checks_Against_Real_Redis"
```
**Purpose**: Test rapid execution (30 checks in ~300ms)  
**Duration**: ~5s  
**Key Validation**: Connection lifecycle per call  

### Test 6: Multiple Instances
```bash
dotnet test --filter "Should_Monitor_Multiple_Redis_Instances_Concurrently"
```
**Purpose**: Simulate real monitoring with multiple configured checks  
**Duration**: ~10s  
**Key Validation**: ASP.NET Core integration  

### Test 7: Stress Test ??
```bash
dotnet test --filter "Should_Handle_Connection_Stress_Test_With_Real_Redis"
```
**Purpose**: 100 concurrent health checks  
**Duration**: ~15s  
**Key Validation**: Production-scale concurrency  

## Troubleshooting

### Issue: Docker not running
**Error**: `Cannot connect to Docker daemon`  
**Solution**: 
```bash
# Windows
Start Docker Desktop

# Linux
sudo systemctl start docker
```

### Issue: Port already in use
**Error**: `Port 6379 is already allocated`  
**Solution**: Testcontainers uses random ports, but if Redis is running locally:
```bash
# Stop local Redis
docker stop $(docker ps -q --filter "ancestor=redis")

# Or stop Docker Desktop and restart
```

### Issue: Image pull timeout
**Error**: `Failed to pull image redis`  
**Solution**:
```bash
# Pre-pull the image
docker pull redis:latest

# Or use a proxy if behind corporate firewall
```

### Issue: Tests timeout
**Error**: `Test execution timed out`  
**Solution**:
```bash
# Increase timeout in test settings
dotnet test --filter "Category=Docker" -- NUnit.DefaultTimeout=120000
```

## Docker Container Details

### Automatic Management
- Container starts before tests
- Container stops after tests
- Unique name per test run
- Auto-cleanup on failure

### Manual Inspection (during test run)
```bash
# List running containers
docker ps

# View Redis logs
docker logs <container-id>

# Connect to Redis CLI
docker exec -it <container-id> redis-cli
```

## Performance Benchmarks

| Test | Checks | Duration | Success Rate |
|------|--------|----------|--------------|
| Concurrent (20) | 20 | ~10s | 100% |
| Sequential (30) | 30 | ~5s | 100% |
| Multiple Instances | 30 | ~10s | 100% |
| Stress Test (100) | 100 | ~15s | 100% |

## Integration with CI/CD

### GitHub Actions
```yaml
name: Redis Acceptance Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    
    - name: Run Redis Acceptance Tests
      run: |
        dotnet test --filter "FullyQualifiedName~DockerRedisAcceptanceTests" \
                    --logger "trx;LogFileName=test-results.trx"
    
    - name: Upload Test Results
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: '**/test-results.trx'
```

### Azure DevOps
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Redis Acceptance Tests'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--filter "FullyQualifiedName~DockerRedisAcceptanceTests" --logger trx'
```

## Why These Tests Matter

### Before Fix
- ? Random `ObjectDisposedException` in production
- ? Unpredictable failures under load
- ? Difficult to reproduce locally
- ? No way to verify the fix

### After Fix + Tests
- ? Verified fix with real Redis
- ? Reproducible test scenarios
- ? Confidence in concurrency handling
- ? Production-like validation
- ? Regression prevention

## Best Practices

1. **Run before committing**: Ensure changes don't break Redis monitoring
2. **Run in CI/CD**: Automated validation on every PR
3. **Monitor duration**: Tests should complete in ~1-2 minutes
4. **Check logs**: Review test output for warnings
5. **Update on changes**: Add tests for new Redis features

## Quick Reference Card

```bash
# All acceptance tests
dotnet test --filter "Category=Docker&Category=Acceptance"

# Only Redis acceptance tests  
dotnet test --filter "FullyQualifiedName~DockerRedisAcceptanceTests"

# Concurrency tests only
dotnet test --filter "FullyQualifiedName~DockerRedisAcceptanceTests&FullyQualifiedName~Concurrent"

# Stress test only
dotnet test --filter "Should_Handle_Connection_Stress_Test_With_Real_Redis"

# With verbose output
dotnet test --filter "FullyQualifiedName~DockerRedisAcceptanceTests" --logger "console;verbosity=detailed"

# Generate test report
dotnet test --filter "FullyQualifiedName~DockerRedisAcceptanceTests" --logger "trx;LogFileName=redis-tests.trx"
```

---

## Summary

? **7 acceptance tests** validate the fix with real Redis  
? **Fully automated** with Docker containers  
? **No manual setup** required  
? **Production-like** scenarios  
? **100% pass rate** after fix  

Run these tests to ensure Redis health monitoring works correctly under concurrent load!
