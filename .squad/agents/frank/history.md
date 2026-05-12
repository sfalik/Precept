## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs derive from durable catalog shape rather than enum-identity switches or parallel lists.
- Interpolation work must preserve compile-time guarantees first; plans that trade structural certainty for runtime validation remain philosophically out of bounds.
- Proof and qualifier fixes should stay bounded to catalog metadata and conservative symbolic reasoning rather than speculative provenance systems.

## Live Guidance

- String holes remain out of scope for interpolated typed constants; typed-hole composition is the only acceptable path.
- Slice 6 stays numeric-only, including the single-hole whole-value fallback; qualifier, dimension, modifier, and presence obligations remain declaration-driven.
- Temporal semantics stay with `duration` / `period`; `quantity of 'time'` remains invalid, while temporal-denominated prices stay on `price of ...`.
- MCP/public tooling contracts should continue to expose curated projections rather than raw core catalog records.

## Historical Summary

- 2026-05-11 through early 2026-05-12 established the current durable research baseline: inventory-item root-cause triage, temporal guidance, proof-gap closure assessment, qualifier-path fixes, DTO-free MCP execution planning, and the field-state-guarantee investigation.
- The comma-list spike sequence is now locked at the documentation/research layer: parser-disambiguation docs need correction, MCP surfaces stay catalog-driven, and the spike record captures both the full-scope exploration and the later event-list deferral decision.
- Older detailed batch-by-batch chronology lives in `.squad/decisions.md`; this history keeps only durable architectural guidance plus the latest high-value rulings.

## Recent Updates

### 2026-05-12T14:57:13.598-04:00 — Field-state guarantee investigation

- Confirmed the compiler does not currently enforce state-scoped omit/readonly access during transition or state-hook action resolution.
- Locked the recommended fix shape as a post-resolution validation pass after access modes populate, with dedicated diagnostics instead of speculative state-blind enforcement changes.

### 2026-05-12T15:15:54-04:00 — Comma-list spike reframed and audited

- Captured both the full-scope design exploration and the later state-only/event-deferral revision in the decision ledger so the research trail preserves Shane’s direction changes.
- Exhaustive doc audit locked the critical parser-doc invariant update and confirmed no MCP code change is required because syntax output stays catalog-driven.

### 2026-05-12T17:56:47-04:00 — Hover B2/B3 are V1; B4 deferred

- B2 routing mismatch is a V1 blocker: `HoverHandler.cs` must try construct-span dispatch for rule/ensure/transition/reject/access/omit before generic operator/function/accessor fallbacks.
- B3 mutability honesty is a V1 scope correction, not guarded-access implementation: only unconditional `AccessModes` may feed V1 writable counts/state lists.
- B4 state proof missing-path narrative is deferred until `StateGraph` exposes a stable predecessor-edge explanation; V1 ships only the grounded two-line unreachable-state summary.
- B2/B3 still merge only against a green hover-test baseline.
