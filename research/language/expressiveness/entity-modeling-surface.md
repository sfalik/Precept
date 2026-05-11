# Entity-Modeling Surface

Research grounding for [#17 — Computed / derived fields](https://github.com/sfalik/Precept/issues/17) and [#22 — Data-only precepts](https://github.com/sfalik/Precept/issues/22).

This file is durable domain research, not a proposal body. It synthesizes the two existing research threads — [computed-fields.md](./computed-fields.md) and [data-only-precepts-research.md](./data-only-precepts-research.md) — because they are both asking the same product question: **what belongs inside one language for governed business entities?**

## Background and Problem

Precept's philosophy already says the product governs business entities, not just workflows. Two open proposals test that claim from opposite directions:

- **Computed fields (#17)** ask whether a business fact that is *derived* from other persistent facts should live as a first-class field definition instead of being repeated across transition rows.
- **Data-only precepts (#22)** ask whether a business entity that is *lifecycle-light* should still live in Precept instead of being pushed into a second validator or schema tool.

Those are not separate category questions. They are the two axes of the same entity-modeling surface:

| | Stored value | Derived value |
|---|---|---|
| **Lifecycle-heavy entity** | Current stateful precepts | Stateful precepts with computed fields |
| **Lifecycle-light entity** | Data-only precepts | Stateless precepts with computed fields |

The governing idea is the same in all four cells: the entity has facts that must remain structurally valid, and the contract should make those facts explicit.

The sample corpus shows the pressure clearly:

- `travel-reimbursement.precept` and `loan-application.precept` keep important totals and affordability facts in workflow rows today, even though those facts are conceptually field-level domain data.
- The domain map's sample audit found **derived values recomputed in multiple rows in 8/21 samples**, which keeps computed fields materially relevant.
- The absence of stateless samples is not evidence against data-only entities. It is a product-surface artifact: current syntax requires states, so the sample set cannot yet demonstrate the stateless form the philosophy now endorses.

## Precedent Survey

| Category | Systems | What they show | Precept implication |
|---|---|---|---|
| **Databases** | [PostgreSQL generated columns](https://www.postgresql.org/docs/current/ddl-generated-columns.html), [SQL Server computed columns](https://learn.microsoft.com/en-us/sql/relational-databases/tables/specify-computed-columns-in-a-table), [MySQL generated columns](https://dev.mysql.com/doc/refman/8.0/en/create-table-generated-columns.html) | One table definition can contain stored columns, derived columns, and constraints; derived columns are explicitly declared and automatically maintained. | Strong precedent for **stored-vs-derived facts inside one entity definition**, with explicit writeability and recomputation rules. |
| **Languages** | [C# properties](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties), [Kotlin properties](https://kotlinlang.org/docs/properties.html) | A single type can expose backed and computed properties side by side; computed members are a normal part of the type surface, not a separate subsystem. | Derived facts belonging on the entity surface is normal language design, especially in a .NET-adjacent product. |
| **End-user tools** | [Excel formulas](https://support.microsoft.com/en-us/office/overview-of-formulas-in-excel-ecfdc708-9162-49e8-b993-c311f47ca173), [Google Sheets formulas](https://support.google.com/docs/answer/46977?hl=en) | Users expect formula-bearing fields to be declared once and kept current automatically. | Computed fields are teachable and legible when they look like explicit field definitions, not hidden recalculation code. |
| **Progressive-complexity systems** | [Terraform data sources](https://developer.hashicorp.com/terraform/language/data-sources), [GraphQL queries](https://graphql.org/learn/queries/) | One language can cover lighter and heavier modeling shapes without forcing a tool split. | Strong precedent for **stateful and stateless** entity definitions coexisting in one language. |
| **Pure validators** | [FluentValidation](https://docs.fluentvalidation.net/en/latest/start.html), [JSON Schema object constraints](https://json-schema.org/understanding-json-schema/reference/object), [Pydantic `computed_field`](https://docs.pydantic.dev/latest/api/fields/#pydantic.fields.computed_field) | Validators handle shape and field rules well; Pydantic also shows that computed values can coexist with schema definitions. But validation is still an invoked check, not structural governance. | Confirms the demand for lifecycle-light definitions and field derivation, while also showing why validator-style invocation is too weak for Precept's promise. |
| **Pure state machines** | [XState context](https://stately.ai/docs/context), [Stateless for .NET](https://github.com/dotnet-state-machine/stateless) | State machines can hold context, but data mutation remains explicit action logic; they do not make derived facts or field integrity first-class. | Lifecycle structure alone is insufficient. Precept's category remains entity governance, not process routing. |
| **Policy / decision engines** | [OPA / Rego policy language](https://www.openpolicyagent.org/docs/latest/policy-language/), [Cedar policy syntax](https://docs.cedarpolicy.com/policies/syntax-policy.html), [Camunda FEEL overview](https://docs.camunda.io/docs/components/modeler/feel/what-is-feel/) | These systems derive decisions from supplied data contexts, often statelessly. They are strong on expressions, weak on persistent entity-local ownership. | Good precedent for deterministic derivation and stateless evaluation, but the decision stays external to the entity. Precept should not drift into request-time decisioning. |
| **Enterprise record-model platforms** | [Salesforce formula fields](https://trailhead.salesforce.com/content/learn/modules/point_click_business_logic/formula_fields), [Dataverse calculated columns](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields), [Dataverse formula columns](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/formula-columns) | Enterprise entity platforms already treat stored fields, calculated fields, and lifecycle/process automation as parts of one record-model surface. | Strongest category precedent that Precept's surface can widen without changing product identity. |
| **Industry data standards** | [HL7 FHIR Patient](https://www.hl7.org/fhir/patient.html), [ACORD standards](https://www.acord.org/standards), [ISO 20022 message definitions](https://www.iso20022.org/iso-20022-message-definitions) | Standards define entity vocabularies and allowable structures, but usually not runtime enforcement or derivation mechanics. | Confirms that real domains contain many lifecycle-light but highly governed definitions. Precept is the enforcement layer standards do not provide. |
| **Master data management** | [Reltio data modeling docs](https://docs.reltio.com/en/objectives/model-data/data-modeling-at-a-glance) | MDM platforms organize durable entity records, reference data, survivorship, and stewardship workflows around canonical entities. | Supports the PM claim that domains are dominated by governed entity definitions, not just workflow items. Precept should stay entity-first, but not attempt cross-system golden-record responsibilities. |
| **Workflow orchestrators** | [Temporal workflows](https://docs.temporal.io/workflows), [MassTransit sagas](https://masstransit.io/documentation/patterns/saga) | Orchestrators manage distributed process steps and long-running coordination, not field-level structural validity inside a single entity contract. | Clear non-goal boundary: Precept should not solve orchestration by smuggling process logic into computed or stateless surfaces. |

## Cross-Category Pattern

Across the full survey, three patterns repeat.

### 1. Stored and derived facts usually share one surface

Databases, spreadsheets, languages, and enterprise platforms all normalize the idea that one entity definition can contain both:

- stored values that are explicitly set, and
- derived values that are explicitly declared.

The consistent rule is that the derivation stays visible. Systems that support derivation do **not** treat it as hidden background magic; they expose the formula, make writeability rules explicit, and separate derivation from initialization.

### 2. Simple and complex entity shapes usually share one language

Terraform, GraphQL, SQL DDL, and enterprise platforms all let authors start with a lighter declaration shape and move to a heavier one when needed. They do not force a jump to a different language just because a particular entity has no lifecycle graph or no procedural behavior yet.

That matters for Precept because the product claim is not "one language for workflows." It is "one language for governed entities." If the domain's simpler entities fall out of the language, the category claim collapses.

### 3. No adjacent category covers the full intersection

Every adjacent category leaves a gap:

- validators lack structural prevention,
- state machines lack field governance,
- policy engines are external and request-scoped,
- orchestrators govern process, not entity integrity,
- databases and enterprise platforms are not embeddable domain contracts in application code.

That is why these two proposals are category-extending rather than category-diluting. They move Precept toward the center of its own stated identity: **governed entities, regardless of how much lifecycle or derivation they need**.

## Philosophy Fit

### One language for governed entities, whether lifecycle-heavy or lifecycle-light

This is the core fit test. `docs/philosophy.md` already says:

- data and rules are primary,
- states are instrumental rather than primary, and
- states are optional when lifecycle structure is unnecessary.

Data-only precepts strengthen that position instead of weakening it. They remove the arbitrary threshold where an entity stops qualifying for Precept because its rules are mostly invariant-shaped rather than state-graph-shaped.

The wrong category story is "Precept is fundamentally a workflow DSL, but maybe it can also do simple records." The right category story is the inverse: **Precept is an entity-governance DSL, and workflow is one optional dimension of that governance.**

### Stored-vs-derived values belong in the entity contract, not in transition mechanics

Computed fields fit only when they improve the "data truth over movement truth" principle.

The safe shape is:

- the formula lives on the field,
- the field is read-only,
- the runtime recomputes it before rule evaluation,
- tools show both the formula and the resulting value.

That reduces row duplication without weakening first-match routing. The unsafe shape is moving business facts into hidden recalculation machinery or letting transition bodies compete with the declared formula.

### Stateless does not mean ungoverned

A data-only precept is not a fallback validator mode. It still carries Precept's core guarantees:

- compile-time structural checks,
- explicit editability,
- deterministic update behavior,
- inspectable outcomes,
- no bypass around invariants.

The absence of states changes *which* coordinates matter, not whether the entity is governed. The protected configuration is simply field data rather than `(state, field data)`.

### AI legibility improves when the entity surface is explicit

Both features help AI and human readers only if they remain visible at the declaration layer:

- statelessness should be obvious from the absence of lifecycle declarations plus explicit edit policy,
- derivation should be obvious from the field declaration itself,
- neither feature should hide mutations or enforcement timing behind implicit behavior.

That aligns with the project's tooling-first and AI-readable philosophy. The contract should read like the entity model, not like an optimization pass.

## Semantic Contracts To Make Explicit

These contracts are the durable boundary of the entity-modeling surface.

### 1. Progressive-complexity contract

Statefulness is optional, but never implicit. Authors can start without states and later add them, but the migration must be explicit and diagnostics must prevent silent behavioral changes. Stateless is a first-class form, not a temporary parser loophole.

### 2. Stored-vs-derived contract

Derived fields must stay:

- entity-local,
- declared once,
- read-only to `set`, `edit`, and external input surfaces,
- recomputed at an explicit point before validation in every relevant pipeline.

Without that contract, computed fields become stale caches instead of trustworthy entity facts.

### 3. Operation-surface contract for stateless definitions

The runtime and tooling surface must say exactly what happens when a precept has no lifecycle graph:

- what `CreateInstance` returns,
- what `Inspect` shows,
- what `Fire` does when no events exist,
- how `Update` enforces editability and invariants,
- how compatibility checks represent the lack of a current state.

The absence of workflow must be explicit in the APIs, not inferred by exceptions or undocumented nulls.

### 4. Visibility contract

Inspectability is part of the product category. Tooling must surface:

- whether a field is stored or derived,
- the derivation expression when one exists,
- whether the instance is stateful or stateless,
- the effective editable-field surface in either form.

If authors cannot see the contract boundary, the category promise becomes rhetorical instead of operational.

### 5. Boundary contract

Entity-modeling features must not become covert escapes into adjacent categories:

- no hidden recomputation or lazy caching,
- no event-argument or cross-entity derivation,
- no implicit workflow semantics in stateless definitions,
- no orchestration logic masquerading as field governance.

## Dead Ends and Rejected Directions

### Split the domain across two tools

Treating workflow entities as "Precept territory" and lifecycle-light entities as "validator territory" produces exactly the mixed-tooling problem the philosophy now rejects. It fractures the domain model and makes the simpler majority of entities second-class.

### Hidden recomputation

If derived facts recompute lazily, cache invisibly, or update at undocumented points, authors can no longer reason about whether guards, invariants, and inspect output are looking at fresh data. Hidden recomputation is a direct violation of inspectable determinism.

### Workflow bypass through a stateless escape hatch

Stateless precepts should not become a way to avoid modeling lifecycle gates that genuinely matter. "No states declared" must mean the entity truly has no lifecycle dimension worth governing, not that the author wants to side-step editability or transition rules.

### Cross-entity or external-source derivation

As soon as a field definition depends on another entity, a network call, or an external provider, the language stops being a one-file integrity contract and starts behaving like a distributed query surface. That is a category shift, not a small convenience feature.

### Smuggling orchestration into entity modeling

Workflow engines and sagas coordinate work across boundaries. Precept should not imitate them by adding hidden triggers, asynchronous recomputation, or process-step semantics to data-only or computed-field features.

## Proposal Implications

Keep this section short: the main value of this document is category knowledge, not proposal text.

- **#17** should stay framed as a read-only, explicit stored-vs-derived contract that removes formula drift without introducing hidden recomputation.
- **#22** should stay framed as a one-language, progressive-complexity contract for lifecycle-light but still governed entities.
- The two proposals should cross-reference each other during review, because together they define Precept's entity-modeling boundary more than either one does alone.

## Cross-References

- [Computed Fields](./computed-fields.md)
- [Data-Only Precepts: Research & Rationale](./data-only-precepts-research.md)
- [Expression Evaluation](../references/expression-evaluation.md)
- [Language Research Domain Map](../domain-map.md)
- [Language Research Domain Batches](../domain-research-batches.md)
- [Precept philosophy](../../../philosophy.md)
