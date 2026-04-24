namespace Precept.Pipeline;

/// <summary>
/// Every token the lexer can produce. Categories match the spec §1.1 tables.
/// </summary>
public enum TokenKind
{
    // ── Keywords: Declaration ───────────────────────────────────────
    Precept,
    Field,
    State,
    Event,
    Rule,
    Ensure,
    As,
    Default,
    Optional,
    Because,
    Initial,

    // ── Keywords: Prepositions ──────────────────────────────────────
    In,
    To,
    From,
    On,
    Of,
    Into,

    // ── Keywords: Control ──────────────────────────────────────────
    When,
    If,
    Then,
    Else,

    // ── Keywords: Actions ──────────────────────────────────────────
    Set,
    Add,
    Remove,
    Enqueue,
    Dequeue,
    Push,
    Pop,
    Clear,

    // ── Keywords: Outcomes ─────────────────────────────────────────
    Transition,
    No,
    Reject,

    // ── Keywords: Access Modes (v2) ────────────────────────────────
    Write,
    Read,
    Omit,

    // ── Keywords: Logical Operators ────────────────────────────────
    And,
    Or,
    Not,

    // ── Keywords: Membership ───────────────────────────────────────
    Contains,
    Is,

    // ── Keywords: Quantifiers / Modifiers ──────────────────────────
    All,
    Any,

    // ── Keywords: State Modifiers (v2) ─────────────────────────────
    Terminal,
    Required,
    Irreversible,
    Success,
    Warning,
    Error,

    // ── Keywords: Constraints ──────────────────────────────────────
    Nonnegative,
    Positive,
    Nonzero,
    Notempty,
    Min,
    Max,
    Minlength,
    Maxlength,
    Mincount,
    Maxcount,
    Maxplaces,
    Ordered,

    // ── Keywords: Types (Primitive / Collection) ───────────────────
    StringType,
    BooleanType,
    IntegerType,
    DecimalType,
    NumberType,
    ChoiceType,
    /// <summary>
    /// Parser-synthesized only. The lexer always emits <see cref="Set"/> for the word "set".
    /// The parser reinterprets it as <c>SetType</c> when the preceding token is <see cref="As"/> or <see cref="Of"/>.
    /// </summary>
    SetType,
    QueueType,
    StackType,

    // ── Keywords: Temporal Types (v2) ──────────────────────────────
    DateType,
    TimeType,
    InstantType,
    DurationType,
    PeriodType,
    TimezoneType,
    ZonedDateTimeType,
    DateTimeType,

    // ── Keywords: Business-Domain Types (v2) ───────────────────────
    MoneyType,
    CurrencyType,
    QuantityType,
    UnitOfMeasureType,
    DimensionType,
    PriceType,
    ExchangeRateType,

    // ── Keywords: Literals ─────────────────────────────────────────
    True,
    False,

    // ── Operators ──────────────────────────────────────────────────
    DoubleEquals,
    NotEquals,
    CaseInsensitiveEquals,
    CaseInsensitiveNotEquals,
    GreaterThanOrEqual,
    LessThanOrEqual,
    GreaterThan,
    LessThan,
    Assign,
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    Arrow,

    // ── Punctuation ────────────────────────────────────────────────
    Dot,
    Comma,
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,

    // ── Literals ───────────────────────────────────────────────────
    NumberLiteral,
    StringLiteral,
    StringStart,
    StringMiddle,
    StringEnd,
    TypedConstant,
    TypedConstantStart,
    TypedConstantMiddle,
    TypedConstantEnd,

    // ── Identifiers ────────────────────────────────────────────────
    Identifier,

    // ── Structural ─────────────────────────────────────────────────
    EndOfSource,
    NewLine,
    Comment,
}
