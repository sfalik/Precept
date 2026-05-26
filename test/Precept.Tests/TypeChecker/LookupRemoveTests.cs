using FluentAssertions;
using Precept.Language;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// `remove F K` on a `lookup of K to V` field must type-check the argument against
/// the lookup's KEY type, not the value type — otherwise the dispatch would reject
/// legitimate `remove Lookup Key` shapes and accept incoherent `remove Lookup Value`
/// shapes.
/// </summary>
public class LookupRemoveTests
{
    [Fact]
    public void Remove_OnLookup_AcceptsKeyType()
    {
        var precept = """
            precept Widget
            field Items as lookup of string to integer
            state Draft initial
            state Done terminal
            event Drop(Key as string notempty)
            event Finish
            from Draft on Drop -> remove Items Drop.Key -> no transition
            from Draft on Finish -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionInnerTypeError),
            because: "remove on lookup expects the key type — Drop.Key (string) matches Items's key type");
    }

    [Fact]
    public void Remove_OnLookup_RejectsValueType()
    {
        var precept = """
            precept Widget
            field Items as lookup of string to integer
            state Draft initial
            state Done terminal
            event Drop(Quantity as integer)
            event Finish
            from Draft on Drop -> remove Items Drop.Quantity -> no transition
            from Draft on Finish -> transition Done
            """;

        CheckExpectingError(precept, DiagnosticCode.CollectionInnerTypeError);
    }

    [Fact]
    public void Remove_OnLookup_AcceptsLiteralKey()
    {
        var precept = """
            precept Widget
            field Items as lookup of string to integer
            state Draft initial
            state Done terminal
            event DropFixed
            event Finish
            from Draft on DropFixed -> remove Items "specific-key" -> no transition
            from Draft on Finish -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionInnerTypeError),
            because: "string literal matches the lookup's string key type");
    }

    [Fact]
    public void Remove_OnSet_StillAcceptsElementType()
    {
        // Regression guard: lookup-key dispatch must not break the set/bag/list path.
        var precept = """
            precept Widget
            field Tags as set of string
            state Draft initial
            state Done terminal
            event Drop(Tag as string)
            event Finish
            from Draft on Drop -> remove Tags Drop.Tag -> no transition
            from Draft on Finish -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionInnerTypeError),
            because: "remove on set still uses element type (regression check)");
    }
}
