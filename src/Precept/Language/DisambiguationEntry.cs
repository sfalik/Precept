using System.Collections.Immutable;

namespace Precept.Language;

/// <summary>
/// Maps a leading token to disambiguation metadata for construct resolution.
/// When multiple constructs share a leading token (e.g. <c>in</c> leads
/// StateEnsure, AccessMode, and OmitDeclaration), the parser inspects
/// <see cref="DisambiguationTokens"/> to distinguish them.
/// </summary>
public sealed record DisambiguationEntry(
    TokenKind                  LeadingToken,
    ImmutableArray<TokenKind>? DisambiguationTokens = null,
    ConstructSlotKind?         LeadingTokenSlot = null);
