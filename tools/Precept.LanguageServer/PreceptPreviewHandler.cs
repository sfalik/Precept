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
            PreceptOutcomeKind.Accepted => "enabled",
            PreceptOutcomeKind.AcceptedInPlace => "noTransition",
            PreceptOutcomeKind.Rejected => "blocked",
            PreceptOutcomeKind.NotApplicable => "notApplicable",
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

        if (fire.Outcome is not (PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace))
        {
            var reason = fire.Reasons.FirstOrDefault() ?? $"Event '{request.EventName}' did not fire.";
            return new PreceptPreviewResponse(false, Error: reason, Errors: fire.Reasons, Snapshot: BuildSnapshot(session));
        }

        return new PreceptPreviewResponse(true, Snapshot: BuildSnapshot(session));
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
            if ((fire.Outcome is PreceptOutcomeKind.Accepted or PreceptOutcomeKind.AcceptedInPlace) && fire.UpdatedInstance is not null)
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
                    PreceptOutcomeKind.Accepted => "enabled",
                    PreceptOutcomeKind.AcceptedInPlace => "noTransition",
                    PreceptOutcomeKind.Rejected => "blocked",
                    _ => "undefined"
                };

                return new PreceptPreviewEventStatus(inspect.EventName, outcome, inspect.TargetState, inspect.Reasons, args);
            })
            .ToArray();

        var transitions = session.Model.Transitions
            .SelectMany(t => t.Clauses.Select(clause => new PreceptPreviewTransition(
                t.FromState,
                clause.Outcome is PreceptStateTransition st ? st.TargetState : t.FromState,
                t.EventName,
                clause.Predicate ?? t.Predicate,
                clause.Outcome switch
                {
                    PreceptStateTransition => "transition",
                    PreceptNoTransition => "noTransition",
                    PreceptRejection => "reject",
                    _ => "transition"
                })))
            .ToArray();

        var diagnostics = PreceptTextDocumentSyncHandler.SharedAnalyzer.GetDiagnostics(session.Uri)
            .Select(d => new PreceptPreviewDiagnostic(
                d.Severity?.ToString() ?? "Info",
                d.Message,
                (int)d.Range.Start.Line,
                (int)d.Range.Start.Character))
            .ToArray();

        var ruleDefinitions = new List<PreceptPreviewRuleInfo>();
        foreach (var field in session.Engine.Fields.Where(f => f.Rules is not null))
            foreach (var rule in field.Rules!)
                ruleDefinitions.Add(new PreceptPreviewRuleInfo($"field:{field.Name}", rule.ExpressionText, rule.Reason));
        foreach (var field in session.Engine.CollectionFields.Where(f => f.Rules is not null))
            foreach (var rule in field.Rules!)
                ruleDefinitions.Add(new PreceptPreviewRuleInfo($"field:{field.Name}", rule.ExpressionText, rule.Reason));
        foreach (var rule in session.Model.TopLevelRules ?? Array.Empty<PreceptRule>())
            ruleDefinitions.Add(new PreceptPreviewRuleInfo("topLevel", rule.ExpressionText, rule.Reason));
        foreach (var state in session.Model.States.Where(s => s.Rules is not null && s.Rules.Count > 0))
            foreach (var rule in state.Rules!)
                ruleDefinitions.Add(new PreceptPreviewRuleInfo($"state:{state.Name}", rule.ExpressionText, rule.Reason));
        foreach (var evt in session.Engine.Events.Where(e => e.Rules is not null && e.Rules.Count > 0))
            foreach (var rule in evt.Rules!)
                ruleDefinitions.Add(new PreceptPreviewRuleInfo($"event:{evt.Name}", rule.ExpressionText, rule.Reason));

        return new PreceptPreviewSnapshot(
            session.Engine.Name,
            session.Instance.CurrentState,
            session.Engine.States,
            transitions,
            events,
            new Dictionary<string, object?>(session.Instance.InstanceData, StringComparer.Ordinal),
            diagnostics,
            null,
            ruleDefinitions.Count > 0 ? ruleDefinitions : null);
    }

    private static string ResolveText(PreceptPreviewRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Text))
            return request.Text;

        return PreceptTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(request.Uri, out var text)
            ? text
            : string.Empty;
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
