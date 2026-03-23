using System.Collections.Generic;

namespace Precept;

public sealed record PreceptDefinition(
    string Name,
    IReadOnlyList<PreceptState> States,
    PreceptState InitialState,
    IReadOnlyList<PreceptEvent> Events,
    IReadOnlyList<PreceptField> Fields,
    IReadOnlyList<PreceptCollectionField> CollectionFields,
    IReadOnlyList<PreceptInvariant>? Invariants = null,
    IReadOnlyList<StateAssertion>? StateAsserts = null,
    IReadOnlyList<PreceptStateAction>? StateActions = null,
    IReadOnlyList<EventAssertion>? EventAsserts = null,
    IReadOnlyList<PreceptTransitionRow>? TransitionRows = null,
    IReadOnlyList<PreceptEditBlock>? EditBlocks = null);

public sealed record PreceptState(
    string Name);

public sealed record PreceptEvent(
    string Name,
    IReadOnlyList<PreceptEventArg> Args);

public sealed record PreceptEventArg(
    string Name,
    PreceptScalarType Type,
    bool IsNullable,
    bool HasDefaultValue = false,
    object? DefaultValue = null);

public sealed record PreceptField(
    string Name,
    PreceptScalarType Type,
    bool IsNullable,
    bool HasDefaultValue = false,
    object? DefaultValue = null);

public sealed record PreceptCollectionField(
    string Name,
    PreceptCollectionKind CollectionKind,
    PreceptScalarType InnerType);


public enum PreceptCollectionKind
{
    Set,
    Queue,
    Stack
}

public enum PreceptScalarType
{
    String,
    Number,
    Boolean,
    Null
}

public abstract record PreceptExpression;

public sealed record PreceptLiteralExpression(object? Value) : PreceptExpression;

public sealed record PreceptIdentifierExpression(string Name, string? Member = null) : PreceptExpression;

public sealed record PreceptUnaryExpression(string Operator, PreceptExpression Operand) : PreceptExpression;

public sealed record PreceptBinaryExpression(string Operator, PreceptExpression Left, PreceptExpression Right) : PreceptExpression;

public sealed record PreceptParenthesizedExpression(PreceptExpression Inner) : PreceptExpression;

public sealed record PreceptSetAssignment(
    string Key,
    string ExpressionText,
    PreceptExpression Expression,
    int SourceLine = 0,
    int ExpressionStartColumn = 0,
    int ExpressionEndColumn = 0);

public sealed record PreceptCollectionMutation(
    PreceptCollectionMutationVerb Verb,
    string TargetField,
    string? ExpressionText,
    PreceptExpression? Expression,
    string? IntoField = null,
    int SourceLine = 0,
    int ExpressionStartColumn = 0,
    int ExpressionEndColumn = 0);

public enum PreceptCollectionMutationVerb
{
    Add,
    Remove,
    Enqueue,
    Dequeue,
    Push,
    Pop,
    Clear
}

/// <summary>Abstract base for the three possible outcomes of a transition row.</summary>
public abstract record PreceptClauseOutcome;

/// <summary>The <c>transition &lt;State&gt;</c> outcome — moves to a new state.</summary>
public sealed record StateTransition(string TargetState) : PreceptClauseOutcome;

/// <summary>The <c>reject</c> outcome — event is explicitly rejected.</summary>
public sealed record Rejection(string? Reason = null) : PreceptClauseOutcome;

/// <summary>The <c>no transition</c> outcome — event is accepted but state does not change.</summary>
public sealed record NoTransition : PreceptClauseOutcome;

/// <summary>Preposition for state asserts: <c>in</c>, <c>to</c>, <c>from</c>.</summary>
public enum AssertAnchor
{
    /// <summary><c>in &lt;State&gt;</c> — while residing in the state (entry + AcceptedInPlace).</summary>
    In,
    /// <summary><c>to &lt;State&gt;</c> — crossing into the state (entry only).</summary>
    To,
    /// <summary><c>from &lt;State&gt;</c> — crossing out of the state (exit only).</summary>
    From
}

/// <summary>
/// A global data invariant: <c>invariant &lt;expr&gt; because "reason"</c>.
/// Always holds, checked post-commit on every transition.
/// </summary>
public sealed record PreceptInvariant(
    string ExpressionText,
    PreceptExpression Expression,
    string Reason,
    int SourceLine = 0);

/// <summary>
/// A state-scoped assert: <c>in/to/from &lt;State&gt; assert &lt;expr&gt; because "reason"</c>.
/// Temporal scope depends on <see cref="Anchor"/>.
/// </summary>
public sealed record StateAssertion(
    AssertAnchor Anchor,
    string State,
    string ExpressionText,
    PreceptExpression Expression,
    string Reason,
    int SourceLine = 0);

/// <summary>
/// A state entry/exit action: <c>to/from &lt;State&gt; -&gt; &lt;actions&gt;</c>.
/// Automatic mutations that fire on state change.
/// </summary>
public sealed record PreceptStateAction(
    AssertAnchor Anchor,
    string State,
    IReadOnlyList<PreceptSetAssignment> SetAssignments,
    IReadOnlyList<PreceptCollectionMutation>? CollectionMutations = null,
    int SourceLine = 0);

/// <summary>
/// An event-scoped assert: <c>on &lt;Event&gt; assert &lt;expr&gt; because "reason"</c>.
/// Arg-only validation, checked pre-transition.
/// </summary>
public sealed record EventAssertion(
    string EventName,
    string ExpressionText,
    PreceptExpression Expression,
    string Reason,
    int SourceLine = 0);

/// <summary>
/// A flat transition row: <c>from &lt;State&gt; on &lt;Event&gt; [when &lt;expr&gt;] [-&gt; actions]* -&gt; &lt;outcome&gt;</c>.
/// A self-contained transition row.
/// Multiple rows for the same (State, Event) pair are evaluated top-to-bottom, first match wins.
/// </summary>
public sealed record PreceptTransitionRow(
    string FromState,
    string EventName,
    PreceptClauseOutcome Outcome,
    IReadOnlyList<PreceptSetAssignment> SetAssignments,
    IReadOnlyList<PreceptCollectionMutation>? CollectionMutations = null,
    string? WhenText = null,
    PreceptExpression? WhenGuard = null,
    int SourceLine = 0);

/// <summary>
/// Editable field declaration: <c>in &lt;State&gt; edit &lt;Field&gt;, &lt;Field&gt;</c>.
/// Specifies which fields can be modified directly while residing in a state.
/// Runtime <c>Update</c> API is deferred — model/parser included now.
/// </summary>
public sealed record PreceptEditBlock(
    string State,
    IReadOnlyList<string> FieldNames,
    int SourceLine = 0);
