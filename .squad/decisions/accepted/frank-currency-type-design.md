# Currency Type Design

**By:** Frank  
**Date:** 2026-05-04T19:10:08-04:00  
**Status:** ✅ Locked — Shane selected frank-114 on 2026-05-04. Moved to `accepted/`. OQ-CUR-1 and OQ-CUR-4 remain open.  
**Context:** Shane request: `currency` value type backed by ISO 4217, following the same pattern as `unit`  
**Investigation doc:** `docs/working/precept-value-types-investigation.md` §8  

---

## Summary

`currency` is currently a validated-string DSL type: CLR backing is `string`, no accessors in the catalog entry, D10's implicit `maxplaces` reads from a hardcoded table in the type checker. This design elevates `currency` to a first-class catalog type following the `unit`/`UnitCatalog` pattern exactly — a `sealed class Currency` backed by a `CurrencyCatalog` that loads ISO 4217 from an embedded resource.

**Prior draft note:** An earlier inbox pass proposed `readonly record struct CurrencyCode` with well-known static constants. This design supersedes it. See §Alternatives Rejected for the detailed rejection rationale.

---

## The `unit` Pattern — What "Following It" Means

The `unit` design established in `docs/working/precept-value-types-investigation.md` §5:

| Element | `unit` | `currency` (this design) |
|---------|--------|--------------------------|
| DSL keyword | `unitofmeasure` | `currency` |
| CLR identity type | `Unit` (sealed class) | `Currency` (sealed class) |
| Catalog | `UnitCatalog` | `CurrencyCatalog` |
| Standard | UCUM | ISO 4217 |
| Backing | Embedded resource | Embedded resource |
| Internal lookup | `FrozenDictionary<string, Unit>` | `FrozenDictionary<string, Currency>` |
| Well-known constants | None — pure database model | None — pure database model |

The architecture is identical. The implementation is simpler because ISO 4217 is a simpler standard (no tiers, no grammar).

---

## CLR Type: `Currency` Sealed Class

```csharp
/// An ISO 4217 currency, identified by its alphabetic code.
/// Interned from the CurrencyCatalog. Identity by code.
public sealed class Currency : IEquatable<Currency>
{
    public string AlphaCode   { get; }   // "USD", "EUR", "JPY"
    public int    NumericCode { get; }   // 840, 978, 392
    public string Name        { get; }   // "US Dollar", "Euro", "Yen"
    public string Symbol      { get; }   // "$", "€", "¥" — curated supplement (OQ-CUR-1)
    public int    MinorUnit   { get; }   // 2 (USD), 0 (JPY), 3 (KWD)

    // Equality by alphabetic code
    public bool Equals(Currency? other) => AlphaCode == other?.AlphaCode;
    public override int GetHashCode()   => AlphaCode.GetHashCode();
    public override string ToString()   => AlphaCode;
}
```

**Why `sealed class`, not `readonly record struct`:**

| Consideration | Why sealed class wins |
|---|---|
| **Interning** | `CurrencyCatalog.Get("USD")` always returns the same instance. `ReferenceEquals` is O(1). Interning requires heap identity — structs cannot be interned. |
| **Catalog lifetime** | ~180 instances live for the application lifetime. GC overhead is irrelevant. |
| **Extensible metadata** | `Countries`, `IsActive`, `ReplacedBy`, `InactiveSince` can be added without breaking binary layout. |
| **Identity, not a value** | "USD" is an entity. `Money` is the value that carries a `Currency`. Value types are for values; reference types are for entities. |
| **Nullable** | `Currency?` means "no currency" cleanly via reference null. |

The `readonly record struct` option was previously explored (prior inbox draft). It was rejected because: (a) the `unit` precedent is sealed class for the same structural reasons; (b) interning from a small fixed catalog is the correct model for identity types; (c) future metadata fields need layout flexibility. Details in §Alternatives Rejected.

---

## `CurrencyCatalog`

```csharp
public sealed class CurrencyCatalog
{
    // Singleton loaded from embedded ISO 4217 resource
    public static CurrencyCatalog Default { get; }

    // Alpha code lookup (primary key)
    public Currency Get(string alphaCode);
    public bool TryGet(string alphaCode, out Currency currency);

    // Numeric code lookup
    public Currency GetByNumericCode(int numericCode);
    public bool TryGetByNumericCode(int numericCode, out Currency currency);

    // Validation
    public bool IsValid(string alphaCode);

    // Enumeration
    public IReadOnlyList<Currency> All { get; }

    public string DataVersion { get; }  // ISO 4217 amendment identifier
}
```

Internal backing:
```csharp
private readonly FrozenDictionary<string, Currency> _byAlpha;    // "USD" → Currency
private readonly FrozenDictionary<int,    Currency> _byNumeric;  // 840 → Currency
```

Loaded once from an embedded ISO 4217 resource file. `FrozenDictionary` is correct: ~180 entries, read-only after initialization, full table fits in L1 cache.

**No well-known static constants.** Following the pure database model established for `UnitCatalog`. `CurrencyCatalog.Default.Get("USD")` is the entry point — the same way `UnitCatalog.Default.Get("kg")` works. Consumers can define their own `static Currency USD => CurrencyCatalog.Default.Get("USD")` in application code if they want convenience accessors. The library doesn't bless a subset.

---

## `TypeKind.Currency` Catalog Entry — Before and After

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
        new FixedReturnAccessor("symbol",       TypeKind.String,  "Display symbol (e.g., '$', '€') — curated, not ISO 4217 (OQ-CUR-1)"),
        new FixedReturnAccessor("minorUnit",    TypeKind.Integer, "Decimal places per ISO 4217 minor unit (2 for USD, 0 for JPY, 3 for KWD)"),
        new FixedReturnAccessor("numericCode",  TypeKind.Integer, "ISO 4217 numeric code (840 for USD, 978 for EUR)"),
    ],
    DisplayName: "currency",
    HoverDescription: "An ISO 4217 currency — exposes .name, .symbol, .minorUnit, and .numericCode. Carries notempty implicitly.",
    UsageExample: "field BaseCurrency as currency default 'USD'"
),
```

No `QualifierShape` added. `currency` does not support `in` or `of`. The serialized form is unchanged: `"USD"`. Accessors are derived from `CurrencyCatalog` at evaluation time; zero storage cost.

---

## DSL Surface

**Declaration unchanged:**
```precept
field BaseCurrency    as currency default 'USD'
field InvoiceCurrency as currency optional
```

**New accessor patterns in guards and rules:**
```precept
# Precision check using minor unit from the catalog
rule Payment.currency.minorUnit >= 0
  because "currency must have valid precision"

# Display formatting using symbol
from Active on GenerateReceipt
  set ReceiptLine = Amount + ' ' + BaseCurrency.symbol + ' (' + BaseCurrency.name + ')'

# External system integration via numeric code
when BaseCurrency.numericCode == 978   # EUR = 978
  set Region = 'Eurozone'
```

**Validation unchanged:**
- `'USD'` at compile time → `CurrencyCatalog.Default.IsValid(code)`
- `'USDX'` → compile error: not a recognized ISO 4217 code

---

## D10 Structural Grounding

D10 (implicit `maxplaces` from ISO 4217 `MinorUnit`) currently uses a hardcoded table in the type checker. After this design:

```csharp
// Type checker D10 logic after this design:
var minorUnit = CurrencyCatalog.Default.Get(currencyCode).MinorUnit;
// Apply as implicit maxplaces constraint
```

This removes the only place where the type checker embeds per-currency knowledge rather than deriving it from the catalog. Architecturally significant.

---

## Impact on Related CLR Types (Money, Price, ExchangeRate)

With `Currency` as a CLR type, `Money`, `Price`, and `ExchangeRate` can carry `Currency` references instead of `string`:

| Field | Current | Recommended (OQ-CUR-2) |
|-------|---------|------------------------|
| `Money.Currency` | `string` | `Currency` |
| `Price.Currency` | `string` | `Currency` |
| `ExchangeRate.From` | `string` | `Currency` |
| `ExchangeRate.To` | `string` | `Currency` |

Size: no change — `string` and `Currency` are both 8-byte managed references on x64. Since none of these CLR types are shipped yet, there is no breaking-change cost. Frank recommends upgrading (OQ-CUR-2).

---

## Alternatives Rejected

### `readonly record struct CurrencyCode` (prior inbox draft)

The prior draft proposed `readonly record struct CurrencyCode` with ~20 well-known static constants (`CurrencyCode.USD`, `CurrencyCode.EUR`, etc.).

**Rejected for three reasons:**

1. **Contradicts the `unit` architectural precedent.** The investigation doc §7 explicitly chose `sealed class Unit` (not `record struct`) and documented the reasoning: interning requires heap identity, catalog lifetime means GC overhead is irrelevant, extensible metadata requires layout flexibility. `currency` has the same structural profile — an identity type, catalog-backed, ~180 instances. The same reasoning applies. Choosing a different shape for `currency` creates an architectural inconsistency between two identity types that differ only in their backing standard.

2. **Contradicts the pure database model principle.** The `unit` design §4 explicitly rejected well-known static constants on the grounds that "consumers want convenience, they can define their own `static Unit Kg => ...` in application code. The library shouldn't bless a subset." The prior draft's well-known statics (`CurrencyCode.USD`, `CurrencyCode.EUR`, etc.) violate this principle. The fact that ISO 4217 has fewer codes (180 vs 2,600+) doesn't change the architectural principle — it just means the subset is more enumerable, not that the library should curate it.

3. **Premature optimization.** The argument "copy cost is trivial for 3 characters" misses the point. The question isn't copy cost — it's architectural consistency. Reference equality, catalog interning, and extensibility are design properties, not performance properties.

**Tradeoff accepted by rejecting this alternative:** `CurrencyCatalog.Default.Get("USD")` is slightly more verbose than `CurrencyCode.USD`. This is the same tradeoff accepted for `Unit` — and accepted for the same reasons.

### Enum

Closed at compile time, violates catalog-driven architecture, forces hardcoded knowledge into code.

### Keep `string` (status quo)

Leaves D10 with a hardcoded table, prevents accessor usage in DSL, inconsistent with `UnitOfMeasure`/`Unit` pattern.

---

## Open Questions

**OQ-CUR-1:** Should `symbol` (e.g., `$`, `€`) be included as a curated supplement to ISO 4217?  
`symbol` is **not in the ISO 4217 specification** — it's colloquially associated. Multiple currencies share `$`.  
- **Option A (recommended):** Include as a curated field. Every invoice renderer needs it; out-of-band lookups violate catalog-driven architecture. Use unambiguous symbol where one exists, alpha code fallback where ambiguous.  
- **Option B:** Exclude. Simpler, but forces parallel metadata outside the catalog.

**OQ-CUR-2:** Should `Money.Currency`, `Price.Currency`, `ExchangeRate.From`/`.To` be upgraded from `string` to `Currency`?  
- **Option A (recommended):** Upgrade all three. No breaking change (types not shipped). Structural consistency. Direct `.MinorUnit` access.  
- **Option B:** Keep `string`. `currency` fields use `Get<Currency>()`; compound type fields stay `string`.

**OQ-CUR-3:** Should `Get<string>()` remain valid on `currency`-typed fields alongside `Get<Currency>()`?  
- **Option A (recommended):** Both supported. `Get<Currency>()` is primary; `Get<string>()` returns the alpha code for consumers that only need the code.  
- **Option B:** Only `Get<Currency>()`. `Get<string>()` on a `currency` field is a type mismatch.

**OQ-CUR-4:** Shipping mechanism — embedded resource vs. separate data package?  
- **Option A (recommended for v1):** Embedded resource. Code additions are multi-year-cadence rare; name amendments have no runtime impact. Revisit if amendment drift creates real friction.  
- **Option B:** Separate `Precept.Currencies` data package (analogous to `NodaTime.Tzdb`).
