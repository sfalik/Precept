namespace Precept.Language;

public sealed record UcumAtom(
    string Code,
    string Name,
    DimensionVector Vector,
    UcumExactFactor Scale,
    bool Prefixable,
    string? AnnotationClass);
