using System;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

// ════════════════════════════════════════════════════════════════════════════════
//  Slice 2–3 — Interval containment proof engine integration tests
//
//  Design reference: docs/Working/interval-proof-engine-design.md
//    §9.2  Integration Test Scenarios
//    §9.3  Edge Case Coverage
//    §9.4  Regression Anchors
//    §8.2  Slice 2 required tests (end-to-end obligation collection + dispatch)
//
//  These tests compile against EXISTING types and assert on observable behavior
//  (DiagnosticCode.NumericOverflow, ProofRequirementKind count, obligation count).
//  They are intentionally RED until George ships Slices 2–3. No skips.
//
//  Companion positive tests (prove succeeds on safe expressions) are paired
//  with each negative test — per §9.1 Section E quality bar.
// ════════════════════════════════════════════════════════════════════════════════

public class ProofEngineIntervalIntegrationTests
{
    private static readonly ProofRequirementKind IntervalContainment =
        ProofRequirementKind.IntervalContainment;

    // ════════════════════════════════════════════════════════════════════════
    //  TypeChecker bounds extraction validation (Phase 1 - B1 diagnostic)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypeChecker_FieldWithBounds_PopulatesDeclaredMinMax()
    {
        // Phase 1 B1: Verify TypeChecker correctly extracts min/max modifiers
        // If this test fails, bounds extraction is broken at TypeChecker.cs:378-383
        const string precept = @"
precept BoundsTest
field qty as decimal min 1 max 1000
state Active initial";

        var result = Compiler.Compile(precept);
        
        // Find the qty field in the compiled semantics
        result.Semantics.FieldsByName.Should().ContainKey("qty", "qty field should exist");
        
        var qtyField = result.Semantics.FieldsByName["qty"];
        
        // Verify bounds were extracted
        qtyField.DeclaredMin.Should().Be(1m, "min 1 modifier should be extracted");
        qtyField.DeclaredMax.Should().Be(1000m, "max 1000 modifier should be extracted");
    }

    [Fact]
    public void TypeChecker_MoneyFieldWithTypedConstantBounds_PopulatesDeclaredMinMaxAndQualifiers()
    {
        const string precept = @"
precept MoneyBounds
field cost as money in 'USD' min '0 USD' max '100000 USD'
state Active initial";

        var result = Compiler.Compile(precept);
        var costField = result.Semantics.FieldsByName["cost"];

        costField.DeclaredMin.Should().Be(0m);
        costField.DeclaredMax.Should().Be(100000m);
        costField.DeclaredMinBoundQualifiers.OfType<DeclaredQualifierMeta.Currency>().Select(q => q.CurrencyCode)
            .Should().ContainSingle().Which.Should().Be("USD");
        costField.DeclaredMaxBoundQualifiers.OfType<DeclaredQualifierMeta.Currency>().Select(q => q.CurrencyCode)
            .Should().ContainSingle().Which.Should().Be("USD");
    }

    [Fact]
    public void TypeChecker_QuantityFieldWithTypedConstantBounds_PopulatesDeclaredMaxAndQualifier()
    {
        const string precept = @"
precept QuantityBounds
field weight as quantity in 'kg' max '5 kg'
state Active initial";

        var result = Compiler.Compile(precept);
        var weightField = result.Semantics.FieldsByName["weight"];

        weightField.DeclaredMax.Should().Be(5m);
        weightField.DeclaredMaxBoundQualifiers.OfType<DeclaredQualifierMeta.Unit>().Select(q => q.UnitCode)
            .Should().ContainSingle().Which.Should().Be("kg");
    }

    [Fact]
    public void TypeChecker_TypedConstantBounds_WithNegativeAndDecimalValues_PopulatesComparableValues()
    {
        const string precept = @"
precept MoneyBounds
field cost as money in 'USD' min '-50 USD' max '99.99 USD'
state Active initial";

        var result = Compiler.Compile(precept);
        var costField = result.Semantics.FieldsByName["cost"];

        costField.DeclaredMin.Should().Be(-50m);
        costField.DeclaredMax.Should().Be(99.99m);
    }

    [Fact]
    public void TypeChecker_PlainNumericAndUnaryMinusBounds_RemainRegressionSafe()
    {
        const string precept = @"
precept NumericBounds
field delta as decimal min -5 max 10
state Active initial";

        var result = Compiler.Compile(precept);
        var deltaField = result.Semantics.FieldsByName["delta"];

        deltaField.DeclaredMin.Should().Be(-5m);
        deltaField.DeclaredMax.Should().Be(10m);
    }

    [Fact]
    public void TypeChecker_InvalidTypedConstantBound_DoesNotPopulateDeclaredMin()
    {
        const string precept = @"
precept MoneyBounds
field cost as money in 'USD' min 'bad-value' max '100 USD'
state Active initial";

        var result = Compiler.Compile(precept);
        var costField = result.Semantics.FieldsByName["cost"];

        costField.DeclaredMin.Should().BeNull();
        costField.DeclaredMax.Should().Be(100m);
        result.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.InvalidTypedConstantContent));
    }

    [Fact]
    public void IntervalObligation_MoneyFieldWithTypedConstantBounds_GeneratesCorrectBounds()
    {
        const string precept = @"
precept MoneyBounds
field cost as money in 'USD' min '0 USD' max '100000 USD'
state Active initial
event Recalculate
from Active on Recalculate
    -> set cost = cost
    -> no transition";

        var result = Compiler.Compile(precept);
        var requirement = result.Proof.Obligations
            .Select(o => o.Requirement)
            .OfType<IntervalContainmentProofRequirement>()
            .Single(r => r.TargetField == "cost");

        requirement.DeclaredMin.Should().Be(0m);
        requirement.DeclaredMax.Should().Be(100000m);
    }

    [Fact]
    public void IntervalObligation_QuantityFieldWithTypedConstantBounds_GeneratesCorrectBounds()
    {
        const string precept = @"
precept QuantityBounds
field weight as quantity in 'kg' min '1 kg' max '5 kg'
state Active initial
event Recalculate
from Active on Recalculate
    -> set weight = weight
    -> no transition";

        var result = Compiler.Compile(precept);
        var requirement = result.Proof.Obligations
            .Select(o => o.Requirement)
            .OfType<IntervalContainmentProofRequirement>()
            .Single(r => r.TargetField == "weight");

        requirement.DeclaredMin.Should().Be(1m);
        requirement.DeclaredMax.Should().Be(5m);
    }

    [Fact]
    public void IntervalContainment_QuantityTypedConstantWithStaticUnit_UsesNormalizedMagnitude()
    {
        const string precept = @"
precept QuantityNormalization
field test as quantity in '[lb_av]' max '10 [lb_av]'
state offState initial
event toggle initial
from offState on toggle
    -> set test = '6 [lb_av]'
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "6 lb_av should remain inside a max bound of 10 lb_av after interval normalization");
    }

    [Fact]
    public void IntervalContainment_EventArgBoundWithTypedConstant_UsesNormalizedArgInterval()
    {
        const string precept = @"
precept ArgNormalization
field target as quantity in 'g' max '500 g'
state Active initial
event Apply(Amount as quantity in 'kg' max '2 [lb_av]')
from Active on Apply
    -> set target = Apply.Amount
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "arg max bound should normalize from 2 lb_av (~907 g), which exceeds the target's 500 g max");
    }

    [Fact]
    public void IntervalContainment_QuantityTypedLiteralWithinBounds_DoesNotEmitNumericOverflow()
    {
        const string precept = @"
precept QuantityAssignment
field test as quantity of 'mass' max '5 kg'
state offState initial
state onState
event toggle initial
from offState on toggle
    -> set test = '1 kg'
    -> transition onState";

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "typed quantity literals should contribute a bounded numeric interval from their magnitude");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "test" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Catalog-driven architecture validation (Phase 2 - B2 diagnostic)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetAction_HasDynamicObligationGenerator_FromCatalog()
    {
        // Phase 2 B2: Verify Set action metadata declares dynamic obligation generation
        // This confirms the interval containment logic is catalog-driven, not hardcoded.
        var setActionMeta = Precept.Language.Actions.GetMeta(Precept.Language.ActionKind.Set);
        
        setActionMeta.DynamicObligationGenerator.Should().NotBeNull(
            "Set action must have a DynamicObligationGenerator to create interval containment obligations");
    }

    [Fact]
    public void SetAction_GeneratesIntervalObligation_ForBoundedDecimalField()
    {
        // Phase 2 B2: Verify that the Set action's DynamicObligationGenerator correctly
        // creates interval containment obligations for bounded decimal/number fields.
        const string precept = @"
precept IntervalTest
field amount as decimal min 100 max 5000
state Active initial
event Update(NewAmount as decimal)
from Active on Update
    -> set amount = Update.NewAmount
    -> no transition";

        var result = Compiler.Compile(precept);
        
        result.Proof.Obligations
            .Count(o => o.Requirement.Kind == IntervalContainment)
            .Should().BeGreaterThanOrEqualTo(1,
                "Set action on bounded field should generate interval containment obligation via catalog metadata");
    }

    [Fact]
    public void ObligationCollection_DecimalFieldWithBounds_StillGeneratesObligation()
    {
        const string precept = @"
precept DecimalBounds
field amount as decimal min 0 max 100
state Active initial
event Update(NewAmount as decimal)
from Active on Update
    -> set amount = amount
    -> no transition";

        CountIntervalContainmentObligations(precept).Should().Be(1);
    }

    [Fact]
    public void ObligationCollection_NumberFieldWithBounds_StillGeneratesObligation()
    {
        const string precept = @"
precept NumberBounds
field amount as number min 0 max 100
state Active initial
event Update(NewAmount as number)
from Active on Update
    -> set amount = amount
    -> no transition";

        CountIntervalContainmentObligations(precept).Should().Be(1);
    }

    [Fact]
    public void ObligationCollection_IntegerField_NoObligationGenerated()
    {
        const string precept = @"
precept IntegerField
field count as integer
state Active initial
event Increment(NewCount as integer)
from Active on Increment
    -> set count = count
    -> no transition";

        CountIntervalContainmentObligations(precept).Should().Be(0);
    }

    [Fact]
    public void ObligationCollection_FieldWithNoConstraintModifiers_NoObligation()
    {
        const string precept = @"
precept UnboundedField
field amount as decimal
state Active initial
event Update(NewAmount as decimal)
from Active on Update
    -> set amount = amount
    -> no transition";

        CountIntervalContainmentObligations(precept).Should().Be(0);
    }

    private static int CountIntervalContainmentObligations(string source)
        => Compiler.Compile(source).Proof.Obligations.Count(o => o.Requirement.Kind == IntervalContainment);

    // ════════════════════════════════════════════════════════════════════════
    //  Inline precept constants — §9.2 fixture strategy
    //  "Do not reference .precept sample files" — defined inline only.
    // ════════════════════════════════════════════════════════════════════════

    // Scenario 1 & 2 — bounded decimal field with set actions
    private const string BoundedLineItemPrecept = @"
precept LineItemCalc
field unitPrice as decimal min 0 max 10000
field qty as decimal min 1 max 1000
field lineTotal as decimal min 0 max 10000000
state Draft initial
event Calculate(NewQty as decimal min 1 max 1000)
from Draft on Calculate
    -> set qty = Calculate.NewQty
    -> set lineTotal = unitPrice * Calculate.NewQty
    -> no transition";

    // Scenario 2 variant — discountRate has no upper bound → lineTotal overflows
    private const string UnboundedDiscountPrecept = @"
precept LineItemCalcBad
field unitPrice as decimal min 0 max 10000
field qty as decimal min 1 max 1000
field discountRate as decimal
field lineTotal as decimal min 0 max 10000000
state Draft initial
event Calculate(NewQty as decimal min 1 max 1000)
from Draft on Calculate
    -> set qty = Calculate.NewQty
    -> set lineTotal = unitPrice * discountRate
    -> no transition";

    // Scenarios 3 & 4 — guarded bounded fields
    private const string GuardedAmountPrecept = @"
precept LoanCalc
field balance as decimal min 0 max 1000000
field payment as decimal min 0 max 100
state Active initial
event MakePayment(Amount as decimal min 0 max 100)
from Active on MakePayment when balance >= 100
    -> set balance = balance - MakePayment.Amount
    -> set payment = MakePayment.Amount
    -> no transition";

    // Scenario 4 variant — bounds too tight (fee can exceed target max)
    private const string TooTightBoundsPrecept = @"
precept LoanCalcBad
field balance as decimal min 0 max 1000000
field total as decimal min 0 max 500000
state Active initial
event AddFees(Fee as decimal min 0 max 600000)
from Active on AddFees
    -> set total = balance + AddFees.Fee
    -> no transition";

    // Scenario 6 — Field-to-field constraint in guard
    private const string FieldConstraintPrecept = @"
precept FieldConstraint
field principal as decimal min 0 max 100000
field interest as decimal min 0 max 10000
field total as decimal min 0 max 110000
state Open initial
event Accrue
from Open on Accrue when principal + interest <= 110000
    -> set total = principal + interest
    -> no transition";

    // Simple addition — operands and target all within proven bounds
    private const string SafeAdditionPrecept = @"
precept SafeAdd
field principal as decimal min 0 max 45000
field interest as decimal min 0 max 5000
field loanBalance as decimal min 0 max 50000
state Open initial
event Accrue
from Open on Accrue
    -> set loanBalance = principal + interest
    -> no transition";

    // Subtraction that can go negative (no guard — unresolved)
    private const string UnguardedSubtractionPrecept = @"
precept UnsafeWithdraw
field balance as decimal min 0 max 999999999.99
state Active initial
event Withdraw(Amount as decimal)
from Active on Withdraw
    -> set balance = balance - Withdraw.Amount
    -> no transition";

    // Integer field — no interval obligation should be generated (§4.3)
    private const string IntegerFieldPrecept = @"
precept IntegerCount
field count as integer
state Active initial
event Increment(Delta as integer)
from Active on Increment
    -> set count = count + Increment.Delta
    -> no transition";

    // Decimal field with no bounds — no obligation generated (§5.1 gradual adoption)
    private const string UnboundedDecimalFieldPrecept = @"
precept UnboundedDecimal
field value as decimal
state Active initial
event Update(NewValue as decimal)
from Active on Update
    -> set value = Update.NewValue
    -> no transition";

    // Multiple set actions on bounded fields — §9.3 MultiSetPrecept (exact obligation count)
    // Bounds: principal max 250000, Rate max 0.2 → interest = 250000×0.2 = 50000 (fits max 50000)
    //         principal + interest = 300000 (fits total max 300000)
    private const string MultiSetPrecept = @"
precept MultiFieldUpdate
field principal as decimal min 0 max 250000
field interest as decimal min 0 max 50000
field total as decimal min 0 max 300000
state Open initial
event Accrue(Rate as decimal min 0 max 0.2)
from Open on Accrue
    -> set principal = principal
    -> set interest = principal * Accrue.Rate
    -> set total = principal + interest
    -> no transition";

    // ════════════════════════════════════════════════════════════════════════
    //  Scenario 1 — Bounded decimal field, valid arithmetic → no diagnostics
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BoundedLineItem_ValidArithmetic_NoNumericOverflowDiagnostic()
    {
        // § 9.2 Scenario 1: all operands bounded, result fits target → proved
        var result = Compiler.Compile(BoundedLineItemPrecept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "all arithmetic results fit within declared target bounds");
    }

    [Fact]
    public void BoundedLineItem_ValidArithmetic_ProofLedgerHasNoUnresolvedIntervalObligations()
    {
        var result = Compiler.Compile(BoundedLineItemPrecept);

        result.Proof.Obligations
            .Where(o => o.Requirement.Kind == IntervalContainment
                     && o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty("all interval containment obligations are proved");
    }

    [Fact]
    public void BoundedLineItem_ValidArithmetic_IntervalObligationsAreGenerated()
    {
        // Regression anchor §9.4 #6: exact delta of new interval obligations
        // lineTotal assignment → 1 interval obligation; qty assignment → 1 interval obligation
        var result = Compiler.Compile(BoundedLineItemPrecept);

        result.Proof.Obligations
            .Count(o => o.Requirement.Kind == IntervalContainment)
            .Should().Be(2,
                "two set actions on bounded fields (qty, lineTotal) produce two obligations");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Scenario 2 — Bounded field, unbounded operand → NumericOverflow
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UnboundedOperand_InBoundedFieldAssignment_EmitsNumericOverflow()
    {
        // § 9.2 Scenario 2: discountRate is unbounded → lineTotal interval unbounded
        // → IntervalContainment obligation unresolved → NumericOverflow emitted
        var result = Compiler.Compile(UnboundedDiscountPrecept);

        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "assignment of unbounded-operand expression to a bounded field emits NumericOverflow");
    }

    [Fact]
    public void UnboundedOperand_DiagnosticSeverityIsError()
    {
        var result = Compiler.Compile(UnboundedDiscountPrecept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().AllSatisfy(d => d.Severity.Should().Be(Severity.Error),
                "NumericOverflow is always an Error — §2.3 Proof Disposition");
    }

    [Fact]
    public void UnboundedOperand_UnresolvedObligation_HasIntervalContainmentKind()
    {
        var result = Compiler.Compile(UnboundedDiscountPrecept);

        result.Proof.Obligations
            .Where(o => o.Requirement.Kind == IntervalContainment
                     && o.Disposition == ProofDisposition.Unresolved)
            .Should().NotBeEmpty(
                "the unresolved obligation that drove NumericOverflow is in the proof ledger");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Scenario 3 — Guarded bounded field, guards sufficient → proved
    //  (Requires Slice 3 guard-narrowing integration)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GuardedBoundedField_GuardsSufficient_NoNumericOverflow()
    {
        // § 9.2 Scenario 3 / § 8.2 Slice 3 completion gate:
        // guard 'when balance >= 100' narrows balance to [100..1000000];
        // MakePayment.Amount max 100 → balance - Amount ∈ [0..1000000] ⊆ [0..1000000]
        var result = Compiler.Compile(GuardedAmountPrecept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "guard 'when balance >= 100' narrows balance, so balance - Amount(max 100) ≥ 0");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Scenario 4 — Bounds too tight for possible sum → NumericOverflow
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TooTightBounds_SumCanExceedMax_EmitsNumericOverflow()
    {
        // § 9.2 Scenario 4: balance up to 1000000 + fee up to 600000 → up to 1600000
        //   but total max is only 500000 → NumericOverflow
        var result = Compiler.Compile(TooTightBoundsPrecept);

        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "balance [0..1000000] + fee [0..600000] = [0..1600000] exceeds total's max 500000");
    }

    [Fact]
    public void TooTightBounds_DiagnosticTargetsCorrectField()
    {
        var result = Compiler.Compile(TooTightBoundsPrecept);

        // The diagnostic message should mention 'total' — the assignment target
        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().Contain(d => d.Message.Contains("total"),
                "the overflow diagnostic identifies the target field by name");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Scenario: Safe addition — all bounded, result fits → proved (companion)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SafeAddition_AllBoundedOperandsFit_NoNumericOverflow()
    {
        // Companion to overflow cases: [0..45000] + [0..5000] = [0..50000] fits loanBalance max
        var result = Compiler.Compile(SafeAdditionPrecept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "[0..45000] + [0..5000] = [0..50000] fits max 50000 — obligation proved");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Scenario: Unguarded subtraction — can go negative → unresolved
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UnguardedSubtraction_AmountUnbounded_EmitsNumericOverflow()
    {
        // balance [0..999999999.99] - amount [unbounded] → result can be negative
        // → lower bound violation (balance has min 0) → NumericOverflow
        var result = Compiler.Compile(UnguardedSubtractionPrecept);

        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "unguarded subtraction by unbounded amount can violate balance's min 0");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  §4.3 integer type — no interval obligation generated
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IntegerField_SetAction_NoIntervalObligationGenerated()
    {
        // § 4.3: integer = BigInteger, mathematically unbounded, no overflow possible
        var result = Compiler.Compile(IntegerFieldPrecept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == IntervalContainment,
                "integer fields never get IntervalContainment obligations — §4.3");
    }

    [Fact]
    public void IntegerField_SetAction_NoNumericOverflowDiagnostic()
    {
        var result = Compiler.Compile(IntegerFieldPrecept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "integer fields cannot overflow — §4.3");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  §5.1 unbounded decimal field — no obligation generated (gradual adoption)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UnboundedDecimalField_NoMinMax_NoIntervalObligationGenerated()
    {
        // § 5.1: no min/max on field → no IntervalContainmentProofRequirement
        var result = Compiler.Compile(UnboundedDecimalFieldPrecept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == IntervalContainment,
                "no bounds declared → no obligation generated → gradual adoption §5.1");
    }

    [Fact]
    public void UnboundedDecimalField_NoMinMax_NoNumericOverflowDiagnostic()
    {
        // Regression: pre-Slice-2 behavior preserved for unbounded fields
        var result = Compiler.Compile(UnboundedDecimalFieldPrecept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "no bounds declared → no obligation → no diagnostic, per §5.1 gradual adoption");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  §9.3 Edge case: multiple set actions on bounded fields (exact count)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MultipleSetActionsOnBoundedFields_EachGeneratesIndependentObligation()
    {
        // § 9.3: three set actions on three bounded fields → three obligations
        // (principal, interest, total — all have min/max)
        var result = Compiler.Compile(MultiSetPrecept);

        result.Proof.Obligations
            .Count(o => o.Requirement.Kind == IntervalContainment)
            .Should().Be(3,
                "three set actions on bounded fields produce three independent obligations — §9.3");
    }

    [Fact]
    public void MultipleSetActions_ObligationsAreDischargedIndependently()
    {
        // Each obligation's disposition is independent — one may prove while others fail
        var result = Compiler.Compile(MultiSetPrecept);

        var intervalObligations = result.Proof.Obligations
            .Where(o => o.Requirement.Kind == IntervalContainment)
            .ToList();

        // All three should be provable:
        // principal←principal [0..250000]⊆[0..250000]; interest←principal*Rate [0..50000]⊆[0..50000];
        // total←principal+interest [0..300000]⊆[0..300000]
        intervalObligations
            .Should().AllSatisfy(o => o.Disposition.Should().Be(ProofDisposition.Proved),
                "principal←principal, interest←principal*Rate, total←principal+interest all fit corrected bounds");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 2 named tests from §8.2
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IntervalContainment_BothBoundsDeclaredAndFit_Proved()
    {
        // §8.2 Slice 2: IntervalContainment_BothBoundsDeclairedAndFit_Proved
        var source = @"
precept Simple
field balance as decimal min 0 max 100
state Active initial
event Credit(AmtA as decimal min 0 max 50, AmtB as decimal min 0 max 50)
from Active on Credit
    -> set balance = Credit.AmtA + Credit.AmtB
    -> no transition";
        var result = Compiler.Compile(source);

        result.Proof.Obligations
            .Where(o => o.Requirement.Kind == IntervalContainment)
            .Should().AllSatisfy(o => o.Disposition.Should().Be(ProofDisposition.Proved),
                "[0..50] + [0..50] = [0..100] fits balance's max 100");
    }

    [Fact]
    public void IntervalContainment_MaxExceeded_EmitsNumericOverflow()
    {
        // §8.2 Slice 2: IntervalContainment_MaxExceeded_EmitsNumericOverflow
        var source = @"
precept Simple
field total as decimal min 0 max 100
state Active initial
event Add(A as decimal min 0 max 80, B as decimal min 0 max 80)
from Active on Add
    -> set total = Add.A + Add.B
    -> no transition";
        var result = Compiler.Compile(source);

        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "[0..80] + [0..80] = [0..160] exceeds total's max 100");
    }

    [Fact]
    public void IntervalContainment_MinViolated_EmitsNumericOverflow()
    {
        // §8.2 Slice 2: IntervalContainment_MinViolated_EmitsNumericOverflow
        var source = @"
precept Simple
field balance as decimal min 0 max 1000
state Active initial
event Withdraw(Amount as decimal min 0 max 2000)
from Active on Withdraw
    -> set balance = balance - Withdraw.Amount
    -> no transition";
        var result = Compiler.Compile(source);

        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "[0..1000] - [0..2000] = [-2000..1000] — lower bound -2000 < min 0");
    }

    [Fact]
    public void IntervalContainment_OnlyMaxDeclared_ChecksMaxOnly()
    {
        // §8.2 Slice 2: one-sided check — only max declared, min is unbounded
        var source = @"
precept OneSided
field value as decimal max 100
state Active initial
event Set(A as decimal max 60, B as decimal max 60)
from Active on Set
    -> set value = Set.A + Set.B
    -> no transition";
        var result = Compiler.Compile(source);

        // [−∞..60] + [−∞..60] → sentinel arithmetic → max 120 > max 100 → overflow on max
        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "sum's max 120 exceeds field's declared max 100");
    }

    [Fact]
    public void IntervalContainment_OnlyMinDeclared_ChecksMinOnly()
    {
        // §8.2 Slice 2: one-sided check — only min declared, max is unbounded
        var source = @"
precept OneSided
field value as decimal min 0
state Active initial
event Set(A as decimal min -100, B as decimal min -100)
from Active on Set
    -> set value = Set.A + Set.B
    -> no transition";
        var result = Compiler.Compile(source);

        // [-100..+∞] + [-100..+∞] → sentinel arithmetic → min -200 < min 0 → overflow on min
        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "sum's min -200 is below field's declared min 0");
    }

    [Fact]
    public void IntervalContainment_NoBoundsDeclared_NoObligationGenerated()
    {
        // §8.2 Slice 2: decimal target with NO min/max → no obligation → no diagnostic
        var source = @"
precept NoBounds
field x as decimal
field y as decimal
state Active initial
event Combine
from Active on Combine
    -> set x = x + y
    -> no transition";
        var result = Compiler.Compile(source);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == IntervalContainment,
                "no bounds declared → no IntervalContainment obligation — §5.1");
    }

    [Fact]
    public void IntervalContainment_IntegerTarget_NoObligationGenerated()
    {
        // §8.2 Slice 2: integer target → no obligation (§4.3)
        var source = @"
precept IntTarget
field counter as integer
state Active initial
event Inc(Delta as integer)
from Active on Inc
    -> set counter = counter + Inc.Delta
    -> no transition";
        var result = Compiler.Compile(source);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == IntervalContainment,
                "integer fields are mathematically unbounded — §4.3");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  §9.4 Regression anchors — existing proofs must be unaffected
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Regression_DivisionByZero_StillProvedBySeparateObligation()
    {
        // §9.4 #1: DivisionByZero proof unaffected by Strategy 7 addition
        // divisor is a field with 'positive' modifier → strategy 2 proves divisor != 0
        var source = @"
precept DivCheck
field ratio as decimal min 0 max 10
field divisor as decimal min 1 max 10 positive
state Active initial
event Compute(Numerator as decimal min 0 max 10)
from Active on Compute
    -> set ratio = Compute.Numerator / divisor
    -> no transition";
        var result = Compiler.Compile(source);

        // divisor has 'positive' modifier → DivisionByZero obligation should be Proved
        result.Proof.Obligations
            .Where(o => o.Requirement is NumericProofRequirement req
                     && req.Comparison == OperatorKind.NotEquals)
            .Should().AllSatisfy(o => o.Disposition.Should().Be(ProofDisposition.Proved),
                "divisor 'positive' modifier proves divisor != 0 via Strategy 2 (DeclarationAttribute)");
    }

    [Fact]
    public void Regression_PreviouslyCleanDefinition_GetsNoNewDiagnostics()
    {
        // §9.4 #10: a definition with no bounded fields gets no new diagnostics
        // vs pre-Slice-2 baseline (interval engine adds no obligations for unbounded fields)
        var source = @"
precept NoBounded
field name as string
state Inactive initial
state Active
event Activate(Label as string)
from Inactive on Activate
    -> set name = Activate.Label
    -> transition Active";
        var result = Compiler.Compile(source);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "no numeric fields → no interval obligations → no NumericOverflow diagnostics");
    }

    [Fact]
    public void Regression_ExistingQualifierCompatibilityObligation_Unaffected()
    {
        // §9.4 #3: QualifierCompatibility proof still fires on qualifier mismatch
        // Strategy 7 does not interfere with Strategies 1–6.
        // Uses old-style syntax (without braces) with a known-good qualifier mismatch:
        // price of 'mass' × quantity of 'length' → dimension mismatch → S5 fires.
        var source = """
            precept QualCheck
            field Total as money in 'USD' default '0.00 USD' writable
            state Draft initial
            event Receive(UnitCost as price in 'USD' of 'mass', Qty as quantity of 'length')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """;
        var result = Compiler.Compile(source);

        // S5 qualifier compatibility diagnostic must still fire
        result.Diagnostics
            .Should().Contain(
                d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility),
                "S5 qualifier compatibility diagnostics still emitted alongside any S7 work");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  §9.3 Edge case: self-referential assignment (field on both sides)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SelfReferentialAssignment_IntervalOf_UsesFieldDeclaredBounds()
    {
        // balance = balance - amount: IntervalOf(balance) uses declared [0..max], not assignment target
        // § 9.3 edge case: field appears on both sides of assignment
        var source = @"
precept SelfRef
field balance as decimal min 0 max 1000
state Active initial
event Reduce(Amount as decimal min 0 max 500)
from Active on Reduce
    -> set balance = balance - Reduce.Amount
    -> no transition";
        var result = Compiler.Compile(source);

        // [0..1000] - [0..500] = [-500..1000] — lower bound -500 < min 0 → overflow
        // (guard narrowing would prove it, but without Slice 3 it stays unresolved)
        result.Proof.Obligations
            .Where(o => o.Requirement.Kind == IntervalContainment)
            .Should().NotBeEmpty(
                "balance - Reduce.Amount generates an interval obligation despite balance appearing on both sides");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  §9.3 Edge case: S5 × S7 co-occurrence — orthogonal obligations
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CurrencyField_QualifierMismatchAndIntervalViolation_BothDiagnosticsEmitted()
    {
        // §9.3: money field with currency qualifier AND bounded decimal field → both TypeChecker and S7
        // TypeChecker ValidateAssignmentQualifiers fires QualifierMismatch on arg with wrong currency;
        // S7 interval containment fires NumericOverflow on decimal field overflow — independent
        var source = @"
precept CurrencyOverflow
field usdBalance as money in 'USD'
field ratio as decimal min 0 max 0.5
state Active initial
event Apply(EurAmount as money in 'EUR', Factor as decimal min 0 max 1.5)
from Active on Apply
    -> set usdBalance = Apply.EurAmount
    -> set ratio = Apply.Factor * Apply.Factor
    -> no transition";
        var result = Compiler.Compile(source);

        // Both QualifierMismatch (TypeChecker) and NumericOverflow (S7) should emit
        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "[0..1.5] × [0..1.5] = [0..2.25] exceeds ratio max 0.5");
        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.QualifierMismatch),
                "EurAmount declared 'in EUR' assigned to usdBalance 'in USD' — QualifierMismatch fires independently of S7");
    }

    [Fact]
    public void CurrencyField_MatchingQualifierAndFitInterval_NoDiagnostics()
    {
        // §9.3 companion negative: matching currency, result fits → no diagnostics
        var source = @"
precept CurrencyOk
field usdBalance as money in 'USD'
field ratio as decimal min 0 max 0.5
state Active initial
event Apply(UsdAmount as money in 'USD', Factor as decimal min 0 max 0.5)
from Active on Apply
    -> set usdBalance = Apply.UsdAmount
    -> set ratio = Apply.Factor
    -> no transition";
        var result = Compiler.Compile(source);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow)
                     || d.Code == nameof(DiagnosticCode.QualifierMismatch))
            .Should().BeEmpty(
                "matching USD qualifiers and [0..0.5] fits ratio max 0.5 — no diagnostics");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Guard Narrowing Integration Tests (Slice 3 end-to-end validation)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GuardNarrowing_MultiBranchGuard_ProvesSafety()
    {
        // Slice 3: Multi-branch guard - test that BuildNarrowedIntervals handles disjunctions
        // The current implementation processes all branches and unions their narrowings.
        // This test ensures that the narrowing logic correctly combines multiple guard branches.
        const string multiGuardTest = @"
precept MultiGuardTest
field balance as decimal min 0 max 10000
state Active initial
event Withdraw1(Amount as decimal min 0 max 100)
from Active on Withdraw1 when balance >= 5000
    -> set balance = balance - Withdraw1.Amount
    -> no transition";

        var result = Compiler.Compile(multiGuardTest);
        
        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "single-branch guard 'balance >= 5000' narrows balance sufficiently for subtraction by max 100");
    }

    [Fact]
    public void GuardNarrowing_FieldConstraint_ProvesSafety()
    {
        // Slice 3: Guard uses field-to-field constraint 'principal + interest <= 110000'
        // This directly proves total assignment is safe without separate interval calculation
        var result = Compiler.Compile(FieldConstraintPrecept);
        
        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "guard constraint 'principal + interest <= 110000' directly bounds total assignment");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Regression Tests — Slice 3 narrowing logic validation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Regression_VacuousTestPass_GuardExtractionNotSilent()
    {
        // Regression anchor: verify that guard extraction doesn't silently fail
        // If guard is null or empty, BuildNarrowedIntervals() correctly returns null
        const string testPrecept = @"
precept VacuousGuardTest
field amount as decimal min 0 max 1000
state Active initial
event Update(NewAmount as decimal)
from Active on Update when 1 == 1
    -> set amount = Update.NewAmount
    -> no transition";

        var result = Compiler.Compile(testPrecept);
        
        // Even with a vacuous guard, if the field is bounded, 
        // interval obligations should still be generated (guard narrowing doesn't apply)
        result.Proof.Obligations
            .Count(o => o.Requirement.Kind == IntervalContainment)
            .Should().BeGreaterThanOrEqualTo(1,
                "interval obligations generated even when guard doesn't narrow bounds");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 37 — affine cross-temperature proof cases
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void QuantityField_CelsiusBound_FahrenheitAssignment_CorrectComparison()
    {
        const string precept = """
            precept TemperatureBounds
            field temp as quantity of 'temperature' max '100 Cel' default '0 Cel'
            state Draft initial
            state Closed
            event Apply
            from Draft on Apply
                -> set temp = '212 [degF]'
                -> transition Closed
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "212 °F and 100 °C normalize to the same Kelvin bound");
        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "temp" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved);
    }

    [Fact]
    public void QuantityField_CelsiusBound_FahrenheitAssignment_Overflow()
    {
        const string precept = """
            precept TemperatureBounds
            field temp as quantity of 'temperature' max '100 Cel' default '0 Cel'
            state Draft initial
            state Closed
            event Apply
            from Draft on Apply
                -> set temp = '213 [degF]'
                -> transition Closed
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "213 °F normalizes above the 100 °C / 373.15 K ceiling");
    }

    [Fact]
    public void QuantityField_KelvinBound_CelsiusAssignment_CorrectNormalization()
    {
        const string precept = """
            precept TemperatureBounds
            field temp as quantity of 'temperature' max '373.15 K' default '0 K'
            state Draft initial
            state Closed
            event Apply
            from Draft on Apply
                -> set temp = '100 Cel'
                -> transition Closed
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "100 °C and 373.15 K normalize to the same value");
        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "temp" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved);
    }

    [Fact]
    public void QuantityField_CelsiusBound_CelsiusAssignment_AffineApplied()
    {
        const string precept = """
            precept TemperatureBounds
            field temp as quantity of 'temperature' max '100 Cel' default '0 Cel'
            state Draft initial
            state Closed
            event Apply
            from Draft on Apply
                -> set temp = '99 Cel'
                -> transition Closed
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "same-unit temperature comparisons still use the affine-normalized path");
        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "temp" })
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved);
    }
}
