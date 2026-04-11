using System;
using System.Collections.Generic;
using System.Linq;

namespace Precept;

public sealed class PreceptEngine
{
    // Transition lookup: (State, Event) → ordered list of transition rows (first-match wins)
    private readonly Dictionary<(string State, string Event), List<PreceptTransitionRow>> _transitionRowMap;
    private readonly Dictionary<string, PreceptField> _fieldMap;
    private readonly Dictionary<string, PreceptCollectionField> _collectionFieldMap;
    private readonly Dictionary<string, Dictionary<string, PreceptEventArg>> _eventArgContractMap;

    // New constraint storage
    private readonly IReadOnlyList<PreceptInvariant> _invariants;
    private readonly IReadOnlyList<EventAssertion> _eventAsserts;
    private readonly Dictionary<string, List<EventAssertion>> _eventAssertMap;
    private readonly IReadOnlyList<StateAssertion> _stateAsserts;
    private readonly IReadOnlyList<PreceptStateAction> _stateActions;
    private readonly Dictionary<(AssertAnchor Prep, string State), List<StateAssertion>> _stateAssertMap;
    private readonly Dictionary<(AssertAnchor Prep, string State), List<PreceptStateAction>> _stateActionMap;

    // Editability: state → set of editable field names (union of all matching edit blocks)
    private readonly IReadOnlyDictionary<string, HashSet<string>> _editableFieldsByState;

    /// <summary>Guarded edit blocks evaluated per-call: block.WhenGuard != null entries stored here.</summary>
    private readonly IReadOnlyList<PreceptEditBlock> _guardedEditBlocks;

    /// <summary>Editable fields for stateless (root-level) edit declarations. Null if no root edit blocks.</summary>
    private HashSet<string>? _rootEditableFields;

    public string Name { get; }
    public IReadOnlyList<string> States { get; }
    public string? InitialState { get; }

    /// <summary>True when the precept has no state declarations.</summary>
    public bool IsStateless => States.Count == 0;
    public IReadOnlyList<PreceptEvent> Events { get; }
    public IReadOnlyList<PreceptField> Fields { get; }
    public IReadOnlyList<PreceptCollectionField> CollectionFields { get; }

    internal PreceptEngine(PreceptDefinition model)
    {
        Name = model.Name;
        States = model.States.Select(s => s.Name).ToArray();
        InitialState = model.InitialState?.Name;
        Events = model.Events;
        Fields = model.Fields;
        CollectionFields = model.CollectionFields;

        // Build transition row map
        _transitionRowMap = new Dictionary<(string State, string Event), List<PreceptTransitionRow>>();
        if (model.TransitionRows is { Count: > 0 })
        {
            foreach (var row in model.TransitionRows)
            {
                var key = (row.FromState, row.EventName);
                if (!_transitionRowMap.TryGetValue(key, out var list))
                {
                    list = new List<PreceptTransitionRow>();
                    _transitionRowMap[key] = list;
                }
                list.Add(row);
            }
        }

        _fieldMap = model.Fields
            .ToDictionary(f => f.Name, f => f, StringComparer.Ordinal);

        _collectionFieldMap = model.CollectionFields
            .ToDictionary(f => f.Name, f => f, StringComparer.Ordinal);

        _eventArgContractMap = model.Events
            .ToDictionary(
                evt => evt.Name,
                evt => evt.Args.ToDictionary(a => a.Name, a => a, StringComparer.Ordinal),
                StringComparer.Ordinal);

        // Invariants
        _invariants = model.Invariants ?? Array.Empty<PreceptInvariant>();

        // Event asserts
        _eventAsserts = model.EventAsserts ?? Array.Empty<EventAssertion>();
        _eventAssertMap = new Dictionary<string, List<EventAssertion>>(StringComparer.Ordinal);
        foreach (var ea in _eventAsserts)
        {
            if (!_eventAssertMap.TryGetValue(ea.EventName, out var list))
            {
                list = new List<EventAssertion>();
                _eventAssertMap[ea.EventName] = list;
            }
            list.Add(ea);
        }

        // State asserts (preposition-aware)
        _stateAsserts = model.StateAsserts ?? Array.Empty<StateAssertion>();
        _stateAssertMap = new Dictionary<(AssertAnchor Prep, string State), List<StateAssertion>>();
        foreach (var sa in _stateAsserts)
        {
            var key = (sa.Anchor, sa.State);
            if (!_stateAssertMap.TryGetValue(key, out var list))
            {
                list = new List<StateAssertion>();
                _stateAssertMap[key] = list;
            }
            list.Add(sa);
        }

        // State actions (entry/exit automatic mutations)
        _stateActions = model.StateActions ?? Array.Empty<PreceptStateAction>();
        _stateActionMap = new Dictionary<(AssertAnchor Prep, string State), List<PreceptStateAction>>();
        foreach (var sa in _stateActions)
        {
            var key = (sa.Anchor, sa.State);
            if (!_stateActionMap.TryGetValue(key, out var list))
            {
                list = new List<PreceptStateAction>();
                _stateActionMap[key] = list;
            }
            list.Add(sa);
        }

        // Editable fields: build per-state and root-level editable field name sets.
        // Root-level edit blocks (block.State == null) support stateless precepts.
        // Field list ["all"] expands to all declared scalar + collection field names.
        var editMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var guardedBlocks = new List<PreceptEditBlock>();
        if (model.EditBlocks is { Count: > 0 })
        {
            foreach (var block in model.EditBlocks)
            {
                // Guarded edit blocks are evaluated per-call, not precomputed
                if (block.WhenGuard is not null)
                {
                    guardedBlocks.Add(block);
                    continue;
                }

                var expandedNames = ExpandEditFieldNames(block.FieldNames);
                if (block.State is null)
                {
                    _rootEditableFields ??= new HashSet<string>(StringComparer.Ordinal);
                    foreach (var fn in expandedNames)
                        _rootEditableFields.Add(fn);
                }
                else
                {
                    if (!editMap.TryGetValue(block.State, out var fieldSet))
                    {
                        fieldSet = new HashSet<string>(StringComparer.Ordinal);
                        editMap[block.State] = fieldSet;
                    }
                    foreach (var fn in expandedNames)
                        fieldSet.Add(fn);
                }
            }
        }
        _editableFieldsByState = editMap;
        _guardedEditBlocks = guardedBlocks;
    }

    /// <summary>
    /// Expands ["all"] to all declared field names, or returns the list unchanged.
    /// "all" means all scalar fields + all collection fields.
    /// </summary>
    private IEnumerable<string> ExpandEditFieldNames(IReadOnlyList<string> fieldNames)
    {
        if (fieldNames.Count == 1 && string.Equals(fieldNames[0], "all", StringComparison.Ordinal))
            return Fields.Select(static f => f.Name).Concat(CollectionFields.Select(static f => f.Name));
        return fieldNames;
    }

    /// <summary>
    /// Evaluates guarded edit blocks for the given state against instance data.
    /// Returns the set of field names granted by passing guards.
    /// Fail-closed: guard evaluation error → field not granted.
    /// </summary>
    private HashSet<string> EvaluateGuardedEditFields(string state, IReadOnlyDictionary<string, object?> data)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in _guardedEditBlocks)
        {
            if (block.State is null || !string.Equals(block.State, state, StringComparison.Ordinal))
                continue;

            // Fail-closed: any evaluation error → guard treated as false
            try
            {
                var guardResult = PreceptExpressionRuntimeEvaluator.Evaluate(block.WhenGuard!, data);
                if (!guardResult.Success || guardResult.Value is not true)
                    continue;
            }
            catch
            {
                continue; // Fail-closed
            }

            foreach (var fieldName in ExpandEditFieldNames(block.FieldNames))
                result.Add(fieldName);
        }
        return result;
    }

    public PreceptInstance CreateInstance(
        IReadOnlyDictionary<string, object?>? instanceData = null)
    {
        if (IsStateless)
        {
            var statelessData = BuildInitialInstanceData(instanceData);
            // SYNC:CONSTRAINT:C35
            if (!TryValidateDataContract(statelessData, out var dataError))
                throw new InvalidOperationException(dataError);
            return new PreceptInstance(Name, null, null, DateTimeOffset.UtcNow, statelessData);
        }
        return CreateInstance(InitialState!, instanceData);
    }

    public PreceptInstance CreateInstance(
        string initialState,
        IReadOnlyDictionary<string, object?>? instanceData = null)
    {
        if (IsStateless)
            throw new ArgumentException(
                $"Precept '{Name}' is stateless. Use CreateInstance(instanceData) — the state argument is not valid.",
                nameof(initialState));

        // SYNC:CONSTRAINT:C33
        if (string.IsNullOrWhiteSpace(initialState))
            throw new ArgumentException(DiagnosticCatalog.C33.FormatMessage(), nameof(initialState));

        // SYNC:CONSTRAINT:C34
        if (!States.Contains(initialState, StringComparer.Ordinal))
            throw new InvalidOperationException(DiagnosticCatalog.C34.FormatMessage(("stateName", initialState), ("workflowName", Name)));

        var data = BuildInitialInstanceData(instanceData);
        // SYNC:CONSTRAINT:C35
        if (!TryValidateDataContract(data, out var dataError))
            throw new InvalidOperationException(dataError);

        return new PreceptInstance(
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

        // Initialize collection fields as empty lists (clean public format)
        foreach (var collField in CollectionFields)
            merged[collField.Name] = new List<object>();

        if (instanceData is not null)
        {
            foreach (var pair in instanceData)
            {
                // If caller provides a list for a collection field, store as plain list
                if (_collectionFieldMap.ContainsKey(pair.Key) && pair.Value is System.Collections.IEnumerable enumerable && pair.Value is not string)
                {
                    var items = new List<object>();
                    foreach (var item in enumerable)
                    {
                        if (item is not null)
                            items.Add(item);
                    }
                    merged[pair.Key] = items;
                    continue;
                }

                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }

    /// <summary>
    /// Converts clean public InstanceData (plain lists under field names) to the internal evaluation
    /// format (CollectionValue objects under __collection__ prefixed keys).
    /// </summary>
    private Dictionary<string, object?> HydrateInstanceData(IReadOnlyDictionary<string, object?> cleanData)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in cleanData)
        {
            if (_collectionFieldMap.TryGetValue(kvp.Key, out var colField))
            {
                var cv = new CollectionValue(colField.CollectionKind, colField.InnerType);
                if (kvp.Value is System.Collections.IEnumerable items && kvp.Value is not string)
                {
                    var list = new List<object>();
                    foreach (var item in items)
                        if (item is not null) list.Add(item);
                    cv.LoadFrom(list);
                }
                result[$"__collection__{kvp.Key}"] = cv;
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        return result;
    }

    /// <summary>
    /// Converts internal evaluation data (CollectionValue under __collection__ keys) to clean public
    /// InstanceData format (plain lists under field names, no __collection__ prefix).
    /// </summary>
    private static IReadOnlyDictionary<string, object?> DehydrateData(Dictionary<string, object?> internalData)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in internalData)
        {
            if (kvp.Key.StartsWith("__collection__", StringComparison.Ordinal))
            {
                var fieldName = kvp.Key.Substring("__collection__".Length);
                result[fieldName] = kvp.Value is CollectionValue cv ? cv.ToSerializableList() : new List<object>();
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        return result;
    }

    public PreceptCompatibilityResult CheckCompatibility(PreceptInstance instance)
    {
        if (!instance.WorkflowName.Equals(Name, StringComparison.Ordinal))
        {
            return PreceptCompatibilityResult.NotCompatible(
                $"Instance workflow '{instance.WorkflowName}' does not match compiled workflow '{Name}'.");
        }

        if (IsStateless)
        {
            if (instance.CurrentState is not null)
                return PreceptCompatibilityResult.NotCompatible(
                    $"Stateless precept '{Name}' expects CurrentState to be null, but got '{instance.CurrentState}'.");
        }
        else
        {
            if (!States.Contains(instance.CurrentState!, StringComparer.Ordinal))
                return PreceptCompatibilityResult.NotCompatible($"Instance state '{instance.CurrentState}' is not defined in workflow '{Name}'.");
        }

        if (!TryValidateDataContract(instance.InstanceData, out var dataError))
            return PreceptCompatibilityResult.NotCompatible(dataError);

        // Verify the instance satisfies all current constraints (invariants + state asserts).
        var internalData = HydrateInstanceData(instance.InstanceData);
        var ruleViolations = new List<ConstraintViolation>();
        ruleViolations.AddRange(EvaluateInvariants(internalData));
        if (instance.CurrentState is not null)
            ruleViolations.AddRange(EvaluateStateAssertions(AssertAnchor.In, instance.CurrentState, internalData));
        if (ruleViolations.Count > 0)
            return PreceptCompatibilityResult.NotCompatible(
                $"Instance violates {ruleViolations.Count} rule(s): {string.Join("; ", ruleViolations.Select(v => v.Message))}");

        return PreceptCompatibilityResult.Compatible();
    }

    public EventInspectionResult Inspect(
        PreceptInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
            return EventInspectionResult.Undefined(instance.CurrentState, eventName, compatibility.Reason!);

        // Stateless precepts have no transition surface.
        if (IsStateless)
            return EventInspectionResult.Undefined(instance.CurrentState, eventName,
                $"Precept '{Name}' is stateless — events have no transition surface.");

        // Hydrate clean InstanceData to internal format for engine evaluation
        var internalData = HydrateInstanceData(instance.InstanceData);

        // Fast-path: if all rows for (state, event) have when guards, try evaluating without
        // event args. If all guards fail → NotApplicable (avoids requiring event args for discovery).
        if (_transitionRowMap.TryGetValue((instance.CurrentState!, eventName), out var preCheckRows))
        {
            bool allGuardsPresent = preCheckRows.All(r => r.WhenGuard is not null);
            if (allGuardsPresent)
            {
                var instanceOnlyContext = BuildEvaluationData(internalData, eventName, null);
                bool anyGuardPasses = preCheckRows.Any(r =>
                {
                    var whenResult = PreceptExpressionRuntimeEvaluator.Evaluate(r.WhenGuard!, instanceOnlyContext, _fieldMap);
                    return whenResult.Success && whenResult.Value is true;
                });
                if (!anyGuardPasses)
                    return EventInspectionResult.Unmatched(instance.CurrentState, eventName);
            }
        }

        // Inspect is a discovery API — it intentionally accepts calls with missing/partial event arguments
        // so callers can determine required args via RequiredEventArgumentKeys before firing.
        if (eventArguments != null && !TryValidateEventArguments(eventName, eventArguments, out var eventArgError))
            return EventInspectionResult.Rejected(instance.CurrentState, eventName, new[] { ConstraintViolation.Simple(eventArgError!) });

        var evaluationArguments = BuildEvaluationData(internalData, eventName, eventArguments);

        // Check event asserts — only when caller provides event arguments.
        if (eventArguments != null)
        {
            var eventAssertViolations = EvaluateEventAssertions(eventName, BuildDirectEvaluationData(eventName, eventArguments));
            if (eventAssertViolations.Count > 0)
                return EventInspectionResult.Rejected(instance.CurrentState, eventName, eventAssertViolations);
        }

        var resolution = ResolveTransition(instance.CurrentState!, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.Undefined)
            return EventInspectionResult.Undefined(instance.CurrentState, eventName, resolution.NotDefinedReason!);

        if (resolution.Kind == TransitionResolutionKind.Unmatched)
            return EventInspectionResult.Unmatched(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.NoTransition)
            return EventInspectionResult.NoTransition(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.MatchedRow is null)
            return EventInspectionResult.Rejected(instance.CurrentState, eventName, resolution.Violations);

        var matchedRow = resolution.MatchedRow;
        var targetState = ((StateTransition)matchedRow.Outcome).TargetState;

        // Simulate full pipeline: exit actions → row mutations → entry actions
        var simulatedData = new Dictionary<string, object?>(internalData, StringComparer.Ordinal);
        var simulatedCollections = CloneCollections(simulatedData);

        // Exit actions
        ExecuteStateActions(AssertAnchor.From, instance.CurrentState!,
            simulatedData, simulatedCollections, eventName, eventArguments);

        // Row mutations
        foreach (var assignment in matchedRow.SetAssignments)
        {
            var ctx = BuildEvaluationDataWithCollections(simulatedData, simulatedCollections, eventName, eventArguments);
            var eval = PreceptExpressionRuntimeEvaluator.Evaluate(assignment.Expression, ctx);
            if (eval.Success)
                simulatedData[assignment.Key] = eval.Value;
        }
        if (matchedRow.CollectionMutations is { } mutations)
            ExecuteCollectionMutations(mutations, simulatedCollections, simulatedData, eventName, eventArguments);

        // Entry actions
        ExecuteStateActions(AssertAnchor.To, targetState,
            simulatedData, simulatedCollections, eventName, eventArguments);

        CommitCollections(simulatedData, simulatedCollections);

        // Validate invariants + state asserts
        var violations = CollectConstraintViolations(
            instance.CurrentState!, targetState, simulatedData);
        if (violations.Count > 0)
            return EventInspectionResult.ConstraintFailure(instance.CurrentState, eventName, violations);

        return EventInspectionResult.Transitioned(
            instance.CurrentState,
            eventName,
            targetState,
            GetRequiredEventArgumentKeys(eventName));
    }

    /// <summary>
    /// Inspects all events reachable from the instance's current state and returns a summary
    /// including the current state, serialized data, and per-event inspection results.
    /// </summary>
    public InspectionResult Inspect(PreceptInstance instance)
    {
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
            return new InspectionResult(
                instance.CurrentState,
                instance.InstanceData,
                Array.Empty<EventInspectionResult>());

        // Stateless precepts: return all events with Undefined outcome.
        if (IsStateless)
        {
            var statelessEventResults = Events
                .Select(e => EventInspectionResult.Undefined(
                    null, e.Name,
                    $"Precept '{Name}' is stateless — events have no transition surface."))
                .ToArray();
            var statelessEditableFields = BuildEditableFieldInfosForStateless(instance.InstanceData);
            return new InspectionResult(null, instance.InstanceData, statelessEventResults, statelessEditableFields);
        }

        var eventDeclarationOrder = Events
            .Select((e, i) => (e.Name, i))
            .ToDictionary(x => x.Name, x => x.i, StringComparer.Ordinal);

        var outgoingEventNames = _transitionRowMap.Keys
            .Where(k => string.Equals(k.State, instance.CurrentState, StringComparison.Ordinal))
            .Select(k => k.Event)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => eventDeclarationOrder.TryGetValue(n, out var idx) ? idx : int.MaxValue)
            .ThenBy(n => n, StringComparer.Ordinal)
            .ToArray();

        var eventResults = outgoingEventNames
            .Select(eventName => Inspect(instance, eventName))
            .ToArray();

        var editableFieldInfos = BuildEditableFieldInfos(instance.CurrentState, instance.InstanceData);

        return new InspectionResult(instance.CurrentState, instance.InstanceData, eventResults, editableFieldInfos);
    }

    /// <summary>
    /// Applies a hypothetical patch to a working copy of instance data, runs the full
    /// validation pipeline, and returns a <see cref="InspectionResult"/> with
    /// violations reflected in <see cref="PreceptEditableFieldInfo.Violation"/>.
    /// No commit occurs — the instance is unchanged.
    /// </summary>
    public InspectionResult Inspect(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)
    {
        var baseResult = Inspect(instance);
        var editableFieldInfos = baseResult.EditableFields;

        // If no edit blocks at all, return the base result unchanged
        if (editableFieldInfos is null)
            return baseResult;

        var builder = new UpdatePatchBuilder();
        patch(builder);

        var operations = builder.GetOperations();
        if (operations.Count == 0)
            return baseResult;

        // Validate conflicts
        var conflictError = builder.ValidateConflicts(_fieldMap, _collectionFieldMap);
        if (conflictError is not null)
        {
            // Return with a synthetic violation on the first field
            var violatedInfos = new List<PreceptEditableFieldInfo>();
            foreach (var info in editableFieldInfos)
            {
                var matchesOp = operations.Any(op => string.Equals(op.FieldName, info.FieldName, StringComparison.Ordinal));
                if (matchesOp)
                {
                    violatedInfos.Add(info with { Violation = conflictError });
                }
                else
                {
                    violatedInfos.Add(info);
                }
            }
            return new InspectionResult(baseResult.CurrentState, baseResult.InstanceData, baseResult.Events, violatedInfos);
        }

        // Check editability — union of static + guarded edit blocks
        HashSet<string>? editableNames;
        if (IsStateless)
        {
            editableNames = _rootEditableFields;
        }
        else
        {
            editableNames = _editableFieldsByState.TryGetValue(instance.CurrentState!, out var editable)
                ? new HashSet<string>(editable, StringComparer.Ordinal) : null;

            if (_guardedEditBlocks.Count > 0 && instance.CurrentState is not null)
            {
                var hydrated = HydrateInstanceData(instance.InstanceData);
                var guardedFields = EvaluateGuardedEditFields(instance.CurrentState, hydrated);
                if (guardedFields.Count > 0)
                {
                    editableNames ??= new HashSet<string>(StringComparer.Ordinal);
                    editableNames.UnionWith(guardedFields);
                }
            }
        }
        foreach (var op in operations)
        {
            var stateLabel = IsStateless ? "(stateless)" : $"state '{instance.CurrentState}'";
            if (editableNames is null || !editableNames.Contains(op.FieldName))
            {
                var reason = $"Field '{op.FieldName}' is not editable in {stateLabel}.";
                var violatedInfos = editableFieldInfos.Select(info =>
                    string.Equals(info.FieldName, op.FieldName, StringComparison.Ordinal)
                        ? info with { Violation = reason }
                        : info).ToList();
                return new InspectionResult(baseResult.CurrentState, baseResult.InstanceData, baseResult.Events, violatedInfos);
            }
        }

        // Simulate: apply patch to working copy and evaluate rules
        var internalData = HydrateInstanceData(instance.InstanceData);
        var updatedData = new Dictionary<string, object?>(internalData, StringComparer.Ordinal);
        var workingCollections = CloneCollections(updatedData);

        ApplyPatchOperations(operations, updatedData, workingCollections);
        CommitCollections(updatedData, workingCollections);

        // Evaluate rules on working copy
        var violations = new List<ConstraintViolation>();
        violations.AddRange(EvaluateInvariants(updatedData));
        if (instance.CurrentState is not null)
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.In, instance.CurrentState, updatedData));

        if (violations.Count == 0)
            return baseResult;

        // Map violations to affected fields
        var patchedFieldNames = new HashSet<string>(
            operations.Select(op => op.FieldName), StringComparer.Ordinal);
        var violation = string.Join("; ", violations.Select(v => v.Message));

        var resultInfos = editableFieldInfos.Select(info =>
            patchedFieldNames.Contains(info.FieldName)
                ? info with { Violation = violation }
                : info).ToList();

        return new InspectionResult(baseResult.CurrentState, baseResult.InstanceData, baseResult.Events, resultInfos);
    }

    /// <summary>
    /// Coerces raw event argument values (strings from CLI input, JsonElement from JSON parsing)
    /// to the runtime types declared in the event's argument contract.
    /// Returns null if input is null.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? CoerceEventArguments(
        string eventName,
        IReadOnlyDictionary<string, object?>? args)
    {
        if (args is null || args.Count == 0)
            return args;

        if (!_eventArgContractMap.TryGetValue(eventName, out var argContracts) || argContracts.Count == 0)
            return args;

        var coerced = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in args)
        {
            if (!argContracts.TryGetValue(kvp.Key, out var contract))
            {
                coerced[kvp.Key] = kvp.Value;
                continue;
            }
            coerced[kvp.Key] = CoerceArgumentValue(kvp.Value, contract);
        }
        return coerced;
    }

    private static object? CoerceArgumentValue(object? value, PreceptEventArg contract)
    {
        // Unwrap JsonElement from System.Text.Json deserialization.
        if (value is System.Text.Json.JsonElement jsonElement)
            value = UnwrapJsonElement(jsonElement);

        if (value is null)
            return null;

        return contract.Type switch
        {
            PreceptScalarType.Number => CoerceToNumber(value),
            PreceptScalarType.Integer => CoerceToInteger(value),
            PreceptScalarType.Decimal => CoerceToDecimal(value),
            PreceptScalarType.Boolean => CoerceToBoolean(value),
            PreceptScalarType.String => value?.ToString(),
            PreceptScalarType.Choice => value?.ToString(),
            PreceptScalarType.Null => null,
            _ => value
        };
    }

    private static object? UnwrapJsonElement(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString(),
            // Distinguish integer vs floating-point JSON numbers
            System.Text.Json.JsonValueKind.Number => element.TryGetInt64(out var l) ? (object?)l : element.GetDouble(),
            System.Text.Json.JsonValueKind.True => (object?)true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            System.Text.Json.JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private static object? CoerceToInteger(object value)
    {
        if (value is long) return value;
        if (value is int i) return (long)i;
        if (value is short s) return (long)s;
        if (value is byte b) return (long)b;
        if (value is sbyte sb) return (long)sb;
        if (value is string str && long.TryParse(str, out var l)) return l;
        return value;
    }

    private static object? CoerceToNumber(object value)
    {
        if (value is double or float or int or long or decimal or byte or sbyte or short or ushort or uint or ulong)
            return Convert.ToDouble(value);
        if (value is string s && double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;
        return value;
    }

    private static object? CoerceToBoolean(object value)
    {
        if (value is bool)
            return value;
        if (value is string s)
        {
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
        }
        return value;
    }

    private static object? CoerceToDecimal(object value)
    {
        if (value is decimal) return value;
        if (value is double d) return (decimal)d;
        if (value is float f) return (decimal)f;
        if (value is long l) return (decimal)l;
        if (value is int i) return (decimal)i;
        if (value is string str && decimal.TryParse(str, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return value;
    }

    public FireResult Fire(
        PreceptInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var internalData = HydrateInstanceData(instance.InstanceData);
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
        {
            return FireResult.Undefined(instance.CurrentState, eventName, new[] { compatibility.Reason! });
        }

        // Stateless precepts have no transition surface — Fire always returns Undefined.
        if (IsStateless)
            return FireResult.Undefined(instance.CurrentState, eventName,
                new[] { $"Precept '{Name}' is stateless — events have no transition surface." });

        if (!TryValidateEventArguments(eventName, eventArguments, out var eventArgError))
            return FireResult.Rejected(instance.CurrentState, eventName, new[] { ConstraintViolation.Simple(eventArgError!) });

        var evaluationArguments = BuildEvaluationData(internalData, eventName, eventArguments);

        // Stage 1: Event asserts (args-only context, pre-transition)
        var eventAssertViolations = EvaluateEventAssertions(eventName, BuildDirectEvaluationData(eventName, eventArguments));
        if (eventAssertViolations.Count > 0)
            return FireResult.Rejected(instance.CurrentState, eventName, eventAssertViolations);

        // Stage 2: First-match row selection
        var resolution = ResolveTransition(instance.CurrentState!, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.Undefined)
            return FireResult.Undefined(instance.CurrentState, eventName, new[] { resolution.NotDefinedReason! });

        if (resolution.Kind == TransitionResolutionKind.Unmatched)
            return FireResult.Unmatched(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.MatchedRow is null)
            return FireResult.Rejected(instance.CurrentState, eventName, resolution.Violations);

        var matchedRow = resolution.MatchedRow;
        var updatedData = new Dictionary<string, object?>(internalData, StringComparer.Ordinal);
        var workingCollections = CloneCollections(updatedData);

        // ── No-transition branch ──
        if (matchedRow.Outcome is NoTransition)
        {
            // Row mutations only (no exit/entry actions for no-transition)
            var mutError = ExecuteRowMutations(matchedRow, updatedData, workingCollections, eventName, eventArguments);
            if (mutError is not null)
                return FireResult.Rejected(instance.CurrentState, eventName, new[] { ConstraintViolation.Simple(mutError) });

            CommitCollections(updatedData, workingCollections);

            // Validate: invariants + 'in' asserts for current state
            var violations = new List<ConstraintViolation>();
            violations.AddRange(EvaluateInvariants(updatedData));
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.In, instance.CurrentState!, updatedData));
            if (violations.Count > 0)
                return FireResult.ConstraintFailure(instance.CurrentState, eventName, violations);

            var noTransitionUpdated = instance with
            {
                LastEvent = eventName,
                UpdatedAt = DateTimeOffset.UtcNow,
                InstanceData = DehydrateData(updatedData)
            };
            return FireResult.NoTransition(instance.CurrentState, eventName, noTransitionUpdated);
        }

        // ── Transition branch ──
        var targetState = ((StateTransition)matchedRow.Outcome).TargetState;

        // Stage 3: Exit actions (from <sourceState> -> ...)
        ExecuteStateActions(AssertAnchor.From, instance.CurrentState!,
            updatedData, workingCollections, eventName, eventArguments);

        // Stage 4: Row mutations (set assignments + collection mutations)
        var rowMutError = ExecuteRowMutations(matchedRow, updatedData, workingCollections, eventName, eventArguments);
        if (rowMutError is not null)
            return FireResult.Rejected(instance.CurrentState, eventName, new[] { ConstraintViolation.Simple(rowMutError) });

        // Stage 5: Entry actions (to <targetState> -> ...)
        ExecuteStateActions(AssertAnchor.To, targetState,
            updatedData, workingCollections, eventName, eventArguments);

        CommitCollections(updatedData, workingCollections);

        // Stage 6: Validation (invariants + state asserts, collect-all)
        var validationViolations = CollectConstraintViolations(
            instance.CurrentState!, targetState, updatedData);
        if (validationViolations.Count > 0)
            return FireResult.ConstraintFailure(instance.CurrentState, eventName, validationViolations);

        var updated = instance with
        {
            CurrentState = targetState,
            LastEvent = eventName,
            UpdatedAt = DateTimeOffset.UtcNow,
            InstanceData = DehydrateData(updatedData)
        };

        return FireResult.Transitioned(instance.CurrentState, eventName, targetState, updated);
    }

    /// <summary>
    /// Atomically updates editable fields on an instance. Only fields declared in an
    /// <c>in &lt;State&gt; edit</c> block for the current state are mutable.
    /// After applying edits, evaluates all invariants and state asserts.
    /// </summary>
    public UpdateResult Update(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)
    {
        var builder = new UpdatePatchBuilder();
        patch(builder);

        // Build-time conflict detection
        var conflictError = builder.ValidateConflicts(_fieldMap, _collectionFieldMap);
        if (conflictError is not null)
            return UpdateResult.Failed(UpdateOutcome.InvalidInput, new[] { conflictError });

        var operations = builder.GetOperations();
        if (operations.Count == 0)
            return UpdateResult.Failed(UpdateOutcome.InvalidInput, new[] { "Patch is empty." });

        // Hydrate instance data early — needed for guarded edit evaluation
        var internalData = HydrateInstanceData(instance.InstanceData);

        // Stage 1: Editability check — all fields in patch must be editable.
        // Union of static (unconditional) + dynamic (guarded) edit blocks.
        HashSet<string>? editableFields;
        if (IsStateless)
        {
            editableFields = _rootEditableFields;
        }
        else
        {
            editableFields = _editableFieldsByState.TryGetValue(instance.CurrentState!, out var editable)
                ? new HashSet<string>(editable, StringComparer.Ordinal) : null;

            // Add fields from guarded edit blocks that pass their guards
            if (_guardedEditBlocks.Count > 0 && instance.CurrentState is not null)
            {
                var guardedFields = EvaluateGuardedEditFields(instance.CurrentState, internalData);
                if (guardedFields.Count > 0)
                {
                    editableFields ??= new HashSet<string>(StringComparer.Ordinal);
                    editableFields.UnionWith(guardedFields);
                }
            }
        }

        var notAllowed = new List<string>();
        foreach (var op in operations)
        {
            var stateLabel = IsStateless ? "(stateless)" : $"state '{instance.CurrentState}'";
            if (editableFields is null || !editableFields.Contains(op.FieldName))
                notAllowed.Add($"Field '{op.FieldName}' is not editable in {stateLabel}.");
        }
        if (notAllowed.Count > 0)
            return UpdateResult.Failed(UpdateOutcome.UneditableField, notAllowed);

        // Stage 2: Type check + unknown field validation
        foreach (var op in operations)
        {
            var typeError = ValidateUpdateOperation(op);
            if (typeError is not null)
                return UpdateResult.Failed(UpdateOutcome.InvalidInput, new[] { typeError });
        }

        // Stage 3: Atomic mutation on working copy (internalData already hydrated above)
        var updatedData = new Dictionary<string, object?>(internalData, StringComparer.Ordinal);
        var workingCollections = CloneCollections(updatedData);

        var mutError = ApplyPatchOperations(operations, updatedData, workingCollections);
        if (mutError is not null)
            return UpdateResult.Failed(UpdateOutcome.InvalidInput, new[] { mutError });

        CommitCollections(updatedData, workingCollections);

        // Stage 4: Rules evaluation (invariants + 'in' state asserts)
        var violations = new List<ConstraintViolation>();
        violations.AddRange(EvaluateInvariants(updatedData));
        if (instance.CurrentState is not null)
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.In, instance.CurrentState, updatedData));
        if (violations.Count > 0)
            return UpdateResult.Failed(UpdateOutcome.ConstraintFailure, violations);

        // Stage 5: Commit
        var updated = instance with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            InstanceData = DehydrateData(updatedData)
        };

        return UpdateResult.Succeeded(updated);
    }

    private string? ValidateUpdateOperation(UpdatePatchOperation op)
    {
        bool isScalar = _fieldMap.ContainsKey(op.FieldName);
        bool isCollection = _collectionFieldMap.ContainsKey(op.FieldName);

        if (!isScalar && !isCollection)
            return $"Unknown field '{op.FieldName}'.";

        if (isScalar && op.Kind != UpdateOpKind.Set)
            return $"Granular operations are only valid on collection fields (field '{op.FieldName}' is scalar).";

        if (isCollection && op.Kind == UpdateOpKind.Set)
            return $"Use Replace for collection fields (field '{op.FieldName}').";

        // Type check for scalar Set
        if (isScalar && op.Kind == UpdateOpKind.Set)
        {
            var field = _fieldMap[op.FieldName];
            if (!TryValidateScalarValue(field.Name, field.Type, field.IsNullable, op.Value, out var typeError))
                return typeError;
        }

        // Type check for collection element values
        if (isCollection && op.Value is not null)
        {
            var col = _collectionFieldMap[op.FieldName];
            if (op.Kind is UpdateOpKind.Add or UpdateOpKind.Remove
                or UpdateOpKind.Enqueue or UpdateOpKind.Push)
            {
                if (!TryValidateScalarValue(op.FieldName, col.InnerType, false, op.Value, out var elemError))
                    return $"Collection element type mismatch: {elemError}";
            }
        }

        // Type check for Replace values
        if (isCollection && op.Kind == UpdateOpKind.Replace && op.Values is not null)
        {
            var col = _collectionFieldMap[op.FieldName];
            foreach (var val in op.Values)
            {
                if (!TryValidateScalarValue(op.FieldName, col.InnerType, false, val, out var elemError))
                    return $"Collection element type mismatch: {elemError}";
            }
        }

        return null;
    }

    private string? ApplyPatchOperations(
        IReadOnlyList<UpdatePatchOperation> operations,
        Dictionary<string, object?> data,
        Dictionary<string, CollectionValue> workingCollections)
    {
        foreach (var op in operations)
        {
            if (_fieldMap.ContainsKey(op.FieldName))
            {
                // Scalar set
                data[op.FieldName] = op.Value;
                continue;
            }

            if (!workingCollections.TryGetValue(op.FieldName, out var collection))
                return $"Unknown collection field '{op.FieldName}'.";

            switch (op.Kind)
            {
                case UpdateOpKind.Add:
                    collection.Add(op.Value!);
                    break;
                case UpdateOpKind.Remove:
                    collection.Remove(op.Value!);
                    break;
                case UpdateOpKind.Enqueue:
                    collection.Enqueue(op.Value!);
                    break;
                case UpdateOpKind.Dequeue:
                    if (collection.Count == 0)
                        return $"Cannot dequeue from empty queue '{op.FieldName}'.";
                    collection.Dequeue();
                    break;
                case UpdateOpKind.Push:
                    collection.Push(op.Value!);
                    break;
                case UpdateOpKind.Pop:
                    if (collection.Count == 0)
                        return $"Cannot pop from empty stack '{op.FieldName}'.";
                    collection.Pop();
                    break;
                case UpdateOpKind.Replace:
                    collection.Clear();
                    if (op.Values is not null)
                    {
                        foreach (var val in op.Values)
                            collection.Add(val);
                    }
                    break;
                case UpdateOpKind.Clear:
                    collection.Clear();
                    break;
            }
        }
        return null;
    }

    private IReadOnlyList<PreceptEditableFieldInfo>? BuildEditableFieldInfosForStateless(
        IReadOnlyDictionary<string, object?> instanceData)
    {
        if (_rootEditableFields is null || _rootEditableFields.Count == 0)
            return null;

        var result = new List<PreceptEditableFieldInfo>();
        foreach (var field in Fields)
        {
            if (!_rootEditableFields.Contains(field.Name))
                continue;
            instanceData.TryGetValue(field.Name, out var currentValue);
            var typeName = field.Type.ToString().ToLowerInvariant();
            result.Add(new PreceptEditableFieldInfo(field.Name, typeName, field.IsNullable, currentValue));
        }
        foreach (var col in CollectionFields)
        {
            if (!_rootEditableFields.Contains(col.Name))
                continue;
            instanceData.TryGetValue(col.Name, out var currentValue);
            var typeName = $"{col.CollectionKind.ToString().ToLowerInvariant()}<{col.InnerType.ToString().ToLowerInvariant()}>";
            result.Add(new PreceptEditableFieldInfo(col.Name, typeName, false, currentValue));
        }
        return result;
    }

    /// <summary>
    /// Returns the set of editable field names for the given state, or empty if none.
    /// </summary>
    internal IReadOnlySet<string> GetEditableFieldNames(string? state, IReadOnlyDictionary<string, object?>? instanceData = null)
    {
        if (state is null)
            return _rootEditableFields is not null ? _rootEditableFields : EmptyStringSet.Instance;

        HashSet<string>? combined = null;
        if (_editableFieldsByState.TryGetValue(state, out var staticFields))
            combined = new HashSet<string>(staticFields, StringComparer.Ordinal);

        if (_guardedEditBlocks.Count > 0 && instanceData is not null)
        {
            var hydrated = HydrateInstanceData(instanceData);
            var guardedFields = EvaluateGuardedEditFields(state, hydrated);
            if (guardedFields.Count > 0)
            {
                combined ??= new HashSet<string>(StringComparer.Ordinal);
                combined.UnionWith(guardedFields);
            }
        }

        return combined is not null ? combined : EmptyStringSet.Instance;
    }

    /// <summary>
    /// Builds <see cref="PreceptEditableFieldInfo"/> entries for all editable fields
    /// in the given state, populated with current values from instance data.
    /// Returns null when no edit declarations exist for this engine.
    /// </summary>
    private IReadOnlyList<PreceptEditableFieldInfo>? BuildEditableFieldInfos(
        string? state, IReadOnlyDictionary<string, object?> instanceData)
    {
        // Stateless path handled by dedicated BuildEditableFieldInfosForStateless
        if (state is null)
            return BuildEditableFieldInfosForStateless(instanceData);

        // Build combined editable field set: static + guarded
        HashSet<string>? editableNames = null;
        if (_editableFieldsByState.TryGetValue(state, out var staticNames) && staticNames.Count > 0)
            editableNames = new HashSet<string>(staticNames, StringComparer.Ordinal);

        if (_guardedEditBlocks.Count > 0)
        {
            var hydrated = HydrateInstanceData(instanceData);
            var guardedFields = EvaluateGuardedEditFields(state, hydrated);
            if (guardedFields.Count > 0)
            {
                editableNames ??= new HashSet<string>(StringComparer.Ordinal);
                editableNames.UnionWith(guardedFields);
            }
        }

        if (editableNames is null || editableNames.Count == 0)
        {
            // Return null only if there are NO edit blocks at all (static or guarded)
            return (_editableFieldsByState.Count == 0 && _guardedEditBlocks.Count == 0) ? null : Array.Empty<PreceptEditableFieldInfo>();
        }

        var result = new List<PreceptEditableFieldInfo>();
        // Maintain declaration order: iterate Fields then CollectionFields
        foreach (var field in Fields)
        {
            if (!editableNames.Contains(field.Name))
                continue;
            instanceData.TryGetValue(field.Name, out var currentValue);
            var typeName = field.IsNullable
                ? field.Type.ToString().ToLowerInvariant()
                : field.Type.ToString().ToLowerInvariant();
            result.Add(new PreceptEditableFieldInfo(
                field.Name, typeName, field.IsNullable, currentValue));
        }
        foreach (var col in CollectionFields)
        {
            if (!editableNames.Contains(col.Name))
                continue;
            instanceData.TryGetValue(col.Name, out var currentValue);
            var typeName = $"{col.CollectionKind.ToString().ToLowerInvariant()}<{col.InnerType.ToString().ToLowerInvariant()}>";
            result.Add(new PreceptEditableFieldInfo(
                col.Name, typeName, false, currentValue));
        }
        return result;
    }

    /// <summary>
    /// Evaluates all invariants and the current state's 'in' asserts against the
    /// instance's current data. Returns a flat list of violation reason strings.
    /// </summary>
    internal IReadOnlyList<ConstraintViolation> EvaluateCurrentRules(PreceptInstance instance)
    {
        var violations = new List<ConstraintViolation>();
        violations.AddRange(EvaluateInvariants(instance.InstanceData));
        if (instance.CurrentState is not null)
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.In, instance.CurrentState, instance.InstanceData));
        return violations;
    }

    private TransitionResolution ResolveTransition(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?> evaluationData)
    {
        // SYNC:CONSTRAINT:C36
        if (string.IsNullOrWhiteSpace(currentState))
            throw new ArgumentException(DiagnosticCatalog.C36.FormatMessage(), nameof(currentState));
        // SYNC:CONSTRAINT:C37
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException(DiagnosticCatalog.C37.FormatMessage(), nameof(eventName));

        if (!States.Contains(currentState, StringComparer.Ordinal))
            return TransitionResolution.NotDefined($"Unknown state '{currentState}'.");

        if (!Events.Any(e => e.Name.Equals(eventName, StringComparison.Ordinal)))
            return TransitionResolution.NotDefined($"Unknown event '{eventName}'.");

        if (!_transitionRowMap.TryGetValue((currentState, eventName), out var rows) || rows.Count == 0)
            return TransitionResolution.NotDefined($"No transition for '{eventName}' from '{currentState}'.");

        // First-match evaluation: iterate rows, evaluate each row's WhenGuard.
        // First row whose guard passes (or has no guard) is the match.
        var reasons = new List<string>();
        foreach (var row in rows)
        {
            if (row.WhenGuard is not null)
            {
                var guardResult = PreceptExpressionRuntimeEvaluator.Evaluate(row.WhenGuard, evaluationData, _fieldMap);
                if (!guardResult.Success || guardResult.Value is not bool guardBool || !guardBool)
                {
                    reasons.Add(row.WhenText is not null
                        ? $"Guard '{row.WhenText}' failed."
                        : "Guard failed.");
                    continue;
                }
            }

            // Guard passed (or no guard — unguarded row)
            return row.Outcome switch
            {
                StateTransition => TransitionResolution.Accepted(row),
                NoTransition => TransitionResolution.NoTransition(row),
                Rejection rej => TransitionResolution.RejectedByRow(rej, eventName, row.SourceLine),
                _ => TransitionResolution.RejectedWithStrings(reasons)
            };
        }

        // No row matched — if all had guards, NotApplicable; otherwise Rejected.
        if (rows.All(r => r.WhenGuard is not null))
            return TransitionResolution.NotApplicable();
        return TransitionResolution.RejectedWithStrings(reasons);
    }

    private enum TransitionResolutionKind
    {
        Transition,
        Rejected,
        Undefined,
        NoTransition,
        Unmatched
    }

    private sealed record TransitionResolution(
        TransitionResolutionKind Kind,
        PreceptTransitionRow? MatchedRow,
        string? NotDefinedReason,
        IReadOnlyList<ConstraintViolation> Violations)
    {
        internal static TransitionResolution Accepted(PreceptTransitionRow row) =>
            new(TransitionResolutionKind.Transition, row, null, Array.Empty<ConstraintViolation>());

        internal static TransitionResolution RejectedByRow(Rejection rej, string eventName, int sourceLine)
        {
            var reason = string.IsNullOrWhiteSpace(rej.Reason)
                ? $"Event '{eventName}' was rejected."
                : rej.Reason!;
            var violation = new ConstraintViolation(
                reason,
                new ConstraintSource.TransitionRejectionSource(reason, eventName, sourceLine),
                new ConstraintTarget[] { new ConstraintTarget.EventTarget(eventName) });
            return new(TransitionResolutionKind.Rejected, null, null, new[] { violation });
        }

        internal static TransitionResolution RejectedWithStrings(IReadOnlyList<string> reasons)
        {
            var violations = reasons.Select(r => new ConstraintViolation(r,
                new ConstraintSource.TransitionRejectionSource(r, "", 0),
                new ConstraintTarget[] { new ConstraintTarget.DefinitionTarget() })).ToList();
            return new(TransitionResolutionKind.Rejected, null, null, violations);
        }

        internal static TransitionResolution NotDefined(string reason) =>
            new(TransitionResolutionKind.Undefined, null, reason, Array.Empty<ConstraintViolation>());

        internal static TransitionResolution NoTransition(PreceptTransitionRow row) =>
            new(TransitionResolutionKind.NoTransition, row, null, Array.Empty<ConstraintViolation>());

        internal static TransitionResolution NotApplicable() =>
            new(TransitionResolutionKind.Unmatched, null, null, Array.Empty<ConstraintViolation>());
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

    // ---- Constraint evaluation helpers ----

    private IReadOnlyList<ConstraintViolation> EvaluateEventAssertions(string eventName, IReadOnlyDictionary<string, object?> evaluationData)
    {
        if (!_eventAssertMap.TryGetValue(eventName, out var asserts) || asserts.Count == 0)
            return Array.Empty<ConstraintViolation>();

        var violations = new List<ConstraintViolation>();
        foreach (var assert in asserts)
        {
            // Guard pre-flight: when guard is present and evaluates false, skip this assertion
            if (assert.WhenGuard is not null)
            {
                var guardResult = PreceptExpressionRuntimeEvaluator.Evaluate(assert.WhenGuard, evaluationData);
                if (!guardResult.Success || guardResult.Value is not true)
                    continue;
            }

            var result = PreceptExpressionRuntimeEvaluator.Evaluate(assert.Expression, evaluationData);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
            {
                var subjects = ExpressionSubjects.ExtractForEventAssert(assert.Expression, eventName);
                var targets = new List<ConstraintTarget>();
                foreach (var (evt, arg) in subjects.ArgReferences)
                    targets.Add(new ConstraintTarget.EventArgTarget(evt, arg));
                targets.Add(new ConstraintTarget.EventTarget(eventName));

                violations.Add(new ConstraintViolation(
                    assert.Reason,
                    new ConstraintSource.EventAssertionSource(assert.ExpressionText, assert.Reason, eventName, assert.SourceLine),
                    targets));
            }
        }
        return violations;
    }

    private IReadOnlyList<ConstraintViolation> EvaluateInvariants(IReadOnlyDictionary<string, object?> data)
    {
        if (_invariants.Count == 0)
            return Array.Empty<ConstraintViolation>();

        var violations = new List<ConstraintViolation>();
        foreach (var inv in _invariants)
        {
            // Guard pre-flight: when guard is present and evaluates false, skip this invariant
            if (inv.WhenGuard is not null)
            {
                var guardResult = PreceptExpressionRuntimeEvaluator.Evaluate(inv.WhenGuard, data);
                if (!guardResult.Success || guardResult.Value is not true)
                    continue;
            }

            var result = PreceptExpressionRuntimeEvaluator.Evaluate(inv.Expression, data);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
            {
                var subjects = ExpressionSubjects.Extract(inv.Expression);
                var targets = new List<ConstraintTarget>();
                foreach (var fieldName in subjects.FieldReferences)
                    targets.Add(new ConstraintTarget.FieldTarget(fieldName));
                targets.Add(new ConstraintTarget.DefinitionTarget());

                violations.Add(new ConstraintViolation(
                    inv.Reason,
                    new ConstraintSource.InvariantSource(inv.ExpressionText, inv.Reason, inv.SourceLine),
                    targets));
            }
        }
        return violations;
    }

    /// <summary>
    /// Evaluates state asserts for a given preposition and state.
    /// </summary>
    private IReadOnlyList<ConstraintViolation> EvaluateStateAssertions(
        AssertAnchor preposition, string state, IReadOnlyDictionary<string, object?> data)
    {
        if (!_stateAssertMap.TryGetValue((preposition, state), out var asserts) || asserts.Count == 0)
            return Array.Empty<ConstraintViolation>();

        var violations = new List<ConstraintViolation>();
        foreach (var assert in asserts)
        {
            // Guard pre-flight: when guard is present and evaluates false, skip this assertion
            if (assert.WhenGuard is not null)
            {
                var guardResult = PreceptExpressionRuntimeEvaluator.Evaluate(assert.WhenGuard, data);
                if (!guardResult.Success || guardResult.Value is not true)
                    continue;
            }

            var result = PreceptExpressionRuntimeEvaluator.Evaluate(assert.Expression, data);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
            {
                var subjects = ExpressionSubjects.Extract(assert.Expression);
                var targets = new List<ConstraintTarget>();
                foreach (var fieldName in subjects.FieldReferences)
                    targets.Add(new ConstraintTarget.FieldTarget(fieldName));
                targets.Add(new ConstraintTarget.StateTarget(assert.State, assert.Anchor));

                violations.Add(new ConstraintViolation(
                    assert.Reason,
                    new ConstraintSource.StateAssertionSource(assert.ExpressionText, assert.Reason, assert.State, assert.Anchor, assert.SourceLine),
                    targets));
            }
        }
        return violations;
    }

    /// <summary>
    /// Collects all validation violations post-mutation: invariants + preposition-aware state asserts.
    /// </summary>
    private IReadOnlyList<ConstraintViolation> CollectConstraintViolations(
        string sourceState, string targetState, IReadOnlyDictionary<string, object?> data)
    {
        var violations = new List<ConstraintViolation>();

        // Invariants (always)
        violations.AddRange(EvaluateInvariants(data));

        if (string.Equals(sourceState, targetState, StringComparison.Ordinal))
        {
            // AcceptedInPlace / self-transition: evaluate 'to' + 'in' asserts
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.To, targetState, data));
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.In, sourceState, data));
        }
        else
        {
            // State transition: evaluate 'from' source + 'to' target + 'in' target
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.From, sourceState, data));
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.To, targetState, data));
            violations.AddRange(EvaluateStateAssertions(AssertAnchor.In, targetState, data));
        }

        return violations;
    }

    /// <summary>
    /// Executes state entry/exit actions (automatic mutations).
    /// </summary>
    private void ExecuteStateActions(
        AssertAnchor preposition, string state,
        Dictionary<string, object?> data, Dictionary<string, CollectionValue> workingCollections,
        string eventName, IReadOnlyDictionary<string, object?>? eventArguments)
    {
        if (!_stateActionMap.TryGetValue((preposition, state), out var actions))
            return;

        foreach (var action in actions)
        {
            foreach (var assignment in action.SetAssignments)
            {
                var ctx = BuildEvaluationDataWithCollections(data, workingCollections, eventName, eventArguments);
                var eval = PreceptExpressionRuntimeEvaluator.Evaluate(assignment.Expression, ctx);
                if (eval.Success)
                    data[assignment.Key] = eval.Value;
            }

            if (action.CollectionMutations is { } mutations)
                ExecuteCollectionMutations(mutations, workingCollections, data, eventName, eventArguments);
        }
    }

    /// <summary>
    /// Executes set assignments and collection mutations from a matched transition row.
    /// Returns an error string on failure, null on success.
    /// </summary>
    private string? ExecuteRowMutations(
        PreceptTransitionRow row,
        Dictionary<string, object?> data, Dictionary<string, CollectionValue> workingCollections,
        string eventName, IReadOnlyDictionary<string, object?>? eventArguments)
    {
        foreach (var assignment in row.SetAssignments)
        {
            var ctx = BuildEvaluationDataWithCollections(data, workingCollections, eventName, eventArguments);
            var eval = PreceptExpressionRuntimeEvaluator.Evaluate(assignment.Expression, ctx);
            if (!eval.Success)
                return $"Data assignment failed: {eval.Error}";

            if (!TryValidateAssignedValue(assignment.Key, eval.Value, out var contractError))
                return contractError;

            data[assignment.Key] = eval.Value;
        }

        if (row.CollectionMutations is { } mutations)
        {
            var mutError = ExecuteCollectionMutations(mutations, workingCollections, data, eventName, eventArguments);
            if (mutError is not null)
                return mutError;
        }

        return null;
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
        IReadOnlyList<PreceptCollectionMutation> mutations,
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
                var result = PreceptExpressionRuntimeEvaluator.Evaluate(mutation.Expression, ctx);
                if (!result.Success)
                    return $"Collection mutation failed: {result.Error}";
                argValue = result.Value;
            }

            switch (mutation.Verb)
            {
                case PreceptCollectionMutationVerb.Add:
                    collection.Add(argValue!);
                    break;
                case PreceptCollectionMutationVerb.Remove:
                    collection.Remove(argValue!);
                    break;
                case PreceptCollectionMutationVerb.Enqueue:
                    collection.Enqueue(argValue!);
                    break;
                case PreceptCollectionMutationVerb.Dequeue:
                    if (collection.Count == 0)
                        return $"Collection mutation failed: cannot dequeue from empty queue '{mutation.TargetField}'.";
                    if (mutation.IntoField is not null)
                        data[mutation.IntoField] = collection.Peek();
                    collection.Dequeue();
                    break;
                case PreceptCollectionMutationVerb.Push:
                    collection.Push(argValue!);
                    break;
                case PreceptCollectionMutationVerb.Pop:
                    if (collection.Count == 0)
                        return $"Collection mutation failed: cannot pop from empty stack '{mutation.TargetField}'.";
                    if (mutation.IntoField is not null)
                        data[mutation.IntoField] = collection.Peek();
                    collection.Pop();
                    break;
                case PreceptCollectionMutationVerb.Clear:
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
            // Collection fields are stored under their plain names in clean InstanceData
            if (_collectionFieldMap.ContainsKey(key))
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

            // Choice membership check for event args
            if (arg.Type == PreceptScalarType.Choice &&
                value is string argStrVal &&
                arg.ChoiceValues is not null &&
                !arg.ChoiceValues.Contains(argStrVal, StringComparer.Ordinal))
            {
                error = $"Event argument validation failed: '{argStrVal}' is not a member of choice({string.Join(", ", arg.ChoiceValues.Select(v => $"\"{v}\""))}) for argument '{arg.Name}'.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryToDecimalValue(object? value, out decimal d)
    {
        switch (value)
        {
            case decimal dec: d = dec; return true;
            case double dbl: d = (decimal)dbl; return true;
            case float flt: d = (decimal)flt; return true;
            case long l: d = l; return true;
            case int i: d = i; return true;
            default: d = default; return false;
        }
    }

    private static bool ViolatesMaxplaces(decimal value, int places)
    {
        // Count actual decimal places by removing trailing zeros
        var scale = (int)BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
        return scale > places;
    }

    private bool TryValidateAssignedValue(string dataFieldName, object? value, out string error)
    {
        if (!_fieldMap.TryGetValue(dataFieldName, out var contract))
        {
            error = string.Empty;
            return true;
        }

        if (!TryValidateScalarValue(contract.Name, contract.Type, contract.IsNullable, value, out error))
        {
            error = $"Data assignment failed: {error}";
            return false;
        }

        if (value is null)
        {
            error = string.Empty;
            return true;
        }

        // maxplaces constraint check for decimal fields
        if (contract.Type == PreceptScalarType.Decimal && contract.Constraints is not null)
        {
            foreach (var c in contract.Constraints)
            {
                if (c is FieldConstraint.Maxplaces mp && TryToDecimalValue(value, out var dv) &&
                    ViolatesMaxplaces(dv, mp.Places))
                {
                    error = $"Data assignment failed: '{contract.Name}' value exceeds maxplaces {mp.Places}.";
                    return false;
                }
            }
        }

        // Choice membership check
        if (contract.Type == PreceptScalarType.Choice &&
            value is string strVal &&
            contract.ChoiceValues is not null &&
            !contract.ChoiceValues.Contains(strVal, StringComparer.Ordinal))
        {
            error = $"Data assignment failed: '{strVal}' is not a member of choice({string.Join(", ", contract.ChoiceValues.Select(v => $"\"{v}\""))}) for field '{contract.Name}'.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateScalarValue(string name, PreceptScalarType type, bool isNullable, object? value, out string error)
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
            PreceptScalarType.String => value is string,
            PreceptScalarType.Boolean => value is bool,
            PreceptScalarType.Number => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal,
            PreceptScalarType.Integer => value is long or int or short or byte or sbyte,
            PreceptScalarType.Decimal => value is decimal or double or float or long or int or short or byte or sbyte,
            PreceptScalarType.Choice => value is string,
            PreceptScalarType.Null => false,
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

public sealed record PreceptDiagnostic(
    int Line,
    int Column,
    string Message,
    string? Code,
    ConstraintSeverity Severity,
    string? StateContext = null);

public sealed record CompileFromTextResult(
    PreceptDefinition? Model,
    PreceptEngine? Engine,
    IReadOnlyList<PreceptDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == ConstraintSeverity.Error);
}

public static class PreceptCompiler
{
    internal static ValidationResult Validate(PreceptDefinition model)
    {
        // SYNC:CONSTRAINT:C26
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        var diagnostics = new List<PreceptValidationDiagnostic>();

        if (!model.IsStateless)
        {
            // SYNC:CONSTRAINT:C27
            if (string.IsNullOrWhiteSpace(model.InitialState!.Name))
            {
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C27,
                    DiagnosticCatalog.C27.FormatMessage(),
                    model.SourceLine));
            }

            // SYNC:CONSTRAINT:C28
            if (!model.States.Any(state => string.Equals(state.Name, model.InitialState!.Name, StringComparison.Ordinal)))
            {
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C28,
                    DiagnosticCatalog.C28.FormatMessage(("stateName", model.InitialState!.Name), ("workflowName", model.Name)),
                    model.InitialState!.SourceLine > 0 ? model.InitialState!.SourceLine : model.SourceLine,
                    StateContext: model.InitialState!.Name));
            }
        }

        // SYNC:CONSTRAINT:C38
        // SYNC:CONSTRAINT:C39
        // SYNC:CONSTRAINT:C40
        // SYNC:CONSTRAINT:C41
        // SYNC:CONSTRAINT:C42
        // SYNC:CONSTRAINT:C43
        // SYNC:CONSTRAINT:C46
        // SYNC:CONSTRAINT:C47
        var typeCheck = PreceptTypeChecker.Check(model);
        diagnostics.AddRange(typeCheck.Diagnostics);

        CollectCompileTimeDiagnostics(model, diagnostics);

        if (!diagnostics.Any(diagnostic => diagnostic.Constraint.Id is "C27" or "C28"))
            diagnostics.AddRange(PreceptAnalysis.Analyze(model).Diagnostics);

        return new ValidationResult(diagnostics, typeCheck.TypeContext);
    }

    public static CompileFromTextResult CompileFromText(string text)
    {
        var (model, parseDiagnostics) = PreceptParser.ParseWithDiagnostics(text);
        if (model is null || parseDiagnostics.Count > 0)
            return new CompileFromTextResult(null, null, parseDiagnostics.Select(ToDiagnostic).ToArray());

        var validation = Validate(model);
        var diagnostics = validation.Diagnostics.Select(ToDiagnostic).ToArray();
        if (validation.HasErrors)
            return new CompileFromTextResult(model, null, diagnostics);

        return new CompileFromTextResult(model, new PreceptEngine(model), diagnostics);
    }

    public static PreceptEngine Compile(PreceptDefinition model)
    {
        var validation = Validate(model);
        ThrowIfValidationFailed(validation);
        return new PreceptEngine(model);
    }

    private static PreceptDiagnostic ToDiagnostic(ParseDiagnostic diagnostic)
        => new(diagnostic.Line, diagnostic.Column, diagnostic.Message, diagnostic.Code, ConstraintSeverity.Error);

    private static PreceptDiagnostic ToDiagnostic(PreceptValidationDiagnostic diagnostic)
        => new(diagnostic.Line, diagnostic.Column, diagnostic.Message, diagnostic.DiagnosticCode, diagnostic.Constraint.Severity, diagnostic.StateContext);

    private static void ThrowIfValidationFailed(ValidationResult validation)
    {
        if (!validation.HasErrors)
            return;

        var first = validation.Diagnostics.First(diagnostic => diagnostic.Constraint.Severity == ConstraintSeverity.Error);
        var location = first.Line > 0 ? $"Line {first.Line}: " : string.Empty;
        var stateContext = string.IsNullOrWhiteSpace(first.StateContext)
            ? string.Empty
            : $" [state {first.StateContext}]";
        throw new InvalidOperationException($"{location}{first.DiagnosticCode}{stateContext}: {first.Message}");
    }

    private static void CollectCompileTimeDiagnostics(PreceptDefinition model, List<PreceptValidationDiagnostic> diagnostics)
    {
        // Structural checks: duplicate and subsumed state asserts
        CollectDuplicateStateAssertDiagnostics(model, diagnostics);
        CollectSubsumedStateAssertDiagnostics(model, diagnostics);

        // SYNC:CONSTRAINT:C55
        if (!model.IsStateless && model.EditBlocks is { Count: > 0 })
        {
            foreach (var eb in model.EditBlocks.Where(static eb => eb.State is null))
            {
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C55,
                    DiagnosticCatalog.C55.FormatMessage(),
                    eb.SourceLine));
            }
        }

        var defaultData = BuildDefaultData(model);

        // 1. Validate invariants against default values
        //    Synthetic invariants (generated by field constraint desugaring) are skipped:
        //    C59 covers bad defaults for scalar constraints; collection constraints have no
        //    user-declared default and should not block compilation of the precept.
        if (model.Invariants is { Count: > 0 })
        {
            foreach (var inv in model.Invariants)
            {
                if (inv.IsSynthetic) continue;
                var result = PreceptExpressionRuntimeEvaluator.Evaluate(inv.Expression, defaultData);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                {
                    // SYNC:CONSTRAINT:C29
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C29,
                        DiagnosticCatalog.C29.FormatMessage(("reason", inv.Reason)),
                        inv.SourceLine));
                }
            }
        }

        // 2. Validate initial state asserts (in + to) against default data
        // Stateless precepts have no state asserts — InitialState is null and this block is unreachable for them.
        if (!model.IsStateless && model.StateAsserts is { Count: > 0 })
        {
            var initialStateName = model.InitialState!.Name;
            foreach (var sa in model.StateAsserts)
            {
                if (!string.Equals(sa.State, initialStateName, StringComparison.Ordinal))
                    continue;
                if (sa.Anchor is not (AssertAnchor.In or AssertAnchor.To))
                    continue;

                var result = PreceptExpressionRuntimeEvaluator.Evaluate(sa.Expression, defaultData);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                {
                    // SYNC:CONSTRAINT:C30
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C30,
                        DiagnosticCatalog.C30.FormatMessage(("reason", sa.Reason), ("stateName", initialStateName)),
                        sa.SourceLine,
                        StateContext: sa.State));
                }
            }
        }

        // 3. Validate event asserts against event argument defaults (when all args have defaults)
        if (model.EventAsserts is { Count: > 0 })
        {
            var eventsByName = model.Events.ToDictionary(e => e.Name, e => e, StringComparer.Ordinal);
            foreach (var ea in model.EventAsserts)
            {
                if (!eventsByName.TryGetValue(ea.EventName, out var evt))
                    continue;

                var eventDefaults = new Dictionary<string, object?>(StringComparer.Ordinal);
                bool allArgsHaveDefaults = true;
                foreach (var arg in evt.Args)
                {
                    if (arg.HasDefaultValue)
                    {
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

                var result = PreceptExpressionRuntimeEvaluator.Evaluate(ea.Expression, eventDefaults);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                {
                    // SYNC:CONSTRAINT:C31
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C31,
                        DiagnosticCatalog.C31.FormatMessage(("reason", ea.Reason), ("eventName", ea.EventName)),
                        ea.SourceLine));
                }
            }
        }

        // 4. Validate literal set assignments against invariants
        CollectLiteralSetAssignmentDiagnostics(model, defaultData, diagnostics);
    }

    private static void CollectLiteralSetAssignmentDiagnostics(PreceptDefinition model, IReadOnlyDictionary<string, object?> defaultData, List<PreceptValidationDiagnostic> diagnostics)
    {
        var invariants = model.Invariants ?? Array.Empty<PreceptInvariant>();

        if (invariants.Count == 0)
            return;

        void CheckLiteralAssignment(PreceptSetAssignment assignment)
        {
            var constantEval = PreceptExpressionRuntimeEvaluator.Evaluate(assignment.Expression, EmptyInstanceData.Instance);
            if (!constantEval.Success)
                return;

            var ctx = new Dictionary<string, object?>(defaultData, StringComparer.Ordinal)
            {
                [assignment.Key] = constantEval.Value
            };

            foreach (var inv in invariants)
            {
                var result = PreceptExpressionRuntimeEvaluator.Evaluate(inv.Expression, ctx);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                {
                    // SYNC:CONSTRAINT:C32
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C32,
                        DiagnosticCatalog.C32.FormatMessage(("sourceLine", assignment.SourceLine), ("key", assignment.Key), ("expression", assignment.ExpressionText), ("reason", inv.Reason)),
                        assignment.SourceLine));
                }
            }
        }

        if (model.TransitionRows is { Count: > 0 })
        {
            foreach (var row in model.TransitionRows)
            {
                foreach (var assignment in row.SetAssignments)
                    CheckLiteralAssignment(assignment);
            }
        }
    }

    // SYNC:CONSTRAINT:C44
    private static void CollectDuplicateStateAssertDiagnostics(PreceptDefinition model, List<PreceptValidationDiagnostic> diagnostics)
    {
        if (model.StateAsserts is not { Count: > 1 })
            return;

        var seen = new HashSet<(AssertAnchor Anchor, string State, string Expression)>();
        foreach (var sa in model.StateAsserts)
        {
            if (!seen.Add((sa.Anchor, sa.State, sa.ExpressionText)))
            {
                var prep = sa.Anchor.ToString().ToLowerInvariant();
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C44,
                    DiagnosticCatalog.C44.FormatMessage(
                        ("preposition", prep), ("state", sa.State),
                        ("expression", sa.ExpressionText), ("sourceLine", sa.SourceLine)),
                    sa.SourceLine,
                    StateContext: sa.State));
            }
        }
    }

    // SYNC:CONSTRAINT:C45
    private static void CollectSubsumedStateAssertDiagnostics(PreceptDefinition model, List<PreceptValidationDiagnostic> diagnostics)
    {
        if (model.StateAsserts is not { Count: > 1 })
            return;

        var inAsserts = new HashSet<(string State, string Expr)>();
        foreach (var sa in model.StateAsserts)
        {
            if (sa.Anchor == AssertAnchor.In)
                inAsserts.Add((sa.State, sa.ExpressionText));
        }

        if (inAsserts.Count == 0)
            return;

        foreach (var sa in model.StateAsserts)
        {
            if (sa.Anchor == AssertAnchor.To && inAsserts.Contains((sa.State, sa.ExpressionText)))
            {
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C45,
                    DiagnosticCatalog.C45.FormatMessage(
                        ("state", sa.State), ("expression", sa.ExpressionText),
                        ("sourceLine", sa.SourceLine)),
                    sa.SourceLine,
                    StateContext: sa.State));
            }
        }
    }

    private static IReadOnlyDictionary<string, object?> BuildDefaultData(PreceptDefinition model)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in model.Fields)
        {
            if (field.HasDefaultValue)
                data[field.Name] = field.DefaultValue;
        }
        foreach (var col in model.CollectionFields)
        {
            data[$"__collection__{col.Name}"] = new CollectionValue(col.CollectionKind, col.InnerType);
        }
        return data;
    }
}

public sealed record PreceptInstance(
    string WorkflowName,
    string? CurrentState,
    string? LastEvent,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, object?> InstanceData);

public sealed record PreceptCompatibilityResult(bool IsCompatible, string? Reason)
{
    internal static PreceptCompatibilityResult Compatible() => new(true, null);
    internal static PreceptCompatibilityResult NotCompatible(string reason) => new(false, reason);
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

public sealed record EventInspectionResult(
    TransitionOutcome Outcome,
    string? CurrentState,
    string EventName,
    string? TargetState,
    IReadOnlyList<string> RequiredEventArgumentKeys,
    IReadOnlyList<ConstraintViolation> Violations)
{
    public bool IsSuccess => Outcome is TransitionOutcome.Transition
        or TransitionOutcome.NoTransition;

    internal static EventInspectionResult Transitioned(string? state, string evt, string target, IReadOnlyList<string> requiredEventArgumentKeys) =>
        new(TransitionOutcome.Transition, state, evt, target, requiredEventArgumentKeys, Array.Empty<ConstraintViolation>());

    internal static EventInspectionResult NoTransition(string? state, string evt) =>
        new(TransitionOutcome.NoTransition, state, evt, state, Array.Empty<string>(), Array.Empty<ConstraintViolation>());

    internal static EventInspectionResult Undefined(string? state, string evt, string reason) =>
        new(TransitionOutcome.Undefined, state, evt, null, Array.Empty<string>(),
            new[] { ConstraintViolation.Simple(reason) });

    internal static EventInspectionResult Unmatched(string? state, string evt) =>
        new(TransitionOutcome.Unmatched, state, evt, null, Array.Empty<string>(), Array.Empty<ConstraintViolation>());

    internal static EventInspectionResult Rejected(string? state, string evt, IReadOnlyList<ConstraintViolation> violations) =>
        new(TransitionOutcome.Rejected, state, evt, null, Array.Empty<string>(), violations);

    internal static EventInspectionResult ConstraintFailure(string? state, string evt, IReadOnlyList<ConstraintViolation> violations) =>
        new(TransitionOutcome.ConstraintFailure, state, evt, null, Array.Empty<string>(), violations);
}

public sealed record InspectionResult(
    string? CurrentState,
    IReadOnlyDictionary<string, object?> InstanceData,
    IReadOnlyList<EventInspectionResult> Events,
    IReadOnlyList<PreceptEditableFieldInfo>? EditableFields = null);

public sealed record PreceptEditableFieldInfo(
    string FieldName,
    string FieldType,
    bool IsNullable,
    object? CurrentValue,
    string? Violation = null);

public enum UpdateOutcome
{
    Update,
    UneditableField,
    ConstraintFailure,
    InvalidInput
}

public sealed record UpdateResult(
    UpdateOutcome Outcome,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance)
{
    public bool IsSuccess => Outcome is UpdateOutcome.Update;

    internal static UpdateResult Succeeded(PreceptInstance updated) =>
        new(UpdateOutcome.Update, Array.Empty<ConstraintViolation>(), updated);

    internal static UpdateResult Failed(UpdateOutcome outcome, IReadOnlyList<ConstraintViolation> violations) =>
        new(outcome, violations, null);

    internal static UpdateResult Failed(UpdateOutcome outcome, IReadOnlyList<string> reasons) =>
        new(outcome, reasons.Select(r => new ConstraintViolation(r,
            new ConstraintSource.InvariantSource("", r),
            new[] { new ConstraintTarget.DefinitionTarget() as ConstraintTarget })).ToList(), null);
}

public interface IUpdatePatchBuilder
{
    IUpdatePatchBuilder Set(string fieldName, object? value);
    IUpdatePatchBuilder Add(string fieldName, object value);
    IUpdatePatchBuilder Remove(string fieldName, object value);
    IUpdatePatchBuilder Enqueue(string fieldName, object value);
    IUpdatePatchBuilder Dequeue(string fieldName);
    IUpdatePatchBuilder Push(string fieldName, object value);
    IUpdatePatchBuilder Pop(string fieldName);
    IUpdatePatchBuilder Replace(string fieldName, IEnumerable<object> values);
    IUpdatePatchBuilder Clear(string fieldName);
}

internal enum UpdateOpKind
{
    Set,
    Add,
    Remove,
    Enqueue,
    Dequeue,
    Push,
    Pop,
    Replace,
    Clear
}

internal sealed record UpdatePatchOperation(
    string FieldName,
    UpdateOpKind Kind,
    object? Value = null,
    IReadOnlyList<object>? Values = null);

internal sealed class UpdatePatchBuilder : IUpdatePatchBuilder
{
    private readonly List<UpdatePatchOperation> _operations = new();

    public IUpdatePatchBuilder Set(string fieldName, object? value)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Set, value));
        return this;
    }

    public IUpdatePatchBuilder Add(string fieldName, object value)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Add, value));
        return this;
    }

    public IUpdatePatchBuilder Remove(string fieldName, object value)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Remove, value));
        return this;
    }

    public IUpdatePatchBuilder Enqueue(string fieldName, object value)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Enqueue, value));
        return this;
    }

    public IUpdatePatchBuilder Dequeue(string fieldName)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Dequeue));
        return this;
    }

    public IUpdatePatchBuilder Push(string fieldName, object value)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Push, value));
        return this;
    }

    public IUpdatePatchBuilder Pop(string fieldName)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Pop));
        return this;
    }

    public IUpdatePatchBuilder Replace(string fieldName, IEnumerable<object> values)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Replace, Values: values.ToList()));
        return this;
    }

    public IUpdatePatchBuilder Clear(string fieldName)
    {
        _operations.Add(new UpdatePatchOperation(fieldName, UpdateOpKind.Clear));
        return this;
    }

    public IReadOnlyList<UpdatePatchOperation> GetOperations() => _operations;

    public string? ValidateConflicts(
        Dictionary<string, PreceptField> fieldMap,
        Dictionary<string, PreceptCollectionField> collectionFieldMap)
    {
        // Duplicate Set on same scalar field
        var scalarSets = new HashSet<string>(StringComparer.Ordinal);
        var collectionReplacements = new HashSet<string>(StringComparer.Ordinal);
        var collectionGranular = new HashSet<string>(StringComparer.Ordinal);

        foreach (var op in _operations)
        {
            bool isScalar = fieldMap.ContainsKey(op.FieldName);
            bool isCollection = collectionFieldMap.ContainsKey(op.FieldName);

            if (isScalar && op.Kind == UpdateOpKind.Set)
            {
                if (!scalarSets.Add(op.FieldName))
                    return $"Duplicate Set on field '{op.FieldName}'.";
            }

            if (isCollection && op.Kind == UpdateOpKind.Set)
                return $"Use Replace for collection fields (field '{op.FieldName}').";

            if (isScalar && op.Kind != UpdateOpKind.Set)
                return $"Granular operations are only valid on collection fields (field '{op.FieldName}' is scalar).";

            if (isCollection && op.Kind == UpdateOpKind.Replace)
                collectionReplacements.Add(op.FieldName);

            if (isCollection && op.Kind is not UpdateOpKind.Replace and not UpdateOpKind.Clear)
                collectionGranular.Add(op.FieldName);
        }

        // Replace + granular on same collection
        foreach (var field in collectionReplacements)
        {
            if (collectionGranular.Contains(field))
                return $"Cannot combine Replace with granular operations on field '{field}'.";
        }

        return null;
    }
}

internal sealed class EmptyStringSet : IReadOnlySet<string>
{
    internal static readonly EmptyStringSet Instance = new();

    public int Count => 0;
    public bool Contains(string item) => false;
    public bool IsProperSubsetOf(IEnumerable<string> other) => other.Any();
    public bool IsProperSupersetOf(IEnumerable<string> other) => false;
    public bool IsSubsetOf(IEnumerable<string> other) => true;
    public bool IsSupersetOf(IEnumerable<string> other) => !other.Any();
    public bool Overlaps(IEnumerable<string> other) => false;
    public bool SetEquals(IEnumerable<string> other) => !other.Any();
    public IEnumerator<string> GetEnumerator() => Enumerable.Empty<string>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed record FireResult(
    TransitionOutcome Outcome,
    string? PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance)
{
    public bool IsSuccess => Outcome is TransitionOutcome.Transition
        or TransitionOutcome.NoTransition;

    internal static FireResult Transitioned(string? state, string evt, string newState, PreceptInstance updated) =>
        new(TransitionOutcome.Transition, state, evt, newState, Array.Empty<ConstraintViolation>(), updated);

    internal static FireResult NoTransition(string? state, string evt, PreceptInstance updated) =>
        new(TransitionOutcome.NoTransition, state, evt, state, Array.Empty<ConstraintViolation>(), updated);

    internal static FireResult Undefined(string? state, string evt, IReadOnlyList<string> reasons) =>
        new(TransitionOutcome.Undefined, state, evt, null,
            reasons.Select(r => new ConstraintViolation(r,
                new ConstraintSource.InvariantSource("", r),
                new[] { new ConstraintTarget.DefinitionTarget() as ConstraintTarget })).ToList(), null);

    internal static FireResult Unmatched(string? state, string evt) =>
        new(TransitionOutcome.Unmatched, state, evt, null, Array.Empty<ConstraintViolation>(), null);

    internal static FireResult Rejected(string? state, string evt, IReadOnlyList<ConstraintViolation> violations) =>
        new(TransitionOutcome.Rejected, state, evt, null, violations, null);

    internal static FireResult ConstraintFailure(string? state, string evt, IReadOnlyList<ConstraintViolation> violations) =>
        new(TransitionOutcome.ConstraintFailure, state, evt, null, violations, null);
}

public enum TransitionOutcome
{
    Undefined,
    Unmatched,
    Rejected,
    ConstraintFailure,
    Transition,
    NoTransition
}
