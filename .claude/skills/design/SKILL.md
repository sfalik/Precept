---
name: design
description: Stage 2 of the engineering lifecycle — lock a design with required structure (four-leg rationale per decision, acceptance criteria, doc-update enumeration). Triggers on — design, lock a design, propose, spec out, specification, "let's design X", design doc, formalize this approach. Takes a topic (and optional research source) and produces a locked design doc in `docs/Working/`. Refuses to mark designs "locked" without four-leg rationale on every decision.
---

# Precept Design Lock

Stage 2 of the engineering lifecycle. Produces a locked design doc in `docs/Working/` that downstream `/plan` and `/promote` skills can consume reliably.

## When to use

- Idea or research conclusion is ready to commit to a specific approach
- Implementation can't start until the design is locked (alternatives still in play; acceptance unclear)
- User says "let's design X" / "let's spec this out" / "lock this in"
- After `/research` produces conclusions that need to advance to a design

## When NOT to use

- Idea is too early — still need research (use `/research`)
- Already implementing — use `/promote` afterward to canonicalize
- Pure bug-fix or polish work — designs not warranted

## High & Ultra Modes

**Trigger:** `/design high <args>` or `/design ultra <args>` (also recognise "high-rigour"/"ultra" phrasing in the request). **Opt-in only** — these spend many sub-agents and tokens; they are never the default. Reach for them on high-stakes, hard-to-reverse, or easy-to-get-subtly-wrong work where a single pass is not enough.

Both modes run this skill as a multi-agent `Workflow` instead of a single inline pass, and add independent multiplicity + adversarial verification *on top of* this skill's normal discipline — which still fully applies (nothing below replaces the required structure, gates, or checks). Every spawned agent works fluency-first and verifies its claims against source.

- **high** — ≥3 independent design candidates from different framings → an adversarial judge panel (soundness / completeness / buildability) ranks them and names the must-graft ideas → synthesis from the strongest spine grafting the best → one completeness-critic + red-team pass.
- **ultra** — more candidates, **independent re-derivation of the load-bearing decisions** (convergence is the evidence), and the critic + red-team **loop-until-dry**, with a final red-team before lock.

---

## Pre-design gate (Non-Negotiable)

Before writing any section of a design that introduces or modifies language surface (keyword, type, operator, modifier, construct, expression form, syntax), the **CLAUDE.md Pre-Design Owner Consultation gate** must be satisfied. Specifically:

- The conversation that led to this design must contain a record of surfacing the proposal to the owner — what was checked, what was found in the canonical-doc area, what the owner authorized.
- If the proposal touches an area where the canonical doc has a locked prior decision (`## Alternatives rejected`, locked Decision blocks, explicit "no X" statements), Tier 3 consultation evidence is required: the prior locked decision was quoted verbatim, the conflict was surfaced honestly, and the owner explicitly authorized the override / extension / closure path.

**Refuse to lock a design that lacks consultation evidence in the conversation history.** A design that contradicts a canonical-doc locked decision with no upstream owner-authorization conversation is a Tier 3 violation regardless of how well its four-leg rationale is written. The four-leg discipline cannot retroactively authorize an override the owner never authorized.

The skill produces the design; the owner authorizes the *what to design*. Confusing the two reintroduces the failure mode the gate exists to prevent.

## Before you start

Read these before writing any section of the design. They ground every decision; reading them after is too late.

**Always — these three first:**
- `docs/philosophy.md` — Precept's core commitments. Every section of the design is evaluated against these.
- `docs/README.md` — the doc landscape and navigation gateway. Know what exists before deciding what to read.
- `docs/language/README.md` — the language: spec, canonical types, grammar. Precept's design decisions are language decisions; this is the primary substance. The docs here are what a design is measured against — **not** the catalogs, which are incomplete (see `CLAUDE.md § Catalog System`). Catalog-before-code still applies to anything the design ships; a catalog is never evidence that something is complete.

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
status: <Draft | Semantics-Stated | Externally-Grounded | Locked YYYY-MM-DD>
phase-target: <Phase N from current readiness plan, or 'TBD'>
comparable-systems-research-status: <strong | partial | not-applicable — <one-line justification>>
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

The row labels below are the spec's own names for the eleven principles. If they ever disagree with `§ 0.1`, the spec is right — re-read it and correct this table.

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention, not detection | | | | |
| 2. One file, complete rules | | | | |
| 3. Deterministic semantics | | | | |
| 4. Full inspectability | | | | |
| 5. Keyword-anchored readability | | | | |
| 6. Explicit domain meaning over primitive convenience | | | | |
| 7. Compile-time-first static checking | | | | |
| 8. Approximation honesty | | | | |
| 9. Mandatory rationale (`because`) | | | | |
| 10. Totality (every expression evaluates to a result) | | | | |
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

Precept's primary author is the **domain expert**, not the developer (see `docs/philosophy.md § Who authors a precept` and `docs/language/precept-language-spec.md § 0.8 Authoring Audience`). Language surface decisions must serve that reader. Provide:

**Worked example.** A 5-10 line `.precept` snippet a domain expert would actually write using this feature. Plausible domain (financial, lifecycle, regulatory, scheduling, etc.), not a synthetic compiler-test fragment. Show the feature in its intended context, not in isolation.

**Error message.** Pick one specific misuse a domain expert is plausibly going to commit. Write the diagnostic message exactly as it would appear (PRE-code, audience-targeted wording, recovery hint if applicable). Explain in one sentence why the wording serves the domain-expert reader rather than the developer.

**10-minute teaching path.** What does the domain expert need to read to use this feature? Ordered list of 2-5 docs / sections / sample files. The path must be ≤10 minutes for a competent domain expert; if it isn't, the feature is too complex for the surface and should be reconsidered.

**Reviewer obligation.** A missing Audience and Teachability section on a language-surface change is a BLOCKER. A worked example that's a compiler-test fragment rather than a plausible domain scenario is a CONCERN. An error message that uses compiler-internal vocabulary is a CONCERN.

## Legibility — plain language and worked samples (EVERY doc, not only language-surface ones)

The doc is read later by the owner or a domain expert to make a decision — write for *that reader*, who does not have the author's context, not for yourself. This applies to **every** doc the lifecycle produces, including analysis, feasibility, and roadmap docs that touch no language surface. (Derived from what made the 2026-07 proof-engine docs readable; the failure mode it prevents is the "decoder-ring" doc dense with coined terms.)

- **No coined term without a plain definition on first use.** Prefer plain prose. Where a technical term is genuinely load-bearing (`weakest precondition`, `Farkas certificate`, `octagon`), name it once and gloss it in plain words in the same breath — *"variable elimination (combine two inequalities to cancel a shared term)"* — then use the plain phrasing. A `## Glossary` / "How to read this document" block is fine for a necessarily-dense doc, but a glossary is **not** a licence to keep the body jargon-dense.
- **Spell out internal labels.** No bare `Q7`, `R4`, `§1b`, `D-3`, `S1/S2`, `C3-F2` treated as self-explanatory — say what each means in plain words, with at most a light parenthetical pointer. These labels rot and mean nothing to a later reader.
- **Every behavioral claim gets a picture.** For any claim about what the system accepts, rejects, or computes, show a concrete `.precept` example — usually the case that fails **and** the author's fix. Ground it in real `samples/` conventions, syntax-validate it (use the precept MCP as a *syntax* oracle — never the current proof verdict as design authority), and label proposed-design vs. current behavior with a `(today: …)` note where they differ.
- **Expansion, not dumbing-down.** Plain language adds clarity; it never drops rigor, `path:line` citations, or the four-leg rationale. If a passage cannot be said plainly, that usually means the idea is not yet clear — not that jargon is required.

**Reviewer obligation.** Coined jargon used without a plain definition, a bare internal label, or a behavioral claim with no worked sample is a CONCERN; a doc that is systematically jargon-dense (a decoder-ring read) is a BLOCKER.

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

**Soundness preservation claim.** Name the specific principles from `docs/language/precept-language-spec.md § 0.1` that this construct could threaten (most often Principles 7, 10, 11 — compile-time-first static checking, totality, static completeness). For each, state in one sentence why the principle continues to hold after this construct ships. Example: "Principle 11 holds because the new construct produces no expression form whose evaluation is undefined; the proof engine discharges divisor safety and bounds before any runtime path is reachable."

**Reviewer obligation.** A design touching evaluation, proof obligations, or typing without a Semantic Rules section is a BLOCKER. Prose descriptions of behavior without reduction/typing rule notation are CONCERNs for non-trivial cases.

## Architecture Grounding

[Required when the design touches catalog structure, pipeline stage boundaries, public API contracts, or cross-component interfaces. Omit with an explicit one-line note for designs that don't touch these surfaces.]

### Precept-internal placement

**Layer placement:**
Which layer does this behavior belong in — catalog metadata, pipeline stage, public API, tooling derivation? Why does it belong there and not in an adjacent layer? If behavior is being placed in pipeline code rather than catalog metadata, explain the structural limitation that requires it.

**Cross-component propagation:**
How does this change propagate across component boundaries? State the impact for each, or explicitly note "None":
- Runtime (parser, type checker, evaluator, diagnostics):
- Tooling (syntax highlighting, completions, hover, semantic tokens):
- MCP (vocabulary, DTOs, tool output):

**Breaking changes:**
Does this change any public contract — API surface, diagnostic codes, catalog member names that flow to grammar/completions/MCP vocabulary? If yes, state explicitly.

### External architectural precedent

**Mandatory for non-trivial architectural changes.** Cite at least one comparable system's solution to the architectural problem this design touches, with excerpt, and explain Precept's divergence. Internal consistency is necessary but not sufficient — the project's own `compiler-and-runtime-design.md § 2` explicitly grounds the catalog-driven choice in CEL/OPA/CUE comparisons; locked designs must hold themselves to the same comparative standard.

Comparators to consider (not exhaustive):
- **Roslyn** — descriptor-based diagnostics, language-version axes, analyzer SDK separation
- **TypeScript** — incremental type-checking model, structural typing
- **CEL** — type registry, fluent expression evaluation, embedding model
- **OPA** — compiler vs. evaluator boundary, partial evaluation
- **CUE** — lattice-based evaluation, constraint propagation, schema/data unification
- **Dhall** — total functional design, normalization-based type checking
- **Rust** — trait system, macro hygiene, query-based compilation
- **GHC** — desugaring pass, Core IR, type-class resolution
- **MLIR / LLVM** — pluggable dialects, lowering strategy

Pick the comparator most relevant to the design's architectural problem. Cite a specific section or quote of the comparator's docs/spec/source. State what Precept takes and what Precept deliberately diverges from, and why.

Reviewer treats a missing external comparator on a non-trivial architectural change as a CONCERN. "No precedent — novel architectural choice" is acceptable but requires explicit acknowledgment and a paragraph defending why the novelty is warranted.

## Inventory of what will be built
File-level detail: catalog entries, type/record shapes, file paths, test stubs.
This is enumerable content — explicit and specific. Pointer-philosophy
does NOT apply here (this is the spec, not the canonical doc).

## Decisions

Each decision is self-classified by stakes; the required-leg set scales with stakes.

### Stakes classification

| Stakes | Definition | Examples |
|---|---|---|
| **exploratory** | Decision is included for discussion / future resolution; author is not ready to lock. Skips the per-stakes leg requirements but must declare what's keeping it open. Cannot survive into `Locked` status — must resolve or be removed before the doc locks. | Three-way syntax bake-off awaiting prototype data; placement question depending on Phase N work; tradeoff awaiting usability test |
| **low** | Recoverable choice; reversal touches <5 docs/samples; no public-surface lock-in | Severity choice on a new diagnostic, RecoverySteps wording, NIT-level naming |
| **medium** | Choice that affects more than one consumer but is reversible with bounded effort | New modifier semantics, new accessor, new diagnostic code |
| **high** | Touches public-surface contracts (catalog member names, diagnostic codes, MCP vocabulary, keyword choices); reversal is costly | New keyword, new construct, new type, new operator, public API addition |
| **irreversible** | Once shipped to external authors, reversal is effectively infinite cost | Keyword retirement, public-API breaking change, semantic change to existing operator, catalog enum renumbering |

State the stakes explicitly: `**Stakes**: exploratory | low | medium | high | irreversible`. The reviewer flags missing or implausible stakes classification. **Exploratory is a draft-mode marker** — it lets authors sketch decisions they don't yet know enough to lock, without forcing premature four-leg work. A design can carry mixed exploratory + locked decisions in `Draft` / `Semantics-Stated` / `Externally-Grounded` status; lock-time refuses any exploratory decisions remaining.

### Required legs by stakes

| Leg | exploratory | low | medium | high | irreversible |
|---|---|---|---|---|---|
| Rationale | — | ✓ | ✓ | ✓ | ✓ |
| Tradeoff accepted | — | ✓ | ✓ | ✓ | ✓ |
| Alternatives considered | — | — | ✓ | ✓ | ✓ |
| Precedent | — | — | ✓ | ✓ | ✓ |
| Sources consulted (with excerpt) | — | — | ✓ | ✓ | ✓ |
| Counter-evidence | — | — | — | ✓ | ✓ |
| Reversibility | — | — | — | ✓ | ✓ |
| Blast radius | — | — | — | ✓ | ✓ |
| Falsifiers (in companion section) | — | — | — | — | ✓ |
| 24-hour cooling-off before Locked | — | — | — | — | ✓ |
| Exploratory-because + Working-hypothesis + Decision-needed-before | ✓ | — | — | — | — |
| Survives into Locked status | ✗ | ✓ | ✓ | ✓ | ✓ |

### Decision template

```markdown
### Decision N: <one-line decision>

**Stakes**: <exploratory | low | medium | high | irreversible>

- **Rationale**: why this choice
- **Tradeoff accepted**: the known downside being taken on
- **Alternatives considered**: each alternative + why it was rejected
  [required for medium+]
- **Precedent**: research / prior art / existing pattern that grounds the choice
  (or explicit "no precedent — novel choice, accepting risk")
  [required for medium+]
- **Sources consulted for this decision**: one or more source identifiers
  with a short verbatim or near-verbatim excerpt that proves the source was
  read (e.g., `path/to/file.cs:L1-L20 — "<excerpt>"`, or `docs/foo.md § N —
  "<excerpt>"`).
  [required for medium+]
- **Strongest counter-evidence**: the source (internal or external) that most
  plausibly argues against this decision, with excerpt, and a one-sentence
  response. Honest "no counter-evidence found after looking" is acceptable
  but must say where the author looked.
  [required for high+]
- **Reversibility**: `Easy` | `Hard` | `Effectively-irreversible-post-ship`,
  with one-sentence justification.
  [required for high+]
- **Blast radius**: catalogs touched, docs touched, samples touched, external
  consumers affected.
  [required for high+]
```

### Decision template — exploratory variant

```markdown
### Decision N: <one-line decision — TBD>

**Stakes**: exploratory

- **Exploratory because**: <what's keeping this open — depends on Phase N
  prototype data, waiting on usability test, blocked on owner judgment, etc.>
- **Working hypothesis**: <current leaning, NOT committed; may flip on new
  information>
- **Decision needed before**: <the latest point this must resolve — Phase N
  exit, before construct ships, before tests reference it>
- **Open questions**: <enumerated unknowns that would let this decision lock>
```

The exploratory variant skips Rationale, Tradeoff, Alternatives, Precedent, Sources, Counter-evidence, Reversibility, Blast-radius. The author is honestly saying "I don't know enough yet." When information arrives, the decision is re-classified to `low | medium | high | irreversible` and the appropriate legs are filled.

**Cannot survive into Locked status.** A design moving to `Locked YYYY-MM-DD` must either (a) resolve every exploratory decision into a full-leg one, or (b) remove the exploratory decision from the doc entirely. Lock-time refuses any `Stakes: exploratory` decisions still present.

### Honest-answer exits

- "No precedent — novel choice, accepting risk" — acceptable answer for the Precedent leg. Novel decisions in language-design space are real; the skill does not invent precedent.
- "No counter-evidence found after looking in X, Y, Z" — acceptable answer for the Counter-evidence leg. Names the search surface so the claim is falsifiable.
- "No sources consulted — pure-policy choice, no external state informed this" — acceptable for the Sources leg when genuinely true.
- "No tradeoff identified — flag for review" — discouraged; tradeoff is usually identifiable with thought. If used, the reviewer treats as a CONCERN that the author hasn't yet found the tradeoff.

### Source-citation discipline

Citations must be reproducible — a future reviewer can open the source and verify the excerpt. Two strengthening rules:

1. **External URL citations** must (a) include the full quoted excerpt verbatim (no paraphrasing or truncation), (b) include the access date, and (c) be preferred only when no in-tree or paper-PDF equivalent exists. For standards documents (RFCs, ISO docs, papers), include a stable identifier (RFC#, DOI, paper title + venue + year). Prefer locally-mirrored copies in `research/references/` over live URLs.

2. **Frontmatter aggregation**: `sources-consulted` in frontmatter must list every source identifier that appears in any decision's `Sources consulted` leg. Mechanical set-membership check at lock time.

### Cooling-off for irreversible decisions

Decisions with `Stakes: irreversible` cannot advance from Draft to Locked in the same session. The doc carries `status: Stage-3-Externally-Grounded` for at least 24 hours before advancing to `Locked`. The cooling-off forces a second pass — reading the design after time away surfaces gaps the original session missed. Falsifiers section (below) is also required.

### Falsifiers (separate section, required for irreversible decisions and external-author-visible changes)

For any design with an `irreversible` decision, or any design that locks behavior visible to external authors (language surface, error messages, diagnostic codes, MCP vocabulary, public-API shape), add a `## Falsifiers` section.

Format: 2-5 specific observations that, if seen post-ship, would force a redesign. Concrete, measurable, decision-changing. Examples:

- "If three or more samples in `samples/` need explicit-cast workarounds to satisfy the new typing rule, the rule is over-strict and should relax to <weaker form>."
- "If `precept_compile` p99 latency exceeds 50ms on the median sample after this construct ships, the parser strategy is wrong and should be reconsidered."
- "If a single domain expert in a usability test cannot author a working example using this feature within 10 minutes, the audience-fit claim is falsified."

Falsifiers are paired with `/audit` for revisit discipline — periodic checks against the falsifier list catch designs that aged badly.

## Acceptance criteria
Test-shaped. "This passes" / "this fails as expected" / "this is documented in Y."
Specific enough that `/plan` can derive Phase exit criteria from them.

## Dependencies
- Upstream: what must be in place first (other locked designs, shipped code, owner decisions)
- Downstream: what this design enables

## Doc-update enumeration
Per the CLAUDE.md routing table, which canonical docs will need updates when this
design ships. Listed upfront so `/plan` can include them as Phase
sub-tasks and `/promote` can verify them at promotion time.

Example:
- `docs/language/precept-language-spec.md` § N — feature definition
- `docs/compiler/<stage>.md` § Design Rationale and Decisions — design lift
- `docs/language/catalog-system.md` § <catalog> — if new catalog entry

## Operational dimensions

[Optional section; required only when the design touches one or more of the
prompts below. Auto-skip categories that don't apply.]

- **Security** (required if the design touches source-text ingestion — lexer,
  parser, MCP tool input): does this expand the attack surface? Does it
  enable resource-exhaustion or pathological-input attacks? State the
  defensive posture.
- **Observability** (required if the design touches runtime evaluation or
  diagnostic surface): when this construct misbehaves at runtime or surfaces
  a diagnostic, how does the operator/author diagnose it? Does it propagate
  to traces, logs, structured outcomes?
- **Evolvability** (required if the design depends on an external standard —
  NodaTime API, ICU, UCUM, ISO 4217, TZDB): what is the migration story when
  the upstream changes? Is the version-pinning strategy stated? What breaks
  if the upstream removes or renames a feature?

## Open questions
Anything unresolved. The skill refuses to mark "Locked" if any open question
remains. Either resolve or move to a separate Wave 0 triage doc.
```

## Staged advancement

A design advances through four states; each state has its own advancement
criteria. Stages exist so process weight matches decision stakes — small
severity choices go Draft → Locked in one session, while keyword retirements
ladder through all four.

| Status | Criteria to advance to next stage |
|---|---|
| **Draft** | Decision text written; stakes classified per decision. May skip directly to Locked for designs where every decision is `Stakes: low` and no language-surface change. |
| **Semantics-Stated** | + Semantic Rules section present (if affected by guard 4) + Decision text + per-decision Rationale and Tradeoff |
| **Externally-Grounded** | + Language Design Grounding (if affected by guard 2) + Architecture Grounding with external precedent (if affected by guard 5) + per-decision Sources consulted (with excerpts) + per-decision Counter-evidence (for high+ stakes) |
| **Locked YYYY-MM-DD** | + Philosophy Alignment matrix filled + Audience and Teachability (if language surface) + Acceptance criteria + Doc-update enumeration + Falsifiers (if external-author-visible) + Reversibility / Blast radius legs (for high+ stakes) + No open questions + **No `Stakes: exploratory` decisions remaining** + **Research-adequacy verified (if any `Stakes: irreversible` decision)** |

Cooling-off requirement: a design with any `Stakes: irreversible` decision must hold `status: Externally-Grounded` for at least 24 hours before advancing to `Locked`. The cooling-off is structural — it forces a second-pass review of the design after time away.

### Research-adequacy gate (irreversible decisions)

Designs with at least one `Stakes: irreversible` decision must clear a research-adequacy check before advancing to `Locked`. The gate has four honest exits, exactly one of which must apply:

- **(a) Research-cited**: the design cites a research file in `research/` that surveyed the relevant comparable systems with verbatim excerpts and meets `/research` Stage-1 quality. The cited file must appear in `sources-consulted` and in at least one per-decision `Sources consulted for this decision:` leg. Frontmatter declares `comparable-systems-research-status: strong`.
- **(b) Inline-survey**: per-decision comparable-systems survey is carried inline — for each external system named in the decision's prose, the leg supplies a verbatim excerpt, an access date, and a stable identifier (file path, RFC#, DOI, paper title + venue + year, or live URL with mirror). The inline survey meets the same discipline as a Stage-1 research artifact; the cumulative legs across decisions cover every comparator named. Frontmatter declares `comparable-systems-research-status: partial`.
- **(c) Not-applicable**: the design genuinely makes no comparable-systems claims. The frontmatter declares `comparable-systems-research-status: not-applicable — <one-line justification>` (e.g., "purely Precept-internal placement decision; no language-surface or architectural-precedent claim"). Reviewer treats this exit as a CONCERN if the design's prose nonetheless names external systems.
- **(d) Novel-verified**: the author looked for comparators, found none that apply to the decision's problem, and is locking the decision as honestly novel. The frontmatter declares `comparable-systems-research-status: novel-verified`. The design must include a `## Novel-verified declaration` section listing (1) **the obvious comparators checked** — at minimum 3, drawn from the reviewer's `Mandatory comparator-checking by topic` table for the relevant surface; (2) **a one-line "doesn't apply because <reason>" for each**; (3) **a one-paragraph defense of why the novelty is warranted** given the gap. The exit is for designs that are genuinely first-of-their-kind, not for designs where the author didn't want to do the survey. Reviewer treats this exit as a CONCERN if (a) any comparator on the reviewer's topic table is missing from the checked-list without justification, or (b) the design's prose nonetheless names a system that DOES solve the decision's problem and the doesn't-apply line is unconvincing.

The gate is enforced by skill-text obligation + reviewer-agent invocation (see `.claude/agents/precept-reviewer.md § Mandatory comparator-checking by topic`). Without docs-lint (Phase 0 deferred), authors who skip the reviewer can ship past the gate; the discipline is author-side + reviewer-side, not build-time.

See `research/INDEX.md` for the cross-corpus topic-to-file map when looking for a citable research file. The reviewer's topic-to-comparator table lists the comparators expected for each topic surface.

**Lightweight path for low-stakes designs.** When every decision is `Stakes: low` and no language-surface or pipeline/catalog/API change is involved, the skill compresses Draft → Locked in one session. Required content shrinks accordingly (per the legs-by-stakes table). The skill flags any decision that looks high-stakes but is marked `low` — that's a stakes-classification error, not a fast-path.

## Behavioral guards

The skill enforces:

1. **No "Locked" status without Philosophy Alignment.** The section must be present and the principle-coverage matrix must be filled — every one of the eleven principles in `precept-language-spec.md § 0.1` must have a row with no blank cells. Rows marked "N/A" require a one-line justification. "This design is consistent with Precept's philosophy" with no matrix is refused. The companion-commitments paragraph (Stateless-first-class, Domain-expert-primary-author) must be present unless trivially N/A.

2. **Language surface changes require Language Design Grounding.** If the design introduces or modifies any token, keyword, construct, modifier, type, operator, accessor, or expression form: the section must be present and both sub-sections must be substantive. The "general language design" sub-section must engage with the broader field — comparable languages, PLT theory, or explicit acknowledgment of a gap in `research/language/`. Citing only Precept-internal docs is refused.

3. **Language surface changes require Audience and Teachability.** Same trigger as guard 2. The worked example must be plausible-domain (not a compiler-test fragment), the error message must use domain-targeted vocabulary, and the 10-minute teaching path must be enumerated. Missing this section on a language-surface change is refused.

4. **Designs touching evaluation, proof, or typing require Semantic Rules.** If the design introduces a new expression form, modifies typing behavior, adds a proof obligation, or changes constraint semantics: the Semantic Rules section must be present with reduction/typing-rule sketches and a soundness-preservation claim naming the specific principles preserved. Prose descriptions without notation are refused for non-trivial cases.

5. **Pipeline/API/catalog changes require Architecture Grounding.** If the design touches catalog structure, pipeline stage boundaries, public API contracts, or cross-component interfaces: all sub-sections must be present. Precept-internal placement: layer placement + cross-component propagation (no blanks; explicit "None" required per category) + breaking changes. External architectural precedent: at least one comparator's solution cited with excerpt for non-trivial architectural changes. "No precedent — novel architectural choice" is acceptable but requires explicit acknowledgment.

6. **Every decision carries stakes-appropriate legs.** Each decision declares `Stakes: exploratory | low | medium | high | irreversible`. Required legs scale with stakes (see § Decisions § Required legs by stakes). High-stakes decisions require Counter-evidence, Reversibility, and Blast-radius legs. Irreversible decisions additionally require a `## Falsifiers` section and a 24-hour cooling-off period before advancing to `Locked`. **Exploratory decisions** skip the per-stakes leg requirements but must declare Exploratory-because + Working-hypothesis + Decision-needed-before; they cannot survive into `Locked` status (the doc either resolves or removes them before locking). Missing stakes classification or skipped legs are refused.

7. **External-author-visible changes require Falsifiers.** If the design locks behavior visible to external authors (language surface, error messages, diagnostic codes, MCP vocabulary, public-API shape), a `## Falsifiers` section is required with 2-5 specific observations that would force redesign post-ship. Missing Falsifiers on an external-author-visible change is refused.

8. **No "Locked" status with open questions.** Forces resolution before locking. If questions are too big to resolve in the session, the skill suggests creating a separate Wave 0 decision-triage doc.

9. **Acceptance criteria must be test-shaped.** The skill refuses vague criteria like "works correctly." Prompts for specific testable conditions.

10. **Doc-update enumeration must be present.** The skill consults the CLAUDE.md routing table for the file paths the design touches and pre-populates the doc-update section. Author can edit or expand.

11. **Every decision must cite the sources that informed it — with proof-of-reading.** A citation is `<source identifier> — <short verbatim excerpt>`. The excerpt is the forcing function: it can't be fabricated without opening the source. Citations are listed per-decision (under the "Sources consulted for this decision" leg) AND aggregated in the frontmatter `sources-consulted` field. The skill checks two things at lock time:
   - **Decision text vs. citations.** If a decision's prose names external state (a file path, a code identifier, a doc section, a tool, a sample, a bug ID, an enum, an interface, a precept feature, a research conclusion, another design doc) but the decision's `Sources consulted` leg is empty, refuse to lock. The author either cites what they consulted or explicitly declares "no sources consulted — pure-policy choice."
   - **Frontmatter aggregation.** `sources-consulted` in frontmatter must list every source identifier that appears in any decision's `Sources consulted` leg. The check is mechanical set membership — every per-decision citation also appears at the top of the doc.
   "Source" is an open category — anything with a permanent address that informed the design qualifies. The skill does NOT hardcode which source types are acceptable; the discipline is "cite what you read, regardless of what kind of thing it is."

12. **External URL citations must be reproducible.** When a citation is to an external URL (not an in-tree file): the excerpt must be the full verbatim quote (no paraphrasing or truncation); the citation must include the access date; for standards docs (RFCs, ISO docs, papers), a stable identifier (RFC#, DOI, title+venue+year) is required; mirroring to `research/references/` is strongly preferred over live URLs. Paraphrased URL citations or missing access dates are refused.

13. **Irreversible decisions require cooling-off.** A decision marked `Stakes: irreversible` cannot advance from Draft to Locked in the same session. The doc carries `status: Externally-Grounded` for at least 24 hours before advancing. The cooling-off is a structural pause: re-reading the design after time away surfaces gaps the original session missed.

14. **Staged advancement criteria apply.** Designs that touch language surface, pipeline/catalog/API, or evaluation/proof/typing must ladder through Draft → Semantics-Stated → Externally-Grounded → Locked, with each stage's advancement criteria satisfied before the status advances. The lightweight path (Draft → Locked in one session) is reserved for designs where every decision is `Stakes: low` and no language-surface change is involved. Skipping stages on a non-lightweight design is refused.

15. **Operational dimensions required when triggered.** If the design touches source-text ingestion (lexer/parser/MCP input), a Security prompt must be addressed. If it touches runtime evaluation or diagnostic surface, an Observability prompt must be addressed. If it depends on an external standard (NodaTime, ICU, UCUM, ISO 4217, TZDB), an Evolvability prompt must be addressed. Skipping a triggered prompt without explicit "N/A — <reason>" is refused.

16. **Research-adequacy gate for irreversible decisions.** A design with any `Stakes: irreversible` decision cannot advance to `Locked` until research-adequacy is verified (see § Staged advancement § Research-adequacy gate). The author must declare exactly one of: `comparable-systems-research-status: strong` (cites a Stage-1 research file in `research/`), `partial` (inline survey per decision meets Stage-1 discipline), `not-applicable` (with one-line justification), or `novel-verified` (with a `## Novel-verified declaration` section listing the obvious comparators checked, a doesn't-apply line for each, and a one-paragraph defense of the novelty). Missing `comparable-systems-research-status` frontmatter on an irreversible-decision design is refused. The gate is cross-checked against the reviewer's `Mandatory comparator-checking by topic` table; topics named in decision prose that lack the expected comparator citations are flagged as a CONCERN; topics absent from the checked-list under `novel-verified` are likewise flagged.

17. **Spec-first verification — a decision the canonical spec already settles is NOT a decision (Non-Negotiable).** Before framing anything as a `## Decision`, grep the canonical spec/design for whether it is already answered: `grep -in "<concept>" docs/language/precept-language-spec.md docs/language/business-domain-types.md docs/language/temporal-type-system.md docs/compiler/proof-engine.md docs/compiler/type-checker.md`. Three outcomes:
    - **Spec already decides it** (locked Decision block, `## Alternatives rejected` entry, explicit "no X"/"X is …" statement, or a proof-contract/obligation item): it is **not a design decision — it is implementation against locked spec.** Cite the section verbatim; do not re-litigate it as an open choice, and do not classify it as `Stakes: <anything>` requiring fresh four-leg. (If the design wants to *override* the locked decision, that is the Tier-3 owner-consultation path per CLAUDE.md, not a silent design decision.)
    - **Spec claims it implemented but it isn't** (verify implemented obligations against actual behavior — `precept_compile` a probe): that is a *bug against locked spec* + doc-drift, not a design decision. Surface it as such.
    - **Spec is genuinely silent** (the grep returns nothing on-point): legitimate design decision — proceed, and note which spec sections were checked.
    Refuse to lock a design that frames as an "open decision" something a cited spec section already settles. The forcing function: each `## Decision`'s `Sources consulted` leg must show the spec section was checked for prior settlement (a quote, or an explicit "grepped X/Y/Z — spec is silent on this"). This is the design-side of read-before-asserting: the owner must never have to ask "is this already in the spec?"

## Composability

- **Input**: human-discipline obligation — if a `research/` file exists for the design's topic, the author must read it before locking, cite it in `sources-consulted`, and copy verbatim excerpts into per-decision `Sources consulted for this decision:` legs for any claim the research grounds. The historical `--from <research-doc>` flag was a rhetorical claim — no implementation; designs that cited it produced no operational difference. Removed in Phase 8 (2026-05-25) along with the empirical finding that no in-tree design had ever used it.
- **Output**: locked design at `docs/Working/<slug>.md` — consumed by `/plan` for phase planning, and later by `/promote` for canonical doc updates.

## Anti-patterns to refuse

- Skip the four-leg structure ("it's obvious")
- Leave acceptance criteria as prose ("the feature works")
- Skip doc-update enumeration ("I'll figure it out later")
- Mark "Locked" with `(?)` markers or `TBD` placeholders in decision rationale
- Mark "Locked" with `Stakes: exploratory` decisions still present (exploratory is a draft-mode marker — resolve or remove before locking)
- Use `Stakes: exploratory` as a way to skip the four-leg for a decision the author IS confident about (the exit is for genuine uncertainty, not for skipping homework)
- Cite a source without an excerpt ("Consulted: `Modifiers.cs`" — bare; no proof of reading). The excerpt is the forcing function. Bare-path citations are refused.
- Make claims about external state with no `Sources consulted` ("The catalog already has 8 of these" — no citation). The skill refuses to lock when prose references external state but the citation leg is empty.
- Write `## Philosophy Alignment` as a single sentence ("this design is consistent with Precept's philosophy") — requires addressing each commitment specifically
- Omit `## Language Design Grounding` for a language surface change ("semantics are obvious")
- Write `## Language Design Grounding` citing only Precept-internal docs — general language design requires engaging the broader field (comparable systems, PLT theory)
- Omit `## Architecture Grounding` for a pipeline or catalog change ("catalog discipline is obvious here")
- Leave any Runtime / Tooling / MCP propagation category blank rather than explicitly "None"
- Coin a shorthand ("the cell", "the seam") and use it throughout without ever defining it in plain words
- Reference internal decision labels (`Q7`, `R4`, `§1b`, `D-3`) as if the reader has the source open
- Make a behavioral claim (what compiles / rejects / is computed) with no concrete, syntax-validated `.precept` sample
- Lean on a glossary to justify keeping the body dense with undefined jargon

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
| Irreversible decision with no `comparable-systems-research-status` frontmatter | Refuse; require one of `strong` / `partial` / `not-applicable — <justification>` / `novel-verified` |
| Decision prose names external systems but no comparator citations | Refuse; require either Stage-1 research citation, inline survey legs, or `novel-verified` declaration with the named systems addressed in the doesn't-apply lines |
| Author claims "novel" but did no comparator check | Offer `novel-verified` exit: pick the obvious 3-5 comparators for this surface (from reviewer's topic table), write a one-line doesn't-apply for each, defend the novelty in a paragraph |
| Author wants to sketch a decision but isn't ready to commit four-leg | Offer `Stakes: exploratory`: decision lives in the doc with Exploratory-because + Working-hypothesis + Decision-needed-before legs; resolves to a real stakes value (or drops out) before lock |
| Author marks a decision exploratory to skip four-leg | Refuse — exploratory is for genuine uncertainty, not for offloading work. If the author knows the rationale, the four-leg is fast to write |
| Design ready to Lock but exploratory decisions still present | Refuse Lock until each exploratory is either resolved into a `low | medium | high | irreversible` decision with appropriate legs, or removed from the doc |
| A `## Decision` frames an open choice the canonical spec already settles | Refuse; grep + cite the settling section. It is implementation against locked spec, not a decision (or a Tier-3 override conversation if the design wants to change the lock) |
| A decision's `Sources consulted` leg shows no check of the canonical spec for prior settlement | Refuse; require a quote of the relevant spec section, or an explicit "grepped X/Y/Z — spec silent on this" |
