using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace StateMachine.Dsl;

public interface IGuardEvaluator
{
    GuardEvaluationResult Evaluate(string expression, IReadOnlyDictionary<string, object?> data);
}

public sealed record GuardEvaluationResult(bool IsPassed, string? FailureReason)
{
    public static GuardEvaluationResult Passed() => new(true, null);
    public static GuardEvaluationResult Failed(string reason) => new(false, reason);
}

internal sealed class DefaultGuardEvaluator : IGuardEvaluator
{
    public GuardEvaluationResult Evaluate(string expression, IReadOnlyDictionary<string, object?> data)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return GuardEvaluationResult.Passed();

        var trimmed = expression.Trim();
        DslExpression parsed;
        try
        {
            parsed = DslExpressionParser.Parse(trimmed);
        }
        catch (InvalidOperationException ex)
        {
            return GuardEvaluationResult.Failed($"Guard '{trimmed}' failed: {ex.Message}");
        }

        var evaluation = DslExpressionRuntimeEvaluator.Evaluate(parsed, data);
        if (!evaluation.Success)
            return GuardEvaluationResult.Failed($"Guard '{trimmed}' failed: {evaluation.Error}");

        if (evaluation.Value is not bool result)
            return GuardEvaluationResult.Failed($"Guard '{trimmed}' failed: expression must evaluate to boolean.");

        return result
            ? GuardEvaluationResult.Passed()
            : GuardEvaluationResult.Failed($"Guard '{trimmed}' failed.");
    }

    private static bool TryParseLiteral(string text, out object? value)
    {
        if (text.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            value = null;
            return true;
        }

        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        if ((text.Length >= 2 && text[0] == '\'' && text[^1] == '\'') ||
            (text.Length >= 2 && text[0] == '"' && text[^1] == '"'))
        {
            value = text.Substring(1, text.Length - 2);
            return true;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            value = number;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryToNumber(object? value, out double number)
    {
        switch (value)
        {
            case byte b: number = b; return true;
            case sbyte sb: number = sb; return true;
            case short s: number = s; return true;
            case ushort us: number = us; return true;
            case int i: number = i; return true;
            case uint ui: number = ui; return true;
            case long l: number = l; return true;
            case ulong ul: number = ul; return true;
            case float f: number = f; return true;
            case double d: number = d; return true;
            case decimal dec: number = (double)dec; return true;
            default:
                number = default;
                return false;
        }
    }
}

public sealed class DslWorkflowDefinition
{
    private readonly Dictionary<(string State, string Event), List<DslTransition>> _transitionMap;
    private readonly Dictionary<(string State, string Event), List<DslTerminalRule>> _terminalRuleMap;
    private readonly Dictionary<string, DslFieldContract> _dataContractMap;
    private readonly Dictionary<string, Dictionary<string, DslFieldContract>> _eventArgContractMap;
    private readonly IGuardEvaluator _guardEvaluator;
    private static readonly Regex IdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public string Name { get; }
    public IReadOnlyList<string> States { get; }
    public IReadOnlyList<DslEvent> Events { get; }
    public IReadOnlyList<DslFieldContract> DataFields { get; }

    internal DslWorkflowDefinition(DslMachine machine, IGuardEvaluator guardEvaluator)
    {
        Name = machine.Name;
        States = machine.States;
        Events = machine.Events;
        DataFields = machine.DataFields;
        _guardEvaluator = guardEvaluator;

        _transitionMap = machine.Transitions
            .GroupBy(t => (t.FromState, t.EventName))
            .ToDictionary(
                g => (g.Key.FromState, g.Key.EventName),
                g => g.ToList(),
                EqualityComparer<(string State, string Event)>.Default);

        _terminalRuleMap = machine.TerminalRules
            .GroupBy(rule => (rule.FromState, rule.EventName))
            .ToDictionary(
                g => (g.Key.FromState, g.Key.EventName),
                g => g.OrderBy(r => r.Order).ToList(),
                EqualityComparer<(string State, string Event)>.Default);

        _dataContractMap = machine.DataFields
            .ToDictionary(f => f.Name, f => f, StringComparer.Ordinal);

        _eventArgContractMap = machine.Events
            .ToDictionary(
                evt => evt.Name,
                evt => evt.Args.ToDictionary(a => a.Name, a => a, StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    public DslWorkflowInstance CreateInstance(
        string initialState,
        IReadOnlyDictionary<string, object?>? instanceData = null)
    {
        if (string.IsNullOrWhiteSpace(initialState))
            throw new ArgumentException("Initial state is required.", nameof(initialState));

        if (!States.Contains(initialState, StringComparer.Ordinal))
            throw new InvalidOperationException($"State '{initialState}' is not defined in workflow '{Name}'.");

        var data = instanceData ?? EmptyInstanceData.Instance;
        if (!TryValidateDataContract(data, out var dataError))
            throw new InvalidOperationException(dataError);

        return new DslWorkflowInstance(
            Name,
            initialState,
            null,
            DateTimeOffset.UtcNow,
            data);
    }

    public DslInstanceCompatibilityResult CheckCompatibility(DslWorkflowInstance instance)
    {
        if (!instance.WorkflowName.Equals(Name, StringComparison.Ordinal))
        {
            return DslInstanceCompatibilityResult.NotCompatible(
                $"Instance workflow '{instance.WorkflowName}' does not match compiled workflow '{Name}'.");
        }

        if (!States.Contains(instance.CurrentState, StringComparer.Ordinal))
            return DslInstanceCompatibilityResult.NotCompatible($"Instance state '{instance.CurrentState}' is not defined in workflow '{Name}'.");

        if (!TryValidateDataContract(instance.InstanceData, out var dataError))
            return DslInstanceCompatibilityResult.NotCompatible(dataError);

        return DslInstanceCompatibilityResult.Compatible();
    }

    public DslInspectionResult Inspect(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var evaluationData = BuildDirectEvaluationData(eventName, eventArguments);
        var resolution = ResolveTransition(currentState, eventName, evaluationData);

        return resolution.Kind switch
        {
            TransitionResolutionKind.Accepted => DslInspectionResult.Accepted(
                currentState,
                eventName,
                resolution.Transition!.ToState,
                GetRequiredEventArgumentKeys(eventName)),
            TransitionResolutionKind.NotDefined => DslInspectionResult.NotDefined(currentState, eventName, resolution.NotDefinedReason!),
            _ => DslInspectionResult.Rejected(currentState, eventName, resolution.Reasons)
        };
    }

    public DslInspectionResult Inspect(
        DslWorkflowInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
            return DslInspectionResult.NotDefined(instance.CurrentState, eventName, compatibility.Reason!);

        if (eventArguments is not null && !TryValidateEventArguments(eventName, eventArguments, out var eventArgError))
            return DslInspectionResult.Rejected(instance.CurrentState, eventName, new[] { eventArgError! });

        var evaluationArguments = BuildEvaluationData(instance.InstanceData, eventName, eventArguments);
        return Inspect(instance.CurrentState, eventName, evaluationArguments);
    }

    public DslFireResult Fire(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var inspection = Inspect(currentState, eventName, eventArguments);
        if (!inspection.IsDefined)
            return DslFireResult.NotDefined(currentState, eventName, inspection.Reasons);

        if (!inspection.IsAccepted || inspection.TargetState is null)
            return DslFireResult.Rejected(currentState, eventName, inspection.Reasons);

        return DslFireResult.Accepted(currentState, eventName, inspection.TargetState);
    }

    public DslInstanceFireResult Fire(
        DslWorkflowInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
        {
            var reasons = new[] { compatibility.Reason! };
            return DslInstanceFireResult.NotDefined(instance.CurrentState, eventName, reasons);
        }

        if (!TryValidateEventArguments(eventName, eventArguments, out var eventArgError))
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { eventArgError! });

        var evaluationArguments = BuildEvaluationData(instance.InstanceData, eventName, eventArguments);
        var resolution = ResolveTransition(instance.CurrentState, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.NotDefined)
            return DslInstanceFireResult.NotDefined(instance.CurrentState, eventName, new[] { resolution.NotDefinedReason! });

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.Transition is null)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, resolution.Reasons);

        var updatedData = new Dictionary<string, object?>(instance.InstanceData, StringComparer.Ordinal);
        var assignment = resolution.Transition.TransformAssignments.LastOrDefault();
        if (assignment is not null)
        {
            var assignmentContext = BuildEvaluationData(instance.InstanceData, eventName, eventArguments);
            var assignmentEvaluation = DslExpressionRuntimeEvaluator.Evaluate(assignment.Expression, assignmentContext);
            if (!assignmentEvaluation.Success)
                return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { $"Data assignment failed: {assignmentEvaluation.Error}" });

            if (!TryValidateAssignedValue(assignment.Key, assignmentEvaluation.Value, out var contractError))
                return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { contractError! });

            updatedData[assignment.Key] = assignmentEvaluation.Value;
        }

        var updated = instance with
        {
            CurrentState = resolution.Transition.ToState,
            LastEvent = eventName,
            UpdatedAt = DateTimeOffset.UtcNow,
            InstanceData = updatedData
        };

        return DslInstanceFireResult.Accepted(instance.CurrentState, eventName, resolution.Transition.ToState, updated);
    }

    private TransitionResolution ResolveTransition(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?> evaluationData)
    {
        if (string.IsNullOrWhiteSpace(currentState))
            throw new ArgumentException("Current state is required.", nameof(currentState));
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name is required.", nameof(eventName));

        if (!States.Contains(currentState, StringComparer.Ordinal))
            return TransitionResolution.NotDefined($"Unknown state '{currentState}'.");

        if (!Events.Any(e => e.Name.Equals(eventName, StringComparison.Ordinal)))
            return TransitionResolution.NotDefined($"Unknown event '{eventName}'.");

        _transitionMap.TryGetValue((currentState, eventName), out var transitions);
        _terminalRuleMap.TryGetValue((currentState, eventName), out var terminalRules);

        var hasTransitions = transitions is not null && transitions.Count > 0;
        var hasTerminalRules = terminalRules is not null && terminalRules.Count > 0;

        if (!hasTransitions && !hasTerminalRules)
            return TransitionResolution.NotDefined($"No transition for '{eventName}' from '{currentState}'.");

        var orderedOutcomes = new List<OutcomeCandidate>();
        if (hasTransitions)
        {
            foreach (var transition in transitions!)
                orderedOutcomes.Add(OutcomeCandidate.FromTransition(transition));
        }

        if (hasTerminalRules)
        {
            foreach (var terminalRule in terminalRules!)
                orderedOutcomes.Add(OutcomeCandidate.FromTerminal(terminalRule));
        }

        orderedOutcomes.Sort((left, right) => left.Order.CompareTo(right.Order));

        var reasons = new List<string>();
        foreach (var candidate in orderedOutcomes)
        {
            if (candidate.Transition is not null)
            {
                var transition = candidate.Transition;
                if (string.IsNullOrWhiteSpace(transition.GuardExpression))
                    return TransitionResolution.Accepted(transition);

                var evaluation = _guardEvaluator.Evaluate(transition.GuardExpression, evaluationData);
                if (evaluation.IsPassed)
                    return TransitionResolution.Accepted(transition);

                reasons.Add(
                    evaluation.FailureReason
                    ?? $"Guard '{transition.GuardExpression}' failed.");

                continue;
            }

            if (candidate.TerminalRule is null)
                continue;

            var terminalRule = candidate.TerminalRule;
            if (string.IsNullOrWhiteSpace(terminalRule.GuardExpression))
                return ResolveTerminal(currentState, eventName, terminalRule, reasons);

            var terminalEvaluation = _guardEvaluator.Evaluate(terminalRule.GuardExpression, evaluationData);
            if (terminalEvaluation.IsPassed)
                return ResolveTerminal(currentState, eventName, terminalRule, reasons);

            reasons.Add(
                terminalEvaluation.FailureReason
                ?? $"Guard '{terminalRule.GuardExpression}' failed.");
        }

        return TransitionResolution.Rejected(reasons);
    }

    private static TransitionResolution ResolveTerminal(
        string currentState,
        string eventName,
        DslTerminalRule terminalRule,
        IReadOnlyList<string> guardFailureReasons)
    {
        return terminalRule.Kind switch
        {
            DslTerminalKind.NoTransition => TransitionResolution.NotDefined($"No transition for '{eventName}' from '{currentState}'."),
            DslTerminalKind.Reject => TransitionResolution.Rejected(new[]
            {
                string.IsNullOrWhiteSpace(terminalRule.Reason)
                    ? (guardFailureReasons.FirstOrDefault() ?? $"No transition for '{eventName}' from '{currentState}'.")
                    : terminalRule.Reason!
            }),
            _ => TransitionResolution.Rejected(guardFailureReasons)
        };
    }

    private bool TryResolveAssignmentValue(
        string assignmentExpression,
        IReadOnlyDictionary<string, object?> instanceData,
        string eventName,
        IReadOnlyDictionary<string, object?> eventArguments,
        out object? assignedValue,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(assignmentExpression))
        {
            assignedValue = null;
            error = "Data assignment expression is empty.";
            return false;
        }

        var expression = assignmentExpression.Trim();

        if (TryParseLiteral(expression, out assignedValue))
        {
            error = null;
            return true;
        }

        if (TryResolveScopedIdentifier(expression, out var scope, out var key))
        {
            if (scope.Equals("data", StringComparison.Ordinal))
            {
                error = "Data assignment failed: 'data.' scope is no longer supported. Use bare data field names.";
                assignedValue = null;
                return false;
            }

            if (!scope.Equals(eventName, StringComparison.Ordinal))
            {
                error = $"Data assignment failed: event-argument scope '{scope}' does not match current event '{eventName}'.";
                return false;
            }

            if (!eventArguments.TryGetValue(key, out assignedValue))
            {
                error = $"Data assignment failed: event argument '{key}' was not provided.";
                return false;
            }

            error = null;
            return true;
        }

        if (IdentifierRegex.IsMatch(expression))
        {
            if (!instanceData.TryGetValue(expression, out assignedValue))
            {
                error = $"Data assignment failed: data field '{expression}' was not provided.";
                return false;
            }

            error = null;
            return true;
        }

        error = $"Data assignment failed: expression '{expression}' is not supported. Use literals, <DataField>, or {eventName}.<Arg>.";
        return false;
    }

    private IReadOnlyList<string> GetRequiredEventArgumentKeys(string eventName)
    {
        if (!_eventArgContractMap.TryGetValue(eventName, out var argContract) || argContract.Count == 0)
            return Array.Empty<string>();

        return argContract.Values
            .Where(a => !a.IsNullable)
            .Select(a => a.Name)
            .ToArray();
    }

    private IReadOnlyDictionary<string, object?> BuildEvaluationData(
        IReadOnlyDictionary<string, object?> instanceData,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments)
    {
        var evaluation = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var kvp in instanceData)
            evaluation[kvp.Key] = kvp.Value;

        if (eventArguments is not null)
        {
            foreach (var kvp in eventArguments)
                evaluation[$"{eventName}.{kvp.Key}"] = kvp.Value;
        }

        return evaluation;
    }

    private IReadOnlyDictionary<string, object?> BuildDirectEvaluationData(
        string eventName,
        IReadOnlyDictionary<string, object?>? values)
    {
        if (values is null || values.Count == 0)
            return EmptyInstanceData.Instance;

        var evaluation = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in values)
        {
            if (kvp.Key.Contains('.', StringComparison.Ordinal))
                evaluation[kvp.Key] = kvp.Value;

            evaluation[kvp.Key] = kvp.Value;
            evaluation[$"{eventName}.{kvp.Key}"] = kvp.Value;
        }

        return evaluation;
    }

    private static bool TryResolveScopedIdentifier(string text, out string scope, out string key)
    {
        scope = string.Empty;
        key = string.Empty;

        var separatorIndex = text.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex == text.Length - 1)
            return false;

        if (text.IndexOf('.', separatorIndex + 1) >= 0)
            return false;

        scope = text[..separatorIndex];
        key = text[(separatorIndex + 1)..];

        return IdentifierRegex.IsMatch(scope) && IdentifierRegex.IsMatch(key);
    }

    private bool TryValidateDataContract(IReadOnlyDictionary<string, object?> data, out string error)
    {
        if (_dataContractMap.Count == 0)
        {
            error = string.Empty;
            return true;
        }

        foreach (var key in data.Keys)
        {
            if (!_dataContractMap.ContainsKey(key))
            {
                error = $"Data validation failed: unknown data field '{key}'.";
                return false;
            }
        }

        foreach (var field in _dataContractMap.Values)
        {
            if (!data.TryGetValue(field.Name, out var value))
            {
                if (!field.IsNullable)
                {
                    error = $"Data validation failed: required data field '{field.Name}' is missing.";
                    return false;
                }

                continue;
            }

            if (!TryValidateScalarValue(field, value, out error))
                return false;
        }

        error = string.Empty;
        return true;
    }

    private bool TryValidateEventArguments(string eventName, IReadOnlyDictionary<string, object?>? eventArguments, out string? error)
    {
        if (!_eventArgContractMap.TryGetValue(eventName, out var eventContract) || eventContract.Count == 0)
        {
            error = null;
            return true;
        }

        var args = eventArguments ?? EmptyInstanceData.Instance;

        foreach (var key in args.Keys)
        {
            if (!eventContract.ContainsKey(key))
            {
                error = $"Event argument validation failed: unknown argument '{key}' for event '{eventName}'.";
                return false;
            }
        }

        foreach (var arg in eventContract.Values)
        {
            if (!args.TryGetValue(arg.Name, out var value))
            {
                if (!arg.IsNullable)
                {
                    error = $"Event argument validation failed: required argument '{arg.Name}' for event '{eventName}' is missing.";
                    return false;
                }

                continue;
            }

            if (!TryValidateScalarValue(arg, value, out error))
            {
                error = $"Event argument validation failed: {error}";
                return false;
            }
        }

        error = null;
        return true;
    }

    private bool TryValidateAssignedValue(string dataFieldName, object? value, out string error)
    {
        if (!_dataContractMap.TryGetValue(dataFieldName, out var contract))
        {
            error = string.Empty;
            return true;
        }

        if (TryValidateScalarValue(contract, value, out error))
            return true;

        error = $"Data assignment failed: {error}";
        return false;
    }

    private static bool TryValidateScalarValue(DslFieldContract contract, object? value, out string error)
    {
        if (value is null)
        {
            if (!contract.IsNullable)
            {
                error = $"'{contract.Name}' does not allow null values.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        bool typeMatches = contract.Type switch
        {
            DslScalarType.String => value is string,
            DslScalarType.Boolean => value is bool,
            DslScalarType.Number => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal,
            DslScalarType.Null => false,
            _ => false
        };

        if (!typeMatches)
        {
            error = $"'{contract.Name}' expects {contract.Type.ToString().ToLowerInvariant()} value.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseLiteral(string text, out object? value)
    {
        if (text.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            value = null;
            return true;
        }

        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        if ((text.Length >= 2 && text[0] == '\'' && text[^1] == '\'') ||
            (text.Length >= 2 && text[0] == '"' && text[^1] == '"'))
        {
            value = text.Substring(1, text.Length - 2);
            return true;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            value = number;
            return true;
        }

        value = null;
        return false;
    }

    private enum TransitionResolutionKind
    {
        Accepted,
        Rejected,
        NotDefined
    }

    private sealed record TransitionResolution(
        TransitionResolutionKind Kind,
        DslTransition? Transition,
        string? NotDefinedReason,
        IReadOnlyList<string> Reasons)
    {
        internal static TransitionResolution Accepted(DslTransition transition) =>
            new(TransitionResolutionKind.Accepted, transition, null, Array.Empty<string>());

        internal static TransitionResolution Rejected(IReadOnlyList<string> reasons) =>
            new(TransitionResolutionKind.Rejected, null, null, reasons);

        internal static TransitionResolution NotDefined(string reason) =>
            new(TransitionResolutionKind.NotDefined, null, reason, new[] { reason });
    }

    private sealed record OutcomeCandidate(int Order, DslTransition? Transition, DslTerminalRule? TerminalRule)
    {
        internal static OutcomeCandidate FromTransition(DslTransition transition) =>
            new(transition.Order, transition, null);

        internal static OutcomeCandidate FromTerminal(DslTerminalRule terminalRule) =>
            new(terminalRule.Order, null, terminalRule);
    }

}

public static class DslWorkflowCompiler
{
    public static DslWorkflowDefinition Compile(DslMachine machine, IGuardEvaluator? guardEvaluator = null)
    {
        if (machine is null)
            throw new ArgumentNullException(nameof(machine));

        return new DslWorkflowDefinition(machine, guardEvaluator ?? new DefaultGuardEvaluator());
    }
}

public sealed record DslWorkflowInstance(
    string WorkflowName,
    string CurrentState,
    string? LastEvent,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, object?> InstanceData);

public sealed record DslInstanceCompatibilityResult(bool IsCompatible, string? Reason)
{
    internal static DslInstanceCompatibilityResult Compatible() => new(true, null);
    internal static DslInstanceCompatibilityResult NotCompatible(string reason) => new(false, reason);
}

internal sealed class EmptyInstanceData : IReadOnlyDictionary<string, object?>
{
    internal static readonly EmptyInstanceData Instance = new();

    public IEnumerable<string> Keys => Array.Empty<string>();
    public IEnumerable<object?> Values => Array.Empty<object?>();
    public int Count => 0;
    public object? this[string key] => throw new KeyNotFoundException();
    public bool ContainsKey(string key) => false;
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
        Enumerable.Empty<KeyValuePair<string, object?>>().GetEnumerator();
    public bool TryGetValue(string key, out object? value)
    {
        value = null;
        return false;
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed record DslInspectionResult(
    DslOutcomeKind Outcome,
    bool IsDefined,
    bool IsAccepted,
    string CurrentState,
    string EventName,
    string? TargetState,
    IReadOnlyList<string> RequiredEventArgumentKeys,
    IReadOnlyList<string> Reasons)
{
    internal static DslInspectionResult Accepted(string state, string evt, string target, IReadOnlyList<string> requiredEventArgumentKeys) =>
        new(DslOutcomeKind.Enabled, true, true, state, evt, target, requiredEventArgumentKeys, Array.Empty<string>());

    internal static DslInspectionResult NotDefined(string state, string evt, string reason) =>
        new(DslOutcomeKind.Undefined, false, false, state, evt, null, Array.Empty<string>(), new[] { reason });

    internal static DslInspectionResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Blocked, true, false, state, evt, null, Array.Empty<string>(), reasons);
}

public sealed record DslFireResult(
    DslOutcomeKind Outcome,
    bool IsDefined,
    bool IsAccepted,
    string CurrentState,
    string EventName,
    string? NewState,
    IReadOnlyList<string> Reasons)
{
    internal static DslFireResult Accepted(string state, string evt, string newState) =>
        new(DslOutcomeKind.Enabled, true, true, state, evt, newState, Array.Empty<string>());

    internal static DslFireResult NotDefined(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Undefined, false, false, state, evt, null, reasons);

    internal static DslFireResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Blocked, true, false, state, evt, null, reasons);
}

public sealed record DslInstanceFireResult(
    DslOutcomeKind Outcome,
    bool IsDefined,
    bool IsAccepted,
    string PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<string> Reasons,
    DslWorkflowInstance? UpdatedInstance)
{
    internal static DslInstanceFireResult Accepted(string state, string evt, string newState, DslWorkflowInstance updated) =>
        new(DslOutcomeKind.Enabled, true, true, state, evt, newState, Array.Empty<string>(), updated);

    internal static DslInstanceFireResult NotDefined(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Undefined, false, false, state, evt, null, reasons, null);

    internal static DslInstanceFireResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Blocked, true, false, state, evt, null, reasons, null);
}

public enum DslOutcomeKind
{
    Undefined,
    Blocked,
    Enabled
}
