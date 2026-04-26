using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class TypesTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllTypeKinds))]
    public void GetMeta_ReturnsWithoutThrowing_ForEveryTypeKind(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        meta.Should().NotBeNull();
    }

    [Fact]
    public void All_ContainsExactlyAsManyEntries_AsEnumValues()
    {
        var expected = Enum.GetValues<TypeKind>().Length;
        Types.All.Should().HaveCount(expected);
    }

    [Theory]
    [MemberData(nameof(AllTypeKinds))]
    public void GetMeta_EveryEntry_HasCorrectKindAndNonEmptyDescription(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        meta.Kind.Should().Be(kind);
        meta.Description.Should().NotBeNullOrWhiteSpace();
    }

    // ── Token references ────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(SurfaceTypeKinds))]
    public void GetMeta_SurfaceTypes_HaveNonNullTokenWithText(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        meta.Token.Should().NotBeNull($"{kind} is a surface type and should have a Token");
        meta.Token!.Text.Should().NotBeNull($"{kind} token should have keyword text");
    }

    [Theory]
    [InlineData(TypeKind.Error)]
    [InlineData(TypeKind.StateRef)]
    public void GetMeta_InternalTypes_HaveNullToken(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        meta.Token.Should().BeNull($"{kind} is internal and has no surface keyword");
    }

    // ── Category correctness ────────────────────────────────────────────────────

    [Theory]
    [InlineData(TypeKind.String, TypeCategory.Scalar)]
    [InlineData(TypeKind.Boolean, TypeCategory.Scalar)]
    [InlineData(TypeKind.Integer, TypeCategory.Scalar)]
    [InlineData(TypeKind.Decimal, TypeCategory.Scalar)]
    [InlineData(TypeKind.Number, TypeCategory.Scalar)]
    [InlineData(TypeKind.Choice, TypeCategory.Scalar)]
    [InlineData(TypeKind.Date, TypeCategory.Temporal)]
    [InlineData(TypeKind.Time, TypeCategory.Temporal)]
    [InlineData(TypeKind.Instant, TypeCategory.Temporal)]
    [InlineData(TypeKind.Duration, TypeCategory.Temporal)]
    [InlineData(TypeKind.Period, TypeCategory.Temporal)]
    [InlineData(TypeKind.Timezone, TypeCategory.Temporal)]
    [InlineData(TypeKind.ZonedDateTime, TypeCategory.Temporal)]
    [InlineData(TypeKind.DateTime, TypeCategory.Temporal)]
    [InlineData(TypeKind.Money, TypeCategory.BusinessDomain)]
    [InlineData(TypeKind.Currency, TypeCategory.BusinessDomain)]
    [InlineData(TypeKind.Quantity, TypeCategory.BusinessDomain)]
    [InlineData(TypeKind.UnitOfMeasure, TypeCategory.BusinessDomain)]
    [InlineData(TypeKind.Dimension, TypeCategory.BusinessDomain)]
    [InlineData(TypeKind.Price, TypeCategory.BusinessDomain)]
    [InlineData(TypeKind.ExchangeRate, TypeCategory.BusinessDomain)]
    [InlineData(TypeKind.Set, TypeCategory.Collection)]
    [InlineData(TypeKind.Queue, TypeCategory.Collection)]
    [InlineData(TypeKind.Stack, TypeCategory.Collection)]
    [InlineData(TypeKind.Error, TypeCategory.Special)]
    [InlineData(TypeKind.StateRef, TypeCategory.Special)]
    public void GetMeta_HasCorrectCategory(TypeKind kind, TypeCategory expectedCategory)
    {
        Types.GetMeta(kind).Category.Should().Be(expectedCategory);
    }

    // ── Trait correctness ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(TypeKind.Integer)]
    [InlineData(TypeKind.Decimal)]
    [InlineData(TypeKind.Number)]
    [InlineData(TypeKind.Date)]
    [InlineData(TypeKind.Time)]
    [InlineData(TypeKind.Instant)]
    [InlineData(TypeKind.Duration)]
    [InlineData(TypeKind.DateTime)]
    [InlineData(TypeKind.Money)]
    [InlineData(TypeKind.Quantity)]
    [InlineData(TypeKind.Price)]
    public void GetMeta_OrderableTypes_HaveOrderableTrait(TypeKind kind)
    {
        Types.GetMeta(kind).Traits.Should().HaveFlag(TypeTrait.Orderable);
    }

    [Theory]
    [InlineData(TypeKind.String)]
    [InlineData(TypeKind.Boolean)]
    [InlineData(TypeKind.Choice)]
    [InlineData(TypeKind.Period)]
    [InlineData(TypeKind.Timezone)]
    [InlineData(TypeKind.Currency)]
    [InlineData(TypeKind.UnitOfMeasure)]
    [InlineData(TypeKind.ZonedDateTime)]
    [InlineData(TypeKind.ExchangeRate)]
    [InlineData(TypeKind.Dimension)]
    [InlineData(TypeKind.Set)]
    [InlineData(TypeKind.Queue)]
    [InlineData(TypeKind.Stack)]
    [InlineData(TypeKind.Error)]
    [InlineData(TypeKind.StateRef)]
    public void GetMeta_NonOrderableTypes_DoNotHaveOrderableTrait(TypeKind kind)
    {
        Types.GetMeta(kind).Traits.Should().NotHaveFlag(TypeTrait.Orderable);
    }

    // ── WidensTo ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_Integer_WidensToDecimalAndNumber()
    {
        Types.GetMeta(TypeKind.Integer).WidensTo
            .Should().BeEquivalentTo([TypeKind.Decimal, TypeKind.Number]);
    }

    [Theory]
    [MemberData(nameof(NonIntegerTypeKinds))]
    public void GetMeta_NonIntegerTypes_HaveEmptyWidensTo(TypeKind kind)
    {
        Types.GetMeta(kind).WidensTo.Should().BeEmpty(
            $"{kind} should not widen to anything");
    }

    // ── QualifierShape ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_Money_HasCurrencyQualifier()
    {
        var meta = Types.GetMeta(TypeKind.Money);
        meta.QualifierShape.Should().NotBeNull();
        meta.QualifierShape!.Slots.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new QualifierSlot(TokenKind.In, QualifierAxis.Currency));
    }

    [Fact]
    public void GetMeta_Quantity_HasUnitQualifier()
    {
        var meta = Types.GetMeta(TypeKind.Quantity);
        meta.QualifierShape.Should().NotBeNull();
        meta.QualifierShape!.Slots.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new QualifierSlot(TokenKind.Of, QualifierAxis.Unit));
    }

    [Fact]
    public void GetMeta_Period_HasTemporalDimensionQualifier()
    {
        var meta = Types.GetMeta(TypeKind.Period);
        meta.QualifierShape.Should().NotBeNull();
        meta.QualifierShape!.Slots.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new QualifierSlot(TokenKind.Of, QualifierAxis.TemporalDimension));
    }

    [Fact]
    public void GetMeta_Price_HasCurrencyAndUnitQualifiers()
    {
        var meta = Types.GetMeta(TypeKind.Price);
        meta.QualifierShape.Should().NotBeNull();
        meta.QualifierShape!.Slots.Should().HaveCount(2);
        meta.QualifierShape.Slots[0].Should().Be(new QualifierSlot(TokenKind.In, QualifierAxis.Currency));
        meta.QualifierShape.Slots[1].Should().Be(new QualifierSlot(TokenKind.Of, QualifierAxis.Unit));
    }

    [Fact]
    public void GetMeta_ExchangeRate_HasFromAndToQualifiers()
    {
        var meta = Types.GetMeta(TypeKind.ExchangeRate);
        meta.QualifierShape.Should().NotBeNull();
        meta.QualifierShape!.Slots.Should().HaveCount(2);
        meta.QualifierShape.Slots[0].Should().Be(new QualifierSlot(TokenKind.In, QualifierAxis.FromCurrency));
        meta.QualifierShape.Slots[1].Should().Be(new QualifierSlot(TokenKind.To, QualifierAxis.ToCurrency));
    }

    [Theory]
    [MemberData(nameof(UnqualifiedTypeKinds))]
    public void GetMeta_UnqualifiedTypes_HaveNullQualifierShape(TypeKind kind)
    {
        Types.GetMeta(kind).QualifierShape.Should().BeNull(
            $"{kind} does not accept qualifiers");
    }

    // ── Accessors: Collections ──────────────────────────────────────────────────

    [Theory]
    [InlineData(TypeKind.Set)]
    [InlineData(TypeKind.Queue)]
    [InlineData(TypeKind.Stack)]
    public void GetMeta_CollectionTypes_HaveCountAccessor(TypeKind kind)
    {
        var accessors = Types.GetMeta(kind).Accessors;
        accessors.Should().ContainSingle(a => a.Name == "count")
            .Which.Should().BeOfType<FixedReturnAccessor>()
            .Which.Returns.Should().Be(TypeKind.Integer);
    }

    [Fact]
    public void GetMeta_Set_HasMinAndMaxAccessorsRequiringOrderable()
    {
        var accessors = Types.GetMeta(TypeKind.Set).Accessors;

        var min = accessors.Should().ContainSingle(a => a.Name == "min").Subject;
        min.RequiredTraits.Should().HaveFlag(TypeTrait.Orderable);
        min.Should().NotBeOfType<FixedReturnAccessor>("min returns inner type, not a fixed type");

        var max = accessors.Should().ContainSingle(a => a.Name == "max").Subject;
        max.RequiredTraits.Should().HaveFlag(TypeTrait.Orderable);
        max.Should().NotBeOfType<FixedReturnAccessor>("max returns inner type, not a fixed type");
    }

    [Theory]
    [InlineData(TypeKind.Queue)]
    [InlineData(TypeKind.Stack)]
    public void GetMeta_QueueAndStack_HavePeekAccessor(TypeKind kind)
    {
        var accessors = Types.GetMeta(kind).Accessors;
        var peek = accessors.Should().ContainSingle(a => a.Name == "peek").Subject;
        peek.Should().NotBeOfType<FixedReturnAccessor>("peek returns inner type, not a fixed type");
    }

    // ── Accessors: Business-Domain ──────────────────────────────────────────────

    [Fact]
    public void GetMeta_Money_HasAmountAndCurrencyAccessors()
    {
        var accessors = Types.GetMeta(TypeKind.Money).Accessors;
        accessors.Should().HaveCount(2);

        var amount = accessors.Single(a => a.Name == "amount") as FixedReturnAccessor;
        amount.Should().NotBeNull();
        amount!.Returns.Should().Be(TypeKind.Decimal);

        var currency = accessors.Single(a => a.Name == "currency") as FixedReturnAccessor;
        currency.Should().NotBeNull();
        currency!.Returns.Should().Be(TypeKind.Currency);
        currency.ReturnsQualifier.Should().Be(QualifierAxis.Currency);
    }

    [Fact]
    public void GetMeta_ExchangeRate_HasAmountFromToAccessors()
    {
        var accessors = Types.GetMeta(TypeKind.ExchangeRate).Accessors;
        accessors.Should().HaveCount(3);

        (accessors.Single(a => a.Name == "amount") as FixedReturnAccessor)!
            .Returns.Should().Be(TypeKind.Decimal);
        (accessors.Single(a => a.Name == "from") as FixedReturnAccessor)!
            .ReturnsQualifier.Should().Be(QualifierAxis.FromCurrency);
        (accessors.Single(a => a.Name == "to") as FixedReturnAccessor)!
            .ReturnsQualifier.Should().Be(QualifierAxis.ToCurrency);
    }

    // ── Accessors: Temporal ─────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_Date_HasYearMonthDayDayOfWeekAccessors()
    {
        var accessors = Types.GetMeta(TypeKind.Date).Accessors;
        accessors.Should().HaveCount(4);
        accessors.Select(a => a.Name).Should().BeEquivalentTo(["year", "month", "day", "dayOfWeek"]);
        accessors.Should().AllSatisfy(a =>
        {
            var fra = a as FixedReturnAccessor;
            fra.Should().NotBeNull();
            fra!.Returns.Should().Be(TypeKind.Integer);
        });
    }

    [Fact]
    public void GetMeta_Instant_HasInZoneAccessorWithTimezoneParameter()
    {
        var accessors = Types.GetMeta(TypeKind.Instant).Accessors;
        var inZone = accessors.Should().ContainSingle(a => a.Name == "inZone").Subject as FixedReturnAccessor;
        inZone.Should().NotBeNull();
        inZone!.Returns.Should().Be(TypeKind.ZonedDateTime);
        inZone.ParameterType.Should().Be(TypeKind.Timezone);
    }

    [Fact]
    public void GetMeta_ZonedDateTime_HasCompositeAccessors()
    {
        var accessors = Types.GetMeta(TypeKind.ZonedDateTime).Accessors;
        accessors.Select(a => a.Name).Should().Contain("instant");
        accessors.Select(a => a.Name).Should().Contain("timezone");
        accessors.Select(a => a.Name).Should().Contain("datetime");
        accessors.Select(a => a.Name).Should().Contain("date");
        accessors.Select(a => a.Name).Should().Contain("time");
        // Also has date + time component accessors (year, month, day, dayOfWeek, hour, minute, second)
        accessors.Select(a => a.Name).Should().Contain("year");
        accessors.Select(a => a.Name).Should().Contain("hour");
    }

    // ── Accessors: String ───────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_String_HasLengthAccessor()
    {
        var accessors = Types.GetMeta(TypeKind.String).Accessors;
        var length = accessors.Should().ContainSingle(a => a.Name == "length").Subject as FixedReturnAccessor;
        length.Should().NotBeNull();
        length!.Returns.Should().Be(TypeKind.Integer);
    }

    // ── ByToken dictionary ──────────────────────────────────────────────────────

    [Fact]
    public void ByToken_ContainsAllSurfaceTypes()
    {
        foreach (var meta in Types.All.Where(m => m.Token is not null))
        {
            Types.ByToken.Should().ContainKey(meta.Token!.Kind,
                $"ByToken should map {meta.Token.Kind} → {meta.Kind}");
        }
    }

    [Fact]
    public void ByToken_ExcludesErrorAndStateRef()
    {
        // Error and StateRef have no Token — they shouldn't be in ByToken
        Types.ByToken.Values.Should().NotContain(m => m.Kind == TypeKind.Error);
        Types.ByToken.Values.Should().NotContain(m => m.Kind == TypeKind.StateRef);
    }

    [Fact]
    public void ByToken_SetTypeAlias_MapsToSameMetaAsSet()
    {
        Types.ByToken.Should().ContainKey(TokenKind.SetType);
        Types.ByToken[TokenKind.SetType].Should().BeSameAs(Types.ByToken[TokenKind.Set]);
    }

    [Fact]
    public void ByToken_ValueRoundtrips_ThroughGetMeta()
    {
        foreach (var (tokenKind, typeMeta) in Types.ByToken)
        {
            if (tokenKind == TokenKind.SetType) continue; // alias — skip roundtrip
            var fresh = Types.GetMeta(typeMeta.Kind);
            fresh.Kind.Should().Be(typeMeta.Kind,
                $"ByToken[{tokenKind}] should map to TypeKind.{typeMeta.Kind}");
            fresh.Token.Should().Be(typeMeta.Token,
                $"ByToken[{tokenKind}] should have the same Token");
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    public static TheoryData<TypeKind> AllTypeKinds()
    {
        var data = new TheoryData<TypeKind>();
        foreach (var kind in Enum.GetValues<TypeKind>())
            data.Add(kind);
        return data;
    }

    public static TheoryData<TypeKind> SurfaceTypeKinds()
    {
        var data = new TheoryData<TypeKind>();
        foreach (var kind in Enum.GetValues<TypeKind>())
        {
            if (kind is not (TypeKind.Error or TypeKind.StateRef))
                data.Add(kind);
        }
        return data;
    }

    public static TheoryData<TypeKind> NonIntegerTypeKinds()
    {
        var data = new TheoryData<TypeKind>();
        foreach (var kind in Enum.GetValues<TypeKind>())
        {
            if (kind != TypeKind.Integer)
                data.Add(kind);
        }
        return data;
    }

    public static TheoryData<TypeKind> UnqualifiedTypeKinds()
    {
        var qualified = new HashSet<TypeKind>
        {
            TypeKind.Money, TypeKind.Quantity, TypeKind.Period,
            TypeKind.Price, TypeKind.ExchangeRate,
        };

        var data = new TheoryData<TypeKind>();
        foreach (var kind in Enum.GetValues<TypeKind>())
        {
            if (!qualified.Contains(kind))
                data.Add(kind);
        }
        return data;
    }

    // ── ImpliedModifiers — default empty for most types ─────────────────────────

    [Fact]
    public void AllTypes_ImpliedModifiers_IsNotNull()
    {
        foreach (var meta in Types.All)
        {
            meta.ImpliedModifiers.Should().NotBeNull(
                $"{meta.Kind} ImpliedModifiers should default to empty, not null");
        }
    }

    [Fact]
    public void MostTypes_ImpliedModifiers_AreEmpty()
    {
        // Only the 4 identity types (Currency, UnitOfMeasure, Dimension, Timezone) carry Notempty
        var withImplied = Types.All.Where(m => m.ImpliedModifiers.Length > 0).ToList();
        withImplied.Should().HaveCountLessThanOrEqualTo(4,
            "only the identity types have implied modifiers");
    }

    // ── ProofRequirements — accessor default ────────────────────────────────────

    [Fact]
    public void AllAccessors_ProofRequirements_IsNotNull()
    {
        foreach (var meta in Types.All)
        {
            foreach (var acc in meta.Accessors)
            {
                acc.ProofRequirements.Should().NotBeNull(
                    $"{meta.Kind}.{acc.Name} ProofRequirements should default to empty");
            }
        }
    }

    // M3 ── Collection accessor proof requirements ────────────────────────────

    [Theory]
    [InlineData(TypeKind.Set, "min")]
    [InlineData(TypeKind.Set, "max")]
    [InlineData(TypeKind.Queue, "peek")]
    [InlineData(TypeKind.Stack, "peek")]
    public void CollectionAccessor_RequiresNonEmptyCollection(TypeKind typeKind, string accessorName)
    {
        var accessor = Types.GetMeta(typeKind).Accessors.Single(a => a.Name == accessorName);
        accessor.ProofRequirements.Should().HaveCount(1,
            $"{typeKind}.{accessorName} requires a non-empty collection");
        var req = accessor.ProofRequirements[0].Should().BeOfType<NumericProofRequirement>().Subject;
        req.Comparison.Should().Be(OperatorKind.GreaterThan,
            $"{typeKind}.{accessorName} requires count > 0");
        req.Threshold.Should().Be(0);
    }

    [Fact]
    public void CollectionCount_HasNoProofRequirements()
    {
        foreach (var kind in new[] { TypeKind.Set, TypeKind.Queue, TypeKind.Stack })
        {
            var accessor = Types.GetMeta(kind).Accessors.Single(a => a.Name == "count");
            accessor.ProofRequirements.Should().BeEmpty(
                $"{kind}.count is always safe — no proof required");
        }
    }

    // M6 ── ImpliedModifiers ─────────────────────────────────────────────────

    [Theory]
    [InlineData(TypeKind.Currency)]
    [InlineData(TypeKind.UnitOfMeasure)]
    [InlineData(TypeKind.Dimension)]
    [InlineData(TypeKind.Timezone)]
    public void IdentityTypes_HaveNotemptyImpliedModifier(TypeKind typeKind)
    {
        Types.GetMeta(typeKind).ImpliedModifiers.Should().Contain(ModifierKind.Notempty,
            $"{typeKind} is an identity type and must carry the notempty implied modifier");
    }

    [Theory]
    [InlineData(TypeKind.Integer)]
    [InlineData(TypeKind.String)]
    [InlineData(TypeKind.Money)]
    public void NonIdentityTypes_HaveNoImpliedModifiers(TypeKind typeKind)
    {
        Types.GetMeta(typeKind).ImpliedModifiers.Should().BeEmpty(
            $"{typeKind} is not an identity type and should carry no implied modifiers");
    }
}
