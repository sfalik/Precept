using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class UcumCatalogDriftTests
{
    private const string ResourceName = "Precept.Data.Ucum.ucum-essence.xml";

    private static readonly (string Code, DimensionVector Vector)[] RequiredUniverseAtoms =
    [
        ("m", new DimensionVector(1, 0, 0, 0, 0, 0, 0)),
        ("g", new DimensionVector(0, 1, 0, 0, 0, 0, 0)),
        ("s", new DimensionVector(0, 0, 1, 0, 0, 0, 0)),
        ("A", new DimensionVector(0, 0, 0, 1, 0, 0, 0)),
        ("K", new DimensionVector(0, 0, 0, 0, 1, 0, 0)),
        ("mol", new DimensionVector(0, 0, 0, 0, 0, 1, 0)),
        ("cd", new DimensionVector(0, 0, 0, 0, 0, 0, 1)),
        ("rad", DimensionVector.None),
        ("each", DimensionVector.None)
    ];

    private static readonly string[] RequiredTier1Codes =
    [
        "m", "km", "[in_i]", "[ft_i]", "[mi_i]", "[nmi_i]",
        "kg", "g", "[lb_av]", "[oz_av]", "[gr]", "t",
        "L", "mL", "[gal_us]", "[bbl_us]",
        "m2", "har", "[acr_us]",
        "K", "Cel", "[degF]", "[degR]",
        "J", "kW.h", "W", "[Btu_IT]",
        "Pa", "bar", "[psi]", "mm[Hg]",
        "m/s", "km/h", "[kn_i]",
        "N", "kN", "[lbf_av]",
        "1", "%", "[ppm]", "[iU]",
        "rad", "deg", "gon"
    ];

    private static readonly string[] ExcludedTier1Codes = ["s", "min", "h", "d", "mol", "[oz_tr]", "[pwt_tr]", "[oz_ap]", "[lb_ap]"];

    private static readonly (string Code, DimensionVector Vector)[] DerivedTier1Vectors =
    [
        ("m2", new DimensionVector(2, 0, 0, 0, 0, 0, 0)),
        ("km2", new DimensionVector(2, 0, 0, 0, 0, 0, 0)),
        ("m/s", new DimensionVector(1, 0, -1, 0, 0, 0, 0)),
        ("km/h", new DimensionVector(1, 0, -1, 0, 0, 0, 0)),
        ("kW.h", new DimensionVector(2, 1, -2, 0, 0, 0, 0))
    ];

    [Fact]
    public void UcumAtomCatalogAll_MatchesUniverseInvariants()
    {
        var all = UcumAtomCatalog.All;
        var xmlAtomCodes = LoadXmlAtomCodes();

        xmlAtomCodes.Count.Should().BeGreaterThanOrEqualTo(300, "the embedded UCUM essence snapshot should still contain hundreds of atom definitions");
        all.Count.Should().BeGreaterThanOrEqualTo(xmlAtomCodes.Count, "the catalog should cover the embedded UCUM XML atom universe");
        all.Keys.Should().Contain(xmlAtomCodes, "every embedded UCUM XML atom code should be exposed through UcumAtomCatalog.All");
        all.Values.Should().OnlyContain(atom => !string.IsNullOrWhiteSpace(atom.Code), "every UCUM atom should keep a non-empty code");
        all.Values.Should().OnlyContain(atom => !string.IsNullOrWhiteSpace(atom.Name), "every UCUM atom should keep a non-empty display name");
        all.Values.Select(atom => atom.Code).Distinct(StringComparer.Ordinal).Count().Should().Be(all.Count, "the UCUM atom universe should not contain duplicate codes");

        foreach (var (code, expectedVector) in RequiredUniverseAtoms)
        {
            var atom = GetRequiredAtom(all, code);
            atom.Vector.Should().Be(expectedVector, $"{code} should keep its expected UCUM dimension vector");
        }

        all["[lb_av]"].PrintSymbol.Should().Be("lb");
        all["[oz_av]"].PrintSymbol.Should().Be("oz");
        all["[gr]"].PrintSymbol.Should().BeNull();
    }

    [Fact]
    public void BrowseTier1_MatchesCuratedTier1Invariants()
    {
        var tier1 = UcumAtomCatalog.BrowseTier1();
        var tier1Codes = tier1.Select(atom => atom.Code).ToArray();

        tier1.Should().HaveCount(146);
        tier1Codes.Distinct(StringComparer.Ordinal).Count().Should().Be(146, "Tier 1 should not contain duplicate codes");

        foreach (var code in RequiredTier1Codes)
            tier1Codes.Should().Contain(code, $"Tier 1 should retain curated UCUM code '{code}'");

        foreach (var code in ExcludedTier1Codes)
            tier1Codes.Should().NotContain(code, $"Tier 1 intentionally excludes '{code}' per the UCUM curation research");
    }

    [Fact]
    public void UcumCatalog_RemainsAThinCompatibilityShim()
    {
        var atomCatalogTier1 = UcumAtomCatalog.BrowseTier1();
        var shimTier1 = UcumCatalog.BrowseTier1();
        var kilogram = UcumCatalog.LookupAtom("kg");

        UcumCatalog.All.Should().BeSameAs(UcumAtomCatalog.All);
        UcumCatalog.All.Count.Should().Be(UcumAtomCatalog.All.Count);
        shimTier1.Count.Should().Be(atomCatalogTier1.Count);
        kilogram.Should().NotBeNull();
        kilogram.Should().BeSameAs(UcumAtomCatalog.All["kg"]);
    }

    [Fact]
    public void BrowseTier1_ResolvesDerivedEntriesWithExpectedVectors()
    {
        var tier1 = UcumAtomCatalog.BrowseTier1();

        foreach (var atom in tier1)
            atom.Vector.Should().NotBeNull("every Tier 1 browse entry should carry a synthesized dimension vector");

        foreach (var (code, expectedVector) in DerivedTier1Vectors)
        {
            var atom = tier1.SingleOrDefault(candidate => candidate.Code == code);
            atom.Should().NotBeNull($"Tier 1 should contain derived UCUM expression '{code}'");
            atom!.Vector.Should().Be(expectedVector, $"Tier 1 expression '{code}' should resolve to the expected dimension vector");
        }
    }

    private static UcumAtom GetRequiredAtom(IReadOnlyDictionary<string, UcumAtom> atoms, string code)
    {
        atoms.Should().ContainKey(code);
        return atoms[code];
    }

    private static IReadOnlyCollection<string> LoadXmlAtomCodes()
    {
        using var stream = typeof(UcumAtomCatalog).Assembly.GetManifestResourceStream(ResourceName);
        stream.Should().NotBeNull($"embedded resource '{ResourceName}' should exist");

        var document = XDocument.Load(stream!);
        return document
            .Descendants()
            .Where(element => element.Name.LocalName is "base-unit" or "unit")
            .Select(element => element.Attribute("Code")?.Value)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
