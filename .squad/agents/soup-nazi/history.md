## Core Context

- Owns test discipline, validation strategy, and compile/runtime verification for the DSL and surrounding tools.
- Samples and hero candidates should be validated against the real compiler/runtime before the team treats them as canonical.
- Coverage work should record meaningful gaps, not just raw counts; behavior claims need executable proof.

## Recent Updates

### 2026-04-11 — Issue #29 (Slice 7): integer type test suite written
- Created `test/Precept.Tests/PreceptIntegerTypeTests.cs` — 34 tests: 31 passing, 3 skipped.
- **Test categories:** parser (8: field declarations, long literal semantics, double vs long distinction, nullable, large literal, set/queue/stack of integer), type-checker coercion (8: C60 narrowing, C61 maxplaces, widening to number, no-error cases, Theory x3), runtime arithmetic (8: +/-/\*//, negative division truncation, %, ==, widened to number), runtime event args (2: long, coerce int→long), runtime guards (1: comparison routing), collections (1: set<integer> min/max via guard routing).
- **3 tests skipped with TODO for George:** `nonnegative`, `positive`, and `min/max` constraints on integer fields do not get desugared to runtime invariants. `BuildScalarConstraintExpr` in `PreceptParser.cs` has branches only for `Number` and `String` types — `Integer` falls through to the no-op return. Constraints are parsed and type-checked but never enforced at runtime.
- **Key patterns hardened:**
  - Integer literals in DSL parse as `long`; decimal-point literals parse as `double`. Assert `5L` not `5.0`.
  - For TypeCheck "BeEmpty" assertions: use `-> transition B` (not `-> no transition`) so B is reachable (no C48) and A doesn't become a dead-end (no C50).
  - For TypeCheck "emits C60" assertions: same pattern — `-> transition B` isolates C60 as the only diagnostic, enabling `ContainSingle()`.
  - C61 (maxplaces on integer) must be tested via `DirectAction` model construction — maxplaces is not yet a parseable DSL keyword. The model also emits C53 (no events), so assert `Contain(d => d.Constraint.Id == "C61")` not `ContainSingle()`.
  - Integer divides as C# long truncation toward zero: `-5 / 2 = -2`, not `-3` (floor).
  - `int` (32-bit) passed as event arg is coerced to `long` by `CoerceToInteger`.
- **Gap filed:** `.squad/decisions/inbox/soup-nazi-integer-tests.md`.

### 2026-04-10 — Issue #13: field-level constraints test strategy + scaffold written
- Created `test/Precept.Tests/FieldConstraintTests.cs` — 37 tests: 3 ready (pass now) + 34 stubs (NotImplementedException until George's implementation lands).
- **3 baseline ready tests** confirm the desugar destination works today: invariant-expressed nonnegative/positive/min-max semantics already produce ConstraintFailure at runtime.
- **34 stubs** cover: parser acceptance (12), C57 type mismatch (4), C58 contradictory/duplicate (3), C59 default-violates-constraint (6), nullable semantics (2), runtime desugaring verification (8), event arg constraint outcome verification (1).
- **Design decision flagged:** `nonnegative positive` combined → recommend C58 (subsumed constraint). `positive` strictly subsumes `nonnegative`; the weaker constraint is dead code. Documented in scaffold and decision file. Needs George + Shane sign-off.
- **Key distinction documented:** field constraints desugar to invariants (→ ConstraintFailure); event arg constraints desugar to on-event asserts (→ Rejected). Test `Fire_EventArgNotempty_EmptyString_ProducesRejected` pins this semantics gap.
- **CatalogDriftTests entries noted:** C57, C58, C59 require three new entries in `CatalogDriftTests.cs` using `-> no transition` targets (documented in scaffold and decision file).
- Decision filed: `.squad/decisions/inbox/soup-nazi-issue13-test-strategy.md`.
- Key learning: the `TypeCheckResult.Diagnostics[n].Constraint.Id` + `.DiagnosticCode` pattern from StringAccessorTests.cs is the established assertion form for new diagnostic codes. The commented-out assertions in stubs follow this pattern for easy activation.

### 2026-04-10 — Issue #10: three-level dotted form tests written
- Unlocked 2 deferred parser/type-checker tests in `StringAccessorTests.cs` (converted from placeholder invariant-field forms to actual `Submit.Name.length` guard forms): `Parse_ThreeLevel_EventArgLength_AcceptsForm`, `Check_ThreeLevel_EventArgLength_NonNullable_NoDiagnostic`.
- Added 3 new tests: `Check_ThreeLevel_NullableEventArg_WithoutGuard_ProducesC56` (C56 on nullable event arg — the critical path), `Check_ThreeLevel_NullableEventArg_WithNullGuard_NoC56` (null narrowing removes C56), `Fire_ThreeLevel_EventArgLength_GuardRouting` ([Theory], 2 cases: "Bob" → Transition, "Bo" → Rejected).
- Invariant scope skipped with note: event args are not accessible in invariant scope.
- Runtime arg key format confirmed: `["Name"] = value` (arg name only, no event prefix).
- Decision filed: `.squad/decisions/inbox/soup-nazi-issue10-three-level-tests.md`.

### 2026-04-10 — Issue #10: string .length accessor test file authored
- Created `test/Precept.Tests/StringAccessorTests.cs` (23 tests) covering all Issue #10 acceptance criteria.
- **Test categories:** parser (2), type checker (7 incl. C56/PRECEPT056 and null narrowing), runtime value semantics (4 incl. UTF-16 emoji), null-guard compound evaluation (4), invariant context (2), event assert context (2), guard routing (2), regression (1).
- **Key learnings:**
  - Event arg access in transition guards uses dotted form `EventName.ArgName` (e.g. `Submit.Note`); appending `.length` produces a three-level dotted form `EventName.ArgName.length` that George's parser extension must handle.
  - Event asserts (`on Submit assert`) support both bare arg names and dotted prefix; three-level dotted form (`Submit.Name.length`) follows the same pattern.
  - The `and`/`or` null-narrowing precedent already exists for C42 (nullable scalar in set context); C56 follows the same narrowing contract.
  - Literal `set Field = ""` with a `Name.length >= 1` invariant throws at compile time (literal invariant check). Use a non-literal event arg as set source to defeat compile-time check and test runtime invariant enforcement.
  - UTF-16 emoji "💀" has `.Length == 2` in .NET strings; test explicitly documents this as the platform-consistent semantics decision.
  - Type error codes for `.length` on non-string types are unknown pre-implementation — tests assert `HasErrors` + `NotBeEmpty()` with TODO for specific code pinning.
  - Regression guard: `Regression_CollectionCount_ParsesAndCompiles_Unaffected` verifies `.count` is unaffected by the `.length` addition.

### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### 2026-04-10 - Issue #31 Slice 7: keyword logical operator tests
- Updated 9 existing test files (8 in `test/Precept.Tests/`, 1 in `test/Precept.LanguageServer.Tests/`) to replace DSL symbols `&&` → `and`, `||` → `or`, `!` → `not` in all `.precept` string literals and operator assertions.
- Created new `test/Precept.Tests/PreceptKeywordLogicalOperatorTests.cs` covering: basic keyword parsing (not/and/or), precedence validation (not > and > or), null narrowing through `not (Field == null)`, `!=` operator unaffected, old symbols `&&`/`||`/`!` produce parse errors, compound expression parse/evaluate, invariant context (or/and).
- George's runtime changes (parser, tokenizer, type checker, evaluator, samples) were already on the branch — tests written against the finished spec.
- Key learning: always check if the partner's runtime changes are already on branch before assuming tests will fail. The old-symbol-produces-error tests and keyword tests may pass immediately if George's work is complete.

### 2026-04-08 - Charter: MCP Regression Testing skill section added
- `charter.md` now includes a `## MCP Regression Testing` section with the full 4-round methodology, all authoring rules hard-won from live execution (multi-line rows, `when` guard placement, `dequeue`/`pop` into, diagnostic code vs. constraint index, C50 scope), and per-round pass criteria.
- The section is the canonical reference for future regression rounds authored by any agent — no need to rediscover parse failures from scratch.

### 2026-04-08 - Exploratory MCP regression rounds 1 & 2 (synthesized probes)
- Wrote 127 new tests across 3 files covering all stateless precept behaviors: parser (IsStateless, root edit all/fields, C12/C13/C49/C55 diagnostics), runtime (CreateInstance null state, Fire→Undefined, Inspect→null currentState, Update editability), MCP tools (CompileTool IsStateless/InitialState/StateCount, InspectTool/FireTool/UpdateTool with null currentState), and LS completions (root edit 'all' and field names).
- Fixed 8 nullable warnings in pre-existing test files (InitialState!, TransitionRows!, col!).
- Fixed stale CompileToolTests.DeadEndState_HintDiagnostic — C50 severity is Warning not Hint per squad decision.
- Final baseline: 754 passing, 0 failing (612 core / 55 mcp / 87 ls). Build: 0 warnings, 0 errors.
- Key learning: root edit blocks have State == null; "all" is stored as FieldNames sentinel ["all"] expanded at engine construction. GetEditableFieldNames(null) is the internal API. Stateless CreateInstance(state, ...) throws ArgumentException with "stateless" in the message.

### 2026-04-10 - Hero candidate DSL validation
- Compile-validated the hero candidate set with precept_compile and separated valid examples from advisory failures.
- Key learning: a hero sample is not real until the engine accepts it without caveat.

### 2026-04-10 - Test refresh coverage analysis
- Reviewed current test/project coverage and documented where follow-up strengthening would matter most.
- Key learning: coverage summaries are useful only when they identify the specific behavioral lanes still at risk.

### 2026-04-08 - Exploratory MCP regression rounds 1 & 2 (synthesized probes)
- Executed 18 compile probes and 3 runtime shapes via live MCP tools (no sample file reads except to learn syntax).
- Round 1: 15/18 probes passed as authored. Three failures were test plan syntax errors, not engine bugs. Corrected versions all passed.
  - Multi-line transition action chains fail parsing — all actions must be on one line.
  - `when` guard must appear before the first `->`, not after it.
  - `dequeue`/`pop` require `into <field>` target; bare form is not valid.
  - Probe 16 used wrong code: duplicate initial state emits PRECEPT008, not C13/PRECEPT013.
  - Probe 17 had wrong expectation: C50 fires only for states WITH rows that can't reach another state. Zero-row terminal state = no diagnostic.
- Round 2: All 7 outcome kinds confirmed across Approval/FeatureGate/RangeGuard shapes. Transition, NoTransition, Rejected, ConstraintFailure, UneditableField, Update, Undefined — all verified.
- Verdict: PASS (engine). Five test plan items need correction.
- Report: `.squad/decisions/inbox/soup-nazi-mcp-regression-exploratory.md`.
- Key learning: `from any` expansion is eager at compile time — visible as separate rows in compile output. C-prefixed constraint indices ≠ emitted PRECEPT diagnostic codes.

### 2026-04-08 - MCP regression pass for PR #48 (data-only precepts)
- Full manual regression via live MCP tools: 24 sample compiles (Round 1), stateful end-to-end on maintenance-work-order (Round 2), stateless end-to-end on customer-profile + fee-schedule (Round 3), 4 edge-case diagnostics (Round 4).
- Round 1: 24/24 valid, 0 error diagnostics. 3 new stateless samples (customer-profile, fee-schedule, payment-method) all return `isStateless: true`.
- Round 2: Draft→Open transition fired correctly; UneditableField and Update outcomes both confirmed.
- Round 3: `edit all` expands to all fields, selective edit (`edit BaseFee, DiscountPercent, MinimumCharge`) blocks TaxRate with UneditableField "(stateless)". ConstraintFailure triggered by MarketingOptIn invariant. Fire on stateless precept → Undefined with "stateless" message.
- Round 4: C12, C55, C49, and parse-failure all behave exactly per spec. C49 is warning (precept stays valid:true); C55 is error (valid:false).
- Verdict: PASS. PR #48 is clear to merge. Report filed to `.squad/decisions/inbox/soup-nazi-mcp-regression.md`.
- Key learning: Inspect of an event that triggers a state assertion shows ConstraintFailure in pre-flight when required args are absent (e.g., Assign in Open shows ConstraintFailure against the Scheduled assertion before args are evaluated). Fire still succeeds when args are provided.

### 2026-04-08 - Frank PR #48 review gaps: C49 multi-event + stateful edit-all
- Added `Parse_C49_MultipleEvents_ProducesOneWarningPerEvent` to `PreceptStatelessParserTests` — verifies 3-event stateless precept produces exactly 3 PRECEPT049 warnings (Decision 10 guarantee).
- Added `Update_StatefulPrecept_EditAll_ExpandsToAllFields` to `PreceptStatelessUpdateTests` — verifies `in State edit all` on a stateful precept expands correctly through the engine (Update returns success, not UneditableField).
- Both tests pass; committed on feature/issue-22-data-only-precepts.
- Key learning: `engine.Update(instance, patch => patch.Set(...))` is the only overload — task brief's `Dictionary` form doesn't exist in the API; use the builder lambda.
