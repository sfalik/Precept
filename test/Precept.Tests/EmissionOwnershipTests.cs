using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Single-stage emission-ownership behavior: each consolidated dual-emission code
/// is produced by exactly one stage, and the producing-component <see cref="DiagnosticStage"/>
/// labels are honest (binder codes → Bind, MCP tool errors → Tooling).
/// </summary>
public class EmissionOwnershipTests
{
    private static System.Collections.Generic.List<Diagnostic> Of(Compilation compilation, DiagnosticCode code) =>
        compilation.Diagnostics.Where(d => d.Code == code.ToString()).ToList();

    // ── Stage labels (honest taxonomy) ───────────────────────────────────────────

    [Theory]
    [InlineData(DiagnosticCode.DuplicateFieldName)]
    [InlineData(DiagnosticCode.DuplicateStateName)]
    [InlineData(DiagnosticCode.DuplicateEventName)]
    [InlineData(DiagnosticCode.BindingShadowsField)]
    [InlineData(DiagnosticCode.UndeclaredArg)]
    [InlineData(DiagnosticCode.UndeclaredField)]
    [InlineData(DiagnosticCode.UndeclaredState)]
    [InlineData(DiagnosticCode.UndeclaredEvent)]
    [InlineData(DiagnosticCode.CircularComputedField)]
    public void NameBinderProducedCodes_ReportBindStage(DiagnosticCode code)
    {
        Diagnostics.GetMeta(code).Stage.Should().Be(DiagnosticStage.Bind);
    }

    [Fact]
    public void McpToolInternalError_ReportsToolingStage()
    {
        Diagnostics.GetMeta(DiagnosticCode.McpToolInternalError).Stage.Should().Be(DiagnosticStage.Tooling);
    }

    // ── Single emission — name-resolution family (Bind) ──────────────────────────

    [Fact]
    public void UndeclaredField_InExpression_EmitsExactlyOnce()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            rule Amount >= 0 because "positive"
            """);

        var undeclared = Of(compilation, DiagnosticCode.UndeclaredField);
        undeclared.Should().ContainSingle(because: "one undeclared field reference must produce exactly one diagnostic");
        undeclared[0].Stage.Should().Be(DiagnosticStage.Bind);
    }

    [Fact]
    public void UndeclaredField_InSetActionTarget_EmitsExactlyOnce()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field Name as string default "x"
            state Draft initial
            state Done
            event Complete
            from Draft on Complete -> set Missing = "y" -> transition Done
            """);

        Of(compilation, DiagnosticCode.UndeclaredField)
            .Where(d => d.Args.Contains("Missing"))
            .Should().ContainSingle(because: "an undeclared set-action target must emit exactly one UndeclaredField");
    }

    [Fact]
    public void UndeclaredState_InTransitionFromList_EmitsOncePerName()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field Count as number default 0
            state Active initial
            event Submit
            from Missing1, Missing2 on Submit -> no transition
            """);

        var undeclared = Of(compilation, DiagnosticCode.UndeclaredState);
        undeclared.Should().HaveCount(2, because: "each unknown name in a from-list must emit exactly one UndeclaredState");
        undeclared.Where(d => d.Args.Contains("Missing1")).Should().ContainSingle();
        undeclared.Where(d => d.Args.Contains("Missing2")).Should().ContainSingle();
        undeclared.Should().OnlyContain(d => d.Stage == DiagnosticStage.Bind);
    }

    [Fact]
    public void UndeclaredState_InTransitionTarget_EmitsExactlyOnce()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            state Draft initial
            event Complete
            from Draft on Complete -> transition Gone
            """);

        Of(compilation, DiagnosticCode.UndeclaredState)
            .Where(d => d.Args.Contains("Gone"))
            .Should().ContainSingle(because: "an undeclared transition target state must emit exactly one UndeclaredState");
    }

    [Fact]
    public void UndeclaredEvent_InTransitionRow_EmitsExactlyOnce()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            state Draft initial
            state Done
            from Draft on Ghost -> transition Done
            """);

        Of(compilation, DiagnosticCode.UndeclaredEvent)
            .Where(d => d.Args.Contains("Ghost"))
            .Should().ContainSingle(because: "an undeclared event in a transition row must emit exactly one UndeclaredEvent");
    }

    [Fact]
    public void Undeclared_NameResolutionFamily_IsBindOwned_NeverType()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field Count as number default 0
            state Active initial
            event Submit
            from Missing on Submit -> set Nope = 1 -> transition Gone
            """);

        compilation.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.UndeclaredField)
                     || d.Code == nameof(DiagnosticCode.UndeclaredState)
                     || d.Code == nameof(DiagnosticCode.UndeclaredEvent))
            .Should().OnlyContain(d => d.Stage == DiagnosticStage.Bind,
                because: "the name-resolution family is owned exclusively by the binder");
    }

    [Fact]
    public void EventMemberNotAnArg_EmitsUndeclaredArg_NotUndeclaredField()
    {
        // Event.member where member isn't an arg of that event: the binder owns this as
        // UndeclaredArg (not UndeclaredField); the type checker defers entirely.
        var compilation = Compiler.Compile("""
            precept Widget
            field Name as string default "x"
            state Draft initial
            state Done
            event Submit(Value as string)
            from Draft on Submit -> set Name = Submit.Bogus -> transition Done
            """);

        Of(compilation, DiagnosticCode.UndeclaredArg)
            .Where(d => d.Args.Contains("Bogus"))
            .Should().ContainSingle(because: "a member that isn't an arg of a known event must emit exactly one UndeclaredArg");
        compilation.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.UndeclaredField) && d.Args.Contains("Bogus"))
            .Should().BeEmpty(because: "the type checker no longer mis-codes this position as UndeclaredField");
    }

    // ── Single emission — structural duals ───────────────────────────────────────

    [Fact]
    public void NoInitialState_EmitsExactlyOnce_FromGraph()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            state Draft
            state Done terminal
            event Complete
            from Draft on Complete -> transition Done
            """);

        var diags = Of(compilation, DiagnosticCode.NoInitialState);
        diags.Should().ContainSingle(because: "a stateful precept with no initial state must emit NoInitialState exactly once");
        diags[0].Stage.Should().Be(DiagnosticStage.Graph);
    }

    [Fact]
    public void CircularComputedField_IsBindOwned_NoTypeStageDuplicate()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field A as number <- B + 1
            field B as number <- A + 1
            """);

        var diags = Of(compilation, DiagnosticCode.CircularComputedField);
        // The binder reports one diagnostic per field participating in the cycle (its
        // existing per-field contract). The consolidation removes the type-checker's
        // duplicate DFS detector, so every CircularComputedField is now Bind-owned.
        diags.Should().NotBeEmpty();
        diags.Should().OnlyContain(d => d.Stage == DiagnosticStage.Bind,
            because: "CircularComputedField is owned exclusively by the binder");
    }

    [Fact]
    public void CircularComputedField_SelfReference_EmitsExactlyOnce_FromBind()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field A as number <- A + 1
            """);

        var diags = Of(compilation, DiagnosticCode.CircularComputedField);
        diags.Should().ContainSingle(because: "a single-field self-reference cycle must emit exactly one CircularComputedField");
        diags[0].Stage.Should().Be(DiagnosticStage.Bind);
    }
}
