using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class CompileToolProofTests
{
    private static string ReadSample(string fileName) =>
        File.ReadAllText(Path.Combine(TestPaths.SamplesDir, fileName));

    [Fact]
    public void NonnegativeConstraint_ProofFieldHasInterval()
    {
        var text = """
            precept Test
            field Amount as number default 10 nonnegative
            """;

        var result = CompileTool.Run(text);

        result.Proof.Should().NotBeNull();
        result.Proof!.Global.Should().NotBeNull();
        result.Proof.Global!.Fields.Should().ContainKey("Amount");
        var amount = result.Proof.Global.Fields!["Amount"];
        amount.Interval.Should().NotBeNull();
        amount.Interval!.Lower.Should().Be(0);
        amount.Interval.LowerInclusive.Should().BeTrue();
        amount.Display.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PositiveConstraint_ProofFieldStrictlyAboveZero()
    {
        var text = """
            precept Test
            field Rate as number default 1 positive
            """;

        var result = CompileTool.Run(text);

        result.Proof.Should().NotBeNull();
        var rate = result.Proof!.Global!.Fields!["Rate"];
        rate.Interval.Should().NotBeNull();
        rate.Interval!.Lower.Should().Be(0);
        rate.Interval.LowerInclusive.Should().BeFalse();
    }

    [Fact]
    public void MinMaxConstraints_ProofFieldHasBoundedInterval()
    {
        var text = """
            precept Test
            field Priority as number default 1 min 1 max 10
            """;

        var result = CompileTool.Run(text);

        result.Proof.Should().NotBeNull();
        var priority = result.Proof!.Global!.Fields!["Priority"];
        priority.Interval.Should().NotBeNull();
        priority.Interval!.Lower.Should().Be(1);
        priority.Interval.LowerInclusive.Should().BeTrue();
        priority.Interval.Upper.Should().Be(10);
        priority.Interval.UpperInclusive.Should().BeTrue();
    }

    [Fact]
    public void NoNumericFields_ProofIsNull()
    {
        var text = """
            precept Test
            field Name as string default ""
            """;

        var result = CompileTool.Run(text);

        result.Proof.Should().BeNull();
    }

    [Fact]
    public void ProofSources_AttributedToConstraint()
    {
        var text = """
            precept Test
            field Amount as number default 10 nonnegative
            """;

        var result = CompileTool.Run(text);

        var amount = result.Proof!.Global!.Fields!["Amount"];
        amount.Sources.Should().NotBeEmpty();
    }

    [Fact]
    public void ComputedField_ProofIncludesComputedRange()
    {
        var text = ReadSample("computed-tax-net.precept");

        var result = CompileTool.Run(text);

        result.Proof.Should().NotBeNull();
        var fields = result.Proof!.Global!.Fields!;

        // Subtotal has positive constraint
        fields.Should().ContainKey("Subtotal");
        fields["Subtotal"].Interval.Should().NotBeNull();
        fields["Subtotal"].Interval!.Lower.Should().Be(0);
        fields["Subtotal"].Interval!.LowerInclusive.Should().BeFalse();

        // TaxRate has min 0 max 0.99
        fields.Should().ContainKey("TaxRate");
        fields["TaxRate"].Interval.Should().NotBeNull();
        fields["TaxRate"].Interval!.Lower.Should().Be(0);
        fields["TaxRate"].Interval!.Upper.Should().Be(0.99);
    }

    [Fact]
    public void TransitiveOrdering_ProofFieldsPresent()
    {
        var text = ReadSample("transitive-ordering.precept");

        var result = CompileTool.Run(text);

        result.Proof.Should().NotBeNull();
        var fields = result.Proof!.Global!.Fields!;

        // High, Mid, Low all have positive constraint
        fields.Should().ContainKey("High");
        fields.Should().ContainKey("Mid");
        fields.Should().ContainKey("Low");
    }

    [Fact]
    public void ProofDisplay_UsesNaturalLanguage()
    {
        var text = """
            precept Test
            field Rate as number default 5 min 1 max 100
            """;

        var result = CompileTool.Run(text);

        var rate = result.Proof!.Global!.Fields!["Rate"];
        rate.Display.Should().NotBeNullOrWhiteSpace();
        // Display should be natural language, not interval notation
        rate.Display.Should().NotStartWith("[");
        rate.Display.Should().NotStartWith("(");
    }

    [Fact]
    public void IntervalDto_HasStructuredBounds()
    {
        var text = """
            precept Test
            field Amount as number default 50 min 1 max 100
            """;

        var result = CompileTool.Run(text);

        var interval = result.Proof!.Global!.Fields!["Amount"].Interval!;
        interval.Lower.Should().Be(1);
        interval.LowerInclusive.Should().BeTrue();
        interval.Upper.Should().Be(100);
        interval.UpperInclusive.Should().BeTrue();
        interval.Display.Should().NotBeNullOrWhiteSpace();
    }
}
