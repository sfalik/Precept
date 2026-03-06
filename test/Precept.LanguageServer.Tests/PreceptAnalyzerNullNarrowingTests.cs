using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptAnalyzerNullNarrowingTests
{
    [Fact]
    public void Diagnostics_Guard_NullCheckAndNumericComparison_UsesNarrowedType()
    {
        const string text = """
            precept M
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount != null && RetryCount > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Guard_NullCheckOrNumericComparison_UsesNarrowedType()
    {
        const string text = """
            precept M
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount == null || RetryCount > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Guard_NullableNumericWithoutNarrowing_FailsComparison()
    {
        const string text = """
            precept M
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("operator '>' requires numeric operands");
    }

    [Fact]
    public void Diagnostics_Set_NullableValueAssignedToNonNullableTarget_Fails()
    {
        const string text = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go -> set Value = RetryCount -> transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("set target 'Value' type mismatch");
    }

    [Fact]
    public void Diagnostics_Set_NullableValueNarrowedByGuard_AssignsCleanly()
    {
        const string text = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount != null -> set Value = RetryCount -> transition B
            from A on Go -> reject "blocked"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_CrossBranch_ElseIfAfterNullReject_NoFalsePositiveOnNumericComparison()
    {
        // After "if RetryCount == null -> no transition", the else-if should see RetryCount as number.
        const string text = """
            precept M
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount == null -> no transition
            from A on Go when RetryCount > 0 -> transition B
            from A on Go -> no transition
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_CrossBranch_ElseAfterNullReject_NarrowedSymbolsForSetAssignment()
    {
        // After "if RetryCount == null -> no transition", the else branch sees RetryCount as number.
        const string text = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            state A initial
            state B
            event Go
            from A on Go when RetryCount == null -> no transition
            from A on Go -> set Value = RetryCount -> transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_CrossBranch_MultiChain_NoFalsePositives()
    {
        // Three-branch chain: if null/no-transition, else if positive/transition, else/no-transition.
        const string text = """
            precept M
            field X as number nullable
            state A initial
            state B
            event Go
            from A on Go when X == null -> no transition
            from A on Go when X > 0 -> transition B
            from A on Go -> no transition
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_CrossBranch_NonNullNarrowingGuard_SubsequentBranchStillSeesNullable()
    {
        // "if SomeFlag" does not narrow Item's nullability, so "Item > 0" should still error.
        const string text = """
            precept M
            field SomeFlag as boolean default true
            field Item as number nullable
            state A initial
            state B
            event Go
            from A on Go when SomeFlag -> transition B
            from A on Go when Item > 0 -> transition B
            from A on Go -> no transition
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("operator '>' requires numeric operands");
    }

    private static Diagnostic[] Analyze(string text)
    {
        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetDiagnostics(uri).ToArray();
    }
}
