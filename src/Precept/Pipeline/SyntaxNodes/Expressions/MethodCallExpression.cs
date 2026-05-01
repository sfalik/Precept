using System.Collections.Immutable;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Method call: <c>obj.Method(arg, ...)</c>.</summary>
public sealed record MethodCallExpression(
    SourceSpan Span,
    Expression Receiver,
    string MethodName,
    ImmutableArray<Expression> Arguments) : Expression(Span);
