---
name: lifecycle-1-research
description: Stage 1 of the engineering lifecycle — research and exploration that feeds /lifecycle-2-design. Conduct technical, cross-domain, or feasibility research that informs Precept's language design, architecture, tooling, or product positioning. Triggers on — research, investigate, survey, compare alternatives, evaluate feasibility, prior art, precedent, landscape, "how do other tools handle X". Use this for any task whose output is a markdown document in `research/` (or a domain-owned research folder), not code. Excludes: brand identity research (use `design/brand/research/`) and UX research (use `design/system/research/`).
---

# Precept Research (Lifecycle Stage 1)

Stage 1 of the engineering lifecycle. Conclusions that lock decisions feed forward to `/lifecycle-2-design`.

Research in this repo informs language design, architecture, tooling, and policy. The discipline matters as much as the findings — evidence-oriented, citation-rich, and never shadow policy.

## Pre-research gate (Non-Negotiable)

If the research topic is grounding a **new language-surface feature** (keyword, type, operator, modifier, construct, expression form, syntax) — not a pure comparator survey or feasibility check on already-authorized scope — the **CLAUDE.md Pre-Design Owner Consultation gate** must be satisfied before the research starts.

Research can ground a decision the owner authorizes (good). Research **cannot** authorize the underlying *what should we build* question by itself; if no conversation has happened with the owner, the research will inherit a `what to design` decision the owner never made.

Surface to the owner first: name the finding/gap, cite the canonical-doc area you'd check, sketch what kind of research would help. Get alignment on whether the research is the right next step. Then proceed.

The exception is **horizon groundwork** — research that the project intentionally produces ahead of downstream decisions. Horizon research is authorized when the owner has said "this area is coming, do the upfront research" — that *is* the consultation. Mark such research with `status: Active` and a horizon-groundwork declaration per § Step 7.

## Step 1: Triage — Has This Been Researched Already?

Before any external investigation, check whether the team has already covered the ground:

- **Technical / cross-domain:** `research/` (subfolders: `language/`, `architecture/`, `philosophy/`, `product/`, `security/`, plus `language/expressiveness/` for comparator studies)
- **Brand:** `design/brand/research/`
- **UX / design system:** `design/system/research/`
- **Raw source captures:** each domain's `references/` folder
- **Critiques of specific artifacts:** each domain's `reviews/` folder

Read `research/README.md` and `research/language/README.md` first — they're the canonical entry points and index the existing work.

If existing research covers the question (in whole or in part), **cite it, don't duplicate it.** Build forward from prior findings; don't re-investigate solved problems.

### Step 1b: Does the canonical spec already DECIDE this? (Non-Negotiable — spec-first)

Before researching any language-surface or proof/typing/semantics question, **grep the canonical docs and read the relevant section** — the answer may already be locked there, making the research moot:

```
grep -in "<concept>" docs/language/precept-language-spec.md docs/language/business-domain-types.md docs/language/temporal-type-system.md docs/compiler/proof-engine.md docs/compiler/type-checker.md
```

If the canonical spec/design **already decides** the question (a locked Decision block, a `## Alternatives rejected` entry, an explicit "no X" / "X is …" statement), the research is **moot — cite the spec and stop.** Do not commission a survey to re-derive a conclusion the spec already locked. Quote the section that settles it.

This is the research-side of the spec-first discipline: a survey that re-litigates a settled spec decision is wasted work that risks contradicting the lock (the `units { }` block failure — research was commissioned for entity-scoped units when `business-domain-types.md § D6` already rejected the construct). **The grep is the first action, not the last.** Honest exit: if the grep genuinely returns nothing on-point, say so (cite the sections checked) and proceed — that's the legitimate research case.

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

Every research file follows this shape. The frontmatter and four sections (Methodology, Findings, Threats to Validity, Sources) are **required**; the others apply to research that proposes conclusions.

```markdown
---
status: Active | Promoted to: <canonical-link> | Cited | Stale | Superseded by: <link> | Archived
authored: YYYY-MM-DD
author: <name or role>
topic: <one-line topic — used for cross-folder INDEX discoverability>
external-engagement: <strong | partial | purely-internal — see Behavioral Guards § G1>
---

# <Title>

> One-sentence framing of what this research investigates and why it matters.

## Background

What prompted this investigation. Link to the proposal issue, decision, or
upstream research that motivated it.

## Methodology

How the investigation was conducted. **Required content:**

- **Research question** — the specific question being answered.
- **Search strategy** — what was searched (catalogs, source code, web, academic
  databases, library docs). Name the venues.
- **Inclusion / exclusion criteria** — what counted as relevant, what was
  deliberately excluded.
- **Source-grade declaration** — Primary / Secondary / Tertiary mix (see
  § Source Grading below).
- **Time bounds** — when the investigation ran. Source-fetch dates matter
  for evolving external state (vendor docs, library behavior).

A research file without a Methodology section is, structurally, advocacy
for a position the author reached. Methodology is the discipline that
distinguishes survey from opinion.

## Findings

The substance. Citation-rich. Each load-bearing claim grounded in a source
with **verbatim excerpt** (see § Citation Discipline below).

For comparator surveys (the most common shape), use a table format:
each row is a comparator system; each column is a property; each cell
links to or quotes a specific source.

## Threats to Validity

**Required.** What might be wrong with this research's conclusions?

- **Sources that couldn't be fetched** — if a primary source returned 404 /
  429 / paywall and was "supplemented from knowledge," declare it here.
  Knowledge-from-training-data is Tertiary; if a load-bearing claim rests
  on it, that's a threat.
- **Selection bias** — comparators chosen because the author already knew
  them, vs comprehensive enumeration of the comparator space.
- **Recency** — the source material may have changed since fetch. State the
  fetch dates explicitly.
- **Domain mismatch** — the comparators surveyed solve adjacent problems,
  not the exact problem.
- **Other knowable gaps** — anything else a careful reader would flag as
  weakening the conclusion.

Honest exit: `## Threats to Validity` with the single sentence "No threats
identified — flag for review" is acceptable when true. The section being
absent is not acceptable.

## Implications for Precept

How the findings bear on Precept's design, architecture, or positioning.
Make the bridge explicit — research that doesn't connect back is just
trivia.

## Conclusions

For each conclusion you're proposing the team adopt, include all four:

- **Rationale** — why this conclusion, not just what it is
- **Alternatives considered and rejected** — with reasons for rejection
- **Precedent** — the evidence from above that grounds it
- **Tradeoff accepted** — the known downside being taken on

A conclusion that states WHAT without WHY is incomplete. Flag it as draft.

## What would change this conclusion

**Required when the research proposes conclusions.** 2-3 observations or
evidence-shapes that, if encountered, would force re-investigation. Parallel
to the `## Falsifiers` section in `lifecycle-2-design` — Hillel Wayne's
"what would change my mind" school.

Examples:

- "If three or more comparator systems we initially excluded turn out to do
  X, the conclusion 'no comparable system does X' is wrong."
- "If a benchmark in environment Y shows the proposed approach is 10x slower
  than the alternative, the feasibility claim falls."
- "If domain experts in usability testing can't successfully use the proposed
  surface within 10 minutes, the readability claim is wrong."

Honest exit: "purely exploratory; no conclusions proposed" is acceptable
when the research is exploratory; in that case this section may be omitted.

## Open Questions

What this research did not resolve. What would need further investigation,
and roughly what shape that would take.

## Sources

Backstop bibliography. Every external source cited in Findings appears here
with:

- **Title**
- **Author / org**
- **Stable identifier** (DOI / RFC# / ISO# / ISBN / venue+year / library
  release version + URL)
- **Source grade** (Primary / Secondary / Tertiary — see § Source Grading)
- **Access date** for any URL-based source
- **Mirrored to** path if the source has been snapshotted to
  `research/references/<topic>/` (load-bearing external sources should be
  mirrored to defend against URL rot)
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

- **Add the new file to `research/INDEX.md`** in the appropriate topic section (this is the mandatory final step of every research-completion change-set). If a new topic doesn't exist, create a new `## Topic name` section in the appropriate position. The INDEX is the cross-corpus discovery surface; without an entry there, future agents won't find the file.
- If the research informs a GitHub proposal issue, **link from the issue body** to the research file.
- If `research/language/README.md` (or another subfolder README) maintains an issue map, **update it** to connect the new research to its consuming proposal.
- If the research promotes a conclusion that becomes a locked decision, that decision lives in a spec or design doc — **not** in the research file. The research is the evidence; the decision is the policy.

## Step 7: Promote-or-Cite Rule

Research does not become policy by sitting on disk. There are exactly **four** valid endpoint states, declared in the frontmatter `status` field:

1. **`Promoted to: <canonical-link>`** — the conclusions are adopted, and the decision is written into a spec, design doc, or `docs/philosophy.md` change (with owner approval for philosophy). The research file remains as evidence; the canonical doc is the policy.
2. **`Cited`** — the research is referenced from a proposal issue, decision document, or another research file. It's load-bearing for something concrete.
3. **`Active`** with explicit horizon-groundwork declaration — research the project intentionally produces before downstream decisions need it. Use sparingly; overuse re-creates the shadow-policy problem under a different label. Document the intended consumer in the file (e.g., "feeds Phase N proposal for X").
4. **`Archived`** — moved to `research/archive/` with a `archived: YYYY-MM-DD — <reason>` line in frontmatter explaining why it didn't ship.

A `Stale` or `Superseded by: <link>` status is also valid for research that's been overtaken by later work; the file remains in place as historical context.

A research file with `status: Active` and no horizon-groundwork declaration that lacks inbound citations from `docs/` or another `research/` file is **shadow policy** by the skill's definition. The Promote-or-Cite expectation is the file-completion gate the author runs before declaring the research complete.

## Source Grading

Different evidence has different weight. The skill enforces three grades:

| Grade | Definition | Examples |
|---|---|---|
| **Primary** | Standards, peer-reviewed papers, authoritative library docs with public versioning, official language specifications, RFCs, ISO standards | TC39 spec, ECMA-262, RFC 3339, ISO 4217, POPL paper, Joda-Money Javadoc, Rust Reference, TypeScript Handbook, NodaTime API docs |
| **Secondary** | Vendor documentation, community implementations, prominent blog posts by named authors with subject-matter expertise | Stripe API docs, AWS API reference, Hillel Wayne blog posts, named-author technical articles |
| **Tertiary** | Forum posts, knowledge claims supplemented when source is unfetchable, casual commentary | Stack Overflow answers, GitHub issue comments, training-data knowledge with no canonical source, anonymous blog posts |

**Discipline:**

- Designs with **only Tertiary sources** on a load-bearing decision are CONCERNs in `precept-reviewer` review.
- The Sources section must declare the grade per source, OR the grade must be inferable from the identifier shape (RFC# → Primary, vendor URL → Secondary, training-data → Tertiary).
- Mixing grades is normal and acceptable; the discipline is **honesty about which is which**.
- Knowledge-from-training-data on a load-bearing claim is Tertiary and must be flagged in `## Threats to Validity`.

## Citation Discipline

Every external citation in Findings or Conclusions must carry:

1. **Verbatim excerpt** — a short direct quote from the source (no paraphrasing, no truncation that changes meaning). The excerpt is the forcing function: it cannot be fabricated without opening the source.
2. **Stable identifier** — for standards/academic: RFC#, DOI, ISO#, ISBN, or paper title + venue + year. For library/vendor docs: the documented version or release tag plus a URL.
3. **Access date** — when the URL was fetched. URLs rot; access dates make claims falsifiable later.
4. **Source grade** — declared inline or in the Sources section (see § Source Grading above).
5. **Mirrored path** — for load-bearing external sources, the snapshot at `research/references/<topic>/<source-name>.md`. The mirror defends against URL rot and lets the reviewer verify the excerpt without re-fetching.

**Unfetchable sources.** If a primary source returns 404 / 429 / paywall at fetch time, two options:

- **Use a different source.** A library's Javadoc disagrees with a vendor blog? The Javadoc wins; cite it instead.
- **Mark the claim as Tertiary and flag in Threats to Validity.** Document the failed fetch, name what was substituted (knowledge / mirror / different source), and surface that the load-bearing claim now rests on weaker evidence. Future investigation can upgrade the source.

**Citation format example:**

```markdown
**JSR-354 deliberate decoupling** [Primary; access date 2026-04-13;
mirrored to `research/references/jsr-354/user-guide-§3.2.md`]:

> "JSR-354 provides MonetaryAmount as an interface, deliberately allowing
> implementations to vary in precision and rounding model. The reference
> implementation Moneta is capable of supporting arbitrary precision
> and scale."
> — JSR-354 User Guide § 3.2 Precision and Rounding
> (https://javamoney.github.io/ri-1.4/, accessed 2026-04-13)
```

Bare citations without excerpt (e.g., "see JSR-354" or "Stripe docs confirm this") are insufficient — the verbatim excerpt is the discipline.

## Behavioral Guards

The skill enforces these as **refusal gates**. The skill refuses to mark a research file complete (status `Cited` / `Promoted` / `Active`) if any guard fails. Honest "no" answers are acceptable but must be declared, not skipped.

1. **No status field is refused.** Every research file's frontmatter must carry a `status:` field. Missing status is a draft, not a complete artifact.

2. **External-engagement declaration is required.** Research must declare `external-engagement: strong | partial | purely-internal` in frontmatter. `purely-internal` is acceptable when the research is genuinely about Precept-internal state (e.g., catalog enumeration), but must be declared explicitly. Refuse to mark complete if the question has external answers and `external-engagement: purely-internal` is declared — that's dishonest framing.

3. **Verbatim excerpts on load-bearing claims.** Each claim about an external system that grounds a conclusion must carry a verbatim excerpt from the source per § Citation Discipline. Bare prose claims ("Stripe handles this by X") with no excerpt are refused.

4. **`## Methodology` section is required.** Refuse research without an explicit Methodology section naming research question, search strategy, inclusion/exclusion criteria, source-grade mix, and time bounds. A single-paragraph methodology is acceptable; absence is not.

5. **`## Threats to Validity` section is required.** Refuse research without an explicit Threats to Validity section. Lists the strongest reasons this conclusion might be wrong (unfetchable sources, selection bias, recency, domain mismatch). Honest exit: "No threats identified — flag for review" is acceptable when true.

6. **`## What would change this conclusion` is required for research that proposes conclusions.** Refuse research that promotes a conclusion without 2-3 falsifying observations. Exploratory research that proposes no conclusion may omit this section if explicitly marked exploratory.

7. **Source-grading must be honest.** Refuse research that grades training-data knowledge as Primary, or grades vendor blog posts as Primary. The grading is a discipline check; mis-grading is the same failure mode as paraphrasing a quote.

8. **Promote-or-Cite at file completion + INDEX update.** Before declaring the research complete, the author verifies one of: inbound citation from `docs/` exists or is added in the same change-set, inbound citation from another `research/` file exists or is added, `status: Active` with horizon-groundwork declaration is documented, or the file moves to `research/archive/`. New shadow-policy files are refused. **AND**: the new file must appear in `research/INDEX.md` under its topic. Missing INDEX entry is refused even if the file is otherwise complete.

9. **Sub-folder taxonomy is enforced.** Research filed in the wrong folder per the table in § Step 2 is refused. Use the folder for the topic, not the folder convenient to the author's session.

10. **Mirror load-bearing external sources.** External URLs that ground a load-bearing claim should be snapshotted to `research/references/<topic>/`. Live-URL-only citations for load-bearing decisions are CONCERNs in `precept-reviewer` review.

**Honest limitation**: without docs-lint (Phase 0 out of scope), these guards depend on (a) skill text gating author behavior, (b) `precept-reviewer` post-hoc verification when invoked, (c) author discipline. The guards are obligations the skill spells out; the reviewer enforces them mechanically via grep when reviewing research-doc PRs.

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
