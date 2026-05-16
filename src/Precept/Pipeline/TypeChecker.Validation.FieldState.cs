using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    private static void BuildOmitLookup(ConstructManifest manifest, CheckContext ctx)
    {
        if (!manifest.ByKind.Contains(ConstructKind.OmitDeclaration))
            return;

        foreach (var construct in manifest.ByKind[ConstructKind.OmitDeclaration])
        {
            var stateSlot = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
            var resolvedStates = ResolveStateTargets(stateSlot, ctx);
            if (resolvedStates.IsDefaultOrEmpty)
                continue;

            var fieldSlot = construct.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget);
            if (fieldSlot?.FieldName is not { } fieldName)
                continue;

            var fieldNames = ImmutableArray.CreateBuilder<string>(1 + fieldSlot.AdditionalFields.Length);
            if (HasKeywordTokenMeta(fieldName, meta => meta.IsFieldBroadcast))
            {
                foreach (var field in ctx.Fields)
                    fieldNames.Add(field.Name);
            }
            else
            {
                AddDeclaredField(fieldName);
                foreach (var (additionalFieldName, _) in fieldSlot.AdditionalFields)
                    AddDeclaredField(additionalFieldName);
            }

            if (fieldNames.Count == 0)
                continue;

            if (resolvedStates.Any(state => state.IsWildcard))
            {
                foreach (var state in ctx.States)
                {
                    foreach (var resolvedFieldName in fieldNames)
                        ctx.OmitLookup.Add((state.Name, resolvedFieldName));
                }

                continue;
            }

            foreach (var resolvedState in resolvedStates)
            {
                foreach (var resolvedFieldName in fieldNames)
                    ctx.OmitLookup.Add((resolvedState.StateName, resolvedFieldName));
            }

            void AddDeclaredField(string candidateFieldName)
            {
                if (ctx.FieldLookup.TryGetValue(candidateFieldName, out var typedField))
                    fieldNames.Add(typedField.Name);
            }
        }
    }

    private static void CollectFieldRefsFromExpression(
        TypedExpression? expr, List<TypedFieldRef> refs)
    {
        if (expr is null)
            return;

        switch (expr)
        {
            case TypedBinaryOp bin:
                CollectFieldRefsFromExpression(bin.Left, refs);
                CollectFieldRefsFromExpression(bin.Right, refs);
                break;

            case TypedFunctionCall func:
                foreach (var arg in func.Arguments)
                    CollectFieldRefsFromExpression(arg, refs);
                break;

            case TypedUnaryOp un:
                CollectFieldRefsFromExpression(un.Operand, refs);
                break;

            case TypedConditional cond:
                CollectFieldRefsFromExpression(cond.Condition, refs);
                CollectFieldRefsFromExpression(cond.ThenBranch, refs);
                CollectFieldRefsFromExpression(cond.ElseBranch, refs);
                break;

            case TypedQuantifier quant:
                CollectFieldRefsFromExpression(quant.Collection, refs);
                CollectFieldRefsFromExpression(quant.Predicate, refs);
                break;

            case TypedMemberAccess mem:
                CollectFieldRefsFromExpression(mem.Object, refs);
                break;

            case TypedInterpolatedString interp:
                foreach (var seg in interp.Segments)
                {
                    if (seg is TypedHoleSegment hole)
                        CollectFieldRefsFromExpression(hole.Expression, refs);
                }
                break;

            case TypedListLiteral list:
                foreach (var elem in list.Elements)
                    CollectFieldRefsFromExpression(elem, refs);
                break;

            case TypedFieldRef fieldRef:
                refs.Add(fieldRef);
                break;

            case TypedPostfixOp:
            case TypedArgRef:
            case TypedLiteral:
            case TypedErrorExpression:
                break;
        }
    }

    private static void EmitD130ForWildcard(
        string fieldName, SourceSpan span, CheckContext ctx)
    {
        var omittedStates = ctx.States
            .Where(state => ctx.OmitLookup.Contains((state.Name, fieldName)))
            .Select(state => state.Name)
            .ToImmutableArray();

        if (omittedStates.IsDefaultOrEmpty)
            return;

        ctx.Diagnostics.Add(
            Diagnostics.Create(
                DiagnosticCode.OmittedFieldReadInState,
                span,
                fieldName,
                string.Join(", ", omittedStates)));
    }

    private static void ValidateFieldStateGuarantees(CheckContext ctx)
    {
        var fieldRefs = new List<TypedFieldRef>();
        var computedDependencyLookup = BuildComputedDependencyLookup(ctx);

        void EmitDiagnosticsForFieldRefs(string? stateName)
        {
            foreach (var fieldRef in fieldRefs)
            {
                if (stateName is not null)
                {
                    if (ctx.OmitLookup.Contains((stateName, fieldRef.FieldName)))
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(
                                DiagnosticCode.OmittedFieldReadInState,
                                fieldRef.Span,
                                fieldRef.FieldName,
                                stateName));
                    }
                }
                else
                {
                    EmitD130ForWildcard(fieldRef.FieldName, fieldRef.Span, ctx);
                }
            }

            fieldRefs.Clear();
        }

        foreach (var row in ctx.TransitionRows)
        {
            fieldRefs.Clear();
            CollectFieldRefsFromExpression(row.Guard, fieldRefs);
            EmitDiagnosticsForFieldRefs(row.FromState);

            if (row is TypedTransitionRowSuccess successRow)
            {
                foreach (var action in successRow.Actions)
                {
                    if (action is not TypedInputAction inputAction)
                        continue;

                    fieldRefs.Clear();
                    CollectFieldRefsFromExpression(inputAction.InputExpression, fieldRefs);
                    CollectFieldRefsFromExpression(inputAction.SecondaryExpression, fieldRefs);
                    EmitDiagnosticsForFieldRefs(row.FromState);
                }
            }
        }

        foreach (var ensure in ctx.Ensures)
        {
            if (ensure.Kind is not (ConstraintKind.StateResident or ConstraintKind.StateExit) ||
                ensure.AnchorState is null)
            {
                continue;
            }

            fieldRefs.Clear();
            CollectFieldRefsFromExpression(ensure.Condition, fieldRefs);
            EmitDiagnosticsForFieldRefs(ensure.AnchorState);
        }

        foreach (var hook in ctx.StateHooks)
        {
            fieldRefs.Clear();
            CollectFieldRefsFromExpression(hook.Guard, fieldRefs);
            EmitDiagnosticsForFieldRefs(hook.StateName);

            foreach (var action in hook.Actions)
            {
                if (action is not TypedInputAction inputAction)
                    continue;

                fieldRefs.Clear();
                CollectFieldRefsFromExpression(inputAction.InputExpression, fieldRefs);
                CollectFieldRefsFromExpression(inputAction.SecondaryExpression, fieldRefs);
                EmitDiagnosticsForFieldRefs(hook.StateName);
            }
        }

        // D131: write actions cannot target fields omitted in the target or entered state.
        foreach (var row in ctx.TransitionRows.OfType<TypedTransitionRowSuccess>())
        {
            if (row.TargetState is null)
                continue;

            foreach (var action in row.Actions)
            {
                if (ctx.OmitLookup.Contains((row.TargetState, action.FieldName)))
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(
                            DiagnosticCode.OmittedFieldSetInTargetState,
                            action.Span,
                            action.FieldName,
                            row.TargetState));
                }
            }
        }

        foreach (var hook in ctx.StateHooks)
        {
            if (hook.Scope != AnchorScope.OnEntry)
                continue;

            foreach (var action in hook.Actions)
            {
                if (ctx.OmitLookup.Contains((hook.StateName, action.FieldName)))
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(
                            DiagnosticCode.OmittedFieldSetInTargetState,
                            action.Span,
                            action.FieldName,
                            hook.StateName));
                }
            }
        }

        // D132/D143: transitions that materialize a required field on entry must assign it,
        // and the first materializing assignment cannot read that field before it exists.
        foreach (var row in ctx.TransitionRows.OfType<TypedTransitionRowSuccess>())
        {
            if (row.TargetState is null)
                continue;

            var effectiveFromStates = row.FromState is not null
                ? [row.FromState]
                : ctx.States.Select(state => state.Name);

            foreach (var fromState in effectiveFromStates)
            {
                foreach (var field in ctx.Fields)
                {
                    if (!IsRequiredFieldWithoutImplicitValue(field))
                        continue;

                    var omitInFrom = ctx.OmitLookup.Contains((fromState, field.Name));
                    var omitInTarget = ctx.OmitLookup.Contains((row.TargetState, field.Name));
                    if (!omitInFrom || omitInTarget)
                        continue;

                    var hasSet = false;
                    var materialized = false;

                    foreach (var action in row.Actions)
                    {
                        if (!IsSetAction(action.Kind) ||
                            !string.Equals(action.FieldName, field.Name, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        hasSet = true;
                        if (materialized)
                            continue;

                        if (action is TypedInputAction inputAction)
                        {
                            fieldRefs.Clear();
                            CollectFieldRefsFromExpression(inputAction.InputExpression, fieldRefs);
                            CollectFieldRefsFromExpression(inputAction.SecondaryExpression, fieldRefs);

                            foreach (var fieldRef in fieldRefs)
                            {
                                if (!FieldDependsTransitivelyOn(fieldRef.FieldName, field.Name, computedDependencyLookup))
                                    continue;

                                ctx.Diagnostics.Add(
                                    Diagnostics.Create(
                                        DiagnosticCode.MaterializedFieldSelfReference,
                                        fieldRef.Span,
                                        field.Name,
                                        fromState,
                                        row.TargetState));
                            }
                        }

                        materialized = true;
                    }

                    if (!hasSet)
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(
                                DiagnosticCode.RequiredFieldUnassignedOnEntry,
                                row.RowSpan,
                                field.Name,
                                fromState,
                                row.TargetState));
                    }
                }
            }
        }
    }

    private static void ValidateConstructionGuarantees(CheckContext ctx)
    {
        var requiredFields = ctx.Fields
            .Where(field => NeedsInitialEvent(field, ctx))
            .ToImmutableArray();

        if (requiredFields.IsDefaultOrEmpty)
            return;

        var initialEvent = ctx.Events.FirstOrDefault(evt => evt.IsInitial);
        if (initialEvent is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.RequiredFieldsNeedInitialEvent,
                    requiredFields[0].NameSpan,
                    string.Join(", ", requiredFields.Select(field => field.Name))));
            return;
        }

        var initialActionChains = GetInitialConstructionActionChains(ctx, initialEvent);
        if (initialActionChains.IsDefaultOrEmpty)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.InitialEventMissingAssignments,
                    initialEvent.NameSpan,
                    initialEvent.Name,
                    string.Join(", ", requiredFields.Select(field => field.Name))));
            return;
        }

        foreach (var (span, actions, coveredStateNames) in initialActionChains)
        {
            var invalidFields = GetInvalidRequiredFieldAssignments(requiredFields, actions, coveredStateNames, ctx);
            if (invalidFields.IsDefaultOrEmpty)
                continue;

            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.InitialEventMissingAssignments,
                    span,
                    initialEvent.Name,
                    string.Join(", ", invalidFields)));
        }
    }

    private static void ValidateConstructionGuardFieldAccess(CheckContext ctx)
    {
        var fieldRefs = new List<TypedFieldRef>();

        foreach (var row in ctx.EventHandlers.Where(handler => handler.IsConstruction))
        {
            if (row.Guard is null)
                continue;

            fieldRefs.Clear();
            CollectFieldRefsFromExpression(row.Guard, fieldRefs);

            foreach (var fieldRef in fieldRefs)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(
                        DiagnosticCode.ConstructionGuardReadsUninitializedField,
                        fieldRef.Span,
                        fieldRef.FieldName,
                        row.EventName));
            }
        }
    }

    private static void ValidateInitialAssignmentSelfReads(CheckContext ctx)
    {
        var initialEvent = ctx.Events.FirstOrDefault(evt => evt.IsInitial);
        if (initialEvent is null)
            return;

        foreach (var (_, actions, _) in GetInitialConstructionActionChains(ctx, initialEvent))
            ValidateInitialAssignmentSelfReads(actions, initialEvent.Name, ctx);
    }

    private static void ValidateInitialAssignmentSelfReads(
        ImmutableArray<TypedAction> actions,
        string initialEventName,
        CheckContext ctx)
    {
        var priorAssignments = new HashSet<string>(StringComparer.Ordinal);
        var firstAssignmentIndices = GetFirstAssignmentIndices(actions);
        var fieldRefs = new List<TypedFieldRef>();

        for (var actionIndex = 0; actionIndex < actions.Length; actionIndex++)
        {
            var action = actions[actionIndex];
            if (!IsSetAction(action.Kind) || action is not TypedInputAction inputAction)
                continue;

            if (!ctx.FieldLookup.TryGetValue(action.FieldName, out var field))
                continue;

            var fieldName = field.Name;
            var isFirstRequiredAssignment = !priorAssignments.Contains(fieldName) && IsRequiredFieldWithoutImplicitValue(field);

            fieldRefs.Clear();
            CollectFieldRefsFromExpression(inputAction.InputExpression, fieldRefs);
            CollectFieldRefsFromExpression(inputAction.SecondaryExpression, fieldRefs);

            foreach (var fieldRef in fieldRefs)
            {
                if (isFirstRequiredAssignment &&
                    string.Equals(fieldRef.FieldName, fieldName, StringComparison.Ordinal))
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(
                            DiagnosticCode.UninitializedFieldReadInInitialAssignment,
                            fieldRef.Span,
                            fieldName,
                            initialEventName));
                    continue;
                }

                if (string.Equals(fieldRef.FieldName, fieldName, StringComparison.Ordinal) ||
                    !IsReadBeforeFirstAssignment(fieldRef.FieldName, actionIndex, priorAssignments, firstAssignmentIndices, ctx))
                {
                    continue;
                }

                ctx.Diagnostics.Add(
                    Diagnostics.Create(
                        DiagnosticCode.UninitializedCrossFieldReadInInitialAssignment,
                        fieldRef.Span,
                        fieldRef.FieldName,
                        initialEventName));
            }

            priorAssignments.Add(fieldName);
        }
    }

    private static ImmutableArray<(SourceSpan Span, ImmutableArray<TypedAction> Actions, ImmutableArray<string> CoveredStateNames)> GetInitialConstructionActionChains(
        CheckContext ctx,
        TypedEvent initialEvent)
    {
        var builder = ImmutableArray.CreateBuilder<(SourceSpan Span, ImmutableArray<TypedAction> Actions, ImmutableArray<string> CoveredStateNames)>();

        if (ctx.States.Count == 0)
        {
            foreach (var handler in ctx.EventHandlers.OfType<TypedEventRowSuccess>().Where(handler =>
                         string.Equals(handler.EventName, initialEvent.Name, StringComparison.Ordinal)))
            {
                builder.Add((handler.Syntax.Span, handler.Actions, ImmutableArray<string>.Empty));
            }

            foreach (var row in ctx.TransitionRows.OfType<TypedTransitionRowSuccess>().Where(row =>
                         string.Equals(row.EventName, initialEvent.Name, StringComparison.Ordinal) &&
                         row.FromState is null))
            {
                builder.Add((row.RowSpan, row.Actions, ImmutableArray<string>.Empty));
            }

            return builder.ToImmutable();
        }

        var constructionHandlers = ctx.EventHandlers
            .OfType<TypedEventRowSuccess>()
            .Where(handler =>
                handler.IsConstruction &&
                string.Equals(handler.EventName, initialEvent.Name, StringComparison.Ordinal))
            .ToImmutableArray();

        if (!constructionHandlers.IsDefaultOrEmpty)
        {
            var coveredStateNames = ctx.States
                .Where(state => state.Modifiers.Contains(ModifierKind.InitialState))
                .Select(state => state.Name)
                .ToImmutableArray();

            foreach (var handler in constructionHandlers)
                builder.Add((handler.Syntax.Span, handler.Actions, coveredStateNames));

            return builder.ToImmutable();
        }

        var initialStateNames = ctx.States
            .Where(state => state.Modifiers.Contains(ModifierKind.InitialState))
            .Select(state => state.Name)
            .ToImmutableArray();
        var initialStateLookup = initialStateNames.ToHashSet(StringComparer.Ordinal);

        foreach (var row in ctx.TransitionRows.OfType<TypedTransitionRowSuccess>().Where(row =>
                     string.Equals(row.EventName, initialEvent.Name, StringComparison.Ordinal) &&
                     (row.FromState is null || initialStateLookup.Contains(row.FromState))))
        {
            var coveredStateNames = row.FromState is null
                ? initialStateNames
                : ImmutableArray.Create(row.FromState!);
            builder.Add((row.RowSpan, row.Actions, coveredStateNames));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> GetInvalidRequiredFieldAssignments(
        ImmutableArray<TypedField> requiredFields,
        ImmutableArray<TypedAction> actions,
        ImmutableArray<string> coveredStateNames,
        CheckContext ctx) =>
        requiredFields
            .Where(field => IsRequiredOnConstructionPath(field, coveredStateNames, ctx))
            .Where(field => actions.Count(action =>
                IsSetAction(action.Kind) &&
                string.Equals(action.FieldName, field.Name, StringComparison.Ordinal)) != 1)
            .Select(field => field.Name)
            .ToImmutableArray();

    private static bool IsRequiredOnConstructionPath(
        TypedField field,
        ImmutableArray<string> coveredStateNames,
        CheckContext ctx)
    {
        if (coveredStateNames.IsDefaultOrEmpty)
            return true;

        return coveredStateNames.Any(stateName => !ctx.OmitLookup.Contains((stateName, field.Name)));
    }

    private static Dictionary<string, ImmutableArray<string>> BuildComputedDependencyLookup(CheckContext ctx)
    {
        var builder = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var dep in ctx.ComputedDeps)
        {
            if (!builder.TryGetValue(dep.FieldName, out var dependsOn))
            {
                dependsOn = new HashSet<string>(StringComparer.Ordinal);
                builder[dep.FieldName] = dependsOn;
            }

            foreach (var dependency in dep.DependsOn)
                dependsOn.Add(dependency);
        }

        return builder.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToImmutableArray(),
            StringComparer.Ordinal);
    }

    private static bool FieldDependsTransitivelyOn(
        string referencedFieldName,
        string targetFieldName,
        IReadOnlyDictionary<string, ImmutableArray<string>> computedDependencyLookup) =>
        FieldDependsTransitivelyOn(
            referencedFieldName,
            targetFieldName,
            computedDependencyLookup,
            new HashSet<string>(StringComparer.Ordinal));

    private static bool FieldDependsTransitivelyOn(
        string referencedFieldName,
        string targetFieldName,
        IReadOnlyDictionary<string, ImmutableArray<string>> computedDependencyLookup,
        HashSet<string> visited)
    {
        if (!visited.Add(referencedFieldName))
            return false;

        if (string.Equals(referencedFieldName, targetFieldName, StringComparison.Ordinal))
            return true;

        if (!computedDependencyLookup.TryGetValue(referencedFieldName, out var dependencies))
            return false;

        foreach (var dependency in dependencies)
        {
            if (FieldDependsTransitivelyOn(dependency, targetFieldName, computedDependencyLookup, visited))
                return true;
        }

        return false;
    }

    private static Dictionary<string, int> GetFirstAssignmentIndices(ImmutableArray<TypedAction> actions)
    {
        var firstAssignmentIndices = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var actionIndex = 0; actionIndex < actions.Length; actionIndex++)
        {
            var action = actions[actionIndex];
            if (!IsSetAction(action.Kind) || firstAssignmentIndices.ContainsKey(action.FieldName))
                continue;

            firstAssignmentIndices[action.FieldName] = actionIndex;
        }

        return firstAssignmentIndices;
    }

    private static bool IsReadBeforeFirstAssignment(
        string fieldName,
        int actionIndex,
        HashSet<string> priorAssignments,
        IReadOnlyDictionary<string, int> firstAssignmentIndices,
        CheckContext ctx)
    {
        if (priorAssignments.Contains(fieldName) ||
            !firstAssignmentIndices.TryGetValue(fieldName, out var firstAssignmentIndex) ||
            firstAssignmentIndex <= actionIndex ||
            !ctx.FieldLookup.TryGetValue(fieldName, out var field))
        {
            return false;
        }

        return IsRequiredFieldWithoutImplicitValue(field);
    }

    private static bool IsSetAction(ActionKind kind) => kind == ActionKind.Set;

    private static bool IsRequiredFieldWithoutImplicitValue(TypedField field) =>
        !field.IsOptional &&
        field.DefaultExpression is null &&
        !field.IsComputed &&
        !IsCollectionField(field);

    private static bool NeedsInitialEvent(TypedField field, CheckContext ctx)
    {
        if (!IsRequiredFieldWithoutImplicitValue(field))
            return false;

        var initialStates = ctx.States
            .Where(state => state.Modifiers.Contains(ModifierKind.InitialState))
            .Select(state => state.Name)
            .ToImmutableArray();

        if (initialStates.IsDefaultOrEmpty)
            return true;

        return initialStates.Any(stateName => !ctx.OmitLookup.Contains((stateName, field.Name)));
    }

    private static bool IsCollectionField(TypedField field) => field.ResolvedType is
        TypeKind.Set or
        TypeKind.Queue or
        TypeKind.Stack or
        TypeKind.Log or
        TypeKind.LogBy or
        TypeKind.Bag or
        TypeKind.List or
        TypeKind.QueueBy or
        TypeKind.Lookup;
}
