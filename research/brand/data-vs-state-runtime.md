# Data vs. State: A Runtime Engineer's Analysis

**Authored by:** George, Runtime Developer  
**Role:** Core engine implementation — I own `PreceptRuntime.cs`, the type checker, and the expression evaluator. I am the ground truth on what the engine actually does.  
**In response to:** Shane's framing — *"states are just vehicles to drive data through a workflow"*  
**Companion docs:**
- `data-vs-state-architecture.md` (Frank, language architecture)
- `data-vs-state-philosophy.md` (Peterman, brand)  
**Status:** Runtime research — implementation-level findings, not brand decisions

---

## Preface

Frank's architecture doc and Peterman's brand doc have already covered the *what* and *why* of the reframe question at a good level. My job here is to go one level lower: the actual code. I read `PreceptRuntime.cs` end-to-end for this writeup. What follows is what I found.

---

## 1. The Fire Pipeline — Stage by Stage

The `Fire` method in `PreceptRuntime.cs` runs six sequential stages. Here is what each stage does and which dimension of the contract it enforces:

| Stage | What it does | Enforces |
|-------|-------------|----------|
| 1 | Event asserts (`on X assert`) | **Data only** — validates event arg values before anything moves |
| 2 | Row selection via guard evaluation | **Data drives routing** — state narrows the candidate set, data determines which row wins |
| 3 | Exit actions (`from X → set ...`) | **State-keyed, data-mutating** — state activates the action; the action mutates data |
| 4 | Row mutations (`set Field = expr`) | **Data only** — pure field value writes |
| 5 | Entry actions (`to X → set ...`) | **State-keyed, data-mutating** — same as exit actions, opposite direction |
| 6 | Post-mutation validation: invariants + state asserts | **Data primary** — every expression evaluated here is a data expression; state activates which asserts run |

Observation: **not one stage evaluates a "state expression."** State appears as an *indexing key* (which rows? which actions? which asserts?) but it never appears as an evaluated expression. The engine has no mechanism to write `when CurrentState == "Approved"` — `currentState` is not in the expression evaluation context. I verified this in `BuildEvaluationData`: the evaluation dictionary receives instance fields, collection backing values, and event arg values. Current state is never injected.

**This is the most important implementation-level fact for copy purposes:** every expression the engine evaluates is a data expression. State governs which expressions fire, not what they say.

---

## 2. The Update Pipeline — What It Actually Does

`Update` is worth analyzing separately because it is the operation most directly relevant to "data independence."

```
Stage 1: Editability check — is this field in `in <State> edit` for current state?
Stage 2: Type check + unknown field validation
Stage 3: Atomic mutation on working copy (data)
Stage 4: Rules evaluation — invariants + `in` state asserts
Stage 5: Commit — instance with same CurrentState, new InstanceData
```

Two things stand out:

**State plays a gate role, not a result role.** `Update` reads `instance.CurrentState` for the editability check (stage 1) and to activate the right `in` asserts (stage 4), but the committed result — `instance with { InstanceData = DehydrateData(updatedData) }` — contains the *same* `CurrentState`. State is the input authorization mechanism. Data is the only thing that changes.

**The constraint check order reveals the priority.** Stage 4 runs invariants first, *then* state asserts. Invariants are global data rules — they fire regardless of state. State asserts fire second, scoped to the current state. The engine evaluates the global data contract before it evaluates the state-scoped contract. This ordering is not cosmetic; it reflects that invariants are the broader enforcement surface.

**On "data enforcement is independent of state":** partially true, but the framing needs precision. *Invariant enforcement* is completely state-independent — invariants fire on every operation regardless of current state. *State-assert enforcement* requires state as an activation key but still evaluates data expressions. And *editability enforcement* requires state entirely. So the correct statement is: **the data expressions are state-agnostic; the *activation* of some of those expressions depends on state position.**

---

## 3. The Constraint Types — A Complete Taxonomy

There are exactly four constraint kinds in the runtime. Here is each one, what it actually evaluates, and when it fires:

### `invariant <expr> because "reason"`

Declared in `PreceptModel.cs` as `PreceptInvariant`. Evaluated by `EvaluateInvariants()`, which receives only the data dictionary — no state parameter at all. Fires on every `Fire` and every `Update`, always. The state is never consulted during evaluation.

```precept
invariant RequestedAmount >= 0 because "Requested amount cannot be negative"
invariant ApprovedAmount <= RequestedAmount because "Approved amount cannot exceed the request"
```

These expressions reference only data fields. The engine does not check what state the instance is in. **This is a pure data guarantee with zero state dependency.**

### `on <Event> assert <expr> because "reason"`

Declared as `EventAssertion`. Evaluated by `EvaluateEventAssertions()`, which builds a context containing only event arg values — no instance data, no state. This runs before stage 2 (row selection), which means it fires before state even participates in the pipeline.

```precept
on Submit assert Amount > 0 because "Loan requests must be positive"
on Approve assert Amount > 0 because "Approved amounts must be positive"
```

**Pure data validation on event inputs. State is not consulted.**

### `in/to/from <State> assert <expr> because "reason"`

Declared as `StateAssertion`. Evaluated by `EvaluateStateAssertions(anchor, state, data)`, which uses `(anchor, state)` as a lookup key into `_stateAssertMap`, then evaluates the expression against the data dictionary. The expression itself is a data expression:

```precept
in Approved assert DocumentsVerified because "Approved loans must have verified documents"
in Funded assert ApprovedAmount > 0 because "Funded loans must have a positive approved amount"
```

`DocumentsVerified` is a boolean field. `ApprovedAmount` is a number field. State's role: it determines *whether this assert fires at all*. The assert itself always evaluates data. **State is the activation gate; data is what's checked.**

### Guards (`when <expr>` on transition rows)

Guards are stored as `PreceptExpression` on `PreceptTransitionRow`. Evaluated by `PreceptExpressionRuntimeEvaluator.Evaluate(row.WhenGuard, evaluationData)` where `evaluationData` contains instance fields and event args — no state. Guards can only read data.

```precept
from UnderReview on Approve when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2
```

This guard references five data fields. There is no `currentState` variable. The state (`UnderReview`) is the lookup key that retrieved this row in the first place, but it plays no role in guard evaluation.

**Routing within a state is 100% data-driven.**

---

## 4. Answering Shane's Questions Directly

### Q1: What percentage of constraint/invariant enforcement is about data values vs state transitions?

**100% of constraint expressions are data expressions.** There is no constraint expression type in the runtime that evaluates state. The four constraint kinds (invariant, event assert, state assert, guard) all evaluate expressions whose inputs are field values and event arg values exclusively.

State's role is always structural: which rows participate, which constraints activate, which fields are editable. It is never the thing being *evaluated*.

In quantitative terms from `loan-application.precept`: 5 invariants (pure data), 8 event asserts (pure data), 2 state asserts (data expressions, state-keyed). 15 constraint checks, 15 data expressions, 2 of which are state-activated. State participation as *activation condition*: 13%. State participation in *expression evaluation*: 0%.

### Q2: Is state required for the engine to function?

Yes, and in three specific ways that cannot be trivially replaced:

**Transition routing.** `_transitionRowMap` is keyed on `(State, Event)`. Without state, every event would require evaluating every row declared for that event across all states. First-match semantics would become undefined — rows from different states could conflict.

**Edit authorization.** `_editableFieldsByState` is a per-state set of mutable field names. `Update` checks this before allowing any mutation. Without state, you either allow all fields to be always editable (which destroys access control) or you have no `Update` operation at all.

**Conditional constraint activation.** `_stateAssertMap[(anchor, state)]` requires a state to look up the right assert set. Without state, you could only have global invariants — you'd lose the ability to declare constraints that apply specifically in certain lifecycle positions.

A data-only engine without states is architecturally possible, but it would be a weaker contract: flat (no lifecycle), unguarded for direct mutation, and incapable of state-conditional data requirements. It would be a validator, not a domain integrity engine.

### Q3: Does the `Update` operation support the claim that data enforcement is independent of state?

**Partially.** The claim is accurate for invariants (which fire in `Update` regardless of state) and for the type checking and field coercion in stages 2–3. These are entirely state-independent.

The claim is not accurate for editability (the field must be declared in `in <State> edit` for the current state) and for the state assert evaluation in stage 4 (which activates based on current state).

The most defensible statement about `Update`: "The engine enforces data constraints on every direct mutation, and the fields available for mutation are governed by the current lifecycle position." Both halves matter.

The most interesting implementation detail about `Update` for copy purposes: **it never changes `CurrentState`.** The committed result is `instance with { InstanceData = ... }`. State is input context for `Update`; it is not an output. This is the clearest demonstration in the codebase that data is the runtime's *value output*: Update is the purest form of "here is what the engine protects — your data values."

### Q4: When the engine rejects something, is it more often a state violation or a data violation?

**The substantive rejections are data. The structural rejections are state.**

There are two classes of rejection:

*Structural rejections* — the operation doesn't make sense given the current state topology:
- No rows defined for `(currentState, event)` → `Undefined`
- Unknown state or unknown event passed in → argument exceptions

These are state-structural. They're not rule violations; they're "you asked for something that doesn't exist."

*Rule violations* — a declared constraint failed:
- Event assert fires on bad input arg values → data
- Guard fails, fallback `reject` row fires → data-driven routing reached a `reject`
- Invariant fails post-mutation → data  
- State assert fails post-mutation → data expression (state-activated)

Every rule-based rejection evaluates a data expression. Even the `reject "reason"` outcomes are reached *because* the data-driven guards selected that row. State determined which rows were candidates; data determined which row ran.

**Are these separable?** Mostly yes, by category. Structural rejections are pure state; rule rejections are pure data (with state as activation context for state asserts). The only genuinely joint rejection is: "you're in state X, which activates constraint C, and your current field values violate C" — that requires both state (to activate C) and data (to evaluate C). But even there, the expression being violated is always a data expression.

### Q5: What is the most precise technical description of what the engine guarantees?

Across both `Fire` and `Update`, the engine guarantees:

> **Every committed instance configuration — the pair (currentState, fieldValues) — satisfies all applicable declared constraints.** Global invariants hold unconditionally. State-conditional assertions hold whenever the instance is in, entering, or leaving the named state. Event-conditional assertions hold for every event that begins processing. No invalid configuration can exist because the engine atomically validates before committing and discards the proposed world if any constraint fires.

Note the word "configuration." An instance is not just a state label. It is not just a bag of fields. It is a `(state, data)` pair, and validity is always a property of that pair taken together.

The compile-time layer adds a second guarantee: **reachability.** No instance can be in a state that wasn't declared reachable from the initial state via declared transitions. The runtime guarantee covers validity of the current configuration; the compile-time guarantee covers the integrity of the path that got there.

Together: the engine prevents both invalid configurations and invalid paths.

---

## 5. What "States Are Vehicles for Data" Gets Right and Wrong

### What it gets right

The primary *value output* of the engine is data. When `Fire` succeeds, the caller receives `result.UpdatedInstance`, and downstream code reads field values: `ApprovedAmount`, `DocumentsVerified`, `DecisionNote`. The state label on the result is consumed by the *engine* on the next call; the data is consumed by the *application*. In that sense, data is what the engine's work produces for the outside world.

Guards are pure data expressions. The routing decisions within a state — which branch executes, which outcome runs — are driven entirely by field values and event arg values. Data does the discriminating.

The `Update` operation, which has no analog in state machine theory, is a direct data-manipulation API. It exists because the engine treats data as a first-class managed resource, not just a side effect of transitions.

### What it gets wrong

"Vehicle" implies passive containment. States are not passive. They do three active things:

**They activate constraint sets.** `in Approved assert DocumentsVerified` doesn't just label a state; it creates a binding rule that must hold for any instance in that position. States are active rule-activators.

**They authorize data mutations.** The `in <State> edit` block controls which fields can be changed via `Update`. States are the access control boundary for direct data modification. A field that is editable in one state may be read-only in another. This is not passive.

**They provide compile-time structural guarantees that data expressions cannot.** Unreachable states, dead-end states, and transition type-checking are graph properties. You cannot replace them with data constraints.

The more accurate mechanical description: **state is the coordinate system; data is the substance at those coordinates; the engine ensures every (coordinate, substance) pair satisfies every applicable rule.**

---

## 6. The Claim That Is Most Accurate and Most Useful

For brand purposes, the question is: what is both true and resonant?

**"Invalid states are structurally impossible"** — true, but undersells. It covers only one dimension of the guarantee and uses mechanism vocabulary ("states") rather than outcome vocabulary.

**"Invalid data is structurally impossible"** — true for invariants; not complete for state-conditional constraints. A field value that is valid in one state may be invalid in another. The constraint is joint.

**"Invalid entity configurations are structurally impossible"** — the most precise single-sentence claim that covers the full guarantee. "Configuration" = (currentState, fieldValues). Nothing is omitted, nothing is overstated, and it doesn't require the reader to think in state machine terms.

From a runtime engineer's perspective, "configuration" is exactly the right noun. An instance's validity is always assessed as a whole — both dimensions, evaluated together, committed atomically or rejected as a unit.

---

## 7. Notes for Copy Writers

Things that would make a brand claim technically inaccurate:

1. **"The engine prevents invalid states."** The engine prevents invalid *configurations*. A state alone cannot be invalid in isolation — it's the (state, data) pair that is validated. A state with incorrect data is the real thing being prevented.

2. **"Data is protected regardless of state."** Not fully accurate. Invariants are state-agnostic; state-conditional assertions are not. The more accurate framing: "Invariants protect data globally; state-conditional rules protect data within specific lifecycle positions."

3. **"State is just a label."** Not accurate. State activates different constraint sets, authorizes different field mutations, and is required for transition routing. A state's name carries semantic meaning that the engine enforces concretely.

4. **"Guards are state-based."** No. Guards cannot reference current state. Guards are pure data expressions. State determines which rows are candidates; data determines which candidate wins.

5. **"Update bypasses the workflow."** No. `Update` requires the field to be declared editable in the current state. It is fully governed by the contract. It is a state-authorized direct data mutation.

6. **"The same state always has the same behavior."** True for structural routing (same rows, same constraint activations). Not true for guard evaluation — two instances in the same state with different field values will route differently when an event fires. Data drives within-state behavior.

The cleanest true statement that covers all of these edge cases: **"Precept ensures that every entity configuration — the combination of its lifecycle position and its field values — always satisfies every declared rule. Invalid configurations are structurally impossible."**

---

## Summary Table

| Claim | Technically accurate? | Notes |
|---|---|---|
| "Invalid states are structurally impossible" | Partially | States alone can't be invalid; configurations (state+data) are what's validated |
| "Invalid data is structurally impossible" | Mostly | True for invariants; state-conditional data rules are (state+data) joint |
| "Invalid configurations are structurally impossible" | Yes | Most precise claim covering both dimensions |
| "States are just vehicles for data" | No | States are active rule-activators, not passive containers |
| "States define the rules; data is what those rules protect" | Yes | This is architecturally precise and reflects how the code works |
| "Guards are data-driven" | Yes | 100% accurate. Guards cannot reference state at all |
| "All constraint expressions are data expressions" | Yes | No constraint kind evaluates state in an expression — only as an activation key |
| "Update demonstrates that data is the engine's primary output" | Yes | Update's result changes only data, never state |
| "State is the coordinate; data is the substance" | Yes | Architecturally precise; whether it's copy-friendly is Peterman's call |
