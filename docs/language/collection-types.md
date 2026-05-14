# Collection Types

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Canonical Design |
| Implementation state | Partially implemented — `set`/`queue`/`stack` surface shipped; `log of T`, `log of T by P`, `bag of T`, `list of T`, `queue of T by P`, and `lookup of K to V` approved and locked on `spike/Precept-V2`; `quantifier predicates` (`each`/`any`/`no`) and `notempty` on collections approved and locked on `spike/Precept-V2`, not yet implemented |
| Scope | Nine collection kinds: `set of T`, `queue of T`, `stack of T`, `log of T`, `log of T by P`, `bag of T`, `list of T`, `queue of T by P`, `lookup of K to V`; actions, accessors, constraints, emptiness safety, membership, inner type system; quantifier predicates (`each`/`any`/`no`) |
| Related | [Primitive Types](primitive-types.md) · [Language Spec](precept-language-spec.md) §§2.3, 3.6, 3.8 · [Type Checker](../compiler/type-checker.md) |

---

## Contents

- [Overview](#overview)
- [`set`](#set)
- [`queue`](#queue)
- [`stack`](#stack)
- [`log of T`](#log-of-t)
- [`log of T by P`](#log-of-t-by-p)
- [Inner Type System](#inner-type-system)
  - [`~string` — case-insensitive inner type](#string--case-insensitive-inner-type)
  - [Temporal and Business-Domain Inner Types](#temporal-and-business-domain-inner-types)
  - [Token vocabulary](#token-vocabulary)
- [Membership Operator](#membership-operator)
- [Emptiness Safety](#emptiness-safety)
  - [Access proof obligations](#access-proof-obligations)
  - [Mutation proof obligations](#mutation-proof-obligations)
  - [Safe operations (no proof required)](#safe-operations-no-proof-required)
  - [Guard pattern](#guard-pattern)
- [Constraint Catalog](#constraint-catalog)
- [Quantifier Predicates](#quantifier-predicates)
- [List Literal Defaults](#list-literal-defaults)
- [String Interpolation](#string-interpolation)
- [Action Summary](#action-summary)
- [Accessor Summary](#accessor-summary)
- [`bag of T`](#bag-of-t)
- [`list of T`](#list-of-t)
- [`queue of T by P`](#queue-of-t-by-p)
- [`lookup of K to V`](#lookup-of-k-to-v)
- [Deferred and Rejected Types](#deferred-and-rejected-types)
  - [Evaluation Criteria](#evaluation-criteria)
  - [Candidate 3: `deque of T`](#candidate-3-deque-of-t)
  - [Rejected: Ring Buffer / Circular Buffer](#rejected-ring-buffer--circular-buffer)
  - [Rejected: Bounded Collection (`capacity` modifier)](#rejected-bounded-collection-capacity-modifier)
  - [Rejected: Multimap](#rejected-multimap)
  - [Rejected: `sortedset of T`](#rejected-sortedset-of-t)
  - [Priority Summary](#priority-summary)
- [Comparison With Other Collection Systems](#comparison-with-other-collection-systems)
- [Cross-References](#cross-references)

## Overview

Precept has nine collection types. Each wraps a scalar inner type (or two scalar types for `queue of T by P` and `lookup of K to V`), provides a fixed set of mutation actions, and enforces structural safety through the proof engine. There are no user-defined collection types, no nested collections, and no collection-of-collection types.

| Kind | Ordering | Duplicates | Use case | Example |
|------|----------|------------|----------|---------|
| `set of T` | Unordered | No | Tags, categories, membership sets | `field Tags as set of string` |
| `queue of T` | FIFO | Yes | Work queues, waiting lists, processing pipelines | `field AgentQueue as queue of string` |
| `stack of T` | LIFO | Yes | Undo logs, step histories, nested contexts | `field RepairSteps as stack of string` |
| `log of T` | Insertion order (append-only) | Yes | Audit trails, event histories, compliance records | `field AuditTrail as log of string` |
| `log of T by P` | P order (append-only, P unique) | No (P unique) | Compliance records with external ordering keys | `field AuditLog as log of string by instant` |
| `bag of T` | Unordered | Yes (per-element count) | Inventory, shopping carts, frequency counting | `field CartItems as bag of string` |
| `list of T` | Insertion order (mutable positions) | Yes | Approval chains, ordered steps | `field ApprovalChain as list of string` |
| `queue of T by P` | P order (priority/ordered) | Yes (stable FIFO tiebreak) | Priority work queues, triage systems | `field ClaimQueue as queue of string by integer` |
| `lookup of K to V` | By key | No (keys unique, values may repeat) | Config tables, fee schedules, per-category settings | `field CoverageLimits as lookup of string to decimal` |

**Grammar:**

```
CollectionType  :=  (set | queue | stack) of ScalarType
                |   bag of ScalarType
                |   list of ScalarType
                |   log of ScalarType
                |   log of ScalarType by ScalarType
                |   queue of ScalarType by ScalarType DirectionModifier?
                |   lookup of ScalarType to ScalarType
DirectionModifier := ascending | descending
```

The inner type `T` must be a scalar type — any primitive type (`string`, `integer`, `decimal`, `number`, `boolean`, `choice`) or the special `~string` variant. Collections of collections are not supported.

**Common surface:** All nine kinds share `.count`. `set`, `queue`, `stack`, `bag`, `log`, and `list` share `contains` for value membership. `lookup` uses `contains` for key membership; `queue of T by P` uses `contains` for value membership. `set`, `queue`, `stack`, `bag`, `log`, and `list` support `mincount`/`maxcount` constraints and list literal defaults (except `queue of T by P` and `lookup of K to V` — see per-type constraint notes). `clear` applies to `set`, `queue`, `stack`, `bag`, and `list` only — not log types (append-only) and not `lookup` (has per-key `remove`). Kind-specific operations are documented per section below.

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

## `log of T`

**Declaration:**

```precept
field AuditTrail     as log of string
field AssessmentNotes as log of string
field StatusHistory  as log of string
field EventLog       as log of instant
field AuditTrail     as log of string notempty
```

**Behavior:** Append-only ordered sequence. Elements can be added but never removed. The append-only invariant guarantees `.count` is monotonically non-decreasing — the proof engine uses this to discharge non-emptiness proofs after any `append`. Models audit trails, event histories, and compliance records where immutability is a structural requirement, not a convention.

**Actions:**

| Action | Syntax | Behavior |
|--------|--------|----------|
| `append` | `append F Expr` | Add element to the end of the log. Always safe — no precondition. |

No removal operations exist by design. `clear`, `remove`, and all other mutation actions are type errors on a log field.

```precept
from any on any
    -> append AuditTrail "{CurrentState} -> {Event.Name}"
    -> no transition

from InReview on AddNote
    -> append AssessmentNotes AddNote.Text
    -> no transition
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Always safe — returns 0 for empty log. |
| `.first` | `T` | `.count > 0` guard required | First (oldest) element. Obligation: `UnguardedCollectionAccess`. Discharged statically by `notempty`. |
| `.last` | `T` | `.count > 0` guard required | Last (most recent) element. Obligation: `UnguardedCollectionAccess`. Discharged statically by `notempty`. |
| `.at(N)` | `T` | `N >= 0 and N < F.count` | Element at zero-based position `N`. Obligation: `UnguardedCollectionAccess`. |

```precept
from Review on Examine when AuditTrail.count > 0
    -> set FirstEntry = AuditTrail.first
    -> set LastEntry  = AuditTrail.last
    -> no transition
from Review on Examine
    -> reject "Audit trail is empty"
```

```precept
from Audit on GetEntry when AuditTrail.count > GetEntry.Index and GetEntry.Index >= 0
    -> set Entry = AuditTrail.at(GetEntry.Index)
    -> no transition
```

**Constraints:** `notempty`, `mincount N`, `maxcount N`, `optional`, `default [...]`. `notempty` statically discharges `.first` and `.last` access safety — no per-access `.count > 0` guard needed when the field is declared `notempty`.

**Proof engine implications:** `append` is always safe. `.first`/`.last` require `.count > 0` (same obligation as `.peek` on queue/stack). `.at(N)` requires an index-bounds guard: `N >= 0 and N < F.count`. The append-only invariant lets the proof engine conclude `.count > 0` after any `append` — subsequent `.first`/`.last` accesses in the same action block are safe without a separate guard.

**Backing type:** Custom `ImmutableLog<T>` — pair-of-stacks (Okasaki functional queue) using two `ImmutableStack<T>` instances (front + back). O(1) `append`, O(1) `.last`, amortized O(1) `.first`, O(n) `.at(index)`. Structural sharing across snapshots.

**Type errors:**

| Expression | Diagnostic |
|---|---|
| `add LogField Expr` | `CollectionOperationOnScalar` — `add` is a set operation |
| `remove LogField Expr` | `CollectionOperationOnScalar` — `remove` is a set operation |
| `enqueue LogField Expr` | `CollectionOperationOnScalar` — `enqueue` is a queue operation |
| `push LogField Expr` | `CollectionOperationOnScalar` — `push` is a stack operation |
| `clear LogField` | `CollectionOperationOnScalar` — `clear` is not valid on an append-only log |
| `set LogField = Expr` | `ScalarOperationOnCollection` |
| `append LogField Expr` where `Expr` type ≠ `T` | `TypeMismatch` |

---

## `log of T by P`

**Declaration:**

```precept
field ComplianceLog as log of string  by instant
field AuditLog      as log of string  by integer
field EventHistory  as log of string  by integer
```

**Behavior:** Append-only ordered log where entries are ordered by an external ordering key `P`, not by insertion order. `P` must be **unique** across all entries. Uniqueness is a precondition on `append`: authors write a `when not (F contains P)` guard to discharge it. Without the guard the proof engine raises `UnguardedCollectionAccess`; if the guard is present and evaluates false at runtime the transition rejects. The compiler cannot prove uniqueness statically in general — the same pattern as emptiness-guarded accessors.

Models compliance records and audit logs where entries carry an external ordering key (timestamp from an external system, regulatory sequence number, business priority ID) and the log must be readable in key order. `P` values are expected to arrive mostly in order (new `P` > current max), with occasional out-of-order entries. `P` must be orderable — numeric types and `choice of T(...) ordered`.

**Actions:**

| Action | Syntax | Behavior |
|--------|--------|----------|
| `append F Expr by P` | `append ComplianceLog entry by RecordEvent.Timestamp` | Add entry at P-ordered position. `Expr` must be type `T`; `P` must match the declared ordering key type. Precondition: `P` not already in log. Write `when not (F contains P)` guard — proof engine raises `UnguardedCollectionAccess` if absent. |

```precept
from Active on RecordEvent
    when not (ComplianceLog contains RecordEvent.Timestamp)
    -> append ComplianceLog RecordEvent.Description by RecordEvent.Timestamp
    -> no transition
from Active on RecordEvent
    -> reject "Duplicate timestamp — entry already recorded"

from Filing on SubmitEntry
    when not (AuditLog contains SubmitEntry.SequenceNumber)
    -> append AuditLog SubmitEntry.Payload by SubmitEntry.SequenceNumber
    -> no transition
from Filing on SubmitEntry
    -> reject "Duplicate sequence number"
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Always safe. |
| `.first` | `T` | `.count > 0` guard required | Entry with minimum `P` value. Obligation: `UnguardedCollectionAccess`. |
| `.last` | `T` | `.count > 0` guard required | Entry with maximum `P` value. Obligation: `UnguardedCollectionAccess`. |
| `.at(N)` | `T` | `N >= 0 and N < F.count` | Nth entry in `P` order (zero-based). Obligation: `UnguardedCollectionAccess`. |

```precept
from Audit on GetOldest when ComplianceLog.count > 0
    -> set OldestEntry = ComplianceLog.first
    -> no transition
from Audit on GetOldest
    -> reject "Compliance log is empty"
```

**Constraints:** `notempty`, `mincount N`, `maxcount N`, `optional`. `notempty` statically discharges `.first`/`.last` access safety. No `default [...]` — list literals do not carry ordering keys.

**Backing type:** Custom immutable sorted linked list with P-order insertion and cached head + tail pointers. O(1) in-order append (new `P` > current max — the overwhelmingly common case for timestamps), O(k) near-tail out-of-order insertion, O(n) worst-case. O(1) `.first`/`.last`, O(n) `.at(index)`. Structural sharing: in-order append shares entire prefix.

**Uniqueness enforcement:** `P` uniqueness is a precondition on `append`, not a runtime fault. Authors write `when not (F contains P)` (P-type argument → key membership) to guard the transition; the proof engine raises `UnguardedCollectionAccess` if the guard is absent. If the guard evaluates false at runtime the transition rejects — consistent with all other preconditions in Precept.

The uniqueness check has **the same complexity as the append itself** — it piggybacks on the position-finding scan at no additional cost:

- **Common case (P > current max):** Compare P to the cached tail pointer. If `P > tail.P`, uniqueness is proven in O(1) — a duplicate cannot exist beyond the current max. The `contains` guard is O(1) and the append is O(1).
- **Out-of-order case:** The scan back from the tail to find the insertion position already traverses existing P values in sorted order. Uniqueness is verified during that same O(k) traversal — no second pass.

`ImmutableSortedDictionary<P, T>` would require O(log n) `ContainsKey` even for the common in-order case. The custom sorted linked list is therefore strictly better on the uniqueness check performance as well as on append.

**Distinction from `queue of T by P`:**

| Property | `log of T by P` | `queue of T by P` |
|---|---|---|
| Removal | None — append-only, entries are permanent | `dequeue` removes the front element |
| `P` uniqueness | Required — `when not (F contains P)` precondition; transition rejects if guard fails | Not required — stable FIFO tiebreak |
| `.at(N)` | Available | Not available |
| Purpose | Permanent ordered compliance record | Ordered work queue |

**Type errors:**

| Expression | Diagnostic |
|---|---|
| `add LogField Expr` | `CollectionOperationOnScalar` |
| `clear LogField` | `CollectionOperationOnScalar` — not valid on an append-only log |
| `set LogField = Expr` | `ScalarOperationOnCollection` |
| `append LogField Expr` (missing `by`) | `MissingOrderingKey` — `log of T by P` requires a `by P` clause on every `append` |
| `append LogField Expr by P` where `Expr` type ≠ `T` | `TypeMismatch` |
| `append LogField Expr by P` where `P` expression type ≠ declared ordering type | `TypeMismatch` |

---

## Inner Type System

The inner type `T` in any Precept collection must be a scalar type. For two-type-parameter collections (`queue of T by P`, `lookup of K to V`), both type parameters must be scalar types. The collection grammar is:

```
CollectionType    :=  (set | queue | stack) of ScalarType
                  |   bag of ScalarType
                  |   list of ScalarType
                  |   log of ScalarType
                  |   log of ScalarType by ScalarType
                  |   queue of ScalarType by ScalarType DirectionModifier?
                  |   lookup of ScalarType to ScalarType
DirectionModifier :=  ascending | descending
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

`~string` is valid as a collection inner type and as a scalar field type. As a collection inner type, it selects `StringComparer.OrdinalIgnoreCase` as the collection's comparer, governing membership testing, deduplication (for sets), and ordering (for `.min`/`.max`) consistently.

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

**`~string` as a scalar field type.** `field Email as ~string` is valid. The type checker enforces that equality comparisons (`==`/`!=`) are replaced with `~=`/`!~`, that `startsWith`/`endsWith` are replaced with `~startsWith`/`~endsWith`, and that a `~string` field is not tested against a case-sensitive collection with `contains`. When testing a `~string` value against a case-sensitive collection, use a quantifier: `any e in Roles (e ~= Email)`. See [Primitive Types](primitive-types.md) §`~string` for the full scalar enforcement rules, type unification, event arg declarations, and the `choice of ~string` exclusion.

**`lookup of ~string to V`.** When the key type is `~string`, the lookup must be constructed with `ImmutableDictionary.Create(StringComparer.OrdinalIgnoreCase)`. Behavioral consequence: `put "MEDICAL" = 100` followed by `put "medical" = 200` is an **overwrite** (not an error) — the keys compare equal under `OrdinalIgnoreCase`, so the second `put` replaces the first. This is consistent with `set of ~string` deduplication and with `put` being an explicit upsert by design. `contains` on `lookup of ~string to V` uses `OrdinalIgnoreCase` for key membership, consistent with all other CI collection kinds.

> **Diagnostic code 66 reassignment.** `CaseInsensitiveStringOnNonCollection` (code 66) existed to guard against scalar `~string` in a non-collection context. It was defined in `DiagnosticCode.cs` but was **never emitted** by the parser — the parser fell into `ExpectedToken` instead. When scalar `~string` ships, code 66 is **reassigned** to `CaseInsensitiveFieldRequiresTildeEquals`. Since it was never emitted, reassignment is safe.

**`~string` in queue, stack, and log.** While `~string` is most meaningful for sets (where deduplication and membership benefit from case-insensitive comparison), it is also valid as the inner type for `queue of ~string`, `stack of ~string`, and `log of ~string`. The `contains` operator on these collections uses `OrdinalIgnoreCase` matching.

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

The same binding rule applies to multi-qualifier inner types:

```precept
field FxRates as set of exchangerate in 'USD' to 'EUR'
```

This parses as `set of (exchangerate in 'USD' to 'EUR')`. Both qualifiers belong to the inner scalar type.

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
| `LogType` | `log` | Log collection type |
| `Of` | `of` | Collection inner type connector |
| `By` | `by` | Ordering key connector in `log of T by P` and `queue of T by P` (contextual keyword) |
| `Tilde` | `~` | Case-insensitive modifier — collection inner type (`set of ~string`) or scalar field type qualifier (`field Email as ~string`) |
| `Append` | `append` | Add an element to the end of a log or list field |
| `Into` | `into` | Dequeue/pop target keyword |
| `Contains` | `contains` | Membership operator |
| `Mincount` | `mincount` | Minimum count constraint |
| `Maxcount` | `maxcount` | Maximum count constraint |
| `BagType` | `bag` | Bag collection type |
| `ListType` | `list` | List collection type |
| `LookupType` | `lookup` | Lookup collection type |
| `To` | `to` | Key-to-value connector in `lookup of K to V` (contextual keyword) |
| `Put` | `put` | Put action keyword for lookup |
| `For` | `for` | Lookup key accessor (`F for K`) — requires a `when F contains K` guard. The compiler raises `KeyPresenceSafety` if the guard is absent. |
| `Insert` | `insert` | Insert action keyword for list |
| `At` | `at` | Position keyword in `insert F Expr at N` / `remove F at N` (contextual keyword) |
| `Ascending` | `ascending` | Sort direction modifier (contextual keyword in type position) |
| `Descending` | `descending` | Sort direction modifier (contextual keyword in type position) |
| `Countof` | `countof` | Per-element count accessor on bag |
| `Peekby` | `peekby` | Ordering-value peek accessor on `queue of T by P` |

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
| `log of T` | `T` | `boolean` |
| `log of T by P` | `T` | `boolean` — value membership |
| `log of T by P` | `P` | `boolean` — key (P) membership; use this to guard `append` |
| `bag of T` | `T` | `boolean` — value membership (count ≥ 1) |
| `list of T` | `T` | `boolean` |
| `queue of T by P` | `T` | `boolean` — value membership |
| `lookup of K to V` | `K` | `boolean` — key membership; when `K = ~string`, uses `OrdinalIgnoreCase` (consistent with all CI collection kinds) |
| non-collection | — | type error |

**Case sensitivity:** `contains` on `set of string` is case-sensitive (ordinal). `contains` on `set of ~string` is case-insensitive (`OrdinalIgnoreCase`). `contains` on `queue of ~string`, `stack of ~string`, `log of ~string`, `log of ~string by P`, `bag of ~string`, and `list of ~string` is also case-insensitive — CI membership applies to all collection kinds when the inner type is `~string`. The full CI enforcement rules (including diagnostic codes) are defined canonically in [precept-language-spec.md § 3.8 Semantic Checks](precept-language-spec.md#38-semantic-checks).

**No proof requirement.** `contains` on an empty collection returns `false` — it is always safe to call without a count guard.

---

## Emptiness Safety

**Canonical source for diagnostic codes:** [`src/Precept/Language/DiagnosticCode.cs`](../../src/Precept/Language/DiagnosticCode.cs) and [`src/Precept/Language/Diagnostics.cs`](../../src/Precept/Language/Diagnostics.cs). Collection safety codes are 63–65 and 99–106; see [spec §3.10](precept-language-spec.md#310-diagnostic-catalog) for the full group reference.

Precept enforces emptiness safety through proof obligations.Operations that would fault on an empty collection require the author to prove the collection is non-empty via a `.count > 0` guard in the enclosing `when` clause.

### Access proof obligations

| Expression | Proof required | Diagnostic if unguarded |
|---|---|---|
| `SetField.min` | `SetField.count > 0` | `UnguardedCollectionAccess` |
| `SetField.max` | `SetField.count > 0` | `UnguardedCollectionAccess` |
| `QueueField.peek` | `QueueField.count > 0` | `UnguardedCollectionAccess` |
| `StackField.peek` | `StackField.count > 0` | `UnguardedCollectionAccess` |
| `LogField.first` | `LogField.count > 0` | `UnguardedCollectionAccess` |
| `LogField.last` | `LogField.count > 0` | `UnguardedCollectionAccess` |
| `LogField.at(N)` | `N >= 0 and N < LogField.count` | `UnguardedCollectionAccess` |
| `ListField.first` | `ListField.count > 0` | `UnguardedCollectionAccess` |
| `ListField.last` | `ListField.count > 0` | `UnguardedCollectionAccess` |
| `ListField.at(N)` | `N >= 0 and N < ListField.count` | `UnguardedCollectionAccess` |
| `QueueByPField.peek` | `QueueByPField.count > 0` | `UnguardedCollectionAccess` |
| `QueueByPField.peekby` | `QueueByPField.count > 0` | `UnguardedCollectionAccess` |
| `LookupField for K` | `LookupField contains K` | `KeyPresenceSafety` |

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
| `append` on `log of T` | Always succeeds — append-only, no precondition. |
| `append` on `log of T by P` | Precondition: `P` not already in log. Write `when not (F contains P)` guard — see Emptiness Safety § Proof obligations. |
| `clear` | No-op on empty — no fault possible. |
| `add`, `remove` (bag) | `add` always safe. `remove` is no-op when element count is 0 — no fault possible. |
| `append`, `remove` (list) | `append` always safe. `remove` (first-occurrence) is no-op if absent — no fault possible. |
| `insert F Expr at N`, `remove F at N` (list) | Require index-bounds guard: `N >= 0 and N <= F.count` for `insert`, `N >= 0 and N < F.count` for `remove`. |
| `enqueue` (queue of T by P) | Always safe — no precondition. |
| `put`, `remove` (lookup) | Both always safe — `put` creates or overwrites; `remove` is no-op if absent. |
| `F for K` (lookup) | Requires `F contains K` guard. Obligation: `KeyPresenceSafety`. |

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

The proof engine recognizes `F.count > 0` in the `when` clause as sufficient proof for all `.peek`, `.min`, `.max`, `.first`, `.last`, `dequeue`, and `pop` operations on `F` within that transition row's action block. This is structurally guaranteed — if the guard is satisfied, the collection is non-empty, and the operation is safe.

For `lookup of K to V`, the analogous pattern is key-presence: `F contains K` in the `when` clause discharges `KeyPresenceSafety` for `F for K` in the same row's action block.

**Philosophy:** Emptiness safety connects to the language's totality guarantee (§0.11 of the spec): every expression evaluates to a result, never to an undefined or fault state. The compiler proves emptiness safety or emits a diagnostic. Runtime fault checks exist only as defensive redundancy for paths the compiler has already proven unreachable.

---

## Constraint Catalog

> The constraints below are defined canonically in [precept-language-spec.md § 3.3 Context-Sensitive Type Resolution](precept-language-spec.md#33-context-sensitive-type-resolution). This section shows per-type applicability and collection-specific validation rules. If this section and the spec disagree, the spec is authoritative.

| Constraint | Applicable to | Meaning |
|---|---|---|
| `notempty` | `set`, `queue`, `stack`, `log`, `bag`, `list`, `queue of T by P` | Collection must contain at least one element. Statically discharges `.min`/`.max`/`.peek`/`.peekby`/`.first`/`.last` access safety. Equivalent to `mincount 1`. |
| `mincount N` | `set`, `queue`, `stack`, `log`, `bag`, `list`, `queue of T by P`, `lookup` | Collection must contain at least N elements |
| `maxcount N` | `set`, `queue`, `stack`, `log`, `bag`, `list`, `queue of T by P`, `lookup` | Collection must contain at most N elements |
| `optional` | any field type (including collections) | Field may be unset; requires `is set` guard before use |
| `default [...]` | `set`, `queue`, `stack`, `log`, `bag`, `list` | Initial value — list literal of scalar elements. Not applicable to `queue of T by P` (two-axis elements) or `lookup of K to V` (key-value pairs require explicit key syntax). |

**Constraint validation rules:**

| Check | Diagnostic |
|---|---|
| `mincount` > `maxcount` on same field | `InvalidModifierBounds` |
| Negative `mincount` or `maxcount` | `InvalidModifierValue` |
| Duplicate modifier | `DuplicateModifier` |
| `mincount`/`maxcount` on scalar field | `InvalidModifierForType` |
| `min`/`max`/`nonnegative`/`minlength`/`maxlength`/`maxplaces` on collection field | `InvalidModifierForType` |

**Scalar constraints do not apply to collections.** `min`, `max`, `minlength`, `maxlength`, `maxplaces`, `nonnegative`, `positive`, `nonzero`, and `ordered` are all type errors when applied as field-level modifiers on collection fields (e.g., `field Tags as set of string ordered` is invalid). Collections have their own constraint vocabulary: `notempty`, `mincount`, and `maxcount`. Note that `ordered` on the *inner `choice of T(...)` type* is valid — `field Priorities as set of choice of string("low", "medium", "high") ordered` declares an ordered-choice inner type, not a collection-level modifier.

> **Element-level constraints:** `notempty`, `mincount`, and `maxcount` constrain the collection as a whole. To constrain individual elements (e.g., "all items must be positive" or "no items may be empty"), use a quantifier predicate *(see [§ Quantifier Predicates](#quantifier-predicates) for syntax)*.

---

## Quantifier Predicates

**Status:** Locked design — approved for `spike/Precept-V2`. Parser support ships first, independent of proof engine work. Not yet implemented.

**Philosophy compatibility:** §0.4.1 of the language spec explicitly carves out bounded predicates from the no-iteration rule.

**Motivation:** The collection surface provides membership testing (`contains`) and cardinality (`.count`, `mincount`, `maxcount`), but no way to express constraints over the *elements* of a collection. Business rules like "all items must be positive" or "at least one reviewer matches the submitter" require element-level predicates.

**Research basis:** CEL (Common Expression Language) provides 5 parse-time macros — `all`, `exists`, `exists_one`, `filter`, `map` — all non-Turing-complete, expanding at parse time over finite collections. OPA/Rego uses universal/existential quantification over sets. SQL uses `ALL`/`ANY`/`EXISTS` over subqueries.

**Design:** Bounded quantifier predicates only — `each`, `any`, `no`. No general loops, no `map`/`filter`/`reduce`.

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

**Binding variable type:** The binding variable has the collection's inner type. If the collection is `set of ~string`, the binding variable is `~string` — `~string` enforcement rules apply inside the predicate. For example, `each item in Tags (item == "admin")` where `Tags` is `set of ~string` triggers `CaseInsensitiveFieldRequiresTildeEquals` on `item == "admin"` — use `item ~= "admin"` instead.

**Grammar:**

```
QuantifierExpr  :=  QuantifierKind Identifier in CollectionField '(' BoolExpr ')'
QuantifierKind  :=  each | any | no
```

#### Binding variable rules

| Property | Rule |
|----------|------|
| **Type** | The collection's inner type. `set of ~string` → binding var is `~string`. `queue of T by P` → binding var is a two-field projection (`.value` → `T`, `.by` → `P`). |
| **Scope** | Strictly within the `(` … `)` predicate expression — not visible outside the quantifier. |
| **Shadowing** | If a field with the same name exists at global scope, the binding variable shadows it inside the predicate. Error: `BindingShadowsField` — rename the binding to avoid confusion. |
| **Keyword collision** | Reserved keyword as binding variable name is a parse error (`ExpectedIdentifier`). |

**Lexer note:** `any` is already a reserved keyword in the Precept lexer. `each` requires a new lexer entry. `no` is already reserved and requires disambiguation from `no transition` context; `any` is already reserved and requires disambiguation from its type modifier role. See §1.2 and §2.1 of the language spec for the disambiguation rules.

**`CollectionRef` restriction (v1):** `CollectionRef` is restricted to a bare field name (`Identifier`) in v1. `each item in Event.Tags (...)` is a parse error — use `set Field = Event.Tags` in an action before the guard, or restructure. The parser emits `ExpectedFieldName` at the `in` position if a non-identifier follows.

**Keyword decisions (locked):** The language ships `each`, `any`, `no` as three distinct keywords. Three keywords are more discoverable and read more naturally in business rules. `no` reads better than `not any` in business rule context.

**Named binding (locked):** The quantifier syntax uses author-named binding variables (`item`, `r`, `a`). Named bindings are explicit and avoid nesting ambiguity. A fixed `it` pronoun creates scoping problems if quantifiers are ever nested.

**What is deliberately held back:** `map`, `filter`, `reduce`, `sum`, and any transformation that produces a new collection or aggregate value. These require structured collection element types (a larger feature surface) and would cross the line from predicate into computation.

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

**Design:** List literals are deliberately restricted to `default` clauses. They are not general-purpose expressions — you cannot assign a list literal via `set` or use one in a `when` guard. This keeps the action surface focused on element-level operations (`add`, `remove`, `enqueue`, `push`, `append`) rather than bulk replacement.

---

## String Interpolation

Collections are a type error inside string interpolation. Each `{expr}` inside `"..."` is type-checked independently, and collection-typed expressions emit `InvalidInterpolationCoercion`.

```precept
# Type error: collection in interpolation
set Message = "Tags: {Tags}"        # InvalidInterpolationCoercion

# Correct: use scalar accessors
set Message = "Tag count: {Tags.count}"
```

This restriction exists because collections have no canonical string representation — they are unordered (set) or ordered-by-insertion or append-only (queue, stack, log), and the language does not define a serialization format for them.

---

## Action Summary

| Action | Set | Queue | Stack | Log | Bag | List | Queue/P | Lookup | Value | Proof |
|--------|-----|-------|-------|-----|-----|------|---------|--------|-------|-------|
| `add F Expr` | ✓ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | `T` | — |
| `remove F Expr` | ✓ | ✗ | ✗ | ✗ | ✓ (decrement) | ✓ (first-occurrence) | ✗ | ✗ | `T` | — |
| `enqueue F Expr` | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | `T` | — |
| `enqueue F Expr by P` | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | `T`, `P` | — |
| `dequeue F` | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | — | `.count > 0` |
| `dequeue F into G` | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | — | `.count > 0` |
| `dequeue F into G by H` | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | — | `.count > 0` |
| `push F Expr` | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | `T` | — |
| `pop F` | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | — | `.count > 0` |
| `pop F into G` | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | — | `.count > 0` |
| `append F Expr` | ✗ | ✗ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ | `T` | — |
| `insert F Expr at N` | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | `T` | index-bounds |
| `remove F at N` | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | — | index-bounds |
| `put F K = V` | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | `K`, `V` | — |
| `remove F K` (lookup) | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | `K` | — |
| `clear F` | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ | ✓ | ✗ | — | — |

**Cross-kind errors:** Applying an action to the wrong collection kind emits `CollectionOperationOnScalar`. Applying a collection action to a scalar field emits `CollectionOperationOnScalar`. Applying a scalar action (`set =`) to a collection field emits `ScalarOperationOnCollection`.

---

## Accessor Summary

| Member | Set | Queue | Stack | Log | Bag | List | Queue/P | Lookup | Returns | Proof |
|--------|-----|-------|-------|-----|-----|------|---------|--------|---------|-------|
| `.count` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | `integer` | — |
| `.min` | ✓ (T orderable) | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | `T` | `.count > 0` |
| `.max` | ✓ (T orderable) | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | `T` | `.count > 0` |
| `.peek` | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✓ | ✗ | `T` | `.count > 0` |
| `.peekby` | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | `P` | `.count > 0` |
| `.first` | ✗ | ✗ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ | `T` | `.count > 0` |
| `.last` | ✗ | ✗ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ | `T` | `.count > 0` |
| `.at(N)` | ✗ | ✗ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ | `T` | `N >= 0 and N < F.count` |
| `.countof(E)` | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | `integer` | — |
| `F for K` | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | `V` | `F contains K` |

---

## `bag of T`

**Declaration:**

```precept
field CartItems      as bag of string
field Ingredients    as bag of string
field DiagnosisCodes as bag of string
field ItemQuantities as bag of integer
```

**Behavior:** Multiset (unordered collection with duplicates). Each element has an associated count. Adding an element increments its count; removing decrements (removing when count is 1 deletes the element). An element with count 0 is absent.

**Actions:**

| Action | Syntax | Behavior |
|--------|--------|----------|
| `add` | `add F Expr` | Increment count of `Expr` in `F`. Always safe — no precondition. |
| `remove` | `remove F Expr` | Decrement count of `Expr`. If count reaches 0, element is removed. No-op if element absent. |
| `clear` | `clear F` | Remove all elements. |

```precept
from Shopping on AddItem
    -> add CartItems AddItem.SKU
    -> no transition

from Shopping on RemoveItem when CartItems contains RemoveItem.SKU
    -> remove CartItems RemoveItem.SKU
    -> no transition

rule CartItems.countof("hazmat-item") <= 3
    because "No more than 3 hazardous items per order"
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Total element count including all duplicates (sum of all per-element counts). Always safe. |
| `.countof(Expr)` | `integer` | None | Count of a specific element. Returns 0 if absent. Always safe. |
| `contains` | `boolean` | None | Value membership (returns `true` if count ≥ 1). Always safe. |

```precept
from Reviewing on CheckHazmat when CartItems.countof("hazmat-item") > 0
    -> set HazmatCount = CartItems.countof("hazmat-item")
    -> no transition
```

**Constraints:** `notempty`, `mincount N`, `maxcount N`, `optional`, `default [T, T, ...]`. `notempty` enforces `.count >= 1` (state-entry constraint). No `.first`/`.last`/`.peek` — bag is unordered, no positional accessors.

**Proof engine implications:** No new proof obligations beyond count-based. `.countof(Expr)` is always safe (returns 0 if absent). The proof engine must understand `remove` decrements rather than unconditionally deletes — element persists until count reaches 0. Sequential action tracking: proof engine tracks how `add`/`remove` changes `.count` within a transition row.

**Backing type:** `ImmutableDictionary<T, int>` mapping each element to its count. `add` = `SetItem(k, count+1)`, `remove` = count > 1 ? `SetItem(k, count-1)` : `Remove(k)`. O(log n) all ops.

**Type errors:**

| Scenario | Error |
|---|---|
| `add F Expr` where `Expr` type ≠ `T` | `TypeMismatch` |
| `.countof(Expr)` where `Expr` type ≠ `T` | `TypeMismatch` |
| `enqueue BagField Expr` | `CollectionOperationOnScalar` — `enqueue` is a queue operation |
| `set BagField = Expr` | `ScalarOperationOnCollection` |

---

## `list of T`

**Declaration:**

```precept
field ApprovalChain   as list of string
field ProcessingSteps as list of string
field Waypoints       as list of string
field PriorityItems   as list of integer
```

**Behavior:** Ordered sequence with stable positions. Duplicates allowed. Supports arbitrary insertion and removal by position or value. Differs from `log` (append-only) by supporting arbitrary removal; differs from `queue`/`stack` by supporting positional access without consuming elements.

**Actions:**

| Action | Syntax | Behavior |
|--------|--------|----------|
| `append` | `append F Expr` | Add `Expr` to the end of `F`. Always safe. |
| `insert at N` | `insert F Expr at N` | Insert `Expr` at zero-based position `N`, shifting subsequent elements. Precondition: `N >= 0 and N <= F.count`. Author writes guard; proof engine raises `UnguardedCollectionAccess` if absent. |
| `remove` | `remove F Expr` | Remove first occurrence of `Expr`. No-op if absent. |
| `remove at N` | `remove F at N` | Remove element at zero-based position `N`, shifting subsequent elements. Precondition: `N >= 0 and N < F.count`. Author writes guard; proof engine raises `UnguardedCollectionAccess` if absent. Parser distinguishes `remove F at N` from `remove F Expr` by the trailing `at` keyword immediately after the field token. |
| `clear` | `clear F` | Remove all elements. |

```precept
from Draft on AddReviewer
    -> append ApprovalChain AddReviewer.ReviewerId
    -> no transition

from Draft on RemoveReviewer when ApprovalChain contains RemoveReviewer.ReviewerId
    -> remove ApprovalChain RemoveReviewer.ReviewerId
    -> no transition

from Active on GetNextReviewer when ApprovalChain.count > 0
    -> set CurrentReviewer = ApprovalChain.first
    -> no transition
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Always safe. |
| `.first` | `T` | `.count > 0` guard required | First element. Obligation: `UnguardedCollectionAccess`. Discharged statically by `notempty`. |
| `.last` | `T` | `.count > 0` guard required | Last element. Obligation: `UnguardedCollectionAccess`. Discharged statically by `notempty`. |
| `.at(N)` | `T` | `N >= 0 and N < F.count` | Element at zero-based position `N`. Obligation: `UnguardedCollectionAccess`. |

```precept
from Active on InspectChain when ApprovalChain.count > 0
    -> set FirstReviewer = ApprovalChain.first
    -> set LastReviewer  = ApprovalChain.last
    -> no transition

from Active on GetReviewer when ApprovalChain.count > GetReviewer.Index and GetReviewer.Index >= 0
    -> set Reviewer = ApprovalChain.at(GetReviewer.Index)
    -> no transition
```

**Constraints:** `notempty`, `mincount N`, `maxcount N`, `optional`, `default [T, T, ...]`. `notempty` statically discharges `.first`/`.last` access obligations.

**Proof engine implications:** Index-bounds obligations (`UnguardedCollectionAccess`) for `.at(N)`, `insert F Expr at N`, and `remove F at N`. Author writes `when N >= 0 and N < F.count` (or `N <= F.count` for `insert`) guard; proof engine raises `UnguardedCollectionAccess` if absent. Sequential action tracking: proof engine tracks count changes from `insert`, `remove`, `remove at N`, and `clear` within a transition row and re-verifies subsequent access guards against updated count. Positional stability across mutations (e.g., after `remove Items at 0`, positions shift) is the author's responsibility — the proof engine proves access safety, not value-level positional invariants.

**Backing type:** `ImmutableList<T>` (.NET) — AVL tree with structural sharing. O(log n) insert, remove, index access. O(1) `.count`.

**Type errors:**

| Scenario | Error |
|---|---|
| `insert F Expr at N` where `N` type is not `integer` | `TypeMismatch` |
| `.at(N)` where `N` type is not `integer` | `TypeMismatch` |
| `append F Expr` where `Expr` type ≠ `T` | `TypeMismatch` |
| `insert F Expr at N` where `Expr` type ≠ `T` | `TypeMismatch` |
| `set ListField = Expr` | `ScalarOperationOnCollection` |

---

## `queue of T by P`

**Declaration:**

```precept
field ClaimQueue  as queue of string by choice of string("critical", "high", "normal", "low") ordered
field WorkItems   as queue of string by integer ascending
field ScoredQueue as queue of string by integer descending
field TriageQueue as queue of string by integer
```

**Behavior:** A queue where elements are dequeued by ordering value rather than insertion order. Each element has two axes: a value (type `T`) and an ordering key (type `P`). The ordering type `P` must be orderable (numeric or `choice of T(...) ordered`). Dequeue always removes the element with the best ordering value according to the declared sort direction. When multiple elements share the same ordering value, they are dequeued in the order they were enqueued — **insertion-order (stable) tiebreaking**. This is a language guarantee, not an implementation detail.

**Actions:**

| Action | Syntax | Behavior |
|--------|--------|----------|
| `enqueue` | `enqueue F Expr by P` | Add element `Expr` with ordering value `P`. Always safe — no precondition. |
| `dequeue` | `dequeue F` | Remove and discard the front (best-ordered) element. Precondition: `F.count > 0`. Obligation: `UnguardedCollectionMutation`. |
| `dequeue into` | `dequeue F into G` | Remove front element; store value in field `G` (type `T`). Precondition: `F.count > 0`. Obligation: `UnguardedCollectionMutation`. |
| `dequeue into by` | `dequeue F into G by H` | Remove front element; store value in `G` (type `T`) and ordering value in `H` (type `P`). Precondition: `F.count > 0`. Obligation: `UnguardedCollectionMutation`. |
| `clear` | `clear F` | Remove all elements. |

```precept
field ClaimQueue as queue of string
    by choice of string("critical", "high", "normal", "low") ordered

from Receiving on FileClaim
    -> enqueue ClaimQueue FileClaim.ClaimId by FileClaim.Severity
    -> no transition

from Processing on ProcessNext when ClaimQueue.count > 0
    -> dequeue ClaimQueue into CurrentClaim by CurrentSeverity
    -> transition Reviewing
```

```precept
from Processing on CheckNext when ClaimQueue.count > 0
    -> set NextClaimId  = ClaimQueue.peek
    -> set NextSeverity = ClaimQueue.peekby
    -> no transition
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Total number of elements across all ordering groups. Always safe. |
| `.peek` | `T` | `.count > 0` guard required | Element value of the front (best-ordered) item. Obligation: `UnguardedCollectionAccess`. |
| `.peekby` | `P` | `.count > 0` guard required | Ordering value of the front (best-ordered) item. Obligation: `UnguardedCollectionAccess`. |

```precept
from Triage on Inspect when ClaimQueue.count > 0
    -> set TopClaim    = ClaimQueue.peek
    -> set TopSeverity = ClaimQueue.peekby
    -> no transition
from Triage on Inspect
    -> reject "No claims in queue"
```

**Constraints:** `notempty`, `mincount N`, `maxcount N`, `optional`. No `default [...]` — elements have two axes (T and P); list literal syntax would be ambiguous. `notempty` statically discharges `.peek`/`.peekby` access obligations.

**Proof engine implications:** Emptiness obligations identical to `queue`. The ordering value introduces a secondary type requirement — the `by` argument on `enqueue` must match the declared ordering type `P`, and `P` must satisfy `TypeTrait.Orderable`. The proof engine must understand that dequeue order is by ordering value (respecting direction), not insertion, which affects reasoning about which element `.peek` and `.peekby` return. The direction modifier is a static property of the field declaration.

**Backing type:** `SortedDictionary<TPriority, Queue<TElement>>` (each ordering bucket is a FIFO queue) with a separately maintained element counter. For `ordered choice of T` ordering types, the comparer is built from declaration-position rank at build time. .NET's `PriorityQueue<T,P>` cannot be used directly — it explicitly does not guarantee tiebreak order.

**Type errors:**

| Scenario | Error |
|---|---|
| `enqueue F Expr by P` where `Expr` type ≠ `T` | `TypeMismatch` |
| `enqueue F Expr by P` where `P` expression type ≠ declared ordering type | `TypeMismatch` |
| `dequeue F into G` where `G` type ≠ `T` | `TypeMismatch` |
| `.peek` or `.peekby` without `count > 0` guard | `UnguardedCollectionAccess` |
| `P` type is not orderable (does not satisfy `TypeTrait.Orderable`) | `TypeTrait.Orderable` violation |

#### Sort Direction

The sort direction determines which ordering value is dequeued first. Direction is declared at the field level only — it is a static property of the field declaration and cannot be overridden per-operation.

```precept
# ordered choice: declaration-position rank 0 ("critical") dequeued first (ascending default)
field ClaimQueue  as queue of string
    by choice of string("critical", "high", "normal", "low") ordered

# numeric ordering: lowest integer dequeued first
field WorkItems   as queue of string by integer ascending

# numeric ordering, highest first
field ScoredQueue as queue of string by integer descending

# default: ascending
field TriageQueue as queue of string by integer
```

- `ascending` (default) — lowest rank dequeued first. For ordered choice, the first listed value (declaration-position rank 0) dequeues first. For integers, the lowest number dequeues first.
- `descending` — highest rank dequeued first. For ordered choice, the last listed value dequeues first. For integers, the highest number dequeues first.

If no direction modifier is specified, the default is `ascending`.

**Design rationale:** Direction is fixed at the field level because it is a semantic property of what the queue represents. A field that needs to dequeue from both ends is two fields, not one field with ambiguous processing logic. Per-operation overrides were considered and rejected: `.peek` cannot reflect an override that hasn't happened yet, creating a footgun where you peek the highest but dequeue the lowest; the proof engine complexity for conditional field behavior is unjustified for an edge case; and the "without redeclaring the field" justification is circular — if your logic genuinely needs both directions, the model is wrong.

#### Peek with Ordering Value

`.peek` returns the element value (type `T`) of the front-of-queue item — consistent with `.peek` on `queue`. A separate `.peekby` accessor returns the ordering value (type `P`) of the same front-of-queue item. The name reflects its meaning directly: "peek at the `by`-axis value of the front element."

| Accessor | Returns | Guard required | Description |
|---|---|---|---|
| `.peek` | `T` | `.count > 0` | Element value of the front (best-ordered) item |
| `.peekby` | `P` | `.count > 0` | Ordering value of the front (best-ordered) item |
| `.count` | `integer` | — | Number of elements in the queue |

**Design rationale:** Two separate accessors (`.peek` and `.peekby`) rather than a tuple or compound return. This is consistent with Precept's flat accessor model — every accessor returns a single scalar value. The declaration maps directly onto the two accessors: `queue of T by P` → `.peek` returns `T`, `.peekby` returns `P`.

#### Dequeue with Ordering Value Capture

The `dequeue ... into` form is extended with an optional `by` capture clause:

```precept
-> dequeue ClaimQueue into CurrentClaim by CurrentSeverity
```

- `CurrentClaim` receives the dequeued element value (type `T` — here `string`).
- `CurrentSeverity` receives the dequeued element's ordering value (type `P` — here the declared `choice of string(...) ordered` type).
- Both target fields must be declared and type-compatible.
- The `by` capture is optional — `dequeue ClaimQueue into CurrentClaim` remains valid and discards the ordering value.

#### Quantifier Predicates on Ordering Axis

When a quantifier (`each`/`any`/`no`) iterates over a `queue of T by P`, the binding variable exposes two fields:

- `.value` — the element (type `T`)
- `.by` — the ordering value (type `P`)

This is a meaningful design distinction: for single-type collections (`set`, `queue`, `stack`), the binding variable IS the element (a bare scalar). For `queue of T by P`, the binding variable is a **two-field projection** exposing both axes.

```precept
# Assert no low-severity claims are waiting in the queue
rule no claim in ClaimQueue (claim.by == "low")
    because "Low-severity claims must not enter the processing queue"

# Existential check on element value
when any claim in ClaimQueue (claim.value == TargetClaimId)

# Compound predicate across both axes
rule no claim in ClaimQueue (claim.by == "critical" and claim.value == "")
    because "Critical claims must have a claim ID"
```

When the ordering axis is choice-typed, the declaration is the constraint — no rule needed. An element with ordering value outside the declared choices is a type error at the `enqueue` site:

```precept
field TriageQueue as queue of ClaimId
    by choice of string("normal", "high") ordered
```

An ordering value of `"critical"` cannot enter `TriageQueue` — the type prevents it. With ordered choice, the valid set is fixed at declaration; no rule is needed to enforce it.

**Grammar for `queue of T by P` quantifier binding:**

```
# For queue of T by P: binding exposes .value and .by
QuantifierExpr  :=  QuantifierKind Identifier in OrderedQueueField '(' BoolExpr ')'
# Inside BoolExpr, Identifier.value : T and Identifier.by : P
```

**Note:** For `lookup of K to V`, the analogous binding shape would be `.key` and `.value`. The principle generalizes: two-type-parameter collections expose a two-field binding in quantifier scope.

#### Grammar Fit — Generalized Two-Type-Parameter Pattern

The `queue of T by P` and `lookup of K to V` types both take two type parameters connected by a role keyword. The generalized grammar:

```
TwoParamCollectionType :=
    queue  of ScalarType by ScalarType DirectionModifier?
  | lookup of ScalarType to ScalarType

DirectionModifier := ascending | descending
```

Both `by` and `to` are **role-connector keywords** (prepositions) that introduce the secondary type parameter. The `of` keyword introduces the primary type. This is the generalized pattern for all two-type-parameter collection types.

For `queue of T by P`, the full field declaration grammar is:

```
OrderedQueueDecl :=
    field Identifier as queue of ScalarType by ScalarType DirectionModifier?
```

Where `DirectionModifier` defaults to `ascending` if omitted.

#### Resolved Design Questions

The following questions from frank-14 are now resolved:

1. **~~Priority direction~~** — RESOLVED. Direction is declared at the field level only (`ascending`/`descending` modifier, default `ascending`). Per-operation override rejected: `.peek` cannot reflect a not-yet-executed override; proof engine complexity is unjustified; a field needing both directions is a modelling error — use two fields.

2. **~~Peek behavior~~** — RESOLVED. `.peek` returns the element value (type `T`); `.peekby` returns the ordering value (type `P`). Two separate scalar accessors, consistent with the flat accessor model. The declaration maps directly: `queue of T by P` → `.peek` returns `T`, `.peekby` returns `P`.

3. **~~Priority-type constraints~~** — RESOLVED. Quantifier predicates (`each`/`any`/`no`) apply to the ordering axis via a two-field binding: `binding.value` (element) and `binding.by` (ordering value). This generalizes to all two-type-parameter collections.

4. **~~Dequeue capture~~** — RESOLVED. Approved syntax: `dequeue F into Target by OrderTarget`. The `by` capture is optional — omitting it discards the ordering value.

5. **~~Grammar alignment with `lookup`~~** — RESOLVED. Both `queue of T by P` and `lookup of K to V` use the generalized two-type-parameter pattern: `of PrimaryType role-connector SecondaryType`. Role connectors are `by` and `to` respectively — both prepositions naming the semantic relationship.

6. **~~Equal-priority tiebreaking~~** — RESOLVED. Insertion-order (stable) tiebreaking is the language guarantee. .NET's `PriorityQueue<T,P>` cannot be the direct backing type (unspecified tiebreak). Approved backing: `SortedDictionary<TPriority, Queue<TElement>>` with a separately maintained element counter and declaration-derived comparers for ordered-choice `P` types. See inbox record `frank-george-priorityqueue-backing-structure.md`.

#### Open Questions (New)

1. **Quantifier binding shape for single-type vs. two-type collections.** When a quantifier iterates a `set of string`, the binding IS the string. When it iterates a `queue of string by integer`, the binding exposes `.value` and `.by`. This means the type checker must distinguish binding shapes by collection kind. Is this implicit (the type checker infers the shape from the collection type) or explicit (the author declares the binding shape)? Implicit is simpler and consistent with how the binding already adapts to the collection's inner type.

2. **`ascending`/`descending` keyword reuse.** These keywords may also be relevant for future ordering features beyond `queue of T by P`. Should they be reserved broadly as ordering modifiers, or scoped specifically to priority queue declarations?

---

## `lookup of K to V`

**Declaration:**

```precept
field CoverageLimits  as lookup of string to decimal
field FeeSchedule     as lookup of string to decimal
field ConfigSettings  as lookup of string to string
field CategoryCounts  as lookup of string to integer
```

**Behavior:** Key-value association. Each key maps to exactly one value. Keys are unique — `put` with an existing key overwrites. `remove` on an absent key is a no-op. Value access via `for` keyword requires a key-presence guard.

**Actions:**

| Action | Syntax | Behavior |
|--------|--------|----------|
| `put` | `put F K = V` | Set key `K` to value `V`. Always safe — creates or overwrites. |
| `remove` | `remove F K` | Remove entry with key `K`. No-op if absent. Always safe. |

Value access (`F for K`) is an expression (not an action) — used in `set` and `when` clauses. Precondition: `F contains K`. Author writes `when F contains K` guard; proof engine raises `KeyPresenceSafety` if absent.

```precept
from Draft on SetCoverage
    -> put CoverageLimits SetCoverage.CoverageType = SetCoverage.Limit
    -> no transition

from Active on CheckCoverage when CoverageLimits contains CheckCoverage.CoverageType
    -> set CurrentLimit = CoverageLimits for CheckCoverage.CoverageType
    -> no transition

from Active on RemoveCoverage
    -> remove CoverageLimits RemoveCoverage.CoverageType
    -> no transition

rule CoverageLimits.count <= 10
    because "No more than 10 coverage types per policy"
```

**Accessors:**

| Member | Returns | Proof requirement | Notes |
|--------|---------|-------------------|-------|
| `.count` | `integer` | None | Number of key-value pairs. Always safe. |
| `F for K` | `V` | `F contains K` guard required | Value at key `K`. Obligation: `KeyPresenceSafety`. |
| `contains K` | `boolean` | None | Key membership (type `K` argument). Always safe — returns `false` if lookup is empty. |

```precept
from Active on ApplyFee when FeeSchedule contains ApplyFee.TransactionType
    -> set ApplicableFee = FeeSchedule for ApplyFee.TransactionType
    -> no transition
from Active on ApplyFee
    -> reject "No fee defined for this transaction type"
```

**Constraints:** `notempty`, `mincount N`, `maxcount N`, `optional`. No `default [...]` — key-value pairs require explicit key syntax not supported in list-literal form.

**Proof engine implications:** New obligation category: **key-presence safety** (`KeyPresenceSafety`). `F for K` is guarded by `F contains K` in a `when` clause — same structural pattern as emptiness-guarded accessors but keyed rather than count-based. The proof engine must track key-presence from `contains` guards in `when` clauses and propagate that information to `for` access in the same transition row's actions. `put` and `remove` are always safe.

**Backing type:** `ImmutableDictionary<K, V>`. O(log n) all ops. `put` = `SetItem`, `remove` = `Remove` (no-op if absent per .NET contract).

**Type errors:**

| Scenario | Error |
|---|---|
| `put F K = V` where `K` type does not match declared `K` | `TypeMismatch` |
| `put F K = V` where `V` type does not match declared `V` | `TypeMismatch` |
| `F for K` where `K` type does not match declared `K` | `TypeMismatch` |
| `contains K` where argument type does not match declared `K` | `TypeMismatch` |
| `F for K` without `F contains K` guard | `KeyPresenceSafety` |
| `K` or `V` is not a scalar type | `CollectionInnerTypeError` |

---

## Deferred and Rejected Types

> The following collection types were evaluated and either deferred or rejected. They are preserved as a design record.

### Evaluation Criteria

Every candidate must pass all six filters before advancing:

1. **Unlocks a business rule** that `set`/`queue`/`stack` cannot express today
2. **Deterministic and inspectable** — no hidden ordering surprises, no ambient state
3. **Proof engine can reason about safety** — access patterns are statically verifiable
4. **Fits Precept's keyword-anchored flat-statement style** — no nested expressions or builder chains
5. **Business analyst readable** — the keyword name communicates behavior without documentation
6. **Non-Turing-complete** — finite, bounded operations; no unbounded recursion or general iteration

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

**Priority:** **Deferred.** The double-ended access pattern is rare in business-rule domains. Most real scenarios are either FIFO (queue) or LIFO (stack). The escalation pattern can be modeled with two separate queues (priority + normal) or a `queue of T by P`. The keyword surface cost is high relative to the business-rule unlock (`push-front`, `push-back`, `pop-front`, `pop-back` = 4 new action keywords). Re-evaluate after `queue of T by P` ships and real usage confirms a gap that priority queuing cannot fill.

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

**Proof engine implications:** Strictly harder than `lookup`. Each key maps to a collection, so the proof engine must reason about nested collection emptiness, per-key cardinality, and cross-key relationships. This compounds the already-complex `lookup` proof surface.

**Grammar fit:** `field F as multimap of K to V`. Reuses the `of ... to` connector from `lookup`.

**Action surface:** Same as `lookup` (`put`, `remove`, `contains`, `for`) but `F for key` returns a collection, not a scalar — introducing nested collection semantics that Precept explicitly excludes.

**Priority/Recommendation:** **Reject.** A multimap is a collection of collections behind a key lookup — two levels of indirection that enter general-purpose data structure territory. Even if `lookup` ships, `multimap` adds nested collection semantics that Precept's flat-statement philosophy prohibits. The one-to-many pattern is better served by explicit set fields (`field EngineeringTeam as set of string`, `field SalesTeam as set of string`) with cross-collection constraints (`subset`, `disjoint`) relating them.

### Rejected: `sortedset of T`

**Motivation:** Like `set` but maintains elements in sorted order by the inner type's natural ordering. `.min` and `.max` were claimed to be "always-safe" when combined with `notempty`, and iteration order would be deterministic.

**Why rejected:** No Precept construct can observe sorted iteration order. Quantifiers (`each`, `any`, `no`) are boolean predicates — order-independent by definition. `.min`/`.max` return the minimum and maximum value regardless of storage order; the proof obligation for safe access is discharged by `notempty` alone. `set of T notempty` is proof-identical to `sortedset of T notempty` — the sorted storage contributes nothing to the safety guarantee. The action surface (`add`, `remove`, `clear`) is identical to `set`.

The sole difference between `sortedset` and `set` that is visible at the language surface is the type name. A type whose behavior is indistinguishable from another type by any language construct is not a type — it is an implementation detail wearing a type costume. Tree-backed storage with O(log n) inserts buys no benefit in a DSL governing business contracts where collections are small.

If Precept ever adds ordered-iteration constructs that make sorted order observable, `sortedset` can be re-evaluated at that time with an actual consuming construct to justify it.

### Priority Summary

| Candidate | Status | Rationale |
|---|---|---|
| `deque of T` | **Deferred** | Rare in business-rule domains; escalation covered by `queue of T by P`; re-evaluate after priority queuing ships |
| `sortedset of T` | **Rejected** | No Precept construct observes iteration order; `set of T notempty` is proof-identical |
| `ringbuffer of T` | **Rejected** | Silent eviction violates inspectability — implicit mutation the proof engine cannot track |
| `capacity` modifier | **Rejected** | Synonym for `maxcount` — language surface cost with no capability gain |
| `multimap of K to V` | **Rejected** | Nested collection semantics Precept explicitly excludes |

---

## Comparison With Other Collection Systems

> A brief survey of collection types across languages and frameworks, mapped to Precept's current and proposed surface.

| Capability | .NET | Java | Python | Rust | SQL | CEL | F#/Haskell | Precept (shipped) | Precept (proposed) |
|---|---|---|---|---|---|---|---|---|---|
| **Unordered unique set** | `HashSet<T>` | `HashSet` | `set` | `HashSet` | — | — | `Set<'T>` | `set of T` ✓ | — |
| **Sorted unique set** | `SortedSet<T>` | `TreeSet` | — | `BTreeSet` | — | — | `Set<'T>` (sorted) | — | Rejected¶ |
| **Insertion-order set** | — | `LinkedHashSet` | — | `IndexSet`* | — | — | — | — | Not proposed†† |
| **Multiset / bag** | — | — | `Counter` | — | `MULTISET` | — | — | `bag of T` ✓ | — |
| **FIFO queue** | `Queue<T>` | `ArrayDeque` | `deque` | `VecDeque` | — | — | — | `queue of T` ✓ | — |
| **LIFO stack** | `Stack<T>` | `ArrayDeque` | `list` | `Vec` | — | — | — | `stack of T` ✓ | — |
| **Double-ended queue** | — | `ArrayDeque` | `deque` | `VecDeque` | — | — | — | — | `deque of T` (**Deferred**) |
| **Priority queue** | `PriorityQueue<T,P>` | `PriorityQueue` | `heapq` | `BinaryHeap` | — | — | — | `queue of T by P` ✓ | — |
| **Append-only log** | `ImmutableList<T>` | — | — | — | — | `list` (immutable) | `list` (cons) | `log of T` ✓, `log of T by P` ✓ | — |
| **Ordered sequence with random access** | `List<T>` | `ArrayList` | `list` | `Vec<T>` | — | — | — | `list of T` ✓ | — |
| **Key-value map** | `Dictionary<K,V>` | `HashMap` | `dict` | `HashMap` | — | `map` | `Map<'K,'V>` | `lookup of K to V` ✓ | — |
| **Ring buffer** | — | `CircularFifoQueue`* | `deque(maxlen=N)` | — | — | — | — | — | Rejected† |
| **Multimap** | `ILookup<K,V>` | `Multimap` (Guava) | — | — | — | — | `Map[K, List[V]]`‡ | — | Rejected§ |
| **Non-empty guarantee** | — | — | — | — | `NOT NULL` | — | `NonEmpty` | `notempty`/`mincount 1` ✓ | — |
| **Element uniqueness** | inherent in sets | inherent in sets | inherent in sets | inherent in sets | `UNIQUE` | — | inherent in sets | inherent in `set` ✓ | — |
| **Cardinality constraints** | — | — | — | — | `CHECK` | `.size()` | — | `mincount`/`maxcount` ✓ | — |
| **Element predicates** | LINQ `.All`/`.Any` | Streams | comprehensions | `.iter().all()`/`.any()` | `ALL`/`ANY`/`EXISTS` | `.all()`/`.exists()` | `forall`/`exists` | `each`/`any`/`no` (approved, spike/Precept-V2) | — |
| **Subset/disjoint** | `.IsSubsetOf` | `.containsAll` | `<=`/`.isdisjoint` | `.is_subset` | — | — | `Set.isSubset` | — | — |

\* `IndexSet` is from the `indexmap` crate, not Rust std. `CircularFifoQueue` is from Apache Commons Collections.
† Ring buffer rejected: silent eviction on full `enqueue` violates inspectability — use `queue` + `maxcount` + explicit `dequeue`.
‡ Scala `Map[K, List[V]]` is the idiomatic multimap encoding, not a dedicated type.
§ Multimap rejected: nested collection semantics Precept explicitly excludes. `subset`/`disjoint` keywords rejected: quantifier predicates (`each`/`any`/`no`) already express subset and disjoint constraints as rules.
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
