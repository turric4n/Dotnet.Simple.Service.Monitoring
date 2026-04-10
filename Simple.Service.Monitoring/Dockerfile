# =============================================================================
# Simple.Service.Monitoring - Runtime-Only Docker Image
# =============================================================================
# This image contains only the .NET runtime and the pre-built application.
# Build the application OUTSIDE Docker first:
#
#   dotnet publish Simple.Service.Monitoring/Simple.Service.Monitoring.csproj \
#       -c Release -o ./publish
#
# Then build the image:
#
#   docker build -t turric4n/simple-service-monitoring:latest \
#       -f Simple.Service.Monitoring/Dockerfile ./publish
#
# =============================================================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0

# OCI Image Labels
LABEL org.opencontainers.image.title="Simple Service Monitoring"
LABEL org.opencontainers.image.description="Standalone service monitoring suite with dashboard UI. Supports HTTP, TCP, DNS, SQL, Redis, RabbitMQ, Kafka, gRPC, and 20+ more service types with 24 alert transport channels."
LABEL org.opencontainers.image.source="https://github.com/turric4n/Dotnet.Simple.Service.Monitoring"
LABEL org.opencontainers.image.licenses="MIT"
LABEL org.opencontainers.image.authors="Turrican aka Enrique Fuentes"
LABEL org.opencontainers.image.vendor="Turrican Software"

WORKDIR /app

# Install curl for health check
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create writable data directory for LiteDb storage
RUN mkdir -p /app/data && chmod 777 /app/data

# Copy pre-built publish output (build context = ./publish folder)
COPY . .

EXPOSE 5000

# Default environment variables — all overridable at runtime
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Container health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -sf http://localhost:5000/ || exit 1

ENTRYPOINT ["dotnet", "Simple.Service.Monitoring.dll"]
