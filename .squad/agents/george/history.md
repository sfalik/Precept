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



## Recent Updates

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
