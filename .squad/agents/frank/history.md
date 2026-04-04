## Core Context

- Owns architecture, system boundaries, and review gates across the runtime, tooling, and documentation surfaces.
- Core architectural discipline: keep MCP tools as thin wrappers, keep docs honest about implemented behavior, and document open decisions instead of inventing values.
- Technical-surface work flows through Elaine (UX), Peterman (brand compliance), Frank (architectural fit), then Shane (sign-off).
- README and brand-spec changes should reflect actual runtime semantics, not speculative future behavior.

## Recent Updates

### 2026-04-06 - Architectural knowledge refresh
- Reconfirmed runtime boundaries, DSL semantics, and the need to keep implementation, docs, and tool wrappers aligned.
- Key learning: architectural review is most valuable when it names blockers, explicit tradeoffs, and the narrowest safe execution path.

### 2026-04-07 - brand-spec section 1.4 palette structure review
- Validated the split between identity palette rules and syntax-surface rules.
- Key learning: surface documentation should separate universal contracts from local rendering details.
