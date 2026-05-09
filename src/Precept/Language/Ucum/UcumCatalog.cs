namespace Precept.Language;

public static class UcumCatalog
{
    private static readonly string[] Tier1Codes = ["kg", "m", "s", "mol", "K", "L", "N", "J", "Pa", "[degF]"];

    public static UcumParseResult Parse(string expression) => UcumParser.Parse(expression);

    public static bool IsValid(string expression) => Parse(expression).IsValid;

    public static IReadOnlyList<UcumAtom> BrowseTier1() =>
        Tier1Codes.Select(code => UcumAtomCatalog.All[code]).ToArray();

    public static UcumAtom? LookupAtom(string code) =>
        UcumAtomCatalog.All.TryGetValue(code, out var atom) ? atom : null;
}
