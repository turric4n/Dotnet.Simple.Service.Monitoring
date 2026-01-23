# Simple.Service.Monitoring Tests

This directory contains unit and integration tests for the Simple.Service.Monitoring application.

## Test Structure

- **HealthCheckTests.cs**: Unit tests for the HealthCheck implementation
- **ProgramTests.cs**: Tests for the Program startup configuration
- **StartupTests.cs**: Tests for the Startup class and service configuration

## Running Tests

### Using .NET CLI

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with detailed output
dotnet test --verbosity detailed
```

### Using Visual Studio

Open the solution in Visual Studio and use Test Explorer to run tests.

## Test Frameworks

- **xUnit**: Primary testing framework
- **FluentAssertions**: For fluent assertion syntax
- **Moq**: For mocking dependencies
- **Microsoft.AspNetCore.Mvc.Testing**: For integration testing

## Coverage

Code coverage reports are generated automatically during the CI/CD pipeline and can be viewed on Codecov.
