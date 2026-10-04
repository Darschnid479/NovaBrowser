@echo off
setlocal EnableExtensions EnableDelayedExpansion
title NOVA Browser - Upload til GitHub
cd /d "%~dp0"

set "REPO_URL=https://github.com/Darschnid479/NovaBrowser.git"
set "BRANCH=main"

echo.
echo ================================================
echo          NOVA Browser - GitHub Upload
echo ================================================
echo.
echo Denne filen laster opp ALT i denne mappen
echo til:
echo %REPO_URL%
echo.

where git >nul 2>&1
if errorlevel 1 (
    echo [FEIL] Git er ikke installert eller finnes ikke i PATH.
    echo.
    echo Installer Git for Windows fra:
    echo https://git-scm.com/download/win
    echo.
    pause
    exit /b 1
)

echo [OK] Git funnet.
git --version
echo.

rem ------------------------------------------------
rem Opprett Git-repo lokalt hvis det ikke finnes
rem ------------------------------------------------
if not exist ".git" (
    echo [1/7] Oppretter lokalt Git-repository...
    git init
    if errorlevel 1 goto :error
) else (
    echo [1/7] Git-repository finnes allerede.
)

rem ------------------------------------------------
rem Sett hovedbranch til main
rem ------------------------------------------------
echo [2/7] Setter branch til %BRANCH%...
git branch -M %BRANCH%
if errorlevel 1 goto :error

rem ------------------------------------------------
rem Sett/oppdater remote origin
rem ------------------------------------------------
echo [3/7] Kobler til GitHub-repository...

git remote get-url origin >nul 2>&1
if errorlevel 1 (
    git remote add origin "%REPO_URL%"
    if errorlevel 1 goto :error
) else (
    git remote set-url origin "%REPO_URL%"
    if errorlevel 1 goto :error
)

echo Remote:
git remote -v
echo.

rem ------------------------------------------------
rem Hent eksisterende GitHub-innhold hvis repo ikke er tomt
rem ------------------------------------------------
echo [4/7] Sjekker GitHub for eksisterende innhold...
git ls-remote --exit-code origin refs/heads/%BRANCH% >nul 2>&1
if not errorlevel 1 (
    echo Fant eksisterende %BRANCH%-branch pa GitHub.
    echo Henter siste versjon for a unnga unodvendige konflikter...
    git fetch origin %BRANCH%
    if errorlevel 1 goto :autherror

    git rev-parse HEAD >nul 2>&1
    if not errorlevel 1 (
        git merge origin/%BRANCH% --allow-unrelated-histories --no-edit
        if errorlevel 1 (
            echo.
            echo [FEIL] Det oppstod en merge-konflikt.
            echo Ingen filer er sendt til GitHub.
            echo Los konflikten i prosjektmappen og kjor BAT-filen pa nytt.
            echo.
            pause
            exit /b 1
        )
    )
) else (
    echo Ingen eksisterende main-branch funnet, eller repository er tomt.
)

rem ------------------------------------------------
rem Legg til alle filer
rem ------------------------------------------------
echo.
echo [5/7] Legger til alle prosjektfiler...
git add --all
if errorlevel 1 goto :error

rem ------------------------------------------------
rem Commit bare hvis noe har endret seg
rem ------------------------------------------------
echo [6/7] Lager commit...

git diff --cached --quiet
if errorlevel 1 (
    set "STAMP=%date% %time%"
    git commit -m "Update NOVA Browser - %STAMP%"
    if errorlevel 1 (
        echo.
        echo [FEIL] Commit kunne ikke opprettes.
        echo.
        echo Hvis Git ber om navn og e-post, kjor:
        echo   git config --global user.name "Ditt Navn"
        echo   git config --global user.email "din-epost@example.com"
        echo.
        pause
        exit /b 1
    )
) else (
    echo Ingen nye endringer a committe.
)

rem ------------------------------------------------
rem Push
rem ------------------------------------------------
echo.
echo [7/7] Laster opp til GitHub...
echo.
echo Hvis GitHub ber deg logge inn, fullfor innloggingen.
echo.

git push -u origin %BRANCH%
if errorlevel 1 goto :autherror

echo.
echo ================================================
echo               FERDIG!
echo ================================================
echo.
echo Alt er lastet opp til:
echo https://github.com/Darschnid479/NovaBrowser
echo.
echo Du kan na lukke dette vinduet.
echo.
pause
exit /b 0

:autherror
echo.
echo ================================================
echo          GITHUB-INNLOGGING / PUSH FEILET
echo ================================================
echo.
echo GitHub godtar ikke vanlig kontopassord via Git.
echo.
echo En enkel losning er Git Credential Manager, som normalt
echo folger med Git for Windows. Ved neste push skal det
echo apnes et GitHub-innloggingsvindu i nettleseren.
echo.
echo Du kan ogsa installere GitHub CLI og kjore:
echo   gh auth login
echo.
echo Deretter kjorer du denne BAT-filen pa nytt.
echo.
pause
exit /b 1

:error
echo.
echo ================================================
echo                  FEIL
echo ================================================
echo.
echo En Git-kommando feilet.
echo Ingen ekstra filer blir slettet av denne BAT-filen.
echo Se meldingen over for detaljer.
echo.
pause
exit /b 1
