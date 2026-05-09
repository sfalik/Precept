namespace Precept.Language;

/// <summary>
/// Marks an abstract record as a catalog discriminated union (DU).
///
/// All concrete sealed subtypes must have explicit arms in any type-pattern switch expression
/// over this type. Wildcard and discard arms (<c>_ =></c>) are prohibited: they silently
/// swallow new subtypes added to the hierarchy, bypassing exhaustiveness enforcement.
///
/// Apply this attribute to the abstract base of any DU whose concrete subtypes must be
/// exhaustively covered in downstream switches. Enforced by Roslyn analyzer
/// <c>PRECEPT0025</c> at Error severity.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CatalogDUAttribute : Attribute { }
