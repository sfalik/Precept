# Brand-Spec Palette Structure Review
**Section under review:** § 1.4 Color System and § 1.4.1 Color Usage  
**Reviewer:** J. Peterman  
**Date:** 2026-04-06  
**Status:** Recommendation — pending Shane sign-off before edits

---

## The Problem in One Sentence

Section 1.4 currently houses two distinct, unrelated things — a brand palette and a syntax-highlighting palette — stacked on top of each other with no visual or structural separation, while § 1.4.1 then re-lists the same brand palette colors a second time under a different heading.

---

## Current Structure Mapped

### § 1.4 Color System
1. Intro paragraph: *"8 authoring-time shades in 5 families, plus 3 runtime verdict colors…"*
2. Callout: **Locked design** — describes the syntax highlighting system
3. Callout: **Semantic color as brand** — philosophical framing (2-dimension system: category + constraint pressure)
4. **`pc-palette` card — "Precept Color System · 8 + 3"**: Brand-wide palette grouped by family (Brand Indigo, Text Family, Structural, Semantic verdicts) + Gold note. This is the brand palette.
5. **Per-category syntax cards** — Structure·Indigo (#4338CA / #6366F1), States·Violet (#A898F5), Events·Cyan (#30B8E8), Data·Bright Slate (#B0BEC5 / #9AA8B5 / #84929F), Rules·Gold (#FBBF24), Comments (#9096A6), Verdicts (#34D399 / #FB7185 / #FCD34D). These describe how colors map to DSL token categories in the editor.
6. **Hue map callout** — geometric overview of the syntax color arc (45°→260°)
7. **Constraint signaling table** — States/Events/Data, base vs. italic styling, detection method

### § 1.4.1 Color Usage
1. Intro: *"How each color is used across product surfaces…"*
2. **Color Roles table** — re-lists all 12 colors from the `pc-palette` card, but adds a "Specific Uses" column
3. **README & Markdown Application table** — surface-specific guidance for GitHub Markdown context
4. **Color Usage Q&A** — gold, error rose, success green, warning amber usage FAQs
5. **README color contract callout**

### § 2.1 Syntax Editor
1. Purpose card + Color application description (references "§ 1.4" for the palette)
2. Live syntax block example
3. **Constraint-aware highlighting table** — States/Events/Data, base vs. italic styling (verbatim duplicate of § 1.4's constraint signaling table)
4. Diagnostics card

---

## The Three Structural Problems

### Problem 1: Two Palettes in One Section, Neither Labeled

The `pc-palette` card and the per-category syntax cards describe **fundamentally different color systems**:

| | `pc-palette` card | Per-category syntax cards |
|---|---|---|
| **Purpose** | Brand identity palette — colors for UI, badges, wordmark, README | Syntax highlighting — how DSL token categories are rendered in the editor |
| **Colors** | #6366F1, #818CF8, #C7D2FE, #E5E5E5, #A1A1AA, #71717A, #27272A, #09090B + verdicts | #4338CA, #6366F1, #A898F5, #30B8E8, #B0BEC5, #9AA8B5, #84929F, #9096A6 + verdicts |
| **Audience** | Anyone building with the brand (extension dev, docs author, badge maker) | Language server implementer, syntax theme author, George/Kramer |
| **Belongs in** | § 1.4 — Brand Identity | § 2.1 — Syntax Editor |

These two systems share some colors (indigo, verdicts) but are not the same system. A reader moving through § 1.4 hits the brand palette, then immediately hits seven more cards that shift to a completely different framing (token categories) without any transitional signal. The section has no seam. It reads as one system but contains two.

### Problem 2: Conflicting "8+3" Nomenclature

The intro paragraph of § 1.4 says: *"8 authoring-time shades in 5 families, plus 3 runtime verdict colors."*  
The `pc-palette` card header says: *"Precept Color System · 8 + 3 — 8 core colors · 3 semantic colors · 1 syntax accent."*

These two "8+3" counts **describe different things**:

- The **intro paragraph's "8+3"** = 8 syntax token shades (two indigo, one violet, one cyan, three slate, one gold) + 3 runtime verdicts (emerald, rose, amber). This is the syntax highlighting system.
- The **palette card's "8+3"** = 8 brand/UI colors (three indigo tones + three text tones + two structural) + 3 semantic verdicts. This is the brand palette.

Both call themselves "8+3." Neither clarifies which "8" and which "3" it means. A reader who reads the intro, then looks at the palette card, will assume they describe the same thing. They do not.

### Problem 3: § 1.4.1 Re-Lists the Brand Palette

The "Color Roles" table in § 1.4.1 catalogs the same 12 colors shown in the `pc-palette` card immediately above it in § 1.4: brand, brand-light, brand-muted, text, text-secondary, text-muted, border, bg, success, error, warning, gold — in the same order, with the same role names.

The `pc-palette` card gives you: swatch + role name + hex.  
The Color Roles table gives you: swatch + role name + hex + specific uses column.

The only new information in 1.4.1's table is the "Specific Uses" column. But a reader must cross-compare both presentations to discover this, and many won't — they'll just wonder why they're seeing the same colors twice.

**Compounding this:** § 2.1 contains a constraint signaling table that is a verbatim copy of § 1.4's constraint signaling card. The same content exists in two places, neither referencing the other.

---

## Recommendation: What Should Move, Stay, Merge, or Rename

### § 1.4 — Rename to "Color System · Brand Palette" and Slim Down

**Keep:**
- Intro paragraph — rewrite to distinguish the two systems clearly (see note below)
- "Semantic color as brand" callout — this is brand philosophy, belongs here
- "Locked design" callout — belongs here
- The `pc-palette` card — this IS the brand palette source of truth; stays in § 1.4
- Hue map callout — brand-level orientation for the syntax arc; acceptable here as a cross-reference bridge

**Remove from § 1.4 (move to § 2.1):**
- All seven per-category syntax cards: Structure, States, Events, Data, Rules, Comments, Verdicts
- The constraint signaling table

**Rewrite note — intro paragraph:** The current intro conflates the two systems. It should say something like: *"The Precept color system has two layers: the brand palette (12 colors across 5 families, documented here) and the syntax highlighting palette (8 DSL token categories + 3 runtime verdicts, documented in § 2.1). The brand palette is the source of truth for all product surfaces. The syntax palette is derived from it."*

### § 1.4.1 — Rename to "Cross-Surface Color Application" and Remove Redundancy

**Remove:**
- The full "Color Roles" table — role names + hexes are already in the `pc-palette` card above. This table's only value is the "Specific Uses" column. Either: (a) collapse to a single-column "Specific Uses" table that assumes the reader just saw the palette card, or (b) eliminate entirely and distribute the usage notes inline into the palette card itself.

**Keep:**
- "README & Markdown Application" table — genuinely cross-surface guidance, not duplicated elsewhere
- "Color Usage Q&A" — useful operational guidance, especially for gold and error rose restrictions
- "README color contract" callout — brand-level strategic summary

**Rename:** "Color Usage" → "Cross-Surface Color Application" — the current title undersells what the section actually does. It's not just *usage*; it's the application contract for using the palette on real surfaces.

### § 2.1 — Expand with Moved Syntax Content

**Add (moved from § 1.4):**
- All seven per-category syntax cards (Structure, States, Events, Data, Rules, Comments, Verdicts)
- Hue map callout (if removed from 1.4) or add a reference to it
- Constraint signaling table — **replace** the existing constraint-aware highlighting table with this (they cover the same ground; the 1.4 version is slightly more detailed)

**Keep:**
- Existing Purpose card
- Color application description (update the reference from generic "§ 1.4" to "§ 1.4 Brand Palette" for precision)
- Live syntax block example
- Diagnostics card

**Structural note for 2.1:** With the per-category cards moved here, § 2.1 becomes substantially richer. Consider adding a section intro card that orients the reader: *"The syntax editor uses a dedicated 8-shade token palette derived from the brand palette. Below: the full token color system, followed by the live example and diagnostics."* This prevents the reader from feeling dropped into swatches without context.

---

## Proposed Clean Structure

```
§ 1.4  Color System · Brand Palette                           [LOCKED]
  ├── Intro — clarifies the two-layer system (brand + syntax)
  ├── "Locked design" callout
  ├── "Semantic color as brand" callout
  ├── pc-palette card — 8+3 Brand Color System (source of truth)
  └── Hue map callout (orientation bridge — acceptable here)

§ 1.4.1  Cross-Surface Color Application                      [LOCKED]
  ├── Streamlined "Specific Uses" table (no re-listing of role names already above)
  ├── README & Markdown Application table
  ├── Color Usage Q&A
  └── README color contract callout

§ 2.1  Syntax Editor                                          [LOCKED]
  ├── [existing] Purpose card
  ├── [existing] Color application description (updated reference)
  ├── [MOVED] Per-category syntax cards: Structure, States, Events, Data, Rules, Comments, Verdicts
  ├── [MOVED] Constraint signaling table (replaces the duplicate "constraint-aware highlighting" table)
  ├── [existing] Live syntax block example
  └── [existing] Diagnostics card
```

---

## On the "Second Palette" Question Specifically

The second palette — the per-category syntax cards — **belongs in § 2.1, not in § 1.4 and definitely not in § 2.1 as a second presentation of what's already in § 1.4.**

The test is simple: *Who needs this information, and when?*

- A docs author setting badge colors needs the brand palette (§ 1.4). They do not need to know that Events are cyan #30B8E8 at 195°.
- George implementing the language server needs the syntax token palette. He needs it in § 2.1 where the editor surface lives. Finding it in § 1.4 alongside UI brand colors is disorienting.
- The two audiences should not have to excavate through each other's content.

The `pc-palette` card and the per-category syntax cards serve different readers in different contexts. Section identity requires choosing which audience § 1.4 addresses. It should address brand identity consumers. § 2.1 should address syntax surface implementers.

---

## What This Doesn't Require

- No color values change. Zero.
- No locked decisions are altered. The palette stays locked.
- No content is deleted — only moved or restructured.
- The brand-spec HTML is already well-structured; this is reorganization at the section-and-block level, not a rewrite.

The work here is placement and labeling. The content itself is good.

---

## Confidence

High. The three problems are structural, not interpretive. The solution is mechanical: move the syntax cards out of § 1.4 into § 2.1, trim the redundant color roles table in § 1.4.1, and clarify the intro framing. No team alignment needed beyond Shane's sign-off on the direction before edits begin.
