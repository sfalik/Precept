using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Layer 3 enforcement for ExpressionFormKind: catalog completeness, annotation bridge,
/// and parse-round-trip correctness. Mirrors PRECEPT0019 at xUnit runtime so that adding
/// a new ExpressionFormKind without a parser handler fails both the analyzer and these tests.
/// </summary>
public class ExpressionFormCoverageTests
{
    private static Expression ParseExpr(string source)
    {
        var tokens = Lexer.Lex(source);
        var session = new Parser.ParseSession(tokens.Tokens);
        return session.ParseExpression(0);
    }

    // ── Group 1: Catalog completeness ─────────────────────────────────────

    [Fact]
    public void ExpressionForms_All_ContainsAllEnumMembers()
    {
        foreach (var kind in Enum.GetValues<ExpressionFormKind>())
        {
            ExpressionForms.All.Should().ContainSingle(m => m.Kind == kind,
                because: $"ExpressionForms.All must contain exactly one entry for {kind}");
        }
    }

    [Fact]
    public void ExpressionForms_All_NoDuplicateKinds()
    {
        ExpressionForms.All.Select(m => m.Kind)
            .Should().OnlyHaveUniqueItems(
                because: "no two ExpressionForms.All entries may share the same Kind");
    }

    [Fact]
    public void ExpressionForms_All_NoNullHoverDocs()
    {
        foreach (var meta in ExpressionForms.All)
        {
            meta.HoverDocs.Should().NotBeNullOrWhiteSpace(
                because: $"{meta.Kind} must have a non-empty HoverDocs string");
        }
    }

    [Fact]
    public void ExpressionForms_All_NoNullCategory()
    {
        foreach (var meta in ExpressionForms.All)
        {
            Enum.IsDefined(meta.Category).Should().BeTrue(
                because: $"{meta.Kind} must have a defined ExpressionCategory value");
        }
    }

    // ── Group 2: Parser annotation coverage (reflection) ──────────────────

    [Fact]
    public void Parser_HasHandlesCatalogExhaustivelyForExpressionFormKind()
    {
        var preceptAssembly = typeof(ExpressionFormKind).Assembly;
        var matchingTypes = preceptAssembly.GetTypes()
            .Where(t => t.GetCustomAttributes<HandlesCatalogExhaustivelyAttribute>()
                .Any(a => a.CatalogEnum == typeof(ExpressionFormKind)))
            .ToList();

        matchingTypes.Should().HaveCount(3,
            because: "ParseSession, TypeChecker, and GraphAnalyzer must each carry [HandlesCatalogExhaustively(typeof(ExpressionFormKind))]");
    }

    [Fact]
    public void Parser_HandlesFormAnnotations_CoverAllExpressionFormKinds()
    {
        var preceptAssembly = typeof(ExpressionFormKind).Assembly;
        var annotatedTypes = preceptAssembly.GetTypes()
            .Where(t => t.GetCustomAttributes<HandlesCatalogExhaustivelyAttribute>()
                .Any(a => a.CatalogEnum == typeof(ExpressionFormKind)))
            .ToList();

        foreach (var annotatedType in annotatedTypes)
        {
            var handledKinds = annotatedType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetCustomAttributes<HandlesFormAttribute>())
                .Select(a => a.Kind)
                .OfType<ExpressionFormKind>()
                .ToHashSet();

            foreach (var kind in Enum.GetValues<ExpressionFormKind>())
            {
                handledKinds.Should().Contain(kind,
                    because: $"{annotatedType.Name} must have at least one [HandlesForm({kind})] annotation");
            }
        }
    }

    // ── Group 3: Parse round-trip per form ────────────────────────────────

    [Theory]
    [InlineData("42",           typeof(LiteralExpression))]
    [InlineData("\"hello\"",    typeof(LiteralExpression))]
    [InlineData("myField",      typeof(IdentifierExpression))]
    [InlineData("(x + 1)",      typeof(ParenthesizedExpression))]
    [InlineData("a + b",        typeof(BinaryExpression))]
    [InlineData("not x",        typeof(UnaryExpression))]
    [InlineData("obj.prop",     typeof(MemberAccessExpression))]
    [InlineData("[1, 2]",       typeof(ListLiteralExpression))]
    public void ParseExpression_ReturnsCorrectNodeTypeForForm(string input, Type expectedType)
    {
        var expr = ParseExpr(input);
        expr.Should().BeOfType(expectedType,
            because: $"parsing \"{input}\" must produce a {expectedType.Name}");
    }

    [Fact]
    public void ParseExpression_Conditional_ReturnsCorrectNodeType()
    {
        var expr = ParseExpr("if flag then a else b");
        expr.Should().BeOfType<ConditionalExpression>();
    }

    [Fact]
    public void ParseExpression_FunctionCall_ReturnsCorrectNodeType()
    {
        var expr = ParseExpr("someFunc(x)");
        expr.Should().BeOfType<CallExpression>();
    }

    [Fact]
    public void ParseExpression_MethodCall_ReturnsCorrectNodeType()
    {
        var expr = ParseExpr("obj.Method(x)");
        expr.Should().BeOfType<MethodCallExpression>();
    }
}
