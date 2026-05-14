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

### 2026-05-14T17:37:50.029-04:00 — Design doc resolution pass: all 6 conditions resolved

- Completed the design document resolution pass on `docs/Working/quantity-normalization-design.md`, resolving all six §5.5.6 conditions in a single edit pass.
- **Condition 1:** SUPERSEDED markers verified on §3.6, §3.7, §7 Q2 — §0's "store both" is the single authoritative bounds-storage design.
- **Condition 2:** Replaced "universal post-step" in §0 Q6 with expression-type-dispatched `TryGetStaticScalingFactor` pseudocode. Added constraint table showing which expression types scale vs. which are excluded. This is the critical double-normalization prevention mechanism.
- **Condition 3:** Added `GetFieldBounds` fix to Slice 16 spec — reads `NormalizedDeclaredMin ?? DeclaredMin` with null fallback.
- **Condition 4:** Added `TryGetStaticNumericValue` fix to Slice 16 spec — normalizes `StaticMagnitude` via `TryGetStaticScalingFactor` before returning trusted facts.
- **Condition 5:** Decided Option (a) for `TypedEventArg` — parallel `NormalizedDeclaredMin/Max` fields, architecturally consistent with `TypedField`. Added Slice 15b spec.
- **Condition 6:** Updated `NumericInterval.Scale` to `Scale(decimal factor)` in Revised Key Types and Slice 14. Factor conversion happens once in `TryGetStaticScalingFactor`.
- Added §5.6 Extended Slice Details (Slices 22–26) from George's gap audit with full objective/files/approach/tests/dependencies for each.
- Replaced George's §0.6 header with Frank's Design Resolution Summary including the condition resolution table and implementation gate clearance.
- Key pattern: the `TryGetStaticScalingFactor` helper is the single dispatch point for all expression-type → scaling-factor decisions. This is the design's core invariant — one function, one place, no scattered switching.

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

### 2026-05-14T17:48:42.442-04:00 — Doc-sync slice (Slice 27) added to quantity normalization design

- Audited all canonical documentation surfaces for staleness after quantity normalization (Slices 14–21).
- **Needs updates:** `docs/language/precept-language-spec.md` (§0.6 + §5 — add unit-aware normalization to proof engine contract), `docs/compiler/proof-engine.md` (obligation record, interval source table, Strategy 6 normalization), `docs/Working/interval-proof-engine-design.md` (tracker cross-ref, interval table annotations, obligation parameter annotations), `docs/runtime/runtime-api.md` (three-layer enforcement model from §0.5 needs a canonical home), MCP DTO (`CompileProofObligationDto` gains `NormalizedDeclaredMin/Max`).
- **Confirmed clean:** `docs/language/catalog-system.md` (catalog describes types, not comparison behavior), `README.md` (no quantity/normalization content), `docs/philosophy.md` (no scope change), `samples/` (bug fix is in compiler, samples unchanged), `docs/mcp/` (directory doesn't exist).
- The three-layer enforcement model (compile-time / ingress / defense-in-depth) identified in §0.5 is the most significant doc gap — it's a named architectural concept without a canonical home in the published docs.

## Learnings

### 2026-05-14T17:51 — PRE0027 Diagnosis: No Errors Found
- Investigated suspected PRE0027 (DuplicateArgName) errors in sample files. Result: **no PRE0027 errors exist** anywhere in the repository.
- The actual error in `samples/Test.precept` is PRE0078 (interval overflow), which is pre-existing — the literal `6 [lb_av]` already violated `max '5 kg'` before George's edit.
- George's modification (changing `'6 [lb_av]'` to `'{test2} [lb_av]'`) changed the proof shape from concrete-interval to unbounded-interval but did not introduce a new error category.
- Recommendation: revert Test.precept; if interpolated-quantity test cases are needed for normalization work, create a new sample with satisfiable bounds.
- Diagnosis written to `.squad/decisions/inbox/frank-pre0027-diagnosis.md`.
