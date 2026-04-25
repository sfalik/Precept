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
