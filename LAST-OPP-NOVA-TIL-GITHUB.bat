@echo off
setlocal EnableExtensions DisableDelayedExpansion
title NOVA - Last opp til GitHub
if not exist "%~dp0tools\Upload-Nova.ps1" (
  echo [FEIL] Pakk ut hele ZIP-filen. tools\Upload-Nova.ps1 mangler.
  pause
  exit /b 1
)
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Upload-Nova.ps1"
set "RESULT=%ERRORLEVEL%"
echo.
pause
exit /b %RESULT%
