# Precept Value Types — CLR Mapping Investigation

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04 (last synced 2026-05-05)  
**Status:** Investigation — all verdicts locked.  
**Context:** OQ-3 resolution (compound CLR types) from `runtime-api-public-surface-spec.md`

---


## 3. UCUM Scope Decision — Full vs. Subset

### The Scale Problem

UCUM has ~2,600 predefined unit atoms. With the compositional grammar, the expressible unit space is infinite (`mol/L`, `kg.m-2.s-1`, etc.).

### Option A: Full UCUM (All Atoms + Grammar)

**Pros:**
- Complete interoperability — any UCUM code from external systems is valid
- No "why isn't unit X supported?" friction
- Future-proof for domains Precept hasn't entered yet

**Cons:**
- Includes units irrelevant to business domains (radioactivity: `Bq`, `Ci`; optical: `lm`, `lx`)
- Discoverability suffers — 2,600 atoms in autocomplete is noise
- Validation of compositional expressions requires a UCUM parser at runtime

### Option B: Curated Subset (Business Domain Coverage)

**Pros:**
- Clean discoverability — users see relevant units
- Simpler validation
- Smaller metadata footprint

**Cons:**
- Subset boundary is arbitrary and will need ongoing expansion
- Interop friction when external systems send codes outside the subset
- Maintenance cost of curating "what's in"

### What Business Domains Actually Need

| Domain | Units Commonly Required |
|--------|------------------------|
| **Commerce** | Currency (separate), weight (kg, lb, oz, g), volume (L, gal, mL), dimensions (m, cm, in, ft) |
| **Logistics** | Weight, volume, distance (km, mi), area (m2, ft2), temperature (Cel, [degF]) |
| **Manufacturing** | Length, mass, force (N), pressure (Pa, psi), temperature, electrical (A, V, W, Ohm) |
| **Healthcare** | Clinical units ([IU], mg/dL, mmol/L, mL/min), dosage, body measurements |
| **Agriculture** | Area (ha, acre), volume, mass, concentration |

A practical business-domain subset is approximately **150–250 unit atoms** plus the compositional grammar for derived expressions.

### Frank's Recommendation: Hybrid — Full Grammar, Tiered Discovery

**Ship the full UCUM grammar parser** (so any valid UCUM expression is structurally acceptable), but **tier the unit database for discovery:**

1. **Tier 1 — Common (~150 atoms):** Surfaced in autocomplete, documentation, and builder APIs. Covers commerce, logistics, manufacturing, healthcare basics.
2. **Tier 2 — Extended (~500 atoms):** Valid and recognized, but not surfaced proactively. Includes scientific, specialized clinical, legacy customary.
3. **Tier 3 — Full UCUM (~2,600 atoms):** Valid if referenced by code. Never surfaced in discovery. Exists in the database for interop acceptance.
4. **Custom expressions:** Any grammatically valid UCUM composition (e.g., `kg.m/s2`) is accepted by the parser. No database entry required — the grammar is the validator.

This gives Precept:
- Zero interop friction (any UCUM code works)
- Clean UX (Tier 1 in autocomplete)
- No maintenance burden of rejecting valid codes

---

## 4. Database vs. Code — Representation Architecture

### The NodaTime Precedent

NodaTime architecture:

```
[Code types: Instant, LocalDate, ZonedDateTime, DateTimeZone]
         ↓ reads from
[Data file: tzdb.dat — IANA timezone database, versioned, updateable]
         ↓ loaded via
[Provider abstraction: IDateTimeZoneProvider]
```

The types are generic shells. The *data* (zone rules, transitions, offsets) is external and updateable without recompiling the library.

### Option A: Database-Backed (NodaTime Model)

```
[Code types: Unit, Dimension, Quantity — generic shells]
         ↓ reads from
[Data file: ucum.dat — UCUM unit database, versioned]
         ↓ loaded via
[Provider: IUnitProvider / catalog metadata]
```

**Implications:**
- ✅ **Updateability** — new UCUM versions ship as data file updates, not library releases
- ✅ **Catalog-driven fit** — the unit database IS catalog metadata; pipeline stages consume it generically
- ✅ **Runtime flexibility** — tier assignment, custom metadata per unit, all in the data layer
- ⚠️ **No compile-time unit identity** — `Unit` is structurally a string-identified value, not a typed constant *(C# API consumers only — the Precept type checker validates unit identifiers against the catalog at parse/check time; this gap does not affect DSL authors)*
- ⚠️ **Discoverability requires tooling** — IDE support must read the database to offer completions

### Option B: Code-Backed (Static Members)

```csharp
public static class Units
{
    public static readonly Unit Kilogram = new("kg", Dimension.Mass, ...);
    public static readonly Unit Meter = new("m", Dimension.Length, ...);
    // ... 150+ static fields
}
```

**Implications:**
- ✅ **Compile-time discoverability** — `Units.Kilogram` appears in IntelliSense
- ✅ **Object identity** — reference equality, pattern matching
- ⚠️ **Not updateable** — new units require library recompilation
- ⚠️ **Violates catalog-driven architecture** — domain knowledge is hardcoded in C# source, not metadata
- ⚠️ **Tier 2/3 bloat** — 2,600 static fields is untenable; forces subset commitment

### Option C: Hybrid — Well-Known Constants + Database

```csharp
// Well-known constants (Tier 1) for compile-time convenience
public static class Units
{
    public static Unit Kilogram => UnitDatabase.Get("kg");
    public static Unit Meter => UnitDatabase.Get("m");
    // ~50 most common as convenience accessors
}

// Full database for everything else
public sealed class UnitDatabase
{
    public Unit Get(string ucumCode);
    public IEnumerable<Unit> Browse(UnitTier tier);
    public bool IsValid(string ucumExpression); // grammar validation
}
```

**Implications:**
- ✅ Convenience for common cases
- ✅ Full UCUM coverage via database
- ⚠️ Two paths to the same unit — must ensure `Units.Kilogram == UnitDatabase.Get("kg")`
- ⚠️ Slight architectural impurity — well-known constants are syntactic sugar, not a parallel system

### Frank's Recommendation: Database-Backed (Option A), Pure

**Use the pure database model.** Reasoning:

1. **Catalog-driven architecture is non-negotiable.** Units are domain metadata. The catalog IS the database. Hardcoding units in C# source violates the fundamental architectural principle.

2. **NodaTime proves the pattern works.** `DateTimeZone.ForId("America/New_York")` is string-identified, database-resolved. Nobody complains about lacking `DateTimeZone.NewYork` as a static field. The tooling (IDE, language server) provides discoverability.

3. **Precept already has the tooling layer.** The language server provides completions from catalog metadata. The MCP server exposes discovery. Adding unit discovery is incremental, not novel.

4. **UCUM stability means updates are rare** (~years between revisions), but when they happen, a data file update is vastly simpler than a library release.

5. **Well-known constants are premature optimization.** If consumers want convenience, they can define their own `static Unit Kg => ...` in application code. The library shouldn't bless a subset.

The unit database is a catalog entry — shipped as embedded resource, loadable from external file, versioned, and exposed through the standard catalog discovery API.

---

## 5. The Unit Type Design (Precept-Built)

### Core Types

```csharp
/// A unit of measure identified by its UCUM code.
/// Evaluator-internal type — not part of the public API. Consumers receive `UnitOfMeasure` at the API boundary.
/// Value-semantics, immutable, interned from the unit database.
internal sealed class Unit : IEquatable<Unit>
{
    public string Code { get; }           // UCUM code: "kg", "m/s2", "[lb_av]"
    public string Name { get; }           // Human name: "kilogram", "meter per second squared"
    public Dimension Dimension { get; }   // Dimensional category
    public UnitTier Tier { get; }         // Discovery tier (1/2/3/derived)
    
    // Equality by UCUM code (canonical form)
    public bool Equals(Unit? other) => Code == other?.Code;
    public override int GetHashCode() => Code.GetHashCode();
}

/// Dimensional category for dimensional analysis — evaluator-internal.
/// Public API proxy is `MeasureDimension` (see §12).
/// A product of base dimension exponents.
internal readonly record struct Dimension(
    int Length,       // m
    int Mass,         // kg
    int Time,         // s
    int Current,      // A
    int Temperature,  // K
    int Amount,       // mol
    int Luminosity)   // cd
{
    public static readonly Dimension Dimensionless = default;
    public static readonly Dimension Length = new(1, 0, 0, 0, 0, 0, 0);
    public static readonly Dimension Mass = new(0, 1, 0, 0, 0, 0, 0);
    // ... etc.
    
    public bool IsCompatibleWith(Dimension other) => this == other;
}

internal enum UnitTier { Common, Extended, Full, Derived }
```

### Why `sealed class` Not `struct` or `record struct`

- **Interned identity** — Units are resolved from the database and interned. Two lookups of `"kg"` return the same object instance. This enables reference equality as a fast path.
- **Nullable** — `Unit?` represents "no unit" cleanly (dimensionless quantities in some contexts).
- **Extensible metadata** — future fields (symbol, print format, conversion factor to base) don't break binary layout.
- **Not `record`** — records imply structural equality with all fields. Unit equality is strictly by `Code`. Using `class` with explicit `IEquatable<Unit>` makes the contract clear.

> **API boundary note:** `Unit`, `Dimension`, and `UnitTier` are evaluator-internal types. They live in the `Precept` assembly and are never exposed at the public API surface. At the API boundary, consumers receive `UnitOfMeasure` (a `readonly record struct` carrying the validated UCUM code string) and `MeasureDimension` (a `readonly record struct` carrying the dimension name). The evaluator resolves `UnitOfMeasure.Code` → `Unit` via `UnitCatalog.Get(code)` internally. See §12 for the full dual-shape API boundary design.

### Quantity

```csharp
/// A measured quantity: an amount with its unit.
/// Lives in Precept.Types; uses UnitOfMeasure (the public proxy) not the evaluator-internal Unit.
public readonly record struct Quantity(decimal Amount, UnitOfMeasure Unit)
{
    // Display
    public override string ToString() => $"{Amount} {Unit.Code}";
}
```

- `record struct` — value semantics, cheap to copy, stack-allocable.
- `decimal` for Amount — matches Precept's `decimal` type semantics (exact representation, no floating-point surprise).
- `UnitOfMeasure` carries the validated UCUM code string. The evaluator resolves it to the internal `Unit` catalog entity via `UnitCatalog.Get(code)` when dimensional analysis or conversion metadata is needed.

### Money — Separate from Unit System

```csharp
/// A monetary amount with its currency.
public readonly record struct Money(decimal Amount, Currency Currency)
{
    // Currency wraps an ISO 4217 code: "USD", "EUR", "GBP"
    public override string ToString() => $"{Amount} {Currency.Code}";
}
```

**`Currency` is a first-class CLR type (§8), NOT part of the unit system.** Reasoning:

1. UCUM does not define currencies. Currency is not a unit of measure — it's a medium of exchange with no dimensional analysis.
2. ISO 4217 is the correct standard for currency codes. It's a flat code list, not a compositional grammar.
3. `Currency` is a `sealed class` backed by `CurrencyCatalog` (§8) — structurally parallel to `Unit`, but architecturally separate.
4. Conversion between currencies is fundamentally different from unit conversion — it requires temporal exchange rates, not fixed factors.

### Unit Database / Provider

```csharp
/// The unit catalog — loads and serves unit metadata.
public sealed class UnitCatalog
{
    // Singleton loaded from embedded UCUM data
    public static UnitCatalog Default { get; }
    
    // Core lookup
    public Unit Get(string ucumCode);
    public bool TryGet(string ucumCode, out Unit unit);
    
    // Grammar validation (accepts any valid UCUM expression)
    public bool IsValidExpression(string expression);
    
    // Parse a composed expression into a Unit (interned or synthesized)
    public Unit Parse(string ucumExpression);
    
    // Discovery
    public IEnumerable<Unit> Browse(UnitTier? tier = null, Dimension? dimension = null);
    
    // Metadata
    public string Version { get; } // UCUM version: "2.1"
}
```

### How It Fits the Catalog-Driven Architecture

The `UnitCatalog` is a runtime catalog — it follows the same pattern as `Tokens`, `Types`, `Constructs`, etc.:
- Metadata-driven: unit definitions are data, not code
- Pipeline stages consume generically: the type checker validates unit codes against the catalog; the evaluator resolves `Quantity` from it
- Tooling derives from it: language server completions, MCP `precept_language` output, TextMate grammar patterns for unit literals

### Public API Discovery Surface

```csharp
// On TypeMeta / FieldDescriptor level:
public sealed record QuantityFieldDescriptor(
    string FieldName,
    UnitOfMeasure? ConstrainedUnit,       // null = any unit; non-null = `quantity in 'kg'` form
    MeasureDimension? ConstrainedDimension); // null = any dimension; non-null = `quantity of 'mass'` form
```

In the DSL, a field declaration like `quantity in 'kg'` constrains to a specific unit; `quantity of 'mass'` constrains to a dimension; bare `quantity` allows any unit. See §6 for the full constraint-level rationale.

---

## 6. Quantity Field Constraint Levels

Quantity field declarations support three constraint levels:

- **`quantity`** — accepts any valid quantity value with any unit.
- **`quantity of 'mass'`** — constrains to a specific dimension; any unit within that dimension is valid (e.g., `kg`, `g`, `lb`). The type checker rejects assignments from incompatible dimensions at compile time.
- **`quantity in 'kg'`** — constrains to an exact unit. The runtime rejects quantity values with a different unit at the event-arg input boundary and at `set` time.

These three levels map directly onto `QuantityFieldDescriptor` (§5 — Public API Discovery Surface): `ConstrainedUnit` is non-null for the unit-constrained form; `ConstrainedDimension` is non-null for the dimension-constrained form; both null for bare `quantity`.

The three-level model is structurally correct for Precept. `quantity in 'kg'` communicates the invariant at declaration time — the type checker verifies compatible operations at compile time and the runtime enforces the unit boundary at data ingress. `quantity of 'mass'` serves the case where any mass-compatible unit is acceptable (a logistics field might accept `kg` or `lb`), which cannot be expressed with unit-only constraints. Bare `quantity` is valid for genuinely open measurement fields — sensor readings, generic integration points, fields whose unit is supplied at runtime.

`quantity of 'mass'` is not redundant with `quantity in 'kg'`: dimension constraints allow dimension-compatible substitution; unit constraints hard-lock to a single unit. Collapsing to two levels pushes dimension-level invariants into guards, discarding a structurally enforceable constraint. Collapsing to one level abandons structural enforcement entirely.

---

## Addendum: struct vs class Evaluation

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04  
**Trigger:** Shane's question on whether the value-holding type (`Quantity`) should be `sealed class`, `struct`, or `readonly record struct` given the expression evaluation hotpath.

---

### Types Under Scrutiny

There are two distinct types in the unit system design, and they require different answers:

| Type | Current design | Holds |
|------|---------------|-------|
| `Unit` | `sealed class` | UCUM code, name, dimension, tier — the *identity* of a unit |
| `Quantity` | `readonly record struct(decimal Amount, UnitOfMeasure Unit)` | A measured amount + its unit — the *value* of a quantity field |

Shane's question targets `Quantity`. The `Unit` sealed class is **not in question** — its sealed-class shape is correct for different reasons (interning, nullable-by-reference, extensible metadata) and is addressed at the end of this section.

---

### 1. Field Layout and Copy Cost

Before analyzing hotpath behavior, ground the discussion in concrete numbers.

```
Quantity layout (64-bit process):
  decimal Amount  →  16 bytes  (System.Decimal = 4 × int32 internally)
  Unit Unit       →   8 bytes  (managed reference)
  ─────────────────────────────
  Total           →  24 bytes
```

**24 bytes is the threshold question.** Microsoft's struct guidance targets ≤16 bytes as "trivially cheap." At 24 bytes, the analysis must go further:

- Copy cost: 3 × 8-byte register moves on x64 — a few nanoseconds in practice.
- Allocation cost (class equivalent): minimum ~40 bytes on heap (object header + method table pointer + 16-byte decimal + 8-byte reference) plus GC bookkeeping entry, write barrier on assignment, and eventual collection scan.
- **Verdict:** 24-byte copy is cheaper than one heap allocation by a wide margin. Size alone does not disqualify struct.

---

### 2. Boxing Hazards

Boxing occurs when a struct is stored as `object`, passed through an interface, or placed in a non-generic collection. The audit:

| Site | Boxing? | Why |
|------|---------|-----|
| `Get<Quantity>()` → return by value | ❌ | Generic return, value copied to caller |
| `IArgBuilder.Set<Quantity>(name, v)` | ❌ | Generic parameter, passed by value |
| `ImmutableArray<Quantity>` (hypothetical) | ❌ | Generic collection, no boxing |
| `ConstraintViolation.FailingValue` | ❌ | `JsonElement?`, not `Quantity` |
| `FieldSnapshot.Value` | ❌ | `JsonElement?`, not `Quantity` |
| `object` anywhere on the public surface | ❌ | None — public API is clean by design |
| Internal evaluator stack (if `object[]` frame) | ⚠️ | Implementation choice |

The only boxing risk is the evaluator's internal dispatch stack. If the evaluator uses a heterogeneous `object[]` value stack (common in interpreted runtime implementations), every `Quantity` operation would box on push and unbox on pop. **This risk is absorbed by the `PreceptValue` layer, not by `Quantity`.** The evaluator operates on `PreceptValue` instances for its internal slot storage; `Quantity` is only materialized externally. If the evaluator later gains a typed computation path (for optimization), it can use `Quantity` on a typed stack without boxing.

**Boxing verdict:** No boxing hazards exist in the designed API surface. The struct is safe.

---

### 4. Copy Semantics and Mutability

With `readonly record struct`:

- Every assignment copies the 24 bytes. In practice this is `Amount` (16 bytes) + `Unit` reference (8 bytes).
- `readonly` on the struct means no method can mutate it; the compiler never inserts defensive copies when calling members through a read-only reference.
- Immutability is correct semantics: a quantity like "12 kg" should not change in place. Arithmetic operations produce new values, not mutations.

**Defensive copy hazard:** This is the primary struct footgun. It occurs when a non-readonly method is called on a struct stored in a `readonly` field or `in` parameter — the compiler silently copies the struct to allow the call. With `readonly record struct`, **all auto-generated members are readonly** (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, property getters). Custom arithmetic operator implementations should also be `static` or `readonly`. This is trivially achievable and straightforward to enforce.

**Is 24 bytes too large for copy?** No. The comparison is: copy 24 bytes (deterministic, register-resident) vs. allocate on heap (non-deterministic latency, GC pressure, cache miss on dereference). The struct wins even at 24 bytes for short-lived values. The benchmark crossover point for "struct copy becomes more expensive than heap reference" is typically 64–128 bytes, not 24.

---

### 5. Record Struct Specifics

`readonly record struct` adds over a plain `readonly struct`:

- **Value equality** (`Equals`, `==`): `Quantity(10m, kg) == Quantity(10m, kg)` → `true`. This is semantically correct — two quantities with the same amount and unit are equal. Structural equality is the right contract.
- **`with` expressions**: `value with { Amount = newAmount }` — convenient for the evaluator if it needs to produce a modified quantity (e.g., unit conversion result without allocating).
- **`Deconstruct`**: `var (amount, unit) = quantityValue;` — ergonomic for pattern matching in evaluation logic.
- **`GetHashCode`**: Auto-generated from all fields. Correct for dictionary keying if quantities are ever used as keys.

No runtime overhead vs plain `readonly struct`. Record struct is strictly additive here.

**Equality note:** The auto-generated equality for `readonly record struct Quantity(decimal Amount, Unit Unit)` calls `EqualityComparer<Unit>.Default.Equals(this.Unit, other.Unit)`, which routes through `Unit`'s `IEquatable<Unit>` implementation (equality by `Code`). This is correct: two `Quantity` instances using the same unit code are equal even if they hold different interned object instances.

---

### 6. C# 12+ Improvements Applicable Here

Two features are worth noting for future optimization paths:

**`ref readonly` parameters**: If the evaluator gains a typed computation path that passes `Quantity` between evaluation stages, `ref readonly Quantity` eliminates the 24-byte copy entirely for large argument chains. Not needed now, but the struct design makes this available.

**`in` parameters**: Already available. `bool IsCompatibleUnit(in Quantity a, in Quantity b)` avoids copying when checking unit compatibility in constraint evaluation.

**`InlineArray`**: Not applicable here — this is for fixed-size numeric buffers, not compound value types.

---

### 7. Why `Unit` as `sealed class` Is Correct

The `Unit` type should remain `sealed class`. This is a separate decision from `Quantity`:

- **Interning**: `UnitCatalog.Get("kg")` always returns the same object instance. Reference equality (`object.ReferenceEquals`) becomes a fast O(1) comparison path before falling back to `Code` string comparison. Interning requires heap identity — a struct cannot be interned.
- **Nullable representation**: `Unit?` (nullable reference) represents "no unit" cleanly. Nullable struct (`Unit?`) works but carries different semantics and requires the Nullable machinery.
- **Extensible metadata**: Future additions (symbol, canonical display format, conversion factor chains) do not break the binary layout. The `sealed class` can grow fields; a shipped struct's size is baked into calling assemblies.
- **Correct lifetime**: `Unit` instances live for the lifetime of the `UnitCatalog` (application-scoped singleton). They are not short-lived scratch objects. GC overhead is negligible against a small fixed set of interned instances.
- **Not a value type conceptually**: "kg" is not an amount — it's an identity. Value types are for values; reference types are for entities. `Unit` is an entity (identified by code, with metadata, interned). `Quantity` is a value (an amount of something).

---

### 8. Recommendation

**`Quantity`: `readonly record struct` — confirmed correct. No change needed.**

The current proposal in §5 already makes the right call. This evaluation exists to document WHY, not to prescribe a correction.

**The case for sealed class does not hold for `Quantity`:**
- It is not interned and does not require reference identity.
- It has no extensibility requirements — the two fields are the complete definition.
- It is short-lived in the hotpath — heap allocation is active harm, not neutral choice.
- The public API surface has no `object` exposure that would force boxing.

**Two shapes, two purposes — already built into the design:**

| Shape | Type | Use | Lifetime |
|-------|------|-----|----------|
| `readonly record struct` | `Quantity` | API boundary, expression scratch (future typed path) | Transient |
| 32-byte tagged struct | `PreceptValue` | Runtime slot storage | Opaque tagged union; no per-value heap allocation |
| `sealed class` (internal) | `Unit` | Evaluator-internal unit identity, interned from `UnitCatalog`; public API proxy is `UnitOfMeasure` (see §12) | Catalog lifetime |

If the evaluator later gains an optimization path that operates on `Quantity` structs directly on a typed computation stack (bypassing `PreceptValue` for short-lived arithmetic), the struct design enables that path without any breaking change.

**The only scenario where sealed class would win:** if `Quantity` needed to participate in a class hierarchy for polymorphism, or if its size grew beyond ~64 bytes making copy cost prohibitive. Neither condition holds. Twenty-four bytes, no polymorphism, correct value semantics → struct is the right tool.

---

## 7. Business Types — Coverage Assessment

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04  
**Trigger:** Shane identified `Price` and `ExchangeRate` as missing from the investigation. This section audits the full space of business value types that deserve first-class treatment in Precept's public API.

---

### Design Principle: When Does a Business Concept Earn Its Own Type?

A concept deserves a dedicated Precept value type when it satisfies **at least two** of:

1. **Structural distinction** — it carries fields that are not present on any existing type, OR it carries the same fields but with different *invariants* that the type system should enforce.
2. **Behavioral distinction** — operations on it differ from the nearest existing type (different arithmetic, different comparison, different validation rules).
3. **Semantic distinction with runtime consequence** — it names something the evaluator, constraints, or guards must *recognise and treat differently* at evaluation time.

A concept that fails all three is a **semantic decoration** — it can be expressed as the underlying type with metadata (field name, documentation, domain context). Precept does not mint types for documentation purposes.

---

### D12 — Decimal Backing Mandate

All seven business-domain types (`money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`) use `decimal` as their magnitude backing. This is locked by **D12** in `docs/language/business-domain-types.md`.

**Key constraints:**
- `double`/`number` is explicitly rejected as a scalar operand for business types. The type checker emits a teachable diagnostic when `number` (which is `double`-backed) appears as an operand alongside a business-domain type.
- The result-type algebra demands homogeneous backing: `price × quantity → money`, `money / quantity → price`, `money / price → quantity`. If any one type used `double`, every cross-type operation would hit a `decimal ÷ double` boundary that silently injects `double`-precision artifacts. `0.1 + 0.2 ≠ 0.3` in IEEE 754 — unacceptable for business arithmetic.
- `integer` widens to `decimal` losslessly, so `Amount * 2` works without friction. Fractional literals resolve to `decimal` via context-sensitive literal typing when the co-operand is a business-domain type.

This mandate is cross-cutting — it applies to `Money`, `Quantity`, `Price`, and `ExchangeRate` uniformly. The CLR struct definitions throughout this document use `decimal` for all magnitude fields in conformance with D12.

---

### 7.1 Price — First-Class Canonical Named Type

**`price` IS a first-class canonical named type** defined in `docs/language/business-domain-types.md` (§ Runtime engine changes, § Operator tables). It is structurally distinct from `Money`.

**CLR shape:**
```csharp
public readonly record struct Price(decimal Amount, Currency Currency, UnitOfMeasure Unit)
```

**Three-field backing:**
- `decimal Amount` — magnitude (the numeric value)
- `Currency Currency` — numerator currency (ISO 4217, e.g. `"USD"`)
- `UnitOfMeasure Unit` — denominator unit (UCUM, e.g. `"kg"`, `"each"`)

**Structural distinction from `Money`:** `Money` is `(decimal Amount, Currency Currency)` — two fields. `Price` has a third field: the denominator unit. A price is inherently a compound type — currency *per* unit. `'24.50 USD/kg'` cannot be represented by `Money` without losing the denominator.

**Key operator — dimensional cancellation:**
- `price × quantity → money` (denominator unit cancels: `'24.50 USD/kg' × '3 kg' → '73.50 USD'`)
- `money / quantity → price`
- `money / price → quantity`

**DSL declaration syntax:** `field UnitPrice as price in 'USD/each'`

**Design note:** The `decimal` magnitude backing is consistent with D12 (decimal backing mandate for all seven business-domain types). The CLR type name `Price` follows the same naming convention as `Money`, `Quantity`, `ExchangeRate`.

---

### 7.2 ExchangeRate — YES, a Separate Type

**CLR shape:**
```csharp
/// The rate to convert one unit of the source currency into the target currency.
/// Convention: 1 From = Amount × To
public readonly record struct ExchangeRate(
    decimal Amount,      // e.g., 0.92 means 1 From = 0.92 To
    Currency From,       // ISO 4217 source currency: "USD"
    Currency To)         // ISO 4217 target currency: "EUR"
{
    public override string ToString() => $"{Amount} {From}/{To}";
}
```

**Why this earns its own type:**

1. **Structural distinction:** Three fields — a currency *pair* plus a rate magnitude. This is not a `Money` (which is amount + single currency). It cannot be expressed as any existing type without loss of information.

2. **Behavioral distinction:** An exchange rate is NOT money. You don't "add" two exchange rates. You *apply* a rate to convert a `Money` from one currency to another. The operation is: `Money × ExchangeRate → Money`. This is fundamentally different from money arithmetic.

3. **Semantic distinction with runtime consequence:** Guards and constraints that validate currency conversion logic need to inspect the rate's currency pair to ensure the `.from`/`.to` match the operands. The type checker can validate that a rate's `From` matches the source `Money.Currency`. This is a type-level invariant.

**Accessors (canonical per `docs/language/business-domain-types.md`):**

| Accessor | Type | Meaning |
|----------|------|---------|
| `.from` | `currency` | Source currency (`'USD'` in `'USD/EUR'`) |
| `.to` | `currency` | Target currency (`'EUR'` in `'USD/EUR'`) |
| `.amount` | `decimal` | Magnitude (the numeric part) |

**Implicit `positive` constraint (locked — D16 Corollary 2):** `ExchangeRate` carries an implicit `positive` constraint on `Amount`. Zero and negative exchange rates are always invalid configurations. A zero rate silently converts any amount to zero — a degenerate result indistinguishable from a modeling error. A negative rate has no economic meaning. Explicitly declaring `positive` or `nonzero` on an `exchangerate` field is redundant (compiler may warn). This is enforced at the same tiers as `in`-constraint enforcement: literal assignment (compile), event-arg input (runtime boundary), and `set` time (runtime).

**Design notes:**

- **Temporal validity is NOT on the type.** An exchange rate value is "the rate at the point in time it was captured." The *timestamp* of when the rate was valid is a separate field on the entity — `rate_as_of: datetime` alongside `exchange_rate: ExchangeRate`. The value type is timeless; the entity tracks when.
- **Inverse derivation:** `ExchangeRate(0.92m, "USD", "EUR")` implies `ExchangeRate(1/0.92m, "EUR", "USD")`. Whether Precept provides a `.Invert()` method is an API convenience question, not a type design question.
- **32 bytes:** `decimal + Currency + Currency` = 16 + 8 + 8 = 32 bytes (reference sizes on x64; `Currency` instances are interned from `CurrencyCatalog`, not per-instance heap allocations). Still well under the copy-cost crossover. `readonly record struct` is correct.
- **Currency pair identity:** Two rates are "for the same pair" if `From` and `To` match. The `record struct` auto-generated equality handles this correctly.


---

## 8. Currency Type Design

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04  
**Status:** ✅ **Locked — 2026-05-04.** Shane selected **frank-114** (`sealed class Currency`, ISO 4217 catalog model). See `.squad/decisions/accepted/frank-currency-type-design.md`.  
**Trigger:** Shane's request to elevate `currency` from a validated-string backing to a first-class CLR value type backed by ISO 4217, following the `Unit`/`UnitCatalog` pattern.

### Decision Summary

Shane selected **frank-114**: `sealed class Currency`, consistent with `Unit`, backed by `CurrencyCatalog` loaded from embedded ISO 4217 resource.

**Why frank-114 over the `readonly record struct` alternative (prior inbox draft):**

The prior draft proposed `readonly record struct CurrencyCode` with well-known static constants. It was superseded for three structural reasons:
1. **Contradicts the `unit` architectural precedent.** §5 chose `sealed class Unit` for interning, extensible metadata, and catalog lifetime. `currency` has the identical structural profile — an identity type, catalog-backed, ~180 instances. Choosing a different shape creates an architectural inconsistency between two identity types that differ only in their backing standard.
2. **Contradicts the pure database model principle.** Well-known statics (`CurrencyCode.USD`, `CurrencyCode.EUR`) violate the "library doesn't bless a subset" principle established for `UnitCatalog`. Consumers can define their own convenience accessors in application code.
3. **Interning requires heap identity.** `CurrencyCatalog.Default.Get("USD")` always returns the same instance; `ReferenceEquals` is O(1). Structs cannot be interned.

**Tradeoff accepted:** `CurrencyCatalog.Default.Get("USD")` is more verbose than `CurrencyCode.USD`. This is the same ergonomic tradeoff accepted for `UnitCatalog` — and accepted for the same reasons.

All open questions (OQ-CUR-1, OQ-CUR-2, OQ-CUR-3, OQ-CUR-4) are resolved — see §8.10.

---

### 8.1 Why `currency` Deserves First-Class CLR Backing

The §7.6 summary records `currency` as "CLR backing is `string` (ISO 4217) — Not a new struct — string-backed identity type in the DSL." This is the *minimum viable* representation. The `unit`/`UnitCatalog` pattern established in §5 proves the correct architecture: a standard identity (UCUM code / ISO 4217 code) backed by a catalog type with structured metadata. `currency` should follow this exact pattern.

Four concrete reasons the upgrade is warranted:

1. **ISO 4217 carries metadata the runtime already uses.** `MinorUnit` (decimal places per currency) is the foundation of D10's implicit `maxplaces` on `money` fields. Today this is a lookup into a hardcoded table embedded in the type checker. With a `Currency` CLR type, D10 reads `CurrencyCatalog.Default.Get(code).MinorUnit` — the catalog is the single canonical source. Hardcoded tables are a catalog-driven architecture violation.

2. **Consumers need enriched accessor access.** DSL guards and constraints already reference `.currency` on `money`, `price`, and `exchangerate` fields. Today these return the bare `string` code. After this design, `payment.currency.name` returns "Euro", `payment.currency.symbol` returns "€", `payment.currency.minorUnit` returns 2 — directly, at evaluation time, with no out-of-band lookup.

3. **The `unit` pattern proves the architecture.** `unitofmeasure` fields return a string code at the bare string layer; `UnitCatalog` enriches it to a `Unit` object. The parallel is exact: `currency` fields return a string code; `CurrencyCatalog` enriches it to a `Currency` object. Inconsistency between the two identity types would be an architectural smell.

4. **`symbol` is on every invoice.** Every business system that formats monetary amounts needs the currency symbol. Without `.symbol` on `currency`, consumers must maintain a parallel out-of-band lookup table — exactly the structural duplication the catalog-driven architecture prevents.

---

### 8.2 The ISO 4217 Standard — Scope and Coverage

ISO 4217 defines:

| Field | Example (USD) | Example (JPY) | Example (KWD) | In the standard? |
|-------|--------------|--------------|--------------|-----------------|
| Alphabetic code | `USD` | `JPY` | `KWD` | ✅ Yes |
| Numeric code | `840` | `392` | `414` | ✅ Yes |
| Name | `US Dollar` | `Yen` | `Kuwaiti Dinar` | ✅ Yes |
| Minor unit (decimal places) | `2` | `0` | `3` | ✅ Yes |
| Symbol | `$` | `¥` | `KD` | ❌ Not in ISO 4217 — colloquial only |

The standard covers approximately **180 active codes** plus a small set of special codes (XAU for gold, XDR for SDR, XTS for testing). Unlike UCUM, ISO 4217:
- Has no compositional grammar — a currency code is always atomic
- Has no tiers — all 180 codes are equally relevant for business
- Is fully enumerable as a flat list
- Has a simpler validation model: dictionary lookup only, no grammar parser

**Amendment cadence:** ISO 4217 amendments are published several times per year as countries rename currencies or join/leave currency unions. Actual code *additions* are rare — they happen only when a country adopts a new currency, which is a multi-year event. Most amendments are country-name corrections. This is more frequent than UCUM (years between revisions) but the functional impact of being one amendment behind is negligible. ISO 4217 data ships as an embedded resource in the `CurrencyCatalog` assembly — no separate data package at v1; promote if amendment drift creates meaningful operational friction.

---

### 8.3 CLR Representation — `Currency` Sealed Class

```csharp
/// An ISO 4217 currency, identified by its alphabetic code.
/// Value-semantics by code. Interned from the CurrencyCatalog.
public sealed class Currency : IEquatable<Currency>
{
    public string AlphaCode   { get; }   // ISO 4217 alphabetic: "USD", "EUR", "JPY"
    public int    NumericCode { get; }   // ISO 4217 numeric: 840, 978, 392
    public string Name        { get; }   // Official name: "US Dollar", "Euro", "Yen"
    public string Symbol      { get; }   // Display symbol: "$", "€", "¥" (curated supplement; disambiguated where shared, e.g., "US$" for USD)
    public int    MinorUnit   { get; }   // Decimal places: 2 (USD), 0 (JPY), 3 (KWD)

    // Equality by alphabetic code
    public bool Equals(Currency? other) => AlphaCode == other?.AlphaCode;
    public override int GetHashCode()   => AlphaCode.GetHashCode();
    public override string ToString()   => AlphaCode;  // Serializable identity
}
```

#### Why `sealed class`, Not `readonly record struct`

The reasoning is identical to §7 (`Unit`):

| Consideration | Why sealed class wins |
|---|---|
| **Interning** | `CurrencyCatalog.Get("USD")` always returns the same instance. `ReferenceEquals` is a O(1) fast path. Interning requires heap identity — structs cannot be interned. |
| **Nullable** | `Currency?` means "no currency" cleanly via reference null. |
| **Extensible metadata** | Future fields (`Countries`, `IsActive`, `ReplacedBy`, `InactiveSince`) add no breaking layout change. |
| **Correct lifetime** | ~180 instances live for the `CurrencyCatalog` lifetime (application-scoped singleton). GC overhead is negligible for a small fixed population. |
| **Identity, not a value** | "USD" is an entity — it identifies a currency. `Money` is the value that *carries* a `Currency`. Value types are for values; reference types are for entities. |

The case for `readonly record struct` does not hold: 180 fixed interned instances benefit from reference identity, the catalog lifetime is application-scoped (no hotpath copy cost), and future extensibility requires flexible layout.

---

### 8.4 `CurrencyCatalog` (Analogous to `UnitCatalog`)

```csharp
/// The ISO 4217 currency catalog — loads and serves currency metadata.
public sealed class CurrencyCatalog
{
    // Singleton loaded from embedded ISO 4217 data resource
    public static CurrencyCatalog Default { get; }

    // Alphabetic code lookup (primary key — ISO 4217 alpha-3)
    public Currency Get(string alphaCode);
    public bool TryGet(string alphaCode, out Currency currency);

    // Numeric code lookup (ISO 4217 numeric-3)
    public Currency GetByNumericCode(int numericCode);
    public bool TryGetByNumericCode(int numericCode, out Currency currency);

    // Validation
    public bool IsValid(string alphaCode);

    // Enumeration — all active codes (~180)
    public IReadOnlyList<Currency> All { get; }

    // Metadata
    public string DataVersion { get; }  // ISO 4217 amendment identifier, e.g., "2024-03"
}
```

**Internal backing:**

```csharp
// Loaded once at startup, frozen for the runtime lifetime
private readonly FrozenDictionary<string, Currency> _byAlpha;    // "USD" → Currency
private readonly FrozenDictionary<int,    Currency> _byNumeric;  // 840 → Currency
```

`FrozenDictionary` is appropriate: ~180 entries, loaded once, never mutated. Lookup is O(1), the full table fits in L1 cache.

#### How `CurrencyCatalog` Differs from `UnitCatalog`

UCUM drove `UnitCatalog`'s design complexity: 2,600+ atoms, a compositional grammar, infinite derived expressions, and three discovery tiers. ISO 4217 needs none of that:

| Feature | `UnitCatalog` | `CurrencyCatalog` |
|---|---|---|
| Entry count | ~2,600+ atoms | ~180 codes |
| Grammar parser | Yes — UCUM expressions | No — codes are atomic |
| Discovery tiers | Yes — Tier 1/2/3 | No — all codes are Tier 1 |
| `IsValidExpression` | Yes | No — `IsValid(alphaCode)` only |
| `Browse(tier, dimension)` | Yes | No — `All` suffices |

The catalog architecture (embedded resource, `FrozenDictionary` at startup, singleton) is the same. The implementation is simpler because the standard is simpler.

#### How It Fits the Catalog-Driven Architecture

`CurrencyCatalog` is a runtime catalog — same pattern as `UnitCatalog`:
- **Metadata-driven:** currency definitions are data (embedded resource), not code
- **Pipeline stages consume generically:** the type checker validates codes against the catalog; the evaluator resolves `Currency` objects for accessor evaluation
- **D10 grounding:** `money in 'USD'` implicit `maxplaces` reads `CurrencyCatalog.Default.Get("USD").MinorUnit` — the catalog is the canonical source, eliminating the hardcoded table
- **Tooling derives from it:** language server completions, hover text, MCP `precept_language` output

---

### 8.5 Catalog Entry — `TypeKind.Currency` After This Design

The `TypeMeta` record for `TypeKind.Currency` gains four accessors. Before/after:

**Before (current):**
```csharp
TypeKind.Currency => new(
    kind, Tokens.GetMeta(TokenKind.CurrencyType),
    "Currency identity (e.g., USD, EUR)",
    TypeCategory.BusinessDomain,
    Traits: TypeTrait.EqualityComparable,
    ImpliedModifiers: [ModifierKind.Notempty],
    DisplayName: "currency",
    HoverDescription: "An ISO 4217 currency code identifier such as 'USD' or 'EUR'. Carries notempty implicitly.",
    UsageExample: "field BaseCurrency as currency default 'USD'"
),
```

**After (this design):**
```csharp
TypeKind.Currency => new(
    kind, Tokens.GetMeta(TokenKind.CurrencyType),
    "ISO 4217 currency identity with code, name, symbol, and precision metadata",
    TypeCategory.BusinessDomain,
    Traits: TypeTrait.EqualityComparable,
    ImpliedModifiers: [ModifierKind.Notempty],
    Accessors:
    [
        new FixedReturnAccessor("name",        TypeKind.String,  "Official ISO 4217 currency name (e.g., 'US Dollar')"),
        new FixedReturnAccessor("symbol",       TypeKind.String,  "Display symbol (e.g., '$', '€') — from curated supplement; disambiguated where symbol is shared across currencies (e.g., 'US$' for USD)"),
        new FixedReturnAccessor("minorUnit",    TypeKind.Integer, "Decimal places per ISO 4217 minor unit (2 for USD, 0 for JPY, 3 for KWD)"),
        new FixedReturnAccessor("numericCode",  TypeKind.Integer, "ISO 4217 numeric code (840 for USD, 978 for EUR)"),
    ],
    DisplayName: "currency",
    HoverDescription: "An ISO 4217 currency — exposes .name, .symbol, .minorUnit, and .numericCode. Carries notempty implicitly.",
    UsageExample: "field BaseCurrency as currency default 'USD'"
),
```

**Critical invariant:** The serialized value is unchanged. A `currency` field serializes as its alpha code string (`"USD"`) and deserializes the same way. The four accessors are derived from `CurrencyCatalog` at evaluation time. They add zero storage cost.

No `QualifierShape` is added. `currency` intentionally supports neither `in` nor `of`. The type IS the identity; qualification would mean "a currency that is this specific currency" — a constant, not a constraint. Use `rule` or `when` guards for equality checks on identity types.

---

### 8.6 DSL Surface — Declaration and Accessors

**Field declaration** — unchanged:
```precept
field BaseCurrency    as currency default 'USD'
field InvoiceCurrency as currency optional
```

**New accessor patterns in guards and rules:**
```precept
# Precision check on an open-currency money field using the catalog's minor unit
rule Payment.amount.scale <= Payment.currency.minorUnit
  because "payment amount precision must not exceed the currency's minor unit"

# Guard-based currency display in computed output
from Active on GenerateReceipt
  set ReceiptLine = Amount + ' ' + BaseCurrency.symbol + ' (' + BaseCurrency.name + ')'

# External system integration via numeric code
when BaseCurrency.numericCode == 978   # EUR = ISO 4217 numeric 978
  set Region = 'Eurozone'
```

**Typed constant literals** — unchanged:
```precept
field BaseCurrency as currency default 'USD'
```

**Validation** — unchanged:
- Literal `'USD'` at compile time → validated against `CurrencyCatalog.Default`
- Runtime event-arg/update boundary → validated via `CurrencyCatalog.Default.IsValid(code)`
- `'USDX'` → compile error: not a recognized ISO 4217 code

---

### 8.7 Precision and D10 — Structural Grounding

D10 (implicit `maxplaces` from ISO 4217 `MinorUnit`) becomes structurally grounded by `CurrencyCatalog`:

| Aspect | Before | After |
|--------|--------|-------|
| Source of truth | Hardcoded lookup table in type checker | `CurrencyCatalog.Default.Get(code).MinorUnit` |
| Where ISO data lives | Two places: the type checker table + the currency validation registry | One place: `CurrencyCatalog` |
| Catalog-driven? | No — domain knowledge is hardcoded in pipeline stage | Yes — catalog is canonical source |

Type checker D10 logic after this design:
```csharp
var minorUnit = CurrencyCatalog.Default.Get(currencyCode).MinorUnit;
// Implicit maxplaces constraint = minorUnit
```

This is a small but architecturally significant cleanup: it removes the only place where the type checker embeds per-currency metadata rather than deriving it from the catalog.

**Does `currency` compose with `amount` to form a new type?** No. `money` is already the composite `(decimal amount, currency)` type. There is no `field Price : amount in USD` syntax — declare `field Price as money in 'USD'`. `currency` is the identity-only type; this design does not introduce new composition patterns.

---

### 8.8 Impact on Related CLR Types (Money, Price, ExchangeRate)

Introducing `Currency` as a structured CLR type raises a ripple question: should `Money`, `Price`, and `ExchangeRate` change their currency fields from `string` to `Currency`?

**Before OQ-CUR-2 upgrade:**
```csharp
public readonly record struct Money(decimal Amount, string Currency)
public readonly record struct Price(decimal Amount, string Currency, string Unit)
public readonly record struct ExchangeRate(decimal Amount, string From, string To)
```

**After OQ-CUR-2 upgrade (✅ Applied):**
```csharp
public readonly record struct Money(decimal Amount, Currency Currency)
public readonly record struct Price(decimal Amount, Currency Currency, UnitOfMeasure Unit)
public readonly record struct ExchangeRate(decimal Amount, Currency From, Currency To)
```

Size impact on x64: `string` (8 bytes managed reference) and `Currency` (8 bytes managed reference) have the same slot size. No change to struct size.

**Rationale for upgrade:**
1. **No breaking-change cost** — none of these CLR types are shipped yet
2. **Structural consistency** — the catalog model says `money.currency` returns `TypeKind.Currency`; the CLR type matches
3. **Direct `.MinorUnit` access** — `Money.Currency.MinorUnit` eliminates a re-lookup for D10 enforcement in the evaluator
4. **Interning benefit** — the `Currency` references in `Money`, `Price`, and `ExchangeRate` instances all point to the same ~180 interned objects; no per-instance metadata duplication

**OQ-CUR-2: ✅ Locked — upgrade applied.**

---

### 8.9 ISO 4217 vs. UCUM — Architectural Parallel with One Key Difference

Both currency and unit use the same catalog-driven pattern:

| Aspect | `unit` / `UnitCatalog` | `currency` / `CurrencyCatalog` |
|---|---|---|
| DSL keyword | `unitofmeasure` | `currency` |
| CLR identity type | `Unit` (sealed class) | `Currency` (sealed class) |
| Catalog | `UnitCatalog` | `CurrencyCatalog` |
| Standard | UCUM | ISO 4217 |
| Backing | Embedded `ucum.dat` | Embedded `iso4217.dat` |
| Internal lookup | `FrozenDictionary<string, Unit>` | `FrozenDictionary<string, Currency>` |

**The key difference** is complexity, not architecture:

| Dimension | UCUM | ISO 4217 |
|---|---|---|
| Entry count | ~2,600 atoms | ~180 codes |
| Grammar | Yes — infinite derived expressions | No — atomic codes only |
| Discovery tiers | Yes — Tier 1/2/3 | No — all codes are Tier 1 |
| Validation | Grammar parser + dictionary | Dictionary only |

`CurrencyCatalog` is a simpler implementation of the same pattern. The architectural identity is preserved.

---

### 8.10 Currency API Surface Decisions

`symbol` is included in `Currency`. The "not in ISO 4217" objection is a purity argument, not a practical one. Every business display of monetary amounts needs the currency symbol; requiring callers to maintain a parallel symbol map defeats the catalog-driven architecture's purpose. Symbols that are ambiguous across currencies use a disambiguated form in the curated supplement (e.g., `US$` for USD rather than bare `$`, which is shared by USD/CAD/AUD/NZD/HKD). The field is sourced from the curated supplement accompanying the ISO 4217 standard — not from the standard itself — and this is noted in the field comment.

`Money.Currency`, `Price.Currency`, and `ExchangeRate.From`/`.To` use the `Currency` CLR type. None of these CLR types are shipped; the upgrade carries no breaking-change cost. Structural consistency with the catalog-backed `sealed class Currency` design is mandatory.

Both `Get<Currency>()` and `Get<string>()` are supported for `currency`-typed fields. `Get<Currency>()` returns the full catalog-backed `Currency` object with all accessors (`.name`, `.symbol`, `.minorUnit`, `.numericCode`). `Get<string>()` returns the alpha code string for consumers that only need the code — serialization, logging, code-only comparison. The `TypeRuntime<Currency>` adapter handles both via the standard typed-lane dispatch.

ISO 4217 data is embedded as a compiled resource in the assembly hosting `CurrencyCatalog`. ~180 rows, loaded once at startup. Amendment cadence is primarily country name corrections; actual code additions happen only when a country adopts a new currency (multi-year event). A separate data package is not justified at v1; promote if amendment drift creates meaningful operational friction.

---

### 8.11 Summary

**Status: ✅ Locked** — Shane selected frank-114 on 2026-05-04. All open questions resolved — see §8.10.

| Design Point | Decision | Status | Rationale |
|---|---|---|---|
| CLR type name | `Currency` | ✅ Locked | Parallel to `Unit` — same naming convention |
| CLR shape | `sealed class` | ✅ Locked | Interning, extensible metadata, catalog lifetime — identical to `Unit` reasoning |
| Fields | `AlphaCode`, `NumericCode`, `Name`, `Symbol`, `MinorUnit` | ✅ Locked | ISO 4217 defined + curated `Symbol` supplement (see §8.10) |
| Catalog | `CurrencyCatalog` | ✅ Locked | Embedded resource, `FrozenDictionary` backing, analogous to `UnitCatalog` |
| Tiers | None | ✅ Locked | ~180 codes, flat list — no tiering needed (unlike UCUM) |
| Grammar parser | None | ✅ Locked | ISO 4217 is flat — no compositional grammar |
| D10 grounding | `CurrencyCatalog.Default.Get(code).MinorUnit` | ✅ Locked | Removes hardcoded table from type checker |
| DSL declaration | Unchanged — `field X as currency` | ✅ Locked | Identity types don't support `in`/`of` |
| New accessors | `.name`, `.symbol`, `.minorUnit`, `.numericCode` | ✅ Locked | Derived from catalog at evaluation time; zero storage cost |
| Serialization | Unchanged — `"USD"` (alpha code string) | ✅ Locked | Value is the code; metadata is always derived |
| `Money.Currency` type | Upgrade to `Currency` (OQ-CUR-2) | ✅ Locked | Structural consistency; direct `.MinorUnit` access; no breaking change |
| `symbol` inclusion | Included — curated supplement with disambiguation | ✅ Locked | See §8.10 |
| `Get<T>()` currency lanes | Both `Get<Currency>()` and `Get<string>()` supported | ✅ Locked | See §8.10 |
| Shipping mechanism | Embedded resource in `CurrencyCatalog` assembly | ✅ Locked | See §8.10 |

---

## 9. Computation Model — Evaluator-Only (LOCKED)

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04  
**Status:** ✅ **Locked — 2026-05-05.** Supersedes all Option A (computation-on-types) analysis.

### 9.1 The Verdict

**CLR types (`Money`, `Price`, `ExchangeRate`, `Quantity`) are pure data records.** They carry:
- Construction
- `ToString()` / `Parse()`
- Value equality (auto-generated by `readonly record struct`)

They do NOT carry:
- Arithmetic operators (`+`, `-`, `*`, `/`)
- Validation logic
- Named computation methods (`ConvertTo`, `RoundToMinorUnit`, etc.)

**All computation lives in named executor modules** — `internal static` classes in `src/Precept/Runtime/Operations/`:
- `MoneyOperations`
- `QuantityOperations`
- `PriceOperations`
- `ExchangeRateOperations`

Each method in an executor module corresponds to one `OperationKind`.

### 9.2 Why Not Computation on Types (Option A Rejection)

Option A proposed that `Money`, `Price`, etc. carry their own arithmetic methods (with `IUnitConversionSource` injection where needed). It was rejected for three structural reasons:

1. **Structural duplication of domain rules.** Seven domain rules (same-currency guard, same-unit guard, D15 boundary, D16 exception table, D8 auto-conversion, precision enforcement, positive-rate invariant) would need enforcement in both CLR type operators AND the evaluator pipeline. Two enforcement points for the same rule must stay manually in sync — a maintenance trap.

2. **Injection parameter problem.** C# operators cannot take extra parameters. `Quantity.operator+(Quantity, Quantity)` requires same-unit enforcement, but D8 auto-conversion requires an `IUnitConversionSource`. Under Option A, this forces D8 into a named method (`AddSameDimension(other, source)`) that cannot be an `operator+` — breaking the operator surface promise.

3. **Return-type ambiguity.** `Money / Money` returns `decimal` (ratio, same currency), but `Money / Quantity` returns `Price`. C# cannot return two types from one operator signature based on operand combinations. This forces named methods that bypass the operator surface.

### 9.3 How Option B Works

Under the evaluator-only model:

```
Type checker → reads ProofRequirements from OperationMeta (compile time)
Evaluator    → calls executor module method (single runtime enforcement point)
```

- **Type checker:** validates operations are legal by reading `OperationMeta` from the `Language.Operations` catalog. Same-currency, same-unit, and D15/D16 checks are compile-time proof requirements.
- **Evaluator:** three-line dispatch — resolve operation, index into `TypeRuntimeMeta.BinaryExecutors`, call delegate. Zero domain logic in the evaluator.
- **Executor module:** the SINGLE runtime enforcement point for domain rules. D8 auto-conversion lives in `QuantityOperations.Add()` with full access to `UnitCatalog`. One path, not two.

---

## 10. Operations Registry & Executor Dispatch (LOCKED)

**By:** Frank (Lead Architect)  
**Date:** 2026-05-05  
**Status:** ✅ **Locked.**

### 10.1 Catalog vs. Execution — Separation

Two things that must NEVER be confused:

| Concern | Lives in | Type | Purpose |
|---------|----------|------|---------|
| **Operation legality** | `Language.Operations` catalog | `OperationMeta` | Language spec in machine-readable form. Consumed by type checker, language server, MCP, doc generator. |
| **Operation execution** | `Runtime/Operations/` modules | `Func<PreceptValue, PreceptValue, PreceptValue>` | Evaluator dispatch. Consumed only by the runtime. |

**`OperationMeta` records carry NO delegate fields, NO executor references.** The catalog-driven architecture axiom: *pipeline stages read catalogs; catalogs do not become pipeline stages.* Putting executors on `OperationMeta` inverts this relationship.

### 10.2 Registration Mechanism

```
TypeRuntimeMeta.BinaryExecutors[(int)kind]  ← SOURCE OF TRUTH
         ↓ (read at build time by opcode builder)
BinaryOp.Executor field                     ← EMBEDDED IN OPCODE
         ↓ (called at eval time)
opcode.Executor(left, right)                ← EVALUATOR DISPATCH
```

- **`TypeRuntimeMeta.BinaryExecutors` / `UnaryExecutors`** are instance arrays populated from executor module static methods at type initialization. These are the authority.
- **Opcode embedding:** `BinaryOp` gains `Executor: Func<PreceptValue, PreceptValue, PreceptValue>`. `UnaryOp` gains `Executor: Func<PreceptValue, PreceptValue>`. The opcode builder fetches from `TypeRuntimeMeta` at build time.
- **Evaluator dispatch:** `opcode.Executor(l, r)` — two steps (deref opcode → call delegate). No global array lookup, no Kind-based indexing at eval time.

### 10.3 Why Embedded Delegates (Not a Global Array)

The evaluator previously proposed a global `Operations.BinaryExecutors[]` flat array indexed by `OperationKind`. This was eliminated:

- **The fatal flaw:** opcodes are `sealed record` (reference types). The memory-layout argument ("flat value-type array, cache-friendly") was factually wrong. The evaluator already chases a heap pointer to reach every opcode.
- **Embedded path:** deref opcode → fetch delegate → call (2 steps).
- **Global array path:** deref opcode → extract Kind → index static array → fetch delegate → call (4 steps).
- Embedded has one fewer indirection. Global array is eliminated.

### 10.4 Delegate Shape

**Verdict:** `static readonly Func<PreceptValue, PreceptValue, PreceptValue>` delegates. Not `unsafe delegate*`.

- All executor methods are static — no closures, no instance state.
- `unsafe delegate*` saves ~150ns per event at business-operation cadence — unmeasurable. But it propagates `unsafe` through `BinaryOp`, `ExecutionPlan`, and into user-facing APIs.
- `static readonly Func<>`: ~48 bytes per delegate on x64. ~100 operations × 48 bytes = ~4.8 KB total, allocated once at type initialization, immortal for process lifetime. Zero per-eval allocation. Zero GC pressure.
- JIT devirtualizes and inlines static delegate calls in hot paths.

### 10.5 `record struct` Opcodes — Not Pursued

If opcodes were `record struct`, cache density would improve 4×. This is theoretically correct but practically irrelevant: Precept evaluates 5–50 opcodes per dispatch; the entire working set fits in L1 cache regardless. The embedded-delegate verdict holds for the deeper architectural reason: simplicity — one fewer indirection, one fewer global mutable structure, one fewer initialization ceremony, self-contained evaluator. Do not pursue `record struct` opcodes until profiling demands it.

---

## 11. Namespace Organization — `Precept.Types`

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04 (updated 2026-05-05)  
**Status:** Decided.

`Money`, `Quantity`, `Currency`, `UnitOfMeasure`, `MeasureDimension`, `UnitCatalog`, `CurrencyCatalog`, `Price`, and `ExchangeRate` live in the `Precept.Types` namespace within the existing `Precept` assembly. No separate NuGet package or assembly split is introduced.

---

## 12. Identity Type Shapes — Dual-Shape API Boundary

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04  
**Status:** Naming locked by Shane directives. Shape rationale accepted.

### 12.1 The Dual-Shape Principle

> *Expose the catalog entity at the API boundary only when ALL its properties are consumer-facing. Use a proxy struct when the entity carries evaluator-internal metadata.*

This produces an apparent asymmetry between `Currency` (sealed class, directly exposed) and `UnitOfMeasure`/`MeasureDimension` (readonly record structs, proxy shapes). The asymmetry is architecturally justified:

| Identity Type | API Boundary Shape | Why |
|---|---|---|
| **Currency** | `sealed class Currency` (direct entity) | Every property (AlphaCode, Name, Symbol, MinorUnit, NumericCode) is consumer-facing. No evaluator-internal fields. |
| **Unit of Measure** | `readonly record struct UnitOfMeasure` (proxy) | The evaluator-internal `Unit` (sealed class) carries Tier, DimensionVector, conversion factors — evaluator-internal metadata that doesn't belong on the public surface. The proxy carries only the validated UCUM code string. |
| **Dimension** | `readonly record struct MeasureDimension` (proxy) | The evaluator-internal `Dimension` (7-exponent SI vector) is dimensional analysis machinery. The proxy carries only the dimension name string. |

### 12.2 `UnitOfMeasure` — API Boundary Type

```csharp
/// Lightweight API proxy for unit-of-measure identity.
/// Carries only the validated UCUM code string.
public readonly record struct UnitOfMeasure(string Code)
{
    // Well-known constants (~25–30 Tier 1 units for DX convenience)
    public static readonly UnitOfMeasure Kilogram = new("kg");
    public static readonly UnitOfMeasure Meter = new("m");
    public static readonly UnitOfMeasure Liter = new("L");
    // ... etc.

    public static UnitOfMeasure Parse(string ucumCode) => new(ucumCode);
    public static bool TryParse(string ucumCode, out UnitOfMeasure result) { /* ... */ }

    public override string ToString() => Code;
}
```

### 12.3 `MeasureDimension` — API Boundary Type

```csharp
/// Lightweight API proxy for dimension identity.
/// Carries only the dimension name string.
public readonly record struct MeasureDimension(string Name)
{
    // Well-known constants (~12–15 named dimensions)
    public static readonly MeasureDimension Mass = new("mass");
    public static readonly MeasureDimension Length = new("length");
    public static readonly MeasureDimension Time = new("time");
    // ... etc.

    public static MeasureDimension Parse(string name) => new(name);
    public static bool TryParse(string name, out MeasureDimension result) { /* ... */ }

    public override string ToString() => Name;
}
```

### 12.4 Naming Decisions (Locked by Shane)

| Original Name | Final Name | Rationale |
|---|---|---|
| `UnitOfMeasureCode` | `UnitOfMeasure` | Shane directive: `Code` suffix disliked. No CLR naming conflict with evaluator-internal `Unit`. |
| `DimensionCode` | `MeasureDimension` | Shane directive: `Dimension` conflicts with existing `ProofRequirementMeta.Dimension` and planned algebraic `Dimension` type. `MeasureDimension` is distinct and domain-readable. |
| `CurrencyCode` | `Currency` | Not relevant — `Currency` (sealed class) is already the accepted direction per §8. |

### 12.5 Relationship to §5 (`Unit` Sealed Class)

§5 describes the evaluator-internal `Unit` sealed class — this is correct and unchanged. The `Unit` sealed class lives in the `Precept` (evaluator) assembly. At the API boundary, consumers see `UnitOfMeasure` (the proxy struct). The evaluator resolves `UnitOfMeasure.Code` → `Unit` via `UnitCatalog.Get(code)` internally.

---

## 13. `PreceptValue` — Internal-Only Axiom

**By:** Frank (Lead Architect)  
**Date:** 2026-05-05  
**Status:** ✅ **Locked** — Shane directive.

### 13.1 The Ruling

`PreceptValue` is a **strictly internal type** and must NEVER appear in any:
- Public method signature
- Return type
- Property type
- Generic constraint

The raw lane public indexer (`version["fieldName"]`) returns `JsonElement`, not `PreceptValue`.

### 13.2 Implications for Value Types

The dual-shape model established in §Addendum (struct vs. class evaluation) is confirmed:

| Layer | Type | Shape | Purpose |
|-------|------|-------|---------|
| Internal runtime slots | `PreceptValue` | 32-byte tagged struct | Opaque tagged union; all field and arg values at runtime |
| Public API boundary | `Money`, `Quantity`, `Price`, etc. | `readonly record struct` | Materialized on `Get<T>()`, no allocation on read |
| Raw lane | `JsonElement` | CLR struct | Wire-format access, no Precept types needed |

Consumers access value types via:
- **Typed lane:** `version.Get<Money>("total")` — materializes the `readonly record struct` from the internal `PreceptValue`
- **Raw lane:** `version["total"]` → `JsonElement` — zero Precept type dependency

### 13.3 Why Axiom 1 Is Non-Negotiable

Four reasons (sourced from collection types investigation §3):
1. **Brittleness** — evaluator-internal types have different stability requirements than the public surface
2. **AI agent hostility** — opaque internal types degrade agent accuracy when reasoning about APIs
3. **Contract** — generic type parameters are the hardest leakage vector to contain
4. **Dual-shape model** — collections are the vectorized case of the same internal/external shape rule that governs scalar value types
