# Revalidering av 0.3.0 etter første komplette Windows-bygg

Den opprinnelige valideringskjøringen `37202764701` bestod 407 modell-/policykontroller, 116 WPF-layoutkontroller og 56 vindus-/nettmotorkontroller. Disse historiske loggene og bildene er bevart uendret.

Den påfølgende ordinære PR-kjøringen `37202999132` avdekket en viktig avgrensning: etter en bevisst, brå terminering av hele testmotorens prosesstre ble nettmotoren gjenopprettet, men en nylig skrevet `localStorage`-verdi på testsiden var ikke bevart. Den opprinnelige testen antok at denne verdien alltid ville overleve. Denne antakelsen var for sterk og er ikke en egenskap NOVA har implementert eller dokumentert som garantert.

## Hva som er endret

Bare integrasjonstesten og dokumentasjonen er endret i denne revalideringen. Selve appens VB.NET/XAML-kode er uendret fra Windows-bygget.

Testen registrerer nå eksplisitt verdien som ble observert etter den tvungne termineringen. Den kontrollerer at ingen private data dukker opp i normal kontekst, at nettstedslagring igjen er skrivbar og lesbar, og at vindu/fanedata og funksjonalitet overlever motorens omstart. Dette gir 57 vindus-/nettmotorkontroller. Den rå observasjonen om hvorvidt den gamle verdien overlevde, forblir synlig i testloggen.

Det er ikke lagt inn en kunstig ventetid eller gjentatte forsøk for å skjule datatapet. En grønn kjøring skal her bety korrekt avgrenset gjenoppretting, ikke at nettsteders siste skrivinger alltid overlever en tvungen prosessavslutning.

## Betydning for brukeren

**NOVA lover ikke å bevare nettstedets nylige lokale data eller ulagrede arbeid ved et hardt krasj eller tvungen avslutning.** Bruk vanlig lukking og lagre viktig arbeid. NOVAs egen atomiske innstillings-/øktfil er separat fra nettsteders WebView2-lagring. Stabil lagring ved vanlige avslutninger og flere hverdagsnettsteder krever videre tester; testen her er ikke en full datavarighetsrevisjon.

Den siste statusen for de nye kontrollene finnes under PR #4 og workflowen «Build NOVA on Windows». Eventuelle nye logger ligger i artefakten «NOVA-validation-evidence» for den aktuelle kjøringen.
