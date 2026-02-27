using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace StateMachine.Dsl;

public interface IGuardEvaluator
{
    GuardEvaluationResult Evaluate(string expression, IReadOnlyDictionary<string, object?> context);
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

    public GuardEvaluationResult Evaluate(string expression, IReadOnlyDictionary<string, object?> context)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return GuardEvaluationResult.Passed();

        var trimmed = expression.Trim();

        var comparison = ComparisonRegex.Match(trimmed);
        if (comparison.Success)
            return EvaluateComparison(trimmed, comparison, context);

        var boolean = BooleanRegex.Match(trimmed);
        if (boolean.Success)
            return EvaluateBoolean(trimmed, boolean, context);

        return GuardEvaluationResult.Failed($"Guard '{trimmed}' is not supported by the default evaluator.");
    }

    private static GuardEvaluationResult EvaluateComparison(
        string originalExpression,
        Match comparison,
        IReadOnlyDictionary<string, object?> context)
    {
        string variable = comparison.Groups["left"].Value;
        string op = comparison.Groups["op"].Value;
        string literalText = comparison.Groups["right"].Value.Trim();

        if (!context.TryGetValue(variable, out var currentValue))
            return GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed: context key '{variable}' was not provided.");

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
        IReadOnlyDictionary<string, object?> context)
    {
        string variable = boolean.Groups["left"].Value;
        bool negate = boolean.Groups["not"].Success;

        if (!context.TryGetValue(variable, out var value))
            return GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed: context key '{variable}' was not provided.");

        if (value is not bool b)
            return GuardEvaluationResult.Failed($"Guard '{originalExpression}' failed: context key '{variable}' is not a boolean.");

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
    private readonly IGuardEvaluator _guardEvaluator;

    public string Name { get; }
    public string Version { get; }
    public IReadOnlyList<string> States { get; }
    public IReadOnlyList<DslEvent> Events { get; }

    internal DslWorkflowDefinition(DslMachine machine, IGuardEvaluator guardEvaluator)
    {
        Name = machine.Name;
        Version = ComputeVersion(machine);
        States = machine.States;
        Events = machine.Events;
        _guardEvaluator = guardEvaluator;

        _transitionMap = machine.Transitions
            .GroupBy(t => (t.FromState, t.EventName))
            .ToDictionary(
                g => (g.Key.FromState, g.Key.EventName),
                g => g.ToList(),
                EqualityComparer<(string State, string Event)>.Default);
    }

    public DslWorkflowInstance CreateInstance(
        string initialState,
        IReadOnlyDictionary<string, object?>? contextSnapshot = null)
    {
        if (string.IsNullOrWhiteSpace(initialState))
            throw new ArgumentException("Initial state is required.", nameof(initialState));

        if (!States.Contains(initialState, StringComparer.Ordinal))
            throw new InvalidOperationException($"State '{initialState}' is not defined in workflow '{Name}'.");

        return new DslWorkflowInstance(
            Name,
            Version,
            initialState,
            null,
            DateTimeOffset.UtcNow,
            contextSnapshot ?? EmptyContext.Instance);
    }

    public DslInstanceCompatibilityResult CheckCompatibility(DslWorkflowInstance instance)
    {
        if (!instance.WorkflowName.Equals(Name, StringComparison.Ordinal))
        {
            return DslInstanceCompatibilityResult.NotCompatible(
                $"Instance workflow '{instance.WorkflowName}' does not match compiled workflow '{Name}'.");
        }

        if (!instance.WorkflowVersion.Equals(Version, StringComparison.Ordinal))
        {
            return DslInstanceCompatibilityResult.NotCompatible(
                $"Instance workflow version '{instance.WorkflowVersion}' does not match compiled workflow version '{Version}'.");
        }

        if (!States.Contains(instance.CurrentState, StringComparer.Ordinal))
            return DslInstanceCompatibilityResult.NotCompatible($"Instance state '{instance.CurrentState}' is not defined in workflow '{Name}'.");

        return DslInstanceCompatibilityResult.Compatible();
    }

    public DslInspectionResult Inspect(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        if (string.IsNullOrWhiteSpace(currentState))
            throw new ArgumentException("Current state is required.", nameof(currentState));
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name is required.", nameof(eventName));

        if (!States.Contains(currentState, StringComparer.Ordinal))
            return DslInspectionResult.NotDefined(currentState, eventName, $"Unknown state '{currentState}'.");

        if (!Events.Any(e => e.Name.Equals(eventName, StringComparison.Ordinal)))
            return DslInspectionResult.NotDefined(currentState, eventName, $"Unknown event '{eventName}'.");

        if (!_transitionMap.TryGetValue((currentState, eventName), out var transitions) || transitions.Count == 0)
            return DslInspectionResult.NotDefined(currentState, eventName, $"No transition defined for event '{eventName}' in state '{currentState}'.");

        var runtimeContext = context ?? EmptyContext.Instance;
        var reasons = new List<string>();

        foreach (var transition in transitions)
        {
            if (string.IsNullOrWhiteSpace(transition.GuardExpression))
                return DslInspectionResult.Accepted(currentState, eventName, transition.ToState);

            var evaluation = _guardEvaluator.Evaluate(transition.GuardExpression, runtimeContext);
            if (evaluation.IsPassed)
                return DslInspectionResult.Accepted(currentState, eventName, transition.ToState);

            reasons.Add(evaluation.FailureReason ?? $"Guard '{transition.GuardExpression}' failed.");
        }

        return DslInspectionResult.Rejected(currentState, eventName, reasons);
    }

    public DslInspectionResult Inspect(
        DslWorkflowInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
            return DslInspectionResult.NotDefined(instance.CurrentState, eventName, compatibility.Reason!);

        var mergedContext = MergeContexts(instance.ContextSnapshot, context);
        return Inspect(instance.CurrentState, eventName, mergedContext);
    }

    public DslFireResult Fire(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        var inspection = Inspect(currentState, eventName, context);
        if (!inspection.IsDefined)
            return DslFireResult.NotDefined(currentState, eventName, inspection.Reasons);

        if (!inspection.IsAccepted || inspection.TargetState is null)
            return DslFireResult.Rejected(currentState, eventName, inspection.Reasons);

        return DslFireResult.Accepted(currentState, eventName, inspection.TargetState);
    }

    public DslInstanceFireResult Fire(
        DslWorkflowInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
        {
            var reasons = new[] { compatibility.Reason! };
            return DslInstanceFireResult.NotDefined(instance.CurrentState, eventName, reasons);
        }

        var mergedContext = MergeContexts(instance.ContextSnapshot, context);
        var fire = Fire(instance.CurrentState, eventName, mergedContext);
        if (!fire.IsDefined)
            return DslInstanceFireResult.NotDefined(instance.CurrentState, eventName, fire.Reasons);

        if (!fire.IsAccepted || fire.NewState is null)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, fire.Reasons);

        var updated = instance with
        {
            CurrentState = fire.NewState,
            LastEvent = eventName,
            UpdatedAt = DateTimeOffset.UtcNow,
            ContextSnapshot = mergedContext
        };

        return DslInstanceFireResult.Accepted(instance.CurrentState, eventName, fire.NewState, updated);
    }

    private static IReadOnlyDictionary<string, object?> MergeContexts(
        IReadOnlyDictionary<string, object?>? baseContext,
        IReadOnlyDictionary<string, object?>? overlayContext)
    {
        if ((baseContext is null || baseContext.Count == 0) && (overlayContext is null || overlayContext.Count == 0))
            return EmptyContext.Instance;

        var merged = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (baseContext is not null)
        {
            foreach (var kvp in baseContext)
                merged[kvp.Key] = kvp.Value;
        }

        if (overlayContext is not null)
        {
            foreach (var kvp in overlayContext)
                merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    private static string ComputeVersion(DslMachine machine)
    {
        var sb = new StringBuilder();
        sb.Append(machine.Name).Append('|');
        foreach (var state in machine.States)
            sb.Append("S:").Append(state).Append('|');
        foreach (var evt in machine.Events)
            sb.Append("E:").Append(evt.Name).Append(':').Append(evt.ArgumentType).Append('|');
        foreach (var transition in machine.Transitions)
        {
            sb.Append("T:")
              .Append(transition.FromState)
              .Append("->")
              .Append(transition.ToState)
              .Append('@')
              .Append(transition.EventName)
              .Append('?')
              .Append(transition.GuardExpression)
              .Append('|');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).Substring(0, 12);
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
    string WorkflowVersion,
    string CurrentState,
    string? LastEvent,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, object?> ContextSnapshot);

public sealed record DslInstanceCompatibilityResult(bool IsCompatible, string? Reason)
{
    internal static DslInstanceCompatibilityResult Compatible() => new(true, null);
    internal static DslInstanceCompatibilityResult NotCompatible(string reason) => new(false, reason);
}

internal sealed class EmptyContext : IReadOnlyDictionary<string, object?>
{
    internal static readonly EmptyContext Instance = new();

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
    bool IsDefined,
    bool IsAccepted,
    string CurrentState,
    string EventName,
    string? TargetState,
    IReadOnlyList<string> Reasons)
{
    internal static DslInspectionResult Accepted(string state, string evt, string target) =>
        new(true, true, state, evt, target, Array.Empty<string>());

    internal static DslInspectionResult NotDefined(string state, string evt, string reason) =>
        new(false, false, state, evt, null, new[] { reason });

    internal static DslInspectionResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(true, false, state, evt, null, reasons);
}

public sealed record DslFireResult(
    bool IsDefined,
    bool IsAccepted,
    string CurrentState,
    string EventName,
    string? NewState,
    IReadOnlyList<string> Reasons)
{
    internal static DslFireResult Accepted(string state, string evt, string newState) =>
        new(true, true, state, evt, newState, Array.Empty<string>());

    internal static DslFireResult NotDefined(string state, string evt, IReadOnlyList<string> reasons) =>
        new(false, false, state, evt, null, reasons);

    internal static DslFireResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(true, false, state, evt, null, reasons);
}

public sealed record DslInstanceFireResult(
    bool IsDefined,
    bool IsAccepted,
    string PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<string> Reasons,
    DslWorkflowInstance? UpdatedInstance)
{
    internal static DslInstanceFireResult Accepted(string state, string evt, string newState, DslWorkflowInstance updated) =>
        new(true, true, state, evt, newState, Array.Empty<string>(), updated);

    internal static DslInstanceFireResult NotDefined(string state, string evt, IReadOnlyList<string> reasons) =>
        new(false, false, state, evt, null, reasons, null);

    internal static DslInstanceFireResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(true, false, state, evt, null, reasons, null);
}
