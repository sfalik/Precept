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
