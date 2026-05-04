# Precept Value Types — CLR Mapping Investigation

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04  
**Status:** Investigation — awaiting owner decisions  
**Context:** OQ-3 resolution (compound CLR types) from `runtime-api-public-surface-spec.md`

---

## 1. UCUM Overview

### What Is UCUM?

The **Unified Code for Units of Measure** (UCUM) is a machine-readable coding system for unambiguous representation of measurement units in electronic data exchange. It is not a measurement system itself — it is a *code system* that assigns unique, parseable string codes to units from multiple measurement systems.

### Domain Coverage

| Domain | Examples |
|--------|----------|
| SI base units | `m`, `kg`, `s`, `A`, `K`, `mol`, `cd` |
| SI derived | `N` (newton), `Pa` (pascal), `J` (joule), `W` (watt) |
| SI prefixes | `k` (kilo), `m` (milli), `u` (micro), `n` (nano) |
| US/Imperial customary | `[ft_i]` (foot), `[lb_av]` (pound avoirdupois), `[gal_us]` (US gallon) |
| Clinical/laboratory | `[IU]` (international unit), `[pH]`, `mg/dL` |
| Dimensionless | `%`, `[ppth]` (parts per thousand) |
| Arbitrary combinations | Any unit expression via UCUM grammar: `kg.m/s2`, `mol/L` |

UCUM covers approximately **2,600+ predefined unit atoms** plus an infinite set of derived expressions formed by its compositional grammar (multiplication `.`, division `/`, exponentiation, prefix application).

### Is It the Right Foundation?

**Yes.** UCUM is the right *code foundation* for Precept because:

1. **Machine-parseable grammar** — UCUM codes are not opaque strings; they have a formal grammar that enables dimensional analysis and conversion derivation.
2. **Interoperability standard** — adopted by HL7/FHIR, LOINC, ISO 11240, CDISC. Any Precept deployment interfacing with healthcare, logistics, or scientific systems will encounter UCUM codes.
3. **Living standard** — maintained by Regenstrief Institute. Current version: v2.1. Updates are infrequent (years between revisions), meaning the code set is stable.
4. **Complete coverage** — SI, customary, clinical, and combinatorial. No business domain Precept targets lacks UCUM coverage.

### Comparison to Other Standards

| Standard | Role | Machine-Readable? | Compositional Grammar? | Precept Fit |
|----------|------|-------------------|----------------------|-------------|
| **UCUM** | Unit coding system | Yes — formal grammar | Yes — arbitrary expressions | ✅ Foundation |
| **SI (BIPM)** | Defines the 7 base units + derived | No — human prose | No | Covered by UCUM |
| **ISO 80000** | Notation/presentation rules | No — document standard | No | Orthogonal (display layer) |
| **Domain-specific** (e.g., UNECE Rec.20) | Trade/logistics unit codes | Yes — flat code list | No — no composition | Subset of UCUM |

**Conclusion:** UCUM is the foundation. SI and ISO 80000 inform what UCUM encodes; they are not alternatives.

---

## 2. .NET Ecosystem Survey

### Existing Libraries

| Library | Units | Standard | Type Design | Active? |
|---------|-------|----------|-------------|---------|
| **UnitsNet** | ~1,500 units across 100+ quantity types | Own taxonomy (loosely SI-aligned) | One struct per quantity type (`Length`, `Mass`, etc.) with enum-per-unit | Yes |
| **QuantityTypes** | ~200 units | SI-focused | Generic `Quantity<T>` | Dormant |
| **UnitConversion** | Small | Custom | Flat converters | Dormant |
| **Gu.Units** | ~50 quantity types | SI | Source-generated structs | Low activity |

No .NET library implements UCUM. No .NET library provides a database-driven unit system.

### Why UnitsNet Is Inadequate for Precept

UnitsNet is the closest candidate. Here's why it doesn't fit:

| Gap | Problem for Precept |
|-----|-------------------|
| **Closed code-generation model** | Units are code-generated into separate C# types at build time. Adding a unit requires regenerating the library. Not extensible at runtime. |
| **No UCUM codes** | Units use internal enum identifiers (`LengthUnit.Meter`), not standardized codes. No UCUM-interop without a mapping layer. |
| **One type per quantity dimension** | `Length`, `Mass`, `Volume` are all separate types. A generic `Quantity` that holds "any quantity with its unit" doesn't exist cleanly. |
| **No compositional grammar** | Can't express arbitrary compound units (`kg.m/s2`) — only predefined ones. |
| **No database/catalog architecture** | All knowledge is compiled into static C# code. No updateability story. |
| **Conversion is internal** | Conversion factors are internal constants, not inspectable metadata. |

### The NodaTime Analogy

`System.DateTime` failed because it conflated instant, local date, local time, and zoned time into one type with no explicit calendar or timezone model. NodaTime built a correct type system from scratch.

`UnitsNet` fails similarly: it conflates the *code system*, the *unit identity*, the *dimensional model*, and the *conversion engine* into a single monolithic code-generated structure with no standard identity layer and no separation of concerns.

**Conclusion: Precept must build its own unit type system.** No existing .NET library provides UCUM-based identity, database-driven metadata, catalog-driven architecture, or the generic `Quantity` shape Precept needs. The investment is justified for the same structural reasons NodaTime was justified.

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
/// Value-semantics, immutable, interned from the unit database.
public sealed class Unit : IEquatable<Unit>
{
    public string Code { get; }           // UCUM code: "kg", "m/s2", "[lb_av]"
    public string Name { get; }           // Human name: "kilogram", "meter per second squared"
    public Dimension Dimension { get; }   // Dimensional category
    public UnitTier Tier { get; }         // Discovery tier (1/2/3/derived)
    
    // Equality by UCUM code (canonical form)
    public bool Equals(Unit? other) => Code == other?.Code;
    public override int GetHashCode() => Code.GetHashCode();
}

/// Dimensional category for dimensional analysis.
/// A product of base dimension exponents.
public readonly record struct Dimension(
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

public enum UnitTier { Common, Extended, Full, Derived }
```

### Why `sealed class` Not `struct` or `record struct`

- **Interned identity** — Units are resolved from the database and interned. Two lookups of `"kg"` return the same object instance. This enables reference equality as a fast path.
- **Nullable** — `Unit?` represents "no unit" cleanly (dimensionless quantities in some contexts).
- **Extensible metadata** — future fields (symbol, print format, conversion factor to base) don't break binary layout.
- **Not `record`** — records imply structural equality with all fields. Unit equality is strictly by `Code`. Using `class` with explicit `IEquatable<Unit>` makes the contract clear.

### Quantity

```csharp
/// A measured quantity: an amount with its unit.
public readonly record struct Quantity(decimal Amount, Unit Unit)
{
    // Display
    public override string ToString() => $"{Amount} {Unit.Code}";
}
```

- `record struct` — value semantics, cheap to copy, stack-allocable.
- `decimal` for Amount — matches Precept's `decimal` type semantics (exact representation, no floating-point surprise).
- `Unit` is the reference to the interned unit instance.

### Money — Separate from Unit System

```csharp
/// A monetary amount with its currency.
public readonly record struct Money(decimal Amount, string Currency)
{
    // Currency is ISO 4217 code: "USD", "EUR", "GBP"
    public override string ToString() => $"{Amount} {Currency}";
}
```

**Currency stays as `string`, NOT part of the unit system.** Reasoning:

1. UCUM does not define currencies. Currency is not a unit of measure — it's a medium of exchange with no dimensional analysis.
2. ISO 4217 is the correct standard for currency codes. It's a flat code list, not a compositional grammar.
3. Precept already decided `currency` fields are `string` (ISO 4217). `Money.Currency` is consistent.
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
    Unit? ConstrainedUnit,    // null = any unit allowed
    Dimension? ConstrainedDimension);  // null = any dimension
```

In the DSL, a field declaration like `quantity in 'kg'` constrains to a specific unit; `quantity of 'mass'` constrains to a dimension; bare `quantity` allows any unit.

---

## 6. Shane's Open Questions for Resolution

### OQ-3a: UCUM as Foundation Standard

**Shane decides:** Confirm UCUM as the unit identity standard for Precept's unit system.

- **Option A (recommended):** Yes — UCUM codes are the canonical unit identity. Full grammar support, tiered discovery.
- **Option B:** No — define a Precept-proprietary code system. (Frank advises against: reinventing a solved problem with interop cost.)

### OQ-3b: Scope — Full Grammar + Tiered Discovery

**Shane decides:** Accept the tiered model (all UCUM valid, discovery curated) vs. hard subset (reject unknown codes).

- **Option A (recommended):** Hybrid — full UCUM grammar accepted, Tier 1 (~150 atoms) surfaced proactively. No code is ever "invalid" if it's grammatically correct UCUM.
- **Option B:** Hard subset — only Tier 1 units are valid. External codes outside the subset are rejected. Simpler but creates interop friction.
- **Option C:** Everything equal — all 2,600 atoms surfaced with equal weight in tooling. Noisy but zero curation cost.

### OQ-3c: Database-Backed Architecture

**Shane decides:** Confirm database-backed (NodaTime-style data file) vs. code-backed (static members) vs. hybrid.

- **Option A (recommended):** Pure database — unit metadata is an embedded data resource, updateable independently. No static `Units.X` in the library.
- **Option B:** Hybrid — database + well-known static convenience members for Tier 1.
- **Option C:** Code-backed — all units as static fields. (Frank advises against: violates catalog-driven architecture.)

### OQ-3d: Currency Separation

**Shane decides:** Confirm that `Money.Currency` remains `string` (ISO 4217) and is NOT part of the unit type system.

- **Option A (recommended):** Yes — currency is separate. `Money` is its own type with `string Currency`. No unit system involvement.
- **Option B:** Unify — currency becomes a unit in the unit system with `Dimension.Currency`. (Frank advises against: currencies aren't units, conversion is temporal.)

### OQ-3e: Quantity Shape

**Shane decides:** Confirm `Quantity` as `readonly record struct { decimal Amount, Unit Unit }`.

- **Option A (recommended):** As proposed — `record struct`, `decimal` amount, reference to interned `Unit`.
- **Option B:** `Amount` as `double` instead of `decimal`. (Frank advises against: Precept uses `decimal` semantics throughout; floating-point would be inconsistent.)

### OQ-3f: DSL Constraint Granularity for Quantity Fields

**Shane decides:** What constraint levels does the DSL expose for quantity fields?

- **Option A:** Three levels: `quantity` (any), `quantity of 'mass'` (dimension-constrained), `quantity in 'kg'` (unit-constrained).
- **Option B:** Two levels: `quantity` (any) and `quantity in 'kg'` (unit-constrained only). Dimension constraints derived from unit.
- **Option C:** One level: `quantity` only. Constraint enforcement is via rules/guards, not field declaration.

This is a language surface decision that affects the type checker and DSL grammar. Frank leans toward Option A for maximum expressiveness, but it's a language design call.

### OQ-3g: Unit Catalog Shipping Mechanism

**Shane decides:** How is the UCUM data shipped?

- **Option A (recommended):** Embedded resource in the Precept NuGet package. Updated with library releases. Sufficient given UCUM's multi-year revision cadence.
- **Option B:** Separate NuGet package (like `NodaTime.Tzdb`). Allows data updates without library updates. More complex packaging.
- **Option C:** External file loaded at runtime. Maximum flexibility but operational complexity for consumers.

Given UCUM updates every ~5 years (vs. tzdb's multiple times per year), Option A is pragmatic. If update cadence increases, promote to Option B later.

---

## Summary

Precept should build its own unit type system using UCUM as the identity foundation — the same pattern NodaTime used with IANA tzdb. The architecture is database-backed, catalog-driven, with a compositional grammar parser for derived expressions. Currency remains separate. The investment is justified because no existing .NET library provides the right combination of standard identity, metadata architecture, and generic quantity representation.

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
| `Quantity` | `readonly record struct(decimal Amount, Unit Unit)` | A measured amount + its unit — the *value* of a quantity field |

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

### 2. Allocation and GC Pressure

#### Expression evaluation hotpath

The Precept evaluator runs inside every `Fire`, `Update`, and `Create` call. It evaluates constraint expressions, guard conditions, and computed field formulas. For a precept with quantity fields, the evaluator processes nodes like:

```
weight / volume > threshold_density
total_cost = unit_price * quantity_kg
shipment_mass > min_kg AND shipment_mass < max_kg
```

Each arithmetic or comparison operation on quantities produces an intermediate `Quantity`. These are **the shortest-lived objects in the entire runtime** — typically consumed by the next AST node, never surviving past the expression evaluation frame.

| Shape | Intermediate per expression | GC implication |
|-------|---------------------------|----------------|
| `sealed class` | 1 heap allocation per intermediate | Escapes to heap; collected in Gen0 sweep |
| `readonly record struct` | 0 heap allocations for temporaries | Stack-resident; reclaimed at frame exit |

For a `Fire` call evaluating 6 rules across 3 quantity fields, the class approach may produce 20–40 short-lived heap objects per call. In high-throughput scenarios (bulk validation, batch processing), this becomes measurable Gen0 pressure. With struct: **zero allocations for intermediate values.**

#### Stored values vs. scratch values

There is a critical distinction:

- **Scratch** — intermediate results during expression evaluation. Extremely short-lived. Struct dominates.
- **Stored** — the value sitting in a `Version` field slot. Long-lived. But the stored representation is `PreceptValue` (the internal sealed class hierarchy), not `Quantity`. The `Quantity` struct is only **materialized at the API boundary** via `Get<Quantity>()`.

The design already achieves a natural dual-shape without engineering it artificially:

```
┌─────────────────────────────────────────────────────────────────┐
│  Internal runtime                   │  Public API boundary       │
│                                     │                            │
│  PreceptValue (sealed class)        │  Quantity             │
│  ← lives in slot arrays             │  ← materialized on Get<T>  │
│  ← GC-tracked, reference-shared     │  ← struct, returned by val │
│  ← correct for long-lived storage   │  ← no allocation on read   │
└─────────────────────────────────────────────────────────────────┘
```

The `Quantity` struct is the right shape for the boundary type. The `PreceptValue` class is the right shape for the storage type. There is no tension — they serve different phases.

---

### 3. Boxing Hazards

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
| `sealed class` (subtype) | Internal `PreceptValue` | Runtime slot storage | Entity lifetime |
| `sealed class` | `Unit` | Unit identity, interned from catalog | Catalog lifetime |

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

> **RETRACTION:** An earlier version of this section incorrectly claimed "Price is NOT a Separate Type" and stated it was structurally identical to `Money`. This contradicts the canonical design in `docs/language/business-domain-types.md`. The correction follows.

**`price` IS a first-class canonical named type** defined in `docs/language/business-domain-types.md` (§ Runtime engine changes, § Operator tables). It is structurally distinct from `Money`.

**CLR shape:**
```csharp
public readonly record struct Price(decimal Amount, string Currency, string Unit)
```

**Three-field backing:**
- `decimal Amount` — magnitude (the numeric value)
- `string Currency` — numerator currency (ISO 4217, e.g. `"USD"`)
- `string Unit` — denominator unit (UCUM, e.g. `"kg"`, `"each"`)

**Structural distinction from `Money`:** `Money` is `(decimal Amount, string Currency)` — two fields. `Price` has a third field: the denominator unit. A price is inherently a compound type — currency *per* unit. `'24.50 USD/kg'` cannot be represented by `Money` without losing the denominator.

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
    string From,         // ISO 4217 source currency: "USD"
    string To)           // ISO 4217 target currency: "EUR"
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
- **32 bytes:** `decimal + string + string` = 16 + 8 + 8 = 32 bytes (reference sizes on x64; the `string` fields hold interned ISO 4217 references, not per-instance heap allocations). Still well under the copy-cost crossover. `readonly record struct` is correct.
- **Currency pair identity:** Two rates are "for the same pair" if `From` and `To` match. The `record struct` auto-generated equality handles this correctly.

**Open questions for Shane:**

- OQ-7a: Should the DSL expose `exchangerate` as a built-in field type (like `money` and `quantity`), or is this a domain-specific composite that users declare as individual fields? Frank leans toward built-in — the structural shape is universal enough.

---

### 7.3 Percentage — Future Candidate (Deferred)

> **Status:** Deferred pending separate investigation. `docs/language/business-domain-types.md` § Explicit Exclusions explicitly states: *"Whether `percent` is a type or syntactic sugar for `number / 100` is a separate investigation."*

**Previously proposed shape:**
```csharp
/// A dimensionless ratio expressed as parts per hundred.
public readonly record struct Percentage(decimal Value)
{
    /// The decimal multiplier (e.g., 25% → 0.25)
    public decimal AsMultiplier => Value / 100m;
    
    public override string ToString() => $"{Value}%";
}
```

**Why it was considered:**

1. **Behavioral distinction:** The perennial "is 25% stored as `25` or `0.25`?" problem. Every system that stores percentages as bare decimals invites a class of bugs where someone divides by 100 twice or not at all. A dedicated type with explicit `.Value` (the human-readable number, e.g. 25) and `.AsMultiplier` (the computation form, 0.25) eliminates this class of error by making the representation unambiguous.

2. **Structural distinction (invariant):** A percentage has a natural domain. While unconstrained percentages exist (> 100% is valid for growth rates), the *representation convention* is always parts-per-hundred. The type makes this convention structural.

3. **Evaluator interaction:** `money * percentage` is a common expression pattern (tax calculation, discount application, interest). The evaluator can recognise `Money × Percentage → Money` as `Amount * Percentage.AsMultiplier` without users hand-writing the division.

**Why deferred:** The canonical design doc has explicitly excluded `percent` from the seven business-domain types. The question of whether it becomes a type, syntactic sugar, or a UCUM `%` dimensionless unit requires its own investigation separate from the main business-domain type system. This investigation should not pre-commit a recommendation that the canonical doc has explicitly left open.

**Questions for future investigation:**

- Should `Percentage` allow negative values (e.g., `-5%` for a discount)?
- Should `Percentage` participate in the unit system as UCUM's `%` dimensionless unit, or remain structurally separate (like `Money` is separate from quantity)?
- Is `percent` a type or syntactic sugar for `decimal / 100`?

---

### 7.4 DateRange — YES, a Separate Type

**Proposed shape:**
```csharp
/// An inclusive range between two dates (or open-ended).
public readonly record struct DateRange(
    DateOnly? Start,   // null = unbounded start
    DateOnly? End)     // null = unbounded end
{
    public bool Contains(DateOnly date) =>
        (Start is null || date >= Start) && (End is null || date <= End);
    
    public override string ToString() => $"[{Start?.ToString() ?? "∞"}, {End?.ToString() ?? "∞"}]";
}
```

> **CLR type note:** The public API surface uses `DateOnly` (per `docs/working/runtime-api-public-surface-spec.md` §3.4 which maps `date` → `DateOnly`). The internal runtime uses NodaTime `LocalDate`; the dual-shape boundary materializes `DateOnly` for consumers.

**Why this earns its own type:**

1. **Structural distinction:** Two dates forming a bounded interval. This is not "a date" — it's a *range*. Different shape, different fields.
2. **Behavioral distinction:** The fundamental operation is *containment* — "is this date within the range?" This is used pervasively in guards and constraints (effective dates, eligibility windows, validity periods).
3. **Invariant enforcement:** `Start <= End` (when both are non-null) is a type-level invariant that bare date fields cannot express without guards.

**Design note:** Public API uses `DateOnly`; internal runtime uses NodaTime `LocalDate`. The dual-shape model materializes `DateOnly` at the API boundary, consistent with the CLR type mapping in `runtime-api-public-surface-spec.md`.

**Open questions for Shane:**

- OQ-7e: Inclusive vs. exclusive end? Convention varies by domain. Frank recommends inclusive-inclusive (`[start, end]`) as the default with a separate `DateInterval` (exclusive end) only if demanded.
- OQ-7f: Should there be a parallel `DateTimeRange` for `Instant`-bounded intervals? Frank leans yes for completeness, but it can wait for demand.

---

### 7.5 Candidates Considered and REJECTED

These do NOT earn first-class types. Rationale for each:

#### Ratio (generic)

**Would be:** `readonly record struct Ratio(decimal Numerator, decimal Denominator)`

**Rejected because:** A ratio is just a decimal value. The separate numerator/denominator representation only matters if you need to preserve the original fraction for display (e.g., "3/4" instead of "0.75"). That's a formatting concern, not a type-system concern. The evaluator operates on computed values. If a domain needs a displayed fraction, that's a string field alongside the computed decimal.

#### Range\<T\> (generic bounded interval)

**Would be:** `readonly record struct Range<T>(T Lower, T Upper) where T : IComparable<T>`

**Rejected for now because:** Generic range is an attractive abstraction but introduces generic type parameters into the Precept value-type surface. The evaluator dispatches on concrete types, not open generics. `DateRange` is the one range type with enough business ubiquity to ship. If `QuantityRange` or `MoneyRange` demand emerges, they can be added as concrete types — not as generic specializations.

#### Weight / Volume / Length (domain-specific quantity subtypes)

**Would be:** `readonly record struct Weight(decimal Amount, Unit Unit)` where `Unit.Dimension == Mass`

**Rejected because:** This is already handled by `Quantity` with a dimension constraint. The DSL declaration `quantity of 'mass'` constrains to the mass dimension — no need for a separate CLR type per dimension. The type system is `Quantity`; the constraint system narrows it.

#### Duration

**Already covered:** The earlier decision locked `duration` → NodaTime `Duration`. No new type needed.

#### Address / Email / PhoneNumber (formatted string types)

**Rejected because:** These are validation patterns on `string`, not structural value types. Precept's constraint system (`validate` declarations) handles format validation. Minting a CLR type for every validated string format violates the principle that Precept's type surface is finite and structural.

#### CompoundMoney (multi-currency basket)

**Would be:** A collection of `Money` entries summing a position across currencies.

**Rejected because:** This is a *collection*, not a value type. It's `PreceptList<Money>` — handled by the collection system, not the scalar type system.

---

### 7.6 Summary Table

| Candidate | Verdict | CLR Type | Shape | Rationale |
|-----------|---------|----------|-------|-----------|
| **Price** | ✅ Canonical named type | `Price` | `readonly record struct(decimal Amount, string Currency, string Unit)` | Three-field backing; `price × quantity → money` dimensional cancellation. Defined in `business-domain-types.md`. |
| **ExchangeRate** | ✅ Canonical named type | `ExchangeRate` | `readonly record struct(decimal Amount, string From, string To)` | Structural + behavioral distinction. Three fields, non-monetary arithmetic. Implicit `positive` (D16 Corollary 2). |
| **Percentage** | ⏳ Deferred | — | — | Separate investigation per `business-domain-types.md` § Explicit Exclusions. Not confirmed as a type. |
| **DateRange** | ✅ New type | `DateRange` | `readonly record struct(DateOnly? Start, DateOnly? End)` | Structural (interval), behavioral (containment), invariant-enforced. |
| **currency** | DSL type | `string` | CLR backing is `string` (ISO 4217) | Not a new struct — string-backed identity type in the DSL. |
| **unitofmeasure** | DSL type | `string` | CLR backing is `string` (UCUM code) | Not a new struct — string-backed identity type in the DSL. |
| **dimension** | DSL type | `string` | CLR backing is `string` (dimension name) | Not a new struct — string-backed identity type in the DSL. |
| **Ratio** | ❌ Rejected | — | — | A decimal. Fraction display is formatting, not type structure. |
| **Range\<T\>** | ❌ Rejected | — | — | Generic dispatch incompatible with evaluator. Concrete types only. |
| **Weight/Volume/etc.** | ❌ Rejected | — | — | Handled by `Quantity` + dimension constraint. |
| **Address/Email/etc.** | ❌ Rejected | — | — | Validation patterns on `string`, not structural types. |
| **CompoundMoney** | ❌ Rejected | — | — | Collection concern (`PreceptList<Money>`), not scalar type. |

---

### 7.7 Doc Scoping Note

This document is currently titled "Unit Type System Investigation." With the addition of `ExchangeRate`, `Percentage`, and `DateRange` — none of which are unit-system types — the scope has outgrown the title.

**Recommendation:** Rename to **"Precept Value Types — Investigation and Design"** once Shane decides on the candidates above. The unit system (UCUM, `Quantity`, `Unit`) is one section; the broader value-type surface (`Money`, `ExchangeRate`, `Percentage`, `DateRange`) is another. Both belong in the same document because they share the same design principles (struct vs. class, dual-shape model, catalog-driven metadata), but the title should reflect the actual scope.
