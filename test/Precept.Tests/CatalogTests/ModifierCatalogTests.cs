using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogTests;

public sealed class ModifierCatalogTests
{
    private static readonly ModifierKind[] UniversalApplicabilityKinds =
    [
        ModifierKind.Optional,
        ModifierKind.Default,
        ModifierKind.Writable,
    ];

    public static TheoryData<ModifierKind, ModifierMeta> AllModifiers => CatalogTestReflection.AllModifiers();
    public static TheoryData<ModifierKind, ValueModifierMeta> AllValueModifiers => CatalogTestReflection.AllValueModifiers();

    [Theory]
    [MemberData(nameof(AllModifiers))]
    public void ModifierMeta_KeywordSurface_IsNonEmpty(ModifierKind kind, ModifierMeta meta)
        => CatalogTestReflection.ReadModifierKeyword(meta).Should().NotBeNullOrWhiteSpace(
            $"ModifierKind.{kind} must expose a non-empty keyword surface");

    [Theory]
    [MemberData(nameof(AllValueModifiers))]
    public void ValueModifierMeta_ApplicableTypes_AreExplicitOrUseTheUniversalSentinel(ModifierKind kind, ValueModifierMeta meta)
    {
        var applicableTypes = CatalogTestReflection.ReadApplicableTypes(meta);

        applicableTypes.Should().NotBeNull($"ModifierKind.{kind} must declare type applicability metadata");

        if (applicableTypes.Count == 0)
        {
            UniversalApplicabilityKinds.Should().Contain(kind,
                $"ModifierKind.{kind} uses an empty applicability list, which this catalog reserves as the universal-type sentinel");
            return;
        }

        applicableTypes.Should().OnlyContain(target => target != null,
            $"ModifierKind.{kind} should not contain null applicability targets");
        applicableTypes.Where(target => target.Kind.HasValue)
            .Should().OnlyContain(target => Enum.IsDefined(target.Kind!.Value),
                $"ModifierKind.{kind} should only reference declared {nameof(TypeKind)} values");
    }
}
