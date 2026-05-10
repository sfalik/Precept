using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public sealed class CollectionMutationProofTests
{
    [Fact]
    public void Pop_WithCountGuard_CompilesClean()
    {
        var compilation = Compile("""
            precept PopGuarded
            field Steps as stack of string
            field LastStep as string optional
            state Active initial
            event Undo
            from Active on Undo when Steps.count > 0
                -> pop Steps into LastStep
                -> no transition
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero));
    }

    [Fact]
    public void Dequeue_WithCountGuard_CompilesClean()
    {
        var compilation = Compile("""
            precept DequeueGuarded
            field Queue as queue of string
            field Current as string optional
            state Active initial
            event Next
            from Active on Next when Queue.count > 0
                -> dequeue Queue into Current
                -> no transition
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero));
    }

    [Fact]
    public void Pop_WithNotempty_CompilesClean()
    {
        var compilation = Compile("""
            precept PopNotempty
            field Steps as stack of string notempty
            field LastStep as string optional
            state Active initial
            event Undo
            from Active on Undo
                -> pop Steps into LastStep
                -> no transition
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero));
    }

    [Fact]
    public void Pop_WithNoGuard_EmitsCorrectFieldName()
    {
        var compilation = Compile("""
            precept PopUnguarded
            field Steps as stack of string
            field LastStep as string optional
            state Active initial
            event Undo
            from Active on Undo
                -> pop Steps into LastStep
                -> no transition
            """);

        compilation.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero));

        var diagnostic = compilation.Diagnostics.Single(d => d.Code == nameof(DiagnosticCode.UnguardedCollectionMutation));
        diagnostic.Message.Should().Contain("Steps");
        diagnostic.Message.Should().NotContain("<unknown>");
    }

    private static Compilation Compile(string source)
        => Compiler.Compile(source);
}
