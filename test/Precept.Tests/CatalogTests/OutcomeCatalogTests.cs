using System;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogTests;

public sealed class OutcomeCatalogTests
{
    public static TheoryData<OutcomeKind, OutcomeMeta> AllOutcomes => CatalogTestReflection.AllOutcomes();

    [Theory]
    [MemberData(nameof(AllOutcomes))]
    public void OutcomeMeta_Kind_IsADeclaredEnumMember(OutcomeKind kind, OutcomeMeta meta)
    {
        meta.Kind.Should().Be(kind);
        Enum.IsDefined(meta.Kind).Should().BeTrue($"OutcomeKind.{kind} should remain a declared enum member");
    }

    [Theory]
    [MemberData(nameof(AllOutcomes))]
    public void OutcomeMeta_SerializedKind_IsNonEmpty(OutcomeKind kind, OutcomeMeta meta)
        => meta.SerializedKind.Should().NotBeNullOrWhiteSpace(
            $"OutcomeKind.{kind} must declare a non-empty serialized kind for tooling and MCP output");
}
