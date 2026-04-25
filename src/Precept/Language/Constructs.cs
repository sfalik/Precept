namespace Precept.Language;

/// <summary>
/// Catalog of grammar constructs — every declaration shape the parser
/// can produce. Source of truth for MCP vocabulary, LS completions
/// (context-sensitive construct suggestions), and parser validation.
/// </summary>
public static class Constructs
{
    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
    {
        ConstructKind.PreceptHeader => new(
            kind,
            "precept header",
            "File-level header that names the precept",
            "precept LoanApplication",
            []),

        ConstructKind.FieldDeclaration => new(
            kind,
            "field declaration",
            "Declares one or more typed fields with optional modifiers and a computed expression",
            "field amount as money nonnegative",
            []),

        ConstructKind.StateDeclaration => new(
            kind,
            "state declaration",
            "Declares one or more lifecycle states with optional state modifiers",
            "state Draft initial, Submitted, Approved terminal success",
            []),

        ConstructKind.EventDeclaration => new(
            kind,
            "event declaration",
            "Declares one or more named events with optional arguments and the initial modifier",
            "event Submit(approver as string)",
            []),

        ConstructKind.RuleDeclaration => new(
            kind,
            "rule declaration",
            "Declares a data-truth constraint with a guard and reason",
            "rule amount > 0 because \"Amount must be positive\"",
            []),

        ConstructKind.TransitionRow => new(
            kind,
            "transition row",
            "State-to-state transition with guard, actions, and outcome",
            "from Draft on Submit -> set reviewer = approver -> transition Submitted",
            []),

        ConstructKind.StateEnsure => new(
            kind,
            "state ensure",
            "State-scoped constraint that must hold on entry, exit, or while in a state",
            "in Approved ensure amount > 0 because \"Approved amount must be positive\"",
            [ConstructKind.StateDeclaration]),

        ConstructKind.AccessMode => new(
            kind,
            "access mode",
            "Declares field visibility (write, read, or omit) optionally scoped to states",
            "in Draft write amount",
            [ConstructKind.StateDeclaration]),

        ConstructKind.StateAction => new(
            kind,
            "state action",
            "Entry or exit hook that fires actions when entering or leaving a state",
            "to Submitted -> set submittedAt = now()",
            [ConstructKind.StateDeclaration]),

        ConstructKind.EventEnsure => new(
            kind,
            "event ensure",
            "Event-scoped constraint that must hold when an event fires",
            "on Submit ensure reviewer != \"\" because \"Reviewer required\"",
            [ConstructKind.EventDeclaration]),

        ConstructKind.StatelessHook => new(
            kind,
            "stateless event hook",
            "Event handler with actions but no state transitions (stateless precepts)",
            "on UpdateName -> set name = newName",
            []),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ConstructKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every ConstructMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ConstructMeta> All { get; } =
        Enum.GetValues<ConstructKind>().Select(GetMeta).ToArray();
}
