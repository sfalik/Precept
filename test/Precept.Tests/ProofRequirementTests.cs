using System;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class ProofRequirementTests
{
    // ── Shared test fixtures ────────────────────────────────────────────────────

    private static readonly ParameterMeta TestParam = new(TypeKind.Integer);
    private static readonly TypeAccessor TestAccessor = new("count", "Element count");

    // ── ProofSubject DU ─────────────────────────────────────────────────────────

    [Fact]
    public void ParamSubject_HoldsParameterReference()
    {
        var subject = new ParamSubject(TestParam);
        subject.Parameter.Should().BeSameAs(TestParam);
    }

    [Fact]
    public void SelfSubject_DefaultAccessorIsNull()
    {
        var subject = new SelfSubject();
        subject.Accessor.Should().BeNull();
    }

    [Fact]
    public void SelfSubject_CanHoldAccessorReference()
    {
        var subject = new SelfSubject(TestAccessor);
        subject.Accessor.Should().BeSameAs(TestAccessor);
    }

    // ── NumericProofRequirement ─────────────────────────────────────────────────

    [Fact]
    public void NumericProof_DivisorSafety()
    {
        var req = new NumericProofRequirement(
            new ParamSubject(TestParam),
            OperatorKind.NotEquals,
            0m,
            "Divisor must not be zero");

        req.Subject.Should().BeOfType<ParamSubject>();
        req.Comparison.Should().Be(OperatorKind.NotEquals);
        req.Threshold.Should().Be(0m);
        req.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NumericProof_CollectionNonEmpty()
    {
        var countAccessor = new TypeAccessor("count", "Element count");
        var req = new NumericProofRequirement(
            new SelfSubject(countAccessor),
            OperatorKind.GreaterThan,
            0m,
            "Collection must not be empty");

        req.Subject.Should().BeOfType<SelfSubject>();
        ((SelfSubject)req.Subject).Accessor.Should().NotBeNull();
        req.Comparison.Should().Be(OperatorKind.GreaterThan);
        req.Threshold.Should().Be(0m);
    }

    [Fact]
    public void NumericProof_SqrtNonNegative()
    {
        var input = new ParameterMeta(TypeKind.Decimal);
        var req = new NumericProofRequirement(
            new ParamSubject(input),
            OperatorKind.GreaterThanOrEqual,
            0m,
            "sqrt operand must be non-negative");

        req.Comparison.Should().Be(OperatorKind.GreaterThanOrEqual);
        req.Threshold.Should().Be(0m);
    }

    // ── PresenceProofRequirement ────────────────────────────────────────────────

    [Fact]
    public void PresenceProof_OptionalFieldAccess()
    {
        var req = new PresenceProofRequirement(
            new SelfSubject(),
            "Optional field must be set before access");

        req.Subject.Should().BeOfType<SelfSubject>();
        ((SelfSubject)req.Subject).Accessor.Should().BeNull("null = the field itself");
        req.Description.Should().NotBeNullOrEmpty();
    }

    // ── DimensionProofRequirement ───────────────────────────────────────────────

    [Fact]
    public void DimensionProof_DateDimension()
    {
        var period = new ParameterMeta(TypeKind.Period);
        var req = new DimensionProofRequirement(
            new ParamSubject(period),
            PeriodDimension.Date,
            "Period must have date dimension for date arithmetic");

        req.RequiredDimension.Should().Be(PeriodDimension.Date);
        req.Subject.Should().BeOfType<ParamSubject>();
    }

    [Fact]
    public void DimensionProof_TimeDimension()
    {
        var period = new ParameterMeta(TypeKind.Period);
        var req = new DimensionProofRequirement(
            new ParamSubject(period),
            PeriodDimension.Time,
            "Period must have time dimension for time arithmetic");

        req.RequiredDimension.Should().Be(PeriodDimension.Time);
    }

    // ── PeriodDimension enum ────────────────────────────────────────────────────

    [Fact]
    public void PeriodDimension_HasThreeValues()
    {
        Enum.GetValues<PeriodDimension>().Should().HaveCount(3);
    }

    // ── DU completeness ─────────────────────────────────────────────────────────

    [Fact]
    public void FiveProofRequirementSubtypes()
    {
        // Verify all five concrete subtypes exist and are distinct
        ProofRequirement numeric  = new NumericProofRequirement(
            new ParamSubject(TestParam), OperatorKind.GreaterThan, 0m, "test");
        ProofRequirement presence = new PresenceProofRequirement(
            new SelfSubject(), "test");
        ProofRequirement dimension = new DimensionProofRequirement(
            new ParamSubject(TestParam), PeriodDimension.Any, "test");
        ProofRequirement qualifierCompat = new QualifierCompatibilityProofRequirement(
            new ParamSubject(TestParam), new ParamSubject(TestParam),
            QualifierAxis.Currency, "test");
        ProofRequirement modifier = new ModifierRequirement(
            new ParamSubject(TestParam), ModifierKind.Ordered, "test");

        numeric.Should().BeOfType<NumericProofRequirement>();
        presence.Should().BeOfType<PresenceProofRequirement>();
        dimension.Should().BeOfType<DimensionProofRequirement>();
        qualifierCompat.Should().BeOfType<QualifierCompatibilityProofRequirement>();
        modifier.Should().BeOfType<ModifierRequirement>();
    }

    // ── Kind property on base ───────────────────────────────────────────────────

    [Fact]
    public void BaseKind_IsAccessibleWithoutPatternMatch()
    {
        ProofRequirement req = new NumericProofRequirement(
            new ParamSubject(TestParam), OperatorKind.GreaterThan, 0m, "test");
        req.Kind.Should().Be(ProofRequirementKind.Numeric);
    }

    [Fact]
    public void TwoProofSubjectSubtypes()
    {
        ProofSubject param = new ParamSubject(TestParam);
        ProofSubject self = new SelfSubject();

        param.Should().BeOfType<ParamSubject>();
        self.Should().BeOfType<SelfSubject>();
    }

    // ── Equality semantics ──────────────────────────────────────────────────────

    [Fact]
    public void ParamSubject_EqualityByReference()
    {
        // Two ParamSubjects referencing the same ParameterMeta should be equal
        var a = new ParamSubject(TestParam);
        var b = new ParamSubject(TestParam);
        a.Should().Be(b);
    }

    [Fact]
    public void ParamSubject_DifferentParams_NotEqual()
    {
        var paramA = new ParameterMeta(TypeKind.Integer);
        var paramB = new ParameterMeta(TypeKind.Decimal);
        var a = new ParamSubject(paramA);
        var b = new ParamSubject(paramB);
        a.Should().NotBe(b);
    }

    [Fact]
    public void NumericProof_ComparisonUsesOperatorKind()
    {
        // Verify OperatorKind is reused — no parallel enum
        var req = new NumericProofRequirement(
            new ParamSubject(TestParam),
            OperatorKind.NotEquals,
            0m,
            "test");

        req.Comparison.Should().BeOneOf(
            OperatorKind.Equals, OperatorKind.NotEquals,
            OperatorKind.LessThan, OperatorKind.LessThanOrEqual,
            OperatorKind.GreaterThan, OperatorKind.GreaterThanOrEqual);
    }

    [Fact]
    public void ProofSatisfaction_Numeric_HoldsProjectionComparisonBound()
    {
        var projection = new SatisfactionProjection.Accessor("length");
        var bound = new NumericBoundSource.Constant(0m);
        var satisfaction = new ProofSatisfaction.Numeric(projection, OperatorKind.GreaterThan, bound);

        satisfaction.RequirementKind.Should().Be(ProofRequirementKind.Numeric);
        satisfaction.Projection.Should().BeSameAs(projection);
        satisfaction.Comparison.Should().Be(OperatorKind.GreaterThan);
        satisfaction.Bound.Should().BeSameAs(bound);
    }

    [Fact]
    public void ProofSatisfaction_Presence_HasPresenceRequirementKind()
    {
        var satisfaction = new ProofSatisfaction.Presence();

        satisfaction.RequirementKind.Should().Be(ProofRequirementKind.Presence);
    }

    [Fact]
    public void ProofSatisfaction_Dimension_HoldsSource()
    {
        var source = new DimensionSource.Constant(PeriodDimension.Date);
        var satisfaction = new ProofSatisfaction.Dimension(source);

        satisfaction.RequirementKind.Should().Be(ProofRequirementKind.Dimension);
        satisfaction.Source.Should().BeSameAs(source);
    }

    [Fact]
    public void ProofSatisfaction_Modifier_HoldsRequiredModifier()
    {
        var satisfaction = new ProofSatisfaction.Modifier(ModifierKind.Ordered);

        satisfaction.RequirementKind.Should().Be(ProofRequirementKind.Modifier);
        satisfaction.RequiredModifier.Should().Be(ModifierKind.Ordered);
    }

    [Fact]
    public void ProofSatisfaction_QualifierCompatibility_HoldsAxis()
    {
        var satisfaction = new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Currency);

        satisfaction.RequirementKind.Should().Be(ProofRequirementKind.QualifierCompatibility);
        satisfaction.Axis.Should().Be(QualifierAxis.Currency);
    }

    [Fact]
    public void SatisfactionProjection_SelfValue_IsDistinctFromAccessor()
    {
        SatisfactionProjection projection = new SatisfactionProjection.SelfValue();

        projection.Should().NotBeOfType<SatisfactionProjection.Accessor>();
    }

    [Fact]
    public void SatisfactionProjection_Accessor_HoldsName()
    {
        var projection = new SatisfactionProjection.Accessor("count");

        projection.Name.Should().Be("count");
    }

    [Fact]
    public void NumericBoundSource_Constant_HoldsValue()
    {
        var bound = new NumericBoundSource.Constant(12.5m);

        bound.Value.Should().Be(12.5m);
    }

    [Fact]
    public void NumericBoundSource_DeclarationValue_IsDistinct()
    {
        NumericBoundSource bound = new NumericBoundSource.DeclarationValue();

        bound.Should().NotBeOfType<NumericBoundSource.Constant>();
    }

    [Fact]
    public void DimensionSource_Constant_HoldsValue()
    {
        var source = new DimensionSource.Constant(PeriodDimension.Time);

        source.Value.Should().Be(PeriodDimension.Time);
    }

    [Fact]
    public void DimensionSource_DeclaredTemporalDimension_IsDistinct()
    {
        DimensionSource source = new DimensionSource.DeclaredTemporalDimension();

        source.Should().NotBeOfType<DimensionSource.Constant>();
    }
}
