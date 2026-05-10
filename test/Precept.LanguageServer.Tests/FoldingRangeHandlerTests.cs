using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;
using Precept.LanguageServer.Handlers;
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
}
