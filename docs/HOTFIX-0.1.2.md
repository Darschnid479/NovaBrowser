# NOVA 0.1.2 - WebView2 runtime hotfix

Dato: 2. oktober 2026. Basert paa NOVA-Browser-VB.NET-FIXED.zip.

## Det brukerens logg faktisk viser

System.IO.FileNotFoundException i
WebView2CompositionControl.TryInitializeD3DImage(), OnApplyTemplate(),
Loaded, SizeChanged og Dispose(). Feilen opptrer ogsaa under Window_Closing.
Den gamle loggeren tok bare med feiltype og kallstakk, ikke FileName,
HResult eller indre unntak. Loggen beviser derfor ikke hvilken DLL som mangler.

## Diagnose og grunnlag

Prosjektet brukte TargetFramework net10.0-windows. Dette angir ikke
Windows SDK-versjonen som CompositionControl trenger for WinRT-grafikken.
Microsofts dokumentasjon beskriver at et Windows-versjonsspesifikt maal
legger til riktig Windows SDK targeting-pakke. WebView2-feilrapporter har
samme kallstakk naar Microsoft.Windows.SDK.NET ikke blir inkludert.
Dette er en konkret feil i byggoppsettet og en sannsynlig forklaring paa
brukerens problem, ikke en bekreftet identifisering av filen i hans logg.

## Endringer

- TargetFramework: net10.0-windows10.0.17763.0.
- CopyLocalLockFileAssemblies=true; PublishTrimmed=false;
  PublishSingleFile=false. WebView2-pakkeversjonen er ikke endret.
- MSBuild stopper bygg/publisering dersom Microsoft.Windows.SDK.NET.dll
  eller WinRT.Runtime.dll mangler fra resultatmappen.
- Program.vb proever aa laste disse modulene foer hovedvinduet opprettes.
- En felles ReleaseTabView rydder opp ved feil, retry, fanelukking og avslutning.
  Eventuelle Dispose-feil logges uten aa kaste dem videre.
- Feil paa UI-traaden viser maksimalt en fatal dialog per programkjoering.
- Ved fatal feil stoppes lagring; en delvis krasjtilstand skal ikke overskrive
  den lagrede fanesesjonen. Normal avslutning beholder normal lagring.
- Loggen inkluderer HResult, en avgrenset liste med kjente modulnavn,
  underliggende unntak og versjon/arkitektur for kjoeremiljoeet.
  Vilkaarlig Exception.Message, Data, nettadresser og dokumentnavn dumpes ikke.
  Kallstakker kan likevel inneholde lokale kildekodestier/brukernavn.
- START-NOVA.cmd og BYGG-EXE.cmd oppdaterer avhengighetene ved restore.
- AAPNE-FEILLOGG.cmd aapner den lokale feilloggen uten aa endre den.
- Tretten nye kodekontroller for diagnostikk er lagt til eksisterende 49.
  Forventet antall ved vellykket kjoering er 62. De er ikke kjoert her.

## Oppgradering

Lukk gammel NOVA, pakk ut i en ny mappe, og start START-NOVA.cmd derfra.
Ikke flytt gamle byggemapper eller bare NOVA.exe over i den nye mappen.
Ikke slett den lokale nettleserprofilen. Ingen innstillinger migreres eller
nullstilles av denne rettelsen. ZIP-en inneholder kildekode, ikke ferdig EXE.

## Kontroller paa Windows som fortsatt maa kjoeres

1. START-NOVA.cmd maa lykkes med restore, kodekontroller, bygging og oppstart.
2. Naviger til en vanlig HTTPS-side; startsiden alene starter ikke WebView2.
3. Aapne og bytt flere faner, inkludert en privat fane.
4. Endre vindusstoerrelse og maksimer med en nettside synlig.
5. Vis innstillinger og kommandofelt over en nettside.
6. Lukk fane/vindu under oppstart av en nettside; ingen feildialogkaskade.
7. BYGG-EXE.cmd maa lage komplett win-x64-mappe med begge WinRT-modulene.
8. Start den NYE publiserte EXE-filen, og besoek en nettside igjen.
9. Bruk en separat kopi av publisert mappe for negativ test: flytt midlertidig
   Microsoft.Windows.SDK.NET.dll ut av den kopien. Oppstart skal gi en
   forstaaelig manglende-komponentfeil og en logg med biblioteknavnet.
   Gjenopprett filen i testkopien etterpaa. Ikke endre nettleserprofilen.

Disse testene er ikke markert som bestaatt. Se VERIFISERING.txt for
faktiske kontroller i leveransemiljoeet.

## Kilder brukt til rettelsen

Microsoft Learn - Windows-versjonsspesifikt TFM for WinRT:
https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps

MicrosoftEdge/WebView2Feedback #5436 - samme avhengighet og kjent feil:
https://github.com/MicrosoftEdge/WebView2Feedback/issues/5436

MicrosoftEdge/WebView2Feedback #5702 - samme TryInitializeD3DImage-kallstakk:
https://github.com/MicrosoftEdge/WebView2Feedback/issues/5702

Microsoft Learn - CopyLocalLockFileAssemblies:
https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#copylocallockfileassemblies

Microsoft Learn - FileNotFoundException.FileName:
https://learn.microsoft.com/en-us/dotnet/api/system.io.filenotfoundexception.filename

Kilder begrunner rettelsen, men erstatter ikke bygging og kjoering av NOVA.
