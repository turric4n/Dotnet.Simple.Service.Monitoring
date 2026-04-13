#!/usr/bin/env bash
# =============================================================================
# Kythr - Docker Image Build Script (Linux/macOS/CI)
# =============================================================================
# Usage:
#   ./build-docker.sh                                  # builds with :latest tag
#   ./build-docker.sh --tag 1.0.0                      # builds with :1.0.0 tag
#   ./build-docker.sh --tag 1.0.0 --push               # builds and pushes
#   ./build-docker.sh --image myregistry/myimage        # custom image name
# =============================================================================

set -euo pipefail

# Defaults
TAG="latest"
IMAGE_NAME="turric4n/kythr"
PUSH=false
CONFIGURATION="Release"
PUBLISH_DIR="./publish"
PROJECT_PATH="Kythr/Kythr.csproj"
DOCKERFILE_PATH="Kythr/Dockerfile"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --tag|-t)       TAG="$2"; shift 2 ;;
        --image|-i)     IMAGE_NAME="$2"; shift 2 ;;
        --push|-p)      PUSH=true; shift ;;
        --config|-c)    CONFIGURATION="$2"; shift 2 ;;
        --output|-o)    PUBLISH_DIR="$2"; shift 2 ;;
        --help|-h)
            echo "Usage: $0 [--tag TAG] [--image IMAGE] [--push] [--config CONFIG] [--output DIR]"
            exit 0
            ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

FULL_IMAGE_TAG="${IMAGE_NAME}:${TAG}"

echo "============================================================"
echo " Kythr - Docker Build"
echo "============================================================"
echo ""
echo "  Image:          ${FULL_IMAGE_TAG}"
echo "  Configuration:  ${CONFIGURATION}"
echo "  Publish Dir:    ${PUBLISH_DIR}"
echo ""

# Step 1: Clean publish directory
if [ -d "${PUBLISH_DIR}" ]; then
    echo "[1/3] Cleaning previous publish output..."
    rm -rf "${PUBLISH_DIR}"
fi

# Step 2: Publish the application
echo "[2/3] Publishing application (${CONFIGURATION})..."
dotnet publish "${PROJECT_PATH}" -c "${CONFIGURATION}" -o "${PUBLISH_DIR}" /p:UseAppHost=false

echo "  Published to: ${PUBLISH_DIR}"

# Step 3: Build Docker image
echo "[3/3] Building Docker image: ${FULL_IMAGE_TAG} ..."
docker build -t "${FULL_IMAGE_TAG}" -f "${DOCKERFILE_PATH}" "${PUBLISH_DIR}"

echo ""
echo "============================================================"
echo " Docker image built successfully!"
echo " Image: ${FULL_IMAGE_TAG}"
echo "============================================================"

# Also tag as latest if a specific version tag was given
if [ "${TAG}" != "latest" ]; then
    LATEST_TAG="${IMAGE_NAME}:latest"
    echo "  Also tagging as: ${LATEST_TAG}"
    docker tag "${FULL_IMAGE_TAG}" "${LATEST_TAG}"
fi

# Optional: Push to Docker Hub
if [ "${PUSH}" = true ]; then
    echo ""
    echo "Pushing to Docker Hub..."
    docker push "${FULL_IMAGE_TAG}"
    if [ "${TAG}" != "latest" ]; then
        docker push "${IMAGE_NAME}:latest"
    fi
    echo "  Pushed successfully!"
fi

echo ""
echo "Run the container:"
echo "  docker run -p 5000:5000 ${FULL_IMAGE_TAG}"
echo ""
echo "Run with custom config:"
echo "  docker run -p 5000:5000 -v ./config/appsettings.yml:/app/appsettings.yml:ro ${FULL_IMAGE_TAG}"
echo ""
