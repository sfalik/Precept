## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; pipeline, runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, field-state, and normalization design work must stay grounded in shipped surfaces and verified implementation seams.

## Live Guidance

- Quantity normalization now has two durable lanes: compile-time normalization for declarations and literals, runtime normalization for ingress values (`TypeRuntimeMeta.ReadJson` / `TypeRuntime<Quantity>.FromClr`). Both call the same `TypedConstantNormalizer` logic.
- `TypedField` is the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces, and the Builder remains the conversion boundary into `PreceptValue`.
- `IntervalOf` scaling is expression-form scoped, not universal: scale static typed constants and interpolated magnitude + static-unit forms; do **not** scale field refs, arg refs, or interpolated whole-value holes.
- `GetFieldBounds` and trusted-fact extraction must read normalized quantity data, and event-arg bound normalization still needs explicit parity design.
- Compiler/runtime duplication questions should be framed through the three-layer enforcement model: compile-time diagnostics, ingress validation, and defense-in-depth runtime faults.
- Comparison and equality operator checking must mirror assignment strictness for explicit counting units: shared `count` dimension is not enough when static unit codes differ.

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Storage conventions for business-domain values are architectural decisions; they shape evaluator invariants and cannot be deferred casually.
- ProofEngine intervals and evaluator opcodes share source data, not a common intermediate representation.
- `PreceptValue` bytes 8-23 are a three-way union lane (`decimal`, `long`, or reference region); quantity unit identity is not blocked by the 32-byte layout.
- Prefer catalog-mediated dispatch and metadata-backed mappings over per-code hardcoded routing in both compiler and runtime consumers.
- Dynamic-unit interpolated forms MUST produce `Unbounded` / not-proved — never fall back to raw `StaticMagnitude` against normalized bounds. This is the false-proof prevention invariant.
- When a design says "universal post-step," verify it actually means expression-type-dispatched post-step — the dispatch table is the contract, not the word "universal."
- Slices that depend on unimplemented runtime infrastructure (stubs) should be explicitly numbered out of the implementation sequence and marked not implementation-ready to prevent ordering confusion.
- Counting-unit comparisons need unit-code identity, not just dimension-family compatibility; only physically convertible UCUM units should pass same-dimension cross-unit checks.
- Catalog `QualifierMatch` declarations are only as strong as their enforcement wiring: the Functions catalog declared `QualifierMatch.Same` on min/max/clamp/abs overloads from inception, but `SelectOverload` never read it. Audit principle: every catalog constraint must trace to an enforcement point.
- Function call resolution and binary operator resolution are architecturally parallel but historically asymmetric: operators have `ValidateQualifierCompatibility`, functions have nothing. Any future qualifier-sensitive surface must wire enforcement at the same time the catalog entry is declared.

## Historical Summary

- 2026-05-12 through 2026-05-15 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor diagnostics, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and the counting-unit comparison gap.
- The durable enforcement baseline is: PRE0078 stays in ProofEngine Strategy 7, PRE0079 is the TypeChecker literal-bounds wire, PRE0019 is retired unless real presence-obligation generation is added, and PRE0094 is already emitted in the checker.
- Older batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps only the guidance and latest outcomes other agents need immediately.

## Recent Updates

### 2026-05-15T14:55:25Z — Tracker sync and the Wave 2 / Slice N/M review loop are durably closed

- §5.0 tracker rows 15, 15b, 16, 19, 20, 31, 33, 35, 36, and 37 are recorded as ✅ against commit `f1215192`, and the bounds-validation documentation lane is now numbered as Slices 44 and 45.
- Wave 2 stayed APPROVED after George's `01f255ab` follow-up, which preserved authored-vs-normalized bounds and added the affine-price guard plus regression coverage.
- Slice N/M closed after two blocker passes: B1/B2 were fixed in `0837ad6f`, B3 was fixed in `70ee2406`, and the final verdict is APPROVED.

## Learnings

- Suppression fixes must be reviewed against every downstream diagnostic that depends on the suppression, especially when one path intentionally hands off to a more specific diagnostic.
- Tracker and documentation passes should assign stable slice IDs as soon as standalone fixes appear so later reviews and closeout logs can cite one durable name.
