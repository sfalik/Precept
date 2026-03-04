using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StateMachine.Dsl;
using System.Collections.Concurrent;
using System.Text.Json;

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
        var coercedArgs = CoerceEventArgs(session.Machine, request.EventName!, request.Args);
        var inspect = session.Definition.Inspect(session.Instance, request.EventName!, coercedArgs);
        var evt = session.Machine.Events.FirstOrDefault(e => string.Equals(e.Name, request.EventName, StringComparison.Ordinal));
        var args = (evt?.Args ?? Array.Empty<DslFieldContract>())
            .Select(arg => new SmPreviewEventArg(arg.Name, arg.Type.ToString().ToLowerInvariant(), arg.IsNullable, arg.HasDefaultValue, arg.DefaultValue))
            .ToArray();

        var outcome = inspect.Outcome switch
        {
            DslOutcomeKind.Enabled => "enabled",
            DslOutcomeKind.NoTransition => "noTransition",
            DslOutcomeKind.Blocked => "blocked",
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
        var coercedArgs = CoerceEventArgs(session.Machine, request.EventName!, request.Args);
        var fire = session.Definition.Fire(session.Instance, request.EventName!, coercedArgs);
        if (fire.UpdatedInstance is not null)
            session.Instance = fire.UpdatedInstance;

        if (!fire.IsAccepted)
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
            var coercedStepArgs = CoerceEventArgs(session.Machine, step.EventName, step.Args);
            var fire = session.Definition.Fire(session.Instance, step.EventName, coercedStepArgs);
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
        var eventDeclarationOrder = session.Machine.Events
            .Select((e, i) => (e.Name, i))
            .ToDictionary(x => x.Name, x => x.i, StringComparer.Ordinal);

        var outgoingEventNames = session.Machine.Transitions
            .Where(t => string.Equals(t.FromState, session.Instance.CurrentState, StringComparison.Ordinal))
            .Select(t => t.EventName)
            .Concat(
                session.Machine.TerminalRules
                    .Where(r => string.Equals(r.FromState, session.Instance.CurrentState, StringComparison.Ordinal))
                    .Select(r => r.EventName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => eventDeclarationOrder.TryGetValue(n, out var idx) ? idx : int.MaxValue)
            .ThenBy(n => n, StringComparer.Ordinal)
            .ToArray();

        var events = outgoingEventNames
            .Select(eventName =>
            {
                var inspect = session.Definition.Inspect(session.Instance, eventName);
                var evt = session.Machine.Events.FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.Ordinal));
                var args = (evt?.Args ?? Array.Empty<DslFieldContract>())
                    .Select(arg => new SmPreviewEventArg(arg.Name, arg.Type.ToString().ToLowerInvariant(), arg.IsNullable, arg.HasDefaultValue, arg.DefaultValue))
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

        var activeRuleViolations = session.Definition.EvaluateCurrentRules(session.Instance);

        var ruleDefinitions = new List<SmPreviewRuleInfo>();
        foreach (var field in session.Machine.DataFields.Where(f => f.Rules is not null))
            foreach (var rule in field.Rules!)
                ruleDefinitions.Add(new SmPreviewRuleInfo($"field:{field.Name}", rule.ExpressionText, rule.Reason));
        foreach (var field in session.Machine.CollectionFields.Where(f => f.Rules is not null))
            foreach (var rule in field.Rules!)
                ruleDefinitions.Add(new SmPreviewRuleInfo($"field:{field.Name}", rule.ExpressionText, rule.Reason));
        foreach (var rule in session.Machine.TopLevelRules ?? Array.Empty<DslRule>())
            ruleDefinitions.Add(new SmPreviewRuleInfo("topLevel", rule.ExpressionText, rule.Reason));
        foreach (var (stateName, stateRuleList) in session.Machine.StateRules ?? (IReadOnlyDictionary<string, IReadOnlyList<DslRule>>)new Dictionary<string, IReadOnlyList<DslRule>>())
            foreach (var rule in stateRuleList)
                ruleDefinitions.Add(new SmPreviewRuleInfo($"state:{stateName}", rule.ExpressionText, rule.Reason));
        foreach (var evt in session.Machine.Events.Where(e => e.Rules is not null && e.Rules.Count > 0))
            foreach (var rule in evt.Rules!)
                ruleDefinitions.Add(new SmPreviewRuleInfo($"event:{evt.Name}", rule.ExpressionText, rule.Reason));

        return new SmPreviewSnapshot(
            session.Definition.Name,
            session.Instance.CurrentState,
            session.Definition.States,
            transitions,
            events,
            new Dictionary<string, object?>(session.Instance.InstanceData, StringComparer.Ordinal),
            diagnostics,
            activeRuleViolations.Count > 0 ? activeRuleViolations : null,
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

    /// <summary>
    /// Coerces event argument values from their JSON-deserialized form (often strings from UI text inputs
    /// or JsonElement from System.Text.Json) to the runtime types declared in the event contract.
    /// </summary>
    private static IReadOnlyDictionary<string, object?>? CoerceEventArgs(
        DslMachine machine,
        string eventName,
        IReadOnlyDictionary<string, object?>? args)
    {
        if (args is null || args.Count == 0)
            return args;

        var eventDef = machine.Events.FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.Ordinal));
        if (eventDef is null || eventDef.Args.Count == 0)
            return args;

        var argContracts = eventDef.Args.ToDictionary(a => a.Name, a => a, StringComparer.Ordinal);
        var coerced = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var kvp in args)
        {
            if (!argContracts.TryGetValue(kvp.Key, out var contract))
            {
                coerced[kvp.Key] = kvp.Value;
                continue;
            }

            coerced[kvp.Key] = CoerceValue(kvp.Value, contract);
        }

        return coerced;
    }

    private static object? CoerceValue(object? value, DslFieldContract contract)
    {
        // Unwrap JsonElement from System.Text.Json deserialization.
        if (value is JsonElement jsonElement)
            value = UnwrapJsonElement(jsonElement);

        // Already correct type or null.
        if (value is null)
            return null;

        return contract.Type switch
        {
            DslScalarType.Number => CoerceToNumber(value),
            DslScalarType.Boolean => CoerceToBoolean(value),
            DslScalarType.String => value?.ToString(),
            DslScalarType.Null => null,
            _ => value
        };
    }

    private static object? CoerceToNumber(object value)
    {
        if (value is double or float or int or long or decimal or byte or sbyte or short or ushort or uint or ulong)
            return Convert.ToDouble(value);

        if (value is string s && double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;

        return value; // Let runtime validation report the error.
    }

    private static object? CoerceToBoolean(object value)
    {
        if (value is bool)
            return value;

        if (value is string s)
        {
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return value; // Let runtime validation report the error.
    }

    private static object? UnwrapJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }
}
