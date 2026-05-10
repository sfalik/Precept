using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.NameBinder;

public sealed class StateWildcardTests
{
    [Fact]
    public void StateWildcard_AnyInTransitionFrom_NoError()
    {
        var compilation = Compile("""
            precept WildcardFrom
            field Flag as boolean default false
            state Draft initial
            state Done terminal
            event Toggle
            from any on Toggle -> set Flag = true -> no transition
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredState));
        compilation.Symbols.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredState));
    }

    [Fact]
    public void StateWildcard_AnyInTransitionTo_NoError()
    {
        var compilation = Compile("""
            precept WildcardTo
            field Flag as boolean default false
            state Draft initial
            state Done terminal
            event Finish
            from Draft on Finish -> transition Done
            to any -> set Flag = true
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredState));
        compilation.Symbols.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredState));
    }

    [Fact]
    public void StateWildcard_NoPRE0028Firing()
    {
        var compilation = Compile("""
            precept WildcardNoDiag
            field Flag as boolean default false
            state Draft initial
            state Done terminal
            event Toggle
            from any on Toggle -> set Flag = true -> no transition
            to any -> set Flag = false
            """);

        compilation.Symbols.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredState));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredState));
    }

    private static Compilation Compile(string source) => Compiler.Compile(source);
}
