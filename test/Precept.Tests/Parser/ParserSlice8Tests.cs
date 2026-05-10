using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using System.Linq;
using Xunit;

namespace Precept.Tests.Parser;

public sealed class ParserSlice8Tests
{
    private static Compilation Compile(string source) => Compiler.Compile(source);

    [Fact]
    public void Parser_Bug004_EventArgDefault_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug004
            field Total as number writable default 0
            state Draft initial
            state Done terminal
            event Submit(amount as number default 1)
            from Draft on Submit -> set Total = amount -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug005_CommaSeparatedFieldTarget_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug005
            field FirstName as string writable
            field LastName as string writable
            state Draft initial
            state Done terminal
            in Draft modify FirstName, LastName editable
            event Submit
            from Draft on Submit -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug027_ChoiceEventArgType_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug027
            state Draft initial
            state Done terminal
            event Submit(status as choice of string("Draft", "Done"))
            from Draft on Submit -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug020_GuardedStateEnsure_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug020
            field Amount as number default 1
            state Draft initial
            state Done terminal
            event Complete
            in Draft when Amount > 0 ensure Amount > 0 because "ok"
            from Draft on Complete -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug044_GuardedStateAction_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug044
            field Amount as number writable default 1
            state Draft initial
            state Done terminal
            event Submit
            to Done when Amount > 0 -> set Amount = Amount
            from Draft on Submit -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug045_LogOrderingModifier_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug045
            field History as log of string by integer ascending
            state Draft initial
            state Done terminal
            event Close
            from Draft on Close -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug031_InterpolatedRejectAndBecause_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug031
            field Amount as number default 1
            state Draft initial
            state Done terminal
            event Submit
            event Complete
            rule Amount > 0 because "Amount is {Amount}"
            from Draft on Submit -> reject "Bad amount: {Amount}"
            from Draft on Complete -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug019_TypedConstantsInExpression_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug019
            field OpenedOn as date writable default '2026-01-01'
            state Draft initial
            state Done terminal
            event Submit
            from Draft on Submit -> set OpenedOn = '2026-01-01' -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse(
            string.Join("; ", compilation.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug030_ComputedForwardReference_CompilesClean()
    {
        var compilation = Compile("""
            precept Bug030
            field Total as number <- Subtotal + 1
            field Subtotal as number default 1
            state Draft initial
            state Done terminal
            event Submit
            from Draft on Submit -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Parser_Bug030_ComputedCycle_ReportsCircularComputedField()
    {
        var compilation = Compile("""
            precept Bug030Cycle
            field A as number <- B + 1
            field B as number <- A + 1
            state Draft initial
            """);

        compilation.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.CircularComputedField));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

}
