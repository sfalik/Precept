using System.Collections.Immutable;

namespace Precept.Pipeline;

// ════════════════════════════════════════════════════════════════════════════════
//  TypedModel — output of the type-checking stage
// ════════════════════════════════════════════════════════════════════════════════

public sealed record class TypedModel(
    ImmutableDictionary<string, FieldSymbol>   Fields,
    ImmutableDictionary<string, StateSymbol>   States,
    ImmutableDictionary<string, EventSymbol>   Events,
    ImmutableArray<ResolvedRule>                Rules,
    ImmutableArray<ResolvedEnsure>             Ensures,
    ImmutableArray<ResolvedTransitionRow>      TransitionRows,
    ImmutableArray<ResolvedAccessMode>         AccessModes,
    ImmutableArray<ResolvedStateAction>        StateActions,
    ImmutableArray<ResolvedStatelessHook>      StatelessHooks,
    string?                                    InitialState,
    ImmutableArray<Diagnostic>                 Diagnostics
);

// ════════════════════════════════════════════════════════════════════════════════
//  Symbol types
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>A declared field with resolved type, modifiers, and optional computed expression.</summary>
public sealed record FieldSymbol(
    string            Name,
    ResolvedType      Type,
    bool              IsOptional,
    bool              IsComputed,
    ResolvedModifiers Modifiers,
    TypedExpression?  ComputedExpression,
    TypedExpression?  DefaultValue,
    SourceSpan        Span
);

/// <summary>A declared state with its modifiers.</summary>
public sealed record StateSymbol(
    string                            Name,
    bool                              IsInitial,
    ImmutableArray<StateModifierKind> Modifiers,
    SourceSpan                        Span
);

/// <summary>A declared event with typed arguments.</summary>
public sealed record EventSymbol(
    string                                    Name,
    ImmutableDictionary<string, ArgSymbol>    Args,
    bool                                      IsInitial,
    SourceSpan                                Span
);

/// <summary>An event argument with resolved type and modifiers.</summary>
public sealed record ArgSymbol(
    string            Name,
    ResolvedType      Type,
    bool              IsOptional,
    ResolvedModifiers Modifiers,
    TypedExpression?  DefaultValue,
    SourceSpan        Span
);

// ════════════════════════════════════════════════════════════════════════════════
//  Resolved modifiers
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>All validated modifier values for a field or argument.</summary>
public sealed record ResolvedModifiers(
    bool     Nonnegative,
    bool     Positive,
    bool     Nonzero,
    bool     Notempty,
    bool     Ordered,
    decimal? MinValue,
    decimal? MaxValue,
    int?     MinLength,
    int?     MaxLength,
    int?     MinCount,
    int?     MaxCount,
    int?     MaxPlaces
);

// ════════════════════════════════════════════════════════════════════════════════
//  Resolved declarations
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>A rule with its typed condition, optional guard, and message.</summary>
public sealed record ResolvedRule(
    TypedExpression  Condition,
    TypedExpression? Guard,
    TypedExpression  Message,
    SourceSpan       Span
);

/// <summary>An ensure constraint with anchor, scope, typed condition, and message.</summary>
public sealed record ResolvedEnsure(
    EnsureAnchor           Anchor,
    EnsureScope            Scope,
    ImmutableArray<string> StateNames,
    string?                EventName,
    TypedExpression        Condition,
    TypedExpression?       Guard,
    TypedExpression        Message,
    SourceSpan             Span
);

public enum EnsureScope { State, Event }

/// <summary>A transition row with resolved states, event, guard, actions, and outcome.</summary>
public sealed record ResolvedTransitionRow(
    ImmutableArray<string>         FromStates,
    string                         EventName,
    TypedExpression?               Guard,
    ImmutableArray<ResolvedAction> Actions,
    ResolvedOutcome                Outcome,
    SourceSpan                     Span
);

/// <summary>A typed action step.</summary>
public sealed record ResolvedAction(
    ActionKind       Kind,
    string           FieldName,
    TypedExpression? Value,
    string?          IntoFieldName,
    SourceSpan       Span
);

public enum ActionKind { Set, Add, Remove, Enqueue, Dequeue, Push, Pop, Clear }

/// <summary>A resolved transition outcome.</summary>
public abstract record ResolvedOutcome(SourceSpan Span);
public sealed record TransitionOutcome(string StateName, SourceSpan Span) : ResolvedOutcome(Span);
public sealed record NoTransitionOutcome(SourceSpan Span) : ResolvedOutcome(Span);
public sealed record RejectOutcome(TypedExpression Message, SourceSpan Span) : ResolvedOutcome(Span);

/// <summary>A resolved access mode declaration.</summary>
public sealed record ResolvedAccessMode(
    ImmutableArray<string> StateNames,
    AccessMode             Mode,
    ImmutableArray<string> FieldNames,
    bool                   IsAll,
    TypedExpression?       Guard,
    SourceSpan             Span
);

/// <summary>A resolved state action (to/from hooks).</summary>
public sealed record ResolvedStateAction(
    StateActionAnchor              Anchor,
    ImmutableArray<string>         StateNames,
    TypedExpression?               Guard,
    ImmutableArray<ResolvedAction> Actions,
    SourceSpan                     Span
);

/// <summary>A resolved stateless event hook.</summary>
public sealed record ResolvedStatelessHook(
    string                         EventName,
    ImmutableArray<ResolvedAction> Actions,
    SourceSpan                     Span
);

// ════════════════════════════════════════════════════════════════════════════════
//  Typed expressions
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>A typed wrapper around an expression AST node. Carries the resolved type.</summary>
public sealed record TypedExpression(
    Expression   Syntax,
    ResolvedType Type,
    SourceSpan   Span
);

// ════════════════════════════════════════════════════════════════════════════════
//  Resolved type hierarchy
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>The type system's representation of a resolved type.</summary>
public abstract record ResolvedType;

// ── Scalar types ────────────────────────────────────────────────────────────
public sealed record StringType(bool CaseInsensitive = false) : ResolvedType;
public sealed record BooleanType()  : ResolvedType;
public sealed record IntegerType()  : ResolvedType;
public sealed record DecimalType()  : ResolvedType;
public sealed record NumberType()   : ResolvedType;
public sealed record ChoiceType(ImmutableArray<string> Values, bool IsOrdered) : ResolvedType;

// ── Temporal types ──────────────────────────────────────────────────────────
public sealed record DateType()          : ResolvedType;
public sealed record TimeType()          : ResolvedType;
public sealed record InstantType()       : ResolvedType;
public sealed record DurationType()      : ResolvedType;
public sealed record PeriodType(string? Unit, string? Dimension) : ResolvedType;
public sealed record TimezoneType()      : ResolvedType;
public sealed record ZonedDateTimeType() : ResolvedType;
public sealed record DateTimeType()      : ResolvedType;

// ── Business-domain types ───────────────────────────────────────────────────
public sealed record MoneyType(string? Currency)             : ResolvedType;
public sealed record CurrencyType()                          : ResolvedType;
public sealed record QuantityType(string? Unit, string? Dimension) : ResolvedType;
public sealed record UnitOfMeasureType()                     : ResolvedType;
public sealed record DimensionType()                         : ResolvedType;
public sealed record PriceType(string? Currency, string? Unit, string? Dimension) : ResolvedType;
public sealed record ExchangeRateType(string? FromCurrency, string? ToCurrency) : ResolvedType;

// ── Collection types ────────────────────────────────────────────────────────
public sealed record SetType(ResolvedType ElementType)   : ResolvedType;
public sealed record QueueType(ResolvedType ElementType) : ResolvedType;
public sealed record StackType(ResolvedType ElementType) : ResolvedType;

// ── Special types ───────────────────────────────────────────────────────────
/// <summary>Propagated when a sub-expression has an error. Suppresses cascading diagnostics.</summary>
public sealed record ErrorType() : ResolvedType;

/// <summary>The type of a state name reference in transition/ensure targets.</summary>
public sealed record StateRefType() : ResolvedType;
