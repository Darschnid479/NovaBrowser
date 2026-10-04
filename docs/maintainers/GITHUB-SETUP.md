# Sett opp GitHub-presentasjonen

## 1. Last opp

Kjør `LAST-OPP-NOVA-TIL-GITHUB.bat` fra den utpakkede mappen. README bruker relative bildestier; bildene vises når filene er i repoet. Dette krever ikke GitHub Pages.

## 2. Aktiver den separate nettsiden

På `Darschnid479/NovaBrowser`, åpne **Settings > Pages**. Under **Build and deployment > Source**, velg **GitHub Actions**.

Åpne deretter **Actions > Publish NOVA website > Run workflow**, velg `main` og start. En vellykket deployment viser nettadressen under Pages og i workflowens environment.

Forventet adresse for dette repoet er `https://darschnid479.github.io/NovaBrowser/`, men den er ikke omtalt som live før deployment lykkes. Etter aktivering publiserer workflowen nye endringer i `docs/` ved push til main. Før Pages er aktivert, hopper workflowen over publisering og skriver en forklaring i kjørerapporten.

## 3. About-feltet

Klikk tannhjulet ved **About** og bruk gjerne denne beskrivelsen:

> A personal Windows browser built with Visual Basic .NET, WPF and WebView2. Themes, vertical tabs and a first-run setup. Early development.

Legg inn Pages-adressen som Website **etter** vellykket publisering. Aktuelle topics: `browser`, `visual-basic`, `vbnet`, `wpf`, `webview2`, `windows`, `customization`.

## 4. Delingsbilde

Under **Settings > General > Social preview**, last opp `docs/assets/brand/social-card.png`. Det er 1280 x 640 piksler. Bildet i README er allerede koblet inn; GitHubs eget delingsbilde må angis separat i innstillingene.

## 5. Beskyttelse og rapportering

Vurder pull requests og påkrevde Windows-kontroller før main kan endres. Opplasteren stopper ved avvist push til en beskyttet gren. Aktiver privat sårbarhetsrapportering før du ber brukere sende sensitive feilrapporter. Ingen falsk supportadresse er lagt inn i dokumentasjonen.

## 6. Ikke lov en ferdig release for tidlig

En grønn Windows Actions-kjøring kan gi en testartefakt; dette er ikke automatisk en signert eller offentlig godkjent release. Se RELEASING.md før offentlig distribusjon.
