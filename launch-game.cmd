@echo off
title OpenRA
for %%x in (%*) do (
  if "%%~x" EQU "Game.Mod" (goto launch)
)

:choosemod
set /P mod=Select mod (ra, cnc, d2k, ts) or --exit: 
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
OpenRA.Game.exe Game.Mod=%mod% %*
goto end
:launch
OpenRA.Game.exe %*

:end
if %errorlevel% neq 0 goto crashdialog
exit /b
:crashdialog
echo ----------------------------------------
echo OpenRA has encountered a fatal error.
echo   * Log Files are available in Documents\OpenRA\Logs
echo   * FAQ is available at https://github.com/OpenRA/OpenRA/wiki/FAQ
echo ----------------------------------------
pause