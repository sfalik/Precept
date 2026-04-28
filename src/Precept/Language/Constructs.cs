using System.Collections.Frozen;
using System.Collections.Immutable;

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
    private static readonly ConstructSlot SlotRuleExpression    = new(ConstructSlotKind.RuleExpression);

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
            [new(TokenKind.Precept)]),

        ConstructKind.FieldDeclaration => new(
            kind,
            "field declaration",
            "Declares one or more typed fields with optional modifiers and a computed expression",
            "field amount as money nonnegative",
            [],
            [SlotIdentifierList, SlotTypeExpression, SlotModifierList, SlotComputeExpression],
            [new(TokenKind.Field)]),

        ConstructKind.StateDeclaration => new(
            kind,
            "state declaration",
            "Declares one or more lifecycle states with optional state modifiers",
            "state Draft initial, Submitted, Approved terminal success",
            [],
            [SlotIdentifierList, SlotStateModifierList],
            [new(TokenKind.State)]),

        ConstructKind.EventDeclaration => new(
            kind,
            "event declaration",
            "Declares one or more named events with optional arguments and the initial modifier",
            "event Submit(approver as string)",
            [],
            [SlotIdentifierList, SlotArgumentList],
            [new(TokenKind.Event)]),

        ConstructKind.RuleDeclaration => new(
            kind,
            "rule declaration",
            "Declares a data-truth constraint with a guard and reason",
            "rule amount > 0 because \"Amount must be positive\"",
            [],
            [SlotRuleExpression, SlotGuardClause, SlotBecauseClause],
            [new(TokenKind.Rule)]),

        ConstructKind.TransitionRow => new(
            kind,
            "transition row",
            "State-to-state transition with guard, actions, and outcome",
            "from Draft on Submit -> set reviewer = approver -> transition Submitted",
            [],
            [SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotActionChain, SlotOutcome],
            [new(TokenKind.From, [TokenKind.On])]),

        ConstructKind.StateEnsure => new(
            kind,
            "state ensure",
            "State-scoped constraint that must hold on entry, exit, or while in a state",
            "in Approved ensure amount > 0 because \"Approved amount must be positive\"",
            [ConstructKind.StateDeclaration],
            [SlotStateTarget, SlotEnsureClause],
            [new(TokenKind.In, [TokenKind.Ensure]), new(TokenKind.To, [TokenKind.Ensure]), new(TokenKind.From, [TokenKind.Ensure])]),

        ConstructKind.AccessMode => new(
            kind,
            "access mode",
            "Declares field access per state: 'in State modify Field readonly|editable [when Guard]'",
            "in Draft modify Amount editable",
            [],
            [SlotStateTarget, SlotFieldTarget, SlotAccessModeKeyword, SlotGuardClause],
            [new(TokenKind.In, [TokenKind.Modify])]),

        ConstructKind.OmitDeclaration => new(
            kind,
            "omit declaration",
            "Structurally excludes a field from a state: 'in State omit Field' (no guard — exclusion is unconditional)",
            "in Draft omit InternalNotes",
            [],
            [SlotStateTarget, SlotFieldTarget],
            [new(TokenKind.In, [TokenKind.Omit])]),

        ConstructKind.StateAction=> new(
            kind,
            "state action",
            "Entry or exit hook that fires actions when entering or leaving a state",
            "to Submitted -> set submittedAt = now()",
            [ConstructKind.StateDeclaration],
            [SlotStateTarget, SlotActionChain],
            [new(TokenKind.To, [TokenKind.Arrow]), new(TokenKind.From, [TokenKind.Arrow])]),

        ConstructKind.EventEnsure => new(
            kind,
            "event ensure",
            "Event-scoped constraint that must hold when an event fires",
            "on Submit ensure reviewer != \"\" because \"Reviewer required\"",
            [ConstructKind.EventDeclaration],
            [SlotEventTarget, SlotEnsureClause],
            [new(TokenKind.On, [TokenKind.Ensure])]),

        ConstructKind.EventHandler => new(
            kind,
            "event handler",
            "Event handler with actions but no state transitions (stateless precepts)",
            "on UpdateName -> set name = newName",
            [],
            [SlotEventTarget, SlotActionChain],
            [new(TokenKind.On, [TokenKind.Arrow])]),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ConstructKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every ConstructMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ConstructMeta> All { get; } =
        Enum.GetValues<ConstructKind>().Select(GetMeta).ToArray();

    // ════════════════════════════════════════════════════════════════════════════
    //  Derived indexes
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps each leading token to the constructs that can begin with it,
    /// along with the disambiguation entry for each.
    /// </summary>
    public static IReadOnlyDictionary<TokenKind, ImmutableArray<(ConstructKind Kind, DisambiguationEntry Entry)>>
        ByLeadingToken { get; } = All
            .SelectMany(meta => meta.Entries.Select(entry => (meta.Kind, entry)))
            .GroupBy(t => t.entry.LeadingToken)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());

    /// <summary>
    /// The set of all tokens that can begin a construct declaration.
    /// </summary>
    public static FrozenSet<TokenKind> LeadingTokens { get; } = All
        .SelectMany(m => m.Entries)
        .Select(e => e.LeadingToken)
        .ToFrozenSet();
}
