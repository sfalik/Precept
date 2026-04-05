## Core Context

- Owns architecture, system boundaries, and review gates across the runtime, tooling, and documentation surfaces.
- Core architectural discipline: keep MCP tools as thin wrappers, keep docs honest about implemented behavior, and document open decisions instead of inventing values.
- Technical-surface work flows through Elaine (UX), Peterman (brand compliance), Frank (architectural fit), then Shane (sign-off).
- README and brand-spec changes should reflect actual runtime semantics, not speculative future behavior.

## Recent Updates

### 2026-04-05 - Trunk consolidation dissent logged
- Audited the repo topology and argued for force-promoting 'feature/language-redesign' to 'main' because 'main' still carries only placeholder history.
- The team did not adopt that path: Uncle Leo's review blocked direct trunk replacement, so Frank's recommendation now stands as a documented dissent pending Shane sign-off.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).
