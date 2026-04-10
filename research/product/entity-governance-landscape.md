# Entity Governance Landscape — Analog Research Memo

**Date:** 2026-04-08
**Researcher:** Steinbrenner (PM)
**Scope:** Competitive and analog landscape analysis for Precept's entity-first/data-centric repositioning
**Grounded in:** `design/brand/philosophy.md`, `research/brand/philosophy-draft-v2.md`, `research/product/data-vs-state-pm-research.md`, `research/brand/data-vs-state-philosophy.md`
**Requested by:** Shane

---

## Executive Summary

Precept occupies a structural gap in the developer tooling landscape. No existing tool combines **declared entity fields, lifecycle states, transition guards, and data constraints into a single enforceable contract**. Adjacent tools each govern a slice of this problem — validation libraries check data shape, state machines manage transitions, policy engines evaluate authorization, enterprise platforms bundle everything behind a proprietary runtime, and industry standards define vocabularies without enforcement. Precept is the first embeddable library that binds all of these concerns into one deterministic engine. This memo maps that landscape in detail and draws positioning conclusions.

---

## 1. Enterprise Platform Record Models

### 1.1 Salesforce (Standard & Custom Objects)

**What it is:** The dominant CRM/platform-as-a-service. Entities are modeled as "objects" (Account, Contact, Opportunity, or custom objects). Each object has fields, validation rules (formula expressions that reject saves), workflow rules, record-triggered flows, and approval processes.

**Entity/data governance model:**
- **Validation rules:** Formula expressions evaluated on record save. If the formula returns `true`, the save is rejected with an error message. Rules can reference any field on the record and related records.
- **Record types + page layouts:** Control which fields are visible/required per business process.
- **Workflow rules / Flows:** Automate field updates, email alerts, and task creation on record changes. Record-triggered flows provide state-transition-like automation.
- **Approval processes:** Sequential or parallel approval chains with entry criteria, approval steps, and actions on approve/reject — the closest Salesforce gets to a lifecycle state machine.

**What Precept does that Salesforce doesn't:**
- **Structural prevention.** Salesforce validation rules run inside a broader save pipeline and surface as save-time errors. Precept's constraints are the contract itself — there is no separate validator, flow, or trigger layer to keep in sync.
- **Full inspectability.** There is no Salesforce equivalent of `Inspect` — you cannot ask "what would happen if I changed this field to X?" without actually attempting the DML and catching the exception.
- **Co-defined lifecycle and data rules in one artifact.** Salesforce scatters governance across validation rules, flows, approval processes, field-level security, and Apex triggers. Precept declares all rules in one `.precept` file.
- **Determinism.** Salesforce execution order (before triggers → validation rules → after triggers → workflows → flows) has complex interaction effects. Precept's pipeline is a fixed, documented sequence with no side effects.

**What Salesforce does that Precept doesn't:**
- Full persistence, UI, API, and multi-tenancy — it's a platform, not a library.
- Cross-object relationships, rollup summaries, sharing rules, and org-wide security.
- Ecosystem: AppExchange, declarative tooling for non-developers, thousands of ISV integrations.

**Positioning implication:** Precept provides *Salesforce-grade entity governance* as an embeddable .NET library — the same category of protection (field validation + lifecycle gating + constraint enforcement) without the platform dependency. This is the strongest analog for explaining what Precept does to a non-technical audience.

---

### 1.2 ServiceNow (Tables, Business Rules, Flows)

**What it is:** Enterprise IT service management and workflow platform. Entities are modeled as tables (Incident, Change Request, Problem, or custom tables). Governance is implemented via business rules (server-side JavaScript triggered on table operations), client scripts, UI policies, data policies, and Flow Designer workflows.

**Entity/data governance model:**
- **Business rules:** Execute before/after insert/update/delete/query on a table. Can abort operations, modify field values, or enforce conditions. Analogous to database triggers.
- **Data policies:** Enforce mandatory/read-only field constraints independent of the UI form — the closest to Precept's `edit` declarations.
- **State model:** ServiceNow uses a `State` field with configurable transitions on ITSM tables. Transition conditions are configured per table, with optional "from/to" enforcement.
- **Flow Designer:** Visual workflow automation with triggers, conditions, and actions.

**What Precept does that ServiceNow doesn't:**
- **One-file completeness.** ServiceNow governance is distributed across business rules, data policies, ACLs, client scripts, UI policies, and flows — often with conflicting or redundant logic.
- **Compile-time analysis.** ServiceNow business rules can have runtime errors, unreachable conditions, and conflicting logic that is only discovered at execution time.
- **Tighter enforcement surface.** ServiceNow enforcement is split across several platform features and execution surfaces. Precept keeps the rule surface smaller: the entity contract and the engine that executes it.

**What ServiceNow does that Precept doesn't:**
- Full ITSM/ITOM/CSM workflow platform with UI, integrations, CMDB, and orchestration.
- Multi-table relationships, access control lists, and approval workflows.

**Positioning implication:** Same as Salesforce — Precept is the *library-weight* version of what these platforms achieve at *platform-weight*. For teams building on .NET instead of buying a platform, Precept fills the entity governance gap.

---

### 1.3 Guidewire (InsuranceSuite Entity Models)

**What it is:** A dominant insurance core platform. InsuranceSuite (PolicyCenter, ClaimCenter, BillingCenter) models insurance entities (Policy, Claim, Billing Account) with complex lifecycle states, validation rules, and business rules written in Gosu (a custom JVM language).

**Entity/data governance model:**
- **Entity model:** Strongly-typed entity definitions with fields, relationships, and type hierarchies.
- **Validation rules:** Gosu-based validation methods on entity classes, invoked at specific lifecycle points.
- **State model:** Claims follow a lifecycle (Open → Investigation → Negotiation → Closed); policies follow (Draft → Quoted → Bound → In-Force → Cancelled). State transitions gate which operations are available.
- **Business rules:** Gosu scripts that enforce data conditions, calculate derived values, and control transitions.

**What Precept does that Guidewire doesn't:**
- **Platform-free.** Guidewire is a multi-million-dollar platform commitment. Precept is a NuGet package.
- **Declarative.** Guidewire rules are imperative Gosu code scattered across entity classes. Precept rules are declarative DSL in a single file.
- **Inspectability.** No Guidewire equivalent of non-mutating `Inspect` across all possible events.

**What Guidewire does that Precept doesn't:**
- Complete insurance domain model, integrations, analytics, cloud infrastructure.
- Industry-specific entity hierarchies (Coverage, Exposure, Reserve, Payment).

**Positioning implication:** Precept can model the *same entity governance patterns* Guidewire uses (lifecycle-gated data validation on insurance entities) in a fraction of the complexity. For insurance teams building outside Guidewire, or for adjacent industries with similar lifecycle-heavy entities, Precept is the natural tool.

---

## 2. Master Data Management (MDM)

**What it is:** A discipline (and category of platforms: Informatica MDM, SAP MDM, Reltio, Semarchy) for ensuring that an organization's critical reference data (customer, product, supplier records) is consistent, accurate, and governed across systems.

**Entity/data governance model:**
- **Golden record management:** Deduplication, matching, and merging of entity records across sources.
- **Data quality rules:** Field-level validation, completeness checks, standardization rules.
- **Stewardship workflows:** Human review workflows for data changes that fail automated rules.
- **Data governance policies:** Organizational rules about who can create/modify/approve master data.

**What Precept does that MDM doesn't:**
- **Lifecycle-aware constraints.** MDM validates data quality (format, completeness, consistency) but has no concept of entity lifecycle — a product record in `Draft` vs. `Active` vs. `Retired` all face the same validation rules. Precept's state-conditional constraints make data rules lifecycle-dependent.
- **Structural prevention.** MDM flags data quality issues for stewardship review. Precept rejects invalid configurations at the engine boundary.
- **Embeddable enforcement.** MDM is infrastructure — ETL pipelines, matching engines, stewardship UIs. Precept is a library that enforces rules at the code layer.

**What MDM does that Precept doesn't:**
- Cross-system record matching, deduplication, and survivorship logic.
- Data lineage and provenance tracking.
- Enterprise-scale data governance workflows with human-in-the-loop stewardship.

**Positioning implication:** MDM governs data *across systems*. Precept governs data *within an entity*. They are complementary. A Precept definition could enforce the structural rules that an MDM platform checks — but at the application boundary, not the integration layer. For teams that need MDM-grade data quality rules at the code level (e.g., microservice-owned entities), Precept is the enforcement mechanism MDM can't provide.

---

## 3. Decision & Policy Engines

### 3.1 DMN (Decision Model and Notation)

**What it is:** An OMG standard for modeling business decisions as decision tables, decision requirement diagrams, and FEEL expressions. Used in BPM suites (Camunda, IBM, Trisotech) to externalize decision logic from process code.

**Entity/data governance model:**
- **Decision tables:** Map input conditions to output values. Stateless: given inputs, produce outputs.
- **FEEL expressions:** Typed expression language for evaluating conditions and computing values.
- **Decision services:** Invoked as stateless, side-effect-free evaluations from BPMN processes.

**What Precept does that DMN doesn't:**
- **Entity binding.** DMN evaluates decisions against *presented* input data — it has no concept of a persistent entity with fields, states, or a lifecycle. Precept's rules are bound to the entity.
- **Mutation + constraint enforcement.** DMN is read-only evaluation. Precept fires transitions that mutate data and validates the result.
- **Lifecycle awareness.** DMN has no state model. Rules don't vary by lifecycle position.

**What DMN does that Precept doesn't:**
- **Business-user authoring.** Decision tables are designed for non-technical users. Precept's DSL targets developers.
- **Process integration.** DMN is designed to be invoked from BPMN processes.
- **Standard interoperability.** DMN models are portable across compliant engines.

**Positioning implication:** DMN answers "what should the decision be?" Precept answers "what is the entity allowed to become?" DMN could theoretically be used to *compute* values that Precept constraints *enforce* — they address different lifecycle moments. Precept is not a decision engine; it is the enforcement layer that ensures decisions, once made, produce valid entity configurations.

---

### 3.2 OPA / Rego (Open Policy Agent)

**What it is:** A CNCF-graduated general-purpose policy engine. Policies are written in Rego, a declarative language. OPA evaluates queries against structured data (JSON) and policies to produce allow/deny decisions or arbitrary structured output. Primary use cases: Kubernetes admission control, API authorization, CI/CD policy.

**Entity/data governance model:**
- **Policy-as-code:** Rego policies define invariants over structured data. Evaluation is stateless — OPA receives input data, evaluates policies, and returns a decision.
- **Decoupled enforcement:** OPA decouples policy *decision* from policy *enforcement*. The calling system must implement enforcement.
- **No entity model.** OPA has no concept of entities, fields, lifecycle, or state. It evaluates rules against whatever JSON is presented.

**What Precept does that OPA doesn't:**
- **Entity binding.** Precept rules are declared on a named entity with typed fields. OPA rules are generic over arbitrary JSON.
- **State machine.** OPA has no lifecycle concept. Rules cannot vary by entity state.
- **Mutation.** OPA is purely evaluative — it doesn't change data. Precept fires transitions that mutate and validate.
- **Structural enforcement.** OPA produces a decision; the calling code must respect it. Precept's runtime *is* the enforcement — invalid mutations are structurally rejected.

**What OPA does that Precept doesn't:**
- **Infrastructure-scope policy.** Kubernetes admission, API gateway authorization, CI/CD gates — OPA operates at the infrastructure layer.
- **Language-agnostic.** OPA is a standalone service callable from any language. Precept is .NET-specific.
- **Distributed policy management.** OPA bundles, discovery services, and decision logging.

**Positioning implication:** OPA governs *who can do what* at call boundaries. Precept governs *what the entity can become* at the data layer. OPA is an authorization engine; Precept is an integrity engine. In a complete system, OPA might gate whether a user can attempt an operation, and Precept would enforce whether the resulting entity configuration is valid.

---

### 3.3 Cedar (Amazon)

**What it is:** A policy language developed by Amazon (open-sourced, used in Amazon Verified Permissions). Cedar policies express fine-grained authorization: `permit` or `forbid` a `principal` performing an `action` on a `resource`, with conditions.

**Entity/data governance model:**
- **RBAC + ABAC.** Cedar supports role-based and attribute-based access control through entity hierarchies and condition clauses (`when`/`unless`).
- **Schema-validated.** Cedar policies can be validated against a schema that defines entity types, actions, and attribute shapes.
- **Stateless evaluation.** Like OPA, Cedar evaluates authorization requests — it has no persistence, no lifecycle, no mutation.

**What Precept does that Cedar doesn't:**
- Everything from the OPA comparison applies. Cedar is authorization-specific — even more narrowly scoped than OPA.
- **Entity lifecycle.** Cedar's "entity" concept is for authorization hierarchy (User → Group → Role), not for business entity governance with fields and state.

**What Cedar does that Precept doesn't:**
- **Formal verification.** Cedar's language is designed for automated reasoning — policies can be mathematically proven to never conflict.
- **Cloud-native authorization.** AWS Verified Permissions provides managed Cedar evaluation.

**Positioning implication:** Cedar is the strongest example of a tool with "entity" in its vocabulary but a completely different meaning. Cedar entities are authorization subjects/resources; Precept entities are business records with governed data. No overlap in function. Useful for clarifying that "entity governance" means data integrity, not access control.

---

## 4. Pure Validation Tools

### 4.1 FluentValidation

**What it is:** The dominant .NET validation library (~250M NuGet downloads). Strongly-typed validation rules built with a fluent API. Validators are classes that define rules for a POCO type.

**Entity/data governance model:**
- **Rule chains on POCOs.** `RuleFor(x => x.Name).NotEmpty().MaximumLength(100)`.
- **Conditional rules.** `When(x => x.Status == "Active", () => { ... })` — developers manually implement state-aware validation.
- **External to the entity.** Validators are separate classes invoked explicitly by calling code. Nothing prevents bypassing the validator.

**What Precept does that FluentValidation doesn't:**
- **State-aware constraints.** FluentValidation's `When` clauses are ad-hoc — the developer must manually synchronize validation conditions with entity state. Precept's constraints are structurally bound to states.
- **Lifecycle model.** No transitions, no guards, no inspectability. FluentValidation validates a snapshot; it has no concept of "moving from Draft to Submitted requires these conditions."
- **Structural enforcement.** FluentValidation runs when called. Precept's engine enforces constraints on every mutation — there is no code path that bypasses the contract.
- **Inspectability.** No equivalent of `Inspect` — you cannot ask FluentValidation "what would fail if I changed this field?"

**What FluentValidation does that Precept doesn't:**
- **Broader validation surface.** Regex, email format, credit card, cross-property comparison, collection validation, inheritance-based rule composition.
- **ASP.NET integration.** Automatic model validation in MVC/API pipelines.
- **Community and ecosystem.** 10+ years, massive adoption, extensive documentation.

**Positioning implication:** FluentValidation is Precept's most important *negative differentiator*. The positioning must explicitly address: "If FluentValidation is enough for your entity, use FluentValidation. Precept is for when validation depends on lifecycle state and must be structurally enforced." The 10:1 download ratio (FluentValidation vs. Stateless) confirms that most developers search for validation first. The developer who needs Precept has *already tried* FluentValidation and found it insufficient because their rules change with state.

---

### 4.2 JSON Schema

**What it is:** A declarative language for defining structure and constraints of JSON data. Used for API contract validation, configuration validation, and data exchange.

**Entity/data governance model:**
- **Structural constraints.** Type, format, required fields, min/max, pattern, enum, array item constraints.
- **Conditional schemas.** `if/then/else`, `oneOf`, `anyOf` — can express "if field A equals X, then field B must satisfy Y."
- **Static analysis.** Schema defines the shape; validation checks conformance at a point in time.

**What Precept does that JSON Schema doesn't:**
- **Lifecycle.** JSON Schema validates a document snapshot. No state, no transitions.
- **Mutation enforcement.** JSON Schema checks a finished document — it doesn't prevent invalid intermediate states during construction.
- **Behavioral rules.** No guards, no events, no inspectability.

**What JSON Schema does that Precept doesn't:**
- **Language-agnostic.** Used across every programming language.
- **API contract standard.** Integral to OpenAPI/Swagger specifications.
- **Ecosystem.** Enormous tooling: code generation, documentation, form builders.

**Positioning implication:** JSON Schema validates *documents*. Precept governs *entities*. A `.precept` definition is conceptually a "JSON Schema that knows about lifecycle" — but the implementation gap is vast. JSON Schema is the "good enough" baseline that demonstrates why static validation alone is insufficient for entities with lifecycle.

---

### 4.3 Ardalis.GuardClauses

**What it is:** A .NET library (~20M downloads) for inline precondition checks. `Guard.Against.Null(order)`, `Guard.Against.NegativeOrZero(quantity)`. Imperative, assertion-style validation at method entry points.

**Entity/data governance model:**
- **Imperative preconditions.** Each guard is a manual call at a specific code location. No centralized declaration.
- **Fail-fast pattern.** Throws immediately on violation — no state awareness, no lifecycle.

**What Precept does that GuardClauses doesn't:**
- Everything — different categories entirely. GuardClauses is method-level input validation. Precept is entity-level lifecycle governance. The comparison is useful only to show the spectrum: GuardClauses (imperative, per-call) → FluentValidation (declarative, per-type) → Precept (declarative, per-entity-lifecycle).

**Positioning implication:** GuardClauses anchors the "imperative validation" end of the spectrum. Useful in copy as the "before" state: "You started with Guard.Against.Null. You moved to FluentValidation for centralized rules. Now your rules depend on entity lifecycle — Precept is the next step."

---

## 5. Industry Vocabularies & Standards

### 5.1 FHIR (Fast Healthcare Interoperability Resources)

**What it is:** The dominant healthcare data exchange standard (HL7, v5.0 R5). Defines ~150 resource types (Patient, Observation, MedicationRequest, Claim, etc.) with typed fields, cardinality constraints, terminology bindings, and extension mechanisms.

**Entity/data governance model:**
- **Resource definitions.** Each FHIR resource has a StructureDefinition specifying fields, types, cardinality (0..1, 1..*, etc.), and terminology bindings.
- **Profiles.** Constrain base resources for specific use cases — a US Core Patient profile requires SSN-format identifiers.
- **Validation.** FHIR validators check resource instances against profiles. Validation is structural (required fields, type conformance) and terminological (coded values from required value sets).
- **No lifecycle model.** FHIR resources represent a snapshot. Resource status fields (e.g., `MedicationRequest.status`) are just coded values — FHIR does not enforce valid status transitions.

**What Precept does that FHIR doesn't:**
- **Lifecycle enforcement.** FHIR's `status` fields are unconstrained coded values — nothing prevents a MedicationRequest from going directly from `draft` to `completed` without passing through `active`. Precept's state machine enforces valid transitions.
- **Data-state binding.** FHIR doesn't know that a `completed` MedicationRequest should have a `dispenseRequest` populated. Precept's state-conditional constraints enforce this.
- **Runtime prevention.** FHIR validation is a check-after-the-fact pass. Precept prevents invalid configurations at the mutation boundary.

**What FHIR does that Precept doesn't:**
- **Domain vocabulary.** 150+ resource types with standardized semantics, terminologies, and exchange protocols.
- **Interoperability.** REST API conventions, search parameters, bulk data access, SMART-on-FHIR authorization.
- **Ecosystem.** EHR integrations, national standards (US Core, AU Base), tooling across every language.

**Positioning implication:** FHIR defines *what a healthcare entity looks like*. Precept can enforce *what the entity is allowed to become*. A FHIR MedicationRequest resource could be modeled as a `.precept` file that declares the resource's fields, valid status transitions, and state-conditional required fields — giving FHIR's vocabulary an enforcement engine it currently lacks. This is a powerful domain-specific value story.

---

### 5.2 ACORD (Insurance Data Standards)

**What it is:** The insurance industry's data standard body. ACORD defines standardized transaction types, forms, and XML/JSON schemas for data exchange between insurance carriers, agents, brokers, and reinsurers. It is used across global P&C, life, annuity, and adjacent insurance markets.

**Entity/data governance model:**
- **Transaction schemas.** Typed message definitions for insurance operations (policy submission, claim notification, endorsement).
- **Code lists.** Standardized enumerated values for coverage types, loss types, transaction types.
- **No enforcement.** ACORD defines structure and vocabulary. Enforcement is the responsibility of consuming systems.

**What Precept does that ACORD doesn't:**
- **Enforcement.** ACORD schemas define the shape of insurance data exchanges. Precept can enforce that an insurance entity modeled against ACORD's vocabulary satisfies structural rules at every lifecycle point — draft through binding through endorsement.
- **Lifecycle.** ACORD transactions are events, not lifecycle states. Precept adds the lifecycle structure ACORD doesn't provide.

**What ACORD does that Precept doesn't:**
- Industry-standard vocabularies, code lists, and message schemas adopted across the insurance ecosystem.

**Positioning implication:** Like FHIR, ACORD is a vocabulary Precept can *enforce*. An insurance entity modeled in a `.precept` file using ACORD field names and code lists would give teams a declarative, enforceable entity contract grounded in industry standard semantics. This is especially relevant given Guidewire's dominance — teams building outside Guidewire need entity governance for ACORD-conformant data.

---

### 5.3 ISO 20022 (Financial Messaging)

**What it is:** The universal financial industry message scheme. Defines a modeling methodology, central business dictionary, and XML/ASN.1 schemas for financial messages (payments, securities, trade finance, FX). Being adopted by SWIFT, the Federal Reserve (FedNow), ECB (TARGET), and major payment networks globally.

**Entity/data governance model:**
- **Business components.** Reusable data definitions (PartyIdentification, AccountIdentification, Amount) with typed fields and constraints.
- **Message definitions.** Complete transaction message structures composed from business components.
- **No runtime enforcement.** Like FHIR and ACORD, ISO 20022 defines vocabulary and structure — enforcement is left to implementers.

**What Precept does that ISO 20022 doesn't:**
- **Entity lifecycle.** A payment instruction moves through states (Initiated → Validated → Authorized → Settled → Reconciled). ISO 20022 defines the message at each point but doesn't enforce valid transitions between them.
- **Runtime enforcement.** ISO 20022 schemas validate message shape. Precept enforces entity integrity across the lifecycle.

**Positioning implication:** Financial instruments, payment instructions, and trade transactions are lifecycle-heavy entities with strict data rules that change by state — exactly Precept's target. Modeling ISO 20022 business components as `.precept` fields and lifecycle states as Precept states would give financial systems a runtime enforcement layer over the standard's vocabulary.

---

## 6. State Machine & Workflow Tools

### 6.1 Stateless (.NET)

**What it is:** The dominant .NET state machine library (~25M NuGet downloads). Lightweight, fluent API for defining states, triggers, guard clauses, entry/exit actions, hierarchical states, and parameterized triggers.

**Entity/data governance model:**
- **State + triggers.** States and triggers are generic .NET types (enums, strings, etc.). Transitions are configured with `Configure(State).Permit(Trigger, DestinationState)`.
- **Guard clauses.** `PermitIf(Trigger, State, () => condition)` — guards are arbitrary C# lambdas, not declared constraints.
- **No data model.** Stateless has no concept of entity fields. State is a single value; there is no declared field schema, no constraints on data, no invariants.
- **Entry/exit actions.** Side effects on state transitions — arbitrary code, not enforced constraints.

**What Precept does that Stateless doesn't:**
- **Declared field model.** Precept defines the entity's fields, their types, default values, and editability per state. Stateless tracks a single state value.
- **Data constraints.** Precept enforces invariants and state-conditional assertions on field data. Stateless cannot express "in state Approved, amount must be > 0."
- **Inspectability.** Stateless offers `PermittedTriggers` (which triggers are valid) but cannot predict *what would happen* to data if a trigger fired — no `Inspect` equivalent.
- **Compile-time analysis.** Precept's compiler detects unreachable states, dead ends, type errors. Stateless has no static analysis.
- **DSL.** Precept's `.precept` file is a declarative, readable artifact. Stateless configuration is imperative C# code.

**What Stateless does that Precept doesn't:**
- **Hierarchical states.** Stateless supports substates — `OnHold` as a substate of `Connected`. Precept's state model is flat.
- **External state storage.** Stateless can read/write state from any backing store via function delegates.
- **Lightweight embedding.** Stateless is ~zero overhead — a single class with no DSL parsing. Precept has compilation cost.

**Positioning implication:** Stateless is Precept's most direct competitor and the most important differentiation target. The replacement story is: *"You're using Stateless for your entity's lifecycle + FluentValidation for your entity's data rules + manual glue code to connect them. Precept replaces all three with one contract."* This three-tool-replacement narrative is the strongest adoption angle.

---

### 6.2 XState (JavaScript/TypeScript)

**What it is:** The leading JavaScript state machine and statecharts library. Implements the actor model with state machines and statecharts (hierarchical states, parallel states, history states). Provides context (data), actions, guards, and invoked services.

**Entity/data governance model:**
- **Context.** XState machines have a typed `context` object — the closest analog to Precept's fields. Context is mutated via `assign` actions on transitions.
- **Guards.** Conditional transitions based on context values and event data.
- **No constraints.** XState does not enforce invariants on context data. A guard prevents a transition from firing, but nothing prevents an `assign` action from setting context to an invalid state.
- **Actions are imperative.** `assign` actions are JavaScript functions — they can set context to anything.

**What Precept does that XState doesn't:**
- **Post-mutation constraint enforcement.** Precept evaluates constraints *after* mutation — if the resulting configuration is invalid, the entire transition is rejected. XState's `assign` always succeeds; there is no rollback.
- **Declared field schema.** XState's `context` is a JavaScript object — no declared type constraints beyond what TypeScript provides at compile time.
- **Inspectability with data outcomes.** XState's `getNextSnapshot` can preview the next state but doesn't check data validity of the result.

**What XState does that Precept doesn't:**
- **Statecharts.** Hierarchical states, parallel states, history states — full Harel statechart semantics.
- **Actor model.** Spawned actors, invoked services, inter-machine communication.
- **Visual tooling.** Stately Studio provides a visual editor and inspector for state machines.
- **Cross-platform.** JavaScript runs everywhere — browser, Node, Deno, Bun.

**Positioning implication:** XState is the strongest intellectual analog — it's the closest any tool comes to Precept's vision of "state machine + data context." But XState stops short of *enforcing* data validity. Its context mutations always succeed. Precept's rollback-on-constraint-violation is the structural guarantee XState lacks. For .NET developers familiar with XState's concepts, Precept is "XState with data enforcement."

---

### 6.3 MassTransit Saga (State Machines)

**What it is:** MassTransit's saga state machine — a distributed process orchestrator built on message-based communication. Sagas track the state of long-running distributed transactions, correlating messages to saga instances stored in persistent repositories (EF Core, MongoDB, Redis, etc.).

**Entity/data governance model:**
- **Saga state.** Sagas have a `CorrelationId`, a `CurrentState`, and custom properties (e.g., `OrderDate`).
- **State machine.** States, events, and transitions defined with MassTransit's fluent API. Guards via `If`/`IfElse` on transitions.
- **Message-driven.** All state transitions are triggered by messages — no direct mutation API.
- **No data constraints.** Saga properties can be set to any value during transitions. No invariant enforcement.

**What Precept does that MassTransit Saga doesn't:**
- **Data integrity.** MassTransit sagas track process state but don't enforce data constraints. A saga's `OrderDate` can be null in `Completed` state — nothing prevents it.
- **Single-entity scope.** Precept governs one entity's integrity. MassTransit orchestrates multi-service distributed processes.
- **Inspectability.** No equivalent of `Inspect` — you cannot preview what a message would do to a saga without sending it.
- **Declarative.** Precept is a DSL file. MassTransit sagas are C# classes with fluent configuration.

**What MassTransit Saga does that Precept doesn't:**
- **Distributed orchestration.** Message-based, multi-service process coordination with compensation (saga pattern).
- **Persistence.** Built-in saga repositories with optimistic/pessimistic concurrency.
- **Transport integration.** RabbitMQ, Azure Service Bus, Amazon SQS, etc.

**Positioning implication:** MassTransit sagas operate at the *process* layer; Precept operates at the *entity* layer. They are complementary, not competitive. A MassTransit saga might orchestrate a multi-step approval process, and at each step, fire a Precept event to ensure the entity remains valid. This is the strongest "plays well with" story in the landscape.

---

### 6.4 Elsa Workflows

**What it is:** An open-source .NET workflow engine. Workflows are defined programmatically (C#) or visually (built-in designer). Activities are the building blocks — HTTP request triggers, timers, conditional branches, code execution, database operations.

**Entity/data governance model:**
- **Workflow state.** Workflows track execution position (current activity, branch, etc.) and workflow variables.
- **No entity model.** Elsa orchestrates *processes*, not *entities*. There are no declared fields, no constraints, no entity lifecycle.
- **Activity-based.** Each step is an activity — code execution, HTTP call, delay, branch. Activities are behavioral, not declarative.

**What Precept does that Elsa doesn't:**
- **Entity governance.** Elsa has no concept of entity fields, constraints, or invariants. It orchestrates work; Precept governs data.
- **Declarative contract.** Precept is a single DSL file. Elsa workflows are procedural activity graphs.
- **Data enforcement.** Elsa variables have no constraints. Precept fields have typed constraints enforced at every mutation.

**What Elsa does that Precept doesn't:**
- **Workflow orchestration.** Long-running processes, timers, HTTP triggers, human tasks, parallel branches.
- **Visual designer.** Browser-based drag-and-drop workflow builder.
- **Activity library.** Extensible activity types for integration with external systems.

**Positioning implication:** Elsa is firmly in the "workflow engine" category that Precept has explicitly moved away from. The distinction is clear: Elsa orchestrates *what happens next* in a process; Precept governs *what the entity is allowed to become*. If someone compares Precept to Elsa, the framing is wrong — redirect to the entity governance frame.

---

## 7. Strategic Analysis

### 7.1 Unique Position

**Precept is the only tool that binds declared entity fields, lifecycle states, transition guards, and data constraints into one enforceable contract — as an embeddable library.**

**2x2 in words:** imagine one axis as **lifecycle awareness** and the other as **structural data enforcement**. Validation libraries sit high on data enforcement but low on lifecycle. State machine libraries sit high on lifecycle but low on data enforcement. Policy engines sit off to the side — strong on decisioning, weak on entity ownership. Salesforce, ServiceNow, and Guidewire occupy the "both" corner, but only as full platforms. **Precept occupies that same "both" corner in embeddable-library form.**

The landscape breaks into clear lanes:

| Lane | Tools | What's missing |
|------|-------|---------------|
| Validates data, ignores lifecycle | FluentValidation, JSON Schema, GuardClauses | No state awareness, no transition enforcement |
| Manages lifecycle, ignores data | Stateless, XState, MassTransit Saga | No field schema, no data constraints |
| Evaluates policy, ignores entity | OPA, Cedar, DMN | No entity binding, no mutation, no persistence |
| Defines vocabulary, no enforcement | FHIR, ACORD, ISO 20022 | Structural definitions only — enforcement is left to implementers |
| Full governance, requires platform | Salesforce, ServiceNow, Guidewire | Platform dependency, vendor lock-in, not embeddable |

Precept is the only entry in the empty cell: **embeddable entity governance with lifecycle-aware data enforcement**. This is not a competitive position against any single tool — it is a *structural gap* in the tooling landscape.

### 7.2 Biggest Adoption Funnel

The PM research (`data-vs-state-pm-research.md`) identified the 10:1 download ratio between FluentValidation (~250M) and Stateless (~25M). The primary funnel insight:

**The developer who needs Precept has already outgrown FluentValidation + Stateless used separately.** Their entry query is some variant of:
- "validation depends on entity state"
- "different validation rules per status"
- "enforce invariants across state transitions"
- "state machine with data constraints"

The adoption funnel is:
1. **Awareness:** "Most validation libraries don't know about state. Most state machines don't know about data."
2. **Recognition:** Developer recognizes their exact pain — ad-hoc `if (status == "Approved")` checks scattered across validators.
3. **Evaluation:** "Precept binds both into one contract where invalid configurations are structurally impossible."
4. **Proof:** Show a `.precept` file vs. the equivalent Stateless + FluentValidation + glue code.

### 7.3 Combination-of-Tools Replacement Story

The strongest positioning narrative is the **three-tool replacement**:

> *"Today you're using three tools that don't talk to each other:*
> - *Stateless for your entity's lifecycle (what state is it in? what transitions are allowed?)*
> - *FluentValidation for your entity's data rules (is the data valid?)*
> - *Manual glue code to connect them (if state is X, run validator Y)*
>
> *Precept replaces all three with one `.precept` file. The lifecycle and the data rules are co-defined. The glue code disappears. Invalid configurations become structurally impossible."*

This narrative works because:
1. It names tools the developer already knows and uses.
2. It identifies the specific failure mode (the glue code that can be bypassed/inconsistent).
3. It offers a concrete reduction (three artifacts → one).
4. The proof is immediate: show the `.precept` file.

For more sophisticated audiences (enterprise architects, platform teams), add the platform analogy:

> *"Salesforce and ServiceNow solve this at the platform level — validation rules, workflows, and state management all in one system. Precept provides the same category of entity governance as an embeddable .NET library."*

### 7.4 Does "Domain Integrity Engine" Still Fit?

**Yes — and the entity-first reframe strengthens it.**

- **"Domain"** → the business domain. Entity-first framing makes this more concrete — these are domain entities (orders, claims, applications), not abstract state machines.
- **"Integrity"** → data integrity across the lifecycle. The reframe from "invalid states" to "invalid configurations" makes "integrity" carry more weight — it's about the *data*, not just the state label.
- **"Engine"** → a runtime that compiles and executes. This distinguishes Precept from passive validators (FluentValidation) and external decision services (OPA, DMN).

The category name works because:
1. No other tool claims it — it's genuinely unique.
2. It scales across the landscape: Precept provides *domain integrity* whether compared to validation tools, state machines, policy engines, or enterprise platforms.
3. It doesn't over-index on state machines (avoiding the "why not Stateless?" objection) or on validation (avoiding the "why not FluentValidation?" objection).

The only risk is that "domain integrity" requires education — it's a *category creation* play, not a *category capture* play. But the research confirms this is the correct strategy: there is no existing category to capture, because the gap Precept fills has never had an incumbent.

---

## 8. Recommended Copy Angles

### Primary (Hero)

> "Most validation libraries don't know about state. Most state machines don't know about data. Precept binds both into one executable contract where invalid configurations are structurally impossible."

### Category Anchor

> "Precept is a domain integrity engine for .NET."

### Three-Tool Replacement

> "Replace Stateless + FluentValidation + manual glue with one `.precept` file."

### Platform Analog

> "Salesforce-grade entity governance. NuGet-package weight."

### Standards Enforcement

> "FHIR defines what a resource looks like. ACORD defines what an insurance transaction contains. ISO 20022 defines what a payment message carries. Precept enforces what the entity is *allowed to become*."

### Spectrum Position

> "Guard clauses validate inputs. FluentValidation validates data. Precept governs entities."

### For the Burned Developer

> "You've been burned by a CancelledOrder with Total > 0. By an Approved loan with missing signatures. By a validator that didn't know which state made the field required. Precept makes those configurations structurally impossible."

---

## Selected Research Touchpoints

- Salesforce Trailhead — Data Modeling / Objects Intro: standard objects, custom objects, fields, records
- ServiceNow Docs — Business Rules: server-side rules triggered on table operations
- Guidewire Developer APIs / InsuranceSuite materials: business entities and insurance domain APIs
- IBM Think / Informatica glossary — Master Data Management: golden records, cross-system consistency, stewardship
- Camunda DMN reference — decision tables, inputs, outputs, rules
- Open Policy Agent docs — decoupled policy decision-making over structured JSON input
- Cedar language guide / AWS Verified Permissions docs — authorization policies, principals, actions, resources, context
- FluentValidation docs — strongly-typed validation rules for .NET
- JSON Schema ecosystem materials — document-structure and constraint validation
- HL7 FHIR overview and resource list — resources, StructureDefinition, profiles, constraints
- ACORD standards site — insurance data standards and implementation guides
- ISO 20022 message definitions catalogue — business areas, message sets, schemas
- Stateless README — state machines, triggers, guards, hierarchical states, introspection
- XState docs — actor-based state management, statecharts, context
- MassTransit saga docs — long-lived, message-driven stateful transactions with persistence
- Elsa Workflows docs — .NET workflow libraries, visual designer, long-running workflows

*End of memo.*
