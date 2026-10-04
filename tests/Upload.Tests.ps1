# Integration tests: local disposable bare Git remotes only. NEVER contacts GitHub.
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot '../tools/NovaUpload.psm1') -Force
$root=Join-Path ([IO.Path]::GetTempPath()) ('NOVA-upload-test-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$repo=Join-Path $root 'remote.git'
& git init --bare --initial-branch=main $repo
if ($LASTEXITCODE -ne 0) { throw 'git init failed' }
$source=Join-Path $root 'source with spaces'
New-Item -ItemType Directory -Path (Join-Path $source 'src/NovaBrowser') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $source 'NOVA.sln'),'test solution')
[IO.File]::WriteAllText((Join-Path $source 'src/NovaBrowser/NovaBrowser.vbproj'),'<Project />')
[IO.File]::WriteAllText((Join-Path $source '.gitignore'),"artifacts/`n*.log`n")
[IO.File]::WriteAllText((Join-Path $source 'README.md'),'first')
New-Item -ItemType Directory -Path (Join-Path $source 'artifacts'),(Join-Path $source 'bin') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $source 'artifacts/private.txt'),'private screenshot notes')
[IO.File]::WriteAllText((Join-Path $source 'bin/build.txt'),'build output')
$argsCommon=@{SourceRoot=$source; RepoUrl=$repo; AuthorName='NOVA upload test'; AuthorEmail='test@example.invalid'}
$r=Invoke-NovaUpload @argsCommon -Approve { $true }
if (-not $r.Uploaded) { throw 'Empty remote upload failed' }
$files=& git --git-dir=$repo ls-tree -r --name-only main
if ($files -notcontains 'README.md' -or $files -match 'artifacts|bin/') { throw 'File filtering failed' }
$r=Invoke-NovaUpload @argsCommon -Approve { throw 'No-change upload must not ask for approval' }
if ($r.Changed) { throw 'No-change detection failed' }
$before=& git --git-dir=$repo rev-parse main
[IO.File]::WriteAllText((Join-Path $source 'README.md'),'second')
$r=Invoke-NovaUpload @argsCommon -Approve { $false }
if ($r.Uploaded -or (& git --git-dir=$repo rev-parse main) -ne $before) { throw 'Cancel altered remote' }
$r=Invoke-NovaUpload @argsCommon -PreviewOnly
if ($r.Uploaded -or (& git --git-dir=$repo rev-parse main) -ne $before) { throw 'Preview altered remote' }
$r=Invoke-NovaUpload @argsCommon -Approve { $true }
if ((& git --git-dir=$repo show main:README.md) -ne 'second') { throw 'Existing remote update failed' }
# A remotely added file not present in the package must be preserved.
$clone=Join-Path $root 'other'
& git clone --quiet $repo $clone
& git -C $clone config user.name 'Test'
& git -C $clone config user.email 'test@example.invalid'
[IO.File]::WriteAllText((Join-Path $clone 'REMOTE-ONLY.md'),'keep me')
& git -C $clone add .
& git -C $clone commit -qm 'Remote-only file'
& git -C $clone push --quiet origin main
[IO.File]::WriteAllText((Join-Path $source 'README.md'),'third')
$r=Invoke-NovaUpload @argsCommon -Approve { $true }
if ((& git --git-dir=$repo show main:REMOTE-ONLY.md) -ne 'keep me') { throw 'Remote-only file lost' }
[IO.File]::WriteAllText((Join-Path $source '.env'),'never upload')
$blocked=$false
try { Invoke-NovaUpload @argsCommon -Approve { $true } | Out-Null } catch { $blocked=$_.Exception.Message -match 'privat fil' }
if (-not $blocked) { throw 'Secret filename was not blocked' }
Remove-Item -LiteralPath (Join-Path $source '.env')
# Build the fixture dynamically so the repository does not contain a token-like literal.
[IO.File]::WriteAllText((Join-Path $source 'token.txt'),('gh'+'p_'+('a'*36)))
$blocked=$false
try { Invoke-NovaUpload @argsCommon -Approve { $true } | Out-Null } catch { $blocked=$_.Exception.Message -match 'hemmelig nokkel' }
if (-not $blocked) { throw 'Token content was not blocked' }
Write-Host 'PASS: empty/existing remote, unchanged package, cancel, preview, remote-only preservation, file filtering, secret filename and token detection.'
Write-Host "Disposable fixtures kept at $root"
