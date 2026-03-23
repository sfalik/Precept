using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class InspectTool
{
    [McpServerTool(Name = "precept_inspect")]
    [Description("From a given state and data, evaluate all declared events and report what each would do — without mutating anything.")]
    public static InspectResult Run(
        [Description("Path to the .precept file")] string path,
        [Description("Current state name")] string currentState,
        [Description("Current instance data (field name → value)")] Dictionary<string, object?>? data = null,
        [Description("Optional event arguments to use during evaluation (event name → { arg name → value })")] Dictionary<string, Dictionary<string, object?>>? eventArgs = null)
    {
        if (!File.Exists(path))
            return InspectResult.WithError($"File not found: {path}");

        var text = File.ReadAllText(path);
        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(text);

        if (model is null || diagnostics.Count > 0)
        {
            return InspectResult.WithError(
                string.Join("; ", diagnostics.Select(d => d.Message)));
        }

        PreceptEngine engine;
        try
        {
            engine = PreceptCompiler.Compile(model);
        }
        catch (Exception ex)
        {
            return InspectResult.WithError($"Compilation failed: {ex.Message}");
        }

        var nativeData = JsonConvert.ToNativeDict(data);

        PreceptInstance instance;
        try
        {
            instance = engine.CreateInstance(currentState, nativeData?.AsReadOnly());
        }
        catch (Exception ex)
        {
            return InspectResult.WithError($"Instance creation failed: {ex.Message}");
        }

        var eventResults = new List<InspectEventDto>();

        foreach (var evt in engine.Events)
        {
            // Check if caller supplied args for this event
            var suppliedArgs = eventArgs is not null && eventArgs.TryGetValue(evt.Name, out var args)
                ? args
                : null;

            // Check if event has required args that were not supplied
            var requiredMissing = evt.Args
                .Where(a => !a.HasDefaultValue && !a.IsNullable)
                .Where(a => suppliedArgs is null || !suppliedArgs.ContainsKey(a.Name))
                .ToList();

            if (requiredMissing.Count > 0 && suppliedArgs is null)
            {
                eventResults.Add(new InspectEventDto(
                    evt.Name,
                    null,
                    null,
                    null,
                    null,
                    true,
                    requiredMissing.Select(a => new RequiredArgDto(a.Name, a.Type.ToString().ToLowerInvariant())).ToList(),
                    "Supply args via eventArgs to see the full outcome"));
                continue;
            }

            // Fire in read-only mode using a snapshot
            var snapshot = engine.CreateInstance(currentState, nativeData?.AsReadOnly());
            var argDict = suppliedArgs is not null
                ? JsonConvert.ToNativeDict(suppliedArgs)
                : null;

            var fireResult = engine.Fire(
                snapshot,
                evt.Name,
                argDict is { Count: > 0 } ? new Dictionary<string, object?>(argDict, StringComparer.Ordinal) : null);

            var dto = new InspectEventDto(
                evt.Name,
                fireResult.Outcome.ToString(),
                fireResult.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition
                    ? fireResult.NewState ?? fireResult.PreviousState
                    : null,
                fireResult.UpdatedInstance is not null
                    ? ToDictionary(fireResult.UpdatedInstance.InstanceData)
                    : null,
                fireResult.Reasons.Count > 0 ? string.Join("; ", fireResult.Reasons) : null,
                false,
                null,
                null);

            eventResults.Add(dto);
        }

        // Sort: actionable first (Transition/NoTransition), then unavailable, then requiresArgs
        eventResults.Sort((a, b) =>
        {
            var orderA = GetSortOrder(a);
            var orderB = GetSortOrder(b);
            return orderA.CompareTo(orderB);
        });

        return new InspectResult(currentState, eventResults, null);
    }

    private static int GetSortOrder(InspectEventDto dto)
    {
        if (dto.RequiresArgs == true) return 3;
        return dto.Outcome switch
        {
            "Transition" or "NoTransition" => 1,
            _ => 2
        };
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
    IReadOnlyList<InspectEventDto> Events,
    string? Error)
{
    public static InspectResult WithError(string message) =>
        new(null, [], message);
}

public sealed record InspectEventDto(
    string Event,
    string? Outcome,
    string? ResultState,
    Dictionary<string, object?>? ResultData,
    string? Reason,
    bool? RequiresArgs,
    IReadOnlyList<RequiredArgDto>? RequiredArgs,
    string? Note);

public sealed record RequiredArgDto(string Name, string Type);
