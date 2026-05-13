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
    //  Inline precept constants — §9.2 fixture strategy
    //  "Do not reference .precept sample files" — defined inline only.
    // ════════════════════════════════════════════════════════════════════════

    // Scenario 1 & 2 — bounded decimal field with set actions
    private const string BoundedLineItemPrecept = @"
precept LineItemCalc {
    field unitPrice: decimal min 0 max 10000
    field quantity: decimal min 1 max 1000
    field discountRate: decimal min 0 max 0.5
    field lineTotal: decimal min 0 max 10000000
    event Calculate(newQty: decimal min 1 max 1000)
    Draft -> Draft on Calculate:
        set quantity to newQty
        set lineTotal to unitPrice * newQty * (1 - discountRate)
}";

    // Scenario 2 variant — discountRate has no upper bound → lineTotal overflows
    private const string UnboundedDiscountPrecept = @"
precept LineItemCalcBad {
    field unitPrice: decimal min 0 max 10000
    field quantity: decimal min 1 max 1000
    field discountRate: decimal
    field lineTotal: decimal min 0 max 10000000
    event Calculate(newQty: decimal min 1 max 1000)
    Draft -> Draft on Calculate:
        set quantity to newQty
        set lineTotal to unitPrice * newQty * (1 - discountRate)
}";

    // Scenarios 3 & 4 — guarded bounded fields
    private const string GuardedAmountPrecept = @"
precept LoanCalc {
    field balance: decimal min 0 max 1000000
    field payment: decimal min 0 max 10000
    event MakePayment(amount: decimal)
    Active -> Active on MakePayment
        require amount >= 1
        require amount <= balance:
        set balance to balance - amount
        set payment to amount
}";

    // Scenario 4 variant — bounds too tight (fee can exceed target max)
    private const string TooTightBoundsPrecept = @"
precept LoanCalcBad {
    field balance: decimal min 0 max 1000000
    field total: decimal min 0 max 500000
    event AddFees(fee: decimal min 0 max 600000)
    Active -> Active on AddFees:
        set total to balance + fee
}";

    // Simple addition — operands and target all within proven bounds
    private const string SafeAdditionPrecept = @"
precept SafeAdd {
    field principal: decimal min 0 max 45000
    field interest: decimal min 0 max 5000
    field loanBalance: decimal min 0 max 50000
    event Accrue()
    Open -> Open on Accrue:
        set loanBalance to principal + interest
}";

    // Subtraction that can go negative (no guard — unresolved)
    private const string UnguardedSubtractionPrecept = @"
precept UnsafeWithdraw {
    field balance: decimal min 0 max 999999999.99
    event Withdraw(amount: decimal)
    Active -> Active on Withdraw:
        set balance to balance - amount
}";

    // Integer field — no interval obligation should be generated (§4.3)
    private const string IntegerFieldPrecept = @"
precept IntegerCount {
    field count: integer
    event Increment(delta: integer)
    Active -> Active on Increment:
        set count to count + delta
}";

    // Decimal field with no bounds — no obligation generated (§5.1 gradual adoption)
    private const string UnboundedDecimalFieldPrecept = @"
precept UnboundedDecimal {
    field value: decimal
    event Update(newValue: decimal)
    Active -> Active on Update:
        set value to newValue
}";

    // Multiple set actions on bounded fields — §9.3 MultiSetPrecept (exact obligation count)
    private const string MultiSetPrecept = @"
precept MultiFieldUpdate {
    field principal: decimal min 0 max 1000000
    field interest: decimal min 0 max 50000
    field total: decimal min 0 max 1050000
    event Accrue(rate: decimal min 0 max 0.2)
    Open -> Open on Accrue:
        set principal to principal
        set interest to principal * rate
        set total to principal + interest
}";

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
        // lineTotal assignment → 1 interval obligation; quantity assignment → 1 interval obligation
        var result = Compiler.Compile(BoundedLineItemPrecept);

        result.Proof.Obligations
            .Count(o => o.Requirement.Kind == IntervalContainment)
            .Should().Be(2,
                "two set actions on bounded fields (quantity, lineTotal) produce two obligations");
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
        // "require amount <= balance" narrows amount interval so balance - amount >= 0
        var result = Compiler.Compile(GuardedAmountPrecept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "guard 'require amount <= balance' proves the subtraction stays within [0..max]");
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

        // All three should be provable (all operands bounded, sums fit within declared bounds)
        intervalObligations
            .Should().AllSatisfy(o => o.Disposition.Should().Be(ProofDisposition.Proved),
                "principal←principal (identity), interest←principal*rate, total←principal+interest all fit bounds");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 2 named tests from §8.2
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IntervalContainment_BothBoundsDeclaredAndFit_Proved()
    {
        // §8.2 Slice 2: IntervalContainment_BothBoundsDeclairedAndFit_Proved
        var source = @"
precept Simple {
    field balance: decimal min 0 max 100
    event Credit(amount: decimal min 0 max 50)
    Active -> Active on Credit:
        set balance to balance + amount
}";
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
precept Simple {
    field total: decimal min 0 max 100
    event Add(a: decimal min 0 max 80, b: decimal min 0 max 80)
    Active -> Active on Add:
        set total to a + b
}";
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
precept Simple {
    field balance: decimal min 0 max 1000
    event Withdraw(amount: decimal min 0 max 2000)
    Active -> Active on Withdraw:
        set balance to balance - amount
}";
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
precept OneSided {
    field value: decimal max 100
    event Set(a: decimal max 60, b: decimal max 60)
    Active -> Active on Set:
        set value to a + b
}";
        var result = Compiler.Compile(source);

        // [−∞..60] + [−∞..60] = [−∞..120] — max 120 > max 100 → overflow on max
        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "sum's max 120 exceeds field's declared max 100");
    }

    [Fact]
    public void IntervalContainment_OnlyMinDeclared_ChecksMinOnly()
    {
        // §8.2 Slice 2: one-sided check — only min declared, max is unbounded
        var source = @"
precept OneSided {
    field value: decimal min 0
    event Set(a: decimal min -100, b: decimal min -100)
    Active -> Active on Set:
        set value to a + b
}";
        var result = Compiler.Compile(source);

        // [-100..+∞] + [-100..+∞] = [-200..+∞] — min -200 < min 0 → overflow on min
        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "sum's min -200 is below field's declared min 0");
    }

    [Fact]
    public void IntervalContainment_NoBoundsDeclared_NoObligationGenerated()
    {
        // §8.2 Slice 2: decimal target with NO min/max → no obligation → no diagnostic
        var source = @"
precept NoBounds {
    field x: decimal
    field y: decimal
    event Combine()
    Active -> Active on Combine:
        set x to x + y
}";
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
precept IntTarget {
    field counter: integer
    event Inc(delta: integer)
    Active -> Active on Inc:
        set counter to counter + delta
}";
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
        var source = @"
precept DivCheck {
    field ratio: decimal min 0 max 10
    event Compute(divisor: decimal min 1 max 10, numerator: decimal min 0 max 100)
    Active -> Active on Compute:
        set ratio to numerator / divisor
}";
        var result = Compiler.Compile(source);

        // divisor has min 1 → DivisionByZero obligation should be Proved
        result.Proof.Obligations
            .Where(o => o.Requirement is NumericProofRequirement req
                     && req.Comparison == OperatorKind.NotEquals)
            .Should().AllSatisfy(o => o.Disposition.Should().Be(ProofDisposition.Proved),
                "divisor min 1 proves divisor != 0 via Strategy 2 (DeclarationAttribute)");
    }

    [Fact]
    public void Regression_PreviouslyCleanDefinition_GetsNoNewDiagnostics()
    {
        // §9.4 #10: a definition with no bounded fields gets no new diagnostics
        // vs pre-Slice-2 baseline (interval engine adds no obligations for unbounded fields)
        var source = @"
precept NoBounded {
    field name: string
    field active: boolean
    event Activate()
    Inactive -> Active on Activate:
        set active to true
}";
        var result = Compiler.Compile(source);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(
                "no numeric fields → no interval obligations → no NumericOverflow diagnostics");
    }

    [Fact]
    public void Regression_ExistingQualifierCompatibilityObligation_Unaffected()
    {
        // §9.4 #3: QualifierCompatibility proof still fires on currency mismatch
        // Strategy 7 does not interfere with Strategies 1–6
        var source = @"
precept QualCheck {
    field balanceUSD: decimal as USD
    field balanceEUR: decimal as EUR
    event Cross()
    Active -> Active on Cross:
        set balanceUSD to balanceUSD + balanceEUR
}";
        var result = Compiler.Compile(source);

        // QualifierMismatch should still fire independently of NumericOverflow
        result.Proof.Obligations
            .Should().Contain(
                o => o.Requirement.Kind == ProofRequirementKind.QualifierCompatibility,
                "S5 qualifier compatibility obligations still collected alongside S7");
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
precept SelfRef {
    field balance: decimal min 0 max 1000
    event Reduce(amount: decimal min 0 max 500)
    Active -> Active on Reduce:
        set balance to balance - amount
}";
        var result = Compiler.Compile(source);

        // [0..1000] - [0..500] = [-500..1000] — lower bound -500 < min 0 → overflow
        // (guard narrowing would prove it, but without Slice 3 it stays unresolved)
        result.Proof.Obligations
            .Where(o => o.Requirement.Kind == IntervalContainment)
            .Should().NotBeEmpty(
                "balance - amount generates an interval obligation despite balance appearing on both sides");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  §9.3 Edge case: S5 × S7 co-occurrence — orthogonal obligations
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CurrencyField_QualifierMismatchAndIntervalViolation_BothDiagnosticsEmitted()
    {
        // §9.3: decimal field with currency qualifier AND bounds → both S5 and S7 obligations
        // S5 checks dimensional compatibility; S7 checks numeric containment — independent
        var source = @"
precept CurrencyOverflow {
    field usdBalance: decimal as USD min 0 max 100
    field eurAmount: decimal as EUR min 0 max 200
    event Transfer()
    Active -> Active on Transfer:
        set usdBalance to usdBalance + eurAmount
}";
        var result = Compiler.Compile(source);

        // Both QualifierMismatch (S5) and NumericOverflow (S7) should emit
        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.NumericOverflow),
                "[0..100] + [0..200] = [0..300] exceeds usdBalance max 100");
        result.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.QualifierMismatch),
                "USD + EUR qualifier mismatch — S5 fires independently of S7");
    }

    [Fact]
    public void CurrencyField_MatchingQualifierAndFitInterval_NoDiagnostics()
    {
        // §9.3 companion negative: same currency, result fits → no diagnostics
        var source = @"
precept CurrencyOk {
    field balance: decimal as USD min 0 max 1000
    event Credit(amount: decimal as USD min 0 max 500)
    Active -> Active on Credit:
        set balance to balance + amount
}";
        var result = Compiler.Compile(source);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow)
                     || d.Code == nameof(DiagnosticCode.QualifierMismatch))
            .Should().BeEmpty(
                "same currency, [0..500]+[0..500]=[0..1000] fits max 1000 — no diagnostics");
    }
}
