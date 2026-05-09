using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Language;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class DomainsTool
{
    [McpServerTool(Name = "precept_domains")]
    [Description("Return the Precept domain catalog: ISO 4217 currencies, curated UCUM tier-1 units (150 entries), UCUM SI prefixes, and named physical dimensions. Call when working with money, quantity, price, or temporal fields.")]
    public static DomainsDto Domains()
    {
        var baseDomains = LanguageTool.Language().Domains;

        var prefixes = UcumPrefixCatalog.All.Values
            .OrderBy(p => p.Order)
            .Select(p => new UcumPrefixDto(
                p.Code,
                p.Name,
                p.Factor.Numerator.ToString(),
                p.Factor.Denominator.ToString(),
                p.Factor.Base10Exponent))
            .ToArray();

        return new(
            baseDomains.Currencies,
            baseDomains.UcumTier1Units,
            prefixes,
            baseDomains.Dimensions
        );
    }
}
