using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Parser routing for initial-event rows (the on-row family that includes both
/// stateless event handlers and entity-construction rows), EventRowReject,
/// TransitionRowReject, guard support for all on-rows, and RejectClause slot emission.
/// All on-rows parse as EventRow; construction-vs-handler classification happens in
/// the type checker via resolvedEvent.IsInitial.
/// </summary>
public class ParserInitialEventRowTests
{
    private static ConstructManifest Parse(string source) =>
        Pipeline.Parser.Parse(Lexer.Lex(source));

    // ════════════════════════════════════════════════════════════════════════════
    //  §1. on-row routing
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OnRow_NoReject_RoutesToEventRow()
    {
        // 'on <event> -> <actions>' parses as EventRow regardless of whether the
        // bound event is initial (type checker classifies via resolvedEvent.IsInitial).
        var manifest = Parse("on Start -> set status = \"active\"");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventRow,
            "'on Start -> set ...' must route to EventRow");
    }

    [Fact]
    public void OnRow_WithReject_RoutesToEventRowReject()
    {
        // 'on <event> when <cond> -> reject "msg"' must produce EventRowReject.
        var manifest = Parse("on Start when amount > 0 -> reject \"too low\"");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventRowReject,
            "'on Start when ... -> reject ...' must route to EventRowReject");
    }

    [Fact]
    public void OnRow_AllowsGuard()
    {
        // Guards are valid on all on-rows — PRE0014 must not fire.
        var manifest = Parse("on Start when amount > 0 -> set status = \"active\"");

        manifest.Diagnostics
            .Should().NotContain(d => d.Code == nameof(DiagnosticCode.EventHandlerDoesNotSupportGuard),
                "all on-rows allow guards — PRE0014 must not fire");

        manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventRow);
    }

    [Fact]
    public void OnRow_NoInitial_RoutesToEventRow()
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
