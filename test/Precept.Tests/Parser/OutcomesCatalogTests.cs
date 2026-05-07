using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

public class OutcomesCatalogTests
{
    [Fact]
    public void TransitionOutcome_MinimalPrecept_ParsesTransitionOutcomeAndStateName()
    {
        var compilation = CompilePrecept(
            "state Draft initial",
            "state Approved",
            "event Submit",
            "from Draft on Submit -> transition Approved");

        compilation.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();

        var outcome = GetOutcomeSlot(GetOnlyTransitionRow(compilation)).Outcome;
        outcome.Should().BeOfType<TransitionOutcome>()
            .Which.StateName.Should().Be("Approved");
    }

    [Fact]
    public void NoTransitionOutcome_MinimalPrecept_ParsesNoTransitionOutcome()
    {
        var compilation = CompilePrecept(
            "state Draft initial",
            "event Submit",
            "from Draft on Submit -> no transition");

        compilation.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();

        var outcome = GetOutcomeSlot(GetOnlyTransitionRow(compilation)).Outcome;
        outcome.Should().BeOfType<NoTransitionOutcome>();
    }

    [Fact]
    public void RejectOutcome_MinimalPrecept_ParsesRejectOutcomeAndReason()
    {
        var compilation = CompilePrecept(
            "state Draft initial",
            "event Submit",
            "from Draft on Submit -> reject \"Denied\"");

        compilation.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();

        var outcome = GetOutcomeSlot(GetOnlyTransitionRow(compilation)).Outcome;
        outcome.Should().BeOfType<RejectOutcome>()
            .Which.Reason.Should().Be("Denied");
    }

    [Fact]
    public void Outcome_GarbageTokenAfterArrow_ReturnsMalformedOutcomeAndExpectedOutcomeDiagnostic()
    {
        var manifest = ParsePrecept(
            "state Draft initial",
            "event Submit",
            "from Draft on Submit -> 42");

        var outcome = GetOutcomeSlot(GetOnlyTransitionRow(manifest)).Outcome;

        outcome.Should().BeOfType<MalformedOutcome>();
        manifest.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ExpectedOutcome));
    }

    [Fact]
    public void Outcome_MissingEntirely_ReturnsMalformedOutcomeAndExpectedOutcomeDiagnostic()
    {
        ConstructManifest? manifest = null;
        Action act = () => manifest = ParsePrecept(
            "state Draft initial",
            "event Submit",
            "from Draft on Submit");

        act.Should().NotThrow();
        manifest.Should().NotBeNull();

        var outcome = GetOutcomeSlot(GetOnlyTransitionRow(manifest!)).Outcome;

        outcome.Should().BeOfType<MalformedOutcome>();
        manifest!.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ExpectedOutcome));
    }

    [Theory]
    [InlineData("from Draft on Submit -> transition")]
    [InlineData("from Draft on Submit -> no")]
    [InlineData("from Draft on Submit -> reject")]
    public void Outcome_PartialForm_ReturnsMalformedOutcomeAndExpectedOutcomeDiagnostic(string row)
    {
        var manifest = ParsePrecept(
            "state Draft initial",
            "event Submit",
            row);

        var outcome = GetOutcomeSlot(GetOnlyTransitionRow(manifest)).Outcome;

        outcome.Should().BeOfType<MalformedOutcome>();
        manifest.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ExpectedOutcome));
    }

    [Fact]
    public void OutcomesCatalog_All_CountMatchesDeclaredOutcomeKinds()
    {
        Outcomes.All.Should().HaveCount(Enum.GetValues<OutcomeKind>().Length)
            .And.HaveCount(3);
    }

    [Fact]
    public void OutcomesCatalog_GetMeta_ReturnsForEveryOutcomeKind()
    {
        foreach (var kind in Enum.GetValues<OutcomeKind>())
        {
            var act = () => Outcomes.GetMeta(kind);

            act.Should().NotThrow(because: $"GetMeta must handle {kind}");
            Outcomes.GetMeta(kind).Kind.Should().Be(kind);
        }
    }

    [Fact]
    public void OutcomesCatalog_ByLeadingToken_CoversEveryCatalogEntry()
    {
        Outcomes.ByLeadingToken.Keys.Should().BeEquivalentTo(Outcomes.All.Select(meta => meta.LeadingToken));
        Outcomes.ByLeadingToken.Values.Select(meta => meta.Kind).Should().BeEquivalentTo(Enum.GetValues<OutcomeKind>());
    }

    [Fact]
    public void OutcomeForms_AllThreeFormsInSinglePrecept_CompileAndParseWithoutErrors()
    {
        var compilation = CompilePrecept(
            "state Draft initial",
            "state Approved",
            "event Approve",
            "event Hold",
            "event Deny",
            "from Draft on Approve -> transition Approved",
            "from Draft on Hold -> no transition",
            "from Draft on Deny -> reject \"Denied\"");

        compilation.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();

        var outcomes = GetTransitionRows(compilation.ConstructManifest)
            .Select(row => GetOutcomeSlot(row).Outcome)
            .ToList();

        outcomes.Should().HaveCount(3);
        outcomes[0].Should().BeOfType<TransitionOutcome>();
        outcomes[1].Should().BeOfType<NoTransitionOutcome>();
        outcomes[2].Should().BeOfType<RejectOutcome>();
    }

    [Fact]
    public void OutcomeForms_MixedValidAndInvalidRows_RecoveryPreservesValidOutcomes()
    {
        var manifest = ParsePrecept(
            "state Draft initial",
            "state Approved",
            "event Approve",
            "event Hold",
            "event Deny",
            "from Draft on Approve -> transition Approved",
            "from Draft on Hold -> reject",
            "from Draft on Deny -> no transition");

        var outcomes = GetTransitionRows(manifest)
            .Select(row => GetOutcomeSlot(row).Outcome)
            .ToList();

        outcomes.Should().HaveCount(3);
        outcomes[0].Should().BeOfType<TransitionOutcome>();
        outcomes[1].Should().BeOfType<MalformedOutcome>();
        outcomes[2].Should().BeOfType<NoTransitionOutcome>();
        manifest.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ExpectedOutcome));
    }

    private static Compilation CompilePrecept(params string[] bodyLines)
    {
        var source = BuildSource(bodyLines);
        return Compiler.Compile(source);
    }

    private static ConstructManifest ParsePrecept(params string[] bodyLines)
    {
        var source = BuildSource(bodyLines);
        return Precept.Pipeline.Parser.Parse(Lexer.Lex(source));
    }

    private static string BuildSource(params string[] bodyLines) =>
        string.Join(Environment.NewLine, bodyLines.Prepend("precept OutcomeDispatch"));

    private static ParsedConstruct GetOnlyTransitionRow(Compilation compilation) =>
        GetOnlyTransitionRow(compilation.ConstructManifest);

    private static ParsedConstruct GetOnlyTransitionRow(ConstructManifest manifest)
    {
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.TransitionRow);
        return manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
    }

    private static OutcomeSlot GetOutcomeSlot(ParsedConstruct row) =>
        row.Slots.OfType<OutcomeSlot>().Single();

    private static IQueryable<ParsedConstruct> GetTransitionRows(ConstructManifest manifest) =>
        manifest.Constructs.Where(c => c.Meta.Kind == ConstructKind.TransitionRow).AsQueryable();
}
