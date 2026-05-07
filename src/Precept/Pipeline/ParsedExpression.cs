using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Abstract base of the ParsedExpression discriminated union. Each subtype
/// corresponds to exactly one <see cref="ExpressionFormKind"/> catalog member.
/// </summary>
public abstract record ParsedExpression(ExpressionFormKind Kind, SourceSpan Span);

/// <summary>A literal token as written in source.</summary>
public sealed record LiteralExpression(TokenKind LiteralKind, string Text, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.Literal, Span);

/// <summary>A bare field, argument, or quantifier binding name.</summary>
public sealed record IdentifierExpression(string Name, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.Identifier, Span);

/// <summary>A parenthesized expression that preserves grouping intent.</summary>
public sealed record GroupedExpression(ParsedExpression Inner, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.Grouped, Span);

/// <summary>An infix binary operation: left op right.</summary>
public sealed record BinaryOperationExpression(ParsedExpression Left, TokenKind Operator, ParsedExpression Right, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.BinaryOperation, Span);

/// <summary>A prefix unary operation: op operand.</summary>
public sealed record UnaryOperationExpression(TokenKind Operator, ParsedExpression Operand, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.UnaryOperation, Span);

/// <summary>Dot access on an expression target.</summary>
public sealed record MemberAccessExpression(ParsedExpression Target, TokenKind MemberTokenKind, string MemberName, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.MemberAccess, Span);

/// <summary>An if/then/else expression.</summary>
public sealed record ConditionalExpression(ParsedExpression Condition, ParsedExpression ThenBranch, ParsedExpression ElseBranch, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.Conditional, Span);

/// <summary>A named function call: name(args).</summary>
public sealed record FunctionCallExpression(string FunctionName, ImmutableArray<ParsedExpression> Arguments, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.FunctionCall, Span);

/// <summary>A method-style call on a target expression.</summary>
public sealed record MethodCallExpression(ParsedExpression Target, TokenKind MemberTokenKind, string MethodName, ImmutableArray<ParsedExpression> Arguments, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.MethodCall, Span);

/// <summary>A list literal: [elem, elem, ...].</summary>
public sealed record ListLiteralExpression(ImmutableArray<ParsedExpression> Elements, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.ListLiteral, Span);

/// <summary>A postfix presence check: expr is set / expr is not set.</summary>
public sealed record PostfixOperationExpression(ParsedExpression Operand, bool IsNegated, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.PostfixOperation, Span);

/// <summary>A bounded quantifier over a collection expression.</summary>
public sealed record QuantifierExpression(TokenKind QuantifierToken, string BindingName, ParsedExpression Collection, ParsedExpression Predicate, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.Quantifier, Span);

/// <summary>A case-insensitive function call using the ~ prefix surface.</summary>
// TODO: Revisit whether the parser should stamp resolved FunctionKind here once the CI-variant lookup decision lands.
public sealed record CIFunctionCallExpression(string FunctionName, ImmutableArray<ParsedExpression> Arguments, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.CIFunctionCall, Span);

/// <summary>
/// Missing expression sentinel — used when expression parsing fails or an expression is expected but absent.
/// Emitted when a guard or ensure clause keyword is present but no expression follows.
/// </summary>
public sealed record MissingExpression(SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.Literal, Span);

// ════════════════════════════════════════════════════════════════════════════
//  Interpolated string support
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Abstract base of the interpolation segment discriminated union.
/// Interpolated strings contain a sequence of text segments and hole segments.
/// </summary>
public abstract record InterpolationSegment(SourceSpan Span);

/// <summary>A literal text portion of an interpolated string.</summary>
public sealed record TextSegment(string Text, SourceSpan Span) : InterpolationSegment(Span);

/// <summary>An embedded expression hole within an interpolated string.</summary>
public sealed record HoleSegment(ParsedExpression Expression, SourceSpan Span) : InterpolationSegment(Span);

/// <summary>An interpolated string with embedded expressions: "Hello {name}!".</summary>
public sealed record InterpolatedStringExpression(ImmutableArray<InterpolationSegment> Segments, SourceSpan Span)
    : ParsedExpression(ExpressionFormKind.InterpolatedString, Span);
