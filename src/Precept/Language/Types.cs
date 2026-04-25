using System.Collections.Frozen;

namespace Precept.Language;

public static class Types
{
    // ── Shared accessor building blocks ─────────────────────────────────────────

    private static readonly TypeAccessor[] DateComponentAccessors =
    [
        new FixedReturnAccessor("year",      TypeKind.Integer, "Year component"),
        new FixedReturnAccessor("month",     TypeKind.Integer, "Month component"),
        new FixedReturnAccessor("day",       TypeKind.Integer, "Day component"),
        new FixedReturnAccessor("dayOfWeek", TypeKind.Integer, "Day of week (0=Sunday)"),
    ];

    private static readonly TypeAccessor[] TimeComponentAccessors =
    [
        new FixedReturnAccessor("hour",   TypeKind.Integer, "Hour component"),
        new FixedReturnAccessor("minute", TypeKind.Integer, "Minute component"),
        new FixedReturnAccessor("second", TypeKind.Integer, "Second component"),
    ];

    // ── Shared qualifier shapes ─────────────────────────────────────────────────

    private static readonly QualifierShape QS_Currency = new([new(TokenKind.In, QualifierAxis.Currency)]);
    private static readonly QualifierShape QS_Unit     = new([new(TokenKind.Of, QualifierAxis.Unit)]);

    private static readonly QualifierShape QS_CurrencyUnit = new(
    [
        new(TokenKind.In, QualifierAxis.Currency),
        new(TokenKind.Of, QualifierAxis.Unit),
    ]);

    private static readonly QualifierShape QS_ExchangeRate = new(
    [
        new(TokenKind.In, QualifierAxis.FromCurrency),
        new(TokenKind.To, QualifierAxis.ToCurrency),
    ]);

    // ── Shared widening targets ─────────────────────────────────────────────────

    private static readonly TypeKind[] IntegerWidens = [TypeKind.Decimal, TypeKind.Number];

    // ── Collection accessor helpers ─────────────────────────────────────────────

    private static readonly TypeAccessor[] SetAccessors =
    [
        new FixedReturnAccessor("count", TypeKind.Integer, "Number of elements"),
        new TypeAccessor("min", "Minimum element", RequiredTraits: TypeTrait.Orderable),
        new TypeAccessor("max", "Maximum element", RequiredTraits: TypeTrait.Orderable),
    ];

    private static readonly TypeAccessor[] QueueAccessors =
    [
        new FixedReturnAccessor("count", TypeKind.Integer, "Number of elements"),
        new TypeAccessor("peek", "Front element"),
    ];

    private static readonly TypeAccessor[] StackAccessors =
    [
        new FixedReturnAccessor("count", TypeKind.Integer, "Number of elements"),
        new TypeAccessor("peek", "Top element"),
    ];

    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static TypeMeta GetMeta(TypeKind kind) => kind switch
    {
        // ── Scalar ─────────────────────────────────────────────────────
        TypeKind.String => new(
            kind, Tokens.GetMeta(TokenKind.StringType),
            "Variable-length text",
            TypeCategory.Scalar,
            Accessors: [new FixedReturnAccessor("length", TypeKind.Integer, "Character count")]
        ),

        TypeKind.Boolean => new(
            kind, Tokens.GetMeta(TokenKind.BooleanType),
            "True or false",
            TypeCategory.Scalar
        ),

        TypeKind.Integer => new(
            kind, Tokens.GetMeta(TokenKind.IntegerType),
            "Whole number (arbitrary precision)",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable,
            WidensTo: IntegerWidens
        ),

        TypeKind.Decimal => new(
            kind, Tokens.GetMeta(TokenKind.DecimalType),
            "Fixed-point decimal number",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable
        ),

        TypeKind.Number => new(
            kind, Tokens.GetMeta(TokenKind.NumberType),
            "Floating-point number",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable
        ),

        TypeKind.Choice => new(
            kind, Tokens.GetMeta(TokenKind.ChoiceType),
            "Enumerated string set",
            TypeCategory.Scalar
        ),

        // ── Temporal ───────────────────────────────────────────────────
        TypeKind.Date => new(
            kind, Tokens.GetMeta(TokenKind.DateType),
            "Calendar date (year-month-day)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable,
            Accessors: DateComponentAccessors
        ),

        TypeKind.Time => new(
            kind, Tokens.GetMeta(TokenKind.TimeType),
            "Time of day (hour-minute-second)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable,
            Accessors: TimeComponentAccessors
        ),

        TypeKind.Instant => new(
            kind, Tokens.GetMeta(TokenKind.InstantType),
            "UTC point in time",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable,
            Accessors:
            [
                new FixedReturnAccessor("inZone", TypeKind.ZonedDateTime, "Convert to zoned datetime", ParameterType: TypeKind.Timezone),
            ]
        ),

        TypeKind.Duration => new(
            kind, Tokens.GetMeta(TokenKind.DurationType),
            "Exact elapsed time (hours/minutes/seconds)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable,
            Accessors:
            [
                new FixedReturnAccessor("totalDays",    TypeKind.Number, "Total days (fractional)"),
                new FixedReturnAccessor("totalHours",   TypeKind.Number, "Total hours (fractional)"),
                new FixedReturnAccessor("totalMinutes", TypeKind.Number, "Total minutes (fractional)"),
                new FixedReturnAccessor("totalSeconds", TypeKind.Number, "Total seconds (fractional)"),
            ]
        ),

        TypeKind.Period => new(
            kind, Tokens.GetMeta(TokenKind.PeriodType),
            "Calendar-relative duration (years/months/days)",
            TypeCategory.Temporal,
            QualifierShape: QS_Unit,
            Accessors:
            [
                new FixedReturnAccessor("years",   TypeKind.Integer, "Years component"),
                new FixedReturnAccessor("months",  TypeKind.Integer, "Months component"),
                new FixedReturnAccessor("weeks",   TypeKind.Integer, "Weeks component"),
                new FixedReturnAccessor("days",    TypeKind.Integer, "Days component"),
                new FixedReturnAccessor("hours",   TypeKind.Integer, "Hours component"),
                new FixedReturnAccessor("minutes", TypeKind.Integer, "Minutes component"),
                new FixedReturnAccessor("seconds", TypeKind.Integer, "Seconds component"),
                new FixedReturnAccessor("hasDateComponent", TypeKind.Boolean, "Has year/month/day components"),
                new FixedReturnAccessor("hasTimeComponent", TypeKind.Boolean, "Has hour/minute/second components"),
                new FixedReturnAccessor("basis",     TypeKind.String,    "Period basis unit name"),
                new FixedReturnAccessor("dimension", TypeKind.Dimension, "Dimension family of the unit"),
            ]
        ),

        TypeKind.Timezone => new(
            kind, Tokens.GetMeta(TokenKind.TimezoneType),
            "IANA timezone identifier",
            TypeCategory.Temporal
        ),

        TypeKind.ZonedDateTime => new(
            kind, Tokens.GetMeta(TokenKind.ZonedDateTimeType),
            "Date and time in a specific timezone",
            TypeCategory.Temporal,
            Accessors:
            [
                new FixedReturnAccessor("instant",  TypeKind.Instant,  "UTC instant"),
                new FixedReturnAccessor("timezone", TypeKind.Timezone, "Timezone identifier", ReturnsQualifier: QualifierAxis.Timezone),
                new FixedReturnAccessor("datetime", TypeKind.DateTime, "Local date and time"),
                new FixedReturnAccessor("date",     TypeKind.Date,     "Date component"),
                new FixedReturnAccessor("time",     TypeKind.Time,     "Time component"),
                ..DateComponentAccessors,
                ..TimeComponentAccessors,
            ]
        ),

        TypeKind.DateTime => new(
            kind, Tokens.GetMeta(TokenKind.DateTimeType),
            "Date and time without timezone",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable,
            Accessors:
            [
                new FixedReturnAccessor("date",   TypeKind.Date,          "Date component"),
                new FixedReturnAccessor("time",   TypeKind.Time,          "Time component"),
                new FixedReturnAccessor("inZone", TypeKind.ZonedDateTime, "Convert to zoned datetime", ParameterType: TypeKind.Timezone),
                ..DateComponentAccessors,
                ..TimeComponentAccessors,
            ]
        ),

        // ── Business-Domain ────────────────────────────────────────────
        TypeKind.Money => new(
            kind, Tokens.GetMeta(TokenKind.MoneyType),
            "Monetary amount with currency",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_Currency,
            Traits: TypeTrait.Orderable,
            Accessors:
            [
                new FixedReturnAccessor("amount",   TypeKind.Decimal,  "Numeric amount"),
                new FixedReturnAccessor("currency", TypeKind.Currency, "Currency identifier", ReturnsQualifier: QualifierAxis.Currency),
            ]
        ),

        TypeKind.Currency => new(
            kind, Tokens.GetMeta(TokenKind.CurrencyType),
            "Currency identity (e.g., USD, EUR)",
            TypeCategory.BusinessDomain
        ),

        TypeKind.Quantity => new(
            kind, Tokens.GetMeta(TokenKind.QuantityType),
            "Measured amount with unit",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_Unit,
            Traits: TypeTrait.Orderable,
            Accessors:
            [
                new FixedReturnAccessor("amount",    TypeKind.Decimal,       "Numeric magnitude"),
                new FixedReturnAccessor("unit",      TypeKind.UnitOfMeasure, "Unit of measure", ReturnsQualifier: QualifierAxis.Unit),
                new FixedReturnAccessor("dimension", TypeKind.Dimension,     "Dimension family"),
            ]
        ),

        TypeKind.UnitOfMeasure => new(
            kind, Tokens.GetMeta(TokenKind.UnitOfMeasureType),
            "Unit of measure identity (e.g., kg, miles)",
            TypeCategory.BusinessDomain,
            Accessors:
            [
                new FixedReturnAccessor("dimension", TypeKind.Dimension, "Dimension family of this unit"),
            ]
        ),

        TypeKind.Dimension => new(
            kind, Tokens.GetMeta(TokenKind.DimensionType),
            "Dimension family identity (e.g., length, mass)",
            TypeCategory.BusinessDomain
        ),

        TypeKind.Price => new(
            kind, Tokens.GetMeta(TokenKind.PriceType),
            "Price: monetary amount per unit",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_CurrencyUnit,
            Traits: TypeTrait.Orderable,
            Accessors:
            [
                new FixedReturnAccessor("amount",    TypeKind.Decimal,       "Numeric amount"),
                new FixedReturnAccessor("currency",  TypeKind.Currency,      "Currency identifier", ReturnsQualifier: QualifierAxis.Currency),
                new FixedReturnAccessor("unit",      TypeKind.UnitOfMeasure, "Unit of measure", ReturnsQualifier: QualifierAxis.Unit),
                new FixedReturnAccessor("dimension", TypeKind.Dimension,     "Dimension family"),
            ]
        ),

        TypeKind.ExchangeRate => new(
            kind, Tokens.GetMeta(TokenKind.ExchangeRateType),
            "Currency exchange rate",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_ExchangeRate,
            Accessors:
            [
                new FixedReturnAccessor("amount", TypeKind.Decimal,  "Exchange rate value"),
                new FixedReturnAccessor("from",   TypeKind.Currency, "Source currency", ReturnsQualifier: QualifierAxis.FromCurrency),
                new FixedReturnAccessor("to",     TypeKind.Currency, "Target currency", ReturnsQualifier: QualifierAxis.ToCurrency),
            ]
        ),

        // ── Collection ─────────────────────────────────────────────────
        TypeKind.Set => new(
            kind, Tokens.GetMeta(TokenKind.Set),
            "Unordered collection with unique elements",
            TypeCategory.Collection,
            Accessors: SetAccessors
        ),

        TypeKind.Queue => new(
            kind, Tokens.GetMeta(TokenKind.QueueType),
            "First-in first-out collection",
            TypeCategory.Collection,
            Accessors: QueueAccessors
        ),

        TypeKind.Stack => new(
            kind, Tokens.GetMeta(TokenKind.StackType),
            "Last-in first-out collection",
            TypeCategory.Collection,
            Accessors: StackAccessors
        ),

        // ── Special ────────────────────────────────────────────────────
        TypeKind.Error => new(
            kind, null,
            "Error type — suppresses cascading diagnostics",
            TypeCategory.Special
        ),

        TypeKind.StateRef => new(
            kind, null,
            "State name reference in transition/ensure targets",
            TypeCategory.Special
        ),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, $"No metadata for TypeKind.{kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All + derived indexes
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<TypeMeta> All { get; } =
        Enum.GetValues<TypeKind>().Select(GetMeta).ToArray();

    /// <summary>
    /// Derived frozen index: <see cref="TokenKind"/> → <see cref="TypeMeta"/>.
    /// Includes both <see cref="TokenKind.Set"/> and <see cref="TokenKind.SetType"/>
    /// (parser-synthesized alias) mapped to the same Set <see cref="TypeMeta"/>.
    /// </summary>
    public static FrozenDictionary<TokenKind, TypeMeta> ByToken { get; } = BuildByToken();

    private static FrozenDictionary<TokenKind, TypeMeta> BuildByToken()
    {
        var dict = new Dictionary<TokenKind, TypeMeta>();
        foreach (var meta in All)
        {
            if (meta.Token is not null)
                dict[meta.Token.Kind] = meta;
        }

        // The lexer emits TokenKind.Set; the parser reinterprets as SetType in type position.
        // Map both to the same TypeMeta so either key works for lookup.
        if (dict.TryGetValue(TokenKind.Set, out var setMeta))
            dict[TokenKind.SetType] = setMeta;

        return dict.ToFrozenDictionary();
    }
}
