## Core Context

- Owns the core DSL/runtime architecture: parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary (pre-2026-04-13): led runtime feasibility and implementation analysis for guarded declarations, event hooks, computed fields, verdict-modifier semantics, and related issue/design-review work.

## Learnings

- Catalog completeness is no longer the main bottleneck; consumer drift is. The biggest remaining leverage is removing hardcoded consumer knowledge in the checker, language server, and tooling.
- Parser and lexer should stay hand-written at the algorithm level, but their vocabulary tables, precedence data, and classification sets should derive from catalog metadata wherever possible.
- Type-checker catalog integration has three highest-value moves: replace `OperatorTable` with `Operations`, move widening to `TypeMeta.WidensTo`, and close parser/checker enum-bridge gaps.
- Proof and safety work fits Precept best as bounded abstract interpretation over the existing narrowing pipeline, not as a general SMT-backed system.
- MCP/CLI surface changes are operating-model decisions. Repo-local development must have one authoritative source-first definition with client-specific projections, not three hand-authored contracts.

## Recent Updates

### 2026-04-27 — MCP operating-model decision closed the dual-surface change
- Treated the Copilot CLI migration as an operating-model change, not a casual config edit.
- Locked the three-surface boundary: repo-root `.mcp.json` for Copilot CLI, `.vscode/mcp.json` for VS Code workspace development, and `tools/Precept.Plugin/.mcp.json` for the shipped payload.
- Reaffirmed that repo-local source-first behavior remains `node tools/scripts/start-precept-mcp.js`, and that the `github` server should not be mirrored into the root CLI file.

### 2026-04-26 — Cross-catalog invariants and analyzer direction
- Catalog audit confirmed surfaced type coverage is complete; the real correctness bug was `Period` missing `EqualityComparable`, and the real architecture debt is consumer drift.
- Enumerated 37 cross-catalog invariants, 16 intra-catalog structural invariants, and the helper surface needed to enforce them.
- The final queue now favors infrastructure-building analyzer work first, especially trait↔operation consistency, because it unlocks the rest of the sweep.

### 2026-04-25 — Catalog-driven pipeline and parser/lexer review
- Reassessed parser metadata-drivenness: grammar stays hand-written, but vocabulary tables and precedence maps should become catalog-derived.
- Confirmed the type checker is the highest-value catalog consumer after diagnostics, with `OperatorTable` and widening logic as the strongest duplication targets.
- Kept the architecture rule explicit: catalogs own language knowledge; stages own algorithms.

### 2026-04-24 — Precept.Next design and contract reviews
- Approved the early TypeChecker slice work while flagging the remaining design/doc mismatches that block a faithful implementation.
- Logged four blockers in the broader docs.next review: type naming drift, numeric-lane contradictions, incomplete function catalog documentation, and typed-constant validation-stage drift.

### 2026-04-18 to 2026-04-19 — Proof engine and type-checker design gate
- Reworked the proof-engine planning docs into the unified architecture and used that design baseline to ground issue #118/type-checker decomposition work.
- Kept Track B design-review discipline explicit: design documents first, implementation plans second.
