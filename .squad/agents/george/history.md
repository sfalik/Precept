## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: led feasibility passes for analyzer/runtime details, parser guardrails, catalog-consumer drift, and diagnostic exhaustiveness discipline.

## Learnings

- The highest implementation payoff comes from eliminating hardcoded parallel copies of catalog knowledge while keeping parser/checker mechanics explicit and hand-authored.
- Exhaustiveness invariants need compile-time or pinned-test enforcement: `BuildNode` switch arms, diagnostic metadata, and slot ordering all need dedicated guards.
- Permanently-locked language invariants require both structural tests and invalid-input diagnostic tests; one without the other leaves a silent gap.
- Disambiguation and recovery rules must name the real mechanism; misleading tests or prose around sync anchors create implementation drift.
- Contract alignment across docs, code, diagnostics, and samples is a prerequisite for trustworthy slice work.

## Recent Updates

### 2026-04-28 — Access-mode migration and shorthand sync
- Confirmed the B4 migration: `Modify`, `Readonly`, `Editable`, and separate `OmitDeclaration` landed cleanly across catalog, samples, and tests.
- Synced the shorthand/AST direction: shared `FieldTarget` shapes stay available to both `modify` and `omit`, while `omit` remains guardless and structurally separate.

### 2026-04-28 — v8 review cycle closed
- george-4 reviewed `docs/working/catalog-parser-design-v8.md` and blocked on 4 concrete issues: missing omit guard diagnostic coverage, unspecified pre-stashed guard handling when routed to `OmitDeclaration`, unclear sync-anchor mechanism wording, and an underspecified 2.1 slice split.
- frank-5 applied all requested fixes. george-5 re-reviewed the targeted areas, verified each fix, and approved v8.
- Phase 1 is complete; proceed to Phase 2 with the corrected v8 document as the canonical parser-design anchor.

