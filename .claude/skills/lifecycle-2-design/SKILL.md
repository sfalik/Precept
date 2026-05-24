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

The skill refuses to mark a design "Locked" if any decision is missing
any of the four legs. Author must either fill the leg honestly or
explicitly state "no precedent" / "no tradeoff identified — flag for review."

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

## Composability

- **Input**: optional `--from <research-doc>` flag — extracts research conclusions and pre-populates the Decisions section's Rationale and Precedent legs from the research findings.
- **Output**: locked design at `docs/Working/<slug>.md` — consumed by `/lifecycle-3-plan` for phase planning, and later by `/lifecycle-5-promote` for canonical doc updates.

## Anti-patterns to refuse

- Skip the four-leg structure ("it's obvious")
- Leave acceptance criteria as prose ("the feature works")
- Skip doc-update enumeration ("I'll figure it out later")
- Mark "Locked" with `(?)` markers or `TBD` placeholders in decision rationale

## Quick reference

| Symptom | Skill response |
|---|---|
| "Locked" status but missing Alternatives leg | Refuse; prompt for each missing leg in turn |
| Vague acceptance ("works correctly") | Refuse; ask "what test demonstrates this?" |
| No doc-update enumeration | Auto-populate from CLAUDE.md routing table; let author edit |
| Decision lacks Precedent | Accept "no precedent — novel" as honest answer; do not invent precedent |
| Open question remains | Refuse "Locked"; offer to move to Wave 0 triage doc |
