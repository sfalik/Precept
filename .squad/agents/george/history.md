# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. DSL that makes invalid states structurally impossible. Declarative `.precept` files compile to executable runtime contracts.
- **Stack:** C# / .NET 10.0 (core runtime, language server, MCP server), TypeScript (VS Code extension), xUnit + FluentAssertions
- **My domain:** `src/Precept/Dsl/` — full DSL pipeline: tokenizer, parser, type checker, expression evaluator, runtime engine, constraint evaluation
- **Key docs:** `docs/PreceptLanguageDesign.md` (DSL spec — law), `docs/RulesDesign.md` (constraints), `docs/ConstraintViolationDesign.md`, `docs/CatalogInfrastructureDesign.md`
- **Tests:** `test/Precept.Tests/` — 666 tests, xUnit + FluentAssertions
- **Samples:** `samples/` — 20 `.precept` files, canonical usage examples
- **Created:** 2026-04-04

## Learnings

### DSL Pipeline Overview (2026-04-04)

The runtime is a **5-stage deterministic pipeline** with explicit, immutable model stages:

1. **Tokenizer** (`PreceptTokenizer.cs`): Converts raw text → token stream via Superpower library. Keyword dictionary auto-built from `TokenSymbolAttribute` metadata on `PreceptToken` enum (zero drift). Handles comments (stripped), strings (C-escape aware), numbers (int/decimal/scientific), operators (multi-char before single), keywords (require delimiters). No indentation — purely token-driven.

2. **Parser** (`PreceptParser.cs`): Token stream → `PreceptDefinition` model tree. Hand-written recursive descent combinators using Superpower. Key model types: `PreceptField`, `PreceptCollectionField`, `PreceptState`, `PreceptEvent`, `PreceptTransitionRow`, `PreceptInvariant`, `StateAssertion`, `EventAssertion`, `PreceptStateAction`, `PreceptEditBlock`. Statements can appear in any order (flat structure — no nesting). Diagnostics include construct forms and context-aware suggestions (e.g., detects missing arrows in transition rows).

3. **Type Checker** (`PreceptTypeChecker.cs`): Walks the model, builds symbol scopes (field/arg scopes per statement), type-checks expressions (unary/binary/identifier/literal). Produces `PreceptTypeContext` with scope info. Detects: unknown identifiers, type mismatches, non-boolean guards/asserts, nullable assignments to non-nullable targets. Validation is compile-time-first: proves unsound contracts early, conservative on data-dependent cases.

4. **Analysis** (`PreceptAnalysis.cs`): Reachability graph from initial state, detects unreachable states, orphaned events, terminal states, dead-end patterns, reject-only rows. Issues are warnings/hints (not errors). Invariants/state asserts compiled to literal check at compile time on defaults.

5. **Compiler** (`PreceptRuntime.cs` builder): Validates constraints (C26–C54), builds lookup maps: `(State, Event) → TransitionRow[]` (first-match), `State → EditableFieldNames`, event/state assert maps. Hydration layer converts clean public `InstanceData` (plain lists) ↔ internal `__collection__<Name>` format (CollectionValue objects for evaluation). Engine is deterministic: no hidden state, no side effects.

### Core Constraint Categories

**Parse/Struct (C1–C25):** Empty input, tokenization, parse structure, duplicate names, nullability/defaults, collection mutations, unreachable rows.

**Compile/Type (C26–C43):** Model non-null, uniqueness, initial state validity, invariant checks on defaults, literal assignment checks, identifier resolution, type soundness, nullable narrowing.

**Compile/Analysis (C44–C54):** Duplicate asserts, subsumed asserts, unreachable states, orphaned events, dead ends, reject-only rows, empty precepts.

**Runtime (C33–C37):** Instance creation validity, data contract enforcement (types, nullability).

### Fire Pipeline (6 Stages)

**Stage 1** (Pre-transition validation): Event asserts only (args-only scope). Violations → Rejected.

**Stage 2** (Row selection): First-match on `(State, Event)`. Guard evaluated with `(internalData + eventArgs)`. First guard that passes (or no guard) = matched row. All guards fail & all guarded = NotApplicable. No row at all = Undefined.

**Stage 3** (Exit actions): Automatic mutations from `from State ->` executed on working copy.

**Stage 4** (Row mutations): Transition row's `set` and collection mutations applied.

**Stage 5** (Entry actions): Automatic mutations from `to TargetState ->` applied.

**Stage 6** (Post-transition validation): Invariants + StateAsserts (anchor=In on target state) evaluated. Violations → ConstraintFailure. Collect-all semantics (all violations reported).

NoTransition branch: Skips exit/entry, validates invariants + 'in' asserts on current state only.

### Collections and Hydration

Collections field use internal **`CollectionValue`** wrapper for evaluation (tracks kind, inner type, peek/min/max). Public API returns clean `List<object>`. Hydration on fire: `Data[Name] → __collection__Name: CollectionValue`. Dehydration on commit: `__collection__Name: CollectionValue → Data[Name]: List`. Collections are cloned during fire simulation, committed atomically after mutations pass validation. Empty collection operations (pop empty stack, dequeue empty queue) fail at runtime.

### State Asserts and Prepositions

- **`in State assert ...`** = "while residing in State" (entry + AcceptedInPlace + no-transition). Checked after every fire that lands in State, and on no-transition mutations in State.
- **`to State assert ...`** = "crossing into State" (entry only). Checked post-mutation when transitioning to State.
- **`from State assert ...`** = "crossing out of State" (exit only). Checked pre-mutation when leaving State. Violation prevents transition entirely.

Invariants always checked post-mutation. Event asserts (args-only) checked pre-transition, before row selection.

### Expression Evaluation (`PreceptExpressionRuntimeEvaluator`)

Binary `&&` and `||` short-circuit. `contains` operator for set membership (`Identifier contains Value`). Collection properties: `.count`, `.min`, `.max`, `.peek` — evaluated from `__collection__<Name>` key. Unary `-` and `!` as expected. Type coercion for JSON input (handles `JsonElement` unwrapping, string→number/bool parsing). Evaluation errors propagate as failed `EvaluationResult` (not exceptions).

### Editable Fields

Declared via `in State edit Field, Field`. Runtime `Update()` API applies patch atomically: (1) editability check, (2) type validation, (3) apply patch to working copy, (4) evaluate invariants + 'in' state asserts, (5) commit. Patch builder validates conflicts (cannot edit same field twice in one patch). `Inspect(..., patch)` for preview mode (no commit).

### Key Edge Cases & Risks

1. **Nullable narrowing in transitions**: Assignment `set NonNullField = NullableField` requires runtime null-check or compile-time proof. Engine evaluates expression but doesn't prevent assigning null. **Risk**: Could violate non-nullable contract if expression evaluates to null. **Mitigation**: Type checker enforces `when` guards to narrow before assignment.

2. **Empty collection operations**: `pop` on empty stack, `dequeue` on empty queue return error strings. Parsed as `Pop` / `Dequeue` verbs, validated at fire time. **Risk**: Late detection if guard doesn't prevent empty state.

3. **Collection min/max on empty sets**: Engine returns error string, not exception. **Guard pattern**: `when Tags.count > 0` before accessing `.min`/`.max`.

4. **First-match row semantics**: Rows are evaluated top-to-bottom, first match wins. If all guarded rows have failing guards → NotApplicable (not Rejected). If some rows are unguarded → first unguarded wins (or all fail guard checks in sequential order). **Risk**: Unreachable rows (unguarded row before other rows for same state+event). Detected as C25 parse-time warning.

5. **Duplicate named states/events/fields**: Caught at parse time (C6, C7, C9). Name uniqueness enforced at model assembly.

6. **State reachability**: Initial state + graph traversal (StateTransition outcomes only). `from any` expands to all states at parse time. **Risk**: Transitions with `when` guards can make states unreachable despite appearing in edges (guards evaluated at runtime). Analysis assumes guards could pass.

7. **No-transition with mutations**: Same invariant+asserts check as transitions. Mutations don't change state but must satisfy rules for current state. **Risk**: Rules might assume state hasn't changed; guards can't prevent no-transition.

8. **Null value handling in defaults**: Non-nullable scalar fields must have non-null defaults (C17, C19). Event args default to non-null or nullable based on `nullable` keyword. Invariants checked on defaults at compile time (literal evaluation). **Risk**: If default is computed expression (not implemented), uncaught at compile time.

9. **Expression scope in field rules**: Parser enforces field rules reference only declaring field + dotted properties. Top-level rules can reference all fields. Runtime evaluates against full data context. **Risk**: If field rule somehow escapes to global scope, constraint evaluation would see fields it shouldn't.

10. **Constraint target extraction**: `ExpressionSubjects.Extract()` walks AST to find field/arg references for violation targets. Bare names → fields; dotted names → args. **Risk**: If expression uses dotted names on fields (e.g., `Field.count`), misattributed to EventArg. Mitigated by type checker rejecting invalid dotted refs.

### Diagnostic Codes

Format: `PRECEPT###` derived from constraint ID (C1 → PRECEPT001). LanguageConstraint registry at runtime with phase/rule/template/severity. All 54 constraints enumerated with human-readable rules. Severity: Error (parse/compile), Warning (analysis), Hint (dead code).

### Key Files Interdependencies

- `PreceptToken.cs` → defines enum + attributes (reflected by tokenizer & language server)
- `PreceptTokenizer.cs` → builds keyword dict from `TokenSymbolAttribute`
- `PreceptParser.cs` → AST models + expression grammar (Superpower combinators)
- `PreceptModel.cs` → record types (immutable, serializable)
- `PreceptTypeChecker.cs` → walks AST, builds scopes, type-checks
- `PreceptAnalysis.cs` → reachability, dead code, pattern detection
- `PreceptRuntime.cs` → engine with maps, fire/inspect/update, constraint evaluation
- `ConstraintViolation.cs` → target/source hierarchy, expression subject extraction
- `DiagnosticCatalog.cs` → constraint registry (C1–C54)
- `ConstructCatalog.cs` → parser form documentation (used by error messages & language server)
- `PreceptExpressionEvaluator.cs` → expression runtime evaluation (no side effects)

### Language Theory Research (2026-04-04)

Produced five language theory reference documents in `docs/research/language-references/`:

1. **`expression-compactness.md`** — Syntactic sugar and derived forms. Precept already has a solid shorthand inventory (multi-state `from`, `from any`, multi-name declarations). The main gap is error attribution through desugaring. Key finding: any new derived form must desugar before type-checking and reattach source spans for diagnostics. Implementation cost is medium due to span tracking requirements.

2. **`constraint-composition.md`** — Predicate combinators and collect-all semantics. Precept already implements collect-all evaluation correctly. The key gap is named reusable predicates — long `when` guards with repeated sub-conditions appear in nearly every complex sample. A `require Name BoolExpr because "..."` form would be the formal equivalent of Alloy predicates. Scope leakage (event arg references in reusable predicates) is the main risk.

3. **`state-machine-expressiveness.md`** — Statecharts vs. flat state machines. Hierarchical states and parallel regions are high semantic cost and not suited to Precept's domain model. The flat self-contained row model is a feature. Low-cost opportunities: multi-event `on` clause and catch-all `on any` row. Harel (1987) and xstate v5 cited.

4. **`multi-event-shorthand.md`** — Multi-event in the `on` clause. The existing multi-state `from` shorthand establishes the desugaring pattern. Multi-event `on` (no-arg form) is the highest-value near-term addition. Arg substitution semantics for events with shared arg names is the tricky case — requires type-checking each expansion. CSP and symbolic automata provide the formal grounding.

5. **`expression-evaluation.md`** — Principled expression expansion. Precept's current expression set is a decidable many-sorted FOL fragment. Safe additions: `length` accessor for strings, `startsWith`/`endsWith` operators (same grammar level as `contains`), `sum` for numeric collections. Risky additions: `matches` with open regex (ReDoS risk), quantified expressions (high parser cost). The `!= ""` workaround in every string-arg sample is the strongest signal that `length` or `startsWith` would have immediate impact.

Also wrote `docs/research/language-references/README.md` with a relevance ranking table and priority observations for Phase 2.

**Key cross-file finding:** Any parser-level addition requires synchronized updates to: parser → tokenizer (for new keyword tokens) → type-checker → expression evaluator → `ExpressionSubjects.Extract()` (for violation attribution) → `precept_language` MCP tool → language server completions. The chain is longer than it looks.

### Phase 1 Research Contribution (2026-04-04)

Contributed three research documents to the Phase 1 hero research sprint:

- **`expression-compactness.md`** — Syntactic sugar and derived forms. Identified that any new derived form must desugar before type-checking and reattach source spans for diagnostics.
- **`constraint-composition.md`** — Predicate combinators. Identified named reusable predicates as the key gap — long `when` guards with repeated sub-conditions appear in nearly every complex sample.
- **`state-machine-expressiveness.md`** — Statecharts vs. flat state machines. Confirmed flat self-contained row model is a feature; identified multi-event `on` clause as lowest-cost high-value addition.
- **`multi-event-shorthand.md`** — Multi-event in `on` clause as highest-priority near-term addition (LOW COST, HIGH VALUE).
- **`expression-evaluation.md`** — String predicates (`.length`, `startsWith`, `endsWith`) as lowest-risk expression additions. The `!= ""` workaround in every string-arg sample is the strongest signal.

**Cross-phase finding:** Documented the 9-point sync chain required for any parser-level addition — this is the main cost driver for future language work.

### Elaine 5-Surface UX Spec Review (2026-04-04)

Reviewed Elaine's `brand/visual-surfaces-draft.html` for technical accuracy. Key findings recorded in `.squad/decisions/inbox/george-surfaces-review.md`:

**Inspector Panel (confirmed implemented):** `tools/Precept.VsCode/webview/inspector-preview.html` is a complete working webview. Key detail Elaine needs: the inspector event bar has **four** outcome states — `enabled` (green), `noTransition` (green dimmed), `blocked` (red), `undefined` (red dimmed). The `notApplicable` outcome is filtered out of the UI entirely — not shown as yellow warning. If yellow/warning for NotApplicable is wanted, it's a new implementation requirement for Kramer.

**Diagnostic code range:** C1–C54 = PRECEPT001–PRECEPT054. Elaine cited PRECEPT001–PRECEPT047. Off by 7.

**CLI surface:** There is no standalone `precept` CLI tool. PRECEPT diagnostic codes appear only in the VS Code Problems panel (via language server), not in `dotnet build` output. The CLI surface as written is aspirational — describing a future tool that doesn't exist yet.

**Terminal states:** "Terminal" is inferred by `PreceptAnalysis.cs` (states with no outgoing transitions to other states). It's a computed analysis property, not a DSL keyword. `state <Name> initial` is the only lifecycle marker in the grammar.

**Docs terminology:** The team calls it "docs" (internal artifacts in `docs/`). "Docs Site" implies a public website that doesn't exist. Flagged as naming issue.

### InitialState Protocol Fix (2026-04-04)

Implemented Frank's protocol fix from his architecture review of Elaine's visual surfaces spec. Fixed blocking gap that prevented state diagram implementation.

**Changes:**
- Added `InitialState: string` property to `PreceptPreviewSnapshot` record in `tools\Precept.LanguageServer\PreceptPreviewProtocol.cs` (line 33, positional parameter after `CurrentState`)
- Updated `BuildSnapshot()` in `tools\Precept.LanguageServer\PreceptPreviewHandler.cs` (line 296) to populate `InitialState` from `session.Engine.InitialState`
- `PreceptEngine.InitialState` was already available — sourced from `model.InitialState.Name` during compilation

**Build/test status:**
- `dotnet build tools\Precept.LanguageServer\` → succeeded (6.6s)
- `dotnet test test\Precept.LanguageServer.Tests\` → 84/84 passed (1.6s)
- No regressions

**Downstream consumers:**
- **Webview (`tools\Precept.VsCode\webview\inspector-preview.html`)**: Consumes `PreceptPreviewSnapshot` at line 1387 (reads `CurrentState`). `InitialState` now available but not yet used. Diagram renderer not yet implemented — this fix unblocks Kramer's diagram implementation (Frank's blocker resolution).
- **MCP tools (`tools\Precept.Mcp\Tools\`)**: No files reference `PreviewSnapshot` — MCP tools use core engine types directly, not preview protocol. Newman's MCP tools are unaffected.

**Documentation:** Preview protocol is not formally documented in a single design doc. Protocol types in `PreceptPreviewProtocol.cs` serve as implementation documentation. `EditableFieldsDesign.md` and `RulesDesign.md` reference the protocol in context of future work but don't document the full structure.

**Design gate compliance:** Shane approved Frank's review plan. This is a one-field additive protocol change with no DSL semantics, parser, type checker, or runtime execution changes — within charter bounds for non-design-gated work. Fix exactly matches Frank's architectural specification in `.squad/decisions/inbox/frank-surfaces-review.md` § Surface 2 blocker resolution.
