# Last opp kildepakken

## Vanlig bruk

Pakk ut hele ZIP-filen i en ny mappe. Behold `LAST-OPP-NOVA-TIL-GITHUB.bat`, `tools/Upload-Nova.ps1` og `tools/NovaUpload.psm1` sammen med resten av prosjektet. Installer Git for Windows ved behov, og dobbeltklikk BAT-filen.

Repoet er satt til `https://github.com/Darschnid479/NovaBrowser.git`, gren `main`. Git Credential Manager kan be deg logge inn i nettleseren. Skriptet ber ikke om å skrive et token inn i kodefilene. En alternativ innloggingsflyt er `gh auth login` fulgt av `gh auth setup-git`.

Opplasteren henter repoets eksisterende historie inn i en **ny midlertidig arbeidskopi**, kopierer pakken over den, og viser filstatus og endringsstørrelse. `JA` godkjenner commit og push. Enter avbryter. Vanlig push avvises dersom nye commits eller grenregler hindrer oppdateringen; det brukes ikke force-push eller automatisk sammenfletting av urelatert historikk.

Den gamle BAT-filens opplastingslogikk er **erstattet**, ikke bare pakket inn. Dette unngår problemet der en ny lokal Git-historikk skulle flettes inn i et repo med eget innhold.

## Hva endres?

Kildemappen endres ikke. Arbeidskopien beholdes under `%TEMP%\NOVA-GitHub-upload-...`, og mappestien skrives ut. Global Git-konfigurasjon endres ikke. Manglende commitnavn/e-post spørres etter og settes bare i arbeidskopien. E-postadressen blir en del av den publiserte Git-historikken: bruk gjerne din egen GitHub noreply-adresse fra kontoinnstillingene.

Filer som bare finnes på GitHub beholdes. Filer med samme navn kan oppdateres etter godkjenning. Slettinger i kildepakken videreføres **ikke** automatisk til repoet. Ved avvik må du se gjennom forskjellen, ikke blindt godkjenne.

## Hva utelates eller stoppes?

Byggemapper, `out`, `.git`, `artifacts`, WebView2-/profilmappenavn, logger, vanlige profilfiler og kompilerte binærfiler utelates. Filer som ser ut som hemmelige nøkler, `.env` og utvalgte tokenmønstre stopper opplastingen. Filer over 45 MB stoppes. Store binærfiler hører hjemme i Releases.

Dette er en enkel ekstra kontroll, **ikke et bevis på at pakken er fri for persondata eller hemmeligheter**. Les endringslisten og gjennomgå bilder manuelt.

## Bare forhåndsvis endringene

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Upload-Nova.ps1 -PreviewOnly
```

`-ExecutionPolicy Bypass` gjelder denne prosessen. Ingen maskininnstillinger endres, og administratorrettigheter er ikke nødvendige. I et administrert miljø som blokkerer skript, avklar kjøring med IT fremfor å endre organisasjonens regler.

## Test uten GitHub

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\Upload.Tests.ps1
```

Testen bruker bare midlertidige lokale Git-repoer. Den er konfigurert i Windows-workflowen, men var ikke kjørbar i leveransens Linux-miljø. Se QUALITY-REPORT.
