# Entity-Centric Benchmarks for Precept Samples

**Author:** J. Peterman  
**Date:** 2026-05-17  
**Purpose:** Broaden the external benchmark pass beyond workflow platforms to include entity-centric and data-contract examples, informing sample-corpus planning if stateless/data-only precepts land (#22).

**Addendum to:** `peterman-realistic-domain-benchmarks.md`

---

## 0. Why this addendum exists

The earlier benchmark (`peterman-realistic-domain-benchmarks.md`) studied workflow platforms: Temporal, Step Functions, Appian, Entra. That research correctly identified Precept's sweet spot as governed case files with evidence loops and exception handling.

But Precept's positioning as a **domain integrity engine** is broader than workflow. The product binds state, data, and business rules into a single contract — and not every business entity needs a state machine. An insurance company has Claims (complex workflow) and Adjusters (fields + constraints + editability). A procurement system has Purchase Orders (stateful) and Vendors (constrained reference data). An ERP has Orders (lifecycle) and Materials (master data with validation rules).

Issue #22 proposes data-only precepts: fields, invariants, editability — no states, no events, no transitions. That opens a second modeling lane. This addendum grounds that lane in external evidence.

---

## 1. Systems studied

| System / Tool | What I looked at | Why it matters for Precept samples |
|---|---|---|
| **Salesforce** | Custom object modeling, picklists, validation rules, required fields, cross-field formulas | The dominant enterprise entity-definition platform. Shows how real orgs model constrained business objects without workflow. |
| **ServiceNow** | Table definitions, dictionary entries, field types, business rules (before/after/async), choice fields | Record-centric platform where entity constraints and business rules are primary authoring surfaces. |
| **Guidewire** (ClaimCenter, PolicyCenter) | Entity XML (.eti/.etx), type lists, field constraints, foreign keys, Gosu validation rules | Insurance-specific entity modeling. Shows how domain entities are defined with typed constraints and enumerated vocabularies. |
| **SAP MDG** | Entity types, BRFplus validation rules, quality rules management, custom entity validation | Enterprise master data governance. Shows how large orgs define, constrain, and audit reference entities. |
| **JSON Schema** | Business entity schemas, constraint vocabulary (`required`, `enum`, `pattern`, `minLength`, `minimum`), composition (`allOf`, `oneOf`) | The universal schema-validation lingua franca. Shows the constraint patterns every developer already recognizes. |
| **Zod** | TypeScript schema validation, `.refine()`, `.enum()`, `.min()/.max()`, nested objects | The modern schema-validation reference in the TypeScript ecosystem. Directly comparable positioning surface. |
| **FluentValidation** | `.NET` entity validators, conditional rules, cross-field checks, rule sets, nested validators | The dominant .NET validation library. Precept's closest competitive surface for stateless entity contracts. |
| **Pydantic** | Python `BaseModel`, `Field()` constraints, `@validator`, enum types, nested models | Python's schema-validation standard. Shows documentation patterns for constrained entity definitions. |
| **Drools** | DRL entity constraints, classification rules, risk scoring, field-level pattern matching | Business rules engine. Shows how entities are classified, scored, and constrained by policy rules. |
| **NRules** | .NET rules engine, entity-as-fact pattern, classification rules, customer segmentation | .NET rules engine. Shows entity classification and constraint enforcement as named business rules. |
| **XState** | Context (data-only), guards as validation, stateless context patterns, schema integration | State machine library. Shows how even a workflow tool separates data-only context from state transitions. |
| **Terraform** | `resource` (lifecycle) vs. `data` (read-only query) — same language, optional complexity | Infrastructure-as-code precedent for same-language stateful/stateless modeling. Cited in #22. |
| **OpenAPI / Swagger** | `components/schemas`, entity definitions with constraints, `readOnly`/`writeOnly`, `enum`, `example` | API-contract standard. Shows how constrained entities are described for machine and human consumers. |
| **DDD literature** | Value objects, entities, aggregate roots, invariant enforcement, transactional boundaries | Foundational modeling vocabulary. Shows how the industry classifies entity complexity levels. |
| **ERP master data / reference data** | Customer, Vendor, Material, Product master tables; reference tables (country codes, currencies, UOM, payment terms) | Shows the two fundamental entity families: mutable master data with business rules, and static reference data with enumerated values. |

---

## 2. What entity-centric systems actually model

### 2.1 The master data entity

Every enterprise platform studied — Salesforce, ServiceNow, SAP, Guidewire — has a concept of a **master data entity**: a business object that carries fields, constraints, and validation rules but does not necessarily move through a state machine.

**Common structural elements:**

| Element | Salesforce | ServiceNow | SAP MDG | Guidewire |
|---|---|---|---|---|
| Typed fields | ✅ (picklist, currency, date, text, number, lookup) | ✅ (string, integer, choice, date/time, reference) | ✅ (entity attributes with data elements) | ✅ (XML column types with length/required) |
| Required/mandatory fields | ✅ | ✅ | ✅ | ✅ (`required="true"`) |
| Unique constraints | ✅ | ✅ | ✅ | ✅ (`unique="true"`) |
| Enumerated vocabularies | ✅ (picklist values) | ✅ (choice fields) | ✅ (domain fixed values) | ✅ (type lists) |
| Cross-field validation | ✅ (formula-based validation rules) | ✅ (before business rules) | ✅ (BRFplus derivations) | ✅ (Gosu business rules) |
| Conditional constraints | ✅ (`AND(ISPICKVAL(...), ISBLANK(...))`) | ✅ (conditional business rules) | ✅ (trigger-based validation) | ✅ (Gosu conditional logic) |
| Default values | ✅ | ✅ | ✅ | ✅ |

**Key observation:** These platforms do not require a state machine to make entity modeling valuable. The value comes from **fields + constraints + editability + audit** as a self-contained unit.

### 2.2 The reference data entity

Distinct from master data, **reference data** entities are simpler: they define enumerated vocabularies, lookup tables, and classification schemes.

**Examples from the research:**
- Country codes, currency codes, units of measure, payment terms (SAP T005, TCURC, T006)
- ServiceNow choice fields and category tables
- Salesforce global picklist value sets
- Guidewire type lists

**Precept relevance:** A stateless precept modeling a reference entity would be compact — a few fields, tight invariants, locked editability. It looks like a business data dictionary entry.

### 2.3 The schema-validated entity

The schema-validation ecosystem (JSON Schema, Zod, FluentValidation, Pydantic, OpenAPI) models entities as **typed shapes with constraint annotations**.

**Common constraint vocabulary across all four ecosystems:**

| Constraint | JSON Schema | Zod | FluentValidation | Pydantic |
|---|---|---|---|---|
| Required fields | `required: [...]` | chained (no `.optional()`) | `.NotEmpty()` | required by default |
| String length | `minLength`/`maxLength` | `.min()`/`.max()` | `.Length(min, max)` | `Field(min_length=, max_length=)` |
| Numeric range | `minimum`/`maximum` | `.min()`/`.max()` | `.InclusiveBetween()` | `Field(ge=, le=)` |
| Pattern/regex | `pattern` | `.regex()` | `.Matches()` | `Field(pattern=)` |
| Enum values | `enum: [...]` | `z.enum([...])` | `.IsInEnum()` | `Enum` subclass |
| Email format | `format: "email"` | `.email()` | `.EmailAddress()` | `EmailStr` |
| Conditional rules | `if`/`then`/`else` | `.refine()` | `.When(...)` | `@model_validator` |
| Nested objects | `$ref` / `$defs` | `z.object({...})` | `.SetValidator()` | nested `BaseModel` |
| Custom messages | N/A (tooling) | `.message()` | `.WithMessage()` | `ValueError(...)` |

**Key observation:** Every schema-validation tool teaches through **business entity examples** — User, Product, Order, Invoice, Address, Customer. These are the canonical teaching entities of the validation ecosystem. Precept's stateless precepts would live in this same mental space, but with the added promise of **invariants that hold across all mutations, not just at input time**.

### 2.4 The rules-classified entity

Rules engines (Drools, NRules, Microsoft RulesEngine) model entities as **facts** that are classified, scored, or constrained by business rules.

**Patterns observed:**
- Customer segmentation (Premium / Regular / At-Risk based on purchase history and activity)
- Loan applicant risk classification (High / Medium / Low based on credit score bands)
- Order discount eligibility (threshold-based conditional logic)
- Property completeness checks (all required fields present before processing)

**Precept relevance:** A stateless precept with invariants is essentially a rules-classified entity where the classification and constraints are embedded in the definition rather than scattered across rule files.

---

## 3. Answering the four questions

### 3.1 What kinds of entity-centric examples feel realistic and valuable for Precept?

Based on the external evidence, entity-centric samples should come from **five recognizable families**:

**Family A: Master data entities** — mutable business objects with typed fields, business-rule constraints, and controlled editability.

| Candidate | Domain | Why it fits |
|---|---|---|
| **Vendor** / Supplier | Procurement | Name, tax ID, payment terms, banking details, active/inactive flag, compliance status. Every ERP has one. Salesforce, SAP, and ServiceNow all model this as a constrained entity. |
| **Employee** / Staff record | HR | Name, department, role, hire date, active flag, emergency contact. Universally understood. |
| **Product** / Material | Catalog / inventory | SKU, name, category, unit price, unit of measure, stock status. SAP MARA, Salesforce Product2. |
| **Customer** / Account | CRM / commerce | Name, email, phone, billing address, credit limit, tier/segment, active flag. |
| **Provider** / Practitioner | Healthcare | License number, specialty, active status, credentialing date. Guidewire-adjacent. |

**Family B: Reference data entities** — compact, mostly-locked vocabularies and lookup tables.

| Candidate | Domain | Why it fits |
|---|---|---|
| **CountryCode** | Universal | 2-letter code, full name, region, active flag. Classic reference table. |
| **PaymentTerms** | Finance / procurement | Code, description, days-due, discount percentage. |
| **ServiceCategory** | Operations | Code, label, active flag, display order. |
| **RiskTier** | Compliance / underwriting | Tier name, threshold range, review-required flag. |

**Family C: Form/intake entities** — data structures validated at entry, like application forms before they enter a workflow.

| Candidate | Domain | Why it fits |
|---|---|---|
| **ContactForm** | Customer service | Name, email, subject, message, consent flag. The universal "validate before submit" entity. |
| **BenefitsEnrollmentForm** | HR / benefits | Employee ID, plan selection, dependent count, effective date, attestation flag. |
| **VendorOnboardingForm** | Procurement | Company name, tax ID, bank details, insurance certificate flag, W-9 received. |

**Family D: Configuration / settings entities** — system-level objects with constrained parameters.

| Candidate | Domain | Why it fits |
|---|---|---|
| **NotificationPreferences** | Any SaaS | Email enabled, SMS enabled, frequency (daily/weekly/immediate), quiet hours. |
| **PolicyConfiguration** | Insurance / compliance | Coverage type, deductible amount, limit amount, effective date, auto-renew flag. |

**Family E: Scored / classified entities** — entities whose invariants encode classification rules.

| Candidate | Domain | Why it fits |
|---|---|---|
| **CustomerSegment** | CRM | Total purchases, last-activity date, tier (derived from invariants). |
| **RiskAssessment** | Compliance | Score, tier, review-required flag. Invariants encode tier thresholds. |

### 3.2 What makes those examples credible rather than toy-like?

From the external evidence, the credibility signals for entity-centric samples differ from workflow samples. The research points to six:

1. **Domain-bearing field names, not generic placeholders.** `TaxIdentificationNumber`, not `Code`. `CreditLimit`, not `MaxValue`. `UnitOfMeasure`, not `Type`. Every enterprise platform names fields after the business concept they carry.

2. **At least one cross-field constraint.** A single-field `>= 0` check is a database `CHECK` constraint. The moment two fields interact — "if PaymentTerms is COD then CreditLimit must be 0" — the entity stops being a schema and starts being a business rule. Salesforce validation rules, ServiceNow business rules, SAP BRFplus, and Guidewire Gosu all emphasize this as the signal that an entity has policy.

3. **At least one enumerated vocabulary instead of a free string.** Every platform studied — without exception — uses typed enumerations (picklists, choice fields, type lists, enums) for domain concepts. An entity that models `Category as string` when the domain has five known categories is visibly fake.

4. **Realistic nullability.** Not every field is required. Not every field is nullable. The pattern across enterprise platforms is: a core of required identity/policy fields surrounded by optional operational fields. `Phone as string nullable` is more believable than requiring everything or nulling everything.

5. **A because message that sounds like a business notice.** `"Tax ID is required for active vendors"` is credible. `"Field cannot be empty"` is generic. Salesforce validation-rule error messages and FluentValidation `.WithMessage()` both train users to expect domain-specific rejection language.

6. **Constraint density proportional to stakes.** A `CountryCode` with two invariants is fine. A `Vendor` with two invariants is suspiciously thin. The research shows master data entities carrying 5-15 validation rules in production Salesforce orgs and SAP MDG implementations.

**Toy smells for entity samples (distinct from workflow toy smells):**
- Every field is `string` or `number` with no domain typing
- No cross-field rule — just single-field range checks
- The entity could be modeled as a JSON Schema `required` + `type` and nothing more
- The because messages are programmer-facing (`"invalid"`) rather than business-facing
- The entity has no editability restriction — everything is always editable

### 3.3 What non-workflow sample lanes should Precept add or elevate if stateless/data-only precepts land?

Based on the research, three sample lanes deserve explicit investment. Each maps to a recognizable external category.

#### Lane 1: Master data contracts

**What it is:** Constrained, editable business entities that exist without a state machine. The Salesforce custom object, the SAP material master, the ServiceNow CMDB record.

**Target samples:** 2-3 master data entities at different complexity levels.

| Sample | Complexity | Key constraint patterns |
|---|---|---|
| **Vendor** | Standard (8-12 fields) | Cross-field: payment-terms/credit-limit interaction. Conditional: insurance-certificate required if service vendor. Enumerated: vendor type, payment terms. |
| **Product** or **Material** | Standard (6-10 fields) | Range: price > 0, weight >= 0. Enumerated: category, unit of measure. Conditional: days-to-manufacture required if manufactured in-house. |
| **Employee** or **StaffRecord** | Standard (8-10 fields) | Required: name, department, role. Conditional: license number required if role is licensed. Enumerated: department, role. |

**Why this lane matters:** It proves Precept can be the single contract language for an entire domain — not just the workflow half. Issue #22 explicitly cites this: "Forcing users to model simple entities in a different tool creates a two-language split."

#### Lane 2: Reference data definitions

**What it is:** Compact, mostly-locked lookup entities. The Guidewire type list, the SAP reference table, the Salesforce global picklist value set.

**Target samples:** 1-2 reference entities that are deliberately small and tight.

| Sample | Complexity | Key constraint patterns |
|---|---|---|
| **RiskTier** or **ServiceCategory** | Teaching (3-5 fields) | Tight invariants on code format and range. Locked editability except one or two operational fields. |
| **PaymentTerms** | Teaching (4-6 fields) | Enumerated code, days-due range, discount percentage range. All fields required. |

**Why this lane matters:** It shows progressive disclosure — the same language at minimum complexity. Terraform's `data` sources, JSON Schema's simple object definitions, and Zod's flat schemas all serve this role for their ecosystems.

#### Lane 3: Domain-rule contracts (scored/classified entities)

**What it is:** Entities where the invariants encode classification logic or business policy. The Drools risk classifier, the NRules customer segmenter, the SAP MDG quality rule.

**Target samples:** 1 classification-bearing entity that shows invariants doing real policy work.

| Sample | Complexity | Key constraint patterns |
|---|---|---|
| **CreditApplication** (data-only, pre-workflow) | Standard (8-12 fields) | Invariants encode eligibility bands. Cross-field: debt-to-income ratio constraint. Score-range checks. The entity is a policy surface, not just a data bag. |

**Why this lane matters:** It proves that Precept's invariants are not just validation — they are business rules. This is the differentiation point against Zod, FluentValidation, and JSON Schema, all of which validate structure but do not bind rules to the entity as an inspectable contract.

#### Summary: Sample lane allocation

| Lane | Count | Complexity | Role in corpus |
|---|---|---|---|
| Master data contracts | 2-3 | Standard | Prove single-language domain coverage |
| Reference data definitions | 1-2 | Teaching | Prove progressive disclosure |
| Domain-rule contracts | 1 | Standard/Complex | Prove invariants-as-policy differentiation |
| **Total new stateless samples** | **4-6** | Mixed | Broadens the corpus without diluting workflow flagship quality |

This fits within the existing ceiling decision (30-36 canonical, 42 hard upper bound). The stateless lane occupies 4-6 slots, leaving the remaining budget for workflow-quality deepening.

### 3.4 What additional outside research would most improve entity/data-oriented samples after this?

**Research gap 1: Real Salesforce validation rule libraries.**
Salesforce publishes a [validation rules library](https://developer.salesforce.com/docs/atlas.en-us.usefulValidationRules.meta/usefulValidationRules/fields_useful_field_validation_formulas.htm) with hundreds of real cross-field formulas organized by object type (Account, Contact, Opportunity, Case). Mining this for constraint patterns — especially conditional requirements and cross-field interactions — would provide the most concrete inventory of "what real entity constraints look like" for sample authors.

**Research gap 2: ServiceNow CSDM (Common Service Data Model).**
ServiceNow's [CSDM whitepaper](https://www.servicenow.com/community/s/cgfwn76974/attachments/cgfwn76974/common-service-data-model-kb/744/3/CSDM%205%20w%20links.pdf) defines a canonical set of entity types for IT service management. Studying its entity-level constraints and business rules would ground any ITSM-flavored stateless samples.

**Research gap 3: SQL Server Master Data Services business rule examples.**
Microsoft publishes [business rule examples](https://learn.microsoft.com/en-us/sql/master-data-services/business-rule-examples-master-data-services) for master data governance: conditional defaults, cross-field validation, required-based-on-value rules. This is the closest .NET-ecosystem precedent for entity-level policy rules.

**Research gap 4: DDD aggregate invariant patterns in production.**
Microsoft's [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers) reference architecture shows aggregate-root invariant enforcement in a real .NET microservices app. Studying its `Order`, `Buyer`, and `Basket` aggregates would show how invariants are enforced at the entity boundary in production .NET code.

**Research gap 5: Form-schema ecosystems (React Hook Form + Zod, Formik + Yup).**
The form-validation ecosystem generates millions of entity schemas per year. Studying the most common Zod/Yup patterns on GitHub — registration forms, checkout forms, settings pages — would validate which stateless sample shapes are instantly recognizable to frontend developers.

---

## 4. Design precedent summary

The research converges on a clear precedent for stateless entity modeling:

| Ecosystem | Stateful construct | Stateless construct | Same language? |
|---|---|---|---|
| **Terraform** | `resource` (full lifecycle) | `data` (read-only query) | ✅ Same HCL |
| **SQL** | Tables with triggers | Tables with CHECK constraints only | ✅ Same DDL |
| **Salesforce** | Objects with workflow rules / flows | Objects with validation rules only | ✅ Same platform |
| **ServiceNow** | Tables with flow designer | Tables with business rules only | ✅ Same platform |
| **DDD** | Entities with lifecycle behavior | Value objects (no identity, no state) | ✅ Same bounded context |
| **XState** | Machines with states + transitions | Context-only data with guards | ✅ Same library |
| **Precept (#22)** | Precepts with states + events | Precepts with fields + invariants only | ✅ Same DSL |

Every system studied supports both stateful and stateless entity modeling in the same language. Precept would be joining an established pattern, not inventing one.

---

## 5. Source index

### Enterprise platforms

- Salesforce, *Examples of Validation Rules* — https://developer.salesforce.com/docs/atlas.en-us.usefulValidationRules.meta/usefulValidationRules/fields_useful_field_validation_formulas.htm
- Salesforce, *Data Model in Salesforce: A Complete Guide* (SysCloud) — https://www.syscloud.com/saas-data-protection-center/salesforce/data-model-in-salesforce/
- Salesforce, *How to Validate Your Salesforce Data Model* (SalesforceBen) — https://www.salesforceben.com/how-to-validate-your-salesforce-data-model-a-step-by-step-guide/
- ServiceNow, *Data Modeling in ServiceNow* — https://www.servicenow.com/community/itsm-articles/data-modeling-in-servicenow/ta-p/2944368
- ServiceNow, *Field Types and Dictionary Entries* (S2Labs) — https://s2-labs.com/servicenow-admin/servicenow-field-types/
- ServiceNow, *CSDM 5 Whitepaper* — https://www.servicenow.com/community/s/cgfwn76974/attachments/cgfwn76974/common-service-data-model-kb/744/3/CSDM%205%20w%20links.pdf
- ServiceNow, *Business Rules: Complete Developer Guide 2025* (Jitendra Zaa) — https://www.jitendrazaa.com/blog/servicenow/servicenow-business-rules-complete-developer-guide-2025/
- Guidewire, *PolicyCenter data entities* — https://gwwars.s3.us-east-2.amazonaws.com/pc1012docs/10.x/active/config/topics/c_dz3179766.html
- Guidewire, *Data Model & Entities Tutorial* (CloudFoundation) — https://cloudfoundation.com/blog/guidewire-policy-center-training-tutorial-on-data-model-entities/
- SAP, *Concept of the MDG Data Modeling* — https://help.sap.com/docs/SAP_MASTER_DATA_GOVERNANCE/79ef8b1636dd492d8fd430d2d309b90f/4e3d83167d3248e6930929c13ec61a13.html
- SAP, *Definition of Validations and Derivations* — https://help.sap.com/saphelp_slc200/helpdata/en/6a/3110a501c14d0ba3d8e81516af03ec/content.htm
- SAP, *Explaining Master Data Quality: Process and Rules* — https://learning.sap.com/courses/sap-master-data-governance-on-sap-s-4hana/explaining-master-data-quality-process-and-rules
- Microsoft, *Business Rule Examples — SQL Server Master Data Services* — https://learn.microsoft.com/en-us/sql/master-data-services/business-rule-examples-master-data-services

### Schema and validation ecosystems

- JSON Schema, *Examples* — https://json-schema.org/learn/json-schema-examples
- JSON Schema, *Complete Tutorial* (JsonUtils) — https://jsonutils.org/blog/json-schema-complete-tutorial.html
- Zod, *Official Documentation* — https://zod.dev/
- FluentValidation, *Official Documentation* — https://docs.fluentvalidation.net/
- FluentValidation, *Deep Dive: Complex Rules, Rule Sets* (DevelopersVoice) — https://developersvoice.com/blog/dotnet/advanced-fluentvalidation-for-complex-rules/
- Pydantic, *Models Documentation* — https://pydantic.com.cn/en/concepts/models/
- Pydantic, *10 Real World Examples* (MarkAICode) — https://markaicode.com/10-real-world-pydantic-examples-in-python/
- OpenAPI, *Schema Object Specification* — https://swagger.io/specification/
- OpenAPI, *Designing Schemas* (Michael Heap) — https://michaelheap.com/openapi-schema-design/

### Rules engines

- NRules, *Official Documentation* — https://nrules.net/
- NRules, *GitHub Repository* — https://github.com/NRules/NRules
- Drools, *Language Reference* — https://docs.drools.org/8.39.0.Final/drools-docs/docs-website/drools/language-reference/index.html
- Drools, *Rule Engine Documentation* — https://docs.drools.org/latest/drools-docs/drools/rule-engine/index.html
- Microsoft, *RulesEngine* — https://microsoft.github.io/RulesEngine/
- *Rule Engines in .NET* (Yunier's Wiki) — https://yunier.dev/post/2023/rule-engines-in-dotnet/

### DDD and architecture

- Microsoft, *Implementing Value Objects* — https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects
- Baeldung, *Aggregate Root in DDD* — https://www.baeldung.com/cs/aggregate-root-ddd
- Dan Does Code, *DDD Modelling: Aggregates vs Entities* — https://www.dandoescode.com/blog/ddd-modelling-aggregates-vs-entities
- ABP.io, *Demystified Aggregates in DDD & .NET* — https://abp.io/community/articles/demystified-aggregates-in-ddd-and-dotnet-2becl93q

### ERP and master data

- Bluestonex, *7 Types of Master Data Objects with Examples* — https://bluestonex.com/knowledge-bank/7-types-of-master-data-with-examples-data-objects/
- Profisee, *What Is Master Data?* — https://profisee.com/blog/master-data-examples/
- Reltio, *Master Data vs Reference Data* — https://www.reltio.com/resources/blog/master-data-vs-reference-data-data-type-comparison/
- Dataversity, *Master Data vs. Reference Data* — https://www.dataversity.net/articles/master-data-vs-reference-data/

### Infrastructure-as-code precedent

- HashiCorp, *Data Sources — Configuration Language* — https://docs.devnetexperttraining.com/static-docs/Terraform/docs/language/data-sources/index.html
- Spacelift, *Terraform Data Sources* — https://spacelift.io/blog/terraform-data-sources-how-they-are-utilised

### State machine / actor precedent

- Stately, *XState Context Documentation* — https://stately.ai/docs/context
- XState by Example — https://xstatebyexample.com/
