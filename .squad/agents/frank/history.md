## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, and field-state design work must stay grounded in shipped spec and evaluator/runtime surfaces, not inferred intent.

## Live Guidance

- `readonly` / `editable` / guarded access modes govern the Update (patch) surface; they do not restrict event-driven `set` actions unless the spec changes.
- Business-domain magnitude modifier legality is a catalog contract: fix drift in metadata and docs, not with checker-only special cases.
- Required-field constructor enforcement is now an active implementation surface: D93 and D94 are real checker obligations, while stateless construction stays outside the state-entry slice.
- Interval containment is the authoritative overflow-prevention mechanism for bounded arithmetic; obsolete `@bounds`, separate validator-phase, and runtime-fallback proposals are historical only.

## Historical Summary

- 2026-05-12 through 2026-05-13 concentrated Frank's work around hover contract reviews, comma-list `StateTarget` closure, field-state guarantees, modifier applicability drift, constructor diagnostics, and interval-proof design.
- Durable batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps the newest conclusions and immediate guidance other agents need.
- Diagnostic coverage enforcement now uses two gates, negative tests are mandatory for gap closures, and catalog-mediated emission expansion stays selective.

## Recent Updates

### 2026-05-14T00:05:43Z — Diagnostic enforcement revised for interval-proof dependency

- `docs/Working/diagnostic-enforcement.md` now treats PRE0078 `NumericOverflow` as a Strategy 7 / ProofEngine obligation failure instead of a Slice 8 TypeChecker wire.
- The plan now records Slice 9C's dependency on interval-engine Slice 2, narrows PRE0079 to the constant-literal overflow case, and adds the cross-plan dependency table plus Q9/Q10 coordination risks.

### 2026-05-13T18:17:15Z — Interval-proof design doc authored

- `docs/working/interval-proof-engine-design.md` is the authoritative design + implementation plan for Strategy 7 (`IntervalContainment`).
- No new `@bounds` syntax: the engine reads existing `min`/`max` modifiers via `ValueModifierMeta.ProofSatisfactions`.
- `integer` fields are exempt because `TypeKind.Integer` is BigInteger-backed and mathematically unbounded.
- Interval proof results extend existing hover/proof vocabulary; they do not introduce new hover card families.

### 2026-05-13T18:32:11Z — Test strategy revised per Soup Nazi review; strategy count corrected

- B/C/D tests use a real parse helper on inline precept strings; `SemanticIndex` stubs are not an acceptable fixture.
- The plan now partitions interval tests by layer, adds the catalog-count tripwire, and explicitly covers multi-`set` obligation collection.
- `TryDischarge` already had six strategies before interval containment; `CompositionalConstraint` was the missing sixth item in the earlier narrative.
- Qualifier and interval diagnostics are orthogonal and should both surface when both obligations fail on the same expression.

### 2026-05-13T20:05:43Z — Diagnostic enforcement revised for interval proof engine dependency

- `docs/Working/diagnostic-enforcement.md` revised in-place to account for the interval proof engine design.
- **Key finding:** PRE0078 `NumericOverflow` is NOT a TypeChecker gap — it's a ProofEngine obligation failure diagnostic owned by Strategy 7 (IntervalContainment). Removed from Slice 8 scope; Gate 1 allow-list removal coordinated with interval engine Slice 2.
- **Slice 9C now depends on interval engine Slice 2** — the proof obligation consistency audit must include Strategy 7.
- **PRE0079 `OutOfRange` scope narrowed** — general expression-level bounds checking owned by interval engine; PRE0079 retains only constant-literal-assignment case.
- **Cross-plan dependency table added** documenting per-slice coordination between interval engine and enforcement plan.
- Two new open questions (Q9 allow-list coordination, Q10 PRE0078/PRE0079 deduplication risk) flagged for Shane.
- **Key learning:** When a parallel implementation plan introduces a new emission path for an existing diagnostic code, the enforcement plan must acknowledge the external dependency rather than attempt to wire the same code independently. Cross-plan coordination is an ordering constraint, not an optional refinement.

### 2026-05-13T19:55:10Z — Overflow prevention analysis revised to reflect interval proof engine

- `docs/Working/overflow-prevention-design-analysis.md` was revised in place and marked superseded where it diverged from the approved interval-proof design.
- Six obsolete claim categories were corrected: `@bounds` syntax, bounded-integer targeting, separate validator phase, bounded-field runtime fallback, a new diagnostic code, and the old three-wave timeline.
- The interval proof engine now owns arithmetic result-range checking, `NumericOverflow` emission for bounded targets, modifier-derived bound extraction, guard narrowing, catalog-driven interval transfer, and hover interval display.
- The analysis retains only durable value: problem statement, strategy comparison matrix, and historical context, with cross-references to `interval-proof-engine-design.md`.


## Learnings

- If a design depends on an existing diagnostic, confirm the emitting pipeline stage instead of trusting `DiagnosticMeta` declarations or spec prose.
- Gate 2 is a tripwire, not proof of behavioral correctness; every gap closure still needs both positive and negative tests.
- Stale exploratory docs must be revised promptly once an implementation-spec path is approved, or they become misleading guidance for downstream agents.
- `set` into an `omit` target-state field is the decisive field-state rule; Update access modes do not constrain Fire semantics.
- Catalog static initialization may not reach downstream catalog statics during cctor execution; reverse references should defer through `Lazy<T>`.
