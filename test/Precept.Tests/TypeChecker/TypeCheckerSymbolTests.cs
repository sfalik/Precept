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

    [Fact]
    public void TypedField_Presence_GuaranteedWhenNotOptional()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Name")
            .Presence.Should().BeOfType<DeclaredPresenceMeta.Guaranteed>();
    }

    [Fact]
    public void TypedField_Presence_OptionalWhenOptional()
    {
        var precept = """
            precept Widget
            field Notes as string optional
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Notes")
            .Presence.Should().BeOfType<DeclaredPresenceMeta.Optional>();
    }

    [Fact]
    public void TypedField_DeclaredQualifiers_EmptyByDefault()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Fields.Single(f => f.Name == "Name")
            .DeclaredQualifiers.Should().BeEmpty();
    }

    [Fact]
    public void ArgRef_CarriesQualifiers_WhenDeclared()
    {
        var precept = """
            precept Widget
            field Weight as quantity of 'mass' default '1 kg'
            event Measure(a as quantity of 'mass')
            on Measure -> set Weight = a
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var action = index.EventHandlers.OfType<TypedEventRowSuccess>().Single().Actions.Single().Should().BeOfType<TypedInputAction>().Which;
        var argRef = action.InputExpression.Should().BeOfType<TypedArgRef>().Which;

        argRef.DeclaredQualifiers.Should().NotBeNull();
        var qualifier = argRef.DeclaredQualifiers!.Value.Should().ContainSingle().Which;
        qualifier.Should().BeOfType<DeclaredQualifierMeta.Dimension>().Which.DimensionName.Should().Be("mass");
    }

    [Fact]
    public void FieldRef_CarriesQualifiers_WhenDeclared()
    {
        var precept = """
            precept Widget
            field Weight as quantity in 'kg' default '1 kg'
            field Copy as quantity in 'kg' <- Weight
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var fieldRef = index.FieldsByName["Copy"].ComputedExpression.Should().BeOfType<TypedFieldRef>().Which;

        fieldRef.DeclaredQualifiers.Should().NotBeNull();
        var qualifier = fieldRef.DeclaredQualifiers!.Value.Should().ContainSingle().Which;
        var unit = qualifier.Should().BeOfType<DeclaredQualifierMeta.Unit>().Which;
        unit.UnitCode.Should().Be("kg");
        unit.DimensionName.Should().Be("mass");
    }

    [Fact]
    public void TypedField_CarriesInterpolatedUnitQualifier_WhenDeclared()
    {
        var precept = """
            precept Widget
            field StockingUnit as unitofmeasure default 'each'
            field PurchaseUnit as unitofmeasure default 'each'
            field Ratio as quantity in '{StockingUnit}/{PurchaseUnit}'
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var qualifier = index.Fields.Single(f => f.Name == "Ratio")
            .DeclaredQualifiers.Should().ContainSingle().Which;
        var unit = qualifier.Should().BeOfType<DeclaredQualifierMeta.Unit>().Which;
        unit.UnitCode.Should().Be("{StockingUnit}/{PurchaseUnit}");
        unit.DimensionName.Should().Be("{StockingUnit}/{PurchaseUnit}");
    }

    [Fact]
    public void TypedField_CarriesInterpolatedDimensionQualifier_WhenDeclared()
    {
        var precept = """
            precept Widget
            field StockingUnit as unitofmeasure default 'each'
            field QuantityOnHand as quantity of '{StockingUnit.dimension}'
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var qualifier = index.Fields.Single(f => f.Name == "QuantityOnHand")
            .DeclaredQualifiers.Should().ContainSingle().Which;
        qualifier.Should().BeOfType<DeclaredQualifierMeta.Dimension>()
            .Which.DimensionName.Should().Be("{StockingUnit.dimension}");
    }

    [Fact]
    public void TypedArg_CarriesInterpolatedDimensionQualifier_WhenDeclared()
    {
        var precept = """
            precept Widget
            field StockingUnit as unitofmeasure default 'each'
            state Open initial
            event Receive(qty as quantity of '{StockingUnit.dimension}')
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var qualifier = index.Events.Single(e => e.Name == "Receive").Args.Single()
            .DeclaredQualifiers.Should().ContainSingle().Which;
        qualifier.Should().BeOfType<DeclaredQualifierMeta.Dimension>()
            .Which.DimensionName.Should().Be("{StockingUnit.dimension}");
    }

    [Fact]
    public void ArgRef_NullQualifiers_WhenUnqualified()
    {
        var precept = """
            precept Widget
            field Count as integer default 0
            event SetCount(n as integer)
            on SetCount -> set Count = n
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var action = index.EventHandlers.OfType<TypedEventRowSuccess>().Single().Actions.Single().Should().BeOfType<TypedInputAction>().Which;
        var argRef = action.InputExpression.Should().BeOfType<TypedArgRef>().Which;

        (argRef.DeclaredQualifiers is null || argRef.DeclaredQualifiers.Value.IsEmpty)
            .Should().BeTrue();
    }

    [Fact]
    public void QuantityInRad_DoesNotDeriveCountQualifierCategory()
    {
        var qualifier = GetUnitQualifier("Angle", "quantity in 'rad'");

        qualifier.UnitCode.Should().Be("rad");
        qualifier.DimensionName.Should().BeEmpty();
    }

    [Fact]
    public void QuantityInDeg_DoesNotDeriveCountQualifierCategory()
    {
        var qualifier = GetUnitQualifier("AngleDegrees", "quantity in 'deg'");

        qualifier.UnitCode.Should().Be("deg");
        qualifier.DimensionName.Should().BeEmpty();
    }

    [Fact]
    public void QuantityInSteradian_DoesNotDeriveCountQualifierCategory()
    {
        var qualifier = GetUnitQualifier("SolidAngle", "quantity in 'sr'");

        qualifier.UnitCode.Should().Be("sr");
        qualifier.DimensionName.Should().BeEmpty();
    }

    [Fact]
    public void QuantityInUnity_DerivesCountQualifierCategory()
    {
        var qualifier = GetUnitQualifier("Ratio", "quantity in '1'");

        qualifier.UnitCode.Should().Be("1");
        qualifier.DimensionName.Should().Be("count");
    }

    [Fact]
    public void QuantityInPercent_DerivesCountQualifierCategory()
    {
        var qualifier = GetUnitQualifier("Percent", "quantity in '%'");

        qualifier.UnitCode.Should().Be("%");
        qualifier.DimensionName.Should().Be("count");
    }

    [Fact]
    public void QuantityInKilogram_DerivesMassQualifierCategory()
    {
        var qualifier = GetUnitQualifier("Weight", "quantity in 'kg'");

        qualifier.UnitCode.Should().Be("kg");
        qualifier.DimensionName.Should().Be("mass");
    }

    private static DeclaredQualifierMeta.Unit GetUnitQualifier(string fieldName, string typeReference)
    {
        var precept = $"""
            precept Widget
            field {fieldName} as {typeReference}
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var qualifier = index.Fields.Single(f => f.Name == fieldName)
            .DeclaredQualifiers.Should().ContainSingle().Which;

        return qualifier.Should().BeOfType<DeclaredQualifierMeta.Unit>().Which;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  BUG-057 regression: period of/in qualifier propagation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PeriodOfDate_QualifierPreservedInSemanticIndex()
    {
        // BUG-057: 'period of 'date'' qualifier was silently dropped by the type checker.
        var precept = """
            precept Widget
            field Offset as period of 'date'
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var qualifier = index.Fields.Single(f => f.Name == "Offset")
            .DeclaredQualifiers.Should().ContainSingle(because: "period of 'date' qualifier must not be dropped")
            .Which;

        var td = qualifier.Should().BeOfType<DeclaredQualifierMeta.TemporalDimension>().Which;
        td.Value.Should().Be(PeriodDimension.Date);
    }

    [Fact]
    public void PeriodOfTime_QualifierPreservedInSemanticIndex()
    {
        // BUG-057: 'period of 'time'' qualifier must also propagate correctly.
        var precept = """
            precept Widget
            field Delay as period of 'time'
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var qualifier = index.Fields.Single(f => f.Name == "Delay")
            .DeclaredQualifiers.Should().ContainSingle(because: "period of 'time' qualifier must not be dropped")
            .Which;

        var td = qualifier.Should().BeOfType<DeclaredQualifierMeta.TemporalDimension>().Which;
        td.Value.Should().Be(PeriodDimension.Time);
    }

    [Fact]
    public void PeriodInDays_QualifierPreservedInSemanticIndex()
    {
        // BUG-057: 'period in 'days'' qualifier was silently dropped by the type checker.
        var precept = """
            precept Widget
            field Grace as period in 'days'
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var qualifier = index.Fields.Single(f => f.Name == "Grace")
            .DeclaredQualifiers.Should().ContainSingle(because: "period in 'days' qualifier must not be dropped")
            .Which;

        var tu = qualifier.Should().BeOfType<DeclaredQualifierMeta.TemporalUnit>().Which;
        tu.UnitName.Should().Be("days");
        tu.DerivedDimension.Should().Be(PeriodDimension.Date, because: "days is a calendar-based unit");
    }

    [Fact]
    public void PeriodInHours_QualifierPreservedInSemanticIndex()
    {
        // BUG-057: time-level unit must derive PeriodDimension.Time.
        var precept = """
            precept Widget
            field Window as period in 'hours'
            state Open initial
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var qualifier = index.Fields.Single(f => f.Name == "Window")
            .DeclaredQualifiers.Should().ContainSingle(because: "period in 'hours' qualifier must not be dropped")
            .Which;

        var tu = qualifier.Should().BeOfType<DeclaredQualifierMeta.TemporalUnit>().Which;
        tu.UnitName.Should().Be("hours");
        tu.DerivedDimension.Should().Be(PeriodDimension.Time, because: "hours is a time-level unit");
    }

    [Fact]
    public void PeriodOfDate_AllowsDatePlusPeriodOperation_NoDiagnostic()
    {
        // BUG-057: date + period_of_date_field produced PRE0113 because qualifier was dropped.
        var precept = """
            precept Widget
            field StartDate as date
            field Offset as period of 'date'
            field EndDate as date <- StartDate + Offset
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PeriodOfInvalidString_EmitsInvalidTemporalDimensionStringDiagnostic()
    {
        var precept = """
            precept Widget
            field Offset as period of 'week'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidTemporalDimensionString);
    }

    [Fact]
    public void PeriodInInvalidUnit_EmitsInvalidTemporalUnitStringDiagnostic()
    {
        var precept = """
            precept Widget
            field Offset as period in 'fortnights'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidTemporalUnitString);
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
    public void TypedArg_Presence_MatchesIsOptional()
    {
        var precept = """
            precept Widget
            field Name as string optional
            state Open initial
            event Submit(Label as string, Note as string optional)
            from Open on Submit -> set Name = Submit.Note -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var args = index.Events.Single(e => e.Name == "Submit").Args;

        args[0].IsOptional.Should().BeFalse();
        args[0].Presence.Should().BeOfType<DeclaredPresenceMeta.Guaranteed>();
        args[1].IsOptional.Should().BeTrue();
        args[1].Presence.Should().BeOfType<DeclaredPresenceMeta.Optional>();
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
