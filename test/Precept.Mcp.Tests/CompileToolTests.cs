using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class CompileToolTests
{
    [Fact]
    public void Compile_ValidStatefulPrecept_ReturnsHasErrorsFalse()
    {
        var result = CompileTool.Compile(ValidStatefulSource);

        result.HasErrors.Should().BeFalse();
        result.Definition.Should().NotBeNull();
        result.Definition!.States.Should().HaveCount(2);
        result.Definition.States.Should().Contain(state => state.Name == "Pending");
        result.Definition.States.Should().Contain(state => state.Name == "Approved");
    }

    [Fact]
    public void Compile_ValidStatefulPrecept_FieldsPopulated()
    {
        var result = CompileTool.Compile(ValidFieldSource);

        result.HasErrors.Should().BeFalse();
        result.Definition.Should().NotBeNull();

        var field = result.Definition!.Fields.Should().ContainSingle().Subject;
        field.Name.Should().Be("Nickname");
        field.TypeName.Should().Be("string");
        field.IsOptional.Should().BeTrue();
        field.IsWritable.Should().BeTrue();
        field.Modifiers.Should().Contain(new[] { "optional", "writable" });
    }

    [Fact]
    public void Compile_InvalidPrecept_ReturnsHasErrorsTrue()
    {
        var result = CompileTool.Compile(InvalidSource);

        result.HasErrors.Should().BeTrue();
        result.Diagnostics.Should().NotBeEmpty();
        result.Definition.Should().BeNull();
    }

    [Fact]
    public void Compile_InvalidPrecept_DiagnosticCodeFormat()
    {
        var result = CompileTool.Compile(InvalidSource);

        result.Diagnostics.Should().NotBeEmpty();
        result.Diagnostics.Should().OnlyContain(diagnostic =>
            diagnostic.Code.Length == 7 &&
            diagnostic.Code.StartsWith("PRE") &&
            diagnostic.Code.Skip(3).All(char.IsDigit));
    }

    [Fact]
    public void Compile_ValidPrecept_EventsAndRowsPopulated()
    {
        var result = CompileTool.Compile(ValidStatefulSource);

        var @event = result.Definition!.Events.Should().ContainSingle().Subject;
        @event.Name.Should().Be("Approve");
        @event.Rows.Should().ContainSingle();
        @event.Rows[0].FromStates.Should().Equal("Pending");
        @event.Rows[0].Guard.Should().Be("Amount > 0");
        @event.Rows[0].ToState.Should().Be("Approved");
    }

    [Fact]
    public void Compile_ValidStatelessPrecept_IsStatelessTrue()
    {
        var result = CompileTool.Compile(ValidFieldSource);

        result.HasErrors.Should().BeFalse();
        result.Definition.Should().NotBeNull();
        result.Definition!.IsStateless.Should().BeTrue();
        result.Definition.States.Should().BeEmpty();
    }

    [Fact]
    public void Compile_ValidPrecept_RulesPopulated()
    {
        var result = CompileTool.Compile(ValidStatefulSource);

        var rule = result.Definition!.Rules.Should().ContainSingle().Subject;
        rule.Expression.Should().Be("Amount > 0");
        rule.Because.Should().Contain("Loan amount must be positive");
    }

    private const string ValidFieldSource = """
        precept PaymentMethod
        field Nickname as string optional writable
        """;

    private const string ValidStatefulSource = """
        precept LoanApplication
        field Amount as number nonnegative
        state Pending initial
        state Approved terminal
        in Approved ensure Amount > 0 because "Loan amount must stay positive"
        event Approve
        from Pending on Approve when Amount > 0
            -> transition Approved
        rule Amount > 0 because "Loan amount must be positive"
        """;

    private const string InvalidSource = """
        precept Broken
        field Amount as moneys
        """;
}
