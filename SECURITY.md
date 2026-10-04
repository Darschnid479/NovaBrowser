# Security Policy

NOVA Browser is an early-stage project and is not yet positioned as a hardened production browser.

## Reporting a vulnerability

Please avoid publishing exploit details in a public issue before a fix is available.

Until a dedicated private security channel is configured, open a minimal GitHub issue stating that you have a security concern and avoid including secrets, exploit payloads, personal data, or sensitive reproduction material in public.

## Scope

Security-sensitive areas include:

- URL and navigation handling
- permission prompts
- popup behavior
- WebView2 configuration
- local state and profile handling
- downloads
- external protocol handling
- private browsing behavior

## Important note

NOVA uses Microsoft WebView2 as its web engine. Engine-level Chromium/WebView2 vulnerabilities should be reported through Microsoft's appropriate security channels as well.
