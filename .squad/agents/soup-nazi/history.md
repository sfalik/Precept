## Recent Updates

### 2026-04-24 — Slice 5: OperatorTable + numeric literal + binary expression tests

- Created `test/Precept.Next.Tests/OperatorTableTests.cs` (39 test cases after Theory expansion).
- Added 12 integration tests to `TypeCheckerTests.cs` under a Slice 5 section header. Total: 632 passing (+53 from 579).
- Coverage: same-lane arithmetic (integer/decimal/number), widening commutative pairs, cross-lane null, string concat, ErrorType propagation (left/right/both), non-null guarantee, comparison+logical ops returning null, CommonNumericType identity/widening/incompatibility, number literal null-context dispatch, binary field+field arithmetic resolution, cascade suppression.
- Key constraint: `CheckBinaryExpression` passes null to both operands, so number literals always get null context inside binary expressions. This means "fractional in integer context" and "scientific in decimal context" TypeMismatch paths are untestable at integration level in Slice 5 — these are covered by OperatorTable unit tests instead. TypeCheckerTests exercises the dispatch arm reachability and null-context TypeMismatch ("cannot determine numeric type from context").
- The integration test for widening (`Check_IsAssignableTo_IntegerToDecimal_Widens`) uses field+field binary arithmetic: integer+decimal → OperatorTable→DecimalType → condition is DecimalType → TypeMismatch (decimal not boolean). This is the observable proxy for widening working end-to-end.
- `Resolve_ErrorType_DoesNotReturnNull` is a non-null guarantee test — proves ErrorType propagation never produces null, preventing false TypeMismatch cascade diagnostics.
- `Resolve_LogicalOp_ReturnsNull` (And/Or) is a small addition beyond the task spec — logical ops in Slice 5 must return null consistently with comparison ops.

### 2026-04-24 — G7: Faults catalog tests

- Created `test/Precept.Next.Tests/FaultsTests.cs` with 9 test methods (Theory × 8 FaultCode values + 2 standalone Facts = 42 total test cases after expansion).
- Coverage: GetMeta exhaustiveness, All count, non-empty Code/MessageTemplate, Create factory code+codeName, message formatting, CodeName↔enum identity, StaticallyPreventable attribute presence, linked DiagnosticCode validity, no duplicate DiagnosticCode mappings.
- No surprising findings — all 8 FaultCode values have attributes and valid linkage.

### 2026-04-24 — Precept.Next pre-TypeChecker coverage audit

- Audited all files in `test/Precept.Next.Tests/` against `src/Precept.Next/Pipeline/` and `src/Precept.Next/Runtime/`.
- Test suite currently has exactly 2 files: `LexerTests.cs` (54 tests) and `ParserTests.cs` (~151 test cases after InlineData expansion). Total: ~205 test cases.
- All 3 downstream pipeline stages are 1-line stubs: `TypeChecker.cs`, `GraphAnalyzer.cs`, `ProofEngine.cs` all throw `NotImplementedException`.
- All `Runtime/` classes (`Evaluator`, `Precept`, `Version`) are stubs. Only `Faults.cs` has real logic — no tests exist for it.
- `TypedModel.cs`, `GraphResult.cs`, `ProofModel.cs` are stub records with only `ImmutableArray<Diagnostic> Diagnostics` — none have the rich shape described in the docs.
- Found 2 BLOCKERS before TypeChecker tests can compile: (1) TypedModel shape is a stub; (2) DiagnosticCode.cs is missing all type-checker-specific codes (~20 missing codes listed in the type-checker doc).
- `~string` lexer coverage: 6 tests (tilde standalone, not emitted for ~= or !~, set/queue/stack of ~string). Parser: 6 tests. TypeChecker: 0 (blocked — stub).
- `DiagnosticCode.cs` has only 6 type-stage codes (`UndeclaredField`, `TypeMismatch`, `NullInNonNullableContext`, `InvalidMemberAccess`, `FunctionArityMismatch`, `FunctionArgConstraintViolation`) vs ~26 needed per the type-checker doc.
- Filed decisions inbox: `.squad/decisions/inbox/soup-nazi-precept-next-coverage-audit.md`

## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, and language-server validation.
- Keeps behavioral claims tied to executable proof and records gaps as actionable test findings, not just counts.
- Historical summary (pre-2026-04-13): led broad verification for declaration guards, including parser/type-checker/runtime/LS/MCP coverage and test-matrix planning for guarded editability.

## Learnings

- 2026-04-24 Slice 5 OperatorTable + binary expression tests: wrote 39 OperatorTable unit tests + 12 TypeCheckerTests integration tests (632 total, all pass). Critical pattern: `CheckBinaryExpression` calls `CheckExpression(operand, null)` for both sides — number literals always get null context inside binary expressions, emitting "cannot determine numeric type from context" TypeMismatch. This means fractional-in-integer and scientific-in-decimal TypeMismatch paths are only reachable if something passes a numeric expectedType, which no rule-level surface does in Slice 5. OperatorTable unit tests are the only complete test surface for those code paths. Integration widening tests use field+field binary (not field+literal) since field identifiers resolve to their declared type regardless of context. The observable proxy for widening in integration is: integer+decimal binary → condition is DecimalType → TypeMismatch "decimal not boolean" (condition check). ErrorType propagation must return ErrorType (not null) — added explicit non-null guarantee test to prevent false TypeMismatch cascades.

- 2026-04-24 Slice 4 expression checking + rule declaration tests: wrote 19 test methods (22 total test cases after Theory expansion with 4 inline scalar types). Covered all 9 required cases plus 10 additional. Key patterns: (1) `rule true because "ok"` with double-quoted DSL strings requires `\"` escape inside C# string literals — use `"precept T\nrule true because \"ok\""`. (2) `rule (true) because "ok"` for parenthesized expressions — ParenthesizedExpression recurses to inner and returns same type. (3) `rule because "ok"` (missing condition) triggers `NudMissing` in the parser → parse-stage `ExpectedToken` diagnostic, NOT a type-stage `TypeMismatch` — condition resolves to `ErrorType` and cascade suppression fires. (4) Collection field types (`set of string`) stub to `ErrorType` in Slice 4 (`CollectionTypeRef => new ErrorType()` in `ResolveTypeRef`), so `InvalidInterpolationCoercion` CANNOT be triggered via declared fields — that test must wait for Slice 8 (collection type resolution). Only scalar-type fields have real `ResolvedType` in this slice. (5) Theory for `Check_IdentifierExpression_ResolvesToDeclaredFieldType` covers `string/integer/decimal/number` — all 4 produce TypeMismatch (non-boolean used as condition) but the identifier lookup still resolves the correct type, proving the field lookup path works for all scalar types. (6) `model.Rules[0].Guard.Should().BeNull()` for guardless rules — Guard is `TypedExpression?` on `ResolvedRule`. (7) `model.Rules[0].Guard!.Type.Should().BeOfType<BooleanType>()` requires the null-forgiving `!` since FluentAssertions doesn't propagate the `NotBeNull()` assertion to the type system. All 19 new tests passed immediately; total count went from 276 to 303.



- 2026-04-24 Slice 3 state + event registration tests: wrote 27 test methods covering all 9 required cases plus 18 additional. Patterns: (1) Events with states in the same source must include at least one state declaration to avoid `NoInitialState` diagnostic contaminating `model.Diagnostics.Should().BeEmpty()` assertions — always supply `state Draft initial` when testing events cleanly. (2) State modifier `terminal` is the DSL keyword; `StateModifierKind.Terminal` is the enum value — test via `model.States["X"].Modifiers.Should().Contain(StateModifierKind.Terminal)`. (3) For `MultipleInitialStates`, message args are ordered `{0}=first initial, {1}=second initial`; test `diag.Message.Should().Contain(...)` for both names rather than asserting the full format string. (4) When multiple event names share one declaration (`event Approve, Reject(Reason as string)`), each event independently gets the same args dictionary — a useful multi-event+args coverage case. (5) `DuplicateArgName` first-arg-wins pattern mirrors `DuplicateFieldName` behavior — both tested with separate "first wins" fact. (6) `Register_NoStates_NoInitialStateDiagnostic` uses `NotContain` (not `BeEmpty`) because the stateless precept may have other diagnostics; targeting the specific code avoids false failures. All 27 new tests passed immediately; total test count went from 249 to 276.

- 2026-04-24 Slice 2 field registration tests: wrote 13 test methods covering all 7 required cases plus 6 additional. Patterns: (1) Use `as` keyword instead of `:` for the "no diagnostics" test — `:` is an `InvalidCharacter` to the lexer and produces a parser `ExpectedToken` diagnostic via error recovery, which contaminates `model.Diagnostics`. All other tests can use `:` since TypeChecker behavior is tested correctly through parser error recovery. (2) `typeof(T)` is valid in `[InlineData]` for Theory-per-type tests; FluentAssertions `BeOfType(Type)` overload accepts it. (3) `~string` in non-collection scalar field position produces `ErrorType()` (parser doesn't handle it; `Tilde` is only valid as collection inner-type prefix), so `StringType(CaseInsensitive: true)` for scalar fields is NOT testable at this slice. (4) Duplicate field name → first declaration wins (second triggers `DuplicateFieldName` diagnostic); tested both "only one diagnostic" and "first type is preserved" as separate facts. (5) Missing name token (`field : string`) skips registration cleanly — `model.Fields.Should().BeEmpty()` is the right assertion since the TypeChecker's `token.Length == 0` guard fires.

- 2026-04-24 G7 Faults catalog tests: wrote 9 test methods covering exhaustiveness (GetMeta per code, All count), Create factory (code/codeName/message), CodeName identity (matches enum member name), and FaultCode→DiagnosticCode linkage (attribute presence, valid target, uniqueness). Used reflection for `[StaticallyPreventable]` attribute inspection. All current FaultCode templates are static strings with no format placeholders — format-arg coverage is structural only (string.Format tolerates extra args on placeholder-free templates).
- 2026-04-24 Precept.Next coverage audit: TypedModel stub shape is a compile-time blocker for TypeChecker tests — the record needs real fields before any model assertion can compile. DiagnosticCode.cs missing type-checker codes is the second compile-time blocker. Both must be resolved before TypeCheckerTests.cs can be written.
- `Faults.cs` has real testable code (GetMeta, Create, All) but zero tests. This is a pre-existing gap that can be closed at any time without waiting for downstream stubs.
- `DiagnosticCode.cs` enum + `Diagnostics.GetMeta` switch must stay in sync with the type-checker doc as new codes are added. A drift test (parallel to `CatalogDriftTests` in v1) should be added when TypeChecker is implemented.
- 2026-04-24 v2 design review: block test implementation until the docs reconcile five contract collisions: numeric lanes, function catalog / return typing, typed-constant validity stage, diagnostic identity (named vs `C###`), and collection totality. Tests need one oracle, not three.
- Temporal/business surface drift is now the main risk: the specialized design docs assume first-class accessors, qualifiers, and domain-preserving functions, while the core checker blueprint still treats those areas as deferred or numeric-only.

- 2026-04-23 v2 AST/parser clean-room audit: direct v1 names were scrubbed, but clean-room drift can still arrive through stale surface contracts. Watch the event-arg syntax (`with` vs `(...)`), guarded `write` word order, and newline-boundary rules across docs.
- The current v2 AST cannot represent dotted call-style accessors like `.inZone(tz)` because `CallExpression` only accepts a bare function token and the parser design only starts calls from identifier nud.
- Parser test floor for the current v2 surface: about 30 expression shapes, 13 structural declaration forms, and 20+ recovery scenarios before precedence, chaining, and resync permutations.

- When a test is added to satisfy a "count" AC, always verify it also satisfies any "correctness" sub-clauses (e.g., StateContext, message text). A test that checks `ContainSingle()` for AC #21 does NOT satisfy the "correct state context" part of that AC.
- Prior-round blockers can be marked "fixed" while still having a gap: B2 (AC #21) shipped the right test method but the assertion stopped at count=1, never checking StateContext. The AC said "with correct state context."
- Integration tests (diagnostic samples with EXPECT annotations) can cover message text gaps that unit tests leave open — but unit tests with code-only assertions are a weaker safety net. Both should be present where the design specifies message content.
- When Elaine-B1 (FormatInterval → natural language) ships, divisor-safety.precept:41 EXPECT line will need updating. Track this dependency proactively.
- Compile-time/default-data behavior must be tested explicitly whenever new guard semantics are introduced.
- Guard scope rules need separate coverage for field-scoped and arg-scoped contexts.
- Regression risk is highest where hydration, editability, and inspect/update paths share runtime machinery.
- When slice agents do their job, drift tests arrive pre-populated — audit confirms rather than creates. Slice 5 agent added C92/C93 drift entries correctly.
- Event arg constraint keyword → C93 suppression must be tested separately from event arg ensure → C93 suppression. The mechanism overlaps but the AC names them as distinct.
- `from any` expansion tests must cover each proof-scoped diagnostic independently. A null-narrowing `from any` test does NOT satisfy the divisor `from any` AC.
- For structural refactors, regression anchors must map to the moved method clusters' real owning test files, not just the broad umbrella suite. Helper extraction especially needs explicit canaries for row expansion, symbol-table construction, accessor resolution, and proof-context lookup.
- Theory-based tests with `messageFragment` inline data are the strongest pattern for context-aware diagnostic messages — each row self-documents what the message should say.
- Principle #8 stance from the testing seat: compile-clean should not imply safety the checker did not actually prove. Runtime failure tests are a backstop, not the guarantee, but tighter philosophy must preserve already-proven compound patterns rather than flattening the language into trivial-only proofs.

- Compound "assume satisfiable" heuristic has a latent soundness gap: `D - D` is always zero but no C93 fires. Any future compound analysis must address this test (`Check_DivisorCompound_Subtraction_NoWarning`).
- The sample corpus has ZERO non-literal field-based divisors that lack proof — every division by a field or event arg is already constrained. Future proof techniques have no corpus gap to close for existing samples; their value is in enabling NEW patterns (inline if/then/else division, function-wrapped divisors, relational cross-field proofs).
- if/then/else in Precept is ternary expression only — narrowing applies to typing within branches, not control-flow reachability. This simplifies branch analysis compared to general PL.
- Function proofs (abs, max, min, clamp, round, sqrt) have the highest ratio of new provable patterns to implementation complexity. `max(D, 1)` as safe divisor is the single most impactful pattern.
- Interval arithmetic's biggest win in the current corpus is computed field constraint verification (e.g., proving `LineTotal nonnegative` from upstream field ranges).
- A large proof-test count can still hide guarantee holes. For PR #108, the critical misses are concentrated at the design-expansion edges: truth-based `C92` vs unresolved `C93`, the entire `C94`-`C98` family, the 64-fact / 256-node graph caps, and proof surfacing in hover/MCP.

## Recent Updates

### 2026-04-17 — Proof Technique Test Inventory (pre-implementation research)
- Read all 25 sample files, cataloged every arithmetic expression: 5 divisions, 2 modulos, 22+ additions/subtractions, 11+ multiplications, 8 function calls, 5 if/then/else ternaries, 12+ relational guard expressions.
- All sample divisions are already safe: either literal divisors or event-arg-proven. No new technique closes a gap in the existing corpus.
- Identified `Check_DivisorCompound_Subtraction_NoWarning` (`D - D`) as a latent soundness gap that any compound analysis would expose.
- Produced 40+ concrete test cases across 5 techniques with positive/negative/edge categories in actual Precept syntax.
- Output: `temp/soup-nazi-proof-test-inventory.md`


## Recent Updates

### 2026-04-17 — PR #108 Test Review (Issue #106 divisor safety)
- Reviewed full PR: 34 behavioral ACs mapped to tests. 32/34 covered, 2 blockers.
- B1: No test for event arg `positive` constraint keyword suppressing C93 (AC #12). The sqrt variant is tested but not divisor.
- B2: No test for `from any` expansion with per-state divisor proof (AC #21). Existing `from any` test is null-narrowing only.
- Warnings: guarded state ensure exclusion from divisor proof covered by mechanism but no explicit test; generic C93 message text not asserted.
- Strengths: Theory-based proof source × operator matrix, 7-variant or-pattern suite, zero disabled tests, CatalogDriftTests fully populated, code action tests go beyond core AC.
- Total: ~51 new test methods (47 Precept.Tests + 4 LS.Tests). 1463 total tests, 0 failures.
### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### 2026-04-10 - Issue #31 Slice 7: keyword logical operator tests
- Updated 9 existing test files (8 in `test/Precept.Tests/`, 1 in `test/Precept.LanguageServer.Tests/`) to replace DSL symbols `&&` → `and`, `||` → `or`, `!` → `not` in all `.precept` string literals and operator assertions.
- Created new `test/Precept.Tests/PreceptKeywordLogicalOperatorTests.cs` covering: basic keyword parsing (not/and/or), precedence validation (not > and > or), null narrowing through `not (Field == null)`, `!=` operator unaffected, old symbols `&&`/`||`/`!` produce parse errors, compound expression parse/evaluate, invariant context (or/and).
- George's runtime changes (parser, tokenizer, type checker, evaluator, samples) were already on the branch — tests written against the finished spec.
- Key learning: always check if the partner's runtime changes are already on branch before assuming tests will fail. The old-symbol-produces-error tests and keyword tests may pass immediately if George's work is complete.
### 2026-04-15 — Issue #100 Testing Gate: Precept Name Token Scope
- **BLOCKED** PR #101. The TextMate grammar fix is correct (`entity.name.precept.message.precept` → `entity.name.type.precept.precept`), but the semantic token handler (`PreceptSemanticTokensHandler.cs` line 331) still maps `PreceptToken.Precept => "preceptMessage"`. Because `editor.semanticHighlighting.enabled = true` is set for precept files, the semantic path overrides TextMate and the precept name still renders gold.
- Identified that `GetClassifiedTokens_PreceptName_IsPreceptMessage` (line 125, `PreceptSemanticTokensClassificationTests.cs`) asserts the broken behavior and must be updated as part of the fix.
- Required: semantic handler change + legend registration (if new type) + package.json semantic color rule + test update.

### 2026-04-11 — Guarded declaration validation sweep
- Built and verified multi-layer tests for guarded invariants, state asserts, event asserts, and guarded edit blocks, including runtime and MCP coverage.

### 2026-04-17 — Unified proof plan full test review (pre-implementation)
- Reviewed `temp/unified-proof-plan.md` §4-§8a against existing test codebase. Plan has ~166 new tests across 9 files.
- Coverage matrix (§8): All 20 input patterns mapped to planned test files. Every "✅ proves" and "💀 correctly rejected" has at least one test. No coverage gaps in the matrix proper.
- §8a unsupported patterns: Found that 4 of 17 rows need explicit "correctly rejected" tests that are NOT listed in any planned test file: row 1 (non-linear `A*B-C`), row 4 (function opacity `abs(X)-B`), row 14 (inequality-without-ordering `A!=B`), row 16 (modulo `A%B`). Filed as NON-BLOCKING finding — these are easy to add.
- Edge cases missing per file: LinearFormTests needs constant-only normalization, single-term form, `long.MaxValue` GCD stress, and construction-order equality. RationalTests needs `long.MinValue` negation overflow, multiplication overflow, division by zero. ProofContextTests needs deeply nested `Child()`, unknown field `WithAssignment`, opaque expression `IntervalOf`. TransitiveClosureTests needs disconnected graph, self-loop, mixed-scope chains.
- Regression risk: Highest risk files are PreceptTypeCheckerTests.cs (C-Nano section specifically), ConditionalExpressionTests.cs, CatalogDriftTests.cs (C92/C93 entries), DiagnosticSpanPrecisionTests.cs (C93 column tests). The ProofContext signature refactor (commit 2) touches every narrowing method — mechanical but high blast radius.
- Soundness invariant tests: -3..+3 range is acceptable first pass but narrow. Missing: decimal values (0.001, 0.1), larger magnitudes (100, 1000), explicit saturation tests.
- Workaround flagging feasibility: Assessed all 5 unsupported categories. Detection is feasible for all — `LinearForm.TryNormalize` failure reason + expression shape inspection + rule set scanning. Suggestion generation requires ~100-200 new LOC. Recommendation: follow-up PR, not this one — it improves diagnostic quality but doesn't change proof power.
- Verdict: APPROVED-WITH-CAVEATS (1 blocker, 6 non-blocking findings).

### 2026-04-17 — Slice 8: C92/C93 catalog drift + sample audit (#106)
- Verified C92 (literal zero divisor) and C93 (unproven divisor) drift test entries already present and correct in both `ConstraintTriggers` and `LineAccuracyData`.
- Audited 5 sample files: loan-application, invoice-line-item, insurance-claim, travel-reimbursement, clinic-appointment-scheduling — all compile clean, zero C92/C93 diagnostics.
- Critical validation: `travel-reimbursement.precept` with `Submit.Lodging / Submit.Days` (non-literal divisor) produces no C93 warning, confirming `BuildEventEnsureNarrowings` (Slice 4) is working correctly.
- No code changes needed. All 1290 tests pass.

### 2026-04-19 — Issue #118 regression gate audit (PR #123 structural refactor)
- Audited issue #118 acceptance criteria and PR #123 regression anchors against the actual test surface of `PreceptTypeChecker`.
- Verdict: MANAGEABLE.
- Slice 1 anchors are directionally right, but the exact watch methods that matter most are `Check_TypeContext_CapturesScopedSymbolsForGuardedTransition`, `Check_FromAny_UsesPerStateExpansionAndStateEnsureNarrowing`, `Check_DivisorFromAny_PartialStateEnsure_C93WithContext`, `Check_DottedEventArgNullNarrowing_NarrowsSuccessfully`, `Check_BareEventArgInTransitionGuard_ProducesC38`, `Check_StateEnsure_AppliesInCorrectState_C93InOtherState`, and `Regression_CollectionCount_ParsesAndCompiles_Unaffected`.
- Slice 2 anchors in the PR body are incomplete. `ValidateChoiceField(...)` coverage lives in `PreceptChoiceTypeTests.cs` (`C62/C63/C64/C66`), and `C61` maxplaces coverage lives in `PreceptDecimalTypeTests.cs` and `PreceptIntegerTypeTests.cs`, not just `FieldConstraintTests.cs`.
- Slices 3-5 are directionally sufficient if each one runs `dotnet build` plus the targeted anchor set from the PR body before moving on.
- Filed inbox note: `.squad/decisions/inbox/soup-nazi-issue-118-regression-gate.md`.
- Recommended targeted commands before full suite:
	- Slice 1: `dotnet test test/Precept.Tests/ --filter "FullyQualifiedName~Precept.Tests.PreceptTypeCheckerTests|FullyQualifiedName~Precept.Tests.ProofContextScopeTests|FullyQualifiedName~Precept.Tests.StringAccessorTests"`
	- Slice 2: `dotnet test test/Precept.Tests/ --filter "FullyQualifiedName~Precept.Tests.FieldConstraintTests|FullyQualifiedName~Precept.Tests.PreceptChoiceTypeTests|FullyQualifiedName~Precept.Tests.PreceptDecimalTypeTests|FullyQualifiedName~Precept.Tests.PreceptIntegerTypeTests"`
- Full gate: `dotnet build` per slice, then `dotnet test` before PR-ready.
- Verified current branch health for the proposed gate: Slice 1 candidate set 190 passed; Slice 2 candidate set 137 passed.
- Current build still emits 2 warnings in `PreceptTypeChecker.cs`, so the bar stays “no new warnings attributable to the refactor,” not merely “build succeeds.”

### 2026-04-19 — Issue #118 diagnostic drift triage (PR #123)
- Re-ran `DiagnosticSampleDriftTests` locally on current `feature/issue-118`: targeted drift theory passed, and a fresh full-suite run passed `2073/2073`.
- Verified the two reported sample failures are not baseline by running the same drift theory on pre-refactor commit `b686893`; it passed there too.
- Walked the refactor commits `032b897`, `89bf5bb`, `4dad80b`, `eb88c4e`, `ad3f65d`, and `49e9090`; the same drift theory passed on every retained slice commit.
- Read the current branch diff from `b686893..a83956d`: no sample or drift-harness changes, only `PreceptTypeChecker` partial extraction files plus main-file edits.
- Key lesson: when a handoff claims named failing samples but no assertion payload survives, do not classify from squad notes alone. Re-run the exact theory on the pre-refactor base, each retained slice commit, and current `HEAD` before calling something baseline or regression-attributable.

### 2026-04-23 — v2 lexer audit and test-plan gate
- Audited the v2 lexer against the synced lexer spec, literal-system design, diagnostic catalog, token contracts, and representative samples.
- Verdict: BLOCKED for broad suite rollout until the team locks two contract edges: `InputTooLarge` token output and lone-backslash recovery at EOL / EOF inside quoted literals.
- Recommended first slice: quoted-literal token + recovery tests, including blocker-confirming cases before wider parser-facing goldens.
