# Collection Types

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Draft |
| Implementation state | Partially implemented — set/queue/stack surface shipped; §Proposed Extensions are proposals, not yet implemented |
| Scope | Three collection kinds: `set of T`, `queue of T`, `stack of T`; actions, accessors, constraints, emptiness safety, membership, inner type system |
| Related | [Primitive Types](primitive-types.md) · [Language Spec](precept-language-spec.md) §§2.3, 3.6, 3.8 · [Type Checker](../compiler/type-checker.md) |

---

## Overview

Precept has three collection types. Each wraps a scalar inner type, provides a fixed set of mutation actions, and enforces structural safety through the proof engine. There are no user-defined collection types, no nested collections, and no collection-of-collection types.

| Kind | Ordering | Duplicates | Use case | Example |
|------|----------|------------|----------|---------|
| `set of T` | Unordered | No | Tags, categories, membership sets | `field Tags as set of string` |
| `queue of T` | FIFO | Yes | Work queues, waiting lists, processing pipelines | `field AgentQueue as queue of string` |
| `stack of T` | LIFO | Yes | Undo logs, step histories, nested contexts | `field RepairSteps as stack of string` |

**Grammar:**

```
CollectionType  :=  (set | queue | stack) of ScalarType
```

The inner type `T` must be a scalar type — any primitive type (`string`, `integer`, `decimal`, `number`, `boolean`, `choice`) or the special `~string` variant. Collections of collections are not supported.

**Common surface:** All three kinds share `.count`, `contains`, `clear`, `mincount`/`maxcount` constraints, and list literal defaults. Kind-specific operations are documented per section below.

---

## `set`

**Declaration:**

```precept
field Tags as set of string
field Labels as set of ~string
field RequestedFloors as set of number
field PendingInterviewers as set of string
field AllowedDepartments as set of string default []
```

**Behavior:** Unordered collection with no duplicate elements. Membership, deduplication, and ordering (for `.min`/`.max`) are governed by the inner type's comparer — ordinal for `string`, `OrdinalIgnoreCase` for `~string`, natural ordering for numeric types.

**Actions:**

| Action | Syntax | Behavior |
|--------|--------|----------|
| `add` | `add F Expr` | Add element to set. No-op if already present. |
| `remove` | `remove F Expr` | Remove element from set. No-op if not present. |
| `clear` | `clear F` | Remove all elements. |

```precept
from Draft on AddFloor
    -> add RequestedFloors AddFloor.Floor
    -> no transition

from Draft on RemoveFloor when RequestedFloors contains RemoveFloor.Floor
    -> remove RequestedFloors RemoveFloor.Floor
    -> no transition
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Always safe — returns 0 for empty set. |
| `.min` | `T` | `.count > 0` guard required | `T` must be orderable. Proof obligation: `UnguardedCollectionAccess`. |
| `.max` | `T` | `.count > 0` guard required | `T` must be orderable. Proof obligation: `UnguardedCollectionAccess`. |

"Orderable" means the inner type supports `<`/`>` comparison: all numeric types (`integer`, `decimal`, `number`), `string` (including `~string`, which uses `OrdinalIgnoreCase` ordering — deterministic), and `choice of T(...) ordered` (which defines rank by declaration position). `boolean` and unordered `choice of T(...)` are not orderable; `.min`/`.max` on `set of boolean` or `set of choice of T(...)` (without `ordered`) is a type error. On `set of choice of T(...) ordered`, `.min` returns the element with the lowest declaration position and `.max` returns the highest — these are safe when the `.count > 0` guard is satisfied.

```precept
field RiskLevels as set of choice of string("low", "medium", "high") ordered
# .min → "low", .max → "high" (by declaration position)

from Active on Evaluate when RiskLevels.count > 0
    -> set LowestRisk = RiskLevels.min
    -> set HighestRisk = RiskLevels.max
    -> no transition
```

```precept
from Draft on Submit when RequestedFloors.count > 0
    -> set LowestRequestedFloor = RequestedFloors.min
    -> set HighestRequestedFloor = RequestedFloors.max
    -> transition Submitted
```

**Constraints:** `mincount N`, `maxcount N`, `optional`, `default [...]`. See [Constraint Catalog](#constraint-catalog).

**Type errors:**

| Expression | Diagnostic |
|---|---|
| `enqueue SetField Expr` | `CollectionOperationOnScalar` — `enqueue` is a queue operation |
| `push SetField Expr` | `CollectionOperationOnScalar` — `push` is a stack operation |
| `set SetField = Expr` | `ScalarOperationOnCollection` — use `add`/`remove`/`clear` |
| `add SetField Expr` where `Expr` type ≠ `T` | `TypeMismatch` |

---

## `queue`

**Declaration:**

```precept
field AgentQueue as queue of string
field HoldQueue as queue of string
field PartyQueue as queue of string
```

**Behavior:** FIFO-ordered collection. Elements are added at the back (`enqueue`) and removed from the front (`dequeue`). Duplicates are permitted — the same value can appear multiple times.

**Actions:**

| Action | Syntax | Behavior | Proof requirement |
|--------|--------|----------|-------------------|
| `enqueue` | `enqueue F Expr` | Add element to the back. | None |
| `dequeue` | `dequeue F` | Remove and discard the front element. | `.count > 0` guard required. Obligation: `UnguardedCollectionMutation`. |
| `dequeue into` | `dequeue F into G` | Remove front element and store it in field `G`. `G` must be type `T`. | `.count > 0` guard required. Obligation: `UnguardedCollectionMutation`. |
| `clear` | `clear F` | Remove all elements. | None |

```precept
from Accepting on SeatNextParty when PartyQueue.count > 0
    -> set LastCalledParty = PartyQueue.peek
    -> dequeue PartyQueue into CurrentParty
    -> transition Seating
```

```precept
from OnShelf on PlaceHold
    -> enqueue HoldQueue PlaceHold.PatronId
    -> dequeue HoldQueue into PromotedPatron
    -> transition HoldReady
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Always safe. |
| `.peek` | `T` | `.count > 0` guard required | Returns the front element without removing it. Obligation: `UnguardedCollectionAccess`. |

```precept
from New on Assign when AgentQueue.count > 0
    -> set LastQueuedAgent = AgentQueue.peek
    -> dequeue AgentQueue
    -> set AssignedAgent = LastQueuedAgent
    -> transition Assigned
```

**Constraints:** `mincount N`, `maxcount N`, `optional`, `default [...]`. See [Constraint Catalog](#constraint-catalog).

**Type errors:**

| Expression | Diagnostic |
|---|---|
| `add QueueField Expr` | `CollectionOperationOnScalar` — `add` is a set operation |
| `remove QueueField Expr` | `CollectionOperationOnScalar` — `remove` is a set operation |
| `pop QueueField` | `CollectionOperationOnScalar` — `pop` is a stack operation |
| `set QueueField = Expr` | `ScalarOperationOnCollection` |
| `enqueue QueueField Expr` where `Expr` type ≠ `T` | `TypeMismatch` |
| `dequeue QueueField into G` where `G` type ≠ `T` | `TypeMismatch` |

---

## `stack`

**Declaration:**

```precept
field RepairSteps as stack of string
field BorrowerHistory as stack of string
```

**Behavior:** LIFO-ordered collection. Elements are added at the top (`push`) and removed from the top (`pop`). Duplicates are permitted.

**Actions:**

| Action | Syntax | Behavior | Proof requirement |
|--------|--------|----------|-------------------|
| `push` | `push F Expr` | Add element to the top. | None |
| `pop` | `pop F` | Remove and discard the top element. | `.count > 0` guard required. Obligation: `UnguardedCollectionMutation`. |
| `pop into` | `pop F into G` | Remove top element and store it in field `G`. `G` must be type `T`. | `.count > 0` guard required. Obligation: `UnguardedCollectionMutation`. |
| `clear` | `clear F` | Remove all elements. | None |

```precept
from InRepair on LogRepairStep
    -> push RepairSteps LogRepairStep.StepName
    -> no transition

from InRepair on UndoLastStep when RepairSteps.count > 0
    -> pop RepairSteps into LastReversedStep
    -> no transition
from InRepair on UndoLastStep
    -> reject "There is no logged repair step to undo"
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Always safe. |
| `.peek` | `T` | `.count > 0` guard required | Returns the top element without removing it. Obligation: `UnguardedCollectionAccess`. |

**Constraints:** `mincount N`, `maxcount N`, `optional`, `default [...]`. See [Constraint Catalog](#constraint-catalog).

**Type errors:**

| Expression | Diagnostic |
|---|---|
| `add StackField Expr` | `CollectionOperationOnScalar` — `add` is a set operation |
| `remove StackField Expr` | `CollectionOperationOnScalar` — `remove` is a set operation |
| `enqueue StackField Expr` | `CollectionOperationOnScalar` — `enqueue` is a queue operation |
| `set StackField = Expr` | `ScalarOperationOnCollection` |
| `push StackField Expr` where `Expr` type ≠ `T` | `TypeMismatch` |
| `pop StackField into G` where `G` type ≠ `T` | `TypeMismatch` |

---

## Inner Type System

The inner type `T` in `set of T`, `queue of T`, or `stack of T` must be a scalar type. The collection grammar is:

```
CollectionType    :=  (set | queue | stack) of ScalarType
ScalarType        :=  string | ~string | integer | decimal | number | boolean
                  |   ChoiceType
                  |   date | time | datetime | instant | duration
                  |   period ('of' ('date' | 'time') | 'in' PeriodUnit)?
                  |   timezone | zoneddatetime
                  |   money ('in' CurrencyCode)?
                  |   quantity ('of' DimensionCode | 'in' UnitCode)?
                  |   price ('in' CurrencyCode)?
                  |   currency | unitofmeasure | dimension | exchangerate

ChoiceType        :=  choice "of" ChoiceElementType "(" ChoiceValueExpr ("," ChoiceValueExpr)* ")" ordered?
ChoiceElementType :=  string | integer | decimal | number | boolean
ChoiceValueExpr   :=  StringLiteral | NumberLiteral | BooleanLiteral
```

**v1 limit:** `set of choice of string(...)` is not supported in v1. The collection inner type must be a simple scalar, not a parameterized choice type. Nesting typed choice inside a collection requires a separate AST/parser slice.

Collections of collections (`set of set of string`) are not supported. All Precept scalar types — including temporal and business-domain types — are valid inner types. See §Temporal and Business-Domain Inner Types below for ordering constraints and qualified type syntax.

### `~string` — case-insensitive inner type

`~string` is a special inner type variant, not a standalone field type. It selects `StringComparer.OrdinalIgnoreCase` as the collection's comparer, governing membership testing, deduplication (for sets), and ordering (for `.min`/`.max`) consistently.

```precept
field Tags   as set of string    # ordinal — "Apple" ≠ "apple", both coexist
field Labels as set of ~string   # OrdinalIgnoreCase — "Apple" and "apple" are the same element
```

**Key properties:**

| Property | `set of string` | `set of ~string` |
|---|---|---|
| Membership (`contains`) | Case-sensitive | Case-insensitive |
| Deduplication | Case-sensitive | Case-insensitive |
| `.min`/`.max` ordering | Ordinal | OrdinalIgnoreCase (deterministic) |

**`~string` is collection-only.** `field Name as ~string` is a compile-time error: `CaseInsensitiveStringOnNonCollection`. The `~` prefix is only valid immediately after `of` in a collection type position.

**`~string` in queue and stack.** While `~string` is most meaningful for sets (where deduplication and membership benefit from case-insensitive comparison), it is also valid as the inner type for `queue of ~string` and `stack of ~string`. The `contains` operator on these collections uses `OrdinalIgnoreCase` matching.

### Temporal and Business-Domain Inner Types

All temporal and business-domain types are valid collection inner types. Ordering and comparison capabilities vary:

**Temporal types:**

| Inner type | `.min`/`.max` | Notes |
|---|---|---|
| `date` | ✓ | Ordinal calendar ordering |
| `time` | ✓ | |
| `datetime` | ✓ | |
| `instant` | ✓ | UTC timeline ordering |
| `duration` | ✓ | |
| `period` | ✗ — type error | Equality-only (unqualified). Structural components are not directly comparable — `'1 month'` has no fixed length. See Decision #14 in temporal-type-system.md. |
| `period of 'date'` | ✗ — type error | Equality-only. Category constraint (years/months/weeks/days only). Used as arithmetic-safety qualifier for `date ± period` — not an ordering qualifier. |
| `period of 'time'` | ✗ — type error | Equality-only. Category constraint (hours/minutes/seconds only). Used as arithmetic-safety qualifier for `time ± period` — not an ordering qualifier. |
| `period in 'days'` | ✓ | Unit-qualified: all elements are single-basis periods. Ordering is by single component value (`.days`). Valid bases: `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds`. |
| `timezone` | ✗ — type error | Equality-only. No natural ordering across timezone identifiers. |
| `zoneddatetime` | ✗ — type error | Equality-only. NodaTime native equality compares instant + calendar + zone; two values representing the same moment in different timezones are different elements. |

**Business-domain types:**

| Inner type | `.min`/`.max` | Notes |
|---|---|---|
| `money in 'USD'` | ✓ — same currency guaranteed | Unqualified `money`: `.min`/`.max` are type errors (cross-currency ordering undefined; exchange rates are dynamic) |
| `quantity of 'length'` | ✓ — conversion-aware | `.min`/`.max` normalize to SI base unit for comparison; return original element. `'5 km' == '5000 m'` is `TRUE`. See §Qualified inner types. |
| `quantity in 'kg'` | ✓ — same unit guaranteed | Unqualified `quantity`: `.min`/`.max` are type errors |
| `price in 'USD'` | ✓ — same currency guaranteed | Unqualified `price`: `.min`/`.max` are type errors |
| `price in 'USD' of 'mass'` | ✓ — currency + dimension both pinned | Currency is static; mass conversions are static SI |
| `currency` | ✗ — type error | Equality-only |
| `unitofmeasure` | ✗ — type error | Equality-only |
| `dimension` | ✗ — type error | Equality-only |
| `exchangerate` | ✗ — type error | Equality-only |

**Qualified inner types**

`money`, `quantity`, and `price` support qualifiers that restrict elements to a homogeneous denomination and unlock ordering:

```precept
field Charges    as set of money in 'USD'         # all elements USD — .min/.max valid
field Weights    as set of quantity in 'kg'        # all elements kg — .min/.max valid
field Distances  as set of quantity of 'length'    # any length unit — .min/.max valid, conversion-aware
```

**Three qualification levels for `quantity`:**

| Declaration | Admitted elements | `.min`/`.max` | Equality |
|---|---|---|---|
| `set of quantity` | Any quantity (any dimension, any unit) | ✗ type error | Structural (value + unit) |
| `set of quantity of 'length'` | Any length unit (`km`, `miles`, `m`, `cm`, ...) | ✓ conversion-aware | Conversion-aware: `'5 km' == '5000 m'` |
| `set of quantity in 'km'` | `km` only | ✓ structural | Structural: `'5 km' == '5 km'` |

`of 'DimensionCode'` and `in 'UnitCode'` are mutually exclusive. `set of quantity of 'length' in 'km'` is a grammar error.

**Dimension-qualified ordering: how it works**

When a dimension qualifier is present (`of 'length'`), the runtime normalizes all elements to the SI base unit for comparison purposes only. The original element — with its original unit — is returned by `.min`/`.max`:

```precept
field Distances as set of quantity of 'length'

add Distances '5 km'
add Distances '3 miles'
add Distances '10 m'

# Distances.min  →  '10 m'     (10 m < 3 miles ≈ 4828 m < 5 km = 5000 m)
# Distances.max  →  '5 km'
```

Equality inside a dimension-qualified collection is conversion-aware. Adding `'5000 m'` to a set already containing `'5 km'` is a no-op — they are the same element:

```precept
add Distances '5000 m'   # no-op — '5 km' already present (5 km = 5000 m)
```

**Supported dimensions (v1):** `length`, `mass`, `volume`, `area`, `energy`, `pressure`, `temperature`.

SI base units for normalization: `m` (length), `kg` (mass), `L` (volume), `m2` (area), `J` (energy), `Pa` (pressure), `K` (temperature).

**Grammar disambiguation:** The two `of` tokens in `set of quantity of 'length'` are not ambiguous. The parser distinguishes them by lookahead: the collection-connector `of` is always followed by a type keyword; the dimension-qualifier `of` is always followed by a quoted string literal. Binding is `set of (quantity of 'length')`.

When `in 'CurrencyCode'` or `in 'UnitCode'` appears after the inner type in a collection declaration, it binds to the inner type, not the collection. The collection itself has no currency or unit qualifier — the qualifier is part of the inner scalar type specification.

Adding a value of the wrong denomination is a type error at the `add` site:
```precept
add Charges $50 EUR     # TypeMismatch — expected money in 'USD', got money in 'EUR'
```

### Token vocabulary

| Token | Keyword | Role |
|---|---|---|
| `SetType` | `set` | Set collection type (dual-use with action keyword `set`) |
| `QueueType` | `queue` | Queue collection type |
| `StackType` | `stack` | Stack collection type |
| `Of` | `of` | Collection inner type connector |
| `Tilde` | `~` | Case-insensitive inner type prefix |
| `Into` | `into` | Dequeue/pop target keyword |
| `Contains` | `contains` | Membership operator |
| `Mincount` | `mincount` | Minimum count constraint |
| `Maxcount` | `maxcount` | Maximum count constraint |

**`set` disambiguation:** The lexer always emits `TokenKind.Set` for the word `set`. In `ParseTypeRef()`, when followed by `of`, the parser treats it as the collection type `SetType`. Outside type position, `set` is the action keyword for scalar field assignment.

---

## Membership Operator

The `contains` operator tests whether a value is present in a collection. It is a binary infix operator with precedence 40, left-associative.

```precept
when RequestedFloors contains RemoveFloor.Floor
when Tags contains "urgent"
```

**Type rules:**

| Collection type | Value type | Result |
|-----------------|-----------|--------|
| `set of T` | `T` (or widens to `T`) | `boolean` |
| `queue of T` | `T` | `boolean` |
| `stack of T` | `T` | `boolean` |
| non-collection | — | type error |

**Case sensitivity:** `contains` on `set of string` is case-sensitive (ordinal). `contains` on `set of ~string` is case-insensitive (`OrdinalIgnoreCase`). `contains` on `queue of ~string` and `stack of ~string` is also case-insensitive.

**No proof requirement.** `contains` on an empty collection returns `false` — it is always safe to call without a count guard.

---

## Emptiness Safety

Precept enforces emptiness safety through proof obligations. Operations that would fault on an empty collection require the author to prove the collection is non-empty via a `.count > 0` guard in the enclosing `when` clause.

### Access proof obligations

| Expression | Proof required | Diagnostic if unguarded |
|---|---|---|
| `SetField.min` | `SetField.count > 0` | `UnguardedCollectionAccess` |
| `SetField.max` | `SetField.count > 0` | `UnguardedCollectionAccess` |
| `QueueField.peek` | `QueueField.count > 0` | `UnguardedCollectionAccess` |
| `StackField.peek` | `StackField.count > 0` | `UnguardedCollectionAccess` |

### Mutation proof obligations

| Action | Proof required | Diagnostic if unguarded |
|---|---|---|
| `dequeue F` | `F.count > 0` | `UnguardedCollectionMutation` |
| `dequeue F into G` | `F.count > 0` | `UnguardedCollectionMutation` |
| `pop F` | `F.count > 0` | `UnguardedCollectionMutation` |
| `pop F into G` | `F.count > 0` | `UnguardedCollectionMutation` |

### Safe operations (no proof required)

| Operation | Why safe |
|---|---|
| `.count` | Returns 0 on empty — no fault possible. |
| `contains` | Returns `false` on empty — no fault possible. |
| `add`, `remove` | No-op on irrelevant input — no fault possible. |
| `enqueue`, `push` | Always succeeds — adds to any collection. |
| `clear` | No-op on empty — no fault possible. |

### Guard pattern

The idiomatic guard pattern is a `when` clause on the transition row:

```precept
from Accepting on SeatNextParty when PartyQueue.count > 0
    -> set LastCalledParty = PartyQueue.peek
    -> dequeue PartyQueue into CurrentParty
    -> transition Seating
from Accepting on SeatNextParty
    -> reject "No party is currently waiting"
```

The proof engine recognizes `F.count > 0` in the `when` clause as sufficient proof for all `.peek`, `.min`, `.max`, `dequeue`, and `pop` operations on `F` within that transition row's action block. This is structurally guaranteed — if the guard is satisfied, the collection is non-empty, and the operation is safe.

**Philosophy:** Emptiness safety connects to the language's totality guarantee (§0.11 of the spec): every expression evaluates to a result, never to an undefined or fault state. The compiler proves emptiness safety or emits a diagnostic. Runtime fault checks exist only as defensive redundancy for paths the compiler has already proven unreachable.

---

## Constraint Catalog

| Constraint | Applicable to | Meaning |
|---|---|---|
| `mincount N` | `set`, `queue`, `stack` | Collection must contain at least N elements |
| `maxcount N` | `set`, `queue`, `stack` | Collection must contain at most N elements |
| `optional` | any field type (including collections) | Field may be unset; requires `is set` guard before use |
| `default [...]` | `set`, `queue`, `stack` | Initial value — list literal of scalar elements |

**Constraint validation rules:**

| Check | Diagnostic |
|---|---|
| `mincount` > `maxcount` on same field | `InvalidModifierBounds` |
| Negative `mincount` or `maxcount` | `InvalidModifierValue` |
| Duplicate modifier | `DuplicateModifier` |
| `mincount`/`maxcount` on scalar field | `InvalidModifierForType` |
| `min`/`max`/`nonnegative`/`notempty`/`minlength`/`maxlength`/`maxplaces` on collection field | `InvalidModifierForType` |

**Scalar constraints do not apply to collections.** `notempty`, `min`, `max`, `minlength`, `maxlength`, `maxplaces`, `nonnegative`, `positive`, `nonzero`, and `ordered` are all type errors when applied as field-level modifiers on collection fields (e.g., `field Tags as set of string ordered` is invalid). Collections have their own constraint vocabulary: `mincount` and `maxcount`. Note that `ordered` on the *inner `choice of T(...)` type* is valid — `field Priorities as set of choice of string("low", "medium", "high") ordered` declares an ordered-choice inner type, not a collection-level modifier.

---

## List Literal Defaults

Collections support list literal syntax `[...]` for default values. List literals are only valid in the `default` clause position.

```precept
field Tags as set of string default []
field Priorities as set of number default [1, 2, 3]
field InitialQueue as queue of string default ["first"]
```

**Parser:** `LeftBracket` → `ListLiteralExpression`. Elements are comma-separated scalar values.

**Type checking:**

| Check | Fires when | Diagnostic |
|---|---|---|
| Element type mismatch | List element type doesn't match collection element type | `TypeMismatch` |
| List in non-default position | List literal used outside a `default` clause | `ListLiteralOutsideDefault` |
| Empty list as default | Valid — initializes an empty collection | — |

**Design:** List literals are deliberately restricted to `default` clauses. They are not general-purpose expressions — you cannot assign a list literal via `set` or use one in a `when` guard. This keeps the action surface focused on element-level operations (`add`, `remove`, `enqueue`, `push`) rather than bulk replacement.

---

## String Interpolation

Collections are a type error inside string interpolation. Each `{expr}` inside `"..."` is type-checked independently, and collection-typed expressions emit `InvalidInterpolationCoercion`.

```precept
# Type error: collection in interpolation
set Message = "Tags: {Tags}"        # InvalidInterpolationCoercion

# Correct: use scalar accessors
set Message = "Tag count: {Tags.count}"
```

This restriction exists because collections have no canonical string representation — they are unordered (set) or ordered-by-insertion (queue, stack), and the language does not define a serialization format for them.

---

## Action Summary

| Action | Set | Queue | Stack | Value | Proof |
|--------|-----|-------|-------|-------|-------|
| `add F Expr` | ✓ | ✗ | ✗ | `T` | — |
| `remove F Expr` | ✓ | ✗ | ✗ | `T` | — |
| `enqueue F Expr` | ✗ | ✓ | ✗ | `T` | — |
| `dequeue F` | ✗ | ✓ | ✗ | — | `.count > 0` |
| `dequeue F into G` | ✗ | ✓ | ✗ | — | `.count > 0` |
| `push F Expr` | ✗ | ✗ | ✓ | `T` | — |
| `pop F` | ✗ | ✗ | ✓ | — | `.count > 0` |
| `pop F into G` | ✗ | ✗ | ✓ | — | `.count > 0` |
| `clear F` | ✓ | ✓ | ✓ | — | — |

**Cross-kind errors:** Applying an action to the wrong collection kind emits `CollectionOperationOnScalar`. Applying a collection action to a scalar field emits `CollectionOperationOnScalar`. Applying a scalar action (`set =`) to a collection field emits `ScalarOperationOnCollection`.

---

## Accessor Summary

| Member | Set | Queue | Stack | Returns | Proof |
|--------|-----|-------|-------|---------|-------|
| `.count` | ✓ | ✓ | ✓ | `integer` | — |
| `.min` | ✓ (T orderable) | ✗ | ✗ | `T` | `.count > 0` |
| `.max` | ✓ (T orderable) | ✗ | ✗ | `T` | `.count > 0` |
| `.peek` | ✗ | ✓ | ✓ | `T` | `.count > 0` |

---

## Proposed Extensions

> **These are design proposals, not shipped features.** They represent research findings from the collection iteration (frank-6) and collection rules (frank-7) investigations. No syntax below is implemented. All proposals are subject to design review and owner approval before implementation.

### Quantifier Predicates

**Motivation:** The current collection surface provides membership testing (`contains`) and cardinality (`.count`, `mincount`, `maxcount`), but no way to express constraints over the *elements* of a collection. Business rules like "all items must be positive" or "at least one reviewer matches the submitter" require element-level predicates.

**Research basis:** CEL (Common Expression Language) provides 5 parse-time macros — `all`, `exists`, `exists_one`, `filter`, `map` — all non-Turing-complete, expanding at parse time over finite collections. OPA/Rego uses universal/existential quantification over sets. SQL uses `ALL`/`ANY`/`EXISTS` over subqueries.

**Recommendation:** Bounded quantifier predicates only — `each`, `any`, `no`. No general loops, no `map`/`filter`/`reduce`.

**Rationale:** Quantifiers are logical predicates over finite collections, not iteration constructs. They are provably terminating — the collection is finite and no mutation occurs during evaluation. This distinguishes them from general iteration, which the language philosophy explicitly excludes (§0.4.1 "No iteration constructs").

**Approved syntax:**

```precept
# Universal — all elements must satisfy predicate
rule each item in Items (item > 0) because "All items must be positive"

# Existential — at least one element satisfies predicate
when any r in Reviewers (r == Approve.ReviewerName)

# Negated existential — no element satisfies predicate
rule no a in Amounts (a < 0) because "No negative amounts permitted"

# Compound guard — predicate scopes cleanly within parens
from Submitted on Approve
    when any r in Reviewers (r == Approve.ReviewerName) and MissingDocuments.count == 0
    -> set ApprovedBy = Approve.ReviewerName
    -> transition Approved
```

**Syntax form:** `quantifier binding in Collection (predicate)` — the binding variable is a bare identifier named by the author and locally scoped to the parenthesized predicate. The predicate is a boolean expression.

**Grammar sketch:**

```
QuantifierExpr  :=  QuantifierKind Identifier in CollectionField '(' BoolExpr ')'
QuantifierKind  :=  each | any | no
```

**Lexer note:** `any` is already a reserved keyword in the Precept lexer. `each` and `no` require new lexer entries. The previously reserved `all` keyword is superseded by `each` — `each` is grammatically correct ("each item in Items") where `all` would be broken English ("all item in Items").

**Decided (Q2 — three keywords):** The language ships `each`, `any`, `no` as three distinct keywords. Three keywords are more discoverable and read more naturally in business rules. `no` reads better than `not any` in business rule context.

**Decided (Q3 — named binding):** The quantifier syntax uses author-named binding variables (`item`, `r`, `a`). Named bindings are explicit and avoid nesting ambiguity. A fixed `it` pronoun creates scoping problems if quantifiers are ever nested.

**What is deliberately held back:** `map`, `filter`, `reduce`, `sum`, and any transformation that produces a new collection or aggregate value. These require structured collection element types (a larger feature surface) and would cross the line from predicate into computation.

**Philosophy compatibility:** Quantifiers are bounded predicates — they assert a property of every/some/no element in a finite collection. They do not mutate state, do not introduce loop variables, and terminate in bounded time. They are consistent with §0.4.1 IF the spec is amended to explicitly distinguish bounded predicates from general iteration. This is an open question (see below).

### Additional Field Constraints

**Research basis:** Survey of FluentAssertions, Zod, Valibot, FluentValidation, OPA/Rego, Bean Validation (JSR-380), and SQL constraints. A 6-category taxonomy was developed:

1. **Cardinality** — `mincount`/`maxcount` (already shipped)
2. **Membership/value** — `contains` (already shipped)
3. **Element-shape (quantified predicates)** — requires quantifiers (proposed above)
4. **Ordering** — relative order of elements (partial path: `choice of T(...) ordered` as an inner type already enables element-level comparison via declaration-position rank, making `.min`/`.max` valid and enabling quantifier predicates like `each x in Items (x >= "medium")` over ordered-choice sets; the remaining gap is ordering *constraints* on element sequences — e.g., "elements must be monotonically increasing" — which has no path yet)
5. **Cross-collection** — relationships between two collection fields
6. **Aggregate-relational** — `.count`, `.min`, `.max` in rule expressions (already shipped)

**Proposed 3-layer rollout:**

**Layer A (first priority): Field constraint keywords** — ~70% coverage with zero new expression constructs.

| Keyword | Applicable to | Meaning | Proof engine |
|---|---|---|---|
| `unique` | `queue`, `stack` | No duplicate elements permitted | Obligation diagnostic. Redundant (tautological) on `set` — compiler may warn. |
| `notempty` | `set`, `queue`, `stack` | Collection must contain at least one element. Equivalent to `mincount 1`. | Statically verifiable in many cases. |
| `subset` | `set of T` × `set of T` | All elements of this set are in the target set | Static when T is `choice`-typed; obligation diagnostic for `string`. |
| `disjoint` | `set of T` × `set of T` | No elements of this set are in the target set | Same provability boundary as `subset`. |

**`unique` on `set`:** Sets already enforce uniqueness by definition. Applying `unique` to a `set` field is semantically redundant. The recommended behavior is a compiler warning (`RedundantModifier`) — parallel to `nonnegative` + `positive` on the same numeric field.

**`notempty` on collections:** `notempty` is currently a string-only constraint. Extending it to collections creates a natural parallel — `notempty` means "this thing must not be empty," whether it's a string (`.length > 0`) or a collection (`.count > 0`). The alternative is to continue requiring `mincount 1`, which works but is verbose for the most common cardinality constraint.

**`subset`/`disjoint` form (open question):** These could be field modifiers in the declaration (`field Selected as set of string subset AllowedValues`) or rule expressions in the body (`rule Selected subset AllowedValues`). The declaration form is more concise; the rule form is more flexible (can reference computed or conditional relationships).

**Layer B (second priority): Quantifier predicates** — `each`/`any`/`no` as described above.

**Layer C (deferred): Dedicated `check` blocks** — readability-only sugar, no new capability beyond what rules + quantifiers provide. Deferred until the need becomes clear from real-world usage.

### Open Questions

1. **Philosophy compatibility of quantifiers.** Does §0.4.1 "No iteration constructs" philosophically allow bounded quantifiers? Should the spec be amended to explicitly distinguish bounded predicates from general iteration? The distinction is meaningful — quantifiers assert truth over a finite domain, they don't iterate — but the current spec wording doesn't draw this line.

2. **Quantifier priority.** Should quantifiers ship before or after proof engine / graph analyzer work that's already in flight? Quantifiers introduce proof obligations — the compiler must verify that the predicate holds for all elements, or emit a diagnostic. This work depends on the proof engine's ability to reason about collection elements.

3. **Collection `notempty` keyword.** Should `notempty` on a collection reuse the same keyword as string `notempty`, or get a distinct keyword? Reusing the keyword creates a natural parallel ("`notempty` means non-empty, regardless of what it's applied to"). A distinct keyword avoids potential confusion if the semantics diverge in the future.

4. **`unique` on `set` type.** Should `unique` on a `set` produce a warning (redundant — sets can't have duplicates) or silently accept? A warning is parallel to `RedundantModifier` for numeric constraints. Silent acceptance is more forgiving but allows conceptual mistakes to pass unnoticed.

5. **Cross-collection constraint form.** Should `subset`/`disjoint` be field modifiers (in the declaration) or rule expressions (in rule/ensure/when bodies)? Field modifiers are concise and declarative. Rule expressions are more flexible — they can be conditional, they can reference event args, and they can compose with boolean logic.

6. **Proposal granularity.** One combined proposal for all collection extensions, or two separate increments — quantifiers as one, field-level constraints as another? The 3-layer recommendation suggests separate increments (Layer A ships first, Layer B second), but the formal proposal structure is TBD.

7. **Collection type expansion.** After quantifier predicates and field constraints ship, which collection type — if any — should be next? The §Proposed Additional Types section below surveys candidates. Before any of these could become a formal GitHub issue, the following research would be needed: (a) a concrete corpus of `.precept` files that cannot express a real business rule without the proposed type, (b) a proof engine impact assessment for the type's mutation operations, (c) an inner type compatibility analysis (which scalar types are valid, which are type errors), and (d) a philosophy review confirming the type reads as domain declaration, not general-purpose programming.

8. ~~Temporal and business-domain types as inner types~~ — **Resolved.** The `ScalarType` production now includes all temporal and business-domain types. `percentage` was a stale reference — this type does not exist in the business-domain type system. See §Temporal and Business-Domain Inner Types for the full expansion. Locked Decision.

---

## Proposed Additional Types

> **This is research and proposal, not shipped behavior.** The candidates below were evaluated against external collection systems and filtered through Precept's philosophy: prevention not detection, deterministic inspectability, proof engine safety, keyword-anchored flat statements, business-analyst readability, and non-Turing-completeness. No syntax below is implemented.

### Evaluation Criteria

Every candidate must pass all six filters before advancing:

1. **Unlocks a business rule** that `set`/`queue`/`stack` cannot express today
2. **Deterministic and inspectable** — no hidden ordering surprises, no ambient state
3. **Proof engine can reason about safety** — access patterns are statically verifiable
4. **Fits Precept's keyword-anchored flat-statement style** — no nested expressions or builder chains
5. **Business analyst readable** — the keyword name communicates behavior without documentation
6. **Non-Turing-complete** — finite, bounded operations; no unbounded recursion or general iteration

### Candidate 1: `bag of T` (multiset)

**What it is:** Like `set` but allows duplicate elements. Each element has an associated count. Supports `add` (increment count), `remove` (decrement count), `.countof(element)` accessor.

**Business scenario:** An inventory system tracks item quantities. A shopping cart allows multiple units of the same SKU. A claims processor counts how many times a particular diagnosis code appears.

```precept
field CartItems as bag of string

from Shopping on AddItem
    -> add CartItems AddItem.SKU
    -> no transition

from Shopping on RemoveItem when CartItems contains RemoveItem.SKU
    -> remove CartItems RemoveItem.SKU
    -> no transition

rule CartItems.countof("hazmat-item") <= 3
    reason "No more than 3 hazardous items per order"
```

**Proof engine implications:** `.countof(element)` always returns a non-negative integer (0 if absent) — always safe, no proof obligation. `.count` returns total element count including duplicates. The proof engine must understand that `remove` on a bag decrements rather than deletes (the element persists until count reaches 0).

**Grammar fit:**

```
field F as bag of T
```

Natural keyword — "bag" is the established mathematical term for multiset, and is more intuitive to a business analyst than "multiset."

**Action surface:** Reuses `add`, `remove`, `clear`. New accessor: `.countof(element)` — returns integer count of a specific element.

**Priority:** **High.** This unlocks counting and quantity-tracking rules that `set` (which collapses duplicates) and `queue`/`stack` (which don't expose per-element counts) cannot express. Many real business domains need "how many of X" as a first-class concept.

### Candidate 2: `list of T`

**What it is:** An ordered sequence with stable positions, duplicates allowed, and index access. Differs from all existing types:
- `queue` — ordered, but FIFO-only, no random access
- `stack` — ordered, but LIFO-only, no random access
- `log` — ordered + positional read, but append-only (no removal)
- `set` — unordered, no duplicates
- `bag` — unordered, duplicates allowed

`list` is the only type with: ordered positions + arbitrary insertion AND arbitrary positional removal.

**Business scenario:** An approval chain where reviewers can recuse themselves — ordered sequence, arbitrary removal by value or position. Ordered step lists where steps can be retracted. Any domain where sequence matters and elements can be withdrawn.

**Proof engine implications:**
- `.at(N)` — tractable. Requires index-bounds guard (`when Items.count > N`), same pattern as emptiness guards on `.peek`, extended to two-sided bounds check. Author supplies the guard; proof engine discharges from it.
- `insert` / `remove-at` — **shift subsequent positions**. Once element at index 1 is removed, every proof about "element at index 3" is stale. `log`'s append-only invariant gives the proof engine stable positions. `list`'s arbitrary mutation does not. Per-access index-bounds safety is achievable; cross-mutation positional invariants are not. This is a real cost.

**Grammar fit:**

```
field F as list of T
```

Clean. Reuses `of`. No new declaration keywords.

**Action surface:** `append`, `insert F at N Expr` (insert at position), `remove F Expr` (first-occurrence removal), `remove-at F N` (positional removal), `clear`. Accessors: `.at(N)`, `.first`, `.last`, `.count`.

**Relation to `bag` and `log`:**
- vs `bag`: orthogonal. `bag` tracks frequencies (unordered); `list` tracks positions (ordered). Neither subsumes the other.
- vs `log`: deliberate non-overlap. `log` covers "immutable record that accumulates" — positional read (`.at(N)`, `.first`, `.last`) IS in `log`. `list` adds the one thing `log` prohibits: arbitrary removal. If `log` satisfies real positional-read use cases in practice, the incremental case for `list` weakens.

**Priority:** **Low.** Not a reject — no philosophy violation (unlike ring buffer's silent eviction). But the genuine incremental territory over `log` is narrow (arbitrary removal), the proof cost of positional-mutation is real, and mutable-ordered-list business scenarios are rarer than `bag`/`log`/`map` frequency. Right sequencing: evaluate after `log` ships.

### Candidate 3: `deque of T`

**What it is:** Double-ended queue. Supports push/pop at both front and back.

**Business scenario:** A customer service queue that supports both normal FIFO processing and priority escalation to the front. A browser-history model that adds to the back and can trim from either end.

```precept
field ServiceQueue as deque of string

from Active on EnqueueNormal
    -> push-back ServiceQueue EnqueueNormal.CustomerId
    -> no transition

from Active on Escalate
    -> push-front ServiceQueue Escalate.CustomerId
    -> no transition

from Active on ServeNext when ServiceQueue.count > 0
    -> pop-front ServiceQueue into CurrentCustomer
    -> transition Serving
```

**Proof engine implications:** Same emptiness obligations as queue/stack — `pop-front` and `pop-back` require `.count > 0`. Two peek variants: `.peekfront` and `.peekback`, each requiring the same guard.

**Grammar fit:**

```
field F as deque of T
```

**Action surface:** New keywords required: `push-front`, `push-back`, `pop-front`, `pop-back`. New accessors: `.peekfront`, `.peekback`. This is 4 new action keywords and 2 new accessors — a significant surface expansion.

**Priority:** **Low.** The double-ended access pattern is rare in business-rule domains. Most real scenarios are either FIFO (queue) or LIFO (stack). The escalation pattern can be modeled with two separate queues (priority + normal) or a priority queue. The keyword surface cost is high relative to the business-rule unlock.

### Candidate 4: `priorityqueue of T priority P`

**What it is:** A queue where elements are dequeued by priority rather than insertion order. Each element has two axes: a value (type `T`) and a priority (type `P`). The priority type `P` must be orderable (numeric or `choice of T(...) ordered`). Dequeue always removes the element with the best priority according to the declared sort direction.

**Business scenario:** A claims triage system where claims are processed by severity. A work-item queue where urgent items bypass the normal order.

```precept
field ClaimQueue as priorityqueue of string priority integer descending

from Receiving on FileClaim
    -> enqueue ClaimQueue FileClaim.ClaimId priority FileClaim.Severity
    -> no transition

from Processing on ProcessNext when ClaimQueue.count > 0
    -> dequeue ClaimQueue into CurrentClaim priority CurrentPriority
    -> transition Reviewing
```

#### Priority Direction

The sort direction determines which priority value is dequeued first. Direction is declared at the field level and can be overridden per-operation at the dequeue site.

**Declaration-level direction** (sets the default):

```precept
field UrgentQueue as priorityqueue of string priority integer descending
field WorkItems as priorityqueue of string priority integer ascending
field TriageQueue as priorityqueue of string priority integer   # default: ascending
```

- `ascending` (default) — lowest priority value dequeued first. Natural for numbered priority levels where 1 = highest urgency.
- `descending` — highest priority value dequeued first. Natural for severity scores where higher = more urgent.

If no direction modifier is specified, the default is `ascending`.

**Operation-level override** (at the dequeue site):

```precept
# Uses the declaration-level default (descending)
-> dequeue UrgentQueue into NextClaim priority NextPriority

# Overrides to ascending for this specific dequeue
-> dequeue UrgentQueue ascending into NextClaim priority NextPriority
```

The direction modifier follows the collection name and precedes `into`. This fits Precept's modifier-follows-target grammar — the modifier qualifies the dequeue operation on that collection.

**Design rationale:** Supporting both sites gives authors a clean default for the common case while allowing exceptional dequeue logic without redeclaring the field. This parallels how `ordered` modifies the `choice` type at the declaration site but the ordering is always available at the expression site.

#### Peek with Priority

`.peek` returns the element value (type `T`) of the front-of-queue item — consistent with `.peek` on `queue`. A separate `.peekpriority` accessor returns the priority value (type `P`) of the same front-of-queue item.

| Accessor | Returns | Guard required | Description |
|---|---|---|---|
| `.peek` | `T` | `.count > 0` | Element value of the highest-priority item |
| `.peekpriority` | `P` | `.count > 0` | Priority value of the highest-priority item |
| `.count` | `integer` | — | Number of elements in the queue |

```precept
from Processing on CheckNext when ClaimQueue.count > 0
    -> set NextClaimId = ClaimQueue.peek
    -> set NextSeverity = ClaimQueue.peekpriority
    -> no transition
```

**Design rationale:** Two separate accessors (`.peek` and `.peekpriority`) rather than a tuple or compound return. This is consistent with Precept's flat accessor model — every accessor returns a single scalar value. No existing accessor returns multiple values, and introducing compound returns would require a new language mechanism.

#### Dequeue with Priority Capture

The `dequeue ... into` form is extended with an optional `priority` capture clause:

```precept
-> dequeue ClaimQueue into CurrentClaim priority CurrentPriority
```

- `CurrentClaim` receives the dequeued element value (type `T` — here `string`).
- `CurrentPriority` receives the dequeued element's priority (type `P` — here `integer`).
- Both target fields must be declared and type-compatible.
- The `priority` capture is optional — `dequeue ClaimQueue into CurrentClaim` remains valid and discards the priority value.

Full form with direction override:

```precept
-> dequeue ClaimQueue ascending into CurrentClaim priority CurrentPriority
```

#### Quantifier Predicates on Priority Axis

When a quantifier (`each`/`any`/`no`) iterates over a `priorityqueue`, the binding variable exposes two fields:

- `.value` — the element (type `T`)
- `.priority` — the priority value (type `P`)

This is a meaningful design distinction: for single-type collections (`set`, `queue`, `stack`), the binding variable IS the element (a bare scalar). For `priorityqueue`, the binding variable is a **two-field projection** exposing both axes.

```precept
# Assert no low-priority claims in the queue
rule no claim in ClaimQueue (claim.priority < 2)
    reason "All claims must have priority 2 or higher"

# Existential check on element value
when any claim in ClaimQueue (claim.value == TargetClaimId)

# Compound predicate across both axes
rule no claim in ClaimQueue (claim.priority < 3 and claim.value == "")
    reason "High-priority claims must have a claim ID"
```

When the priority axis is choice-typed, the declaration is the constraint — no rule needed. A claim with a priority outside the declared choices is a type error at the `enqueue` site:

```precept
field TriageQueue as priorityqueue of ClaimId priority choice of string("normal", "high")
```

A `"critical"` priority claim cannot enter `TriageQueue` — the type prevents it.

**Grammar for priorityqueue quantifier binding:**

```
# For priorityqueue: binding exposes .value and .priority
QuantifierExpr  :=  QuantifierKind Identifier in PriorityQueueField '(' BoolExpr ')'
# Inside BoolExpr, Identifier.value : T and Identifier.priority : P
```

**Note:** For `map of K to V`, the analogous binding shape would be `.key` and `.value`. The principle generalizes: two-type-parameter collections expose a two-field binding in quantifier scope.

#### Grammar Fit — Generalized Two-Type-Parameter Pattern

The `priorityqueue` and `map` types both take two type parameters connected by a role keyword. The generalized grammar:

```
TwoParamCollectionType :=
    priorityqueue of ScalarType priority ScalarType TypeQualifier?
  | map of ScalarType to ScalarType TypeQualifier?

DirectionModifier := ascending | descending
```

Both `priority` and `to` are **role-connector keywords** that introduce the secondary type parameter. The `of` keyword introduces the primary type. This is the generalized pattern for all two-type-parameter collection types.

For `priorityqueue`, the full field declaration grammar is:

```
PriorityQueueDecl :=
    field Identifier as priorityqueue of ScalarType priority ScalarType DirectionModifier?
```

Where `DirectionModifier` defaults to `ascending` if omitted.

**Proof engine implications:** Emptiness obligations identical to `queue`. The priority value introduces a secondary type requirement — the `priority` argument on `enqueue` must match the declared priority type `P`, and `P` must be orderable. The proof engine must understand that dequeue order is by priority (respecting direction), not insertion, which affects reasoning about which element `.peek` and `.peekpriority` return. The direction modifier is a static property of the field (or a per-operation override) — it does not introduce runtime non-determinism.

**Action surface:** Extends `enqueue` with required `priority` clause. Extends `dequeue into` with optional `priority` capture clause. Adds `ascending`/`descending` as direction modifiers (contextual keywords in type-position and dequeue-position). Reuses `clear`, `.count`. Adds `.peekpriority` accessor. `priority`, `ascending`, `descending` are contextual keywords.

**Priority:** **Medium.** Priority-based processing is a real business pattern, and the two-type-parameter grammar generalizes cleanly with `map`. The proof surface is manageable — emptiness obligations are identical to `queue`, and direction is a static property. The quantifier binding shape (`.value`/`.priority`) sets a precedent for all future two-type-parameter collections.

#### Resolved Design Questions

The following questions from frank-14 are now resolved:

1. **~~Priority direction~~** — RESOLVED. Direction is declared at the field level (`ascending`/`descending` modifier, default `ascending`) and can be overridden per-operation at the dequeue site. The modifier follows its target in both positions, consistent with Precept's grammar conventions.

2. **~~Peek behavior~~** — RESOLVED. `.peek` returns the element value (type `T`); `.peekpriority` returns the priority value (type `P`). Two separate scalar accessors, consistent with the flat accessor model.

3. **~~Priority-type constraints~~** — RESOLVED. Quantifier predicates (`each`/`any`/`no`) apply to the priority axis via a two-field binding: `binding.value` (element) and `binding.priority` (priority value). This generalizes to all two-type-parameter collections.

4. **~~Dequeue capture~~** — RESOLVED. Approved syntax: `dequeue F into Target priority PriorityTarget`. The `priority` capture is optional — omitting it discards the priority value.

5. **~~Grammar alignment with `map`~~** — RESOLVED. Both `priorityqueue` and `map` use the generalized two-type-parameter pattern: `of PrimaryType role-connector SecondaryType`. Role connectors are `priority` and `to` respectively.

#### Open Questions (New)

1. **Quantifier binding shape for single-type vs. two-type collections.** When a quantifier iterates a `set of string`, the binding IS the string. When it iterates a `priorityqueue of string priority integer`, the binding exposes `.value` and `.priority`. This means the type checker must distinguish binding shapes by collection kind. Is this implicit (the type checker infers the shape from the collection type) or explicit (the author declares the binding shape)? Implicit is simpler and consistent with how the binding already adapts to the collection's inner type.

2. **`ascending`/`descending` keyword reuse.** These keywords may also be relevant for future ordering features beyond `priorityqueue`. Should they be reserved broadly as ordering modifiers, or scoped specifically to `priorityqueue` and `dequeue`?

### Candidate 5: `log of T`

**What it is:** An append-only ordered sequence. Elements can be added but never removed. Supports `.count`, positional read (`.at(index)`), `.last`, and `.first`. Models audit trails, event histories, and compliance records.

**Business scenario:** A loan application must maintain an immutable record of all status changes for regulatory compliance. An insurance claim tracks every assessment note chronologically.

```precept
field AuditTrail as log of string

from any on any
    -> append AuditTrail "{CurrentState} -> {Event.Name}: {Event.Reason}"
    -> no transition

rule AuditTrail.count > 0
    reason "Every entity must have at least one audit entry"

from Review on Examine when AuditTrail.count > 0
    -> set LastAction = AuditTrail.last
    -> no transition
```

**Proof engine implications:** `append` is always safe (no precondition). `.last` and `.first` require `.count > 0` (same obligation pattern as `.peek`). `.at(index)` requires `index >= 0 and index < F.count` — this introduces an index-bounds proof obligation, which is a new category of safety proof. The append-only invariant simplifies reasoning: the proof engine knows that `.count` is monotonically non-decreasing within an event.

**Grammar fit:**

```
field F as log of T
```

"Log" is immediately understandable to a business analyst — "this is the audit log."

**Action surface:** One new keyword: `append`. New accessors: `.first`, `.last`, `.at(index)`. No removal operations exist by design.

**Priority:** **High.** Append-only audit trails are a pervasive business requirement across regulated industries (finance, insurance, healthcare). No existing collection type can model "add but never remove" — `queue` allows `dequeue`, `stack` allows `pop`, and `set` allows `remove`. The `log` type makes the immutability guarantee structural, which is exactly Precept's philosophy of prevention over detection.

**Note on `list of T` overlap:** The positional-read accessors — `.at(N)`, `.first`, `.last` — appear in both `log` and the separately evaluated `list of T` candidate. The overlap is deliberate: `log` covers the common case — read-only positional access on an accumulating record with a stable, monotonically growing index space. `list of T` is the separate evaluation for mutable ordered sequences, adding the one operation `log` prohibits: arbitrary positional removal. If `log` satisfies real positional-read use cases in practice, the incremental case for `list` weakens.

### Candidate 6: `map of K to V`

**What it is:** A key-value association. Each key maps to exactly one value. Supports set-by-key, get-by-key, contains-key, and remove-by-key.

**Business scenario:** A policy record maps coverage types to their limits. A fee schedule maps transaction types to fee amounts. A configuration entity maps setting names to values.

```precept
field CoverageLimits as map of string to decimal

from Draft on SetCoverage
    -> put CoverageLimits SetCoverage.CoverageType = SetCoverage.Limit
    -> no transition

from Active on CheckCoverage when CoverageLimits containskey CheckCoverage.CoverageType
    -> set CurrentLimit = CoverageLimits for CheckCoverage.CoverageType
    -> no transition

rule CoverageLimits.count <= 10
    reason "No more than 10 coverage types per policy"
```

**Proof engine implications:** `F for key` requires a `containskey` guard — same pattern as emptiness proofs but keyed. This is a new proof obligation category: key-presence safety. `put` is always safe (creates or overwrites). `removekey` requires no guard (no-op if absent, like `remove` on `set`). The proof engine must track key-presence from `containskey` guards in `when` clauses.

**Grammar fit:**

```
field F as map of K to V
```

Introduces `to` as a type-position keyword connecting key and value types. Both `K` and `V` must be scalar types. The `to` keyword is new in this context but reads naturally.

> **Locked Decision — Access keyword: `for`.** Map value access uses the infix keyword `for`: `CoverageLimits for CheckCoverage.CoverageType`. Considered: `at`. Rejected: `at` would create ambiguity with future temporal constructs and reads less naturally with map field names in business context.

**Action surface:** New keywords: `put`, `removekey`, `containskey`, `for` is the infix key-access keyword. `containskey` parallels `contains` on sets. `.count` reuses existing accessor. `.keys` and `.values` could return sets for use in `contains` and quantifier expressions.

**Priority:** **High.** Key-value association is fundamental to business modeling. Configuration tables, fee schedules, lookup mappings, and per-category settings are everywhere. Today, modeling these in Precept requires either multiple parallel fields or external application logic — both of which break the one-file-complete-rules guarantee. A `map` type keeps the association inside the contract.

### Rejected: Ring Buffer / Circular Buffer

**Motivation:** Fixed-size FIFO that automatically discards the oldest element when capacity is exceeded. Use cases: "last 5 approvals," "recent 10 events," bounded audit history. Python's `collections.deque(maxlen=N)` and Apache Commons' `CircularFifoQueue` serve this pattern.

**Proposed syntax:**

```precept
field RecentApprovals as ringbuffer of string capacity 5

# enqueue always succeeds — if full, oldest element is silently discarded
from Active on LogAction
    -> enqueue AuditTrail LogAction.Description
    -> no transition
```

**Proof engine implications:** The auto-eviction is the fatal flaw. Current `queue` `enqueue` never discards. A ring buffer's `enqueue` silently destroys data — the oldest element vanishes without the author writing a `dequeue`. This is implicit mutation. The proof engine cannot reason about what was lost or verify post-conditions that depend on evicted elements.

**Grammar fit:** `field F as ringbuffer of T capacity N`. Introduces `capacity` as a type-position keyword (see Bounded Collection rejection below — `capacity` is a synonym for `maxcount`).

**Action surface:** Reuses `enqueue`, `dequeue`, `.peek`, `.count`. No new keywords, but `enqueue` gains hidden side effects (eviction) that violate the explicit-action contract.

**Priority/Recommendation:** **Reject.** Silent eviction directly violates the inspectability principle — "nothing is hidden" (philosophy §What makes it different). When an `enqueue` on a full ring buffer discards the oldest element, the author didn't write a `dequeue` — data disappeared implicitly. The bounded-history use case is real but must be modeled with `queue of T` + `maxcount N` + explicit `dequeue` before `enqueue` when at capacity. That pattern makes the eviction visible and provable.

### Rejected: Bounded Collection (`capacity` modifier)

**Motivation:** Enforce maximum size at declaration time via a `capacity` modifier on existing types (`queue of T capacity N`, `set of T capacity N`) rather than via `maxcount` constraints.

**Proposed syntax:**

```precept
# These two would be equivalent:
field Tags as set of string capacity 10
field Tags as set of string maxcount 10
```

**Proof engine implications:** Identical to `maxcount N` — the proof engine already handles cardinality bounds. A `capacity` modifier compiles to the same constraint.

**Grammar fit:** `capacity N` as an alternative to `maxcount N` in the constraint position.

**Action surface:** None — pure syntactic sugar.

**Priority/Recommendation:** **Reject.** `maxcount N` already provides this capability. A `capacity` synonym adds language surface cost with zero capability gain. Precept's philosophy favors a small, precise surface (§0.4.1) — adding a second spelling for the same constraint violates that principle. If the naming is a concern, it's a keyword-alias discussion, not a type discussion.

### Rejected: Multimap

**Motivation:** Key maps to a collection of values — one-to-many relationships. Use cases: category-to-items, department-to-employees, tag-to-documents. Guava's `Multimap`, Scala's `Map[K, List[V]]` idiom.

**Proposed syntax:**

```precept
field DepartmentMembers as multimap of string to string

-> put DepartmentMembers "engineering" "alice"
-> put DepartmentMembers "engineering" "bob"

when DepartmentMembers["engineering"] contains "alice"
```

**Proof engine implications:** Strictly harder than `map`. Each key maps to a collection, so the proof engine must reason about nested collection emptiness, per-key cardinality, and cross-key relationships. This compounds the already-complex `map` proof surface.

**Grammar fit:** `field F as multimap of K to V`. Reuses the `of ... to` connector from `map`.

**Action surface:** Same as `map` (`put`, `removekey`, `containskey`, `for`) but `F for key` returns a collection, not a scalar — introducing nested collection semantics that Precept explicitly excludes.

**Priority/Recommendation:** **Reject.** A multimap is a collection of collections behind a key lookup — two levels of indirection that enter general-purpose data structure territory. Even if `map` ships, `multimap` adds nested collection semantics that Precept's flat-statement philosophy prohibits. The one-to-many pattern is better served by explicit set fields (`field EngineeringTeam as set of string`, `field SalesTeam as set of string`) with cross-collection constraints (`subset`, `disjoint`) relating them.

### Rejected: `sortedset of T`

**Motivation:** Like `set` but maintains elements in sorted order by the inner type's natural ordering. `.min` and `.max` were claimed to be "always-safe" when combined with `notempty`, and iteration order would be deterministic.

**Why rejected:** No Precept construct can observe sorted iteration order. Quantifiers (`each`, `any`, `no`) are boolean predicates — order-independent by definition. `.min`/`.max` return the minimum and maximum value regardless of storage order; the proof obligation for safe access is discharged by `notempty` alone. `set of T notempty` is proof-identical to `sortedset of T notempty` — the sorted storage contributes nothing to the safety guarantee. The action surface (`add`, `remove`, `clear`) is identical to `set`.

The sole difference between `sortedset` and `set` that is visible at the language surface is the type name. A type whose behavior is indistinguishable from another type by any language construct is not a type — it is an implementation detail wearing a type costume. Tree-backed storage with O(log n) inserts buys no benefit in a DSL governing business contracts where collections are small.

If Precept ever adds ordered-iteration constructs that make sorted order observable, `sortedset` can be re-evaluated at that time with an actual consuming construct to justify it.

### Priority Summary

| Candidate | Priority | Rationale |
|---|---|---|
| `bag of T` | **High** | Unlocks quantity tracking — pervasive in commerce, inventory, counting rules |
| `log of T` | **High** | Unlocks append-only audit trails — pervasive in regulated industries, aligns perfectly with prevention philosophy |
| `map of K to V` | **High** | Unlocks key-value association — pervasive in configuration, fee schedules, coverage tables |
| `sortedset of T` | **Reject** | No Precept construct observes iteration order; `.min`/`.max` safety is owned by `notempty` alone; `set notempty` is proof-identical |
| `priorityqueue of T priority P` | **Medium** | Real pattern; two-type-parameter grammar generalizes with `map`; proof surface manageable (emptiness + static direction) |
| `deque of T` | **Low** | Rare in business-rule domains — double-ended access is an infrastructure pattern, not a business-rule pattern |
| `list of T` | **Low** | Narrow incremental territory over `log`; mutable-ordered-list use cases rarer than bag/log/map; evaluate after `log` ships |
| `ringbuffer of T` | **Reject** | Silent eviction violates inspectability — implicit mutation the proof engine cannot track |
| `capacity` modifier | **Reject** | Synonym for `maxcount` — language surface cost with no capability gain |
| `multimap of K to V` | **Reject** | Nested collection semantics Precept explicitly excludes — use explicit set fields + constraints |

### Recommended Rollout

1. **First:** `bag` and `log` — highest business-rule unlock per complexity dollar; `bag` extends the existing set action surface minimally, `log` introduces one new keyword (`append`) and aligns with audit/compliance requirements
2. **Second:** `map` — highest structural value but largest surface expansion (new type connector `to`, new proof obligation category for key-presence)
3. **Evaluate:** `priorityqueue` — two-type-parameter design resolved; evaluate after `map` ships since `priority`/`to` share the role-connector pattern. `deque` deferred — insufficient business-rule pressure

---

## Comparison With Other Collection Systems

> A brief survey of collection types across languages and frameworks, mapped to Precept's current and proposed surface.

| Capability | .NET | Java | Python | Rust | SQL | CEL | F#/Haskell | Precept (shipped) | Precept (proposed) |
|---|---|---|---|---|---|---|---|---|---|
| **Unordered unique set** | `HashSet<T>` | `HashSet` | `set` | `HashSet` | — | — | `Set<'T>` | `set of T` ✓ | — |
| **Sorted unique set** | `SortedSet<T>` | `TreeSet` | — | `BTreeSet` | — | — | `Set<'T>` (sorted) | — | Rejected¶ |
| **Insertion-order set** | — | `LinkedHashSet` | — | `IndexSet`* | — | — | — | — | Not proposed†† |
| **Multiset / bag** | — | — | `Counter` | — | `MULTISET` | — | — | — | `bag of T` |
| **FIFO queue** | `Queue<T>` | `ArrayDeque` | `deque` | `VecDeque` | — | — | — | `queue of T` ✓ | — |
| **LIFO stack** | `Stack<T>` | `ArrayDeque` | `list` | `Vec` | — | — | — | `stack of T` ✓ | — |
| **Double-ended queue** | — | `ArrayDeque` | `deque` | `VecDeque` | — | — | — | — | `deque of T` (low pri) |
| **Priority queue** | `PriorityQueue<T,P>` | `PriorityQueue` | `heapq` | `BinaryHeap` | — | — | — | — | `priorityqueue of T priority P` (med pri) |
| **Append-only log** | `ImmutableList<T>` | — | — | — | — | `list` (immutable) | `list` (cons) | — | `log of T` |
| **Ordered sequence with random access** | `List<T>` | `ArrayList` | `list` | `Vec<T>` | — | — | — | — | `list of T` |
| **Key-value map** | `Dictionary<K,V>` | `HashMap` | `dict` | `HashMap` | — | `map` | `Map<'K,'V>` | — | `map of K to V` |
| **Ring buffer** | — | `CircularFifoQueue`* | `deque(maxlen=N)` | — | — | — | — | — | Rejected† |
| **Multimap** | `ILookup<K,V>` | `Multimap` (Guava) | — | — | — | — | `Map[K, List[V]]`‡ | — | Rejected§ |
| **Non-empty guarantee** | — | — | — | — | `NOT NULL` | — | `NonEmpty` | `notempty`/`mincount 1` ✓ | — |
| **Element uniqueness** | inherent in sets | inherent in sets | inherent in sets | inherent in sets | `UNIQUE` | — | inherent in sets | inherent in `set` ✓ | `unique` on queue/stack |
| **Cardinality constraints** | — | — | — | — | `CHECK` | `.size()` | — | `mincount`/`maxcount` ✓ | — |
| **Element predicates** | LINQ `.All`/`.Any` | Streams | comprehensions | `.iter().all()`/`.any()` | `ALL`/`ANY`/`EXISTS` | `.all()`/`.exists()` | `forall`/`exists` | — | `all`/`any`/`none` |
| **Subset/disjoint** | `.IsSubsetOf` | `.containsAll` | `<=`/`.isdisjoint` | `.is_subset` | — | — | `Set.isSubset` | — | `subset`/`disjoint` |

\* `IndexSet` is from the `indexmap` crate, not Rust std. `CircularFifoQueue` is from Apache Commons Collections.
† Ring buffer rejected: silent eviction on full `enqueue` violates inspectability — use `queue` + `maxcount` + explicit `dequeue`.
‡ Scala `Map[K, List[V]]` is the idiomatic multimap encoding, not a dedicated type.
§ Multimap rejected: nested collection semantics Precept explicitly excludes — use explicit set fields + `subset`/`disjoint` constraints.
¶ Sorted unique set rejected: no Precept construct observes sorted iteration order; `.min`/`.max` safety is owned by `notempty` alone; `set of T notempty` is proof-identical.
†† Insertion-order sets conflate two concerns (uniqueness + temporal ordering) in a way that makes proof-engine reasoning ambiguous — the "order" has no semantic meaning the engine can verify. Precept's `queue` (explicit FIFO) and `set` (explicit uniqueness) are the correct decomposition.

---

## Cross-References

This document is the canonical source for collection type rules. Other documents reference — not restate — these rules:

| Document | What it references | Link pattern |
|---|---|---|
| [Language Spec](precept-language-spec.md) | §2.3 type grammar, §3.6 `contains` typing, §3.8 action validation, accessor tables | Brief summary + "See [Collection Types](collection-types.md)" |
| [Primitive Types](primitive-types.md) | `~string` inner type, `.count` → `integer` production | Cross-reference to §Inner Type System |
| [Type Checker](../compiler/type-checker.md) | Collection action validation, emptiness proof obligations | Brief summary + link |
