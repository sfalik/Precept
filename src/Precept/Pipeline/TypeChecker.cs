using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Type checker — resolves names, types, expressions, and structural constraints from the
/// parsed <see cref="ConstructManifest"/> + <see cref="SymbolTable"/> and produces a
/// <see cref="SemanticIndex"/>.
/// </summary>
/// <remarks>
/// Implementation is staged across Slices 1–10. Slice 1 (typed symbol population) is live.
/// Remaining private methods throw <see cref="NotImplementedException"/> until their owning slice lands.
/// </remarks>
internal static class TypeChecker
{
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

        // Pass 2 + final assembly deferred to Slices 2–10.
        // Return a partial SemanticIndex with the symbol tables populated.
        return BuildPartialSemanticIndex(ctx);
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
            SimpleTypeReference simple => (simple.Type.Kind, null, null),
            CollectionTypeReference coll => (
                coll.CollectionType.Kind,
                ResolveTypeKind(coll.ElementType).Type,
                coll.KeyType is not null ? ResolveTypeKind(coll.KeyType).Type : null),
            ChoiceTypeReference choice => (choice.Type.Kind, null, null),
            CITypeReference ci => (ci.Type.Kind, null, null),
            MissingTypeReference => (TypeKind.Error, null, null),
            _ => (TypeKind.Error, null, null),
        };

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
                Syntax: declared.Syntax);

            ctx.Fields.Add(typedField);
            ctx.FieldLookup[declared.Name] = typedField;

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
            var typedArgs = declared.Args
                .Select(arg => new TypedArg(
                    Name: arg.Name,
                    EventName: arg.EventName,
                    ResolvedType: arg.Type.Kind,
                    ElementType: null, // event arg element types deferred until arg type parsing is richer
                    Modifiers: ImmutableArray<ModifierKind>.Empty,
                    DefaultExpression: null, // Slice 2+
                    IsOptional: false,
                    Span: arg.NameSpan))
                .ToImmutableArray();

            var typedEvent = new TypedEvent(
                Name: declared.Name,
                Args: typedArgs,
                IsInitial: declared.IsInitial,
                Syntax: declared.Syntax);

            ctx.Events.Add(typedEvent);
            ctx.EventLookup[declared.Name] = typedEvent;
        }
    }

    /// <summary>
    /// Build a partial <see cref="SemanticIndex"/> from the populated <see cref="CheckContext"/>.
    /// Only symbol tables (Fields, States, Events) and diagnostics are populated;
    /// normalized declarations and dependency facts are empty pending Slices 2–10.
    /// </summary>
    private static SemanticIndex BuildPartialSemanticIndex(CheckContext ctx)
    {
        var fields = ctx.Fields.ToImmutableArray();
        var states = ctx.States.ToImmutableArray();
        var events = ctx.Events.ToImmutableArray();

        return new SemanticIndex(
            Fields: fields,
            States: states,
            Events: events,
            FieldsByName: fields.ToFrozenDictionary(f => f.Name),
            StatesByName: states.ToFrozenDictionary(s => s.Name),
            EventsByName: events.ToFrozenDictionary(e => e.Name),
            TransitionRows: ImmutableArray<TypedTransitionRow>.Empty,
            Rules: ImmutableArray<TypedRule>.Empty,
            Ensures: ImmutableArray<TypedEnsure>.Empty,
            AccessModes: ImmutableArray<TypedAccessMode>.Empty,
            StateHooks: ImmutableArray<TypedStateHook>.Empty,
            EventHandlers: ImmutableArray<TypedEventHandler>.Empty,
            EditDeclarations: ImmutableArray<TypedEditDeclaration>.Empty,
            EnsuresByState: FrozenDictionary<string, ImmutableArray<TypedEnsure>>.Empty,
            ComputedDeps: ImmutableArray<ComputedFieldDep>.Empty,
            ConstraintRefs: ImmutableArray<ConstraintFieldRefs>.Empty,
            FieldReferences: ImmutableArray<FieldReference>.Empty,
            StateReferences: ImmutableArray<StateReference>.Empty,
            EventReferences: ImmutableArray<EventReference>.Empty,
            Diagnostics: ctx.Diagnostics.ToImmutableArray());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 2 stubs — declaration normalization (Slices 2–9)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolve a <see cref="ParsedExpression"/> node to a <see cref="TypedExpression"/>.
    /// The core recursive resolution function (~250–350 lines when implemented).
    /// </summary>
    private static TypedExpression Resolve(ParsedExpression expr, CheckContext ctx) =>
        throw new NotImplementedException("Slice 2");

    /// <summary>Normalize a transition row construct into a <see cref="TypedTransitionRow"/>.</summary>
    private static TypedTransitionRow NormalizeTransitionRow(ParsedConstruct construct, CheckContext ctx) =>
        throw new NotImplementedException("Slice 5");

    /// <summary>Normalize an event handler construct into a <see cref="TypedEventHandler"/>.</summary>
    private static TypedEventHandler NormalizeEventHandler(ParsedConstruct construct, CheckContext ctx) =>
        throw new NotImplementedException("Slice 5");

    /// <summary>Resolve a quantifier expression arm (push/pop binding stack).</summary>
    private static TypedExpression ResolveQuantifier(QuantifierExpression expr, CheckContext ctx) =>
        throw new NotImplementedException("Slice 9");

    /// <summary>Resolve a function call expression using the Functions catalog overload resolution algorithm.</summary>
    private static TypedExpression ResolveFunctionCall(FunctionCallExpression expr, CheckContext ctx) =>
        throw new NotImplementedException("Slice 3");

    // ════════════════════════════════════════════════════════════════════════
    //  Pass 2 stubs — structural validation (Slices 6–8)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Validate modifier applicability, conflicts, and subsumption for all fields and states.</summary>
    private static void ValidateModifiers(CheckContext ctx) =>
        throw new NotImplementedException("Slice 7");

    /// <summary>
    /// Structural validation sub-pass: computed-field cycle detection, choice validation,
    /// forward-reference prohibition, stateless/stateful cross-validation.
    /// </summary>
    private static void ValidateStructural(CheckContext ctx) =>
        throw new NotImplementedException("Slice 6");

    /// <summary>CI enforcement sub-pass: validate ~string usage on CI functions and operators.</summary>
    private static void ValidateCIEnforcement(CheckContext ctx) =>
        throw new NotImplementedException("Slice 8");

    // ════════════════════════════════════════════════════════════════════════
    //  Final assembly stub (Slice 10)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Transform <see cref="CheckContext"/> mutable accumulators into the immutable <see cref="SemanticIndex"/>.
    /// Derives frozen-dictionary secondary indexes from primary arrays.
    /// </summary>
    private static SemanticIndex BuildSemanticIndex(CheckContext ctx) =>
        throw new NotImplementedException("Slice 10");
}
