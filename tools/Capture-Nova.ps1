# Capture the visible NOVA window only, then require review before adding to docs.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
try {
    Add-Type -AssemblyName System.Drawing
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class NovaCaptureNative {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h,int a,out RECT r,int size);
}
'@
    [NovaCaptureNative]::SetProcessDPIAware() | Out-Null
    $apps=@(Get-Process -Name 'NOVA' -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero })
    if ($apps.Count -eq 0) { throw 'Start NOVA med START-NOVA.cmd forst, og prov igjen.' }
    if ($apps.Count -gt 1) {
        $apps | Format-Table Id,MainWindowTitle
        $chosen=Read-Host 'Skriv prosess-ID for NOVA-vinduet du vil ta bilde av'
        $apps=@($apps | Where-Object { $_.Id.ToString() -eq $chosen })
        if ($apps.Count -ne 1) { throw 'Ingen entydig prosess valgt.' }
    }
    $hwnd=$apps[0].MainWindowHandle
    Write-Host 'Lukk varsler og skjul personlig informasjon. Vis startsiden, temaet eller veiviseren du vil dokumentere.'
    if ((Read-Host 'Skriv JA for a ta ett bilde av NOVA-vinduet om 5 sekunder') -cne 'JA') { exit 0 }
    if ([NovaCaptureNative]::IsIconic($hwnd)) { [NovaCaptureNative]::ShowWindow($hwnd,9) | Out-Null }
    [NovaCaptureNative]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Seconds 5
    if ([NovaCaptureNative]::GetForegroundWindow() -ne $hwnd) { throw 'NOVA er ikke forgrunnsvindu. Ingen bilder ble tatt. Prov igjen og klikk NOVA under nedtellingen.' }
    $rect=New-Object NovaCaptureNative+RECT
    $result=[NovaCaptureNative]::DwmGetWindowAttribute($hwnd,9,[ref]$rect,16)
    if ($result -ne 0 -and -not [NovaCaptureNative]::GetWindowRect($hwnd,[ref]$rect)) { throw 'Fant ikke vindusgrensene.' }
    $width=$rect.Right-$rect.Left; $height=$rect.Bottom-$rect.Top
    if ($width -le 0 -or $height -le 0 -or $width -gt 12000 -or $height -gt 12000) { throw 'Ugyldig vindusstorrelse.' }
    $root=Split-Path -Parent $PSScriptRoot
    $pending=Join-Path $root 'artifacts/screenshots-pending'
    [IO.Directory]::CreateDirectory($pending) | Out-Null
    $name='nova-real-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.png'
    $path=Join-Path $pending $name
    $bitmap=[Drawing.Bitmap]::new($width,$height)
    $graphics=$null
    try {
        $graphics=[Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($rect.Left,$rect.Top,0,0,$bitmap.Size,[Drawing.CopyPixelOperation]::SourceCopy)
        $bitmap.Save($path,[Drawing.Imaging.ImageFormat]::Png)
    } finally {
        if ($null -ne $graphics) { $graphics.Dispose() }
        $bitmap.Dispose()
    }
    Invoke-Item -LiteralPath $path
    Write-Host "Bildet ligger midlertidig i $path. Denne mappen blir IKKE lastet opp."
    Write-Host 'Kontroller at bildet viser NOVA og ikke inneholder privat informasjon eller andre vinduer.'
    if ((Read-Host 'Skriv JA for a kopiere bildet til den publiserbare skjermbildemappen') -ceq 'JA') {
        $destination=Join-Path $root 'docs/assets/screenshots'
        [IO.Directory]::CreateDirectory($destination) | Out-Null
        Copy-Item -LiteralPath $path -Destination (Join-Path $destination $name)
        Write-Host "Godkjent bilde: docs/assets/screenshots/$name"
        Write-Host 'Legg det inn i README.md med korrekt bildetekst. App-forhandsvisningene blir ikke automatisk erstattet.'
    } else { Write-Host 'Bildet forblir lokalt i artifacts. Ingenting er publisert.' }
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
