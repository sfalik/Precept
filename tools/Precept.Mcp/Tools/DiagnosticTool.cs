using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Language;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class DiagnosticTool
{
    [McpServerTool(Name = "precept_diagnostic")]
    [Description("Look up a Precept diagnostic by its code name (e.g., 'UndeclaredField') or PRE-prefixed numeric code (e.g., 'PRE0017'). Returns trigger condition, recovery steps, and before/after fix examples.")]
    public static DiagnosticLookupResultDto Diagnostic(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new(false, null, "Code must be a non-empty string such as 'UndeclaredField' or 'PRE0017'.");

        // Try lookup by code name (case-insensitive)
        var byName = Diagnostics.All
            .FirstOrDefault(d => string.Equals(d.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

        if (byName is not null)
            return new(true, MapDiagnostic(byName), null);

        // Try lookup by PRE#### numeric code
        var trimmed = code.Trim();
        if (trimmed.StartsWith("PRE", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(trimmed[3..], out var numericValue)
            && Enum.IsDefined(typeof(DiagnosticCode), numericValue))
        {
            var enumCode = (DiagnosticCode)numericValue;
            var byNumber = Diagnostics.GetMeta(enumCode);
            return new(true, MapDiagnostic(byNumber), null);
        }

        return new(false, null, $"No diagnostic found for code '{code}'. " +
            "Use a code name (e.g., 'UndeclaredField') or PRE-prefixed number (e.g., 'PRE0017'). " +
            "Call precept_language to browse all diagnostic codes.");
    }

    private static DiagnosticCatalogEntryDto MapDiagnostic(DiagnosticMeta d)
        => new(
            d.Code,
            d.Stage.ToString(),
            d.Severity.ToString(),
            d.Category.ToString(),
            d.MessageTemplate,
            d.RelatedCodes?.Select(c => c.ToString()).ToArray() ?? [],
            d.FixHint,
            d.PreventsFault?.ToString(),
            d.SuggestionSources?.Select(s => s.ToString()).ToArray() ?? [],
            d.TriggerCondition,
            d.RecoverySteps ?? [],
            d.ExampleBefore,
            d.ExampleAfter
        );
}
