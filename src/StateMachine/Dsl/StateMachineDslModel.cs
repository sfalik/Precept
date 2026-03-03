using System.Collections.Generic;

namespace StateMachine.Dsl;

public sealed record DslMachine(
    string Name,
    IReadOnlyList<string> States,
    string InitialState,
    IReadOnlyList<DslEvent> Events,
    IReadOnlyList<DslTransition> Transitions,
    IReadOnlyList<DslTerminalRule> TerminalRules,
    IReadOnlyList<DslFieldContract> DataFields);

public sealed record DslEvent(
    string Name,
    IReadOnlyList<DslFieldContract> Args);

public sealed record DslFieldContract(
    string Name,
    DslScalarType Type,
    bool IsNullable);

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
    DslExpression Expression);

public sealed record DslTransition(
    string FromState,
    string ToState,
    string EventName,
    string? GuardExpression,
    IReadOnlyList<DslSetAssignment> SetAssignments,
    int Order = 0);

public sealed record DslTerminalRule(
    string FromState,
    string EventName,
    DslTerminalKind Kind,
    string? Reason,
    string? GuardExpression = null,
    int Order = 0);

public enum DslTerminalKind
{
    Reject,
    NoTransition
}
