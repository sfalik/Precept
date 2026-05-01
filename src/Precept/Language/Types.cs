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

    private static readonly QualifierShape QS_UnitOrDimension = new(
    [
        new(TokenKind.In, QualifierAxis.Unit),
        new(TokenKind.Of, QualifierAxis.Dimension),
    ], InOfExclusive: true);

    private static readonly QualifierShape QS_TemporalUnitOrDimension = new(
    [
        new(TokenKind.In, QualifierAxis.TemporalUnit),
        new(TokenKind.Of, QualifierAxis.TemporalDimension),
    ], InOfExclusive: true);

    private static readonly QualifierShape QS_CurrencyAndDimension = new(
    [
        new(TokenKind.In, QualifierAxis.Currency),
        new(TokenKind.Of, QualifierAxis.Dimension),
    ], InOfExclusive: false);

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
            Traits: TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            Accessors: [new FixedReturnAccessor("length", TypeKind.Integer, "Character count")],
            DisplayName: "string",
            HoverDescription: "A variable-length text value. Use double quotes for literal text and {Field} for interpolated values.",
            UsageExample: "field CandidateName as string optional maxlength 100"
        ),

        TypeKind.Boolean => new(
            kind, Tokens.GetMeta(TokenKind.BooleanType),
            "True or false",
            TypeCategory.Scalar,
            Traits: TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            DisplayName: "boolean",
            HoverDescription: "A true or false value. Used for flags, toggles, and condition results.",
            UsageExample: "field DepositPaid as boolean default false"
        ),

        TypeKind.Integer => new(
            kind, Tokens.GetMeta(TokenKind.IntegerType),
            "Whole number (arbitrary precision)",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            WidensTo: IntegerWidens,
            DisplayName: "integer",
            HoverDescription: "A whole number with arbitrary precision. Use for counts, identifiers, and exact discrete values.",
            UsageExample: "field ReopenCount as integer default 0 nonnegative"
        ),

        TypeKind.Decimal => new(
            kind, Tokens.GetMeta(TokenKind.DecimalType),
            "Fixed-point decimal number",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            DisplayName: "decimal",
            HoverDescription: "A fixed-point decimal number. Preserves exact fractional precision — use for financial amounts and percentages.",
            UsageExample: "field TaxRate as decimal default 0.1 nonnegative maxplaces 4"
        ),

        TypeKind.Number => new(
            kind, Tokens.GetMeta(TokenKind.NumberType),
            "Floating-point number",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            DisplayName: "number",
            HoverDescription: "A floating-point number. Loses precision in exchange for flexibility. Use approximate() or round() to bridge back to decimal.",
            UsageExample: "field CreditScore as number default 0 nonnegative"
        ),

        TypeKind.Choice => new(
            kind, Tokens.GetMeta(TokenKind.ChoiceType),
            "Enumerated value set with explicit element type",
            TypeCategory.Scalar,
            Traits: TypeTrait.EqualityComparable,
            DisplayName: "choice",
            HoverDescription: "A sealed enumerated type restricted to a declared set of typed values. Requires an explicit element type: 'choice of string(...)', 'choice of integer(...)', etc. Add the 'ordered' modifier to enable comparison operators ranked by declaration order.",
            UsageExample: "field Priority as choice of string(\"Low\",\"Medium\",\"High\") ordered default \"Low\""
        ),

        // ── Temporal ───────────────────────────────────────────────────
        TypeKind.Date => new(
            kind, Tokens.GetMeta(TokenKind.DateType),
            "Calendar date (year-month-day)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors: DateComponentAccessors,
            DisplayName: "date",
            HoverDescription: "A calendar date (year-month-day) with no time component. Supports .year, .month, .day, and .dayOfWeek accessors.",
            UsageExample: "field DueDate as date default '2026-06-01'"
        ),

        TypeKind.Time => new(
            kind, Tokens.GetMeta(TokenKind.TimeType),
            "Time of day (hour-minute-second)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors: TimeComponentAccessors,
            DisplayName: "time",
            HoverDescription: "A time of day (hour-minute-second) with no date or timezone. Supports .hour, .minute, and .second accessors.",
            UsageExample: "field AppointmentTime as time default '09:00:00'"
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
            DisplayName: "instant",
            HoverDescription: "An exact UTC point in time. Use .inZone(timezone) to convert to a zoned date-time for local display.",
            UsageExample: "field FiledAt as instant optional"
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
            DisplayName: "duration",
            HoverDescription: "An exact elapsed time measured in hours, minutes, and seconds. Use when the length of a gap needs to be precise.",
            UsageExample: "field SlaLimit as duration default '72 hours'"
        ),

        TypeKind.Period => new(
            kind, Tokens.GetMeta(TokenKind.PeriodType),
            "Calendar-relative duration (years/months/days)",
            TypeCategory.Temporal,
            Traits: TypeTrait.EqualityComparable,
            QualifierShape: QS_TemporalUnitOrDimension,
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
            DisplayName: "period",
            HoverDescription: "A calendar-relative duration measured in years, months, and days. Use for business deadlines and date offsets.",
            UsageExample: "field GracePeriod as period default '30 days'"
        ),

        TypeKind.Timezone => new(
            kind, Tokens.GetMeta(TokenKind.TimezoneType),
            "IANA timezone identifier",
            TypeCategory.Temporal,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            DisplayName: "timezone",
            HoverDescription: "An IANA timezone identifier such as 'America/New_York'. Carries notempty implicitly — empty timezone is not meaningful.",
            UsageExample: "field ClinicTimezone as timezone default 'America/New_York'"
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
            HoverDescription: "A date and time pinned to a specific timezone. Exposes .instant, .date, .time, and all calendar component accessors.",
            UsageExample: "field IncidentContext as zoneddatetime"
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
            HoverDescription: "A date and time without a timezone. Use when timezone context is handled externally or is not relevant.",
            UsageExample: "field ScheduledFor as datetime default '2026-04-13T09:00:00'"
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
            DisplayName: "money",
            HoverDescription: "A monetary amount bound to a currency. Use 'in USD' to pin currency. Arithmetic enforces same-currency rules.",
            UsageExample: "field TotalCost as money in 'USD'"
        ),

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

        TypeKind.Quantity => new(
            kind, Tokens.GetMeta(TokenKind.QuantityType),
            "Measured amount with unit",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_UnitOrDimension,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("amount",    TypeKind.Decimal,       "Numeric magnitude"),
                new FixedReturnAccessor("unit",      TypeKind.UnitOfMeasure, "Unit of measure", ReturnsQualifier: QualifierAxis.Unit),
                new FixedReturnAccessor("dimension", TypeKind.Dimension,     "Dimension family"),
            ],
            DisplayName: "quantity",
            HoverDescription: "A measured amount bound to a unit of measure. Use 'in kg' to pin units or 'of length' to constrain by dimension. Arithmetic enforces same-dimension rules.",
            UsageExample: "field Weight as quantity in 'kg'"
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
            HoverDescription: "A unit-of-measure identifier such as 'kg' or 'miles'. Carries notempty implicitly. Use .dimension to read the unit's dimension family.",
            UsageExample: "field StockingUom as unitofmeasure default 'each'"
        ),

        TypeKind.Dimension => new(
            kind, Tokens.GetMeta(TokenKind.DimensionType),
            "Dimension family identity (e.g., length, mass)",
            TypeCategory.BusinessDomain,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            DisplayName: "dimension",
            HoverDescription: "A dimension family identifier such as 'length' or 'mass'. Used to enforce dimensional consistency in quantity arithmetic.",
            UsageExample: "field MeasuredDimension as dimension default 'mass'"
        ),

        TypeKind.Price => new(
            kind, Tokens.GetMeta(TokenKind.PriceType),
            "Price: monetary amount per unit",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_CurrencyAndDimension,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("amount",    TypeKind.Decimal,       "Numeric amount"),
                new FixedReturnAccessor("currency",  TypeKind.Currency,      "Currency identifier", ReturnsQualifier: QualifierAxis.Currency),
                new FixedReturnAccessor("unit",      TypeKind.UnitOfMeasure, "Unit of measure", ReturnsQualifier: QualifierAxis.Unit),
                new FixedReturnAccessor("dimension", TypeKind.Dimension,     "Dimension family"),
            ],
            DisplayName: "price",
            HoverDescription: "A monetary amount per unit — combines currency and dimension qualifiers. Use 'in USD/kg' for a specific price, or 'in USD of mass' for currency-pinned with dimension category.",
            UsageExample: "field UnitPrice as price in 'USD/each'"
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
            HoverDescription: "A currency exchange rate from one currency to another. Use 'in USD to EUR' to declare the conversion direction.",
            UsageExample: "field FxRate as exchangerate in 'USD' to 'EUR'"
        ),

        // ── Collection ─────────────────────────────────────────────────
        TypeKind.Set => new(
            kind, Tokens.GetMeta(TokenKind.Set),
            "Unordered collection with unique elements",
            TypeCategory.Collection,
            Accessors: SetAccessors,
            DisplayName: "set",
            HoverDescription: "An unordered collection of unique elements. Use add and remove actions. Supports .count, .min, and .max accessors.",
            UsageExample: "field PendingInterviewers as set of string"
        ),

        TypeKind.Queue => new(
            kind, Tokens.GetMeta(TokenKind.QueueType),
            "First-in first-out collection",
            TypeCategory.Collection,
            Accessors: QueueAccessors,
            DisplayName: "queue",
            HoverDescription: "A first-in first-out collection. Use enqueue and dequeue actions. Supports .count and .peek accessors.",
            UsageExample: "field AgentQueue as queue of string"
        ),

        TypeKind.Stack => new(
            kind, Tokens.GetMeta(TokenKind.StackType),
            "Last-in first-out collection",
            TypeCategory.Collection,
            Accessors: StackAccessors,
            DisplayName: "stack",
            HoverDescription: "A last-in first-out collection. Use push and pop actions. Supports .count and .peek accessors.",
            UsageExample: "field RepairSteps as stack of string"
        ),

        // ── Special ────────────────────────────────────────────────────
        TypeKind.Error => new(
            kind, null,
            "Error type — suppresses cascading diagnostics",
            TypeCategory.Special,
            DisplayName: "error"
        ),

        TypeKind.StateRef => new(
            kind, null,
            "State name reference in transition/ensure targets",
            TypeCategory.Special,
            DisplayName: "state reference"
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
