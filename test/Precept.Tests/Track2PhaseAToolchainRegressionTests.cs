using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public sealed class Track2PhaseAToolchainRegressionTests
{
    [Fact]
    public void FromAny_TransitionRow_CompilesCleanly_WithoutUndeclaredState()
    {
        var compilation = Compile("""
            precept Bug001Regression
            field Flag as boolean default false
            state Draft initial
            state Review
            state Done terminal
            event Toggle
            event Advance
            from any on Toggle -> set Flag = true -> no transition
            from Draft on Advance -> transition Review
            from Review on Advance -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredState),
            because: "'from any' is a wildcard state target, not a named-state lookup (PRE0028)");
    }

    [Fact]
    public void ModifyAll_BroadcastTarget_CompilesCleanly_WithoutUndeclaredField()
    {
        var compilation = Compile("""
            precept Bug026Regression
            field Name as string writable
            field Amount as number
            field Notes as string optional
            state Active initial
            state Closed terminal
            in Closed modify all readonly
            event Close
            from Active on Close -> transition Closed
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField),
            because: "'modify all' is a broadcast field target, not a named-field lookup (PRE0017)");
    }

    [Fact]
    public void ModifyAll_And_OmitAll_BroadcastTargets_CompileCleanly_WithoutUndeclaredField()
    {
        var compilation = Compile("""
            precept Bug037Regression
            field Name as string default "" writable
            field Amount as number default 0
            field Notes as string optional
            state Draft initial
            state Closed terminal
            in Draft omit all
            in Closed modify all readonly
            event Close
            from Draft on Close -> transition Closed
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField),
            because: "'all' should bind as a broadcast target in both omit and modify declarations (PRE0017)");
    }

    [Fact]
    public void ListAt_WithoutNotempty_EmitsUnguardedCollectionAccess_NotDivisionByZero()
    {
        var compilation = Compile("""
            precept Bug039Regression
            field Steps as list of string
            field ThirdStep as string optional <- Steps.at(2)
            """);

        compilation.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionAccess),
            because: "collection access without a non-empty proof should use the collection-safety diagnostic");
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero),
            because: "Steps.at(2) is collection access, not arithmetic division (PRE0083)");
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ExpectedToken),
            because: "the parser should still accept '.at(...)' as a method call");
    }

    [Theory]
    [InlineData("min")]
    [InlineData("max")]
    public void DualUseBuiltinFunctions_CompileInRulePosition_WithoutExpectedToken(string functionName)
    {
        var compilation = Compile($$"""
            precept DualUseRuleRegression
            field Left as number default 0
            field Right as number default 0
            rule {{functionName}}(Left, Right) >= 0 because "msg"
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ExpectedToken),
            because: $"{functionName}(a, b) is a callable built-in in rule expressions (PRE0009)");
    }

    [Theory]
    [InlineData("min")]
    [InlineData("max")]
    public void DualUseBuiltinFunctions_CompileInGuardPosition_WithoutExpectedToken(string functionName)
    {
        var compilation = Compile($$"""
            precept DualUseGuardRegression
            field Left as number default 0
            field Right as number default 0
            state Draft initial
            state Done terminal
            event Advance
            from Draft on Advance when {{functionName}}(Left, Right) >= 0 -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ExpectedToken),
            because: $"{functionName}(a, b) is a callable built-in in transition guards (PRE0009)");
    }

    [Theory]
    [InlineData("min")]
    [InlineData("max")]
    public void DualUseBuiltinFunctions_CompileInActionPosition_WithoutExpectedToken(string functionName)
    {
        var compilation = Compile($$"""
            precept DualUseActionRegression
            field Left as number default 0 writable
            field Right as number default 0 writable
            field Result as number default 0
            state Draft initial
            state Done terminal
            event Advance
            from Draft on Advance -> set Result = {{functionName}}(Left, Right) -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.ExpectedToken),
            because: $"{functionName}(a, b) is a callable built-in in action assignment expressions (PRE0009)");
    }

    private static Compilation Compile(string source)
        => Compiler.Compile(source);
}
