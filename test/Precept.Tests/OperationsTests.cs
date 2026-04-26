using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the Operations catalog: exhaustiveness, DU shape, indexes,
/// bidirectional lookup, qualifier dispatch, and spec-faithful operation entries.
/// </summary>
public class OperationsTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_CoversEveryOperationKind()
    {
        foreach (var kind in Enum.GetValues<OperationKind>())
        {
            var meta = Operations.GetMeta(kind);
            meta.Kind.Should().Be(kind);
            meta.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void All_CountMatchesEnumValues()
    {
        Operations.All.Should().HaveCount(Enum.GetValues<OperationKind>().Length);
    }

    [Fact]
    public void All_KindsAreDistinct()
    {
        Operations.All.Select(m => m.Kind).Should().OnlyHaveUniqueItems();
    }

    // ── DU shape ────────────────────────────────────────────────────────────────

    [Fact]
    public void All_ContainsBothUnaryAndBinarySubtypes()
    {
        Operations.All.OfType<UnaryOperationMeta>().Should().NotBeEmpty();
        Operations.All.OfType<BinaryOperationMeta>().Should().NotBeEmpty();
    }

    [Fact]
    public void All_EveryEntryIsEitherUnaryOrBinary()
    {
        foreach (var meta in Operations.All)
        {
            (meta is UnaryOperationMeta or BinaryOperationMeta)
                .Should().BeTrue($"{meta.Kind} should be Unary or Binary");
        }
    }

    // ── Unary operations ────────────────────────────────────────────────────────

    [Fact]
    public void Unary_Count()
    {
        Operations.All.OfType<UnaryOperationMeta>().Should().HaveCount(9);
    }

    [Theory]
    [InlineData(OperationKind.NegateInteger, TypeKind.Integer)]
    [InlineData(OperationKind.NegateDecimal, TypeKind.Decimal)]
    [InlineData(OperationKind.NegateNumber, TypeKind.Number)]
    [InlineData(OperationKind.NegateMoney, TypeKind.Money)]
    [InlineData(OperationKind.NegateQuantity, TypeKind.Quantity)]
    [InlineData(OperationKind.NegatePrice, TypeKind.Price)]
    [InlineData(OperationKind.NegateDuration, TypeKind.Duration)]
    [InlineData(OperationKind.NegatePeriod, TypeKind.Period)]
    public void Unary_Negate_ResultMatchesOperand(OperationKind kind, TypeKind expected)
    {
        var meta = (UnaryOperationMeta)Operations.GetMeta(kind);
        meta.Op.Should().Be(OperatorKind.Negate);
        meta.Operand.Kind.Should().Be(expected);
        meta.Result.Should().Be(expected, "negation preserves type");
    }

    [Fact]
    public void Unary_NotBoolean()
    {
        var meta = (UnaryOperationMeta)Operations.GetMeta(OperationKind.NotBoolean);
        meta.Op.Should().Be(OperatorKind.Not);
        meta.Operand.Kind.Should().Be(TypeKind.Boolean);
        meta.Result.Should().Be(TypeKind.Boolean);
    }

    // ── Binary: scalar same-type arithmetic ─────────────────────────────────────

    [Theory]
    [InlineData(OperationKind.IntegerPlusInteger, OperatorKind.Plus, TypeKind.Integer)]
    [InlineData(OperationKind.IntegerMinusInteger, OperatorKind.Minus, TypeKind.Integer)]
    [InlineData(OperationKind.IntegerTimesInteger, OperatorKind.Times, TypeKind.Integer)]
    [InlineData(OperationKind.IntegerDivideInteger, OperatorKind.Divide, TypeKind.Integer)]
    [InlineData(OperationKind.IntegerModuloInteger, OperatorKind.Modulo, TypeKind.Integer)]
    [InlineData(OperationKind.DecimalPlusDecimal, OperatorKind.Plus, TypeKind.Decimal)]
    [InlineData(OperationKind.DecimalMinusDecimal, OperatorKind.Minus, TypeKind.Decimal)]
    [InlineData(OperationKind.DecimalTimesDecimal, OperatorKind.Times, TypeKind.Decimal)]
    [InlineData(OperationKind.DecimalDivideDecimal, OperatorKind.Divide, TypeKind.Decimal)]
    [InlineData(OperationKind.DecimalModuloDecimal, OperatorKind.Modulo, TypeKind.Decimal)]
    [InlineData(OperationKind.NumberPlusNumber, OperatorKind.Plus, TypeKind.Number)]
    [InlineData(OperationKind.NumberMinusNumber, OperatorKind.Minus, TypeKind.Number)]
    [InlineData(OperationKind.NumberTimesNumber, OperatorKind.Times, TypeKind.Number)]
    [InlineData(OperationKind.NumberDivideNumber, OperatorKind.Divide, TypeKind.Number)]
    [InlineData(OperationKind.NumberModuloNumber, OperatorKind.Modulo, TypeKind.Number)]
    public void SameTypeArithmetic_LhsRhsResultAllMatch(
        OperationKind kind, OperatorKind op, TypeKind type)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.Op.Should().Be(op);
        meta.Lhs.Kind.Should().Be(type);
        meta.Rhs.Kind.Should().Be(type);
        meta.Result.Should().Be(type);
        meta.BidirectionalLookup.Should().BeFalse("same-type ops don't need bidirectional lookup");
        meta.Match.Should().Be(QualifierMatch.Any);
    }

    // ── Binary: widening ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(OperationKind.IntegerPlusDecimal, OperatorKind.Plus, TypeKind.Decimal)]
    [InlineData(OperationKind.IntegerMinusDecimal, OperatorKind.Minus, TypeKind.Decimal)]
    [InlineData(OperationKind.IntegerTimesDecimal, OperatorKind.Times, TypeKind.Decimal)]
    [InlineData(OperationKind.IntegerDivideDecimal, OperatorKind.Divide, TypeKind.Decimal)]
    [InlineData(OperationKind.IntegerModuloDecimal, OperatorKind.Modulo, TypeKind.Decimal)]
    [InlineData(OperationKind.IntegerPlusNumber, OperatorKind.Plus, TypeKind.Number)]
    [InlineData(OperationKind.IntegerMinusNumber, OperatorKind.Minus, TypeKind.Number)]
    [InlineData(OperationKind.IntegerTimesNumber, OperatorKind.Times, TypeKind.Number)]
    [InlineData(OperationKind.IntegerDivideNumber, OperatorKind.Divide, TypeKind.Number)]
    [InlineData(OperationKind.IntegerModuloNumber, OperatorKind.Modulo, TypeKind.Number)]
    public void Widening_IntegerWidensToTarget(
        OperationKind kind, OperatorKind op, TypeKind resultType)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.Op.Should().Be(op);
        meta.Lhs.Kind.Should().Be(TypeKind.Integer);
        meta.Result.Should().Be(resultType);
        meta.BidirectionalLookup.Should().BeTrue("widening applies in both operand orders");
    }

    // ── Binary: string ──────────────────────────────────────────────────────────

    [Fact]
    public void StringPlusString_Concatenation()
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(OperationKind.StringPlusString);
        meta.Op.Should().Be(OperatorKind.Plus);
        meta.Lhs.Kind.Should().Be(TypeKind.String);
        meta.Rhs.Kind.Should().Be(TypeKind.String);
        meta.Result.Should().Be(TypeKind.String);
        meta.BidirectionalLookup.Should().BeFalse("string concatenation is order-dependent");
    }

    // ── Binary: temporal spot checks ────────────────────────────────────────────

    [Theory]
    [InlineData(OperationKind.DatePlusPeriod, OperatorKind.Plus, TypeKind.Date, TypeKind.Period, TypeKind.Date)]
    [InlineData(OperationKind.DateMinusPeriod, OperatorKind.Minus, TypeKind.Date, TypeKind.Period, TypeKind.Date)]
    [InlineData(OperationKind.DateMinusDate, OperatorKind.Minus, TypeKind.Date, TypeKind.Date, TypeKind.Period)]
    [InlineData(OperationKind.TimeMinusTime, OperatorKind.Minus, TypeKind.Time, TypeKind.Time, TypeKind.Period)]
    [InlineData(OperationKind.InstantMinusInstant, OperatorKind.Minus, TypeKind.Instant, TypeKind.Instant, TypeKind.Duration)]
    [InlineData(OperationKind.InstantPlusDuration, OperatorKind.Plus, TypeKind.Instant, TypeKind.Duration, TypeKind.Instant)]
    [InlineData(OperationKind.DurationDivideDuration, OperatorKind.Divide, TypeKind.Duration, TypeKind.Duration, TypeKind.Number)]
    [InlineData(OperationKind.ZonedDateTimeMinusZonedDateTime, OperatorKind.Minus, TypeKind.ZonedDateTime, TypeKind.ZonedDateTime, TypeKind.Duration)]
    [InlineData(OperationKind.DateTimeMinusDateTime, OperatorKind.Minus, TypeKind.DateTime, TypeKind.DateTime, TypeKind.Period)]
    public void Temporal_OperandAndResultTypes(
        OperationKind kind, OperatorKind op,
        TypeKind lhsType, TypeKind rhsType, TypeKind resultType)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.Op.Should().Be(op);
        meta.Lhs.Kind.Should().Be(lhsType);
        meta.Rhs.Kind.Should().Be(rhsType);
        meta.Result.Should().Be(resultType);
    }

    [Fact]
    public void DatePlusTime_IsBidirectionalLookup()
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(OperationKind.DatePlusTime);
        meta.Op.Should().Be(OperatorKind.Plus);
        meta.Lhs.Kind.Should().Be(TypeKind.Date);
        meta.Rhs.Kind.Should().Be(TypeKind.Time);
        meta.Result.Should().Be(TypeKind.DateTime);
        meta.BidirectionalLookup.Should().BeTrue("date + time = time + date (spec Decision #25)");
    }

    [Theory]
    [InlineData(OperationKind.DurationTimesInteger, TypeKind.Integer)]
    [InlineData(OperationKind.DurationTimesNumber, TypeKind.Number)]
    public void DurationScaling_UsesIntegerOrNumber_NotDecimal(
        OperationKind kind, TypeKind scalarType)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.Op.Should().Be(OperatorKind.Times);
        meta.Lhs.Kind.Should().Be(TypeKind.Duration);
        meta.Rhs.Kind.Should().Be(scalarType);
        meta.Result.Should().Be(TypeKind.Duration);
        meta.BidirectionalLookup.Should().BeTrue("NodaTime supports both operand orders");
    }

    // ── Binary: business-domain spot checks ─────────────────────────────────────

    [Fact]
    public void MoneyTimesDecimal_IsBidirectionalLookup()
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(OperationKind.MoneyTimesDecimal);
        meta.Lhs.Kind.Should().Be(TypeKind.Money);
        meta.Rhs.Kind.Should().Be(TypeKind.Decimal);
        meta.Result.Should().Be(TypeKind.Money);
        meta.BidirectionalLookup.Should().BeTrue();
    }

    [Theory]
    [InlineData(OperationKind.MoneyDivideQuantity, TypeKind.Quantity, TypeKind.Price)]
    [InlineData(OperationKind.MoneyDividePeriod, TypeKind.Period, TypeKind.Price)]
    [InlineData(OperationKind.MoneyDivideDuration, TypeKind.Duration, TypeKind.Price)]
    public void MoneyDivide_ProducesPriceFromVariousDenominators(
        OperationKind kind, TypeKind divisorType, TypeKind resultType)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.Op.Should().Be(OperatorKind.Divide);
        meta.Lhs.Kind.Should().Be(TypeKind.Money);
        meta.Rhs.Kind.Should().Be(divisorType);
        meta.Result.Should().Be(resultType);
    }

    [Theory]
    [InlineData(OperationKind.PriceTimesQuantity, TypeKind.Quantity, TypeKind.Money)]
    [InlineData(OperationKind.PriceTimesPeriod, TypeKind.Period, TypeKind.Money)]
    [InlineData(OperationKind.PriceTimesDuration, TypeKind.Duration, TypeKind.Money)]
    public void PriceTimes_DimensionalCancellation_ProducesMoney(
        OperationKind kind, TypeKind multiplierType, TypeKind resultType)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.Op.Should().Be(OperatorKind.Times);
        meta.Lhs.Kind.Should().Be(TypeKind.Price);
        meta.Rhs.Kind.Should().Be(multiplierType);
        meta.Result.Should().Be(resultType);
        meta.BidirectionalLookup.Should().BeTrue();
    }

    [Fact]
    public void ExchangeRateTimesMoney_ProducesMoney()
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(OperationKind.ExchangeRateTimesMoney);
        meta.Op.Should().Be(OperatorKind.Times);
        meta.Lhs.Kind.Should().Be(TypeKind.ExchangeRate);
        meta.Rhs.Kind.Should().Be(TypeKind.Money);
        meta.Result.Should().Be(TypeKind.Money);
        meta.BidirectionalLookup.Should().BeTrue();
    }

    // ── QualifierMatch entries ───────────────────────────────────────────────────

    [Fact]
    public void QualifierMatch_MoneyDivideMoney_HasTwoEntries()
    {
        var same = (BinaryOperationMeta)Operations.GetMeta(OperationKind.MoneyDivideMoneySameCurrency);
        same.Match.Should().Be(QualifierMatch.Same);
        same.Result.Should().Be(TypeKind.Decimal, "same currency → dimensionless ratio");

        var diff = (BinaryOperationMeta)Operations.GetMeta(OperationKind.MoneyDivideMoneyCrossCurrency);
        diff.Match.Should().Be(QualifierMatch.Different);
        diff.Result.Should().Be(TypeKind.ExchangeRate, "different currencies → exchange rate");
    }

    [Fact]
    public void QualifierMatch_QuantityDivideQuantity_HasTwoEntries()
    {
        var same = (BinaryOperationMeta)Operations.GetMeta(OperationKind.QuantityDivideQuantitySameDimension);
        same.Match.Should().Be(QualifierMatch.Same);
        same.Result.Should().Be(TypeKind.Decimal, "same dimension → dimensionless ratio");

        var diff = (BinaryOperationMeta)Operations.GetMeta(OperationKind.QuantityDivideQuantityCrossDimension);
        diff.Match.Should().Be(QualifierMatch.Different);
        diff.Result.Should().Be(TypeKind.Quantity, "different dimensions → compound quantity");
    }

    [Fact]
    public void QualifierMatch_NonAnyEntries()
    {
        var nonAny = Operations.All
            .OfType<BinaryOperationMeta>()
            .Where(m => m.Match != QualifierMatch.Any)
            .ToList();

        // Arithmetic: 4 (money/money Same/Different, quantity/quantity Same/Different)
        // Comparison: 6 money + 6 quantity + 6 price + 2 exchangerate = 20
        // Total: 24
        nonAny.Should().HaveCount(24);

        // Verify the 4 arithmetic entries are present
        nonAny.Should().Contain(m => m.Kind == OperationKind.MoneyDivideMoneySameCurrency);
        nonAny.Should().Contain(m => m.Kind == OperationKind.MoneyDivideMoneyCrossCurrency);
        nonAny.Should().Contain(m => m.Kind == OperationKind.QuantityDivideQuantitySameDimension);
        nonAny.Should().Contain(m => m.Kind == OperationKind.QuantityDivideQuantityCrossDimension);
    }

    // ── Bidirectional lookup ────────────────────────────────────────────────────

    [Fact]
    public void BidirectionalLookup_AllMarkedEntriesHaveDifferentOperandTypes()
    {
        // BidirectionalLookup flag only makes sense when Lhs and Rhs differ.
        // Same-type operations (integer+integer) don't need it.
        var bidirectional = Operations.All
            .OfType<BinaryOperationMeta>()
            .Where(m => m.BidirectionalLookup);

        foreach (var meta in bidirectional)
        {
            meta.Lhs.Kind.Should().NotBe(meta.Rhs.Kind,
                $"{meta.Kind} is marked BidirectionalLookup but has identical operand types");
        }
    }

    [Fact]
    public void BidirectionalLookup_Count()
    {
        // All bidirectional-lookup entries:
        // Arithmetic widening: 10 (5 integer/decimal + 5 integer/number)
        // Temporal: DatePlusTime, DurationTimesInteger, DurationTimesNumber = 3
        // Business: MoneyTimesDecimal, QuantityTimesDecimal,
        //   QuantityTimesPeriod, QuantityTimesDuration, PriceTimesQuantity,
        //   PriceTimesPeriod, PriceTimesDuration, PriceTimesDecimal,
        //   ExchangeRateTimesMoney, ExchangeRateTimesDecimal = 10
        // Comparison widening: 12 (6 integer/decimal + 6 integer/number)
        // Total: 35
        Operations.All
            .OfType<BinaryOperationMeta>()
            .Count(m => m.BidirectionalLookup)
            .Should().Be(35);
    }

    // ── Unary index ─────────────────────────────────────────────────────────────

    [Fact]
    public void UnaryIndex_ContainsAllUnaryOperations()
    {
        Operations.UnaryIndex.Count.Should().Be(9);
    }

    [Theory]
    [InlineData(OperatorKind.Negate, TypeKind.Integer, OperationKind.NegateInteger)]
    [InlineData(OperatorKind.Negate, TypeKind.Decimal, OperationKind.NegateDecimal)]
    [InlineData(OperatorKind.Negate, TypeKind.Number, OperationKind.NegateNumber)]
    [InlineData(OperatorKind.Negate, TypeKind.Money, OperationKind.NegateMoney)]
    [InlineData(OperatorKind.Negate, TypeKind.Quantity, OperationKind.NegateQuantity)]
    [InlineData(OperatorKind.Negate, TypeKind.Price, OperationKind.NegatePrice)]
    [InlineData(OperatorKind.Negate, TypeKind.Duration, OperationKind.NegateDuration)]
    [InlineData(OperatorKind.Negate, TypeKind.Period, OperationKind.NegatePeriod)]
    [InlineData(OperatorKind.Not, TypeKind.Boolean, OperationKind.NotBoolean)]
    public void UnaryIndex_LookupByOpAndOperand(
        OperatorKind op, TypeKind operand, OperationKind expected)
    {
        Operations.UnaryIndex.TryGetValue((op, operand), out var meta).Should().BeTrue();
        meta!.Kind.Should().Be(expected);
    }

    [Fact]
    public void FindUnary_ReturnsNullForInvalidCombinations()
    {
        Operations.FindUnary(OperatorKind.Negate, TypeKind.String).Should().BeNull();
        Operations.FindUnary(OperatorKind.Negate, TypeKind.Boolean).Should().BeNull();
        Operations.FindUnary(OperatorKind.Not, TypeKind.Integer).Should().BeNull();
        Operations.FindUnary(OperatorKind.Negate, TypeKind.ExchangeRate).Should().BeNull();
    }

    // ── Binary index: canonical lookups ──────────────────────────────────────────

    [Theory]
    [InlineData(OperatorKind.Plus, TypeKind.Integer, TypeKind.Integer, 1)]
    [InlineData(OperatorKind.Minus, TypeKind.Date, TypeKind.Date, 1)]
    [InlineData(OperatorKind.Plus, TypeKind.Date, TypeKind.Time, 1)]
    [InlineData(OperatorKind.Times, TypeKind.Money, TypeKind.Decimal, 1)]
    [InlineData(OperatorKind.Divide, TypeKind.Money, TypeKind.Money, 2)]   // Same + Different
    [InlineData(OperatorKind.Divide, TypeKind.Quantity, TypeKind.Quantity, 2)] // Same + Different
    public void BinaryIndex_CanonicalLookup(
        OperatorKind op, TypeKind lhs, TypeKind rhs, int expectedCount)
    {
        var candidates = Operations.FindCandidates(op, lhs, rhs);
        candidates.Length.Should().Be(expectedCount);
    }

    // ── Binary index: bidirectional reverse lookups ──────────────────────────────

    [Theory]
    [InlineData(OperatorKind.Plus, TypeKind.Time, TypeKind.Date)]       // time + date
    [InlineData(OperatorKind.Plus, TypeKind.Decimal, TypeKind.Integer)] // decimal + integer
    [InlineData(OperatorKind.Minus, TypeKind.Decimal, TypeKind.Integer)] // decimal - integer
    [InlineData(OperatorKind.Times, TypeKind.Decimal, TypeKind.Money)]  // decimal * money
    [InlineData(OperatorKind.Times, TypeKind.Integer, TypeKind.Duration)] // integer * duration
    [InlineData(OperatorKind.Times, TypeKind.Number, TypeKind.Duration)] // number * duration
    [InlineData(OperatorKind.Times, TypeKind.Decimal, TypeKind.Quantity)] // decimal * quantity
    [InlineData(OperatorKind.Times, TypeKind.Quantity, TypeKind.Price)]  // quantity * price
    [InlineData(OperatorKind.Times, TypeKind.Period, TypeKind.Price)]    // period * price
    [InlineData(OperatorKind.Times, TypeKind.Duration, TypeKind.Price)]  // duration * price
    [InlineData(OperatorKind.Times, TypeKind.Money, TypeKind.ExchangeRate)] // money * exchangerate
    [InlineData(OperatorKind.Times, TypeKind.Decimal, TypeKind.ExchangeRate)] // decimal * exchangerate
    [InlineData(OperatorKind.Times, TypeKind.Decimal, TypeKind.Price)]  // decimal * price
    public void BinaryIndex_BidirectionalReverseIsIndexed(
        OperatorKind op, TypeKind lhs, TypeKind rhs)
    {
        var candidates = Operations.FindCandidates(op, lhs, rhs);
        candidates.Length.Should().BeGreaterThan(0,
            $"Reverse lookup ({op}, {lhs}, {rhs}) should be indexed for bidirectional-lookup entry");
    }

    [Fact]
    public void BinaryIndex_NonBidirectional_ReverseNotIndexed()
    {
        // date - period exists but period - date does not
        Operations.FindCandidates(OperatorKind.Minus, TypeKind.Period, TypeKind.Date)
            .Length.Should().Be(0);

        // money / decimal exists but decimal / money does not
        Operations.FindCandidates(OperatorKind.Divide, TypeKind.Decimal, TypeKind.Money)
            .Length.Should().Be(0);

        // duration / integer exists but integer / duration does not
        Operations.FindCandidates(OperatorKind.Divide, TypeKind.Integer, TypeKind.Duration)
            .Length.Should().Be(0);
    }

    [Fact]
    public void FindCandidates_ReturnsEmptyForInvalidCombinations()
    {
        Operations.FindCandidates(OperatorKind.Plus, TypeKind.Money, TypeKind.Integer)
            .Length.Should().Be(0, "money + integer is not supported");

        Operations.FindCandidates(OperatorKind.Times, TypeKind.Money, TypeKind.Money)
            .Length.Should().Be(0, "money * money is not supported");

        Operations.FindCandidates(OperatorKind.Plus, TypeKind.Date, TypeKind.Date)
            .Length.Should().Be(0, "date + date is not supported");

        Operations.FindCandidates(OperatorKind.Times, TypeKind.Decimal, TypeKind.Number)
            .Length.Should().Be(0, "decimal * number is a type error (no implicit widening)");
    }

    // ── Qualifier dispatch via binary index ──────────────────────────────────────

    [Fact]
    public void QualifierDispatch_MoneyDivideMoney_IndexHasTwoEntries()
    {
        var candidates = Operations.FindCandidates(
            OperatorKind.Divide, TypeKind.Money, TypeKind.Money);

        candidates.Length.Should().Be(2);

        var same = candidates.ToArray().Single(c => c.Match == QualifierMatch.Same);
        same.Result.Should().Be(TypeKind.Decimal);

        var diff = candidates.ToArray().Single(c => c.Match == QualifierMatch.Different);
        diff.Result.Should().Be(TypeKind.ExchangeRate);
    }

    [Fact]
    public void QualifierDispatch_QuantityDivideQuantity_IndexHasTwoEntries()
    {
        var candidates = Operations.FindCandidates(
            OperatorKind.Divide, TypeKind.Quantity, TypeKind.Quantity);

        candidates.Length.Should().Be(2);

        var same = candidates.ToArray().Single(c => c.Match == QualifierMatch.Same);
        same.Result.Should().Be(TypeKind.Decimal);

        var diff = candidates.ToArray().Single(c => c.Match == QualifierMatch.Different);
        diff.Result.Should().Be(TypeKind.Quantity);
    }

    // ── Spec constraints: operations that MUST NOT exist ─────────────────────────

    [Theory]
    [InlineData(OperatorKind.Times, TypeKind.Duration, TypeKind.Decimal)]  // Duration * decimal is a type error
    [InlineData(OperatorKind.Divide, TypeKind.Duration, TypeKind.Decimal)] // Duration / decimal is a type error
    [InlineData(OperatorKind.Times, TypeKind.Money, TypeKind.Number)]      // Money * number is a type error
    [InlineData(OperatorKind.Plus, TypeKind.ExchangeRate, TypeKind.ExchangeRate)] // ExchangeRate + ExchangeRate not supported
    [InlineData(OperatorKind.Plus, TypeKind.DateTime, TypeKind.Duration)]  // DateTime ± duration is a compile error (Decision #27)
    [InlineData(OperatorKind.Minus, TypeKind.DateTime, TypeKind.Duration)] // DateTime − duration is a compile error (Decision #27)
    [InlineData(OperatorKind.Plus, TypeKind.Date, TypeKind.Duration)]      // Date + duration not supported
    [InlineData(OperatorKind.Plus, TypeKind.Instant, TypeKind.Period)]     // Instant + period not supported
    [InlineData(OperatorKind.Times, TypeKind.Period, TypeKind.Integer)]    // Period * integer not supported
    public void SpecConstraint_OperationMustNotExist(
        OperatorKind op, TypeKind lhs, TypeKind rhs)
    {
        Operations.FindCandidates(op, lhs, rhs).Length.Should().Be(0,
            $"({op}, {lhs}, {rhs}) should not be a legal operation per the spec");
    }

    [Theory]
    [InlineData(OperatorKind.Negate, TypeKind.ExchangeRate)] // Negative exchange rates have no business meaning
    [InlineData(OperatorKind.Negate, TypeKind.Date)]         // Can't negate a date
    [InlineData(OperatorKind.Negate, TypeKind.String)]       // Can't negate a string
    [InlineData(OperatorKind.Not, TypeKind.Integer)]         // Not only applies to boolean
    public void SpecConstraint_UnaryMustNotExist(
        OperatorKind op, TypeKind operand)
    {
        Operations.FindUnary(op, operand).Should().BeNull(
            $"({op}, {operand}) should not be a legal unary operation per the spec");
    }

    // ── Cross-catalog reference integrity ────────────────────────────────────────

    [Fact]
    public void AllOperations_ReferenceValidOperatorKinds()
    {
        var validOps = new HashSet<OperatorKind>(Enum.GetValues<OperatorKind>());
        foreach (var meta in Operations.All)
        {
            validOps.Should().Contain(meta.Op,
                $"{meta.Kind} references invalid OperatorKind {meta.Op}");
        }
    }

    [Fact]
    public void AllOperations_ReferenceValidTypeKinds()
    {
        var validTypes = new HashSet<TypeKind>(Enum.GetValues<TypeKind>());
        foreach (var meta in Operations.All)
        {
            validTypes.Should().Contain(meta.Result,
                $"{meta.Kind} has invalid Result TypeKind {meta.Result}");

            if (meta is UnaryOperationMeta unary)
                validTypes.Should().Contain(unary.Operand.Kind);
            else if (meta is BinaryOperationMeta binary)
            {
                validTypes.Should().Contain(binary.Lhs.Kind);
                validTypes.Should().Contain(binary.Rhs.Kind);
            }
        }
    }

    // ── Count invariants ────────────────────────────────────────────────────────

    [Fact]
    public void Binary_Count()
    {
        // 83 arithmetic + 104 comparison = 187 binary
        Operations.All.OfType<BinaryOperationMeta>().Should().HaveCount(187);
    }

    [Fact]
    public void Total_Count()
    {
        // 9 unary + 187 binary = 196 total
        Operations.All.Should().HaveCount(196);
    }

    // ── Proof Requirements ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(OperationKind.IntegerDivideInteger)]
    [InlineData(OperationKind.DecimalDivideDecimal)]
    [InlineData(OperationKind.NumberDivideNumber)]
    [InlineData(OperationKind.IntegerDivideDecimal)]
    [InlineData(OperationKind.IntegerDivideNumber)]
    public void Division_HasNonZeroDivisorRequirement(OperationKind kind)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.ProofRequirements.Should().HaveCount(1, $"{kind} divisor must be proven != 0");
        var req = meta.ProofRequirements[0].Should().BeOfType<NumericProofRequirement>().Subject;
        req.Comparison.Should().Be(OperatorKind.NotEquals, $"{kind} divisor must be != 0");
        req.Threshold.Should().Be(0);
    }

    [Theory]
    [InlineData(OperationKind.IntegerModuloInteger)]
    [InlineData(OperationKind.DecimalModuloDecimal)]
    [InlineData(OperationKind.NumberModuloNumber)]
    [InlineData(OperationKind.IntegerModuloDecimal)]
    [InlineData(OperationKind.IntegerModuloNumber)]
    public void Modulo_HasNonZeroDivisorRequirement(OperationKind kind)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.ProofRequirements.Should().HaveCount(1, $"{kind} divisor must be proven != 0");
        var req = meta.ProofRequirements[0].Should().BeOfType<NumericProofRequirement>().Subject;
        req.Comparison.Should().Be(OperatorKind.NotEquals, $"{kind} divisor must be != 0");
        req.Threshold.Should().Be(0);
    }

    [Fact]
    public void Division_ProofSubject_ReferencesRhsType()
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(OperationKind.IntegerDivideDecimal);
        var req = meta.ProofRequirements[0].Should().BeOfType<NumericProofRequirement>().Subject;
        var subject = req.Subject.Should().BeOfType<ParamSubject>().Subject;
        subject.Parameter.Kind.Should().Be(TypeKind.Decimal,
            "divisor for integer / decimal is the decimal RHS");
    }

    // M5 ── Temporal period proof requirements ────────────────────────────────

    [Theory]
    [InlineData(OperationKind.DatePlusPeriod)]
    [InlineData(OperationKind.DateMinusPeriod)]
    public void DatePeriodOps_RequireDateDimension(OperationKind kind)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.ProofRequirements.Should().HaveCount(1, $"{kind} period must have date dimension");
        var req = meta.ProofRequirements[0].Should().BeOfType<DimensionProofRequirement>().Subject;
        req.RequiredDimension.Should().Be(PeriodDimension.Date,
            $"{kind} requires a date-level period operand");
    }

    [Theory]
    [InlineData(OperationKind.TimePlusPeriod)]
    [InlineData(OperationKind.TimeMinusPeriod)]
    public void TimePeriodOps_RequireTimeDimension(OperationKind kind)
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(kind);
        meta.ProofRequirements.Should().HaveCount(1, $"{kind} period must have time dimension");
        var req = meta.ProofRequirements[0].Should().BeOfType<DimensionProofRequirement>().Subject;
        req.RequiredDimension.Should().Be(PeriodDimension.Time,
            $"{kind} requires a time-level period operand");
    }

    // ── Comparison: all produce Boolean ──────────────────────────────────────────

    [Fact]
    public void Comparison_AllProduceBoolean()
    {
        var comparisonOps = new HashSet<OperatorKind>
        {
            OperatorKind.Equals, OperatorKind.NotEquals,
            OperatorKind.LessThan, OperatorKind.GreaterThan,
            OperatorKind.LessThanOrEqual, OperatorKind.GreaterThanOrEqual,
            OperatorKind.CaseInsensitiveEquals, OperatorKind.CaseInsensitiveNotEquals,
        };

        var comparisonEntries = Operations.All
            .OfType<BinaryOperationMeta>()
            .Where(m => comparisonOps.Contains(m.Op));

        foreach (var entry in comparisonEntries)
        {
            entry.Result.Should().Be(TypeKind.Boolean,
                $"{entry.Kind} is a comparison but Result is {entry.Result}");
        }
    }

    [Fact]
    public void Comparison_EntryCount()
    {
        // 18 equality-only + 72 orderable + 12 widening + 2 case-insensitive = 104
        var comparisonOps = new HashSet<OperatorKind>
        {
            OperatorKind.Equals, OperatorKind.NotEquals,
            OperatorKind.LessThan, OperatorKind.GreaterThan,
            OperatorKind.LessThanOrEqual, OperatorKind.GreaterThanOrEqual,
            OperatorKind.CaseInsensitiveEquals, OperatorKind.CaseInsensitiveNotEquals,
        };

        Operations.All
            .OfType<BinaryOperationMeta>()
            .Count(m => comparisonOps.Contains(m.Op))
            .Should().Be(104);
    }

    // ── Comparison: equality-only types ──────────────────────────────────────────

    [Theory]
    [InlineData(TypeKind.Boolean)]
    [InlineData(TypeKind.Period)]
    [InlineData(TypeKind.Timezone)]
    [InlineData(TypeKind.ZonedDateTime)]
    [InlineData(TypeKind.Currency)]
    [InlineData(TypeKind.UnitOfMeasure)]
    [InlineData(TypeKind.Dimension)]
    [InlineData(TypeKind.ExchangeRate)]
    [InlineData(TypeKind.String)]
    public void EqualityOnly_SupportsEqualsAndNotEquals(TypeKind type)
    {
        Operations.FindCandidates(OperatorKind.Equals, type, type)
            .Length.Should().BeGreaterThan(0, $"{type} should support ==");
        Operations.FindCandidates(OperatorKind.NotEquals, type, type)
            .Length.Should().BeGreaterThan(0, $"{type} should support !=");
    }

    [Theory]
    [InlineData(TypeKind.Boolean)]
    [InlineData(TypeKind.Period)]
    [InlineData(TypeKind.Timezone)]
    [InlineData(TypeKind.ZonedDateTime)]
    [InlineData(TypeKind.Currency)]
    [InlineData(TypeKind.UnitOfMeasure)]
    [InlineData(TypeKind.Dimension)]
    [InlineData(TypeKind.ExchangeRate)]
    [InlineData(TypeKind.String)]
    public void EqualityOnly_DoesNotSupportOrdering(TypeKind type)
    {
        Operations.FindCandidates(OperatorKind.LessThan, type, type)
            .Length.Should().Be(0, $"{type} should not support <");
        Operations.FindCandidates(OperatorKind.GreaterThan, type, type)
            .Length.Should().Be(0, $"{type} should not support >");
    }

    // ── Comparison: orderable types ─────────────────────────────────────────────

    [Theory]
    [InlineData(TypeKind.Integer)]
    [InlineData(TypeKind.Decimal)]
    [InlineData(TypeKind.Number)]
    [InlineData(TypeKind.Choice)]
    [InlineData(TypeKind.Date)]
    [InlineData(TypeKind.Time)]
    [InlineData(TypeKind.Instant)]
    [InlineData(TypeKind.Duration)]
    [InlineData(TypeKind.DateTime)]
    [InlineData(TypeKind.Money)]
    [InlineData(TypeKind.Quantity)]
    [InlineData(TypeKind.Price)]
    public void Orderable_SupportsAllSixComparisons(TypeKind type)
    {
        Operations.FindCandidates(OperatorKind.Equals, type, type).Length.Should().BeGreaterThan(0);
        Operations.FindCandidates(OperatorKind.NotEquals, type, type).Length.Should().BeGreaterThan(0);
        Operations.FindCandidates(OperatorKind.LessThan, type, type).Length.Should().BeGreaterThan(0);
        Operations.FindCandidates(OperatorKind.GreaterThan, type, type).Length.Should().BeGreaterThan(0);
        Operations.FindCandidates(OperatorKind.LessThanOrEqual, type, type).Length.Should().BeGreaterThan(0);
        Operations.FindCandidates(OperatorKind.GreaterThanOrEqual, type, type).Length.Should().BeGreaterThan(0);
    }

    // ── Comparison: qualifier requirements ───────────────────────────────────────

    [Theory]
    [InlineData(TypeKind.Money)]
    [InlineData(TypeKind.Quantity)]
    [InlineData(TypeKind.Price)]
    [InlineData(TypeKind.ExchangeRate)]
    public void QualifiedComparison_RequiresSameQualifier(TypeKind type)
    {
        var eqCandidates = Operations.FindCandidates(OperatorKind.Equals, type, type);
        eqCandidates.Length.Should().Be(1);
        eqCandidates[0].Match.Should().Be(QualifierMatch.Same,
            $"{type} == {type} should require same qualifier");
    }

    // ── Comparison: widening ────────────────────────────────────────────────────

    [Theory]
    [InlineData(OperatorKind.Equals)]
    [InlineData(OperatorKind.NotEquals)]
    [InlineData(OperatorKind.LessThan)]
    [InlineData(OperatorKind.GreaterThan)]
    [InlineData(OperatorKind.LessThanOrEqual)]
    [InlineData(OperatorKind.GreaterThanOrEqual)]
    public void WideningComparison_IntegerDecimal_BothDirections(OperatorKind op)
    {
        // Canonical: integer op decimal
        Operations.FindCandidates(op, TypeKind.Integer, TypeKind.Decimal)
            .Length.Should().BeGreaterThan(0);
        // Reverse: decimal op integer (bidirectional-lookup index)
        Operations.FindCandidates(op, TypeKind.Decimal, TypeKind.Integer)
            .Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(OperatorKind.Equals)]
    [InlineData(OperatorKind.NotEquals)]
    [InlineData(OperatorKind.LessThan)]
    [InlineData(OperatorKind.GreaterThan)]
    [InlineData(OperatorKind.LessThanOrEqual)]
    [InlineData(OperatorKind.GreaterThanOrEqual)]
    public void WideningComparison_IntegerNumber_BothDirections(OperatorKind op)
    {
        Operations.FindCandidates(op, TypeKind.Integer, TypeKind.Number)
            .Length.Should().BeGreaterThan(0);
        Operations.FindCandidates(op, TypeKind.Number, TypeKind.Integer)
            .Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(OperatorKind.Equals)]
    [InlineData(OperatorKind.NotEquals)]
    [InlineData(OperatorKind.LessThan)]
    [InlineData(OperatorKind.GreaterThan)]
    [InlineData(OperatorKind.LessThanOrEqual)]
    [InlineData(OperatorKind.GreaterThanOrEqual)]
    public void WideningComparison_DecimalNumber_IsTypeError(OperatorKind op)
    {
        // decimal vs number has no implicit widening — it's a type error
        Operations.FindCandidates(op, TypeKind.Decimal, TypeKind.Number)
            .Length.Should().Be(0);
        Operations.FindCandidates(op, TypeKind.Number, TypeKind.Decimal)
            .Length.Should().Be(0);
    }

    // ── Comparison: case-insensitive ────────────────────────────────────────────

    [Fact]
    public void CaseInsensitive_OnlyForString()
    {
        Operations.FindCandidates(OperatorKind.CaseInsensitiveEquals, TypeKind.String, TypeKind.String)
            .Length.Should().Be(1);
        Operations.FindCandidates(OperatorKind.CaseInsensitiveNotEquals, TypeKind.String, TypeKind.String)
            .Length.Should().Be(1);
    }

    [Fact]
    public void CaseInsensitive_NotForOtherTypes()
    {
        Operations.FindCandidates(OperatorKind.CaseInsensitiveEquals, TypeKind.Integer, TypeKind.Integer)
            .Length.Should().Be(0);
        Operations.FindCandidates(OperatorKind.CaseInsensitiveNotEquals, TypeKind.Boolean, TypeKind.Boolean)
            .Length.Should().Be(0);
    }

    // ── Comparison: spec constraints (must NOT exist) ───────────────────────────

    [Fact]
    public void Comparison_ZonedDateTime_NoOrdering()
    {
        // Spec: NodaTime omits IComparable<ZonedDateTime> — no natural ordering
        Operations.FindCandidates(OperatorKind.LessThan, TypeKind.ZonedDateTime, TypeKind.ZonedDateTime)
            .Length.Should().Be(0);
    }

    [Fact]
    public void Comparison_Period_NoOrdering()
    {
        // Spec: "1 month" isn't always the same length — ordering isn't reliable
        Operations.FindCandidates(OperatorKind.LessThan, TypeKind.Period, TypeKind.Period)
            .Length.Should().Be(0);
    }

    [Fact]
    public void Comparison_ExchangeRate_NoOrdering()
    {
        // Spec: Exchange rates have no meaningful ordering outside their time context
        Operations.FindCandidates(OperatorKind.LessThan, TypeKind.ExchangeRate, TypeKind.ExchangeRate)
            .Length.Should().Be(0);
    }

    // ── ProofRequirements ─ default empty ────────────────────────────────────────

    [Fact]
    public void AllBinaryOperations_ProofRequirements_IsNotNull()
    {
        foreach (var op in Operations.All.OfType<BinaryOperationMeta>())
        {
            op.ProofRequirements.Should().NotBeNull(
                $"{op.Kind} ProofRequirements should default to empty");
        }
    }
}
