# Entity-First Positioning Evidence

**Author:** Frank (Lead Architect & Language Designer)
**Date:** 2026-07-18
**Status:** Durable philosophy research — promoted from sample-realism initiative findings
**Informs:** `docs/philosophy.md`

---

## Purpose

This document distills the reusable, product-level conclusions from the sample-realism research initiative into a durable evidence base for Precept's philosophy. It answers four positioning questions:

1. Why is entity-first — not workflow-first — the correct framing?
2. Why are states optional rather than definitional?
3. Why is workflow one dimension of entity governance, not the defining frame?
4. Where does Precept sit relative to adjacent tool categories?

Every claim below is grounded in external evidence from enterprise platforms, schema-validation ecosystems, rules engines, and industry data standards. Sample-specific planning, corpus rosters, and rewrite sequencing are deliberately excluded — those belong in `docs/research/sample-realism/`.

---

## 1. Entity-first is the correct framing

### 1.1 Every core commitment applies without a state machine

Precept's design commitments are not workflow-specific. Each one holds for entities that have no lifecycle at all:

| Commitment | Workflow reading | Stateless entity reading |
|------------|-----------------|--------------------------|
| **Prevention, not detection** | Invalid state transitions are impossible | Invalid field configurations are impossible — a Customer with a negative rating, a Policy with an empty coverage list, a Product priced below cost |
| **One file, complete rules** | Every guard, transition, and constraint in one place | Every field constraint, invariant, and editability rule in one place — the contract IS the schema plus its rules |
| **Full inspectability** | "What can happen next?" | "What can I edit? What would break if I changed this value?" |
| **Deterministic semantics** | Same inputs → same transition outcome | Same inputs → same validation outcome — no runtime surprises when editing entity data |
| **AI-first legibility** | AI can author and reason about workflows | AI can author and reason about data contracts — arguably easier because the structure is simpler |

The philosophical case is not "Precept also does data contracts." It is that Precept's core commitments naturally extend to data contracts, and refusing to demonstrate that leaves a hole in the product's credibility.

*Source: entity-modeling addendum §1.1*

### 1.2 In every real business domain, data entities outnumber workflow entities

| Domain | Workflow entities | Data/reference entities | Approximate ratio |
|--------|-------------------|------------------------|-------------------|
| Insurance | Claim, Policy Application, Appeal | Adjuster, Provider, Coverage Type, Rate Table, Policy Template | ~2:5 |
| Healthcare | Prior Authorization, Treatment Plan, Patient Encounter | Patient, Provider, Facility, Procedure Code, Formulary Entry | ~3:5 |
| Finance | Loan Application, Invoice, Expense Report | Customer, Account, Rate Card, Fee Schedule, GL Code | ~3:5 |
| HR | Leave Request, Onboarding, Performance Review | Employee, Department, Job Grade, Benefits Package, Pay Band | ~3:5 |
| E-commerce | Order, Return, Dispute | Product, Customer, Shipping Method, Tax Rule, Discount Code | ~3:5 |

A product that can only model the workflow side covers the minority of entities in every domain. The rest go to Zod, FluentValidation, EF annotations, or raw database constraints — creating the two-language, two-runtime split that entity-first framing eliminates.

*Source: entity-modeling addendum §1.2*

### 1.3 Enterprise platforms model constrained entities as first-class objects

Every major enterprise platform studied — Salesforce, ServiceNow, SAP MDG, Guidewire — has a concept of a master data entity: a business object with typed fields, enumerated vocabularies, required/optional declarations, cross-field validation rules, and default values. None of these platforms require a state machine to make entity modeling valuable.

Common structural elements across all four:

- Typed fields (picklist, currency, date, text, number, reference)
- Required/mandatory declarations
- Unique constraints
- Enumerated vocabularies (picklists, choice fields, type lists)
- Cross-field validation rules
- Conditional constraints
- Default values

The value comes from **fields + constraints + editability** as a self-contained unit — which is exactly the contract a stateless precept provides.

*Sources: entity-centric benchmarks §2.1; enterprise-platform survey §1.1 (Salesforce), §1.1 (ServiceNow), §1.1 (Guidewire)*

### 1.4 Reference data is prerequisite to workflow domains

Reference data entities are not peripheral decoration. They are structurally prerequisite to the workflow domains:

- An insurance claim references coverage types, adjusters, and policy templates
- A loan application references fee schedules, regulatory requirements, and credit models
- A compliance filing references jurisdictional rules and document requirements

Demonstrating workflow entities without their reference data counterparts is like demonstrating a database query engine without showing that tables have schemas. A product that governs Claims but not Coverage Types, Loans but not Fee Schedules, is presenting workflows in isolation from the data they depend on.

*Source: entity-modeling addendum §7.1*

---

## 2. States are optional — three archetypes, not one

### 2.1 The three precept archetypes

With stateless precepts in the picture (Issue #22), Precept serves three archetypes:

| Archetype | States? | Events? | Transitions? | Core value | Example |
|-----------|---------|---------|--------------|------------|---------|
| **Workflow precept** | Yes | Yes | Yes | Lifecycle governance — prevention of invalid transitions, inspectable decision paths | Insurance Claim, Loan Application |
| **Entity precept** | No | No | No | Data integrity — prevention of invalid field states, inspectable editability | Customer Profile, Rate Card, Provider Directory |
| **Hybrid precept** | Yes, minimal | Yes, minimal | Yes, few | Lifecycle-light governance — simple status lifecycle with heavy field constraints | Product Listing (Draft → Active → Archived), Employee Record |

The workflow archetype remains the majority use case — lifecycle governance is Precept's strongest differentiator. But entity and hybrid archetypes are legitimate contracts in their own right, not afterthoughts.

*Source: entity-modeling addendum §1.4*

### 2.2 Design precedent for same-language stateful/stateless modeling

The pattern of a single language supporting both stateful and stateless forms is well-established:

- **Terraform:** `resource` blocks (lifecycle-managed) alongside `data` blocks (read-only queries) — same language, optional complexity
- **SQL:** Tables with CHECK constraints only alongside tables with triggers — same DDL
- **DDD:** Value objects (no identity, no behavior) alongside entities with full lifecycle — same bounded context
- **Progressive disclosure:** HTML, CSS, Python all allow starting simple and adding complexity incrementally

Precept follows the same principle: the `.precept` file is the contract language regardless of whether the entity needs lifecycle governance, data integrity alone, or both.

*Source: Issue #22 design precedent section*

---

## 3. Workflow is one dimension, not the defining frame

### 3.1 The framing correction

The sample-realism research initiative began with a workflow-only lens. The domain-fit tiers, the dilution test, and the recommended candidates all centered on entities that "move through a governed lifecycle." That framing was not wrong, but it was incomplete.

The workflow-only lens systematically excluded the majority of entities in every real business domain — the reference data, configuration records, and master data that workflow entities depend on. Broadening the lens to entity-first is not adding a feature to the portfolio; it is correcting a categorical blind spot.

*Source: entity-modeling addendum §1 (opening), Frank learning 2026-05-19*

### 3.2 What the product governs

A workflow engine's primary job is orchestrating work through a process. Precept's primary job is ensuring a business entity's data satisfies its declared rules, at every moment, through every operation.

The state graph structures how the entity moves through its lifecycle. But the state graph is one structural mechanism — it is not the thing the product is *about*. The product is about **governed integrity**: the guarantee that the entity's data conforms to its declared rules, whether those rules are lifecycle-scoped, unconditional, or both.

The dilution test for whether an entity belongs in Precept is not "does it require governed lifecycle transitions?" but "does it require governed integrity — lifecycle transitions, field constraints, editability rules, or a combination?"

*Source: entity-modeling addendum §8 (revised dilution test)*

---

## 4. Positioning relative to adjacent tool categories

### 4.1 Category map

Precept is not a member of any single adjacent category. It occupies a distinct position defined by combining capabilities that existing tools provide only in isolation:

| Category | What they do well | What they lack | Where Precept differs |
|----------|-------------------|----------------|----------------------|
| **Pure validators** (FluentValidation, JSON Schema, Zod, Pydantic) | Data shape and field-level rules at a moment in time | No lifecycle model, no inspectability, no structural guarantee that rules are called | Rules are bound to the entity and enforced structurally — not invoked by convention or middleware |
| **Pure state machines** (XState, Stateless) | Transitions and lifecycle position | No declared field model, no data integrity enforcement — an entity can pass through every valid transition and still hold corrupted field values | Combines lifecycle structure with data enforcement in a single contract |
| **Policy and decision engines** (OPA, Cedar, DMN) | Rule evaluation against presented data context, often at request-evaluation time | Rules are external to the entity, evaluated at call boundaries, not bound to the data they govern | Rules are declarations bound to the entity that make certain configurations structurally impossible, regardless of which code path runs |
| **Enterprise record-model platforms** (Salesforce, ServiceNow, Guidewire) | Entity governance through object models, validation rules, workflow automation, and state management — authoritative and complete | They are platforms — with data residency requirements, UI infrastructure, runtime overhead, and vendor lock-in | Same category of entity governance as a declarative, embeddable .NET library: no platform dependency, no infrastructure to operate |
| **Industry data standards** (FHIR, ACORD, ISO 20022) | Structural rules that governed entities in specific domains must satisfy | They are vocabularies, not enforcement mechanisms — they describe what should be true, not guarantee it | Can express standard structural rules directly, enforced at the code layer rather than at a validation pass |

### 4.2 The positioning statement

Precept combines the data-rule enforcement of a validator, the lifecycle structure of a state machine, and the policy expressiveness of a decision engine — in a single declarative file that is deterministic, inspectable, and embeddable.

No adjacent tool category provides all three together. Validators lack lifecycle. State machines lack data integrity. Policy engines lack entity binding. Enterprise platforms provide the combination but at the cost of platform dependency.

### 4.3 Evidence from enterprise platform survey

Eight enterprise platforms were studied (Salesforce, ServiceNow, Pega, Appian, Camunda, IBM ODM/BAW, Guidewire, Temporal). The dominant lifecycle shapes across these platforms — intake → triage → work → review → close, with approval gates, appeal loops, and escalation branches — all map to single-entity contracts with guards and constraints. Precept's model fits the structural pattern.

Where Precept deliberately does not compete: multi-entity orchestration, cross-case routing, ML-driven next-best-action, UI form generation, and platform-level integrations. These are platform concerns. Precept is a library.

*Sources: enterprise-platform survey §1 (all 8 platforms); enterprise-ecosystem benchmarks §3 (all 8 platforms); entity-centric benchmarks §2, §3*

### 4.4 Evidence from schema-validation ecosystem

The schema-validation ecosystem (JSON Schema, Zod, FluentValidation, Pydantic, OpenAPI) shares a common constraint vocabulary: required fields, string length, numeric range, pattern/regex, enum values, email format, conditional rules, and custom messages. Every one of these tools teaches through business entity examples — User, Product, Order, Invoice, Address, Customer.

Precept's stateless precepts live in this same mental space, but with the added promise that invariants hold across all mutations — not just at input time. A FluentValidation rule runs when you call it. A Precept invariant holds because the runtime structurally prevents any operation from producing a result that violates it.

*Source: entity-centric benchmarks §2.3*

### 4.5 Evidence from rules engine comparison

Rules engines (Drools, NRules, OPA, Cedar) model entities as facts that are classified, scored, or constrained by business rules. The constraint and classification patterns — customer segmentation, risk scoring, eligibility determination — map to Precept's invariant and conditional invariant surface.

The difference: in a rules engine, rules are external policy evaluated against a data context. In Precept, rules are bound to the entity definition. A stateless precept with invariants is essentially a rules-classified entity where the classification and constraints are embedded in the definition rather than scattered across rule files.

*Source: entity-centric benchmarks §2.4; enterprise-platform survey §1 (IBM ODM, Camunda DMN)*

---

## Summary of positioning claims this evidence supports

| Claim in `docs/philosophy.md` | Evidence strength | Key sources |
|-------------------------------|-------------------|-------------|
| Prevention, not detection — for entities as well as workflows | Strong | §1.1 (commitment table), §4.4 (validator comparison) |
| One file, complete rules — including data-only entities | Strong | §1.1, §1.3 (enterprise platform patterns) |
| States are optional | Strong | §2.1 (three archetypes), §2.2 (design precedent), §1.2 (domain ratios) |
| Workflow is one dimension, not the defining frame | Strong | §3.1 (framing correction), §3.2 (governed integrity) |
| Positioning vs. pure validators | Strong | §4.1, §4.4 |
| Positioning vs. pure state machines | Strong | §4.1 |
| Positioning vs. policy/decision engines | Strong | §4.1, §4.5 |
| Positioning vs. enterprise record-model platforms | Strong | §4.1, §4.3 |
| Positioning vs. industry data standards | Moderate | §4.1 (directional, fewer concrete benchmarks) |

---

## What this document is NOT

- Not a sample roster or corpus plan — those belong in `docs/research/sample-realism/`
- Not a language syntax proposal — those belong in GitHub issues and `research/language/`
- Not an implementation plan — Issue #22 owns the stateless-precept technical spec
- Not a brand or marketing document — it is evidence that informs the philosophy doc, not public-facing prose
