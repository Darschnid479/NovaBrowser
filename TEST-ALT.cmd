@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 goto missing
echo [1/4] VB policy- og lagringstester i midlertidige mapper ...
call dotnet run --project tests\NovaBrowser.Checks\NovaBrowser.Checks.vbproj -c Release
if errorlevel 1 goto failed
echo [2/4] WPF ikoner og tekst ...
call dotnet run --project tests\NovaBrowser.UiChecks\NovaBrowser.UiChecks.vbproj -c Release
if errorlevel 1 goto failed
echo [3/4] Hele losningen ...
call dotnet build NOVA.sln -c Release
if errorlevel 1 goto failed
echo [4/4] Ekte vindu med en separat testprofil ...
call dotnet run --project tests\NovaBrowser.ShellChecks\NovaBrowser.ShellChecks.vbproj -c Release --no-build -- artifacts\wpf
if errorlevel 1 goto failed
echo.
echo Vil du ogsa teste nettmotoren mot en lokal testserver?
echo Testen bruker en egen midlertidig profil og simulerer et krasj
 echo KUN i testmotoren. Ingen offentlig nettside eller normalprofil brukes.
choice /c JN /n /m "Kjor utvidet nettmotortest? J/N: "
if errorlevel 2 goto done
call dotnet run --project tests\NovaBrowser.ShellChecks\NovaBrowser.ShellChecks.vbproj -c Release --no-build -- artifacts\wpf --live
if errorlevel 1 goto failed
:done
echo FERDIG. Testbilder ligger i artifacts\wpf.
pause
exit /b 0
:missing
echo Installer .NET 10 SDK og WebView2 Runtime. Se START-HER.txt.
pause
exit /b 1
:failed
echo En test feilet. Se meldingen ovenfor. Ikke slett nettleserprofilen.
pause
exit /b 1
