# Precept philosophy

*This document states the guarantee Precept ships with — the commitments the project is built to keep. It is not a status report on the current build: where the compiler or runtime has not yet caught up to a commitment here, the commitment stands and the gap is tracked as work in progress in the compiler and runtime docs.*

---

## What Precept is

Precept governs what a business entity's data is allowed to become.

A business entity — a loan application, an insurance claim, a rate table, a patient record — carries rules: values it must never hold, combinations it must never enter, positions in its lifecycle where different things must be true. In most systems those rules are scattered across validators, event handlers, state checks, and service-layer conditionals, and every one of those can be bypassed. Precept replaces all of it with one declaration — every rule declared once, holding always. You state the entity's fields, constraints, lifecycle, and rules in a single `.precept` file, and the compiler proves, before any entity exists, that no operation can ever violate what you declared. What cannot be proven does not compile. Invalid configurations are not caught and rejected — they are structurally impossible, because nothing that could produce one exists.

That holds whether or not the entity has a lifecycle. A loan application moves through states; a rate table never does; both carry rules, and Precept governs both in one language, under one principle: the entity's data satisfies its declared rules at every moment.

A `.precept` file declares the entity's **fields** — scalar values and typed collections — its **states**, when lifecycle matters, the **events** that move it, the **rules** that must hold about its data, unconditionally or under a declared condition, and the **ensures** that must hold in specific states or on specific events. Every rule carries a mandatory reason: the language will not accept a constraint without its rationale. One file, complete rules — there is no second place to look, and no scattered logic to bypass.

Only a closed set of operations can ever change a governed entity's data. It is created in its initial state, from the values construction supplies and the defaults its definition declares. An event moves it, carrying typed arguments. A field its definition declares editable is edited. Every way the data can change is declared in the definition, and every value that enters from outside is held to its declared constraints at the moment it enters.

Any of this can be previewed — every possible event, from any version of the entity you hold, live or restored — without executing anything. And an entity comes back from storage by restore, which changes nothing: it returns a configuration that was already committed.

The set is closed because the guarantee requires it: an operation exists in the compiled engine only because the compiler proved every rule survives it, and an operation nobody declared has no such proof and no existence.

The engine is deterministic: same definition, same data, same outcome, every time, anywhere. That is not a performance property — it is what makes the contract checkable, and it forbids things. No randomness. No evaluation that reaches outside the entity's own data — no lookup against another system, another entity, or the host's state in the middle of a rule. The one environmental value the language admits is the clock, and it is admitted honestly, not hidden: `now()` is the sole construct that reads the environment, an author who writes it into a rule is declaring in the open that the rule's correctness depends on when it is evaluated, and within a single operation the clock is one consistent value — same inputs, same outputs, always. Everything else the entity's rules can see entered through a declared operation, where the guarantee saw it arrive.

The engine is inspectable: at any point you can preview every possible operation and see not only what would happen but why, with the complete reasoning exposed. And it is honest about approximation. A contract is only trustworthy if it means what it appears to mean; silent approximation inside an exact-looking path destroys the ability to reason about outcomes. Where a value or operation is inherently approximate, the contract must say so — visibly, in the type system and the public API. Exact stays exact; approximation is declared, never disguised.

The engine embeds in a .NET application as an ordinary library. Where the entity is stored and how it is presented remain the application's business; what its data is allowed to become is Precept's.

---

## Prevention, not detection

Every adjacent tool at the code layer detects: it checks data at a boundary, when called, and reports what it finds. (Database constraints genuinely prevent — at the storage layer, with the costs described below.) Precept prevents where the business logic runs. Business logic cannot produce a result that violates a declared rule; a business process cannot contain an unreachable step, a position with no way out, or a route around a mandatory stage. No approval that contradicts policy, no claim paid against lapsed coverage, no loan funded without ever passing underwriting. The rule-owner's pain has never been the absence of a rule — it is the gap between declaring a rule and having it hold on every path. Precept closes that gap structurally.

Two guarantees close it — one about the entity's data, one about the process it moves through.

The data guarantee: every declared rule holds on every operation, unconditionally or under exactly the conditions the domain declares. A rule that says "the approved amount cannot exceed the coverage limit while the policy is in force" does not hold because something checks it at the right moments; it holds because there is no moment at which it could fail. An operation exists in the compiled engine only because the compiler proved, before any entity existed, that every rule survives it — and the values the operation carries in were held to their declared constraints on the way in. Nothing re-evaluates the rules after the change, and nothing needs to: the invalid configuration is not produced and then rejected. It is never produced.

The process guarantee: the lifecycle itself is proven sound. Every declared state is reachable — no position that exists on paper but that no sequence of operations can arrive at. Every non-terminal state has a way forward — no dead end an entity enters and cannot leave. Required states lie on every path to completion — no entity skips a mandatory step. These are business-process bugs — the claim that can never reach Approved, the loan that enters a state with no exit — and the compiler makes them impossible at definition time, before a single entity exists.

The same absoluteness applies to every calculation in the definition. The compiler does not trust that an expression will succeed — it proves it will. Division by zero, arithmetic overflow, empty collection access: not risks managed at runtime, but compile-time impossibilities. Where a calculation depends on a value supplied at runtime, the compiler proves it safe from something the author declared — a constraint on the field, a constraint on the event argument, the guard on the transition, or a rule that already held before the operation ran. Where none of those closes the proof, the definition does not compile, and the rejection names the premise that would close it. The constraints enforced on entering values are what make those premises true — not a second line of defense standing behind the proof.

The runtime does not take the compiler's word for any of this. The proof travels with the definition, and before an engine will govern under it, it verifies that proof for itself; one whose proof does not verify governs nothing. The confidence rests on verification the engine performed, not on where the definition came from.

A definition that compiles without diagnostics therefore has none of these defects: no unreachable state, no dead end, no type mismatch, no contradictory constraints, no unsatisfiable guard, no division by zero, no overflow, no empty-collection access, no rule that any operation can break. This is where the "contract" metaphor becomes literal — and it is a different category from every tool that enforces rules when called, at boundaries, under conditions that can be bypassed. The difference is not which bugs get caught. It is which bugs can exist.

---

## The hierarchy of concepts

**Data and rules are primary.** The entity's field values are what the engine protects. Every constraint expression in the language is a data expression — rules, guards, and ensures all evaluate field values and event arguments. Rules reach furthest: they hold regardless of lifecycle position and regardless of which operation ran. A rule can be unconditional — holding of every configuration the entity can be in — or guarded, holding whenever a declared condition is met. A guarded rule holds exactly where the domain says it should, and it cannot be bypassed there.

This is governance, not validation. Validation checks data at a moment in time, when called. Governance declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no path around it.

**States are the mechanism that makes governance lifecycle-aware.** A state activates the constraints that apply at that position, authorizes which fields may be edited there, and gates which transitions are available. An entity in `Approved` has different data requirements than the same entity in `Draft`. But state is instrumental, not primary: it is the coordinate system, and the data is the substance. A workflow engine orchestrates work through a process; Precept governs what the entity's data is allowed to be at every moment of one.

**States are optional.** An entity with no lifecycle is declared without states — the same fields, rules, and enforcement, no state machine — and everything in this document holds for it unchanged.

**The unit of the guarantee is the configuration** — the pair of lifecycle position and field values, or for a stateless entity, the field values alone. Invalid configurations are structurally impossible. A valid entity is one whose every constraint holds for its current configuration.

---

## Where Precept stands

Several existing tools govern related but narrower concerns. Precept combines the data-rule enforcement of a validator, the lifecycle structure of a state machine, and the constraint expressiveness of a decision engine in a single deterministic, inspectable contract — enforced structurally, not by convention. No existing tool provides that combination.

- **Hand-rolled service-layer governance:** In most applications, an entity's rules are scattered across layers — validation in an input handler, state checks in a decision point, event handlers that mutate fields, application-layer conditionals that bypass the whole stack. Each layer exists because the one before it wasn't enough. Precept replaces the stack with a single declaration that cannot be bypassed: no code path outside the contract, no window where an invalid configuration exists.
- **Pure validators (FluentValidation, JSON Schema, Zod, Pydantic):** These enforce data shape and field-level rules at a moment in time. They have no lifecycle model, no inspectability, and no guarantee between calls. A FluentValidation rule runs when you call it. A Precept rule holds because no operation the engine can run is able to produce a result that violates it — the compiler proved that of every one of them before the engine existed.
- **Pure state machines (XState, Stateless):** These govern transitions and lifecycle position, with no declared field model and no data enforcement — an entity can pass through every valid transition and still hold corrupted values. That is the failure mode: a `CancelledOrder` in the right state but with `Total > 0`, because no rule required otherwise. Precept joins the lifecycle structure of a state machine to the data enforcement of a constraint engine — a transition that could leave the entity's data invalid is either refused by its guard when it is attempted, or refused by the compiler before the definition ever becomes an engine.
- **Rules engines (Drools, NRules, InRule):** These execute logic reactively — you present facts, the engine infers conclusions and fires matching rules. Powerful for complex decision logic; but the rules live outside the entity model, and nothing verifies at compile time that they are satisfiable or mutually consistent. Precept's rules are bound to the entity, verified complete and consistent at compile time, and enforced on every operation — never inferred at evaluation time.
- **ORM validation (Entity Framework, ActiveRecord):** ORM validation scatters constraint logic across data annotations, migration code, model configuration, and save-time interceptors — and any code path that bypasses the ORM (raw SQL, bulk operations, another service) bypasses the rules. Precept holds all rules in one declarative file, verified at compile time and enforced on every operation, independent of the persistence layer.
- **Database constraints and triggers:** SQL CHECK constraints and triggers are the original declarative constraint mechanism, and they work — but at the storage layer: no lifecycle model, no inspectability, no way to ask "what would happen if I changed this field?" They are database-specific, untestable in CI without a live database, and invisible to application tooling. Precept provides the same structural enforcement at the code layer — testable, inspectable, version-controlled, portable.
- **Policy and decision engines (OPA, Cedar, DMN):** These evaluate rules against a presented data context at the moment of a request. Powerful for authorization and cross-cutting policy. Precept's rules are not decisions made at a call boundary — they are declarations bound to the entity that make certain configurations impossible regardless of which code path runs.
- **Enterprise record-model platforms (Salesforce, ServiceNow, Guidewire):** These provide genuine entity governance — typed fields, enumerated vocabularies, cross-field validation, conditional constraints — but as platforms. Precept provides the same category of governance as an embeddable .NET library: no platform dependency, no data-residency constraint, no infrastructure to operate. The governance lives in your code, versioned with your application, tested in your CI pipeline.
- **Industry data standards (FHIR, ACORD, ISO 20022):** These define enforced vocabulary, checked by validators after the fact — conformance is a validation pass, not a structural property. A FHIR resource, an ACORD transaction, or an ISO 20022 instrument modelled as a precept has the vocabulary's constraints enforced structurally: the non-conforming instance cannot be constructed.
- **Master data management (Informatica MDM, Reltio, Semarchy):** MDM governs the organizational truth about entities *across* systems — deduplication, matching, stewardship. Precept governs individual entity integrity *within* a system. They are complementary: Precept enforces at the code boundary the same data-quality rules MDM checks at the integration layer.

### What Precept complements, not replaces

Precept does not replace workflow orchestrators, event sourcing, or persistence layers. They operate at different architectural layers, and Precept composes with all of them.

- **Workflow orchestrators (MassTransit Sagas, Temporal, Camunda):** These coordinate work across services and manage distributed process state. The orchestrator routes; Precept constrains. A saga moves an approval through its steps, and the entity arriving at each step is in a valid configuration because Precept governed every change to it.
- **Event sourcing (Marten, EventStoreDB, Axon):** A storage architecture — auditability and replay. Precept governs the entity before a command is sourced; the event log records what the governed entity did.
- **ORM persistence (Entity Framework, Dapper):** The ORM is the persistence boundary. Precept governs the entity in memory; the ORM writes the governed result. Precept is storage-agnostic.

For the external evidence behind these positioning claims, see [Entity-First Positioning Evidence](../research/philosophy/entity-first-positioning-evidence.md).

---

## Who authors a precept

The primary author of a `.precept` file is the person who owns the rules — a domain expert or business analyst, someone who thinks in terms of what the data means and what it is allowed to become. They may know SQL or structured data; they are not primarily software developers, and the language does not ask them to be.

What makes that author plausible is the compiler's answer. Every defect this document names as impossible is one the author learns about while writing, in the moment it is introduced — and the refusal teaches. A rejection names the missing premise: cannot prove this rule survives this event; this guard would close the proof. The author learns how their own rules compose without ever learning to program.

The same answer reaches an AI agent. The compiler speaks to editors through a language server and to agents through an MCP server, and an agent authoring a precept meets the same refusal, with the same explanation, that a person does. Neither can produce a definition the compiler has not proven, because it is the same refusal — the guarantee does not weaken when an agent writes the rules.

A developer embeds the compiled engine in a .NET application and connects it at the application boundary. Developers can author precepts too, but the language optimizes for the rule-owner.

---

## What Precept refuses to do

Precept has no escape hatch. No way to suspend a rule in production, no annotation that says trust me, no construct that compiles with its obligations unproven, no warning where an error belongs, no approximation hidden inside a path that looks exact. A definition the compiler cannot prove is refused.

Precept's guarantee has one precondition, and it is worth stating plainly: data must not be tampered with between the moment it is persisted and the moment it is restored. Every value in a committed configuration entered through a proven operation, which is why restoring it needs no re-checking. Change it behind the engine's back and it is no longer the configuration that was proven — nothing the compiler established about it still applies. That is a condition on how the data is kept, not a gap in what the engine promises: the engine's own API offers the declared operations and nothing else, with no privileged write path and no back door for the caller that promises to be careful.

The refusal has a cost, and the author pays it: some things that can be written in another tool cannot be written here at all, and the language admits less than its grammar suggests. That is expressiveness surrendered on purpose — wherever keeping a construct would mean a rule the compiler could not prove, the construct goes and the rule stays. A rule that holds on the paths somebody tested is a hope. A rule that cannot be violated is a guarantee. A smaller language in which every rule is a guarantee is worth more than a larger one in which every rule is a hope.

---
