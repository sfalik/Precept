using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class InspectTool
{
    private const string CompileFailureError =
        "Compilation failed. Use precept_compile to diagnose and fix errors first.";

    [McpServerTool(Name = "precept_inspect")]
    [Description("From a given state and data, evaluate all declared events and report what each would do — without mutating anything.")]
    public static InspectResult Inspect(
        [Description("The precept definition text")] string text,
        [Description("Current state name. Pass null for stateless precepts.")] string? currentState,
        [Description("Current instance data (field name → value)")] Dictionary<string, object?>? data = null,
        [Description("Optional event arguments to use during evaluation (event name → { arg name → value })")] Dictionary<string, Dictionary<string, object?>>? eventArgs = null)
    {
        var compiled = PreceptCompiler.CompileFromText(text);
        if (compiled.Engine is null)
            return InspectResult.WithError(CompileFailureError);

        var engine = compiled.Engine;
        var nativeData = JsonConvert.ToNativeDict(data);

        PreceptInstance instance;
        try
        {
            if (engine.IsStateless)
                instance = engine.CreateInstance(nativeData?.AsReadOnly());
            else if (string.IsNullOrWhiteSpace(currentState))
                return InspectResult.WithError("currentState is required for stateful precepts.");
            else
                instance = engine.CreateInstance(currentState, nativeData?.AsReadOnly());
        }
        catch (Exception ex)
        {
            return InspectResult.WithError($"Instance creation failed: {ex.Message}");
        }

        // Use the engine's state-level Inspect — it returns events in declaration order
        var inspectionResult = engine.Inspect(instance);

        // If eventArgs were supplied, re-inspect those specific events with args
        var eventResults = new List<InspectEventDto>();
        foreach (var evtResult in inspectionResult.Events)
        {
            var suppliedArgs = eventArgs is not null && eventArgs.TryGetValue(evtResult.EventName, out var args)
                ? args
                : null;

            if (suppliedArgs is not null)
            {
                // Re-inspect this event with the supplied args
                var nativeArgs = JsonConvert.ToNativeDict(suppliedArgs)?.AsReadOnly();
                var withArgs = engine.Inspect(instance, evtResult.EventName, nativeArgs);
                eventResults.Add(MapEventInspection(withArgs));
            }
            else
            {
                eventResults.Add(MapEventInspection(evtResult));
            }
        }

        // Project editableFields from InspectionResult
        var editableFields = inspectionResult.EditableFields?.Select(f =>
            new EditableFieldDto(f.FieldName, f.FieldType, f.IsNullable, f.CurrentValue)).ToList();

        return new InspectResult(
            instance.CurrentState,
            ToDictionary(instance.InstanceData),
            eventResults,
            editableFields,
            null);
    }

    private static InspectEventDto MapEventInspection(EventInspectionResult r)
    {
        var resultState = r.IsSuccess ? (r.TargetState ?? r.CurrentState) : null;
        var violations = r.Violations.Select(ViolationDtoMapper.Map).ToList();
        var requiredArgs = r.RequiredEventArgumentKeys.Count > 0
            ? r.RequiredEventArgumentKeys
            : null;

        return new InspectEventDto(r.EventName, r.Outcome.ToString(), resultState, violations, requiredArgs);
    }

    private static Dictionary<string, object?> ToDictionary(IReadOnlyDictionary<string, object?> data)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in data)
            result[kvp.Key] = kvp.Value;
        return result;
    }
}

public sealed record InspectResult(
    string? CurrentState,
    Dictionary<string, object?>? Data,
    IReadOnlyList<InspectEventDto> Events,
    IReadOnlyList<EditableFieldDto>? EditableFields,
    string? Error)
{
    public static InspectResult WithError(string message) =>
        new(null, null, [], null, message);
}

public sealed record InspectEventDto(
    string Event,
    string? Outcome,
    string? ResultState,
    IReadOnlyList<ViolationDto> Violations,
    IReadOnlyList<string>? RequiredArgs);

public sealed record EditableFieldDto(
    string Name,
    string Type,
    bool Nullable,
    object? CurrentValue);
