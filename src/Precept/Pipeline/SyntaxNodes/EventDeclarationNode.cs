using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>event Name(Args) initial?</c></summary>
public sealed record EventDeclarationNode(
    SourceSpan Span,
    ImmutableArray<Token> Names,
    ImmutableArray<ArgumentNode> Arguments,
    bool IsInitial) : Declaration(Span);

/// <summary>A single event argument: <c>name as type modifiers</c></summary>
public sealed record ArgumentNode(
    SourceSpan Span,
    Token Name,
    TypeRefNode Type,
    ImmutableArray<FieldModifierNode> Modifiers) : SyntaxNode(Span);
