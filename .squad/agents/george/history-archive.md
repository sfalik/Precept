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

---

## Archive Batch — 2026-05-02T21:58:21Z (scribe compaction)

---

## Recent Updates

### 2026-05-02 — Frank accepted George's type checker review
- Frank accepted 5 of George's 6 findings on the type-checker design review; the remaining item was reclassified as a non-finding because GAP-032 was already closed.
- Frank's response resolves all 5 pre-requisites George surfaced: existing `FindCandidates`/`FindUnary` stay canonical, pre-Slice 0 shape records are now mandatory, field storage stays array-primary with a derived frozen name index, `TypedInputAction` gains an explicit secondary-role discriminator, and `[HandlesCatalogMember]` ownership must migrate from the stub to real handlers slice by slice.
- The revised plan also locks explicit follow-through for partial-result error recovery, qualifier propagation, `ContentValidation` as a DU, and concrete slice ownership for method calls plus interpolated forms.

### 2026-05-02 — Historical Summary (compacted)
- Archived older update entries to `history-archive.md` until `george` history returned to the active-size target.
- Durable themes retained in active history: current ownership, catalog-driven parser/runtime rules, and the newest implementation/audit outcomes.
- Use the archive for the full slice-by-slice closeout trail and earlier review context.

### 2026-05-02 — Active implementation snapshot
- Detailed GAP-032, Iteration 8 audit, GAP-019 implementation, and rename-shipping notes were moved to `history-archive.md` to bring active context back under the size gate.
- Active durable baseline: `pow(integer, integer)` proof requirements are fixed, GAP-019a/019b are shipped, and the Iteration 8 audit still defines the remaining parser catalog-derivation follow-ons before real TypeChecker/Evaluator implementation begins.
- The newest type-checker-design review adds the current checker guardrails: pre-Slice 0 `SemanticIndex` shapes, array-primary field ordering, existing `Operations` multi-candidate lookup usage, `SecondaryRole` stamping, and disciplined `[HandlesCatalogMember]` stub migration.

### 2026-05-02 — Iteration 10 Catalog-Impl Audit (GAP-060, GAP-061)

- **Variant-action dead arms can hide in sub-switches, not just the top-level dispatch.** GAP-042 cleared the three dead arms from ParseActionStatement's top-level switch. GAP-060 found the same pattern one level down in ParseCollectionIntoStatement's bottom switch: ActionKind.DequeueBy was emitting a real DequeueByStatement instead of throwing. Rule: any arm for a variant action (PrimaryActionKind != null) inside any shape-specific method must be throw — not a constructor call.
- **Modifiers was the last catalog without an O(1) parser-facing token index.** GAP-061 added Modifiers.ByFieldToken (FrozenDictionary of TokenKind to FieldModifierMeta) to replace a LINQ linear scan in ParseFieldModifierNodes. Pattern: all five major parser-facing catalogs (Actions.ByTokenKind, Types.ByToken, Constructs.ByLeadingToken, Operators.ByToken, Modifiers.ByFieldToken) now have FrozenDictionary token-keyed lookup. The scope is intentionally narrow — only FieldModifierMeta, matching the actual lookup need in ParseFieldModifierNodes.
- **All Iteration 9 regression points confirmed clean:** GAP-029/030/031/034/035/040/042 — no regressions.
- **Build environment quirk (shared env):** dotnet build (full solution) fails transiently on .msCoverageSourceRootsMapping_* file locks — a file-lock collision with another process. dotnet build src/Precept/Precept.csproj and dotnet test test/Precept.Tests/ --no-restore both work cleanly. Use targeted build + --no-restore for the inner loop on this machine.

---

## Archive Batch — 2026-05-02T21:58:21Z (final active-history compaction)

---

## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: closed catalog extensibility hardening, PRECEPT0018 analyzer enforcement, parser whitespace-insensitivity, typed constants, event-handler ensure guards, presence-operator Pratt support, the expression-form catalog/annotation bridge, list literals, method calls, the sample/coverage regression layer, and Phase 2a+2b of the parser-gap fix plan (GAP-A/B/C + OperatorMeta DU restructure).
- Current ownership: GAP-019a (`InvalidCallTarget`) and GAP-019b (`UnexpectedKeyword`) diagnostic emission now implemented and tested.

## Learnings

### 2026-05-02 — GAP-035, GAP-040, GAP-042 implementation

- **`TypeMeta.ChoiceLiteralTokens`** (`IReadOnlyList<TokenKind>?`, null default): the catalog field that drives `ParseChoiceValue` dispatch. Null on all ~25 non-choice types — zero churn. Populated for the 5 `ChoiceElement` types: `Integer/Decimal/Number → [NumberLiteral]`, `String → [StringLiteral]`, `Boolean → [True, False]`. The signed-numeric path is `literalTokens?.Contains(NumberLiteral) == true`; the general validity check is `literalTokens?.Contains(cur.Kind) == true`. No `elemToken.Kind is ...` identity switch anywhere in the method. Both dispatches (numeric guard AND the elemToken.Kind switch below it) were eliminated together — Frank was right that catching only the first one would leave the method half-catalog.
- **`ElementParameterAccessor` DU subtype** — mirrors `FixedReturnAccessor` pattern for the parameter axis. No `ParameterType` on base record (passes `null`). Type checker pattern-matches: `ElementParameterAccessor => resolveAgainstElementType`, `_ when accessor.ParameterType is { } => resolveAgainstFixed`, `_ => noParameter`. The DU subtype IS the metadata — illegal state between `ParameterType` and a flag is structurally impossible. `countof` was a copy-paste bug from `at()` (integer index). MCP tools only contain PingTool; no DTO updates required.
- **GAP-042 dead code removal**: variant actions (`PrimaryActionKind != null`) are excluded from `Actions.ByTokenKind` by design. `CollectionValueBy`, `RemoveAtIndex`, and `CollectionIntoBy` SyntaxShape arms in `ParseActionStatement` were dead and their private methods unreachable. The actual `by`/`at` handling was already inline in `ParseCollectionValueStatement` and `ParseCollectionIntoStatement`. Deleted the 3 methods and 3 arms, replaced the incomplete named-value switch with a discard `_ => throw InvalidOperationException(...)`. Pragma `CS8524` removed (discard covers unnamed values too). Zero behavior change — all 2713 tests pass.

### 2026-05-02 — GAP-046: FunctionKind.TildeStartsWith/TildeEndsWith

- **CI functions needed `FunctionKind` enum entries.** `~startsWith` and `~endsWith` were in the spec §3.7 function table but had no catalog entries. Added `TildeStartsWith = 22` and `TildeEndsWith = 23` to `FunctionKind.cs`, with full `FunctionMeta` entries in `Functions.cs` including type signature `(string, string) → Boolean`.
- **`CIVariantOf: FunctionKind?` on `FunctionMeta`.** The new field expresses the CI→base relationship as catalog metadata — the inverse of `HasCIVariant` on the base function. Consumers should pattern-match `CIVariantOf != null` to identify CI entries rather than testing for `~` prefix. Downstream note: Kramer should use `Functions.All.Where(f => f.CIVariantOf != null)` for completions after `~`, NOT `HasCIVariant`.
- **`Names_AreLowerCamelCase` regex update.** The test checked `^[a-z][a-zA-Z]*$` — doesn't allow the tilde. Updated to `^~?[a-z][a-zA-Z]*$`. The leading `~` is intentional and part of the function's canonical surface name in the language.
- **Four test count updates required.** `Total_Count` (21→23), `Total_OverloadCount` (47→49), `ByName_UniqueNames` (20→22), and the name regex. Enum insertion between 20 (Mid) and 21 (Now) is fine — `Enum.GetValues` returns by numeric value, so ordering stays ascending.
- **Parser zero-change gate held.** `CICapableFunctionNames` derives from `HasCIVariant` on base functions — the new CI-variant entries have `HasCIVariant: false` (they ARE the CI form, not the base). No parser churn.



- **`Operations.BinaryIndex` / `UnaryIndex` / `FindCandidates` / `FindUnary` already exist.** Frank proposed `BinaryBySignature`/`UnaryBySignature` as new additions — they are already implemented under these names. Critical difference: `BinaryIndex` returns `BinaryOperationMeta[]` (array), not a single entry. Money/money and quantity/quantity disambiguation requires multi-candidate handling in the checker.
- **SemanticIndex record type definitions must be committed BEFORE any slice implementation begins ("pre-Slice 0").** Without the full record shape in place, no slice test can compile.
- **7 AST expression node types missing from Frank's Resolve pseudocode:** `IsSetExpression`, `IsNotSetExpression`, `CIFunctionCallExpression`, `MethodCallExpression`, `InterpolatedStringExpression`, `InterpolatedTypedConstantExpression`, `TypedConstantExpression`. All exist in `SyntaxNodes/Expressions/`. The core Resolve function is ~250-350 lines, not ~100.
- **GAP-032 (`pow` ProofRequirement) was fixed 2026-05-02** — Frank flagged it as an open gap but it is already closed.
- **Field ordering in SemanticIndex:** `ImmutableDictionary<string, TypedField>` loses declaration order. Preferred pattern: `ImmutableArray<TypedField>` primary + derived `FrozenDictionary` secondary (same pattern as `Functions.ByName`).
- **`[HandlesCatalogMember]` stub migration:** Each slice implementing a real expression form must REMOVE the annotation from the stub `CheckExpression()` and re-apply it to the real handler. Omitting this causes PRECEPT0019 duplicate-coverage diagnostics.
- **`TypedInputAction.SecondaryExpression` needs a role discriminator** (Index / Key / Priority) — a single nullable with a comment is insufficient for the Evaluator.
- **`InterpolatedStringExpression` / `InterpolatedTypedConstantExpression` have no slice in Frank's plan.** Assign to Slice 8 or define as Slice 8a.
- **`ContentValidation` shape for Gap 1 needs a DU** — a flat `string Pattern` hides a per-type switch between regex, NodaTime parse-delegate, and closed-set membership validation strategies.

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



- **Catalog-derived set for keyword-as-function-name tokens**: The correct pattern for `ParseAtom` keyword-function ambiguity is a pre-switch check against a `FrozenSet<TokenKind>` derived at startup from `Functions.All` ∩ `Tokens.Keywords`. This avoids hardcoded switch arms and auto-extends to future functions whose names collide with keywords. The check must precede the switch entirely (not use goto-case or fallthrough) to avoid code duplication.
- **Edit no-ops in shared worktrees**: When working on a shared branch, a file may already carry the expected changes from a parallel commit. My `edit` to `Parser.Expressions.cs` was a no-op because `ea18430` (GAP-031) already included the ParseAtom restructure. Only `Parser.cs` was actually new. Verify with `git diff HEAD` before committing to confirm the actual change scope.



- **RS1030 pattern**: When an analyzer follows a field reference to its declaring syntax to get operations (e.g., checking if a shared array is empty), use `ctx.SemanticModel` if `syntaxNode.SyntaxTree == ctx.SemanticModel.SyntaxTree`, and return early (assume non-empty) if not — never call `compilation.GetSemanticModel()` inside an analyzer. The `CompilationStartAction` wrapper is only needed when per-compilation state must be built up; if the only use was to capture `compilation` for `GetSemanticModel`, remove it entirely and register the `OperationAction` directly.
- **RS1030 + CatalogEnumNames**: `ConstraintKind` and `ProofRequirementKind` cannot be added to `CatalogEnumNames` until their `GetMeta` switches drop the discard arm; adding them prematurely causes PRECEPT0007 to fire on those fallback arms. Track this as a Phase 3 prerequisite via a TODO comment.

## Learnings (continued)

- **Dapr Actor model maps tightly to Precept's execution semantics.** Dapr's turn-based single-threaded access per actor ID is exactly what Precept's immutable-Version, single-event-at-a-time model requires. No concurrent writes to the same instance are possible by construction.
- **The compiled `Precept` definition must be cached per pod, not per actor.** The `Precept` object is "thread-safe, shareable, cacheable" by design — mirroring CEL's Program. In a Dapr deployment, a static `ConcurrentDictionary<string, Precept>` keyed by definition hash per pod is the correct caching strategy. Recompiling per actor activation would be expensive and pointless.
- **JSON deserialization into `object?` is a silent correctness trap.** Precept field values travel as `IReadOnlyDictionary<string, object?>`. When Dapr state stores roundtrip these through JSON, System.Text.Json returns `JsonElement` for number and boolean fields — not `int`, `bool`, `decimal`. Guard expressions evaluated against these will silently fail or produce wrong results. A typed deserialization shim keyed from `FieldDescriptor.Type` is required before guards can be trusted.
- **Dapr Workflow is the wrong model for Precept instances.** The replay/event-sourcing model is designed for orchestration workflows that span multiple services and need unbounded history. Precept instances need a compact current-state snapshot, not a growing event log. The mismatch is fundamental: Precept evaluates against a point-in-time state; Dapr Workflow reconstructs state by replaying the full event sequence from the beginning.
- **State store model is viable but requires external concurrency protection.** OCC via ETags catches concurrent writes but doesn't prevent the race — it just forces a retry. For semantically correct single-event-at-a-time processing, actor placement (which pins an instance to one node at a time) is strictly better than hoping OCC retries are rare.
- **Actor reminders can drive time-based Precept transitions**, because reminders persist across actor deactivation and fire even on cold-start. A reminder registered with the event name as its identifier can call `Fire(eventName)` on reactivation.
- **The `Restore()` contract is the Dapr-state boundary.** Every actor activation that pulls state from the store should call `Restore(stateName, fieldDictionary)` before any `Fire()` or `Update()`. `Restore` validates the persisted data against the current definition, handles schema evolution, and recomputes computed fields. Skipping it is a correctness defect.

- **Named `ParameterMeta` constants are required for proof obligations** — the same instance must appear in both the overload's `Parameters` list and the `NumericProofRequirement`'s `ParamSubject` to satisfy PRECEPT0005 reference-equality. Inline `new(...)` construction at call sites will silently create two distinct instances and break the proof engine's subject-resolution. Always define a `private static readonly ParameterMeta P<Name>` constant and share it.

## Recent Updates

### 2026-05-02T21:58:21Z — Canonical type checker review accepted
- George's canonical checker review is now durable: Frank accepted 5 concerns and 3 missing items, rejected transitive widening, and updated docs/compiler/type-checker.md so the plan is implementation-ready end-to-end.
- The active guardrails George surfaced remain locked: pre-Slice 0 semantic-model shapes come first, field storage stays array-primary with a derived frozen lookup, operation resolution uses existing multi-candidate Operations APIs, TypedInputAction needs an explicit secondary-role discriminator, and real expression handlers must take over [HandlesCatalogMember] ownership from the stub slice by slice.
- Kramer and Soup-Nazi inputs now sit beside the checker plan: tooling stays derivation-first (no mirrored ResolvedArgs cache), and the checker implementation is expected to satisfy the 450-550 test envelope plus the 3 non-negotiable test gates.

### 2026-05-02 — Active implementation snapshot
- Iteration 10 catalog-impl audit findings (GAP-060 and GAP-061) remain part of the live parser baseline: variant-action arms inside shape-specific methods must throw, and parser-facing catalogs should expose O(1) token-keyed indexes.
- The checker/evaluator start point is now explicit: partial-result error recovery, qualifier propagation, event-arg scope in event handlers, and interpolated/method-call slice ownership are no longer open design questions.

### 2026-05-02 — Historical Summary (recompacted)
- Older recent-update detail was moved to history-archive.md during Scribe closeout to keep active context under the 15 KB gate.
- Use the archive for the Phase 2 closeout trail, rename-shipping notes, and earlier parser/analyzer implementation history.