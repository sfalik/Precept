using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Slice 4 integration tests — parse all 28 sample .precept files end-to-end
/// and verify structural guarantees across the full corpus.
///
/// §1  Sample file smoke tests (28 files, [Theory])
/// §2  Required slot completeness — all required slots present on every construct
/// §3  Span bounds — all spans within source text bounds
/// §4  Construct count sanity — > 0, &lt; 500 per file
/// §5  Specific construct spot-checks on known sample files
/// §6  Diagnostic-only parse — malformed input completes via error recovery
/// §7  No exception on empty / whitespace input
/// </summary>
public class ParserIntegrationTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Shared helper
    // ═══════════════════════════════════════════════════════════════════════════

    private static string SamplesRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    public static IEnumerable<object[]> SampleFilePaths =>
        Directory.GetFiles(SamplesRoot, "*.precept")
                 .Select(p => new object[] { p });

    private static ConstructManifest ParseFile(string path)
    {
        var text = File.ReadAllText(path);
        return Precept.Pipeline.Parser.Parse(Lexer.Lex(text));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  §1 — Sample file smoke tests
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(SampleFilePaths))]
    public void SampleFile_ParsesWithoutException_AndReturnsManifest(string path)
    {
        var manifest = ParseFile(path);

        manifest.Should().NotBeNull($"parsing {Path.GetFileName(path)} must return a manifest");
        manifest.Constructs.Length.Should().BeGreaterThan(0,
            $"{Path.GetFileName(path)} must contain at least one parsed construct");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  §2 — Required slot completeness
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(SampleFilePaths))]
    public void SampleFile_AllConstructs_HaveAllRequiredSlots(string path)
    {
        var manifest = ParseFile(path);

        foreach (var construct in manifest.Constructs)
        {
            var requiredKinds = construct.Meta.Slots
                .Where(s => s.IsRequired)
                .Select(s => s.Kind);

            foreach (var kind in requiredKinds)
            {
                construct.Slots.Should().Contain(
                    s => s.Kind == kind,
                    $"required slot {kind} must be present in {construct.Meta.Kind} " +
                    $"(file: {Path.GetFileName(path)}, span: {construct.Span})");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  §3 — Span bounds
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(SampleFilePaths))]
    public void SampleFile_AllSpans_FallWithinSourceBounds(string path)
    {
        var text = File.ReadAllText(path);
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex(text));
        var len = text.Length;

        foreach (var construct in manifest.Constructs)
        {
            construct.Span.Offset.Should().BeGreaterThanOrEqualTo(0,
                $"construct {construct.Meta.Kind} span.Offset must be >= 0 in {Path.GetFileName(path)}");
            construct.Span.End.Should().BeLessThanOrEqualTo(len,
                $"construct {construct.Meta.Kind} span.End must be <= source length in {Path.GetFileName(path)}");

            foreach (var slot in construct.Slots)
            {
                slot.Span.Offset.Should().BeGreaterThanOrEqualTo(0,
                    $"slot {slot.Kind} span.Offset must be >= 0 in {construct.Meta.Kind} ({Path.GetFileName(path)})");
                slot.Span.End.Should().BeLessThanOrEqualTo(len,
                    $"slot {slot.Kind} span.End must be <= source length in {construct.Meta.Kind} ({Path.GetFileName(path)})");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  §4 — Construct count sanity
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(SampleFilePaths))]
    public void SampleFile_ConstructCount_IsWithinSaneBounds(string path)
    {
        var manifest = ParseFile(path);

        manifest.Constructs.Length.Should().BePositive(
            $"{Path.GetFileName(path)} must contain at least one construct");
        manifest.Constructs.Length.Should().BeLessThan(500,
            $"{Path.GetFileName(path)} should not exceed 500 constructs (sanity upper bound)");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  §5 — Specific construct spot-checks
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LoanApplication_ContainsFieldDeclaration_StateDeclaration_AndTransitionRow()
    {
        var path = Path.Combine(SamplesRoot, "loan-application.precept");
        var manifest = ParseFile(path);

        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.FieldDeclaration,
            "loan-application.precept must contain at least one FieldDeclaration");
        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.StateDeclaration,
            "loan-application.precept must contain at least one StateDeclaration");
        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.TransitionRow,
            "loan-application.precept must contain at least one TransitionRow");
    }

    [Fact]
    public void CustomerProfile_ContainsRuleDeclaration_WithRuleExpressionSlot()
    {
        var path = Path.Combine(SamplesRoot, "customer-profile.precept");
        var manifest = ParseFile(path);

        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.RuleDeclaration,
            "customer-profile.precept must contain at least one RuleDeclaration");

        var rule = manifest.Constructs
            .First(c => c.Meta.Kind == ConstructKind.RuleDeclaration);

        rule.Slots.Should().Contain(s => s is RuleExpressionSlot,
            "RuleDeclaration must carry a RuleExpressionSlot");

        var ruleSlot = rule.Slots.OfType<RuleExpressionSlot>().First();
        ruleSlot.Expression.Should().NotBeNull(
            "RuleExpressionSlot.Expression must be non-null");
    }

    [Fact]
    public void TrafficLight_ContainsEventDeclaration()
    {
        var path = Path.Combine(SamplesRoot, "trafficlight.precept");
        var manifest = ParseFile(path);

        manifest.Constructs.Should().Contain(c => c.Meta.Kind == ConstructKind.EventDeclaration,
            "trafficlight.precept must contain at least one EventDeclaration");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  §6 — Diagnostic-only parse (no crash on malformed input)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MalformedInput_ProducesManifestWithoutException()
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex("field ??? garbage @@@"));

        manifest.Should().NotBeNull("error recovery must complete without throwing an exception");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  §7 — No exception on empty / whitespace input
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EmptyInput_ReturnsManifestWithoutException()
    {
        Precept.Pipeline.Parser.Parse(Lexer.Lex("")).Should().NotBeNull();
    }

    [Fact]
    public void WhitespaceOnlyInput_ReturnsManifestWithoutException()
    {
        Precept.Pipeline.Parser.Parse(Lexer.Lex("   \n\n  ")).Should().NotBeNull();
    }
}
