# Diagram Color Mapping — Section Recommendation

**Author:** Elaine (UX/Design)
**Date:** 2025-07-16
**Requested by:** Shane
**Status:** Recommendation — awaiting Shane decision before editing brand-spec

---

## The Problem

§2.2 State Diagram specifies the compile-time structural diagram: node shapes for lifecycle roles (circle, rounded rect, double-border), locked indigo borders, violet state names, cyan event labels, and three event edge styles (solid, dashed, self-loop). This is complete and correct for the **static** diagram.

What's missing is a dedicated specification for how **colors map to diagram elements** — both the structural layer and the runtime verdict overlay. Currently:

1. **Static element colors are scattered.** Node borders, state name text, event labels, and transition arrows each have a hex value mentioned inline in §2.2's prose and tables, but there's no single reference table that maps every diagram element to its locked color. An implementer has to read the full section and mentally reconstruct the mapping.

2. **Runtime verdict overlay is unspecified.** The actual implementation (`inspector-preview.html`) uses three edge color states: enabled (green `--edge`), blocked (red `--edge-error`), and muted (gray `--edge-muted`). None of this appears in the brand-spec. The implementation also uses a glow effect on active transitions. None of this is documented.

3. **Current state highlighting is unspecified.** When the inspector is active, the current state node needs a visual distinction from other nodes. The spec doesn't say how — color, border weight, fill, glow? The implementation likely handles this, but the brand-spec is silent.

4. **Canvas and node fill colors are implicit.** The SVG example uses `fill="none"` for nodes and a `#0c0c0f` rect for the canvas. These aren't declared as specs — they're just what the example happens to do.

5. **The implementation is drifting from the palette.** The webview CSS variables use `#1FFF7A` (enabled), `#FF2A57` (blocked), and `#6D7F9B` (muted) — none of which are locked system colors. The spec should be explicit so implementation can align.

---

## 1. Where Should Diagram Color Mapping Live?

**Recommendation: A new `<h3>` subsection within §2.2, titled "Diagram color mapping."**

### Why not §1.4?

§1.4 is the brand identity color system — "what colors exist and what they mean." The planned restructure (per my prior review, Frank's review, and Peterman's review) moves it toward a compact Semantic Family Reference table plus the 8+3 brand palette card. Diagram-specific element mappings don't belong there because:

- They describe *how one specific surface applies the palette*, not what the palette is.
- They reference diagram-specific concepts (node borders, edge arrows, canvas) that only exist in §2.2.
- §1.4 already has a scope problem with syntax-specific content; adding diagram-specific content would compound it.

### Why not §2.1?

§2.1 is the syntax editor surface. Diagrams are a separate surface with different rules — they don't use syntax-layer colors, they use shape instead of typography for lifecycle, and they have a runtime overlay that syntax doesn't.

### Why not a new §2.2.1?

The brand-spec doesn't use the X.X.1 pattern within visual surfaces (§2.1 Syntax Editor doesn't have §2.1.1). Adding a subsection number would create a hierarchy inconsistency. A new `<h3>` block within the §2.2 card is consistent with how §2.1 and §2.3 are structured — they use `<h3>` headings for Color application, Typography application, Accessibility notes, etc.

### Where within §2.2?

After the existing "Event lifecycle types" table and before any accessibility/AI-first notes (which §2.2 currently lacks but should get). The placement logic:

1. Purpose (what the diagram is) — *existing*
2. Color application (general principle) — *existing*
3. No lifecycle tints callout — *existing*
4. Shape cards (Initial, Intermediate, Terminal, Flow) — *existing*
5. SVG example — *existing*
6. State lifecycle roles table — *existing*
7. Event lifecycle types table — *existing*
8. **→ NEW: Diagram color mapping** (element-by-element color spec)
9. **→ NEW: Runtime verdict overlay** (how colors change during inspection)
10. Accessibility notes — *should be added*
11. AI-first note — *should be added*

---

## 2. What Content Belongs Here vs. General Color System vs. Syntax Highlighting?

### The three-level split

| Level | Section | What it answers | Diagram example |
|-------|---------|----------------|-----------------|
| **Identity** | §1.4 Color System | "Violet means states" | §1.4 says violet is for states across all surfaces |
| **Syntax** | §2.1 Syntax Editor | "State tokens are `#A898F5`, italic when constrained" | §2.1 says how the editor renders state names in code |
| **Diagram** | §2.2 State Diagram | "State name text in nodes is `#A898F5`; constrained states get italic" | §2.2 says how the diagram renders state names in nodes |

### Content that belongs in the new diagram color mapping section

Anything that answers: **"What color/style does this specific diagram element use, and why?"**

- Element → hex → rationale for every visible diagram component
- Runtime overlay rules (how colors change based on verdict status)
- Current state visual treatment
- Canvas and fill specifications
- Hover/interaction color changes

### Content that does NOT belong here

- **Hue family identities** ("violet = states") → stay in §1.4
- **Token-level keyword assignments** ("field is bold indigo") → stay in §2.1
- **Cross-surface usage guidance** ("brand-light is for hover states and accents") → stay in §1.4.1
- **Shape specifications** (circle = initial, double-border = terminal) → already in §2.2's existing tables

---

## 3. Diagram-Specific Mappings to Document

### Layer 1: Structural (compile-time, always visible)

These colors are determined by the precept definition alone. They don't change based on runtime state.

| Element | Color | Hex | Source | Notes |
|---------|-------|-----|--------|-------|
| **Canvas background** | — | `#0c0c0f` | Brand bg | Matches all dark-mode surfaces |
| **Initial node border** | Semantic indigo | `#4338CA` | Structure family | Heavier weight (2.5px); origin emphasis |
| **Intermediate node border** | Semantic indigo | `#4338CA` | Structure family | Standard weight (1.5px) |
| **Terminal node outer border** | Grammar indigo | `#6366F1` | Structure family | Double-border treatment; outer is lighter |
| **Terminal node inner border** | Grammar indigo | `#6366F1` | Structure family | `opacity: 0.3` on outer ring |
| **Node fill** | Transparent | `none` | — | Nodes are outlined, not filled. Background shows through. |
| **State name text** | Violet | `#A898F5` | States family | Constrained states use italic (matches syntax) |
| **Transition arrow stroke** | Grammar indigo | `#6366F1` | Structure family | Flow belongs to structure grammar |
| **Arrow marker/head** | Grammar indigo | `#6366F1` | Structure family | — |
| **Event label text** | Cyan | `#30B8E8` | Events family | Matches syntax highlighting event hue |
| **Guard annotation text** | — | TBD | — | Dashed edges for conditional events; label styling needs spec |
| **Legend text** | Muted | `#52525b` | — | De-emphasized; structural aid only |

### Layer 2: Runtime verdict overlay (inspector-active, changes based on state + event status)

These colors appear when the diagram is paired with an active inspector instance. They show what would happen if you fired each event from the current state.

| Element | Condition | Color | Hex | Notes |
|---------|-----------|-------|-----|-------|
| **Current state node** | Instance is in this state | TBD | TBD | Needs visual distinction — options: brighter border, fill tint, glow, badge. See discussion below. |
| **Enabled transition edge** | Firing this event from current state would succeed | Enabled emerald | `#34D399` | Verdict color; replaces structural indigo on this edge |
| **Blocked transition edge** | Firing this event from current state would be rejected | Blocked rose | `#F87171` | Verdict color; dashed stroke recommended |
| **Warning transition edge** | Event has unmatched guards or partial match | Warning amber | `#FCD34D` | Verdict color |
| **Non-current-state edges** | Edges from states other than the current state | Muted | TBD (see below) | De-emphasized so current-state edges dominate |
| **Enabled transition glow** | Active transition dot/pulse effect | Enabled emerald | `#34D399` | Animated indicator on the enabled edge |
| **Transition target node** | State that would be reached by an enabled transition | TBD | TBD | Subtle highlight on the destination node when hovering |

### Layer 3: Interaction states (hover, focus, selection)

| Element | Trigger | Color change | Notes |
|---------|---------|-------------|-------|
| **Edge hover** | Mouse over edge or event label | Brighten stroke; raise opacity | Makes the hovered transition stand out from parallel edges |
| **Event label hover** | Mouse over event label | Brighten; show tooltip? | Pairs with inspector event list highlighting |
| **Node hover** | Mouse over state node | TBD | May show state's available transitions; coordinate with inspector |

### Open color decisions needed

1. **Current state indicator.** The spec must decide how the "you are here" state is shown. Options:
   - **Option A:** Fill tint — a faint indigo or violet fill on the current node (`#1e1b4b` at 20–30% opacity)
   - **Option B:** Border glow — a subtle drop-shadow or outer glow in indigo
   - **Option C:** Badge/dot — a small indicator dot in emerald on the current node
   - **Recommendation:** Option A (fill tint). It's the most immediately scannable, works at small diagram sizes, and doesn't add a new color or element. The tint `#1e1b4b` is already used as the background for the shape cards in §2.2.

2. **Muted edge color.** Non-current-state edges need a defined color. The implementation uses `#6D7F9B`, which is not in the locked system. Options:
   - `#27272A` (border) — too close to the canvas; edges would nearly vanish
   - `#71717A` (text-muted) — reasonable; matches the muted tier
   - `#52525b` (Tailwind zinc-600) — what the legend already uses; not in the 8+3 system
   - **Recommendation:** `#71717A` (text-muted). It's in the locked palette, clearly subordinate to verdict colors and structural indigo, and visible enough to provide graph continuity.

3. **Guard annotation styling.** Conditional events use dashed edges but the guard text label (e.g., "when Amount > 0") isn't color-specified. Should it use:
   - Cyan (matching the event label) — implies it's event-related
   - Slate (matching data names) — implies it's a data condition
   - Gold (matching rule messages) — implies it's a human-readable constraint
   - **Recommendation:** Slate `#B0BEC5` for guard text. Guards reference data fields, and using slate creates a visual link to the data lane without introducing a new hue.

---

## 4. Interaction with the Planned §1.4 / §1.4.1 / §2.1 Cleanup

### Summary of the planned cleanup (from prior reviews)

- §1.4 slims to: 8+3 brand palette card + compact Semantic Family Reference table + verdicts + hue map
- §1.4.1 trims redundancy, keeps cross-surface usage guidance
- §2.1 absorbs: syntax category cards, constraint signaling table, full token-level spec

### How the new diagram color mapping section interacts

1. **§1.4 → §2.2 reference chain.** After the restructure, §1.4's Semantic Family Reference table will say "Structure = Indigo, States = Violet, Events = Cyan" at the identity level. §2.2 will reference that table and show exactly how those families are applied to diagram elements. This is the same pattern as §2.1 referencing §1.4 for syntax highlighting. Clean separation.

2. **§1.4.1 Color Roles table.** Currently says "diagram state borders" under brand `#6366F1` and "state names in diagrams and syntax" under brand-light `#818CF8`. After the restructure:
   - The Color Roles table should use brief mentions + cross-references: "diagram borders and edges (see §2.2)" rather than inline color specs.
   - This prevents §1.4.1 from becoming a third source of truth for diagram colors.
   - **Note:** `#818CF8` (brand-light) is currently listed as the state name color in diagrams, but §2.2 and brand-decisions.md both use `#A898F5` (violet). This is an existing discrepancy in §1.4.1 that the cleanup should resolve.

3. **§2.1 stays clean.** The diagram color mapping section adds no content to §2.1. The two surfaces don't share implementation detail — they share hue families (from §1.4), but their application rules are completely independent. The "no lifecycle tints" principle in §2.2 is the inverse of §2.1's "no lifecycle colors, use italic" principle — same philosophy, different mechanism.

4. **Verdict colors get a clear home.** Currently, verdicts are defined in §1.4 (brand-level), mentioned in §1.4.1 (cross-surface usage), used in §2.2 (diagram runtime overlay), and used in §2.3 (inspector). The restructure should make §1.4 the definition ("these are the three verdict colors") and §2.2/§2.3 the application ("here's how they appear in diagrams/inspector"). The new diagram color mapping section makes the §2.2 application explicit rather than implied.

5. **Implementation alignment.** The current webview implementation uses off-system colors for diagram edges (`#1FFF7A`, `#FF2A57`, `#6D7F9B`). Once the diagram color mapping section locks the correct values (`#34D399`, `#F87171`, and a chosen muted color), Kramer has a clear spec to align the implementation to.

---

## 5. Proposed Section Structure

Within §2.2 State Diagram, after the existing "Event lifecycle types" table:

```
<h3>Diagram color mapping</h3>

[Intro paragraph: "Every visible diagram element uses a locked color from the 
brand system. This table is the single source of truth for diagram colors."]

[Structural elements table — 12 rows covering canvas, borders, fills, text, 
arrows, markers, legend]

<h3>Runtime verdict overlay</h3>

[Intro paragraph: "When paired with an active inspector instance, the diagram 
shows runtime status using verdict colors. These overlay the structural colors 
for edges originating from the current state."]

[Verdict overlay table — 6–8 rows covering current state, enabled/blocked/warning 
edges, muted edges, glow effects, target node highlights]

[Current state indicator decision callout — whichever option Shane picks]

[Cross-reference: "Verdict colors are defined in Brand Identity §1.4. 
For inspector-panel verdict usage, see §2.3."]
```

---

## 6. Discrepancies Found During This Review

| Location | Current value | Expected value | Issue |
|----------|--------------|----------------|-------|
| §1.4.1 Color Roles, brand-light row | "state names in diagrams" using `#818CF8` | State names are `#A898F5` (violet) in §2.2 and brand-decisions.md | brand-light is an accent, not the diagram state name color |
| Webview `--edge` | `#1FFF7A` | `#34D399` (Enabled emerald) | Off-system green |
| Webview `--edge-error` | `#FF2A57` | `#F87171` (Blocked coral) | Off-system red |
| Webview `--edge-muted` | `#6D7F9B` | TBD (recommend `#71717A`) | Off-system gray |
| Inspector `--ok` | `#1FFF7A` | `#34D399` | Same drift as diagram edges |
| Inspector `--err` | `#FF2A57` | `#F87171` / `#FB7185` | Same drift as diagram edges |
| brand-decisions.md Blocked | `#F87171` | brand-spec.html error `#FB7185` | Known hex discrepancy (noted by Frank) |

The diagram color mapping section, once locked, gives Kramer a single reference for bringing these into compliance.

---

## Summary

The diagram needs its own explicit color mapping section — not because the information doesn't exist, but because it's currently scattered across §1.4 prose, §1.4.1 roles table, §2.2 inline mentions, and the implementation (which has drifted). A dedicated `<h3>` subsection within §2.2, structured as two tables (structural elements + runtime verdict overlay), gives implementers a single source of truth and makes the implementation-to-spec gap visible and closeable.

The new section follows the same pattern the team has already agreed to: §1.4 owns identity ("violet = states"), §2.x sections own application ("state name text in diagram nodes is `#A898F5`"). It introduces no new abstraction layers, no new section numbering, and no new colors — it just makes implicit mappings explicit.
