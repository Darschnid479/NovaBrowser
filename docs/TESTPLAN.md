# Testplan for Windows

**Status ved levering: testene nedenfor er ikke kjørt i Windows.**
Kildestruktur og XML er kontrollert separat. Se VERIFISERING.txt.

## Nye kontroller for 0.2.0

Bruk først eksisterende profil; ikke slett dataene for å teste oppdateringen.

- [ ] Den første oppstarten med 0.2.0 viser veiviseren. Ingen tidligere
      nettside skal åpne før fullføring.
- [ ] Uten søkemotorvalg er Neste deaktivert. Mus, Tab og piltaster virker.
- [ ] Velg hver søkemotor etter tur, gå frem/tilbake, og se at valget beholdes.
- [ ] Prøv alle fire temaer og seks aksenter. Animasjoner kan slås av.
- [ ] Fullfør, lukk og åpne. Veiviseren skal ikke dukke opp igjen automatisk.
- [ ] Åpne veiviseren fra Innstillinger. Forhåndsvis endringer, velg Avbryt:
      tidligere farger, søkemotor og navn skal beholdes.
- [ ] I et separat Windows-testbrukermiljø: lukk førstegangsoppsettet før
      fullføring, og åpne igjen. Veiviseren skal fremdeles vises.
- [ ] Eksisterende bokmerker og lagrede vanlige faner beholdes.
- [ ] Kontroller at URL-en er synlig, loddrett sentrert og venstrestilt.
      Prøv en lang URL, Ctrl+L, End, Home, markering og lim inn.
- [ ] Sidefelt, fanekryss, rullegardinpil, tilbake/frem, last/stopp,
      tilkobling og tom/fylt bokmerkestjerne er forskjellige tegnede ikoner.
- [ ] Åpne en nettside og kontroller at 0.1.2-rettelsen fortsatt virker.

Kjør `TEST-NOVA.cmd` for rene VB-tester og `TEST-UI.cmd` for egne WPF-tester.
Sistnevnte tegner de virkelige NovaIcon-kontrollene og måler den virkelige
TextBox-malen, men tester ikke hele vinduet, WebView2 eller faktisk skjerm-DPI.
Et vellykket resultat fra en av disse er ikke en full systemtest.
`python tools/check_source.py` er en valgfri, separat statisk kildekontroll.

## Regresjonstest for 0.1.2

Kjoer foerst punktene i `HOTFIX-0.1.2.md`. Aa se startsiden er ikke nok;
aapne en nettside for aa teste at CompositionControl virkelig starter.
Kontroller begge WinRT-bibliotekene baade i byggemappen og publisert EXE-mappe.
Ikke slett profilen for aa gjennomfoere rettelsen.

## Kodekontroller

Kjør `TEST-NOVA.cmd`. Prosjektet i tests/NovaBrowser.Checks bruker de faktiske
UrlPolicy-, SearchProviders-, SetupSession-, AppState- og ErrorDiagnostics-filene. Det kontrollerer adresser, søkeencoding, blokkerte
adressetyper, rensing av titler, serialisering og filtrert feildiagnostikk. Det kontakter ingen nettsteder og
endrer ikke en eksisterende NOVA-profil. Et vellykket resultat skriver PASS og antall.
Dette er ikke WPF-, nettverks- eller sikkerhetstester av hele nettleseren.

Deretter:

```powershell
dotnet build NOVA.sln -c Release
dotnet run --project src/NovaBrowser/NovaBrowser.vbproj -c Release
```

## Manuelle kontroller

Alle punkter skal verifiseres, ikke antas bestått:

- [ ] Ren Windows 11 x64: start uten eksisterende NOVA-data.
- [ ] Manglende WebView2 Runtime: forståelig feil på fanen, ikke en stille krasj.
- [ ] Maksimer, gjenopprett, flytt, minimer og endre vindusstørrelse.
- [ ] Skjermskalering 100, 125, 150 og 200 prosent; flere skjermer.
- [ ] Opprett 10 faner. Bytt, lukk via kryss og midtklikk, og åpne igjen.
- [ ] Skriv en ny adresse mens den første nettsiden fortsatt starter.
- [ ] Lukk en fane mens WebView2 initialiseres. Ingen gjenværende nettside over feil fane.
- [ ] Lukk siste fane. En ny startside skal opprettes.
- [ ] Søk, direkte adresse, localhost:port, HTTP, HTTPS, Unicode-adresse, ugyldig adresse.
- [ ] Tilbake/frem, stopp, ny lasting, og retur fra startside til forrige nettside.
- [ ] Alle 24 kombinasjoner av fire temaer og seks aksenter; lesbar tekst i knapper.
- [ ] Navn, bakgrunn, klokke, kompakte faner og animasjoner bevares etter omstart.
- [ ] Windows-animasjoner av: ingen startsidebevegelse eller panelinnblending.
- [ ] Innstillinger og kommandofelt vises over nettsiden, og tastaturfokus holdes i panelet.
- [ ] Nettleserens hurtigtaster virker både på startsiden og med fokus inne i en nettside.
- [ ] Legg til og fjern bokmerker. De seks første vises som snarveier.
- [ ] Slå av NOVAs besøksliste. Nye besøk skal ikke komme inn i state.json-listen.
- [ ] Gjenoppretting på/av og riktig aktiv vanlig fane etter omstart.
- [ ] Privat innlogging deler ikke cookies med normal profil.
- [ ] Lukk siste private fane og opprett en ny; tidligere private innlogginger skal være borte.
- [ ] En unik privat adresse dukker ikke opp i state.json, error.log eller nylig-lukket-listen.
- [ ] Kontroller at et eksplisitt lagret privat bokmerke og en privat nedlastet fil beholdes.
- [ ] Brukerklikk som åpner nytt vindu blir ny fane; automatisk popup blir blokkert.
- [ ] Kamera, mikrofon og posisjon: Avbryt avviser, Tillat virker bare for den aktuelle forespørselen.
- [ ] Bytt side/fane mens en tillatelse venter. Tilgang skal ikke gis til feil side.
- [ ] Ugyldig TLS-sertifikat viser nettmotorens feil; verten må ikke ignorere feilen.
- [ ] Eksterne protokoller, file:, javascript: og data: åpnes ikke fra adressefeltet.
- [ ] Start en nedlasting. Se dialogen og prøv lukking av fane og app underveis.
- [ ] Slett NOVA-historikk og slett nettstedsdata hver for seg med bekreftelse.
- [ ] Frakoblet nett, krasjet nettmotor og manglende skrivetilgang gir forståelig respons.
- [ ] Skadet state.json gir standardoppsett og bevarer en sikkerhetskopi når mulig.
- [ ] Start to appforekomster; den andre skal ikke skrive til samme profil.
- [ ] Kjoer publisert mappe på en maskin uten SDK, men med WebView2 Runtime.

## Kjente testgrenser

DRM-beskyttet video støttes ikke av den valgte Composition-visningen. Dette skal ikke
rapporteres som en bestått streamingtest. Vanlig video, lyd, filopplasting, utskrift,
innlogging og tilgjengelighetsverktøy trenger egne kompatibilitetstester.

Det er ikke laget tester som dokumenterer bedre fart, minnebruk eller sikkerhet enn
Chrome eller Firefox. Slike sammenligninger krever reelle målinger.
