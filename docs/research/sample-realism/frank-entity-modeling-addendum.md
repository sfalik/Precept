# Entity Modeling Addendum: Correcting the Workflow-Heavy Framing

**Author:** Frank (Lead Architect & Language Designer)
**Date:** 2026-05-19
**Status:** Research addendum — correction and expansion to sample-realism research
**Depends on:** `frank-language-and-philosophy.md`, `frank-sample-ceiling-philosophy-addendum.md`, Issue #22 (data-only precepts)

---

## Purpose

This addendum is a correction.

The prior sample-realism research — my own included — framed Precept overwhelmingly as a **workflow and lifecycle engine**. The domain-fit tiers, the dilution test, the domain-ceiling model, and the recommended sample candidates all center on entities that "move through a governed lifecycle." The phrase "governed lifecycle" appears as the load-bearing concept everywhere: Tier 1 domains are defined by lifecycle branching, the dilution test asks "does this domain require governed lifecycle transitions?", and the ceiling model counts "distinct governed-lifecycle patterns."

That framing is not wrong. But it is incomplete, and the incompleteness has real consequences for sample planning.

**Precept is not just about workflow.** It is a domain integrity engine for modeling business entities — their structure, their rules, and optionally their lifecycle. Issue #22 (data-only precepts) makes this explicit at the language level: a precept with no states, no events, and no transitions is a legitimate contract. It defines fields, invariants, editability. It says what a business entity IS and what rules it must obey — without requiring that it GO anywhere.

Shane's directive is clear: the sample corpus, the research, and the positioning must reflect this broader identity. This addendum corrects the workflow-heavy framing and establishes what entity-centric, record-centric, and data-contract modeling means for the sample portfolio.

---

## 1. Why Sample Planning Must Include Entity-Centric and Data-Contract Precepts

### 1.1 The philosophical case

Precept's design philosophy (from `PreceptLanguageDesign.md`) rests on commitments that are not workflow-specific:

| Commitment | Workflow reading | Entity/data-contract reading |
|------------|-----------------|------------------------------|
| **Prevention, not detection** | Invalid state transitions are impossible | Invalid field states are impossible — a Customer with a negative rating, a Policy with an empty coverage list, a Product with a price below cost |
| **One file, complete rules** | Every guard, transition, and constraint in one place | Every field constraint, invariant, and editability rule in one place — the contract IS the schema + rules |
| **Full inspectability** | "What can happen next?" | "What can I edit? What would break if I changed this value?" — the `Inspect(instance, patch)` and `Update()` paths |
| **Deterministic semantics** | Same inputs = same transition outcome | Same inputs = same validation outcome — no runtime surprises when editing entity data |
| **AI-first** | AI can author and reason about workflows | AI can author and reason about data contracts — arguably easier than workflows because the structure is simpler |

Every one of these commitments applies to stateless entities. Prevention means a Customer record can never have an invalid email and a negative rating simultaneously. One-file completeness means the business rules for that Customer live in the precept, not scattered across FluentValidation attributes and database CHECK constraints. Inspectability means an AI agent can call `Inspect(instance, patch)` and know exactly what would happen before committing a change.

**The philosophical case is not "Precept also does data contracts." It is "Precept's core commitments naturally extend to data contracts, and refusing to demonstrate that leaves a hole in the product's credibility."**

### 1.2 The domain-reality case

Real business domains are not all-workflow or all-data. They are mixed:

| Domain | Workflow entities | Data/reference entities | Ratio |
|--------|-------------------|------------------------|-------|
| **Insurance** | Claim, Policy Application, Appeal | Adjuster, Provider, Coverage Type, Rate Table, Policy Template | ~2:5 |
| **Healthcare** | Prior Authorization, Treatment Plan, Patient Encounter | Patient, Provider, Facility, Procedure Code, Formulary Entry | ~3:5 |
| **Finance** | Loan Application, Invoice, Expense Report | Customer, Account, Rate Card, Fee Schedule, GL Code | ~3:5 |
| **HR** | Leave Request, Onboarding, Performance Review | Employee, Department, Job Grade, Benefits Package, Pay Band | ~3:5 |
| **E-commerce** | Order, Return, Dispute | Product, Customer, Shipping Method, Tax Rule, Discount Code | ~3:5 |

In every domain, the data/reference entities **outnumber** the workflow entities. If Precept can only model the workflow side, it covers the minority of a domain's entities. The rest go to Zod, FluentValidation, EF annotations, or raw database constraints — creating exactly the two-language, two-runtime split that Issue #22 identifies as the problem.

A sample corpus that is 100% workflow sends the message: "Use Precept for your Claims, and use something else for your Adjusters and Rate Tables." That is a weaker product story than: "Use Precept for your entire domain — Claims get lifecycle governance, Adjusters get data integrity, and both share the same contract language."

### 1.3 The stateless-precept trajectory

Issue #22 is not a niche feature. It is a category expansion. When it ships:

- The language grammar gains root-level `edit` declarations and the `all` field quantifier.
- The runtime gains nullable `CurrentState`, `IsStateless`, and a stateless `Update()` path.
- The API surface gains a form factor (create instance → update → inspect patch) that looks more like a validated record than a state machine.
- The MCP tools gain the ability to compile, inspect, and update entities with no states.

The sample corpus needs to be ready for this. Not by waiting for #22 to ship and then scrambling — but by identifying now which entity-centric samples the corpus needs, what external analogs are worth studying, and what balance the portfolio should strike.

### 1.4 The three precept archetypes

With #22 in the picture, the sample corpus serves three archetypes, not one:

| Archetype | States? | Events? | Transitions? | Core value | Example |
|-----------|---------|---------|-------------|------------|---------|
| **Workflow precept** | Yes | Yes | Yes | Lifecycle governance — prevention of invalid transitions, inspectable decision paths | Insurance Claim, Loan Application |
| **Entity precept** | No | No | No | Data integrity — prevention of invalid field states, inspectable editability | Customer Profile, Rate Card, Provider Directory |
| **Hybrid precept** | Yes, minimal | Yes, minimal | Yes, few | Lifecycle-light governance — a simple status lifecycle with heavy field constraints | Product Listing (Draft → Active → Archived), Employee Record (Active → OnLeave → Terminated) |

The current research plans for archetype 1 in detail, mentions archetype 2 in passing ("~10–15% of corpus"), and doesn't acknowledge archetype 3 at all.

---

## 2. External Analogs Worth Studying

### 2.1 The gap in prior research

The prior research studied workflow-adjacent systems: Temporal, Step Functions, Entra Lifecycle Workflows, Appian case management. These are excellent comparators for the workflow archetype. But they tell us nothing about entity and reference-data modeling patterns.

The following external systems are worth studying specifically for **entity structure, record models, reference data, policy configuration, and data-contract patterns** — not workflow:

### 2.2 Enterprise platform object models

| System | What to study | Why it matters for Precept |
|--------|--------------|---------------------------|
| **Salesforce Standard/Custom Objects** | Object definitions, field types, validation rules, formula fields, record types, picklist values, required vs optional fields. The Salesforce object model is the most widely deployed entity-definition framework in enterprise software. | Shows how a platform defines entities declaratively — field constraints, categorical values (picklists = our `choice`), computed fields (formulas = our future computed fields), and per-record-type editability rules. The gap between a Salesforce object definition and a Precept entity definition is exactly the gap #22 closes. |
| **Salesforce Industries Data Packs** (Health Cloud, Financial Services Cloud, Manufacturing Cloud) | Pre-built industry data models — Patient, Provider, Claim, Policy, Coverage, Financial Account, Holding, Manufacturing Program, Asset. These are curated entity definitions for specific verticals. | Shows what real-world entity models look like in specific domains. Each data pack defines dozens of entities with field types, validation rules, and relationships. The entity shapes are the strongest available reference for what Precept data-contract samples should model. |
| **ServiceNow Tables and Record Models** | Table definitions, dictionary entries, data policies, client scripts, business rules that enforce field constraints on record creation/update. ServiceNow's CMDB (Configuration Management Database) is a massive entity registry. | Shows how an enterprise platform handles entities that have both a simple status lifecycle AND rich field constraints. A CI (Configuration Item) in the CMDB has a status field (Active/Retired/Stolen) but the real complexity is in the 40+ constrained fields. This is the hybrid archetype. |
| **Guidewire Entity Models** (ClaimCenter, PolicyCenter, BillingCenter) | Claim, Policy, Account, Contact, Activity, Note, Reserve, Payment, Invoice entities. Guidewire's entity model is the insurance industry's de facto standard. | Shows what domain-specific entity models look like at enterprise depth. A Guidewire Policy entity has coverage structures, deductibles, limits, effective dates, endorsements — this is where Precept's `choice`, `date`, `decimal`, and constraint features either prove themselves credible or don't. |

### 2.3 Master data and reference data tools

| System | What to study | Why it matters for Precept |
|--------|--------------|---------------------------|
| **Informatica MDM / Reltio / Profisee** | Master data entity definitions — Golden Record models for Customer, Product, Supplier, Location. Match/merge rules, survivorship rules, field-level trust scoring. | Shows how reference data is modeled with rich field constraints and cross-field validation. A Golden Record is essentially a stateless entity with complex invariants — exactly the data-contract archetype. |
| **Ataccama ONE / Collibra Data Quality** | Data quality rules, profiling constraints, field-level expectations (non-null, range, pattern, referential). | Shows how field-level constraints are expressed in data governance platforms. The constraint vocabulary (non-null, range, pattern, uniqueness, cross-field dependency) maps directly to Precept's invariant and future constraint features. |

### 2.4 Form, schema, and validation ecosystems

| System | What to study | Why it matters for Precept |
|--------|--------------|---------------------------|
| **JSON Schema** | Property definitions, required fields, enums, min/max, pattern, dependencies, conditional validation (`if`/`then`/`else`). | The closest pure-schema analog to a stateless Precept. JSON Schema's field-level constraints and conditional validation are exactly the kind of rules Precept invariants enforce. Studying where JSON Schema falls short (poor readability, no editability concept, no prevention story) reveals Precept's differentiation. |
| **Zod / Valibot / Yup** | Schema-as-code validation: field types, refinements, transforms, conditional validation, error messages. | These are the tools that developers currently use for entity validation in TypeScript. Precept's data-contract archetype competes directly with these — the sample corpus must show why a `.precept` file is more readable, more inspectable, and more auditable than a Zod schema. |
| **FluentValidation / DataAnnotations** | .NET validation: property rules, conditional rules, collection validation, custom validators, error message templates. | The .NET-native competition for entity validation. Precept's data-contract archetype must be compared against FluentValidation for the same entity definitions. Where does the `.precept` file win? (Readability, inspectability, one-file completeness.) Where does it lose? (No cross-entity references, no database integration.) |

### 2.5 Rule catalogs and decision-table systems

| System | What to study | Why it matters for Precept |
|--------|--------------|---------------------------|
| **DMN (Decision Model and Notation) / FEEL** | Decision tables, input/output definitions, hit policies, FEEL expressions for business rules. | DMN is the strongest reference for how business rules are expressed declaratively. A DMN decision table that validates an insurance rate based on age, coverage type, and region is the kind of policy logic that Precept's invariants and conditional invariants should express. The sample corpus needs entries that show Precept handling decision-table-shaped rules — not just workflow gates. |
| **Drools / OpenRules / Sparkling Logic SMARTS** | Rule catalogs: named rules, rule sets, rule templates, conditional execution, priority/salience. | Shows how enterprise rule engines organize and name business rules. Precept's named rules (#8) and conditional invariants (#14) occupy adjacent territory. Studying how rule catalogs handle policy versioning, rule grouping, and conflict resolution reveals patterns the sample corpus should demonstrate. |
| **OPA (Open Policy Agent) / Cedar** | Policy-as-code: attribute-based access control, resource/action/principal models, policy sets, deny-by-default. | Shows how policy enforcement is expressed for entities — not just workflow transitions but "can this principal edit this field given these attributes?" This maps to Precept's edit declarations and invariants. The sample corpus should include samples that feel like policy enforcement for entity data, not just lifecycle governance. |

### 2.6 Insurance and financial product models

| System | What to study | Why it matters for Precept |
|--------|--------------|---------------------------|
| **ACORD data standards** (insurance) | Standard entity definitions for Policy, Claim, Party, Coverage, Vehicle, Property, Loss. Industry-standard field names, types, and constraints. | The canonical entity vocabulary for insurance. Precept samples in the insurance domain should use ACORD-aligned field names and structures to demonstrate domain credibility. |
| **FHIR resources** (healthcare) | Patient, Practitioner, Medication, Condition, Encounter, Coverage, Claim. Typed, constrained, industry-standard entity definitions. | The healthcare entity vocabulary. FHIR resources are essentially constrained entity definitions with validation rules — the healthcare analog of what a stateless Precept would express. |
| **ISO 20022** (financial messaging) | Payment instruction, account statement, securities settlement. Message component types with field constraints, code sets, and conditional presence rules. | Shows how financial entities are defined with enumerated code sets (= `choice`), conditional field presence, and amount constraints (= `decimal` with invariants). |

---

## 3. Sample Lanes That Become Newly Important

### 3.1 The impact of stateless precepts on sample planning

When #22 ships (or when the corpus begins using FUTURE(#22) comments to demonstrate the pattern), several sample lanes that were previously marginal become important:

### 3.2 Newly important sample lanes

| Lane | Why it was marginal before | Why it matters now |
|------|----------------------------|-------------------|
| **Reference data entities** | No way to model them without faking a state machine. The dilution test said "does this domain require governed lifecycle transitions?" and reference data entities don't. They were explicitly excluded. | Reference data entities are the primary use case for stateless precepts. Customer, Product, Provider, Rate — these are the entities that prove Precept is a domain integrity platform, not just a workflow engine. |
| **Configuration and policy records** | Config records don't transition. A rate table doesn't "move through a lifecycle." Previous framing had no home for these. | A rate table with field constraints, invariants, and editability rules is a legitimate data contract. It demonstrates prevention (invalid rate combinations are structurally impossible) and inspectability (what can I edit on this rate table?) without any workflow overhead. |
| **Schema and validation demonstrations** | Previously, the only way to show validation was through event asserts and invariants within a workflow. Validation for its own sake wasn't a sample category. | Stateless precepts make validation-focused samples legitimate. A precept that defines a Contact record with email format rules, phone number constraints, and required-field logic demonstrates Precept competing directly with Zod/FluentValidation. |
| **Domain model suites** | Previously, one-precept-per-sample was the only model. Showing multiple related entities (a Claim AND its Adjuster AND its Coverage Type) wasn't possible without state machines for all of them. | A domain can now be represented as a suite: workflow precepts for the lifecycle entities and data-contract precepts for the reference entities. The sample portfolio can demonstrate a complete bounded context. |
| **Hybrid lifecycle-light entities** | Entities with a trivial status field (Active/Inactive/Archived) but rich field constraints were caught between "too simple for a workflow sample" and "can't be stateless." | These are legitimate samples in their own right. A Product Listing with three states and 15 constrained fields is primarily an entity sample with a thin lifecycle — not a workflow sample with entity features. The corpus should acknowledge this hybrid shape. |

### 3.3 The editability demonstration gap

Current samples demonstrate `edit` declarations in the context of state-scoped editability: "in Draft, you can edit these fields; in Approved, you can't." This is valuable but only tells half the story.

Stateless precepts introduce root-level editability: `edit all` or `edit Field1, Field2`. This is a different pattern — it says "these fields are always editable" or "only these fields are ever editable." The sample corpus needs entries that demonstrate:

- `edit all` — an entity where everything is mutable (Customer profile, Contact record)
- `edit Field1, Field2` — an entity where some fields are locked after creation (Account Number, SSN, Tax ID cannot change; Name, Address, Phone can)
- The graduation path: a formerly stateless entity that gains states, and the compile error that forces `edit` migration

---

## 4. Where the Current Corpus Is Too Workflow-Heavy

### 4.1 The imbalance

The current 21 samples are 100% workflow precepts: every one has states, events, and transitions. The planned expansion (from `frank-language-and-philosophy.md` §6) proposed 20 new candidates — 18 workflow precepts and 2 data-only (customer-profile, product-catalog-entry), both classified as "Teaching" level.

That ratio — 90% workflow, 10% data-only, all data-only samples at teaching level — sends a message: data contracts are a footnote. They are entry-ramp curiosities, not real use cases.

The ceiling philosophy addendum allocated "~10–15%" of the corpus to data-only precepts. That was already too low given the domain reality (§1.2 above), and it compounded the problem by framing data-only samples as "Supporting" — a layer that "exists only to prove Precept isn't workflow-only."

"Exists only to prove Precept isn't workflow-only" is the weakest possible framing for a feature that addresses the majority of entities in every real business domain.

### 4.2 Specific corrections needed

| Previous framing | Correction |
|------------------|------------|
| Data-only samples classified as "Teaching" level | Data-only samples should span the full complexity gradient. A Customer Profile is Teaching. A Rate Card with 12 constrained fields, cross-field invariants, and conditional editability is Standard. A Policy Template with coverage structures, limit hierarchies, and regulatory constraints is Complex. |
| Data-only layer at "~10–15%" of corpus | **20–25%** is more realistic. In a 40-sample corpus, 8–10 entity/data-contract samples. This matches the reality that data entities outnumber workflow entities in every domain. |
| Reference data at Tier 3 ("Credible") in domain-fit ranking | Reference data and entity modeling should be at least **Tier 2** for domain fit. The entities that the workflow precepts govern (Claims) are defined by the reference entities around them (Adjusters, Coverage Types, Policies). You can't demonstrate a credible insurance domain without both. |
| Dilution test question #1: "Does this domain require governed lifecycle transitions?" | Reframe: **"Does this entity require governed integrity — lifecycle transitions, field constraints, or both?"** A Customer record with 8 invariants and 3 locked fields requires governed integrity. It just doesn't need states. |
| Flagship samples = only workflow-dense lifecycle models | Flagship samples should include at least one **domain suite** that shows workflow + entity precepts working as a complementary set. The insurance domain is the obvious candidate: `insurance-claim.precept` (workflow) alongside `insurance-adjuster.precept` (entity) and `insurance-coverage-type.precept` (entity). |

### 4.3 The balance the corpus should strike

| Archetype | Current | Proposed target | Target % in 40-sample corpus |
|-----------|---------|-----------------|------------------------------|
| **Workflow precept** (stateful, lifecycle-governed) | 21/21 (100%) | 24–28 | 60–70% |
| **Entity precept** (stateless, data-contract) | 0/21 (0%) | 6–8 | 15–20% |
| **Hybrid precept** (lifecycle-light, field-heavy) | 0/21 (0%) | 3–5 | 8–12% |
| **Teaching** (minimal, entry-ramp) | 3/21 (14%) | 2–3 | 5–8% |

The workflow archetype remains the majority — that is correct. Precept's lifecycle governance is its strongest differentiator. But the entity and hybrid archetypes must be substantial enough that a developer looks at the sample portfolio and sees a **domain modeling platform**, not a workflow engine with a data-contract afterthought.

---

## 5. Recommended Non-Workflow Sample Categories

### 5.1 Entity precept candidates (stateless data contracts)

| Category | Sample name | Complexity | Key demonstration value |
|----------|------------|-----------|------------------------|
| **Master record** | `customer-profile` | Teaching | Simplest possible data contract. Email required, rating bounded, phone nullable. Entry ramp for stateless precepts. |
| **Master record** | `employee-record` | Standard | Rich field set: name, department, job grade, hire date, salary band. Cross-field invariants (salary within band range). Locked fields (employee ID, SSN). `edit` with field list. |
| **Reference data** | `insurance-coverage-type` | Standard | Coverage code, description, deductible range, premium bounds, eligible policy types. Demonstrates choice-like categorical constraints and cross-field validation. Pairs with `insurance-claim` as a domain suite. |
| **Reference data** | `product-catalog-entry` | Standard | SKU (locked), name, category, price, cost, weight, active flag. Invariant: price >= cost. Shows a reference entity that a workflow entity (order-fulfillment) depends on. |
| **Rate/configuration** | `fee-schedule` | Complex | Fee code, amount, effective range, eligible account types, volume tiers, override flag. Cross-field invariants (tier thresholds non-overlapping). Demonstrates that data contracts can carry real policy density. |
| **Policy template** | `insurance-policy-template` | Complex | Coverage structure, limit hierarchy, deductible rules, regulatory minimums, eligible risk classes. The entity equivalent of the Complex workflow sample. Shows Precept handling deep domain constraints without a state machine. |
| **Compliance artifact** | `regulatory-requirement` | Standard | Requirement code, jurisdiction, effective date, entity types affected, documentation rules. The kind of reference entity that drives guards in workflow precepts (a Loan Application checks regulatory requirements during underwriting). |
| **Provider/directory** | `healthcare-provider` | Standard | NPI, name, specialty, license state, accepting-patients flag, panel capacity. Demonstrates choice (specialty), constraints (NPI format), and locked fields (NPI cannot change). |

### 5.2 Hybrid precept candidates (lifecycle-light, field-heavy)

| Category | Sample name | Complexity | Key demonstration value |
|----------|------------|-----------|------------------------|
| **Product lifecycle** | `product-listing` | Standard | Draft → Active → Discontinued. Only 3 states, 3 events — but 10–12 constrained fields (price, category, inventory threshold, shipping weight, description length). The lifecycle is thin. The field constraints are the point. |
| **Account record** | `vendor-account` | Standard | Pending → Approved → Suspended → Closed. 4 states, but the entity is primarily defined by its field constraints: tax ID validation, payment terms, credit limit, compliance certifications. |
| **Organizational entity** | `department` | Teaching–Standard | Active → Reorganized → Closed. Minimal lifecycle, but demonstrates budget constraints, headcount limits, reporting-line invariants. Shows that even "boring" organizational entities have rules worth governing. |

### 5.3 Domain suite candidates (workflow + entity shown together)

| Domain | Workflow precept | Entity precepts | Suite value |
|--------|-----------------|-----------------|-------------|
| **Insurance** | `insurance-claim` (existing) | `insurance-adjuster`, `insurance-coverage-type` | Shows how the reference entities that a claim references are themselves governed by Precept. The suite demonstrates a complete bounded context. |
| **Finance** | `loan-application` (existing) or `invoice-lifecycle` (new) | `fee-schedule`, `customer-profile` | Shows how financial workflows depend on reference data with its own integrity rules. |
| **Healthcare** | `treatment-authorization` (new) | `healthcare-provider`, `procedure-code` | Shows how a clinical workflow references validated provider and procedure entities. |

---

## 6. Additional Research Lanes

### 6.1 Research lanes the entity/data side needs

The prior research studied workflow-adjacent tools (Temporal, Step Functions, Appian) and expression-language comparators (FEEL, Cedar, LINQ). For the entity/data side, the following research lanes are missing and would strengthen the corpus and the product positioning:

| Research lane | What to study | Expected output |
|---------------|---------------|-----------------|
| **Salesforce object model deep-dive** | Standard object definitions, custom object patterns, validation rules, formula fields, record types, picklist enforcement. Focus on how Salesforce expresses field constraints declaratively. | A comparative study: Salesforce object definition vs. Precept entity definition. Where does Precept win (readability, one-file, no platform lock-in)? Where does Salesforce win (relationships, roll-up fields, workflow triggers)? |
| **JSON Schema / OpenAPI constraint expressiveness** | Property constraints, conditional validation, composition ($allOf, $oneOf), custom formats. | A constraint-vocabulary comparison: what can JSON Schema express that Precept can't, and vice versa? Feeds into constraint proposal (#13) and conditional invariant (#14) design. |
| **FluentValidation / DataAnnotations pattern catalog** | Common .NET validation patterns — conditional rules, collection validation, cross-property rules, inheritance. | A .NET developer's migration guide: "I was writing this in FluentValidation, here's the equivalent Precept." Each pattern becomes a sample candidate or a FUTURE comment. |
| **Master data entity shape catalog** | Golden Record patterns across MDM tools (Informatica, Reltio, Profisee). What fields do master entities have? What constraints? What merge/survivorship rules? | A catalog of entity shapes that Precept should model. Feeds directly into sample design for stateless precepts. |
| **Industry data standards survey** | ACORD (insurance), FHIR (healthcare), ISO 20022 (finance), HR-XML/HR Open Standards (HR). What do industry-standard entity definitions look like? | Domain-specific field vocabulary and constraint patterns. Ensures Precept samples use industry-recognizable names and structures. |
| **Decision-table-to-invariant mapping** | How do DMN decision tables express multi-condition business rules? Can Precept's invariant + conditional invariant (#14) express the same rules? | A translation guide: DMN decision table → Precept invariants. Reveals expressiveness gaps and feeds constraint proposal design. |

### 6.2 Research priorities

If the team has limited research bandwidth, the priority order is:

1. **FluentValidation / DataAnnotations pattern catalog** — highest immediate value because it targets the .NET developer who is Precept's primary audience and would demonstrate direct migration paths.
2. **Industry data standards survey** — ensures samples use real domain vocabulary.
3. **Salesforce object model deep-dive** — the single best reference for how entity definitions work at scale in enterprise platforms.
4. **JSON Schema constraint comparison** — grounds the constraint proposal (#13) in the dominant schema standard.
5. **Master data entity shape catalog** and **decision-table mapping** — valuable but can wait until #22 ships.

---

## 7. Revised Domain-Fit Tiers

The prior research established three domain-fit tiers. This addendum revises them to account for entity and data-contract modeling:

| Tier | Domain family | Precept archetype | Prior ranking | Revised ranking | Rationale for change |
|------|--------------|-------------------|---------------|-----------------|---------------------|
| **Tier 1** | Claims, disputes, appeals | Workflow | Tier 1 | Tier 1 (unchanged) | Canonical Precept shape |
| **Tier 1** | Financial approvals and exceptions | Workflow | Tier 1 | Tier 1 (unchanged) | High policy density |
| **Tier 1** | Compliance and regulatory | Workflow | Tier 1 | Tier 1 (unchanged) | Pure policy enforcement |
| **Tier 2** | Identity and access governance | Workflow | Tier 2 | Tier 2 (unchanged) | Governed lifecycle |
| **Tier 2** | Healthcare authorization | Workflow | Tier 2 | Tier 2 (unchanged) | Regulated and evidence-bearing |
| **Tier 2** | HR lifecycle management | Workflow | Tier 2 | Tier 2 (unchanged) | Governed approval chains |
| **Tier 2** | **Entity/reference data** | **Entity (stateless)** | **Tier 3** | **Tier 2 (promoted)** | **Every Tier 1 and Tier 2 workflow domain has reference entities that need integrity. You can't credibly model insurance without Adjuster, Coverage Type, and Policy Template entities alongside Claims. Reference data is not peripheral — it is the foundation the workflows stand on.** |
| **Tier 2** | **Policy and configuration records** | **Entity (stateless)** | Not ranked | **Tier 2 (new)** | **Rate tables, fee schedules, regulatory requirements, benefit plans. These are the entities that encode the rules that workflow guards reference. Their integrity is a prerequisite for workflow correctness.** |
| **Tier 3** | Scheduling, logistics, fulfillment | Workflow + Hybrid | Tier 3 | Tier 3 (unchanged) | Fits when entity-centric |
| **Tier 3** | Hybrid lifecycle-light entities | Hybrid | Not ranked | Tier 3 (new) | Product listings, vendor accounts, department records. Simple lifecycle, rich constraints. |

### 7.1 The key promotion: Reference data to Tier 2

The previous research placed reference data at Tier 3 ("Credible — shows Precept's range but shouldn't dominate"). That was a mistake born from the workflow-only lens.

Reference data entities are **structurally prerequisite** to the Tier 1 workflow domains. An insurance claim references coverage types, adjusters, policy templates. A loan application references fee schedules, regulatory requirements, credit models. A compliance filing references jurisdictional rules and document requirements.

If the sample corpus demonstrates Claims without Coverage Types, Loans without Fee Schedules, or Filings without Regulatory Requirements, it is demonstrating the workflow entities in isolation from the data entities they depend on. That is like demonstrating a database's query engine without showing that tables have schemas.

Promoting reference data to Tier 2 means the corpus treats entity precepts as legitimate domain demonstrations — not footnotes — and plans for them with the same rigor as workflow samples.

---

## 8. Revised Dilution Test

The prior dilution test (from `frank-sample-ceiling-philosophy-addendum.md` §3.4) asked five questions. Question #1 was:

> **Does this domain require governed lifecycle transitions?** If no, it's either data-only (wait for #22) or wrong for Precept.

This is the question that excluded entity-centric samples from serious consideration. Revised:

| # | Original question | Revised question |
|---|-------------------|-----------------|
| 1 | Does this domain require governed lifecycle transitions? | **Does this entity require governed integrity — lifecycle transitions, field constraints, editability rules, or a combination?** If none of the above, it's wrong for Precept. If lifecycle only, it's a workflow sample. If constraints and editability only, it's an entity sample. If both, it's a hybrid. |
| 2 | Does this sample demonstrate a real business rule that Precept prevents? | (Unchanged — applies equally to workflow guards and entity invariants) |
| 3 | Does this sample offer inspection value? | **Broadened:** For workflow samples, does `Inspect(instance)` reveal non-obvious available actions? For entity samples, does `Inspect(instance, patch)` reveal non-obvious constraint violations or editability restrictions? |
| 4 | Is this domain already well-represented? | (Unchanged) |
| 5 | Would removing this sample leave a visible gap? | (Unchanged) |

---

## 9. Synthesis

### 9.1 What this addendum corrects

The prior sample-realism research made three framing errors:

1. **It defined Precept's sample identity through workflow governance alone.** The domain-fit tiers, the dilution test, and the ceiling model all used "governed lifecycle" as the qualifying criterion. Entity integrity — prevention of invalid field states, inspectable editability, complete data contracts — was treated as supplementary.

2. **It classified entity/data-contract samples at the lowest tier and simplest complexity.** Data-only samples were Teaching-level entries at Tier 3. This makes entity modeling look like a toy feature rather than a genuine product capability.

3. **It studied workflow-adjacent external systems but not entity-modeling platforms.** Temporal, Step Functions, and Appian were benchmarked. Salesforce object models, Guidewire entities, MDM tools, JSON Schema, FluentValidation, and industry data standards were not. This left the entity side of the research grounded in assumption rather than evidence.

### 9.2 What the corpus should look like after this correction

A 40-sample corpus should include approximately:

- **24–28 workflow precepts** (60–70%) — the core product identity, lifecycle governance
- **6–8 entity precepts** (15–20%) — stateless data contracts across the complexity gradient
- **3–5 hybrid precepts** (8–12%) — lifecycle-light, field-heavy entities
- **2–3 teaching samples** (5–8%) — entry ramps for both archetypes

At least two domain suites (workflow + entity precepts in the same domain) should exist to demonstrate Precept as a complete domain-modeling platform.

### 9.3 The one-sentence version

**Precept models business entities — some with governed lifecycles, some with governed data integrity, some with both — and the sample corpus must demonstrate all three shapes with equal seriousness.**

---

*This document is a correction and expansion to the sample-realism research. It does not supersede the prior research — it broadens its framing from workflow-only to entity-inclusive. All prior research on workflow samples, language guardrails, complexity classification, and the FUTURE(#N) comment protocol remains valid and unchanged.*
