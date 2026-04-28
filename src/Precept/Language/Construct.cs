using System.Collections.Immutable;

namespace Precept.Language;

/// <summary>
/// Metadata for a grammar construct / declaration shape.
/// </summary>
public sealed record ConstructMeta(
    ConstructKind                        Kind,
    string                               Name,
    string                               Description,
    string                               UsageExample,
    /// <summary>
    /// Semantic contexts in which this construct is valid. Empty = valid at the top level.
    /// This describes <em>semantic</em> scoping (e.g., <c>state ensure</c> is only valid
    /// after a <c>state</c> declaration) — not syntactic indentation or block nesting.
    /// In Precept's flat line-oriented layout, "nesting" is always semantic, never visual.
    /// </summary>
    ConstructKind[]                      AllowedIn,
    IReadOnlyList<ConstructSlot>         Slots,
    ImmutableArray<DisambiguationEntry>  Entries,
    string?                              SnippetTemplate = null)
{
    /// <summary>Slot sequence for this construct's declaration shape.</summary>
    public IReadOnlyList<ConstructSlot> Slots { get; } = Slots;

    /// <summary>The primary (first) leading token for this construct.</summary>
    public TokenKind PrimaryLeadingToken => Entries[0].LeadingToken;

    /// <summary>Use <see cref="PrimaryLeadingToken"/> or <see cref="Entries"/> instead.</summary>
    [Obsolete("Use PrimaryLeadingToken or Entries")]
    public TokenKind LeadingToken => PrimaryLeadingToken;
}
