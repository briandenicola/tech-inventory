# Tech Inventory API — Multi-stage build
# Base: .NET 10.0 SDK + ASP.NET Core Runtime (Alpine for minimal footprint)
# Non-root user (1000), read-only FS, healthcheck baked in

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files (layer cache optimization)
# Note: repo uses the new XML solution format (`.slnx`). `dotnet restore`
# below targets the csproj directly so the solution file is not strictly
# required, but we copy it for parity with local `dotnet` workflows.
COPY TechInventory.slnx ./
COPY src/TechInventory.Domain/TechInventory.Domain.csproj ./src/TechInventory.Domain/
COPY src/TechInventory.Application/TechInventory.Application.csproj ./src/TechInventory.Application/
COPY src/TechInventory.Infrastructure/TechInventory.Infrastructure.csproj ./src/TechInventory.Infrastructure/
COPY src/TechInventory.Api/TechInventory.Api.csproj ./src/TechInventory.Api/

# Restore dependencies
RUN dotnet restore src/TechInventory.Api/TechInventory.Api.csproj

# Copy remaining source
COPY src/ ./src/

# Build and publish
WORKDIR /src/src/TechInventory.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install wget for healthcheck
RUN apk add --no-cache wget

# Create non-root user and data directory
RUN addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -s /bin/sh -D appuser && \
    mkdir -p /data && \
    chown -R appuser:appgroup /data /app

# Copy published app
COPY --from=build /app/publish .

# Switch to non-root
USER 1000

# Expose API port
EXPOSE 8080

# Set ASP.NET Core to listen on 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TechInventory.Api.dll"]
