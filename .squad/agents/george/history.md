## Core Context

- Owns the core DSL/runtime: parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects the fire/update/inspect pipeline contract and keeps runtime behavior aligned with docs, tests, and MCP output.
- Historical summary (pre-2026-04-13): led runtime feasibility and implementation analysis for guarded declarations, event hooks, computed fields, and verdict-modifier semantics.

## Learnings

- Runtime/design gaps should be separated cleanly from philosophy decisions; product-identity calls belong to Shane.
- Recompute-style features succeed when insertion points are explicit across Fire, Update, and Inspect.
- Documentation must describe implemented pipeline stages exactly, especially around editability, hooks, and validation order.

## Recent Updates

### 2026-04-12 — Issue #17 computed fields feasibility
- Confirmed computed fields are feasible with additive parser/model/runtime work and a single recomputation helper inserted before constraint evaluation.

### 2026-04-11 — Issue #14 documentation sync
- Synced language, runtime, editability, MCP, and README docs for declaration `when` guards after implementation.
