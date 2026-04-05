# Brand-Spec Restructure — Elaine's Follow-Up Feedback

**Author:** Elaine (UX/Design)
**Date:** 2025-07-16
**Context:** Consolidation of review criteria and risk points to hold while Peterman implements the brand-spec restructure
**Status:** Active review checklist — tracks against Peterman's in-progress edit pass

---

## What This Document Is

Peterman is implementing the brand-spec information architecture cleanup based on converged feedback from Elaine, Frank, and Shane. This note consolidates the specific review criteria and risk points I need to watch during and after that restructure. It's a review checklist, not new design — everything here derives from the four review documents already filed.

**Source reviews:**
- `brand-spec-palette-structure-review-elaine.md` — §1.4 / §1.4.1 / §2.1 scope split
- `brand-spec-diagram-color-mapping-review-elaine.md` — §2.2 diagram color mapping
- `brand-spec-palette-structure-review-frank.md` — architectural two-level color split
- `brand-spec-diagram-color-mapping-review-frank.md` — diagram section placement and content

---

## 1. Section Architecture — Review Criteria

The restructure moves from "everything in §1.4" to a two-level architecture. When reviewing Peterman's pass, verify:

### §1.4 Color System (identity level)
- [ ] Contains the 8+3 brand palette card — unchanged
- [ ] Contains a **compact Semantic Family Reference table** (5 rows: Structure/States/Events/Data/Rules → hue → hex → meaning) — NOT the full category cards
- [ ] Contains verdict colors framed as general semantic colors (success/error/warning), not syntax-specific
- [ ] Contains background spec (#0c0c0f)
- [ ] Contains "Semantic color as brand" callout
- [ ] Contains forward reference to §2.1 for token-level detail
- [ ] Does **NOT** contain the 6 syntax family cards (Structure·Indigo through Constraint Signaling)
- [ ] Does **NOT** contain the hue map (moves to §2.1)
- [ ] Intro paragraph no longer says "8 authoring-time shades" — the ambiguous "8" count resolved

### §1.4.1 Color Usage (cross-surface application)
- [ ] Color Roles table trimmed to cross-surface usage only — not restating palette definitions
- [ ] Diagram mentions are brief + cross-reference §2.2 ("diagram borders and edges, see §2.2")
- [ ] Gold restriction consolidated to max 2 occurrences (one in 8+3 card, one in §2.1 Rules card)
- [ ] README & Markdown Application table retained (unique content)
- [ ] Color Usage Q&A retained, shortened where it duplicates §2.1

### §2.1 Syntax Editor (surface implementation)
- [ ] Absorbed all 6 syntax family cards from §1.4
- [ ] Absorbed constraint signaling table (merged with existing constraint-aware highlighting — not duplicated)
- [ ] Absorbed hue map
- [ ] Opening cross-reference: "applies semantic family identities from §1.4 through an 8-shade system"
- [ ] Self-contained: implementer reads §2.1 alone for complete syntax highlighting spec

### Cross-references (all surfaces)
- [ ] §2.2, §2.3, §2.4, §2.5 references to "Brand Identity §1.4" still resolve after restructure
- [ ] §2.2 updated to reference both §1.4 (identity) and §2.1 (hex adaptation) where needed

---

## 2. Duplication Removal — Specific Checks

These are the known duplication points. After the restructure, each fact should live in exactly one place (or at most two: identity + surface application).

| Fact | Current locations | Target |
|------|------------------|--------|
| Gold is syntax-only | 8+3 card note, Rules card, Color Roles table, Color Usage Q&A | 8+3 card (§1.4) + Rules card (§2.1) — 2 only |
| Constraint signaling (italic = constrained) | §1.4 lines 605–611, §2.1 lines 821–826 | §2.1 only (merged into one table) |
| Verdict "runtime only" framing | 8+3 palette card, Verdicts family card, §1.4.1 Color Roles | §1.4 card (identity) + §2.2/§2.3 (surface application) |
| State names are #A898F5 | §1.4 family card, §2.1, §2.2 SVG | §1.4 hue table (identity) + §2.1 (syntax) + §2.2 (diagram) — each at its own level |

---

## 3. Diagram Color Mapping — Preservation Checklist

The restructure should include (or make space for) the dedicated diagram color mapping. Review criteria:

### Section placement
- [ ] New h3 "Diagram color mapping" within §2.2 — not §1.4, not §2.1
- [ ] Positioned between the "No lifecycle tints" callout and the shape tiles (Frank's recommendation) OR after the event lifecycle types table (my recommendation) — either is acceptable, but pick one and be consistent

### Minimum content (4 categories)
- [ ] **Static elements:** canvas bg, node borders (per lifecycle), node fill, state name text, transition edges + arrowheads, event labels, guard annotations, legend
- [ ] **Interactive elements:** active state highlight, enabled/blocked/warning transition edges + labels
- [ ] **Semantic signals:** constrained state/event italic, orphaned node opacity, dead-end shape
- [ ] **Exclusions:** data fields (inspector), rule messages (syntax-only), comments (editorial)

### Runtime verdict overlay
- [ ] Separate h3 or clearly distinct subsection within §2.2
- [ ] Uses locked verdict hexes from §1.4: #34D399 (enabled), #FB7185 (blocked), #FCD34D (warning)
- [ ] Cross-reference to §1.4 for definitions, §2.3 for inspector usage

---

## 4. Open Diagram Decisions — Shane Resolution Needed

These three decisions from my diagram review remain open. Peterman can structure the section around them, but the specific values need Shane's sign-off before locking.

| # | Decision | My Recommendation | Status |
|---|----------|-------------------|--------|
| 1 | **Current state indicator style** | Fill tint (#1e1b4b at 10–15% opacity) — most scannable, works at small diagram sizes, uses existing color | ⏳ Awaiting Shane |
| 2 | **Muted edge color** (non-current-state edges) | #71717A (text-muted) — in the locked system, subordinate to verdict/structure colors | ⏳ Awaiting Shane |
| 3 | **Guard annotation text color** | Slate #B0BEC5 (data family) — guards reference data fields, visual link to data lane | ⏳ Awaiting Shane |

**Risk if unresolved:** Peterman can write the section structure with TBD placeholders for these values, but Kramer can't align implementation until they're locked. Each deferred decision is one more cycle before the webview CSS matches the spec.

---

## 5. Hex Discrepancies — Must Resolve During Restructure

Three known color mismatches exist across source documents. The restructure is the natural moment to fix all three.

| Element | brand-decisions.md | brand-spec SVG | brand-spec palette card | Resolution |
|---------|-------------------|---------------|------------------------|------------|
| Error/Blocked | #F87171 | #f43f5e (SVG legend) | #FB7185 | Lock to #FB7185 — palette card is source of truth |
| Warning | #FDE047 | n/a | #FCD34D | Lock to #FCD34D — palette card is source of truth |
| Enabled/Success | #34D399 | n/a | #34D399 | ✅ Consistent |

Additionally, §1.4.1 Color Roles table lists brand-light #818CF8 as "state names in diagrams" — but §2.2 and brand-decisions.md both use #A898F5 (violet). **This is wrong.** State names in diagrams are violet, not brand-light. Fix during restructure.

---

## 6. Risk Points During Implementation

### Risk A: §2.1 becomes unwieldy
**What:** Absorbing all 6 family cards + hue map + constraint signaling makes §2.1 significantly longer.
**Mitigation:** Frank estimates ~180 lines post-absorption (from ~57). That's proportional to §2.1's importance. But monitor — if it bloats past 250 lines, consider collapsible sections.

### Risk B: Cross-reference breakage
**What:** Other sections reference "Brand Identity §1.4" for colors. Moving content changes what §1.4 contains.
**Mitigation:** §1.4 retains the semantic identity (hue families, 8+3 card). References to "the §1.4 palette" remain valid. References to specific syntax-level detail need updating to point at §2.1.

### Risk C: "Semantic color as brand" philosophy diluted
**What:** Removing the family cards from §1.4 might make the color system feel less prominent in the brand section.
**Mitigation:** The compact Semantic Family Reference table preserves the identity claim. Moving implementation detail actually strengthens the brand statement by making it crisper.

### Risk D: Diagram section ships with too many TBDs
**What:** If the three open diagram decisions stay unresolved, the diagram color mapping section has visible gaps.
**Mitigation:** Peterman should structure the section with placeholder callouts. The static element mapping is fully locked and can ship immediately. Only the interactive layer has TBDs. Flag to Shane that these block Kramer's implementation alignment.

---

## 7. What I'll Review When Peterman Delivers

After Peterman completes the restructure, my review pass covers:

1. **Section scope** — does each section stay within its abstraction level?
2. **Duplication count** — spot-check the 4 duplication points from §2 above
3. **Cross-references** — verify every §2.x section can be read standalone with links back to §1.4
4. **Diagram mapping completeness** — every visible diagram element has a locked color
5. **Hex consistency** — all 3 discrepancies resolved, §1.4.1 brand-light error fixed
6. **Accessibility** — no contrast ratios broken by the move; verdict colors still documented as needing icon redundancy
7. **Reader flow** — read §1.4 → §1.4.1 → §2.1 → §2.2 sequentially; does the information architecture feel natural?

---

## Summary

The restructure is well-scoped and well-agreed. The risk isn't disagreement — it's execution fidelity. The checks above ensure that what lands in the brand-spec matches what we converged on across four separate review documents. I'm holding this as my review checklist until Peterman's pass is complete.
