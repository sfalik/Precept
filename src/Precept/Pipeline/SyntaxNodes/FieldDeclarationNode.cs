using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline.SyntaxNodes;

/// <summary><c>field Name as Type Modifiers [-> Expr]</c></summary>
public sealed record FieldDeclarationNode(
    SourceSpan Span,
    ImmutableArray<Token> Names,
    TypeRefNode Type,
    ImmutableArray<FieldModifierNode> Modifiers,
    Expression? ComputedExpression) : Declaration(Span);
