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

## Before you start

Read these before writing any section of the design. They ground every decision; reading them after is too late.

**Always — these three first:**
- `docs/philosophy.md` — Precept's core commitments. Every section of the design is evaluated against these.
- `docs/README.md` — the doc landscape and navigation gateway. Know what exists before deciding what to read.
- `docs/language/README.md` — the language surface: spec, canonical types, grammar, catalog as source of truth. Precept's design decisions are language decisions; this is the primary substance.

**Then navigate by topic using the README system.**

Each area has a README that maps its documents, reading order, and cross-references. Start with the README for your topic — it tells you which docs to read and in what order. Don't guess.

| Topic | Entry point | What to look for |
|---|---|---|
| Language surface (token, keyword, type, operator, modifier, construct, accessor) | `docs/language/README.md` | Relevant spec sections and type docs; reading order starts with `precept-language-spec.md` |
| Comparable systems, PLT theory, language precedent | `research/language/README.md` | Domain index maps each language domain to its expressiveness study and theory companion |
| Pipeline stage (lexer, parser, type checker, proof engine, etc.) | `docs/compiler/README.md` | Stage doc for the relevant stage; cross-cutting: `diagnostic-system.md`, `literal-system.md` |
| How pipeline stages connect, artifact types | `docs/compiler-and-runtime-design.md` | Read before diving into individual stage docs; carries the non-negotiable rules and catalog-first invariants that ground stage-level decisions |
| Runtime API or public contract | `docs/runtime/README.md` | Stage docs in reading order; `runtime-api.md` for the public surface |
| Tooling (LS, MCP, extension) | `docs/tooling/README.md` | Component doc for the relevant tool |

Each README's **Reading Order** and **Relationship to Other Docs** sections tell you what to read next. Follow those, not intuition. When a design spans multiple areas, follow the cross-references between READMEs.

These are not optional. If you haven't read what a section depends on, don't write it — go read first.

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

## Philosophy Alignment

[Required for every design — "Not applicable" is not acceptable.]

Fill the principle-coverage matrix. Every principle from `docs/language/precept-language-spec.md § 0.1` (eleven principles) is a row. No row may be left blank — if a principle is unaffected, say "N/A" explicitly with a one-line justification. The matrix forces engagement with the full principle set, not a curated subset.

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention not detection | | | | |
| 2. One file, complete rules | | | | |
| 3. Determinism | | | | |
| 4. Full inspectability | | | | |
| 5. Keyword-anchored readability | | | | |
| 6. Governance not validation | | | | |
| 7. Compile-time totality | | | | |
| 8. Honesty about approximation | | | | |
| 9. Mandatory rationale (`because`) | | | | |
| 10. Static semantic checking | | | | |
| 11. Static completeness (no runtime faults from well-typed programs) | | | | |

Then, for any row marked Affected? = Y with a Tension or Tradeoff that isn't N/A: state the tradeoff being accepted and why it's justified, in 2-3 sentences. See `docs/philosophy.md` for the canonical commitments.

**Companion commitments** (from philosophy.md, not in § 0.1 but still load-bearing): Stateless-first-class, Domain-expert-primary-author. Address these in a brief paragraph after the matrix — does this design respect them? — unless they are trivially N/A for this design.

## Language Design Grounding

[Required when the design touches language surface — new token, keyword, construct, modifier, type, operator, accessor, or expression form. Omit with an explicit one-line note for designs that don't touch language surface.]

**General language design:**
What does the field say about this kind of construct? State the evaluation semantics precisely — binding, evaluation order, type inference implications. What do comparable languages or DSLs do? What does Precept take from those approaches and what does it deliberately diverge from, and why?

Check `research/language/README.md` — its domain index maps each language domain to its expressiveness study and theory companion. If a relevant study exists, cite it. If none exists for this domain, note the gap explicitly.

Citing only Precept-internal docs for this sub-section is not acceptable — language surface decisions must be grounded in the broader field.

**Precept-specific application:**
Which principles or deliberate exclusions in `docs/language/precept-language-spec.md` does this proposal touch, extend, or risk conflicting with? Cite by section or principle number.

## Audience and Teachability

[Required when the design touches language surface — new token, keyword, construct, modifier, type, operator, accessor, or expression form. Omit with an explicit one-line note for designs that don't touch language surface.]

Precept's primary author is the **domain expert**, not the developer (see `docs/philosophy.md § Who authors a precept` and `docs/language/precept-language-spec.md § 0.7 Authoring Audience`). Language surface decisions must serve that reader. Provide:

**Worked example.** A 5-10 line `.precept` snippet a domain expert would actually write using this feature. Plausible domain (financial, lifecycle, regulatory, scheduling, etc.), not a synthetic compiler-test fragment. Show the feature in its intended context, not in isolation.

**Error message.** Pick one specific misuse a domain expert is plausibly going to commit. Write the diagnostic message exactly as it would appear (PRE-code, audience-targeted wording, recovery hint if applicable). Explain in one sentence why the wording serves the domain-expert reader rather than the developer.

**10-minute teaching path.** What does the domain expert need to read to use this feature? Ordered list of 2-5 docs / sections / sample files. The path must be ≤10 minutes for a competent domain expert; if it isn't, the feature is too complex for the surface and should be reconsidered.

**Reviewer obligation.** A missing Audience and Teachability section on a language-surface change is a BLOCKER. A worked example that's a compiler-test fragment rather than a plausible domain scenario is a CONCERN. An error message that uses compiler-internal vocabulary is a CONCERN.

## Semantic Rules

[Required when the design touches expression evaluation, typing rules, proof obligations, or constraint semantics. Omit with an explicit one-line note for designs that touch only diagnostics, formatting, or documentation.]

State the semantic rules precisely enough that a competent reader can derive the construct's behavior without ambiguity. Required content:

**Evaluation / reduction rules.** For new expression forms, state the reduction rule. Prose notation is acceptable; rule notation is preferred for non-trivial cases. Example forms:

```
E[set X to e]  →  E'[X = v]   where  e ⇓ v
```

For constructs that don't introduce expressions, state the binding rule, evaluation order, or transition rule analogously.

**Typing rules.** For new typing behavior, sketch the inference rule with premises and conclusion. Hindley-Milner style is acceptable:

```
  Γ ⊢ e : τ      τ ∈ AcceptedTypes(modifier)
  ──────────────────────────────────────────
       Γ ⊢ field X modifier e : τ
```

**Proof obligations.** For constructs that introduce new proof obligations, state what the proof engine must establish before the construct is accepted. Cite the ProofRequirement catalog entry the obligation maps to (or note the new entry being added).

**Soundness preservation claim.** Name the specific principles from `docs/language/precept-language-spec.md § 0.1` that this construct could threaten (most often Principles 7, 10, 11 — totality, static semantic checking, static completeness). For each, state in one sentence why the principle continues to hold after this construct ships. Example: "Principle 11 holds because the new construct produces no expression form whose evaluation is undefined; the proof engine discharges divisor safety and bounds before any runtime path is reachable."

**Reviewer obligation.** A design touching evaluation, proof obligations, or typing without a Semantic Rules section is a BLOCKER. Prose descriptions of behavior without reduction/typing rule notation are CONCERNs for non-trivial cases.

## Architecture Grounding

[Required when the design touches catalog structure, pipeline stage boundaries, public API contracts, or cross-component interfaces. Omit with an explicit one-line note for designs that don't touch these surfaces.]

**Layer placement:**
Which layer does this behavior belong in — catalog metadata, pipeline stage, public API, tooling derivation? Why does it belong there and not in an adjacent layer? If behavior is being placed in pipeline code rather than catalog metadata, explain the structural limitation that requires it.

**Cross-component propagation:**
How does this change propagate across component boundaries? State the impact for each, or explicitly note "None":
- Runtime (parser, type checker, evaluator, diagnostics):
- Tooling (syntax highlighting, completions, hover, semantic tokens):
- MCP (vocabulary, DTOs, tool output):

**Breaking changes:**
Does this change any public contract — API surface, diagnostic codes, catalog member names that flow to grammar/completions/MCP vocabulary? If yes, state explicitly.

**General architecture:**
What do comparable systems handle for this kind of design problem? What does Precept's approach take from or deliberately diverge from those patterns, and why?

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

1. **No "Locked" status without Philosophy Alignment.** The section must be present and the principle-coverage matrix must be filled — every one of the eleven principles in `precept-language-spec.md § 0.1` must have a row with no blank cells. Rows marked "N/A" require a one-line justification. "This design is consistent with Precept's philosophy" with no matrix is refused. The companion-commitments paragraph (Stateless-first-class, Domain-expert-primary-author) must be present unless trivially N/A.

2. **Language surface changes require Language Design Grounding.** If the design introduces or modifies any token, keyword, construct, modifier, type, operator, accessor, or expression form: the section must be present and both sub-sections must be substantive. The "general language design" sub-section must engage with the broader field — comparable languages, PLT theory, or explicit acknowledgment of a gap in `research/language/`. Citing only Precept-internal docs is refused.

3. **Language surface changes require Audience and Teachability.** Same trigger as guard 2. The worked example must be plausible-domain (not a compiler-test fragment), the error message must use domain-targeted vocabulary, and the 10-minute teaching path must be enumerated. Missing this section on a language-surface change is refused.

4. **Designs touching evaluation, proof, or typing require Semantic Rules.** If the design introduces a new expression form, modifies typing behavior, adds a proof obligation, or changes constraint semantics: the Semantic Rules section must be present with reduction/typing-rule sketches and a soundness-preservation claim naming the specific principles preserved. Prose descriptions without notation are refused for non-trivial cases.

5. **Pipeline/API/catalog changes require Architecture Grounding.** If the design touches catalog structure, pipeline stage boundaries, public API contracts, or cross-component interfaces: all three sub-sections (layer placement, cross-component propagation, breaking changes) must be present and addressed. Any propagation category left blank rather than explicitly "None" is refused.

6. **No "Locked" status without four-leg decisions.** Every decision must carry Rationale + Alternatives + Precedent + Tradeoff. The skill checks for the four headers and asks the author to fill missing ones one at a time. Author can answer "no precedent — novel choice" or "no tradeoff identified — flag for review", but cannot skip the question.

7. **No "Locked" status with open questions.** Forces resolution before locking. If questions are too big to resolve in the session, the skill suggests creating a separate Wave 0 decision-triage doc.

8. **Acceptance criteria must be test-shaped.** The skill refuses vague criteria like "works correctly." Prompts for specific testable conditions.

9. **Doc-update enumeration must be present.** The skill consults the CLAUDE.md routing table for the file paths the design touches and pre-populates the doc-update section. Author can edit or expand.

10. **Every decision must cite the sources that informed it — with proof-of-reading.** A citation is `<source identifier> — <short verbatim excerpt>`. The excerpt is the forcing function: it can't be fabricated without opening the source. Citations are listed per-decision (under the "Sources consulted for this decision" leg) AND aggregated in the frontmatter `sources-consulted` field. The skill checks two things at lock time:
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
- Write `## Philosophy Alignment` as a single sentence ("this design is consistent with Precept's philosophy") — requires addressing each commitment specifically
- Omit `## Language Design Grounding` for a language surface change ("semantics are obvious")
- Write `## Language Design Grounding` citing only Precept-internal docs — general language design requires engaging the broader field (comparable systems, PLT theory)
- Omit `## Architecture Grounding` for a pipeline or catalog change ("catalog discipline is obvious here")
- Leave any Runtime / Tooling / MCP propagation category blank rather than explicitly "None"

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
| Philosophy Alignment absent or superficial | Refuse; prompt author to address each core commitment specifically |
| Language surface change with no Language Design Grounding | Refuse; require both sub-sections |
| Language Design Grounding cites only Precept-internal docs | Refuse; require engagement with broader field (comparable systems or PLT) |
| Pipeline/API/catalog change with no Architecture Grounding | Refuse; require layer placement + propagation + breaking change assessment |
| Architecture Grounding propagation category left blank | Refuse; require explicit "None" or impact description per category |
