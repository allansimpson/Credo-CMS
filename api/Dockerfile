# Phase 6 S19 — API container for the local deployment dry-run.
# Multi-stage build: SDK builds + publishes; runtime image is the
# minimal aspnet/runtime layer.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.sln ./
COPY *.slnx ./
COPY CredoCms.Domain/*.csproj CredoCms.Domain/
COPY CredoCms.Application/*.csproj CredoCms.Application/
COPY CredoCms.Infrastructure/*.csproj CredoCms.Infrastructure/
COPY CredoCms.Api/*.csproj CredoCms.Api/
RUN dotnet restore CredoCms.Api/CredoCms.Api.csproj

COPY CredoCms.Domain/ CredoCms.Domain/
COPY CredoCms.Application/ CredoCms.Application/
COPY CredoCms.Infrastructure/ CredoCms.Infrastructure/
COPY CredoCms.Api/ CredoCms.Api/

RUN dotnet publish CredoCms.Api/CredoCms.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "CredoCms.Api.dll"]
