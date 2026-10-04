@echo off
setlocal
cd /d "%~dp0"
echo NOVA 0.2.0 - Windows/WPF icon and address layout tests
echo Tests actual vector drawing and text layout without opening any websites.
call dotnet run --project "tests\NovaBrowser.UiChecks\NovaBrowser.UiChecks.vbproj" --configuration Release
set result=%errorlevel%
pause
exit /b %result%
