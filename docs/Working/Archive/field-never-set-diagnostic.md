---
status: Promoted 2026-05-27 — implemented in commits `0d61f792`, `12d7c422`, `0feb135a`, `2ac416b5` (Phase 5 W-B); reachability gating added in commit `11599944` (post-Phase-5 Slice 5). Canonical content lives in `docs/language/precept-language-spec.md § 2.2` (editable as the canonical write modifier; `writable` retired), `docs/language/catalog-system.md § Modifier Catalog` (`ModifierKind.Writable` and `TokenKind.Writable` retired), `docs/compiler/graph-analyzer.md § 6.7` (FieldNeverSet write-site enumeration + reachability gate), `docs/compiler/diagnostic-system.md § PRE0150` (FieldNeverSet). This archive retains the four-leg rationale and the Stage-1 comparator survey for future audit reference. Original Locked history: v4 Locked 2026-05-26 — re-Locked after Stage-1 comparator survey landed at `research/language/expressiveness/access-modifier-keyword-unification.md`. Survey verdict: 6 of 8 surveyed languages — TypeScript, Kotlin, Swift, C#, Java, Scala — share the cross-position unified-keyword pattern; F# partial (let mutable is a keyword pair); Rust documented exception (Rust Book verbatim: "Rust doesn't allow us to mark only certain fields as mutable"). Decision 5 framing tightened from "every comparable language uses one keyword" to "dominant industry pattern (6 of 8 surveyed) with one documented exception (Rust)".
phase-target: Phase 5 (Proof engine satisfiability — dead-code family); access-modifier unification co-ships
comparable-systems-research-status: strong — Stage-1 research artifact at `research/language/expressiveness/access-modifier-keyword-unification.md` (339 lines, 14 Primary sources fetched 2026-05-26) grounds Decision 5's irreversible writable→editable keyword retirement.
supersedes: v1 (2026-05-25 morning), v2 (2026-05-25 evening — Locked without Phase 4 leg structure)
sources-consulted:
  - research/language/expressiveness/access-modifier-keyword-unification.md — Stage-1 cross-position keyword precedent survey; 6/8 strong (TypeScript, Kotlin, Swift, C#, Java, Scala) + 1/8 partial (F# let mutable) + 1/8 documented exception (Rust at struct-field position) across 14 Primary sources fetched 2026-05-26
  - docs/Working/Archive/field-state-guarantees-v2.md — originating design (v1) — Layer 1 / Layer 2 composition model
  - docs/Working/frank-review-field-never-set-writable-vs-editable.md — Frank's architectural review v1; B1 finding (both editable sites should be treated as write sites)
  - docs/language/precept-language-spec.md § 2.2 (lines ~996-1017) — current `writable`/`editable` keyword definitions; redundancy rules
  - docs/language/precept-language-spec.md (lines 1647-1651) — existing `WritableOnEventArg` diagnostic spec
  - docs/language/catalog-system.md § Modifier Catalog — current `ValueModifierMeta` vs `AccessModifierMeta` distinction
  - samples/prescription-refill-request.precept — original `Priority` field declaration; would trigger `FieldNeverSet`
  - src/Precept/Language/DiagnosticCode.cs — current top ordinal `McpToolInternalError = 149`
  - src/Precept/Language/Diagnostics.cs (lines 700-754) — 8 existing graph-stage dead-code diagnostics (precedent for Severity = Warning)
  - src/Precept/Language/Modifiers.cs (line 244) — current `ModifierKind.Writable` arm (to be retired)
  - src/Precept/Language/Tokens.cs (line 113) — current `TokenKind.Writable` entry (to be retired)
  - TypeScript Handbook § Readonly Properties (typescriptlang.org/docs/handbook/2/objects.html) — `readonly` keyword usage at interface fields, class properties, mapped types
  - Kotlin Reference § Properties (kotlinlang.org/docs/properties.html) — `val` / `var` keyword usage at property declarations and local variable bindings
  - Rust Reference § Patterns and § Variables (doc.rust-lang.org/reference) — `mut` keyword usage in pattern bindings, function parameters, and reference types
research-status:
  external-engagement: strong (resolved 2026-05-26). The Stage-1 research artifact at `research/language/expressiveness/access-modifier-keyword-unification.md` surveys 8 comparator languages with verbatim Primary-source excerpts. 6 of 8 (TypeScript readonly, Kotlin val/var, Swift let/var, C# readonly, Java final, Scala val/var) share the cross-position unified-keyword pattern. F# (let mutable) is partial — same direction but keyword pair rather than single shared keyword. Rust is the documented exception per the Rust Book: *"Rust doesn't allow us to mark only certain fields as mutable"* — confirms Phase-8 retrofit's precision-issue flag was correct. The original v2 claim "every comparable language uses one keyword" is replaced by the more accurate "dominant industry pattern (6 of 8 surveyed) with one documented exception".
  prior-step-resolved: 2026-05-26 — full comparator survey landed; Decision 5's precedent leg re-grounded on the survey's 14 Primary sources.
---

# `FieldNeverSet` Diagnostic + Access-Modifier Unification

## Goal

When done, two coupled outcomes ship together:

1. The compiler emits a `FieldNeverSet` Warning naming any field that has no write site in the precept — no construction event arg, no value-establishing transition action, no caller-side write capability (`editable` at field declaration or in a state-scoped `modify` row), no computed `<-` expression. Such a field can only hold its declared default (or remain unset for `optional`), so any rule, ensure, or consumer-side read sees a constant value. `samples/prescription-refill-request.precept`'s original `Priority` declaration would have been caught.
2. The `writable` / `editable` keyword split is unified. `writable` is recategorized from `ValueModifierMeta` to `AccessModifierMeta`, renamed to `editable`, and the field-level Access Modifier site is added. The redundancy machinery, D3 default, and Layer-1/Layer-2 composition model are preserved — only the keyword and catalog category change. This unification is co-shipped because the FieldNeverSet analyzer's catalog-driven write-capability check depends on a single Access-Modifier category being authoritative.

## Scope

**In scope:**
- One new diagnostic: `FieldNeverSet`
  - Stage: `DiagnosticStage.Graph`
  - Category: `DiagnosticCategory.Structure`
  - Severity: `Warning`
- A new graph-analyzer sub-pass aggregating, per field declaration, every write site discoverable in the type-checked program
- Catalog property additions:
  - `ActionMeta.WriteSemantics` (new) — discriminator of `None | EstablishesValue | MutatesContents | ClearsContents`; only `EstablishesValue` suppresses `FieldNeverSet`
  - `ValueModifierMeta.IsWritable` (or equivalent) eliminated when `ModifierKind.Writable` retires; the field-declaration site of `ModifierKind.Write` (the renamed access modifier) carries the writable semantics from then on
- **Access-modifier unification (co-shipped surface change):**
  - Retire `ModifierKind.Writable` from `ValueModifierMeta`
  - Add `AccessModifierMeta.ApplicableDeclarationSites` to allow `Write` (= `editable`) at field-declaration position AND state-scoped `modify` rows
  - Rename keyword: field declarations write `editable` where they previously wrote `writable`
  - Eliminate the keyword `writable` from the language surface
  - All redundancy rules, D3 read-only default, Layer 1 / Layer 2 composition model preserved
- Test coverage for every write-site shape (Create arg, scalar `set`, value-establishing collection actions, `put`, field-level `editable`, per-state `modify F editable`, state-entry hook, computed `<-`)
- Corpus sweep: every sample tripping `FieldNeverSet` is fixed before merge; every sample using `writable` is renamed to `editable` in the same PR
- Pre-existing catalog-count drift (148 → 149 in `catalog-system.md`) is corrected in the same PR

**Out of scope:**
- `FieldNeverRead` — set-but-never-read detection. Ambiguous because Precept fields flow to consumers regardless of internal governance. Deferred to a separate design.
- Suppression annotations (e.g., `# ungoverned`). Not needed: `FieldNeverSet` has no false-positive path that requires opt-out — a field with no write site truly is stuck at its default.
- Vacuous-rule / tautological-guard detection — separate work (readiness-plan F-LANG-SPEC-04, -05).
- Strict-mode escalation policy (warn → error). Operational decision, separate from catalog design.

**Deferred to future:**
- `FieldNeverRead` (separate design, broader scope)
- "Dead default" detection (field with a default that's also always the assigned value)
- Whole-corpus dead-code reports / batch tooling

## Inventory of what will be built

**Catalog property additions:**

- `src/Precept/Language/Action.cs` (or `ActionMeta.cs`) — add `WriteSemantics: ActionWriteSemantics` property to `ActionMeta`
- `src/Precept/Language/ActionWriteSemantics.cs` (new file) — discriminator enum: `None | EstablishesValue | MutatesContents | ClearsContents`. Classification table:
  - `EstablishesValue`: `set`, `put`, `add`, `enqueue`, `append`, `insert`
  - `MutatesContents`: (reserved — no current actions; future structural mutations land here)
  - `ClearsContents`: `clear`, `remove`, `removeAt`, `dequeue`, `pop`
  - `None`: actions that don't write to a field at all (e.g., `reject`, `transition`, `no transition`)
- `src/Precept/Language/Actions.cs` — populate `WriteSemantics` on every `ActionMeta` entry
- `src/Precept/Language/AccessModifierMeta.cs` (or `Modifiers.cs`) — add `ApplicableDeclarationSites` to `AccessModifierMeta` (mirroring the existing `ValueModifierMeta.ApplicableDeclarationSites` field). `Write` access modifier becomes valid at BOTH `FieldDeclaration` and `ModifyRow` sites.

**Access-modifier unification (catalog refactor):**

- `src/Precept/Language/ModifierKind.cs` — remove `Writable = 15`
- `src/Precept/Language/Modifiers.cs` — remove the `ModifierKind.Writable` arm of `GetMeta`; update the `Write` arm to include field-declaration applicability
- `src/Precept/Language/TokenKind.cs` — remove `TokenKind.Writable`
- `src/Precept/Language/Tokens.cs` — remove the `Writable` token entry
- `src/Precept/Language/Lexer.cs` — remove keyword recognition for `writable`
- `src/Precept/Pipeline/Parser.*.cs` — update field-declaration modifier parsing to accept `editable` at the field-modifier position
- `src/Precept/Language/DiagnosticCode.cs` — `WritableOnEventArg` becomes `EditableOnEventArg`; rename and update message template
- Generated grammar: `tmLanguage.json` regenerates from the catalog (no hand edit)

**New diagnostic:**

- `src/Precept/Language/DiagnosticCode.cs` — add `FieldNeverSet` at next available ordinal (current top is `McpToolInternalError = 149`, so `FieldNeverSet = 150`)
- `src/Precept/Language/Diagnostics.cs` — add `GetMeta` entry: `Severity.Warning`, `DiagnosticStage.Graph`, `DiagnosticCategory.Structure`, message `"Field '{0}' has no write site — it can only hold its declared default or remain unset"`, full FixHint/TriggerCondition/RecoverySteps/ExampleBefore/ExampleAfter, `RelatedCodes: [DiagnosticCode.UnreachableState, DiagnosticCode.UnhandledEvent]`

**New analyzer:**

- `src/Precept/Pipeline/GraphAnalyzer.FieldWriteSites.cs` (new partial-class extension of `GraphAnalyzer`, following the existing convention used by `Parser.Actions.cs`, `Parser.Expressions.cs`, `ProofEngine.Strategies.cs`) — `AnalyzeFieldWriteSites` method. Walks the type-checked program:
  - Collects all `set` / `put` / `add` / `enqueue` / `append` / `insert` actions (filtered via `ActionMeta.WriteSemantics == EstablishesValue` — catalog-driven, not hardcoded)
  - Excludes `clear` / `remove` / `removeAt` / `dequeue` / `pop` (catalog reports `ClearsContents` — these don't establish a value)
  - Collects field-level Access Modifier presence: any field with `ModifierKind.Write` (the unified `editable`) at the field-declaration site
  - Collects per-state Access Modifier rows: any `in <State> modify F <mode>` row where the mode's `AccessModifierMeta.IsWritable == true` (catalog-driven check, not keyword-hardcoded)
  - Collects state-entry hooks targeting the field
  - Collects computed-field declarations (`field X as T <- expr`) — implicit write
  - Collects construction-event arg assignments (`-> set X = Create.X` patterns)
  - Emits `FieldNeverSet` for any field whose write-site collection is empty

**Pipeline wiring:**

- `src/Precept/Pipeline/GraphAnalyzer.cs` — call `AnalyzeFieldWriteSites(ctx)` after reachability and completeness passes, before serialization to `Compilation.Diagnostics`

**Allow-list registration:**

- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` — register `FieldNeverSet` against `Gate2` (cross-project test detection gap — matches the pattern used by `DeadEndState`, `UnreachableState`, etc.); do NOT add to any "unwired" list

**Tests:**

- `test/Precept.Tests/GraphAnalyzer/FieldWriteSiteAnalyzerTests.cs` — unit tests for the write-site aggregator covering every shape
- `test/Precept.Tests/GraphAnalyzer/FieldNeverSetEmissionTests.cs` — diagnostic emission tests:
  - Positive: field declared with default, no write site → warning emitted
  - Negative (parameterized over every write-site shape): each shape → no warning
  - Edge: optional field with no default, no writes → warning emitted
  - Negative: `clear`-only / `remove`-only writes → warning STILL emitted (these don't establish a value)
- `test/Precept.Tests/Language/ActionMetaTests.cs` — verify `WriteSemantics` populated for every `ActionMeta` entry
- `test/Precept.Tests/Language/AccessModifierMetaTests.cs` — verify `Write` (editable) is valid at both field-declaration and modify-row sites; `Writable` modifier no longer exists
- Corpus regression: `SampleCompilesCleanTests` continues to pass after the unification renames every `writable` → `editable` and the diagnostic emits on remaining dead fields

**Sample corpus sweep:**

- Every sample using `writable` is renamed to `editable` (mechanical rename in the same PR)
- Every sample tripping `FieldNeverSet` is fixed (wire into governance, make computed, or delete) — enumerated by running the new analyzer at implementation time

**Pre-existing catalog-count drift:**

- `docs/language/catalog-system.md` lines 290, 716, 1989, 1993 currently state "148" but `DiagnosticCode.cs` already has 149 active codes (top ordinal `McpToolInternalError = 149`). Update those four sites to reflect the post-this-PR count of **150** (existing 149 + new `FieldNeverSet`).
- `docs/Working/compiler-readiness-review-2026-05-24.md` also says 148; update.

## Decisions

### Decision 1: Ship `FieldNeverSet` only; defer `FieldNeverRead` to separate design

**Stakes**: medium — scope decision; `FieldNeverRead` can be added later as a separate diagnostic with its own ordinal. Reversible by future design work; doesn't lock public surface beyond reserving the name `FieldNeverSet`.

- **Rationale**: `FieldNeverSet` and `FieldNeverRead` have different confidence and design surfaces. `FieldNeverSet` is unambiguous — a field with no write site can only hold its default, representing an authoring oversight. `FieldNeverRead` is ambiguous — Precept fields flow to consumers regardless of internal governance, so set-but-never-read isn't necessarily a defect. Shipping together forces a single severity / suppression policy across two cases that warrant different treatment.
- **Alternatives considered**:
  - *Ship both together.* Rejected — `FieldNeverRead` requires a suppression mechanism and a stable "read" definition (does interpolation count? does `is set` count? does `modify editable` count as a read by the caller?). Separate design conversation that should not block the unambiguous half.
  - *Single merged `DeadField` diagnostic with varying severity.* Rejected — forces the catalog out of its self-describing convention (one stable trigger / severity per `DiagnosticCode`).
- **Precedent**: The graph dead-code suite splits related concerns into distinct diagnostics: `UnreachableState`, `DeadEndState`, `StructuralSinkState`, `AlwaysRejecting` — all "state/event graph dead code" but each carries a distinct trigger and recovery path.
- **Tradeoff accepted**: Set-but-never-read fields (the production-order Priority pattern) stay uncaught until the deferred design ships. Acceptable — the author can still notice via review; the structurally worse case (declared-but-never-set) is the one that flies under review radar.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Diagnostics.cs` (lines 700-754) — *"`UnreachableState`, `DeadEndState`, `StructuralSinkState`, `AlwaysRejecting`"* — 8 graph-stage dead-code diagnostics confirm the "one diagnostic per unambiguous trigger" precedent.
  - `samples/prescription-refill-request.precept` — original `Priority` field declaration; would trigger `FieldNeverSet`. Concrete in-corpus motivating example.

### Decision 2: Severity = Warning, not Error

**Stakes**: medium — severity is reversible at the catalog level (`DiagnosticMeta.Severity` is one property of one entry). Escalation to Error possible later via CI policy or strict-mode gating without language-surface change.

- **Rationale**: A field with no write site is recoverable — the author can wire it up, make it computed, or delete it. Compilation can continue, the precept can still run. Erroring would block precepts that are otherwise structurally sound and may be in mid-development. Warning surfaces the issue without blocking; CI policy or `/lifecycle-6-review --strict` can elevate at promotion time.
- **Alternatives considered**:
  - *Error.* Rejected — too disruptive. Authors frequently declare fields before wiring them; erroring would force premature scaffolding. The 8 existing graph-level dead-code diagnostics reserve Error for structural integrity violations (`TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`) and use Warning for the "potentially intentional, definitely surface-able" category — `FieldNeverSet` is squarely in the warning camp.
  - *Info.* Rejected — too quiet. Warnings are the established "you should look at this" level.
- **Precedent**: `UnreachableState` (PRE0080), `UnhandledEvent` (PRE0081), `DeadEndState`, `AlwaysRejecting` all ship as Warning. `FieldNeverSet` is the field-level analog and inherits the same disposition.
- **Tradeoff accepted**: Warnings can be ignored; some authors will not address them. Mitigated by IDE squiggles via the language server and eventual `/lifecycle-6-review --strict` gating. Policy escalation is a separate downstream concern.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Diagnostics.cs` (lines 700-754) — *graph-stage dead-code suite*: `UnreachableState`, `UnhandledEvent`, `DeadEndState`, `AlwaysRejecting` all carry `Severity.Warning`; `TerminalStateHasOutgoingEdges` and `IrreversibleStateHasBackEdge` carry `Severity.Error`. The Warning/Error partition correlates with "recoverable authoring issue" vs "structural integrity violation."

### Decision 3: Analyzer lives in the Graph stage as a partial-class extension of `GraphAnalyzer`

**Stakes**: medium — internal architecture choice. Could be refactored to a different stage or to a dedicated stage later without surface impact. Reversible.

- **Rationale**: Field-write-site analysis is a whole-program, post-type-check pass — the same shape as the existing graph analyzer's reachability and completeness passes. It walks the type-checked AST globally and emits Structure-category diagnostics. The graph analyzer is the natural home.
- **Alternatives considered**:
  - *Add to the type checker.* Rejected — type checking is statement-local in shape, even though `TypeChecker.Validation.FieldState.cs` does some cross-row work (initial-event-rooted action-chain enumeration for `InitialEventMissingAssignments`). Whole-program field-write-site analysis matches the graph stage's BFS-over-everything shape more cleanly than the type checker's initial-event-rooted machinery.
  - *Create a new dedicated pipeline stage.* Rejected — adds a stage for one diagnostic. The graph analyzer's existing sub-pass structure accommodates field analysis cleanly.
  - *Runtime check.* Rejected — too late; the whole point is compile-time detection.
  - *Separate file (`GraphAnalyzer/` subfolder).* Rejected — the existing layout is flat with partial-class splits (`Parser.Actions.cs`, `Parser.Expressions.cs`, `ProofEngine.Strategies.cs`). Use the established `GraphAnalyzer.FieldWriteSites.cs` partial-class convention.
- **Precedent**: All 8 existing graph-level dead-code diagnostics emit from the graph analyzer stage (`Diagnostics.cs` lines 700-754). Partial-class extension is the established codebase convention for sub-pass organization.
- **Tradeoff accepted**: Graph analyzer becomes slightly larger and gains one more sub-pass. Catalog convention already supports modular sub-passes.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/Parser.Actions.cs`, `Parser.Expressions.cs`, `ProofEngine.Strategies.cs` — partial-class-extension convention is in active use across the pipeline. Filename pattern: `<Stage>.<SubPass>.cs`.
  - `src/Precept/Pipeline/TypeChecker.Validation.FieldState.cs` — the existing cross-row machinery in the type checker; reviewed as alternative-home candidate. The pattern there is initial-event-rooted; field-write-site analysis is whole-program-rooted, a structurally different shape.

### Decision 4: Caller-side write capability counts as a write site (any `editable` Access Modifier at any site)

**Stakes**: high — defines what `FieldNeverSet` catches. Reversal would re-fire (or un-fire) the diagnostic on existing precepts once shipped to authors. Affects every `editable` field in every sample / consumer precept.

- **Rationale**: A field whose access mode is `editable` — whether declared at the field-level (Layer 1 baseline) or in a state-scoped `modify` row (Layer 2 override) — exposes the field to runtime mutation by the caller through the runtime API. Even if no transition row inside the precept writes the field, the caller can. Treating both `editable` sites as write sites avoids false positives on fields the author has deliberately delegated to caller-side editing.
- **Alternatives considered**:
  - *Only count internal write sites (set/add/enqueue/append/put/insert/state-entry/computed).* Rejected — would emit `FieldNeverSet` on fields the author has explicitly opened to external mutation. False positive on a legitimate authoring shape.
  - *Treat field-level `editable` differently from state-scoped `editable`.* Rejected (this is Frank's B1 from review v1 of the design) — both grant caller write capability via the runtime API; both carry author intent that the field is callable-writable. Treating them differently creates an asymmetry that would need its own explanation. The unification (Decision 5) makes them the same keyword with the same catalog category; the write-site check should treat them uniformly.
  - *Require both an internal write site AND an `editable` declaration to suppress.* Rejected — overengineered. The point of `FieldNeverSet` is "no path can write this field," not "the author followed a specific shape."
- **Precedent**: The runtime API (`docs/runtime/runtime-api.md`) makes caller-side editing of `editable` fields a first-class operation. Treating both `editable` sites as write sites aligns the analyzer with the runtime contract. Frank's architectural review v1 (`docs/Working/frank-review-field-never-set-writable-vs-editable.md` B1) reached the same conclusion via the semantically-equivalent-to-every-state framing.
- **Tradeoff accepted**: A field declared `editable` (or with `modify F editable` in some state), never written internally, never read anywhere, will not trip `FieldNeverSet`. That's a `FieldNeverRead` case (out of scope) plus an `editable` declaration; the declaration carries semantic weight (grants caller-side write capability), so it should not be ignored.
- **Sources consulted for this decision**:
  - `docs/Working/frank-review-field-never-set-writable-vs-editable.md` § B1 — Frank's architectural review v1: *"both editable sites grant caller write capability via the runtime API; treating them differently creates an asymmetry that would need its own explanation."*
  - `docs/runtime/runtime-api.md` § Update — caller-side `editable` field mutation is a first-class operation, not an exceptional path.
- **Strongest counter-evidence**: The strongest argument against this decision is that "caller-side write capability" is a runtime-API claim, not a structural claim about the precept itself — a field declared `editable` may still be authoring-dead if the runtime API consumer never writes it. The counter-position would treat `FieldNeverSet` as "no write site in the precept," ignoring `editable` declarations. **Response**: that interpretation would emit false positives on legitimate caller-write authoring shapes; Precept's structural guarantee includes the runtime API surface (the precept's caller contract), not just internal transitions. The runtime API is part of "the precept" structurally. No counter-evidence found in `docs/runtime/`, `docs/language/precept-language-spec.md`, or `samples/` for treating caller-write capability as outside the structural surface.
- **Reversibility**: Hard — once shipped, reversing means changing which fields trip the diagnostic in consumer code. Authors who relied on the current rule would see their precepts start tripping (or stop tripping) the diagnostic. Not infinite cost (severity is Warning, not Error), but author experience disruption.
- **Blast radius**: All `editable` fields in `samples/` (76 samples) plus future consumer precepts. Catalog impact: `ActionMeta.WriteSemantics` already covers internal actions; this decision additionally couples the analyzer to `AccessModifierMeta.IsWritable`. Test impact: parameterized over 8 write-site shapes (per Acceptance criterion 2).

### Decision 5: Unify the `writable` / `editable` keyword split into a single `editable` Access Modifier

**Stakes**: **irreversible**. Once `writable` is removed from the language surface and consumer precepts write `editable`, restoring `writable` would break every consumer precept. Per the lifecycle-2-design skill's irreversible-decision discipline, this decision requires Falsifiers (see § Falsifiers below) and 24-hour cooling-off before re-Locking.

- **Rationale**: `writable` and `editable` currently mean the same operational thing (write capability) at two different positions (field declaration vs per-state `modify` row), encoded in two different catalog categories (`ValueModifierMeta` vs `AccessModifierMeta`). The categorical split is itself questionable: Value Modifiers constrain *values* (notempty, optional, maxlength, ordered), while Access Modifiers govern *access* (editable, readonly, omit). `writable` is structurally an access concern wearing a value-modifier name tag — it doesn't constrain values; it grants access. Recategorizing `Writable` as an Access Modifier and unifying the keyword on `editable` eliminates the categorical mismatch and removes the keyword learning cost.
- **Alternatives considered**:
  - *Keep the split as shipped.* Rejected — the runtime is still a stub; breaking changes have zero migration cost. The current split is internally consistent but not load-bearing; preserving it would lock in incidental complexity.
  - *Unify on `writable`.* Rejected — `editable` reads more naturally at the per-state position (`in Draft modify F editable`), and the tristate per-state vocabulary (`editable` / `readonly` / `omit`) is more established. Renaming the per-state side would touch more documentation than renaming the field-level side.
  - *Keep two keywords but harmonize the catalog category.* Rejected — the catalog category is the structural problem; harmonizing the category but keeping two keywords creates an arbitrary lexical split. Unification on both axes is cleaner.
  - *Independent investigation surfaces missed rationale.* I read the spec sections 996-1017 and 1647-1651 and the originating design in `docs/Working/Archive/field-state-guarantees-v2.md` looking for a load-bearing reason for the split. The spec defends the two-layer composition model (Layer 1 baseline + Layer 2 override) but does not justify why the two layers need different keywords. The split is incidental, not load-bearing.
- **Precedent — strong (8-language survey at `research/language/expressiveness/access-modifier-keyword-unification.md`)**: 6 of 8 surveyed production languages share the cross-position unified-keyword pattern; 1 is partial; 1 is the documented exception. Net verdict: **dominant industry pattern with one explicit exception** (narrower than the original v2 framing of "every comparable language" but better-grounded). Per-comparator detail:
  - **TypeScript `readonly`** — STRONG. Used at 5 positions: interface property declarations, class property declarations, mapped types (`Readonly<T> = { readonly [P in keyof T]: T[P] }`), readonly array shorthand (`readonly Type[]`), readonly tuples. Verbatim excerpts in the research file (typescriptlang.org/docs/handbook/2/objects.html, accessed 2026-05-26).
  - **Kotlin `val` / `var`** — STRONG. Same keywords at class property declarations, top-level property declarations, and local variable bindings. Verbatim (kotlinlang.org/docs/properties.html + basic-syntax.html, accessed 2026-05-26).
  - **Swift `let` / `var`** — STRONG. Constants and variables at both local-binding and stored-property positions: *"You declare constants with the `let` keyword and variables with the `var` keyword"* (TheBasics.md) and *"Stored properties can be either variable stored properties (introduced by the `var` keyword) or constant stored properties (introduced by the `let` keyword)"* (Properties.md, accessed 2026-05-26 via swift-book canonical source).
  - **C# `readonly`** — STRONG. 5-context enumeration in the C# language reference: instance fields, static fields, parameters, struct fields, reference-readonly parameters (learn.microsoft.com/.../keywords/readonly, accessed 2026-05-26).
  - **Java `final`** — STRONG. JLS § 8.3.1.2 (final fields) and § 14.4 (final locals) both define `final` with the same syntactic role across positions; same keyword spans field declarations, formal parameters, and local-variable declarations (accessed 2026-05-26).
  - **Scala `val` / `var`** — STRONG. Same keywords at class members, primary-constructor parameters, top-level declarations, and local bindings (docs.scala-lang.org/tour/classes.html + tour/basics.html, accessed 2026-05-26).
  - **F# `let mutable`** — PARTIAL. F# uses a keyword pair (`let` + `mutable`) rather than a single shared keyword. From learn.microsoft.com/.../fsharp/language-reference/values/: *"You can use the keyword `mutable` to specify a variable that can be changed. Mutable variables in F# should generally have a limited scope, either as a field of a type or as a local value."* Same direction (cross-position mutability declaration) but two keywords compose instead of one.
  - **Rust `mut`** — EXPLICIT EXCEPTION. `mut` appears at pattern bindings, function parameters, method receivers (`&mut self`), and reference types (`&mut T`) — but NOT at struct field declarations. Rust Book Chapter 5.1 verbatim: *"Note that the entire instance must be mutable; Rust doesn't allow us to mark only certain fields as mutable."* This confirms the Phase-8 retrofit's precision-issue flag was correct; Rust is the documented exception that proves the rule rather than a counter-example to unification.
  - **Net rationale**: 6 of 8 surveyed languages share the unified-keyword pattern across property-declaration and other positions; F# is a partial precedent (same direction, keyword pair); Rust is the documented exception (binding-level inheritance is a deliberate design choice, not a missed unification). This survey supports the `writable` → `editable` unification on broader grounds than the v2 framing claimed.
- **Tradeoff accepted**: A real refactor — catalog, token, lexer, parser, spec, sample corpus all touch. Plus `WritableOnEventArg` → `EditableOnEventArg` rename. Plus `docs/language/precept-language-spec.md` § 2.2 rewrites. Substantial, but the runtime hasn't shipped, the language hasn't shipped to consumers, and the cost is paid once. The alternative is locking in a known-suboptimal design forever.
- **Sources consulted for this decision**:
  - `docs/language/precept-language-spec.md` § 2.2 (lines ~996-1017) — current keyword definitions; reviewed for load-bearing rationale of the split; found none.
  - `docs/Working/Archive/field-state-guarantees-v2.md` — originating design; reviewed for split justification; defends two-layer composition but not two-keyword split.
  - `src/Precept/Language/Modifiers.cs` (line 244) — current `ModifierKind.Writable` arm in `ValueModifierMeta`; structural placement to be moved to `AccessModifierMeta`.
  - TypeScript handbook § Readonly Properties — pattern confirms one-keyword-across-positions for `readonly`. **Verbatim excerpt pending source-fetch in Phase-10 comparator survey.**
  - Kotlin reference § Properties — pattern confirms one-keyword-across-positions for `val`/`var`. **Verbatim excerpt pending source-fetch in Phase-10 comparator survey.**
  - Rust reference § Patterns and § Variables — `mut` keyword usage in bindings, parameters, methods, references; **NOT at struct field declarations**. Surfaces the original claim's precision error.
- **Strongest counter-evidence**: The strongest argument against unification is the **stability cost**: the current two-keyword split, however incidentally arrived at, is internally consistent and any existing samples / docs / muscle-memory rely on the current vocabulary. Argued from incumbent-position-as-default. **Response**: the runtime hasn't shipped to external consumers; sample corpus is in-tree and renameable; documentation is also in-tree and renameable. Incumbent stability has no external load-bearers yet. Once the runtime ships, this argument grows teeth — *which is precisely why the unification has to happen now, not later.*
  - **Open: comparator-survey adequacy.** The Phase 8 retrofit surfaces that the comparable-systems claim was overstated. A proper comparator survey covering TypeScript, Kotlin, Rust, Swift (`var`/`let`), C# (`readonly`, `init`), Java (`final`), F# (`mutable`), Scala (`val`/`var`) — with verbatim excerpts and stable identifiers — is **prerequisite to re-Locking this design**. The unification position is defensible on narrower grounds (TypeScript + Kotlin precedent + Precept-internal catalog-category cleanup) but the survey-grounded version of the precedent leg has not been written.
- **Reversibility**: **Effectively-irreversible-post-ship**. Once consumer `.precept` files use `editable` at field declarations, restoring `writable` would break every consumer; the migration cost is infinite once the runtime ships. **Pre-ship (now)**: reversible at sample-corpus rename cost (76 files × `writable` → `editable`; 8 samples currently contain the keyword). The window for reversibility closes when the runtime + language ship to external consumers — which is also why this decision must firm up before Phase 5 implementation.
- **Blast radius**: Catalogs touched: `ModifierKind` (remove `Writable`), `ValueModifierMeta` (remove `Writable` arm), `AccessModifierMeta` (extend `ApplicableDeclarationSites`), `TokenKind` (remove `Writable`), `Tokens` (remove `Writable` entry), `DiagnosticCode` (`WritableOnEventArg` → `EditableOnEventArg`). Docs touched: `precept-language-spec.md § 2.2`, `catalog-system.md § Modifier Catalog`, `primitive-types.md`. Samples touched: every sample currently using `writable` at field declarations (corpus sweep at implementation time). External consumers affected: zero today; **all consumers post-v1 runtime ship**.

### Decision 6: Write-site classification is catalog-driven via `ActionMeta.WriteSemantics`

**Stakes**: high — introduces a new catalog property that every existing and future `ActionMeta` entry must classify. Reversal would mean removing the property + reverting consumers; once the property is documented + shipped, third-party action additions (if/when the catalog opens to plugins) would also depend on it.

- **Rationale**: Hardcoding the set of "write" action kinds inside the new analyzer is exactly the parallel-list violation CLAUDE.md § Catalog System forbids. The analyzer would need to encode which `ActionKind`s establish a field value — knowledge that belongs in the catalog. Adding a `WriteSemantics: ActionWriteSemantics` discriminator to `ActionMeta` makes the classification catalog-driven: the analyzer asks the catalog "which actions establish a value?" rather than restating the answer in its own logic. Bonus catch surfaced by the precept-reviewer audit: `clear` / `remove` / `removeAt` / `dequeue` / `pop` should NOT count as write sites that suppress `FieldNeverSet` — clearing or removing from an empty collection is a no-op, and removing the last element doesn't establish a value. Without the discriminator, the analyzer either hardcodes "writes minus clearers" (parallel list) or accepts false negatives (clear-only fields suppress the diagnostic but shouldn't).
- **Alternatives considered**:
  - *Hardcode the write-set inside the analyzer.* Rejected — parallel-list anti-pattern; the catalog already knows the action shapes, and the analyzer should derive from it.
  - *Boolean `IsWrite` flag instead of the four-way discriminator.* Rejected — would lose the `Clear` / `Remove` distinction that's the practical reason for the property. The discriminator (`None | EstablishesValue | MutatesContents | ClearsContents`) captures what `FieldNeverSet` needs today AND leaves headroom for future analyzers that care about "any mutation" vs "value-establishing" vs "value-clearing."
  - *Compute write-semantics on the fly from the action's shape.* Rejected — same parallel-list problem; the "shape" analysis would itself need a catalog answer.
- **Precedent**: The catalog already carries similar per-`ActionKind` properties (`ActionMeta.LooksLikeRow`, etc.) and per-`OperatorKind` properties (`OperatorMeta.IsCommutative`, `OperatorMeta.IsAssociative`). `WriteSemantics` follows the same catalog-property pattern.
- **Tradeoff accepted**: One new property on `ActionMeta` plus one new discriminator enum. Each `ActionMeta` entry now classifies its write-semantics — a one-time correctness cost paid by the catalog author, not by every consumer. The classification is also documentation: future-readers of `Actions.cs` can see at a glance which actions establish values.
- **Sources consulted for this decision**:
  - `CLAUDE.md § Catalog System (Non-Negotiable)` — *"Never maintain parallel keyword lists. If a parser/LS/MCP consumer hardcodes what a catalog already knows, that's a violation."* Grounds the rejection of the hardcoded-in-analyzer alternative.
  - `src/Precept/Language/Actions.cs` — current `ActionMeta` entries; reviewed to confirm `LooksLikeRow` precedent and to enumerate the actions requiring `WriteSemantics` classification (15 ActionKind members per the catalog).
  - `src/Precept/Language/OperatorMeta.cs` — `IsCommutative`, `IsAssociative` precedent for boolean/discriminator catalog properties.
- **Strongest counter-evidence**: A boolean `IsWrite` would be simpler and sufficient for `FieldNeverSet` alone. The four-way discriminator is forward-looking — designed for analyzers that don't yet exist (mutation-tracking, dataflow). The counter-position: YAGNI; ship the boolean and extend later if needed. **Response**: the practical case for the four-way is in this same design — `clear` / `remove` need to be distinct from `set` / `add` to avoid false negatives. A boolean would force the analyzer to hardcode "writes minus clearers" which is the parallel-list anti-pattern this decision rejects. The four-way is not speculative — it's what `FieldNeverSet` needs today.
- **Reversibility**: Hard — once the property is documented and consumed by `FieldNeverSet`, removing it would mean either (a) restoring hardcoded write-set logic in the analyzer (violates catalog discipline) or (b) merging the discriminator into a boolean (loses the clear-vs-write distinction). Not irreversible-post-ship in the same sense as Decision 5 (no consumer surface) but materially difficult.
- **Blast radius**: Catalogs touched: `ActionMeta` (new property), `Actions` (every entry classified — 15 ActionKind members). Pipeline touched: `GraphAnalyzer.FieldWriteSites.cs` consumes the property. Tests touched: `ActionMetaTests.WriteSemantics_PopulatedForEveryActionKind`. Future catalog-extension consumers (mutation analyzers, dataflow analyzers) inherit the classification model.

## Falsifiers

Required per the lifecycle-2-design skill's irreversible-decision discipline (Decision 5) and external-author-visible change discipline (the diagnostic and the keyword change both surface to authors). If any of these is observed post-ship, the design must be re-investigated and possibly revised:

1. **F1 — Sample-corpus over-fire.** If five or more samples in `samples/` (≥7% of the 75-sample corpus) trip `FieldNeverSet` and the fix in each case is "add a no-op write site to satisfy the analyzer" rather than "wire the field into real governance," the diagnostic is over-firing on legitimate authoring shapes. The catalog-driven write-site classification (Decision 6) or the `editable`-as-write-site rule (Decision 4) needs revision.
2. **F2 — Domain-expert teaching-path failure.** If a single domain expert in a usability test cannot, within 10 minutes of reading the diagnostic + RecoverySteps, correctly fix a precept that trips `FieldNeverSet`, the diagnostic's message template + RecoverySteps fail the Authoring Audience commitment (Authoring Audience § 0.7 in the spec). The diagnostic is shipping accurate-but-incomprehensible signal.
3. **F3 — `editable` keyword causes confusion.** If a domain expert reading `field PlanName as string editable` interprets `editable` as "the field is currently being edited" or any meaning other than "the caller has write capability on this field," the keyword choice (Decision 5) has not improved on `writable`'s readability. Strong response: revert Decision 5 (the keyword unification) — which is why it must be checked at the sample-corpus + tutorial-walkthrough stage *before* the runtime ships.
4. **F4 — `clear`-only fields under-fire after Decision 6.** If `clear` or `remove`-only fields are observed in shipped corpus *not* tripping `FieldNeverSet`, the catalog-driven write-site classification has a hole — either an `ActionMeta.WriteSemantics` entry is mis-classified, or the analyzer is not consulting the discriminator correctly. Strong response: audit `ActionMeta.WriteSemantics` per-entry against the catalog (acceptance criterion 6).
5. **F5 — Caller-side write capability doesn't suppress.** If a field declared `editable` at field-level (no internal write sites, no per-state `modify`) trips `FieldNeverSet`, Decision 4's analyzer integration is wrong — the analyzer is treating `editable` as "internal access mode" not "caller write capability." Strong response: fix the analyzer's access-modifier consultation; Decision 4's framing is correct, the implementation diverged.

The falsifiers are paired with `/lifecycle-7-audit` when that ships — periodic revisit of the falsifier list catches designs that aged badly.

## Retrofit summary (Phase 8 — 2026-05-25)

The Phase-8 retrofit added the lifecycle-2-design Phase-4 leg structure to the design's 6 decisions and surfaced **one substantive precision issue** in Decision 5's precedent leg:

- **Decision 5's comparator-precedent claim is overstated as originally written.** TypeScript `readonly` and Kotlin `val`/`var` are accurate same-keyword-at-multiple-positions precedents; Rust `mut` is not a field-declaration precedent (Rust struct fields don't carry `mut`); SQL `GRANT UPDATE` is a conceptual analogy, not a same-keyword precedent.
- **The decision's underlying claim (unify the keyword) is still defensible**, but on narrower precedent grounds (TypeScript + Kotlin + Precept-internal catalog-category cleanup) than originally stated.
- **The design's status has been demoted from `Locked` to `Externally-Grounded`** pending a proper comparator survey (the Phase-10 prerequisite work for re-Locking). A standalone research file at `research/language/expressiveness/access-modifier-keyword-unification.md` should cover TypeScript, Kotlin, Rust, Swift, C#, Java, F#, Scala with verbatim excerpts + stable identifiers before Phase 5 implementation lands.
- **`## Falsifiers` section added** per the irreversible-decision discipline.
- **Per-decision `Sources consulted` legs added** for all 6 decisions. Five of six are well-grounded; Decision 5's external-citation legs are flagged as "verbatim excerpt pending source-fetch in Phase-10 comparator survey."
- **`Strongest counter-evidence` + `Reversibility` + `Blast radius` legs added** to high+ stakes Decisions 4, 5, 6.

The retrofit's value (validating the Phase 8 keystone claim): the design's locked-but-pending-implementation status meant the precision issue surfaces *before* code ships, not after. Had the original `Locked` status held without retrofit, the language would have shipped `editable` (irreversibly) with a precedent claim that doesn't fully hold.

## Acceptance criteria

Each criterion is test-shaped and resolvable by running a specific test or check:

1. **Trip on declared-but-never-set:** A test precept with `field X as integer default 0`, no `Create.X` arg, no value-establishing action targeting X, no `editable` (field-level or per-state), no computed `<-` for X, compiles with `FieldNeverSet` Warning naming X. Test: `FieldNeverSetEmissionTests.Trips_OnDeclaredButNeverSet`.
2. **No trip on each write-site shape:** Eight parameterized test precepts, each demonstrating one write-site shape (initial event arg + `-> set`, non-initial event arg + `-> set`, scalar `set`, `add`/`enqueue`/`append`/`insert`/`put` value-establishing actions, field-level `editable`, per-state `modify F editable`, state-entry hook with `-> set`, computed `<-`), all compile with **zero** `FieldNeverSet` diagnostics. Test: `FieldNeverSetEmissionTests.NoTrip_OnEachWriteSiteShape` (parameterized).
3. **Trip on optional field with no default, no writes:** `field X as string optional`, never written → `FieldNeverSet` still fires. The diagnostic does not require a `default` — the criterion is "no write site."
4. **Trip on clear-only / remove-only field:** A field whose only "write" actions are `clear` or `remove` (no `set`/`add`/etc.) → `FieldNeverSet` still fires. Tests: `FieldNeverSetEmissionTests.Trips_OnClearOnlyField`, `FieldNeverSetEmissionTests.Trips_OnRemoveOnlyField`.
5. **No regression on shipped corpus:** `SampleCompilesCleanTests` (currently 76 samples) continues to pass after the unification renames every `writable` → `editable` AND the FieldNeverSet diagnostic ships. Every sample that would otherwise trip the diagnostic is fixed before merge.
6. **`ActionMeta.WriteSemantics` populated for every entry:** `ActionMetaTests.WriteSemantics_PopulatedForEveryActionKind` — iterates `Actions.All`, asserts `WriteSemantics != null` for every entry.
7. **`AccessModifierMeta.ApplicableDeclarationSites` accepts both sites for `Write`:** `AccessModifierMetaTests.Write_ValidAtFieldDeclaration` and `Write_ValidAtModifyRow` both pass.
8. **`ModifierKind.Writable` no longer exists:** `ModifierKindTests.Writable_IsRemoved` — asserts `Enum.IsDefined(typeof(ModifierKind), "Writable")` is `false`. `TokenKindTests.Writable_IsRemoved` — same for `TokenKind.Writable`.
9. **MCP catalog completeness:** `precept_diagnostic("FieldNeverSet")` returns full metadata — message template, TriggerCondition, FixHint, RecoverySteps, ExampleBefore, ExampleAfter, RelatedCodes. `precept_types`, `precept_operations`, `precept_patterns` continue to surface; `precept_diagnostic("WritableOnEventArg")` returns 404 or equivalent (the diagnostic is renamed).
10. **`DiagnosticCatalogTests` (reflection-based gate):** registers `FieldNeverSet` with `DiagnosticStage.Graph`, `DiagnosticCategory.Structure`, `Severity.Warning`, and non-empty Message, FixHint, TriggerCondition, RecoverySteps, ExampleBefore, ExampleAfter.
11. **`Precept0027DiagnosticEmissionCoverage` and `Precept0028DiagnosticTestCoverage` analyzers pass** on the new code — no "unwired" or "untested" entries for `FieldNeverSet`. (Note: Gate2 allow-list registration is the cross-project test detection workaround for diagnostics whose tests live in a different project; verify Gate2 registration is correct.)
12. **Doc enumeration verified:** Every doc listed in § Doc-update enumeration has the corresponding update committed in the same PR.

## Dependencies

**Upstream:**
- Existing graph analyzer infrastructure (in place)
- Type checker's cross-row machinery in `src/Precept/Pipeline/TypeChecker.Validation.FieldState.cs` (in place — already drives `InitialEventMissingAssignments` and `MaterializedFieldSelfReference`; the new analyzer does not depend on it but the cross-row pattern is established)
- Catalog convention for new diagnostic ordinals (in place — current top is 149, this PR pushes to 150)
- `AccessModifierMeta` and `ValueModifierMeta` records (in place — Decision 5 modifies `AccessModifierMeta`, retires `ModifierKind.Writable` from `ValueModifierMeta`)

**Downstream:**
- Sample corpus rename obligation — every `writable` → `editable` (mechanical, part of this PR)
- Sample corpus fix obligation — every `FieldNeverSet` trip (substantive, part of this PR)
- `FieldNeverRead` design (separate, deferred)
- Strict-mode policy (CI / `/lifecycle-6-review --strict`) — separate operational decision

## Doc-update enumeration

Per the CLAUDE.md § Documentation Sync routing table, the following canonical docs require updates in the same PR:

**For the diagnostic:**
- **`docs/compiler/diagnostic-system.md`** — add `FieldNeverSet` to the diagnostic catalog table in the Graph stage section; rename `WritableOnEventArg` entry to `EditableOnEventArg`
- **`docs/compiler/graph-analyzer.md`** § 6 (phases section, ~lines 319-434) — document the new `FieldWriteSites` sub-pass after reachability and completeness
- **`docs/language/catalog-system.md`** lines 290, 716, 1989, 1993 — bump `DiagnosticCode` count to **150** (corrects pre-existing 148→149 drift AND adds the new code in one pass)
- **`docs/Working/compiler-readiness-review-2026-05-24.md`** — bump count to 150 (corrects pre-existing 148→149 drift)
- **`docs/Working/compiler-readiness-plan-2026-05-24.md`** — append the design ID to Phase 5 scope (dead-code family), with a link back to this design doc

**For the unification:**
- **`docs/language/precept-language-spec.md`** § 2.2 (lines ~996-1017) — rewrite to reflect single `editable` Access Modifier at field declaration AND per-state `modify`; update the keyword table at line 272 to remove `Writable` and adjust `Write` description; update redundancy rules at lines 1006-1012 to use `editable` consistently; update the diagnostic name at lines 1647-1651 (`WritableOnEventArg` → `EditableOnEventArg`)
- **`docs/language/primitive-types.md`** — verify the field declaration examples and the modifier catalog reference `editable` not `writable`
- **`docs/language/catalog-system.md`** § Modifier Catalog — remove `Writable` from Value Modifier section; add field-declaration applicability to `Write` in Access Modifier section; bump modifier count appropriately
- **`tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`** — DO NOT hand-edit; regenerate from the catalog. Verify the regenerated grammar drops `writable` and accepts `editable` at the field-modifier position.

**For the analyzer / catalog property:**
- **`docs/compiler/type-checker.md`** — note the new `ActionMeta.WriteSemantics` property if the type-checker doc enumerates `ActionMeta` fields (verify; may not require update)
- **`docs/language/catalog-system.md`** § Action Catalog — document the new `WriteSemantics` property

**No update required (verified):**
- `docs/runtime/runtime-api.md` — runtime contract unaffected; diagnostic is compile-time-only; `editable` access modifier behavior at the runtime API surface is preserved
- `docs/tooling/mcp.md` — `precept_diagnostic` is catalog-driven via `Diagnostics.GetMeta`; no formatter change needed

## Open questions

*None.* Every design decision is locked with four legs filled. Operational details deferred to implementation (not open design questions):

- Exact `FieldNeverSet` enum ordinal — `150` per current catalog count; verify at implementation time
- Specific message-template wording — tightened via `/lifecycle-6-review --strict` against existing template style
- Corpus-sweep enumeration — performed by running the analyzer against `samples/` at implementation time; feeds into the implementing PR's sample-fix commits
- Whether the `WritableOnEventArg` → `EditableOnEventArg` rename also slots the diagnostic into a different `DiagnosticStage` or `DiagnosticCategory` — verify at implementation; the existing code is `Parser` stage, `Surface` category; the rename should preserve both
