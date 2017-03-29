@echo off
title OpenRA
:choosemod
set /P mod=Select mod (ra, cnc, d2k, ts) or --exit: 
if /I "%mod%" EQU "--exit" (exit)
if /I "%mod%" EQU "ra" (goto launch)
if /I "%mod%" EQU "cnc" (goto launch)
if /I "%mod%" EQU "ts" (goto launch)
if /I "%mod%" EQU "d2k" (goto launch)
echo.
echo Unknown mod: %mod%
echo.
goto choosemod
:launch
OpenRA.Game.exe Game.Mod=%mod%
if %errorlevel% neq 0 goto crashdialog
exit
:crashdialog
echo ----------------------------------------
echo OpenRA has encountered a fatal error.
echo   * Log Files are available in Documents\OpenRA\Logs
echo   * FAQ is available at https://github.com/OpenRA/OpenRA/wiki/FAQ
echo ----------------------------------------
pause