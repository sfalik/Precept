using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

/// <summary>
/// Tests for hover proof section integration (Step 14).
/// Verifies Elaine's UX requirements: natural language, no interval notation,
/// no compiler internals, attribution required.
/// </summary>
public class PreceptHoverProofTests
{
    [Fact]
    public void Hover_FieldWithMinMax_ShowsProvenSafeWithAttribution()
    {
        const string text = """
            precept M
            field Ra$$te as number default 50 min 1 max 100
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().Contain("Proven safe:");
        content.Should().Contain("1 to 100 (inclusive)");
        content.Should().Contain("from:");
        content.Should().Contain("field constraint: min 1");
        content.Should().Contain("field constraint: max 100");
    }

    [Fact]
    public void Hover_FieldReference_WithMinMax_ShowsProvenSafe()
    {
        const string text = """
            precept M
            field Rate as number default 50 min 1 max 100
            state A initial
            event Go
            from A on Go when Ra$$te > 0 -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().Contain("Proven safe:");
        content.Should().Contain("1 to 100 (inclusive)");
    }

    [Fact]
    public void Hover_FieldWithNoConstraints_NoProofSection()
    {
        const string text = """
            precept M
            field Bal$$ance as number default 0
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().NotContain("Proven safe:");
    }

    [Fact]
    public void Hover_FieldWithPositiveConstraint_ShowsAlwaysGreaterThanZero()
    {
        const string text = """
            precept M
            field Amo$$unt as number default 5 positive
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().Contain("Proven safe:");
        content.Should().Contain("always greater than 0");
        content.Should().Contain("field constraint: positive");
    }

    [Fact]
    public void Hover_FieldWithNonnegativeConstraint_ShowsZeroOrGreater()
    {
        const string text = """
            precept M
            field Sco$$re as number default 0 nonnegative
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().Contain("Proven safe:");
        content.Should().Contain("0 or greater");
        content.Should().Contain("field constraint: nonnegative");
    }

    [Fact]
    public void Hover_NoIntervalNotation_InHoverOutput()
    {
        // Elaine UX rule #1: No interval notation like (0, +∞) or [1, 100]
        const string text = """
            precept M
            field Ra$$te as number default 50 min 1 max 100
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().NotContain("(0,");
        content.Should().NotContain("[1,");
        content.Should().NotContain("+∞");
        content.Should().NotContain("-∞");
    }

    [Fact]
    public void Hover_NoCompilerInternals_InHoverOutput()
    {
        // Elaine UX rule #2: No compiler internals
        const string text = """
            precept M
            field Ra$$te as number default 50 min 1 max 100
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().NotContain("LinearForm");
        content.Should().NotContain("RelationalGraph");
        content.Should().NotContain("ProofContext");
    }

    [Fact]
    public void Hover_ProofSectionSeparatedByHorizontalRule()
    {
        const string text = """
            precept M
            field Ra$$te as number default 50 min 1 max 100
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        // Proof section is separated from existing content by ---
        content.Should().Contain("---");
        // Existing field type info still present
        content.Should().Contain("number");
        content.Should().Contain("Rate");
    }

    [Fact]
    public void Hover_StringField_NoProofSection()
    {
        const string text = """
            precept M
            field Na$$me as string default ""
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().NotContain("Proven safe:");
    }

    [Fact]
    public void Hover_StatelessPrecept_FieldWithMinMax_ShowsProof()
    {
        const string text = """
            precept M
            field Ra$$te as number default 50 min 1 max 100
            """;

        var (code, position) = ExtractPosition(text);
        var info = PreceptDocumentIntellisense.Analyze(code);
        var hover = PreceptDocumentIntellisense.CreateHover(info, position);

        hover.Should().NotBeNull();
        var content = hover!.Contents.ToString()!;
        content.Should().Contain("Proven safe:");
        content.Should().Contain("1 to 100 (inclusive)");
    }

    private static (string text, Position position) ExtractPosition(string textWithMarker)
    {
        var index = textWithMarker.IndexOf("$$", StringComparison.Ordinal);
        index.Should().BeGreaterThanOrEqualTo(0);

        var text = textWithMarker.Replace("$$", string.Empty, StringComparison.Ordinal);
        var prefix = textWithMarker[..index];
        var line = prefix.Count(static ch => ch == '\n');
        var lastNewLine = prefix.LastIndexOf('\n');
        var character = lastNewLine >= 0 ? prefix.Length - lastNewLine - 1 : prefix.Length;
        return (text, new Position(line, character));
    }
}
