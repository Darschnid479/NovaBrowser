# Bildeopprinnelse

## Faktisk app / 0.3.0

`assets/app-0.3.0/` er generert av den kjørende Windows-appen i [denne valideringskjøringen](https://github.com/Darschnid479/NovaBrowser/actions/runs/37202764701), ikke av en HTML-modell eller bildegenerator.

- `*-window.png`: PrintWindow-opptak av appens native Windows-vindu. DWM/grafikkoppsettet på testmaskinen kan påvirke hva metoden tegner.
- `*-client.png`: RenderTargetBitmap av appens levende WPF-klientflate. Windows sin ytre tittellinje er ikke med.
- `nova-03-live-engine.png`: WebView2 CapturePreviewAsync av en kontrollert lokal HTTP-testside. Siden beskriver eksplisitt at den er en test. Ingen påstand om at en offentlig nettside ble besøkt.

Bildene bruker testprofil og testdata. [SHA-256 og byggekontekst](validation/0.3.0/evidence.json).

## Historisk presentasjonsmateriale / 0.2.0

`assets/previews/` og `preview.html` inneholder tydelig merkede HTML-rekonstruksjoner fra den tidligere presentasjonspakken. Dette er ikke opptak fra Windows-appen. `assets/screenshots/website-*` viser den tidligere HTML-landingssiden. De er ikke dokumentasjon på siste appversjon.

Et eget lokalt bilde kan tas med `TA-EKTE-SKJERMBILDE.bat`. Skjul private opplysninger og kontroller resultatet før godkjenning.
