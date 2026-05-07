using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Name-binding stage: collects declarations from the construct manifest and
/// resolves identifier references to their declarations.
/// </summary>
/// <remarks>
/// <para>Runs after Parser, before TypeChecker. Input: ConstructManifest. Output: SymbolTable.</para>
/// <para>Pass 1 collects all field, state, and event declarations.
/// Pass 2 resolves references in expressions, slot targets, and outcomes.</para>
/// </remarks>
public static class NameBinder
{
    /// <summary>
    /// Bind declarations and resolve references from the parsed construct manifest.
    /// </summary>
    public static SymbolTable Bind(ConstructManifest manifest)
    {
        var binder = new BinderState();

        // Pass 1: Collect declarations
        foreach (var construct in manifest.Constructs)
        {
            binder.CollectDeclarations(construct);
        }

        // Build lookup dictionaries after Pass 1
        binder.BuildDictionaries();

        // Pass 2: Resolve references
        foreach (var construct in manifest.Constructs)
        {
            binder.ResolveReferences(construct);
        }

        return binder.ToSymbolTable();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Binder State
    // ════════════════════════════════════════════════════════════════════════════

    private sealed class BinderState
    {
        // Declaration builders (Pass 1 output)
        private readonly ImmutableArray<DeclaredField>.Builder _fields = ImmutableArray.CreateBuilder<DeclaredField>();
        private readonly ImmutableArray<DeclaredState>.Builder _states = ImmutableArray.CreateBuilder<DeclaredState>();
        private readonly ImmutableArray<DeclaredEvent>.Builder _events = ImmutableArray.CreateBuilder<DeclaredEvent>();
        private readonly ImmutableArray<SymbolReference>.Builder _references = ImmutableArray.CreateBuilder<SymbolReference>();
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        // Working dictionaries for duplicate detection (built during Pass 1)
        private readonly Dictionary<string, DeclaredField> _fieldsByName = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DeclaredState> _statesByName = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DeclaredEvent> _eventsByName = new(StringComparer.Ordinal);

        // Declaration order tracker for forward-reference detection (Q7)
        private int _fieldOrder;

        public void CollectDeclarations(ParsedConstruct construct)
        {
            switch (construct.Meta.Kind)
            {
                case ConstructKind.FieldDeclaration:
                    CollectField(construct);
                    break;
                case ConstructKind.StateDeclaration:
                    CollectState(construct);
                    break;
                case ConstructKind.EventDeclaration:
                    CollectEvent(construct);
                    break;
            }
        }

        private void CollectField(ParsedConstruct construct)
        {
            var idSlot = construct.GetSlot<IdentifierListSlot>(ConstructSlotKind.IdentifierList);
            var typeSlot = construct.GetSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);
            var modSlot = construct.GetSlot<ModifierListSlot>(ConstructSlotKind.ModifierList);
            var computeSlot = construct.GetSlot<ComputeExpressionSlot>(ConstructSlotKind.ComputeExpression);

            if (idSlot is null) return;

            var type = typeSlot?.TypeRef ?? new MissingTypeReference(construct.Span);
            var modifiers = modSlot?.Modifiers ?? ImmutableArray<ParsedModifier>.Empty;
            bool isComputed = computeSlot is not null;

            foreach (var name in idSlot.Names)
            {
                if (_fieldsByName.ContainsKey(name))
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.DuplicateFieldName,
                        idSlot.Span,
                        name));
                    continue;
                }

                var field = new DeclaredField(
                    Name: name,
                    Type: type,
                    Modifiers: modifiers,
                    IsComputed: isComputed,
                    Syntax: construct,
                    NameSpan: idSlot.Span,
                    DeclarationOrder: _fieldOrder++);

                _fields.Add(field);
                _fieldsByName[name] = field;
            }
        }

        private void CollectState(ParsedConstruct construct)
        {
            var entrySlot = construct.GetSlot<StateEntryListSlot>(ConstructSlotKind.StateEntryList);
            if (entrySlot is null) return;

            foreach (var (name, modifiers) in entrySlot.Entries)
            {
                if (_statesByName.ContainsKey(name))
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.DuplicateStateName,
                        entrySlot.Span,
                        name));
                    continue;
                }

                var state = new DeclaredState(
                    Name: name,
                    Modifiers: modifiers,
                    Syntax: construct,
                    NameSpan: entrySlot.Span);

                _states.Add(state);
                _statesByName[name] = state;
            }
        }

        private void CollectEvent(ParsedConstruct construct)
        {
            var idSlot = construct.GetSlot<IdentifierListSlot>(ConstructSlotKind.IdentifierList);
            var argSlot = construct.GetSlot<ArgumentListSlot>(ConstructSlotKind.ArgumentList);
            var initialSlot = construct.GetSlot<InitialMarkerSlot>(ConstructSlotKind.InitialMarker);

            if (idSlot is null) return;

            bool isInitial = initialSlot?.IsPresent ?? false;
            var argTuples = argSlot?.Args ?? ImmutableArray<(string Name, TypeMeta Type)>.Empty;

            foreach (var eventName in idSlot.Names)
            {
                if (_eventsByName.ContainsKey(eventName))
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.DuplicateEventName,
                        idSlot.Span,
                        eventName));
                    continue;
                }

                // Build DeclaredArg array from argument tuples
                var argsBuilder = ImmutableArray.CreateBuilder<DeclaredArg>(argTuples.Length);
                foreach (var (argName, argType) in argTuples)
                {
                    argsBuilder.Add(new DeclaredArg(
                        Name: argName,
                        Type: argType,
                        EventName: eventName,
                        NameSpan: argSlot!.Span));
                }

                var evt = new DeclaredEvent(
                    Name: eventName,
                    Args: argsBuilder.ToImmutable(),
                    IsInitial: isInitial,
                    Syntax: construct,
                    NameSpan: idSlot.Span);

                _events.Add(evt);
                _eventsByName[eventName] = evt;
            }
        }

        public void BuildDictionaries()
        {
            // Already built during Pass 1 into working dictionaries
        }

        public void ResolveReferences(ParsedConstruct construct)
        {
            // Determine context: is this an event-scoped construct?
            string? eventContext = null;
            DeclaredEvent? contextEvent = null;

            var eventTargetSlot = construct.GetSlot<EventTargetSlot>(ConstructSlotKind.EventTarget);
            if (eventTargetSlot?.EventName is { } eventName && _eventsByName.TryGetValue(eventName, out var evt))
            {
                eventContext = eventName;
                contextEvent = evt;
            }

            // Resolve state target slots
            var stateTargetSlot = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
            if (stateTargetSlot?.StateName is { } stateName)
            {
                ResolveStateReference(stateName, stateTargetSlot.Span);
            }

            // Resolve event target slots (the event name reference itself)
            if (eventTargetSlot?.EventName is { } eventRefName)
            {
                ResolveEventReference(eventRefName, eventTargetSlot.Span);
            }

            // Resolve field target slots (for access mode, omit declarations)
            var fieldTargetSlot = construct.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget);
            if (fieldTargetSlot?.FieldName is { } fieldName)
            {
                ResolveFieldReference(fieldName, fieldTargetSlot.Span, null);
            }

            // Resolve expressions in various slots
            ResolveExpressionSlots(construct, contextEvent);

            // Resolve outcome state references (transition State)
            var outcomeSlot = construct.GetSlot<OutcomeSlot>(ConstructSlotKind.Outcome);
            if (outcomeSlot?.Outcome is TransitionOutcome transition)
            {
                ResolveStateReference(transition.StateName, transition.Span);
            }
        }

        private void ResolveExpressionSlots(ParsedConstruct construct, DeclaredEvent? eventContext)
        {
            // Guard clause
            var guardSlot = construct.GetSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
            if (guardSlot is not null)
            {
                WalkExpression(guardSlot.Expression, eventContext, ImmutableHashSet<string>.Empty, null);
            }

            // Ensure clause
            var ensureSlot = construct.GetSlot<EnsureClauseSlot>(ConstructSlotKind.EnsureClause);
            if (ensureSlot is not null)
            {
                WalkExpression(ensureSlot.Expression, eventContext, ImmutableHashSet<string>.Empty, null);
            }

            // Compute expression (field declarations)
            var computeSlot = construct.GetSlot<ComputeExpressionSlot>(ConstructSlotKind.ComputeExpression);
            if (computeSlot is not null)
            {
                // For compute expressions, we need to track the declaring field for forward-ref detection
                var idSlot = construct.GetSlot<IdentifierListSlot>(ConstructSlotKind.IdentifierList);
                DeclaredField? declaringField = null;
                if (idSlot?.Names.Length > 0)
                {
                    _fieldsByName.TryGetValue(idSlot.Names[0], out declaringField);
                }
                WalkExpression(computeSlot.Expression, eventContext, ImmutableHashSet<string>.Empty, declaringField);
            }

            // Rule expression
            var ruleSlot = construct.GetSlot<RuleExpressionSlot>(ConstructSlotKind.RuleExpression);
            if (ruleSlot is not null)
            {
                WalkExpression(ruleSlot.Expression, eventContext, ImmutableHashSet<string>.Empty, null);
            }

            // Action chain
            var actionSlot = construct.GetSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
            if (actionSlot is not null)
            {
                foreach (var action in actionSlot.Actions)
                {
                    WalkAction(action, eventContext);
                }
            }
        }

        private void WalkAction(ParsedAction action, DeclaredEvent? eventContext)
        {
            var bindings = ImmutableHashSet<string>.Empty;

            switch (action)
            {
                case AssignAction assign:
                    WalkExpression(assign.Target, eventContext, bindings, null);
                    WalkExpression(assign.Value, eventContext, bindings, null);
                    break;
                case CollectionValueAction cv:
                    WalkExpression(cv.Target, eventContext, bindings, null);
                    WalkExpression(cv.Value, eventContext, bindings, null);
                    break;
                case CollectionIntoAction ci:
                    WalkExpression(ci.Target, eventContext, bindings, null);
                    if (ci.IntoTarget is not null) WalkExpression(ci.IntoTarget, eventContext, bindings, null);
                    break;
                case FieldOnlyAction fo:
                    WalkExpression(fo.Target, eventContext, bindings, null);
                    break;
                case CollectionValueByAction cvb:
                    WalkExpression(cvb.Target, eventContext, bindings, null);
                    WalkExpression(cvb.Value, eventContext, bindings, null);
                    WalkExpression(cvb.OrderingKey, eventContext, bindings, null);
                    break;
                case InsertAtAction ia:
                    WalkExpression(ia.Target, eventContext, bindings, null);
                    WalkExpression(ia.Value, eventContext, bindings, null);
                    WalkExpression(ia.Index, eventContext, bindings, null);
                    break;
                case RemoveAtAction ra:
                    WalkExpression(ra.Target, eventContext, bindings, null);
                    WalkExpression(ra.Index, eventContext, bindings, null);
                    break;
                case PutKeyValueAction pkv:
                    WalkExpression(pkv.Target, eventContext, bindings, null);
                    WalkExpression(pkv.Key, eventContext, bindings, null);
                    WalkExpression(pkv.Value, eventContext, bindings, null);
                    break;
                case CollectionIntoByAction cib:
                    WalkExpression(cib.Target, eventContext, bindings, null);
                    if (cib.IntoTarget is not null) WalkExpression(cib.IntoTarget, eventContext, bindings, null);
                    if (cib.OrderingCapture is not null) WalkExpression(cib.OrderingCapture, eventContext, bindings, null);
                    break;
            }
        }

        private void WalkExpression(
            ParsedExpression expr,
            DeclaredEvent? eventContext,
            ImmutableHashSet<string> bindings,
            DeclaredField? declaringField)
        {
            switch (expr)
            {
                case IdentifierExpression id:
                    ResolveIdentifier(id.Name, id.Span, eventContext, bindings, declaringField);
                    break;

                case GroupedExpression grouped:
                    WalkExpression(grouped.Inner, eventContext, bindings, declaringField);
                    break;

                case BinaryOperationExpression binary:
                    WalkExpression(binary.Left, eventContext, bindings, declaringField);
                    WalkExpression(binary.Right, eventContext, bindings, declaringField);
                    break;

                case UnaryOperationExpression unary:
                    WalkExpression(unary.Operand, eventContext, bindings, declaringField);
                    break;

                case MemberAccessExpression memberAccess:
                    // Special handling for EventName.ArgName pattern (qualified event arg access)
                    if (memberAccess.Target is IdentifierExpression targetId &&
                        _eventsByName.TryGetValue(targetId.Name, out var targetEvent))
                    {
                        // Target is an event name — record the event reference
                        _references.Add(new SymbolReference(targetId.Span, targetId.Name, new EventTarget(targetEvent)));

                        // Resolve the member as an arg on that event
                        var argOnEvent = targetEvent.Args.FirstOrDefault(a => a.Name == memberAccess.MemberName);
                        if (argOnEvent is not null)
                        {
                            _references.Add(new SymbolReference(memberAccess.Span, memberAccess.MemberName, new ArgTarget(argOnEvent)));
                        }
                        else
                        {
                            _diagnostics.Add(Diagnostics.Create(
                                DiagnosticCode.UndeclaredArg,
                                memberAccess.Span,
                                memberAccess.MemberName,
                                targetEvent.Name));
                            _references.Add(new SymbolReference(memberAccess.Span, memberAccess.MemberName, new UnresolvedTarget(memberAccess.MemberName, SymbolCategory.Any)));
                        }
                    }
                    else
                    {
                        // Normal member access — walk the target
                        WalkExpression(memberAccess.Target, eventContext, bindings, declaringField);
                    }
                    break;

                case ConditionalExpression cond:
                    WalkExpression(cond.Condition, eventContext, bindings, declaringField);
                    WalkExpression(cond.ThenBranch, eventContext, bindings, declaringField);
                    WalkExpression(cond.ElseBranch, eventContext, bindings, declaringField);
                    break;

                case FunctionCallExpression func:
                    foreach (var arg in func.Arguments)
                    {
                        WalkExpression(arg, eventContext, bindings, declaringField);
                    }
                    break;

                case MethodCallExpression method:
                    WalkExpression(method.Target, eventContext, bindings, declaringField);
                    foreach (var arg in method.Arguments)
                    {
                        WalkExpression(arg, eventContext, bindings, declaringField);
                    }
                    break;

                case ListLiteralExpression list:
                    foreach (var elem in list.Elements)
                    {
                        WalkExpression(elem, eventContext, bindings, declaringField);
                    }
                    break;

                case PostfixOperationExpression postfix:
                    WalkExpression(postfix.Operand, eventContext, bindings, declaringField);
                    break;

                case QuantifierExpression quant:
                    // Check for binding shadowing field (Q6 — hard error)
                    if (_fieldsByName.ContainsKey(quant.BindingName))
                    {
                        _diagnostics.Add(Diagnostics.Create(
                            DiagnosticCode.BindingShadowsField,
                            quant.Span,
                            quant.BindingName));
                    }
                    // Walk collection without the new binding
                    WalkExpression(quant.Collection, eventContext, bindings, declaringField);
                    // Walk predicate with the binding in scope
                    var newBindings = bindings.Add(quant.BindingName);
                    WalkExpression(quant.Predicate, eventContext, newBindings, declaringField);
                    break;

                case CIFunctionCallExpression ciFunc:
                    foreach (var arg in ciFunc.Arguments)
                    {
                        WalkExpression(arg, eventContext, bindings, declaringField);
                    }
                    break;

                case InterpolatedStringExpression interp:
                    foreach (var segment in interp.Segments)
                    {
                        if (segment is HoleSegment hole)
                        {
                            WalkExpression(hole.Expression, eventContext, bindings, declaringField);
                        }
                    }
                    break;

                case LiteralExpression:
                case MissingExpression:
                    // Nothing to resolve
                    break;
            }
        }

        private void ResolveIdentifier(
            string name,
            SourceSpan span,
            DeclaredEvent? eventContext,
            ImmutableHashSet<string> bindings,
            DeclaredField? declaringField)
        {
            // Scoping order (spec § 3):
            // 1. Quantifier bindings (innermost first, but we just have a flat set)
            // 2. Event args (if in event context)
            // 3. Fields

            // 1. Quantifier binding check
            if (bindings.Contains(name))
            {
                _references.Add(new SymbolReference(span, name, new BindingTarget(name)));
                return;
            }

            // 2. Event arg check (qualified form: EventName.ArgName)
            // The member access EventName.ArgName is handled in MemberAccessExpression walking,
            // but bare arg references in event context also resolve here
            if (eventContext is not null)
            {
                var arg = eventContext.Args.FirstOrDefault(a => a.Name == name);
                if (arg is not null)
                {
                    _references.Add(new SymbolReference(span, name, new ArgTarget(arg)));
                    return;
                }
            }

            // 3. Field check
            if (_fieldsByName.TryGetValue(name, out var field))
            {
                // Q7: Forward-reference detection in computed field expressions
                if (declaringField is not null && field.DeclarationOrder > declaringField.DeclarationOrder)
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.UndeclaredField,
                        span,
                        name));
                    _references.Add(new SymbolReference(span, name, new UnresolvedTarget(name, SymbolCategory.Field)));
                    return;
                }

                _references.Add(new SymbolReference(span, name, new FieldTarget(field)));
                return;
            }

            // Unresolved — emit diagnostic
            _diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.UndeclaredField,
                span,
                name));
            _references.Add(new SymbolReference(span, name, new UnresolvedTarget(name, SymbolCategory.Field)));
        }

        private void ResolveStateReference(string name, SourceSpan span)
        {
            if (_statesByName.TryGetValue(name, out var state))
            {
                _references.Add(new SymbolReference(span, name, new StateTarget(state)));
            }
            else
            {
                _diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UndeclaredState,
                    span,
                    name));
                _references.Add(new SymbolReference(span, name, new UnresolvedTarget(name, SymbolCategory.State)));
            }
        }

        private void ResolveEventReference(string name, SourceSpan span)
        {
            if (_eventsByName.TryGetValue(name, out var evt))
            {
                _references.Add(new SymbolReference(span, name, new EventTarget(evt)));
            }
            else
            {
                _diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UndeclaredEvent,
                    span,
                    name));
                _references.Add(new SymbolReference(span, name, new UnresolvedTarget(name, SymbolCategory.Event)));
            }
        }

        private void ResolveFieldReference(string name, SourceSpan span, DeclaredField? declaringField)
        {
            if (_fieldsByName.TryGetValue(name, out var field))
            {
                _references.Add(new SymbolReference(span, name, new FieldTarget(field)));
            }
            else
            {
                _diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.UndeclaredField,
                    span,
                    name));
                _references.Add(new SymbolReference(span, name, new UnresolvedTarget(name, SymbolCategory.Field)));
            }
        }

        public SymbolTable ToSymbolTable()
        {
            return new SymbolTable(
                Fields: _fields.ToImmutable(),
                States: _states.ToImmutable(),
                Events: _events.ToImmutable(),
                FieldsByName: _fieldsByName.ToImmutableDictionary(StringComparer.Ordinal),
                StatesByName: _statesByName.ToImmutableDictionary(StringComparer.Ordinal),
                EventsByName: _eventsByName.ToImmutableDictionary(StringComparer.Ordinal),
                References: _references.ToImmutable(),
                Diagnostics: _diagnostics.ToImmutable()
            );
        }
    }
}
