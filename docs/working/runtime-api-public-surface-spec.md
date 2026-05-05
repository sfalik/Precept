# Runtime API — Public Surface Specification

**By:** Frank  
**Date:** 2026-05-04 (updated)  
**Status:** Draft — OQ-1 through OQ-5 resolved; OQ-C1/C2/C3 resolved and locked (2026-05-04)  
**Purpose:** Authoritative specification for the redesigned public API surface. Drives `docs/runtime/runtime-api.md` updates after approval.

---

## Governing Decisions

| Decision | Source |
|----------|--------|
| `PreceptValue` → `internal` | `frank-preceptvalue-internal.md` |
| Registration surface eliminated | `frank-registration-surface-rethink.md` |
| CLR type discovery via `TypeMeta.ClrType` + descriptors | `frank-clrtype-discovery.md` |

**Axioms:**
1. No public method signature, return type, property type, or generic constraint exposes `PreceptValue`.
2. The type system is closed. There is no public registration surface.
3. The public API speaks two value languages: `JsonElement` (raw lane) and `T` via `Get<T>()` (typed lane).
4. Discovery of valid `T` is metadata, not machinery.

**Why Axiom 1 is non-negotiable:**
- **Brittleness** — `PreceptValue` evolves with the evaluator (new subtypes, restructured slot model). Exposing it makes internal refactoring a breaking API change. The internal and external surfaces have different stability requirements.
- **AI agent hostility** — An AI agent calling `precept_inspect` or `precept_compile` cannot reason about opaque internal types. `IReadOnlyList<string>` is immediately actionable; `IReadOnlyList<PreceptValue>` requires the agent to discover what `PreceptValue` is, enumerate subtypes, and figure out how to cast — a multi-step recovery chain that degrades accuracy.
- **Contract** — Generic type parameters are the hardest leakage vector to detect: `IReadOnlyList<PreceptValue>` satisfies `IReadOnlyList<T>` at the call site but exposes the internal type inside the generic. The collection type mapping exists specifically to close this gap.
- **Dual-shape model** — `PreceptValue` is the internal slot-storage shape; CLR types are the external materialization shape. Collections are the vectorized case of this rule — not an exception to it.

---

## §1 Core Operations

### 1.1 `Precept.From` — Construction

```csharp
public static Precept From(Compilation compilation);
```

Unchanged. Returns the executable model. Only succeeds with an error-free `Compilation`.

### 1.2 `Precept.Create` — Entity Construction

```csharp
// JSON lane
public EventOutcome Create(JsonElement? args = null);

// Typed lane
public EventOutcome Create(Action<IArgBuilder>? args = null);
```

**Parameters:**
- `args` (JSON lane): A `JsonElement` representing a JSON object where each property is an arg name and each value is the arg's value in the Precept type's canonical JSON representation. Example: `{"Amount": 500.00, "ApplicantName": "Jane Doe"}`. Pass `null` or omit for precepts without an initial event.
- `args` (typed lane): Builder callback. Each `Set<T>(name, value)` call sets one arg.

**Returns:** `EventOutcome` (7 variants — see §2.1).

### 1.3 `Precept.InspectCreate` — Preview Construction

```csharp
// JSON lane
public EventInspection InspectCreate(JsonElement? args = null);

// Typed lane
public EventInspection InspectCreate(Action<IArgBuilder>? args = null);
```

Same parameter semantics as `Create`. Returns `EventInspection` (see §2.3).

### 1.4 `Precept.FromJson` — Entity Restoration

```csharp
public Version FromJson(JsonElement document);
```

Accepts a full persistence envelope (as produced by `Version.ToJson()`) and hydrates a `Version`.

**Parameters:**
- `document`: A `JsonElement` representing the persistence envelope. Must contain `$precept` and `fields`; `$state` is optional (absent means stateless).

**Behavior:**
- Validates that `document["$precept"]` matches `this.Name` (case-sensitive). Throws `ArgumentException` if mismatched — this prevents accidentally restoring a document against the wrong precept definition.
- Extracts `$state` and `fields` from the document, then hydrates a `Version`.
- Unknown `$`-prefixed properties (e.g., `$id`, `$version`) are silently ignored — this enables forward compatibility. A document produced by a future version of the runtime (which emits `$version`) can still be restored by an older runtime that doesn't know about that property.

**Returns:** `Version` — the hydrated entity snapshot.

**Throws:**
- `ArgumentException` — if `$precept` is missing, or doesn't match `this.Name`, or `fields` is missing/not an object, or any field-level validation error.

**No typed overload.** FromJson is a hydration path from serialized storage.

**No constraint validation.** Pure hydration — loads data as-is without running constraints. Constraints fire only on `Fire` and `Update`. If the precept definition has evolved since the data was stored, FromJson still succeeds. The caller is responsible for migrating data or triggering revalidation via `Update` if schema evolution requires it.

**Round-trip contract:**

```csharp
Version restored = precept.FromJson(original.ToJson());
// restored.State == original.State
// restored.ToJson().ToString() == original.ToJson().ToString()
```

### 1.5 `Version.Fire` — Event Commit

```csharp
// JSON lane
public EventOutcome Fire(string eventName, JsonElement? args = null);

// Typed lane
public EventOutcome Fire(string eventName, Action<IArgBuilder>? args = null);
```

**Parameters:**
- `eventName`: The event to fire.
- `args` (JSON lane): JSON object of arg values, or `null` for events with no args / all-optional args.
- `args` (typed lane): Builder callback for typed arg construction.

**Returns:** `EventOutcome` (7 variants).

### 1.6 `Version.Update` — Field Write Commit

```csharp
// JSON lane
public UpdateOutcome Update(JsonElement? fields = null);

// Typed lane
public UpdateOutcome Update(Action<IFieldBuilder>? fields = null);
```

**Parameters:**
- `fields` (JSON lane): JSON object of field patches. Each property is a field name, each value is the new value. Only patched fields need be present.
- `fields` (typed lane): Builder callback. Each `Set<T>(name, value)` call patches one field.

**Returns:** `UpdateOutcome` (4 variants — see §2.2).

### 1.7 `Version.InspectFire` — Event Preview

```csharp
// JSON lane
public EventInspection InspectFire(string eventName, JsonElement? args = null);

// Typed lane
public EventInspection InspectFire(string eventName, Action<IArgBuilder>? args = null);
```

Same parameter semantics as `Fire`. Returns `EventInspection`.

### 1.8 `Version.InspectUpdate` — Update Preview

```csharp
// JSON lane
public UpdateInspection InspectUpdate(JsonElement? fields = null);

// Typed lane
public UpdateInspection InspectUpdate(Action<IFieldBuilder>? fields = null);
```

Same parameter semantics as `Update`. Returns `UpdateInspection`.

### 1.9 `Version` — Field Read

```csharp
// Raw lane
public JsonElement this[string fieldName] { get; }

// Typed lane
public T Get<T>(string fieldName);
```

**`this[fieldName]`** — Returns the field's current value as `JsonElement`.

**`Get<T>(fieldName)`** — Returns the field's value deserialized as `T`. Valid `T` values are the CLR types listed in §3.4. An invalid `T` throws `InvalidOperationException`.

Both throw `InvalidOperationException` for omitted/unresolved fields — these are programmer errors. Check `FieldAccess` first if field presence is uncertain.

### 1.10 `Version.ToJson` — Full Persistence Document

```csharp
public JsonElement ToJson();
```

Returns a self-contained JSON document (the **persistence envelope**) that includes the precept name, current state, and all resolved field data. This is the recommended serialization path for storing a `Version` in a database, message, or file.

**Document structure:**

```json
{
  "$precept": "LoanApplication",
  "$state": "UnderReview",
  "fields": {
    "amount": 50000.00,
    "applicantName": "Jane Doe",
    "riskScore": 72
  }
}
```

**Envelope properties (locked — v1):**

| Property | Type | Description |
|----------|------|-------------|
| `$precept` | `string` | The precept definition name (`Precept.Name`). Always present. |
| `$state` | `string?` | Current state name, or absent for stateless precepts. |
| `fields` | `object` | Field data. Unresolved fields omitted. |

**Reserved property names (future expansion — NOT emitted in v1):**

| Property | Intended purpose | Notes |
|----------|-----------------|-------|
| `$id` | Entity identity | Caller-assigned persistent identifier |
| `$version` | Optimistic concurrency token | Monotonic integer or opaque string |
| `$timestamp` | Last-modified instant | ISO 8601 UTC |
| `$schemaVersion` | Precept definition version | For migration detection; identifies which version of the precept definition produced this document |
| `$envelopeVersion` | Envelope format version | Defaults to `1` when absent; only emitted if the envelope format itself changes |

**Conventions:**
- All envelope metadata uses the `$` prefix — borrowed from JSON Schema / JSON-LD convention. This avoids any collision with user-defined field names (which cannot start with `$`).
- Field data lives under the `fields` key — never at the top level. This makes the envelope extensible without risking property-name collisions between envelope metadata and domain fields.
- Property names are camelCase throughout (both envelope and fields).

**Never throws.** Always succeeds.

---

## §2 Result Types

### 2.1 `EventOutcome` (discriminated union — 7 variants)

```csharp
public abstract record EventOutcome
{
    public sealed record Transitioned(Version Result, FiredArgs Args) : EventOutcome;
    public sealed record Applied(Version Result, FiredArgs Args) : EventOutcome;
    public sealed record Rejected(string Reason, FiredArgs Args) : EventOutcome;
    public sealed record InvalidArgs(string Reason) : EventOutcome;
    public sealed record ConstraintsFailed(ImmutableArray<ConstraintViolation> Violations) : EventOutcome;
    public sealed record Unmatched() : EventOutcome;
    public sealed record UndefinedEvent() : EventOutcome;
}
```

**No `PreceptValue` on any variant.** `FiredArgs` provides both raw (`JsonElement`) and typed (`Get<T>()`) access to submitted args.

### 2.2 `UpdateOutcome`

```csharp
public abstract record UpdateOutcome
{
    public sealed record Updated(Version Result) : UpdateOutcome;
    public sealed record ConstraintsFailed(ImmutableArray<ConstraintViolation> Violations) : UpdateOutcome;
    public sealed record FieldNotEditable(string FieldName, FieldAccessMode ActualMode) : UpdateOutcome;
    public sealed record InvalidFields(string Reason) : UpdateOutcome;
}
```

**`RestoreOutcome` — removed.** `FromJson` returns `Version` directly (see §1.4). Invalid inputs throw `ArgumentException` — they are programmer errors, not business outcomes.

### 2.3 Inspection Types

```csharp
public sealed record EventInspection(
    Prospect OverallProspect,
    ImmutableArray<TransitionInspection> Transitions,
    ImmutableArray<ConstraintResult> EventEnsures,
    ImmutableArray<FieldSnapshot> FieldSnapshots);

public sealed record TransitionInspection(
    Prospect Prospect,
    string? TargetState,
    ImmutableArray<ConstraintResult> Constraints,
    ImmutableArray<FieldSnapshot> PostFields);

public sealed record UpdateInspection(
    ImmutableArray<FieldSnapshot> Fields,
    ImmutableArray<ConstraintResult> Constraints,
    ImmutableArray<EventInspection> Events);

public enum Prospect { Certain, Possible, Impossible }
```

### 2.4 `FieldSnapshot`

```csharp
public sealed record FieldSnapshot(
    string FieldName,
    FieldAccessMode Mode,
    string FieldType,
    bool IsResolved,
    JsonElement? Value,
    Type ClrType);
```

- `Value`: The field's current value as `JsonElement`. `null` when `IsResolved == false` (unresolved computed field or structurally absent).
- `ClrType`: The valid CLR type for `Get<T>()` on this field. Precomputed from `FieldDescriptor.ClrType`.
- Typed access: `Get<T>()` is NOT on `FieldSnapshot`. Consumers use the `Value` JsonElement or call `version.Get<T>(fieldName)` on the `Version` instance. `FieldSnapshot` is a diagnostic/inspection record — not an active accessor.

### 2.5 `FieldAccessInfo`

```csharp
public sealed record FieldAccessInfo(
    FieldDescriptor Field,
    FieldAccessMode Mode,
    JsonElement CurrentValue);

public enum FieldAccessMode { Readonly, Editable }
```

`CurrentValue` is `JsonElement` — the field's value serialized to its canonical JSON form. Internally produced by calling `TypeRuntimeMeta.WriteJson` on the slot's `PreceptValue`.

### 2.6 `FiredArgs`

```csharp
public sealed class FiredArgs
{
    public static readonly FiredArgs Empty;

    public JsonElement this[string name] { get; }
    public T Get<T>(string name);
    public bool TryGet<T>(string name, out T value);
}
```

- Indexer returns `JsonElement` — the submitted arg value in canonical JSON form.
- `Get<T>()` — typed access (see §3).
- `TryGet<T>()` — returns `false` for absent optional args (see §3.3).

### 2.7 `ConstraintViolation`

```csharp
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,
    string? Because,
    ImmutableArray<FieldSnapshot> RelevantFields,
    string? FailingSubexpression,
    JsonElement? FailingValue);
```

- `FailingValue`: The value at the failure site as `JsonElement?`. Nullable because some constraints (relational, multi-field) don't have a single attributable failing value.
- `RelevantFields`: Snapshots carry `JsonElement?` values (per §2.4).

### 2.8 `ConstraintResult`

```csharp
public sealed record ConstraintResult(
    ConstraintDescriptor Constraint,
    IReadOnlyList<string> FieldNames,
    ConstraintStatus Status);

public enum ConstraintStatus { Satisfied, Violated, Unresolvable }
```

Unchanged — no `PreceptValue` exposure.

### 2.9 `ConstraintDescriptor`

```csharp
public sealed record ConstraintDescriptor(
    ConstraintKind Kind,
    string? ScopeTarget,
    string ExpressionText,
    string Because,
    IReadOnlyList<string> ReferencedFields,
    bool HasGuard,
    int SourceLine);

public enum ConstraintKind { Rule, StateEnsureIn, StateEnsureTo, StateEnsureFrom, EventEnsure }
```

Unchanged — already clean.

### 2.10 Descriptors

```csharp
public sealed record FieldDescriptor(
    string Name,
    TypeKind Type,
    int SlotIndex,
    IReadOnlyList<ModifierKind> Modifiers,
    string? DefaultExpression,
    bool IsComputed,
    int SourceLine,
    Type ClrType);       // ← resolved at Precept Builder time

public sealed record ArgDescriptor(
    string Name,
    TypeKind Type,
    bool IsOptional,
    string? DefaultExpression,
    int SourceLine,
    Type ClrType);       // ← resolved at Precept Builder time

public sealed record EventDescriptor(
    string Name,
    IReadOnlyList<ArgDescriptor> Args,
    int SourceLine);
```

`ClrType` on both `FieldDescriptor` and `ArgDescriptor` is `System.Type` — the CLR projection valid for `Get<T>()` / `Set<T>()`. For collection-typed fields, this carries the collection CLR type (e.g., `typeof(IReadOnlyList<long>)` for `List<integer>`). Resolved at Precept Builder time from `TypeMeta.ClrType` + collection wrapping.

---

## §3 The Typed Lane — `Get<T>()` Design

### 3.1 Where `Get<T>()` Appears

| Location | Signature | Purpose |
|----------|-----------|---------|
| `Version.Get<T>(string fieldName)` | `public T Get<T>(string fieldName)` | Read a field value as a typed CLR value |
| `FiredArgs.Get<T>(string name)` | `public T Get<T>(string name)` | Read a submitted arg value as typed CLR value |

`Get<T>()` does NOT appear on `FieldSnapshot`, `FieldAccessInfo`, or `ConstraintViolation`. These are diagnostic/inspection records that carry `JsonElement` values for serialization and display. Active typed access is on the entity (`Version`) and the args container (`FiredArgs`).

### 3.2 Resolution Semantics

```csharp
// Internal resolution (conceptual)
T Get<T>(string fieldName)
{
    PreceptValue slot = this.Slots[ResolveSlotIndex(fieldName)];
    TypeRuntime<T> runtime = TypeRuntimeTable.Lookup<T>();  // internal, closed lookup
    if (runtime == null)
        throw new InvalidOperationException(
            $"Type '{typeof(T).Name}' is not a valid CLR projection for Precept values. " +
            $"Valid types: int, long, decimal, string, bool, DateOnly, TimeOnly, DateTimeOffset, ...");
    if (!IsCompatible(field.Type, typeof(T)))
        throw new InvalidOperationException(
            $"Cannot read field '{fieldName}' (Precept type '{field.Type}') as '{typeof(T).Name}'. " +
            $"Expected: '{field.ClrType.Name}'.");
    return runtime.ToClr(slot);
}
```

**Resolution order:**
1. Resolve the field/arg name to its slot index (throws `KeyNotFoundException` if undefined).
2. Look up `TypeRuntime<T>` in the internal table keyed by `typeof(T)` (throws `InvalidOperationException` if `T` is not a recognized CLR projection).
3. Validate that `T` is compatible with the field's declared Precept type (throws `InvalidOperationException` if mismatched).
4. Call `TypeRuntime<T>.ToClr(slotValue)` to perform the conversion.

### 3.3 `TryGet<T>()` Variant

```csharp
public bool TryGet<T>(string name, out T value);
```

Present on `FiredArgs` only. Returns `false` when:
- The arg was not provided (optional arg, absent).

Does NOT swallow type errors — a mismatched `T` still throws `InvalidOperationException`. `TryGet` handles presence/absence, not type safety.

**Not on `Version`.** A field is either resolved or omitted. Omitted fields throw on indexer access (programming error). Resolved fields always have a value. There is no "maybe present" semantic for fields.

### 3.4 Valid CLR Types (Closed Set)

| Precept Type | Canonical CLR Type (`T`) | Notes |
|---|---|---|
| `integer` | `long` | No `Get<int>()` convenience overload — callers cast themselves (OQ-1 resolved) |
| `decimal` | `decimal` | |
| `number` | `double` | |
| `string` / `text` | `string` | |
| `boolean` | `bool` | |
| `date` | `DateOnly` | |
| `time` | `TimeOnly` | |
| `instant` | `DateTimeOffset` | |
| `duration` | `Duration` (NodaTime) | Same pattern as all other temporal types — direct NodaTime usage, no wrapper (OQ-4 resolved) |
| `choice` | `string` | The selected variant name |
| `money` | `Money` | `readonly record struct(decimal Amount, string Currency)` — Currency is ISO 4217 string. NOT a unit. (OQ-3d, OQ-3e resolved) |
| `currency` | `string` | ISO 4217 code |
| `quantity` | `Quantity` | `readonly record struct(decimal Amount, Unit Unit)` — Unit identity is UCUM code (OQ-3a, OQ-3e resolved) |
| `Unit` | `Unit` | `sealed class` — UCUM-identified unit value. Database-backed, no static members. `sealed class` (not struct) because units are interned: `UnitCatalog.Get("kg")` returns the same instance every time, enabling reference equality as a fast path. (OQ-3c resolved) |
| `stateref` | `string` | The state name |
| `set of T`, `queue of T`, `stack of T`, `bag of T`, `list of T`, `log of T` | `IReadOnlyList<TElement>` | `TElement` is the scalar CLR projection of `T`. All six single-type collections share the same CLR surface. `TypeMeta.ClrType` is scalar only; wrapping applied at descriptor-build time (OQ-2 resolved). See `docs/working/precept-collection-types-investigation.md`. |
| `log of T by P`, `queue of T by P` | `IReadOnlyList<KeyedElement<TValue, TKey>>` | `TValue` is CLR projection of `T`; `TKey` is CLR projection of `P`. `KeyedElement<TValue, TKey>` is `readonly record struct(TValue Value, TKey Key)`. |
| `lookup of K to V` | `IReadOnlyDictionary<TKey, TValue>` | `TKey` is CLR projection of `K`; `TValue` is CLR projection of `V`. Standard .NET dictionary interface — `ContainsKey`, `TryGetValue`, indexer `[key]`, `Keys`, `Values`. |

**Collection `Get<T>()` call-site examples:**

```csharp
// set of string
IReadOnlyList<string> tags = version.Get<IReadOnlyList<string>>("Tags");

// list of integer
IReadOnlyList<long> scores = version.Get<IReadOnlyList<long>>("Scores");

// log of string by instant (keyed collection)
IReadOnlyList<KeyedElement<string, DateTimeOffset>> log =
    version.Get<IReadOnlyList<KeyedElement<string, DateTimeOffset>>>("AuditLog");
string entry = log[0].Value;
DateTimeOffset when = log[0].Key;

// queue of string by integer (priority queue)
IReadOnlyList<KeyedElement<string, long>> pq =
    version.Get<IReadOnlyList<KeyedElement<string, long>>>("ClaimQueue");

// lookup of string to decimal (map)
IReadOnlyDictionary<string, decimal> limits =
    version.Get<IReadOnlyDictionary<string, decimal>>("CoverageLimits");
decimal auto = limits["auto"];
bool hasHome = limits.ContainsKey("home");
```

**Collection null/empty semantics:**
- Collections are **never null** at the CLR level. `Get<IReadOnlyList<string>>("Tags")` always returns a non-null instance.
- Empty collections have `Count == 0`. Missing/unset fields return an empty collection, not null.
- The `optional` modifier does not apply to collection fields — they are always present (possibly empty).
- Collection elements are never null — Precept has no `null` value.
- Dictionary values are never null; missing keys throw `KeyNotFoundException` per standard .NET dictionary semantics.

**`KeyedElement<TValue, TKey>` — new public type:**

```csharp
namespace Precept.Types;

/// <summary>
/// An element paired with its ordering/priority key, as stored in keyed collections
/// (log of T by P, queue of T by P).
/// </summary>
public readonly record struct KeyedElement<TValue, TKey>(TValue Value, TKey Key);
```

This is the only custom collection type added. All other mappings use standard BCL interfaces.

This is the **complete, closed set.** No external types can be added. The internal `TypeRuntime<T>` table is populated at static initialization for every entry in this table and sealed.

### 3.5 Throws on Invalid `T`

```
InvalidOperationException:
  "Cannot read field 'amount' (Precept type 'decimal') as 'Int32'. Expected: 'Decimal'."
```

The exception message names the field, the Precept type, the invalid `T`, and the expected CLR type. Enough information for the developer to fix the call immediately.

---

## §4 CLR Type Discovery Surface

### 4.1 Catalog Source of Truth: `TypeMeta.ClrType`

```csharp
public record TypeMeta(
    TypeKind Kind,
    TokenMeta? Token,
    string Description,
    TypeCategory Category,
    string DisplayName,
    Type ClrType,            // ← the CLR projection for this Precept type
    // ... existing fields
);
```

`TypeMeta.ClrType` carries the **scalar** CLR type. For `TypeKind.Integer`, it's `typeof(long)`. For `TypeKind.Date`, it's `typeof(DateOnly)`. Collection wrapping (`IReadOnlyList<T>`) is done at descriptor-build time, not on `TypeMeta`.

### 4.2 Precomputed on Descriptors

Consumers discover the valid `T` directly from the descriptor they already hold:

```csharp
FieldDescriptor field = precept.Fields[0];
Type clrType = field.ClrType;  // e.g., typeof(decimal) or typeof(IReadOnlyList<long>)
```

```csharp
ArgDescriptor arg = version.RequiredArgs("submit")[0];
Type clrType = arg.ClrType;  // e.g., typeof(string)
```

**Collection descriptor-build cases:** For collection-typed fields, `FieldDescriptor.ClrType` encodes the full constructed generic type. The Builder handles three cases:

1. **Single-type collection** → `typeof(IReadOnlyList<>).MakeGenericType(elementClrType)`
2. **Keyed collection** → `typeof(IReadOnlyList<>).MakeGenericType(typeof(KeyedElement<,>).MakeGenericType(elementClrType, keyClrType))`
3. **Lookup** → `typeof(IReadOnlyDictionary<,>).MakeGenericType(keyClrType, valueClrType)`

No secondary lookup needed. The Precept Builder resolves this once during `Precept.From()`.

### 4.3 MCP/Tooling Surface

The `precept_compile` tool output includes `clrType` on field and arg entries:

```json
{
  "fields": [
    { "name": "amount", "type": "decimal", "clrType": "System.Decimal" }
  ],
  "events": [
    {
      "name": "submit",
      "args": [
        { "name": "approver", "type": "string", "clrType": "System.String" }
      ]
    }
  ]
}
```

The `precept_language` tool includes CLR projections in its type listing for AI consumers.

### 4.4 Discovery Summary

| Consumer | How they discover the CLR type |
|----------|-------------------------------|
| .NET developer (design-time) | `FieldDescriptor.ClrType` / `ArgDescriptor.ClrType` |
| .NET developer (from Version) | `version.FieldAccess[i].Field.ClrType` |
| AI agent (MCP) | `clrType` string in `precept_compile` output |
| IDE (completions) | `FieldDescriptor.ClrType` via language server |
| Reflection/codegen | `TypeMeta.ClrType` from the Types catalog |

---

## §5 Consistency Audit

### 5.1 Every "Value" on the Public Surface

| Location | Type | Lane |
|----------|------|------|
| `Version[fieldName]` | `JsonElement` | Raw |
| `Version.Get<T>(fieldName)` | `T` | Typed |
| `Version.ToJson()` | `JsonElement` (Object) | Raw |
| `FiredArgs[name]` | `JsonElement` | Raw |
| `FiredArgs.Get<T>(name)` | `T` | Typed |
| `FieldAccessInfo.CurrentValue` | `JsonElement` | Raw |
| `FieldSnapshot.Value` | `JsonElement?` | Raw |
| `ConstraintViolation.FailingValue` | `JsonElement?` | Raw |

✅ No `PreceptValue` anywhere. ✅ No `object?` anywhere.

### 5.2 Every Input Parameter Carrying Field/Arg Data

| Location | Type | Notes |
|----------|------|-------|
| `Create(args)` | `JsonElement?` or `Action<IArgBuilder>?` | JSON object or builder |
| `Fire(event, args)` | `JsonElement?` or `Action<IArgBuilder>?` | JSON object or builder |
| `Update(fields)` | `JsonElement?` or `Action<IFieldBuilder>?` | JSON object or builder |
| `FromJson(document)` | `JsonElement` | JSON only — no typed overload |
| `InspectCreate(args)` | `JsonElement?` or `Action<IArgBuilder>?` | Mirrors Create |
| `InspectFire(event, args)` | `JsonElement?` or `Action<IArgBuilder>?` | Mirrors Fire |
| `InspectUpdate(fields)` | `JsonElement?` or `Action<IFieldBuilder>?` | Mirrors Update |
| `IArgBuilder.Set<T>(name, value)` | `T` | Resolved through internal `TypeRuntime<T>` |
| `IFieldBuilder.Set<T>(name, value)` | `T` | Resolved through internal `TypeRuntime<T>` |

✅ All JSON-lane inputs are `JsonElement` (or `JsonElement?` when optional). No `JsonDocument`, no `string`. Callers parse upstream: `JsonDocument.Parse(str).RootElement`.

### 5.3 Null Handling

| Scenario | Behavior |
|----------|----------|
| `JsonElement` with `ValueKind == Null` on input | Valid — represents a null/absent value for nullable fields or optional args. Per-type `ReadJson` handles null semantics. |
| `JsonElement?` parameter is C# `null` | Means "no input provided" (e.g., no args, no patch). Distinct from a JSON null value. |
| `FieldSnapshot.Value` is `null` | Means `IsResolved == false` — field has no meaningful value yet (unresolved computed). |
| `ConstraintViolation.FailingValue` is `null` | Means no single attributable failing value (multi-field constraint). |
| `Version[fieldName]` for an omitted field | Throws `InvalidOperationException` — omitted fields are structurally absent. |
| `FiredArgs[name]` for an absent optional arg | Throws `KeyNotFoundException` — use `TryGet<T>()`. |

### 5.4 JSON Lane / Typed Lane Symmetry

Both lanes cover exactly the same operations:
- ✅ `Create` — both lanes
- ✅ `Fire` — both lanes
- ✅ `Update` — both lanes
- ✅ `InspectCreate` — both lanes
- ✅ `InspectFire` — both lanes
- ✅ `InspectUpdate` — both lanes
- ✅ `FromJson` — JSON only (deliberate asymmetry — documented reason: storage always provides serialized data; returns `Version` directly, not a DU)

**Output symmetry:** Every value on every result type is available as both `JsonElement` (directly on the record) and `T` (via `Get<T>()` on the `Version`/`FiredArgs` container). No asymmetry.

**Output-only JSON method — `Version.ToJson()`:** `ToJson()` is output-only in the JSON lane (returns the full persistence envelope as a single `JsonElement`). There is no typed bulk equivalent — field types are heterogeneous, so a single `T` does not apply. For typed bulk access, iterate `Version.FieldAccess` and call `Get<T>(fieldName)` per field using `FieldAccessInfo.Field.ClrType`.

---

## §6 What Stays Internal

| Type/Surface | Visibility | Reason |
|---|---|---|
| `PreceptValue` | `internal` | Evaluation currency; representation must be freely changeable. **`PreceptValue` MUST NOT appear in any public method signature, return type, property type, or generic constraint.** The collection type mapping exists specifically to prevent this leakage through generic type parameters. |
| `TypeRuntime<T>` | `internal` | Full internal conversion record (CLR ↔ PreceptValue ↔ JSON) |
| `TypeRuntimeMeta` | `internal` | Catalog-owned hot-path delegates (ReadJson, WriteJson, executors) |
| `TypeMapping<T>` | **Does not exist** | Registration surface eliminated — type system is closed |
| `PreceptRuntime.Register<T>()` | **Does not exist** | No registration entry point needed for a closed type system |
| `PreceptValue.FromScalar(...)` etc. | `internal` | Factory methods are internal machinery |
| `TypeRuntimeTable` (or equivalent) | `internal` | Lookup keyed by `typeof(T)` — static init, sealed |
| Slot arrays (`PreceptValue[]`) | `internal` | Entity representation invisible to callers |
| Presence mask (`bool[]`) | `internal` | Builder internals |
| `FiredArgs` internal slot storage | `internal` | External view is `JsonElement` / `Get<T>()` only |
| `BinaryExecutorDelegate` / `UnaryExecutorDelegate` | `internal` | Evaluator dispatch delegates |
| `PreceptList<T>` | `internal` | Lazy adapter — callers see only `IReadOnlyList<T>` |
| `PreceptLookup<TKey, TValue>` | `internal` | Lazy adapter — callers see only `IReadOnlyDictionary<TKey, TValue>` |

---

## §7 Breaking Change Assessment

### 7.1 Signature Changes (Breaking)

| Change | Current Signature | New Signature | Impact |
|--------|-------------------|---------------|--------|
| `Version` indexer return type | `PreceptValue this[string]` | `JsonElement this[string]` | **Breaking** — all callers reading `version["field"]` get a different type |
| `FiredArgs` indexer return type | `PreceptValue this[string]` | `JsonElement this[string]` | **Breaking** — all callers reading args via indexer |
| `FieldAccessInfo.CurrentValue` type | `PreceptValue` | `JsonElement` | **Breaking** — record property type change |
| `ConstraintViolation.FailingValue` type | `PreceptValue?` | `JsonElement?` | **Breaking** — record property type change |
| `FieldSnapshot.Value` type | `object?` (source) | `JsonElement?` | **Breaking** — property type change |
| `FieldSnapshot` gains `ClrType` | N/A | `Type ClrType` | **Breaking** — new required constructor parameter for positional record |
| `FieldDescriptor` gains `ClrType` | N/A | `Type ClrType` | **Breaking** — new required parameter |
| `ArgDescriptor` gains `ClrType` | N/A | `Type ClrType` | **Breaking** — new required parameter |

### 7.2 Removals (Breaking)

| Removal | Impact |
|---------|--------|
| `TypeRuntime<T>` → internal | **Breaking** — any external code referencing this type won't compile |
| `TypeRuntimeMeta` → internal | **Breaking** — same |
| `PreceptRuntime.Register<T>()` → removed | **Breaking** — any code calling Register |
| `TypeMapping<T>` → never existed (but if from prior design: removed) | **Breaking** if interim version shipped it |
| `PreceptValue` → internal | **Breaking** — any code referencing the struct type |
| All `PreceptValue` conversion methods | **Breaking** — `ToClr<T>()`, `ToJson()`, `AsDecimal()`, etc. |

### 7.3 Additions (Non-Breaking)

| Addition | Notes |
|----------|-------|
| `FiredArgs.TryGet<T>(string, out T)` | New method — additive |
| `TypeMeta.ClrType` | New property on existing record — additive if using `with` expressions; breaking if positional construction is used externally |
| `clrType` in MCP output | Additive JSON field — non-breaking for consumers |

### 7.4 Net Assessment

This is a **coordinated breaking change** across the entire public value surface. It should ship as a single atomic version (not incremental deprecation) because:
1. The current runtime is stub-only — no external consumers exist yet.
2. The change is total — every `PreceptValue` leakage point changes simultaneously.
3. Incremental migration would require a compatibility layer that serves no one.

**Pre-1.0 prerogative:** The runtime has no published stable release. These are design-time breaking changes, not production breaks.

---

## §8 Full `Precept` Class Surface

```csharp
public sealed class Precept
{
    // Construction
    public static Precept From(Compilation compilation);

    // Entity creation — JSON lane
    public EventOutcome Create(JsonElement? args = null);
    public EventInspection InspectCreate(JsonElement? args = null);

    // Entity creation — typed lane
    public EventOutcome Create(Action<IArgBuilder>? args = null);
    public EventInspection InspectCreate(Action<IArgBuilder>? args = null);

    // Entity restoration — JSON only
    public Version FromJson(JsonElement document);

    // Definition-level queries
    public IReadOnlyList<string> States { get; }
    public IReadOnlyList<FieldDescriptor> Fields { get; }
    public IReadOnlyList<EventDescriptor> Events { get; }
    public string? InitialState { get; }
    public string? InitialEvent { get; }
    public bool IsStateless { get; }

    // Constraint catalog (Tier 1)
    public IReadOnlyList<ConstraintDescriptor> Constraints { get; }
}
```

**Change from current:** `Fields` returns `IReadOnlyList<FieldDescriptor>` (was `IReadOnlyList<string>`). `Events` returns `IReadOnlyList<EventDescriptor>` (was `IReadOnlyList<string>`). This gives callers typed descriptors with `ClrType` immediately — no secondary lookup. `States` remains `IReadOnlyList<string>` (state names don't carry type metadata).

---

## §9 Full `Version` Class Surface

```csharp
public sealed record Version
{
    // Identity
    public Precept Precept { get; }
    public string? State { get; }

    // Field access — raw lane
    public JsonElement this[string fieldName] { get; }

    // Field access — typed lane
    public T Get<T>(string fieldName);

    // Field metadata
    public IReadOnlyList<FieldAccessInfo> FieldAccess { get; }

    // Structural queries
    public IReadOnlyList<EventDescriptor> AvailableEvents { get; }
    public IReadOnlyList<ArgDescriptor> RequiredArgs(string eventName);

    // Applicable constraints (Tier 2)
    public IReadOnlyList<ConstraintDescriptor> ApplicableConstraints { get; }

    // Commit — JSON lane
    public EventOutcome Fire(string eventName, JsonElement? args = null);
    public UpdateOutcome Update(JsonElement? fields = null);

    // Commit — typed lane
    public EventOutcome Fire(string eventName, Action<IArgBuilder>? args = null);
    public UpdateOutcome Update(Action<IFieldBuilder>? fields = null);

    // Inspect — JSON lane
    public EventInspection InspectFire(string eventName, JsonElement? args = null);
    public UpdateInspection InspectUpdate(JsonElement? fields = null);

    // Inspect — typed lane
    public EventInspection InspectFire(string eventName, Action<IArgBuilder>? args = null);
    public UpdateInspection InspectUpdate(Action<IFieldBuilder>? fields = null);

    // Persistence
    public JsonElement ToJson();
}
```

**Changes from current:**
- Indexer returns `JsonElement` (was `PreceptValue`).
- `AvailableEvents` returns `IReadOnlyList<EventDescriptor>` (was `IReadOnlyList<string>`).
- `RequiredArgs` returns `IReadOnlyList<ArgDescriptor>` (enriched shape with `ClrType`).

---

## §10 Ingress Types

### `IArgBuilder`

```csharp
public interface IArgBuilder
{
    IArgBuilder Set<T>(string name, T value);
}
```

Resolution: internal `TypeRuntime<T>` lookup. Invalid `T` → `InvalidOperationException`. Mismatch between `T` and the arg's declared Precept type → `InvalidOperationException`.

### `IFieldBuilder`

```csharp
public interface IFieldBuilder
{
    IFieldBuilder Set<T>(string name, T value);
}
```

Same resolution semantics. Invalid `T` or field type mismatch → `InvalidOperationException`.

---

## §11 Design Decisions Made in This Spec

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | `TryGet<T>()` on `FiredArgs` only, not on `Version` | Fields are either resolved or omitted; there is no "maybe present" semantic. Optional args have presence/absence semantics that warrant Try. |
| 2 | `Precept.Fields` → `IReadOnlyList<FieldDescriptor>` | Exposing typed descriptors with `ClrType` eliminates the need for a secondary lookup. The string list was a placeholder. |
| 3 | `Precept.Events` → `IReadOnlyList<EventDescriptor>` | Same reasoning — typed descriptors carry richer metadata. |
| 4 | `Version.AvailableEvents` → `IReadOnlyList<EventDescriptor>` | Consistent with `Precept.Events`. |
| 5 | `FieldSnapshot` includes `ClrType` | Inspection results should carry discovery metadata so consumers don't need a cross-reference to the definition. |
| 6 | `FiredArgs` indexer throws `KeyNotFoundException` for absent optional args | Consistent with dictionary semantics. `TryGet<T>()` is the presence-aware path. |
| 7 | No `Get<T>()` on `FieldSnapshot` or `FieldAccessInfo` | These are passive records, not active accessors. Typed access lives on `Version` (which holds the internal slots). |
| 8 | `Version.ToJson()` omits unresolved fields from `fields` | Consistent with `FromJson` round-trip semantics — `precept.FromJson(version.ToJson())` is valid and idempotent. Absent means "not provided," matching how `FromJson` interprets a missing property under `fields`. Returning `null` for unresolved fields would introduce ambiguity with legitimately null nullable fields. |
| 9 | `FromJson` returns `Version` directly, not `RestoreOutcome` | FromJson is hydration from storage — data was valid when saved, so restoration is not a failable business operation. Invalid inputs (unknown state, malformed JSON, unknown fields) are programmer errors → `ArgumentException`. Constraints are NOT run on restore — they fire only on `Fire`/`Update`. Schema evolution is handled by the caller via migration or post-restore `Update`, not by the restore path itself. Eliminates `RestoreOutcome` DU entirely. |
| 10 | Persistence API named `ToJson()`/`FromJson()` (not `Serialize`/`Restore`, `Snapshot`/`Restore`, or `ToDocument`/`FromDocument`) | Owner decision (Shane, 2026-05-04). Most expressive and self-describing pair for the public API. `PreceptValue.ToJson()` and `TypeRuntime` delegates (`FromJson`/`ToJson`) are internal — no public API collision. `ToJson()` returning `JsonElement` (not `string`) is intentional and documented. Alternatives rejected: `Serialize`/`Restore` (slightly off-register for instance methods in .NET), `Snapshot`/`Restore` (domain language but less immediately obvious to new consumers), `ToDocument`/`FromDocument` (avoids string-implies issue but less immediately legible). Tradeoff accepted: `ToJson()` returning `JsonElement` rather than `string` may mildly surprise developers — mitigated by clear XML docs on the method. |
| 11 | All DU variants are nested inside their abstract record base (`EventOutcome.Transitioned`, `EventOutcome.ConstraintsFailed`, `UpdateOutcome.ConstraintsFailed`, etc.) | Nesting all variants inside the abstract record body resolves the `ConstraintsFailed` name collision without namespace pollution, and creates a uniform, discoverable API surface. In a switch expression on `EventOutcome`, every arm reads `EventOutcome.Variant` — the DU base name is consistent visible context. Partial nesting (some variants nested, others top-level) was rejected: mixing nesting levels in the same DU is inconsistent and adds cognitive load. Option A (single shared `ConstraintsFailed : EventOutcome, UpdateOutcome`) requires converting bases to interfaces, which opens the hierarchy to external implementations; Precept's deterministic, catalog-driven design requires closed hierarchies. Option B (operation-verb prefix names — `FireConstraintsFailed`, `UpdateConstraintsFailed`) was rejected as defeating the purpose of the rename. Tradeoff accepted: fully-qualified names (`EventOutcome.ConstraintsFailed`) are slightly more verbose in documentation; at call sites inside a switch expression on `EventOutcome` the qualification is natural and aids readability. |
| 12 | `EventConstraintsFailed` renamed to `EventOutcome.ConstraintsFailed`; `UpdateConstraintsFailed` renamed to `UpdateOutcome.ConstraintsFailed` | The `Event`/`Update` prefix was a domain-scope qualifier that misled callers into believing only event-scoped constructs (e.g., `ensures` on an event) can fail through `Fire`, or only field-level `rule` declarations through `Update`. In reality, constraint failures surface from any combination of `rule` declarations, `ensures` scoped to events, and `ensures` scoped to states — across both operations. The correct umbrella is `ConstraintsFailed`. The containing DU (`EventOutcome` vs. `UpdateOutcome`) already identifies which operation produced the result; no operation-type prefix is needed on the variant itself. |
| 13 | `UpdateOutcome.AccessDenied` renamed to `UpdateOutcome.FieldNotEditable` | `AccessDenied` carried a security/RBAC connotation that actively misled callers. This variant fires when a field's *declared access mode* (`Readonly` vs `Editable`) prevents direct editing — a structural property of the field declaration, not an authorization decision. `FieldNotEditable` is precise, structurally accurate, and anchored to the `Editable` concept already present in `FieldAccessMode`. Surfaced by Elaine (API naming assessment, 2026-05-04). |
| 14 | `RowInspection` renamed to `TransitionInspection`; `EventInspection.Rows` renamed to `EventInspection.Transitions` | "Row" is Precept-internal vocabulary for a guard/mutation pair in an event handler. External developers read it as a database row, table row, or layout row — none of which apply. The concept being inspected is a *transition* — one guard branch within an event — which is immediately legible to any developer familiar with state machines. `TransitionInspection` is philosophy-aligned and vocabulary-consistent. The `Transitions` property name follows the type rename. Surfaced by Elaine (API naming assessment, 2026-05-04). |
| 15 | `ConstraintViolation.BecauseClause` renamed to `ConstraintViolation.Because` | `ConstraintDescriptor.Because` already uses the DSL keyword form. `ConstraintViolation.BecauseClause` used a different name for functionally the same concept — the reason text. These two types are tightly paired in every usage context; inconsistent names for the same concept add unnecessary cognitive load. Normalized to `Because` on both types. Surfaced by Elaine (API naming assessment, 2026-05-04). |
| 16 | `UpdateOutcome.InvalidInput` renamed to `UpdateOutcome.InvalidFields` | `Update()` takes a `fields` parameter, not `args`. `InvalidInput` was a generic name that didn't reflect the Update operation's own vocabulary. The rename creates a clean parallel: `InvalidArgs` for operations that take args (Create, Fire); `InvalidFields` for Update, which takes fields. Surfaced by Elaine (API naming assessment, 2026-05-04). |
| 17 | `UpdateOutcome.FieldWriteCommitted` renamed to `UpdateOutcome.Updated` | `FieldWriteCommitted` was a three-word verbose log-entry-style name that broke the naming register of the other success variants (`Transitioned`, `Applied` — short, past-tense, entity-centric). `Updated` is concise, consistent with the register, and reads cleanly in switch expressions: `outcome is UpdateOutcome.Updated u`. Surfaced by Elaine (API naming assessment, 2026-05-04). |

---

## §12 Open Questions for Owner

These decisions are required before implementation begins. Each is flagged as TBD in the spec above.

### ~~OQ-1~~: `integer` → `int` vs `long` ✅ RESOLVED

**Decision:** `long` is the canonical CLR projection. NO convenience `Get<int>()` overload. Callers cast themselves.  
**Rationale:** Eliminates the overflow-check surface. A single canonical type is simpler and avoids the "which integer accessor do I use?" ambiguity. Callers who need `int` write `(int)version.Get<long>("field")` and accept the truncation risk explicitly.

### ~~OQ-2~~: Collection CLR type wrapping strategy ✅ RESOLVED

**Decision:** `TypeMeta.ClrType` returns the scalar CLR type only. `FieldDescriptor`/`ArgDescriptor` applies `IReadOnlyList<T>` wrapping at Precept Builder time.  
**Rationale:** Keeps `TypeMeta` entries 1:1 with Precept scalar types. The Builder has full type-parameter context from the DSL declaration and produces the final `Type` (e.g., `typeof(IReadOnlyList<long>)`) on the descriptor. No combinatorial explosion of generic instantiations in the catalog.

### ~~OQ-3~~: Compound types (`money`, `quantity`) — CLR projection ✅ RESOLVED

Resolved across 7 sub-questions:

| Sub-Q | Decision |
|-------|----------|
| **OQ-3a** | UCUM codes are canonical unit identity |
| **OQ-3b** | Full UCUM grammar accepted; discovery is tiered — Tier 1 (~150 atoms) surfaced proactively, full grammar always valid |
| **OQ-3c** | Pure database-backed — unit metadata is an embedded resource; NO static `Units.X` members in the library |
| **OQ-3d** | Currency is separate from the unit system. `Money.Currency` is `string` (ISO 4217). Not a unit. |
| **OQ-3e** | `Quantity` = `readonly record struct(decimal Amount, Unit Unit)`; `Money` = `readonly record struct(decimal Amount, string Currency)` |
| **OQ-3f** | DSL constraint granularity follows `docs/language/business-domain-types.md` — three levels: `quantity` (any), `quantity of 'length'` (dimension-constrained), `quantity in 'kg'` (unit-constrained). CLR mapping: `QuantityFieldDescriptor` carries `Unit? ConstrainedUnit` and `Dimension? ConstrainedDimension`. |
| **OQ-3g** | UCUM data shipped as embedded resource in Precept NuGet package; updated with library releases |

**Rationale:** See `docs/working/precept-value-types-investigation.md` for the full analysis. UCUM provides machine-parseable, interoperable unit identity. Database-backed architecture aligns with Precept's catalog-driven design. Separating money from units avoids dimensional confusion (currency is not a physical quantity).

### ~~OQ-7~~: Business value types coverage ✅ RESOLVED (cross-referenced from existing docs)

All sub-questions resolved by cross-reference to `docs/language/business-domain-types.md` and `docs/language/temporal-type-system.md`:

| Sub-Q | Resolution |
|-------|-----------|
| ~~**OQ-7a**~~ | **Resolved.** `Rate` is `decimal`. D12 locks `decimal` for all seven business-domain types — no `double` in the business chain. |
| ~~**OQ-7b**~~ | **Resolved.** Temporal validity stays at field/rules layer. The `exchangerate` type carries no validity timestamp — the doc explicitly notes ordering has "no meaning outside their time context," confirming that context belongs to the field. |
| ~~**OQ-7c**~~ | **Deferred — separate investigation.** Business-domain-types.md Exclusions section explicitly states: "Whether `percent` is a type or syntactic sugar for `number / 100` is a separate investigation." Out of scope for this spec. |
| ~~**OQ-7d**~~ | **Resolved — not a new type.** `DateRange` is defined in `docs/language/temporal-type-system.md`. No new CLR type needed. |
| ~~**OQ-7e**~~ | **Resolved — renamed.** `docs/working/precept-value-types-investigation.md` |
| ~~**OQ-7f**~~ | **Resolved.** Type list is complete per both design docs. `Percentage` is a deferred separate investigation, not a gap. |

### ~~OQ-4~~: `duration` CLR type ✅ RESOLVED

**Decision:** NodaTime `Duration` directly. No wrapper. Same pattern as all other temporal types.  
**Rationale:** NodaTime is already an accepted dependency for temporal types. `Duration` handles ISO 8601 duration semantics correctly (unlike `TimeSpan` which conflates elapsed time with calendar periods). Consistency with other temporal types (all NodaTime) outweighs the zero-dependency argument.

### ~~OQ-5~~: Collection CLR type shapes ✅ RESOLVED

**Decision:** Three CLR shapes for 9 collection types. Option A (lazy adapter) locked.  
**Source:** `docs/working/precept-collection-types-investigation.md`

| Shape | DSL Types | CLR Public Type |
|---|---|---|
| Single-type | `set`, `queue`, `stack`, `bag`, `list`, `log` | `IReadOnlyList<TElement>` |
| Keyed | `log by`, `queue by` | `IReadOnlyList<KeyedElement<TValue, TKey>>` |
| Map | `lookup` | `IReadOnlyDictionary<TKey, TValue>` |

One new public type: `KeyedElement<TValue, TKey>` (`readonly record struct`). Two internal adapters: `PreceptList<T>`, `PreceptLookup<K, V>`. Remaining sub-questions (OQ-C1, OQ-C2, OQ-C3) documented in §13.7.

---

## §13 Collection Surface Design (~~OQ-5~~ ✅ RESOLVED)

### 13.1 The Problem

Internally, collection fields store `PreceptValue[]` in slots. The public API must return `IReadOnlyList<T>` (per OQ-2 decision). But if the backing array is `PreceptValue[]`, a naive implementation would either:

- **Materialize a new `T[]` on every access** — allocation cost, GC pressure, O(n) per read
- **Return a lazy adapter** that wraps `PreceptValue[]` and projects on index access — zero allocation, no materialization, O(1) per read

Neither can expose `PreceptValue` to callers. The design question is: what's the right internal structure?

### 13.2 Options

#### Option A — Eager-on-First-Read Adapter

An internal `PreceptList<T> : IReadOnlyList<T>` materializes a `T[]` from the internal `PreceptValue[]` **on first access** (eager-on-first-read), then serves all subsequent reads from the materialized array.

```csharp
// Internal — callers only see IReadOnlyList<T>
internal sealed class PreceptList<T> : IReadOnlyList<T>
{
    private readonly PreceptValue[] _slots;
    private readonly Func<PreceptValue, T> _project;
    private T[]? _materialized;

    public T this[int index] => Materialized[index];
    public int Count => _slots.Length;

    private T[] Materialized => _materialized ??= Materialize();
    private T[] Materialize()
    {
        var result = new T[_slots.Length];
        for (int i = 0; i < _slots.Length; i++)
            result[i] = _project(_slots[i]);
        return result;
    }
    // IEnumerable<T> implementation via materialized array
}
```

**Pros:**
- Projection cost paid once — O(n) on first read, O(1) indexing thereafter
- At Precept's scale (typically ≤100 elements), materialization is sub-microsecond
- Immutable by construction — no `T[]` reference escapes; materialized snapshot is stable
- Lazy at the Version level (adapter constructed on first field read), not element-level

**Cons:**
- O(n) allocation on first access (trivial at typical collection sizes)
- Materialized `T[]` retained for adapter lifetime — minor memory cost
- LINQ `.ToList()` by callers still allocates — but that's their choice

#### Option B — Materialized Copy

At public API access time, eagerly project all values to `T[]` and return a `ReadOnlyCollection<T>` (or bare array via `IReadOnlyList<T>`).

```csharp
// At access time
T[] materialized = new T[slots.Length];
for (int i = 0; i < slots.Length; i++)
    materialized[i] = project(slots[i]);
return materialized; // IReadOnlyList<T> via array covariance
```

**Pros:**
- Simple implementation — no custom collection type
- Projection cost paid once (predictable)
- No ongoing coupling to internal `PreceptValue[]` — snapshot semantics

**Cons:**
- Allocation on every access (unless cached)
- If cached, adds a cache-invalidation concern when the field is mutated internally
- Array returned via `IReadOnlyList<T>` can be downcast to `T[]` and mutated — requires `ReadOnlyCollection<T>` wrapping or `Array.AsReadOnly()`

#### Option C — Typed Internal Storage (No PreceptValue for Collections)

Collections in the runtime store `T[]` typed to the field's CLR type directly. No `PreceptValue[]` intermediate for collection elements. The runtime knows `T` at field construction time (from `FieldDescriptor.ClrType`).

```csharp
// Internal slot for a List<integer> field stores long[] directly
internal object CollectionSlot; // boxed T[] — cast at access time
// OR a typed slot structure that avoids boxing
```

**Pros:**
- Zero projection overhead — the array IS the public value (wrapped in ReadOnlyCollection)
- Simplest public API path — return `Array.AsReadOnly(typedArray)`
- No custom collection type needed

**Cons:**
- Requires the evaluator to project `PreceptValue → T` at write time (during event fire), not at read time
- The internal slot can't be uniform `PreceptValue[]` — complicates the evaluator's slot-access model
- Evaluator operations (constraint checking, rule evaluation) must work with typed values or convert back to `PreceptValue` for expression evaluation
- Breaks symmetry with scalar fields (which store `PreceptValue` and project on read)

### 13.3 Evaluation Against Constraints

| Constraint | Option A (Eager-on-First-Read) | Option B (Per-Access Materialization) | Option C (Typed Storage) |
|---|---|---|---|
| PreceptValue not in public API | ✅ Hidden behind `IReadOnlyList<T>` | ✅ Fully projected | ✅ Never created for collections |
| Thin implementation | ✅ ~40 lines | ⚠️ Thin but needs per-access allocation or separate caching | ❌ Requires evaluator refactoring |
| Consistent with scalar access | ✅ Same internal PreceptValue storage — materialization is the projection step | ⚠️ Different timing — project on every access | ❌ Breaks uniformity |
| Immutability | ✅ Structurally immutable — materialized snapshot is stable | ⚠️ Requires wrapping (not bare array) | ✅ `ReadOnlyCollection<T>` wrapping |

### 13.4 Frank's Recommendation: Option A — Eager-on-First-Read Adapter

**Option A is the clear winner.** Rationale:

1. **Consistency:** Scalar fields project from `PreceptValue` on `Get<T>()` access. Collection fields materialize from `PreceptValue[]` to `T[]` on first access — same internal storage, same projection direction, one-time cost.

2. **Thinness:** `PreceptList<T>` is approximately 40 lines of straightforward code. No per-access allocation, no evaluator changes, no slot-model refactoring.

3. **O(n) once, O(1) thereafter:** The adapter materializes the full `T[]` on first read. At Precept's scale (typically ≤100 elements), this is sub-microsecond. All subsequent index accesses hit the materialized array directly — no projection, no GC pressure.

4. **Immutability:** `IReadOnlyList<T>` has no mutation methods. The internal `PreceptValue[]` is owned by the runtime and never exposed. The materialized `T[]` is a stable snapshot. Callers cannot mutate.

5. **No evaluator changes:** The evaluator continues to work uniformly with `PreceptValue[]` slots for all fields (scalar and collection). Collection fields simply have their slot hold an array of `PreceptValue` rather than a single `PreceptValue`. The adapter handles materialization transparently.

6. **Compound type friendliness:** For `Quantity` or `Money` elements, eager materialization pays the projection cost once on first read rather than on every index access. Callers who access multiple elements (iteration, LINQ) pay the same total cost as per-index projection but with better cache locality.

### 13.5 ~~Shane's Remaining Decision~~ ✅ RESOLVED

**Resolved.** Option A (eager-on-first-read adapter) is locked. The full investigation (`docs/working/precept-collection-types-investigation.md`) confirmed eager-on-first-read `T[]` materialization (§15 correction), the three CLR shapes, the `KeyedElement<TValue, TKey>` public type, and the `PreceptLookup<K, V>` internal adapter. No owner call required.

### 13.6 Internal Adapter Inventory (Locked)

| Internal Type | Public Surface | Covers |
|---|---|---|
| `PreceptList<T> : IReadOnlyList<T>` | `IReadOnlyList<TElement>` | `set`, `queue`, `stack`, `bag`, `list`, `log` |
| `PreceptList<KeyedElement<T, P>> : IReadOnlyList<KeyedElement<T, P>>` | `IReadOnlyList<KeyedElement<TValue, TKey>>` | `log by`, `queue by` |
| `PreceptLookup<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>` | `IReadOnlyDictionary<TKey, TValue>` | `lookup` |

All three adapters wrap internal `PreceptValue` storage and materialize to typed arrays/dictionaries on first access (eager-on-first-read). Callers never see the adapter type names — they interact through the standard BCL interfaces only.

### 13.7 Remaining Decisions — All Locked (2026-05-04)

| OQ | Decision | Status |
|---|---|---|
| **OQ-C1** | `bag of T`: `IReadOnlyList<T>` with duplicates. LINQ `GroupBy` for frequency. No special bag-frequency CLR type. | ✅ LOCKED |
| **OQ-C2** | `KeyedElement<TValue, TKey>` confirmed. `readonly record struct KeyedElement<TValue, TKey>(TValue Value, TKey Key)`. | ✅ LOCKED |
| **OQ-C3** | Store in **declared direction**. `Enqueue`/`LogByAppend` take direction param and insert in correct sorted position. `arr[0]` is always "front" in declared order. `Peek`, `Dequeue`, log iteration are direction-naive. `PreceptPairList<TValue, TKey>` returns `arr[0]` as index 0 — no adapter flip. JSON serializer iterates forward — no inversion. `FieldDescriptor.SortDirection` is informational metadata only. | ✅ LOCKED |

**OQ-C3 rationale:** Direction is "compiled in" at write time by `Enqueue`/`LogByAppend`. One place owns direction (insertion); all reads are direction-naive. Alternative (always store ascending, flip in CLR adapter) rejected — JSON serializer also needed a flip, splitting ownership across two places.

Source: `docs/working/precept-collection-types-investigation.md` §9. Decision record: `frank-oq-c3-direction-resolved.md`.

---

*End of specification. This document is the authoritative reference for the public API redesign. It drives `docs/runtime/runtime-api.md` updates after owner sign-off.*
