using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

public sealed class ActionSecondaryDispatchRegressionTests
{
    [Fact]
    public void AppendBy_Form_CompilesCleanly()
    {
        var compilation = Compiler.Compile("""
            precept AppendByRegression
            field Steps as log of string by string
            state Active initial
            state Done terminal
            event Record(Value as string, Key as string)
            event Finish
            from Active on Record -> append Steps Record.Value by Record.Key -> no transition
            from Active on Finish -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void EnqueueBy_Form_CompilesCleanly()
    {
        var compilation = Compiler.Compile("""
            precept EnqueueByRegression
            field Queue as queue of string by integer
            state Active initial
            state Done terminal
            event Rank(Value as string, Priority as integer)
            event Finish
            from Active on Rank -> enqueue Queue Rank.Value by Rank.Priority -> no transition
            from Active on Finish -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void RemoveAt_Form_CompilesCleanly()
    {
        var compilation = Compiler.Compile("""
            precept RemoveAtRegression
            field Steps as list of string notempty
            state Active initial
            state Done terminal
            event RemoveStep(Index as integer)
            event Finish
            from Active on RemoveStep -> remove Steps at RemoveStep.Index -> no transition
            from Active on Finish -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Insert_PlainListField_NoModifiers_CompilesClean()
    {
        var compilation = Compiler.Compile("""
            precept TaskQueue
            field Steps as list of string
            field Position as number default 0 nonnegative
            state Active initial
            state Done terminal
            event Add(NewStep as string, Position as number)
            event Finish
            from Active on Add
                -> insert Steps Add.NewStep at Add.Position
                -> no transition
            from Active on Finish -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Insert_WithNotemptyField_CompilesClean()
    {
        var compilation = Compiler.Compile("""
            precept TaskQueue
            field Steps as list of string notempty
            field Position as number default 0 nonnegative
            state Active initial
            state Done terminal
            event Add(NewStep as string, Position as number)
            event Finish
            from Active on Add
                -> insert Steps Add.NewStep at Add.Position
                -> no transition
            from Active on Finish -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }
}
