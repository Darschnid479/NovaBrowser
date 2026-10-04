# Entry point used by LAST-OPP-NOVA-TIL-GITHUB.bat. Compatible with Windows PowerShell 5.1.
[CmdletBinding()]
param([switch]$PreviewOnly)
$ErrorActionPreference = 'Stop'
try {
    Import-Module (Join-Path $PSScriptRoot 'NovaUpload.psm1') -Force
    $root = Split-Path -Parent $PSScriptRoot
    Invoke-NovaUpload -SourceRoot $root -PreviewOnly:$PreviewOnly | Out-Null
    exit 0
} catch {
    Write-Host "`nOPPLASTING STOPPET" -ForegroundColor Red
    Write-Host $_.Exception.Message
    Write-Host 'Ingen force-push eller automatisk konfliktlosning er forsokt.'
    Write-Host 'Ved innloggingsfeil: fullfor Git Credential Manager, eller bruk gh auth login + gh auth setup-git.'
    Write-Host 'Ved avvist push: repoet kan ha nye commits eller beskyttet main. Sammenlign filene og prov igjen; ikke bruk --force.'
    exit 1
}
