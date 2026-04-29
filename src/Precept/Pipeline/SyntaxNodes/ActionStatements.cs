namespace Precept.Pipeline.SyntaxNodes;

/// <summary>Action statements used in event handlers, transitions, and state actions.</summary>

public sealed record SetStatement(SourceSpan Span, Language.Token Field, Expression Value) : Statement(Span);
public sealed record AddStatement(SourceSpan Span, Language.Token Field, Expression Value) : Statement(Span);
public sealed record RemoveStatement(SourceSpan Span, Language.Token Field, Expression Value) : Statement(Span);
public sealed record EnqueueStatement(SourceSpan Span, Language.Token Field, Expression Value) : Statement(Span);
public sealed record DequeueStatement(SourceSpan Span, Language.Token Field, Language.Token? IntoField) : Statement(Span);
public sealed record PushStatement(SourceSpan Span, Language.Token Field, Expression Value) : Statement(Span);
public sealed record PopStatement(SourceSpan Span, Language.Token Field, Language.Token? IntoField) : Statement(Span);
public sealed record ClearStatement(SourceSpan Span, Language.Token Field) : Statement(Span);
