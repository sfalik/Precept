using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class FireTool
{
    private const string CompileFailureError =
        "Compilation failed. Use precept_compile to diagnose and fix errors first.";

    [McpServerTool(Name = "precept_fire")]
    [Description("Fire a single event against a precept from a given state and data snapshot. Returns the execution outcome.")]
    public static FireResult Fire(
        [Description("The precept definition text")] string text,
        [Description("Current state name. Pass null for stateless precepts.")] string? currentState,
        [Description("The event to fire")] string @event,
        [Description("Current instance data (field name → value)")] Dictionary<string, object?>? data = null,
        [Description("Optional event arguments (arg name → value)")] Dictionary<string, object?>? args = null)
    {
        var compiled = PreceptCompiler.CompileFromText(text);
        if (compiled.Engine is null)
            return FireResult.WithError(CompileFailureError);

        var engine = compiled.Engine;
        var nativeData = JsonConvert.ToNativeDict(data);

        PreceptInstance instance;
        try
        {
            if (engine.IsStateless)
                instance = engine.CreateInstance(nativeData?.AsReadOnly());
            else if (string.IsNullOrWhiteSpace(currentState))
                return FireResult.WithError("currentState is required for stateful precepts.");
            else
                instance = engine.CreateInstance(currentState, nativeData?.AsReadOnly());
        }
        catch (Exception ex)
        {
            return FireResult.WithError($"Instance creation failed: {ex.Message}");
        }

        var nativeArgs = args is not null
            ? JsonConvert.ToNativeDict(args)
            : null;

        var fireResult = engine.Fire(
            instance,
            @event,
            nativeArgs is { Count: > 0 } ? new Dictionary<string, object?>(nativeArgs, StringComparer.Ordinal) : null);

        var violations = fireResult.Violations.Select(ViolationDtoMapper.Map).ToList();

        return new FireResult(
            @event,
            fireResult.Outcome.ToString(),
            fireResult.PreviousState,
            fireResult.NewState,
            fireResult.UpdatedInstance is not null
                ? ToDictionary(fireResult.UpdatedInstance.InstanceData)
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

public sealed record FireResult(
    string? Event,
    string? Outcome,
    string? FromState,
    string? ToState,
    Dictionary<string, object?>? Data,
    IReadOnlyList<ViolationDto> Violations,
    string? Error)
{
    public static FireResult WithError(string message) =>
        new(null, null, null, null, null, [], message);
}
