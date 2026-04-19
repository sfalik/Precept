---
name: "external-research"
description: "How to conduct external research on comparable systems, capture findings in the repo, and connect them to proposal issues. Covers research angles, methodology, output format, and the commit-to-repo requirement."
domain: "research, design review, language design, architecture"
confidence: "medium"
source: "earned — issue #115 evaluator research ceremony 2026-04-19; Frank + George parallel research on numeric lane integrity"
---

# Skill: External Research — Structured Evidence Gathering

## The Problem

Design decisions made without external evidence are fragile — they can't be defended during review, challenged constructively, or revisited with new information. Research that stays in chat context is effectively lost: it doesn't survive sessions, isn't discoverable by other agents, and can't be cited from proposal issues.

## The Pattern

**External research is conducted by 2+ agents with complementary angles, committed to `research/` in the repo, and referenced from the proposal issue.** Findings are structured, source-cited, and explicitly mapped to the design decisions they support or challenge.

## When This Applies

- Any **Track B proposal** (introduces a new canonical design doc) — research is mandatory
- Any proposal where **design decisions lack external precedent evidence** — research is strongly recommended
- Any proposal touching a **domain the team hasn't built in before** (new type system, new evaluation model, new serialization format)
- When the user says "research", "look at how others do this", "find precedent", "external evidence"

## Research Methodology

### 1. Define Research Angles

Before spawning researchers, decompose the question into 3–5 complementary angles. Each angle should be assigned to the agent best equipped for it.

| Angle type | Example | Best-fit agent |
|------------|---------|----------------|
| **Architecture patterns** | How do comparable systems architect this? | Lead/Architect |
| **Implementation patterns** | How do .NET systems implement this? | Runtime Dev |
| **Precedent survey** | What do 5+ production systems do? | Any technical agent |
| **Testing patterns** | How do production systems test this? | Tester or Runtime Dev |
| **Standards/specs** | What do formal specs say? | Lead/Architect |

**Rule:** At least two angles must be covered by different agents. Single-agent research misses blind spots.

### 2. Fetch Real Sources

Agents MUST use `fetch_webpage` or equivalent to retrieve actual documentation, source code, blog posts, or spec text. Research based purely on training data is not acceptable — it can't be cited, verified, or updated.

**Source quality hierarchy:**
1. Official language/framework specs (C# spec, ECMAScript spec, DMN spec)
2. Primary source code (GitHub repos of the systems being studied)
3. Official documentation (learn.microsoft.com, kotlinlang.org, etc.)
4. Engineering blog posts from the system's authors
5. Third-party analysis (use with caution — verify claims against primary sources)

**Minimum source count:** Each research angle should cite at least 3 distinct sources. Fewer than 3 suggests insufficient coverage.

### 3. Structure Findings

Each finding follows this format:

```markdown
### {System or Pattern Name}

**Source:** {URL or reference}

**Key insight:** {1-2 sentences — what this teaches us}

**Relevance to Precept:** {How this maps to our specific design question}

**Recommendation:** Adopt / Reject / Inform — with reasoning
```

Group findings under the research angle headings. Within each angle, order by relevance (most relevant first).

### 4. Map Findings to Design Decisions

If the proposal has numbered design decisions (DD1, DD2, etc.), every finding must state which decision(s) it **validates**, **challenges**, or is **neutral** toward. This makes the research directly actionable during design review.

**Format:**
```markdown
**DD impact:** Validates DD1, DD2. Challenges DD9 — context-sensitive literals
are a DSL policy choice, not host-language precedent.
```

### 5. Produce a Summary Table

End the research file with an actionable summary:

```markdown
## Summary

| Design Decision | External Evidence | Strength | Action |
|-----------------|-------------------|----------|--------|
| DD1: Three lanes | CEL, C# spec, F# | Strong | Validated — no change needed |
| DD9: Context literals | NCalc, DynamicExpresso | Moderate | Reframe as DSL policy, not precedent |
```

### 6. Commit to `research/`

**This is non-negotiable.** Research that stays in chat is lost research.

- File location: `research/{domain}/{topic}.md` (e.g., `research/language/evaluator-architecture-survey.md`)
- If the domain folder doesn't exist, create it
- Update `research/{domain}/README.md` (or `research/README.md`) with a reference to the new file
- Reference the research file from the proposal issue (as an issue comment with a link)

### 7. Flag Gaps and Open Questions

If research reveals questions that can't be answered from external sources (e.g., "benchmark decimal vs double performance locally"), list them explicitly as **open items** at the end of the research file. These become inputs to the design review agenda.

## Output Quality Bar

A research file is ready for design review when:

- [ ] At least 2 complementary angles covered
- [ ] Each angle has 3+ distinct cited sources
- [ ] Findings are mapped to design decisions (if applicable)
- [ ] Summary table with evidence strength and recommended action
- [ ] Committed to `research/` (not floating in chat)
- [ ] Referenced from the proposal issue
- [ ] Open questions listed explicitly

## Anti-Patterns

| Anti-pattern | Why it fails | Fix |
|--------------|-------------|-----|
| Research from memory only | Can't be cited, verified, or challenged | Fetch real sources |
| Single-agent research | Misses blind spots from one perspective | 2+ agents with different angles |
| Research without DD mapping | Findings float without connection to decisions | Map every finding to a decision |
| Chat-only research | Lost after session ends | Commit to `research/` |
| Research after design review | Evidence arrives too late to inform decisions | Research triggers before design review |
| Averaging conflicting findings | Hides real disagreement | Surface conflicts explicitly, apply authority hierarchy |

## Integration with Other Skills

- **multi-researcher-synthesis** — use when combining findings from 2+ research agents into a single file
- **proposal-review** — research findings are inputs to the review; reviewers cite them when evaluating design decisions
- **dsl-philosophy-filter** — external precedent helps answer the 7 philosophy filter questions with evidence, not opinion

## Example

Issue #115 (numeric lane integrity campaign) — two parallel research passes:

1. **Frank (architecture):** CEL, FEEL/DMN, NRules, C# spec numeric promotions, Pratt parser dispatch patterns
2. **George (implementation):** .NET decimal handling, JSON serialization fidelity, NCalc/DynamicExpresso type dispatch, EF Core type mapping, property-based testing patterns

Findings validated DD1–DD6, identified DD9 as needing reframing, and surfaced three implementation recommendations (dispatch table, type+value test assertions, decimal overflow policy).
