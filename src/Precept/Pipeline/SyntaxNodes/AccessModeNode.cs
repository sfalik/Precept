using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>in State modify Field readonly|editable [when Guard]</c></summary>
public sealed record AccessModeNode(
    SourceSpan Span,
    StateTargetNode State,
    FieldTargetNode Fields,
    Token Mode,
    Expression? Guard) : Declaration(Span);
