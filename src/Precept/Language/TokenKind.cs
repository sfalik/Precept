namespace Precept.Language;

/// <summary>
/// Every token the lexer can produce. Categories match the spec §1.1 tables.
/// </summary>
public enum TokenKind
{
    // ── Keywords: Declaration ───────────────────────────────────────
    Precept     =   1,
    Field       =   2,
    State       =   3,
    Event       =   4,
    Rule        =   5,
    Ensure      =   6,
    As          =   7,
    Default     =   8,
    Optional    =   9,
    Writable    =  10,
    Because     =  11,
    Initial     =  12,

    // ── Keywords: Prepositions ──────────────────────────────────────
    In          =  13,
    To          =  14,
    From        =  15,
    On          =  16,
    Of          =  17,
    Into        =  18,

    // ── Keywords: Control ──────────────────────────────────────────
    When        =  19,
    If          =  20,
    Then        =  21,
    Else        =  22,

    // ── Keywords: Actions ──────────────────────────────────────────
    Set         =  23,
    Add         =  24,
    Remove      =  25,
    Enqueue     =  26,
    Dequeue     =  27,
    Push        =  28,
    Pop         =  29,
    Clear       =  30,

    // ── Keywords: Outcomes ─────────────────────────────────────────
    Transition  =  31,
    No          =  32,
    Reject      =  33,

    // ── Keywords: Access Modes (B4 — 2026-04-28) ──────────────────
    // Write and Read retired: vocabulary locked B4 (2026-04-28).
    // New surface: in State modify Field readonly|editable [when Guard]
    //              in State omit Field
    Modify      =  34,
    Readonly    =  35,
    Editable    =  36,
    Omit        =  37,

    // ── Keywords: Logical Operators ────────────────────────────────
    And         =  38,
    Or          =  39,
    Not         =  40,

    // ── Keywords: Membership ───────────────────────────────────────
    Contains    =  41,
    Is          =  42,

    // ── Keywords: Quantifiers / Modifiers ──────────────────────────
    All         =  43,
    Any         =  44,

    // ── Keywords: State Modifiers (v2) ─────────────────────────────
    Terminal    =  45,
    Required    =  46,
    Irreversible =  47,
    Success     =  48,
    Warning     =  49,
    Error       =  50,

    // ── Keywords: Constraints ──────────────────────────────────────
    Nonnegative =  51,
    Positive    =  52,
    Nonzero     =  53,
    Notempty    =  54,
    Min         =  55,
    Max         =  56,
    Minlength   =  57,
    Maxlength   =  58,
    Mincount    =  59,
    Maxcount    =  60,
    Maxplaces   =  61,
    Ordered     =  62,

    // ── Keywords: Types (Primitive / Collection) ───────────────────
    StringType  =  63,
    BooleanType =  64,
    IntegerType =  65,
    DecimalType =  66,
    NumberType  =  67,
    ChoiceType  =  68,
    /// <summary>
    /// Parser-synthesized only. The lexer always emits <see cref="Set"/> for the word "set".
    /// The parser reinterprets it as <c>SetType</c> when the preceding token is <see cref="As"/> or <see cref="Of"/>.
    /// </summary>
    SetType     =  69,
    QueueType   =  70,
    StackType   =  71,

    // ── Keywords: Temporal Types (v2) ──────────────────────────────
    DateType    =  72,
    TimeType    =  73,
    InstantType =  74,
    DurationType =  75,
    PeriodType  =  76,
    TimezoneType =  77,
    ZonedDateTimeType =  78,
    DateTimeType =  79,

    // ── Keywords: Business-Domain Types (v2) ───────────────────────
    MoneyType        =  80,
    CurrencyType     =  81,
    QuantityType     =  82,
    UnitOfMeasureType =  83,
    DimensionType    =  84,
    PriceType        =  85,
    ExchangeRateType =  86,

    // ── Keywords: Literals ─────────────────────────────────────────
    True        =  87,
    False       =  88,

    // ── Operators ──────────────────────────────────────────────────
    DoubleEquals              =  89,
    NotEquals                 =  90,
    CaseInsensitiveEquals     =  91,
    CaseInsensitiveNotEquals  =  92,
    Tilde                     =  93, // ~ prefix on string in collection inner type position
    GreaterThanOrEqual        =  94,
    LessThanOrEqual           =  95,
    GreaterThan               =  96,
    LessThan                  =  97,
    Assign                    =  98,
    Plus                      =  99,
    Minus                     = 100,
    Star                      = 101,
    Slash                     = 102,
    Percent                   = 103,
    Arrow                     = 104,

    // ── Punctuation ────────────────────────────────────────────────
    Dot          = 105,
    Comma        = 106,
    LeftParen    = 107,
    RightParen   = 108,
    LeftBracket  = 109,
    RightBracket = 110,

    // ── Literals ───────────────────────────────────────────────────
    NumberLiteral       = 111,
    StringLiteral       = 112,
    StringStart         = 113,
    StringMiddle        = 114,
    StringEnd           = 115,
    TypedConstant       = 116,
    TypedConstantStart  = 117,
    TypedConstantMiddle = 118,
    TypedConstantEnd    = 119,

    // ── Identifiers ────────────────────────────────────────────────
    Identifier  = 120,

    // ── Structural ─────────────────────────────────────────────────
    EndOfSource = 121,
    NewLine     = 122,
    Comment     = 123,
}
