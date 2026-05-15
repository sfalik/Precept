namespace Precept.Language;

public sealed record UcumParsedUnit(
    string SourceText,
    string CanonicalCode,
    DimensionVector Vector,
    UcumExactFactor Scale,
    decimal? AffineOffset,
    string? PreferredDimensionAlias,
    IReadOnlyList<UcumAtom> UsedAtoms,
    IReadOnlyList<string> Annotations);
