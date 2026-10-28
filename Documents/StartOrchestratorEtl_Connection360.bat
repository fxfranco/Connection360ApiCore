@echo off

:: ============================================================================
:: OBTENER LA RUTA DONDE ESTÁ GUARDADO ESTE .BAT
:: ============================================================================
set "RAIZ=%~dp0"

:: --- API 1: ApiCore ---
title Servicio Continuo - NETCore 10 (DLL)
echo [%date% %time%] Iniciando servicio interno...

set "SUB_DIR=Apis\Connection360.Etl.Orchestrator"
set "DLL_NAME=Connection360.Etl.Orchestrator.dll"
echo "%RAIZ%%SUB_DIR%\%DLL_NAME%"
if exist "%RAIZ%%SUB_DIR%\%DLL_NAME%" (
    cd /d "%RAIZ%%SUB_DIR%"
	dotnet "%DLL_NAME%"
) else (
    echo   [ERROR] No se encontro el archivo: %RAIZ%%SUB_DIR%\%DLL_NAME%
)