# Connection360ApiCore

Backend de la aplicación **Conexión 360**: plataforma de trazabilidad de envíos de comercio exterior. Expone una API de negocio (inicio, mis envíos, reportes, configuración y administración de usuarios), un **API Gateway** con autenticación JWT, un servicio de **notificaciones** multicanal (tiempo real, correo y SMS), un **ETL** programado que consolida la información de sistemas externos y un módulo transversal de **observabilidad** (logs, métricas y trazas).

> Solución en **.NET 10** con **arquitectura hexagonal** (Domain / Application / Infrastructure), principios **SOLID**, patrón **Outbox** sobre **Kafka**, y pruebas unitarias y de integración en cada componente.

---

## Tabla de contenido

1. [Descripción general](#1-descripción-general)
2. [Características y funcionalidades](#2-características-y-funcionalidades)
3. [Arquitectura](#3-arquitectura)
4. [Tecnologías usadas](#4-tecnologías-usadas)
5. [Estructura de la solución](#5-estructura-de-la-solución)
6. [Información de los proyectos](#6-información-de-los-proyectos)
7. [Proyectos de pruebas](#7-proyectos-de-pruebas)
8. [Requisitos](#8-requisitos)
9. [Instalación y ejecución local](#9-instalación-y-ejecución-local)
10. [Configuración](#10-configuración)
11. [Ejecución con Docker](#11-ejecución-con-docker)
12. [Despliegue en AWS con Terraform](#12-despliegue-en-aws-con-terraform)
13. [CI/CD](#13-cicd)
14. [Endpoints principales](#14-endpoints-principales)
15. [Notas técnicas relevantes](#15-notas-técnicas-relevantes)
16. [Seguridad](#16-seguridad)
17. [Solución de problemas](#17-solución-de-problemas)
18. [Documentación adicional](#18-documentación-adicional)

---

## 1. Descripción general

Connection360 centraliza la información de los envíos (embarques, estados, hitos, documentos y alertas) que provienen de varios sistemas externos (BPMS, Datalogs, SIM, OpenComex, Asiscomex, SystemCarrier) y la entrega a los clientes y colaboradores mediante una API segura, notificaciones en tiempo real y reportes.

Componentes en ejecución:

| Componente | Rol |
|---|---|
| **API Gateway** | Único punto de entrada para el front. Autentica con JWT, aplica CORS y *rate limiting*, y enruta con YARP hacia las APIs internas. |
| **API core** (`Connection360.Api`) | Lógica de negocio: envíos, reportes, configuración, usuarios (Auth0) y publicación de eventos mediante Outbox → Kafka. |
| **API de notificaciones** (`Connection360Notification.Api`) | Consume eventos de Kafka, persiste las notificaciones en MongoDB y las entrega por SignalR, correo (SMTP) y SMS (Brevo). |
| **ETL Orchestrator + ETL App** | Ejecuta periódicamente (cron) la extracción desde las APIs externas hacia PostgreSQL, con seguimiento de cambios y alertas. |
| **Observabilidad** | Librerías compartidas que registran logs, métricas y trazas de todos los componentes en MongoDB. |

## 2. Características y funcionalidades

**Negocio**
- Panel de inicio con totales y filtros por cliente/estado.
- Mis envíos: listado, filtros, histórico y detalle de cada envío.
- Reportes de inicio.
- Configuración de notificaciones por usuario (canales: correo, SMS, tiempo real) y configuración maestra (solo administradores).
- Administración de usuarios y roles: listar, consultar, actualizar, activar/inactivar y eliminar (Auth0).
- Aprovisionamiento de clientes, colaboradores y relación cliente–colaborador (creación y listados).
- Roles soportados: `ADMIN`, `CLIENT`, `ANALISTAOPE`, `ANALISTASAC`.

**Notificaciones**
- Consumo de eventos desde Kafka y persistencia en MongoDB.
- Entrega por **SignalR** (tiempo real), **correo** (MailKit) y **SMS** (Brevo), respetando las preferencias de canal de cada usuario.
- Alertas del ETL: se guarda **una copia por administrador** (con su `ClientId`); si no hay administradores, se conserva el mensaje original con `ClientId` vacío.
- Marcar notificaciones como leídas y consultar el historial.
- Plantilla HTML de correo configurable.

**ETL**
- Ejecución programada por expresión **cron** con zona horaria configurable (Colombia por defecto).
- Control de trabajos (`etl_job_control`) con retención configurable y tiempo máximo de ejecución.
- Seguimiento de cambios (*change tracking*) y refresco de **vistas materializadas**.
- Sanitización de errores (no se filtran tokens/JWT en logs ni alertas) y **alertas** por fallo al equipo administrador.
- Paginación opcional y timeouts configurables hacia las APIs externas.

**Plataforma**
- **Patrón Outbox**: los eventos se guardan en `outbox_messages` dentro de la misma transacción y `OutboxPublisherWorker` los publica en Kafka.
- **Sincronización de usuarios Auth0 → MongoDB** (`Auth0UserSyncWorker`) con manejo de *rate limit* (429) y *timeouts* IPv6.
- **Caché de coordenadas** (MemoryCache) para consultas a OpenStreetMap/Nominatim, con límite de tamaño, expiración y evicción configurables.
- **Observabilidad nativa** (sin dependencias externas): logs, métricas (`Meter`) y trazas (`ActivitySource`) escritas por lotes en MongoDB.
- Versionado de API (`/api/v1/...`), Swagger/OpenAPI, validación con FluentValidation y *health checks* (`/health`).
- Hora de Colombia centralizada (abstracción de tiempo en Domain).

## 3. Arquitectura

```
                    Front (web)
                        │  HTTPS / JWT
                ┌───────▼────────┐
                │  API Gateway   │  YARP · JWT · CORS · Rate limit
                └───┬────────┬───┘
        /api/**     │        │  /api/v1/notifications/** y /api/v1/hubs/notifications (SignalR)
          ┌─────────▼──┐  ┌──▼─────────────────┐
          │ API core   │  │ API Notificaciones │ ── SignalR · SMTP · Brevo SMS
          └──┬──────┬──┘  └──┬─────────────┬───┘
   Outbox    │      │        │ consume     │
 ┌───────────▼┐  ┌──▼────────▼──┐     ┌────▼─────┐
 │ PostgreSQL │  │    Kafka     │     │ MongoDB  │ (notificaciones, usuarios básicos, observabilidad)
 └───────▲────┘  └──────────────┘     └──────────┘
         │ carga (cron)
 ┌───────┴────────────┐        APIs externas: BPMS · Datalogs · SIM · OpenComex · Asiscomex · SystemCarrier
 │ ETL Orchestrator   │──────► ETL App (proceso hijo)
 └────────────────────┘
```

Cada componente sigue la **arquitectura hexagonal**:

- **Domain**: entidades, objetos de valor, excepciones, puertos (interfaces) y reglas de negocio. Sin dependencias externas.
- **Application**: casos de uso (`UseCases`), DTOs y puertos de entrada/salida; depende solo de Domain.
- **Infrastructure**: adaptadores de salida (PostgreSQL/Dapper, MongoDB, Kafka, Auth0, correo, SMS, APIs externas, caché) y registro de dependencias.
- **Api / App** (capa de entrada): controladores, *middlewares*, filtros, autenticación y composición (`Program.cs`).

## 4. Tecnologías usadas

| Área | Tecnología |
|---|---|
| Plataforma | .NET 10 (`net10.0`), ASP.NET Core, Worker Services (`Microsoft.Extensions.Hosting`) |
| API Gateway | YARP (`Yarp.ReverseProxy`), JWT Bearer, rate limiting, CORS |
| Documentación API | Swashbuckle / OpenAPI, `Asp.Versioning.Mvc` |
| Validación | FluentValidation |
| Base de datos relacional | PostgreSQL · Npgsql · Dapper |
| Base de datos documental | MongoDB (`MongoDB.Driver`) |
| Mensajería | Apache Kafka (`Confluent.Kafka`), patrón Outbox |
| Identidad | Auth0 (JWT + Management API `Auth0.ManagementApi`) |
| Notificaciones | SignalR, MailKit (SMTP), Brevo (SMS) |
| Geolocalización | OpenStreetMap / Nominatim |
| Caché | `Microsoft.Extensions.Caching.Memory` |
| Observabilidad | Implementación propia (logs, métricas, trazas) → MongoDB |
| Contenedores | Docker, Docker Compose (imágenes por componente) |
| Infraestructura como código | Terraform (AWS Academy: VPC, EC2, EBS, ALB, NAT) |
| CI/CD | GitHub Actions, Gitleaks, SonarCloud, Docker Hub |
| Pruebas | xUnit 2.9, Moq 4.20, FluentAssertions 6.12, `Microsoft.AspNetCore.Mvc.Testing`, coverlet |

## 5. Estructura de la solución

La solución es `Connection360ApiCore.slnx`.

```
Connection360ApiCore/
├── Connection360.Api/                       API core (entrada)
├── Connection360.Application/               Casos de uso y puertos del core
├── Connection360.Domain/                    Dominio del core
├── Connection360.Infrastructure/            Adaptadores del core (PostgreSQL, Kafka, Auth0, caché, APIs externas)
├── Connection360.ApiGateway/                API Gateway (YARP + JWT)
├── Connection360Notification.Api/           API de notificaciones (+ SignalR)
├── Connection360Notification.Application/   Casos de uso de notificaciones
├── Connection360Notification.Domain/        Dominio de notificaciones
├── Connection360Notification.Infrastructure/ MongoDB, Kafka, SignalR, correo, SMS
├── Connection360.Etl.Orchestrator/          Orquestador (cron) del ETL
├── Connection360.Etl.App/                   Aplicación de consola del ETL
├── Connection360.Etl.Application/           Casos de uso del ETL
├── Connection360.Etl.Domain/                Dominio del ETL
├── Connection360.Etl.Infrastructure/        Adaptadores del ETL (APIs externas, PostgreSQL)
├── Connection360.Observability.Domain/      Modelo de observabilidad
├── Connection360.Observability.Application/ Puertos y servicios de observabilidad
├── Connection360.Observability.AspNetCore/  Integración con ASP.NET Core
├── Connection360.Observability.Infrastructure.Mongo/  Persistencia en MongoDB por lotes
├── *.Tests / *.Test / *.IntegrationTests    Proyectos de pruebas (ver sección 7)
├── Documents/                               Scripts SQL, Swagger, ejemplos de appsettings y .bat de arranque
├── deploy/                                  Scripts para construir/publicar imágenes Docker
├── docker-compose.yml, .env.example         Despliegue con Docker Compose
└── .gitleaksignore, .dockerignore           Configuración de seguridad y contexto Docker
```

## 6. Información de los proyectos

### Aplicaciones ejecutables

| Proyecto | Tipo | Descripción |
|---|---|---|
| `Connection360.ApiGateway` | Web API (YARP) | Punto de entrada. Endpoint `/connect/token`, validación JWT, CORS, *rate limiting*, *health check* `/health`. Rutas: `/api/v{n}/hubs/notifications/**` (SignalR) y `/api/v{n}/notifications/**` hacia notificaciones, y `/api/**` hacia la API core. |
| `Connection360.Api` | Web API | Controladores `home`, `myshipments`, `reports` y `settings`. Incluye `OutboxPublisherWorker` y `Auth0UserSyncWorker`. |
| `Connection360Notification.Api` | Web API + SignalR | Controlador `notifications` y hub `api/v{n}/hubs/notifications`. Consume Kafka y entrega por SignalR, correo y SMS. |
| `Connection360.Etl.Orchestrator` | Consola (worker) | Servicio de larga duración que lanza `Etl.App` como proceso hijo según el cron configurado. |
| `Connection360.Etl.App` | Consola | Ejecuta una corrida del ETL (extracción, carga, *change tracking*, refresco de vistas, alertas). |

### Capas de dominio / aplicación / infraestructura

| Proyecto | Contenido principal |
|---|---|
| `Connection360.Domain` | Entidades, enums, DTOs de dominio, excepciones, puertos, servicios, `Settings` y abstracción de hora de Colombia. |
| `Connection360.Application` | Casos de uso, DTOs, enums, puertos y servicios de aplicación. |
| `Connection360.Infrastructure` | `Adapters` (PostgreSQL/Dapper, Auth0), `Caching` (coordenadas), `ExternalApi` (OpenStreetMap), `Messaging` (Outbox y sincronización Auth0), `Persistence`, `Mappers`, `DependencyInjection`. |
| `Connection360Notification.Domain` | `NotificationMessage`, preferencias de canal, datos básicos de usuario y roles, puertos y settings. |
| `Connection360Notification.Application` | Casos de uso (p. ej. `ProcessIncomingNotificationUseCase`), DTOs, mapeos y servicios. |
| `Connection360Notification.Infrastructure` | Adaptadores MongoDB, consumidor Kafka, SignalR, `Email` (MailKit), `Sms` (Brevo). |
| `Connection360.Etl.Domain` / `Etl.Application` / `Etl.Infrastructure` | Entidades y constantes del ETL, casos de uso y puertos, y adaptadores hacia las APIs externas y PostgreSQL. |
| `Connection360.Observability.*` | `Domain` (modelo), `Application` (puertos), `AspNetCore` (middlewares/telemetría HTTP) e `Infrastructure.Mongo` (escritor por lotes `BatchingTelemetryWriter`). |

## 7. Proyectos de pruebas

Todas las pruebas usan **xUnit**, **Moq** y **FluentAssertions**; las de integración usan además `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) y todas recolectan cobertura con **coverlet**.

### Pruebas unitarias

| Proyecto | Qué valida |
|---|---|
| `Connection360.Domain.Tests` | Entidades, objetos de valor y reglas de dominio del core. |
| `Connection360.Application.Tests` | Casos de uso y servicios de aplicación del core. |
| `Connection360.Infrastructure.Tests` | Adaptadores: Outbox, sincronización Auth0 (429, IPv4-first), caché de coordenadas, mapeos y persistencia. |
| `Connection360.Api.Tests` | Controladores, filtros y *middlewares* de la API core. |
| `Connection360.ApiGateway.Tests` | Token endpoint, autenticación y configuración del gateway. |
| `Connection360Notification.Domain.Test` | Dominio de notificaciones (`NotificationMessage`, preferencias, etc.). |
| `TestProject1Connection360Notification.Application.Test` | Casos de uso de notificaciones (p. ej. `ProcessIncomingNotificationUseCase`: alertas por administrador). |
| `Connection360Notification.Infrastructure.Test` | MongoDB, Kafka, correo y SMS (con dobles). |
| `Connection360Notification.Api.Test` | Controladores y hub de notificaciones. |
| `Connection360.Etl.Domain.Test` | Dominio del ETL. |
| `Connection360.Etl.Application.Test` | Casos de uso del ETL, sanitización de errores (`EtlErrorSanitizer`) y alertas. |
| `Connection360.Etl.Infrastructure.Test` | Clientes de APIs externas y repositorios del ETL. |
| `Connection360.Etl.App.Test` | Composición y ejecución de la aplicación de consola del ETL. |
| `Connection360.Etl.Orchestrator.Test` | Programación cron y ejecución del proceso hijo. |
| `Connection360.Observability.Domain.Test` | Modelo de observabilidad. |
| `Connection360.Observability.Application.Test` | Servicios y puertos de observabilidad. |
| `Connection360.Observability.AspNetCore.Test` | Telemetría HTTP y *middlewares*. |
| `Connection360.Observability.Infrastructure.Mongo.Test` | Escritor por lotes y persistencia Mongo. |

### Pruebas de integración

| Proyecto | Qué valida |
|---|---|
| `Connection360.Api.IntegrationTests` | La API core levantada en memoria: autenticación, autorización por rol, caché de coordenadas (con Nominatim simulado), arranque del host. |
| `Connection360.ApiGateway.IntegrationTests` | Gateway completo: emisión de token, JWT, rutas YARP, CORS y *rate limiting*. |
| `Connection360Notification.Api.IntegrationTests` | API de notificaciones: host, flujo de notificación entrante y alertas (guardado por administrador y entrega por los tres canales). |
| `Connection360.Etl.App.IntegrationTests` | Aplicación ETL con dependencias reemplazadas. |
| `Connection360.Etl.Orchestrator.IntegrationTests` | Orquestador y lanzamiento del proceso hijo. |

> Las pruebas de integración **no requieren** PostgreSQL, MongoDB ni Kafka reales: reemplazan los adaptadores externos por dobles en la `WebApplicationFactory`.

### Ejecutar las pruebas

```bash
# Toda la solución
dotnet test Connection360ApiCore.slnx

# Un proyecto
dotnet test Connection360.Infrastructure.Tests

# Filtrar por nombre de clase o prueba
dotnet test --filter "FullyQualifiedName~OutboxPublisherWorkerTests"

# Solo integración
dotnet test Connection360.Api.IntegrationTests
dotnet test Connection360Notification.Api.IntegrationTests

# Con cobertura (genera coverage.cobertura.xml en TestResults/)
dotnet test Connection360ApiCore.slnx --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Notas para escribir pruebas: las que tocan `Meter`/`ActivitySource` estáticos se agrupan en una `[Collection]` de xUnit para no ejecutarse en paralelo, y cada prueba de integración que cambia configuración usa su **propia** instancia de `WebApplicationFactory` (no `WithWebHostBuilder`).

## 8. Requisitos

**Para compilar y ejecutar localmente**
- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --version` ≥ 10.0).
- PostgreSQL (esquema y vistas en `Documents/*.sql`).
- MongoDB.
- Apache Kafka (con Zookeeper o modo KRaft).
- Cuenta de **Auth0** con una aplicación M2M autorizada para la Management API.
- Credenciales de las APIs externas del ETL (BPMS, Datalogs, SIM, OpenComex, Asiscomex, SystemCarrier) — solo para el ETL.
- Opcional: servidor SMTP (correo) y cuenta de **Brevo** (SMS).
- IDE recomendado: Visual Studio 2022+ / Rider / VS Code con C# Dev Kit.

**Para contenedores y nube (opcional)**
- Docker y Docker Compose.
- Terraform ≥ 1.x y AWS CLI (laboratorio AWS Academy) para el despliegue.
- Git.

## 9. Instalación y ejecución local

1. **Clonar** el repositorio y restaurar:

   ```bash
   git clone <url-del-repositorio>
   cd Connection360ApiCore
   dotnet restore Connection360ApiCore.slnx
   dotnet build Connection360ApiCore.slnx
   ```

2. **Preparar las bases de datos.** Ejecutar en PostgreSQL los scripts de `Documents/` (tablas `outbox_messages`, `etl_job_control`, logs, canales de notificación por cliente/colaborador, `customers`, vistas `vw_*`). Crear la base de datos de MongoDB y las colecciones configuradas.

3. **Levantar Kafka** y crear el tópico configurado (`Kafka:Topic`).

4. **Configurar** cada proyecto (ver [sección 10](#10-configuración)). Para desarrollo use `appsettings.Development.json` o *User Secrets*; hay ejemplos en `Documents/api`, `Documents/apigateway`, `Documents/apinotifications` y `Documents/orquestrator`. **No suba credenciales reales al repositorio.**

5. **Ejecutar los componentes** (en terminales distintas, en este orden recomendado):

   ```bash
   dotnet run --project Connection360Notification.Api
   dotnet run --project Connection360.Api
   dotnet run --project Connection360.ApiGateway
   dotnet run --project Connection360.Etl.Orchestrator   # opcional: ETL programado
   ```

   Los puertos locales están en `Properties/launchSettings.json` de cada proyecto. En Windows también puede usar `Documents/StartApis_Connection360.bat`, `Documents/StartOrchestratorEtl_Connection360.bat` y `Documents/StopApis_Connection360.bat`.

6. **Probar**: `GET /health` en cada servicio, Swagger en cada API (entorno Development) y los archivos `*.http` de cada proyecto. El front debe consumir **solo el gateway**.

7. **Ejecutar una corrida manual del ETL** (opcional):

   ```bash
   dotnet run --project Connection360.Etl.App
   ```

## 10. Configuración

En local la configuración sale de `appsettings*.json` y *User Secrets*; en Docker/AWS **todo entra por variables de entorno** con el formato `Seccion__Clave` (p. ej. `Jwt__Secret`). El archivo `.env.example` lista todas las variables; cópielo a `.env` y complételo (el `.env` **no se versiona**).

| Grupo | Variables (prefijo `.env`) | Descripción |
|---|---|---|
| Imágenes | `REGISTRY`, `*_VERSION`, `DOCKER_NETWORK` | Registro Docker Hub, versiones de imagen y red compartida. |
| Gateway | `GATEWAY_HTTP_PORT`, `GATEWAY_HTTPS_PORT`, `GATEWAY_CERT_*`, `GATEWAY_HTTPS_REDIRECT`, `CORS_ORIGIN_*`, `CORE_API_URL`, `NOTIFICATION_API_URL`, `RATE_LIMIT_*` | Puertos, TLS, CORS, destinos de proxy y límite de peticiones. |
| JWT | `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_ROLES`, `JWT_ACCESS_TOKEN_MINUTES`, `JWT_REFRESH_TOKEN_DAYS` | Emisión y validación de tokens. |
| Datos | `POSTGRES_CONNECTION`, `MONGO_CONNECTION`, `MONGO_DATABASE`, `MONGO_NOTIFICATION_COLLECTION`, `MONGO_BASIC_USER_COLLECTION` | Cadenas de conexión y colecciones. |
| Kafka / Outbox | `KAFKA_BOOTSTRAP_SERVERS`, `KAFKA_GROUP_ID`, `KAFKA_TOPIC`, `OUTBOX_POLLING_INTERVAL_SECONDS` | Mensajería y frecuencia de publicación. |
| Auth0 | `AUTH0_DOMAIN`, `AUTH0_CLIENT_ID`, `AUTH0_CLIENT_SECRET` | Management API (M2M). |
| Sincronización de usuarios | `USER_SYNC_ENABLED`, `USER_SYNC_TOPIC`, `USER_SYNC_INTERVAL_MINUTES`, `USER_SYNC_BATCH_SIZE`, `USER_SYNC_STARTUP_RETRY_SECONDS`, `USER_SYNC_GROUP_ID` | Worker Auth0 → MongoDB. **Habilítelo en una sola instancia a la vez.** |
| Caché de coordenadas | `OPENSTREETMAP_*`, `COORDINATES_CACHE_*` | Nominatim y política de la caché. |
| Observabilidad | `OBSERVABILITY_ENABLED` | Activa logs/métricas/trazas hacia MongoDB. |
| Correo | `SMTP_*` | Servidor, remitente, destinatario, timeout y plantilla. |
| SMS | `BREVO_SMS_*` | API Brevo, remitente y código de país por defecto. |
| ETL | `ETL_ENABLED`, `ETL_CRON`, `ETL_TIMEZONE`, `ETL_TIMEOUT_MINUTES`, `ETL_JOB_CONTROL_RETENTION_DAYS`, `ETL_VIEWS_*`, `ETL_SYSTEM_USER` | Programación y control del ETL. |
| APIs externas | `EXTERNAL_API_*`, `BPMS_*`, `DATALOGS_*`, `SIM_*`, `OPENCOMEX_*`, `ASISCOMEX_*`, `SYSTEMCARRIER_*` | URL base y API key de cada sistema. |

## 11. Ejecución con Docker

Cada componente tiene su `Dockerfile` (multi-etapa, usuario no root, `appsettings.json` vacío; todo se configura por variables de entorno).

| Servicio (compose) | Imagen | Exposición |
|---|---|---|
| `api-gateway` | `connection360.apigateway` | **8080** HTTP / **8443** HTTPS (único expuesto) |
| `api-connection360` | `connection360.api` | Solo red interna |
| `api-notification` | `connection360notification.api` | Solo red interna (+ SignalR) |
| `etl-orchestrator` | `connection360.etl.orchestrator` | Sin puertos (incluye el ETL en `/app/etl`) |
| `etl-app` | `connection360.etl.app` | Corrida única manual |

```bash
# 1. Red compartida con PostgreSQL, MongoDB y Kafka (si no existe)
docker network create connection360-network

# 2. Configuración
cp .env.example .env            # completar valores
mkdir -p certs                   # colocar certs/gateway.pfx (dotnet dev-certs https -ep ./certs/gateway.pfx -p <clave>)

# 3. Construir y publicar imágenes (opcional)
./deploy/build-images.ps1 -Push      # Windows  (Linux/macOS: ./deploy/build-images.sh --push)

# 4. Levantar
docker compose --env-file .env --env-file deploy/versions.env up -d
docker compose logs -f etl-orchestrator
docker compose run --rm etl-app      # corrida manual del ETL
```

Detalles (TLS, red, APIs externas con certificados autofirmados, `host.docker.internal`) en [`deploy/README.md`](deploy/README.md).

## 12. Despliegue en AWS con Terraform

`https://github.com/fxfranco/Connection360IaC` despliega las imágenes en **AWS Academy** usando solo servicios básicos (VPC, EC2, EBS, ALB, NAT): `gateway` (subred pública), `api`, `notification`, `etl` (subredes privadas) y `data` (PostgreSQL, MongoDB, Zookeeper, Kafka y migraciones con disco EBS persistente). Las IP privadas son fijas y el acceso SSH es únicamente desde su IP hacia el gateway, que actúa de bastión.

```bash
terraform init
terraform plan  -var-file=environments/dev.tfvars
terraform apply -var-file=environments/dev.tfvars
terraform destroy -var-file=environments/dev.tfvars  # al terminar el laboratorio
```

Notas: el laboratorio se apaga cada 4 horas (las instancias conservan IP privada y datos); si pierde acceso por SSH revise que `my_ip` coincida con su IP pública actual. Más detalle en [`https://github.com/fxfranco/Connection360IaC/README.md`](https://github.com/fxfranco/Connection360IaC/README.md).

## 13. CI/CD

Los workflows de GitHub Actions están en `ci-workflows/` (cópielos a `.github/workflows/` para activarlos):

| Workflow | Propósito |
|---|---|
| `Connection360-CI-Fases.yml` | Workflow reutilizable con las fases DevSecOps: **Gitleaks** (detección de secretos), pruebas unitarias con cobertura, análisis de dependencias (SCA), pruebas de integración, **SonarCloud** y *Quality Gate*. |
| `Connection360-CI-Develop.yml` | CI para la rama `develop`. |
| `Connection360-CI-release.yml` | CI para ramas/etiquetas de release. |
| `Connection360-CD-release.yml` | CD: construye y publica las imágenes en **Docker Hub**. |
| `https://github.com/fxfranco/Connection360IaC` | CD de infraestructura: despliegue (`apply`) y destrucción (`destroy`) en AWS. |
| `main-fixed/` | Variantes de los workflows para la rama principal. |

Los secretos (`SONAR_TOKEN`, credenciales de Docker Hub, etc.) se definen en *Settings → Secrets* del repositorio. Los falsos positivos de Gitleaks se registran por huella en `.gitleaksignore`.

## 14. Endpoints principales

Todas las rutas van detrás del gateway y requieren `Authorization: Bearer <JWT>` salvo `/connect/token` y `/health`. Versión actual: `v1`.

**Gateway**

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/connect/token` | Emite el token de acceso. |
| GET | `/health` | Estado del servicio. |

**API core** — `/api/v1/...`

| Recurso | Endpoints | Roles |
|---|---|---|
| `home` | `GET totals`, `GET filters` | ADMIN, CLIENT, ANALISTAOPE, ANALISTASAC |
| `myshipments` | `GET allshipments`, `filterShipments`, `allhistory`, `filterhistory`, `detailsshipments` | ADMIN, CLIENT, ANALISTAOPE, ANALISTASAC |
| `reports` | `GET home` | ADMIN, CLIENT, ANALISTAOPE, ANALISTASAC |
| `settings` (usuario) | `GET viewnotifications`, `POST createnotifications`, `PATCH updatenotifications` | ADMIN, CLIENT, ANALISTAOPE, ANALISTASAC |
| `settings` (administración) | `GET viewmaster`, `POST createmaster`, `PATCH updatemaster`, `GET listusers`, `GET getuser`, `PATCH updateuser/{userId}`, `GET listroles`, `PATCH updateuserstatus/{userId}`, `DELETE deleteuser/{userId}` | ADMIN |
| `settings` (aprovisionamiento) | `GET createcustomerdb`, `createcollaboratordb`, `createcustomercollaboratordb`, `listcustomersdb`, `listcollaboratorsdb`, `listcustomercollaboratordb` | ADMIN |

**API de notificaciones** — `/api/v1/notifications/...`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `allnotifications` | Notificaciones del usuario. |
| PATCH | `readnotification/{idClient}/{idNotification}` | Marca una notificación como leída. |
| WS | `/api/v1/hubs/notifications` | Hub SignalR de notificaciones en tiempo real (token por query string). |

La especificación completa está en `Documents/swaggerConnection360ApiCore.json` y `Documents/swaggerConnection360ApiNotifications.json`.

## 15. Notas técnicas relevantes

- **Outbox**: `outbox_messages` (PostgreSQL) → `OutboxPublisherWorker` → Kafka, con intervalo configurable; garantiza entrega *al menos una vez* sin perder eventos.
- **Sincronización Auth0**: usa un `ManagementClient` compartido y consulta los roles por membresía (`GET /roles` + `GET /roles/{id}/users`) en vez de una llamada por usuario para no agotar el *rate limit* de Auth0 (429 `Global limit has been reached`). Ante un 429 espera al menos 60 s antes de reintentar. Mantenga **una sola instancia** con `USER_SYNC_ENABLED=true`. Un `HttpClient` con prioridad IPv4 evita *timeouts* en redes con IPv6 roto.
- **Alertas del ETL**: se guarda una copia por administrador (con su `ClientId`) y se entrega por SignalR, correo y SMS; sin administradores se conserva el mensaje original con `ClientId` vacío.
- **Caché de coordenadas**: `MemoryCache` con tamaño máximo, porcentaje de evicción, expiración absoluta/deslizante y largo máximo de clave configurables; evita golpear Nominatim repetidamente.
- **Observabilidad**: escritura por lotes (`BatchingTelemetryWriter`) hacia MongoDB; se desactiva con `OBSERVABILITY_ENABLED=false`.
- **ETL**: el Orchestrator lanza `Etl.App` como proceso hijo que hereda las variables de entorno del contenedor; el control de ejecuciones queda en `etl_job_control`.
- **Hora**: toda la lógica temporal usa la hora de Colombia a través de una abstracción del dominio (facilita las pruebas).
- **Versionado de imágenes**: la versión sale del `<Version>` del `.csproj` de cada proyecto ejecutable.

## 16. Seguridad

- Nunca versione secretos: `.env`, `secrets.auto.tfvars` y los certificados `.pfx` están excluidos por `.gitignore`; `appsettings.Development.json` **no** lo está, así que no coloque credenciales reales en él (use *User Secrets* o variables de entorno). La imagen Docker excluye además los `appsettings.*.json`, `.pfx` y `.env` (`.dockerignore`).
- Los pipelines ejecutan **Gitleaks** y **SonarCloud** en cada cambio.
- El gateway aplica JWT, CORS restrictivo por origen y *rate limiting*; solo el gateway se expone a Internet.
- `/connect/token` valida clientes de demostración con secretos fijos en código (`TokenEndpoints.cs`): **reemplácelo antes de exponer el gateway públicamente**.
- `EXTERNAL_API_ALLOW_INVALID_CERT=true` es solo para desarrollo.
- Los errores del ETL se sanitizan (tokens/JWT) antes de registrarse o enviarse en alertas.

## 17. Solución de problemas

| Síntoma | Causa probable | Qué revisar |
|---|---|---|
| `429 - Global limit has been reached` (Auth0) | Demasiadas llamadas a la Management API o varias instancias sincronizando | Una sola instancia con `USER_SYNC_ENABLED=true`; revisar logs del tenant (`api_limit`) y esperar a que se renueve el *bucket*. |
| `TaskCanceledException` al llamar a Auth0 en local | Ruta IPv6 rota hacia Auth0 | Ya mitigado con conexión IPv4 primero; verifique con `curl -4` vs `curl -6`. |
| Contenedores no conectan a Kafka/PostgreSQL | Host `localhost` en las cadenas | Use el nombre del contenedor en la red Docker y el listener interno de Kafka. |
| Timeout por SSH en AWS | Cambió su IP pública | Actualice `my_ip` en `environments/dev.tfvars` y aplique de nuevo. |
| Pruebas fallan al correr en paralelo | Estado estático de telemetría | Agrupar en la misma `[Collection]` de xUnit. |

## 18. Documentación adicional

- [`deploy/README.md`](deploy/README.md) — imágenes Docker, build y publicación.
- [`https://github.com/fxfranco/Connection360IaC/README.md`](https://github.com/fxfranco/Connection360IaC/README.md) — infraestructura en AWS Academy.
- `Documents/` — scripts SQL, Swagger de las APIs, ejemplos de `appsettings` y scripts `.bat` de arranque.
- `https://github.com/fxfranco/Connection360ApiCore/tree/main/.github/workflows/` — definición de los pipelines.
