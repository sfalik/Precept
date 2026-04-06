# Architectural Review: Diagram Color Mapping Section

**Reviewer:** Frank (Lead/Architect)
**Date:** 2025-07-15
**Requested by:** Shane
**Scope:** Where a dedicated diagram color mapping belongs in brand-spec.html, what it covers, and how it interacts with the planned §1.4 restructure
**Status:** Recommendation — no edits made

---

## Executive Summary

§2.2 State Diagram currently specifies shape semantics (initial/intermediate/terminal) and edge types (transition/conditional/stationary) in detail, but its color specification is scattered: a one-paragraph introduction ("same hue families, not syntax-layer colors"), a callout about lifecycle tints, and hex values embedded silently in SVG attributes. There is no authoritative color mapping table.

This is the same structural gap that the syntax family cards fill for §2.1 — and the gap becomes more obvious once those cards move out of §1.4. The diagram surface needs its own color specification at the same level of authority.

---

## 1. Placement: §2.2, Not §1.4 or §1.4.1

### The principle

The three palette structure reviews (Frank, Elaine, Peterman) converge on a two-level architecture:

- **§1.4 owns identity** — "Violet means states." Brand-level semantic family assignments.
- **§2.x sections own implementation** — "Here's exactly how surface X applies those families."

Diagram color mapping is surface-specific implementation. It answers: "Given the brand families, what color/weight/style does each diagram element get?" That question belongs in §2.2 — the same way syntax token-level coloring belongs in §2.1.

### Why not §1.4?

§1.4 already has a "State diagram coloring" paragraph in brand-decisions.md (5-row table: borders, names, arrows, labels, verdicts). That table is identity-level — it declares family assignments. It should migrate into §1.4's planned Semantic Family Reference table as cross-surface examples. But the implementation detail (which border weight for initial vs. terminal, dashed vs. solid for blocked edges, whether active state gets a fill overlay) is §2.2 material.

### Why not §1.4.1?

§1.4.1 Cross-Surface Color Application is for cross-cutting lookups: "Where does `brand-light` go?" Its color roles table already includes some diagram mentions ("diagram state borders," "state names in diagrams and syntax"). Those brief mentions are appropriate for §1.4.1's cross-cutting role. The full specification — with element-by-element detail, interactive states, and edge styling — belongs in the surface section.

### Why not a separate §2.2.1?

The current spec uses `h3` subsections within `h2` surface sections (e.g., "Purpose," "Color application," "State lifecycle roles" are all h3 within §2.2). A dedicated h3 subsection within §2.2 follows the existing pattern. A separate §2.2.1 would break the numbering pattern (only §1.4 currently uses .x.y numbering). Stay with h3.

---

## 2. Proposed Position Within §2.2

Current §2.2 flow:

```
1. Purpose card
2. "Color application" paragraph + "No lifecycle tints" callout
3. Shape tiles (Initial / Intermediate / Terminal / Flow)
4. SVG diagram example
5. State lifecycle roles table
6. Event lifecycle types table
7. Protocol gap callout
```

The diagram color mapping table should go immediately after item 2 and before item 3. The reader encounters:

1. **What the diagram is** (Purpose)
2. **What colors it uses** (Color application intro → mapping table)
3. **How shapes encode structure** (Shape tiles → lifecycle tables)
4. **A concrete example** (SVG demonstrating the mapping)

This is the natural information architecture: principle → specification → illustration.

### Subsection name

**`Diagram color mapping`** — parallel to the syntax family cards' naming convention (factual, not decorative).

---

## 3. What Belongs Where — Boundary Definitions

### §1.4 Color System (identity-level)

Owns the semantic family reference. After the planned restructure, §1.4 should contain a compact table like:

| Family | Hue | Brand Identity | Surfaces |
|--------|-----|---------------|----------|
| Structure | Indigo | DSL control and grammar | Syntax, diagram, inspector |
| States | Violet | State machine nouns | Syntax, diagram, inspector |
| Events | Cyan | Transition verbs | Syntax, diagram, inspector |
| Data | Slate | Fields, types, values | Syntax, inspector |
| Rules | Gold | Human-readable messages | Syntax only |
| Verdicts | Green / Red / Yellow | Runtime outcomes | Inspector, diagram (never syntax) |

This table says "Violet = states, everywhere." It does NOT say what shade, weight, or style the diagram uses for state names. That's §2.2's job.

### §2.1 Syntax Editor (syntax-level)

After absorbing the family cards: owns token-by-token color + typography (bold/italic/normal) + keyword-to-shade assignments + constraint signaling. Diagram doesn't use these typography rules — it has its own visual language (shape, edge style, weight).

### §2.2 State Diagram (diagram-level)

Owns element-by-element color mapping. Every visual element in the diagram — node, edge, label, fill, legend, overlay — gets an authoritative hex + style specification. Includes both static (always-on) and interactive (runtime/inspector-connected) elements.

### The boundary rule

If a color decision could be different between syntax and diagram without violating brand identity, it's surface-specific. Examples:

- State names are Violet `#A898F5` in both surfaces — **identity-level** (§1.4 declares this).
- State names are italic-when-constrained in syntax but italic-when-constrained in the diagram too — but the *typography rules* are different (syntax uses CSS font-style; diagram uses SVG font-style). The decision is identity-level; the implementation is surface-level.
- Event labels are Cyan `#30B8E8` in both surfaces — identity-level.
- Transition arrows are Grammar Indigo `#6366F1` in diagrams but there's no arrow concept in syntax — **diagram-specific** (§2.2 only).
- Blocked transitions use Error `#FB7185` dashed — **diagram-specific** (§2.2 only).
- Active state gets an indigo fill overlay — **diagram-specific** (§2.2 only).

---

## 4. Minimum Authoritative Mapping

The diagram color mapping table must cover every visual element that appears or could appear in the state diagram, organized by element type.

### 4.1 Static elements (always present in any diagram)

| Element | Color | Hex | Weight / Style | Rationale |
|---------|-------|-----|---------------|-----------|
| Canvas background | Brand bg | `#0c0c0f` | — | Matches authoring background; all contrast ratios computed against this |
| State node border (initial) | Structure · Semantic | `#4338CA` | 2.5px solid | Heavier stroke signals structural importance |
| State node border (intermediate) | Structure · Semantic | `#4338CA` | 1.5px solid | Standard stroke |
| State node border (terminal — inner) | Structure · Grammar | `#6366F1` | 2px solid | Inner border of double-border treatment |
| State node border (terminal — outer) | Structure · Grammar | `#6366F1` | 1px solid, 30% opacity | Outer halo of double-border treatment |
| State node fill | None | transparent | — | Nodes are outlined, not filled — shape reads against dark canvas |
| State name text | States · Violet | `#A898F5` | 600 weight; italic if constrained | Category hue; italic mirrors syntax constraint signaling |
| Transition edge (line) | Structure · Grammar | `#6366F1` | 1.5px solid | Flow is grammar — connective tissue, not semantic driver |
| Transition edge (arrowhead) | Structure · Grammar | `#6366F1` | filled marker | Matches line color |
| Event label (on edge) | Events · Cyan | `#30B8E8` | normal weight | Category hue; labels are verbs naming the transition |
| Guard/condition annotation | Data · Names | `#B0BEC5` | normal weight, smaller font | Guard text references fields/conditions — data family |
| Legend elements | Match respective categories | various | smaller font, muted | Each legend entry uses the color of the category it represents |

### 4.2 Interactive elements (runtime-connected diagram)

When the diagram is connected to the inspector (active instance state):

| Element | Color | Hex | Weight / Style | Rationale |
|---------|-------|-----|---------------|-----------|
| Active state fill | Structure · Semantic | `#4338CA` | 10–15% opacity fill | Subtle fill overlay; the node lights up without overpowering the border |
| Active state border | Structure · Semantic | `#4338CA` | 3px solid | Heavier than default to distinguish current state |
| Enabled transition edge | Verdict · Success | `#34D399` | 1.5px solid | Green = this transition can fire |
| Blocked transition edge | Verdict · Error | `#FB7185` | 1.5px dashed | Red + dash = this transition is prevented |
| Warning/unmatched edge | Verdict · Warning | `#FCD34D` | 1.5px dotted | Amber + dot = guard present, outcome uncertain |
| Enabled event label | Verdict · Success | `#34D399` | normal weight | Label inherits verdict color when interactive |
| Blocked event label | Verdict · Error | `#FB7185` | normal weight | Label inherits verdict color when interactive |

### 4.3 Semantic signals (derived from compile-time analysis)

These are not interactive — they reflect static analysis of the precept definition:

| Signal | Element affected | Visual treatment |
|--------|-----------------|-----------------|
| Constrained state | State name text | Italic (same rule as syntax) |
| Constrained event | Event label text | Italic (same rule as syntax) |
| Dead-end state (no outbound transitions) | Node border | Double-border terminal shape (already specified in lifecycle roles) |
| Orphaned state (unreachable) | Entire node | Reduced opacity (40–50%) — visually de-emphasized |
| Self-loop event (stationary) | Edge | Self-loop arrow returning to same node |

### 4.4 What is NOT in this mapping

- **Data field display** — fields belong in the inspector panel (§2.3), not the diagram.
- **Rule message text** — gold messages are syntax-only. Diagrams don't display `because` strings.
- **Comment text** — comments are editorial and don't appear in diagrams.
- **Typography specifics** (font family, font size) — already specified in §2.2's shape tiles and the SVG example. The color mapping doesn't duplicate them.

---

## 5. Interaction with the Planned Family-Card Move

### The pattern the move establishes

The consensus plan (Frank, Elaine, Peterman all agree) is:

```
§1.4  →  Semantic family identities (compact table)
§2.1  →  Syntax surface color spec (absorbed family cards + constraint signaling)
§2.2  →  Diagram surface color spec (this new section)
§2.3  →  Inspector surface color spec (Elaine's design)
```

Each §2.x surface gets a self-contained color specification that says "here's how this surface applies the §1.4 families." The diagram color mapping section completes this pattern.

### Timing dependency

The diagram color mapping can be added to §2.2 **independently** of the family-card move. It doesn't reference the family cards themselves — it references §1.4's semantic family identities, which exist today and will survive the restructure unchanged. However, it makes more narrative sense to do both in the same pass: the restructure creates the pattern (§2.1 gets its color spec), and the diagram section extends it (§2.2 gets its color spec).

### Cross-references after the restructure

§2.2's color application intro currently says:

> "State diagrams use the same hue families from Brand Identity § 1.4, but they do not use syntax-layer colors."

After the restructure, this should update to:

> "State diagrams apply the semantic family identities from § 1.4. For syntax-specific color application, see § 2.1. The diagram uses shape and edge styling — not typography — to carry structural and constraint information."

This distinguishes the two §2.x surfaces clearly: syntax uses typography (bold/italic), diagrams use geometry (shape/edge style/weight).

---

## 6. The SVG Example as Validation

The existing SVG diagram in §2.2 (lines 890–937) already implements most of this mapping:

| Element in SVG | Color used | Mapping match? |
|---------------|-----------|---------------|
| Initial circle border | `#4338CA` stroke 2.5 | ✅ Matches proposed: Semantic, 2.5px |
| Intermediate rect border | `#4338CA` stroke 1.5 | ✅ Matches proposed: Semantic, 1.5px |
| Terminal double-border inner | `#6366F1` stroke 2 | ✅ Matches proposed: Grammar, 2px |
| Terminal double-border outer | `#6366F1` stroke 1, opacity 0.3 | ✅ Matches proposed: Grammar, 1px, 30% |
| State names | `#A898F5` | ✅ Matches proposed: Violet |
| "Approved" italic | `font-style:italic` | ✅ Matches proposed: constrained state |
| Transition lines | `#6366F1` stroke 1.5 | ✅ Matches proposed: Grammar, 1.5px |
| Arrowheads | `#6366F1` fill | ✅ Matches proposed: Grammar |
| Event labels | `#30B8E8` | ✅ Matches proposed: Cyan |
| Legend blocked line | `#f43f5e` dashed | ⚠️ Uses #f43f5e, not #FB7185 — needs sync |
| Canvas background | `#0c0c0f` | ✅ Matches proposed |

The SVG validates the mapping and becomes the illustration of the specification. One hex discrepancy: the blocked legend line uses `#f43f5e` (Tailwind rose-500) instead of `#FB7185` (the brand error color). This should be corrected.

---

## 7. Hex Discrepancies to Resolve

Three diagram-related color discrepancies exist across source documents:

| Element | brand-decisions.md | brand-spec SVG | brand-spec palette card | Recommendation |
|---------|-------------------|---------------|------------------------|---------------|
| Error/Blocked | `#F87171` | `#f43f5e` (legend) | `#FB7185` | Lock to `#FB7185` (palette card is source of truth) |
| Warning | `#FDE047` | n/a | `#FCD34D` | Lock to `#FCD34D` (palette card is source of truth) |
| Enabled/Success | `#34D399` | n/a | `#34D399` | Consistent ✅ |

The palette card in §1.4 is the source of truth for hex values. brand-decisions.md should be considered historical. The SVG should be updated to match the palette card.

---

## Recommendation Summary

1. **Add a "Diagram color mapping" h3 subsection to §2.2**, positioned between the "No lifecycle tints" callout and the shape tiles. This is the same structural level as other §2.2 subsections.

2. **The mapping table covers four categories:** static elements, interactive elements, semantic signals, and exclusions. This is the authoritative reference for every color decision in the state diagram.

3. **§1.4 does NOT gain a diagram color section.** The semantic family reference table (planned) includes diagram as a surface where families apply, but the implementation detail stays in §2.2.

4. **§1.4.1 keeps its brief diagram mentions** in the color roles table ("diagram state borders," "state names in diagrams"). These are cross-cutting usage notes — exactly what §1.4.1 is for. They don't overlap with §2.2's implementation detail.

5. **The diagram mapping can land independently** of the family-card restructure, but landing them together creates a cleaner narrative: every §2.x surface gets its own complete color specification.

6. **Fix the three hex discrepancies** as part of the same edit pass.

---

## Risk Assessment

**Risk: Overspecification locks the diagram before implementation is complete.**
Mitigation: The SVG example already implements 90% of the static mapping. The interactive elements (active state, verdict overlays) are forward-looking but grounded in the brand-decisions.md mapping and inspector panel design patterns. Nothing proposed is speculative.

**Risk: Overlap with §2.3 Inspector Panel for verdict colors.**
Mitigation: §2.3 uses verdicts in a list context (per-event outcome rows). §2.2 uses verdicts on edges and labels. Different visual forms, same semantic colors. No overlap — they're applying the same family to different surfaces.

**Risk: Guard/condition annotations not yet implemented.**
Mitigation: The mapping declares the color (Data · Names `#B0BEC5`) but the note about guard display can be flagged as forward-looking. The color family is locked; the typography detail can be refined when implementation catches up.

---

## Decision Required

This is a structural addition to the brand spec — a new subsection in §2.2 with an authoritative color mapping table plus hex cleanup. No existing locked values are changed. The mapping codifies what the SVG already demonstrates.

**Recommended owner:** J. Peterman (brand-spec maintainer) with Elaine review for interactive state UX and Frank review for protocol alignment.

**Decision filed to:** `.squad/decisions/inbox/frank-diagram-color-mapping.md`
