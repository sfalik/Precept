using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptAnalyzerCollectionMutationTests
{
    // ── Add / Push / Enqueue value type checking ─────────────────────────────

    [Fact]
    public void Diagnostics_Collection_NullableValueInAdd_ProducesError()
    {
        // string? Value cannot be added to set<string> because inner type is non-nullable string.
        const string text = """
            precept M
            string? Value
            set<string> Tags
            state A initial
            state B
            event Go
            from A on Go
              add Tags Value
              transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("value");
        diagnostics[0].Message.Should().Contain("type mismatch");
    }

    [Fact]
    public void Diagnostics_Collection_NullableValueNarrowedByGuard_Push_NoError()
    {
        // Inside "if Value != null" the guard narrows string? to string, so the push is valid.
        const string text = """
            precept M
            string? Value
            stack<string> History
            state A initial
            state B
            event Go
            from A on Go
              if Value != null
                push History Value
                transition B
              else
                reject "no value"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Collection_TypeMismatch_NumberExpressionInStringSet_ProducesError()
    {
        // Literal 42 is number; set<string> inner type is string — mismatch.
        const string text = """
            precept M
            set<string> Tags
            state A initial
            state B
            event Go
            from A on Go
              add Tags 42
              transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("type mismatch");
    }

    // ── Dequeue / Pop into-field type checking ────────────────────────────────

    [Fact]
    public void Diagnostics_Collection_DequeueIntoTypeMismatch_ProducesError()
    {
        // queue<string> inner type is string; target field is number — mismatch.
        const string text = """
            precept M
            number Target = 0
            queue<string> Names
            state A initial
            state B
            event Go
            from A on Go
              dequeue Names into Target
              transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Target");
        diagnostics[0].Message.Should().Contain("number");
    }

    [Fact]
    public void Diagnostics_Collection_DequeueIntoMatchingType_NoError()
    {
        // queue<string> inner type is string; target field is string — valid.
        const string text = """
            precept M
            string LastName = ""
            queue<string> Names
            state A initial
            state B
            event Go
            from A on Go
              dequeue Names into LastName
              transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Collection_CrossBranch_NullableAddAfterNullNoTransition_NoError()
    {
        // In the else branch, string? Value is narrowed to string (non-nullable) by the prior null guard,
        // so add Tags Value in the else branch should be valid.
        const string text = """
            precept M
            string? Value
            set<string> Tags
            state A initial
            state B
            event Go
            from A on Go
              if Value == null
                no transition
              else
                add Tags Value
                transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    private static Diagnostic[] Analyze(string text)
    {
        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetDiagnostics(uri).ToArray();
    }
}
