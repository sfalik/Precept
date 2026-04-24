# Precept philosophy

> **Authority boundary:** This file lives in `docs/`, the repository's legacy/current reference set. Use it for the implemented v1 surface, current product reference, or historical context. If you are designing or implementing `src/Precept.Next` / the v2 clean-room pipeline, start in [docs.next/README.md](../docs.next/README.md) instead.

---

## What the product does

Precept is a domain integrity engine for .NET — a single declarative contract that governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible. You declare an entity's fields, constraints, lifecycle states, and transitions in one `.precept` file. The runtime compiles that declaration into an immutable engine that enforces every rule on every operation — no invalid combination of lifecycle position and field data can persist.

That works for lifecycle-driven entities (loan applications, insurance claims, contracts), for validation-heavy entities whose correctness is the primary concern (medical claims, financial instruments, regulatory filings), for reference and configuration entities that rarely change but must always be structurally sound (product catalogs, policy records), and for data-only entities that need no lifecycle at all (address records, patient demographics, counterparty identities). The unifying principle is governed integrity — the entity's data satisfies its declared rules at every moment, whether those rules involve lifecycle transitions, field constraints, or both.

If you're writing service-layer code that manually enforces business rules across entity lifecycle transitions — rules scattered across validators, event handlers, and state checks — Precept gives you one contract that replaces all of that.

In every real business domain, data and reference entities outnumber workflow entities. Insurance has Claims but also Adjusters, Coverage Types, Rate Tables, and Policy Templates. Finance has Loan Applications but also Customers, Fee Schedules, and Rate Cards. A product that governs only the workflow side covers the minority. Precept governs the entire domain — lifecycle entities and data-only entities alike — in one contract language.

A `.precept` file declares the entity's **fields** (scalar values and typed collections), **states** (the positions in its lifecycle, when lifecycle matters), **events** (the triggers that select which transition to execute, carrying typed arguments into the operation), **rules** (constraints that must hold about the entity's data — unconditionally, or only when a declared guard condition is met), and **ensures** (state- or event-scoped constraints that must hold in specific contexts). Every constraint carries a mandatory reason — the engine requires not just the rule, but its rationale. The runtime compiles the file into an immutable engine — validating structural soundness at compile time — and exposes four operations:

- **CreateInstance**: start an entity in its initial state with default field values.
- **Inspect**: ask "what would happen if I fired this event?" for every event, from any state. Non-mutating. The answer is always available.
- **Fire**: execute a transition. The engine validates input arguments, selects a matching transition row via guards, executes mutations, evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds. If any fails, the transition is rejected — the invalid configuration never exists. An event can also be explicitly forbidden — a deliberate prohibition, not just an unrouted trigger.
- **Update**: edit a field directly. The same constraint enforcement applies. Which fields can change depends on where the entity is in its lifecycle — the definition declares this, and the engine enforces it.

The engine is deterministic — same definition, same data, same outcome. Nothing is hidden.
Precept does not present approximation as exactness. If a value or operation is inherently approximate, that fact must be explicit in the contract. Exact-value lanes remain exact.

A contract is only trustworthy if its semantics are honest at the surface. Silent approximation inside an exact-looking path weakens the user's ability to reason about outcomes, even when the runtime is internally consistent. Precept therefore draws a hard line between exact and approximate behavior and requires that line to be visible in the type system and public surface. If approximation is part of the domain, the contract must say so plainly so inspection, enforcement, and consuming code all operate against the same truth.

---

## The hierarchy of concepts

Precept models business entities. Within that:

**Data and rules are the primary concern.** The entity's field values are what the engine protects. Every constraint expression in the runtime is a data expression — rules, guards, and ensures all evaluate field values and event arguments. The engine's job is to ensure that no invalid combination of field values can persist. Rules are the broadest enforcement surface — they hold regardless of lifecycle position and regardless of which operation ran. A rule can be unconditional (always checked) or guarded (checked only when a declared condition is met), but in either case, the engine enforces it structurally on every operation where it applies. Guarded rules do not weaken the guarantee; they make it precise — the rule applies exactly where the domain says it should, and the engine ensures it cannot be bypassed.

This is not validation — it is governance. Validation checks data at a moment in time, when called. Governance declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract.

**States are the structural mechanism that makes data protection lifecycle-aware — when lifecycle is present.** A state activates the constraint set appropriate to that position, authorizes which fields can be mutated directly, and gates which transitions are available. State is not a passive label; it is an active rule-activator. An entity in `Approved` has different data requirements than the same entity in `Draft` — the state defines what must be true about the data there. But state is instrumental, not primary. It is the coordinate system; the entity's data is the substance the system governs.

**States are optional.** Some entities need full lifecycle governance — a loan application must move through discrete positions, and the rules vary by position. Others only need data integrity — a patient demographic record, a financial instrument definition, a policy configuration object. For those, a stateless precept provides the same field declarations, rules, and constraint enforcement without a state machine. This is not a secondary capability — it follows directly from Precept's core commitments. Prevention, one-file completeness, inspectability, and determinism all hold for stateless entities. The design precedent is established: Terraform uses `resource` and `data` blocks in one language; SQL uses the same DDL for tables with triggers and tables with CHECK constraints only; DDD places value objects and entities in the same bounded context.

**Workflow is one dimension of entity lifecycle — not the defining frame.** A workflow engine orchestrates work through a process. Precept ensures a business entity's data satisfies its declared rules, at every moment, through every operation. The state graph structures how the entity moves through its lifecycle; the entity's data and rules are what the product governs. The governing concept is **governed integrity** — lifecycle transitions, field constraints, editability rules, or any combination, across every entity in the business domain.

The full guarantee the engine provides is about **configurations** — the pair of (current lifecycle position, current field values), or for stateless entities, simply the current field values. Invalid configurations are structurally impossible. A valid entity is simply one where every constraint holds for its current configuration.

---

## What makes it different

- **Prevention, not detection.** Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed, not caught after the fact. The contract prevents them.
- **One file, complete rules.** Every field, rule, ensure, and transition lives in the `.precept` definition. There is no scattered logic across service layers, ORM event handlers, or validators that can be bypassed.
- **Data integrity across the lifecycle.** Rules protect field data — unconditionally or under declared guard conditions — regardless of lifecycle position. State-conditional ensures layer lifecycle-aware data constraints on top. Together, they ensure the entity's data is valid not just at creation, but through every transition, every direct edit, and every operation — with no window in between where an invalid combination can exist.
- **Full inspectability.** At any point, you can preview every possible action and its outcome without executing anything. The engine exposes the complete reasoning: conditions evaluated, branches taken, constraints applied. You see not only what would happen, but why. Nothing is hidden.
- **Compile-time structural checking.** The compiler catches structural problems — unreachable states, type mismatches, constraint contradictions, and more — before runtime. This is where the "contract" metaphor becomes literal: the compile step validates the definition's structural soundness before any instance exists.

### Positioning in the broader category

Several existing tools govern related but narrower concerns. In a single declarative file, Precept ensures an entity's data satisfies its declared rules at every moment — across lifecycle transitions, field constraints, and state-conditional logic — structurally, not by convention. It combines the data-rule enforcement of a validator, the lifecycle structure of a state machine, and the constraint expressiveness of a decision engine in a single deterministic, inspectable contract. No existing tool provides this combination.

- **Hand-rolled service-layer governance:** In most .NET applications, entity lifecycle and data rules are scattered across multiple layers: validation in an input handler, state checks in a decision point, event handlers that mutate fields and trigger side effects, and application-layer conditionals that bypass the whole system. Each layer exists because the one before it wasn't enough. Precept consolidates that governance into a single declaration that can never be bypassed — no code path outside the contract, no window where an invalid configuration exists.
- **Pure validators (FluentValidation, JSON Schema, Zod, Pydantic):** These enforce data shape and field-level rules at a moment in time. They have no lifecycle model, no inspectability, and no structural guarantee against mutation. A FluentValidation rule runs when you call it. A Precept rule holds because the runtime structurally prevents any operation from producing a result that violates it. Precept provides the same rule enforcement, but bound to the entity and enforced structurally — not called by convention.
- **Pure state machines (XState, Stateless):** These govern transitions and lifecycle position. They have no declared field model and no data integrity enforcement — an entity can pass through every valid transition and still hold corrupted field values. That is the failure mode: a `CancelledOrder` in the right state but with `Total > 0` because no rule required otherwise. Precept combines the lifecycle structure of a state machine with the data enforcement of a constraint engine — the transition that produces an invalid data configuration is rejected before it commits.
- **Rules engines (Drools, NRules, InRule):** Rules engines execute logic reactively — you present facts and the engine infers conclusions or fires matching rules. They are powerful for complex decision logic and forward chaining. But rules are written external to the entity model, and the engine cannot verify at compile time that all rule conditions are satisfiable or non-contradicting. Precept's rules are declarations bound to specific entity configurations — compile-time verified as complete and consistent, structurally enforced on every operation, not inferred from rule evaluation.
- **ORM validation (Entity Framework, ActiveRecord):** ORM validation distributes constraint logic across data annotations, migration code, model configuration, and SaveChanges interceptors. The rules exist, but they're scattered — annotations on models, hooks in the context, conditionals in the service layer. No single artifact captures the entity's complete governance, and any code path that bypasses the ORM (raw SQL, bulk operations, another service) bypasses the rules. Precept centralizes all rules in a single declarative file that is compile-time verified and enforced on every operation, independent of the persistence layer.
- **Database constraints and triggers:** SQL CHECK constraints and triggers are the original declarative constraint mechanism — and they work. But they operate at the storage layer: no lifecycle model, no inspectability, no way to ask "what would happen if I changed this field?" Database constraints are database-specific syntax, untestable in CI without a live database, and invisible to application-layer tooling. Precept enforces the same structural guarantees at the code layer — testable, inspectable, version-controlled, and portable.
- **Policy and decision engines (OPA, Cedar, DMN):** These evaluate rules against a presented data context at request-evaluation time — even when embedded in the application process. They are powerful for authorization and cross-cutting policy. Precept's rules are not external decisions made at a call boundary — they are declarations bound to the entity that make certain data configurations structurally impossible, regardless of which code path runs.
- **Enterprise record-model platforms (Salesforce, ServiceNow, Guidewire):** These platforms provide entity governance through object models, validation rules, workflow automation, and state management. They are authoritative and complete — but they are platforms. Every major enterprise platform studied models constrained entities as first-class objects with typed fields, enumerated vocabularies, cross-field validation, and conditional constraints. Precept provides the same category of entity governance as a declarative, embeddable .NET library: no platform dependency, no data residency constraint, no UI or runtime infrastructure to operate. Your entity governance lives in your code, versioned alongside your application, testable in your CI pipeline.
- **Industry data standards (FHIR, ACORD, ISO 20022):** These standards define enforced vocabulary through profiles, structure definitions, and constraint rules — and compliant systems validate against them. But the enforcement is validation-pass-based: a FHIR validator checks conformance after the fact; it does not structurally prevent an invalid resource from being constructed. A FHIR resource, an ACORD transaction, or an ISO 20022 instrument can be modelled as a precept, and the runtime enforces the vocabulary's constraints structurally rather than at a validation pass.
- **Master data management (Informatica MDM, Reltio, Semarchy):** MDM governs the organizational truth about entities *across* systems — deduplication, matching, stewardship workflows. Precept governs individual entity integrity *within* a system — field constraints, lifecycle rules, structural prevention. They are complementary: Precept enforces at the code boundary the same data quality rules that MDM checks at the integration layer.

### What Precept complements, not replaces

Precept is not a replacement for workflow orchestrators, event sourcing frameworks, or ORM persistence layers. These systems operate at different architectural layers, and Precept integrates with all of them.

- **Workflow orchestrators (MassTransit Sagas, Temporal, Camunda):** These coordinate work across services and manage distributed process state. Temporal routes work to the right step and replays deterministically; Precept ensures the entity arriving at each step is in a valid configuration. The orchestrator routes; Precept constrains. In a complete system, a MassTransit saga might orchestrate a multi-step approval process, and at each step, fire a Precept event to ensure the entity remains structurally sound.
- **Event sourcing (Marten, EventStoreDB, Axon):** Event sourcing captures every state change as an immutable event and enforces rules through command handlers before appending. Precept and event sourcing address the same goal — preventing invalid entity state — at different layers. Precept is a constraint engine that validates entity configuration; event sourcing is a storage architecture that provides auditability and replay. They compose naturally: Precept validates before a command is sourced, and the event log provides the audit trail.
- **ORM persistence (Entity Framework, Dapper):** The ORM is the persistence boundary. Precept runs before it — validating that the entity's data satisfies its declared rules before anything is saved. Precept is storage-agnostic; it governs the entity in memory, and the ORM writes the governed result.

For the external evidence base behind these positioning claims, see [Entity-First Positioning Evidence](research/philosophy/entity-first-positioning-evidence.md).

---

## Who authors a precept

The primary author of a `.precept` file is a domain expert or a business analyst — someone who owns the rules of the business entity they are defining. They may be familiar with SQL or structured data, but they are not primarily software developers. The language is designed for someone who thinks in terms of what the data *means* and what it is *allowed to become*.

A developer integrates the compiled engine into a .NET application — wiring up `Fire`, `Inspect`, and `Update` at the application boundary. The developer may also author `.precept` files, but this is not the primary case the language design optimizes for.

---

## The word itself

"Precept" means a strict rule or principle of action. The product treats business constraints as unbreakable precepts.

The word points toward rules and their application — to data, to conduct, to what is permitted. It does not specifically foreground state machines or workflow. This is correct alignment: the primary thing Precept governs is *what the entity's data is allowed to become*, and the state machine is the mechanism through which that governance is structured across a lifecycle. A precept governs behavior and data. The runtime enforces it.

---
