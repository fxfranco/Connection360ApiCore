# Connection360 – contenedores Docker

## Componentes e imágenes

| Componente | Imagen (Docker Hub) | Tipo | Puerto interno |
|---|---|---|---|
| `Connection360.ApiGateway` | `<registro>/connection360.apigateway:<versión>` | Web (YARP). **Único expuesto al front** | 8080 (HTTP) · 8443 (HTTPS) |
| `Connection360.Api` | `<registro>/connection360.api:<versión>` | Web, solo red interna | 8080 |
| `Connection360Notification.Api` | `<registro>/connection360notification.api:<versión>` | Web (+ SignalR), solo red interna | 8080 |
| `Connection360.Etl.Orchestrator` | `<registro>/connection360.etl.orchestrator:<versión>` | Consola de larga duración (+ ETL empaquetado en `/app/etl`) | – |
| `Connection360.Etl.App` | `<registro>/connection360.etl.app:<versión>` | Consola de corrida única (`docker compose run --rm etl-app`) | – |

`<versión>` es el `<Version>` del `.csproj` de cada proyecto, en minúsculas (`1.0.0-alphaBaseInit.1` → `1.0.0-alphabaseinit.1`). Cada imagen también se etiqueta `latest`.

## Cómo está construida cada imagen
- Dockerfile en la carpeta de cada proyecto; el **contexto de build es la raíz de la solución**, así se copian todas las dependencias (proyectos referenciados). El `.dockerignore` de la raíz deja fuera binarios, pruebas, `appsettings.Development.json`, `*.pfx` y `.env`.
- Multi-etapa: compila con el SDK y ejecuta con el runtime ligero (`aspnet` para las APIs, `runtime` para consola), como usuario **no root**.
- **El `appsettings.json` de la imagen queda vacío (`{}`)** y se eliminan los `appsettings.*.json`. Toda la configuración entra por variables de entorno (`Seccion__Clave`, p. ej. `Jwt__Secret`); `docker-compose.yml` las arma a partir del `.env`.
- **Orchestrator + ETL:** el orquestador lanza el ETL como proceso hijo (`dotnet Connection360.Etl.App.dll`), por eso su imagen incluye el ETL publicado en `/app/etl`. El proceso hijo hereda las variables de entorno del contenedor, así que la configuración del ETL (PostgreSQL, Mongo, APIs externas) se define en el servicio `etl-orchestrator`. La imagen `connection360.etl.app` independiente sirve para corridas manuales o para un scheduler externo.

## Construir y publicar en Docker Hub
```powershell
docker login
./deploy/build-images.ps1 -Push                      # versión = <Version> de cada .csproj
./deploy/build-images.ps1 -Version 1.2.0 -Push       # versión manual para todas
./deploy/build-images.ps1 -Components api,gateway    # solo algunos
./deploy/build-images.ps1 -Platform linux/amd64      # si el hosting es amd64 y construyes en otra arquitectura
```
En Linux/macOS: `./deploy/build-images.sh --push` (mismas opciones: `--version`, `--components`, `--platform`, `--registry`, `--dry-run`).
El script escribe `deploy/versions.env` con las versiones construidas.

## Levantar todo
```bash
docker network create connection360-network          # solo si no existe; la comparten PostgreSQL, MongoDB y Kafka
cp .env.example .env                                  # completar valores (el .env NO se versiona)
mkdir certs && # colocar certs/gateway.pfx (ver TLS)
docker compose --env-file .env --env-file deploy/versions.env up -d
docker compose logs -f etl-orchestrator
docker compose run --rm etl-app                       # corrida manual del ETL (opcional)
```

### Red y contenedores existentes (PostgreSQL / MongoDB / Kafka)
Los contenedores de BD y Kafka deben estar conectados a la red `DOCKER_NETWORK`. En el `.env`, el host de cada cadena de conexión es el **nombre del contenedor/servicio** en esa red (no `localhost`). Para Kafka, `KAFKA_BOOTSTRAP_SERVERS` debe ser el listener que Kafka anuncia **dentro** de la red Docker (p. ej. `kafka:29092`); si anuncia solo `localhost:9092`, los clientes en otros contenedores no podrán conectar.

### TLS del gateway (HTTP y HTTPS)
El gateway atiende `8080` (HTTP) y `8443` (HTTPS). El certificado `.pfx` se **monta** desde `GATEWAY_CERT_DIR` (por defecto `./certs`) y su contraseña va en `GATEWAY_CERT_PASSWORD`; nunca se mete en la imagen. Para pruebas: `dotnet dev-certs https -ep ./certs/gateway.pfx -p <password>` (solo válido para `localhost`); para producción usa un certificado real (Let's Encrypt, etc.).
`GATEWAY_HTTPS_REDIRECT=false` (por defecto) atiende HTTP y HTTPS sin redirigir; con `true` redirige HTTP→HTTPS.
En un hosting que termina TLS en su balanceador, usa solo el puerto HTTP 8080 y, **solo si el gateway queda detrás de ese proxy de confianza**, `GATEWAY_FORWARDED_HEADERS=true`.

### APIs externas del ETL (equipo Windows)
Dentro del contenedor `localhost` es el propio contenedor. Usa la IP/nombre del equipo Windows o `host.docker.internal` (si corren en la misma máquina que Docker). Si esas APIs usan un certificado HTTPS autofirmado (p. ej. el de desarrollo de IIS Express/Kestrel), el contenedor no lo conoce y la llamada fallará por TLS: instala la CA en la imagen/host o expón esas APIs con un certificado de confianza.

**Solo desarrollo – aceptar certificados no válidos:** en el `.env` pon `EXTERNAL_API_ALLOW_INVALID_CERT=true` (se envía como `ExternalApi__AllowInvalidServerCertificate`) y recrea el servicio (`docker compose up -d --force-recreate etl-orchestrator`). El ETL tolera los certificados TLS de desarrollo de las APIs externas (nombre que no coincide, autofirmado, cadena no confiable) pero sigue rechazando los vencidos, revocados o inválidos, y escribe una advertencia en el log al iniciar. Por defecto es `false`; no lo actives en producción. Aplica solo al ETL (`Connection360.Etl.App`).

## Seguridad
- Los secretos (`JWT_SECRET`, cadenas de conexión, contraseña SMTP, Auth0…) van en el `.env` o, en la nube, en el gestor de secretos del hosting. No uses `appsettings.Development.json` con credenciales reales dentro del repositorio.
- `/connect/token` del gateway valida clientes de demostración con secretos fijos en código (`TokenEndpoints.cs`): reemplázalo antes de exponer el gateway a internet.
