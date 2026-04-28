namespace Precept.Language;

/// <summary>
/// Catalog of grammar constructs — every declaration shape the parser
/// can produce. Source of truth for MCP vocabulary, LS completions
/// (context-sensitive construct suggestions), and parser validation.
/// </summary>
public static class Constructs
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Shared slot instances (reused across construct entries)
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly ConstructSlot SlotIdentifierList    = new(ConstructSlotKind.IdentifierList);
    private static readonly ConstructSlot SlotTypeExpression    = new(ConstructSlotKind.TypeExpression);
    private static readonly ConstructSlot SlotModifierList      = new(ConstructSlotKind.ModifierList,       IsRequired: false);
    private static readonly ConstructSlot SlotStateModifierList = new(ConstructSlotKind.StateModifierList,  IsRequired: false);
    private static readonly ConstructSlot SlotArgumentList      = new(ConstructSlotKind.ArgumentList,       IsRequired: false);
    private static readonly ConstructSlot SlotComputeExpression = new(ConstructSlotKind.ComputeExpression,  IsRequired: false);
    private static readonly ConstructSlot SlotGuardClause       = new(ConstructSlotKind.GuardClause,        IsRequired: false, Description: "when expression");
    private static readonly ConstructSlot SlotActionChain       = new(ConstructSlotKind.ActionChain,        IsRequired: false);
    private static readonly ConstructSlot SlotOutcome           = new(ConstructSlotKind.Outcome);
    private static readonly ConstructSlot SlotStateTarget       = new(ConstructSlotKind.StateTarget);
    private static readonly ConstructSlot SlotOptStateTarget    = new(ConstructSlotKind.StateTarget,        IsRequired: false);
    private static readonly ConstructSlot SlotEventTarget       = new(ConstructSlotKind.EventTarget);
    private static readonly ConstructSlot SlotEnsureClause      = new(ConstructSlotKind.EnsureClause);
    private static readonly ConstructSlot SlotBecauseClause     = new(ConstructSlotKind.BecauseClause);
    private static readonly ConstructSlot SlotAccessModeKeyword = new(ConstructSlotKind.AccessModeKeyword);
    private static readonly ConstructSlot SlotFieldTarget       = new(ConstructSlotKind.FieldTarget);

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
            [],
            [SlotIdentifierList],
            TokenKind.Precept),

        ConstructKind.FieldDeclaration => new(
            kind,
            "field declaration",
            "Declares one or more typed fields with optional modifiers and a computed expression",
            "field amount as money nonnegative",
            [],
            [SlotIdentifierList, SlotTypeExpression, SlotModifierList, SlotComputeExpression],
            TokenKind.Field),

        ConstructKind.StateDeclaration => new(
            kind,
            "state declaration",
            "Declares one or more lifecycle states with optional state modifiers",
            "state Draft initial, Submitted, Approved terminal success",
            [],
            [SlotIdentifierList, SlotStateModifierList],
            TokenKind.State),

        ConstructKind.EventDeclaration => new(
            kind,
            "event declaration",
            "Declares one or more named events with optional arguments and the initial modifier",
            "event Submit(approver as string)",
            [],
            [SlotIdentifierList, SlotArgumentList],
            TokenKind.Event),

        ConstructKind.RuleDeclaration => new(
            kind,
            "rule declaration",
            "Declares a data-truth constraint with a guard and reason",
            "rule amount > 0 because \"Amount must be positive\"",
            [],
            [SlotGuardClause, SlotBecauseClause],
            TokenKind.Rule),

        ConstructKind.TransitionRow => new(
            kind,
            "transition row",
            "State-to-state transition with guard, actions, and outcome",
            "from Draft on Submit -> set reviewer = approver -> transition Submitted",
            [],
            [SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotActionChain, SlotOutcome],
            TokenKind.From),

        ConstructKind.StateEnsure => new(
            kind,
            "state ensure",
            "State-scoped constraint that must hold on entry, exit, or while in a state",
            "in Approved ensure amount > 0 because \"Approved amount must be positive\"",
            [ConstructKind.StateDeclaration],
            [SlotStateTarget, SlotEnsureClause],
            TokenKind.In),

        ConstructKind.AccessMode => new(
            kind,
            "access mode",
            "Declares field write access per state via 'in' scope (write, read, or omit), or 'write all' at root level for stateless precepts",
            "in Draft write Amount",
            [],
            [SlotOptStateTarget, SlotAccessModeKeyword, SlotFieldTarget],
            TokenKind.Write),

        ConstructKind.StateAction=> new(
            kind,
            "state action",
            "Entry or exit hook that fires actions when entering or leaving a state",
            "to Submitted -> set submittedAt = now()",
            [ConstructKind.StateDeclaration],
            [SlotStateTarget, SlotActionChain],
            TokenKind.To),

        ConstructKind.EventEnsure => new(
            kind,
            "event ensure",
            "Event-scoped constraint that must hold when an event fires",
            "on Submit ensure reviewer != \"\" because \"Reviewer required\"",
            [ConstructKind.EventDeclaration],
            [SlotEventTarget, SlotEnsureClause],
            TokenKind.On),

        ConstructKind.EventHandler => new(
            kind,
            "event handler",
            "Event handler with actions but no state transitions (stateless precepts)",
            "on UpdateName -> set name = newName",
            [],
            [SlotEventTarget, SlotActionChain],
            TokenKind.On),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ConstructKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every ConstructMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ConstructMeta> All { get; } =
        Enum.GetValues<ConstructKind>().Select(GetMeta).ToArray();
}
