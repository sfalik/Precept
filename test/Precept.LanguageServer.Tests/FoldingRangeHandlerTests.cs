using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;
using Precept.LanguageServer.Handlers;
using Precept.Pipeline;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class FoldingRangeHandlerTests
{
    [Fact]
    public void GetFoldingRanges_MultiLineConstruct_ProducesFoldingRange()
    {
        var compilation = Compiler.Compile("""
            precept Order
            field Quantity
                as number
                default 0
            """);

        var ranges = FoldingRangeHandler.GetFoldingRanges(compilation);

        ranges.Should().ContainSingle(range =>
            range.StartLine == 1
            && range.EndLine == 3
            && range.Kind == FoldingRangeKind.Region);
    }

    [Fact]
    public void GetFoldingRanges_MultipleMultiLineConstructs_ProduceOneRangePerConstruct()
    {
        var compilation = CreateCompilation(
            new SourceSpan(0, 12, 2, 1, 4, 6),
            new SourceSpan(13, 9, 5, 1, 7, 4));

        var ranges = FoldingRangeHandler.GetFoldingRanges(compilation);

        ranges.Should().HaveCount(2);
        ranges.Should().Contain(range => range.StartLine == 1 && range.EndLine == 3 && range.Kind == FoldingRangeKind.Region);
        ranges.Should().Contain(range => range.StartLine == 4 && range.EndLine == 6 && range.Kind == FoldingRangeKind.Region);
    }

    [Fact]
    public void GetFoldingRanges_SingleLineConstruct_ExcludesFromFolding()
    {
        var compilation = Compiler.Compile("""
            precept Order
            field Quantity as number default 0
            """);

        var ranges = FoldingRangeHandler.GetFoldingRanges(compilation);

        ranges.Should().BeEmpty();
    }

    [Fact]
    public void GetFoldingRanges_EmptySource_ReturnsEmpty()
    {
        var compilation = Compiler.Compile(string.Empty);

        var ranges = FoldingRangeHandler.GetFoldingRanges(compilation);

        ranges.Should().BeEmpty();
    }

    private static Compilation CreateCompilation(params SourceSpan[] spans)
    {
        var meta = Precept.Language.Constructs.All.First();
        var constructs = spans
            .Select(span => new ParsedConstruct(meta, ImmutableArray<SlotValue>.Empty, span))
            .ToImmutableArray();

        return new Compilation(
            new TokenStream(ImmutableArray<Precept.Language.Token>.Empty, ImmutableArray<Precept.Language.Diagnostic>.Empty),
            new ConstructManifest(constructs, ImmutableArray<Precept.Language.Diagnostic>.Empty),
            SymbolTable.Empty,
            SemanticIndex.Empty,
            StateGraph.Empty,
            new ProofLedger(
                ImmutableArray<ProofObligation>.Empty,
                ImmutableArray<FaultSiteLink>.Empty,
                ImmutableArray<ConstraintInfluenceEntry>.Empty,
                ImmutableArray<InitialStateSatisfiabilityResult>.Empty,
                ImmutableArray<Precept.Language.Diagnostic>.Empty),
            ImmutableArray<Precept.Language.Diagnostic>.Empty,
            false);
    }
}
