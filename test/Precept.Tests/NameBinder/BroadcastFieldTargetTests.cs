using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.NameBinder;

public sealed class BroadcastFieldTargetTests
{
    [Fact]
    public void BroadcastFieldTarget_AllInModify_NoError()
    {
        var compilation = Compile("""
            precept BroadcastModify
            field Name as string writable
            field Amount as number
            field Notes as string optional
            state Draft initial
            state Closed terminal
            in Closed modify all readonly
            event Close
            from Draft on Close -> transition Closed
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
        compilation.Symbols.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void BroadcastFieldTarget_AllInOmit_NoError()
    {
        var compilation = Compile("""
            precept BroadcastOmit
            field Name as string optional writable
            field Amount as number optional
            state Draft initial
            state Closed terminal
            in Draft omit all
            event Close
            from Draft on Close -> transition Closed
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
        compilation.Symbols.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    private static Compilation Compile(string source) => Compiler.Compile(source);
}
