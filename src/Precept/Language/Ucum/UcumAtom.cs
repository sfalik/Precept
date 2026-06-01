namespace Precept.Language;

/// <param name="ScaleIsRational">
/// Whether <see cref="Scale"/> (the unit's scale relative to its dimension's base)
/// is an exact rational suitable for amount-conversion. <c>false</c> for units whose
/// scale-to-base is irrational: the plane-angle family (<c>rad</c>/<c>deg</c>/<c>gon</c>/
/// <c>'</c>/<c>''</c>), whose reduction passes through <c>[pi]</c> and is stored only as
/// a rational <em>approximation</em>; and logarithmic units (<c>B</c>/<c>Np</c>, and the
/// <c>dB</c> they back), defined by a <c>lg</c>/<c>ln</c> UCUM function rather than a
/// multiplicative scale. <c>true</c> for everything else — including the affine units
/// <c>Cel</c>/<c>[degF]</c>, whose amount-conversion scale (e.g. <c>5/9</c>) is an exact
/// rational; the affine <see cref="AffineOffset"/> is a separate absolute-reading concern.
/// </param>
public sealed record UcumAtom(
    string Code,
    string Name,
    DimensionVector Vector,
    UcumExactFactor Scale,
    bool Prefixable,
    string? AnnotationClass,
    string? PrintSymbol = null,
    decimal? AffineOffset = null,
    bool ScaleIsRational = true);
