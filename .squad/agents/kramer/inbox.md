## Slice 0b — 2026-05-10
Deleted LanguageServerStubs.cs, PreceptPreviewProtocol.cs, LegacyHandlerCompat.cs, and 13 legacy shim-backed language-server test files. Verified `dotnet build` succeeds for the LS and LS test projects, `dotnet test` passes for Precept.Tests (3737/3737), and the LS test project now discovers 0 tests and still builds successfully.
