# Precept Collection Types — Design Investigation

**By:** Frank  
**Date:** 2026-05-04  
**Status:** Investigation complete — all architectural decisions locked  
**Depends on:** `runtime-api-public-surface-spec.md` (§13, OQ-2, OQ-5), `precept-value-types-investigation.md`  
**Decision records:** frank-105 (CLR API), frank-106 (internal representation), frank-107 (scalability), frank-108 (pair layout), frank-109 (action architecture), frank-110 (CoW protocol)

---

## §1 — Scope and Purpose

This document is the **canonical working reference** for the complete collection types design. It covers:

- The full collection type vocabulary of the Precept DSL (9 distinct collection kinds)
- CLR type mapping for each collection kind at the `Get<T>()` call site (§4–§8)
- Element type projection rules (§6)
- Null, empty, and missing semantics (§7)
- The key-value map surface (`lookup`) (§8)
- **Internal representation** — `PreceptValue[]` backing, stride conventions, pair layout (§11)
- **Scalability thresholds and lazy-load extensibility seam** (§12)
- **Collection action architecture** — `CollectionActions` static helpers (§13)
- **Copy-on-write protocol** — multi-mutation handling within events (§14)

**Out of scope:** Scalar type mapping (covered in `precept-value-types-investigation.md`).

**Prior decisions respected:**
- OQ-2 (resolved): `TypeMeta.ClrType` is scalar only; `IReadOnlyList<T>` wrapping applied at descriptor-build time
- OQ-5 / §13.4 (Frank's recommendation): Option A — lazy adapter `PreceptList<T> : IReadOnlyList<T>` wrapping internal `PreceptValue[]`
- The public API exposes `IReadOnlyList<T>` as the CLR type — callers never see `PreceptList<T>` by name

---

## §2 — Precept Collection Types Catalog

The Precept DSL defines **9 collection type kinds** across two parametric shapes:

### Single-type collections (parameterized by element type `T`)

| DSL Type | TypeKind | Syntax | Semantics | Ordering | Uniqueness | Mutability Model |
|----------|----------|--------|-----------|----------|------------|-----------------|
| `set` | `Set` | `set of T` | Unordered unique elements | None | Unique | add, remove, clear |
| `queue` | `Queue` | `queue of T` | FIFO | Insertion order | Duplicates allowed | enqueue, dequeue, clear |
| `stack` | `Stack` | `stack of T` | LIFO | Insertion order (reversed) | Duplicates allowed | push, pop, clear |
| `bag` | `Bag` | `bag of T` | Unordered multiset | None | Duplicates tracked by count | add, remove, clear |
| `list` | `List` | `list of T` | Ordered, positional access | Index-based | Duplicates allowed | append, insert, remove at, clear |
| `log` | `Log` | `log of T` | Append-only ordered | Insertion order | Duplicates allowed | append (no clear) |

### Two-type collections (parameterized by element type `T` and ordering/key type `P`/`K`)

| DSL Type | TypeKind | Syntax | Semantics | Ordering | Key constraint |
|----------|----------|--------|-----------|----------|---------------|
| `log by` | `LogBy` | `log of T by P` | Append-only, ordered by key P | P-value order | P unique across entries |
| `queue by` | `QueueBy` | `queue of T by P` | Priority queue | P-value order (ascending/descending) | P is ordering key |
| `lookup` | `Lookup` | `lookup of K to V` | Key-value map | None | K unique |

### Common properties

- **All collections are homogeneous** — element type `T` is a single Precept scalar type. No heterogeneous or polymorphic collections exist.
- **Elements are never null** — Precept has no `null` value. An element either exists or doesn't.
- **Collection fields start empty** — unless initialized by the initial event's rules.
- **Collections are mutable internally** (through event rules) but **immutable externally** (public API exposes read-only views).
- **The `optional` modifier does NOT apply to collection fields** — a collection field is always present; it may be empty but never "not set."

---

## §3 — The PreceptValue Leakage Problem

### What PreceptValue is

`PreceptValue` is the internal discriminated union (sealed class hierarchy) used by the evaluator to represent all typed values in the expression evaluation engine. It is the slot-storage representation — the evaluator operates uniformly on `PreceptValue[]` arrays.

### Why it must not leak

1. **Brittleness.** If `PreceptValue` appears in public signatures, internal evaluator refactoring (e.g., changing the slot model, adding new subtypes, restructuring the DU) becomes a breaking API change. The internal and external surfaces have different stability requirements.

2. **AI agent hostility.** An AI agent calling `precept_inspect` or `precept_compile` cannot reason about opaque internal types. When the API returns `IReadOnlyList<string>` or `IReadOnlyList<Money>`, the agent knows exactly what it's dealing with. If it returns `IReadOnlyList<PreceptValue>`, the agent must then figure out what `PreceptValue` is, what subtypes exist, how to cast — a multi-step reasoning chain that degrades accuracy.

3. **Contract violation.** The governing axiom (mini-spec Axiom 1): "No public method signature, return type, property type, or generic constraint exposes `PreceptValue`." Leaking it through collection element types would violate this axiom at the exact point where it's hardest to detect — inside a generic type parameter.

4. **Dual-shape model.** The architectural decision is settled: `PreceptValue` (class) is the internal slot-storage shape; CLR types (`long`, `decimal`, `Money`, etc.) are the external materialization shape. Collections don't get a special exemption from this rule — they're just the vectorized case of the same pattern.

### The specific collection risk

Without careful design, a collection field could leak `PreceptValue` in two ways:
- Returning `IReadOnlyList<PreceptValue>` directly (obvious violation)
- Returning `IReadOnlyList<object>` and boxing `PreceptValue.ToClr()` results (boxing violation — AI agents can't reason about `object`)

Both are rejected. The solution (per OQ-5 resolution) is the lazy adapter: an internal `PreceptList<T> : IReadOnlyList<T>` that projects `PreceptValue → T` on each index access, hiding the internal representation behind a strongly-typed generic interface.

---

## §4 — CLR Type Mapping — Candidates and Evaluation

### 4.1 Single-type collections (set, queue, stack, bag, list, log)

These all have the same shape: a collection of elements of type `T`.

| Option | CLR Type | Familiarity | Immutability | LINQ | AI Legibility | Allocation | Verdict |
|--------|----------|-------------|--------------|------|---------------|------------|---------|
| A | `IReadOnlyList<T>` | ✅ Universal | ✅ No mutation methods | ✅ Full | ✅ Clear and flat | Zero (adapter) | **Winner** |
| B | `T[]` | ✅ Universal | ❌ Mutable | ✅ Full | ✅ Clear | O(n) per access | Rejected — mutability |
| C | `ImmutableArray<T>` | ⚠️ Less known | ✅ Immutable | ✅ Full | ⚠️ Struct — confuses some | O(n) per access | Rejected — materialization cost |
| D | `IReadOnlyCollection<T>` | ✅ Known | ✅ No mutation | ⚠️ No indexer | ⚠️ No indexer — less intuitive | Zero | Rejected — no indexer |
| E | `ReadOnlyCollection<T>` | ✅ Known | ✅ No mutation | ✅ Full | ✅ Clear | O(n) per access | Rejected — requires materialization |
| F | Custom `PreceptSet<T>` etc. | ❌ Unknown | ✅ By design | ✅ Full | ❌ Opaque | Zero | Rejected — adds surface area |

**Locked decision: `IReadOnlyList<T>` for all single-type collections.**

Rationale:
- Consistent with the existing OQ-2 resolution
- Zero allocation (lazy adapter wraps `PreceptValue[]` without copying)
- Familiar to every C# developer and every AI agent
- Indexing, LINQ, `foreach` — all work naturally
- No collection-kind-specific CLR types — reduces API surface to minimum

### 4.2 Why not differentiate by collection kind?

One might argue: "A `set` is unordered — shouldn't it be `IReadOnlySet<T>`? A `queue` should be `IReadOnlyCollection<T>` without indexing?"

**No.** Rationale:
1. **Simplicity.** One CLR type for all single-type collections. Callers don't need to remember which Precept collection maps to which .NET interface.
2. **AI legibility.** An AI agent sees `IReadOnlyList<string>` and knows exactly what operations are available. Differentiating by kind adds cognitive load for zero benefit.
3. **`IReadOnlySet<T>` requires .NET 7+** — narrower compatibility window for marginal gain.
4. **Ordering is a DSL concern, not an API concern.** The public API is a read surface. Whether the elements are ordered by insertion, by key, or are unordered is determined by how the DSL rules mutate the collection — not by what the caller can do with the read-only view.
5. **Indexing is always safe.** Even for unordered collections, `IReadOnlyList<T>[i]` returns *some* element at position `i` in the internal array. The ordering may not be meaningful, but access is still valid and useful for enumeration.

### 4.3 Two-type collections (log by, queue by)

`log of T by P` and `queue of T by P` associate each element with an ordering key. The question: what does `Get<T>()` return?

**Options:**

| Option | CLR Type | Element shape | Pros | Cons |
|--------|----------|--------------|------|------|
| A | `IReadOnlyList<(T Value, P Key)>` | ValueTuple | Flat, familiar | Tuples are positional — `.Item1` hostile to AI |
| B | `IReadOnlyList<KeyedElement<T, P>>` | Named record | Named fields | Adds a public type |
| C | `IReadOnlyList<T>` (key access via separate API) | Element only | Simplest | Loses key association |

**Locked decision: Option B — `IReadOnlyList<KeyedElement<TValue, TKey>>`**

```csharp
public readonly record struct KeyedElement<TValue, TKey>(TValue Value, TKey Key);
```

Rationale:
- Named fields (`.Value`, `.Key`) are self-documenting — AI agents and humans both know what they're looking at
- `ValueTuple` names don't survive reflection — `(T, P)` becomes `(Item1, Item2)` in many tooling contexts
- The type is tiny (one generic struct) and carries no behavior — it's a pure data projection
- Avoids a separate "get keys" API that would force two round-trips to correlate element/key pairs

### 4.4 Lookup (key-value map)

`lookup of K to V` is a key-value map with unique keys. This is fundamentally a dictionary, not a list.

| Option | CLR Type | Pros | Cons |
|--------|----------|------|------|
| A | `IReadOnlyDictionary<TKey, TValue>` | Standard .NET map interface | Perfect fit |
| B | `IReadOnlyList<KeyValuePair<TKey, TValue>>` | Consistent with other collections | Loses O(1) key lookup semantics |
| C | Custom `PreceptLookup<K, V>` | Could add DSL-specific methods | Adds surface area |

**Locked decision: `IReadOnlyDictionary<TKey, TValue>`**

Rationale:
- `lookup` IS a map — the CLR type should reflect that
- `IReadOnlyDictionary<K, V>` gives callers `ContainsKey`, `TryGetValue`, indexer `[key]`, `Keys`, `Values` — all operations that map directly to lookup semantics
- Familiar to every C# developer; AI agents know exactly what this is
- No custom types needed
- The lazy adapter pattern applies here too: `PreceptLookup<K, V> : IReadOnlyDictionary<K, V>` wraps the internal representation and projects on access

---

## §5 — Recommended Mapping (Locked Decisions)

### DSL → CLR Type Mapping Table

| DSL Collection Type | DSL Syntax | CLR Public Type | `Get<T>()` Call |
|---------------------|-----------|-----------------|-----------------|
| `set` | `set of T` | `IReadOnlyList<TElement>` | `Get<IReadOnlyList<string>>("Tags")` |
| `queue` | `queue of T` | `IReadOnlyList<TElement>` | `Get<IReadOnlyList<string>>("AgentQueue")` |
| `stack` | `stack of T` | `IReadOnlyList<TElement>` | `Get<IReadOnlyList<string>>("RepairSteps")` |
| `bag` | `bag of T` | `IReadOnlyList<TElement>` | `Get<IReadOnlyList<string>>("CartItems")` |
| `list` | `list of T` | `IReadOnlyList<TElement>` | `Get<IReadOnlyList<string>>("ApprovalChain")` |
| `log` | `log of T` | `IReadOnlyList<TElement>` | `Get<IReadOnlyList<string>>("AuditTrail")` |
| `log by` | `log of T by P` | `IReadOnlyList<KeyedElement<TValue, TKey>>` | `Get<IReadOnlyList<KeyedElement<string, DateTimeOffset>>>("AuditLog")` |
| `queue by` | `queue of T by P` | `IReadOnlyList<KeyedElement<TValue, TKey>>` | `Get<IReadOnlyList<KeyedElement<string, long>>>("ClaimQueue")` |
| `lookup` | `lookup of K to V` | `IReadOnlyDictionary<TKey, TValue>` | `Get<IReadOnlyDictionary<string, decimal>>("CoverageLimits")` |

### Concrete Call Site Examples

```csharp
// set of string
IReadOnlyList<string> tags = version.Get<IReadOnlyList<string>>("Tags");
string firstTag = tags[0]; // string directly — no casting, no PreceptValue

// queue of string
IReadOnlyList<string> queue = version.Get<IReadOnlyList<string>>("AgentQueue");

// stack of string
IReadOnlyList<string> steps = version.Get<IReadOnlyList<string>>("RepairSteps");

// list of string
IReadOnlyList<string> chain = version.Get<IReadOnlyList<string>>("ApprovalChain");

// set of ~string (case-insensitive)
IReadOnlyList<string> labels = version.Get<IReadOnlyList<string>>("Labels");
// ~string projects as string in the CLR — CI semantics are a DSL constraint, not a CLR type

// log of string by instant
IReadOnlyList<KeyedElement<string, DateTimeOffset>> log = 
    version.Get<IReadOnlyList<KeyedElement<string, DateTimeOffset>>>("AuditLog");
string entry = log[0].Value;
DateTimeOffset when = log[0].Key;

// queue of string by integer (priority)
IReadOnlyList<KeyedElement<string, long>> pq = 
    version.Get<IReadOnlyList<KeyedElement<string, long>>>("ClaimQueue");

// lookup of string to decimal
IReadOnlyDictionary<string, decimal> limits = 
    version.Get<IReadOnlyDictionary<string, decimal>>("CoverageLimits");
decimal auto = limits["auto"]; // direct key access
bool hasHome = limits.ContainsKey("home");
```

### Public Type Addition

One new public type is required:

```csharp
namespace Precept.Runtime;

/// <summary>
/// An element paired with its ordering/priority key, as stored in keyed collections
/// (log of T by P, queue of T by P).
/// </summary>
public readonly record struct KeyedElement<TValue, TKey>(TValue Value, TKey Key);
```

This is the only custom type added. All other mappings use standard BCL interfaces.

---

## §6 — Element Type Mapping

The element type `T` in `IReadOnlyList<T>` is determined by the Precept scalar type declared in the collection's `of` clause. The mapping is the same as for scalar fields (per mini-spec §3.4):

| Precept Element Type | CLR Element Type (`T`) | Example Field | `Get<T>()` |
|---------------------|------------------------|---------------|------------|
| `string` | `string` | `set of string` | `Get<IReadOnlyList<string>>(...)` |
| `~string` | `string` | `set of ~string` | `Get<IReadOnlyList<string>>(...)` |
| `integer` | `long` | `list of integer` | `Get<IReadOnlyList<long>>(...)` |
| `decimal` | `decimal` | `bag of decimal` | `Get<IReadOnlyList<decimal>>(...)` |
| `number` | `double` | `set of number` | `Get<IReadOnlyList<double>>(...)` |
| `boolean` | `bool` | `set of boolean` | `Get<IReadOnlyList<bool>>(...)` |
| `date` | `DateOnly` | `log of date` | `Get<IReadOnlyList<DateOnly>>(...)` |
| `time` | `TimeOnly` | `queue of time` | `Get<IReadOnlyList<TimeOnly>>(...)` |
| `instant` | `DateTimeOffset` | `log of instant` | `Get<IReadOnlyList<DateTimeOffset>>(...)` |
| `duration` | `Duration` (NodaTime) | `set of duration` | `Get<IReadOnlyList<Duration>>(...)` |
| `money` | `Money` | `list of money` | `Get<IReadOnlyList<Money>>(...)` |
| `quantity` | `Quantity` | `set of quantity` | `Get<IReadOnlyList<Quantity>>(...)` |
| `choice` | `string` | `set of choice of string(...)` | `Get<IReadOnlyList<string>>(...)` |

### Key type mapping (for `log by P`, `queue by P`, `lookup of K to V`)

The key/ordering type `P` or `K` follows the same scalar mapping. Common patterns:

| DSL Syntax | CLR `KeyedElement<TValue, TKey>` | CLR Dictionary |
|-----------|----------------------------------|----------------|
| `log of string by instant` | `KeyedElement<string, DateTimeOffset>` | — |
| `queue of string by integer` | `KeyedElement<string, long>` | — |
| `lookup of string to decimal` | — | `IReadOnlyDictionary<string, decimal>` |
| `lookup of string to money` | — | `IReadOnlyDictionary<string, Money>` |

### Nested collections

**Precept does not support nested collections.** The grammar (`CollectionType := ... of ScalarType ...`) requires element types to be scalar types. You cannot declare `list of list of string` or `set of set of integer`. This is a deliberate language design constraint — it keeps the type system flat, avoids deeply nested generic types, and makes the CLR mapping straightforward.

---

## §7 — Null, Empty, and Missing Semantics

### The Precept value model has no null

Precept has no `null` value. Elements either exist in a collection or they don't. There is no "null element" concept.

### Empty vs. missing vs. not-set

| Condition | What it means | CLR representation |
|-----------|---------------|-------------------|
| Collection field is empty | Field exists, has zero elements | `IReadOnlyList<T>` with `.Count == 0` |
| Collection field has elements | Normal case | `IReadOnlyList<T>` with `.Count > 0` |
| Field not yet evaluated | Programmer error — check `FieldAccess` first | Throws `InvalidOperationException` |

### Key observations

1. **Collection fields are never null at the CLR level.** `Get<IReadOnlyList<string>>("Tags")` always returns a non-null `IReadOnlyList<string>`. It may have `Count == 0` but it is never `null`.

2. **The `optional` modifier does not apply to collections.** A collection field is always "set" — its value is the collection (possibly empty). The `is set` / `is not set` operators are not valid on collection fields.

3. **No nullable element types.** `IReadOnlyList<string>` never contains null elements. `IReadOnlyList<Money>` (where `Money` is a struct) cannot contain null by construction. For reference types (`string`), the runtime guarantees no null elements because Precept has no null literal.

4. **Dictionary values are never null.** `IReadOnlyDictionary<string, decimal>["key"]` always returns a valid `decimal`. Missing keys throw `KeyNotFoundException` per standard .NET dictionary semantics — callers use `TryGetValue` or `ContainsKey` for presence checks.

### Descriptor-level indication

`FieldDescriptor.ClrType` carries the full constructed generic type:
- For `set of string`: `typeof(IReadOnlyList<string>)`
- For `lookup of string to decimal`: `typeof(IReadOnlyDictionary<string, decimal>)`
- For `log of string by instant`: `typeof(IReadOnlyList<KeyedElement<string, DateTimeOffset>>)`

This is always non-nullable. Callers can rely on the descriptor's `ClrType` to know exactly what `Get<T>()` will return.

---

## §8 — Map/Dictionary Types (Lookup)

### The `lookup` collection

Precept has exactly one key-value map type: `lookup of K to V`.

- **Syntax:** `field CoverageLimits as lookup of string to decimal`
- **Keys are unique.** Duplicate key insertion (`put`) replaces the existing value.
- **Access:** DSL expression `CoverageLimits for "auto"` returns the value for key "auto". At the CLR level, this maps to dictionary indexer access.
- **Actions:** `put` (upsert), `remove` (by key), `clear`
- **Accessors:** `.count` only

### CLR surface

```csharp
IReadOnlyDictionary<string, decimal> limits = 
    version.Get<IReadOnlyDictionary<string, decimal>>("CoverageLimits");

// Standard dictionary operations
decimal autoLimit = limits["auto"];           // throws KeyNotFoundException if absent
bool hasHome = limits.ContainsKey("home");    // presence check
limits.TryGetValue("auto", out var val);      // safe access

// Enumeration
foreach (var kvp in limits)
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
```

### Internal implementation

An internal `PreceptLookup<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>` wraps the internal stride-2 `PreceptValue[]` backing array (keys at even indices, values at odd indices) and projects keys and values through the CLR↔PreceptValue mapping on access. Same lazy-adapter pattern as `PreceptList<T>`. Materialization to an internal dictionary occurs on first access for O(1) key lookup semantics.

### Key type constraints

In the DSL grammar, both `K` and `V` in `lookup of K to V` are `ScalarType`. This means:
- Keys can be: `string`, `~string`, `integer`, `decimal`, `number`, `boolean`, `date`, `time`, `instant`, `duration`, `money`, `quantity`, `choice`, etc.
- In practice, keys should support equality semantics. The DSL's `contains` operator on a lookup tests key membership.

---

## §9 — Open Questions

### OQ-C1: Bag element count exposure

A `bag of T` tracks element frequency (`.countof(E)` accessor in DSL). The current recommendation maps `bag` to `IReadOnlyList<T>` where duplicates appear multiple times. Alternative: should the bag expose a dedicated surface like `IReadOnlyList<(T Element, long Count)>` or `IReadOnlyDictionary<T, long>`?

**Lean:** Keep `IReadOnlyList<T>` with duplicates. Rationale: consistent with other collections; the `.countof(E)` accessor is a DSL expression concern (available in constraints/rules), not a public API read concern. If callers need frequency, they can use LINQ `GroupBy`. The DSL prevents the bag from growing unbounded through constraints.

**Decision needed from Shane:** Confirm `IReadOnlyList<T>` for bags, or prefer a frequency-aware surface.

### OQ-C2: `KeyedElement` naming

The `KeyedElement<TValue, TKey>` struct is a new public type. Alternative names considered:
- `OrderedEntry<TValue, TKey>` — emphasizes ordering
- `KeyedItem<TValue, TKey>` — slightly different connotation
- `CollectionEntry<TValue, TKey>` — too generic

**Lean:** `KeyedElement` is precise and neutral. The `Key` field serves both as an ordering key (for `queue by`, `log by`) and conceptually as a secondary value that the DSL associates with each element.

**Decision needed from Shane:** Confirm `KeyedElement<TValue, TKey>` or rename.

### OQ-C3: Direction modifier exposure

`queue of T by P ascending` vs `queue of T by P descending` — the ordering direction is part of the type declaration. Should `FieldDescriptor` carry an `IsDescending` flag or similar?

**Lean:** Yes — on `FieldDescriptor`, not on the CLR type. The CLR type is always `IReadOnlyList<KeyedElement<T, P>>` regardless of direction. The direction affects internal ordering (what `.peek` returns, what index 0 is) but not the type shape. A `SortDirection` property on the descriptor is sufficient for callers who need to know.

**Decision needed from Shane:** Confirm direction lives on descriptor only.

---

## §10 — Impact on Mini-Spec

The following changes are needed in `docs/working/runtime-api-public-surface-spec.md`:

### §3.4 CLR Type Table — Rows to Update

The current single row:

> | Collections (`List<T>`, etc.) | `IReadOnlyList<TElement>` | Element type is the scalar CLR projection... |

Should be expanded to three rows:

| Precept Type | Canonical CLR Type | Notes |
|---|---|---|
| `set of T`, `queue of T`, `stack of T`, `bag of T`, `list of T`, `log of T` | `IReadOnlyList<TElement>` | `TElement` is the scalar CLR projection of `T`. All six single-type collections share the same CLR surface. |
| `log of T by P`, `queue of T by P` | `IReadOnlyList<KeyedElement<TValue, TKey>>` | `TValue` is CLR projection of `T`; `TKey` is CLR projection of `P`. `KeyedElement<,>` is `readonly record struct(TValue Value, TKey Key)`. |
| `lookup of K to V` | `IReadOnlyDictionary<TKey, TValue>` | `TKey` is CLR projection of `K`; `TValue` is CLR projection of `V`. Standard .NET dictionary interface. |

### §13 — Additions needed

1. Add `KeyedElement<TValue, TKey>` definition to the public type inventory
2. Add the `PreceptLookup<K, V> : IReadOnlyDictionary<K, V>` internal adapter alongside `PreceptList<T>`
3. Add a note that `FieldDescriptor.ClrType` for keyed collections carries the full constructed generic (e.g., `typeof(IReadOnlyList<KeyedElement<string, long>>)`)

### §4 — ClrType Discovery

Add a note to §4.2 that for two-type collections, `FieldDescriptor.ClrType` encodes both type parameters. The descriptor-build-time wrapping logic must handle three cases:
1. Single-type collection → `typeof(IReadOnlyList<>).MakeGenericType(elementClrType)`
2. Keyed collection → `typeof(IReadOnlyList<>).MakeGenericType(typeof(KeyedElement<,>).MakeGenericType(elementClrType, keyClrType))`
3. Lookup → `typeof(IReadOnlyDictionary<,>).MakeGenericType(keyClrType, valueClrType)`

---

*End of public API investigation. All recommendations are grounded in the DSL's actual type system as implemented in `src/Precept/Language/Types.cs` and documented in `docs/language/precept-language-spec.md`. No assumptions were made about types that don't exist.*

---

## §11 — Internal Representation (frank-106, frank-108)

### Universal backing: `PreceptValue[]`

**All 9 collection kinds** use `PreceptValue[]` as their internal runtime representation. The specialized backing types originally specified in design docs (Okasaki pair-of-stacks, `ImmutableDictionary<T,int>`, `ImmutableList<T>`, `SortedDictionary<P, Queue<T>>`, etc.) are obsolete — they predate the runtime's actual slot model and never survived contact with it.

A collection field occupies one slot in the evaluator's `PreceptValue[]` working copy. That slot holds a `PreceptValue` with a collection tag variant whose reference region points to the element backing array.

### Layout conventions

| Collection Category | Kinds | Stride | Layout |
|---|---|---|---|
| **Single-value** | `list`, `set`, `queue`, `stack`, `log` | 1 | `[v₀, v₁, v₂, ...]` — one `PreceptValue` per element |
| **Pair** | `lookup`, `bag`, `log by P`, `queue by P` | 2 | `[k₀, v₀, k₁, v₁, ...]` — even indices = key/element, odd indices = value/frequency |

The stride is a **kind-specific layout convention within the same CLR type**, not a type boundary. The evaluator never dispatches on "is this a 1D or 2D array?" — it's always `PreceptValue[]`.

### Pair collection layout rationale (frank-108)

Stride-2 flat array wins decisively over alternatives:

| Alternative | Non-Negotiable Killer |
|---|---|
| `PreceptValue[,]` (2D rectangular) | **No `ArrayPool` support.** Cannot pool multidimensional arrays. Allocation on every mutation. |
| `PreceptValue[][]` (jagged) | N+1 heap allocations per collection. Catastrophic GC pressure. No spatial locality. |
| `(PreceptValue, PreceptValue)[]` (struct tuple) | **Second CLR type = second pool.** Breaks type uniformity. Dispatch boundary at slot layer. |

**Ergonomics mitigation** — `ref` helper accessors provide named-field readability without changing the backing type:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static ref PreceptValue Key(PreceptValue[] arr, int i) => ref arr[i * 2];

[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static ref PreceptValue Val(PreceptValue[] arr, int i) => ref arr[i * 2 + 1];
```

### Why this representation

1. **Consistency with the slot model.** The evaluator operates on `PreceptValue[]` slot arrays. Collections are slots. Same type eliminates a type boundary.
2. **One pool.** `ArrayPool<PreceptValue>.Shared` serves both the slot working copy and all collection backing arrays. No secondary pools.
3. **Structural sharing is worthless.** Precept's versioning model is replace-the-whole-thing. No multi-version tree where shared tails save memory.
4. **Collection semantics belong in the evaluator.** The evaluator owns all mutation logic via prebuilt action plans. The data structure is dumb storage.
5. **Performance is fine at Precept's scale.** Business entity collections are small. Copying them is free relative to the evaluator's per-Fire cost.

### What this obsoletes

All specified backing types are obsolete as implementation guidance:
- `ImmutableLog<T>` (Okasaki pair-of-stacks)
- `ImmutableDictionary<T, int>` (bag backing)
- `ImmutableList<T>` (list backing)
- `SortedDictionary<TPriority, Queue<TElement>>` (queue-by-P backing)
- `ImmutableDictionary<K, V>` (lookup backing)
- Custom immutable sorted linked list (log-by-P backing)

All replaced by: **`PreceptValue[]` with evaluator-enforced invariants.**

---

## §12 — Scalability and Lazy-Load Seam (frank-107)

### Size safety zones

| Threshold | Response |
|-----------|----------|
| **<500 elements** | Don't think about it. Well within acceptable bounds. |
| **500–2,000 elements** | `maxcount` should be documented as best practice. Lint warning for logs without `maxcount`. |
| **>2,000 elements** | Yellow zone. Per-event copy cost is 64–160 KB. Starting to dominate evaluator's memory budget. |
| **>10,000 elements** | Design smell. Entity needs archival, snapshotting, or external log storage. |

### The dangerous kind: `log`

`log of T` and `log of T by P` are the **only structurally unbounded kinds.** Every other kind has natural drainage (queue/stack dequeue/pop), replacement semantics (set add/remove, lookup put), or explicit user intent to bound (list, bag).

**`maxcount` should be mandatory guidance for logs.** Without it, logs grow without bound. At 2K entries, recommend archival pattern (snapshot + external store).

### Lazy-load extensibility seam

`PreceptValue[]` does NOT lock us in. The evaluator's slot indirection already provides the extensibility seam:

1. The evaluator never indexes into collection backing directly — kind-specific action logic does.
2. The `PreceptValue` reference region is a pointer — can point to anything without changing struct layout.
3. Action logic is already factored per-kind — adding an overload path is a local refactor.

**Future upgrade path** (deferred — do NOT implement prematurely):

```csharp
internal interface ICollectionBacking
{
    PreceptValue[] Materialize();        // force full array (for commit)
    int Count { get; }                   // cheap for both lazy and eager
    PreceptValue ElementAt(int index);   // lazy window access
}
```

Ship `PreceptValue[]` with no abstraction layer. The seam exists architecturally. Don't pay for it until needed.

---

## §13 — Collection Action Architecture (frank-109)

### Decision: Static helper methods, NOT wrapper types

Collection mutation logic lives as **static methods in a companion `CollectionActions` class**, called directly by the evaluator's action dispatch. No class instances, no lifecycle ownership, no type boundary between the evaluator and the backing array.

### The correct shape

```csharp
/// <summary>
/// In-place mutation helpers for collection operations. Each method receives a
/// mutable Span (evaluator-owned working copy) and returns the new logical count.
/// The evaluator owns the CoW boundary and pool lifecycle.
/// </summary>
internal static class CollectionActions
{
    // === Single-value kinds (stride 1) ===
    public static int AddToSet(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int Enqueue(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int Push(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int AppendToLog(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int InsertAt(Span<PreceptValue> backing, int count, int index, PreceptValue element) { ... }

    // === Pair kinds (stride 2) ===
    public static int PutLookup(Span<PreceptValue> backing, int count, PreceptValue key, PreceptValue value) { ... }
    public static int AddToBag(Span<PreceptValue> backing, int count, PreceptValue element) { ... }
    public static int EnqueueByPriority(Span<PreceptValue> backing, int count, PreceptValue element, PreceptValue priority) { ... }

    // === Stride-2 ergonomic helpers ===
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PreceptValue Key(PreceptValue[] arr, int i) => ref arr[i * 2];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref PreceptValue Val(PreceptValue[] arr, int i) => ref arr[i * 2 + 1];
}
```

### Why NOT wrapper types

| Concern | Why wrappers lose |
|---------|-------------------|
| **Pool lifecycle ambiguity** | Some backing arrays are pool-rented working copies; others are committed version arrays. The evaluator knows which is which. A wrapper type cannot safely manage pool returns without knowing array provenance. |
| **Catalog duplication** | Wrapper types would be behavioral clones of catalog-specified semantics hardcoded into C# classes — exactly the parallel implementation the catalog architecture prohibits. |
| **ICollectionBacking migration friction** | Wrappers become technical debt the moment `ICollectionBacking` arrives — they're either the interface implementation (then not wrappers) or gratuitous indirection. |
| **Evaluator complexity** | Wrap-construct-call-extract overhead on every mutation. Static helpers are one line: `CollectionActions.Add(span, count, element)`. |

### Design principles preserved

- **Evaluator owns ALL pool lifecycle decisions** — no other actor rents or returns.
- **CollectionActions has no state, no lifecycle, no ownership** — pure computation on caller-provided buffers.
- **Independently testable** as pure functions with zero ceremony.
- **Clean migration path** to `ICollectionBacking` — change parameter types, nothing else.

---

## §14 — Copy-on-Write Protocol for Multi-Mutation Events (frank-110)

### The problem

Multiple mutations to the same collection slot within one event handler:

```precept
on OrderPlaced:
    add item1 to items
    add item2 to items
    add item3 to items
```

With naive per-mutation CoW: 3 allocations, 2 immediately discarded. At scale (200-entry log appended to 5 times = 5 allocations × 200 elements) this compounds to O(N×K).

### Solution: Option C-2 — Evaluator's working-copy model handles this

The evaluator's existing architecture already creates the conditions for efficient multi-mutation. The protocol:

| Step | Actor | Action |
|------|-------|--------|
| **First mutation** to collection slot | Evaluator | Detects alias via `ReferenceEquals(currentBacking, originalSlots[slot].CollectionBacking)`. Rents working array from pool. Copies existing elements. |
| **All mutations** (including first) | CollectionActions | Mutates in-place on the Span. Returns new count. **Never allocates.** |
| **Subsequent mutations** to same slot | Evaluator | Detects backing is already private (not aliased). Passes directly — no clone. |
| **Commit** (success) | Evaluator | Working array in slot IS the new version's backing. Zero-copy promotion (donated to Version). |
| **Discard** (constraint failure) | Evaluator | Returns all working collection arrays to pool. |
| **Resize** (capacity exceeded) | Evaluator | Rents larger array, copies, returns old. |

### Cost model

| Scenario | Naive (Option A) | Option C-2 |
|----------|-----------------|------------|
| 3 adds to empty set | 3 allocs, 0+1+2 copies | 1 alloc (capacity 3+), 3 in-place writes |
| 5 appends to 200-entry log | 5 allocs, 1010 total copies | 1 alloc, 200 copies, 5 in-place writes |
| **1 add to set (common case)** | 1 alloc, K copies | **1 alloc, K copies (identical — zero overhead)** |

**Performance:** O(K) + O(N) for K mutations on N-element collection (not O(N×K)).

### CollectionActions signature refinement

frank-109 originally stated "pure functions: array in, array out." This is refined to:

> **"Span in, count out."** CollectionActions receives `Span<PreceptValue>` (a mutable view of the evaluator's working array) and returns the new element count. The evaluator owns the CoW boundary — not CollectionActions.

The architectural principles are preserved and strengthened:
- ✅ No wrapper types
- ✅ Evaluator owns pool lifecycle (strengthened — only actor touching pool)
- ✅ Static helper class
- ✅ CollectionActions has no state, no lifecycle, no ownership

### ArrayPool lifecycle — unambiguous

| Array Type | Who Rents | Who Returns | When |
|-----------|-----------|-------------|------|
| Committed version backing | Nobody (donated from previous Fire) | Nobody (GC'd with Version) | N/A |
| Working collection array | Evaluator (on first mutation) | Evaluator (constraint failure) OR donated (commit success) | End of row |
| Resized array | Evaluator (capacity exceeded) | Old: returned immediately. New: same lifecycle as working. | Resize point |

### Rollback semantics

On constraint failure, the evaluator walks the working copy and returns any collection backing that diverged from the original:

```csharp
for (int i = 0; i < workingCopy.Length; i++)
{
    if (workingCopy[i].IsCollection &&
        !ReferenceEquals(workingCopy[i].GetCollectionBacking(), originalSlots[i].GetCollectionBacking()))
    {
        ArrayPool<PreceptValue>.Shared.Return(workingCopy[i].GetCollectionBacking());
    }
}
```

### Tradeoff accepted

- **Evaluator action dispatch is slightly more complex** — `ReferenceEquals` check + first-mutation clone adds ~5 lines per collection action dispatch path. Accepted because the alternative (N unnecessary allocations) is worse.
- **CollectionActions methods mutate their Span argument** — not "pure" in the strictest sense. Accepted because behavior depends only on inputs (no external state), and mutation IS the correct semantic.

---

## §15 — CLR Public API Direction (frank-105)

### Two-lane symmetry preserved

The runtime API speaks two value languages:
- **Raw lane:** `version["fieldName"]` → `JsonElement`
- **Typed lane:** `version.Get<T>("fieldName")` → `T`

Collections participate in BOTH lanes. `Get<IReadOnlyList<string>>("Tags")` is the typed read. `version["Tags"]` returns the JSON array. Neither is optional.

### Adapter materialization strategy

The adapter (`PreceptList<T> : IReadOnlyList<T>`) materializes to an internal `T[]` **on first access** (eager-on-first-read). This is:
- O(n) once per Version per field read
- At Precept's scale (typically ≤100 elements), sub-microsecond
- After materialization, indexing is O(1)

The "zero-copy per-index projection" claim from earlier analysis is corrected: **"lazy" means lazy at the Version level** (adapter constructed on first field read), not lazy at the element level.

### `log` — potential dedicated interface (still open)

`log` may warrant a dedicated `IReadOnlyLog<T>` that omits the indexer and exposes only `.First`, `.Last`, `.Count`, `IEnumerable<T>`. This remains open — current default is `IReadOnlyList<T>` with materialization.

---

*End of investigation. All architectural decisions are locked. Open questions (OQ-C1, OQ-C2, OQ-C3) remain for Shane confirmation.*
