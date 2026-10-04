# Fra kildekode til releasekladd

Det finnes ingen ferdig, verifisert EXE i denne ZIP-filen.

1. Kjør Windows-bygg, modelltester og UI-tester. Test appen manuelt, inkludert vindusknapper, adresser, faner, veiviser, nettvisning og lukking.
2. Gjennomgå `docs/STATUS.md`, testresultater og ekte appbilder. Oppdater dokumentasjon som faktisk er blitt utdatert.
3. Oppdater versjonsnummer, endringslogg og release-notater ved en reell appendring. Denne pakken har bevart appversjon 0.2.0.
4. Opprett en Git-tag for committen som skal testes, for eksempel `v0.2.0` bare dersom den faktisk er committen som skal utgis.
5. Under Actions, kjør **Prepare NOVA release draft** med taggen. Workflowen bygger taggens kode og kontrollerer at versjonsnummeret stemmer.
6. Workflowen lager en **draft prerelease** med Windows ZIP og SHA-256. Den publiserer ikke utgivelsen offentlig. Kontroller innhold, test på en ren Windows-PC og legg til testresultater før eventuell publisering.

SHA-256 avdekker endrede bytes, men er **ikke** en digital signatur. Denne pipeline har ingen kode-signering. .NET-publisering er satt til self-contained; WebView2 Runtime må være installert separat. Behold hele den publiserte mappen.

Workflows bruker GitHubs innebygde `GITHUB_TOKEN` med jobbavgrensede rettigheter. Ikke legg personlige tokens eller signeringsnøkler i kode eller offentlige repo-filer.
