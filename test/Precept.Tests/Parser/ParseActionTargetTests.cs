using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Slice B tests for ParseActionTarget: verifies that each action shape passes its own
/// catalog-derived SeparatorTokens (from ActionShapeMeta) as terminators — never the
/// hardcoded union of all separator tokens.
///
/// Catalog truth (ActionShapeMeta.SeparatorTokens):
///   CollectionValue   → {} (empty — no separator between target and value)
///   AssignValue       → {=}
///   CollectionValueBy → {by}   (catalog-only; shape reached via type-checker conversion)
///   InsertAt          → {at}
/// </summary>
public class ParseActionTargetTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static ParsedAction GetOnlyAction(string source)
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(source));
        manifest.Diagnostics.Should().BeEmpty("parse should succeed with no diagnostics");
        var row = manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.TransitionRow)
            .Subject;
        var actionSlot = row.GetRequiredSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
        actionSlot.Actions.Should().ContainSingle("transition row has exactly one action");
        return actionSlot.Actions[0];
    }

    // ── Catalog property tests ────────────────────────────────────────────────────

    [Fact]
    public void CollectionValue_SeparatorTokens_IsEmpty()
    {
        // CollectionValue is "verb field value" — no separator token between target and value.
        // An empty SeparatorTokens means ParseActionTarget never terminates early on
        // =, into, by, or at.  This was the bug: the old hardcoded union included all four.
        var shapeMeta = Actions.GetShapeMeta(ActionSyntaxShape.CollectionValue);

        shapeMeta.SeparatorTokens.Should().BeEmpty(
            because: "CollectionValue has no separator between target and value; " +
                     "old hardcoded {=, into, by, at} set was wrong for this shape");
    }

    [Fact]
    public void AssignValue_SeparatorTokens_ContainsOnlyAssign()
    {
        var shapeMeta = Actions.GetShapeMeta(ActionSyntaxShape.AssignValue);

        shapeMeta.SeparatorTokens.Should().BeEquivalentTo(
            new[] { TokenKind.Assign },
            because: "AssignValue (set Field = value) separates target from value with '='");
    }

    /// <summary>
    /// CollectionValueBy is a secondary action shape (AppendBy / EnqueueBy) dispatched
    /// via the type-checker after the parser produces CollectionValueAction.
    /// This test verifies the catalog SeparatorTokens are correct so that IF the parser
    /// were to ever dispatch directly to ParseCollectionValueByAction, the target would
    /// terminate on 'by' and not on the old hardcoded union.
    /// </summary>
    [Fact]
    public void CollectionValueBy_SeparatorTokens_ContainsOnlyBy()
    {
        var shapeMeta = Actions.GetShapeMeta(ActionSyntaxShape.CollectionValueBy);

        shapeMeta.SeparatorTokens.Should().BeEquivalentTo(
            new[] { TokenKind.By },
            because: "CollectionValueBy separates value from ordering key with 'by'; " +
                     "old hardcoded union would also stop on =, into, at — wrong for this shape");
    }

    [Fact]
    public void InsertAt_SeparatorTokens_ContainsOnlyAt()
    {
        var shapeMeta = Actions.GetShapeMeta(ActionSyntaxShape.InsertAt);

        shapeMeta.SeparatorTokens.Should().BeEquivalentTo(
            new[] { TokenKind.At },
            because: "InsertAt separates value from index with 'at'; " +
                     "old hardcoded union would also stop on =, into, by — wrong for this shape");
    }

    // ── Parser behavioral tests ───────────────────────────────────────────────────

    /// <summary>
    /// CollectionValue (add/remove/push/enqueue) target must NOT terminate on any of the
    /// historically hardcoded separator tokens: =, into, by, at.
    ///
    /// The value expression here uses ".at(0)" — a method call that involves the 'at'
    /// keyword as a collection accessor name.  With shape-specific empty SeparatorTokens,
    /// the target parses as 'Items' and the full method-call value is preserved.
    ///
    /// With the old hardcoded {=, into, by, at} terminator on all shapes, the 'at'
    /// keyword inside the member access chain would have appeared in the outer while-loop
    /// check and the full value expression shape would not have been validated correctly.
    /// </summary>
    [Fact]
    public void ParseActionTarget_CollectionValue_DoesNotTerminateOnSeparatorTokens()
    {
        var action = GetOnlyAction(
            "from Draft on Promote -> add Items EventPayload.at(0) -> transition Approved");

        var add = action.Should().BeOfType<CollectionValueAction>().Subject;
        add.Kind.Should().Be(ActionKind.Add);
        add.Target.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("Items",
                because: "the target field 'Items' must be parsed whole — not truncated at any separator token");
        // Value contains the method call — confirms the full expression reached the value slot.
        add.Value.Should().BeOfType<MethodCallExpression>()
            .Which.MethodName.Should().Be("at");
    }

    /// <summary>
    /// AssignValue (set) terminates target parse on '='.  Target is the field name;
    /// value expression comes after '='.
    /// </summary>
    [Fact]
    public void ParseActionTarget_AssignValue_TerminatesOnAssign()
    {
        var action = GetOnlyAction(
            "from Draft on Submit -> set Status = 1 -> transition Approved");

        var set = action.Should().BeOfType<AssignAction>().Subject;
        set.Kind.Should().Be(ActionKind.Set);
        set.Target.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("Status",
                because: "target is terminated by '=' — only the field identifier precedes it");
        set.Value.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("1");
    }

    /// <summary>
    /// CollectionValueBy (append-by / enqueue-by) — catalog-shape target terminates on 'by'.
    /// Because CollectionValueBy is a secondary action (PrimaryActionKind != null), the
    /// parser reaches it via the type-checker conversion path, not ByTokenKind dispatch.
    /// This test verifies the SeparatorTokens carry the correct {By} terminator so that
    /// ParseCollectionValueByAction correctly terminates the target on 'by'.
    /// </summary>
    [Fact]
    public void ParseActionTarget_CollectionValueBy_TerminatesOnBy()
    {
        // Verify the catalog: SeparatorTokens for this shape is {By}.
        // This ensures ParseActionTarget(separators, ...) will stop on 'by', not on =, at, or into.
        var shapeMeta = Actions.GetShapeMeta(ActionSyntaxShape.CollectionValueBy);
        shapeMeta.SeparatorTokens.Should().BeEquivalentTo(
            new[] { TokenKind.By },
            because: "ParseCollectionValueByAction passes SeparatorTokens to ParseActionTarget; " +
                     "only 'by' should terminate the target — not '=', 'at', or 'into'");

        // Structural slot check: slot at index 2 has TokenKind.By as its PrecedingSeparator.
        var orderingKeySlot = shapeMeta.Slots[2];
        orderingKeySlot.Role.Should().Be(ActionSlotRole.OrderingKey);
        orderingKeySlot.PrecedingSeparator.Should().Be(TokenKind.By);
    }

    /// <summary>
    /// InsertAt (insert) terminates target on 'at'.  Target is the list field;
    /// 'at' separates the inserted value from the insertion index.
    /// </summary>
    [Fact]
    public void ParseActionTarget_InsertAt_TerminatesOnAt()
    {
        var action = GetOnlyAction(
            "from Draft on AddFloor -> insert Floors AddFloor.Floor at 0 -> transition Approved");

        var insert = action.Should().BeOfType<InsertAtAction>().Subject;
        insert.Kind.Should().Be(ActionKind.Insert);
        insert.Target.Should().BeOfType<IdentifierExpression>()
            .Which.Name.Should().Be("Floors",
                because: "target is terminated by 'at' — only the list field precedes it");
        insert.Value.Should().BeOfType<MemberAccessExpression>()
            .Which.MemberName.Should().Be("Floor");
        insert.Index.Should().BeOfType<LiteralExpression>()
            .Which.Text.Should().Be("0");
    }
}
