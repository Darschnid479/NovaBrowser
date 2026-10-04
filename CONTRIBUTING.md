# Bidra til NOVA

NOVA er under utvikling. Små, etterprøvbare endringer er mer verdifulle enn store løfter.

## Feil

Bruk saksmalen. Beskriv versjon, Windows-versjon, skjermskalering, forventet oppførsel, faktisk oppførsel og konkrete trinn. Et relevant skjermbilde er nyttig, men fjern privat informasjon. Ikke last opp profilmappen eller uredigerte sensitive logger.

## Kode

Lag en gren, hold endringen avgrenset og forklar hva du faktisk har testet. Appkoden er Visual Basic .NET med Option Strict; grensesnittet er WPF/XAML. Nettsiden er separat HTML/CSS/JavaScript. Ikke legg inn en ny appavhengighet bare for en enkel grafisk detalj.

```powershell
python tools/check_source.py
python tools/check_package.py
dotnet run --project tests/NovaBrowser.Checks/NovaBrowser.Checks.vbproj -c Release
dotnet run --project tests/NovaBrowser.UiChecks/NovaBrowser.UiChecks.vbproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Upload.Tests.ps1
```

UI-testene krever Windows. Skriv tydelig hvilke tester du ikke har kjørt. Ikke merk en forhåndsvisning som et ekte app-skjermbilde.

## Ytelse og sikkerhet

Legg ved reproducerbar metode og rådata ved sammenlignende påstander. Sikkerhetsproblemer følger SECURITY.md. Ingen hardkodede tokens, konto-passord eller hemmelige testdata i en pull request.
