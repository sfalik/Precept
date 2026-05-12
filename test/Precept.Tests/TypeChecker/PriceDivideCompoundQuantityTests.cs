using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// P3 -- Type algebra: price / compound-quantity -> price (dimension elevation).
///
/// Rule: price[C, X] / quantity[Y/X] -> price[C, Y]
/// Shared dimension X cancels; compound numerator Y becomes the result unit.
/// </summary>
public class PriceDivideCompoundQuantityTests
{
    private static readonly SourceSpan TestSpan = new(0, 1, 1, 1, 1, 2);

    private static CheckContext BuildContext(string preceptText)
    {
        var tokens   = Lexer.Lex(preceptText);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);
        var symbols  = Precept.Pipeline.NameBinder.Bind(manifest);
        return Precept.Pipeline.TypeChecker.CreateContext(manifest, symbols);
    }

    // == Catalog entry ============================================================

    [Fact]
    public void PriceDivideQuantity_CatalogEntry_IsCorrectShape()
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(OperationKind.PriceDivideQuantity);

        meta.Op.Should().Be(OperatorKind.Divide);
        meta.Lhs.Kind.Should().Be(TypeKind.Price);
        meta.Rhs.Kind.Should().Be(TypeKind.Quantity);
        meta.Result.Should().Be(TypeKind.Price);
        meta.ResultQualifierPolicy.Should().Be(ResultQualifierPolicy.CompoundDimensionElevation);
    }

    [Fact]
    public void PriceDivideQuantity_CatalogEntry_HasNonZeroDivisorProof()
    {
        var meta = (BinaryOperationMeta)Operations.GetMeta(OperationKind.PriceDivideQuantity);

        var numericReq = meta.ProofRequirements
            .OfType<NumericProofRequirement>()
            .FirstOrDefault();
        numericReq.Should().NotBeNull("divisor must be proven non-zero");
        numericReq!.Comparison.Should().Be(OperatorKind.NotEquals);
        numericReq.Threshold.Should().Be(0m);

        var subject = numericReq.Subject.Should().BeOfType<ParamSubject>().Subject;
        subject.Parameter.Kind.Should().Be(TypeKind.Quantity,
            "non-zero proof applies to the quantity (divisor) operand");
    }

    // == Expression resolution ====================================================

    [Fact]
    public void PriceDivideCompoundQuantity_ResolvesToPriceDivideQuantityOp()
    {
        var ctx = BuildContext("""
            precept Widget
            field ListPrice as price in 'USD' of 'mass'
            field ConvFactor as quantity in 'each/kg' default '1 each/kg'
            state Open initial
            """);

        // Resolve: ListPrice / ConvFactor
        var left  = new IdentifierExpression("ListPrice", TestSpan);
        var right = new IdentifierExpression("ConvFactor", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Slash, right, TestSpan);

        var result = Precept.Pipeline.TypeChecker.ResolveExpression(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>()
            .Which.ResolvedOp.Should().Be(OperationKind.PriceDivideQuantity);
        result.ResultType.Should().Be(TypeKind.Price);
    }

    // == Integration: assignment qualifiers =======================================

    [Fact]
    public void Price_DivideCompoundQuantity_CompilesWithoutTypeMismatch()
    {
        // price[USD, mass] / quantity[each/kg] -> price[USD, each/count]
        // Assign to any price in USD -- type-checks without TypeMismatch
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field ListPrice as price in 'USD' of 'mass'
            field ConvFactor as quantity in 'each/kg' default '1 each/kg' writable
            field CostPerUnit as price in 'USD' of 'count'
            state Open initial
            state Closed
            event Finalize
            from Open on Finalize
                -> set CostPerUnit = ListPrice / ConvFactor
                -> transition Closed
            """);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().NotContain(nameof(DiagnosticCode.TypeMismatch),
                "price / compound-quantity should resolve to a price type");
    }

    [Fact]
    public void Price_DivideCompoundQuantity_CurrencyMismatch_EmitsQualifierMismatch()
    {
        // Result carries USD but target expects EUR
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Widget
            field ListPrice as price in 'USD' of 'mass'
            field ConvFactor as quantity in 'each/kg' default '1 each/kg' writable
            field CostPerUnit as price in 'EUR' of 'count'
            state Open initial
            state Closed
            event Finalize
            from Open on Finalize
                -> set CostPerUnit = ListPrice / ConvFactor
                -> transition Closed
            """);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().Contain(nameof(DiagnosticCode.QualifierMismatch),
                "currency mismatch between result (USD) and target (EUR) must be caught");
    }
}