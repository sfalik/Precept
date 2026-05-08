# Grammar generator message-position wiring

**Author:** Kramer  
**Date:** 2026-05-08T18:36:50.783-04:00  
**Status:** Implemented on `Precept-V2-Radical`

## What changed
- Replaced the hardcoded `messageStrings` repository block in `tools/Precept.GrammarGen/Program.cs` with catalog-derived generation from `Tokens.All.Where(m => m.IsMessagePosition)`.
- Wired the same repository block to also consume `Functions.All.Where(f => f.IsMessagePosition)` so future built-ins can gold-scope trailing message strings without new hardcoding.
- Removed the stale TODO at the function wire-in point; unflagged functions still emit normal function-call patterns and intentionally contribute no message-string patterns.

## Validation
- Ran `dotnet build tools/Precept.GrammarGen/Precept.GrammarGen.csproj`.
- Ran `dotnet run --project tools/Precept.GrammarGen/Precept.GrammarGen.csproj -- --output tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`.
- Verified the generated grammar still contains a `messageStrings` repository entry with catalog-derived `because` and `reject` patterns and still includes `#messageStrings` before `#strings`.
- The generated `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` did not change, confirming the catalog-derived output matches the prior behavior.

## Note for Scribe
- This closes the grammar generator catalog gap for message-position strings.
