using System.Collections.Immutable;
using System.Linq;
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

        // Pass 2: Resolve references (computed fields in dependency order)
        binder.ResolveAllReferences(manifest.Constructs);

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

            for (var i = 0; i < idSlot.Names.Length; i++)
            {
                var name = idSlot.Names[i];
                var nameSpan = idSlot.NameSpans.Length > i ? idSlot.NameSpans[i] : idSlot.Span;
                if (_fieldsByName.ContainsKey(name))
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.DuplicateFieldName,
                        nameSpan,
                        name));
                    continue;
                }

                var field = new DeclaredField(
                    Name: name,
                    Type: type,
                    Modifiers: modifiers,
                    IsComputed: isComputed,
                    Syntax: construct,
                    NameSpan: nameSpan,
                    DeclarationOrder: _fieldOrder++);

                _fields.Add(field);
                _fieldsByName[name] = field;
            }
        }

        private void CollectState(ParsedConstruct construct)
        {
            var entrySlot = construct.GetSlot<StateEntryListSlot>(ConstructSlotKind.StateEntryList);
            if (entrySlot is null) return;

            foreach (var entry in entrySlot.Entries)
            {
                if (_statesByName.ContainsKey(entry.Name))
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.DuplicateStateName,
                        entry.NameSpan,
                        entry.Name));
                    continue;
                }

                var state = new DeclaredState(
                    Name: entry.Name,
                    Modifiers: entry.Modifiers,
                    Syntax: construct,
                    NameSpan: entry.NameSpan);

                _states.Add(state);
                _statesByName[entry.Name] = state;
            }
        }

        private void CollectEvent(ParsedConstruct construct)
        {
            var entrySlot = construct.GetSlot<EventEntryListSlot>(ConstructSlotKind.EventEntryList);
            if (entrySlot is null) return;

            foreach (var entry in entrySlot.Entries)
            {
                var eventName = entry.Name;
                var nameSpan = entry.NameSpan;

                if (_eventsByName.ContainsKey(eventName))
                {
                    _diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.DuplicateEventName,
                        nameSpan,
                        eventName));
                    continue;
                }

                // Build DeclaredArg array from per-entry argument list
                var argsBuilder = ImmutableArray.CreateBuilder<DeclaredArg>(entry.Args.Length);
                foreach (var argEntry in entry.Args)
                {
                    argsBuilder.Add(new DeclaredArg(
                        Name: argEntry.Name,
                        Type: argEntry.Type,
                        EventName: eventName,
                        Modifiers: argEntry.Modifiers,
                        NameSpan: argEntry.NameSpan,
                        ParsedModifiers: argEntry.ParsedModifiers));
                }

                var evt = new DeclaredEvent(
                    Name: eventName,
                    Args: argsBuilder.ToImmutable(),
                    IsInitial: entry.IsInitial,
                    Syntax: construct,
                    NameSpan: nameSpan);

                _events.Add(evt);
                _eventsByName[eventName] = evt;
            }
        }

        public void BuildDictionaries()
        {
            // Already built during Pass 1 into working dictionaries
        }

        public void ResolveAllReferences(ImmutableArray<ParsedConstruct> constructs)
        {
            var computedConstructs = new List<ParsedConstruct>();

            foreach (var construct in constructs)
            {
                if (IsComputedFieldConstruct(construct))
                {
                    computedConstructs.Add(construct);
                    continue;
                }

                ResolveReferences(construct);
            }

            foreach (var construct in OrderComputedConstructsByDependency(computedConstructs))
            {
                ResolveReferences(construct);
            }
        }

        private static bool IsComputedFieldConstruct(ParsedConstruct construct) =>
            construct.Meta.Kind == ConstructKind.FieldDeclaration
            && construct.GetSlot<ComputeExpressionSlot>(ConstructSlotKind.ComputeExpression) is not null;

        private IReadOnlyList<ParsedConstruct> OrderComputedConstructsByDependency(
            IReadOnlyList<ParsedConstruct> computedConstructs)
        {
            if (computedConstructs.Count == 0)
            {
                return [];
            }

            var constructByField = new Dictionary<string, ParsedConstruct>(StringComparer.Ordinal);
            var dependencies = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var dependents = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var construct in computedConstructs)
            {
                var fieldName = GetPrimaryFieldName(construct);
                if (fieldName is null)
                {
                    continue;
                }

                constructByField[fieldName] = construct;
                dependencies[fieldName] = [];
                dependents[fieldName] = [];
            }

            foreach (var (fieldName, construct) in constructByField.OrderBy(static kvp => kvp.Key, StringComparer.Ordinal))
            {
                var computeSlot = construct.GetSlot<ComputeExpressionSlot>(ConstructSlotKind.ComputeExpression);
                if (computeSlot is null)
                {
                    continue;
                }

                var refs = new HashSet<string>(StringComparer.Ordinal);
                CollectFieldDependencies(computeSlot.Expression, refs, ImmutableHashSet<string>.Empty);

                foreach (var dep in refs.Where(constructByField.ContainsKey))
                {
                    dependencies[fieldName].Add(dep);
                    dependents[dep].Add(fieldName);
                }
            }

            var ready = new SortedSet<(int Order, string Name)>(
                dependencies
                    .Where(static kvp => kvp.Value.Count == 0)
                    .Select(kvp => (GetFieldDeclarationOrder(kvp.Key), kvp.Key)));
            var orderedFields = new List<string>(dependencies.Count);

            while (ready.Count > 0)
            {
                var next = ready.Min;
                ready.Remove(next);
                orderedFields.Add(next.Name);

                foreach (var dependent in dependents[next.Name]
                             .OrderBy(GetFieldDeclarationOrder)
                             .ThenBy(static name => name, StringComparer.Ordinal))
                {
                    if (!dependencies[dependent].Remove(next.Name) || dependencies[dependent].Count != 0)
                    {
                        continue;
                    }

                    ready.Add((GetFieldDeclarationOrder(dependent), dependent));
                }
            }

            var cyclicFields = dependencies
                .Where(static kvp => kvp.Value.Count > 0)
                .Select(static kvp => kvp.Key)
                .OrderBy(GetFieldDeclarationOrder)
                .ThenBy(static name => name, StringComparer.Ordinal)
                .ToArray();
            var cyclicSet = cyclicFields.ToHashSet(StringComparer.Ordinal);

            foreach (var fieldName in cyclicFields)
            {
                var field = _fieldsByName[fieldName];
                var cycleMembers = dependencies[fieldName]
                    .Where(cyclicSet.Contains)
                    .OrderBy(GetFieldDeclarationOrder)
                    .ThenBy(static name => name, StringComparer.Ordinal)
                    .ToArray();
                var cycle = string.Join(" → ", cycleMembers.Prepend(fieldName).Append(fieldName));

                _diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.CircularComputedField,
                    field.NameSpan,
                    fieldName,
                    cycle));
            }

            orderedFields.AddRange(cyclicFields);

            return orderedFields
                .Select(field => constructByField[field])
                .Distinct()
                .ToArray();
        }

        private static string? GetPrimaryFieldName(ParsedConstruct construct) =>
            construct.GetSlot<IdentifierListSlot>(ConstructSlotKind.IdentifierList)?.Names.FirstOrDefault();

        private int GetFieldDeclarationOrder(string fieldName) =>
            _fieldsByName.TryGetValue(fieldName, out var field)
                ? field.DeclarationOrder
                : int.MaxValue;

        private static void CollectFieldDependencies(
            ParsedExpression expression,
            ISet<string> refs,
            ImmutableHashSet<string> bindings)
        {
            switch (expression)
            {
                case IdentifierExpression id:
                    if (!bindings.Contains(id.Name))
                        refs.Add(id.Name);
                    break;
                case GroupedExpression grouped:
                    CollectFieldDependencies(grouped.Inner, refs, bindings);
                    break;
                case BinaryOperationExpression binary:
                    CollectFieldDependencies(binary.Left, refs, bindings);
                    CollectFieldDependencies(binary.Right, refs, bindings);
                    break;
                case UnaryOperationExpression unary:
                    CollectFieldDependencies(unary.Operand, refs, bindings);
                    break;
                case MemberAccessExpression member:
                    CollectFieldDependencies(member.Target, refs, bindings);
                    break;
                case ConditionalExpression conditional:
                    CollectFieldDependencies(conditional.Condition, refs, bindings);
                    CollectFieldDependencies(conditional.ThenBranch, refs, bindings);
                    CollectFieldDependencies(conditional.ElseBranch, refs, bindings);
                    break;
                case FunctionCallExpression functionCall:
                    foreach (var argument in functionCall.Arguments)
                        CollectFieldDependencies(argument, refs, bindings);
                    break;
                case MethodCallExpression methodCall:
                    CollectFieldDependencies(methodCall.Target, refs, bindings);
                    foreach (var argument in methodCall.Arguments)
                        CollectFieldDependencies(argument, refs, bindings);
                    break;
                case ListLiteralExpression list:
                    foreach (var element in list.Elements)
                        CollectFieldDependencies(element, refs, bindings);
                    break;
                case PostfixOperationExpression postfix:
                    CollectFieldDependencies(postfix.Operand, refs, bindings);
                    break;
                case QuantifierExpression quantifier:
                    CollectFieldDependencies(quantifier.Collection, refs, bindings);
                    CollectFieldDependencies(quantifier.Predicate, refs, bindings.Add(quantifier.BindingName));
                    break;
                case CIFunctionCallExpression ciFunction:
                    foreach (var argument in ciFunction.Arguments)
                        CollectFieldDependencies(argument, refs, bindings);
                    break;
                case InterpolatedStringExpression interpolated:
                    foreach (var segment in interpolated.Segments.OfType<HoleSegment>())
                        CollectFieldDependencies(segment.Expression, refs, bindings);
                    break;
                case InterpolatedTypedConstantExpression interpolatedTyped:
                    foreach (var segment in interpolatedTyped.Segments.OfType<HoleSegment>())
                        CollectFieldDependencies(segment.Expression, refs, bindings);
                    break;
            }
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
                ResolveStateReference(stateName, stateTargetSlot.NameSpan);
            }

            // Resolve event target slots (the event name reference itself)
            if (eventTargetSlot?.EventName is { } eventRefName)
            {
                ResolveEventReference(eventRefName, eventTargetSlot.NameSpan);
            }

            // Resolve field target slots (for access mode, omit declarations)
            var fieldTargetSlot = construct.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget);
            if (fieldTargetSlot?.FieldName is { } fieldName)
            {
                ResolveFieldReference(fieldName, fieldTargetSlot.NameSpan, null);
                foreach (var (additionalFieldName, additionalFieldSpan) in fieldTargetSlot.AdditionalFields)
                {
                    ResolveFieldReference(additionalFieldName, additionalFieldSpan, null);
                }
            }

            // Resolve expressions in various slots
            ResolveExpressionSlots(construct, contextEvent);

            // Resolve outcome state references (transition State)
            var outcomeSlot = construct.GetSlot<OutcomeSlot>(ConstructSlotKind.Outcome);
            if (outcomeSlot?.Outcome is TransitionOutcome transition)
            {
                ResolveStateReference(transition.StateName, transition.StateSpan);
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
                            _references.Add(new SymbolReference(memberAccess.MemberSpan, memberAccess.MemberName, new ArgTarget(argOnEvent)));
                        }
                        else
                        {
                            _diagnostics.Add(Diagnostics.Create(
                                DiagnosticCode.UndeclaredArg,
                                memberAccess.Span,
                                memberAccess.MemberName,
                                targetEvent.Name));
                            _references.Add(new SymbolReference(memberAccess.MemberSpan, memberAccess.MemberName, new UnresolvedTarget(memberAccess.MemberName, SymbolCategory.Any)));
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

                case InterpolatedTypedConstantExpression interpTyped:
                    foreach (var segment in interpTyped.Segments)
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
            if (IsStateWildcardKeyword(name))
            {
                return;
            }

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

        private static bool IsStateWildcardKeyword(string name) =>
            Tokens.Keywords.TryGetValue(name, out var keywordKind)
            && Tokens.GetMeta(keywordKind).IsStateWildcard;

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
            if (IsFieldBroadcastKeyword(name))
            {
                return;
            }

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

        private static bool IsFieldBroadcastKeyword(string name) =>
            Tokens.Keywords.TryGetValue(name, out var keywordKind)
            && Tokens.GetMeta(keywordKind).IsFieldBroadcast;

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
