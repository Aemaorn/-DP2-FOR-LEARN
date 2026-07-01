# --------------------------
# Stage 1: Build
# --------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder

WORKDIR /src

# Copy configuration files
COPY ["Directory.Packages.props", "Directory.Packages.props"]

# Copy only project files for faster build cache reuse
COPY ["GHB.DP2.Api/GHB.DP2.Api.csproj", "GHB.DP2.Api/"]
COPY ["GHB.DP2.Application/GHB.DP2.Application.csproj", "GHB.DP2.Application/"]
COPY ["GHB.DP2.Domain/GHB.DP2.Domain.csproj", "GHB.DP2.Domain/"]
COPY ["GHB.DP2.Infrastructure/GHB.DP2.Infrastructure.csproj", "GHB.DP2.Infrastructure/"]
COPY ["GHB.DP2.ServiceDefaults/GHB.DP2.ServiceDefaults.csproj", "GHB.DP2.ServiceDefaults/"]
COPY ["GHB.DP2.SourceGenerators/GHB.DP2.SourceGenerators.csproj", "GHB.DP2.SourceGenerators/"]

# Restore packages with explicit configuration
RUN dotnet restore "GHB.DP2.Api/GHB.DP2.Api.csproj" \
    --runtime linux-x64 \
    --verbosity minimal

# Copy all sources
COPY . .

WORKDIR "/src/GHB.DP2.Api"

# Publish directly without separate build step to avoid package resolution issues
RUN dotnet publish "GHB.DP2.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --runtime linux-x64 \
    --verbosity minimal \
    -p:UseAppHost=false \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false

# --------------------------
# Stage 2: Runtime
# --------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install OpenLDAP client libraries and gosu for privilege drop
RUN apt update && \
    apt install --no-install-recommends -y libldap-2.5-0 curl gosu &&\
    ln -sf /usr/lib/x86_64-linux-gnu/libldap-2.5.so.0 /usr/lib/libldap.so.2 && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -d /app -s /bin/bash appuser

WORKDIR /app
COPY --from=builder /app/publish .
COPY ["GHB.DP2.Api/entrypoint.sh", "/app/entrypoint.sh"]
RUN chmod +x /app/entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]
