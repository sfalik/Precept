using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the ExpressionForms catalog (13th catalog): enum completeness,
/// metadata shape, IsLeftDenotation correctness, and GetMeta exhaustiveness.
/// </summary>
public class ExpressionFormCatalogTests
{
    // ── Completeness ───────────────────────────────────────────────────────────

    [Fact]
    public void ExpressionForms_All_HasExpectedCount()
    {
        ExpressionForms.All.Should().HaveCount(11);
    }

    [Fact]
    public void ExpressionForms_All_NoneNull()
    {
        foreach (var meta in ExpressionForms.All)
        {
            meta.Should().NotBeNull();
            meta.HoverDocs.Should().NotBeNullOrWhiteSpace(
                because: $"{meta.Kind} must have non-empty HoverDocs");
        }
    }

    [Fact]
    public void ExpressionForms_All_KindsAreDistinct()
    {
        ExpressionForms.All.Select(m => m.Kind).Should().OnlyHaveUniqueItems();
    }

    // ── GetMeta exhaustiveness ─────────────────────────────────────────────────

    [Fact]
    public void ExpressionForms_GetMeta_AllMembersHandled()
    {
        foreach (var kind in Enum.GetValues<ExpressionFormKind>())
        {
            var act = () => ExpressionForms.GetMeta(kind);
            act.Should().NotThrow(because: $"GetMeta must not throw for {kind}");
            ExpressionForms.GetMeta(kind).Kind.Should().Be(kind);
        }
    }

    // ── IsLeftDenotation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(ExpressionFormKind.BinaryOperation)]
    [InlineData(ExpressionFormKind.MemberAccess)]
    [InlineData(ExpressionFormKind.MethodCall)]
    [InlineData(ExpressionFormKind.PostfixOperation)]
    public void ExpressionForms_IsLeftDenotation_CorrectForLedForms(ExpressionFormKind kind)
    {
        ExpressionForms.GetMeta(kind).IsLeftDenotation.Should().BeTrue(
            because: $"{kind} is a left-denotation (infix/postfix) form");
    }

    [Theory]
    [InlineData(ExpressionFormKind.Literal)]
    [InlineData(ExpressionFormKind.Identifier)]
    [InlineData(ExpressionFormKind.Grouped)]
    [InlineData(ExpressionFormKind.UnaryOperation)]
    [InlineData(ExpressionFormKind.Conditional)]
    [InlineData(ExpressionFormKind.FunctionCall)]
    [InlineData(ExpressionFormKind.ListLiteral)]
    public void ExpressionForms_IsLeftDenotation_CorrectForNudForms(ExpressionFormKind kind)
    {
        ExpressionForms.GetMeta(kind).IsLeftDenotation.Should().BeFalse(
            because: $"{kind} is a null-denotation (atom/prefix) form");
    }

    // ── Category shape ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ExpressionFormKind.Literal,         ExpressionCategory.Atom)]
    [InlineData(ExpressionFormKind.Identifier,      ExpressionCategory.Atom)]
    [InlineData(ExpressionFormKind.Grouped,         ExpressionCategory.Atom)]
    [InlineData(ExpressionFormKind.BinaryOperation, ExpressionCategory.Composite)]
    [InlineData(ExpressionFormKind.UnaryOperation,  ExpressionCategory.Composite)]
    [InlineData(ExpressionFormKind.MemberAccess,    ExpressionCategory.Composite)]
    [InlineData(ExpressionFormKind.Conditional,     ExpressionCategory.Composite)]
    [InlineData(ExpressionFormKind.FunctionCall,    ExpressionCategory.Invocation)]
    [InlineData(ExpressionFormKind.MethodCall,      ExpressionCategory.Invocation)]
    [InlineData(ExpressionFormKind.ListLiteral,     ExpressionCategory.Collection)]
    [InlineData(ExpressionFormKind.PostfixOperation, ExpressionCategory.Composite)]
    public void ExpressionForms_Category_CorrectForAllForms(ExpressionFormKind kind, ExpressionCategory expected)
    {
        ExpressionForms.GetMeta(kind).Category.Should().Be(expected);
    }

    // ── All count matches enum ─────────────────────────────────────────────────

    [Fact]
    public void ExpressionForms_All_CountMatchesEnumValues()
    {
        ExpressionForms.All.Should().HaveCount(Enum.GetValues<ExpressionFormKind>().Length);
    }

    // ── PostfixOperation shape ────────────────────────────────────────────────

    [Fact]
    public void ExpressionForms_PostfixOperation_IsLeftDenotation()
    {
        ExpressionForms.GetMeta(ExpressionFormKind.PostfixOperation).IsLeftDenotation
            .Should().BeTrue(because: "PostfixOperation extends an existing left operand");
    }

    [Fact]
    public void ExpressionForms_PostfixOperation_LeadTokenIsIs()
    {
        ExpressionForms.GetMeta(ExpressionFormKind.PostfixOperation).LeadTokens
            .Should().ContainSingle()
            .Which.Should().Be(TokenKind.Is);
    }

    [Fact]
    public void ExpressionForms_PostfixOperation_CategoryIsComposite()
    {
        ExpressionForms.GetMeta(ExpressionFormKind.PostfixOperation).Category
            .Should().Be(ExpressionCategory.Composite);
    }
}
