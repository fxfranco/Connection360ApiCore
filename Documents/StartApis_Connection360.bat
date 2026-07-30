@echo off
setlocal enabledelayedexpansion

:: ============================================================================
:: OBTENER LA RUTA DONDE ESTÁ GUARDADO ESTE .BAT
:: ============================================================================
set "RAIZ=%~dp0"

echo ===================================================
echo   Iniciando Servicios .NET Core
echo   Ruta Base: %RAIZ%
echo ===================================================
echo.

echo ===================================================
echo Version de .NET:
dotnet --version
echo ===================================================
echo.

:: ============================================================================
:: EJECUCIÓN DE CADA API
:: Formato de las instrucciones por cada API:
:: 1. Subcarpeta de la API
:: 2. Nombre del archivo .dll
:: 3. Puerto o Endpoint de prueba
:: ============================================================================

:: --- API 1: One BPMS ---
set "SUB_DIR=Apis\Connection360.Api"
set "DLL_NAME=Connection360.Api.dll"
set "URL_TEST1=https://localhost:44369/health"

echo [1/2] Lanzando API Connection360...
if exist "%RAIZ%%SUB_DIR%\%DLL_NAME%" (
    start /b "" cmd /c "cd /d "%RAIZ%%SUB_DIR%" && dotnet "%DLL_NAME%"" > nul 2>&1
) else (
    echo   [ERROR] No se encontro el archivo: %RAIZ%%SUB_DIR%\%DLL_NAME%
)

:: --- API 2: Two SIM ---
set "SUB_DIR=Apis\Connection360.ApiGateway"
set "DLL_NAME=Connection360.ApiGateway.dll"
set "URL_TEST2=https://localhost:44368/health"

echo [2/2] Lanzando API Connection360 ApiGateway...
if exist "%RAIZ%%SUB_DIR%\%DLL_NAME%" (
    start /b "" cmd /c "cd /d "%RAIZ%%SUB_DIR%" && dotnet "%DLL_NAME%"" > nul 2>&1
) else (
    echo   [ERROR] No se encontro el archivo: %RAIZ%%SUB_DIR%\%DLL_NAME%
)

:: ============================================================================
:: TIEMPO DE ESPERA Y COMPROBACIÓN
:: ============================================================================
echo.
echo Esperando 10 segundos a que los procesos inicien...
timeout /t 10 /nobreak > nul
echo.

echo ===================================================
echo   Verificando Conexion a las APIs
echo ===================================================

call :VerificarAPI "API Connection360" "%URL_TEST1%"
call :VerificarAPI "API Connection360 ApiGateway" "%URL_TEST2%"

echo.
echo ===================================================
echo Proceso finalizado.
echo ===================================================
pause
goto :eof


:: ============================================================================
:: FUNCIÓN DE VERIFICACIÓN HTTP (PowerShell)
:: ============================================================================
:VerificarAPI
set "NOMBRE_API=%~1"
set "URL_API=%~2"

powershell -Command "$ProgressPreference = 'SilentlyContinue'; try { $res = Invoke-WebRequest -Uri '%URL_API%' -UseBasicParsing -TimeoutSec 4; if ($res.StatusCode -ge 200 -and $res.StatusCode -lt 400) { exit 0 } else { exit 1 } } catch { exit 1 }"

if %errorlevel% equ 0 (
    echo [ OK ] %NOMBRE_API% esta respondiendo en: %URL_API%
) else (
    echo [ERROR] %NOMBRE_API% NO responde en: %URL_API%
)
goto :eof