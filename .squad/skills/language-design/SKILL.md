---
name: "language-design"
description: "Domain knowledge skill for Precept DSL design. Covers where language research lives, how to capture new research, how to use it in decisions, and how to keep it current."
domain: "language-design"
confidence: "high"
source: "earned — audit of Frank's #13 review revealed selective research reading; broadened from review-only to full research lifecycle"
---

## Context

This skill governs how agents work with the **language design research corpus** — reading it before making decisions, capturing new findings consistently, and keeping it current as the DSL evolves.

**Applies to:** Any agent doing language-related work — design proposals, feasibility analysis, implementation planning, code review of parser/runtime changes, or writing DSL samples. Primary users: Frank (Lead/Architect), George (Runtime Dev), Kramer (Tooling), Soup Nazi (Tester on language features).

**Why this exists:** An audit found agents citing research files by name without engaging the data — reconstructing tables from general knowledge, skipping the verbosity analysis, and treating PLT concepts as common knowledge without attribution. Research is only useful if agents actually read it and build on it.

## Research Location

| Category | Path | Contents |
|----------|------|----------|
| Research map | `research/language/README.md` | Issue → research grounding map, cross-cutting file index |
| Domain studies | `research/language/expressiveness/` | Per-domain expressiveness surveys, verbosity analysis, expression audit |
| PLT theory | `research/language/references/` | Formal references — type systems, constraints, expressions, state machines |
| Batch planning | `research/language/domain-research-batches.md` | Research batch structure and sequencing |
| Domain map | `research/language/domain-map.md` | Which domains have been studied and which are open |
| Language spec | `docs/PreceptLanguageDesign.md` | Canonical record of what EXISTS in the runtime |
| Implementation plans | `docs/archive/PreceptLanguageImplementationPlan.md` | Archived — all phases implemented |

### Key cross-cutting files (always relevant)

- **Verbosity analysis** — `expressiveness/internal-verbosity-analysis.md` — corpus-wide statement counts, gate metrics, verbosity smell rankings, sample-level data
- **Expression language audit** — `expressiveness/expression-language-audit.md` — expression coverage, operator behavior, evaluation gaps

## Using Research in Work

### Before any language decision

1. Read `research/language/README.md` § "Open proposal issue map" to find relevant files
2. Read the domain study and its PLT companion for the area you're working in
3. Check the verbosity analysis — it's always relevant, even if the task isn't about verbosity
4. **Cite specific data** in your output — at least 2 data points per file read

### Citation standard — "show the data"

For each research file read, the review output must contain **at least 2 specific data points** that could only come from reading that file. Acceptable citations:

| Citation type | Example | NOT acceptable |
|---|---|---|
| **Named number** | "46 of 55 invariant statements (84%) enforce basic data-shape bounds" | "Most invariants are simple bounds" |
| **Named framework** | "The Evans Specification Pattern (DDD Ch. 9) uses And/Or/Not combinators" | "Specification patterns exist in DDD" |
| **Exact finding** | "Zero of 21 samples pass the 6–8 statement gate; closest is crosswalk-signal at 29" | "Samples are verbose" |
| **Sample-specific data** | "loan-application has 44 statements; 4 are non-negative invariants" | "Several samples have non-negative checks" |
| **Theory attribution** | "Pombrio & Krishnamurthi's resugaring framework requires diagnostic fidelity" | "Error messages should reference the original syntax" |

**Rule:** If a claim could be made by any expert in constraint languages without reading the file, it is not a citation. Cite the file-specific evidence.

### 4. Precedent survey integration

The domain research files contain structured precedent surveys (tables comparing Zod, FluentValidation, Pydantic, SQL, etc.). When the review needs comparable-system evidence:

- **Reference the existing table** in the research file — do NOT reconstruct it from general knowledge
- If the existing table doesn't cover the angle needed, **extend it** with new rows/columns and note what's new vs. what's from the corpus
- Cite the table's location: "Per the precedent survey in constraint-composition-domain.md §Precedent Survey..."

### 5. Verbosity analysis integration

The verbosity analysis (`internal-verbosity-analysis.md`) contains:

- **Statement count distribution** for all 21+ samples (range, median, sorted list)
- **Gate metric:** 6–8 target statements; zero samples pass
- **Three ranked verbosity smells:** (1) Event argument ingestion, (2) Guard-pair header duplication, (3) Non-negative numeric invariants
- **Sample-specific counts** with breakdowns (declarations, headers, actions)

When a proposal claims to reduce verbosity, the review must:
- Cite the specific verbosity smell ranking it addresses
- Quote the before/after statement counts for affected samples
- State whether the proposal moves any sample closer to the gate

### 6. Output structure — Research Citations section

Every language design review must include a dedicated section:

```markdown
## Research Citations

### Domain research: {filename}
- {specific data point 1}
- {specific data point 2}

### PLT theory: {filename}
- {specific framework or finding 1}
- {specific framework or finding 2}

### Verbosity analysis
- {specific metric or sample data 1}
- {specific metric or sample data 2}

### Not found in research
- {any claim made from general knowledge — flagged explicitly}
```

This section makes citation auditable. Claims that aren't grounded in the corpus must be explicitly marked as general expertise.

## Capturing New Research

When you discover something worth preserving — a precedent survey, a design tradeoff analysis, a PLT framework comparison, or data from evaluating a proposal:

### Where to put it

| Type of finding | Location | Format |
|-----------------|----------|--------|
| Domain expressiveness study (new area) | `research/language/expressiveness/{domain}.md` | Problem statement → precedent survey → Precept implications |
| PLT theory reference | `research/language/references/{topic}.md` | Framework name → source → key concepts → Precept relevance |
| Proposal-specific analysis | `research/language/expressiveness/{proposal-domain}.md` | Extend existing file or create new domain file |
| Verbosity findings | Update `expressiveness/internal-verbosity-analysis.md` | Add rows to existing tables, update counts |
| Cross-cutting insight | `research/language/README.md` § "Cross-cutting research" | Add file reference + one-line description |

### Format consistency

Every research file should include:
- **Date** and **author**
- **Scope** — what question it answers
- **Grounded in** — what input files/data it builds on
- **Precedent survey** (if applicable) — structured table, not prose
- **Precept implications** — what this means for the DSL, not just what was found

### Maintaining the research map

After creating or updating research:
1. Update `research/language/README.md` — add/update the issue map entry and cross-cutting index
2. Update `research/language/domain-map.md` if a new domain was studied

## Maintaining Existing Research

- **When a proposal ships:** Update the domain study to note what was implemented vs. what remains open
- **When samples change:** Re-run verbosity counts and update `internal-verbosity-analysis.md`
- **When a precedent survey is extended:** Note what's new vs. what was already there
- **When research is superseded:** Don't delete — add a note pointing to the newer work

## Examples

### Good — grounded in corpus data

> "The constraint-composition-domain study found 46 of 55 invariants (84%) enforce basic data-shape bounds. The verbosity analysis confirms this is the #3 ranked verbosity smell, present in 19 of 21 samples. The Evans Specification Pattern (constraint-composition.md §Specification Pattern) uses combinable And/Or/Not predicates — Precept's keyword constraints take a different approach (closed vocabulary vs. open combinators) but serve the same scope tier (field-local)."

### Bad — general expertise dressed as research

> "Most constraint systems have both keyword-form and predicate-form constraints. The research confirms this is the universal pattern. The verbosity evidence supports co-location."

This cites nothing specific. Any constraint-language expert could write it without reading any file.

## Anti-Patterns

1. **Reconstructing tables that already exist.** If the domain research has a precedent survey, reference it — don't build a parallel one from memory.
2. **Claiming "research grounding solid" without citations.** Meta-assessment of research quality is not citation. Show the data.
3. **Skipping the verbosity analysis.** The charter says it's "always relevant." Zero engagement = skill violation. Even if the proposal isn't primarily about verbosity, state what the verbosity data says about the affected construct.
4. **Citing file names without file content.** "Per constraint-composition-domain.md" is a file reference, not a citation. What does the file say? Quote the finding.
5. **Using PLT concepts without attribution.** If the concept comes from a named framework in the theory file (Evans, Pombrio & Krishnamurthi, Boolean lattice), cite it by name and source.
