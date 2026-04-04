# Precept Language Spec Brief

**Author:** Steinbrenner (PM)
**Date:** 2026-04-04
**Source documents read:** PreceptLanguageDesign.md, RuntimeApiDesign.md, PreceptLanguageImplementationPlan.md, EditableFieldsDesign.md, RulesDesign.md, ConstraintViolationDesign.md, ConstraintViolationImplementationPlan.md, CatalogInfrastructureDesign.md, SyntaxHighlightingDesign.md, SyntaxHighlightingImplementationPlan.md, McpServerDesign.md, McpServerImplementationPlan.md, CliDesign.md, DiagramLayoutRedesign.md, @ToDo.md. Sample files: loan-application.precept, trafficlight.precept, hiring-pipeline.precept, insurance-claim.precept.

---

## 1. What Precept Is

Precept is a domain integrity engine for .NET — a purpose-built DSL that binds an entity's lifecycle (states), triggers (events), data (fields), and business rules (invariants, asserts, constraints) into a single executable contract expressed as a `.precept` file. The core contract is this: if the file compiles, the workflow is internally consistent; if `Fire()` or `Update()` succeeds, the resulting state is provably valid. Invalid states are not just discouraged — the runtime structurally prevents them. The DSL is flat, keyword-anchored, and indentation-free by design, making it reliably parseable by both humans and AI agents. The complete toolchain spans a C# runtime (NuGet), a VS Code extension (editor), a Copilot agent plugin (MCP server + skills), and 21 canonical sample workflows.

---

## 2. The DSL Surface

### Constructs — What the Language Can Express Today

**Header**
- `precept <Name>` — required first non-comment line; declares the workflow identity

**Fields**
- `field <Name> as <Type> [nullable] [default <Literal>]` — scalar field (string/number/boolean)
- `field <Name>, <Name>, ... as <Type> ...` — multi-name field shorthand (shared type/default)
- Collection fields: `field <Name> as set of <Scalar>` / `queue of <Scalar>` / `stack of <Scalar>`
- Collection defaults: `default ["a", "b"]` list literal form

**Invariants**
- `invariant <BoolExpr> because "<Reason>"` — global data constraint; always holds; checked post-commit

**States**
- `state <Name> [initial]` — single state declaration; exactly one `initial` required
- `state <Name>, <Name>, ...` — multi-name form; `initial` may appear on any entry

**State Asserts** (checked post-mutation, pre-commit)
- `in <StateTarget> assert <BoolExpr> because "<Reason>"` — holds while residing in state (entry + in-place)
- `to <StateTarget> assert <BoolExpr> because "<Reason>"` — holds only on crossing *into* state
- `from <StateTarget> assert <BoolExpr> because "<Reason>"` — holds only on crossing *out of* state
- `<StateTarget>` = single state, comma-separated states, or `any`

**State Entry/Exit Actions** (automatic mutations, no event args)
- `to <StateTarget> -> <ActionChain>` — fires on crossing into state
- `from <StateTarget> -> <ActionChain>` (action, not assert) — fires on crossing out of state

**Editable Fields**
- `in <StateTarget> edit <Field>, <Field>, ...` — declares fields mutable via `Update()` while in state; multiple declarations are unioned per state

**Events**
- `event <Name>` — bare event (no args)
- `event <Name> with <ArgList>` — event with typed argument contract
- `event <Name>, <Name>, ...` — multi-name form (shared args when `with` is present)
- Arg form: `<ArgName> as <Type> [nullable] [default <Literal>]`

**Event Asserts**
- `on <EventName> assert <BoolExpr> because "<Reason>"` — pre-transition, arg-only scope; multiple allowed

**Transition Rows**
- `from <StateTarget> on <EventName> [when <BoolExpr>] [-> <Action>]* -> <Outcome>` — flat, self-contained row
- `<Outcome>` = `transition <State>` | `no transition` | `reject "<Reason>"`
- First-match evaluation top-to-bottom for same `(state, event)` pair

**Action Vocabulary**
- `set <Field> = <Expr>` — scalar assignment
- `add <SetField> <Expr>` — set collection add
- `remove <SetField> <Expr>` — set collection remove
- `enqueue <QueueField> <Expr>` — queue enqueue
- `dequeue <QueueField> [into <Field>]` — queue dequeue
- `push <StackField> <Expr>` — stack push
- `pop <StackField> [into <Field>]` — stack pop
- `clear <CollectionField>` — collection clear

**Expressions**
- Arithmetic: `+`, `-`, `*`, `/`, `%`
- Logical: `&&`, `||`, `!`
- Comparison: `==`, `!=`, `<`, `<=`, `>`, `>=`
- Membership: `contains`
- Parentheses, unary negation
- Identifier access: bare name (field/arg), dotted `EventName.ArgName` (required in transition rows)
- Collection accessors: `.count`, `.min`, `.max` (set), `.peek` (queue/stack)

**Comments**
- `#` lines and inline `#` end-of-line comments

### Type System (Locked)

| Scalar | Collection |
|--------|-----------|
| `string` | `set of <Scalar>` |
| `number` | `queue of <Scalar>` |
| `boolean` | `stack of <Scalar>` |

Nullability: explicit `nullable` keyword. Non-nullable scalars require `default`. Collection fields default to empty.

### Compile-Time Static Analysis

The compiler performs full expression-level type checking (StaticValueKind: string, number, boolean, null, unions). Scope enforcement: bare arg names invalid in transition rows (must use dotted form). Diagnostic severity: **Error** (blocks compilation), **Warning** (structural concern), **Hint** (informational).

Graph-level diagnostics (C48–C53):
- C48: Unreachable state (Warning)
- C49: Orphaned event (Warning)
- C50: Dead-end state (Hint)
- C51: Reject-only (state, event) pair (Warning)
- C52: Event never succeeds from any reachable state (Warning)
- C53: Empty precept (Hint)

**Designed but not yet implemented:**
- Same-preposition contradiction detection (two asserts, same preposition, same state, provably empty per-field domains) — requires interval/set analysis on expression ASTs
- Cross-preposition deadlock detection (`in`/`to` vs `from` asserts making state provably unexitable)

### Rules System (note: RulesDesign.md describes an earlier syntax)

RulesDesign.md uses indented `rule <Expr> "<Reason>"` syntax and type-prefix field declarations (`number Balance = 0`). The current parser uses the flat `invariant`/`assert` paradigm from PreceptLanguageDesign.md. The "rules" as a named construct with `rule` keyword do not appear in any current sample file — the design has been subsumed into `invariant` and `assert`. This is a documentation/naming discrepancy that needs PM attention (see § 7 below).

---

## 3. The Runtime API

Three-step pipeline: **Parse → Compile → Create Instance → Fire/Inspect/Update**

### `PreceptParser`
- `Parse(text)` → `PreceptDefinition` (throws on error)
- `ParseWithDiagnostics(text)` → `(PreceptDefinition?, IReadOnlyList<ParseDiagnostic>)` (tooling-safe)

### `PreceptCompiler`
- `Compile(model)` → `PreceptEngine` (throws on semantic error; validates state/event/field references and literal `set` assignments against rules)
- `CompileFromText(text)` → composed pipeline returning structured `ValidationResult` with diagnostics

### `PreceptEngine` (immutable, thread-safe)
Properties: `Name`, `States`, `InitialState`, `Events`, `Fields`, `CollectionFields`

| Method | What it does |
|--------|-------------|
| `CreateInstance([initialState], [data])` | Creates a new workflow instance; merges data with declared defaults |
| `Inspect(instance)` | Aggregate: evaluates all events in discovery mode; returns state, data, per-event outcomes, editable fields |
| `Inspect(instance, eventName, [args])` | Per-event: non-mutating evaluation; returns outcome, target state, required arg keys, violations |
| `Inspect(instance, patch)` | Hypothetical patch: simulates field update, returns violations without committing |
| `Fire(instance, eventName, [args])` | Mutating event execution; 9-stage pipeline with full rollback on failure; returns `FireResult` |
| `Update(instance, patch)` | Atomically updates editable fields; enforces editability, type check, invariants/asserts; returns `UpdateResult` |
| `CheckCompatibility(instance)` | Validates externally loaded instance against current engine (schema evolution safety) |
| `CoerceEventArguments(eventName, args)` | Coerces JSON/string arg values to declared scalar types; never throws |

### Result Types

| Type | Outcome enum values |
|------|-------------------|
| `FireResult` | `Transition`, `NoTransition`, `Rejected`, `ConstraintFailure`, `Unmatched`, `Undefined` |
| `UpdateResult` | `Update`, `ConstraintFailure`, `UneditableField`, `InvalidInput` |
| `EventInspectionResult` | Same `TransitionOutcome` values |

`ConstraintViolation` carries: `Message`, `ConstraintTargetKind` (Field/EventArg/Event/State/Definition), `ConstraintSourceKind` (Invariant/StateAssertion/EventAssertion/Rejection), `ExpressionSubjects`, structured targets list.

### Model Types (Parse Tree)
`PreceptDefinition`, `PreceptState`, `PreceptEvent`, `PreceptEventArg`, `PreceptField`, `PreceptCollectionField`, `PreceptInvariant`, `StateAssertion`, `EventAssertion`, `PreceptTransitionRow`, `StateTransition`, `Rejection`, `NoTransition`, `PreceptEditBlock`

---

## 4. Feature Status Table

| Feature | Status | Design Doc | Notes |
|---------|--------|-----------|-------|
| Core DSL (fields, states, events, transitions) | **Shipped** | PreceptLanguageDesign.md | Fully implemented, 21 sample files |
| Invariants (`invariant … because`) | **Shipped** | PreceptLanguageDesign.md | Checked post-commit, all scopes |
| State asserts (`in`/`to`/`from … assert`) | **Shipped** | PreceptLanguageDesign.md | All three anchor forms |
| State entry/exit actions (`to`/`from … ->`) | **Shipped** | PreceptLanguageDesign.md | Automatic mutations on state change |
| Editable fields (`in … edit`) | **Shipped** | EditableFieldsDesign.md | `Update()` API + inspect integration |
| Event asserts (`on … assert`) | **Shipped** | PreceptLanguageDesign.md | Arg-only scope enforced |
| Collection fields (set/queue/stack) | **Shipped** | PreceptLanguageDesign.md | Full mutation vocabulary |
| Multi-name field/state/event declarations | **Shipped** | PreceptLanguageDesign.md | Comma-separated forms |
| `any` keyword expansion | **Shipped** | PreceptLanguageDesign.md | Expands to all declared states |
| Compile-time type checking | **Shipped** | PreceptLanguageDesign.md | StaticValueKind system, phases D-F |
| Scope enforcement (dotted event args) | **Shipped** | PreceptLanguageDesign.md | Phase E |
| Structured `ConstraintViolation` | **Shipped** | ConstraintViolationDesign.md | Replaces flat string `Reasons` lists |
| `Rejected` vs `ConstraintFailure` split | **Shipped** | ConstraintViolationDesign.md | Distinct outcome kinds |
| Graph analysis diagnostics (C48-C53) | **Shipped** | PreceptLanguageDesign.md § Graph | Phase I |
| Naming rename (CV Phases 0-3) | **Shipped** | ConstraintViolationImplementationPlan.md | Clean API names |
| Catalog infrastructure (3 tiers) | **Shipped** | CatalogInfrastructureDesign.md | TokenCategory, ConstructCatalog, DiagnosticCatalog |
| MCP 5-tool surface | **Shipped** | McpServerDesign.md | language, compile, inspect, fire, update |
| Copilot agent plugin + skills | **Shipped** | McpServerImplementationPlan.md Phases 7-8 | Claude format, precept-author agent + 2 skills |
| VS Code extension (syntax, preview) | **Shipped** | SyntaxHighlightingDesign.md | TextMate + semantic tokens (generic types) |
| Diagram layout (ELK fix) | **Shipped** | DiagramLayoutRedesign.md | Option B implemented |
| Syntax highlighting custom palette (8-shade) | **Not Started** | SyntaxHighlightingImplementationPlan.md | Phase 0 started in plan, all phases unchecked |
| `preceptConstrained` italic modifier | **Designed** | SyntaxHighlightingImplementationPlan.md Phase 7 | Explicitly deferred |
| TextMate grammar scope split | **Designed** | SyntaxHighlightingImplementationPlan.md Phase 8 | Explicitly deferred |
| CLI (`smcli`) | **Designed** | CliDesign.md | Not started; uses stale `Dsl*` naming (pre-rename) |
| Structured violations in preview protocol | **Designed** | @ToDo.md "Later" | `PreceptPreviewEventStatus.Reasons` still flat strings |
| Same-preposition contradiction detection | **Designed** | PreceptLanguageDesign.md § Checks #4 | Requires interval/set analysis on ASTs |
| Cross-preposition deadlock detection | **Designed** | PreceptLanguageDesign.md § Checks #5 | Requires interval + reachability reasoning |
| Fluent interface | **Planned** | @ToDo.md "Later" | No design doc |
| Plugin distribution (marketplace publish) | **Partial** | McpServerImplementationPlan.md Phase 9 | Docs updated; actual publish pending |
| Sample integration tests (CLI-driven) | **Planned** | @ToDo.md "Later" | Deferred until CLI exists |
| Passing one precept as event argument | **Idea** | @ToDo.md "Ideas" | No design, no timeline |

---

## 5. Open Language Questions

1. **RulesDesign.md vs current DSL:** RulesDesign.md specifies a `rule` keyword with indented syntax and type-prefix field declarations. The current language uses `invariant` and `assert` with flat syntax. It is unclear whether `rule` was a discarded design name for `invariant`/`assert`, or whether the `rule` keyword itself was ever parsed. The design doc says "Implemented" but no sample file uses `rule`. Needs explicit resolution in docs.

2. **Same-preposition contradiction detection (Check #4):** Flagged as a compile-time error in the design spec but not implemented. Requires per-field interval/set analysis. No timeline in the roadmap.

3. **Cross-preposition deadlock detection (Check #5):** Same status as Check #4. Flagged as compile-time error, unimplemented.

4. **`EventAssert` ordering relative to `EventDecl`:** The spec says "whether EventAssert must appear after its EventDecl is not locked yet." This is an open grammar order question. Currently informally recommended but not enforced.

5. **Collection defaults list literal exact rules:** The spec says `default ["a", "b"]` is supported but notes "exact literal rules TBD." This is an unresolved detail.

6. **CLI vs MCP overlap:** The @ToDo.md asks "Decide whether a standalone CLI is still needed if MCP already covers the same workflows." This is an unresolved architectural decision.

7. **`rule` keyword in `RulesDesign.md`:** The design describes field rules, top-level rules, state rules, and event rules using an indented `rule` keyword. The current PreceptLanguageDesign.md does not mention this keyword. The relationship between the two needs documentation.

8. **Structured violations in preview protocol:** The webview `PreceptPreviewEventStatus.Reasons` is still `IReadOnlyList<string>`. Carrying full `ConstraintViolation` data to the webview is designed but deferred — meaning the preview UI is inconsistent with the runtime's structured violation model.

---

## 6. Roadmap Assessment

### Clearly Done (Phase 4c complete)
The core language runtime is feature-complete: all DSL constructs parse, type-check, and execute. The constraint violation model (structured `ConstraintViolation`, `Rejected`/`ConstraintFailure` split) is fully implemented and tested. The MCP 5-tool surface is live. The Copilot agent plugin and skills are drafted and validated. Graph analysis diagnostics are in place. 666 tests pass across three test projects.

### Actively In Progress (or immediately queued)
- **Syntax highlighting palette (8 shades):** Design is locked, implementation plan is written with 8 phases, zero phases checked. This is the most concrete ready-to-execute work item. The `TokenCategory.Grammar` refactor (Phase 0) is a standalone non-breaking change.
- **Plugin distribution (Phase 9):** README and design docs updated, MCP provider removed from extension. The actual marketplace publish is the remaining step.

### Deferred but Designed
- **Custom syntax highlighting** (full 8-phase plan): Phases 0-6 are straightforward, Phases 7-8 explicitly deferred for post-v1.
- **CLI (`smcli`):** Full design exists. Deferred pending the CLI-vs-MCP decision. Note: the design doc uses stale `Dsl*` naming (pre Phase 0-3 renames) — needs an audit pass before implementation.
- **Structured violations in preview protocol:** Designed, deferred. Low risk to defer; medium effort to implement.
- **Same-preposition contradiction / cross-preposition deadlock:** Designed in spec, unimplemented. Requires non-trivial interval analysis. No owner, no timeline.

### Speculative / Aspirational
- Fluent interface for `engine.CreateInstance` — mentioned in "Later" with no design doc
- Passing a precept as an event argument — "Ideas" section, no design
- Sample integration tests driven through CLI — blocked on CLI existing

### What's Blocking Release
**Nothing is blocking a v1 release of the core runtime + MCP + extension.** The core feature set is complete and tested. What's missing for a quality release:
1. Syntax highlighting palette (the extension currently relies on theme defaults — the flagship visual identity is undelivered)
2. Marketplace publish for the plugin (distribution)
3. CLI (useful but optional given MCP coverage)

---

## 7. PM Concerns

**Concern 1 — Syntax highlighting is the ship blocker they're not calling a ship blocker.**
The 8-shade palette is a first-class design artifact with its own locked brand spec, design doc, and 8-phase implementation plan. Zero phases are checked. The extension currently renders `.precept` files with whatever the active VS Code theme decides — completely defeating the "color encodes meaning" value proposition. This should be treated as a release gate, not a backlog item. Phase 0 (TokenCategory.Grammar refactor) is a 1-day task with no risk. Phases 1-6 are mechanical. Start this now.

**Concern 2 — RulesDesign.md is a documentation liability.**
The doc says "Status: Implemented" and describes a `rule` keyword that doesn't exist in the current language surface. Either: (a) `rule` was an early name that got renamed to `invariant`/`assert` and the doc was never updated, or (b) the feature was partially implemented and then the syntax changed. Either way, this doc is actively misleading anyone who reads it — including AI agents writing `.precept` files. Fix it or archive it.

**Concern 3 — The CLI decision has been deferred too long.**
The @ToDo.md asks whether a CLI is still needed given MCP. The design exists; the MCP is live. Someone needs to make this call: kill the CLI design, or implement it. Letting it sit as an open "maybe" consumes mental overhead every time the roadmap is reviewed and leaves 20+ potential sample integration tests unwritten.

**Concern 4 — Preview protocol inconsistency.**
The runtime returns rich structured `ConstraintViolation` objects. The preview webview still receives flat strings. This means the webview can't do field-level inline highlighting — a capability the runtime has been ready to provide since the constraint violation redesign. It's not a blocker, but it's a visible UX gap in the flagship developer surface (the preview panel).

**Concern 5 — Contradiction/deadlock detection is in the spec as compile errors, not implemented.**
PreceptLanguageDesign.md § State Asserts lists same-preposition contradiction (Check #4) and cross-preposition deadlock (Check #5) as **compile-time errors**. They are not implemented. This means the spec says something will be an error at compile time, but it isn't. That's a spec/implementation divergence. Either implement them (interval analysis work) or downgrade them in the spec to "future work." The current state is a false promise.

**What should ship next (priority order):**
1. **Syntax highlighting Phases 0-6** — 3-5 days, high visibility, enables the README hero example to look correct
2. **Marketplace publish (plugin + extension)** — distribution; can't claim "available on Claude Marketplace" without it
3. **Fix RulesDesign.md** — documentation debt, 1 hour, any agent can do it
4. **Preview protocol structured violations** — medium effort, unlocks richer inspector UI
5. **CLI-or-kill decision** — gate on whether to implement or archive
