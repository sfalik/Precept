# Precept Value Types — CLR Mapping Investigation

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04 (last synced 2026-05-05)  
**Status:** Investigation — core verdicts locked; open questions remain for Shane  
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

**Open questions (not yet resolved by Shane):**
- **OQ-CUR-1:** Include curated `symbol` supplement? Frank recommends Option A (include). Shane has not responded. → **Open.**
- **OQ-CUR-4:** Embedded resource vs. separate data package? Frank recommends Option A (embedded, v1). Shane has not responded. → **Open.**

**Presumed agreed given the locked design:**
- **OQ-CUR-2:** Upgrade `Money.Currency`, `Price.Currency`, `ExchangeRate.From`/`.To` from `string` to `Currency`. No breaking change (types not shipped). Structural consistency. Presumed agreed.

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

**Amendment cadence:** ISO 4217 amendments are published several times per year as countries rename currencies or join/leave currency unions. Actual code *additions* are rare — they happen only when a country adopts a new currency, which is a multi-year event. Most amendments are country-name corrections. This is more frequent than UCUM (years between revisions) but the functional impact of being one amendment behind is negligible. The shipping mechanism is flagged as OQ-CUR-4.

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
    public string Symbol      { get; }   // Display symbol: "$", "€", "¥" (curated supplement — OQ-CUR-1)
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
        new FixedReturnAccessor("symbol",       TypeKind.String,  "Display symbol (e.g., '$', '€') — curated supplement, not in ISO 4217 spec (OQ-CUR-1)"),
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

**Current:**
```csharp
public readonly record struct Money(decimal Amount, string Currency)
public readonly record struct Price(decimal Amount, string Currency, string Unit)
public readonly record struct ExchangeRate(decimal Amount, string From, string To)
```

**Upgraded:**
```csharp
public readonly record struct Money(decimal Amount, Currency Currency)
public readonly record struct Price(decimal Amount, Currency Currency, string Unit)
public readonly record struct ExchangeRate(decimal Amount, Currency From, Currency To)
```

Size impact on x64: `string` (8 bytes managed reference) and `Currency` (8 bytes managed reference) have the same slot size. No change to struct size.

**Frank's recommendation: upgrade.** Reasons:
1. **No breaking-change cost** — none of these CLR types are shipped yet
2. **Structural consistency** — the catalog model says `money.currency` returns `TypeKind.Currency`; the CLR type should match
3. **Direct `.MinorUnit` access** — `Money.Currency.MinorUnit` eliminates a re-lookup for D10 enforcement in the evaluator
4. **Interning benefit** — the `Currency` references in `Money`, `Price`, and `ExchangeRate` instances all point to the same ~180 interned objects; no per-instance metadata duplication

This is flagged as **OQ-CUR-2** for Shane's decision.

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

### 8.10 Open Questions

**OQ-CUR-1: Include `symbol`?** — ⏳ **Open** (Shane has not responded)

`symbol` (e.g., `$`, `€`, `¥`) is **not part of the ISO 4217 specification**. It is colloquially associated but the standard does not define symbols. Multiple currencies share the dollar sign (`$`): USD, CAD, AUD, NZD, HKD, etc.

- **Option A (recommended by Frank):** Include `symbol` as a curated supplement maintained in the same embedded resource as the ISO 4217 data. Use the unambiguous symbol where one exists (e.g., `€` for EUR, `£` for GBP), and the alpha code as the fallback where the symbol is ambiguous (e.g., `US$` for USD to distinguish from `CA$`, `A$`). Frank leans toward inclusion: every invoice renderer needs it; requiring an out-of-band lookup is a catalog-architecture violation.
- **Option B:** Exclude `symbol` entirely — expose only the four ISO-4217-defined fields. Simpler, but forces consumers to maintain a parallel metadata structure.

**OQ-CUR-2: Upgrade `Money.Currency`, `Price.Currency`, `ExchangeRate.From`/`.To` from `string` to `Currency`?** — ✅ **Presumed agreed** (no breaking-change cost; structural consistency with locked design)

Frank recommends upgrading (§8.8). Given that the `sealed class Currency` design is locked and none of these CLR types are shipped, OQ-CUR-2 is treated as agreed pending explicit override from Shane.

- **Option A (agreed):** All three upgraded — `Money(decimal Amount, Currency Currency)`, etc. Structural consistency, no breaking change, direct `.MinorUnit` access.
- **Option B:** Keep `string` — `currency` fields return `Currency` from `Get<Currency>()`, but the composite type fields (`Money.Currency`, etc.) stay `string`.

**OQ-CUR-3: `Get<Currency>()` vs. `Get<string>()` for `currency`-typed fields?** — ⏳ **Open**

When `Get<T>()` is called on a `currency`-typed field with `T = Currency`, the runtime returns the `Currency` object from the catalog. Should `Get<string>()` also be supported as a fallback returning the alpha code?

- **Option A (recommended by Frank):** Both work. `Get<Currency>()` is the primary typed API. `Get<string>()` returns the alpha code string for consumers who only need the code. The `TypeRuntime<Currency>` for `currency` resolves via `CurrencyCatalog.Default.Get(alphaCode)`.
- **Option B:** Only `Get<Currency>()`. `Get<string>()` on a `currency` field is a type mismatch at call time.

**OQ-CUR-4: Shipping mechanism — embedded resource vs. separate package?** — ⏳ **Open** (Shane has not responded)

ISO 4217 amendments are more frequent than UCUM revisions, though actual code changes are rare.

- **Option A (recommended by Frank for v1):** Embedded resource in the Precept NuGet package. Code additions happen only when countries change currencies (multi-year cadence). Name corrections don't affect runtime behavior. If amendment drift creates real friction, promote to Option B.
- **Option B:** Separate `Precept.Currencies` data package (analogous to `NodaTime.Tzdb`). Data updates without library releases. More packaging complexity, justified only if amendment frequency creates meaningful drift.

---

### 8.11 Summary

**Status: ✅ Locked** — Shane selected frank-114 on 2026-05-04. OQ-CUR-1 and OQ-CUR-4 remain open pending Shane's input.

| Design Point | Decision | Status | Rationale |
|---|---|---|---|
| CLR type name | `Currency` | ✅ Locked | Parallel to `Unit` — same naming convention |
| CLR shape | `sealed class` | ✅ Locked | Interning, extensible metadata, catalog lifetime — identical to `Unit` reasoning |
| Fields | `AlphaCode`, `NumericCode`, `Name`, `Symbol`*, `MinorUnit` | ✅ Locked (Symbol pending OQ-CUR-1) | ISO 4217 defined + curated `Symbol` supplement |
| Catalog | `CurrencyCatalog` | ✅ Locked | Embedded resource, `FrozenDictionary` backing, analogous to `UnitCatalog` |
| Tiers | None | ✅ Locked | ~180 codes, flat list — no tiering needed (unlike UCUM) |
| Grammar parser | None | ✅ Locked | ISO 4217 is flat — no compositional grammar |
| D10 grounding | `CurrencyCatalog.Default.Get(code).MinorUnit` | ✅ Locked | Removes hardcoded table from type checker |
| DSL declaration | Unchanged — `field X as currency` | ✅ Locked | Identity types don't support `in`/`of` |
| New accessors | `.name`, `.symbol`, `.minorUnit`, `.numericCode` | ✅ Locked | Derived from catalog at evaluation time; zero storage cost |
| Serialization | Unchanged — `"USD"` (alpha code string) | ✅ Locked | Value is the code; metadata is always derived |
| `Money.Currency` type | Upgrade to `Currency` (OQ-CUR-2) | ✅ Presumed agreed | Structural consistency; direct `.MinorUnit` access; no breaking change |
| `symbol` inclusion | OQ-CUR-1 — Frank recommends Option A | ⏳ Open | Shane has not responded |
| Shipping mechanism | OQ-CUR-4 — Frank recommends Option A (embedded) | ⏳ Open | Shane has not responded |

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

### 9.4 The NodaTime Analogy Correction

The NodaTime analogy (§4) justified building a separate type system. It was wrongly extended to mean "types carry computation like NodaTime types do." The distinction: NodaTime carries computation because it has no runtime — its types ARE the runtime. Precept has a runtime. The analogy justifies the separate assembly; it does not require computation on the types.

### 9.5 Implications for CLR Type Definitions

All value type CLR shapes in this document (§5 `Quantity`, §7.1 `Price`, §7.2 `ExchangeRate`, §8.3 `Currency`) are confirmed as pure data records. They expose their fields, equality, construction, and formatting — nothing else. Any arithmetic example in this document (e.g., `price × quantity → money`) describes evaluator behavior, not CLR operator overloads.

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

## 11. Type Library Assembly — `Precept.Types` (Recommended)

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04  
**Status:** Recommendation recorded — Shane confirmation needed.

### 11.1 The Problem

Consumers who want `Money`, `Currency`, `UnitOfMeasure`, etc. in their DTO layer must not pull in the full Precept compiler pipeline. The NodaTime analogy taken seriously implies a separate package, not just separate files.

### 11.2 Dependency Graph

```
Precept.Types (no Precept dependencies)
     ↑
Precept (evaluator assembly — references Precept.Types)
     ↑
Consumer code (may reference either)
```

### 11.3 Assembly Contents

| Assembly | Contents |
|----------|----------|
| **`Precept.Types`** | `Currency`, `CurrencyCatalog`, `UnitOfMeasure`, `MeasureDimension`, `Money`, `Price`, `ExchangeRate`, `Quantity`, `KeyedElement<TValue, TKey>`, `IUnitConversionSource`, embedded ISO 4217 resource |
| **`Precept`** (evaluator) | `Unit` (sealed class with Tier + DimensionVector + ConversionFactors), `UnitCatalog`, `DimensionCatalog`, all pipeline stages, embedded UCUM resource |

### 11.4 Why the Split

- `Precept.Types` has zero Precept dependencies — it's a pure type library.
- Consumer DTOs and domain models can reference `Money`, `Quantity`, etc. without pulling in the lexer, parser, type checker, graph analyzer, or evaluator.
- The evaluator references `Precept.Types` and adds the enriched catalog entities (`Unit`, `UnitCatalog`) that carry evaluator-internal metadata (tiers, conversion factors, SI exponent vectors).

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
| Internal runtime slots | `PreceptValue` subtype hierarchy | `sealed class` | GC-tracked, reference-shared, correct for long-lived storage |
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

---

## 14. Operator Overload Analysis (Superseded — Historical Record)

**By:** Frank (Lead Architect)  
**Date:** 2026-05-04  
**Status:** ❌ **Superseded** by §9 (Evaluator-Only Computation). Retained for historical context only.

### 14.1 Summary of Analysis

Before the evaluator-only model was locked, Option A explored C# operator overloads on the CLR types. The analysis identified three structural problems with naïve operator overloads:

1. **Injection parameter problem:** operators cannot take extra parameters. D8 auto-conversion requires `IUnitConversionSource` — impossible to pass via `operator+`.
2. **Return-type ambiguity:** `Money / Money` → `decimal` (ratio) vs. `Money / Quantity` → `Price`. Same operand types, different return types — C# cannot express this.
3. **Commutative cross-type operators:** `Price * Quantity` and `Quantity * Price` must both work. Declaring the operator on both types creates coupling.

### 14.2 Per-Type Surfaces Analyzed (Now Moot)

- **Money:** additive (`+`, `-`), scaling (`* decimal`, `/ decimal`), ratio (`Money / Money` → `decimal`), comparisons, named `DeriveRate`/`DivideBy`
- **ExchangeRate:** currency conversion (`operator*`), scaling, named `Apply`/`Invert`
- **Price:** dimensional cancellation (`price * quantity → money`), scaling, additive
- **Quantity:** same-unit operators, D8 as named method, compound division

**All of this is now historical.** Under the locked evaluator-only model (§9), CLR types carry NO operators. Computation lives in named executor modules. This section exists to document WHY operators were rejected, not to prescribe them.
