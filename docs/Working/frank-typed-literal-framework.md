# Typed Literal Parsing — Architecture Analysis

## Status

| Property | Value |
|---|---|
| Author | Frank (Lead/Architect) |
| Date | 2026-05-09T11:11:01-04:00 |
| Branch | spike |
| Scope | Temporal literal parsing, unified typed literal framework, catalog integration |
| Grounding | `docs/compiler/literal-system.md`, `docs/language/temporal-type-system.md`, `docs/working/frank-ucum-iso-gap.md`, `src/Precept/Language/Type.cs`, `src/Precept/Language/Types.cs` |

---

## Q1: Temporal Literal Parsing

### Does Precept need a shared NodaTime-based temporal literal parser?

**Yes. Unequivocally.**

The UCUM architecture established a shared parser in `src/Precept/Language/Ucum/` consumed by the type checker, language server, runtime evaluator, MCP, and arg validation. Time was explicitly excluded from UCUM because temporal values are a different domain with a different backing library (NodaTime vs. UCUM). That exclusion does not mean temporal parsing stays ad-hoc — it means temporal parsing gets its own shared subsystem, architecturally parallel to UCUM.

**The current state is already a bug in progress.** Look at `TypeChecker.Expressions.cs` lines 217–256. The `ValidateNodaTime` method dispatches on raw pattern strings:

```csharp
if (noda.NodaTimePattern == "uuuu'-'MM'-'dd")     // date
if (noda.NodaTimePattern == "HH':'mm':'ss")        // time
if (noda.NodaTimePattern == "uuuu'-'MM'-'dd'T'HH':'mm':'ss")  // datetime
if (noda.NodaTimePattern == "NormalizingIso")       // period
```

This is a raw string-matching dispatch that:
1. Lives only in the type checker — no other consumer can reuse it.
2. Does not handle `instant` (`'2026-04-13T14:30:00Z'`) — there is no `NodaTimeValidation` entry for `instant`.
3. Does not handle `timezone` (`'America/New_York'`) — no validation at all.
4. Does not handle `zoneddatetime` (`'2026-04-13T14:30:00[America/New_York]'`) — no validation.
5. Does not handle `duration` literal quantities (`'72 hours'`, `'2 hours + 30 minutes'`) — these are documented in the temporal-type-system spec but have no parsing path.
6. Does not handle `period` literal quantities (`'30 days'`, `'2 years + 6 months'`) — the spec says quantities use the `<integer> <unit>` form with `+` combination, but the current path only handles ISO 8601 period format (`P30D`, `P1Y6M`).
7. Returns `object?` — the evaluator, MCP, and language server need typed NodaTime objects, not boxed unknowns.

**The gap is worse than UCUM's was.** UCUM had a closed-set placeholder that at least covered the 58 most common units. Temporal parsing has pattern-string dispatch that misses three of eight temporal types entirely and doesn't handle the primary authoring form (quantity syntax) at all.

### Where does it live?

**`src/Precept/Language/Time/`** — parallel to `src/Precept/Language/Ucum/`.

The subsystem belongs beside the language catalogs as a shared language-level component. It must NOT live only in `Runtime/` (compile-time validation, language server, and MCP need it pre-evaluation). It must NOT live in tooling (tooling is a consumer, not the authority).

### What does it parse?

Seven distinct literal forms, mapped to NodaTime types:

| Literal form | Example content | NodaTime type | Parser method |
|---|---|---|---|
| Date | `2026-04-15` | `LocalDate` | `LocalDatePattern.Iso` |
| Time | `14:30:00` or `14:30` | `LocalTime` | `LocalTimePattern.ExtendedIso` with `HH:mm` fallback |
| DateTime | `2026-04-15T14:30:00` | `LocalDateTime` | `LocalDateTimePattern.ExtendedIso` |
| Instant | `2026-04-15T14:30:00Z` | `Instant` | `InstantPattern.ExtendedIso` |
| ZonedDateTime | `2026-04-15T14:30:00[America/New_York]` | `ZonedDateTime` | Custom: parse datetime portion + bracket-enclosed IANA TZ |
| Timezone | `America/New_York` | `DateTimeZone` | `DateTimeZoneProviders.Tzdb.GetZoneOrNull(...)` |
| Temporal quantity | `30 days`, `72 hours`, `2 years + 6 months` | `Period` or `Duration` (context-dependent) | Custom parser: `<integer> <unit>` with `+` combination |

### Format strings / NodaTime type mappings

| Precept type | NodaTime backing | Parse pattern | Canonical serialization |
|---|---|---|---|
| `date` | `LocalDate` | `uuuu'-'MM'-'dd` | `2026-04-15` |
| `time` | `LocalTime` | `HH':'mm':'ss` (also `HH':'mm`) | `14:30:00` |
| `datetime` | `LocalDateTime` | `uuuu'-'MM'-'dd'T'HH':'mm':'ss` | `2026-04-15T14:30:00` |
| `instant` | `Instant` | `uuuu'-'MM'-'dd'T'HH':'mm':'ss'Z'` | `2026-04-15T14:30:00Z` |
| `zoneddatetime` | `ZonedDateTime` | Custom bracket format | `2026-04-15T14:30:00[America/New_York]` |
| `timezone` | `DateTimeZone` | IANA TZ ID lookup | `America/New_York` |
| `duration` | `Duration` | Quantity: `<int> <unit> [+ <int> <unit>]*` | NodaTime round-trip |
| `period` | `Period` | Quantity: `<int> <unit> [+ <int> <unit>]*` | NodaTime normalizing ISO |

### Consumer matrix

| Consumer | How it uses the temporal parser |
|---|---|
| **Type checker** (`TypeChecker.Expressions.cs`) | `ResolveTypedConstant` dispatches to the temporal parser via `ContentValidation`. Gets back a strongly-typed `TemporalParseResult` with the NodaTime value, or diagnostics. Replaces the current raw `ValidateNodaTime` method. |
| **Language server — validation** | Same parser in tolerant mode for real-time diagnostics as the author types. No second validator. |
| **Language server — hover** | Parsed temporal value displayed on hover: `'2026-04-15'` → "date: April 15, 2026 (Tuesday)" |
| **Language server — completions** | After `as date default '`, suggest `YYYY-MM-DD`. After `'30 `, suggest temporal unit names. |
| **Runtime evaluator** | Materializes typed NodaTime values from `TypedTypedConstant` nodes. The parser result IS the materialized value — no re-parsing. |
| **MCP `precept_compile`** | Surfaces parsed temporal metadata in typed constant output — the value, format, validation status. |
| **MCP `precept_fire` / `precept_update`** | Arg validation for temporal field values — parse incoming JSON temporal strings through the same parser before evaluator execution. |

### Proposed physical layout

| File | Type | Responsibility |
|---|---|---|
| `src/Precept/Language/Time/TemporalParser.cs` | `static class TemporalParser` | Entry point. Dispatches by `TypeKind` to specific parse methods. |
| `src/Precept/Language/Time/TemporalParseResult.cs` | `readonly record struct TemporalParseResult` | Success/failure wrapper: `NodaTime` value, diagnostics, canonical text. |
| `src/Precept/Language/Time/QuantityParser.cs` | `static class TemporalQuantityParser` | Parses `<integer> <unit> [+ <integer> <unit>]*` for duration/period quantities. |
| `src/Precept/Language/Time/TemporalUnits.cs` | `static class TemporalUnits` | Unit name table: `days`→calendar, `hours`→timeline, `months`→always-period, etc. Context-dependent resolution metadata. |
| `src/Precept/Language/Time/TemporalDiagnostic.cs` | `sealed record TemporalDiagnostic` | Parser diagnostic with span, code, message, and teachable suggestion. |

---

## Q2: Unified Pluggable Framework — Recommendation

### Decision: APPROVED — with a critical constraint.

The unified framework is the `ContentValidation` DU on `TypeMeta`, resolved through the `ITypedConstantValidator` interface already named in `docs/compiler/literal-system.md`. The literal system doc already answered this question — it just hasn't been implemented yet. What Shane is asking about is the implementation shape.

### Rationale

The three typed-literal domains (temporal, currency/money, UCUM/quantity) share an identical lifecycle:

1. **Context determines the type** — the type checker propagates `expectedType` inward.
2. **Content is parsed and validated** — the parser for that type checks whether the raw text is valid.
3. **A structured result is returned** — containing the typed value, canonical text, and diagnostics.
4. **Multiple consumers need the same parser** — type checker, language server, evaluator, MCP, runtime arg validation.

This lifecycle IS the framework. The pluggable interface is not an abstraction fetish — it is the minimum contract that prevents six consumers from each building their own dispatch logic.

### What the unified interface looks like

```csharp
namespace Precept.Language;

/// <summary>
/// Validates and parses the raw text content of a typed constant ('...') against
/// a specific type. Registered per-TypeKind via TypeMeta.ContentValidation.
/// Consumed by the type checker, language server, evaluator, MCP, and runtime arg validation.
/// </summary>
public interface ITypedConstantValidator
{
    /// <summary>
    /// Parse and validate raw typed-constant text for the given type context.
    /// </summary>
    /// <param name="rawText">The text between single quotes, after interpolation substitution.</param>
    /// <param name="targetType">The TypeKind determined by expression context.</param>
    /// <param name="context">Optional context for resolution (e.g., temporal quantity context
    /// for duration-vs-period disambiguation).</param>
    /// <returns>A result containing the parsed value, canonical text, and diagnostics.</returns>
    TypedConstantParseResult Validate(string rawText, TypeKind targetType, TypedConstantContext? context = null);
}

/// <summary>
/// Result of typed constant validation. Structurally identical role to UcumParseResult
/// and TemporalParseResult — but this is the consumer-facing boundary type used by
/// the type checker and all downstream consumers.
/// </summary>
public readonly record struct TypedConstantParseResult(
    bool IsValid,
    /// <summary>The parsed CLR value (NodaTime type, decimal, string, etc.). Null on failure.</summary>
    object? Value,
    /// <summary>Canonical text representation of the parsed value.</summary>
    string? CanonicalText,
    /// <summary>Human-readable format description for error messages.</summary>
    string FormatDescription,
    /// <summary>Diagnostics with teachable messages. Empty on success.</summary>
    IReadOnlyList<TypedConstantDiagnostic> Diagnostics
);

/// <summary>
/// A diagnostic produced during typed-constant validation.
/// </summary>
public sealed record TypedConstantDiagnostic(
    string Code,
    string Message,
    /// <summary>Optional suggestion for the author — teachable error messages.</summary>
    string? Suggestion = null
);

/// <summary>
/// Context passed to validators for disambiguation. The type checker populates this
/// from the enclosing expression. For temporal quantities, this carries whether the
/// context is calendar (date ±) or timeline (instant ±).
/// </summary>
public sealed record TypedConstantContext(
    /// <summary>The type of the expression on the other side of the operator, if any.</summary>
    TypeKind? PeerType = null,
    /// <summary>The operator connecting the typed constant to its peer, if any.</summary>
    OperatorKind? Operator = null
);
```

### Where does it live architecturally?

**The interface and result types live in `src/Precept/Language/`** — beside `Type.cs` and `Types.cs`. They are language-level contracts.

**The domain-specific validators live in their domain folders:**

| Domain | Validator location | Validator class |
|---|---|---|
| Temporal | `src/Precept/Language/Time/TemporalValidator.cs` | `TemporalValidator : ITypedConstantValidator` |
| UCUM (unit/quantity/dimension) | `src/Precept/Language/Ucum/UcumValidator.cs` | `UcumValidator : ITypedConstantValidator` |
| Currency | `src/Precept/Language/CurrencyValidator.cs` | `CurrencyValidator : ITypedConstantValidator` |
| Money | `src/Precept/Language/MoneyValidator.cs` | `MoneyValidator : ITypedConstantValidator` |
| Price | `src/Precept/Language/PriceValidator.cs` | `PriceValidator : ITypedConstantValidator` |
| Exchange Rate | `src/Precept/Language/ExchangeRateValidator.cs` | `ExchangeRateValidator : ITypedConstantValidator` |

There is **no** `src/Precept/Language/TypedLiterals/` folder. The interface lives in `Language/` directly. The implementations live in their domain folders. This avoids an unnecessary indirection layer — the domains already exist as organizational units.

### How does the type checker wire it up?

Via `TypeMeta.ContentValidation`. This is the catalog hook.

The current `ContentValidation` DU (`RegexValidation`, `NodaTimeValidation`, `ClosedSetValidation`) is **replaced** by a single property:

```csharp
// On TypeMeta:
ITypedConstantValidator? ContentValidator { get; }
```

Wait — that breaks the catalog-as-data principle. The DU carries declarative metadata; an interface carries executable code. The catalog is supposed to be the language specification in machine-readable form, not a bag of strategy objects.

**Revised approach — keep the DU, add a resolver:**

The `ContentValidation` DU stays as declarative metadata on `TypeMeta`. A **static resolver** maps DU instances to validator implementations. This preserves the catalog's data-only nature while giving consumers a single dispatch path.

```csharp
// In src/Precept/Language/TypedConstantValidation.cs

namespace Precept.Language;

/// <summary>
/// Resolves a ContentValidation descriptor to its ITypedConstantValidator implementation.
/// This is the single dispatch point — every consumer calls this instead of
/// pattern-matching on the DU themselves.
/// </summary>
public static class TypedConstantValidation
{
    /// <summary>
    /// Validate typed constant content using the validator registered for the given
    /// ContentValidation descriptor.
    /// </summary>
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
        QuantityValidation qty => QuantityValidator.Validate(rawText, targetType, qty, context),
        _ => TypedConstantParseResult.Accepted(rawText),
    };
}
```

**This replaces `ValidateContent` in `TypeChecker.Expressions.cs`.** The type checker's `ResolveTypedConstant` method calls `TypedConstantValidation.Validate(cv, rawText, targetType, context)` instead of its own inline dispatch. Every other consumer does the same.

### ContentValidation DU evolution

The DU gains new subtypes for the domains that need richer metadata than the current three provide:

```csharp
// New subtypes added to the ContentValidation DU in Type.cs:

/// <summary>
/// Validates typed constant content as a UCUM unit expression.
/// Replaces ClosedSetValidation for unitofmeasure.
/// </summary>
public sealed record UcumValidation(
    string FormatDescription, string[] Examples
) : ContentValidation(FormatDescription, Examples);

/// <summary>
/// Validates typed constant content as a compound value: magnitude + domain identifier.
/// Used for money ('100 USD'), quantity ('5 kg'), price ('4.17 USD/each'),
/// and exchange rate ('1.08 USD/EUR').
/// </summary>
public sealed record QuantityValidation(
    QuantityDomain Domain,
    string FormatDescription,
    string[] Examples
) : ContentValidation(FormatDescription, Examples);

public enum QuantityDomain
{
    Money,       // <decimal> <ISO-4217>
    Quantity,    // <decimal> <UCUM-unit>
    Price,       // <decimal> <ISO-4217>/<UCUM-unit>
    ExchangeRate // <decimal> <ISO-4217>/<ISO-4217>
}
```

`NodaTimeValidation` stays but gains a `TemporalLiteralKind` discriminator to replace the pattern-string dispatch:

```csharp
/// <summary>
/// Validates typed constant content by parsing as a NodaTime temporal type.
/// The Kind discriminator replaces pattern-string matching for dispatch.
/// </summary>
public sealed record NodaTimeValidation(
    TemporalLiteralKind LiteralKind,
    string NodaTimePattern,
    string FormatDescription,
    string[] Examples
) : ContentValidation(FormatDescription, Examples);

public enum TemporalLiteralKind
{
    Date,
    Time,
    DateTime,
    Instant,
    ZonedDateTime,
    Timezone,
    TemporalQuantity,  // duration/period quantity form: '30 days', '72 hours'
}
```

### How does the language server use it?

**(a) Real-time validation diagnostics:** The language server calls `TypedConstantValidation.Validate(...)` with the same `ContentValidation` metadata the type checker uses. No second validator. The parse result's `Diagnostics` list contains teachable messages that become squiggles in the editor.

**(b) Hover showing parsed value:** The validator returns a structured `Value` (e.g., `NodaTime.LocalDate`). The language server's hover provider formats this: `'2026-04-15'` → "**date**: April 15, 2026 (Tuesday)". For quantities: `'72 hours'` → "**duration**: 72 hours (3 days)". For money: `'100 USD'` → "**money**: $100.00 (US Dollar)".

**(c) Completions suggesting valid values:** The `ContentValidation` DU carries `Examples` on every subtype. The language server uses these as completion snippets after `default '`. For temporal types, it also suggests the format pattern. For UCUM, it suggests Tier 1 atoms. For currency, it suggests ISO 4217 codes from `CurrencyCatalog`.

### How does the runtime evaluator use it?

The evaluator receives `TypedTypedConstant` AST nodes from the type checker. These nodes already carry the parsed value from the validation pass (the `parsedValue` that today is `object?`). The evaluator does NOT re-parse — it unwraps the typed value and wraps it in the appropriate runtime representation.

When the type checker stores a `TypedConstantParseResult.Value` on the typed node, the evaluator reads it directly:
- `LocalDate` → runtime date value
- `Duration` → runtime duration value
- Parsed money struct → runtime money value

No re-parsing. One parser, one parse, multiple consumers.

### How does MCP tooling use it?

**`precept_compile`:** Surfaces `TypedConstantParseResult` metadata on typed-constant nodes in the compilation output — the canonical text, format description, and validation diagnostics. This gives the AI consumer structured information about what each literal means.

**`precept_fire` / `precept_update` arg validation:** When the runtime boundary receives temporal, currency, or unit values from JSON, it validates them through the same `TypedConstantValidation.Validate(...)` path. The arg value must pass the same validation that a source literal would pass. If the runtime accepts a value the compiler wouldn't, that is a bug.

### Concrete C# shape — method-level signatures

```csharp
// ── Entry point (replaces TypeChecker.ValidateContent) ─────────────

namespace Precept.Language;

public static class TypedConstantValidation
{
    public static TypedConstantParseResult Validate(
        ContentValidation validation,
        string rawText,
        TypeKind targetType,
        TypedConstantContext? context = null);
}

// ── Temporal validator ─────────────────────────────────────────────

namespace Precept.Language.Time;

internal static class TemporalValidator
{
    public static TypedConstantParseResult Validate(
        string rawText,
        TypeKind targetType,
        NodaTimeValidation validation,
        TypedConstantContext? context);

    // Internal dispatch:
    private static TypedConstantParseResult ParseDate(string rawText);
    private static TypedConstantParseResult ParseTime(string rawText);
    private static TypedConstantParseResult ParseDateTime(string rawText);
    private static TypedConstantParseResult ParseInstant(string rawText);
    private static TypedConstantParseResult ParseZonedDateTime(string rawText);
    private static TypedConstantParseResult ParseTimezone(string rawText);
    private static TypedConstantParseResult ParseTemporalQuantity(
        string rawText, TypeKind targetType, TypedConstantContext? context);
}

// ── UCUM validator ─────────────────────────────────────────────────

namespace Precept.Language.Ucum;

internal static class UcumValidator
{
    public static TypedConstantParseResult Validate(
        string rawText,
        TypeKind targetType,
        UcumValidation validation,
        TypedConstantContext? context);
}

// ── Closed-set validator ───────────────────────────────────────────

namespace Precept.Language;

internal static class ClosedSetValidator
{
    public static TypedConstantParseResult Validate(
        string rawText,
        ClosedSetValidation validation);
}

// ── Regex validator ────────────────────────────────────────────────

namespace Precept.Language;

internal static class RegexValidator
{
    public static TypedConstantParseResult Validate(
        string rawText,
        RegexValidation validation);
}

// ── Compound quantity validator ────────────────────────────────────

namespace Precept.Language;

internal static class QuantityValidator
{
    public static TypedConstantParseResult Validate(
        string rawText,
        TypeKind targetType,
        QuantityValidation validation,
        TypedConstantContext? context);
}
```

---

## Q3: Catalog Integration — ContentValidation as the Plugin Hook

### Decision: ContentValidation IS the catalog hook. No separate registry.

The `ContentValidation` DU on `TypeMeta` is the catalog entry point for typed literal validation. The `TypedConstantValidation.Validate(...)` static dispatcher is the single code path that resolves a DU entry to its validator. There is no `ITypedConstantValidator` registry, no service locator, no DI container, no plugin discovery.

### Why no separate registry?

The literal-system doc names `ITypedConstantValidator` as a registration API with an open question about the registration surface. I am closing that question now:

**There is no runtime extensibility requirement.** Precept is a closed language. The set of types is defined by the catalogs. The set of typed-literal validators is 1:1 with the `ContentValidation` DU subtypes. When a new type is added, a new DU subtype is added, and a new case is added to `TypedConstantValidation.Validate`. This is the catalog pattern — the DU is the spec, the switch is the machinery.

A registry/plugin approach is needed when:
- Third parties add types at runtime — Precept does not support this.
- The set of validators is unknown at compile time — it is known.
- Different deployments need different validators — they don't.

The `ITypedConstantValidator` interface as named in the literal-system doc becomes an **internal implementation interface** if validators want to share structure, or it disappears entirely in favor of static methods. I recommend the latter — static methods on per-domain validator classes, dispatched by the static `TypedConstantValidation.Validate` switch. No interface overhead.

### What about the literal-system doc's open question?

The open question reads:

> The literal system defines `ITypedConstantValidator` as the extension hook for typed-constant validation, but the registration surface is still undecided.

**Answer:** The registration surface is `ContentValidation` on `TypeMeta`. A type declares its validation strategy as a `ContentValidation` DU entry. The dispatcher resolves DU subtypes to validator implementations. There is no separate registration API. The DU IS the registry.

The `ITypedConstantValidator` name in the literal-system doc should be updated to reflect this. It was a placeholder for the shape of the answer. The shape of the answer is: **catalog metadata → static dispatch → domain validator**. No interface needed.

### Does this foreclose future extensibility?

No. If Precept ever needs a type whose validator cannot be described by an existing DU subtype, a new subtype is added. That is the catalog-driven evolution model. Adding a DU subtype is a source-compatible change that adds a new arm to the switch. It is the same evolution pattern used for `TokenMeta`, `ConstructMeta`, and every other catalog.

### The architecture in one sentence

**`TypeMeta.ContentValidation` (catalog metadata) → `TypedConstantValidation.Validate(...)` (static dispatcher) → domain-specific validator (static methods) → `TypedConstantParseResult` (structured result consumed by all six consumers).**

---

## Proposed Architecture

### Folder structure

```
src/Precept/Language/
├── Type.cs                          # ContentValidation DU (updated with new subtypes)
├── Types.cs                         # TypeMeta entries (updated with new validation instances)
├── TypedConstantValidation.cs       # Static dispatcher — the framework entry point
├── TypedConstantParseResult.cs      # Result type, diagnostic type, context type
├── ClosedSetValidator.cs            # ClosedSetValidation → result
├── RegexValidator.cs                # RegexValidation → result
├── CurrencyValidator.cs             # Currency code validation (uses CurrencyCatalog)
├── QuantityValidator.cs             # Compound quantity validation (money, qty, price, exrate)
├── Time/
│   ├── TemporalValidator.cs         # NodaTimeValidation → result (all 7 temporal forms)
│   ├── TemporalQuantityParser.cs    # Parses '<int> <unit> [+ <int> <unit>]*'
│   ├── TemporalUnits.cs             # Unit name table with context-dependent resolution
│   ├── TemporalParseResult.cs       # Domain-internal result (wraps NodaTime value)
│   └── TemporalDiagnostic.cs        # Teachable diagnostics
├── Ucum/
│   ├── UcumValidator.cs             # UcumValidation → result (wraps UcumCatalog.Parse)
│   └── ... (existing UCUM subsystem)
```

### Consumer wiring

#### Type checker (compile-time)

```csharp
// In TypeChecker.Expressions.cs — ResolveTypedConstant
private static TypedExpression ResolveTypedConstant(LiteralExpression lit, CheckContext ctx, TypeKind? expectedType)
{
    // ... existing context resolution ...

    var meta = Types.GetMeta(targetType);
    var cv = meta.ContentValidation;

    if (cv is null)
        return new TypedTypedConstant(targetType, rawText, rawText, lit.Span);

    // NEW: single dispatch through the framework
    var peerContext = new TypedConstantContext(ctx.PeerType, ctx.PeerOperator);
    var result = TypedConstantValidation.Validate(cv, rawText, targetType, peerContext);

    if (result.IsValid)
        return new TypedTypedConstant(targetType, rawText, result.Value, lit.Span);

    foreach (var diag in result.Diagnostics)
        ctx.Diagnostics.Add(Diagnostic.Create(DiagnosticCode.InvalidTypedConstant,
            lit.Span, rawText, diag.Message));

    return new TypedErrorExpression(lit.Span);
}
```

The old `ValidateContent`, `ValidateNodaTime`, `ValidateClosedSet`, and `ValidateRegex` methods in `TypeChecker.Expressions.cs` are deleted. All validation goes through `TypedConstantValidation.Validate`.

#### Language server (diagnostics + hover + completions)

```csharp
// Validation — same parser, no second path
var result = TypedConstantValidation.Validate(meta.ContentValidation, content, targetType, context);
// result.Diagnostics → squiggles in editor

// Hover
if (result.IsValid)
{
    var hoverText = result.Value switch
    {
        LocalDate d => $"**{meta.DisplayName}**: {d:MMMM d, yyyy} ({d.DayOfWeek})",
        Duration dur => $"**duration**: {dur}",
        Period per => $"**period**: {per}",
        // ... etc
    };
}

// Completions — from ContentValidation.Examples
var examples = meta.ContentValidation?.Examples ?? [];
```

#### Evaluator (value materialization)

The evaluator reads the pre-parsed value from the typed AST node. No re-parsing:

```csharp
// TypedTypedConstant already carries result.Value from the type checker pass
var materializedValue = typedConstant.ParsedValue switch
{
    LocalDate d => new PreceptDateValue(d),
    LocalTime t => new PreceptTimeValue(t),
    Duration dur => new PreceptDurationValue(dur),
    Period per => new PreceptPeriodValue(per),
    Instant inst => new PreceptInstantValue(inst),
    // ... etc
};
```

#### MCP (compile output + arg validation)

```csharp
// precept_compile — project validation metadata
var fieldDto = new FieldDto
{
    Default = new TypedConstantDto
    {
        RawText = rawText,
        CanonicalText = result.CanonicalText,
        IsValid = result.IsValid,
        Diagnostics = result.Diagnostics,
    }
};

// precept_fire / precept_update — arg validation at runtime boundary
var meta = Types.GetMeta(field.TypeKind);
if (meta.ContentValidation is not null)
{
    var result = TypedConstantValidation.Validate(meta.ContentValidation, incomingValue, field.TypeKind);
    if (!result.IsValid)
        throw new PreceptValidationException(result.Diagnostics);
}
```

### TypeMeta entries — updated

The `Types.cs` entries for temporal types update their `ContentValidation` to use the enriched `NodaTimeValidation` with `TemporalLiteralKind`:

```csharp
// Date — existing, updated with LiteralKind
private static readonly NodaTimeValidation DateValidation = new(
    TemporalLiteralKind.Date,
    "uuuu'-'MM'-'dd",
    "ISO 8601 date (YYYY-MM-DD)",
    ["2026-01-15", "2024-12-31"]);

// Instant — NEW (currently missing!)
private static readonly NodaTimeValidation InstantValidation = new(
    TemporalLiteralKind.Instant,
    "uuuu'-'MM'-'dd'T'HH':'mm':'ss'Z'",
    "ISO 8601 UTC instant (YYYY-MM-DDThh:mm:ssZ)",
    ["2026-04-13T14:30:00Z", "2024-12-31T23:59:59Z"]);

// Timezone — NEW (currently missing!)
private static readonly NodaTimeValidation TimezoneValidation = new(
    TemporalLiteralKind.Timezone,
    "IANA",
    "IANA timezone identifier",
    ["America/New_York", "Europe/London", "Asia/Tokyo"]);

// ZonedDateTime — NEW (currently missing!)
private static readonly NodaTimeValidation ZonedDateTimeValidation = new(
    TemporalLiteralKind.ZonedDateTime,
    "uuuu'-'MM'-'dd'T'HH':'mm':'ss'['tz']'",
    "ISO 8601 datetime with IANA timezone",
    ["2026-04-13T14:30:00[America/New_York]"]);

// Duration — NEW (quantity form!)
private static readonly NodaTimeValidation DurationValidation = new(
    TemporalLiteralKind.TemporalQuantity,
    "quantity",
    "Temporal quantity: <integer> <unit> [+ <integer> <unit>]*",
    ["72 hours", "2 hours + 30 minutes", "3600 seconds"]);
```

The `unitofmeasure` entry updates from `ClosedSetValidation` to `UcumValidation`:

```csharp
private static readonly UcumValidation UnitOfMeasureValidationNew = new(
    "UCUM expression",
    ["kg", "m/s^2", "mg/dL"]);
```

The `money` entry gains a `QuantityValidation`:

```csharp
// Currently money has no ContentValidation — it gains one
private static readonly QuantityValidation MoneyValidation = new(
    QuantityDomain.Money,
    "Monetary amount: <decimal> <ISO-4217>",
    ["100 USD", "50.25 EUR"]);
```

---

## Implementation Scope

### What code needs to be written

| Slice | Files | Effort | Dependencies |
|---|---|---|---|
| **1. Framework types** | `TypedConstantParseResult.cs`, `TypedConstantValidation.cs` | Small | None |
| **2. Migrate existing validators** | Update `ClosedSetValidator.cs`, `RegexValidator.cs` to return `TypedConstantParseResult`. Delete `ValidateContent`/`ValidateNodaTime`/`ValidateClosedSet`/`ValidateRegex` from `TypeChecker.Expressions.cs`. Wire `ResolveTypedConstant` to `TypedConstantValidation.Validate`. | Medium | Slice 1 |
| **3. Temporal parser** | `Time/TemporalValidator.cs`, `Time/TemporalQuantityParser.cs`, `Time/TemporalUnits.cs`, `Time/TemporalDiagnostic.cs`. Parse all 7 temporal literal forms. | Large | Slice 1 |
| **4. ContentValidation DU update** | Add `UcumValidation`, `QuantityValidation` subtypes. Add `TemporalLiteralKind` enum. Update `NodaTimeValidation` with `LiteralKind` discriminator. | Small | Slice 1 |
| **5. TypeMeta entries** | Add `InstantValidation`, `TimezoneValidation`, `ZonedDateTimeValidation`, `DurationValidation` to `Types.cs`. Update existing temporal entries with `LiteralKind`. | Small | Slices 3, 4 |
| **6. UCUM validator wrapper** | `Ucum/UcumValidator.cs` wrapping `UcumCatalog.Parse` in `TypedConstantParseResult`. | Small | Slice 1, UCUM parser |
| **7. Compound quantity validators** | `QuantityValidator.cs`, `MoneyValidator.cs`, `PriceValidator.cs`, `ExchangeRateValidator.cs`. | Medium | Slices 1, 6 |
| **8. Language server integration** | Update hover, completions, and diagnostics to use `TypedConstantValidation.Validate`. | Medium | Slices 1–5 |
| **9. MCP integration** | Update `CompileTool.cs` to surface parsed-value metadata. Update arg validation paths. | Small | Slices 1–5 |

### Who implements it

George. The architecture is fully specified here — method signatures, file locations, consumer wiring, DU evolution, and the migration path from the current inline validators to the framework. No further design questions should be necessary.

### Implementation order

Slices 1 → 2 → 4 → 3 → 5 → 6 → 7 → 8 → 9.

Slice 2 (migration) should go first after the framework types because it proves the framework works with existing validators before adding new ones. Slice 3 (temporal parser) is the largest piece and the primary value delivery.

---

## Literal-System Doc Sync

The literal-system doc's open question about `ITypedConstantValidator` registration is answered by this analysis:

- **Registration surface:** `ContentValidation` DU on `TypeMeta`.
- **Dispatch:** `TypedConstantValidation.Validate(...)` static switch on DU subtypes.
- **No interface:** Static methods per domain validator, not an `ITypedConstantValidator` interface.
- **No registry:** The DU is the registry. Adding a type means adding a DU subtype and a switch arm.

The literal-system doc should be updated to reflect this decision when the implementation lands.

---

## Q4: Runtime Arg Parsing for Typed Literal Types

### The gap

The framework above covers **compile-time validation of literals** and **evaluator materialization of pre-parsed AST values**. It does NOT cover the runtime ingress path: when a caller fires an event via `Version.Fire(...)` or `Precept.Create(...)`, arg values arrive as raw data — `JsonElement` (JSON lane) or typed CLR values via `IArgBuilder.Set<T>` (typed lane). If an event declares `arg LoadedAt as datetime` or `arg Weight as quantity`, the runtime must parse/coerce those incoming values into their typed `PreceptValue` form before the evaluator can use them in guard expressions and action chains.

This is a real gap. Today's arg types are limited to primitives (`string`, `number`, `decimal`, `boolean`) where `TypeRuntime<T>.FromJson` / `FromClr` handles the conversion trivially. The moment Money, DateTime, Duration, Quantity, or any other typed-literal type becomes legal in an `ArgDecl` position, the runtime ingress must know how to parse structured value representations — and it must reject invalid ones with the same rigor the compiler applies to source literals.

### Decision: No separate "arg coercion" contract. `TypeRuntime<T>` IS the runtime arg parsing path.

The typed literal framework has **two parsing surfaces**, and they are architecturally distinct:

| Surface | When | Input | Parser | Output |
|---|---|---|---|---|
| **Compile-time validation** | Type checker processes a `'...'` literal in source | Raw text between quotes | `TypedConstantValidation.Validate(ContentValidation, rawText, TypeKind)` | `TypedConstantParseResult` with typed value + diagnostics |
| **Runtime arg ingress** | `Fire` / `Create` receives arg values from callers | `JsonElement` (JSON lane) or `T` (typed lane) | `TypeRuntime<T>.FromJson` / `FromClr` | `PreceptValue` |

These are NOT the same operation. The compile-time path parses DSL literal syntax (`'2026-04-15'`, `'100 USD'`, `'5 kg'`). The runtime path parses wire-format data (JSON values) or accepts pre-typed CLR values. The input shapes, error models, and performance constraints are fundamentally different.

**`ITypedConstantValidator` / `TypedConstantValidation.Validate` is NOT invoked during event firing.** It is a compile-time facility. Reusing it at runtime would mean:

1. Requiring callers to format values as DSL literal strings (`"2026-04-15"` as a string, not a JSON date), which defeats the purpose of a typed API.
2. Paying parse costs that the wire format already avoids (JSON carries structured dates, not Precept literal syntax).
3. Conflating compile-time diagnostics (with spans, suggestions, teachable messages) with runtime validation errors (which need programmatic error codes and no source spans).

### How runtime arg parsing actually works — per lane

#### JSON lane (`JsonElement`)

When `Version.Fire("loadParcel", jsonArgs)` is called, the Fire boundary iterates the event's `ArgDescriptor` list. For each declared arg, it reads the corresponding property from the `JsonElement` and converts it via `TypeRuntimeMeta.ReadJson` — the same delegate registered on each `TypeMeta` entry in the Types catalog.

For typed-literal types, `ReadJson` handles the wire representation:

| Precept type | JSON wire format | `ReadJson` behavior |
|---|---|---|
| `date` | `"2026-04-15"` (ISO 8601 string) | Parse via `LocalDatePattern.Iso` → `PreceptValue` |
| `time` | `"14:30:00"` (ISO 8601 string) | Parse via `LocalTimePattern.ExtendedIso` → `PreceptValue` |
| `datetime` | `"2026-04-15T14:30:00"` (ISO 8601 string) | Parse via `LocalDateTimePattern.ExtendedIso` → `PreceptValue` |
| `instant` | `"2026-04-15T14:30:00Z"` (ISO 8601 string) | Parse via `InstantPattern.ExtendedIso` → `PreceptValue` |
| `duration` | `"PT72H"` or `"72:00:00"` (ISO 8601 / .NET) | Parse via `DurationPattern` → `PreceptValue` |
| `period` | `"P30D"` or `"P2Y6M"` (ISO 8601) | Parse via `PeriodPattern.NormalizingIso` → `PreceptValue` |
| `money` | `{"amount": 100.00, "currency": "USD"}` (structured) | Read decimal + currency code → `PreceptValue` |
| `quantity` | `{"magnitude": 5.0, "unit": "kg"}` (structured) | Read decimal + UCUM unit → `PreceptValue` |
| `currency` | `"USD"` (ISO 4217 string) | Validate against `CurrencyCatalog` → `PreceptValue` |
| `unitofmeasure` | `"kg"` (UCUM string) | Validate against UCUM parser → `PreceptValue` |
| `timezone` | `"America/New_York"` (IANA string) | Validate against TZDB → `PreceptValue` |

**The JSON wire format is NOT the DSL literal format.** DSL literals use `'100 USD'` (compound string). JSON args use `{"amount": 100.00, "currency": "USD"}` (structured object). This is deliberate — JSON callers should not have to learn Precept literal syntax. The wire format uses the canonical JSON representation for each domain.

**Parsing failures in the JSON lane** produce `EventOutcome.InvalidArgs` — the same outcome that fires today for missing required args or unknown arg names. The `InvalidArgs.Reason` string describes what failed: `"Arg 'LoadedAt': invalid date format — expected ISO 8601 (YYYY-MM-DD), got '2026/04/15'"`. This is a structured rejection at the Fire boundary, before the evaluator runs. No exceptions are thrown — `InvalidArgs` is a first-class outcome variant.

#### Typed lane (`Action<IArgBuilder>`)

When `version.Fire("loadParcel", args => args.Set<LocalDate>("LoadedAt", new LocalDate(2026, 4, 15)))` is called, the `IArgBuilder.Set<T>` call resolves through `TypeRuntime<T>.FromClr`. The caller provides a properly typed CLR value. No parsing occurs — the value is already typed.

For typed-literal types, the `TypeRuntime<T>` registrations are:

| Precept type | CLR type `T` | `FromClr` behavior |
|---|---|---|
| `date` | `LocalDate` | Direct wrap → `PreceptValue` |
| `time` | `LocalTime` | Direct wrap → `PreceptValue` |
| `datetime` | `LocalDateTime` | Direct wrap → `PreceptValue` |
| `instant` | `Instant` | Direct wrap → `PreceptValue` |
| `duration` | `Duration` | Direct wrap → `PreceptValue` |
| `period` | `Period` | Direct wrap → `PreceptValue` |
| `money` | `PreceptMoney` (or project-defined struct) | Direct wrap → `PreceptValue` |
| `quantity` | `PreceptQuantity` (or project-defined struct) | Direct wrap → `PreceptValue` |

**There is no parsing in the typed lane.** The caller provides the typed value; `FromClr` wraps it. Type mismatches (e.g., `Set<string>` for a `date` arg) are caught by the `ClrType` validation on `ArgDescriptor` at the builder boundary.

### Where does validation happen in the Fire pipeline?

The Fire pipeline (documented in `runtime-api.md`) is:

> arg validation → row matching → action chain → computed recomputation → constraint evaluation → commit/discard

**Arg validation is the first stage.** It runs before the evaluator touches the working copy. This is where:

1. **Presence checking** occurs — required args present, no unknown arg names.
2. **Type coercion** occurs — `TypeRuntimeMeta.ReadJson` (JSON lane) or `TypeRuntime<T>.FromClr` (typed lane) converts each arg value to `PreceptValue`.
3. **Constraint modifier validation** occurs — `nonnegative`, `positive`, `notempty`, etc. are checked against the materialized `PreceptValue`.

If any of these fail, the pipeline short-circuits to `EventOutcome.InvalidArgs` with the failure reason. The evaluator never sees invalid arg data.

### Shared parsing infrastructure — what IS shared vs. what is NOT

The temporal parser in `src/Precept/Language/Time/` and the UCUM parser in `src/Precept/Language/Ucum/` ARE shared infrastructure. Both the compile-time validators and the runtime `TypeRuntimeMeta.ReadJson` delegates call into the same underlying NodaTime/UCUM parsing logic. The difference is:

| Aspect | Compile-time path | Runtime path |
|---|---|---|
| **Entry point** | `TypedConstantValidation.Validate(...)` | `TypeRuntimeMeta.ReadJson` / `TypeRuntime<T>.FromClr` |
| **Input format** | DSL literal text (`'2026-04-15'`, `'100 USD'`) | JSON wire format or typed CLR value |
| **Error model** | `TypedConstantParseResult.Diagnostics` (spans, suggestions, codes) | `InvalidArgs` outcome (reason string, no spans) |
| **Caller** | Type checker, language server, MCP compile | Fire boundary (arg validation stage) |
| **Wraps** | Temporal/UCUM/Currency parsers | Same temporal/UCUM/Currency parsers |

The **parser itself** (e.g., `NodaTime.Text.LocalDatePattern.Iso.Parse(...)`) is the shared layer. The **entry point and error model** differ because compile-time and runtime have different needs.

### Does the framework need changes to support this?

**No.** The typed literal framework as proposed in Q1–Q3 does not need a separate "arg coercion" contract. The runtime arg path already exists in the `TypeRuntime<T>` / `TypeRuntimeMeta` catalog infrastructure. When new typed-literal types are added (Money, Quantity, temporal types), they need:

1. **`TypeRuntime<T>` registration** — `FromClr`, `ToClr`, `FromJson`, `ToJson` for the CLR carrier type.
2. **`TypeRuntimeMeta` on the `TypeMeta` entry** — `ReadJson`, `WriteJson`, `ParseString`, `FormatString` delegates.
3. **`ContentValidation` on the `TypeMeta` entry** — the compile-time literal validator (Q1–Q3 framework).

Items 1 and 2 are the runtime surface. Item 3 is the compile-time surface. They coexist on the same `TypeMeta` entry because the Types catalog is the single source of truth for everything about a Precept type — including both how its literals are validated at compile time and how its values are ingested at runtime.

### What about MCP `precept_fire` / `precept_update` arg validation?

The MCP tools are JSON-lane callers. When `precept_fire` receives arg values from the AI consumer, those values arrive as JSON. The MCP tool passes them to `Version.Fire(string, JsonElement?)` — the JSON lane. The Fire boundary's `TypeRuntimeMeta.ReadJson` handles parsing. The MCP tool does NOT call `TypedConstantValidation.Validate` on arg values — that would require the AI to format values in DSL literal syntax, which is wrong.

The existing proposal text in Q2 (MCP section) that says:

> **`precept_fire` / `precept_update` arg validation:** When the runtime boundary receives temporal, currency, or unit values from JSON, it validates them through the same `TypedConstantValidation.Validate(...)` path.

**This is corrected here.** MCP arg validation goes through `TypeRuntimeMeta.ReadJson` at the Fire boundary, not through `TypedConstantValidation.Validate`. The compile-time validator is for source literals. The runtime validator is `TypeRuntimeMeta.ReadJson`. Both call the same underlying parsers (NodaTime, UCUM, ISO 4217) — but through different entry points with different error models.

### Summary

| Question | Answer |
|---|---|
| Does `ITypedConstantValidator` / `TypedConstantValidation.Validate` get invoked during `Fire`? | **No.** It is compile-time only. |
| Who parses arg values at runtime? | **`TypeRuntimeMeta.ReadJson`** (JSON lane) or **`TypeRuntime<T>.FromClr`** (typed lane), called at the Fire boundary's arg validation stage. |
| How are parsing failures surfaced? | **`EventOutcome.InvalidArgs`** — a first-class outcome variant, not an exception. |
| Does the framework need a separate "arg coercion" contract? | **No.** `TypeRuntime<T>` / `TypeRuntimeMeta` IS the runtime coercion contract. It already exists in the catalog infrastructure. |
| What is shared between compile-time and runtime? | **The underlying domain parsers** (NodaTime patterns, UCUM parser, ISO 4217 catalog). Not the entry points or error models. |
| What must be added per new typed-literal type? | **Three things on `TypeMeta`:** `TypeRuntime<T>` registration, `TypeRuntimeMeta` delegates, and `ContentValidation` DU entry. |
