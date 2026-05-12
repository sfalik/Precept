using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

public class InterpolatedQualifierTests
{
    private static ParsedTypeReference GetFieldType(string source)
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(source));
        manifest.Diagnostics.Should().BeEmpty();
        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        return field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression).TypeRef;
    }

    [Fact]
    public void FieldQualifier_InterpolatedTypedConstant_ParsesAsInterpolatedQualifier()
    {
        var typeRef = GetFieldType("field Ratio as quantity in '{StockingUnit}/{PurchaseUnit}'");

        var qualified = typeRef.Should().BeOfType<QualifiedTypeReference>().Subject;
        var qualifier = qualified.Qualifiers.Should().ContainSingle().Which;
        var interpolated = qualifier.Should().BeOfType<InterpolatedParsedQualifier>().Which.Expression;

        interpolated.Segments.Should().HaveCount(5);
        interpolated.Segments[0].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
        interpolated.Segments[1].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("StockingUnit");
        interpolated.Segments[2].Should().BeOfType<TextSegment>().Which.Text.Should().Be("/");
        interpolated.Segments[3].Should().BeOfType<HoleSegment>()
            .Which.Expression.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("PurchaseUnit");
        interpolated.Segments[4].Should().BeOfType<TextSegment>().Which.Text.Should().BeEmpty();
    }
}
