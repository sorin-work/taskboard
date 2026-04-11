# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between code changes.
COPY Directory.Build.props .editorconfig ./
COPY src/Taskboard.Api/Taskboard.Api.csproj src/Taskboard.Api/
RUN dotnet restore src/Taskboard.Api/Taskboard.Api.csproj

COPY src/ src/
RUN dotnet publish src/Taskboard.Api/Taskboard.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# The aspnet image already listens on 8080 (ASPNETCORE_HTTP_PORTS).
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
EXPOSE 8080

# Non-root user that ships with the aspnet image.
USER app
ENTRYPOINT ["dotnet", "Taskboard.Api.dll"]
