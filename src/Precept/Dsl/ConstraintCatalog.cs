using System;
using System.Collections.Generic;

namespace Precept;

/// <summary>
/// Tier 3: Constraint Catalog — central registry for all enforcement points
/// (parse errors, compile-time checks, runtime violations). Each constraint
/// gets an ID, phase, rule description, message template, and severity.
/// Consumers: parser/compiler/runtime error messages, language server diagnostics
/// (with stable codes like PRECEPT007), MCP constraint listing.
/// </summary>
public enum ConstraintSeverity
{
    Error,
    Warning
}

/// <summary>
/// A registered language constraint representing a single enforcement rule.
/// </summary>
public sealed record LanguageConstraint(
    string Id,
    string Phase,
    string Rule,
    string MessageTemplate,
    ConstraintSeverity Severity)
{
    /// <summary>
    /// Formats the <see cref="MessageTemplate"/> by replacing {placeholder} tokens
    /// with the supplied values. Returns <see cref="MessageTemplate"/> unchanged
    /// when called with no arguments.
    /// </summary>
    public string FormatMessage(params (string Key, object? Value)[] args)
    {
        if (args.Length == 0) return MessageTemplate;
        var result = MessageTemplate;
        foreach (var (key, value) in args)
            result = result.Replace($"{{{key}}}", value?.ToString() ?? "null");
        return result;
    }
}

public static class ConstraintCatalog
{
    private static readonly List<LanguageConstraint> _constraints = [];

    /// <summary>
    /// All registered language constraints across parse, compile, and runtime phases.
    /// </summary>
    public static IReadOnlyList<LanguageConstraint> Constraints => _constraints;

    internal static LanguageConstraint Register(
        string id, string phase, string rule,
        string messageTemplate, ConstraintSeverity severity = ConstraintSeverity.Error)
    {
        var c = new LanguageConstraint(id, phase, rule, messageTemplate, severity);
        _constraints.Add(c);
        return c;
    }

    // ═══════════════════════════════════════════════════════════════
    // Parse-phase constraints (C1–C25)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>DSL input must not be empty or whitespace-only.</summary>
    public static readonly LanguageConstraint C1 = Register(
        "C1", "parse",
        "DSL input must not be empty or whitespace-only.",
        "DSL input is empty.");

    /// <summary>Source text must tokenize without errors.</summary>
    public static readonly LanguageConstraint C2 = Register(
        "C2", "parse",
        "Source text must tokenize without errors.",
        "{message}");

    /// <summary>Token stream must parse into a valid DSL structure.</summary>
    public static readonly LanguageConstraint C3 = Register(
        "C3", "parse",
        "Token stream must parse into a valid DSL structure.",
        "Failed to parse DSL input.");

    /// <summary>Expression strings must parse into a valid expression tree.</summary>
    public static readonly LanguageConstraint C4 = Register(
        "C4", "parse",
        "Expression strings must parse into a valid expression tree.",
        "Failed to parse expression: {expression}");

    /// <summary>Number literal tokens must be valid numeric values.</summary>
    public static readonly LanguageConstraint C5 = Register(
        "C5", "parse",
        "Number literal tokens must be valid numeric values.",
        "Invalid number literal: {value}");

    /// <summary>Field names must be unique across scalar and collection fields.</summary>
    public static readonly LanguageConstraint C6 = Register(
        "C6", "parse",
        "Field names must be unique across scalar and collection fields.",
        "Duplicate field '{fieldName}'.");

    /// <summary>State names must be unique.</summary>
    public static readonly LanguageConstraint C7 = Register(
        "C7", "parse",
        "State names must be unique.",
        "Duplicate state '{stateName}'.");

    /// <summary>Only one state may be marked initial.</summary>
    public static readonly LanguageConstraint C8 = Register(
        "C8", "parse",
        "Only one state may be marked initial.",
        "Duplicate initial state. '{stateName}' is already marked initial.");

    /// <summary>Event names must be unique.</summary>
    public static readonly LanguageConstraint C9 = Register(
        "C9", "parse",
        "Event names must be unique.",
        "Duplicate event '{eventName}'.");

    /// <summary>Every transition row must end with exactly one outcome (transition, no transition, or reject).</summary>
    public static readonly LanguageConstraint C10 = Register(
        "C10", "parse",
        "Every transition row must end with exactly one outcome (transition, no transition, or reject).",
        "Transition row for event '{eventName}' is missing an outcome (transition, no transition, or reject).");

    /// <summary>No statements are allowed after an outcome statement in a transition row.</summary>
    public static readonly LanguageConstraint C11 = Register(
        "C11", "parse",
        "No statements are allowed after an outcome statement in a transition row.",
        "Transition row for event '{eventName}': no statements are allowed after an outcome statement.");

    /// <summary>At least one state must be declared.</summary>
    public static readonly LanguageConstraint C12 = Register(
        "C12", "parse",
        "At least one state must be declared.",
        "At least one state must be declared.");

    /// <summary>Exactly one state must be marked initial.</summary>
    public static readonly LanguageConstraint C13 = Register(
        "C13", "parse",
        "Exactly one state must be marked initial.",
        "Exactly one state must be marked initial. Use 'state <Name> initial'.");

    /// <summary>Event assert expressions using dotted access must use the event name as prefix.</summary>
    public static readonly LanguageConstraint C14 = Register(
        "C14", "parse",
        "Event assert expressions using dotted access must use the event name as prefix.",
        "'on {eventName} assert' can only reference event argument identifiers. '{prefix}.{member}' uses an unknown prefix.");

    /// <summary>Event assert dotted member must be a declared argument of the event.</summary>
    public static readonly LanguageConstraint C15 = Register(
        "C15", "parse",
        "Event assert dotted member must be a declared argument of the event.",
        "'on {eventName} assert' can only reference event argument identifiers. '{member}' is not an event argument of '{eventName}'.");

    /// <summary>Event assert plain identifiers must be declared arguments of the event.</summary>
    public static readonly LanguageConstraint C16 = Register(
        "C16", "parse",
        "Event assert plain identifiers must be declared arguments of the event.",
        "'on {eventName} assert' can only reference event argument identifiers. '{identifier}' is not an event argument of '{eventName}'.");

    /// <summary>Non-nullable fields must have a default value.</summary>
    public static readonly LanguageConstraint C17 = Register(
        "C17", "parse",
        "Non-nullable fields must have a default value.",
        "Non-nullable field '{fieldName}' requires a default value.");

    /// <summary>Field default value must match the declared type.</summary>
    public static readonly LanguageConstraint C18 = Register(
        "C18", "parse",
        "Field default value must match the declared type.",
        "Default value for field '{fieldName}' does not match declared type '{fieldType}'.");

    /// <summary>Non-nullable fields cannot have null as a default value.</summary>
    public static readonly LanguageConstraint C19 = Register(
        "C19", "parse",
        "Non-nullable fields cannot have null as a default value.",
        "Default value for field '{fieldName}' does not match declared type '{fieldType}' (null is not allowed for non-nullable fields).");

    /// <summary>Event argument default value must match the declared type.</summary>
    public static readonly LanguageConstraint C20 = Register(
        "C20", "parse",
        "Event argument default value must match the declared type.",
        "Default value for event argument '{argName}' does not match declared type '{argType}'.");

    /// <summary>Non-nullable event arguments cannot have null as a default value.</summary>
    public static readonly LanguageConstraint C21 = Register(
        "C21", "parse",
        "Non-nullable event arguments cannot have null as a default value.",
        "Default value for event argument '{argName}' does not match declared type '{argType}' (null is not allowed for non-nullable arguments).");

    /// <summary>Collection mutation verbs cannot target scalar fields.</summary>
    public static readonly LanguageConstraint C22 = Register(
        "C22", "parse",
        "Collection mutation verbs cannot target scalar fields.",
        "'{verb}' targets '{fieldName}' which is a scalar field, not a collection.");

    /// <summary>Collection mutation verbs must target a declared collection field.</summary>
    public static readonly LanguageConstraint C23 = Register(
        "C23", "parse",
        "Collection mutation verbs must target a declared collection field.",
        "'{verb}' targets unknown collection '{fieldName}'.");

    /// <summary>Collection mutation verb must be compatible with the collection kind (set/queue/stack).</summary>
    public static readonly LanguageConstraint C24 = Register(
        "C24", "parse",
        "Collection mutation verb must be compatible with the collection kind (set/queue/stack).",
        "Cannot '{verb}' on a {collectionKind} collection '{fieldName}'.");

    /// <summary>An unguarded transition row for a (state, event) pair makes subsequent rows for the same pair unreachable.</summary>
    public static readonly LanguageConstraint C25 = Register(
        "C25", "parse",
        "An unguarded transition row for a (state, event) pair makes subsequent rows for the same pair unreachable.",
        "Duplicate 'from {fromState} on {eventName}' row is unreachable \u2014 a previous unguarded row already catches all cases.");

    // ═══════════════════════════════════════════════════════════════
    // Compile-phase constraints (C26–C32)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Compile requires a non-null model.</summary>
    public static readonly LanguageConstraint C26 = Register(
        "C26", "compile",
        "Compile requires a non-null model.",
        "Model cannot be null.");

    /// <summary>Exactly one state must be marked initial (compile-time validation).</summary>
    public static readonly LanguageConstraint C27 = Register(
        "C27", "compile",
        "Exactly one state must be marked initial (compile-time validation).",
        "Exactly one state must be marked initial. Use 'state <Name> initial'.");

    /// <summary>The initial state must be a declared state in the workflow (compile-time validation).</summary>
    public static readonly LanguageConstraint C28 = Register(
        "C28", "compile",
        "The initial state must be a declared state in the workflow (compile-time validation).",
        "Initial state '{stateName}' is not defined in workflow '{workflowName}'.");

    /// <summary>Invariants must hold for default field values at compile time.</summary>
    public static readonly LanguageConstraint C29 = Register(
        "C29", "compile",
        "Invariants must hold for default field values at compile time.",
        "Compile-time invariant violation: \"{reason}\" is violated by default field values.");

    /// <summary>State asserts (in/to) on the initial state must hold for default data at compile time.</summary>
    public static readonly LanguageConstraint C30 = Register(
        "C30", "compile",
        "State asserts (in/to) on the initial state must hold for default data at compile time.",
        "Compile-time state assert violation: \"{reason}\" on initial state '{stateName}' is violated by default data.");

    /// <summary>Event asserts must hold for default argument values at compile time (when all args have defaults).</summary>
    public static readonly LanguageConstraint C31 = Register(
        "C31", "compile",
        "Event asserts must hold for default argument values at compile time (when all args have defaults).",
        "Compile-time event assert violation: \"{reason}\" on event '{eventName}' is violated by default argument values.");

    /// <summary>Literal set assignments in transition rows must not violate invariants at compile time.</summary>
    public static readonly LanguageConstraint C32 = Register(
        "C32", "compile",
        "Literal set assignments in transition rows must not violate invariants at compile time.",
        "Line {sourceLine}: literal assignment 'set {key} = {expression}' violates invariant \"{reason}\".");

    // ═══════════════════════════════════════════════════════════════
    // Runtime-phase constraints (C33–C37)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>CreateInstance requires a non-empty initial state name.</summary>
    public static readonly LanguageConstraint C33 = Register(
        "C33", "runtime",
        "CreateInstance requires a non-empty initial state name.",
        "Initial state is required.");

    /// <summary>The initial state must be a declared state in the workflow (runtime validation).</summary>
    public static readonly LanguageConstraint C34 = Register(
        "C34", "runtime",
        "The initial state must be a declared state in the workflow (runtime validation).",
        "State '{stateName}' is not defined in workflow '{workflowName}'.");

    /// <summary>Instance data must satisfy the workflow's data contract (field existence, type, nullability).</summary>
    public static readonly LanguageConstraint C35 = Register(
        "C35", "runtime",
        "Instance data must satisfy the workflow's data contract (field existence, type, nullability).",
        "{message}");

    /// <summary>ResolveTransition requires a non-empty current state.</summary>
    public static readonly LanguageConstraint C36 = Register(
        "C36", "runtime",
        "ResolveTransition requires a non-empty current state.",
        "Current state is required.");

    /// <summary>ResolveTransition requires a non-empty event name.</summary>
    public static readonly LanguageConstraint C37 = Register(
        "C37", "runtime",
        "ResolveTransition requires a non-empty event name.",
        "Event name is required.");
}
