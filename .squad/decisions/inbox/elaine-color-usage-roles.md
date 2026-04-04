# Decision: Color Usage Roles for 8+3 System

**Date:** 2026-07-12  
**Filed by:** Elaine (UX Designer)  
**Status:** LOCKED  
**Affects:** brand-spec.html §1.4, §1.4.1; README revamp; all product surfaces

## Summary

Defined concrete color usage roles for every color in the locked 8+3 system. Updated brand-spec.html §1.4 palette card to correctly display all 8+3 colors. Added §1.4.1 Color Usage as the definitive reference for how each color maps to product surfaces and README context.

## What Changed

### §1.4 Palette Card — Fixed

**Problem:** The palette card showed an indigo gradient with 8 shades from color-exploration.html, including off-system colors (`#a5b4fc`, `#1e1b4b`, `#312e81`, `#3730a3`). It was an "Indigo overview" card, not the 8+3 brand system.

**Fix:** Replaced with a full-width palette card showing all 8 core colors + 3 semantic colors + gold accent note, organized as:
- Brand Family (indigo trio): `#6366F1` brand, `#818CF8` brand-light, `#C7D2FE` brand-muted
- Text Family: `#E5E5E5` text, `#A1A1AA` text-secondary, `#71717A` text-muted
- Structural: `#27272A` border, `#09090B` bg
- Semantic: `#34D399` success, `#FB7185` error, `#FCD34D` warning
- Note: `#F59E0B` gold (syntax accent only)

### Verdict Colors — Fixed

**Problem:** Verdicts section used wrong hex values inherited from earlier exploration:
- Error: `#F87171` → corrected to `#FB7185`
- Warning: `#FDE047` → corrected to `#FCD34D`

**Why:** The locked semantic colors are emerald `#34D399`, rose `#FB7185`, amber `#FCD34D`. The old values (`#F87171` = Tailwind red-400, `#FDE047` = Tailwind yellow-300) were never part of the locked system.

### §1.4.1 Color Usage — New Section

Added three subsections:

1. **Color Roles table** — All 12 colors with role name and specific usage examples across product surfaces.

2. **README & Markdown Application table** — How brand color maps to GitHub Markdown constraints (wordmark SVG, shields.io badge parameters, hero code block, emoji alignment).

3. **Color Usage Q&A** — Answers five common questions:
   - Secondary highlight → brand-light `#818CF8`
   - Success green in README → CI badges, feature checkmarks
   - Warning amber in README → beta/preview callouts
   - Error rose in README → No (product UI only)
   - Gold outside syntax → No (syntax-only, never UI)

4. **README Color Contract callout** — Defines the three channels for brand identity in plain Markdown: SVG wordmark, shields.io badge row, and DSL keyword rhythm in hero code block.

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| Error rose `#FB7185` never appears in README | Marketing copy should never communicate failure. Error states are product UI feedback, not public messaging. |
| Gold `#F59E0B` is syntax-only, not UI | Gold exists to distinguish human-readable rule messages from machine code. Using it in badges or UI would dilute that semantic precision. |
| Brand identity in GitHub Markdown comes through SVG + badges, not CSS | GitHub strips custom styles. Instead of fighting the platform, the brand speaks through assets (wordmark) and parameters (shields.io colors) that survive rendering. |
| Warning amber is valid in README for beta/preview | Unlike error (which implies failure), warning signals "attention needed" — appropriate for preview features and cautionary notes. |
| Secondary highlight is always brand-light, not a new color | The 8+3 system is closed. Brand-light `#818CF8` serves the "accent" role that might otherwise tempt someone to introduce a new color. |

## Downstream Impact

This decision record is the direct input for:
1. **README revamp** — Peterman can use the README Application table and Q&A without follow-up questions about color.
2. **Brand-spec §1.4 LOCKED status** — The palette card + usage guidance are now definitive enough to lock.
3. **Badge catalog** — Kramer's badge work should use the shields.io color values documented here.

## Off-System Colors Removed from §1.4

| Hex | What It Was | Why Removed |
|-----|-------------|-------------|
| `#a5b4fc` | Indigo gradient swatch (Tailwind indigo-300) | Not in locked 8+3. Shane explicitly called out as off-system. |
| `#1e1b4b` | Deep indigo gradient swatch | Part of indigo exploration ramp, not the brand system. Still used in callout styling as page chrome. |
| `#312e81` | Dark indigo gradient swatch | Same as above. |
| `#3730a3` | Medium-dark indigo gradient swatch | Same as above. |
| `#F87171` | Verdict "Blocked" color (Tailwind red-400) | Wrong. Locked error color is `#FB7185`. |
| `#FDE047` | Verdict "Warning" color (Tailwind yellow-300) | Wrong. Locked warning color is `#FCD34D`. |
