using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StateMachine.Dsl;
using System.Collections.Concurrent;

namespace StateMachine.Dsl.LanguageServer;

internal sealed class SmPreviewHandler : IJsonRpcRequestHandler<SmPreviewRequest, SmPreviewResponse>
{
    private static readonly ConcurrentDictionary<string, PreviewSession> Sessions = new(StringComparer.Ordinal);

    public Task<SmPreviewResponse> Handle(SmPreviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Action))
                return Task.FromResult(new SmPreviewResponse(false, Error: "Missing action."));

            var action = request.Action.Trim().ToLowerInvariant();
            return action switch
            {
                "snapshot" => Task.FromResult(HandleSnapshot(request)),
                "fire" => Task.FromResult(HandleFire(request)),
                "reset" => Task.FromResult(HandleReset(request)),
                "replay" => Task.FromResult(HandleReplay(request)),
                "inspect" => Task.FromResult(HandleInspect(request)),
                _ => Task.FromResult(new SmPreviewResponse(false, Error: $"Unknown action '{request.Action}'."))
            };
        }
        catch (Exception ex)
        {
            return Task.FromResult(new SmPreviewResponse(false, Error: ex.Message));
        }
    }

    private static SmPreviewResponse HandleSnapshot(SmPreviewRequest request)
    {
        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new SmPreviewResponse(false, Error: sessionOrError.Error);

        return new SmPreviewResponse(true, Snapshot: BuildSnapshot(sessionOrError.Session));
    }

    private static SmPreviewResponse HandleInspect(SmPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventName))
            return new SmPreviewResponse(false, Error: "Missing event name for inspect action.");

        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new SmPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var coercedArgs = session.Engine.CoerceEventArguments(request.EventName!, request.Args);
        var inspect = session.Engine.Inspect(session.Instance, request.EventName!, coercedArgs);
        var evt = session.Engine.Events.FirstOrDefault(e => string.Equals(e.Name, request.EventName, StringComparison.Ordinal));
        var args = (evt?.Args ?? Array.Empty<DslEventArg>())
            .Select(arg => new SmPreviewEventArg(arg.Name, arg.Type.ToString().ToLowerInvariant(), arg.IsNullable, arg.HasDefaultValue, arg.DefaultValue))
            .ToArray();

        var outcome = inspect.Outcome switch
        {
            DslOutcomeKind.Accepted => "enabled",
            DslOutcomeKind.AcceptedInPlace => "noTransition",
            DslOutcomeKind.Rejected => "blocked",
            DslOutcomeKind.NotApplicable => "notApplicable",
            _ => "undefined"
        };

        var eventStatus = new SmPreviewEventStatus(
            request.EventName!,
            outcome,
            inspect.TargetState,
            inspect.Reasons,
            args);

        return new SmPreviewResponse(true, InspectResult: eventStatus);
    }

    private static SmPreviewResponse HandleFire(SmPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventName))
            return new SmPreviewResponse(false, Error: "Missing event name for fire action.");

        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new SmPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var coercedArgs = session.Engine.CoerceEventArguments(request.EventName!, request.Args);
        var fire = session.Engine.Fire(session.Instance, request.EventName!, coercedArgs);
        if (fire.UpdatedInstance is not null)
            session.Instance = fire.UpdatedInstance;

        if (fire.Outcome is not (DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace))
        {
            var reason = fire.Reasons.FirstOrDefault() ?? $"Event '{request.EventName}' did not fire.";
            return new SmPreviewResponse(false, Error: reason, Errors: fire.Reasons, Snapshot: BuildSnapshot(session));
        }

        return new SmPreviewResponse(true, Snapshot: BuildSnapshot(session));
    }

    private static SmPreviewResponse HandleReset(SmPreviewRequest request)
    {
        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new SmPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        session.Instance = session.Engine.CreateInstance();
        return new SmPreviewResponse(true, Snapshot: BuildSnapshot(session));
    }

    private static SmPreviewResponse HandleReplay(SmPreviewRequest request)
    {
        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new SmPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var messages = new List<string>();

        foreach (var step in request.Steps ?? Array.Empty<SmPreviewReplayStep>())
        {
            var coercedStepArgs = session.Engine.CoerceEventArguments(step.EventName, step.Args);
            var fire = session.Engine.Fire(session.Instance, step.EventName, coercedStepArgs);
            if ((fire.Outcome is DslOutcomeKind.Accepted or DslOutcomeKind.AcceptedInPlace) && fire.UpdatedInstance is not null)
            {
                session.Instance = fire.UpdatedInstance;
                messages.Add($"{step.EventName}: {fire.PreviousState} -> {fire.NewState}");
                continue;
            }

            var reason = fire.Reasons.FirstOrDefault() ?? "blocked/undefined";
            messages.Add($"{step.EventName}: {reason}");
            return new SmPreviewResponse(false, Error: reason, Snapshot: BuildSnapshot(session), ReplayMessages: messages);
        }

        return new SmPreviewResponse(true, Snapshot: BuildSnapshot(session), ReplayMessages: messages);
    }

    private static SessionResult EnsureSession(SmPreviewRequest request)
    {
        var key = request.Uri.ToString();
        var text = ResolveText(request);
        if (string.IsNullOrWhiteSpace(text))
            return SessionResult.Fail("No source text available for preview.");

        SmTextDocumentSyncHandler.SharedAnalyzer.SetDocumentText(request.Uri, text);

        DslWorkflowModel model;
        DslWorkflowEngine engine;
        try
        {
            model = DslWorkflowParser.Parse(text);
            engine = DslWorkflowCompiler.Compile(model);
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

    private static PreviewSession CreateSession(DocumentUri uri, string sourceText, DslWorkflowModel model, DslWorkflowEngine engine)
    {
        var instance = engine.CreateInstance();
        return new PreviewSession(uri, sourceText, model, engine, instance);
    }

    private static PreviewSession UpdateSession(
        DocumentUri uri,
        PreviewSession existing,
        string sourceText,
        DslWorkflowModel model,
        DslWorkflowEngine engine)
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

    private static SmPreviewSnapshot BuildSnapshot(PreviewSession session)
    {
        var inspectionResult = session.Engine.Inspect(session.Instance);

        var events = inspectionResult.Events
            .Select(inspect =>
            {
                var evt = session.Engine.Events.FirstOrDefault(e => string.Equals(e.Name, inspect.EventName, StringComparison.Ordinal));
                var args = (evt?.Args ?? Array.Empty<DslEventArg>())
                    .Select(arg => new SmPreviewEventArg(arg.Name, arg.Type.ToString().ToLowerInvariant(), arg.IsNullable, arg.HasDefaultValue, arg.DefaultValue))
                    .ToArray();

                var outcome = inspect.Outcome switch
                {
                    DslOutcomeKind.Accepted => "enabled",
                    DslOutcomeKind.AcceptedInPlace => "noTransition",
                    DslOutcomeKind.Rejected => "blocked",
                    _ => "undefined"
                };

                return new SmPreviewEventStatus(inspect.EventName, outcome, inspect.TargetState, inspect.Reasons, args);
            })
            .ToArray();

        var transitions = session.Model.Transitions
            .SelectMany(t => t.Clauses.Select(clause => new SmPreviewTransition(
                t.FromState,
                clause.Outcome is DslStateTransition st ? st.TargetState : t.FromState,
                t.EventName,
                clause.Predicate ?? t.Predicate,
                clause.Outcome switch
                {
                    DslStateTransition => "transition",
                    DslNoTransition => "noTransition",
                    DslRejection => "reject",
                    _ => "transition"
                })))
            .ToArray();

        var diagnostics = SmTextDocumentSyncHandler.SharedAnalyzer.GetDiagnostics(session.Uri)
            .Select(d => new SmPreviewDiagnostic(
                d.Severity?.ToString() ?? "Info",
                d.Message,
                (int)d.Range.Start.Line,
                (int)d.Range.Start.Character))
            .ToArray();

        var ruleDefinitions = new List<SmPreviewRuleInfo>();
        foreach (var field in session.Engine.Fields.Where(f => f.Rules is not null))
            foreach (var rule in field.Rules!)
                ruleDefinitions.Add(new SmPreviewRuleInfo($"field:{field.Name}", rule.ExpressionText, rule.Reason));
        foreach (var field in session.Engine.CollectionFields.Where(f => f.Rules is not null))
            foreach (var rule in field.Rules!)
                ruleDefinitions.Add(new SmPreviewRuleInfo($"field:{field.Name}", rule.ExpressionText, rule.Reason));
        foreach (var rule in session.Model.TopLevelRules ?? Array.Empty<DslRule>())
            ruleDefinitions.Add(new SmPreviewRuleInfo("topLevel", rule.ExpressionText, rule.Reason));
        foreach (var state in session.Model.States.Where(s => s.Rules is not null && s.Rules.Count > 0))
            foreach (var rule in state.Rules!)
                ruleDefinitions.Add(new SmPreviewRuleInfo($"state:{state.Name}", rule.ExpressionText, rule.Reason));
        foreach (var evt in session.Engine.Events.Where(e => e.Rules is not null && e.Rules.Count > 0))
            foreach (var rule in evt.Rules!)
                ruleDefinitions.Add(new SmPreviewRuleInfo($"event:{evt.Name}", rule.ExpressionText, rule.Reason));

        return new SmPreviewSnapshot(
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

    private static string ResolveText(SmPreviewRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Text))
            return request.Text;

        return SmTextDocumentSyncHandler.SharedAnalyzer.TryGetDocumentText(request.Uri, out var text)
            ? text
            : string.Empty;
    }

    private sealed class PreviewSession
    {
        public PreviewSession(DocumentUri uri, string sourceText, DslWorkflowModel model, DslWorkflowEngine engine, DslWorkflowInstance instance)
        {
            Uri = uri;
            SourceText = sourceText;
            Model = model;
            Engine = engine;
            Instance = instance;
        }

        public DocumentUri Uri { get; set; }
        public string SourceText { get; set; }
        public DslWorkflowModel Model { get; set; }
        public DslWorkflowEngine Engine { get; set; }
        public DslWorkflowInstance Instance { get; set; }
    }

    private sealed record SessionResult(bool Success, string? Error, PreviewSession? Session)
    {
        public static SessionResult Ok(PreviewSession session) => new(true, null, session);
        public static SessionResult Fail(string error) => new(false, error, null);
    }

}
