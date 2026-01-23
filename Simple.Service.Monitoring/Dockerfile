# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Simple.Service.Monitoring.csproj", "./"]
RUN dotnet restore "Simple.Service.Monitoring.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Simple.Service.Monitoring.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Simple.Service.Monitoring.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# Copy published files
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Simple.Service.Monitoring.dll"]
