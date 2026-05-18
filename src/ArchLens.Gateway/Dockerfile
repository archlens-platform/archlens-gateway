FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY src/ApiGateway/ArchLens.Gateway/ArchLens.Gateway.csproj src/ApiGateway/ArchLens.Gateway/

RUN dotnet restore src/ApiGateway/ArchLens.Gateway/ArchLens.Gateway.csproj

COPY src/ApiGateway/ src/ApiGateway/

RUN dotnet publish src/ApiGateway/ArchLens.Gateway/ArchLens.Gateway.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ArchLens.Gateway.dll"]
