# Brand-Spec Palette Structure Review

**Author:** Elaine (UX/Design)
**Date:** 2025-07-15
**Triggered by:** Shane feedback on §1.4 information architecture
**Status:** Recommendation — awaiting Shane decision before editing brand-spec

---

## The Problem

§1.4 Color System currently contains two consecutive palette presentations followed by a subsection (§1.4.1 Color Usage) that partially restates both. Shane correctly flags three issues:

1. The second palette (syntax-highlighting category cards) is syntax-editor-specific — does it belong in §2.1?
2. §1.4.1 overlaps with both palettes above it.
3. The overall structure is confusing to read.

---

## 1. Should the Syntax-Highlighting Palette Move to §2.1?

**Short answer:** Partially. Split it.

The current §1.4 contains two distinct things stacked back-to-back:

| Content | Lines | What It Is | Where It Belongs |
|---------|-------|------------|------------------|
| **8+3 Brand Color System card** | 372–486 | Brand-level color inventory: indigo family, text family, structural, semantic verdicts, gold note | §1.4 — this is brand identity |
| **Category cards** (Structure, States, Events, Data, Rules, Comments, Verdicts, Hue Map, Constraint Signaling) | 488–611 | Token-by-token syntax highlighting specification: which DSL token type gets which color, what italic means, what bold means | **§2.1** — this is a surface implementation spec |

**Why the category cards are surface-specific, not brand-level:**

- They describe *how the syntax highlighter assigns colors to tokens*. That's an editor behavior, not a brand identity element.
- They reference DSL-specific concepts: `precept`, `field`, `state`, `event`, `invariant`, `from`, `on`, `in`, `assert`, `reject`, `when`. These only exist in the editor surface.
- The constraint signaling table (lines 605–611) is duplicated nearly verbatim in §2.1 (lines 821–826). This confirms it's a surface spec, not a brand spec.
- §2.1 already says "Syntax highlighting uses the 8-shade palette from Brand Identity §1.4" — but then has to re-explain constraint-aware highlighting because the detail is split across two sections.

**What stays in §1.4:**

The *hue-to-category semantic assignment* is brand-level — it's used across all five surfaces (editor, diagram, inspector, docs, CLI). The fact that "indigo = structure, violet = states, cyan = events, slate = data, gold = messages" is a brand decision. But the detailed token-level spec (which keywords are bold, which are normal, what triggers italic) is an editor implementation detail.

**Recommended split:**

- §1.4 keeps: the 8+3 card, a compact semantic hue table (5 rows: category → hue → hex → role), the verdict colors, and the hue map.
- §2.1 absorbs: the full category cards (Structure, States, Events, Data, Rules, Comments), the constraint signaling table, the syntax code sample. This gives §2.1 everything an implementer needs in one place.

---

## 2. Does §1.4.1 Overlap?

**Yes, significantly.** §1.4.1 Color Usage has three problems:

### Problem A: The Color Roles table restates both palettes

The "Color Roles" table (lines 621–683) re-lists every color from the 8+3 card with cross-surface usage descriptions. This is useful — it's the canonical "what goes where" — but it partially duplicates:

- The 8+3 card's role labels (brand, brand-light, success, etc.)
- The category cards' DSL-level detail (gold = `because`/`reject` messages)

A reader who just finished the 8+3 card and six category cards hits a third presentation of the same information.

### Problem B: Gold's restriction is stated three times

Gold's "syntax only, never a UI color" rule appears in:
1. The 8+3 card's bottom note (line 482)
2. The Rules category card text (line 551)
3. The Color Roles table row (line 681)
4. The Color Usage Q&A (lines 750–751)

Four occurrences of the same rule across 300 lines. That's not emphasis — it's structural confusion.

### Problem C: The README & Markdown Application table is useful but misplaced

The README-specific color guidance (lines 686–732) is genuinely new information — it's not a restatement of the palette. But it's buried inside a section that otherwise feels repetitive. A reader skimming past the overlap will miss the one part that's actually novel.

---

## 3. Recommended Restructuring

### §1.4 Color System (brand-level — "what colors exist and what they mean")

**Keep:**
- Opening paragraph and two callout boxes (locked design + semantic color as brand)
- The 8+3 Brand Color System card (as-is — this is the brand inventory)
- A new compact **Semantic Hue Assignment** table (replaces the category cards):

  | Category | Hue | Hex | Meaning |
  |----------|-----|-----|---------|
  | Structure | Indigo 239–245° | #4338CA / #6366F1 | DSL keywords and grammar |
  | States | Violet 260° | #A898F5 | State names |
  | Events | Cyan 195° | #30B8E8 | Event names |
  | Data | Slate 215° | #B0BEC5 / #9AA8B5 / #84929F | Field names, types, values |
  | Messages | Gold 45° | #FBBF24 | Human-readable rule text |

- Verdicts card (runtime-only — brand-level decision, not syntax-specific)
- Hue map callout

**Remove:** Category cards (Structure, States, Events, Data, Rules, Comments), constraint signaling table.

### §1.4.1 Color Usage (cross-surface application — "what do I use where")

**Keep:**
- Color Roles table (deduplicate: remove any info already covered by the compact hue table above; keep the cross-surface specifics)
- README & Markdown Application table (unique content)
- Color Usage Q&A (unique content — practical guidance)
- README color contract callout

**Revise:** Trim the Color Roles table so it focuses on *cross-surface application decisions* rather than restating palette definitions. Each row should answer "where does this color appear?" not "what is this color?" (the latter is answered by §1.4).

### §2.1 Syntax Editor (surface-specific — "how the editor applies the palette")

**Absorb:**
- All six category cards from current §1.4 (Structure, States, Events, Data, Rules, Comments)
- The constraint signaling table (currently duplicated — remove from §1.4, consolidate here)
- The comment lane explanation

**Already has:** Syntax code sample, constraint-aware highlighting table, diagnostics rendering.

**Result:** §2.1 becomes a self-contained specification. An implementer reads §2.1 alone to build or audit the syntax highlighting.

---

## 4. Changes to My Prior Pass

When this restructure is approved, I would:

1. **Move** the six category cards from §1.4 into §2.1 (after the "Color application" paragraph, before the syntax code sample).
2. **Replace** the category cards in §1.4 with the compact Semantic Hue Assignment table described above.
3. **Delete** the constraint signaling table from §1.4 (lines 601–611), since §2.1 already has it.
4. **Trim** the §1.4.1 Color Roles table to remove rows that simply restate the palette definition — keep only the cross-surface usage column.
5. **Consolidate** Gold's restriction to exactly two places: one note in the 8+3 card (brand-level) and one in the §2.1 Rules card (surface-level). Remove the other two occurrences.
6. **Add** a cross-reference in §2.1's opening: "This section specifies how the syntax editor applies the color system defined in Brand Identity §1.4."
7. **Add** a cross-reference in §1.4's hue table: "For token-level detail, see Syntax Editor §2.1."
8. **Verify** that §2.2 (State Diagram), §2.3 (Inspector), §2.4 (Docs), and §2.5 (CLI) still reference §1.4 correctly after the restructure — their cross-references to "Brand Identity §1.4" should still resolve.

---

## Summary

The core issue is a **scope mismatch**: §1.4 is trying to be both a brand color specification (what colors exist) and a syntax highlighting specification (how the editor uses them). The fix is to split along the seam that already exists — brand decisions stay in §1, surface implementations go to §2. The category cards are the content that crosses the line. Moving them to §2.1 eliminates the duplication, makes §2.1 self-contained, and lets §1.4 focus on what a brand section should focus on: the color system, not the editor.
