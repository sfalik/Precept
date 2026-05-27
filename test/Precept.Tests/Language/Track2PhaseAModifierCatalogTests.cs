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

    [Fact]
    public void WriteAccess_ApplicableAtFieldDeclarationAndStateAccessRow()
    {
        // F-LANG-GRAPH-04 Decision 5: the unified `editable` keyword (ModifierKind.Write)
        // must be applicable at BOTH the field-declaration site and per-state modify rows.
        var meta = (AccessModifierMeta)Modifiers.GetMeta(ModifierKind.Write);
        meta.ApplicableDeclarationSites.Should().HaveFlag(AccessModifierDeclarationSite.FieldDeclaration);
        meta.ApplicableDeclarationSites.Should().HaveFlag(AccessModifierDeclarationSite.StateAccessRow);
    }

    [Fact]
    public void ReadAccess_ApplicableAtStateAccessRowOnly()
    {
        // F-LANG-GRAPH-04 Decision 5: readonly remains a per-state declaration
        // (field-level read-only is expressed by *omitting* `editable`, not by writing `readonly`).
        var meta = (AccessModifierMeta)Modifiers.GetMeta(ModifierKind.Read);
        meta.ApplicableDeclarationSites.Should().Be(AccessModifierDeclarationSite.StateAccessRow);
    }

    [Fact]
    public void OmitAccess_ApplicableAtStateAccessRowOnly()
    {
        var meta = (AccessModifierMeta)Modifiers.GetMeta(ModifierKind.Omit);
        meta.ApplicableDeclarationSites.Should().Be(AccessModifierDeclarationSite.StateAccessRow);
    }
}
