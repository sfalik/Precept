---
status: Draft plan — 2026-05-25
sources:
  - docs/Working/doc-corpus-evaluation-opus.md
  - docs/Working/skills-rigor-evaluation-opus.md
purpose: Execute the combined improvements surfaced by both Opus evaluations — restore corpus trust, scaffold AI-agent discovery, operationalize philosophy alignment, strengthen design and review rigor, reduce structural drift
---

# Doc Corpus + Skills Rigor Improvement Plan

## Sources

This plan executes findings from two independent Opus evaluations:

- [`doc-corpus-evaluation-opus.md`](doc-corpus-evaluation-opus.md) — 18 findings on the doc corpus + skills system as an AI-agent knowledge architecture (referenced below as **DC-Fn**)
- [`skills-rigor-evaluation-opus.md`](skills-rigor-evaluation-opus.md) — 15 findings on the design-lock skill and reviewer agent as process artifacts (referenced below as **SR-Fn**)

Total findings integrated: 33. Many overlap or reinforce each other; the phasing below combines them where they share an intervention.

## Phase summary

| Phase | Goal | Findings addressed | Effort | Status |
|---|---|---|---|---|
| 0 | Foundational tooling — mechanical enforcement layer | DC-F3, DC-F4, SR-F1, SR-F11 | 1-2 days | **Deferred** — CI infrastructure not yet in place; revisit when CI exists |
| 1 | Truth-up — restore corpus trust | DC-F1, DC-F2, DC-F3, DC-F4 | 1 day | Planned |
| 2 | Discovery scaffolding — fresh-agent onboarding | DC-F9, DC-F10, DC-F13, DC-F18 | 2-3 days | Planned |
| 3 | Philosophy operationalization — top-to-bottom alignment | DC-F7, DC-F8, SR-F2, SR-F4, SR-F8 | 2 days | Planned |
| 4 | Decision and citation rigor — strengthen the per-decision discipline | SR-F3, SR-F6, SR-F7, SR-F9, SR-F10, SR-F15 | 2-3 days | Planned |
| 5 | Process and orchestration — staging, independence, skill chain | SR-F5, SR-F12, SR-F13, SR-F14, DC-F12 | 2-3 days | Planned |
| 6 | Structural cleanup — context cost and drift surface | DC-F5, DC-F6, DC-F15, DC-F16 | 2-3 days | Planned |
| 7 | Polish — dedup and consolidation | DC-F14, DC-F17 | 1-2 days | Planned |

**Total estimate:** ~12-15 days focused work (Phase 0 deferred until CI exists).
**Minimum viable improvement:** Phase 1 alone (1 day) closes the most damaging drift.
**Highest-leverage subset:** Phases 1 + 3 (3-4 days) restores corpus trust *and* operationalizes the philosophy commitments that most distinguish Precept.

**Sequencing notes:**
- Phase 0 deferred — early-days project; no CI process yet. Discipline is manual until CI lands; revisit Phase 0 when CI infrastructure exists. The SR-F1 / SR-F11 enforcement gap is acknowledged and accepted as a known limitation during this period.
- Phase 3 combines doc-corpus and skills-rigor findings into one philosophy-alignment intervention — both evaluations converge on the same fix.
- Phases 4, 5, 6, 7 are largely parallelizable after Phase 1.
- Phase 6 (structural restructure) is highest risk; manual cross-reference audit required without lint.
- Exit-criteria checks that reference `docs-lint` are performed manually for now; mark satisfied via grep/visual verification.

---

## Phase 0: Foundational tooling

**Goal:** Build the mechanical enforcement layer so every subsequent fix is durable. The catalog-driven discipline that built Precept's code is applied to Precept's docs and process.

**Findings addressed:**
- **SR-F1** — "Refuses to lock" forcing functions don't bind (in-tree evidence in `field-never-set-diagnostic.md`)
- **SR-F11** — Wrong encoding; markdown-rules-in-LLM-context can't enforce
- **DC-F3** — Dangling references mechanically detectable
- **DC-F4** — Out-of-taxonomy status fields mechanically detectable

### Tasks

1. **Build `docs-lint` CI check.** Single tool (shell script or .NET analyzer in `tools/Precept.DocsLint/`) wired to CI that fails on:
   - Dangling `docs/...` and `*.md` references in any markdown file
   - Status field values outside the canonical taxonomy (per Phase 1 extension)
   - `status: Locked` frontmatter in `docs/Working/*.md` without required sections (Philosophy Alignment, Language Design Grounding if language-surface, Architecture Grounding if pipeline/catalog/API, sources-consulted frontmatter)
   - Per-decision four-leg presence (Rationale, Alternatives, Precedent, Tradeoff) plus citation excerpts in any `status: Locked` doc
   - `sources-consulted` frontmatter ⊇ per-decision `Sources consulted` legs (mechanical set membership check from the existing skill rule)
2. **Wire as pre-commit hook + CI gate.** Pre-commit catches local drift; CI gates merge.
3. **Document the lint contract.** Add `docs/contributing/docs-lint.md` — what it checks, how to satisfy, how to run locally, how to add a check.
4. **Decide grandfather strategy for existing in-tree failures.** `field-never-set-diagnostic.md` currently fails the lint by design (per SR-F1's in-tree evidence). Choose: (a) retrofit it as part of Phase 0 so the lint ships green; (b) ship the lint with an explicit allow-list and tracked debt issues to clear within N weeks. Recommend (a) — eat-the-dog-food principle.

### Exit criteria

- [ ] `docs-lint` runs locally via single command
- [ ] CI invokes `docs-lint` on every PR touching `docs/` or `*.md`
- [ ] `field-never-set-diagnostic.md` either passes or is on a tracked grandfather list
- [ ] `docs/contributing/docs-lint.md` documents the contract

### Doc-touch obligations

- `docs/contributing/docs-lint.md` — new
- `CONTRIBUTING.md` — add docs-lint to pre-commit / CI gate list
- `tools/Precept.DocsLint/` — new (if .NET analyzer route)

---

## Phase 1: Truth-up

**Goal:** Restore corpus trust through mechanical fixes to foundational docs.

**Findings addressed:** **DC-F1**, **DC-F2**, **DC-F3**, **DC-F4**.

### Tasks

1. **DC-F1 — Remove catalog counts from prose.** The enumeration in `catalog-system.md` is canonical; counts are derived, never stated independently. (Better than "single-source the count" — eliminates the parallel-maintenance failure mode entirely.)
   - `docs/language/catalog-system.md` — drop the "Catalog count convention (15 total)" footnote and reconciliation text. Keep the enumeration.
   - `docs/compiler-and-runtime-design.md` — "thirteen catalogs" / "the thirteen catalogs fall into two groups" → "the catalogs" / "the catalogs fall into two groups."
   - `docs/contributing/catalog-driven-checklist.md` — "Fourteen catalogs cover the complete language surface… The fourteen: …" → "The catalogs cover the complete language surface… The catalogs: …"
   - `CLAUDE.md` — note in Catalog System section: "The canonical catalog enumeration lives in `docs/language/catalog-system.md`. Other docs reference catalogs by name, not by count."

2. **DC-F2 — Fix `README.md`.**
   - Audit Quick Example against current runtime — replace API calls that throw `NotImplementedException` with shipped surface, or mark clearly as future API.
   - MCP tool count: "five MCP tools" → match live count in `docs/tooling/mcp.md`.
   - Fix broken link `docs/RuntimeApiDesign.md` → `docs/runtime/runtime-api.md`.
   - Verify all paths resolve.

3. **DC-F3 — Resolve dangling references.**
   - Known: `docs/PreceptLanguageDesign.md` (cited in `docs/language/README.md`, `docs/language/precept-language-spec.md`, `research/README.md`); `README-legacy.md`, `docs/DesignNotes-legacy.md` (cited in CLAUDE.md).
   - For each: either update the reference or delete it.

4. **DC-F4 — Extend status taxonomy.**
   - Collect every unique status value in `docs/**/*.md`.
   - Update `docs/README.md` and `CLAUDE.md` § Doc status conventions to include all in-use values with one-line meaning.
   - Likely additions: `Locked` / `Locked YYYY-MM-DD`, `Incremental`, `Full`, `Partial stub`, `Promoted to: <link>`.
   - Update `lifecycle-2-design/SKILL.md` to use only taxonomy values.

### Exit criteria

- [ ] Zero catalog-count statements in prose outside `catalog-system.md`'s enumeration
- [ ] `README.md` examples compile against the current runtime surface (or are clearly marked as future API)
- [ ] `docs-lint` finds zero dangling references
- [ ] `docs-lint` finds zero out-of-taxonomy status values

### Doc-touch obligations

- `docs/language/catalog-system.md`, `docs/compiler-and-runtime-design.md`, `docs/contributing/catalog-driven-checklist.md`, `CLAUDE.md` — F-1
- `README.md` — F-2
- `docs/language/README.md`, `docs/language/precept-language-spec.md`, `research/README.md`, `CLAUDE.md` — F-3
- `docs/README.md`, `CLAUDE.md`, `.claude/skills/lifecycle-2-design/SKILL.md` — F-4

---

## Phase 2: Discovery scaffolding

**Goal:** Close the fresh-agent onboarding gap. Make stateless agents as effective as onboarded humans.

**Findings addressed:** **DC-F9**, **DC-F10**, **DC-F13**, **DC-F18**.

### Tasks

1. **DC-F9 — Add `docs/glossary.md`.** ~30-40 load-bearing terms, one paragraph each, with cross-references.
   - Starter set: catalog, catalog member, catalog DU, modifier (5 subtypes), qualifier, construct, slot, accessor, descriptor, action, outcome, ensure, rule, guard, transition row, configuration, version, precept, plan, fault, diagnostic, ProofRequirement, ProofSubject, ConstructMeta, SlotValue, SemanticIndex, Compilation, Precept (type), Version (type), TypedField, TypedState, prevention vs detection, governance vs validation.
   - Decision: not always-required (context budget); discover-on-demand via grep or sub-README pointer.

2. **DC-F10 — Add `docs/agent-onboarding.md`.** ~200 lines, five organizing concepts:
   - Catalogs as the language spec in machine-readable form
   - The pipeline → Compilation → Precept → Version chain
   - Lifecycle-driven design (the 7-stage chain; skill-per-stage)
   - Pointer-philosophy and doc-sync
   - Always-required vs context-on-demand reads
   - Linked from `docs/README.md § Start here`. Not always-required reading.

3. **DC-F13 — Canonical sub-area README template.** Apply uniformly to language/, compiler/, runtime/, tooling/.
   - Template: Documents table (path, purpose, status), Reading Order (numbered), Relationship to Other Docs, Cross-cutting concerns.
   - Document the template in `docs/contributing/sub-area-readme-template.md`.

4. **DC-F18 — Expand `docs/tooling/README.md`** (currently 19 lines) to match the template.

### Exit criteria

- [ ] `docs/glossary.md` exists with ≥30 entries
- [ ] `docs/agent-onboarding.md` exists; linked from `docs/README.md`
- [ ] All four sub-area READMEs follow the canonical template
- [ ] `docs/tooling/README.md` is ≥50 lines

### Doc-touch obligations

- `docs/glossary.md`, `docs/agent-onboarding.md`, `docs/contributing/sub-area-readme-template.md` — new
- `docs/language/README.md`, `docs/compiler/README.md`, `docs/runtime/README.md`, `docs/tooling/README.md` — template application
- `docs/README.md`, `CLAUDE.md` — link agent-onboarding

---

## Phase 3: Philosophy operationalization

**Goal:** Close the philosophy-fade gap at both the doc-corpus level and the design-skill level. Both evaluations converge on the same fix from different angles — this phase implements that convergence.

**Findings addressed:**
- **DC-F7** — Approximation honesty fades to zero in operational type docs
- **DC-F8** — Domain-expert primacy has zero operational anchors
- **SR-F2** — No formal semantics, no reduction rules, no soundness obligation (CRITICAL in skills-rigor eval)
- **SR-F4** — Philosophy Alignment can be box-ticked; principles are catalog-shaped but the check is prose-shaped
- **SR-F8** — No teachability / error-message / domain-expert-reasoning check in the design skill (CRITICAL in skills-rigor eval — "single largest gap against Precept's stated positioning")

### Tasks

1. **DC-F7 — Add "Approximation Stance" sections to type docs.** ~5 lines per doc.
   - `primitive-types.md`, `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md` — state for each type family: exact, admits-approximation-in-cases-X-Y, or approximate-by-design.
   - Format: "This type family is [stance]. Reasoning: [link to philosophy commitment]. Implications: [author and downstream consequences]."

2. **DC-F8 + SR-F8 — Authoring Audience.** Combined fix at doc level and skill level.
   - **Doc level:** Add `## Authoring Audience` sub-section to `precept-language-spec.md § 0`. Restate philosophy commitment; list 3-5 operational implications (keyword-anchored grammar, mandatory `because`, readable diagnostics, no opaque proof, no iteration).
   - **Skill level:** Add `## Audience and Teachability` as a required section in `lifecycle-2-design/SKILL.md` for any language-surface change. Required content: (a) the worked example a domain expert would write using this feature (5-10 lines of `.precept`), (b) the error message a domain expert sees on one specific misuse, with explanation, (c) a 10-minute teaching path. Reviewer treats a missing Audience section on a language-surface change as a BLOCKER.

3. **SR-F2 — Add `## Semantic Rules` required section to `lifecycle-2-design/SKILL.md`.** Gated on the same trigger as Language Design Grounding. Required content:
   - For new expression forms: a small-step reduction rule in prose-or-notation form
   - For new typing rules: a Hindley-Milner-style inference-rule sketch with premises and conclusion
   - For new proof obligations: what the proof engine must establish before the construct is accepted
   - A soundness-preservation claim: "Principle N continues to hold because…" naming specific principles the feature could threaten
   - Skill refuses to lock if design touches expression evaluation, proof obligations, or constraint semantics without a Semantic Rules section.

4. **SR-F4 — Principle-coverage matrix for Philosophy Alignment.** Replace prose prompts with a row-per-principle matrix derived from `precept-language-spec.md § 0.1` (the 11 principles). Each row: Affected? (Y/N), How served (1 sentence + cite), Tension (1 sentence or N/A), Tradeoff (1 sentence or N/A). Designers cannot quietly skip a principle.
   - Companion: extract canonical principle list into a machine-extractable form (named section or YAML block) in the spec so the matrix is data-driven.

5. **Reviewer obligations.** Update `.claude/agents/precept-reviewer.md`:
   - Missing Approximation Stance on a new type → BLOCKER.
   - Missing Authoring Audience / Audience and Teachability section on a language-surface design → BLOCKER.
   - Missing Semantic Rules on a design touching evaluation, proof, or typing → BLOCKER.
   - Philosophy Alignment matrix not filled or any row left blank → BLOCKER.

### Exit criteria

- [ ] All four type docs carry an Approximation Stance section
- [ ] `precept-language-spec.md § 0` carries an Authoring Audience section
- [ ] `lifecycle-2-design/SKILL.md` carries required `## Semantic Rules` and `## Audience and Teachability` sections plus principle-coverage-matrix Philosophy Alignment
- [ ] `precept-reviewer.md` enforces all four new obligations as BLOCKERs
- [ ] `grep -c approximation docs/language/business-domain-types.md docs/language/temporal-type-system.md` returns ≥1 each
- [ ] `grep -c "domain expert" docs/language/precept-language-spec.md` returns ≥3

### Doc-touch obligations

- `docs/language/primitive-types.md`, `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md` — Approximation Stance
- `docs/language/precept-language-spec.md § 0` — Authoring Audience + principle-list extraction
- `docs/tooling/language-server.md` — cross-link Authoring Audience as diagnostic/hover design constraint
- `.claude/skills/lifecycle-2-design/SKILL.md` — Audience and Teachability section + Semantic Rules section + principle-coverage matrix for Philosophy Alignment
- `.claude/agents/precept-reviewer.md` — new BLOCKER obligations

---

## Phase 4: Decision and citation rigor

**Goal:** Strengthen the per-decision discipline. The four-leg rationale + citation-with-excerpt is the strongest forcing function in the skill — Phase 4 widens it to close known defeats.

**Findings addressed:**
- **SR-F3** — Citation is unidirectional; no counter-evidence requirement; "no precedent" gets a free pass
- **SR-F6** — No falsifiability / "what would prove me wrong" obligation
- **SR-F7** — Architecture Grounding stops at Precept-internal boundaries; no mandatory external comparator
- **SR-F9** — No reversibility / blast-radius / migration-cost analysis per decision
- **SR-F10** — Source open category but unfetchable URL citations slip in
- **SR-F15** — Decisions have no notion of grain; high-stakes and low-stakes decisions get identical treatment

### Tasks

1. **SR-F3 — Add "Strongest counter-evidence" as a per-decision leg.** Sixth leg required for every decision: the source (internal or external) most plausibly arguing against this decision, with excerpt and one-sentence response. Honest "no counter-evidence found after looking" acceptable if author says where they looked. Plus: "novel decision escalation" rule — decisions marked "no precedent — novel choice" get `status: Locked-Novel` and require a second epistemic-challenger pass.

2. **SR-F9 — Add "Reversibility" and "Blast radius" legs per decision.** Reversibility: Easy / Hard / Effectively-irreversible-post-ship, with one-sentence justification. Blast radius: catalogs touched, docs touched, samples touched, external consumers affected. Aggregate at top of Decisions section as a Lock-cost summary. Designs with any Irreversible decision require a Falsifiers section (per SR-F6).

3. **SR-F6 — Add `## Falsifiers` section.** Required for any design that locks behavior visible to external authors (language surface, error messages, diagnostic codes, MCP vocabulary). Format: 2-5 specific observations that, if seen post-ship, would force redesign. Concrete, measurable, decision-changing. Paired with `/lifecycle-7-audit` for revisit discipline.

4. **SR-F7 — Restructure Architecture Grounding into Precept-internal + External precedent.**
   - **Precept-internal placement** (current three sub-prompts): layer / propagation / breaking
   - **External architectural precedent** (NEW, mandatory): cite at least one comparator's solution to the architectural problem the design touches (CEL, OPA, CUE, Dhall, Roslyn, TypeScript, Rust, etc.), with excerpt, and explain Precept's divergence.
   - Reviewer treats a missing external comparator on a non-trivial architectural change as a CONCERN.

5. **SR-F10 — Strengthen citation discipline for unfetchable sources.**
   - External URL citations must: (a) include the full quoted excerpt verbatim (no paraphrasing/truncation), (b) include the access date, (c) be preferred only when no in-tree or paper-PDF equivalent exists.
   - For standards (RFCs, ISO docs, papers): require stable identifier (RFC#, DOI, title+venue+year). Prefer locally cached copies in `research/references/`.
   - Add `lifecycle-2-design --archive-citations` mode that mirrors external URL citations as text snapshots, committed alongside the design.

6. **SR-F15 — Stakes-based decision grain.** Decisions self-classify: `stakes: low | medium | high | irreversible`. Per-stakes rigor requirements:
   - `low`: Rationale + Tradeoff only (2 legs)
   - `medium`: All current legs (Rationale, Alternatives, Precedent, Tradeoff, Sources, Counter-evidence)
   - `high`: medium + Reversibility + Blast radius
   - `irreversible`: high + Falsifiers section + 24-hour cooling-off before Locked

### Exit criteria

- [ ] `lifecycle-2-design/SKILL.md` template includes Counter-evidence leg, Reversibility leg, Blast-radius leg per decision; Falsifiers section; restructured Architecture Grounding with external comparator requirement; stakes classification
- [ ] `docs-lint` enforces stakes-appropriate leg presence
- [ ] At least one existing in-tree design retrofitted as an example showing the new structure

### Doc-touch obligations

- `.claude/skills/lifecycle-2-design/SKILL.md` — substantial expansion of decision template
- `.claude/agents/precept-reviewer.md` — new CONCERN/BLOCKER obligations
- `tools/Precept.DocsLint/` — extend lint to check new legs

---

## Phase 5: Process and orchestration

**Goal:** Close skill-chain gaps and add structural independence to review. Strengthen the process layer so designs advance with appropriate weight.

**Findings addressed:**
- **SR-F5** — No designated reviewer-of-the-design independence; reviewer shares designer's framing
- **SR-F12** — Binary Draft → Locked; no staged advancement; high-stakes and low-stakes get identical treatment
- **SR-F13** — No security, observability, or evolvability dimension
- **SR-F14** — Reviewer's source verification stops short of mandatory source-checking per change category
- **DC-F12** — Skill chain missing Stage 4 (Execute)

### Tasks

1. **SR-F12 — Staged advancement model.** Replace binary Draft → Locked with four stages:
   - `Draft` → `Semantics-Stated` (Decision text + Semantic Rules section per SR-F2) → `Externally-Grounded` (Language Design Grounding + Architecture Grounding + external precedent + counter-evidence) → `Locked` (all above + acceptance criteria + doc-update enumeration + Falsifiers if applicable)
   - Each stage has explicit advancement criteria the lint enforces.
   - Low-stakes decisions skip stages (Draft → Locked); high-stakes decisions ladder through all four.
   - `/lifecycle-2-design` skill manages stage advancement; reviewer agent enforces.

2. **SR-F5 — Independent re-statement preamble for reviewer.** Mandatory before findings: reviewer must, in 3-5 sentences, restate the design in its own words — then compare that restatement to the design's own framing. Mismatches between restatements are first-class CONCERN findings. Add a "Strongest objection" finding category: even on APPROVED designs, the reviewer names the strongest reason a future engineer might regret this — recorded as NIT for postmortem-style retrospective.

3. **SR-F14 — Mandatory source-checking per change category.** For specific design-change types, the reviewer always opens specific sources regardless of citation:
   - Catalog member change → `src/Precept/Language/<Catalog>.cs` and catalog doc
   - Diagnostic change → `Diagnostics.cs` and `diagnostic-system.md`
   - Modifier-keyword change → `Modifiers.cs`, `TokenKind.cs`, `Tokens.cs`, `Lexer.cs`
   - Language-surface change → `precept-language-spec.md § 0.1` (run principle-coverage check from SR-F4)
   - Reviewer reports as `sources-mandatorily-checked` frontmatter; missing entries are a process violation, not a finding against the design.

4. **SR-F13 — Add `## Operational dimensions` section.** Required for changes touching:
   - Source-text ingestion (lexer/parser) — security prompt about adversarial input
   - Runtime evaluation — observability prompt about how violations surface
   - External dependencies (NodaTime, ICU, UCUM, ISO 4217) — evolvability prompt about upstream changes
   - Optional section; skill auto-skips prompts that don't apply.

5. **DC-F12 — Add `lifecycle-4-execute` skill.**
   - Path: `.claude/skills/lifecycle-4-execute/SKILL.md`.
   - Captures: vertical-slice discipline; PR-body update protocol; commit-message format; post-slice doc-touch verification (per CLAUDE.md routing); runtime/MCP/LS/grammar update reminders; PR-body-is-the-plan rule.
   - Triggers: "implement", "execute against this plan", "start the work", "ship this design", "open the PR".

### Exit criteria

- [ ] `lifecycle-2-design/SKILL.md` supports four stages with explicit advancement criteria
- [ ] `precept-reviewer.md` includes mandatory Independent re-statement preamble and Strongest objection finding category
- [ ] Reviewer mandatory source-checking enumerated per change category
- [ ] Optional `## Operational dimensions` section template defined
- [ ] `lifecycle-4-execute` skill exists and is invoked during Stage 4 work
- [ ] `docs-lint` enforces stage-appropriate completeness

### Doc-touch obligations

- `.claude/skills/lifecycle-2-design/SKILL.md` — staged model, Operational dimensions
- `.claude/skills/lifecycle-4-execute/SKILL.md` — new
- `.claude/agents/precept-reviewer.md` — re-statement preamble, Strongest objection, mandatory source-checking
- `CONTRIBUTING.md` — note `lifecycle-4-execute` in Doc Lifecycle table
- `tools/Precept.DocsLint/` — stage-based completeness checks

---

## Phase 6: Structural cleanup

**Goal:** Reduce context cost and eliminate structural drift sources in the corpus.

**Findings addressed:** **DC-F5**, **DC-F6**, **DC-F15**, **DC-F16**.

### Tasks

1. **DC-F6 — Route runtime/ design proposals to `docs/Working/runtime/`.**
   - Move pre-implementation design docs from `docs/runtime/` to `docs/Working/runtime/`: `evaluator.md` (2179 lines, Stub), `precept-builder.md` (973 lines, Stub), `descriptor-types.md` (206 lines, Stub), plus `result-types.md` and `fault-system.md` if pre-implementation (verify status).
   - Keep in `docs/runtime/`: `runtime-api.md` (public surface contract is locked) with explicit "implementation state" banner.
   - Update `docs/runtime/README.md` to reflect slimmer canonical set + point to `Working/runtime/`.

2. **DC-F5 — Convert `docs/compiler-and-runtime-design.md` to a pointer-hub.** Target: ~30KB, down from 125KB.
   - Keep: Mermaid pipeline diagram, Non-Negotiable Rules, artifact-flow narrative, Audience and "How to read this document" framing.
   - Replace per-stage sections (§§4-10) with one-paragraph summaries + pointers to `docs/compiler/<stage>.md`.
   - Replace runtime sections (§11) with pointer to `docs/runtime/`.
   - Replace tooling sections (§§13-15) with pointer to `docs/tooling/`.

3. **DC-F16 — Split `catalog-system.md`** (4500+ lines) into three docs.
   - `docs/language/catalog-system-architecture.md` — Architectural Identity, Vision, Completeness, Pattern Definition, Roslyn enforcement, Exhaustiveness strategies. ~1500 lines.
   - `docs/language/catalog-inventory.md` — Catalogs by name, members, status, short descriptions. ~1500 lines.
   - `docs/language/catalog-integration.md` — Pipeline stage integration patterns, Qualifier Propagation, Proof Obligations, Construct Slot Model. ~1500 lines.
   - `catalog-system.md` becomes a thin index (~50 lines) pointing to three sub-docs.
   - Update all cross-references atomically (CLAUDE.md, sub-area READMEs, other docs).
   - Preserve § anchor IDs where possible to keep fragment links working.

4. **DC-F15 — Add `docs/Working/Archive/README.md` decision-history index.**
   - One entry per archived doc: filename, date, topic, promoted-to (if applicable), one-line outcome.
   - Organize chronologically + by topic (language, compiler, runtime, tooling, process).
   - Add to `/lifecycle-5-promote` skill: append Archive-index entry on each archival.

### Exit criteria

- [ ] `docs/runtime/` contains only docs whose status is `Implemented`, `Active`, or `Canonical design — public surface locked`
- [ ] `docs/compiler-and-runtime-design.md` is ≤40KB
- [ ] `docs/language/catalog-system.md` is ≤100 lines (thin index); content lives in three sub-docs
- [ ] `docs/Working/Archive/README.md` indexes all 46 archived docs

### Doc-touch obligations

- `docs/runtime/` and `docs/Working/runtime/` — moves + status banner
- `docs/runtime/README.md` — slimmer canonical scope
- `docs/compiler-and-runtime-design.md` — pointer-hub conversion
- `docs/language/catalog-system.md` → split into three docs (original becomes thin index)
- All cross-references to `catalog-system.md` § X — update to point to correct sub-doc
- `docs/Working/Archive/README.md` — new
- `.claude/skills/lifecycle-5-promote/SKILL.md` — Archive-index maintenance obligation

### Risk

The catalog-system split touches every doc that references its sections. Mitigation: one atomic PR with all cross-reference updates landed together. Preserve § anchors to avoid breaking fragment links.

---

## Phase 7: Polish

**Goal:** Final consolidation and dedup.

**Findings addressed:** **DC-F14**, **DC-F17**.

### Tasks

1. **DC-F14 — Reduce duplication between agents and CLAUDE.md.**
   - Audit `.claude/agents/precept-author.md` (32KB) and `.claude/agents/precept-reviewer.md` (16KB) for content duplicating CLAUDE.md.
   - Extract overlapping content to one location (CLAUDE.md by default; agents cite by reference).
   - Each agent body retains only role-specific instructions.
   - Verify no rule lives only in agent body and not in CLAUDE.md.

2. **DC-F17 — Consolidate anti-patterns into `docs/contributing/anti-patterns.md`.**
   - Source material: `catalog-system.md § Pattern Definition`, `catalog-driven-checklist.md § Red flags`, `compiler-and-runtime-design.md § Anti-pattern`, scattered per-stage callouts.
   - Organize by layer: catalog, parser, type checker, proof engine, runtime, language server, MCP, grammar generator.
   - Each entry: pattern, why it's wrong, principle violated, correct alternative.
   - Target: 30-40 entries.
   - Reference from `precept-reviewer.md` Required Reading. Keep catalog-specific red flags in the checklist; reference from anti-patterns.md.

### Exit criteria

- [ ] `precept-author.md` and `precept-reviewer.md` contain no rule absent from CLAUDE.md or a CLAUDE.md-routed doc
- [ ] `docs/contributing/anti-patterns.md` exists with ≥30 entries

### Doc-touch obligations

- `.claude/agents/precept-author.md`, `precept-reviewer.md` — dedup
- `docs/contributing/anti-patterns.md` — new
- `.claude/agents/precept-reviewer.md` — add anti-patterns.md to Required Reading

---

## Open questions

These do not block plan execution; settle during the relevant phase:

1. **Phase 0 — grandfather strategy.** Hard-fail (retrofit existing locked docs) or allow-list with tracked debt? Recommend retrofit (eat the dog food).
2. **Phase 2 — `agent-onboarding.md` as required reading?** Recommend no (optional discovery via docs/README.md); context-budget tradeoff for owner.
3. **Phase 3 — principle list extraction format.** YAML block, dedicated section with stable anchor, or external file? Affects how the lint and the Philosophy Alignment matrix consume it.
4. **Phase 4 — counter-evidence escalation.** "Locked-Novel" status with second-challenger requirement: who fills the second-challenger role in an AI-agent-driven project? Reviewer agent re-invoked with different framing, or separate agent class?
5. **Phase 5 — staged advancement vs. existing locked designs.** Existing in-tree locked designs predate the staged model. Grandfather or re-stage? Recommend: existing designs stay Locked; new designs use the staged model.
6. **Phase 6 — runtime-api.md status banner wording.** Public surface locked but operations throw NotImplementedException. Owner-judgment.
7. **Phase 7 — anti-patterns.md vs catalog-driven-checklist.md boundary.** Plan recommends checklist for catalog-specific, anti-patterns.md for cross-layer. Verify at execution.

## Dependencies

- **Upstream:** None — this plan operates entirely on the doc corpus + skills system; no implementation dependencies on the compiler/runtime.
- **Downstream:** A cleaner corpus + working `docs-lint` + stronger design/review rigor enables higher-quality Phase 4+ feature work. The maxplaces-iso experience (a casual chat suggestion hardening into a locked design without rigor) becomes structurally harder.

## Definition of done

When all seven phases are complete:
- Foundational docs agree on every observable fact (no count drift, no dangling refs, no out-of-taxonomy status fields)
- A fresh AI agent navigates from `docs/philosophy.md` to substantive task via clearly signed paths, with glossary support and a coherent onboarding doc
- The philosophy's most-distinctive commitments (approximation honesty, domain-expert primacy, Principles 7/10/11) are operationally referenced in every doc and skill that could threaten them
- The design-lock skill demands formal semantics, principle-coverage matrix, audience and teachability content, falsifiers, counter-evidence, reversibility, blast radius, stakes-appropriate rigor
- The reviewer agent has structural independence (re-statement preamble, mandatory source-checking, strongest-objection finding)
- The lifecycle skill chain is complete (Stage 4 scaffolded; Stage 7 deferred per existing roadmap)
- `docs-lint` mechanically gates future regressions across all the above
- `compiler-and-runtime-design.md` is a pointer-hub; per-area docs are the canonical references; `catalog-system.md` is split into three navigable sub-docs

The bar moves from "below what the philosophy and skills claim" to "at the bar the philosophy and skills claim" — for the corpus, the skills, and the review process. The runtime implementation is not the subject of this plan.
