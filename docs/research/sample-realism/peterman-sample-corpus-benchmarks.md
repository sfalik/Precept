# Peterman Sample Corpus Benchmarks

Date: 2026-04-08  
Owner: J. Peterman (Brand/DevRel)  
Scope: corpus strategy research

## Question

What does a believable, maintainable official example corpus look like for projects adjacent to Precept, and what is a realistic upper-end range for Precept's own in-repo sample set?

## Starting point

- Precept currently has **21** `.precept` samples in `samples\`.
- The open planning conversation has treated **42 total samples** as a possible doubled target.
- This pass is about **corpus shape and maintainability**, not hero copy or feature marketing.

## External benchmarks

### Benchmark table

| Project | Official sample surface | Observed size / pattern | What matters for Precept |
|---|---|---:|---|
| [Temporal Go SDK samples](https://github.com/temporalio/samples-go) | Dedicated SDK sample repo, separate from core product repo | **75** top-level directories in `samples-go` as of this review; README says each sample demonstrates one SDK feature and includes tests, and it also separates out [fixtures](https://github.com/temporalio/samples-go/tree/main/temporal-fixtures) for edge cases | Large corpora are possible, but only with **their own repo, strong taxonomy, tests, and a hard split between learning samples and fixtures** |
| [XState examples](https://github.com/statelyai/xstate/tree/main/examples) + [docs examples page](https://stately.ai/docs/examples) | Repo examples directory plus a docs-curated showcase | **49** example directories in repo; docs page curates a smaller list of named examples and links many to CodeSandbox | Healthy projects often keep a **bigger repo corpus than the docs actually foreground**; the docs promote the shortlist, not the whole shelf |
| [Dagster examples](https://github.com/dagster-io/dagster/tree/master/examples) | Examples live in the main repo but are explicitly segmented | **36** example directories in `examples`; the [README](https://github.com/dagster-io/dagster/blob/master/examples/README.md) says some are actively maintained, some are marked `UNMAINTAINED`, and points readers to a separate large-scale project repo | Once you reach the mid-30s, you need **status labels, segmentation, and escape hatches for larger or stale examples** |
| [AWS Step Functions sample projects](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-sample-projects.html) + [AWS Prescriptive Guidance patterns](https://docs.aws.amazon.com/prescriptive-guidance/latest/patterns/welcome.html) | Product docs use focused starter examples; broader scenario library lives in a separate patterns system | Step Functions docs highlight focused workflow examples and starter templates (for example, `Map`, callbacks, retries), while AWS keeps the broader long-tail in a domain-filterable patterns library | Canonical examples and long-tail scenario coverage should be **different surfaces**, not one undifferentiated folder |
| [Mermaid examples](https://mermaid.js.org/syntax/examples.html) | Docs-embedded examples page | A small, syntax-oriented gallery page rather than a large domain corpus | Reference-style products often use **embedded examples for syntax coverage** instead of forcing everything into a standalone corpus |
| [JSON Schema reference](https://json-schema.org/understanding-json-schema/) + [conditionals page](https://json-schema.org/understanding-json-schema/reference/conditionals) | Reference docs with repeated focused examples per concept | The official reference teaches with multiple variants of a few compact cases (credit card/billing address, postal code, restaurant tip) rather than a giant business gallery | Micro-examples are best treated as **reference material**, not counted against the main end-to-end sample budget |

## Raw observations

### 1. Official corpora cluster into two very different shapes

- **End-to-end sample galleries**: Temporal, XState, Dagster, AWS pattern libraries.
- **Reference-example systems**: Mermaid and JSON Schema, where the examples live inside syntax/reference docs.

Precept needs both eventually, but they should not be counted as one thing. A 3-line syntax/control example does not carry the same maintenance or roadmap value as a realistic domain contract.

### 2. Large official corpora are usually split before they become comfortable

- Temporal's sample count is high, but it earns that scale by using a **dedicated samples repo**, **per-SDK separation**, and a visible **fixtures** lane.
- XState can support a large examples directory because the docs page still acts as a **curated shortlist**.
- Dagster's repo examples become manageable by labeling some work as **unmaintained**, and by separating **docs snippets**, **experimental**, and larger-scale examples.
- AWS keeps the long-tail in a **separate patterns library** instead of pretending every scenario belongs in the same first-party product sample shelf.

The repeated pattern is clear: once the corpus gets big, teams introduce **tiers, labels, or separate surfaces**.

### 3. Mid-30s looks normal; 50+ needs infrastructure

- Dagster's **36** example directories already require maintenance-status language.
- XState's **49** repo examples are credible, but the docs do not present all 49 as equal-weight canonical learning paths.
- Temporal's **75** top-level directories are credible only because the repo exists specifically for samples and still distinguishes fixtures from actual learning examples.

For a project of Precept's current size and team shape, **50+ co-equal in-repo samples would read as sprawling unless they were heavily segmented**.

### 4. Canonical and long-tail examples are nearly always separated somehow

Common separation patterns:

- **Canonical / docs-promoted**: the shortlist a new user should trust first
- **Reference / syntax / smoke**: tiny examples that prove constructs, not domain realism
- **Experimental / community / unmaintained / fixtures**: useful, but clearly lower-trust or narrower-scope

Precept should adopt the same discipline. Otherwise a tiny control sample and a roadmap-driving domain contract will continue to look like the same kind of artifact.

## What counts as too sparse vs. too sprawling

### Too sparse

An official corpus is too sparse when:

- it has **fewer than roughly 15-20 realistic business examples**, or
- it technically has more files but too many are tiny controls or near-duplicates, or
- the same workflow shape repeats (intake -> review -> approve/reject) across most of the set, leaving finance, dates, reference-data, and exception-heavy cases underrepresented.

The danger is not just "too few files." The danger is **too few distinct pressures**.

### Too sprawling

An official corpus is too sprawling when:

- it pushes past **roughly 45-50 total in one undifferentiated first-party lane**, and
- there is no clean distinction between canonical, control, experimental, or stale examples, and
- maintainers can no longer tell which files are roadmap evidence versus historical leftovers or test fixtures.

Past that point, the benchmark projects either:

- split by repo (Temporal),
- split by docs-promoted subset vs. broader repo corpus (XState),
- split by status and category (Dagster), or
- move long-tail coverage into a pattern library (AWS).

## How benchmark projects separate canonical from long-tail

### Canonical

Canonical examples usually have several of these traits:

- linked directly from docs
- maintained by the core team
- tied to named product capabilities
- small enough to explain, rich enough to teach
- trusted as current best practice

### Long-tail / experimental

Long-tail examples are usually separated by at least one of:

- a different repo or section
- "experimental" or "unmaintained" labels
- domain filters or pattern-library packaging
- community ownership
- fixture/test-only status

### Precept implication

Precept should stop treating the whole `samples\` folder as one flat category. A workable model is:

1. **Canonical business corpus** — the official roadmap evidence set  
2. **Reference/control corpus** — tiny syntax or smoke examples  
3. **Experimental / overflow corpus** — issue-driven, provisional, or lower-maintenance samples

That separation can be physical (folders), editorial (README/index tags), or both.

## Recommendation: Precept's realistic upper-end range

## Short answer

**A realistic upper-end range for Precept is about 38-46 total in-repo samples, with only about 30-36 of those treated as co-equal canonical business samples.**

## Why this range

- It is comfortably above today's **21**, so it gives real room for better domain coverage.
- It stays near the zone where Dagster-sized example sets are still believable without special infrastructure.
- It avoids pretending Precept has Temporal-level sample operations before it actually has a dedicated samples repo, fixtures lane, or per-surface editorial split.
- It matches the repeated benchmark pattern: **once you approach 40+, tiering becomes mandatory**.

## Practical recommendation

For Precept, the most believable ceiling is:

- **30-36 canonical business samples**
- **4-6 reference/control samples**
- **2-4 experimental or overflow samples**

That yields a total band of **36-46**, with **about 40-42 total** feeling plausible **only if** the corpus is explicitly tiered and not all examples are presented as equal-weight roadmap-grade contracts.

## What this means for the current "42" conversation

- **42 total** is defensible as a high-end target.
- **40 roadmap-grade business samples plus 2 controls** is less defensible if all 40 are expected to remain co-equal, first-tier canonical examples in one flat corpus.
- A better reading of the evidence is: **42 can work as a ceiling, not as an undifferentiated canonical shelf**.

## Suggested operating rule

If Precept wants to go beyond **about 42-46 total**, it should first add at least one of the structural moves the benchmark projects use:

- separate canonical vs. experimental lanes
- add maintenance-status labels
- create a docs-promoted shortlist
- or move overflow/fixtures into a separate sample surface

## Recommendation summary

- Do **not** think in terms of "just double it."
- Do think in terms of **coverage tiers** and **editorial trust levels**.
- Treat **~40-42 total** as the credible top end of a well-kept first-party corpus today.
- Treat anything beyond the mid-40s as requiring **new corpus infrastructure**, not just more files.

## Sources

- Temporal Go SDK samples README: https://github.com/temporalio/samples-go/blob/main/README.md
- Temporal Go SDK samples directory: https://github.com/temporalio/samples-go
- XState examples directory: https://github.com/statelyai/xstate/tree/main/examples
- XState docs examples page: https://stately.ai/docs/examples
- Dagster examples directory: https://github.com/dagster-io/dagster/tree/master/examples
- Dagster examples README: https://github.com/dagster-io/dagster/blob/master/examples/README.md
- AWS Step Functions sample projects: https://docs.aws.amazon.com/step-functions/latest/dg/concepts-sample-projects.html
- AWS Prescriptive Guidance patterns: https://docs.aws.amazon.com/prescriptive-guidance/latest/patterns/welcome.html
- Mermaid examples page: https://mermaid.js.org/syntax/examples.html
- JSON Schema reference: https://json-schema.org/understanding-json-schema/
- JSON Schema conditionals reference: https://json-schema.org/understanding-json-schema/reference/conditionals
