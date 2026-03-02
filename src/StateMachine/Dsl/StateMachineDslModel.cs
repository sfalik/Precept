using System.Collections.Generic;

namespace StateMachine.Dsl;

public sealed record DslMachine(
    string Name,
    IReadOnlyList<string> States,
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

public sealed record DslTransition(
    string FromState,
    string ToState,
    string EventName,
    string? GuardExpression,
    string? DataAssignmentKey,
    string? DataAssignmentExpression,
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
