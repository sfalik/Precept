using System.Collections.Frozen;

namespace Precept.Pipeline;

public static class Tokens
{
    private static readonly TokenCategory[] Cat_Decl = [TokenCategory.Declaration];
    private static readonly TokenCategory[] Cat_Prep = [TokenCategory.Preposition];
    private static readonly TokenCategory[] Cat_Ctrl = [TokenCategory.Control];
    private static readonly TokenCategory[] Cat_Act  = [TokenCategory.Action];
    private static readonly TokenCategory[] Cat_Out  = [TokenCategory.Outcome];
    private static readonly TokenCategory[] Cat_Acc  = [TokenCategory.AccessMode];
    private static readonly TokenCategory[] Cat_Log  = [TokenCategory.LogicalOperator];
    private static readonly TokenCategory[] Cat_Mem  = [TokenCategory.Membership];
    private static readonly TokenCategory[] Cat_Qnt  = [TokenCategory.Quantifier];
    private static readonly TokenCategory[] Cat_SMod = [TokenCategory.StateModifier];
    private static readonly TokenCategory[] Cat_Cns  = [TokenCategory.Constraint];
    private static readonly TokenCategory[] Cat_Type = [TokenCategory.Type];
    private static readonly TokenCategory[] Cat_Lit  = [TokenCategory.Literal];
    private static readonly TokenCategory[] Cat_Op   = [TokenCategory.Operator];
    private static readonly TokenCategory[] Cat_Pun  = [TokenCategory.Punctuation];
    private static readonly TokenCategory[] Cat_Id   = [TokenCategory.Identifier];
    private static readonly TokenCategory[] Cat_Str  = [TokenCategory.Structural];

    // Dual-use tokens
    private static readonly TokenCategory[] Cat_ActType = [TokenCategory.Action, TokenCategory.Type];
    private static readonly TokenCategory[] Cat_CnsCtrl = [TokenCategory.Constraint, TokenCategory.Control];

    public static TokenMeta GetMeta(TokenKind kind) => kind switch
    {
        // ── Keywords: Declaration ───────────────────────────────────
        TokenKind.Precept     => new(kind, "precept",     Cat_Decl, "Precept header declaration"),
        TokenKind.Field       => new(kind, "field",       Cat_Decl, "Field declaration"),
        TokenKind.State       => new(kind, "state",       Cat_Decl, "State declaration"),
        TokenKind.Event       => new(kind, "event",       Cat_Decl, "Event declaration"),
        TokenKind.Rule        => new(kind, "rule",        Cat_Decl, "Named rule / invariant declaration"),
        TokenKind.Ensure      => new(kind, "ensure",      Cat_Decl, "State/event assertion keyword"),
        TokenKind.As          => new(kind, "as",          Cat_Decl, "Type annotation"),
        TokenKind.Default     => new(kind, "default",     Cat_Decl, "Default value modifier"),
        TokenKind.Optional    => new(kind, "optional",    Cat_Decl, "Field optionality modifier"),
        TokenKind.Because     => new(kind, "because",     Cat_Decl, "Reason clause"),
        TokenKind.Initial     => new(kind, "initial",     Cat_Decl, "Initial state/event marker"),

        // ── Keywords: Prepositions ─────────────────────────────────
        TokenKind.In          => new(kind, "in",          Cat_Prep, "State scope / type qualifier"),
        TokenKind.To          => new(kind, "to",          Cat_Prep, "Entry-gate ensure / transition target"),
        TokenKind.From        => new(kind, "from",        Cat_Prep, "Exit-gate ensure / transition source"),
        TokenKind.On          => new(kind, "on",          Cat_Prep, "Event trigger"),
        TokenKind.Of          => new(kind, "of",          Cat_Prep, "Collection inner type / dimension family qualifier"),
        TokenKind.Into        => new(kind, "into",        Cat_Prep, "Dequeue/pop target"),

        // ── Keywords: Control ──────────────────────────────────────
        TokenKind.When        => new(kind, "when",        Cat_Ctrl, "Guard clause"),
        TokenKind.If          => new(kind, "if",          Cat_Ctrl, "Conditional expression"),
        TokenKind.Then        => new(kind, "then",        Cat_Ctrl, "Conditional expression"),
        TokenKind.Else        => new(kind, "else",        Cat_Ctrl, "Conditional expression"),

        // ── Keywords: Actions ──────────────────────────────────────
        TokenKind.Set         => new(kind, "set",         Cat_ActType, "Field assignment / set collection type"),
        TokenKind.Add         => new(kind, "add",         Cat_Act,     "Set add action"),
        TokenKind.Remove      => new(kind, "remove",      Cat_Act,     "Set remove action"),
        TokenKind.Enqueue     => new(kind, "enqueue",     Cat_Act,     "Queue enqueue action"),
        TokenKind.Dequeue     => new(kind, "dequeue",     Cat_Act,     "Queue dequeue action"),
        TokenKind.Push        => new(kind, "push",        Cat_Act,     "Stack push action"),
        TokenKind.Pop         => new(kind, "pop",         Cat_Act,     "Stack pop action"),
        TokenKind.Clear       => new(kind, "clear",       Cat_Act,     "Collection clear / optional field clear"),

        // ── Keywords: Outcomes ─────────────────────────────────────
        TokenKind.Transition  => new(kind, "transition",  Cat_Out, "State transition outcome"),
        TokenKind.No          => new(kind, "no",          Cat_Out, "Prefix for 'no transition'"),
        TokenKind.Reject      => new(kind, "reject",      Cat_Out, "Rejection outcome"),

        // ── Keywords: Access Modes ─────────────────────────────────
        TokenKind.Write       => new(kind, "write",       Cat_Acc, "Field write access mode"),
        TokenKind.Read        => new(kind, "read",        Cat_Acc, "Field read access mode"),
        TokenKind.Omit        => new(kind, "omit",        Cat_Acc, "Field omit access mode"),

        // ── Keywords: Logical Operators ────────────────────────────
        TokenKind.And         => new(kind, "and",         Cat_Log, "Logical conjunction"),
        TokenKind.Or          => new(kind, "or",          Cat_Log, "Logical disjunction"),
        TokenKind.Not         => new(kind, "not",         Cat_Log, "Logical negation"),

        // ── Keywords: Membership ───────────────────────────────────
        TokenKind.Contains    => new(kind, "contains",    Cat_Mem, "Collection membership test"),
        TokenKind.Is          => new(kind, "is",          Cat_Mem, "Multi-token operator prefix (is set, is not set)"),

        // ── Keywords: Quantifiers / Modifiers ──────────────────────
        TokenKind.All         => new(kind, "all",         Cat_Qnt, "Universal quantifier"),
        TokenKind.Any         => new(kind, "any",         Cat_Qnt, "State wildcard (in any, from any)"),

        // ── Keywords: State Modifiers ──────────────────────────────
        TokenKind.Terminal    => new(kind, "terminal",    Cat_SMod, "Structural: no outgoing transitions"),
        TokenKind.Required    => new(kind, "required",    Cat_SMod, "Structural: all initial→terminal paths visit this state"),
        TokenKind.Irreversible=> new(kind, "irreversible",Cat_SMod, "Structural: no path back to any ancestor state"),
        TokenKind.Success     => new(kind, "success",     Cat_SMod, "Semantic: success outcome state"),
        TokenKind.Warning     => new(kind, "warning",     Cat_SMod, "Semantic: warning outcome state"),
        TokenKind.Error       => new(kind, "error",       Cat_SMod, "Semantic: error outcome state"),

        // ── Keywords: Constraints ──────────────────────────────────
        TokenKind.Nonnegative => new(kind, "nonnegative", Cat_Cns, "Number/integer constraint: value >= 0"),
        TokenKind.Positive    => new(kind, "positive",    Cat_Cns, "Number/integer constraint: value > 0"),
        TokenKind.Nonzero     => new(kind, "nonzero",     Cat_Cns, "Number/integer constraint: value != 0"),
        TokenKind.Notempty    => new(kind, "notempty",     Cat_Cns, "String constraint: non-empty"),
        TokenKind.Min         => new(kind, "min",         Cat_CnsCtrl, "Numeric minimum constraint / built-in function"),
        TokenKind.Max         => new(kind, "max",         Cat_CnsCtrl, "Numeric maximum constraint / built-in function"),
        TokenKind.Minlength   => new(kind, "minlength",   Cat_Cns, "String minimum length constraint"),
        TokenKind.Maxlength   => new(kind, "maxlength",   Cat_Cns, "String maximum length constraint"),
        TokenKind.Mincount    => new(kind, "mincount",    Cat_Cns, "Collection minimum count constraint"),
        TokenKind.Maxcount    => new(kind, "maxcount",    Cat_Cns, "Collection maximum count constraint"),
        TokenKind.Maxplaces   => new(kind, "maxplaces",   Cat_Cns, "Decimal maximum decimal places constraint"),
        TokenKind.Ordered     => new(kind, "ordered",     Cat_Cns, "Choice ordinal comparison constraint"),

        // ── Keywords: Types (Primitive / Collection) ───────────────
        TokenKind.StringType  => new(kind, "string",      Cat_Type, "Scalar type"),
        TokenKind.BooleanType => new(kind, "boolean",     Cat_Type, "Scalar type"),
        TokenKind.IntegerType => new(kind, "integer",     Cat_Type, "Scalar type: explicit integer"),
        TokenKind.DecimalType => new(kind, "decimal",     Cat_Type, "Scalar type: exact base-10"),
        TokenKind.NumberType  => new(kind, "number",      Cat_Type, "Scalar type: general numeric"),
        TokenKind.ChoiceType  => new(kind, "choice",      Cat_Type, "Constrained string value set type"),
        TokenKind.SetType     => new(kind, "set",         Cat_Type, "Set collection type"),
        TokenKind.QueueType   => new(kind, "queue",       Cat_Type, "Queue collection type"),
        TokenKind.StackType   => new(kind, "stack",       Cat_Type, "Stack collection type"),

        // ── Keywords: Temporal Types ───────────────────────────────
        TokenKind.DateType          => new(kind, "date",          Cat_Type, "Temporal: calendar date"),
        TokenKind.TimeType          => new(kind, "time",          Cat_Type, "Temporal: time of day"),
        TokenKind.InstantType       => new(kind, "instant",       Cat_Type, "Temporal: UTC point in time"),
        TokenKind.DurationType      => new(kind, "duration",      Cat_Type, "Temporal: elapsed time quantity"),
        TokenKind.PeriodType        => new(kind, "period",        Cat_Type, "Temporal: calendar quantity"),
        TokenKind.TimezoneType      => new(kind, "timezone",      Cat_Type, "Temporal: timezone identity"),
        TokenKind.ZonedDateTimeType => new(kind, "zoneddatetime", Cat_Type, "Temporal: date+time+timezone"),
        TokenKind.DateTimeType      => new(kind, "datetime",      Cat_Type, "Temporal: local date+time"),

        // ── Keywords: Business-Domain Types ────────────────────────
        TokenKind.MoneyType         => new(kind, "money",         Cat_Type, "Business: monetary amount"),
        TokenKind.CurrencyType      => new(kind, "currency",      Cat_Type, "Business: currency identity"),
        TokenKind.QuantityType      => new(kind, "quantity",       Cat_Type, "Business: measured quantity"),
        TokenKind.UnitOfMeasureType => new(kind, "unitofmeasure", Cat_Type, "Business: unit identity"),
        TokenKind.DimensionType     => new(kind, "dimension",     Cat_Type, "Business: dimension family identity"),
        TokenKind.PriceType         => new(kind, "price",         Cat_Type, "Business: compound money/quantity rate"),
        TokenKind.ExchangeRateType  => new(kind, "exchangerate",  Cat_Type, "Business: compound currency/currency rate"),

        // ── Keywords: Literals ─────────────────────────────────────
        TokenKind.True        => new(kind, "true",        Cat_Lit, "Boolean literal"),
        TokenKind.False       => new(kind, "false",       Cat_Lit, "Boolean literal"),

        // ── Operators ──────────────────────────────────────────────
        TokenKind.DoubleEquals        => new(kind, "==", Cat_Op, "Equality comparison"),
        TokenKind.NotEquals           => new(kind, "!=", Cat_Op, "Inequality comparison"),
        TokenKind.GreaterThanOrEqual  => new(kind, ">=", Cat_Op, "Greater-than-or-equal comparison"),
        TokenKind.LessThanOrEqual     => new(kind, "<=", Cat_Op, "Less-than-or-equal comparison"),
        TokenKind.GreaterThan         => new(kind, ">",  Cat_Op, "Greater-than comparison"),
        TokenKind.LessThan            => new(kind, "<",  Cat_Op, "Less-than comparison"),
        TokenKind.Assign              => new(kind, "=",  Cat_Op, "Assignment"),
        TokenKind.Plus                => new(kind, "+",  Cat_Op, "Addition / string concatenation"),
        TokenKind.Minus               => new(kind, "-",  Cat_Op, "Subtraction / unary negation"),
        TokenKind.Star                => new(kind, "*",  Cat_Op, "Multiplication"),
        TokenKind.Slash               => new(kind, "/",  Cat_Op, "Division"),
        TokenKind.Percent             => new(kind, "%",  Cat_Op, "Modulo"),
        TokenKind.Arrow               => new(kind, "->", Cat_Op, "Action chain / outcome separator"),

        // ── Punctuation ────────────────────────────────────────────
        TokenKind.Dot          => new(kind, ".",  Cat_Pun, "Member access"),
        TokenKind.Comma        => new(kind, ",",  Cat_Pun, "List separator"),
        TokenKind.LeftParen    => new(kind, "(",  Cat_Pun, "Open parenthesis"),
        TokenKind.RightParen   => new(kind, ")",  Cat_Pun, "Close parenthesis"),
        TokenKind.LeftBracket  => new(kind, "[",  Cat_Pun, "Open bracket"),
        TokenKind.RightBracket => new(kind, "]",  Cat_Pun, "Close bracket"),

        // ── Literals ───────────────────────────────────────────────
        TokenKind.NumberLiteral       => new(kind, null, Cat_Lit, "Numeric literal"),
        TokenKind.StringLiteral       => new(kind, null, Cat_Lit, "String literal (no interpolation)"),
        TokenKind.StringStart         => new(kind, null, Cat_Lit, "String before first interpolation"),
        TokenKind.StringMiddle        => new(kind, null, Cat_Lit, "String between interpolation segments"),
        TokenKind.StringEnd           => new(kind, null, Cat_Lit, "String after last interpolation"),
        TokenKind.TypedConstant       => new(kind, null, Cat_Lit, "Typed constant (no interpolation)"),
        TokenKind.TypedConstantStart  => new(kind, null, Cat_Lit, "Typed constant before first interpolation"),
        TokenKind.TypedConstantMiddle => new(kind, null, Cat_Lit, "Typed constant between interpolation segments"),
        TokenKind.TypedConstantEnd    => new(kind, null, Cat_Lit, "Typed constant after last interpolation"),

        // ── Identifiers ────────────────────────────────────────────
        TokenKind.Identifier  => new(kind, null, Cat_Id, "User-defined identifier"),

        // ── Structural ─────────────────────────────────────────────
        TokenKind.EndOfSource => new(kind, null, Cat_Str, "End of source"),
        TokenKind.NewLine     => new(kind, null, Cat_Str, "Line terminator"),
        TokenKind.Comment     => new(kind, null, Cat_Str, "Comment"),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>All token metadata entries, one per <see cref="TokenKind"/> value.</summary>
    public static IReadOnlyList<TokenMeta> All { get; } =
        Enum.GetValues<TokenKind>().Select(GetMeta).ToList();

    /// <summary>
    /// Keyword text → TokenKind lookup. Used by the lexer to classify identifier
    /// text as a keyword. Dual-use tokens appear once — the parser disambiguates.
    /// <c>SetType</c> is explicitly excluded: the lexer always emits <c>Set</c>;
    /// the parser synthesizes <c>SetType</c> from context.
    /// </summary>
    public static FrozenDictionary<string, TokenKind> Keywords { get; } =
        All
            .Where(m => m.Text is not null && m.Kind != TokenKind.SetType && m.Categories.Any(c =>
                c is TokenCategory.Declaration or TokenCategory.Preposition
                    or TokenCategory.Control or TokenCategory.Action
                    or TokenCategory.Outcome or TokenCategory.AccessMode
                    or TokenCategory.LogicalOperator or TokenCategory.Membership
                    or TokenCategory.Quantifier or TokenCategory.StateModifier
                    or TokenCategory.Constraint or TokenCategory.Type
                    or TokenCategory.Literal))
            .DistinctBy(m => m.Text)
            .ToFrozenDictionary(m => m.Text!, m => m.Kind);
}
