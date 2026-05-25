---
status: Locked 2026-05-25 (v2 — incorporates precept-reviewer + Frank findings)
phase-target: Phase 5 (Proof engine satisfiability — dead-code family); access-modifier unification co-ships
supersedes: v1 (2026-05-25 morning)
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

- **Rationale**: `FieldNeverSet` and `FieldNeverRead` have different confidence and design surfaces. `FieldNeverSet` is unambiguous — a field with no write site can only hold its default, representing an authoring oversight. `FieldNeverRead` is ambiguous — Precept fields flow to consumers regardless of internal governance, so set-but-never-read isn't necessarily a defect. Shipping together forces a single severity / suppression policy across two cases that warrant different treatment.
- **Alternatives considered**:
  - *Ship both together.* Rejected — `FieldNeverRead` requires a suppression mechanism and a stable "read" definition (does interpolation count? does `is set` count? does `modify editable` count as a read by the caller?). Separate design conversation that should not block the unambiguous half.
  - *Single merged `DeadField` diagnostic with varying severity.* Rejected — forces the catalog out of its self-describing convention (one stable trigger / severity per `DiagnosticCode`).
- **Precedent**: The graph dead-code suite splits related concerns into distinct diagnostics: `UnreachableState`, `DeadEndState`, `StructuralSinkState`, `AlwaysRejecting` — all "state/event graph dead code" but each carries a distinct trigger and recovery path.
- **Tradeoff accepted**: Set-but-never-read fields (the production-order Priority pattern) stay uncaught until the deferred design ships. Acceptable — the author can still notice via review; the structurally worse case (declared-but-never-set) is the one that flies under review radar.

### Decision 2: Severity = Warning, not Error

- **Rationale**: A field with no write site is recoverable — the author can wire it up, make it computed, or delete it. Compilation can continue, the precept can still run. Erroring would block precepts that are otherwise structurally sound and may be in mid-development. Warning surfaces the issue without blocking; CI policy or `/lifecycle-6-review --strict` can elevate at promotion time.
- **Alternatives considered**:
  - *Error.* Rejected — too disruptive. Authors frequently declare fields before wiring them; erroring would force premature scaffolding. The 8 existing graph-level dead-code diagnostics reserve Error for structural integrity violations (`TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`) and use Warning for the "potentially intentional, definitely surface-able" category — `FieldNeverSet` is squarely in the warning camp.
  - *Info.* Rejected — too quiet. Warnings are the established "you should look at this" level.
- **Precedent**: `UnreachableState` (PRE0080), `UnhandledEvent` (PRE0081), `DeadEndState`, `AlwaysRejecting` all ship as Warning. `FieldNeverSet` is the field-level analog and inherits the same disposition.
- **Tradeoff accepted**: Warnings can be ignored; some authors will not address them. Mitigated by IDE squiggles via the language server and eventual `/lifecycle-6-review --strict` gating. Policy escalation is a separate downstream concern.

### Decision 3: Analyzer lives in the Graph stage as a partial-class extension of `GraphAnalyzer`

- **Rationale**: Field-write-site analysis is a whole-program, post-type-check pass — the same shape as the existing graph analyzer's reachability and completeness passes. It walks the type-checked AST globally and emits Structure-category diagnostics. The graph analyzer is the natural home.
- **Alternatives considered**:
  - *Add to the type checker.* Rejected — type checking is statement-local in shape, even though `TypeChecker.Validation.FieldState.cs` does some cross-row work (initial-event-rooted action-chain enumeration for `InitialEventMissingAssignments`). Whole-program field-write-site analysis matches the graph stage's BFS-over-everything shape more cleanly than the type checker's initial-event-rooted machinery.
  - *Create a new dedicated pipeline stage.* Rejected — adds a stage for one diagnostic. The graph analyzer's existing sub-pass structure accommodates field analysis cleanly.
  - *Runtime check.* Rejected — too late; the whole point is compile-time detection.
  - *Separate file (`GraphAnalyzer/` subfolder).* Rejected — the existing layout is flat with partial-class splits (`Parser.Actions.cs`, `Parser.Expressions.cs`, `ProofEngine.Strategies.cs`). Use the established `GraphAnalyzer.FieldWriteSites.cs` partial-class convention.
- **Precedent**: All 8 existing graph-level dead-code diagnostics emit from the graph analyzer stage (`Diagnostics.cs` lines 700-754). Partial-class extension is the established codebase convention for sub-pass organization.
- **Tradeoff accepted**: Graph analyzer becomes slightly larger and gains one more sub-pass. Catalog convention already supports modular sub-passes.

### Decision 4: Caller-side write capability counts as a write site (any `editable` Access Modifier at any site)

- **Rationale**: A field whose access mode is `editable` — whether declared at the field-level (Layer 1 baseline) or in a state-scoped `modify` row (Layer 2 override) — exposes the field to runtime mutation by the caller through the runtime API. Even if no transition row inside the precept writes the field, the caller can. Treating both `editable` sites as write sites avoids false positives on fields the author has deliberately delegated to caller-side editing.
- **Alternatives considered**:
  - *Only count internal write sites (set/add/enqueue/append/put/insert/state-entry/computed).* Rejected — would emit `FieldNeverSet` on fields the author has explicitly opened to external mutation. False positive on a legitimate authoring shape.
  - *Treat field-level `editable` differently from state-scoped `editable`.* Rejected (this is Frank's B1 from review v1 of the design) — both grant caller write capability via the runtime API; both carry author intent that the field is callable-writable. Treating them differently creates an asymmetry that would need its own explanation. The unification (Decision 5) makes them the same keyword with the same catalog category; the write-site check should treat them uniformly.
  - *Require both an internal write site AND an `editable` declaration to suppress.* Rejected — overengineered. The point of `FieldNeverSet` is "no path can write this field," not "the author followed a specific shape."
- **Precedent**: The runtime API (`docs/runtime/runtime-api.md`) makes caller-side editing of `editable` fields a first-class operation. Treating both `editable` sites as write sites aligns the analyzer with the runtime contract. Frank's architectural review v1 (`docs/Working/frank-review-field-never-set-writable-vs-editable.md` B1) reached the same conclusion via the semantically-equivalent-to-every-state framing.
- **Tradeoff accepted**: A field declared `editable` (or with `modify F editable` in some state), never written internally, never read anywhere, will not trip `FieldNeverSet`. That's a `FieldNeverRead` case (out of scope) plus an `editable` declaration; the declaration carries semantic weight (grants caller-side write capability), so it should not be ignored.

### Decision 5: Unify the `writable` / `editable` keyword split into a single `editable` Access Modifier

- **Rationale**: `writable` and `editable` currently mean the same operational thing (write capability) at two different positions (field declaration vs per-state `modify` row), encoded in two different catalog categories (`ValueModifierMeta` vs `AccessModifierMeta`). The categorical split is itself questionable: Value Modifiers constrain *values* (notempty, optional, maxlength, ordered), while Access Modifiers govern *access* (editable, readonly, omit). `writable` is structurally an access concern wearing a value-modifier name tag — it doesn't constrain values; it grants access. Recategorizing `Writable` as an Access Modifier and unifying the keyword on `editable` eliminates the categorical mismatch, removes the keyword learning cost, and aligns Precept with every adjacent language (Rust `mut`, TypeScript `readonly`, Kotlin `val`/`var`, SQL `GRANT UPDATE` — all use one keyword across declaration and scoped-override positions).
- **Alternatives considered**:
  - *Keep the split as shipped.* Rejected — the runtime is still a stub; breaking changes have zero migration cost. The current split is internally consistent but not load-bearing; preserving it would lock in incidental complexity.
  - *Unify on `writable`.* Rejected — `editable` reads more naturally at the per-state position (`in Draft modify F editable`), and the tristate per-state vocabulary (`editable` / `readonly` / `omit`) is more established. Renaming the per-state side would touch more documentation than renaming the field-level side.
  - *Keep two keywords but harmonize the catalog category.* Rejected — the catalog category is the structural problem; harmonizing the category but keeping two keywords creates an arbitrary lexical split. Unification on both axes is cleaner.
  - *Independent investigation surfaces missed rationale.* I read the spec sections 996-1017 and 1647-1651 and the originating design in `docs/Working/Archive/field-state-guarantees-v2.md` looking for a load-bearing reason for the split. The spec defends the two-layer composition model (Layer 1 baseline + Layer 2 override) but does not justify why the two layers need different keywords. The split is incidental, not load-bearing.
- **Precedent**: Every comparable language with a layered access-capability model uses one keyword across positions. TypeScript `readonly` appears at field declarations and in mapped types — same word. Rust `mut` appears at field declarations and in patterns/bindings — same word. SQL grants use `GRANT UPDATE` at every scope. No precedent for splitting write-capability vocabulary across declaration and scoped-override sites.
- **Tradeoff accepted**: A real refactor — catalog, token, lexer, parser, spec, sample corpus all touch. Plus `WritableOnEventArg` → `EditableOnEventArg` rename. Plus `docs/language/precept-language-spec.md` § 2.2 rewrites. Substantial, but the runtime hasn't shipped, the language hasn't shipped to consumers, and the cost is paid once. The alternative is locking in a known-suboptimal design forever.

### Decision 6: Write-site classification is catalog-driven via `ActionMeta.WriteSemantics`

- **Rationale**: Hardcoding the set of "write" action kinds inside the new analyzer is exactly the parallel-list violation CLAUDE.md § Catalog System forbids. The analyzer would need to encode which `ActionKind`s establish a field value — knowledge that belongs in the catalog. Adding a `WriteSemantics: ActionWriteSemantics` discriminator to `ActionMeta` makes the classification catalog-driven: the analyzer asks the catalog "which actions establish a value?" rather than restating the answer in its own logic. Bonus catch surfaced by the precept-reviewer audit: `clear` / `remove` / `removeAt` / `dequeue` / `pop` should NOT count as write sites that suppress `FieldNeverSet` — clearing or removing from an empty collection is a no-op, and removing the last element doesn't establish a value. Without the discriminator, the analyzer either hardcodes "writes minus clearers" (parallel list) or accepts false negatives (clear-only fields suppress the diagnostic but shouldn't).
- **Alternatives considered**:
  - *Hardcode the write-set inside the analyzer.* Rejected — parallel-list anti-pattern; the catalog already knows the action shapes, and the analyzer should derive from it.
  - *Boolean `IsWrite` flag instead of the four-way discriminator.* Rejected — would lose the `Clear` / `Remove` distinction that's the practical reason for the property. The discriminator (`None | EstablishesValue | MutatesContents | ClearsContents`) captures what `FieldNeverSet` needs today AND leaves headroom for future analyzers that care about "any mutation" vs "value-establishing" vs "value-clearing."
  - *Compute write-semantics on the fly from the action's shape.* Rejected — same parallel-list problem; the "shape" analysis would itself need a catalog answer.
- **Precedent**: The catalog already carries similar per-`ActionKind` properties (`ActionMeta.LooksLikeRow`, etc.) and per-`OperatorKind` properties (`OperatorMeta.IsCommutative`, `OperatorMeta.IsAssociative`). `WriteSemantics` follows the same catalog-property pattern.
- **Tradeoff accepted**: One new property on `ActionMeta` plus one new discriminator enum. Each `ActionMeta` entry now classifies its write-semantics — a one-time correctness cost paid by the catalog author, not by every consumer. The classification is also documentation: future-readers of `Actions.cs` can see at a glance which actions establish values.

## Acceptance criteria

Each criterion is test-shaped and resolvable by running a specific test or check:

1. **Trip on declared-but-never-set:** A test precept with `field X as integer default 0`, no `Create.X` arg, no value-establishing action targeting X, no `editable` (field-level or per-state), no computed `<-` for X, compiles with `FieldNeverSet` Warning naming X. Test: `FieldNeverSetEmissionTests.Trips_OnDeclaredButNeverSet`.
2. **No trip on each write-site shape:** Eight parameterized test precepts, each demonstrating one write-site shape (initial event arg + `-> set`, non-initial event arg + `-> set`, scalar `set`, `add`/`enqueue`/`append`/`insert`/`put` value-establishing actions, field-level `editable`, per-state `modify F editable`, state-entry hook with `-> set`, computed `<-`), all compile with **zero** `FieldNeverSet` diagnostics. Test: `FieldNeverSetEmissionTests.NoTrip_OnEachWriteSiteShape` (parameterized).
3. **Trip on optional field with no default, no writes:** `field X as string optional`, never written → `FieldNeverSet` still fires. The diagnostic does not require a `default` — the criterion is "no write site."
4. **Trip on clear-only / remove-only field:** A field whose only "write" actions are `clear` or `remove` (no `set`/`add`/etc.) → `FieldNeverSet` still fires. Tests: `FieldNeverSetEmissionTests.Trips_OnClearOnlyField`, `FieldNeverSetEmissionTests.Trips_OnRemoveOnlyField`.
5. **No regression on shipped corpus:** `SampleCompilesCleanTests` (currently 75 samples) continues to pass after the unification renames every `writable` → `editable` AND the FieldNeverSet diagnostic ships. Every sample that would otherwise trip the diagnostic is fixed before merge.
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
