using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.NameBinder;

public sealed class ComputedFieldTests
{
    [Fact]
    public void ComputedField_ForwardReference_NoError()
    {
        var compilation = Compile("""
            precept ForwardComputed
            field Total as number <- Subtotal + 1
            field Subtotal as number default 1
            state Draft initial
            state Done terminal
            event Submit
            from Draft on Submit -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DefaultForwardReference));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.CircularComputedField));
    }

    [Fact]
    public void ComputedField_CircularReference_EmitsCircularComputedField()
    {
        var compilation = Compile("""
            precept CircularComputed
            field A as number <- B + 1
            field B as number <- A + 1
            state Draft initial
            """);

        compilation.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.CircularComputedField));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DefaultForwardReference));
    }

    private static Compilation Compile(string source) => Compiler.Compile(source);
}
