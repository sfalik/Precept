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
        // DimensionalProduct = 12; AssignmentQualifier = 13 (open-field assignment-qualifier
        // discharge relocated to the proof stage) brings the catalog to 13.
        ProofRequirements.All.Should().HaveCount(13);
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
    public void DualSubjectKinds_AreQualifierCompatibilityAndChain()
    {
        var dual = ProofRequirements.All
            .Where(m => m is ProofRequirementMeta.QualifierCompatibility
                        or ProofRequirementMeta.QualifierChain)
            .ToList();
        dual.Should().HaveCount(2);
        dual.Select(d => d.Kind).Should().BeEquivalentTo(new[]
        {
            ProofRequirementKind.QualifierCompatibility,
            ProofRequirementKind.QualifierChain,
        });
    }

    [Fact]
    public void FiveKinds_AreSingleSubject()
    {
        // DimensionalProduct is dual-subject (left and right operand of the multiplicative op),
        // so the single-subject group is everything else. AssignmentQualifier (single-subject:
        // the assigned source) brings it to 10.
        var singleSubject = ProofRequirements.All
            .Where(m => m is not ProofRequirementMeta.QualifierCompatibility
                        and not ProofRequirementMeta.QualifierChain
                        and not ProofRequirementMeta.DimensionalProduct)
            .ToList();
        singleSubject.Should().HaveCount(10);
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

    // ── Regression anchors — IntervalContainment catalog entry ──────────

    [Fact]
    public void IntervalContainment_KindExistsInEnum()
    {
        // IntervalContainment enum value must be 7.
        var kind = ProofRequirementKind.IntervalContainment;
        ((int)kind).Should().Be(7);
    }

    [Fact]
    public void IntervalContainment_IsIntervalContainmentSubtype()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment)
            .Should().BeOfType<ProofRequirementMeta.IntervalContainment>();
    }

    [Fact]
    public void IntervalContainmentProofRequirement_KindIsIntervalContainment()
    {
        var req = new IntervalContainmentProofRequirement(
            Subject:     new SelfSubject(),
            TargetField: "balance",
            DeclaredMin: 0m,
            DeclaredMax: 999_999m,
            AuthoredMin: 0m,
            AuthoredMax: 999_999m,
            Description: "balance must stay within [0 .. 999 999]");
        req.Kind.Should().Be(ProofRequirementKind.IntervalContainment);
    }

    [Fact]
    public void SingleSubjectKinds_NowIncludesIntervalContainment()
    {
        // Single-subject kinds total 10 (DimensionalProduct is dual-subject):
        // Numeric, Presence, Dimension, Modifier, IntervalContainment, LengthContainment,
        // CountContainment, KeyPresence, IndexBounds, AssignmentQualifier.
        var singleSubject = ProofRequirements.All
            .Where(m => m is not ProofRequirementMeta.QualifierCompatibility
                        and not ProofRequirementMeta.QualifierChain
                        and not ProofRequirementMeta.DimensionalProduct)
            .ToList();
        singleSubject.Should().HaveCount(10,
            "AssignmentQualifier joins the existing single-subject kinds");
    }

    // ── Catalog-mediated DiagnosticCode ──────────────────────────────

    [Fact]
    public void IntervalContainment_DiagnosticCode_IsNumericOverflow()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment)
            .DiagnosticCode.Should().Be(DiagnosticCode.NumericOverflow);
    }

    [Fact]
    public void LengthContainment_DiagnosticCode_IsLengthBoundViolation()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.LengthContainment)
            .DiagnosticCode.Should().Be(DiagnosticCode.LengthBoundViolation);
    }

    [Fact]
    public void CountContainment_DiagnosticCode_IsCountBoundViolation()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.CountContainment)
            .DiagnosticCode.Should().Be(DiagnosticCode.CountBoundViolation);
    }

    [Fact]
    public void Presence_DiagnosticCode_IsUnprovedPresenceRequirement()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.Presence)
            .DiagnosticCode.Should().Be(DiagnosticCode.UnprovedPresenceRequirement);
    }

    [Fact]
    public void Modifier_DiagnosticCode_IsUnprovedModifierRequirement()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.Modifier)
            .DiagnosticCode.Should().Be(DiagnosticCode.UnprovedModifierRequirement);
    }

    [Fact]
    public void Dimension_DiagnosticCode_IsUnprovedDimensionRequirement()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.Dimension)
            .DiagnosticCode.Should().Be(DiagnosticCode.UnprovedDimensionRequirement);
    }

    [Fact]
    public void QualifierCompatibility_DiagnosticCode_IsUnprovedQualifierCompatibility()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.QualifierCompatibility)
            .DiagnosticCode.Should().Be(DiagnosticCode.UnprovedQualifierCompatibility);
    }

    [Fact]
    public void QualifierChain_DiagnosticCode_IsUnprovedQualifierCompatibility()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.QualifierChain)
            .DiagnosticCode.Should().Be(DiagnosticCode.UnprovedQualifierCompatibility);
    }

    [Fact]
    public void Numeric_DiagnosticCode_IsNull_BecauseContextDependent()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.Numeric)
            .DiagnosticCode.Should().BeNull(
                "Numeric obligations have a 1:many diagnostic mapping requiring per-obligation context dispatch");
    }

    [Fact]
    public void AllNonNumericNonKeyPresenceKinds_HaveNonNullDiagnosticCode()
    {
        // Numeric and KeyPresence use context-dependent routing (1:many mapping)
        var nonContextual = ProofRequirements.All
            .Where(m => m.Kind != ProofRequirementKind.Numeric
                     && m.Kind != ProofRequirementKind.KeyPresence)
            .ToList();
        nonContextual.Should().AllSatisfy(m =>
            m.DiagnosticCode.Should().NotBeNull($"{m.Kind} should have a catalog-mediated DiagnosticCode"));
    }

    [Fact]
    public void KeyPresence_DiagnosticCode_IsNull_BecauseContextDependent()
    {
        ProofRequirements.GetMeta(ProofRequirementKind.KeyPresence)
            .DiagnosticCode.Should().BeNull(
                "KeyPresence obligations route to PRE0099 or PRE0101 depending on RequireAbsence flag");
    }
}
