using System.Collections.Immutable;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Interpolated typed constant: <c>'Hello {name}'</c>.</summary>
public sealed record InterpolatedTypedConstantExpression(
    SourceSpan Span,
    ImmutableArray<InterpolationPart> Parts) : Expression(Span);
