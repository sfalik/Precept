---
status: Draft evaluation — 2026-05-25
evaluator: Opus 4.7 (independent)
subject: Precept documentation corpus + skills system as AI-agent knowledge architecture
purpose: Assess whether the corpus + skills enable AI agents to design and build the best possible Precept against the stated philosophy
---

# Documentation Corpus + Skills Evaluation

## Verdict

**Below the bar Precept's own philosophy sets for itself — by a meaningful margin, and unevenly.** The corpus is enormous and largely well-written at the artifact level (the per-stage `compiler/*.md` docs in particular follow a disciplined 16-section template and would be defensible in any compiler project). The catalog system is well theorised in `docs/language/catalog-system.md`, the language spec carries genuine non-negotiable rules at the top, and the lifecycle-skill chain (`lifecycle-1-research` → `lifecycle-6-review`) is a real attempt at end-to-end process discipline rare among AI-agent-driven projects. There is nothing structurally hopeless here. But the corpus exhibits three failure modes that limit what AI agents can produce, two of them serious.

**First — and worst — the foundational documents disagree with each other on observable facts.** The catalog count is "thirteen" in `CLAUDE.md` and `docs/compiler-and-runtime-design.md`, "fourteen" in `docs/contributing/catalog-driven-checklist.md`, and "fifteen" in `docs/language/catalog-system.md`. The root `README.md` advertises an API surface (`PreceptCompiler.Compile`, `eng.CreateInstance`, "five MCP tools") that diverges from `docs/runtime/runtime-api.md` (operation bodies throw `NotImplementedException`) and `docs/tooling/mcp.md` (ten live tools, not five). The `docs/language/README.md` and `docs/language/precept-language-spec.md` both cite `docs/PreceptLanguageDesign.md` as authoritative grounding — that file does not exist. `CLAUDE.md` lists `README-legacy.md` and `docs/DesignNotes-legacy.md` as legacy artifacts — neither exists. An AI agent that obeys CLAUDE.md's "verify against code when status is Implemented" instruction will hit this drift within the first session and cannot tell from the documents alone which number to trust. This is exactly the failure mode the catalog system's philosophy was designed to prevent — yet the philosophy hasn't been applied to the documentation about the philosophy.

**Second — the philosophy fades as you descend.** `docs/philosophy.md` is genuinely strong as a foundational document. But the seven core commitments do not all propagate to the docs that implement them. "Approximation honesty" is referenced 2 times in `primitive-types.md` but **zero times** in `business-domain-types.md`, `temporal-type-system.md`, `runtime-api.md`, or `type-checker.md` — the exact docs where the commitment must be operationally defended. "Primary author is the domain expert" appears **zero times** in `precept-language-spec.md`, `runtime-api.md`, or `language-server.md`. Stateless precepts as "first-class" is well-traced through spec and design docs, but the runtime API doc still treats stateful as the canonical case with stateless as an `[Open Question]`. A fresh AI agent reading the spec will derive constraints; reading the runtime docs, they will not derive the same constraints.

**Third — the skill system carries genuine forcing-function ambition but lacks the discovery scaffolding to make first-session agents successful.** There is no "how to read this project's docs" entry document, no glossary (qualifier, modifier, construct, slot, ConstructMeta, SlotValue, descriptor, fault, diagnostic, catalog member, catalog DU — these are all load-bearing and none has a canonical one-paragraph definition any agent can grep for), no first-time-walkthrough, no anti-pattern index, and no decision-history index outside the in-flight `docs/Working/Archive/`. The required reading list (philosophy.md, docs/README.md, docs/language/README.md) is the right size — but the agent must then assemble the rest from scratch every session. The corpus is set up for a developer who has been onboarded by a human; it is not set up for a stateless agent who walks in cold.

These are fixable. The strengths — disciplined stage docs, lifecycle-skill chain, philosophy.md as a real grounding document, catalog-system.md as the architectural keystone — are the right foundations. The gaps are structural and concrete enough to name.

## Philosophy: how well does it ground the project?

`docs/philosophy.md` is doing real work. It is 100 lines of dense prose, ambitious enough to be load-bearing, written carefully enough that an agent can extract design constraints (the seven principles are reusable as a checklist; the positioning section against Salesforce/Guidewire/FluentValidation/XState gives an agent a category vocabulary; the "what makes it different" section is concrete enough to constrain feature decisions). The hierarchy of concepts (data > rules > states) is genuinely useful as a tie-breaker when proposed designs treat states as primary. The "Who authors a precept" section is unusually direct and gives the agent a real audience to optimize for.

But three structural weaknesses limit it as the grounding document the project treats it as:

1. **No "what does it mean operationally to deliver Principle N" sections.** Each of the seven commitments is stated as a property of the product. None is unpacked into "concretely, here is what a design must demonstrate to claim it serves this commitment." For "prevention not detection," what does an agent test? For "honesty about approximation," what type-system property must hold? For "domain expert primary author," what readability invariant constrains language surface decisions? The lifecycle-2-design skill demands that designs address each commitment, but the philosophy itself doesn't tell the agent *how*. The result is that "Philosophy Alignment" sections in design docs (visible in `docs/Working/`) are uneven and sometimes superficial.

2. **No "tension map" between commitments.** The seven principles look harmonious on the page but in practice trade against each other. "Compile-time structural checking" tensions "primary author is the domain expert" — the more the compiler proves, the more error messages the domain expert must understand. "One file, complete rules" tensions "honesty about approximation" — if approximation is opt-in via type choice, what happens when the domain expert chooses `number` without realising the implications? The philosophy doesn't name these tensions, so the agent cannot evaluate a design against the tradeoff frontier. Compare TC39's Process document, which explicitly enumerates how scope, simplicity, and ecosystem fit are routinely traded.

3. **The non-negotiable warning ("Do not edit philosophy.md") is the *only* enforcement mechanism for philosophy drift.** There is no test, no skill that lints downstream docs against philosophy commitments, no automation that flags a runtime doc claiming approximation behavior the philosophy excludes. The doc is treated as if its physical immutability is sufficient. It isn't — the gap shows up downstream as commitments fading from operational docs (see Findings F-7, F-8).

The philosophy is the corpus's strongest single asset. With the three additions above, it could be the load-bearing keystone the project treats it as. Without them, it remains aspirational at the boundary where it meets implementation.

## Corpus structure: factoring, completeness, coherence

The folder factoring is sensible: `language/` (surface), `compiler/` (pipeline stages), `runtime/` (post-compile execution), `tooling/` (consumer surfaces), `contributing/` (process). Each has a README. The 16-section stage-doc template in `docs/compiler/` is a real discipline. Catalog-system.md, primitive-types.md, temporal-type-system.md, and business-domain-types.md form a coherent type-system sub-corpus.

But three structural issues:

1. **`docs/compiler-and-runtime-design.md` is a 125 KB monolith that contradicts the folder factoring.** It is the unified pipeline-and-runtime design doc, sits at the top of `docs/`, and crosses every boundary the per-area folders draw. It's the single most-referenced doc after philosophy.md. It carries the catalog count "thirteen" (the wrong number — see F-1). It includes the Mermaid pipeline diagram every agent needs. It defines `SemanticIndex`, the Compilation/Precept boundary, and the executable model contract — all of which are *also* defined in per-stage docs. There is no clear answer to "if compiler-and-runtime-design.md and `docs/compiler/type-checker.md` disagree about the SemanticIndex, which wins?" The pointer-philosophy that the project applies to canonical content (CONTRIBUTING.md § "Pointer-philosophy applies to canonical content") is not applied to this doc — it duplicates rather than points.

2. **The runtime/ folder is the weakest area.** Five of its six docs have status Stub or Design. `evaluator.md` (2179 lines, "Implementation Status: pending") and `precept-builder.md` (973 lines, Stub) are massive design proposals masquerading as canonical references. An agent following the README's reading order (runtime-api.md → precept-builder.md → evaluator.md → descriptor-types.md → result-types.md → fault-system.md) will read 4000+ lines of *unimplemented* design before getting to anything that exists. This violates the corpus's own discipline: per CLAUDE.md, "Stub — placeholder; design not yet written" — but `precept-builder.md` is 973 lines, which is not a placeholder.

3. **`docs/Working/` and `docs/Working/Archive/` together carry 50+ design artifacts and the corpus has no index of them.** The Archive contains promoted designs ("**Promoted to:** \<canonical link\>") and historical designs (no replacement). The Working folder mixes draft plans, draft designs, draft reviews, and draft evaluations. The Archive is durable historical material — it ought to be indexed (per-domain decision history) but is not. An agent investigating "why was X decided" must grep Archive blindly. Compare Rust's RFC index (`text/0000-template.md` numbered series with `RFC-NNNN` cross-references) or TC39's `proposals` repository with stages — both make decision archaeology trivial for agents.

The catalog system is the most coherent concept in the corpus: catalog-system.md, the per-pipeline-stage docs, and the catalog-driven-checklist.md form a tight ring. But — and this is critical — they disagree on the catalog count. That single inconsistency cuts against the entire catalog discipline by reducing the agent's trust in the docs that describe the discipline.

## Doc quality at the artifact level

Sampled docs (length-weighted by importance):

- `philosophy.md` (100 lines) — strong, dense, ambitious. Carries the most weight of any single doc.
- `language/precept-language-spec.md` (2088 lines) — the spec is the corpus's most rigorous doc. The §0 Preamble, §0.1 Design Principles (11 numbered principles), §0.5 Graph Analyzer contract, §0.6 Proof Engine contract are all genuinely strong. The principles are operational where philosophy's are aspirational. Status table at the top is clear. This doc is doing what the philosophy doc should do.
- `language/catalog-system.md` (4500+ lines per `wc -l` 191592 bytes, ~3000 lines of substance) — the architectural keystone. Well-written but enormous. The "Catalog count convention (15 total)" footnote literally exists because the doc is fighting its own internal drift. The "this document uses 15 throughout" reads as a band-aid over a problem that should have been resolved.
- `compiler/lexer.md`, `compiler/parser.md`, `compiler/type-checker.md`, `compiler/proof-engine.md` — these follow the 16-section template and are reference-grade. Best per-artifact docs in the corpus. The proof-engine doc at 150 KB / 2532 lines is at the upper end of what an agent can productively scan, but the section navigation table makes it tractable.
- `compiler-and-runtime-design.md` (125 KB) — Mermaid diagrams are excellent. Overlap with per-stage docs is substantial. See structural finding above.
- `runtime/runtime-api.md` (909 lines) — well-organized, but design-only — an agent reading it will produce designs against a runtime that doesn't exist.
- `runtime/evaluator.md`, `runtime/precept-builder.md` — exceed reasonable doc size for design proposals; should be in `docs/Working/`.
- `tooling/language-server.md` (1638 lines) — well-organized; the section-routing in 7.x is clear. Implementation Status section at the bottom is the right pattern.
- `tooling/mcp.md` (414 lines) — clean, recent (2026-05-15 consolidation). Best example of a doc that is honest about what's live vs. planned.
- `contributing/catalog-driven-checklist.md` — operational, practical. But uses "fourteen catalogs" — drift.

### Terminology consistency

Sampled terms across docs:

- **qualifier** — heavily used in `proof-engine.md` (31), `precept-language-spec.md` (26), `catalog-system.md` (35), but appears **0 times in `runtime-api.md`**. This is a load-bearing concept (currency for money, unit for quantity) — an agent working on the runtime API will not develop a precise model of qualifier semantics from runtime docs alone.
- **construct / slot / ConstructMeta / SlotValue** — appears consistently in the catalog/parser/spec triad. But there is no canonical definition of "construct slot" outside `catalog-system.md § Construct Slot Model`. An agent searching "what is a slot" gets fragments. Glossary would solve this.
- **descriptor** — appears in runtime docs as "first-class runtime identity for all declared program elements" but is also used as "TextMate scope descriptor" in tooling-surface.md and "ConstraintDescriptor" in compiler-and-runtime-design.md. The polysemy is real and not flagged anywhere.
- **modifier** — well-defined in catalog-system.md but the spec § 0.1 uses "modifier" to mean specifically value modifiers, while catalog-system.md's `Modifiers` catalog is a DU with five subtypes (field constraints, state lifecycle, event modifiers, access modes, anchors). An agent reading spec then catalog will think they understand "modifier" and be wrong about scope.

### Status field reliability

Status fields are present on every doc and generally accurate. Three exceptions:

- `runtime-api.md` says "Doc maturity: Design — public surface locked" but `Precept.cs` and `Version.cs` operation bodies throw `NotImplementedException`. The status is honest about the doc, ambiguous about the runtime. CLAUDE.md says "if a doc says 'Implemented' but the code disagrees, that's drift" — but neither flavor of "design" status warns the agent that *no underlying implementation exists*.
- `precept-builder.md` says "Status: Stub" but is 973 lines. The Stub category meaning has drifted.
- Several docs use "Locked" / "Locked design" status which is not in the CLAUDE.md / `docs/README.md` doc-status taxonomy ("Implemented / Active", "Canonical design", "Design / Draft", "Stub", "Archived"). The taxonomy is leaking new categories without being updated.

## Context management for AI agents

The three required reads — `docs/philosophy.md` (100 lines), `docs/README.md` (57 lines), `docs/language/README.md` (39 lines) — are the right size and the right three (about 200 lines combined; readable in a single context-cheap pass). The decision to make these always-required is correct.

But the navigation system has gaps:

1. **`docs/README.md` is a route table; it is not a reading order.** It tells the agent where things live but not which to read in what sequence. For a typical task ("design a new modifier"), the agent must figure out: catalog-system.md (the architectural backbone), then precept-language-spec.md § 2.4 Field Modifiers, then catalog-driven-checklist.md, then the relevant stage docs. The README implies this chain but doesn't enumerate it.

2. **Per-area READMEs are inconsistent in helpfulness.** `language/README.md` has a 5-step Reading Order. `compiler/README.md` lists pipeline order but no reading order for cross-cutting docs. `runtime/README.md` has a 6-step Reading Order — but it routes the agent through 4000 lines of unimplemented design. `tooling/README.md` is 19 lines and says "Read `docs/compiler-and-runtime-design.md` §§13–15 first" — without giving the section titles. The variance suggests the README discipline hasn't been audited.

3. **No "first session in this codebase" entrypoint.** A fresh agent has CLAUDE.md (project instructions) and three required reads. That is enough to start, but no doc explains the corpus's organizing concepts — catalog-driven design, the Compilation/Precept boundary, the lifecycle-skill chain, the four-leg decision rationale, the pointer-philosophy. Each of these is implicit in some doc but no single "how this project is structured" doc exists. Rust ships `CONTRIBUTING.md`, `rustc-dev-guide`, and `The Rust Programming Language` book as three distinct on-ramps; Precept has CLAUDE.md doing all three jobs.

4. **The skill-system orchestration is uneven.** `lifecycle-2-design`, `lifecycle-3-plan`, `lifecycle-5-promote`, and `lifecycle-6-review` are dense and well-thought-out. `lifecycle-1-research` is also good. But there is no `lifecycle-4-execute` skill — the execution stage exists in `CONTRIBUTING.md` but has no skill. Stage 7 (`lifecycle-7-audit`) is deferred to Phase 9. The chain has two gaps. An agent doing Stage 4 work has to derive process discipline from `CONTRIBUTING.md` and CLAUDE.md combined, not from a skill the way Stages 1–3 are scaffolded.

5. **The `precept-author` agent (32 KB) and `precept-reviewer` agent (16 KB) overlap substantially with the lifecycle skills.** Both agents include their own "Required reading" blocks that duplicate the three required reads, then duplicate parts of CLAUDE.md and parts of catalog discipline. An agent invoking the precept-reviewer sub-agent reads the three required reads twice. The duplication is context-cost — small for one session, real over many.

## Alignment from philosophy to implementation docs

For each of the seven core philosophy commitments, I traced philosophy → spec → catalog/architecture → individual feature docs:

| Commitment | Philosophy | Spec § | Catalog/architecture | Individual feature docs | Signal strength |
|---|---|---|---|---|---|
| **Prevention not detection** | Strong | Strong (§0.1 P1) | Strong (proof-engine.md) | Strong (type-checker.md fault-prevention chain) | Strong throughout |
| **One file complete rules** | Strong | Strong (§0.1 P2) | Implicit | Implicit (no doc states "no cross-file references") | Fades after spec |
| **Determinism** | Strong | Strong (§0.1 P3) | Strong (catalog-system) | Strong (lexer/parser deterministic-by-construction) | Strong throughout |
| **Full inspectability** | Strong | Strong (§0.1 P4) | Partial (catalog enumeration) | Weak — runtime-api.md design has Inspection sections but no "inspectability invariant" | Fades at runtime boundary |
| **Compile-time structural checking** | Strong | Strong (§0.1 P7, P10, P11) | Strong (proof-engine.md) | Strong (graph-analyzer.md) | Strong throughout |
| **Honesty about approximation** | Strong (philosophy.md §) | Weak (§0.1 P8 single para) | Absent from catalog-system.md | **Zero hits in business-domain-types.md, temporal-type-system.md, runtime-api.md, type-checker.md** | Fades immediately |
| **Stateless first-class** | Strong | Strong (§0.2, §3A.4, §3A.5) | Partial (no catalog entry mentioning stateless) | Partial (runtime-api.md has open question for stateless CreateInitialVersion) | Fades at runtime |
| **Domain expert primary author** | Strong | Weak | Absent | **Zero hits in spec, runtime-api.md, language-server.md** | Fades immediately |

The two commitments that fade fastest — *approximation honesty* and *domain expert as primary author* — are precisely the two that most distinguish Precept from comparable systems. Approximation honesty is what separates Precept from "yet another typed DSL"; domain-expert primacy is what separates it from "yet another developer tool." If these commitments are not operationalized through the entire stack, the project ships as something narrower than its philosophy promises — and an agent making decisions in the spec or runtime layer cannot prevent that drift on their own.

**Inspectability** also fades at the runtime boundary, but more recoverably — the runtime-api.md does have Inspection sections, just no "inspectability invariant" stated as a constraint on every decision.

## Skills as gateway

The lifecycle skills are the corpus's structural ambition. They are doing real work — the four-leg rationale, citation-with-excerpt, doc-touch enumeration, sources-consulted aggregation. These are forcing functions a typical project doesn't have. `lifecycle-2-design`'s "no Locked status without [N requirements]" rules are genuinely valuable.

But three structural issues:

1. **The skills are advisory, not enforcing.** The previous evaluation (`docs/Working/skills-rigor-evaluation-opus.md`) already documented this with in-tree evidence (`field-never-set-diagnostic.md` locked without any required sections). I'll note it only: until a lint pass exists, the discipline is rhetorical. This is the most important single observation about the skill system.

2. **The skills are agent-targeted but written as if for humans.** They use second-person prose ("Read these before writing any section"), narrative explanations of why discipline matters, and behavioral guards stated as "refuses to do X". An agent reads these and forms an intent; whether the intent translates to behavior depends on whether the agent re-reads the skill at the right moment. The skills could be more agent-friendly with explicit checklists, decision trees, and stop-points written as imperative actions rather than principles.

3. **The skill discovery is implicit.** Skills are listed in system reminders, but the relationship between skills is not documented. There is no `docs/skills.md` (or equivalent) that maps the lifecycle chain visually, names which skill consumes which other skill's output, and explains the dual-track (issue+PR vs spike-branch). The `CONTRIBUTING.md § Doc Lifecycle` table is the closest thing but doesn't appear in the always-required reads. A fresh agent encountering a "design" task may invoke `/lifecycle-2-design` (correct) or skip directly to writing a design (incorrect) — the corpus doesn't decisively route.

4. **There is no `precept-author`/`precept-reviewer` ↔ lifecycle skill alignment matrix.** Both agents and the skills carry overlapping but not identical sets of rules. An agent invoked as `precept-reviewer` reads `CLAUDE.md` plus its own embedded rules plus (via subprocess to lifecycle-2-design output) the four-leg rule plus citation-with-excerpt. The matrix is not documented; redundancy and conflict between agent and skill is not flagged anywhere.

## Findings

### F-1: Catalog count contradicts itself across foundational docs
**Category:** QUALITY  
**Severity:** CRITICAL  
**Comparison:** Rust's `rust-lang/reference` and `rustc-dev-guide` cross-cite specific RFC numbers; TC39 cross-cites stage numbers — the canonical count of a load-bearing concept is unambiguous.

**What:** The number of catalogs in Precept's catalog system is stated three different ways in foundational docs.  
**Why it matters for AI-agent work:** Catalogs are the canonical architectural primitive — the spec in machine-readable form. An agent reading "thirteen" in CLAUDE.md, "fourteen" in catalog-driven-checklist.md, and "fifteen" in catalog-system.md cannot tell which to trust. Worse, the agent may propagate the wrong count into a design doc, which then carries a wrong fact past every review gate. The catalog-system.md doc itself acknowledges the drift with a footnote ("Some readers count only the language-surface 12+2 = 14; others include the tooling-adjacent SemanticTokenTypes for 15") — that is a workaround for a problem that should not exist.  
**Evidence:** `CLAUDE.md` § Catalog System: "metadata-driven architecture" (no count); `docs/compiler-and-runtime-design.md:104` "thirteen catalogs"; `docs/compiler-and-runtime-design.md:108` "The thirteen catalogs fall into two groups"; `docs/contributing/catalog-driven-checklist.md:17` "Fourteen catalogs cover the complete language surface"; `docs/language/catalog-system.md:13` "Catalog count convention (15 total)" with explicit footnote acknowledging the 14-vs-15 ambiguity.  
**Recommendation:** Pick one count and enforce it everywhere in a single edit pass. The catalog-system.md doc is the source of truth (per its own claim); if the answer is 15, update `CLAUDE.md`, `compiler-and-runtime-design.md`, `contributing/catalog-driven-checklist.md`, and any other reference in one PR. Add a drift-detection test that compares the count statements in these docs and fails CI if they disagree. The catalog system was built to make this class of drift mechanically impossible *in code* — extend the discipline to the docs about catalogs.

### F-2: README.md ships aspirational API claims that contradict every other doc
**Category:** QUALITY  
**Severity:** CRITICAL  
**Comparison:** Stripe's docs.stripe.com is famously honest about what's live vs. planned; `docs.stripe.com` never claims an unshipped API as if it exists.

**What:** `README.md` (the public face of the project, also the load-bearing on-ramp for new agents) advertises an API surface that does not exist.  
**Why it matters for AI-agent work:** A fresh agent told "look at the README to understand the project" will produce code targeting `PreceptCompiler.Compile(def)` and `eng.CreateInstance(state, data)` — neither of which exist (per `runtime-api.md`: "all operation bodies throw `NotImplementedException`"). It will reference "five MCP tools" — but `tooling/mcp.md` documents ten live tools. It will link to `docs/RuntimeApiDesign.md` — that file does not exist (the actual location is `docs/runtime/runtime-api.md`).  
**Evidence:** `README.md:62-65` (code sample with non-existent API); `README.md:84` ("five MCP tools"); `README.md:96` ("docs/RuntimeApiDesign.md"); cross-reference: `tooling/mcp.md:23` ("Live tool surface" lists ten tools); `runtime/runtime-api.md:8` ("Implementation state: Partial stub (`Precept.cs` and `Version.cs` exist; all operation bodies throw `NotImplementedException`)").  
**Recommendation:** Rewrite README's Quick Example and Getting Started to use only APIs the runtime actually exposes today, or mark the example clearly as "designed API, ships in vNext." Update the MCP tool count to ten. Fix the `docs/RuntimeApiDesign.md` link. Add a CI check that grep-finds references to non-existent files in `README.md` and `CLAUDE.md`.

### F-3: Foundational docs reference files that don't exist
**Category:** QUALITY  
**Severity:** MAJOR  
**Comparison:** Linux kernel `Documentation/` tree has strict cross-reference discipline; `make htmldocs` errors on dangling links.

**What:** `CLAUDE.md`, `docs/language/README.md`, `docs/language/precept-language-spec.md`, and `research/README.md` all reference files that do not exist in the tree.  
**Why it matters for AI-agent work:** An agent following the doc trail will hit dangling references. CLAUDE.md's reliability as the index of indexes is undermined when its own listed files are missing.  
**Evidence:** `CLAUDE.md:140` references `README-legacy.md` and `docs/DesignNotes-legacy.md` — neither exists at the repo or under `docs/`. `docs/language/README.md:36` references `docs/PreceptLanguageDesign.md` as "v1 implemented language spec" — does not exist. `docs/language/precept-language-spec.md:11` cites `docs/PreceptLanguageDesign.md` as Grounding — does not exist. `research/README.md:48` references `docs/PreceptLanguageDesign.md` as "the DSL spec that this research informs" — does not exist.  
**Recommendation:** Run a one-time link audit (`grep -rn "docs/[A-Za-z]" docs/ CLAUDE.md README.md CONTRIBUTING.md` and verify each path resolves). Update CLAUDE.md's legacy-files note: either the files were removed (delete the reference) or moved (update the reference). If `PreceptLanguageDesign.md` was archived to `docs/archive/` (likely), update all four citing locations. Add to lint: dangling-reference detection in `docs/Working/` and CLAUDE.md.

### F-4: Doc status taxonomy has leaked new categories without being updated
**Category:** QUALITY  
**Severity:** MAJOR  
**Comparison:** Swift Evolution uses a strict status set (`accepted`, `active review`, `deferred`, `rejected`, `returned`, `withdrawn`) — every proposal carries one; the set is documented; new categories require explicit process change.

**What:** The doc-status taxonomy declared in `docs/README.md` and `CLAUDE.md` is `Implemented / Active`, `Canonical design`, `Design / Draft`, `Stub`, `Archived` — five values. Actual docs use additional values: `Locked design — approved for spike/Precept-V2`, `Locked (Decision #17 — rewritten 2026-04-17)`, `Incremental`, `Full`, `Partial stub`, `Locked YYYY-MM-DD`, `Promoted to`. These are accumulating without being added to the canonical taxonomy.  
**Why it matters for AI-agent work:** CLAUDE.md tells the agent: "Trust [status fields] for routing; verify against code when status is 'Implemented'." But the agent encounters statuses outside the documented set with no routing guidance. "Locked" — does that mean implementation can proceed? "Partial stub" — does that mean read or skip? "Promoted to" — is the doc still authoritative or archived? An agent making routing decisions cannot rely on the status field if the taxonomy isn't comprehensive.  
**Evidence:** `docs/README.md:22-30` (five-value taxonomy); `docs/runtime/runtime-api.md:7` (status "Design — public surface locked"); `docs/runtime/runtime-api.md:8` (status "Partial stub"); `docs/language/precept-language-spec.md:9` ("Incremental"); `docs/compiler/lexer.md:7` ("Full"); `docs/language/collection-types.md:763` ("Locked design — approved for `spike/Precept-V2`"); `docs/Working/Archive/diagnostic-enforcement.md` ("Promoted to: ..."); `lifecycle-2-design/SKILL.md:55` ("status: Locked YYYY-MM-DD").  
**Recommendation:** Extend the canonical taxonomy in `docs/README.md` and `CLAUDE.md` to include all in-use values, OR refactor the in-use values to the existing taxonomy. Document each value's meaning in one line: when an agent should trust it, when an agent should verify. Add to `lifecycle-2-design` the obligation to use a taxonomy-listed value.

### F-5: docs/compiler-and-runtime-design.md is a monolith that duplicates rather than points
**Category:** STRUCTURE  
**Severity:** MAJOR  
**Comparison:** Bazel's documentation site separates the language reference (`build-ref.md`), build encyclopedia (`be-overview.md`), and rules documentation — each pointed-to, not duplicated; updates land in one place.

**What:** `docs/compiler-and-runtime-design.md` is 125 KB / ~3000 lines covering pipeline + runtime + tooling integration. Same material is duplicated in `docs/compiler/*.md` (per-stage), `docs/runtime/*.md` (per-component), `docs/tooling/*.md`. The doc carries non-negotiable rules and Mermaid diagrams that no other doc has — but the rest is duplication.  
**Why it matters for AI-agent work:** When two docs disagree (e.g., the catalog count "thirteen" in this doc vs. "fifteen" in catalog-system.md), the agent has no resolution rule. CLAUDE.md routes work-touches to per-area docs but this doc straddles every area. An agent updating a per-stage doc must also remember to update this monolith — or accept that the monolith drifts. Every new agent reads it because it's referenced everywhere; that's 125 KB of context cost per session that overlaps with the more authoritative per-area docs.  
**Evidence:** Compare `docs/compiler-and-runtime-design.md` §§4-10 (per-stage descriptions) against `docs/compiler/lexer.md`, `parser.md`, `name-binder.md`, `type-checker.md`, `graph-analyzer.md`, `proof-engine.md` — substantial overlap, often verbatim concept restatement. The catalog count discrepancy (F-1) literally appears here.  
**Recommendation:** Convert `compiler-and-runtime-design.md` to a pointer-philosophy hub doc: keep the Mermaid pipeline diagram, non-negotiable rules, and the artifact-flow narrative (the one thing it does that no per-area doc does); replace per-stage sections with one-paragraph summaries plus pointers (`→ docs/compiler/lexer.md`). Target size: 30 KB, not 125 KB. The 95 KB removed is duplication and is the source of every drift between this doc and the per-area docs.

### F-6: runtime/ folder treats design proposals as canonical references
**Category:** STRUCTURE  
**Severity:** MAJOR  
**Comparison:** Rust's stdlib docs (`std::*`) are reference docs for shipped code; RFC documents for unshipped work live in `rust-lang/rfcs`. The two are not blended.

**What:** `docs/runtime/` contains six docs. `runtime-api.md` (909 lines, "Design — public surface locked"), `evaluator.md` (2179 lines, "Stub"), `precept-builder.md` (973 lines, "Stub"), `result-types.md` (449 lines, "Design"), `fault-system.md` (351 lines, "Draft"), `descriptor-types.md` (206 lines, "Stub"). Total: ~5000 lines of design material in a folder that READMEs route agents to as canonical references.  
**Why it matters for AI-agent work:** An agent told "read runtime/ for the runtime contract" reads 5000 lines of designs against unshipped code, none of which will run today. When the runtime ships, every one of these docs will need substantial revision — but the structure suggests they're stable. Worse, an agent designing something that interacts with the runtime will design against the doc, not against code (which throws NotImplementedException), producing a design that may not survive contact with implementation.  
**Evidence:** `runtime-api.md:8` ("Implementation state: Partial stub (`Precept.cs` and `Version.cs` exist; all operation bodies throw `NotImplementedException`)"); the reading order in `runtime/README.md:17-23` routes through all six unimplemented docs.  
**Recommendation:** Move runtime design proposals that are pre-implementation to `docs/Working/runtime/` (with the rest of the in-flight design work). Keep `docs/runtime/` for canonical references to the *shipped* runtime — even if that means `docs/runtime/` is sparse until the runtime ships. Add a "this surface is not yet implemented" banner to docs that remain. The pointer-philosophy applies: until the runtime ships, the runtime folder is a stub with pointers to Working/, not a reference.

### F-7: Approximation honesty fades to zero in operational type docs
**Category:** ALIGNMENT  
**Severity:** MAJOR  
**Comparison:** Dhall's manual carries termination as a first-class invariant through every type doc; the philosophy ("Dhall is total") is operationalized as a property each type must satisfy.

**What:** Philosophy.md commits explicitly to "Honesty about approximation": "Precept does not present approximation as exactness... the line between exact and approximate behavior must be visible in the type system and the language surface." This commitment appears 0 times in `business-domain-types.md`, 0 times in `temporal-type-system.md`, 0 times in `runtime-api.md`, 0 times in `type-checker.md`. The only operational engagement with approximation honesty is in `primitive-types.md` (2 mentions), where the three numeric lanes are discussed.  
**Why it matters for AI-agent work:** An agent designing a new business-domain type (currency, quantity, price) has no anchor in the type doc that constrains the design against approximation drift. The proof engine doc carries the obligation set, but the type doc that *introduces* the type doesn't say "this type is exact (or approximate) because [philosophy]." Future types added to business-domain-types.md by an agent following the doc's existing pattern will inherit the philosophy gap. Approximation honesty becomes a property the project claims rather than a constraint future designs must respect.  
**Evidence:** `grep -c approximation docs/language/business-domain-types.md` returns 0; same for `temporal-type-system.md`, `runtime-api.md`, `type-checker.md`. `docs/philosophy.md:23-26` (the commitment in full prose). `docs/language/primitive-types.md` mentions approximation in the three-lane discussion — the only operational engagement.  
**Recommendation:** Add a "Approximation Stance" section to every type doc (`primitive-types.md`, `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md`), stating one of: "this type family is exact; values are guaranteed to round-trip without loss" / "this type family admits approximation; the cases are X, Y, Z" / "this type family is approximate by domain (e.g., `number`)". The section is mechanical (5 lines per doc) but anchors the commitment operationally. Add to `lifecycle-2-design` the obligation to state the Approximation Stance for any new type.

### F-8: Domain-expert primacy is a philosophy commitment with zero operational anchors
**Category:** ALIGNMENT  
**Severity:** MAJOR  
**Comparison:** TLA+ documentation foregrounds "the spec is read by humans who reason about the system" through every doc; every notation choice is defended on legibility grounds.

**What:** Philosophy.md § "Who authors a precept" commits the primary author to be a domain expert / business analyst, not a software developer. This commitment appears 0 times in `precept-language-spec.md`, 0 times in `runtime-api.md`, 0 times in `language-server.md`.  
**Why it matters for AI-agent work:** The audience claim is what justifies many language-surface decisions: keyword-anchored grammar (P5), mandatory `because` clauses (P9), readable diagnostics, the rejection of opaque solvers (philosophy of proof engine), the lack of iteration constructs. Without operational anchors, an agent designing a new construct will optimize for compiler convenience or AI-author preference, not domain-expert readability — and the corpus has no forcing function to call that out. Compare the dense semantic notation of e.g. CUE or Dhall manuals: they assume PL-literate readers; their philosophy reflects that audience. Precept claims a different audience but does not constrain doc or design decisions accordingly.  
**Evidence:** `grep -c "domain expert" docs/language/precept-language-spec.md docs/runtime/runtime-api.md docs/tooling/language-server.md` all return 0. `docs/philosophy.md § Who authors a precept` (the full commitment). `precept-language-spec.md § 0.1 Principle 5` (Keyword-anchored readability) — the closest operational anchor, but it doesn't tie back to the audience commitment.  
**Recommendation:** Add to `precept-language-spec.md § 0` a sub-section "Authoring Audience" that restates the philosophy commitment and lists 3-5 operational implications (keyword-anchored grammar, mandatory rationale, readable diagnostics, no opaque proof, no iteration). Add to `lifecycle-2-design` Philosophy Alignment: "Does this design assume a developer-level reader for the language surface, or a domain-expert reader? State explicitly and justify." Diagnostic messages and hover text should be reviewed against the audience commitment — language-server.md should reference the audience as a constraint.

### F-9: No glossary; load-bearing terms are diffuse
**Category:** GAP  
**Severity:** MAJOR  
**Comparison:** Stripe's API reference has a glossary; MDN has term definitions; TLA+ ships a precise notation glossary. A corpus with this many domain-specific terms cannot rely on inference.

**What:** Precept introduces 30+ load-bearing terms (catalog, catalog member, modifier, qualifier, construct, slot, accessor, descriptor, action, outcome, ensure, rule, guard, transition row, configuration, version, precept, plan, fault, diagnostic, ProofRequirement, ProofSubject, ConstructMeta, SlotValue, SemanticIndex, Compilation, etc.) — none has a single canonical one-paragraph definition reachable by grep.  
**Why it matters for AI-agent work:** Term disambiguation costs the agent every session. "Qualifier" appears in catalog-system.md (catalog member qualifier propagation) and in type docs (`money in 'USD'` — currency is a qualifier) and in proof-engine.md (qualifier compatibility) — three different scopes for one word. "Descriptor" similarly polysemous. Without a glossary, the agent infers, sometimes wrongly.  
**Evidence:** No `docs/glossary.md` exists. `grep -nE "^### |^#### " docs/language/catalog-system.md | grep -iE "qualifier"` returns the qualifier sub-sections but no "definition of qualifier" anchor. Similarly for "construct," "descriptor," "slot."  
**Recommendation:** Add `docs/glossary.md` — one paragraph per term, with cross-references to the docs that elaborate. Target: ~30-40 terms, ~3-4 pages total. Add to the always-required reads when an agent's task touches the terminology surface. The cost to add is low; the cost to an agent of looking up "qualifier" via grep every session is real.

### F-10: No "how to read this project" entry document
**Category:** GAP  
**Severity:** MAJOR  
**Comparison:** Rust ships rustc-dev-guide as "this is how the compiler is organized, here's how to navigate the source"; TC39 has CONTRIBUTING.md plus the process document; both define the on-ramp explicitly.

**What:** A fresh AI agent has three required reads (philosophy.md, docs/README.md, docs/language/README.md), CLAUDE.md, and the implicit doc-status taxonomy. Nothing exists that says "this is how the project is organized, here are the four organizing concepts (catalogs, pipeline, runtime, tooling), here is the lifecycle, here is what makes Precept's docs different from a typical project."  
**Why it matters for AI-agent work:** Every fresh agent rediscovers the organizing concepts from fragments. The discovery work compounds across sessions and across agents.  
**Evidence:** `docs/README.md` is 57 lines and is a route table, not an explainer. CLAUDE.md is 249 lines and mixes architecture, philosophy, catalog rules, doc-sync rules, build commands, MCP config — it is dense but not a guided introduction.  
**Recommendation:** Add `docs/agent-onboarding.md` (or `docs/reading-guide.md`): ~200 lines, structured as "Concept 1: Catalogs as language spec," "Concept 2: Pipeline-Compilation-Precept-Version chain," "Concept 3: Lifecycle-driven design," "Concept 4: Pointer-philosophy and doc-sync," "Concept 5: Always-required vs context-on-demand reads." Each concept gets a paragraph plus a pointer to the canonical doc that elaborates. List as fourth always-required read in CLAUDE.md.

### F-11: Skill discipline is rhetorical (no lint), already documented in skills-rigor evaluation
**Category:** SKILLS  
**Severity:** CRITICAL  
**Comparison:** Rust RFC discipline is enforced by FCP team review; TC39 stage advancement is a vote — both have mechanical gates the skill system here lacks.

**What:** The lifecycle skills say "refuses to lock without N" multiple times but cannot enforce. (Detailed in `docs/Working/skills-rigor-evaluation-opus.md`.) Carried here only to note it as the largest single ceiling on the skill system's value.  
**Why it matters for AI-agent work:** All downstream rigor (four-leg rationale, citation-with-excerpt, doc-touch enumeration) is forfeited the moment a Locked status can be applied without those checks. Agents will internalize the rhetorical level as the actual bar.  
**Evidence:** Previous evaluation, F-1 in that doc.  
**Recommendation:** As in previous evaluation — ship a `lifecycle-2-lint` companion. Not redundant with this evaluation; the doc-corpus side needs the same forcing function (a doc-corpus lint that catches catalog count drift, dangling references, status-taxonomy violations, etc.).

### F-12: Skill list omits Stage 4 (Execute); chain has gaps
**Category:** SKILLS  
**Severity:** MODERATE  
**Comparison:** TC39 has explicit stages 0-4 each with associated process artifacts; the chain is unbroken.

**What:** Lifecycle skills exist for Stages 1, 2, 3, 5, 6. Stage 4 (Execute) has no skill. Stage 7 (Audit) is deferred. The visible chain in CONTRIBUTING.md has 7 stages, but only 5 are scaffolded by skills.  
**Why it matters for AI-agent work:** An agent in the execution stage has to fall back on CLAUDE.md, CONTRIBUTING.md, precept-author/reviewer agents, and the design doc — without a skill that says "here's how to execute against a Stage 3 plan, here's how to maintain the PR body, here's how to commit vertical slices." Stage 4 is where most actual work happens; it is the under-supported stage.  
**Evidence:** `.claude/skills/` lists lifecycle-1, lifecycle-2, lifecycle-3, lifecycle-5, lifecycle-6 — no lifecycle-4. `CONTRIBUTING.md § Doc Lifecycle` table shows Stage 4 with skill column "— (engineering work)" but the other stages have skills.  
**Recommendation:** Add `lifecycle-4-execute` skill that captures: vertical-slice discipline, PR-body update protocol, commit-message format, post-slice doc-touch verification, and the runtime-MCP-LS-grammar update reminder per CLAUDE.md doc-sync rules. Even a thin skill is better than the gap.

### F-13: Per-area READMEs have inconsistent helpfulness
**Category:** STRUCTURE  
**Severity:** MODERATE  
**Comparison:** Linux kernel `Documentation/<subsystem>/index.rst` follows a consistent template; Bazel doc subsections follow a consistent template.

**What:** `docs/language/README.md` has a 5-step Reading Order. `docs/runtime/README.md` has a 6-step Reading Order. `docs/compiler/README.md` lists pipeline order but no reading order for cross-cutting docs (diagnostic-system, literal-system, tooling-surface, grammar-generator). `docs/tooling/README.md` is 19 lines and says only "Read `docs/compiler-and-runtime-design.md` §§13–15 first." The variance is structural.  
**Why it matters for AI-agent work:** An agent navigating the corpus expects the per-area READMEs to give consistent guidance. Right now they don't — some are 5-step reading orders, some are sparse pointers. The agent has to figure out which README to trust how.  
**Evidence:** See file sizes and content above; this is a direct artifact-level observation.  
**Recommendation:** Adopt a canonical sub-area README template: Documents table (path, purpose, status), Reading Order (numbered), Relationship to Other Docs, Cross-cutting concerns (if any). Apply uniformly to language, compiler, runtime, tooling. The current `docs/language/README.md` is the closest to the right shape.

### F-14: precept-author and precept-reviewer agents duplicate substantial content
**Category:** CONTEXT  
**Severity:** MODERATE  
**Comparison:** Claude Code agent patterns elsewhere typically delegate to a shared knowledge layer; agent prompts focus on role-specific instructions, not project-wide rules.

**What:** `precept-author.md` (32 KB) and `precept-reviewer.md` (16 KB) both contain "Required reading before X" blocks listing the same three required reads, both restate parts of CLAUDE.md's catalog rules, both restate documentation-sync obligations. When a parent session invokes the reviewer, the agent re-reads philosophy.md, docs/README.md, docs/language/README.md — even though the parent has already read them.  
**Why it matters for AI-agent work:** Context cost per agent invocation. Over a long session with multiple sub-agent calls, the duplication is a measurable fraction of total tokens. More structurally — when the rules change in CLAUDE.md, three docs need updating (CLAUDE, author, reviewer); easy to miss one.  
**Evidence:** Compare `precept-author.md:13-25` vs `precept-reviewer.md:11-20` (required reading sections); both expand on philosophy.md commitments inline.  
**Recommendation:** Extract a shared "project ground rules" doc (or link to a section of CLAUDE.md) that both agents cite by reference rather than restate. The agents become thinner; rules update in one place. Alternatively: include the project-rules content only in CLAUDE.md, and have the agents say "Read CLAUDE.md plus these role-specific additions."

### F-15: Working/Archive has no decision-history index
**Category:** GAP  
**Severity:** MODERATE  
**Comparison:** Rust RFCs are numbered and indexed; you can navigate `text/0001-...` through `text/9999-...` chronologically or by topic.

**What:** `docs/Working/Archive/` contains 46 design docs, many "Promoted to: <canonical>" with durable historical rationale. No index. An agent investigating "why was decision X made" must grep blindly.  
**Why it matters for AI-agent work:** Decision archaeology is a recurring need (rationale for renames, reasons for past designs being rejected, why a certain approach was abandoned). Without an index, the agent burns context on grep. Worse, the agent may miss prior decisions and propose something that's already been rejected.  
**Evidence:** No `docs/Working/Archive/README.md`. `ls docs/Working/Archive/` returns 46 unsorted markdown files.  
**Recommendation:** Add `docs/Working/Archive/README.md` as a chronological + topical index of archived designs. Each entry: filename, date, topic, promoted-to canonical, one-line outcome ("retired Writable keyword, see field-never-set-diagnostic"). Maintenance burden is the `/lifecycle-5-promote` skill — add an obligation to update the Archive index when archiving.

### F-16: catalog-system.md is 4500+ lines; AI-agent scan cost is real
**Category:** QUALITY  
**Severity:** MODERATE  
**Comparison:** ECMA-262 is a long spec, but its TOC + section anchoring is precise; a competent reader navigates in O(1) per concept. Rust Reference uses similar structuring.

**What:** `docs/language/catalog-system.md` is the architectural keystone but is 4500+ lines. The TOC is comprehensive but the doc is sprawling — it contains the catalog count discussion, the 15-catalog inventory, the schema anatomy, every catalog's member inventory, the Roslyn enforcement layer, qualifier propagation, proof obligations, construct slot model, syntax reference, and pipeline stage impact patterns. An agent looking up "how do I add a new catalog member" must scan substantial material.  
**Why it matters for AI-agent work:** Every agent that touches catalogs reads this doc. Its size compounds across sessions. The TOC helps but doesn't fix the size.  
**Evidence:** File is 191 KB / 4500+ lines per `wc -l`.  
**Recommendation:** Split catalog-system.md into three docs: `catalog-system-architecture.md` (the principle, the pattern definition, the Roslyn enforcement layer — the design-level material); `catalog-inventory.md` (the 15 catalogs, their member inventories — the reference-level material); `catalog-integration.md` (per-pipeline-stage integration patterns, qualifier propagation, proof obligations — the consumer-level material). Cross-link. Each doc lands at 1500 lines or less. Architectural reads architecture; consumer reads consumer.

### F-17: No anti-pattern catalog beyond MCP precept_patterns
**Category:** GAP  
**Severity:** MINOR  
**Comparison:** Bazel's "Best practices" + "Common pitfalls" pair; React's "Rules of React" doc.

**What:** The `precept_patterns` MCP tool returns DSL-level patterns and anti-patterns. There is no equivalent at the **language-design** level — a doc that says "here are the anti-patterns in *designing* Precept itself" (e.g., "switching on `*Kind` enum identity," "hand-editing tmLanguage.json," "scattering domain knowledge across pipeline stages"). Some of these appear in `contributing/catalog-driven-checklist.md` as "Red flags" — but only for the catalog discipline. Other anti-patterns (e.g., parser anti-patterns, runtime anti-patterns) are scattered.  
**Why it matters for AI-agent work:** An agent designing language surface or pipeline code will encounter the same anti-patterns repeatedly. A single catalog of "things not to do, with examples and rationale" is faster than re-deriving them from per-doc principles.  
**Evidence:** No `docs/anti-patterns.md` exists. Anti-pattern material is split across `catalog-system.md § Pattern Definition`, `contributing/catalog-driven-checklist.md § Red flags`, `compiler-and-runtime-design.md § Anti-pattern` (one specific anti-pattern: hand-edited grammar), per-stage docs scattered.  
**Recommendation:** Consolidate anti-patterns into `docs/contributing/anti-patterns.md`. Organize by layer (catalog, parser, type checker, proof engine, runtime, language server, MCP). Each entry: pattern, why it's wrong, the principle violated, the correct alternative. ~30-40 entries. Linkable.

### F-18: tooling/README.md is too sparse to route an agent
**Category:** STRUCTURE  
**Severity:** MINOR  

**What:** `docs/tooling/README.md` is 19 lines. It does not summarize each tooling surface, does not define what an agent should read for each, does not flag implementation states.  
**Why it matters for AI-agent work:** An agent doing MCP work or language-server work cannot triage from this README. Has to open all three doc files to figure out where the work touches.  
**Evidence:** File is 19 lines including frontmatter. Compare `docs/language/README.md` (38 lines, more useful).  
**Recommendation:** Expand to ~50-80 lines with: Documents table (status, source, doc maturity), Reading Order, Relationship to compiler/runtime artifacts, Common tooling tasks (when to update each surface).

## What the corpus does well

These are genuine strengths — do not disrupt them in pursuit of improvements:

- **The lifecycle-skill chain is real engineering process discipline**, rare among AI-agent-driven projects. Even imperfectly enforced, it raises the design bar significantly.
- **The 16-section stage-doc template** in `docs/compiler/` produces consistently navigable per-stage docs. The discipline is uniform.
- **`docs/philosophy.md` is a strong foundational document.** Its commitments are specific enough to ground decisions and ambitious enough to set the bar.
- **The catalog-driven architecture is well theorised** in `catalog-system.md`. The concept is genuinely novel and the doc engages with it seriously.
- **The four-leg rationale + citation-with-excerpt forcing function** in `lifecycle-2-design` is rare and valuable. The citation excerpt requirement specifically is hard to fake.
- **The `precept-author` agent body** is a careful synthesis of authoring rules — best instance of agent-prompt engineering in the project.
- **`tooling/mcp.md` is exemplary** in distinguishing live vs. planned tools and keeping the boundary honest. This pattern should propagate to runtime/.
- **`docs/language/precept-language-spec.md § 0 Preamble` is the corpus's most operationally precise foundational text** — clearer than philosophy.md on what each principle means at the design level.

## Anchor-by-anchor comparison

| Anchor | Where Precept's corpus lands | Pattern to borrow |
|---|---|---|
| **Rust documentation system** (rust-lang/rust + Reference + Book + RFCs + std docs) | Precept has the rough analogues (CLAUDE.md ≈ contributing, spec ≈ Reference, philosophy ≈ no Rust equivalent, lifecycle skills ≈ RFC process) but lacks the *rustc-dev-guide* layer — the "how the compiler is organized" doc. | Add a rustc-dev-guide analogue: `docs/agent-onboarding.md` (F-10). Adopt RFC-style numbering for in-flight designs in `docs/Working/`. |
| **Swift Evolution + Apple developer docs + The Swift Programming Language** | Swift's distinct audience tiers (book = beginner, reference = developer, evolution = contributor) — Precept blends all into one corpus. | Distinguish reading orders by audience tier (catalog discipline = contributor-tier; precept-author skill = user-tier). |
| **TC39 + MDN + ECMA-262** | Precept's spec carries operational precision similar to ECMA-262 § sections. But the *process doc* gap is real: TC39 stage advancement criteria are explicit; Precept's lifecycle stages are implicit in the CONTRIBUTING.md table. | Adopt TC39-style explicit stage criteria for the lifecycle skills. |
| **TLA+ + Specifying Systems** | TLA+ docs foreground "specifications are read by humans who must reason about them" through every notation choice. Precept's domain-expert commitment (F-8) is the analogue but is not operationalized. | Add the audience commitment as a constraint on every language-surface design decision. |
| **Stripe docs** | Stripe's honesty about what's live vs. planned (`mcp.md` already adopts this) is the standout pattern. README.md and runtime/ should adopt the same pattern. | Apply mcp.md's live/planned discipline to README.md (F-2) and runtime/ (F-6). |
| **Linux kernel `Documentation/`** | Kernel docs have strict cross-reference discipline (`make htmldocs` errors on dangling refs). Precept's corpus has dangling refs (F-3). | Add a doc-link CI check. |
| **Bazel build language docs** | Bazel separates reference (be-overview), language (build-ref), and rules — each tightly scoped. Precept's `compiler-and-runtime-design.md` blends all three. | Convert `compiler-and-runtime-design.md` to a pointer hub (F-5). |
| **Dhall manual** | Dhall's small-corpus discipline ("everything is a spec, but the spec is teachable") — Precept's spec is closer to "everything is a spec" but the teachable layer is missing. The closest substitute is the `precept-author` agent body. | Promote the precept-author agent body to a real "Precept for authors" doc. |

## Three concrete agent-walkthrough scenarios

### Scenario A: Fresh agent invoked for a small language-surface design ("add a `between A and B` shorthand for `>= A and <= B`")

**Reading path the agent would actually take:**
1. CLAUDE.md (Required) — finds Language Surface Design rule (route to /lifecycle-2-design).
2. Invokes /lifecycle-2-design — required reads block.
3. philosophy.md (always-required).
4. docs/README.md (always-required) — navigates to docs/language/README.md.
5. docs/language/README.md — reading order points to precept-language-spec.md, primitive-types.md, type-checker.md, collection-types.md.
6. precept-language-spec.md — scans § 2 Parser Grammar and § 3.6 Expression Typing Rules.
7. catalog-system.md (referenced from language/README.md "IMPORTANT" callout) — scans Operators, ExpressionForms, Tokens.
8. research/language/README.md — checks if a comparable shorthand exists in surveyed systems.

**Where the corpus helps:**
- The "always-required" three reads anchor the work in philosophy.
- The catalog-driven discipline is unambiguous: new operator goes in the Operators catalog first.
- /lifecycle-2-design forces four-leg rationale.

**Where the corpus fails:**
- The catalog count drift (F-1) — agent reads "thirteen" then "fifteen" within one session.
- No glossary (F-9) — "expression form," "operator binding power," "led form," "ExpressionForms catalog" all need disambiguation.
- /lifecycle-2-design is rhetorical (F-11) — agent can declare Locked without all the legs, no mechanical gate.
- catalog-system.md is 4500 lines (F-16) — substantial scan cost.

### Scenario B: Compiler-pipeline change ("add a new diagnostic for unreachable transition rows")

**Reading path:**
1. CLAUDE.md — Doc Routing Table points to `docs/compiler/diagnostic-system.md` + relevant stage doc.
2. compiler/README.md — non-negotiable rules; pipeline order.
3. compiler-and-runtime-design.md (referenced from compiler/README.md "IMPORTANT" callout) — § Non-Negotiable Rules.
4. compiler/diagnostic-system.md (canonical for diagnostics).
5. compiler/graph-analyzer.md (the stage that detects unreachable rows).
6. language/catalog-system.md — Diagnostics catalog entry.
7. catalog-driven-checklist.md — pre-implementation gate.
8. After implementation: precept-reviewer agent for review.

**Where the corpus helps:**
- The 16-section per-stage template makes graph-analyzer.md easy to scan for the right section.
- The Diagnostics catalog gives the agent a clear "add an entry here, propagate everywhere automatically" path.
- The diagnostic-system.md doc has a clear "what gets cataloged" answer.

**Where the corpus fails:**
- compiler-and-runtime-design.md duplicates graph-analyzer.md content (F-5) — agent reads both, time wasted reconciling.
- Status taxonomy drift (F-4) — graph-analyzer.md is "Implemented"; an agent assumes everything in it is shipped, but the proof engine principles 7-10 referenced from graph-analyzer.md are spec-only per spec § 0.6.
- precept-reviewer agent will duplicate the always-required reads (F-14).

### Scenario C: Review of an in-tree diff (PR adding a new modifier)

**Reading path (via precept-reviewer agent):**
1. Agent body sets up reading: CLAUDE.md, philosophy.md, docs/README.md, docs/language/README.md.
2. Plus rules embedded in agent body (16 KB of them).
3. Look up the modifier surface — `catalog-system.md § Modifiers` (a DU with five subtypes).
4. Look up the spec — `precept-language-spec.md § 2.4 Field Modifiers`.
5. Look up downstream: parser, type checker, grammar generator, MCP, LS.
6. Apply non-negotiable rules from CLAUDE.md and agent body.

**Where the corpus helps:**
- The catalog-driven discipline is well-articulated — the reviewer has a strong frame for "is this in catalog, or scattered?"
- catalog-driven-checklist.md gives explicit red flags.
- The Modifiers DU is well-defined in catalog-system.md.

**Where the corpus fails:**
- Reviewer reads catalog-driven-checklist.md's "fourteen catalogs" while the catalog-system.md says "fifteen" (F-1).
- Reviewer has no glossary (F-9) — "modifier" disambiguation across sub-types is real cognitive load.
- The reviewer's role overlaps with /lifecycle-2-design's Locked checks (F-14) — agent doesn't know which gate is which.
- Status taxonomy (F-4) — reviewer sees "Locked design" on a modifier doc but that isn't in the taxonomy.

## Final recommendations

**If only 3 changes could be made:**

1. **Fix catalog count drift + dangling references + add doc-corpus lint (F-1, F-3, partly F-11).** One PR resolves "thirteen vs fourteen vs fifteen" across CLAUDE.md, compiler-and-runtime-design.md, catalog-driven-checklist.md; one PR resolves the dangling references to `docs/PreceptLanguageDesign.md`, `README-legacy.md`, `docs/DesignNotes-legacy.md`. Ship a `docs-lint` CI check (counts statements consistent across docs; cross-references all resolve; status fields all in canonical taxonomy). This single intervention addresses the most damaging class of failure — foundational docs disagreeing with each other — and adds a mechanical gate that prevents future drift.

2. **Add `docs/glossary.md` and `docs/agent-onboarding.md` (F-9, F-10).** Two new docs, ~3-4 pages each. Glossary makes 30-40 load-bearing terms one-click resolvable. Agent-onboarding gives a fresh agent the project's organizing concepts in 200 lines. Together these reduce per-session context cost meaningfully and make the corpus accessible to stateless agents the way it is to onboarded humans.

3. **Fix README.md, route runtime/ design proposals to Working/, apply pointer-philosophy to compiler-and-runtime-design.md (F-2, F-5, F-6).** README ships honest claims about current API + tool count. The 4000 lines of unshipped runtime design move to `docs/Working/runtime/`. The 125 KB compiler-and-runtime-design.md monolith becomes a 30 KB hub doc with pointers. These three structural fixes eliminate the most context-wasting and most drift-prone parts of the corpus.

**If 10 changes:**

Add to the above:

4. **Operationalize approximation honesty (F-7) in every type doc** — Approximation Stance section, ~5 lines per doc.
5. **Operationalize domain-expert primacy (F-8) in language spec § 0** — Authoring Audience section, ~30 lines, plus reviewer obligation.
6. **Adopt a canonical sub-area README template (F-13)** — apply uniformly to language, compiler, runtime, tooling.
7. **Add `lifecycle-4-execute` skill (F-12)** — fills the chain gap.
8. **Add `docs/anti-patterns.md` consolidating the scattered anti-pattern material (F-17).**
9. **Split catalog-system.md into architecture/inventory/integration (F-16)** — three docs of ~1500 lines each.
10. **Add `docs/Working/Archive/README.md` decision-history index (F-15)** with /lifecycle-5-promote skill updating it.

**Direction of travel:** The corpus is set up to be excellent. It currently performs around mid-tier for a project of this ambition. The 10 changes above would bring it to the bar Precept's philosophy claims for itself. The gap is structural and concrete, not foundational. The forcing functions the project has invented (catalog-driven discipline, four-leg rationale, lifecycle skills) work — they just need to be applied to the docs themselves with the same rigor they're applied to code.
