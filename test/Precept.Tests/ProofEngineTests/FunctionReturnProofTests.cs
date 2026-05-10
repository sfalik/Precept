using FluentAssertions;
using Precept;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public sealed class FunctionReturnProofTests
{
    [Fact]
    public void Sqrt_Of_Abs_CompilesClean()
    {
        var compilation = Compile("""
            precept FunctionReturnProof
            field Value as number default 0 writable
            field Result as number default 0 nonnegative
            state Open initial
            state Done terminal
            event Compute
            from Open on Compute
                -> set Result = sqrt(abs(Value))
                -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    private static Compilation Compile(string source)
        => Compiler.Compile(source);
}
