using System;
using System.Collections.Generic;
using System.Linq;

namespace StateMachine.Dsl;

public sealed class DslWorkflowDefinition
{
    private readonly Dictionary<(string State, string Event), DslTransition> _transitionMap;
    private readonly Dictionary<string, DslField> _fieldMap;
    private readonly Dictionary<string, DslCollectionField> _collectionFieldMap;
    private readonly Dictionary<string, Dictionary<string, DslEventArg>> _eventArgContractMap;

    // Rules storage
    private readonly IReadOnlyList<DslRule> _topLevelRules;
    private readonly IReadOnlyList<DslRule> _allFieldRules;
    private readonly IReadOnlyList<DslRule> _allCollectionRules;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DslRule>> _stateRuleMap;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DslRule>> _eventRules;

    public string Name { get; }
    public IReadOnlyList<string> States { get; }
    public string InitialState { get; }
    public IReadOnlyList<DslEvent> Events { get; }
    public IReadOnlyList<DslField> Fields { get; }
    public IReadOnlyList<DslCollectionField> CollectionFields { get; }

    internal DslWorkflowDefinition(DslMachine machine)
    {
        Name = machine.Name;
        States = machine.States.Select(s => s.Name).ToArray();
        InitialState = machine.InitialState.Name;
        Events = machine.Events;
        Fields = machine.Fields;
        CollectionFields = machine.CollectionFields;

        _transitionMap = machine.Transitions
            .ToDictionary(
                t => (t.FromState, t.EventName),
                t => t,
                EqualityComparer<(string State, string Event)>.Default);

        _fieldMap = machine.Fields
            .ToDictionary(f => f.Name, f => f, StringComparer.Ordinal);

        _collectionFieldMap = machine.CollectionFields
            .ToDictionary(f => f.Name, f => f, StringComparer.Ordinal);

        _eventArgContractMap = machine.Events
            .ToDictionary(
                evt => evt.Name,
                evt => evt.Args.ToDictionary(a => a.Name, a => a, StringComparer.Ordinal),
                StringComparer.Ordinal);

        // Flatten rules for runtime access
        _topLevelRules = machine.TopLevelRules ?? Array.Empty<DslRule>();
        _allFieldRules = machine.Fields
            .Where(f => f.Rules is not null)
            .SelectMany(f => f.Rules!)
            .ToArray();
        _allCollectionRules = machine.CollectionFields
            .Where(f => f.Rules is not null)
            .SelectMany(f => f.Rules!)
            .ToArray();
        _stateRuleMap = machine.States
            .Where(s => s.Rules is not null && s.Rules.Count > 0)
            .ToDictionary(s => s.Name, s => s.Rules!, StringComparer.Ordinal);
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
        var hasDefaults = Fields.Any(field => field.HasDefaultValue);
        var hasCollections = CollectionFields.Count > 0;
        if (!hasDefaults && !hasCollections && instanceData is null)
            return EmptyInstanceData.Instance;

        var merged = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in Fields)
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
                if (_collectionFieldMap.TryGetValue(pair.Key, out var collContract) && pair.Value is System.Collections.IEnumerable enumerable && pair.Value is not string)
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
                ((DslStateTransition)resolution.Clause!.Outcome).TargetState,
                GetRequiredEventArgumentKeys(eventName)),
            TransitionResolutionKind.NotApplicable => DslInspectionResult.NotApplicable(currentState, eventName),
            TransitionResolutionKind.NoTransition => DslInspectionResult.AcceptedInPlace(currentState, eventName),
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

        // Fast-path: if this (state, event) pair has a 'when' predicate, evaluate it using only
        // instance data before doing argument validation. This ensures callers who omit event
        // arguments (e.g. bulk discover-mode refresh) still get NotApplicable — not Rejected —
        // when the 'when' guard is false. The 'when' predicate typically references machine fields
        // only, so evaluating without event args is correct and safe.
        if (_transitionMap.TryGetValue((instance.CurrentState, eventName), out var preCheckTransition) &&
            preCheckTransition.PredicateAst is not null)
        {
            var instanceOnlyContext = BuildEvaluationData(instance.InstanceData, eventName, null);
            var whenResult = DslExpressionRuntimeEvaluator.Evaluate(preCheckTransition.PredicateAst, instanceOnlyContext);
            if (!whenResult.Success || whenResult.Value is not bool whenBool || !whenBool)
                return DslInspectionResult.NotApplicable(instance.CurrentState, eventName);
        }

        // Inspect is a discovery API — it intentionally accepts calls with missing/partial event arguments
        // so callers can determine required args via RequiredEventArgumentKeys before firing.
        // Full argument validation happens in Fire().
        if (eventArguments != null && !TryValidateEventArguments(eventName, eventArguments, out var eventArgError))
            return DslInspectionResult.Rejected(instance.CurrentState, eventName, new[] { eventArgError! });

        var evaluationArguments = BuildEvaluationData(instance.InstanceData, eventName, eventArguments);

        // Check event rules — only when caller provides event arguments.
        if (eventArguments != null)
        {
            var eventRuleViolations = EvaluateEventRules(eventName, BuildDirectEvaluationData(eventName, eventArguments));
            if (eventRuleViolations.Count > 0)
                return DslInspectionResult.Rejected(instance.CurrentState, eventName, eventRuleViolations);
        }

        var resolution = ResolveTransition(instance.CurrentState, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.NotDefined)
            return DslInspectionResult.NotDefined(instance.CurrentState, eventName, resolution.NotDefinedReason!);

        if (resolution.Kind == TransitionResolutionKind.NotApplicable)
            return DslInspectionResult.NotApplicable(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.NoTransition)
            return DslInspectionResult.AcceptedInPlace(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.Clause is null)
            return DslInspectionResult.Rejected(instance.CurrentState, eventName, resolution.Reasons);

        // Simulate set assignments and check field/top-level/state rules
        var simulatedData = new Dictionary<string, object?>(instance.InstanceData, StringComparer.Ordinal);
        var simulatedCollections = CloneCollections(simulatedData);

        foreach (var assignment in resolution.Clause.SetAssignments)
        {
            var ctx = BuildEvaluationDataWithCollections(simulatedData, simulatedCollections, eventName, eventArguments);
            var eval = DslExpressionRuntimeEvaluator.Evaluate(assignment.Expression, ctx);
            if (eval.Success)
                simulatedData[assignment.Key] = eval.Value;
        }

        if (resolution.Clause.CollectionMutations is { } mutations)
            ExecuteCollectionMutations(mutations, simulatedCollections, simulatedData, eventName, eventArguments);

        CommitCollections(simulatedData, simulatedCollections);

        var dataRuleViolations = EvaluateDataRules(simulatedData);
        if (dataRuleViolations.Count > 0)
            return DslInspectionResult.Rejected(instance.CurrentState, eventName, dataRuleViolations);

        var targetState = ((DslStateTransition)resolution.Clause.Outcome).TargetState;
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
        if (inspection.Outcome == DslOutcomeKind.NotDefined)
            return DslFireResult.NotDefined(currentState, eventName, inspection.Reasons);

        if (inspection.Outcome == DslOutcomeKind.AcceptedInPlace)
            return DslFireResult.AcceptedInPlace(currentState, eventName);

        if (inspection.Outcome != DslOutcomeKind.Accepted || inspection.TargetState is null)
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
        // Use an args-only context so machine fields with the same name as an event arg cannot
        // shadow the arg value (e.g. CreditScore field must not override Submit.CreditScore).
        var eventRuleViolations = EvaluateEventRules(eventName, BuildDirectEvaluationData(eventName, eventArguments));
        if (eventRuleViolations.Count > 0)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, eventRuleViolations);

        // Stage 2: Guard evaluation (resolve transition)
        var resolution = ResolveTransition(instance.CurrentState, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.NotDefined)
            return DslInstanceFireResult.NotDefined(instance.CurrentState, eventName, new[] { resolution.NotDefinedReason! });

        if (resolution.Kind == TransitionResolutionKind.NotApplicable)
            return DslInstanceFireResult.NotApplicable(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.NoTransition)
        {
            var noTransitionData = new Dictionary<string, object?>(instance.InstanceData, StringComparer.Ordinal);

            // Deep-clone collections for working copy
            var workingCollections = CloneCollections(noTransitionData);

            if (resolution.Clause?.SetAssignments is { } noTransitionSets)
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
            if (resolution.Clause?.CollectionMutations is { } noTransitionMutations)
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

            return DslInstanceFireResult.AcceptedInPlace(instance.CurrentState, eventName, noTransitionUpdated);
        }

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.Clause is null)
            return DslInstanceFireResult.Rejected(instance.CurrentState, eventName, resolution.Reasons);

        // Stage 3: Set execution (on working copy)
        var updatedData = new Dictionary<string, object?>(instance.InstanceData, StringComparer.Ordinal);

        // Deep-clone collections for working copy
        var transitionCollections = CloneCollections(updatedData);

        foreach (var assignment in resolution.Clause.SetAssignments)
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
        if (resolution.Clause.CollectionMutations is { } transitionMutations)
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
        var targetState = ((DslStateTransition)resolution.Clause.Outcome).TargetState;
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

        if (!_transitionMap.TryGetValue((currentState, eventName), out var transition))
            return TransitionResolution.NotDefined($"No transition for '{eventName}' from '{currentState}'.");

        // Evaluate 'when' predicate — if false, the entire block is not applicable
        if (transition.PredicateAst is not null)
        {
            var whenResult = DslExpressionRuntimeEvaluator.Evaluate(transition.PredicateAst, evaluationData);
            if (!whenResult.Success || whenResult.Value is not bool whenBool || !whenBool)
                return TransitionResolution.NotApplicable();
        }

        var reasons = new List<string>();
        foreach (var clause in transition.Clauses)
        {
            // Evaluate clause guard predicate (if/else if)
            if (clause.PredicateAst is not null)
            {
                var guardResult = DslExpressionRuntimeEvaluator.Evaluate(clause.PredicateAst, evaluationData);
                if (!guardResult.Success || guardResult.Value is not bool guardBool || !guardBool)
                {
                    reasons.Add(clause.Predicate is not null
                        ? $"Guard '{clause.Predicate}' failed."
                        : "Guard failed.");
                    continue;
                }
            }
            else if (!string.IsNullOrWhiteSpace(clause.Predicate))
            {
                // Predicate text is set but AST is null (parse failure) — guard cannot be evaluated, skip clause.
                reasons.Add($"Guard '{clause.Predicate}' could not be evaluated.");
                continue;
            }

            // Clause predicate passed (or no predicate — unguarded/else)
            return clause.Outcome switch
            {
                DslStateTransition => TransitionResolution.Accepted(clause),
                DslNoTransition => TransitionResolution.NoTransition(clause),
                DslRejection rej => TransitionResolution.Rejected(new[]
                {
                    string.IsNullOrWhiteSpace(rej.Reason)
                        ? (reasons.FirstOrDefault() ?? $"No transition for '{eventName}' from '{currentState}'.")
                        : rej.Reason!
                }),
                _ => TransitionResolution.Rejected(reasons)
            };
        }

        return TransitionResolution.Rejected(reasons);
    }

    private enum TransitionResolutionKind
    {
        Accepted,
        Rejected,
        NotDefined,
        NoTransition,
        NotApplicable
    }

    private sealed record TransitionResolution(
        TransitionResolutionKind Kind,
        DslClause? Clause,
        string? NotDefinedReason,
        IReadOnlyList<string> Reasons)
    {
        internal static TransitionResolution Accepted(DslClause clause) =>
            new(TransitionResolutionKind.Accepted, clause, null, Array.Empty<string>());

        internal static TransitionResolution Rejected(IReadOnlyList<string> reasons) =>
            new(TransitionResolutionKind.Rejected, null, null, reasons);

        internal static TransitionResolution NotDefined(string reason) =>
            new(TransitionResolutionKind.NotDefined, null, reason, new[] { reason });

        internal static TransitionResolution NoTransition(DslClause clause) =>
            new(TransitionResolutionKind.NoTransition, clause, null, Array.Empty<string>());

        internal static TransitionResolution NotApplicable() =>
            new(TransitionResolutionKind.NotApplicable, null, null, Array.Empty<string>());
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
        foreach (var col in _collectionFieldMap.Values)
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
        if (!_stateRuleMap.TryGetValue(state, out var rules) || rules.Count == 0)
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
        foreach (var col in _collectionFieldMap.Values)
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
        if (_fieldMap.Count == 0 && _collectionFieldMap.Count == 0)
        {
            error = string.Empty;
            return true;
        }

        foreach (var key in data.Keys)
        {
            // Skip internal collection backing keys
            if (key.StartsWith("__collection__", StringComparison.Ordinal))
                continue;

            if (!_fieldMap.ContainsKey(key))
            {
                error = $"Data validation failed: unknown data field '{key}'.";
                return false;
            }
        }

        foreach (var field in _fieldMap.Values)
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

            if (!TryValidateScalarValue(field.Name, field.Type, field.IsNullable, value, out error))
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

            if (!TryValidateScalarValue(arg.Name, arg.Type, arg.IsNullable, value, out error))
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
        if (!_fieldMap.TryGetValue(dataFieldName, out var contract))
        {
            error = string.Empty;
            return true;
        }

        if (TryValidateScalarValue(contract.Name, contract.Type, contract.IsNullable, value, out error))
            return true;

        error = $"Data assignment failed: {error}";
        return false;
    }

    private static bool TryValidateScalarValue(string name, DslScalarType type, bool isNullable, object? value, out string? error)
    {
        if (value is null)
        {
            if (!isNullable)
            {
                error = $"'{name}' does not allow null values.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        bool typeMatches = type switch
        {
            DslScalarType.String => value is string,
            DslScalarType.Boolean => value is bool,
            DslScalarType.Number => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal,
            DslScalarType.Null => false,
            _ => false
        };

        if (!typeMatches)
        {
            error = $"'{name}' expects {type.ToString().ToLowerInvariant()} value.";
            return false;
        }

        error = string.Empty;
        return true;
    }

}

public static class DslWorkflowCompiler
{
    public static DslWorkflowDefinition Compile(DslMachine machine)
    {
        if (machine is null)
            throw new ArgumentNullException(nameof(machine));

        if (string.IsNullOrWhiteSpace(machine.InitialState.Name))
            throw new InvalidOperationException("Exactly one state must be marked initial. Use 'state <Name> initial'.");

        if (!machine.States.Contains(machine.InitialState))
            throw new InvalidOperationException($"Initial state '{machine.InitialState.Name}' is not defined in workflow '{machine.Name}'.");

        // Compile-time rule validations
        ValidateRulesAtCompileTime(machine);

        return new DslWorkflowDefinition(machine);
    }

    private static void ValidateRulesAtCompileTime(DslMachine machine)
    {
        // Build initial instance data (using field defaults)
        var defaultData = BuildDefaultData(machine);

        // 1. Validate field rules against default values
        foreach (var field in machine.Fields)
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
        var initialStateRules = machine.InitialState.Rules;
        if (initialStateRules is not null)
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
        var fieldRuleMap = machine.Fields
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
            foreach (var clause in transition.Clauses)
            {
                foreach (var assignment in clause.SetAssignments)
                    CheckLiteralAssignment(assignment);
            }
        }
    }

    private static IReadOnlyDictionary<string, object?> BuildDefaultData(DslMachine machine)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in machine.Fields)
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
    string CurrentState,
    string EventName,
    string? TargetState,
    IReadOnlyList<string> RequiredEventArgumentKeys,
    IReadOnlyList<string> Reasons)
{
    internal static DslInspectionResult Accepted(string state, string evt, string target, IReadOnlyList<string> requiredEventArgumentKeys) =>
        new(DslOutcomeKind.Accepted, state, evt, target, requiredEventArgumentKeys, Array.Empty<string>());

    internal static DslInspectionResult AcceptedInPlace(string state, string evt) =>
        new(DslOutcomeKind.AcceptedInPlace, state, evt, state, Array.Empty<string>(), Array.Empty<string>());

    internal static DslInspectionResult NotDefined(string state, string evt, string reason) =>
        new(DslOutcomeKind.NotDefined, state, evt, null, Array.Empty<string>(), new[] { reason });

    internal static DslInspectionResult NotApplicable(string state, string evt) =>
        new(DslOutcomeKind.NotApplicable, state, evt, null, Array.Empty<string>(), Array.Empty<string>());

    internal static DslInspectionResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Rejected, state, evt, null, Array.Empty<string>(), reasons);
}

public sealed record DslFireResult(
    DslOutcomeKind Outcome,
    string CurrentState,
    string EventName,
    string? NewState,
    IReadOnlyList<string> Reasons)
{
    internal static DslFireResult Accepted(string state, string evt, string newState) =>
        new(DslOutcomeKind.Accepted, state, evt, newState, Array.Empty<string>());

    internal static DslFireResult AcceptedInPlace(string state, string evt) =>
        new(DslOutcomeKind.AcceptedInPlace, state, evt, state, Array.Empty<string>());

    internal static DslFireResult NotDefined(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.NotDefined, state, evt, null, reasons);

    internal static DslFireResult NotApplicable(string state, string evt) =>
        new(DslOutcomeKind.NotApplicable, state, evt, null, Array.Empty<string>());

    internal static DslFireResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Rejected, state, evt, null, reasons);
}

public sealed record DslInstanceFireResult(
    DslOutcomeKind Outcome,
    string PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<string> Reasons,
    DslWorkflowInstance? UpdatedInstance)
{
    internal static DslInstanceFireResult Accepted(string state, string evt, string newState, DslWorkflowInstance updated) =>
        new(DslOutcomeKind.Accepted, state, evt, newState, Array.Empty<string>(), updated);

    internal static DslInstanceFireResult AcceptedInPlace(string state, string evt, DslWorkflowInstance updated) =>
        new(DslOutcomeKind.AcceptedInPlace, state, evt, state, Array.Empty<string>(), updated);

    internal static DslInstanceFireResult NotDefined(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.NotDefined, state, evt, null, reasons, null);

    internal static DslInstanceFireResult NotApplicable(string state, string evt) =>
        new(DslOutcomeKind.NotApplicable, state, evt, null, Array.Empty<string>(), null);

    internal static DslInstanceFireResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(DslOutcomeKind.Rejected, state, evt, null, reasons, null);
}

public enum DslOutcomeKind
{
    NotDefined,
    NotApplicable,
    Rejected,
    Accepted,
    AcceptedInPlace
}
