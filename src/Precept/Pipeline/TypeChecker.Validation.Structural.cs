using System;
using System.Collections.Generic;
using System.Linq;
using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    /// <summary>
    /// Structural validation sub-pass: computed-field cycle detection.
    /// Reads <see cref="CheckContext.ComputedDeps"/> (populated during computed expression
    /// resolution) and <see cref="CheckContext.Fields"/>.
    /// </summary>
    private static void ValidateStructural(CheckContext ctx)
    {
        ValidateStatelessEventOnNonStatelessPrecept(ctx);
        ValidateConstructionRowStructure(ctx);

        // ── PRE0039 — ComputedFieldWithDefault ──────────────────────────────
        // A computed field (derives via `<-`) cannot also have a `default` expression.
        foreach (var field in ctx.Fields)
        {
            if (field.IsComputed && field.DefaultExpression is not null)
            {
                ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.ComputedFieldWithDefault,
                    field.Syntax.Span, field.Name));
            }
        }

        // ── PRE0027 — DuplicateArgName ──────────────────────────────────────
        // Duplicate parameter name in event arg list.
        foreach (var evt in ctx.Events)
        {
            var argNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var arg in evt.Args)
            {
                if (!argNames.Add(arg.Name))
                {
                    ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.DuplicateArgName,
                        arg.Span, arg.Name, evt.Name));
                }
            }
        }

        // ── PRE0042 — ConflictingAccessModes ────────────────────────────────
        // Two access mode declarations on the same field+state that declare
        // conflicting modes (e.g., one says readonly, the other says editable).
        var accessByFieldState = new Dictionary<(string Field, string State), List<TypedAccessMode>>();
        foreach (var am in ctx.AccessModes)
        {
            var key = (am.FieldName, am.StateName);
            if (!accessByFieldState.TryGetValue(key, out var list))
            {
                list = [];
                accessByFieldState[key] = list;
            }
            list.Add(am);
        }
        foreach (var ((fieldName, stateName), modes) in accessByFieldState)
        {
            if (modes.Count < 2) continue;
            var distinctModes = modes.Select(m => m.Mode).Distinct().ToList();
            if (distinctModes.Count > 1)
            {
                ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.ConflictingAccessModes,
                    modes[1].Syntax.Span, fieldName, stateName));
            }
        }

        // ── PRE0043 — RedundantAccessMode ───────────────────────────────────
        // Access mode declaration is redundant (field is already unconditionally
        // in that mode due to the base field declaration).
        // Only fires for the unambiguous case: writable field + editable access mode.
        foreach (var am in ctx.AccessModes)
        {
            if (am.Guard is not null) continue; // guarded modes are conditional, not redundant

            if (!ctx.FieldLookup.TryGetValue(am.FieldName, out var field)) continue;

            // A field declared `writable` defaults to editable in all states;
            // an explicit `editable` access mode for that field is redundant.
            if (field.IsWritable && am.Mode == ModifierKind.Write)
            {
                ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.RedundantAccessMode,
                    am.Syntax.Span, "editable", am.FieldName, am.StateName));
            }
        }

        // ── Computed field cycle detection (DFS) ──────────────────────────
        // Build adjacency list from ComputedDeps: fieldName → set of dependent field names.
        // O(n) construction, O(n) DFS traversal.
        if (ctx.ComputedDeps.Count > 0)
        {
            var adjacency = new Dictionary<string, List<string>>(ctx.ComputedDeps.Count);
            foreach (var dep in ctx.ComputedDeps)
            {
                if (!adjacency.TryGetValue(dep.FieldName, out var deps))
                {
                    deps = [];
                    adjacency[dep.FieldName] = deps;
                }
                deps.AddRange(dep.DependsOn);
            }

            // DFS with three-color marking: white (unvisited), gray (in stack), black (done)
            var white = new HashSet<string>(adjacency.Keys);
            var gray = new HashSet<string>();
            var black = new HashSet<string>();

            foreach (var startNode in adjacency.Keys)
            {
                if (!white.Contains(startNode)) continue;
                DetectCycles(startNode, adjacency, white, gray, black, [], ctx);
            }
        }

    }

    private static void ValidateStatelessEventOnNonStatelessPrecept(CheckContext ctx)
    {
        // ── PRE0092 — EventHandlerInStatefulPrecept ─────────────────────────
        // Event handlers are the stateless-precept equivalent of transition rows.
        // Using them in a stateful precept creates ambiguous execution semantics.
        // Construction rows are exempt because they are the stateful construction lane.
        if (ctx.States.Count == 0 || ctx.EventHandlers.Count == 0)
            return;

        foreach (var handler in ctx.EventHandlers)
        {
            if (handler.IsConstruction)
                continue;

            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.EventHandlerInStatefulPrecept,
                GetEventNameSpan(handler.Syntax, handler.Syntax.Span), handler.EventName));
        }
    }

    private static void ValidateConstructionRowStructure(CheckContext ctx)
    {
        if (ctx.States.Count == 0)
            return;

        var constructionRows = ctx.EventHandlers
            .Where(handler => handler.IsConstruction)
            .ToList();

        var initialStateNames = ctx.States
            .Where(state => state.Modifiers.Contains(ModifierKind.InitialState))
            .Select(state => state.Name)
            .ToHashSet(StringComparer.Ordinal);

        if (constructionRows.Count == 0)
        {
            foreach (var initialEvent in ctx.Events.Where(evt => evt.IsInitial))
            {
                bool hasLegacyInitialPath = ctx.TransitionRows.Any(row =>
                    string.Equals(row.EventName, initialEvent.Name, StringComparison.Ordinal) &&
                    (row.FromState is null || initialStateNames.Contains(row.FromState)));

                if (!hasLegacyInitialPath)
                {
                    ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.ZeroConstructionRows,
                        initialEvent.NameSpan,
                        initialEvent.Name));
                }
            }
        }

        var constructionEventNames = new List<string>();
        var constructionEventNameSet = new HashSet<string>(StringComparer.Ordinal);
        SourceSpan secondInitialEventSpan = ctx.States[0].NameSpan;

        foreach (var row in constructionRows)
        {
            if (string.IsNullOrWhiteSpace(row.EventName))
                continue;

            if (!constructionEventNameSet.Add(row.EventName))
                continue;

            constructionEventNames.Add(row.EventName);
            if (constructionEventNames.Count == 2)
                secondInitialEventSpan = GetEventNameSpan(row.Syntax, row.Syntax.Span);
        }

        foreach (var row in ctx.TransitionRows)
        {
            if (!constructionEventNameSet.Contains(row.EventName))
                continue;

            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InitialEventInTransitionRow,
                GetEventNameSpan(row.Syntax, row.RowSpan), row.EventName));
        }

        if (constructionEventNames.Count > 1)
        {
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.MultipleInitialEvents,
                secondInitialEventSpan,
                constructionEventNames[0],
                constructionEventNames[1]));
        }
    }

    private static SourceSpan GetEventNameSpan(ParsedConstruct syntax, SourceSpan fallbackSpan)
    {
        var eventTarget = syntax.GetSlot<EventTargetSlot>(ConstructSlotKind.EventTarget);
        return eventTarget?.NameSpan ?? fallbackSpan;
    }

    /// <summary>
    /// DFS cycle detection helper. Walks the adjacency graph using three-color marking.
    /// On back-edge detection (gray → gray), emits <see cref="DiagnosticCode.CircularComputedField"/>.
    /// </summary>
    private static void DetectCycles(
        string node,
        Dictionary<string, List<string>> adjacency,
        HashSet<string> white,
        HashSet<string> gray,
        HashSet<string> black,
        List<string> path,
        CheckContext ctx)
    {
        white.Remove(node);
        gray.Add(node);
        path.Add(node);

        if (adjacency.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (gray.Contains(neighbor))
                {
                    // Back edge → cycle. Build cycle description from path.
                    int cycleStart = path.IndexOf(neighbor);
                    var cycle = string.Join(" → ", path.Skip(cycleStart)) + " → " + neighbor;
                    var field = ctx.FieldLookup[neighbor];
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CircularComputedField, field.Syntax.Span,
                            neighbor, cycle));
                }
                else if (white.Contains(neighbor))
                {
                    DetectCycles(neighbor, adjacency, white, gray, black, path, ctx);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        gray.Remove(node);
        black.Add(node);
    }
}
