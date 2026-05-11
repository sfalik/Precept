using System;
using FluentAssertions;
using Precept.Language;
using Xunit;
using Xunit.Sdk;

namespace Precept.Tests.CatalogTests;

public sealed class OperatorCatalogTests
{
    public static TheoryData<OperatorKind, OperatorMeta> AllOperators => CatalogTestReflection.AllOperators();

    [Theory]
    [MemberData(nameof(AllOperators))]
    public void OperatorMeta_Kind_IsADeclaredEnumMember(OperatorKind kind, OperatorMeta meta)
    {
        meta.Kind.Should().Be(kind);
        Enum.IsDefined(meta.Kind).Should().BeTrue($"OperatorKind.{kind} should remain a declared enum member");
    }

    [Theory]
    [MemberData(nameof(AllOperators))]
    public void OperatorMeta_ResultTypePolicy_IsADeclaredEnumMember(OperatorKind kind, OperatorMeta meta)
        => Enum.IsDefined(meta.ResultTypePolicy).Should().BeTrue(
            $"OperatorKind.{kind} must declare a valid {nameof(ResultTypePolicy)}");

    [Theory]
    [MemberData(nameof(AllOperators))]
    public void OperatorMeta_SymbolSurface_IsNonEmpty(OperatorKind kind, OperatorMeta meta)
        => CatalogTestReflection.ReadOperatorSymbol(meta).Should().NotBeNullOrWhiteSpace(
            $"OperatorKind.{kind} must expose a non-empty operator symbol or keyword sequence");

    [Theory]
    [MemberData(nameof(AllOperators))]
    public void OperatorMeta_ResultTypeShape_MatchesItsPolicy(OperatorKind kind, OperatorMeta meta)
    {
        switch (meta.ResultTypePolicy)
        {
            case ResultTypePolicy.Fixed:
            case ResultTypePolicy.BothOperands:
                meta.ResultType.Should().HaveValue(
                    $"OperatorKind.{kind} uses {meta.ResultTypePolicy} and must declare a fixed result type");
                Enum.IsDefined(meta.ResultType!.Value).Should().BeTrue(
                    $"OperatorKind.{kind} must point at a declared {nameof(TypeKind)} result");
                break;

            case ResultTypePolicy.LhsType:
            case ResultTypePolicy.ElementType:
            case ResultTypePolicy.OperationResult:
                meta.ResultType.Should().BeNull(
                    $"OperatorKind.{kind} derives its result type from operands or operations and should not hardcode one");
                break;

            default:
                throw new XunitException($"Unhandled {nameof(ResultTypePolicy)} value {meta.ResultTypePolicy} for OperatorKind.{kind}.");
        }
    }
}
