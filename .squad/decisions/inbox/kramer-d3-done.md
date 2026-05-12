# Kramer — D3 Activation Event Test Modernization

**Date:** 2026-05-11
**Author:** Kramer (Tooling Dev)
**Status:** Implemented

---

## Summary

Updated `test\Precept.LanguageServer.Tests\ExtensionManifestTests.cs` so `PackageManifest_Activates_WhenAPreceptDocumentOpens` now verifies the Precept language contribution under `contributes.languages` instead of expecting the redundant `onLanguage:precept` activation event.

---

## Decisions Made

### D3: Treat language contribution as the activation contract

VS Code 1.74+ auto-activates extensions for languages declared in `contributes.languages`, so the removed `onLanguage:precept` entry should stay removed. The test now asserts the durable contract (`id: "precept"`) and keeps the valid `workspaceContains:**/*.precept` activation trigger covered.

---

## Validation

- Confirmed `tools\Precept.VsCode\package.json` contributes language id `precept` and only keeps `workspaceContains:**/*.precept` in `activationEvents`.
- `dotnet test test\Precept.LanguageServer.Tests\ --no-restore --nologo --filter "PackageManifest_Activates"` ✅
