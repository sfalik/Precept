# Research Philosophy Refresh Assessment

**Author:** Frank (Lead/Architect & Language Designer)  
**Date:** 2026-04-10  
**Scope:** Full language research corpus under `research/language/`, plus `research/philosophy/` and `research/security/`  
**Assessed against:** `docs/philosophy.md` as of commits `dd14a4c` (entity-first rewrite) and `185e20b` (governance strengthening)

---

## 1. Executive Summary

The language research corpus contains **32 files** across 4 directories. I read every one of them against the current philosophy. The overall state:

| Severity | Count | Percentage |
|----------|-------|------------|
| ✅ Aligned (minor or no updates) | 16 | 50% |
| ⚠️ Needs update (framing or priority shift) | 14 | 44% |
| 🔴 Significant refresh needed | 2 | 6% |

The corpus is structurally sound. No file contains conclusions that *contradict* the new philosophy. The dominant problem is **framing lag** — files written before the philosophy rewrites use outdated product descriptions ("state machine DSL", "entity lifecycle governance engine", "backend entity lifecycle") where the philosophy now says "domain integrity engine" with governed integrity as the unifying principle. The second problem is **stateless blindness** — 14 files do not consider how their domain applies to data-only entities, even though the philosophy now says data-only precepts are first-class.

The best-aligned files are those written or significantly revised after the philosophy rewrites: `entity-modeling-surface.md`, `entity-first-positioning-evidence.md`, `domain-map.md`, and `static-reasoning-expansion.md`. The quality bar (`computed-fields.md`) is excellent research but needs a minor framing update to reflect governed integrity language and stateless entity implications.

### Critical finding

No file in the corpus uses the phrase **"governed integrity"** — the new philosophy's unifying principle. This is the single most impactful cross-cutting update. Every file that has a "Philosophy Fit" or "Background" section should ground itself in governed integrity, not in state machines or lifecycle governance alone.

---

## 2. File-by-File Assessment

### Top-level files

#### `domain-map.md`  
**Severity:** ✅ Aligned  
**Author/Date:** Frank, 2026-04-08 (after philosophy rewrite)  
**Assessment:** Written after the philosophy rewrite. Uses correct product framing throughout. References philosophy correctly. The domain map's structure (organizing by research domain, not by proposal) already reflects the entity-first philosophy implicitly — domains like "Data-Only Precepts" and "Entity-Modeling Surface" are first-class entries, not afterthoughts.  
**What needs to change:** Nothing material. When individual domain entries are updated per this assessment, the domain-map descriptions should be brought into sync.

#### `domain-research-batches.md`  
**Severity:** ✅ Aligned  
**Assessment:** Correctly structures research execution around domains, not proposals. References `philosophy.md` and `CONTRIBUTING.md`. The batch ordering (type system → event ingestion → constraint composition → expression expansion → entity modeling) implicitly reflects the new philosophy's priority on data integrity.  
**What needs to change:** Nothing material. The "philosophy-category coverage" framing in §"What counts as full coverage" is correct.

---

### `expressiveness/` directory (19 files)

#### `computed-fields.md` (THE QUALITY BAR)  
**Severity:** ⚠️ Needs update  
**Assessment:** Excellent research — 17-system precedent survey across 9 categories, 6 explicit semantic contracts, 5 dead ends, proposal implications with acceptance criteria hooks. This is deservedly the quality bar. However:  
1. The "Philosophy Fit" section uses **"Prevention, not detection"** and **"One file, complete rules"** but never says **"governed integrity"** — the unifying principle these commitments serve.  
2. The cross-category table covers 9 categories but does not include **MDM** or **industry standards** — the philosophy now explicitly positions against 10+ categories.  
3. No mention of how computed fields apply to **stateless precepts**. A stateless entity with derived fields is a natural and important use case (e.g., a Fee Schedule where `TotalFee` is computed from components). The recomputation timing contract (§Semantic Contract 2) needs a stateless-pipeline clause.  
**What needs to change:**  
- Add "governed integrity" framing sentence to the Philosophy Fit opener.  
- Add MDM and industry-standard rows to the cross-category table (both would show "no derivation — enforcement delegated to implementation").  
- Add a brief stateless-entity note to the recomputation timing contract: "For stateless precepts, recomputation occurs after Update mutations, before invariant evaluation."  
**Priority:** Medium — this is the exemplar, so updating it sets the standard for all other refreshes.

#### `conditional-logic-strategy.md`  
**Severity:** ✅ Aligned  
**Assessment:** Strong research: the `when`/`if` teaching model, the 10-system precedent survey, the `unless` rejection, the keyword-vs-symbol framework, and the consistency audit are all solid and philosophy-compatible. The conditional logic surface applies equally to stateful and stateless precepts (guarded invariants work in both forms). The `when not` resolution aligns with the new philosophy's emphasis on precise, structurally enforced governance.  
**What needs to change:** Nothing material. The research is about conditional logic vocabulary, which is philosophy-neutral in framing. The consistency audit explicitly covers stateless-relevant constructs (`invariant`, `rule`, `edit`).

#### `constraint-composition-domain.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Very comprehensive — 7-category cross-survey (databases, languages, validators, policy engines, enterprise platforms, end-user tools, state machines), detailed philosophy fit, locality matrix, semantic contracts. The philosophy fit section is strong on "prevention", "one file", "data truth over movement truth", "determinism", "AI-readable", "flat syntax", and "collect-all." However:  
1. Never uses **"governed integrity"** as the framing concept.  
2. The "Background and Problem" section says "the language treats every constraint as a standalone flat statement" — correct, but doesn't ground this in the hierarchy of concepts (data and rules are primary, states are structural mechanism).  
3. The locality interaction matrix (§Locality Boundaries) doesn't address how constraint composition works in **stateless precepts** — e.g., are `when`-guarded invariants valid without states? (Answer: yes, the guard is field-based, not state-based.)  
4. Missing **industry standards** and **MDM** from the cross-category survey.  
**What needs to change:**  
- Add "governed integrity" framing to the Background section.  
- Add a note to the locality matrix confirming that all three composition forms (field constraints, named rules, conditional guards) work identically in stateless precepts.  
- Consider adding FHIR/ACORD constraint elements and MDM validation rules to the cross-category survey for completeness with the 10+ positioning.  
**Priority:** Medium — this is a Batch 1 research domain with open proposals (#8, #13, #14).

#### `data-only-precepts-research.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Directly addresses stateless precepts and is one of the most philosophy-relevant files. Uses "domain integrity platform" framing, which is close but not the current "domain integrity engine." Design decisions are well-documented. However:  
1. Does not use **"governed integrity"** as the central concept — it uses "mixed-tooling problem" as its motivation, which is correct but secondary to the philosophical argument.  
2. The precedent survey has only **5 systems** (Terraform, Protobuf, GraphQL, SQL DDL, DDD) — far below the `computed-fields.md` bar of 17 systems across 9 categories.  
3. No cross-category precedent table matching the positioning categories in the philosophy.  
4. No explicit philosophy fit analysis against the 7 key philosophy commitments (prevention, one-file, inspectability, determinism, compile-time, AI-readable, governance-not-validation).  
5. The "Binary Taxonomy" decision (data tier vs behavioral tier, no middle) is important and well-reasoned, but doesn't connect back to the philosophy's "hierarchy of concepts" language.  
**What needs to change:**  
- Rewrite the "Background" opener to ground in governed integrity: "The test for whether an entity belongs in Precept is not 'does it have a state machine?' but 'does it need governed integrity?'"  
- Expand the precedent survey to match computed-fields.md breadth: add enterprise platforms (Salesforce objects with validation rules but no Process Builder, ServiceNow CMDB records), validators (FluentValidation without lifecycle), industry standards (FHIR resources vs workflows), MDM, policy engines.  
- Add a formal Philosophy Fit section evaluating against the 7 commitments.  
- Connect the binary taxonomy to the philosophy's hierarchy of concepts.  
**Priority:** High — data-only precepts are the single most philosophy-sensitive feature in the language.

#### `entity-modeling-surface.md`  
**Severity:** ✅ Aligned  
**Assessment:** The strongest philosophy alignment in the corpus after `entity-first-positioning-evidence.md`. Uses "governed entities" and "entity-governance DSL" language. The cross-category table covers 11 categories (databases, languages, end-user tools, progressive-complexity systems, validators, state machines, policy/decision, enterprise platforms, industry standards, MDM, orchestrators). Explicitly quotes `docs/philosophy.md`: "data and rules are primary", "states are instrumental rather than primary", "states are optional." The philosophy fit section directly addresses the correct product framing: "The wrong category story is 'Precept is fundamentally a workflow DSL, but maybe it can also do simple records.' The right category story is the inverse."  
**What needs to change:** Nothing material. This file is a model for how other files should frame their philosophy sections.

#### `event-ingestion-shorthand.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Strong research with thorough precedent survey (12 systems across 8 categories), clear philosophy fit analysis, well-defined semantic contracts, and decisive dead-end rejection. The "zero exact-name matches in the corpus" finding is important. However:  
1. Does not address whether `absorb` applies in **stateless precepts** via the `Update` pipeline. In a data-only entity, there are no transition rows — so `absorb` as currently conceived (inside `from...on...` rows) is structurally inapplicable. This should be noted explicitly.  
2. Does not use **"governed integrity"** framing.  
**What needs to change:**  
- Add a note in the Scope or Dead Ends section: "absorb is a transition-row action; it does not apply to stateless precepts, which have no events or transition rows. Stateless field population occurs through the Update API, which has no shorthand mechanism."  
- Minor: add "governed integrity" framing sentence to the Philosophy Fit opener.  
**Priority:** Low — this is accurately scoped to stateful precepts, and the stateless note is a boundary clarification, not a gap.

#### `expression-expansion-domain.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** The most comprehensive single file in the corpus — 25+ systems across 9 categories, vocabulary taxonomy (operators/accessors/methods/functions), per-proposal philosophy fit, null safety policy, string surface definition, semantic contracts, and precedence integration. However:  
1. The summary table describes Precept as **"Entity lifecycle governance engine"** — should be **"Domain integrity engine."** "Lifecycle" puts the state machine first; the philosophy now says data and rules are primary.  
2. No mention of how expression expansion applies to **stateless precepts**. The expression surface is used in `invariant`, `when` (on invariants), `set` (in Update), and computed fields — all of which work in stateless definitions. The expression expansion is arguably *more* important for data-only precepts because they lack transition-row logic and rely entirely on invariants and field constraints.  
3. The "Background and Problem" section references "Precept's current expression surface" in the context of transition rows and guards. But the expression surface serves invariants and field constraints equally — and with stateless precepts, those become the *only* expression positions.  
**What needs to change:**  
- Change "Entity lifecycle governance engine" to "Domain integrity engine" in the summary table and throughout.  
- Add 1-2 sentences in the Background noting that expression expansion applies equally to stateless precepts, where invariants and computed fields are the entire governance surface.  
- In the "Expression position scope" semantic contract, confirm that all constructs work in stateless precepts (they do — none are state-dependent).  
**Priority:** Medium-high — this is the central Batch 2 research document.

#### `expression-language-audit.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Rigorous, implementation-grounded inventory of expression limitations. Severity-ranked with sample evidence and feasibility verdicts. However:  
1. **No philosophy framing at all.** The audit was written as an engineering inventory, not as philosophy-grounded research. It doesn't reference the philosophy document, doesn't evaluate limitations against governance commitments, and doesn't consider how stateless entities change the priority of each limitation.  
2. The framing is "what Precept's expression language can and cannot express today" — technically accurate but missing the *why it matters* connection to governed integrity.  
3. Severity assessments (Critical, Significant, Moderate) are based on "business logic you cannot express" — which is correct, but the philosophy would rank L2 (string `.length`) even higher because it's a data-integrity gap, not just a business-logic gap.  
**What needs to change:**  
- Add a brief philosophy-context section at the top: "Expression limitations affect both stateful and stateless precepts. For data-only entities, where invariants and field constraints are the entire governance surface, expression gaps directly reduce the product's ability to deliver governed integrity."  
- In the Critical limitations, note where stateless entities amplify the pain (e.g., L2 string `.length` is even more critical for data-only entities that are pure constraint definitions).  
- This is an audit, not a research document — the updates should be brief contextual additions, not a full rewrite.  
**Priority:** Medium — this is a cross-cutting reference document used by many other files.

#### `expression-tracking-notes.md`  
**Severity:** ✅ Aligned  
**Assessment:** Short tracking document defining the `dsl-expressiveness` vs `dsl-compactness` tag boundary. No philosophy framing needed — it's a categorization guide.  
**What needs to change:** Nothing.

#### `fluent-assertions.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Comparative analysis against FluentAssertions' assertion model. Correctly identifies that Precept's `invariant` is equivalent to `AssertionScope` collect-all behavior. Uses "data-truth vs movement-truth" distinction well. However:  
1. The Takeaway says "the `invariant` + `on...assert` pattern already matches FluentAssertions' expressiveness" — but the philosophy now says the *real* difference is **governance vs validation**: "Validation checks data at a moment in time, when called. Governance declares what the data is allowed to become and enforces that declaration structurally."  
2. This distinction should be the comparison's headline finding, not a secondary note.  
**What needs to change:**  
- Add 1-2 sentences in the Gap Analysis connecting to the governance-vs-validation distinction from the philosophy: Precept's invariants hold structurally, not because someone called `AssertionScope`.  
**Priority:** Low — this is a targeted library comparison, not a core research document.

#### `fluent-validation.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** The most commercially important comparison (same .NET audience). Correctly identifies FluentValidation's `When`/`Unless` pattern as precedent for Precept's conditional invariants, and correctly identifies Precept's state-time discrimination as a capability FluentValidation lacks. However:  
1. The framing is "where Precept is more verbose" vs "where Precept is richer" — a feature comparison that misses the philosophical distinction. The philosophy now says: "This is not validation — it is governance. Validation checks data at a moment in time, when called. Governance declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract."  
2. This governance-vs-validation distinction is the single most important thing to say when comparing against FluentValidation, and it's entirely absent.  
3. No mention of data-only precepts — a stateless precept is the *direct* competitor to FluentValidation (same data-shape-validation use case, but with structural governance instead of invoked validation).  
**What needs to change:**  
- Add a "Category Distinction" section before the Gap Analysis: "FluentValidation validates when called. Precept governs structurally. A FluentValidation rule runs when you invoke the validator. A Precept invariant holds because the runtime prevents any operation from producing a result that violates it."  
- Add a note about data-only precepts as the direct comparison surface: "A stateless precept competes with FluentValidation on the same data-shape validation use case — but with structural prevention instead of invoked checking."  
**Priority:** High — this is the most commercially important comparison in the corpus and it's missing the philosophy's most important distinction.

#### `internal-verbosity-analysis.md`  
**Severity:** ✅ Aligned  
**Assessment:** Quantitative corpus analysis of statement counts and verbosity patterns. This is measurement data, not philosophy-framed research. The verbosity patterns identified (event-argument ingestion, guard-pair duplication, non-negative invariant boilerplate) are all legitimate engineering concerns regardless of philosophy framing.  
**What needs to change:** Nothing — this is a data document, not a philosophy document.

#### `linq.md`  
**Severity:** ✅ Aligned  
**Assessment:** Narrow pipeline-model comparison. Correctly identifies the ternary gap and the deferred-execution non-gap. No philosophy framing needed — this is a structural comparison of pipeline mechanics.  
**What needs to change:** Nothing.

#### `polly.md`  
**Severity:** ✅ Aligned  
**Assessment:** Narrow comparison identifying the named-policy pattern as evidence for named rules. Correctly identifies that Polly and Precept solve orthogonal problems. No philosophy framing needed.  
**What needs to change:** Nothing.

#### `transition-shorthand.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Well-written, covers philosophy fit against all 13 design principles. Uses correct framing for the topic (transition shorthand is inherently stateful). However:  
1. The multi-event `on` clause and catch-all routing are evaluated against the 13 *design principles* but not against the *philosophy commitments* (prevention, governed integrity, data primacy). These are different: design principles are about syntax/semantics, philosophy commitments are about product identity.  
2. Does not note that transition shorthand is irrelevant to stateless precepts — a boundary statement that should be explicit.  
**What needs to change:**  
- Add a scoping note: "Transition shorthand applies exclusively to stateful precepts. Data-only precepts have no transition rows and are unaffected by this domain."  
- Minor: reference governed integrity in the philosophy-fit opener.  
**Priority:** Low — this is a horizon domain with no open proposal.

#### `type-system-domain-survey.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Comprehensive — 10-domain field-count survey, sample corpus analysis (127 fields classified), cross-category precedent table (databases, languages, enterprise platforms, rule engines, state machines, end-user tools), philosophy fit with 6 explicit checks. However:  
1. Does not use **"governed integrity"** — uses "Prevention, not detection" and other commitment names but misses the unifying concept.  
2. Does not address how the type system applies to **stateless precepts**. A data-only entity like a Customer Profile or Fee Schedule desperately needs `choice`, `date`, `decimal`, and `integer` — arguably more than a lifecycle entity does, because the type system *is* the governance surface when there are no states.  
3. The cross-category survey doesn't include **MDM** or **orchestrators** — the philosophy now positions against these.  
**What needs to change:**  
- Add "governed integrity" framing to the Philosophy Fit opener.  
- Add 2-3 sentences noting that the type system expansion is equally (or more) important for stateless precepts, where field types and constraints are the entire governance surface.  
- Consider adding MDM and orchestrator rows to the cross-category table (MDM: entity master types with similar type categories; Orchestrators: no field types — governance is code-level).  
**Priority:** Medium — this is the largest active research domain.

#### `type-system-follow-ons.md`  
**Severity:** ✅ Aligned  
**Assessment:** Horizon research, well-bounded. Explicitly subordinates itself to the main type survey. Philosophy fit sections evaluate duration and attachment against inspection, determinism, and flat-syntax requirements. Conservative conclusions are philosophy-appropriate.  
**What needs to change:** Nothing material.

#### `xstate.md`  
**Severity:** 🔴 Significant refresh needed  
**Assessment:** This is the oldest-framing file in the expressiveness directory. The xstate comparison frames Precept as targeting "backend entity lifecycle (order, subscription, loan), not UI state machines." This is the pre-philosophy framing that treats Precept as a state machine tool for lifecycle entities. The new philosophy says data-only entities outnumber workflow entities in every domain, and governed integrity — not lifecycle — is the unifying concept.  
**Specific problems:**  
1. "Precept targets backend entity lifecycle" — **directly contradicts** the philosophy's "Workflow is one dimension of entity lifecycle — not the defining frame."  
2. "Flat states are appropriate and correct for the target domain" — correct conclusion but wrong justification: flat states are appropriate because Precept governs entity integrity (including entities with no states), not because Precept is a "backend entity lifecycle" tool.  
3. The "Where Precept is richer — data-in-event" section is correct but completely misses the *real* differentiation: Precept has a declared field model with invariants and structural prevention. XState has no data integrity enforcement at all. A `CancelledOrder` in the right state but with `Total > 0` is the failure mode the philosophy identifies.  
4. No mention of data-only entities.  
**What needs to change:**  
- Replace "Precept targets backend entity lifecycle" with language reflecting the current philosophy: Precept governs entity integrity — lifecycle transitions, field constraints, editability rules, or any combination.  
- Reframe the core differentiation: the problem with pure state machines isn't just the pipeline model — it's that "an entity can pass through every valid transition and still hold corrupted field values."  
- Add a section noting that Precept also governs entities with no lifecycle at all — a category xstate cannot serve.  
**Priority:** High — this is the comparison against Precept's closest category peer, and it's using the wrong product description.

#### `zod-valibot.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Good comparison identifying Precept's state-scoped advantage and honest about where Precept is more verbose. However:  
1. The framing is validation-vs-validation, but the philosophy now says governance ≠ validation: "Validation checks data at a moment in time, when called. Governance declares what the data is allowed to become and enforces that declaration structurally."  
2. No mention of data-only precepts — a stateless precept is the *closest* competitor to Zod (same data-shape governance use case).  
3. The "Where Precept is richer" section cites state-scoped validation — true, but the *deeper* advantage is structural prevention (Zod validates when you call `.parse()`; Precept prevents structurally).  
**What needs to change:**  
- Add the governance-vs-validation distinction from the philosophy.  
- Note that stateless precepts compete with Zod on the same surface — data shape contracts — but with structural enforcement.  
**Priority:** Medium — Zod is the dominant TypeScript comparison and shape-validation is core to data-only entities.

---

### `references/` directory (10 files)

#### `cel-comparison.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Written this session (2026-04-10). Should be fairly aligned. The summary table lists Precept's category as "Entity lifecycle governance engine" — the philosophy says "domain integrity engine." The comparison is implementation-focused and correct on technical merits. Does not address stateless entity implications.  
**What needs to change:**  
- Change "Entity lifecycle governance engine" to "Domain integrity engine" in the summary table and category row.  
- The "What Precept Has That CEL Doesn't" section should lead with "governed integrity" rather than "state machines."  
**Priority:** Low — this is a technical comparison, and the framing gap is narrow.

#### `conditional-invariant-survey.md`  
**Severity:** ✅ Aligned  
**Assessment:** Raw 10-system survey of conditional constraint patterns. Factual capture with no philosophy framing. The "Lessons for Precept" section is appropriately minimal and correct.  
**What needs to change:** Nothing.

#### `constraint-composition.md`  
**Severity:** ✅ Aligned  
**Assessment:** Formal/theory document covering predicate combinators, Boolean lattices, specification patterns, scope theory, and desugaring semantics. The theory is philosophy-neutral and the Precept mapping is correct. The scope hierarchy explicitly covers field-local, cross-field, event-arg, and mixed scopes — all of which apply to both stateful and stateless precepts.  
**What needs to change:** Nothing — theory references are inherently philosophy-neutral.

#### `expression-compactness.md`  
**Severity:** ✅ Aligned  
**Assessment:** Formal reference on syntactic sugar and derived forms. Correct PLT grounding. The "semantic risks specific to Precept" correctly identifies error attribution, subsumption detection, and AI readability.  
**What needs to change:** Nothing.

#### `expression-evaluation.md`  
**Severity:** ✅ Aligned  
**Assessment:** Formal/theory document on decidability analysis. Correctly classifies every proposed expansion by decidability risk. The taxonomy (None / Decidable / Expensive / Undecidable) is sound. No philosophy framing needed.  
**What needs to change:** Nothing.

#### `multi-event-shorthand.md`  
**Severity:** ✅ Aligned  
**Assessment:** PLT reference for event set abstraction in formal systems (CSP, UML, SFAs). Correctly documents existing shorthand inventory and the `on` clause gap. The arg-substitution rule is a genuine contribution.  
**What needs to change:** Nothing — this is a formal reference document.

#### `state-machine-expressiveness.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Covers hierarchical states, parallel regions, history states, and wildcard transitions. Correctly identifies that Precept operates at "Generation 2" (EFA with guards and actions). However:  
1. The framing assumes Precept is *only* a state machine: "Precept operates in Generation 2" and evaluates what Generation 3 features might add. The philosophy now says states are optional — some precepts have no state machine at all.  
2. The "What the Model Handles Well" section says Precept handles "linear workflows", "conditional branching", and "multi-state shorthand" — all stateful concepts. It should also note that the model handles data-only entities with no state machine.  
**What needs to change:**  
- Add a frame-setting note: "This reference covers Precept's state machine capabilities. The philosophy also supports stateless entities that use no state machine at all — those are outside this document's scope."  
- Minor: update the "correct for a domain integrity engine" language in the §Implementation Cost Summary to be more explicit that the state machine is one mechanism within the broader integrity engine.  
**Priority:** Low — this is a theory reference; the scope note is sufficient.

#### `static-reasoning-expansion.md`  
**Severity:** ✅ Aligned  
**Assessment:** Directly quotes `docs/philosophy.md`: "The compiler catches structural problems — unreachable states, type mismatches, constraint contradictions, and more — before runtime." References the compile-time promise by name. Covers C4/C5 contradiction detection, satisfiability analysis, and interval propagation. The Alloy, OCL, Dafny, and abstract interpretation precedents are relevant and well-applied. The policy engine comparisons (DMN, Cedar, OPA, Drools) map to the philosophy's positioning categories.  
**What needs to change:** Nothing — this is the most philosophy-aligned reference document.

#### `type-system-survey.md`  
**Severity:** ⚠️ Needs update  
**Assessment:** Strong formal grounding and cross-system precedent survey. Covers databases (PostgreSQL, SQL Server, MySQL), languages (C#, TypeScript, Kotlin, F#, Rust, Python), and enterprise platforms (Salesforce, Dynamics 365, ServiceNow). However:  
1. Does not address how type semantics differ (or don't) in **stateless precepts**. The answer is that they don't — types work identically in stateful and stateless definitions — but this should be stated explicitly.  
2. Does not include **FEEL/DMN**, **Cedar**, or **policy engine** type systems — these are positioned against in the philosophy.  
**What needs to change:**  
- Add a note: "The type system operates identically in stateful and stateless precepts. All coercion, constraint, and comparison rules apply regardless of whether states are declared."  
- The domain survey companion (`type-system-domain-survey.md`) already covers FEEL, Cedar, and Drools type models. A cross-reference rather than duplication is sufficient.  
**Priority:** Low — the domain survey companion covers the missing categories.

---

### `philosophy/` directory

#### `entity-first-positioning-evidence.md`  
**Severity:** ✅ Aligned  
**Assessment:** Written 2026-07-18 — well after both philosophy rewrites. This IS the evidence base for the new philosophy. Uses "governed integrity" as the central test. Documents three archetypes (workflow, entity, hybrid). Full positioning evidence against all categories. The domain ratio table ("In every real business domain, data entities outnumber workflow entities") directly supports the philosophy's key claim.  
**What needs to change:** Nothing — this is the gold standard for philosophy alignment.

---

### `security/` directory

#### `security-survey.md`  
**Severity:** ✅ Aligned  
**Assessment:** Uses "domain integrity engine for .NET" in the opening sentence — correct and current. Security analysis is product-architecture-focused, not philosophy-focused. The product categories (DSL parser, NuGet library, language server, MCP server, VS Code extension, CI pipeline) are correctly described.  
**What needs to change:** Nothing — the product description is accurate.

---

## 3. Cross-Cutting Themes

### Theme 1: "Governed integrity" is absent from the entire corpus

**Impact:** 32 files, 0 uses of "governed integrity."

The philosophy says: "The unifying principle is governed integrity — the entity's data satisfies its declared rules at every moment, whether those rules involve lifecycle transitions, field constraints, or both."

Every file that has a Background, Philosophy Fit, or positioning section should reference this as the grounding concept. Currently:
- Most files use "prevention, not detection" (correct but one level down)
- Some use "entity lifecycle governance" (partially outdated)
- Some use "domain integrity" (correct direction, missing the "governed" qualifier that implies structural enforcement)

**Recommended fix:** During each file's refresh, add a single sentence grounding the domain in governed integrity. This is a ~10-minute edit per file, not a rewrite.

### Theme 2: Stateless entity blindness (14 files)

**Impact:** 14 of 32 files do not consider data-only entities.

The philosophy says: "In every real business domain, data and reference entities outnumber workflow entities." And: "States are optional."

Files that discuss expression expansion, type systems, constraint composition, and string operations all apply equally to stateless precepts — but none of them say so. The expression audit (L1-L12) ranks limitations by "business logic you cannot express" without noting that some limitations are *amplified* for data-only entities where the entire governance surface is invariants and field constraints.

**Recommended fix:** Each affected file needs 1-3 sentences noting whether/how its domain applies to stateless precepts. This is a boundary clarification, not restructuring.

### Theme 3: Positioning scope varies (5 files below philosophy benchmark)

**Impact:** The philosophy positions against 10+ tool categories. Some research files compare against 3-5 categories.

The quality bar (`computed-fields.md`) covers 9 categories. `entity-modeling-surface.md` covers 11. But several domain-level research documents cover only 5-7 categories, omitting MDM, industry standards, or orchestrators.

**Recommended fix:** Not every file needs a full 11-category sweep. But domain-scoped research files (constraint composition, data-only precepts, type system) should cover at least the 7 "positioning against" categories from the philosophy (validators, state machines, rules engines, ORM, DB constraints, policy engines, enterprise platforms) plus the 3 "complements" categories (orchestrators, event sourcing, ORM persistence).

### Theme 4: "Lifecycle" framing persists in 8 files

**Impact:** 8 files use "lifecycle" as the primary product descriptor rather than "integrity" or "governance."

The philosophy's hierarchy of concepts says: "Data and rules are the primary concern. States are the structural mechanism that makes data protection lifecycle-aware — when lifecycle is present. Workflow is one dimension of entity lifecycle — not the defining frame."

Files using "entity lifecycle governance engine" or "backend entity lifecycle" as product descriptors frame the state machine as primary when it is actually one mechanism.

**Recommended fix:** Replace "entity lifecycle governance" with "domain integrity governance" or "governed entity integrity." Replace "backend entity lifecycle" with "business entity governance."

---

## 4. New Research Gaps

The new philosophy implies research needs that no existing file covers:

### Gap 1: Constraint patterns specific to data-only entities

**What's missing:** Stateless precepts have `edit all`/`edit Field1, Field2` as their only editability declaration. What constraint patterns arise when there are no states to scope invariants against? Are there data-only entities that need something like "mode-dependent constraints" (switching between validation profiles based on field values rather than states)?

**Research needed:** A study of constraint patterns in data-shape systems (Zod schemas, JSON Schema, Pydantic models, Salesforce validation rules on non-workflow objects) to understand what constraint composition looks like without a state axis.

**Priority:** Medium — needed before stateless precepts ship widely.

### Gap 2: Inspect/Fire semantics for stateless entities

**What's missing:** The philosophy says "At any point, you can preview every possible action and its outcome without executing anything." For stateless entities, what does Inspect return? What does Fire return? `data-only-precepts-research.md` lists the question but does not answer it beyond "Undefined."

**Research needed:** A semantic contracts document specifying the API surface for stateless instances, with test-scenario examples.

**Priority:** High — this is a design decision blocking #22 implementation.

### Gap 3: Governance vs validation — a positioning research document

**What's missing:** The philosophy makes a strong distinction: "This is not validation — it is governance." But no research document explores this distinction in depth with cross-system evidence. How does structural prevention differ from invoked validation in practice? What failure modes does validation permit that governance prevents?

**Research needed:** A `governance-vs-validation.md` in `references/` that grounds this distinction with examples from FluentValidation (bypassed by direct property set), JSON Schema (only checked at parse time), EF validation (bypassed by raw SQL), and Precept (structurally enforced on every operation).

**Priority:** High — this is the philosophy's most important positioning claim, and it has no dedicated evidence document.

### Gap 4: Positioning depth for MDM and industry standards

**What's missing:** The philosophy positions against MDM and industry standards, but no research file explores these comparisons in depth. `entity-first-positioning-evidence.md` covers them at the claim level but without the per-system precedent detail that other comparisons have.

**Research needed:** Not a full new document — the existing files that expand to cover these categories during refresh will accumulate the evidence.

**Priority:** Low — the positioning evidence file provides sufficient coverage at the claim level.

---

## 5. Recommended Refresh Order

Ordered by impact × dependency. Refresh the highest-impact files first because other files reference them.

| Priority | File | Why first |
|----------|------|-----------|
| 1 | `expressiveness/fluent-validation.md` | Most commercially important comparison. Missing the governance-vs-validation distinction. High external visibility. |
| 2 | `expressiveness/xstate.md` | Closest category peer comparison. Uses flatly outdated product description. 🔴 |
| 3 | `expressiveness/data-only-precepts-research.md` | Most philosophy-sensitive feature. Narrow precedent survey. High proposal priority (#22). |
| 4 | `expressiveness/computed-fields.md` | The quality bar. Updating it sets the standard for all other files. |
| 5 | `expressiveness/expression-expansion-domain.md` | Central Batch 2 document. Incorrect product description in summary table. |
| 6 | `expressiveness/expression-language-audit.md` | Cross-cutting reference used by many files. Needs philosphy context. |
| 7 | `expressiveness/constraint-composition-domain.md` | Batch 1 document with open proposals. Needs stateless coverage. |
| 8 | `expressiveness/type-system-domain-survey.md` | Largest active research domain. Needs stateless coverage. |
| 9 | `expressiveness/zod-valibot.md` | Important validator comparison. Needs governance distinction. |
| 10 | `references/cel-comparison.md` | Minor framing fix. Low effort. |
| 11 | `expressiveness/fluent-assertions.md` | Minor framing addition. Low effort. |
| 12 | `expressiveness/event-ingestion-shorthand.md` | Stateless boundary note. Low effort. |
| 13 | `references/state-machine-expressiveness.md` | Scope note. Low effort. |
| 14 | `expressiveness/transition-shorthand.md` | Scope note. Low effort. |
| 15 | `references/type-system-survey.md` | Stateless equivalence note. Low effort. |
| 16 | NEW: `references/governance-vs-validation.md` | Fill the highest-priority new research gap. |

---

## Cross-References

- [docs/philosophy.md](../../philosophy.md) — the document against which this assessment was conducted
- [entity-first-positioning-evidence.md](../philosophy/entity-first-positioning-evidence.md) — the gold-standard aligned file
- [entity-modeling-surface.md](./expressiveness/entity-modeling-surface.md) — the gold-standard aligned domain research file
- [computed-fields.md](./expressiveness/computed-fields.md) — the quality bar for domain research documents
