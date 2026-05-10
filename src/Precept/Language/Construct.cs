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
    RoutingFamily                        RoutingFamily,
    string?                              SnippetTemplate  = null,
    ModifierDomain                       ModifierDomain   = ModifierDomain.None,
    bool                                 SupportsPostActionEnsure = false,
    bool                                 IsOutlineNode    = false,
    string?                              OutlineSymbolTag = null)
{
    /// <summary>Slot sequence for this construct's declaration shape.</summary>
    public IReadOnlyList<ConstructSlot> Slots { get; } = Slots;

    /// <summary>The primary (first) leading token for this construct.</summary>
    public TokenKind PrimaryLeadingToken => Entries[0].LeadingToken;

    /// <summary>Use <see cref="PrimaryLeadingToken"/> or <see cref="Entries"/> instead.</summary>
    [Obsolete("Use PrimaryLeadingToken or Entries")]
    public TokenKind LeadingToken => PrimaryLeadingToken;
}

/// <summary>Identifies how the parser routes this construct.</summary>
public enum RoutingFamily
{
    /// <summary>Not set — sentinel value for default-initialization detection.</summary>
    None = 0,
    /// <summary>
    /// Parsed in the file-header preamble (Parser.cs ParseAll pre-loop), not through
    /// the standard dispatch. A duplicate 'precept' line CAN reach ParseDirectConstruct()
    /// and hits the wildcard throw — that guard must remain.
    /// </summary>
    Header,
    /// <summary>Unique leading token; routed directly by ParseDirectConstruct().</summary>
    Direct,
    /// <summary>Shares in/to/from leading token; routed via DisambiguateAndParse().</summary>
    StateScoped,
    /// <summary>Shares the 'on' leading token; routed via DisambiguateAndParse().</summary>
    EventScoped,
}

/// <summary>
/// Identifies which category of modifiers is applicable to a construct.
/// Used by the language server to derive valid modifier suggestions without
/// switching on <see cref="ConstructKind"/> values.
/// </summary>
public enum ModifierDomain
{
    /// <summary>No modifiers apply (or construct does not use a modifier slot).</summary>
    None   = 0,
    /// <summary>Field modifiers apply (e.g., nonnegative, optional, readonly).</summary>
    Field  = 1,
    /// <summary>State modifiers apply (e.g., initial, terminal).</summary>
    State  = 2,
    /// <summary>Event modifiers apply (e.g., initial).</summary>
    Event  = 3,
    /// <summary>Access mode keywords apply (editable, readonly).</summary>
    Access = 4,
    /// <summary>State entry modifiers (anchor, etc.) apply.</summary>
    Anchor = 5,
}
