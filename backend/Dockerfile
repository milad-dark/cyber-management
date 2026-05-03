FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY CyberManagement.sln .
COPY src/CyberManagement.Api/CyberManagement.Api.csproj src/CyberManagement.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/CyberManagement.Api/CyberManagement.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install tools needed at runtime
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CyberManagement.Api.dll"]
