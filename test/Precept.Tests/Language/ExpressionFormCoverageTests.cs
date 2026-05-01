using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

/// <summary>
/// Layer 2 catalog-coverage assertions for ExpressionFormKind.
/// Validates ExpressionForms metadata shape: count, GetMeta completeness,
/// HoverDocs population, IsLeftDenotation correctness, and LeadTokens contract.
/// </summary>
public class ExpressionFormCoverageTests
{
    // ── Completeness ──────────────────────────────────────────────────────────

    [Fact]
    public void ExpressionForms_All_HasExpectedCount()
    {
        ExpressionForms.All.Count.Should().Be(11);
    }

    // ── Per-kind GetMeta exhaustiveness ──────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllFormKindData))]
    public void ExpressionForms_GetMeta_DoesNotThrow(ExpressionFormKind kind)
    {
        // Verifies PRECEPT0007 enforcement: GetMeta handles every catalog member
        var meta = ExpressionForms.GetMeta(kind);
        meta.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(AllFormKindData))]
    public void ExpressionForms_HoverDocs_NonEmpty(ExpressionFormKind kind)
    {
        var meta = ExpressionForms.GetMeta(kind);
        meta.HoverDocs.Should().NotBeNullOrWhiteSpace(
            $"form {kind} must have non-empty HoverDocs");
    }

    // ── IsLeftDenotation ─────────────────────────────────────────────────────

    [Fact]
    public void ExpressionForms_LedForms_IsLeftDenotation_IsTrue()
    {
        var ledForms = new[]
        {
            ExpressionFormKind.BinaryOperation,
            ExpressionFormKind.MemberAccess,
            ExpressionFormKind.MethodCall,
            ExpressionFormKind.PostfixOperation
        };
        foreach (var kind in ledForms)
        {
            ExpressionForms.GetMeta(kind).IsLeftDenotation.Should().BeTrue(
                $"form {kind} is a led (left-denotation) form");
        }
    }

    [Fact]
    public void ExpressionForms_NudForms_IsLeftDenotation_IsFalse()
    {
        var nudForms = new[]
        {
            ExpressionFormKind.Literal,
            ExpressionFormKind.Identifier,
            ExpressionFormKind.Grouped,
            ExpressionFormKind.UnaryOperation,
            ExpressionFormKind.Conditional,
            ExpressionFormKind.FunctionCall,
            ExpressionFormKind.ListLiteral
        };
        foreach (var kind in nudForms)
        {
            ExpressionForms.GetMeta(kind).IsLeftDenotation.Should().BeFalse(
                $"form {kind} is a nud (null-denotation) form");
        }
    }

    // ── LeadTokens contract ───────────────────────────────────────────────────

    [Fact]
    public void AllExpressionFormKinds_DeclareLeadTokens_OrAreCompositeInfix()
    {
        foreach (var meta in ExpressionForms.All)
        {
            if (!meta.IsLeftDenotation)
            {
                meta.LeadTokens.Should().NotBeEmpty(
                    $"nud form {meta.Kind} must declare at least one lead token for parser dispatch");
            }
        }
    }

    public static IEnumerable<object[]> AllFormKindData =>
        Enum.GetValues<ExpressionFormKind>().Select(k => new object[] { k });
}
