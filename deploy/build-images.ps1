<#
.SYNOPSIS
  Construye (y opcionalmente publica en Docker Hub) las imágenes de Connection360.

.DESCRIPTION
  La versión de cada imagen es el <Version> de su .csproj, salvo que se indique -Version (manual, para todas).
  Cada imagen queda con dos tags: <version en minúsculas> y latest (quítalo con -NoLatest).
  Al terminar actualiza deploy/versions.env con las versiones construidas (conserva las de los componentes no construidos).

.EXAMPLE
  ./deploy/build-images.ps1                              # construye los 5 componentes
  ./deploy/build-images.ps1 -Push                        # construye y publica en Docker Hub (requiere docker login)
  ./deploy/build-images.ps1 -Registry miusuario -Components api,gateway
  ./deploy/build-images.ps1 -Version 1.2.0 -Platform linux/amd64
  ./deploy/build-images.ps1 -DryRun                      # solo muestra qué haría
#>
[CmdletBinding()]
param(
    [string]$Registry = 'fxfranco13',
    [switch]$Push,
    [switch]$DryRun,
    [string]$Version = '',
    [string]$Platform = '',
    [switch]$NoLatest,
    [ValidateSet('gateway', 'api', 'notification', 'etl-app', 'etl-orchestrator')]
    [string[]]$Components = @('gateway', 'api', 'notification', 'etl-app', 'etl-orchestrator')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$catalog = @{
    'gateway'          = @{ Project = 'Connection360.ApiGateway';          Image = 'connection360.apigateway';          Var = 'APIGATEWAY_VERSION' }
    'api'              = @{ Project = 'Connection360.Api';                 Image = 'connection360.api';                 Var = 'API_VERSION' }
    'notification'     = @{ Project = 'Connection360Notification.Api';    Image = 'connection360notification.api';     Var = 'NOTIFICATION_API_VERSION' }
    'etl-app'          = @{ Project = 'Connection360.Etl.App';             Image = 'connection360.etl.app';             Var = 'ETL_APP_VERSION' }
    'etl-orchestrator' = @{ Project = 'Connection360.Etl.Orchestrator';    Image = 'connection360.etl.orchestrator';    Var = 'ETL_ORCHESTRATOR_VERSION' }
}

function Get-CsprojVersion([string]$project) {
    $path = Join-Path $root "$project/$project.csproj"
    $match = Select-String -Path $path -Pattern '<Version>(.+?)</Version>' | Select-Object -First 1
    if (-not $match) { return $null }
    return $match.Matches[0].Groups[1].Value.Trim()
}

# Un tag de Docker no admite '+'; se normaliza a minúsculas por consistencia con los tags ya publicados.
function Get-ImageTag([string]$version) { return $version.ToLowerInvariant().Replace('+', '-') }

function Invoke-Docker([string[]]$dockerArgs) {
    Write-Output ("+ docker " + ($dockerArgs -join ' '))
    if (-not $DryRun) {
        & docker @dockerArgs
        if ($LASTEXITCODE -ne 0) { throw "docker $($dockerArgs[0]) falló (código $LASTEXITCODE)." }
    }
}

$versionsFile = Join-Path $root 'deploy/versions.env'
$versionLines = @()
$summary = @()
$env:DOCKER_BUILDKIT = '1'

foreach ($key in $Components) {
    $c = $catalog[$key]
    $appVersion = if ($Version) { $Version } else { Get-CsprojVersion $c.Project }
    if (-not $appVersion) { throw "No se encontró <Version> en $($c.Project)/$($c.Project).csproj (usa -Version)." }

    $tag = Get-ImageTag $appVersion
    $ref = "$Registry/$($c.Image)"

    $buildArgs = @('build', '-f', "$($c.Project)/Dockerfile", '-t', "${ref}:$tag", '--build-arg', "APP_VERSION=$appVersion")
    if (-not $NoLatest) { $buildArgs += @('-t', "${ref}:latest") }
    if ($Platform) { $buildArgs += @('--platform', $Platform) }
    if ($key -eq 'etl-orchestrator') {
        $etlVersion = if ($Version) { $Version } else { Get-CsprojVersion 'Connection360.Etl.App' }
        $buildArgs += @('--build-arg', "ETL_VERSION=$etlVersion")
    }
    # Con un builder de tipo docker-container (el de Docker Desktop si se creó con buildx), el resultado del
    # build NO queda en las imágenes locales a menos que se pida: --push construye y sube en un solo paso,
    # --load lo deja disponible en "docker images" (y para docker compose).
    $buildArgs += $(if ($Push) { '--push' } else { '--load' })
    $buildArgs += '.'

    Write-Host ""
    Write-Host "=== $($c.Project)  ->  ${ref}:$tag"
    Invoke-Docker $buildArgs

    $versionLines += "$($c.Var)=$tag"
    $summary += "  $($c.Project)  ->  ${ref}:$tag"
}

Write-Host ""
Write-Host "Resumen:"
$summary | ForEach-Object { Write-Host $_ }
if (-not $DryRun) {
    # Se conservan las variables de los componentes que NO se construyeron en esta corrida
    # (p. ej. al usar -Components), y se actualizan las que sí.
    $merged = [ordered]@{}
    if (Test-Path $versionsFile) {
        Get-Content $versionsFile | ForEach-Object {
            if ($_ -match '^\s*([A-Za-z0-9_]+)=(.*)$') { $merged[$Matches[1]] = $Matches[2].Trim() }
        }
    }
    foreach ($line in $versionLines) {
        $k, $v = $line -split '=', 2
        $merged[$k] = $v
    }
    Set-Content -Path $versionsFile -Value ($merged.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -Encoding ascii
    Write-Host "Versiones escritas en deploy/versions.env (úsalas con: docker compose --env-file .env --env-file deploy/versions.env up -d)"
}
