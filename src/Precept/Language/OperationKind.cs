namespace Precept.Language;

/// <summary>
/// Every legal typed operator combination. Each member is one
/// (operator, operand type(s)) → result type triple.
/// Metadata lives in <see cref="Operations.GetMeta"/>, not on the enum.
/// </summary>
public enum OperationKind
{
    // ── Unary ──────────────────────────────────────────────────────
    NegateInteger                            =   1,
    NegateDecimal                            =   2,
    NegateNumber                             =   3,
    NegateMoney                              =   4,
    NegateQuantity                           =   5,
    NegatePrice                              =   6,
    NegateDuration                           =   7,
    NegatePeriod                             =   8,
    NotBoolean                               =   9,

    // ── Scalar: same-type arithmetic ───────────────────────────────
    IntegerPlusInteger                       =  10,
    IntegerMinusInteger                      =  11,
    IntegerTimesInteger                      =  12,
    IntegerDivideInteger                     =  13,
    IntegerModuloInteger                     =  14,

    DecimalPlusDecimal                       =  15,
    DecimalMinusDecimal                      =  16,
    DecimalTimesDecimal                      =  17,
    DecimalDivideDecimal                     =  18,
    DecimalModuloDecimal                     =  19,

    NumberPlusNumber                         =  20,
    NumberMinusNumber                        =  21,
    NumberTimesNumber                        =  22,
    NumberDivideNumber                       =  23,
    NumberModuloNumber                       =  24,

    // ── Scalar: widening (integer ↔ decimal) ───────────────────────
    IntegerPlusDecimal                       =  25,
    IntegerMinusDecimal                      =  26,
    IntegerTimesDecimal                      =  27,
    IntegerDivideDecimal                     =  28,
    IntegerModuloDecimal                     =  29,

    // ── Scalar: widening (integer ↔ number) ────────────────────────
    IntegerPlusNumber                        =  30,
    IntegerMinusNumber                       =  31,
    IntegerTimesNumber                       =  32,
    IntegerDivideNumber                      =  33,
    IntegerModuloNumber                      =  34,

    // ── String ─────────────────────────────────────────────────────
    StringPlusString                         =  35,

    // ── Temporal: date ──────────────────────────────────────────────
    DatePlusPeriod                           =  36,
    DateMinusPeriod                          =  37,
    DateMinusDate                            =  38,
    DatePlusTime                             =  39,

    // ── Temporal: time ──────────────────────────────────────────────
    TimePlusPeriod                           =  40,
    TimeMinusPeriod                          =  41,
    TimePlusDuration                         =  42,
    TimeMinusDuration                        =  43,
    TimeMinusTime                            =  44,

    // ── Temporal: instant ───────────────────────────────────────────
    InstantMinusInstant                      =  45,
    InstantPlusDuration                      =  46,
    InstantMinusDuration                     =  47,

    // ── Temporal: duration ──────────────────────────────────────────
    DurationPlusDuration                     =  48,
    DurationMinusDuration                    =  49,
    DurationTimesInteger                     =  50,
    DurationTimesNumber                      =  51,
    DurationDivideInteger                    =  52,
    DurationDivideNumber                     =  53,
    DurationDivideDuration                   =  54,

    // ── Temporal: period ────────────────────────────────────────────
    PeriodPlusPeriod                         =  55,
    PeriodMinusPeriod                        =  56,

    // ── Temporal: zoneddatetime ─────────────────────────────────────
    ZonedDateTimePlusDuration                =  57,
    ZonedDateTimeMinusDuration               =  58,
    ZonedDateTimePlusPeriod                  =  59,
    ZonedDateTimeMinusPeriod                 =  60,
    ZonedDateTimeMinusZonedDateTime          =  61,

    // ── Temporal: datetime ──────────────────────────────────────────
    DateTimePlusPeriod                       =  62,
    DateTimeMinusPeriod                      =  63,
    DateTimeMinusDateTime                    =  64,

    // ── Business: money ─────────────────────────────────────────────
    MoneyPlusMoney                           =  65,
    MoneyMinusMoney                          =  66,
    MoneyTimesDecimal                        =  67,
    MoneyDivideDecimal                       =  68,
    MoneyDivideMoneySameCurrency             =  69,
    MoneyDivideMoneyCrossCurrency            =  70,
    MoneyDivideQuantity                      =  71,
    MoneyDividePeriod                        =  72,
    MoneyDivideDuration                      =  73,

    // ── Business: quantity ──────────────────────────────────────────
    QuantityPlusQuantity                     =  74,
    QuantityMinusQuantity                    =  75,
    QuantityTimesDecimal                     =  76,
    QuantityDivideDecimal                    =  77,
    QuantityDivideQuantitySameDimension      =  78,
    QuantityDivideQuantityCrossDimension     =  79,
    QuantityDividePeriod                     =  80,
    QuantityDivideDuration                   =  81,
    QuantityTimesQuantity                    =  82,
    QuantityTimesPeriod                      =  83,
    QuantityTimesDuration                    =  84,

    // ── Business: price ─────────────────────────────────────────────
    PricePlusPrice                           =  85,
    PriceMinusPrice                          =  86,
    PriceTimesQuantity                       =  87,
    PriceTimesPeriod                         =  88,
    PriceTimesDuration                       =  89,
    PriceTimesDecimal                        =  90,
    PriceDivideDecimal                       =  91,

    // ── Business: exchangerate ──────────────────────────────────────
    ExchangeRateTimesMoney                   =  92,
    ExchangeRateTimesDecimal                 =  93,
    ExchangeRateDivideDecimal                =  94,

    // ════════════════════════════════════════════════════════════════
    //  Comparison operations (result is always Boolean)
    // ════════════════════════════════════════════════════════════════

    // ── Comparison: equality-only types ─────────────────────────────
    BooleanEqualsBoolean                     =  95,
    BooleanNotEqualsBoolean                  =  96,

    PeriodEqualsPeriod                       =  97,
    PeriodNotEqualsPeriod                    =  98,

    TimezoneEqualsTimezone                   =  99,
    TimezoneNotEqualsTimezone                = 100,

    ZonedDateTimeEqualsZonedDateTime         = 101,
    ZonedDateTimeNotEqualsZonedDateTime      = 102,

    CurrencyEqualsCurrency                   = 103,
    CurrencyNotEqualsCurrency                = 104,

    UnitOfMeasureEqualsUnitOfMeasure         = 105,
    UnitOfMeasureNotEqualsUnitOfMeasure      = 106,

    DimensionEqualsDimension                 = 107,
    DimensionNotEqualsDimension              = 108,

    ExchangeRateEqualsExchangeRate           = 109,
    ExchangeRateNotEqualsExchangeRate        = 110,

    // ── Comparison: orderable same-type ─────────────────────────────
    IntegerEqualsInteger                     = 111,
    IntegerNotEqualsInteger                  = 112,
    IntegerLessThanInteger                   = 113,
    IntegerGreaterThanInteger                = 114,
    IntegerLessThanOrEqualInteger            = 115,
    IntegerGreaterThanOrEqualInteger         = 116,

    DecimalEqualsDecimal                     = 117,
    DecimalNotEqualsDecimal                  = 118,
    DecimalLessThanDecimal                   = 119,
    DecimalGreaterThanDecimal                = 120,
    DecimalLessThanOrEqualDecimal            = 121,
    DecimalGreaterThanOrEqualDecimal         = 122,

    NumberEqualsNumber                       = 123,
    NumberNotEqualsNumber                    = 124,
    NumberLessThanNumber                     = 125,
    NumberGreaterThanNumber                  = 126,
    NumberLessThanOrEqualNumber              = 127,
    NumberGreaterThanOrEqualNumber           = 128,

    StringEqualsString                       = 129,
    StringNotEqualsString                    = 130,

    ChoiceEqualsChoice                       = 131,
    ChoiceNotEqualsChoice                    = 132,
    ChoiceLessThanChoice                     = 133,
    ChoiceGreaterThanChoice                  = 134,
    ChoiceLessThanOrEqualChoice              = 135,
    ChoiceGreaterThanOrEqualChoice           = 136,

    DateEqualsDate                           = 137,
    DateNotEqualsDate                        = 138,
    DateLessThanDate                         = 139,
    DateGreaterThanDate                      = 140,
    DateLessThanOrEqualDate                  = 141,
    DateGreaterThanOrEqualDate               = 142,

    TimeEqualsTime                           = 143,
    TimeNotEqualsTime                        = 144,
    TimeLessThanTime                         = 145,
    TimeGreaterThanTime                      = 146,
    TimeLessThanOrEqualTime                  = 147,
    TimeGreaterThanOrEqualTime               = 148,

    InstantEqualsInstant                     = 149,
    InstantNotEqualsInstant                  = 150,
    InstantLessThanInstant                   = 151,
    InstantGreaterThanInstant                = 152,
    InstantLessThanOrEqualInstant            = 153,
    InstantGreaterThanOrEqualInstant         = 154,

    DurationEqualsDuration                   = 155,
    DurationNotEqualsDuration                = 156,
    DurationLessThanDuration                 = 157,
    DurationGreaterThanDuration              = 158,
    DurationLessThanOrEqualDuration          = 159,
    DurationGreaterThanOrEqualDuration       = 160,

    DateTimeEqualsDateTime                   = 161,
    DateTimeNotEqualsDateTime                = 162,
    DateTimeLessThanDateTime                 = 163,
    DateTimeGreaterThanDateTime              = 164,
    DateTimeLessThanOrEqualDateTime          = 165,
    DateTimeGreaterThanOrEqualDateTime       = 166,

    MoneyEqualsMoney                         = 167,
    MoneyNotEqualsMoney                      = 168,
    MoneyLessThanMoney                       = 169,
    MoneyGreaterThanMoney                    = 170,
    MoneyLessThanOrEqualMoney                = 171,
    MoneyGreaterThanOrEqualMoney             = 172,

    QuantityEqualsQuantity                   = 173,
    QuantityNotEqualsQuantity                = 174,
    QuantityLessThanQuantity                 = 175,
    QuantityGreaterThanQuantity              = 176,
    QuantityLessThanOrEqualQuantity          = 177,
    QuantityGreaterThanOrEqualQuantity       = 178,

    PriceEqualsPrice                         = 179,
    PriceNotEqualsPrice                      = 180,
    PriceLessThanPrice                       = 181,
    PriceGreaterThanPrice                    = 182,
    PriceLessThanOrEqualPrice                = 183,
    PriceGreaterThanOrEqualPrice             = 184,

    // ── Comparison: widening (integer ↔ decimal) ────────────────────
    IntegerEqualsDecimal                     = 185,
    IntegerNotEqualsDecimal                  = 186,
    IntegerLessThanDecimal                   = 187,
    IntegerGreaterThanDecimal                = 188,
    IntegerLessThanOrEqualDecimal            = 189,
    IntegerGreaterThanOrEqualDecimal         = 190,

    // ── Comparison: widening (integer ↔ number) ─────────────────────
    IntegerEqualsNumber                      = 191,
    IntegerNotEqualsNumber                   = 192,
    IntegerLessThanNumber                    = 193,
    IntegerGreaterThanNumber                 = 194,
    IntegerLessThanOrEqualNumber             = 195,
    IntegerGreaterThanOrEqualNumber          = 196,

    // ── Comparison: case-insensitive (string only) ──────────────────
    StringCaseInsensitiveEqualsString        = 197,
    StringCaseInsensitiveNotEqualsString     = 198,
}
