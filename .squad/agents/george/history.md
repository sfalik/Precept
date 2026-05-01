## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: closed catalog extensibility hardening, PRECEPT0018 analyzer enforcement, parser whitespace-insensitivity, typed constants, event-handler ensure guards, presence-operator Pratt support, the expression-form catalog/annotation bridge, list literals, method calls, the sample/coverage regression layer, and Phase 2a+2b of the parser-gap fix plan (GAP-A/B/C + OperatorMeta DU restructure).
- Current ownership: Phase 2c (PRECEPT0019 promotion: annotate TypeChecker + GraphAnalyzer, flip Warning→Error) and Slice 27 (Parser.cs split into partial files).

## Learnings

- Spec grammar, parser enforcement, docs, tests, and samples must all agree before a slice is considered complete.
- Durable language truth belongs in catalogs/metadata; avoid hardcoded parallel tables in parser or tooling code.
- Shared record or catalog signature changes require a full construction-site and call-site audit, including dead-code exhaustive switch arms.
- Pratt handlers for tokens that are not in `OperatorPrecedence` must be inserted before the `TryGetValue` guard or they are silently swallowed.
- Use compiler/analyzer exhaustiveness intentionally: CS8509 for switch coverage, PRECEPT0007 for catalog metadata, PRECEPT0019 for handler coverage.
- `HandlesCatalogExhaustivelyAttribute` must allow `Struct` targets for `ref struct` handlers, and `ref struct` types still cannot own static fields.
- Verify real `TokenKind` names before writing metadata or tests; `dotnet build -q` can hide actionable diagnostics during failure analysis.
- Multi-token presence operators are a proposal-scale catalog completeness gap, while keyword member names like `.min` / `.max` need dedicated parser handling in the `Dot` path.
- **`TypeChecker` and `GraphAnalyzer` are both `public static class`** — all stub methods must be `private static`. Reflection test must include `BindingFlags.Static` to find them, and the existing `ContainSingle` assertion must expand to `HaveCount(3)` once all three pipeline stages are annotated.
- **ExpressionFormCoverageTests split**: the existing `test/Precept.Tests/ExpressionFormCoverageTests.cs` is the Layer 3 reflection+round-trip file (Slice 13); the new `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` is the Layer 2 per-kind catalog assertions (Slice 25) — different namespaces, no class name conflict.
- **PRECEPT0019 promotion sequence**: annotate all consuming pipeline classes FIRST (verify zero PRECEPT0019 fires), THEN flip severity to Error and remove WarningsNotAsErrors. The pre-condition check is non-negotiable.
- **PRECEPT0023c analyzer invariant**: `ByTokenSequence` is keyed by the full `(TokenKind, TokenKind?, TokenKind?)` tuple — NOT by the lead token alone. `IsSet=[Is,Set]` and `IsNotSet=[Is,Not,Set]` share the lead token `Is` but are structurally valid because their full sequences differ. An analyzer checking "no two MultiTokenOps share the same lead token" will fire false positives on this real catalog pattern. Always key the uniqueness check on the full sequence, not the first element.



## Recent Updates

### 2026-05-01 — Scribe closeout: gate fully closed
- `.squad/decisions/inbox/george-phase2-gate-closed.md` was merged into `decisions.md`, orchestration/session closeout logs were written, and George's Phase 2 acceptance gate is now durably closed across the squad record.

### 2026-05-01 — Phase 2 gate closed (two follow-up fixes)
- **PRECEPT0023c rewritten**: The Phase 2e implementation checked "no two MultiTokenOp entries may share the same lead token" — wrong because `ByTokenSequence` is keyed by the full tuple. The correct invariant is "no two MultiTokenOp entries may have the same full token sequence." Severity promoted from Warning to Error now that the invariant is correct. Old test renamed (`GivenTwoMultiTokenOpsWithSameLeadToken_…` → `GivenTwoMultiTokenOpsWithSameFullSequence_…`); new `GivenTwoMultiTokenOpsWithSameLeadButDifferentFullSequence_NoDiagnostic` test added to lock in the IsSet/IsNotSet false-positive fix.
- **Spec §2.1 precedence**: Confirmed already resolved in Slice 17. `docs/language/precept-language-spec.md` §2.1 already shows `60` for `is set`/`is not set`. No further spec change required.
- **Full test suite**: 2678 passing (254 Analyzer + 2424 Core), 0 failing. Build: 0 errors, 0 warnings.
- **Plan doc updated**: All 14 acceptance-gate items marked ✅. Plan heading updated to "14 Points ✅ ALL RESOLVED".
- **Decision artifact**: `.squad/decisions/inbox/george-phase2-gate-closed.md` written.

### 2026-05-01 — Phase 2c/2d/2e closeout recorded
- Phase 2c: `TypeChecker` and `GraphAnalyzer` now carry full `ExpressionFormKind` coverage annotations, Layer 2 `ExpressionFormCoverageTests` landed, and PRECEPT0019 was promoted from warning to error with 2300 tests passing.
- Phase 2d: `Parser.cs` was split into `Parser.cs`, `Parser.Declarations.cs`, and `Parser.Expressions.cs` while preserving the single `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` declaration and keeping the parser green at 2300 tests.
- Phase 2e: PRECEPT0020-PRECEPT0023 analyzers, `TokenMeta.IsValidAsMemberName`, catalog-derived `KeywordsValidAsMemberName`, and a real `SetType` duplicate-text fix all landed; final verification reached 2677 passing tests.
- Scribe merged the Phase 2c/2d decision inbox artifacts and recorded george-10/george-11/george-12 closeout logs.

### 2026-05-01 — Phase 2d (Slice 27) complete
- `Parser.cs` split into three `partial` files: `Parser.cs` (~504 lines, core shell + dispatch), `Parser.Declarations.cs` (~1012 lines, all declaration/scope-level parsers), `Parser.Expressions.cs` (~330 lines, Pratt loop + atom parsers + `ExpectIdentifierOrKeywordAsMemberName`).
- Both `public static partial class Parser` and `internal ref partial struct ParseSession` declared in every file.
- `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` present exactly once (primary declaration in `Parser.cs`).
- `KeywordsValidAsMemberName` confirmed as static field on outer `Parser` class (not on `ref struct`); `ExpectIdentifierOrKeywordAsMemberName()` moved to `Parser.Expressions.cs` alongside its only caller (`ParseExpression`).
- Zero behavior change. Build: 0 errors, 0 warnings. Test count: 2300 passing, 0 failing.

### 2026-05-01 — Phase 2c (Slices 23–26) complete
- `TypeChecker` annotated: `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` + all 11 `[HandlesForm]` on `private static CheckExpression` stub.
- `GraphAnalyzer` annotated: same pattern with `private static AnalyzeExpression` stub.
- Reflection tests in `ExpressionFormCoverageTests` updated: `ContainSingle` → `HaveCount(3)`, `First()` → iterate all, `BindingFlags.Instance` → includes `BindingFlags.Static`.
- New `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` created: 26 Layer 2 catalog-shape tests.
- PRECEPT0019 promoted to `DiagnosticSeverity.Error`; `<WarningsNotAsErrors>` removed from `Precept.csproj`.
- Full solution: 0 errors, 0 warnings. Test count: 2300 passing, 0 failing (+26 new tests vs 2274 baseline).

### 2026-05-01 — Phase 2b closeout recorded
- Scribe recorded George-9's Phase 2b completion: the `OperatorMeta` DU restructure plus `ExpressionFormKind.PostfixOperation` landed with 2274 passing tests and 13 new tests added.
- The Phase 2b decision note from `.squad/decisions/inbox/george-phase2b-du.md` was merged into the canonical ledger during closeout.


### 2026-05-01 — Phase 2b (Slices 19–22) complete
- `Arity.Postfix`, `OperatorFamily.Presence`, `OperatorKind.IsSet/IsNotSet` added to enums.
- `OperatorMeta` restructured as abstract record base + `SingleTokenOp` (18 ops) + `MultiTokenOp` (2 ops: `IsSet`, `IsNotSet`).
- `Operators.ByToken` narrowed to `SingleTokenOp` only; `Operators.ByTokenSequence(params TokenKind[])` added for multi-token lookup.
- `ExpressionFormKind.PostfixOperation = 11` added; `[HandlesForm(PostfixOperation)]` added to `ParseExpression`.
- Consumer call site audit (Slice 22): all `.Token.` accesses in `OperatorsTests.cs` now operate on `SingleTokenOp`-typed variables; no stragglers in source.
- Full solution: 0 errors, 0 warnings. Test count: 2274 passing, 0 failing (+13 new tests vs 2261 baseline).
- Decisions captured at `.squad/decisions/inbox/george-phase2b-du.md`.

### 2026-05-01 — Slice 27 parser split decision received
- Frank locked Slice 27 to `partial class Parser` + `partial ref struct ParseSession` with three files: `Parser.cs`, `Parser.Declarations.cs`, and `Parser.Expressions.cs`.
- Keep `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` only on the primary `ParseSession` declaration; let `[HandlesForm(...)]` stay attached to the moved methods in `Parser.Expressions.cs`.
- Shared static vocabulary, including the future `KeywordsValidAsMemberName` set, must stay on the outer `Parser` class because `ref struct` types cannot declare static fields.

### 2026-05-01 — Parser-gap branch state summarized
- Branch work through Slice 13 is durably recorded: typed constants, event-handler ensure guards, presence-operator Pratt support, expression-form catalog/coverage, list literals, method calls, spec fixups, and the regression suites from Slices 8–13 are all in place.
- Remaining known-broken sample sentinels still point at three separate gaps: state/event ensure `when` guards, post-expression field modifiers, and keyword member names (`.min` / `.max`).
