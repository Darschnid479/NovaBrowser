# Kvalitetsrapport / GitHub Pro-pakke

Dato: 2026-10-04. Appkilde: 0.2.0. Rapporten gjelder presentasjonspakken, ikke en godkjenning av nettleseren til produksjonsbruk.

## Faktisk utført i leveransens miljø

| Kontroll | Resultat |
| --- | --- |
| Eksisterende `tools/check_source.py` | 182 kilde-/XAML-påstander bestått. Ingen Windows-kjøring inngår. |
| `tools/check_package.py` | Bestått; lokale HTML-/README-ressurser, etiketter, PNG-format, hjelpefiler og grunnleggende pakkekrav. Antall avhenger av antall filer. |
| `tools/test_site.py` | 103 Chromium/HTML-kontroller bestått. Bredder: 1440, 1024, 768, 390 og 320 piksler. |
| JavaScript | `node --check` bestått for begge JS-filer. |
| YAML | Alle 8 workflow-/saksmal-/Dependabot-filer kunne parses. Parsing er ikke en ekte Actions-kjøring. |
| Appens kilde/assets | Alle 19 filer i `src/` er byte-for-byte identiske med den tidligere GitHub-Ready-pakken for 0.2.0. |
| Bilder | Seks forskjellige, merkede UI-rekonstruksjoner rendret; ekte landingssidebilder tatt ved 1440 og 390 piksler bredde. |
| Visuell kontroll | PC- og mobilutseende, oppstartsforhåndsvisning, bildeetiketter og sidedisposisjon kontrollert visuelt. |
| ZIP | Kontrolleres ved pakking for lesbarhet, komplett innhold og fravær av byggemapper/profildata. |

Nettleserkontrollene sjekket blant annet fire temavalg, oppdatert bilde og tekst, ett aktivt temavalg, FAQ, lastede bilder, fravær av horisontal overflyt, redusert bevegelse, tastaturfokus, riktige fargepaletter og synlige bildemerkinger.

Ressursene ble rendret fra lokale data i minnet. Det kjørte ingen NOVA/WebView2-økt, og denne kontrollen besøkte ingen eksterne nettsteder med NOVA.

## Ikke kjørt eller ikke bekreftet

- Kompilering/kjøring av VB.NET/WPF-appen på Windows.
- Eksisterende .NET modell-/adressekontroller og WPF UI-kontroller i denne sesjonen.
- Faktisk kjøring av BAT, PowerShell-opplaster, PowerShell-integrasjonstester og Windows-skjermbildeverktøyet. De er kildekontrollert, men ikke runtime-verifisert her.
- En ekte push, GitHub Actions-kjøring, Pages-deployment eller releaseopprettelse. Ingen repository-filer ble publisert som del av pakkearbeidet.
- Retting av brukerens rapport om usynlige lukk-/maksimeringsknapper. Appkoden er uendret.
- En uavhengig sikkerhetsrevisjon, full tilgjengelighetsrevisjon, DRM-/nettstedskompatibilitetstest eller ytelsessammenligning mot Chrome/Firefox.

## Gjenta kontroller

```text
python tools/check_source.py
python tools/check_package.py
node --check docs/assets/site.js
node --check docs/assets/preview.js
```

Valgfri visuell test: installer Python-pakken Playwright og en støttet Chromium, og kjør `python tools/test_site.py`. `NOVA_CHROMIUM` kan settes til en eksisterende Chromium-sti. `python tools/render_previews.py` regenererer de merkede HTML-bildene og nettsideskjermbildene. Ingen av disse kjører Windows-appen.

Windows-workflowen er konfigurert til å kjøre modell-/UI-kontrollene og de lokale Git-integrasjonstestene. Først et faktisk vellykket kjøreresultat kan omtales som bestått.

`SHA256SUMS.txt` gjelder byteinnholdet i den leverte kildepakken og utelater seg selv. Git kan normalisere linjeskift ved checkout; en slik Git-kopi trenger ikke ha de samme råhashene. Hashsummer er ikke digitale signaturer.
