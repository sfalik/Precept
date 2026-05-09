using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

namespace Precept.Language;

public sealed record CurrencyEntry(
    string AlphaCode,
    int NumericCode,
    string Name,
    int MinorUnit,
    string Symbol
);

public static class CurrencyCatalog
{
    private const string ResourceName = "Precept.Data.Iso4217.list-one.xml";

    private static readonly FrozenSet<string> ExcludedCodes =
        new[]
        {
            "XAU", "XAG", "XPT", "XPD", "XTS", "XXX",
            "BOV", "CHE", "CHW", "CLF", "COU", "MXV",
            "USN", "UYI", "UYW", "VED", "XAD", "XCG", "ZWG"
        }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> Symbols =
        new Dictionary<string, string>
        {
            ["AED"] = "د.إ",  ["AFN"] = "؋",    ["ALL"] = "L",
            ["AMD"] = "֏",    ["ARS"] = "$",    ["AUD"] = "A$",
            ["AZN"] = "₼",    ["BAM"] = "KM",   ["BBD"] = "Bds$",
            ["BDT"] = "৳",    ["BGN"] = "лв",   ["BHD"] = ".د.ب",
            ["BMD"] = "$",    ["BND"] = "B$",   ["BOB"] = "Bs.",
            ["BRL"] = "R$",   ["BSD"] = "B$",   ["BTN"] = "Nu.",
            ["BWP"] = "P",    ["BYN"] = "Br",   ["BZD"] = "BZ$",
            ["CAD"] = "C$",   ["CDF"] = "FC",   ["CHF"] = "CHF",
            ["CLP"] = "$",    ["CNY"] = "¥",    ["COP"] = "$",
            ["CRC"] = "₡",    ["CUP"] = "₱",    ["CZK"] = "Kč",
            ["DKK"] = "kr",   ["DOP"] = "RD$",  ["DZD"] = "د.ج",
            ["EGP"] = "E£",   ["ERN"] = "Nfk",  ["ETB"] = "Br",
            ["EUR"] = "€",    ["FJD"] = "FJ$",  ["FKP"] = "£",
            ["GBP"] = "£",    ["GEL"] = "₾",    ["GHS"] = "GH₵",
            ["GIP"] = "£",    ["GTQ"] = "Q",    ["GYD"] = "G$",
            ["HKD"] = "HK$",  ["HNL"] = "L",    ["HUF"] = "Ft",
            ["IDR"] = "Rp",   ["ILS"] = "₪",    ["INR"] = "₹",
            ["IQD"] = "ع.د",  ["IRR"] = "﷼",    ["ISK"] = "kr",
            ["JMD"] = "J$",   ["JOD"] = "JD",   ["JPY"] = "¥",
            ["KES"] = "KSh",  ["KGS"] = "сом",  ["KHR"] = "៛",
            ["KPW"] = "₩",    ["KRW"] = "₩",    ["KWD"] = "د.ك",
            ["KYD"] = "CI$",  ["KZT"] = "₸",    ["LAK"] = "₭",
            ["LBP"] = "L£",   ["LKR"] = "Rs",   ["LRD"] = "L$",
            ["MAD"] = "MAD",  ["MDL"] = "L",    ["MGA"] = "Ar",
            ["MKD"] = "ден",  ["MMK"] = "K",    ["MNT"] = "₮",
            ["MOP"] = "MOP$", ["MRU"] = "UM",   ["MUR"] = "₨",
            ["MVR"] = "Rf",   ["MWK"] = "MK",   ["MXN"] = "Mex$",
            ["MYR"] = "RM",   ["MZN"] = "MT",   ["NAD"] = "N$",
            ["NGN"] = "₦",    ["NIO"] = "C$",   ["NOK"] = "kr",
            ["NPR"] = "Rs",   ["NZD"] = "NZ$",  ["OMR"] = "ر.ع.",
            ["PAB"] = "B/.",  ["PEN"] = "S/.",  ["PGK"] = "K",
            ["PHP"] = "₱",    ["PKR"] = "₨",    ["PLN"] = "zł",
            ["PYG"] = "₲",    ["QAR"] = "ر.ق",  ["RON"] = "lei",
            ["RSD"] = "din.", ["RUB"] = "₽",    ["RWF"] = "FRw",
            ["SAR"] = "ر.س",  ["SBD"] = "SI$",  ["SCR"] = "SRe",
            ["SDG"] = "ج.س.", ["SEK"] = "kr",   ["SGD"] = "S$",
            ["SHP"] = "£",    ["SLE"] = "Le",   ["SOS"] = "Sh",
            ["SRD"] = "SRD",  ["SSP"] = "SS£",  ["STN"] = "Db",
            ["SVC"] = "₡",    ["SYP"] = "S£",   ["SZL"] = "E",
            ["THB"] = "฿",    ["TJS"] = "SM",   ["TMT"] = "T",
            ["TND"] = "د.ت",  ["TOP"] = "T$",   ["TRY"] = "₺",
            ["TTD"] = "TT$",  ["TWD"] = "NT$",  ["TZS"] = "TSh",
            ["UAH"] = "₴",    ["UGX"] = "USh",  ["USD"] = "$",
            ["UYU"] = "$U",   ["UZS"] = "сўм",  ["VES"] = "Bs.S",
            ["VND"] = "₫",    ["VUV"] = "VT",   ["WST"] = "WS$",
            ["XAF"] = "FCFA", ["XCD"] = "EC$",  ["XOF"] = "CFA",
            ["XPF"] = "₣",    ["YER"] = "﷼",    ["ZAR"] = "R",
            ["ZMW"] = "ZK",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<FrozenDictionary<string, CurrencyEntry>> _lazy = new(Load);

    public static FrozenDictionary<string, CurrencyEntry> All => _lazy.Value;

    private static FrozenDictionary<string, CurrencyEntry> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");

        var document = XDocument.Load(stream);
        var entries = new Dictionary<string, CurrencyEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in document.Descendants().Where(element => element.Name.LocalName == "CcyNtry"))
        {
            var alphaCode = GetChildValue(entry, "Ccy");
            if (string.IsNullOrWhiteSpace(alphaCode) || ExcludedCodes.Contains(alphaCode))
                continue;

            if (entries.ContainsKey(alphaCode))
                continue;

            var numericCode = ParseNumericCode(alphaCode, GetChildValue(entry, "CcyNbr"));
            var name = GetChildValue(entry, "CcyNm")
                ?? throw new InvalidOperationException($"Currency '{alphaCode}' is missing CcyNm.");
            var minorUnit = ParseMinorUnit(GetChildValue(entry, "CcyMnrUnts"));
            var symbol = Symbols.GetValueOrDefault(alphaCode, alphaCode);

            entries.Add(alphaCode, new CurrencyEntry(alphaCode, numericCode, name, minorUnit, symbol));
        }

        return entries.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetChildValue(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(child => child.Name.LocalName == localName)?.Value.Trim();

    private static int ParseNumericCode(string alphaCode, string? rawText)
    {
        if (int.TryParse(rawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericCode))
            return numericCode;

        throw new InvalidOperationException($"Currency '{alphaCode}' has invalid numeric code '{rawText ?? "<null>"}'.");
    }

    private static int ParseMinorUnit(string? rawText) =>
        int.TryParse(rawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minorUnit)
            ? minorUnit
            : -1;
}
