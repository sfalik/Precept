using System;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class ModifierCatalogCapabilityTests
{
    [Fact]
    public void ValueModifierMeta_UsesCanonicalTypeName()
        => ValueModifierTestAccess.RuntimeTypeName(ModifierKind.Writable)
            .Should().Be("ValueModifierMeta");

    [Fact]
    public void ValueModifierMeta_NoLongerExposesApplicableToEventArgs()
        => ValueModifierTestAccess.GetMeta(ModifierKind.Writable).RuntimeInstance.GetType()
            .GetProperty("ApplicableToEventArgs")
            .Should().BeNull();

    [Fact]
    public void ValueModifierMeta_ExposesDeclarationSiteApplicabilityShape()
    {
        var property = ValueModifierTestAccess.FindDeclarationSiteProperty(
            ValueModifierTestAccess.GetMeta(ModifierKind.Default));
        var names = Enum.GetNames(property!.PropertyType);

        property.Should().NotBeNull();
        property.PropertyType.IsEnum.Should().BeTrue();
        property.PropertyType.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Should().NotBeEmpty();
        names.Should().Contain("FieldDeclaration");
        (Array.IndexOf(names, "EventArgument") >= 0 || Array.IndexOf(names, "EventArgDeclaration") >= 0)
            .Should().BeTrue();
    }

    [Fact]
    public void Min_BoundCounterpart_IsMax()
        => CatalogCapabilityReflection.GetInstanceValue(
                Modifiers.GetMeta(ModifierKind.Min), "BoundCounterpart")
            .Should().Be(ModifierKind.Max);

    [Fact]
    public void Max_BoundCounterpart_IsMin()
        => CatalogCapabilityReflection.GetInstanceValue(
                Modifiers.GetMeta(ModifierKind.Max), "BoundCounterpart")
            .Should().Be(ModifierKind.Min);

    [Fact]
    public void Minlength_BoundCounterpart_IsMaxlength()
        => CatalogCapabilityReflection.GetInstanceValue(
                Modifiers.GetMeta(ModifierKind.Minlength), "BoundCounterpart")
            .Should().Be(ModifierKind.Maxlength);

    [Fact]
    public void Mincount_BoundCounterpart_IsMaxcount()
        => CatalogCapabilityReflection.GetInstanceValue(
                Modifiers.GetMeta(ModifierKind.Mincount), "BoundCounterpart")
            .Should().Be(ModifierKind.Maxcount);

    [Fact]
    public void Writable_ExcludesEventArgumentDeclarations()
    {
        var meta = ValueModifierTestAccess.GetMeta(ModifierKind.Writable);
        ValueModifierTestAccess.HasDeclarationSiteFlag(meta, "FieldDeclaration").Should().BeTrue();
        ValueModifierTestAccess.HasAnyDeclarationSiteFlag(meta, "EventArgument", "EventArgDeclaration").Should().BeFalse();
    }

    [Fact]
    public void Default_IncludesEventArgumentDeclarations()
    {
        var meta = ValueModifierTestAccess.GetMeta(ModifierKind.Default);
        ValueModifierTestAccess.HasDeclarationSiteFlag(meta, "FieldDeclaration").Should().BeTrue();
        ValueModifierTestAccess.HasAnyDeclarationSiteFlag(meta, "EventArgument", "EventArgDeclaration").Should().BeTrue();
    }
}
