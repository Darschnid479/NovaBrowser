@echo off
setlocal EnableExtensions DisableDelayedExpansion
if not exist "%~dp0docs\index.html" (
  echo Pakk ut hele ZIP-filen forst.
  pause
  exit /b 1
)
start "" "%~dp0docs\index.html"
