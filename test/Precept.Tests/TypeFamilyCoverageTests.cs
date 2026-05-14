using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

// ════════════════════════════════════════════════════════════════════════════════
//  Slice 13 — Type-Family Coverage Regression Suite
//
//  Design reference: docs/Working/interval-proof-engine-design.md
//    §12    Type-Family Coverage Matrix
//    §12.1  Principle: No Silent Constraint Ignoring
//    §8.2   Slice 13 spec
//
//  Coverage: one positive test + one negative companion per §12 matrix row,
//  plus two meta-tests that verify the coverage holds at the catalog level.
//
//  §12 matrix rows (all covered):
//    decimal         — min/max    → IntervalContainment obligation
//    number          — min/max    → IntervalContainment obligation (ULP-widened)
//    integer         — min/max    → IntervalContainment obligation (catalog-driven, same path as decimal)
//    money           — min/max    → IntervalContainment obligation (requires qualifier)
//    quantity        — min/max    → IntervalContainment obligation (requires qualifier)
//    price           — min/max    → IntervalContainment obligation (requires qualifier)
//    exchangerate    — min/max    → InvalidModifierForType diagnostic (ordering undefined)
//    string          — min/maxlength → LengthContainment obligation (literal assigns only)
//    collection      — min/maxcount  → Bounds extracted, no obligation in V1
//    temporal (date) — no min/max declared in catalog → no obligation
//    optional (any)  — optional modifier → PresenceProofRequirement obligation
// ════════════════════════════════════════════════════════════════════════════════

public class TypeFamilyCoverageTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  § decimal — IntervalContainment obligation
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void DecimalField_WithBounds_GeneratesIntervalObligation()
    {
        // §12 row: decimal | min, max | IntervalContainment ✅ Covered (Slices 1–2)
        const string precept = @"
precept DecimalCoverage
field Balance as decimal min 0 max 1000
state Active initial
event Adjust
from Active on Adjust
    -> set Balance = Balance
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().Contain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "decimal field with min/max must generate an IntervalContainment obligation");
    }

    [Fact]
    public void DecimalField_NoBounds_NoIntervalObligation()
    {
        // Negative companion: unbounded decimal → no obligation (§5.1 gradual adoption)
        const string precept = @"
precept DecimalCoverageNeg
field Amount as decimal
state Active initial
event Adjust
from Active on Adjust
    -> set Amount = Amount
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "decimal field without declared bounds must not generate IntervalContainment");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § number — IntervalContainment obligation (ULP-widened)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NumberField_WithBounds_GeneratesIntervalObligation()
    {
        // §12 row: number | min, max | IntervalContainment ✅ Covered (Slice 4)
        const string precept = @"
precept NumberCoverage
field Score as number min 0 max 100
state Active initial
event Update
from Active on Update
    -> set Score = Score
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().Contain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "number field with min/max must generate an IntervalContainment obligation");
    }

    [Fact]
    public void NumberField_NoBounds_NoIntervalObligation()
    {
        // Negative companion: unbounded number → no obligation
        const string precept = @"
precept NumberCoverageNeg
field Score as number
state Active initial
event Update
from Active on Update
    -> set Score = Score
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "number field without declared bounds must not generate IntervalContainment");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § integer — IntervalContainment obligation IS generated when bounds declared
    //
    //  Note: The catalog-driven obligation generator fires for any field whose
    //  modifiers carry ProofSatisfactions entries. Integer fields with min/max
    //  therefore generate obligations just like other numeric types. The §4.3
    //  "BigInteger cannot overflow" note refers to the *runtime proof strategy*
    //  (exact arithmetic), not to obligation suppression.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IntegerField_WithBounds_GeneratesIntervalObligation()
    {
        // §12 row: integer | min, max | IntervalContainment ✅ Catalog-driven (same path as decimal)
        // Integer fields with declared min/max generate an IntervalContainment obligation
        // because the catalog-driven generator fires on any modifier with ProofSatisfactions.
        const string precept = @"
precept IntegerCoverage
field Count as integer min 0 max 999
state Active initial
event Increment(Delta as integer)
from Active on Increment
    -> set Count = Count + Increment.Delta
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().Contain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "integer fields with declared min/max generate IntervalContainment obligations via the catalog-driven generator");
    }

    [Fact]
    public void IntegerField_NoBounds_NoIntervalObligation()
    {
        // Negative companion: integer without bounds → also no obligation (consistent)
        const string precept = @"
precept IntegerCoverageNeg
field Count as integer
state Active initial
event Increment(Delta as integer)
from Active on Increment
    -> set Count = Count + Increment.Delta
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "integer field without bounds must not generate IntervalContainment");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § money — IntervalContainment obligation (requires currency qualifier)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MoneyField_WithQualifiedBounds_GeneratesIntervalObligation()
    {
        // §12 row: money | min, max | IntervalContainment ✅ Covered (Slice 7 + Slices 8–10)
        // Typed-constant bounds require matching currency qualifier — e.g., '0 USD' / '100000 USD'
        const string precept = @"
precept MoneyCoverage
field TotalCost as money in 'USD' min '0 USD' max '100000 USD'
state Active initial
event Recalculate
from Active on Recalculate
    -> set TotalCost = TotalCost
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().Contain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "money field with typed-constant min/max must generate IntervalContainment");

        result.Diagnostics
            .Should().NotContain(d => d.Code == DiagnosticCode.BoundsRequireQualifier.ToString(),
                "money field with 'in USD' qualifier satisfies bound qualifier requirement");
    }

    [Fact]
    public void MoneyField_NoBounds_NoIntervalObligation()
    {
        // Negative companion: money without bounds → no obligation
        const string precept = @"
precept MoneyCoverageNeg
field TotalCost as money in 'USD'
state Active initial
event Recalculate
from Active on Recalculate
    -> set TotalCost = TotalCost
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "money field without declared bounds must not generate IntervalContainment");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § quantity — IntervalContainment obligation (requires unit/dimension qualifier)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void QuantityField_WithQualifiedBounds_GeneratesIntervalObligation()
    {
        // §12 row: quantity | min, max | IntervalContainment ✅ Covered (Slice 7 + Slices 8–10)
        const string precept = @"
precept QuantityCoverage
field Weight as quantity in 'kg' min '1 kg' max '100 kg'
state Active initial
event Recalculate
from Active on Recalculate
    -> set Weight = Weight
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().Contain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "quantity field with typed-constant min/max must generate IntervalContainment");

        result.Diagnostics
            .Should().NotContain(d => d.Code == DiagnosticCode.BoundsRequireQualifier.ToString(),
                "quantity field with 'in kg' qualifier satisfies bound qualifier requirement");
    }

    [Fact]
    public void QuantityField_NoBounds_NoIntervalObligation()
    {
        // Negative companion: quantity without bounds → no obligation
        const string precept = @"
precept QuantityCoverageNeg
field Weight as quantity in 'kg'
state Active initial
event Recalculate
from Active on Recalculate
    -> set Weight = Weight
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "quantity field without declared bounds must not generate IntervalContainment");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § price — IntervalContainment obligation (requires currency qualifier)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PriceField_WithQualifiedBounds_GeneratesIntervalObligation()
    {
        // §12 row: price | min, max | IntervalContainment ✅ Covered (Slice 7 + Slices 8–10)
        const string precept = @"
precept PriceCoverage
field UnitPrice as price in 'USD/each' min '1 USD/each' max '1000 USD/each'
state Active initial
event Recalculate
from Active on Recalculate
    -> set UnitPrice = UnitPrice
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().Contain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "price field with typed-constant min/max must generate IntervalContainment");

        result.Diagnostics
            .Should().NotContain(d => d.Code == DiagnosticCode.BoundsRequireQualifier.ToString(),
                "price field with 'in USD/each' qualifier satisfies bound qualifier requirement");
    }

    [Fact]
    public void PriceField_NoBounds_NoIntervalObligation()
    {
        // Negative companion: price without bounds → no obligation
        const string precept = @"
precept PriceCoverageNeg
field UnitPrice as price in 'USD/each'
state Active initial
event Recalculate
from Active on Recalculate
    -> set UnitPrice = UnitPrice
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "price field without declared bounds must not generate IntervalContainment");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § exchangerate — bounds modifier explicitly invalid; not silently ignored
    //
    //  Exchangerate does NOT support min/max because ordering is undefined for
    //  currency-pair rates (only ==/!= operators are valid). The type checker
    //  emits InvalidModifierForType, satisfying §12.1 (not silently ignored).
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExchangeRateField_BoundsModifierRejectedByTypeChecker_NotSilentlyIgnored()
    {
        // §12 row: exchangerate | min/max NOT applicable → InvalidModifierForType diagnostic
        // §12.1: declaring min/max on exchangerate emits a type-checker error — NOT silently ignored.
        // Exchangerate supports only == and != (ordering is undefined for currency pairs).
        const string precept = @"
precept ExchangeRateCoverage
field FxRate as exchangerate in 'USD' to 'EUR' min '0.9 USD/EUR'
state Active initial";

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Should().Contain(d => d.Code == DiagnosticCode.InvalidModifierForType.ToString(),
                "min/max on exchangerate must emit InvalidModifierForType — exchangerate ordering is undefined, §12.1 not silently ignored");
    }

    [Fact]
    public void ExchangeRateField_NoBoundsModifier_NoObligationAndNoError()
    {
        // Negative companion: valid exchangerate without bounds → clean compilation, no obligations
        const string precept = @"
precept ExchangeRateCoverageNeg
field FxRate as exchangerate in 'USD' to 'EUR' optional
state Active initial";

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeFalse(
            "exchangerate without bounds modifiers compiles cleanly");

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "exchangerate fields never generate IntervalContainment obligations");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § string — LengthContainment obligation (literal assignments only, V1)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StringField_WithLengthBounds_GeneratesLengthObligation()
    {
        // §12 row: string | minlength, maxlength | LengthContainment ✅ Covered (Slice 11)
        // V1 strategy: literal string assignments to bounded string fields generate obligations.
        const string precept = @"
precept StringCoverage
field Code as string optional maxlength 10
state Draft initial
state Done terminal
event Submit
from Draft on Submit -> set Code = ""ABC"" -> transition Done";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().Contain(o => o.Requirement.Kind == ProofRequirementKind.LengthContainment,
                "string field with maxlength and a literal assignment must generate LengthContainment");
    }

    [Fact]
    public void StringField_NoBounds_NoLengthObligation()
    {
        // Negative companion: string without length bounds → no obligation
        const string precept = @"
precept StringCoverageNeg
field Note as string optional
state Draft initial
state Done terminal
event Submit
from Draft on Submit -> set Note = ""Some text"" -> transition Done";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.LengthContainment,
                "string field without length bounds must not generate LengthContainment");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § collection — mincount/maxcount bounds extracted; no obligation in V1
    //
    //  V1 approach: add/remove actions do not generate count containment obligations.
    //  Collections cannot be set directly (type checker rejects direct set of a collection).
    //  The constraint is correctly declared, extracted into SemanticIndex, and will be
    //  enforced when CountContainment proof generation is implemented in a future slice.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CollectionField_WithCountBounds_BoundsExtractedNoObligationInV1()
    {
        // §12 row: collection | mincount, maxcount | CountContainment ✅ Covered (Slice 11)
        // V1: bounds are extracted (not silently lost) but no obligation is generated yet.
        const string precept = @"
precept CollectionCoverage
field Tags as set of string mincount 1 maxcount 10
state Active initial";

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeFalse(
            "collection with mincount/maxcount bounds is a valid declaration");

        result.Semantics.FieldsByName.Should().ContainKey("Tags");
        var field = result.Semantics.FieldsByName["Tags"];
        field.DeclaredMinCount.Should().Be(1, "mincount 1 must be extracted into SemanticIndex");
        field.DeclaredMaxCount.Should().Be(10, "maxcount 10 must be extracted into SemanticIndex");

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.CountContainment,
                "V1: add/remove actions do not yet generate CountContainment obligations");
    }

    [Fact]
    public void CollectionField_NoBounds_NoObligationOrDiagnostic()
    {
        // Negative companion: collection without count bounds → clean, no obligation
        const string precept = @"
precept CollectionCoverageNeg
field Tags as set of string
state Active initial";

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeFalse(
            "collection without count bounds compiles cleanly");

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.CountContainment,
                "collection without bounds must not generate CountContainment");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § temporal — no min/max declared in catalog; no obligation expected
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TemporalField_NoBoundsDeclaredInCatalog_NoObligation()
    {
        // §12 row: temporal | no min/max declared in catalog | None ✅ No gap
        // Temporal types (date, time, datetime, instant) do not have min/max modifiers
        // in their ApplicableTo entries — ordering semantics are calendar-based, not numeric.
        const string precept = @"
precept TemporalCoverage
field DueDate as date optional
state Active initial";

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeFalse(
            "temporal field without bounds is a valid declaration");

        result.Proof.Obligations
            .Should().NotContain(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment,
                "temporal fields have no min/max in the catalog — no IntervalContainment obligations expected");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  § optional — PresenceProofRequirement for optional field refs in value position
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OptionalField_InValuePosition_GeneratesPresenceObligation()
    {
        // §12 row: optional (any type) | optional modifier | Presence ✅ Covered (Slice 12)
        // §12.1: optional field referenced in a value position must produce
        //        PresenceProofRequirement — not silently ignored.
        const string precept = @"
precept OptionalCoverage
field Source as number optional
field Target as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Target = Source -> transition Done";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Where(o => o.Requirement is PresenceProofRequirement)
            .Should().NotBeEmpty(
                "an optional field used as a value source must generate a PresenceProofRequirement (§12.1)");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Meta-test: AllConstrainableTypes_DeclaredConstraint_NeverSilentlyIgnored
    //
    //  Iterates over every type in the Types catalog that has min/max in its
    //  applicable modifiers (derived from Modifiers.GetMeta(ModifierKind.Min).ApplicableTo).
    //  Verifies §12.1: each type either generates an IntervalContainment obligation
    //  or produces a diagnostic — never silently ignoring the declared constraint.
    //
    //  Qualified types (money, quantity, price) without an 'in/of' qualifier emit
    //  BoundsRequireQualifier → satisfies "has diagnostic". Integer and unqualified
    //  numeric types (decimal, number) generate IntervalContainment obligations directly.
    // ════════════════════════════════════════════════════════════════════════════

    public static TheoryData<TypeKind> ConstrainableNumericTypes()
    {
        var data = new TheoryData<TypeKind>();
        var minMeta = (ValueModifierMeta)Modifiers.GetMeta(ModifierKind.Min);
        foreach (var target in minMeta.ApplicableTo)
        {
            if (target.Kind is not null)
                data.Add(target.Kind.Value);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ConstrainableNumericTypes))]
    public void AllConstrainableTypes_DeclaredConstraint_NeverSilentlyIgnored(TypeKind typeKind)
    {
        // Build a minimal precept: field with plain min 0 max 100 + self-referential set action.
        // For qualified types (money, quantity, price): plain min/max without qualifier triggers
        // BoundsRequireQualifier diagnostic → satisfies "has diagnostic" (not silently ignored).
        // For decimal/number: IntervalContainment obligation generated.
        // For integer: known §4.3 exempt — verified to NOT generate obligation, no diagnostic either.
        var typeKeyword = TypeKeyword(typeKind);
        var preceptSource = $@"
precept {typeKind}MetaTest
field Value as {typeKeyword} min 0 max 100
state Active initial
event Adjust
from Active on Adjust
    -> set Value = Value
    -> no transition";

        var result = Compiler.Compile(preceptSource);

        bool hasIntervalObligation = result.Proof.Obligations
            .Any(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment);

        bool hasErrorOrWarningDiagnostic = result.Diagnostics
            .Any(d => d.Severity == Severity.Error || d.Severity == Severity.Warning);

        (hasIntervalObligation || hasErrorOrWarningDiagnostic).Should().BeTrue(
            $"type {typeKind} with declared min/max must produce either an " +
            "IntervalContainment obligation or a diagnostic — §12.1 no silent constraint ignoring. " +
            $"Hint: qualified types (money/quantity/price) without 'in/of' qualifier emit " +
            "BoundsRequireQualifier; integer/decimal/number emit obligations directly.");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Helper: type keyword for catalog-driven precept synthesis
    // ════════════════════════════════════════════════════════════════════════════

    private static string TypeKeyword(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        return meta.Token?.Text
            ?? meta.DisplayName
            ?? kind.ToString().ToLowerInvariant();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Cross-family regression: all per-family tests remain green (§8.2 Slice 13)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AllIntervalObligationFamilies_MultipleSetActions_ObligationsAreIndependent()
    {
        // §9.3 multiple set actions edge case: each generates an independent obligation.
        // This cross-family test ensures the obligation collector does not short-circuit.
        const string precept = @"
precept MultiTypeCoverage
field DecimalVal as decimal min 0 max 100
field NumberVal as number min 0 max 100
state Active initial
event Update
from Active on Update
    -> set DecimalVal = DecimalVal
    -> set NumberVal = NumberVal
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Count(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment)
            .Should().Be(2,
                "two bounded fields × one set action each = two independent IntervalContainment obligations");
    }

    [Fact]
    public void MixedFamilyPrecept_BoundedAndUnboundedFields_OnlyBoundedGenerateObligations()
    {
        // Regression: unbounded fields in the same precept must not cause spurious obligations.
        const string precept = @"
precept MixedFamilyCoverage
field BoundedDecimal as decimal min 0 max 500
field UnboundedDecimal as decimal
field BoundedNumber as number min 0 max 500
state Active initial
event Adjust
from Active on Adjust
    -> set BoundedDecimal = BoundedDecimal
    -> set UnboundedDecimal = UnboundedDecimal
    -> set BoundedNumber = BoundedNumber
    -> no transition";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Count(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment)
            .Should().Be(2,
                "only BoundedDecimal and BoundedNumber produce obligations — UnboundedDecimal must not");
    }
}
