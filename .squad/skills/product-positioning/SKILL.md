---
name: "product-positioning"
description: "Domain knowledge skill for Precept product positioning and strategy. Covers where PM research lives, how to capture new research, how to use it in decisions, and how to keep it current."
domain: "product-positioning"
confidence: "high"
source: "earned — generalized from language-design audit; ensures positioning work is grounded in market data and competitive landscape research"
---

## Context

This skill governs how agents work with the **product positioning research corpus** — market data, competitive landscape, developer adoption patterns, and positioning strategy that inform how Precept is framed and prioritized.

**Applies to:** Any agent doing positioning, roadmap, adoption, or strategy work. Primary users: Steinbrenner (PM), J. Peterman (DevRel/narrative), Frank (architecture decisions with positioning implications).

## Research Location

| Category | Path | Contents |
|----------|------|----------|
| Market positioning | `research/product/data-vs-state-pm-research.md` | Data-first vs state-first analysis, NuGet download ratios (FluentValidation ~250M vs Stateless ~25M), developer personas, wedge query space |
| Competitive landscape | `research/product/entity-governance-landscape.md` | Enterprise platform analogs (Salesforce, ServiceNow, Guidewire), validation libraries, state machine tools, with per-category capability gaps |
| README research | `research/product/readme-research-steinbrenner.md` | Developer evaluation journey (4 stages), project studies (xstate, Polly, etc.), section mapping, CTA patterns |
| Positioning evidence | `research/philosophy/entity-first-positioning-evidence.md` | Entity-first framing rationale, stateless entity positioning, commitment analysis |
| Philosophy research | `research/philosophy/` | Philosophy-level positioning evidence |
| Product philosophy | `docs/philosophy.md` | Category definition, core guarantees, what Precept is/isn't |
| Roadmap | `docs/@ToDo.md` | Active priorities and open work items |
| Brand decisions | `design/brand/brand-decisions.md` | Decided positioning, narrative archetype, voice (positioning overlaps brand) |

### Key data points to know

| Data point | Source |
|------------|--------|
| 10:1 download ratio (validation vs state machines) | `research/product/data-vs-state-pm-research.md` |
| "Frustrated validator" persona | `research/product/data-vs-state-pm-research.md` |
| Wedge queries: "validation depends on state c#" | `research/product/data-vs-state-pm-research.md` |
| "Salesforce-grade entity governance as a NuGet package" | `research/product/entity-governance-landscape.md` |
| 4-stage evaluation journey (5s → 60s → 5min → post-trial) | `research/product/readme-research-steinbrenner.md` |
| Category creation: "domain integrity engine" | `brand-decisions.md` |
| All 5 commitments hold for stateless entities | `research/philosophy/entity-first-positioning-evidence.md` |

## Using Research in Work

### Before any positioning decision

1. Check `brand-decisions.md` for what's already decided (✅ Positioning, ✅ Narrative archetype)
2. Read the relevant research file for the topic area
3. Read `docs/philosophy.md` when the work touches product identity
4. Check `docs/@ToDo.md` for roadmap context
5. **Cite specific data** — market numbers, persona definitions, capability gaps

### Citation standard

| Acceptable | NOT acceptable |
|---|---|
| "Per data-vs-state-pm-research.md: 10:1 download ratio (FluentValidation ~250M vs Stateless ~25M)" | "More developers search for validation" |
| "Per entity-governance-landscape.md §Salesforce: validation rules run inside a broader save pipeline, no equivalent of Inspect" | "Salesforce is more complex" |
| "Per readme-research-steinbrenner.md: Trial Decision stage (2-5 min) — 'Can I get this running quickly?'" | "Developers want quick starts" |

**Rule:** If a claim could be made by any PM without reading the file, it is not a citation.

## Capturing New Research

### Where to put it

| Type of finding | Location |
|-----------------|----------|
| Market/adoption research | `research/product/{topic}.md` |
| Competitive landscape addition | Update `research/product/entity-governance-landscape.md` |
| Philosophy-level positioning evidence | `research/philosophy/{topic}.md` |
| Roadmap change | Update `docs/@ToDo.md` |

### Format consistency

Every research file should include:
- **Date** and **researcher**
- **Scope** — what question it answers
- **Grounded in** — what existing files/decisions it builds on
- **Positioning implication** — what this means for how Precept is framed

## Maintaining Existing Research

- **When a new competitor or analog appears:** Add to `entity-governance-landscape.md` with the same structure (what it is → governance model → what Precept does/doesn't → positioning implication)
- **When philosophy.md changes:** Check whether positioning research references outdated claims
- **When download/market numbers update:** Refresh `data-vs-state-pm-research.md`
- **When the roadmap shifts:** Update `@ToDo.md` and check if prioritization research needs revision
