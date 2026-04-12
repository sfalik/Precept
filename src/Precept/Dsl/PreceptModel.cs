using System.Collections.Generic;

namespace Precept;

public sealed record PreceptDefinition(
    string Name,
    IReadOnlyList<PreceptState> States,
    PreceptState? InitialState,
    IReadOnlyList<PreceptEvent> Events,
    IReadOnlyList<PreceptField> Fields,
    IReadOnlyList<PreceptCollectionField> CollectionFields,
    IReadOnlyList<PreceptInvariant>? Invariants = null,
    IReadOnlyList<StateAssertion>? StateAsserts = null,
    IReadOnlyList<PreceptStateAction>? StateActions = null,
    IReadOnlyList<EventAssertion>? EventAsserts = null,
    IReadOnlyList<PreceptTransitionRow>? TransitionRows = null,
    IReadOnlyList<PreceptEditBlock>? EditBlocks = null,
    int SourceLine = 0)
{
    public bool IsStateless => States.Count == 0;
}

public sealed record PreceptState(
    string Name,
    int SourceLine = 0,
    int SourceColumn = 0);

public sealed record PreceptEvent(
    string Name,
    IReadOnlyList<PreceptEventArg> Args,
    int SourceLine = 0,
    int SourceColumn = 0);

public sealed record PreceptEventArg(
    string Name,
    PreceptScalarType Type,
    bool IsNullable,
    bool HasDefaultValue = false,
    object? DefaultValue = null,
    IReadOnlyList<FieldConstraint>? Constraints = null,
    IReadOnlyList<string>? ChoiceValues = null,
    bool IsOrdered = false,
    int SourceLine = 0);

public sealed record PreceptField(
    string Name,
    PreceptScalarType Type,
    bool IsNullable,
    bool HasDefaultValue = false,
    object? DefaultValue = null,
    IReadOnlyList<FieldConstraint>? Constraints = null,
    IReadOnlyList<string>? ChoiceValues = null,
    bool IsOrdered = false,
    int SourceLine = 0);

public sealed record PreceptCollectionField(
    string Name,
    PreceptCollectionKind CollectionKind,
    PreceptScalarType InnerType,
    IReadOnlyList<FieldConstraint>? Constraints = null,
    IReadOnlyList<string>? ChoiceValues = null,
    int SourceLine = 0);


public enum PreceptCollectionKind
{
    Set,
    Queue,
    Stack
}

/// <summary>
/// A single declaration-level constraint attached to a field, collection field, or event argument.
/// Constraints desugar to <c>invariant</c> / <c>on E assert</c> at parse time.
/// </summary>
public abstract record FieldConstraint
{
    private FieldConstraint() { }

    /// <summary>Value must be &gt;= 0.</summary>
    public sealed record Nonnegative : FieldConstraint;

    /// <summary>Value must be &gt; 0.</summary>
    public sealed record Positive : FieldConstraint;

    /// <summary>Value must be &gt;= <see cref="Value"/>.</summary>
    public sealed record Min(double Value) : FieldConstraint;

    /// <summary>Value must be &lt;= <see cref="Value"/>.</summary>
    public sealed record Max(double Value) : FieldConstraint;

    /// <summary>String or collection must not be empty.</summary>
    public sealed record Notempty : FieldConstraint;

    /// <summary>String length must be &gt;= <see cref="Value"/>.</summary>
    public sealed record Minlength(int Value) : FieldConstraint;

    /// <summary>String length must be &lt;= <see cref="Value"/>.</summary>
    public sealed record Maxlength(int Value) : FieldConstraint;

    /// <summary>Collection count must be &gt;= <see cref="Value"/>.</summary>
    public sealed record Mincount(int Value) : FieldConstraint;

    /// <summary>Collection count must be &lt;= <see cref="Value"/>.</summary>
    public sealed record Maxcount(int Value) : FieldConstraint;

    /// <summary>Value must have at most <see cref="Places"/> decimal places. Decimal fields only.</summary>
    public sealed record Maxplaces(int Places) : FieldConstraint;
}

public enum PreceptScalarType
{
    String,
    Number,
    Boolean,
    Null,
    Integer,   // #29
    Decimal,   // #27 (scaffold)
    Choice,    // #25 (scaffold)
}

public abstract record PreceptExpression;

public sealed record PreceptLiteralExpression(object? Value) : PreceptExpression;

public sealed record PreceptIdentifierExpression(string Name, string? Member = null, string? SubMember = null) : PreceptExpression;

public sealed record PreceptUnaryExpression(string Operator, PreceptExpression Operand) : PreceptExpression;

public sealed record PreceptBinaryExpression(string Operator, PreceptExpression Left, PreceptExpression Right) : PreceptExpression;

public sealed record PreceptParenthesizedExpression(PreceptExpression Inner) : PreceptExpression;

/// <summary>General built-in function call: name(arg1, arg2, ...).</summary>
public sealed record PreceptFunctionCallExpression(string Name, PreceptExpression[] Arguments) : PreceptExpression;

/// <summary>Conditional expression: if &lt;condition&gt; then &lt;thenBranch&gt; else &lt;elseBranch&gt;.</summary>
public sealed record PreceptConditionalExpression(PreceptExpression Condition, PreceptExpression ThenBranch, PreceptExpression ElseBranch) : PreceptExpression;

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
    int SourceLine = 0,
    bool IsSynthetic = false,
    string? WhenText = null,
    PreceptExpression? WhenGuard = null);

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
    int SourceLine = 0,
    string? WhenText = null,
    PreceptExpression? WhenGuard = null);

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
    int SourceLine = 0,
    string? WhenText = null,
    PreceptExpression? WhenGuard = null);

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
    string? State,
    IReadOnlyList<string> FieldNames,
    int SourceLine = 0,
    string? WhenText = null,
    PreceptExpression? WhenGuard = null);
