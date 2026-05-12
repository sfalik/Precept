using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

public class ParserInterpolatedQualifierTests
{
    private static ParsedConstruct ParseSingleField(string source)
    {
        var tokens = Lexer.Lex(source);
        tokens.Diagnostics.Should().BeEmpty();

        var manifest = Pipeline.Parser.Parse(tokens);
        manifest.Diagnostics.Should().BeEmpty();
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        return manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
    }

    [Fact]
    public void Field_InterpolatedInQualifier_ParsesAsQualifiedTypeReference()
    {
        var field = ParseSingleField("field Rate as money in '{Currency}'");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        var typeRef = typeSlot.TypeRef.Should().BeOfType<QualifiedTypeReference>().Subject;
        typeRef.Qualifiers.Should().ContainSingle();
        typeRef.Qualifiers[0].Preposition.Should().Be(TokenKind.In);
    }

    [Fact]
    public void Field_InterpolatedOfQualifier_ParsesAsQualifiedTypeReference()
    {
        var field = ParseSingleField("field Qty as quantity of '{Unit.dimension}'");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        var typeRef = typeSlot.TypeRef.Should().BeOfType<QualifiedTypeReference>().Subject;
        typeRef.Qualifiers.Should().ContainSingle();
        typeRef.Qualifiers[0].Preposition.Should().Be(TokenKind.Of);
    }

    [Fact]
    public void Field_InterpolatedCompoundUnitQualifier_ParsesAsQualifiedTypeReference()
    {
        var field = ParseSingleField("field Conv as quantity in '{A}/{B}'");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        var typeRef = typeSlot.TypeRef.Should().BeOfType<QualifiedTypeReference>().Subject;
        typeRef.Qualifiers.Should().ContainSingle();
        typeRef.Qualifiers[0].Preposition.Should().Be(TokenKind.In);
    }

    [Fact]
    public void EventArg_InterpolatedOfQualifier_ParsesWithoutExpectedToken()
    {
        var tokens = Lexer.Lex("event Receive(Qty as quantity of '{Unit.dimension}')");
        tokens.Diagnostics.Should().BeEmpty();

        var manifest = Pipeline.Parser.Parse(tokens);
        manifest.Diagnostics.Should().BeEmpty();
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.EventDeclaration);
    }

    [Fact]
    public void PriceField_InterpolatedInAndOfQualifiers_ParsesAsQualifiedTypeReference()
    {
        var field = ParseSingleField("field Price as price in '{Curr}' of '{Unit.dimension}'");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        var typeRef = typeSlot.TypeRef.Should().BeOfType<QualifiedTypeReference>().Subject;
        typeRef.Qualifiers.Should().HaveCount(2);
        typeRef.Qualifiers.Select(q => q.Preposition).Should().Equal(TokenKind.In, TokenKind.Of);
    }

    [Fact]
    public void Field_MalformedInterpolatedQualifier_EmitsExpectedToken()
    {
        var tokens = Lexer.Lex("field Bad as money in '{Currency'");
        var manifest = Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ExpectedToken));
    }
}
