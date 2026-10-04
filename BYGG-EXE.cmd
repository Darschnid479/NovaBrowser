@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 goto missing
call dotnet --list-sdks | findstr /r /b "10\." >nul
if errorlevel 1 goto missing
echo NOVA 0.2.0 - ikoner, adressefelt og oppstartsveiviser
echo [1/3] Kjoerer kodekontroller ...
call dotnet run --project "tests\NovaBrowser.Checks\NovaBrowser.Checks.vbproj" --configuration Release
if errorlevel 1 goto failed
echo [2/3] Henter Windows-programavhengigheter ...
call dotnet restore "src\NovaBrowser\NovaBrowser.vbproj" --runtime win-x64 --force-evaluate -p:SelfContained=true
if errorlevel 1 goto failed
echo [3/3] Bygger NOVA for Windows x64 ...
call dotnet publish "src\NovaBrowser\NovaBrowser.vbproj" --configuration Release --runtime win-x64 --self-contained true --output "out\win-x64" --no-restore -p:PublishSingleFile=false
if errorlevel 1 goto failed
if not exist "out\win-x64\Microsoft.Windows.SDK.NET.dll" goto incomplete
if not exist "out\win-x64\WinRT.Runtime.dll" goto incomplete
echo.
echo Ferdig: out\win-x64\NOVA.exe
echo Behold HELE win-x64-mappen. Ikke flytt bare EXE-filen.
echo WebView2 Evergreen Runtime maa fortsatt vaere installert.
start "" explorer "%~dp0out\win-x64"
pause
exit /b 0
:missing
echo Installer .NET 10 SDK. Se START-HER.txt.
pause
exit /b 1
:incomplete
echo FEIL: Windows-komponenter mangler i EXE-mappen.
:failed
echo Byggingen ble stoppet. Les feilen ovenfor foer du bruker EXE-filen.
pause
exit /b 1
