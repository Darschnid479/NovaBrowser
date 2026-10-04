# NOVA 0.2.0

## Hva som ble rettet

Skjermbildet viser små firkanter i ikonrekken. I 0.1.2 brukte IconButton
Segoe MDL2 Assets, men den implisitte TextBlock-stilen satte Segoe UI på
tekstinnholdet. WPF gir en stilsetter høyere prioritet enn en arvet verdi.
Dette er en kodeforklaring som passer symptomet; fontoppløsningen er ikke
inspisert i en kjørende Windows-prosess hos brukeren.

Alle MDL2-tegn, både i XAML og i dynamiske knappoppdateringer, er erstattet
med navngitte vektorformer. Faner, rullegardinpil, vindusknapper og
bokmerkestatus er også med. Ingen fontfiler følger pakken.

Tekstboksmalen brukte tidligere Padding både som margen til innholdet og,
gjennom WPFs innebygde videresending, som innvendig avstand. Dette kan
redusere tekstplassen kraftig i et lavt adressefelt. Margin er nå null;
innholdsholderen bruker loddrett sentrering. URL-en er fortsatt venstrestilt,
ikke skjult eller sentrert på en måte som kutter starten av adressen.

## Veiviser

1. Søkemotor: Google, DuckDuckGo, Bing. Et valg kreves før Neste aktiveres.
2. Utseende: fire temaer, seks aksenter og animasjoner med forhåndsvisning.
3. Personlig: navn, åpne vanlige faner igjen og lagre NOVAs besøksliste.

Lagringsformatet har fortsatt SchemaVersion 1. Den nye boolske verdien
SetupCompleted er False når den mangler i et eldre profilformat. Dette gir
engangsoppsett for eksisterende brukere. Avbrutt oppsett markeres ikke fullført.
Første oppstart laster ingen tidligere nettside før oppsettet er fullført.
Veiviseren kan også åpnes fra innstillinger, uten å lukke eksisterende faner.

Ingen kode for sletting av profiler, endring av Windows-standarder eller
installering av fonter er lagt til. WebView2-motor og 0.1.2-avhengighetene
beholdes. Søkeleverandørene er samlet i SearchProviders.vb slik at veiviser,
innstillinger og adressehåndtering bruker samme valg.

## Nye filer

- src/NovaBrowser/UI/NovaIcon.vb
- src/NovaBrowser/Services/SearchProviders.vb
- src/NovaBrowser/Models/SetupSession.vb
- src/NovaBrowser/MainWindow.Setup.vb
- tests/NovaBrowser.UiChecks/* og TEST-UI.cmd
- tools/check_source.py

## Teststatus

Se VERIFISERING.txt. XML- og kildekontroller samt separat SVG-geometrikontroll
er gjennomført. .NET-kompilering, VB-testene, WPF-testene og faktisk Windows-UI
må fortsatt kjøres. SVG-geometrikontrollen er ikke et skjermbilde fra NOVA.
