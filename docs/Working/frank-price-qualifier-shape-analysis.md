# Price Qualifier Shape Analysis — Frank

**Date:** 2026-05-15  
**Author:** Frank  
**Status:** Proposal — awaiting owner review

---

## Summary

The `QualifierShape` / `QualifierSlot` type model has three concrete expressiveness gaps against the `price` qualifier contract in `docs/language/business-domain-types.md`. A minimum-surface fix requires one new `QualifierAxis` value, one new `DeclaredQualifierMeta` subtype, one new field on `QualifierShape`, and targeted updates to five downstream consumers. No consumer needs to be rebuilt; each change is isolated.

---

## 1. The Current Model

`price` uses `QS_CurrencyAndDimension`:

```csharp
private static readonly QualifierShape QS_CurrencyAndDimension = new(
[
    new QualifierSlot(TokenKind.In,  QualifierAxis.Currency),
    new QualifierSlot(TokenKind.Of,  QualifierAxis.Dimension),
], InOfExclusive: false);
```

`QualifierSlot` is a `sealed record(TokenKind Preposition, QualifierAxis Axis)` — a 1:1 mapping of one preposition to one fixed axis. `QualifierShape` is a `sealed record(IReadOnlyList<QualifierSlot> Slots, bool InOfExclusive = false)`.

`MapLiteralQualifier` in the type checker dispatches on `qualifier.Axis` and returns exactly one `DeclaredQualifierMeta?`.

---

## 2. The Expressiveness Gaps — Exact

### Gap 1: `price in 'kg'` — `in` cannot carry a Unit axis

The slot `(In, QualifierAxis.Currency)` hardcodes `in` as always pinning the Currency axis. The type checker routes to `MapCurrencyQualifier("kg", ...)`, which checks `CurrencyCatalog.All.ContainsKey("kg")`, fails, and emits `InvalidCurrencyCode`. There is no path through the slot model that lets `in` route to `MapUnitQualifier` for `price`.

**The model cannot express:** "for `price`, `in` may carry either a currency code or a unit code, disambiguated by registry lookup at check time."

### Gap 2: `price in 'USD/each'` — one `in` value must fill two axes simultaneously

`'USD/each'` should produce both a `Currency("USD")` and a `Unit("each")` qualifier from a single `in` typed constant. The current model has `MapLiteralQualifier` returning `DeclaredQualifierMeta?` — one nullable result per qualifier. There is no mechanism to emit two `DeclaredQualifierMeta` records from a single parsed qualifier, and no `DeclaredQualifierMeta` subtype that carries both a currency and a unit axis together.

**The model cannot express:** "a single `in` typed constant that pins both the currency and unit axes at once."

### Gap 3: `in` + `of` coexistence is unconditional — it should be conditional

`InOfExclusive: false` permits any coexistence of `in` and `of` on `price`. The design says only one coexistence case is valid: `in 'USD' of 'mass'` (currency-only `in` + dimension). The following should be errors:
- `price in 'kg' of 'mass'` — unit `in` + dimension is redundant/nonsensical  
- `price in 'USD/each' of 'mass'` — compound `in` already pins the denominator

`InOfExclusive: false` is a binary flag; there is no catalog-level mechanism to express "coexistence is valid only when `in` resolves to a Currency, not a Unit or compound value."

**The model cannot express:** "the `of` slot is valid only when `in` resolves to a currency-only value."

---

## 3. The Proposed Solution

### Principle

This is a catalog-system problem. The fix must live in the metadata shape that drives consumers — not in consumers hardcoding price-specific behavior. Three targeted additions: one new `QualifierAxis` value, one new `DeclaredQualifierMeta` subtype, and one new field on `QualifierShape`.

### Change 1: `QualifierAxis.PriceIn` (new enum value)

Add `PriceIn` to `QualifierAxis`:

```csharp
public enum QualifierAxis
{
    None,
    Currency,
    Unit,
    Dimension,
    FromCurrency,
    ToCurrency,
    Timezone,
    TemporalDimension,
    TemporalUnit,
    PriceIn,    // ← new: polymorphic 'in' for price — registry-disambiguated, may carry compound value
}
```

This is a price-specific axis sentinel. Its presence on a slot signals to the type checker: "this `in` value must be disambiguated at check time via registry lookup." It does NOT mean all future price-like types use `PriceIn` — it means the slot carries metadata-declared polymorphism, declared in the shape.

### Change 2: `DeclaredQualifierMeta.CompoundPrice` (new DU subtype)

Add to `DeclaredQualifierMeta.cs`:

```csharp
public sealed record CompoundPrice(
    string CurrencyCode,
    string UnitCode,
    string DimensionName,
    QualifierOrigin Origin = QualifierOrigin.Explicit,
    TokenKind? Preposition = TokenKind.In,
    ProofSatisfaction[]? ProofSatisfactions = null,
    string? SourceFieldName = null)
    : DeclaredQualifierMeta(QualifierAxis.PriceIn, Origin, Preposition, ProofSatisfactions, SourceFieldName);
```

`Axis` is set to `QualifierAxis.PriceIn` — a first-class axis value that proof engine and qualifier comparison logic can match on. Consumers that need the currency component read `CompoundPrice.CurrencyCode`; those that need the unit component read `CompoundPrice.UnitCode`. The DU shape makes each component explicit and non-nullable.

### Change 3: `OfRequiresCurrencyIn` field on `QualifierShape`

```csharp
public sealed record QualifierShape(
    IReadOnlyList<QualifierSlot> Slots,
    bool InOfExclusive = false,
    bool OfRequiresCurrencyIn = false  // ← new
);
```

When `OfRequiresCurrencyIn` is true, the type checker must verify that if both `in` and `of` are present, the `in` qualifier resolved to `DeclaredQualifierMeta.Currency` (not `Unit` or `CompoundPrice`). This is a catalog-declared constraint, not a type-identity check.

### Change 4: Update `QS_CurrencyAndDimension`

```csharp
private static readonly QualifierShape QS_CurrencyAndDimension = new(
[
    new QualifierSlot(TokenKind.In,  QualifierAxis.PriceIn),
    new QualifierSlot(TokenKind.Of,  QualifierAxis.Dimension),
], InOfExclusive: false, OfRequiresCurrencyIn: true);
```

The `InOfExclusive: false` stays — `in` and `of` may coexist. The `OfRequiresCurrencyIn: true` adds the conditional constraint. Slot `in` now carries `PriceIn` instead of `Currency`, routing the type checker to registry disambiguation.

---

## 4. Downstream Consumer Impact

### 4a. Parser — `Parser.Types.cs` (minimal)

`TryParseQualifiers` iterates `typeMeta.QualifierShape.Slots` and reads `slot.Preposition` and `slot.Axis`. It passes `slot.Axis` into `LiteralParsedQualifier` and `InterpolatedParsedQualifier` constructors.

With `PriceIn` on the slot, the parser produces `LiteralParsedQualifier(In, PriceIn, "USD/each", span)`. No structural change required — the parser is already catalog-driven and axis-agnostic. **Zero parser code changes.**

### 4b. Type Checker — `TypeChecker.cs` (primary change site)

Three targeted additions:

**i. New `MapPriceInQualifier`**

Add a case in `MapLiteralQualifier`:
```csharp
QualifierAxis.PriceIn => MapPriceInQualifier(qualifier.Value, qualifier.ValueSpan, ctx),
```

`MapPriceInQualifier` performs registry disambiguation:
1. If `CurrencyCatalog.All.ContainsKey(value)` → return `new DeclaredQualifierMeta.Currency(value)`
2. If `UcumParser.Parse(value).IsValid` → return `new DeclaredQualifierMeta.Unit(value, ...)`
3. If value contains `/` and parses as `<currency>/<unit>` → return `new DeclaredQualifierMeta.CompoundPrice(currency, unit, dimension)`
4. Otherwise → emit `InvalidPriceQualifier` diagnostic, return `null`

**ii. `MapInterpolatedQualifier` interpolated price `in` path**

Add `QualifierAxis.PriceIn` to the expectedType switch — maps to `TypeKind.Price` (the full price type) or left unresolved (interpolated compound is already handled by the proof engine as a dynamic qualifier). This is a judgment call; the minimal safe option is to return `null` for interpolated `PriceIn` (no resolution), preserving existing behavior for interpolated price fields.

**iii. `OfRequiresCurrencyIn` enforcement**

In `ExtractQualifiers`, after the existing `InOfExclusive` check, add:

```csharp
if (qualified.InnerType is SimpleTypeReference simpleInner2 &&
    simpleInner2.Type.QualifierShape?.OfRequiresCurrencyIn == true)
{
    bool hasIn  = qualified.Qualifiers.Any(q => q.Preposition == TokenKind.In);
    bool hasOf  = qualified.Qualifiers.Any(q => q.Preposition == TokenKind.Of);
    if (hasIn && hasOf)
    {
        // Check whether 'in' resolved to currency-only — find it in the builder
        var inMeta = builder.FirstOrDefault(m => m.Preposition == TokenKind.In);
        if (inMeta is not DeclaredQualifierMeta.Currency)
            ctx.Diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.InvalidQualifierCoexistence, qualified.Span));
    }
}
```

This requires a new `DiagnosticCode.InvalidQualifierCoexistence` — a one-line catalog entry. Diagnostic text: "The `of` qualifier on `price` is only valid when `in` carries a currency. Use `in 'USD' of 'mass'` — not `in 'kg' of 'mass'` or `in 'USD/each' of 'mass'`."

**What the type checker leaves unchanged:** all existing `Currency`, `Unit`, `Dimension`, `FromCurrency`, `ToCurrency`, `TemporalUnit`, `TemporalDimension` dispatch arms are not touched.

### 4c. Language Server — `CompletionHandler.cs` (minor)

`TryGetDeclarationQualifierContext` reads `qualifierSlot.Axis` to call `TryMapQualifierAxisToExpectedType`. For `QualifierAxis.PriceIn`, that function currently returns `false` (no case matches). The result: `price in '<cursor>'` produces no completions. This is a degradation but not incorrect — the completion is deferred.

**Correct minimal fix:** add `QualifierAxis.PriceIn` to return `false` explicitly (document the intent) and add a TODO for a follow-up that provides combined ISO 4217 + UCUM completions for the polymorphic case.

Alternatively, return `TypeKind.Currency` for `PriceIn` as a pragmatic first pass — it provides currency completions, which is correct for the majority use case. The unit case will not get completions, but completions for currency are not wrong.

**`RichHoverFactory.cs` line 1820**: `slots.Select(slot => slot.Axis)` is used to enumerate expected qualifier axes. With `PriceIn` in the slot, the hover factory would enumerate `PriceIn` as an axis name. This is benign unless the hover display text renders `PriceIn` as-is (ugly). The hover factory that formats axes should add a render branch: `QualifierAxis.PriceIn → "currency, unit, or 'currency/unit'"`. Currently `GetQualifierAxisName` or equivalent needs a `PriceIn` case.

**Lines 2475–2477 in `RichHoverFactory.cs`**: hover for a parsed qualifier looks up resolved `DeclaredQualifierMeta` by `qualifier.Axis`. For `PriceIn` qualifiers, the resolved meta is `Currency`, `Unit`, or `CompoundPrice` — the hover factory should use the resolved meta's axis, not `qualifier.Axis`, for the axis label. This is already how it works for unambiguous axes; the `CompoundPrice` subtype just needs a render arm.

### 4d. MCP Formatter — `CatalogFormatters.cs` (minor)

`RenderQualifierShape` formats each slot as `RenderToken(slot.Preposition) \`{slot.Axis}\``. For the `PriceIn` slot, the rendered output would be `in \`PriceIn\`` — which is correct but opaque to consumers.

**Fix**: add a rendering branch for `QualifierAxis.PriceIn`:
```csharp
private static string RenderQualifierShape(QualifierShape shape)
    => JoinOrNone(shape.Slots.Select(slot => slot.Axis == QualifierAxis.PriceIn
        ? $"{RenderToken(slot.Preposition)} `currency`, `unit`, or compound `currency/unit`"
        : $"{RenderToken(slot.Preposition)} `{slot.Axis}`"))
        + (shape.InOfExclusive ? "; `in`/`of` are mutually exclusive" : string.Empty)
        + (shape.OfRequiresCurrencyIn ? "; `of` only valid when `in` is currency-only" : string.Empty);
```

Also add rendering in the MCP qualifier-value render path (`qualifier.Axis` at line 971) for `CompoundPrice` — show both currency and unit components.

---

## 5. What Changes vs. What Does Not

### Changes (7 targeted touch points)

| File | Change |
|------|--------|
| `src/Precept/Language/Type.cs` | Add `QualifierAxis.PriceIn`; add `OfRequiresCurrencyIn: bool` to `QualifierShape` |
| `src/Precept/Language/DeclaredQualifierMeta.cs` | Add `CompoundPrice(CurrencyCode, UnitCode, DimensionName)` subtype |
| `src/Precept/Language/Types.cs` | Update `QS_CurrencyAndDimension` to use `PriceIn` slot + `OfRequiresCurrencyIn: true` |
| `src/Precept/Pipeline/TypeChecker.cs` | Add `MapPriceInQualifier`; add `OfRequiresCurrencyIn` enforcement in `ExtractQualifiers` |
| `src/Precept/Language/DiagnosticCatalog` (or equivalent) | Add `InvalidQualifierCoexistence` diagnostic code |
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | Handle `PriceIn` in `TryMapQualifierAxisToExpectedType` |
| `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` | Add `PriceIn` axis label + `CompoundPrice` render arm |
| `tools/Precept.Mcp/CatalogFormatters.cs` | Add `PriceIn` slot render + `CompoundPrice` qualifier render |

### Does Not Change

- **Parser** — `TryParseQualifiers` is already catalog-driven and axis-agnostic; zero changes
- **All non-price types** — their `QualifierSlot` instances use existing axis values; zero impact
- **`exchangerate`** — `QS_ExchangeRate` is unchanged
- **`quantity`/`period`/`money`** — all use `QS_UnitOrDimension` or `QS_TemporalUnitOrDimension` which are unchanged
- **`InOfExclusive` logic** — unchanged for all types that use it
- **All existing `DeclaredQualifierMeta` subtypes** — `Currency`, `Unit`, `Dimension`, etc. are untouched
- **Proof engine qualifier reads** — `ResolveQualifierFromExpression`, `QualifiersAreCompatible`, `ResolveQualifierOnAxis` — read `DeclaredQualifierMeta.Axis`; need only `CompoundPrice` arm added to existing `switch` patterns there (follow-up, not blocking)

---

## 6. Alternative Options Considered

### Alt A: Dual `in` slots — `(In, Currency)` and `(In, Unit)`

Add a second slot `(In, QualifierAxis.Unit)` to `QS_CurrencyAndDimension`. The parser iterates slots and would parse the first matching `in` token for both slots simultaneously — ambiguous. The parser's slot-iteration model expects at most one slot per preposition. Adding two `In` slots breaks the loop invariant.

**Rejected.** The parser cannot handle two slots with the same preposition. `PriceIn` with registry disambiguation is the correct single-slot model.

### Alt B: `QualifierSlot` as a full DU (`SingleAxisSlot` / `PolymorphicSlot`)

Make `QualifierSlot` an abstract record with two sealed subtypes. Consumers (`Parser.Types.cs`, `RichHoverFactory.cs`, `CompletionHandler.cs`) pattern-match on the subtype. Also requires changing `ParsedQualifier` to carry `CandidateAxes: IReadOnlyList<QualifierAxis>` instead of a single `Axis`.

**Tradeoffs vs. the `PriceIn` approach:**
- ✅ More structurally honest — "this slot has multiple candidate axes" is literally in the DU shape  
- ✅ Follows the flat-record anti-pattern avoidance principle
- ❌ Changes `QualifierSlot`, `ParsedQualifier` (two AST records), the parser, and every consumer that reads `slot.Axis`
- ❌ Surface area is 3× the `PriceIn` approach for the same expressiveness gain
- ❌ Does not solve the compound-value problem on its own — still needs `CompoundPrice`

**Not recommended for this change. Viable if `QualifierSlot` needs further polymorphism beyond price.** If a second polymorphic slot type is ever needed for another type, upgrade to the DU at that point.

### Alt C: Compound parsing at the type checker only — no new axis value

Leave `QualifierAxis.Currency` on the `in` slot. Extend `MapCurrencyQualifier` to try ISO 4217 first, then UCUM, then compound. This is the smallest code change.

**Rejected.** It encodes price-specific disambiguation logic in `MapCurrencyQualifier`, which is named and semantically scoped to currency. It makes the function lie about what it maps. Any consumer that reads the axis name `Currency` from the slot and infers "this slot accepts only currencies" will be wrong for `price`. `PriceIn` as an explicit catalog entry makes the polymorphism visible in the metadata, which is the architectural requirement.

---

## 7. Architectural Judgment

The `PriceIn` approach is the minimum correct solution given the catalog-driven architecture. It:
1. Declares the slot's polymorphic behavior in catalog metadata (`QualifierAxis.PriceIn` on the slot), not in consumer logic
2. Declares the coexistence constraint in catalog metadata (`OfRequiresCurrencyIn` on the shape), not in type-checker identity checks
3. Introduces exactly one new DU subtype (`CompoundPrice`) for the structurally distinct compound case
4. Does not disturb any consumer for any type other than `price`

The proof engine needs a follow-up: `ResolveQualifierOnAxis` for `Price` should pattern-match `CompoundPrice` and expose `.CurrencyCode` on the `Currency` axis and `.UnitCode` on the `Unit` axis. That is a separate slice from the qualifier shape fix and should be tracked accordingly.

---

## 8. Prerequisite

A new diagnostic code `InvalidQualifierCoexistence` must be added to the `DiagnosticCatalog` before the `OfRequiresCurrencyIn` enforcement is wired. Suggest placing it adjacent to `MutuallyExclusiveQualifiers`.
