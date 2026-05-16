using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Slice 2 tests: Parser routing for construction rows (ConstructionRowReject),
/// TransitionRowReject, guard support for all on-rows, and RejectClause slot emission.
/// Slice 8b: ConstructionRow is no longer produced by the parser — all on-rows parse as EventRow.
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
        // Slice 8b: 'on <event> -> <actions>' (initial classification happens at type-check time)
        // must produce EventRow — ConstructionRow is no longer emitted by the parser.
        var manifest = Parse("on Start -> set status = \"active\"");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventRow,
            "'on Start -> set ...' must route to EventRow (construction classification via type checker)");
    }

    [Fact]
    public void ConstructionRowReject_EmitsCorrectKind()
    {
        // Slice 8b: 'on <event> when <cond> -> reject "msg"' must produce ConstructionRowReject.
        var manifest = Parse("on Start when amount > 0 -> reject \"too low\"");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.ConstructionRowReject,
            "'on Start when ... -> reject ...' must route to ConstructionRowReject");
    }

    [Fact]
    public void ConstructionRow_AllowsGuard()
    {
        // Slice 8b: guards are now valid on all on-rows — PRE0014 must not fire.
        var manifest = Parse("on Start when amount > 0 -> set status = \"active\"");

        manifest.Diagnostics
            .Should().NotContain(d => d.Code == nameof(DiagnosticCode.EventHandlerDoesNotSupportGuard),
                "all on-rows allow guards — PRE0014 must not fire");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventRow);
    }

    [Fact]
    public void EventRow_NoInitial_EmitsEventRow()
    {
        // 'on <event> -> <actions>' must produce EventRow.
        var manifest = Parse("on Pause -> set paused = true");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventRow,
            "'on Pause -> ...' must route to EventRow");
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
