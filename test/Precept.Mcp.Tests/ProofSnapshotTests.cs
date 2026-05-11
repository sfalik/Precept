using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

/// <summary>
/// Verifies that CompileTool.Run returns correct structured proof data (ProofSnapshot)
/// for various precept inputs. Covers: field interval bounds, display text, attribution
/// sources, scope structure, no-proof cases, and computed-field proofs.
///
/// Note: result.Proof.Events is always null in the current implementation — field-level
/// proofs are scoped to Global only. This is documented by test.
/// </summary>
public class ProofSnapshotTests
{
    private static string ReadSample(string fileName) =>
        File.ReadAllText(Path.Combine(TestPaths.SamplesDir, fileName));

    // ── Scope structure ───────────────────────────────────────────────────────

    [Fact]
    public void PositiveField_ProofIsNotNull()
    {
        var text = """
            precept Test
            field Amount as number default 1 positive
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().NotBeNull();
    }

    [Fact]
    public void PositiveField_GlobalScopeIsNotNull()
    {
        var text = """
            precept Test
            field Amount as number default 1 positive
            """;

        var result = CompileTool.Run(text);

        result.Proof!.Global.Should().NotBeNull();
        result.Proof.Global!.Fields.Should().ContainKey("Amount");
    }

    [Fact]
    public void Proof_EventsScopeIsAlwaysNull()
    {
        // Events-scoped proof is not yet implemented. Events must be null.
        var text = """
            precept Test
            field Amount as number default 1 positive
            state Open initial
            state Closed
            event Close
            from Open on Close -> transition Closed
            """;

        var result = CompileTool.Run(text);

        result.Proof.Should().NotBeNull();
        result.Proof!.Events.Should().BeNull();
    }

    // ── Interval bounds: positive ─────────────────────────────────────────────

    [Fact]
    public void PositiveField_IntervalIsOpenAboveZero()
    {
        var text = """
            precept Test
            field Amount as number default 1 positive
            """;

        var result = CompileTool.Run(text);

        var entry = result.Proof!.Global!.Fields!["Amount"];
        entry.Interval.Should().NotBeNull();
        entry.Interval!.Lower.Should().Be(0);
        entry.Interval.LowerInclusive.Should().BeFalse("positive means strictly > 0, so lower bound is open");
        entry.Interval.Upper.Should().Be(double.PositiveInfinity);
        entry.Interval.UpperInclusive.Should().BeFalse();
    }

    [Fact]
    public void PositiveField_DisplayIsAlwaysGreaterThanZero()
    {
        var text = """
            precept Test
            field Score as number default 1 positive
            """;

        var result = CompileTool.Run(text);

        var entry = result.Proof!.Global!.Fields!["Score"];
        entry.Display.Should().Be("always greater than 0");
        entry.Interval!.Display.Should().Be("always greater than 0");
    }

    // ── Interval bounds: nonnegative ──────────────────────────────────────────

    [Fact]
    public void NonnegativeField_IntervalIsHalfOpenFromZero()
    {
        var text = """
            precept Test
            field Balance as number default 0 nonnegative
            """;

        var result = CompileTool.Run(text);

        var entry = result.Proof!.Global!.Fields!["Balance"];
        entry.Interval.Should().NotBeNull();
        entry.Interval!.Lower.Should().Be(0);
        entry.Interval.LowerInclusive.Should().BeTrue("nonnegative means >= 0, so lower bound is closed");
        entry.Interval.Upper.Should().Be(double.PositiveInfinity);
        entry.Interval.UpperInclusive.Should().BeFalse();
    }

    [Fact]
    public void NonnegativeField_DisplayIsZeroOrGreater()
    {
        var text = """
            precept Test
            field Balance as number default 0 nonnegative
            """;

        var result = CompileTool.Run(text);

        var entry = result.Proof!.Global!.Fields!["Balance"];
        entry.Display.Should().Be("0 or greater");
    }

    // ── Interval bounds: min N ────────────────────────────────────────────────

    [Fact]
    public void MinConstraintField_IntervalStartsAtMin()
    {
        var text = """
            precept Test
            field Priority as number default 5 min 5
            """;

        var result = CompileTool.Run(text);

        var entry = result.Proof!.Global!.Fields!["Priority"];
        entry.Interval.Should().NotBeNull();
        entry.Interval!.Lower.Should().Be(5);
        entry.Interval.LowerInclusive.Should().BeTrue("min means >= N, so lower bound is closed");
        entry.Interval.Upper.Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void MinConstraintField_DisplayIsNOrGreater()
    {
        var text = """
            precept Test
            field Priority as number default 5 min 5
            """;

        var result = CompileTool.Run(text);

        var entry = result.Proof!.Global!.Fields!["Priority"];
        entry.Display.Should().Be("5 or greater");
    }

    // ── Interval bounds: min N max M ──────────────────────────────────────────

    [Fact]
    public void MinMaxConstraintField_IntervalIsBounded()
    {
        var text = """
            precept Test
            field TaxRate as number default 0.08 min 0 max 0.99
            """;

        var result = CompileTool.Run(text);

        var entry = result.Proof!.Global!.Fields!["TaxRate"];
        entry.Interval.Should().NotBeNull();
        entry.Interval!.Lower.Should().Be(0);
        entry.Interval.LowerInclusive.Should().BeTrue();
        entry.Interval.Upper.Should().Be(0.99);
        entry.Interval.UpperInclusive.Should().BeTrue("max means <= M, so upper bound is closed");
    }

    [Fact]
    public void MinMaxConstraintField_DisplayIsRange()
    {
        var text = """
            precept Test
            field Score as number default 5 min 2 max 10
            """;

        var result = CompileTool.Run(text);

        var entry = result.Proof!.Global!.Fields!["Score"];
        entry.Display.Should().Be("2 to 10 (inclusive)");
    }

    // ── Attribution sources ───────────────────────────────────────────────────

    [Fact]
    public void PositiveField_SourcesContainPositiveConstraintLabel()
    {
        var text = """
            precept Test
            field Amount as number default 1 positive
            """;

        var result = CompileTool.Run(text);

        var sources = result.Proof!.Global!.Fields!["Amount"].Sources;
        sources.Should().Contain("field constraint: positive");
    }

    [Fact]
    public void NonnegativeField_SourcesContainNonnegativeConstraintLabel()
    {
        var text = """
            precept Test
            field Balance as number default 0 nonnegative
            """;

        var result = CompileTool.Run(text);

        var sources = result.Proof!.Global!.Fields!["Balance"].Sources;
        sources.Should().Contain("field constraint: nonnegative");
    }

    [Fact]
    public void MinConstraintField_SourcesContainMinLabel()
    {
        var text = """
            precept Test
            field Priority as number default 5 min 5
            """;

        var result = CompileTool.Run(text);

        var sources = result.Proof!.Global!.Fields!["Priority"].Sources;
        sources.Should().Contain("field constraint: min 5");
    }

    [Fact]
    public void MinMaxConstraintField_SourcesContainBothLabels()
    {
        var text = """
            precept Test
            field Score as number default 5 min 2 max 10
            """;

        var result = CompileTool.Run(text);

        var sources = result.Proof!.Global!.Fields!["Score"].Sources;
        sources.Should().Contain("field constraint: min 2");
        sources.Should().Contain("field constraint: max 10");
    }

    // ── No-proof cases ────────────────────────────────────────────────────────

    [Fact]
    public void StringFieldOnly_ProofIsNull()
    {
        var text = """
            precept Test
            field Name as string default ""
            field Description as string nullable
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().BeNull("string fields carry no numeric proof data");
    }

    [Fact]
    public void BooleanFieldOnly_ProofIsNull()
    {
        var text = """
            precept Test
            field IsActive as boolean default false
            field IsVerified as boolean nullable
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().BeNull("boolean fields carry no numeric proof data");
    }

    [Fact]
    public void UnconstrainedNumberField_ProofIsNull()
    {
        var text = """
            precept Test
            field Amount as number default 0
            field Quantity as number nullable
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().BeNull("unconstrained number fields have unknown interval — no proof data emitted");
    }

    [Fact]
    public void MixedFields_ProofContainsOnlyConstrainedNumericFields()
    {
        var text = """
            precept Test
            field Name as string default ""
            field IsActive as boolean default false
            field UncheckedCount as number default 0
            field Amount as number default 1 positive
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().NotBeNull();
        result.Proof!.Global!.Fields.Should().ContainKey("Amount");
        result.Proof.Global.Fields.Should().NotContainKey("Name");
        result.Proof.Global.Fields.Should().NotContainKey("IsActive");
        result.Proof.Global.Fields.Should().NotContainKey("UncheckedCount");
    }

    // ── Computed field proofs (from samples) ──────────────────────────────────

    [Fact]
    public void SumOnRhsRuleSample_AllConstrainedFieldsHaveProofEntries()
    {
        var text = ReadSample("sum-on-rhs-rule.precept");

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().NotBeNull();

        var fields = result.Proof!.Global!.Fields!;
        fields.Should().ContainKey("Total");
        fields.Should().ContainKey("Tax");
        fields.Should().ContainKey("Fee");
        fields.Should().ContainKey("Net");
    }

    [Fact]
    public void SumOnRhsRuleSample_NetFieldIsProvenPositive()
    {
        var text = ReadSample("sum-on-rhs-rule.precept");

        var result = CompileTool.Run(text);

        var netEntry = result.Proof!.Global!.Fields!["Net"];
        netEntry.Interval.Should().NotBeNull();
        netEntry.Interval!.Lower.Should().Be(0);
        netEntry.Interval.LowerInclusive.Should().BeFalse("Net is proven > 0 via rule Total > Tax + Fee");
        netEntry.Interval.Upper.Should().Be(double.PositiveInfinity);
        netEntry.Display.Should().Be("always greater than 0");
    }

    [Fact]
    public void ComputedTaxNetSample_TaxRateHasBoundedInterval()
    {
        var text = ReadSample("computed-tax-net.precept");

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().NotBeNull();

        var taxRateEntry = result.Proof!.Global!.Fields!["TaxRate"];
        taxRateEntry.Interval.Should().NotBeNull();
        taxRateEntry.Interval!.Lower.Should().Be(0);
        taxRateEntry.Interval.LowerInclusive.Should().BeTrue();
        taxRateEntry.Interval.Upper.Should().Be(0.99);
        taxRateEntry.Interval.UpperInclusive.Should().BeTrue();
        taxRateEntry.Display.Should().Be("0 to 0.99 (inclusive)");
    }

    [Fact]
    public void ComputedTaxNetSample_SubtotalIsProvenPositive()
    {
        var text = ReadSample("computed-tax-net.precept");

        var result = CompileTool.Run(text);

        var subtotalEntry = result.Proof!.Global!.Fields!["Subtotal"];
        subtotalEntry.Interval!.Lower.Should().Be(0);
        subtotalEntry.Interval.LowerInclusive.Should().BeFalse();
        subtotalEntry.Display.Should().Be("always greater than 0");
    }

    // ── Max-only constraint ───────────────────────────────────────────────────

    [Fact]
    public void MaxOnlyConstraintField_IntervalIsOpenEndedBelow()
    {
        var text = """
            precept Test
            field Discount as number default 0 max 100
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().NotBeNull();

        var entry = result.Proof!.Global!.Fields!["Discount"];
        entry.Interval.Should().NotBeNull();
        entry.Interval!.Upper.Should().Be(100);
        entry.Interval.UpperInclusive.Should().BeTrue("max means <= 100");
        entry.Interval.Lower.Should().Be(double.NegativeInfinity);
        entry.Display.Should().Be("100 or less");
    }

    // ── Multiple constrained fields in one precept ────────────────────────────

    [Fact]
    public void MultipleConstrainedFields_AllAppearInProofGlobalScope()
    {
        var text = """
            precept Test
            field A as number default 1 positive
            field B as number default 0 nonnegative
            field C as number default 5 min 3 max 20
            """;

        var result = CompileTool.Run(text);

        result.Valid.Should().BeTrue();
        result.Proof.Should().NotBeNull();
        result.Proof!.Global!.Fields.Should().ContainKey("A");
        result.Proof.Global.Fields.Should().ContainKey("B");
        result.Proof.Global.Fields.Should().ContainKey("C");
    }
}
