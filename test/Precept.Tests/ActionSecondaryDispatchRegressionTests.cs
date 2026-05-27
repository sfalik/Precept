using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

public sealed class ActionSecondaryDispatchRegressionTests
{
    [Fact]
    public void AppendBy_Form_CompilesCleanly()
    {
        // Per F-LANG-COLL-05 (Phase 4 W-E), AppendBy on `log of T by P` requires a
        // `when not (F contains P)` guard for ordering-key uniqueness.
        var compilation = Compiler.Compile("""
            precept AppendByRegression
            field Steps as log of string by string
            state Active initial
            state Done terminal
            event Record(Value as string, Key as string)
            event Finish
            from Active on Record
                when not (Steps contains Record.Key)
                -> append Steps Record.Value by Record.Key
                -> no transition
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
        // Per Phase 4 W-E (F-LANG-COLL-09), remove-at-N requires an explicit
        // bounds guard `when N >= 0 and N < F.count`. Prior to W-E this passed
        // with only the non-empty proof from the notempty modifier.
        var compilation = Compiler.Compile("""
            precept RemoveAtRegression
            field Steps as list of string notempty
            state Active initial
            state Done terminal
            event RemoveStep(Index as integer)
            event Finish
            from Active on RemoveStep
                when RemoveStep.Index >= 0 and RemoveStep.Index < Steps.count
                -> remove Steps at RemoveStep.Index
                -> no transition
            from Active on Finish -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Insert_PlainListField_NoModifiers_CompilesClean()
    {
        // Per Phase 4 W-E (F-LANG-COLL-09), insert-at-N requires bounds
        // `when N >= 0 and N <= F.count`. Position is integer-typed here.
        var compilation = Compiler.Compile("""
            precept TaskQueue
            field Steps as list of string
            field Position as integer default 0 nonnegative
            state Active initial
            state Done terminal
            event Add(NewStep as string, Position as integer)
            event Finish
            from Active on Add
                when Add.Position >= 0 and Add.Position <= Steps.count
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
        // Same bounds-guard requirement as the plain-list variant.
        var compilation = Compiler.Compile("""
            precept TaskQueue
            field Steps as list of string notempty
            field Position as integer default 0 nonnegative
            state Active initial
            state Done terminal
            event Add(NewStep as string, Position as integer)
            event Finish
            from Active on Add
                when Add.Position >= 0 and Add.Position <= Steps.count
                -> insert Steps Add.NewStep at Add.Position
                -> no transition
            from Active on Finish -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().BeEmpty();
    }
}
