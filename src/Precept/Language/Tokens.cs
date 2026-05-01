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

    // Dual-use tokens
    private static readonly TokenCategory[] Cat_ActType = [TokenCategory.Action, TokenCategory.Type];

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
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword"),
        TokenKind.Field       => new(kind, "field",       Cat_Decl, "Field declaration",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_DeclStart),
        TokenKind.State       => new(kind, "state",       Cat_Decl, "State declaration",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_DeclStart),
        TokenKind.Event       => new(kind, "event",       Cat_Decl, "Event declaration",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_DeclStart),
        TokenKind.Rule        => new(kind, "rule",        Cat_Decl, "Named rule / invariant declaration",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_DeclStart),
        TokenKind.Ensure      => new(kind, "ensure",      Cat_Decl, "State/event assertion keyword",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword"),
        TokenKind.As          => new(kind, "as",          Cat_Decl, "Type annotation",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterIdent),
        TokenKind.Default     => new(kind, "default",     Cat_Decl, "Default value modifier",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_FieldModifier),
        TokenKind.Optional    => new(kind, "optional",    Cat_Decl, "Field optionality modifier",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_FieldModifier),
        TokenKind.Writable    => new(kind, "writable",    Cat_Decl, "Field writable-baseline modifier",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_FieldModifier),
        TokenKind.Because     => new(kind, "because",     Cat_Decl, "Reason clause",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword"),
        TokenKind.Initial     => new(kind, "initial",     Cat_Decl, "Initial state/event marker",
            TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword", ValidAfter: VA_StateModifier),

        // ── Keywords: Prepositions ─────────────────────────────────
        TokenKind.In          => new(kind, "in",          Cat_Prep, "State scope / type qualifier",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),
        TokenKind.To          => new(kind, "to",          Cat_Prep, "Entry-gate ensure / transition target",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword", ValidAfter: VA_DeclStart),
        TokenKind.From        => new(kind, "from",        Cat_Prep, "Exit-gate ensure / transition source",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword", ValidAfter: VA_DeclStart),
        TokenKind.On          => new(kind, "on",          Cat_Prep, "Event trigger",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),
        TokenKind.Of          => new(kind, "of",          Cat_Prep, "Collection inner type / dimension family qualifier",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),
        TokenKind.Into        => new(kind, "into",        Cat_Prep, "Dequeue/pop target",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterIdent),

        // ── Keywords: Control ──────────────────────────────────────
        TokenKind.When        => new(kind, "when",        Cat_Ctrl, "Guard clause",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),
        TokenKind.If          => new(kind, "if",          Cat_Ctrl, "Conditional expression",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),
        TokenKind.Then        => new(kind, "then",        Cat_Ctrl, "Conditional expression",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),
        TokenKind.Else        => new(kind, "else",        Cat_Ctrl, "Conditional expression",
            TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),

        // ── Keywords: Actions ──────────────────────────────────────
        TokenKind.Set         => new(kind, "set",         Cat_ActType, "Field assignment / set collection type",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_AfterArrow),
        TokenKind.Add         => new(kind, "add",         Cat_Act,     "Set add action",
            TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
        TokenKind.Remove      => new(kind, "remove",      Cat_Act,     "Set remove action",
            TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
        TokenKind.Enqueue     => new(kind, "enqueue",     Cat_Act,     "Queue enqueue action",
            TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
        TokenKind.Dequeue     => new(kind, "dequeue",     Cat_Act,     "Queue dequeue action",
            TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
        TokenKind.Push        => new(kind, "push",        Cat_Act,     "Stack push action",
            TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
        TokenKind.Pop         => new(kind, "pop",         Cat_Act,     "Stack pop action",
            TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
        TokenKind.Clear       => new(kind, "clear",       Cat_Act,     "Collection clear / optional field clear",
            TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),

        // ── Keywords: Outcomes ─────────────────────────────────────
        TokenKind.Transition  => new(kind, "transition",  Cat_Out, "State transition outcome",
            TextMateScope: "keyword.other.outcome.precept", SemanticTokenType: "keyword", ValidAfter: VA_Transition),
        TokenKind.No          => new(kind, "no",          Cat_Out, "Prefix for 'no transition'",
            TextMateScope: "keyword.other.outcome.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
        TokenKind.Reject      => new(kind, "reject",      Cat_Out, "Rejection outcome",
            TextMateScope: "keyword.other.outcome.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),

        // ── Keywords: Access Modes (B4 — 2026-04-28) ──────────────────
        // Write and Read retired: vocabulary locked B4. New: modify/readonly/editable.
        TokenKind.Modify        => new(kind, "modify",        Cat_Acc, "Access mode verb: declare field access constraint",
            TextMateScope: "keyword.other.access-mode.precept", SemanticTokenType: "keyword"),
        TokenKind.Readonly      => new(kind, "readonly",      Cat_Acc, "Access mode adjective: field is read-only in this state",
            TextMateScope: "keyword.other.access-mode.precept", SemanticTokenType: "keyword", IsAccessModeAdjective: true),
        TokenKind.Editable      => new(kind, "editable",      Cat_Acc, "Access mode adjective: field is writable in this state",
            TextMateScope: "keyword.other.access-mode.precept", SemanticTokenType: "keyword", IsAccessModeAdjective: true),
        TokenKind.Omit          => new(kind, "omit",          Cat_Acc, "Field omit: field is structurally absent",
            TextMateScope: "keyword.other.access-mode.precept", SemanticTokenType: "keyword"),

        // ── Keywords: Logical Operators ────────────────────────────
        TokenKind.And         => new(kind, "and",         Cat_Log, "Logical conjunction",
            TextMateScope: "keyword.operator.logical.precept", SemanticTokenType: "operator"),
        TokenKind.Or          => new(kind, "or",          Cat_Log, "Logical disjunction",
            TextMateScope: "keyword.operator.logical.precept", SemanticTokenType: "operator"),
        TokenKind.Not         => new(kind, "not",         Cat_Log, "Logical negation",
            TextMateScope: "keyword.operator.logical.precept", SemanticTokenType: "operator"),

        // ── Keywords: Membership ───────────────────────────────────
        TokenKind.Contains    => new(kind, "contains",    Cat_Mem, "Collection membership test",
            TextMateScope: "keyword.operator.membership.precept", SemanticTokenType: "keyword"),
        TokenKind.Is          => new(kind, "is",          Cat_Mem, "Multi-token operator prefix (is set, is not set)",
            TextMateScope: "keyword.operator.membership.precept", SemanticTokenType: "keyword"),

        // ── Keywords: Quantifiers / Modifiers ──────────────────────
        TokenKind.All         => new(kind, "all",         Cat_Qnt, "Universal quantifier",
            TextMateScope: "keyword.other.quantifier.precept", SemanticTokenType: "keyword", ValidAfter: VA_AllQuantifier),
        TokenKind.Any         => new(kind, "any",         Cat_Qnt, "State wildcard (in any, from any)",
            TextMateScope: "keyword.other.quantifier.precept", SemanticTokenType: "keyword", ValidAfter: VA_AnyQuantifier),

        // ── Keywords: State Modifiers ──────────────────────────────
        TokenKind.Terminal    => new(kind, "terminal",    Cat_SMod, "Structural: no outgoing transitions",
            TextMateScope: "storage.modifier.state.precept", SemanticTokenType: "modifier", ValidAfter: VA_StateModifier),
        TokenKind.Required    => new(kind, "required",    Cat_SMod, "Structural: all initial→terminal paths visit this state",
            TextMateScope: "storage.modifier.state.precept", SemanticTokenType: "modifier", ValidAfter: VA_StateModifier),
        TokenKind.Irreversible=> new(kind, "irreversible",Cat_SMod, "Structural: no path back to any ancestor state",
            TextMateScope: "storage.modifier.state.precept", SemanticTokenType: "modifier", ValidAfter: VA_StateModifier),
        TokenKind.Success     => new(kind, "success",     Cat_SMod, "Semantic: success outcome state",
            TextMateScope: "storage.modifier.state.precept", SemanticTokenType: "modifier", ValidAfter: VA_StateModifier),
        TokenKind.Warning     => new(kind, "warning",     Cat_SMod, "Semantic: warning outcome state",
            TextMateScope: "storage.modifier.state.precept", SemanticTokenType: "modifier", ValidAfter: VA_StateModifier),
        TokenKind.Error       => new(kind, "error",       Cat_SMod, "Semantic: error outcome state",
            TextMateScope: "storage.modifier.state.precept", SemanticTokenType: "modifier", ValidAfter: VA_StateModifier),

        // ── Keywords: Constraints ──────────────────────────────────
        TokenKind.Nonnegative => new(kind, "nonnegative", Cat_Cns, "Number/integer constraint: value >= 0",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_FieldModifier),
        TokenKind.Positive    => new(kind, "positive",    Cat_Cns, "Number/integer constraint: value > 0",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_FieldModifier),
        TokenKind.Nonzero     => new(kind, "nonzero",     Cat_Cns, "Number/integer constraint: value != 0",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_FieldModifier),
        TokenKind.Notempty    => new(kind, "notempty",     Cat_Cns, "String constraint: non-empty",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_FieldModifier),
        TokenKind.Min         => new(kind, "min",         Cat_Cns, "Numeric minimum value constraint",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_ValuedConstraint,
            IsValidAsMemberName: true),
        TokenKind.Max         => new(kind, "max",         Cat_Cns, "Numeric maximum value constraint",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_ValuedConstraint,
            IsValidAsMemberName: true),
        TokenKind.Minlength   => new(kind, "minlength",   Cat_Cns, "String minimum length constraint",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_ValuedConstraint),
        TokenKind.Maxlength   => new(kind, "maxlength",   Cat_Cns, "String maximum length constraint",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_ValuedConstraint),
        TokenKind.Mincount    => new(kind, "mincount",    Cat_Cns, "Collection minimum count constraint",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_ValuedConstraint),
        TokenKind.Maxcount    => new(kind, "maxcount",    Cat_Cns, "Collection maximum count constraint",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_ValuedConstraint),
        TokenKind.Maxplaces   => new(kind, "maxplaces",   Cat_Cns, "Decimal maximum decimal places constraint",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_ValuedConstraint),
        TokenKind.Ordered     => new(kind, "ordered",     Cat_Cns, "Choice ordinal comparison constraint",
            TextMateScope: "keyword.other.constraint.precept", SemanticTokenType: "decorator", ValidAfter: VA_FieldModifier),

        // ── Keywords: Types (Primitive / Collection) ───────────────
        TokenKind.StringType  => new(kind, "string",      Cat_Type, "Scalar type",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.BooleanType => new(kind, "boolean",     Cat_Type, "Scalar type",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.IntegerType => new(kind, "integer",     Cat_Type, "Scalar type: explicit integer",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.DecimalType => new(kind, "decimal",     Cat_Type, "Scalar type: exact base-10",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.NumberType  => new(kind, "number",      Cat_Type, "Scalar type: general numeric",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.ChoiceType  => new(kind, "choice",      Cat_Type, "Constrained string value set type",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.SetType     => new(kind, null,          Cat_Type, "Set collection type (parser-synthesized from TokenKind.Set in type position)",
            TextMateScope: null, SemanticTokenType: null, ValidAfter: VA_TypeRef),
        TokenKind.QueueType   => new(kind, "queue",       Cat_Type, "Queue collection type",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.StackType   => new(kind, "stack",       Cat_Type, "Stack collection type",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),

        // ── Keywords: Temporal Types ───────────────────────────────
        TokenKind.DateType          => new(kind, "date",          Cat_Type, "Temporal: calendar date",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.TimeType          => new(kind, "time",          Cat_Type, "Temporal: time of day",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.InstantType       => new(kind, "instant",       Cat_Type, "Temporal: UTC point in time",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.DurationType      => new(kind, "duration",      Cat_Type, "Temporal: elapsed time quantity",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.PeriodType        => new(kind, "period",        Cat_Type, "Temporal: calendar quantity",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.TimezoneType      => new(kind, "timezone",      Cat_Type, "Temporal: timezone identity",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.ZonedDateTimeType => new(kind, "zoneddatetime", Cat_Type, "Temporal: date+time+timezone",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.DateTimeType      => new(kind, "datetime",      Cat_Type, "Temporal: local date+time",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),

        // ── Keywords: Business-Domain Types ────────────────────────
        TokenKind.MoneyType         => new(kind, "money",         Cat_Type, "Business: monetary amount",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.CurrencyType      => new(kind, "currency",      Cat_Type, "Business: currency identity",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.QuantityType      => new(kind, "quantity",       Cat_Type, "Business: measured quantity",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.UnitOfMeasureType => new(kind, "unitofmeasure", Cat_Type, "Business: unit identity",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.DimensionType     => new(kind, "dimension",     Cat_Type, "Business: dimension family identity",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.PriceType         => new(kind, "price",         Cat_Type, "Business: compound money/quantity rate",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),
        TokenKind.ExchangeRateType  => new(kind, "exchangerate",  Cat_Type, "Business: compound currency/currency rate",
            TextMateScope: "storage.type.precept", SemanticTokenType: "type", ValidAfter: VA_TypeRef),

        // ── Keywords: Literals ─────────────────────────────────────
        TokenKind.True        => new(kind, "true",        Cat_Lit, "Boolean literal",
            TextMateScope: "constant.language.boolean.precept", SemanticTokenType: "keyword"),
        TokenKind.False       => new(kind, "false",       Cat_Lit, "Boolean literal",
            TextMateScope: "constant.language.boolean.precept", SemanticTokenType: "keyword"),

        // ── Operators ──────────────────────────────────────────────
        TokenKind.DoubleEquals        => new(kind, "==", Cat_Op, "Equality comparison",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.NotEquals           => new(kind, "!=", Cat_Op, "Inequality comparison",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.CaseInsensitiveEquals    => new(kind, "~=", Cat_Op, "Case-insensitive equality (string-only)",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.CaseInsensitiveNotEquals => new(kind, "!~", Cat_Op, "Case-insensitive not-equals (string-only)",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.Tilde                    => new(kind, "~",  Cat_Op, "Case-insensitive collection inner type prefix",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.GreaterThanOrEqual  => new(kind, ">=", Cat_Op, "Greater-than-or-equal comparison",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.LessThanOrEqual     => new(kind, "<=", Cat_Op, "Less-than-or-equal comparison",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.GreaterThan         => new(kind, ">",  Cat_Op, "Greater-than comparison",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.LessThan            => new(kind, "<",  Cat_Op, "Less-than comparison",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.Assign              => new(kind, "=",  Cat_Op, "Assignment",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.Plus                => new(kind, "+",  Cat_Op, "Addition / string concatenation",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.Minus               => new(kind, "-",  Cat_Op, "Subtraction / unary negation",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.Star                => new(kind, "*",  Cat_Op, "Multiplication",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.Slash               => new(kind, "/",  Cat_Op, "Division",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.Percent             => new(kind, "%",  Cat_Op, "Modulo",
            TextMateScope: "keyword.operator.precept", SemanticTokenType: "operator"),
        TokenKind.Arrow               => new(kind, "->", Cat_Str, "Action chain / outcome separator",
            TextMateScope: "keyword.operator.arrow.precept", SemanticTokenType: "operator"),

        // ── Punctuation ────────────────────────────────────────────
        TokenKind.Dot          => new(kind, ".",  Cat_Pun, "Member access",
            TextMateScope: "punctuation.precept", SemanticTokenType: "operator"),
        TokenKind.Comma        => new(kind, ",",  Cat_Pun, "List separator",
            TextMateScope: "punctuation.precept", SemanticTokenType: "operator"),
        TokenKind.LeftParen    => new(kind, "(",  Cat_Pun, "Open parenthesis",
            TextMateScope: "punctuation.precept", SemanticTokenType: "operator"),
        TokenKind.RightParen   => new(kind, ")",  Cat_Pun, "Close parenthesis",
            TextMateScope: "punctuation.precept", SemanticTokenType: "operator"),
        TokenKind.LeftBracket  => new(kind, "[",  Cat_Pun, "Open bracket",
            TextMateScope: "punctuation.precept", SemanticTokenType: "operator"),
        TokenKind.RightBracket => new(kind, "]",  Cat_Pun, "Close bracket",
            TextMateScope: "punctuation.precept", SemanticTokenType: "operator"),

        // ── Literals ───────────────────────────────────────────────
        TokenKind.NumberLiteral       => new(kind, null, Cat_Lit, "Numeric literal",
            TextMateScope: "constant.numeric.precept", SemanticTokenType: "number"),
        TokenKind.StringLiteral       => new(kind, null, Cat_Lit, "String literal (no interpolation)",
            TextMateScope: "string.quoted.double.precept", SemanticTokenType: "string"),
        TokenKind.StringStart         => new(kind, null, Cat_Lit, "String before first interpolation",
            TextMateScope: "string.quoted.double.precept", SemanticTokenType: "string"),
        TokenKind.StringMiddle        => new(kind, null, Cat_Lit, "String between interpolation segments",
            TextMateScope: "string.quoted.double.precept", SemanticTokenType: "string"),
        TokenKind.StringEnd           => new(kind, null, Cat_Lit, "String after last interpolation",
            TextMateScope: "string.quoted.double.precept", SemanticTokenType: "string"),
        TokenKind.TypedConstant       => new(kind, null, Cat_Lit, "Typed constant (no interpolation)",
            TextMateScope: "string.quoted.single.precept", SemanticTokenType: "string"),
        TokenKind.TypedConstantStart  => new(kind, null, Cat_Lit, "Typed constant before first interpolation",
            TextMateScope: "string.quoted.single.precept", SemanticTokenType: "string"),
        TokenKind.TypedConstantMiddle => new(kind, null, Cat_Lit, "Typed constant between interpolation segments",
            TextMateScope: "string.quoted.single.precept", SemanticTokenType: "string"),
        TokenKind.TypedConstantEnd    => new(kind, null, Cat_Lit, "Typed constant after last interpolation",
            TextMateScope: "string.quoted.single.precept", SemanticTokenType: "string"),

        // ── Identifiers ────────────────────────────────────────────
        TokenKind.Identifier  => new(kind, null, Cat_Id, "User-defined identifier",
            TextMateScope: "entity.name.precept", SemanticTokenType: "variable"),

        // ── Structural ─────────────────────────────────────────────
        TokenKind.EndOfSource => new(kind, null, Cat_Str, "End of source",
            TextMateScope: null, SemanticTokenType: null),
        TokenKind.NewLine     => new(kind, null, Cat_Str, "Line terminator",
            TextMateScope: null, SemanticTokenType: null),
        TokenKind.Comment     => new(kind, null, Cat_Str, "Comment",
            TextMateScope: "comment.line.precept", SemanticTokenType: "comment"),

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
    /// whose <c>Text</c> is exactly two characters and whose categories include
    /// <see cref="TokenCategory.Operator"/> or <see cref="TokenCategory.Structural"/>.
    /// Used by the lexer to resolve multi-character operators in a single table lookup
    /// (maximal-munch guarantee).
    /// </summary>
    public static FrozenDictionary<(char, char), (TokenKind Kind, string Text)> TwoCharOperators { get; } =
        All
            .Where(m => m.Text is { Length: 2 } && m.Categories.Any(c =>
                c is TokenCategory.Operator or TokenCategory.Structural))
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
