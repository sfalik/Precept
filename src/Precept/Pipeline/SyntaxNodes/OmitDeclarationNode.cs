namespace Precept.Pipeline.SyntaxNodes;

/// <summary>
/// <c>in State omit Field</c> — structural exclusion, unconditional.
/// No Mode property, no Guard property.
/// </summary>
public sealed record OmitDeclarationNode(
    SourceSpan Span,
    StateTargetNode State,
    FieldTargetNode Fields) : Declaration(Span);
