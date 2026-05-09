using System;
using System.Collections.Frozen;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class CurrencyCatalogSyncTests
{
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

    // Committed snapshot at src/Precept/Data/Iso4217/list-one.xml. Refresh with VS Code task 'iso4217: refresh'.
    internal static string Iso4217XmlPath =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Precept", "Data", "Iso4217", "list-one.xml"));

    [Fact]
    public void CurrencyCatalog_MatchesIso4217Xml()
    {
        var xmlCodes = LoadXmlCurrencyCodes(Iso4217XmlPath);
        var catalogCodes = GetCatalogCurrencyCodes();

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
            "these ISO 4217 codes are in the XML but missing from the catalog: {0}",
            string.Join(", ", xmlCodesNotInCatalog));

        catalogCodesNotInXml.Should().BeEmpty(
            "these catalog codes are not in the current ISO 4217 XML: {0}",
            string.Join(", ", catalogCodesNotInXml));
    }

    private static FrozenSet<string> GetCatalogCurrencyCodes()
    {
        if (Types.GetMeta(TypeKind.Currency).ContentValidation is ClosedSetValidation validation)
            return validation.AllowedValues;

        var field = typeof(Types).GetField("Iso4217CurrencyCodes", BindingFlags.Static | BindingFlags.NonPublic);
        var codes = field?.GetValue(null) as FrozenSet<string>;

        if (codes is not null)
            return codes;

        throw new InvalidOperationException(
            "Unable to locate the currency catalog through TypeKind.Currency metadata or Types.Iso4217CurrencyCodes.");
    }

    private static FrozenSet<string> LoadXmlCurrencyCodes(string xmlPath)
    {
        var document = XDocument.Load(xmlPath);

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "CcyNtry")
            .Select(entry => entry.Elements().FirstOrDefault(child => child.Name.LocalName == "Ccy")?.Value)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!.Trim())
            .ToFrozenSet(CodeComparer);
    }
}
