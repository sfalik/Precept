# Decisions Archive

Archived decision records moved from `decisions.md` after exceeding the active-file size threshold.

---

## Archive Batch — 2026-04-05T02:00:30Z

---

# Brand-Spec Final Review Verdict

**Author:** Elaine (UX/Design)
**Date:** 2025-07-16
**Context:** Final review of brand-spec.html after Peterman's information architecture restructure and drift-normalization pass
**Status:** APPROVED

---

## Verdict: APPROVED

All known feedback from the four source reviews (Elaine palette structure, Elaine diagram color mapping, Frank palette structure, Frank diagram color mapping) and the consolidated follow-up checklist has been addressed. The one remaining open item — current state indicator style — is explicitly called out in §2.2 as awaiting Shane sign-off, which is the correct disposition.

---

## Checklist Results

### §1.4 Color System — ✅ All criteria met
- 8+3 brand palette card: present, unchanged
- Compact Semantic Family Reference table: present (6 rows, identity-level only)
- Verdicts framed as general semantic colors (success/error/warning): correct
- Background spec, "Semantic color as brand" callout, "Locked design" callout: all present
- Forward references to §2.1 and §2.2: present in both hue map and semantic family reference
- Syntax family cards removed from §1.4: confirmed — all 6 cards now live in §2.1
- Intro paragraph no longer says "8 authoring-time shades": rewritten to describe two-layer system (brand palette + syntax palette). Ambiguous "8" count resolved.

### §1.4.1 Cross-Surface Color Application — ✅ All criteria met
- Renamed from "Color Usage" to "Cross-Surface Color Application"
- Color Roles table focuses on cross-surface application guidance
- Diagram mentions are brief with §2.2 cross-references
- brand-light row corrected: now explicitly states "State names in syntax and diagrams use Violet #A898F5, not brand-light — see § 2.1, § 2.2"
- README & Markdown Application table: retained (unique content)
- Color Usage Q&A: retained

### §2.1 Syntax Editor — ✅ All criteria met
- All 6 syntax family cards absorbed from §1.4
- Hue map absorbed
- Constraint signaling table consolidated (single table, no duplicate in §1.4)
- Opening cross-reference: "applies semantic family identities from Brand Identity § 1.4 through an 8-shade token system"
- Self-contained: implementer reads §2.1 alone for complete spec

### §2.2 State Diagram — ✅ All criteria met
- New "Diagram color mapping" h3: present, positioned after Event lifecycle types
- Static elements table: 11 rows covering canvas, borders, fills, text, edges, arrowheads, labels, guards, legend
- "Runtime verdict overlay" h3: present as separate subsection
- Verdict hexes locked: #34D399, #FB7185, #FCD34D — all match §1.4 palette card
- Muted edge color resolved: #71717A (text-muted)
- Guard annotation color resolved: #B0BEC5 (Data · Names)
- Semantic signals section: constrained state/event, terminal, orphaned, self-loop
- Cross-references to §1.4 and §2.3: present

### Hex Discrepancies — ✅ All resolved
- Error/Blocked: locked to #FB7185 throughout (no trace of #F87171 or #f43f5e)
- Warning: locked to #FCD34D throughout (no trace of #FDE047)
- Success: #34D399 consistent throughout
- SVG legend blocked line: corrected from #f43f5e to #FB7185
- No off-system colors (#1FFF7A, #FF2A57, #6D7F9B) appear anywhere in brand-spec

### Duplication — ✅ Acceptable
- Constraint signaling: single occurrence in §2.1 only
- State names #A898F5: clean three-level split (§1.4 identity → §2.1 syntax → §2.2 diagram)
- Gold restriction: 5 mentions across 3 sections — more than the ideal 2, but each serves a distinct abstraction level (palette card, semantic family reference, cross-surface roles, Q&A, implementation card). Not the same structural confusion as the original 4-in-300-lines problem.

### Cross-references — ✅ All resolve
- §2.2, §2.3, §2.4, §2.5 all reference §1.4 correctly
- §2.2 references both §1.4 (identity) and §2.1 (syntax contrast)
- §2.2 verdict overlay references §2.3 for inspector usage

---

## Remaining Open Items (Not Blockers)

1. **Current state indicator style** — explicitly flagged in §2.2 as awaiting Shane sign-off. Two options documented (Elaine: #1e1b4b at 20-30% opacity; Frank: #4338CA at 10-15% opacity). This is correct — it's a design decision, not a spec gap.

2. **Hue map in both §1.4 and §2.1** — present in both sections. Defensible as an "orientation bridge" (Peterman's rationale) in §1.4, and as implementer context in §2.1. Not a duplication concern given its compact size (3 lines).

3. **No explicit diagram "Exclusions" subsection** — my original recommendation included listing what's NOT in the diagram (data fields, rule messages, comments). This is implicit from the mapping tables' scope. Minor; not a restructure concern.

---

## Summary

The brand-spec information architecture is now clean. §1.4 owns identity, §1.4.1 owns cross-surface application, §2.1 owns syntax, §2.2 owns diagrams. Each section stays within its abstraction level, cross-references resolve correctly, hex values are consistent, and the diagram has a comprehensive color mapping that didn't exist before. The restructure accomplished exactly what the four reviews converged on.

Ready for Shane's final sign-off.

---

---

# Brand-Spec Restructure: UX Review Gate and Remaining Risk Points

**Filed by:** Elaine (UX/Design)
**Date:** 2025-07-16
**Status:** ACTIVE — review criteria locked; awaiting Peterman's restructure delivery

---

## Decision Context

Peterman is implementing the brand-spec information architecture cleanup (§1.4 scope split, duplication removal, §2.1 absorption, §2.2 diagram color mapping). Four review documents from Elaine and Frank converge on the same structural recommendations. This decision record establishes what the team should track during and after that work.

## Team-Relevant Recommendations

### 1. Review gate after Peterman's restructure

Before the restructured brand-spec is considered complete, it should pass through:

1. **Elaine** — UX/IA review against the checklist in `brand/references/brand-spec-followup-feedback-elaine.md` (section scope, duplication, cross-references, diagram completeness, hex consistency, reader flow)
2. **Frank** — Architecture review confirming the two-level split (identity vs. surface application) holds
3. **Shane** — Final sign-off, including resolution of three open diagram decisions

This follows the established design gate sequence. The restructure is structural, not creative — but structural errors cascade.

### 2. Three diagram decisions block implementation

The following remain open and block Kramer's webview CSS alignment:

| Decision | Elaine's Recommendation |
|----------|------------------------|
| Current state indicator | Fill tint (#1e1b4b at 10–15% opacity) |
| Muted edge color | #71717A (text-muted, in system) |
| Guard annotation color | Slate #B0BEC5 (data family) |

**Recommendation to Shane:** Resolve these before or concurrent with Peterman's restructure so the diagram section can ship without TBD gaps.

### 3. Hex discrepancies must resolve during restructure

Three known mismatches (Error: #F87171 vs #FB7185 vs #f43f5e; Warning: #FDE047 vs #FCD34D; brand-light #818CF8 misattributed as diagram state name color). The palette card in §1.4 is the source of truth. These should be fixed atomically during the restructure — not deferred.

### 4. Gold restriction consolidation

Gold's "syntax only" rule currently appears 4 times across 300 lines. Consolidate to exactly 2: one in the 8+3 card (§1.4, brand-level) and one in the Rules category card (§2.1, surface-level). Four restatements isn't emphasis — it's a signal that the information doesn't have a clear home.

---

## Applies To

- `brand/brand-spec.html` (primary artifact being restructured)
- All `§2.x` visual surface sections (cross-reference integrity)
- Kramer's inspector/diagram webview CSS (implementation alignment post-spec)

## Full Checklist

See: `brand/references/brand-spec-followup-feedback-elaine.md`

---

---

# Brand-Spec §1.4 Palette Restructuring

**Filed by:** Elaine
**Date:** 2025-07-15
**Status:** PROPOSED — awaiting Shane decision
**Reference:** `brand/references/brand-spec-palette-structure-review-elaine.md`

---

## Decision Needed

Split §1.4 Color System along the brand-vs-surface seam:

- **§1.4** keeps: 8+3 palette card, a compact semantic hue assignment table, verdict colors, hue map. Scope: "what colors exist and what they mean."
- **§1.4.1** keeps: cross-surface Color Roles table (trimmed to remove palette restatements), README application table, Q&A. Scope: "what color goes where."
- **§2.1** absorbs: the six syntax-highlighting category cards (Structure, States, Events, Data, Rules, Comments) and the constraint signaling table. Scope: "how the editor applies the palette."

## Why

Shane flagged that §1.4 has two palettes stacked back-to-back, and §1.4.1 overlaps with both. The root cause: §1.4 mixes brand-level color identity with surface-level editor implementation. The category cards are syntax-editor-specific content living in a brand-identity section.

Moving them to §2.1:
1. Eliminates the "two palettes in a row" problem
2. Makes §2.1 self-contained for implementers (no jumping back to §1.4 for token-level detail)
3. Removes the duplicated constraint signaling table (currently in both §1.4 and §2.1)
4. Reduces Gold's "syntax only" restriction from 4 occurrences to 2

## Impact

- Brand-spec.html: §1.4, §1.4.1, §2.1 restructured
- No locked color values change — this is information architecture only
- Cross-references in §2.2–2.5 remain valid (they reference §1.4 for the hue system, which still lives there)
- Full edit plan in the reference doc above

## Recommendation

Approve this restructure. It's a readability fix with no impact on locked design decisions.

---

---

# Semantic Reframe Review — Elaine's Verdict

**Author:** Elaine (UX/Design)
**Date:** 2025-07-17
**Requested by:** Shane
**Status:** APPROVED WITH REMAINING CHANGES

---

## Verdict: APPROVED WITH REMAINING CHANGES

The brand-spec has made major structural progress. The family cards are in §2.1, §2.2 has a complete diagram color mapping, cross-references are clean, and the semantic family reference table in §1.4 is exactly the 5+3 story Shane wants. But the section's **framing** still wraps that table inside a "brand palette" narrative that hasn't been rewritten to match the clarified 5+3 model, and `brand-light`/`brand-muted` remain in the general story.

---

## Focus Area Assessment

### 1. §1.4 — General and non-repetitive?

**Partially.** The semantic family reference table (lines 497–545) is correct — 5 families (Indigo, Violet, Cyan, Bright Slate, Gold) + Verdicts (Green, Red, Yellow). This IS the 5+3 system.

**Remaining issue:** The intro paragraph (line 353) still frames §1.4 as "the brand palette — 12 colors across brand, text, structural, semantic, and syntax-accent families." Shane's clarification says §1.4 should describe **only the general semantic color system**. The 8+3 palette card (brand/brand-light/brand-muted + text tiers + structural tokens) appears before the semantic family reference table and dominates the section's narrative. The conceptual order should lead with the 5+3 semantic system, with the brand palette tokens positioned as infrastructure rather than the headline story.

**Also:** The semantic family reference table leaks surface-level shade detail. Structure shows two hexes (#4338CA + #6366F1); Data shows three (#B0BEC5 + #9AA8B5 + #84929F). At the identity level, each family should have a single representative hex or a family range, with the shade breakdown deferred to §2.1. This is minor but makes the "general, not syntax-specific" claim less clean.

### 2. `brand-light` / `brand-muted` — Gone from the general story?

**No.** Both still appear:
- In the 8+3 palette card (lines 390–403) as equal members of the Brand Indigo group
- In §1.4.1 Color Roles table (lines 564–571) with general cross-surface usage descriptions
- In §1.4.1 Q&A (line 673) — actively recommended as general tools ("I need a secondary palette highlight")

Shane's clarification: these "should disappear unless a specific surface truly needs a local tonal variant." They haven't disappeared. They haven't even been demoted or flagged as local variants.

**What to do:** Remove brand-light and brand-muted from the §1.4 narrative and §1.4.1 general guidance. If a specific surface (e.g., docs, hover states) genuinely needs them, declare them there as a local tonal variant of the Indigo family, not as general system colors.

### 3. §1.4.1 — Still too repetitive?

**Somewhat.** The Color Roles table (lines 555–617) re-lists all 12 colors from the 8+3 card with role name, hex, and swatch — then adds a "Specific Uses" column. The only new information per row is that column. A reader who just scrolled past the 8+3 card sees the same swatches again.

**Improvement from prior state:** The intro now properly positions §1.4.1 as cross-surface guidance, and the brand-light row correctly notes that state names use Violet #A898F5 (line 565). Cross-references to §2.1 and §2.2 are clean.

**Remaining fix:** Trim the table to a "Specific Uses" reference — the reader already knows the swatches and hexes from the card above. Or collapse to a compact list that says "brand: wordmark, section headers, CTA buttons, diagram borders" without re-presenting the swatch.

### 4. §2.1 — Extra shades as local surface variants?

**Yes — this is correct.** ✅

The family cards in §2.1 (lines 729–830) use shading variants within families for syntax clarity: two indigos for Structure (Semantic vs Grammar), three slates for Data (Names, Types, Values). These are properly framed as syntax-surface implementation decisions. The opening paragraph (line 724) credits §1.4 for the identity-level families. This matches Shane's "§2.1 may use shading variants within a family as needed for syntax clarity."

### 5. §2.2 — Follows 5+3 logic, no extraneous colors?

**Yes — this is correct.** ✅

The diagram color mapping table (lines 1012–1030) and runtime verdict overlay (lines 1032–1086) use only colors from the 5 semantic families (Indigo, Violet, Cyan, Slate) + 3 outcome colors (Green, Red, Yellow) + neutral UI tokens (bg, text-muted) for canvas and legend. No extraneous colors. The SVG legend blocked line now uses #FB7185 (line 985), resolving the prior hex discrepancy.

The open decisions (current state indicator, lines 1088–1096) are properly flagged as awaiting Shane sign-off.

---

## Summary of Remaining Changes

| # | Issue | Severity | Section |
|---|-------|----------|---------|
| 1 | §1.4 intro still frames section as "brand palette" story, not "5+3 semantic system" story | Medium | §1.4 intro |
| 2 | `brand-light` and `brand-muted` still appear as general system colors in the palette card and §1.4.1 | Medium | §1.4, §1.4.1 |
| 3 | Semantic family reference table exposes surface-level shade counts (2 hexes for Structure, 3 for Data) | Low | §1.4 table |
| 4 | §1.4.1 Color Roles table re-lists all 12 swatches from the card above | Low | §1.4.1 |
| 5 | §1.4.1 Q&A still actively recommends brand-light/brand-muted as general tools | Medium | §1.4.1 Q&A |

Items 1, 2, and 5 directly contradict Shane's clarified feedback. Items 3 and 4 are polish but reinforce the wrong framing.

---

## What's Already Right

- ✅ Family cards properly moved to §2.1
- ✅ §2.1 treats shade variants as local surface decisions
- ✅ §2.2 diagram color mapping is complete and 5+3-compliant
- ✅ §2.2 SVG hex discrepancy fixed (#FB7185)
- ✅ Semantic family reference table exists with correct 5+3 content
- ✅ Cross-references between §1.4, §2.1, and §2.2 are clean
- ✅ Constraint signaling consolidated in §2.1 (no §1.4 duplicate)
- ✅ Open diagram decisions properly called out as awaiting Shane

---

---

# README Research Recommendations — J. Peterman

**Date:** 2025-01-18  
**Status:** Pending Shane review  
**Research file:** `brand/references/readme-research-peterman.md`

---

## Summary

Studied 13 READMEs (8 comparable libraries, 5 exemplar projects) with real measurements. Three README models identified: Content-Rich, Gateway, Hybrid. **Recommendation: Hybrid model for Precept** — hook + hero + AI-first section + features + links.

---

## Key Recommendations

### 1. Structure: Hybrid README Model

**Pattern:**
- Opening hook (23 words): "Precept is a domain integrity engine for .NET that binds an entity's state, data, and business rules into a single executable DSL contract."
- Hero code (18 lines): Subscription Billing DSL sample (DSL only, no runtime code)
- One-line clarifier: "Precept unifies state machines, validation, and business rules into a single DSL — replacing three separate libraries with one executable contract."
- **AI-First Tooling section** (new, unique to Precept)
- Installation (dotnet add, VS Code extension)
- Quick links (Docs, Samples, Language Reference, MCP Server, Copilot Plugin)
- Features list (bullet overview of 9 DSL constructs)

**Why Hybrid?**
- **Not Content-Rich** (like XState, Stateless) — Precept has docs site; README shouldn't duplicate construct reference
- **Not Gateway** (like Vue, FastEndpoints) — DSL syntax must be shown immediately to prove readability
- **Hybrid works** (like Zod, FluentValidation) — hero sample proves "you can read this," docs site handles depth

**Evidence:** Zod (13-line hero, 8-word hook), FluentValidation (24-line hero, 18-word hook), React (12-line hero, 9-word hook) all use Hybrid model successfully.

---

### 2. Hero Code: 18 Lines, DSL Only

**Recommendation:** Use Subscription Billing sample (18 DSL statements, validated in line-economy research)

**What to show:**
- `precept SubscriptionBilling`
- 2-3 fields (Status, BillingCycleDay, ActiveSince)
- 3 states (Trial, Active, Cancelled)
- 2 events (Activate, Cancel)
- 3 event handlers with guards and transitions
- 1 constraint or invariant

**What NOT to show:**
- C# runtime invocation code (`var engine = new PreceptEngine(...)`)
- JSON state snapshots
- Fire results or verdicts

**Why?** Hero's job is to prove **the DSL is readable**. Runtime integration belongs in docs. Reader should see DSL and think "I understand this without reading the manual" — that's Precept's value prop.

**Evidence:** Hero code in studied READMEs ranges 6–26 lines (median 13). Precept's 18 lines aligns with FluentValidation (24), Stateless (18), XState (26) — all show complete round-trip within constraints.

---

### 3. Positioning: Category-Creating Language

**Recommendation:** "Precept is a domain integrity engine for .NET that binds an entity's state, data, and business rules into a single executable DSL contract." (23 words)

**Follow with one-line clarifier:** "Precept unifies state machines, validation, and business rules into a single DSL — replacing three separate libraries with one executable contract."

**Why?**
- **"Domain integrity engine"** is a new category claim (like Bun's "all-in-one toolkit," XState's "actor-based orchestration")
- **Not comparative** — doesn't say "better than X," says "different abstraction layer"
- **Concrete outcome** — "executable DSL contract" is the differentiator

**Evidence:** Category-creating tools use "[X] is a [new category] for [platform]" structure. Bun: "all-in-one toolkit for JavaScript and TypeScript apps." Polly: "resilience and transient-fault-handling library." NRules: "production rules engine for .NET, based on the Rete matching algorithm."

---

### 4. AI-First Tooling Section (NEW — Unique Opportunity)

**Recommendation:** Add dedicated section immediately after hero code:

```markdown
### AI-First Tooling

Precept ships with native MCP server integration and a GitHub Copilot plugin:
- **MCP server:** 5 tools (`precept_compile`, `precept_fire`, `precept_inspect`, `precept_update`, `precept_language`)
- **Copilot plugin:** Agent definition + 2 skills for DSL authoring and debugging
- **Language server:** Full LSP support with diagnostics, completions, hover, semantic tokens, and live preview

AI agents can author, validate, and debug `.precept` files without human intervention.
```

**Why?**
- **Unique to Precept:** No comparable library studied leads with MCP + Copilot + LSP as unified tooling story
- **Factual, concrete:** Lists specific tools, not marketing claims
- **Category differentiation:** "AI-first" is Precept's secondary positioning (per brand decisions)

**Evidence:** Only 1 of 13 projects studied mentions AI tooling (NRules links to "GPT Rules Writer" in passing). Precept's integrated MCP + Copilot + LSP story is a **competitive advantage** — should be front and center in README.

---

### 5. Copy Tone: Concrete, Confident, Technical

**Guidelines:**
- **Declarative, present tense** — "Precept is," "Precept compiles," "Precept ships with" (not "tries to," "hopes to")
- **Concrete metrics** — "18-line hero," "9 DSL constructs," "5 MCP tools" (not "many," "various," "multiple")
- **Technical precision** — "DSL runtime," "interpreter," "contract," "LSP" (not "tool," "system," "framework")
- **No hedging** — "unifies" (not "aims to unify"), "replaces" (not "can replace")
- **J. Peterman voice** — evocative but precise, authoritative with warmth (not marketing fluff)

**Evidence:** Successful READMEs use declarative tone (Polly: "Polly is," Bun: "Bun is"), concrete claims (Biome: "97% compatibility," "450+ rules"), technical precision (NRules: "Rete matching algorithm," Bun: "written in Zig").

---

## Anti-Patterns to Avoid

Based on research findings:

1. **Don't bury the hero code** — Polly's hero appears at line 51 (too late)
2. **Don't show runtime invocation in hero** — MediatR's README is all registration code (confusing)
3. **Don't redirect to docs before showing code** — Vue's README is pure links (wrong for unknown category)
4. **Don't write 32-word opening sentence** — Polly's hook lists six strategies (too dense)
5. **Don't skip "What is X?" framing** — Bun and Zod both ask "What is [X]?" before answering
6. **Don't use generic examples** — No "foo/bar" — real domains only (Subscription Billing, Coffee Order)

---

## Decision Points for Shane

1. **README model:** Approve Hybrid model (hook + hero + AI-first + features + links)?
2. **Hero sample:** Approve Subscription Billing (18 DSL statements) as hero code?
3. **Positioning:** Approve "domain integrity engine" as category claim?
4. **AI-first section:** Approve dedicated MCP + Copilot + LSP section after hero?
5. **Copy tone:** Approve J. Peterman voice guidelines (concrete, confident, technical)?

**Next step:** Draft new README.md using approved structure.

---

## Research File

Full research with measurements, quotes, and synthesis: `brand/references/readme-research-peterman.md`

---

## J. Peterman — Brand Review: Elaine's 5-Surface UX Spec
**Date:** 2026-04-04
**Status:** Approved with notes

---

### Surface-by-surface notes

#### Surface 1: Syntax Editor — ✅ Compliant
The spec is precise and correct. The full 8-shade authoring palette is applied with exact hex values. The "bold = semantic drivers, italic = constrained tokens, normal = everything else" typography signal is stated cleanly. The explicit rule — "No runtime verdicts in syntax highlighting" — is written as a hard constraint, which is exactly right. The AI-first note is accurate and earns its place: semantic tokens as a machine-readable metadata channel is a real insight, not decoration.

One note: `#9096A6` for comments is correctly identified as "dusk indigo, editorial, outside the semantic palette." That framing is consistent with brand-decisions.md. No change needed.

#### Surface 2: State Diagram — ⚠️ Needs revision (see Priority Issues)
The prose and callout box are excellent. "Lifecycle role is shown by shape, not tint" is the correct principle, stated with conviction. The AI-first note — diagrams as machine-readable knowledge graphs — is strong brand positioning.

The problem is the color table contradicts the callout. The spec lists three separate node colors:
- `#A5B4FC` for initial states
- `#94A3B8` for intermediate states  
- `#C4B5FD` for terminal states

None of these are in the locked 8+3 system. And their existence directly violates the "no lifecycle tints" principle stated two lines above. The spec cannot simultaneously claim shape carries lifecycle structure and then assign three different tint values to three lifecycle roles. One of them has to win. Per brand-decisions.md, shape wins.

Additionally: the event sub-shading entry references `#38BDF8`, `#7DD3FC`, and `#0EA5E9` as cyan sub-shades for event subtypes. These are off-system (Tailwind sky family, not the locked `#30B8E8` cyan). The locked system has one event hue. If subtype differentiation is needed, the mechanism is Elaine's — but the colors must come from the locked palette.

#### Surface 3: Inspector Panel — ✅ Compliant with one minor note
Solid. Verdict colors (`#34D399`, `#F87171`, `#FDE047`) are applied correctly and exclusively to runtime outcomes. The data color mapping (slate names / types / values, violet for current state label) is consistent with the editor. The "field names italic if guarded by invariant" rule correctly mirrors the syntax highlighting convention.

One point worth flagging: the spec uses `#FBBF24` gold for constraint message text beneath violated fields. This is defensible — the `because`/`reject` message payloads earn the warm interrupt whether they appear in the editor or the inspector, and it maintains cross-surface consistency. It's not a violation. But it means gold appears in two distinct contexts (authoring-time rule strings AND runtime violation messages), and those contexts must remain visually separate. They are, because violation messages appear below a field in inspector layout, not inline with syntax. I'm noting it, not flagging it as a problem.

Minor: `#9096A6` (the comments/editorial shade) is specified for read-only field indicators. This color sits intentionally outside the semantic palette. Using it for "cannot be modified" is a legitimate secondary role. Worth documenting in the integrated brand-spec as "editorial shade — second use: read-only field indicator."

Brand positioning for inspector is correct. "The inspector is both a human debugging tool and an API contract for AI tools" is exactly the right frame.

#### Surface 4: Docs Site — ✅ Approved as aspirational, with one flag
The color system is sound — indigo accents, dark neutral chrome, syntax palette in embedded code blocks. The callout box treatment maps cleanly to brand-spec.html patterns.

One flag: the typography section suggests "System font stack or Cascadia Cove 400" for prose. If Precept's brand is Cascadia Cove throughout — and it is — then "system font stack" is not a locked option; it's an escape hatch. The spec should read: Cascadia Cove for all surfaces, with monospace system font (Consolas, Courier New) as the fallback chain, not as an equivalent alternative.

H2/H3 at "600–700 weight" is also loosely specified. Brand-spec uses 700 for headings. A 600-weight sub-heading tier may be intentional and is reasonable — just needs to be pinned to a specific value when the docs site moves from aspirational to locked.

The PETERMAN REVIEW comment in the HTML (line 168) asks whether docs site branding is my responsibility. It is. When the docs site moves to active design, I own the brand compliance review. This draft correctly holds the aspirational specs here rather than locking them prematurely. That's the right call.

#### Surface 5: CLI / Terminal — ✅ Compliant
The verdict color mapping is correct and consistent. The explicit decision — "No structural indigo on CLI. Deep indigo (#6366F1) does not render well on light terminal themes" — is sound. Brand indigo belongs to UI chrome and syntax highlighting, not terminal escape sequences. This is the right trade.

The typography guidance (bold + verdict color for error headers, default color for file paths and diagnostic text) is sensible and light terminal-safe. The AI-first note — "color should never be the only signal — symbol and text structure matter more" — is right and reinforces the color + symbol redundancy principle across all surfaces.

---

### Priority issues

**1. State Diagram: Node color table must be corrected (blocks integration into brand-spec.html)**

The three-color lifecycle tinting (`#A5B4FC`, `#94A3B8`, `#C4B5FD`) directly contradicts the spec's own "no lifecycle tints" principle and introduces three off-system hex values. The resolution is clear: state nodes use the indigo family for borders/structure (`#4338CA`/`#6366F1`) and state names use violet (`#A898F5`). Shape (circle, rounded rect, double-border) encodes initial/intermediate/terminal. The color table entry should be revised to a single line: "State nodes: indigo border (`#6366F1`), dark background, state name text in violet (`#A898F5`)."

**2. State Diagram: Off-system event sub-shade colors**

`#38BDF8`, `#7DD3FC`, `#0EA5E9` are not in the locked system. If transition/conditional/stationary event subtypes need visual differentiation in diagrams, the mechanism must either use the single locked cyan (`#30B8E8`) with other signals (dashed vs. solid edges, label suffixes), or the introduction of additional shades must go through a formal palette extension decision — not appear in a spec draft as fait accompli. Remove these specific values until a decision is made.

---

### Minor notes

1. **Docs site: prose typography** — "System font stack" should read "Cascadia Cove, with monospace system fallback." Not equivalent alternatives.

2. **Docs site: H2/H3 weights** — Pin "600–700" to a specific value when the docs site moves to locked. 600 is not currently a specified brand weight.

3. **Inspector: `#9096A6` editorial reuse** — Document explicitly in the integrated spec that this shade serves two roles: syntax comments (authoring-time) and read-only field indicators (inspector). Clarifies intent for any implementer.

4. **Inspector: gold constraint messages** — Note in the integrated spec that `#FBBF24` serves both authoring-time rule message strings (in syntax highlighting) and runtime constraint message text (in inspector). The contexts are spatially separate, so there's no confusion — but the dual role should be documented.

---

### Overall verdict

The draft is well-grounded in the locked system — Elaine clearly read brand-decisions.md, not just skimmed it. One internal contradiction in the state diagram section (lifecycle tints specified while the principle bans them, using off-system hex values) must be resolved before this spec goes into brand-spec.html. Everything else is either compliant or correctly flagged as aspirational.

---

---

# Decision: Brand mark corrected to three forms

**Date:** 2025-07-17  
**Author:** J. Peterman  
**Files changed:** `brand/brand-spec.html`, `brand/explorations/visual-language-exploration.html`

## What was wrong

Two previous agent passes corrupted the brand mark section in `brand/brand-spec.html` section 1.3:

1. **Size-variant display** — The "Brand mark form" card was replaced with Full (64px) / Badge (32px) / Micro (16px) size rows, all showing the same single state-machine mark. This was never requested and incorrectly implied a size-variant system was locked.

2. **"Brand in system" card** — A "Brand in system: the two primary surfaces" card (DSL code + state diagram) was incorrectly appended inside section 1.3. This content belongs elsewhere; section 1.3 covers the brand mark form itself.

3. **TBD placeholder** — `visual-language-exploration.html` Surface 3 still showed two dashed TBD boxes with "Autonomous prototyping loop running" text, never updated with actual marks.

## What was fixed

The correct three marks — sourced exactly from `brand/explorations/semantic-color-exploration.html` section 5, lines 637–675 — are now displayed in both files:

| Mark | Label | Concept |
|------|-------|---------|
| Mark 1 | State + transition | The state-machine atomic unit |
| Mark 2 | Tablet / precept | The written rule / code tablet |
| Mark 3 | Tablet + state machine | Combined form — **primary mark** |

All three SVGs are 64×64, use the locked palette (Indigo #6366F1, Emerald #34D399, Slate #475569, Ground #1E1B4B), and share one conceptual language.

### Changes made

**`brand/brand-spec.html` — section 1.3:**
- Replaced "Brand mark form" card (Full/Badge/Micro) with "Brand mark — three forms" card
- Removed incorrectly-added "Brand in system: the two primary surfaces" card
- Color key and prototyping-loop callout retained

**`brand/explorations/visual-language-exploration.html` — Surface 3:**
- Replaced TBD placeholder boxes with the same three-mark flex display
- Now in sync with brand-spec

---

---

# Decision: Reduce Emerald Arrow Visual Weight

**Author:** Elaine (UX Designer)
**Date:** 2025-07-25
**File:** brand/brand-spec.html

## Context

After the latest brand mark revision, the Emerald transition arrow and arrowhead read slightly heavy across all three brand-mark contexts. The task was to dial them back a notch — keeping them clearly readable as flow indicators, but reducing their dominance.

## Changes Made

### State + transition icon (64×64)
- Arrow line: `stroke-width="2"` → `stroke-width="1.5"`
- Arrowhead path: `M36,28 L42,32 L36,36` (8px tall) → `M37,29.5 L42,32 L37,34.5` (5px tall). Trimmed both height and width proportionally.

### Combined mark — wordmark-in-context instance
- Arrow line: `stroke-width="1.5"` → `stroke-width="1.2"`
- Arrowhead path: `M33,42 L36,44 L33,46` (4px tall) → `M33.5,42.5 L36,44 L33.5,45.5` (3px tall)

### Combined mark — three-forms instance
- Same adjustments as above (these two instances were in sync and kept in sync).

### State machine diagram edges + marker
- All three edge lines: `stroke-width="1.5"` → `stroke-width="1.2"`
- Marker definition: `markerWidth/Height 8×8, refX=7` → `7×7, refX=6, refY=3.5`, path proportionally scaled `M0,0 L7,3.5 L0,7`
- Legend "Flow" line: `stroke-width="1.5"` → `stroke-width="1.2"`

## Rationale

- The state+transition icon carried a 2px arrow stroke — noticeably thicker than all other strokes in the icon system (which run at 1.5). Bringing it to 1.5 aligns with the existing weight vocabulary.
- The arrowhead fill paths were sized generously relative to their context. Trimming them by ~30% keeps them legible as directional cues without competing with the state node and target shapes.
- The diagram edges at 1.5 were heavier than the diagrammatic convention warrants for supporting elements. 1.2 keeps them present and readable while letting the state nodes (2.5/2.0) hold hierarchy.
- All four contexts were updated in the same pass to maintain consistency across the brand mark system.

---


---

## Decision: spm-row Horizontal Padding in Single-Row Groups

**Author:** Elaine  
**Date:** 2025-07-16  
**Section:** §2.1 Syntax Editor — Core Semantic Tokens table  
**Status:** Resolved

### Problem

In the `.spm-*` surface palette mapping layout, the CSS rule for `.spm-row` sets `padding: 14px 24px`. For multi-row groups (Structure · Indigo, Data · Bright Slate) the rows are inside an `spm-grid` wrapper, and `.spm-grid > .spm-row { padding: 12px 0; }` correctly removes the horizontal padding so only the container's `padding: 16px 24px` applies.

Single-row groups (State, Event, Messages, Comment) used bare `<div style="padding: 16px 24px;">` wrappers — no `spm-grid` class. The row's own 24px horizontal padding stacked on the container's 24px, visually indenting those rows ~48px from the section edge vs. ~24px for multi-row groups.

### Decision

Add `class="spm-grid"` to the single-row wrapper `div`s. This activates the `.spm-grid > .spm-row` override without requiring any new CSS rules or content changes.

### Pattern

**Rule:** All `.spm-row` elements must be direct children of a `.spm-grid` wrapper. Never place a `.spm-row` inside a bare div with explicit horizontal padding — the double-indent is unavoidable without the `spm-grid` class.

---

