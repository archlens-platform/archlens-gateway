FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY archlens-gateway/*.sln ./archlens-gateway/
COPY archlens-gateway/Directory.Build.props ./archlens-gateway/
COPY archlens-gateway/src/ArchLens.Gateway/*.csproj ./archlens-gateway/src/ArchLens.Gateway/

WORKDIR /src/archlens-gateway
RUN dotnet restore src/ArchLens.Gateway/ArchLens.Gateway.csproj

WORKDIR /src
COPY archlens-gateway/ ./archlens-gateway/

WORKDIR /src/archlens-gateway
RUN dotnet publish src/ArchLens.Gateway -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
LABEL org.opencontainers.image.source="https://github.com/archlens-platform/archlens-gateway"
LABEL org.opencontainers.image.title="ArchLens Gateway"
LABEL org.opencontainers.image.version="1.0.0"
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ArchLens.Gateway.dll"]
