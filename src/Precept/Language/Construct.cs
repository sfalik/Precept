namespace Precept.Language;

/// <summary>
/// Metadata for a grammar construct / declaration shape.
/// <c>Variants</c> and <c>ConstructSlot</c> arrays are deferred until
/// the grammar-generation plumbing is implemented.
/// </summary>
public sealed record ConstructMeta(
    ConstructKind   Kind,
    string          Name,
    string          Description,
    string          Example,
    ConstructKind[] AllowedIn);
