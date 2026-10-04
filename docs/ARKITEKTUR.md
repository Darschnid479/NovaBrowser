# Arkitektur / 0.3.0

MainWindow er delt i egne partial-klasser for vindu, faner, lokale forslag, nedlastinger, veiviser og nettstedsforespørsler. BrowserPolicies og ProfileRepository har ingen WPF-avhengighet og testes separat. UI er XAML og egen vektortegning; ingen ikonfont er nødvendig.

Vindusrammen eies av Windows. WPF eier startside/paneler, mens WebView2CompositionControl viser nettsiden. Hver fane har egen visning; visninger opprettes ved aktivering og avhendiges når fanen lukkes. Asynkrone oppstarter og motorhendelser kontrollerer fortsatt fane-/visningsidentitet før de endrer UI.

Nettleserprofiler og nedlastinger er ikke UI-data som kan sendes via vilkårlige websidemeldinger. Host objects og web messaging er deaktivert. Innvilgede tillatelser er knyttet til dokumentkontekst. Dette er defensive valg, ikke en sikkerhetssertifisering.

Profildata skrives med atomisk utskifting. Normal og privat kontekst skilles i øktlagring og lokale forslag. Ekstern runtime vedlikeholdes separat fra NOVA-appens kildekode.

## Historiske implementasjonsnotater fra tidligere versjoner

Notatene nedenfor kan omtale den tidligere egendefinerte vindusrammen og 0.2.0-teststatusen. Ved motstrid gjelder 0.3.0-koden og QUALITY-REPORT.

# Tillegg: arkitektur i 0.2.0

NovaIcon tegner frosne geometrier uten tekst. MainWindow.Setup eier veiviserens
visning; SetupSession inneholder en separat kopi og trinnkontroll uten WPF.
SearchProviders er felles katalog for veiviser, innstillinger og UrlPolicy.
Ved første oppstart skilles vindusinnlasting fra StartBrowsingSession.
PersistState avbryter hvis fanesesjonen aldri har startet, slik at avbrutt
oppsett ikke kan erstatte den gamle sesjonen med en tom liste.

---

# Arkitektur og videre utvikling

## Lag

Program.vb oppretter WPF-applikasjonen og hindrer to samtidige appforekomster.
En delt WebView2-environment ligger i en cachet Task. Hver nettleserfane har sin egen
WebView2CompositionControl, lokal tilstand og nummerering av navigasjonsforespørsler.
Nummereringen gjør at en gammel oppstart ikke overstyrer en nyere adresse brukeren skrev.
Lukkede faner markeres før nettmotoren frigjøres.

Startsiden er native WPF, ikke HTML lastet fra nettet. Nettsteder får ingen host objects,
ingen JavaScript-bro til .NET og ingen lokal filhåndtering via adressefeltet.
WebView2 håndterer parsing, rendering, JavaScript, TLS og nettverk. NOVA eier
vinduskrom, faner, presentasjon og egen lokal tilstand.

Hovedvinduet er delt over tre partial-klasser av praktiske grunner. XAML står for
presentasjonen; .vb står for all applikasjonslogikk. En større produksjonsutgave
bør flytte mer av fanestyring og kommandoer til egne tjenester/visningsmodeller.

## Tilstand

StateStore lagrer vanlig JSON gjennom en midlertidig fil og bytter filen ved oppdatering.
Skrivinger samles med en kort DispatcherTimer. Sluttlagring skjer før vinduet lukkes.
Maks 500 bokmerker, 500 historikkoppføringer og 50 lagrede faner.
Uleselig tilstand prøves bevart med et tidsstemplet .corrupt-navn før standardvalg brukes.

Gjenopprettede faner opprettes som modeller uten å starte alle nettmotorer med en gang.
En motor som først er startet, beholdes til fanen lukkes. Dette er ikke en implementasjon
av automatisk fanehvile. Ingen sideinnhold, passord eller informasjonskapsler kopieres inn i JSON.
Vanlige adresser og titler lagres likevel, og kan inneholde sensitive opplysninger.

## Sikkerhetsvalg

- Ingen `--no-sandbox`, ignorering av sertifikatfeil eller universell tillatelse til kamera/mikrofon.
- Host objects og WebMessage-broen er deaktivert. Ingen automatisk skriptinjeksjon.
- Bare brukerutløste vindusforespørsler får ny fane. Samme environment/profil brukes
  med GetDeferral og NewWindow for å beholde den normale vindusrelasjonen.
- Tillatelser får et eget WPF-spørsmål. Standard er nei. Ukjente og uønskede
  bakgrunnsforespørsler avvises. Varige tillatelser lagres ikke.
- Private faner bruker IsInPrivateModeEnabled på en separat profil. Denne blir ikke
  brukt i NOVAs vanlige besøksliste, nylig-lukket-liste eller gjenoppretting.
- Sletting ber om bekreftelse. Filer lastes aldri automatisk ned og kjøres som kode av NOVA.
  Selve WebView2-nedlastingsopplevelsen beholder nettmotorens standardoppførsel.
- Nettmotorens tastaturhandlinger som kan gjøre mer arbeid, sendes tilbake gjennom
  Dispatcher slik at COM-hendelsen kan avsluttes først.

Dette er begrensninger i verten, ikke en sikkerhetssertifisering. Verten trenger fortsatt
Windows-testing, sikkerhetsgjennomgang, testing mot ondsinnede sider, tilgjengelighetstesting
og vedlikehold av alle avhengigheter før bred distribusjon.

## Naturlige neste utviklingstrinn

Først: få et grønt Windows-bygg og kjør testplanen på reelle maskiner.
Deretter: automatisert UI-testing, ytelsesmålinger av flere faner, valgfri vanlig
WebView2-visning for DRM/ytelse, bedre nedlastingsside og mer detaljert tillatelsesstyring.
Til slutt: signert installer, sikker oppdateringskanal og flere testede profiler.
Dette er forslag, ikke funksjoner som er ferdig implementert i 0.1.2.

## Runtime-avhengigheter i 0.1.2

Prosjektet bruker et Windows-versjonsspesifikt .NET-maal for at WinRT-
projeksjonen skal tas med. MSBuild kontrollerer to noedvendige DLL-er
etter bygging og publisering; Program.vb proever aa laste dem foer WPF
hovedvinduet starter. Dette erstatter ikke en faktisk test av Direct3D,
WebView2 Runtime eller visning av en nettside. Se HOTFIX-0.1.2.md.
Unntak fra normal sluttlagring: ved fatal UI-feil skal delvis krasjtilstand
ikke lagres. Feilloggen utelater vilkaarlige meldinger og ukjente filnavn.
