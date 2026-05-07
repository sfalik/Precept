using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

public class TypeReferenceTests
{
    private static ParsedConstruct ParseSingleField(string source)
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(source));
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        return manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
    }

    [Fact]
    public void SetOfString_TypeExpressionSlot_PreservesCollectionAndElementTypes()
    {
        var field = ParseSingleField("field items as set of string");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        var typeRef = typeSlot.TypeRef.Should().BeOfType<CollectionTypeReference>().Subject;
        typeRef.CollectionType.Kind.Should().Be(TypeKind.Set);
        typeRef.KeyType.Should().BeNull();
        typeRef.ElementType.Should().BeOfType<SimpleTypeReference>()
            .Which.Type.Kind.Should().Be(TypeKind.String);
    }

    [Fact]
    public void QueueOfNumber_TypeExpressionSlot_PreservesCollectionAndElementTypes()
    {
        var field = ParseSingleField("field pending as queue of number");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        var typeRef = typeSlot.TypeRef.Should().BeOfType<CollectionTypeReference>().Subject;
        typeRef.CollectionType.Kind.Should().Be(TypeKind.QueueBy,
            "QueueType currently resolves through the catalog to the queue-by family metadata");
        typeRef.KeyType.Should().BeNull();
        typeRef.ElementType.Should().BeOfType<SimpleTypeReference>()
            .Which.Type.Kind.Should().Be(TypeKind.Number);
    }

    [Fact]
    public void StackOfBoolean_TypeExpressionSlot_PreservesCollectionAndElementTypes()
    {
        var field = ParseSingleField("field flags as stack of boolean");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        var typeRef = typeSlot.TypeRef.Should().BeOfType<CollectionTypeReference>().Subject;
        typeRef.CollectionType.Kind.Should().Be(TypeKind.Stack);
        typeRef.KeyType.Should().BeNull();
        typeRef.ElementType.Should().BeOfType<SimpleTypeReference>()
            .Which.Type.Kind.Should().Be(TypeKind.Boolean);
    }

    [Fact]
    public void ChoiceOfString_TypeExpressionSlot_PreservesElementTypeAndDomain()
    {
        var field = ParseSingleField("field status as choice of string(\"Draft\", \"Submitted\")");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        var typeRef = typeSlot.TypeRef.Should().BeOfType<ChoiceTypeReference>().Subject;
        typeRef.Type.Kind.Should().Be(TypeKind.Choice);
        typeRef.ElementType.Should().NotBeNull();
        typeRef.ElementType!.Kind.Should().Be(TypeKind.String);
        typeRef.Domain.Should().Equal("Draft", "Submitted");
    }

    [Fact]
    public void TildeString_TypeExpressionSlot_PreservesCaseInsensitiveTypeReference()
    {
        var field = ParseSingleField("field name as ~string");
        var typeSlot = field.GetRequiredSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression);

        typeSlot.TypeRef.Should().BeOfType<CITypeReference>()
            .Which.Type.Kind.Should().Be(TypeKind.String);
    }
}
