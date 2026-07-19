using System.Collections.Immutable;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Interpolated typed constants key by result type + static parts (text,
/// magnitude, qualifier) + per-slot semantic kind — '{X} USD' and '{X} EUR'
/// are different constants even with identical hole expressions.
/// </summary>
public class InterpolatedConstantKeyTests
{
    private static readonly TypedExpression Hole =
        new TypedFieldRef(TypeKind.Decimal, "X", IsCaseInsensitive: false,
            DeclaredQualifiers: null, Span: default);

    private static InterpolatedTypedConstant MoneyConst(
        string currency, InterpolationSlotKind slotKind = InterpolationSlotKind.Magnitude) =>
        new(
            Slots: [new TypedInterpolationSlot(Hole, slotKind)],
            ResultType: TypeKind.Money,
            Span: default,
            StaticQualifier: new StaticCurrencyQualifier(currency));

    [Fact]
    public void DifferentStaticCurrencies_KeyDifferently()
    {
        WpCalculator.Canonicalize(MoneyConst("USD")).Key
            .Should().NotBe(WpCalculator.Canonicalize(MoneyConst("EUR")).Key);
    }

    [Fact]
    public void SameStaticCurrency_KeysEqual()
    {
        WpCalculator.Canonicalize(MoneyConst("USD")).Key
            .Should().Be(WpCalculator.Canonicalize(MoneyConst("USD")).Key);
    }

    [Fact]
    public void DifferentSlotKinds_KeyDifferently()
    {
        WpCalculator.Canonicalize(MoneyConst("USD", InterpolationSlotKind.Magnitude)).Key
            .Should().NotBe(
                WpCalculator.Canonicalize(MoneyConst("USD", InterpolationSlotKind.Currency)).Key);
    }

    [Fact]
    public void DifferentResultTypes_KeyDifferently()
    {
        var money = new InterpolatedTypedConstant(
            [new TypedInterpolationSlot(Hole, InterpolationSlotKind.Magnitude)],
            TypeKind.Money, default);
        var quantity = new InterpolatedTypedConstant(
            [new TypedInterpolationSlot(Hole, InterpolationSlotKind.Magnitude)],
            TypeKind.Quantity, default);

        WpCalculator.Canonicalize(money).Key
            .Should().NotBe(WpCalculator.Canonicalize(quantity).Key);
    }

    [Fact]
    public void StaticMagnitude_ParticipatesInTheKey()
    {
        var one = new InterpolatedTypedConstant(
            [new TypedInterpolationSlot(Hole, InterpolationSlotKind.Currency)],
            TypeKind.Money, default, StaticMagnitude: 1m);
        var two = new InterpolatedTypedConstant(
            [new TypedInterpolationSlot(Hole, InterpolationSlotKind.Currency)],
            TypeKind.Money, default, StaticMagnitude: 2m);

        WpCalculator.Canonicalize(one).Key
            .Should().NotBe(WpCalculator.Canonicalize(two).Key);
    }
}
