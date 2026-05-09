using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class OperationsTool
{
    [McpServerTool(Name = "precept_operations")]
    [Description("Return all 198 typed operator combinations available in Precept, with an optional category filter by LHS type name (e.g., 'Money', 'Integer', 'Quantity'). Returns available categories, matching operations, and the total count.")]
    public static OperationsResultDto Operations(string? category = null)
    {
        var allOps = LanguageTool.Language().Operations;

        var categories = allOps
            .Select(op => op.LhsType)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();

        var filtered = string.IsNullOrWhiteSpace(category)
            ? allOps
            : allOps
                .Where(op => string.Equals(op.LhsType, category, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        return new(
            categories,
            filtered,
            filtered.Length,
            string.IsNullOrWhiteSpace(category) ? null : category
        );
    }
}
