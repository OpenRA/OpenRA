@echo off
title OpenRA
for %%x in (%*) do (
  if "%%~x" EQU "Game.Mod" (goto launch)
)

:choosemod
set /P mod="Select mod (ra, cnc, d2k, ts) or --exit: "
if /I "%mod%" EQU "--exit" (exit /b)
if "%mod%" EQU "ra" (goto launchmod)
if "%mod%" EQU "cnc" (goto launchmod)
if "%mod%" EQU "ts" (goto launchmod)
if "%mod%" EQU "d2k" (goto launchmod)
echo.
echo Unknown mod: %mod%
echo.
goto choosemod

:launchmod
bin\OpenRA.exe Engine.EngineDir=".." Game.Mod=%mod% %*
goto end
:launch
bin\OpenRA.exe Engine.EngineDir=".." %*

:end
if %errorlevel% neq 0 goto crashdialog
exit /b

:crashdialog
set logs=%AppData%\OpenRA\Logs
if exist %USERPROFILE%\Documents\OpenRA\Logs (set logs=%USERPROFILE%\Documents\OpenRA\Logs)
if exist Support\Logs (set logs=%cd%\Support\Logs)

echo ----------------------------------------
echo OpenRA has encountered a fatal error.
echo   * Log Files are available in %logs%
echo   * FAQ is available at https://github.com/OpenRA/OpenRA/wiki/FAQ
echo ----------------------------------------
pause
