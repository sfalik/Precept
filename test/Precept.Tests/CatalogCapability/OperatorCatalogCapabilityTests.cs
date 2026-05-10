using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class OperatorCatalogCapabilityTests
{
    [Theory]
    [InlineData(OperatorKind.Or, ResultTypePolicy.BothOperands)]
    [InlineData(OperatorKind.And, ResultTypePolicy.BothOperands)]
    [InlineData(OperatorKind.Not, ResultTypePolicy.Fixed)]
    [InlineData(OperatorKind.Contains, ResultTypePolicy.Fixed)]
    [InlineData(OperatorKind.IsSet, ResultTypePolicy.Fixed)]
    [InlineData(OperatorKind.IsNotSet, ResultTypePolicy.Fixed)]
    public void BooleanOperators_DeclareBooleanResultTypes(OperatorKind kind, ResultTypePolicy policy)
    {
        var meta = Operators.GetMeta(kind);

        meta.ResultType.Should().Be(TypeKind.Boolean);
        meta.ResultTypePolicy.Should().Be(policy);
    }

    [Fact]
    public void LookupAccess_ResultTypePolicy_IsElementType()
    {
        var meta = Operators.GetMeta(OperatorKind.LookupAccess);

        meta.ResultType.Should().BeNull();
        meta.ResultTypePolicy.Should().Be(ResultTypePolicy.ElementType);
    }

    [Theory]
    [InlineData(OperatorKind.Plus)]
    [InlineData(OperatorKind.Minus)]
    [InlineData(OperatorKind.Times)]
    [InlineData(OperatorKind.Divide)]
    [InlineData(OperatorKind.Modulo)]
    public void BinaryArithmeticOperators_UseOperationResultPolicy(OperatorKind kind)
    {
        var meta = Operators.GetMeta(kind);

        meta.ResultType.Should().BeNull();
        meta.ResultTypePolicy.Should().Be(ResultTypePolicy.OperationResult);
    }

    [Fact]
    public void Negate_ResultTypePolicy_IsLhsType()
    {
        var meta = Operators.GetMeta(OperatorKind.Negate);

        meta.ResultType.Should().BeNull();
        meta.ResultTypePolicy.Should().Be(ResultTypePolicy.LhsType);
    }

    [Fact]
    public void FixedAndBothOperandsPolicies_DeclareResultTypes()
    {
        Operators.All
            .Where(meta => meta.ResultTypePolicy == ResultTypePolicy.Fixed
                || meta.ResultTypePolicy == ResultTypePolicy.BothOperands)
            .Should().OnlyContain(meta => meta.ResultType != null);
    }

    [Fact]
    public void DerivedPolicies_LeaveResultTypeNull()
    {
        Operators.All
            .Where(meta => meta.ResultTypePolicy == ResultTypePolicy.ElementType
                || meta.ResultTypePolicy == ResultTypePolicy.LhsType
                || meta.ResultTypePolicy == ResultTypePolicy.OperationResult)
            .Should().OnlyContain(meta => meta.ResultType == null);
    }
}
