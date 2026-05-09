namespace Precept.Runtime;

/// <summary>
/// ISO 4217 currency identity. Instances are interned from <c>CurrencyCatalog</c>.
/// Equality is structural (all fields); <c>ToString()</c> returns <see cref="AlphaCode"/>.
/// Validated against the currency catalog at compile time for literals and at the
/// fire/update boundary for runtime values.
/// </summary>
public sealed record Currency(
    string AlphaCode,
    int NumericCode,
    string Name,
    int MinorUnit,
    string Symbol);
