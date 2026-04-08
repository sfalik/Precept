# Precept philosophy

---

## What the product does

Precept is a domain integrity engine for .NET — a single declarative contract that governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible. You declare an entity's fields, constraints, lifecycle states, and transitions in one `.precept` file. The runtime compiles that declaration into an immutable engine that enforces every rule on every operation — no invalid combination of lifecycle position and field data can persist.

That works for lifecycle-driven entities (loan applications, insurance claims, contracts), for validation-heavy entities whose correctness is the primary concern (medical claims, financial instruments, regulatory filings), for reference and configuration entities that rarely change but must always be structurally sound (product catalogs, policy records), and for data-only entities that need no lifecycle at all (address records, patient demographics, counterparty identities). The unifying principle is governed integrity — the entity's data satisfies its declared rules at every moment, whether those rules involve lifecycle transitions, field constraints, or both.

If you're writing service-layer code that manually enforces business rules across entity lifecycle transitions — rules scattered across validators, event handlers, and state checks — Precept gives you one contract that replaces all of that.

In every real business domain, data and reference entities outnumber workflow entities. Insurance has Claims but also Adjusters, Coverage Types, Rate Tables, and Policy Templates. Finance has Loan Applications but also Customers, Fee Schedules, and Rate Cards. A product that governs only the workflow side covers the minority. Precept governs the entire domain — lifecycle entities and data-only entities alike — in one contract language.

A `.precept` file declares the entity's **fields** (scalar values and typed collections, each with their own mutation operations), **states** (the positions in its lifecycle, when lifecycle matters), **events** (the triggers that select which transition to execute, carrying typed arguments into the operation), **invariants** (rules that must always be true about the entity's data), and **assertions** (state- or event-scoped rules that must hold in specific contexts). Every constraint carries a mandatory reason — the engine requires not just the rule, but its rationale. The runtime compiles the file into an immutable engine — validating structural soundness at compile time — and exposes four operations:

- **CreateInstance**: start an entity in its initial state with default field values.
- **Inspect**: ask "what would happen if I fired this event?" for every event, from any state. Non-mutating. The answer is always available.
- **Fire**: execute a transition. The engine validates input arguments, selects a matching transition row via guards, executes mutations, evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds. If any fails, the transition is rejected — the invalid configuration never exists. An event can also be explicitly forbidden — a deliberate prohibition, not just an unrouted trigger.
- **Update**: edit a field directly. The same constraint enforcement applies. Which fields can change depends on where the entity is in its lifecycle — the definition declares this, and the engine enforces it.

The engine is deterministic — same definition, same data, same outcome. Nothing is hidden.

---

## The hierarchy of concepts

Precept models business entities. Within that:

**Data and rules are the primary concern.** The entity's field values are what the engine protects. Every constraint expression in the runtime is a data expression — invariants, guards, and assertions all evaluate field values and event arguments. The engine's job is to ensure that no invalid combination of field values can persist. Invariants are unconditional: they hold regardless of lifecycle position, regardless of which operation ran, always. They are the broadest enforcement surface.

This is not validation — it is governance. Validation checks data at a moment in time, when called. Governance declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract.

**States are the structural mechanism that makes data protection lifecycle-aware — when lifecycle is present.** A state activates the constraint set appropriate to that position, authorizes which fields can be mutated directly, and gates which transitions are available. State is not a passive label; it is an active rule-activator. An entity in `Approved` has different data requirements than the same entity in `Draft` — the state defines what must be true about the data there. But state is instrumental, not primary. It is the coordinate system; the entity's data is the substance the system governs.

**States are optional.** Some entities need full lifecycle governance — a loan application must move through discrete positions, and the rules vary by position. Others only need data integrity — a patient demographic record, a financial instrument definition, a policy configuration object. For those, a stateless precept provides the same field declarations, invariants, and constraint enforcement without a state machine. This is not a secondary capability — it follows directly from Precept's core commitments. Prevention, one-file completeness, inspectability, and determinism all hold for stateless entities. The design precedent is established: Terraform uses `resource` and `data` blocks in one language; SQL uses the same DDL for tables with triggers and tables with CHECK constraints only; DDD places value objects and entities in the same bounded context.

**Workflow is one dimension of entity lifecycle — not the defining frame.** A workflow engine orchestrates work through a process. Precept ensures a business entity's data satisfies its declared rules, at every moment, through every operation. The state graph structures how the entity moves through its lifecycle; the entity's data and rules are what the product governs. The governing concept is **governed integrity** — lifecycle transitions, field constraints, editability rules, or any combination. The test for whether an entity belongs in Precept: does it need governed integrity?

The full guarantee the engine provides is about **configurations** — the pair of (current lifecycle position, current field values), or for stateless entities, simply the current field values. Invalid configurations are structurally impossible. A valid entity is simply one where every constraint holds for its current configuration.

---

## What makes it different

- **Prevention, not detection.** Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed, not caught after the fact. The contract prevents them.
- **One file, complete rules.** Every field, invariant, assertion, and transition lives in the `.precept` definition. There is no scattered logic across service layers, ORM event handlers, or validators that can be bypassed.
- **Data integrity across the lifecycle.** Invariants protect field data unconditionally. State-conditional assertions layer lifecycle-aware data rules on top. Together, they ensure the entity's data is valid not just at creation, but through every transition, every direct edit, and every operation — with no window in between where an invalid combination can exist.
- **Full inspectability.** At any point, you can preview every possible action and its outcome without executing anything. Nothing is hidden.
- **Compile-time structural checking.** The compiler catches structural problems — unreachable states, type mismatches, constraint contradictions, and more — before runtime. This is where the "contract" metaphor becomes literal: the compile step validates the definition's structural soundness before any instance exists.

### Positioning in the broader category

Several existing tools govern related but narrower concerns. Precept occupies a distinct position — it combines the data-rule enforcement of a validator, the lifecycle structure of a state machine, and the policy expressiveness of a decision engine in a single declarative file that is deterministic, inspectable, and embeddable. No adjacent tool category provides all three together.

- **Pure validators (FluentValidation, JSON Schema, Zod, Pydantic):** These enforce data shape and field-level rules at a moment in time. They have no lifecycle model, no inspectability, and no structural guarantee against mutation. A FluentValidation rule runs when you call it. A Precept invariant holds because the runtime structurally prevents any operation from producing a result that violates it. Precept provides the same rule enforcement, but bound to the entity and enforced structurally — not called by convention.
- **Pure state machines (XState, Stateless):** These govern transitions and lifecycle position. They have no declared field model and no data integrity enforcement — an entity can pass through every valid transition and still hold corrupted field values. That is the failure mode: a `CancelledOrder` in the right state but with `Total > 0` because the cancellation code forgot to zero it. Precept combines the lifecycle structure of a state machine with the data enforcement of a constraint engine — the transition that produces an invalid data configuration is rejected before it commits.
- **Policy and decision engines (OPA, Cedar, DMN):** These evaluate rules against a presented data context, often at request-evaluation time and external to the entity. They are powerful for authorization and cross-cutting policy. Precept's rules are not external decisions made at a call boundary — they are declarations bound to the entity that make certain data configurations structurally impossible, regardless of which code path runs.
- **Enterprise record-model platforms (Salesforce, ServiceNow, Guidewire):** These platforms provide entity governance through object models, validation rules, workflow automation, and state management. They are authoritative and complete — but they are platforms. Every major enterprise platform studied models constrained entities as first-class objects with typed fields, enumerated vocabularies, cross-field validation, and conditional constraints. Precept provides the same category of entity governance as a declarative, embeddable .NET library: no platform dependency, no data residency constraint, no UI or runtime infrastructure to operate. Your entity governance lives in your code, versioned alongside your application, testable in your CI pipeline.
- **Industry data standards (FHIR, ACORD, ISO 20022):** As a vertical application, Precept can express domain vocabularies directly. These standards define the structural rules that governed entities in specific domains must satisfy — but they define vocabulary, not enforcement. A FHIR resource, an ACORD transaction, or an ISO 20022 instrument can be modelled as a precept, and the runtime enforces the vocabulary's constraints at the code layer rather than at a validation pass.
- **Master data management (Informatica MDM, Reltio, Semarchy):** MDM governs the organizational truth about entities *across* systems — deduplication, matching, stewardship workflows. Precept governs individual entity integrity *within* a system — field constraints, lifecycle rules, structural prevention. They are complementary: Precept enforces at the code boundary the same data quality rules that MDM checks at the integration layer.

### What Precept complements, not replaces

Precept is not a replacement for workflow orchestrators like MassTransit Sagas or Temporal. Those systems coordinate work across services and manage distributed process state. Precept ensures each entity arrives at each step in a valid configuration. The orchestrator routes; Precept constrains. In a complete system, a MassTransit saga might orchestrate a multi-step approval process, and at each step, fire a Precept event to ensure the entity remains structurally sound.

For the external evidence base behind these positioning claims, see [Entity-First Positioning Evidence](research/philosophy/entity-first-positioning-evidence.md).

---

## The word itself

"Precept" means a strict rule or principle of action. The product treats business constraints as unbreakable precepts.

The word points toward rules and their application — to data, to conduct, to what is permitted. It does not specifically foreground state machines or workflow. This is correct alignment: the primary thing Precept governs is *what the entity's data is allowed to become*, and the state machine is the mechanism through which that governance is structured across a lifecycle. A precept governs behavior and data. The runtime enforces it.

---
