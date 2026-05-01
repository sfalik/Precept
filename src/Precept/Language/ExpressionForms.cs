namespace Precept.Language;

/// <summary>
/// Classification of expression shapes the Precept expression parser can produce.
/// The 13th catalog — a machine-readable taxonomy of what the Pratt parser can construct
/// and what role each form plays (null-denotation atom vs. left-denotation extension).
/// </summary>
public enum ExpressionFormKind
{
    // Atoms — null-denotation (nud): start a new expression
    Literal         = 1,
    Identifier      = 2,
    Grouped         = 3,

    // Composites — infix or prefix structural forms
    BinaryOperation = 4,
    UnaryOperation  = 5,
    MemberAccess    = 6,
    Conditional     = 7,

    // Invocations — call forms
    FunctionCall    = 8,
    MethodCall      = 9,

    // Collections — aggregate literal forms
    ListLiteral     = 10,
}

/// <summary>
/// High-level grouping of expression forms for consumers that need to bucket
/// forms by structural role without enumerating individual kinds.
/// </summary>
public enum ExpressionCategory { Atom = 1, Composite = 2, Invocation = 3, Collection = 4 }

/// <summary>
/// Metadata record for a single expression form.
/// </summary>
/// <param name="Kind">The enum member this record describes.</param>
/// <param name="Category">High-level structural category.</param>
/// <param name="IsLeftDenotation">
///   True for led (infix/postfix) forms that extend an existing left operand;
///   false for nud (prefix/atom) forms that start a new expression.
/// </param>
/// <param name="LeadTokens">
///   Token kinds that introduce this form in null-denotation position.
///   Empty for left-denotation forms — they are triggered by the Pratt loop's
///   led dispatch, not by an initial token.
/// </param>
/// <param name="HoverDocs">Human-readable description for tooling.</param>
public sealed record ExpressionFormMeta(
    ExpressionFormKind        Kind,
    ExpressionCategory        Category,
    bool                      IsLeftDenotation,
    IReadOnlyList<TokenKind>  LeadTokens,
    string                    HoverDocs);

/// <summary>
/// Catalog of all expression forms the Precept expression parser can produce.
/// </summary>
public static class ExpressionForms
{
    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static ExpressionFormMeta GetMeta(ExpressionFormKind kind) => kind switch
    {
        ExpressionFormKind.Literal          => new(kind, ExpressionCategory.Atom,       false, [TokenKind.StringLiteral, TokenKind.NumberLiteral, TokenKind.StringStart, TokenKind.TypedConstant, TokenKind.TypedConstantStart, TokenKind.True, TokenKind.False], "A literal value: string, number, boolean, or typed constant."),
        ExpressionFormKind.Identifier       => new(kind, ExpressionCategory.Atom,       false, [TokenKind.Identifier],    "A bare field or parameter name."),
        ExpressionFormKind.Grouped          => new(kind, ExpressionCategory.Atom,       false, [TokenKind.LeftParen],     "A parenthesized expression: (expr)."),
        ExpressionFormKind.BinaryOperation  => new(kind, ExpressionCategory.Composite,  true,  [],                        "An infix binary operation: left op right."),
        ExpressionFormKind.UnaryOperation   => new(kind, ExpressionCategory.Composite,  false, [TokenKind.Not, TokenKind.Minus], "A prefix unary operation: op expr."),
        ExpressionFormKind.MemberAccess     => new(kind, ExpressionCategory.Composite,  true,  [TokenKind.Dot],           "Dot-access on an expression: target.member."),
        ExpressionFormKind.Conditional      => new(kind, ExpressionCategory.Composite,  false, [TokenKind.If],            "A conditional expression: if cond then valueA else valueB."),
        ExpressionFormKind.FunctionCall     => new(kind, ExpressionCategory.Invocation, false, [TokenKind.Identifier],    "A named function call: name(args)."),
        ExpressionFormKind.MethodCall       => new(kind, ExpressionCategory.Invocation, true,  [TokenKind.LeftParen],     "A method call on an expression: target.method(args)."),
        ExpressionFormKind.ListLiteral      => new(kind, ExpressionCategory.Collection, false, [TokenKind.LeftBracket],   "A list literal: [elem, elem, ...]."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — flat list
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ExpressionFormMeta> All { get; } =
        Enum.GetValues<ExpressionFormKind>().Select(GetMeta).ToList().AsReadOnly();
}
