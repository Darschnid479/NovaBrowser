# Lokal lagring og personvern

Dette er en beskrivelse av kildekodens oppsett, ikke en uavhengig sikkerhetsrevisjon.

NOVA lagrer appens tilstand under `%LOCALAPPDATA%\NOVA-Browser`. Det kan omfatte navn, innstillinger, bokmerker, NOVAs besøksliste, vanlige faner, WebView2-profil og feillogg. Ikke last opp denne mappen til GitHub.

Det er ingen egen NOVA-konto eller NOVA-sky implementert. Det betyr ikke at alle nettverksforbindelser er borte: nettsteder, valgt søkemotor, WebView2 og runtime-oppdateringer kan ha egne nettverkstjenester og vilkår.

Valget om å ikke lagre **NOVAs besøksliste** er ikke det samme som å slette all data som nettmotoren lagrer. Private faner tas etter kodeoppsettet ikke med i NOVAs historikk/gjenoppretting. Privat nettlesing gjør deg ikke anonym for nettsteder, nettverkseier eller internettleverandør. Ikke stol på en uprøvd utviklingsversjon for sensitiv bruk.

Opplastingsverktøyet utelater vanlige runtime-/profilmappenavn, byggemapper og enkelte private filer. Det ser også etter noen velkjente token-/nøkkelmønstre. Det er **ikke** en garanti mot lekkasje. Skjermbilder og vanlige dokumenter kan inneholde persondata som et slikt mønstersøk ikke fanger opp.

Skjermbildeverktøyet lagrer først i `artifacts/screenshots-pending/`, som ikke tas med av opplasteren. Først etter at brukeren har sett bildet og skrevet `JA`, kopieres det til `docs/assets/screenshots/`. Bare det valgte NOVA-vinduets synlige rektangel tas med; overliggende varsler kan fremdeles komme med og må kontrolleres.
