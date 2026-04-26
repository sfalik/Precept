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

    private static readonly QualifierShape QS_Currency         = new([new(TokenKind.In, QualifierAxis.Currency)]);
    private static readonly QualifierShape QS_Unit             = new([new(TokenKind.Of, QualifierAxis.Unit)]);
    private static readonly QualifierShape QS_TemporalDimension = new([new(TokenKind.Of, QualifierAxis.TemporalDimension)]);

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

    private static readonly FixedReturnAccessor CollectionCountAccessor =
        new("count", TypeKind.Integer, "Number of elements");

    private static readonly TypeAccessor[] SetAccessors =
    [
        CollectionCountAccessor,
        new TypeAccessor("min", "Minimum element",
            RequiredTraits: TypeTrait.Orderable,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Collection must be non-empty"),
            ]),
        new TypeAccessor("max", "Maximum element",
            RequiredTraits: TypeTrait.Orderable,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Collection must be non-empty"),
            ]),
    ];

    private static readonly TypeAccessor[] QueueAccessors =
    [
        CollectionCountAccessor,
        new TypeAccessor("peek", "Front element",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Queue must be non-empty"),
            ]),
    ];

    private static readonly TypeAccessor[] StackAccessors =
    [
        CollectionCountAccessor,
        new TypeAccessor("peek", "Top element",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Stack must be non-empty"),
            ]),
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
            Traits: TypeTrait.EqualityComparable,
            Accessors: [new FixedReturnAccessor("length", TypeKind.Integer, "Character count")],
            HoverDescription: "A variable-length text value. Use double quotes for literal text and {Field} for interpolated values."
        ),

        TypeKind.Boolean => new(
            kind, Tokens.GetMeta(TokenKind.BooleanType),
            "True or false",
            TypeCategory.Scalar,
            Traits: TypeTrait.EqualityComparable,
            HoverDescription: "A true or false value. Used for flags, toggles, and condition results."
        ),

        TypeKind.Integer => new(
            kind, Tokens.GetMeta(TokenKind.IntegerType),
            "Whole number (arbitrary precision)",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            WidensTo: IntegerWidens,
            HoverDescription: "A whole number with arbitrary precision. Use for counts, identifiers, and exact discrete values."
        ),

        TypeKind.Decimal => new(
            kind, Tokens.GetMeta(TokenKind.DecimalType),
            "Fixed-point decimal number",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            HoverDescription: "A fixed-point decimal number. Preserves exact fractional precision — use for financial amounts and percentages."
        ),

        TypeKind.Number => new(
            kind, Tokens.GetMeta(TokenKind.NumberType),
            "Floating-point number",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            HoverDescription: "A floating-point number. Loses precision in exchange for flexibility. Use approximate() or round() to bridge back to decimal."
        ),

        TypeKind.Choice => new(
            kind, Tokens.GetMeta(TokenKind.ChoiceType),
            "Enumerated string set",
            TypeCategory.Scalar,
            Traits: TypeTrait.EqualityComparable,
            HoverDescription: "A field restricted to a declared set of string values. Add the 'ordered' modifier to enable comparison operators between choice values."
        ),

        // ── Temporal ───────────────────────────────────────────────────
        TypeKind.Date => new(
            kind, Tokens.GetMeta(TokenKind.DateType),
            "Calendar date (year-month-day)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors: DateComponentAccessors,
            HoverDescription: "A calendar date (year-month-day) with no time component. Supports .year, .month, .day, and .dayOfWeek accessors."
        ),

        TypeKind.Time => new(
            kind, Tokens.GetMeta(TokenKind.TimeType),
            "Time of day (hour-minute-second)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors: TimeComponentAccessors,
            HoverDescription: "A time of day (hour-minute-second) with no date or timezone. Supports .hour, .minute, and .second accessors."
        ),

        TypeKind.Instant => new(
            kind, Tokens.GetMeta(TokenKind.InstantType),
            "UTC point in time",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("inZone", TypeKind.ZonedDateTime, "Convert to zoned datetime", ParameterType: TypeKind.Timezone),
            ],
            HoverDescription: "An exact UTC point in time. Use .inZone(timezone) to convert to a zoned date-time for local display."
        ),

        TypeKind.Duration => new(
            kind, Tokens.GetMeta(TokenKind.DurationType),
            "Exact elapsed time (hours/minutes/seconds)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("totalDays",    TypeKind.Number, "Total days (fractional)"),
                new FixedReturnAccessor("totalHours",   TypeKind.Number, "Total hours (fractional)"),
                new FixedReturnAccessor("totalMinutes", TypeKind.Number, "Total minutes (fractional)"),
                new FixedReturnAccessor("totalSeconds", TypeKind.Number, "Total seconds (fractional)"),
            ],
            HoverDescription: "An exact elapsed time measured in hours, minutes, and seconds. Use when the length of a gap needs to be precise."
        ),

        TypeKind.Period => new(
            kind, Tokens.GetMeta(TokenKind.PeriodType),
            "Calendar-relative duration (years/months/days)",
            TypeCategory.Temporal,
            Traits: TypeTrait.EqualityComparable,
            QualifierShape: QS_TemporalDimension,
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
            ],
            HoverDescription: "A calendar-relative duration measured in years, months, and days. Use for business deadlines and date offsets."
        ),

        TypeKind.Timezone => new(
            kind, Tokens.GetMeta(TokenKind.TimezoneType),
            "IANA timezone identifier",
            TypeCategory.Temporal,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            HoverDescription: "An IANA timezone identifier such as 'America/New_York'. Carries notempty implicitly — empty timezone is not meaningful."
        ),

        TypeKind.ZonedDateTime => new(
            kind, Tokens.GetMeta(TokenKind.ZonedDateTimeType),
            "Date and time in a specific timezone",
            TypeCategory.Temporal,
            Traits: TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("instant",  TypeKind.Instant,  "UTC instant"),
                new FixedReturnAccessor("timezone", TypeKind.Timezone, "Timezone identifier", ReturnsQualifier: QualifierAxis.Timezone),
                new FixedReturnAccessor("datetime", TypeKind.DateTime, "Local date and time"),
                new FixedReturnAccessor("date",     TypeKind.Date,     "Date component"),
                new FixedReturnAccessor("time",     TypeKind.Time,     "Time component"),
                ..DateComponentAccessors,
                ..TimeComponentAccessors,
            ],
            DisplayName: "zoned date-time",
            HoverDescription: "A date and time pinned to a specific timezone. Exposes .instant, .date, .time, and all calendar component accessors."
        ),

        TypeKind.DateTime => new(
            kind, Tokens.GetMeta(TokenKind.DateTimeType),
            "Date and time without timezone",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("date",   TypeKind.Date,          "Date component"),
                new FixedReturnAccessor("time",   TypeKind.Time,          "Time component"),
                new FixedReturnAccessor("inZone", TypeKind.ZonedDateTime, "Convert to zoned datetime", ParameterType: TypeKind.Timezone),
                ..DateComponentAccessors,
                ..TimeComponentAccessors,
            ],
            DisplayName: "date-time",
            HoverDescription: "A date and time without a timezone. Use when timezone context is handled externally or is not relevant."
        ),

        // ── Business-Domain ────────────────────────────────────────────
        TypeKind.Money => new(
            kind, Tokens.GetMeta(TokenKind.MoneyType),
            "Monetary amount with currency",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_Currency,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("amount",   TypeKind.Decimal,  "Numeric amount"),
                new FixedReturnAccessor("currency", TypeKind.Currency, "Currency identifier", ReturnsQualifier: QualifierAxis.Currency),
            ],
            HoverDescription: "A monetary amount bound to a currency. Use 'in USD' to pin currency. Arithmetic enforces same-currency rules."
        ),

        TypeKind.Currency => new(
            kind, Tokens.GetMeta(TokenKind.CurrencyType),
            "Currency identity (e.g., USD, EUR)",
            TypeCategory.BusinessDomain,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            HoverDescription: "An ISO 4217 currency code identifier such as 'USD' or 'EUR'. Carries notempty implicitly."
        ),

        TypeKind.Quantity => new(
            kind, Tokens.GetMeta(TokenKind.QuantityType),
            "Measured amount with unit",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_Unit,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("amount",    TypeKind.Decimal,       "Numeric magnitude"),
                new FixedReturnAccessor("unit",      TypeKind.UnitOfMeasure, "Unit of measure", ReturnsQualifier: QualifierAxis.Unit),
                new FixedReturnAccessor("dimension", TypeKind.Dimension,     "Dimension family"),
            ],
            HoverDescription: "A measured amount bound to a unit of measure. Use 'of kg' to pin units. Arithmetic enforces same-dimension rules."
        ),

        TypeKind.UnitOfMeasure => new(
            kind, Tokens.GetMeta(TokenKind.UnitOfMeasureType),
            "Unit of measure identity (e.g., kg, miles)",
            TypeCategory.BusinessDomain,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            Accessors:
            [
                new FixedReturnAccessor("dimension", TypeKind.Dimension, "Dimension family of this unit"),
            ],
            DisplayName: "unit of measure",
            HoverDescription: "A unit-of-measure identifier such as 'kg' or 'miles'. Carries notempty implicitly. Use .dimension to read the unit's dimension family."
        ),

        TypeKind.Dimension => new(
            kind, Tokens.GetMeta(TokenKind.DimensionType),
            "Dimension family identity (e.g., length, mass)",
            TypeCategory.BusinessDomain,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            HoverDescription: "A dimension family identifier such as 'length' or 'mass'. Used to enforce dimensional consistency in quantity arithmetic."
        ),

        TypeKind.Price => new(
            kind, Tokens.GetMeta(TokenKind.PriceType),
            "Price: monetary amount per unit",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_CurrencyUnit,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("amount",    TypeKind.Decimal,       "Numeric amount"),
                new FixedReturnAccessor("currency",  TypeKind.Currency,      "Currency identifier", ReturnsQualifier: QualifierAxis.Currency),
                new FixedReturnAccessor("unit",      TypeKind.UnitOfMeasure, "Unit of measure", ReturnsQualifier: QualifierAxis.Unit),
                new FixedReturnAccessor("dimension", TypeKind.Dimension,     "Dimension family"),
            ],
            HoverDescription: "A monetary amount per unit — combines currency and unit qualifiers. Use 'in USD of kg' for a price per kilogram in dollars."
        ),

        TypeKind.ExchangeRate => new(
            kind, Tokens.GetMeta(TokenKind.ExchangeRateType),
            "Currency exchange rate",
            TypeCategory.BusinessDomain,
            Traits: TypeTrait.EqualityComparable,
            QualifierShape: QS_ExchangeRate,
            Accessors:
            [
                new FixedReturnAccessor("amount", TypeKind.Decimal,  "Exchange rate value"),
                new FixedReturnAccessor("from",   TypeKind.Currency, "Source currency", ReturnsQualifier: QualifierAxis.FromCurrency),
                new FixedReturnAccessor("to",     TypeKind.Currency, "Target currency", ReturnsQualifier: QualifierAxis.ToCurrency),
            ],
            DisplayName: "exchange rate",
            HoverDescription: "A currency exchange rate from one currency to another. Use 'in USD to EUR' to declare the conversion direction."
        ),

        // ── Collection ─────────────────────────────────────────────────
        TypeKind.Set => new(
            kind, Tokens.GetMeta(TokenKind.Set),
            "Unordered collection with unique elements",
            TypeCategory.Collection,
            Accessors: SetAccessors,
            HoverDescription: "An unordered collection of unique elements. Use add and remove actions. Supports .count, .min, and .max accessors."
        ),

        TypeKind.Queue => new(
            kind, Tokens.GetMeta(TokenKind.QueueType),
            "First-in first-out collection",
            TypeCategory.Collection,
            Accessors: QueueAccessors,
            HoverDescription: "A first-in first-out collection. Use enqueue and dequeue actions. Supports .count and .peek accessors."
        ),

        TypeKind.Stack => new(
            kind, Tokens.GetMeta(TokenKind.StackType),
            "Last-in first-out collection",
            TypeCategory.Collection,
            Accessors: StackAccessors,
            HoverDescription: "A last-in first-out collection. Use push and pop actions. Supports .count and .peek accessors."
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
