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

"Orderable" means the inner type supports `<`/`>` comparison: all numeric types (`integer`, `decimal`, `number`), `string` (including `~string`, which uses `OrdinalIgnoreCase` ordering — deterministic), and `choice(...) ordered` (which defines rank by declaration position). `boolean` and unordered `choice(...)` are not orderable; `.min`/`.max` on `set of boolean` or `set of choice(...)` (without `ordered`) is a type error. On `set of choice(...) ordered`, `.min` returns the element with the lowest declaration position and `.max` returns the highest — these are safe when the `.count > 0` guard is satisfied.

```precept
field RiskLevels as set of choice("low", "medium", "high") ordered
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
CollectionType  :=  (set | queue | stack) of ScalarType
ScalarType      :=  string | ~string | integer | decimal | number | boolean | choice(...) ordered?
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

**Scalar constraints do not apply to collections.** `notempty`, `min`, `max`, `minlength`, `maxlength`, `maxplaces`, `nonnegative`, `positive`, `nonzero`, and `ordered` are all type errors when applied as field-level modifiers on collection fields (e.g., `field Tags as set of string ordered` is invalid). Collections have their own constraint vocabulary: `mincount` and `maxcount`. Note that `ordered` on the *inner `choice(...)` type* is valid — `field Priorities as set of choice("low", "medium", "high") ordered` declares an ordered-choice inner type, not a collection-level modifier.

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
4. **Ordering** — relative order of elements (partial path: `choice(...) ordered` as an inner type already enables element-level comparison via declaration-position rank, making `.min`/`.max` valid and enabling quantifier predicates like `Items.all(x, x >= "medium")` over ordered-choice sets; the remaining gap is ordering *constraints* on element sequences — e.g., "elements must be monotonically increasing" — which has no path yet)
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

### Candidate 1: `sortedset of T`

**What it is:** Like `set` but maintains elements in sorted order by the inner type's natural ordering. `.min` and `.max` are always-safe accessors (no emptiness guard needed when combined with `notempty`), and iteration order is deterministic.

**Business scenario:** A loan application tracks required approval levels. The business rule is "the highest approval level determines the signing authority." With a plain `set`, `.min`/`.max` require emptiness guards every time. A `sortedset` with a `notempty` constraint makes these always-safe.

```precept
field ApprovalLevels as sortedset of choice("team-lead", "director", "vp", "cfo") ordered notempty

# .min → always-safe, returns lowest declared rank
# .max → always-safe, returns highest declared rank
rule SigningAuthority == ApprovalLevels.max
    reason "Signing authority must match the highest required approval"
```

**Proof engine implications:** If the field carries `notempty` (or `mincount 1`), `.min`/`.max` are statically safe — no `UnguardedCollectionAccess` needed. The proof engine must track the `notempty` constraint as a proof discharge for access obligations. The inner type `T` must be orderable.

**Grammar fit:**

```
field F as sortedset of T
```

Reuses the `of` connector and all existing constraint keywords (`mincount`, `maxcount`, `notempty`).

**Action surface:** Identical to `set` — `add`, `remove`, `clear`. No new keywords. The sorted invariant is maintained internally.

**Priority:** **Medium.** The business value is real (eliminating emptiness guards on ordered collections), but `set` + explicit guards covers the same ground with more ceremony. This is a convenience and safety improvement, not a capability gap.

### Candidate 2: `bag of T` (multiset)

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

### Candidate 4: `priorityqueue of T`

**What it is:** A queue where elements are dequeued by priority rather than insertion order. Each enqueue includes a priority value; dequeue always removes the highest-priority element.

**Business scenario:** A claims triage system where claims are processed by severity. A work-item queue where urgent items bypass the normal order.

```precept
field ClaimQueue as priorityqueue of string

from Receiving on FileClaim
    -> enqueue ClaimQueue FileClaim.ClaimId priority FileClaim.Severity
    -> no transition

from Processing on ProcessNext when ClaimQueue.count > 0
    -> dequeue ClaimQueue into CurrentClaim
    -> transition Reviewing
```

**Proof engine implications:** Emptiness obligations identical to `queue`. The priority value introduces a secondary type requirement — the `priority` argument must be orderable. The proof engine must understand that dequeue order is by priority, not insertion, which affects reasoning about which element `.peek` returns.

**Grammar fit:**

```
field F as priorityqueue of T
```

New syntax required for `enqueue ... priority Expr`. The `priority` keyword is new.

**Action surface:** Extends `enqueue` with optional `priority` clause. Reuses `dequeue`, `dequeue into`, `clear`, `.peek`, `.count`. Adds `priority` as a contextual keyword.

**Priority:** **Medium.** Priority-based processing is a real business pattern, but the interaction between the priority type and the inner type creates complexity. The proof engine must reason about priority ordering, not just element types. Consider whether this is better served by a `sortedset` with a composite key or by application-layer logic outside the precept.

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

### Candidate 6: `map of K to V`

**What it is:** A key-value association. Each key maps to exactly one value. Supports set-by-key, get-by-key, contains-key, and remove-by-key.

**Business scenario:** A policy record maps coverage types to their limits. A fee schedule maps transaction types to fee amounts. A configuration entity maps setting names to values.

```precept
field CoverageLimits as map of string to decimal

from Draft on SetCoverage
    -> put CoverageLimits SetCoverage.CoverageType = SetCoverage.Limit
    -> no transition

from Active on CheckCoverage when CoverageLimits containskey CheckCoverage.CoverageType
    -> set CurrentLimit = CoverageLimits.get(CheckCoverage.CoverageType)
    -> no transition

rule CoverageLimits.count <= 10
    reason "No more than 10 coverage types per policy"
```

**Proof engine implications:** `.get(key)` requires a `containskey` guard — same pattern as emptiness proofs but keyed. This is a new proof obligation category: key-presence safety. `put` is always safe (creates or overwrites). `removekey` requires no guard (no-op if absent, like `remove` on `set`). The proof engine must track key-presence from `containskey` guards in `when` clauses.

**Grammar fit:**

```
field F as map of K to V
```

Introduces `to` as a type-position keyword connecting key and value types. Both `K` and `V` must be scalar types. The `to` keyword is new in this context but reads naturally.

**Action surface:** New keywords: `put`, `removekey`, `containskey`, `.get(key)`. `containskey` parallels `contains` on sets. `.count` reuses existing accessor. `.keys` and `.values` could return sets for use in `contains` and quantifier expressions.

**Priority:** **High.** Key-value association is fundamental to business modeling. Configuration tables, fee schedules, lookup mappings, and per-category settings are everywhere. Today, modeling these in Precept requires either multiple parallel fields or external application logic — both of which break the one-file-complete-rules guarantee. A `map` type keeps the association inside the contract.

### Priority Summary

| Candidate | Priority | Rationale |
|---|---|---|
| `bag of T` | **High** | Unlocks quantity tracking — pervasive in commerce, inventory, counting rules |
| `log of T` | **High** | Unlocks append-only audit trails — pervasive in regulated industries, aligns perfectly with prevention philosophy |
| `map of K to V` | **High** | Unlocks key-value association — pervasive in configuration, fee schedules, coverage tables |
| `sortedset of T` | **Medium** | Convenience improvement — eliminates emptiness guards on ordered collections, but `set` + guards covers same ground |
| `priorityqueue of T` | **Medium** | Real pattern but complex proof surface — priority ordering adds a secondary type axis |
| `deque of T` | **Low** | Rare in business-rule domains — double-ended access is an infrastructure pattern, not a business-rule pattern |

### Recommended Rollout

1. **First:** `bag` and `log` — highest business-rule unlock per complexity dollar; `bag` extends the existing set action surface minimally, `log` introduces one new keyword (`append`) and aligns with audit/compliance requirements
2. **Second:** `map` — highest structural value but largest surface expansion (new type connector `to`, new proof obligation category for key-presence)
3. **Evaluate:** `sortedset` — only after quantifier predicates ship, because sorted-set value compounds with element-level predicates
4. **Defer:** `priorityqueue` and `deque` — insufficient business-rule pressure to justify the surface cost

---

## Comparison With Other Collection Systems

> A brief survey of collection types across languages and frameworks, mapped to Precept's current and proposed surface.

| Capability | .NET | Java | Python | Rust | SQL | CEL | F#/Haskell | Precept (shipped) | Precept (proposed) |
|---|---|---|---|---|---|---|---|---|---|
| **Unordered unique set** | `HashSet<T>` | `HashSet` | `set` | `HashSet` | — | — | `Set<'T>` | `set of T` ✓ | — |
| **Sorted unique set** | `SortedSet<T>` | `TreeSet` | — | `BTreeSet` | — | — | `Set<'T>` (sorted) | — | `sortedset of T` |
| **Insertion-order set** | — | `LinkedHashSet` | — | `IndexSet`* | — | — | — | — | Not proposed† |
| **Multiset / bag** | — | — | `Counter` | — | `MULTISET` | — | — | — | `bag of T` |
| **FIFO queue** | `Queue<T>` | `ArrayDeque` | `deque` | `VecDeque` | — | — | — | `queue of T` ✓ | — |
| **LIFO stack** | `Stack<T>` | `ArrayDeque` | `list` | `Vec` | — | — | — | `stack of T` ✓ | — |
| **Double-ended queue** | — | `ArrayDeque` | `deque` | `VecDeque` | — | — | — | — | `deque of T` (low pri) |
| **Priority queue** | `PriorityQueue<T,P>` | `PriorityQueue` | `heapq` | `BinaryHeap` | — | — | — | — | `priorityqueue of T` (med pri) |
| **Append-only log** | `ImmutableList<T>` | — | — | — | — | `list` (immutable) | `list` (cons) | — | `log of T` |
| **Key-value map** | `Dictionary<K,V>` | `HashMap` | `dict` | `HashMap` | — | `map` | `Map<'K,'V>` | — | `map of K to V` |
| **Non-empty guarantee** | — | — | — | — | `NOT NULL` | — | `NonEmpty` | `notempty`/`mincount 1` ✓ | — |
| **Element uniqueness** | inherent in sets | inherent in sets | inherent in sets | inherent in sets | `UNIQUE` | — | inherent in sets | inherent in `set` ✓ | `unique` on queue/stack |
| **Cardinality constraints** | — | — | — | — | `CHECK` | `.size()` | — | `mincount`/`maxcount` ✓ | — |
| **Element predicates** | LINQ `.All`/`.Any` | Streams | comprehensions | `.iter().all()`/`.any()` | `ALL`/`ANY`/`EXISTS` | `.all()`/`.exists()` | `forall`/`exists` | — | `all`/`any`/`none` |
| **Subset/disjoint** | `.IsSubsetOf` | `.containsAll` | `<=`/`.isdisjoint` | `.is_subset` | — | — | `Set.isSubset` | — | `subset`/`disjoint` |

\* `IndexSet` is from the `indexmap` crate, not Rust std.
† Insertion-order sets conflate two concerns (uniqueness + temporal ordering) in a way that makes proof-engine reasoning ambiguous — the "order" has no semantic meaning the engine can verify. Precept's `queue` (explicit FIFO) and `set` (explicit uniqueness) are the correct decomposition.
