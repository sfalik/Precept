# Decision: Priorityqueue Design — Five Open Questions Resolved

**Date:** 2026-04-29
**Author:** Frank (Lead Language Designer)
**Scope:** `docs/language/collection-types.md` — `priorityqueue` subsection in § Proposed Additional Types
**Trigger:** Shane's answers to five open questions from frank-14

---

## Resolved Questions

### 1. Priority Direction

**Decision:** Direction is specifiable at both declaration and operation sites. Declaration sets the default; dequeue can override.

- Declaration: `field F as priorityqueue of T priority P descending`
- Operation override: `-> dequeue F ascending into Target priority PTarget`
- Default is `ascending` (lowest value dequeued first) when no modifier is specified.
- Modifier follows its target in both positions, consistent with Precept's modifier-follows-target grammar.

### 2. Peek Behavior

**Decision:** `.peek` returns the element value (type `T`); `.peekpriority` returns the priority value (type `P`).

Two separate scalar accessors rather than a compound return. This is consistent with Precept's flat accessor model — every existing accessor returns a single scalar. Both require `.count > 0` emptiness guard.

### 3. Priority-Type Constraints (Quantifier Predicates)

**Decision:** Quantifier predicates (`each`/`any`/`no`) work on the priority axis via a two-field binding.

When iterating a `priorityqueue`, the binding variable exposes `.value` (element, type `T`) and `.priority` (priority, type `P`). Example: `no claim in ClaimQueue (claim.priority < 2)`.

This is a meaningful design precedent: single-type collections produce bare scalar bindings; two-type-parameter collections produce two-field projection bindings. The pattern generalizes to `map` (`.key`/`.value`).

### 4. Dequeue Priority Capture

**Decision:** Approved syntax: `-> dequeue F into Target priority PriorityTarget`.

`PriorityTarget` receives the dequeued item's priority value, typed as the declared priority type. The `priority` capture clause is optional — omitting it discards the priority value.

### 5. Grammar Alignment with `map`

**Decision:** Generalized two-type-parameter grammar using role-connector keywords.

```
TwoParamCollectionType :=
    priorityqueue of ScalarType priority ScalarType TypeQualifier?
  | map of ScalarType to ScalarType TypeQualifier?
```

`priority` and `to` are role-connector keywords introducing the secondary type parameter. `of` introduces the primary type. This is the canonical pattern for all two-type-parameter collection types in Precept.

---

## New Open Questions Surfaced

1. **Quantifier binding shape inference.** The type checker must distinguish bare-scalar bindings (single-type collections) from two-field projection bindings (two-type-parameter collections). Should this be implicit (inferred from collection type) or explicit?

2. **`ascending`/`descending` keyword scope.** These may also apply to `sortedset` iteration or future ordering features. Reserve broadly or scope to `priorityqueue`?

---

## Artifacts Updated

- `docs/language/collection-types.md` — full priorityqueue subsection rewritten with all five decisions integrated
- Priority Summary table and Recommended Rollout updated
- Comparison table updated to `priorityqueue of T priority P`
