@echo off
setlocal
if not exist "%LOCALAPPDATA%\NOVA-Browser\error.log" goto missing
start "" notepad.exe "%LOCALAPPDATA%\NOVA-Browser\error.log"
exit /b 0
:missing
echo Ingen error.log er opprettet ennaa.
echo Ved byggefeil: kopier teksten fra byggevinduet i stedet.
pause
exit /b 1
