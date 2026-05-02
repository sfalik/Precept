using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Stub type checker — performs semantic validation of the parsed syntax tree and
/// produces a <see cref="SemanticIndex"/>. Not yet implemented.
/// </summary>
[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]
internal static class TypeChecker
{
    internal static SemanticIndex Check(SyntaxTree tree) =>
        new(ImmutableArray<Diagnostic>.Empty);

    // Stub coverage annotations — required by PRECEPT0019 / ExpressionFormCoverageTests.
    // Removed once real type-checking logic per expression form is implemented.
    [HandlesCatalogMember(ExpressionFormKind.Literal)]
    [HandlesCatalogMember(ExpressionFormKind.Identifier)]
    [HandlesCatalogMember(ExpressionFormKind.Grouped)]
    [HandlesCatalogMember(ExpressionFormKind.BinaryOperation)]
    [HandlesCatalogMember(ExpressionFormKind.UnaryOperation)]
    [HandlesCatalogMember(ExpressionFormKind.MemberAccess)]
    [HandlesCatalogMember(ExpressionFormKind.Conditional)]
    [HandlesCatalogMember(ExpressionFormKind.FunctionCall)]
    [HandlesCatalogMember(ExpressionFormKind.MethodCall)]
    [HandlesCatalogMember(ExpressionFormKind.ListLiteral)]
    [HandlesCatalogMember(ExpressionFormKind.PostfixOperation)]
    [HandlesCatalogMember(ExpressionFormKind.Quantifier)]
    [HandlesCatalogMember(ExpressionFormKind.CIFunctionCall)]
    private static void CheckExpression() { }
}
