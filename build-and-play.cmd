@echo off
title OpenRA - Build and Play
cd /d "%~dp0"

echo ========================================
echo   OpenRA TAK Support - Build and Play
echo ========================================
echo.

echo [1/2] Cleaning previous build...
powershell -ExecutionPolicy Bypass -File make.ps1 clean
if %errorlevel% neq 0 (
    echo Clean failed, continuing anyway...
)
echo.

echo [2/2] Building...
powershell -ExecutionPolicy Bypass -File make.ps1 all
if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo   BUILD FAILED
    echo   Make sure .NET 8.0 SDK is installed:
    echo   https://dotnet.microsoft.com/download
    echo ========================================
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build succeeded! Launching game...
echo ========================================
echo.

bin\OpenRA.exe Engine.EngineDir=".." Engine.LaunchPath="%~dpf0" Game.Mod=ra

if %errorlevel% neq 0 (
    echo.
    echo ----------------------------------------
    echo OpenRA has encountered a fatal error.
    set logs=%AppData%\OpenRA\Logs
    if exist Support\Logs (set logs=%cd%\Support\Logs)
    echo   Log Files: %logs%
    echo ----------------------------------------
    pause
)
