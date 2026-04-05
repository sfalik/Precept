# Architectural Review: brand-spec §1.4 Palette Structure

**Reviewer:** Frank (Lead/Architect)
**Date:** 2026-04-07
**Requested by:** Shane
**Scope:** brand-spec.html §1.4 Color System, §1.4.1 Color Usage, §2.1 Syntax Editor
**Status:** Review complete. No edits made. Recommendations for restructure.

---

## Executive Summary

Section 1.4 conflates two distinct palettes that serve different audiences and purposes. The section intro promises "8 authoring-time shades in 5 families" (a syntax-highlighting concept), but the first palette card delivers 8 general-purpose UI tokens (brand, text, structural) plus 3 semantic colors — a different "8" entirely. The actual 8 syntax shades appear later in the family cards (Structure, States, Events, Data, Rules). This creates an ambiguity that cascades into §1.4.1 and §2.1, producing overlap and reader confusion.

The fix is structural, not content — the material is all correct and well-designed. It just needs to be separated by abstraction level.

---

## Findings

### Finding 1: Two palettes, one section, conflicting "8" counts

**§1.4 currently contains two distinct palette systems:**

| # | What | Lines | Audience | Level |
|---|------|-------|----------|-------|
| A | "Precept Color System · 8 + 3" card | 372–486 | UI designers, docs authors, badge makers | Foundational brand tokens |
| B | Syntax family cards (Structure through Constraint Signaling) | 488–611 | Extension developers, grammar maintainers | Syntax-surface implementation |

**The "8" collision:** The section intro (line 353) says "8 authoring-time shades in 5 families, plus 3 runtime verdict colors." This "8" refers to: Semantic, Grammar, State, Event, Data Names, Data Types, Data Values, Messages — the syntax shades. But the "8 + 3" brand palette card uses "8" to mean: brand, brand-light, brand-muted, text, text-secondary, text-muted, border, bg. These are two completely different sets of eight. A reader who sees the section intro, then encounters the brand palette card, will not find the promised 8 authoring-time shades — they'll find UI design tokens instead.

**Diagnosis:** Palette A is foundational brand infrastructure. It belongs in §1.4. Palette B is a syntax-surface color mapping with typography rules, keyword lists, and constraint signaling. It's implementation guidance for a specific visual surface — the editor — and belongs in §2.1.

### Finding 2: The syntax family cards are surface-specific, not foundational

The family cards (Structure · Indigo, States · Violet, Events · Cyan, etc.) specify:
- Exact hex values per token category
- Typography rules (bold, italic, normal)
- Keyword-to-shade assignments
- Constraint detection logic (what triggers italic)
- Hue map relationships

These are engineering instructions for the syntax editor. They tell a grammar or language-server implementor exactly what color and style to apply to each DSL token type. Other visual surfaces (state diagrams, inspector panel, docs) explicitly do NOT use these shade assignments — §2.2 says "State diagrams use the same hue families... but they do not use syntax-layer colors." The family cards are syntax-layer colors.

**However:** There's a legitimate brand argument that the *semantic family identities* (Indigo = structure, Violet = states, Cyan = events, Slate = data, Gold = rules) are brand-level decisions, not surface-level decisions. The *identity* is brand. The *implementation* (specific hex per shade, keyword lists, typography rules) is surface.

### Finding 3: §1.4.1 Color Usage overlaps without clear boundary

§1.4.1 provides a cross-cutting roles matrix mapping each brand token to specific uses across surfaces. This is valuable — it answers "where does brand-light go?" across all contexts.

**Overlap with §1.4 family cards:** The §1.4.1 table includes entries like "grammar keywords in syntax highlighting" (for brand #6366F1) and "state names in diagrams and syntax" (for brand-light #818CF8). These are partial restatements of what the family cards in §1.4 already specify in much greater detail. A reader working on syntax highlighting has to read three places: the family cards in §1.4, the roles table in §1.4.1, and the surface spec in §2.1.

**Overlap with §2.1:** §1.4.1's gold Q&A ("Can I use gold outside of syntax highlighting? No.") restates what the gold note in the 8+3 palette card already says, and what §2.1 will demonstrate. Three sources for the same fact.

§1.4.1 is not *wrong* — it serves a useful "quick lookup" function. But it should be positioned as the cross-cutting guide it is, not as a detail expansion that retells syntax-specific material.

### Finding 4: §2.1 duplicates constraint signaling from §1.4

§2.1's "Constraint-aware highlighting" table (lines 821–826) is nearly identical to the "Constraint signaling" table in §1.4 (lines 605–610). Same three rows, same columns, same content. If the family cards move to §2.1, this duplication resolves naturally — the constraint signaling table merges with the constraint-aware highlighting table as one authoritative reference.

### Finding 5: Verdicts appear in three places

Verdict colors (success/enabled, error/blocked, warning) appear in:
1. The 8+3 brand palette card (§1.4, lines 453–478) as "Semantic" group
2. The Verdicts family card (§1.4, lines 572–590) with "Runtime only" framing
3. §1.4.1 Color Roles table (lines 664–677) with surface-specific uses

The brand palette card correctly treats verdicts as general-purpose semantic colors (success, error, warning). The family card reframes them as "Runtime only — never in syntax highlighting" with different naming (Enabled, Blocked, Warning). These are two valid framings at two abstraction levels, but placing them adjacent in the same section makes it unclear which is authoritative.

### Finding 6: Minor hex discrepancy with brand-decisions.md

Not the focus of this review, but noted for completeness:
- brand-decisions.md uses Blocked `#F87171` and Warning `#FDE047`
- brand-spec.html uses error `#FB7185` and warning `#FCD34D`

These may be intentional refinements, but the delta should be explicitly noted if so.

---

## Recommendation: Two-Level Color Architecture

### Principle

**§1.4 owns the identity.** "What do our colors *mean*?" — the semantic families, the brand tokens, the philosophy.

**§2.1 owns the implementation.** "How are colors *applied* to the syntax editor?" — the specific shades, typography, keyword mappings, constraint signaling.

This mirrors how every other section works: §1.3 defines the wordmark and brand mark (identity); §2.x sections apply typography to specific surfaces.

### Proposed structure

#### §1.4 Color System (Brand Tokens + Semantic Families)

Keep:
- The "Precept Color System · 8 + 3" brand palette card (Palette A) — this IS foundational
- Background specification (#0c0c0f)
- "Semantic color as brand" callout (the philosophy)
- Verdicts as general-purpose semantic colors (success, error, warning)

Add:
- A brief **Semantic Family Reference** table — one row per family, conceptual only:

| Family | Hue | Brand Identity |
|--------|-----|---------------|
| Structure | Indigo 239–245° | DSL control and grammar |
| States | Violet 260° | State machine nouns |
| Events | Cyan 195° | Transition verbs |
| Data | Slate 215° | Fields, types, values |
| Rules | Gold 45° | Human-readable messages |
| Verdicts | Green / Red / Yellow | Runtime outcomes (never in syntax) |

This table declares the *identity-level color assignments* without descending into typography, keyword lists, or constraint detection. It says "Violet means states" — a brand claim. It does NOT say "State names use #A898F5, italic when constrained" — that's surface implementation.

Add a forward reference: *"For syntax-level color application including typography, keyword mapping, and constraint signaling, see §2.1 Syntax Editor."*

Remove from §1.4:
- The full family cards (Structure · Indigo through Constraint Signaling) → move to §2.1
- The hue map → move to §2.1 (it's about inter-shade relationships in the syntax system)

Update the section intro:
- Change "8 authoring-time shades in 5 families" to language that covers both the brand palette and the semantic families without the ambiguous "8" count.

#### §1.4.1 Color Usage (Cross-Cutting Roles Matrix)

Keep as-is. Its value is the cross-cutting "where does this color go?" lookup table. It should:
- Reference §1.4 for token definitions ("what is brand-light?")
- Reference §2.1 for syntax-specific detail ("how does gold work in the editor?")
- Remove or condense duplicate explanations that are now in §2.1

The gold Q&A ("Can I use gold outside of syntax highlighting?") can stay as a useful FAQ — it's answering a cross-cutting question, which is §1.4.1's job. But it should be a short answer with a §2.1 forward reference, not a full rationale paragraph.

#### §2.1 Syntax Editor (Full Syntax Color Reference)

Absorb from §1.4:
- All family cards (Structure · Indigo, States · Violet, Events · Cyan, Data · Slate, Rules · Gold, Comments, Verdicts)
- Hue map
- Constraint signaling table (merges with existing §2.1 constraint-aware highlighting table — they're identical)

§2.1 becomes the single authoritative reference for "what color and typography does each DSL token get?" It already has the syntax mockup and diagnostics. With the family cards added, it becomes complete.

Opening line should say: *"The syntax editor applies the semantic family identities from §1.4 through an 8-shade system with typography-based constraint signaling."* This preserves the link — §1.4 defines the identity, §2.1 implements it.

### What this achieves

1. **No more ambiguous "8"** — §1.4 doesn't promise 8 authoring-time shades. It delivers brand tokens and family identities.
2. **Single source for syntax colors** — §2.1 is the one place you go for "how do I color a `.precept` token?" No more checking three sections.
3. **Clean abstraction boundary** — Identity (§1.4) vs Application (§2.x) mirrors the rest of the document.
4. **Constraint signaling deduplication** — One table in §2.1 instead of near-identical tables in both sections.
5. **Verdict clarity** — §1.4 defines them as general semantic colors; §2.1 says "not in syntax"; §2.2+ says how they appear in other surfaces.
6. **No material loss** — Everything that's in §1.4 today survives, just in the right location.

---

## Risk Assessment

**Risk: Breaking the "semantic color IS brand" philosophy.**
Mitigation: The Semantic Family Reference table in §1.4 preserves the identity-level claim. Moving the implementation cards doesn't weaken the brand claim — it strengthens it by making the identity statement clearer and the implementation self-contained.

**Risk: §2.1 becomes too long.**
Mitigation: §2.1 is currently quite short (57 lines of HTML). Absorbing the family cards brings it to ~180 lines — still manageable and proportional to its importance as the primary visual surface.

**Risk: Cross-references from other sections.**
§2.2 (State Diagram) already references "Brand Identity § 1.4" for hue families. This reference should update to point at both §1.4 (for family identity) and §2.1 (for the hex values it adapts). The meaning doesn't change — diagrams "use the same hue families but not syntax-layer colors."

---

## Decision Required

This is a structural refactor of the brand spec's information architecture. No content changes, no color decisions, no locked values affected. The recommendation preserves everything that's there today and puts it in the section that matches its abstraction level.

**Recommended owner:** J. Peterman (brand-spec maintainer) with Elaine review for UX impact.

**Decision filed to:** `.squad/decisions/inbox/frank-brandspec-palette-structure.md`
