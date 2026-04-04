# README Research Skill

**Skill:** Comparative README research for developer libraries/tools  
**Owner:** J. Peterman (Brand/DevRel)  
**Created:** 2025-01-18  
**Status:** Active  

---

## Purpose

Research and synthesize README patterns from comparable and exemplar projects to inform README structure, hero code conventions, positioning language, and copy tone for a new developer library or tool.

---

## When to Use

- Before writing or revising a project README
- When positioning a new developer tool or library
- When selecting a hero code example
- When defining README structure (Content-Rich vs. Gateway vs. Hybrid)
- When researching competitive positioning language

---

## Research Method

### 1. Select Projects to Study

**Comparable projects** (8-10):
- Same domain or adjacent domains (e.g., state machines, validation, rules engines)
- Similar platform (.NET, TypeScript, JavaScript, etc.)
- Established libraries with mature READMEs

**Exemplar projects** (3-5):
- Famous for excellent READMEs regardless of domain
- Category-creating tools (e.g., Bun, React, Temporal)
- Developer tools with strong first-impression content

### 2. Fetch READMEs

Use `github-mcp-server-get_file_contents` to fetch actual README.md files. **Do not describe what you imagine they look like — read the actual content.**

### 3. Measure Each Project

For every project studied, record:

| Measurement | How to Measure |
|-------------|----------------|
| **Opening hook** | Exact text of first sentence or paragraph, word count |
| **Hero placement** | Lines from top of file to start of hero code |
| **Hero code** | Line count of primary code example |
| **Section structure** | Ordered list of top-level headings |
| **Positioning claim** | Exact text of one-sentence descriptor ("X is a Y for Z") |
| **AI-first signals** | Any mentions of MCP, AI agents, GPT, Copilot, or agent-aware tooling |
| **Notable patterns** | Anything distinctive (diagrams, side-by-side tables, philosophy sections, sponsor placement, etc.) |

### 4. Synthesize Findings

After studying 8+ projects, synthesize:

1. **Structural patterns** — What are the dominant README models? (Content-Rich, Gateway, Hybrid?)
2. **Hero section conventions** — Where does hero code appear? How long? What does it show?
3. **Positioning language** — What structure do category-creating tools use? What length? What elements?
4. **Copy conventions** — Tone (declarative vs. hedging), structure (bullets vs. prose), voice (technical vs. casual)?
5. **AI-first signals** — How common is AI tooling mentioned? How is it positioned?
6. **Competitive positioning** — Do projects compare explicitly? Implicitly? Not at all?

### 5. Document Findings

Write findings to a structured markdown file with:

- **Projects Studied** section (one subsection per project with all measurements)
- **Synthesis** section (structural patterns, hero conventions, positioning, copy, AI-first, competitive positioning)
- **Recommendations** section (specific structure, hero code, positioning, tone for your project)
- **Appendix** with measurement table (all projects, all metrics in one comparison table)

---

## Output Format

### Research File Structure

```markdown
# README Research — [Your Perspective]

**By:** [Your Name/Role]  
**Date:** [YYYY-MM-DD]  
**Status:** Research complete — synthesis ready for [action]

---

## Research Method

[Describe selection criteria, measurement approach, synthesis method]

---

## Projects Studied

### [Project Name]
**Domain:** [Brief description]

- **Opening hook:** "[exact text]" ([word count] words)
- **Hero placement:** [N] lines from top, [N] lines of code
- **Positioning:** "[exact tagline/descriptor]"
- **Section structure:** [ordered list]
- **AI-first signals:** [Yes/No + details]
- **Notable patterns:** [bulleted observations]

[Repeat for each project]

---

## Synthesis: What Makes a Great README

### Structural Patterns
[Analysis of README models observed]

### Hero Section Conventions
[Analysis of hero code placement, length, complexity]

### Positioning Language Patterns
[Analysis of how tools position themselves]

### Copy Conventions
[Analysis of tone, structure, voice]

### AI-First Signals
[Analysis of how projects handle AI tooling]

### Competitive Positioning
[Analysis of explicit vs. implicit vs. no comparison]

---

## Recommendations for [Your Project]

### 1. Structure: [Model Name]
[Specific structure recommendation with rationale]

### 2. Hero Code: [Length] Lines, [Content Type]
[Specific hero example recommendation with rationale]

### 3. Positioning: [Language Pattern]
[Specific positioning sentence(s) with rationale]

### 4. [Other Recommendations]
[Additional sections as needed]

---

## Appendix: Measurement Table

| Project | Opening Hook (words) | Hero Placement (lines) | Hero Code (lines) | README Model |
|---------|---------------------|------------------------|-------------------|--------------|
| ... | ... | ... | ... | ... |
```

---

## Key Principles

1. **Real data only** — Fetch actual READMEs, measure actual line counts, quote actual text. No imagined or inferred data.
2. **Study both comparable and exemplar** — Comparable projects show category norms, exemplar projects show what's possible.
3. **Measure everything** — Word counts, line counts, placement — precision builds trust in synthesis.
4. **Synthesize patterns, not opinions** — "8 of 10 projects use X" is synthesis. "I think X is better" is opinion.
5. **Concrete recommendations** — "Use Hybrid model with 18-line hero" is actionable. "Consider a code example" is not.

---

## Examples of Use

### Example 1: README Research for Precept (2025-01-18)

**Context:** Precept is a domain integrity engine for .NET. Needed README structure, hero code conventions, positioning language.

**Projects studied:** 13 (8 comparable: XState, FluentValidation, Stateless, Zod, Polly, MediatR, FastEndpoints, NRules; 5 exemplar: Fastlane, Bun, Biome, React, Vue)

**Key findings:**
- Three README models identified: Content-Rich (XState, Stateless, Polly), Gateway (Bun, FastEndpoints, Vue), Hybrid (Zod, FluentValidation, React)
- Hero code ranges 6–26 lines (median 13)
- AI-first positioning rare (only NRules mentions GPT tooling) — opportunity for Precept
- Category-creating tools use "[X] is a [new category] for [platform]" positioning structure

**Recommendations:**
- **Structure:** Hybrid model (hook + hero + AI-first section + features + links)
- **Hero:** 18 lines (Subscription Billing DSL sample, no runtime code)
- **Positioning:** "Precept is a domain integrity engine for .NET that binds an entity's state, data, and business rules into a single executable DSL contract."
- **AI-first section:** Lead with MCP + Copilot + LSP as unique differentiator

**Output:** `brand/references/readme-research-peterman.md` (36KB, 13 projects, full synthesis)

---

## Related Skills

- **Hero Example Selection** — Choosing and validating hero code examples (separate from README research)
- **Competitive Analysis** — Deeper competitive positioning research beyond README content
- **Copy Review** — Voice, tone, and brand consistency review for developer-facing content

---

## Notes

- This skill is **research**, not **writing** — it produces recommendations, not draft READMEs
- README research should be done **before** drafting or revising a README, not after
- Research findings should be **filed** for future reference — they inform more than just the immediate README revision
- If research reveals a pattern (e.g., "AI-first positioning is rare"), that's a **competitive opportunity** — flag it explicitly in recommendations
