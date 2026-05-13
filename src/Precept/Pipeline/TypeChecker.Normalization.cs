using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    // ════════════════════════════════════════════════════════════════════════
    //  Pass 2 — transition row + event handler normalization (Slice 5)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Iterate all <see cref="ConstructKind.TransitionRow"/> constructs from the manifest,
    /// resolve each to a <see cref="TypedTransitionRow"/>, and accumulate into <see cref="CheckContext.TransitionRows"/>.
    /// Records <see cref="StateReference"/> and <see cref="EventReference"/> sites for LS navigation.
    /// </summary>
    private static void PopulateTransitionRows(ConstructManifest manifest, CheckContext ctx)
    {
        foreach (var construct in manifest.ByKind[ConstructKind.TransitionRow])
        {
            var rows = NormalizeTransitionRow(construct, ctx);
            ctx.TransitionRows.AddRange(rows);
        }

        // D26: if any TypedErrorExpression in transition rows → at least one Error diagnostic must exist
        if (ctx.TransitionRows.Any(r => ContainsErrorExpression(r))
            && !ctx.Diagnostics.Any(d => d.Severity == Severity.Error))
        {
            throw new InvalidOperationException(
                "D26 violated: TypedErrorExpression present in transition rows but no Error-severity diagnostic emitted.");
        }
    }

    /// <summary>
    /// Iterate all <see cref="ConstructKind.EventHandler"/> constructs from the manifest,
    /// resolve each to a <see cref="TypedEventHandler"/>, and accumulate into <see cref="CheckContext.EventHandlers"/>.
    /// Records <see cref="EventReference"/> sites for LS navigation.
    /// </summary>
    private static void PopulateEventHandlers(ConstructManifest manifest, CheckContext ctx)
    {
        foreach (var construct in manifest.ByKind[ConstructKind.EventHandler])
        {
            var handler = NormalizeEventHandler(construct, ctx);
            ctx.EventHandlers.Add(handler);
        }

        // D26: if any TypedErrorExpression in event handlers → at least one Error diagnostic must exist
        if (ctx.EventHandlers.Any(h => h.Actions.Any(a => a is TypedInputAction ia && ContainsErrorExpressionInAction(ia)))
            && !ctx.Diagnostics.Any(d => d.Severity == Severity.Error))
        {
            throw new InvalidOperationException(
                "D26 violated: TypedErrorExpression present in event handlers but no Error-severity diagnostic emitted.");
        }
    }

    /// <summary>
    /// Iterate all <see cref="ConstructKind.RuleDeclaration"/> constructs from the manifest,
    /// resolve each to a <see cref="TypedRule"/>, and accumulate into <see cref="CheckContext.Rules"/>.
    /// </summary>
    private static void PopulateRules(ConstructManifest manifest, CheckContext ctx)
    {
        if (!manifest.ByKind.Contains(ConstructKind.RuleDeclaration))
            return;

        foreach (var construct in manifest.ByKind[ConstructKind.RuleDeclaration])
        {
            var ruleSlot = construct.GetRequiredSlot<RuleExpressionSlot>(ConstructSlotKind.RuleExpression);
            ctx.CurrentScope = FieldScopeMode.AllFields;
            var condition = Resolve(ruleSlot.Expression, ctx);

            TypedExpression? guard = null;
            var guardSlot = construct.GetSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
            if (guardSlot is not null)
            {
                guard = Resolve(guardSlot.Expression, ctx);
                if (guard is not TypedErrorExpression && guard.ResultType != TypeKind.Boolean)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.TypeMismatch, guardSlot.Expression.Span,
                            Types.GetMeta(TypeKind.Boolean).DisplayName,
                            Types.GetMeta(guard.ResultType).DisplayName));
                    guard = new TypedErrorExpression(guardSlot.Expression.Span);
                }
            }

            var becauseSlot = construct.GetRequiredSlot<BecauseClauseSlot>(ConstructSlotKind.BecauseClause);
            var message = new TypedLiteral(TypeKind.String, becauseSlot.Message, becauseSlot.Span);

            ctx.Rules.Add(new TypedRule(condition, guard, message, construct));
            ctx.ConstraintRefs.Add(new ConstraintFieldRefs(
                new RuleIdentity(ctx.Rules.Count - 1),
                CollectFieldRefs(condition).Distinct().ToImmutableArray(),
                CollectArgRefs(condition).Distinct().ToImmutableArray()));
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 2b — construct normalization (B2)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Process <see cref="ConstructKind.StateEnsure"/> and <see cref="ConstructKind.EventEnsure"/>
    /// constructs into <see cref="TypedEnsure"/> records.
    /// </summary>
    private static void PopulateEnsures(ConstructManifest manifest, CheckContext ctx)
    {
        // —— State ensures (in/to/from State ensure Expr because Msg) ——
        if (manifest.ByKind.Contains(ConstructKind.StateEnsure))
        {
            foreach (var construct in manifest.ByKind[ConstructKind.StateEnsure])
            {
                var stateSlot = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
                var anchorStates = ResolveStateTargets(stateSlot, ctx);

                // Determine constraint kind from leading token (catalog-driven; fallback stays resident)
                var constraintKind = construct.LeadingTokenKind is { } leadingToken
                                     && Constraints.ByToken.TryGetValue(leadingToken, out var ck)
                    ? ck
                    : ConstraintKind.StateResident;

                var ensureSlot = construct.GetSlot<EnsureClauseSlot>(ConstructSlotKind.EnsureClause);
                if (ensureSlot is null) continue;

                ctx.CurrentScope = FieldScopeMode.AllFields;
                var condition = Resolve(ensureSlot.Expression, ctx);

                TypedExpression? ensureGuard = null;
                var ensureGuardSlot = construct.GetSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
                if (ensureGuardSlot is not null)
                {
                    ctx.CurrentScope = FieldScopeMode.AllFields;
                    ensureGuard = Resolve(ensureGuardSlot.Expression, ctx);
                    if (ensureGuard is not TypedErrorExpression && ensureGuard.ResultType != TypeKind.Boolean)
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(DiagnosticCode.TypeMismatch, ensureGuardSlot.Expression.Span,
                                Types.GetMeta(TypeKind.Boolean).DisplayName,
                                Types.GetMeta(ensureGuard.ResultType).DisplayName));
                        ensureGuard = new TypedErrorExpression(ensureGuardSlot.Expression.Span);
                    }
                }

                var becauseSlot = construct.GetSlot<BecauseClauseSlot>(ConstructSlotKind.BecauseClause);
                var message = becauseSlot is not null
                    ? new TypedLiteral(TypeKind.String, becauseSlot.Message, becauseSlot.Span)
                    : new TypedLiteral(TypeKind.String, "", construct.Span);

                foreach (var anchorState in anchorStates)
                {
                    ctx.Ensures.Add(new TypedEnsure(
                        Kind: constraintKind,
                        AnchorState: anchorState.StateName,
                        AnchorEvent: null,
                        Condition: condition,
                        Guard: ensureGuard,
                        Message: message,
                        Syntax: construct));
                    ctx.ConstraintRefs.Add(new ConstraintFieldRefs(
                        new EnsureIdentity(constraintKind, anchorState.StateName, ctx.Ensures.Count - 1),
                        CollectFieldRefs(condition).Distinct().ToImmutableArray(),
                        CollectArgRefs(condition).Distinct().ToImmutableArray()));
                }
            }
        }

        // —— Event ensures (on Event ensure Expr because Msg) ——
        if (manifest.ByKind.Contains(ConstructKind.EventEnsure))
        {
            foreach (var construct in manifest.ByKind[ConstructKind.EventEnsure])
            {
                var eventSlot = construct.GetSlot<EventTargetSlot>(ConstructSlotKind.EventTarget);
                string? anchorEvent = null;
                TypedEvent? resolvedEvent = null;
                if (eventSlot?.EventName is not null)
                {
                    if (ctx.EventLookup.TryGetValue(eventSlot.EventName, out var evTyped))
                    {
                        anchorEvent = evTyped.Name;
                        resolvedEvent = evTyped;
                        ctx.EventReferences.Add(new EventReference(evTyped, eventSlot.NameSpan));
                    }
                    else
                    {
                        anchorEvent = eventSlot.EventName;
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(DiagnosticCode.UndeclaredEvent, eventSlot.NameSpan, eventSlot.EventName));
                    }
                }

                // Set event args scope for the ensure expression
                IReadOnlyDictionary<string, TypedArg>? previousArgs = ctx.CurrentEventArgs;
                if (resolvedEvent is not null)
                    ctx.CurrentEventArgs = resolvedEvent.Args.ToFrozenDictionary(a => a.Name);

                try
                {
                    var ensureSlot = construct.GetSlot<EnsureClauseSlot>(ConstructSlotKind.EnsureClause);
                    if (ensureSlot is null) continue;

                    ctx.CurrentScope = FieldScopeMode.AllFields;
                    var condition = Resolve(ensureSlot.Expression, ctx);

                    TypedExpression? eventEnsureGuard = null;
                    var eventEnsureGuardSlot = construct.GetSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
                    if (eventEnsureGuardSlot is not null)
                    {
                        ctx.CurrentScope = FieldScopeMode.AllFields;
                        eventEnsureGuard = Resolve(eventEnsureGuardSlot.Expression, ctx);
                        if (eventEnsureGuard is not TypedErrorExpression && eventEnsureGuard.ResultType != TypeKind.Boolean)
                        {
                            ctx.Diagnostics.Add(
                                Diagnostics.Create(DiagnosticCode.TypeMismatch, eventEnsureGuardSlot.Expression.Span,
                                    Types.GetMeta(TypeKind.Boolean).DisplayName,
                                    Types.GetMeta(eventEnsureGuard.ResultType).DisplayName));
                            eventEnsureGuard = new TypedErrorExpression(eventEnsureGuardSlot.Expression.Span);
                        }
                    }

                    var becauseSlot = construct.GetSlot<BecauseClauseSlot>(ConstructSlotKind.BecauseClause);
                    var message = becauseSlot is not null
                        ? new TypedLiteral(TypeKind.String, becauseSlot.Message, becauseSlot.Span)
                        : new TypedLiteral(TypeKind.String, "", construct.Span);

                    ctx.Ensures.Add(new TypedEnsure(
                        Kind: ConstraintKind.EventPrecondition,
                        AnchorState: null,
                        AnchorEvent: anchorEvent,
                        Condition: condition,
                        Guard: eventEnsureGuard,
                        Message: message,
                        Syntax: construct));
                    ctx.ConstraintRefs.Add(new ConstraintFieldRefs(
                        new EnsureIdentity(ConstraintKind.EventPrecondition, anchorEvent, ctx.Ensures.Count - 1),
                        CollectFieldRefs(condition).Distinct().ToImmutableArray(),
                        CollectArgRefs(condition).Distinct().ToImmutableArray()));
                }
                finally
                {
                    ctx.CurrentEventArgs = previousArgs;
                }
            }
        }
    }

    /// <summary>
    /// Process <see cref="ConstructKind.AccessMode"/> constructs into
    /// <see cref="TypedAccessMode"/> records.
    /// </summary>
    private static bool HasKeywordTokenMeta(string text, Func<TokenMeta, bool> predicate) =>
        Tokens.Keywords.TryGetValue(text, out var kind) && predicate(Tokens.GetMeta(kind));

    private readonly record struct ResolvedStateTarget(string StateName, SourceSpan Span, bool IsWildcard);

    private static ImmutableArray<ResolvedStateTarget> ResolveStateTargets(StateTargetSlot? stateSlot, CheckContext ctx)
    {
        if (stateSlot is null || stateSlot.StateNames.IsDefaultOrEmpty)
            return [new ResolvedStateTarget("any", stateSlot?.Span ?? SourceSpan.Missing, IsWildcard: true)];

        var wildcardEntries = ImmutableArray.CreateBuilder<ResolvedStateTarget>();
        for (var i = 0; i < stateSlot.StateNames.Length; i++)
        {
            var stateName = stateSlot.StateNames[i];
            var nameSpan = i < stateSlot.NameSpans.Length ? stateSlot.NameSpans[i] : stateSlot.NameSpan;
            if (HasKeywordTokenMeta(stateName, meta => meta.IsStateWildcard))
                wildcardEntries.Add(new ResolvedStateTarget(stateName, nameSpan, IsWildcard: true));
        }

        if (wildcardEntries.Count > 0)
        {
            if (wildcardEntries.Count != stateSlot.StateNames.Length)
            {
                ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.StateListContainsWildcard, wildcardEntries[0].Span));
                return ImmutableArray<ResolvedStateTarget>.Empty;
            }

            return [wildcardEntries[0]];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var builder = ImmutableArray.CreateBuilder<ResolvedStateTarget>(stateSlot.StateNames.Length);
        for (var i = 0; i < stateSlot.StateNames.Length; i++)
        {
            var stateName = stateSlot.StateNames[i];
            var nameSpan = i < stateSlot.NameSpans.Length ? stateSlot.NameSpans[i] : stateSlot.NameSpan;
            if (!seen.Add(stateName))
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.DuplicateStateInList, nameSpan, stateName));
                continue;
            }

            if (ctx.StateLookup.TryGetValue(stateName, out var typedState))
            {
                builder.Add(new ResolvedStateTarget(typedState.Name, nameSpan, IsWildcard: false));
                ctx.StateReferences.Add(new StateReference(typedState, nameSpan));
            }
            else
            {
                builder.Add(new ResolvedStateTarget(stateName, nameSpan, IsWildcard: false));
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.UndeclaredState, nameSpan, stateName));
            }
        }

        return builder.ToImmutable();
    }

    private static void PopulateAccessModes(ConstructManifest manifest, CheckContext ctx)
    {
        if (manifest.ByKind.Contains(ConstructKind.AccessMode))
        {
            foreach (var construct in manifest.ByKind[ConstructKind.AccessMode])
                PopulateAccessModesForConstruct(construct, ctx, forcedMode: null, emitUndeclaredDiagnostics: true);
        }

        if (manifest.ByKind.Contains(ConstructKind.OmitDeclaration))
        {
            foreach (var construct in manifest.ByKind[ConstructKind.OmitDeclaration])
                PopulateAccessModesForConstruct(construct, ctx, ModifierKind.Omit, emitUndeclaredDiagnostics: false);
        }
    }

    private static void PopulateAccessModesForConstruct(
        ParsedConstruct construct,
        CheckContext ctx,
        ModifierKind? forcedMode,
        bool emitUndeclaredDiagnostics)
    {
        // —— State reference ——
        var stateSlot = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
        var resolvedStates = ResolveStateTargets(stateSlot, ctx);

        // —— Field reference ——
        var fieldSlot = construct.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget);
        string fieldName = "";
        var additionalFieldNames = ImmutableArray<(string FieldName, SourceSpan Span)>.Empty;
        if (fieldSlot?.FieldName is not null)
        {
            fieldName = ResolveAccessModeFieldName(fieldSlot.FieldName, fieldSlot.NameSpan, ctx, emitUndeclaredDiagnostics);

            if (!fieldSlot.AdditionalFields.IsDefaultOrEmpty)
            {
                var additionalBuilder = ImmutableArray.CreateBuilder<(string FieldName, SourceSpan Span)>(fieldSlot.AdditionalFields.Length);
                foreach (var (additionalFieldName, additionalFieldSpan) in fieldSlot.AdditionalFields)
                {
                    additionalBuilder.Add((
                        ResolveAccessModeFieldName(additionalFieldName, additionalFieldSpan, ctx, emitUndeclaredDiagnostics),
                        additionalFieldSpan));
                }

                additionalFieldNames = additionalBuilder.ToImmutable();
            }
        }

        // —— Mode (readonly / editable / omit → modifier kind) ——
        ModifierKind mode;
        TypedExpression? guard = null;
        if (forcedMode is { } explicitMode)
        {
            mode = explicitMode;
        }
        else
        {
            var modeSlot = construct.GetSlot<AccessModeSlot>(ConstructSlotKind.AccessModeKeyword);
            mode = modeSlot?.AccessMode is { } accessToken
                   && Modifiers.ByAccessToken.TryGetValue(accessToken, out var accessMeta)
                ? accessMeta.Kind
                : ModifierKind.Read;

            // —— Optional guard ——
            var guardSlot = construct.GetSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
            if (guardSlot is not null)
            {
                ctx.CurrentScope = FieldScopeMode.AllFields;
                guard = Resolve(guardSlot.Expression, ctx);
                if (guard is not TypedErrorExpression && guard.ResultType != TypeKind.Boolean)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.TypeMismatch, guardSlot.Expression.Span,
                            Types.GetMeta(TypeKind.Boolean).DisplayName,
                            Types.GetMeta(guard.ResultType).DisplayName));
                    guard = new TypedErrorExpression(guardSlot.Expression.Span);
                }
            }
        }

        foreach (var resolvedState in resolvedStates)
        {
            ctx.AccessModes.Add(new TypedAccessMode(
                StateName: resolvedState.StateName,
                FieldName: fieldName,
                Mode: mode,
                Guard: guard,
                Syntax: construct));

            foreach (var (additionalFieldName, _) in additionalFieldNames)
            {
                ctx.AccessModes.Add(new TypedAccessMode(
                    StateName: resolvedState.StateName,
                    FieldName: additionalFieldName,
                    Mode: mode,
                    Guard: guard,
                    Syntax: construct));
            }
        }
    }

    private static string ResolveAccessModeFieldName(
        string fieldName,
        SourceSpan fieldSpan,
        CheckContext ctx,
        bool emitUndeclaredDiagnostics)
    {
        if (HasKeywordTokenMeta(fieldName, meta => meta.IsFieldBroadcast))
        {
            return fieldName;
        }

        if (ctx.FieldLookup.TryGetValue(fieldName, out var typedField))
        {
            ctx.FieldReferences.Add(new FieldReference(typedField, fieldSpan));
            return typedField.Name;
        }

        if (emitUndeclaredDiagnostics)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.UndeclaredField, fieldSpan, fieldName));
        }

        return fieldName;
    }

    /// <summary>
    /// Process <see cref="ConstructKind.StateAction"/> constructs (on-entry/on-exit action chains)
    /// into <see cref="TypedStateHook"/> records.
    /// </summary>
    private static void PopulateStateHooks(ConstructManifest manifest, CheckContext ctx)
    {
        if (!manifest.ByKind.Contains(ConstructKind.StateAction))
            return;

        foreach (var construct in manifest.ByKind[ConstructKind.StateAction])
        {
            // —— State reference ——
            var stateSlot = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
            var resolvedStates = ResolveStateTargets(stateSlot, ctx);

            // —— Hook scope from leading token (catalog-driven; fallback stays OnEntry) ——
            var scope = construct.LeadingTokenKind is { } leadingToken
                        && Modifiers.ByAnchorToken.TryGetValue(leadingToken, out var anchorMeta)
                ? anchorMeta.Scope
                : AnchorScope.OnEntry;

            // —— Optional guard ——
            TypedExpression? guard = null;
            var guardSlot = construct.GetSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
            if (guardSlot is not null)
            {
                ctx.CurrentScope = FieldScopeMode.AllFields;
                guard = Resolve(guardSlot.Expression, ctx);
                if (guard is not TypedErrorExpression && guard.ResultType != TypeKind.Boolean)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.TypeMismatch, guardSlot.Expression.Span,
                            Types.GetMeta(TypeKind.Boolean).DisplayName,
                            Types.GetMeta(guard.ResultType).DisplayName));
                    guard = new TypedErrorExpression(guardSlot.Expression.Span);
                }
            }

            // —— Action chain ——
            var actionChainSlot = construct.GetSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
            var actions = ImmutableArray<TypedAction>.Empty;
            if (actionChainSlot is not null)
            {
                var builder = ImmutableArray.CreateBuilder<TypedAction>(actionChainSlot.Actions.Length);
                foreach (var parsedAction in actionChainSlot.Actions)
                    builder.Add(ResolveAction(parsedAction, ctx));
                actions = builder.MoveToImmutable();
            }

            foreach (var resolvedState in resolvedStates)
            {
                ctx.StateHooks.Add(new TypedStateHook(
                    Scope: scope,
                    StateName: resolvedState.StateName,
                    Guard: guard,
                    Actions: actions,
                    Syntax: construct));
            }
        }
    }

    /// <summary>
    /// D24 placeholder — process edit declaration constructs into
    /// <see cref="TypedEditDeclaration"/> records. Currently a no-op if
    /// no edit declaration <see cref="ConstructKind"/> exists in the manifest.
    /// </summary>
    private static void PopulateEditDeclarations(ConstructManifest manifest, CheckContext ctx)
    {
        // Edit declarations use ConstructKind.OmitDeclaration or a future dedicated kind.
        // For now, OmitDeclaration is the closest existing construct for field exclusion.
        // Full edit declaration support (D24) will arrive with stateless-precept design.
        if (!manifest.ByKind.Contains(ConstructKind.OmitDeclaration))
            return;

        foreach (var construct in manifest.ByKind[ConstructKind.OmitDeclaration])
        {
            var fieldSlot = construct.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget);
            var fields = ImmutableArray<string>.Empty;
            bool isEditAll = false;

            if (fieldSlot is not null)
            {
                var fieldName = fieldSlot.FieldName;
                if (fieldName is null || HasKeywordTokenMeta(fieldName, meta => meta.IsFieldBroadcast))
                {
                    isEditAll = true;
                }
                else
                {
                    var fieldBuilder = ImmutableArray.CreateBuilder<string>(1 + fieldSlot.AdditionalFields.Length);
                    fieldBuilder.Add(fieldName);
                    if (ctx.FieldLookup.TryGetValue(fieldName, out var typedField))
                        ctx.FieldReferences.Add(new FieldReference(typedField, fieldSlot.NameSpan));

                    foreach (var (additionalFieldName, additionalFieldSpan) in fieldSlot.AdditionalFields)
                    {
                        fieldBuilder.Add(additionalFieldName);
                        if (ctx.FieldLookup.TryGetValue(additionalFieldName, out var additionalTypedField))
                            ctx.FieldReferences.Add(new FieldReference(additionalTypedField, additionalFieldSpan));
                    }

                    fields = fieldBuilder.ToImmutable();
                }
            }

            ctx.EditDeclarations.Add(new TypedEditDeclaration(
                EditableFields: fields,
                IsEditAll: isEditAll,
                Syntax: construct));
        }
    }
}
