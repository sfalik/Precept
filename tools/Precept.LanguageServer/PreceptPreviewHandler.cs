using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;
using System.Collections.Concurrent;

namespace Precept.LanguageServer;

internal sealed class PreceptPreviewHandler : IJsonRpcRequestHandler<PreceptPreviewRequest, PreceptPreviewResponse>
{
    private static readonly ConcurrentDictionary<string, PreviewSession> Sessions = new(StringComparer.Ordinal);

    public Task<PreceptPreviewResponse> Handle(PreceptPreviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Action))
                return Task.FromResult(new PreceptPreviewResponse(false, Error: "Missing action."));

            var action = request.Action.Trim().ToLowerInvariant();
            return action switch
            {
                "snapshot" => Task.FromResult(HandleSnapshot(request)),
                "fire" => Task.FromResult(HandleFire(request)),
                "reset" => Task.FromResult(HandleReset(request)),
                "replay" => Task.FromResult(HandleReplay(request)),
                "inspect" => Task.FromResult(HandleInspect(request)),
                "inspectupdate" => Task.FromResult(HandleInspectUpdate(request)),
                "update" => Task.FromResult(HandleUpdate(request)),
                _ => Task.FromResult(new PreceptPreviewResponse(false, Error: $"Unknown action '{request.Action}'."))
            };
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PreceptPreviewResponse(false, Error: ex.Message));
        }
    }

    private static PreceptPreviewResponse HandleSnapshot(PreceptPreviewRequest request)
    {
        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new PreceptPreviewResponse(false, Error: sessionOrError.Error);

        return new PreceptPreviewResponse(true, Snapshot: BuildSnapshot(sessionOrError.Session));
    }

    private static PreceptPreviewResponse HandleInspect(PreceptPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventName))
            return new PreceptPreviewResponse(false, Error: "Missing event name for inspect action.");

        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new PreceptPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var coercedArgs = session.Engine.CoerceEventArguments(request.EventName!, request.Args);
        var inspect = session.Engine.Inspect(session.Instance, request.EventName!, coercedArgs);
        var evt = session.Engine.Events.FirstOrDefault(e => string.Equals(e.Name, request.EventName, StringComparison.Ordinal));
        var args = (evt?.Args ?? Array.Empty<PreceptEventArg>())
            .Select(arg => new PreceptPreviewEventArg(arg.Name, arg.Type.ToString().ToLowerInvariant(), arg.IsNullable, arg.HasDefaultValue, arg.DefaultValue))
            .ToArray();

        var outcome = inspect.Outcome switch
        {
            TransitionOutcome.Transition => "enabled",
            TransitionOutcome.NoTransition => "noTransition",
            TransitionOutcome.ConstraintFailure => "blocked",
            TransitionOutcome.Rejected => "blocked",
            TransitionOutcome.Unmatched => "notApplicable",
            _ => "undefined"
        };

        var eventStatus = new PreceptPreviewEventStatus(
            request.EventName!,
            outcome,
            inspect.TargetState,
            inspect.Reasons,
            args);

        return new PreceptPreviewResponse(true, InspectResult: eventStatus);
    }

    private static PreceptPreviewResponse HandleFire(PreceptPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventName))
            return new PreceptPreviewResponse(false, Error: "Missing event name for fire action.");

        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new PreceptPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var coercedArgs = session.Engine.CoerceEventArguments(request.EventName!, request.Args);
        var fire = session.Engine.Fire(session.Instance, request.EventName!, coercedArgs);
        if (fire.UpdatedInstance is not null)
            session.Instance = fire.UpdatedInstance;

        if (fire.Outcome is not (TransitionOutcome.Transition or TransitionOutcome.NoTransition))
        {
            var reason = fire.Reasons.FirstOrDefault() ?? $"Event '{request.EventName}' did not fire.";
            return new PreceptPreviewResponse(false, Error: reason, Errors: fire.Reasons, Snapshot: BuildSnapshot(session));
        }

        return new PreceptPreviewResponse(true, Snapshot: BuildSnapshot(session));
    }

    private static PreceptPreviewResponse HandleUpdate(PreceptPreviewRequest request)
    {
        if (request.FieldUpdates is null || request.FieldUpdates.Count == 0)
            return new PreceptPreviewResponse(false, Error: "Missing field updates for update action.");

        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new PreceptPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var result = session.Engine.Update(session.Instance, builder =>
        {
            foreach (var (fieldName, value) in request.FieldUpdates)
                builder.Set(fieldName, UnwrapJToken(value));
        });

        if (result.Outcome != UpdateOutcome.Update || result.UpdatedInstance is null)
        {
            var reason = result.Reasons.FirstOrDefault() ?? "Update failed.";
            return new PreceptPreviewResponse(false, Error: reason, Errors: result.Reasons, Snapshot: BuildSnapshot(session));
        }

        session.Instance = result.UpdatedInstance;
        return new PreceptPreviewResponse(true, Snapshot: BuildSnapshot(session));
    }

    private static PreceptPreviewResponse HandleInspectUpdate(PreceptPreviewRequest request)
    {
        if (request.FieldUpdates is null)
            return new PreceptPreviewResponse(false, Error: "Missing field updates for inspectUpdate action.");

        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new PreceptPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var normalizedPatch = request.FieldUpdates.ToDictionary(
            kvp => kvp.Key,
            kvp => UnwrapJToken(kvp.Value),
            StringComparer.Ordinal);

        var inspect = InspectDraftPatch(session, normalizedPatch);
        var editable = MapEditableFields(inspect.EditableFields);
        var fullErrors = ExtractEditableViolations(editable);
        var fieldErrors = BuildFieldErrors(session, normalizedPatch, fullErrors);
        var attributedErrors = fieldErrors is null
            ? Array.Empty<string>()
            : fieldErrors.Values.SelectMany(static errs => errs).Distinct(StringComparer.Ordinal).ToArray();
        var formErrors = fullErrors.Where(err => !attributedErrors.Contains(err, StringComparer.Ordinal)).ToArray();
        var attributedEditable = ApplyAttributedViolations(editable, fieldErrors);

        return new PreceptPreviewResponse(
            true,
            EditableFields: attributedEditable,
            Errors: fullErrors.Length > 0 ? fullErrors : null,
            FieldErrors: fieldErrors,
            FormErrors: formErrors.Length > 0 ? formErrors : null,
            CanSave: fullErrors.Length == 0);
    }

    private static PreceptPreviewResponse HandleReset(PreceptPreviewRequest request)
    {
        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new PreceptPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        session.Instance = session.Engine.CreateInstance();
        return new PreceptPreviewResponse(true, Snapshot: BuildSnapshot(session));
    }

    private static PreceptPreviewResponse HandleReplay(PreceptPreviewRequest request)
    {
        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new PreceptPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var messages = new List<string>();

        foreach (var step in request.Steps ?? Array.Empty<PreceptPreviewReplayStep>())
        {
            var coercedStepArgs = session.Engine.CoerceEventArguments(step.EventName, step.Args);
            var fire = session.Engine.Fire(session.Instance, step.EventName, coercedStepArgs);
            if ((fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition) && fire.UpdatedInstance is not null)
            {
                session.Instance = fire.UpdatedInstance;
                messages.Add($"{step.EventName}: {fire.PreviousState} -> {fire.NewState}");
                continue;
            }

            var reason = fire.Reasons.FirstOrDefault() ?? "blocked/undefined";
            messages.Add($"{step.EventName}: {reason}");
            return new PreceptPreviewResponse(false, Error: reason, Snapshot: BuildSnapshot(session), ReplayMessages: messages);
        }

        return new PreceptPreviewResponse(true, Snapshot: BuildSnapshot(session), ReplayMessages: messages);
    }

    private static SessionResult EnsureSession(PreceptPreviewRequest request)
    {
        var key = request.Uri.ToString();
        var text = ResolveText(request);
        if (string.IsNullOrWhiteSpace(text))
            return SessionResult.Fail("No source text available for preview.");

        PreceptTextDocumentSyncHandler.SharedAnalyzer.SetDocumentText(request.Uri, text);

        PreceptDefinition model;
        PreceptEngine engine;
        try
        {
            model = PreceptParser.Parse(text);
            engine = PreceptCompiler.Compile(model);
        }
        catch (Exception ex)
        {
            return SessionResult.Fail(ex.Message);
        }

        var session = Sessions.AddOrUpdate(
            key,
            _ => CreateSession(request.Uri, text, model, engine),
            (_, existing) => UpdateSession(request.Uri, existing, text, model, engine));

        return SessionResult.Ok(session);
    }

    private static PreviewSession CreateSession(DocumentUri uri, string sourceText, PreceptDefinition model, PreceptEngine engine)
    {
        var instance = engine.CreateInstance();
        return new PreviewSession(uri, sourceText, model, engine, instance);
    }

    private static PreviewSession UpdateSession(
        DocumentUri uri,
        PreviewSession existing,
        string sourceText,
        PreceptDefinition model,
        PreceptEngine engine)
    {
        if (string.Equals(existing.SourceText, sourceText, StringComparison.Ordinal))
            return existing;

        var compatibility = engine.CheckCompatibility(existing.Instance);
        var nextInstance = compatibility.IsCompatible
            ? existing.Instance
            : engine.CreateInstance();

        existing.SourceText = sourceText;
        existing.Uri = uri;
        existing.Model = model;
        existing.Engine = engine;
        existing.Instance = nextInstance;
        return existing;
    }

    private static PreceptPreviewSnapshot BuildSnapshot(PreviewSession session)
    {
        var inspectionResult = session.Engine.Inspect(session.Instance);

        var events = inspectionResult.Events
            .Select(inspect =>
            {
                var evt = session.Engine.Events.FirstOrDefault(e => string.Equals(e.Name, inspect.EventName, StringComparison.Ordinal));
                var args = (evt?.Args ?? Array.Empty<PreceptEventArg>())
                    .Select(arg => new PreceptPreviewEventArg(arg.Name, arg.Type.ToString().ToLowerInvariant(), arg.IsNullable, arg.HasDefaultValue, arg.DefaultValue))
                    .ToArray();

                var outcome = inspect.Outcome switch
                {
                    TransitionOutcome.Transition => "enabled",
                    TransitionOutcome.NoTransition => "noTransition",
                    TransitionOutcome.Rejected => "blocked",
                    _ => "undefined"
                };

                return new PreceptPreviewEventStatus(inspect.EventName, outcome, inspect.TargetState, inspect.Reasons, args);
            })
            .ToArray();

        var transitions = (session.Model.TransitionRows ?? Array.Empty<PreceptTransitionRow>())
            .Select(row => new PreceptPreviewTransition(
                row.FromState,
                row.Outcome is StateTransition st ? st.TargetState : row.FromState,
                row.EventName,
                row.WhenText,
                row.Outcome switch
                {
                    StateTransition => "transition",
                    NoTransition => "noTransition",
                    Rejection => "reject",
                    _ => "transition"
                }))
            .ToArray();

        var diagnostics = PreceptTextDocumentSyncHandler.SharedAnalyzer.GetDiagnostics(session.Uri)
            .Select(d => new PreceptPreviewDiagnostic(
                d.Severity?.ToString() ?? "Info",
                d.Message,
                (int)d.Range.Start.Line,
                (int)d.Range.Start.Character))
            .ToArray();

        var ruleDefinitions = new List<PreceptPreviewRuleInfo>();
        foreach (var inv in session.Model.Invariants ?? Array.Empty<PreceptInvariant>())
            ruleDefinitions.Add(new PreceptPreviewRuleInfo("invariant", inv.ExpressionText, inv.Reason));
        foreach (var sa in session.Model.StateAsserts ?? Array.Empty<StateAssertion>())
            ruleDefinitions.Add(new PreceptPreviewRuleInfo($"state:{sa.Anchor.ToString().ToLowerInvariant()}:{sa.State}", sa.ExpressionText, sa.Reason));
        foreach (var ea in session.Model.EventAsserts ?? Array.Empty<EventAssertion>())
            ruleDefinitions.Add(new PreceptPreviewRuleInfo($"event:{ea.EventName}", ea.ExpressionText, ea.Reason));

        return new PreceptPreviewSnapshot(
            session.Engine.Name,
            session.Instance.CurrentState,
            session.Engine.States,
            transitions,
            events,
            new Dictionary<string, object?>(session.Instance.InstanceData, StringComparer.Ordinal),
            diagnostics,
            null,
            ruleDefinitions.Count > 0 ? ruleDefinitions : null,
            MapEditableFields(inspectionResult.EditableFields));
    }

    private static IReadOnlyList<PreceptPreviewEditableField>? MapEditableFields(IReadOnlyList<PreceptEditableFieldInfo>? editableFields)
        => editableFields?.Select(ef => new PreceptPreviewEditableField(
            ef.FieldName,
            ef.FieldType,
            ef.IsNullable,
            ef.CurrentValue,
            ef.Violation)).ToArray();

    private static InspectionResult InspectDraftPatch(PreviewSession session, IReadOnlyDictionary<string, object?> patch)
        => session.Engine.Inspect(session.Instance, builder =>
        {
            foreach (var (fieldName, value) in patch)
                builder.Set(fieldName, value);
        });

    private static string[] ExtractEditableViolations(IReadOnlyList<PreceptPreviewEditableField>? editable)
        => editable?
            .Where(e => !string.IsNullOrWhiteSpace(e.Violation))
            .Select(e => e.Violation!)
            .Distinct(StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();

    private static IReadOnlyList<PreceptPreviewEditableField>? ApplyAttributedViolations(
        IReadOnlyList<PreceptPreviewEditableField>? editable,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? fieldErrors)
    {
        if (editable is null)
            return null;

        if (fieldErrors is null || fieldErrors.Count == 0)
            return editable.Select(static field => field with { Violation = null }).ToArray();

        return editable.Select(field =>
        {
            if (!fieldErrors.TryGetValue(field.FieldName, out var errors) || errors.Count == 0)
                return field with { Violation = null };

            return field with { Violation = string.Join(" · ", errors) };
        }).ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>>? BuildFieldErrors(
        PreviewSession session,
        IReadOnlyDictionary<string, object?> patch,
        IReadOnlyList<string> fullErrors)
    {
        if (patch.Count == 0 || fullErrors.Count == 0)
            return null;

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var fieldName in patch.Keys)
        {
            var reducedPatch = patch
                .Where(kvp => !string.Equals(kvp.Key, fieldName, StringComparison.Ordinal))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

            var reducedErrors = reducedPatch.Count == 0
                ? Array.Empty<string>()
                : ExtractEditableViolations(MapEditableFields(InspectDraftPatch(session, reducedPatch).EditableFields));

            var attributed = fullErrors
                .Where(err => !reducedErrors.Contains(err, StringComparer.Ordinal))
                .ToArray();

            if (attributed.Length > 0)
                result[fieldName] = attributed;
        }

        return result.Count > 0 ? result : null;
    }

    private static string ResolveText(PreceptPreviewRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Text))
            return request.Text;

        return PreceptTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(request.Uri, out var text)
            ? text
            : string.Empty;
    }

    private static object? UnwrapJToken(object? value)
    {
        if (value is JValue jv)
            return jv.Type switch
            {
                JTokenType.String => jv.Value<string>(),
                JTokenType.Integer => jv.Value<double>(),
                JTokenType.Float => jv.Value<double>(),
                JTokenType.Boolean => jv.Value<bool>(),
                JTokenType.Null => null,
                _ => jv.Value<object>()
            };
        return value;
    }

    private sealed class PreviewSession
    {
        public PreviewSession(DocumentUri uri, string sourceText, PreceptDefinition model, PreceptEngine engine, PreceptInstance instance)
        {
            Uri = uri;
            SourceText = sourceText;
            Model = model;
            Engine = engine;
            Instance = instance;
        }

        public DocumentUri Uri { get; set; }
        public string SourceText { get; set; }
        public PreceptDefinition Model { get; set; }
        public PreceptEngine Engine { get; set; }
        public PreceptInstance Instance { get; set; }
    }

    private sealed record SessionResult(bool Success, string? Error, PreviewSession? Session)
    {
        public static SessionResult Ok(PreviewSession session) => new(true, null, session);
        public static SessionResult Fail(string error) => new(false, error, null);
    }

}
