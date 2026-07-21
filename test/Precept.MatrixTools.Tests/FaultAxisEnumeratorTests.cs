using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Pins the catalog-declared proof-requirement site list. The total and the per-catalog
/// split are the denominator the obligation-discharge matrix's fault family is measured
/// against, so a change to either must be a deliberate edit to this test, not a silent
/// drift: adding a requirement to a catalog entry widens the family, and removing one
/// narrows what the definition has to cover.
/// </summary>
public class FaultAxisEnumeratorTests
{
    [Fact]
    public void SiteCountAndPerCatalogSplitArePinned()
    {
        var sites = FaultAxisEnumerator.Enumerate();

        sites.Should().HaveCount(101);

        sites.GroupBy(s => s.CatalogSource)
            .ToDictionary(g => g.Key, g => g.Count())
            .Should().BeEquivalentTo(new Dictionary<string, int>
            {
                ["Operations.cs"] = 74,
                ["Functions.cs"] = 2,
                ["Actions.cs"] = 7,
                ["Types.cs"] = 18,
            });
    }

    [Fact]
    public void PerRequirementKindSplitIsPinned()
    {
        var byKind = FaultAxisEnumerator.Enumerate()
            .GroupBy(s => s.RequirementKind)
            .ToDictionary(g => g.Key, g => g.Count());

        byKind.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["Numeric"] = 49,
            ["QualifierCompatibility"] = 32,
            ["IndexBounds"] = 5,
            ["QualifierChain"] = 5,
            ["Dimension"] = 4,
            ["Modifier"] = 4,
            ["DimensionalProduct"] = 1,
            ["KeyPresence"] = 1,
        });
    }

    /// <summary>
    /// Five of the thirteen requirement kinds are never declared by a catalog entry — every
    /// instance of them is constructed in pipeline code instead. That is the exact gap a
    /// catalog walk cannot close, so it is pinned here rather than left to be rediscovered.
    /// </summary>
    [Fact]
    public void KindsWithNoCatalogDeclarationSiteArePinned()
    {
        var declared = FaultAxisEnumerator.Enumerate()
            .Select(s => s.RequirementKind)
            .ToHashSet();

        Enum.GetValues<ProofRequirementKind>()
            .Select(k => k.ToString())
            .Where(k => !declared.Contains(k))
            .Should().BeEquivalentTo(
            [
                "Presence",
                "IntervalContainment",
                "LengthContainment",
                "CountContainment",
                "AssignmentQualifier",
            ]);
    }

    [Fact]
    public void DivideByZeroOnIntegerDivisionIsAnOperationSite()
    {
        var site = FaultAxisEnumerator.Enumerate()
            .Single(s => s.SiteId == "op/IntegerDivideInteger/numeric-0");

        site.CatalogSource.Should().Be("Operations.cs");
        site.RequirementKind.Should().Be("Numeric");
        site.Condition.Should().Be("Divisor must be non-zero");
        site.ApplicableTypeFamilies.Should().Equal("Integer", "Integer");
        // Both operand slots of a same-type entry share one ParameterMeta instance, so the
        // catalog cannot say which operand by object identity — the note must say so.
        site.Notes.Should().Contain("share one ParameterMeta instance");
    }

    [Fact]
    public void SqrtNonNegativityIsAFunctionSiteOnItsOnlyOverload()
    {
        var site = FaultAxisEnumerator.Enumerate()
            .Single(s => s.SiteId == "fn/Sqrt/overload-0/numeric-0");

        site.CatalogSource.Should().Be("Functions.cs");
        site.Condition.Should().Be("Argument must be non-negative");
        site.Subject.Should().Be("value");
    }

    [Fact]
    public void PowExponentRequirementIsScopedToTheIntegerOverload()
    {
        var sites = FaultAxisEnumerator.Enumerate()
            .Where(s => s.SiteId.StartsWith("fn/Pow/"))
            .ToArray();

        sites.Should().ContainSingle();
        sites[0].SiteId.Should().Be("fn/Pow/overload-0/numeric-0");
        sites[0].Subject.Should().Be("exp");
        sites[0].Notes.Should().Contain("overload 0 of 3");
    }

    [Fact]
    public void ListAtCarriesBothNonEmptinessAndIndexBounds()
    {
        var sites = FaultAxisEnumerator.Enumerate()
            .Where(s => s.SiteId.StartsWith("accessor/List/at/"))
            .ToArray();

        sites.Select(s => s.RequirementKind).Should().Equal("Numeric", "IndexBounds");
        sites[1].Subject.Should().Be("index");
        sites[1].Notes.Should().Contain("StrictlyBefore");
    }

    [Fact]
    public void RemoveAtCarriesBothNonEmptinessAndIndexBounds()
    {
        var sites = FaultAxisEnumerator.Enumerate()
            .Where(s => s.SiteId.StartsWith("action/RemoveAt/"))
            .ToArray();

        sites.Select(s => s.RequirementKind).Should().Equal("Numeric", "IndexBounds");
        sites.Should().OnlyContain(s => s.CatalogSource == "Actions.cs");
    }

    /// <summary>
    /// The uniqueness requirement on <c>append F Expr by P</c> is the only key-presence site
    /// any catalog declares, and it is an absence requirement (the key must NOT already exist).
    /// </summary>
    [Fact]
    public void AppendByUniquenessIsTheOnlyKeyPresenceSite()
    {
        var site = FaultAxisEnumerator.Enumerate()
            .Single(s => s.RequirementKind == "KeyPresence");

        site.SiteId.Should().Be("action/AppendBy/keypresence-0");
        site.Notes.Should().Contain("ABSENT");
    }

    [Fact]
    public void EverySiteCarriesANonEmptyConditionAndNote()
    {
        FaultAxisEnumerator.Enumerate().Should().OnlyContain(
            s => !string.IsNullOrWhiteSpace(s.Condition)
              && !string.IsNullOrWhiteSpace(s.Notes)
              && !string.IsNullOrWhiteSpace(s.Subject));
    }

    /// <summary>
    /// Every subject must resolve against the declaring entry's own parameter list. An
    /// unresolvable subject would mean the catalog's ParamSubject object-identity contract
    /// (ProofRequirement.cs, enforced by analyzer PRECEPT0005) was broken for that entry.
    /// </summary>
    [Fact]
    public void NoSubjectFailsToResolve()
    {
        FaultAxisEnumerator.Enumerate()
            .Where(s => s.Subject.Contains("not resolvable") || s.Subject.Contains("not rendered"))
            .Should().BeEmpty();
    }

    [Fact]
    public void SiteIdsAreUnique()
    {
        var sites = FaultAxisEnumerator.Enumerate();
        sites.Select(s => s.SiteId).Distinct().Should().HaveCount(sites.Length);
    }
}
