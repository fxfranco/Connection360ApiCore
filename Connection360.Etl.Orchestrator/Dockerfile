# syntax=docker/dockerfile:1.7
# ============================================================================================
# Connection360.Etl.Orchestrator
#   Contexto de build: RAÍZ de la solución.
#     docker build -f Connection360.Etl.Orchestrator/Dockerfile -t <registro>/connection360.etl.orchestrator:<version> .
#   Lo normal es usar deploy/build-images.ps1 (o .sh), que toma la versión del <Version> del .csproj.
#
#   Programa de consola (Exe). Es un proceso de LARGA DURACIÓN que lanza Connection360.Etl.App como proceso hijo
#   (dotnet Connection360.Etl.App.dll), por eso esta imagen EMPAQUETA también el ETL en /app/etl. El ETL
#   hereda las variables de entorno del contenedor, así que su configuración se pasa a este contenedor.
#   Configuración: appsettings.json VACÍO ({}); todo se entrega con variables de entorno al arrancar.
# ============================================================================================
ARG DOTNET_VERSION=10.0

# ---------- ETAPA 1: compilación y publicación ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY . .

# El .csproj de este proyecto referencia appsettings.Development.json como <Content> (copiar a la salida). El
# .dockerignore lo excluye del contexto para no filtrar credenciales, así que se crea vacío para que el build no falle
# (luego se elimina de la publicación).
RUN echo '{}' > Connection360.Etl.Orchestrator/appsettings.Development.json \
 && echo '{}' > Connection360.Etl.App/appsettings.Development.json

ARG APP_VERSION=
# Versión del ETL empaquetado (por defecto la de su .csproj).
ARG ETL_VERSION=
RUN --mount=type=cache,id=connection360-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "Connection360.Etl.Orchestrator/Connection360.Etl.Orchestrator.csproj" \
 && dotnet restore "Connection360.Etl.App/Connection360.Etl.App.csproj" \
 && dotnet publish "Connection360.Etl.Orchestrator/Connection360.Etl.Orchestrator.csproj" \
        -c Release -o /app/publish --no-restore \
        -p:UseAppHost=false ${APP_VERSION:+"-p:Version=${APP_VERSION}"} \
 && dotnet publish "Connection360.Etl.App/Connection360.Etl.App.csproj" \
        -c Release -o /app/publish/etl --no-restore \
        -p:UseAppHost=false ${ETL_VERSION:+"-p:Version=${ETL_VERSION}"}

# appsettings vacío en ambos (orquestador y ETL): se eliminan los de la publicación y se deja un {}.
RUN find /app/publish -maxdepth 2 -name 'appsettings*.json' -delete \
 && echo '{}' > /app/publish/appsettings.json \
 && echo '{}' > /app/publish/etl/appsettings.json

# ---------- ETAPA 2: ejecución (solo runtime de .NET, sin SDK) ----------
FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS final
WORKDIR /app

ARG APP_VERSION=
ARG ETL_VERSION=
LABEL org.opencontainers.image.title="Connection360.Etl.Orchestrator" \
      org.opencontainers.image.version="${APP_VERSION}" \
      com.connection360.etl.app.version="${ETL_VERSION}" \
      org.opencontainers.image.description="Connection360 ETL Orchestrator (programa de consola de larga duración que programa y lanza Connection360.Etl.App por cron)"

ENV DOTNET_ENVIRONMENT=Production \
    Orchestrator__CronJobs__0__Name=Connection360.Etl.App \
    Orchestrator__CronJobs__0__ExecutablePath=dotnet \
    Orchestrator__CronJobs__0__Arguments=Connection360.Etl.App.dll \
    Orchestrator__CronJobs__0__WorkingDirectory=/app/etl \
    Orchestrator__CronJobs__0__Enabled=true

COPY --from=build /app/publish .

# Sin privilegios de root (usuario "app" de la imagen base).
USER $APP_UID
ENTRYPOINT ["dotnet", "Connection360.Etl.Orchestrator.dll"]
