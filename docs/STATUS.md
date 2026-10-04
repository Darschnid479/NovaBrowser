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
