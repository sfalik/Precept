using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

public class ParserBackArrowTests
{
    private static ConstructManifest Parse(string source) => Precept.Pipeline.Parser.Parse(Lexer.Lex(source));

    private static ParsedConstruct ParseField(string source)
    {
        var manifest = Parse(source);
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        return manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
    }

    [Fact]
    public void ComputedField_BackArrow_BasicNumericExpression_ParsesWithoutErrors()
    {
        var manifest = Parse("field amount as number <- amount + 1");

        manifest.Diagnostics.Should().BeEmpty();
        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var compute = field.Slots.Should().ContainSingle(s => s.Kind == ConstructSlotKind.ComputeExpression).Subject;

        compute.Should().BeOfType<ComputeExpressionSlot>()
            .Which.Expression.Should().BeOfType<BinaryOperationExpression>();
    }

    [Fact]
    public void ComputedField_BackArrow_StringPrefixExpression_ParsesWithoutErrors()
    {
        var manifest = Parse("field label as string <- \"prefix_\" + name");

        manifest.Diagnostics.Should().BeEmpty();
        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var compute = field.Slots.OfType<ComputeExpressionSlot>().Single();

        compute.Expression.Should().BeOfType<BinaryOperationExpression>();
        compute.Span.Should().NotBe(SourceSpan.Missing);
    }

    [Fact]
    public void ComputedField_BackArrow_MultiOperandExpression_ParsesWithoutErrors()
    {
        var manifest = Parse("field full as string <- first + \" \" + last");

        manifest.Diagnostics.Should().BeEmpty();
        var field = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.FieldDeclaration);
        var compute = field.Slots.OfType<ComputeExpressionSlot>().Single();

        compute.Expression.Should().BeOfType<BinaryOperationExpression>();
        compute.Span.Should().NotBe(SourceSpan.Missing);
    }

    [Fact]
    public void ComputedField_BackArrow_MultiFieldPrecept_ParsesOneComputedFieldAmongPlainFields()
    {
        var source = string.Join("\n",
            "precept Person",
            "field first as string",
            "field last as string",
            "field full as string <- first + \" \" + last");

        var manifest = Parse(source);

        manifest.Diagnostics.Should().BeEmpty();

        var fields = manifest.Constructs
            .Where(c => c.Meta.Kind == ConstructKind.FieldDeclaration)
            .ToList();

        fields.Should().HaveCount(3);
        fields.Count(field => field.Slots.Any(s => s.Kind == ConstructSlotKind.ComputeExpression)).Should().Be(1);
        fields.Should().Contain(field => field.Slots.OfType<IdentifierListSlot>().Single().Names.Single() == "full"
            && field.Slots.Any(s => s.Kind == ConstructSlotKind.ComputeExpression));
    }

    [Fact]
    public void TransitionRow_Arrow_OutcomeSeparator_StillParsesWithoutErrors()
    {
        var manifest = Parse("from Draft on Submit -> transition Submitted");

        manifest.Diagnostics.Should().BeEmpty();
        manifest.Constructs.Should().ContainSingle(c => c.Meta.Kind == ConstructKind.TransitionRow);
    }

    [Fact]
    public void EventHandler_Arrow_ActionChain_StillParsesWithoutErrors()
    {
        var manifest = Parse("on UpdateName -> set name = newName");

        manifest.Diagnostics.Should().BeEmpty();
        var handler = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.EventHandler);
        handler.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.ActionChain);
    }

    [Fact]
    public void TransitionRow_Arrow_ActionChainAndOutcome_StillParseTogether()
    {
        var manifest = Parse("from Draft on Submit -> set amount = 1 -> transition Submitted");

        manifest.Diagnostics.Should().BeEmpty();
        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.ActionChain);
        row.Slots.Should().Contain(s => s.Kind == ConstructSlotKind.Outcome);
    }

    [Fact]
    public void BackArrow_UsedAsActionChainSeparator_ProducesParseError()
    {
        var manifest = Parse("on UpdateName <- set name = newName");

        manifest.Diagnostics.Should().Contain(d => d.Stage == DiagnosticStage.Parse);
        manifest.Constructs.Should().NotContain(c => c.Meta.Kind == ConstructKind.EventHandler);
    }

    [Fact]
    public void ComputedField_BackArrow_WithoutExpression_ProducesParseError()
    {
        var manifest = Parse("field amount as number <-");

        manifest.Diagnostics.Should().Contain(d => d.Stage == DiagnosticStage.Parse);
    }

    [Fact]
    public void ComputedField_BackArrow_SpanStartsAtBackArrowToken_AndIsNotMissing()
    {
        const string source = "field amount as number <- amount * rate";

        var field = ParseField(source);
        var compute = field.Slots.OfType<ComputeExpressionSlot>().Single();

        compute.Span.Should().NotBe(SourceSpan.Missing);
        compute.Span.Offset.Should().Be(source.IndexOf("<-", System.StringComparison.Ordinal));
        compute.Span.End.Should().Be(source.Length);
    }

    [Fact]
    public void BackArrow_TokenMetadata_IsRegistered()
    {
        var meta = Tokens.GetMeta(TokenKind.BackArrow);

        meta.Kind.Should().Be(TokenKind.BackArrow);
        meta.Text.Should().Be("<-");
        meta.Description.Should().Be("Computed field derivation");
        meta.Categories.Should().Contain(TokenCategory.Operator);
        meta.TextMateScope.Should().Be("keyword.operator.arrow.precept");
    }
}
