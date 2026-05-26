---
status: Draft plan — 2026-05-25 (extended with Phases 8-11 from research-system evaluation)
sources:
  - docs/Working/doc-corpus-evaluation-opus.md
  - docs/Working/skills-rigor-evaluation-opus.md
  - docs/Working/research-system-evaluation-opus.md
purpose: Execute the combined improvements surfaced by three Opus evaluations — restore corpus trust, scaffold AI-agent discovery, operationalize philosophy alignment, strengthen design and review rigor, reduce structural drift, and close the rhetorical-vs-real gap in research/citation discipline
---

# Doc Corpus + Skills Rigor Improvement Plan

## Sources

This plan executes findings from three independent Opus evaluations:

- [`doc-corpus-evaluation-opus.md`](doc-corpus-evaluation-opus.md) — 18 findings on the doc corpus + skills system as an AI-agent knowledge architecture (referenced below as **DC-Fn**)
- [`skills-rigor-evaluation-opus.md`](skills-rigor-evaluation-opus.md) — 15 findings on the design-lock skill and reviewer agent as process artifacts (referenced below as **SR-Fn**)
- [`research-system-evaluation-opus.md`](research-system-evaluation-opus.md) — 15 findings on the research corpus + `lifecycle-1-research` skill + Stage 1→2 handoff (referenced below as **RS-Fn**)

Total findings integrated: 48. Many overlap or reinforce each other; the phasing below combines them where they share an intervention.

## Phase summary

| Phase | Goal | Findings addressed | Effort | Status |
|---|---|---|---|---|
| 0 | Foundational tooling — mechanical enforcement layer | DC-F3, DC-F4, SR-F1, SR-F11 | 1-2 days | **Deferred** — CI infrastructure not yet in place; revisit when CI exists |
| 1 | Truth-up — restore corpus trust | DC-F1, DC-F2, DC-F3, DC-F4 | 1 day | ✅ Complete (`5ee32337`) |
| 2 | Discovery scaffolding — fresh-agent onboarding | DC-F9, DC-F10, DC-F13, DC-F18 | 2-3 days | ✅ Complete (`a231ce70`) |
| 3 | Philosophy operationalization — top-to-bottom alignment | DC-F7, DC-F8, SR-F2, SR-F4, SR-F8 | 2 days | ✅ Complete (`d5c6532c`) |
| 4 | Decision and citation rigor — strengthen the per-decision discipline | SR-F3, SR-F6, SR-F7, SR-F9, SR-F10, SR-F15 | 2-3 days | ✅ Complete (`18c41269`) |
| 5 | Process and orchestration — staging, independence, skill chain | SR-F5, SR-F12, SR-F13, SR-F14, DC-F12 | 2-3 days | ✅ Complete (`0a3cf6aa`) |
| 6 | Structural cleanup — context cost and drift surface | DC-F5, DC-F6, DC-F15, DC-F16 | 2-3 days | ✅ Complete (`744ef566` → `3a35e132`) |
| 7 | Polish — dedup and consolidation | DC-F14, DC-F17 | 1-2 days | ✅ Complete (`c4f64ef9`) |
| 8 | Operationalize Phase 4 — close rhetorical-vs-real gap (retrofit + skill text; no tooling) | RS-F1, RS-F9 | 1-2 days | ✅ Complete (`2e62d006`) |
| 9 | Strengthen `lifecycle-1-research` into a real Stage 1 (skill text) | RS-F2, RS-F3, RS-F8, RS-F12, RS-F13, RS-F14 | 2 days | ✅ Complete (`990293da`) |
| 10 | Promote-or-cite enforcement + corpus archival sweep (audit + skill text; no tooling) | RS-F5, RS-F7, RS-F10 | 2 days | ✅ Complete (`59917e25`) |
| 11 | Research-adequacy gate at design lock (skill text + reviewer table; no tooling) | RS-F6, RS-F15 | 1 day | ✅ Complete (`3d6578cc`) |

**Total remaining estimate:** ~5-7 days focused work for Phases 8-11 (Phase 0 out of scope; Phases 1-7 complete). Smaller than originally proposed because all `docs-lint` tooling tasks are out of scope along with Phase 0.
**Minimum viable Phase 8+ improvement:** Phase 8 alone (1-2 days) closes the most damaging finding — that Phase 4's locked-design rules are universally unenforced in-tree — by retrofitting the one in-tree locked design.
**Highest-leverage Phase 8+ subset:** Phases 8 + 10 (3-5 days) restore the discipline contract *and* clear the shadow-policy research backlog.

**Sequencing notes:**
- **Phase 0 is out of scope** (early-days project; no CI process). All `docs-lint` tooling work is deferred with it. The SR-F1 / SR-F11 / RS-F1 enforcement gap is acknowledged and accepted as a known limitation. **Without CI, Phases 8-11 rely on: (a) skill text gating author behavior, (b) `precept-reviewer` post-hoc verification, (c) manual `grep`-based audits at commit time.** No automated build-time gate ships in this plan.
- Phase 3 combined doc-corpus and skills-rigor findings into one philosophy-alignment intervention — both evaluations converged on the same fix.
- Phases 4, 5, 6, 7 were largely parallelizable after Phase 1 and shipped in order.
- Phase 6 (structural restructure) was the highest-risk previous phase; mitigation via precept-reviewer pre-review surfaced two BLOCKERs before execution (§15 dual-canonical and §14 stale precept_language references) — both addressed.
- **Phases 8-11 sequencing**: Phase 8 first (retrofit + skill discipline) → Phase 9 in parallel with Phase 10 → Phase 11 last (depends on a clean corpus). Phase 8 is the keystone: until at least one in-tree locked design demonstrates Phase 4 rules can bind in practice, every prior phase's "guard X refuses Y" claim is rhetorical (the failure mode RS-F1 names empirically).
- **Honest limitation**: without docs-lint, Phases 8-11 cannot mechanically refuse a locked design that violates the rules. The reviewer agent will catch it post-hoc if invoked; authors who skip the reviewer ship past the gate. This is the cost of deferring Phase 0; the gates become discipline + reviewer-verification, not automated enforcement.

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

**Findings addressed:** **DC-F5**, **DC-F6** (revised), **DC-F15**, **DC-F16** (revised).

**Revisions from the original plan (recorded 2026-05-25):**
- **DC-F6 revised** — No file moves. Runtime design docs stay in `docs/runtime/` because the *design* is canonical even when implementation is a stub. Phase 2 added implementation-state honesty to `docs/runtime/README.md` (banner) and individual docs' status fields. Phase 6 verifies each runtime doc's status field matches reality; no relocation. Reasons: (a) moving implies "up for revision," which weakens the public-surface contract; (b) `docs/Working/` is for in-flight work being argued, not designs awaiting implementation; (c) honesty is achieved via status fields and banner, not folder location; (d) moving breaks cross-references across the corpus.
- **DC-F16 revised** — No split. Apply catalog-driven discipline to the doc about catalogs: the Level 3 Member Inventories section is parallel knowledge (the catalogs in `src/Precept/Language/*.cs` ARE the inventory; MCP catalog-reference tools surface it on demand). Remove the ~1500-line inventory section; keep architecture + integration together in `catalog-system.md`; ensure TOC is comprehensive. Single source of truth preserved; drift surface eliminated. The split-into-three would create cross-doc reference burden and split-the-baby moments where it's unclear which doc some content belongs to.

### Tasks

1. **DC-F5 — Convert `docs/compiler-and-runtime-design.md` to a pointer-hub.** Target: ~30KB, down from 125KB.
   - Keep: Mermaid pipeline diagram, Non-Negotiable Rules, artifact-flow narrative, Audience and "How to read this document" framing.
   - Replace per-stage sections (§§4-10) with one-paragraph summaries + pointers to `docs/compiler/<stage>.md`.
   - Replace runtime sections (§11) with pointer to `docs/runtime/`.
   - Replace tooling sections (§§13-15) with pointer to `docs/tooling/`.
   - Preserve § anchor IDs for sections that remain as pointer-targets so existing fragment links still resolve.

2. **DC-F6 (revised) — Verify implementation-state honesty across runtime docs.**
   - No file moves; the design is canonical.
   - Audit each `docs/runtime/*.md`: verify the status field accurately reflects implementation maturity (`Stub`, `Partial stub`, `Design — public surface locked`, etc., per the canonical taxonomy).
   - Confirm the implementation-state banner added in Phase 2 to `docs/runtime/README.md` adequately signals the design-locked-but-implementation-pending pattern.
   - If any individual runtime doc's status field is misleading, fix it.

3. **DC-F16 (revised) — Trim `catalog-system.md`'s inventory section.**
   - Remove the Level 3 Member Inventories section (~1500 lines).
   - Replace with a single paragraph: "The canonical catalog inventory lives in `src/Precept/Language/*.cs`. Query the MCP catalog-reference tools (`precept_syntax`, `precept_types`, `precept_operations`, `precept_domains`, `precept_proofs`, `precept_patterns`) for current member lists with descriptions. This document deliberately does not maintain a parallel listing — applying the catalog-driven philosophy to the doc about the catalog system."
   - Keep architecture + integration content (~3000 lines) together in `catalog-system.md`.
   - Audit the Contents TOC at the top — verify every section has a stable anchor and is listed.
   - No file split; preserves single-source-of-truth property.

4. **DC-F15 — Add `docs/Working/Archive/README.md` decision-history index.**
   - One entry per archived doc: filename, date, topic, promoted-to (if applicable), one-line outcome.
   - Organize chronologically + by topic (language, compiler, runtime, tooling, process).
   - Add to `/lifecycle-5-promote` skill: append Archive-index entry on each archival.

### Exit criteria

- [ ] `docs/compiler-and-runtime-design.md` is ≤40KB
- [ ] Every `docs/runtime/*.md` carries an implementation-state field that matches reality
- [ ] `docs/language/catalog-system.md` no longer contains member inventories; target ~3000 lines (down from 4500); TOC complete with stable anchors
- [ ] `docs/Working/Archive/README.md` indexes all archived docs

### Doc-touch obligations

- `docs/compiler-and-runtime-design.md` — pointer-hub conversion
- `docs/runtime/*.md` — verify each doc's implementation-state field (most already correct from Phase 2)
- `docs/language/catalog-system.md` — remove Member Inventories section + audit TOC
- `docs/Working/Archive/README.md` — new
- `.claude/skills/lifecycle-5-promote/SKILL.md` — Archive-index maintenance obligation

### Risk

The compiler-and-runtime-design.md pointer-hub conversion is the highest-risk change — many docs cite specific sections. Mitigation: preserve § anchor IDs for sections kept as pointers; verify cross-references resolve after the conversion. The catalog-system inventory removal is lower risk because consumers of inventory data already go through source / MCP tools.

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

## Phase 8: Operationalize Phase 4 — close the rhetorical-vs-real gap

**Goal:** Demonstrate that the citation and decision-discipline rules in `lifecycle-2-design` can bind in practice. Retrofit the one in-tree locked design to the full Phase 4 standard, and tighten the skill text so the rules read as obligations rather than aspirations. **No tooling work** — Phase 0 is out of scope; enforcement remains discipline + reviewer-agent post-hoc verification.

**Findings addressed:** **RS-F1** (Phase 4 rules unenforced on every locked design — addressed by retrofit + reviewer-agent obligation), **RS-F9** (`--from <research-doc>` undocumented/unused — addressed by removing the rhetorical claim).

**Why Phase 8 is the keystone:** the empirical citation audit found that `field-never-set-diagnostic.md` is `Locked 2026-05-25` with 6 decisions, every one missing every Phase 4 leg. The only docs that follow the lifecycle-2 frontmatter discipline are the meta-docs about the system (the three Opus evals + this plan), not designs the system produced. **Until at least one in-tree locked design demonstrates Phase 4 rules can bind in practice, every prior phase's "guard X refuses Y" claim is rhetorical.** Phase 8 produces that proof.

### Tasks

1. **Retrofit `docs/Working/field-never-set-diagnostic.md` to Phase 4 standard.**
   - Add `sources-consulted` frontmatter listing every source identifier cited inline.
   - Classify each of the 6 decisions with `**Stakes**: low | medium | high | irreversible` (the keyword retirement of `writable` is `irreversible` per the skill's own table).
   - Add `Sources consulted for this decision:` leg per decision, with verbatim excerpts for every external-language claim (Rust `mut`, TypeScript `readonly`, Kotlin `val`/`var`, SQL `GRANT UPDATE`). If excerpts can't be produced because the citations were always plausible-prose-not-citation, that's the finding — surface to owner.
   - Add `## Falsifiers` section (external-author-visible language change with `irreversible` stakes).
   - Add `Strongest counter-evidence:` leg on every high+ stakes decision (Decision 5 minimum).
   - **Honest failure path:** if the retrofit reveals the comparable-systems grounding doesn't exist in the form Phase 4 requires, the design's `Locked` status comes off and the doc returns to `Externally-Grounded`; the design proceeds only after the gap is filled by Phase 10 / 11 work.

2. **Remove the rhetorical `--from <research-doc>` claim from `lifecycle-2-design/SKILL.md`.**
   - The skill currently advertises a `--from` flag that "extracts research conclusions and pre-populates the Decisions section's Rationale and Precedent legs from the research findings." No design in the tree shows evidence of having been produced this way; the mechanism either doesn't exist or has never been used.
   - Drop the `--from` claim from `§ Composability`.
   - Replace with explicit human-discipline obligation: "If a `research/` file exists for the topic, cite it in `sources-consulted` and copy verbatim excerpts into per-decision `Sources consulted for this decision:` legs."
   - Revisit a mechanical `--from` implementation only if manual discipline proves insufficient.

3. **Strengthen reviewer-agent post-hoc enforcement.**
   - Add to `precept-reviewer.md` § 9 (Per-Decision Rationale and Stakes-Based Rigor): when reviewing a `Locked` design doc, the reviewer must mechanically check for each required leg via grep and report a BLOCKER finding for every missing leg.
   - Add to § 13 (Source Verification): on any locked design touching a topic with existing `research/` content, the reviewer must check whether the design cites that research; missing citation is at minimum a CONCERN.
   - These are skill-text gates the reviewer applies; without CI, they only fire when the reviewer is invoked.

### Exit criteria

- [ ] `docs/Working/field-never-set-diagnostic.md` carries the full Phase 4 leg structure (or is returned to `Externally-Grounded` if the grounding doesn't exist — surface honestly)
- [ ] `lifecycle-2-design/SKILL.md` `--from` claim is removed and replaced with human-discipline obligation
- [ ] `precept-reviewer.md` carries explicit grep-based leg-checking discipline + research-citation check for affected topics
- [ ] No new locked designs in `docs/Working/` ship without the leg structure (verified by reviewer-agent invocation on PRs touching `docs/Working/`)

### Doc-touch obligations

- `docs/Working/field-never-set-diagnostic.md` — full retrofit
- `.claude/skills/lifecycle-2-design/SKILL.md` — remove `--from` claim; replace with human-discipline obligation
- `.claude/agents/precept-reviewer.md` — extend § 9 + § 13 with grep-based leg-checking + research-citation check

### Risk

The retrofit may surface that `field-never-set-diagnostic.md` Decision 5 (keyword unification) wasn't grounded in cited Rust / TypeScript / Kotlin / SQL behavior — it was plausible-sounding prose. **Surfacing this is the point**; the corpus needs to confront whether existing designs hold up under Phase 4 scrutiny. Mitigation: if the retrofit reveals the grounding doesn't exist, that's a finding to surface to the owner, not a reason to skip the retrofit. The design may need to advance through Phase 10 / 11 work before re-Locking.

**Honest limitation**: without docs-lint (Phase 0 out of scope), Phase 8 cannot mechanically refuse a locked design that violates the rules. The reviewer agent will catch it post-hoc if invoked; authors who skip the reviewer ship past the gate. This is the cost of deferring Phase 0.

---

## Phase 9: Strengthen `lifecycle-1-research` into a real Stage 1

**Goal:** Add refusal gates, required structural sections, and source-grading discipline to the research skill so it produces artifacts at the level the system claims they're produced at.

**Findings addressed:** **RS-F2** (template not stage), **RS-F3** (citation discipline), **RS-F8** (missing Methodology / Threats to Validity sections), **RS-F12** (research-vs-advocacy distinction), **RS-F13** (source-grading absent), **RS-F14** (no Status field on research files).

**Why Phase 9 is necessary:** The research-system evaluation found 2 imperative-gating instances in `lifecycle-1-research` vs 64 in `lifecycle-2-design`. The skill describes a methodology but has no rejection criteria. The best research files (currency survey, formal-spec comparators, parser-combinator) meet a high bar *because the author chose to* — nothing in the skill required it. Other files (xstate.md, polly.md, linq.md, temporal-type-strategy.md) cite a single URL or zero external sources. The variance is structural, not a quality problem the author can fix without skill changes.

### Tasks

1. **Add explicit numbered behavioral guards to `.claude/skills/lifecycle-1-research/SKILL.md`.** Parallel to lifecycle-2-design's 15 guards. Minimum set:
   - **Guard 1: External sources required for external questions.** Refuse research that cites zero external sources on a question whose answer exists in the broader field. Explicit honest exit: `external-engagement-status: purely-internal — <one-line justification>` in frontmatter.
   - **Guard 2: Verbatim excerpt per load-bearing claim.** Refuse claims about external systems without a verbatim excerpt and stable source identifier (DOI / RFC# / ISO# / vendor-doc URL with access date).
   - **Guard 3: Methodology section required.** Refuse research without `## Methodology` naming what was searched, what was excluded, and why. Single-paragraph methodology is acceptable; absence is not.
   - **Guard 4: Threats-to-validity section required.** Refuse research without `## Threats to Validity` (or "no threats identified — flag for review" as honest exit). Lists the strongest reasons this conclusion might be wrong.
   - **Guard 5: Falsifiability for conclusions.** Research files that propose a conclusion (not just a survey) must include `## What would change this conclusion` section — 2-3 observations or evidence-shapes that would force re-investigation.

2. **Add `Status:` taxonomy to research-file frontmatter.**
   - Values: `Active` (informing current decisions), `Promoted` (decision adopted into spec/design — include link), `Cited` (referenced from another doc but not yet decision-adopting), `Stale` (predates major redesign), `Superseded by: <link>`, `Archived`.
   - Document in skill body + research/README.md.
   - Apply retroactively as opportunistic work — Phase 10's corpus sweep is the natural place to land the bulk retrofit.

3. **Add source-grading guidance to the skill.**
   - **Primary**: standards (RFC#, ISO#, W3C), peer-reviewed papers (DOI + venue + year), authoritative library docs with public versioning.
   - **Secondary**: vendor documentation, community implementations, prominent blog posts by named authors.
   - **Tertiary**: forum posts, knowledge claims supplemented when source is unfetchable.
   - Designs with only tertiary sources on a load-bearing decision are CONCERNs in `precept-reviewer` review.
   - The skill must require source-grade declaration per citation, OR each citation's nature is inferable from the identifier format (RFC# → Primary, vendor URL → Secondary, etc.).

4. **Add external-citation discipline section to the skill.**
   - Every external URL: full quoted excerpt verbatim, access date, stable identifier when standards/academic/RFC, preferred local mirror at `research/references/` for load-bearing sources.
   - Mirror discipline: load-bearing external sources should be snapshotted to `research/references/<topic>/<source-name>.md` with the verbatim excerpt + metadata. Live URLs may rot; mirrors do not.

5. **Update `precept-reviewer.md` to add a Stage 1 (research-doc) review path.**
   - When review target is a `research/*.md` file, reviewer applies the Phase 9 guards (sources, methodology, threats, falsifiability, source-grading).
   - Add to § 13 Source Verification: the reviewer must independently sample at least 3 external citations from the research file and verify excerpts.

### Exit criteria

- [ ] `lifecycle-1-research/SKILL.md` has explicit numbered behavioral guards (≥5)
- [ ] Skill enforces Methodology, Threats to Validity, and What-would-change-this-conclusion sections for research that proposes conclusions
- [ ] Skill carries source-grading taxonomy (Primary / Secondary / Tertiary)
- [ ] Skill mandates external-citation discipline (verbatim excerpt, access date, stable identifier, mirror)
- [ ] `Status:` field convention documented + ≥10 existing research files retrofitted opportunistically
- [ ] `precept-reviewer.md` carries a Stage 1 (research-doc) review path

### Doc-touch obligations

- `.claude/skills/lifecycle-1-research/SKILL.md` — substantial expansion (behavioral guards, source-grading, citation discipline, required sections)
- `.claude/agents/precept-reviewer.md` — research-doc review section
- `research/README.md` — Status field convention; cross-link to skill changes
- ~10 sampled research files — opportunistic Status-field retrofit (full corpus sweep in Phase 10)

### Risk

Adding refusal gates retroactively defines existing research as substandard. **Mitigation:** explicit grandfather rule — research files predating the cutoff date retain their current state; new gates apply to research produced after the cutoff. The retroactive opportunistic work in Phase 10 is upgrade, not validation-gate. The 6 high-quality research files identified in the eval (currency, formal-spec, parser-combinator, intellisense, domain-integrity, case-insensitive) already meet most of the new bar; their retrofit is incremental.

---

## Phase 10: Promote-or-cite enforcement + corpus archival sweep

**Goal:** Make the promote-or-cite rule actually binding. Audit every `research/*.md` file for inbound citations from `docs/`. Promote, cite, or archive each. Correct sub-folder taxonomy errors. Add the cross-corpus topic index.

**Findings addressed:** **RS-F5** (promote-or-cite empirically violated for 4 of 5 sampled files), **RS-F7** (sub-folder taxonomy unenforced; mis-filed research), **RS-F10** (no cross-corpus topic index).

**Why Phase 10 is necessary:** The research-system eval found that of 5 sampled research files, 4 are sub-cited or shadow-policy by the skill's own definition (`parser-combinator-scalability.md`, `formal-spec-languages-comparators.md`, `security/security-survey.md`, `research/product/*.md`). `research/archive/` contains exactly one file across the entire corpus history — the rule the skill claims (research that didn't ship goes to archive) is empirically unenforced. The corpus contains substantive shadow policy.

### Tasks

1. **Inbound-citation audit across all `research/` files.**
   - For every file, identify: (a) inbound citations from `docs/`, (b) inbound citations from another `research/` file, (c) inbound citations from a sample or test.
   - Produce `research/audit-promote-or-cite-2026-MM-DD.md` listing audit results in a table: file path / inbound-citation count / inbound-citation sources / categorization.
   - Three categorizations per file: `Promoted` (conclusion adopted into canonical doc — list canonical doc), `Cited` (referenced from another design/research/proposal), `Shadow` (no inbound citation; needs action).

2. **For each Shadow file, choose one of three resolutions:**
   - **Promote.** The conclusion is adopted into a canonical doc; the research file remains as evidence. Effort: tactical doc-update per file. Owner-judgment-required for which conclusion goes where.
   - **Cite.** Add a cross-reference from an existing design/proposal/spec that uses the conclusion implicitly. Effort: one edit per file.
   - **Archive.** Move to `research/archive/<topic>/` with a one-line note in frontmatter (`archived: <date> — <reason>`) explaining why it didn't ship. The skill mandates this for content that doesn't promote/cite.
   - **Honest exit:** "deliberate horizon groundwork" — research the project intentionally produces before downstream decisions need it. If declared, the file carries `Status: Active — horizon groundwork` and is exempt from the promote-or-cite gate until a downstream decision either adopts or supersedes it.

3. **Sub-folder taxonomy correction.** Per RS-F7:
   - `parser-combinator-scalability.md` → `research/architecture/compiler/`
   - `precept-language-mcp-audit.md`, `precept-language-tool-architecture.md` → `research/architecture/` (or new `research/tooling/`)
   - `philosophy-refresh-assessment.md` → `research/philosophy/`
   - `ucum-tier1-curation.md` → `research/language/references/`
   - Use `git mv` to preserve history. Update inbound citations in `docs/` and other `research/` files in the same PR.

4. **Add `research/INDEX.md` topic index.**
   - Single-page topic-to-file map across the full corpus.
   - Topics: currency precision, unit normalization, guard composition, timezone semantics, access modifiers, proof discharge, parser architecture, type system, state machines, etc.
   - Per topic: short description + file list (with sub-folder paths).
   - Anchored from `research/README.md § Start here`.
   - Maintenance obligation: `/lifecycle-1-research` skill updates `research/INDEX.md` as the final step on every new research file.

5. **Add inbound-citation expectation to `lifecycle-1-research` skill.**
   - Every new research file must, at completion, satisfy one of: (a) inbound citation from `docs/` exists or is added in the same PR, (b) inbound citation from another `research/` file exists or is added, (c) `Status: Active — horizon groundwork` is declared in frontmatter, OR (d) the file moves to `research/archive/`.
   - The skill's promote-or-cite step becomes a checklist gate the author runs before declaring the research complete.
   - **Honest limitation**: without docs-lint, this is author-discipline + reviewer-checked, not automated.

### Exit criteria

- [ ] Inbound-citation audit committed at `research/audit-promote-or-cite-2026-MM-DD.md`
- [ ] Every `research/` file categorized as `Promoted` / `Cited` / `Horizon groundwork` / `Archived`
- [ ] Mis-filed research relocated per the skill taxonomy (≥4 known relocations)
- [ ] `research/INDEX.md` exists with cross-folder topic-to-file map; linked from `research/README.md`
- [ ] `lifecycle-1-research` skill carries the inbound-citation expectation as a completion gate
- [ ] `lifecycle-1-research` skill carries the obligation to update `research/INDEX.md` on every new file

### Doc-touch obligations

- `research/audit-promote-or-cite-2026-MM-DD.md` — new
- `research/INDEX.md` — new
- ≥4 research files — relocations via `git mv`
- Inbound citations in `docs/` to relocated research files — updated
- `research/README.md`, `research/language/README.md`, `research/architecture/compiler/README.md` — cross-link to INDEX
- `.claude/skills/lifecycle-1-research/SKILL.md` — INDEX.md maintenance obligation + sub-folder discipline guards + inbound-citation completion gate

### Risk

Bulk relocations break inbound links from `docs/`. **Mitigation:** `git mv` preserves history; `grep -rn "research/<old-path>"` and update every reference in the same PR. The audit is the riskier work because it may surface that the corpus contains more shadow-policy than the team expects — surface honestly to owner; the resolution per file is owner-judgment, not skill-automation.

**Honest limitation**: without docs-lint, the inbound-citation expectation is enforced by author discipline + reviewer-checked when invoked. New shadow files can accumulate between reviewer passes.

The "deliberate horizon groundwork" exit must be used sparingly. Every file flagged as horizon work is a research artifact that's structurally allowed to sit without adoption. Overuse re-creates the shadow-policy problem under a different label.

---

## Phase 11: Research-adequacy gate at design lock

**Goal:** Close the gap where a `Locked irreversible` design can ship without verifiable prior-art research for the comparable-systems claims it makes. Add a research-adequacy check to the design skill's Locked-advancement criteria and a topic-to-comparator table to the reviewer agent.

**Findings addressed:** **RS-F6** (no research-shaped gap detection), **RS-F15** (reviewer's Source Verification is post-hoc; doesn't catch missing-source claims).

**Why Phase 11 depends on prior phases:** The gate Phase 11 adds — "design with irreversible decisions must cite a research file or carry an inline survey" — depends on (a) the citation discipline of Phase 8 actually binding, and (b) the research corpus being in a clean state (Phase 10) so the gate has trustworthy material to check against. Adding the gate before the corpus is clean would force designs to cite research that hasn't been categorized or quality-checked.

### Tasks

1. **Add a Locked-advancement criterion to `lifecycle-2-design/SKILL.md`.**
   - For any design with at least one `Stakes: irreversible` decision, the design must either:
     - (a) Cite a research file in `research/` that surveyed the relevant comparable systems with verbatim excerpts and meets Phase 9's Stage-1 quality bar, OR
     - (b) Carry an inline survey leg per decision (`Inline survey:`) meeting the same discipline as a Stage-1 research artifact: per-comparator verbatim excerpt, access date, stable identifier.
   - Honest exit: `comparable-systems-research-status: not-applicable — <one-line justification>` declaring no comparable-system claims are being made.

2. **Add a "Mandatory comparator-checking by topic" table to `.claude/agents/precept-reviewer.md`** (parallel to the change-category table in § 13's mandatory-source-checking).

   Topic → mandatory comparators (the reviewer always checks the design cites these or has explicit declarations):
   - **Access modifiers** → Rust references, TypeScript references, Kotlin references, Java references
   - **Temporal types** → Joda-Time / java.time / Python datetime / NodaTime / chrono / Pendulum references
   - **Money/currency** → Joda-Money / JSR-354 / NodaMoney / Stripe / Adyen references
   - **Constraint composition** → CEL / OPA / CUE / FluentValidation references
   - **State machines** → xstate / Stateless.NET / SCXML references
   - **Parser architecture** → Roslyn / ANTLR / Pratt / PEG (Ford 2004) / Superpower references
   - **Proof systems** → Dafny / Liquid Haskell / SPARK Ada / CBMC / Frama-C references
   - **Quantity / units** → UCUM / NIST SP 811 / Pint (Python) / units library (Haskell) references

3. **Document the gate in `lifecycle-2-design/SKILL.md § Staged advancement`.**
   - Add a row to the stage-advancement criteria table: advancement from `Externally-Grounded` to `Locked` for designs with `irreversible` decisions requires research-adequacy verification.
   - Cross-link to `precept-reviewer.md § Mandatory comparator-checking by topic`.
   - The gate is enforced by skill text + reviewer-agent invocation, not by build-time lint (Phase 0 deferred).

### Exit criteria

- [ ] `lifecycle-2-design/SKILL.md` carries the research-adequacy gate as an explicit Locked-advancement criterion
- [ ] `precept-reviewer.md` carries the topic-to-comparator table
- [ ] At least one in-tree design exercises the gate (likely `field-never-set-diagnostic.md` if its retrofit in Phase 8 surfaces the keyword-retirement comparable-systems gap)

### Doc-touch obligations

- `.claude/skills/lifecycle-2-design/SKILL.md` — research-adequacy gate; staged-advancement table update
- `.claude/agents/precept-reviewer.md` — topic-to-comparator mandatory-checking table
- `research/INDEX.md` — referenced from the gate

### Risk

The gate could over-fire on small designs that touch a topic but don't make load-bearing comparable-systems claims. **Mitigation:** the gate triggers only on `irreversible` stakes OR on explicit comparable-systems-claims-in-prose detection (heuristic: prose mentions named comparator systems like "Rust", "TypeScript", "Joda-Time" etc.). The honest exit (`comparable-systems-research-status: not-applicable`) is acceptable but must be declared and reviewer-checked.

The topic-to-comparator table will become stale as Precept's scope evolves. **Mitigation:** the maintenance obligation is added to `/lifecycle-7-audit` (when that ships) — periodically review the table against the current scope and add/remove topics.

**Honest limitation**: without docs-lint (Phase 0 out of scope), the gate is enforced by skill-text obligation + reviewer-agent invocation. Authors who skip the reviewer can ship a `Locked irreversible` design without the research adequacy check.

---

## Open questions

These do not block plan execution; settle during the relevant phase:

1. **Phase 0 — grandfather strategy.** Hard-fail (retrofit existing locked docs) or allow-list with tracked debt? Recommend retrofit (eat the dog food). Phase 8 partially addresses this by retrofitting `field-never-set-diagnostic.md`.
2. **Phase 2 — `agent-onboarding.md` as required reading?** Recommend no (optional discovery via docs/README.md); context-budget tradeoff for owner. (Resolved in Phase 2: optional discovery via docs/README.md.)
3. **Phase 3 — principle list extraction format.** YAML block, dedicated section with stable anchor, or external file? Affects how the lint and the Philosophy Alignment matrix consume it.
4. **Phase 4 — counter-evidence escalation.** "Locked-Novel" status with second-challenger requirement: who fills the second-challenger role in an AI-agent-driven project? Reviewer agent re-invoked with different framing, or separate agent class?
5. **Phase 5 — staged advancement vs. existing locked designs.** Existing in-tree locked designs predate the staged model. Grandfather or re-stage? Recommend: existing designs stay Locked; new designs use the staged model. (Phase 8 retests this for `field-never-set-diagnostic.md` — may force re-staging.)
6. **Phase 6 — runtime-api.md status banner wording.** Public surface locked but operations throw NotImplementedException. Owner-judgment. (Resolved in Phase 6: implementation-state field on each doc + Phase 2 banner.)
7. **Phase 7 — anti-patterns.md vs catalog-driven-checklist.md boundary.** Plan recommends checklist for catalog-specific, anti-patterns.md for cross-layer. Verify at execution. (Resolved in Phase 7: anti-patterns.md is the cross-layer index; checklist is the catalog-specific operational gate.)
8. **Phase 8 — `--from <research-doc>` Option A vs Option B.** Implement mechanically vs remove the claim? Recommend Option B for Phase 8 (remove); revisit Option A in a later phase if manual discipline proves insufficient.
9. **Phase 8 — `field-never-set-diagnostic.md` honest-failure path.** If Phase 4 retrofit reveals the comparable-systems grounding doesn't exist in citation form, does the design return to `Externally-Grounded` (recommended) or stay `Locked` with tracked debt? Owner-judgment.
10. **Phase 9 — research-skill grandfather rule cutoff date.** When do the new gates apply to new research? Recommend: gates apply to research produced after Phase 9 ships; pre-existing research is upgraded opportunistically in Phase 10.
11. **Phase 10 — "deliberate horizon groundwork" exit overuse.** How sparingly should this exit be used to keep the promote-or-cite rule binding? Recommend: max N% of files at any time can carry `horizon groundwork` status; periodic audit catches drift.
12. **Phase 11 — topic-to-comparator table staleness.** Who owns updating the table as Precept's scope evolves? Recommend: `/lifecycle-7-audit` (when shipped) takes the maintenance obligation.

## Dependencies

- **Upstream:** None — this plan operates entirely on the doc corpus + skills system; no implementation dependencies on the compiler/runtime.
- **Downstream:** A cleaner corpus + stronger design/review rigor + the eventual docs-lint (when Phase 0 is revisited with CI) enables higher-quality Phase 4+ feature work. The maxplaces-iso experience (a casual chat suggestion hardening into a locked design without rigor) becomes structurally harder.

## Definition of done

When Phases 1-7 are complete (✅ as of 2026-05-25):
- Foundational docs agree on every observable fact (no count drift, no dangling refs, no out-of-taxonomy status fields)
- A fresh AI agent navigates from `docs/philosophy.md` to substantive task via clearly signed paths, with glossary support and a coherent onboarding doc
- The philosophy's most-distinctive commitments (approximation honesty, domain-expert primacy, Principles 7/10/11) are operationally referenced in every doc and skill that could threaten them
- The design-lock skill demands formal semantics, principle-coverage matrix, audience and teachability content, falsifiers, counter-evidence, reversibility, blast radius, stakes-appropriate rigor
- The reviewer agent has structural independence (re-statement preamble, mandatory source-checking, strongest-objection finding)
- The lifecycle skill chain is complete (Stage 4 scaffolded; Stage 7 deferred per existing roadmap)
- `compiler-and-runtime-design.md` is a pointer-hub; per-area docs are the canonical references; `catalog-system.md` inventory removed (catalogs in source + MCP tools are the canonical inventory)

When Phases 8-11 are complete:
- At least one in-tree locked design carries the full Phase 4 leg structure as proof the rules can bind in practice
- `lifecycle-1-research` is a real Stage 1 with refusal gates, required Methodology + Threats to Validity + What-would-change-this-conclusion sections, source-grading discipline, and Status field
- Every research file is categorized (Promoted / Cited / Horizon groundwork / Archived); shadow policy is surfaced by the audit
- `research/INDEX.md` provides cross-corpus topic-to-file discoverability; sub-folder taxonomy is corrected
- The research-adequacy gate at design lock is documented in the design skill and the reviewer agent (skill-text + reviewer-invocation enforcement; no automated gate)
- The reviewer agent carries a topic-to-comparator mandatory-checking table

**Limitation acknowledged**: without CI + docs-lint (Phase 0 out of scope), Phases 8-11 close the *authoring-discipline* and *reviewer-discipline* loops, but cannot mechanically refuse a non-compliant doc at commit time. The full mechanical gate ships when CI infrastructure is in place and Phase 0 is revisited.

The bar moves from "below what the philosophy and skills claim" to "at the bar the philosophy and skills claim" — for the corpus, the skills, and the review process. The runtime implementation is not the subject of this plan.
