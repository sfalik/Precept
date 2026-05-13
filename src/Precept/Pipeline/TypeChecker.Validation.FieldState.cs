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

            foreach (var action in row.Actions)
            {
                if (action is not TypedInputAction inputAction)
                    continue;

                fieldRefs.Clear();
                CollectFieldRefsFromExpression(inputAction.InputExpression, fieldRefs);
                CollectFieldRefsFromExpression(inputAction.SecondaryExpression, fieldRefs);
                EmitDiagnosticsForFieldRefs(row.FromState);
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
        foreach (var row in ctx.TransitionRows)
        {
            if (row.Outcome != TransitionRowOutcome.Transition || row.TargetState is null)
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

        // D132: transitions that materialize a required field on entry must assign it.
        foreach (var row in ctx.TransitionRows)
        {
            if (row.Outcome != TransitionRowOutcome.Transition || row.TargetState is null)
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

                    var hasSet = row.Actions.Any(action =>
                        IsSetAction(action.Kind) &&
                        string.Equals(action.FieldName, field.Name, StringComparison.Ordinal));

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

        if (ctx.States.Count == 0)
            return;

        var initialStateNames = ctx.States
            .Where(state => state.Modifiers.Contains(ModifierKind.InitialState))
            .Select(state => state.Name)
            .ToHashSet(StringComparer.Ordinal);

        var initialRows = ctx.TransitionRows
            .Where(row =>
                string.Equals(row.EventName, initialEvent.Name, StringComparison.Ordinal) &&
                row.FromState is { } fromState &&
                initialStateNames.Contains(fromState))
            .ToImmutableArray();

        if (initialRows.IsDefaultOrEmpty)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.InitialEventMissingAssignments,
                    initialEvent.NameSpan,
                    initialEvent.Name,
                    string.Join(", ", requiredFields.Select(field => field.Name))));
            return;
        }

        foreach (var row in initialRows)
        {
            var missingFields = requiredFields
                .Where(field => !row.Actions.Any(action =>
                    IsSetAction(action.Kind) &&
                    string.Equals(action.FieldName, field.Name, StringComparison.Ordinal)))
                .Select(field => field.Name)
                .ToImmutableArray();

            if (missingFields.IsDefaultOrEmpty)
                continue;

            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.InitialEventMissingAssignments,
                    row.RowSpan,
                    initialEvent.Name,
                    string.Join(", ", missingFields)));
        }
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
