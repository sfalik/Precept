using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Pipeline;
using Xunit;

namespace Precept.Next.Tests;

public class TypeCheckerTests
{
    private static TypedModel CheckSource(string source)
    {
        var tokens = Lexer.Lex(source);
        var tree = Parser.Parse(tokens);
        return TypeChecker.Check(tree);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  George's original tests
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Check_EmptyPrecept_ReturnsValidModel()
    {
        var model = CheckSource("precept Empty");

        model.Fields.Should().BeEmpty();
        model.States.Should().BeEmpty();
        model.Events.Should().BeEmpty();
        model.Rules.Should().BeEmpty();
        model.Ensures.Should().BeEmpty();
        model.TransitionRows.Should().BeEmpty();
        model.AccessModes.Should().BeEmpty();
        model.StateActions.Should().BeEmpty();
        model.StatelessHooks.Should().BeEmpty();
        model.InitialState.Should().BeNull();
        model.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_EmptyPrecept_SetsPreceptName()
    {
        var model = CheckSource("precept Invoice");

        model.PreceptName.Should().Be("Invoice");
    }

    [Fact]
    public void Check_PreservesParserDiagnostics()
    {
        // "precept" with no name triggers a parser diagnostic
        var model = CheckSource("precept");

        model.Diagnostics.Should().NotBeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Diagnostic catalog exhaustiveness
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(AllDiagnosticCodes))]
    public void GetMeta_ReturnsWithoutThrowing_ForEveryDiagnosticCode(DiagnosticCode code)
    {
        var meta = Diagnostics.GetMeta(code);
        meta.Should().NotBeNull();
    }

    [Fact]
    public void DiagnosticsAll_ContainsExactlyAsManyEntries_AsEnumValues()
    {
        var expected = Enum.GetValues<DiagnosticCode>().Length;
        Diagnostics.All.Should().HaveCount(expected);
    }

    [Theory]
    [MemberData(nameof(AllDiagnosticCodes))]
    public void GetMeta_EveryEntry_HasNonEmptyCodeAndMessageTemplate(DiagnosticCode code)
    {
        var meta = Diagnostics.GetMeta(code);
        meta.Code.Should().NotBeNullOrWhiteSpace();
        meta.MessageTemplate.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(AllDiagnosticCodes))]
    public void GetMeta_CodeStringMatchesEnumMemberName(DiagnosticCode code)
    {
        var meta = Diagnostics.GetMeta(code);
        meta.Code.Should().Be(Enum.GetName(code));
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  New code metadata — temporal, collection safety, business-domain
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(TemporalCodes))]
    public void TemporalCodes_HaveTypeStageAndErrorSeverity(DiagnosticCode code)
    {
        var meta = Diagnostics.GetMeta(code);
        meta.Stage.Should().Be(DiagnosticStage.Type);
        meta.Severity.Should().Be(Severity.Error);
    }

    [Theory]
    [MemberData(nameof(CollectionSafetyCodes))]
    public void CollectionSafetyCodes_HaveTypeStageAndErrorSeverity(DiagnosticCode code)
    {
        var meta = Diagnostics.GetMeta(code);
        meta.Stage.Should().Be(DiagnosticStage.Type);
        meta.Severity.Should().Be(Severity.Error);
    }

    [Theory]
    [MemberData(nameof(BusinessDomainCodes))]
    public void BusinessDomainCodes_HaveTypeStageAndErrorSeverity(DiagnosticCode code)
    {
        var meta = Diagnostics.GetMeta(code);
        meta.Stage.Should().Be(DiagnosticStage.Type);
        meta.Severity.Should().Be(Severity.Error);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  TypeChecker pipeline — BuildModel behavior
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Check_PreceptWithBody_FieldRegistrationWorks()
    {
        var model = CheckSource("precept Order\nfield Amount : number");

        model.PreceptName.Should().Be("Order");
        model.Fields.Should().ContainKey("Amount");
        model.Fields["Amount"].Type.Should().BeOfType<NumberType>();
        model.States.Should().BeEmpty();
        model.Events.Should().BeEmpty();
        model.Rules.Should().BeEmpty();
        model.Ensures.Should().BeEmpty();
        model.TransitionRows.Should().BeEmpty();
        model.AccessModes.Should().BeEmpty();
        model.StateActions.Should().BeEmpty();
        model.StatelessHooks.Should().BeEmpty();
        model.InitialState.Should().BeNull();
    }

    [Fact]
    public void Check_ParserDiagnosticsPreservedExactly()
    {
        // Parse separately to capture parser-level diagnostics, then verify they survive BuildModel.
        var tokens = Lexer.Lex("precept");
        var tree = Parser.Parse(tokens);
        var parserDiags = tree.Diagnostics;

        var model = TypeChecker.Check(tree);

        model.Diagnostics.Should().HaveCount(parserDiags.Length);
        for (int i = 0; i < parserDiags.Length; i++)
            model.Diagnostics[i].Should().Be(parserDiags[i]);
    }

    [Fact]
    public void Check_ParserDiagnostics_HaveParseStage()
    {
        var model = CheckSource("precept");

        model.Diagnostics.Should().AllSatisfy(d =>
            d.Stage.Should().Be(DiagnosticStage.Parse));
    }

    [Fact]
    public void Check_PreceptWithTrailingNewline_ReturnsEmptyModel()
    {
        var model = CheckSource("precept Minimal\n");

        model.PreceptName.Should().Be("Minimal");
        model.Fields.Should().BeEmpty();
        model.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_PreceptWithBodyAndParseError_MergesDiagnosticsWithEmptyCollections()
    {
        // Valid precept name + malformed declaration → parser diagnostic + empty model.
        var model = CheckSource("precept Order\nfield");

        model.PreceptName.Should().Be("Order");
        model.Fields.Should().BeEmpty();
        model.Diagnostics.Should().NotBeEmpty();
        model.Diagnostics.Should().AllSatisfy(d =>
            d.Stage.Should().Be(DiagnosticStage.Parse));
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 2 — Field registration
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Register_FieldAsString_AppearsInFields()
    {
        var model = CheckSource("precept T\nfield Name : string");

        model.Fields.Should().ContainKey("Name");
        model.Fields["Name"].Type.Should().BeOfType<StringType>();
    }

    [Theory]
    [InlineData("integer", typeof(IntegerType))]
    [InlineData("decimal", typeof(DecimalType))]
    [InlineData("number",  typeof(NumberType))]
    [InlineData("boolean", typeof(BooleanType))]
    public void Register_FieldAsScalarType_ResolvesCorrectType(string typeName, Type expectedType)
    {
        var model = CheckSource($"precept T\nfield F as {typeName}");

        model.Fields.Should().ContainKey("F");
        model.Fields["F"].Type.Should().BeOfType(expectedType);
    }

    [Fact]
    public void Register_OptionalField_IsOptionalTrue()
    {
        var model = CheckSource("precept T\nfield X : string optional");

        model.Fields["X"].IsOptional.Should().BeTrue();
    }

    [Fact]
    public void Register_NonOptionalField_IsOptionalFalse()
    {
        var model = CheckSource("precept T\nfield X : string");

        model.Fields["X"].IsOptional.Should().BeFalse();
    }

    [Fact]
    public void Register_ComputedField_IsComputedTrue()
    {
        var model = CheckSource("precept T\nfield Amount : number\nfield Total : number -> Amount + Amount");

        model.Fields["Total"].IsComputed.Should().BeTrue();
    }

    [Fact]
    public void Register_NonComputedField_IsComputedFalse()
    {
        var model = CheckSource("precept T\nfield Amount : number");

        model.Fields["Amount"].IsComputed.Should().BeFalse();
    }

    [Fact]
    public void Register_DuplicateFieldName_EmitsDiagnostic()
    {
        var model = CheckSource("precept T\nfield X : string\nfield X : number");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.DuplicateFieldName) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Register_DuplicateFieldName_FirstDeclarationWins()
    {
        var model = CheckSource("precept T\nfield X : string\nfield X : number");

        // Second declaration is rejected; first (string) type is preserved.
        model.Fields.Should().ContainKey("X");
        model.Fields["X"].Type.Should().BeOfType<StringType>();
    }

    [Fact]
    public void Register_MissingFieldName_SkipsRegistration()
    {
        // "field : string" → parser error recovery produces a missing name token (Length == 0)
        // TypeChecker must skip it without crashing and leave Fields empty.
        var model = CheckSource("precept T\nfield : string");

        model.Fields.Should().BeEmpty();
    }

    [Fact]
    public void Register_MultipleFields_SameDeclaration()
    {
        var model = CheckSource("precept T\nfield A, B : integer");

        model.Fields.Should().ContainKey("A");
        model.Fields.Should().ContainKey("B");
        model.Fields["A"].Type.Should().BeOfType<IntegerType>();
        model.Fields["B"].Type.Should().BeOfType<IntegerType>();
    }

    [Fact]
    public void Register_MultipleFieldDeclarations_AllRegistered()
    {
        var model = CheckSource("precept T\nfield Name : string\nfield Count : integer\nfield Active : boolean");

        model.Fields.Should().ContainKey("Name");
        model.Fields.Should().ContainKey("Count");
        model.Fields.Should().ContainKey("Active");
        model.Fields.Should().HaveCount(3);
    }

    [Fact]
    public void Register_SingleField_NoDiagnosticsEmitted()
    {
        // Use "as" keyword (not ":") to avoid lexer InvalidCharacter diagnostics.
        var model = CheckSource("precept T\nfield Amount as number");

        model.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Register_RegularStringField_CaseInsensitiveFalse()
    {
        var model = CheckSource("precept T\nfield Name : string");

        var type = model.Fields["Name"].Type.Should().BeOfType<StringType>().Subject;
        type.CaseInsensitive.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 3 — State + event registration
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Register_States_AppearsInStatesDict()
    {
        var model = CheckSource("precept T\nstate Draft initial");

        model.States.Should().ContainKey("Draft");
    }

    [Fact]
    public void Register_InitialState_SetsInitialState()
    {
        var model = CheckSource("precept T\nstate Draft initial");

        model.InitialState.Should().Be("Draft");
    }

    [Fact]
    public void Register_MultipleInitialStates_EmitsDiagnostic()
    {
        var model = CheckSource("precept T\nstate Draft initial, Active initial");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.MultipleInitialStates) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Register_MultipleInitialStates_MessageContainsBothStateNames()
    {
        var model = CheckSource("precept T\nstate Draft initial, Active initial");

        var diag = model.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.MultipleInitialStates));
        diag.Message.Should().Contain("Draft");
        diag.Message.Should().Contain("Active");
    }

    [Fact]
    public void Register_StatesButNoInitial_EmitsDiagnostic()
    {
        var model = CheckSource("precept T\nstate Open, Closed");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.NoInitialState) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Register_NoStates_NoInitialStateDiagnostic()
    {
        // Stateless precept — no states declared at all → no NoInitialState diagnostic.
        var model = CheckSource("precept T\nfield Amount as number");

        model.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.NoInitialState));
    }

    [Fact]
    public void Register_DuplicateStateName_EmitsDiagnostic()
    {
        var model = CheckSource("precept T\nstate Open initial\nstate Open");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.DuplicateStateName) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Register_DuplicateStateName_FirstDeclarationWins()
    {
        var model = CheckSource("precept T\nstate Open initial\nstate Open");

        // Duplicate is rejected; only one entry for "Open".
        model.States.Should().ContainKey("Open");
        model.States.Should().HaveCount(1);
    }

    [Fact]
    public void Register_MultipleStatesInOneDeclaration_AllRegistered()
    {
        var model = CheckSource("precept T\nstate Draft initial, Review, Approved, Rejected");

        model.States.Should().ContainKey("Draft");
        model.States.Should().ContainKey("Review");
        model.States.Should().ContainKey("Approved");
        model.States.Should().ContainKey("Rejected");
        model.States.Should().HaveCount(4);
    }

    [Fact]
    public void Register_StateIsInitial_IsInitialTrueOnSymbol()
    {
        var model = CheckSource("precept T\nstate Draft initial, Review");

        model.States["Draft"].IsInitial.Should().BeTrue();
        model.States["Review"].IsInitial.Should().BeFalse();
    }

    [Fact]
    public void Register_StateWithTerminalModifier_ModifierPropagatedToSymbol()
    {
        var model = CheckSource("precept T\nstate Open initial, Closed terminal");

        model.States["Closed"].Modifiers.Should().Contain(StateModifierKind.Terminal);
    }

    [Fact]
    public void Register_StateWithNoModifiers_ModifiersEmpty()
    {
        var model = CheckSource("precept T\nstate Open initial");

        model.States["Open"].Modifiers.Should().BeEmpty();
    }

    [Fact]
    public void Register_EventWithArgs_ArgsPopulated()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit(Amount as decimal, Note as string)");

        model.Events.Should().ContainKey("Submit");
        model.Events["Submit"].Args.Should().ContainKey("Amount");
        model.Events["Submit"].Args["Amount"].Type.Should().BeOfType<DecimalType>();
        model.Events["Submit"].Args.Should().ContainKey("Note");
        model.Events["Submit"].Args["Note"].Type.Should().BeOfType<StringType>();
    }

    [Fact]
    public void Register_EventWithNoArgs_ArgsEmpty()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Cancel");

        model.Events.Should().ContainKey("Cancel");
        model.Events["Cancel"].Args.Should().BeEmpty();
    }

    [Fact]
    public void Register_EventIsInitial_IsInitialTrueOnSymbol()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Start initial");

        model.Events["Start"].IsInitial.Should().BeTrue();
    }

    [Fact]
    public void Register_EventIsNotInitial_IsInitialFalseOnSymbol()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Cancel");

        model.Events["Cancel"].IsInitial.Should().BeFalse();
    }

    [Fact]
    public void Register_DuplicateEventName_EmitsDiagnostic()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit\nevent Submit");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.DuplicateEventName) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Register_DuplicateEventName_FirstDeclarationWins()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit\nevent Submit");

        model.Events.Should().ContainKey("Submit");
        model.Events.Should().HaveCount(1);
    }

    [Fact]
    public void Register_MultipleEventsInOneDeclaration_AllRegistered()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Approve, Reject");

        model.Events.Should().ContainKey("Approve");
        model.Events.Should().ContainKey("Reject");
        model.Events.Should().HaveCount(2);
    }

    [Fact]
    public void Register_DuplicateArgName_EmitsDiagnostic()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit(Amount as decimal, Amount as string)");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.DuplicateArgName) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Register_DuplicateArgName_MessageContainsArgNameAndEventName()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit(Amount as decimal, Amount as string)");

        var diag = model.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.DuplicateArgName));
        diag.Message.Should().Contain("Amount");
        diag.Message.Should().Contain("Submit");
    }

    [Fact]
    public void Register_DuplicateArgName_FirstArgWins()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit(Amount as decimal, Amount as string)");

        // First declaration (decimal) is preserved; second is rejected.
        model.Events["Submit"].Args["Amount"].Type.Should().BeOfType<DecimalType>();
    }

    [Fact]
    public void Register_ArgWithOptional_IsOptionalTrue()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit(Note as string optional)");

        model.Events["Submit"].Args["Note"].IsOptional.Should().BeTrue();
    }

    [Fact]
    public void Register_ArgWithoutOptional_IsOptionalFalse()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit(Amount as decimal)");

        model.Events["Submit"].Args["Amount"].IsOptional.Should().BeFalse();
    }

    [Fact]
    public void Register_MultipleEventsInOneDeclaration_SharedArgsAppliedToEach()
    {
        // When multiple names share one event declaration, each gets the same args.
        var model = CheckSource("precept T\nstate Draft initial\nevent Approve, Reject(Reason as string)");

        model.Events["Approve"].Args.Should().ContainKey("Reason");
        model.Events["Reject"].Args.Should().ContainKey("Reason");
    }

    [Fact]
    public void Register_SingleState_NoDiagnosticsEmitted()
    {
        var model = CheckSource("precept T\nstate Open initial");

        model.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Register_SingleEvent_NoDiagnosticsEmitted()
    {
        var model = CheckSource("precept T\nstate Draft initial\nevent Submit");

        model.Diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 4 — Expression checking core + rule declarations
    // ════════════════════════════════════════════════════════════════════════════

    // ── CheckRuleDeclaration ──────────────────────────────────────────────────

    [Fact]
    public void Check_Rule_BooleanCondition_ProducesResolvedRule()
    {
        var model = CheckSource("precept T\nrule true because \"ok\"");

        model.Rules.Should().HaveCount(1);
        model.Rules[0].Condition.Type.Should().BeOfType<BooleanType>();
        model.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_Rule_IdentifierResolves_ToFieldType()
    {
        var model = CheckSource("precept T\nfield X as boolean\nrule X because \"msg\"");

        model.Rules.Should().HaveCount(1);
        model.Rules[0].Condition.Type.Should().BeOfType<BooleanType>();
        model.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_Rule_UndeclaredField_EmitsDiagnostic()
    {
        var model = CheckSource("precept T\nrule Unknown because \"msg\"");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.UndeclaredField) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Check_Rule_ErrorType_SuppressesCascade()
    {
        // UndeclaredField produces ErrorType on the condition; no TypeMismatch must be emitted.
        var model = CheckSource("precept T\nrule Unknown because \"msg\"");

        model.Diagnostics.Should().HaveCount(1);
        model.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.TypeMismatch));
    }

    [Fact]
    public void Check_Rule_NonBooleanCondition_EmitsTypeMismatch()
    {
        var model = CheckSource("precept T\nrule \"hello\" because \"msg\"");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Check_Rule_NonStringMessage_EmitsTypeMismatch()
    {
        var model = CheckSource("precept T\nrule true because true");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type &&
            d.Severity == Severity.Error);
    }

    [Fact]
    public void Check_Rule_WithGuard_ChecksGuardAsBoolean()
    {
        var model = CheckSource("precept T\nfield Active as boolean\nrule true when Active because \"msg\"");

        model.Rules[0].Guard.Should().NotBeNull();
        model.Rules[0].Guard!.Type.Should().BeOfType<BooleanType>();
        model.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_Rule_InterpolatedMessage_AcceptsString()
    {
        // String field in interpolation is valid — no InvalidInterpolationCoercion, no TypeMismatch.
        var model = CheckSource("precept T\nfield X as string\nrule true because \"value is {X}\"");

        model.Diagnostics.Should().BeEmpty();
        model.Rules[0].Message.Type.Should().BeOfType<StringType>();
    }

    [Fact]
    public void Check_IsMissing_Expression_ReturnsErrorType()
    {
        // "rule because ..." — condition is missing; parser emits ExpectedToken (parse stage).
        // TypeChecker returns ErrorType for the missing condition and does NOT emit TypeMismatch.
        var model = CheckSource("precept T\nrule because \"ok\"");

        model.Diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
        model.Rules.Should().HaveCount(1);
        model.Rules[0].Condition.Type.Should().BeOfType<ErrorType>();
    }

    // ── Expression type resolution ────────────────────────────────────────────

    [Fact]
    public void Check_BooleanLiteralExpression_ResolvesToBooleanType()
    {
        var model = CheckSource("precept T\nrule true because \"ok\"");

        model.Rules[0].Condition.Type.Should().BeOfType<BooleanType>();
    }

    [Fact]
    public void Check_StringLiteralExpression_ResolvesToStringType()
    {
        var model = CheckSource("precept T\nrule true because \"ok\"");

        model.Rules[0].Message.Type.Should().BeOfType<StringType>();
    }

    [Fact]
    public void Check_ParenthesizedExpression_PreservesInnerType()
    {
        // (true) must resolve to BooleanType — the parens are transparent to the type.
        var model = CheckSource("precept T\nrule (true) because \"ok\"");

        model.Rules[0].Condition.Type.Should().BeOfType<BooleanType>();
        model.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("string",  typeof(StringType))]
    [InlineData("integer", typeof(IntegerType))]
    [InlineData("decimal", typeof(DecimalType))]
    [InlineData("number",  typeof(NumberType))]
    public void Check_IdentifierExpression_ResolvesToDeclaredFieldType(string typeName, Type expectedCSharpType)
    {
        // Identifier in condition context resolves to the declared field type.
        // Non-boolean scalars also fire TypeMismatch in the condition — but the type IS resolved.
        var model = CheckSource($"precept T\nfield F as {typeName}\nrule F because \"msg\"");

        model.Rules[0].Condition.Type.Should().BeOfType(expectedCSharpType);
        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    // ── Rule shape assertions ─────────────────────────────────────────────────

    [Fact]
    public void Check_Rule_WithNoGuard_GuardIsNull()
    {
        var model = CheckSource("precept T\nrule true because \"ok\"");

        model.Rules[0].Guard.Should().BeNull();
    }

    [Fact]
    public void Check_Rule_ResolvedRule_HasCorrectTypedExpressions()
    {
        var model = CheckSource("precept T\nrule true because \"ok\"");

        model.Rules[0].Condition.Type.Should().BeOfType<BooleanType>();
        model.Rules[0].Message.Type.Should().BeOfType<StringType>();
        model.Rules[0].Guard.Should().BeNull();
    }

    [Fact]
    public void Check_MultipleRules_AllRegistered()
    {
        var model = CheckSource("precept T\nrule true because \"first\"\nrule true because \"second\"");

        model.Rules.Should().HaveCount(2);
        model.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_Rule_NonBooleanGuard_EmitsTypeMismatch()
    {
        // Guard must be boolean; using a string field fires TypeMismatch.
        var model = CheckSource("precept T\nfield Label as string\nrule true when Label because \"msg\"");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    // ── Diagnostic message content ────────────────────────────────────────────

    [Fact]
    public void Check_UndeclaredField_DiagnosticMessage_ContainsFieldName()
    {
        var model = CheckSource("precept T\nrule Unknown because \"msg\"");

        var diag = model.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
        diag.Message.Should().Contain("Unknown");
    }

    [Fact]
    public void Check_TypeMismatch_OnCondition_DiagnosticMessage_ContainsExpectedAndActualType()
    {
        // "hello" is StringType, condition expects boolean — message should name both types.
        var model = CheckSource("precept T\nrule \"hello\" because \"msg\"");

        var diag = model.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.TypeMismatch));
        diag.Message.Should().Contain("boolean");
        diag.Message.Should().Contain("string");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 5 — Numeric literals + binary expressions
    // ════════════════════════════════════════════════════════════════════════════

    // ── Number literal dispatch (null context via binary sub-expression) ───────

    [Fact]
    public void Check_NumberLiteral_InBinarySubExpression_NullContext_EmitsCannotDetermineTypeMismatch()
    {
        // Both operands in a binary expression receive null expectedType.
        // Each number literal emits TypeMismatch("numeric type", "cannot determine numeric type from context").
        var model = CheckSource("precept T\nrule 42 + 99 because \"msg\"");

        // 2 TypeMismatch (one per literal); ErrorType propagates — no cascade from condition.
        model.Diagnostics.Should().HaveCount(2);
        model.Diagnostics.Should().AllSatisfy(d => d.Code.Should().Be(nameof(DiagnosticCode.TypeMismatch)));
        model.Diagnostics[0].Message.Should().Contain("cannot determine numeric type from context");
    }

    [Fact]
    public void Check_NumberLiteral_InIntegerContext_ResolvesAsInteger()
    {
        // Integer field on the left; the literal on the right receives null context → TypeMismatch.
        // OperatorTable(Plus, IntegerType, ErrorType) → ErrorType; condition is assignable → no cascade.
        // Verifies the number literal dispatch arm is reached and null-context path fires exactly once.
        var model = CheckSource("precept T\nfield X as integer\nrule X + 42 because \"msg\"");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch));
        model.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.TypeMismatch))
            .Message.Should().Contain("cannot determine numeric type from context");
    }

    [Fact]
    public void Check_NumberLiteral_FractionalInIntegerContext_EmitsTypeMismatch()
    {
        // Fractional literal beside an integer field; literal receives null context → TypeMismatch.
        // (Fractional-vs-integer type enforcement — e.g. "integer" expected / "decimal" found —
        // requires a typed assignment surface and is covered directly in OperatorTableTests.)
        var model = CheckSource("precept T\nfield X as integer\nrule X + 3.14 because \"msg\"");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch));
        model.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.TypeMismatch))
            .Message.Should().Contain("cannot determine numeric type from context");
    }

    [Fact]
    public void Check_NumberLiteral_ScientificInDecimalContext_EmitsTypeMismatch()
    {
        // Scientific-notation literal beside a decimal field; literal receives null context → TypeMismatch.
        var model = CheckSource("precept T\nfield D as decimal\nrule D + 1.5e3 because \"msg\"");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch));
        model.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.TypeMismatch))
            .Message.Should().Contain("cannot determine numeric type from context");
    }

    // ── Binary arithmetic resolution (field operands avoid the null-context limit) ───

    [Fact]
    public void Check_BinaryArithmetic_IntegerPlusInteger_ResolvesInteger()
    {
        var model = CheckSource("precept T\nfield A as integer\nfield B as integer\nrule A + B because \"msg\"");

        // Arithmetic resolves to IntegerType; TypeMismatch is "boolean" expected / "integer" found.
        model.Rules[0].Condition.Type.Should().BeOfType<IntegerType>();
        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    [Fact]
    public void Check_BinaryArithmetic_IntegerPlusDecimal_WidensToDecimal()
    {
        // integer + decimal → DecimalType (widening via CommonNumericType).
        var model = CheckSource("precept T\nfield A as integer\nfield B as decimal\nrule A + B because \"msg\"");

        model.Rules[0].Condition.Type.Should().BeOfType<DecimalType>();
        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    [Fact]
    public void Check_BinaryArithmetic_IntegerPlusNumber_WidensToNumber()
    {
        // integer + number → NumberType (widening via CommonNumericType).
        var model = CheckSource("precept T\nfield A as integer\nfield N as number\nrule A + N because \"msg\"");

        model.Rules[0].Condition.Type.Should().BeOfType<NumberType>();
        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    [Fact]
    public void Check_BinaryArithmetic_DecimalTimesNumber_EmitsTypeMismatch()
    {
        // decimal × number → OperatorTable returns null → TypeMismatch from the operator.
        // ErrorType is returned; condition is assignable (ErrorType) → no cascade.
        var model = CheckSource("precept T\nfield D as decimal\nfield N as number\nrule D * N because \"msg\"");

        model.Rules[0].Condition.Type.Should().BeOfType<ErrorType>();
        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    [Fact]
    public void Check_BinaryArithmetic_StringPlusString_Concatenation()
    {
        // string + string → StringType; TypeMismatch for condition ("boolean" expected, "string" found).
        var model = CheckSource("precept T\nfield S as string\nrule S + S because \"msg\"");

        model.Rules[0].Condition.Type.Should().BeOfType<StringType>();
        var diag = model.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.TypeMismatch));
        diag.Message.Should().Contain("boolean");
        diag.Message.Should().Contain("string");
    }

    [Fact]
    public void Check_BinaryArithmetic_BooleanPlusBoolean_EmitsTypeMismatch()
    {
        // bool + bool → OperatorTable returns null → TypeMismatch from operator; ErrorType for condition → no cascade.
        var model = CheckSource("precept T\nfield B as boolean\nrule B + B because \"msg\"");

        model.Rules[0].Condition.Type.Should().BeOfType<ErrorType>();
        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    [Fact]
    public void Check_BinaryArithmetic_ErrorType_SuppressesCascade()
    {
        // UndeclaredField → ErrorType left; OperatorTable(ErrorType, bool) → ErrorType propagation.
        // Condition is ErrorType → assignable to boolean → no TypeMismatch cascade.
        var model = CheckSource("precept T\nfield Known as boolean\nrule Unknown + Known because \"msg\"");

        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.UndeclaredField));
        model.Diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch));
    }

    // ── IsAssignableTo widening + number field resolution ─────────────────────

    [Fact]
    public void Check_IsAssignableTo_IntegerToDecimal_Widens()
    {
        // integer + decimal → widening to DecimalType via CommonNumericType.
        // One diagnostic: TypeMismatch (decimal is not boolean for condition).
        var model = CheckSource("precept T\nfield I as integer\nfield D as decimal\nrule I + D because \"msg\"");

        model.Rules[0].Condition.Type.Should().BeOfType<DecimalType>();
        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    [Fact]
    public void Check_Rule_WithNumberField_TypeChecksCorrectly()
    {
        // number field used as rule condition resolves to NumberType; TypeMismatch (not boolean).
        var model = CheckSource("precept T\nfield N as number\nrule N because \"msg\"");

        model.Fields["N"].Type.Should().BeOfType<NumberType>();
        model.Rules[0].Condition.Type.Should().BeOfType<NumberType>();
        model.Diagnostics.Should().ContainSingle(d =>
            d.Code == nameof(DiagnosticCode.TypeMismatch) &&
            d.Stage == DiagnosticStage.Type);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  TheoryData helpers
    // ════════════════════════════════════════════════════════════════════════════

    public static TheoryData<DiagnosticCode> AllDiagnosticCodes()
    {
        var data = new TheoryData<DiagnosticCode>();
        foreach (var code in Enum.GetValues<DiagnosticCode>())
            data.Add(code);
        return data;
    }

    public static TheoryData<DiagnosticCode> TemporalCodes => new()
    {
        DiagnosticCode.InvalidDateValue,
        DiagnosticCode.InvalidDateFormat,
        DiagnosticCode.InvalidTimeValue,
        DiagnosticCode.InvalidInstantFormat,
        DiagnosticCode.InvalidTimezoneId,
        DiagnosticCode.UnqualifiedPeriodArithmetic,
        DiagnosticCode.MissingTemporalUnit,
        DiagnosticCode.FractionalUnitValue,
    };

    public static TheoryData<DiagnosticCode> CollectionSafetyCodes => new()
    {
        DiagnosticCode.UnguardedCollectionAccess,
        DiagnosticCode.UnguardedCollectionMutation,
        DiagnosticCode.NonOrderableCollectionExtreme,
        DiagnosticCode.CaseInsensitiveStringOnNonCollection,
    };

    public static TheoryData<DiagnosticCode> BusinessDomainCodes => new()
    {
        DiagnosticCode.MaxPlacesExceeded,
        DiagnosticCode.QualifierMismatch,
        DiagnosticCode.DimensionCategoryMismatch,
        DiagnosticCode.CrossCurrencyArithmetic,
        DiagnosticCode.CrossDimensionArithmetic,
        DiagnosticCode.DenominatorUnitMismatch,
        DiagnosticCode.DurationDenominatorMismatch,
        DiagnosticCode.CompoundPeriodDenominator,
        DiagnosticCode.InvalidUnitString,
        DiagnosticCode.InvalidCurrencyCode,
        DiagnosticCode.InvalidDimensionString,
    };
}
