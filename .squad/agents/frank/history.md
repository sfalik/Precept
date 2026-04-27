## Core Context

- Owns the core DSL/runtime architecture: parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the 2026 combined compiler/runtime design consolidation, proof/fault boundary hardening, catalog-consumer drift analysis, and canonical design-doc promotion work.

## Learnings

- D3's closed-world access default (read unless explicitly opened to write) is a structural safety property, not just a convenience choice. Inverting it to write-by-default changes the omission failure mode from safe (locked) to unsafe (exposed).
- Computed fields align naturally with D3's read default. A universal write default creates a hidden exception for computed fields that weakens inspectability.
- Runtime boundaries should be described by dependency direction, not by claiming that no analysis knowledge crosses. Lowering can and should carry runtime-native residue derived from compile-time analysis.
- Catalog completeness is no longer the main bottleneck; consumer drift is. The highest-value work is removing hardcoded language knowledge from checker, LS, and tooling consumers.
- Parser and lexer algorithms should remain hand-written, but vocabulary tables, precedence data, and classification sets should derive from catalog metadata wherever possible.
- Proof and safety work fit Precept as bounded abstract interpretation over the existing narrowing pipeline, not as a general SMT-backed system.
- The clean consumer split is stable: language-intelligence surfaces read `CompilationResult`; execution and preview surfaces read lowered `Precept`.
- Constraint evaluation and proof/fault are sibling contracts, not one pseudo-validation system: runtime constraint plans govern expected outcomes, while faults remain impossible-path backstops.
- The action family and naming rule are stable architectural memory: three semantic shapes only, with semantic field names instead of syntax-shaped names.
- MCP/CLI surface changes are operating-model decisions. Repo-local development needs one authoritative source-first definition with client-specific projections.

## Recent Updates

### 2025-07-14 — Per-field `readonly` proposal analysis
- Rejected the proposal to invert D3 into a write-by-default model with field-level `readonly`.
- Locked the rationale: conservative defaults, auditability, computed-field consistency, and domain-language positioning all favor the existing `write`-opens-exceptions model.
- If verbosity relief is ever needed, the acceptable lane is narrower sugar such as `write all except ...`, not a default inversion.

### 2026-07-17 — Combined design comprehensive revision
- Applied team review feedback to `combined-design-v2`, adding parser specificity, flat evaluation-plan commitments, versioning/restore clarifications, innovations callouts, and new grammar/MCP sections.
- Promotion outcome: the revised working doc became `docs/compiler-and-runtime-design.md`, replacing the short-form version and absorbing its surviving rationale.

### 2026-04-28 — Combined design boundary and philosophy revisions
- Corrected the overclaimed "nothing crosses the boundary" language and recentered the document on Precept's philosophy and guarantee rather than on defending a split.
- Logged that descriptors and lowered runtime-native shapes legitimately carry selected analysis knowledge across the lowering boundary.

### 2026-04-26 — Catalog audit and doc promotion lane
- Confirmed catalog surfaced-type coverage was largely complete and that the bigger risk is consumer drift.
- Promoted the combined design doc to its canonical location and kept code, not design prose, as the source of truth for concrete signatures.

### 2026-04-24 — Precept.Next pre-TypeChecker gate
- Found TypeChecker start blocked by contract scaffolding gaps: hollow TypedModel surface, nullable SyntaxTree root mismatch, missing diagnostics, and no SourceSpan→SourceRange bridge.
