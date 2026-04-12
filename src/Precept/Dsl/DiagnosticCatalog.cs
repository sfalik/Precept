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
    Warning,
    Hint
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

    /// <summary>
    /// Creates a <see cref="ConstraintViolationException"/> with the formatted message.
    /// </summary>
    public ConstraintViolationException ToException(params (string Key, object? Value)[] args)
        => new(this, FormatMessage(args));

    public ConstraintViolationException ToException(int sourceLine, params (string Key, object? Value)[] args)
        => new(this, FormatMessage(args)) { SourceLine = sourceLine };
}

/// <summary>
/// Thrown when a constraint is violated during parsing or assembly.
/// Carries the <see cref="LanguageConstraint"/> for diagnostic code derivation
/// and optional source line for accurate squiggle placement.
/// </summary>
public sealed class ConstraintViolationException(LanguageConstraint constraint, string message)
    : InvalidOperationException(message)
{
    public LanguageConstraint Constraint { get; } = constraint;
    public int SourceLine { get; init; }
}

public static class DiagnosticCatalog
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

    /// <summary>
    /// Converts a constraint ID (e.g. "C7") to an LSP diagnostic code (e.g. "PRECEPT007").
    /// </summary>
    public static string ToDiagnosticCode(string constraintId)
    {
        var digits = constraintId.AsSpan(1);
        return $"PRECEPT{digits.ToString().PadLeft(3, '0')}";
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

    /// <summary>At least one field or state must be declared.</summary>
    public static readonly LanguageConstraint C12 = Register(
        "C12", "parse",
        "At least one field or state must be declared.",
        "At least one field or state must be declared.");

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

    // ═══════════════════════════════════════════════════════════════
    // Compile-phase type constraints (C38–C43)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Identifiers used in expressions must resolve in the current scope.</summary>
    public static readonly LanguageConstraint C38 = Register(
        "C38", "compile",
        "Identifiers used in expressions must resolve in the current scope.",
        "Unknown identifier '{identifier}'.");

    /// <summary>Expressions assigned to typed targets must produce assignable values.</summary>
    public static readonly LanguageConstraint C39 = Register(
        "C39", "compile",
        "Expressions assigned to typed targets must produce assignable values.",
        "{context} type mismatch: expected {expected} but expression produces {actual}.");

    /// <summary>Unary operators must be applied to operands of the correct type.</summary>
    public static readonly LanguageConstraint C40 = Register(
        "C40", "compile",
        "Unary operators must be applied to operands of the correct type.",
        "{message}");

    /// <summary>Binary operators must be applied to operands of compatible types.</summary>
    public static readonly LanguageConstraint C41 = Register(
        "C41", "compile",
        "Binary operators must be applied to operands of compatible types.",
        "{message}");

    /// <summary>Nullable values must be narrowed before assignment to non-nullable targets.</summary>
    public static readonly LanguageConstraint C42 = Register(
        "C42", "compile",
        "Nullable values must be narrowed before assignment to non-nullable targets.",
        "{context} type mismatch: expected {expected} but expression produces {actual}.");

    /// <summary>Collection pop/dequeue targets must accept the collection inner type.</summary>
    public static readonly LanguageConstraint C43 = Register(
        "C43", "compile",
        "Collection pop/dequeue targets must accept the collection inner type.",
        "{message}");

    // ═══════════════════════════════════════════════════════════════
    // Compile-phase structural constraints (C44–C47)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Duplicate assert: same preposition, state, and expression text appearing more than once.</summary>
    public static readonly LanguageConstraint C44 = Register(
        "C44", "compile",
        "Duplicate assert: same preposition, state, and expression appearing more than once.",
        "Duplicate state assert: '{preposition} {state} assert {expression}' appears more than once (line {sourceLine}).");

    /// <summary>Subsumed assert: 'to' assert is redundant when an identical 'in' assert exists on the same state.</summary>
    public static readonly LanguageConstraint C45 = Register(
        "C45", "compile",
        "Subsumed assert: 'to' assert is redundant when an identical 'in' assert exists on the same state.",
        "Subsumed state assert: 'to {state} assert {expression}' is redundant — an identical 'in {state} assert' already covers entry (line {sourceLine}).");

    /// <summary>Expressions in rule positions (guards, invariants, asserts) must produce a boolean value.</summary>
    public static readonly LanguageConstraint C46 = Register(
        "C46", "compile",
        "Expressions in rule positions (guards, invariants, asserts) must produce a boolean value.",
        "{context} must be a boolean expression, but expression produces {actual}.");

    /// <summary>Identical guard on duplicate transition rows for the same state+event pair.</summary>
    public static readonly LanguageConstraint C47 = Register(
        "C47", "compile",
        "Identical guard on duplicate transition rows for the same state+event pair.",
        "Duplicate guard: '{guard}' appears more than once for '{state}' on '{event}' (duplicate at line {sourceLine}).");

    /// <summary>State cannot be reached from the initial state via any transition path.</summary>
    public static readonly LanguageConstraint C48 = Register(
        "C48", "compile",
        "State cannot be reached from the initial state via any transition path.",
        "State '{State}' is unreachable from the initial state.",
        ConstraintSeverity.Warning);

    /// <summary>Event is declared but never referenced in any transition row.</summary>
    public static readonly LanguageConstraint C49 = Register(
        "C49", "compile",
        "Event is declared but never referenced in any transition row.",
        "Event '{Event}' is declared but never referenced in any transition row.",
        ConstraintSeverity.Warning);

    /// <summary>Non-terminal state has outgoing rows but none can reach another state.</summary>
    public static readonly LanguageConstraint C50 = Register(
        "C50", "compile",
        "Non-terminal state has outgoing rows but none can reach another state.",
        "State '{State}' has outgoing transitions but all reject or no-transition — no path forward.",
        ConstraintSeverity.Warning);

    /// <summary>Every transition row for a state/event pair rejects, so the event can never succeed there.</summary>
    public static readonly LanguageConstraint C51 = Register(
        "C51", "compile",
        "Every transition row for a state/event pair rejects, so the event can never succeed there.",
        "Every transition row for ({State}, {Event}) ends in reject — the event can never succeed from this state. Remove the rows and let Undefined handle it.",
        ConstraintSeverity.Warning);

    /// <summary>Event can never succeed from any reachable state.</summary>
    public static readonly LanguageConstraint C52 = Register(
        "C52", "compile",
        "Event can never succeed from any reachable state.",
        "Event '{Event}' can never succeed from any reachable state — every reachable state either has no rows or all rows reject.",
        ConstraintSeverity.Warning);

    /// <summary>Precept declares no events.</summary>
    public static readonly LanguageConstraint C53 = Register(
        "C53", "compile",
        "Precept declares no events.",
        "Precept '{Name}' declares no events.",
        ConstraintSeverity.Hint);

    /// <summary>Transition rows must reference declared states (both source and target).</summary>
    public static readonly LanguageConstraint C54 = Register(
        "C54", "parse",
        "Transition rows must reference declared states (both source and target).",
        "Undeclared state '{stateName}' referenced in transition row.");

    /// <summary>Root-level 'edit' declaration is not valid when states are declared.</summary>
    public static readonly LanguageConstraint C55 = Register(
        "C55", "compile",
        "Root-level 'edit' is not valid when states are declared.",
        "Root-level `edit` is not valid when states are declared. Use `in any edit all` or `in <State> edit <Fields>` instead.");

    /// <summary>Member access on nullable string requires explicit null guard before '.length'.</summary>
    public static readonly LanguageConstraint C56 = Register(
        "C56", "compile",
        "Member access on nullable string requires explicit null guard before '.length'.",
        "'{field}.length' requires a null check — '{field}' is nullable. Use '{field} != null and {field}.length ...' or '{field} == null or {field}.length ...'.");

    // ═══════════════════════════════════════════════════════════════
    // Field-level constraint diagnostics (C57–C59)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Constraint applied to an incompatible field type.</summary>
    public static readonly LanguageConstraint C57 = Register(
        "C57", "compile",
        "Constraint applied to an incompatible field type.",
        "Constraint '{constraint}' is not valid for type '{type}'.");

    /// <summary>Contradictory or duplicate constraints on the same field.</summary>
    public static readonly LanguageConstraint C58 = Register(
        "C58", "compile",
        "Contradictory or duplicate constraints on the same field.",
        "{message}");

    /// <summary>Default value violates a declared constraint.</summary>
    public static readonly LanguageConstraint C59 = Register(
        "C59", "compile",
        "Default value violates a declared constraint.",
        "Default value '{value}' violates constraint '{constraint}'. The default must satisfy all declared constraints.");

    // ═══════════════════════════════════════════════════════════════
    // Integer type diagnostics (C60–C61)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Narrowing assignment: cannot assign a non-integer numeric value to an integer field without explicit conversion.</summary>
    public static readonly LanguageConstraint C60 = Register(
        "C60", "compile",
        "Narrowing assignment: cannot assign non-integer value to integer field without explicit conversion.",
        "Narrowing assignment: cannot assign '{actual}' to integer field '{name}' without explicit conversion. An explicit integer conversion function is planned; see documentation.");

    /// <summary>'maxplaces' constraint applies only to decimal fields.</summary>
    public static readonly LanguageConstraint C61 = Register(
        "C61", "compile",
        "'maxplaces' constraint applies only to decimal fields.",
        "'maxplaces' constraint applies only to decimal fields. Integer fields cannot have fractional precision.");

    // ═══════════════════════════════════════════════════════════════════
    // Choice type diagnostics (C62–C68)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>choice type requires at least one value.</summary>
    public static readonly LanguageConstraint C62 = Register(
        "C62", "compile",
        "choice type requires at least one value.",
        "choice type on field '{name}' requires at least one value.");

    /// <summary>Duplicate value in choice set.</summary>
    public static readonly LanguageConstraint C63 = Register(
        "C63", "compile",
        "Duplicate value in choice set.",
        "Duplicate value '{value}' in choice set for field '{name}'.");

    /// <summary>Default value is not a member of the choice set.</summary>
    public static readonly LanguageConstraint C64 = Register(
        "C64", "compile",
        "Default value is not a member of the choice set.",
        "Default value '{value}' is not a member of choice({values}) for field '{name}'.");

    /// <summary>Ordinal comparison requires the 'ordered' constraint on the choice field.</summary>
    public static readonly LanguageConstraint C65 = Register(
        "C65", "compile",
        "Ordinal comparison requires the 'ordered' constraint on the choice field.",
        "Ordinal comparison '{operator}' requires the 'ordered' constraint. Add 'ordered' to the field declaration, or use '==' / '!=' for unordered comparison.");

    /// <summary>'ordered' constraint applies only to choice types.</summary>
    public static readonly LanguageConstraint C66 = Register(
        "C66", "compile",
        "'ordered' constraint applies only to choice types.",
        "'ordered' constraint applies only to choice types. Field '{name}' is '{type}', not a choice field.");

    /// <summary>Ordinal comparison cannot be applied to two choice fields — ordinal rank is field-local.</summary>
    public static readonly LanguageConstraint C67 = Register(
        "C67", "compile",
        "Ordinal comparison cannot be applied to two choice fields — ordinal rank is field-local.",
        "Ordinal comparison '{operator}' cannot be applied to two choice fields. Ordinal rank is field-local — the two fields have independent orderings. Use '==' / '!=' to compare choice field values.");

    /// <summary>Literal value is not a member of the choice set.</summary>
    public static readonly LanguageConstraint C68 = Register(
        "C68", "compile",
        "Literal value is not a member of the choice set.",
        "'{value}' is not a member of choice({values}) for '{name}'.");

    // ═══════════════════════════════════════════════════════════════
    // When-guard diagnostics (C69+)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Cross-scope guard reference in when clause.</summary>
    public static readonly LanguageConstraint C69 = Register(
        "C69", "compile",
        "Cross-scope guard reference in when clause.",
        "Guard expression references '{name}' which belongs to a different scope. Invariant and edit guards can only reference entity fields; event assert guards can only reference event arguments.");

    // ═══════════════════════════════════════════════════════════════
    // Modifier diagnostics (C70+)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Duplicate modifier on field or event argument declaration.</summary>
    public static readonly LanguageConstraint C70 = Register(
        "C70", "parse",
        "Duplicate modifier on field or event argument declaration.",
        "Duplicate modifier '{modifier}' on '{name}'. Each modifier may appear at most once per declaration.");

    // ═══════════════════════════════════════════════════════════════
    // Function diagnostics (C71–C77)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Unknown function name in expression.</summary>
    public static readonly LanguageConstraint C71 = Register(
        "C71", "compile",
        "Unknown function name in expression.",
        "Unknown function '{name}'.");

    /// <summary>Function called with incorrect number of arguments.</summary>
    public static readonly LanguageConstraint C72 = Register(
        "C72", "compile",
        "Function called with incorrect number of arguments.",
        "{name}() called with {count} argument(s), but no matching overload found.");

    /// <summary>Function argument type mismatch.</summary>
    public static readonly LanguageConstraint C73 = Register(
        "C73", "compile",
        "Function argument type mismatch.",
        "{name}() no matching overload: {param} argument expects {expected} but got {actual}.");

    /// <summary>round() precision argument must be a non-negative integer literal.</summary>
    public static readonly LanguageConstraint C74 = Register(
        "C74", "compile",
        "round() precision argument must be a non-negative integer literal.",
        "round() precision argument must be a non-negative integer literal.");

    /// <summary>pow() exponent must be integer type.</summary>
    public static readonly LanguageConstraint C75 = Register(
        "C75", "compile",
        "pow() exponent must be integer type.",
        "pow() exponent must be integer type, but got {actual}.");

    /// <summary>sqrt() requires a non-negative argument proof.</summary>
    public static readonly LanguageConstraint C76 = Register(
        "C76", "compile",
        "sqrt() requires a non-negative argument. Add a 'nonnegative' constraint or guard with '>= 0'.",
        "sqrt() requires a non-negative argument. '{arg}' may be negative. Add a 'nonnegative' constraint or guard with '{arg} >= 0 and ...'.");

    /// <summary>Function does not accept nullable arguments.</summary>
    public static readonly LanguageConstraint C77 = Register(
        "C77", "compile",
        "Function does not accept nullable arguments.",
        "Function '{name}' does not accept nullable arguments. '{arg}' may be null. Add a null check.");

    // ═══════════════════════════════════════════════════════════════
    // Compile-phase constraints: conditional expressions (C78–C79)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Conditional expression condition must be a non-nullable boolean.</summary>
    // SYNC:CONSTRAINT:C78
    public static readonly LanguageConstraint C78 = Register(
        "C78", "compile",
        "Conditional expression condition must be a non-nullable boolean.",
        "Conditional expression condition must be a non-nullable boolean, but got {actual}.");

    /// <summary>Conditional expression branches must produce the same scalar type.</summary>
    // SYNC:CONSTRAINT:C79
    public static readonly LanguageConstraint C79 = Register(
        "C79", "compile",
        "Conditional expression branches must produce the same scalar type.",
        "Conditional expression branches must produce the same scalar type, but got {thenType} and {elseType}.");

    // ═══════════════════════════════════════════════════════════════
    // Parse-phase constraints: computed/derived fields (C80–C82)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>A field cannot have both a default value and a derived expression.</summary>
    // SYNC:CONSTRAINT:C80
    public static readonly LanguageConstraint C80 = Register(
        "C80", "parse",
        "A field cannot have both a default value and a derived expression.",
        "Field '{fieldName}' has both a default value and a derived expression. Use one or the other.");

    /// <summary>A nullable field cannot have a derived expression.</summary>
    // SYNC:CONSTRAINT:C81
    public static readonly LanguageConstraint C81 = Register(
        "C81", "parse",
        "A nullable field cannot have a derived expression.",
        "Field '{fieldName}' is nullable and has a derived expression. Computed fields cannot be nullable.");

    /// <summary>Multi-name field declarations cannot have a derived expression.</summary>
    // SYNC:CONSTRAINT:C82
    public static readonly LanguageConstraint C82 = Register(
        "C82", "parse",
        "Multi-name field declarations cannot have a derived expression.",
        "Multi-name field declaration cannot have a derived expression. Each computed field must be declared separately.");
}
