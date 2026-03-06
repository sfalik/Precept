using System.Collections.Generic;
using Superpower;

namespace Precept;

/// <summary>
/// Tier 2: Construct Catalog — parser combinators register syntax templates,
/// descriptions, and working examples. Core infrastructure used by parser error
/// messages, language server hovers/completions, and future MCP serialization.
/// </summary>
public sealed record ConstructInfo(
    string Name,
    string Form,
    string Context,
    string Description,
    string Example);

public static class ConstructCatalog
{
    private static readonly List<ConstructInfo> _constructs = [];
    public static IReadOnlyList<ConstructInfo> Constructs => _constructs;

    /// <summary>
    /// Registers a construct with the catalog and returns the parser unchanged.
    /// Usage: <c>parser.Register(new ConstructInfo(...))</c>
    /// </summary>
    public static TokenListParser<PreceptToken, T> Register<T>(
        this TokenListParser<PreceptToken, T> parser, ConstructInfo info)
    {
        _constructs.Add(info);
        return parser;
    }
}
