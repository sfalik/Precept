## Core Context

- Owns the core DSL/runtime: parser, type checker, graph analysis, runtime engine, and authoritative language semantics.
- The engine flow is parser to semantic analysis to graph/state validation to runtime execution. Fire behavior must stay consistent with diagnostics, README examples, and MCP output.
- Key runtime areas to protect: constraints, transition guards, event assertions, collection hydration, edit rules, and diagnostic catalogs.
- Documentation should describe the six-stage fire pipeline and implemented semantics accurately, without inventing capabilities.

## Recent Updates

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
