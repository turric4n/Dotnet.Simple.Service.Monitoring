# Simple.Service.Monitoring.UI - Frontend Build Setup

## Issue Resolution

The build was failing with:
```
MSB3073: The command "npm run build:dev" exited with code 2.
```

## Root Cause

The `node_modules` directory was missing because `npm install` had not been run. The webpack build script requires all npm dependencies to be installed before it can compile the TypeScript/JavaScript assets.

## Solution

Run the following command in the `Simple.Service.Monitoring.UI` directory:

```bash
cd Simple.Service.Monitoring.UI
npm install
```

This installs all required dependencies:
- webpack (bundler)
- TypeScript compiler
- CSS loaders
- SignalR client
- vis-timeline (timeline visualization)
- Other dev dependencies

## Build Process Flow

The `.csproj` file has custom targets that run webpack during build:

### Debug Build
```xml
<Target Name="WebpackDevelopment" BeforeTargets="Build" Condition="'$(Configuration)' == 'Debug'">
    <Exec Command="npm run build:dev" ContinueOnError="false" />
</Target>
```

### Release Build
```xml
<Target Name="WebpackProduction" BeforeTargets="Build" Condition="'$(Configuration)' == 'Release'">
    <Exec Command="npm run build" ContinueOnError="false" />
</Target>
```

## Build Output

The webpack build creates:

- **`wwwroot/js/`** - Compiled JavaScript bundles with content hashing
  - `monitoring.[hash].js` - Main application bundle
  - `monitoring-vendors.[hash].js` - Third-party dependencies

- **`wwwroot/css/`** - Extracted CSS files with content hashing
  - `monitoring.[hash].css` - Application styles
  - `monitoring-vendors.[hash].css` - Third-party styles

- **`wwwroot/lib/`** - Copied static assets
  - SignalR client library
  - vis-timeline styles

- **`wwwroot/asset-manifest.json`** - Mapping of asset names to hashed filenames

## Development Workflow

### First Time Setup
```bash
cd Simple.Service.Monitoring.UI
npm install
```

### Regular Development
```bash
# Build in development mode
dotnet build -c Debug

# Or use webpack watch mode for auto-rebuild
npm run watch
```

### Production Build
```bash
# Build with minification
dotnet build -c Release
```

### Clean Build
```bash
# Clean webpack output
npm run clean

# Or use dotnet clean (triggers webpack clean)
dotnet clean
```

## npm Scripts Available

| Script | Description |
|--------|-------------|
| `npm run build` | Production build with minification |
| `npm run build:dev` | Development build with source maps |
| `npm run dev` | Same as build:dev |
| `npm run watch` | Watch mode - auto-rebuild on file changes |
| `npm run clean` | Remove built assets |

## Troubleshooting

### Build fails with "npm: command not found"
- Install Node.js from https://nodejs.org/
- Restart your terminal/IDE

### Build fails with module errors
```bash
# Delete node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

### Webpack version conflicts
```bash
# Clear npm cache
npm cache clean --force
npm install
```

### Missing TypeScript files
- Ensure all `.ts` files are in `Front/src/`
- Check `tsconfig.json` includes pattern

### Build succeeds but UI doesn't load
- Check browser console for 404 errors
- Verify `asset-manifest.json` was created
- Ensure `AssetHelper.cs` is reading the manifest correctly

## CI/CD Considerations

In your CI/CD pipeline, ensure you:

1. **Install Node.js** (usually v18 or v20)
2. **Run `npm ci`** (faster than npm install, uses package-lock.json)
3. **Build the project** (npm build runs automatically via MSBuild)

Example GitHub Actions:
```yaml
- name: Setup Node.js
  uses: actions/setup-node@v3
  with:
    node-version: '20'

- name: Install npm dependencies
  working-directory: Simple.Service.Monitoring.UI
  run: npm ci

- name: Build
  run: dotnet build -c Release
```

Example Dockerfile:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
RUN apt-get install -y nodejs

WORKDIR /src
COPY . .

# Restore and build (npm install happens via MSBuild)
RUN dotnet restore
RUN dotnet build -c Release
```

## Dependencies Overview

### Runtime Dependencies
- `@microsoft/signalr` - Real-time communication
- `vis-timeline` - Timeline visualization
- `chartjs-plugin-stacked100` - Chart plugin

### Development Dependencies
- `webpack` - Module bundler
- `typescript` - Type-safe JavaScript
- `ts-loader` - TypeScript loader for webpack
- `css-loader` - CSS module loader
- `mini-css-extract-plugin` - Extract CSS to files
- `clean-webpack-plugin` - Clean output directory
- `copy-webpack-plugin` - Copy static assets
- `webpack-manifest-plugin` - Generate asset manifest

## Notes

- Content hashing (`[contenthash]`) ensures browser cache invalidation
- Source maps are generated for debugging
- The build is configured to work with the embedded resource pattern used by the library

---

## Quick Reference

```bash
# First time or after git pull
npm install

# Regular development
dotnet build

# Watch mode for frontend development
npm run watch

# Clean everything
dotnet clean
```

? **Build is now working!** All tests pass and the application compiles successfully.
