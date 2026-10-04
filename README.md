<div align="center">

# NOVA Browser

### **Internet, on your terms.**

A modern Windows browser experiment built with **Visual Basic .NET**, **WPF**, and **Microsoft WebView2**.

[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D4?logo=windows11&logoColor=white)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Visual Basic](https://img.shields.io/badge/Visual%20Basic-.NET-512BD4)](https://learn.microsoft.com/dotnet/visual-basic/)
[![WebView2](https://img.shields.io/badge/Engine-WebView2-0B57D0?logo=microsoftedge&logoColor=white)](https://developer.microsoft.com/microsoft-edge/webview2/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build](https://github.com/Darschnid479/NovaBrowser/actions/workflows/windows-build.yml/badge.svg)](https://github.com/Darschnid479/NovaBrowser/actions/workflows/windows-build.yml)

**NOVA is focused on a polished, personal, highly customizable browsing experience.**

[Features](#-features) · [Quick start](#-quick-start) · [Roadmap](#-roadmap) · [Architecture](#-architecture) · [Contributing](#-contributing)

</div>

---

## ✨ What is NOVA?

NOVA is an open-source Windows browser project exploring what a browser can feel like when **personalization, clean UI, and simple controls** are treated as first-class features.

It is not a new rendering engine. NOVA hosts **Microsoft WebView2 / Chromium** inside a custom WPF shell, while the browser experience itself is written in **Visual Basic .NET**.

> **Project status:** early development. Expect rapid changes, rough edges, and breaking changes between versions.

## 🚀 Features

- **Multi-tab browsing** with normal and private tabs
- **First-run setup wizard** for search engine, theme, accent, animations, and privacy preferences
- **Four built-in themes** — Midnight, Dawn, Forest, and Graphite
- **Six accent colors** and multiple background styles
- **Custom vector icons** with no external icon-font dependency
- **Unified address + search bar**
- **Google, DuckDuckGo, or Bing** as the default search provider
- **Bookmarks and local browsing history**
- **Session restore** for normal tabs
- **Recently closed tabs** with `Ctrl + Shift + T`
- **Command palette** with `Ctrl + K`
- **Permission prompts** for camera, microphone, location, clipboard, and notifications
- **Private browsing profile** for private tabs
- **Keyboard-first navigation**
- **Local settings storage** — no NOVA cloud account required

## 🖼️ Screenshots

Screenshots are coming as the UI stabilizes.

> Want to help? Add screenshots to `docs/images/` and replace this section with real captures from the latest build.

## ⚡ Quick start

### Requirements

- Windows 10 or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)

### Run from source

```powershell
git clone https://github.com/Darschnid479/NovaBrowser.git
cd NovaBrowser
dotnet run --project src/NovaBrowser/NovaBrowser.vbproj -c Release
```

Or on Windows, double-click:

```text
START-NOVA.cmd
```

### Build a Windows package

```text
BYGG-EXE.cmd
```

The published build is placed in:

```text
out\win-x64\
```

Keep the entire output folder together; NOVA is not currently published as a single self-contained executable.

## 🎨 Personalization

NOVA is designed to feel personal without requiring an account.

| Setting | Options |
|---|---|
| Theme | Midnight · Dawn · Forest · Graphite |
| Accent | Iris · Cyan · Mint · Coral · Gold · Blue |
| Background | Aurora · Orbit · None |
| Search | Google · DuckDuckGo · Bing |
| Motion | On / Off |
| Session restore | On / Off |
| NOVA history | On / Off |

## ⌨️ Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl + T` | New tab |
| `Ctrl + W` | Close tab |
| `Ctrl + Shift + N` | New private tab |
| `Ctrl + Shift + T` | Reopen closed tab |
| `Ctrl + Tab` | Next tab |
| `Ctrl + Shift + Tab` | Previous tab |
| `Ctrl + L` | Focus address bar |
| `Ctrl + K` | Open command palette |
| `Ctrl + D` | Bookmark current page |
| `Ctrl + H` | Open history |
| `Ctrl + J` | Open downloads |
| `Ctrl + B` | Toggle sidebar |
| `Alt + Left / Right` | Back / forward |
| `Ctrl + R` / `F5` | Reload |
| `Ctrl + + / - / 0` | Zoom in / out / reset |

## 🧱 Architecture

```text
NOVA Browser
├─ WPF UI / custom chrome
├─ Tab + browser state management
├─ Search / URL policy
├─ Local state store
├─ Permissions + popup handling
└─ Microsoft WebView2
   └─ Chromium rendering engine
```

Key files:

- `src/NovaBrowser/MainWindow.xaml` — main window layout
- `src/NovaBrowser/MainWindow.xaml.vb` — tabs and browser engine integration
- `src/NovaBrowser/MainWindow.Interactions.vb` — user actions and settings
- `src/NovaBrowser/MainWindow.Setup.vb` — first-run wizard
- `src/NovaBrowser/MainWindow.Privacy.vb` — permissions and privacy actions
- `src/NovaBrowser/UI/Styles.xaml` — shared UI styles
- `src/NovaBrowser/UI/ThemeManager.vb` — themes and accents
- `src/NovaBrowser/Services/UrlPolicy.vb` — address/search behavior
- `src/NovaBrowser/Services/StateStore.vb` — local persistence

More detail: [`docs/ARKITEKTUR.md`](docs/ARKITEKTUR.md)

## 🗺️ Roadmap

NOVA is still young. Planned areas include:

- [ ] Proper window controls and maximized-by-default startup
- [ ] Drag-and-drop tab reordering
- [ ] Better tab search and tab groups
- [ ] Installer and signed releases
- [ ] Automatic update flow
- [ ] Password / credential integration strategy
- [ ] Extension strategy
- [ ] Sleeping/background tab management
- [ ] Multiple windows
- [ ] Better downloads UI
- [ ] More accessibility testing
- [ ] Performance profiling and startup optimization
- [ ] Real screenshot gallery and release demo video

Have an idea? Open a [feature request](../../issues/new?template=feature_request.yml).

## 🔐 Privacy & data

NOVA does not add its own analytics or cloud account system.

Local data is stored under:

```text
%LOCALAPPDATA%\NOVA-Browser
```

This can include settings, bookmarks, NOVA history, session state, WebView2 browser data, and error logs. Private browsing is **not anonymity**: websites, network administrators, and internet providers may still observe traffic.

See the existing privacy and verification notes in the project documentation before relying on NOVA for sensitive browsing.

## ⚠️ Current limitations

NOVA is a browser shell around WebView2, not a new web engine. Performance and security have not been benchmarked against Chrome, Firefox, Edge, or other production browsers.

Known project limitations currently include no extension store, no cloud sync, no built-in password manager, no auto-updater, no installer, and incomplete multi-window / tab-dragging support. DRM-protected playback can also be limited by the chosen WebView2 composition control.

## 🛠️ Development

Run project checks:

```powershell
dotnet run --project tests/NovaBrowser.Checks/NovaBrowser.Checks.vbproj -c Release
```

Run the Windows UI checks:

```text
TEST-UI.cmd
```

The repository includes a GitHub Actions workflow for Windows builds in:

```text
.github/workflows/windows-build.yml
```

## 🤝 Contributing

Contributions, bug reports, UI ideas, and testing feedback are welcome.

Before opening a pull request, please read [`CONTRIBUTING.md`](CONTRIBUTING.md).

- Bug? Use the bug report template.
- Idea? Use the feature request template.
- Security concern? Please read [`SECURITY.md`](SECURITY.md) first.

## 📄 License

NOVA Browser is released under the [MIT License](LICENSE).

Microsoft WebView2 and other dependencies remain subject to their own licenses and terms.

---

<div align="center">

**NOVA Browser** · Built in Visual Basic .NET · Designed for Windows

⭐ If you like the direction of the project, star the repository.

</div>
