using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class ProofRequirementCatalogTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_ReturnsForEveryProofRequirementKind()
    {
        foreach (var kind in Enum.GetValues<ProofRequirementKind>())
        {
            var meta = ProofRequirements.GetMeta(kind);
            meta.Kind.Should().Be(kind);
            meta.Description.Should().NotBeNullOrEmpty($"{kind} must have a description");
        }
    }

    [Fact]
    public void All_ContainsEveryKindExactlyOnce()
    {
        var expected = Enum.GetValues<ProofRequirementKind>().ToHashSet();
        var actual = ProofRequirements.All.Select(m => m.Kind).ToHashSet();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void All_IsInDeclarationOrder()
    {
        var kinds = ProofRequirements.All.Select(m => (int)m.Kind).ToList();
        kinds.Should().BeInAscendingOrder();
    }

    // ── Count invariant ─────────────────────────────────────────────────────────

    [Fact]
    public void Total_Count()
    {
        ProofRequirements.All.Should().HaveCount(5);
    }

    // ── DU subtype correctness ──────────────────────────────────────────────────

    [Fact]
    public void Numeric_IsNumericSubtype()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.Numeric)
            .Should().BeOfType<ProofRequirementMeta.Numeric>();
    }

    [Fact]
    public void Presence_IsPresenceSubtype()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.Presence)
            .Should().BeOfType<ProofRequirementMeta.Presence>();
    }

    [Fact]
    public void Dimension_IsDimensionSubtype()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.Dimension)
            .Should().BeOfType<ProofRequirementMeta.Dimension>();
    }

    [Fact]
    public void Modifier_IsModifierSubtype()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.Modifier)
            .Should().BeOfType<ProofRequirementMeta.Modifier>();
    }

    [Fact]
    public void QualifierCompatibility_IsQualifierCompatibilitySubtype()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.QualifierCompatibility)
            .Should().BeOfType<ProofRequirementMeta.QualifierCompatibility>();
    }

    // ── QualifierCompatibility is the only dual-subject kind ───────────────────

    [Fact]
    public void OnlyQualifierCompatibility_IsDualSubject()
    {
        var dual = ProofRequirements.All
            .OfType<ProofRequirementMeta.QualifierCompatibility>()
            .ToList();
        dual.Should().HaveCount(1);
        dual[0].Kind.Should().Be(ProofRequirementKind.QualifierCompatibility);
    }

    [Fact]
    public void FourKinds_AreSingleSubject()
    {
        var singleSubject = ProofRequirements.All
            .Where(m => m is not ProofRequirementMeta.QualifierCompatibility)
            .ToList();
        singleSubject.Should().HaveCount(4);
    }

    // ── Instance Kind property matches catalog ──────────────────────────────────

    [Fact]
    public void NumericProofRequirement_KindIsNumeric()
    {
        var req = new NumericProofRequirement(
            new SelfSubject(), OperatorKind.GreaterThan, 0m, "test");
        req.Kind.Should().Be(ProofRequirementKind.Numeric);
    }

    [Fact]
    public void PresenceProofRequirement_KindIsPresence()
    {
        var req = new PresenceProofRequirement(new SelfSubject(), "test");
        req.Kind.Should().Be(ProofRequirementKind.Presence);
    }

    [Fact]
    public void DimensionProofRequirement_KindIsDimension()
    {
        var param = new ParameterMeta(TypeKind.Period);
        var req = new DimensionProofRequirement(
            new ParamSubject(param), PeriodDimension.Date, "test");
        req.Kind.Should().Be(ProofRequirementKind.Dimension);
    }

    [Fact]
    public void ModifierRequirement_KindIsModifier()
    {
        var param = new ParameterMeta(TypeKind.Integer);
        var req = new ModifierRequirement(
            new ParamSubject(param), ModifierKind.Ordered, "test");
        req.Kind.Should().Be(ProofRequirementKind.Modifier);
    }

    [Fact]
    public void QualifierCompatibilityProofRequirement_KindIsQualifierCompatibility()
    {
        var left  = new ParameterMeta(TypeKind.Money);
        var right = new ParameterMeta(TypeKind.Money);
        var req = new QualifierCompatibilityProofRequirement(
            new ParamSubject(left), new ParamSubject(right),
            QualifierAxis.Currency, "test");
        req.Kind.Should().Be(ProofRequirementKind.QualifierCompatibility);
    }

    // ── Dual-subject shape for QualifierCompatibility ──────────────────────────

    [Fact]
    public void QualifierCompatibility_HasDistinctLeftAndRightSubjects()
    {
        var left  = new ParameterMeta(TypeKind.Money);
        var right = new ParameterMeta(TypeKind.Money);
        var leftSubject  = new ParamSubject(left);
        var rightSubject = new ParamSubject(right);

        var req = new QualifierCompatibilityProofRequirement(
            leftSubject, rightSubject, QualifierAxis.Currency, "test");

        req.LeftSubject.Should().BeSameAs(leftSubject);
        req.RightSubject.Should().BeSameAs(rightSubject);
    }
}
