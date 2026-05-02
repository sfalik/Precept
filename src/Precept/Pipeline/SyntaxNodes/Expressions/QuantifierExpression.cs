using Precept.Pipeline.SyntaxNodes;

namespace Precept.Pipeline.SyntaxNodes.Expressions;

/// <summary>
/// A bounded quantifier expression: <c>each e in items (predicate)</c>,
/// <c>any e in items (predicate)</c>, or <c>no e in items (predicate)</c>.
/// </summary>
public sealed record QuantifierExpression(
    SourceSpan Span,
    Language.Token Quantifier,
    Language.Token Binding,
    Expression Collection,
    Expression Predicate) : Expression(Span);

/// <summary>
/// A case-insensitive function call: <c>~startsWith(str, prefix)</c> or <c>~endsWith(str, suffix)</c>.
/// </summary>
public sealed record CIFunctionCallExpression(
    SourceSpan Span,
    Language.Token FunctionName,
    Expression Subject,
    Expression Argument) : Expression(Span);
