using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;
using Xunit;

namespace Precept.Tests;

public class ParserInfrastructureTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 2.2: Vocabulary FrozenDictionaries
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void VocabularyDictionaries_AreNonEmpty()
    {
        Parser.OperatorPrecedence.Count.Should().BeGreaterThan(0, "OperatorPrecedence should have entries");
        Parser.TypeKeywords.Count.Should().BeGreaterThan(0, "TypeKeywords should have entries");
        Parser.ModifierKeywords.Count.Should().BeGreaterThan(0, "ModifierKeywords should have entries");
        Parser.ActionKeywords.Count.Should().BeGreaterThan(0, "ActionKeywords should have entries");
    }

    [Fact]
    public void VocabularyDictionaries_ArePopulatedFromCatalogs()
    {
        // Operators
        Parser.OperatorPrecedence.Should().ContainKey(TokenKind.Plus, "+ is a binary operator");
        Parser.OperatorPrecedence.Should().ContainKey(TokenKind.Star, "* is a binary operator");
        Parser.OperatorPrecedence.Should().ContainKey(TokenKind.And, "and is a binary operator");
        Parser.OperatorPrecedence.Should().ContainKey(TokenKind.Or, "or is a binary operator");

        // Types
        Parser.TypeKeywords.Should().Contain(TokenKind.StringType, "string is a type keyword");
        Parser.TypeKeywords.Should().Contain(TokenKind.IntegerType, "integer is a type keyword");
        Parser.TypeKeywords.Should().Contain(TokenKind.MoneyType, "money is a type keyword");

        // Modifiers
        Parser.ModifierKeywords.Should().Contain(TokenKind.Nonnegative, "nonnegative is a field modifier");
        Parser.ModifierKeywords.Should().Contain(TokenKind.Positive, "positive is a field modifier");
        Parser.ModifierKeywords.Should().Contain(TokenKind.Optional, "optional is a field modifier");

        // Actions
        Parser.ActionKeywords.Should().Contain(TokenKind.Set, "set is an action keyword");
        Parser.ActionKeywords.Should().Contain(TokenKind.Add, "add is an action keyword");
        Parser.ActionKeywords.Should().Contain(TokenKind.Remove, "remove is an action keyword");
    }

    [Fact]
    public void OperatorPrecedence_ContainsOnlyBinaryOperators()
    {
        // Negate is unary — it should NOT be in the binary precedence table
        Parser.OperatorPrecedence.Should().NotContainKey(TokenKind.Not,
            "Not is a keyword unary operator — should not be in binary precedence table");
    }

    [Fact]
    public void OperatorPrecedence_PrecedenceValuesArePositive()
    {
        foreach (var kvp in Parser.OperatorPrecedence)
        {
            kvp.Value.Precedence.Should().BeGreaterThan(0,
                $"operator {kvp.Key} must have positive precedence");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 2.3: InvokeSlotParser exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EveryConstructSlotKindIsUsedByAtLeastOneConstruct()
    {
        var usedSlotKinds = Constructs.All
            .SelectMany(meta => meta.Slots)
            .Select(slot => slot.Kind)
            .Distinct()
            .ToHashSet();

        foreach (var slotKind in Enum.GetValues<ConstructSlotKind>())
        {
            usedSlotKinds.Should().Contain(slotKind,
                $"ConstructSlotKind.{slotKind} is not used by any construct — potential dead code");
        }
    }

    [Fact]
    public void InvokeSlotParser_SwitchIsExhaustive()
    {
        // Compile-time guarantee (CS8509): if a new ConstructSlotKind member is added
        // without a corresponding arm in InvokeSlotParser, the build fails.
        // This test documents the invariant — the real enforcement is the compiler.
        var enumCount = Enum.GetValues<ConstructSlotKind>().Length;
        enumCount.Should().Be(16,
            "if this count changes, verify InvokeSlotParser has a matching arm");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Slice 2.4: BuildNode exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildNodeHandlesEveryConstructKind()
    {
        foreach (var kind in Enum.GetValues<ConstructKind>())
        {
            var slotCount = Constructs.GetMeta(kind).Slots.Count;
            var slots = new SyntaxNode?[slotCount];

            // We expect the arm to exist (no ArgumentOutOfRangeException).
            // It may throw NullReferenceException or NotImplementedException
            // because slots are null — that's fine, it proves the arm exists.
            var act = () => Parser.BuildNode(kind, slots, SourceSpan.Missing);

            act.Should().NotThrow<ArgumentOutOfRangeException>(
                $"BuildNode must have an arm for ConstructKind.{kind}");
        }
    }

    [Fact]
    public void BuildNode_OmitDeclaration_CastsCorrectly()
    {
        var span = SourceSpan.Missing;
        var stateToken = new Token(TokenKind.Identifier, "Draft", span);
        var stateTarget = new StateTargetNode(span, stateToken, IsQuantifier: false);
        var fieldToken = new Token(TokenKind.Identifier, "Amount", span);
        var fieldTarget = new SingularFieldTarget(span, fieldToken);

        var slots = new SyntaxNode?[] { stateTarget, fieldTarget };
        var result = Parser.BuildNode(ConstructKind.OmitDeclaration, slots, span);

        result.Should().BeOfType<OmitDeclarationNode>();
        var omit = (OmitDeclarationNode)result;
        omit.State.Should().BeSameAs(stateTarget);
        omit.Fields.Should().BeSameAs(fieldTarget);
    }

    [Fact]
    public void BuildNode_AccessMode_CastsCorrectly()
    {
        var span = SourceSpan.Missing;
        var stateTarget = new StateTargetNode(span,
            new Token(TokenKind.Identifier, "Draft", span), false);
        var fieldTarget = new SingularFieldTarget(span,
            new Token(TokenKind.Identifier, "Amount", span));

        // AccessMode slots: [StateTarget, FieldTarget, AccessModeKeyword, GuardClause(opt)]
        // AccessModeKeyword slot returns a SyntaxNode that BuildNode converts via AsToken()
        // which throws NotImplementedException. Instead, test that the arm dispatches without
        // ArgumentOutOfRangeException by providing 4 slots.
        var slots = new SyntaxNode?[] { stateTarget, fieldTarget, stateTarget /* placeholder */, null };

        // The AsToken() call will throw NotImplementedException — that's expected.
        // The test verifies no ArgumentOutOfRangeException (arm exists).
        var act = () => Parser.BuildNode(ConstructKind.AccessMode, slots, span);
        act.Should().NotThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BuildNode_UnknownKind_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Parser.BuildNode((ConstructKind)999, [], SourceSpan.Missing);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
