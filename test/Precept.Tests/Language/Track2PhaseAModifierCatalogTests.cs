using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class Track2PhaseAModifierCatalogTests
{
    [Fact]
    public void ValueModifierMeta_UsesCanonicalTypeName()
        => ValueModifierTestAccess.RuntimeTypeName(ModifierKind.Writable).Should().Be("ValueModifierMeta");

    [Fact]
    public void Writable_DeclarationSiteApplicability_ExcludesEventArguments()
    {
        var meta = ValueModifierTestAccess.GetMeta(ModifierKind.Writable);

        ValueModifierTestAccess.HasDeclarationSiteFlag(meta, "FieldDeclaration").Should().BeTrue();
        ValueModifierTestAccess.HasAnyDeclarationSiteFlag(meta, "EventArgument", "EventArgDeclaration").Should().BeFalse();
    }

    [Fact]
    public void Default_DeclarationSiteApplicability_IncludesEventArguments()
    {
        var meta = ValueModifierTestAccess.GetMeta(ModifierKind.Default);

        ValueModifierTestAccess.HasDeclarationSiteFlag(meta, "FieldDeclaration").Should().BeTrue();
        ValueModifierTestAccess.HasAnyDeclarationSiteFlag(meta, "EventArgument", "EventArgDeclaration").Should().BeTrue();
    }

    [Theory]
    [InlineData(ModifierKind.Min, ModifierKind.Max)]
    [InlineData(ModifierKind.Max, ModifierKind.Min)]
    [InlineData(ModifierKind.Minlength, ModifierKind.Maxlength)]
    [InlineData(ModifierKind.Maxlength, ModifierKind.Minlength)]
    [InlineData(ModifierKind.Mincount, ModifierKind.Maxcount)]
    [InlineData(ModifierKind.Maxcount, ModifierKind.Mincount)]
    public void BoundCounterpart_MatchesExpectedPair(ModifierKind kind, ModifierKind counterpart)
        => ValueModifierTestAccess.GetMeta(kind).BoundCounterpart.Should().Be(counterpart);
}
