using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// BUG-012: ordinal comparison between an ordered-choice field and a same-set literal.
/// The proof engine must lift the <c>Ordered</c> modifier from the field-side operand
/// onto a same-set literal operand. Field-vs-field comparisons already discharge cleanly
/// via the <c>DeclarationAttribute</c> strategy.
/// </summary>
public class OrderedChoiceLiteralTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void IntegerOrderedChoice_LessThanLiteral_DischargesOrderedRequirement()
    {
        var ledger = Prove("""
            precept Repro
            field Severity as choice of integer(1, 2, 3, 4, 5) ordered default 3
            field IsCritical as boolean <- Severity <= 2
            state Open initial
            """);

        ledger.Obligations
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(because: "the integer literal 2 inherits Severity's 'ordered' from the binary-op sibling");
    }

    [Fact]
    public void StringOrderedChoice_LessThanOrEqualLiteral_DischargesOrderedRequirement()
    {
        var ledger = Prove("""
            precept Repro
            field Tier as choice of string("Low", "Medium", "High") ordered default "Low"
            field IsLow as boolean <- Tier <= "Low"
            state Open initial
            """);

        ledger.Obligations
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(because: "the string literal \"Low\" inherits Tier's 'ordered' from the binary-op sibling");
    }

    [Fact]
    public void OrderedChoiceFieldVsField_StillDischarges()
    {
        var ledger = Prove("""
            precept Repro
            field Severity as choice of integer(1, 2, 3, 4, 5) ordered default 3
            field Threshold as choice of integer(1, 2, 3, 4, 5) ordered default 1
            field R as boolean <- Severity <= Threshold
            state Open initial
            """);

        ledger.Obligations
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(because: "field-vs-field ordered-choice comparisons discharge via DeclarationAttribute");
    }

    [Fact]
    public void UnorderedChoiceField_LessThanLiteral_StillEmitsUnproved()
    {
        var ledger = Prove("""
            precept Repro
            field Tag as choice of string("A", "B", "C") default "A"
            field IsA as boolean <- Tag <= "A"
            state Open initial
            """);

        ledger.Obligations
            .Where(o => o.Requirement is ModifierRequirement { Required: ModifierKind.Ordered })
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().NotBeEmpty(because: "an unordered choice field cannot lift Ordered onto a literal sibling");
    }

    [Fact]
    public void IntegerOrderedChoice_LiteralLeftFieldRight_AlsoDischarges()
    {
        // Inverted operand order — literal on the left, field on the right. The
        // resolver returns the Right operand first, so this exercises the direct
        // field-discharge path (not the literal-inference fallback), confirming
        // ordered-choice comparison works regardless of literal position.
        var ledger = Prove("""
            precept Repro
            field Severity as choice of integer(1, 2, 3, 4, 5) ordered default 3
            field IsCritical as boolean <- 2 >= Severity
            state Open initial
            """);

        ledger.Obligations
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(because: "inverted-operand-order ordered-choice comparisons still lift the modifier symmetrically");
    }

    [Fact]
    public void OrderedChoiceCollectionAccessor_VsField_DischargesViaElementType()
    {
        // Accessor return-type for `Tiers.min` on `set of choice of T(...) ordered`
        // must discharge the Ordered requirement on the field-vs-accessor comparison.
        // The accessor result propagates ordered-ness via TypedMemberAccess.ChoiceMetadata
        // (D-3 Option A) — exercises the inline-slot path.
        var ledger = Prove("""
            precept Repro
            field Severities as set of choice of string("Info", "Warn", "Error") ordered
            field Threshold as choice of string("Info", "Warn", "Error") ordered default "Info"
            field IsHigh as boolean <- Severities.min >= Threshold
            state Open initial
            """);

        ledger.Obligations
            .Where(o => o.Requirement is ModifierRequirement { Required: ModifierKind.Ordered })
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(because: "the .min accessor on an ordered choice set discharges Ordered via TypedMemberAccess.ChoiceMetadata");
    }

    [Fact]
    public void OrderedChoiceConditional_VsField_DischargesViaInlineMetadata()
    {
        // Conditional whose branches are both ordered-choice fields propagates the
        // ordered bit onto TypedConditional.ChoiceMetadata; comparing the conditional
        // result against a same-set field discharges without walking back to a single
        // field declaration.
        var ledger = Prove("""
            precept Repro
            field PrimaryTier as choice of string("Low", "Medium", "High") ordered default "Low"
            field BackupTier as choice of string("Low", "Medium", "High") ordered default "Low"
            field UseBackup as boolean default false
            field Threshold as choice of string("Low", "Medium", "High") ordered default "Low"
            field IsHigh as boolean <- (if UseBackup then BackupTier else PrimaryTier) >= Threshold
            state Open initial
            """);

        ledger.Obligations
            .Where(o => o.Requirement is ModifierRequirement { Required: ModifierKind.Ordered })
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(because: "both branches return an ordered-choice; TypedConditional.ChoiceMetadata carries the bit forward");
    }
}
