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
    private static readonly Regex ComparisonRegex = new(
        "^(?<left>[A-Za-z_][A-Za-z0-9_]*)\\s*(?<op>==|!=|>=|<=|>|<)\\s*(?<right>.+)$",
        RegexOptions.Compiled);

    private static readonly Regex BooleanRegex = new(
        "^(?<not>!)?(?<left>[A-Za-z_][A-Za-z0-9_]*)$",
        RegexOptions.Compiled);

    public GuardEvaluationResult Evaluate(string expression, IReadOnlyDictionary<string, object?> data)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return GuardEvaluationResult.Passed();

        var trimmed = expression.Trim();

        var comparison = ComparisonRegex.Match(trimmed);
        if (comparison.Success)
            return EvaluateComparison(trimmed, comparison, data);

        var boolean = BooleanRegex.Match(trimmed);
        if (boolean.Success)
            return EvaluateBoolean(trimmed, boolean, data);

        return GuardEvaluationResult.Failed($"Guard '{trimmed}' is not supported by the default evaluator.");
    }

    private static GuardEvaluationResult EvaluateComparison(
        string originalExpression,
        Match comparison,
        IReadOnlyDictionary<string, object?> data)
    {
        string variable = comparison.Groups["left"].Value;
        string op = comparison.Groups["op"].Value;
        string literalText = comparison.Groups["right"].Value.Trim();

        if (!data.TryGetValue(variable, out var currentValue))
            return GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed: data key '{variable}' was not provided.");

        if (!TryParseLiteral(literalText, out var literal))
            return GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed: literal '{literalText}' is not supported.");

        if (TryToNumber(currentValue, out var leftNumber) && TryToNumber(literal, out var rightNumber))
        {
            bool numericResult = op switch
            {
                ">" => leftNumber > rightNumber,
                ">=" => leftNumber >= rightNumber,
                "<" => leftNumber < rightNumber,
                "<=" => leftNumber <= rightNumber,
                "==" => leftNumber == rightNumber,
                "!=" => leftNumber != rightNumber,
                _ => false
            };

            return numericResult
                ? GuardEvaluationResult.Passed()
                : GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed.");
        }

        if (op is ">" or ">=" or "<" or "<=")
            return GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed: relational operators require numeric values.");

        bool equals = Equals(currentValue, literal);
        bool result = op == "==" ? equals : !equals;

        return result
            ? GuardEvaluationResult.Passed()
            : GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed.");
    }

    private static GuardEvaluationResult EvaluateBoolean(
        string originalExpression,
        Match boolean,
        IReadOnlyDictionary<string, object?> data)
    {
        string variable = boolean.Groups["left"].Value;
        bool negate = boolean.Groups["not"].Success;

        if (!data.TryGetValue(variable, out var value))
            return GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed: data key '{variable}' was not provided.");

        if (value is not bool b)
            return GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed: data key '{variable}' is not a boolean.");

        bool result = negate ? !b : b;
        return result
            ? GuardEvaluationResult.Passed()
            : GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed.");
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
    private readonly Dictionary<(string State, string Event), DslTerminalRule> _terminalRuleMap;
    private readonly IGuardEvaluator _guardEvaluator;
    private static readonly Regex IdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public string Name { get; }
    public IReadOnlyList<string> States { get; }
    public IReadOnlyList<DslEvent> Events { get; }

    internal DslWorkflowDefinition(DslMachine machine, IGuardEvaluator guardEvaluator)
    {
        Name = machine.Name;
        States = machine.States;
        Events = machine.Events;
        _guardEvaluator = guardEvaluator;

        _transitionMap = machine.Transitions
            .GroupBy(t => (t.FromState, t.EventName))
            .ToDictionary(
                g => (g.Key.FromState, g.Key.EventName),
                g => g.ToList(),
                EqualityComparer<(string State, string Event)>.Default);

        _terminalRuleMap = machine.TerminalRules
            .ToDictionary(
                rule => (rule.FromState, rule.EventName),
                rule => rule,
                EqualityComparer<(string State, string Event)>.Default);
    }

    public DslWorkflowInstance CreateInstance(
        string initialState,
        IReadOnlyDictionary<string, object?>? instanceData = null)
    {
        if (string.IsNullOrWhiteSpace(initialState))
            throw new ArgumentException("Initial state is required.", nameof(initialState));

        if (!States.Contains(initialState, StringComparer.Ordinal))
            throw new InvalidOperationException($"State '{initialState}' is not defined in workflow '{Name}'.");

        return new DslWorkflowInstance(
            Name,
            initialState,
            null,
            DateTimeOffset.UtcNow,
            instanceData ?? EmptyInstanceData.Instance);
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

        return DslInstanceCompatibilityResult.Compatible();
    }

    public DslInspectionResult Inspect(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var evaluationData = eventArguments ?? EmptyInstanceData.Instance;
        var resolution = ResolveTransition(currentState, eventName, evaluationData);

        return resolution.Kind switch
        {
            TransitionResolutionKind.Accepted => DslInspectionResult.Accepted(
                currentState,
                eventName,
                resolution.Transition!.ToState,
                ExtractRequiredEventArgumentKeys(resolution.Transition.DataAssignmentExpression)),
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

        var evaluationArguments = eventArguments ?? instance.InstanceData;
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

        var evaluationArguments = eventArguments ?? instance.InstanceData;
        var resolution = ResolveTransition(instance.CurrentState, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.NotDefined)
            return DslInstanceFireResult.NotDefined(instance.CurrentState, eventName, new[] { resolution.NotDefinedReason! });

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.Transition is null)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, resolution.Reasons);

        var updatedData = new Dictionary<string, object?>(instance.InstanceData, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(resolution.Transition.DataAssignmentKey) &&
            !string.IsNullOrWhiteSpace(resolution.Transition.DataAssignmentExpression))
        {
            if (!TryResolveAssignmentValue(
                resolution.Transition.DataAssignmentExpression,
                eventArguments ?? EmptyInstanceData.Instance,
                out var assignedValue,
                out var assignmentError))
            {
                return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { assignmentError! });
            }

            updatedData[resolution.Transition.DataAssignmentKey!] = assignedValue;
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

        _terminalRuleMap.TryGetValue((currentState, eventName), out var terminalRule);

        if (!_transitionMap.TryGetValue((currentState, eventName), out var transitions) || transitions.Count == 0)
        {
            if (terminalRule is not null)
                return ResolveTerminal(currentState, eventName, terminalRule, Array.Empty<string>());

            return TransitionResolution.NotDefined($"No transition for '{eventName}' from '{currentState}'.");
        }

        var reasons = new List<string>();
        foreach (var transition in transitions)
        {
            if (string.IsNullOrWhiteSpace(transition.GuardExpression))
                return TransitionResolution.Accepted(transition);

            var evaluation = _guardEvaluator.Evaluate(transition.GuardExpression, evaluationData);
            if (evaluation.IsPassed)
                return TransitionResolution.Accepted(transition);

            reasons.Add(
                transition.GuardFailureReason
                ?? evaluation.FailureReason
                ?? $"Guard '{transition.GuardExpression}' failed.");
        }

        if (terminalRule is not null)
            return ResolveTerminal(currentState, eventName, terminalRule, reasons);

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

    private static bool TryResolveAssignmentValue(
        string assignmentExpression,
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

        if (expression.StartsWith("data.", StringComparison.Ordinal))
        {
            assignedValue = null;
            error = "Data assignment failed: 'data.' references are not allowed in transforms; use a bare event-argument key or a literal.";
            return false;
        }

        if (expression.StartsWith("arg.", StringComparison.Ordinal))
        {
            assignedValue = null;
            error = "Data assignment failed: 'arg.' prefix is deprecated; use a bare event-argument key (for example, 'Reason').";
            return false;
        }

        if (IdentifierRegex.IsMatch(expression))
        {
            var key = expression;
            if (!eventArguments.TryGetValue(key, out assignedValue))
            {
                error = $"Data assignment failed: event argument '{key}' was not provided.";
                return false;
            }

            error = null;
            return true;
        }

        error = $"Data assignment failed: expression '{expression}' is not supported.";
        return false;
    }

    private static IReadOnlyList<string> ExtractRequiredEventArgumentKeys(string? assignmentExpression)
    {
        if (string.IsNullOrWhiteSpace(assignmentExpression))
            return Array.Empty<string>();

        var expression = assignmentExpression.Trim();
        if (TryParseLiteral(expression, out _))
            return Array.Empty<string>();

        if (IdentifierRegex.IsMatch(expression))
            return new[] { expression };

        return Array.Empty<string>();
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
