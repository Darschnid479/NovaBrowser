@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 goto missing
call dotnet --list-sdks | findstr /r /b "10\." >nul
if errorlevel 1 goto missing
echo NOVA 0.2.0 - ikoner, adressefelt og oppstartsveiviser
echo [1/3] Henter programavhengigheter med riktig Windows-maal ...
call dotnet restore "src\NovaBrowser\NovaBrowser.vbproj" --runtime win-x64 --force-evaluate
if errorlevel 1 goto failed
echo [2/3] Kjoerer kodekontroller ...
call dotnet run --project "tests\NovaBrowser.Checks\NovaBrowser.Checks.vbproj" --configuration Release
if errorlevel 1 goto failed
echo [3/3] Bygger og starter NOVA ...
call dotnet run --project "src\NovaBrowser\NovaBrowser.vbproj" --configuration Release --runtime win-x64 --no-restore
if errorlevel 1 goto failed
exit /b 0
:missing
echo Du trenger .NET 10 SDK. SDK er ikke det samme som Runtime.
echo Installer fra https://dotnet.microsoft.com/download/dotnet/10.0
echo Les START-HER.txt, og start denne filen igjen.
pause
exit /b 1
:failed
echo.
echo NOVA kunne ikke bygges eller startes. Les feilen ovenfor.
echo Er det en programfeil, aapne AAPNE-FEILLOGG.cmd.
echo Ikke slett state.json eller WebView2-mappen.
pause
exit /b 1
