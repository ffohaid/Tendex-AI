# =============================================================================
# Tendex AI - Backend Dockerfile (Multi-stage Build)
# =============================================================================
# Produces a minimal, secure production image for the .NET 10 API.
# Stage 1: Restore & build (SDK image)
# Stage 2: Publish (SDK image)
# Stage 3: Runtime (ASP.NET runtime image, non-root user)
# =============================================================================

# ---------------------------------------------------------------------------
# Stage 1: Restore dependencies (cached layer)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy only project files first for better layer caching
COPY Directory.Build.props ./
COPY global.json ./

COPY src/TendexAI.Domain/TendexAI.Domain.csproj src/TendexAI.Domain/
COPY src/TendexAI.Application/TendexAI.Application.csproj src/TendexAI.Application/
COPY src/TendexAI.Infrastructure/TendexAI.Infrastructure.csproj src/TendexAI.Infrastructure/
COPY src/TendexAI.API/TendexAI.API.csproj src/TendexAI.API/

# Restore only the API project (which pulls all dependencies transitively)
# NOTE: We do NOT use TendexAI.slnx because .dockerignore excludes tests/
RUN dotnet restore src/TendexAI.API/TendexAI.API.csproj

# ---------------------------------------------------------------------------
# Stage 2: Build & Publish
# ---------------------------------------------------------------------------
FROM restore AS publish
WORKDIR /src

# Copy all source code
COPY . .

RUN dotnet publish src/TendexAI.API/TendexAI.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Stage 3: Runtime (minimal, non-root)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Security: Create non-root user
RUN groupadd --gid 1001 tendex && \
    useradd --uid 1001 --gid tendex --shell /bin/false --create-home tendex

# Install curl for healthcheck with retries to reduce transient network failures in CI builds
RUN set -eux; \
    apt_get_update() { apt-get update -o Acquire::Retries=5 -o Acquire::http::Timeout=30 -o Acquire::https::Timeout=30; }; \
    apt_get_update; \
    for attempt in 1 2 3; do \
        if apt-get install -y --no-install-recommends curl; then \
            break; \
        fi; \
        if [ "$attempt" -eq 3 ]; then \
            exit 1; \
        fi; \
        apt-get clean; \
        rm -rf /var/lib/apt/lists/*; \
        sleep 5; \
        apt_get_update; \
    done; \
    rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=publish /app/publish .

# Create keys directory for OpenIddict persistent RSA keys
# This directory should be mounted as a Docker volume for persistence
RUN mkdir -p /app/keys && chown tendex:tendex /app/keys && chmod 700 /app/keys
RUN mkdir -p /app/uploads && chown tendex:tendex /app/uploads && chmod 755 /app/uploads

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Expose port
EXPOSE 8080

# Switch to non-root user
USER tendex

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -sf http://localhost:8080/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "TendexAI.API.dll"]
