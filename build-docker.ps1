# =============================================================================
# Kythr - Docker Image Build Script (Windows)
# =============================================================================
# Usage:
#   .\build-docker.ps1                              # builds with :latest tag
#   .\build-docker.ps1 -Tag "1.0.0"                 # builds with :1.0.0 tag
#   .\build-docker.ps1 -Tag "1.0.0" -Push           # builds and pushes to Docker Hub
#   .\build-docker.ps1 -ImageName "myregistry/img"   # custom image name
# =============================================================================

param(
    [string]$Tag = "latest",
    [string]$ImageName = "turric4n/kythr",
    [switch]$Push,
    [string]$Configuration = "Release",
    [string]$PublishDir = "./publish"
)

$ErrorActionPreference = "Stop"

$FullImageTag = "${ImageName}:${Tag}"
$ProjectPath = "Kythr/Kythr.csproj"
$DockerfilePath = "Kythr/Dockerfile"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Kythr - Docker Build" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Image:          $FullImageTag" -ForegroundColor Yellow
Write-Host "  Configuration:  $Configuration" -ForegroundColor Yellow
Write-Host "  Publish Dir:    $PublishDir" -ForegroundColor Yellow
Write-Host ""

# Step 1: Clean publish directory
if (Test-Path $PublishDir) {
    Write-Host "[1/3] Cleaning previous publish output..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $PublishDir
}

# Step 2: Publish the application
Write-Host "[2/3] Publishing application ($Configuration)..." -ForegroundColor Green
dotnet publish $ProjectPath -c $Configuration -o $PublishDir /p:UseAppHost=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "  Published to: $PublishDir" -ForegroundColor Gray

# Step 3: Build Docker image
Write-Host "[3/3] Building Docker image: $FullImageTag ..." -ForegroundColor Green
docker build -t $FullImageTag -f $DockerfilePath $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: docker build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Docker image built successfully!" -ForegroundColor Green
Write-Host " Image: $FullImageTag" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan

# Optional: Also tag as latest if a specific version tag was given
if ($Tag -ne "latest") {
    $LatestTag = "${ImageName}:latest"
    Write-Host "  Also tagging as: $LatestTag" -ForegroundColor Gray
    docker tag $FullImageTag $LatestTag
}

# Optional: Push to Docker Hub
if ($Push) {
    Write-Host ""
    Write-Host "Pushing to Docker Hub..." -ForegroundColor Green
    docker push $FullImageTag
    if ($Tag -ne "latest") {
        docker push "${ImageName}:latest"
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: docker push failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "  Pushed successfully!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Run the container:" -ForegroundColor Cyan
Write-Host "  docker run -p 5000:5000 $FullImageTag" -ForegroundColor White
Write-Host ""
Write-Host "Run with custom config:" -ForegroundColor Cyan
Write-Host "  docker run -p 5000:5000 -v ./config/appsettings.yml:/app/appsettings.yml:ro $FullImageTag" -ForegroundColor White
Write-Host ""
