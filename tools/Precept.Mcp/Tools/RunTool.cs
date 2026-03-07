using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class RunTool
{
    [McpServerTool(Name = "precept_run")]
    [Description("Execute a sequence of events against a precept and return step-by-step outcomes.")]
    public static RunResult Run(
        [Description("Path to the .precept file")] string path,
        [Description("Initial instance data (field name → value). Uses defaults if omitted.")] Dictionary<string, object?>? initialData = null,
        [Description("Steps to execute, each with an event name and optional args.")] List<RunStep>? steps = null)
    {
        if (!File.Exists(path))
            return RunResult.WithError($"File not found: {path}");

        var text = File.ReadAllText(path);
        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(text);

        if (model is null || diagnostics.Count > 0)
        {
            return RunResult.WithError(
                string.Join("; ", diagnostics.Select(d => d.Message)));
        }

        PreceptEngine engine;
        try
        {
            engine = PreceptCompiler.Compile(model);
        }
        catch (Exception ex)
        {
            return RunResult.WithError($"Compilation failed: {ex.Message}");
        }

        var nativeData = JsonConvert.ToNativeDict(initialData);

        PreceptInstance instance;
        try
        {
            instance = engine.CreateInstance(nativeData?.AsReadOnly());
        }
        catch (Exception ex)
        {
            return RunResult.WithError($"Instance creation failed: {ex.Message}");
        }

        if (steps is null or { Count: 0 })
        {
            return new RunResult(
                [], instance.CurrentState, ToDictionary(instance.InstanceData), null, null);
        }

        var stepResults = new List<RunStepResult>();

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var args = step.Args is not null
                ? JsonConvert.ToNativeDict(step.Args)
                : null;

            var fireResult = engine.Fire(
                instance,
                step.Event,
                args is { Count: > 0 } ? new Dictionary<string, object?>(args, StringComparer.Ordinal) : null);

            var outcomeStr = fireResult.Outcome.ToString();

            stepResults.Add(new RunStepResult(
                i + 1,
                step.Event,
                outcomeStr,
                fireResult.NewState ?? fireResult.PreviousState,
                fireResult.UpdatedInstance is not null
                    ? ToDictionary(fireResult.UpdatedInstance.InstanceData)
                    : ToDictionary(instance.InstanceData),
                fireResult.Reasons.Count > 0 ? string.Join("; ", fireResult.Reasons) : null));

            if (fireResult.Outcome is PreceptOutcomeKind.Rejected
                or PreceptOutcomeKind.NotDefined
                or PreceptOutcomeKind.NotApplicable)
            {
                return new RunResult(
                    stepResults,
                    instance.CurrentState,
                    ToDictionary(instance.InstanceData),
                    i + 1,
                    null);
            }

            // Update instance for next step
            instance = fireResult.UpdatedInstance!;
        }

        return new RunResult(
            stepResults,
            instance.CurrentState,
            ToDictionary(instance.InstanceData),
            null,
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

public sealed record RunStep(string Event, Dictionary<string, object?>? Args = null);

public sealed record RunStepResult(
    int Step,
    string Event,
    string Outcome,
    string State,
    Dictionary<string, object?> Data,
    string? Reason);

public sealed record RunResult(
    IReadOnlyList<RunStepResult> Steps,
    string? FinalState,
    Dictionary<string, object?>? FinalData,
    int? AbortedAt,
    string? Error)
{
    public static RunResult WithError(string message) =>
        new([], null, null, null, message);
}
