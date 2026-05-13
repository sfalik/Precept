using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Catalog-aware tests for keyword tokens that are valid as member names after '.'.
/// The catalog property <see cref="TokenMeta.IsValidAsMemberName"/> (derived from
/// <see cref="Tokens.KeywordsValidAsMemberName"/>) governs which keyword tokens the parser
/// may accept as member/method name tokens in member-access expressions.
///
/// Root cause addressed:
///   BUG-025: Keyword-named accessors rejected — <c>at</c>, <c>peekby</c>, <c>min</c>,
///            <c>max</c>, etc. are not in <c>IsValidAsMemberName</c>; parser requires Identifier.
///   BUG-039: <c>list.at(N)</c> rejected — <c>at</c> keyword collision.
/// </summary>
public class MemberAccessTests
{
    // ── Catalog metadata ─────────────────────────────────────────────────────────

    [Fact]
    public void TokenMeta_At_IsValidAsMemberName()
    {
        Tokens.GetMeta(TokenKind.At).IsValidAsMemberName.Should().BeTrue(
            because: "'at' is an indexed-access accessor for list/log/stack types — " +
                     "it must be valid after '.' to allow expressions like 'Steps.at(0)'");
    }

    [Fact]
    public void TokenMeta_Peekby_IsValidAsMemberName()
    {
        Tokens.GetMeta(TokenKind.Peekby).IsValidAsMemberName.Should().BeTrue(
            because: "'peekby' is the priority-queue ordering-key accessor — " +
                     "it must be valid after '.' to allow expressions like 'Tasks.peekby'");
    }

    [Fact]
    public void TokenMeta_Min_IsValidAsMemberName()
    {
        Tokens.GetMeta(TokenKind.Min).IsValidAsMemberName.Should().BeTrue(
            because: "'min' is an element accessor on set types — " +
                     "it must be valid after '.' to allow expressions like 'Scores.min'");
    }

    [Fact]
    public void TokenMeta_Max_IsValidAsMemberName()
    {
        Tokens.GetMeta(TokenKind.Max).IsValidAsMemberName.Should().BeTrue(
            because: "'max' is an element accessor on set types — " +
                     "it must be valid after '.' to allow expressions like 'Scores.max'");
    }

    [Theory]
    [InlineData(TokenKind.From)]
    [InlineData(TokenKind.To)]
    public void TokenMeta_ExchangeRateKeywordAccessor_IsValidAsMemberName(TokenKind kind)
    {
        var meta = Tokens.GetMeta(kind);

        meta.IsValidAsMemberName.Should().BeTrue(
            because: $"'{meta.Text}' is an exchangerate accessor and must remain valid after '.'");
    }

    [Fact]
    public void KeywordsValidAsMemberName_ContainsAt()
    {
        Tokens.KeywordsValidAsMemberName.Should().Contain(TokenKind.At,
            because: "the 'at' keyword token must appear in the member-name-valid set " +
                     "because type accessors use the name 'at'");
    }

    [Fact]
    public void KeywordsValidAsMemberName_ContainsPeekby()
    {
        Tokens.KeywordsValidAsMemberName.Should().Contain(TokenKind.Peekby,
            because: "the 'peekby' keyword token must appear in the member-name-valid set " +
                     "because the QueueBy type exposes a 'peekby' accessor");
    }

    [Theory]
    [InlineData(TokenKind.From)]
    [InlineData(TokenKind.To)]
    public void KeywordsValidAsMemberName_ContainsExchangeRateKeywordAccessor(TokenKind kind)
    {
        Tokens.KeywordsValidAsMemberName.Should().Contain(kind,
            because: $"'{Tokens.GetMeta(kind).Text}' is declared as an exchangerate accessor in the type catalog");
    }

    [Fact]
    public void KeywordsValidAsMemberName_IsNonEmpty()
    {
        Tokens.KeywordsValidAsMemberName.Should().NotBeEmpty(
            because: "at minimum 'at', 'peekby', 'from', and 'to' are keyword-named accessors and must be included");
    }

    // ── Parser: keyword member names parse as member access ──────────────────────

    [Fact]
    public void Parser_CollectionActionWithAtMethodCall_DoesNotEmitDiagnostics()
    {
        // BUG-039: 'at' keyword collision — 'EventPayload.at(0)' must parse as a method call
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(
            "from Draft on Promote -> add Items EventPayload.at(0) -> transition Approved"));

        manifest.Diagnostics.Should().BeEmpty(
            because: "'at' after '.' must be recognized as a keyword member name, not a position keyword");
    }

    [Fact]
    public void Parser_CollectionActionWithAtMethodCall_ProducesMethodCallExpression()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(
            "from Draft on Promote -> add Items EventPayload.at(0) -> transition Approved"));

        manifest.Diagnostics.Should().BeEmpty();
        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var actionSlot = row.GetRequiredSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
        var addAction = actionSlot.Actions[0].Should().BeOfType<CollectionValueAction>().Subject;

        addAction.Value.Should().BeOfType<MethodCallExpression>(
            because: "EventPayload.at(0) must parse as a method call with 'at' as the member name");
        ((MethodCallExpression)addAction.Value).MethodName.Should().Be("at");
    }

    // ── Full compilation: keyword member access expressions compile cleanly ────────

    [Fact]
    public void Compiler_ListAtAccessor_CompilesCleanly()
    {
        // BUG-039: 'list.at(N)' rejected — 'at' ambiguity
        var compilation = Compiler.Compile("""
            precept ListAt
            field Steps as list of string notempty
            field LastStep as string optional
            state Active initial
            state Done terminal
            event ReadStep(Index as integer)
            from Active on ReadStep
                -> set LastStep = Steps.at(ReadStep.Index)
                -> transition Done
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "Steps.at(N) uses the 'at' keyword as a member name — it must parse and type-check cleanly");
    }

    [Fact]
    public void Compiler_CollectionCountAccessor_CompilesCleanly()
    {
        // 'count' is a plain identifier accessor (not a keyword), but verifying the pipeline works
        var compilation = Compiler.Compile("""
            precept CollectionCount
            field Tags as set of string
            state Active initial
            event Trim
            from Active on Trim when Tags.count > 0 -> no transition
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "Tags.count uses the 'count' accessor in a guard expression and must compile cleanly");
    }

    [Fact]
    public void Compiler_PeekbyAccessor_CompilesCleanly()
    {
        // BUG-025: keyword-named accessors rejected; 'peekby' is a keyword token
        var compilation = Compiler.Compile("""
            precept PeekByAccessor
            field Tasks as queue of string by integer notempty
            field NextPriority as integer <- Tasks.peekby
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "Tasks.peekby uses the 'peekby' keyword as a member name — it must compile cleanly");
    }

    [Fact]
    public void Compiler_SetMinAccessor_CompilesCleanly()
    {
        // BUG-025: 'min' is a keyword (Min = 55) that is also a set element accessor
        var compilation = Compiler.Compile("""
            precept SetMin
            field Scores as set of integer notempty
            field LowestScore as integer <- Scores.min
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "Scores.min uses the 'min' keyword as a member name — it must compile cleanly");
    }

    [Fact]
    public void Compiler_SetMaxAccessor_CompilesCleanly()
    {
        var compilation = Compiler.Compile("""
            precept SetMax
            field Scores as set of integer notempty
            field HighestScore as integer <- Scores.max
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "Scores.max uses the 'max' keyword as a member name — it must compile cleanly");
    }

    [Fact]
    public void Compiler_ExchangeRateFromToAccessors_CompileCleanly()
    {
        var compilation = Compiler.Compile("""
            precept ExchangeRateAccessors
            field FxRate as exchangerate in 'USD' to 'EUR'
            field SourceCurrency as currency <- FxRate.from
            field TargetCurrency as currency <- FxRate.to
            event Seed(Rate as exchangerate in 'USD' to 'EUR') initial
            on Seed -> set FxRate = Seed.Rate
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "FxRate.from and FxRate.to use keyword tokens as exchangerate member names and must compile cleanly");
    }
}
