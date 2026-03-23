using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class SchemaTool
{
    [McpServerTool(Name = "precept_schema")]
    [Description("Return the full structure of a precept as typed JSON — states, fields, events with their args, and the transition table.")]
    public static SchemaResult Run(
        [Description("Path to the .precept file")] string path)
    {
        if (!File.Exists(path))
            return SchemaResult.WithError($"File not found: {path}");

        var text = File.ReadAllText(path);
        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(text);

        if (model is null || diagnostics.Count > 0)
        {
            return SchemaResult.WithError(
                string.Join("; ", diagnostics.Select(d => d.Message)));
        }

        var states = model.States
            .Select(s => new StateDto(s.Name, GetStateRules(model, s.Name)))
            .ToList();

        var fields = model.Fields
            .Select(f => new FieldDto(f.Name, f.Type.ToString().ToLowerInvariant(), f.IsNullable, FormatDefault(f)))
            .ToList();

        var collectionFields = model.CollectionFields
            .Select(cf => new CollectionFieldDto(cf.Name, cf.CollectionKind.ToString().ToLowerInvariant(), cf.InnerType.ToString().ToLowerInvariant()))
            .ToList();

        var events = model.Events
            .Select(e => new EventDto(e.Name,
                e.Args.Select(a => new EventArgDto(a.Name, a.Type.ToString().ToLowerInvariant(), a.IsNullable, !a.HasDefaultValue && !a.IsNullable)).ToList()))
            .ToList();

        var transitions = (model.TransitionRows ?? [])
            .GroupBy(r => (r.FromState, r.EventName))
            .Select(g => new TransitionDto(
                g.Key.FromState,
                g.Key.EventName,
                g.Select(SummarizeBranch).ToList()))
            .ToList();

        return new SchemaResult(model.Name, model.InitialState.Name, states, fields, collectionFields, events, transitions, null);
    }

    private static List<string> GetStateRules(PreceptDefinition model, string stateName)
    {
        if (model.StateAsserts is null) return [];
        return model.StateAsserts
            .Where(sa => string.Equals(sa.State, stateName, StringComparison.Ordinal))
            .Select(sa => sa.Reason)
            .ToList();
    }

    private static object? FormatDefault(PreceptField f)
    {
        if (f.HasDefaultValue) return f.DefaultValue;
        if (f.IsNullable) return null;
        return null;
    }

    private static string SummarizeBranch(PreceptTransitionRow row)
    {
        var guard = row.WhenText is not null ? $"if {row.WhenText}" : "else";
        var outcome = row.Outcome switch
        {
            StateTransition t => $"transition {t.TargetState}",
            Rejection r => r.Reason is not null ? $"reject \"{r.Reason}\"" : "reject",
            NoTransition => "no transition",
            _ => "unknown"
        };

        // For unguarded rows, just show the outcome
        if (row.WhenText is null)
            return $"→ {outcome}";

        return $"{guard} → {outcome}";
    }
}

public sealed record SchemaResult(
    string? Name,
    string? InitialState,
    IReadOnlyList<StateDto>? States,
    IReadOnlyList<FieldDto>? Fields,
    IReadOnlyList<CollectionFieldDto>? CollectionFields,
    IReadOnlyList<EventDto>? Events,
    IReadOnlyList<TransitionDto>? Transitions,
    string? Error)
{
    public static SchemaResult WithError(string message) =>
        new(null, null, null, null, null, null, null, message);
}

public sealed record StateDto(string Name, IReadOnlyList<string> Rules);
public sealed record FieldDto(string Name, string Type, bool Nullable, object? Default);
public sealed record CollectionFieldDto(string Name, string Kind, string InnerType);
public sealed record EventDto(string Name, IReadOnlyList<EventArgDto> Args);
public sealed record EventArgDto(string Name, string Type, bool Nullable, bool Required);
public sealed record TransitionDto(string From, string On, IReadOnlyList<string> Branches);
