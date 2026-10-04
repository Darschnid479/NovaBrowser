"""Package only AFTER the Windows integration checks have actually passed."""
from pathlib import Path
import os, re, shutil, hashlib, json
R=Path(__file__).resolve().parents[1]
A=R/'artifacts'
def put(name,text):
    p=R/name; p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text.strip()+'\n',encoding='utf-8',newline='\n')
def text(name): return (R/name).read_text(encoding='utf-8-sig')
def cmd(name,body):
    (R/name).write_bytes((body.strip()+'\n').replace('\r\n','\n').replace('\n','\r\n').encode('ascii'))
logs={name:(A/name).read_text(encoding='utf-8-sig') for name in ['policy-tests.log','layout-tests.log','shell-tests.log','build.log','publish.log']}
assert 'PASS:' in logs['policy-tests.log'] and 'PASS:' in logs['layout-tests.log']
assert 'real WPF/engine checks' in logs['shell-tests.log'] and 'PASS: real WebView2 smoke test' in logs['shell-tests.log']
assert 'Build succeeded.' in logs['build.log']
run=os.environ['GITHUB_RUN_ID']; commit=os.environ['GITHUB_SHA']
url='https://github.com/Darschnid479/NovaBrowser/actions/runs/'+run
policy=re.search(r'PASS: (\d+) checks',logs['policy-tests.log']).group(1)
layout=re.search(r'PASS: (\d+) WPF',logs['layout-tests.log']).group(1)
shell=re.search(r'PASS: (\d+) real WPF/engine',logs['shell-tests.log']).group(1)
shots=R/'docs/assets/app-0.3.0'; shots.mkdir(parents=True,exist_ok=True)
required=['nova-03-'+theme+'-client.png' for theme in ['midnight','dawn','forest','graphite']]+['nova-03-settings-client.png','nova-03-setup-client.png','nova-03-compact-client.png','nova-03-live-engine.png']
for name in required:
    assert (A/'wpf'/name).is_file(), name
for image in (A/'wpf').glob('*.png'): shutil.copy2(image,shots/image.name)
validation=R/'docs/validation/0.3.0'; validation.mkdir(parents=True,exist_ok=True)
for name in ['policy-tests.log','layout-tests.log','shell-tests.log','build.log','publish.log','source-checks.log','package-checks.log']:
    shutil.copy2(A/name,validation/(name.removesuffix('.log')+'.txt'))
metadata={'version':'0.3.0','workflow_run':run,'workflow_url':url,'input_commit':commit,'date':'2026-10-04','policy_checks':int(policy),'wpf_layout_checks':int(layout),'wpf_engine_checks':int(shell),'source_files':{p.relative_to(R).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((R/'src').rglob('*')) if p.is_file() and not any(part in {'bin','obj'} for part in p.parts)},'images':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(shots.glob('*.png'))}}
put('docs/validation/0.3.0/evidence.json',json.dumps(metadata,indent=2,ensure_ascii=False))
hero='nova-03-midnight-window.png' if (shots/'nova-03-midnight-window.png').exists() else 'nova-03-midnight-client.png'
put('README.md',f'''
<div align="center">
<img src="docs/assets/brand/nova-mark.png" width="72" alt="NOVA symbol">

# NOVA Browser

### Internett. På din måte.

**Et personlig arbeidsområde for nettet. Bygget i Visual Basic .NET.**

[![Windows build](https://github.com/Darschnid479/NovaBrowser/actions/workflows/windows-build.yml/badge.svg)](https://github.com/Darschnid479/NovaBrowser/actions/workflows/windows-build.yml)
![Version](https://img.shields.io/badge/preview-0.3.0-b1a1ff?style=flat-square)
![Platform](https://img.shields.io/badge/Windows-x64-8797d8?style=flat-square)
[![MIT](https://img.shields.io/badge/license-MIT-83e3b8?style=flat-square)](LICENSE)

[Opplevelsen](#opplevelsen) · [Ekte appbilder](#ekte-appbilder) · [Kom i gang](#kom-i-gang) · [Testbevis](docs/QUALITY-REPORT.md) · [Veikart](docs/ROADMAP.md)
</div>

> **0.3.0 / utviklingsversjon.** Windows-bygging og automatiserte tester er kjørt, inkludert den faktiske nettmotoren. NOVA bruker Microsoft WebView2; dette er ikke en ny nettmotor eller dokumentert raskere/sikrere enn Chrome og Firefox. [Den konkrete valideringskjøringen]({url}).

## Opplevelsen

NOVA skal være et sted der fanene er ryddige, uttrykket er ditt og vanlige handlinger sitter i fingrene. 0.3.0 prioriterer det som gjør en nettleser brukbar over tid: fungerende vindusbetjening, robust lagring og kontroll over faner og nedlastinger.

| Arbeidsflyt | Personlighet | Pålitelighet |
| --- | --- | --- |
| Fest, flytt, dupliser og demp faner | Fire temaer og seks aksentfarger | Ekte Windows-knapper for lukk, minimer og maksimer |
| Lokale adresseforslag fra faner og bokmerker | Eget førstegangsoppsett | Atomisk profillagring med siste gyldige sikkerhetskopi |
| Nedlastingspanel med pause/fortsett/avbryt | Fokusmodus med `Ctrl+Shift+F` | Spør om gjenoppretting etter unormal avslutning |
| PDF/utskrift og JSON-bokmerkeutveksling | Sidefelt som tilpasses vindusbredden | Isolerte automatiske tester av privat lagring og motorfeil |

**Høyreklikk en fane** for faneverktøyene. Skriv i adressefeltet for lokale forslag; ingenting sendes til en søkeforslagstjeneste mens du skriver. Automatiske hvilende faner er **av som standard** og kan aktiveres i innstillingene. Bakgrunnsoppdateringer kan pauses, og minnebesparelse er ikke garantert.

## Ekte appbilder

<img src="docs/assets/app-0.3.0/{hero}" width="100%" alt="NOVA 0.3.0 fra den kjørende Windows-appen, med Midnight-tema">

Bildene er produsert av den faktiske WPF-appen på en isolert Windows-testmaskin. Filer med `-window` er native PrintWindow-opptak; `-client` er rendering av den levende WPF-klientflaten uten Windows-tittellinjen. Testdata er ikke en virkelig brukers nettleserhistorikk.

<table><tr>
<td width="50%"><img src="docs/assets/app-0.3.0/nova-03-dawn-client.png" alt="Ekte WPF-klientflate i Dawn"><b>Dawn</b><br>Lyst tema, samme arbeidsområde.</td>
<td width="50%"><img src="docs/assets/app-0.3.0/nova-03-forest-client.png" alt="Ekte WPF-klientflate i Forest"><b>Forest</b><br>Et roligere grønt uttrykk.</td>
</tr><tr>
<td><img src="docs/assets/app-0.3.0/nova-03-setup-client.png" alt="Kjørende WPF-veiviser"><b>Førstegangsoppsett</b><br>Valgene begynner hos deg.</td>
<td><img src="docs/assets/app-0.3.0/nova-03-settings-client.png" alt="Kjørende WPF-innstillinger"><b>Personlig tilpasning</b><br>Tema, søk, økter og ressursvalg.</td>
</tr></table>

[Reell nettmotor som viser en lokal testside](docs/assets/app-0.3.0/nova-03-live-engine.png) · [Kompakt vindu](docs/assets/app-0.3.0/nova-03-compact-client.png) · [Bildeopprinnelse](docs/VISUELT.md).

Designforhåndsvisningene i `docs/assets/previews/` er beholdt som et historisk 0.2.0-arkiv: hver slik HTML-rekonstruksjon er **ikke skjermbilde fra Windows-appen**. De nye 0.3.0-bildene over er ikke HTML-rekonstruksjoner.

## Kom i gang

**Ferdig Windows-pakke:** Pakk ut hele mappen, behold alle DLL-filer ved siden av `NOVA.exe`, og start `NOVA.exe`. Den publiserte utviklingspakken inkluderer .NET, men trenger Microsoft WebView2 Runtime separat. Den er usignert og er ikke en ferdig offentlig stabil utgivelse.

**Fra kildekode:** Windows x64, .NET 10 SDK og WebView2 Runtime kreves. Åpne `NOVA.sln` i Visual Studio, eller kjør:

```powershell
git clone https://github.com/Darschnid479/NovaBrowser.git
cd NovaBrowser
.\\START-NOVA.cmd
```

På utviklingsgrenen: `git switch work/nova-0.3.0`. Hovedgrenen endres først ved gjennomgått sammenslåing av endringene.

| Fil | Bruk |
| --- | --- |
| `START-NOVA.cmd` | Kontroller, bygg og start. |
| `BYGG-EXE.cmd` | Lag selvstendig Windows x64-mappe i `out/win-x64`. |
| `TEST-ALT.cmd` | Bygg, kjør modell-/WPF-tester og valgfri faktisk nettmotortest. |
| `SE-NETTSIDEN.bat` | Forhåndsvis den medfølgende landingssiden. |
| `LAST-OPP-NOVA-TIL-GITHUB.bat` | Vis endringer og last opp først etter at du skriver `JA`. |
| `TA-EKTE-SKJERMBILDE.bat` | Ta og godkjenn et bilde fra din egen NOVA. |

**Oppgradering:** Lukk gammel NOVA. Ta gjerne en lokal sikkerhetskopi av `%LOCALAPPDATA%\\NOVA-Browser` mens programmet er lukket. Ikke last opp profilen. Pakk ut programmet i en ny mappe uten gamle `bin`, `obj` eller `out`. Eksisterende innstillinger og bokmerker brukes videre. Ikke slett profilen for å oppgradere.

## Tastaturet ditt er en snarvei

| Handling | Tast |
| --- | --- |
| Ny / lukk fane | `Ctrl+T` / `Ctrl+W` |
| Privat / gjenåpne vanlig fane | `Ctrl+Shift+N` / `Ctrl+Shift+T` |
| Adresse / kommandoer | `Ctrl+L` / `Ctrl+K` |
| Bytt fane | `Ctrl+Tab` / `Ctrl+Shift+Tab` |
| Historikk / nedlastinger | `Ctrl+H` / `Ctrl+J` |
| Sidefelt / fokusmodus | `Ctrl+B` / `Ctrl+Shift+F` |
| Skriv ut / lukk vindu | `Ctrl+P` / `Alt+F4` |

## Kvalitet som kan etterprøves

I [den registrerte Windows-kjøringen]({url}) bestod **{policy} modell-/policykontroller**, **{layout} WPF-ikon-/tekstkontroller** og **{shell} kontroller av faktisk vindu/nettmotor**. Antall kontroller er ikke det samme som antall brukersituasjoner; 256 av policykontrollene dekker kombinasjoner av vilkår for hvilende faner.

Tester bruker egne midlertidige profiler og en HTTP-server bundet til `127.0.0.1`. Motorfeiltesten avslutter kun motorprosessen som tilhører den isolerte testprofilen. Den tester ikke banknettsteder, DRM-strømming, alle nedlastingsdialoger, alle grafikkdrivere eller ytelse mot andre nettlesere.

[Full kvalitetsrapport](docs/QUALITY-REPORT.md) · [Kjente begrensninger](docs/STATUS.md) · [Personvern](docs/PRIVACY.md) · [Arkitektur](docs/ARKITEKTUR.md) · [Endringslogg](CHANGELOG.md).

## Videre retning

Ambisjonen er å fortjene en plass ved siden av etablerte nettlesere. Før neste milepæl prioriteres hverdagsnettsteder, DPI/tilgjengelighet, nedlastingsdialoger, signert distribusjon og målbare ytelsestester. Fanegrupper, flere vinduer, synkronisering, passordbehandler og utvidelser er ikke levert i 0.3.0.

[Bidra](CONTRIBUTING.md) · [Rapporter en feil](https://github.com/Darschnid479/NovaBrowser/issues/new/choose) · [Sikkerhet](SECURITY.md) · [MIT-lisens](LICENSE).
''')
put('docs/QUALITY-REPORT.md',f'''
# NOVA 0.3.0 / valideringsrapport

Dato: 2026-10-04. [GitHub Actions-kjøring]({url}). Inngangscommit: `{commit}`. De materialiserte kildefilenes SHA-256 er i [evidence.json](validation/0.3.0/evidence.json); testkjøringen bruker samme materialiserte kilde som leveransen.

## Faktisk utført på Windows

| Område | Resultat | Bevis |
| --- | --- | --- |
| Modell, URL-policy, søkeforslag og profillagring | {policy} kontroller bestod | [Logg](validation/0.3.0/policy-tests.txt) |
| WPF-ikoner og tekstlayout | {layout} kontroller bestod | [Logg](validation/0.3.0/layout-tests.txt) |
| Faktisk vindu og WebView2 | {shell} kontroller bestod | [Logg](validation/0.3.0/shell-tests.txt) |
| Full løsning | Kompilert; advarsler behandles som feil | [Byggelogg](validation/0.3.0/build.txt) |
| Windows x64 | Selvstendig, usignert mappe publisert | [Publiseringslogg](validation/0.3.0/publish.txt) |

Kontrolltallet inkluderer 256 kombinasjoner av åtte vilkår for fanepause. Dette er regresjonsbevis, ikke en full sikkerhetsrevisjon eller en garanti for alle nettsteder.

## Nettmotoren ble faktisk startet

Integrasjonstesten lastet to kontrollerte HTTP-sider fra en lokal server bundet til 127.0.0.1. Den kontrollerte tilbake/frem-grunnlaget, hjem-til-nettside-navigering, lydstatus, normal/privat localStorage-isolasjon, ny privat økt, oppvåkning etter suspendering, lukking under initialisering og ny motor etter en bevisst krasjtest. Bare motoren for en unik midlertidig testprofil ble avsluttet. Ingen brukerprofil eller offentlig nettside ble brukt.

Suspendering er en best-effort-funksjon: testen sammenholder modellens tilstand med motorens faktiske tilstand og sjekker at aktivering gjenopptar siden. Dette beviser ikke en bestemt prosent minnebesparelse eller at alle sider kan suspenderes.

## Ikke bekreftet av disse testene

Manuell bruk av alle nedlastings-/PDF-/bokmerkedialoger, nettsteders før-lukking-varsler, skjermdeling og samtlige tillatelser, DRM/video, alle DPI-/flerskjermkombinasjoner, skjermlesere, langtidsstabilitet, installeroppførsel, kode-signering og sammenligninger med Chrome/Firefox. Bruk en etablert nettleser til kritiske oppgaver inntil relevante tester er gjennomført.

## Gjenskap

Kjør `TEST-ALT.cmd` på Windows. Den rene testen av vinduet besøker ingen nettsider. Ved separat bekreftelse kjøres nettmotortesten mot loopback og med en egen testprofil. Kildekodens normalprofil påvirkes ikke. Rålogger og bildehashes er bevart i `docs/validation/0.3.0/`.
''')
put('docs/STATUS.md','''
# Produktstatus / 0.3.0

Utviklingsversjon med kompilert Windows x64-kandidat og automatisert reell WPF/WebView2-validering. [Konkrete resultater](QUALITY-REPORT.md).

## Endret i denne versjonen

Standard Windows-tittellinje med ekte vindusknapper erstatter den feilutsatte egendefinerte rammen. Faner kan festes, flyttes, dupliseres, dempes og pauses. Adresseforslag beregnes lokalt. Nedlastinger har et eget panel. Profillagring er atomisk, med en tidligere gyldig kopi og synlig gjenoppretting etter feil. Private faner lagres ikke som nettleserøkter.

## Viktige begrensninger

- WebView2CompositionControl kan påvirke bildefrekvens og DRM-avspilling. Dette er ikke en ny nettmotor.
- Native før-lukking-varsler for ulagrede nettskjemaer er ikke en fullverdig fanelukkingsmekanisme i denne utgaven. Lagre arbeid før faner lukkes.
- Hvilende faner kan pause live-oppdateringer. Automatisk pause er av som standard og gir ingen garantert minnebesparelse. Faner med lyd, nedlasting, festing eller innvilgede tillatelser holdes utenfor automatisk pause.
- Nedlastingslisten er for inneværende økt, med opptil åtte aktive nedlastinger og inntil 100 poster. Private poster fjernes når eierfanen lukkes. Nedlastede filer på disken slettes ikke automatisk.
- JSON-bokmerkeimport støtter NOVAs eksportformat, ikke alle andre nettleseres HTML- eller databaseformater.
- Ferdigbygget distribusjon er usignert og krever WebView2 Runtime. Ingen automatiske appoppdateringer er implementert.
- Fanegrupper, passordbehandler, skysynkronisering, utvidelsesbutikk og flere samtidige appvinduer er ikke levert.

Den konkrete GUI-/nettmotortesten er et steg fremover fra tidligere kun kildekodekontroller, ikke dokumentasjon på at NOVA er best eller sikrest.
''')
put('docs/PRIVACY.md','''
# Lokal lagring og personvern / 0.3.0

Normal profil: `%LOCALAPPDATA%\\NOVA-Browser`. Den kan inneholde navn, innstillinger, bokmerker, NOVAs lokale besøksliste, vanlige faner, WebView2-data og feillogg. Appdata er ikke et kryptert passordhvelv. Ikke last opp profilmappen.

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
''')
put('docs/VISUELT.md',f'''
# Bildeopprinnelse

## Faktisk app / 0.3.0

`assets/app-0.3.0/` er generert av den kjørende Windows-appen i [denne valideringskjøringen]({url}), ikke av en HTML-modell eller bildegenerator.

- `*-window.png`: PrintWindow-opptak av appens native Windows-vindu. DWM/grafikkoppsettet på testmaskinen kan påvirke hva metoden tegner.
- `*-client.png`: RenderTargetBitmap av appens levende WPF-klientflate. Windows sin ytre tittellinje er ikke med.
- `nova-03-live-engine.png`: WebView2 CapturePreviewAsync av en kontrollert lokal HTTP-testside. Siden beskriver eksplisitt at den er en test. Ingen påstand om at en offentlig nettside ble besøkt.

Bildene bruker testprofil og testdata. [SHA-256 og byggekontekst](validation/0.3.0/evidence.json).

## Historisk presentasjonsmateriale / 0.2.0

`assets/previews/` og `preview.html` inneholder tydelig merkede HTML-rekonstruksjoner fra den tidligere presentasjonspakken. Dette er ikke opptak fra Windows-appen. `assets/screenshots/website-*` viser den tidligere HTML-landingssiden. De er ikke dokumentasjon på siste appversjon.

Et eget lokalt bilde kan tas med `TA-EKTE-SKJERMBILDE.bat`. Skjul private opplysninger og kontroller resultatet før godkjenning.
''')
put('docs/ROADMAP.md','''
# Veikart med kvalitetskrav

## 0.3 / levert utviklingsgrunnlag

Native vindusbetjening, lokale adresseforslag, festing/lyd/pausing av faner, nedlastingspanel, fokusmodus, bokmerkeutveksling og robust øktlagring. Automatiske Windows- og nettmotortester følger med. Se QUALITY-REPORT for bevis og avgrensninger.

## Neste milepæl / stabilitet til daglig bruk

Akseptansekrav: dokumenterte tester på minst to Windows-maskiner med ulik skalering; korrekt fokus og tastaturflyt; nedlasting, pause/fortsett/avbrudd og lagringsdialoger; skjemaer med ulagret arbeid; bokmerke-/PDF-dialoger; lyd/video og konto-pålogging på et representativt nettstedsett. Feil skal ha reproduksjonstrinn og regresjonstester.

## Distribusjon og vedlikehold

Før stabil release: velg og finansier kode-signering, installer og avinstaller uten å miste brukerdata utilsiktet, kontrollert oppdateringsflyt med tilbakeføring, avhengighetskontroll og tydelig sikkerhetskontakt. Ikke skjul en usignert utviklingspakke bak et «stable»-merke.

## Differensiering

Vurder fanegrupper/arbeidsområder, flere vinduer og import fra andre nettlesere etter at grunnlaget er stabilt. Ytelse skal måles med fast maskinvare, nettstedssett, minne/CPU og flere målinger. Ingen konkurranserangering før reproduserbare målinger finnes.

## Langsiktig

Synkronisering, passordlagring, utvidelser og plattformutvidelser krever egne design- og sikkerhetsbeslutninger. Ingen leveringsdato eller funksjon er lovet her.
''')
put('docs/RELEASE-NOTES.md','''
# NOVA 0.3.0 / development preview

Native Windows-vindusknapper og responsivt sidefelt. Festing, flytting, duplisering, demping og valgfri suspendering av faner. Lokale adresseforslag uten nettverksbasert autocomplete. Eget nedlastingspanel, fokusmodus, PDF/utskrift og JSON-bokmerkeutveksling. Atomisk profillagring, sikkerhetskopi, spørsmål etter unormal avslutning og forbedret motorfeilhåndtering.

Windows-bygg og den faktiske WPF/WebView2-integrasjonen er kontrollert i automatiserte tester. Se QUALITY-REPORT.md for presis testdekning. Usignert forhåndsutgave; WebView2 Runtime kreves. Ingen garanti for DRM, samtlige nettsteder eller bedre ytelse/sikkerhet enn etablerte nettlesere.

Lukk gammel utgave, behold profilen, og pakk ut hele programmet i en ny mappe. Ta en lokal sikkerhetskopi før viktig testing.
''')
old=text('CHANGELOG.md')
put('CHANGELOG.md','''# Endringslogg

## 0.3.0 / 2026-10-04

- Ekte Windows-tittellinje med lukk/minimer/maksimer; start maksimert og husk normal størrelse.
- Høyreklikkmeny for festing, flytting, duplisering, lyd og hvile av faner.
- Lokale adresseforslag med privat kontekst og tastaturbetjening.
- Nedlastingspanel med fremdrift, pause/fortsett/avbryt og visning i Utforsker.
- Fokusmodus, JSON-bokmerkeimport/-eksport og PDF/utskrift.
- Atomisk lagring, siste gyldige kopi, kontrollert profilgjenoppretting og krasjdeteksjon.
- Guarder for foreldede WebView2-kall, gjenoppstart av nettmotor og strengere tillatelseskontekst.
- Reell WPF/WebView2-integrasjonstest, skjermbilder og advarsler-som-feil i byggingen.

## Historikk fra tidligere leveranser

'''+old)
put('LES-MEG-FORST.txt','''
NOVA 0.3.0 / WINDOWS-NETTLESER

KILDEPAKKE: Pakk ut alt i en ny mappe. Kjor START-NOVA.cmd.
Du trenger Windows x64, .NET 10 SDK og Microsoft WebView2 Runtime.

FERDIG WINDOWS-PAKKE: Pakk ut alle filer og kjor NOVA.exe.
.NET er inkludert i den selvstendige pakken; WebView2 Runtime er separat.
Dette er en usignert utviklingsversjon, ikke en ferdig stabil release.

Lukk gammel NOVA for oppgradering. Ikke kopier gamle bin/obj/out-mapper.
Eksisterende profil brukes videre. Ta en lokal sikkerhetskopi mens appen
ikke kjorer. IKKE last opp profilmappen til GitHub.

NYTT: Native vindusknapper; fest/dupliser/demp faner via hoyreklikk;
lokale adresseforslag; nedlastinger (Ctrl+J); fokus (Ctrl+Shift+F);
robust lagring, valgfri fanehvile, bokmerkeutveksling og PDF/utskrift.

TESTER: TEST-ALT.cmd. Se docs/QUALITY-REPORT.md for faktiske testresultater.
NETTSIDE: SE-NETTSIDEN.bat.
GITHUB: LAST-OPP-NOVA-TIL-GITHUB.bat viser endringer for du skriver JA.
BAT-filen trenger hjelpefilene i tools; ikke flytt bare BAT-filen.

Ekte appbilder og bevis: docs/assets/app-0.3.0 og docs/validation/0.3.0.
Dette prosjektet bruker WebView2/Chromium, ikke en egen nettmotor.
''')
cmd('TEST-ALT.cmd',r'''
@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 goto missing
echo [1/4] VB policy- og lagringstester i midlertidige mapper ...
call dotnet run --project tests\NovaBrowser.Checks\NovaBrowser.Checks.vbproj -c Release
if errorlevel 1 goto failed
echo [2/4] WPF ikoner og tekst ...
call dotnet run --project tests\NovaBrowser.UiChecks\NovaBrowser.UiChecks.vbproj -c Release
if errorlevel 1 goto failed
echo [3/4] Hele losningen ...
call dotnet build NOVA.sln -c Release
if errorlevel 1 goto failed
echo [4/4] Ekte vindu med en separat testprofil ...
call dotnet run --project tests\NovaBrowser.ShellChecks\NovaBrowser.ShellChecks.vbproj -c Release --no-build -- artifacts\wpf
if errorlevel 1 goto failed
echo.
echo Vil du ogsa teste nettmotoren mot en lokal testserver?
echo Testen bruker en egen midlertidig profil og simulerer et krasj
 echo KUN i testmotoren. Ingen offentlig nettside eller normalprofil brukes.
choice /c JN /n /m "Kjor utvidet nettmotortest? J/N: "
if errorlevel 2 goto done
call dotnet run --project tests\NovaBrowser.ShellChecks\NovaBrowser.ShellChecks.vbproj -c Release --no-build -- artifacts\wpf --live
if errorlevel 1 goto failed
:done
echo FERDIG. Testbilder ligger i artifacts\wpf.
pause
exit /b 0
:missing
echo Installer .NET 10 SDK og WebView2 Runtime. Se START-HER.txt.
pause
exit /b 1
:failed
echo En test feilet. Se meldingen ovenfor. Ikke slett nettleserprofilen.
pause
exit /b 1
''')
for filename in ['START-NOVA.cmd','BYGG-EXE.cmd','AAPNE-FEILLOGG.cmd','START-HER.txt']:
    p=R/filename; t=p.read_text(encoding='utf-8-sig').replace('0.2.0','0.3.0')
    if filename.endswith('.cmd'):
        t=t.replace('ikoner, adressefelt og oppstartsveiviser','faner, fokus og robust lagring')
        p.write_bytes(t.replace('\r\n','\n').replace('\n','\r\n').encode('ascii'))
    else: p.write_text(t,encoding='utf-8')

# Preserve the established site design, replace the visual evidence with real WPF.
p=R/'docs/index.html'; html=p.read_text(encoding='utf-8').replace('0.2.0','0.3.0').replace('idæ','idé')
for name in ['midnight','dawn','forest','graphite','setup','settings']:
    html=html.replace('assets/previews/nova-'+name+'.png','assets/app-0.3.0/nova-03-'+name+'-client.png')
html=html.replace('DESIGNFORHÅNDSVISNING','KJØRENDE WPF-APP').replace('HTML-REKONSTRUKSJON / IKKE APP-SKJERMBILDE','FAKTISK WPF-KLIENTFLATE / WINDOWS')
html=html.replace('UI-en er rekonstruert fra prosjektet i HTML. Bildet er ikke tatt av en kjørende Windows-app.','Bildet er rendret fra den kjørende WPF-appen på Windows. Den ytre Windows-tittellinjen er ikke med i klientflaten.')
html=html.replace('HTML-rekonstruksjon av NOVAs startside','Faktisk WPF-klientflate fra NOVAs startside').replace('Designforhåndsvisning av NOVA','Kjørende WPF-klientflate fra NOVA').replace('Designforhåndsvisning av NOVAs','Kjørende WPF-klientflate fra NOVAs')
html=html.replace('Rekonstruert oppstartsveiviser &middot; Ikke kjørende app','Kjørende WPF-veiviser &middot; Windows-testprofil')
html=html.replace('Vertikale faner, private faner og gjenåpning av fanen du lukket litt for fort.','Fest, flytt, dupliser og demp faner. Private økter og gjenåpning av fanen du lukket litt for fort.')
html=html.replace('Adressefelt, søk og et kommandofelt. Med hurtigtaster som lar deg holde flyten.','Lokale adresseforslag, fokusmodus og et kommandofelt. Med hurtigtaster som lar deg holde flyten.')
html=html.replace('Faner, startside, temaer, veiviser, bokmerker, lokal besøksliste og kommandofelt.','Fanestyring, lokale forslag, nedlastingspanel, fokusmodus, temaer og robust lokal lagring.')
html=html.replace('Verifisere vindusknapper, skalering, tekstfelt, tilgjengelighet og nettleserøkter på Windows.','Utvid testene av hverdagsnettsteder, DPI, tilgjengelighet og nedlastingsdialoger på flere maskiner.')
html=html.replace('Fanegrupper, sovende faner, signerte utgivelser og oppdateringer. Planer, ikke tidsløfter.','Fanegrupper, flere vinduer, signerte utgivelser og oppdateringer. Planer, ikke tidsløfter.')
html=html.replace('Appbildene er merkede HTML-rekonstruksjoner basert på NOVA 0.3.0. De er ikke tatt av Windows-appen. Kildepakken inneholder et verktøy for å ta ekte skjermbilder på Windows.','Bildene på denne siden er rendret fra den faktiske WPF-appen på Windows. Klientbildene utelater den ytre Windows-tittellinjen. Repoet inneholder også native vindusopptak og en separat nettmotorfangst av en lokal testside.')
p.write_text(html,encoding='utf-8')
p=R/'docs/assets/site.js'; js=p.read_text(encoding='utf-8')
js=js.replace('assets/previews/nova-','assets/app-0.3.0/nova-03-').replace('${theme}.png','${theme}-client.png').replace('${key}.png','${key}-client.png')
# Keep native theme names; replace exact suffix when URL is constructed differently.
js=re.sub(r'(nova-03-\$\{[^}]+\})\.png',r'\1-client.png',js)
p.write_text(js,encoding='utf-8')

# Existing historical architecture notes are retained below a current map.
old=text('docs/ARKITEKTUR.md')
put('docs/ARKITEKTUR.md','''# Arkitektur / 0.3.0

MainWindow er delt i egne partial-klasser for vindu, faner, lokale forslag, nedlastinger, veiviser og nettstedsforespørsler. BrowserPolicies og ProfileRepository har ingen WPF-avhengighet og testes separat. UI er XAML og egen vektortegning; ingen ikonfont er nødvendig.

Vindusrammen eies av Windows. WPF eier startside/paneler, mens WebView2CompositionControl viser nettsiden. Hver fane har egen visning; visninger opprettes ved aktivering og avhendiges når fanen lukkes. Asynkrone oppstarter og motorhendelser kontrollerer fortsatt fane-/visningsidentitet før de endrer UI.

Nettleserprofiler og nedlastinger er ikke UI-data som kan sendes via vilkårlige websidemeldinger. Host objects og web messaging er deaktivert. Innvilgede tillatelser er knyttet til dokumentkontekst. Dette er defensive valg, ikke en sikkerhetssertifisering.

Profildata skrives med atomisk utskifting. Normal og privat kontekst skilles i øktlagring og lokale forslag. Ekstern runtime vedlikeholdes separat fra NOVA-appens kildekode.

## Historiske implementasjonsnotater fra tidligere versjoner

Notatene nedenfor kan omtale den tidligere egendefinerte vindusrammen og 0.2.0-teststatusen. Ved motstrid gjelder 0.3.0-koden og QUALITY-REPORT.

'''+old)
put('docs/TESTPLAN-0.3.0.md','''# Manuell akseptanse / neste testøkt

Dette er oppgaver som fortsatt må sjekkes manuelt, ikke beståtte tester.

1. Åpne appen på din Windows-maskin. Kontroller X/minimer/maksimer, dra vinduet, Snap og 100/125/150/200 prosent skalering. Prøv 820 x 600 og flere skjermer.
2. Besøk ufølsomme hverdagsnettsteder. Test tilbake/frem, Ctrl+L, adresseforslag, bokmerker, faner, fokus og temaer. Test førstegangsveiviser og avbryt fra innstillinger.
3. Last ned en ufarlig testfil. Avbryt lagringsdialogen, lagre, pause, fortsett, avbryt, vis filen i mappe. Test at en uferdig nedlasting gir varsel ved fanelukking. Ikke kjør ukjente filer.
4. Eksporter og importer NOVA-bokmerker, avbryt dialoger, prøv ugyldig JSON. Test PDF og vanlig utskrift fra en ufølsom side.
5. Test lyd/video, tastatur uten mus og skjermleser. Bekreft at automatiske hvilende faner er av som standard og at lyd, aktive nedlastinger og innvilgede tillatelser ikke settes i hvile.
6. Lagre arbeid før faner lukkes; støtte for nettstedets egne ulagret-arbeid-varsler er foreløpig begrenset. Registrer konkrete feil med trinn, versjon, Windows-utgave og anonymisert skjermbilde.

Ikke legg profiler, cookies, passord eller en uredigert feillogg i en offentlig GitHub-sak.
''')
# Checksums are generated later, after the exact deliverable files are finalized.
print('0.3.0 presentation uses real test outputs; no unverified test status was promoted')
