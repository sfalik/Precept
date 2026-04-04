## Core Context

- Owns the core DSL/runtime: parser, type checker, graph analysis, runtime engine, and authoritative language semantics.
- The engine flow is parser to semantic analysis to graph/state validation to runtime execution. Fire behavior must stay consistent with diagnostics, README examples, and MCP output.
- Key runtime areas to protect: constraints, transition guards, event assertions, collection hydration, edit rules, and diagnostic catalogs.
- Documentation should describe the six-stage fire pipeline and implemented semantics accurately, without inventing capabilities.

## Recent Updates

### 2026-04-04 - DSL pipeline overview
- Consolidated the runtime mental model, major constraint categories, fire stages, and edge cases for downstream agents.
- Key learning: Precept's value is structural integrity across state, data, and rules; every outward-facing description should preserve that unified model.

### 2026-04-06 - README restructure proposal review
- Checked that proposed README/API explanations matched the real runtime surface.
- Key learning: the quickest way to damage trust is to let public examples diverge from actual parser/runtime behavior.
