# Precept philosophy

---

## What the product does

Precept is a .NET library for modelling business entities — entities whose data must satisfy declared rules throughout their entire existence. That includes lifecycle-driven entities (loan applications, orders, subscriptions, insurance claims, contracts, project approvals), validation-heavy entities whose correctness is the primary concern (medical claims, financial instruments, regulatory filings), reference and configuration entities that rarely change but must always be structurally sound (product definitions, policy records, product catalogs), and data-only entities that require no lifecycle states at all (address records, patient demographics, counterparty identities). The unifying characteristic is not that the entity transitions between states — it is that the entity's data must conform to declared rules, period.

You write a `.precept` file that declares the entity's **fields** (the data it holds), **states** (the positions in its lifecycle, when lifecycle matters), **events** (the operations that can change it), **invariants** (rules that must always be true about its data), and **assertions** (state- or event-scoped rules that must hold in specific contexts). The runtime compiles that file into an immutable engine with four operations:

- **CreateInstance**: start an entity in its initial state with default field values.
- **Inspect**: ask "what would happen if I fired this event?" for every event, from any state. Non-mutating. The answer is always available.
- **Fire**: execute a transition. Guards are evaluated, mutations are applied, and every declared constraint is checked against the resulting configuration. If any constraint fails, the transition is rejected — the invalid configuration never exists.
- **Update**: edit a field directly. The same constraint enforcement applies. Fields are only editable when the entity's current lifecycle position authorizes it.

The engine is deterministic. Same definition + same data = same outcome, always. There is no hidden state.

---

## The hierarchy of concepts

Precept models business entities. Within that:

**Data and rules are the primary concern.** The entity's field values are what the engine protects. Every constraint expression in the runtime — invariants, `when` conditions, event assertions, state-conditional assertions — is a data expression. The engine's job is to ensure that no invalid combination of field values can persist. Invariants are unconditional: they hold regardless of lifecycle position, regardless of which operation ran, always. They are the broadest enforcement surface.

**States are the structural mechanism that makes data protection lifecycle-aware — when lifecycle is present.** A state activates the constraint set appropriate to that position, authorizes which fields can be mutated directly, and gates which transitions are available. State is not a passive label; it is an active rule-activator. An entity in `Approved` has different data requirements than the same entity in `Draft` — the state defines what must be true about the data there. But state is instrumental, not primary. It is the coordinate system; the entity's data is the substance the system governs.

**States are optional.** Some entities need full lifecycle governance — a loan application must move through discrete positions, and the rules vary by position. Others only need data integrity — a patient demographic record, a financial instrument definition, a policy configuration object. For those, a stateless precept provides the same field declarations, invariants, and constraint enforcement without a state machine. If Issue #22 lands, this becomes a first-class capability: entities can be governed by Precept purely at the data layer, with no lifecycle modelled at all.

**Workflow is one dimension of entity lifecycle — not the defining frame.** Precept evolved from a workflow engine, but it is not one now. A workflow engine's primary job is orchestrating work through a process. Precept's primary job is ensuring a business entity's data satisfies its declared rules, at every moment, through every operation. The state graph structures how the entity moves through its lifecycle; it is not the thing the product is *about*.

The full guarantee the engine provides is about **configurations** — the pair of (current lifecycle position, current field values), or for stateless entities, simply the current field values. A valid entity is one where every constraint holds for its current configuration. Invalid configurations are structurally impossible.

---

## What makes it different

- **Prevention, not detection.** Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed, not caught after the fact. The contract prevents them.
- **One file, complete rules.** Every field, invariant, assertion, and transition lives in the `.precept` definition. There is no scattered logic across service layers, ORM event handlers, or validators that can be bypassed.
- **Data integrity across the lifecycle.** Invariants protect field data unconditionally. State-conditional assertions layer lifecycle-aware data rules on top. Together, they ensure the entity's data is valid not just at creation, but through every transition, every direct edit, and every operation — with no window in between where an invalid combination can exist.
- **Full inspectability.** At any point, you can preview every possible action and its outcome without executing anything. Nothing is hidden.
- **Compile-time structural checking.** The compiler catches unreachable states, dead-end states, type mismatches, null-safety violations, and structural contradictions before runtime.

### Positioning in the broader category

Several existing tools govern related but narrower concerns. Precept occupies a distinct position relative to each:

- **Pure validators (FluentValidation, JSON Schema):** These enforce data shape and field-level rules at a moment in time. They have no lifecycle model, no inspectability, and no structural guarantee against mutation. Precept provides the same rule enforcement, but bound to the entity and enforced structurally — not called by convention.
- **Pure state machines (XState, Stateless):** These govern transitions and lifecycle position. They have no declared field model and no data integrity enforcement — an entity can pass through every valid transition and still hold corrupted field values. Precept combines the lifecycle structure of a state machine with the data enforcement of a constraint engine.
- **Policy and decision engines (OPA, Cedar, DMN):** These evaluate rules against a presented data context, often at request-evaluation time and external to the entity. They are powerful for authorization and cross-cutting policy. Precept's rules are not external decisions made at a call boundary — they are declarations bound to the entity that make certain data configurations structurally impossible, regardless of which code path runs.
- **Enterprise record models (Salesforce, ServiceNow, Guidewire):** These platforms provide entity governance through object models, validation rules, workflow automation, and state management. They are authoritative and complete — but they are platforms. Precept provides the same category of entity governance as a declarative, embeddable .NET library: no platform dependency, no data residency constraint, no UI or runtime infrastructure to operate.
- **Industry data standards (FHIR, ACORD, ISO 20022):** These vocabularies describe the structural rules that governed entities in specific domains must satisfy. Precept can express those structural rules directly — a FHIR resource, an ACORD transaction, or an ISO 20022 instrument can be modelled as a precept, and the runtime enforces the vocabulary's constraints at the code layer rather than at a validation pass.

---

## The word itself

"Precept" means a strict rule or principle of action. The product treats business constraints as unbreakable precepts.

The word points toward rules and their application — to data, to conduct, to what is permitted. It does not specifically foreground state machines or workflow. This is correct alignment: the primary thing Precept governs is *what the entity's data is allowed to become*, and the state machine is the mechanism through which that governance is structured across a lifecycle. A precept governs behavior and data. The runtime enforces it.

---
