---
name: lifecycle-1-research
description: Stage 1 of the engineering lifecycle — research and exploration that feeds /lifecycle-2-design. Conduct technical, cross-domain, or feasibility research that informs Precept's language design, architecture, tooling, or product positioning. Triggers on — research, investigate, survey, compare alternatives, evaluate feasibility, prior art, precedent, landscape, "how do other tools handle X". Use this for any task whose output is a markdown document in `research/` (or a domain-owned research folder), not code. Excludes: brand identity research (use `design/brand/research/`) and UX research (use `design/system/research/`).
---

# Precept Research (Lifecycle Stage 1)

Stage 1 of the engineering lifecycle. Conclusions that lock decisions feed forward to `/lifecycle-2-design`.

Research in this repo informs language design, architecture, tooling, and policy. The discipline matters as much as the findings — evidence-oriented, citation-rich, and never shadow policy.

## Step 1: Triage — Has This Been Researched Already?

Before any external investigation, check whether the team has already covered the ground:

- **Technical / cross-domain:** `research/` (subfolders: `language/`, `architecture/`, `philosophy/`, `product/`, `security/`, plus `language/expressiveness/` for comparator studies)
- **Brand:** `design/brand/research/`
- **UX / design system:** `design/system/research/`
- **Raw source captures:** each domain's `references/` folder
- **Critiques of specific artifacts:** each domain's `reviews/` folder

Read `research/README.md` and `research/language/README.md` first — they're the canonical entry points and index the existing work.

If existing research covers the question (in whole or in part), **cite it, don't duplicate it.** Build forward from prior findings; don't re-investigate solved problems.

## Step 2: Determine the Right Folder

The folder taxonomy is enforced — putting research in the wrong place creates shadow policy.

| Topic | Folder |
|---|---|
| Language expressiveness, comparator studies (xstate, polly, fluent-validation), proposal grounding | `research/language/` (or `research/language/expressiveness/` for tool comparators) |
| Compiler/runtime architecture options, scalability, feasibility | `research/architecture/` |
| Product philosophy, positioning evidence, entity-governance landscape | `research/philosophy/` or `research/product/` |
| Security investigations | `research/security/` |
| UCUM, domain curation, source material | `research/language/references/` or topic-appropriate `references/` |
| Brand identity, voice/tone, brand precedent | `design/brand/research/` (NOT `research/`) |
| Visual system, surface design, UX patterns | `design/system/research/` (NOT `research/`) |

If the topic spans domains (e.g., language + UX), put it in `research/` and cross-link from the secondary domain. Cross-domain synthesis is what `research/` is for.

## Step 3: Decide — Inline or Delegate

**Inline (do it in the active session):**
- ≤ 5 web fetches expected
- Single, narrow question
- Quick precedent check or feasibility sanity test
- Output is < 200 lines

**Delegate to a sub-agent (heavy lifting):**
- Multi-tool comparator survey (e.g., "how do xstate, statecharts.io, sCxml, fsm-as-promised handle X")
- Deep external corpus reading (specifications, academic papers, large blog posts)
- Many web fetches that would blow the parent context
- The output is expected to be > 300 lines or take > 30 minutes of investigation

**Delegation prompt template** (spawn `general-purpose` with this):

```
You are conducting research for the Precept project.

Methodology: read `.claude/skills/research/SKILL.md` and follow it strictly.

Topic: <one-sentence question>
Scope: <what's in scope, what's out>
Target folder: research/<subfolder>/
Suggested filename: <kebab-case-slug>.md

Investigate using WebFetch/WebSearch as needed. Check existing research in the
target folder first. Write the output file in the structure specified by the skill.

Return a one-paragraph summary of findings when done.
```

The sub-agent does the heavy lifting in isolation; you stay in the parent session.

## Step 4: Document Structure

Every research file follows this shape (adapt to topic, but keep these sections):

```markdown
# <Title>

> One-sentence framing of what this research investigates and why it matters.

## Background

What prompted this investigation. Link to the proposal issue, decision, or
upstream research that motivated it.

## Methodology

How the investigation was conducted — sources consulted, criteria applied,
what was deliberately excluded. Short.

## Findings

The substance. Citation-rich. Each claim grounded in a source (link, quote,
or named precedent).

## Implications for Precept

How the findings bear on Precept's design, architecture, or positioning. Make
the bridge explicit — research that doesn't connect back is just trivia.

## Conclusions

For each conclusion you're proposing the team adopt, include all four:

- **Rationale** — why this conclusion, not just what it is
- **Alternatives considered and rejected** — with reasons for rejection
- **Precedent** — the evidence from above that grounds it
- **Tradeoff accepted** — the known downside being taken on

A conclusion that states WHAT without WHY is incomplete. Flag it as draft.

## Open Questions

What this research did not resolve. What would need further investigation,
and roughly what shape that would take.

## Sources

Inline citations are preferred; this section is a backstop bibliography for
sources cited multiple times or worth promoting.
```

The Per-Decision Rationale shape (rationale + alternatives + precedent + tradeoff) is required by `CLAUDE.md` for any locked decision. Research that proposes conclusions must satisfy it; research that's purely exploratory can defer it to the proposal that consumes the research.

## Step 5: File Naming and Placement

- **Kebab-case** filenames: `parser-combinator-scalability.md`, `ucum-tier1-curation.md`, `entity-governance-landscape.md`
- **Descriptive, not generic:** prefer `xstate-statechart-comparison.md` over `state-machines.md`
- **Comparator studies** of a single tool go in `research/language/expressiveness/<tool>.md`
- **Surveys** spanning multiple tools go in the topical folder (e.g., `research/product/entity-governance-landscape.md`)
- **One file per coherent investigation** — split a sprawling topic into multiple files rather than producing a monolithic doc

## Step 6: Link, Don't Bury

Research only earns its keep if it gets read.

- If the research informs a GitHub proposal issue, **link from the issue body** to the research file.
- If `research/language/README.md` (or another subfolder README) maintains an issue map, **update it** to connect the new research to its consuming proposal.
- If the research promotes a conclusion that becomes a locked decision, that decision lives in a spec or design doc — **not** in the research file. The research is the evidence; the decision is the policy.

## Step 7: Promote-or-Cite Rule

Research does not become policy by sitting on disk. There are exactly two valid endpoints:

1. **Promoted** — the conclusions are adopted, and the decision is written into a spec, design doc, or `docs/philosophy.md` change (with owner approval for philosophy). The research file remains as evidence; the spec is the policy.
2. **Cited** — the research is referenced from a proposal issue, decision document, or another research file. It's load-bearing for something concrete.

If research is neither promoted nor cited, it's shadow policy — claims with no governance. Don't let it sit there. Either promote it, cite it, or move it to `research/archive/` with a one-line note on why it didn't ship.

## What NOT to Do

- **Don't duplicate existing research.** Check the folder structure first.
- **Don't write opinion without evidence.** Every claim needs a source, a quote, a citation, or named precedent.
- **Don't put brand or UX research in `research/`.** Use `design/brand/research/` or `design/system/research/`.
- **Don't bury conclusions.** If the research influences a proposal, link from the proposal.
- **Don't update `docs/philosophy.md`** based on research — surface the gap and wait for owner approval per CLAUDE.md.
- **Don't ship "research" that's actually a draft decision.** If the work is locking a decision, write the decision doc; cite the research that grounds it.
- **Don't run `dotnet build`** to validate `.precept` snippets in research. Use the `precept_compile` MCP tool.

## Quick Reference — Common Investigation Patterns

- *"How do other state-machine DSLs handle X?"* → `research/language/expressiveness/<tool-or-pattern>.md`, comparator structure
- *"Is implementation approach X feasible?"* → `research/architecture/<subsystem>/<approach>-feasibility.md`
- *"What does the landscape look like for Y?"* → `research/product/<y>-landscape.md` or `research/philosophy/<y>-evidence.md`
- *"What does the formal CS / academic literature say about Z?"* → `research/philosophy/` or `research/architecture/`, depending on bearing
- *"What domain catalog values should we ship for W?"* → `research/language/references/<w>-curation.md`

Stay evidence-oriented. The findings are the value — the methodology just makes sure they're trustworthy.
