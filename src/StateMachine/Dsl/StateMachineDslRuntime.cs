using System;
using System.Collections.Generic;
using System.Linq;

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
}

public sealed class DslWorkflowDefinition
{
    private readonly Dictionary<(string State, string Event), List<DslTransition>> _transitionMap;
    private readonly Dictionary<(string State, string Event), List<DslTerminalRule>> _terminalRuleMap;
    private readonly Dictionary<string, DslFieldContract> _dataContractMap;
    private readonly Dictionary<string, DslCollectionFieldContract> _collectionContractMap;
    private readonly Dictionary<string, Dictionary<string, DslFieldContract>> _eventArgContractMap;
    private readonly IGuardEvaluator _guardEvaluator;

    // Rules storage
    private readonly IReadOnlyList<DslRule> _topLevelRules;
    private readonly IReadOnlyList<DslRule> _allFieldRules; // field rules from all data fields
    private readonly IReadOnlyList<DslRule> _allCollectionRules; // field rules from collection fields
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DslRule>> _stateRules;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DslRule>> _eventRules;

    public string Name { get; }
    public IReadOnlyList<string> States { get; }
    public string InitialState { get; }
    public IReadOnlyList<DslEvent> Events { get; }
    public IReadOnlyList<DslFieldContract> DataFields { get; }
    public IReadOnlyList<DslCollectionFieldContract> CollectionFields { get; }

    internal DslWorkflowDefinition(DslMachine machine, IGuardEvaluator guardEvaluator)
    {
        Name = machine.Name;
        States = machine.States;
        InitialState = machine.InitialState;
        Events = machine.Events;
        DataFields = machine.DataFields;
        CollectionFields = machine.CollectionFields;
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

        _collectionContractMap = machine.CollectionFields
            .ToDictionary(f => f.Name, f => f, StringComparer.Ordinal);

        _eventArgContractMap = machine.Events
            .ToDictionary(
                evt => evt.Name,
                evt => evt.Args.ToDictionary(a => a.Name, a => a, StringComparer.Ordinal),
                StringComparer.Ordinal);

        // Flatten rules for runtime access
        _topLevelRules = machine.TopLevelRules ?? Array.Empty<DslRule>();
        _allFieldRules = machine.DataFields
            .Where(f => f.Rules is not null)
            .SelectMany(f => f.Rules!)
            .ToArray();
        _allCollectionRules = machine.CollectionFields
            .Where(f => f.Rules is not null)
            .SelectMany(f => f.Rules!)
            .ToArray();
        _stateRules = machine.StateRules
            ?? (IReadOnlyDictionary<string, IReadOnlyList<DslRule>>)new Dictionary<string, IReadOnlyList<DslRule>>(StringComparer.Ordinal);
        _eventRules = machine.Events
            .Where(e => e.Rules is not null && e.Rules.Count > 0)
            .ToDictionary(e => e.Name, e => e.Rules!, StringComparer.Ordinal);
    }

    public DslWorkflowInstance CreateInstance(
        IReadOnlyDictionary<string, object?>? instanceData = null)
        => CreateInstance(InitialState, instanceData);

    public DslWorkflowInstance CreateInstance(
        string initialState,
        IReadOnlyDictionary<string, object?>? instanceData = null)
    {
        if (string.IsNullOrWhiteSpace(initialState))
            throw new ArgumentException("Initial state is required.", nameof(initialState));

        if (!States.Contains(initialState, StringComparer.Ordinal))
            throw new InvalidOperationException($"State '{initialState}' is not defined in workflow '{Name}'.");

        var data = BuildInitialInstanceData(instanceData);
        if (!TryValidateDataContract(data, out var dataError))
            throw new InvalidOperationException(dataError);

        return new DslWorkflowInstance(
            Name,
            initialState,
            null,
            DateTimeOffset.UtcNow,
            data);
    }

    private IReadOnlyDictionary<string, object?> BuildInitialInstanceData(IReadOnlyDictionary<string, object?>? instanceData)
    {
        var hasDefaults = DataFields.Any(field => field.HasDefaultValue);
        var hasCollections = CollectionFields.Count > 0;
        if (!hasDefaults && !hasCollections && instanceData is null)
            return EmptyInstanceData.Instance;

        var merged = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in DataFields)
        {
            if (field.HasDefaultValue)
                merged[field.Name] = field.DefaultValue;
        }

        // Initialize collection fields as empty CollectionValues
        foreach (var collField in CollectionFields)
        {
            var collectionKey = $"__collection__{collField.Name}";
            merged[collectionKey] = new CollectionValue(collField.CollectionKind, collField.InnerType);
        }

        if (instanceData is not null)
        {
            foreach (var pair in instanceData)
            {
                // If caller provides a list for a collection field, load it
                if (_collectionContractMap.TryGetValue(pair.Key, out var collContract) && pair.Value is System.Collections.IEnumerable enumerable && pair.Value is not string)
                {
                    var collectionKey = $"__collection__{pair.Key}";
                    var collection = new CollectionValue(collContract.CollectionKind, collContract.InnerType);
                    var items = new List<object>();
                    foreach (var item in enumerable)
                    {
                        if (item is not null)
                            items.Add(item);
                    }
                    collection.LoadFrom(items);
                    merged[collectionKey] = collection;
                    continue;
                }

                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
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

        // Check event rules first
        var eventRuleViolations = EvaluateEventRules(eventName, evaluationData);
        if (eventRuleViolations.Count > 0)
            return DslInspectionResult.Rejected(currentState, eventName, eventRuleViolations);

        var resolution = ResolveTransition(currentState, eventName, evaluationData);

        return resolution.Kind switch
        {
            TransitionResolutionKind.Accepted => DslInspectionResult.Accepted(
                currentState,
                eventName,
                resolution.Transition!.ToState,
                GetRequiredEventArgumentKeys(eventName)),
            TransitionResolutionKind.NoTransition => DslInspectionResult.NoTransition(
                currentState,
                eventName),
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

        // Inspect is a discovery API — it intentionally accepts calls with missing/partial event arguments
        // so callers can determine required args via RequiredEventArgumentKeys before firing.
        // Full argument validation happens in Fire().
        if (eventArguments != null && !TryValidateEventArguments(eventName, eventArguments, out var eventArgError))
            return DslInspectionResult.Rejected(instance.CurrentState, eventName, new[] { eventArgError! });

        var evaluationArguments = BuildEvaluationData(instance.InstanceData, eventName, eventArguments);

        // Check event rules — only when caller provides event arguments.
        // When eventArguments is null this is a pure discovery call; event rules cannot be
        // evaluated without their required inputs, and RequiredEventArgumentKeys will inform
        // the caller which args are needed before firing.
        if (eventArguments != null)
        {
            var eventRuleViolations = EvaluateEventRules(eventName, evaluationArguments);
            if (eventRuleViolations.Count > 0)
                return DslInspectionResult.Rejected(instance.CurrentState, eventName, eventRuleViolations);
        }

        var resolution = ResolveTransition(instance.CurrentState, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.NotDefined)
            return DslInspectionResult.NotDefined(instance.CurrentState, eventName, resolution.NotDefinedReason!);

        if (resolution.Kind == TransitionResolutionKind.NoTransition)
            return DslInspectionResult.NoTransition(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.Transition is null)
            return DslInspectionResult.Rejected(instance.CurrentState, eventName, resolution.Reasons);

        // Simulate set assignments and check field/top-level/state rules
        var simulatedData = new Dictionary<string, object?>(instance.InstanceData, StringComparer.Ordinal);
        var simulatedCollections = CloneCollections(simulatedData);

        foreach (var assignment in resolution.Transition.SetAssignments)
        {
            var ctx = BuildEvaluationDataWithCollections(simulatedData, simulatedCollections, eventName, eventArguments);
            var eval = DslExpressionRuntimeEvaluator.Evaluate(assignment.Expression, ctx);
            if (eval.Success)
                simulatedData[assignment.Key] = eval.Value;
        }

        if (resolution.Transition.CollectionMutations is { } mutations)
            ExecuteCollectionMutations(mutations, simulatedCollections, simulatedData, eventName, eventArguments);

        CommitCollections(simulatedData, simulatedCollections);

        var dataRuleViolations = EvaluateDataRules(simulatedData);
        if (dataRuleViolations.Count > 0)
            return DslInspectionResult.Rejected(instance.CurrentState, eventName, dataRuleViolations);

        var targetState = resolution.Transition.ToState;
        var stateRuleViolations = EvaluateStateRules(targetState, simulatedData);
        if (stateRuleViolations.Count > 0)
            return DslInspectionResult.Rejected(instance.CurrentState, eventName, stateRuleViolations);

        return DslInspectionResult.Accepted(
            instance.CurrentState,
            eventName,
            targetState,
            GetRequiredEventArgumentKeys(eventName));
    }

    public DslFireResult Fire(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var inspection = Inspect(currentState, eventName, eventArguments);
        if (!inspection.IsDefined)
            return DslFireResult.NotDefined(currentState, eventName, inspection.Reasons);

        if (inspection.Outcome == DslOutcomeKind.NoTransition)
            return DslFireResult.NoTransition(currentState, eventName);

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

        // Stage 1: Event rules (checked before guard evaluation)
        var eventRuleViolations = EvaluateEventRules(eventName, evaluationArguments);
        if (eventRuleViolations.Count > 0)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, eventRuleViolations);

        // Stage 2: Guard evaluation (resolve transition)
        var resolution = ResolveTransition(instance.CurrentState, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.NotDefined)
            return DslInstanceFireResult.NotDefined(instance.CurrentState, eventName, new[] { resolution.NotDefinedReason! });

        if (resolution.Kind == TransitionResolutionKind.NoTransition)
        {
            var noTransitionData = new Dictionary<string, object?>(instance.InstanceData, StringComparer.Ordinal);

            // Deep-clone collections for working copy
            var workingCollections = CloneCollections(noTransitionData);

            if (resolution.TerminalRule?.SetAssignments is { } noTransitionSets)
            {
                foreach (var assignment in noTransitionSets)
                {
                    var assignmentContext = BuildEvaluationDataWithCollections(noTransitionData, workingCollections, eventName, eventArguments);
                    var assignmentEvaluation = DslExpressionRuntimeEvaluator.Evaluate(assignment.Expression, assignmentContext);
                    if (!assignmentEvaluation.Success)
                        return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { $"Data assignment failed: {assignmentEvaluation.Error}" });

                    if (!TryValidateAssignedValue(assignment.Key, assignmentEvaluation.Value, out var contractError))
                        return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { contractError! });

                    noTransitionData[assignment.Key] = assignmentEvaluation.Value;
                }
            }

            // Execute collection mutations
            if (resolution.TerminalRule?.CollectionMutations is { } noTransitionMutations)
            {
                var mutationError = ExecuteCollectionMutations(noTransitionMutations, workingCollections, noTransitionData, eventName, eventArguments);
                if (mutationError is not null)
                    return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { mutationError });
            }

            // Commit working collections back to data
            CommitCollections(noTransitionData, workingCollections);

            // Note: no-transition does NOT trigger state rules

            var noTransitionUpdated = instance with
            {
                LastEvent = eventName,
                UpdatedAt = DateTimeOffset.UtcNow,
                InstanceData = noTransitionData
            };

            return DslInstanceFireResult.NoTransition(instance.CurrentState, eventName, noTransitionUpdated);
        }

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.Transition is null)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, resolution.Reasons);

        // Stage 3: Set execution (on working copy)
        var updatedData = new Dictionary<string, object?>(instance.InstanceData, StringComparer.Ordinal);

        // Deep-clone collections for working copy
        var transitionCollections = CloneCollections(updatedData);

        foreach (var assignment in resolution.Transition.SetAssignments)
        {
            var assignmentContext = BuildEvaluationDataWithCollections(updatedData, transitionCollections, eventName, eventArguments);
            var assignmentEvaluation = DslExpressionRuntimeEvaluator.Evaluate(assignment.Expression, assignmentContext);
            if (!assignmentEvaluation.Success)
                return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { $"Data assignment failed: {assignmentEvaluation.Error}" });

            if (!TryValidateAssignedValue(assignment.Key, assignmentEvaluation.Value, out var contractError))
                return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { contractError! });

            updatedData[assignment.Key] = assignmentEvaluation.Value;
        }

        // Execute collection mutations
        if (resolution.Transition.CollectionMutations is { } transitionMutations)
        {
            var mutationError = ExecuteCollectionMutations(transitionMutations, transitionCollections, updatedData, eventName, eventArguments);
            if (mutationError is not null)
                return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, new[] { mutationError });
        }

        // Commit working collections to updatedData for rule evaluation
        CommitCollections(updatedData, transitionCollections);

        // Stage 4: Field and top-level rules (checked against post-set data; rollback on failure)
        var dataRuleViolations = EvaluateDataRules(updatedData);
        if (dataRuleViolations.Count > 0)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, dataRuleViolations);

        // Stage 5: State rules (only checked on state transition, including self-transitions)
        var targetState = resolution.Transition.ToState;
        var stateRuleViolations = EvaluateStateRules(targetState, updatedData);
        if (stateRuleViolations.Count > 0)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, stateRuleViolations);

        var updated = instance with
        {
            CurrentState = targetState,
            LastEvent = eventName,
            UpdatedAt = DateTimeOffset.UtcNow,
            InstanceData = updatedData
        };

        return DslInstanceFireResult.Accepted(instance.CurrentState, eventName, targetState, updated);
    }

    /// <summary>
    /// Evaluates all field rules, top-level rules, and the current state's entry rules against the
    /// instance's current data. Returns a flat list of violated rule reason strings. An empty list
    /// means all rules are satisfied. Never throws — violations are collected without short-circuiting.
    /// </summary>
    public IReadOnlyList<string> EvaluateCurrentRules(DslWorkflowInstance instance)
    {
        var violations = new List<string>();
        violations.AddRange(EvaluateDataRules(instance.InstanceData));
        violations.AddRange(EvaluateStateRules(instance.CurrentState, instance.InstanceData));
        return violations;
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
            DslTerminalKind.NoTransition => TransitionResolution.NoTransition(terminalRule),
            DslTerminalKind.Reject => TransitionResolution.Rejected(new[]
            {
                string.IsNullOrWhiteSpace(terminalRule.Reason)
                    ? (guardFailureReasons.FirstOrDefault() ?? $"No transition for '{eventName}' from '{currentState}'.")
                    : terminalRule.Reason!
            }),
            _ => TransitionResolution.Rejected(guardFailureReasons)
        };
    }

    private IReadOnlyList<string> GetRequiredEventArgumentKeys(string eventName)
    {
        if (!_eventArgContractMap.TryGetValue(eventName, out var argContract) || argContract.Count == 0)
            return Array.Empty<string>();

        return argContract.Values
            .Where(a => !a.IsNullable && !a.HasDefaultValue)
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

        // Inject collection backing values so guards can reference .count, .min, etc.
        foreach (var col in _collectionContractMap.Values)
        {
            var backingKey = $"__collection__{col.Name}";
            if (instanceData.TryGetValue(backingKey, out var existing) && existing is CollectionValue)
            {
                // Already in dict from instanceData copy
            }
            else
            {
                evaluation[backingKey] = new CollectionValue(col.CollectionKind, col.InnerType);
            }
        }

        if (eventArguments is not null)
        {
            foreach (var kvp in eventArguments)
            {
                // Inject prefixed form unconditionally; inject bare form only if it
                // does not shadow an existing instance data field (instance data takes precedence).
                evaluation[$"{eventName}.{kvp.Key}"] = kvp.Value;
                if (!kvp.Key.Contains('.', StringComparison.Ordinal) && !instanceData.ContainsKey(kvp.Key))
                    evaluation[kvp.Key] = kvp.Value;
            }
        }

        // Inject default values for any declared args not supplied by caller.
        if (_eventArgContractMap.TryGetValue(eventName, out var argContract))
        {
            foreach (var arg in argContract.Values)
            {
                var key = $"{eventName}.{arg.Name}";
                if (!evaluation.ContainsKey(key))
                {
                    if (arg.HasDefaultValue)
                    {
                        evaluation[key] = arg.DefaultValue;
                        if (!instanceData.ContainsKey(arg.Name))
                            evaluation[arg.Name] = arg.DefaultValue;
                    }
                    else if (arg.IsNullable)
                    {
                        evaluation[key] = null;
                        if (!instanceData.ContainsKey(arg.Name))
                            evaluation[arg.Name] = null;
                    }
                }
            }
        }

        return evaluation;
    }

    private IReadOnlyDictionary<string, object?> BuildDirectEvaluationData(
        string eventName,
        IReadOnlyDictionary<string, object?>? values)
    {
        var evaluation = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (values is not null)
        {
            foreach (var kvp in values)
            {
                if (kvp.Key.Contains('.', StringComparison.Ordinal))
                    evaluation[kvp.Key] = kvp.Value;

                evaluation[kvp.Key] = kvp.Value;
                evaluation[$"{eventName}.{kvp.Key}"] = kvp.Value;
            }
        }

        // Inject default values for any declared args not supplied by caller.
        if (_eventArgContractMap.TryGetValue(eventName, out var argContract))
        {
            foreach (var arg in argContract.Values)
            {
                var key = $"{eventName}.{arg.Name}";
                if (!evaluation.ContainsKey(key))
                {
                    if (arg.HasDefaultValue)
                    {
                        evaluation[arg.Name] = arg.DefaultValue;
                        evaluation[key] = arg.DefaultValue;
                    }
                    else if (arg.IsNullable)
                    {
                        evaluation[arg.Name] = null;
                        evaluation[key] = null;
                    }
                }
            }
        }

        return evaluation;
    }

    private IReadOnlyDictionary<string, object?> BuildEvaluationDataWithCollections(
        Dictionary<string, object?> data,
        Dictionary<string, CollectionValue> workingCollections,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments)
    {
        var evaluation = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var kvp in data)
        {
            if (!kvp.Key.StartsWith("__collection__", StringComparison.Ordinal))
                evaluation[kvp.Key] = kvp.Value;
        }

        // Inject working-copy collections so set-expressions see read-your-writes
        foreach (var kvp in workingCollections)
            evaluation[$"__collection__{kvp.Key}"] = kvp.Value;

        if (eventArguments is not null)
        {
            foreach (var kvp in eventArguments)
            {
                // Inject prefixed form unconditionally; inject bare form only if it
                // does not shadow an existing data field (data takes precedence).
                evaluation[$"{eventName}.{kvp.Key}"] = kvp.Value;
                if (!kvp.Key.Contains('.', StringComparison.Ordinal) && !data.ContainsKey(kvp.Key))
                    evaluation[kvp.Key] = kvp.Value;
            }
        }

        // Inject default values for any declared args not supplied by caller.
        if (_eventArgContractMap.TryGetValue(eventName, out var argContract))
        {
            foreach (var arg in argContract.Values)
            {
                var key = $"{eventName}.{arg.Name}";
                if (!evaluation.ContainsKey(key))
                {
                    if (arg.HasDefaultValue)
                    {
                        evaluation[key] = arg.DefaultValue;
                        if (!data.ContainsKey(arg.Name))
                            evaluation[arg.Name] = arg.DefaultValue;
                    }
                    else if (arg.IsNullable)
                    {
                        evaluation[key] = null;
                        if (!data.ContainsKey(arg.Name))
                            evaluation[arg.Name] = null;
                    }
                }
            }
        }

        return evaluation;
    }

    // ---- Rule evaluation helpers ----

    private IReadOnlyList<string> EvaluateEventRules(string eventName, IReadOnlyDictionary<string, object?> evaluationData)
    {
        if (!_eventRules.TryGetValue(eventName, out var rules) || rules.Count == 0)
            return Array.Empty<string>();

        var violations = new List<string>();
        foreach (var rule in rules)
        {
            var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, evaluationData);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
                violations.Add(rule.Reason);
        }
        return violations;
    }

    private IReadOnlyList<string> EvaluateDataRules(IReadOnlyDictionary<string, object?> data)
    {
        var violations = new List<string>();

        foreach (var rule in _allFieldRules)
        {
            var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, data);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
                violations.Add(rule.Reason);
        }

        foreach (var rule in _allCollectionRules)
        {
            var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, data);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
                violations.Add(rule.Reason);
        }

        foreach (var rule in _topLevelRules)
        {
            var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, data);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
                violations.Add(rule.Reason);
        }

        return violations;
    }

    private IReadOnlyList<string> EvaluateStateRules(string state, IReadOnlyDictionary<string, object?> data)
    {
        if (!_stateRules.TryGetValue(state, out var rules) || rules.Count == 0)
            return Array.Empty<string>();

        var violations = new List<string>();
        foreach (var rule in rules)
        {
            var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, data);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
                violations.Add(rule.Reason);
        }
        return violations;
    }

    private Dictionary<string, CollectionValue> CloneCollections(Dictionary<string, object?> data)
    {
        var clones = new Dictionary<string, CollectionValue>(StringComparer.Ordinal);
        foreach (var col in _collectionContractMap.Values)
        {
            var backingKey = $"__collection__{col.Name}";
            if (data.TryGetValue(backingKey, out var val) && val is CollectionValue cv)
                clones[col.Name] = cv.Clone();
            else
                clones[col.Name] = new CollectionValue(col.CollectionKind, col.InnerType);
        }
        return clones;
    }

    private string? ExecuteCollectionMutations(
        IReadOnlyList<DslCollectionMutation> mutations,
        Dictionary<string, CollectionValue> workingCollections,
        Dictionary<string, object?> data,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments)
    {
        foreach (var mutation in mutations)
        {
            if (!workingCollections.TryGetValue(mutation.TargetField, out var collection))
                return $"Collection mutation failed: unknown collection field '{mutation.TargetField}'.";

            object? argValue = null;
            if (mutation.Expression is not null)
            {
                var ctx = BuildEvaluationDataWithCollections(data, workingCollections, eventName, eventArguments);
                var result = DslExpressionRuntimeEvaluator.Evaluate(mutation.Expression, ctx);
                if (!result.Success)
                    return $"Collection mutation failed: {result.Error}";
                argValue = result.Value;
            }

            switch (mutation.Verb)
            {
                case DslCollectionMutationVerb.Add:
                    collection.Add(argValue!);
                    break;
                case DslCollectionMutationVerb.Remove:
                    collection.Remove(argValue!);
                    break;
                case DslCollectionMutationVerb.Enqueue:
                    collection.Enqueue(argValue!);
                    break;
                case DslCollectionMutationVerb.Dequeue:
                    if (collection.Count == 0)
                        return $"Collection mutation failed: cannot dequeue from empty queue '{mutation.TargetField}'.";
                    if (mutation.IntoField is not null)
                        data[mutation.IntoField] = collection.Peek();
                    collection.Dequeue();
                    break;
                case DslCollectionMutationVerb.Push:
                    collection.Push(argValue!);
                    break;
                case DslCollectionMutationVerb.Pop:
                    if (collection.Count == 0)
                        return $"Collection mutation failed: cannot pop from empty stack '{mutation.TargetField}'.";
                    if (mutation.IntoField is not null)
                        data[mutation.IntoField] = collection.Peek();
                    collection.Pop();
                    break;
                case DslCollectionMutationVerb.Clear:
                    collection.Clear();
                    break;
            }
        }
        return null;
    }

    private static void CommitCollections(Dictionary<string, object?> data, Dictionary<string, CollectionValue> workingCollections)
    {
        foreach (var kvp in workingCollections)
            data[$"__collection__{kvp.Key}"] = kvp.Value;
    }

    private bool TryValidateDataContract(IReadOnlyDictionary<string, object?> data, out string error)
    {
        if (_dataContractMap.Count == 0 && _collectionContractMap.Count == 0)
        {
            error = string.Empty;
            return true;
        }

        foreach (var key in data.Keys)
        {
            // Skip internal collection backing keys
            if (key.StartsWith("__collection__", StringComparison.Ordinal))
                continue;

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
                if (arg.HasDefaultValue || arg.IsNullable)
                    continue;

                error = $"Event argument validation failed: required argument '{arg.Name}' for event '{eventName}' is missing.";
                return false;
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

    private enum TransitionResolutionKind
    {
        Accepted,
        Rejected,
        NotDefined,
        NoTransition
    }

    private sealed record TransitionResolution(
        TransitionResolutionKind Kind,
        DslTransition? Transition,
        DslTerminalRule? TerminalRule,
        string? NotDefinedReason,
        IReadOnlyList<string> Reasons)
    {
        internal static TransitionResolution Accepted(DslTransition transition) =>
            new(TransitionResolutionKind.Accepted, transition, null, null, Array.Empty<string>());

        internal static TransitionResolution Rejected(IReadOnlyList<string> reasons) =>
            new(TransitionResolutionKind.Rejected, null, null, null, reasons);

        internal static TransitionResolution NotDefined(string reason) =>
            new(TransitionResolutionKind.NotDefined, null, null, reason, new[] { reason });

        internal static TransitionResolution NoTransition(DslTerminalRule terminalRule) =>
            new(TransitionResolutionKind.NoTransition, null, terminalRule, null, Array.Empty<string>());
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

        if (string.IsNullOrWhiteSpace(machine.InitialState))
            throw new InvalidOperationException("Exactly one state must be marked initial. Use 'state <Name> initial'.");

        if (!machine.States.Contains(machine.InitialState, StringComparer.Ordinal))
            throw new InvalidOperationException($"Initial state '{machine.InitialState}' is not defined in workflow '{machine.Name}'.");

        // Compile-time rule validations
        ValidateRulesAtCompileTime(machine);

        return new DslWorkflowDefinition(machine, guardEvaluator ?? new DefaultGuardEvaluator());
    }

    private static void ValidateRulesAtCompileTime(DslMachine machine)
    {
        // Build initial instance data (using field defaults)
        var defaultData = BuildDefaultData(machine);

        // 1. Validate field rules against default values
        foreach (var field in machine.DataFields)
        {
            if (field.Rules is null || field.Rules.Count == 0)
                continue;

            // Only check at compile time when there is an explicit default value to validate against.
            // Nullable fields without defaults have no fixed initial value to check here.
            if (!field.HasDefaultValue)
                continue;

            foreach (var rule in field.Rules)
            {
                var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, defaultData);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                    throw new InvalidOperationException($"Line {rule.SourceLine}: compile-time rule violation: rule \"{rule.Reason}\" on field '{field.Name}' is violated by the field's default value.");
            }
        }

        // 2. Validate collection rules at creation (collections start empty; count=0, contains=false)
        foreach (var col in machine.CollectionFields)
        {
            if (col.Rules is null || col.Rules.Count == 0)
                continue;

            var colCtx = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [$"__collection__{col.Name}"] = new CollectionValue(col.CollectionKind, col.InnerType)
            };
            // Also add empty collection proxy for collection identifier expressions
            foreach (var pair in defaultData)
                colCtx[pair.Key] = pair.Value;

            foreach (var rule in col.Rules)
            {
                var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, colCtx);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                    throw new InvalidOperationException($"Line {rule.SourceLine}: compile-time rule violation: rule \"{rule.Reason}\" on collection field '{col.Name}' is violated at creation (collection starts empty).");
            }
        }

        // 3. Validate top-level rules against default values
        if (machine.TopLevelRules is not null)
        {
            foreach (var rule in machine.TopLevelRules)
            {
                var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, defaultData);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                    throw new InvalidOperationException($"Line {rule.SourceLine}: compile-time rule violation: top-level rule \"{rule.Reason}\" is violated by default field values.");
            }
        }

        // 4. Validate initial state entry rules against default data
        if (machine.StateRules is not null && machine.StateRules.TryGetValue(machine.InitialState, out var initialStateRules))
        {
            foreach (var rule in initialStateRules)
            {
                var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, defaultData);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                    throw new InvalidOperationException($"Line {rule.SourceLine}: compile-time rule violation: state rule \"{rule.Reason}\" on initial state '{machine.InitialState}' is violated by default data.");
            }
        }

        // 5. Validate event rules against event argument defaults
        foreach (var evt in machine.Events)
        {
            if (evt.Rules is null || evt.Rules.Count == 0)
                continue;

            var eventDefaults = new Dictionary<string, object?>(StringComparer.Ordinal);
            bool allArgsHaveDefaults = true;
            foreach (var arg in evt.Args)
            {
                if (arg.HasDefaultValue)
                {
                    // Inject both bare and prefixed forms, matching BuildDirectEvaluationData
                    eventDefaults[arg.Name] = arg.DefaultValue;
                    eventDefaults[$"{evt.Name}.{arg.Name}"] = arg.DefaultValue;
                }
                else if (arg.IsNullable)
                {
                    eventDefaults[arg.Name] = null;
                    eventDefaults[$"{evt.Name}.{arg.Name}"] = null;
                }
                else
                {
                    allArgsHaveDefaults = false;
                    break;
                }
            }

            if (!allArgsHaveDefaults)
                continue;

            foreach (var rule in evt.Rules)
            {
                var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, eventDefaults);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                    throw new InvalidOperationException($"Line {rule.SourceLine}: compile-time rule violation: event rule \"{rule.Reason}\" on event '{evt.Name}' is violated by default argument values.");
            }
        }

        // 6. Warn about untargeted states with entry rules (emitted as compile-time warning via InvalidOperationException hint)
        // This is a warning not an error per design. We'll skip throwing but leave as a comment for future diagnostics.
        // 7. Validate literal set assignments against field and top-level rules
        ValidateLiteralSetAssignments(machine, defaultData);
    }

    private static void ValidateLiteralSetAssignments(DslMachine machine, IReadOnlyDictionary<string, object?> defaultData)
    {
        var fieldRuleMap = machine.DataFields
            .Where(f => f.Rules is not null && f.Rules.Count > 0)
            .ToDictionary(f => f.Name, f => f.Rules!, StringComparer.Ordinal);

        var topLevelRules = machine.TopLevelRules ?? Array.Empty<DslRule>();

        void CheckLiteralAssignment(DslSetAssignment assignment)
        {
            // Try to evaluate the assignment expression as a constant (no field references needed).
            // This catches literal numbers, strings, bools, null, and also constant expressions like -1.
            var constantEval = DslExpressionRuntimeEvaluator.Evaluate(assignment.Expression, EmptyInstanceData.Instance);
            if (!constantEval.Success)
                return; // Expression depends on runtime data — cannot check at compile time

            var assignedValue = constantEval.Value;

            if (!fieldRuleMap.TryGetValue(assignment.Key, out var fieldRules) && topLevelRules.Count == 0)
                return;

            // Build data context with the constant value in place of the field's default
            var ctx = new Dictionary<string, object?>(defaultData, StringComparer.Ordinal)
            {
                [assignment.Key] = assignedValue
            };

            if (fieldRuleMap.TryGetValue(assignment.Key, out fieldRules))
            {
                foreach (var rule in fieldRules)
                {
                    var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, ctx);
                    if (!result.Success || result.Value is not bool boolVal || !boolVal)
                        throw new InvalidOperationException($"Line {assignment.SourceLine}: literal assignment 'set {assignment.Key} = {assignment.ExpressionText}' violates rule \"{rule.Reason}\" on field '{assignment.Key}'.");
                }
            }

            foreach (var rule in topLevelRules)
            {
                var result = DslExpressionRuntimeEvaluator.Evaluate(rule.Expression, ctx);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                    throw new InvalidOperationException($"Line {assignment.SourceLine}: literal assignment 'set {assignment.Key} = {assignment.ExpressionText}' violates top-level rule \"{rule.Reason}\".");
            }
        }

        foreach (var transition in machine.Transitions)
        {
            foreach (var assignment in transition.SetAssignments)
                CheckLiteralAssignment(assignment);
        }

        foreach (var terminalRule in machine.TerminalRules)
        {
            if (terminalRule.SetAssignments is null) continue;
            foreach (var assignment in terminalRule.SetAssignments)
                CheckLiteralAssignment(assignment);
        }
    }

    private static IReadOnlyDictionary<string, object?> BuildDefaultData(DslMachine machine)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in machine.DataFields)
        {
            if (field.HasDefaultValue)
                data[field.Name] = field.DefaultValue;
        }
        foreach (var col in machine.CollectionFields)
        {
            data[$"__collection__{col.Name}"] = new CollectionValue(col.CollectionKind, col.InnerType);
        }
        return data;
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

    internal static DslInspectionResult NoTransition(string state, string evt) =>
        new(DslOutcomeKind.NoTransition, true, true, state, evt, state, Array.Empty<string>(), Array.Empty<string>());

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

    internal static DslFireResult NoTransition(string state, string evt) =>
        new(DslOutcomeKind.NoTransition, true, true, state, evt, state, Array.Empty<string>());

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

    internal static DslInstanceFireResult NoTransition(string state, string evt, DslWorkflowInstance updated) =>
        new(DslOutcomeKind.NoTransition, true, true, state, evt, state, Array.Empty<string>(), updated);

    internal static DslInstanceFireResult NotDefined(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Undefined, false, false, state, evt, null, reasons, null);

    internal static DslInstanceFireResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Blocked, true, false, state, evt, null, reasons, null);
}

public enum DslOutcomeKind
{
    Undefined,
    Blocked,
    Enabled,
    NoTransition
}
