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

    private static readonly ConstructSlot SlotIdentifierList    = new(ConstructSlotKind.IdentifierList,    IsList: true, ItemIntroducerToken: TokenKind.Comma);
    private static readonly ConstructSlot SlotTypeExpression    = new(ConstructSlotKind.TypeExpression,    Vocabulary: SlotVocabulary.TypeKeywords);
    private static readonly ConstructSlot SlotModifierList      = new(ConstructSlotKind.ModifierList,       IsRequired: false, Vocabulary: SlotVocabulary.Modifiers);
    private static readonly ConstructSlot SlotStateEntryList    = new(ConstructSlotKind.StateEntryList,    IsList: true, ItemIntroducerToken: TokenKind.Comma, Vocabulary: SlotVocabulary.StateEntryNames);
    private static readonly ConstructSlot SlotArgumentList      = new(ConstructSlotKind.ArgumentList,       IsRequired: false);
    private static readonly ConstructSlot SlotComputeExpression    = new(ConstructSlotKind.ComputeExpression,  IsRequired: false, TerminationTokens: [], Vocabulary: SlotVocabulary.Expression);
    private static readonly ConstructSlot SlotGuardClause          = new(ConstructSlotKind.GuardClause,        IsRequired: false, Description: "when expression", TerminationTokens: [TokenKind.Because, TokenKind.Arrow], Vocabulary: SlotVocabulary.Expression);
    private static readonly ConstructSlot SlotPreVerbGuardEnsure   = new(ConstructSlotKind.GuardClause,        IsRequired: false, Description: "when expression", TerminationTokens: [TokenKind.Ensure], Vocabulary: SlotVocabulary.Expression);
    private static readonly ConstructSlot SlotPreVerbGuardArrow    = new(ConstructSlotKind.GuardClause,        IsRequired: false, Description: "when expression", TerminationTokens: [TokenKind.Arrow], Vocabulary: SlotVocabulary.Expression);
    private static readonly ConstructSlot SlotPreVerbGuardModify   = new(ConstructSlotKind.GuardClause,        IsRequired: false, Description: "when expression", TerminationTokens: [TokenKind.Modify], Vocabulary: SlotVocabulary.Expression);
    private static readonly ConstructSlot SlotActionChain          = new(ConstructSlotKind.ActionChain,        IsRequired: false, IsChainable: true, ItemIntroducerToken: TokenKind.Arrow, Vocabulary: SlotVocabulary.ActionVerbs);
    private static readonly ConstructSlot SlotOutcome           = new(ConstructSlotKind.Outcome,           Vocabulary: SlotVocabulary.OutcomeKeywords);
    private static readonly ConstructSlot SlotStateTarget       = new(ConstructSlotKind.StateTarget,        Description: "single state name, `any` for all states, or comma-delimited list of state names", IsList: true, ItemIntroducerToken: TokenKind.Comma, Vocabulary: SlotVocabulary.StateNames);
    private static readonly ConstructSlot SlotOptStateTarget    = new(ConstructSlotKind.StateTarget,        IsRequired: false, IsList: true, ItemIntroducerToken: TokenKind.Comma, Vocabulary: SlotVocabulary.StateNames);
    private static readonly ConstructSlot SlotEventTarget       = new(ConstructSlotKind.EventTarget,       Vocabulary: SlotVocabulary.EventNames);
    private static readonly ConstructSlot SlotEnsureClause      = new(ConstructSlotKind.EnsureClause,       TerminationTokens: [TokenKind.Because], Vocabulary: SlotVocabulary.Expression);
    private static readonly ConstructSlot SlotBecauseClause     = new(ConstructSlotKind.BecauseClause);
    private static readonly ConstructSlot SlotOptBecauseClause  = new(ConstructSlotKind.BecauseClause,      IsRequired: false);
    private static readonly ConstructSlot SlotAccessModeKeyword = new(ConstructSlotKind.AccessModeKeyword, Vocabulary: SlotVocabulary.AccessModes);
    private static readonly ConstructSlot SlotFieldTarget       = new(ConstructSlotKind.FieldTarget,       IsList: true, ItemIntroducerToken: TokenKind.Comma, Vocabulary: SlotVocabulary.FieldNames);
    private static readonly ConstructSlot SlotRuleExpression    = new(ConstructSlotKind.RuleExpression,     TerminationTokens: [TokenKind.When, TokenKind.Because], Vocabulary: SlotVocabulary.Expression);
    private static readonly ConstructSlot SlotInitialMarker     = new(ConstructSlotKind.InitialMarker,      IsRequired: false);
    private static readonly ConstructSlot SlotRejectClause      = new(ConstructSlotKind.RejectClause,      Vocabulary: SlotVocabulary.RejectReason);
    private static readonly ConstructSlot SlotSuccessOutcome    = new(ConstructSlotKind.SuccessOutcome,    Vocabulary: SlotVocabulary.OutcomeKeywords);

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
            [new(TokenKind.Precept)],
            RoutingFamily.Header,
            SnippetTemplate: "precept ${1:Name}",
            IsOutlineNode: true,
            OutlineSymbolTag: "Module"),

        ConstructKind.FieldDeclaration => new(
            kind,
            "field declaration",
            "Declares one or more typed fields with optional modifiers and a computed expression",
            "field amount as money nonnegative",
            [],
            [SlotIdentifierList, SlotTypeExpression, SlotModifierList, SlotComputeExpression],
            [new(TokenKind.Field)],
            RoutingFamily.Direct,
            SnippetTemplate: "field ${1:Name} as ${2:type}",
            ModifierDomain: ModifierDomain.Field,
            IsOutlineNode: true,
            OutlineSymbolTag: "Property"),

        ConstructKind.StateDeclaration => new(
            kind,
            "state declaration",
            "Declares one or more lifecycle states with optional state modifiers",
            "state Draft initial, Submitted, Approved terminal success",
            [],
            [SlotStateEntryList],
            [new(TokenKind.State)],
            RoutingFamily.Direct,
            SnippetTemplate: "state ${1:Name}",
            ModifierDomain: ModifierDomain.State,
            IsOutlineNode: true,
            OutlineSymbolTag: "Enum"),

        ConstructKind.EventDeclaration => new(
            kind,
            "event declaration",
            "Declares one or more named events with optional arguments and the initial modifier",
            "event Submit(approver as string)",
            [],
            [SlotIdentifierList, SlotArgumentList, SlotInitialMarker],
            [new(TokenKind.Event)],
            RoutingFamily.Direct,
            SnippetTemplate: "event ${1:Name}",
            ModifierDomain: ModifierDomain.Event,
            IsOutlineNode: true,
            OutlineSymbolTag: "Function"),

        ConstructKind.RuleDeclaration => new(
            kind,
            "rule declaration",
            "Declares a data-truth constraint with a guard and reason",
            "rule amount > 0 because \"Amount must be positive\"",
            [],
            [SlotRuleExpression, SlotGuardClause, SlotBecauseClause],
            [new(TokenKind.Rule)],
            RoutingFamily.Direct,
            SnippetTemplate: "rule ${1:expression} because \"${2:reason}\"",
            IsOutlineNode: true,
            OutlineSymbolTag: "Boolean"),

        ConstructKind.TransitionRow => new(
            kind,
            "transition row",
            "State-to-state transition with guard, actions, and outcome",
            "from Draft on Submit -> set reviewer = approver -> transition Submitted",
            [],
            [SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotActionChain, SlotOutcome],
            [new(TokenKind.From, [TokenKind.On])],
            RoutingFamily.StateScoped),

        ConstructKind.StateEnsure => new(
            kind,
            "state ensure",
            "State-scoped constraint that must hold on entry, exit, or while in a state",
            "in Approved ensure amount > 0 because \"Approved amount must be positive\"",
            [ConstructKind.StateDeclaration],
            [SlotStateTarget, SlotPreVerbGuardEnsure, SlotEnsureClause, SlotOptBecauseClause],
            [new(TokenKind.In, [TokenKind.Ensure]), new(TokenKind.To, [TokenKind.Ensure]), new(TokenKind.From, [TokenKind.Ensure])],
            RoutingFamily.StateScoped),

        ConstructKind.AccessMode => new(
            kind,
            "access mode",
            "Declares field access per state: 'in State [when Guard] modify Field readonly|editable'",
            "in Draft when IsOwner modify Amount editable",
            [],
            [SlotStateTarget, SlotPreVerbGuardModify, SlotFieldTarget, SlotAccessModeKeyword],
            [new(TokenKind.In, [TokenKind.Modify])],
            RoutingFamily.StateScoped,
            ModifierDomain: ModifierDomain.Access),

        ConstructKind.OmitDeclaration => new(
            kind,
            "omit declaration",
            "Structurally excludes a field from a state: 'in State omit Field' (no guard — exclusion is unconditional)",
            "in Draft omit InternalNotes",
            [],
            [SlotStateTarget, SlotFieldTarget],
            [new(TokenKind.In, [TokenKind.Omit])],
            RoutingFamily.StateScoped),

        ConstructKind.StateAction=> new(
            kind,
            "state action",
            "Entry or exit hook that fires actions when entering or leaving a state",
            "to Submitted -> set submittedAt = now()",
            [ConstructKind.StateDeclaration],
            [SlotStateTarget, SlotPreVerbGuardArrow, SlotActionChain],
            [new(TokenKind.To, [TokenKind.Arrow]), new(TokenKind.From, [TokenKind.Arrow])],
            RoutingFamily.StateScoped),

        ConstructKind.EventEnsure => new(
            kind,
            "event ensure",
            "Event-scoped constraint that must hold when an event fires",
            "on Submit ensure reviewer != \"\" because \"Reviewer required\"",
            [ConstructKind.EventDeclaration],
            [SlotEventTarget, SlotPreVerbGuardEnsure, SlotEnsureClause, SlotOptBecauseClause],
            [new(TokenKind.On, [TokenKind.Ensure])],
            RoutingFamily.EventScoped),

        ConstructKind.EventRow => new(
            kind,
            "event row",
            "Event handler with optional guard and actions — serves both stateless handlers and construction rows; type checker classifies construction via resolvedEvent.IsInitial",
            "on UpdateName -> set name = newName",
            [],
            [SlotEventTarget, SlotPreVerbGuardArrow, SlotActionChain],
            [new(TokenKind.On, [TokenKind.Arrow])],
            RoutingFamily.EventScoped),

        ConstructKind.ConstructionRow => new(
            kind,
            "construction row",
            "Construction success path (Slice 8b: no longer produced by parser; all on-rows parse as EventRow and are promoted by the type checker via resolvedEvent.IsInitial)",
            "on Create -> set status = \"active\"",
            [],
            [SlotEventTarget, SlotPreVerbGuardArrow, SlotActionChain],
            [],
            RoutingFamily.EventScoped),

        ConstructKind.ConstructionRowReject => new(
            kind,
            "construction row reject",
            "Reject path for on-rows: refuses event with a reason (produced by ResolveRejectVariant from EventRow, not via direct disambiguation)",
            "on Create when amount <= 0 -> reject \"Amount must be positive\"",
            [],
            [SlotEventTarget, SlotPreVerbGuardArrow, SlotRejectClause],
            [],
            RoutingFamily.EventScoped),

        ConstructKind.TransitionRowReject => new(
            kind,
            "transition row reject",
            "Transition reject path: refuses event with a reason instead of transitioning",
            "from Draft on Submit when reviewer == \"\" -> reject \"Reviewer required\"",
            [],
            [SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotRejectClause],
            [new(TokenKind.From, [TokenKind.On, TokenKind.Reject])],
            RoutingFamily.StateScoped),

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
