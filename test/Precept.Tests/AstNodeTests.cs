using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;
using Precept.Pipeline.SyntaxNodes.Expressions;
using Xunit;

namespace Precept.Tests;

public class AstNodeTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 2.1a: Base types + FieldTargetNode DU
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FieldTargetNode_IsAbstract()
    {
        typeof(FieldTargetNode).IsAbstract.Should().BeTrue(
            "FieldTargetNode is the DU base — must be abstract");
    }

    [Fact]
    public void FieldTargetNode_SingularSubtype_IsSealed()
    {
        typeof(SingularFieldTarget).IsSealed.Should().BeTrue();
        typeof(SingularFieldTarget).IsAssignableTo(typeof(FieldTargetNode)).Should().BeTrue();
    }

    [Fact]
    public void FieldTargetNode_ListSubtype_IsSealed()
    {
        typeof(ListFieldTarget).IsSealed.Should().BeTrue();
        typeof(ListFieldTarget).IsAssignableTo(typeof(FieldTargetNode)).Should().BeTrue();
    }

    [Fact]
    public void FieldTargetNode_AllSubtype_IsSealed()
    {
        typeof(AllFieldTarget).IsSealed.Should().BeTrue();
        typeof(AllFieldTarget).IsAssignableTo(typeof(FieldTargetNode)).Should().BeTrue();
    }

    [Fact]
    public void SyntaxNodeHierarchy_DeclarationExtendsBase()
    {
        typeof(Declaration).IsAssignableTo(typeof(SyntaxNode)).Should().BeTrue();
        typeof(Declaration).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void SyntaxNodeHierarchy_StatementExtendsBase()
    {
        typeof(Statement).IsAssignableTo(typeof(SyntaxNode)).Should().BeTrue();
        typeof(Statement).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void SyntaxNodeHierarchy_ExpressionExtendsBase()
    {
        typeof(Expression).IsAssignableTo(typeof(SyntaxNode)).Should().BeTrue();
        typeof(Expression).IsAbstract.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 2.1b: Declaration nodes
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly Type[] ExpectedDeclarationNodes =
    [
        typeof(PreceptHeaderNode),
        typeof(FieldDeclarationNode),
        typeof(StateDeclarationNode),
        typeof(EventDeclarationNode),
        typeof(RuleDeclarationNode),
        typeof(TransitionRowNode),
        typeof(StateEnsureNode),
        typeof(AccessModeNode),
        typeof(OmitDeclarationNode),
        typeof(StateActionNode),
        typeof(EventEnsureNode),
        typeof(EventHandlerNode),
    ];

    [Fact]
    public void AllDeclarationNodes_AreSealedRecords()
    {
        foreach (var type in ExpectedDeclarationNodes)
        {
            type.IsSealed.Should().BeTrue($"{type.Name} must be sealed");
            type.IsAssignableTo(typeof(Declaration)).Should().BeTrue(
                $"{type.Name} must inherit Declaration");
            // Records have a compiler-generated EqualityContract property
            type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance)
                .Should().NotBeNull($"{type.Name} must be a record (has EqualityContract)");
        }
    }

    [Fact]
    public void DeclarationNodeCount_Matches_ConstructKindCount()
    {
        var concreteDeclarationTypes = typeof(Declaration).Assembly
            .GetTypes()
            .Where(t => t.IsSealed && !t.IsAbstract && typeof(Declaration).IsAssignableFrom(t))
            .ToList();

        var constructKindCount = Enum.GetValues<ConstructKind>().Length;

        concreteDeclarationTypes.Should().HaveCount(constructKindCount,
            $"there should be exactly one Declaration subtype per ConstructKind ({constructKindCount})");
    }

    [Fact]
    public void FieldTargetNode_SubtypesHaveCorrectProperties()
    {
        // SingularFieldTarget has Name (Token)
        typeof(SingularFieldTarget).GetProperty("Name")!.PropertyType
            .Should().Be(typeof(Token));

        // ListFieldTarget has Names (ImmutableArray<Token>)
        typeof(ListFieldTarget).GetProperty("Names")!.PropertyType
            .Should().Be(typeof(ImmutableArray<Token>));

        // AllFieldTarget has AllToken (Token)
        typeof(AllFieldTarget).GetProperty("AllToken")!.PropertyType
            .Should().Be(typeof(Token));
    }

    [Fact]
    public void OmitDeclarationNode_HasExactlyTwoSlotProperties()
    {
        var type = typeof(OmitDeclarationNode);

        type.GetProperty("State").Should().NotBeNull();
        type.GetProperty("State")!.PropertyType.Should().Be(typeof(StateTargetNode));

        type.GetProperty("Fields").Should().NotBeNull();
        type.GetProperty("Fields")!.PropertyType.Should().Be(typeof(FieldTargetNode));

        // Must NOT have Mode or Guard
        type.GetProperty("Mode").Should().BeNull("OmitDeclarationNode has no Mode");
        type.GetProperty("Guard").Should().BeNull("OmitDeclarationNode has no Guard");
    }

    [Fact]
    public void AccessModeNode_HasFourSlotProperties()
    {
        var type = typeof(AccessModeNode);

        type.GetProperty("State").Should().NotBeNull();
        type.GetProperty("State")!.PropertyType.Should().Be(typeof(StateTargetNode));

        type.GetProperty("Fields").Should().NotBeNull();
        type.GetProperty("Fields")!.PropertyType.Should().Be(typeof(FieldTargetNode));

        type.GetProperty("Mode").Should().NotBeNull();
        type.GetProperty("Mode")!.PropertyType.Should().Be(typeof(Token));

        type.GetProperty("Guard").Should().NotBeNull();
        type.GetProperty("Guard")!.PropertyType.Should().Be(typeof(Expression));
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  TypeRefNode — new parameterized forms (Slices 1-7b)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ScalarTypeRefNode_CaseInsensitive_DefaultsFalse()
    {
        var span = SourceSpan.Missing;
        var tok = new Token(TokenKind.StringType, "string", span);
        var node = new ScalarTypeRefNode(span, tok, []);
        node.CaseInsensitive.Should().BeFalse("CaseInsensitive defaults to false");
    }

    [Fact]
    public void LogByTypeRefNode_IsSealed_AndExtendsTypeRefNode()
    {
        typeof(LogByTypeRefNode).IsSealed.Should().BeTrue();
        typeof(LogByTypeRefNode).IsAssignableTo(typeof(TypeRefNode)).Should().BeTrue();
    }

    [Fact]
    public void LogByTypeRefNode_HasRequiredFields()
    {
        var t = typeof(LogByTypeRefNode);
        t.GetProperty("ElementType")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("OrderingKeyType")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("CaseInsensitive")!.PropertyType.Should().Be(typeof(bool));
        t.GetProperty("Qualifiers").Should().NotBeNull();
    }

    [Fact]
    public void QueueByTypeRefNode_IsSealed_AndExtendsTypeRefNode()
    {
        typeof(QueueByTypeRefNode).IsSealed.Should().BeTrue();
        typeof(QueueByTypeRefNode).IsAssignableTo(typeof(TypeRefNode)).Should().BeTrue();
    }

    [Fact]
    public void QueueByTypeRefNode_HasSortDirectionField()
    {
        var t = typeof(QueueByTypeRefNode);
        t.GetProperty("SortDirection")!.PropertyType.Should().Be(typeof(SortDirection));
        t.GetProperty("ElementType")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("OrderingKeyType")!.PropertyType.Should().Be(typeof(Token));
    }

    [Fact]
    public void LookupTypeRefNode_IsSealed_AndExtendsTypeRefNode()
    {
        typeof(LookupTypeRefNode).IsSealed.Should().BeTrue();
        typeof(LookupTypeRefNode).IsAssignableTo(typeof(TypeRefNode)).Should().BeTrue();
    }

    [Fact]
    public void LookupTypeRefNode_HasKeyTypeAndValueType()
    {
        var t = typeof(LookupTypeRefNode);
        t.GetProperty("KeyType")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("ValueType")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("CaseInsensitive")!.PropertyType.Should().Be(typeof(bool));
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  New action statement nodes (Slices 1-7b)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AppendStatement_HasFieldAndValue()
    {
        var t = typeof(AppendStatement);
        t.IsSealed.Should().BeTrue();
        t.IsAssignableTo(typeof(Statement)).Should().BeTrue();
        t.GetProperty("Field")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Value")!.PropertyType.Should().Be(typeof(Expression));
    }

    [Fact]
    public void AppendByStatement_HasFieldValueAndKey()
    {
        var t = typeof(AppendByStatement);
        t.IsSealed.Should().BeTrue();
        t.IsAssignableTo(typeof(Statement)).Should().BeTrue();
        t.GetProperty("Field")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Value")!.PropertyType.Should().Be(typeof(Expression));
        t.GetProperty("Key")!.PropertyType.Should().Be(typeof(Expression));
    }

    [Fact]
    public void InsertStatement_HasFieldValueAndIndex()
    {
        var t = typeof(InsertStatement);
        t.IsSealed.Should().BeTrue();
        t.IsAssignableTo(typeof(Statement)).Should().BeTrue();
        t.GetProperty("Field")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Value")!.PropertyType.Should().Be(typeof(Expression));
        t.GetProperty("Index")!.PropertyType.Should().Be(typeof(Expression));
    }

    [Fact]
    public void RemoveAtStatement_HasFieldAndIndex()
    {
        var t = typeof(RemoveAtStatement);
        t.IsSealed.Should().BeTrue();
        t.IsAssignableTo(typeof(Statement)).Should().BeTrue();
        t.GetProperty("Field")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Index")!.PropertyType.Should().Be(typeof(Expression));
    }

    [Fact]
    public void PutStatement_HasFieldKeyAndValue()
    {
        var t = typeof(PutStatement);
        t.IsSealed.Should().BeTrue();
        t.IsAssignableTo(typeof(Statement)).Should().BeTrue();
        t.GetProperty("Field")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Key")!.PropertyType.Should().Be(typeof(Expression));
        t.GetProperty("Value")!.PropertyType.Should().Be(typeof(Expression));
    }

    [Fact]
    public void EnqueueByStatement_HasFieldValueAndPriority()
    {
        var t = typeof(EnqueueByStatement);
        t.IsSealed.Should().BeTrue();
        t.IsAssignableTo(typeof(Statement)).Should().BeTrue();
        t.GetProperty("Field")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Value")!.PropertyType.Should().Be(typeof(Expression));
        t.GetProperty("Priority")!.PropertyType.Should().Be(typeof(Expression));
    }

    [Fact]
    public void DequeueByStatement_HasFieldAndOptionalInto()
    {
        var t = typeof(DequeueByStatement);
        t.IsSealed.Should().BeTrue();
        t.IsAssignableTo(typeof(Statement)).Should().BeTrue();
        t.GetProperty("Field")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("IntoField")!.PropertyType.Should().Be(typeof(Token?));
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Quantifier and CIFunctionCall expression nodes (Slices 1-7b)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void QuantifierExpression_IsSealed_AndExtendsExpression()
    {
        typeof(QuantifierExpression).IsSealed.Should().BeTrue();
        typeof(QuantifierExpression).IsAssignableTo(typeof(Expression)).Should().BeTrue();
    }

    [Fact]
    public void QuantifierExpression_HasQuantifierBindingCollectionPredicate()
    {
        var t = typeof(QuantifierExpression);
        t.GetProperty("Quantifier")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Binding")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Collection")!.PropertyType.Should().Be(typeof(Expression));
        t.GetProperty("Predicate")!.PropertyType.Should().Be(typeof(Expression));
    }

    [Fact]
    public void CIFunctionCallExpression_IsSealed_AndExtendsExpression()
    {
        typeof(CIFunctionCallExpression).IsSealed.Should().BeTrue();
        typeof(CIFunctionCallExpression).IsAssignableTo(typeof(Expression)).Should().BeTrue();
    }

    [Fact]
    public void CIFunctionCallExpression_HasFunctionNameSubjectAndArgument()
    {
        var t = typeof(CIFunctionCallExpression);
        t.GetProperty("FunctionName")!.PropertyType.Should().Be(typeof(Token));
        t.GetProperty("Subject")!.PropertyType.Should().Be(typeof(Expression));
        t.GetProperty("Argument")!.PropertyType.Should().Be(typeof(Expression));
    }
}
