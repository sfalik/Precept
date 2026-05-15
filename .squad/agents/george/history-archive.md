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







---







## Archive Batch — 2026-05-11T01:38:51Z







---







### 2026-05-10T20:56:42Z — Track 2 slices 4/9/10/11 durably recorded



- George-6's commit `df874e15` established `OperatorMeta.ResultType` / `ResultTypePolicy` as the catalog authority for operator result typing, including the `OperationResult` handoff to `OperationMeta.Result` for arithmetic.



- George-7 finished t2-9 in `b7868d60` and `2f75c829`: TypeChecker now consumes operator typing metadata directly, tightened adjacent choice/quantifier/modifier typing, and closed BUG-002,003,007,009,010,028,029,038,040,046,052,053 at 3,925 / 3,925.



- George-8 finished t2-10 in `def91dbb` and `b08b1fc4`: wildcard/broadcast name resolution is catalog-derived, computed fields bind via stable topological ordering with cycle diagnostics, and BUG-001,026,030,037 closed at 3,911 / 3,911.



- George-9 finished t2-11 in `004e68be`, `e48c0071`, and `599206b6`: proof obligations now project by proof-site metadata, collection mutation diagnostics bind real field names, 5 new proof tests landed, and the transient shared-tree binder failures were superseded by George-7's clean full run.







---







### 2026-05-10T19:47:35Z — Grammar doc-fix commits and validation recorded



- George-5 durably closed the grammar/spec/catalog documentation batch in commits 9b8e8384 and b8e7df94, covering the precept-grammar.md correction pass plus the removal of illegal trailing-ensure EventHandler grammar and obsolete SupportsPostActionEnsure documentation.



- The squad ledger, orchestration log, and this history now agree on the batch boundary, so follow-up doc work should cite the committed artifacts rather than the deleted inbox notes.



- Final validation stayed green across all four test projects at 4,388 passing tests.







---







### 2026-05-10T15:34:08Z — Slice 2E and Slice C closeouts recorded



- Scribe merged both your t2-2 Slice C note and your Slice 2E BUG-049a completion into `.squad/decisions.md`, with BUG-049a paired to Frank's approved design review as one canonical closeout entry.



- Durable implementation rules now recorded: shape-method separators come from `Actions.GetShapeMeta(...).Slots[n].PrecedingSeparator`, and intrinsic non-negative accessor returns discharge through `FixedReturnAccessor.ReturnNonnegative` while action cardinality obligations reuse the single shared `Types.CollectionCountAccessor`.



- Validation anchors now captured in the ledger: Slice C stayed green at 4056/4056 on `ef6fedcb`; Slice 2E closed targeted build + tests at 3857 passing on `f2d1dece` and `e826e4bd`.







---







## Archive Batch — 2026-05-11T20:03:33Z







---







## Core Context







- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.



- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.



- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.



- Action syntax work must stay metadata-first: slot roles are typed catalog values, separator tokens derive from slot metadata, and optional slots replace ad-hoc support flags.







## Learnings







### 2026-05-11 — ConflictingModifiers diagnostic implemented (optional + notempty)







- `optional notempty` was silently accepted; Frank's verdict: compile error.



- Added `DiagnosticCode.ConflictingModifiers = 120` in the Type section of `DiagnosticCode.cs`.



- Added the full catalog entry in `Diagnostics.cs` adjacent to `WritableOnEventArg` (structural modifier group).



- Wired the type checker: the mutual-exclusion fallback in `TypeChecker.Validation.cs` previously emitted `InvalidModifierForType` with a string-concatenated conflict message; changed it to emit `ConflictingModifiers` with two clean token-text args matching the `{0}` / `{1}` template.



- Added `MutuallyExclusiveWith: [ModifierKind.Optional]` to the `Notempty` catalog entry.



- **Key surprise:** The task instructions said the one-way declaration on `Notempty` was sufficient. It is NOT — `PRECEPT0011c` (Roslyn analyzer in `Precept.Analyzers`) enforces symmetric `MutuallyExclusiveWith` declarations at compile time. I also had to add `MutuallyExclusiveWith: [ModifierKind.Notempty]` to the `Optional` entry. The build fails with a hard error if symmetry is violated.



- Fixed the sample `apartment-rental-application.precept` to remove `notempty` from the `Approve` event arg.



- Build: `dotnet build src/Precept/Precept.csproj` — 0 errors, 0 warnings.







### 2026-05-11T14:55:38.348-04:00 — Interpolation LOE review exposed the real runtime/tooling cost







- Frank's frank-23 plan is workable, but the real cost is Slice 2: the type-grammar matcher, segment classifier, three new diagnostics, and typed-node propagation push the feature to roughly 795 changed lines when tests and required walkers are counted.



- The plan currently under-lists critical traversal follow-through: `NameBinder` still only walks `InterpolatedStringExpression`, and the language server has four separate interpolated-string walkers beyond `SemanticTokensHandler` that will all need `TypedInterpolatedTypedConstant` support.



- The hardest design edge is temporal compounds: the doc says `duration`/`period` compounds generalize to N components, while the slice text assumes a small finite matcher. That scope choice changes implementation complexity more than any parser work does.







### 2026-05-11T09:32:39.453-04:00 — Pipeline audit fixes landed cleanly on feature/pipeline-audit-fixes







- Added repo-root `Directory.Build.props` so the workspace defaults to a single Release-with-PDB build surface.



- Eliminated every remaining `Debug.Assert` site in `src/Precept/` and converted D26/D5/mode-stack invariants to unconditional `InvalidOperationException` guards; the pipeline no longer has build-configuration-dependent safety nets.



- Closed the D26 diagnostic gaps by emitting `TypeMismatch` before returning `TypedErrorExpression`/error-typed `TypedAction` from operator lookup failures plus the `Resolve()` / `ResolveAction()` defensive fallbacks.



- `Fault.cs` already had the requested `ExpressionContext` and `InputValues` fields, so I verified the shape and added regression coverage instead of re-implementing it.



- The fix-plan's GraphAnalyzer loop assumed `TypedEvent.Modifiers`, but the current semantic model exposes only `TypedEvent.IsInitial`; I derived the active event modifier set from that surface before dispatching through `EventModifierMeta.RequiredAnalysis`, which preserves the intended invariant without widening the typed-event model mid-fix.



- Validation closed green with `dotnet build src/Precept/Precept.csproj -c Release` after each change group and a final `dotnet test test/Precept.Tests/ -c Release` run at 4,598 / 4,598 passing.







### 2026-05-11T00:08:48-04:00 — Text qualifier axis: design drafted, implementation blocked







- **System state found:** `QualifierAxis` has 9 members (None, Currency, Unit, Dimension, FromCurrency, ToCurrency, Timezone, TemporalDimension, TemporalUnit). All are single-value axes mapped to external catalogs. `text`/`String` type has `QualifierShape: null` — no qualifier support at all.



- **Design gate triggered:** Frank's V1/V2 scope ruling (`.squad/decisions/inbox/frank-typed-literal-completion-review.md`) explicitly deferred `text` qualifier-aware mode to V2, calling out "no current qualifier shape for `text`". No approved design exists for the DSL type-system change.



- **Key design tension:** `field Status as text in ['pending', 'active', 'closed']` requires the FIRST multi-value qualifier in the system. All existing qualifiers take a single `TypedConstant`: `in 'USD'`, `in 'days'`. A closed string set is structurally different and may require a `ParsedQualifier` discriminated union if bracket-list syntax is chosen. This is precedent-setting.



- **Design doc written:** `docs/Working/george-text-qualifier-design.md` covers syntax options (bracket-list vs repeated-single-value), `DeclaredQualifierMeta.TextValues` shape, parser change surface, type checker behavior, runtime enforcement question, Kramer's integration point, and Newman's MCP concern.



- **Inbox note:** `.squad/decisions/inbox/george-text-qualifier-design-needed.md` — implementation is blocked pending Frank + Shane sign-off on the design doc.



- **No implementation code written.** Design gate respected.







### 2026-05-10T15:38:30-04:00 — SupportsPostActionEnsure removed (BUG)



- Code commit: `c1572613`; test commit: `5be86341`. Final suite: 4,388 total (3,891 Precept.Tests, 280 Analyzers.Tests, 157 LS.Tests, 60 Mcp.Tests). Zero failures.



- `SupportsPostActionEnsure` was an out-of-band parser injection flag that grafted EventEnsure slot semantics (`ensure expr because reason`) onto EventHandler after the main slot-walk. This violated the `on`-family disambiguation contract: `ensure` and `->` are mutually exclusive routing tokens — the parser must never mix their semantics on a single construct.



- Removal pattern: delete the flag from `ConstructMeta`, remove it from the `EventHandler` catalog entry, delete the conditional post-slot-walk block in `ParseScopedConstruct`. Three tests asserting the deleted behavior were removed. No replacement — the behavior was wrong, not merely misphrased.



- This confirms the same principle as `SupportsPreVerbWhenGuard`: ad-hoc support flags on `ConstructMeta` are always wrong. If a construct needs extended parsing, that extension must be encoded as catalog-driven optional slots in the slot walk.







### 2026-05-10T15:32:08-04:00 — BUG-020 committed; full suite confirmed green



- All 6 BUG-020 commits landed cleanly on `Precept-V2-Radical`: core implementation (`b5dc7c3e`), tests (`ec068569`), grammar/spec/catalog docs (`eb225f8a`), samples (`4a6cb93f`), working docs (`103c3be1`), squad history (`078dbe32`).



- Final test count: 4,391 across Precept.Tests (3,894), Analyzers.Tests (280), LanguageServer.Tests (157), Mcp.Tests (60). Zero failures.



- The `SupportsPreVerbWhenGuard` removal confirms the pattern: optional pre-verb clauses should be catalog-driven optional slots in the slot walk, not ad-hoc flags on the construct record. The parser stays metadata-first.







- `CollectionValueBy` (AppendBy, EnqueueBy) and `RemoveAtIndex` (RemoveAt) are "secondary" action shapes: their `PrimaryActionKind` is non-null, so they are excluded from `Actions.ByTokenKind`. The parser NEVER directly dispatches to these shapes via `ByTokenKind`. Their parse methods exist in the switch but are unreachable from the normal action chain parse path. Tests for these shapes must be catalog property tests, not behavioral parser tests. Behavioral coverage requires the type-checker conversion path.



- When propagating shape-specific context through shape-method signatures, the cleanest approach is to compute the FrozenSet once at the dispatch site (ParseActionByShape) and pass it as a parameter to every shape method. This avoids re-computing per call site and keeps the shape methods unaware of the catalog lookup.



- `ParseExpression`'s `terminates()` lambda is checked in the WHILE loop (after ParseNud), not before ParseNud. ParseNud runs unconditionally on the current token. This means the termination set only matters for preventing led-loop continuation, not for gating the initial expression parse. For tokens with no binary-operator led binding power (=, into, by, at), the while loop breaks naturally via GetLedBindingPower = -1. The old hardcoded termination was redundant for the outer while loop but the design fix is still correct because it documents shape-specific intent and prevents future breakage if these tokens gain binary-operator roles.



- Slice 8 guard parsing needed disambiguation awareness: for `in/to/from` constructs with optional pre-verb `when`, the disambiguation keyword (`ensure`/`->`) may appear after an expression, so candidate selection must scan past the guard rather than relying on fixed `Peek(2)`.



- Typed constants were already valid expression atoms in the Pratt parser, but field modifier value parsing still hardcoded start tokens and skipped `TypedConstant`; using `ExpressionStartTokens` in modifier parsing fixed the leak and prevented stray top-level `ExpectedToken` failures.



- Forward references in computed expressions cannot be validated by declaration order alone. Allowing all-field resolution for computed formulas plus cycle diagnostics (`CircularComputedField`) is the stable contract; declaration-order enforcement remains appropriate for default expressions only.







- `ActionSlotRole` must be a typed enum, not a string on `ActionSyntaxSlot.Role`. Freeform strings on catalog records are always wrong — the catalog is the source of truth and its values must be first-class types.



- Project analyzer PRECEPT0018 requires explicit 1-based enum values. New semantic enums must declare `= 1` on the first member and renumber accordingly.



- `CollectionIntoBy`'s final slot is an output capture variable, so the durable role name is `OrderingCapture`, not `OrderingKey`.



- When removing a flag field (`IntoSupported`) that is equivalent to derived slot metadata, keep the downstream DTO stable by deriving the old surface from `GetShapeMeta()` instead of preserving duplicate state.



- `CollectionValue` and `CollectionValueBy` both have positional value slots with `PrecedingSeparator = null`; slot well-formedness should validate first-slot/null and `SeparatorTokens` consistency, not require separators on every later slot.



- Shape method bodies call `Actions.GetShapeMeta(ActionSyntaxShape.X)` directly and index into `Slots[n].PrecedingSeparator!.Value` for both required `Expect()` calls and optional `if (Peek().Kind == slot.PrecedingSeparator)` guards. This is the correct pattern — shape methods know their own shape identity, so calling `GetShapeMeta` inside is clean and allocation-cheap compared to the alternative of widening the parameter signature from `FrozenSet<TokenKind>` to full `ActionShapeMeta`.



- String literal `LiteralExpression.Text` is the raw lexed text WITHOUT surrounding quotes. `"Walk"` in source produces Text = `Walk`. Tests asserting on literal text must not include quote characters.



- Shared-environment MSBuild cache-file locks (`MSB3492: Could not read existing file`) are reliably cleared by deleting the offending `.cache` file manually (`Remove-Item`) before the build invocation. The lock is always stale (held by the VS Code language server OmniSharp/Roslyn process); deleting the file unblocks the build without killing any process.



- BUG-049a fix pattern: if a numeric accessor is structurally guaranteed non-negative, carry that fact on `FixedReturnAccessor.ReturnNonnegative` and let Strategy 2 discharge `>= 0` directly from accessor metadata. For collection counts, unify all action obligations on `Types.CollectionCountAccessor` (B1) instead of duplicating local accessor instances.



- `SupportsPreVerbWhenGuard` was pure duplicate metadata. Scoped constructs can encode pre-verb `when` support entirely by placing an optional `GuardClause` slot in the ordered slot list with construct-specific termination tokens (`Ensure`, `Arrow`, `Modify`).



- `ParseScopedConstruct` does not need phased anchor/guard/disambiguation handling. A single slot walk works if it checks for family disambiguation tokens before each post-anchor slot, preserves the `->` exception for `ActionChain`, and lets slot metadata drive everything else.







### 2026-05-10T16:59:02.8292215-04:00 — t2-4 OperatorMeta result typing landed



- `OperatorMeta` now carries `ResultType` and `ResultTypePolicy`; the old `StaticResultType` / `LookupValueType` / `ArithmeticPromotion` naming is gone. The durable policy set is `Fixed`, `LhsType`, `ElementType`, `BothOperands`, and `OperationResult`.



- Catalog assignments are now explicit: `or` / `and` declare `ResultType = boolean` with `BothOperands`; `not`, comparisons, `contains`, `is set`, and `is not set` declare `boolean` with `Fixed`; unary `-` uses `LhsType`; binary arithmetic uses `OperationResult`; `for` (`LookupAccess`) uses `ElementType` so t2-9 can read the lookup value type from the typed LHS metadata.



- `OperationResult` is the important shape decision for t2-9: arithmetic cannot be modeled as simple promotion because temporal and business-domain operator results come from `Operations.GetMeta(...).Result`, not from a primitive widen rule. Final validation for this slice: `dotnet test test/Precept.Tests/` green at 3,899 passing.







## Historical Summary







- Earlier 2026-05-09 and 2026-05-10 work completed the typed-literal system, enriched diagnostics/quickstart/syntax catalogs, added `TypedField.NameSpan` and `ArgReference`, landed outline/snippet/catalog metadata, shipped the Track 2 Phase A safe batch, renamed the value-modifier family, and closed the TokenMeta alias cleanup plus BUG-039 documentation follow-through.



- Durable chronology, rationale, and commit anchors live in `.squad/decisions.md`; this history keeps only the live implementation guidance George needs for the next slices.



- 2026-05-10T09:53:14Z — t2-2 Slice C: shape method body rewire: George-6 completed Slice C by replacing hardcoded separator tokens in all 7 affected shape methods: `ParseAssignValueAction`, `ParseCollectionIntoAction`, `ParseCollectionValueByAction`, `ParseInsertAtAction`, `ParseRemoveAtIndexAction`, `ParsePutKeyValueAction`, `ParseCollectionIntoByAction`.



- 2026-05-10T09:53:14Z — t2-2 Slice B: ParseActionTarget shape-specific separators: `ParseActionTarget` now accepts `FrozenSet<TokenKind> separators` from `Actions.GetShapeMeta(meta.SyntaxShape).SeparatorTokens`; old hardcoded `{=, into, by, at}` union is gone.



- 2026-05-10T13:53:14Z — t2-2 Slice A complete with typed operand roles: Shane's scope ruling is now durable: no deferrals inside the slice, operand roles are in scope now, `ActionSyntaxSlot.Role` must be `ActionSlotRole`, and `IntoSupported` is removed rather than preserved beside slot metadata.



- 2026-05-10T13:53:14Z — Scribe handoff for t2-2 Slice B: George-5's Slice B result is now recorded in `.squad/decisions.md` and the orchestration/session logs, so future parser separator work should use the canonical ledger entry rather than the transient inbox note.



- 2026-05-11T01:38:51Z — Older recent-update entries were summarized into `history-archive.md`; keep this file focused on live guidance and the newest batch context.







## Recent Updates







### 2026-05-11T01:38:51Z — Terminal-state split closed with clean downstream validation



- George's C119/C108 graph-analyzer split is now the canonical implementation for terminal-state diagnostics; `DeadEndStateFact` keeps proof suppression semantics while vacuous no-terminal warnings stay gone.



- Soup Nazi's downstream test/handler fixes are part of the same durable batch boundary: `MemberAccessExpression` helpers now stamp both spans, arg-reference navigation again prefers `Event.Arg`, and the full suite closed green at 5,085 / 5,085.







### 2026-05-10T21:11:48-04:00 — Terminal-state diagnostic split implemented (StructuralSinkState + gated DeadEndState)



- Added `StructuralSinkState = 119` to `DiagnosticCode`; added full catalog entry in `Diagnostics.cs`.



- `GraphAnalyzer.Analyze()` now computes structural sinks (reachable, non-terminal, zero outgoing) first and fires C119 (Message A) unconditionally.



- `DeadEndState` (C108 / Message B) is now gated: only fires when `terminalStates.Length > 0`, and excludes structural sinks to prevent double-firing.



- `DeadEndStateFact` contains: BFS dead-ends when terminals exist; only structural sinks when no terminals exist. This preserves ProofEngine obligation suppression without false claims.



- Key invariant: structural sinks can never appear as `fromState` in proof obligations (no outgoing transitions), so removing them from the fact when terminals are absent is safe for the ProofEngine.



- Updated 2 existing `GraphAnalyzerTests` (`Analyze_DeadEndState_EmitsWarningAndDeadEndFact`, `Analyze_MultipleDeadEndStates_AllReported`) and added 2 new tests (`Analyze_NoTerminalStates_StructuralSinkFires_DeadEndDoesNot`, `Analyze_DeadEndState_WithOutgoingTransitions_FiresMessageB`).



- Pre-existing `TypeCheckerFunctionTests.cs` build errors (MemberAccessExpression missing Span parameter) are unrelated to this work — they exist in the working tree and were present before this task.







### 2026-05-11T00:27:07Z — BUG-057 temporal qualifier fix recorded



- Commit `2763a433` fixed `TypeChecker.ExtractQualifiers()` so `period of 'date'` / `period of 'time'` and `period in 'days'` qualifiers survive into semantic metadata instead of being dropped.



- Added temporal qualifier diagnostics (`InvalidTemporalDimensionString`, `InvalidTemporalUnitString`) plus 7 regression tests in `TypeCheckerSymbolTests`; the batch closes at 4,531 core tests and 105 MCP tests passing.







### 2026-05-10T23:55:32Z — BUG-057 routed to Slice 8; t2-16 plan updated



- George-7 narrowed BUG-057 to field-type/parser support for qualified `period` declarations, updated `precept-toolchain-bugs.md`, and wrote `george-bug057-slice-assessment.md` recommending Slice 8 as the first implementation home.



- George-6 appended the t2-16 DTO Source Generator slice spec to `precept-toolchain-plan.md`, so Track 2 now has an explicit generator-planning slice ready for implementation follow-through.







## Archive Batch — 2026-05-12T02:12:11Z







---







### 2026-05-11T21:23:24.768-04:00 — Slice 12 temporal chain validation complete







- `src/Precept/Language/Operations.cs`: `Operations.Create()` switch arms at lines 615-633 now add `QualifierChainProofRequirement` entries for `PriceTimesPeriod` and `PriceTimesDuration`.







- Constructor shape confirmed from `src/Precept/Language/ProofRequirement.cs` lines 106-112: `QualifierChainProofRequirement(ProofSubject LeftSubject, QualifierAxis LeftAxis, ProofSubject RightSubject, QualifierAxis RightAxis, string Description)`.







- Added `test/Precept.Tests/ProofEngineTemporalChainTests.cs`: `Prove()` at lines 12-16, `AssertSingleChainObligation()` at lines 19-25, and 12 scenario tests at lines 30-169 covering proved matches, mismatches, bare-operand obligations, and regression anchors.







- Validation: targeted class passed all 12 tests; full `dotnet test test/Precept.Tests/` remains at 26 pre-existing spike-branch failures, unchanged.







---







### 2026-05-12T01:05:25Z — Inventory coverage audit tightened remaining interpolation follow-up







- `inventory-item.precept` still exposes a compound-unit interpolation gap, so the checker follow-up needs `unitofmeasure` U2 plus quantity compound patterns for forms like `'{StockingUnit}/{PurchaseUnit}'` and `'0 {StockingUnit}/{PurchaseUnit}'`, with dimensional validation of the resulting compound unit.







- No new proof-engine bug was confirmed: BUG-B remains covered through interpolation plus Slice 9 fallback, while BUG-A now looks like an explicit regression-gap risk once event args parse cleanly.







---







### 2026-05-11 — Slice 11B: Temporal Price Denominator Type System Extension







- Added `ImpliedQualifiers` to `TypeMeta` record; Duration entry carries `TemporalDimension(Time, Baseline)`.







- Extended `ExtractQualifiers` in TypeChecker: `price of 'time'`/`'date'` routes to `MapTemporalDimensionQualifier`; `quantity of 'time'` still emits `InvalidDimensionString` (type-gated guard).







- Added `TemporalUnit` and `TemporalDimension` arms to `ExtractComparableValue`; `PeriodDimension.Any` → null (locked).







- Extended `ResolveQualifierOnAxis` with `Dimension → TemporalDimension` fallback and implied-qualifier loop.







- Added `ExtractComparableValueForTest` and `GetImpliedQualifierOnAxis` internal test entry points.







- MCP DTO: added `string[]? ImpliedQualifiers` to `TypeCatalogEntryDto`; rendered as `"Axis:Value"` strings.







- 13 new tests, all pass. 26 pre-existing spike branch failures unchanged.







- Slice 12 (PriceTimesPeriod/PriceTimesDuration chain requirements) is now unblocked.







---







### 2026-05-11T22:41:00Z — Slice 1 Parser for InterpolatedTypedConstantExpression complete







- Rewrote `ParseInterpolatedTypedConstant()` to produce `InterpolatedTypedConstantExpression` with full segment AST (mirrors `InterpolatedStringExpression`).







- Added `ExpressionFormKind.InterpolatedTypedConstant = 15` with catalog metadata; moved `TypedConstantStart` from Literal LeadTokens to the new form.







- Added NameBinder arms for both `CollectFieldDependencies` and `WalkExpression`.







- TC stub (`ResolveInterpolatedTypedConstantExpressionStub`) routes the new node to crash-prevention diagnostics; the old `ResolveInterpolatedTypedConstantStub` for `LiteralExpression(TypedConstantStart)` is now dead code.







- 10 parser round-trip tests added covering 1/2/3-hole patterns, expressions in holes, and form kind verification.







- Segment shape: always starts and ends with `TextSegment` (may be empty); for N holes → 2N+1 segments.







---







### 2026-05-11T20:03:33Z — ConflictingModifiers implementation recorded







- George's canonical closeout is `DiagnosticCode.ConflictingModifiers = 120`, dedicated validator routing, and symmetric `MutuallyExclusiveWith` declarations on `Optional` and `Notempty`.







- The PRECEPT0011c symmetry requirement is now a durable implementation note for any future mutual-exclusion work.







---







### 2026-05-11T20:03:33Z — Interpolation LOE warning retained







- The interpolation plan is still feasible, but Slice 2 owns most of the complexity and the binder / language-server walker follow-through must be treated as in-scope work, not cleanup.







---







### 2026-05-11T22:41:00Z — Slices 10+11: Assignment qualifier propagation







- Slice 11 (G9): Added `FromCurrency` and `ToCurrency` cases to `ValidateAssignmentQualifiers` switch. Exchange rate field-to-field assignment now validates from/to currency match.







- Slice 10 (G7): Added binary/unary expression handling to `ValidateAssignmentQualifiers` via recursive leaf operand extraction (`ExtractLeafOperands`). `set usdField = eurField + eurField` now correctly produces `QualifierMismatch`.







- Architecture decision: leaf-extraction approach over proof obligations — keeps consistency with existing direct-diagnostic pattern in `ValidateAssignmentQualifiers`.







- Known limitation: bare-operand-to-qualified-target (`set usdField = bareField + bareField`) deferred to proof engine scope.







---







### 2025-07-11 — Part B Slices 7+8+9: Proof engine qualifier coverage







- Slice 7: Added QualifierCompatibilityProofRequirement on QualifierAxis.Currency to all 8 money operations (2 arithmetic + 6 comparison). Closes critical gap where `money in 'USD' + money in 'EUR'` had no proof enforcement.







- Slice 8: Introduced QualifierChainProofRequirement DU subtype with dual-subject, dual-axis design for cross-type qualifier chains. Added to ExchangeRateTimesMoney (FromCurrency↔Currency) and PriceTimesQuantity (Dimension↔Dimension). Extended ProofEngine Strategy 5 with chain comparison via ExtractComparableValue.







- Slice 9: Added Unit→Dimension fallback in ResolveQualifierOnAxis so dimension-only fields satisfy Unit-axis proofs.







- Updated MCP tools (LanguageTool, ProofsTool) for chain rendering and dual-subject classification.







- Updated ProofRequirementCatalogTests for 6th kind. 19 new ProofEngine tests, all 193 pass.







---







### 2026-05-11 — Slice 12: Temporal Chain Validation — BLOCKED







- Investigated G8 (PriceTimesPeriod) and G13 (PriceTimesDuration) for QualifierChainProofRequirement additions.







- Finding: price type uses `QS_CurrencyAndDimension` with `of` → `QualifierAxis.Dimension` (physical). No temporal qualifier axis exists on price. No `per` preposition exists in the token catalog. Duration is unqualified.







- Adding chain requirements would break existing valid `price × period` arithmetic (ResolveQualifierOnAxis returns null → proof always fails).







- `ExtractComparableValue` also lacks TemporalDimension/TemporalUnit arms.







- Deferred: requires price type extension with temporal denominator support before catalog entries can be added.







- Decision filed: `.squad/decisions/inbox/george-slice12-blocked.md`.







---







### 2026-05-11T22:41:49Z — Slice 6: ProofEngine compositional constraint propagation (S6)







- Added ProofStrategy.CompositionalConstraint = 6 and TryCompositionalConstraintProof strategy.







- Strategy discharges numeric obligations on fields whose ALL assignments are TypedInterpolatedTypedConstant nodes where magnitude/whole-value slot source carries a satisfying modifier (nonzero, positive, etc.).







- Conservative intersection semantics: ALL assignment paths must satisfy; any non-interpolated assignment causes decline.







- Helpers: FindInterpolatedAssignments (scans transition rows + event handlers), GetMagnitudeSlotSource (magnitude → whole-value fallback), ResolveSourceModifiers (field + arg ref resolution).







- Reuses existing SatisfactionCovers() for subsumption — no new proof logic.







- 10 new tests, all 193 ProofEngine tests pass.







---







### 2025-07-24 — Slice 2: Full type-grammar matching for interpolated typed constants







- Replaced both interpolated typed constant stubs with complete ResolveInterpolatedTypedConstant() implementation.







- Redesigned form matching from element-per-element to segment-aware model using SegmentForm with TextMatch delegates, correctly handling parser's 2N+1 segment structure.







- Per-type form tables: Money (4), Quantity (4), Price (8), ExchangeRate (8), Currency/UoM/Dimension (1 each), Duration/Period (4 single + compound).







- Added 4 diagnostics: InvalidInterpolatedTypedConstantForm (121), InterpolationNotSupportedForType (122), InterpolatedTypedConstantHoleTypeMismatch (123), DimensionMismatchInUnitSlot (124).







- Added TypedInterpolatedTypedConstant, TypedInterpolationSlot, InterpolationSlotKind to SemanticIndex.







- 39 new tests, 4 existing tests updated. All 129 typed constant tests pass.







---







### 2026-05-11T22:41:49Z — Squad batch closeout







- Slice 1 parser work landed with `InterpolatedTypedConstantExpression` and 10 parser tests.







- Part B slices 7+8+9 landed proof-engine qualifier enforcement, chain requirements, and dimension fallback.







- Slices 10+11 landed assignment qualifier propagation plus `FromCurrency`/`ToCurrency` handling.







- Slice 2 and Slice 12 remain in progress under the current batch.



## 2026-05-12T03:35:43Z summarization from history.md



### 2026-05-11T22:05:37.512-04:00 — RC-2 compound-unit interpolation patterns completed















- `TypeChecker.Expressions.cs` already had `UnitOfMeasureForms` U2 (`'{A}/{B}'`) and `PriceForms` P5 (`'0 {Currency}/{Unit}'`), so the regression was isolated to `QuantityForms` missing the numeric-prefixed compound-unit variants.















- Added quantity forms for `'0 {A}/{B}'`, `'0 {A}/each'`, and `'0 each/{B}'`, plus a `MatchNumericSpaceUnitSlash()` matcher for the fixed-numerator/denominator-hole shape.















- Added regression tests for all three quantity forms and a price guardrail test for `'0 {Currency}/{Unit}'`; targeted typed-constant tests now pass, and the full test project still only shows the pre-existing 26 spike-branch failures.















- Surprise: A2B had already landed the plain `unitofmeasure` compound form and the price grammar needed by `AverageCost`/`ListPrice`; only the quantity table lagged behind.



### 2026-05-11T22:05:37.512-04:00 — RC-1 interpolated qualifier parsing completed















- `TryParseQualifiers()` in `src/Precept/Pipeline/Parser.cs` was still hard-gated to `TokenKind.TypedConstant`; qualifier sites never entered `ParseInterpolatedTypedConstant()`, so interpolated `in '...'` / `of '...'` values fell straight into PRE0009.















- I split `ParsedQualifier` into literal vs interpolated forms in `src/Precept/Pipeline/ParsedTypeReference.cs`, then taught `TryParseQualifiers()` to accept `TypedConstantStart` and preserve the parsed `InterpolatedTypedConstantExpression` instead of dropping the site on the floor.















- `ExtractQualifiers()` in `src/Precept/Pipeline/TypeChecker.cs` now resolves interpolated qualifier forms against the expected qualifier type (`currency`, `unitofmeasure`, `dimension`) before threading placeholder qualifier metadata downstream, so field and arg declarations keep their qualifier slots instead of silently losing them.















- Follow-on surprise: turning qualifier interpolation on surfaced two tightly coupled gaps already sitting next to Slice 2 — binary-op context propagation for interpolated typed constants and the one-hole `unitofmeasure` compound forms (`'{A}/each'`, `'each/{B}'`). I patched both in `TypeChecker.Expressions.cs` so the new qualifier-path tests stayed green and the suite returned to the known 26-failure spike baseline.















- MCP compile validation in this session stayed attached to stale/disconnected server state after the first check, so I verified the sample with the rebuilt public `Precept.Compiler` API as a fallback: `samples/inventory-item.precept` no longer reports PRE0009 on the qualifier declaration lines (71-104 window / actual declarations 80-109 and arg sites 166-207).



### 2026-05-12T02:12:11Z — RC-1 and RC-2 inventory blockers closed















- RC-1 is now durably closed: qualifier positions accept interpolated typed constants, ParsedQualifier preserves literal vs interpolated forms, and inventory-style field/event-arg qualifiers no longer die at PRE0009.















- RC-2 is now durably closed: QuantityForms covers Q6/Q7/Q8 (`0 {A}/{B}`, `0 {A}/each`, `0 each/{B}`), while the already-landed price and unit-of-measure compound forms remain the supporting guardrails.















- Validation state recorded from the execution batch: core build clean, typed-constant battery at 102/102, and the broader spike branch unchanged at the known 26 pre-existing failures.



### 2026-05-11T22:34:01Z — D1/D2 AlwaysRejecting and StateAlwaysRejects diagnostics (codes 125, 126)







- Adding a span to a positional record (TypedTransitionRow) requires inserting RowSpan before Syntax (so Syntax stays last, matching the established NameSpan, Syntax positional pattern on TypedState/TypedEvent/TypedField). Only one construction site existed (TypeChecker.NormalizeTransitionRow) so the migration was surgical.







- PRECEPT0024 is why GraphAnalyzer cannot call .Syntax on any Typed* record. The pattern is always: extract span in TypeChecker at construction time, carry it as a named property, use it in GraphAnalyzer via that property. The existing CollectEdgeSpans comment documents this explicitly.







- D1 suppresses D2 by returning the flagged event name set from EmitAlwaysRejecting. Clean output parameter rather than threading mutable shared state through Analyze.







- The wildcard-override logic in EmitStateAlwaysRejects mirrors BuildEdges exactly: build xplicitStateEvents from rows where FromState is not null && StatesByName.ContainsKey && EventsByName.ContainsKey, then for each (state, event) pair prefer explicit rows if any exist, else fall back to wildcard rows.







- TransitionRowOutcome (semantic enum in SemanticIndex.cs) and OutcomeKind (catalog enum in Outcomes.cs) are parallel enums with the same three values. TransitionRowOutcome.Reject is correct inside graph analysis — no new catalog entry needed.







- The 26 pre-existing spike-branch failures (TypeCheckerAssemblyTests + TypeCheckerAssignmentQualifierTests) are invariant across this work. 9 new GraphAnalyzerTests added, all green.



### 2026-05-11T23:29:22.2031046-04:00 — C4 TypedArgRef qualifier resolution







- `ProofEngine.ResolveQualifierOnAxis()` can resolve direct `TypedArgRef` qualifiers without going through `semantics.FieldsByName`; the direct arg path needs the same Unit→Dimension and Dimension→TemporalDimension fallback rules as the field path.



- `GetFieldName()` also has to recognize `TypedArgRef` so PRE0114 messages stop collapsing direct arg diagnostics to `<unknown>`.



- On `samples/inventory-item.precept`, the C4 fix dropped PRE0114 from 73 to 66. The remaining `<unknown>` PRE0114s are attached to composite operand subtrees, not direct arg refs, so they belong to follow-up proof-expression work rather than this narrow arg-resolution gap.

---

## Archive Batch — 2026-05-15T14:55:25Z

---

## Recent Updates
### 2026-05-14T17:10:32.283-04:00 — Exhaustive interpolated typed-constant audit widened the normalization track

- Audited `TypedInterpolatedTypedConstant` coverage against quantity-normalization Slices 19-21 and confirmed those slices fix the motivating bug but are **not** exhaustive.
- Confirmed five gap categories: bounded interpolated `set` false positives, qualifier-proof false positives, silent qualifier-mismatch acceptance in assignment/defaults, missing interpolated field-default proofing, and completely unwired event-arg defaults.
- Durable severity callout: three false-positive classes and one silent-wrong acceptance are already real; only fully dynamic qualifier text (`'{n} {u}'`-style forms) should remain conservative/unproved by design.
- Recommended the next planning set as slices 22-26: capture static interpolated qualifier metadata, route it through qualifier consumers, extend interval extraction beyond quantity, add field-default proof coverage, and decide event-arg default resolution.

### 2026-05-14T22:00:00Z — PRE0027 diagnosis: Test.precept revert recommended

- Frank investigated suspected PRE0027 (`DuplicateArgName`) errors. Result: **none exist anywhere** in the repository.
- The only error in `samples/Test.precept` is **PRE0078** (pre-existing), present before George's edit. George's change (`'6 [lb_av]'` → `'{test2} [lb_av]'`) changed proof shape but not error category.
- **Recommendation from Frank:** revert `samples/Test.precept` via `git checkout samples/Test.precept`. If interpolated-quantity test coverage is needed for normalization work, create a new sample file with satisfiable bounds.
- `test/Precept.Analyzers.Tests/AnalyzerTestHelper.cs` addition (`AnalyzeWithFilePathsAsync<TAnalyzer>()`) is clean, legitimate C# test infrastructure.

### 2026-05-14T22:00:00Z — Frank's conditions resolution + Slice 15b confirmed: event-arg bound normalization approved

- Frank resolved all six §5.5.6 conditions — the implementation gate for Slices 14–21 is cleared (pending Shane's sign-off).
- Key outcome for George: **Slice 15b** adds `NormalizedDeclaredMin/Max` to `TypedEventArg` (Option a). This is now a design-locked requirement, architecturally parallel to `TypedField`.
- Slices 22–26 have full §5.6 detail entries; George can reference those for implementation planning.



- Frank independently approved the normalization design with conditions and identified the same high-risk areas George's audit hit from the code side: `IntervalOf` scoping, normalized-field bound reads, normalized `StaticMagnitude`, and missing event-arg bound parity.
- Treat the combined George + Frank result as the current architectural baseline before any implementation slices are started.

### 2026-05-15T00:08:25Z — Slice P1 landed typed-constant hole presence-proof traversal

- `ProofEngine.WalkExpression` now traverses `TypedInterpolatedTypedConstant` slot expressions and can emit presence obligations for optional arg refs only in that hole context.
- Focused presence tests passed, and `samples/Test.precept` now reports PRE0116 on line 14 instead of compiling cleanly through the gap.
- Landed as commit `ae19510f`, aligned with Frank's architectural decision that this is a separate presence-proof repair rather than part of quantity-normalization Slices 14–21.

## Learnings

### 2026-05-15T10:55:25.692-04:00 — B3 fix: count-dimension fields need the same bare-numeric PRE0018 suppression lane as unit-qualified quantities

- `AllowsBareNumericQuantityBound` cannot gate only on `DeclaredQualifierMeta.Unit`; `quantity of 'count'` fields carry only a `Dimension` qualifier and rely on the separate PRE0138 path to explain the ambiguity.
- The safe fix is to reuse the existing `IsCountDimension(string)` helper from `TypeChecker.Validation.Modifiers.cs` via the declared dimension qualifiers, so the checker suppresses PRE0018 without forking duplicate count-dimension detection logic.
- Regression proof: all 14 `TypeCheckerQualifierCompatibilityTests` pass after the change, including the B2 negatives (`quantity of 'mass' max 5` and `quantity max 5`) and the Slice M count-dimension case (`quantity of 'count' max 4`).

### 2026-05-14T23:43:11.224-04:00 — First-wave quantity-normalization slices landed with parser/runtime edge constraints

- Slice 43 rename was broader than the initial file list: language-server source (`TypedConstantCollector`, `RichHoverFactory`, handlers) also referenced the semantic node type and had to be renamed with the runtime/test surfaces to keep solution compile integrity.
- `UcumExactFactor` has no decimal conversion helper; normalization code must derive decimal factors from `Numerator`, `Denominator`, and `Base10Exponent` explicitly.
- UCUM affine units require preserving function-wrapper identity (`Cel`, `degF`, `degRe`) while still stripping wrappers for scale evaluation; offset assignment cannot depend on stripped expressions alone.
- `min`/`max` parse as constraint keywords in this branch’s grammar context, so function-call qualifier enforcement regression coverage used `clamp` + `abs` to exercise `QualifierMatch.Same` while keeping the same enforcement path in `SelectOverload`.

### 2026-05-14T23:17:29Z — Slices 14–27 full codebase audit: all NOT_STARTED

Full audit against `src/Precept/` and `test/Precept.Tests/` confirmed **zero slices implemented**:

- **Slice 14:** `TypedConstantNormalizer.cs` does not exist; the `Language/Numeric/` subdirectory does not exist.
- **Slice 15:** `TypedField` has no `NormalizedDeclaredMin/Max`. `TryGetComparableTypedConstantValue` strips raw magnitude without UCUM scaling.
- **Slice 15b:** `TypedArg` has no `NormalizedDeclaredMin/Max`. `ExtractArgInterval` reads `arg.DeclaredMin` directly.
- **Slice 16:** `TryGetTypedConstantMagnitude` returns raw tuple item1. `TryGetStaticScalingFactor` does not exist. `GetFieldBounds` and `TryGetStaticNumericValue` use raw declared/static values.
- **Slice 17:** No cross-unit normalization overflow tests. Only `lb_av` hit is `BoundsQualifierMismatch` rejection test.
- **Slice 18:** `IntervalContainmentProofRequirement` has no `DeclaredQualifier` field.
- **Slices 19–21:** `IntervalOfNarrowed` has no `TypedInterpolatedTypedConstant` case. `NumericInterval` has no `Scale(decimal)`.
- **Slice 22:** `TypedInterpolatedTypedConstant` has `StaticMagnitude` but no `StaticQualifier`.
- **Slice 23:** `ResolveQualifierFromInterpolatedConstant` exists but reads slots, not `StaticQualifier`.
- **Slices 24–25:** No interpolated constant interval or fold coverage.
- **Slice 26:** `TypedArg.DefaultExpression` hardcoded null; no `ResolveEventArgExpressions`.
- **Slice 27:** No doc sync in `precept-language-spec.md` or `proof-engine.md`.

Key fact: Several methods that the slices will modify already exist as pre-existing baselines (`TryGetComparableTypedConstantValue`, `GetFieldBounds`, `ExtractArgInterval`, `TryGetTypedConstantMagnitude`, `ResolveQualifierFromInterpolatedConstant`) — none carry normalization logic yet. Slice 14 is the hard prerequisite for all others.

- Typed interpolated typed-constant holes were bypassing presence-proof generation entirely; the fix is to recurse `TypedInterpolatedTypedConstant.Slots` through `WalkExpression` so optional field reads inside holes emit PRE0116 unless a guard proves presence. Verified with new proof-engine tests and with `samples/Test.precept`, which now reports `UnprovedPresenceRequirement` on line 14.
- Quantity-normalization review: the compile-time core is implementable, but the design still has implementation traps Shane should gate on — duplicate Slice 22 numbering, runtime Slice 22 depending on nonexistent `TypeRuntimeMeta`/`TypeRuntime<T>` surfaces, display drift once computed intervals become normalized, and `TryGetStaticNumericValue` becoming unsound if dynamic-unit interpolated constants fall back to raw `StaticMagnitude`. Also: the actual arg semantic type is `TypedArg`, not `TypedEventArg`, so slice specs must target the real code surface.

### 2026-05-15T02:37:53Z — Function-call qualifier enforcement gap added to the counting-unit fix track

- Frank's comprehensive cross-counting-unit audit found the remaining critical checker hole is not another binary-op branch; it is function-call resolution.
- `TypeChecker.Expressions.Callables.cs` resolves `min`/`max`/`clamp`/`abs` overloads without ever enforcing `FunctionOverload.Match`, so `QualifierMatch.Same` metadata is currently dead for function calls.
- Implementation direction is locked in the design doc: add `ValidateFunctionQualifierCompatibility` immediately after `SelectOverload`, reuse PRE0137 for explicit cross-counting-unit mismatches, and leave `in` membership as a separate deferred follow-up.

### 2026-05-14T22:57:25.658-04:00 — §5.7 slice review found stale paths and the wrong membership surface

- Blocked the §5.7 execution plan as written: the repo does **not** have `src/Precept/Catalog/...`, `DiagnosticCatalog.cs`, `FunctionsCatalog.cs`, or `TypeChecker.TryGetStaticScalingFactor()` today. The real current surfaces are `src/Precept/Language/Ucum/UcumAtomCatalog.cs`, `src/Precept/Language/Diagnostics.cs`, and `src/Precept/Language/Functions.cs`; any affine helper still needs to be introduced.
- Confirmed Gap C's real seam is still `TypeChecker.Expressions.Callables.cs` `SelectOverload`, but the qualifier check must guard both `TypedFunctionCall` return paths there (direct winner and context-retry winner).
- Confirmed Gap D is **not** an `in` / `not in` path in the current DSL. Membership is `contains`, and the checker route is `ResolveBinaryOp` → `TryResolveCatalogBinaryWithoutOperation` → `CreateSyntheticBinaryOp`. `OperatorTypingTests.cs` is the current regression anchor for that surface.
- Verified PRE0137 is available: `DiagnosticCode.CountBoundViolation = 136` is the current high watermark, so 137 is the next free ordinal.
- Found missed regression surfaces for slices 30–33: `test/Precept.Tests/ProofEngineTests.cs` PartB Slice7/9 still assume old proof-only behavior, and `test/Precept.Tests/TypeChecker/OperatorTypingTests.cs` already covers `contains` typing.

### 2026-05-14T23:11:17.096-04:00 — §5.7 re-review approved after Frank’s corrections

- Re-reviewed the revised §5.7 slice plan and cleared the original blockers: stale catalog/diagnostic/function references were corrected to the real `src/Precept/Language/...` surfaces, and the membership slice now targets `contains` through `ResolveBinaryOp` → `TryResolveCatalogBinaryWithoutOperation` → `CreateSyntheticBinaryOp`.
- Spot-checks against source confirmed `ValidateQualifierCompatibility`, `ResolveFunctionCall`, `SelectOverload`, `CreatePendingAtom`, `StripFunctionWrapper`, and the current `TypedInterpolatedTypedConstant` semantic node; PRE0137 remains free because `DiagnosticCode.CountBoundViolation = 136` is still the high watermark.

### 2026-05-15T03:13:42Z — George approved the revised §5.7 slice plan

- George’s re-review cleared the earlier §5.7 blockers: the slice list now points at the real `src/Precept/Language/...` seams, covers both successful `SelectOverload` returns, and moves the membership work to Precept’s actual `contains` operator path.
- Scribe merged George’s approval note into `.squad/decisions/decisions.md`, cleared the inbox file, and recorded the approval as the current architectural baseline for slices 30–43.

### 2026-05-15T03:17:29Z — Scribe recorded slice-audit baseline

- Scribe merged George's slice-audit note into `.squad/decisions/decisions.md` and cleared `.squad/decisions/inbox/george-slice-audit.md`.
- Durable baseline: slices 14–27 in `docs/Working/quantity-normalization-design.md` remain **NOT_STARTED** across `src/Precept/` and `test/Precept.Tests/`.
- Scribe wrote the orchestration/session logs for the slice-audit + doc-tracker batch so later agents can treat George's audit as the canonical pre-implementation status check.

### 2026-05-15T03:43:11Z — First-wave slices 43/14/34/22/30/32 landed on the spike branch

- `spike/Precept-V2-Radical` now carries the first implementation wave for quantity normalization: Slices 43, 14, 34, 22, 30, and 32 are committed, with per-slice `dotnet build src/Precept/Precept.csproj` runs clean.
- The shipped surface is broader than the slice labels alone: `InterpolatedTypedConstant` rename propagation reached runtime, tests, and language-server code, while the branch also gained `TypedConstantNormalizer`, `NumericInterval.Scale(decimal)`, UCUM `AffineOffset`, and static interpolated qualifier metadata.
- Comparison operators now share qualifier-enforcement strictness with arithmetic, and `QualifierMatch.Same` is finally enforced in `SelectOverload`; because `min`/`max` parse as constraint keywords on this branch, `clamp` and `abs` are the durable regression anchors for that function-call path.
- PRE0137 is now wired through the function-call same-match path as part of Slice 32.

### 2026-05-15T07:59:53.548-04:00 — Wave 2A slices 15/15b/16/19/20/31/33 implementation learnings

- TryGetComparableTypedConstantValue is the single extraction seam for both field and event-arg min/max modifiers; normalizing there automatically aligns PopulateFields and PopulateEvents without duplicating bound parsing logic.
- IntervalOf normalization must remain expression-type scoped: scaling only raw typed constants and magnitude-slot interpolations prevents double-normalization of TypedFieldRef/TypedArgRef intervals.
- TryGetStaticNumericValue for interpolated constants is only sound when unit scaling is statically known; dynamic unit-slot forms must return no trusted fact instead of falling back to raw StaticMagnitude.
- Routing CreateSyntheticBinaryOp through ValidateQualifierCompatibility cleanly extends PRE0137/PRE0071 checks to contains membership while preserving existing element-type compatibility gates.

### 2026-05-15 — Slice N B1+B2 fix: AllowsBareNumericQuantityBound over-suppression

- `AllowsBareNumericQuantityBound` in TypeChecker.cs was checking only `targetType == TypeKind.Quantity && IsBareNumericLiteral(expression)`. It did not verify a Unit qualifier was present on the field.
- Result: PRE0018 (TypeMismatch) was incorrectly suppressed for `quantity of 'mass' max 5` (dimension-only) and `quantity max 5` (unqualified) fields — not just unit-qualified ones.
- Fix: added `qualifiers.Any(q => q is DeclaredQualifierMeta.Unit)` gate, mirroring the exact condition in `ShouldAllowUnitQualifiedQuantityBareNumericBound` in the Validation.Modifiers partial.
- Lesson: `ValidateBoundQualifierRequirements` is a separate method from `ValidateBoundQualifierCompatibility` and fires PRE0133 for unqualified/dimension-only quantity independently of the AllowsBareNumericQuantityBound gate. Both PRE0018 and PRE0133 fire post-fix for these cases — that is correct behavior and tests should not assert PRE0133 does not fire.
- The `IsBareNumericLiteral` / `IsBareNumericBoundLiteral` duplication (W1) was trivially resolved by removing `IsBareNumericBoundLiteral` from Validation.Modifiers.cs and using `IsBareNumericLiteral` from TypeChecker.cs (same partial class).

### 2026-05-15T10:55:25.692-04:00 — W1/W2 follow-up: raw-vs-normalized bounds and affine price guard

- Current branch HEAD already preserves raw `DeclaredMin/Max` alongside `NormalizedDeclaredMin/Max` for both `TypedField` and `TypedArg`; the follow-up work here was to verify that split and add regression coverage that locks it in for quantity bounds where normalization changes the magnitude.
- The right regression shape for W1 is a unit-qualified quantity bound whose authored value differs from its base-unit normalized value (`'5 g'` vs `0.005` in a `kg` field/arg). That proves future hover/MCP consumers can read authored values while the proof engine still consumes normalized intervals.
- `NormalizePrice` must never silently treat affine-offset denominator units as pure scale factors. Guarding through `TryGetStaticAffineParams` and throwing on non-null offsets prevents theoretical-but-dangerous wrong math if temperature units ever reach price denominators.
