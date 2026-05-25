---
name: lifecycle-2-design
description: Stage 2 of the engineering lifecycle — lock a design with required structure (four-leg rationale per decision, acceptance criteria, doc-update enumeration). Triggers on — design, lock a design, propose, spec out, specification, "let's design X", design doc, formalize this approach. Takes a topic (and optional research source) and produces a locked design doc in `docs/Working/`. Refuses to mark designs "locked" without four-leg rationale on every decision.
---

# Precept Design Lock

Stage 2 of the engineering lifecycle. Produces a locked design doc in `docs/Working/` that downstream `/lifecycle-3-plan` and `/lifecycle-5-promote` skills can consume reliably.

## When to use

- Idea or research conclusion is ready to commit to a specific approach
- Implementation can't start until the design is locked (alternatives still in play; acceptance unclear)
- User says "let's design X" / "let's spec this out" / "lock this in"
- After `/lifecycle-1-research` produces conclusions that need to advance to a design

## When NOT to use

- Idea is too early — still need research (use `/lifecycle-1-research`)
- Already implementing — use `/lifecycle-5-promote` afterward to canonicalize
- Pure bug-fix or polish work — designs not warranted

## Required output structure

A markdown file at `docs/Working/<topic-slug>.md` with these sections:

```markdown
---
status: Locked YYYY-MM-DD
phase-target: <Phase N from current readiness plan, or 'TBD'>
sources-consulted:
  - <opaque source identifier>: <one-line note on what was checked>
  - <opaque source identifier>: <one-line note>
  # ...
---

# <Title>

## Goal
<One sentence, testable. "When done, X works as Y demonstrates.">

## Scope
- **In scope**: ...
- **Out of scope**: ...
- **Deferred to future**: ...

## Inventory of what will be built
File-level detail: catalog entries, type/record shapes, file paths, test stubs.
This is enumerable content — explicit and specific. Pointer-philosophy
does NOT apply here (this is the spec, not the canonical doc).

## Decisions

For each locked design decision, all four legs are REQUIRED:

### Decision N: <one-line decision>

- **Rationale**: why this choice
- **Alternatives considered**: each alternative + why it was rejected
- **Precedent**: research / prior art / existing pattern that grounds the choice
  (or explicit "no precedent — novel choice, accepting risk")
- **Tradeoff accepted**: the known downside being taken on
- **Sources consulted for this decision**: one or more source identifiers
  with a short verbatim or near-verbatim excerpt that proves the source was
  read (e.g., `path/to/file.cs:L1-L20 — "<excerpt>"`, or `docs/foo.md § N —
  "<excerpt>"`). Source identifier is opaque — anything with a permanent
  address (code file with line range, doc section, MCP tool query, URL,
  another design doc, test fixture, sample file, bug entry, RFC, etc.).
  Honest "no sources consulted — pure-policy choice, no external state
  informed this" is acceptable when true.

The skill refuses to mark a design "Locked" if any decision is missing
any of the five legs. Author must either fill the leg honestly or
explicitly state "no precedent" / "no tradeoff identified — flag for review"
/ "no sources consulted — pure-policy choice."

## Acceptance criteria
Test-shaped. "This passes" / "this fails as expected" / "this is documented in Y."
Specific enough that `/lifecycle-3-plan` can derive Phase exit criteria from them.

## Dependencies
- Upstream: what must be in place first (other locked designs, shipped code, owner decisions)
- Downstream: what this design enables

## Doc-update enumeration
Per the CLAUDE.md routing table, which canonical docs will need updates when this
design ships. Listed upfront so `/lifecycle-3-plan` can include them as Phase
sub-tasks and `/lifecycle-5-promote` can verify them at promotion time.

Example:
- `docs/language/precept-language-spec.md` § N — feature definition
- `docs/compiler/<stage>.md` § Design Rationale and Decisions — design lift
- `docs/language/catalog-system.md` § <catalog> — if new catalog entry

## Open questions
Anything unresolved. The skill refuses to mark "Locked" if any open question
remains. Either resolve or move to a separate Wave 0 triage doc.
```

## Behavioral guards

The skill enforces:

1. **No "Locked" status without four-leg decisions.** Every decision must carry Rationale + Alternatives + Precedent + Tradeoff. The skill checks for the four headers and asks the author to fill missing ones one at a time. Author can answer "no precedent — novel choice" or "no tradeoff identified — flag for review", but cannot skip the question.

2. **No "Locked" status with open questions.** Forces resolution before locking. If questions are too big to resolve in the session, the skill suggests creating a separate Wave 0 decision-triage doc.

3. **Acceptance criteria must be test-shaped.** The skill refuses vague criteria like "works correctly." Prompts for specific testable conditions.

4. **Doc-update enumeration must be present.** The skill consults the CLAUDE.md routing table for the file paths the design touches and pre-populates the doc-update section. Author can edit or expand.

5. **Every decision must cite the sources that informed it — with proof-of-reading.** A citation is `<source identifier> — <short verbatim excerpt>`. The excerpt is the forcing function: it can't be fabricated without opening the source. Citations are listed per-decision (under the "Sources consulted for this decision" leg) AND aggregated in the frontmatter `sources-consulted` field. The skill checks two things at lock time:
   - **Decision text vs. citations.** If a decision's prose names external state (a file path, a code identifier, a doc section, a tool, a sample, a bug ID, an enum, an interface, a precept feature, a research conclusion, another design doc) but the decision's `Sources consulted` leg is empty, refuse to lock. The author either cites what they consulted or explicitly declares "no sources consulted — pure-policy choice."
   - **Frontmatter aggregation.** `sources-consulted` in frontmatter must list every source identifier that appears in any decision's `Sources consulted` leg. The check is mechanical set membership — every per-decision citation also appears at the top of the doc.
   "Source" is an open category — anything with a permanent address that informed the design qualifies. The skill does NOT hardcode which source types are acceptable; the discipline is "cite what you read, regardless of what kind of thing it is."

## Composability

- **Input**: optional `--from <research-doc>` flag — extracts research conclusions and pre-populates the Decisions section's Rationale and Precedent legs from the research findings.
- **Output**: locked design at `docs/Working/<slug>.md` — consumed by `/lifecycle-3-plan` for phase planning, and later by `/lifecycle-5-promote` for canonical doc updates.

## Anti-patterns to refuse

- Skip the four-leg structure ("it's obvious")
- Leave acceptance criteria as prose ("the feature works")
- Skip doc-update enumeration ("I'll figure it out later")
- Mark "Locked" with `(?)` markers or `TBD` placeholders in decision rationale
- Cite a source without an excerpt ("Consulted: `Modifiers.cs`" — bare; no proof of reading). The excerpt is the forcing function. Bare-path citations are refused.
- Make claims about external state with no `Sources consulted` ("The catalog already has 8 of these" — no citation). The skill refuses to lock when prose references external state but the citation leg is empty.

## Quick reference

| Symptom | Skill response |
|---|---|
| "Locked" status but missing Alternatives leg | Refuse; prompt for each missing leg in turn |
| Vague acceptance ("works correctly") | Refuse; ask "what test demonstrates this?" |
| No doc-update enumeration | Auto-populate from CLAUDE.md routing table; let author edit |
| Decision lacks Precedent | Accept "no precedent — novel" as honest answer; do not invent precedent |
| Open question remains | Refuse "Locked"; offer to move to Wave 0 triage doc |
| Decision references external state but `Sources consulted` empty | Refuse; ask the author to cite what they read or honestly declare "pure-policy choice — no external state informed this" |
| Citation has no excerpt (bare path or section name) | Refuse; ask the author to open the source and paste a short verbatim excerpt |
| Per-decision citations not aggregated in frontmatter `sources-consulted` | Auto-aggregate; author confirms |
