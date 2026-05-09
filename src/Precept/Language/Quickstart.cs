namespace Precept.Language;

/// <summary>A core concept in the Precept DSL, with a one-line example.</summary>
public sealed record CoreConceptEntry(string Name, string Summary, string Example);

/// <summary>
/// A guide entry describing when to call a specific MCP tool and what it returns.
/// </summary>
public sealed record ToolGuideEntry(string ToolName, string WhenToCall, string ReturnsSummary);

/// <summary>A minimal DSL example showing a specific pattern, verified to compile clean.</summary>
public sealed record MinimalExample(string Title, string Description, string DslSnippet);

/// <summary>
/// Quickstart catalog: the entry point for AI agents beginning to work with Precept.
/// Contains a brief product description, core concepts, tool guide, and minimal examples.
/// All <see cref="MinimalExample.DslSnippet"/> values compile with zero diagnostics.
/// </summary>
public static class QuickstartCatalog
{
    /// <summary>
    /// A 2–3 sentence description of what Precept is and what it does.
    /// </summary>
    public static string WhatIsPrecept { get; } =
        "Precept is a domain integrity engine for .NET — a DSL runtime that governs how a " +
        "business entity's data evolves under business rules across its lifecycle, making " +
        "invalid configurations structurally impossible. You define states, events, transitions, " +
        "and field constraints in a '.precept' file; the runtime enforces them at every operation. " +
        "If an operation would violate a declared rule or constraint, the runtime rejects it before " +
        "any state change occurs.";

    /// <summary>
    /// The core compile-time guarantee Precept provides.
    /// </summary>
    public static string CoreGuarantee { get; } =
        "If a transition compiles in Precept, it is guaranteed to be reachable from some state — " +
        "dead transitions are a compile error, and the runtime will never execute a path the " +
        "compiler has not verified.";

    /// <summary>
    /// The five core concepts every AI agent must understand before writing Precept DSL.
    /// </summary>
    public static IReadOnlyList<CoreConceptEntry> CoreConcepts { get; } =
    [
        new(
            "Precept",
            "The top-level definition of a business entity's lifecycle — its fields, states, events, and transition rules.",
            "precept LoanApplication"),

        new(
            "Fields",
            "Typed data attached to the entity; may be scalar, collection, computed, or optional; carry constraints such as 'nonnegative' and 'maxplaces'.",
            "field Amount as money in 'USD' default 0"),

        new(
            "States",
            "Named lifecycle positions the entity can occupy; one is 'initial', terminal ones end the lifecycle, and others are intermediate.",
            "state Draft initial"),

        new(
            "Events",
            "Named inputs that drive state transitions; may carry typed arguments used in guards and actions.",
            "event Submit(Applicant as string, Amount as number)"),

        new(
            "Transitions",
            "State changes triggered when an event fires in a given state, optionally guarded by a 'when' condition and followed by field actions.",
            "from Draft on Submit when Submit.Amount > 0 -> set RequestedAmount = Submit.Amount -> transition UnderReview"),
    ];

    /// <summary>
    /// A guide to the 8 AI authoring MCP tools — when to call each one and what it returns.
    /// </summary>
    public static IReadOnlyList<ToolGuideEntry> ToolGuide { get; } =
    [
        new(
            "precept_quickstart",
            "Call first when beginning a new Precept authoring session or when you need a high-level orientation to the language.",
            "Returns product description, core concepts, tool guide, and minimal verified examples — the minimum context needed to begin writing a precept."),

        new(
            "precept_syntax",
            "Call when you need construct shapes, action syntax, outcome keywords, operator precedence, or expression grammar rules.",
            "Returns the constructs catalog, actions catalog, operators catalog, and grammar meta-rules — everything needed to form syntactically correct statements."),

        new(
            "precept_types",
            "Call when you need type names, field modifiers (nonnegative, maxplaces), qualifiers (in 'USD', of 'kg'), or the proof requirements a modifier implies.",
            "Returns the types catalog and modifiers catalog with traits, widening rules, and qualifier/accessor metadata."),

        new(
            "precept_operations",
            "Call when you need to know which operators are legal for a given type pair, their precedence, and their associativity.",
            "Returns the operators catalog with binding power, associativity, type applicability, and precedence table."),

        new(
            "precept_proofs",
            "Call when writing guards (when clauses) or ensure constraints to understand proof obligations, runtime faults, and what the proof engine verifies at compile time.",
            "Returns the constraints catalog and fault codes with proof requirements, prevention relationships, and suggestion sources."),

        new(
            "precept_patterns",
            "Call to see compile-verified examples of common multi-construct patterns and anti-patterns before writing your first draft of a precept.",
            "Returns CommonPatterns (8 verified examples) and AntiPatterns (3 common mistakes with corrections) from the SyntaxReference catalog."),

        new(
            "precept_diagnostic",
            "Call with a PRE-code (e.g., PRE0017) when the compiler returns an error you do not understand.",
            "Returns the diagnostic metadata for that code: trigger condition, recovery steps, and before/after examples showing how to fix it."),

        new(
            "precept_domains",
            "Call when working with money, quantities, units, currencies, or time — to see valid currency codes, unit identifiers, dimension names, and temporal types.",
            "Returns the domains catalog including currencies (ISO 4217), UCUM tier-1 units, dimensions, and temporal unit metadata."),
    ];

    /// <summary>
    /// Three minimal DSL examples that compile with zero diagnostics, showing the simplest
    /// valid precept shapes an AI agent can use as a starting point.
    /// </summary>
    public static IReadOnlyList<MinimalExample> MinimalExamples { get; } =
    [
        new(
            "Simplest valid precept",
            "A precept with no fields, no states, and no events. Valid and compiles clean. Useful as a starting scaffold.",
            "precept Example"),

        new(
            "Stateless data object",
            "A precept with typed fields and constraints but no lifecycle states or events. Governs what a valid data configuration looks like.",
            """
            precept FeeSchedule

            field BaseFee as decimal default 0 nonnegative maxplaces 2 writable
            field DiscountPercent as decimal default 0 nonnegative max 100 maxplaces 2 writable
            field TaxRate as decimal default 0.1 nonnegative maxplaces 4
            """),

        new(
            "Minimal lifecycle",
            "The absolute minimum stateful precept: one initial state, one terminal state, one event, and one transition.",
            """
            precept Order

            state Draft initial
            state Complete terminal

            event Submit

            from Draft on Submit
                -> transition Complete
            """),
    ];
}
