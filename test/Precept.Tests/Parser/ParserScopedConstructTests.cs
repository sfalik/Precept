using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Tests for the Slice 2 scoped construct family:
///   TransitionRow, StateEnsure, AccessMode, OmitDeclaration,
///   StateAction, EventEnsure, EventHandler.
///
/// Complements Slice 1 direct construct tests in ParserDirectConstructTests.cs.
/// Does NOT duplicate RED-P tests already in EnsureBecauseClauseSlotTests.cs:
///   - Parser_StateEnsure_WithBecause_ProducesBecauseClauseSlotAtIndex2
///   - Parser_StateEnsure_WithoutBecause_HasNoBecauseClauseSlot
///   - Parser_StateEnsure_BecauseMissingString_ProducesParseError
///   - Parser_EventEnsure_WithBecause_ProducesBecauseClauseSlotAtIndex2
///   - Parser_EventEnsure_WithoutBecause_HasNoBecauseClauseSlot
///   - Parser_EventEnsure_BecauseMissingString_ProducesParseError
///
/// Test status at time of writing:
///   GREEN  — catalog metadata tests; pass immediately (no parser dependency)
///   RED-P  — parser behavioral tests; red until Parser.Parse replaces the stub
/// </summary>
public class ParserScopedConstructTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  §1. Catalog metadata — GREEN
    //  These tests verify slot structure, routing families, and disambiguation
    //  entries in the Constructs catalog.  No Lexer / Parser dependency.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ConstructKind.TransitionRow,    5)]
    [InlineData(ConstructKind.StateEnsure,      3)]
    [InlineData(ConstructKind.AccessMode,       4)]
    [InlineData(ConstructKind.OmitDeclaration,  2)]
    [InlineData(ConstructKind.StateAction,      2)]
    [InlineData(ConstructKind.EventEnsure,      3)]
    [InlineData(ConstructKind.EventHandler,     2)]
    public void ScopedConstruct_CatalogSlotCount_MatchesExpectedCount(
        ConstructKind kind, int expectedCount)
    {
        // GREEN — catalog only.
        var meta = Constructs.GetMeta(kind);
        meta.Slots.Should().HaveCount(expectedCount,
            $"{kind} must declare exactly {expectedCount} slot(s) in the Constructs catalog");
    }

    [Theory]
    [InlineData(ConstructKind.TransitionRow)]
    [InlineData(ConstructKind.StateEnsure)]
    [InlineData(ConstructKind.AccessMode)]
    [InlineData(ConstructKind.OmitDeclaration)]
    [InlineData(ConstructKind.StateAction)]
    public void StateScopedConstruct_RoutingFamily_IsStateScoped(ConstructKind kind)
    {
        // GREEN — all state-scoped constructs must belong to the StateScoped routing family.
        var meta = Constructs.GetMeta(kind);
        meta.RoutingFamily.Should().Be(RoutingFamily.StateScoped,
            $"{kind} must belong to RoutingFamily.StateScoped");
    }

    [Theory]
    [InlineData(ConstructKind.EventEnsure)]
    [InlineData(ConstructKind.EventHandler)]
    public void EventScopedConstruct_RoutingFamily_IsEventScoped(ConstructKind kind)
    {
        // GREEN
        var meta = Constructs.GetMeta(kind);
        meta.RoutingFamily.Should().Be(RoutingFamily.EventScoped,
            $"{kind} must belong to RoutingFamily.EventScoped");
    }

    [Fact]
    public void TransitionRow_CatalogSlotOrder_IsStateTarget_EventTarget_Guard_Actions_Outcome()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.TransitionRow).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget,   "Slots[0]: source state");
        slots[1].Kind.Should().Be(ConstructSlotKind.EventTarget,   "Slots[1]: triggering event");
        slots[2].Kind.Should().Be(ConstructSlotKind.GuardClause,   "Slots[2]: optional when-guard");
        slots[3].Kind.Should().Be(ConstructSlotKind.ActionChain,   "Slots[3]: optional action chain");
        slots[4].Kind.Should().Be(ConstructSlotKind.Outcome,       "Slots[4]: required outcome");
    }

    [Fact]
    public void TransitionRow_RequiredAndOptionalSlots_AreCorrect()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.TransitionRow).Slots;
        slots[0].IsRequired.Should().BeTrue("StateTarget is required on TransitionRow");
        slots[1].IsRequired.Should().BeTrue("EventTarget is required on TransitionRow");
        slots[2].IsRequired.Should().BeFalse("GuardClause is optional on TransitionRow");
        slots[3].IsRequired.Should().BeFalse("ActionChain is optional on TransitionRow");
        slots[4].IsRequired.Should().BeTrue("Outcome is required on TransitionRow");
    }

    [Fact]
    public void TransitionRow_CatalogEntry_LeadingToken_IsFrom_WithDisambiguationToken_On()
    {
        // GREEN — TransitionRow has exactly one entry: from + on disambiguation.
        var meta = Constructs.GetMeta(ConstructKind.TransitionRow);
        meta.Entries.Should().ContainSingle("TransitionRow has a single leading-token entry");
        var entry = meta.Entries[0];
        entry.LeadingToken.Should().Be(TokenKind.From, "TransitionRow leads with 'from'");
        entry.DisambiguationTokens.Should().NotBeNull();
        entry.DisambiguationTokens!.Value.Should().Contain(TokenKind.On,
            "TransitionRow is disambiguated by 'on' at peek(2): 'from State on Event'");
    }

    [Fact]
    public void AccessMode_CatalogSlotOrder_IsStateTarget_FieldTarget_AccessModeKeyword_Guard()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.AccessMode).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget,      "Slots[0]: state scope");
        slots[1].Kind.Should().Be(ConstructSlotKind.FieldTarget,      "Slots[1]: target field");
        slots[2].Kind.Should().Be(ConstructSlotKind.AccessModeKeyword,"Slots[2]: readonly or editable");
        slots[3].Kind.Should().Be(ConstructSlotKind.GuardClause,      "Slots[3]: optional when-guard");
    }

    [Fact]
    public void AccessMode_RequiredAndOptionalSlots_AreCorrect()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.AccessMode).Slots;
        slots[0].IsRequired.Should().BeTrue("StateTarget is required on AccessMode");
        slots[1].IsRequired.Should().BeTrue("FieldTarget is required on AccessMode");
        slots[2].IsRequired.Should().BeTrue("AccessModeKeyword is required on AccessMode");
        slots[3].IsRequired.Should().BeFalse("GuardClause is optional on AccessMode");
    }

    [Fact]
    public void OmitDeclaration_CatalogSlotOrder_IsStateTarget_FieldTarget()
    {
        // GREEN — OmitDeclaration has no optional slots.
        var slots = Constructs.GetMeta(ConstructKind.OmitDeclaration).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget, "Slots[0]: state scope");
        slots[1].Kind.Should().Be(ConstructSlotKind.FieldTarget, "Slots[1]: excluded field");
        slots[0].IsRequired.Should().BeTrue("StateTarget is required on OmitDeclaration");
        slots[1].IsRequired.Should().BeTrue("FieldTarget is required on OmitDeclaration");
    }

    [Fact]
    public void StateAction_CatalogSlotOrder_IsStateTarget_ActionChain()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.StateAction).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget, "Slots[0]: entry/exit state");
        slots[1].Kind.Should().Be(ConstructSlotKind.ActionChain, "Slots[1]: actions to fire");
    }

    [Fact]
    public void StateAction_CatalogEntries_CoversToAndFrom()
    {
        // GREEN — StateAction can lead with 'to' (entry) or 'from' (exit).
        var meta = Constructs.GetMeta(ConstructKind.StateAction);
        meta.Entries.Should().HaveCount(2,
            "StateAction has two entries: 'to State ->' and 'from State ->'");
        var leadingTokens = meta.Entries.Select(e => e.LeadingToken).ToList();
        leadingTokens.Should().Contain(TokenKind.To,   "'to' leads StateAction for entry hooks");
        leadingTokens.Should().Contain(TokenKind.From, "'from' leads StateAction for exit hooks");
        foreach (var entry in meta.Entries)
        {
            entry.DisambiguationTokens.Should().NotBeNull();
            entry.DisambiguationTokens!.Value.Should().Contain(TokenKind.Arrow,
                $"StateAction entry with '{entry.LeadingToken}' is disambiguated by '->' at peek(2)");
        }
    }

    [Fact]
    public void StateEnsure_CatalogEntries_CoversInToAndFrom()
    {
        // GREEN — StateEnsure can lead with 'in', 'to', or 'from'.
        var meta = Constructs.GetMeta(ConstructKind.StateEnsure);
        meta.Entries.Should().HaveCount(3,
            "StateEnsure has three entries: in, to, from — all disambiguated by 'ensure'");
        var leadingTokens = meta.Entries.Select(e => e.LeadingToken).ToList();
        leadingTokens.Should().Contain(TokenKind.In,   "'in State ensure' — invariant on state");
        leadingTokens.Should().Contain(TokenKind.To,   "'to State ensure' — on entry");
        leadingTokens.Should().Contain(TokenKind.From, "'from State ensure' — on exit");
        foreach (var entry in meta.Entries)
        {
            entry.DisambiguationTokens.Should().NotBeNull();
            entry.DisambiguationTokens!.Value.Should().Contain(TokenKind.Ensure,
                $"StateEnsure entry with '{entry.LeadingToken}' is disambiguated by 'ensure'");
        }
    }

    [Fact]
    public void EventHandler_CatalogSlotOrder_IsEventTarget_ActionChain()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.EventHandler).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.EventTarget, "Slots[0]: event name");
        slots[1].Kind.Should().Be(ConstructSlotKind.ActionChain, "Slots[1]: optional actions");
        slots[1].IsRequired.Should().BeFalse("ActionChain is optional on EventHandler");
    }

    [Fact]
    public void EventHandler_CatalogSlots_DoNotInclude_OutcomeSlot()
    {
        // GREEN — EventHandler is stateless: it fires actions but causes no transition.
        var slots = Constructs.GetMeta(ConstructKind.EventHandler).Slots;
        slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.Outcome,
            "EventHandler must not declare an Outcome slot — it causes no state transition");
    }

    [Fact]
    public void EventHandler_CatalogSlots_DoNotInclude_StateTargetSlot()
    {
        // GREEN — EventHandler is not state-scoped; it has no source-state target.
        var slots = Constructs.GetMeta(ConstructKind.EventHandler).Slots;
        slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.StateTarget,
            "EventHandler must not declare a StateTarget slot — it is event-scoped, not state-scoped");
    }

    [Fact]
    public void EventEnsure_CatalogEntry_LeadingToken_IsOn_WithDisambiguationToken_Ensure()
    {
        // GREEN
        var meta = Constructs.GetMeta(ConstructKind.EventEnsure);
        meta.Entries.Should().ContainSingle("EventEnsure has a single leading-token entry");
        var entry = meta.Entries[0];
        entry.LeadingToken.Should().Be(TokenKind.On,
            "EventEnsure leads with 'on'");
        entry.DisambiguationTokens.Should().NotBeNull();
        entry.DisambiguationTokens!.Value.Should().Contain(TokenKind.Ensure,
            "EventEnsure is disambiguated by 'ensure' at peek(2): 'on Event ensure'");
    }

    [Fact]
    public void EventHandler_CatalogEntry_LeadingToken_IsOn_WithDisambiguationToken_Arrow()
    {
        // GREEN
        var meta = Constructs.GetMeta(ConstructKind.EventHandler);
        meta.Entries.Should().ContainSingle("EventHandler has a single leading-token entry");
        var entry = meta.Entries[0];
        entry.LeadingToken.Should().Be(TokenKind.On,
            "EventHandler leads with 'on'");
        entry.DisambiguationTokens.Should().NotBeNull();
        entry.DisambiguationTokens!.Value.Should().Contain(TokenKind.Arrow,
            "EventHandler is disambiguated by '->' at peek(2): 'on Event ->'");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §2. Lexer smoke tests — GREEN
    //  Ensures test source strings lex without errors so RED-P failures are
    //  parser failures, not malformed source strings.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    // TransitionRow variants
    [InlineData("from Draft on Submit -> transition Submitted")]
    [InlineData("from Draft on Submit when DocumentsVerified -> transition Submitted")]
    [InlineData("from Draft on Submit -> set amount = 1 -> transition Submitted")]
    [InlineData("from Draft on Submit -> no transition")]
    [InlineData("from Draft on Submit -> reject \"Reason\"")]
    // StateEnsure with different leading tokens (complementary; in-variant covered by EnsureBecauseClauseSlotTests)
    [InlineData("to Approved ensure amount > 0")]
    [InlineData("from Draft ensure amount > 0")]
    [InlineData("to Approved ensure amount > 0 because \"Approved amount must be positive\"")]
    [InlineData("from Draft ensure amount > 0 because \"Draft amount must be positive\"")]
    // AccessMode
    [InlineData("in Draft modify Amount editable")]
    [InlineData("in Draft modify Amount readonly")]
    [InlineData("in Draft modify Amount editable when DocumentsVerified")]
    // OmitDeclaration
    [InlineData("in Draft omit InternalNotes")]
    // StateAction
    [InlineData("to Submitted -> set submittedAt = 1")]
    [InlineData("from Draft -> set amount = 0")]
    // EventEnsure (without because — with-because covered by EnsureBecauseClauseSlotTests)
    [InlineData("on Submit ensure amount > 0")]
    // EventHandler
    [InlineData("on UpdateName -> set name = newName")]
    public void ScopedConstruct_LexesWithoutErrors(string source)
    {
        // GREEN — ensures test source strings are syntactically lexable.
        var stream = Lexer.Lex(source);
        stream.Diagnostics.Should().BeEmpty(
            $"'{source}' must lex cleanly; lex errors indicate a bad test source string");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §3. TransitionRow — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionRow_HappyPath_ProducesCorrectKind()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "a minimal TransitionRow (no guard, no actions) must parse without errors");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.TransitionRow,
            "exactly one TransitionRow must be produced");
    }

    [Fact]
    public void TransitionRow_HappyPath_StateTargetSlot_ContainsSourceStateName()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull("TransitionRow must be produced");

        var stateSlot = row!.Slots.OfType<StateTargetSlot>().FirstOrDefault();
        stateSlot.Should().NotBeNull("Slots must contain a StateTargetSlot");
        stateSlot!.StateName.Should().Be("Draft",
            "StateTargetSlot must carry the source state name 'Draft'");
    }

    [Fact]
    public void TransitionRow_HappyPath_EventTargetSlot_ContainsEventName()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull();

        var eventSlot = row!.Slots.OfType<EventTargetSlot>().FirstOrDefault();
        eventSlot.Should().NotBeNull("Slots must contain an EventTargetSlot");
        eventSlot!.EventName.Should().Be("Submit",
            "EventTargetSlot must carry the event name 'Submit'");
    }

    [Fact]
    public void TransitionRow_FromAny_UsesWildcardStateTarget_AndPreservesTransitionOutcome()
    {
        var tokens = Lexer.Lex("from any on Submit -> transition Approved");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty();

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Slots.OfType<StateTargetSlot>().Single().StateName.Should().Be("any");
        row.Slots.OfType<OutcomeSlot>().Single().Outcome.Should().BeOfType<TransitionOutcome>()
            .Which.StateName.Should().Be("Approved");
    }

    [Fact]
    public void TransitionRow_HappyPath_OutcomeSlot_IsPresent()
    {
        // RED-P: Parser is a stub. Outcome is a required slot on TransitionRow.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull();

        row!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.Outcome,
            "Outcome slot is required on TransitionRow and must always be present");
    }

    [Fact]
    public void TransitionRow_WithoutGuardOrActions_GuardClauseSlot_IsAbsent()
    {
        // RED-P: Absent optional GuardClause slot must not be materialized.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull();

        row!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.GuardClause,
            "optional GuardClause slot must be absent when no 'when' clause is present");
    }

    [Fact]
    public void TransitionRow_WithoutGuardOrActions_ActionChainSlot_IsAbsent()
    {
        // RED-P: Absent optional ActionChain slot must not be materialized.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull();

        row!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.ActionChain,
            "optional ActionChain slot must be absent when no actions are present");
    }

    [Fact]
    public void TransitionRow_WithGuard_GuardClauseSlot_IsPresent()
    {
        // RED-P: When a 'when' clause is present, GuardClause slot must be materialized.
        var tokens = Lexer.Lex("from Draft on Submit when DocumentsVerified -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("TransitionRow with guard must parse without errors");

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull();

        row!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.GuardClause,
            "optional GuardClause slot must be materialized when a 'when' clause is present");
    }

    [Fact]
    public void TransitionRow_WithActions_ActionChainSlot_IsPresent()
    {
        // RED-P: When actions are present, ActionChain slot must be materialized.
        var tokens = Lexer.Lex("from Draft on Submit -> set amount = 1 -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("TransitionRow with action must parse without errors");

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull();

        row!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.ActionChain,
            "optional ActionChain slot must be materialized when actions are present");
    }

    [Fact]
    public void TransitionRow_SlotOrdering_StateTarget_Before_EventTarget_Before_Outcome()
    {
        // RED-P: Required slots must appear in catalog-defined order.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull();

        var stateIdx  = row!.Slots.IndexOf(row.Slots.First(s => s.Kind == ConstructSlotKind.StateTarget));
        var eventIdx  = row.Slots.IndexOf(row.Slots.First(s => s.Kind == ConstructSlotKind.EventTarget));
        var outcomeIdx = row.Slots.IndexOf(row.Slots.First(s => s.Kind == ConstructSlotKind.Outcome));

        stateIdx.Should().BeLessThan(eventIdx,
            "StateTarget (catalog Slots[0]) must appear before EventTarget (catalog Slots[1])");
        eventIdx.Should().BeLessThan(outcomeIdx,
            "EventTarget (catalog Slots[1]) must appear before Outcome (catalog Slots[4])");
    }

    [Fact]
    public void TransitionRow_Span_IsNotMissing()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Should().NotBeNull();
        row!.Span.Should().NotBe(SourceSpan.Missing,
            "TransitionRow span must cover real source positions");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §4. StateEnsure — RED-P (complementary)
    //  The EnsureBecauseClauseSlotTests already covers:
    //    - 'in' leading token + with/without because + bad because
    //  This section covers the 'to' and 'from' leading tokens and slot ordering.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StateEnsure_WithToLeadingToken_ProducesCorrectKind()
    {
        // RED-P: 'to State ensure' must produce StateEnsure, not StateAction.
        var tokens = Lexer.Lex("to Approved ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "'to State ensure expr' is valid StateEnsure DSL");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateEnsure,
            "'to State ensure' must produce a StateEnsure construct (not StateAction)");
    }

    [Fact]
    public void StateEnsure_WithFromLeadingToken_ProducesCorrectKind()
    {
        // RED-P: 'from State ensure' must produce StateEnsure, not TransitionRow.
        var tokens = Lexer.Lex("from Draft ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "'from State ensure expr' is valid StateEnsure DSL");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateEnsure,
            "'from State ensure' must produce StateEnsure (not TransitionRow)");
    }

    [Fact]
    public void StateEnsure_WithToLeadingToken_WithBecause_BecauseClauseSlot_IsPresent()
    {
        // RED-P: Optional BecauseClause is materialized for 'to' variant with because.
        var tokens = Lexer.Lex("to Approved ensure amount > 0 because \"Approved amount must be positive\"");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty();

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateEnsure);
        ensure.Should().NotBeNull();

        ensure!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.BecauseClause,
            "BecauseClause slot must be present when 'because' appears in 'to' variant");
    }

    [Fact]
    public void StateEnsure_WithFromLeadingToken_WithoutBecause_BecauseClauseSlot_IsAbsent()
    {
        // RED-P: Optional BecauseClause must be absent for 'from' variant without because.
        var tokens = Lexer.Lex("from Draft ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateEnsure);
        ensure.Should().NotBeNull();

        ensure!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.BecauseClause,
            "optional BecauseClause slot must be absent when no 'because' clause is present");
    }

    [Fact]
    public void StateEnsure_HappyPath_StateTargetSlot_ContainsStateName()
    {
        // RED-P: StateTarget slot carries the scoping state name.
        var tokens = Lexer.Lex("to Approved ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateEnsure);
        ensure.Should().NotBeNull();

        var stateSlot = ensure!.Slots.OfType<StateTargetSlot>().FirstOrDefault();
        stateSlot.Should().NotBeNull("StateEnsure must contain a StateTargetSlot");
        stateSlot!.StateName.Should().Be("Approved",
            "StateTargetSlot must carry the state name 'Approved'");
    }

    [Fact]
    public void StateEnsure_HappyPath_EnsureClauseSlot_IsPresent()
    {
        // RED-P: EnsureClause is required on StateEnsure.
        var tokens = Lexer.Lex("from Draft ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateEnsure);
        ensure.Should().NotBeNull();

        ensure!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.EnsureClause,
            "EnsureClause slot is required and must always be present on StateEnsure");
    }

    [Fact]
    public void StateEnsure_SlotOrdering_StateTarget_Before_EnsureClause()
    {
        // RED-P: StateTarget (catalog Slots[0]) must precede EnsureClause (catalog Slots[1]).
        var tokens = Lexer.Lex("to Approved ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateEnsure);
        ensure.Should().NotBeNull();

        var stateIdx  = ensure!.Slots.IndexOf(ensure.Slots.First(s => s.Kind == ConstructSlotKind.StateTarget));
        var ensureIdx = ensure.Slots.IndexOf(ensure.Slots.First(s => s.Kind == ConstructSlotKind.EnsureClause));

        stateIdx.Should().BeLessThan(ensureIdx,
            "StateTarget (catalog Slots[0]) must appear before EnsureClause (catalog Slots[1])");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §5. AccessMode — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AccessMode_HappyPath_ProducesCorrectKind()
    {
        // RED-P: 'in State modify Field editable' must produce AccessMode.
        var tokens = Lexer.Lex("in Draft modify Amount editable");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "'in State modify Field editable' is valid AccessMode DSL");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.AccessMode,
            "exactly one AccessMode construct must be produced");
    }

    [Fact]
    public void AccessMode_HappyPath_StateTargetSlot_ContainsStateName()
    {
        // RED-P
        var tokens = Lexer.Lex("in Draft modify Amount editable");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var access = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Should().NotBeNull();

        var stateSlot = access!.Slots.OfType<StateTargetSlot>().FirstOrDefault();
        stateSlot.Should().NotBeNull("AccessMode must contain a StateTargetSlot");
        stateSlot!.StateName.Should().Be("Draft",
            "StateTargetSlot must carry the scoping state name 'Draft'");
    }

    [Fact]
    public void AccessMode_HappyPath_FieldTargetSlot_ContainsFieldName()
    {
        // RED-P
        var tokens = Lexer.Lex("in Draft modify Amount editable");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var access = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Should().NotBeNull();

        var fieldSlot = access!.Slots.OfType<FieldTargetSlot>().FirstOrDefault();
        fieldSlot.Should().NotBeNull("AccessMode must contain a FieldTargetSlot");
        fieldSlot!.FieldName.Should().Be("Amount",
            "FieldTargetSlot must carry the field name 'Amount'");
    }

    [Fact]
    public void AccessMode_HappyPath_AccessModeKeywordSlot_IsPresent()
    {
        // RED-P: AccessModeKeyword is a required slot on AccessMode.
        var tokens = Lexer.Lex("in Draft modify Amount editable");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var access = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Should().NotBeNull();

        access!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.AccessModeKeyword,
            "AccessModeKeyword slot is required on AccessMode");
    }

    [Fact]
    public void AccessMode_HappyPath_AccessModeKeywordSlot_CarriesEditableToken()
    {
        // RED-P: AccessModeSlot.AccessMode must carry the resolved token kind.
        var tokens = Lexer.Lex("in Draft modify Amount editable");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var access = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Should().NotBeNull();

        var modeSlot = access!.Slots.OfType<AccessModeSlot>().FirstOrDefault();
        modeSlot.Should().NotBeNull("AccessModeSlot must be present");
        modeSlot!.AccessMode.Should().Be(TokenKind.Editable,
            "AccessModeSlot must carry TokenKind.Editable when 'editable' appears in source");
    }

    [Fact]
    public void AccessMode_HappyPath_AccessModeKeywordSlot_CarriesReadonlyToken()
    {
        // RED-P: AccessModeSlot carries TokenKind.Readonly when 'readonly' appears.
        var tokens = Lexer.Lex("in Draft modify Amount readonly");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("'readonly' is a valid access mode keyword");

        var access = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Should().NotBeNull();

        var modeSlot = access!.Slots.OfType<AccessModeSlot>().FirstOrDefault();
        modeSlot.Should().NotBeNull();
        modeSlot!.AccessMode.Should().Be(TokenKind.Readonly,
            "AccessModeSlot must carry TokenKind.Readonly when 'readonly' appears in source");
    }

    [Fact]
    public void AccessMode_ModifyAll_UsesFieldWildcard_AndRequestedMode()
    {
        var tokens = Lexer.Lex("in Draft modify all readonly");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty();

        var access = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Slots.OfType<FieldTargetSlot>().Single().FieldName.Should().Be("all");
        access.Slots.OfType<AccessModeSlot>().Single().AccessMode.Should().Be(TokenKind.Readonly);
    }

    [Fact]
    public void AccessMode_WithGuard_GuardClauseSlot_IsPresent()
    {
        // RED-P: Optional GuardClause is materialized when a 'when' clause is present.
        var tokens = Lexer.Lex("in Draft modify Amount editable when DocumentsVerified");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("AccessMode with guard must parse without errors");

        var access = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Should().NotBeNull();

        access!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.GuardClause,
            "optional GuardClause slot must be materialized when 'when' clause is present");
    }

    [Fact]
    public void AccessMode_WithoutGuard_GuardClauseSlot_IsAbsent()
    {
        // RED-P: Absent optional GuardClause must not be materialized.
        var tokens = Lexer.Lex("in Draft modify Amount editable");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var access = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Should().NotBeNull();

        access!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.GuardClause,
            "optional GuardClause slot must be absent when no 'when' clause is present");
    }

    [Fact]
    public void AccessMode_SlotOrdering_StateTarget_FieldTarget_AccessModeKeyword()
    {
        // RED-P: Required slots appear in catalog-defined order.
        var tokens = Lexer.Lex("in Draft modify Amount editable");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var access = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Should().NotBeNull();

        var stateIdx  = access!.Slots.IndexOf(access.Slots.First(s => s.Kind == ConstructSlotKind.StateTarget));
        var fieldIdx  = access.Slots.IndexOf(access.Slots.First(s => s.Kind == ConstructSlotKind.FieldTarget));
        var modeIdx   = access.Slots.IndexOf(access.Slots.First(s => s.Kind == ConstructSlotKind.AccessModeKeyword));

        stateIdx.Should().BeLessThan(fieldIdx,
            "StateTarget (catalog Slots[0]) must precede FieldTarget (catalog Slots[1])");
        fieldIdx.Should().BeLessThan(modeIdx,
            "FieldTarget (catalog Slots[1]) must precede AccessModeKeyword (catalog Slots[2])");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §6. OmitDeclaration — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OmitDeclaration_HappyPath_ProducesCorrectKind()
    {
        // RED-P: 'in State omit Field' must produce OmitDeclaration.
        var tokens = Lexer.Lex("in Draft omit InternalNotes");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "'in State omit Field' is valid OmitDeclaration DSL");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.OmitDeclaration,
            "exactly one OmitDeclaration construct must be produced");
    }

    [Fact]
    public void OmitDeclaration_HappyPath_StateTargetSlot_ContainsStateName()
    {
        // RED-P
        var tokens = Lexer.Lex("in Draft omit InternalNotes");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var omit = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.OmitDeclaration);
        omit.Should().NotBeNull();

        var stateSlot = omit!.Slots.OfType<StateTargetSlot>().FirstOrDefault();
        stateSlot.Should().NotBeNull("OmitDeclaration must contain a StateTargetSlot");
        stateSlot!.StateName.Should().Be("Draft",
            "StateTargetSlot must carry the scoping state name 'Draft'");
    }

    [Fact]
    public void OmitDeclaration_HappyPath_FieldTargetSlot_ContainsFieldName()
    {
        // RED-P
        var tokens = Lexer.Lex("in Draft omit InternalNotes");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var omit = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.OmitDeclaration);
        omit.Should().NotBeNull();

        var fieldSlot = omit!.Slots.OfType<FieldTargetSlot>().FirstOrDefault();
        fieldSlot.Should().NotBeNull("OmitDeclaration must contain a FieldTargetSlot");
        fieldSlot!.FieldName.Should().Be("InternalNotes",
            "FieldTargetSlot must carry the excluded field name 'InternalNotes'");
    }

    [Fact]
    public void OmitDeclaration_OmitAll_UsesFieldWildcard()
    {
        var tokens = Lexer.Lex("in Draft omit all");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty();

        var omit = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.OmitDeclaration);
        omit.Slots.OfType<StateTargetSlot>().Single().StateName.Should().Be("Draft");
        omit.Slots.OfType<FieldTargetSlot>().Single().FieldName.Should().Be("all");
    }

    [Fact]
    public void OmitDeclaration_SlotCount_IsExactlyTwo()
    {
        // RED-P: OmitDeclaration has no optional slots; slot count is always 2.
        var tokens = Lexer.Lex("in Draft omit InternalNotes");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var omit = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.OmitDeclaration);
        omit.Should().NotBeNull();

        omit!.Slots.Should().HaveCount(2,
            "OmitDeclaration has exactly 2 required slots and no optional slots");
    }

    [Fact]
    public void OmitDeclaration_SlotOrdering_StateTarget_Before_FieldTarget()
    {
        // RED-P
        var tokens = Lexer.Lex("in Draft omit InternalNotes");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var omit = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.OmitDeclaration);
        omit.Should().NotBeNull();

        omit!.Slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget,
            "Slots[0] must be StateTarget per catalog ordering");
        omit.Slots[1].Kind.Should().Be(ConstructSlotKind.FieldTarget,
            "Slots[1] must be FieldTarget per catalog ordering");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §7. StateAction — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StateAction_WithToLeadingToken_ProducesCorrectKind()
    {
        // RED-P: 'to State -> actions' must produce StateAction.
        var tokens = Lexer.Lex("to Submitted -> set submittedAt = 1");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "'to State -> actions' is valid StateAction DSL (entry hook)");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateAction,
            "'to State -> ...' must produce StateAction, not StateEnsure");
    }

    [Fact]
    public void StateAction_WithFromLeadingToken_ProducesCorrectKind()
    {
        // RED-P: 'from State -> actions' must produce StateAction.
        var tokens = Lexer.Lex("from Draft -> set amount = 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "'from State -> actions' is valid StateAction DSL (exit hook)");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateAction,
            "'from State -> ...' must produce StateAction, not TransitionRow");
    }

    [Fact]
    public void StateAction_HappyPath_StateTargetSlot_ContainsStateName()
    {
        // RED-P
        var tokens = Lexer.Lex("to Submitted -> set submittedAt = 1");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var action = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateAction);
        action.Should().NotBeNull();

        var stateSlot = action!.Slots.OfType<StateTargetSlot>().FirstOrDefault();
        stateSlot.Should().NotBeNull("StateAction must contain a StateTargetSlot");
        stateSlot!.StateName.Should().Be("Submitted",
            "StateTargetSlot must carry the target state name 'Submitted'");
    }

    [Fact]
    public void StateAction_HappyPath_ActionChainSlot_IsPresent()
    {
        // RED-P: ActionChain slot must be present when actions follow '->'.
        var tokens = Lexer.Lex("to Submitted -> set submittedAt = 1");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var action = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateAction);
        action.Should().NotBeNull();

        action!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.ActionChain,
            "ActionChain slot must be present on StateAction when actions are supplied");
    }

    [Fact]
    public void StateAction_SlotOrdering_StateTarget_Before_ActionChain()
    {
        // RED-P
        var tokens = Lexer.Lex("to Submitted -> set submittedAt = 1");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var action = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateAction);
        action.Should().NotBeNull();

        var stateIdx  = action!.Slots.IndexOf(action.Slots.First(s => s.Kind == ConstructSlotKind.StateTarget));
        var actionIdx = action.Slots.IndexOf(action.Slots.First(s => s.Kind == ConstructSlotKind.ActionChain));

        stateIdx.Should().BeLessThan(actionIdx,
            "StateTarget (catalog Slots[0]) must appear before ActionChain (catalog Slots[1])");
    }

    [Fact]
    public void StateAction_DoesNotContain_OutcomeSlot()
    {
        // RED-P: StateAction fires entry/exit hooks; it does not produce a state transition outcome.
        var tokens = Lexer.Lex("to Submitted -> set submittedAt = 1");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var action = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateAction);
        action.Should().NotBeNull();

        action!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.Outcome,
            "StateAction must not produce an Outcome slot — it is not a transition");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §8. EventEnsure — RED-P (complementary)
    //  EnsureBecauseClauseSlotTests already covers with/without because.
    //  This section covers slot ordering, happy-path kind, and disambiguation.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventEnsure_HappyPath_ProducesCorrectKind()
    {
        // RED-P: 'on Event ensure expr' must produce EventEnsure (not EventHandler).
        var tokens = Lexer.Lex("on Submit ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "'on Event ensure expr' is valid EventEnsure DSL");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventEnsure,
            "'on Submit ensure' must produce EventEnsure, not EventHandler");
    }

    [Fact]
    public void EventEnsure_HappyPath_EventTargetSlot_ContainsEventName()
    {
        // RED-P
        var tokens = Lexer.Lex("on Submit ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventEnsure);
        ensure.Should().NotBeNull();

        var eventSlot = ensure!.Slots.OfType<EventTargetSlot>().FirstOrDefault();
        eventSlot.Should().NotBeNull("EventEnsure must contain an EventTargetSlot");
        eventSlot!.EventName.Should().Be("Submit",
            "EventTargetSlot must carry the event name 'Submit'");
    }

    [Fact]
    public void EventEnsure_HappyPath_EnsureClauseSlot_IsPresent()
    {
        // RED-P: EnsureClause is required on EventEnsure.
        var tokens = Lexer.Lex("on Submit ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventEnsure);
        ensure.Should().NotBeNull();

        ensure!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.EnsureClause,
            "EnsureClause slot is required and must always be present on EventEnsure");
    }

    [Fact]
    public void EventEnsure_SlotOrdering_EventTarget_Before_EnsureClause()
    {
        // RED-P: Required slots must appear in catalog-defined order.
        var tokens = Lexer.Lex("on Submit ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventEnsure);
        ensure.Should().NotBeNull();

        var eventIdx  = ensure!.Slots.IndexOf(ensure.Slots.First(s => s.Kind == ConstructSlotKind.EventTarget));
        var ensureIdx = ensure.Slots.IndexOf(ensure.Slots.First(s => s.Kind == ConstructSlotKind.EnsureClause));

        eventIdx.Should().BeLessThan(ensureIdx,
            "EventTarget (catalog Slots[0]) must appear before EnsureClause (catalog Slots[1])");
    }

    [Fact]
    public void EventEnsure_DoesNotContain_OutcomeSlot()
    {
        // RED-P: EventEnsure governs event preconditions; it has no transition outcome.
        var tokens = Lexer.Lex("on Submit ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var ensure = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventEnsure);
        ensure.Should().NotBeNull();

        ensure!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.Outcome,
            "EventEnsure must not produce an Outcome slot");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §9. EventHandler — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventHandler_HappyPath_ProducesCorrectKind()
    {
        // RED-P: 'on Event -> actions' must produce EventHandler (not EventEnsure).
        var tokens = Lexer.Lex("on UpdateName -> set name = newName");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty(
            "'on Event -> actions' is valid EventHandler DSL");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventHandler,
            "'on UpdateName ->' must produce EventHandler, not EventEnsure");
    }

    [Fact]
    public void EventHandler_HappyPath_EventTargetSlot_ContainsEventName()
    {
        // RED-P
        var tokens = Lexer.Lex("on UpdateName -> set name = newName");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var handler = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventHandler);
        handler.Should().NotBeNull();

        var eventSlot = handler!.Slots.OfType<EventTargetSlot>().FirstOrDefault();
        eventSlot.Should().NotBeNull("EventHandler must contain an EventTargetSlot");
        eventSlot!.EventName.Should().Be("UpdateName",
            "EventTargetSlot must carry the event name 'UpdateName'");
    }

    [Fact]
    public void EventHandler_WithAction_ActionChainSlot_IsPresent()
    {
        // RED-P: ActionChain slot is materialized when actions follow '->'.
        var tokens = Lexer.Lex("on UpdateName -> set name = newName");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var handler = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventHandler);
        handler.Should().NotBeNull();

        handler!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.ActionChain,
            "optional ActionChain slot must be materialized when actions are present on EventHandler");
    }

    [Fact]
    public void EventHandler_DoesNotContain_OutcomeSlot()
    {
        // RED-P: EventHandler causes no state transition — Outcome must never appear.
        var tokens = Lexer.Lex("on UpdateName -> set name = newName");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var handler = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventHandler);
        handler.Should().NotBeNull();

        handler!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.Outcome,
            "EventHandler must not contain an Outcome slot — it is stateless and causes no transition");
    }

    [Fact]
    public void EventHandler_DoesNotContain_StateTargetSlot()
    {
        // RED-P: EventHandler is event-scoped, not state-scoped — StateTarget must never appear.
        var tokens = Lexer.Lex("on UpdateName -> set name = newName");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var handler = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventHandler);
        handler.Should().NotBeNull();

        handler!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.StateTarget,
            "EventHandler must not contain a StateTarget slot — it is event-scoped, not state-scoped");
    }

    [Fact]
    public void EventHandler_SlotOrdering_EventTarget_Before_ActionChain()
    {
        // RED-P: EventTarget (catalog Slots[0]) must precede ActionChain (catalog Slots[1]).
        var tokens = Lexer.Lex("on UpdateName -> set name = newName");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var handler = manifest.Constructs.SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventHandler);
        handler.Should().NotBeNull();

        var eventIdx  = handler!.Slots.IndexOf(handler.Slots.First(s => s.Kind == ConstructSlotKind.EventTarget));
        var actionIdx = handler.Slots.IndexOf(handler.Slots.First(s => s.Kind == ConstructSlotKind.ActionChain));

        eventIdx.Should().BeLessThan(actionIdx,
            "EventTarget (catalog Slots[0]) must precede ActionChain (catalog Slots[1])");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §10. Disambiguation tests — RED-P
    //
    //  These tests verify that the catalog-driven disambiguation protocol at
    //  peek(2) correctly selects the intended construct when multiple constructs
    //  share the same leading token.
    //
    //  Disambiguation groups:
    //    'from' → TransitionRow (on) | StateEnsure (ensure) | StateAction (->)
    //    'in'   → StateEnsure (ensure) | AccessMode (modify) | OmitDeclaration (omit)
    //    'to'   → StateEnsure (ensure) | StateAction (->)
    //    'on'   → EventEnsure (ensure) | EventHandler (->)
    // ════════════════════════════════════════════════════════════════════════════

    // ── 'from' group ────────────────────────────────────────────────────────────

    [Fact]
    public void Disambiguation_From_WithOn_ProducesTransitionRow_NotStateEnsure()
    {
        // RED-P: peek(2) == 'on' → TransitionRow.
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.TransitionRow,
            "'from State on Event' must route to TransitionRow via peek(2) == 'on'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.StateEnsure,
            "StateEnsure must NOT be produced when peek(2) is 'on'");
    }

    [Fact]
    public void Disambiguation_From_WithEnsure_ProducesStateEnsure_NotTransitionRow()
    {
        // RED-P: peek(2) == 'ensure' → StateEnsure.
        var tokens = Lexer.Lex("from Draft ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateEnsure,
            "'from State ensure' must route to StateEnsure via peek(2) == 'ensure'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.TransitionRow,
            "TransitionRow must NOT be produced when peek(2) is 'ensure'");
    }

    [Fact]
    public void Disambiguation_From_WithArrow_ProducesStateAction_NotTransitionRow()
    {
        // RED-P: peek(2) == '->' → StateAction (exit hook, not a transition row).
        var tokens = Lexer.Lex("from Draft -> set amount = 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateAction,
            "'from State ->' must route to StateAction via peek(2) == '->'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.TransitionRow,
            "TransitionRow must NOT be produced when peek(2) is '->' (no 'on' present)");
    }

    // ── 'in' group ──────────────────────────────────────────────────────────────

    [Fact]
    public void Disambiguation_In_WithEnsure_ProducesStateEnsure_NotAccessMode()
    {
        // RED-P: peek(2) == 'ensure' → StateEnsure.
        var tokens = Lexer.Lex("in Approved ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateEnsure,
            "'in State ensure' must route to StateEnsure via peek(2) == 'ensure'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.AccessMode,
            "AccessMode must NOT be produced when peek(2) is 'ensure'");
    }

    [Fact]
    public void Disambiguation_In_WithModify_ProducesAccessMode_NotStateEnsure()
    {
        // RED-P: peek(2) == 'modify' → AccessMode.
        var tokens = Lexer.Lex("in Draft modify Amount editable");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.AccessMode,
            "'in State modify' must route to AccessMode via peek(2) == 'modify'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.StateEnsure,
            "StateEnsure must NOT be produced when peek(2) is 'modify'");
    }

    [Fact]
    public void Disambiguation_In_WithOmit_ProducesOmitDeclaration_NotAccessMode()
    {
        // RED-P: peek(2) == 'omit' → OmitDeclaration.
        var tokens = Lexer.Lex("in Draft omit InternalNotes");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.OmitDeclaration,
            "'in State omit' must route to OmitDeclaration via peek(2) == 'omit'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.AccessMode,
            "AccessMode must NOT be produced when peek(2) is 'omit'");
    }

    // ── 'to' group ──────────────────────────────────────────────────────────────

    [Fact]
    public void Disambiguation_To_WithEnsure_ProducesStateEnsure_NotStateAction()
    {
        // RED-P: peek(2) == 'ensure' → StateEnsure.
        var tokens = Lexer.Lex("to Approved ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateEnsure,
            "'to State ensure' must route to StateEnsure via peek(2) == 'ensure'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.StateAction,
            "StateAction must NOT be produced when peek(2) is 'ensure'");
    }

    [Fact]
    public void Disambiguation_To_WithArrow_ProducesStateAction_NotStateEnsure()
    {
        // RED-P: peek(2) == '->' → StateAction (entry hook).
        var tokens = Lexer.Lex("to Submitted -> set submittedAt = 1");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateAction,
            "'to State ->' must route to StateAction via peek(2) == '->'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.StateEnsure,
            "StateEnsure must NOT be produced when peek(2) is '->'");
    }

    // ── 'on' group ──────────────────────────────────────────────────────────────

    [Fact]
    public void Disambiguation_On_WithArrow_ProducesEventHandler_NotEventEnsure()
    {
        // RED-P: peek(2) == '->' → EventHandler.
        var tokens = Lexer.Lex("on UpdateName -> set name = newName");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventHandler,
            "'on Event ->' must route to EventHandler via peek(2) == '->'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.EventEnsure,
            "EventEnsure must NOT be produced when peek(2) is '->'");
    }

    [Fact]
    public void Disambiguation_On_WithEnsure_ProducesEventEnsure_NotEventHandler()
    {
        // RED-P: peek(2) == 'ensure' → EventEnsure.
        var tokens = Lexer.Lex("on Submit ensure amount > 0");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventEnsure,
            "'on Event ensure' must route to EventEnsure via peek(2) == 'ensure'");
        manifest.Constructs.Should().NotContain(
            c => c.Meta.Kind == ConstructKind.EventHandler,
            "EventHandler must NOT be produced when peek(2) is 'ensure'");
    }
}
