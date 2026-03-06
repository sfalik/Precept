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
            number? RetryCount
            state A initial
            state B
            event Go
            from A on Go
              if RetryCount != null && RetryCount > 0
                transition B
              else
                reject \"blocked\"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Guard_NullCheckOrNumericComparison_UsesNarrowedType()
    {
        const string text = """
            precept M
            number? RetryCount
            state A initial
            state B
            event Go
            from A on Go
              if RetryCount == null || RetryCount > 0
                transition B
              else
                reject \"blocked\"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Guard_NullableNumericWithoutNarrowing_FailsComparison()
    {
        const string text = """
            precept M
            number? RetryCount
            state A initial
            state B
            event Go
            from A on Go
              if RetryCount > 0
                transition B
              else
                reject \"blocked\"
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
            number Value = 0
            number? RetryCount
            state A initial
            state B
            event Go
            from A on Go
              set Value = RetryCount
              transition B
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
            number Value = 0
            number? RetryCount
            state A initial
            state B
            event Go
            from A on Go
              if RetryCount != null
                set Value = RetryCount
                transition B
              else
                reject \"blocked\"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    // ── Cross-branch null narrowing ───────────────────────────────────────────

    [Fact]
    public void Diagnostics_CrossBranch_ElseIfAfterNullReject_NoFalsePositiveOnNumericComparison()
    {
        // After "if RetryCount == null -> no transition", the else-if should see RetryCount as number
        // (not number?), so "RetryCount > 0" must not produce a false-positive diagnostic.
        const string text = """
            precept M
            number? RetryCount
            state A initial
            state B
            event Go
            from A on Go
              if RetryCount == null
                no transition
              else if RetryCount > 0
                transition B
              else
                no transition
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_CrossBranch_ElseAfterNullReject_NarrowedSymbolsForSetAssignment()
    {
        // After "if RetryCount == null -> no transition", the else branch sees RetryCount as number
        // (not number?). "set Value = RetryCount" in the else branch must not be flagged.
        const string text = """
            precept M
            number Value = 0
            number? RetryCount
            state A initial
            state B
            event Go
            from A on Go
              if RetryCount == null
                no transition
              else
                set Value = RetryCount
                transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_CrossBranch_MultiChain_NoFalsePositives()
    {
        // Full three-branch chain: if null/no-transition, else if positive/transition, else/no-transition.
        // Each branch should validate cleanly with cross-branch narrowing applied.
        const string text = """
            precept M
            number? X
            state A initial
            state B
            event Go
            from A on Go
              if X == null
                no transition
              else if X > 0
                transition B
              else
                no transition
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_CrossBranch_NonNullNarrowingGuard_SubsequentBranchStillSeesNullable()
    {
        // "if SomeFlag" does not narrow Item's nullability, so the else-if should still see
        // Item as number? and flag "Item > 0" as an error.
        const string text = """
            precept M
            boolean SomeFlag = true
            number? Item
            state A initial
            state B
            event Go
            from A on Go
              if SomeFlag
                transition B
              else if Item > 0
                transition B
              else
                no transition
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
