# Data vs. State: Architectural Analysis

**Authored by:** Frank, Lead Architect and Language Designer  
**In response to:** Shane's framing — *"states are just vehicles to drive data through a workflow"*  
**Companion doc:** `data-vs-state-philosophy.md` (Peterman, brand perspective)  
**Status:** Architecture research — findings, not decisions

---

## Preface

Peterman's document covers the positioning angle. This document covers the architecture angle. These are different questions and the answers don't fully overlap. Positioning is about resonance; architecture is about accuracy. A framing can be resonant but architecturally misleading, or architecturally precise but dead on arrival as copy. This document settles what's *true*. The brand team decides what to do with it.

---

## 1. What State Does in the Runtime

States are not labels. They are load-bearing structures. Here is the complete list of things state does in the Precept runtime — not as positioning, as mechanics:

### 1a. Transition routing gate

The `from X on Y` row is only a candidate for execution when the instance is currently in state `X`. This is the most obvious state function: state governs which transition rows participate in evaluation when an event fires. Without state, the engine would have no way to narrow the candidate row set — it would have to evaluate every row for every event, and first-match-wins would become undefined.

```precept
from UnderReview on Approve when DocumentsVerified && CreditScore >= 680 ...
from UnderReview on Approve -> reject "..."
```

Both rows are candidates only because the instance is in `UnderReview`. The same `Approve` event fired from `Draft` would find no matching rows and be rejected automatically. State is doing structural filtering here, not just labeling.

### 1b. Edit authorization gate

`in X edit Field` controls which fields can be mutated directly via the `Update` operation. State authorizes or denies the edit — the data's mutability is governed by position in the workflow.

```precept
in Red edit VehiclesWaiting, LeftTurnQueued
in UnderReview edit FraudFlag
```

`FraudFlag` is not editable from `Draft` or `Approved`. This is not expressible purely in terms of data values — it requires knowing *where* you are. This is the clearest counterexample to "state is just a vehicle": state is the access-control boundary for data mutation.

### 1c. State-conditional data invariants

`in X assert`, `to X assert`, and `from X assert` create data constraints that are *state-scoped*. These are not global invariants — they only hold (or are checked) when the named state is relevant.

```precept
in Approved assert DocumentsVerified because "Approved loans must have verified documents"
in Funded assert ApprovedAmount > 0 because "Funded loans must have a positive approved amount"
```

`DocumentsVerified` must be `true` in `Approved` — but the same field being `false` in `Draft` is perfectly legal. The state is doing real semantic work: it defines *which data constraints apply here*. "Approved" means something. It's not a neutral waypoint; it's a semantic commitment that imposes its own data requirements.

This is the strongest architectural argument against "state as vehicle." A vehicle carries cargo passively. A vehicle doesn't say "while you're inside me, this cargo must weigh more than 10 kg." A state does exactly that.

### 1d. Compile-time structural guarantees

The state graph enables a class of compile-time checks that are structurally impossible to express in terms of data alone:

- **Unreachable states**: a declared state no transition can reach.
- **Dead-end states**: a non-terminal state with no outgoing transitions (an instance can be trapped forever).
- **Transition type-checking**: the target state of `transition X` must be a declared state name.

These are graph properties. They require a state graph to compute. No amount of `invariant` declarations on field data can replace them.

### 1e. What state cannot do

State cannot:
- Express the current values of entity fields (that is data's job)
- Drive guard evaluation (guards are data expressions; state is not readable in a guard)
- Enforce that a field value is non-negative (that is an invariant)
- Determine which of two `from UnderReview on Approve` rows fires (that is data's job, via the `when` guard)

This asymmetry is key: **state gates access; data drives decisions within those gates.**

---

## 2. What Data Does in the Runtime

### 2a. The entity's substance

Fields are the entity. They persist across every transition. When you call `Fire`, the instance that comes back has the same fields, potentially with new values. State is just one more property of that snapshot (`currentState`). If you serialized and stored an instance, you would store fields and state together — but it's the fields that represent everything meaningful about what happened to this entity over its lifetime.

### 2b. Unconditional invariants

`invariant` declarations are data-level and state-agnostic. They hold always:

```precept
invariant RequestedAmount >= 0 because "Requested amount cannot be negative"
invariant ApprovedAmount <= RequestedAmount because "Approved amount cannot exceed the request"
```

These fire on every `Fire` and every `Update`, regardless of current state. The engine doesn't ask "are we in the right state for this invariant?" It always checks them. In this sense, data constraints have broader scope than state constraints.

### 2c. Guard expressions drive routing

Guards (`when` clauses) are pure data expressions. They determine which transition row wins when multiple rows match the same `from/on` header:

```precept
from Red on Advance when LeftTurnQueued -> ... -> transition FlashingGreen
from Red on Advance when VehiclesWaiting > 0 -> ... -> transition Green
from Red on Advance -> reject "No demand detected at red"
```

The data values `LeftTurnQueued` and `VehiclesWaiting` determine the outcome. State says "we are in Red evaluating Advance." Data determines *which* Advance outcome executes. Control flow within a state is entirely data-driven.

### 2d. Event args are the input channel

Event args are typed data that flow in when an event fires. They participate in both event-level assertions (`on X assert`) and in guard expressions. The transition rows compute with them directly (`Approve.Amount`, `Submit.Applicant`). Without event args, you couldn't parameterize transitions — every event would have to be a fixed action, and the data model would have to pre-stage all inputs as fields.

### 2e. The primary output of Fire is a data mutation

When `Fire` succeeds, the caller receives `result.UpdatedInstance`. The substance of that result is the new field values. The new state is also in the result, but the caller's downstream logic typically acts on the data: "what is `ApprovedAmount` now?" "Is `DocumentsVerified` true?" State is consumed by the engine on the next call; data is consumed by the application.

---

## 3. The Relationship: Co-Equal Dimensions, Not a Hierarchy

Here is the precise architectural statement of how state and data relate in Precept:

> An instance's identity at any moment is the pair **(currentState, fieldValues)**. Neither alone is sufficient to determine what the instance can do, what constraints apply, or whether it is valid. Both are required.

"State as vehicle for data" implies a hierarchy where data is primary and state is instrumental. That hierarchy is **not present in the architecture**. The more accurate model is:

- **State is a coordinate** — it locates the instance in the workflow topology and activates the relevant constraint set for that location.
- **Data is the substance** — it is what the entity knows, and it is what guards evaluate and mutations change.
- **Validity is always joint** — an instance is valid if and only if its *combination* of state and field values satisfies all constraints: global invariants (data-only), state-conditional asserts (state-scoped data constraints), and structural state-graph properties (state-only).

A loan application in `Approved` with `DocumentsVerified = false` is invalid. The invalidity requires both state and data to express — you cannot catch it with a pure-data invariant (the rule only applies in `Approved`) and you cannot catch it with a pure-state check (the rule is about a field value). The engine enforces this because it combines both.

### Why Shane's framing has genuine validity

Shane's framing isn't wrong — it captures something real. The *value* delivered to the application is expressed in data. When you run the loan workflow, the outcome is `ApprovedAmount`, `DecisionNote`, and `DocumentsVerified` — not the state label "Approved." The state label is a *summary* of which data constraints currently apply and which operations are currently available. In that sense, yes: state structures the rules, and data is what those rules protect.

The more precise articulation of Shane's insight is:

> **States define the rules that must hold. Data is the substance those rules protect.**

That is true and architecturally defensible. "State as vehicle" slightly overstates data primacy by implying state is passive. States are active rule-activators, not passive containers.

---

## 4. Is "Invalid States Are Structurally Impossible" Accurate but Incomplete?

**Yes — accurate and incomplete.**

**What it accurately covers:**
- An instance cannot occupy a state that wasn't reached through a declared transition.
- State-conditional assertions (`in X assert`) enforce that the data meets state-specific requirements whenever you are in that state.
- The engine rejects the proposed world before committing if any constraint fires.

**What it misses:**

1. **Invariants prevent invalid data regardless of state.** "Invalid *data* is structurally impossible" is equally true and equally guaranteed by the engine. The flag `invariant ClaimAmount >= 0` never fires because the runtime won't commit a world where it fails. That's a data guarantee, not a state guarantee.

2. **Event arg assertions prevent invalid inputs.** `on Submit assert Amount > 0` means the engine refuses to begin a transition with invalid incoming data. The invalid configuration never starts, let alone finishes.

3. **Guard-based routing prevents invalid transitions given valid state + invalid data.** If you're in `UnderReview` and `DocumentsVerified = false`, the `Approve` event rejects via the row fallback. The invalid combination of (state=UnderReview, DocumentsVerified=false) producing an Approved outcome is prevented — not because of state checking, but because data-gated routing finds no valid transition and the fallback `reject` fires.

**The complete guarantee is:**

> **No entity configuration — meaning the combination of current state and current field values — can exist unless it satisfies every declared constraint: global invariants, state-conditional asserts, event-conditional asserts, and the structural state graph rules.**

The flagship claim "invalid states are structurally impossible" names one dimension of this guarantee. The full claim is about *configurations* (state × data pairs), not states alone.

**Is "invalid data cannot persist" equally defensible?**

Mostly — but slightly weaker. Global invariants absolutely prevent invalid persistent data. But state-conditional data rules (`in Approved assert`) are mixed: they're expressed on data fields, but they only apply in certain states. Saying "invalid data cannot persist" is true for invariants. For state-conditional constraints, what's actually true is "the (state, data) combination cannot exist if it violates any applicable constraint." The single-axis framing loses that joint nature.

The cleanest single claim that covers the full guarantee:

> **Invalid entity configurations are structurally impossible.**

"Configuration" = (state, fieldValues). That one word captures both dimensions without hierarchy, without mechanism bias, and without omitting either axis.

---

## 5. Would a Data-First Reframe Require DSL or Runtime Changes?

**No. Zero changes required.**

The DSL already treats data as first-class:

- `field` and `invariant` declarations are state-agnostic and appear in the recommended ordering before states, events, or transitions.
- The `invariant` keyword is the only statement kind that has no state or event qualifier — it is purely a data constraint.
- Guards are data expressions. The DSL has no syntax for guarding on current state in a `when` clause — you can only read field values. State gates which rows are candidates; data determines which candidate wins.
- The `Update` operation is the only operation in the four-operation surface that has no analog in state-machine theory — it's a direct field edit with constraint enforcement, which is a data-native concept.
- The color system already gives States (violet) and Data (slate) co-equal visual lanes. The palette doesn't subordinate one to the other.

The reframe is a positioning and copy change. The architecture already supports it. No new syntax, no new runtime behavior, no new constraint kind is needed to express "data is what we protect." That was always true; it just wasn't foregrounded in the brand copy.

---

## 6. What I'd Keep and What I'd Change

### Keep

**"Domain integrity engine"** — this is the right category claim. "Integrity" is the correct word for what the engine provides: a guarantee that the entity's state-data combination is always internally consistent. The word "integrity" already points toward data quality, not just state-machine correctness. It's doing more work than it gets credit for.

**"Structurally impossible"** — this is precise. It distinguishes Precept's prevention guarantee from the detection guarantee of validators (which can be bypassed). Keep this phrase in the flagship claim.

**The four-operation description** — mechanically accurate. CreateInstance, Inspect, Fire, Update are the right surface.

**"One file, every rule"** — true and resonant. Keep.

**"Same definition + same data = same outcome"** — this is the determinism guarantee and it's valuable. Note it says "same *data*" — data already features in the core determinism claim.

### Change

**The flagship claim noun.** "Invalid *states*" → "Invalid *configurations*." This is a one-word architectural correction that makes the claim broader, more accurate, and more resonant to the non-state-machine-fluent audience without losing precision.

**The listing order in philosophy copy.** Currently: "states, fields, events, guards, and constraints." Data-first order: "fields (data), states, events, and constraints." The listing order implicitly telegraphs which concept is primary. The recommended ordering in `PreceptLanguageDesign.md` already puts fields and invariants before states — align the philosophy copy to match.

**"Binds state machines, validation, and business rules"** — "state machines" is mechanism-level. The brand copy should say what the mechanism *produces*, not what it uses internally. Candidate: "governs how entity data evolves through its lifecycle." Same fact; outcome-first.

**"Invalid states don't get caught after the fact"** in the differentiation section. This should be: "Invalid data and unauthorized transitions don't get caught after the fact." The word "data" earns its place in the differentiation claim. The current version undersells by limiting the claim to states.

### What I would not adopt from "state as vehicle"

The specific metaphor "vehicle" implies state is passive — a container that moves data around. That's not architecturally accurate:

- States activate different constraint sets.
- States authorize or deny edit operations.
- States provide compile-time structural guarantees.

A vehicle doesn't do any of that. A better architectural metaphor, if you need one: **state is a coordinate in a governed space; data is the substance that occupies that coordinate; the engine ensures the combination is always valid.** But that's too much metaphor for copy — "invalid configurations are structurally impossible" does the job in three words without requiring the metaphor.

---

## Summary

| Question | Answer |
|---|---|
| Is "state as vehicle for data" architecturally accurate? | Partially. State structures the rules; data is what those rules protect. But state is an active rule-activator, not a passive carrier. |
| Is "invalid states are structurally impossible" accurate? | Yes, accurate. |
| Is it complete? | No. The engine also prevents invalid data (invariants) and invalid event inputs (event asserts). The full guarantee is about configurations, not states. |
| Does data-first reframe require DSL changes? | None. The DSL is already data-first in structure; only positioning copy changes. |
| Strongest defensible flagship claim? | "Invalid entity configurations are structurally impossible." |
| What to keep? | "Domain integrity engine," "structurally impossible," four-operation description, "one file, every rule." |
| What to change? | "Invalid states" → "invalid configurations"; listing order (fields before states in copy); "state machines" → "entity lifecycle" in description. |
