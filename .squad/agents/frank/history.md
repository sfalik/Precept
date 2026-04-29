## Core Context

- Owns the core DSL/runtime architecture across parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the combined compiler/runtime design consolidation, access-mode redesign, parser catalog-shape direction, catalog extensibility hardening, and the spike-mode operating model.

## Learnings

- Conservative defaults are structural guarantees: write/edit surfaces open exceptions, they do not become the baseline by omission.
- Metadata belongs in catalogs when consumers need per-member knowledge; pipeline/tooling drift comes from hardcoded parallel copies.
- Parser algorithms stay hand-written, but vocabulary tables, precedence data, and disambiguation metadata should derive from catalog truth where possible.
- Authoring consumers read `CompilationResult`; execution/preview consumers read lowered `Precept`; runtime-native lowered data may intentionally preserve selected analysis residue.
- When a construct family splits, verification must cover catalog entries, AST nodes, build paths, routing tests, slot-order tests, and regression anchors.
- A complete design evaluation requires reading implementation alongside design; wrapper-node and remediation-only additions can be correct even when absent from the design docs.
- Spike mode only becomes enforceable when routing, ceremony guards, durable wisdom, and contributor workflow docs are patched together; leaving one surface untouched lets the coordinator regress into PR-demanding behavior during exploratory work.

## Recent Updates

### 2026-04-29 — PRECEPT0018 correctness gate closed
- George's follow-up commit `e7a643d` added the three missing required PRECEPT0018 regression tests plus the two advisory visibility anchors, closing Frank's only blocking finding (B1).
- Final state recorded durably: analyzer, attribute, all 3 `[AllowZeroDefault]` placements, and all 23 enum fixes were already correct; the lane is now blocked on nothing.

### 2026-04-28 — Spike mode first-class across squad workflow
- Patched routing, ceremonies, durable wisdom, and `CONTRIBUTING.md` so spike activation/exit are explicit, spike branches follow `spike/{kebab-description}`, PR-demanding ceremonies are suppressed while `spike_mode: true`, and exploratory work exits through deliberate closeout.

### 2026-04-28 — Catalog extensibility closeout
- Catalog-driven extensibility audit identified the parser hardening gaps; plan review blocked weak spots; deep re-review caught wildcard switches defeating CS8509; final B1-B7 re-review approved the fixed implementation and the remaining enum deviations as safe.

### 2026-04-28 — Parser design loop closed
- Evaluated parser design v5-v8, approved the remediation batch, authored the permanent `docs/compiler/parser-v2.md` reference, and finished the cross-surface consistency audit that aligned spec, parser docs, catalogs, diagnostics, and representative samples.

### 2026-04-28 — Vision/spec migration boundary locked
- Audited the language vision against the live spec, identified two contradictions plus the philosophy-first content that must migrate before archival, and recommended deferring vision archival until the spec absorbs the identity-bearing material.

### 2026-04-29 — Spike-mode decision recorded durably
- Scribe merged the spike-mode architectural decision into `decisions.md`, archived aged decision records under the hard gate, and propagated the new operating model into team memory.
