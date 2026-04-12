using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class CompileTool
{
    [McpServerTool(Name = "precept_compile")]
    [Description("Parse, type-check, analyze, and compile a precept definition. Returns the full typed structure alongside any diagnostics.")]
    public static CompileResult Run(
        [Description("The precept definition text")] string text)
    {
        var result = PreceptCompiler.CompileFromText(text);
        var model = result.Model;

        var diagnostics = result.Diagnostics
            .Select(d => new DiagnosticDto(d.Line, d.Column, d.Message, d.Code,
                d.Severity.ToString().ToLowerInvariant()))
            .ToList();

        if (model is null)
            return CompileResult.DiagnosticsOnly(diagnostics);

        var states = model.States
            .Select(s => new StateDto(s.Name, GetStateRules(model, s.Name)))
            .ToList();

        var fields = model.Fields
            .Select(f => new FieldDto(f.Name, f.Type.ToString().ToLowerInvariant(), f.IsNullable, FormatDefault(f),
                f.Constraints?.Select(FormatConstraint).ToList(),
                f.ChoiceValues is { Count: > 0 } ? f.ChoiceValues : null,
                f.IsOrdered ? (bool?)true : null))
            .ToList();

        var collectionFields = model.CollectionFields
            .Select(cf => new CollectionFieldDto(cf.Name, cf.CollectionKind.ToString().ToLowerInvariant(), cf.InnerType.ToString().ToLowerInvariant(),
                cf.Constraints?.Select(FormatConstraint).ToList()))
            .ToList();

        var events = model.Events
            .Select(e => new EventDto(e.Name,
                e.Args.Select(a => new EventArgDto(a.Name, a.Type.ToString().ToLowerInvariant(), a.IsNullable, !a.HasDefaultValue && !a.IsNullable,
                    a.Constraints?.Select(FormatConstraint).ToList(),
                    a.ChoiceValues is { Count: > 0 } ? a.ChoiceValues : null,
                    a.IsOrdered ? (bool?)true : null)).ToList()))
            .ToList();

        var transitions = (model.TransitionRows ?? [])
            .GroupBy(r => (r.FromState, r.EventName))
            .Select(g => new TransitionDto(
                g.Key.FromState,
                g.Key.EventName,
                g.Select(MapBranch).ToList()))
            .ToList();

        var invariants = (model.Invariants ?? [])
            .Where(inv => !inv.IsSynthetic)
            .Select(inv => new InvariantDto(inv.ExpressionText, inv.WhenText, inv.Reason, inv.SourceLine, inv.IsSynthetic))
            .ToList();

        var stateAsserts = (model.StateAsserts ?? [])
            .Select(sa => new StateAssertDto(sa.Anchor.ToString().ToLowerInvariant(), sa.State, sa.ExpressionText, sa.WhenText, sa.Reason, sa.SourceLine))
            .ToList();

        var eventAsserts = (model.EventAsserts ?? [])
            .Select(ea => new EventAssertDto(ea.EventName, ea.ExpressionText, ea.WhenText, ea.Reason, ea.SourceLine))
            .ToList();

        var editBlocks = (model.EditBlocks ?? [])
            .Select(eb => new EditBlockDto(eb.State, eb.WhenText, eb.FieldNames, eb.SourceLine))
            .ToList();

        return new CompileResult(
            !result.HasErrors,
            model.IsStateless,
            model.Name,
            model.InitialState?.Name,
            model.States.Count,
            model.Events.Count,
            states,
            fields,
            collectionFields,
            events,
            transitions,
            invariants,
            stateAsserts,
            eventAsserts,
            editBlocks,
            diagnostics);
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
        return null;
    }

    private static string FormatConstraint(FieldConstraint c) => c switch
    {
        FieldConstraint.Nonnegative => "nonnegative",
        FieldConstraint.Positive => "positive",
        FieldConstraint.Notempty => "notempty",
        FieldConstraint.Min m => $"min {m.Value}",
        FieldConstraint.Max m => $"max {m.Value}",
        FieldConstraint.Minlength m => $"minlength {m.Value}",
        FieldConstraint.Maxlength m => $"maxlength {m.Value}",
        FieldConstraint.Mincount m => $"mincount {m.Value}",
        FieldConstraint.Maxcount m => $"maxcount {m.Value}",
        FieldConstraint.Maxplaces m => $"maxplaces {m.Places}",
        _ => c.GetType().Name.ToLowerInvariant()
    };

    private static BranchDto MapBranch(PreceptTransitionRow row)
    {
        var (outcome, target, reason) = row.Outcome switch
        {
            StateTransition t => ("transition", (string?)t.TargetState, (string?)null),
            Rejection r => ("reject", (string?)null, r.Reason),
            NoTransition => ("no-transition", (string?)null, (string?)null),
            _ => ("unknown", (string?)null, (string?)null)
        };

        return new BranchDto(row.WhenText, outcome, target, reason);
    }
}

// ── Compile result DTOs (inline — sole consumer) ──────────────

public sealed record CompileResult(
    bool Valid,
    bool IsStateless,
    string? Name,
    string? InitialState,
    int StateCount,
    int EventCount,
    IReadOnlyList<StateDto>? States,
    IReadOnlyList<FieldDto>? Fields,
    IReadOnlyList<CollectionFieldDto>? CollectionFields,
    IReadOnlyList<EventDto>? Events,
    IReadOnlyList<TransitionDto>? Transitions,
    IReadOnlyList<InvariantDto>? Invariants,
    IReadOnlyList<StateAssertDto>? StateAsserts,
    IReadOnlyList<EventAssertDto>? EventAsserts,
    IReadOnlyList<EditBlockDto>? EditBlocks,
    IReadOnlyList<DiagnosticDto> Diagnostics)
{
    public static CompileResult DiagnosticsOnly(IReadOnlyList<DiagnosticDto> diagnostics) =>
        new(false, false, null, null, 0, 0, null, null, null, null, null, null, null, null, null, diagnostics);
}

public sealed record DiagnosticDto(int Line, int Column, string Message, string? Code, string Severity);

public sealed record StateDto(string Name, IReadOnlyList<string> Rules);
public sealed record FieldDto(string Name, string Type, bool Nullable, object? Default,
    IReadOnlyList<string>? Constraints = null,
    IReadOnlyList<string>? ChoiceValues = null,
    bool? IsOrdered = null);
public sealed record CollectionFieldDto(string Name, string Kind, string InnerType, IReadOnlyList<string>? Constraints = null);
public sealed record EventDto(string Name, IReadOnlyList<EventArgDto> Args);
public sealed record EventArgDto(string Name, string Type, bool Nullable, bool Required,
    IReadOnlyList<string>? Constraints = null,
    IReadOnlyList<string>? ChoiceValues = null,
    bool? IsOrdered = null);
public sealed record TransitionDto(string From, string On, IReadOnlyList<BranchDto> Branches);
public sealed record BranchDto(string? Guard, string Outcome, string? Target, string? Reason);
public sealed record InvariantDto(string Expression, string? When, string Reason, int Line, bool IsSynthetic);
public sealed record StateAssertDto(string Anchor, string State, string Expression, string? When, string Reason, int Line);
public sealed record EventAssertDto(string Event, string Expression, string? When, string Reason, int Line);
public sealed record EditBlockDto(string? State, string? When, IReadOnlyList<string> Fields, int Line);
