using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class UpdateTool
{
    private const string CompileFailureError =
        "Compilation failed. Use precept_compile to diagnose and fix errors first.";

    [McpServerTool(Name = "precept_update")]
    [Description("Apply a direct field edit to a precept instance and return the result — including any constraint violations.")]
    public static UpdateToolResult Update(
        [Description("The precept definition text")] string text,
        [Description("Current state name. Pass null for stateless precepts.")] string? currentState,
        [Description("Current instance data (field name → value)")] Dictionary<string, object?>? data = null,
        [Description("Fields to update (field name → new value)")] Dictionary<string, object?>? fields = null)
    {
        var compiled = PreceptCompiler.CompileFromText(text);
        if (compiled.Engine is null)
            return UpdateToolResult.WithError(CompileFailureError);

        var engine = compiled.Engine;
        var nativeData = JsonConvert.ToNativeDict(data);

        PreceptInstance instance;
        try
        {
            if (engine.IsStateless)
                instance = engine.CreateInstance(nativeData?.AsReadOnly());
            else if (string.IsNullOrWhiteSpace(currentState))
                return UpdateToolResult.WithError("currentState is required for stateful precepts.");
            else
                instance = engine.CreateInstance(currentState, nativeData?.AsReadOnly());
        }
        catch (Exception ex)
        {
            return UpdateToolResult.WithError($"Instance creation failed: {ex.Message}");
        }

        if (fields is null || fields.Count == 0)
            return UpdateToolResult.WithError("No fields provided to update.");

        var nativeFields = JsonConvert.ToNativeDict(fields)!;

        var updateResult = engine.Update(instance, patch =>
        {
            foreach (var kvp in nativeFields)
                patch.Set(kvp.Key, kvp.Value);
        });

        var violations = updateResult.Violations.Select(ViolationDtoMapper.Map).ToList();

        return new UpdateToolResult(
            updateResult.Outcome.ToString(),
            updateResult.UpdatedInstance is not null
                ? ToDictionary(updateResult.UpdatedInstance.InstanceData)
                : ToDictionary(instance.InstanceData),
            violations,
            null);
    }

    private static Dictionary<string, object?> ToDictionary(IReadOnlyDictionary<string, object?> data)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in data)
            result[kvp.Key] = kvp.Value;
        return result;
    }
}

public sealed record UpdateToolResult(
    string? Outcome,
    Dictionary<string, object?>? Data,
    IReadOnlyList<ViolationDto> Violations,
    string? Error)
{
    public static UpdateToolResult WithError(string message) =>
        new(null, null, [], message);
}
