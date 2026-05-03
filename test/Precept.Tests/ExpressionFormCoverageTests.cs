using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Layer 3 enforcement for ExpressionFormKind: catalog completeness.
/// </summary>
public class ExpressionFormCoverageTests
{
    // ── Catalog completeness ───────────────────────────────────────────────

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
}

