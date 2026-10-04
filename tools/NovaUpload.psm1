Set-StrictMode -Version Latest

function Invoke-NovaGit {
    param([string[]]$GitArguments, [switch]$AllowMissing)
    $old = $ErrorActionPreference
    try {
        # Windows PowerShell 5.1 wraps native stderr in ErrorRecord objects.
        # Git exit codes, not progress written to stderr, determine success.
        $ErrorActionPreference = 'Continue'
        $lines = & git @GitArguments 2>&1
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $old }
    if ($code -ne 0 -and -not ($AllowMissing -and $code -eq 1)) {
        throw ("Git feilet (kode {0}): {1}" -f $code, (($lines | ForEach-Object { $_.ToString() }) -join "`n"))
    }
    if ($code -ne 0) { return '' }
    return (($lines | ForEach-Object { $_.ToString() }) -join "`n")
}

function Copy-NovaPackage {
    param([string]$SourceRoot, [string]$DestinationRoot)
    $skipDirs = @('.git','.vs','.vscode','.idea','bin','obj','out','artifacts','node_modules','__pycache__','.venv','WebView2','profiles','logs')
    $skipFiles = @('state.json','error.log','Cookies','History','Login Data','Local State','.DS_Store','Thumbs.db')
    $textExt = @('.vb','.xaml','.xml','.vbproj','.sln','.json','.yml','.yaml','.md','.txt','.ps1','.psm1','.bat','.cmd','.py','.js','.css','.html','.svg','.config','.props','.targets')
    $secretPattern = '(?m)-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----|\bgh[pousr]_[A-Za-z0-9]{30,}\b|\bgithub_pat_[A-Za-z0-9_]{40,}\b|\bAKIA[A-Z0-9]{16}\b'
    $copied = 0
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $pending.Push($SourceRoot)
    while ($pending.Count -gt 0) {
        $folder = $pending.Pop()
        foreach ($item in Get-ChildItem -LiteralPath $folder -Force) {
            if ($item.Name -eq '.git') { continue } # Also exclude the .git file used by worktrees.
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Lenke/junction stoppet: $($item.FullName). Bruk vanlige filer i kildepakken."
            }
            if ($item.PSIsContainer) {
                if ($skipDirs -notcontains $item.Name) { $pending.Push($item.FullName) }
                continue
            }
            if ($item.Name -match '^state\.json($|\.)') { continue }
            if ($skipFiles -contains $item.Name -or $item.Extension -match '^\.(log|tmp|pdb|dll|exe|user|suo|zip|7z)$') { continue }
            if ($item.Name -match '^(\.env($|\.)|id_(rsa|ed25519|ecdsa)$|credentials\.json$|secrets\.json$)' -and $item.Name -ne '.env.example') {
                throw "Mulig privat fil stoppet: $($item.Name). Flytt den ut av pakken og prov igjen."
            }
            if ($item.Extension -match '^\.(pfx|p12|pem|key)$') { throw "Nokkel-/sertifikatfil stoppet: $($item.Name)" }
            if ($item.Length -gt 45MB) { throw "Fil over 45 MB stoppet: $($item.Name). Binaere utgivelser skal i Releases, ikke kildekoden." }
            if ($textExt -contains $item.Extension -or $item.Name -in @('.gitignore','.gitattributes','.editorconfig','LICENSE')) {
                $text = [IO.File]::ReadAllText($item.FullName)
                if ($text -match $secretPattern) { throw "Mulig hemmelig nokkel stoppet i $($item.Name). Verdien blir ikke skrevet ut." }
            }
            $relative = $item.FullName.Substring($SourceRoot.Length).TrimStart([char[]]@('\','/'))
            $target = Join-Path $DestinationRoot $relative
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
            Copy-Item -LiteralPath $item.FullName -Destination $target -Force
            $copied++
        }
    }
    return $copied
}

function Invoke-NovaUpload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$SourceRoot,
        [string]$RepoUrl = 'https://github.com/Darschnid479/NovaBrowser.git',
        [string]$AuthorName = '', [string]$AuthorEmail = '',
        [switch]$PreviewOnly,
        [scriptblock]$Approve
    )
    $ErrorActionPreference = 'Stop'
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Installer Git for Windows: https://git-scm.com/download/win' }
    $SourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path.TrimEnd([char[]]@('\','/'))
    foreach ($required in @('NOVA.sln','README.md','.gitignore','src/NovaBrowser/NovaBrowser.vbproj')) {
        if (-not (Test-Path -LiteralPath (Join-Path $SourceRoot $required))) { throw "Feil mappe. Mangler $required. Pakk ut HELE ZIP-filen forst." }
    }
    Write-Host "`nNOVA / LAST OPP KILDEPAKKEN" -ForegroundColor Cyan
    Write-Host "Mal: $RepoUrl"
    Write-Host 'Det gjores ingen force-push. Eksterne filer som bare finnes pa GitHub beholdes.'
    Write-Host 'Endrede filer med samme navn blir vist for godkjenning. En nodvendig sletting gjores ikke av dette verktoyet.'
    Write-Host "`n[1/5] Leser repository. Fullfor eventuell GitHub-innlogging."
    $heads = Invoke-NovaGit -GitArguments @('ls-remote','--heads',$RepoUrl)
    $hasHeads = -not [string]::IsNullOrWhiteSpace($heads)
    $hasMain = $heads -match '(?m)\srefs/heads/main\s*$'
    if ($hasHeads -and -not $hasMain) { throw 'Repoet har innhold, men ingen main-gren. Avklar riktig gren for du laster opp.' }
    $work = Join-Path ([IO.Path]::GetTempPath()) ('NOVA-GitHub-upload-' + [Guid]::NewGuid().ToString('N'))
    Write-Host "[2/5] Lager separat arbeidskopi: $work"
    $clone = @('clone','--quiet')
    if ($hasMain) { $clone += @('--branch','main','--single-branch') }
    $clone += @('--',$RepoUrl,$work)
    Invoke-NovaGit -GitArguments $clone | Out-Null
    try {
        if (-not $hasHeads) { Invoke-NovaGit -GitArguments @('-C',$work,'symbolic-ref','HEAD','refs/heads/main') | Out-Null }
        # Refuse symbolic links in the remote tree before copying into the clone.
        $index = Invoke-NovaGit -GitArguments @('-C',$work,'ls-files','--stage')
        if ($index -match '(?m)^120000 ') { throw 'Repoet inneholder symbolske lenker. Vurder dem manuelt for opplasting.' }
        Write-Host '[3/5] Kopierer kildefiler, filtrerer lokale data og ser etter enkelte kjente hemmelighetsmonstre.'
        $count = Copy-NovaPackage -SourceRoot $SourceRoot -DestinationRoot $work
        Invoke-NovaGit -GitArguments @('-C',$work,'add','--all','--','.') | Out-Null
        $status = Invoke-NovaGit -GitArguments @('-C',$work,'diff','--cached','--name-status')
        if ([string]::IsNullOrWhiteSpace($status)) {
            Write-Host 'Ingen endringer. Ingenting ble lastet opp.' -ForegroundColor Green
            return @{ Uploaded=$false; Changed=$false; WorkDirectory=$work }
        }
        Write-Host "`n[4/5] Kontroller endringene ($count pakkefiler vurdert):"
        Write-Host $status
        Write-Host (Invoke-NovaGit -GitArguments @('-C',$work,'diff','--cached','--stat'))
        Write-Host "`nSe hele forskjellen ved behov: git -C `"$work`" diff --cached"
        Write-Host 'Kontrollen finner ikke alle typer persondata eller hemmeligheter. Se spesielt gjennom skjermbilder og dokumenter.' -ForegroundColor Yellow
        Write-Host 'Repository kan vaere offentlig. Ingen filer er lastet opp enna.'
        if ($PreviewOnly) { return @{ Uploaded=$false; Changed=$true; WorkDirectory=$work } }
        $ok = if ($null -ne $Approve) { & $Approve } else { (Read-Host 'Skriv JA for a laste opp disse endringene til main; Enter avbryter') -ceq 'JA' }
        if (-not $ok) {
            Write-Host 'Avbrutt. GitHub er uendret.'
            return @{ Uploaded=$false; Changed=$true; WorkDirectory=$work }
        }
        if (-not $AuthorName) { $AuthorName = Invoke-NovaGit -GitArguments @('-C',$work,'config','--get','user.name') -AllowMissing }
        if (-not $AuthorEmail) { $AuthorEmail = Invoke-NovaGit -GitArguments @('-C',$work,'config','--get','user.email') -AllowMissing }
        if (-not $AuthorName) { $AuthorName = Read-Host 'Navn som skal sta pa committen (f.eks. Darschnid479)' }
        if (-not $AuthorEmail) {
            Write-Host 'Bruk gjerne din GitHub noreply-adresse fra https://github.com/settings/emails'
            $AuthorEmail = Read-Host 'E-post for committen (blir synlig i Git-historikken)'
        }
        if ([string]::IsNullOrWhiteSpace($AuthorName) -or $AuthorEmail -notmatch '^[^\s@]+@[^\s@]+\.[^\s@]+$') { throw 'Navn og en gyldig e-postadresse er nodvendig. GitHub er ikke oppdatert.' }
        Invoke-NovaGit -GitArguments @('-C',$work,'config','--local','user.name',$AuthorName) | Out-Null
        Invoke-NovaGit -GitArguments @('-C',$work,'config','--local','user.email',$AuthorEmail) | Out-Null
        Invoke-NovaGit -GitArguments @('-C',$work,'commit','-m','Polish NOVA: presentation, documentation and source package') | Out-Null
        Write-Host '[5/5] Laster opp med vanlig fast-forward-kontroll...'
        Invoke-NovaGit -GitArguments @('-C',$work,'push','--set-upstream','origin','HEAD:refs/heads/main') | Out-Null
        Write-Host "FERDIG: $RepoUrl" -ForegroundColor Green
        return @{ Uploaded=$true; Changed=$true; WorkDirectory=$work }
    } finally {
        Write-Host "Arbeidskopien er beholdt i: $work"
        Write-Host 'Kildemappen og din lokale Git-historikk er ikke endret.'
    }
}
Export-ModuleMember -Function Invoke-NovaUpload
