# Offisielle kilder brukt ved implementasjon

Kontrollert 2. oktober 2026. Dokumentasjon er ikke det samme som en kjørt test av dette prosjektet.

1. Microsoft: WebView2 med WPF; både Visual Basic og C# støttes.
   https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf
2. Microsoft: WebView2CompositionControl og WPF-lag over nettinnhold.
   https://learn.microsoft.com/en-us/microsoft-edge/webview2/platforms/wpf
3. Microsoft: klassen WebView2CompositionControl, inkludert levetid, tastaturhendelser,
   bildefrekvens og DRM-begrensning.
   https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.wpf.webview2compositioncontrol
4. Microsoft: utvikling av sikre WebView2-verter.
   https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security
5. Microsoft: EnsureCoreWebView2Async med controller options.
   https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.wpf.webview2compositioncontrol.ensurecorewebview2async
6. Microsoft: NewWindowRequested og utsatt behandling.
   https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.newwindowrequested
7. Microsoft: profil, InPrivate, foretrukket fargevalg og standard lagring.
   https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2profile
8. Microsoft: ClearBrowsingDataAsync.
   https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2profile.clearbrowsingdataasync
9. Microsoft: PermissionRequested-argumenter og SavesInProfile.
   https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2permissionrequestedeventargs
10. Microsoft: OpenDefaultDownloadDialog.
    https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.opendefaultdownloaddialog
11. NuGet: Microsoft.Web.WebView2 1.0.4258.31, festet versjon i prosjektfilen.
    https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4258.31
12. Microsoft: .NET 10 SDK og runtime.
    https://dotnet.microsoft.com/en-us/download/dotnet/10.0

## Tillegg for rettelse 0.1.2

Se HOTFIX-0.1.2.md for kildene til WinRT-avhengigheten,
Windows-spesifikt TargetFramework og den forbedrede feilloggingen.


## Tillegg for 0.2.0 (lest 2. oktober 2026)

Microsoft dokumenterer at VerticalContentAlignment bare virker når malen
bruker verdien; WPF-kilden viser at TextBoxBase selv overfører Padding til
ScrollViewer. Stiler har høyere prioritet enn arvede egenskaper.

- https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.control.verticalcontentalignment?view=windowsdesktop-10.0
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/textbox-styles-and-templates
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/properties/dependency-property-value-precedence
- https://raw.githubusercontent.com/dotnet/wpf/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/TextBoxBase.cs
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/shapes-and-basic-drawing-in-wpf-overview

Dokumentasjonen begrunner endringene, men beviser ikke at den nye applikasjonen
kompilerer eller tegnes korrekt på brukerens Windows-PC.
