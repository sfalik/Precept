using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Interpolated string: <c>"Hello {name}, you owe {amount}"</c>.</summary>
public sealed record InterpolatedStringExpression(
    SourceSpan Span,
    ImmutableArray<InterpolationPart> Parts) : Expression(Span);

/// <summary>DU: a segment of an interpolated string.</summary>
public abstract record InterpolationPart(SourceSpan Span) : SyntaxNode(Span);

/// <summary>Literal text segment within an interpolated string.</summary>
public sealed record TextInterpolationPart(SourceSpan Span, Token Text) : InterpolationPart(Span);

/// <summary>Expression hole <c>{expr}</c> within an interpolated string.</summary>
public sealed record ExpressionInterpolationPart(SourceSpan Span, Expression Value) : InterpolationPart(Span);
