# George — Proof Message Rewrites

Date: 2026-05-12
Branch: spike/Precept-V2-Radical

## Summary
- Implemented Elaine Section A proof diagnostic copy rewrites in `src/Precept/Language/Diagnostics.cs`.
- Upgraded PRE0114 emission in `src/Precept/Pipeline/ProofEngine.cs` to pass six structured args: operand labels, axis, context clause, and left/right qualifier values.
- Added regression coverage for the new PRE0114 bracketed-message format and updated exact-message assertions for PRE0112, PRE0113, and PRE0116.

## Files
- `src/Precept/Language/Diagnostics.cs`
- `src/Precept/Pipeline/ProofEngine.cs`
- `test/Precept.Tests/DiagnosticsTests.cs`
- `test/Precept.Tests/ProofEngineTests.cs`
- `docs/compiler/proof-engine.md`
- `docs/compiler/diagnostic-system.md`
- `docs/runtime/fault-system.md`
- `.squad/agents/george/history.md`

## Validation
- `dotnet build src\Precept\Precept.csproj`
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build`
- Result: `4914/4914` tests passed.

## Notes
- Chose ProofEngine-side qualifier resolution instead of widening `QualifierCompatibilityProofRequirement`; the requirement catalog stays unchanged and emission can derive both symbolic qualifier values directly from the resolved operands.
- `QualifierChainProofRequirement` now also supplies the shared PRE0114 template's left/right qualifier-value args so the diagnostic contract stays internally consistent.
