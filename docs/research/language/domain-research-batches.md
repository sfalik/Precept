# Language Research Domain Batches

This document prioritizes the language research corpus by **domain**, not by individual issue. It is the execution plan for building durable research coverage before the next proposal and implementation wave.

## Audit basis

This plan was built from four inputs:

1. the current open proposal queue (`#8`, `#9`, `#10`, `#11`, `#13`, `#14`, `#15`, `#16`, `#17`, `#22`, `#25`, `#26`, `#27`, `#29`, `#31`);
2. the current research corpus under `docs/research/language/`;
3. the philosophy filter in `docs/philosophy.md` plus the proposal requirements in `CONTRIBUTING.md`;
4. the sample corpus in `samples/`.

### Sample signals that matter for research ordering

- **21** sample precepts are currently in the corpus.
- They contain **132** event-to-field assignment statements, which keeps **event ingestion shorthand** high on the list.
- They contain **117** arithmetic assignment statements, which keeps **expression expansion** and **computed fields** relevant.
- They contain **97** basic positive / non-negative constraints, which keeps **field-level constraint composition** relevant.
- Samples such as `clinic-appointment-scheduling.precept` and `library-book-checkout.precept` still model calendar behavior with numeric day counters, which keeps **type system expansion** materially relevant.

## What counts as “full philosophy-category coverage”

A domain is fully researched only when the corpus covers all four of these:

1. **user need** — sample pressure and real domain pressure;
2. **external precedent** — how adjacent systems express the same need;
3. **philosophy fit** — why the feature helps without weakening prevention, inspectability, flat syntax, or first-match routing;
4. **non-goals** — what adjacent systems do that Precept should explicitly refuse.

## Domain status map

| Domain | Open proposals | Current research status | Batch |
|---|---|---|---|
| Type system expansion | `#25`, `#26`, `#27`, `#29` | **Weak / missing.** Proposal bodies reference type-system research files that are not currently present under `docs/research/language/`. | **Batch 1** |
| Event ingestion shorthand | `#11` | **Weak.** Strong internal corpus evidence, but no dedicated external precedent sweep or philosophy-fit writeup. | **Batch 1** |
| Constraint composition | `#8`, `#13`, `#14` | **Partial, but Batch-1 relevant.** Good fragments exist, but `#13` keeps the whole shared-precedent lane in the open-proposal / incomplete-domain packet until the validator / rule / declaration research is normalized together. | **Batch 1** |
| Expression expansion | `#9`, `#10`, `#15`, `#16`, `#31` | **Partial.** The expression audit is strong, but string operations, logical keywords, and function-surface decisions still need one joined precedent pass. | **Batch 2** |
| Entity-modeling surface | `#17`, `#22` | **Partial.** Both proposals have meaningful research, but the category-wide sweep and philosophy framing still need consolidation. | **Batch 2** |
| Static reasoning expansion | none | **Future-relevant.** Directly tied to compile-time guarantees already promised in docs and roadmap. | **Batch 3** |
| Transition shorthand | none | **Future-relevant.** Already called out as a likely next compactness lane by the references corpus. | **Batch 3** |
| Type-system follow-ons | none | **Future-relevant.** Residual gaps are visible, but proposal writing should wait for the main type-system corpus. | **Batch 3** |

---

## Batch 1 — open proposals with the largest active research gaps

These domains already have open proposals, but the durable research corpus is not yet strong enough to support confident sequencing.

### Effort: Type system expansion

**Domains included**

- constrained categorical values;
- calendar dates;
- exact-decimal arithmetic;
- whole-number semantics.

**Open proposals informed**

- `#25` choice type
- `#26` date type
- `#27` decimal type
- `#29` integer type

**Why this belongs in Batch 1**

- The proposal set is active, but the corpus is missing the two research documents the issues already cite: `expressiveness/type-system-domain-survey.md` and `references/type-system-survey.md`.
- This is the highest-blast-radius language lane: parser, checker, evaluator, grammar, language server, MCP surface, samples, and runtime API all move together.
- The sample corpus and PM history both show sustained type pressure, but that evidence is not yet anchored in durable language-research docs.

**Shared precedent sweep**

One external pass should cover all four domains together:

- **entity-definition platforms:** ServiceNow, Salesforce, Dataverse;
- **rule / decision systems:** FEEL, Cedar, Drools, NRules, SQL / PostgreSQL;
- **workflow boundary checks:** Camunda and Temporal, to verify what should stay host-side rather than become a Precept type.

**Research output expected from this pass**

- one domain survey grounded in business entities and sample pressure;
- one type-semantics reference sweep covering operators, coercions, literals, constraints, and non-goals;
- a clear boundary statement for what does **not** enter the type system yet.

### Effort: Event ingestion shorthand

**Domains included**

- intake-style event-to-field mapping;
- explicit-vs-implicit payload hydration in transition chains.

**Open proposals informed**

- `#11` absorb shorthand

**Why this belongs in Batch 1**

- The internal evidence is strong: the sample corpus contains **132** event-to-field assignment statements.
- The external evidence is still thin. The current corpus shows the problem, but it does not yet do the philosophy work on visibility, auditability, silent ignores, or override rules.
- This feature is compactness-positive only if it stays explicit enough that authors and agents can still see what happened.

**Shared precedent sweep**

- **state-machine action models:** xstate `assign()` and adjacent state/context update patterns;
- **entity / form intake systems:** platforms that hydrate record state from submitted payloads;
- **validator posture checks:** tools that deliberately avoid hydration, to sharpen Precept’s non-goals.

**Research output expected from this pass**

- a direct comparison between explicit `set` chains and shorthand hydration patterns;
- a philosophy-fit note on when shorthand hides too much behavior;
- a narrow non-goals section that prevents this from turning into a general mapping DSL.

### Effort: Constraint composition

**Domains included**

- reusable named predicates;
- conditional declarations;
- declaration-local basic constraints.

**Open proposals informed**

- `#8` named rule declarations
- `#13` field-level range / basic constraints
- `#14` `when` guards on declarations

**Why this belongs in Batch 1**

- The lane is no longer just “partially researched later.” `#13` keeps the whole validator / rule / declaration lane in the active open-proposal set, and the current research is still fragmented across multiple files instead of one durable domain packet.
- The proposals share one precedent family — validator DSLs, fluent rule design, and rule / policy systems — so sequencing them apart would force duplicate precedent work.
- `#13` is also the most philosophy-sensitive item in the lane because it pressures the keyword-anchored syntax model. That makes a joined Batch-1 pass more urgent, not less.

**Shared precedent sweep**

- **validator DSLs:** FluentValidation, Zod, Valibot, Joi;
- **readability / fluent rule design:** FluentAssertions;
- **rule / policy systems:** Drools, Cedar, DMN;
- **formal grounding:** Alloy and specification-pattern style predicate reuse.

**What this effort should produce**

- one consolidated model of when a condition belongs in a rule, a declaration guard, or a declaration suffix;
- a locality matrix: field-local, cross-field, event-arg, and forbidden mixes;
- a non-goals list that blocks parameterized rules, hidden control flow, and overgrown suffix vocabularies.

---

## Batch 2 — open proposals with partial research that needs full philosophy-category coverage

These domains already have meaningful research. The work now is to turn fragmented evidence into complete, reusable domain packets.

### Effort: Expression expansion

**Domains included**

- conditional value selection;
- string size and substring tests;
- logical keyword forms;
- bounded built-in functions.

**Open proposals informed**

- `#9` conditional expressions
- `#10` string `.length`
- `#15` string `.contains()`
- `#16` built-in function library
- `#31` logical keywords (`and`, `or`, `not`)

**Why this belongs in Batch 2**

- `expression-language-audit.md`, `expression-evaluation.md`, and `conditional-logic-strategy.md` already give this lane a real foundation.
- The remaining gap is coherence: naming, null policy, keyword-vs-symbol policy, function-call surface area, and “table-stakes vs language bloat” need to be resolved together.
- This should be treated as **one effort**, not five unrelated micro-passes, because every decision here changes the shape of the expression language authors see.

**Shared precedent sweep**

- **validator DSLs:** Zod, Valibot, Joi, FluentValidation;
- **readable assertion vocabularies:** FluentAssertions;
- **expression languages / selectors:** LINQ, FEEL / DMN, Cedar, SQL, Python-style keyword logic where relevant.

**What this effort should produce**

- one vocabulary map for accessors, methods, functions, and operators;
- one null-safety policy for string and function expansion;
- one boundary statement for what remains outside Precept’s expression surface.

### Effort: Entity-modeling surface

**Domains included**

- derived / computed fields;
- data-only precepts with no lifecycle graph.

**Open proposals informed**

- `#17` computed / derived fields
- `#22` data-only precepts

**Why this belongs in Batch 2**

- Both proposals already have dedicated research documents with real precedent and philosophy content.
- What they still need is a category-level synthesis around Precept’s product position: one language for governed entities, whether lifecycle-heavy or lifecycle-light.
- This is not greenfield research anymore; it is consolidation and alignment.

**Shared precedent sweep**

- **declarative data systems:** SQL tables with checks, generated / computed columns;
- **enterprise entity platforms:** Salesforce formula fields, Dynamics calculated columns, ServiceNow calculated fields;
- **progressive-complexity systems:** Terraform, GraphQL, DDD-style value objects / entities;
- **lightweight derivation precedents:** spreadsheets and Pydantic-style computed values.

**What this effort should produce**

- a clean split between “stored-but-derived” and “state-free-but-governed”;
- a positioning note showing how these features extend Precept’s category rather than dilute it;
- explicit non-goals for hidden recomputation, cross-entity magic, and workflow bypass.

---

## Batch 3 — no open proposal yet, but clear future relevance

These domains should be researched after Batches 1 and 2 so proposal writing starts from evidence instead of intuition.

### Effort: Static reasoning expansion

**Domains included**

- same-preposition contradiction detection;
- cross-preposition deadlock detection;
- value-range propagation beyond null narrowing.

**Open proposals informed**

- none yet

**Why this belongs here**

- These capabilities are already called out in `docs/@ToDo.md` and align directly with Precept’s compile-time structural-checking promise.
- They are future-relevant, but they do not currently need an issue body before the research shape is clearer.

**Shared precedent sweep**

- Alloy and OCL;
- abstract interpretation and interval analysis;
- SAT / SMT-backed rule satisfiability work;
- DMN / rule-engine diagnostics where they expose contradiction analysis.

### Effort: Transition shorthand

**Domains included**

- multi-event `on` clauses;
- catch-all event routing forms;
- row-count reduction that preserves explicit state-machine reading.

**Open proposals informed**

- none yet

**Why this belongs here**

- `references/README.md` already identifies multi-event `on` as one of the highest-value future compactness candidates.
- `internal-verbosity-analysis.md` shows repeated row-header patterns, but no proposal should be opened until the shorthand rules and desugaring model are researched together.

**Shared precedent sweep**

- xstate and statecharts;
- UML multi-trigger transitions;
- CSP / symbolic automata;
- desugaring and resugaring literature for diagnostic fidelity.

### Effort: Type-system follow-ons

**Domains included**

- duration / temporal intervals;
- attachment or document-reference fields;
- other residual high-value domain types that survive the main type sweep.

**Open proposals informed**

- none yet

**Why this belongs here**

- PM research already surfaced residual gaps after choice/date work, but those gaps should **not** become standalone proposal churn before the main type-system corpus lands.
- This is a continuation lane of the **same type-system effort**, not a separate research program.

**Shared precedent sweep**

- reuse the Batch 1 type-system precedent targets;
- extend them only where the temporal or document-reference cases need extra evidence.

---

## Sequencing rules

1. **Batch 1 first.** It closes the largest evidence holes under already-open proposals.
2. **Batch 2 second.** It turns existing research fragments into complete domain packets ready for proposal review and implementation sequencing.
3. **Batch 3 third.** It seeds the next proposal wave without distracting the team from the current open queue.

## PM call

If research bandwidth is tight, the first three passes should be:

1. **Type system expansion**
2. **Constraint composition**
3. **Expression expansion**

That order best matches user value, current evidence gaps, and the number of downstream proposals each pass unlocks.
