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

"Orderable" means the inner type supports `<`/`>` comparison: all numeric types (`integer`, `decimal`, `number`) and `string` (including `~string`, which uses `OrdinalIgnoreCase` ordering — deterministic). `boolean` and `choice` (unordered) are not orderable; `.min`/`.max` on `set of boolean` or `set of choice(...)` is a type error.

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
CollectionType  :=  (set | queue | stack) of ScalarType
ScalarType      :=  string | ~string | integer | decimal | number | boolean | choice(...)
```

Collections of collections (`set of set of string`) are not supported. Collections of temporal or business-domain types are not currently supported — the inner type must be a primitive.

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

**Scalar constraints do not apply to collections.** `notempty`, `min`, `max`, `minlength`, `maxlength`, `maxplaces`, `nonnegative`, `positive`, `nonzero`, and `ordered` are all type errors on collection fields. Collections have their own constraint vocabulary: `mincount` and `maxcount`.

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

**Recommendation:** Bounded quantifier predicates only — `all`, `any`, `none`. No general loops, no `map`/`filter`/`reduce`.

**Rationale:** Quantifiers are logical predicates over finite collections, not iteration constructs. They are provably terminating — the collection is finite and no mutation occurs during evaluation. This distinguishes them from general iteration, which the language philosophy explicitly excludes (§0.4.1 "No iteration constructs").

**Proposed syntax:**

```precept
# In rule expressions — global truth
rule Items.all(item, item > 0)

# In guard expressions — conditional matching
from Submitted on Approve when Reviewers.any(r, r == Approve.ReviewerName)

# Negated quantifier — no matching element
rule Amounts.none(a, a < 0)
```

**Syntax form:** `Collection.quantifier(binding, predicate)` — the binding variable is named by the author and scoped to the predicate expression. The predicate is a boolean expression.

**Lexer note:** `All` and `Any` are already reserved keywords in the Precept lexer. Disambiguation is positional — `All`/`Any` after a `.` in member-access position is a quantifier call; elsewhere it retains its existing meaning (if any).

**What is deliberately held back:** `map`, `filter`, `reduce`, `sum`, and any transformation that produces a new collection or aggregate value. These require structured collection element types (a larger feature surface) and would cross the line from predicate into computation.

**Philosophy compatibility:** Quantifiers are bounded predicates — they assert a property of every/some/no element in a finite collection. They do not mutate state, do not introduce loop variables, and terminate in bounded time. They are consistent with §0.4.1 IF the spec is amended to explicitly distinguish bounded predicates from general iteration. This is an open question (see below).

### Additional Field Constraints

**Research basis:** Survey of FluentAssertions, Zod, Valibot, FluentValidation, OPA/Rego, Bean Validation (JSR-380), and SQL constraints. A 6-category taxonomy was developed:

1. **Cardinality** — `mincount`/`maxcount` (already shipped)
2. **Membership/value** — `contains` (already shipped)
3. **Element-shape (quantified predicates)** — requires quantifiers (proposed above)
4. **Ordering** — relative order of elements (no clear path yet)
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

**Layer B (second priority): Quantifier predicates** — `all`/`any`/`none` as described above.

**Layer C (deferred): Dedicated `check` blocks** — readability-only sugar, no new capability beyond what rules + quantifiers provide. Deferred until the need becomes clear from real-world usage.

### Open Questions

1. **Philosophy compatibility of quantifiers.** Does §0.4.1 "No iteration constructs" philosophically allow bounded quantifiers? Should the spec be amended to explicitly distinguish bounded predicates from general iteration? The distinction is meaningful — quantifiers assert truth over a finite domain, they don't iterate — but the current spec wording doesn't draw this line.

2. **Three quantifiers or two + negation?** Should the language ship `all`/`any`/`none` as three distinct keywords, or `all`/`any` with `not ... any` serving as `none`? Three keywords are more discoverable and read more naturally in business rules. Two + negation is more minimal and avoids adding a keyword that's syntactically equivalent to `not any`.

3. **Named binding variable or fixed `it`?** Should the quantifier syntax use a named binding — `Items.all(item, item > 0)` — or a fixed pronoun — `Items.all(it > 0)`? Named bindings are more explicit and avoid ambiguity in nested expressions. A fixed `it` is terser but creates scoping problems if quantifiers are ever nested.

4. **Quantifier priority.** Should quantifiers ship before or after proof engine / graph analyzer work that's already in flight? Quantifiers introduce proof obligations — the compiler must verify that the predicate holds for all elements, or emit a diagnostic. This work depends on the proof engine's ability to reason about collection elements.

5. **Collection `notempty` keyword.** Should `notempty` on a collection reuse the same keyword as string `notempty`, or get a distinct keyword? Reusing the keyword creates a natural parallel ("`notempty` means non-empty, regardless of what it's applied to"). A distinct keyword avoids potential confusion if the semantics diverge in the future.

6. **`unique` on `set` type.** Should `unique` on a `set` produce a warning (redundant — sets can't have duplicates) or silently accept? A warning is parallel to `RedundantModifier` for numeric constraints. Silent acceptance is more forgiving but allows conceptual mistakes to pass unnoticed.

7. **Cross-collection constraint form.** Should `subset`/`disjoint` be field modifiers (in the declaration) or rule expressions (in rule/ensure/when bodies)? Field modifiers are concise and declarative. Rule expressions are more flexible — they can be conditional, they can reference event args, and they can compose with boolean logic.

8. **Proposal granularity.** One combined proposal for all collection extensions, or two separate increments — quantifiers as one, field-level constraints as another? The 3-layer recommendation suggests separate increments (Layer A ships first, Layer B second), but the formal proposal structure is TBD.

---

## Cross-References

This document is the canonical source for collection type rules. Other documents reference — not restate — these rules:

| Document | What it references | Link pattern |
|---|---|---|
| [Language Spec](precept-language-spec.md) | §2.3 type grammar, §3.6 `contains` typing, §3.8 action validation, accessor tables | Brief summary + "See [Collection Types](collection-types.md)" |
| [Primitive Types](primitive-types.md) | `~string` inner type, `.count` → `integer` production | Cross-reference to §Inner Type System |
| [Type Checker](../compiler/type-checker.md) | Collection action validation, emptiness proof obligations | Brief summary + link |
