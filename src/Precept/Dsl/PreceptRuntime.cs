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
    private readonly IReadOnlyList<PreceptEventAssert> _eventAsserts;
    private readonly Dictionary<string, List<PreceptEventAssert>> _eventAssertMap;
    private readonly IReadOnlyList<PreceptStateAssert> _stateAsserts;
    private readonly IReadOnlyList<PreceptStateAction> _stateActions;
    private readonly Dictionary<(PreceptAssertPreposition Prep, string State), List<PreceptStateAssert>> _stateAssertMap;
    private readonly Dictionary<(PreceptAssertPreposition Prep, string State), List<PreceptStateAction>> _stateActionMap;

    // Editability: state → set of editable field names (union of all matching edit blocks)
    private readonly IReadOnlyDictionary<string, HashSet<string>> _editableFieldsByState;

    public string Name { get; }
    public IReadOnlyList<string> States { get; }
    public string InitialState { get; }
    public IReadOnlyList<PreceptEvent> Events { get; }
    public IReadOnlyList<PreceptField> Fields { get; }
    public IReadOnlyList<PreceptCollectionField> CollectionFields { get; }

    internal PreceptEngine(PreceptDefinition model)
    {
        Name = model.Name;
        States = model.States.Select(s => s.Name).ToArray();
        InitialState = model.InitialState.Name;
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
        _eventAsserts = model.EventAsserts ?? Array.Empty<PreceptEventAssert>();
        _eventAssertMap = new Dictionary<string, List<PreceptEventAssert>>(StringComparer.Ordinal);
        foreach (var ea in _eventAsserts)
        {
            if (!_eventAssertMap.TryGetValue(ea.EventName, out var list))
            {
                list = new List<PreceptEventAssert>();
                _eventAssertMap[ea.EventName] = list;
            }
            list.Add(ea);
        }

        // State asserts (preposition-aware)
        _stateAsserts = model.StateAsserts ?? Array.Empty<PreceptStateAssert>();
        _stateAssertMap = new Dictionary<(PreceptAssertPreposition Prep, string State), List<PreceptStateAssert>>();
        foreach (var sa in _stateAsserts)
        {
            var key = (sa.Preposition, sa.State);
            if (!_stateAssertMap.TryGetValue(key, out var list))
            {
                list = new List<PreceptStateAssert>();
                _stateAssertMap[key] = list;
            }
            list.Add(sa);
        }

        // State actions (entry/exit automatic mutations)
        _stateActions = model.StateActions ?? Array.Empty<PreceptStateAction>();
        _stateActionMap = new Dictionary<(PreceptAssertPreposition Prep, string State), List<PreceptStateAction>>();
        foreach (var sa in _stateActions)
        {
            var key = (sa.Preposition, sa.State);
            if (!_stateActionMap.TryGetValue(key, out var list))
            {
                list = new List<PreceptStateAction>();
                _stateActionMap[key] = list;
            }
            list.Add(sa);
        }

        // Editable fields: build per-state union of editable field names
        var editMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        if (model.EditBlocks is { Count: > 0 })
        {
            foreach (var block in model.EditBlocks)
            {
                if (!editMap.TryGetValue(block.State, out var fieldSet))
                {
                    fieldSet = new HashSet<string>(StringComparer.Ordinal);
                    editMap[block.State] = fieldSet;
                }
                foreach (var fieldName in block.FieldNames)
                    fieldSet.Add(fieldName);
            }
        }
        _editableFieldsByState = editMap;
    }

    public PreceptInstance CreateInstance(
        IReadOnlyDictionary<string, object?>? instanceData = null)
        => CreateInstance(InitialState, instanceData);

    public PreceptInstance CreateInstance(
        string initialState,
        IReadOnlyDictionary<string, object?>? instanceData = null)
    {
        // SYNC:CONSTRAINT:C33
        if (string.IsNullOrWhiteSpace(initialState))
            throw new ArgumentException(ConstraintCatalog.C33.FormatMessage(), nameof(initialState));

        // SYNC:CONSTRAINT:C34
        if (!States.Contains(initialState, StringComparer.Ordinal))
            throw new InvalidOperationException(ConstraintCatalog.C34.FormatMessage(("stateName", initialState), ("workflowName", Name)));

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

        if (!States.Contains(instance.CurrentState, StringComparer.Ordinal))
            return PreceptCompatibilityResult.NotCompatible($"Instance state '{instance.CurrentState}' is not defined in workflow '{Name}'.");

        if (!TryValidateDataContract(instance.InstanceData, out var dataError))
            return PreceptCompatibilityResult.NotCompatible(dataError);

        // Verify the instance satisfies all current constraints (invariants + state asserts).
        var internalData = HydrateInstanceData(instance.InstanceData);
        var ruleViolations = new List<string>();
        ruleViolations.AddRange(EvaluateInvariants(internalData));
        ruleViolations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.In, instance.CurrentState, internalData));
        if (ruleViolations.Count > 0)
            return PreceptCompatibilityResult.NotCompatible(
                $"Instance violates {ruleViolations.Count} rule(s): {string.Join("; ", ruleViolations)}");

        return PreceptCompatibilityResult.Compatible();
    }

    public PreceptEventInspectionResult Inspect(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var evaluationData = BuildDirectEvaluationData(eventName, eventArguments);

        // Check event asserts first
        var eventAssertViolations = EvaluateEventAsserts(eventName, evaluationData);
        if (eventAssertViolations.Count > 0)
            return PreceptEventInspectionResult.Rejected(currentState, eventName, eventAssertViolations);

        var resolution = ResolveTransition(currentState, eventName, evaluationData);

        return resolution.Kind switch
        {
            TransitionResolutionKind.Accepted => PreceptEventInspectionResult.Accepted(
                currentState,
                eventName,
                ((PreceptStateTransition)resolution.MatchedRow!.Outcome).TargetState,
                GetRequiredEventArgumentKeys(eventName)),
            TransitionResolutionKind.NotApplicable => PreceptEventInspectionResult.NotApplicable(currentState, eventName),
            TransitionResolutionKind.NoTransition => PreceptEventInspectionResult.AcceptedInPlace(currentState, eventName),
            TransitionResolutionKind.NotDefined => PreceptEventInspectionResult.NotDefined(currentState, eventName, resolution.NotDefinedReason!),
            _ => PreceptEventInspectionResult.Rejected(currentState, eventName, resolution.Reasons)
        };
    }

    public PreceptEventInspectionResult Inspect(
        PreceptInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
            return PreceptEventInspectionResult.NotDefined(instance.CurrentState, eventName, compatibility.Reason!);

        // Hydrate clean InstanceData to internal format for engine evaluation
        var internalData = HydrateInstanceData(instance.InstanceData);

        // Fast-path: if all rows for (state, event) have when guards, try evaluating without
        // event args. If all guards fail → NotApplicable (avoids requiring event args for discovery).
        if (_transitionRowMap.TryGetValue((instance.CurrentState, eventName), out var preCheckRows))
        {
            bool allGuardsPresent = preCheckRows.All(r => r.WhenGuard is not null);
            if (allGuardsPresent)
            {
                var instanceOnlyContext = BuildEvaluationData(internalData, eventName, null);
                bool anyGuardPasses = preCheckRows.Any(r =>
                {
                    var whenResult = PreceptExpressionRuntimeEvaluator.Evaluate(r.WhenGuard!, instanceOnlyContext);
                    return whenResult.Success && whenResult.Value is true;
                });
                if (!anyGuardPasses)
                    return PreceptEventInspectionResult.NotApplicable(instance.CurrentState, eventName);
            }
        }

        // Inspect is a discovery API — it intentionally accepts calls with missing/partial event arguments
        // so callers can determine required args via RequiredEventArgumentKeys before firing.
        if (eventArguments != null && !TryValidateEventArguments(eventName, eventArguments, out var eventArgError))
            return PreceptEventInspectionResult.Rejected(instance.CurrentState, eventName, new[] { eventArgError! });

        var evaluationArguments = BuildEvaluationData(internalData, eventName, eventArguments);

        // Check event asserts — only when caller provides event arguments.
        if (eventArguments != null)
        {
            var eventAssertViolations = EvaluateEventAsserts(eventName, BuildDirectEvaluationData(eventName, eventArguments));
            if (eventAssertViolations.Count > 0)
                return PreceptEventInspectionResult.Rejected(instance.CurrentState, eventName, eventAssertViolations);
        }

        var resolution = ResolveTransition(instance.CurrentState, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.NotDefined)
            return PreceptEventInspectionResult.NotDefined(instance.CurrentState, eventName, resolution.NotDefinedReason!);

        if (resolution.Kind == TransitionResolutionKind.NotApplicable)
            return PreceptEventInspectionResult.NotApplicable(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.NoTransition)
            return PreceptEventInspectionResult.AcceptedInPlace(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.MatchedRow is null)
            return PreceptEventInspectionResult.Rejected(instance.CurrentState, eventName, resolution.Reasons);

        var matchedRow = resolution.MatchedRow;
        var targetState = ((PreceptStateTransition)matchedRow.Outcome).TargetState;

        // Simulate full pipeline: exit actions → row mutations → entry actions
        var simulatedData = new Dictionary<string, object?>(internalData, StringComparer.Ordinal);
        var simulatedCollections = CloneCollections(simulatedData);

        // Exit actions
        ExecuteStateActions(PreceptAssertPreposition.From, instance.CurrentState,
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
        ExecuteStateActions(PreceptAssertPreposition.To, targetState,
            simulatedData, simulatedCollections, eventName, eventArguments);

        CommitCollections(simulatedData, simulatedCollections);

        // Validate invariants + state asserts
        var violations = CollectValidationViolations(
            instance.CurrentState, targetState, simulatedData);
        if (violations.Count > 0)
            return PreceptEventInspectionResult.Rejected(instance.CurrentState, eventName, violations);

        return PreceptEventInspectionResult.Accepted(
            instance.CurrentState,
            eventName,
            targetState,
            GetRequiredEventArgumentKeys(eventName));
    }

    /// <summary>
    /// Inspects all events reachable from the instance's current state and returns a summary
    /// including the current state, serialized data, and per-event inspection results.
    /// </summary>
    public PreceptInspectionResult Inspect(PreceptInstance instance)
    {
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
            return new PreceptInspectionResult(
                instance.CurrentState,
                instance.InstanceData,
                Array.Empty<PreceptEventInspectionResult>());

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

        return new PreceptInspectionResult(instance.CurrentState, instance.InstanceData, eventResults, editableFieldInfos);
    }

    /// <summary>
    /// Applies a hypothetical patch to a working copy of instance data, runs the full
    /// validation pipeline, and returns a <see cref="PreceptInspectionResult"/> with
    /// violations reflected in <see cref="PreceptEditableFieldInfo.Violation"/>.
    /// No commit occurs — the instance is unchanged.
    /// </summary>
    public PreceptInspectionResult Inspect(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)
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
                    var violation = new PreceptViolation(conflictError, Array.Empty<PreceptEditableFieldInfo>());
                    violatedInfos.Add(info with { Violation = violation });
                }
                else
                {
                    violatedInfos.Add(info);
                }
            }
            return new PreceptInspectionResult(baseResult.CurrentState, baseResult.InstanceData, baseResult.Events, violatedInfos);
        }

        // Check editability
        var editableNames = _editableFieldsByState.TryGetValue(instance.CurrentState, out var editable)
            ? editable : null;
        foreach (var op in operations)
        {
            if (editableNames is null || !editableNames.Contains(op.FieldName))
            {
                var reason = $"Field '{op.FieldName}' is not editable in state '{instance.CurrentState}'.";
                var violatedInfos = editableFieldInfos.Select(info =>
                    string.Equals(info.FieldName, op.FieldName, StringComparison.Ordinal)
                        ? info with { Violation = new PreceptViolation(reason, Array.Empty<PreceptEditableFieldInfo>()) }
                        : info).ToList();
                return new PreceptInspectionResult(baseResult.CurrentState, baseResult.InstanceData, baseResult.Events, violatedInfos);
            }
        }

        // Simulate: apply patch to working copy and evaluate rules
        var internalData = HydrateInstanceData(instance.InstanceData);
        var updatedData = new Dictionary<string, object?>(internalData, StringComparer.Ordinal);
        var workingCollections = CloneCollections(updatedData);

        ApplyPatchOperations(operations, updatedData, workingCollections);
        CommitCollections(updatedData, workingCollections);

        // Evaluate rules on working copy
        var violations = new List<string>();
        violations.AddRange(EvaluateInvariants(updatedData));
        violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.In, instance.CurrentState, updatedData));

        if (violations.Count == 0)
            return baseResult;

        // Map violations to affected fields
        var patchedFieldNames = new HashSet<string>(
            operations.Select(op => op.FieldName), StringComparer.Ordinal);
        var violationObj = new PreceptViolation(
            string.Join("; ", violations),
            Array.Empty<PreceptEditableFieldInfo>());

        var resultInfos = editableFieldInfos.Select(info =>
            patchedFieldNames.Contains(info.FieldName)
                ? info with { Violation = violationObj }
                : info).ToList();

        // Wire up AffectedFields on the violation
        var affectedFields = resultInfos.Where(f => f.Violation is not null).ToList();
        violationObj = violationObj with { AffectedFields = affectedFields };
        resultInfos = resultInfos.Select(info =>
            info.Violation is not null ? info with { Violation = violationObj } : info).ToList();

        return new PreceptInspectionResult(baseResult.CurrentState, baseResult.InstanceData, baseResult.Events, resultInfos);
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
            PreceptScalarType.Boolean => CoerceToBoolean(value),
            PreceptScalarType.String => value?.ToString(),
            PreceptScalarType.Null => null,
            _ => value
        };
    }

    private static object? UnwrapJsonElement(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString(),
            System.Text.Json.JsonValueKind.Number => element.GetDouble(),
            System.Text.Json.JsonValueKind.True => (object?)true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            System.Text.Json.JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
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

    public PreceptFireResult Fire(
        PreceptInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArguments = null)
    {
        var internalData = HydrateInstanceData(instance.InstanceData);
        var compatibility = CheckCompatibility(instance);
        if (!compatibility.IsCompatible)
        {
            var reasons = new[] { compatibility.Reason! };
            return PreceptFireResult.NotDefined(instance.CurrentState, eventName, reasons);
        }

        if (!TryValidateEventArguments(eventName, eventArguments, out var eventArgError))
            return PreceptFireResult.Rejected(instance.CurrentState, eventName, new[] { eventArgError! });

        var evaluationArguments = BuildEvaluationData(internalData, eventName, eventArguments);

        // Stage 1: Event asserts (args-only context, pre-transition)
        var eventAssertViolations = EvaluateEventAsserts(eventName, BuildDirectEvaluationData(eventName, eventArguments));
        if (eventAssertViolations.Count > 0)
            return PreceptFireResult.Rejected(instance.CurrentState, eventName, eventAssertViolations);

        // Stage 2: First-match row selection
        var resolution = ResolveTransition(instance.CurrentState, eventName, evaluationArguments);
        if (resolution.Kind == TransitionResolutionKind.NotDefined)
            return PreceptFireResult.NotDefined(instance.CurrentState, eventName, new[] { resolution.NotDefinedReason! });

        if (resolution.Kind == TransitionResolutionKind.NotApplicable)
            return PreceptFireResult.NotApplicable(instance.CurrentState, eventName);

        if (resolution.Kind == TransitionResolutionKind.Rejected || resolution.MatchedRow is null)
            return PreceptFireResult.Rejected(instance.CurrentState, eventName, resolution.Reasons);

        var matchedRow = resolution.MatchedRow;
        var updatedData = new Dictionary<string, object?>(internalData, StringComparer.Ordinal);
        var workingCollections = CloneCollections(updatedData);

        // ── No-transition branch ──
        if (matchedRow.Outcome is PreceptNoTransition)
        {
            // Row mutations only (no exit/entry actions for no-transition)
            var mutError = ExecuteRowMutations(matchedRow, updatedData, workingCollections, eventName, eventArguments);
            if (mutError is not null)
                return PreceptFireResult.Rejected(instance.CurrentState, eventName, new[] { mutError });

            CommitCollections(updatedData, workingCollections);

            // Validate: invariants + 'in' asserts for current state
            var violations = new List<string>();
            violations.AddRange(EvaluateInvariants(updatedData));
            violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.In, instance.CurrentState, updatedData));
            if (violations.Count > 0)
                return PreceptFireResult.Rejected(instance.CurrentState, eventName, violations);

            var noTransitionUpdated = instance with
            {
                LastEvent = eventName,
                UpdatedAt = DateTimeOffset.UtcNow,
                InstanceData = DehydrateData(updatedData)
            };
            return PreceptFireResult.AcceptedInPlace(instance.CurrentState, eventName, noTransitionUpdated);
        }

        // ── Transition branch ──
        var targetState = ((PreceptStateTransition)matchedRow.Outcome).TargetState;

        // Stage 3: Exit actions (from <sourceState> -> ...)
        ExecuteStateActions(PreceptAssertPreposition.From, instance.CurrentState,
            updatedData, workingCollections, eventName, eventArguments);

        // Stage 4: Row mutations (set assignments + collection mutations)
        var rowMutError = ExecuteRowMutations(matchedRow, updatedData, workingCollections, eventName, eventArguments);
        if (rowMutError is not null)
            return PreceptFireResult.Rejected(instance.CurrentState, eventName, new[] { rowMutError });

        // Stage 5: Entry actions (to <targetState> -> ...)
        ExecuteStateActions(PreceptAssertPreposition.To, targetState,
            updatedData, workingCollections, eventName, eventArguments);

        CommitCollections(updatedData, workingCollections);

        // Stage 6: Validation (invariants + state asserts, collect-all)
        var validationViolations = CollectValidationViolations(
            instance.CurrentState, targetState, updatedData);
        if (validationViolations.Count > 0)
            return PreceptFireResult.Rejected(instance.CurrentState, eventName, validationViolations);

        var updated = instance with
        {
            CurrentState = targetState,
            LastEvent = eventName,
            UpdatedAt = DateTimeOffset.UtcNow,
            InstanceData = DehydrateData(updatedData)
        };

        return PreceptFireResult.Accepted(instance.CurrentState, eventName, targetState, updated);
    }

    /// <summary>
    /// Atomically updates editable fields on an instance. Only fields declared in an
    /// <c>in &lt;State&gt; edit</c> block for the current state are mutable.
    /// After applying edits, evaluates all invariants and state asserts.
    /// </summary>
    public PreceptUpdateResult Update(PreceptInstance instance, Action<IUpdatePatchBuilder> patch)
    {
        var builder = new UpdatePatchBuilder();
        patch(builder);

        // Build-time conflict detection
        var conflictError = builder.ValidateConflicts(_fieldMap, _collectionFieldMap);
        if (conflictError is not null)
            return PreceptUpdateResult.Failed(PreceptUpdateOutcome.Invalid, new[] { conflictError });

        var operations = builder.GetOperations();
        if (operations.Count == 0)
            return PreceptUpdateResult.Failed(PreceptUpdateOutcome.Invalid, new[] { "Patch is empty." });

        // Stage 1: Editability check — all fields in patch must be editable in current state
        var editableFields = _editableFieldsByState.TryGetValue(instance.CurrentState, out var editable)
            ? editable : null;
        var notAllowed = new List<string>();
        foreach (var op in operations)
        {
            if (editableFields is null || !editableFields.Contains(op.FieldName))
                notAllowed.Add($"Field '{op.FieldName}' is not editable in state '{instance.CurrentState}'.");
        }
        if (notAllowed.Count > 0)
            return PreceptUpdateResult.Failed(PreceptUpdateOutcome.NotAllowed, notAllowed);

        // Stage 2: Type check + unknown field validation
        foreach (var op in operations)
        {
            var typeError = ValidateUpdateOperation(op);
            if (typeError is not null)
                return PreceptUpdateResult.Failed(PreceptUpdateOutcome.Invalid, new[] { typeError });
        }

        // Stage 3: Atomic mutation on working copy
        var internalData = HydrateInstanceData(instance.InstanceData);
        var updatedData = new Dictionary<string, object?>(internalData, StringComparer.Ordinal);
        var workingCollections = CloneCollections(updatedData);

        var mutError = ApplyPatchOperations(operations, updatedData, workingCollections);
        if (mutError is not null)
            return PreceptUpdateResult.Failed(PreceptUpdateOutcome.Invalid, new[] { mutError });

        CommitCollections(updatedData, workingCollections);

        // Stage 4: Rules evaluation (invariants + 'in' state asserts)
        var violations = new List<string>();
        violations.AddRange(EvaluateInvariants(updatedData));
        violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.In, instance.CurrentState, updatedData));
        if (violations.Count > 0)
            return PreceptUpdateResult.Failed(PreceptUpdateOutcome.Blocked, violations);

        // Stage 5: Commit
        var updated = instance with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            InstanceData = DehydrateData(updatedData)
        };

        return PreceptUpdateResult.Updated(updated);
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

    /// <summary>
    /// Returns the set of editable field names for the given state, or empty if none.
    /// </summary>
    internal IReadOnlySet<string> GetEditableFieldNames(string state)
    {
        if (_editableFieldsByState.TryGetValue(state, out var fields))
            return fields;
        return EmptyStringSet.Instance;
    }

    /// <summary>
    /// Builds <see cref="PreceptEditableFieldInfo"/> entries for all editable fields
    /// in the given state, populated with current values from instance data.
    /// Returns null when no edit declarations exist for this engine.
    /// </summary>
    private IReadOnlyList<PreceptEditableFieldInfo>? BuildEditableFieldInfos(
        string state, IReadOnlyDictionary<string, object?> instanceData)
    {
        if (_editableFieldsByState.Count == 0)
            return null;

        if (!_editableFieldsByState.TryGetValue(state, out var editableNames) || editableNames.Count == 0)
            return Array.Empty<PreceptEditableFieldInfo>();

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
    internal IReadOnlyList<string> EvaluateCurrentRules(PreceptInstance instance)
    {
        var violations = new List<string>();
        violations.AddRange(EvaluateInvariants(instance.InstanceData));
        violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.In, instance.CurrentState, instance.InstanceData));
        return violations;
    }

    private TransitionResolution ResolveTransition(
        string currentState,
        string eventName,
        IReadOnlyDictionary<string, object?> evaluationData)
    {
        // SYNC:CONSTRAINT:C36
        if (string.IsNullOrWhiteSpace(currentState))
            throw new ArgumentException(ConstraintCatalog.C36.FormatMessage(), nameof(currentState));
        // SYNC:CONSTRAINT:C37
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException(ConstraintCatalog.C37.FormatMessage(), nameof(eventName));

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
                var guardResult = PreceptExpressionRuntimeEvaluator.Evaluate(row.WhenGuard, evaluationData);
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
                PreceptStateTransition => TransitionResolution.Accepted(row),
                PreceptNoTransition => TransitionResolution.NoTransition(row),
                PreceptRejection rej => TransitionResolution.Rejected(new[]
                {
                    string.IsNullOrWhiteSpace(rej.Reason)
                        ? (reasons.FirstOrDefault() ?? $"No transition for '{eventName}' from '{currentState}'.")
                        : rej.Reason!
                }),
                _ => TransitionResolution.Rejected(reasons)
            };
        }

        // No row matched — if all had guards, NotApplicable; otherwise Rejected.
        if (rows.All(r => r.WhenGuard is not null))
            return TransitionResolution.NotApplicable();
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
        PreceptTransitionRow? MatchedRow,
        string? NotDefinedReason,
        IReadOnlyList<string> Reasons)
    {
        internal static TransitionResolution Accepted(PreceptTransitionRow row) =>
            new(TransitionResolutionKind.Accepted, row, null, Array.Empty<string>());

        internal static TransitionResolution Rejected(IReadOnlyList<string> reasons) =>
            new(TransitionResolutionKind.Rejected, null, null, reasons);

        internal static TransitionResolution NotDefined(string reason) =>
            new(TransitionResolutionKind.NotDefined, null, reason, new[] { reason });

        internal static TransitionResolution NoTransition(PreceptTransitionRow row) =>
            new(TransitionResolutionKind.NoTransition, row, null, Array.Empty<string>());

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

    // ---- Constraint evaluation helpers ----

    private IReadOnlyList<string> EvaluateEventAsserts(string eventName, IReadOnlyDictionary<string, object?> evaluationData)
    {
        if (!_eventAssertMap.TryGetValue(eventName, out var asserts) || asserts.Count == 0)
            return Array.Empty<string>();

        var violations = new List<string>();
        foreach (var assert in asserts)
        {
            var result = PreceptExpressionRuntimeEvaluator.Evaluate(assert.Expression, evaluationData);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
                violations.Add(assert.Reason);
        }
        return violations;
    }

    private IReadOnlyList<string> EvaluateInvariants(IReadOnlyDictionary<string, object?> data)
    {
        if (_invariants.Count == 0)
            return Array.Empty<string>();

        var violations = new List<string>();
        foreach (var inv in _invariants)
        {
            var result = PreceptExpressionRuntimeEvaluator.Evaluate(inv.Expression, data);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
                violations.Add(inv.Reason);
        }
        return violations;
    }

    /// <summary>
    /// Evaluates state asserts for a given preposition and state.
    /// </summary>
    private IReadOnlyList<string> EvaluateStateAsserts(
        PreceptAssertPreposition preposition, string state, IReadOnlyDictionary<string, object?> data)
    {
        if (!_stateAssertMap.TryGetValue((preposition, state), out var asserts) || asserts.Count == 0)
            return Array.Empty<string>();

        var violations = new List<string>();
        foreach (var assert in asserts)
        {
            var result = PreceptExpressionRuntimeEvaluator.Evaluate(assert.Expression, data);
            if (!result.Success || result.Value is not bool boolVal || !boolVal)
                violations.Add(assert.Reason);
        }
        return violations;
    }

    /// <summary>
    /// Collects all validation violations post-mutation: invariants + preposition-aware state asserts.
    /// </summary>
    private IReadOnlyList<string> CollectValidationViolations(
        string sourceState, string targetState, IReadOnlyDictionary<string, object?> data)
    {
        var violations = new List<string>();

        // Invariants (always)
        violations.AddRange(EvaluateInvariants(data));

        if (string.Equals(sourceState, targetState, StringComparison.Ordinal))
        {
            // AcceptedInPlace / self-transition: evaluate 'to' + 'in' asserts
            violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.To, targetState, data));
            violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.In, sourceState, data));
        }
        else
        {
            // State transition: evaluate 'from' source + 'to' target + 'in' target
            violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.From, sourceState, data));
            violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.To, targetState, data));
            violations.AddRange(EvaluateStateAsserts(PreceptAssertPreposition.In, targetState, data));
        }

        return violations;
    }

    /// <summary>
    /// Executes state entry/exit actions (automatic mutations).
    /// </summary>
    private void ExecuteStateActions(
        PreceptAssertPreposition preposition, string state,
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

    private static bool TryValidateScalarValue(string name, PreceptScalarType type, bool isNullable, object? value, out string? error)
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

public static class PreceptCompiler
{
    public static PreceptEngine Compile(PreceptDefinition model)
    {
        // SYNC:CONSTRAINT:C26
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        // SYNC:CONSTRAINT:C27
        if (string.IsNullOrWhiteSpace(model.InitialState.Name))
            throw new InvalidOperationException(ConstraintCatalog.C27.FormatMessage());

        // SYNC:CONSTRAINT:C28
        if (!model.States.Contains(model.InitialState))
            throw new InvalidOperationException(ConstraintCatalog.C28.FormatMessage(("stateName", model.InitialState.Name), ("workflowName", model.Name)));

        // Compile-time constraint validations
        ValidateConstraintsAtCompileTime(model);

        return new PreceptEngine(model);
    }

    private static void ValidateConstraintsAtCompileTime(PreceptDefinition model)
    {
        var defaultData = BuildDefaultData(model);

        // 1. Validate invariants against default values
        if (model.Invariants is { Count: > 0 })
        {
            foreach (var inv in model.Invariants)
            {
                var result = PreceptExpressionRuntimeEvaluator.Evaluate(inv.Expression, defaultData);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                    // SYNC:CONSTRAINT:C29
                    throw new InvalidOperationException(
                        ConstraintCatalog.C29.FormatMessage(("reason", inv.Reason)));
            }
        }

        // 2. Validate initial state asserts (in + to) against default data
        if (model.StateAsserts is { Count: > 0 })
        {
            var initialStateName = model.InitialState.Name;
            foreach (var sa in model.StateAsserts)
            {
                if (!string.Equals(sa.State, initialStateName, StringComparison.Ordinal))
                    continue;
                if (sa.Preposition is not (PreceptAssertPreposition.In or PreceptAssertPreposition.To))
                    continue;

                var result = PreceptExpressionRuntimeEvaluator.Evaluate(sa.Expression, defaultData);
                if (!result.Success || result.Value is not bool boolVal || !boolVal)
                    // SYNC:CONSTRAINT:C30
                    throw new InvalidOperationException(
                        ConstraintCatalog.C30.FormatMessage(("reason", sa.Reason), ("stateName", initialStateName)));
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
                    // SYNC:CONSTRAINT:C31
                    throw new InvalidOperationException(
                        ConstraintCatalog.C31.FormatMessage(("reason", ea.Reason), ("eventName", ea.EventName)));
            }
        }

        // 4. Validate literal set assignments against invariants
        ValidateLiteralSetAssignments(model, defaultData);
    }

    private static void ValidateLiteralSetAssignments(PreceptDefinition model, IReadOnlyDictionary<string, object?> defaultData)
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
                    // SYNC:CONSTRAINT:C32
                    throw new InvalidOperationException(
                        ConstraintCatalog.C32.FormatMessage(("sourceLine", assignment.SourceLine), ("key", assignment.Key), ("expression", assignment.ExpressionText), ("reason", inv.Reason)));
            }
        }

        // Check assignments in transition rows
        if (model.TransitionRows is { Count: > 0 })
        {
            foreach (var row in model.TransitionRows)
                foreach (var assignment in row.SetAssignments)
                    CheckLiteralAssignment(assignment);
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
    string CurrentState,
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

public sealed record PreceptEventInspectionResult(
    PreceptOutcomeKind Outcome,
    string CurrentState,
    string EventName,
    string? TargetState,
    IReadOnlyList<string> RequiredEventArgumentKeys,
    IReadOnlyList<string> Reasons)
{
    internal static PreceptEventInspectionResult Accepted(string state, string evt, string target, IReadOnlyList<string> requiredEventArgumentKeys) =>
        new(PreceptOutcomeKind.Accepted, state, evt, target, requiredEventArgumentKeys, Array.Empty<string>());

    internal static PreceptEventInspectionResult AcceptedInPlace(string state, string evt) =>
        new(PreceptOutcomeKind.AcceptedInPlace, state, evt, state, Array.Empty<string>(), Array.Empty<string>());

    internal static PreceptEventInspectionResult NotDefined(string state, string evt, string reason) =>
        new(PreceptOutcomeKind.NotDefined, state, evt, null, Array.Empty<string>(), new[] { reason });

    internal static PreceptEventInspectionResult NotApplicable(string state, string evt) =>
        new(PreceptOutcomeKind.NotApplicable, state, evt, null, Array.Empty<string>(), Array.Empty<string>());

    internal static PreceptEventInspectionResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(PreceptOutcomeKind.Rejected, state, evt, null, Array.Empty<string>(), reasons);
}

public sealed record PreceptInspectionResult(
    string CurrentState,
    IReadOnlyDictionary<string, object?> InstanceData,
    IReadOnlyList<PreceptEventInspectionResult> Events,
    IReadOnlyList<PreceptEditableFieldInfo>? EditableFields = null);

public sealed record PreceptEditableFieldInfo(
    string FieldName,
    string FieldType,
    bool IsNullable,
    object? CurrentValue,
    PreceptViolation? Violation = null);

public sealed record PreceptViolation(
    string Reason,
    IReadOnlyList<PreceptEditableFieldInfo> AffectedFields);

public enum PreceptUpdateOutcome
{
    Updated,
    NotAllowed,
    Blocked,
    Invalid
}

public sealed record PreceptUpdateResult(
    PreceptUpdateOutcome Outcome,
    IReadOnlyList<string> Reasons,
    PreceptInstance? UpdatedInstance)
{
    internal static PreceptUpdateResult Updated(PreceptInstance updated) =>
        new(PreceptUpdateOutcome.Updated, Array.Empty<string>(), updated);

    internal static PreceptUpdateResult Failed(PreceptUpdateOutcome outcome, IReadOnlyList<string> reasons) =>
        new(outcome, reasons, null);
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

public sealed record PreceptFireResult(
    PreceptOutcomeKind Outcome,
    string PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<string> Reasons,
    PreceptInstance? UpdatedInstance)
{
    internal static PreceptFireResult Accepted(string state, string evt, string newState, PreceptInstance updated) =>
        new(PreceptOutcomeKind.Accepted, state, evt, newState, Array.Empty<string>(), updated);

    internal static PreceptFireResult AcceptedInPlace(string state, string evt, PreceptInstance updated) =>
        new(PreceptOutcomeKind.AcceptedInPlace, state, evt, state, Array.Empty<string>(), updated);

    internal static PreceptFireResult NotDefined(string state, string evt, IReadOnlyList<string> reasons) =>
        new(PreceptOutcomeKind.NotDefined, state, evt, null, reasons, null);

    internal static PreceptFireResult NotApplicable(string state, string evt) =>
        new(PreceptOutcomeKind.NotApplicable, state, evt, null, Array.Empty<string>(), null);

    internal static PreceptFireResult Rejected(string state, string evt, IReadOnlyList<string> reasons) =>
        new(PreceptOutcomeKind.Rejected, state, evt, null, reasons, null);
}

public enum PreceptOutcomeKind
{
    NotDefined,
    NotApplicable,
    Rejected,
    Accepted,
    AcceptedInPlace
}
