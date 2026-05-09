using System.Collections.Frozen;

namespace Precept.Language;

public static class UcumCatalog
{
    // Compatibility shim: new code should use UcumAtomCatalog for catalog data and UcumParser for parsing.
    public static FrozenDictionary<string, UcumAtom> All => UcumAtomCatalog.All;

    public static UcumParseResult Parse(string expression) => UcumParser.Parse(expression);

    public static bool IsValid(string expression) => UcumParser.Parse(expression).IsValid;

    public static IReadOnlyList<UcumAtom> BrowseTier1() => UcumAtomCatalog.BrowseTier1();

    public static UcumAtom? LookupAtom(string code) =>
        UcumAtomCatalog.All.TryGetValue(code, out var atom) ? atom : null;
}
