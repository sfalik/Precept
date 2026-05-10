using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using NodaTime;
using NodaTime.Text;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Type checker — resolves names, types, expressions, and structural constraints from the
/// parsed <see cref="ConstructManifest"/> + <see cref="SymbolTable"/> and produces a
/// <see cref="SemanticIndex"/>.
/// </summary>
/// <remarks>
/// Pipeline stages: PopulateFields → PopulateStates → PopulateEvents →
/// ResolveFieldExpressions → PopulateTransitionRows → PopulateEventHandlers →
/// PopulateRules → PopulateEnsures → PopulateAccessModes → PopulateStateHooks →
/// PopulateEditDeclarations → ValidateModifiers → ValidateStructural →
/// ValidateCIEnforcement → BuildSemanticIndex (final assembly with D26 global assert).
/// </remarks>
internal static partial class TypeChecker
{
    private static readonly FrozenSet<string> CountQualifierUnitCodes = new[]
    {
        "1",
        "%",
        "[ppm]",
        "[ppb]",
        "[ppth]",
        "[pptr]",
        "each",
        "[iU]",
        "[arb'U]",
        "[USP'U]",
        "[CFU]",
        "[pH]",
        "dB",
    }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> NonCountDimensionlessUnitCodes = new[]
    {
        "rad",
        "deg",
        "'",
        "''",
        "gon",
        "sr",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Entry point: type-check <paramref name="manifest"/> using pre-resolved
    /// <paramref name="symbols"/> and return a <see cref="SemanticIndex"/>.
    /// </summary>
    internal static SemanticIndex Check(ConstructManifest manifest, SymbolTable symbols)
    {
        var ctx = new CheckContext();

        // Pass 1: populate typed symbols from SymbolTable declarations
        PopulateFields(symbols, ctx);
        PopulateStates(symbols, ctx);
        PopulateEvents(symbols, ctx);

        // Pass 1b: resolve field default/computed expressions (B1)
        ResolveFieldExpressions(symbols, ctx);

        // Pass 2: normalize transition rows and event handlers (Slice 5)
        PopulateTransitionRows(manifest, ctx);
        PopulateEventHandlers(manifest, ctx);
        PopulateRules(manifest, ctx);

        // Pass 2b: normalize ensures, access modes, state hooks, edit declarations (B2)
        PopulateEnsures(manifest, ctx);
        PopulateAccessModes(manifest, ctx);
        PopulateStateHooks(manifest, ctx);
        PopulateEditDeclarations(manifest, ctx);

        // Modifier validation (Slice 7) — depends only on Pass 1 symbols
        ValidateModifiers(ctx);

        // Structural validation (Slice 6) — runs after Pass 2; reads ComputedDeps
        // (populated during expression resolution) for cycle detection.
        ValidateStructural(ctx);

        // CI enforcement (Slice 8) — runs after all expression resolution;
        // walks resolved expression trees for ~string consistency violations.
        ValidateCIEnforcement(ctx);

        return BuildSemanticIndex(ctx);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 1 — typed symbol population (Slice 1)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolve a <see cref="ParsedTypeReference"/> to a <see cref="TypeKind"/> using the
    /// Types catalog. For collections, also resolves the inner element type and key type.
    /// Returns <see cref="TypeKind.Error"/> for <see cref="MissingTypeReference"/>.
    /// </summary>
    private static (TypeKind Type, TypeKind? ElementType, TypeKind? KeyType) ResolveTypeKind(ParsedTypeReference typeRef) =>
        typeRef switch
        {
            SimpleTypeReference simple       => (simple.Type.Kind, null, null),
            QualifiedTypeReference qualified => ResolveTypeKind(qualified.InnerType),
            CollectionTypeReference coll => (
                ResolveCollectionTypeKind(coll),
                ResolveTypeKind(coll.ElementType).Type,
                coll.KeyType is not null ? ResolveTypeKind(coll.KeyType).Type : null),
            ChoiceTypeReference choice => (choice.Type.Kind, null, null),
            CITypeReference ci => (ci.Type.Kind, null, null),
            MissingTypeReference => (TypeKind.Error, null, null),
            _ => (TypeKind.Error, null, null),
        };

    private static TypeKind ResolveCollectionTypeKind(CollectionTypeReference collection) =>
        collection switch
        {
            { CollectionType.Kind: TypeKind.Queue, KeyType: not null } => TypeKind.QueueBy,
            { CollectionType.Kind: TypeKind.Log, KeyType: not null } => TypeKind.LogBy,
            _ => collection.CollectionType.Kind,
        };

    /// <summary>
    /// Extracts <see cref="DeclaredQualifierMeta"/> values from a <see cref="QualifiedTypeReference"/>,
    /// validating each qualifier value against its catalog and emitting diagnostics for invalid values.
    /// Enforces <c>in</c>/<c>of</c> mutual exclusion for types with <see cref="QualifierShape.InOfExclusive"/>.
    /// Returns empty if <paramref name="typeRef"/> is not a <see cref="QualifiedTypeReference"/>.
    /// </summary>
    private static ImmutableArray<DeclaredQualifierMeta> ExtractQualifiers(
        ParsedTypeReference typeRef, CheckContext ctx)
    {
        if (typeRef is not QualifiedTypeReference qualified)
            return ImmutableArray<DeclaredQualifierMeta>.Empty;

        // Enforce in/of mutual exclusion for types that declare it
        if (qualified.InnerType is SimpleTypeReference simpleInner &&
            simpleInner.Type.QualifierShape?.InOfExclusive == true)
        {
            bool hasIn = qualified.Qualifiers.Any(q => q.Preposition == TokenKind.In);
            bool hasOf = qualified.Qualifiers.Any(q => q.Preposition == TokenKind.Of);
            if (hasIn && hasOf)
                ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.MutuallyExclusiveQualifiers, qualified.Span));
        }

        var builder = ImmutableArray.CreateBuilder<DeclaredQualifierMeta>(qualified.Qualifiers.Length);
        foreach (var qualifier in qualified.Qualifiers)
        {
            DeclaredQualifierMeta? meta = qualifier.Axis switch
            {
                QualifierAxis.Currency     => MapCurrencyQualifier(qualifier, ctx),
                QualifierAxis.Unit         => MapUnitQualifier(qualifier, ctx),
                QualifierAxis.Dimension    => MapDimensionQualifier(qualifier, ctx),
                QualifierAxis.FromCurrency => MapFromCurrencyQualifier(qualifier, ctx),
                QualifierAxis.ToCurrency   => MapToCurrencyQualifier(qualifier, ctx),
                _                          => null,
            };
            if (meta is not null)
                builder.Add(meta);
        }
        return builder.ToImmutable();
    }

    private static DeclaredQualifierMeta.Currency MapCurrencyQualifier(ParsedQualifier q, CheckContext ctx)
    {
        if (!CurrencyCatalog.All.ContainsKey(q.Value))
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidCurrencyCode, q.ValueSpan, q.Value));
        return new DeclaredQualifierMeta.Currency(q.Value);
    }

    private static DeclaredQualifierMeta.Unit MapUnitQualifier(ParsedQualifier q, CheckContext ctx)
    {
        if (CountQualifierUnitCodes.Contains(q.Value))
            return new DeclaredQualifierMeta.Unit(q.Value, "count");

        var result = UcumParser.Parse(q.Value);
        if (!result.IsValid)
        {
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidUnitString, q.ValueSpan, q.Value));
            return new DeclaredQualifierMeta.Unit(q.Value, "");
        }

        return new DeclaredQualifierMeta.Unit(q.Value, DeriveUnitDimensionName(result.Unit!));
    }

    private static string DeriveUnitDimensionName(UcumParsedUnit unit)
    {
        if (!unit.Vector.IsDimensionless)
            return unit.PreferredDimensionAlias ?? "";

        if (CountQualifierUnitCodes.Contains(unit.CanonicalCode))
            return "count";

        if (IsNonCountDimensionlessUnit(unit))
            return "";

        return DimensionCatalog.TryGetAlias(unit.Vector, out var alias) && alias is not null
            ? alias.Name
            : unit.PreferredDimensionAlias ?? "";
    }

    private static bool IsNonCountDimensionlessUnit(UcumParsedUnit unit) =>
        NonCountDimensionlessUnitCodes.Contains(unit.CanonicalCode)
        || unit.UsedAtoms.Any(atom => NonCountDimensionlessUnitCodes.Contains(atom.Code));

    private static DeclaredQualifierMeta.Dimension MapDimensionQualifier(ParsedQualifier q, CheckContext ctx)
    {
        if (!DimensionCatalog.All.ContainsKey(q.Value))
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidDimensionString, q.ValueSpan, q.Value));
        return new DeclaredQualifierMeta.Dimension(q.Value);
    }

    private static DeclaredQualifierMeta.FromCurrency MapFromCurrencyQualifier(ParsedQualifier q, CheckContext ctx)
    {
        if (!CurrencyCatalog.All.ContainsKey(q.Value))
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidCurrencyCode, q.ValueSpan, q.Value));
        return new DeclaredQualifierMeta.FromCurrency(q.Value);
    }

    private static DeclaredQualifierMeta.ToCurrency MapToCurrencyQualifier(ParsedQualifier q, CheckContext ctx)
    {
        if (!CurrencyCatalog.All.ContainsKey(q.Value))
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.InvalidCurrencyCode, q.ValueSpan, q.Value));
        return new DeclaredQualifierMeta.ToCurrency(q.Value);
    }

    /// <summary>
    /// Populate <see cref="CheckContext.Fields"/> from <see cref="SymbolTable.Fields"/>.
    /// Resolves <see cref="TypeKind"/>, extracts declared modifiers, computes implied modifiers
    /// from the Types catalog (D3), and builds <see cref="TypedField"/> records.
    /// </summary>
    private static void PopulateFields(SymbolTable symbols, CheckContext ctx)
    {
        foreach (var declared in symbols.Fields)
        {
            var (resolvedType, elementType, keyType) = ResolveTypeKind(declared.Type);

            // Declared modifiers: extract ModifierKind values from ParsedModifier list
            var modifiers = declared.Modifiers
                .Select(m => m.Kind)
                .ToImmutableArray();

            // Implied modifiers from the Types catalog (D3: catalog-driven, no inline logic)
            var impliedModifiers = resolvedType != TypeKind.Error
                ? Types.GetMeta(resolvedType).ImpliedModifiers.ToImmutableArray()
                : ImmutableArray<ModifierKind>.Empty;

            bool isOptional = modifiers.Contains(ModifierKind.Optional);
            bool isWritable = modifiers.Contains(ModifierKind.Writable);

            var typedField = new TypedField(
                Name: declared.Name,
                ResolvedType: resolvedType,
                ElementType: elementType,
                KeyType: keyType,
                Modifiers: modifiers,
                ImpliedModifiers: impliedModifiers,
                DefaultExpression: null,   // Slice 2+
                ComputedExpression: null,  // Slice 2+
                Qualifier: null,           // Slice 2+
                IsComputed: declared.IsComputed,
                IsOptional: isOptional,
                IsWritable: isWritable,
                Presence: isOptional
                    ? new DeclaredPresenceMeta.Optional()
                    : new DeclaredPresenceMeta.Guaranteed(),
                DeclaredQualifiers: ExtractQualifiers(declared.Type, ctx),
                NameSpan: declared.NameSpan,
                Syntax: declared.Syntax);

            ctx.Fields.Add(typedField);
            ctx.FieldLookup[declared.Name] = typedField;

            // CI tracking (Slice 8): record ~string fields and ~string-element collections
            if (declared.Type is CITypeReference)
                ctx.CIFields.Add(declared.Name);
            else if (declared.Type is CollectionTypeReference { ElementType: CITypeReference })
                ctx.CIElementCollections.Add(declared.Name);

            // Choice domain validation (Slice 6): empty domain and duplicate values
            if (declared.Type is ChoiceTypeReference choiceRef)
            {
                if (choiceRef.Domain.IsEmpty)
                {
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.EmptyChoice, declared.NameSpan));
                }
                else
                {
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var value in choiceRef.Domain)
                    {
                        if (!seen.Add(value))
                        {
                            ctx.Diagnostics.Add(
                                Diagnostics.Create(DiagnosticCode.DuplicateChoiceValue, declared.NameSpan, value));
                        }
                    }
                }
            }

            // Diagnostic for unknown type (MissingTypeReference → TypeKind.Error).
            // The parser already emits a diagnostic for the missing type token;
            // the checker emits TypeMismatch to surface the field-level impact.
            if (resolvedType == TypeKind.Error)
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.TypeMismatch, declared.NameSpan, "type", declared.Name));
            }
        }
    }

    /// <summary>
    /// Populate <see cref="CheckContext.States"/> from <see cref="SymbolTable.States"/>.
    /// Resolves modifier flags and validates initial/terminal state counts (D7).
    /// </summary>
    private static void PopulateStates(SymbolTable symbols, CheckContext ctx)
    {
        string? firstInitialStateName = null;
        SourceSpan firstInitialSpan = default;

        foreach (var declared in symbols.States)
        {
            var typedState = new TypedState(
                Name: declared.Name,
                Modifiers: declared.Modifiers,
                NameSpan: declared.NameSpan,
                Syntax: declared.Syntax);

            ctx.States.Add(typedState);
            ctx.StateLookup[declared.Name] = typedState;

            // Track initial state for count validation
            if (declared.Modifiers.Contains(ModifierKind.InitialState))
            {
                if (firstInitialStateName is null)
                {
                    firstInitialStateName = declared.Name;
                    firstInitialSpan = declared.NameSpan;
                }
                else
                {
                    // D7: Multiple initial states → diagnostic
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.MultipleInitialStates, declared.NameSpan,
                            firstInitialStateName, declared.Name));
                }
            }
        }

        // D7: Zero initial states on a stateful precept → diagnostic
        // A stateless precept (no states at all) is valid; only fire when states exist but none is initial.
        if (symbols.States.Length > 0 && firstInitialStateName is null)
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(DiagnosticCode.NoInitialState, symbols.States[0].NameSpan));
        }
    }

    /// <summary>
    /// Populate <see cref="CheckContext.Events"/> from <see cref="SymbolTable.Events"/>.
    /// Resolves event argument types via the Types catalog.
    /// </summary>
    private static void PopulateEvents(SymbolTable symbols, CheckContext ctx)
    {
        foreach (var declared in symbols.Events)
        {
            var argsBuilder = ImmutableArray.CreateBuilder<TypedArg>(declared.Args.Length);
            foreach (var arg in declared.Args)
            {
                var (resolvedType, _, _) = ResolveTypeKind(arg.Type);
                argsBuilder.Add(new TypedArg(
                    Name: arg.Name,
                    EventName: arg.EventName,
                    ResolvedType: resolvedType,
                    ElementType: null, // event arg element types deferred until arg type parsing is richer
                    Modifiers: arg.Modifiers,
                    DefaultExpression: null, // Slice 2+
                    IsOptional: arg.Modifiers.Contains(ModifierKind.Optional),
                    Presence: arg.Modifiers.Contains(ModifierKind.Optional)
                        ? new DeclaredPresenceMeta.Optional()
                        : new DeclaredPresenceMeta.Guaranteed(),
                    DeclaredQualifiers: ExtractQualifiers(arg.Type, ctx),
                    Span: arg.NameSpan));
            }

            var typedEvent = new TypedEvent(
                Name: declared.Name,
                Args: argsBuilder.ToImmutable(),
                IsInitial: declared.IsInitial,
                NameSpan: declared.NameSpan,
                Syntax: declared.Syntax);

            ctx.Events.Add(typedEvent);
            ctx.EventLookup[declared.Name] = typedEvent;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 1b — field expression resolution (B1)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolve default and computed expressions on fields. Populates
    /// <see cref="TypedField.DefaultExpression"/>, <see cref="TypedField.ComputedExpression"/>,
    /// and <see cref="CheckContext.ComputedDeps"/> entries. Default expressions remain
    /// prior-field only; computed expressions resolve against all declared fields.
    /// </summary>
    private static void ResolveFieldExpressions(SymbolTable symbols, CheckContext ctx)
    {
        for (int i = 0; i < ctx.Fields.Count; i++)
        {
            var typedField = ctx.Fields[i];
            var declared = symbols.Fields[i];

            // —— Default expression (from modifier with HasValue) ——
            var defaultMod = declared.Modifiers.FirstOrDefault(
                m => m.Kind == ModifierKind.Default);
            if (defaultMod?.Value is not null and not MissingExpression)
            {
                ctx.CurrentScope = FieldScopeMode.PriorFieldsOnly;
                ctx.CurrentFieldIndex = i;
                var resolved = Resolve(defaultMod.Value, ctx, typedField.ResolvedType);
                ctx.Fields[i] = ctx.Fields[i] with { DefaultExpression = resolved };
                ctx.FieldLookup[typedField.Name] = ctx.Fields[i];
                ctx.CurrentScope = FieldScopeMode.AllFields;
                ctx.CurrentFieldIndex = -1;
            }

            // —— Computed expression (from ComputeExpressionSlot on the field's Syntax) ——
            var computeSlot = declared.Syntax.GetSlot<ComputeExpressionSlot>(
                ConstructSlotKind.ComputeExpression);
            if (computeSlot is not null && computeSlot.Expression is not MissingExpression)
            {
                ctx.CurrentScope = FieldScopeMode.AllFields;
                ctx.CurrentFieldIndex = i;
                var resolved = Resolve(computeSlot.Expression, ctx, typedField.ResolvedType);
                ctx.Fields[i] = ctx.Fields[i] with { ComputedExpression = resolved };
                ctx.FieldLookup[typedField.Name] = ctx.Fields[i];
                ctx.CurrentScope = FieldScopeMode.AllFields;
                ctx.CurrentFieldIndex = -1;

                // Extract ComputedFieldDep entries by walking TypedFieldRef nodes
                var deps = new List<string>();
                CollectFieldRefs(resolved, deps);
                if (deps.Count > 0)
                {
                    ctx.ComputedDeps.Add(new ComputedFieldDep(
                        FieldName: typedField.Name,
                        DependsOn: deps.Distinct().ToImmutableArray()));
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Test entry points (InternalsVisibleTo — Precept.Tests)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a <see cref="CheckContext"/> with Pass 1 symbols populated.
    /// Used by tests to build a resolution context without going through Check().
    /// </summary>
    internal static CheckContext CreateContext(ConstructManifest manifest, SymbolTable symbols)
    {
        var ctx = new CheckContext();
        PopulateFields(symbols, ctx);
        PopulateStates(symbols, ctx);
        PopulateEvents(symbols, ctx);
        return ctx;
    }

    /// <summary>
    /// Resolves a single <see cref="ParsedExpression"/> in the given context.
    /// Thin wrapper over the private <see cref="Resolve"/> for test access.
    /// </summary>
    internal static TypedExpression ResolveExpression(ParsedExpression expr, CheckContext ctx, TypeKind? expectedType = null) =>
        Resolve(expr, ctx, expectedType);

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
            var row = NormalizeTransitionRow(construct, ctx);
            ctx.TransitionRows.Add(row);
        }

        // D26: if any TypedErrorExpression in transition rows → at least one Error diagnostic must exist
        Debug.Assert(
            !ctx.TransitionRows.Any(r => ContainsErrorExpression(r)) ||
            ctx.Diagnostics.Any(d => d.Severity == Severity.Error),
            "D26: TypedErrorExpression present in transition rows but no Error-severity diagnostic emitted.");
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
        Debug.Assert(
            !ctx.EventHandlers.Any(h => h.Actions.Any(a => a is TypedInputAction ia && ContainsErrorExpressionInAction(ia))) ||
            ctx.Diagnostics.Any(d => d.Severity == Severity.Error),
            "D26: TypedErrorExpression present in event handlers but no Error-severity diagnostic emitted.");
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

            ctx.Rules.Add(new TypedRule(condition, guard, message, ImmutableArray<string>.Empty, construct));
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
                string? anchorState = null;
                if (stateSlot?.StateName is not null)
                {
                    if (HasKeywordTokenMeta(stateSlot.StateName, meta => meta.IsStateWildcard))
                    {
                        anchorState = null;
                    }
                    else if (ctx.StateLookup.TryGetValue(stateSlot.StateName, out var typedState))
                    {
                        anchorState = typedState.Name;
                        ctx.StateReferences.Add(new StateReference(typedState, stateSlot.Span));
                    }
                    else
                    {
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(DiagnosticCode.UndeclaredState, stateSlot.Span, stateSlot.StateName));
                    }
                }

                // Determine constraint kind from leading token (catalog-driven; fallback stays resident)
                var constraintKind = construct.LeadingTokenKind is { } leadingToken
                                     && Constraints.ByToken.TryGetValue(leadingToken, out var ck)
                    ? ck
                    : ConstraintKind.StateResident;

                var ensureSlot = construct.GetSlot<EnsureClauseSlot>(ConstructSlotKind.EnsureClause);
                if (ensureSlot is null) continue;

                ctx.CurrentScope = FieldScopeMode.AllFields;
                var condition = Resolve(ensureSlot.Expression, ctx);

                var becauseSlot = construct.GetSlot<BecauseClauseSlot>(ConstructSlotKind.BecauseClause);
                var message = becauseSlot is not null
                    ? new TypedLiteral(TypeKind.String, becauseSlot.Message, becauseSlot.Span)
                    : new TypedLiteral(TypeKind.String, "", construct.Span);

                ctx.Ensures.Add(new TypedEnsure(
                    Kind: constraintKind,
                    AnchorState: anchorState,
                    AnchorEvent: null,
                    Condition: condition,
                    Guard: null,
                    Message: message,
                    SemanticSubjects: ImmutableArray<string>.Empty,
                    Syntax: construct));
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
                        ctx.EventReferences.Add(new EventReference(evTyped, eventSlot.Span));
                    }
                    else
                    {
                        anchorEvent = eventSlot.EventName;
                        ctx.Diagnostics.Add(
                            Diagnostics.Create(DiagnosticCode.UndeclaredEvent, eventSlot.Span, eventSlot.EventName));
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

                    var becauseSlot = construct.GetSlot<BecauseClauseSlot>(ConstructSlotKind.BecauseClause);
                    var message = becauseSlot is not null
                        ? new TypedLiteral(TypeKind.String, becauseSlot.Message, becauseSlot.Span)
                        : new TypedLiteral(TypeKind.String, "", construct.Span);

                    ctx.Ensures.Add(new TypedEnsure(
                        Kind: ConstraintKind.EventPrecondition,
                        AnchorState: null,
                        AnchorEvent: anchorEvent,
                        Condition: condition,
                        Guard: null,
                        Message: message,
                        SemanticSubjects: ImmutableArray<string>.Empty,
                        Syntax: construct));
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

    private static void PopulateAccessModes(ConstructManifest manifest, CheckContext ctx)
    {
        if (!manifest.ByKind.Contains(ConstructKind.AccessMode))
            return;

        foreach (var construct in manifest.ByKind[ConstructKind.AccessMode])
        {
            // —— State reference ——
            var stateSlot = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
            string stateName = "";
            if (stateSlot?.StateName is not null)
            {
                if (HasKeywordTokenMeta(stateSlot.StateName, meta => meta.IsStateWildcard))
                {
                    stateName = stateSlot.StateName;
                }
                else if (ctx.StateLookup.TryGetValue(stateSlot.StateName, out var typedState))
                {
                    stateName = typedState.Name;
                    ctx.StateReferences.Add(new StateReference(typedState, stateSlot.Span));
                }
                else
                {
                    stateName = stateSlot.StateName;
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.UndeclaredState, stateSlot.Span, stateSlot.StateName));
                }
            }

            // —— Field reference ——
            var fieldSlot = construct.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget);
            string fieldName = "";
            if (fieldSlot?.FieldName is not null)
            {
                if (HasKeywordTokenMeta(fieldSlot.FieldName, meta => meta.IsFieldBroadcast))
                {
                    fieldName = fieldSlot.FieldName;
                }
                else if (ctx.FieldLookup.TryGetValue(fieldSlot.FieldName, out var typedField))
                {
                    fieldName = typedField.Name;
                    ctx.FieldReferences.Add(new FieldReference(typedField, fieldSlot.Span));
                }
                else
                {
                    fieldName = fieldSlot.FieldName;
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.UndeclaredField, fieldSlot.Span, fieldSlot.FieldName));
                }
            }

            // —— Mode (readonly / editable / omit → modifier kind) ——
            var modeSlot = construct.GetSlot<AccessModeSlot>(ConstructSlotKind.AccessModeKeyword);
            ModifierKind mode = modeSlot?.AccessMode is { } accessToken
                                && Modifiers.ByAccessToken.TryGetValue(accessToken, out var accessMeta)
                ? accessMeta.Kind
                : ModifierKind.Read;

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

            ctx.AccessModes.Add(new TypedAccessMode(
                StateName: stateName,
                FieldName: fieldName,
                Mode: mode,
                Guard: guard,
                Syntax: construct));
        }
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
            string stateName = "";
            if (stateSlot?.StateName is not null)
            {
                if (HasKeywordTokenMeta(stateSlot.StateName, meta => meta.IsStateWildcard))
                {
                    stateName = stateSlot.StateName;
                }
                else if (ctx.StateLookup.TryGetValue(stateSlot.StateName, out var typedState))
                {
                    stateName = typedState.Name;
                    ctx.StateReferences.Add(new StateReference(typedState, stateSlot.Span));
                }
                else
                {
                    stateName = stateSlot.StateName;
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.UndeclaredState, stateSlot.Span, stateSlot.StateName));
                }
            }

            // —— Hook scope from leading token (catalog-driven; fallback stays OnEntry) ——
            var scope = construct.LeadingTokenKind is { } leadingToken
                        && Modifiers.ByAnchorToken.TryGetValue(leadingToken, out var anchorMeta)
                ? anchorMeta.Scope
                : AnchorScope.OnEntry;

            // —— Guard (state hooks may support guards in future; currently none in slot list) ——
            TypedExpression? guard = null;

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

            ctx.StateHooks.Add(new TypedStateHook(
                Scope: scope,
                StateName: stateName,
                Guard: guard,
                Actions: actions,
                Syntax: construct));
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
                    fields = [fieldName];
                    if (ctx.FieldLookup.TryGetValue(fieldName, out var typedField))
                        ctx.FieldReferences.Add(new FieldReference(typedField, fieldSlot.Span));
                }
            }

            ctx.EditDeclarations.Add(new TypedEditDeclaration(
                EditableFields: fields,
                IsEditAll: isEditAll,
                Syntax: construct));
        }
    }

    /// <summary>Normalize a transition row construct into a <see cref="TypedTransitionRow"/>.</summary>
    private static TypedTransitionRow NormalizeTransitionRow(ParsedConstruct construct, CheckContext ctx)
    {
        // —— FromState resolution ——
        var stateTargetSlot = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
        string? fromState = null;
        if (stateTargetSlot is not null && stateTargetSlot.StateName is not null)
        {
            if (HasKeywordTokenMeta(stateTargetSlot.StateName, meta => meta.IsStateWildcard))
            {
                fromState = null;
            }
            else if (ctx.StateLookup.TryGetValue(stateTargetSlot.StateName, out var fromTypedState))
            {
                fromState = fromTypedState.Name;
                ctx.StateReferences.Add(new StateReference(fromTypedState, stateTargetSlot.Span));
            }
            else
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.UndeclaredState, stateTargetSlot.Span, stateTargetSlot.StateName));
            }
        }
        // StateName == null or wildcard token text → any-state wildcard (D10): FromState stays null, no error

        // —— Event resolution ——
        var eventTargetSlot = construct.GetRequiredSlot<EventTargetSlot>(ConstructSlotKind.EventTarget);
        string eventName = "";
        TypedEvent? resolvedEvent = null;
        if (eventTargetSlot.EventName is not null)
        {
            if (ctx.EventLookup.TryGetValue(eventTargetSlot.EventName, out var evTyped))
            {
                eventName = evTyped.Name;
                resolvedEvent = evTyped;
                ctx.EventReferences.Add(new EventReference(evTyped, eventTargetSlot.Span));
            }
            else
            {
                eventName = eventTargetSlot.EventName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.UndeclaredEvent, eventTargetSlot.Span, eventTargetSlot.EventName));
            }
        }

        // —— Set event args scope ——
        IReadOnlyDictionary<string, TypedArg>? previousArgs = ctx.CurrentEventArgs;
        if (resolvedEvent is not null)
        {
            ctx.CurrentEventArgs = resolvedEvent.Args
                .ToFrozenDictionary(a => a.Name);
        }

        try
        {
            // —— Guard resolution ——
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

            // —— Action chain resolution ——
            var actionChainSlot = construct.GetSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
            var actions = ImmutableArray<TypedAction>.Empty;
            if (actionChainSlot is not null)
            {
                var builder = ImmutableArray.CreateBuilder<TypedAction>(actionChainSlot.Actions.Length);
                foreach (var parsedAction in actionChainSlot.Actions)
                    builder.Add(ResolveAction(parsedAction, ctx));
                actions = builder.MoveToImmutable();
            }

            // —— Outcome resolution ——
            var outcomeSlot = construct.GetSlot<OutcomeSlot>(ConstructSlotKind.Outcome);
            TransitionRowOutcome outcome = TransitionRowOutcome.NoTransition;
            string? targetState = null;
            string? rejectReason = null;

            if (outcomeSlot is not null)
            {
                switch (outcomeSlot.Outcome)
                {
                    case TransitionOutcome trans:
                        outcome = TransitionRowOutcome.Transition;
                        if (ctx.StateLookup.TryGetValue(trans.StateName, out var toTypedState))
                        {
                            targetState = toTypedState.Name;
                            ctx.StateReferences.Add(new StateReference(toTypedState, trans.Span));
                        }
                        else
                        {
                            ctx.Diagnostics.Add(
                                Diagnostics.Create(DiagnosticCode.UndeclaredState, trans.Span, trans.StateName));
                        }
                        break;
                    case NoTransitionOutcome:
                        outcome = TransitionRowOutcome.NoTransition;
                        break;
                    case RejectOutcome reject:
                        outcome = TransitionRowOutcome.Reject;
                        rejectReason = reject.Reason;
                        break;
                    case MalformedOutcome:
                        outcome = TransitionRowOutcome.NoTransition;
                        break;
                }
            }

            return new TypedTransitionRow(
                FromState: fromState,
                EventName: eventName,
                TargetState: targetState,
                Guard: guard,
                Actions: actions,
                Outcome: outcome,
                RejectReason: rejectReason,
                ResultQualifier: null,
                Syntax: construct);
        }
        finally
        {
            ctx.CurrentEventArgs = previousArgs;
        }
    }

    /// <summary>Normalize an event handler construct into a <see cref="TypedEventHandler"/>.</summary>
    private static TypedEventHandler NormalizeEventHandler(ParsedConstruct construct, CheckContext ctx)
    {
        // —— Event resolution ——
        var eventTargetSlot = construct.GetRequiredSlot<EventTargetSlot>(ConstructSlotKind.EventTarget);
        string eventName = "";
        TypedEvent? resolvedEvent = null;
        if (eventTargetSlot.EventName is not null)
        {
            if (ctx.EventLookup.TryGetValue(eventTargetSlot.EventName, out var evTyped))
            {
                eventName = evTyped.Name;
                resolvedEvent = evTyped;
                ctx.EventReferences.Add(new EventReference(evTyped, eventTargetSlot.Span));
            }
            else
            {
                eventName = eventTargetSlot.EventName;
                ctx.Diagnostics.Add(
                    Diagnostics.Create(DiagnosticCode.UndeclaredEvent, eventTargetSlot.Span, eventTargetSlot.EventName));
            }
        }

        // —— Set event args scope ——
        IReadOnlyDictionary<string, TypedArg>? previousArgs = ctx.CurrentEventArgs;
        if (resolvedEvent is not null)
        {
            ctx.CurrentEventArgs = resolvedEvent.Args
                .ToFrozenDictionary(a => a.Name);
        }

        try
        {
            // —— Action chain resolution ——
            var actionChainSlot = construct.GetSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
            var actions = ImmutableArray<TypedAction>.Empty;
            if (actionChainSlot is not null)
            {
                var builder = ImmutableArray.CreateBuilder<TypedAction>(actionChainSlot.Actions.Length);
                foreach (var parsedAction in actionChainSlot.Actions)
                    builder.Add(ResolveAction(parsedAction, ctx));
                actions = builder.MoveToImmutable();
            }

            return new TypedEventHandler(
                EventName: eventName,
                Actions: actions,
                Syntax: construct);
        }
        finally
        {
            ctx.CurrentEventArgs = previousArgs;
        }
    }

    /// <summary>Check whether a transition row contains any <see cref="TypedErrorExpression"/>.</summary>
    private static bool ContainsErrorExpression(TypedTransitionRow row)
    {
        if (row.Guard is TypedErrorExpression) return true;
        foreach (var action in row.Actions)
        {
            if (action is TypedInputAction ia)
            {
                if (ia.InputExpression is TypedErrorExpression) return true;
                if (ia.SecondaryExpression is TypedErrorExpression) return true;
            }
        }
        return false;
    }

    /// <summary>Check whether a typed input action contains a <see cref="TypedErrorExpression"/>.</summary>
    private static bool ContainsErrorExpressionInAction(TypedInputAction action)
    {
        if (action.InputExpression is TypedErrorExpression) return true;
        if (action.SecondaryExpression is TypedErrorExpression) return true;
        return false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Final assembly (Slice 10)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Transform <see cref="CheckContext"/> mutable accumulators into the immutable <see cref="SemanticIndex"/>.
    /// Derives frozen-dictionary secondary indexes from primary arrays (D4).
    /// Enforces D26 global invariant: any <see cref="TypedErrorExpression"/> in the index
    /// requires ≥1 <see cref="Severity.Error"/> diagnostic.
    /// </summary>
    private static SemanticIndex BuildSemanticIndex(CheckContext ctx)
    {
        var fields = ctx.Fields.ToImmutableArray();
        var states = ctx.States.ToImmutableArray();
        var events = ctx.Events.ToImmutableArray();
        var ensures = ctx.Ensures.ToImmutableArray();

        var index = new SemanticIndex(
            Fields:           fields,
            States:           states,
            Events:           events,
            FieldsByName:     fields.ToFrozenDictionary(f => f.Name),
            StatesByName:     states.ToFrozenDictionary(s => s.Name),
            EventsByName:     events.ToFrozenDictionary(e => e.Name),
            TransitionRows:   ctx.TransitionRows.ToImmutableArray(),
            Rules:            ctx.Rules.ToImmutableArray(),
            Ensures:          ensures,
            AccessModes:      ctx.AccessModes.ToImmutableArray(),
            StateHooks:       ctx.StateHooks.ToImmutableArray(),
            EventHandlers:    ctx.EventHandlers.ToImmutableArray(),
            EditDeclarations: ctx.EditDeclarations.ToImmutableArray(),
            EnsuresByState:   ensures
                                  .Where(e => e.AnchorState is not null)
                                  .GroupBy(e => e.AnchorState!)
                                  .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray()),
            ComputedDeps:     ctx.ComputedDeps.ToImmutableArray(),
            ConstraintRefs:   ctx.ConstraintRefs.ToImmutableArray(),
            FieldReferences:  ctx.FieldReferences.ToImmutableArray(),
            StateReferences:  ctx.StateReferences.ToImmutableArray(),
            EventReferences:  ctx.EventReferences.ToImmutableArray(),
            ArgReferences:    ctx.ArgReferences.ToImmutableArray(),
            Diagnostics:      ctx.Diagnostics.ToImmutableArray());

        // D26: If any TypedErrorExpression exists, at least one Error diagnostic must be present
        Debug.Assert(
            !ContainsAnyErrorExpression(index) ||
            index.Diagnostics.Any(d => d.Severity == Severity.Error),
            "D26 violated: TypedErrorExpression present but no Error diagnostic in SemanticIndex");

        return index;
    }

    /// <summary>
    /// Walk all expression-bearing sites in the <see cref="SemanticIndex"/> and return
    /// <c>true</c> if any <see cref="TypedErrorExpression"/> is found. Used by D26.
    /// </summary>
    private static bool ContainsAnyErrorExpression(SemanticIndex index)
    {
        // Fields: default + computed expressions
        foreach (var f in index.Fields)
        {
            if (ContainsError(f.DefaultExpression) || ContainsError(f.ComputedExpression))
                return true;
        }

        // Events: arg default expressions
        foreach (var ev in index.Events)
        {
            foreach (var arg in ev.Args)
            {
                if (ContainsError(arg.DefaultExpression))
                    return true;
            }
        }

        // Transition rows: guard + actions
        foreach (var row in index.TransitionRows)
        {
            if (ContainsError(row.Guard))
                return true;
            if (ActionsContainError(row.Actions))
                return true;
        }

        // Rules: condition + guard + message
        foreach (var rule in index.Rules)
        {
            if (ContainsError(rule.Condition) || ContainsError(rule.Guard) || ContainsError(rule.Message))
                return true;
        }

        // Ensures: condition + guard + message
        foreach (var ensure in index.Ensures)
        {
            if (ContainsError(ensure.Condition) || ContainsError(ensure.Guard) || ContainsError(ensure.Message))
                return true;
        }

        // Access modes: guard
        foreach (var am in index.AccessModes)
        {
            if (ContainsError(am.Guard))
                return true;
        }

        // State hooks: guard + actions
        foreach (var hook in index.StateHooks)
        {
            if (ContainsError(hook.Guard))
                return true;
            if (ActionsContainError(hook.Actions))
                return true;
        }

        // Event handlers: actions
        foreach (var handler in index.EventHandlers)
        {
            if (ActionsContainError(handler.Actions))
                return true;
        }

        return false;
    }

    /// <summary>Returns true if any action in the list contains a <see cref="TypedErrorExpression"/>.</summary>
    private static bool ActionsContainError(ImmutableArray<TypedAction> actions)
    {
        foreach (var action in actions)
        {
            if (action is TypedInputAction input)
            {
                if (ContainsError(input.InputExpression) || ContainsError(input.SecondaryExpression))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Recursively check whether <paramref name="expr"/> is or contains a <see cref="TypedErrorExpression"/>.
    /// Returns false for null expressions.
    /// </summary>
    private static bool ContainsError(TypedExpression? expr) => expr switch
    {
        null => false,
        TypedErrorExpression => true,
        TypedBinaryOp bin => ContainsError(bin.Left) || ContainsError(bin.Right),
        TypedUnaryOp un => ContainsError(un.Operand),
        TypedFunctionCall fn => fn.Arguments.Any(ContainsError),
        TypedMemberAccess ma => ContainsError(ma.Object),
        TypedConditional cond => ContainsError(cond.Condition) || ContainsError(cond.ThenBranch) || ContainsError(cond.ElseBranch),
        TypedQuantifier q => ContainsError(q.Collection) || ContainsError(q.Predicate),
        TypedInterpolatedString interp => interp.Segments.Any(s => s is TypedHoleSegment hole && ContainsError(hole.Expression)),
        TypedListLiteral list => list.Elements.Any(ContainsError),
        TypedPostfixOp post => ContainsError(post.Operand),
        _ => false, // TypedFieldRef, TypedArgRef, TypedLiteral, TypedTypedConstant — leaf nodes
    };
}
