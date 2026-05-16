using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Slice 2 tests: Parser routing for construction rows (ConstructionRow/ConstructionRowReject),
/// TransitionRowReject, PRE0014 lifting for initial events, and RejectClause slot emission.
/// </summary>
public class ParserConstructionRowTests
{
    private static ConstructManifest Parse(string source) =>
        Pipeline.Parser.Parse(Lexer.Lex(source));

    // ════════════════════════════════════════════════════════════════════════════
    //  §1. Construction row routing
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConstructionRow_EmitsCorrectKind()
    {
        // 'on <event> initial -> <actions>' must produce ConstructionRow.
        var manifest = Parse("on Start initial -> set status = \"active\"");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.ConstructionRow,
            "'on Start initial -> set ...' must route to ConstructionRow");
    }

    [Fact]
    public void ConstructionRowReject_EmitsCorrectKind()
    {
        // 'on <event> initial when <cond> -> reject "msg"' must produce ConstructionRowReject.
        var manifest = Parse("on Start initial when amount > 0 -> reject \"too low\"");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.ConstructionRowReject,
            "'on Start initial when ... -> reject ...' must route to ConstructionRowReject");
    }

    [Fact]
    public void ConstructionRow_AllowsGuard()
    {
        // Construction rows (initial modifier) must NOT emit PRE0014.
        var manifest = Parse("on Start initial when amount > 0 -> set status = \"active\"");

        manifest.Diagnostics
            .Should().NotContain(d => d.Code == nameof(DiagnosticCode.EventHandlerDoesNotSupportGuard),
                "construction rows allow guards — PRE0014 must not fire");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.ConstructionRow);
    }

    [Fact]
    public void EventRow_NoInitial_EmitsEventRow()
    {
        // 'on <event> -> <actions>' without 'initial' must produce EventRow.
        var manifest = Parse("on Pause -> set paused = true");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventRow,
            "'on Pause -> ...' (no initial) must route to EventRow");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §2. TransitionRowReject routing
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionRowReject_EmitsCorrectKind()
    {
        // 'from <state> on <event> when <cond> -> reject "msg"' must produce TransitionRowReject.
        var manifest = Parse("from Idle on Start when amount < 0 -> reject \"invalid\"");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.TransitionRowReject,
            "'from ... on ... when ... -> reject ...' must route to TransitionRowReject");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §3. RejectClause slot emission
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RejectClause_EmitsCorrectSlot()
    {
        // The reject clause in a TransitionRowReject must produce a RejectClauseSlot.
        var manifest = Parse("from Idle on Start -> reject \"not allowed\"");

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRowReject);
        var rejectSlot = row.Slots.OfType<RejectClauseSlot>().SingleOrDefault();

        rejectSlot.Should().NotBeNull("TransitionRowReject must contain a RejectClauseSlot");
        rejectSlot!.Reason.Should().Be("not allowed");
    }
}
