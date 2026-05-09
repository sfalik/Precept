# Typed Literal System — Comprehensive Implementation Plan

## Status

| Property | Value |
|---|---|
| Author | Frank (Lead/Architect) |
| Date | 2026-05-09T12:33:31-04:00 |
| Scope | All pre-work for the typed literal type system: parsers, validators, catalogs, data loaders, scripts, canonical doc updates |
| Supersedes | `docs/Working/frank-typed-literal-framework.md`, `docs/Working/frank-ucum-iso-gap.md`, `docs/Working/frank-typed-literal-gap-review.md`, `docs/Working/frank-data-layer-decision.md` |

---

## 1. Overview

This plan covers everything needed to bring the typed literal type system to complete pre-work state. When fully executed:

- All C# pre-work is written: parsers, validators, catalogs, data loaders, scripts, stubs, type registrations
- All canonical docs are updated: accurate, complete, written as standing design
- `docs/Working/` contains only `.gitkeep` — every decision is embedded in canonical docs
- The codebase is ready for the language server, evaluator, builder, runtime, and MCP implementations to be designed and built next

**Not in scope:** Language server implementation, evaluator implementation, builder implementation, runtime execution engine, MCP tool implementation. But: the canonical docs for these components contain everything their implementors need.

---

## 2. Prerequisite Reading

Before starting any slice, read these in order:

1. `docs/language/catalog-system.md` — metadata-driven architecture principles
2. `docs/compiler/literal-system.md` — current typed literal system design
3. `docs/language/temporal-type-system.md` — temporal type design
4. `docs/runtime/runtime-api.md` — runtime surface, CLR type mappings, TypeRuntimeMeta
5. `docs/language/precept-language-spec.md` — language spec (reference)
6. `src/Precept/Language/Type.cs` — ContentValidation DU and TypeMeta shape
7. `src/Precept/Language/Types.cs` — current TypeMeta entries and validation instances
8. `src/Precept/Pipeline/TypeChecker.Expressions.cs` — inline validation methods being replaced

---

## 3. Implementation Slices

### Slice 1: Data Layer — Embedded XML Resources and Loaders

**Dependencies:** None  
**Assignee:** George

#### 1a. ISO 4217 Embedded XML

**Files:**
- **Modify** `tools/scripts/refresh-iso4217.js` — change target path from generating `CurrencyCatalog.g.cs` to downloading XML only to `src/Precept/Data/Iso4217/list-one.xml` (already done — confirm the script writes to the correct path and does NOT generate C#)
- **Create** `src/Precept/Data/Iso4217/` directory if absent — ensure `list-one.xml` is present (the refresh script already targets this path)
- **Verify** the `.csproj` includes `src/Precept/Data/Iso4217/list-one.xml` as `EmbeddedResource`

**What to do:**
1. Verify `refresh-iso4217.js` targets `src/Precept/Data/Iso4217/list-one.xml` (it does — line 6–7 of the script)
2. Ensure the Precept `.csproj` has an `<EmbeddedResource Include="Data\Iso4217\list-one.xml" />` entry
3. Run the refresh script to confirm the XML is current

**Tests:** None for this sub-slice — the loader (1c) has the tests.

#### 1b. UCUM Embedded XML

**Files:**
- **Create** `tools/scripts/refresh-ucum.js` — downloads `ucum-essence.xml` from the UCUM distribution site to `src/Precept/Data/Ucum/ucum-essence.xml`. Pattern after `refresh-iso4217.js`: HTTPS download, directory creation, write to target, error handling with fallback URLs
- **Create** `src/Precept/Data/Ucum/ucum-essence.xml` — initial download via the refresh script
- **Verify** the `.csproj` includes `src/Precept/Data/Ucum/ucum-essence.xml` as `EmbeddedResource`

**What to do:**
1. Write `refresh-ucum.js` following the `refresh-iso4217.js` pattern
2. Run it to download the UCUM essence XML
3. Add `<EmbeddedResource Include="Data\Ucum\ucum-essence.xml" />` to the `.csproj`

**Tests:** Script smoke test — run it and confirm the file downloads without error.

#### 1c. CurrencyCatalog Loader Migration

**Files:**
- **Modify** `src/Precept/Language/CurrencyCatalog.cs` — replace the hardcoded `CurrencyEntry[]` array with a `Lazy<FrozenDictionary<string, CurrencyEntry>>` that loads from the embedded `list-one.xml` resource at first access. `CurrencyEntry` gains a `Symbol` field (5th parameter). The `All` property becomes a lazy-loaded `FrozenDictionary<string, CurrencyEntry>`.

**CurrencyEntry record (updated):**
```csharp
public sealed record CurrencyEntry(
    string AlphaCode,    // e.g. "USD"       — from ISO 4217
    int    NumericCode,  // e.g. 840         — from ISO 4217
    string Name,         // e.g. "US Dollar" — from ISO 4217
    int    MinorUnit,    // e.g. 2           — from ISO 4217
    string Symbol        // e.g. "$"         — Precept-owned augmentation; defaults to AlphaCode
);
```

**Symbol data:** A private `FrozenDictionary<string, string> Symbols` in `CurrencyCatalog.cs` maps ~130 alpha codes to their Unicode display symbols. Currencies without an entry use their `AlphaCode` as the symbol — no null checks at call sites. The dictionary is hardcoded in C# source (not an XML file, not generated). ISO 4217 does not include symbols; this is Precept-owned first-party augmentation data.

**What to do:**
1. Remove the hardcoded array of 159 entries
2. Add a `private static readonly FrozenDictionary<string, string> Symbols` dictionary with ~130 currency symbol mappings (curated from Unicode CLDR and common financial usage)
3. Add a `private static readonly Lazy<FrozenDictionary<string, CurrencyEntry>>` field
4. Write an XML parser that reads `list-one.xml` from the assembly's embedded resources
5. Apply the same exclusion rules documented at the top of the current file (exclude precious metals, fund codes, testing codes, XXX)
6. At entry construction, merge symbol: `Symbol = Symbols.GetValueOrDefault(alphaCode, alphaCode)`
7. Expose `public static FrozenDictionary<string, CurrencyEntry> All => _lazy.Value;`

**Tests:**
- `CurrencyCatalogTests.cs` — verify:
  - `All` loads successfully, contains expected count (~159 entries)
  - Known codes present: USD, EUR, GBP, JPY
  - Known exclusions absent: XAU, XTS, XXX
  - Correct `MinorUnit` values: JPY=0, BHD=3, USD=2
  - Symbol mappings: `USD.Symbol == "$"`, `EUR.Symbol == "€"`, `JPY.Symbol == "¥"`
  - Currencies without explicit symbols fall back to alpha code: e.g. `XDR.Symbol == "XDR"`
- `CurrencyCatalogSyncTests.cs` — verify the embedded XML resource is present and parseable

#### 1d. UCUM Atom and Prefix Data Loaders

**Files:**
- **Create** `src/Precept/Language/Ucum/UcumAtom.cs` — `sealed record UcumAtom(string Code, string Name, DimensionVector Vector, UcumExactFactor Scale, bool Prefixable, string? AnnotationClass)`
- **Create** `src/Precept/Language/Ucum/UcumPrefix.cs` — `sealed record UcumPrefix(string Code, string Name, UcumExactFactor Factor, int Order)`
- **Create** `src/Precept/Language/Ucum/UcumExactFactor.cs` — exact rational representation for conversion factors: numerator/denominator pair + base-10 exponent. No floating-point approximation.
- **Create** `src/Precept/Language/Ucum/UcumAtomCatalog.cs` — static class with `Lazy<FrozenDictionary<string, UcumAtom>>` loaded from the embedded `ucum-essence.xml`. NOT a generated file.

  **Two-phase load (required for derived unit resolution):** The UCUM XML defines units in two categories: (1) the 7 SI base units (`m`, `s`, `kg`, `A`, `K`, `mol`, `cd`) with intrinsic dimension vectors, and (2) all other defined units (`N`, `J`, `Pa`, `Hz`, `L`, `[degF]`, etc.) specified as expressions of other units via `<value Unit="..." UNIT="...">` attributes. `UcumAtom.Vector` and `UcumAtom.Scale` must represent **fully resolved SI-relative values** — not raw definition expressions.

  The loader implements a two-phase strategy:
  - **Phase 1:** Load the 7 SI base units with their intrinsic dimension vectors (hardcoded or read from XML). These serve as the resolution roots.
  - **Phase 2:** For each defined unit, parse its `<value Unit="...">` expression using a **minimal inline expression evaluator** (not the full UcumParser from Slice 3 — the loader is self-contained). This mini-evaluator handles the restricted `<value>` grammar: unit atoms, prefix+atom combinations, exponents, and multiplication/division operators as they appear in the XML definition strings. It resolves transitively (e.g., `J` → defined in terms of `N` → defined in terms of `kg`, `m`, `s`) by iterating until all units are resolved or flagging circular definitions as errors.

  This design means **Slice 1d has no dependency on Slice 3**. The full UcumParser (Slice 3) depends on UcumAtomCatalog (Slice 1d), not the reverse. The mini-evaluator in the loader handles only the restricted `<value>` attribute syntax — it is simpler than the full parser because `<value>` expressions in the XML use a smaller grammar subset.
- **Create** `src/Precept/Language/Ucum/UcumPrefixCatalog.cs` — static class with the ~20 SI prefixes. These are stable enough to hardcode, or load from `ucum-essence.xml` — implementor's choice. Exposes `FrozenDictionary<string, UcumPrefix>` and a `LongestPrefixMatch(string code)` helper.

**Tests:**
- `UcumAtomCatalogTests.cs` — verify loads successfully, contains expected atom count (~300), includes known atoms (`kg`, `m`, `s`, `mol`, `[degF]`), correct dimension vectors, correct prefixability flags
- `UcumPrefixCatalogTests.cs` — verify all 20 SI prefixes present, correct factors, longest-prefix-match works (`mg` → `m` prefix + `g` atom)
- `UcumExactFactorTests.cs` — verify arithmetic: multiply, divide, rational accuracy (no floating-point drift)

---

### Slice 2: DimensionVector and DimensionCatalog

**Dependencies:** Slice 1d (UcumAtom)  
**Assignee:** George

**Files:**
- **Create** `src/Precept/Language/Ucum/DimensionVector.cs` — `readonly record struct DimensionVector(int Length, int Mass, int Time, int ElectricCurrent, int Temperature, int AmountOfSubstance, int LuminousIntensity)` with `Multiply`, `Divide`, `Pow`, `IsDimensionless`, `Equals` helpers
- **Create** `src/Precept/Language/Ucum/DimensionCatalog.cs` — static class with `sealed record DimensionAlias(string Name, DimensionVector Vector, string Description)`. Curated set of Precept dimension names mapped to vectors:
  - `length` → (1,0,0,0,0,0,0)
  - `mass` → (0,1,0,0,0,0,0)
  - `temperature` → (0,0,0,0,1,0,0)
  - `volume` → (3,0,0,0,0,0,0) — length³
  - `area` → (2,0,0,0,0,0,0) — length²
  - `speed` → (1,0,-1,0,0,0,0) — length/time
  - `energy` → (2,1,-2,0,0,0,0) — mass·length²/time²
  - `pressure` → (-1,1,-2,0,0,0,0) — mass/(length·time²)
  - `force` → (1,1,-2,0,0,0,0) — mass·length/time²
  - `count` → (0,0,0,0,0,0,0) — dimensionless with approved count annotations
  - **No `time` dimension** — per the locked decision, temporal quantities use `duration`/`period`, not `quantity of 'time'`
- Exposes `AllAliases`, `TryGetAlias(DimensionVector)`, `GetByName(string)`

**Tests:**
- `DimensionVectorTests.cs` — arithmetic, equality, dimensionless check, standard vector identities (force = mass × acceleration)
- `DimensionCatalogTests.cs` — all aliases present, vector lookups correct, `time` dimension absent, `count` is dimensionless

---

### Slice 3: UCUM Parser

**Dependencies:** Slices 1d, 2  
**Assignee:** George

**Files:**
- **Create** `src/Precept/Language/Ucum/UcumTokenKind.cs` — `enum UcumTokenKind { Atom, Prefix, Dot, Slash, OpenParen, CloseParen, Exponent, Annotation, EndOfInput, Error }`
- **Create** `src/Precept/Language/Ucum/UcumToken.cs` — `readonly record struct UcumToken(UcumTokenKind Kind, string Text, int Start, int Length)`
- **Create** `src/Precept/Language/Ucum/UcumLexer.cs` — `static class UcumLexer` with `IReadOnlyList<UcumToken> Tokenize(string expression)`. Handles bracketed atoms (`[degF]`, `mm[Hg]`), annotations (`{annotation}`), signed exponents, dot/slash operators.
- **Create** `src/Precept/Language/Ucum/UcumExpression.cs` — AST: `abstract record UcumExpression` with subtypes `UcumAtomNode`, `UcumPrefixedAtomNode`, `UcumProductNode`, `UcumQuotientNode`, `UcumExponentNode`, `UcumGroupNode`, `UcumAnnotatedNode`
- **Create** `src/Precept/Language/Ucum/UcumParsedUnit.cs` — `sealed record UcumParsedUnit(string SourceText, string CanonicalCode, DimensionVector Vector, UcumExactFactor Scale, string? PreferredDimensionAlias, IReadOnlyList<UcumAtom> UsedAtoms, IReadOnlyList<string> Annotations)`. Annotations (e.g. `{RBC}` from `{RBC}/uL`, `{count}`) are preserved for display purposes but are **excluded** from `DimensionVector` comparison, `UcumExactFactor` computation, and `CanonicalCode` generation. Annotations have no effect on dimensional analysis or unit conversion — UCUM specifies this explicitly. When `Annotations` is empty, treat as `[]` (never null).
- **Create** `src/Precept/Language/Ucum/UcumDiagnostic.cs` — `sealed record UcumDiagnostic(string Code, string Message, int Start, int Length, string? Suggestion)`
- **Create** `src/Precept/Language/Ucum/UcumParseResult.cs` — `readonly record struct UcumParseResult(bool IsValid, UcumParsedUnit? Unit, IReadOnlyList<UcumDiagnostic> Diagnostics)`
- **Create** `src/Precept/Language/Ucum/UcumParser.cs` — `static class UcumParser` with `UcumParseResult Parse(string expression)`. LL(1) recursive descent parser implementing the grammar:
  ```
  Expression  := Product [ '/' Product ]
  Product     := Factor { '.' Factor }
  Factor      := Primary [ Exponent ] [ Annotation ]
  Primary     := AtomOrPrefixedAtom | '(' Expression ')'
  AtomOrPrefixedAtom := resolved by longest valid catalog match
  Exponent    := signed integer suffix
  Annotation  := '{' text '}'
  ```
  After parsing, the semantic reducer computes canonical code, reduced terms, exact scale, dimension vector, and alias classification via `DimensionCatalog.TryGetAlias`.
- **Create** `src/Precept/Language/Ucum/UcumCatalog.cs` — `static class UcumCatalog` facade: `UcumParseResult Parse(string expression)`, `bool IsValid(string expression)`, `IReadOnlyList<UcumAtom> BrowseTier1()`, `UcumAtom? LookupAtom(string code)`

**Tests:**
- `UcumLexerTests.cs` — tokenizes simple atoms, prefixed atoms, bracketed atoms, dot/slash expressions, exponents, annotations, error recovery
- `UcumParserTests.cs` — parses and validates:
  - Simple atoms: `kg`, `m`, `s`, `mol`
  - Prefixed atoms: `mg`, `cm`, `mmol`
  - Multiplication: `kg.m`
  - Division: `m/s`
  - Exponents: `m/s^2`, `kg.m/s^2`
  - Grouping: `mmol/(L.min)`
  - Annotations: `{count}`, `{RBC}/uL`
  - Bracketed atoms: `[degF]`, `mm[Hg]`
  - Complex: `kg.m/s^2` (force), `J/s` (power)
  - Invalid: empty string, unrecognized atom, unbalanced parens, double slash
- `UcumCatalogTests.cs` — facade methods, Tier 1 browse, atom lookup
- Verify dimension vectors: `kg.m/s^2` → force vector, `m/s` → speed vector

---

### Slice 4: Temporal Parser

**Dependencies:** None (parallel with Slices 1–3)  
**Assignee:** George

**Files:**
- **Create** `src/Precept/Language/Time/TemporalLiteralKind.cs` — `enum TemporalLiteralKind { Date, Time, DateTime, Instant, ZonedDateTime, Timezone, TemporalQuantity }`
- **Create** `src/Precept/Language/Time/TemporalParseResult.cs` — `readonly record struct TemporalParseResult(bool IsValid, object? Value, string? CanonicalText, IReadOnlyList<TemporalDiagnostic> Diagnostics)`
- **Create** `src/Precept/Language/Time/TemporalDiagnostic.cs` — `sealed record TemporalDiagnostic(string Code, string Message, string? Suggestion)`
- **Create** `src/Precept/Language/Time/TemporalUnits.cs` — static unit name table mapping unit words to their temporal kind:
  - Always-period: `years`, `months`
  - Always-duration: (none — all sub-day units are context-dependent)
  - Context-dependent: `weeks`, `days`, `hours`, `minutes`, `seconds`
  - Each entry carries: singular/plural forms, calendar vs timeline classification, `Period`/`Duration` construction delegate
- **Create** `src/Precept/Language/Time/TemporalQuantityParser.cs` — `static class TemporalQuantityParser` parsing `<integer> <unit> [+ <integer> <unit>]*`. Returns `Period` or `Duration` based on unit classification and context. Enforces integer-only magnitudes for temporal quantities.
- **Create** `src/Precept/Language/Time/TemporalParser.cs` — `static class TemporalParser` entry point dispatching by `TemporalLiteralKind`:
  - `Date` → `LocalDatePattern.Iso.Parse(rawText)`
  - `Time` → `LocalTimePattern.ExtendedIso.Parse(rawText)` with `HH:mm` fallback
  - `DateTime` → `LocalDateTimePattern.ExtendedIso.Parse(rawText)`
  - `Instant` → `InstantPattern.ExtendedIso.Parse(rawText)`
  - `ZonedDateTime` → custom: parse datetime portion + bracket-enclosed IANA TZ via `DateTimeZoneProviders.Tzdb`
  - `Timezone` → `DateTimeZoneProviders.Tzdb.GetZoneOrNull(rawText)`
  - `TemporalQuantity` → delegates to `TemporalQuantityParser`

**Tests:**
- `TemporalParserTests.cs` — all 7 forms:
  - Date: `2026-04-15` ✓, `2026/04/15` ✗, `2026-13-01` ✗
  - Time: `14:30:00` ✓, `14:30` ✓, `25:00:00` ✗
  - DateTime: `2026-04-15T14:30:00` ✓
  - Instant: `2026-04-15T14:30:00Z` ✓, `2026-04-15T14:30:00` ✗ (no Z)
  - ZonedDateTime: `2026-04-15T14:30:00[America/New_York]` ✓, `2026-04-15T14:30:00[Invalid/Zone]` ✗
  - Timezone: `America/New_York` ✓, `Mars/Olympus` ✗
  - TemporalQuantity: `30 days` ✓, `72 hours` ✓, `2 years + 6 months` ✓, `0.5 days` ✗ (non-integer), `30 parsecs` ✗ (invalid unit)
- `TemporalQuantityParserTests.cs` — unit resolution, combined quantities, integer enforcement
- `TemporalUnitsTests.cs` — all unit words recognized, classification correct

---

### Slice 5: ContentValidation DU Update

**Dependencies:** Slices 3, 4  
**Assignee:** George

**Files:**
- **Modify** `src/Precept/Language/Type.cs` — update the `ContentValidation` DU:
  1. `NodaTimeValidation` gains `TemporalLiteralKind LiteralKind` as first parameter (before `NodaTimePattern`):
     ```csharp
     public sealed record NodaTimeValidation(
         TemporalLiteralKind LiteralKind,
         string NodaTimePattern,
         string FormatDescription,
         string[] Examples
     ) : ContentValidation(FormatDescription, Examples);
     ```
  2. Add `UcumValidation`:
     ```csharp
     public sealed record UcumValidation(
         string FormatDescription, string[] Examples
     ) : ContentValidation(FormatDescription, Examples);
     ```
  3. Add four domain-specific compound validators (resolving gap G15 — separate DU subtypes, not a single `QuantityValidation` with enum):
     ```csharp
     public sealed record MoneyValidation(
         string FormatDescription, string[] Examples
     ) : ContentValidation(FormatDescription, Examples);

     public sealed record QuantityValidation(
         string FormatDescription, string[] Examples
     ) : ContentValidation(FormatDescription, Examples);

     public sealed record PriceValidation(
         string FormatDescription, string[] Examples
     ) : ContentValidation(FormatDescription, Examples);

     public sealed record ExchangeRateValidation(
         string FormatDescription, string[] Examples
     ) : ContentValidation(FormatDescription, Examples);
     ```
  4. `ClosedSetValidation` — unchanged (still used for `currency`, `dimension`)
  5. `RegexValidation` — unchanged

**Design note on G15:** Four separate DU subtypes (`MoneyValidation`, `QuantityValidation`, `PriceValidation`, `ExchangeRateValidation`) replace the single `QuantityValidation(QuantityDomain)` approach. Each subtype carries exactly the metadata its validator needs. This is catalog-idiomatic — no enum-identity switching.

**Tests:**
- Existing tests continue to compile and pass after `NodaTimeValidation` signature change
- Verify DU pattern matching exhaustiveness — the `TypedConstantValidation.Validate` switch must cover all new subtypes

---

### Slice 6: Validation Framework Types

**Dependencies:** Slice 5  
**Assignee:** George

**Files:**
- **Create** `src/Precept/Language/TypedConstantParseResult.cs` — consumer-facing result type:
  ```csharp
  public readonly record struct TypedConstantParseResult(
      bool IsValid,
      object? Value,
      string? CanonicalText,
      string FormatDescription,
      IReadOnlyList<TypedConstantDiagnostic> Diagnostics)
  {
      public static TypedConstantParseResult Accepted(string rawText) => new(true, rawText, rawText, "", []);
      public static TypedConstantParseResult Failed(string formatDesc, params TypedConstantDiagnostic[] diags) => new(false, null, null, formatDesc, diags);
  }

  public sealed record TypedConstantDiagnostic(string Code, string Message, string? Suggestion = null);

  public sealed record TypedConstantContext(TypeKind? PeerType = null, OperatorKind? Operator = null);
  ```
- **Create** `src/Precept/Language/TypedConstantValidation.cs` — static dispatcher:
  ```csharp
  public static class TypedConstantValidation
  {
      public static TypedConstantParseResult Validate(
          ContentValidation validation,
          string rawText,
          TypeKind targetType,
          TypedConstantContext? context = null) => validation switch
      {
          NodaTimeValidation noda => TemporalValidator.Validate(rawText, targetType, noda, context),
          ClosedSetValidation closed => ClosedSetValidator.Validate(rawText, closed),
          RegexValidation regex => RegexValidator.Validate(rawText, regex),
          UcumValidation ucum => UcumValidator.Validate(rawText, targetType, ucum, context),
          MoneyValidation money => MoneyValidator.Validate(rawText, money),
          QuantityValidation qty => QuantityValidator.Validate(rawText, targetType, qty, context),
          PriceValidation price => PriceValidator.Validate(rawText, price),
          ExchangeRateValidation exrate => ExchangeRateValidator.Validate(rawText, exrate),
          _ => TypedConstantParseResult.Accepted(rawText),
      };
  }
  ```

**Tests:**
- `TypedConstantValidationTests.cs` — dispatcher routes correctly to each validator, default arm returns Accepted

---

### Slice 7: Domain-Specific Validators

**Dependencies:** Slices 3, 4, 6  
**Assignee:** George

**Files:**
- **Create** `src/Precept/Language/Time/TemporalValidator.cs` — wraps `TemporalParser` results in `TypedConstantParseResult`. Dispatches on `NodaTimeValidation.LiteralKind`.
- **Create** `src/Precept/Language/Ucum/UcumValidator.cs` — wraps `UcumCatalog.Parse` results in `TypedConstantParseResult`.
- **Create** `src/Precept/Language/ClosedSetValidator.cs` — extracts from `TypeChecker.Expressions.cs`, returns `TypedConstantParseResult` instead of tuple.
- **Create** `src/Precept/Language/RegexValidator.cs` — extracts from `TypeChecker.Expressions.cs`, returns `TypedConstantParseResult`.
- **Create** `src/Precept/Language/CurrencyValidator.cs` — validates ISO 4217 code against `CurrencyCatalog.All`. Returns `TypedConstantParseResult` with the `CurrencyEntry` as parsed value.
- **Create** `src/Precept/Language/MoneyValidator.cs` — parses `<decimal> <ISO-4217>` format (e.g., `100 USD`). Validates amount as decimal, currency code against `CurrencyCatalog`.
- **Create** `src/Precept/Language/QuantityValidator.cs` — parses `<decimal> <UCUM-unit>` format (e.g., `5 kg`). Validates magnitude as decimal, unit via `UcumCatalog.Parse`.
- **Create** `src/Precept/Language/PriceValidator.cs` — parses `<decimal> <ISO-4217>/<UCUM-unit>` format (e.g., `4.17 USD/each`). Validates each component.
- **Create** `src/Precept/Language/ExchangeRateValidator.cs` — parses `<decimal> <ISO-4217>/<ISO-4217>` format (e.g., `1.08 USD/EUR`). Validates both currency codes.

**Tests:**
- `TemporalValidatorTests.cs` — each `TemporalLiteralKind` produces correct `TypedConstantParseResult`
- `UcumValidatorTests.cs` — valid/invalid UCUM expressions produce correct results
- `ClosedSetValidatorTests.cs` — membership checks, case sensitivity
- `RegexValidatorTests.cs` — pattern matching, failure messages
- `CurrencyValidatorTests.cs` — valid codes, invalid codes, case handling
- `MoneyValidatorTests.cs` — `100 USD` ✓, `50.25 EUR` ✓, `100` ✗ (no currency), `100 XYZ` ✗ (invalid code)
- `QuantityValidatorTests.cs` — `5 kg` ✓, `2.5 mg/dL` ✓, `5 parsecs` ✗
- `PriceValidatorTests.cs` — `4.17 USD/each` ✓, `10.00 EUR/kg` ✓
- `ExchangeRateValidatorTests.cs` — `1.08 USD/EUR` ✓, `0.92 EUR/USD` ✓

---

### Slice 8: TypeMeta Entry Updates

**Dependencies:** Slices 5, 7  
**Assignee:** George

**Files:**
- **Modify** `src/Precept/Language/Types.cs`:
  1. **Update existing temporal entries** with `TemporalLiteralKind`:
     - `DateValidation` → `new NodaTimeValidation(TemporalLiteralKind.Date, "uuuu'-'MM'-'dd", ...)`
     - `TimeValidation` → `new NodaTimeValidation(TemporalLiteralKind.Time, "HH':'mm':'ss", ...)`
     - `DateTimeValidation` → `new NodaTimeValidation(TemporalLiteralKind.DateTime, ...)`
     - `PeriodValidation` → `new NodaTimeValidation(TemporalLiteralKind.TemporalQuantity, "quantity", "Temporal quantity: <integer> <unit> [+ <integer> <unit>]*", ["30 days", "2 years + 6 months", "P30D"])`
       - **Critical:** Period must accept BOTH ISO 8601 format (`P30D`) and quantity form (`30 days`). The `TemporalValidator` handles both by trying quantity parse first, then falling back to `PeriodPattern.NormalizingIso`.
  2. **Add new temporal entries:**
     - `InstantValidation` → `new NodaTimeValidation(TemporalLiteralKind.Instant, ...)`
     - `TimezoneValidation` → `new NodaTimeValidation(TemporalLiteralKind.Timezone, ...)`
     - `ZonedDateTimeValidation` → `new NodaTimeValidation(TemporalLiteralKind.ZonedDateTime, ...)`
     - `DurationValidation` → `new NodaTimeValidation(TemporalLiteralKind.TemporalQuantity, "quantity", ..., ["72 hours", "2 hours + 30 minutes", "3600 seconds"])`
  3. **Replace `unitofmeasure` validation:**
     - Remove `RecognizedUnits` frozen set
     - Remove `UnitOfMeasureValidation` (`ClosedSetValidation`)
     - Replace with `new UcumValidation("UCUM expression", ["kg", "m/s^2", "mg/dL"])`
  4. **Update `dimension` validation:**
     - Remove `RecognizedDimensions` frozen set
     - Derive `DimensionValidation` from `DimensionCatalog.AllAliases` names: `new ClosedSetValidation("recognized dimensions", DimensionCatalog.AllNames, ...)`. `dimension` retains `ClosedSetValidation` — the curated catalog is a closed set by design.
  5. **Add new domain type validations:**
     - `money` → `new MoneyValidation("Monetary amount: <decimal> <ISO-4217>", ["100 USD", "50.25 EUR"])`
     - `quantity` → `new QuantityValidation("Physical quantity: <decimal> <UCUM-unit>", ["5 kg", "2.5 mg/dL"])`
     - `price` → `new PriceValidation("Price: <decimal> <ISO-4217>/<UCUM-unit>", ["4.17 USD/each", "10.00 EUR/kg"])`
     - `exchangerate` → `new ExchangeRateValidation("Exchange rate: <decimal> <ISO-4217>/<ISO-4217>", ["1.08 USD/EUR"])`
  6. **Wire `ContentValidation` onto all TypeMeta entries** that currently lack it (instant, timezone, zoneddatetime, duration, money, quantity, price, exchangerate)

**Tests:**
- `TypesTests.cs` — verify every TypeMeta entry that should have ContentValidation does, and none that shouldn't does
- Regression: existing date/time/datetime/period/currency validation behavior unchanged

---

### Slice 9: TypeChecker Migration

**Dependencies:** Slices 6, 8  
**Assignee:** George

**Files:**
- **Modify** `src/Precept/Pipeline/TypeChecker.Expressions.cs`:
  1. **Delete** `ValidateContent` method (lines 208–215)
  2. **Delete** `ValidateNodaTime` method (lines 217–256)
  3. **Delete** `ValidateClosedSet` method (lines 258–264)
  4. **Delete** `ValidateRegex` method (lines 266–273)
  5. **Replace** the call in `ResolveTypedConstant` (line 192) from:
     ```csharp
     var (isValid, parsedValue, errorMessage) = ValidateContent(cv, rawText, meta.DisplayName);
     ```
     To:
     ```csharp
     var result = TypedConstantValidation.Validate(cv, rawText, targetType);
     ```
  6. Update the success/failure branches to use `TypedConstantParseResult`:
     ```csharp
     if (result.IsValid)
         return new TypedTypedConstant(targetType, rawText, result.Value, lit.Span);

     foreach (var diag in result.Diagnostics)
         ctx.Diagnostics.Add(
             Diagnostics.Create(DiagnosticCode.InvalidTypedConstantContent, lit.Span,
                 rawText, diag.Message));
     return new TypedErrorExpression(lit.Span);
     ```

**Tests:**
- **Full regression:** Run `dotnet test test/Precept.Tests/` — every existing test must pass. The migration replaces internal dispatch, not behavior.
- Specific: typed constant validation for `date`, `time`, `datetime`, `period`, `currency`, `unitofmeasure`, `dimension` — all produce identical diagnostics before and after

---

### Slice 10: Runtime Type Stubs

**Dependencies:** Slices 3, 4  
**Assignee:** George

**Note:** These are stubs. The runtime execution engine is out of scope, but the types and shapes must exist so canonical docs reference real code.

**Files:**
- **Create** `src/Precept/Runtime/Measures/Unit.cs` — `internal sealed class Unit` (stub: evaluator-internal enriched unit entity built from `UcumParsedUnit`)
- **Create** `src/Precept/Runtime/Measures/MeasureDimension.cs` — `internal readonly record struct MeasureDimension` (stub: runtime-facing dimension wrapper over `DimensionVector`)
- **Create** `src/Precept/Runtime/Measures/UnitFactory.cs` — `internal static class UnitFactory` (stub: converts parsed units into interned runtime `Unit` instances)

  **Interning key design (for when this is implemented):** The interning key for `Unit` is `(DimensionVector, UcumExactFactor)`. Two units with the same dimension vector and the same scale factor are the same physical unit regardless of source expression — so `N` and `kg.m/s^2` are the same `Unit` instance. `SourceText` and `CanonicalCode` from `UcumParsedUnit` are display-only properties preserved for user-facing output, not part of the identity. `UcumParsedUnit` provides all fields needed for this interning strategy when the evaluator is implemented.

**Tests:** None — these are stubs with `throw new NotImplementedException()` bodies. The stubs exist so the file inventory is accurate and cross-references resolve.

---

### Slice 11: Canonical Doc Updates

**Dependencies:** Slices 1–10 complete  
**Assignee:** Frank (review), George (mechanical updates)

See Section 4 for the full per-document checklist.

---

### Slice 12: Working Doc Deletion

**Dependencies:** Slice 11 complete  
**Assignee:** George

**Files to delete:**
- `docs/Working/frank-typed-literal-framework.md`
- `docs/Working/frank-ucum-iso-gap.md`
- `docs/Working/frank-typed-literal-gap-review.md`
- `docs/Working/frank-data-layer-decision.md`

**Verification:** After deletion, `docs/Working/` contains only `.gitkeep`. Every decision from these files is embedded in canonical docs.

---

## 4. Canonical Doc Update Checklist

### `docs/compiler/literal-system.md`

1. **Close open question on `ITypedConstantValidator` (line 496–498).** Replace with standing design:
   - Registration surface is `ContentValidation` on `TypeMeta`
   - Dispatch is `TypedConstantValidation.Validate(...)` — static switch on DU subtypes
   - No interface — static methods per domain validator
   - The DU is the registry; adding a type means adding a DU subtype and a switch arm
2. **Add content validation table for all typed-literal types** — date, time, datetime, instant, zoneddatetime, timezone, duration, period, currency, unitofmeasure, dimension, money, quantity, price, exchangerate. Table columns: Precept type, ContentValidation subtype, format description, examples.
3. **Add Restore column to the consumer matrix.** "Restore reads stored field values via `TypeRuntimeMeta.ReadJson`; shares delegates with Fire and Update JSON lanes."
4. **Update quantity unit names table** — confirm temporal unit names are comprehensive
5. **Close or address remaining open questions** (interpolated typed-constant validation timing, structural validation fallback)
6. **Add `stateref` disposition note** — explicitly state that `stateref` validation is a name-binder concern (validates against declared state names), not a domain parser. It does not use ContentValidation.

### `docs/runtime/runtime-api.md`

1. **Fix CLR type mapping table (the critical fix).** Replace:
   - `date` → ~~`DateOnly`~~ → `LocalDate`
   - `time` → ~~`TimeOnly`~~ → `LocalTime`
   - `instant` → ~~`DateTimeOffset`~~ → `Instant`
   - Add: `datetime` → `LocalDateTime`
   - Add: `zoneddatetime` → `ZonedDateTime`
   - Add: `timezone` → `DateTimeZone`
   - `duration` → `Duration` (NodaTime) — already correct
   - `period` → `Period` (NodaTime) — add if missing
2. **Fix the Fire example code** (line 272) — replace `DateTimeOffset` with `Instant`:
   ```csharp
   .Set<Instant>("timestamp", SystemClock.Instance.GetCurrentInstant())
   ```
3. **Fix `Deliberate Exclusions` § "No migration logic"** (line 769) — replace `FromJson` with `Restore`, remove the claim that it "bypasses constraint validation" (it doesn't — `RestoreConstraintsFailed` exists for schema drift detection). Write as:
   > No migration logic. If the definition changed since data was stored, migration is the caller's responsibility. Restore evaluates constraints against the restored state — schema drift is detected via `RestoreConstraintsFailed`, not silently accepted.
4. **Add JSON wire format table** — new section or table showing the JSON wire representation for each typed-literal type used by `TypeRuntimeMeta.ReadJson`:

   | Precept type | JSON wire format | Example |
   |---|---|---|
   | `date` | ISO 8601 string | `"2026-04-15"` |
   | `time` | ISO 8601 string | `"14:30:00"` |
   | `datetime` | ISO 8601 string | `"2026-04-15T14:30:00"` |
   | `instant` | ISO 8601 UTC string | `"2026-04-15T14:30:00Z"` |
   | `zoneddatetime` | Object: `{datetime, timezone}` | `{"datetime":"2026-04-15T14:30:00","timezone":"America/New_York"}` |
   | `timezone` | IANA string | `"America/New_York"` |
   | `duration` | ISO 8601 duration string | `"PT72H"` |
   | `period` | ISO 8601 period string | `"P30D"` |
   | `money` | Object: `{amount, currency}` | `{"amount":100.00,"currency":"USD"}` |
   | `quantity` | Object: `{magnitude, unit}` | `{"magnitude":5.0,"unit":"kg"}` |
   | `price` | Object: `{amount, currency, unit}` | `{"amount":4.17,"currency":"USD","unit":"each"}` |
   | `exchangerate` | Object: `{rate, from, to}` | `{"rate":1.08,"from":"USD","to":"EUR"}` |
   | `currency` | ISO 4217 string | `"USD"` |
   | `unitofmeasure` | UCUM string | `"kg"` |
   | `dimension` | Dimension name string | `"mass"` |

5. **Add `TypeRuntimeMeta` delegate shapes section** — document what `ReadJson` and `WriteJson` do for each typed-literal type, clarifying that these are the runtime ingress/egress delegates and are distinct from compile-time `ContentValidation`.
6. **Clarify `ParseString`/`FormatString` relationship** — add note: `ParseString` delegates for typed-literal types call the same underlying domain parsers as `ContentValidation` validators but produce `PreceptValue` instead of `TypedConstantParseResult`. `FormatString` produces the canonical string representation (same as `CanonicalText` from the compile-time validator).
7. **Rename all remaining `FromJson` references** to `Restore` for consistency.
8. **Note that `TypeRuntimeMeta` is not yet on the `TypeMeta` record in code** — it is aspirational in the doc and will be added when runtime support lands. The three-registration requirement (TypeRuntime<T>, TypeRuntimeMeta, ContentValidation) is the target state.

### `docs/language/temporal-type-system.md`

1. **Add canonical temporal parser section** — reference `src/Precept/Language/Time/` as the shared temporal parsing subsystem. Document the 7 literal forms, their NodaTime backing types, parse patterns, and canonical serialization.
2. **Reconcile with `literal-system.md`** — ensure cross-references are consistent. The temporal type system doc describes WHAT the temporal types are; the literal system doc describes HOW they enter the language as literals.
3. **Update CLR type references** to match the corrected `runtime-api.md` table.
4. **Remove any references to `System.DateOnly`, `System.TimeOnly`, `System.DateTimeOffset`** — NodaTime types are canonical.

### `docs/language/catalog-system.md`

1. **Add external reference data distinction** — new section or paragraph in the architectural identity section explaining:
   - Precept catalogs (TokenMeta, TypeMeta, etc.) = language specification in machine-readable form
   - External reference data (ISO 4217, UCUM) = third-party authoritative databases Precept validates against
   - External data uses embedded XML + lazy load, NOT the catalog pattern
   - The test: "Is this part of a complete description of Precept?" — 159 currency codes are NOT Precept; `TypeMeta` for `currency` IS Precept
2. **Update the consumer table** — add `TypedConstantValidation.Validate` as a consumer of the Types catalog's `ContentValidation` metadata
3. **Note that `ContentValidation` is the catalog hook for typed-literal validation** — the DU on `TypeMeta` is the registry; `TypedConstantValidation.Validate` is the dispatcher

### `docs/language/precept-language-spec.md`

1. Review for any stale temporal type references. Update if any CLR type mentions exist.
2. Confirm all 8 temporal types are listed with correct semantics.

### `docs/runtime/evaluator.md` (if exists)

1. Add note on runtime measures: `src/Precept/Runtime/Measures/` contains evaluator-internal types (`Unit`, `MeasureDimension`, `UnitFactory`) that wrap the shared UCUM parsed-unit model for runtime arithmetic.

### Grammar generator assessment

1. **Verify temporal keywords** — confirm `date`, `time`, `datetime`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime` are already in the grammar as type keywords. They should be — they're in the `Types` catalog and the grammar generator reads from it.
2. **Verify no new token kinds needed** — the typed literal system uses `TypedConstant` tokens (already exist). The content inside `'...'` is opaque text validated by `ContentValidation`. No new token kinds are needed for any of the new typed-literal types.
3. **Document this assessment** in a brief note in the plan's verification section.

---

## 5. File Inventory

### New Files

| File | Purpose |
|---|---|
| `tools/scripts/refresh-ucum.js` | Downloads UCUM essence XML |
| `src/Precept/Data/Ucum/ucum-essence.xml` | UCUM authoritative data |
| `src/Precept/Language/Ucum/DimensionVector.cs` | 7-dimensional SI vector |
| `src/Precept/Language/Ucum/DimensionCatalog.cs` | Curated dimension aliases |
| `src/Precept/Language/Ucum/UcumAtom.cs` | UCUM atom record |
| `src/Precept/Language/Ucum/UcumPrefix.cs` | UCUM prefix record |
| `src/Precept/Language/Ucum/UcumExactFactor.cs` | Exact rational conversion factor |
| `src/Precept/Language/Ucum/UcumAtomCatalog.cs` | Lazy loader from embedded XML |
| `src/Precept/Language/Ucum/UcumPrefixCatalog.cs` | Prefix table + longest-match |
| `src/Precept/Language/Ucum/UcumTokenKind.cs` | UCUM lexer token kinds |
| `src/Precept/Language/Ucum/UcumToken.cs` | UCUM lexer token |
| `src/Precept/Language/Ucum/UcumLexer.cs` | UCUM expression tokenizer |
| `src/Precept/Language/Ucum/UcumExpression.cs` | UCUM AST node types |
| `src/Precept/Language/Ucum/UcumParsedUnit.cs` | Consumer-facing parsed unit |
| `src/Precept/Language/Ucum/UcumDiagnostic.cs` | Parser diagnostics |
| `src/Precept/Language/Ucum/UcumParseResult.cs` | Parse result wrapper |
| `src/Precept/Language/Ucum/UcumParser.cs` | LL(1) UCUM parser |
| `src/Precept/Language/Ucum/UcumCatalog.cs` | UCUM facade |
| `src/Precept/Language/Ucum/UcumValidator.cs` | UCUM → TypedConstantParseResult |
| `src/Precept/Language/Time/TemporalLiteralKind.cs` | Temporal form discriminator |
| `src/Precept/Language/Time/TemporalParser.cs` | Temporal parse entry point |
| `src/Precept/Language/Time/TemporalParseResult.cs` | Temporal parse result |
| `src/Precept/Language/Time/TemporalDiagnostic.cs` | Temporal diagnostics |
| `src/Precept/Language/Time/TemporalQuantityParser.cs` | `<int> <unit> + ...` parser |
| `src/Precept/Language/Time/TemporalUnits.cs` | Unit name table |
| `src/Precept/Language/Time/TemporalValidator.cs` | Temporal → TypedConstantParseResult |
| `src/Precept/Language/TypedConstantParseResult.cs` | Framework result type |
| `src/Precept/Language/TypedConstantValidation.cs` | Static dispatcher |
| `src/Precept/Language/ClosedSetValidator.cs` | Extracted from TypeChecker |
| `src/Precept/Language/RegexValidator.cs` | Extracted from TypeChecker |
| `src/Precept/Language/CurrencyValidator.cs` | ISO 4217 code validator |
| `src/Precept/Language/MoneyValidator.cs` | `<decimal> <ISO-4217>` |
| `src/Precept/Language/QuantityValidator.cs` | `<decimal> <UCUM-unit>` |
| `src/Precept/Language/PriceValidator.cs` | `<decimal> <ISO-4217>/<UCUM-unit>` |
| `src/Precept/Language/ExchangeRateValidator.cs` | `<decimal> <ISO-4217>/<ISO-4217>` |
| `src/Precept/Runtime/Measures/Unit.cs` | Evaluator-internal unit (stub) |
| `src/Precept/Runtime/Measures/MeasureDimension.cs` | Runtime dimension wrapper (stub) |
| `src/Precept/Runtime/Measures/UnitFactory.cs` | Parsed unit → runtime unit (stub) |

### Modified Files

| File | Changes |
|---|---|
| `src/Precept/Language/Type.cs` | ContentValidation DU: add UcumValidation, MoneyValidation, QuantityValidation, PriceValidation, ExchangeRateValidation; update NodaTimeValidation with TemporalLiteralKind |
| `src/Precept/Language/Types.cs` | New ContentValidation instances for instant/timezone/zoneddatetime/duration/money/quantity/price/exchangerate; update existing temporal validations; replace unit/dimension frozen sets |
| `src/Precept/Language/CurrencyCatalog.cs` | Replace hardcoded array with lazy XML loader; `CurrencyEntry` gains `Symbol` (5th field); private `Symbols` dictionary merged at load time |
| `src/Precept/Pipeline/TypeChecker.Expressions.cs` | Delete inline validators; replace with TypedConstantValidation.Validate dispatch |
| `src/Precept/Precept.csproj` | Add EmbeddedResource entries for Iso4217 and UCUM XML files |
| `docs/compiler/literal-system.md` | Close open questions, add content validation table, add Restore to consumer matrix |
| `docs/runtime/runtime-api.md` | Fix CLR type table, fix Fire example, fix Deliberate Exclusions, add JSON wire format table |
| `docs/language/temporal-type-system.md` | Add canonical parser section, reconcile CLR types |
| `docs/language/catalog-system.md` | Add external reference data distinction, update consumer table |

### New Test Files

| File | Slice | Tests |
|---|---|---|
| `test/Precept.Tests/Language/CurrencyCatalogTests.cs` | 1c | Load count, known codes, exclusions, MinorUnit, symbol mappings, alpha-code fallback |
| `test/Precept.Tests/Language/CurrencyCatalogSyncTests.cs` | 1c | Embedded XML resource present and parseable |
| `test/Precept.Tests/Language/Ucum/UcumAtomCatalogTests.cs` | 1d | Load count, known atoms, dimension vectors, prefixability flags |
| `test/Precept.Tests/Language/Ucum/UcumPrefixCatalogTests.cs` | 1d | 20 SI prefixes, correct factors, longest-prefix-match |
| `test/Precept.Tests/Language/Ucum/UcumExactFactorTests.cs` | 1d | Multiply, divide, rational accuracy (no floating-point drift) |
| `test/Precept.Tests/Language/Ucum/DimensionVectorTests.cs` | 2 | Arithmetic, equality, dimensionless check, standard vector identities |
| `test/Precept.Tests/Language/Ucum/DimensionCatalogTests.cs` | 2 | All aliases present, vector lookups, `time` absent, `count` dimensionless |
| `test/Precept.Tests/Language/Ucum/UcumLexerTests.cs` | 3 | Simple atoms, prefixed, bracketed, dot/slash, exponents, annotations, error recovery |
| `test/Precept.Tests/Language/Ucum/UcumParserTests.cs` | 3 | Simple, prefixed, multiply, divide, exponents, grouping, annotations, complex, invalid |
| `test/Precept.Tests/Language/Ucum/UcumCatalogTests.cs` | 3 | Facade methods, Tier 1 browse, atom lookup, dimension vectors |
| `test/Precept.Tests/Language/Time/TemporalParserTests.cs` | 4 | All 7 forms: date, time, datetime, instant, zoneddatetime, timezone, quantity |
| `test/Precept.Tests/Language/Time/TemporalQuantityParserTests.cs` | 4 | Unit resolution, combined quantities, integer enforcement |
| `test/Precept.Tests/Language/Time/TemporalUnitsTests.cs` | 4 | All unit words recognized, classification correct |
| `test/Precept.Tests/Language/TypedConstantValidationTests.cs` | 6 | Dispatcher routes correctly, default arm returns Accepted |
| `test/Precept.Tests/Language/Time/TemporalValidatorTests.cs` | 7 | Each TemporalLiteralKind → correct TypedConstantParseResult |
| `test/Precept.Tests/Language/Ucum/UcumValidatorTests.cs` | 7 | Valid/invalid UCUM → correct results |
| `test/Precept.Tests/Language/ClosedSetValidatorTests.cs` | 7 | Membership checks, case sensitivity |
| `test/Precept.Tests/Language/RegexValidatorTests.cs` | 7 | Pattern matching, failure messages |
| `test/Precept.Tests/Language/CurrencyValidatorTests.cs` | 7 | Valid codes, invalid codes, case handling |
| `test/Precept.Tests/Language/MoneyValidatorTests.cs` | 7 | `100 USD` ✓, `50.25 EUR` ✓, `100` ✗, `100 XYZ` ✗ |
| `test/Precept.Tests/Language/QuantityValidatorTests.cs` | 7 | `5 kg` ✓, `2.5 mg/dL` ✓, `5 parsecs` ✗ |
| `test/Precept.Tests/Language/PriceValidatorTests.cs` | 7 | `4.17 USD/each` ✓, `10.00 EUR/kg` ✓ |
| `test/Precept.Tests/Language/ExchangeRateValidatorTests.cs` | 7 | `1.08 USD/EUR` ✓, `0.92 EUR/USD` ✓ |
| `test/Precept.Tests/Language/TypesTests.cs` | 8 | Every TypeMeta entry with ContentValidation, regression on date/time/datetime/period/currency |

**Note:** Slice 10 (runtime stubs) has no tests — stubs are `throw new NotImplementedException()`. Slices 11–12 (docs + deletion) have no tests.

### Deleted Files

| File | Reason |
|---|---|
| `docs/Working/frank-typed-literal-framework.md` | Superseded by canonical docs |
| `docs/Working/frank-ucum-iso-gap.md` | Superseded by canonical docs |
| `docs/Working/frank-typed-literal-gap-review.md` | Superseded by canonical docs |
| `docs/Working/frank-data-layer-decision.md` | Superseded by canonical docs |

---

## 6. End-State Verification

When this plan is complete, verify:

1. **Build passes:** `dotnet build` succeeds with no errors or warnings from new code
2. **All tests pass:** `dotnet test` — every existing test still passes, plus all new tests
3. **UCUM parser works:** `UcumCatalog.Parse("kg.m/s^2")` returns valid result with force dimension vector
4. **Temporal parser works:** `TemporalParser.Parse(TemporalLiteralKind.Date, "2026-04-15")` returns `LocalDate`
5. **Validation framework works:** `TypedConstantValidation.Validate(DateValidation, "2026-04-15", TypeKind.Date)` returns valid result with `LocalDate` value
6. **TypeChecker migration verified:** All typed constant validation in `TypeChecker.Expressions.cs` goes through `TypedConstantValidation.Validate` — no inline validator methods remain
7. **CurrencyCatalog loads from XML:** `CurrencyCatalog.All.Count` returns ~159
8. **UcumAtomCatalog loads from XML:** `UcumAtomCatalog.All.Count` returns ~300
9. **Canonical docs complete:** Every open question in `literal-system.md` is closed. `runtime-api.md` CLR type table is correct. No `DateOnly`/`TimeOnly`/`DateTimeOffset` references for temporal types anywhere in docs.
10. **Working docs deleted:** `docs/Working/` contains only `.gitkeep`
11. **Grammar generator:** No new token kinds needed — temporal and domain type keywords already in the Types catalog. Confirm by inspecting generated grammar output.
12. **No Working-doc labels in canonical docs:** No Q1/Q2/B1/G3 labels anywhere in canonical doc prose

---

## 7. Dependency Graph

```
Slice 1a (ISO XML)  ──┐
Slice 1b (UCUM XML) ──┤
Slice 1c (Currency) ──┤──→ Slice 2 (DimensionVector) ──→ Slice 3 (UCUM Parser) ──┐
Slice 1d (UCUM Data)──┘                                                          │
                                                                                  │
Slice 4 (Temporal Parser) ──────────────────────────────────────────────────────→ │
                                                                                  │
                                                                                  ├──→ Slice 5 (DU Update) ──→ Slice 6 (Framework) ──→ Slice 7 (Validators)
                                                                                  │                                                        │
                                                                                  │                                                        ▼
                                                                                  │                                               Slice 8 (TypeMeta)
                                                                                  │                                                        │
                                                                                  │                                                        ▼
                                                                                  │                                               Slice 9 (TypeChecker)
                                                                                  │
                                                                                  ├──→ Slice 10 (Runtime Stubs)
                                                                                  │
                                                                                  └──→ Slice 11 (Docs) ──→ Slice 12 (Delete Working)
```

**Parallelism:** Slices 1 and 4 can execute in parallel. Within Slice 1, sub-slices 1a/1b/1c/1d are partially parallel. Slice 1d is self-contained (the two-phase loader has its own mini-resolver — no dependency on Slice 3). Slice 10 is independent of most other slices.
