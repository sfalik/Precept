using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class Track2PhaseAConstructCatalogTests
{
    [Fact]
    public void StateEnsure_SupportsPreVerbWhenGuard_True()
        => Constructs.GetMeta(ConstructKind.StateEnsure).SupportsPreVerbWhenGuard.Should().BeTrue();

    [Fact]
    public void StateAction_SupportsPreVerbWhenGuard_True()
        => Constructs.GetMeta(ConstructKind.StateAction).SupportsPreVerbWhenGuard.Should().BeTrue();

    [Fact]
    public void EventEnsure_SupportsPreVerbWhenGuard_True()
        => Constructs.GetMeta(ConstructKind.EventEnsure).SupportsPreVerbWhenGuard.Should().BeTrue();

    [Fact]
    public void EventHandler_SupportsPostActionEnsure_True()
        => Constructs.GetMeta(ConstructKind.EventHandler).SupportsPostActionEnsure.Should().BeTrue();
}
