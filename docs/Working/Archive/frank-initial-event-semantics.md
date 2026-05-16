# Initial Event & Initial State — Semantics Explained

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-15T19:37:38-04:00
**Requested by:** Shane

---

## 1. What `initial` Means on a State vs. on an Event

They are **two orthogonal concepts** that happen to share a keyword.

| Surface | Grammar | Meaning |
|---------|---------|---------|
| `state Draft initial` | `StateEntry := Identifier ("initial")? StateModifier*` | This state is the **starting position** in the lifecycle graph. The entity begins here at construction. Exactly one state must carry this modifier in a stateful precept. |
| `event Create(...) initial` | `event Identifier ("(" ArgList ")")? ("initial")?` | This event is the **construction event** — the precept's constructor. The runtime's `Create(args)` fires this event atomically during entity creation. At most one event may carry this modifier. |

**Key insight:** `initial` on a state answers "where does the entity start?" — `initial` on an event answers "how does the entity get created?"

---

## 2. Interaction at Construction Time — The Construction Contract

When BOTH exist in a stateful precept, construction proceeds as:

1. **Build a hollow version:** defaults applied, initial state set, omitted fields structurally absent.
2. **Fire the initial event** with caller args through the standard pipeline — same guards, mutations, ensures, constraint checking as any other event.
3. **Return the outcome** — same `EventOutcome` verdict space (Transitioned, Applied, Rejected, ConstraintsFailed, Unmatched).

The initial event doesn't *place* the entity in the initial state — that's already done in step 1. The initial event *acts on* the entity that is already in the initial state. It's a mutation event that runs at construction, not a state-setting mechanism.

**The construction chain is:** Hollow version (with initial state set) → initial event fires → constraints evaluated → outcome returned.

---

## 3. `from State on Event` vs. Stateless `on Event -> ...` — Behavioral Difference

### Stateful pattern: `from <InitialState> on <InitialEvent>`
```precept
state Draft initial
event Create(Name as string) initial
from Draft on Create -> set Name = Create.Name -> no transition
```

This is a **transition row**. It supports:
- `when` guards for conditional routing
- Multiple rows for the same event (guarded discrimination)
- Outcomes: `transition X`, `no transition`, `reject "reason"`
- The entity is already IN `Draft` when this row evaluates

### Stateless pattern: `on Event -> ...`
```precept
event Start(Name as string) initial
on Start -> set Name = Start.Name
```

This is a **stateless event handler** (§ Stateless event hook). It:
- Does NOT support `when` guards
- Does NOT support an outcome keyword — there's no state to transition to/from
- Is mutually exclusive with having states (compile error if you mix them)
- Always executes the action chain; always results in `Applied` (or rejection/constraint failure)

### Answer to the direct question:
An `initial` event is NOT required to use `from <State> on <Event>` constructs. You can write `from Draft on Create -> ...` where `Create` is any event. What makes it the *construction* event is the `initial` modifier on the event declaration. The `from` row just says "when this event fires while in Draft, do this." You can have a non-initial event fire `from Draft on ...` too — it just won't be the construction event.

---

## 4. Edge Cases: States Without Initial Event, and Vice Versa

### Stateless precept WITH initial event
```precept
precept Counter
field count as integer
event Start initial
on Start -> set count = 0
```

- `Version.State` is `null` in the constructed version.
- Step 1 (initial state set) is **omitted** — no state to assign.
- State-entry semantics (`to State ensure`, `in State ensure`) do not fire — they're structurally absent.
- The initial event fires through the full pipeline. Same construction diagnostics apply.
- Valid and well-defined. The spec explicitly documents this as the natural extension.

### Stateful precept WITHOUT initial event
```precept
precept Widget
field Name as string default "unnamed"
state Draft initial terminal
```

- `Create()` is **parameterless** and always succeeds.
- The compiler guarantees (via `RequiredFieldsNeedInitialEvent`) that all fields have defaults or are optional. If they don't, it's a compile error.
- The hollow version (defaults + initial state) IS the final version. No event fires.
- `precept.InitialEvent` is `null` at the API level.
- `Create()` returns `EventOutcome.Applied` — always.

### Stateless precept WITHOUT initial event
- Same as above but `Version.State` is also `null`.
- All fields must have defaults or be optional (enforced by `RequiredFieldsNeedInitialEvent`).
- Valid configuration for simple data containers.

---

## 5. The Construction Chain When Both Are Present

Yes — in the canonical stateful case, BOTH a state and an event are marked `initial` and they refer to different aspects:

- The **initial state** is where the entity lives (its lifecycle position).
- The **initial event** is what fires to hydrate the entity's data at that position.

The chain:

```
Hollow version created
    ├── State := InitialState (e.g., "Draft")
    ├── Fields := defaults applied, omitted fields absent
    │
    ▼
Initial event fires (standard pipeline)
    ├── Guard evaluation (if transition rows have `when` clauses)
    ├── Matched row's action chain executes (set, add, etc.)
    ├── Outcome evaluates (no transition / transition X)
    ├── Entry ensures fire (if transitioning to a state)
    ├── Field constraints evaluated
    ├── Global rules evaluated
    │
    ▼
EventOutcome returned to caller
```

**Critical subtlety:** The initial event can **transition away** from the initial state at construction time! If the matched row says `-> transition Active`, the entity leaves `Draft` immediately. The outcome would be `EventOutcome.Transitioned` with the entity now in `Active`. This is by design — construction-time routing based on intake data.

---

## 6. Type Checker Diagnostics — The Enforcement Surface

The D130–D135 range and related diagnostics are NOT a contiguous "construction block." The construction enforcement lives primarily in:

| Diagnostic | Code | Fires When |
|------------|------|------------|
| `MultipleInitialStates` | (type stage) | More than one state has `initial` |
| `NoInitialState` | (type stage) | Stateful precept, no state marked `initial` |
| `RequiredFieldsNeedInitialEvent` | D93 | Required fields exist (no default, not optional, not computed) but no event is marked `initial` |
| `InitialEventMissingAssignments` | D94 | Initial event's construction paths don't assign all required fields that are non-omit at construction |
| `UninitializedFieldReadInInitialAssignment` | D142 | Self-referential read: `set X = X + 1` where X has no default and no prior assignment in the chain |
| `UninitializedCrossFieldReadInInitialAssignment` | D144 | Cross-field read: `set Y = X * 2` where X has no default and isn't assigned earlier in the chain |
| `MaterializedFieldSelfReference` | D143 | Transition materializes a field from `omit` → present, but reads it before any value exists |
| `RequiredFieldUnassignedOnEntry` | D132 | Transition moves a required field from `omit` to non-omit without assigning it |

**D130–D131** are about omitted-field access (reading/writing omit fields in state-anchored contexts) — related but not construction-specific.

### What D94 actually checks:
For each row that matches the initial event from an initial state (or wildcard `from any`), it inspects the action chain. If ANY row path fails to assign a required field, D94 fires. It checks **per-row**, not just "at least one row assigns it."

### Stateless construction check:
D93/D94/D142/D144 all apply to stateless precepts too. The validator inspects stateless initial handlers (`on Event -> ...`) and `from any` wildcard rows identically to explicit initial-state rows.

---

## 7. Subtleties and Gotchas

### 7a. Construction can transition away from the initial state
Most people assume the entity stays in `Draft` after construction. Wrong. If the initial event's matched row says `-> transition Active`, the entity leaves immediately. `EventOutcome.Transitioned` is a valid construction result.

### 7b. D94 checks per-row, not aggregate
If you have two guarded rows for the initial event and one of them doesn't assign a required field, D94 fires — even if the other row does. Every construction path must be complete.

### 7c. `from any on InitialEvent` counts as a construction path
Wildcard `from any` rows are included in the construction guarantee check. If you rely on `from any on Start -> ...` as your construction path, it must assign required fields.

### 7d. Omitted fields in the initial state are NOT required at construction
If `in Draft omit Name` exists and Draft is the initial state, then `Name` is structurally absent at construction — D94 does NOT require the initial event to set it. The field materializes later when a transition moves to a state where it's not omitted (enforced by D132 at that point).

### 7e. No initial event ≠ broken — it means "all fields self-hydrate"
A stateful precept with no initial event is perfectly valid IF every field has a default, is optional, or is computed. The compiler enforces this. `Create()` becomes parameterless and infallible.

### 7f. Event handlers cannot coexist with states
`on Event -> actions` (stateless handler syntax) is a compile error in a stateful precept. You MUST use `from State on Event -> ...` transition rows. These are structurally incompatible forms — you can't mix them.

### 7g. `Unmatched` is a valid construction outcome
If all initial-event rows have `when` guards and none match the provided args, construction returns `EventOutcome.Unmatched`. The entity is not created. This is intentional — guarded intake discrimination.

### 7h. Entry ensures on the initial state fire at construction
`to Draft ensure CreditScore >= 300` fires when the entity ENTERS Draft — which includes construction (the entity enters its initial state at construction time). These are construction-time intake invariants with no special syntax.

### 7i. Construction self-read ordering matters
D142 and D144 check **sequential ordering within the action chain**. `set count = 0 -> set count = count + 1` is fine (count is initialized by the first action). `set count = count + 1` alone is the error. The chain is sequential.

---

## Summary Mental Model

```
┌─────────────────────────────────────────────────┐
│  "initial" on STATE = where entity starts       │
│  "initial" on EVENT = how entity gets created   │
│                                                 │
│  They compose: state sets position,             │
│  event hydrates data AT that position.          │
│                                                 │
│  Either can exist without the other.            │
│  Both absent = defaults-only, parameterless.    │
│  State absent + event present = stateless ctor. │
│  State present + event absent = all-defaults.   │
│  Both present = the canonical construction.     │
└─────────────────────────────────────────────────┘
```

No inconsistencies found between the spec (`precept-language-spec.md`), the runtime API doc (`runtime-api.md`), and the diagnostic implementations in `Diagnostics.cs`. The code enforces exactly what the spec describes.
