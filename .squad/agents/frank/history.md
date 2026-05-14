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

### 2026-05-14T01:23:00-04:00 — Interpolated quantity expression analysis for normalization design

- Discovered that the actual `samples/Test.precept` `NumericOverflow` fires on an **interpolated** expression (`'{test2} [lb_av]'` → `TypedInterpolatedTypedConstant`), not a static literal (`TypedTypedConstant`).
- Traced two independent failure paths: Strategy 7 (`IntervalOfNarrowed` has no case for `TypedInterpolatedTypedConstant` → `Unbounded`) and Strategy 6 (`SatisfactionCovers` returns null for `DeclarationValue` bounds → conservative fail).
- Confirmed the static-literal normalization fix (Slices 14–18) does NOT fix the interpolated case — these are independent problems hitting different code paths.
- Extended `docs/Working/quantity-normalization-design.md` with §1.5 (interpolated analysis), §5.3 (Slices 19–21 for interpolated interval track), and Q5/Q6 in §7.
- Filed decision record `.squad/decisions/inbox/frank-interpolated-quantity-analysis.md` with D1–D4.
- Key design extension needed: `TypedInterpolatedTypedConstant` must carry a `UcumParsedUnit?` for static unit portions; `IntervalOfNarrowed` must recurse into magnitude slots; interval scaling by UCUM factor enables cross-unit proof discharge.

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
- `precept_compile` runs the full compilation pipeline including the ProofEngine — `Compiler.Compile()` calls `ProofEngine.Prove()` and returns all diagnostics. Do not claim MCP tools skip proof obligations.
- `PreceptValue`'s bytes 8–23 union payload is three-way: `decimal`, `long`, or a **reference region** (managed pointer). Do not characterize it as decimal-only. 23 of 32 `TypeKind` members use the reference lane. Business-domain composite types like `quantity` can carry unit information via the reference region — the "no space for unit reference" claim is incorrect. The exact internal layout is a pending implementation decision per `evaluator.md`.
- Adding ## Contents after status blocks materially improves long-form doc navigation for both humans and AI agents.
- When a compile pipeline runs per-keystroke, the performance question is never "is this computation expensive?" (decimal math is nanoseconds) but "is it redundant?" — normalize once, store on the semantic model, let downstream consumers read pre-computed results.
- Abstract interpretation (proof engine intervals) and concrete execution (evaluator opcodes) should never share an intermediate representation — they share source data (normalized bounds on `TypedField`), not execution plans.
- Universal post-step patterns (compute raw interval → scale by static unit) are strictly superior to per-node-type normalization scattered across switch arms in `IntervalOfNarrowed`. The `TryGetStaticUnit` helper unifies static and interpolated typed constants.
- "Store both original and normalized" on `TypedField` resolves the display-vs-comparison tension permanently — 2 extra `decimal?` fields per field is negligible cost for eliminating an entire class of design questions.
- Long runtime/tooling docs need a `## Contents` section driven by live H2/H3 headings so AI navigation stays reliable.
