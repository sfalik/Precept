using System.Collections.Frozen;

namespace Precept.Language;

public static class UcumPrefixCatalog
{
    private static readonly Lazy<FrozenDictionary<string, UcumPrefix>> _lazy = new(Load);

    public static FrozenDictionary<string, UcumPrefix> All => _lazy.Value;

    public static UcumPrefix? LongestPrefixMatch(string code)
    {
        foreach (var prefix in All.Values.OrderByDescending(prefix => prefix.Code.Length).ThenBy(prefix => prefix.Order))
        {
            if (code.Length > prefix.Code.Length && code.StartsWith(prefix.Code, StringComparison.Ordinal))
                return prefix;
        }

        return null;
    }

    private static FrozenDictionary<string, UcumPrefix> Load() =>
        new[]
        {
            new UcumPrefix("Y",  "yotta", UcumExactFactor.Parse("1e24"),  0),
            new UcumPrefix("Z",  "zetta", UcumExactFactor.Parse("1e21"),  1),
            new UcumPrefix("E",  "exa",   UcumExactFactor.Parse("1e18"),  2),
            new UcumPrefix("P",  "peta",  UcumExactFactor.Parse("1e15"),  3),
            new UcumPrefix("T",  "tera",  UcumExactFactor.Parse("1e12"),  4),
            new UcumPrefix("G",  "giga",  UcumExactFactor.Parse("1e9"),   5),
            new UcumPrefix("M",  "mega",  UcumExactFactor.Parse("1e6"),   6),
            new UcumPrefix("k",  "kilo",  UcumExactFactor.Parse("1e3"),   7),
            new UcumPrefix("h",  "hecto", UcumExactFactor.Parse("1e2"),   8),
            new UcumPrefix("da", "deka",  UcumExactFactor.Parse("1e1"),   9),
            new UcumPrefix("d",  "deci",  UcumExactFactor.Parse("1e-1"), 10),
            new UcumPrefix("c",  "centi", UcumExactFactor.Parse("1e-2"), 11),
            new UcumPrefix("m",  "milli", UcumExactFactor.Parse("1e-3"), 12),
            new UcumPrefix("u",  "micro", UcumExactFactor.Parse("1e-6"), 13),
            new UcumPrefix("n",  "nano",  UcumExactFactor.Parse("1e-9"), 14),
            new UcumPrefix("p",  "pico",  UcumExactFactor.Parse("1e-12"), 15),
            new UcumPrefix("f",  "femto", UcumExactFactor.Parse("1e-15"), 16),
            new UcumPrefix("a",  "atto",  UcumExactFactor.Parse("1e-18"), 17),
            new UcumPrefix("z",  "zepto", UcumExactFactor.Parse("1e-21"), 18),
            new UcumPrefix("y",  "yocto", UcumExactFactor.Parse("1e-24"), 19),
        }
        .ToFrozenDictionary(prefix => prefix.Code, StringComparer.Ordinal);
}
