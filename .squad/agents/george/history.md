## Core Context

- Owns the core DSL/runtime: parser, type checker, graph analysis, runtime engine, and authoritative language semantics.
- The engine flow is parser to semantic analysis to graph/state validation to runtime execution. Fire behavior must stay consistent with diagnostics, README examples, and MCP output.
- Key runtime areas to protect: constraints, transition guards, event assertions, collection hydration, edit rules, and diagnostic catalogs.
- Documentation should describe the six-stage fire pipeline and implemented semantics accurately, without inventing capabilities.

## Recent Updates

### 2026-04-12 — Issue #17 Computed Fields: Runtime Feasibility Assessment
- **Verdict: Feasible.** No fundamental blockers, no architectural mismatches.
- **Parser (Small):** `derivedOpt` combinator after TypeRef in FieldDecl. Arrow disambiguation is trivial — `field` keyword vs. `from` keyword disambiguates before either combinator reaches `->`. ~25 lines.
- **Type Checker (Medium-High):** 6 concerns — mutual exclusions (nullable/default + derived), nullable-ref rejection, collection accessor safety (.count only), event-arg scope rejection, dependency graph + topological sort, cycle detection (C85). ~150 lines total. All additive, no existing logic modified.
- **Expression Evaluator (None):** Existing `Evaluate(expr, context)` handles computed expressions with zero changes. Scope restriction is enforced by type checker + context dictionary, not the evaluator.
- **Runtime Engine (Medium):** New `RecomputeDerivedFields` helper (~25 lines) + 5 one-line insertion points (Fire no-transition, Fire transition, Update, Inspect event, Inspect patch) + `BuildInitialInstanceData` for initial computed values. Insertion point: after `CommitCollections`, before constraint evaluation in every pipeline.
- **External Input Rejection (Small):** 3 API boundaries — CreateInstance, Update, MCP. Engine-level rejection propagates to all consumers.
- **Model:** `PreceptField` gains `DerivedExpression?` and `DerivedExpressionText?` optional tail params. Non-breaking for all existing code.
- **New Diagnostics:** 8 codes (C80–C87) covering mutual exclusions, nullable/event-arg/unsafe-accessor rejection, cycle detection, edit/set restrictions.
- **Risk flags:** Multi-name field declarations for computed fields (recommend reject), `BuildDefaultData` needs computed field initial values (semantic edge needing test coverage).
- **Total estimate:** ~300-350 lines production code, 6-7 vertical slices.
- Assessment filed to `temp/george-proposal-review-17.json`.

### 2026-04-12 — Event hooks runtime implementation impact analysis
- Analyzed parser, model, type checker, and engine impact for `on <Event> -> <ActionChain>` (Issue A: stateless event hooks).
- **Parser:** Small. Syntactically unambiguous — `Arrow` after `On + Identifier` distinguishes `EventActionDecl` from `EventAssertDecl`. Reuses existing `ActionChain` unchanged. New `EventActionResult` private record.
- **Model:** Extra-small. New `PreceptEventAction` record (mirrors `PreceptStateAction`).
- **Type checker:** Medium. C49 suppression logic (3-case matrix), field-only scope validation.
- **Engine:** Medium. Hard-abort semantics recommended for hook constraint violations (vs. silent-swallow for state hooks). Execution order: after asserts, before invariants.
- Full analysis filed at `.squad/decisions/inbox/george-event-hooks-runtime-impact.md` (now merged to decisions.md).



### 2026-04-11 — Slice 11: Documentation updates for `when` guards on declarations (Issue #14)

Updated 6 documentation files to sync with the completed `when` guard implementation:
- **`docs/PreceptLanguageDesign.md`**: Updated grammar rules (Invariant, StateAsserts, EventAssert, StateEditDecl) to include `WhenOpt`. Added semantics note about when guards on declarations (guard scope, C69, narrowing exclusion). Added conditional forms to state asserts section, conditional editability subsection, when guard note to event asserts section. Added C69 to constraint codes table. Added when guards bullet to Status section.
- **`docs/EditableFieldsDesign.md`**: Added conditional syntax form to Syntax section. Added "Conditional editability" subsection to Semantics. Updated `PreceptEditBlock` record to show `WhenText`/`WhenGuard` params. Documented `_guardedEditBlocks` field and `EvaluateGuardedEditFields` helper in engine editability map section.
- **`docs/ConstraintViolationDesign.md`**: Added C69 to compile-phase diagnostics table.
- **`docs/RuntimeApiDesign.md`**: Added `when` guard evaluation notes to fire pipeline (event asserts, invariants/state asserts, edit blocks).
- **`docs/McpServerDesign.md`**: Added declaration arrays (`invariants`, `stateAsserts`, `eventAsserts`, `editBlocks`) with `when?` property to `precept_compile` output documentation.
- **`README.md`**: Added "Conditional declarations" bullet to feature list.
- All 1,094 tests passing (883 core + 137 LS + 74 MCP).

### 2026-04-11 — Issue #14 full implementation scope document

Complete spec written covering all 4 forms and 19 change sites across 5 files. Filed to `.squad/decisions/inbox/george-issue14-implementation-scope.md`. Key determination: single implementable slice with a trivial prerequisite commit for the B1 narrowing fix.

### 2026-04-11 — PR #63 Slices 0, 1, 2: model + diagnostic + parser injection for `when` guards

- **Slice 0:** Added `&& stateAssert.WhenGuard is null` filter to `BuildStateAssertNarrowings` in `PreceptTypeChecker.cs`. Prevents guarded assertions from participating in unconditional per-state type narrowing.
- **Slice 1:** Added `WhenText`/`WhenGuard` optional tail parameters to `PreceptInvariant`, `StateAssertion`, `EventAssertion`, `PreceptEditBlock`. Added C69 diagnostic for cross-scope guard references.
- **Slice 2:** Injected `OptionalWhenGuardParser` into `InvariantDecl`, `StateAssertDecl`, `EventAssertDecl`, and `EditDecl`. Updated `StateAssertResult` and `EditResult` private records to carry guard fields. Updated `AssembleModel` to thread guards through to model records. Updated construct descriptions to show `[when <Guard>]`.
- **Build:** Clean (0 errors, 0 warnings). **Tests:** 849 passed, 0 failed.
- **Key pattern:** `OptionalWhenGuardParser` injection point differs for edit blocks (between `StateTarget` and `Edit` token) vs. Forms 1–3 (between `BoolExpr` and `Because`). This was already documented in the implementation scope but confirmed in practice.

### 2026-04-11 — Form 4 simplicity analysis (Issue #14 follow-up)

### 2026-04-11 — Slice 5: Runtime Form 4 (conditional edit with additive approach)
- Added `_guardedEditBlocks` field (`IReadOnlyList<PreceptEditBlock>`) to `PreceptEngine`. Constructor now routes `block.WhenGuard is not null` edit blocks to this list; unconditional blocks continue to `_editableFieldsByState` unchanged.
- New private helper `EvaluateGuardedEditFields(state, data)`: iterates guarded blocks, evaluates guards via `PreceptExpressionRuntimeEvaluator.Evaluate`, fail-closed (any error/non-true → field not granted), returns union of passing field names.
- Updated 4 call sites: `Update` (Stage 1 editability check — moved `HydrateInstanceData` up for guard eval), `Inspect(patch)` editability check, `BuildEditableFieldInfos`, `GetEditableFieldNames` (added optional `instanceData` parameter).
- All copy-on-read pattern: static editable sets copied to new `HashSet` before union with guarded fields — `_editableFieldsByState` dictionary never mutated.
- Build clean. All 1,045 tests pass (850 core + 128 LS + 67 MCP). Zero regressions — no existing tests use guarded edit blocks.

## Learnings

### 2026-04-11 — Slice 4: Runtime engine guard pre-flight for Forms 1–3
- Added guard pre-flight to `EvaluateInvariants`, `EvaluateStateAssertions`, and `EvaluateEventAssertions` in `PreceptRuntime.cs`.
- Pattern: if `WhenGuard is not null`, evaluate against the same data dictionary as the body expression. If guard fails or evaluates to non-true, `continue` (skip the declaration). Collect-all semantics preserved — no caller changes needed.
- Event assertions use `evaluationData` (field + arg data) for guard evaluation, matching the body expression scope. Invariants and state asserts use field-only `data`.
- Build clean. All 850 tests pass — no existing tests use `when` guards, so zero regressions expected and confirmed.

## Learnings

- **Full implementation scope for Issue #14 (all 4 forms, same-wave):** 19 change sites, ~162 lines, 5 files. Single PR is the recommendation, with a trivial prerequisite commit for the B1 narrowing fix (1 line in `BuildStateAssertNarrowings`).
- **Injection point for `EditDecl` guard is different from Forms 1–3.** Forms 1–3 inject `OptionalWhenGuardParser` between `BoolExpr` and `Because`. Form 4 injects it between `StateTarget` and `Edit` token. Both reuse the same `OptionalWhenGuardParser` static parser.
- **Four model records need `WhenGuard?`/`WhenText?` tail params.** Pattern is identical to `PreceptTransitionRow`. Being optional/tail params makes the change non-breaking for all existing call sites.
- **`StateAssertResult` and `EditResult` are private parser records** — they also need the guard fields threaded through to `AssembleModel`. `InvariantResult` wraps `PreceptInvariant` directly so guard passes through its constructor. `EventAssertResult` wraps `EventAssertion` directly — same.
- **`CollectCompileTimeDiagnostics` lives in `PreceptRuntime.cs`, not `PreceptTypeChecker.cs`.** The C29/C30/C31 guard-skip changes are in `PreceptRuntime.cs` (~line 1904). Confirm before assigning.
- **Guard evaluation against `BuildDefaultData` for C29/C30/C31:** `BuildDefaultData` only includes fields with explicit defaults; nullable-no-default fields are absent. Missing key → evaluator `Fail` → fail-closed → skip body check. This is correct EC-3 behavior.
- **Nullable field guard behavior resolved:** Evaluator returns `EvaluationResult.Fail` (not throw) for all error conditions. Fail-closed means guard not satisfied → skip assertion. Safe for both absent-from-dict (field not in data) and present-with-null-value (null comparison evaluates correctly).
- **`_guardedEditBlocks` contains only Form-4 guarded blocks** (blocks where `block.WhenGuard is not null`). Unconditional blocks continue to pre-populate `_editableFieldsByState` unchanged. The split happens in the constructor loop.
- **Three `Update`/`Inspect`/`BuildEditableFieldInfos` call sites need the guarded edit second pass.** Pattern is identical across all three: filter by state, evaluate guard against hydrated data, merge passing fields into editable set.
- **Hydration reorder in `Update` is safe:** `HydrateInstanceData` is a pure read (builds internal format dict from clean InstanceData). Moving it from Stage 3 to the top of `Update` has no behavioral effect on existing code paths (Stage 3 still mutates the resulting dict, but reads from it first is fine).
- **C69 cross-scope guardian diagnostic:** C38 already fires for identifiers truly absent from scope. C69 is a better-message supplement for the case where the identifier exists in the "wrong" scope (e.g., dotted event arg in an invariant guard). Implemented as `CheckCrossScopeGuardIdentifiers` helper (~18 lines) called after each guard `ValidateExpression` call.
- **`in any when <guard> edit` at zero states:** `ExpandStateTargets` with empty `states` list returns empty — zero `PreceptEditBlock` entries created. Silent parse success, but structurally no-op. Type checker would catch the semantic issue via existing C55 logic if applicable.

- **`PreceptEditBlock` currently has NO `WhenGuard` field.** The model record is `(State, FieldNames, SourceLine)`. Form 4 requires adding `PreceptExpression? WhenGuard = null` as an optional parameter — identical pattern to how transition rows already carry `WhenGuard`.
- **The "architectural mismatch" is real but not a rework.** `_editableFieldsByState` is a `Dictionary<string, HashSet<string>>` built at construction — cannot precompute guarded blocks. But the additive path avoids touching it entirely: a new `_guardedEditBlocks : IReadOnlyList<PreceptEditBlock>` field routes guarded blocks separately, evaluated at call time.
- **Three engine call sites** need a second-pass guard evaluation loop (in order of how data flows): `Update` Stage 1, `Inspect(patch)` editability check, `BuildEditableFieldInfos`. All three already have instance data in scope (or adjacent) — the pattern is the same as `EvaluateInvariants`.
- **Hydration ordering edge.** `Update` currently hydrates instance data at Stage 3, after the Stage 1 editability check. Guard evaluation needs hydrated data. The fix is to hydrate at Stage 1 when guarded blocks exist — functionally a no-op (hydration is a pure read), but a minor structural rearrangement. The cleanest approach: call `HydrateInstanceData` unconditionally at the top of `Update`. Already done in `Inspect(patch)` — the pattern exists.
- **`GetEditableFieldNames` is `internal`, test-only.** Only the test suite calls it. No language server or MCP code references it. A signature addition (`data?` parameter) is entirely contained.
- **Guard evaluation infrastructure is already in place.** `PreceptExpressionRuntimeEvaluator.Evaluate(expression, data)` is called today by `EvaluateInvariants`, `EvaluateStateAssertions`, and transition row first-match logic. Form 4 guard evaluation is the same call with no new evaluator changes.
- **New type-checker concern: guard scope in edit blocks.** Edit guards must be field-only — no event arg references. A small type-checker addition is needed (roughly same as the C69 work flagged for cross-scope guard references). ~5 lines, one new diagnostic.
- **`in any when <guard> edit <fields>` expansion.** Parser already emits one `PreceptEditBlock` per state for `any`. With guard, each block carries the same `WhenGuard` — runtime evaluates per state naturally. No special handling needed in the engine.
- **Complexity tier vs. Forms 1–3.** Form 4 under the additive approach is roughly 1.5× the effort of a single Form 1–3 instance. The extra pieces over any one of Forms 1-3: one new engine field, constructor routing, hydration reorder in 2 methods, second-pass at 3 call sites. All well-defined and bounded. Same-wave is defensible; follow-on issue is also defensible as a PR-size preference rather than a technical necessity.

### 2026-04-11 — Slice 8: sample file updates (integer/decimal/choice)
- **Commit:** `a7b4fb0` on `feature/wave3-integer-decimal-choice`.
- **integer samples (3 files):**
  - `crosswalk-signal.precept`: `CycleCount` and `CountdownSeconds` → `integer`. Clean; `CountdownSeconds = 20/7/0` and `CycleCount + 1` are all integer literals.
  - `hiring-pipeline.precept`: `FeedbackCount` → `integer`. `FeedbackCount + 1` and `FeedbackCount >= 2` are integer-compatible.
  - `it-helpdesk-ticket.precept`: `ReopenCount` → `integer`; also added `Priority as choice("Low","Medium","High","Critical") default "Low"` + `in New edit Priority` (covers choice requirement in same file).
- **decimal samples (3 files):**
  - `insurance-claim.precept`: `ClaimAmount` and `ApprovedAmount` → `decimal maxplaces 2`. Event args `Submit.Amount` and `Approve.Amount` also updated to `decimal` to satisfy assignment type checking.
  - `fee-schedule.precept`: `BaseFee`, `DiscountPercent`, `MinimumCharge` → `decimal maxplaces 2`; `TaxRate` → `decimal maxplaces 4` (rate precision differs from currency).
  - `travel-reimbursement.precept`: `MileageTotal` → `decimal maxplaces 2`; `set MileageTotal = round(Submit.Miles * Submit.Rate, 2)` (second round() sample after event-registration).
- **choice samples (2 files):**
  - `customer-profile.precept`: `PreferredContactMethod` → `choice("email","phone","sms") default "email"`. Removed the now-redundant `!= ""` invariant (choice membership structurally enforces non-empty).
  - `it-helpdesk-ticket.precept`: see above.
- **Key learnings:**
  - `truncate()` is NOT a valid expression in the parser despite appearing in the type-checker error hint text. Reverting `EstimatedWaitMinutes` to `number` in restaurant-waitlist was the right call.
  - `number_literal * 10` (even when one operand is `integer`) produces `number` type because `10` literal is typed as `number`. Only direct integer+integer or integer assignments avoid widening.
  - When changing a field to `decimal`, all event args that feed into that field via `set` must also be `decimal` (no implicit promotion from `number`).

### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### 2026-04-10 - Issue #31 Slices 1-4 + Samples (keyword logical operators)
- **Token names found:** `And`, `Or`, `Not` — already existed in `PreceptToken` enum with old `[TokenSymbol("&&")]`, `[TokenSymbol("||")]`, `[TokenSymbol("!")]`. Changed to `[TokenSymbol("and")]`, `[TokenSymbol("or")]`, `[TokenSymbol("not")]`. Both `TokenCategory.Operator` attributes were correct; no category changes needed.
- **Tokenizer protection:** `requireDelimiters: true` on keyword registration (step 7 in `Build()`) is the mechanism that prevents `android` from matching `And` + `roid`. The operator entries (`&&`, `||`, `!`) were in steps 4-5 (plain span/character matches without delimiters) — removing them from those sections was sufficient, since `And`/`Or`/`Not` are now registered as keywords via the keyword loop.
- **Surprises — none.** The branch already had all five source-file changes prepared (PreceptToken.cs, PreceptTokenizer.cs, PreceptParser.cs, PreceptTypeChecker.cs, PreceptExpressionEvaluator.cs) plus sample file changes. The work was complete; it only needed a build verification and commit.
- **ApplyNarrowing location:** `PreceptTypeChecker.cs` around line 889 (method body starts after `StripParentheses` call). The pattern-matched `"!"` → `"not"` update was in the `PreceptUnaryExpression { Operator: "not" } unary` destructure; `"&&"` → `"and"` and `"||"` → `"or"` were `binary.Operator ==` string comparisons in the same method. Both were already updated on the branch.
- **`CatalogDriftTests.cs` change:** C4 test used `PreceptParser.ParseExpression("&&")` to trigger parse-expression failure path; updated to `"and"` since the old `&&` is no longer a valid token.
- **Build:** 0 errors, 0 warnings on full solution (`dotnet build`). Committed as `83497aa` on `squad/31-keyword-logical-operators`.
- **Key pattern:** When a branch has pre-staged diffs, always run `git status` + `git diff` to inventory existing work before touching files — avoids double-applying changes.

### 2026-04-08 - Slice 4 implementation for issue #22 (data-only precepts)
- `PreceptEngine.IsStateless` computed property (`States.Count == 0`); `InitialState` made `string?`.
- Constructor: `InitialState = model.InitialState?.Name`; edit blocks loop replaced to route `block.State == null` to `_rootEditableFields` (new `HashSet<string>?` field); `"all"` sentinel expanded via `ExpandEditFieldNames()` private helper.
- `CreateInstance(data)`: stateless path bypasses 2-arg and creates instance with `CurrentState = null`. `CreateInstance(state, data)`: throws `ArgumentException` for stateless.
- `CheckCompatibility`: stateless branch validates `CurrentState == null`; stateful branch validates state membership; `EvaluateStateAssertions` wrapped in `if (instance.CurrentState is not null)` guard.
- `Fire`: early `Undefined` return after compatibility check when `IsStateless`.
- `Inspect(instance, event)`: early `Undefined` return after compatibility check when `IsStateless`.
- `Inspect(instance)`: stateless path produces all events with `Undefined` outcome; editable fields via new `BuildEditableFieldInfosForStateless`.
- `Update` Stage 1: branches on `IsStateless` to pull editableFieldSet from `_rootEditableFields` vs `_editableFieldsByState`; Stage 4: `EvaluateStateAssertions` null-guarded.
- `GetEditableFieldNames(string? state)` + `BuildEditableFieldInfos(string? state, ...)` accept nullable state; BuildEditableFieldInfos delegates to `BuildEditableFieldInfosForStateless` for null.
- Result records: `EventInspectionResult.CurrentState`, `InspectionResult.CurrentState`, `FireResult.PreviousState` made `string?`; all 12 factory method `string state` params made `string?`.
- Nullable fixups: null-forgiving `!` after IsStateless guards in Fire/Inspect stateful branches; `EvaluateCurrentRules` + `Inspect(patch)` `EvaluateStateAssertions` null-guarded; `TryValidateScalarValue` `out string? error` → `out string error`; `InitialState` dereferences in `PreceptAnalysis` and `CollectCompileTimeDiagnostics` made null-safe with `!`.
- Build: 0 errors, 0 warnings on `src/Precept/Precept.csproj`. Committed as `d3fe90d`.
- Unguarded `CurrentState` in OTHER projects (not fixed in this slice): `tools/Precept.Mcp/Tools/CompileTool.cs:54`, `tools/Precept.LanguageServer/PreceptPreviewHandler.cs:296-297`, `tools/Precept.LanguageServer/PreceptDocumentIntellisense.cs:465`, `test/Precept.Tests/NewSyntaxParserTests.cs:34,1044,1058,1072,1086`, `test/Precept.Tests/PreceptWorkflowTests.cs:47,921`, `test/Precept.Tests/PreceptCollectionTests.cs:946`.
- Key pattern: after `if (IsStateless) return ...;` the compiler still sees `CurrentState` as `string?` — use `instance.CurrentState!` throughout the now-stateful path. Compiler stops emitting CS8604 for the same variable after the first flagged call site in a method, so some warning sites only appear after upstream ones are fixed.

### 2026-04-08 - Slice 3 implementation for issue #22 (data-only precepts)
- C50 severity upgraded from `ConstraintSeverity.Hint` to `ConstraintSeverity.Warning` in `DiagnosticCatalog.cs`. Safe: all 21 samples produce zero C50 diagnostics.
- `PreceptRuntime.Validate`: wrapped C27/C28 checks inside `if (!model.IsStateless)`. Both checks dereference `model.InitialState.Name` which is null for stateless; the guard prevents `NullReferenceException`. Used null-forgiving operator (`!`) inside the block — parser's C13 guarantees `InitialState != null` when `States.Count > 0`.
- `PreceptAnalysis.Analyze`: added stateless early-return immediately after the three variable declarations (`states`, `events`, `transitionRows`), before `allStateNames`. Fires C49 per declared event (each is structurally orphaned — no state routing surface). Suppresses C53 (no-events hint) for stateless. Returns all-empty state/reachability arrays. Comment uses `// Stateful path continues below...` before the moved `allStateNames` declaration.
- Build: 0 warnings, 0 errors. The 23 nullable warnings from Slices 1/2 resolved (build mode/config difference — Precept.csproj alone reports clean). Committed as `72c65c1`.

### 2026-04-08 - Slice 2 implementation for issue #22 (data-only precepts)
- Implemented all parser/diagnostic changes for data-only precepts: `FieldTarget` parser (`all` | identifier list, mirrors `StateTarget`/`any`); `EditDecl` updated to use `FieldTarget` instead of inline identifier list; `RootEditDecl` parser (`edit <FieldTarget>`, root-level, no `in` prefix); `RootEditResult` sealed record; `AssembleModel` routes `RootEditResult` → `PreceptEditBlock(State: null, ...)`; C12 broadened ("at least one field or state"); C13 made conditional on `states.Count > 0`; C55 added to `DiagnosticCatalog` (compile phase, Error) and enforced in `CollectCompileTimeDiagnostics`.
- Key design decision: C55 is "compile" phase, not "parse" — enforced in type-checker pass, not in `AssembleModel`. Allows root `edit` to parse cleanly; cross-cutting check (states + root edit) runs at validation time.
- Build confirmed: 23 nullable warnings (Slice 4 scope, unchanged from Slice 1), 0 errors. Committed as `7e9bece`.

### 2026-04-08 - Slice 1 implementation for issue #22 (data-only precepts)
- Implemented all 4 model/token changes as specified: `PreceptToken.All` added after `Any` with `[TokenCategory(Grammar)]`/`[TokenSymbol("all")]`; `PreceptDefinition.InitialState` made nullable (`PreceptState?`); `PreceptDefinition.IsStateless` computed property added (`States.Count == 0`); `PreceptEditBlock.State` made nullable (`string?`); `PreceptInstance.CurrentState` made nullable (`string?`).
- Build succeeded with 23 expected nullable warnings (CS8602/CS8604/CS8620/CS8601) in `PreceptRuntime.cs` and `PreceptAnalysis.cs` — all in Slice 4 scope. Zero build errors.
- Committed as `e0eac05` on `feature/issue-22-data-only-precepts`.

### 2026-04-08 - Issue #22 semantic rules runtime/parser analysis
- Reviewed issue #22 semantic rules against actual parser/type-checker code at Shane's request.
- Key findings: (1) C12 fires at end of AssembleModel — adding a state doesn't violate anything, making "states forbidden" tautological. (2) EventDecl parser has zero state dependencies — events parse fine without states but have no dispatch surface; a new diagnostic would be needed. (3) C54 already rejects transitions referencing undeclared states, making "transitions forbidden" structurally redundant.
- Recommended reframe: drop tautological rules, add explicit "events forbidden in stateless" as a new type-checker diagnostic.

### 2026-04-08 - Language research corpus completed
- George's type-system lane now sits inside a finished language research corpus: Batch 1/2 evidence landed in `54a77da` and `48860ae`, and the closing corpus/index sweep landed in `3cc5343`.
- Final bookkeeping preserved the domain-first indexes so the type-system survey and semantics work stay grounded in research docs rather than being pushed back into proposal-body edits.

### 2026-04-08 - Type-system research corpus landed
- The type-system domain research packet landed as part of Batch 1, and the later rewrite of `docs/research/language/references/type-system-survey.md` was included in the Batch 2 commit `48860ae`.
- The durable type lane now has both a domain survey and a stronger formal/reference survey, which keeps the research corpus aligned with the no-proposal-body-edit rule for batch curation.

### 2026-04-05 - Named rule scope and naming converged
- Confirmed field-scoped reuse is sound in when, invariant, and state assert, while on <Event> assert remains incompatible because it is event-arg-only.
- Reweighted the naming decision around Precept's readability goals and aligned the runtime recommendation with rule over predicate.

### 2026-04-04 - DSL pipeline overview
- Consolidated the runtime mental model, major constraint categories, fire stages, and edge cases for downstream agents.
- Key learning: Precept's value is structural integrity across state, data, and rules; every outward-facing description should preserve that unified model.

### 2026-04-06 - README restructure proposal review
- Checked that proposed README/API explanations matched the real runtime surface.
- Key learning: the quickest way to damage trust is to let public examples diverge from actual parser/runtime behavior.

### 2026-05-14 - Named predicate naming analysis

- Re-evaluated whether `guard` is the correct category for a declaration reusable in transition `when`, `invariant`, and state `assert`.
- Key finding: `guard` carries strong transition-gate semantics (UML, xstate) and is misleading when a declaration appears in `invariant` or state `assert` positions. The concept is a **named field-scope boolean predicate**, not a transition guard.
- Compared options: `guard` (misleading), `predicate` (recommended), `rule` (existing informal overloading in docs), `let`/`define` (too generic, blurs computed-field boundary), split `guard`+`predicate` (fragmentation without benefit).
- Verdict: rename from `guard` → `predicate`. Keyword `predicate <Name> when <BoolExpr>`. All prior structural recommendations (reuse sites, scope rules, exclusions) unchanged.
- Amended `docs/research/dsl-expressiveness/expression-feature-proposals.md` (Proposal 3: all keyword and prose references updated) and `expression-language-audit.md` (L3: title, implementation notes, verdict, philosophy fit updated).
- Wrote `.squad/decisions/inbox/george-guard-naming.md` with full options analysis.
- Key pattern: when a DSL construct's name implies narrower semantics than its actual reuse scope, fix the name before the design doc is written — not after.

### 2026-05-01 - Named guard reuse scope analysis

- Researched whether named guard declarations (Proposal 3) can be soundly reused in `invariant` and `in/to/from <State> assert` contexts, not just `when` clauses.
- Key finding: guard body scope (fields + collection accessors) is a subset of invariant/state-assert scope — reuse is sound in both. `on <Event> assert` is explicitly incompatible (event-arg-only scope at Stage 1, disjoint from field scope).
- Verdict: `feasible-with-caveats`. Caveats: (a) guard body must be validated in field-only scope at declaration time, not deferred to use site; (b) `on <Event> assert` must produce a clear diagnostic; (c) cycle risk resolved since guard-to-guard refs are banned.
- Updated `docs/research/dsl-expressiveness/expression-feature-proposals.md` (Proposal 3 scope rules, new invariant/assert examples) and `expression-language-audit.md` (L3 what is missing, implementation notes, verdict).
- Wrote `.squad/decisions/inbox/george-guard-reuse.md` with full analysis for team.

- Audited the full expression surface: parser, type checker, evaluator, all 21 sample files.
- Produced `docs/research/dsl-expressiveness/expression-language-audit.md` with 12 numbered limitations, implementation verdicts, and cross-cutting notes.
- Key findings:
  - No ternary expression (forces row duplication for conditional values)
  - No `string.length` accessor (string length constraints are inexpressible — trivially fixable)
  - No named guard declarations (multi-condition guards must be copy-pasted verbatim)
  - `on <Event> assert` scope excludes data fields (pipeline design constraint, not parser issue)
  - No numeric math functions like `abs()` (requires new function-call AST node)
  - `contains` is collection-only; no substring matching on strings
  - Division by zero produces silent NaN/infinity at runtime — evaluator should return Fail
- Feasibility verdicts: ternary=feasible, string.length=feasible, named-guards=feasible-with-caveats, abs/functions=feasible-with-caveats, collection-any-all predicates=not-recommended.
- The `on <Event> assert` scope limitation is the one item needing a design decision before any code — it touches the fire pipeline stage contract, not just the parser.
- Notified team via `.squad/decisions/inbox/george-expression-limitations.md`. No implementation until Frank's proposal and Shane's approval.

### 2026-04-08 - Issue #22 semantic rules review (data-only precepts)
- Reviewed issue #22 semantic rule about "states, events, and transitions forbidden in stateless precepts" against actual parser/type-checker code.
- Key findings from parser source: (1) C12 fires at the *end* of AssembleModel — adding a `state` to a definition doesn't violate anything, it just makes it stateful. The "prohibition" is tautological. (2) `EventDecl` parser has zero state dependencies — events parse fine without states, but have no dispatch surface. The type checker would need a *new* diagnostic to reject them. (3) C54 already rejects transition rows referencing undeclared states, making an explicit "transitions forbidden" rule structurally redundant.
- Recommended the issue reframe: remove tautological "states forbidden" rule, add explicit "events forbidden in stateless" as a new type-checker diagnostic (not parser), and note that transitions are already structurally impossible via C54.

## Learnings

### 2026-04-11 — Issue #14 runtime feasibility assessment (when-guards on constraint declarations)

- `OptionalWhenGuardParser` (PreceptParser.cs line 909) is already generic and reusable — no tokenizer changes needed for any of the four proposed forms.
- `BoolExpr` terminates before `when` naturally because `when` is a keyword token, not an `Identifier`. This is the same mechanism that already works in transition rows.
- Parser injection point for forms 1–3 (invariant, state assert, event assert): between `from expr in BoolExpr` and `from _ in Token.EqualTo(PreceptToken.Because)` in each declaration. One-liner change per form.
- Parser injection point for form 4 (edit guard): between `StateTarget` and `Token.EqualTo(PreceptToken.Edit)`. Statement union ordering is safe — `EditDecl.Try()` already precedes `StateAssertDecl.Try()` (line 964 vs 973 in statement union).
- Model records need only `WhenGuard = null, WhenText = null` as optional tail parameters on `PreceptInvariant`, `StateAssertion`, `EventAssertion`, `PreceptEditBlock` — same shape as existing `PreceptTransitionRow.WhenGuard/WhenText`.
- Type checker: use existing `dataSymbols` for invariant/state assert guards; use `BuildEventAssertSymbols` result for event assert guards. No new scope infrastructure needed. Cross-scope violations produce C38 by default; C69 (new) would give a targeted message.
- CRITICAL — narrowing exclusion: Guarded state asserts must be excluded from `BuildStateAssertNarrowings`. Add `WhenGuard is null` check before contributing to narrowing. If missed, type-checker over-narrows transition row types based on conditionally-true assertions.
- CRITICAL — edit guard runtime scope: `in State when <guard> edit <fields>` (form 4) breaks the static `_editableFieldsByState` HashSet built at construction time. Guard evaluation is per-instance and per-call. This is a qualitatively different implementation scope — recommend a separate sub-feature or defer.
- C29/C30 (compile-time invariant/assert checks against defaults): must evaluate guard first; if guard is false for defaults, skip the body check.
- `not` dependency (issue #31): SHIPPED April 10. Not a blocker.
- No existing "guard-skipped" status in engine output. Silent skip (no violation) is correct for v1. "Guard-skipped" as a distinct inspect annotation is an optional enhancement, not a gating requirement.
- Synthetic invariant flag (`IsSynthetic = true`) must be checked: field constraint desugaring should never attach a WhenGuard to synthetic invariants.

### 2026-04-11 — PR #63 reviewer blockers B1–B5: missing test coverage for `when` guards

Added 5 tests requested by Soup Nazi to resolve reviewer blockers on PR #63:
- **B1** (`NewSyntaxParserTests.Parse_ConditionalStateAssert_To_WhenGuardParsed`): Verifies `to <State> assert ... when <guard> because "..."` parses with Anchor=To, non-null WhenGuard/WhenText.
- **B2** (`NewSyntaxParserTests.Parse_ConditionalStateAssert_From_WhenGuardParsed`): Verifies `from <State> assert ... when <guard> because "..."` parses with Anchor=From, non-null WhenGuard.
- **B3** (`NewSyntaxRuntimeTests.Fire_GuardedStateAssert_WhenNot_SkipsWhenTrue`): `in Open assert X > 0 when not Bypass` — Bypass=true skips assert (no violation), Bypass=false applies assert.
- **B4** (`NewSyntaxRuntimeTests.Fire_GuardedEventAssert_WhenNot_SkipsWhenTrue`): `on Submit assert Submit.Amount > 0 when not Submit.IsDraft` — IsDraft=true skips (transition), IsDraft=false rejects.
- **B5** (`GuardedEditTests.Update_GuardedEdit_WhenNot_NegativeGuard`): `in Open when not IsLocked edit Notes` — IsLocked=false → editable, IsLocked=true → UneditableField.
- All 5 tests passing.

### 2026-04-12 — Conditional expression tests (Issue #9)

Wrote 24 tests for conditional expressions (`if <cond> then <expr> else <expr>`) and fixed 3 catalog drift entries:
- **Parser tests (8):** simple conditional, field ref + comparison, nested via parens, set RHS in full precept, invariant expression, when guard expression, arithmetic branches, boolean branches.
- **Type checker tests (9):** valid conditional (no diags), C78 non-boolean condition (number, string, nullable boolean — 3 tests), C79 branch type mismatch (string vs number, boolean vs string — 2 tests), null-narrowing in then-branch, nested conditional type consistency, function call in branch.
- **Runtime tests (7):** set with conditional, true branch, false branch, comparison condition, nested conditional, invariant with conditional, null-narrowing produces correct value.
- **Catalog drift fixes:** Added C78 and C79 to `ConstraintTriggers` dictionary and `LineAccuracyCases`. Both use `-> no transition` to avoid C54 masking.
- **Key gotchas found:**
  - Literal `0` is typed as `integer`, not `number`. When using `abs(field)` (returns `number`) in a conditional branch, the other branch must also be `number`-typed (e.g. `0.0`), or C79 fires for branch type mismatch.
  - An unguarded transition row followed by a reject row triggers C25 (unreachable duplicate). For conditional expressions that handle both null and non-null cases inline (via `if Name != null then Name else "default"`), the reject fallback row is unnecessary and must be omitted.
  - All 1,096 tests passing after changes.
