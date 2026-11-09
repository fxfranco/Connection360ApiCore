# syntax=docker/dockerfile:1.7
# ============================================================================================
# Connection360.Etl.App
#   Contexto de build: RAÍZ de la solución.
#     docker build -f Connection360.Etl.App/Dockerfile -t <registro>/connection360.etl.app:<version> .
#   Lo normal es usar deploy/build-images.ps1 (o .sh), que toma la versión del <Version> del .csproj.
#
#   Programa de consola (Exe). Corre UNA vez y termina (código de salida 0 = éxito). Lo normal es que lo lance el Orchestrator;
#   para una corrida manual:  docker compose run --rm etl-app
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
RUN echo '{}' > Connection360.Etl.App/appsettings.Development.json

ARG APP_VERSION=

RUN --mount=type=cache,id=connection360-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "Connection360.Etl.App/Connection360.Etl.App.csproj" \
 && dotnet publish "Connection360.Etl.App/Connection360.Etl.App.csproj" \
        -c Release -o /app/publish --no-restore \
        -p:UseAppHost=false ${APP_VERSION:+"-p:Version=${APP_VERSION}"}

# appsettings vacío: se eliminan los de la publicación (incluido cualquier Development) y se deja un {}.
RUN find /app/publish -maxdepth 1 -name 'appsettings*.json' -delete \
 && echo '{}' > /app/publish/appsettings.json

# ---------- ETAPA 2: ejecución (solo runtime de .NET, sin SDK) ----------
FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS final
WORKDIR /app

ARG APP_VERSION=

LABEL org.opencontainers.image.title="Connection360.Etl.App" \
      org.opencontainers.image.version="${APP_VERSION}" \
      org.opencontainers.image.description="Connection360 ETL (consola: extrae de APIs externas, transforma, carga en PostgreSQL y refresca vistas materializadas)"

ENV DOTNET_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Sin privilegios de root (usuario "app" de la imagen base).
USER $APP_UID
ENTRYPOINT ["dotnet", "Connection360.Etl.App.dll"]
