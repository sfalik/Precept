using System.Text.Json;

namespace Precept.Mcp.Tools;

internal static class JsonConvert
{
    /// <summary>
    /// Converts JsonElement values (from MCP deserialization) to native .NET types
    /// expected by the Precept runtime (string, bool, double, null).
    /// </summary>
    internal static Dictionary<string, object?>? ToNativeDict(Dictionary<string, object?>? source)
    {
        if (source is null) return null;

        var result = new Dictionary<string, object?>(source.Count, StringComparer.Ordinal);
        foreach (var kvp in source)
            result[kvp.Key] = ToNative(kvp.Value);
        return result;
    }

    internal static object? ToNative(object? value)
    {
        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.GetString(),
                JsonValueKind.Number => je.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => je.GetRawText()
            };
        }

        return value;
    }
}
