using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class DocumentStateVersioningTests
{
    [Fact]
    public void TryUpdate_OlderVersion_DoesNotReplaceCurrentCompilation()
    {
        var state = new DocumentState();
        var currentCompilation = Compiler.Compile("""
            precept CurrentOrder
            state Draft initial
            """);
        var currentSuggestions = CreateSuggestions(
            DiagnosticCode.UndeclaredField,
            new SuggestionInfo("Quantity", "Quantitty"),
            new SourceSpan(10, 8, 1, 11, 1, 19));
        var staleCompilation = Compiler.Compile("""
            precept StaleOrder
            state Pending initial
            """);
        var staleSuggestions = CreateSuggestions(
            DiagnosticCode.UndeclaredEvent,
            new SuggestionInfo("Submit", "Submt"),
            new SourceSpan(20, 5, 2, 7, 2, 12));

        InvokeTryUpdate(state, version: 2, currentCompilation, currentSuggestions).Should().BeTrue();

        var accepted = InvokeTryUpdate(state, version: 1, staleCompilation, staleSuggestions);

        accepted.Should().BeFalse();
        state.Current.Should().Be(currentCompilation);
        state.Suggestions.Should().BeEquivalentTo(currentSuggestions);
        GetVersion(state).Should().Be(2);
    }

    [Fact]
    public void TryUpdate_NewerVersion_ReplacesCurrentCompilationAndSuggestions()
    {
        var state = new DocumentState();
        var originalCompilation = Compiler.Compile("""
            precept CurrentOrder
            state Draft initial
            """);
        var originalSuggestions = CreateSuggestions(
            DiagnosticCode.UndeclaredField,
            new SuggestionInfo("Quantity", "Quantitty"),
            new SourceSpan(10, 8, 1, 11, 1, 19));
        var newerCompilation = Compiler.Compile("""
            precept UpdatedOrder
            state Submitted initial
            """);
        var newerSuggestions = CreateSuggestions(
            DiagnosticCode.UndeclaredState,
            new SuggestionInfo("Approved", "Aproved"),
            new SourceSpan(30, 7, 2, 7, 2, 14));

        InvokeTryUpdate(state, version: 1, originalCompilation, originalSuggestions).Should().BeTrue();

        var accepted = InvokeTryUpdate(state, version: 3, newerCompilation, newerSuggestions);

        accepted.Should().BeTrue();
        state.Current.Should().Be(newerCompilation);
        state.Suggestions.Should().BeEquivalentTo(newerSuggestions);
        GetVersion(state).Should().Be(3);
    }

    private static bool InvokeTryUpdate(
        DocumentState state,
        int version,
        Compilation compilation,
        IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> suggestions)
    {
        var tryUpdate = typeof(DocumentState).GetMethod(
            "TryUpdate",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(int),
                typeof(Compilation),
                typeof(IReadOnlyDictionary<DiagnosticKey, SuggestionInfo>),
            ],
            modifiers: null);

        tryUpdate.Should().NotBeNull("Slice 26 adds version-gated document updates.");

        var result = tryUpdate!.Invoke(state, [version, compilation, suggestions]);

        result.Should().BeOfType<bool>("accepted-version publishing depends on a success signal.");
        return (bool)result!;
    }

    private static int GetVersion(DocumentState state)
    {
        var version = typeof(DocumentState).GetProperty(
            "Version",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        version.Should().NotBeNull("Slice 26 tracks the accepted document version.");

        var value = version!.GetValue(state);
        value.Should().BeOfType<int>();
        return (int)value!;
    }

    private static IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> CreateSuggestions(
        DiagnosticCode code,
        SuggestionInfo suggestion,
        SourceSpan span) =>
        new Dictionary<DiagnosticKey, SuggestionInfo>
        {
            [new DiagnosticKey(code, span)] = suggestion,
        };
}
