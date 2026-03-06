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
    // Add / Push / Enqueue value type checking

    [Fact]
    public void Diagnostics_Collection_NullableValueInAdd_ProducesError()
    {
        // string? Value cannot be added to set of string because inner type is non-nullable string.
        const string text = """
            precept M
            field Value as string nullable
            field Tags as set of string
            state A initial
            state B
            event Go
            from A on Go -> add Tags Value -> transition B
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
            field Value as string nullable
            field History as stack of string
            state A initial
            state B
            event Go
            from A on Go when Value != null -> push History Value -> transition B
            from A on Go -> reject "no value"
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostics_Collection_TypeMismatch_NumberExpressionInStringSet_ProducesError()
    {
        // Literal 42 is number; set of string inner type is string - mismatch.
        const string text = """
            precept M
            field Tags as set of string
            state A initial
            state B
            event Go
            from A on Go -> add Tags 42 -> transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("type mismatch");
    }

    // Dequeue / Pop into-field type checking

    [Fact]
    public void Diagnostics_Collection_DequeueIntoTypeMismatch_ProducesError()
    {
        // queue of string inner type is string; target field is number - mismatch.
        const string text = """
            precept M
            field Target as number default 0
            field Names as queue of string
            state A initial
            state B
            event Go
            from A on Go -> dequeue Names into Target -> transition B
            """;

        var diagnostics = Analyze(text);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Target");
        diagnostics[0].Message.Should().Contain("number");
    }

    [Fact]
    public void Diagnostics_Collection_DequeueIntoMatchingType_NoError()
    {
        // queue of string inner type is string; target field is string - valid.
        const string text = """
            precept M
            field LastName as string default ""
            field Names as queue of string
            state A initial
            state B
            event Go
            from A on Go -> dequeue Names into LastName -> transition B
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
            field Value as string nullable
            field Tags as set of string
            state A initial
            state B
            event Go
            from A on Go when Value == null -> no transition
            from A on Go -> add Tags Value -> transition B
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
