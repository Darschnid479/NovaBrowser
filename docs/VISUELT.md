# Bilder og opprinnelse

## Ingen oppdiktede app-skjermbilder

Pakken har ikke et brukbart, fullstendig skjermbilde av den kjørende NOVA-appen. `assets/previews/` inneholder **HTML-rekonstruksjoner**, tydelig merket nederst i hvert bilde og ved bruk i README/nettsiden. De viser en designstudie basert på kildekoden, ikke dokumentasjon på at vindusbetjening, WebView2 eller andre funksjoner virker.

| Ressurs | Hva den viser |
| --- | --- |
| `nova-midnight.png` | Startside med Midnight + Iris. |
| `nova-dawn.png` | Startside med Dawn + Iris. |
| `nova-forest.png` | Startside med Forest + Mint. |
| `nova-graphite.png` | Startside med Graphite + Blue. |
| `nova-setup.png` | Rekonstruert søkemotortrinn. Eksempelvalg, ikke et ekte oppsett. |
| `nova-settings.png` | Rekonstruert utsnitt av innstillingspanelet. |

Farger er hentet fra `src/NovaBrowser/UI/ThemeManager.vb`. Ikonbaner er hentet fra `UI/NovaIcon.vb`. Oppsettet er basert på `MainWindow.xaml`; HTML/CSS er ikke WPF, og geometri, skriftgjengivelse og detaljer kan avvike. Vist tidspunkt og navn er demonstrasjonsverdier, ikke nettleseraktivitet. Søkeleverandørene illustreres med bokstavmerker, ikke lånte varemerkelogoer.

## Ekte skjermbilder av nettsiden

`assets/screenshots/website-desktop.png` og `website-mobile.png` er tatt av den medfølgende HTML-landingssiden, rendret i Chromium ved henholdsvis 1440 og 390 piksler bredde. De er **ikke** skjermbilder fra Windows-appen. Ressursene ble lastet lokalt i minnet under kontrollen; ingen nettsteder ble besøkt av NOVA.

`assets/brand/social-card.png` er et typografisk banner, rendret fra `tools/brand-artwork.html`. Merket er basert på prosjektets eksisterende `Nova.ico`. Ingen fontfiler eller eksterne stockbilder inngår.

## Lag ekte Windows-bilder

1. Start NOVA med `START-NOVA.cmd` og åpne riktig visning. Fjern private adresser og varsler.
2. Kjør `TA-EKTE-SKJERMBILDE.bat`. Bekreft opptaket og hold NOVA fremst under nedtellingen.
3. Se bildet som åpnes. Skriv `JA` bare når det trygt kan publiseres. Bildet kopieres da til docs-mappen.
4. Sett inn filen i README med tekst som angir versjon, tema, Windows-versjon og skalering. Fjern "HTML-rekonstruksjon" **bare for bilder som faktisk er tatt av appen**.

Verktøyet krever Windows og er ikke kjørt i dette byggemiljøet. Det erstatter ikke bildene automatisk, og laster ikke opp noe på egen hånd.
