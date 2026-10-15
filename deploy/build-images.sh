#!/usr/bin/env bash
# Construye (y opcionalmente publica en Docker Hub) las imágenes de Connection360.
#   Versión de cada imagen = <Version> de su .csproj, salvo que se indique --version (manual, para todas).
#
#   ./deploy/build-images.sh                         # construye los 5 componentes
#   ./deploy/build-images.sh --push                  # construye y publica en Docker Hub (requiere docker login)
#   ./deploy/build-images.sh --registry miusuario --components api,gateway
#   ./deploy/build-images.sh --version 1.2.0 --platform linux/amd64
#   ./deploy/build-images.sh --dry-run               # solo muestra qué haría
set -euo pipefail

REGISTRY="fxfranco13"; PUSH=0; DRY=0; VERSION_OVERRIDE=""; PLATFORM=""; NO_LATEST=0
COMPONENTS="gateway,api,notification,etl-app,etl-orchestrator"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --registry)   REGISTRY="$2"; shift 2;;
    --push)       PUSH=1; shift;;
    --dry-run)    DRY=1; shift;;
    --version)    VERSION_OVERRIDE="$2"; shift 2;;
    --platform)   PLATFORM="$2"; shift 2;;
    --components) COMPONENTS="$2"; shift 2;;
    --no-latest)  NO_LATEST=1; shift;;
    -h|--help)    sed -n '2,10p' "$0"; exit 0;;
    *) echo "Argumento desconocido: $1" >&2; exit 2;;
  esac
done

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# clave | proyecto | imagen | variable de versión
declare -A PROJECT=( [gateway]=Connection360.ApiGateway [api]=Connection360.Api [notification]=Connection360Notification.Api [etl-app]=Connection360.Etl.App [etl-orchestrator]=Connection360.Etl.Orchestrator )
declare -A IMAGE=( [gateway]=connection360.apigateway [api]=connection360.api [notification]=connection360notification.api [etl-app]=connection360.etl.app [etl-orchestrator]=connection360.etl.orchestrator )
declare -A VARNAME=( [gateway]=APIGATEWAY_VERSION [api]=API_VERSION [notification]=NOTIFICATION_API_VERSION [etl-app]=ETL_APP_VERSION [etl-orchestrator]=ETL_ORCHESTRATOR_VERSION )

csproj_version() { sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$1/$1.csproj" | head -n1 | tr -d '\r'; }
tag_of() { echo "$1" | tr 'A-Z' 'a-z' | tr '+' '-'; }   # un tag de Docker no admite '+'
run() { echo "+ $*"; [[ $DRY -eq 1 ]] || "$@"; }

VERSIONS_FILE="$ROOT/deploy/versions.env"
[[ $DRY -eq 1 ]] || touch "$VERSIONS_FILE"   # se conservan las variables de los componentes no construidos
declare -a SUMMARY=()

IFS=',' read -ra LIST <<< "$COMPONENTS"
for key in "${LIST[@]}"; do
  [[ -n "${PROJECT[$key]:-}" ]] || { echo "Componente desconocido: $key (válidos: ${!PROJECT[*]})" >&2; exit 2; }
  proj="${PROJECT[$key]}"
  version="${VERSION_OVERRIDE:-$(csproj_version "$proj")}"
  [[ -n "$version" ]] || { echo "No se encontró <Version> en $proj/$proj.csproj (usa --version)." >&2; exit 1; }
  tag="$(tag_of "$version")"
  ref="$REGISTRY/${IMAGE[$key]}"

  args=(docker build -f "$proj/Dockerfile" -t "$ref:$tag" --build-arg "APP_VERSION=$version")
  [[ $NO_LATEST -eq 1 ]] || args+=(-t "$ref:latest")
  [[ -z "$PLATFORM" ]] || args+=(--platform "$PLATFORM")
  if [[ "$key" == "etl-orchestrator" ]]; then
    etl_version="${VERSION_OVERRIDE:-$(csproj_version Connection360.Etl.App)}"
    args+=(--build-arg "ETL_VERSION=$etl_version")
  fi
  # Con un builder docker-container el resultado no queda en las imágenes locales a menos que se pida:
  # --push construye y sube en un solo paso; --load lo deja disponible en "docker images".
  if [[ $PUSH -eq 1 ]]; then args+=(--push); else args+=(--load); fi
  args+=(.)

  echo; echo "=== $proj  ->  $ref:$tag"
  DOCKER_BUILDKIT=1 run "${args[@]}"
  if [[ $DRY -eq 0 ]]; then
    sed -i "/^${VARNAME[$key]}=/d" "$VERSIONS_FILE"
    echo "${VARNAME[$key]}=$tag" >> "$VERSIONS_FILE"
  fi
  SUMMARY+=("$proj|$ref:$tag")
done

echo; echo "Resumen:"; printf '  %s\n' "${SUMMARY[@]//|/  ->  }"
[[ $DRY -eq 1 ]] || echo "Versiones escritas en deploy/versions.env (úsalas con: docker compose --env-file .env --env-file deploy/versions.env up -d)"
