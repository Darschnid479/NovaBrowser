# Contributing to NOVA Browser

Thanks for helping improve NOVA.

## Before you start

- Search existing issues before opening a new one.
- Keep changes focused and easy to review.
- Do not weaken TLS, certificate, permission, or browser security behavior just to make a site work.
- Do not commit secrets, local browser profiles, build output, or personal logs.

## Development setup

Requirements:

- Windows 10/11
- .NET 10 SDK
- WebView2 Runtime
- Visual Studio with .NET desktop development, or the `dotnet` CLI

Run the project:

```powershell
dotnet run --project src/NovaBrowser/NovaBrowser.vbproj -c Release
```

Run checks:

```powershell
dotnet run --project tests/NovaBrowser.Checks/NovaBrowser.Checks.vbproj -c Release
```

## Pull requests

A good PR should:

1. Explain the problem and the chosen solution.
2. Keep unrelated refactors out of the same PR.
3. Include testing notes.
4. Include screenshots for visible UI changes when possible.
5. Mention any known limitations or follow-up work.

## Code style

- Prefer readable names over abbreviations.
- Keep UI logic, state storage, URL policy, and privacy logic separated where practical.
- Treat error handling and privacy behavior as part of the feature, not as an afterthought.
- Avoid new dependencies unless they provide clear value.

## Reporting bugs

Please include:

- NOVA version or commit
- Windows version
- What you expected
- What actually happened
- Steps to reproduce
- Relevant `error.log` excerpt, after removing personal information

Never publish your WebView2 profile or `state.json` if it contains private browsing data.
