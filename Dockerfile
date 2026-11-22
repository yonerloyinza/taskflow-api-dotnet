# Multi-stage Dockerfile for taskflow-api-c-sharp
# VPS AWS EC2 + Traefik — .NET 9 Alpine

# ==========================================
# Stage 1: Build
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached independently
COPY *.sln ./
COPY src/TaskFlow.Api/TaskFlow.Api.csproj src/TaskFlow.Api/
COPY src/TaskFlow.Application/TaskFlow.Application.csproj src/TaskFlow.Application/
COPY src/TaskFlow.Domain/TaskFlow.Domain.csproj src/TaskFlow.Domain/
COPY src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj src/TaskFlow.Infrastructure/

RUN dotnet restore src/TaskFlow.Api/TaskFlow.Api.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish src/TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app/publish --no-restore

# ==========================================
# Stage 2: Migrations (dotnet ef against DIRECT_URL)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS migrations
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    PATH="$PATH:/root/.dotnet/tools"

COPY . .

RUN dotnet tool install --global dotnet-ef --version 9.0.0 \
    && dotnet restore src/TaskFlow.Api/TaskFlow.Api.csproj

ENTRYPOINT ["dotnet", "ef", "database", "update", \
    "--project", "src/TaskFlow.Api/TaskFlow.Api.csproj", \
    "--startup-project", "src/TaskFlow.Api/TaskFlow.Api.csproj"]

# ==========================================
# Stage 3: Runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache ca-certificates tzdata wget

# Non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

# Internal port only: Traefik routes HTTPS -> 3000
EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD wget -q -O - http://127.0.0.1:3000/health | grep -q '"status":"ok"' || exit 1

ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]