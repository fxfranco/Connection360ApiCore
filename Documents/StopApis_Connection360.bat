@echo off
setlocal enabledelayedexpansion

:: ============================================================================
:: CONFIGURACIÓN DE LAS APIS A DETENER
:: Agrega el nombre exacto del archivo .dll de cada API que deseas cerrar.
:: ============================================================================

set API_COUNT=2

set API[1]=Connection360.Api.dll
set API[2]=Connection360.ApiGateway.dll

:: ============================================================================
:: PROCESO DE CIERRE DE APIS
:: ============================================================================
echo ===================================================
echo   Deteniendo Servicios de .NET Core 10
echo ===================================================
echo.

for /l %%i in (1,1,%API_COUNT%) do (
    set DLL_NAME=!API[%%i]!
    echo [%%i/%API_COUNT%] Buscando y cerrando !DLL_NAME!...
    call :DetenerProceso "!DLL_NAME!"
)

echo.
echo ===================================================
echo Proceso finalizado. Todas las APIs configuradas han sido detenidas.
echo ===================================================
pause
goto :eof

:: ============================================================================
:: FUNCIÓN AUXILIAR PARA CERRAR PROCESO ESPECÍFICO
:: ============================================================================
:DetenerProceso
set TARGET_DLL=%~1

powershell -Command "$procs = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'dotnet.exe' -and $_.CommandLine -like '*%TARGET_DLL%*' }; if ($procs) { foreach ($p in $procs) { Stop-Process -Id $p.ProcessId -Force; Write-Host '  [ OK ] Se detuvo el proceso PID:' $p.ProcessId } } else { Write-Host '  [INFO] No se encontro ninguna API ejecutando %TARGET_DLL%' }"

goto :eof