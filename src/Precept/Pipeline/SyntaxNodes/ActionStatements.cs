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

// ── Collection action statements (new types) ────────────────────────────────
/// <summary><c>append field value</c> — appends to log, logBy, or list.</summary>
public sealed record AppendStatement(SourceSpan Span, Language.Token Field, Expression Value) : Statement(Span);
/// <summary><c>append field value by key</c> — appends to logBy with explicit key expression.</summary>
public sealed record AppendByStatement(SourceSpan Span, Language.Token Field, Expression Value, Expression Key) : Statement(Span);
/// <summary><c>insert field value at index</c> — inserts into list at given index.</summary>
public sealed record InsertStatement(SourceSpan Span, Language.Token Field, Expression Value, Expression Index) : Statement(Span);
/// <summary><c>removeAt field index</c> — removes from list at given index.</summary>
public sealed record RemoveAtStatement(SourceSpan Span, Language.Token Field, Expression Index) : Statement(Span);
/// <summary><c>put field key value</c> — sets key→value in lookup.</summary>
public sealed record PutStatement(SourceSpan Span, Language.Token Field, Expression Key, Expression Value) : Statement(Span);
/// <summary><c>enqueue field value by priority</c> — enqueues into queueBy with priority expression.</summary>
public sealed record EnqueueByStatement(SourceSpan Span, Language.Token Field, Expression Value, Expression Priority) : Statement(Span);
/// <summary><c>dequeue field into binding</c> — dequeues from queueBy, binding result to optional identifier.</summary>
public sealed record DequeueByStatement(SourceSpan Span, Language.Token Field, Language.Token? IntoField) : Statement(Span);
