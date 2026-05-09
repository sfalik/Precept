using System;
using System.Collections.Frozen;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class CurrencyCatalogSyncTests
{
    private const string ResourceName = "Precept.Data.Iso4217.list-one.xml";
    private static readonly StringComparer CodeComparer = StringComparer.OrdinalIgnoreCase;

    // Codes present in ISO 4217 XML but intentionally excluded from CurrencyCatalog.
    // XAU/XAG/XPT/XPD: precious metals — commodities, not currencies in business workflows.
    // XTS: reserved for testing purposes by ISO 4217.
    // XXX: "no currency" placeholder.
    // BOV/CHE/CHW/CLF/COU/MXV/USN/UYI/UYW/VED/XAD/XCG/ZWG: fund codes and supranational
    //   accounting units — not transactional currencies used in business workflows.
    private static readonly FrozenSet<string> IntentionalExclusions =
        new[]
        {
            "XAU", "XAG", "XPT", "XPD", "XTS", "XXX",
            "BOV", "CHE", "CHW", "CLF", "COU", "MXV",
            "USN", "UYI", "UYW", "VED", "XAD", "XCG", "ZWG"
        }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void CurrencyCatalog_MatchesEmbeddedIso4217Xml()
    {
        var xmlCodes = LoadXmlCurrencyCodes();
        var catalogCodes = CurrencyCatalog.All.Keys.ToFrozenSet(CodeComparer);

        var xmlCodesNotInCatalog = xmlCodes
            .Except(catalogCodes, CodeComparer)
            .Except(IntentionalExclusions, CodeComparer)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        var catalogCodesNotInXml = catalogCodes
            .Except(xmlCodes, CodeComparer)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        xmlCodesNotInCatalog.Should().BeEmpty(
            "these ISO 4217 codes are in the embedded XML but missing from the catalog: {0}",
            string.Join(", ", xmlCodesNotInCatalog));

        catalogCodesNotInXml.Should().BeEmpty(
            "these catalog codes are not in the embedded ISO 4217 XML: {0}",
            string.Join(", ", catalogCodesNotInXml));
    }

    private static FrozenSet<string> LoadXmlCurrencyCodes()
    {
        using var stream = typeof(CurrencyCatalog).Assembly.GetManifestResourceStream(ResourceName);
        stream.Should().NotBeNull($"embedded resource '{ResourceName}' should exist");

        var document = XDocument.Load(stream!);

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "CcyNtry")
            .Select(entry => entry.Elements().FirstOrDefault(child => child.Name.LocalName == "Ccy")?.Value)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!.Trim())
            .ToFrozenSet(CodeComparer);
    }
}
