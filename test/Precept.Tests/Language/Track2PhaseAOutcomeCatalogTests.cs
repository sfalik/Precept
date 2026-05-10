using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class Track2PhaseAOutcomeCatalogTests
{
    [Theory]
    [InlineData(OutcomeKind.Transition, "transition")]
    [InlineData(OutcomeKind.NoTransition, "no transition")]
    [InlineData(OutcomeKind.Reject, "reject")]
    public void SerializedKind_MatchesExpectedValue(OutcomeKind kind, string expected)
        => Outcomes.GetMeta(kind).SerializedKind.Should().Be(expected);

    [Fact]
    public void SerializedKind_IsDistinctAcrossOutcomes()
        => Outcomes.All.Select(meta => meta.SerializedKind).Should().OnlyHaveUniqueItems();
}
