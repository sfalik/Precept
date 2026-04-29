using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>
/// Discriminated union: the field target in access mode and omit declarations.
/// Three shapes carry structurally different data — DU ensures compile-time
/// exhaustiveness via pattern matching.
/// </summary>
public abstract record FieldTargetNode(SourceSpan Span) : SyntaxNode(Span);

/// <summary>A single named field target.</summary>
public sealed record SingularFieldTarget(SourceSpan Span, Token Name) : FieldTargetNode(Span);

/// <summary>A comma-separated list of field names.</summary>
public sealed record ListFieldTarget(SourceSpan Span, ImmutableArray<Token> Names) : FieldTargetNode(Span);

/// <summary>The <c>all</c> keyword, targeting every field.</summary>
public sealed record AllFieldTarget(SourceSpan Span, Token AllToken) : FieldTargetNode(Span);
