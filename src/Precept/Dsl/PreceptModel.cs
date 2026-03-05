using System.Collections.Generic;

namespace Precept;

public sealed record DslWorkflowModel(
    string Name,
    IReadOnlyList<DslState> States,
    DslState InitialState,
    IReadOnlyList<DslEvent> Events,
    IReadOnlyList<DslTransition> Transitions,
    IReadOnlyList<DslField> Fields,
    IReadOnlyList<DslCollectionField> CollectionFields,
    IReadOnlyList<DslRule>? TopLevelRules = null);

public sealed record DslState(
    string Name,
    IReadOnlyList<DslRule>? Rules = null);

public sealed record DslEvent(
    string Name,
    IReadOnlyList<DslEventArg> Args,
    IReadOnlyList<DslRule>? Rules = null);

public sealed record DslEventArg(
    string Name,
    DslScalarType Type,
    bool IsNullable,
    bool HasDefaultValue = false,
    object? DefaultValue = null);

public sealed record DslField(
    string Name,
    DslScalarType Type,
    bool IsNullable,
    bool HasDefaultValue = false,
    object? DefaultValue = null,
    IReadOnlyList<DslRule>? Rules = null);

public sealed record DslCollectionField(
    string Name,
    DslCollectionKind CollectionKind,
    DslScalarType InnerType,
    IReadOnlyList<DslRule>? Rules = null);

/// <summary>
/// A declarative boolean constraint declared with the <c>rule</c> keyword.
/// </summary>
public sealed record DslRule(
    string ExpressionText,
    DslExpression Expression,
    string Reason,
    int SourceLine,
    int ExpressionStartColumn,
    int ExpressionEndColumn,
    int ReasonStartColumn,
    int ReasonEndColumn);

public enum DslCollectionKind
{
    Set,
    Queue,
    Stack
}

public enum DslScalarType
{
    String,
    Number,
    Boolean,
    Null
}

public abstract record DslExpression;

public sealed record DslLiteralExpression(object? Value) : DslExpression;

public sealed record DslIdentifierExpression(string Name, string? Member = null) : DslExpression;

public sealed record DslUnaryExpression(string Operator, DslExpression Operand) : DslExpression;

public sealed record DslBinaryExpression(string Operator, DslExpression Left, DslExpression Right) : DslExpression;

public sealed record DslParenthesizedExpression(DslExpression Inner) : DslExpression;

public sealed record DslSetAssignment(
    string Key,
    string ExpressionText,
    DslExpression Expression,
    int SourceLine = 0,
    int ExpressionStartColumn = 0,
    int ExpressionEndColumn = 0);

public sealed record DslCollectionMutation(
    DslCollectionMutationVerb Verb,
    string TargetField,
    string? ExpressionText,
    DslExpression? Expression,
    string? IntoField = null,
    int SourceLine = 0,
    int ExpressionStartColumn = 0,
    int ExpressionEndColumn = 0);

public enum DslCollectionMutationVerb
{
    Add,
    Remove,
    Enqueue,
    Dequeue,
    Push,
    Pop,
    Clear
}

/// <summary>
/// Represents a complete <c>from &lt;State&gt; on &lt;Event&gt; [when &lt;Expr&gt;]</c> block.
/// One instance per (FromState, EventName) pair.
/// </summary>
public sealed record DslTransition(
    string FromState,
    string EventName,
    IReadOnlyList<DslClause> Clauses,
    int SourceLine = 0,
    string? Predicate = null,
    DslExpression? PredicateAst = null);

/// <summary>
/// Represents one <c>if</c> / <c>else if</c> / <c>else</c> / unguarded branch within a <c>from…on</c> block.
/// </summary>
public sealed record DslClause(
    DslClauseOutcome Outcome,
    IReadOnlyList<DslSetAssignment> SetAssignments,
    int SourceLine = 0,
    string? Predicate = null,
    DslExpression? PredicateAst = null,
    IReadOnlyList<DslCollectionMutation>? CollectionMutations = null);

/// <summary>Abstract base for the three possible outcomes of a <see cref="DslClause"/>.</summary>
public abstract record DslClauseOutcome;

/// <summary>The <c>transition &lt;State&gt;</c> outcome — moves to a new state.</summary>
public sealed record DslStateTransition(string TargetState) : DslClauseOutcome;

/// <summary>The <c>reject</c> outcome — event is explicitly rejected.</summary>
public sealed record DslRejection(string? Reason = null) : DslClauseOutcome;

/// <summary>The <c>no transition</c> outcome — event is accepted but state does not change.</summary>
public sealed record DslNoTransition : DslClauseOutcome;
