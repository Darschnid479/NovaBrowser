# Produktstatus / app 0.2.0

## Implementert i kildekoden

Faner og private faner; adresse-/søkefelt; Google, DuckDuckGo og Bing; bokmerker; NOVAs lokale besøksliste; gjenoppretting av vanlige faner; temaer, aksenter og bakgrunner; kommandofelt; oppstartsveiviser; nettstedsforespørsler og WebView2-integrasjon.

Dette er kildekodestatus, ikke en godkjenning av funksjonene til daglig bruk.

## Kjente eller uavklarte forhold

- Brukeren har rapportert at lukk-/maksimeringsknappene ikke er synlige. Koden inneholder slike kontroller, men problemet i den kjørende appen er **ikke verifisert løst**.
- Det har tidligere vært kompileringsfeil, manglende runtime-komponenter og ikon-/tekstfeltproblemer. 0.2.0 inneholder tidligere rettelser, men en komplett Windows-test av denne leveransen er ikke utført her.
- WebView2CompositionControl har dokumenterte begrensninger som kan påvirke bildefrekvens og DRM-avspilling. Se Microsoft-kilden i SOURCES.
- Det finnes ingen bekreftet sammenlignende ytelses-, sikkerhets- eller minnetest mot Chrome/Firefox.
- PowerShell-opplastingen og skjermbildeverktøyet krever reell Windows-kjøring. Medfølgende integrasjonstest er konfigurert for Windows Actions; et oppsett er ikke det samme som et bestått testresultat.

## Ikke inkludert

Ny nettmotor, installasjonsprogram, signert offentlig Windows-release, automatiske appoppdateringer, passordbehandler, utvidelsesbutikk, skysynkronisering, ferdige fanegrupper og full støtte for flere vinduer.

## Denne leveransen endrer

GitHub-presentasjon, landingsside, bildepakke, dokumentasjon, opplasting og vedlikeholdsverktøy. Appkoden i `src/` er ikke endret.
