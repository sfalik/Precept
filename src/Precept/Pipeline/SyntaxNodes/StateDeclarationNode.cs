using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>state Name Modifiers, ...</c></summary>
public sealed record StateDeclarationNode(
    SourceSpan Span,
    ImmutableArray<StateEntryNode> Entries) : Declaration(Span);

/// <summary>A single state name with its modifiers.</summary>
public sealed record StateEntryNode(
    SourceSpan Span,
    Token Name,
    ImmutableArray<Token> Modifiers) : SyntaxNode(Span);
