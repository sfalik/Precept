# Data vs. State: Should Precept's Philosophy Reframe?

**Research question:** Should Precept's brand philosophy shift to treat *data* as the primary concern — with states as vehicles that drive data through a workflow — rather than foregrounding state machines and invalid-state prevention?

Requested by Shane. Authored by J. Peterman, Brand/DevRel.

---

## 1. What the current philosophy says, and why it was framed that way

### The current frame

The locked positioning statement (from `brand-decisions.md`):

> "Precept is a domain integrity engine for .NET — a single declarative contract that binds **state**, data, and business rules into an engine where **invalid states** are structurally impossible."

The README opening:

> "By treating business constraints as unbreakable precepts, it binds **state machines**, validation, and business rules into a single executable contract where **invalid states** are structurally impossible."

State appears twice in the headline claim. The flagship differentiator is framed as "invalid states are structurally impossible." The entity is understood to *move through states*. Data is present but secondary.

### Why it was framed this way

The state-first framing came from a legitimate positioning logic: the problem Precept was solving most visibly was the scattered-validation problem, and the most distinctive mechanism was the state-machine-backed constraint engine. At the time the positioning was set:

1. "State machine" is a known CS concept — using it as an anchor reduced explanation burden.
2. "Invalid states are structurally impossible" is a strong, specific, falsifiable claim — easy to prove with a code sample.
3. Competing tools (Stateless, XState, Workflow Core) positioned on state machines, so Precept was differentiating *within* a known category by adding enforcement.
4. "Domain integrity engine" was coined to avoid being reduced to just a state machine library, and it worked — the phrase is unique.

The state-first framing is technically accurate. It is not wrong. But Shane's challenge raises the right question: is it the *most resonant* entry point, or is it an inside-out view of the problem?

---

## 2. External precedent: how comparable tools handle this

### Event-sourced / CQRS tooling (Axon, EventStoreDB)

These tools make the strongest case for data-first thinking. In event sourcing, **state is a derived artifact** — the append-only event stream (data) is the source of truth, and current state is computed from it. Axon markets itself as a framework for "event-driven microservices." EventStoreDB says "the database designed for Event Sourcing." Neither leads with state. The data (events) is the primary citizen; state is a projection.

This is philosophically aligned with Shane's framing: *states are the read layer; data (events, mutations) is what actually happened.*

**Relevance for Precept:** Precept's runtime stores `state + data` as a snapshot. When you `Fire` an event, the primary output is `UpdatedInstance` — mutated field values plus a new state. The data mutation is the substance; the state transition is the label you put on the new configuration. EventStoreDB's framing suggests there's strong intellectual precedent for this view.

### Workflow engines (Elsa, Workflow Core)

Workflow Core's most resonant line: *"Think: long running processes with multiple tasks that need to track state."* Note how it lands: the *process* is the hero, and tracking state is a feature of the process, not the defining characteristic. State serves the workflow; the workflow serves the data/outcome.

Elsa's positioning is more platform-like ("powerful workflow library that enables workflow execution"), but even there, the *execution* is foregrounded — the movement of work through a pipeline. State checkpoints are infrastructure for that movement.

**Relevance for Precept:** Neither of these tools says "invalid states are impossible." They don't lead with state correctness because their users aren't primarily thinking about state validity — they're thinking about work completion and data outcomes. The state-first framing may be narrowing Precept's appeal to a subset of the actual use case.

### Stateful actor frameworks (Akka, Orleans)

Akka doesn't market "manage your actor state" — it markets "build reactive systems." Orleans says "a straightforward approach to building distributed, high-scale applications." State is implicit; the external frame is the application capability enabled by the framework.

**Relevance for Precept:** The lesson: if your mechanism is state-based but your user's goal is something else (reliable data, correct business outcomes), lead with the goal. The mechanism can be in the second sentence.

### Entity Framework / ORMs

Purely data-first. EF's opening: "Entity Framework Core is a modern object-database mapper for .NET." No state, no workflow. The entire frame is: your data, mapped correctly, accessible through code. The ORM category owns "data integrity" in the sense of schema fidelity — which is precisely the void Precept fills at the *business rule* level.

**Relevance for Precept:** This is the most telling comparison. When a developer hears "domain integrity engine," they may instinctively reach for ORM-category thinking: *data model, schema, correctness.* Precept could lean into this — it's an ORM-level guarantee, but for business rules and lifecycle transitions, not just schema. "Your entity data follows the rules" is immediately comprehensible in ORM terms.

### Domain-driven design tooling

DDD's canonical language revolves around *aggregates*, *entities*, and *domain events*. An aggregate's job is to protect *invariants* — rules about the data. State (via state machines) is one *mechanism* for protecting invariants, but DDD doesn't define aggregates as primarily state-centric. Eric Evans and Vaughn Vernon talk about invariants protecting *data consistency*, not state correctness per se.

**Relevance for Precept:** In DDD vocabulary, Precept is an aggregate-enforcement engine. The business aggregate (LoanApplication, Subscription, Order) has data that must satisfy invariants throughout its lifecycle. State is the lifecycle structure; data integrity is the goal. This is Shane's framing, articulated through DDD precedent.

### The strongest flagship claim precedents

Reviewing the adjacent space:

| Tool | Flagship claim | Mechanism foregrounded | Outcome foregrounded |
|------|---------------|----------------------|---------------------|
| Temporal | "Durable execution" | Execution engine | Durability (outcome) |
| Serilog | "Diagnostic logging for .NET" | Library | Diagnostics (outcome) |
| FluentValidation | "Strongly-typed validation rules" | Fluent interface | Type-safe rules (outcome) |
| Polly | "Resilience strategies: Retry, Circuit Breaker…" | Strategies | Resilience (outcome) |
| EventStoreDB | "Database designed for event sourcing" | Database design | Event sourcing (outcome) |

The pattern: the strongest claims **name the outcome first, mechanism second** (or not at all). Temporal doesn't say "a state machine that persists." It says "durable execution." The mechanism (activity-based state machine with journaling) is invisible in the tagline.

By contrast, "where **invalid states** are structurally impossible" is mechanism-forward. It tells you *how* Precept works (via states), not *what you get* (data that is always valid, rules that always hold).

---

## 3. Recommendation

**Adopt a unified framing with data as the primary outcome and states as the organizing mechanism.** This is not a pivot away from states — it is a reframe of what states are *for*.

The current philosophy has it exactly backward in one specific sentence. The flagship claim "invalid states are structurally impossible" is technically true but *undersells* the outcome. The actual guarantee that developers care about is:

**Your entity data cannot enter an invalid configuration — structurally, not by convention.**

Invalid *data* is the pain. Invalid *state* is one way data becomes invalid (being in the wrong state for the operations you perform). But "invalid data" is a more universal, more immediately felt problem than "invalid state."

**This recommendation is not a hedge.** The current state-first framing is a liability — it filters in the 5% of developers who think explicitly in state machines and filters out the 95% who just want their entity data to be valid. Precept's mechanism is state-machine-based, but its *value* is data integrity. The brand should speak to the value.

This is supported by:
1. External precedent — Temporal, Polly, and EventStoreDB all lead with outcome, not mechanism.
2. DDD vocabulary — aggregates protect *invariants* (data rules), not *state positions*.
3. The name itself — "Precept" means a rule of action or conduct. Rules govern *behavior and data*. They don't specifically govern state machines.
4. The product's own mechanics — when you Fire an event, the primary artifact is mutated field data. State is a label on the new configuration.

---

## 4. Candidate framings for the flagship claim

### Candidate A — Data-outcome primary

> "Precept is a domain integrity engine for .NET — a single declarative contract where **your entity data follows the rules, always**, and invalid configurations are structurally impossible."

What changes: "invalid states" → "invalid configurations." The word "configurations" encompasses both state and data — it says: the *combination* of state + field values cannot be wrong. This is actually more precise than "invalid states," which technically only refers to the state label.

### Candidate B — Lifecycle-and-data unified

> "Precept binds an entity's lifecycle and data into one contract. **What your data can become is defined by where it is.** Invalid configurations are structurally impossible."

What changes: Introduces the word "lifecycle" (neutral — not state-machine-specific) and makes explicit the relationship: the data's valid range depends on its position in the workflow. This is Shane's exact framing in philosophical form.

**Of the two, Candidate A is stronger for the single-sentence positioning.** It preserves "domain integrity engine" (the category we own), adds "your entity data follows the rules" (the outcome-first statement), and retains "structurally impossible" (the mechanism-level guarantee). It is one fluid sentence, not two.

Candidate B is stronger as a *narrative* sentence — inside a philosophy document, an about page, or a talk introduction, "what your data can become is defined by where it is" is memorable and philosophically precise. Use it in copy, not in the tagline.

---

## 5. What would need to change

### In `philosophy.md`

The icon philosophy document's description of the product currently reads:

> "Precept is a .NET library. You write a .precept file that declares an entity's states, fields, events, guards, and constraints in a flat, keyword-anchored DSL."

This lists `states` before `fields`. A data-first reframe would reorder:

> "You declare the entity's **fields** (data) first, then the **states** that organize its lifecycle, the **events** that drive transitions, and the **rules** that keep data valid at every step."

The "what makes it different" section currently leads with:

> "Prevention, not detection. Invalid **states** don't get caught after the fact — they are structurally impossible."

Revise to:

> "Prevention, not detection. Invalid data and unauthorized transitions don't get caught after the fact — they are structurally impossible. The contract prevents them."

The word "states" only needs to appear in the four-operation description (CreateInstance, Inspect, Fire, Update) — those are mechanical. In the philosophy and differentiation sections, "configurations," "data," and "rules" should carry the frame.

### In `brand-decisions.md`

**The locked positioning statement needs one surgical revision:**

Current:
> "Precept is a domain integrity engine for .NET — a single declarative contract that binds **state**, data, and business rules into an engine where **invalid states** are structurally impossible."

Revised (Candidate A):
> "Precept is a domain integrity engine for .NET — a single declarative contract that governs how entity **data** evolves through its lifecycle, making invalid configurations structurally impossible."

The phrase "binds state, data, and business rules" was accurate but listed the mechanism ("state") before the asset ("data"). The revision makes clear that the governed *thing* is data, and the *lifecycle* is the organizing frame.

**The README opening** (in `brand-decisions.md`'s narrative arc section) would update the supporting sentence:

Current:
> "By treating business constraints as unbreakable precepts, it binds **state machines**, validation, and business rules into a single executable contract where **invalid states** are structurally impossible."

Revised:
> "By treating business rules as unbreakable precepts, it governs how entity data evolves through its lifecycle — one contract, one file, structurally valid at every step."

### What does NOT change

- "Domain integrity engine" — keep it. It's unique and it already points toward data-quality thinking.
- The category-creation strategy — this is still the right archetype.
- The AI-native secondary frame — unaffected.
- The voice decisions — unaffected.
- The color system, typography, and visual language — unaffected.
- The four-operations description — mechanical, accurate as written.
- "One file, every rule" — this still holds and should stay in supporting copy.

---

## Appendix: The name and the framing

"Precept" means *a general rule intended to regulate behavior or thought* (Merriam-Webster). A precept regulates **conduct** — it is about what you are permitted to do and how things should be.

This word does not foreground state. It foregrounds rules and their application to behavior and data. A precept is a constraint on *action* — on what can happen to data, not on which node in a graph you occupy.

"Domain integrity" as a phrase was coined to escape the state-machine bucket. It's a good category name. But its full potential is only realized if the philosophy makes clear what *integrity* refers to: the integrity of the entity's **data** across its entire lifecycle, governed by the contract's rules.

The reframe is not a repositioning. It's finishing the thought that "domain integrity engine" started.
