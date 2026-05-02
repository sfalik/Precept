# George History Archive

Archived updates moved from `history.md` during Scribe summarization.

---

## Archive Batch — 2026-05-02T19:42:01Z

---

### 2026-05-01 — Annotation rename propagated to implementation context
- Scribe recorded the attribute rename closeout: future parser/type-checker/analyzer work should use `[HandlesCatalogMember]` rather than the retired `[HandlesForm]` name.
- Frank-9's sweep found no additional catalog-enum dispatchers that currently need exhaustiveness annotations, so implementation follow-ons stay limited to new distributed handlers introduced by future commits.

---

### 2026-05-01 — Scribe closeout: gate fully closed
- `.squad/decisions/inbox/george-phase2-gate-closed.md` was merged into `decisions.md`, orchestration/session closeout logs were written, and George's Phase 2 acceptance gate is now durably closed across the squad record.

---

### 2026-05-01 — Phase 2 gate closed (two follow-up fixes)
- **PRECEPT0023c rewritten**: The Phase 2e implementation checked "no two MultiTokenOp entries may share the same lead token" — wrong because `ByTokenSequence` is keyed by the full tuple. The correct invariant is "no two MultiTokenOp entries may have the same full token sequence." Severity promoted from Warning to Error now that the invariant is correct. Old test renamed (`GivenTwoMultiTokenOpsWithSameLeadToken_…` → `GivenTwoMultiTokenOpsWithSameFullSequence_…`); new `GivenTwoMultiTokenOpsWithSameLeadButDifferentFullSequence_NoDiagnostic` test added to lock in the IsSet/IsNotSet false-positive fix.
- **Spec §2.1 precedence**: Confirmed already resolved in Slice 17. `docs/language/precept-language-spec.md` §2.1 already shows `60` for `is set`/`is not set`. No further spec change required.
- **Full test suite**: 2678 passing (254 Analyzer + 2424 Core), 0 failing. Build: 0 errors, 0 warnings.
- **Plan doc updated**: All 14 acceptance-gate items marked ✅. Plan heading updated to "14 Points ✅ ALL RESOLVED".
- **Decision artifact**: `.squad/decisions/inbox/george-phase2-gate-closed.md` written.

---

### 2026-05-01 — Phase 2c/2d/2e closeout recorded
- Phase 2c: `TypeChecker` and `GraphAnalyzer` now carry full `ExpressionFormKind` coverage annotations, Layer 2 `ExpressionFormCoverageTests` landed, and PRECEPT0019 was promoted from warning to error with 2300 tests passing.
- Phase 2d: `Parser.cs` was split into `Parser.cs`, `Parser.Declarations.cs`, and `Parser.Expressions.cs` while preserving the single `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` declaration and keeping the parser green at 2300 tests.
- Phase 2e: PRECEPT0020-PRECEPT0023 analyzers, `TokenMeta.IsValidAsMemberName`, catalog-derived `KeywordsValidAsMemberName`, and a real `SetType` duplicate-text fix all landed; final verification reached 2677 passing tests.
- Scribe merged the Phase 2c/2d decision inbox artifacts and recorded george-10/george-11/george-12 closeout logs.

---

### 2026-05-01 — Phase 2d (Slice 27) complete
- `Parser.cs` split into three `partial` files: `Parser.cs` (~504 lines, core shell + dispatch), `Parser.Declarations.cs` (~1012 lines, all declaration/scope-level parsers), `Parser.Expressions.cs` (~330 lines, Pratt loop + atom parsers + `ExpectIdentifierOrKeywordAsMemberName`).
- Both `public static partial class Parser` and `internal ref partial struct ParseSession` declared in every file.
- `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` present exactly once (primary declaration in `Parser.cs`).
- `KeywordsValidAsMemberName` confirmed as static field on outer `Parser` class (not on `ref struct`); `ExpectIdentifierOrKeywordAsMemberName()` moved to `Parser.Expressions.cs` alongside its only caller (`ParseExpression`).
- Zero behavior change. Build: 0 errors, 0 warnings. Test count: 2300 passing, 0 failing.

---

### 2026-05-01 — Phase 2c (Slices 23–26) complete
- `TypeChecker` annotated: `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` + all 11 `[HandlesForm]` on `private static CheckExpression` stub.
- `GraphAnalyzer` annotated: same pattern with `private static AnalyzeExpression` stub.
- Reflection tests in `ExpressionFormCoverageTests` updated: `ContainSingle` → `HaveCount(3)`, `First()` → iterate all, `BindingFlags.Instance` → includes `BindingFlags.Static`.
- New `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` created: 26 Layer 2 catalog-shape tests.
- PRECEPT0019 promoted to `DiagnosticSeverity.Error`; `<WarningsNotAsErrors>` removed from `Precept.csproj`.
- Full solution: 0 errors, 0 warnings. Test count: 2300 passing, 0 failing (+26 new tests vs 2274 baseline).

---

### 2026-05-01 — Phase 2b closeout recorded
- Scribe recorded George-9's Phase 2b completion: the `OperatorMeta` DU restructure plus `ExpressionFormKind.PostfixOperation` landed with 2274 passing tests and 13 new tests added.
- The Phase 2b decision note from `.squad/decisions/inbox/george-phase2b-du.md` was merged into the canonical ledger during closeout.

## Archive Batch — 2026-05-02T19:42:01Z (overflow trim)

---

### 2026-05-01 — Phase 2b (Slices 19–22) complete
- `Arity.Postfix`, `OperatorFamily.Presence`, `OperatorKind.IsSet/IsNotSet` added to enums.
- `OperatorMeta` restructured as abstract record base + `SingleTokenOp` (18 ops) + `MultiTokenOp` (2 ops: `IsSet`, `IsNotSet`).
- `Operators.ByToken` narrowed to `SingleTokenOp` only; `Operators.ByTokenSequence(params TokenKind[])` added for multi-token lookup.
- `ExpressionFormKind.PostfixOperation = 11` added; `[HandlesForm(PostfixOperation)]` added to `ParseExpression`.
- Consumer call site audit (Slice 22): all `.Token.` accesses in `OperatorsTests.cs` now operate on `SingleTokenOp`-typed variables; no stragglers in source.
- Full solution: 0 errors, 0 warnings. Test count: 2274 passing, 0 failing (+13 new tests vs 2261 baseline).
- Decisions captured at `.squad/decisions/inbox/george-phase2b-du.md`.

---

### 2026-05-01 — Slice 27 parser split decision received
- Frank locked Slice 27 to `partial class Parser` + `partial ref struct ParseSession` with three files: `Parser.cs`, `Parser.Declarations.cs`, and `Parser.Expressions.cs`.
- Keep `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` only on the primary `ParseSession` declaration; let `[HandlesForm(...)]` stay attached to the moved methods in `Parser.Expressions.cs`.
- Shared static vocabulary, including the future `KeywordsValidAsMemberName` set, must stay on the outer `Parser` class because `ref struct` types cannot declare static fields.

---

### 2026-05-01 — Parser-gap branch state summarized
- Branch work through Slice 13 is durably recorded: typed constants, event-handler ensure guards, presence-operator Pratt support, expression-form catalog/coverage, list literals, method calls, spec fixups, and the regression suites from Slices 8–13 are all in place.
- Remaining known-broken sample sentinels still point at three separate gaps: state/event ensure `when` guards, post-expression field modifiers, and keyword member names (`.min` / `.max`).
---

## Archive Batch — 2026-05-02T19:48:45Z

---
### 2026-05-02 — GAP-032: `pow(integer, integer)` ProofRequirement added

- Added `PPowIntExp = new(TypeKind.Integer, "exp")` named constant alongside `PSqrtNumber` in the shared param section of `Functions.cs`.
- Applied `NumericProofRequirement(new ParamSubject(PPowIntExp), OperatorKind.GreaterThanOrEqual, 0m, ...)` to the `Integer^Integer` overload only — Decimal and Number lanes excluded per spec §0.6 item 4.
- Build: 0 errors, 0 warnings. Tests: 2690 passing, 0 failing.
- GAP-032 marked Fixed in `docs/working/language-consistency-gaps.md`.

### 2026-05-02 — Iteration 8: Runtime/Parser Implementation Audit— Parser Catalog Derivation (A), Type Checker Catalog Consumption (B), Lexer Token Classification (C), Evaluator Function/Operator Dispatch (D).
- **TypeChecker**: Stub only (`HandlesCatalogMember` annotations, no logic). Nothing to audit; no violations possible.
- **Evaluator**: Stub only (all methods `throw new NotImplementedException()`). Catalog-driven implementation guide is in place for D8/R4 phase. No violations.
- **Lexer**: Fully catalog-driven. Keyword lookup via `Tokens.Keywords` (catalog). Operator scanning via `TwoCharOperators`, `SingleCharOperators`, `PunctuationChars` (all catalog-derived). No hardcoded lists found. Clean.
- **Parser**: 3 new Catalog-Impl gaps found.
  - **GAP-029** (`IsOutcomeAhead`): Hardcodes `{Transition, No, Reject}` — should derive from `TokenCategory.Outcome`.
  - **GAP-030** (`ParseAtom` min/max cases): Hardcodes `case TokenKind.Min: case TokenKind.Max:` — should derive from `Functions.ByName` ∩ `Tokens.Keywords` as a catalog-driven `KeywordsUsableAsFunctionNames` set.
  - **GAP-031** (unary/postfix binding powers): `not`→25, negate→65, `is set`→60 — all match catalog values but are not read from `Operators.ByToken`/`ByTokenSequence`. Should use named constants derived from catalog.
- **Prior gap verification**: GAP-025 (`Notempty.ApplicableTo` → `StringAndCollectionTypes`) ✅, GAP-026 (`CollectionTypes` 9 members) ✅, GAP-028 (`sqrt` Number-only overload) ✅. All three confirmed correct in catalog.
- **Final count**: 31 gaps total, 28 Fixed, 3 Unresolved.
- **Learnings**: The `OperatorPrecedence` FrozenDictionary in `Parser.cs` correctly excludes unary operators from the binary-only table — but that means unary/postfix binding powers live as bare literals in ParseAtom/ParseExpression with no enforcement mechanism to detect catalog drift. The fix is catalog-derived `private static readonly int` constants on the outer `Parser` class (not `ParseSession` — ref struct can't own statics).

### 2026-05-02 — GAP-019a/019b: InvalidCallTarget and UnexpectedKeyword implemented

- **GAP-019a (`InvalidCallTarget`)**: The infix `LeftParen` branch in `Parser.Expressions.cs` previously had a `// unreachable` comment + silent `break`. `42(args)` and `(A+B)(args)` were silently swallowing the `(args)` tokens, causing cascading `ExpectedToken` errors. Fixed by emitting `DiagnosticCode.InvalidCallTarget` with a short expression description before the `break`.
- **GAP-019b (`UnexpectedKeyword`)**: The `ParseAtom` default fallback previously always emitted `ExpectedToken`. Now it checks `AllKeywordKinds.Contains(current.Kind)` (catalog-derived from `Tokens.Keywords.Values`) and emits `UnexpectedKeyword` for keywords, `ExpectedToken` for non-keywords.
- **`AllKeywordKinds`**: Added as a `FrozenSet<TokenKind>` to the outer `Parser` class (not `ParseSession` — `ref struct` cannot own static fields). Derived from `Tokens.Keywords.Values.ToFrozenSet()`. Fully catalog-driven.
- **`DescribeCallTarget`**: Private static helper on `ParseSession` that returns a human-readable label for the non-callable expression in the diagnostic message.
- **Test coverage**: 6 new tests added to `ExpressionParserTests.cs` covering both gaps and the regression case.
- **Pre-existing WIP conflict**: A WIP change to `Tokens.cs` (Arrow: `Cat_Str` → `Cat_Op`) was in the workspace and broke the committed test `Arrow_IsStructural_NotExpressionOperator`. Reverted `Tokens.cs` to the committed state — the Arrow category change is out of scope for this task.
- **Final validation**: 2692 passing tests, 0 failures.


- George-8's follow-up on Frank's review is now durable: PRECEPT0013 dropped the RS1030 `Compilation.GetSemanticModel()` path and `CatalogAnalysisHelpers` carries the Phase 3 TODO for `ConstraintKind` / `ProofRequirementKind` coverage gating.
- Soup-Nazi-4 then closed the 6 missing-test gaps from the full coverage review and fixed the RS1030 follow-on issue, pushing branch validation to 2687 passing tests.
- Coordinator commit `4d988d8` added commented-out `ConstraintKind` / `ProofRequirementKind` entries in `CatalogEnumNames`; treat them as future activation context, not a live Phase 3 completion.

### 2026-05-01T20:10:18Z — HandlesCatalogMember rename shipped
- George-7 mechanically renamed `[HandlesForm]` to `[HandlesCatalogMember]` across the shared attribute, distributed-dispatch call sites, PRECEPT0019, tests, and docs in commit `08fdf85` on `spike/Precept-V2`.
- Validation closed green at `2424/2424` tests passing; treat remaining `[HandlesForm]` mentions in old notes as historical rename context only.
