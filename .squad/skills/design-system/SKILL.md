---
name: "design-system"
description: "Domain knowledge skill for Precept's visual design system. Covers where design system research lives, how to capture new research, how to use it in decisions, and how to keep it current."
domain: "design-system"
confidence: "high"
source: "earned — generalized from language-design audit; ensures design work is grounded in the research corpus and design foundations"
---

## Context

This skill governs how agents work with the **design system corpus** — the semantic visual system, surface specs, foundations, and design explorations that define how Precept looks and feels.

**Applies to:** Any agent doing design-system work — semantic tokens, color mappings, surface layouts, component specs, typography application, diagram design, preview panel UX, brand spec execution. Primary users: Elaine (UX Designer — primary owner), Peterman (brand meaning input), Frank (boundary decisions), Kramer (preview implementation).

## Research Location

| Category | Path | Contents |
|----------|------|----------|
| Design system root | `design/system/` | Canonical reusable visual-system guidance |
| Foundations | `design/system/foundations/` | Foundation-level rules: tokens, scales, spacing |
| Surfaces | `design/system/surfaces/` | Per-surface specs (preview panel, diagrams, etc.) |
| Research | `research/design-system/` | Research informing system decisions |
| References | `research/design-system/references/` | External reference material |
| Reviews | `research/design-system/reviews/` | Prior review artifacts |
| Visual system HTML | `design/system/foundations/semantic-visual-system.html` | Canonical visual system artifact |
| Brand spec (co-owned) | `design/brand/brand-spec.html` | Canonical brand artifact — Elaine executes, Peterman supplies meaning |
| Explorations | `design/brand/explorations/` | HTML prototypes: color, typography, visual language |
| Preview PRD | `docs/PreviewInspectorRedesignPrd.md` | PRD for preview surfaces |
| Diagram layout | `docs/DiagramLayoutRedesign.md` | Layout specs for diagram rendering |

### Brand-adjacent sources (read when crossing the brand-system boundary)

| File | Contents |
|------|----------|
| `research/brand/color-systems.md` | Color system research informing palette |
| `research/brand/typography.md` | Typography research informing type scale |
| `research/brand/visual-language.md` | Visual language patterns and principles |
| `design/brand/brand-decisions.md` | Decided brand elements that constrain design system choices |

## Using Research in Work

### Before any design system decision

1. Read the relevant `design/system/` files for the surface or foundation you're changing
2. Check `design/brand/brand-decisions.md` for brand-level constraints (color, typography, etc.)
3. Check `design/brand/reviews/` and `research/design-system/reviews/` for prior feedback on the same surface
4. Read PRDs (`PreviewInspectorRedesignPrd.md`, `DiagramLayoutRedesign.md`) when touching those surfaces
5. **Cite specific data** in your output — tokens, specs, prior review findings

### Citation standard

| Acceptable | NOT acceptable |
|---|---|
| "Per semantic-visual-system.html: surface-bg maps to slate-900 in dark mode" | "Use the dark background color" |
| "Per brand-spec-palette-structure-review-elaine.md: 5-tier semantic palette structure" | "We have a palette system" |
| "Per PreviewInspectorRedesignPrd.md §Goals: real-time preview on every keystroke" | "The preview should be fast" |
| "Per brand-decisions.md §Typography: Inter for UI, JetBrains Mono for code" | "Use a monospace font for code" |

**Rule:** If a claim could be made by any UX designer without reading the file, it is not a citation.

## Capturing New Research

### Where to put it

| Type of finding | Location |
|-----------------|----------|
| Research informing design system rules | `research/design-system/{topic}.md` |
| External reference material | `research/design-system/references/{topic}.md` |
| Review of a design artifact | `research/design-system/reviews/{artifact}-review-{agent}.md` or `design/brand/reviews/` |
| Foundation rule (decided) | `design/system/foundations/{area}.md` or update existing |
| Surface spec (decided) | `design/system/surfaces/{surface}.md` |
| HTML prototype | `design/brand/explorations/{name}.html` |

### Format consistency

Every research file should include:
- **Date** and **author**
- **Scope** — what question it answers
- **Grounded in** — what brand decisions or existing foundations it builds on
- Reference the brand spec and visual system HTML by path when they inform the work

## Maintaining Existing Research

- **When a foundation rule ships:** Update the relevant `foundations/` file
- **When brand decisions change:** Check whether design system foundations reference outdated brand constraints
- **When a surface spec is implemented:** Update `surfaces/` to match what was built
- **When review feedback is addressed:** Note resolution in the review file or link to the PR that fixed it
