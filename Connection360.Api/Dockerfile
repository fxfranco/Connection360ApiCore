# syntax=docker/dockerfile:1.7
# ============================================================================================
# Connection360.Api
#   Contexto de build: RAÍZ de la solución (así se copian TODAS las dependencias del proyecto).
#     docker build -f Connection360.Api/Dockerfile -t <registro>/connection360.api:<version> .
#   Lo normal es usar deploy/build-images.ps1 (o .sh), que toma la versión del <Version> del .csproj.
#
#   Configuración: el appsettings.json de la imagen queda VACÍO ({}). Todo se entrega al arrancar el
#   contenedor con variables de entorno (sección__clave, p. ej. Jwt__Secret) o con un archivo montado.
# ============================================================================================
ARG DOTNET_VERSION=10.0

# ---------- ETAPA 1: compilación y publicación ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY . .

# Si se pasa APP_VERSION se usa en lugar del <Version> del .csproj (versión manual).
ARG APP_VERSION=
# restore + publish en el mismo RUN para reutilizar la caché de NuGet entre builds (BuildKit).
RUN --mount=type=cache,id=connection360-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "Connection360.Api/Connection360.Api.csproj" \
 && dotnet publish "Connection360.Api/Connection360.Api.csproj" -c Release -o /app/publish --no-restore \
        -p:UseAppHost=false ${APP_VERSION:+-p:Version=${APP_VERSION}}

# appsettings vacío: se eliminan los de la publicación (incluido cualquier Development) y se deja un {}.
RUN find /app/publish -maxdepth 1 -name 'appsettings*.json' -delete \
 && echo '{}' > /app/publish/appsettings.json

# ---------- ETAPA 2: ejecución (solo runtime de ASP.NET, sin SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

ARG APP_VERSION=
LABEL org.opencontainers.image.title="Connection360.Api" \
      org.opencontainers.image.version="${APP_VERSION}" \
      org.opencontainers.image.description="Connection360 API Core (hexagonal: application, domain, infrastructure)"

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

# Sin privilegios de root (usuario "app" de la imagen base).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Connection360.Api.dll"]
