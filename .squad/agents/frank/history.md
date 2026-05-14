## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, and field-state design work must stay grounded in shipped spec and evaluator/runtime surfaces, not inferred intent.

## Live Guidance

- `readonly` / `editable` / guarded access modes govern the Update (patch) surface; they do not restrict event-driven `set` actions unless the spec changes.
- Business-domain magnitude modifier legality is a catalog contract: fix drift in metadata and docs, not with checker-only special cases.
- Required-field constructor enforcement is now an active implementation surface: D93 and D94 are real checker obligations, while stateless construction stays outside the state-entry slice.
- Interval containment is the authoritative overflow-prevention mechanism for bounded arithmetic; obsolete `@bounds`, separate validator-phase, and runtime-fallback proposals are historical only.
- Quantity normalization for cross-unit comparisons is currently a compile-time concern; runtime `PreceptValue` normalization is deferred until the later Phase 3 runtime track.

## Historical Summary

- 2026-05-12 through 2026-05-14 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor diagnostics, interval-proof design, and diagnostic-enforcement architecture.
- The durable enforcement baseline is now: PRE0078 stays in ProofEngine Strategy 7, PRE0079 is the TypeChecker literal-bounds wire, PRE0019 is retired unless a real presence-obligation generation gap is found, and PRE0094 is already emitted in the checker.
- Durable batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps only the latest guidance other agents need immediately.

## Recent Updates

### 2026-05-14T05:12:08Z — Quantity normalization design for cross-unit bound comparison

- Authored `docs/Working/quantity-normalization-design.md` for the false-positive `NumericOverflow` path where typed-constant magnitudes are compared before UCUM scaling is applied.
- The two compile-time defect sites are `TypeChecker.Validation.Modifiers.cs:TryExtractTypedConstantMagnitude` and `ProofEngine.Composition.cs:TryGetTypedConstantMagnitude`; both should consume one shared helper at `src/Precept/Language/Numeric/TypedConstantNormalizer.cs`.
- Rejected the earlier `TypeMeta.NumericNormalization` DU proposal as unnecessary, confirmed the existing UCUM infrastructure is sufficient, kept money out of normalization, and deferred runtime/`PreceptValue` work to Phase 3.
- Open questions recorded for Shane: diagnostic display format, `DeclaredMin`/`DeclaredMax` storage, scope boundary, and normalizer namespace.

### 2026-05-14T04:43:00Z — Final enforcement review corrected PRE0094 inventory drift and restored PRE0019 retirement annotation

- `docs/Working/diagnostic-enforcement.md` now reflects that PRE0094 already has two live emitters in `TypeChecker.Validation.FieldState.cs`, so the open gap count is 49 and the emission inventory is 84.
- Slice 3 is recorded as already wired, the §3.7 D2 table again marks PRE0019 as retired, and the Elaine naming references plus Q1/Q2/Q10 were rechecked with no new open questions.

### 2026-05-13T18:17:15Z–2026-05-14T04:00:00Z — Diagnostic enforcement decisions consolidated

- Strategy 7 (`IntervalContainment`) is the approved bounded-expression design surface; obsolete validator/runtime overflow proposals are historical only.
- Dynamic qualifier enforcement stays split by lane: the TypeChecker skips PRE0070–0074 when the qualifier is dynamic, while ProofEngine Strategy 5 remains the enforcement point.
- PRE0079 stays a TypeChecker-only literal-bounds diagnostic with zero live emitters today; no new proof infrastructure is needed for that future wire.

## Learnings

- If a design depends on an existing diagnostic, confirm the live emitting pipeline stage instead of trusting catalog metadata or prose.
- Gate 2 is a tripwire, not proof of behavioral correctness; every gap closure still needs positive and negative tests.
- Gate 1 allow-list entries should stay root-cause oriented; per-code citation churn adds noise without improving enforcement.
- Strip comment content before Gate 2 scans `DiagnosticCode.*`; it cheaply removes the doc-comment false-positive class.
- Catalog static initialization may not reach downstream catalog statics during cctor execution; reverse references should defer through `Lazy<T>`.
- `set` into an `omit` target-state field is the decisive field-state rule; Update access modes do not constrain Fire semantics.
- Proof diagnostic naming should describe the state of the author's definition (condition-first), not the compiler's failed attempt.
- When typed-constant tuples already carry UCUM scale information, prefer a shared normalization helper over inventing new metadata shapes.
