# George C2 done

## What I changed
- Broke the parser-side circular dependency by changing `Parser.KeywordsValidAsMemberName` to reuse `Tokens.KeywordsValidAsMemberName` directly.
- Kept the catalog-derived source of truth intact: `Tokens.KeywordsValidAsMemberName` still derives from `Types.All` accessor names mapped back through `Tokens.Keywords`.
- Added parser/runtime regression coverage for exchangerate keyword accessors so `from` and `to` stay valid after `.` and `FxRate.from` / `FxRate.to` compile cleanly.

## Validation
- `dotnet build src\Precept\Precept.csproj --nologo` ✅
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo` is still blocked by the pre-existing `TypedTransitionRow` constructor compile failures in `test\Precept.Tests\ProofLedgerTests.cs` and `test\Precept.Tests\ProofEngineTests.cs`.
- Manual compiler validation via the built `Precept.dll` succeeds for a focused `exchangerate` accessor snippet using `FxRate.from` and `FxRate.to`.

## inventory-item.precept
- The sample no longer needs a parser-side keyword-member-name special case for `.from` / `.to`; the catalog path resolves them correctly.
- Current manual compile output for the workspace sample shows **0** `ExpectedToken` / PRE0009 diagnostics. Remaining fallout is now semantic-only (`UnprovedQualifierCompatibility x66`, `TypeMismatch x8`).
