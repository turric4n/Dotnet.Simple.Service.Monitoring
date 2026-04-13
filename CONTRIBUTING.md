# Contributing to Kythr

Thank you for your interest in contributing! This guide will help you get started.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or latest LTS)
- [Node.js 18+](https://nodejs.org/) (for the React dashboard)
- Visual Studio 2022 or VS Code
- Git

### Development Setup

```bash
# Clone the repository
git clone https://github.com/turric4n/Kythr.git
cd Kythr

# Restore and build
dotnet restore Kythr/Kythr.sln
dotnet build Kythr/Kythr.sln

# Run tests
dotnet test Kythr.Tests/Kythr.Tests.csproj

# (Optional) Start infrastructure for integration tests
docker-compose up -d
```

### UI Development

```bash
cd Kythr.UI
npm install
npm run watch    # Dev mode with hot reload
npm run build    # Production build
```

## How to Contribute

### Reporting Bugs

1. Check [existing issues](https://github.com/turric4n/Kythr/issues) to avoid duplicates
2. Open a new issue using the **Bug Report** template
3. Include: .NET version, OS, configuration (sanitized), steps to reproduce, expected vs actual behavior

### Suggesting Features

1. Open an issue using the **Feature Request** template
2. Describe the use case and expected behavior
3. Discuss the design before implementing

### Pull Requests

1. Fork the repository
2. Create a feature branch from `master`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. Make your changes following the [coding guidelines](#coding-guidelines)
4. Add or update tests as needed
5. Ensure all tests pass:
   ```bash
   dotnet test Kythr.Tests/Kythr.Tests.csproj
   ```
6. Commit with a clear message:
   ```bash
   git commit -m "feat(monitors): add RabbitMQ queue depth check"
   ```
7. Push and open a Pull Request against `master`

## Coding Guidelines

### C# / .NET

- Target .NET Standard 2.1 for library projects
- Follow standard C# naming conventions (PascalCase for public members, camelCase for private)
- Use `async/await` for I/O operations
- Add XML doc comments for public APIs
- Keep monitor implementations in `Kythr.Library/Monitoring/Implementations/`
- Keep publisher implementations in `Kythr.Library/Monitoring/Implementations/Publishers/`

### Adding a New Monitor

1. Add the service type to `ServiceType.cs` enum
2. Create `YourServiceMonitoring.cs` in `Monitoring/Implementations/`
3. Inherit from `ServiceMonitoringBase` and implement `SetUpMonitoring()`
4. Register in `StandardStackMonitoring.cs`
5. Add tests in `Kythr.Tests/Monitors/`
6. Update documentation

### Adding a New Publisher

1. Create a directory under `Monitoring/Implementations/Publishers/YourPublisher/`
2. Implement the publisher class
3. Add transport settings model in `Options/`
4. Register in the publisher pipeline
5. Add tests and documentation

### TypeScript / React (Dashboard)

- Use TypeScript strict mode
- Follow the existing component patterns (shadcn/ui, Tailwind CSS)
- Use Zustand for state management
- Use TanStack Query for server state

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(monitors): add Oracle database monitor
fix(publishers): resolve Telegram HTML encoding
docs: update service types reference
test(redis): add concurrency safety tests
```

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `Kythr/` | Host application |
| `Kythr.Library/` | Core library (monitors, publishers, models) |
| `Kythr.Extensions/` | DI extension methods |
| `Kythr.UI/` | React SPA dashboard |
| `Kythr.UI.Extensions/` | UI middleware |
| `Kythr.Tests/` | All tests |
| `docs/` | Additional documentation |
| `wiki/` | GitHub Wiki source |

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
