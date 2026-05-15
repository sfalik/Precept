using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

// ════════════════════════════════════════════════════════════════════════════
//  TypedExpression discriminated union
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Abstract base of the TypedExpression discriminated union.
/// The type checker's output for expressions. Parser-side counterpart: <see cref="ParsedExpression"/>.
/// All subtypes carry <see cref="ResultType"/> and <see cref="Span"/>; expressions do not
/// have a separate <see cref="ParsedConstruct"/> back-pointer.
/// </summary>
public abstract record TypedExpression(TypeKind ResultType, SourceSpan Span);

/// <summary>A resolved reference to a field declaration.</summary>
public sealed record TypedFieldRef(
    TypeKind ResultType,
    string FieldName,
    bool IsCaseInsensitive,
    ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>A resolved reference to an event argument.</summary>
public sealed record TypedArgRef(
    TypeKind ResultType,
    string EventName,
    string ArgName,
    ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>A resolved literal value.</summary>
public sealed record TypedLiteral(
    TypeKind ResultType,
    object? Value,
    SourceSpan Span,
    ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers = null
) : TypedExpression(ResultType, Span);

/// <summary>A resolved binary operation with its catalog-selected operation kind.</summary>
public sealed record TypedBinaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Left,
    TypedExpression Right,
    QualifierBinding? ResultQualifier,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>A resolved unary operation with its catalog-selected operation kind.</summary>
public sealed record TypedUnaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Operand,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>A resolved function call with overload selected from the Functions catalog.</summary>
public sealed record TypedFunctionCall(
    TypeKind ResultType,
    FunctionKind ResolvedFunction,
    ImmutableArray<TypedExpression> Arguments,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>A resolved member access or method call with accessor from the Types catalog.</summary>
public sealed record TypedMemberAccess(
    TypeKind ResultType,
    TypedExpression Object,
    TypeAccessor ResolvedAccessor,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>A resolved if/then/else conditional expression.</summary>
public sealed record TypedConditional(
    TypeKind ResultType,
    TypedExpression Condition,
    TypedExpression ThenBranch,
    TypedExpression ElseBranch,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>
/// A resolved bounded quantifier over a collection.
/// <see cref="ResultType"/> is always <see cref="TypeKind.Boolean"/>.
/// </summary>
public sealed record TypedQuantifier(
    TypeKind ResultType,
    string BindingName,
    TypeKind BindingType,
    TypedExpression Collection,
    TypedExpression Predicate,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>A resolved interpolated string with typed hole expressions.</summary>
public sealed record TypedInterpolatedString(
    ImmutableArray<TypedInterpolationSegment> Segments,
    SourceSpan Span
) : TypedExpression(TypeKind.String, Span);

/// <summary>Abstract base of the typed interpolation segment discriminated union.</summary>
public abstract record TypedInterpolationSegment(SourceSpan Span);

/// <summary>A literal text portion of a typed interpolated string.</summary>
public sealed record TypedTextSegment(string Text, SourceSpan Span) : TypedInterpolationSegment(Span);

/// <summary>A typed expression hole within a typed interpolated string.</summary>
public sealed record TypedHoleSegment(TypedExpression Expression, SourceSpan Span) : TypedInterpolationSegment(Span);

/// <summary>
/// A typed constant with validated content (e.g., <c>'2026-01-01'</c> as date,
/// <c>'100 USD'</c> as money). Content validation is delegated to <see cref="TypeMeta.ContentValidation"/>
/// when available; otherwise validated per-type.
/// </summary>
public sealed record TypedTypedConstant(
    TypeKind ResultType,
    string RawText,
    object? ParsedValue,
    ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>
/// A resolved interpolated typed constant with slot-annotated hole expressions.
/// Each hole is assigned a semantic slot identity by the type-grammar matching algorithm.
/// </summary>
public sealed record InterpolatedTypedConstant(
    ImmutableArray<TypedInterpolationSlot> Slots,
    TypeKind ResultType,
    SourceSpan Span,
    decimal? StaticMagnitude = null,
    StaticInterpolatedQualifier? StaticQualifier = null
) : TypedExpression(ResultType, Span);

public abstract record StaticInterpolatedQualifier;
public sealed record StaticCurrencyQualifier(string CurrencyCode) : StaticInterpolatedQualifier;
public sealed record StaticUnitQualifier(UcumParsedUnit Unit) : StaticInterpolatedQualifier;
public sealed record StaticCurrencyAndUnitQualifier(string CurrencyCode, UcumParsedUnit Unit) : StaticInterpolatedQualifier;
public sealed record StaticFromToCurrenciesQualifier(string FromCode, string ToCode) : StaticInterpolatedQualifier;

/// <summary>A resolved hole expression with its assigned slot identity.</summary>
public sealed record TypedInterpolationSlot(
    TypedExpression Expression,
    InterpolationSlotKind SlotKind
);

/// <summary>Semantic identity of a hole slot in an interpolated typed constant.</summary>
public enum InterpolationSlotKind
{
    Magnitude = 1,
    Currency,
    Unit,
    FromCurrency,
    ToCurrency,
    WholeValue,
    NumeratorUnit,
    DenominatorUnit
}

/// <summary>
/// A typed list literal with unified element type.
/// <see cref="ResultType"/> is the collection kind; <see cref="ElementType"/> is the unified element kind.
/// </summary>
public sealed record TypedListLiteral(
    TypeKind ResultType,
    TypeKind ElementType,
    ImmutableArray<TypedExpression> Elements,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// <summary>
/// A postfix presence check: <c>expr is set</c> or <c>expr is not set</c>.
/// <see cref="TypedExpression.ResultType"/> is always <see cref="TypeKind.Boolean"/>.
/// </summary>
public sealed record TypedPostfixOp(
    TypedExpression Operand,
    bool IsNegated,
    SourceSpan Span
) : TypedExpression(TypeKind.Boolean, Span);

/// <summary>
/// Error expression — propagates <see cref="TypeKind.Error"/>, replacing failed sub-expressions.
/// Invariant (D26): any <see cref="SemanticIndex"/> containing a <c>TypedErrorExpression</c>
/// must also contain ≥1 <see cref="Severity.Error"/> diagnostic. Enforced by unconditional invariant check in TypeChecker.
/// </summary>
public sealed record TypedErrorExpression(SourceSpan Span)
    : TypedExpression(TypeKind.Error, Span);

// ════════════════════════════════════════════════════════════════════════════
//  QualifierBinding discriminated union (D9)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Qualifier propagation rule for a binary operation result.
/// Used on <see cref="TypedBinaryOp.ResultQualifier"/> and <see cref="TypedTransitionRow.ResultQualifier"/>.
/// </summary>
public abstract record QualifierBinding;

/// <summary>Result inherits qualifier identity from the named field.</summary>
public sealed record InheritedQualifier(string FieldName) : QualifierBinding;

/// <summary>Both operands must carry the same qualifier; the result inherits it.</summary>
public sealed record SameQualifierRequired : QualifierBinding;

/// <summary>A quantity product cancels a compound-unit denominator and inherits the numerator unit.</summary>
public sealed record CompoundUnitCancellationRequired : QualifierBinding;

/// <summary>
/// Result inherits qualifiers from the qualifier-bearing operand in a scalar operation.
/// The non-qualifier-bearing operand (e.g., decimal) is transparent to qualifier flow.
/// </summary>
public sealed record QualifiedOperandInherited : QualifierBinding;

/// <summary>
/// Result currency is the ToCurrency of the exchangerate operand.
/// Used for <c>ExchangeRateTimesMoney</c> currency conversion operations.
/// </summary>
public sealed record CurrencyConversionRequired : QualifierBinding;

/// <summary>
/// Proof that price divided by compound-quantity needs dimension elevation resolution.
/// The price's denominator dimension must match the compound-quantity's denominator dimension,
/// and the result carries the compound-quantity's numerator unit.
/// </summary>
public sealed record CompoundDimensionElevationRequired : QualifierBinding;

// ════════════════════════════════════════════════════════════════════════════
//  ActionSecondaryRole enum (D5)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Discriminates the semantic role of <see cref="TypedInputAction.SecondaryExpression"/>.
/// Invariant (D5): <c>SecondaryRole.HasValue == (SecondaryExpression != null)</c>.
/// The Evaluator switches on this value for dispatch — without it, it would need to
/// back-reference <see cref="TypedAction.Kind"/> defeating the DU's purpose.
/// </summary>
public enum ActionSecondaryRole
{
    /// <summary>Secondary expression is the insertion index: <c>insert … at &lt;index&gt;</c>.</summary>
    Index = 1,
    /// <summary>Secondary expression is the ordering/grouping key: <c>put … key &lt;key&gt;</c>, <c>appendBy &lt;key&gt;</c>, <c>enqueueBy &lt;key&gt;</c>.</summary>
    Key   = 2,
}

// ════════════════════════════════════════════════════════════════════════════
//  TypedAction discriminated union (3-shape)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Base typed action — no operand (e.g., clear). Downstream consumers that need
/// operand data should match on the sealed subtypes.
/// </summary>
public record TypedAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
);

/// <summary>
/// Input action — carries a resolved value expression and an optional secondary expression.
/// Invariant: <c>SecondaryRole.HasValue == (SecondaryExpression != null)</c>.
/// </summary>
public sealed record TypedInputAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    TypedExpression InputExpression,
    TypedExpression? SecondaryExpression,
    ActionSecondaryRole? SecondaryRole,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedAction(Kind, FieldName, FieldType, ProofRequirements, Span);

/// <summary>
/// Binding action — carries an optional "into" target field name (e.g., dequeue into, pop into).
/// </summary>
public sealed record TypedBindingAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    string? Binding,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedAction(Kind, FieldName, FieldType, ProofRequirements, Span);

// ════════════════════════════════════════════════════════════════════════════
//  Symbol records
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Typed field declaration — the type checker's resolved output for a field.</summary>
public sealed record TypedField(
    string Name,
    TypeKind ResolvedType,
    TypeKind? ElementType,
    TypeKind? KeyType,
    ImmutableArray<ModifierKind> Modifiers,
    ImmutableArray<ModifierKind> ImpliedModifiers,
    TypedExpression? DefaultExpression,
    TypedExpression? ComputedExpression,
    QualifierBinding? Qualifier,
    bool IsComputed,
    bool IsOptional,
    bool IsWritable,
    DeclaredPresenceMeta Presence,
    ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers,
    SourceSpan NameSpan,
    ParsedConstruct Syntax,
    decimal? DeclaredMin = null,
    decimal? DeclaredMax = null,
    decimal? NormalizedDeclaredMin = null,
    decimal? NormalizedDeclaredMax = null,
    ImmutableArray<DeclaredQualifierMeta> DeclaredMinBoundQualifiers = default,
    ImmutableArray<DeclaredQualifierMeta> DeclaredMaxBoundQualifiers = default,
    int? DeclaredMinLength = null,
    int? DeclaredMaxLength = null,
    int? DeclaredMinCount = null,
    int? DeclaredMaxCount = null
);

/// <summary>Typed state declaration.</summary>
public sealed record TypedState(
    string Name,
    ImmutableArray<ModifierKind> Modifiers,
    SourceSpan NameSpan,
    ParsedConstruct Syntax
);

/// <summary>Typed event declaration.</summary>
public sealed record TypedEvent(
    string Name,
    ImmutableArray<TypedArg> Args,
    bool IsInitial,
    SourceSpan NameSpan,
    ParsedConstruct Syntax
);

/// <summary>
/// Typed event argument. Arguments are part of their enclosing event construct,
/// so they carry a <see cref="SourceSpan"/> rather than a <see cref="ParsedConstruct"/> back-pointer.
/// </summary>
public sealed record TypedArg(
    string Name,
    string EventName,
    TypeKind ResolvedType,
    TypeKind? ElementType,
    ImmutableArray<ModifierKind> Modifiers,
    TypedExpression? DefaultExpression,
    bool IsOptional,
    DeclaredPresenceMeta Presence,
    ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers,
    SourceSpan Span,
    decimal? DeclaredMin = null,
    decimal? DeclaredMax = null,
    decimal? NormalizedDeclaredMin = null,
    decimal? NormalizedDeclaredMax = null,
    ImmutableArray<DeclaredQualifierMeta> DeclaredMinBoundQualifiers = default,
    ImmutableArray<DeclaredQualifierMeta> DeclaredMaxBoundQualifiers = default
);

// ════════════════════════════════════════════════════════════════════════════
//  Normalized declaration records
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// A normalized transition row from the state-machine table.
/// </summary>
public sealed record TypedTransitionRow(
    /// <summary>
    /// Source state name, or <c>null</c> for the any-state wildcard (the <c>*</c> transition).
    /// A <c>null</c> value means the row fires in any source state — it is NOT an unresolved
    /// reference. <see cref="GraphAnalyzer"/> filters wildcard rows with <c>== null</c>.
    /// </summary>
    string? FromState,
    string EventName,
    string? TargetState,
    TypedExpression? Guard,
    ImmutableArray<TypedAction> Actions,
    TransitionRowOutcome Outcome,
    string? RejectReason,
    QualifierBinding? ResultQualifier,
    /// <summary>
    /// Span covering the full transition row construct.
    /// Pre-extracted in <see cref="TypeChecker"/> from <c>construct.Span</c>;
    /// available in <see cref="GraphAnalyzer"/> without violating PRECEPT0024.
    /// </summary>
    SourceSpan RowSpan,
    ParsedConstruct Syntax
);

/// <summary>Outcome of a transition row.</summary>
public enum TransitionRowOutcome { Transition = 1, NoTransition = 2, Reject = 3 }

/// <summary>A normalized rule (invariant constraint) declaration.</summary>
public sealed record TypedRule(
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ParsedConstruct Syntax
);

/// <summary>A normalized ensure (state/event-anchored constraint) declaration.</summary>
public sealed record TypedEnsure(
    ConstraintKind Kind,
    string? AnchorState,
    string? AnchorEvent,
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ParsedConstruct Syntax
);

/// <summary>A normalized access-mode declaration (read/write/omit control per state).</summary>
public sealed record TypedAccessMode(
    string StateName,
    string FieldName,
    ModifierKind Mode,
    TypedExpression? Guard,
    ParsedConstruct Syntax
);

/// <summary>A normalized state lifecycle hook (on-entry or on-exit actions).</summary>
public sealed record TypedStateHook(
    AnchorScope Scope,
    string StateName,
    TypedExpression? Guard,
    ImmutableArray<TypedAction> Actions,
    ParsedConstruct Syntax
);

/// <summary>A normalized event handler (unconditional action chain on an event).</summary>
public sealed record TypedEventHandler(
    string EventName,
    ImmutableArray<TypedAction> Actions,
    ParsedConstruct Syntax
);

/// <summary>
/// Placeholder for stateless-precept edit declarations (<c>edit all</c> / <c>edit Field1, Field2</c>).
/// Full implementation deferred pending stateless-precept design (D24).
/// </summary>
public sealed record TypedEditDeclaration(
    ImmutableArray<string> EditableFields,
    bool IsEditAll,
    ParsedConstruct Syntax
);

// ════════════════════════════════════════════════════════════════════════════
//  Dependency fact records
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Computed field dependency — records which fields a computed field reads.</summary>
public sealed record ComputedFieldDep(
    string FieldName,
    ImmutableArray<string> DependsOn
);

/// <summary>
/// Constraint field references — which fields and args a constraint expression reads.
/// Used by the ProofEngine for obligation scoping and by the LS for semantic subjects.
/// </summary>
public sealed record ConstraintFieldRefs(
    ConstraintIdentity ConstraintIdentity,
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<string> ReferencedArgs
);

// ════════════════════════════════════════════════════════════════════════════
//  ConstraintIdentity discriminated union (shared with ProofEngine)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Identity of a constraint declaration. Shared between <see cref="ConstraintFieldRefs"/>
/// in SemanticIndex and the ProofEngine's obligation tracking — both stages use the identical type.
/// </summary>
public abstract record ConstraintIdentity;

/// <summary>Identity of a <c>rule</c> declaration (invariant constraint).</summary>
public sealed record RuleIdentity(int RuleIndex) : ConstraintIdentity;

/// <summary>Identity of an <c>ensure</c> declaration (state/event-anchored constraint).</summary>
public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorName, int EnsureIndex) : ConstraintIdentity;

// ════════════════════════════════════════════════════════════════════════════
//  Reference site records (CC#3)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>A reference site where a field was read; recorded at resolution time for LS semantic tokens and navigation.</summary>
public sealed record FieldReference(TypedField Field, SourceSpan Site);

/// <summary>A reference site where a state was referenced; recorded at resolution time for LS semantic tokens and navigation.</summary>
public sealed record StateReference(TypedState State, SourceSpan Site);

/// <summary>A reference site where an event was referenced; recorded at resolution time for LS semantic tokens and navigation.</summary>
public sealed record EventReference(TypedEvent Event, SourceSpan Site);

/// <summary>A reference site where an event argument was referenced; recorded at resolution time for LS semantic tokens and navigation.</summary>
public sealed record ArgReference(TypedArg Arg, SourceSpan Site);

// ════════════════════════════════════════════════════════════════════════════
//  SemanticIndex — flat semantic inventory (D4)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// The type checker's output: a flat semantic inventory of resolved symbols, typed expressions,
/// normalized declarations, and dependency facts. Organized by semantic role, not source position.
/// <para>
/// <b>D4:</b> Primary storage is <see cref="ImmutableArray{T}"/> (preserves declaration order);
/// secondary lookup is <see cref="FrozenDictionary{K,V}"/> (O(1) by name). Never use
/// <c>ImmutableDictionary</c> as primary storage.
/// </para>
/// <para>
/// <b>D26 (Slice 10):</b> If any <see cref="TypedErrorExpression"/> is present in any typed expression
/// reachable from this index, at least one <see cref="Severity.Error"/> diagnostic must also be present.
/// Enforced by unconditional <c>throw</c> at construction time in Slice 10.
/// </para>
/// </summary>
public sealed record SemanticIndex(
    // ── Symbol tables — ordered arrays (primary) ──────────────────────────
    ImmutableArray<TypedField>  Fields,
    ImmutableArray<TypedState>  States,
    ImmutableArray<TypedEvent>  Events,

    // ── Derived lookup indexes (secondary, O(1)) ──────────────────────────
    FrozenDictionary<string, TypedField>  FieldsByName,
    FrozenDictionary<string, TypedState>  StatesByName,
    FrozenDictionary<string, TypedEvent>  EventsByName,

    // ── Normalized declarations — ordered arrays ──────────────────────────
    ImmutableArray<TypedTransitionRow>    TransitionRows,
    ImmutableArray<TypedRule>             Rules,
    ImmutableArray<TypedEnsure>           Ensures,
    ImmutableArray<TypedAccessMode>       AccessModes,
    ImmutableArray<TypedStateHook>        StateHooks,
    ImmutableArray<TypedEventHandler>     EventHandlers,
    ImmutableArray<TypedEditDeclaration>  EditDeclarations,

    // ── Secondary derived indexes over normalized declarations (CC#22) ─────
    FrozenDictionary<string, ImmutableArray<TypedEnsure>> EnsuresByState,

    // ── Dependency facts ──────────────────────────────────────────────────
    ImmutableArray<ComputedFieldDep>      ComputedDeps,
    ImmutableArray<ConstraintFieldRefs>   ConstraintRefs,

    // ── Reference sites (CC#3) ────────────────────────────────────────────
    ImmutableArray<FieldReference>        FieldReferences,
    ImmutableArray<StateReference>        StateReferences,
    ImmutableArray<EventReference>        EventReferences,
    ImmutableArray<ArgReference>          ArgReferences,

    // ── Diagnostics ───────────────────────────────────────────────────────
    ImmutableArray<Diagnostic>            Diagnostics
)
{
    /// <summary>Creates an empty SemanticIndex with all collections empty and no diagnostics.</summary>
    public static SemanticIndex Empty { get; } = new(
        Fields:           ImmutableArray<TypedField>.Empty,
        States:           ImmutableArray<TypedState>.Empty,
        Events:           ImmutableArray<TypedEvent>.Empty,
        FieldsByName:     FrozenDictionary<string, TypedField>.Empty,
        StatesByName:     FrozenDictionary<string, TypedState>.Empty,
        EventsByName:     FrozenDictionary<string, TypedEvent>.Empty,
        TransitionRows:   ImmutableArray<TypedTransitionRow>.Empty,
        Rules:            ImmutableArray<TypedRule>.Empty,
        Ensures:          ImmutableArray<TypedEnsure>.Empty,
        AccessModes:      ImmutableArray<TypedAccessMode>.Empty,
        StateHooks:       ImmutableArray<TypedStateHook>.Empty,
        EventHandlers:    ImmutableArray<TypedEventHandler>.Empty,
        EditDeclarations: ImmutableArray<TypedEditDeclaration>.Empty,
        EnsuresByState:   FrozenDictionary<string, ImmutableArray<TypedEnsure>>.Empty,
        ComputedDeps:     ImmutableArray<ComputedFieldDep>.Empty,
        ConstraintRefs:   ImmutableArray<ConstraintFieldRefs>.Empty,
        FieldReferences:  ImmutableArray<FieldReference>.Empty,
        StateReferences:  ImmutableArray<StateReference>.Empty,
        EventReferences:  ImmutableArray<EventReference>.Empty,
        ArgReferences:    ImmutableArray<ArgReference>.Empty,
        Diagnostics:      ImmutableArray<Diagnostic>.Empty
    );
}

