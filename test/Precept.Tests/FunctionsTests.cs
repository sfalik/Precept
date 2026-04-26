using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class FunctionsTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_ReturnsForEveryFunctionKind()
    {
        foreach (var kind in Enum.GetValues<FunctionKind>())
        {
            var meta = Functions.GetMeta(kind);
            meta.Kind.Should().Be(kind);
            meta.Name.Should().NotBeNullOrEmpty($"{kind} must have a name");
            meta.Description.Should().NotBeNullOrEmpty($"{kind} must have a description");
            meta.Overloads.Should().NotBeEmpty($"{kind} must have at least one overload");
        }
    }

    [Fact]
    public void All_ContainsEveryKindExactlyOnce()
    {
        var expected = Enum.GetValues<FunctionKind>().ToHashSet();
        var actual = Functions.All.Select(m => m.Kind).ToHashSet();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void All_IsInDeclarationOrder()
    {
        var kinds = Functions.All.Select(m => (int)m.Kind).ToList();
        kinds.Should().BeInAscendingOrder();
    }

    // ── Count invariants ────────────────────────────────────────────────────────

    [Fact]
    public void Total_Count()
    {
        // 12 numeric + 8 string + 1 temporal = 21
        Functions.All.Should().HaveCount(21);
    }

    [Fact]
    public void Total_OverloadCount()
    {
        // See catalog-system.md § 3 and spec § 3.7 for the derivation:
        // min(5) + max(5) + abs(5) + clamp(5) + floor(2) + ceil(2) +
        // truncate(2) + round(2) + roundPlaces(5) + approximate(1) +
        // pow(3) + sqrt(3) + trim(1) + startsWith(1) + endsWith(1) +
        // toLower(1) + toUpper(1) + left(1) + right(1) + mid(1) + now(1) = 49
        Functions.All.Sum(m => m.Overloads.Count).Should().Be(49);
    }

    // ── Every overload references valid types ───────────────────────────────────

    [Fact]
    public void AllOverloads_ReferenceValidTypeKinds()
    {
        var validTypes = new HashSet<TypeKind>(Enum.GetValues<TypeKind>());
        foreach (var meta in Functions.All)
        {
            foreach (var overload in meta.Overloads)
            {
                validTypes.Should().Contain(overload.ReturnType,
                    $"{meta.Kind} overload has invalid ReturnType {overload.ReturnType}");
                foreach (var param in overload.Parameters)
                {
                    validTypes.Should().Contain(param.Kind,
                        $"{meta.Kind} overload has invalid parameter TypeKind {param.Kind}");
                }
            }
        }
    }

    // ── ByName index ────────────────────────────────────────────────────────────

    [Fact]
    public void ByName_ContainsAllUniqueNames()
    {
        var expectedNames = Functions.All.Select(m => m.Name).Distinct().ToHashSet();
        Functions.ByName.Keys.ToHashSet().Should().BeEquivalentTo(expectedNames);
    }

    [Fact]
    public void ByName_RoundMapsToTwoEntries()
    {
        // Round and RoundPlaces are separate FunctionKind members with the same Name
        Functions.ByName["round"].Should().HaveCount(2);
        Functions.ByName["round"].Select(m => m.Kind)
            .Should().BeEquivalentTo([FunctionKind.Round, FunctionKind.RoundPlaces]);
    }

    [Fact]
    public void ByName_UniqueNames()
    {
        // 21 FunctionKind members, but "round" appears twice → 20 unique names
        Functions.ByName.Should().HaveCount(20);
    }

    [Fact]
    public void FindByName_KnownFunction_ReturnsEntries()
    {
        Functions.FindByName("min").Length.Should().Be(1);
        Functions.FindByName("round").Length.Should().Be(2);
    }

    [Fact]
    public void FindByName_UnknownFunction_ReturnsEmpty()
    {
        Functions.FindByName("doesNotExist").Length.Should().Be(0);
    }

    // ── Names are lowercase identifiers ─────────────────────────────────────────

    [Fact]
    public void Names_AreLowerCamelCase()
    {
        foreach (var meta in Functions.All)
        {
            meta.Name.Should().MatchRegex(@"^[a-z][a-zA-Z]*$",
                $"{meta.Kind} name '{meta.Name}' should be lowerCamelCase");
        }
    }

    // ── Numeric function overloads ──────────────────────────────────────────────

    [Theory]
    [InlineData(FunctionKind.Min, 5)]
    [InlineData(FunctionKind.Max, 5)]
    [InlineData(FunctionKind.Abs, 5)]
    [InlineData(FunctionKind.Clamp, 5)]
    [InlineData(FunctionKind.Floor, 2)]
    [InlineData(FunctionKind.Ceil, 2)]
    [InlineData(FunctionKind.Truncate, 2)]
    [InlineData(FunctionKind.Round, 2)]
    [InlineData(FunctionKind.RoundPlaces, 5)]
    [InlineData(FunctionKind.Approximate, 1)]
    [InlineData(FunctionKind.Pow, 3)]
    [InlineData(FunctionKind.Sqrt, 3)]
    public void NumericFunction_OverloadCount(FunctionKind kind, int expectedCount)
    {
        Functions.GetMeta(kind).Overloads.Should().HaveCount(expectedCount);
    }

    // ── Per-function arity checks ───────────────────────────────────────────────

    [Theory]
    [InlineData(FunctionKind.Min, 2)]
    [InlineData(FunctionKind.Max, 2)]
    [InlineData(FunctionKind.Abs, 1)]
    [InlineData(FunctionKind.Clamp, 3)]
    [InlineData(FunctionKind.Floor, 1)]
    [InlineData(FunctionKind.Ceil, 1)]
    [InlineData(FunctionKind.Truncate, 1)]
    [InlineData(FunctionKind.Round, 1)]
    [InlineData(FunctionKind.RoundPlaces, 2)]
    [InlineData(FunctionKind.Approximate, 1)]
    [InlineData(FunctionKind.Pow, 2)]
    [InlineData(FunctionKind.Sqrt, 1)]
    [InlineData(FunctionKind.Trim, 1)]
    [InlineData(FunctionKind.StartsWith, 2)]
    [InlineData(FunctionKind.EndsWith, 2)]
    [InlineData(FunctionKind.ToLower, 1)]
    [InlineData(FunctionKind.ToUpper, 1)]
    [InlineData(FunctionKind.Left, 2)]
    [InlineData(FunctionKind.Right, 2)]
    [InlineData(FunctionKind.Mid, 3)]
    [InlineData(FunctionKind.Now, 0)]
    public void AllOverloads_HaveConsistentArity(FunctionKind kind, int expectedArity)
    {
        var meta = Functions.GetMeta(kind);
        foreach (var overload in meta.Overloads)
        {
            overload.Parameters.Count.Should().Be(expectedArity,
                $"{kind} overloads should all have arity {expectedArity}");
        }
    }

    // ── Business-type overloads with QualifierMatch.Same ────────────────────────

    [Theory]
    [InlineData(FunctionKind.Min)]
    [InlineData(FunctionKind.Max)]
    [InlineData(FunctionKind.Abs)]
    [InlineData(FunctionKind.Clamp)]
    [InlineData(FunctionKind.RoundPlaces)]
    public void BusinessTypeOverloads_HaveQualifierMatchSame(FunctionKind kind)
    {
        var meta = Functions.GetMeta(kind);
        var businessOverloads = meta.Overloads
            .Where(o => o.Match == QualifierMatch.Same)
            .ToList();

        // Should have exactly 2 business-type overloads: money and quantity
        businessOverloads.Should().HaveCount(2,
            $"{kind} should have money and quantity overloads with QualifierMatch.Same");

        businessOverloads.Select(o => o.ReturnType).Should()
            .BeEquivalentTo([TypeKind.Money, TypeKind.Quantity]);
    }

    [Theory]
    [InlineData(FunctionKind.Floor)]
    [InlineData(FunctionKind.Ceil)]
    [InlineData(FunctionKind.Truncate)]
    [InlineData(FunctionKind.Round)]
    [InlineData(FunctionKind.Approximate)]
    [InlineData(FunctionKind.Pow)]
    [InlineData(FunctionKind.Sqrt)]
    [InlineData(FunctionKind.Trim)]
    [InlineData(FunctionKind.StartsWith)]
    [InlineData(FunctionKind.EndsWith)]
    [InlineData(FunctionKind.ToLower)]
    [InlineData(FunctionKind.ToUpper)]
    [InlineData(FunctionKind.Left)]
    [InlineData(FunctionKind.Right)]
    [InlineData(FunctionKind.Mid)]
    [InlineData(FunctionKind.Now)]
    public void NoBusinessTypeOverloads_NoQualifierMatch(FunctionKind kind)
    {
        var meta = Functions.GetMeta(kind);
        meta.Overloads.All(o => o.Match == null).Should().BeTrue(
            $"{kind} should have no QualifierMatch on any overload");
    }

    // ── Rounding family: decimal|number → integer ───────────────────────────────

    [Theory]
    [InlineData(FunctionKind.Floor)]
    [InlineData(FunctionKind.Ceil)]
    [InlineData(FunctionKind.Truncate)]
    [InlineData(FunctionKind.Round)]
    public void RoundingFamily_OnlyDecimalAndNumber(FunctionKind kind)
    {
        var meta = Functions.GetMeta(kind);
        meta.Overloads.Should().HaveCount(2);

        var inputTypes = meta.Overloads.Select(o => o.Parameters[0].Kind).ToHashSet();
        inputTypes.Should().BeEquivalentTo([TypeKind.Decimal, TypeKind.Number]);

        meta.Overloads.All(o => o.ReturnType == TypeKind.Integer).Should().BeTrue(
            $"{kind} should always return integer");
    }

    // ── Lane bridge functions ───────────────────────────────────────────────────

    [Fact]
    public void Approximate_IsDecimalToNumber()
    {
        var meta = Functions.GetMeta(FunctionKind.Approximate);
        meta.Overloads.Should().HaveCount(1);
        meta.Overloads[0].Parameters.Should().HaveCount(1);
        meta.Overloads[0].Parameters[0].Kind.Should().Be(TypeKind.Decimal);
        meta.Overloads[0].ReturnType.Should().Be(TypeKind.Number);
    }

    [Fact]
    public void RoundPlaces_NumericOverloads_ReturnDecimal()
    {
        var meta = Functions.GetMeta(FunctionKind.RoundPlaces);
        var numericOverloads = meta.Overloads
            .Where(o => o.Match == null)
            .ToList();

        numericOverloads.Should().HaveCount(3);
        numericOverloads.All(o => o.ReturnType == TypeKind.Decimal).Should().BeTrue(
            "round(value, places) → decimal for numeric types");
        numericOverloads.All(o => o.Parameters[1].Kind == TypeKind.Integer).Should().BeTrue(
            "places parameter must be integer");
    }

    // ── Pow: same type as base ──────────────────────────────────────────────────

    [Fact]
    public void Pow_ReturnsSameTypeAsBase()
    {
        var meta = Functions.GetMeta(FunctionKind.Pow);
        foreach (var overload in meta.Overloads)
        {
            overload.ReturnType.Should().Be(overload.Parameters[0].Kind,
                $"pow({overload.Parameters[0].Kind}, integer) should return {overload.Parameters[0].Kind}");
            overload.Parameters[1].Kind.Should().Be(TypeKind.Integer,
                "exponent must always be integer");
        }
    }

    // ── Sqrt: always returns number ─────────────────────────────────────────────

    [Fact]
    public void Sqrt_AlwaysReturnsNumber()
    {
        var meta = Functions.GetMeta(FunctionKind.Sqrt);
        meta.Overloads.All(o => o.ReturnType == TypeKind.Number).Should().BeTrue(
            "sqrt always returns number");
    }

    // ── String functions ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(FunctionKind.Trim)]
    [InlineData(FunctionKind.ToLower)]
    [InlineData(FunctionKind.ToUpper)]
    public void StringUnary_StringToString(FunctionKind kind)
    {
        var meta = Functions.GetMeta(kind);
        meta.Overloads.Should().HaveCount(1);
        meta.Overloads[0].Parameters.Should().HaveCount(1);
        meta.Overloads[0].Parameters[0].Kind.Should().Be(TypeKind.String);
        meta.Overloads[0].ReturnType.Should().Be(TypeKind.String);
    }

    [Theory]
    [InlineData(FunctionKind.StartsWith)]
    [InlineData(FunctionKind.EndsWith)]
    public void StringPredicate_TwoStringsToBoolean(FunctionKind kind)
    {
        var meta = Functions.GetMeta(kind);
        meta.Overloads.Should().HaveCount(1);
        meta.Overloads[0].Parameters.Should().HaveCount(2);
        meta.Overloads[0].Parameters[0].Kind.Should().Be(TypeKind.String);
        meta.Overloads[0].Parameters[1].Kind.Should().Be(TypeKind.String);
        meta.Overloads[0].ReturnType.Should().Be(TypeKind.Boolean);
    }

    [Theory]
    [InlineData(FunctionKind.Left)]
    [InlineData(FunctionKind.Right)]
    public void LeftRight_StringAndIntegerToString(FunctionKind kind)
    {
        var meta = Functions.GetMeta(kind);
        meta.Overloads.Should().HaveCount(1);
        meta.Overloads[0].Parameters.Should().HaveCount(2);
        meta.Overloads[0].Parameters[0].Kind.Should().Be(TypeKind.String);
        meta.Overloads[0].Parameters[1].Kind.Should().Be(TypeKind.Integer);
        meta.Overloads[0].ReturnType.Should().Be(TypeKind.String);
    }

    [Fact]
    public void Mid_StringAndTwoIntegersToString()
    {
        var meta = Functions.GetMeta(FunctionKind.Mid);
        meta.Overloads.Should().HaveCount(1);
        var overload = meta.Overloads[0];
        overload.Parameters.Should().HaveCount(3);
        overload.Parameters[0].Kind.Should().Be(TypeKind.String);
        overload.Parameters[1].Kind.Should().Be(TypeKind.Integer);
        overload.Parameters[2].Kind.Should().Be(TypeKind.Integer);
        overload.ReturnType.Should().Be(TypeKind.String);
    }

    // ── Temporal ────────────────────────────────────────────────────────────────

    [Fact]
    public void Now_ZeroArgsReturnsInstant()
    {
        var meta = Functions.GetMeta(FunctionKind.Now);
        meta.Overloads.Should().HaveCount(1);
        meta.Overloads[0].Parameters.Should().BeEmpty();
        meta.Overloads[0].ReturnType.Should().Be(TypeKind.Instant);
    }

    // ── Abs: same type as input for pure numeric ────────────────────────────────

    [Fact]
    public void Abs_NumericOverloads_ReturnSameType()
    {
        var meta = Functions.GetMeta(FunctionKind.Abs);
        var numericOverloads = meta.Overloads
            .Where(o => o.Match == null)
            .ToList();

        numericOverloads.Should().HaveCount(3);
        foreach (var overload in numericOverloads)
        {
            overload.ReturnType.Should().Be(overload.Parameters[0].Kind,
                $"abs({overload.Parameters[0].Kind}) should return {overload.Parameters[0].Kind}");
        }
    }

    // ── Min/Max: same type as inputs for pure numeric ───────────────────────────

    [Theory]
    [InlineData(FunctionKind.Min)]
    [InlineData(FunctionKind.Max)]
    public void MinMax_NumericOverloads_ReturnSameType(FunctionKind kind)
    {
        var meta = Functions.GetMeta(kind);
        var numericOverloads = meta.Overloads
            .Where(o => o.Match == null)
            .ToList();

        numericOverloads.Should().HaveCount(3);
        foreach (var overload in numericOverloads)
        {
            overload.ReturnType.Should().Be(overload.Parameters[0].Kind,
                $"{kind}({overload.Parameters[0].Kind}, ...) should return {overload.Parameters[0].Kind}");
            overload.Parameters[1].Kind.Should().Be(overload.Parameters[0].Kind,
                $"{kind} overload params should be same type");
        }
    }

    // ── Spec constraints (negative) ─────────────────────────────────────────────

    [Fact]
    public void Floor_NoIntegerOverload()
    {
        var meta = Functions.GetMeta(FunctionKind.Floor);
        meta.Overloads.Should().NotContain(
            o => o.Parameters.Any(p => p.Kind == TypeKind.Integer),
            "floor(integer) is redundant — integers are already integers");
    }

    [Fact]
    public void Round_NoIntegerOverload()
    {
        var meta = Functions.GetMeta(FunctionKind.Round);
        meta.Overloads.Should().NotContain(
            o => o.Parameters.Any(p => p.Kind == TypeKind.Integer),
            "round(integer) is redundant — integers are already integers");
    }

    [Fact]
    public void Approximate_OnlyDecimal_NotNumberOrInteger()
    {
        var meta = Functions.GetMeta(FunctionKind.Approximate);
        meta.Overloads.Should().HaveCount(1, "approximate is decimal → number only");
        meta.Overloads[0].Parameters[0].Kind.Should().Be(TypeKind.Decimal);
    }

    // ── QualifierMatch total count ──────────────────────────────────────────────

    [Fact]
    public void QualifierMatch_TotalSameCount()
    {
        // 5 functions × 2 business types (money, quantity) = 10
        var count = Functions.All.SelectMany(m => m.Overloads)
            .Count(o => o.Match == QualifierMatch.Same);
        count.Should().Be(10);
    }

    // ── ProofRequirements ─ default empty ────────────────────────────────────────

    [Fact]
    public void AllOverloads_ProofRequirements_IsNotNull()
    {
        foreach (var fn in Functions.All)
        {
            foreach (var overload in fn.Overloads)
            {
                overload.ProofRequirements.Should().NotBeNull(
                    $"{fn.Name} overload ProofRequirements should default to empty");
            }
        }
    }

    // M2 ── Sqrt proof requirements ────────────────────────────────────────────

    [Theory]
    [InlineData(0)] // integer overload
    [InlineData(1)] // decimal overload
    [InlineData(2)] // number overload
    public void Sqrt_AllOverloads_HaveNonNegativeRequirement(int overloadIndex)
    {
        var meta = Functions.GetMeta(FunctionKind.Sqrt);
        var overload = meta.Overloads[overloadIndex];
        overload.ProofRequirements.Should().HaveCount(1,
            $"sqrt overload[{overloadIndex}] operand must be proven >= 0");
        var req = overload.ProofRequirements[0].Should().BeOfType<NumericProofRequirement>().Subject;
        req.Comparison.Should().Be(OperatorKind.GreaterThanOrEqual,
            $"sqrt overload[{overloadIndex}] requires operand >= 0");
        req.Threshold.Should().Be(0);
    }

    [Fact]
    public void Sqrt_RequirementCount_IsOnePerOverload()
    {
        var meta = Functions.GetMeta(FunctionKind.Sqrt);
        meta.Overloads.Should().HaveCount(3, "sqrt has 3 overloads");
        foreach (var overload in meta.Overloads)
        {
            overload.ProofRequirements.Should().HaveCount(1,
                $"sqrt overload for {overload.Parameters[0].Kind} must have exactly 1 proof requirement");
        }
    }

    // M10 ── ParameterMeta.Name ────────────────────────────────────────────────

    [Theory]
    [InlineData(FunctionKind.Clamp)]
    [InlineData(FunctionKind.Mid)]
    public void MultiParamFunctions_HaveNamedParameters(FunctionKind kind)
    {
        var meta = Functions.GetMeta(kind);
        foreach (var overload in meta.Overloads)
        {
            foreach (var param in overload.Parameters)
            {
                param.Name.Should().NotBeNullOrEmpty(
                    $"{kind} parameter should have a non-null Name");
            }
        }
    }

    [Fact]
    public void Clamp_ParameterNames_Are_Value_Lo_Hi()
    {
        var meta = Functions.GetMeta(FunctionKind.Clamp);
        var intOverload = meta.Overloads.First(o =>
            o.Parameters[0].Kind == TypeKind.Integer && o.Match == null);
        intOverload.Parameters[0].Name.Should().Be("value");
        intOverload.Parameters[1].Name.Should().Be("lo");
        intOverload.Parameters[2].Name.Should().Be("hi");
    }

    [Fact]
    public void Left_ParameterNames_Are_Str_N()
    {
        var meta = Functions.GetMeta(FunctionKind.Left);
        var overload = meta.Overloads[0];
        overload.Parameters[0].Name.Should().Be("str");
        overload.Parameters[1].Name.Should().Be("n");
    }

    [Fact]
    public void Sqrt_ParameterNames_Are_Value()
    {
        var meta = Functions.GetMeta(FunctionKind.Sqrt);
        foreach (var overload in meta.Overloads)
        {
            overload.Parameters[0].Name.Should().Be("value",
                $"sqrt({overload.Parameters[0].Kind}) parameter should be named 'value'");
        }
    }
}
