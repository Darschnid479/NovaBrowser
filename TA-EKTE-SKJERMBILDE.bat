@echo off
setlocal EnableExtensions DisableDelayedExpansion
title NOVA - Ekte skjermbilde
if not exist "%~dp0tools\Capture-Nova.ps1" (
  echo Pakk ut hele ZIP-filen forst.
  pause
  exit /b 1
)
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Capture-Nova.ps1"
set "RESULT=%ERRORLEVEL%"
echo.
pause
exit /b %RESULT%
