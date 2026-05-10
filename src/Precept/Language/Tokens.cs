using System.Collections.Frozen;

namespace Precept.Language;

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

    // ── ValidAfter predecessor sets ─────────────────────────────────────────────
    // Shared arrays for common predecessor token sets used by ValidAfter.
    // These encode "which tokens can immediately precede this token in a valid program."

    /// <summary>
    /// Declaration-starting keywords are keyword-anchored, not newline-following.
    /// Whitespace is cosmetic (§0.1.5); NewLine tokens are stripped before the parser.
    /// This set is advisory completion metadata, not a parse constraint.
    /// </summary>
    private static readonly TokenKind[] VA_DeclStart = [];

    /// <summary>Type keywords appear after 'as' (field type) or 'of' (collection inner type / qualifier).</summary>
    private static readonly TokenKind[] VA_TypeRef = [TokenKind.As, TokenKind.Of];

    /// <summary>Action keywords and outcome keywords appear after '->' in action chains.</summary>
    private static readonly TokenKind[] VA_AfterArrow = [TokenKind.Arrow];

    /// <summary>'transition' follows '->' or 'no' keyword.</summary>
    private static readonly TokenKind[] VA_Transition = [TokenKind.Arrow, TokenKind.No];

    /// <summary>'as' and 'into' appear after Identifier (field name or event arg name).</summary>
    private static readonly TokenKind[] VA_AfterIdent = [TokenKind.Identifier];

    /// <summary>'all' follows modify or omit keyword (e.g. modify all, omit all). write/read and write all retired in B4 (2026-04-28).</summary>
    private static readonly TokenKind[] VA_AllQuantifier = [TokenKind.Modify, TokenKind.Omit];

    /// <summary>'any' follows prepositions as a state wildcard (in any, from any, to any).</summary>
    private static readonly TokenKind[] VA_AnyQuantifier = [TokenKind.In, TokenKind.From, TokenKind.To];

    /// <summary>State modifiers appear after Identifier (state name) or after other state modifiers.</summary>
    private static readonly TokenKind[] VA_StateModifier =
    [
        TokenKind.Identifier,
        TokenKind.Initial, TokenKind.Terminal, TokenKind.Required,
        TokenKind.Irreversible, TokenKind.Success, TokenKind.Warning, TokenKind.Error,
    ];

    /// <summary>
    /// Field constraint/modifier keywords appear after type keywords, type qualifiers,
    /// or after other constraints. This covers the modifier zone in field declarations.
    /// </summary>
    private static readonly TokenKind[] VA_FieldModifier =
    [
        // After type keywords (scalar)
        TokenKind.StringType, TokenKind.BooleanType, TokenKind.IntegerType,
        TokenKind.DecimalType, TokenKind.NumberType, TokenKind.ChoiceType,
        // After temporal type keywords
        TokenKind.DateType, TokenKind.TimeType, TokenKind.InstantType,
        TokenKind.DurationType, TokenKind.PeriodType, TokenKind.TimezoneType,
        TokenKind.ZonedDateTimeType, TokenKind.DateTimeType,
        // After business-domain type keywords
        TokenKind.MoneyType, TokenKind.CurrencyType, TokenKind.QuantityType,
        TokenKind.UnitOfMeasureType, TokenKind.DimensionType, TokenKind.PriceType,
        TokenKind.ExchangeRateType,
        // After collection inner-type close
        TokenKind.RightParen,
        // After type qualifiers (in 'USD', of 'mass') — typed constant ends the qualifier
        TokenKind.TypedConstant, TokenKind.StringLiteral, TokenKind.Identifier,
        // After other modifiers/constraints
        TokenKind.Optional, TokenKind.Writable, TokenKind.Default, TokenKind.Nonnegative, TokenKind.Positive,
        TokenKind.Nonzero, TokenKind.Notempty, TokenKind.Ordered,
        TokenKind.NumberLiteral, // after min/max/minlength/etc. value
    ];

    /// <summary>Valued constraints (min, max, minlength, etc.) can follow the same predecessors as field modifiers.</summary>
    private static readonly TokenKind[] VA_ValuedConstraint = VA_FieldModifier;

    public static TokenMeta GetMeta(TokenKind kind) => kind switch
    {
        // ── Keywords: Declaration ───────────────────────────────────
        TokenKind.Precept     => new(kind, "precept",     Cat_Decl, "Precept header declaration",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic),
        TokenKind.Field       => new(kind, "field",       Cat_Decl, "Field declaration",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_DeclStart),
        TokenKind.State       => new(kind, "state",       Cat_Decl, "State declaration",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_DeclStart),
        TokenKind.Event       => new(kind, "event",       Cat_Decl, "Event declaration",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_DeclStart),
        TokenKind.Rule        => new(kind, "rule",        Cat_Decl, "Named rule / invariant declaration",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_DeclStart),
        TokenKind.Ensure      => new(kind, "ensure",      Cat_Decl, "State/event assertion keyword",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic),
        TokenKind.As          => new(kind, "as",          Cat_Decl, "Type annotation",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_AfterIdent),
        TokenKind.Default     => new(kind, "default",     Cat_Decl, "Default value modifier",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_FieldModifier),
        TokenKind.Optional    => new(kind, "optional",    Cat_Decl, "Field optionality modifier",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_FieldModifier),
        TokenKind.Writable    => new(kind, "writable",    Cat_Decl, "Field writable-baseline modifier",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_FieldModifier),
        TokenKind.Because     => new(kind, "because",     Cat_Decl, "Reason clause",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, IsMessagePosition: true),
        TokenKind.Initial     => new(kind, "initial",     Cat_Decl, "Initial state/event marker",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_StateModifier),

        // ── Keywords: Prepositions ─────────────────────────────────
        TokenKind.In          => new(kind, "in",          Cat_Prep, "State scope / type qualifier",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.To          => new(kind, "to",          Cat_Prep, "Entry-gate ensure / transition target",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_DeclStart,
            IsValidAsMemberName: true),
        TokenKind.From        => new(kind, "from",        Cat_Prep, "Exit-gate ensure / transition source",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_DeclStart,
            IsValidAsMemberName: true),
        TokenKind.On          => new(kind, "on",          Cat_Prep, "Event trigger",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.Of          => new(kind, "of",          Cat_Prep, "Collection inner type / dimension family qualifier",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.Into        => new(kind, "into",        Cat_Prep, "Dequeue/pop target",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_AfterIdent),

        // ── Keywords: Control ──────────────────────────────────────
        TokenKind.When        => new(kind, "when",        Cat_Ctrl, "Guard clause",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.If          => new(kind, "if",          Cat_Ctrl, "Conditional expression",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.Then        => new(kind, "then",        Cat_Ctrl, "Conditional expression",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.Else        => new(kind, "else",        Cat_Ctrl, "Conditional expression",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),

        // ── Keywords: Actions ──────────────────────────────────────
        TokenKind.Set         => new(kind, "set",         Cat_Act, "Field assignment / set collection type",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Add         => new(kind, "add",         Cat_Act,     "Set add action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Remove      => new(kind, "remove",      Cat_Act,     "Set remove action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Enqueue     => new(kind, "enqueue",     Cat_Act,     "Queue enqueue action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Dequeue     => new(kind, "dequeue",     Cat_Act,     "Queue dequeue action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Push        => new(kind, "push",        Cat_Act,     "Stack push action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Pop         => new(kind, "pop",         Cat_Act,     "Stack pop action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Clear       => new(kind, "clear",       Cat_Act,     "Collection clear / optional field clear",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),

        // ── Keywords: Outcomes ─────────────────────────────────────
        TokenKind.Transition  => new(kind, "transition",  Cat_Out, "State transition outcome",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_Transition),
        TokenKind.No          => new(kind, "no",          Cat_Out, "Prefix for 'no transition'",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Reject      => new(kind, "reject",      Cat_Out, "Rejection outcome",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow, IsMessagePosition: true),

        // ── Keywords: Access Modes (B4 — 2026-04-28) ──────────────────
        // Write and Read retired: vocabulary locked B4. New: modify/readonly/editable.
        TokenKind.Modify        => new(kind, "modify",        Cat_Acc, "Access mode verb: declare field access constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic),
        TokenKind.Readonly      => new(kind, "readonly",      Cat_Acc, "Access mode adjective: field is read-only in this state",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, IsAccessModeAdjective: true),
        TokenKind.Editable      => new(kind, "editable",      Cat_Acc, "Access mode adjective: field is writable in this state",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, IsAccessModeAdjective: true),
        TokenKind.Omit          => new(kind, "omit",          Cat_Acc, "Field omit: field is structurally absent",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic),

        // ── Keywords: Logical Operators ────────────────────────────
        TokenKind.And         => new(kind, "and",         Cat_Log, "Logical conjunction",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.Or          => new(kind, "or",          Cat_Log, "Logical disjunction",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.Not         => new(kind, "not",         Cat_Log, "Logical negation",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),

        // ── Keywords: Membership ───────────────────────────────────
        TokenKind.Contains    => new(kind, "contains",    Cat_Mem, "Collection membership test",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.Is          => new(kind, "is",          Cat_Mem, "Multi-token operator prefix (is set, is not set)",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),

        // ── Keywords: Quantifiers / Modifiers ──────────────────────
        TokenKind.All         => new(kind, "all",         Cat_Qnt, "Universal quantifier",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_AllQuantifier,
            IsBroadcastFieldTarget: true),
        TokenKind.Any         => new(kind, "any",         Cat_Qnt, "State wildcard (in any, from any)",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_AnyQuantifier,
            IsStateWildcard: true),

        // ── Keywords: State Modifiers ──────────────────────────────
        TokenKind.Terminal    => new(kind, "terminal",    Cat_SMod, "Structural: no outgoing transitions",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_StateModifier),
        TokenKind.Required    => new(kind, "required",    Cat_SMod, "Structural: all initial→terminal paths visit this state",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_StateModifier),
        TokenKind.Irreversible=> new(kind, "irreversible",Cat_SMod, "Structural: no path back to any ancestor state",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_StateModifier),
        TokenKind.Success     => new(kind, "success",     Cat_SMod, "Semantic: success outcome state",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_StateModifier),
        TokenKind.Warning     => new(kind, "warning",     Cat_SMod, "Semantic: warning outcome state",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_StateModifier),
        TokenKind.Error       => new(kind, "error",       Cat_SMod, "Semantic: error outcome state",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_StateModifier),

        // ── Keywords: Constraints ──────────────────────────────────
        TokenKind.Nonnegative => new(kind, "nonnegative", Cat_Cns, "Number/integer constraint: value >= 0",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_FieldModifier),
        TokenKind.Positive    => new(kind, "positive",    Cat_Cns, "Number/integer constraint: value > 0",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_FieldModifier),
        TokenKind.Nonzero     => new(kind, "nonzero",     Cat_Cns, "Number/integer constraint: value != 0",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_FieldModifier),
        TokenKind.Notempty    => new(kind, "notempty",     Cat_Cns, "String or collection constraint: non-empty",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_FieldModifier),
        TokenKind.Min         => new(kind, "min",         Cat_Cns, "Numeric minimum value constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_ValuedConstraint,
            IsAlsoBuiltinFunction: true, IsValidAsMemberName: true),
        TokenKind.Max         => new(kind, "max",         Cat_Cns, "Numeric maximum value constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_ValuedConstraint,
            IsAlsoBuiltinFunction: true, IsValidAsMemberName: true),
        TokenKind.Minlength   => new(kind, "minlength",   Cat_Cns, "String minimum length constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_ValuedConstraint),
        TokenKind.Maxlength   => new(kind, "maxlength",   Cat_Cns, "String maximum length constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_ValuedConstraint),
        TokenKind.Mincount    => new(kind, "mincount",    Cat_Cns, "Collection minimum count constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_ValuedConstraint),
        TokenKind.Maxcount    => new(kind, "maxcount",    Cat_Cns, "Collection maximum count constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_ValuedConstraint),
        TokenKind.Maxplaces   => new(kind, "maxplaces",   Cat_Cns, "Decimal maximum decimal places constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_ValuedConstraint),
        TokenKind.Ordered     => new(kind, "ordered",     Cat_Cns, "Choice ordinal comparison constraint",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar, ValidAfter: VA_FieldModifier),

        // ── Keywords: Types (Primitive / Collection) ───────────────
        TokenKind.StringType  => new(kind, "string",      Cat_Type, "Scalar type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.BooleanType => new(kind, "boolean",     Cat_Type, "Scalar type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.IntegerType => new(kind, "integer",     Cat_Type, "Scalar type: explicit integer",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.DecimalType => new(kind, "decimal",     Cat_Type, "Scalar type: exact base-10",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.NumberType  => new(kind, "number",      Cat_Type, "Scalar type: general numeric",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.ChoiceType  => new(kind, "choice",      Cat_Type, "Constrained string value set type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.SetType     => new(kind, null,          Cat_Type, "Set collection type (parser-synthesized from TokenKind.Set in type position)",
            ValidAfter: null),
        TokenKind.QueueType   => new(kind, "queue",       Cat_Type, "Queue collection type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.StackType   => new(kind, "stack",       Cat_Type, "Stack collection type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),

        // ── Keywords: Temporal Types ───────────────────────────────
        TokenKind.DateType          => new(kind, "date",          Cat_Type, "Temporal: calendar date",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef,
            IsValidAsMemberName: true),
        TokenKind.TimeType          => new(kind, "time",          Cat_Type, "Temporal: time of day",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef,
            IsValidAsMemberName: true),
        TokenKind.InstantType       => new(kind, "instant",       Cat_Type, "Temporal: UTC point in time",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef,
            IsValidAsMemberName: true),
        TokenKind.DurationType      => new(kind, "duration",      Cat_Type, "Temporal: elapsed time quantity",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.PeriodType        => new(kind, "period",        Cat_Type, "Temporal: calendar quantity",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.TimezoneType      => new(kind, "timezone",      Cat_Type, "Temporal: timezone identity",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef,
            IsValidAsMemberName: true),
        TokenKind.ZonedDateTimeType => new(kind, "zoneddatetime", Cat_Type, "Temporal: date+time+timezone",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.DateTimeType      => new(kind, "datetime",      Cat_Type, "Temporal: local date+time",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef,
            IsValidAsMemberName: true),

        // ── Keywords: Business-Domain Types ────────────────────────
        TokenKind.MoneyType         => new(kind, "money",         Cat_Type, "Business: monetary amount",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.CurrencyType      => new(kind, "currency",      Cat_Type, "Business: currency identity",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef,
            IsValidAsMemberName: true),
        TokenKind.QuantityType      => new(kind, "quantity",       Cat_Type, "Business: measured quantity",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.UnitOfMeasureType => new(kind, "unitofmeasure", Cat_Type, "Business: unit identity",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.DimensionType     => new(kind, "dimension",     Cat_Type, "Business: dimension family identity",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef,
            IsValidAsMemberName: true),
        TokenKind.PriceType         => new(kind, "price",         Cat_Type, "Business: compound money/quantity rate",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.ExchangeRateType  => new(kind, "exchangerate",  Cat_Type, "Business: compound currency/currency rate",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),

        // ── Keywords: Literals ─────────────────────────────────────
        TokenKind.True        => new(kind, "true",        Cat_Lit, "Boolean literal",
            VisualCategory: SemanticTokenTypeKind.Value),
        TokenKind.False       => new(kind, "false",       Cat_Lit, "Boolean literal",
            VisualCategory: SemanticTokenTypeKind.Value),

        // ── Operators ──────────────────────────────────────────────
        TokenKind.DoubleEquals        => new(kind, "==", Cat_Op, "Equality comparison",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.NotEquals           => new(kind, "!=", Cat_Op, "Inequality comparison",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.CaseInsensitiveEquals    => new(kind, "~=", Cat_Op, "Case-insensitive equality (string-only)",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.CaseInsensitiveNotEquals => new(kind, "!~", Cat_Op, "Case-insensitive not-equals (string-only)",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Tilde                    => new(kind, "~",  Cat_Op, "Case-insensitive collection inner type prefix",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.GreaterThanOrEqual  => new(kind, ">=", Cat_Op, "Greater-than-or-equal comparison",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.LessThanOrEqual     => new(kind, "<=", Cat_Op, "Less-than-or-equal comparison",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.GreaterThan         => new(kind, ">",  Cat_Op, "Greater-than comparison",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.LessThan            => new(kind, "<",  Cat_Op, "Less-than comparison",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Assign              => new(kind, "=",  Cat_Op, "Assignment",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Plus                => new(kind, "+",  Cat_Op, "Addition / string concatenation",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Minus               => new(kind, "-",  Cat_Op, "Subtraction / unary negation",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Star                => new(kind, "*",  Cat_Op, "Multiplication",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Slash               => new(kind, "/",  Cat_Op, "Division",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Percent             => new(kind, "%",  Cat_Op, "Modulo",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Arrow               => new(kind, "->", Cat_Op, "Action chain / outcome separator",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.BackArrow           => new(kind, "<-", Cat_Op, "Computed field derivation",
            VisualCategory: SemanticTokenTypeKind.Operator),

        // ── Punctuation ────────────────────────────────────────────
        TokenKind.Dot          => new(kind, ".",  Cat_Pun, "Member access",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.Comma        => new(kind, ",",  Cat_Pun, "List separator",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.LeftParen    => new(kind, "(",  Cat_Pun, "Open parenthesis",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.RightParen   => new(kind, ")",  Cat_Pun, "Close parenthesis",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.LeftBracket  => new(kind, "[",  Cat_Pun, "Open bracket",
            VisualCategory: SemanticTokenTypeKind.Operator),
        TokenKind.RightBracket => new(kind, "]",  Cat_Pun, "Close bracket",
            VisualCategory: SemanticTokenTypeKind.Operator),

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
        TokenKind.Identifier  => new(kind, null, Cat_Id, "User-defined identifier",
            VisualCategory: SemanticTokenTypeKind.Name),

        // ── Structural ─────────────────────────────────────────────
        TokenKind.EndOfSource => new(kind, null, Cat_Str, "End of source"),
        TokenKind.NewLine     => new(kind, null, Cat_Str, "Line terminator"),
        TokenKind.Comment     => new(kind, null, Cat_Str, "Comment",
            VisualCategory: SemanticTokenTypeKind.Comment),

        // ── New Collection type keywords ────────────────────────────────
        TokenKind.BagType     => new(kind, "bag",        Cat_Type, "Bag collection type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.ListType    => new(kind, "list",       Cat_Type, "List collection type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.LogType     => new(kind, "log",        Cat_Type, "Log collection type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),
        TokenKind.LookupType  => new(kind, "lookup",     Cat_Type, "Lookup collection type",
            VisualCategory: SemanticTokenTypeKind.Type, ValidAfter: VA_TypeRef),

        // ── New ordering / indexing keywords ───────────────────────────
        TokenKind.By          => new(kind, "by",         Cat_Prep, "Ordering key preposition",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.At          => new(kind, "at",         Cat_Prep, "Index position preposition",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar,
            IsValidAsMemberName: true),
        TokenKind.Ascending   => new(kind, "ascending",  Cat_Decl, "Ascending sort order",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),
        TokenKind.Descending  => new(kind, "descending", Cat_Decl, "Descending sort order",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),

        // ── New action keywords ─────────────────────────────────────────
        TokenKind.Append      => new(kind, "append",     Cat_Act,  "Log/list append action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Insert      => new(kind, "insert",     Cat_Act,  "List insert action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),
        TokenKind.Put         => new(kind, "put",        Cat_Act,  "Lookup put action",
            VisualCategory: SemanticTokenTypeKind.KeywordSemantic, ValidAfter: VA_AfterArrow),

        // ── New quantifier keyword ──────────────────────────────────────
        TokenKind.Each        => new(kind, "each",       Cat_Qnt,  "Bounded quantifier: each element",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),

        // ── New lookup access operator ──────────────────────────────────
        TokenKind.For         => new(kind, "for",        Cat_Prep, "Lookup key access infix operator",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar),

        // ── New member-name tokens ─────────────────────────────────────
        TokenKind.Countof     => new(kind, "countof",    Cat_Cns,  "Bag element count accessor",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar,
            IsValidAsMemberName: true),
        TokenKind.Peekby      => new(kind, "peekby",     Cat_Cns,  "Priority queue ordering-key peek accessor",
            VisualCategory: SemanticTokenTypeKind.KeywordGrammar,
            IsValidAsMemberName: true),

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

    /// <summary>
    /// Two-character operator table. Keys are <c>(first char, second char)</c> tuples;
    /// values are <c>(TokenKind, text)</c> pairs. Derived from <see cref="All"/> entries
    /// whose <c>Text</c> is exactly two characters and whose category includes
    /// <see cref="TokenCategory.Operator"/>.
    /// Used by the lexer to resolve multi-character operators in a single table lookup
    /// (maximal-munch guarantee).
    /// </summary>
    public static FrozenDictionary<(char, char), (TokenKind Kind, string Text)> TwoCharOperators { get; } =
        All
            .Where(m => m.Text is { Length: 2 } && m.Categories.Any(c =>
                c is TokenCategory.Operator))
            .ToFrozenDictionary(m => (m.Text![0], m.Text[1]), m => (m.Kind, m.Text!));

    /// <summary>
    /// Single-character operator table. Keys are the operator character; values are
    /// <c>(TokenKind, text)</c> pairs. Derived from <see cref="All"/> entries whose
    /// <c>Text</c> is exactly one character and whose categories include
    /// <see cref="TokenCategory.Operator"/>.
    /// Used by the lexer after the two-char guard fails.
    /// </summary>
    public static FrozenDictionary<char, (TokenKind Kind, string Text)> SingleCharOperators { get; } =
        All
            .Where(m => m.Text is { Length: 1 } && m.Categories.Any(c =>
                c is TokenCategory.Operator))
            .ToFrozenDictionary(m => m.Text![0], m => (m.Kind, m.Text!));

    /// <summary>
    /// Punctuation character table. Keys are the punctuation character; values are
    /// <c>(TokenKind, text)</c> pairs. Derived from <see cref="All"/> entries whose
    /// <c>Text</c> is exactly one character and whose categories include
    /// <see cref="TokenCategory.Punctuation"/>.
    /// Delimiter characters (<c>{</c>, <c>}</c>, <c>"</c>, <c>'</c>) are absent —
    /// they have dedicated mode-transition handling in <c>ScanToken</c> and are never
    /// emitted as punctuation tokens.
    /// </summary>
    public static FrozenDictionary<char, (TokenKind Kind, string Text)> PunctuationChars { get; } =
        All
            .Where(m => m.Text is { Length: 1 } && m.Categories.Any(c =>
                c is TokenCategory.Punctuation))
            .ToFrozenDictionary(m => m.Text![0], m => (m.Kind, m.Text!));

    /// <summary>
    /// Set of characters that can begin a two-character operator. Used as a fast guard
    /// in <c>TryScanOperator</c> before attempting a tuple lookup into
    /// <see cref="TwoCharOperators"/>, avoiding a tuple allocation on every non-starter.
    /// </summary>
    public static FrozenSet<char> TwoCharOperatorStarters { get; } =
        TwoCharOperators.Keys.Select(k => k.Item1).ToFrozenSet();

    /// <summary>
    /// Tokens that function as access-mode adjectives. Derived from
    /// <see cref="TokenMeta.IsAccessModeAdjective"/> entries.
    /// </summary>
    public static FrozenSet<TokenKind> AccessModeKeywords { get; } =
        All.Where(m => m.IsAccessModeAdjective).Select(m => m.Kind).ToFrozenSet();
}
