using System.Collections.Generic;

namespace StateMachine.Dsl;

public sealed record DslMachine(
    string Name,
    IReadOnlyList<string> States,
    IReadOnlyList<DslEvent> Events,
    IReadOnlyList<DslTransition> Transitions);

public sealed record DslEvent(string Name);

public sealed record DslTransition(
    string FromState,
    string ToState,
    string EventName,
    string? GuardExpression,
    string? DataAssignmentKey,
    string? DataAssignmentExpression);
