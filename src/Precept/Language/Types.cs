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

    // ── ContentValidation instances ─────────────────────────────────────────────

    private static readonly NodaTimeValidation DateValidation = new(
        TemporalLiteralKind.Date,
        "uuuu'-'MM'-'dd",
        "ISO 8601 date (YYYY-MM-DD)",
        ["2026-01-15", "2024-12-31"]);

    private static readonly NodaTimeValidation TimeValidation = new(
        TemporalLiteralKind.Time,
        "HH':'mm':'ss",
        "ISO 8601 extended time (HH:mm:ss)",
        ["09:00:00", "14:30:00"]);

    private static readonly NodaTimeValidation DateTimeValidation = new(
        TemporalLiteralKind.DateTime,
        "uuuu'-'MM'-'dd'T'HH':'mm':'ss",
        "ISO 8601 date-time (YYYY-MM-DDThh:mm:ss)",
        ["2026-04-13T09:00:00", "2024-12-31T23:59:59"]);

    private static readonly NodaTimeValidation InstantValidation = new(
        TemporalLiteralKind.Instant,
        "uuuu'-'MM'-'dd'T'HH':'mm':'ss'Z'",
        "ISO 8601 instant (YYYY-MM-DDThh:mm:ssZ)",
        ["2026-04-15T14:30:00Z"]);

    private static readonly NodaTimeValidation TimezoneValidation = new(
        TemporalLiteralKind.Timezone,
        "IANA timezone",
        "IANA timezone identifier",
        ["America/New_York", "UTC"]);

    private static readonly NodaTimeValidation ZonedDateTimeValidation = new(
        TemporalLiteralKind.ZonedDateTime,
        "uuuu'-'MM'-'dd'T'HH':'mm':'ss'['timezone']'",
        "Zoned date-time: YYYY-MM-DDThh:mm:ss[Area/Location]",
        ["2026-04-15T14:30:00[America/New_York]"]);

    private static readonly NodaTimeValidation DurationValidation = new(
        TemporalLiteralKind.TemporalQuantity,
        "quantity",
        "Temporal quantity: <integer> <unit> [+ <integer> <unit>]*",
        ["72 hours", "2 hours + 30 minutes", "3600 seconds"]);

    private static readonly NodaTimeValidation PeriodValidation = new(
        TemporalLiteralKind.TemporalQuantity,
        "NormalizingIso",
        "Temporal quantity: <integer> <unit> [+ <integer> <unit>]*",
        ["30 days", "2 years + 6 months", "P30D"]);

    private static readonly ClosedSetValidation CurrencyValidation = new(
        "ISO 4217",
        CurrencyCatalog.All.Keys.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
        "ISO 4217 currency code",
        ["USD", "EUR", "GBP"]);

    private static readonly UcumValidation UnitOfMeasureValidation = new(
        "UCUM expression",
        ["kg", "m/s^2", "mg/dL"]);

    private static readonly ClosedSetValidation DimensionValidation = new(
        "recognized dimensions",
        DimensionCatalog.All.Keys.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
        "Dimension family identifier",
        ["length", "mass", "count"]);

    private static readonly MoneyValidation MoneyLiteralValidation = new(
        "Monetary amount: <decimal> <ISO-4217>",
        ["100 USD", "50.25 EUR"]);

    private static readonly QuantityValidation QuantityLiteralValidation = new(
        "Physical quantity: <decimal> <UCUM-unit>",
        ["5 kg", "2.5 mg/dL"]);

    private static readonly PriceValidation PriceLiteralValidation = new(
        "Price: <decimal> <ISO-4217>/<UCUM-unit>",
        ["4.17 USD/each", "10.00 EUR/kg"]);

    private static readonly ExchangeRateValidation ExchangeRateLiteralValidation = new(
        "Exchange rate: <decimal> <ISO-4217>/<ISO-4217>",
        ["1.08 USD/EUR"]);

    // ── Collection accessor helpers ─────────────────────────────────────────────

    internal static readonly FixedReturnAccessor CollectionCountAccessor =
        new("count", TypeKind.Integer, "Number of elements", ReturnNonnegative: true);

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

    private static readonly TypeAccessor[] LogAccessors =
    [
        CollectionCountAccessor,
        new TypeAccessor("first", "First (oldest) element",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Log must be non-empty"),
            ]),
        new TypeAccessor("last", "Last (most recent) element",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Log must be non-empty"),
            ]),
        new TypeAccessor("at", "Element at zero-based position N",
            ParameterType: TypeKind.Integer,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Index must be within bounds"),
            ]),
    ];

    private static readonly TypeAccessor[] LogByAccessors =
    [
        CollectionCountAccessor,
        new TypeAccessor("first", "Entry with minimum ordering key",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Log must be non-empty"),
            ]),
        new TypeAccessor("last", "Entry with maximum ordering key",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Log must be non-empty"),
            ]),
        new TypeAccessor("at", "Nth entry in ordering key order (zero-based)",
            ParameterType: TypeKind.Integer,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Index must be within bounds"),
            ]),
    ];

    private static readonly TypeAccessor[] BagAccessors =
    [
        CollectionCountAccessor,
        new ElementParameterAccessor("countof", "Count of a specific element (0 if absent)"),
    ];

    private static readonly TypeAccessor[] ListAccessors =
    [
        CollectionCountAccessor,
        new TypeAccessor("first", "First element",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "List must be non-empty"),
            ]),
        new TypeAccessor("last", "Last element",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "List must be non-empty"),
            ]),
        new TypeAccessor("at", "Element at zero-based position N",
            ParameterType: TypeKind.Integer,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Index must be within bounds"),
            ]),
    ];

    private static readonly TypeAccessor[] QueueByAccessors =
    [
        CollectionCountAccessor,
        new TypeAccessor("peek", "Element value of the front (best-ordered) item",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Queue must be non-empty"),
            ]),
        new TypeAccessor("peekby", "Ordering value of the front (best-ordered) item",
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Queue must be non-empty"),
            ]),
    ];

    private static readonly TypeAccessor[] LookupAccessors =
    [
        CollectionCountAccessor,
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
            UsageExample: "field CandidateName as string optional maxlength 100",
            ChoiceLiteralTokens: [TokenKind.StringLiteral]
        ),

        TypeKind.Boolean => new(
            kind, Tokens.GetMeta(TokenKind.BooleanType),
            "True or false",
            TypeCategory.Scalar,
            Traits: TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            DisplayName: "boolean",
            HoverDescription: "A true or false value. Used for flags, toggles, and condition results.",
            UsageExample: "field DepositPaid as boolean default false",
            ChoiceLiteralTokens: [TokenKind.True, TokenKind.False]
        ),

        TypeKind.Integer => new(
            kind, Tokens.GetMeta(TokenKind.IntegerType),
            "Whole number (arbitrary precision)",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            WidensTo: IntegerWidens,
            DisplayName: "integer",
            HoverDescription: "A whole number with arbitrary precision. Use for counts, identifiers, and exact discrete values.",
            UsageExample: "field ReopenCount as integer default 0 nonnegative",
            ChoiceLiteralTokens: [TokenKind.NumberLiteral]
        ),

        TypeKind.Decimal => new(
            kind, Tokens.GetMeta(TokenKind.DecimalType),
            "Fixed-point decimal number",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            DisplayName: "decimal",
            HoverDescription: "A fixed-point decimal number. Preserves exact fractional precision — use for financial amounts and percentages.",
            UsageExample: "field TaxRate as decimal default 0.1 nonnegative maxplaces 4",
            ChoiceLiteralTokens: [TokenKind.NumberLiteral]
        ),

        TypeKind.Number => new(
            kind, Tokens.GetMeta(TokenKind.NumberType),
            "Floating-point number",
            TypeCategory.Scalar,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable | TypeTrait.ChoiceElement,
            DisplayName: "number",
            HoverDescription: "A floating-point number. Loses precision in exchange for flexibility. Use approximate() or round() to bridge back to decimal.",
            UsageExample: "field CreditScore as number default 0 nonnegative",
            ChoiceLiteralTokens: [TokenKind.NumberLiteral]
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
            UsageExample: "field DueDate as date default '2026-06-01'",
            ContentValidation: DateValidation
        ),

        TypeKind.Time => new(
            kind, Tokens.GetMeta(TokenKind.TimeType),
            "Time of day (hour-minute-second)",
            TypeCategory.Temporal,
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors: TimeComponentAccessors,
            DisplayName: "time",
            HoverDescription: "A time of day (hour-minute-second) with no date or timezone. Supports .hour, .minute, and .second accessors.",
            UsageExample: "field AppointmentTime as time default '09:00:00'",
            ContentValidation: TimeValidation
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
            UsageExample: "field FiledAt as instant optional",
            ContentValidation: InstantValidation
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
            UsageExample: "field SlaLimit as duration default '72 hours'",
            ContentValidation: DurationValidation,
            ImpliedQualifiers: [new DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Time, QualifierOrigin.Baseline)]
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
            UsageExample: "field GracePeriod as period default '30 days'",
            ContentValidation: PeriodValidation
        ),

        TypeKind.Timezone => new(
            kind, Tokens.GetMeta(TokenKind.TimezoneType),
            "IANA timezone identifier",
            TypeCategory.Temporal,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            DisplayName: "timezone",
            HoverDescription: "An IANA timezone identifier such as 'America/New_York'. Carries notempty implicitly — empty timezone is not meaningful.",
            UsageExample: "field ClinicTimezone as timezone default 'America/New_York'",
            ContentValidation: TimezoneValidation
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
            UsageExample: "field IncidentContext as zoneddatetime",
            ContentValidation: ZonedDateTimeValidation
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
            UsageExample: "field ScheduledFor as datetime default '2026-04-13T09:00:00'",
            ContentValidation: DateTimeValidation
        ),

        // ── Business-Domain ────────────────────────────────────────────
        TypeKind.Money => new(
            kind, Tokens.GetMeta(TokenKind.MoneyType),
            "Monetary amount with currency",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_Currency,
            RequiredBoundQualifierAxes: [QualifierAxis.Currency],
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("amount",   TypeKind.Decimal,  "Numeric amount"),
                new FixedReturnAccessor("currency", TypeKind.Currency, "Currency identifier", ReturnsQualifier: QualifierAxis.Currency),
            ],
            DisplayName: "money",
            HoverDescription: "A monetary amount bound to a currency. Use 'in USD' to pin currency. Arithmetic enforces same-currency rules.",
            UsageExample: "field TotalCost as money in 'USD'",
            ContentValidation: MoneyLiteralValidation
        ),

        TypeKind.Currency => new(
            kind, Tokens.GetMeta(TokenKind.CurrencyType),
            "Currency identity (e.g., USD, EUR)",
            TypeCategory.BusinessDomain,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            DisplayName: "currency",
            HoverDescription: "An ISO 4217 currency code identifier such as 'USD' or 'EUR'. Carries notempty implicitly.",
            UsageExample: "field BaseCurrency as currency default 'USD'",
            ContentValidation: CurrencyValidation
        ),

        TypeKind.Quantity => new(
            kind, Tokens.GetMeta(TokenKind.QuantityType),
            "Measured amount with unit",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_UnitOrDimension,
            RequiredBoundQualifierAxes: [QualifierAxis.Unit, QualifierAxis.Dimension],
            Traits: TypeTrait.Orderable | TypeTrait.EqualityComparable,
            Accessors:
            [
                new FixedReturnAccessor("amount",    TypeKind.Decimal,       "Numeric magnitude"),
                new FixedReturnAccessor("unit",      TypeKind.UnitOfMeasure, "Unit of measure", ReturnsQualifier: QualifierAxis.Unit),
                new FixedReturnAccessor("dimension", TypeKind.Dimension,     "Dimension family"),
            ],
            DisplayName: "quantity",
            HoverDescription: "A measured amount bound to a unit of measure. Use 'in kg' to pin units or 'of length' to constrain by dimension. Arithmetic enforces same-dimension rules.",
            UsageExample: "field Weight as quantity in 'kg'",
            ContentValidation: QuantityLiteralValidation
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
            UsageExample: "field StockingUom as unitofmeasure default 'each'",
            ContentValidation: UnitOfMeasureValidation
        ),

        TypeKind.Dimension => new(
            kind, Tokens.GetMeta(TokenKind.DimensionType),
            "Dimension family identity (e.g., length, mass)",
            TypeCategory.BusinessDomain,
            Traits: TypeTrait.EqualityComparable,
            ImpliedModifiers: [ModifierKind.Notempty],
            DisplayName: "dimension",
            HoverDescription: "A dimension family identifier such as 'length' or 'mass'. Used to enforce dimensional consistency in quantity arithmetic.",
            UsageExample: "field MeasuredDimension as dimension default 'mass'",
            ContentValidation: DimensionValidation
        ),

        TypeKind.Price => new(
            kind, Tokens.GetMeta(TokenKind.PriceType),
            "Price: monetary amount per unit",
            TypeCategory.BusinessDomain,
            QualifierShape: QS_CurrencyAndDimension,
            RequiredBoundQualifierAxes: [QualifierAxis.Currency],
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
            UsageExample: "field UnitPrice as price in 'USD/each'",
            ContentValidation: PriceLiteralValidation
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
            UsageExample: "field FxRate as exchangerate in 'USD' to 'EUR'",
            ContentValidation: ExchangeRateLiteralValidation
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

        TypeKind.Log => new(
            kind, Tokens.GetMeta(TokenKind.LogType),
            "Append-only ordered log (insertion order)",
            TypeCategory.Collection,
            Accessors: LogAccessors,
            DisplayName: "log",
            HoverDescription: "An append-only ordered log. Use append action. Supports .count, .first, .last, and .at(N) accessors. No clear action — append-only.",
            UsageExample: "field AuditTrail as log of string",
            NotemptyApplicable: true
        ),

        TypeKind.LogBy => new(
            kind, Tokens.GetMeta(TokenKind.LogType),
            "Append-only ordered log keyed by an ordering value (P unique)",
            TypeCategory.Collection,
            Accessors: LogByAccessors,
            DisplayName: "log by",
            HoverDescription: "An append-only log ordered by an external key P. Use 'append F Expr by P'. P must be unique across entries. Supports .count, .first, .last, and .at(N) accessors.",
            UsageExample: "field AuditLog as log of string by instant",
            NotemptyApplicable: true
        ),

        TypeKind.Bag => new(
            kind, Tokens.GetMeta(TokenKind.BagType),
            "Unordered multiset (element plus count)",
            TypeCategory.Collection,
            Accessors: BagAccessors,
            DisplayName: "bag",
            HoverDescription: "An unordered collection that tracks element frequency. Use add and remove actions. Supports .count and .countof(E) accessors.",
            UsageExample: "field CartItems as bag of string",
            NotemptyApplicable: true
        ),

        TypeKind.List => new(
            kind, Tokens.GetMeta(TokenKind.ListType),
            "Ordered list with index access and mutable positions",
            TypeCategory.Collection,
            Accessors: ListAccessors,
            DisplayName: "list",
            HoverDescription: "An ordered list with positional access and insertion. Use append, insert, remove at actions. Supports .count, .first, .last, and .at(N) accessors.",
            UsageExample: "field ApprovalChain as list of string",
            NotemptyApplicable: true
        ),

        TypeKind.QueueBy => new(
            kind, Tokens.GetMeta(TokenKind.QueueType),
            "Priority queue ordered by an ordering key P",
            TypeCategory.Collection,
            Accessors: QueueByAccessors,
            DisplayName: "queue by",
            HoverDescription: "A priority queue ordered by an external key P. Use enqueue by / dequeue actions. Supports .count, .peek, and .peekby accessors.",
            UsageExample: "field ClaimQueue as queue of string by integer",
            NotemptyApplicable: true
        ),

        TypeKind.Lookup => new(
            kind, Tokens.GetMeta(TokenKind.LookupType),
            "Key-value map (keys unique, F for K access)",
            TypeCategory.Collection,
            Accessors: LookupAccessors,
            DisplayName: "lookup",
            HoverDescription: "A key-value map with unique keys. Use put and remove actions. Access values with 'F for K'. Supports .count accessor.",
            UsageExample: "field CoverageLimits as lookup of string to decimal",
            NotemptyApplicable: false
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
                dict.TryAdd(meta.Token.Kind, meta);
        }

        // The lexer emits TokenKind.Set; the parser reinterprets as SetType in type position.
        // Map both to the same TypeMeta so either key works for lookup.
        if (dict.TryGetValue(TokenKind.Set, out var setMeta))
            dict[TokenKind.SetType] = setMeta;

        return dict.ToFrozenDictionary();
    }
}
