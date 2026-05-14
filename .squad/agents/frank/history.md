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

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Storage conventions for business-domain values are architectural decisions; they shape evaluator invariants and cannot be deferred casually.
- ProofEngine intervals and evaluator opcodes share source data, not a common intermediate representation.
- `PreceptValue` bytes 8-23 are a three-way union lane (`decimal`, `long`, or reference region); quantity unit identity is not blocked by the 32-byte layout.
- Prefer catalog-mediated dispatch and metadata-backed mappings over per-code hardcoded routing in both compiler and runtime consumers.

## Historical Summary

- 2026-05-12 through 2026-05-14 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor diagnostics, interval-proof design, quantity normalization, and diagnostic-enforcement architecture.
- The durable enforcement baseline is: PRE0078 stays in ProofEngine Strategy 7, PRE0079 is the TypeChecker literal-bounds wire, PRE0019 is retired unless real presence-obligation generation is added, and PRE0094 is already emitted in the checker.
- Older batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps only the guidance and latest outcomes other agents need immediately.

## Recent Updates

### 2026-05-14T17:10:32.283-04:00 — Interpolated normalization review closed with approval conditions

- Completed the exhaustive architectural review of `docs/Working/quantity-normalization-design.md` and approved the direction **with conditions**.
- Locked the design correction that §0 supersedes the competing §3.6 / §3.7 / §7 Q2 descriptions, so the doc must carry one canonical bounds-storage story.
- Confirmed the key follow-up requirements: expression-form-scoped `IntervalOf` scaling, normalized reads in `GetFieldBounds`, normalized `StaticMagnitude` in trusted-fact extraction, and a decision on event-arg bound parity.
- Cross-agent note: George's exhaustive gap audit proves Slices 19-21 are necessary but not exhaustive; implementation planning must account for the wider interpolated qualifier/default surface.

### 2026-05-14T17:10:32.283-04:00 — Diagnostic enforcement alignment recorded as a three-layer model

- Confirmed the enforcement mission did **not** compound compiler/runtime duplication; most wired diagnostics are compile-time-only structural checks.
- Recorded the canonical three-layer model: compiler diagnostics, ingress validation, and defense-in-depth faults linked through `[StaticallyPreventable]`.
- Captured two durable follow-ups: ingress validation should become a deliberate surface for quantity/choice/dynamic-qualifier checks, and catalog-mediated dispatch remains the preferred alignment pattern.
- Preserved the companion implementation-notes record so future sessions can recover enforcement reality without re-auditing the mission.
