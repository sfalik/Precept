# Skill: Brand Icon Spec Alignment

## When to use
When editing, creating, or auditing SVG icons in the brand spec that depict Precept surfaces (diagrams, syntax, inspector).

## Pattern
Brand mark icons are small-scale renderings of their parent surface. They must use the same semantic color mapping and shape vocabulary as the surface they represent.

## Checklist
1. **State node shapes** — Circle = Initial only. Rounded rect = Intermediate. Double-border rect = Terminal.
2. **Node borders** — Use §2.2 indigo shades: `#4338CA` (Semantic) for initial/intermediate nodes, `#6366F1` (Grammar) for terminal double-border.
3. **Flow edges** — Grammar Indigo `#6366F1`. Never Emerald (verdict-only).
4. **Code page borders** — `#6366f1` for visible tablet outlines. Never `#27272a` (barely visible against `#1e1b4b` ground).
5. **Signal colors (Emerald, Amber, Rose)** — Reserved for runtime verdict overlay. Never in static brand marks.
6. **Color key** — Must list only colors actually used in the current icons. Update after every icon change.
7. **Lockup consistency** — The wordmark-context combined icon must match the §1.3 brand mark combined icon exactly.
