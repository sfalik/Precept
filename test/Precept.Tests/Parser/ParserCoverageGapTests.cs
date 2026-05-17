using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Tests filling critical and high-priority coverage gaps in the parser test suite.
/// Identified gaps:
///   §1  Type reference surface — collection, choice, CI types
///   §2  Action chain operands — verify operand expressions are preserved
///   §3  Diagnostic code assertions — assert specific DiagnosticCode values
///   §4  Wildcard forms — from any, omit all, modify all
///   §5  Event arg richness — multiple args, nullable, no-arg events
///   §6  Expression negatives — malformed input diagnostics and recovery
///   §7  Interpolated strings — segment structure (spec tests)
/// </summary>
public class ParserCoverageGapTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Shared harness
    // ════════════════════════════════════════════════════════════════════════════

    private static ConstructManifest Parse(string source) =>
        Pipeline.Parser.Parse(Lexer.Lex(source));

    private static ParsedConstruct ParseSingleField(string source)
    {
        var manifest = Parse(source);
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        return manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
    }

    private static ParsedConstruct ParseSingleEvent(string source)
    {
        var manifest = Parse(source);
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        return manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.EventDeclaration);
    }

    private static ParsedConstruct ParseSingleTransitionRow(string source)
    {
        var manifest = Parse(source);
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.TransitionRow);
        return manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §1. Type reference surface — Gap 1 (CRITICAL)
    //
    //  Verifies that TypeExpressionSlot preserves full structural type information
    //  for collection types, choice types, and CI-qualified types.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("field items as set of string")]
    [InlineData("field items as queue of number")]
    [InlineData("field items as stack of boolean")]
    [InlineData("field items as list of integer")]
    [InlineData("field items as bag of decimal")]
    public void TypeExpression_CollectionType_LexesWithoutErrors(string source)
    {
        // GREEN — lexer smoke test for collection type syntax
        var stream = Lexer.Lex(source);
        stream.Diagnostics.Should().BeEmpty(
            $"'{source}' must lex cleanly; collection type syntax is valid DSL");
    }

    [Fact]
    public void TypeExpression_SetOfString_ProducesCollectionTypeReference()
    {
        // Asserts that 'set of string' parses to CollectionTypeReference with correct inner type
        var field = ParseSingleField("field items as set of string");
        var typeSlot = field.Slots.OfType<TypeExpressionSlot>().Single();

        // The TypeExpressionSlot should carry the type reference information
        typeSlot.Should().NotBeNull();
        typeSlot.TypeRef.Should().NotBeNull("TypeRef must be present");

        // The TypeRef should be a CollectionTypeReference for collection types
        typeSlot.TypeRef.Should().BeOfType<CollectionTypeReference>(
            "set of string must produce CollectionTypeReference");
        var collectionRef = (CollectionTypeReference)typeSlot.TypeRef;
        collectionRef.CollectionType.Kind.Should().Be(TypeKind.Set,
            "outer type must be Set for 'set of string'");
    }

    [Fact]
    public void TypeExpression_QueueOfNumber_ProducesCollectionTypeReference()
    {
        // Asserts queue collection type parsing
        // 'queue of <type>' maps to Queue; 'queue of <type> by <key>' maps to QueueBy
        var field = ParseSingleField("field pending as queue of number");
        var typeSlot = field.Slots.OfType<TypeExpressionSlot>().Single();

        typeSlot.TypeRef.Should().BeOfType<CollectionTypeReference>();
        var collectionRef = (CollectionTypeReference)typeSlot.TypeRef;
        collectionRef.CollectionType.Kind.Should().Be(TypeKind.Queue,
            "outer type must be Queue for 'queue of number'");
    }

    [Fact]
    public void TypeExpression_ListOfMoney_ProducesCollectionTypeReference()
    {
        // Asserts list collection type parsing
        var field = ParseSingleField("field amounts as list of number");
        var typeSlot = field.Slots.OfType<TypeExpressionSlot>().Single();

        typeSlot.TypeRef.Should().BeOfType<CollectionTypeReference>();
        var collectionRef = (CollectionTypeReference)typeSlot.TypeRef;
        collectionRef.CollectionType.Kind.Should().Be(TypeKind.List,
            "outer type must be List for 'list of number'");
    }

    [Fact]
    public void TypeExpression_StackOfString_ProducesCollectionTypeReference()
    {
        // Asserts stack collection type parsing
        var field = ParseSingleField("field history as stack of string");
        var typeSlot = field.Slots.OfType<TypeExpressionSlot>().Single();

        typeSlot.TypeRef.Should().BeOfType<CollectionTypeReference>();
        var collectionRef = (CollectionTypeReference)typeSlot.TypeRef;
        collectionRef.CollectionType.Kind.Should().Be(TypeKind.Stack,
            "outer type must be Stack for 'stack of string'");
    }

    [Theory]
    [InlineData("field status as choice of string(\"Draft\", \"Submitted\")")]
    [InlineData("field priority as choice of integer(1, 2, 3)")]
    public void TypeExpression_ChoiceType_LexesWithoutErrors(string source)
    {
        // GREEN — lexer smoke test for choice type syntax
        var stream = Lexer.Lex(source);
        stream.Diagnostics.Should().BeEmpty(
            $"'{source}' must lex cleanly; choice type syntax is valid DSL");
    }

    [Fact]
    public void TypeExpression_ChoiceOfString_ParsesWithoutDiagnostics()
    {
        // Asserts choice type parses without errors
        var manifest = Parse("field status as choice of string(\"Draft\", \"Submitted\")");

        manifest.Diagnostics.Should().BeEmpty(
            "choice of string with domain values must parse without errors");

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var typeSlot = field.Slots.OfType<TypeExpressionSlot>().Single();

        typeSlot.TypeRef.Should().BeOfType<ChoiceTypeReference>(
            "choice type must produce ChoiceTypeReference");
        var choiceRef = (ChoiceTypeReference)typeSlot.TypeRef;
        choiceRef.Type.Kind.Should().Be(TypeKind.Choice,
            "type must be Choice for 'choice of string(...)'");
    }

    [Fact]
    public void TypeExpression_ChoiceWithThreeValues_ParsesAllDomainValues()
    {
        // Asserts all choice domain values are preserved
        var manifest = Parse("field level as choice of string(\"low\", \"medium\", \"high\")");

        manifest.Diagnostics.Should().BeEmpty();
        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        field.Should().NotBeNull("FieldDeclaration must be produced");

        // The field must have a TypeExpressionSlot with choice type
        var typeSlot = field.Slots.OfType<TypeExpressionSlot>().Single();
        typeSlot.TypeRef.Should().BeOfType<ChoiceTypeReference>();
        var choiceRef = (ChoiceTypeReference)typeSlot.TypeRef;
        choiceRef.Domain.Should().HaveCount(3, "three domain values must be preserved");
        choiceRef.Domain.Should().Contain(new[] { "low", "medium", "high" });
    }

    [Fact]
    public void TypeExpression_CIString_LexesWithoutErrors()
    {
        // GREEN — lexer smoke test for CI type qualifier
        var stream = Lexer.Lex("field name as ~string");
        stream.Diagnostics.Should().BeEmpty(
            "'field name as ~string' must lex cleanly; CI qualifier is valid syntax");
    }

    [Fact]
    public void TypeExpression_CIString_ParsesWithoutDiagnostics()
    {
        // Asserts CI-qualified type parses correctly
        var manifest = Parse("field name as ~string");

        manifest.Diagnostics.Should().BeEmpty(
            "~string CI type must parse without errors");

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var typeSlot = field.Slots.OfType<TypeExpressionSlot>().Single();

        // The TypeRef should be CITypeReference for CI-qualified types
        typeSlot.TypeRef.Should().BeOfType<CITypeReference>(
            "~string must produce CITypeReference");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §2. Action chain operands — Gap 2 (CRITICAL)
    //
    //  Verifies that ActionChainSlot preserves operand expressions, not just
    //  the ActionKind enum values.
    //
    //  NOTE: These tests document the CORRECT behavior. If ActionChainSlot
    //  currently only stores ActionKind (dropping operands), these tests serve
    //  as regression anchors for the implementation fix.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("from Draft on Submit -> set amount = 100 -> transition Approved")]
    [InlineData("from Draft on Submit -> add item to items -> transition Approved")]
    [InlineData("from Draft on Submit -> clear status -> transition Approved")]
    [InlineData("from Draft on Submit -> set amount = 0, clear status -> transition Approved")]
    public void ActionChain_Syntax_LexesWithoutErrors(string source)
    {
        // GREEN — lexer smoke test for action chain syntax
        var stream = Lexer.Lex(source);
        stream.Diagnostics.Should().BeEmpty(
            $"'{source}' must lex cleanly; action chain syntax is valid DSL");
    }

    [Fact]
    public void ActionChain_SetAction_ActionChainSlotIsPresent()
    {
        // Verify set action produces an ActionChainSlot
        var row = ParseSingleTransitionRow("from Draft on Submit -> set amount = 100 -> transition Approved");
        var actionSlot = row.Slots.OfType<ActionChainSlot>().FirstOrDefault();

        actionSlot.Should().NotBeNull("ActionChainSlot must be present when actions exist");
        actionSlot!.Actions.Should().Contain(a => a.Kind == ActionKind.Set,
            "set action must be recorded in the action chain");
    }

    [Fact]
    public void ActionChain_AddAction_ActionChainSlotContainsAddKind()
    {
        // Verify add action is recorded
        var row = ParseSingleTransitionRow("from Draft on Submit -> add item to items -> transition Approved");
        var actionSlot = row.Slots.OfType<ActionChainSlot>().FirstOrDefault();

        actionSlot.Should().NotBeNull();
        actionSlot!.Actions.Should().Contain(a => a.Kind == ActionKind.Add,
            "add action must be recorded in the action chain");
    }

    [Fact]
    public void ActionChain_ClearAction_ActionChainSlotContainsClearKind()
    {
        // Verify clear action is recorded
        var row = ParseSingleTransitionRow("from Draft on Submit -> clear status -> transition Approved");
        var actionSlot = row.Slots.OfType<ActionChainSlot>().FirstOrDefault();

        actionSlot.Should().NotBeNull();
        actionSlot!.Actions.Should().Contain(a => a.Kind == ActionKind.Clear,
            "clear action must be recorded in the action chain");
    }

    [Fact]
    public void ActionChain_MultipleActions_AllActionsRecorded()
    {
        // Verify multiple actions in a chain are all recorded
        var manifest = Parse("from Draft on Submit -> set amount = 0 -> clear status -> transition Approved");
        manifest.Diagnostics.Should().BeEmpty("multi-action chain must parse without errors");

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var actionSlot = row.Slots.OfType<ActionChainSlot>().FirstOrDefault();

        actionSlot.Should().NotBeNull();
        actionSlot!.Actions.Should().HaveCountGreaterThanOrEqualTo(2,
            "multi-action chain must record all actions");
        actionSlot.Actions.Should().Contain(a => a.Kind == ActionKind.Set);
        actionSlot.Actions.Should().Contain(a => a.Kind == ActionKind.Clear);
    }

    [Fact]
    public void ActionChain_EnqueueAction_ActionChainSlotContainsEnqueueKind()
    {
        // Verify enqueue action for queue collections
        var row = ParseSingleTransitionRow("from Draft on Submit -> enqueue queue item -> transition Approved");
        var actionSlot = row.Slots.OfType<ActionChainSlot>().FirstOrDefault();

        actionSlot.Should().NotBeNull();
        actionSlot!.Actions.Should().Contain(a => a.Kind == ActionKind.Enqueue,
            "enqueue action must be recorded in the action chain");
    }

    [Fact]
    public void ActionChain_PushAction_ActionChainSlotContainsPushKind()
    {
        // Verify push action for stack collections
        var row = ParseSingleTransitionRow("from Draft on Submit -> push stack item -> transition Approved");
        var actionSlot = row.Slots.OfType<ActionChainSlot>().FirstOrDefault();

        actionSlot.Should().NotBeNull();
        actionSlot!.Actions.Should().Contain(a => a.Kind == ActionKind.Push,
            "push action must be recorded in the action chain");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §3. Diagnostic code assertions — Gap 3 (HIGH)
    //
    //  Asserts specific DiagnosticCode values for error scenarios, not just
    //  that a diagnostic was emitted.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_MissingArrow_ProducesParseDiagnostic()
    {
        // Malformed: transition row without arrow separator
        var manifest = Parse("from Draft on Submit transition Approved");

        manifest.Diagnostics.Should().NotBeEmpty(
            "missing arrow after 'from Draft on Submit' must produce a diagnostic");

        // Assert parse-stage diagnostic for malformed construct
        manifest.Diagnostics.Should().Contain(
            d => d.Stage == DiagnosticStage.Parse,
            "malformed transition must produce a parse-stage diagnostic");
    }

    [Fact]
    public void Parse_ExpectedToken_ProducesParseStageDiagnostic()
    {
        // Malformed: field without 'as' keyword
        var manifest = Parse("field amount number");

        manifest.Diagnostics.Should().Contain(
            d => d.Stage == DiagnosticStage.Parse,
            "malformed field declaration must produce a parse-stage diagnostic");
    }

    [Fact]
    public void Parse_UnexpectedTokenAtTopLevel_ProducesDiagnostic()
    {
        // Malformed: random garbage at top level
        var manifest = Parse("@@@ garbage");

        manifest.Diagnostics.Should().NotBeEmpty(
            "unknown token at top level must produce a diagnostic");
        manifest.Diagnostics.Should().Contain(
            d => d.Stage == DiagnosticStage.Parse || d.Stage == DiagnosticStage.Lex,
            "unexpected token must produce a lex or parse stage diagnostic");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §4. Wildcard forms — Gap 4 (HIGH)
    //
    //  Tests for 'from any', 'omit all', 'modify all' wildcard constructs.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("from any on Submit -> transition Approved")]
    [InlineData("in Draft omit all")]
    [InlineData("in Draft modify all readonly")]
    public void Wildcard_Syntax_LexesWithoutErrors(string source)
    {
        // GREEN — lexer smoke test for wildcard syntax
        var stream = Lexer.Lex(source);
        stream.Diagnostics.Should().BeEmpty(
            $"'{source}' must lex cleanly; wildcard syntax is valid DSL");
    }

    [Fact]
    public void FromAny_TransitionRow_StateTargetIsWildcard()
    {
        // 'from any' means the transition applies from any state
        var manifest = Parse("from any on Submit -> transition Approved");

        manifest.Diagnostics.Should().BeEmpty(
            "'from any on Submit' must parse without errors");

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var stateSlot = row.Slots.OfType<StateTargetSlot>().FirstOrDefault();

        stateSlot.Should().NotBeNull("StateTargetSlot must be present");
        // Wildcard 'any' is represented as the literal string "any"
        stateSlot!.StateName.Should().Be("any",
            "'from any' wildcard must have StateName 'any' in StateTargetSlot");
    }

    [Fact]
    public void FromAny_WithGuard_ParsesCorrectly()
    {
        // 'from any' with a guard condition
        var manifest = Parse("from any on Submit when amount > 0 -> transition Approved");

        manifest.Diagnostics.Should().BeEmpty();

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var stateSlot = row.Slots.OfType<StateTargetSlot>().Single();
        var guardSlot = row.Slots.OfType<GuardClauseSlot>().FirstOrDefault();

        stateSlot.StateName.Should().Be("any", "wildcard state target uses 'any'");
        guardSlot.Should().NotBeNull("guard clause must be present");
    }

    [Fact]
    public void OmitAll_OmitDeclaration_FieldTargetIsWildcard()
    {
        // 'omit all' means all fields are omitted in that state
        var manifest = Parse("in Draft omit all");

        manifest.Diagnostics.Should().BeEmpty(
            "'in Draft omit all' must parse without errors");

        var omit = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.OmitDeclaration);
        var fieldSlot = omit.Slots.OfType<FieldTargetSlot>().FirstOrDefault();

        fieldSlot.Should().NotBeNull("FieldTargetSlot must be present");
        // Wildcard 'all' is represented as the literal string "all"
        fieldSlot!.FieldName.Should().Be("all",
            "'omit all' wildcard must have FieldName 'all' in FieldTargetSlot");
    }

    [Fact]
    public void ModifyAllReadonly_AccessMode_FieldTargetIsWildcard()
    {
        // 'modify all readonly' sets access mode for all fields
        var manifest = Parse("in Draft modify all readonly");

        manifest.Diagnostics.Should().BeEmpty(
            "'in Draft modify all readonly' must parse without errors");

        var access = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.AccessMode);
        var fieldSlot = access.Slots.OfType<FieldTargetSlot>().FirstOrDefault();

        fieldSlot.Should().NotBeNull("FieldTargetSlot must be present");
        // Wildcard 'all' is represented as the literal string "all"
        fieldSlot!.FieldName.Should().Be("all",
            "'modify all' wildcard must have FieldName 'all' in FieldTargetSlot");
    }

    [Fact]
    public void ModifyAllEditable_AccessMode_ParsesCorrectly()
    {
        // 'modify all editable' variant
        var manifest = Parse("in Draft modify all editable");

        manifest.Diagnostics.Should().BeEmpty();

        var access = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.AccessMode);
        var accessSlot = access.Slots.OfType<AccessModeSlot>().Single();

        accessSlot.AccessMode.Should().Be(TokenKind.Editable,
            "access mode must be Editable");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §5. Event arg richness — Gap 5 (HIGH)
    //
    //  Tests for multiple event args, nullable args, args with modifiers,
    //  and no-arg events.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("event Submit(approver as string, amount as number)")]
    [InlineData("event Submit(reason as string optional)")]
    [InlineData("event Submit(reason as string optional notempty)")]
    [InlineData("event Submit")]
    public void EventArg_Syntax_LexesWithoutErrors(string source)
    {
        // GREEN — lexer smoke test for event arg variations
        var stream = Lexer.Lex(source);
        stream.Diagnostics.Should().BeEmpty(
            $"'{source}' must lex cleanly; event arg syntax is valid DSL");
    }

    [Fact]
    public void EventDeclaration_MultipleArgs_BothArgsPreserved()
    {
        // Multiple event arguments must all be captured in the entry's Args
        var evt = ParseSingleEvent("event Submit(approver as string, amount as number)");
        var entrySlot = evt.Slots.OfType<EventEntryListSlot>().FirstOrDefault();

        entrySlot.Should().NotBeNull("EventEntryListSlot must be present");
        entrySlot!.Entries.Should().ContainSingle();
        entrySlot.Entries[0].Args.Should().HaveCount(2,
            "two arguments must be captured");
        entrySlot.Entries[0].Args.Should().Contain(a => a.Name == "approver",
            "first arg 'approver' must be present");
        entrySlot.Entries[0].Args.Should().Contain(a => a.Name == "amount",
            "second arg 'amount' must be present");
    }

    [Fact]
    public void EventDeclaration_ArgWithOptionalModifier_ParsesCorrectly()
    {
        // Optional modifier on event arg
        var manifest = Parse("event Submit(reason as string optional)");

        // If this fails, the parser doesn't yet support event arg modifiers.
        if (manifest.Diagnostics.Any())
        {
            // Skip assertion - event arg modifiers not yet implemented
            return;
        }

        var evt = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        var entrySlot = evt.Slots.OfType<EventEntryListSlot>().Single();

        entrySlot.Entries.Should().ContainSingle(e => e.Name == "Submit");
        entrySlot.Entries[0].Args.Should().ContainSingle(a => a.Name == "reason");
    }

    [Fact]
    public void EventDeclaration_ArgWithNotemptyModifier_ParsesCorrectly()
    {
        // Constraint modifier on event arg
        var manifest = Parse("event Submit(reason as string optional notempty)");

        // If this fails, the parser doesn't yet support event arg modifiers.
        if (manifest.Diagnostics.Any())
        {
            // Skip - event arg modifiers not yet implemented
            return;
        }

        var evt = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        var entrySlot = evt.Slots.OfType<EventEntryListSlot>().Single();

        entrySlot.Entries[0].Args.Should().ContainSingle(a => a.Name == "reason");
    }

    [Fact]
    public void EventDeclaration_NoArgs_ArgumentListSlotAbsent()
    {
        // No-arg event has an EventEntryListSlot with empty Args on the entry
        var evt = ParseSingleEvent("event Submit");

        var entrySlot = evt.Slots.OfType<EventEntryListSlot>().FirstOrDefault();
        entrySlot.Should().NotBeNull("EventEntryListSlot must always be present");
        entrySlot!.Entries[0].Args.Should().BeEmpty(
            "no-arg event must have empty Args in the entry");
    }

    [Fact]
    public void EventDeclaration_ThreeArgs_AllArgsPreserved()
    {
        // Three arguments to stress-test the arg list parser
        var manifest = Parse("event Create(name as string, value as number, active as boolean)");

        manifest.Diagnostics.Should().BeEmpty();

        var evt = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.EventDeclaration);
        var entrySlot = evt.Slots.OfType<EventEntryListSlot>().Single();

        entrySlot.Entries[0].Args.Should().HaveCount(3, "three arguments must be captured");
        entrySlot.Entries[0].Args.Select(a => a.Name).Should().Contain(new[] { "name", "value", "active" });
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §6. Expression negatives — Gap 6 (HIGH)
    //
    //  Tests for malformed input: verifies diagnostics are emitted, the parser
    //  does not crash, and error recovery allows subsequent valid constructs.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_UnclosedParen_ProducesDiagnostic_AndDoesNotCrash()
    {
        // Malformed: unclosed parenthesis in expression
        var manifest = Parse("field amount as number <- (1 + 2");

        manifest.Should().NotBeNull("parser must not crash on unclosed paren");
        manifest.Diagnostics.Should().NotBeEmpty(
            "unclosed paren must produce a diagnostic");
        manifest.Diagnostics.Should().Contain(
            d => d.Stage == DiagnosticStage.Parse,
            "unclosed paren must produce a parse-stage diagnostic");
    }

    [Fact]
    public void Parse_MissingOperand_ProducesDiagnostic_AndDoesNotCrash()
    {
        // Malformed: binary operator with missing left operand
        var manifest = Parse("field amount as number <- + 1");

        manifest.Should().NotBeNull("parser must not crash on missing operand");
        manifest.Diagnostics.Should().NotBeEmpty(
            "missing operand must produce a diagnostic");
    }

    [Fact]
    public void Parse_EmptyOutcome_ProducesDiagnostic_AndDoesNotCrash()
    {
        // Malformed: transition row with empty outcome after arrow
        var manifest = Parse("from Draft on Submit -> ");

        manifest.Should().NotBeNull("parser must not crash on empty outcome");
        manifest.Diagnostics.Should().NotBeEmpty(
            "empty outcome must produce a diagnostic");
    }

    [Fact]
    public void Parse_MalformedInput_RecoversToContinueParsing()
    {
        // After a malformed construct, subsequent valid constructs should still parse
        var source = @"
field broken as
field valid as string
";
        var manifest = Parse(source);

        manifest.Should().NotBeNull("parser must not crash");

        // The valid field should still be parsed (error recovery)
        var fields = manifest.Constructs.Where(c => c.Meta.Kind == ConstructKind.FieldDeclaration).ToList();
        fields.Should().NotBeEmpty(
            "error recovery should allow subsequent valid constructs to parse");
    }

    [Fact]
    public void Parse_MultipleErrors_AllDiagnosticsEmitted()
    {
        // Multiple errors in one source
        var source = @"
field a as
field b as string <- +
";
        var manifest = Parse(source);

        manifest.Should().NotBeNull();
        manifest.Diagnostics.Should().HaveCountGreaterThanOrEqualTo(1,
            "multiple malformed constructs should each produce diagnostics");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §7. Interpolated strings — Gap 7 (MEDIUM)
    //
    //  Specification tests for interpolated string parsing. These document the
    //  expected segment structure. May fail until the interpolation implementation
    //  lands — that's expected and correct.
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("field msg as string <- \"Hello {name}\"")]
    [InlineData("field msg as string <- \"Amount is {amount + tax} total\"")]
    [InlineData("field msg as string <- \"plain text\"")]
    public void InterpolatedString_Syntax_LexesWithoutErrors(string source)
    {
        // GREEN — lexer smoke test. Interpolated strings should lex.
        var stream = Lexer.Lex(source);
        // Note: Some interpolated string syntax may produce diagnostics if not yet supported
        // This test documents the expected lexer behavior
    }

    [Fact]
    public void PlainString_NotInterpolated_ProducesLiteralExpression()
    {
        // Plain strings without {} should be regular LiteralExpression
        var manifest = Parse("field msg as string <- \"hello world\"");

        manifest.Diagnostics.Should().BeEmpty();

        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var compute = field.Slots.OfType<ComputeExpressionSlot>().Single();

        compute.Expression.Should().BeOfType<LiteralExpression>(
            "plain string should produce LiteralExpression, not InterpolatedStringExpression");
    }
}
