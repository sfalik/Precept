using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;
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
}
