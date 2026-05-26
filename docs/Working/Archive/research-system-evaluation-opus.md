---
status: Draft evaluation — 2026-05-25
evaluator: Opus 4.7 (independent)
subject: Precept research system — research/ corpus + lifecycle-1-research skill + Stage 1→2 handoff
purpose: Assess whether design decisions are actually grounded in research as claimed; surface Phase 8+ candidates for the corpus-improvement plan
prior-evaluations:
  - docs/Working/Archive/skills-rigor-evaluation-opus.md
  - docs/Working/Archive/doc-corpus-evaluation-opus.md
---

# Research System Evaluation

## Verdict

**The research corpus is genuinely substantive — but the research *system* is rhetorical, not enforced.** Read in isolation, the best research files (currency-precision-coupling-survey, formal-spec-languages-comparators, parser-combinator-scalability, intellisense-discoverability-ux, domain-integrity-formal-concept, case-insensitive-comparison-survey) hold up to peer-reviewed-survey standards: real external sources, verbatim excerpts, comparator-by-comparator analysis, threats-to-validity, surfaced open questions. The author has done genuine research work. The system claims this rigor is uniform; the system also claims rigor is enforced at the design-skill boundary. Neither claim is true.

The single most damaging finding is structural and empirical: **zero locked designs in the tree carry `sources-consulted` frontmatter, zero use `Stakes:` classification, and zero have `## Falsifiers` sections** — despite the lifecycle-2-design skill claiming all three are required at lock time. The skill's "refuses to lock" guards are unenforced text. The only docs that follow the lifecycle-2 frontmatter discipline are the two prior Opus evaluations and the corpus-improvement plan — meta-docs about the system, not designs the system produced. This is the same "discipline is rhetorical" failure mode the doc-corpus eval surfaced about Phase 0 enforcement, now confirmed at the most load-bearing surface: the formal handoff between research and design.

The research skill itself is qualitatively weaker than the design skill: 2 instances of imperative gating language ("must / required / refuse") vs. 64 in lifecycle-2-design. It has no refusal gates, no quality-bar acceptance criteria, no "fail this and the research doesn't ship" mechanisms. It is a template, not a stage. Its strongest claim — the promote-or-cite rule — is unenforced and demonstrably broken: `parser-combinator-scalability.md` is a high-quality research artifact that the language-area README cites only as a navigation aid, with no corresponding design doc that adopts its conclusions; `data-vs-state-pm-research.md` and `readme-research-steinbrenner.md` in `research/product/` are not cited from anywhere in `docs/` that I could find. The corpus is doing real work, but the system around it is doing none of the work it claims.

## How research and design integrate today

The formal pipeline:

1. **Stage 1 (`lifecycle-1-research`)** produces a markdown file in `research/<subfolder>/` following a 7-step process (triage, folder, inline/delegate, document structure, naming, link, promote-or-cite).
2. **Stage 2 (`lifecycle-2-design`)** consumes Stage 1 output via the `--from <research-doc>` flag, which the skill claims pre-populates Decisions section legs from research findings.
3. **Reviewer (`precept-reviewer`) § 13 Source Verification** — for design-doc reviews, the reviewer must open every cited source and verify the excerpt.
4. **Phase 4 of corpus-improvement plan** added the "Strongest counter-evidence" leg per high-stakes decision, plus stricter discipline for external URL citations (access date, verbatim excerpt, stable identifier for standards).

**Where it works (rarely):**

- `research/architecture/compiler/currency-precision-coupling-survey.md` → cited from `docs/language/business-domain-types.md:1740` AND `docs/Working/compiler-readiness-plan-2026-05-24.md:126` → grounds Position 3 decision for F-LANG-BIZ-02. This is the textbook success case: substantive external survey, multi-source verbatim excerpts (Joda-Money Javadoc, JSR-354 user guide, Stripe docs, Adyen docs), explicit counter-evidence (the JSR-354 deliberate decoupling — strongest argument against the position adopted), open questions surfaced (boundary enforcement, hyperinflationary currencies, multi-currency arithmetic).
- `research/language/expressiveness/string-ordering-*.md` (6 files) → grounds the `primitive-types.md § String Ordering — Out of Scope` decision. The decision cycles through multiple research files as evidence converged on "no demand signal across 12 business domains."

**Where it breaks (most common):**

- The currently-active `docs/Working/field-never-set-diagnostic.md` is marked `status: Locked 2026-05-25` and carries **no `sources-consulted` frontmatter, no `Stakes:` classification, no `## Falsifiers` section, no "Strongest counter-evidence" leg on any of its 6 decisions, no "Sources consulted for this decision" leg on any decision**. The decisions cite "the spec sections 996-1017" (no excerpt), `samples/prescription-refill-request.precept` (no excerpt), and "every comparable language with a layered access-capability model" (no comparator named with section/excerpt for Rust `mut` or TypeScript `readonly`). The skill's Decision 5 names "TypeScript `readonly`, Rust `mut`, Kotlin `val`/`var`, SQL `GRANT UPDATE`" as precedent without a single citation that could survive `precept-reviewer § 13` opening. This is a `Locked YYYY-MM-DD` design with `Stakes: irreversible` decisions (keyword retirement is in the irreversible bucket per the skill's own table) that should have failed every Phase 4 gate.
- The skill says "refuses to lock if decision references external state but `Sources consulted` empty" — and yet the doc locks anyway. The forcing function is text in a markdown file; the agent that wrote the design didn't read it, didn't apply it, and the agent that reviewed (if there was a reviewer pass) didn't catch it.
- The `--from <research-doc>` integration in lifecycle-2-design is mentioned twice in the skill but the only locked design (field-never-set-diagnostic.md) cites zero research files. Either the integration was never used, or it was used and produced nothing visible. Either way, there's no evidence the Stage 1 → Stage 2 pipeline is operationally active.

## Empirical citation audit

I sampled 10 specific citation claims across docs and research files. The results:

| # | Claim source | Cited as | Verifiable? | Verdict |
|---|---|---|---|---|
| 1 | `research/architecture/compiler/currency-precision-coupling-survey.md` § Joda-Money | Joda-Money Javadoc: *"Every currency has a certain standard number of decimal places. This is fixed to this number of decimal places."* | Yes — published Joda-Money documentation has this exact phrasing on `CurrencyUnit` | **PASS** — excerpt accurate, claim accurate |
| 2 | Same file § JSR-354 | JSR-354 RI user guide: *"is capable of supporting arbitrary precision and scale"* | Yes — appears in JSR-354 RI docs | **PASS** |
| 3 | Same file § Adyen | Adyen: *"For CLP, CVE, IDR, and ISK the ISO 4217 standard has a different number of decimals than shown in our currency codes table."* | Yes — Adyen currency-codes documentation | **PASS** — high-load-bearing claim, verifiable |
| 4 | `research/language/parser-combinator-scalability.md` § Source 2 | Bryan Ford 2004 PEG paper formal properties (determinism, no left recursion, packrat linear time) | Partially — paper is real (Ford, "Parsing Expression Grammars: A Recognition-Based Syntactic Foundation", POPL 2004), claims accurate; **no DOI / no stable identifier / no verbatim excerpt** | **CONCERN** — Phase 4 mandates stable identifier (DOI/title+venue+year); only "title+year" supplied |
| 5 | Same file § Source 5 (ANTLR) | ANTLR LL(*) capabilities, error recovery | **HTTP 429 at fetch — author explicitly notes "from knowledge"** | **CONCERN** — author honestly flags inability to fetch; no fallback to mirrored source; this is exactly the case Phase 4 says should be mirrored to `research/references/` |
| 6 | `research/philosophy/formal-spec-languages-comparators.md` § Event-B | event-b.org "page unavailable at fetch time — supplemented from knowledge" | **No verbatim from event-b.org** | **CONCERN** — same pattern; "from knowledge" supplementation has no mechanism to flag for later verification |
| 7 | `research/language/expressiveness/temporal-type-strategy.md` § 8 Research Trail | Cites 4 prior internal research files + Precept docs only | Yes, internal files exist | **FAIL on Phase 4 / Stage-2 guard 2** — synthesis doc cites zero external sources; lifecycle-2-design says "Citing only Precept-internal docs for [Language Design Grounding] is not acceptable"; this is a `Status: Synthesis document` artifact that cascades into the temporal type system spec |
| 8 | `docs/Working/field-never-set-diagnostic.md` Decision 5 | "TypeScript `readonly` appears at field declarations and in mapped types — same word. Rust `mut` appears at field declarations and in patterns/bindings — same word." | Claims plausibly true but **zero citation, zero excerpt, zero link** | **FAIL** — naked claim about external languages used as Precedent; Stage 2 guard 11 says "Refuse; ask the author to open the source and paste a short verbatim excerpt" |
| 9 | `research/language/expressiveness/intellisense-discoverability-ux.md` § 3 Nielsen | *"Minimize the user's memory load by making elements, actions, and options visible. The user should not have to remember information from one part of the interface to another."* — Jakob Nielsen, Heuristic #6 (1994, updated 2020) | Yes — accurate quote from 10 Usability Heuristics; **no access date, no URL, no DOI** | **CONCERN** — claim true but provenance discipline missing |
| 10 | `research/language/expressiveness/case-insensitive-comparison-survey.md` § Lua 5.4 | Lua 5.4 Reference Manual § 3.4.4: *"The operator `~=` is exactly the negation of equality (`==`)."* | Yes — exact text from `https://www.lua.org/manual/5.4/manual.html` | **PASS** — excerpt accurate, claim accurate |

**Aggregate: 4 PASS, 5 CONCERN, 1 FAIL out of 10.**

The CONCERN rate is the load-bearing finding: **half the citations would not survive `precept-reviewer § 13` source verification** because they either lack stable identifiers (Phase 4 mandate) or were unfetchable and substituted by author knowledge with no mechanism to flag for later verification. The skill says external URL citations "must include the access date" — across 56 expressiveness research files, **zero** contain the strings "access date" or "Retrieved". The discipline exists in skill text, not in research practice.

The FAIL is more consequential: the only Locked-in-tree design that's not a meta-doc (`field-never-set-diagnostic.md`) makes Precedent claims about other languages with zero verifiable citation. This is the maxplaces-iso pattern the corpus-improvement plan was supposed to prevent — and Phase 4's mechanism didn't.

## Research-skill adequacy

The `lifecycle-1-research` skill is structurally a template, not a stage. Comparing it side-by-side with `lifecycle-2-design`:

| Property | lifecycle-1-research | lifecycle-2-design |
|---|---|---|
| Imperative gating language ("must / required / refuse") | 2 instances | 64 instances |
| Refusal gates ("refuse to ship if X") | 0 | 15 numbered behavioral guards |
| Required structural sections enforced | 0 (document structure is "adapt to topic") | 11+ (Philosophy Alignment matrix, Semantic Rules, Architecture Grounding, Audience, Falsifiers, Doc-update enumeration) |
| Per-claim discipline | Loose ("citation-rich") | Strict (verbatim excerpt mandatory per Phase 4) |
| Methodology requirement | "Short" | Per-decision four-leg + stakes-appropriate legs |
| Counter-evidence requirement | None | Per-high-stakes decision |
| Threats-to-validity requirement | None | None — but should exist |

**What's missing structurally (comparison anchors):**

- **Rust RFC "Prior art" section** — RFCs require naming the strongest comparable systems (TC39 stages, GHC proposals, Haskell extensions, ML-family languages) AND explaining why Rust differs. The skill says "compare alternatives" but has no obligation that the comparison be exhaustive against the strongest comparable systems for the domain. Result: `temporal-type-strategy.md` exists without citing Joda-Time (the direct progenitor of NodaTime, whose API and ZonedDateTime semantics map directly), without citing Python `datetime`/`dateutil`/`pendulum`, without citing JSR-310 / `java.time`, without citing Rust's `chrono`/`time` crates, without citing CEL's time semantics. NodaTime is well-cited but the comparator field is one library.
- **TC39 Stage 1 "References" requirement** — a TC39 proposal at Stage 1+ must reference at least one cross-engine implementer commitment and survey of the prior-art landscape. The Precept skill has no equivalent — research can ship with one comparable system cited.
- **POPL/PLDI "Related Work" tradition** — academic papers must explicitly enumerate the closest prior work AND distinguish from it. The Precept skill's "alternatives considered and rejected" leg approximates this but is on the consuming design, not the research. The research that produces the conclusion is structurally allowed to be "I read this, here's what I think."
- **Systematic review (Kitchenham/Petersen) protocol** — research question; inclusion/exclusion criteria; search strategy; data extraction template; threats to validity. The Precept skill has none of these. `domain-integrity-formal-concept.md` is excellent but it's excellent *because the author chose to engage with C.J. Date, Fowler, Greg Young*; nothing in the skill required the engagement.
- **Cochrane-style evidence grading** — different evidence has different weight (peer-reviewed paper > standards doc > vendor docs > blog post > author knowledge). The skill is silent on weighting; "Source is an open category" loses the gradation. Result: a Stripe documentation page and a POPL paper count identically.
- **Hillel Wayne / Bret Victor "what would change my mind" school** — explicit falsifiability for the research conclusion. The skill's "Open Questions" section approximates this but isn't the same: open questions are "what we didn't resolve" not "what observation would prove this conclusion wrong."
- **AWS PRFAQ "Working Backwards" evidence** — for product/positioning research, what evidence supports this is what customers want? `research/product/` has 3 files; none follow PRFAQ structure or any rigorous customer-evidence pattern.

**What's actually enforced:** folder discipline (where the file goes) and the promote-or-cite rule (which, as shown below, is unenforced).

## Research-corpus quality

Sample-by-sample assessment:

### High-quality (would survive peer review with minor citation tightening)

- **`research/architecture/compiler/currency-precision-coupling-survey.md`** (Frank, 2026-05-25). 11 surveyed systems with verbatim excerpts; explicit threats (wire-format vs type-system distinction); strongest-counter-evidence engagement (JSR-354 deliberate decoupling); 4 open questions surfaced. **Anchor: PRFAQ-grade plus academic Related-Work grade.** Missing: access dates on external URLs (Stripe docs, Adyen docs), formal stable identifiers for IFRS IAS 21 / US GAAP ASC 830.
- **`research/language/parser-combinator-scalability.md`** (Frank, 2026-04-19). 5+ comparator engagement (Superpower, PEG/Ford, recursive descent, Roslyn, ANTLR), explicit limitations table, three-option analysis with verdicts. **Anchor: Rust RFC prior-art-grade.** Missing: DOI for Ford 2004, ANTLR fallback mirror, stable identifier for Roslyn wiki snapshot.
- **`research/philosophy/formal-spec-languages-comparators.md`** (Frank, 2026-04-19). Alloy / TLA+ / Event-B / Z notation engaged. Verdicts grounded; the Event-B / Precept structural-identity observation is exactly the kind of insight serious survey produces. **Anchor: POPL Related-Work grade.** Missing: stable identifiers on academic sources; "supplemented from knowledge" gaps.
- **`research/language/expressiveness/case-insensitive-comparison-survey.md`**. 15+ systems surveyed; explicit cascade analysis. **PASS on comparator quality.**
- **`research/language/expressiveness/intellisense-discoverability-ux.md`** (Elaine). Power Fx, Salesforce, Notion, Airtable, TypeScript, C#, Java surveyed with Nielsen heuristic grounding; 7-section structure; explicit recommendation tied to evidence. **PASS on UX-research-grade.**
- **`research/philosophy/domain-integrity-formal-concept.md`** (Frank, 2026-04-19). C.J. Date relational theory, DDD, Fowler, Microsoft .NET DDD guidance, Greg Young. Each source consulted, each integrated to product positioning. **PASS.**
- **`research/architecture/compiler/temporal-type-hierarchy-survey.md`**. Multi-library comparator with explicit type-by-type tables. Strong on factual content; weak on synthesis (declares itself "raw research collection. No interpretation, no conclusions, no recommendations").

### Mixed quality (substantive content but skill-discipline gaps)

- **`research/language/expressiveness/temporal-type-strategy.md`** (Frank, 2026-04-13). Substantive but **cites zero external sources** — only prior internal research files and Precept docs. This is exactly the lifecycle-2-design guard 2 violation ("Citing only Precept-internal docs is refused") manifesting in a Stage 1 artifact. The "Research Trail" section is 4 prior internal docs. Joda-Time, Python `datetime`, JSR-310, Rust `chrono` — none cited. Yet this document is the conceptual ground for the temporal type system that's in spec docs.
- **`research/language/expressiveness/data-only-precepts-research.md`**. Cites Terraform, Protobuf, GraphQL, SQL DDL, DDD, Evans 2003 — but every citation is bare ("Evans (2003) explicitly recognizes"), no excerpt, no edition/page, no DOI for the principle (the Apple HIG "progressive disclosure since 1985" claim has no source URL). Naked-claim style throughout. Would not survive `precept-reviewer § 13`.
- **`research/language/expressiveness/xstate.md`** + **`polly.md`** + **`fluent-validation.md`** + **`zod-valibot.md`** + **`linq.md`**. Each cites a single source URL at the top of the file (`https://xstate.js.org/docs/`, etc.) with no access date and no per-claim excerpt. These read as "I went and looked at this and here's what I think" — author-knowledge-grade dressed as comparator survey. The corpus is sound; the citation provenance is weak.

### Low quality / orphan-shaped

- **`research/product/data-vs-state-pm-research.md`** and **`research/product/readme-research-steinbrenner.md`** — I cannot find these cited from any canonical doc in `docs/`. The promote-or-cite rule says research is either promoted (the conclusion lives in a spec) or cited (referenced from a proposal/decision/spec/another research file). These are at minimum sub-cited.
- **`research/security/security-survey.md`** — substantial document by "Uncle Leo (Security Champion)" but I found no `docs/security/` doc that incorporates it, no proposal that cites it, no design that adopts its findings. The OWASP / SLSA / supply-chain content is real and useful but doesn't connect forward to a security-discipline page anywhere in `docs/`.
- **`research/language/research-conditional-construction.md`** — `docs/archive/lifecycle-review-phase-1-2026-05-24.md` explicitly notes this file isn't cited from `research/README.md` and observes the discoverability obligation is "met via cross-link rather than README." This is the failure mode: the README claims a domain index; the index isn't actually maintained as the corpus evolves.

### Filing errors

The `research/language/` top level contains files that should be in `research/architecture/` per the skill's own taxonomy:

- `parser-combinator-scalability.md` is at `research/language/` but is fundamentally a compiler-architecture investigation (PEG vs ANTLR vs hand-written recursive descent); should be in `research/architecture/compiler/`.
- `precept-language-mcp-audit.md` and `precept-language-tool-architecture.md` are tooling-architecture audits, not language research; should be in `research/architecture/`.
- `philosophy-refresh-assessment.md` is in `research/language/` but its content is product-positioning / philosophy; should be in `research/philosophy/`.

The taxonomy isn't enforced; the table in the skill ("Topic → Folder") is rhetorical.

## Promote-or-cite rule — is it binding?

The skill says: *"If research is neither promoted nor cited, it's shadow policy — claims with no governance. Don't let it sit there. Either promote it, cite it, or move it to `research/archive/` with a one-line note on why it didn't ship."*

Empirical check:

- `research/archive/` contains **one** file (`precept-value-types-investigation.md`). Either every other research artifact was promoted or cited (claim: not credible) or the archive discipline is unenforced.
- I tested 5 specific research files for inbound citations from `docs/` (excluding self-references within `research/`):
  - `currency-precision-coupling-survey.md` — **2 inbound citations** (canonical spec + readiness plan). **PASS.**
  - `parser-combinator-scalability.md` — cited only from `research/language/README.md` as a "navigation aid" entry; **no design doc or spec adopts conclusions**. **Sub-cited.**
  - `formal-spec-languages-comparators.md` — surveys whether Precept should add formal-spec-language comparators to philosophy.md, concludes "no"; **decision lives only in the research file**, not as a `docs/philosophy.md` decision-record entry. **Shadow policy** by the skill's own definition.
  - `security/security-survey.md` — **no inbound citations from docs/**. Substantive content; no canonical home; not archived.
  - `domain-integrity-formal-concept.md` — substantive philosophy grounding; **no inbound citations from docs/philosophy.md** that connect the formal lineage. Conclusions are real but not promoted.

Four of five sampled files are sub-cited or shadow-policy. The promote-or-cite rule is not binding; nothing mechanically enforces it; the README claim that "deliberate horizon groundwork" covers the gap is convenient framing rather than discipline.

## Discoverability for AI agents

Scenario: a fresh agent inherits a task ("design the `quantity in 'kg'` arithmetic rules"). Can it find what's already been researched?

**Existing path:**

1. `research/README.md` → "Start here: `research/language/README.md`"
2. `research/language/README.md` → has a "Domain index" with 11 domain rows + an "Open proposal issue map" with 18 issues + a "Cross-cutting research" section + a "Domains with research but no active proposal" section + a "Implemented domains" section + a "Design validation artifacts" section
3. Domain index doesn't have a `units-of-measure` or `quantity` row; the agent has to search.
4. Cross-cutting research section has 12 entries; "ucum-tier1-curation.md" is in `research/language/` but not in this listing (it's mentioned in `Workflow reminder` indirectly).
5. The architecture-side research at `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md`, `quantity-normalization-design-survey.md`, `business-units-quantity-normalization-survey.md` is in a completely separate folder with its own README that the language README **does not cross-link to**.

**Result:** A fresh agent looking for unit-of-measure research must (a) know to search both `research/language/` and `research/architecture/compiler/`, (b) not be misled by the language README's domain index that doesn't list quantity/UoM as a domain, (c) discover that `ucum-tier1-curation.md` exists in `research/language/` (not under language/expressiveness or language/references).

The "Domain index" is the discoverability mechanism the README claims; it's incomplete. The "issue map" assumes the agent knows the issue number, which a fresh agent generally doesn't.

**Cross-folder index is missing.** There is no `research/INDEX.md` or `research/topic-index.md` that maps topics ("currency precision," "unit normalization," "guard composition," "timezone semantics") to the files anywhere in the corpus. The language-README and architecture-README exist independently; an agent searching across the corpus has to know both exist.

## Findings

### F-1: Phase 4 lifecycle-2-design rules are unenforced on every existing locked design
**Category:** CITATION-DISCIPLINE  
**Severity:** CRITICAL  
**Comparison:** Rust RFC tooling (rfcbot mechanically gates rfcs without the required sections)

**What:** No locked design in `docs/Working/` or `docs/Working/Archive/` has `sources-consulted` frontmatter, `Stakes:` classification on decisions, `## Falsifiers` section, "Sources consulted for this decision" leg, or "Strongest counter-evidence" leg — yet the skill claims to refuse to lock without these.

**Why it matters:** The Per-Decision Rationale rule is one of three CLAUDE.md non-negotiables. Phase 4 of the corpus-improvement plan was the operationalization. Phase 4 is text in a skill file the agent doesn't enforce. The skill's "refuses to lock" is rhetorical.

**Evidence:** `grep -l "sources-consulted" docs/Working/*.md docs/Working/Archive/*.md` returns only the three meta-docs (the prior Opus evaluations + the improvement plan). `grep -l "Stakes:"` returns zero design docs. `grep -l "## Falsifiers"` returns only the skills-rigor eval and the improvement plan. `docs/Working/field-never-set-diagnostic.md` is `Locked 2026-05-25` with 6 decisions, every one missing every Phase 4 leg.

**Recommendation:** Phase 8 must retrofit Phase 4 on at least one in-tree design (eat-the-dog-food) and add the docs-lint check that mechanically refuses to merge a `Locked` doc missing the leg set. Without a mechanical gate, no rule about citation discipline binds.

---

### F-2: lifecycle-1-research skill has no refusal gates — it's a template, not a stage
**Category:** SKILL-ADEQUACY  
**Severity:** CRITICAL  
**Comparison:** lifecycle-2-design has 15 numbered behavioral guards with explicit "Refuse" language; lifecycle-1-research has 2 imperative-tone instances total

**What:** The research skill describes a methodology but has no rejection criteria. There is no quality bar the skill enforces; an agent can produce a research artifact that's three paragraphs of advocacy with no external citations and the skill has no obligation to reject it.

**Why it matters:** The doc-corpus eval surfaced "discipline is rhetorical" as the systemic failure mode of lifecycle-2-design's old "refuses to lock" claims. The research skill is at the same stage now. If the design skill needs 15 numbered behavioral guards to bind, the research skill — which produces the evidence the design depends on — needs equivalent guards.

**Evidence:**
- `grep -c "refuse\|Refuse\|reject\|Reject\|must\|MUST\|required\|Required"` on the two skills: research = 2; design = 64.
- The "Step 4: Document Structure" section says "adapt to topic" — the structure is suggested, not required.
- The "What NOT to Do" section is a 7-bullet list with no refusal mechanism.
- No mention of citation-with-excerpt being a refusal criterion (which is what design enforces downstream).

**Recommendation:** Add explicit behavioral guards to lifecycle-1-research. Minimum set: (1) refuse research that cites zero external sources for a question that has external answers; (2) refuse research without verbatim excerpts on load-bearing claims; (3) refuse research without an explicit threats-to-validity / counter-evidence-considered section; (4) refuse research that promotes a conclusion without listing the strongest counter-position considered and rejected.

---

### F-3: External URL citation discipline (access date, stable identifier, verbatim excerpt) is universally absent in research
**Category:** CITATION-DISCIPLINE  
**Severity:** MAJOR  
**Comparison:** Cochrane Collaboration / academic publishing — every external source has access date, DOI/ISBN/ISSN, full quoted excerpt

**What:** Phase 4 of the corpus-improvement plan said external URL citations "must (a) include the full quoted excerpt verbatim, (b) include the access date, (c) be preferred only when no in-tree or paper-PDF equivalent exists." Across 56 expressiveness research files, **zero** contain "access date" or "Retrieved". Sources are typically a single URL at the file header with no per-claim re-citation.

**Why it matters:** Half the 10 citations I audited would not survive `precept-reviewer § 13` verification because the source either has no stable identifier or returned HTTP 429 / 404 at fetch time and was "supplemented from knowledge." This is exactly the scenario the discipline is supposed to prevent.

**Evidence:** 
- `parser-combinator-scalability.md` § Source 5: "antlr.org — HTTP 429 at time of fetch; from knowledge"
- `formal-spec-languages-comparators.md` § Event-B: "event-b.org (page unavailable at fetch time — supplemented from knowledge)"
- `intellisense-discoverability-ux.md` § 3 Nielsen — quote provided, no access date or URL on the quoted text
- `temporal-type-strategy.md` — zero external citations; 4 internal research files cited as the only Research Trail

**Recommendation:** Mandate a `## Sources` section as required (not optional) in every research file, with one entry per external source: title, author/org, stable identifier (DOI / RFC# / ISBN / ISO# / vendor-doc URL + access date), accessed-on date, and a "mirrored to" path if the source has been snapshotted to `research/references/`. Existing files retrofitted as opportunistic work, not bulk.

---

### F-4: The temporal-type-strategy synthesis violates the very Stage 2 grounding rule it eventually feeds
**Category:** RESEARCH-QUALITY  
**Severity:** MAJOR  
**Comparison:** Rust RFC for chrono / time crates would cite java.time / Joda-Time / Python datetime / pendulum / Boost.DateTime explicitly

**What:** `research/language/expressiveness/temporal-type-strategy.md` is a load-bearing synthesis document — it grounds the canonical type docs (`docs/language/temporal-type-system.md`) and is referenced from the spec. It cites zero external sources. The "Research Trail" section is 4 prior internal research files. No engagement with Joda-Time (the progenitor library NodaTime is a port of), no engagement with java.time / JSR-310 (the standardization Joda-Time fed into), no engagement with Python `datetime` (the standard non-JVM reference), no engagement with Rust `chrono`/`time`, no engagement with Pendulum or Arrow as opinionated alternatives.

**Why it matters:** Stage 2 guard 2 says "Citing only Precept-internal docs is refused" for Language Design Grounding. The Stage 1 artifact that feeds Stage 2 has the same gap and is structurally allowed to. The system claims grounding; the source artifact is internal-only.

**Evidence:** Read `research/language/expressiveness/temporal-type-strategy.md` § 8 Research Trail and § 2 Type Decision Table. Every "rationale" in the type-decision table is a NodaTime + Precept-philosophy argument; no comparator gets verbatim engagement.

**Recommendation:** Either (a) retroactively add an external-comparator section to temporal-type-strategy.md (Joda-Time, java.time, Python datetime, Rust chrono); or (b) explicitly mark the file as "synthesis-only, comparator survey lives in `temporal-type-hierarchy-survey.md`" and require the spec to cite both. The current ambiguity lets the synthesis file stand as if it had done the survey work.

---

### F-5: The promote-or-cite rule is empirically violated for at least 4 of 5 sampled research files
**Category:** INTEGRATION  
**Severity:** MAJOR  
**Comparison:** Cochrane review obligation — synthesis must be promoted into guidance or explicitly de-prioritized; nothing sits as orphan policy

**What:** The skill claims "shadow policy" doesn't sit on disk. The corpus contains substantive shadow policy.

**Evidence:**
- `parser-combinator-scalability.md` — substantive research; no design or spec adopts conclusions; only navigational reference from language README.
- `formal-spec-languages-comparators.md` — declares verdict ("philosophy.md should not add formal-spec comparators"); no entry in `docs/philosophy.md` records this decision; the conclusion lives only in the research file.
- `security/security-survey.md` — no inbound citation from `docs/`; no `docs/security/` exists.
- `research/product/data-vs-state-pm-research.md`, `readme-research-steinbrenner.md` — no inbound citation found from canonical docs.
- `research/archive/` has exactly one file; the skill says research that didn't ship goes there.

**Why it matters:** If the rule binds, the corpus's research is either policy (promoted) or evidence (cited from policy). If it doesn't, research is the convenient place to write things that aren't ready to be policy but get to act like it.

**Recommendation:** Add an inbound-citation lint check to docs-lint. Every research file in `research/` must either (a) appear in a `Promoted` table that lists the canonical doc adopting the conclusion, or (b) appear in an inbound-citation listing showing ≥1 design/spec/proposal cites it, or (c) be moved to `research/archive/`. Annually triggered by `/lifecycle-7-audit`.

---

### F-6: No "research-shaped gap" detection mechanism
**Category:** GAP-DETECTION  
**Severity:** MAJOR  
**Comparison:** TC39 Stage 1 → Stage 2 advancement criteria explicitly require "prior art identified" — Precept has no equivalent gate

**What:** When a design needs research it doesn't have, nothing in the current system catches the gap. The design ships with weak grounding because no mechanism notices that grounding is weak.

**Evidence:** `field-never-set-diagnostic.md` is `Stakes: irreversible` (keyword retirement of `writable`) per the skill's own table. The decision invokes Rust, TypeScript, Kotlin, SQL as precedent with no citation. A research file engaging with those four languages' access-modifier vocabularies does not exist. Yet the design locks.

**Why it matters:** A design can produce a `Locked irreversible` decision without ever consulting prior art that exists. There's no "advance to Locked" gate that asks "is the prior-art research present at the level this decision needs?" The reviewer can flag it post-hoc, but: (a) the reviewer wasn't asked, (b) the human merging may not run the reviewer.

**Recommendation:** Add a research-adequacy gate to lifecycle-2-design's staged advancement. To advance from `Externally-Grounded` to `Locked` with any `irreversible` decision, the design must either (a) cite a research file in `research/` that surveyed the comparable systems for the irreversible-decision domain, or (b) carry the new "no research exists — author surveyed inline, here's the inline survey" leg with the same discipline as a full research artifact would require.

---

### F-7: Sub-folder taxonomy isn't enforced; mis-filed research distorts the discoverability index
**Category:** DISCOVERABILITY  
**Severity:** MAJOR  
**Comparison:** Systematic review search-strategy protocol — sources have provenance that lets a future reviewer reproduce the search

**What:** Multiple research files are filed under taxonomically-wrong folders. The "Determine the Right Folder" table in the skill is rhetorical.

**Evidence:**
- `parser-combinator-scalability.md` in `research/language/` — it's a compiler-architecture investigation; belongs in `research/architecture/compiler/`.
- `precept-language-mcp-audit.md` and `precept-language-tool-architecture.md` in `research/language/` — tooling-architecture audits; belong in `research/architecture/` or a new `research/tooling/`.
- `philosophy-refresh-assessment.md` in `research/language/` — product-positioning; belongs in `research/philosophy/`.
- `ucum-tier1-curation.md` is at `research/language/` directly; should be `research/language/references/` per the skill ("source captures belong in each domain's `references/` folder").

**Why it matters:** Fresh agents look for compiler-architecture research in `research/architecture/compiler/`. Files filed in `research/language/` for historical reasons are invisible to that search.

**Recommendation:** One-time corpus sweep to move files into correct folders. Add a docs-lint check that flags research file paths against the skill's filing table. Update the language README's domain index to reflect actual file locations.

---

### F-8: Research files lack structural sections (Methodology, Threats to Validity, Counter-evidence) that distinguish survey from advocacy
**Category:** RESEARCH-QUALITY  
**Severity:** MAJOR  
**Comparison:** Petersen et al. systematic mapping study — required sections include search strategy, inclusion criteria, threats to validity

**What:** 35 of 56 expressiveness research files lack a `## Methodology` section. Across all sampled research, **zero** carry a `## Threats to Validity` section. The skill's template lists "Methodology" but says "How the investigation was conducted... Short" — and the field is widely interpreted as optional.

**Why it matters:** A research file without an explicit methodology is, structurally, advocacy for a position the author reached. The substantive ones happen to also be the ones the author chose to make rigorous. Nothing in the system forces rigor.

**Evidence:** `grep -L "## Methodology" research/language/expressiveness/*.md` returns 35 of 56 files. `grep -rln "Threats to Validity" research/` returns 0.

**Recommendation:** Make `## Methodology` and `## Threats to Validity` required sections in the skill template, with explicit content requirements: methodology = what was searched, what was excluded, why; threats = the strongest reason this conclusion might be wrong, plus what would change my mind.

---

### F-9: Research-to-design `--from` integration is undocumented and unused
**Category:** INTEGRATION  
**Severity:** MAJOR  
**Comparison:** Rust RFC tooling — `rfcbot` mechanically links proposals to prior art; the link is structural, not narrative

**What:** lifecycle-2-design claims an `--from <research-doc>` flag that "extracts research conclusions and pre-populates the Decisions section's Rationale and Precedent legs from the research findings." No design in the tree shows evidence of having been produced this way. The mechanism either doesn't exist or has never been used.

**Why it matters:** The handoff is the load-bearing seam. If Stage 1 → Stage 2 is just "open the research file in your context and read it," the integration is human discipline. Phase 4 didn't add a mechanical check that a design with research-adjacent content actually cites the research that exists. The integration is rhetorical.

**Evidence:** No design carries `--from` provenance in its frontmatter; no docs-lint check enforces that a design about temporal types cites `temporal-type-strategy.md` and `temporal-type-hierarchy-survey.md`.

**Recommendation:** Either implement `--from` mechanically (lifecycle-2-design ingests the research file's findings into a structured legs-population) and require provenance in frontmatter (`grounded-by: research/path/to/file.md`); or remove the claim from the skill and replace with an explicit "the author manually consulted research/X.md" obligation in `sources-consulted` for any design touching a domain where `research/` content exists.

---

### F-10: Cross-folder discoverability is missing — there is no topic-index across the corpus
**Category:** DISCOVERABILITY  
**Severity:** MODERATE  
**Comparison:** TC39 References doc maps topic → spec section → implementation status across the JS ecosystem in one table

**What:** The corpus has two main READMEs (`research/language/README.md`, `research/architecture/compiler/README.md`) and lesser ones in each subfolder. No topic-index spans the full corpus. An agent looking for "currency precision" research has to know that the file is in `research/architecture/compiler/`, not `research/language/`.

**Why it matters:** Fresh agents are the standard case. The discoverability burden falls on them disproportionately. The five-organizing-concepts onboarding doc helps with concepts but not with topic→file traversal.

**Evidence:** No `research/INDEX.md`, `research/topic-index.md`, or equivalent exists. The language README's domain index is language-only; architecture and security have their own indexes; nothing aggregates.

**Recommendation:** Add `research/INDEX.md` — single-page topic-to-file map, kept up to date as new research lands. Anchored from `research/README.md` Start-here section. Maintenance obligation: `/lifecycle-1-research` skill updates it as final step on every new research file.

---

### F-11: Research and design citations on bare external claims ("Rust does X", "TypeScript does Y") are universally weak
**Category:** CITATION-DISCIPLINE  
**Severity:** MODERATE  
**Comparison:** Rust RFC prior-art convention — comparable-system claims carry explicit reference (e.g., "Haskell's GHC accomplishes this via [X type-class feature] — see [GHC users guide § N]")

**What:** Bare claims about "every comparable language" or "Rust's mut", "TypeScript's readonly" appear in research and design without per-claim citation or excerpt. The reader cannot verify without independently knowing the comparator system.

**Why it matters:** Such claims are the weakest argument shape — they're appeals to authority without authority on record. The Source Verification reviewer obligation can't catch them because there's no claim-citation to open.

**Evidence:** `field-never-set-diagnostic.md` Decision 5: "every comparable language with a layered access-capability model uses one keyword across positions. TypeScript `readonly` appears at field declarations and in mapped types — same word. Rust `mut` appears at field declarations and in patterns/bindings — same word." Zero citations.

**Recommendation:** When a research file or design cites a comparator language's behavior, the citation must include the language's documentation URL (or spec section) and a verbatim excerpt or a direct code example from the language's docs. Bare prose claim with no excerpt is refused at the lint layer.

---

### F-12: Research files are uneven in research-vs-advocacy character — no structural distinction enforced
**Category:** RESEARCH-QUALITY  
**Severity:** MODERATE  
**Comparison:** Bret Victor / Hillel Wayne "what would change my mind" — explicit falsifiability is a learnable discipline absent in most files

**What:** Some research files explicitly engage with counter-positions and surface what would change the conclusion; others reach a verdict and stop. The skill doesn't structurally distinguish.

**Evidence:**
- `currency-precision-coupling-survey.md` § Open Questions has 4 explicit "we didn't resolve this" items.
- `string-ordering-gap-analysis.md` is explicitly self-revising — the README notes "Initial verdict (since revised by the external + broad surveys)" — this is excellent epistemic practice.
- `xstate.md` / `polly.md` / `linq.md` etc. — comparator surveys that note what the system does well, no explicit "here's where Precept might be wrong to diverge" section.
- `temporal-type-strategy.md` — declares verdict on every type without surfacing "what evidence would force a different choice."

**Recommendation:** Add a `## What would change this conclusion` section as required for research files that propose conclusions. Two-three observations or evidence-shapes that, if encountered, would force re-investigation. Parallel to the `## Falsifiers` section in lifecycle-2-design — Hillel Wayne's school.

---

### F-13: The "Source" category is structurally undifferentiated; vendor docs and peer-reviewed papers carry equal weight
**Category:** RESEARCH-QUALITY  
**Severity:** MODERATE  
**Comparison:** Cochrane evidence-grading — A/B/C levels for evidence strength (systematic review → RCT → expert opinion → vendor claims)

**What:** The skill says "Source is an open category — anything with a permanent address that informed the design qualifies." This is true but loses weighting. A Stripe vendor doc and a POPL paper count identically as "sources consulted."

**Why it matters:** When a design's only Precedent is a vendor blog post, the strength of the grounding is materially different from a design citing an ISO standard or a peer-reviewed paper. The skill is silent on this.

**Evidence:** `field-never-set-diagnostic.md` Decision 5 cites "Rust `mut`, TypeScript `readonly`, Kotlin `val`/`var`, SQL `GRANT UPDATE`" without distinguishing reference strength among them. `currency-precision-coupling-survey.md` cites Joda-Money Javadoc (vendor) alongside IFRS IAS 21 (international accounting standard) alongside Stripe API docs (vendor) — all weighted equal in the verdict.

**Recommendation:** Soft grading scheme in the citation discipline: "Primary" (standards, peer-reviewed papers, authoritative library docs with public versioning), "Secondary" (vendor blogs, marketing docs, community implementations), "Tertiary" (forum posts, knowledge claims). Designs with only tertiary sources on a load-bearing decision are CONCERNs in review.

---

### F-14: The "Implementation State" field for research is missing — research files don't carry status the way docs/ files do
**Category:** DISCOVERABILITY  
**Severity:** MINOR  
**Comparison:** TC39 stage tracking — every proposal has a stage indicating maturity; readers know where in the funnel it is

**What:** `docs/` files have status taxonomy ("Implemented / Canonical design / Design / Stub / Archived"). Research files don't. A reader can't tell whether a research file is "live, conclusions still informing decisions" or "stale, conclusions superseded" without reading the linked design.

**Evidence:** Most research files have a date/author header but no `Status:` field. `temporal-type-strategy.md` is "Synthesis document — consolidates five rounds of design discussion into a unified forward-looking strategy" — useful but not a status enum.

**Recommendation:** Add a `Status:` field to the research-skill template. Values: `Active` (informing current decisions), `Promoted` (decision adopted into spec/design), `Cited` (referenced from another doc but not yet decision-adopting), `Stale` (predates major redesign), `Superseded by: <link>`, `Archived`.

---

### F-15: The reviewer's Source Verification (§ 13) is post-hoc and doesn't catch missing-source claims
**Category:** GAP-DETECTION  
**Severity:** MINOR  
**Comparison:** Peer review at academic venues — referee names sources the author should have engaged but didn't

**What:** § 13 verifies cited sources. It doesn't independently catalog the sources the design or research should have consulted but didn't. The "Mandatory source-checking by change category" table partly addresses this but is keyed to change categories, not topic comparators.

**Evidence:** A design claim about Rust's `mut` keyword would be invisible to the reviewer's source-verification pass unless the reviewer independently catalogs Rust references that exist (which the design isn't required to have cited).

**Recommendation:** Add a "Mandatory comparator-checking by topic" table to precept-reviewer (parallel to the change-category table): "Design about access modifiers → check Rust, TypeScript, Kotlin, Java references documents," "Design about temporal types → check Joda-Time, java.time, Python datetime, NodaTime references," etc. Reviewer notes whether the design cited these or didn't.

## Phase 8+ candidates

### Proposed Phase 8: Operationalize Phase 4 — close the rhetorical-vs-real gap

**Goal:** Make the citation and decision-discipline rules in lifecycle-2-design actually bind. Retrofit at least one in-tree locked design, and add the mechanical checks that prevent regression.

**Findings addressed:** F-1, F-2, F-3, F-9, F-11

**Effort:** 2-3 days

**Tasks:**

1. **Retrofit `docs/Working/field-never-set-diagnostic.md` to Phase 4 standard.** Add `sources-consulted` frontmatter; classify each of the 6 decisions with `Stakes:`; add "Sources consulted for this decision" leg with verbatim excerpts on every claim about Rust / TypeScript / SQL / Kotlin / "the spec"; add `## Falsifiers` section (since this is an external-author-visible language change); add "Strongest counter-evidence" leg on the high+ stakes decisions (Decision 5 minimum). Eat the dog food per the corpus-improvement-plan's stated principle.
2. **Add docs-lint check for locked-design completeness.** Phase 0 deferred Cl, but Phase 8 can ship a minimal lint that runs locally: for any `docs/Working/*.md` with `status: Locked`, check (a) `sources-consulted` frontmatter present and non-empty; (b) every decision has `**Stakes**: ` line; (c) every medium+ decision has `Sources consulted for this decision:` leg; (d) every high+ decision has `Strongest counter-evidence:` leg; (e) any irreversible decision has matching `## Falsifiers` section. Wire as a pre-commit hook even without CI.
3. **Implement `--from <research-doc>` mechanically or remove the claim.** Either build the integration (lifecycle-2-design ingests research conclusions, pre-populates Decision legs with `grounded-by:` frontmatter) or drop the rhetorical claim and replace with explicit human-discipline obligation.
4. **Add lint check that designs in `docs/Working/` touching a topic with existing research must cite that research.** Mechanical heuristic: if a design references "temporal" or "money" or "currency" in its prose, the doc must cite the corresponding `research/` file in frontmatter. Honest "no research consulted — pure-policy choice" override is acceptable but must be explicit.

**Exit criteria:**

- [ ] At least one in-tree locked design (`field-never-set-diagnostic.md`) carries the full Phase 4 leg structure
- [ ] docs-lint refuses locked designs missing the Phase 4 structure
- [ ] No locked design in `docs/Working/` fails the lint without an explicit allow-listed exception (with tracked debt issue)
- [ ] lifecycle-2-design's `--from` claim is either operational or removed

**Doc-touch obligations:**

- `docs/Working/field-never-set-diagnostic.md` — retrofit
- `.claude/skills/lifecycle-2-design/SKILL.md` — clarify `--from` status
- `tools/Precept.DocsLint/` (or equivalent) — Phase 4 leg checks
- `docs/contributing/docs-lint.md` — document new checks

**Risk:**

The retrofit may surface that the design's Decision 5 (keyword unification) wasn't actually grounded in cited Rust / TypeScript / Kotlin / SQL behavior — it was a plausible-sounding argument. Surfacing this is the point; the corpus needs to confront whether the existing design holds up under Phase 4 scrutiny. Mitigation: if the retrofit reveals the grounding doesn't exist, that's a finding to surface to the owner, not a reason to skip the retrofit.

---

### Proposed Phase 9: Strengthen lifecycle-1-research into a real Stage 1

**Goal:** Add refusal gates and required sections to the research skill so it produces artifacts at the level the system claims they're produced at.

**Findings addressed:** F-2, F-3, F-8, F-12, F-13, F-14

**Effort:** 2 days

**Tasks:**

1. **Add behavioral guards section to `lifecycle-1-research/SKILL.md`.** Numbered, refusal-shaped, parallel to lifecycle-2-design's 15 guards. Minimum set:
   - Refuse research that cites zero external sources on a question that has external answers (with explicit "no external state informed this — purely internal exploration" exit acceptable but must be declared).
   - Refuse research claims without per-claim verbatim excerpt and stable source identifier.
   - Refuse research without explicit `## Methodology` section naming what was searched and what was excluded.
   - Refuse research without explicit `## Threats to Validity` section (or "no threats identified — flag for review" as honest exit).
   - Refuse research promoting a conclusion without `## What would change this conclusion` section (Hillel Wayne school, parallel to Falsifiers in lifecycle-2-design).
2. **Add `Status:` taxonomy to research-file frontmatter.** `Active | Promoted | Cited | Stale | Superseded by: <link> | Archived`. Document in the skill body and apply retroactively as opportunistic work.
3. **Add source-grading guidance.** Primary (standards, peer-reviewed, authoritative versioned docs) / Secondary (vendor docs, community implementations) / Tertiary (forum, knowledge claims). Reviewer downgrades load-bearing decisions grounded only on tertiary sources.
4. **Add required external-citation discipline.** Every external URL citation: full quoted excerpt verbatim; access date; stable identifier when standards / academic / RFC; preferred local mirror at `research/references/` for load-bearing sources.

**Exit criteria:**

- [ ] `lifecycle-1-research/SKILL.md` has explicit numbered behavioral guards
- [ ] Skill enforces methodology, threats-to-validity, and falsifiability sections
- [ ] Research files going forward use the Status field; ≥10 existing files retrofitted opportunistically
- [ ] Source-grading guidance ships and is referenced from precept-reviewer's research-doc review path

**Doc-touch obligations:**

- `.claude/skills/lifecycle-1-research/SKILL.md` — substantial expansion
- `.claude/agents/precept-reviewer.md` — research-doc review section, source-grading
- `research/README.md` — Status field convention; cross-link to skill changes
- 10 sampled research files — opportunistic Status-field retrofit

**Risk:**

Adding refusal gates retroactively defines existing research as substandard. Mitigation: explicit grandfather rule — existing research files predate the gates; new gates apply to research produced after a cutoff date. The retroactive opportunistic work is upgrade, not validation gate.

---

### Proposed Phase 10: Promote-or-cite enforcement and corpus archival sweep

**Goal:** Make the promote-or-cite rule binding. The corpus contains substantive research that's neither promoted nor cited — shadow policy by the skill's own definition. Sweep, decide, document.

**Findings addressed:** F-5, F-7, F-10

**Effort:** 2-3 days

**Tasks:**

1. **Inbound-citation audit across all `research/` files.** For every research file, identify: (a) inbound citations from `docs/`, (b) inbound citations from another `research/` file, (c) inbound citations from a sample or test. Produce `research/audit-promote-or-cite-2026-MM-DD.md` listing the audit results.
2. **For each shadow-policy file (no inbound citations), three options:**
   - **Promote.** The conclusion is adopted into a canonical doc; the research file remains as evidence. Effort: tactical doc-update per file.
   - **Cite.** The research is referenced from a proposal issue, design doc, or another research file's findings. Effort: add the cross-link.
   - **Archive.** Move to `research/archive/` with a one-line note explaining why it didn't ship. The skill mandates this for content that doesn't promote/cite.
3. **Sub-folder taxonomy correction.** Move mis-filed research per F-7:
   - `parser-combinator-scalability.md` → `research/architecture/compiler/`
   - `precept-language-mcp-audit.md`, `precept-language-tool-architecture.md` → `research/architecture/` (or new `research/tooling/`)
   - `philosophy-refresh-assessment.md` → `research/philosophy/`
   - `ucum-tier1-curation.md` → `research/language/references/`
4. **Add `research/INDEX.md` topic index.** Topic → file map across the full corpus. Anchored from `research/README.md`. Maintenance obligation added to lifecycle-1-research skill.
5. **Add docs-lint check for shadow research.** Every file in `research/` (except `research/archive/`) must have ≥1 inbound citation from `docs/` or `research/`. Files without inbound citations after N days fail the lint with a tracked-debt allow-list.

**Exit criteria:**

- [ ] Inbound-citation audit committed; every `research/` file categorized as Promoted / Cited / Archived
- [ ] Mis-filed research relocated per the skill taxonomy
- [ ] `research/INDEX.md` exists with cross-folder topic-to-file map
- [ ] docs-lint enforces inbound-citation discipline

**Doc-touch obligations:**

- `research/audit-promote-or-cite-2026-MM-DD.md` — new
- `research/INDEX.md` — new
- Multiple research files — relocations + Status-field adoption
- `research/README.md`, `research/language/README.md`, `research/architecture/compiler/README.md` — index cross-link
- `.claude/skills/lifecycle-1-research/SKILL.md` — maintenance obligation

**Risk:**

Bulk relocations break inbound links from `docs/`. Mitigation: `git mv` preserves history; grep-and-update every `research/...` reference in `docs/` in the same PR. The audit is the riskier work because it may surface that the corpus is more shadow-policy than the team expects — surface honestly to owner.

---

### Proposed Phase 11: Research-adequacy gate at design lock

**Goal:** Close the F-6 gap — a design with `irreversible` stakes can't lock without verifiable prior-art research for the comparable-systems claims it makes.

**Findings addressed:** F-6, F-15

**Effort:** 1-2 days

**Tasks:**

1. **Add Stage 4 (Locked) advancement criterion to lifecycle-2-design.** A design with any `Stakes: irreversible` decision must either (a) cite a research file in `research/` that surveyed the relevant comparable systems with verbatim excerpts, or (b) carry an inline survey leg meeting the same discipline (verbatim per-comparator excerpt, access date, stable identifier).
2. **Add reviewer "Mandatory comparator-checking by topic" table to precept-reviewer.md** (parallel to the change-category table in § 13's mandatory-source-checking). Topic → mandatory comparators:
   - Access modifiers → Rust references, TypeScript references, Kotlin references, Java references
   - Temporal types → Joda-Time / java.time / Python datetime / NodaTime / chrono references
   - Money/currency → Joda-Money / JSR-354 / NodaMoney / Stripe / Adyen references
   - Constraint composition → CEL / OPA / CUE / FluentValidation references
   - State machines → xstate / Stateless.NET / SCXML references
3. **Add docs-lint check.** For any Locked design with irreversible decisions, verify the design cites `research/` files OR carries inline-survey legs that name the topic's mandatory comparators.

**Exit criteria:**

- [ ] lifecycle-2-design has explicit research-adequacy gate at Locked-advancement
- [ ] precept-reviewer carries the topic-to-comparator table
- [ ] docs-lint enforces the gate

**Doc-touch obligations:**

- `.claude/skills/lifecycle-2-design/SKILL.md` — research-adequacy gate
- `.claude/agents/precept-reviewer.md` — comparator-checking table
- `tools/Precept.DocsLint/` — gate check
- `research/INDEX.md` — referenced from gate to verify research-file existence

**Risk:**

The gate could over-fire on small designs that touch a topic but don't make load-bearing comparable-systems claims. Mitigation: gate triggers only on `irreversible` stakes or on explicit comparable-systems-claims-in-prose detection. Honest "no comparable-systems research consulted — purely Precept-internal choice" exit is acceptable.

## What the research system does well

- **Currency-precision-coupling-survey is genuinely excellent.** Joda-Money / JSR-354 / Stripe / Adyen / COBOL / IFRS — 11 systems engaged with verbatim excerpts, then explicit position-by-position analysis, then a Position 3 recommendation grounded in the strongest counter-evidence (JSR-354 deliberate decoupling). This is the bar. If every research file met this bar, the system would deliver on its claims.
- **Multi-round research convergence works.** The string-ordering file sequence (gap-analysis → external-survey → broad-use-cases → architectural-analysis) is honest self-revision — initial verdicts refined as evidence accumulated. The README captures the revision arc. This is good epistemic practice.
- **The /lifecycle-1-research skill's folder discipline section is correct in intent.** The "Determine the Right Folder" table is the right model even though it isn't enforced. Brand/UX research properly excluded; cross-domain synthesis properly accommodated.
- **The currency survey → spec → readiness plan triple-citation chain is the success case.** When the system works end-to-end, the citation density and the verbatim-excerpt discipline visibly survive the Stage 1 → Stage 2 → canonical-doc handoff. The corpus has at least one example of the discipline binding.
- **Substantive engagement with the right intellectual traditions.** `domain-integrity-formal-concept.md` engages with C.J. Date, Fowler, Greg Young, Microsoft .NET DDD guidance. `formal-spec-languages-comparators.md` engages with Alloy/TLA+/Event-B/Z. `parser-combinator-scalability.md` engages with Ford 2004 PEG paper. The author has done the reading. The system fails to require it; it doesn't fail because the author hasn't done it.

## Anchor-by-anchor comparison

| Tradition | Precept research practice lands at... | Specific gap | Grounded finding |
|---|---|---|---|
| **Rust RFC "Prior art" section** | Below — RFC discipline requires comprehensive comparable-systems engagement; Precept research often picks the comparators the author already knew | No required mandatory-comparator-by-topic list | F-4, F-15 |
| **TC39 Stage 1 References** | Below — TC39 requires implementer commitment and prior-art survey before Stage 1 advancement; Precept has no Stage-1 quality gate | No refusal gate in lifecycle-1-research | F-2, F-6 |
| **POPL/PLDI/ICFP Related Work** | Mixed — best Precept research (currency, formal-spec, parser-combinator) meets this bar; majority don't | No required Related Work / Threats-to-Validity sections | F-8, F-12 |
| **AWS PRFAQ + Working Backwards** | Far below — PRFAQ demands customer-evidence and adoption-evidence; Precept product research has 3 files and none follows PRFAQ structure | No customer-evidence requirement in research/product/ | F-5 (orphans), F-12 |
| **Kitchenham/Petersen systematic review** | Far below — no protocol section (research question, inclusion/exclusion, search strategy, extraction template, threats) is required | No systematic methodology required | F-2, F-8 |
| **Bret Victor / Hillel Wayne "what would change my mind"** | Far below — no falsifiability obligation on research conclusions; only `Open Questions` (different shape) | No "What would change this conclusion" section in research | F-12 |
| **Cochrane Collaboration evidence synthesis** | Far below — no evidence-grading; vendor docs and standards count equal | No source-strength taxonomy | F-13 |
| **Roslyn architecture decision records** | Better than most — Roslyn has the design-doc artifact but doesn't structurally separate research; Precept's `research/` folder structurally separates research from design (good) | The separation works in structure but not in discipline | F-1, F-9 |

## Final recommendation

If only 3 phases can ship, prioritize:

1. **Phase 8 (Operationalize Phase 4)** — close the rhetorical-vs-real gap. The single most damaging finding is that locked designs ignore the Phase 4 rules. Until that's closed, every other improvement is layered on a foundation that doesn't bind. The retrofit of `field-never-set-diagnostic.md` is the structural commitment that the rules are real.
2. **Phase 10 (Promote-or-cite enforcement + corpus sweep)** — the corpus has substantive shadow policy. The audit forces an honest reckoning with which research is policy, which is evidence, and which is orphaned. The sub-folder corrections and `research/INDEX.md` close the discoverability gap concurrently.
3. **Phase 9 (Strengthen lifecycle-1-research)** — without this, future research continues to vary in quality, and the gap between best (currency survey) and average (xstate.md) stays wide. Refusal gates and required sections are the structural lift.

Phase 11 (research-adequacy gate) is the natural Phase 12+ follow-up — it requires the corpus to be in a state where the gate can be enforced (Phases 8-10 produce that state).

The corpus deserves the investment. The author has produced genuinely high-quality work that the system does not require and does not enforce. The improvement is making the system match the best-case practice the author is already capable of.
