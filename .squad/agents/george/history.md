## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Active durable baseline: parser/type-checker work should stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.

## Learnings

- The checker start point is locked: pre-Slice 0 typed shapes first, existing `Operations` multi-candidate lookup APIs stay canonical, `TypedInputAction` needs an explicit secondary-role discriminator, and `[HandlesCatalogMember]` ownership moves from stub to real handlers slice by slice.
- GAP-060 and GAP-061 are durable parser hygiene rules: variant-action arms hidden inside shape-specific methods must throw when unreachable, and parser-facing catalogs should expose O(1) indexes for the token axis the parser actually queries.
- CI function follow-through is now metadata-native: checker/tooling consumers resolve `~startsWith` and `~endsWith` through real FunctionKind/FunctionMeta entries rather than parser-side naming conventions alone.
- Shared-environment build discipline matters: targeted build/test commands are more reliable than full-solution runs when external file locks interfere with the workspace.

## Recent Updates

### 2026-05-02T21:58:21Z — Canonical review accepted and compacted
- Frank accepted George's checker review, resolved the open pre-requisites, and left transitive widening rejected; the canonical checker plan is now implementation-ready.
- Kramer and Soup-Nazi constraints are now part of the active baseline: keep typed models derivation-first and treat the 450-550 test envelope plus 3 non-negotiable gates as required follow-through.

### 2026-05-02 — Active implementation snapshot
- Keep the parser baseline from Iteration 10 live: no fake variant-action constructors in nested switches, and no parser-side linear scans where a catalog index belongs.
- The checker/evaluator next step remains explicit: partial-result error recovery, qualifier propagation, event-arg scope in event handlers, and explicit slice ownership for method/interpolated forms are no longer open design questions.

### 2026-05-02 — Historical Summary (fully compacted)
- Older active-history detail was moved to history-archive.md during Scribe closeout to keep George under the 15 KB gate.
- Use the archive for the Phase 2 closeout trail, parser-gap implementation sequence, and earlier analyzer-shipping notes.
