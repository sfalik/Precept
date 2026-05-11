# Stateless Events

**Research date:** 2026-04-17  
**Author:** Frank (Lead / Architect / Language Designer)  
**Triggered by:** Design evaluation session — stateless precepts (#22) have a mutation surface gap; events declared in stateless precepts are currently dead code.  
**Status:** Locked. Proposal ready. Linked issue: #112.

---

## The Problem Domain

Precept's stateless precepts (#22) can declare fields, invariants, and editability rules. What they cannot express is operations: "when this event fires, update these fields." Events can be declared and their arg shapes enforced, but the runtime aborts at "no transition surface" before any action chain executes.

This creates an expressiveness gap that pushes authors toward two antipatterns:

1. **Phantom states** — declaring a `state Default` placeholder solely to host transition rows, when the entity has no meaningful lifecycle position.
2. **Pseudo-lifecycle** — encoding position as a field value and routing events by guarding on that field. This buries what is semantically a lifecycle in a data field, defeating the structural enforcement Precept is designed to provide.

Both antipatterns are structurally dishonest: the precept claims a shape it does not have. The proposed `on EventName` bare form closes this gap by giving stateless precepts a first-class mutation surface.

---

## Precedent Survey

### 1. Redux (JavaScript/TypeScript)

**Model:** Pure function reducers. Each reducer handles one action type — no current state required, no routing table, no lifecycle position.

```javascript
// Reducer: "when this action fires, compute new state"
function cartReducer(state = initialState, action) {
  switch (action.type) {
    case 'UPDATE_QUANTITY':
      return {
        ...state,
        quantity: action.payload.quantity,
        total: action.payload.quantity * state.unitPrice,
      };
    case 'APPLY_DISCOUNT':
      return {
        ...state,
        discount: action.payload.discount,
        total: state.quantity * state.unitPrice * (1 - action.payload.discount),
      };
    default:
      return state;
  }
}
```

**Key properties relevant to Precept:**
- The action handler executes regardless of any "current position" — there is no state machine.
- Multiple cases can match, but the `switch` selects exactly one branch (first-match equivalent).
- The return value is the new data state — pure mutation semantics.
- Guards (`when` equivalents) are expressed as inline conditionals inside the case body.
- No lifecycle, no position: the entity is fully characterized by its data and its declared action handlers.

**What Redux does NOT have:** Guard-based row selection. In Redux, the switch case selects the handler; conditional behavior is inline code. Precept separates guard evaluation from action execution (the `when` clause is a first-class pre-condition, not inline logic), which is the more declarative and auditable form.

**Precedent strength for Precept:** Strong. Redux reducers are the dominant pattern for stateless event-driven mutation in modern software. The "action fires → data transforms → new data state" model is exactly the model Precept's stateless `on` form needs to express.

---

### 2. XState v5 — Targetless Transitions

**Source:** https://stately.ai/docs/transitions (2026-04-11 fetch, documented in `event-hooks.md`)

**Model:** Root-level `on:` blocks with no `target` property. The machine receives the event, executes the assigned actions, and does not change state.

```typescript
const pricingMachine = createMachine({
  context: { quantity: 1, unitPrice: 0, total: 0 },
  on: {
    UPDATE_QUANTITY: {
      // No target: no state change
      actions: assign({
        quantity: ({ event }) => event.quantity,
        total: ({ context, event }) => event.quantity * context.unitPrice,
      }),
    },
  },
});
```

**Key properties:**
- "Targetless" means the event fires an action chain but causes no state change.
- XState enforces this structurally: omitting `target` is not ambiguous, it is the defined form.
- Authors cannot accidentally transition when they mean to mutate.

**What XState requires:** A machine always has a current state. XState simulates stateless behavior via the root machine level — a structural workaround. Precept's stateless precepts are first-class, not workarounds. The `on EventName` form in a stateless precept is the explicit, unambiguous version of what XState approximates via root-level targeting.

**Precedent strength:** Strong for the targetless concept. XState confirms that state machines can expose an event surface that produces mutations without routing, and that this is a valid, expressible idiom in the state machine world.

---

### 3. CQRS Command Handlers

**Model:** Command/Query Responsibility Segregation separates write operations (commands) from read operations (queries). A command handler receives a command, validates it, and mutates the aggregate. No routing by position — the handler is dispatched to by command type, not by the aggregate's current state.

```csharp
public class UpdateQuantityHandler : ICommandHandler<UpdateQuantityCommand>
{
    public Result Handle(UpdateQuantityCommand command, OrderItemAggregate aggregate)
    {
        if (command.NewQuantity <= 0)
            return Result.Failure("Quantity must be positive");
        
        aggregate.Quantity = command.NewQuantity;
        aggregate.Total = aggregate.Quantity * aggregate.UnitPrice;
        return Result.Success();
    }
}
```

**Key properties:**
- The handler is registered against a command type, not a state.
- Guards (business rule checks) precede mutations.
- The handler is pure: same command + same aggregate state = same mutation outcome.
- Multiple handlers for the same aggregate type coexist without conflict.

**What CQRS is missing that Precept provides:** CQRS command handlers are code, not declarations. The structural enforcement of invariants after mutation is the developer's responsibility. Precept's stateless `on` form is a declarative CQRS command handler with structural invariant enforcement — the postcondition check is not optional or developer-authored, it is built into the fire pipeline.

**Precedent strength:** Strong for the "handler per command type, no state routing" pattern. The CQRS model confirms that event/command-driven mutation without lifecycle routing is a mature, well-understood design pattern.

---

### 4. Drools Stateless Sessions

**Model:** A Drools stateless knowledge session executes a rule set against a fact set in a single pass. Rules fire when their conditions are met, regardless of any session state — there is no "current state" to route through.

```
rule "Compute Order Total"
when
    $item : OrderItem(quantity > 0, unitPrice > 0)
then
    modify($item) { setTotal($item.getQuantity() * $item.getUnitPrice()) }
end
```

**Key properties:**
- Rules fire when their `when` block is satisfied — this is the guard evaluation model.
- Multiple rules can fire in a single pass (Drools agenda-based resolution handles conflicts).
- Stateless sessions produce no retained working memory — each invocation is independent.
- The rule body is the mutation surface: `modify`, `insert`, `retract`.

**What Drools does differently:** Drools fires all matching rules (subject to salience/conflict resolution), not first-match like Precept's row selection. This is a meaningful semantic difference — Precept's first-match model is more predictable and auditable. But the structural pattern — guard → mutation, no position routing — is directly analogous.

**Precedent strength:** Moderate-strong. Drools stateless sessions confirm that declarative rule-based mutation (guard + action, no lifecycle) is a proven pattern in the enterprise decision-engine space.

---

### 5. Event Sourcing — Aggregate Apply Methods

**Model:** Event-sourced aggregates reconstruct state by replaying events. Each event has an `Apply` method that mutates the aggregate's in-memory projection. The `Apply` method does not route by state — it unconditionally updates the data projection.

```csharp
public class OrderItem
{
    public void Apply(QuantityUpdatedEvent @event)
    {
        Quantity = @event.NewQuantity;
        Total = Quantity * UnitPrice;
    }
    
    public void Apply(PriceAdjustedEvent @event)
    {
        UnitPrice = @event.NewUnitPrice;
        Total = Quantity * UnitPrice;
    }
}
```

**Key properties:**
- `Apply` methods are dispatched by event type, not by aggregate state.
- The method executes unconditionally (guards, if any, are in the command handling layer).
- Multiple apply methods coexist without conflict.
- The projection model is exactly "event fires → fields update" — the same model Precept's stateless `on` form expresses.

**What event sourcing leaves implicit:** Invariant enforcement. In event-sourced systems, invariants are checked during command handling; by the time an event is applied, it has already been validated. Precept's fire pipeline runs invariant checks after mutation regardless of which layer the event arrives from — a stronger guarantee.

**Precedent strength:** Strong for the "event type dispatching + field mutation" model. Event sourcing confirms that registering field-mutation handlers against event types — without lifecycle routing — is standard practice in DDD-influenced architectures.

---

## The Pseudo-Lifecycle Antipattern

### What it is

A pseudo-lifecycle is a lifecycle encoded in a data field. Instead of declaring states (`Draft`, `Confirmed`, `Shipped`) and transition rows, the author declares a `Status as choice` field and guards stateless event handlers on its value.

### Why it is harmful

The pseudo-lifecycle defeats Precept's core structural guarantee at three levels:

**1. Enforcement is advisory, not structural.** In a true stateful precept, the transition table defines which operations are valid from which positions. An event that has no `from <State>` row for the current state is structurally rejected — the runtime never evaluates the action chain. In a pseudo-lifecycle, every event is structurally valid from "any position." Only the guard prevents the wrong operation. If the guard is missing or wrong, the operation executes. The runtime cannot distinguish "I forgot to guard this" from "this is intentional."

**2. The model is underdeclared.** A stateful precept makes positions explicit: the type checker can detect unreachable states (C13), transitions that do not respect state topology (C50), and positions where the entity is stuck. A pseudo-lifecycle is invisible to these checks. The author's lifecycle-as-field can have unreachable values, contradictory guard conditions, and dead event handlers — and the type checker has no vocabulary to flag them.

**3. Inspectability is degraded.** `precept_inspect` can show exactly what events are valid from a named state, because states are first-class. A pseudo-lifecycle has no first-class positions to inspect from. Callers must reason about which field values are "current positions" themselves.

### Structural proof

Given a precept with N events, a `Status` field with M distinct values, and guards of the form `when Status = "X"`:

- A stateful encoding declares M states and N × M (at most) transition rows. The type checker can verify the transition table's coverage and topology.
- A pseudo-lifecycle encoding declares 0 states and N stateless rows. Each row carries a `when Status = "X"` guard. The type checker sees N event handlers with field guards — identical to any other data-conditional mutation.

The structural distinction the type checker can observe: in the pseudo-lifecycle, the same field appears as the discriminator in guards across multiple events, with a finite named vocabulary of literals that partitions the guard space. This is the heuristic signal for C102.

### When data-conditional mutation is NOT a pseudo-lifecycle

The heuristic must not fire for legitimate data-conditional behavior. Two patterns that are NOT pseudo-lifecycle:

1. **Threshold-based branching:** `when Amount > 1000` and `when Amount <= 1000` — the discriminator is a numeric threshold, not a named position. These are genuinely independent data conditions, not lifecycle positions.

2. **Optional-feature guards:** `when NotificationsEnabled = true` — a boolean field used as a feature flag. This is a data condition, not a lifecycle position, because the entity doesn't progress through "notifications on → notifications off" as lifecycle stages.

The heuristic fires when: (a) the discriminator field is a `choice` or `string` type with a finite named vocabulary, (b) the guards use equality comparisons against distinct literals, and (c) the same field appears across 3+ event handlers. That cluster is the pseudo-lifecycle signal.

---

## Guardrail Strategy Analysis

Three tiers of guardrails are designed for the stateless event surface. This section analyzes why each tier takes its chosen form.

### Tier 1: Structural constraints (hard errors)

Two structural hard errors bookend the feature:

**C_STATELESS_TRANSITION:** `transition` keyword in a stateless `on` block.

This is a hard error because `transition` is semantically incoherent in a precept with no states. There is no state to transition to. The error category is "meaningless instruction" — the same category as writing `goto` in a language without labels, or `async` on a function with no awaitable calls. Hard error is unambiguous; warning would imply that `transition` is sometimes valid in a stateless block, which it is not.

**C_STATELESS_MIXING:** Bare `on` rows coexist with `from/on` rows in the same precept.

This is a hard error because the entity cannot coherently be both position-aware and position-agnostic. The mixing produces two irreconcilable models:
- `from S on E → ...` says "this event is only valid from state S"
- `on E → ...` says "this event is valid regardless of position"

For the same event E, these would need a priority or override model. Precept does not have one. The hard error prevents authors from creating a precept with undefined semantics. This is symmetrical to C55 (root `edit` is invalid when states are declared) — the symmetry is intentional.

### Tier 2: Heuristic warning (C102)

The pseudo-lifecycle warning is a heuristic because the target antipattern cannot be detected without false positives at compile time. The heuristic is calibrated to minimize false positives while still surfacing the most common antipattern cases.

**Why warning, not error:**
The heuristic targets a code-smell category, not a structural incoherence. Structurally incoherent code gets hard errors (Tier 1). Code that is valid but indicates a possible design mistake gets warnings. This is the established pattern across static analysis tools (ESLint, Roslyn analyzers, Pylance).

**Why 3+ events, not 2+:**
The 2-event case has too many legitimate interpretations. An entity that handles `Activate` and `Deactivate` with `when Status = "active"` guards might genuinely be stateless with two data-conditional branches. The guard presence on a shared discriminator field doesn't prove lifecycle intent. At 3+ events, the cluster becomes harder to explain as coincidental data-conditioning and stronger as lifecycle-in-a-field evidence.

**Alternative: full mutual exclusivity proof**
A sound heuristic would prove that the guards partition the discriminator field's value space — that no two rows' guards can simultaneously be true. This would eliminate false positives on threshold-branching patterns. The cost is constraint solving at compile time. The current heuristic is field-type + literal equality + count. The full mutual exclusivity proof is a possible enhancement once the base feature is stable.

**Alternative: skip C102, rely on documentation**
The warning is the teaching mechanism. Authors who encounter C102 are at exactly the right moment — writing a stateless precept with event handlers — to receive the structural guidance. Documentation-only education reaches authors who read docs; C102 reaches authors who are coding.

### Tier 3: MCP tool guidance

**`precept_language` pre-flight guidance:** The stateless-vs-stateful decision rule is included in the `precept_language` output. Authors using the MCP tools to author a precept receive this guidance before they write the first line. This surfaces the design question at the right authoring moment.

**`precept_inspect` pattern output:** Stateless `on` handlers are surfaced in inspect output — each event shows what it would do from the current stateless state. This makes the mutation surface observable without execution.

**`precept_compile` suggestion block:** Skipped for this feature. The C102 diagnostic carries enough teaching content that a separate suggestion block in compile output would duplicate it. Diagnostic messages should teach; if the diagnostic message is good, the suggestion block is redundant overhead.

---

## Alternatives Considered and Rejected

### Alternative A: A separate `action` keyword

```precept
action UpdateTotal
    -> set Total = Quantity * UnitPrice
```

**Why rejected:** An `action` keyword creates a new top-level declaration form that is not event-triggered — it's a named procedure. This imports the "named procedure" concept from general-purpose languages, where Precept's model is declarative event-driven. If named actions exist, they can be called from transition rows, creating a subroutine model. That model is significantly more complex (call semantics, scope rules, recursion guards, tooling surface) than the proposal requires. The expressiveness need is "event fires → mutations execute," which `on EventName` expresses directly without a new declaration concept.

### Alternative B: Event-bound default blocks (`on EventName default`)

```precept
on UpdateTotal default
    -> set Total = Quantity * UnitPrice
```

A "default" suffix would signal "this fires when no guarded row matches." The issue is that stateless precepts already have a model for this: the unguarded row (no `when` clause). A `default` suffix adds syntax for a concept already expressible. Rejected as redundant.

### Alternative C: Extend `from any on EventName` to stateless precepts

**Syntax:** Allow authors to write `from any on E → ...` in stateless precepts, where `any` is interpreted as "the only implicit state."

**Why rejected:** This collapses the stateful/stateless distinction syntactically. Authors who see `from any on E` cannot know whether the precept is stateful (with real states) or stateless (with a phantom `any`). The bare `on E` form makes the stateless model explicit at a glance. Syntactic clarity is worth the additional keyword form.

### Alternative D: Stateless events as a separate file form (`stateless precept`)

**Syntax:** `stateless precept Name` as a distinct declaration form.

**Why rejected:** The stateless/stateful distinction is already modeled by the presence or absence of state declarations (#22). Adding `stateless` as an explicit keyword is redundant — if no `state` declarations exist, the precept is stateless. A `stateless` keyword modifier would need to be enforced (error if you also declare states), creating a consistency requirement that the absence-of-states model already provides for free. Rejected as over-specified.

### Alternative E: No guardrail for pseudo-lifecycle (ship without C102)

**Why rejected:** The pseudo-lifecycle antipattern is the most likely misuse of stateless events — precisely because stateful precepts exist and authors will reach for state-in-a-field as a familiarity workaround. Shipping the feature without C102 would leave the most common misuse path unaddressed. C102 is relatively cheap to implement (pattern matching, not constraint solving) and high-value as a teaching diagnostic. The feature is incomplete without it.

---

## Philosophy Implications

### The mutation surface vs. the transition surface

`docs/philosophy.md` describes stateless precepts as having no "transition surface." This is accurate for the original #22 design — stateless precepts have fields, invariants, and editability rules, but no operation that fires action chains.

Stateless events change this. They add a **mutation surface**: a declared set of event-triggered action chains that mutate fields without routing by position. This is a philosophy expansion, not a philosophy contradiction.

The core guarantee is preserved intact:

> Invalid configurations are structurally impossible.

Stateless `on` handlers are evaluated through the same fire pipeline as stateful transition rows: action chain executes, invariants check, result is returned. A stateless mutation that would violate an invariant is rejected before the data change persists. The structural guarantee holds.

What changes is the taxonomy of surfaces:

| Surface | What it governs | Present in |
|---------|----------------|-----------|
| Transition surface | Where the entity is, what operations its position permits | Stateful precepts |
| Mutation surface | What data becomes when an operation occurs | Stateless precepts with `on` blocks |
| Constraint surface | What configurations are valid at any moment | Both |
| Editability surface | Which fields can be directly modified | Both |

The philosophy update required: add "mutation surface" to the surface taxonomy and clarify that stateless precepts may have either or both of the constraint and mutation surfaces.

### One-sentence rule for authors

> If knowing WHERE the entity is affects WHAT it can do next, you need states.  
> If the event just transforms data and the entity has no meaningful position, use stateless events.

This rule is the decision gate for `precept_language` pre-flight guidance and the `precept-authoring` SKILL.md Step 3 decision fork.

### The "prevention, not detection" principle

Stateless events do not weaken this principle. Prevention in Precept means: an operation that would produce an invalid data configuration is rejected before the configuration is committed. Stateless `on` blocks are evaluated through the same post-mutation invariant pipeline as stateful rows. The rejection happens at the same point in the fire pipeline. The guarantee is identical.

---

## Open Questions (Deferred)

The following were considered and deliberately deferred. They are not part of this proposal but are recorded here to prevent re-derivation.

1. **Stateless event hooks (`to` / `from` analogs in stateless context):** Stateful precepts support `to <State> -> ...` and `from <State> -> ...` entry/exit hooks. Stateless precepts could support per-event `before` / `after` hooks. Deferred — no corpus evidence of demand, adds complexity to the mutation surface. Revisit if sample analysis shows need.

2. **Stateless ordered mutation (multiple `on` blocks for the same event, all firing):** Today, first-match row selection applies. An "all-matching" model (fire every `on E` row whose guard passes) would support composable mutation patterns. Deferred — semantic implications for invariant checking order are non-trivial. First-match is safer for the initial surface.

3. **`precept_compile` suggestion block for C102:** Adding a structured suggestion to the compile output when C102 fires was considered and skipped. The diagnostic message content is sufficient. Revisit if user research shows authors are confused by the warning.

---

## Links

- GitHub issue: #NNN (coordinator fills in)
- `research/language/expressiveness/data-only-precepts-research.md` — philosophy foundation for stateless precepts
- `research/language/expressiveness/event-hooks.md` — XState/SCXML/Akka precedent survey for event-level action hooks
- `docs/PreceptLanguageDesign.md` § Stateless Precepts
- `docs/philosophy.md` § Entity Surfaces
