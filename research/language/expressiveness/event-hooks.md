# Event Hooks Research

**Research date:** 2026-04-11  
**Author:** Frank (Lead / Architect / Language Designer)  
**Triggered by:** Shane's `on Advance -> set Count = Count + 1` parse error in TrafficLight + subsequent stateless precept insight  
**Status:** Two sub-cases confirmed. Issue A (stateless) → proposal-ready. Issue B (stateful) → further deliberation required.

---

## The Problem Domain

Precept's `on <Event>` form today is arg-only: it introduces event asserts that validate the shape of incoming event arguments. The validation result is `Rejected` or passes through. There is no action surface — no mutations, no outcome selection. The form cannot answer "what does this event *do* to the entity?"

For stateful precepts, `what the event does` is expressed in transition rows: `from <State> on <Event> [when <Guard>] -> <Actions> -> <Outcome>`. The row is state-scoped and self-contained.

For stateless precepts, no equivalent form exists at all. Events are declared. Event asserts compile. But `Fire` returns `Undefined` before asserts even execute, because the runtime aborts at "no transition surface." Event declarations in stateless precepts are currently dead code.

This document surveys how comparable systems handle this problem, then derives locked design positions for Precept.

---

## Part 1 — External Precedent Survey

### 1. XState v5

**Source:** https://stately.ai/docs/actions, https://stately.ai/docs/transitions  
**Fetchdate:** 2026-04-11

**Flat event hook mechanism — targetless root-level `on:`**

XState v5 supports event-level actions that fire regardless of which child state is active through a specific pattern: placing a transition at the **root machine level** with no `target` property.

```typescript
const countMachine = createMachine({
  context: { count: 0 },
  // Root-level on: fires for ALL states when this event is received
  on: {
    increment: {
      // No target = targetless self-transition
      actions: assign({
        count: ({ context, event }) => context.count + event.value,
      }),
    },
  },
});
```

This is XState's flat event hook idiom. The key properties:
- **Source scope:** All states inherit the root machine's `on:` handlers. A more specific state-level handler preempts the root handler (child-over-parent priority).  
- **No target** → the current state does not change. No exit/entry actions fire.
- **Actions do fire:** `assign`, `log`, custom side effects — all execute.
- **Execution in event payload scope:** `event.value`, `event.someArg` are accessible in the action function.
- **No stateless analog:** XState machines always have a current state. The root-level pattern simulates "cross-state" behavior via hierarchy, not statefulness.

**Execution order (XState v5 macrostep):** Event received → select enabled transitions (deepest child first, root if no child matches) → execute transition actions → execute exit/entry actions for state changes. For a targetless root transition: event → execute action → no state change, no exit/entry.

**Limitations:** Child-state handlers shadow root handlers for the same event. If any child state handles `increment`, the root `increment` handler does NOT fire. This is XState's hierarchy-based resolution — useful but requires authors to understand scoping.

**Stateless entity analog:** XState has no concept of a stateless machine. The root state serves as the always-active non-state, but this is an architectural workaround, not a first-class feature.

**Summary for Precept comparison:**
- Yes, flat event hooks exist in XState v5 — but via hierarchy (root-state scoping), not a dedicated syntax
- Args accessible via `context`/`event` parameters
- Execution order: before any state entry/exit actions (targetless → no state change → no entry/exit)
- No conflict with transition rows — targetless transitions don't compete with routed transitions

---

### 2. SCXML / Harel Statecharts

**Source:** https://www.w3.org/TR/scxml/#AlgorithmforSCXMLInterpretation  
**Fetchdate:** 2026-04-11

**No flat event hook at machine level.** SCXML (W3C Recommendation, Sept 2015) defines all executable content within three locations:

1. `<onentry>` — fires when a state is entered  
2. `<onexit>` — fires when a state is exited  
3. `<transition>` executable content — fires when a specific transition is taken

**Execution order (normative, §3.13):** When a transition T is taken, the processor:
1. Executes `<onexit>` handlers of all states being exited (innermost first)
2. Executes the executable content *inside* the `<transition>` element
3. Executes `<onentry>` handlers of all states being entered (outermost first)

**Wildcard event descriptor:** `<transition event="*">` matches any event not handled by a more specific transition. This is NOT a hook — it is still a regular transition competing in first-match order within a state. It requires a source state container.

**Machine-level wildcard:** At the `<scxml>` root level, a `<transition event="*">` can be placed. In practice this acts as a global catch-all because the root `<scxml>` element is an ancestor of all states. It is structurally equivalent to XState's root-level `on:`. However:
- It is still a transition, not an unconditional hook
- It fires only if no more-specific transition is taken (inheritance-override semantics)
- Authors must explicitly know about LCCA (Least Common Compound Ancestor) resolution

**No stateless analog.** SCXML does not model entities without states. All machines must have at least one `<state>` child. The concept of a "stateless SCXML document" is not defined.

**`<finalize>` preprocessing:** The only machine-level "pre-event action" SCXML defines is `<finalize>` — executable content that runs before an event from an invoked subprocess is placed on the event queue. This is strictly limited to `<invoke>` orchestration and has no equivalent in single-machine event handling.

**Harel statecharts background:** Harel's original formulation (1987) defines reactive systems via hierarchy and broadcast events. Cross-state event handling is always through hierarchy — a parent state handles events that children don't. There is no concept of a "machine-level action hook" separate from transition executable content.

**Summary for Precept comparison:**
- No flat event hook without hierarchy
- Execution order is strict: exit → transition content → entry
- No stateless machine support
- Wildcard transitions (`event="*"`) approximate catch-alls but are still transitions, not hooks

---

### 3. Akka Classic FSM

**Source:** https://doc.akka.io/libraries/akka-core/current/fsm.html  
**Fetchdate:** 2026-04-11

**Two relevant mechanisms: `whenUnhandled` and `onTransition`**

```scala
class Buncher extends FSM[State, Data] {
  // State-specific handlers
  when(Active, stateTimeout = 1.second) {
    case Event(Flush, t: Todo) => goto(Idle)...
  }

  // Fires for any event NOT matched by any when() block — not a hook, a fallback
  whenUnhandled {
    case Event(Queue(obj), t @ Todo(ref, v)) =>
      goto(Active).using(t.copy(queue = v :+ obj))  // common to all states
    case Event(e, s) =>
      log.warning("received unhandled request {} in state {}/{}", e, stateName, s)
      stay()
  }

  // Fires when state CHANGES — NOT on every event fire
  onTransition {
    case Active -> Idle =>
      stateData match {
        case Todo(ref, queue) => ref ! Batch(queue)
      }
  }
}
```

**`whenUnhandled`:** Acts like a fallback/catch-all handler. An event reaching `whenUnhandled` means NO `when(S)` block in any currently-active state matched it. It is NOT an unconditional hook — it only fires for unmatched events. Same event in two states where one state has a handler: the matched state fires its handler, `whenUnhandled` is NOT invoked.

**`onTransition`:** Fires when the FSM state actually changes. Pattern matches `(fromState, toState)`. It is NOT event-driven — it's transition-driven. It does NOT fire when the same state is revisited via `stay()` (use `goto(S)` from state `S` to get `S -> S` emission). It cannot access event args.

**Key finding:** Akka Classic FSM has **no flat event hook** — no mechanism to say "whenever event E fires, regardless of state, execute these mutations." The closest patterns are:
- `whenUnhandled`: fires only when no state matched (fallback, not hook)
- `onTransition`: fires on state change, not event fire

**Why this matters for Precept:** Akka's design is explicit that transition-reactive logic (`onTransition`) is structurally separate from event-reactive logic (`when`). The separation is principled — transition hooks are side-effect dispatchers (sending batches, canceling timers); event handlers are behavioral definitions. Precept's `to/from <State> ->` hooks follow the same structural pattern as Akka's `onTransition`.

**Summary for Precept comparison:**
- No flat event hook regardless of source state
- `whenUnhandled` ≠ event hook (fallback only)
- `onTransition` ≠ event hook (state-change callback, arg-less)
- Akka's design confirms that separating "what happens on event" from "what happens on state change" is principled

---

### 4. Spring State Machine

**Knowledge base synthesis (docs.spring.io/spring-statemachine)**

Spring State Machine (Java framework, spring.io) provides machine-level listeners via the `StateMachineListener<S, E>` interface:

```java
stateMachine.addStateListener(new StateMachineListenerAdapter<States, Events>() {
    @Override
    public void transition(Transition<States, Events> transition) {
        // Called for EVERY transition attempt, regardless of source state
    }
    
    @Override
    public void eventNotAccepted(Message<Events> event) {
        // Called when no transition accepted the event
    }
    
    @Override
    public void stateEntered(State<States, Events> state) { ... }
    
    @Override
    public void stateExited(State<States, Events> state) { ... }
});
```

**`transition()` callback:** Fires for every state transition that occurs. This IS a form of universal event hook but:
- It's a **runtime callback listener**, not a declarative DSL construct
- Fires post-transition (after state mutation), not as a first-class action in the execution pipeline
- Cannot reject or modify the transition — observational only
- Lives outside the state machine definition, in application code wiring

**`eventNotAccepted()`:** Like Akka's `whenUnhandled` — fires only when no matching transition was found for the event.

**Declarative actions in Spring SM builder:** When using the builder API, Spring SM supports `action(Action<S,E>)` on individual transitions and `entryAction`/`exitAction` on states. No flat "fires for this event in any state" declarative action exists at the machine level.

**Summary for Precept comparison:**
- Universal event hooks exist as runtime listeners, not DSL
- `transition()` is post-transition, not pre-commit, observational only
- No stateless machine support
- Confirms the pattern is useful (the listener API is widely used for audit logging) but the implementation is outside the state machine declaration

---

### 5. Redux / Redux-Saga (Event-Bus Comparison)

**Knowledge base synthesis**

Redux `applyMiddleware(...)` is the canonical event-bus-level hook pattern:

```javascript
const loggingMiddleware = store => next => action => {
  console.log('dispatching', action);         // Before state update
  const result = next(action);               // Execute all remaining middleware + reducer
  console.log('next state', store.getState()); // After state update
  return result;
};

// Every action dispatched goes through ALL middleware, regardless of what reducer handles it
const store = createStore(reducer, applyMiddleware(loggingMiddleware, analyticsMiddleware));
```

This IS a first-class flat event hook — every dispatched action flows through the middleware chain. No state machine, no "which state am I in?" question. Pure event-bus: event fires → middleware chain → state update.

**Key lessons from the Redux middleware pattern:**

1. **Utility is clear:** Middleware is the standard mechanism for audit logging, analytics tracking, error handling, and cross-cutting concerns in Redux. These are exactly the use cases proposed for Precept event hooks.

2. **The decoupling risk is real:** Redux middleware is notoriously hard to reason about because ANY action triggers ALL middleware. Debugging unexpected behavior requires understanding the full middleware stack. This is the "hidden mutation" problem Precept's Principle 7 guards against.

3. **Redux-Saga** takes this further — sagas observe every action and can spawn complex async workflows from any action, anywhere. The expressiveness that results is powerful but the "action side effects are everywhere" problem is a frequent source of Redux application complexity.

4. **The Precept lesson:** The middleware pattern is useful precisely because developers need to bolt cross-cutting concerns onto events. But Redux middleware creates exactly the "reader of any row must check the middleware stack" problem that Principle 7 was written to prevent. An event hook *for stateless precepts* avoids this: there are no transition rows to be confused by. In a stateful precept, the concern is valid.

**Summary for Precept comparison:**
- Redux proves the utility of flat event hooks for cross-cutting concerns
- Redux also proves the coupling cost — hidden action side effects are a long-standing pain point in Redux applications
- The Precept stateless case avoids the coupling problem because there are no other rows to be confused by
- The Precept stateful case inherits the same coupling risk as Redux middleware

---

### Comparative Table

| System | Flat event hook? | Mechanism | Stateless analog? | Execution order relative to state entry/exit | Constraints |
|---|---|---|---|---|---|
| **XState v5** | Yes (via hierarchy) | Targetless root-level `on:` handler | No — always stateful | Before entry/exit (targetless = no state change) | Child state handlers shadow root handlers |
| **SCXML** | No at flat level | Parent-state `<transition event="*">` (requires hierarchy) | No — states required | Exit → transition content → entry (normative) | Hierarchy required for cross-state scope |
| **Akka Classic FSM** | No | `whenUnhandled` (fallback) + `onTransition` (state-change only) | No | `onTransition` fires post-state-change | Neither is an unconditional event hook |
| **Spring SM** | Via runtime listener only | `StateMachineListener.transition()` callback | No | Post-transition, observational | Outside DSL, cannot reject/modify |
| **Redux middleware** | Yes — pure event bus | `applyMiddleware(store => next => action => ...)` | N/A — no state machine | Before reducer (pre-state-update) | Fires for ALL actions, hard to scope |
| **Precept (proposed)** | Proposed — Case A (stateless) | `on <Event> -> <ActionChain>` | **Yes — first-class** | After event asserts, before invariants | No Principle 7 tension in stateless context |

**Key synthesis finding:** No system in the survey provides a flat event hook for a *stateless entity* with typed event arguments and post-mutation constraint enforcement. This is a Precept-specific innovation. The use case — "event triggers field mutation, invariants enforce correctness, no states required" — maps most closely to a CQRS command model: command receipt → validate args → execute mutation → enforce invariants. All surveyed systems that have event hooks do so within state machine contexts where "which state am I in?" is always asked first.

---

## Part 2 — Precept Design Alignment

### A. Stateless Precept Event Hooks (Issue A)

#### Proposed syntax

**Primary form — mutation hook:**
```precept
on <EventName> -> <ActionChain>
```

Where `<ActionChain>` is the same mutation pipeline allowed in transition rows: zero or more `set`/collection action steps. No outcome terminator (`transition`, `no transition`, `reject`) — those belong to the row model, which stateless precepts don't have.

```precept
precept SubscriptionAccount

field Balance as number default 0

event Deposit with Amount as number
on Deposit assert Amount > 0 because "Deposit amount must be positive"
on Deposit -> set Balance = Balance + Deposit.Amount

event Withdraw with Amount as number
on Withdraw assert Amount > 0 because "Withdrawal amount must be positive"
on Withdraw -> set Balance = Balance - Withdraw.Amount

invariant Balance >= 0 because "Balance cannot be negative"
```

**Combined form — assert + hook:**

The `on <Event> assert ... because ...` form and `on <Event> -> ...` form are separate statements. An event may have both:
```precept
on Deposit assert Amount > 0 because "Deposit amount must be positive"
on Deposit -> set Balance = Balance + Deposit.Amount
```

There is no combined `on Deposit assert Amount > 0 because "..." -> set Balance = ...` form. The two concerns (argument validation, field mutation) are distinct declarations. Precedent: transition rows separate guards from actions (`when <Guard> -> <Actions>`). So too here: assert validates argument shape, hook executes consequence. One concern per declaration.

#### Execution order in the fire pipeline

The stateless fire pipeline (with event hooks) collapses the stateful pipeline to five stages:

| Stage | Stateful | Stateless (with hooks) |
|---|---|---|
| 1. Event asserts | ✓ Validate event args | ✓ Validate event args |
| 2. Row selection | ✓ FIRST-MATCH | **Not applicable — no rows** |
| 3. Guard evaluation | ✓ `when` on row | **Not applicable** |
| 4. Row mutations | ✓ `set` / collection ops | **Replaced by event hook mutations** |
| 5. Exit actions | ✓ `from <S> ->` | **Not applicable — no states** |
| 6. Entry actions | ✓ `to <S> ->` | **Not applicable — no states** |
| 7. Invariant enforcement | ✓ Collect all | ✓ Collect all |

**Locked execution order for stateless event hooks:**
1. Event asserts — validate event args first. If any assert fails → `Rejected`, hook does not execute.
2. Event hook mutations — apply `set` / collection operations.
3. Invariant enforcement — collect all violations against post-mutation state. If any invariant fails → `Rejected`. Mutations are rolled back.

This order is the only coherent option. It mirrors what SCXML calls "executable content on transition" (stage 2 in SCXML's exit → transition-content → entry model), adapted to a stageless context. It also mirrors the Precept stateful pipeline: asserts before mutations, invariants after.

**External precedent confirming this order:** SCXML §4.1 states executable content is "performed as part of taking transitions." The order within a transition is: evaluate guard (`cond`) → execute transition content → enter target state. Precept's analogue: evaluate asserts → execute hook mutations → enforce invariants.

#### Outcome model

In a stateless precept with event hooks, the three possible outcomes are:

| Outcome | When |
|---|---|
| `Success` | Event asserts pass, hook mutations applied, all invariants satisfied post-mutation |
| `Rejected` | (a) Any event assert fails (arg validation), OR (b) Any invariant violated after hook mutations |
| `Undefined` | Event not declared in this precept |

No `Unmatched` (no rows to fail to match). No `NoTransition` (no states to transition to/from). The outcome space is the minimal sufficient set.

**Rejected subdivisions (for diagnostics/MCP output):**
- Rejected via assert: `violations` list contains event-assert failures. Hook did not execute.
- Rejected via invariant: `violations` list contains invariant failures. Hook did execute (mutations visible in the violation context).

Both report as `Rejected` to the caller. The distinction is surfaced in the `violations` structure for diagnostic and MCP tooling.

#### What happens to C49?

**Current C49:** "Event 'X' is declared but never referenced in any transition row." Emitted as Warning for each event in a stateless precept.

**With event hooks, C49 needs revision.** An event that has a hook IS referenced — it has behavioral meaning. The diagnostic should be suppressed for events that have at least one `on <Event> -> ...` hook.

**Proposed C49 revision (two conditions):**
- Stateless precept, event declared, **no hooks AND no event asserts** → C49 Warning: "Event 'X' is declared but has no effect" (tighter message: event truly dead)
- Stateless precept, event declared, **has event asserts but no hooks** → C49 Warning: "Event 'X' validates arguments but has no action effect" (separate, new diagnostic message acknowledging the assert-only pattern is intentional and potentially valid)
- Stateless precept, event declared, **has at least one hook** → C49 suppressed

The current C49 framing ("unreachable — no transition surface") is a fossil of the stateless design being incomplete. When hooks ship, C49's framing changes from "this event is unreachable" to "this event has no effect." These are meaningfully different warnings: the first suggests a structural error; the second is closer to an informational/style note.

**Note:** The "has asserts but no hooks" sub-case is worth examining — it may be intentional (validate-only events used for side-channel input validation, not field mutation). Whether C49 should continue to warn in that case, or whether it should be a separate diagnostic, is a decision for the proposal to lock.

#### Can event args be referenced in the hook action?

**Yes.** The mutation chain in event hooks must support `<EventName>.<ArgName>` references (e.g., `Deposit.Amount`) for the feature to be useful. Without arg references, an event hook can only compute from current field values — it cannot incorporate the event's payload into the mutation.

**Precedent:** Transition row mutations already support arg references:
```precept
from Draft on Submit -> set ApplicantName = Submit.Applicant
```

Event hook mutations follow the same pattern:
```precept
on Deposit -> set Balance = Balance + Deposit.Amount
```

**Type checking scope:** The type checker must resolve `Deposit.Amount` in an event hook as: "event `Deposit` has arg `Amount` of type `number`; reference is valid." This is the same scope resolution used in transition row mutations. No new type-checking mechanism is required.

**The C16 gap:** Current C16 errors on `Balance` in `on Withdraw assert Amount <= Balance` because event asserts are arg-only. In an event hook, field references are permitted: `set Balance = Balance - Withdraw.Amount` references both the field `Balance` and the arg `Withdraw.Amount`. This is correct and expected — hooks are field-mutation constructs, not pure arg validators.

#### `precept_inspect` output for stateless precepts

Today, `Inspect(instance, event)` on a stateless precept returns `Undefined` for all events. With event hooks, the MCP tool must return a meaningful preview.

**Proposed MCP `precept_inspect` output for stateless event with hooks:**
```json
{
  "event": "Deposit",
  "outcome": "Success",
  "mutations": [
    { "field": "Balance", "from": 100, "to": 150 }
  ],
  "assertsEvaluated": [
    { "expression": "Amount > 0", "result": true }
  ],
  "invariantsEvaluated": [
    { "expression": "Balance >= 0", "result": true }
  ]
}
```

The `mutations` array mirrors the existing MCP output for stateful transitions. The `assertsEvaluated` and `invariantsEvaluated` arrays are the structured constraint trace already proposed in the Issue #14 design work (Newman B2 additions). No new DTO shapes are required — the existing stateful trace model applies directly.

---

### B. Stateful Precept Event Hooks (Issue B)

#### Position: Separate proposal

**Frank's position:** Issue A (stateless) and Issue B (stateful) must be separate proposals. They share syntax (`on <Event> ->`), but their semantics, Principle 7 implications, and design risk profiles are fundamentally different.

**Rationale:**

1. **Issue A has zero Principle 7 tension.** Stateless precepts have no transition rows. There is nothing for an event hook to hide behind. The hook IS the row.

2. **Issue B has unresolved Principle 7 tension.** A stateful precept event hook runs *in addition to* whichever transition row matches. A reader of `from Red on Advance -> transition Green` does not know whether there is also `on Advance -> set Count = Count + 1` firing alongside it. This is exactly the shared-context problem Principle 7 was written to prevent.

3. **Issue A has a fully determined execution order.** In stateless precepts: asserts → hook mutations → invariants. No ambiguity.

4. **Issue B has four viable execution order positions** with different semantics (before row selection? after row, before exit? after exit, before entry? after entry?). Each position produces different behavior for counters, timestamps, and auditing. This is not a minor decision.

5. **Issue A can ship without resolving Issue B.** The stateless case is a bounded, principled feature. No implementation of Issue A constrains Issue B's execution order choice.

#### Minimum design work for Issue B

Before Issue B can be evaluated seriously:

1. **Execution order must be decided.** The four options are:
   - **Option 1: Before row selection** — fires even if no row matches or guard fails. Semantics: "this always runs when this event fires in any state, regardless of routing outcome." This conflicts with `Undefined` and `Unmatched` outcomes — does the hook fire on those?
   - **Option 2: After matching row identified, before mutations** — fires only if a row matches. The hook runs first, then the row's own mutations. This is the "setup" position.
   - **Option 3: After transition row mutations, before exit actions** — this was Frank's initial suggestion in `frank-event-hook-gap.md`. External evidence from SCXML confirms this position: transition content runs after exits of the *source* state but before entries of the *target* state. In SCXML, this position is used for data manipulation that bridges old and new state. For Precept, this means the hook can see the row's mutations in the same `Balance` that was modified by the row.
   - **Option 4: After exit actions, before entry actions** — runs between `from <S> ->` and `to <S> ->` hooks. This is the most disruptive to the mental model: the entity is mid-transition, neither fully in the old state nor the new state.

2. **Outcome scoping for each option must be specified:**
   - Does the hook fire on `Undefined` outcome (no rows for state+event)?
   - Does the hook fire on `Unmatched` outcome (rows exist but all guards false)?
   - Does the hook fire on `reject` outcome?
   - Does the hook fire on `no transition` outcome?

3. **Principle 7 tension must be explicitly stated in the proposal.** The proposal must either: (a) argue that the bounded exception is acceptable (with criteria for when an exception is acceptable), or (b) propose a mitigation that preserves the self-contained-row property (e.g., a lint rule that flags rows that would be affected by a hook without declaring they depend on one).

**Frank's current recommendation for execution order (pending external evidence):** Option 3 — after transition row mutations, before exit actions. Rationale: SCXML §4.1 places transition executable content ("executable content occurs inside `<onentry>` and `<onexit>` elements *as well as inside transitions*") in the execution gap between exit-actions and entry-actions. In the stateful pipeline, a cross-event hook is most analogous to a second block of transition content — it fires after the row's own mutations but before the state-routing actions complete. This position is consistent with the "data manipulation bridge" role event hooks serve in most domain scenarios (counters, timestamps, audit fields).

**But Option 3 requires locking:** "fires on Success outcomes only (transition, no transition), not on Unmatched or Undefined." This is the principled baseline. Firing on Unmatched would require the hook to run even when the entity doesn't respond to the event — semantically incoherent for a domain action. Firing on Undefined is worse: the event didn't route at all.

---

### C. Locked Design Decisions for Issue A (Stateless Event Hooks)

These decisions should be locked in the proposal issue body. Each follows the per-decision format required by CONTRIBUTING.md.

---

**Decision A1 — Syntax form: `on <Event> -> <ActionChain>`**

- **What:** The event hook form is `on <EventName> -> <ActionChain>` where `<ActionChain>` is one or more `set`/collection action steps (no outcome terminator).
- **Why:** Consistent with existing `->` semantics ("`->` means do something"). Keyword-anchored. Reads naturally: "on Deposit, do this."
- **Alternatives rejected:** (a) A new keyword like `effect` or `handle` — adds to vocabulary without gain; (b) Inline on the assert line — conflates arg validation with field mutation, violating single-concern declaration principle.
- **Precedent:** `to <State> -> <Action>` form for state entry hooks uses the same structure. `from ... on ... -> <Actions> -> <Outcome>` normalizes `->` as the action/consequence separator.
- **Tradeoff accepted:** Syntax is ambiguous if read in isolation: `on Deposit -> set Balance = ...` could be misread as a transition row by a new author. Context disambiguation required: transition rows have `from <State>` prefix; event hooks do not.

---

**Decision A2 — Arg references permitted in hook mutations**

- **What:** Event hook mutations may reference event args via `<EventName>.<ArgName>` (e.g., `Deposit.Amount`).
- **Why:** The primary use case for event hooks is incorporating event payload into field state (balance += deposit amount). Without arg access, the feature is nearly useless.
- **Alternatives rejected:** Arg-free hooks only — this would force roundabout field-caching patterns and is inconsistent with transition row behavior which already allows arg references.
- **Precedent:** Transition row mutations already support `Submit.Applicant`, `Approve.Amount` arg references. The type-checking mechanism is already built.
- **Tradeoff accepted:** Introducing `<Event>.<Arg>` in a non-row context expands the scope where event arg names must be resolved. The type checker must not allow field references in event asserts (C16 still applies there) but must allow them in event hooks. Clear diagnostic boundary.

---

**Decision A3 — Execution order: asserts → hook mutations → invariants**

- **What:** The stateless fire pipeline in order: (1) evaluate all event asserts — if any fail, reject before hook runs; (2) execute hook mutations; (3) evaluate all invariants — if any fail, reject (roll back mutations).
- **Why:** Mirrors the existing stateful pipeline structure (asserts → row mutations → invariants). Consistent with SCXML's normative order (evaluation → content execution → constraint check). Fail-fast at the cheapest stage.
- **Alternatives rejected:** (a) Run hook before asserts — allows field mutation even when event args are invalid; (b) Merge asserts and hooks into one pass — violates single-concern declaration principle; (c) Check invariants before hook runs — makes invariants check pre-mutation state, defeating their purpose.
- **Precedent:** SCXML §3.1.1: "the state machine will perform actions A" occurs after evaluating transition conditions. Akka FSM event handlers execute only after pattern matching succeeds. Fail-fast arg validation is universal across surveyed systems.
- **Tradeoff accepted:** An event assert failure prevents hook mutations from running. Authors who want partial mutation on bad input cannot do so — the entity is never modified when args are invalid.

---

**Decision A4 — Outcome space: Success / Rejected / Undefined**

- **What:** The stateless fire pipeline produces exactly three outcomes. `Unmatched` and `NoTransition` do not exist in stateless context.
- **Why:** There are no rows to fail to match. There are no states to (not) transition to. The reduced outcome space is a direct consequence of the stateless model.
- **Alternatives rejected:** Introducing `NoTransition` as a hook-fired-but-no-state-change outcome — confusing (no states = what is "no transition" even?). Introducing `Unmatched` for "no hook declared for this event" — collapses to `Undefined` because undeclared events already map to `Undefined`.
- **Precedent:** CQRS command model: command validate → execute → post-condition. Success/fail. Stateless constraint enforcement has no "moved to next state" concept.
- **Tradeoff accepted:** Authors cannot express "this event has no effect in this case" (there are no guards and no cases in a stateless model). If conditional behavior is needed, fields and invariants are the mechanism.

---

**Decision A5 — C49 revision: suppress when a hook exists**

- **What:** C49 is suppressed for an event that has at least one `on <Event> -> ...` hook. C49 may still fire for events that have event asserts only (the "validates but has no action effect" sub-case — details to be locked in proposal).
- **Why:** C49's warning "event declared but unreachable" is semantically incorrect when a hook exists. The event IS reachable — it fires, executes, and mutates the entity.
- **Alternatives rejected:** Remove C49 entirely — this would silence helpful diagnostics for truly inert event declarations. Upgrade C49 to error — too aggressive; declaring an event without a hook may be deliberate (e.g., during incremental authoring).
- **Precedent:** Compiler warning suppression when a pattern is resolved is standard practice. C49 was designed to diagnose a specific structural gap. When the gap no longer exists, the warning should be silent.
- **Tradeoff accepted:** Authors who declare an event with only assert (no hook) in a stateless precept may still see a warning, which could be surprising if their intent is validation-only. This sub-case may warrant a new diagnostic code (C49v2) to distinguish from truly dead declarations.

---

## Part 3 — Dead Ends Explored

**"Stateless event hook as a variant of the event assert form"**  
Considered: extending `on <Event> assert ... because ... -> <Action>` to allow a trailing action. Rejected: conflates argument validation (event assert) with field mutation (event hook). These have different failure semantics, different scoping rules (assert arg-only, hook field+arg), and different purposes. Keeping them as separate declarations preserves single-concern clarity.

**"Hook fires before event asserts"**  
The logical argument: asserts validate args, hooks use args, what if the arg is valid but the entity state makes the hook nonsensical? Rejected: this is what invariants are for. Pre-assert mutation is incoherent — if the arg is invalid, the mutation is built on an invalid foundation. Every surveyed system validates input before acting on it.

**"Hook fires on Unmatched/Undefined in the stateless context"**  
Not applicable — there are no rows in a stateless precept, so `Unmatched` doesn't exist. `Undefined` means the event isn't declared. Hook-on-Undefined would require inventing a "fire this action even when the event isn't recognized" semantic, which has no precedent and would make invariant enforcement incoherent (the entity's state resulted from an unknown cause).

**"Combine Issue A and Issue B into one proposal"**  
Strongly considered, briefly. The shared syntax (`on <Event> ->`) creates a temptation to combine them. Rejected for the reasons stated in Part 2B: the Principle 7 tension is real for Issue B and does not exist for Issue A. Conflating them in one proposal would either (a) force Issue A to carry Issue B's unresolved design questions, or (b) allow Issue B to slip through on the coattails of Issue A's clean case.

---

## Locked Conclusions

1. **Issue A (stateless) is a confirmed gap, not a deferred feature.** Events in stateless precepts are currently dead code. The C49 warning acknowledges the gap without resolving it. Stateless event hooks are the principled resolution.

2. **Issue A has zero Principle 7 tension.** Principle 7 governs "self-contained rows." Stateless precepts have no rows. The principle does not apply to the stateless context.

3. **The correct execution order for Issue A is: event asserts → hook mutations → invariants.** This is the minimal, coherent order. It mirrors the existing stateful pipeline and is confirmed by SCXML's normative execution order.

4. **Event arg references (`<Event>.<Arg>`) must be permitted in hook mutations.** Without arg access, the feature has no primary use case.

5. **C49 must be revised when hooks ship.** An event with a hook is not unreachable. The message changes from "no transition surface" to "no action effect" only for truly inert declarations.

6. **No system in the survey provides a stateless flat event hook — this is Precept-native territory.** The closest analogies are CQRS command handling (validate → execute → post-condition) and XState's targetless root transitions (hierarchy-based, not stateless).

7. **Issue B (stateful) is not blocked but requires execution order and Principle 7 to be formally resolved before a proposal can advance.** Frank's provisional recommendation: execution order Option 3 (after row mutations, before exit actions) — but this must be deliberated, not assumed.

---

## References and Citations

- SCXML W3C Recommendation, §3.1.1 "Basic State Machine Notation": https://www.w3.org/TR/scxml/#BasicStateMachine  
- SCXML §4.1 "Introduction to Executable Content": https://www.w3.org/TR/scxml/#executable-content  
- SCXML §3.13 "Selecting and Executing Transitions": https://www.w3.org/TR/scxml/#SelectingAndExecutingTransitions  
- XState v5 Actions: https://stately.ai/docs/actions  
- XState v5 Transitions (targetless self-transitions): https://stately.ai/docs/transitions  
- Akka Classic FSM (`whenUnhandled`, `onTransition`): https://doc.akka.io/libraries/akka-core/current/fsm.html  
- Spring State Machine listener API: https://docs.spring.io/spring-statemachine/docs/current/reference/  
- `docs/PreceptLanguageDesign.md` — Principle 7, stateless precept spec, C49 definition  
- `docs/philosophy.md` — governed integrity, stateless precept positioning  
- `.squad/decisions/inbox/frank-event-hook-gap.md` — gap investigation and addendum  
- `research/language/expressiveness/xstate.md` — existing XState comparison  
- `research/language/expressiveness/transition-shorthand.md` — transition compactness background  
- `research/language/references/state-machine-expressiveness.md` — PLT generation taxonomy
