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

    private static SmPreviewResponse HandleFire(SmPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventName))
            return new SmPreviewResponse(false, Error: "Missing event name for fire action.");

        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new SmPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        var fire = session.Definition.Fire(session.Instance, request.EventName!, request.Args);
        if (fire.UpdatedInstance is not null)
            session.Instance = fire.UpdatedInstance;

        if (!fire.IsAccepted)
        {
            var reason = fire.Reasons.FirstOrDefault() ?? $"Event '{request.EventName}' did not fire.";
            return new SmPreviewResponse(false, Error: reason, Snapshot: BuildSnapshot(session));
        }

        return new SmPreviewResponse(true, Snapshot: BuildSnapshot(session));
    }

    private static SmPreviewResponse HandleReset(SmPreviewRequest request)
    {
        var sessionOrError = EnsureSession(request);
        if (!sessionOrError.Success || sessionOrError.Session is null)
            return new SmPreviewResponse(false, Error: sessionOrError.Error);

        var session = sessionOrError.Session;
        session.Instance = session.Definition.CreateInstance(session.Definition.InitialState);
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
            var fire = session.Definition.Fire(session.Instance, step.EventName, step.Args);
            if (fire.IsAccepted && fire.UpdatedInstance is not null)
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

        DslMachine machine;
        DslWorkflowDefinition definition;
        try
        {
            machine = StateMachineDslParser.Parse(text);
            definition = DslWorkflowCompiler.Compile(machine);
        }
        catch (Exception ex)
        {
            return SessionResult.Fail(ex.Message);
        }

        var session = Sessions.AddOrUpdate(
            key,
            _ => CreateSession(request.Uri, text, machine, definition),
            (_, existing) => UpdateSession(request.Uri, existing, text, machine, definition));

        return SessionResult.Ok(session);
    }

    private static PreviewSession CreateSession(DocumentUri uri, string sourceText, DslMachine machine, DslWorkflowDefinition definition)
    {
        var instance = definition.CreateInstance(definition.InitialState);
        return new PreviewSession(uri, sourceText, machine, definition, instance);
    }

    private static PreviewSession UpdateSession(
        DocumentUri uri,
        PreviewSession existing,
        string sourceText,
        DslMachine machine,
        DslWorkflowDefinition definition)
    {
        if (string.Equals(existing.SourceText, sourceText, StringComparison.Ordinal))
            return existing;

        var compatibility = definition.CheckCompatibility(existing.Instance);
        var nextInstance = compatibility.IsCompatible
            ? existing.Instance
            : definition.CreateInstance(definition.InitialState);

        existing.SourceText = sourceText;
        existing.Uri = uri;
        existing.Machine = machine;
        existing.Definition = definition;
        existing.Instance = nextInstance;
        return existing;
    }

    private static SmPreviewSnapshot BuildSnapshot(PreviewSession session)
    {
        var outgoingEventNames = session.Machine.Transitions
            .Where(t => string.Equals(t.FromState, session.Instance.CurrentState, StringComparison.Ordinal))
            .Select(t => t.EventName)
            .Concat(
                session.Machine.TerminalRules
                    .Where(r => string.Equals(r.FromState, session.Instance.CurrentState, StringComparison.Ordinal))
                    .Select(r => r.EventName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        var events = outgoingEventNames
            .Select(eventName =>
            {
                var inspect = session.Definition.Inspect(session.Instance, eventName);
                var evt = session.Machine.Events.FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.Ordinal));
                var args = (evt?.Args ?? Array.Empty<DslFieldContract>())
                    .Select(arg => new SmPreviewEventArg(arg.Name, arg.Type.ToString().ToLowerInvariant(), arg.IsNullable))
                    .ToArray();

                var outcome = inspect.Outcome switch
                {
                    DslOutcomeKind.Enabled => "enabled",
                    DslOutcomeKind.NoTransition => "noTransition",
                    DslOutcomeKind.Blocked => "blocked",
                    _ => "undefined"
                };

                return new SmPreviewEventStatus(
                    eventName,
                    outcome,
                    inspect.TargetState,
                    inspect.Reasons,
                    args);
            })
            .ToArray();

        var transitions = session.Machine.Transitions
            .Select(t => new SmPreviewTransition(t.FromState, t.ToState, t.EventName, t.GuardExpression, "transition"))
            .Concat(session.Machine.TerminalRules
                .Select(r => new SmPreviewTransition(r.FromState, r.FromState, r.EventName, r.GuardExpression, r.Kind.ToString().ToLowerInvariant())))
            .ToArray();

        var diagnostics = SmTextDocumentSyncHandler.SharedAnalyzer.GetDiagnostics(session.Uri)
            .Select(d => new SmPreviewDiagnostic(
                d.Severity?.ToString() ?? "Info",
                d.Message,
                (int)d.Range.Start.Line,
                (int)d.Range.Start.Character))
            .ToArray();

        return new SmPreviewSnapshot(
            session.Definition.Name,
            session.Instance.CurrentState,
            session.Definition.States,
            transitions,
            events,
            new Dictionary<string, object?>(session.Instance.InstanceData, StringComparer.Ordinal),
            diagnostics);
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
        public PreviewSession(DocumentUri uri, string sourceText, DslMachine machine, DslWorkflowDefinition definition, DslWorkflowInstance instance)
        {
            Uri = uri;
            SourceText = sourceText;
            Machine = machine;
            Definition = definition;
            Instance = instance;
        }

        public DocumentUri Uri { get; set; }
        public string SourceText { get; set; }
        public DslMachine Machine { get; set; }
        public DslWorkflowDefinition Definition { get; set; }
        public DslWorkflowInstance Instance { get; set; }
    }

    private sealed record SessionResult(bool Success, string? Error, PreviewSession? Session)
    {
        public static SessionResult Ok(PreviewSession session) => new(true, null, session);
        public static SessionResult Fail(string error) => new(false, error, null);
    }
}
