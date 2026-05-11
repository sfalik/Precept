using System;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogTests;

public sealed class TypeCatalogTests
{
    public static TheoryData<TypeKind, TypeMeta> AllTypes => CatalogTestReflection.AllTypes();

    [Theory]
    [MemberData(nameof(AllTypes))]
    public void TypeMeta_Kind_IsADeclaredEnumMember(TypeKind kind, TypeMeta meta)
    {
        meta.Kind.Should().Be(kind);
        Enum.IsDefined(meta.Kind).Should().BeTrue($"TypeKind.{kind} should remain a declared enum member");
    }

    [Theory]
    [MemberData(nameof(AllTypes))]
    public void TypeMeta_SurfaceName_IsNonEmpty(TypeKind kind, TypeMeta meta)
        => CatalogTestReflection.ReadTypeSurfaceName(meta).Should().NotBeNullOrWhiteSpace(
            $"TypeKind.{kind} must expose a non-empty serialized/display name for tooling output");
}
