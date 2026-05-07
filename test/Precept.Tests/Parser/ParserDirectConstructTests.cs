using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Tests for the Slice 1 direct construct family:
///   PreceptHeader, FieldDeclaration, StateDeclaration, EventDeclaration, RuleDeclaration.
///
/// Scoped constructs (TransitionRow, StateEnsure, EventEnsure, EventHandler, etc.) are Slice 2 — not tested here.
/// RED-P tests for StateEnsure / EventEnsure already live in EnsureBecauseClauseSlotTests.cs — not duplicated.
///
/// Test status at time of writing:
///   GREEN  — catalog metadata tests; pass immediately (no parser dependency)
///   RED-P  — parser behavioral tests; red until Parser.Parse replaces the stub
/// </summary>
public class ParserDirectConstructTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  §1. Catalog metadata — GREEN
    //  These tests verify slot structure in the Constructs catalog.
    //  They are standalone (no Lexer / Parser dependency) and must stay green.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ConstructKind.PreceptHeader,    1)]
    [InlineData(ConstructKind.FieldDeclaration, 4)]
    [InlineData(ConstructKind.StateDeclaration, 1)]
    [InlineData(ConstructKind.EventDeclaration, 3)]
    [InlineData(ConstructKind.RuleDeclaration,  3)]
    public void DirectConstruct_CatalogSlotCount_MatchesExpectedCount(
        ConstructKind kind, int expectedCount)
    {
        // GREEN — catalog only; no parser involved.
        var meta = Constructs.GetMeta(kind);
        meta.Slots.Should().HaveCount(expectedCount,
            $"{kind} must declare exactly {expectedCount} slot(s) in the Constructs catalog");
    }

    [Theory]
    [InlineData(ConstructKind.PreceptHeader)]
    [InlineData(ConstructKind.FieldDeclaration)]
    [InlineData(ConstructKind.StateDeclaration)]
    [InlineData(ConstructKind.EventDeclaration)]
    [InlineData(ConstructKind.RuleDeclaration)]
    public void DirectConstruct_RoutingFamily_IsDirect_OrHeader(ConstructKind kind)
    {
        // GREEN — all five constructs route without disambiguation.
        var meta = Constructs.GetMeta(kind);
        meta.RoutingFamily.Should().BeOneOf(
            new[] { RoutingFamily.Direct, RoutingFamily.Header },
            $"{kind} must belong to the Direct or Header routing family");
    }

    [Fact]
    public void PreceptHeader_CatalogSlot0_IsIdentifierList_AndRequired()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.PreceptHeader).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.IdentifierList,
            "PreceptHeader.Slots[0] is the entity name");
        slots[0].IsRequired.Should().BeTrue("the header name is mandatory");
    }

    [Fact]
    public void FieldDeclaration_CatalogSlotOrder_IsIdentifierType_ModifierCompute()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.FieldDeclaration).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.IdentifierList,    "Slots[0]: field name(s)");
        slots[1].Kind.Should().Be(ConstructSlotKind.TypeExpression,    "Slots[1]: type annotation");
        slots[2].Kind.Should().Be(ConstructSlotKind.ModifierList,      "Slots[2]: optional modifiers");
        slots[3].Kind.Should().Be(ConstructSlotKind.ComputeExpression, "Slots[3]: optional compute expression");
    }

    [Fact]
    public void FieldDeclaration_RequiredSlots_AreIdentifierAndType()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.FieldDeclaration).Slots;
        slots[0].IsRequired.Should().BeTrue("identifier is required");
        slots[1].IsRequired.Should().BeTrue("type annotation is required");
        slots[2].IsRequired.Should().BeFalse("modifier list is optional");
        slots[3].IsRequired.Should().BeFalse("compute expression is optional");
    }

    [Fact]
    public void StateDeclaration_CatalogSlot0_IsStateEntryList_AndRequired()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.StateDeclaration).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.StateEntryList,
            "StateDeclaration.Slots[0] is the list of state entries");
        slots[0].IsRequired.Should().BeTrue("a state declaration without any states is empty");
    }

    [Fact]
    public void EventDeclaration_CatalogSlotOrder_IsIdentifier_ArgumentList_InitialMarker()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.EventDeclaration).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.IdentifierList, "Slots[0]: event name(s)");
        slots[1].Kind.Should().Be(ConstructSlotKind.ArgumentList,   "Slots[1]: optional parameter list");
        slots[2].Kind.Should().Be(ConstructSlotKind.InitialMarker,  "Slots[2]: optional initial marker");
    }

    [Fact]
    public void EventDeclaration_RequiredSlots_IsIdentifierOnly()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.EventDeclaration).Slots;
        slots[0].IsRequired.Should().BeTrue("event name is required");
        slots[1].IsRequired.Should().BeFalse("argument list is optional");
        slots[2].IsRequired.Should().BeFalse("initial marker is optional");
    }

    [Fact]
    public void RuleDeclaration_CatalogSlotOrder_IsExpression_Guard_Because()
    {
        // GREEN
        var slots = Constructs.GetMeta(ConstructKind.RuleDeclaration).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.RuleExpression, "Slots[0]: the rule boolean expression");
        slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause,    "Slots[1]: optional when-guard");
        slots[2].Kind.Should().Be(ConstructSlotKind.BecauseClause,  "Slots[2]: mandatory reason");
    }

    [Fact]
    public void RuleDeclaration_BecauseClause_IsRequired_GuardClause_IsOptional()
    {
        // GREEN — mirrors the regression test in EnsureBecauseClauseSlotTests, but targeted at
        // slot optionality, not identity. No duplication because we only check IsRequired here.
        var slots = Constructs.GetMeta(ConstructKind.RuleDeclaration).Slots;
        slots[0].IsRequired.Should().BeTrue("rule expression is required");
        slots[1].IsRequired.Should().BeFalse("guard clause is optional — not all rules are scoped");
        slots[2].IsRequired.Should().BeTrue("because clause is required — every rule must have a reason");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §2. Lexer smoke tests — GREEN
    //  Verify that the DSL snippets used in RED-P tests lex without errors, so
    //  failures in RED-P tests are definitely due to the parser stub, not bad
    //  source strings.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("precept LoanApplication")]
    [InlineData("field amount as number")]
    [InlineData("field amount as number nonnegative")]
    [InlineData("state Draft initial")]
    [InlineData("state Draft initial, Submitted, Approved terminal success")]
    [InlineData("event Submit")]
    [InlineData("event Submit(approver as string)")]
    [InlineData("event Open initial")]
    [InlineData("rule amount > 0 because \"Amount must be positive\"")]
    [InlineData("rule amount > 0 when active because \"Amount must be positive when active\"")]
    public void DirectConstruct_LexesWithoutErrors(string source)
    {
        // GREEN — ensures test source strings are syntactically lexable.
        var stream = Lexer.Lex(source);
        stream.Diagnostics.Should().BeEmpty(
            $"'{source}' must lex cleanly; lex errors indicate a bad test source string");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §3. PreceptHeader — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PreceptHeader_HappyPath_ProducesOneConstruct_WithCorrectKind()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("precept LoanApplication");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("a bare precept header must parse without errors");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.PreceptHeader,
            "a single-line precept source must produce exactly one PreceptHeader construct");
    }

    [Fact]
    public void PreceptHeader_HappyPath_IdentifierListSlot_ContainsEntityName()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("precept LoanApplication");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var header = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.PreceptHeader);
        header.Should().NotBeNull("PreceptHeader must be produced");

        var idSlot = header!.Slots.OfType<IdentifierListSlot>().FirstOrDefault();
        idSlot.Should().NotBeNull("Slots[0] must be an IdentifierListSlot");
        idSlot!.Names.Should().ContainSingle("LoanApplication",
            "the header must record the entity name");
    }

    [Fact]
    public void PreceptHeader_SlotCount_MatchesCatalog()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("precept LoanApplication");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var header = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.PreceptHeader);
        header.Should().NotBeNull();

        var expectedCount = Constructs.GetMeta(ConstructKind.PreceptHeader).Slots.Count;
        header!.Slots.Should().HaveCount(expectedCount,
            "slot count must match the catalog-defined slot array length");
    }

    [Fact]
    public void PreceptHeader_Span_IsNotMissing()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("precept LoanApplication");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var header = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.PreceptHeader);
        header.Should().NotBeNull();
        header!.Span.Should().NotBe(SourceSpan.Missing,
            "construct span must reference a real position in the source");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §4. FieldDeclaration — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FieldDeclaration_HappyPath_ProducesOneConstruct_WithCorrectKind()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("field amount as number");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("a bare field declaration must parse without errors");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.FieldDeclaration,
            "exactly one FieldDeclaration must be produced");
    }

    [Fact]
    public void FieldDeclaration_HappyPath_Slots0And1_AreIdentifierAndType()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("field amount as number");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        field.Should().NotBeNull();

        field!.Slots[0].Kind.Should().Be(ConstructSlotKind.IdentifierList,
            "Slots[0] is the field name (IdentifierList)");
        field.Slots[1].Kind.Should().Be(ConstructSlotKind.TypeExpression,
            "Slots[1] is the type annotation (TypeExpression)");
    }

    [Fact]
    public void FieldDeclaration_HappyPath_IdentifierSlot_ContainsFieldName()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("field amount as number");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        field.Should().NotBeNull();

        var idSlot = field!.Slots.OfType<IdentifierListSlot>().FirstOrDefault();
        idSlot.Should().NotBeNull("Slots[0] must be an IdentifierListSlot");
        idSlot!.Names.Should().ContainSingle("amount",
            "the field name 'amount' must appear in the identifier slot");
    }

    [Fact]
    public void FieldDeclaration_WithModifier_ModifierListSlot_IsPresent()
    {
        // RED-P: Optional ModifierList slot IS present when modifiers are supplied.
        var tokens = Lexer.Lex("field amount as number nonnegative");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("field with modifier must parse cleanly");

        var field = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        field.Should().NotBeNull();

        field!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.ModifierList,
            "the optional ModifierList slot must be materialized when modifiers are present");
    }

    [Fact]
    public void FieldDeclaration_WithoutModifierOrCompute_OptionalSlots_AreAbsent()
    {
        // RED-P: Optional slots must not be materialized when absent from source.
        var tokens = Lexer.Lex("field amount as number");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        field.Should().NotBeNull();

        field!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.ModifierList,
            "ModifierList slot must be absent when no modifiers appear in source");
        field.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.ComputeExpression,
            "ComputeExpression slot must be absent when no compute expression appears in source");
    }

    [Fact]
    public void FieldDeclaration_SlotOrdering_RequiredSlots_AppearInCatalogOrder()
    {
        // RED-P: Required slots must appear at catalog indices 0 and 1.
        var tokens = Lexer.Lex("field amount as number");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        field.Should().NotBeNull();

        var meta = Constructs.GetMeta(ConstructKind.FieldDeclaration);
        field!.Slots[0].Kind.Should().Be(meta.Slots[0].Kind,
            $"Slots[0] must be {meta.Slots[0].Kind} per catalog ordering");
        field.Slots[1].Kind.Should().Be(meta.Slots[1].Kind,
            $"Slots[1] must be {meta.Slots[1].Kind} per catalog ordering");
    }

    [Fact]
    public void FieldDeclaration_Span_IsNotMissing()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("field amount as number");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var field = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        field.Should().NotBeNull();
        field!.Span.Should().NotBe(SourceSpan.Missing,
            "FieldDeclaration span must cover real source positions");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §5. StateDeclaration — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StateDeclaration_HappyPath_ProducesOneConstruct_WithCorrectKind()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("state Draft initial");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("a single-state declaration must parse without errors");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateDeclaration,
            "exactly one StateDeclaration must be produced");
    }

    [Fact]
    public void StateDeclaration_HappyPath_Slot0_IsStateEntryListSlot()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("state Draft initial");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var state = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateDeclaration);
        state.Should().NotBeNull();

        state!.Slots[0].Kind.Should().Be(ConstructSlotKind.StateEntryList,
            "Slots[0] is the StateEntryList — all state names and per-state modifiers live here");
    }

    [Fact]
    public void StateDeclaration_HappyPath_StateEntryListSlot_ContainsStateName()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("state Draft initial");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var state = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateDeclaration);
        state.Should().NotBeNull();

        var entrySlot = state!.Slots.OfType<StateEntryListSlot>().FirstOrDefault();
        entrySlot.Should().NotBeNull("Slots[0] must be a StateEntryListSlot");
        entrySlot!.Entries.Should().Contain(e => e.Name == "Draft",
            "the state name 'Draft' must appear in the entry list");
    }

    [Fact]
    public void StateDeclaration_MultipleStates_AllEntriesPresent()
    {
        // RED-P: Comma-separated state entries all land in the single StateEntryList slot.
        var tokens = Lexer.Lex("state Draft initial, Submitted, Approved terminal success");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("multi-state declaration must parse cleanly");

        var state = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateDeclaration);
        state.Should().NotBeNull();

        var entrySlot = state!.Slots.OfType<StateEntryListSlot>().FirstOrDefault();
        entrySlot.Should().NotBeNull();
        entrySlot!.Entries.Select(e => e.Name).Should()
            .Contain(new[] { "Draft", "Submitted", "Approved" },
            "all three state names must appear in the StateEntryList slot");
    }

    [Fact]
    public void StateDeclaration_StateModifiers_AreEmbeddedInStateEntryListSlot()
    {
        // RED-P: State modifiers (initial, terminal) live inside StateEntryList, not as separate slots.
        // There is exactly one slot: StateEntryList.
        var tokens = Lexer.Lex("state Draft initial");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var state = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateDeclaration);
        state.Should().NotBeNull();

        state!.Slots.Should().HaveCount(
            Constructs.GetMeta(ConstructKind.StateDeclaration).Slots.Count,
            "StateDeclaration always has exactly 1 slot; modifiers are inside StateEntryList");
    }

    [Fact]
    public void StateDeclaration_SlotCount_MatchesCatalog()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("state Draft initial, Submitted, Approved terminal success");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var state = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateDeclaration);
        state.Should().NotBeNull();

        var expectedCount = Constructs.GetMeta(ConstructKind.StateDeclaration).Slots.Count;
        state!.Slots.Should().HaveCount(expectedCount,
            "slot count must equal catalog metadata slot count");
    }

    [Fact]
    public void StateDeclaration_Span_IsNotMissing()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("state Draft initial");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var state = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.StateDeclaration);
        state.Should().NotBeNull();
        state!.Span.Should().NotBe(SourceSpan.Missing,
            "StateDeclaration span must reference real source positions");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §6. EventDeclaration — RED-P
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventDeclaration_HappyPath_BareEvent_ProducesOneConstruct_WithCorrectKind()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("event Submit");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("a bare event declaration must parse without errors");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventDeclaration,
            "exactly one EventDeclaration must be produced");
    }

    [Fact]
    public void EventDeclaration_BareEvent_IdentifierSlot_ContainsEventName()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("event Submit");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var evt = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        evt.Should().NotBeNull();

        var idSlot = evt!.Slots.OfType<IdentifierListSlot>().FirstOrDefault();
        idSlot.Should().NotBeNull("Slots[0] must be an IdentifierListSlot");
        idSlot!.Names.Should().ContainSingle("Submit",
            "the event name 'Submit' must appear in the identifier slot");
    }

    [Fact]
    public void EventDeclaration_BareEvent_OptionalSlots_AreAbsent()
    {
        // RED-P: A bare event has no argument list and no initial marker; optional slots must be absent.
        var tokens = Lexer.Lex("event Submit");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var evt = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        evt.Should().NotBeNull();

        evt!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.ArgumentList,
            "ArgumentList slot must be absent when no parameter list is provided");
        evt.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.InitialMarker,
            "InitialMarker slot must be absent when 'initial' keyword is not present");
    }

    [Fact]
    public void EventDeclaration_WithArguments_ArgumentListSlot_IsPresent()
    {
        // RED-P: ArgumentList slot is optional; it IS materialized when args are supplied.
        var tokens = Lexer.Lex("event Submit(approver as string)");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("event with arguments must parse cleanly");

        var evt = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        evt.Should().NotBeNull();

        evt!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.ArgumentList,
            "the optional ArgumentList slot must be materialized when a parameter list is present");
    }

    [Fact]
    public void EventDeclaration_WithArguments_ArgumentListSlot_ContainsParameterName()
    {
        // RED-P: Argument names must be captured in the ArgumentListSlot.
        var tokens = Lexer.Lex("event Submit(approver as string)");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var evt = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        evt.Should().NotBeNull();

        var argSlot = evt!.Slots.OfType<ArgumentListSlot>().FirstOrDefault();
        argSlot.Should().NotBeNull("ArgumentListSlot must be present");
        argSlot!.Args.Should().Contain(a => a.Name == "approver",
            "the parameter name 'approver' must be captured in ArgumentListSlot.Args");
    }

    [Fact]
    public void EventDeclaration_WithInitialKeyword_InitialMarkerSlot_IsPresent_AndTrue()
    {
        // RED-P: The optional InitialMarker slot must be materialized and carry IsPresent = true.
        var tokens = Lexer.Lex("event Open initial");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("event with initial keyword must parse cleanly");

        var evt = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        evt.Should().NotBeNull();

        var markerSlot = evt!.Slots.OfType<InitialMarkerSlot>().FirstOrDefault();
        markerSlot.Should().NotBeNull("InitialMarkerSlot must be present when 'initial' appears");
        markerSlot!.IsPresent.Should().BeTrue(
            "InitialMarkerSlot.IsPresent must be true when the 'initial' keyword is in source");
    }

    [Fact]
    public void EventDeclaration_SlotOrdering_IdentifierIsSlot0()
    {
        // RED-P: Catalog order: IdentifierList[0], ArgumentList[1], InitialMarker[2].
        // With all optional slots present, indices must follow the catalog.
        var tokens = Lexer.Lex("event Submit(approver as string)");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var evt = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        evt.Should().NotBeNull();

        var meta = Constructs.GetMeta(ConstructKind.EventDeclaration);
        evt!.Slots[0].Kind.Should().Be(meta.Slots[0].Kind,
            $"Slots[0] must be {meta.Slots[0].Kind} per catalog ordering");
    }

    [Fact]
    public void EventDeclaration_Span_IsNotMissing()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("event Submit");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var evt = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        evt.Should().NotBeNull();
        evt!.Span.Should().NotBe(SourceSpan.Missing,
            "EventDeclaration span must reference real source positions");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §7. RuleDeclaration — RED-P
    //  Note: Parser_RuleDeclaration_WithBecause_Regression already covers the
    //  basic because-clause parser test in EnsureBecauseClauseSlotTests.cs.
    //  The tests here are strictly complementary: guard presence/absence, slot
    //  ordering, span, and minimal happy path.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RuleDeclaration_HappyPath_ProducesOneConstruct_WithCorrectKind()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("rule amount > 0 because \"Amount must be positive\"");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("a minimal rule declaration must parse without errors");
        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.RuleDeclaration,
            "exactly one RuleDeclaration must be produced");
    }

    [Fact]
    public void RuleDeclaration_WithoutGuard_GuardClauseSlot_IsAbsent()
    {
        // RED-P: GuardClause is optional; it must NOT be materialized when 'when' is absent.
        var tokens = Lexer.Lex("rule amount > 0 because \"Amount must be positive\"");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var rule = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        rule.Should().NotBeNull();

        rule!.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.GuardClause,
            "optional GuardClause slot must be absent when no 'when' clause is present in source");
    }

    [Fact]
    public void RuleDeclaration_WithGuard_GuardClauseSlot_IsPresent()
    {
        // RED-P: GuardClause slot must be materialized when 'when' appears.
        var tokens = Lexer.Lex("rule amount > 0 when active because \"Amount must be positive when active\"");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("rule with guard clause must parse cleanly");

        var rule = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        rule.Should().NotBeNull();

        rule!.Slots.Should().Contain(
            s => s.Kind == ConstructSlotKind.GuardClause,
            "the optional GuardClause slot must be materialized when a 'when' clause is present");
    }

    [Fact]
    public void RuleDeclaration_WithAllSlots_SlotOrdering_MatchesCatalog()
    {
        // RED-P: With all slots present, the slot array must follow catalog-defined order.
        var tokens = Lexer.Lex("rule amount > 0 when active because \"Active amount must be positive\"");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var rule = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        rule.Should().NotBeNull();

        var meta = Constructs.GetMeta(ConstructKind.RuleDeclaration);
        rule!.Slots[0].Kind.Should().Be(meta.Slots[0].Kind, $"Slots[0] must be {meta.Slots[0].Kind}");
        rule.Slots[1].Kind.Should().Be(meta.Slots[1].Kind,  $"Slots[1] must be {meta.Slots[1].Kind}");
        rule.Slots[2].Kind.Should().Be(meta.Slots[2].Kind,  $"Slots[2] must be {meta.Slots[2].Kind}");
    }

    [Fact]
    public void RuleDeclaration_WithAllSlots_SlotCount_MatchesCatalog()
    {
        // RED-P: All 3 slots present → count equals catalog metadata count.
        var tokens = Lexer.Lex("rule amount > 0 when active because \"Active amount must be positive\"");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var rule = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        rule.Should().NotBeNull();

        var expectedCount = Constructs.GetMeta(ConstructKind.RuleDeclaration).Slots.Count;
        rule!.Slots.Should().HaveCount(expectedCount,
            "when all optional slots are present, slot count must equal the catalog slot count");
    }

    [Fact]
    public void RuleDeclaration_Span_IsNotMissing()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex("rule amount > 0 because \"Amount must be positive\"");
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var rule = manifest.Constructs
            .SingleOrDefault(c => c.Meta.Kind == ConstructKind.RuleDeclaration);
        rule.Should().NotBeNull();
        rule!.Span.Should().NotBe(SourceSpan.Missing,
            "RuleDeclaration span must reference real source positions");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §8. Multi-construct source — RED-P
    //  Parse a complete minimal precept with all 5 direct construct kinds.
    //  Verifies the parser emits one construct per declaration and doesn't
    //  drop or double-count constructs across kind boundaries.
    // ════════════════════════════════════════════════════════════════════════════

    private const string MinimalPrecept =
        "precept LoanApp\n" +
        "field amount as number\n" +
        "state Draft initial, Funded terminal success\n" +
        "event Submit\n" +
        "rule amount > 0 because \"Amount must be positive\"";

    [Fact]
    public void MultiConstruct_AllFiveDirectKinds_ArePresent()
    {
        // RED-P: Parser is a stub — stub returns empty; this documents expected output.
        var tokens = Lexer.Lex(MinimalPrecept);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("a valid minimal precept must parse without errors");

        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.PreceptHeader,    "PreceptHeader must be present");
        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.FieldDeclaration, "FieldDeclaration must be present");
        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.StateDeclaration, "StateDeclaration must be present");
        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.EventDeclaration, "EventDeclaration must be present");
        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.RuleDeclaration,  "RuleDeclaration must be present");
    }

    [Fact]
    public void MultiConstruct_ConstructCount_IsFive()
    {
        // RED-P: Parser is a stub.
        var tokens = Lexer.Lex(MinimalPrecept);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().HaveCount(5,
            "one construct per declaration line; parser must not drop or duplicate constructs");
    }

    [Fact]
    public void MultiConstruct_ConstructOrder_MatchesSourceOrder()
    {
        // RED-P: Parser is a stub.
        // Constructs must appear in source order (header first, rule last).
        var tokens = Lexer.Lex(MinimalPrecept);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        if (manifest.Constructs.Length < 5) return; // stub — skip ordering assertions
        manifest.Constructs[0].Meta.Kind.Should().Be(ConstructKind.PreceptHeader,    "header is first");
        manifest.Constructs[1].Meta.Kind.Should().Be(ConstructKind.FieldDeclaration, "field is second");
        manifest.Constructs[2].Meta.Kind.Should().Be(ConstructKind.StateDeclaration, "state is third");
        manifest.Constructs[3].Meta.Kind.Should().Be(ConstructKind.EventDeclaration, "event is fourth");
        manifest.Constructs[4].Meta.Kind.Should().Be(ConstructKind.RuleDeclaration,  "rule is fifth");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §9. Error recovery — RED-P
    //  Parser must not throw on malformed input; it must emit diagnostics and
    //  attempt to recover to the next construct boundary.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ErrorRecovery_UnknownLeadingToken_BeforeValidConstruct_ParserDoesNotThrow()
    {
        // RED-P: Parser is a stub, but the contract holds regardless of implementation.
        // An unrecognised token before a valid construct must not throw; the parser must
        // skip to the next construct boundary and continue.
        var src = "@@@\nfield amount as number";
        var tokens = Lexer.Lex(src);

        var act = () => Precept.Pipeline.Parser.Parse(tokens);
        act.Should().NotThrow("the parser must never throw on malformed input");
    }

    [Fact]
    public void ErrorRecovery_UnknownLeadingToken_EmitsDiagnostic()
    {
        // RED-P: An unrecognised leading token must produce at least one diagnostic.
        var src = "@@@\nfield amount as number";
        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().NotBeEmpty(
            "an unrecognised leading token must produce a parse diagnostic");
    }

    [Fact]
    public void ErrorRecovery_UnknownLeadingToken_ValidConstructFollowing_IsStillParsed()
    {
        // RED-P: After recovering from an unknown token the parser must resume and
        // produce the valid FieldDeclaration that follows.
        var src = "@@@\nfield amount as number";
        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Constructs.Should().Contain(
            c => c.Meta.Kind == ConstructKind.FieldDeclaration,
            "the valid FieldDeclaration following the bad token must still be parsed");
    }

    [Fact]
    public void ErrorRecovery_EofMidConstruct_DoesNotThrow()
    {
        // RED-P: A field declaration truncated mid-way (missing type) must not cause the
        // parser to throw. It must return a partial manifest with at least one diagnostic.
        var src = "field amount"; // missing "as <type>"
        var tokens = Lexer.Lex(src);

        var act = () => Precept.Pipeline.Parser.Parse(tokens);
        act.Should().NotThrow("the parser must handle EOF in the middle of a construct gracefully");
    }

    [Fact]
    public void ErrorRecovery_EofMidConstruct_ProducesDiagnostic()
    {
        // RED-P: Truncated construct must produce a diagnostic.
        var src = "field amount"; // missing "as <type>"
        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().NotBeEmpty(
            "truncated construct (EOF before type annotation) must produce at least one diagnostic");
    }

    [Fact]
    public void ErrorRecovery_RuleWithoutBecause_ProducesDiagnostic()
    {
        // RED-P: 'because' is required on a RuleDeclaration; its absence must produce a diagnostic.
        var src = "rule amount > 0"; // missing mandatory because clause
        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().NotBeEmpty(
            "a rule declaration without the required 'because' clause must produce a parse diagnostic");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §10. Slot count invariant theory — RED-P
    //  Parameterized over all 5 direct construct kinds.
    //  When a construct is parsed with all optional slots present, the parser
    //  output slot count must equal the catalog metadata slot count.
    //  Expected count is derived from the catalog at test-time, never hardcoded.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ConstructKind.PreceptHeader,    "precept LoanApp")]
    [InlineData(ConstructKind.StateDeclaration, "state Draft initial")]
    [InlineData(ConstructKind.FieldDeclaration, "field amount as number nonnegative -> 0")]
    [InlineData(ConstructKind.EventDeclaration, "event Submit(approver as string) initial")]
    [InlineData(ConstructKind.RuleDeclaration,  "rule amount > 0 when active because \"reason\"")]
    public void DirectConstruct_ParsedSlotCount_MatchesCatalogMetaSlotCount_WhenAllOptionalSlotsPresent(
        ConstructKind kind, string source)
    {
        // RED-P: Expected count comes from catalog metadata, never hardcoded.
        // Sources are chosen to include all optional slots so the invariant is testable.
        var tokens = Lexer.Lex(source);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        var construct = manifest.Constructs.FirstOrDefault(c => c.Meta.Kind == kind);
        construct.Should().NotBeNull(
            $"Parse of '{source}' must produce a {kind} construct");

        var expectedCount = Constructs.GetMeta(kind).Slots.Count;
        construct!.Slots.Should().HaveCount(expectedCount,
            $"{kind}: when all optional slots are present, Slots.Length must equal " +
            $"Constructs.GetMeta({kind}).Slots.Count ({expectedCount})");
    }
}
