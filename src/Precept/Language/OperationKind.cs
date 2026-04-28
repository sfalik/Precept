namespace Precept.Language;

/// <summary>
/// Every legal typed operator combination. Each member is one
/// (operator, operand type(s)) → result type triple.
/// Metadata lives in <see cref="Operations.GetMeta"/>, not on the enum.
/// </summary>
public enum OperationKind
{
    // ── Unary ──────────────────────────────────────────────────────
    NegateInteger,
    NegateDecimal,
    NegateNumber,
    NegateMoney,
    NegateQuantity,
    NegatePrice,
    NegateDuration,
    NegatePeriod,
    NotBoolean,

    // ── Scalar: same-type arithmetic ───────────────────────────────
    IntegerPlusInteger,
    IntegerMinusInteger,
    IntegerTimesInteger,
    IntegerDivideInteger,
    IntegerModuloInteger,

    DecimalPlusDecimal,
    DecimalMinusDecimal,
    DecimalTimesDecimal,
    DecimalDivideDecimal,
    DecimalModuloDecimal,

    NumberPlusNumber,
    NumberMinusNumber,
    NumberTimesNumber,
    NumberDivideNumber,
    NumberModuloNumber,

    // ── Scalar: widening (integer ↔ decimal) ───────────────────────
    IntegerPlusDecimal,
    IntegerMinusDecimal,
    IntegerTimesDecimal,
    IntegerDivideDecimal,
    IntegerModuloDecimal,

    // ── Scalar: widening (integer ↔ number) ────────────────────────
    IntegerPlusNumber,
    IntegerMinusNumber,
    IntegerTimesNumber,
    IntegerDivideNumber,
    IntegerModuloNumber,

    // ── String ─────────────────────────────────────────────────────
    StringPlusString,

    // ── Temporal: date ──────────────────────────────────────────────
    DatePlusPeriod,
    DateMinusPeriod,
    DateMinusDate,
    DatePlusTime,

    // ── Temporal: time ──────────────────────────────────────────────
    TimePlusPeriod,
    TimeMinusPeriod,
    TimePlusDuration,
    TimeMinusDuration,
    TimeMinusTime,

    // ── Temporal: instant ───────────────────────────────────────────
    InstantMinusInstant,
    InstantPlusDuration,
    InstantMinusDuration,

    // ── Temporal: duration ──────────────────────────────────────────
    DurationPlusDuration,
    DurationMinusDuration,
    DurationTimesInteger,
    DurationTimesNumber,
    DurationDivideInteger,
    DurationDivideNumber,
    DurationDivideDuration,

    // ── Temporal: period ────────────────────────────────────────────
    PeriodPlusPeriod,
    PeriodMinusPeriod,

    // ── Temporal: zoneddatetime ─────────────────────────────────────
    ZonedDateTimePlusDuration,
    ZonedDateTimeMinusDuration,
    ZonedDateTimePlusPeriod,
    ZonedDateTimeMinusPeriod,
    ZonedDateTimeMinusZonedDateTime,

    // ── Temporal: datetime ──────────────────────────────────────────
    DateTimePlusPeriod,
    DateTimeMinusPeriod,
    DateTimeMinusDateTime,

    // ── Business: money ─────────────────────────────────────────────
    MoneyPlusMoney,
    MoneyMinusMoney,
    MoneyTimesDecimal,
    MoneyDivideDecimal,
    MoneyDivideMoneySameCurrency,
    MoneyDivideMoneyCrossCurrency,
    MoneyDivideQuantity,
    MoneyDividePeriod,
    MoneyDivideDuration,

    // ── Business: quantity ──────────────────────────────────────────
    QuantityPlusQuantity,
    QuantityMinusQuantity,
    QuantityTimesDecimal,
    QuantityDivideDecimal,
    QuantityDivideQuantitySameDimension,
    QuantityDivideQuantityCrossDimension,
    QuantityDividePeriod,
    QuantityDivideDuration,
    QuantityTimesQuantity,
    QuantityTimesPeriod,
    QuantityTimesDuration,

    // ── Business: price ─────────────────────────────────────────────
    PricePlusPrice,
    PriceMinusPrice,
    PriceTimesQuantity,
    PriceTimesPeriod,
    PriceTimesDuration,
    PriceTimesDecimal,
    PriceDivideDecimal,

    // ── Business: exchangerate ──────────────────────────────────────
    ExchangeRateTimesMoney,
    ExchangeRateTimesDecimal,
    ExchangeRateDivideDecimal,

    // ════════════════════════════════════════════════════════════════
    //  Comparison operations (result is always Boolean)
    // ════════════════════════════════════════════════════════════════

    // ── Comparison: equality-only types ─────────────────────────────
    BooleanEqualsBoolean,
    BooleanNotEqualsBoolean,

    PeriodEqualsPeriod,
    PeriodNotEqualsPeriod,

    TimezoneEqualsTimezone,
    TimezoneNotEqualsTimezone,

    ZonedDateTimeEqualsZonedDateTime,
    ZonedDateTimeNotEqualsZonedDateTime,

    CurrencyEqualsCurrency,
    CurrencyNotEqualsCurrency,

    UnitOfMeasureEqualsUnitOfMeasure,
    UnitOfMeasureNotEqualsUnitOfMeasure,

    DimensionEqualsDimension,
    DimensionNotEqualsDimension,

    ExchangeRateEqualsExchangeRate,
    ExchangeRateNotEqualsExchangeRate,

    // ── Comparison: orderable same-type ─────────────────────────────
    IntegerEqualsInteger,
    IntegerNotEqualsInteger,
    IntegerLessThanInteger,
    IntegerGreaterThanInteger,
    IntegerLessThanOrEqualInteger,
    IntegerGreaterThanOrEqualInteger,

    DecimalEqualsDecimal,
    DecimalNotEqualsDecimal,
    DecimalLessThanDecimal,
    DecimalGreaterThanDecimal,
    DecimalLessThanOrEqualDecimal,
    DecimalGreaterThanOrEqualDecimal,

    NumberEqualsNumber,
    NumberNotEqualsNumber,
    NumberLessThanNumber,
    NumberGreaterThanNumber,
    NumberLessThanOrEqualNumber,
    NumberGreaterThanOrEqualNumber,

    StringEqualsString,
    StringNotEqualsString,

    ChoiceEqualsChoice,
    ChoiceNotEqualsChoice,
    ChoiceLessThanChoice,
    ChoiceGreaterThanChoice,
    ChoiceLessThanOrEqualChoice,
    ChoiceGreaterThanOrEqualChoice,

    DateEqualsDate,
    DateNotEqualsDate,
    DateLessThanDate,
    DateGreaterThanDate,
    DateLessThanOrEqualDate,
    DateGreaterThanOrEqualDate,

    TimeEqualsTime,
    TimeNotEqualsTime,
    TimeLessThanTime,
    TimeGreaterThanTime,
    TimeLessThanOrEqualTime,
    TimeGreaterThanOrEqualTime,

    InstantEqualsInstant,
    InstantNotEqualsInstant,
    InstantLessThanInstant,
    InstantGreaterThanInstant,
    InstantLessThanOrEqualInstant,
    InstantGreaterThanOrEqualInstant,

    DurationEqualsDuration,
    DurationNotEqualsDuration,
    DurationLessThanDuration,
    DurationGreaterThanDuration,
    DurationLessThanOrEqualDuration,
    DurationGreaterThanOrEqualDuration,

    DateTimeEqualsDateTime,
    DateTimeNotEqualsDateTime,
    DateTimeLessThanDateTime,
    DateTimeGreaterThanDateTime,
    DateTimeLessThanOrEqualDateTime,
    DateTimeGreaterThanOrEqualDateTime,

    MoneyEqualsMoney,
    MoneyNotEqualsMoney,
    MoneyLessThanMoney,
    MoneyGreaterThanMoney,
    MoneyLessThanOrEqualMoney,
    MoneyGreaterThanOrEqualMoney,

    QuantityEqualsQuantity,
    QuantityNotEqualsQuantity,
    QuantityLessThanQuantity,
    QuantityGreaterThanQuantity,
    QuantityLessThanOrEqualQuantity,
    QuantityGreaterThanOrEqualQuantity,

    PriceEqualsPrice,
    PriceNotEqualsPrice,
    PriceLessThanPrice,
    PriceGreaterThanPrice,
    PriceLessThanOrEqualPrice,
    PriceGreaterThanOrEqualPrice,

    // ── Comparison: widening (integer ↔ decimal) ────────────────────
    IntegerEqualsDecimal,
    IntegerNotEqualsDecimal,
    IntegerLessThanDecimal,
    IntegerGreaterThanDecimal,
    IntegerLessThanOrEqualDecimal,
    IntegerGreaterThanOrEqualDecimal,

    // ── Comparison: widening (integer ↔ number) ─────────────────────
    IntegerEqualsNumber,
    IntegerNotEqualsNumber,
    IntegerLessThanNumber,
    IntegerGreaterThanNumber,
    IntegerLessThanOrEqualNumber,
    IntegerGreaterThanOrEqualNumber,

    // ── Comparison: case-insensitive (string only) ──────────────────
    StringCaseInsensitiveEqualsString,
    StringCaseInsensitiveNotEqualsString,
}
