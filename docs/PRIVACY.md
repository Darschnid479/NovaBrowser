# Lokal lagring og personvern / 0.3.0

Normal profil: `%LOCALAPPDATA%\NOVA-Browser`. Den kan inneholde navn, innstillinger, bokmerker, NOVAs lokale besøksliste, vanlige faner, WebView2-data og feillogg. Appdata er ikke et kryptert passordhvelv. Ikke last opp profilmappen.

## Lagrede kopier

`state.json` skrives via en midlertidig fil og atomisk utskifting. `state.json.bak` kan inneholde forrige gyldige tilstand, inkludert tidligere historikk. Ved skadet fil kan opptil tre `state.json.corrupt-*` beholdes for gjenoppretting. Handlingene for eksplisitt sletting av historikk/nettstedsdata fjerner også disse profilkopiene etter vellykket lagring. Feilloggen er separat og kan inneholde tekniske stier eller detaljer; gå gjennom den før deling.

Å slå av lagring av NOVAs besøksliste sletter ikke tidligere historikk eller WebView2s øvrige data. Bruk de eksplisitte sletteknappene for eksisterende data.

## Adresseforslag og privat modus

Adresseforslag kommer fra åpne faner, bokmerker og NOVAs lokale historikk; det finnes ingen ekstern autocomplete-tjeneste. Når du faktisk søker eller besøker en adresse, sendes den til valgt søkemotor/nettsted.

Private faner bruker WebView2s InPrivate-profilvalg og tas ikke med i NOVAs lagrede økter eller historikk. Private forslag viser ikke normal historikk eller normale faner. Bokmerker er bevisst felles. En ny privat økt får et nytt midlertidig profilnavn. En isolert test av localStorage mellom normal og privat økt bestod; dette er ikke en full revisjon av alle WebView2-data.

Nedlastingsmetadata holdes i minnet for økten. Private poster fjernes når eierfanen lukkes, men filer du selv lagrer er vanlige filer på disken. Privat betyr ikke anonym overfor nettsteder, nettverk eller internettleverandør.

Det finnes ingen NOVA-konto eller NOVA-sky. Nettsteder, søkemotorer, WebView2 og runtime-oppdateringer kan ha egne nettverkstjenester og vilkår. Ikke bruk en utviklingsversjon som bevis for total anonymitet eller sikkerhet.

## Testing og opplasting

`NOVA_PROFILE_ROOT` lar testverktøy velge en separat, absolutt profilbane. Integrasjonstester bruker unike midlertidige mapper, ikke normalprofilen. GitHub-opplasteren filtrerer kjente profil-/byggefiler og enkelte hemmeligheter, men manuell gjennomgang er fortsatt nødvendig. Skjermbilder kan inneholde personopplysninger. `TA-EKTE-SKJERMBILDE.bat` ber om godkjenning før et eget skjermbilde blir publiserbart.
