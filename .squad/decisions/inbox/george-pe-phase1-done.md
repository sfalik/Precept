### 2026-05-08T23:21:03.236-04:00: Phase 1 proof-engine prework closed
**By:** George (requested by Shane)
**What:** Completed the Phase 1 proof-engine prework slices P1-P8 with structural-only changes: proof satisfaction carriers, declared presence/qualifier metadata, modifier proof-satisfaction catalog data, semantic-index carrier slots on `TypedField`/`TypedArg`, `ObligationContext` on `ProofObligation`, proof diagnostic codes 112-115, and the matching doc ordinal corrections.

**Commits:**
- P1 `f1de70dc` — `feat(proof-engine): P1 — ProofSatisfaction DU and supporting types`
- P2 `161eb1fa` — `feat(proof-engine): P2 — DeclaredPresenceMeta carrier type`
- P3 `267dd7bd` — `feat(proof-engine): P3 — DeclaredQualifierMeta carrier type`
- P4 `5d6945c4` — `feat(proof-engine): P4 — FieldModifierMeta.ProofSatisfactions catalog metadata`
- P5 `1bdf53f4` — `feat(proof-engine): P5 — TypedField/TypedArg presence and qualifier carrier properties`
- P6 `445c3127` — `feat(proof-engine): P6 — ObligationContext DU on ProofObligation`
- P7 `247ba37f` — `feat(proof-engine): P7 — diagnostic codes 112-115 for proof stage`
- P8 `647de929` — `docs(proof-engine): P8 — correct diagnostic code ordinals 96-99 → 112-115`

**Files touched (high-signal):**
- Runtime/language: `src/Precept/Language/ProofRequirement.cs`, `src/Precept/Language/DeclaredPresence.cs`, `src/Precept/Language/DeclaredQualifierMeta.cs`, `src/Precept/Language/Modifier.cs`, `src/Precept/Language/Modifiers.cs`, `src/Precept/Pipeline/SemanticIndex.cs`, `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/ProofLedger.cs`, `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Diagnostics.cs`
- Tests: `test/Precept.Tests/ProofRequirementTests.cs`, `test/Precept.Tests/ModifiersTests.cs`, `test/Precept.Tests/TypeChecker/TypeCheckerExpressionTests.cs`, `test/Precept.Tests/TypeChecker/TypeCheckerQuantifierTests.cs`, `test/Precept.Tests/TypeChecker/TypeCheckerSymbolTests.cs`, `test/Precept.Tests/ProofLedgerTests.cs`, `test/Precept.Tests/DiagnosticsTests.cs`
- Docs: `docs/compiler/proof-engine.md`, `docs/compiler/diagnostic-system.md`, `docs/Working/frank-proof-engine-gap-analysis.md`

**Validation:**
- `dotnet build src\Precept\Precept.csproj --nologo` succeeded during slice validation.
- Final `dotnet test -nologo` summary: 3910 total, 3714 passed, 196 failed.
- Final `dotnet build -nologo` succeeded.
- Remaining failures are pre-existing: 194 `Precept.LanguageServer.Tests` failures from `LanguageServerStubs.cs` `NotImplementedException` paths, plus 2 `Precept.Tests` `TokensTests` failures around `TokenKind.Set` classification.

**Surprises / deviations:**
- `ConstraintIdentity` already existed in `SemanticIndex.cs`, so no new identity carrier was needed for P6.
- The spec's proof diagnostic ordinals were stale; the implementation correctly used 112-115 instead of 96-99.
- `docs/compiler/proof-engine.md` already carried a large unrelated branch diff, so the P8 doc commit necessarily rode on top of a broader proof-engine doc sync instead of a tiny isolated ordinal-only patch.
- `FieldModifierMeta.ProofSatisfactions` test assertions had to avoid FluentAssertions expression-tree paths; simple `foreach` assertions were more robust.

**Tricky construction sites:**
- `TypedField` record construction in `src/Precept/Pipeline/TypeChecker.cs`
- Manual `TypedArg` scaffolds in `test/Precept.Tests/TypeChecker/TypeCheckerExpressionTests.cs`
- Manual `TypedArg` scaffolds in `test/Precept.Tests/TypeChecker/TypeCheckerQuantifierTests.cs`
- Proof-ledger shape tests in `test/Precept.Tests/ProofLedgerTests.cs`

**Phase 2 handoff:**
- The structural metadata surface is now in place for obligation instantiation, strategy evaluation, and proof diagnostic emission.
- Phase 2 can assume proof-bearing modifiers already declare satisfactions, semantic symbols expose presence/qualifier carriers, obligations can record context, and proof-stage diagnostics 112-115 are reserved with metadata.
- `ProofEngine.cs` remains the behavioral frontier; Phase 2 should implement runtime-neutral proof analysis against the new carriers rather than reshaping these types again.
