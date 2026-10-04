@echo off
setlocal
cd /d "%~dp0"
call dotnet run --project "tests\NovaBrowser.Checks\NovaBrowser.Checks.vbproj" --configuration Release
set result=%errorlevel%
pause
exit /b %result%
