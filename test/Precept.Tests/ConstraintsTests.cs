using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class ConstraintsTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMeta_ReturnsForEveryConstraintKind()
    {
        foreach (var kind in Enum.GetValues<ConstraintKind>())
        {
            var meta = Constraints.GetMeta(kind);
            meta.Kind.Should().Be(kind);
            meta.Description.Should().NotBeNullOrEmpty($"{kind} must have a description");
        }
    }

    [Fact]
    public void All_ContainsEveryKindExactlyOnce()
    {
        var expected = Enum.GetValues<ConstraintKind>().ToHashSet();
        var actual = Constraints.All.Select(m => m.Kind).ToHashSet();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void All_IsInDeclarationOrder()
    {
        var kinds = Constraints.All.Select(m => (int)m.Kind).ToList();
        kinds.Should().BeInAscendingOrder();
    }

    // ── Count invariant ─────────────────────────────────────────────────────────

    [Fact]
    public void Total_Count()
    {
        Constraints.All.Should().HaveCount(5);
    }

    // ── DU subtype correctness ──────────────────────────────────────────────────

    [Fact]
    public void Invariant_IsInvariantSubtype()
    {
        Constraints.GetMeta(ConstraintKind.Invariant)
            .Should().BeOfType<ConstraintMeta.Invariant>();
    }

    [Theory]
    [InlineData(ConstraintKind.StateResident)]
    [InlineData(ConstraintKind.StateEntry)]
    [InlineData(ConstraintKind.StateExit)]
    public void StateKinds_AreStateAnchoredSubtype(ConstraintKind kind)
    {
        Constraints.GetMeta(kind).Should().BeAssignableTo<ConstraintMeta.StateAnchored>(
            $"{kind} should be a StateAnchored subtype");
    }

    [Fact]
    public void EventPrecondition_IsEventPreconditionSubtype()
    {
        Constraints.GetMeta(ConstraintKind.EventPrecondition)
            .Should().BeOfType<ConstraintMeta.EventPrecondition>();
    }

    // ── StateAnchored grouping ──────────────────────────────────────────────────

    [Fact]
    public void ThreeKinds_AreStateAnchored()
    {
        var stateAnchored = Constraints.All
            .OfType<ConstraintMeta.StateAnchored>()
            .ToList();
        stateAnchored.Should().HaveCount(3);
        stateAnchored.Select(m => m.Kind).Should().BeEquivalentTo(
        [
            ConstraintKind.StateResident,
            ConstraintKind.StateEntry,
            ConstraintKind.StateExit,
        ]);
    }

    [Fact]
    public void Invariant_IsNotStateAnchored()
    {
        Constraints.GetMeta(ConstraintKind.Invariant)
            .Should().NotBeAssignableTo<ConstraintMeta.StateAnchored>();
    }

    [Fact]
    public void EventPrecondition_IsNotStateAnchored()
    {
        Constraints.GetMeta(ConstraintKind.EventPrecondition)
            .Should().NotBeAssignableTo<ConstraintMeta.StateAnchored>();
    }

    // ── Specific subtype assertions ─────────────────────────────────────────────

    [Fact]
    public void StateResident_IsStateResidentSubtype()
    {
        Constraints.GetMeta(ConstraintKind.StateResident)
            .Should().BeOfType<ConstraintMeta.StateResident>();
    }

    [Fact]
    public void StateEntry_IsStateEntrySubtype()
    {
        Constraints.GetMeta(ConstraintKind.StateEntry)
            .Should().BeOfType<ConstraintMeta.StateEntry>();
    }

    [Fact]
    public void StateExit_IsStateExitSubtype()
    {
        Constraints.GetMeta(ConstraintKind.StateExit)
            .Should().BeOfType<ConstraintMeta.StateExit>();
    }

    // ── Pattern-match switch coverage ──────────────────────────────────────────

    [Fact]
    public void PatternMatch_CoverageIsExhaustive()
    {
        // Every kind should route to exactly one bucket
        foreach (var meta in Constraints.All)
        {
            var bucket = meta switch
            {
                ConstraintMeta.Invariant         => "always",
                ConstraintMeta.StateAnchored     => "state",
                ConstraintMeta.EventPrecondition => "event",
                _                                => "unknown",
            };
            bucket.Should().NotBe("unknown", $"{meta.Kind} should route to a known bucket");
        }
    }

    [Fact]
    public void OnlyInvariant_RoutesToAlwaysBucket()
    {
        var always = Constraints.All
            .Where(m => m is ConstraintMeta.Invariant)
            .ToList();
        always.Should().HaveCount(1);
        always[0].Kind.Should().Be(ConstraintKind.Invariant);
    }

    [Fact]
    public void OnlyEventPrecondition_RoutesToEventBucket()
    {
        var events = Constraints.All
            .Where(m => m is ConstraintMeta.EventPrecondition)
            .ToList();
        events.Should().HaveCount(1);
        events[0].Kind.Should().Be(ConstraintKind.EventPrecondition);
    }
}
