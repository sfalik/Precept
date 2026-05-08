using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 1 — Typed Symbol Population.
/// Covers TypeKind resolution, collection ElementType (D2), optional modifier,
/// modifier preservation, ImpliedModifiers (D3), state modifier flags,
/// event arg types, and initial/terminal diagnostic cases (D7).
/// </summary>
public class TypeCheckerSymbolTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  1. TypeKind resolution — scalar and temporal types
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("string",         TypeKind.String)]
    [InlineData("boolean",        TypeKind.Boolean)]
    [InlineData("integer",        TypeKind.Integer)]
    [InlineData("decimal",        TypeKind.Decimal)]
    [InlineData("number",         TypeKind.Number)]
    [InlineData("date",           TypeKind.Date)]
    [InlineData("time",           TypeKind.Time)]
    [InlineData("instant",        TypeKind.Instant)]
    [InlineData("duration",       TypeKind.Duration)]
    [InlineData("period",         TypeKind.Period)]
    [InlineData("timezone",       TypeKind.Timezone)]
    [InlineData("datetime",       TypeKind.DateTime)]
    [InlineData("zoneddatetime",  TypeKind.ZonedDateTime)]
    [InlineData("currency",       TypeKind.Currency)]
    [InlineData("unitofmeasure",  TypeKind.UnitOfMeasure)]
    [InlineData("dimension",      TypeKind.Dimension)]
    public void ScalarType_ResolvesToCorrectTypeKind(string dslKeyword, TypeKind expected)
    {
        var precept = $"""
            precept Widget
            field MyField as {dslKeyword}
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "MyField")
            .ResolvedType.Should().Be(expected);
    }

    [Fact]
    public void MoneyType_ResolvesToMoneyTypeKind()
    {
        var precept = """
            precept Widget
            field Cost as money
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Cost")
            .ResolvedType.Should().Be(TypeKind.Money);
    }

    [Fact]
    public void QuantityType_ResolvesToQuantityTypeKind()
    {
        var precept = """
            precept Widget
            field Weight as quantity
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Weight")
            .ResolvedType.Should().Be(TypeKind.Quantity);
    }

    [Fact]
    public void PriceType_ResolvesToPriceTypeKind()
    {
        var precept = """
            precept Widget
            field UnitPrice as price
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "UnitPrice")
            .ResolvedType.Should().Be(TypeKind.Price);
    }

    [Fact]
    public void ExchangeRateType_ResolvesToExchangeRateTypeKind()
    {
        var precept = """
            precept Widget
            field FxRate as exchangerate
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "FxRate")
            .ResolvedType.Should().Be(TypeKind.ExchangeRate);
    }

    [Fact]
    public void ChoiceType_ResolvesToChoiceTypeKind()
    {
        var precept = """
            precept Widget
            field Priority as choice of string("Low","Medium","High")
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Priority")
            .ResolvedType.Should().Be(TypeKind.Choice);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  2. Collection type resolution (D2 — ElementType)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetOfString_ResolvesCollectionWithElementType()
    {
        var precept = """
            precept Widget
            field Items as set of string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "Items");

        field.ResolvedType.Should().Be(TypeKind.Set);
        field.ElementType.Should().Be(TypeKind.String);
    }

    [Fact]
    public void QueueOfNumber_ResolvesCollectionWithElementType()
    {
        var precept = """
            precept Widget
            field Pending as queue of number
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "Pending");

        field.ResolvedType.Should().Be(TypeKind.Queue);
        field.ElementType.Should().Be(TypeKind.Number);
    }

    [Fact]
    public void StackOfBoolean_ResolvesCollectionWithElementType()
    {
        var precept = """
            precept Widget
            field Steps as stack of boolean
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "Steps");

        field.ResolvedType.Should().Be(TypeKind.Stack);
        field.ElementType.Should().Be(TypeKind.Boolean);
    }

    [Fact]
    public void LogOfString_ResolvesCollectionWithElementType()
    {
        var precept = """
            precept Widget
            field AuditTrail as log of string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "AuditTrail");

        field.ResolvedType.Should().Be(TypeKind.Log);
        field.ElementType.Should().Be(TypeKind.String);
    }

    [Fact]
    public void ListOfInteger_ResolvesCollectionWithElementType()
    {
        var precept = """
            precept Widget
            field Approvers as list of integer
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "Approvers");

        field.ResolvedType.Should().Be(TypeKind.List);
        field.ElementType.Should().Be(TypeKind.Integer);
    }

    [Fact]
    public void BagOfString_ResolvesCollectionWithElementType()
    {
        var precept = """
            precept Widget
            field CartItems as bag of string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "CartItems");

        field.ResolvedType.Should().Be(TypeKind.Bag);
        field.ElementType.Should().Be(TypeKind.String);
    }

    [Fact]
    public void ScalarField_HasNullElementType()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Name")
            .ElementType.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  3. Optional modifier
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OptionalField_IsOptionalTrue()
    {
        var precept = """
            precept Widget
            field Notes as string optional
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Notes")
            .IsOptional.Should().BeTrue();
    }

    [Fact]
    public void RequiredField_IsOptionalFalse()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Name")
            .IsOptional.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  4. Modifier preservation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void WritableModifier_PreservedInTypedField()
    {
        var precept = """
            precept Widget
            field Name as string writable
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var field = index.Fields.Single(f => f.Name == "Name");

        field.Modifiers.Should().Contain(ModifierKind.Writable);
        field.IsWritable.Should().BeTrue();
    }

    [Fact]
    public void NonnegativeModifier_PreservedInTypedField()
    {
        var precept = """
            precept Widget
            field Count as number nonnegative
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Count")
            .Modifiers.Should().Contain(ModifierKind.Nonnegative);
    }

    [Fact]
    public void NotemptyModifier_PreservedInTypedField()
    {
        var precept = """
            precept Widget
            field Label as string notempty
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Label")
            .Modifiers.Should().Contain(ModifierKind.Notempty);
    }

    [Fact]
    public void FieldWithoutWritable_IsWritableFalse()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Name")
            .IsWritable.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  5. ImpliedModifiers (D3 — catalog-driven)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TimezoneField_CarriesImpliedNotempty()
    {
        var precept = """
            precept Widget
            field Tz as timezone
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Tz")
            .ImpliedModifiers.Should().Contain(ModifierKind.Notempty);
    }

    [Fact]
    public void CurrencyField_CarriesImpliedNotempty()
    {
        var precept = """
            precept Widget
            field Curr as currency
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Curr")
            .ImpliedModifiers.Should().Contain(ModifierKind.Notempty);
    }

    [Fact]
    public void UnitOfMeasureField_CarriesImpliedNotempty()
    {
        var precept = """
            precept Widget
            field Uom as unitofmeasure
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Uom")
            .ImpliedModifiers.Should().Contain(ModifierKind.Notempty);
    }

    [Fact]
    public void DimensionField_CarriesImpliedNotempty()
    {
        var precept = """
            precept Widget
            field Dim as dimension
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Dim")
            .ImpliedModifiers.Should().Contain(ModifierKind.Notempty);
    }

    [Fact]
    public void StringField_HasNoImpliedModifiers()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Name")
            .ImpliedModifiers.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  6. State modifier preservation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void InitialState_HasInitialStateModifier()
    {
        var precept = """
            precept Widget
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.States.Single(s => s.Name == "Open")
            .Modifiers.Should().Contain(ModifierKind.InitialState);
    }

    [Fact]
    public void TerminalState_HasTerminalModifier()
    {
        var precept = """
            precept Widget
            state Open initial
            state Done terminal
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.States.Single(s => s.Name == "Done")
            .Modifiers.Should().Contain(ModifierKind.Terminal);
    }

    [Fact]
    public void ErrorState_HasErrorModifier()
    {
        var precept = """
            precept Widget
            state Open initial
            state Failed error
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.States.Single(s => s.Name == "Failed")
            .Modifiers.Should().Contain(ModifierKind.Error);
    }

    [Fact]
    public void NonInitialState_DoesNotHaveInitialModifier()
    {
        var precept = """
            precept Widget
            state Open initial
            state Processing
            state Done terminal
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.States.Single(s => s.Name == "Processing")
            .Modifiers.Should().NotContain(ModifierKind.InitialState);
    }

    [Fact]
    public void MultipleStates_AllPopulated()
    {
        var precept = """
            precept Widget
            state Open initial
            state Processing
            state Done terminal
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.States.Should().HaveCount(3);
        index.StatesByName.Should().ContainKey("Open");
        index.StatesByName.Should().ContainKey("Processing");
        index.StatesByName.Should().ContainKey("Done");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  7. Event args
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventWithTypedArgs_ResolvesArgTypes()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Submit(Label as string, Amount as decimal)
            from Open on Submit -> set Name = Submit.Label -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var evt = index.Events.Single(e => e.Name == "Submit");

        evt.Args.Should().HaveCount(2);
        evt.Args[0].Name.Should().Be("Label");
        evt.Args[0].ResolvedType.Should().Be(TypeKind.String);
        evt.Args[1].Name.Should().Be("Amount");
        evt.Args[1].ResolvedType.Should().Be(TypeKind.Decimal);
    }

    [Fact]
    public void EventWithOptionalArg_ArgIsOptional()
    {
        var precept = """
            precept Widget
            field Reason as string optional
            state Open initial
            event Cancel(Reason as string optional)
            from Open on Cancel -> set Reason = Cancel.Reason -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var arg = index.Events.Single(e => e.Name == "Cancel").Args.Single();

        arg.IsOptional.Should().BeTrue();
        arg.ResolvedType.Should().Be(TypeKind.String);
    }

    [Fact]
    public void EventWithNoArgs_ArgsEmpty()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Advance
            from Open on Advance -> set Count = Count + 1 -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Events.Single(e => e.Name == "Advance")
            .Args.Should().BeEmpty();
    }

    [Fact]
    public void EventArgWithNotempty_ModifierPreserved()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Submit(Label as string notempty)
            from Open on Submit -> set Name = Submit.Label -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Events.Single(e => e.Name == "Submit").Args.Single()
            .Modifiers.Should().Contain(ModifierKind.Notempty);
    }

    [Fact]
    public void MultipleEvents_AllPopulated()
    {
        var precept = """
            precept Widget
            field Status as string default "none"
            state Open initial
            event Start
            event Stop
            event Pause
            from Open on Start -> set Status = "started" -> no transition
            from Open on Stop -> set Status = "stopped" -> no transition
            from Open on Pause -> set Status = "paused" -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Events.Should().HaveCount(3);
        index.EventsByName.Should().ContainKey("Start");
        index.EventsByName.Should().ContainKey("Stop");
        index.EventsByName.Should().ContainKey("Pause");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  8. Initial/terminal diagnostic cases (D7)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ZeroInitialStates_EmitsNoInitialStateDiagnostic()
    {
        var precept = """
            precept Widget
            state Open
            state Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.NoInitialState);
    }

    [Fact]
    public void TwoInitialStates_EmitsMultipleInitialStatesDiagnostic()
    {
        var precept = """
            precept Widget
            state Open initial
            state Ready initial
            state Done terminal
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.MultipleInitialStates);
    }

    [Fact]
    public void SingleInitialState_NoInitialDiagnostic()
    {
        var precept = """
            precept Widget
            state Open initial
            state Processing
            state Done terminal
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().NotContain(DiagnosticCode.NoInitialState.ToString())
            .And.NotContain(DiagnosticCode.MultipleInitialStates.ToString());
    }

    [Fact]
    public void ZeroTerminalStates_NoDiagnostic()
    {
        var precept = """
            precept Widget
            state Open initial
            state Processing
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        // Open lifecycle — no terminal states is valid
        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  9. Field population — lookup indexes
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MultipleFields_AllPopulatedWithLookupIndex()
    {
        var precept = """
            precept Widget
            field Name as string
            field Count as number default 0
            field Active as boolean default true
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Should().HaveCount(3);
        index.FieldsByName.Should().ContainKey("Name");
        index.FieldsByName.Should().ContainKey("Count");
        index.FieldsByName.Should().ContainKey("Active");
    }

    [Fact]
    public void FieldsByName_ReturnsSameInstanceAsFieldsArray()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var fromArray = index.Fields.Single(f => f.Name == "Name");
        var fromDict = index.FieldsByName["Name"];

        fromDict.Should().BeSameAs(fromArray);
    }
}
