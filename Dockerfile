# =============================================================================
# Thrive Stream Controller - Multi-stage Docker Build
# Builds both React UI and .NET API, serves UI as static files from API
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build React UI
# -----------------------------------------------------------------------------
FROM node:20-alpine AS ui-build

WORKDIR /app/ui

# Copy package files first for better caching
COPY UI/package*.json ./

# Install dependencies
RUN npm ci

# Copy UI source
COPY UI/ ./

# Build production bundle
RUN npm run build

# -----------------------------------------------------------------------------
# Stage 2: Build .NET API
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api-build

WORKDIR /app

# Copy solution and project files first for better caching
COPY API/ThriveStreamController.sln ./
COPY API/ThriveStreamController.API/ThriveStreamController.API.csproj ./ThriveStreamController.API/
COPY API/ThriveStreamController.Core/ThriveStreamController.Core.csproj ./ThriveStreamController.Core/
COPY API/ThriveStreamController.Data/ThriveStreamController.Data.csproj ./ThriveStreamController.Data/
COPY API/ThriveStreamController.Tests/ThriveStreamController.Tests.csproj ./ThriveStreamController.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all API source
COPY API/ ./

# Build and publish
RUN dotnet publish ThriveStreamController.API/ThriveStreamController.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# -----------------------------------------------------------------------------
# Stage 3: Final Runtime Image
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published API
COPY --from=api-build /app/publish ./

# Copy built UI to wwwroot
COPY --from=ui-build /app/ui/dist ./wwwroot

# Create data directory for SQLite database and logs
RUN mkdir -p /app/data /app/logs && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/thrivestream.db"

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/obs/status || exit 1

# Start the application
ENTRYPOINT ["dotnet", "ThriveStreamController.API.dll"]

