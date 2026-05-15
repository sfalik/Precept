using System.Collections.Frozen;

namespace Precept.Language;

public static class DimensionCatalog
{
    public sealed record DimensionAlias(string Name, DimensionVector Vector, string Description);

    private static readonly DimensionAlias[] Aliases =
    [
        new("length", new DimensionVector(1, 0, 0, 0, 0, 0, 0), "Length"),
        new("mass", new DimensionVector(0, 1, 0, 0, 0, 0, 0), "Mass"),
        new("time", new DimensionVector(0, 0, 1, 0, 0, 0, 0), "Time"),
        new("temperature", new DimensionVector(0, 0, 0, 0, 1, 0, 0), "Temperature"),
        new("volume", new DimensionVector(3, 0, 0, 0, 0, 0, 0), "Volume"),
        new("area", new DimensionVector(2, 0, 0, 0, 0, 0, 0), "Area"),
        new("speed", new DimensionVector(1, 0, -1, 0, 0, 0, 0), "Speed"),
        new("energy", new DimensionVector(2, 1, -2, 0, 0, 0, 0), "Energy"),
        new("pressure", new DimensionVector(-1, 1, -2, 0, 0, 0, 0), "Pressure"),
        new("force", new DimensionVector(1, 1, -2, 0, 0, 0, 0), "Force"),
        new("count", DimensionVector.None, "Dimensionless count"),
    ];

    /// <summary>
    /// All registered dimension aliases keyed case-insensitively by name.
    /// Mirrors <see cref="CurrencyCatalog.All"/> in shape.
    /// </summary>
    public static FrozenDictionary<string, DimensionAlias> All { get; } =
        Aliases.ToFrozenDictionary(alias => alias.Name, StringComparer.OrdinalIgnoreCase);

    public static bool TryGetAlias(DimensionVector vector, out DimensionAlias? alias)
    {
        alias = Aliases.FirstOrDefault(candidate => candidate.Vector == vector);
        return alias is not null;
    }

    public static DimensionAlias GetByName(string name) => All[name];
}
