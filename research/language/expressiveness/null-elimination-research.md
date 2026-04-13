# Deep External Research — Could Precept Eliminate Nulls Entirely?

> **Related issue**: #17 (Computed Fields)
> **Prior research**: `nullable-computed-fields-research.md` (same directory)
> **Type**: External feasibility & suitability analysis — NOT a proposal
> **Date**: 2025-07-19

## Executive Summary

This study surveys how languages, type systems, configuration DSLs, serialization formats, databases, domain modeling paradigms, and UI form systems handle the absence of values — and whether their approaches could allow Precept to eliminate the `nullable` keyword entirely. The central question: **is there a coherent, Precept-aligned alternative to nullable that handles all five of Precept's identified nullable usage patterns without introducing a worse complexity budget?**

**Verdict: NOT FEASIBLE as a total elimination. FEASIBLE as a reduction.** Precept's nullable serves five distinct roles. Two roles (phase-dependent data and "not yet provided") can be structurally eliminated through state-scoped field visibility. Two roles (optional event arguments and genuinely optional data) require an explicit optionality marker that is functionally equivalent to nullable. One role ("set later by lifecycle event") is a hybrid that could be partially addressed by either mechanism. The research below provides the evidence base for these conclusions.

---

## 1. The Billion Dollar Mistake

Tony Hoare introduced `null` into ALGOL W in 1965 "simply because it was so easy to implement." At QCon London 2009, he called it his "billion dollar mistake," estimating the cumulative cost of null pointer exceptions, null reference bugs, and defensive null-checking code at over one billion dollars across the software industry.

His proposed solution: **disjoint unions with discrimination tests** — a pointer type modeled as a union of a null type or an actual type, where the compiler forces you to handle both cases. This is precisely what `Option<T>` / `Maybe` types implement in modern null-free languages.

Key quote: "A programming language designer should be responsible for the mistakes made by programmers using the language." This aligns directly with Precept's philosophy of **prevention, not detection** — the runtime should make invalid configurations structurally impossible, not catch them after the fact.

**Relevance to Precept**: Precept already prevents null-related bugs more aggressively than most languages — non-nullable fields must have defaults, computed fields cannot reference nullable fields (C83), and nullable narrowing in guards provides compile-time safety. The question is whether Precept can go further and eliminate `null` as a concept entirely.

---

## 2. Null-Free Languages Survey

### 2.1 Rust — `Option<T>`

Rust has no null. The `Option<T>` enum has exactly two variants: `Some(T)` and `None`. The compiler forces exhaustive pattern matching — you cannot use an `Option<T>` as if it were a `T` without explicitly handling the `None` case.

```rust
enum Option<T> {
    Some(T),
    None,
}
```

Key properties:
- `Option<T>` and `T` are **different types** — the compiler won't let you confuse them.
- Pattern matching (`match`, `if let`) is the primary extraction mechanism.
- Convenience methods: `unwrap_or(default)`, `map()`, `and_then()`, `is_some()`, `is_none()`.
- Nested optionality: `Option<Option<T>>` is valid and semantically distinct from `Option<T>`.

**Relevance to Precept**: Rust's approach requires a general-purpose type system with generics, pattern matching, and method chaining. Precept's "English-ish" DSL philosophy and minimal-ceremony design principle make Option types impractical — the ceremony cost would be enormous for a DSL audience.

### 2.2 Haskell — `Maybe`

Haskell's `Maybe a = Nothing | Just a` is the purest implementation of Hoare's proposed solution. The type system tracks optionality, and monadic composition (`>>=`) allows chaining operations that might produce `Nothing`.

For JSON interop, Haskell's Aeson library demonstrates the practical challenges:
- `.:` requires a field and fails if absent.
- `.:?` returns `Maybe` for optional fields.
- `omitNothingFields` controls whether `Nothing` values are serialized as `null` or omitted.
- `omittedField` (since aeson 2.2.0) provides defaults when fields are absent in input JSON.
- `allowOmittedFields` controls whether parsing allows missing fields.

**Key insight**: Even in a language with first-class Maybe types, the JSON boundary forces explicit decisions about whether "absent" means "null value present" or "key not present." This distinction is the same tension Precept faces between "field is nullable" and "field doesn't exist in this state."

### 2.3 Elm — `Maybe` with "Avoiding Overuse" Guidance

Elm has no null and no runtime exceptions. `Maybe a = Just a | Nothing`. JSON input must pass through decoders that validate structure — `field "name" string` demands the field exists and is a string, failing explicitly if not.

The Elm Guide's **"Avoiding Overuse"** section is directly relevant to Precept:

> "It is one thing to have a `Maybe` in your model, but some codebases end up with `Maybe` everywhere. This can actually make things worse, not better."

Elm recommends **custom types to model phases** rather than sprinkling `Maybe` throughout a model:

```elm
type Profile
    = Less String          -- just a name
    | More String Info     -- name + full info
```

This "Less/More" pattern replaces multiple `Maybe` fields with a single discriminated type that structurally guarantees which data is present in each phase. The compiler ensures you handle both cases.

**Key insight for Precept**: Elm's guidance directly validates the idea that **states should determine which fields are present**, rather than making individual fields nullable. Precept's state machine already provides the "phase" mechanism — the question is whether to make field visibility state-dependent.

### 2.4 OCaml / ReasonML — `option`

OCaml's `option('a) = None | Some('a)` is built into the language. Pattern matching with `switch` forces exhaustive handling. Optional function arguments use `~prefix=?` syntax for a lightweight optional-parameter mechanism.

**Relevance to Precept**: OCaml's optional function arguments are relevant to Precept's optional event arguments pattern (`Note as string nullable default null`). OCaml solves this with a distinct syntactic marker (`~arg=?`) rather than making the type nullable — the optionality is a property of the **call site**, not the **type**.

---

## 3. Configuration DSLs

Configuration DSLs face Precept's exact tension: they must express structured data where some fields are optional, some have defaults, and some are conditionally present.

### 3.1 Dhall — `Optional` Type, No Null

Dhall has **no null value**. All types are non-nullable by default. Optionality is expressed through the `Optional` type:

```dhall
Some 1    : Optional Natural   -- present
None Natural : Optional Natural -- absent
```

Key properties:
- `Optional` values can be nested: `Some (None Bool)` and `None (Optional Bool)` are semantically distinct.
- Empty lists require explicit type annotations: `[] : List Natural`.
- Record completion (`T::r`) provides defaults for unspecified fields.
- Dhall is **total** — every well-typed program terminates, and no runtime errors are possible.

**Relevance to Precept**: Dhall's record completion mechanism (`T::{ name = "John" }` fills in defaults for missing fields) is conceptually similar to what Precept's `default` keyword does. Dhall proves that a configuration language can work without null, but at the cost of explicit `Optional` wrapping everywhere optionality is needed — which is exactly the ceremony Precept's philosophy tries to avoid.

### 3.2 CUE — Constraints, Not Null

CUE takes a unique approach to optionality through **field constraints**:

```cue
A: {
    foo!:  int    // required — must be provided
    bar?:  string // optional — only constrains if present
    baz:   float  // regular — must be concrete for export
    quux?: _|_    // cannot be specified
}
```

CUE distinguishes three field natures:
- **Regular fields** (`field: value`) — must be concrete for export.
- **Optional field constraints** (`field?: value`) — constrain the value only if specified elsewhere.
- **Required field constraints** (`field!: value`) — must be specified as a regular field.

CUE has no null type. If a required field is missing, export fails with "field is required but not present." Optional fields that aren't specified simply don't appear in the output.

**Key insight for Precept**: CUE's `!` (required) and `?` (optional) modifiers on fields are structurally similar to what Precept could do. The distinction between "this field must be present" and "this field may or may not be present" is expressed at the field declaration level, not through a nullable type.

### 3.3 Pkl — Nullable Types with `?` Suffix

Pkl (Apple's configuration language) has explicit null handling:

```pkl
bird: Bird = null    // Error: Expected Bird, got null
bird2: Bird? = null  // OK: Bird? admits null
```

Key mechanisms:
- `!!` (non-null operator) — asserts non-null, errors if null.
- `??` (null coalescing) — provides a default for null values.
- `?.` (null propagation) — null-safe member access.
- `ifNonNull()` — generalized null-safe transformation.
- Properties of type `X?` default to `Null(x)` where `x` is the default for `X`.

Pkl's `Null(default)` concept is notable: a null value carries a "latent default" that activates when the null is amended. This allows a property to be "switched off" by default but have a meaningful structure when "switched on."

**Key insight for Precept**: Pkl's `Null(default)` pattern is conceptually interesting for Precept's "not yet provided" fields. A field could be absent (not yet set) but carry a type constraint that activates when a value is eventually provided. However, this adds conceptual complexity that Precept's philosophy resists.

### 3.4 Jsonnet — Null as a Regular Value

Jsonnet includes `null` as one of its seven value types with no special handling: "There is no special handling of null, it is a value like any other." Arrays can have null elements and objects can have null fields.

However, Jsonnet's **conditional fields** provide an alternative to null for expressing absence:

```jsonnet
{
    [if condition then "foo"]: "bar",
    // If condition is false, field "foo" doesn't exist at all
}
```

When a field name expression evaluates to null, the field is discarded. This provides structural absence rather than null-valued presence — the field literally doesn't exist in the resulting object.

**Key insight for Precept**: Jsonnet's conditional fields demonstrate that "field doesn't exist" is a legitimate and sometimes preferable alternative to "field exists with null value." This reinforces the case for state-dependent field visibility in Precept.

---

## 4. Serialization Formats — The Boundary Problem

### 4.1 Protocol Buffers (proto3) — Implicit vs. Explicit Presence

Protobuf's handling of field presence is the most detailed treatment of the "is it absent or is it the default?" problem in any serialization format:

**Implicit presence** (proto3 default without `optional`):
- Default values (0, "", false) are **not serialized**.
- After deserialization, you cannot distinguish "explicitly set to 0" from "never set."
- No `has_field()` method is generated.

**Explicit presence** (proto3 with `optional`):
- All explicitly set values are serialized, **including defaults**.
- `has_field()` method is generated to check if a value was explicitly set.
- `clear_field()` method removes the value.

The Protobuf team now **recommends** always using `optional` for proto3 basic types because the implicit presence semantics cause real bugs in practice — especially with merging ("merge-from-default" is impossible under implicit presence) and round-tripping through intermediaries.

**Key insight for Precept**: Protobuf's experience demonstrates that conflating "not set" with "set to default" is a persistent source of bugs. Precept's current `nullable` + `default` system is more explicit than proto3's implicit presence, but the lesson is that **presence tracking matters** and should be explicit.

### 4.2 JSON Schema — `required` Array vs. Type Nullability

JSON Schema separates two orthogonal concerns:

1. **Presence**: The `required` array lists which properties must exist in the object. Properties not in `required` may be absent.
2. **Type**: A property's schema defines what values it accepts. `{"type": "null"}` is a valid type, and `{"type": ["string", "null"]}` means "string or null."

Crucially: **"In JSON a property with value `null` is not equivalent to the property not being present."** A property present with value `null` is distinct from the property being absent. This distinction has been a recurring source of confusion in JSON-based APIs.

JSON Schema's `unevaluatedProperties` (draft 2019-09) and conditional schemas (`if`/`then`/`else`) allow expressing state-dependent field presence — properties can be required or forbidden based on the values of other properties.

**Key insight for Precept**: JSON Schema's conditional required properties (`if type == "business" then department is required`) is structurally similar to Precept's state-dependent invariants. The schema world has converged on the idea that field presence rules should be context-dependent.

---

## 5. Databases and Event Systems

### 5.1 Datomic — Absence as Non-Assertion

Datomic (Rich Hickey's immutable database) takes the most radical approach to absence:

- There is no null. An entity either **has** an attribute or **doesn't have** it.
- Pull queries for missing attributes return an **empty map** `{}` — the attribute is simply omitted from the result, rather than appearing with a null value.
- The `:default` option in pull patterns provides fallback values: `[:artist/endYear :default 0]` returns `0` for entities without an `:artist/endYear` attribute.
- Attributes are asserted (added) and retracted (removed) over time — an entity's set of attributes can change.

This "absence-as-non-assertion" model means there is no ambiguity between "the value is null" and "the attribute doesn't apply." If Paul McCartney has no `:artist/endYear`, it's because he's still active — not because someone set his end year to null.

**Key insight for Precept**: Datomic's model was identified in the prior `nullable-computed-fields-research.md` as the closest philosophical neighbor to Precept. The idea that "this field simply doesn't exist in this context" is more honest than "this field exists but its value is null." Precept's state machine provides a natural mechanism for this: fields could be **visible only in certain states**.

### 5.2 Event Sourcing — Progressive State Building

Event sourcing builds state by replaying a sequence of domain events through projections. The implications for nullability:

1. **State is progressively built** — after `OrderCreated`, only `OrderId` and `CustomerId` exist. `ShippingAddress` doesn't exist yet because no `AddressProvided` event has occurred. There is no "null shipping address" — the concept hasn't been introduced yet.

2. **Projections are the read model** — each projection builds a custom data model optimized for its use case. Different projections of the same event stream may have different fields present at different times.

3. **Events carry data, not absence** — an event like `OrderPlaced { orderId, customerId, items }` asserts what happened. It doesn't assert "shipping address is null." The shipping address simply isn't part of this event.

**Key insight for Precept**: Event sourcing's progressive state building maps directly to Precept's state machine transitions. An event `Submit` carries `ApplicantName` and `RequestedAmount` — it doesn't carry "null DecisionNote." The `DecisionNote` field only acquires meaning when the `Review` or `Decide` event fires. Precept's current design already reflects this: `set DecisionNote = Decide.DecisionNote` only executes in the `Decide` event. The question is whether the DSL should make this structural rather than conventional.

---

## 6. Domain Modeling — Making Illegal States Unrepresentable

Scott Wlaschin's "Domain Modeling Made Functional" (F#) articulates a principle directly relevant to Precept: **make illegal states unrepresentable** through the type system.

The canonical example: a contact with an email OR a postal address OR both, but NOT neither. Instead of:

```fsharp
type Contact = {
    Email: string option  // nullable
    Postal: string option // nullable
    // Bug: both can be None!
}
```

Use a discriminated union:

```fsharp
type ContactInfo =
    | EmailOnly of string
    | PostalOnly of string
    | Both of string * string
```

Now "neither" is structurally impossible. The compiler enforces the business rule.

**Application to Precept**: Precept's state machine already serves this role. A `LoanApplication` in state `PendingReview` structurally guarantees that `ApplicantName` has been provided (the `Submit` event set it). A `LoanApplication` in state `Decided` guarantees that `DecisionNote` has been set (the `Decide` or `Reject` event set it). The nullable usage pattern "set later by lifecycle event" is exactly the problem that state-dependent field visibility would solve.

---

## 7. CRDTs and Distributed State

CRDTs (Conflict-free Replicated Data Types) handle concurrent edits in distributed systems. In systems like Automerge:

- Fields can be **absent** (never set by any replica) or **present** (set by at least one replica).
- There is no null — a field either exists in the document or doesn't.
- Deletion means removing a field, not setting it to null.
- Merge semantics: if replica A sets field `x` and replica B never mentions it, after merge, `x` exists with A's value.

The CRDT model reinforces the distinction between "absent" (never introduced) and "present with no value" (null). The former is the natural state of a distributed document; the latter would create merge conflicts.

**Relevance to Precept**: CRDTs confirm that "absent" is a legitimate, first-class concept distinct from "null." Precept's state machine provides a similar guarantee — a field that hasn't been introduced by any event's `set` action is absent, not null.

---

## 8. UI Form Modeling

UI form libraries face exactly Precept's "not yet provided" problem:

### React Hook Form

- **`defaultValues`**: Every field starts with a default value. The docs explicitly warn: "You should avoid providing `undefined` as a default value, as it conflicts with the default state of a controlled component."
- **`formState`**: Tracks `isDirty`, `isTouched`, `isValid` per field — metadata about the field's lifecycle, not its value.
- **`shouldUnregister`**: When `true`, unmounting an input removes its value entirely (not sets it to null). The field ceases to exist in the form state.
- **Progressive disclosure**: Fields can be conditionally rendered. When hidden, they don't exist in the form data — not "null," but absent.

### Key Patterns

Form libraries distinguish:
1. **Not yet interacted** — field exists with its default, user hasn't touched it.
2. **Touched but empty** — user interacted, left it blank. Value is `""` (string) or `undefined`.
3. **Filled** — user provided a value.
4. **Conditionally hidden** — field doesn't exist in the current form state.

**Key insight for Precept**: Form libraries model the same lifecycle Precept faces — progressive disclosure of fields based on user actions (events) and application state. The best form libraries treat "conditionally hidden" as structural absence (the field doesn't exist), not as null. This aligns with state-dependent field visibility.

---

## 9. Precept's Current Nullable Landscape

An audit of all 25 sample `.precept` files identified **94 nullable usages** across approximately 20 files. These fall into five distinct categories:

### Category 1: "Not yet provided" (e.g., ApplicantName, CustomerName, TicketTitle)

Fields that are null at creation because the first event hasn't provided them yet. Example from `loan-application.precept`:

```
field ApplicantName as string nullable
```

Set by the `Submit` event: `set ApplicantName = Submit.ApplicantName`. Before `Submit`, the field is null. After, it's always non-null.

**Prevalence**: ~30% of nullable usages.

### Category 2: "Set later by lifecycle event" (e.g., AssignedAgent, DecisionNote, ResolutionNote)

Fields that are populated during mid-lifecycle transitions. Example from `it-helpdesk-ticket.precept`:

```
field AssignedAgent as string nullable
```

Null in `Open` state, set during `Assign` event, remains populated through `InProgress` and `Resolved` states.

**Prevalence**: ~25% of nullable usages.

### Category 3: "Optional event argument" (e.g., Note as string nullable default null)

Event arguments that callers may or may not provide. Example from `loan-application.precept`:

```
event Submit
    arg ApplicantName as string
    arg RequestedAmount as number
    arg Note as string nullable default null
```

The `Note` is genuinely optional — some submissions include notes, others don't.

**Prevalence**: ~20% of nullable usages.

### Category 4: "Genuinely optional data" (e.g., Nickname, Description)

Fields that may legitimately never have a value across the entity's entire lifecycle. Example from `customer-profile.precept`:

```
field Nickname as string nullable
```

A customer may or may not have a nickname. No lifecycle event will necessarily provide one.

**Prevalence**: ~15% of nullable usages.

### Category 5: "Phase-dependent data" (e.g., CancellationReason, LastReversedStep)

Fields that only have meaning in certain states. Example from `subscription-cancellation-retention.precept`:

```
field CancellationReason as string nullable
```

This field only has a value after `RequestCancellation` fires. In states before that event, the field isn't just empty — the concept doesn't apply.

**Prevalence**: ~10% of nullable usages.

---

## 10. Alternative Mechanisms — What Could Replace Nullable?

For each category, we assess whether an alternative mechanism could eliminate nullable:

### Category 1 ("not yet provided") → **State-scoped field visibility**

If fields were only visible/required in states where they've been set, `ApplicantName` would not exist in the initial state and would become visible after `Submit`. No nullable needed.

**Feasibility**: HIGH. Precept's state machine already tracks which events have fired. This is the Elm "Less/More" pattern applied to state machines.

**Trade-off**: Requires distinguishing "field declaration" from "field availability." Adds a new concept to the DSL, but eliminates a persistent source of confusion.

### Category 2 ("set later") → **State-scoped visibility (partial)**

`AssignedAgent` could be visible only in states after `Assign`. This works for simple cases but gets complex when the same field is set by multiple events in different branches.

**Feasibility**: MEDIUM. Works cleanly for linear lifecycles, less clean for branching state machines.

### Category 3 ("optional event argument") → **No elimination possible**

An optional event argument is fundamentally about call-site optionality. The caller decides whether to provide `Note`. This is exactly what `nullable` means in this context, and no alternative mechanism eliminates this need.

**Feasibility**: LOW. Optional arguments require some marker. Could rename from `nullable` to `optional`, but the semantics are identical.

### Category 4 ("genuinely optional data") → **No elimination possible**

Some data is genuinely optional. Not every customer has a nickname. No amount of state-machine engineering changes this fact.

**Feasibility**: LOW. An `optional` keyword would be semantically identical to `nullable` for this pattern.

### Category 5 ("phase-dependent data") → **State-scoped field visibility**

`CancellationReason` only applies after `RequestCancellation`. This is the strongest case for state-scoped visibility — the field shouldn't even exist before that event.

**Feasibility**: HIGH. Same mechanism as Category 1.

---

## 11. Cross-Cutting Analysis

### What the null-free languages teach us

Every null-free language replaces null with an **explicit optionality marker** (Option, Maybe, Optional). They don't eliminate the concept of absence — they make it **type-safe and visible**. The key benefit is not "no null" but "the compiler knows which values might be absent."

Precept already achieves this: the `nullable` keyword is an explicit optionality marker, and the type checker enforces C83 (no nullable refs in computed expressions) and narrowing (`when Field != null`).

### What configuration DSLs teach us

Config DSLs split into two camps:
1. **Null-free with Optional** (Dhall): Explicit `Optional` wrapping, high ceremony.
2. **Field-level optionality markers** (CUE `?`, `!`): The field declaration says whether presence is required or optional.

Precept's `nullable` is closer to CUE's approach — optionality is declared at the field level, not through type wrapping. CUE's distinction between `field!:` (required) and `field?:` (optional) is more expressive than a binary nullable/non-nullable split.

### What Datomic and event sourcing teach us

The strongest alternative to nullable is **structural absence** — the field doesn't exist, rather than existing with a null value. Datomic achieves this through its attribute model; event sourcing achieves it through progressive state building. Precept's state machine is the natural mechanism for structural absence: a field could be "not yet part of this entity's state" rather than "part of this entity's state with value null."

### What form libraries teach us

Form libraries confirm that "conditionally absent" is a legitimate, useful concept — distinct from "present with empty value" or "present with null value." React Hook Form's `shouldUnregister: true` (unmounting removes the value, not nullifies it) directly models what state-scoped field visibility would give Precept.

---

## 12. Feasibility Verdict

### Total elimination of nullable: NOT FEASIBLE

Categories 3 and 4 (optional event arguments, genuinely optional data) require an explicit optionality marker. Whether it's called `nullable`, `optional`, or `maybe`, the concept is irreducible. Trying to eliminate it entirely would either:
- Force artificial states to "contain" optional data (over-engineering).
- Require Option/Maybe types that violate Precept's minimal-ceremony philosophy.
- Lose expressiveness that `.precept` authors need.

### Partial reduction of nullable through state-scoped visibility: FEASIBLE

Categories 1, 2, and 5 (~65% of nullable usages) could be structurally addressed by making field visibility state-dependent. If adopted, roughly two-thirds of current nullable usages could be eliminated, replaced by fields that simply don't exist in states where they haven't been set.

### Renaming nullable to optional for remaining cases: POSSIBLE

For categories 3 and 4, the keyword `optional` might communicate intent more clearly than `nullable`. "This field is optional" reads better in English-ish DSL than "this field is nullable." However, this is a cosmetic change, not a semantic one.

---

## 13. Comparison with Prior Research

The prior `nullable-computed-fields-research.md` identified four approaches to null handling:

| Approach | Examples | Precept alignment |
|---|---|---|
| Implicit propagation | SQL | LOW — violates explicitness principle |
| Explicit propagation operators | C#, Kotlin, Swift, TS | MEDIUM — adds operator ceremony |
| Monadic | Haskell | LOW — too abstract for DSL audience |
| Absence-eliminates | Datalog, Datomic | **HIGH** — closest philosophical match |

This research confirms and extends the prior finding. The Datalog/Datomic "absence-eliminates" model is indeed the best fit for Precept, and the mechanism to achieve it is **state-scoped field visibility** — which Precept's existing state machine infrastructure naturally supports.

---

## 14. Recommendations for Future Proposals

This research does NOT propose any language changes. It provides the evidence base for future proposals that might:

1. **Explore state-scoped field visibility** — fields declared with state-aware visibility rules, where a field only "exists" after the event that sets it. This would eliminate ~65% of current nullable usages.

2. **Investigate `optional` as a semantic alternative to `nullable`** — for genuinely optional data and optional event arguments, `optional` might communicate intent more clearly without changing behavior.

3. **Preserve current nullable semantics as-is if no reduction is adopted** — the current system works correctly and is well-understood. The cost of change must be justified by a proportional benefit.

4. **Do NOT pursue Option/Maybe types** — the ceremony cost is too high for Precept's DSL audience, and the benefit over `nullable` is marginal in a domain integrity context (vs. a general-purpose programming context).

Any future proposal should reference this research and the prior `nullable-computed-fields-research.md` for evidence grounding.

---

## Sources

- Tony Hoare, "Null References: The Billion Dollar Mistake," QCon London 2009 (InfoQ)
- Rust Book, Chapter 6.1: "Defining an Enum" — Option<T>
- Haskell Wiki: Maybe type; Hackage: aeson library documentation
- Elm Guide: "Error Handling — Maybe"; "Avoiding Overuse of Maybe"
- OCaml Manual v2: option type and pattern matching
- Dhall Language Tour: Optional values, Record completion
- CUE Language Tour: Struct types — required (`!`), optional (`?`), regular fields
- Pkl Language Reference: Null Values, Nullable Types, Null Coalescing
- Jsonnet Language Reference: Null type, Conditional Fields
- Protocol Buffers: "Application Note: Field Presence" (protobuf.dev)
- JSON Schema: "Understanding JSON Schema — object" (json-schema.org)
- Datomic: Pull API documentation — Missing Attributes, :default option
- Kurrent/EventStore: "Introduction to Event Sourcing"
- Scott Wlaschin: "Designing with Types — Making Illegal States Unrepresentable"
- React Hook Form: useForm API documentation — defaultValues, shouldUnregister, formState
