## Core Context

- Owns architecture, system boundaries, and review gates across the runtime, tooling, and documentation surfaces.
- Core architectural discipline: keep MCP tools as thin wrappers, keep docs honest about implemented behavior, and document open decisions instead of inventing values.
- Technical-surface work flows through Elaine (UX), Peterman (brand compliance), Frank (architectural fit), then Shane (sign-off).
- README and brand-spec changes should reflect actual runtime semantics, not speculative future behavior.

## Recent Updates

### 2026-04-05 - Elaine delivered README hero draft for issue #4
- Elaine produced `brand/readme-hero.svg` and filed the review spec formerly located at `.squad/decisions/inbox/elaine-readme-hero.md`.
- Architectural follow-up is now focused on SVG handoff fit, GitHub constraints, and final README wiring sequence.
### 2026-04-06 - Architectural knowledge refresh
- Reconfirmed runtime boundaries, DSL semantics, and the need to keep implementation, docs, and tool wrappers aligned.
- Key learning: architectural review is most valuable when it names blockers, explicit tradeoffs, and the narrowest safe execution path.

### 2026-04-07 - brand-spec section 1.4 palette structure review
- Validated the split between identity palette rules and syntax-surface rules.
- Key learning: surface documentation should separate universal contracts from local rendering details.

### 2026-04-08 - SVG hero architecture proposal (issue #4)
- Produced architecture proposal for replacing the README hero code block with a branded SVG visual.
- Key constraints documented: GitHub camo proxy strips `<style>`, `<script>`, `<foreignObject>`, and external resources. All styling must be inline SVG attributes.
- Recommended standalone `brand/readme-hero.svg` referenced via `<picture>` pattern in README — not inline SVG in markdown (sanitizer too aggressive).
- Font constraint: Cascadia Cove cannot render in GitHub-served SVGs; recommended `font-family="monospace"` with path-outline escalation as a Shane decision point.
- Key learning: GitHub SVG rendering has two very different sandboxes — inline-in-markdown (aggressive sanitizer) vs. referenced file via camo proxy (permissive but no fonts, no scripts). Architecture decisions must account for which sandbox applies.
- Key learning: For brand-controlled SVGs on GitHub, separate the diagram (SVG) from the text (markdown) to keep both editable and searchable independently.


