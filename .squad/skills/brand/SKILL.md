---
name: "brand"
description: "Domain knowledge skill for Precept brand identity. Covers where brand research lives, how to capture new research, how to use it in decisions, and how to keep it current."
domain: "brand"
confidence: "high"
source: "earned — generalized from language-design audit; ensures brand work is grounded in the research corpus, not generic marketing intuition"
---

## Context

This skill governs how agents work with the **brand research corpus** — reading it before making decisions, capturing new findings consistently, and keeping it current as the brand evolves.

**Applies to:** Any agent doing brand-related work — positioning, voice, narrative, visual identity, README copy, hero content, philosophy-adjacent changes, or reviewing brand artifacts. Primary users: J. Peterman (Brand/DevRel), Elaine (UX — when touching brand-system boundary), Frank (boundary decisions), Steinbrenner (positioning overlap).

## Research Location

| Category | Path | Contents |
|----------|------|----------|
| Brand decisions | `design/brand/brand-decisions.md` | Canonical decided elements — each marked ✅ (decided) or 🔲 (open) |
| Brand research | `research/brand/` | 19 files: positioning, voice, color, typography, visual language, aesthetics, adjacency, philosophy extension, narrative |
| Brand reviews | `design/brand/reviews/` | Review artifacts from prior brand work (palette, diagram colors, README) |
| Hero brief | `design/brand/hero-creative-brief.md` | Creative direction for hero content |
| Brand philosophy | `design/brand/philosophy.md` | Redirects to `docs/philosophy.md` |
| Product philosophy | `docs/philosophy.md` | Grounding document — **read-only, never edit without owner approval** |
| Brand spec | `design/brand/brand-spec.html` | Canonical brand artifact (Elaine executes, Peterman supplies meaning) |
| Research index | `research/brand/README.md` | What goes in research vs. references vs. reviews |
| Explorations | `design/brand/explorations/` | HTML prototypes: color, typography, visual language |

### Key research files by topic

| Topic | File |
|-------|------|
| Positioning | `research/brand/brand-positioning.md`, `research/brand/adjacent-products.md` |
| Voice & tone | `research/brand/voice-and-tone.md` |
| Narrative | `research/brand/brand-narrative.md` |
| Color | `research/brand/color-systems.md` |
| Typography | `research/brand/typography.md` |
| Visual language | `research/brand/visual-language.md` |
| Aesthetic precedents | `research/brand/aesthetic-brands.md` |
| Philosophy | `research/brand/philosophy-brand-extension.md`, `research/brand/philosophy-draft-v2.md`, `research/brand/philosophy-rewrite-brand-impact.md` |
| Data vs state | `research/brand/data-vs-state-philosophy.md`, `research/brand/data-vs-state-architecture.md`, `research/brand/data-vs-state-runtime.md` |
| Brand spec structure | `research/brand/brand-spec-structure-research.md` |
| README | `research/brand/readme-research-peterman.md`, `research/brand/readme-research-elaine.md` |

## Using Research in Work

### Before any brand decision

1. Check `brand-decisions.md` for whether the element is already decided (✅) — cite the existing decision
2. Read the relevant research file(s) for the topic area
3. Read `docs/philosophy.md` when the work touches product identity or positioning
4. **Cite specific data** in your output — at least 2 data points per file read

### Citation standard

| Acceptable | NOT acceptable |
|---|---|
| "Per brand-decisions.md §Voice: 'Authoritative with warmth,' models: Serilog + MediatR + Workflow Core" | "The brand voice is professional" |
| "Per voice-and-tone.md: Formal↔Casual set to 'slightly casual, contractions fine'" | "Keep it conversational" |
| "Per brand-positioning.md: Temporal coined 'durable execution' — same category-creation pattern" | "Category creation is a good strategy" |
| "Per brand-decisions.md §Voice Do/Don't: 'Precept compiles business rules' NOT 'supercharges your logic'" | "Avoid hype language" |

**Rule:** If a claim could be made by any brand strategist without reading the file, it is not a citation.

### Flagging ungrounded claims

When your output includes claims not grounded in the research corpus, flag them:

```markdown
### Claims from general expertise
- {claim} — not found in brand research; based on {source/reasoning}
```

## Capturing New Research

### Where to put it

| Type of finding | Location |
|-----------------|----------|
| Positioning study, voice analysis, visual identity precedent | `research/brand/{topic}.md` |
| Critique of a specific brand artifact | `design/brand/reviews/{artifact}-review-{agent}.md` |
| HTML prototype / exploration | `design/brand/explorations/{name}.html` |
| Approved rule or decision | Update `design/brand/brand-decisions.md` |

### Format consistency

Every research file should include:
- **Date** and **author**
- **Scope** — what question it answers
- **Grounded in** — what existing files/decisions it builds on
- **Precedent analysis** (when applicable) — structured comparison, not prose
- Do NOT put raw source captures in `research/` — those go in a `references/` folder

### After creating or updating research

1. Check if `brand-decisions.md` needs a new 🔲 item or an update to an existing one
2. If research changes a decided element, flag it — don't silently overwrite ✅ decisions

## Maintaining Existing Research

- **When a brand decision ships:** Update `brand-decisions.md` to mark it ✅ with the rationale
- **When philosophy.md changes:** Check whether brand research files reference outdated philosophy claims
- **When the brand spec artifact updates:** Check whether reviews/ findings have been addressed
- **When research is superseded:** Don't delete — add a note pointing to the newer work
