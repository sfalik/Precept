# Null Reduction in Precept — Design Proposal

> **Author**: Frank (Lead/Architect & Language Designer)
> **Requested by**: Shane (Product Owner)
> **Type**: Research proposal — design options and decision points. NOT an implementation spec.
> **Date**: 2026-04-13
> **Grounding research**:
> - [null-elimination-research.md](null-elimination-research.md) — external survey of null-free languages, DSLs, databases, and form systems
> - [nullable-computed-fields-research.md](nullable-computed-fields-research.md) — computed field propagation analysis
> **Governing document**: [docs/philosophy.md](/docs/philosophy.md)

---

## 1. Preamble

This proposal presents a unified design for reducing Precept's reliance on `nullable` — not by replacing it with `Option<T>` types or monadic wrappers (the research ruled those out), but by recognizing that **the state machine is already Precept's optionality mechanism** and making that relationship explicit in the language surface.

The proposal covers three interconnected changes:

1. **Replace null vocabulary with domain language** — rename `nullable` to `optional`, eliminate `null` as a keyword, introduce `none` (absence value), `is present`/`is absent` (presence operators), and `clear` (action keyword counterpart to `set`).
2. **State-scoped field visibility** — fields that only exist in the states where they're meaningful, eliminating ~65% of current `nullable` usage.
3. **Absence-eliminates model for computed fields** — when a computed field's inputs are structurally absent, the computed field is also absent.

Each section presents concrete syntax options, decision points with tradeoffs, and recommendations grounded in the research base and Precept's philosophy.

### Philosophy Principles Governing This Proposal

Every design decision in this proposal is evaluated against these principles from [docs/philosophy.md](/docs/philosophy.md):

| Principle | Short form | Application here |
|---|---|---|
| **Prevention, not detection** | Invalid configurations are structurally impossible | Absent fields cannot be misused — they don't exist in the wrong state |
| **One file, complete rules** | Every rule lives in the `.precept` definition | Field visibility is declared, not scattered across service code |
| **English-ish but not English** | Keywords read naturally but don't attempt sentences | `optional` reads better than `nullable`; visibility syntax should feel like English |
| **Minimal ceremony** | No unnecessary syntax burden | Implicit visibility has zero ceremony; explicit `visible in` adds a clause |
| **Full inspectability** | Preview every action and its outcome | Inspect must show which fields exist in each state |
| **Deterministic** | Same definition, same data, same outcome | Field visibility is fully determined by the state graph |
| **Compile-time structural checking** | Catch mistakes before runtime | Using an absent field in the wrong state is a compile error |

---

## 2. The Precept Way of Thinking About Absence

### Null is not just "no value" — it's an overloaded concept

The [null elimination research](null-elimination-research.md) identified five distinct categories of `nullable` usage across all 25 sample files (94 total usages):

| Category | Example | Prevalence | True semantics |
|---|---|---|---|
| 1. "Not yet provided" | `ApplicantName` before `Submit` | ~30% | Field doesn't exist yet in this lifecycle phase |
| 2. "Set later by lifecycle event" | `AssignedAgent` before `Assign` | ~25% | Field is introduced by a future event |
| 3. "Optional event argument" | `Note as string nullable default null` | ~20% | Caller may or may not provide this |
| 4. "Genuinely optional data" | `Nickname` in CustomerProfile | ~15% | May legitimately never have a value |
| 5. "Phase-dependent data" | `CancellationReason` before cancellation | ~10% | Concept doesn't apply in this state |

Categories 1, 2, and 5 — representing **~65% of all nullable usages** — are not expressing "this field might have no value." They're expressing **"this field doesn't belong to this state."** The difference matters enormously:

- A `null` `AssignedAgent` in the `Open` state of a helpdesk ticket is not "an agent whose name happens to be null." It's "the concept of an assigned agent does not exist yet."
- A `null` `CancellationReason` in the `Active` state of a subscription is not "a cancellation reason that happens to be empty." It's "there is no cancellation — the concept doesn't apply."

### The state machine IS the optionality mechanism

This is the central insight: **Precept's state machine already tracks which phase the entity is in. That phase determines which data exists.** The language should express this directly, instead of using `nullable` as a proxy for "not relevant in this state."

From the philosophy: *"States are the structural mechanism that makes data protection lifecycle-aware."* The data that needs protection changes by state. A field that doesn't exist in a state cannot be misused in that state — this is **prevention, not detection**.

The Elm Guide's "Avoiding Overuse of Maybe" section validates this directly: instead of sprinkling `Maybe` across a model, use custom types to represent phases. Precept's states are those phases.

Datomic's model validates it from the database side: an entity either has an attribute or doesn't. There is no null — just presence or absence. Rich Hickey's design treats this as a first-class concept, not an edge case.

Event sourcing validates it from the architecture side: after `OrderCreated`, only `OrderId` and `CustomerId` exist. `ShippingAddress` doesn't exist yet because no `AddressProvided` event has occurred.

### What remains `optional`

Categories 3 and 4 (~35% of usages) represent genuine optionality:

- **Optional event arguments**: The caller decides whether to provide a `Note`. This is call-site optionality — the field isn't "absent from the entity," it's "the caller chose not to supply it."
- **Genuinely optional data**: Not every customer has a `Nickname`. No lifecycle event will necessarily provide one. This field may legitimately never have a value across the entity's entire life.

These are irreducible. They need a keyword. The question is whether that keyword should be `nullable` or `optional`.

---

## 3. Proposal: Replace Null Vocabulary with Domain Language

Precept currently uses three implementation-derived tokens: `nullable` (type modifier), `null` (value literal), and null-comparison operators (`== null`, `!= null`). All three come from C#, SQL, and database schemas — they describe type-system mechanics, not domain concepts. This section proposes replacing the entire null vocabulary with English-ish alternatives that express intent rather than implementation.

### Why the change

1. **`nullable` is implementation language; `optional` is domain language.** "Field Nickname as string optional" reads as "the Nickname field is a string and it's optional." Compare with "field Nickname as string nullable" — jargon that requires explaining what null is. The word `optional` is instantly understood by domain experts. CUE uses `?` (optional), Dhall uses `Optional`, Protobuf uses `optional` — the industry word for "might not have a value" is overwhelmingly "optional."

2. **`null` is not a domain concept — it's an implementation artifact.** Tony Hoare called it his "billion dollar mistake." The domain concepts are:
   - **"No value was provided"** → `none`
   - **"Does this field have a value?"** → `is present` / `is absent`
   - **"Remove the value from this field"** → `clear`

3. **Precept's philosophy demands English-ish, not implementation-ish** — Principle #2: keywords read naturally but don't attempt full sentences. Principle #12: AI is a first-class consumer, so domain meaning matters more than implementation convention.

4. **Precedent from null-free languages**: Rust (`Option<T>`), Haskell (`Maybe`), Elm (`Maybe`), Datomic (no null — absence as non-assertion) all demonstrate that languages work better when "no value" has domain-appropriate vocabulary rather than a universal `null` token.

### The complete replacement table

| Current | Replacement | Position | English reading |
|---|---|---|---|
| `nullable` | `optional` | Field/arg type modifier | "this field is optional" |
| `default null` | `default none` | Field/arg default value | "the default is: no value" |
| `when Field != null` | `when Field is present` | Guard expression | "when the field has a value" |
| `when Field == null` | `when Field is absent` | Guard expression | "when the field has no value" |
| `assert X != null` | `assert X is present` | Assertion | "X must have a value" |
| `assert X == null` | `assert X is absent` | Assertion | "X must have no value" |
| `set Field = null` | `clear Field` | Transition action | "remove the field's value" |

### Syntax — full before/after

**Before:**
```precept
field Nickname as string nullable
field Note as string nullable default null

event Approve with Note as string nullable default null

from Review on Approve when DecisionNote != null -> ...
in Approved assert DecisionNote != null because "Decision must be documented"
on Reopen assert Note == null or Note != "" because "A supplied note cannot be blank"

from Approved on Revoke -> set ApprovedBy = null -> transition Draft
```

**After:**
```precept
field Nickname as string optional
field Note as string optional default none

event Approve with Note as string optional default none

from Review on Approve when DecisionNote is present -> ...
in Approved assert DecisionNote is present because "Decision must be documented"
on Reopen assert Note is absent or Note != "" because "A supplied note cannot be blank"

from Approved on Revoke -> clear ApprovedBy -> transition Draft
```

### `clear` — the counterpart to `set`

Precept's action keywords are verbs: `set`, `transition`, `reject`, `enqueue`, `dequeue`, `add`, `remove`. `clear` fits naturally in this family as the counterpart to `set`:

```precept
# Setting a value
from Draft on Approve -> set ApprovedBy = Approve.Reviewer -> transition Approved

# Removing a value
from Approved on Revoke -> clear ApprovedBy -> transition Draft
```

The `set`/`clear` pairing mirrors everyday English: "set the alarm" / "clear the alarm." It's immediately intuitive without explanation.

Why `clear` as a standalone action keyword rather than `set Field = none`:
- **Distinct intent**: `set` means "assign a value," `clear` means "remove a value." These are semantically different operations, not the same operation with a special value.
- **Datomic precedent**: Datomic uses **retraction** as a distinct operation from assertion. Retracting an attribute is not "asserting null" — it's removing the attribute entirely. `clear` captures this same distinction.
- **No non-verb action forms**: `set Field = none` would be the only action where the RHS is a keyword rather than an expression. `clear Field` keeps the action vocabulary consistently verb-driven.

### `none` — the absence value

`none` replaces `null` in `default` clauses to express "the default is: no value." It is a keyword restricted to `default` clauses — it does not participate in arbitrary expressions or comparisons.

**Why keyword, not general-purpose literal?** A literal would imply it can appear anywhere a value can — in `set` RHS expressions, in comparisons, in computed field formulas. Restricting `none` to `default` clauses keeps the language surface clean: `default none` to declare absence, `is present`/`is absent` to test for it, `clear` to produce it. Three mechanisms, each with one job.

**Implicit default for optional fields**: `optional` without an explicit `default` clause implies `default none`. The explicit form `optional default none` is also accepted for readability. This avoids forcing ceremony while keeping the option for authors who prefer explicitness.

### `is present` / `is absent` — presence operators

These operators replace null comparisons and serve double duty:

1. **Optional value checking** — for `optional` fields, `is present` tests whether the field has a value.
2. **Visibility checking** — for state-scoped fields in `from any` rows (Section 4), `is present` tests whether the field exists in the current state.

**Type narrowing**: `when Field is present` narrows the field's type in the row body, just as `when Field != null` does today. After the presence check, the field is known to have a concrete value and can be used without further guards.

### Interaction with the C# runtime API

At the C# API boundary, `none` maps to `null`:

| DSL surface | C# `InstanceData` representation |
|---|---|
| `default none` | `null` value in the dictionary |
| `is present` | `value != null` (or key exists with non-null value) |
| `is absent` | `value == null` (or key missing from dictionary) |
| `clear Field` | Sets the dictionary value to `null` |

The DSL is null-free; the runtime uses `null` internally because that's idiomatic C#. The boundary translation is transparent to DSL authors — the same pattern as Elm's `Nothing` mapping to JavaScript's `null/undefined`.

### Migration

Soft deprecation (same approach for `nullable` and `null`):
- Both old and new forms are accepted. Old forms emit deprecation warnings.
- After the deprecation period, `nullable` and `null` become parse errors.
- A codemod script handles the mechanical transformation: `nullable` → `optional`, `default null` → `default none`, `!= null` → `is present`, `== null` → `is absent`, `set Field = null` → `clear Field`.

### Decision points

| # | Decision | Options | Recommendation | Key Tradeoff |
|---|---|---|---|---|
| 1 | Migration approach | A. Hard break / B. Soft deprecation / C. Permanent alias | **B. Soft deprecation** | Migration effort vs. permanent synonym baggage |
| 2 | `none` as keyword vs. implicit | Explicit `default none` required / implicit when `optional` has no `default` / both | **Both (implicit allowed, explicit accepted)** | Readability vs. ceremony |
| 3 | `clear` syntax | Standalone action keyword / `set Field = none` / both | **Standalone action keyword** | Semantic distinction vs. language surface size |
| 4 | `none` in comparisons | Allowed (`when Field == none`) / restricted to `default` only | **Restricted to `default` only** | Use `is present`/`is absent` for testing — one way to do it |

---

## 4. Proposal: State-Scoped Field Visibility

This is the largest and most impactful change. It addresses 65% of current `nullable` usage by making field presence a function of state position.

### The problem

Consider `it-helpdesk-ticket.precept`:

```precept
field TicketTitle as string nullable
field AssignedAgent as string nullable
field LastQueuedAgent as string nullable
field ResolutionNote as string nullable
```

All four are `nullable` — but for different reasons:
- `TicketTitle` is null in `New` because `Triage` hasn't fired yet.
- `AssignedAgent` is null in `New` because `Assign` hasn't fired yet.
- `ResolutionNote` is null until `Resolve` fires.
- `LastQueuedAgent` is a helper field populated during `Assign`.

None of these are "genuinely optional." They're all "this data doesn't exist in this lifecycle phase." The `nullable` keyword is doing the wrong job — it's providing a type-system escape hatch for what is really a lifecycle invariant.

The consequence: guards and assertions must redundantly check for null. `in Assigned assert AssignedAgent != null because "Assigned tickets must name an agent"` is the author manually re-stating what the state machine already knows — you can't be in `Assigned` without having gone through the `Assign` event, which sets `AssignedAgent`.

### Three syntax options

#### Option A: Implicit Visibility from `set` Actions

The runtime infers which states a field is visible in based on which events set it. No new syntax.

**How it works**: The compiler performs reachability analysis on the state graph. If field `X` is only set by events reachable from certain states, the field is "introduced" at the point where the `set` first fires and remains present in all subsequent reachable states. Before that point, the field doesn't exist.

A field is **visible in state S** if there exists a reachable path from the initial state to S that passes through a transition containing `set X = ...` (or the field has a non-null default).

**Rewritten `it-helpdesk-ticket.precept` (relevant sections):**

```precept
precept ItHelpdeskTicket

field TicketTitle as string default ""
field Severity as number default 3
field AssignedAgent as string default ""
field LastQueuedAgent as string default ""
field ResolutionNote as string default ""
field ReopenCount as integer default 0
field Priority as choice("Low","Medium","High","Critical") default "Low"

field AgentQueue as queue of string

# TicketTitle: invisible in New (until Triage), visible in all states after Triage
# AssignedAgent: invisible in New (until Assign), visible in Assigned+
# ResolutionNote: invisible until Resolve, visible in Resolved+

# No 'nullable' on any of these fields.
# The compiler infers visibility from the set actions in transitions.

state New initial
state Assigned
state WaitingOnCustomer
state Resolved
state Closed

# This assert is now REDUNDANT — the compiler knows AssignedAgent
# doesn't exist in New, so you can't be in Assigned without it.
# in Assigned assert AssignedAgent != null because "Assigned tickets must name an agent"

from New on Triage -> set TicketTitle = Triage.Title -> set Severity = Triage.Level -> no transition
from New on Assign when AgentQueue.count > 0
  -> set LastQueuedAgent = AgentQueue.peek
  -> dequeue AgentQueue
  -> set AssignedAgent = LastQueuedAgent
  -> transition Assigned
```

**Interaction with events, guards, invariants, computed fields:**
- A guard referencing `AssignedAgent` in state `New` is a compile error — the field doesn't exist there.
- An invariant referencing `AssignedAgent` needs a `when` guard scoping it to states where the field exists, or the compiler infers the scope automatically.
- Computed fields: if an input is absent in a state, the computed field is also absent in that state (Section 5).

**How InstanceData changes:**
- In state `New`: `InstanceData` does not contain `AssignedAgent`, `ResolutionNote`, etc. They are absent, not null.
- In state `Assigned`: `InstanceData` contains `AssignedAgent` with its string value.
- This maps to the C# `Dictionary<string, object?>` by simply not having the key.

**How CreateInstance / Update / Inspect / Fire behave:**
- `CreateInstance()`: only fields visible in the initial state appear in the data dictionary.
- `Inspect(instance)`: reports which fields exist in the current state, plus what would become visible/absent in each transition target.
- `Fire(instance, event)`: after mutations, fields introduced by `set` actions become visible; the new state's field set is the result.
- `Update(instance, fields)`: can only update fields visible in the current state.

**Pros:**
- Zero new syntax. Existing `.precept` files just drop `nullable` from lifecycle-dependent fields.
- The compiler already knows the state graph and the `set` actions. Pure analysis, no new declarations.
- Aligns perfectly with event sourcing's progressive state building model.
- Eliminates redundant `in Assigned assert AssignedAgent != null` patterns.

**Cons:**
- **Implicit behavior is the biggest risk.** Authors don't see which fields are visible in which states — they must infer it from the transition graph. This violates Principle #4 (locality of reference) and arguably Principle #8 (sound, compile-time-first).
- Fields set on multiple diverging paths create ambiguity. If `FieldX` is set in `from Draft on Approve` and also in `from Draft on Expedite`, is it visible in both targets?
- Re-entrance (e.g., `Reopen` cycling back to `New`) creates visibility questions. If `Reopen` transitions to `New`, does `ResolutionNote` disappear?
- A field set via `no transition` doesn't change state — when does it become visible?
- **Hardest to explain to authors.** The behavior emerges from analysis, not declaration.
- Branch analysis is potentially complex. Fields set conditionally (`when` guards) may only be visible on certain paths.

**Philosophy alignment: MEDIUM.** Zero ceremony (Principle #3) but weak on inspectability (Principle #4 — you can't see the visibility rules by reading the field declaration) and potentially fragile under complex state graphs.

---

#### Option B: Explicit `visible in` Clause

Authors declare which states a field is visible in, directly on the field declaration.

**Syntax:**

```precept
field <Name> as <Type> visible in <State1>, <State2>, ... [default <value>]
```

A field with `visible in` is absent in all unlisted states. The compiler enforces that `set` actions only target the field in transitions leading to (or within) a visible state.

**Rewritten `it-helpdesk-ticket.precept` (relevant sections):**

```precept
precept ItHelpdeskTicket

field TicketTitle as string visible in New, Assigned, WaitingOnCustomer, Resolved, Closed default ""
field Severity as number default 3
field AssignedAgent as string visible in Assigned, WaitingOnCustomer, Resolved, Closed default ""
field LastQueuedAgent as string visible in Assigned, WaitingOnCustomer, Resolved, Closed default ""
field ResolutionNote as string visible in Resolved, Closed default ""
field ReopenCount as integer default 0
field Priority as choice("Low","Medium","High","Critical") default "Low"

field AgentQueue as queue of string
```

Wait — listing every state is verbose. We'd want a shorthand:

**Shorthand: `visible after <Event>`:**

```precept
field TicketTitle as string visible after Triage default ""
field AssignedAgent as string visible after Assign default ""
field ResolutionNote as string visible after Resolve default ""
```

Or **`visible from <State>`** (inclusive):

```precept
field TicketTitle as string visible from New default ""
field AssignedAgent as string visible from Assigned default ""
field ResolutionNote as string visible from Resolved default ""
```

Let me present the cleanest form — `visible in` with explicit state list, plus `visible after` as sugar:

**Rewritten `it-helpdesk-ticket.precept` (clean version):**

```precept
precept ItHelpdeskTicket

# Always-present fields (no visibility clause = visible everywhere)
field Severity as number default 3
field ReopenCount as integer default 0
field Priority as choice("Low","Medium","High","Critical") default "Low"
field AgentQueue as queue of string

# Lifecycle-scoped fields
field TicketTitle as string visible after Triage default ""
field AssignedAgent as string visible after Assign default ""
field LastQueuedAgent as string visible after Assign default ""
field ResolutionNote as string visible after Resolve default ""

state New initial
state Assigned
state WaitingOnCustomer
state Resolved
state Closed
```

**Interaction with events, guards, invariants, computed fields:**
- Guards referencing a field not visible in the current state are compile errors.
- Invariants referencing a field with `visible in/after` are automatically scoped to states where the field is visible. No `when` guard needed.
- `in Assigned assert AssignedAgent != "" because "..."` is valid — `AssignedAgent` is visible in `Assigned`.
- `in New assert AssignedAgent != null` is a compile error — `AssignedAgent` doesn't exist in `New`.

**How InstanceData changes:** Same as Option A — fields absent in the current state are not keys in the dictionary.

**How CreateInstance / Update / Inspect / Fire behave:** Same as Option A.

**Pros:**
- **Explicit and readable.** The field declaration tells you where the field exists. Principle #4 (locality of reference) is satisfied — the visibility rule is right next to the field.
- **Compiler can validate `visible after`** by checking that the named event's transitions all lead to states where a `set` for this field occurs.
- `visible after <Event>` reads naturally: "this field is visible after the Triage event." English-ish (Principle #2).
- No ambiguity about branching paths — the author declares the intent.
- The `visible in` form handles complex graphs where `visible after` is insufficient (field visible in non-contiguous states).

**Cons:**
- New syntax surface. A new keyword (`visible`) and two preposition forms (`in`, `after`).
- `visible in` with explicit state lists can be verbose for precepts with many states.
- `visible after <Event>` is syntactic sugar that requires reachability analysis to expand — similar complexity to Option A, but triggered by an explicit declaration rather than inferred.
- Reopen/cycle semantics must be specified: if `Reopen` sends back to `New`, does a `visible after Assign` field disappear? (Decision point below.)
- Interaction between `visible` and `optional` needs clarity — can a field be both `visible in Assigned` and `optional`?

**Philosophy alignment: HIGH.** Explicit, readable, inspectable, compile-time-verifiable. Adds a concept to the language but eliminates a larger source of confusion (nullable-as-lifecycle-proxy). Strongest preservation of Principle #4 (locality) and Principle #8 (compile-time-first).

---

#### Option C: State Blocks with Field Declarations

Fields are declared inside state blocks, scoping them to that state. Fields declared outside any state block are global.

**Syntax:**

```precept
state New initial
  field Priority as choice("Low","Medium","High","Critical") default "Low"

state Assigned
  field AssignedAgent as string default ""
  field LastQueuedAgent as string default ""

state Resolved
  field ResolutionNote as string default ""
```

Fields declared inside a state block are visible in that state and all states reachable from it (or only in that state — design choice).

**Rewritten `it-helpdesk-ticket.precept` (relevant sections):**

```precept
precept ItHelpdeskTicket

# Global fields — visible in all states
field Severity as number default 3
field ReopenCount as integer default 0
field AgentQueue as queue of string

state New initial
  field Priority as choice("Low","Medium","High","Critical") default "Low"

state Assigned
  field TicketTitle as string default ""
  field AssignedAgent as string default ""
  field LastQueuedAgent as string default ""

state WaitingOnCustomer

state Resolved
  field ResolutionNote as string default ""

state Closed
```

**But this creates problems immediately:**
- `TicketTitle` is set by `Triage` (a `no transition` in `New`), but we declared it in `Assigned`. Where should it live?
- Does `AssignedAgent` carry forward from `Assigned` to `WaitingOnCustomer` to `Resolved`? The block structure doesn't say.
- If `Priority` is declared in `New` but the ticket moves to `Assigned`, does `Priority` disappear?

The block structure requires **inheritance rules** — which states inherit which fields from which other states. This is a significant new concept.

**Interaction with events, guards, invariants, computed fields:**
- Same compile-time enforcement as Options A and B, but the scoping rules are more complex.
- Invariants inside a state block are state-scoped (already how `in <State> assert` works). But field declarations inside state blocks is a new structural concept.

**How InstanceData changes:** Same as Options A and B at the runtime level — the question is what the DSL surface looks like.

**Pros:**
- Visually groups fields with their states. Strong visual locality.
- Familiar to developers who've used class inheritance or nested scopes.

**Cons:**
- **Fundamentally conflicts with Principle #9 (no indentation-based structure).** State blocks require indentation or explicit end markers (`end state`), which the language design explicitly rejected. The current grammar is "flat, keyword-led statements" — this would be the first nested structure.
- **Conflicts with the Superpower parser design.** The grammar was "shaped in part around the March 2026 move to a Superpower-based parser. Flat, keyword-led statements and whitespace-insensitive structure were chosen deliberately."
- Field inheritance rules are complex and under-specified. Forward propagation? Backward propagation? Cross-branch inheritance?
- `TicketTitle` being set by a `no transition` event in `New` but conceptually belonging to `Assigned` has no clean representation.
- Creates pressure for other nested constructs (event blocks, invariant blocks), leading to a fundamentally different grammar.
- **Hardest to implement.** Parser, language server, and all tooling would need to understand nested scope.

**Philosophy alignment: LOW.** Violates locked grammar decisions (flat keyword-led statements, no indentation), introduces nested scoping that the parser architecture was designed to avoid, and adds inheritance complexity that Precept's minimal-ceremony principle resists.

---

### Visibility and Cycles/Re-entrance — Decision Point

When a precept's state graph contains cycles (e.g., `Reopen` sends from `Resolved` back to `New`), what happens to fields that were introduced in intermediate states?

| Option | Behavior | Example | Tradeoff |
|---|---|---|---|
| **Sticky visibility** | Once a field becomes visible, it remains visible even if the entity cycles back to an earlier state. | After `Reopen` to `New`, `AssignedAgent` retains its value. | Simpler mental model. But now `New` has fields it didn't have on first entry — the data shape of `New` depends on history. |
| **State-determined visibility** | The field set is always exactly what the current state declares. Cycling back to `New` removes fields not declared for `New`. | After `Reopen` to `New`, `AssignedAgent` is absent again. | Cleaner semantics — state fully determines data shape. But data is lost on re-entrance, which may surprise authors. |
| **Author-declared** | Combined with Option B, the author explicitly states visibility. `visible in New, Assigned, ...` or `visible after Assign` with a defined cycle-back policy. | The `visible` clause controls whether the field persists through cycles. | Most flexible. Most explicit. Requires the author to think about cycles. |

**Recommendation: Sticky visibility as default, state-determined as opt-in**

Rationale: Most re-entrance patterns (reopen, retry, re-review) intend to preserve data accumulated so far. Wiping `AssignedAgent` when a ticket is reopened would force the author to re-set it in the `Reopen` transition — adding ceremony for no domain benefit. The sticky model matches event sourcing semantics (events are append-only; you don't un-learn that an agent was assigned).

For the rare case where data should be explicitly cleared on re-entrance, the existing `set Field = null` (or with the rename, combined with `optional`) handles it. Under Option B, `visible in` with explicit state lists gives the author full control.

---

### Recommendation for State-Scoped Visibility

**Option B (explicit `visible in` / `visible after` clause) is the recommendation.**

The case:
1. **Explicit over implicit** — Options A's inference is powerful but invisible. Option B's declarations are readable and verifiable. This is Precept's philosophy: "The engine exposes the complete reasoning."
2. **Flat grammar preserved** — Option C requires nested blocks, violating locked grammar decisions. Option B adds a modifier to field declarations, fitting cleanly into the existing flat structure.
3. **`visible after <Event>` is the sweet spot** — it reads naturally ("this field is visible after the Triage event"), it's concise, and it maps directly to the lifecycle semantics. The long-form `visible in <State1>, <State2>, ...` handles complex cases.
4. **Compile-time verifiable** — the compiler can check that every `set` targeting a `visible after X` field occurs in transitions that fire after event `X`. Structural prevention.
5. **Inspectable** — `precept_inspect` can report per-state field sets directly, because they're declared, not inferred.

---

## 5. Proposal: Computed Fields and Absence

### The "Absence Eliminates" Model

If a computed field's inputs include a field that is structurally absent in the current state (not `optional`-with-no-value, but absent because the state doesn't include them via visibility rules), the computed field is also absent in that state.

This follows the Datalog/Datomic model from the research: derived values either exist fully or don't exist at all. There is no "null computed result" — the computation simply doesn't apply.

**Example:**

```precept
field RequestedAmount as number default 0
field ApprovedAmount as number visible after Approve default 0
field ApprovalRatio as number -> ApprovedAmount / RequestedAmount
```

- In `Draft` state: `ApprovedAmount` is absent → `ApprovalRatio` is also absent. No computation occurs.
- In `Approved` state: `ApprovedAmount` is present → `ApprovalRatio` is computed normally.

This preserves C81 ("computed fields always produce a value") within its visibility scope. The constraint becomes: **when all inputs are present, the computed field produces a value.** When any input is absent, the computed field is absent. The formula's guarantee is not weakened — its scope is narrowed.

### Interaction with C83 (no nullable refs in computed expressions)

C83 currently forbids referencing nullable fields in computed expressions. Under this proposal:

- **Absent fields (via `visible in/after`) are distinct from `optional` fields.** A `visible after Assign` field is not nullable — it has a concrete default value when visible, and doesn't exist when invisible.
- C83 continues to forbid `optional` (formerly `nullable`) field references in computed expressions. The motivation is unchanged: a computed field should not produce null.
- The absence-eliminates rule is not about null — it's about the field not existing at all. The computed field's formula never executes with null inputs; it simply doesn't execute.

### Decision Point: Automatic or Opt-In?

| Option | Behavior | Pros | Cons |
|---|---|---|---|
| **Automatic** | If any input to a computed field is visibility-scoped, the computed field inherits the intersection of input visibility scopes automatically. | Zero ceremony. The compiler infers it. Correct by construction. | Implicit — the author must trace the dependency graph to understand which states a computed field exists in. |
| **Opt-in with `visible` on computed field** | The author declares `visible in/after` on the computed field explicitly. The compiler validates that all inputs are visible in those states. | Explicit. Readable. Validates against declarations. | Ceremony — the author must spell out what the compiler could infer. |

**Recommendation: Automatic inference with compile-time visibility reporting.**

Rationale: The computed field's visibility is *determined by* its inputs — it's not an independent choice. Making it opt-in would force the author to manually track dependency chains. But the language server and `precept_compile` should report the inferred visibility scope for every computed field, so the behavior is never hidden.

This matches Precept's philosophy: "deterministic, inspectable model" — the behavior is fully determined by the definition, and the tooling makes it visible.

---

## 6. What Stays `optional`

After state-scoped visibility eliminates ~65% of current nullable usage, two categories remain:

### Category 3: Optional Event Arguments (~20% of current nullable usage)

Event arguments that callers may or may not provide:

```precept
event Approve with Amount as number, Note as string optional default none
event Reopen with Note as string optional default none
event DeclineOffer with Note as string optional default none
```

These are genuine call-site optionality. The event *can be fired* with or without a `Note`. This is not lifecycle-dependent — it's caller-dependent. The `optional` keyword is the right tool.

**Pattern**: `optional default none` means "if the caller doesn't provide it, it's absent." This is directly parallel to OCaml's optional function arguments and Protobuf's `optional` fields.

### Category 4: Genuinely Optional Data (~15% of current nullable usage)

Fields that may legitimately never have a value:

```precept
precept CustomerProfile

field Name as string optional
field Email as string optional
field Phone as string optional
field Nickname as string optional
```

A customer may never provide a nickname. No event provides it. No state requires it. It's genuinely optional data.

In stateless precepts (no lifecycle), these are the ONLY nullable fields — and `optional` communicates the intent perfectly.

### Same `optional` keyword for both? — Decision Point

| Option | Description | Pros | Cons |
|---|---|---|---|
| **A. Same keyword** | `optional` on fields and event args both mean "may have no value." | One keyword to learn. Consistent. Simple. | Different semantic contexts (lifecycle data vs call-site optionality) use the same word. |
| **B. Different keywords** | Fields use `optional`; event args use a different word (e.g., `default none` without a separate keyword). | Semantic precision — the two uses are genuinely different. | Two concepts where one suffices. Over-engineering. |

**Recommendation: Option A (same `optional` keyword)**

Rationale: The underlying semantics are identical — "this value may be absent." The *reason* it may be absent differs (caller choice vs domain optionality), but the type-system fact and the runtime behavior are the same. Adding a second keyword for the same runtime behavior violates minimal ceremony and adds cognitive load for no runtime benefit.

The same word, same behavior principle keeps the language small. The *why* behind the optionality is a documentation concern, not a syntax concern.

---

## 7. Impact on Existing Language Features

### Guards

**Current**: `when Field != null` narrows a nullable field to non-null inside the row body.

**With state-scoped visibility**: Guards referencing a field not visible in the current state are compile errors. No need for null checks on visibility-scoped fields — they're either present (with a concrete value) or absent (and can't be referenced).

**With `optional` fields**: `when Field is present` replaces `when Field != null`, with the same type-narrowing behavior. The keyword changes from `nullable` to `optional` and presence testing moves from null comparisons to the `is present`/`is absent` operators (Section 3).

**Presence testing with `is present` / `is absent`:**

With `null` eliminated from the language (Section 3), `is present` and `is absent` are the standard operators for testing whether an optional field has a value. They also serve as the mechanism for cross-visibility field references:

**For optional fields:**
```precept
# Before: null comparison
when Note != null
when Note == null

# After: presence operators
when Note is present
when Note is absent
```

**For visibility-scoped fields in `from any` rows:**
```precept
from any on Escalate when AssignedAgent is present and Priority == "Critical" -> ...
```

`is present` / `is absent` serve double duty: optional value checking AND visibility checking in cross-state contexts. A `from any` row that references a visibility-scoped field without an `is present` guard remains a compile error — the presence test is required to safely narrow the scope.

**Type narrowing**: `when Field is present` narrows the field to a concrete (non-absent) value in the row body, just as `when Field != null` does today.

### Invariants

**Current**: Invariants reference any field. Nullable fields require null guards.

**With state-scoped visibility**: An invariant referencing a visibility-scoped field is automatically scoped to the states where the field is visible. For example:

```precept
field AssignedAgent as string visible after Assign default ""
invariant AssignedAgent != "" because "Assigned agent cannot be empty"
```

This invariant only applies in states where `AssignedAgent` is visible (after `Assign`). In `New`, the invariant doesn't fire — the field doesn't exist. No `when` guard needed.

The compiler can verify this automatically — the invariant's scope is the intersection of its referenced fields' visibility scopes.

### Edit Blocks

**Current**: `in <State> edit Field1, Field2` controls which fields are directly editable.

**With state-scoped visibility**: An `edit` declaration can only list fields visible in the named state. Listing a field that isn't visible in the state is a compile error.

```precept
field LastAgentNote as string visible after RequestCancellation default ""
in RetentionReview edit LastAgentNote  # Valid — field is visible in RetentionReview
in Active edit LastAgentNote           # Compile error — field not visible in Active
```

### Computed Fields

See Section 5. Summary: the absence-eliminates model means a computed field is absent in states where any of its inputs are absent. C81 and C83 remain in force within the visibility scope.

### Collection Fields

Collection fields could also be visibility-scoped:

```precept
field MissingDocuments as set of string visible after Submit
```

The collection is absent before `Submit` and present (defaulting to empty) after. Collection operations (`add`, `remove`, etc.) on an absent collection are compile errors.

### The `default` Keyword

**Decision point**: does a field with `visible in/after` and a `default` make sense?

Yes. The `default` specifies the initial value when the field *becomes visible*. Example:

```precept
field AssignedAgent as string visible after Assign default ""
```

When `Assign` fires and introduces `AssignedAgent`, its initial value is `""` — unless the `set` action in the transition provides a different value (which it almost always will). The `default` is the fallback.

**Can a field be both `visible in` and `optional`?**

Yes, for fields that are introduced at a certain lifecycle point but remain genuinely optional thereafter:

```precept
field ReviewerNotes as string visible after StartReview optional default none
```

This field doesn't exist before `StartReview`. After `StartReview`, it exists and may be absent (`none`) — genuinely optional, as the reviewer may or may not add notes.

---

## 8. Impact on Runtime APIs and Tooling

### InstanceData (C# `Dictionary<string, object?>`)

**Current**: All fields appear in the dictionary. Nullable fields have `null` values.

**With state-scoped visibility**: Fields absent in the current state are **not keys in the dictionary**. The distinction between "absent" and "present with null" maps to:

| State | Dictionary behavior |
|---|---|
| Field absent (not visible in this state) | Key does not exist in dictionary |
| Field present, value is non-null | Key exists, value is the typed value |
| Field present, value is null (`optional` field) | Key exists, value is `null` |

This is the Datomic model — absence is non-assertion, not null-assertion.

**C# API implications:**
- `instance.Data.ContainsKey("AssignedAgent")` → `false` in `New`, `true` in `Assigned`.
- `instance.Data["AssignedAgent"]` → `KeyNotFoundException` in `New`, value in `Assigned`.
- `instance.Data.TryGetValue("AssignedAgent", out var value)` → `false` in `New`.

### MCP Tools

| Tool | Change |
|---|---|
| `precept_compile` | Field DTOs include a `visibleIn` array listing states where the field exists. Computed fields include an `inferredVisibility` array. |
| `precept_inspect` | State snapshot includes `presentFields` and `absentFields` lists. Each event outcome shows which fields would become visible/absent after firing. |
| `precept_fire` | Result data dictionary only contains fields visible in the resulting state. |
| `precept_update` | Rejects updates to fields not visible in the current state (new diagnostic). |
| `precept_language` | New `visible` keyword in the keyword catalog. New `visible in`, `visible after` construct descriptions. |

### Language Server

| Feature | Change |
|---|---|
| **Completions** | In expression contexts, only offer fields visible in the current state scope. In `visible in/after` clauses, offer state/event names. |
| **Diagnostics** | New diagnostics for: referencing absent field, setting absent field, editing absent field, `visible` with invalid state/event names. |
| **Hover** | Field hover shows visibility scope: "Visible in: Assigned, WaitingOnCustomer, Resolved, Closed" or "Visible after: Assign". |
| **Semantic tokens** | `visible`, `after` highlighted as keywords. State/event names in `visible` clauses get appropriate token types. |

### Preview / Diagram

The state diagram should visualize field presence per state. One approach: each state node expands to show its visible fields.

```
┌────────────────────┐
│      New           │
│ ── Fields ──       │
│ Severity           │
│ Priority           │
│ ReopenCount        │
│ AgentQueue         │
└────────────────────┘
         │ Assign
         ▼
┌────────────────────┐
│    Assigned         │
│ ── Fields ──       │
│ + TicketTitle      │
│ + AssignedAgent    │
│ + LastQueuedAgent  │
│ Severity           │
│ Priority           │
│ ReopenCount        │
│ AgentQueue         │
└────────────────────┘
```

Fields introduced in a state are marked with `+`. This makes the progressive data model visually apparent.

---

## 9. Concrete Before/After — `it-helpdesk-ticket.precept`

### Current Version (nullable)

```precept
precept ItHelpdeskTicket

field TicketTitle as string nullable
field Severity as number default 3
field AssignedAgent as string nullable
field LastQueuedAgent as string nullable
field ResolutionNote as string nullable
field ReopenCount as integer default 0
field Priority as choice("Low","Medium","High","Critical") default "Low"

field AgentQueue as queue of string

invariant Severity >= 1 because "Severity cannot be lower than 1"
invariant Severity <= 5 because "Severity cannot be higher than 5"
invariant ReopenCount >= 0 because "Reopen count cannot be negative"

state New initial
state Assigned
state WaitingOnCustomer
state Resolved
state Closed

in Assigned assert AssignedAgent != null because "Assigned tickets must name an agent"
in New edit Priority

event RegisterAgent with AgentName as string
on RegisterAgent assert AgentName != "" because "An agent name is required"

event Triage with Title as string, Level as number default 3
on Triage assert Title != "" because "A ticket title is required"
on Triage assert Level >= 1 and Level <= 5 because "Severity must stay within the supported range"

event Assign

event CustomerReply
event Resolve with Note as string
on Resolve assert Note != "" because "A resolution note is required"

event CloseTicket

event Reopen with Note as string nullable default null
on Reopen assert Note == null or Note != "" because "A supplied reopen note cannot be blank"

from New on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition
from Assigned on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition
from WaitingOnCustomer on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition
from Resolved on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition

from New on Triage -> set TicketTitle = Triage.Title -> set Severity = Triage.Level -> no transition
from New on Assign when AgentQueue.count > 0 -> set LastQueuedAgent = AgentQueue.peek -> dequeue AgentQueue -> set AssignedAgent = LastQueuedAgent -> transition Assigned
from New on Assign -> reject "No support agent is currently available"

from Assigned on CustomerReply -> transition WaitingOnCustomer
from Assigned on Resolve -> set ResolutionNote = Resolve.Note -> transition Resolved

from WaitingOnCustomer on Assign when AgentQueue.count > 0 -> set LastQueuedAgent = AgentQueue.peek -> dequeue AgentQueue -> set AssignedAgent = LastQueuedAgent -> transition Assigned
from WaitingOnCustomer on Assign -> reject "No support agent is currently available"

from Resolved on CloseTicket -> transition Closed
from Resolved on Reopen -> set ReopenCount = ReopenCount + 1 -> transition New
```

---

### Option B Rewrite (Recommended — `visible after` + `optional`)

```precept
precept ItHelpdeskTicket

# Always-present fields
field Severity as number default 3
field ReopenCount as integer default 0
field Priority as choice("Low","Medium","High","Critical") default "Low"
field AgentQueue as queue of string

# Lifecycle-scoped fields — introduced by specific events
field TicketTitle as string visible after Triage default ""
field AssignedAgent as string visible after Assign default ""
field LastQueuedAgent as string visible after Assign default ""
field ResolutionNote as string visible after Resolve default ""

invariant Severity >= 1 because "Severity cannot be lower than 1"
invariant Severity <= 5 because "Severity cannot be higher than 5"
invariant ReopenCount >= 0 because "Reopen count cannot be negative"

# AssignedAgent != "" is enforced by the field's default and the set action.
# The old 'in Assigned assert AssignedAgent != null' is unnecessary —
# AssignedAgent doesn't exist in New, and its default is "" once visible.

state New initial
state Assigned
state WaitingOnCustomer
state Resolved
state Closed

in New edit Priority

event RegisterAgent with AgentName as string
on RegisterAgent assert AgentName != "" because "An agent name is required"

event Triage with Title as string, Level as number default 3
on Triage assert Title != "" because "A ticket title is required"
on Triage assert Level >= 1 and Level <= 5 because "Severity must stay within the supported range"

event Assign

event CustomerReply
event Resolve with Note as string
on Resolve assert Note != "" because "A resolution note is required"

event CloseTicket

event Reopen with Note as string optional default none
on Reopen assert Note is absent or Note != "" because "A supplied reopen note cannot be blank"

from New on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition
from Assigned on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition
from WaitingOnCustomer on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition
from Resolved on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition

from New on Triage -> set TicketTitle = Triage.Title -> set Severity = Triage.Level -> no transition
from New on Assign when AgentQueue.count > 0 -> set LastQueuedAgent = AgentQueue.peek -> dequeue AgentQueue -> set AssignedAgent = LastQueuedAgent -> transition Assigned
from New on Assign -> reject "No support agent is currently available"

from Assigned on CustomerReply -> transition WaitingOnCustomer
from Assigned on Resolve -> set ResolutionNote = Resolve.Note -> transition Resolved

from WaitingOnCustomer on Assign when AgentQueue.count > 0 -> set LastQueuedAgent = AgentQueue.peek -> dequeue AgentQueue -> set AssignedAgent = LastQueuedAgent -> transition Assigned
from WaitingOnCustomer on Assign -> reject "No support agent is currently available"

from Resolved on CloseTicket -> transition Closed
from Resolved on Reopen -> set ReopenCount = ReopenCount + 1 -> transition New
```

**What changed:**
1. `nullable` → removed from lifecycle-scoped fields, replaced with `visible after <Event>` + concrete `default`.
2. `nullable` → `optional` on the `Reopen` event's `Note` argument (genuine call-site optionality).
3. `in Assigned assert AssignedAgent != null` → removed (redundant — field doesn't exist in states before `Assign`, and has a non-null default once visible).
4. All lifecycle-scoped fields now have concrete defaults instead of implicit null.
5. `null` keyword eliminated: `default null` → `default none`, `Note == null` → `Note is absent`. The DSL surface is now null-free.

**Net effect:** 4 `nullable` annotations replaced by 4 `visible after` clauses and concrete defaults. 1 state assert eliminated. 1 `nullable` → `optional` rename on a genuinely optional event arg. All `null` references replaced with domain-appropriate alternatives (`none`, `is absent`). The precept is MORE explicit about field lifecycle with LESS null-checking ceremony.

---

## 10. Decision Matrix

| # | Decision | Options | Recommendation | Key Tradeoff |
|---|---|---|---|---|
| 1 | Null vocabulary migration | A. Hard break / B. Soft deprecation / C. Permanent alias | **B. Soft deprecation** | Migration effort vs. permanent synonym baggage |
| 2 | `none` default semantics | Explicit `default none` required / implicit when `optional` has no `default` / both | **Both (implicit allowed, explicit accepted)** | Readability vs. ceremony |
| 3 | `clear` syntax | Standalone action keyword / `set Field = none` / both | **Standalone action keyword** | Semantic distinction (`set` assigns, `clear` removes) vs. language surface size |
| 4 | `none` in comparisons | Allowed (`when Field == none`) / restricted to `default` only | **Restricted to `default` only** | Use `is present`/`is absent` for testing — one way to do it |
| 5 | State-scoped visibility syntax | A. Implicit inference / B. Explicit `visible in/after` clause / C. State blocks | **B. Explicit `visible in/after`** | Zero-ceremony inference vs. explicit-but-readable declarations |
| 6 | Cycle/re-entrance behavior | Sticky visibility / State-determined / Author-declared | **Sticky (default)** | Data preservation vs. clean re-entry semantics |
| 7 | Computed field absence | Automatic inference / Opt-in declaration | **Automatic inference** | Implicit deduction vs. explicit declaration ceremony |
| 8 | Cross-visibility field references | Compiler prevents without guard / `is present` guard required | **`is present` guard required** | Presence operators serve double duty — optional checking and visibility checking |
| 9 | Same `optional` keyword for fields and event args | Same keyword / Different keywords | **Same keyword** | Semantic precision vs. language simplicity |
| 10 | `visible after` shorthand | Include / Omit (only `visible in`) | **Include** | Convenience vs. additional syntactic sugar to maintain |
| 11 | `visible` + `optional` combination allowed | Yes / No | **Yes** | Handles "introduced at lifecycle point but genuinely optional" |

---

## 11. Open Questions

### Questions Requiring Prototyping

1. **`visible after` with `no transition` events.** The Triage event in `it-helpdesk-ticket` uses `no transition`. What does `visible after Triage` mean when Triage doesn't change state? The field becomes visible in the current state (New) from that point forward. But "from that point forward" is runtime state, not compile-time state. Does this require tracking "has Triage fired?" as a phantom runtime flag? Or does `visible after Triage` simply expand to "visible in all states reachable after Triage, *including the current state*"?

2. **`visible after` with events that fire in multiple source states.** If `Assign` can fire `from New` and `from WaitingOnCustomer`, `visible after Assign` means the field is introduced in both paths. The compiler needs to compute the reachability set correctly.

3. **Interaction with `from any` rows.** If a `from any` row references a field that's not visible in all states, the compiler should reject it. But common patterns like `from any on RegisterAgent` (which doesn't reference lifecycle-scoped fields) should still work.

4. **Serialization boundary — JSON round-tripping.** When an MCP tool returns instance data, absent fields are omitted from the JSON. When the caller sends data back, absent fields should be ignored if present in the payload. This needs explicit specification.

### Questions Requiring Design Discussion

5. **Should `visible after` support compound expressions?** E.g., `visible after Submit or Expedite` — the field becomes visible after either event. Or is `visible in <state list>` the fallback for complex cases?

6. **Should the default for `visible after` fields be required?** Currently, `nullable` fields default to null if no default is specified. But if a `visible after` field is not nullable, it needs a concrete default. Making `default` required for `visible after` fields is explicit. Making it optional (with a type-appropriate zero value) is convenient but implicit.

7. **How does this interact with rules?** The [rules design](../../../docs/RulesDesign.md) defines conditional evaluation. Do rules inherit visibility scoping from their referenced fields?

8. **Migration path for existing precepts.** ~65% of nullable usages can become `visible after/in`. Is there a mechanical transformation? Can the language server offer a quick-fix that suggests `visible after <Event>` when a nullable field is only set by one event?

9. **Stateless precepts.** State-scoped visibility only applies to stateful precepts. Stateless precepts use `optional` for genuinely optional fields. Is there any interaction or confusion?

10. **Performance implications.** Does computing per-state field visibility add meaningful cost to compilation? For typical precepts (5-15 states, 10-30 fields), this should be negligible. Worth measuring if precepts grow much larger.

### Questions Added by Null Elimination

11. **How does `clear` interact with visibility-scoped fields?** Can you `clear` a required (non-optional) field? Likely not — only `optional` fields can be cleared, since clearing a required field would violate its non-optional constraint. `clear` on a `visible after` field that is not `optional` should be a compile error.

12. **Does `none` appear in InstanceData, or is it mapped to C# `null`?** Mapped — `none` is the DSL surface representation, `null` is the C# runtime representation. The boundary translation is transparent: `default none` produces a `null` value in the dictionary; `is present` compiles to a null check; `clear` sets the value to `null`.

13. **Should `is present` narrow the type?** In the same way `!= null` does today, `is present` should narrow the field to its concrete type in the row body. This enables safe field access after the presence check without redundant guards.

14. **Can `none` appear in comparisons?** E.g., `when Field == none`. If `none` is restricted to `default` clauses only, comparisons use `is absent`. If `none` is allowed in comparisons, it's a synonym for `is absent`. The recommendation is to restrict `none` to `default` clauses and use `is present`/`is absent` for all testing — this avoids two ways to express the same check.

---

## Appendix A: Other Sample Files Under Option B

### `loan-application.precept` (key fields only)

**Current:**
```precept
field ApplicantName as string nullable
field DecisionNote as string nullable
```

**Proposed:**
```precept
field ApplicantName as string visible after Submit default ""
field DecisionNote as string visible after Approve default ""
```

Note: `DecisionNote` is set by both `Approve` and `Decline`. This would need `visible in UnderReview, Approved, Funded, Declined` or `visible after Submit` (if we want it available throughout the review process), or the `visible after` shorthand would need to support multiple events: `visible after Approve, Decline`. Alternatively, it could be `visible in Approved, Declined` — explicitly listing the states where it has a value.

This is exactly the kind of case that surfaces the `visible in` / `visible after` design: `visible after` is clean for simple linear lifecycles, while `visible in` handles branching paths.

### `insurance-claim.precept` (key fields only)

**Current:**
```precept
field ClaimantName as string nullable
field AdjusterName as string nullable
field DecisionNote as string nullable
```

**Proposed:**
```precept
field ClaimantName as string visible after Submit default ""
field AdjusterName as string visible after AssignAdjuster default ""
field DecisionNote as string visible in Approved, Denied, Paid optional default none
```

Note: `DecisionNote` is optional even in the states where it's visible — `in Approved assert DecisionNote is present when FraudFlag` makes it conditionally required, not universally required. This is the `visible` + `optional` combination from Decision #8.

### `subscription-cancellation-retention.precept` (key fields only)

**Current:**
```precept
field SubscriberName as string nullable
field PlanName as string nullable
field CancellationReason as string nullable
field LastAgentNote as string nullable
```

**Proposed:**
```precept
field SubscriberName as string visible after RequestCancellation default ""
field PlanName as string visible after RequestCancellation default ""
field CancellationReason as string visible after RequestCancellation default ""
field LastAgentNote as string visible after RequestCancellation optional default none
```

`LastAgentNote` is visible after `RequestCancellation` but genuinely optional — it's set by `MakeSaveOffer` and `DeclineOffer`, neither of which is guaranteed to fire. Hence `visible after RequestCancellation optional default none`.

---

*End of proposal. —Frank*
