using System.Collections.Frozen;

namespace Precept.Language;

/// <summary>
/// Catalog of all typed operator combinations. Each entry is one legal
/// (operator, operand type(s)) → result type triple. Source of truth for
/// the type checker, doc generation, MCP vocabulary, and evaluator dispatch.
/// Replaces the ad-hoc OperatorTable when fully wired.
/// </summary>
public static class Operations
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Shared ParameterMeta instances — one per TypeKind used as an operand
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly ParameterMeta PInteger = new(TypeKind.Integer);
    private static readonly ParameterMeta PDecimal = new(TypeKind.Decimal);
    private static readonly ParameterMeta PNumber = new(TypeKind.Number);
    private static readonly ParameterMeta PBoolean = new(TypeKind.Boolean);
    private static readonly ParameterMeta PString = new(TypeKind.String);
    private static readonly ParameterMeta PChoice = new(TypeKind.Choice);
    private static readonly ParameterMeta PMoney = new(TypeKind.Money);
    private static readonly ParameterMeta PCurrency = new(TypeKind.Currency);
    private static readonly ParameterMeta PQuantity = new(TypeKind.Quantity);
    private static readonly ParameterMeta PUnitOfMeasure = new(TypeKind.UnitOfMeasure);
    private static readonly ParameterMeta PDimension = new(TypeKind.Dimension);
    private static readonly ParameterMeta PPrice = new(TypeKind.Price);
    private static readonly ParameterMeta PExchangeRate = new(TypeKind.ExchangeRate);
    private static readonly ParameterMeta PDate = new(TypeKind.Date);
    private static readonly ParameterMeta PTime = new(TypeKind.Time);
    private static readonly ParameterMeta PInstant = new(TypeKind.Instant);
    private static readonly ParameterMeta PDuration = new(TypeKind.Duration);
    private static readonly ParameterMeta PPeriod = new(TypeKind.Period);
    private static readonly ParameterMeta PTimezone = new(TypeKind.Timezone);
    private static readonly ParameterMeta PZonedDateTime = new(TypeKind.ZonedDateTime);
    private static readonly ParameterMeta PDateTime = new(TypeKind.DateTime);

    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static OperationMeta GetMeta(OperationKind kind) => kind switch
    {
        // ── Unary ──────────────────────────────────────────────────
        OperationKind.NegateInteger => new UnaryOperationMeta(
            kind, OperatorKind.Negate, PInteger, TypeKind.Integer,
            "Integer negation"),

        OperationKind.NegateDecimal => new UnaryOperationMeta(
            kind, OperatorKind.Negate, PDecimal, TypeKind.Decimal,
            "Decimal negation"),

        OperationKind.NegateNumber => new UnaryOperationMeta(
            kind, OperatorKind.Negate, PNumber, TypeKind.Number,
            "Number negation"),

        OperationKind.NegateMoney => new UnaryOperationMeta(
            kind, OperatorKind.Negate, PMoney, TypeKind.Money,
            "Money negation — preserves currency"),

        OperationKind.NegateQuantity => new UnaryOperationMeta(
            kind, OperatorKind.Negate, PQuantity, TypeKind.Quantity,
            "Quantity negation — preserves unit and dimension"),

        OperationKind.NegatePrice => new UnaryOperationMeta(
            kind, OperatorKind.Negate, PPrice, TypeKind.Price,
            "Price negation — preserves currency and unit"),

        OperationKind.NegateDuration => new UnaryOperationMeta(
            kind, OperatorKind.Negate, PDuration, TypeKind.Duration,
            "Duration negation"),

        OperationKind.NegatePeriod => new UnaryOperationMeta(
            kind, OperatorKind.Negate, PPeriod, TypeKind.Period,
            "Period negation — preserves structural components"),

        OperationKind.NotBoolean => new UnaryOperationMeta(
            kind, OperatorKind.Not, PBoolean, TypeKind.Boolean,
            "Logical negation"),

        // ── Scalar: integer ────────────────────────────────────────
        OperationKind.IntegerPlusInteger => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PInteger, PInteger, TypeKind.Integer,
            "Integer addition"),

        OperationKind.IntegerMinusInteger => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PInteger, PInteger, TypeKind.Integer,
            "Integer subtraction"),

        OperationKind.IntegerTimesInteger => new BinaryOperationMeta(
            kind, OperatorKind.Times, PInteger, PInteger, TypeKind.Integer,
            "Integer multiplication"),

        OperationKind.IntegerDivideInteger => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PInteger, PInteger, TypeKind.Integer,
            "Integer division (truncating)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PInteger), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.IntegerModuloInteger => new BinaryOperationMeta(
            kind, OperatorKind.Modulo, PInteger, PInteger, TypeKind.Integer,
            "Integer modulo",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PInteger), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ── Scalar: decimal ────────────────────────────────────────
        OperationKind.DecimalPlusDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PDecimal, PDecimal, TypeKind.Decimal,
            "Decimal addition"),

        OperationKind.DecimalMinusDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PDecimal, PDecimal, TypeKind.Decimal,
            "Decimal subtraction"),

        OperationKind.DecimalTimesDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Times, PDecimal, PDecimal, TypeKind.Decimal,
            "Decimal multiplication"),

        OperationKind.DecimalDivideDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PDecimal, PDecimal, TypeKind.Decimal,
            "Decimal division",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDecimal), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.DecimalModuloDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Modulo, PDecimal, PDecimal, TypeKind.Decimal,
            "Decimal modulo",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDecimal), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ── Scalar: number ─────────────────────────────────────────
        OperationKind.NumberPlusNumber => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PNumber, PNumber, TypeKind.Number,
            "Number addition (IEEE 754)"),

        OperationKind.NumberMinusNumber => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PNumber, PNumber, TypeKind.Number,
            "Number subtraction (IEEE 754)"),

        OperationKind.NumberTimesNumber => new BinaryOperationMeta(
            kind, OperatorKind.Times, PNumber, PNumber, TypeKind.Number,
            "Number multiplication (IEEE 754)"),

        OperationKind.NumberDivideNumber => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PNumber, PNumber, TypeKind.Number,
            "Number division (IEEE 754)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PNumber), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.NumberModuloNumber => new BinaryOperationMeta(
            kind, OperatorKind.Modulo, PNumber, PNumber, TypeKind.Number,
            "Number modulo (IEEE 754)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PNumber), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ── Scalar: widening integer → decimal ─────────────────────
        OperationKind.IntegerPlusDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PInteger, PDecimal, TypeKind.Decimal,
            "Integer + decimal (widens to decimal)", BidirectionalLookup: true),

        OperationKind.IntegerMinusDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PInteger, PDecimal, TypeKind.Decimal,
            "Integer − decimal (widens to decimal)", BidirectionalLookup: true),

        OperationKind.IntegerTimesDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Times, PInteger, PDecimal, TypeKind.Decimal,
            "Integer × decimal (widens to decimal)", BidirectionalLookup: true),

        OperationKind.IntegerDivideDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PInteger, PDecimal, TypeKind.Decimal,
            "Integer ÷ decimal (widens to decimal)", BidirectionalLookup: true,
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDecimal), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.IntegerModuloDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Modulo, PInteger, PDecimal, TypeKind.Decimal,
            "Integer % decimal (widens to decimal)", BidirectionalLookup: true,
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDecimal), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ── Scalar: widening integer → number ──────────────────────
        OperationKind.IntegerPlusNumber => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PInteger, PNumber, TypeKind.Number,
            "Integer + number (widens to number)", BidirectionalLookup: true),

        OperationKind.IntegerMinusNumber => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PInteger, PNumber, TypeKind.Number,
            "Integer − number (widens to number)", BidirectionalLookup: true),

        OperationKind.IntegerTimesNumber => new BinaryOperationMeta(
            kind, OperatorKind.Times, PInteger, PNumber, TypeKind.Number,
            "Integer × number (widens to number)", BidirectionalLookup: true),

        OperationKind.IntegerDivideNumber => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PInteger, PNumber, TypeKind.Number,
            "Integer ÷ number (widens to number)", BidirectionalLookup: true,
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PNumber), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.IntegerModuloNumber => new BinaryOperationMeta(
            kind, OperatorKind.Modulo, PInteger, PNumber, TypeKind.Number,
            "Integer % number (widens to number)", BidirectionalLookup: true,
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PNumber), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ── String ─────────────────────────────────────────────────
        OperationKind.StringPlusString => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PString, PString, TypeKind.String,
            "String concatenation"),

        // ── Temporal: date ──────────────────────────────────────────
        OperationKind.DatePlusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PDate, PPeriod, TypeKind.Date,
            "Date + period → date (calendar arithmetic)",
            ProofRequirements:
            [
                new DimensionProofRequirement(new ParamSubject(PPeriod), PeriodDimension.Date,
                    "Period must be a date-level period (year, month, or day)"),
            ]),

        OperationKind.DateMinusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PDate, PPeriod, TypeKind.Date,
            "Date − period → date (calendar arithmetic)",
            ProofRequirements:
            [
                new DimensionProofRequirement(new ParamSubject(PPeriod), PeriodDimension.Date,
                    "Period must be a date-level period (year, month, or day)"),
            ]),

        OperationKind.DateMinusDate => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PDate, PDate, TypeKind.Period,
            "Date − date → period (calendar distance)"),

        OperationKind.DatePlusTime => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PDate, PTime, TypeKind.DateTime,
            "Date + time → datetime (composition)", BidirectionalLookup: true),

        // ── Temporal: time ──────────────────────────────────────────
        OperationKind.TimePlusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PTime, PPeriod, TypeKind.Time,
            "Time + period → time (requires period of 'time')",
            ProofRequirements:
            [
                new DimensionProofRequirement(new ParamSubject(PPeriod), PeriodDimension.Time,
                    "Period must be a time-level period (hour, minute, or second)"),
            ]),

        OperationKind.TimeMinusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PTime, PPeriod, TypeKind.Time,
            "Time − period → time (requires period of 'time')",
            ProofRequirements:
            [
                new DimensionProofRequirement(new ParamSubject(PPeriod), PeriodDimension.Time,
                    "Period must be a time-level period (hour, minute, or second)"),
            ]),

        OperationKind.TimePlusDuration => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PTime, PDuration, TypeKind.Time,
            "Time + duration → time (sub-day bridging, wraps at midnight)"),

        OperationKind.TimeMinusDuration => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PTime, PDuration, TypeKind.Time,
            "Time − duration → time (sub-day bridging, wraps at midnight)"),

        OperationKind.TimeMinusTime => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PTime, PTime, TypeKind.Period,
            "Time − time → period (time-component period)"),

        // ── Temporal: instant ───────────────────────────────────────
        OperationKind.InstantMinusInstant => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PInstant, PInstant, TypeKind.Duration,
            "Instant − instant → duration (elapsed nanoseconds)"),

        OperationKind.InstantPlusDuration => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PInstant, PDuration, TypeKind.Instant,
            "Instant + duration → instant (timeline offset)"),

        OperationKind.InstantMinusDuration => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PInstant, PDuration, TypeKind.Instant,
            "Instant − duration → instant (timeline offset)"),

        // ── Temporal: duration ──────────────────────────────────────
        OperationKind.DurationPlusDuration => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PDuration, PDuration, TypeKind.Duration,
            "Duration + duration → duration"),

        OperationKind.DurationMinusDuration => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PDuration, PDuration, TypeKind.Duration,
            "Duration − duration → duration"),

        OperationKind.DurationTimesInteger => new BinaryOperationMeta(
            kind, OperatorKind.Times, PDuration, PInteger, TypeKind.Duration,
            "Duration × integer → duration (scaling)", BidirectionalLookup: true),

        OperationKind.DurationTimesNumber => new BinaryOperationMeta(
            kind, OperatorKind.Times, PDuration, PNumber, TypeKind.Duration,
            "Duration × number → duration (scaling)", BidirectionalLookup: true),

        OperationKind.DurationDivideInteger => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PDuration, PInteger, TypeKind.Duration,
            "Duration ÷ integer → duration (scaling)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PInteger), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.DurationDivideNumber => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PDuration, PNumber, TypeKind.Duration,
            "Duration ÷ number → duration (scaling)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PNumber), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.DurationDivideDuration => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PDuration, PDuration, TypeKind.Number,
            "Duration ÷ duration → number (ratio)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDuration), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ── Temporal: period ────────────────────────────────────────
        OperationKind.PeriodPlusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PPeriod, PPeriod, TypeKind.Period,
            "Period + period → period"),

        OperationKind.PeriodMinusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PPeriod, PPeriod, TypeKind.Period,
            "Period − period → period"),

        // ── Temporal: zoneddatetime ─────────────────────────────────
        OperationKind.ZonedDateTimePlusDuration => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PZonedDateTime, PDuration, TypeKind.ZonedDateTime,
            "ZonedDateTime + duration → zoneddatetime (timeline arithmetic)"),

        OperationKind.ZonedDateTimeMinusDuration => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PZonedDateTime, PDuration, TypeKind.ZonedDateTime,
            "ZonedDateTime − duration → zoneddatetime (timeline arithmetic)"),

        OperationKind.ZonedDateTimePlusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PZonedDateTime, PPeriod, TypeKind.ZonedDateTime,
            "ZonedDateTime + period → zoneddatetime (calendar arithmetic — accepts all components)"),

        OperationKind.ZonedDateTimeMinusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PZonedDateTime, PPeriod, TypeKind.ZonedDateTime,
            "ZonedDateTime − period → zoneddatetime (calendar arithmetic — accepts all components)"),

        OperationKind.ZonedDateTimeMinusZonedDateTime => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PZonedDateTime, PZonedDateTime, TypeKind.Duration,
            "ZonedDateTime − zoneddatetime → duration (instant subtraction)"),

        // ── Temporal: datetime ──────────────────────────────────────
        OperationKind.DateTimePlusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PDateTime, PPeriod, TypeKind.DateTime,
            "DateTime + period → datetime (accepts all components)"),

        OperationKind.DateTimeMinusPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PDateTime, PPeriod, TypeKind.DateTime,
            "DateTime − period → datetime (accepts all components)"),

        OperationKind.DateTimeMinusDateTime => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PDateTime, PDateTime, TypeKind.Period,
            "DateTime − datetime → period (calendar distance)"),

        // ── Business: money ─────────────────────────────────────────
        OperationKind.MoneyPlusMoney => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PMoney, PMoney, TypeKind.Money,
            "Money + money → money (same currency required)"),

        OperationKind.MoneyMinusMoney => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PMoney, PMoney, TypeKind.Money,
            "Money − money → money (same currency required)"),

        OperationKind.MoneyTimesDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Times, PMoney, PDecimal, TypeKind.Money,
            "Money × decimal → money (scaling)", BidirectionalLookup: true),

        OperationKind.MoneyDivideDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PMoney, PDecimal, TypeKind.Money,
            "Money ÷ decimal → money (scaling)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDecimal), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.MoneyDivideMoneySameCurrency => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PMoney, PMoney, TypeKind.Decimal,
            "Money ÷ money (same currency) → decimal (dimensionless ratio)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PMoney), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.MoneyDivideMoneyCrossCurrency => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PMoney, PMoney, TypeKind.ExchangeRate,
            "Money ÷ money (different currencies) → exchangerate",
            Match: QualifierMatch.Different,
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PMoney), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.MoneyDivideQuantity => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PMoney, PQuantity, TypeKind.Price,
            "Money ÷ quantity → price",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PQuantity), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.MoneyDividePeriod => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PMoney, PPeriod, TypeKind.Price,
            "Money ÷ period → price (time-based price derivation)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PPeriod), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.MoneyDivideDuration => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PMoney, PDuration, TypeKind.Price,
            "Money ÷ duration → price (hours/minutes/seconds denominators)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDuration), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ── Business: quantity ──────────────────────────────────────
        OperationKind.QuantityPlusQuantity => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PQuantity, PQuantity, TypeKind.Quantity,
            "Quantity + quantity → quantity (same dimension required)",
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
            ]),

        OperationKind.QuantityMinusQuantity => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PQuantity, PQuantity, TypeKind.Quantity,
            "Quantity − quantity → quantity (same dimension required)",
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
            ]),

        OperationKind.QuantityTimesDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Times, PQuantity, PDecimal, TypeKind.Quantity,
            "Quantity × decimal → quantity (scaling)", BidirectionalLookup: true),

        OperationKind.QuantityDivideDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PQuantity, PDecimal, TypeKind.Quantity,
            "Quantity ÷ decimal → quantity (scaling)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDecimal), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.QuantityDivideQuantitySameDimension => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PQuantity, PQuantity, TypeKind.Decimal,
            "Quantity ÷ quantity (same dimension) → decimal (dimensionless ratio)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PQuantity), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.QuantityDivideQuantityCrossDimension => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PQuantity, PQuantity, TypeKind.Quantity,
            "Quantity ÷ quantity (different dimensions) → compound quantity",
            Match: QualifierMatch.Different,
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PQuantity), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.QuantityDividePeriod => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PQuantity, PPeriod, TypeKind.Quantity,
            "Quantity ÷ period → compound quantity (time-denominator rate)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PPeriod), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.QuantityDivideDuration => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PQuantity, PDuration, TypeKind.Quantity,
            "Quantity ÷ duration → compound quantity (hours/min/sec denominators)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDuration), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        OperationKind.QuantityTimesQuantity => new BinaryOperationMeta(
            kind, OperatorKind.Times, PQuantity, PQuantity, TypeKind.Quantity,
            "Quantity × quantity → quantity (dimensional cancellation)"),

        OperationKind.QuantityTimesPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Times, PQuantity, PPeriod, TypeKind.Quantity,
            "Quantity × period → quantity (time-denominator cancellation)", BidirectionalLookup: true),

        OperationKind.QuantityTimesDuration => new BinaryOperationMeta(
            kind, OperatorKind.Times, PQuantity, PDuration, TypeKind.Quantity,
            "Quantity × duration → quantity (time-denominator cancellation)", BidirectionalLookup: true),

        // ── Business: price ─────────────────────────────────────────
        OperationKind.PricePlusPrice => new BinaryOperationMeta(
            kind, OperatorKind.Plus, PPrice, PPrice, TypeKind.Price,
            "Price + price → price (same currency and unit required)",
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Currency,
                    "Operands must have matching currency qualifiers"),
            ]),

        OperationKind.PriceMinusPrice => new BinaryOperationMeta(
            kind, OperatorKind.Minus, PPrice, PPrice, TypeKind.Price,
            "Price − price → price (same currency and unit required)",
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Currency,
                    "Operands must have matching currency qualifiers"),
            ]),

        OperationKind.PriceTimesQuantity => new BinaryOperationMeta(
            kind, OperatorKind.Times, PPrice, PQuantity, TypeKind.Money,
            "Price × quantity → money (dimensional cancellation)", BidirectionalLookup: true),

        OperationKind.PriceTimesPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Times, PPrice, PPeriod, TypeKind.Money,
            "Price × period → money (time-denominator cancellation)", BidirectionalLookup: true),

        OperationKind.PriceTimesDuration => new BinaryOperationMeta(
            kind, OperatorKind.Times, PPrice, PDuration, TypeKind.Money,
            "Price × duration → money (hours/min/sec cancellation)", BidirectionalLookup: true),

        OperationKind.PriceTimesDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Times, PPrice, PDecimal, TypeKind.Price,
            "Price × decimal → price (scaling)", BidirectionalLookup: true),

        OperationKind.PriceDivideDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PPrice, PDecimal, TypeKind.Price,
            "Price ÷ decimal → price (scaling)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDecimal), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ── Business: exchangerate ──────────────────────────────────
        OperationKind.ExchangeRateTimesMoney => new BinaryOperationMeta(
            kind, OperatorKind.Times, PExchangeRate, PMoney, TypeKind.Money,
            "ExchangeRate × money → money (currency conversion)", BidirectionalLookup: true),

        OperationKind.ExchangeRateTimesDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Times, PExchangeRate, PDecimal, TypeKind.ExchangeRate,
            "ExchangeRate × decimal → exchangerate (scaling)", BidirectionalLookup: true),

        OperationKind.ExchangeRateDivideDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Divide, PExchangeRate, PDecimal, TypeKind.ExchangeRate,
            "ExchangeRate ÷ decimal → exchangerate (scaling)",
            ProofRequirements:
            [
                new NumericProofRequirement(new ParamSubject(PDecimal), OperatorKind.NotEquals, 0m,
                    "Divisor must be non-zero"),
            ]),

        // ════════════════════════════════════════════════════════════
        //  Comparison operations (result is always Boolean)
        // ════════════════════════════════════════════════════════════

        // ── Equality-only types ─────────────────────────────────────
        OperationKind.BooleanEqualsBoolean => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PBoolean, PBoolean, TypeKind.Boolean,
            "Boolean equality"),
        OperationKind.BooleanNotEqualsBoolean => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PBoolean, PBoolean, TypeKind.Boolean,
            "Boolean inequality"),

        OperationKind.PeriodEqualsPeriod => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PPeriod, PPeriod, TypeKind.Boolean,
            "Period structural equality"),
        OperationKind.PeriodNotEqualsPeriod => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PPeriod, PPeriod, TypeKind.Boolean,
            "Period structural inequality"),

        OperationKind.TimezoneEqualsTimezone => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PTimezone, PTimezone, TypeKind.Boolean,
            "Timezone equality"),
        OperationKind.TimezoneNotEqualsTimezone => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PTimezone, PTimezone, TypeKind.Boolean,
            "Timezone inequality"),

        OperationKind.ZonedDateTimeEqualsZonedDateTime => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PZonedDateTime, PZonedDateTime, TypeKind.Boolean,
            "ZonedDateTime equality (multi-dimensional: instant, then local datetime, then zone ID)"),
        OperationKind.ZonedDateTimeNotEqualsZonedDateTime => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PZonedDateTime, PZonedDateTime, TypeKind.Boolean,
            "ZonedDateTime inequality"),

        OperationKind.CurrencyEqualsCurrency => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PCurrency, PCurrency, TypeKind.Boolean,
            "Currency equality (ISO 4217)"),
        OperationKind.CurrencyNotEqualsCurrency => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PCurrency, PCurrency, TypeKind.Boolean,
            "Currency inequality"),

        OperationKind.UnitOfMeasureEqualsUnitOfMeasure => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PUnitOfMeasure, PUnitOfMeasure, TypeKind.Boolean,
            "Unit of measure equality"),
        OperationKind.UnitOfMeasureNotEqualsUnitOfMeasure => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PUnitOfMeasure, PUnitOfMeasure, TypeKind.Boolean,
            "Unit of measure inequality"),

        OperationKind.DimensionEqualsDimension => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PDimension, PDimension, TypeKind.Boolean,
            "Dimension equality"),
        OperationKind.DimensionNotEqualsDimension => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PDimension, PDimension, TypeKind.Boolean,
            "Dimension inequality"),

        OperationKind.ExchangeRateEqualsExchangeRate => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PExchangeRate, PExchangeRate, TypeKind.Boolean,
            "ExchangeRate equality (same currency pair required)",
            Match: QualifierMatch.Same),
        OperationKind.ExchangeRateNotEqualsExchangeRate => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PExchangeRate, PExchangeRate, TypeKind.Boolean,
            "ExchangeRate inequality (same currency pair required)",
            Match: QualifierMatch.Same),

        // ── Orderable same-type: integer ────────────────────────────
        OperationKind.IntegerEqualsInteger => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PInteger, PInteger, TypeKind.Boolean,
            "Integer equality"),
        OperationKind.IntegerNotEqualsInteger => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PInteger, PInteger, TypeKind.Boolean,
            "Integer inequality"),
        OperationKind.IntegerLessThanInteger => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PInteger, PInteger, TypeKind.Boolean,
            "Integer less-than"),
        OperationKind.IntegerGreaterThanInteger => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PInteger, PInteger, TypeKind.Boolean,
            "Integer greater-than"),
        OperationKind.IntegerLessThanOrEqualInteger => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PInteger, PInteger, TypeKind.Boolean,
            "Integer less-than-or-equal"),
        OperationKind.IntegerGreaterThanOrEqualInteger => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PInteger, PInteger, TypeKind.Boolean,
            "Integer greater-than-or-equal"),

        // ── Orderable same-type: decimal ────────────────────────────
        OperationKind.DecimalEqualsDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PDecimal, PDecimal, TypeKind.Boolean,
            "Decimal equality"),
        OperationKind.DecimalNotEqualsDecimal => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PDecimal, PDecimal, TypeKind.Boolean,
            "Decimal inequality"),
        OperationKind.DecimalLessThanDecimal => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PDecimal, PDecimal, TypeKind.Boolean,
            "Decimal less-than"),
        OperationKind.DecimalGreaterThanDecimal => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PDecimal, PDecimal, TypeKind.Boolean,
            "Decimal greater-than"),
        OperationKind.DecimalLessThanOrEqualDecimal => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PDecimal, PDecimal, TypeKind.Boolean,
            "Decimal less-than-or-equal"),
        OperationKind.DecimalGreaterThanOrEqualDecimal => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PDecimal, PDecimal, TypeKind.Boolean,
            "Decimal greater-than-or-equal"),

        // ── Orderable same-type: number ─────────────────────────────
        OperationKind.NumberEqualsNumber => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PNumber, PNumber, TypeKind.Boolean,
            "Number equality (IEEE 754)"),
        OperationKind.NumberNotEqualsNumber => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PNumber, PNumber, TypeKind.Boolean,
            "Number inequality (IEEE 754)"),
        OperationKind.NumberLessThanNumber => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PNumber, PNumber, TypeKind.Boolean,
            "Number less-than"),
        OperationKind.NumberGreaterThanNumber => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PNumber, PNumber, TypeKind.Boolean,
            "Number greater-than"),
        OperationKind.NumberLessThanOrEqualNumber => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PNumber, PNumber, TypeKind.Boolean,
            "Number less-than-or-equal"),
        OperationKind.NumberGreaterThanOrEqualNumber => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PNumber, PNumber, TypeKind.Boolean,
            "Number greater-than-or-equal"),

        // ── Orderable same-type: string ─────────────────────────────
        OperationKind.StringEqualsString => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PString, PString, TypeKind.Boolean,
            "String equality"),
        OperationKind.StringNotEqualsString => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PString, PString, TypeKind.Boolean,
            "String inequality"),

        // ── Orderable same-type: choice ─────────────────────────────
        OperationKind.ChoiceEqualsChoice => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PChoice, PChoice, TypeKind.Boolean,
            "Choice equality (same set required)"),
        OperationKind.ChoiceNotEqualsChoice => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PChoice, PChoice, TypeKind.Boolean,
            "Choice inequality (same set required)"),
        OperationKind.ChoiceLessThanChoice => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PChoice, PChoice, TypeKind.Boolean,
            "Choice less-than (ordinal, ordered + same set required)",
            ProofRequirements:
            [
                new ModifierRequirement(new ParamSubject(PChoice), ModifierKind.Ordered,
                    "Both choice operands must be declared ordered"),
            ]),
        OperationKind.ChoiceGreaterThanChoice => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PChoice, PChoice, TypeKind.Boolean,
            "Choice greater-than (ordinal, ordered + same set required)",
            ProofRequirements:
            [
                new ModifierRequirement(new ParamSubject(PChoice), ModifierKind.Ordered,
                    "Both choice operands must be declared ordered"),
            ]),
        OperationKind.ChoiceLessThanOrEqualChoice => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PChoice, PChoice, TypeKind.Boolean,
            "Choice less-than-or-equal (ordinal, ordered + same set required)",
            ProofRequirements:
            [
                new ModifierRequirement(new ParamSubject(PChoice), ModifierKind.Ordered,
                    "Both choice operands must be declared ordered"),
            ]),
        OperationKind.ChoiceGreaterThanOrEqualChoice => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PChoice, PChoice, TypeKind.Boolean,
            "Choice greater-than-or-equal (ordinal, ordered + same set required)",
            ProofRequirements:
            [
                new ModifierRequirement(new ParamSubject(PChoice), ModifierKind.Ordered,
                    "Both choice operands must be declared ordered"),
            ]),

        // ── Orderable same-type: date ───────────────────────────────
        OperationKind.DateEqualsDate => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PDate, PDate, TypeKind.Boolean,
            "Date equality"),
        OperationKind.DateNotEqualsDate => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PDate, PDate, TypeKind.Boolean,
            "Date inequality"),
        OperationKind.DateLessThanDate => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PDate, PDate, TypeKind.Boolean,
            "Date less-than (earlier)"),
        OperationKind.DateGreaterThanDate => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PDate, PDate, TypeKind.Boolean,
            "Date greater-than (later)"),
        OperationKind.DateLessThanOrEqualDate => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PDate, PDate, TypeKind.Boolean,
            "Date less-than-or-equal"),
        OperationKind.DateGreaterThanOrEqualDate => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PDate, PDate, TypeKind.Boolean,
            "Date greater-than-or-equal"),

        // ── Orderable same-type: time ───────────────────────────────
        OperationKind.TimeEqualsTime => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PTime, PTime, TypeKind.Boolean,
            "Time equality"),
        OperationKind.TimeNotEqualsTime => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PTime, PTime, TypeKind.Boolean,
            "Time inequality"),
        OperationKind.TimeLessThanTime => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PTime, PTime, TypeKind.Boolean,
            "Time less-than (earlier)"),
        OperationKind.TimeGreaterThanTime => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PTime, PTime, TypeKind.Boolean,
            "Time greater-than (later)"),
        OperationKind.TimeLessThanOrEqualTime => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PTime, PTime, TypeKind.Boolean,
            "Time less-than-or-equal"),
        OperationKind.TimeGreaterThanOrEqualTime => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PTime, PTime, TypeKind.Boolean,
            "Time greater-than-or-equal"),

        // ── Orderable same-type: instant ────────────────────────────
        OperationKind.InstantEqualsInstant => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PInstant, PInstant, TypeKind.Boolean,
            "Instant equality"),
        OperationKind.InstantNotEqualsInstant => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PInstant, PInstant, TypeKind.Boolean,
            "Instant inequality"),
        OperationKind.InstantLessThanInstant => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PInstant, PInstant, TypeKind.Boolean,
            "Instant less-than (earlier)"),
        OperationKind.InstantGreaterThanInstant => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PInstant, PInstant, TypeKind.Boolean,
            "Instant greater-than (later)"),
        OperationKind.InstantLessThanOrEqualInstant => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PInstant, PInstant, TypeKind.Boolean,
            "Instant less-than-or-equal"),
        OperationKind.InstantGreaterThanOrEqualInstant => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PInstant, PInstant, TypeKind.Boolean,
            "Instant greater-than-or-equal"),

        // ── Orderable same-type: duration ───────────────────────────
        OperationKind.DurationEqualsDuration => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PDuration, PDuration, TypeKind.Boolean,
            "Duration equality"),
        OperationKind.DurationNotEqualsDuration => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PDuration, PDuration, TypeKind.Boolean,
            "Duration inequality"),
        OperationKind.DurationLessThanDuration => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PDuration, PDuration, TypeKind.Boolean,
            "Duration less-than (shorter)"),
        OperationKind.DurationGreaterThanDuration => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PDuration, PDuration, TypeKind.Boolean,
            "Duration greater-than (longer)"),
        OperationKind.DurationLessThanOrEqualDuration => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PDuration, PDuration, TypeKind.Boolean,
            "Duration less-than-or-equal"),
        OperationKind.DurationGreaterThanOrEqualDuration => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PDuration, PDuration, TypeKind.Boolean,
            "Duration greater-than-or-equal"),

        // ── Orderable same-type: datetime ───────────────────────────
        OperationKind.DateTimeEqualsDateTime => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PDateTime, PDateTime, TypeKind.Boolean,
            "DateTime equality"),
        OperationKind.DateTimeNotEqualsDateTime => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PDateTime, PDateTime, TypeKind.Boolean,
            "DateTime inequality"),
        OperationKind.DateTimeLessThanDateTime => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PDateTime, PDateTime, TypeKind.Boolean,
            "DateTime less-than (earlier)"),
        OperationKind.DateTimeGreaterThanDateTime => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PDateTime, PDateTime, TypeKind.Boolean,
            "DateTime greater-than (later)"),
        OperationKind.DateTimeLessThanOrEqualDateTime => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PDateTime, PDateTime, TypeKind.Boolean,
            "DateTime less-than-or-equal"),
        OperationKind.DateTimeGreaterThanOrEqualDateTime => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PDateTime, PDateTime, TypeKind.Boolean,
            "DateTime greater-than-or-equal"),

        // ── Orderable same-type: money (same currency required) ─────
        OperationKind.MoneyEqualsMoney => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PMoney, PMoney, TypeKind.Boolean,
            "Money equality (same currency required)",
            Match: QualifierMatch.Same),
        OperationKind.MoneyNotEqualsMoney => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PMoney, PMoney, TypeKind.Boolean,
            "Money inequality (same currency required)",
            Match: QualifierMatch.Same),
        OperationKind.MoneyLessThanMoney => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PMoney, PMoney, TypeKind.Boolean,
            "Money less-than (same currency required)",
            Match: QualifierMatch.Same),
        OperationKind.MoneyGreaterThanMoney => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PMoney, PMoney, TypeKind.Boolean,
            "Money greater-than (same currency required)",
            Match: QualifierMatch.Same),
        OperationKind.MoneyLessThanOrEqualMoney => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PMoney, PMoney, TypeKind.Boolean,
            "Money less-than-or-equal (same currency required)",
            Match: QualifierMatch.Same),
        OperationKind.MoneyGreaterThanOrEqualMoney => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PMoney, PMoney, TypeKind.Boolean,
            "Money greater-than-or-equal (same currency required)",
            Match: QualifierMatch.Same),

        // ── Orderable same-type: quantity (same dimension required) ──
        OperationKind.QuantityEqualsQuantity => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PQuantity, PQuantity, TypeKind.Boolean,
            "Quantity equality (same dimension required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
            ]),
        OperationKind.QuantityNotEqualsQuantity => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PQuantity, PQuantity, TypeKind.Boolean,
            "Quantity inequality (same dimension required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
            ]),
        OperationKind.QuantityLessThanQuantity => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PQuantity, PQuantity, TypeKind.Boolean,
            "Quantity less-than (same dimension required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
            ]),
        OperationKind.QuantityGreaterThanQuantity => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PQuantity, PQuantity, TypeKind.Boolean,
            "Quantity greater-than (same dimension required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
            ]),
        OperationKind.QuantityLessThanOrEqualQuantity => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PQuantity, PQuantity, TypeKind.Boolean,
            "Quantity less-than-or-equal (same dimension required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
            ]),
        OperationKind.QuantityGreaterThanOrEqualQuantity => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PQuantity, PQuantity, TypeKind.Boolean,
            "Quantity greater-than-or-equal (same dimension required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
            ]),

        // ── Orderable same-type: price (same currency + unit required)
        OperationKind.PriceEqualsPrice => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PPrice, PPrice, TypeKind.Boolean,
            "Price equality (same currency and unit required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Currency,
                    "Operands must have matching currency qualifiers"),
            ]),
        OperationKind.PriceNotEqualsPrice => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PPrice, PPrice, TypeKind.Boolean,
            "Price inequality (same currency and unit required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Currency,
                    "Operands must have matching currency qualifiers"),
            ]),
        OperationKind.PriceLessThanPrice => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PPrice, PPrice, TypeKind.Boolean,
            "Price less-than (same currency and unit required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Currency,
                    "Operands must have matching currency qualifiers"),
            ]),
        OperationKind.PriceGreaterThanPrice => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PPrice, PPrice, TypeKind.Boolean,
            "Price greater-than (same currency and unit required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Currency,
                    "Operands must have matching currency qualifiers"),
            ]),
        OperationKind.PriceLessThanOrEqualPrice => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PPrice, PPrice, TypeKind.Boolean,
            "Price less-than-or-equal (same currency and unit required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Currency,
                    "Operands must have matching currency qualifiers"),
            ]),
        OperationKind.PriceGreaterThanOrEqualPrice => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PPrice, PPrice, TypeKind.Boolean,
            "Price greater-than-or-equal (same currency and unit required)",
            Match: QualifierMatch.Same,
            ProofRequirements:
            [
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Unit,
                    "Operands must have matching unit qualifiers"),
                new QualifierCompatibilityProofRequirement(new ParamSubject(PPrice), new ParamSubject(PPrice), QualifierAxis.Currency,
                    "Operands must have matching currency qualifiers"),
            ]),

        // ── Widening comparison: integer ↔ decimal ──────────────────
        OperationKind.IntegerEqualsDecimal => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PInteger, PDecimal, TypeKind.Boolean,
            "Integer == decimal (widens to decimal)", BidirectionalLookup: true),
        OperationKind.IntegerNotEqualsDecimal => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PInteger, PDecimal, TypeKind.Boolean,
            "Integer != decimal (widens to decimal)", BidirectionalLookup: true),
        OperationKind.IntegerLessThanDecimal => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PInteger, PDecimal, TypeKind.Boolean,
            "Integer < decimal (widens to decimal)", BidirectionalLookup: true),
        OperationKind.IntegerGreaterThanDecimal => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PInteger, PDecimal, TypeKind.Boolean,
            "Integer > decimal (widens to decimal)", BidirectionalLookup: true),
        OperationKind.IntegerLessThanOrEqualDecimal => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PInteger, PDecimal, TypeKind.Boolean,
            "Integer <= decimal (widens to decimal)", BidirectionalLookup: true),
        OperationKind.IntegerGreaterThanOrEqualDecimal => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PInteger, PDecimal, TypeKind.Boolean,
            "Integer >= decimal (widens to decimal)", BidirectionalLookup: true),

        // ── Widening comparison: integer ↔ number ───────────────────
        OperationKind.IntegerEqualsNumber => new BinaryOperationMeta(
            kind, OperatorKind.Equals, PInteger, PNumber, TypeKind.Boolean,
            "Integer == number (widens to number)", BidirectionalLookup: true),
        OperationKind.IntegerNotEqualsNumber => new BinaryOperationMeta(
            kind, OperatorKind.NotEquals, PInteger, PNumber, TypeKind.Boolean,
            "Integer != number (widens to number)", BidirectionalLookup: true),
        OperationKind.IntegerLessThanNumber => new BinaryOperationMeta(
            kind, OperatorKind.LessThan, PInteger, PNumber, TypeKind.Boolean,
            "Integer < number (widens to number)", BidirectionalLookup: true),
        OperationKind.IntegerGreaterThanNumber => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThan, PInteger, PNumber, TypeKind.Boolean,
            "Integer > number (widens to number)", BidirectionalLookup: true),
        OperationKind.IntegerLessThanOrEqualNumber => new BinaryOperationMeta(
            kind, OperatorKind.LessThanOrEqual, PInteger, PNumber, TypeKind.Boolean,
            "Integer <= number (widens to number)", BidirectionalLookup: true),
        OperationKind.IntegerGreaterThanOrEqualNumber => new BinaryOperationMeta(
            kind, OperatorKind.GreaterThanOrEqual, PInteger, PNumber, TypeKind.Boolean,
            "Integer >= number (widens to number)", BidirectionalLookup: true),

        // ── Case-insensitive: string only ───────────────────────────
        OperationKind.StringCaseInsensitiveEqualsString => new BinaryOperationMeta(
            kind, OperatorKind.CaseInsensitiveEquals, PString, PString, TypeKind.Boolean,
            "String case-insensitive equality (OrdinalIgnoreCase)"),
        OperationKind.StringCaseInsensitiveNotEqualsString => new BinaryOperationMeta(
            kind, OperatorKind.CaseInsensitiveNotEquals, PString, PString, TypeKind.Boolean,
            "String case-insensitive inequality (OrdinalIgnoreCase)"),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown OperationKind: {kind}")
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — materialized list
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<OperationMeta> All { get; } =
        Enum.GetValues<OperationKind>().Select(GetMeta).ToArray();

    // ════════════════════════════════════════════════════════════════════════════
    //  Indexes — O(1) lookup for type checker and evaluator
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Unary index: (Op, Operand.Kind) → UnaryOperationMeta.</summary>
    public static FrozenDictionary<(OperatorKind, TypeKind), UnaryOperationMeta> UnaryIndex { get; } =
        All.OfType<UnaryOperationMeta>()
           .ToFrozenDictionary(m => (m.Op, m.Operand.Kind));

    /// <summary>
    /// Binary index: (Op, Lhs.Kind, Rhs.Kind) → BinaryOperationMeta[].
    /// For bidirectional-lookup entries, both (Lhs, Rhs) and (Rhs, Lhs) orderings are indexed.
    /// Multi-entry groups occur when QualifierMatch disambiguates (money/money, quantity/quantity).
    /// </summary>
    public static FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta[]> BinaryIndex { get; } =
        BuildBinaryIndex();

    private static FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta[]> BuildBinaryIndex()
    {
        var pairs = new List<(
            (OperatorKind Op, TypeKind Lhs, TypeKind Rhs) Key,
            BinaryOperationMeta Meta)>();

        foreach (var meta in All.OfType<BinaryOperationMeta>())
        {
            var canonical = (meta.Op, meta.Lhs.Kind, meta.Rhs.Kind);
            pairs.Add((canonical, meta));

            // For bidirectional-lookup entries with different operand types, also index the reverse.
            if (meta.BidirectionalLookup && meta.Lhs.Kind != meta.Rhs.Kind)
            {
                var reverse = (meta.Op, meta.Rhs.Kind, meta.Lhs.Kind);
                pairs.Add((reverse, meta));
            }
        }

        return pairs
            .GroupBy(p => p.Key)
            .ToFrozenDictionary(g => g.Key, g => g.Select(p => p.Meta).ToArray());
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  FindCandidates — raw catalog query
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns all candidate entries for the given unary operator and operand type.
    /// </summary>
    public static UnaryOperationMeta? FindUnary(OperatorKind op, TypeKind operand) =>
        UnaryIndex.TryGetValue((op, operand), out var meta) ? meta : null;

    /// <summary>
    /// Returns all candidate entries for the given binary operator and operand types.
    /// Most triples have one entry; money/money and quantity/quantity division have two
    /// (disambiguated by <see cref="QualifierMatch"/>).
    /// </summary>
    public static ReadOnlySpan<BinaryOperationMeta> FindCandidates(
        OperatorKind op, TypeKind lhs, TypeKind rhs) =>
        BinaryIndex.TryGetValue((op, lhs, rhs), out var entries)
            ? entries.AsSpan()
            : ReadOnlySpan<BinaryOperationMeta>.Empty;
}
