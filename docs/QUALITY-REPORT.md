# NOVA 0.3.0 / valideringsrapport

> **Revalideringsmerknad:** En senere PR-kjøring viste at en nylig skrevet nettsideverdi kunne gå tapt ved tvungen avslutning av hele testmotorens prosesstre. Gjenoppretting av motor/vindu er derfor ikke en garanti for at nettstedets siste skrivinger overlever. Testen er presisert, og den opprinnelige observasjonen er bevart. [Detaljer og testavgrensning](validation/0.3.0/REVALIDATION.md). Tabellen nedenfor dokumenterer den opprinnelige kjøringen; den oppdaterte testen har 57 vindus-/nettmotorkontroller. Se PR #4 for siste kjøring.

Dato: 2026-10-04. [GitHub Actions-kjøring](https://github.com/Darschnid479/NovaBrowser/actions/runs/37202764701). Inngangscommit: `4431a2e75625e5df085b5f80e7f7bbaab44e1bdb`. De materialiserte kildefilenes SHA-256 er i [evidence.json](validation/0.3.0/evidence.json); appkoden som ble testet er den samme som i leveransen. Integrasjonstestens senere presisering er beskrevet over.

## Faktisk utført på Windows

| Område | Resultat | Bevis |
| --- | --- | --- |
| Modell, URL-policy, søkeforslag og profillagring | 407 kontroller bestod | [Logg](validation/0.3.0/policy-tests.txt) |
| WPF-ikoner og tekstlayout | 116 kontroller bestod | [Logg](validation/0.3.0/layout-tests.txt) |
| Faktisk vindu og WebView2 | 56 kontroller bestod i denne opprinnelige kjøringen | [Logg](validation/0.3.0/shell-tests.txt) |
| Full løsning | Kompilert; advarsler behandles som feil | [Byggelogg](validation/0.3.0/build.txt) |
| Windows x64 | Selvstendig, usignert mappe publisert | [Publiseringslogg](validation/0.3.0/publish.txt) |

Kontrolltallet inkluderer 256 kombinasjoner av åtte vilkår for fanepause. Dette er regresjonsbevis, ikke en full sikkerhetsrevisjon eller en garanti for alle nettsteder.

## Nettmotoren ble faktisk startet

Integrasjonstesten lastet to kontrollerte HTTP-sider fra en lokal server bundet til 127.0.0.1. Den kontrollerte tilbake/frem-grunnlaget, hjem-til-nettside-navigering, lydstatus, normal/privat localStorage-isolasjon, ny privat økt, oppvåkning etter suspendering, lukking under initialisering og ny motor etter en bevisst krasjtest. Bare motoren for en unik midlertidig testprofil ble avsluttet. Ingen brukerprofil eller offentlig nettside ble brukt.

Suspendering er en best-effort-funksjon: testen sammenholder modellens tilstand med motorens faktiske tilstand og sjekker at aktivering gjenopptar siden. Dette beviser ikke en bestemt prosent minnebesparelse eller at alle sider kan suspenderes.

## Ikke bekreftet av disse testene

Manuell bruk av alle nedlastings-/PDF-/bokmerkedialoger, nettsteders før-lukking-varsler, skjermdeling og samtlige tillatelser, DRM/video, alle DPI-/flerskjermkombinasjoner, skjermlesere, langtidsstabilitet, installeroppførsel, kode-signering og sammenligninger med Chrome/Firefox. Nytt nettstedslager er ikke garantert bevart ved tvungen prosessavslutning. Bruk en etablert nettleser til kritiske oppgaver inntil relevante tester er gjennomført.

## Gjenskap

Kjør `TEST-ALT.cmd` på Windows. Den rene testen av vinduet besøker ingen nettsider. Ved separat bekreftelse kjøres nettmotortesten mot loopback og med en egen testprofil. Kildekodens normalprofil påvirkes ikke. Rålogger og bildehashes er bevart i `docs/validation/0.3.0/`.
