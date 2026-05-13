using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class TypeCheckerOmitLookupTests
{
    [Fact]
    public void OmitLookup_SingleFieldSingleState_Present()
    {
        var ctx = BuildContext("""
            precept Widget
            field Amount as number default 0
            state Draft initial
            in Draft omit Amount
            """);

        ctx.OmitLookup.Should().BeEquivalentTo(new[] { (State: "Draft", Field: "Amount") });
    }

    [Fact]
    public void OmitLookup_MultiFieldSingleState_AllPresent()
    {
        var ctx = BuildContext("""
            precept Widget
            field A as number default 0
            field B as number default 0
            field C as number default 0
            state Draft initial
            in Draft omit A, B, C
            """);

        ctx.OmitLookup.Should().BeEquivalentTo(new[]
        {
            (State: "Draft", Field: "A"),
            (State: "Draft", Field: "B"),
            (State: "Draft", Field: "C")
        });
    }

    [Fact]
    public void OmitLookup_SingleFieldMultiState_AllPresent()
    {
        var ctx = BuildContext("""
            precept Widget
            field A as number default 0
            state Draft initial
            state Pending
            in Draft, Pending omit A
            """);

        ctx.OmitLookup.Should().BeEquivalentTo(new[]
        {
            (State: "Draft", Field: "A"),
            (State: "Pending", Field: "A")
        });
    }

    [Fact]
    public void OmitLookup_WildcardState_ExpandsToAllDeclaredStates()
    {
        var ctx = BuildContext("""
            precept Widget
            field A as number default 0
            state Draft initial
            state Submitted
            state Done terminal
            in any omit A
            """);

        ctx.OmitLookup.Should().BeEquivalentTo(new[]
        {
            (State: "Draft", Field: "A"),
            (State: "Submitted", Field: "A"),
            (State: "Done", Field: "A")
        });
    }

    [Fact]
    public void OmitLookup_BroadcastField_ExpandsToAllDeclaredFields()
    {
        var ctx = BuildContext("""
            precept Widget
            field A as number default 0
            field B as number default 0
            state Draft initial
            in Draft omit all
            """);

        ctx.OmitLookup.Should().BeEquivalentTo(new[]
        {
            (State: "Draft", Field: "A"),
            (State: "Draft", Field: "B")
        });
    }

    [Fact]
    public void OmitLookup_NoOmitDeclarations_EmptyLookup()
    {
        var ctx = BuildContext("""
            precept Widget
            field A as number default 0
            state Draft initial
            """);

        ctx.OmitLookup.Should().BeEmpty();
    }

    [Fact]
    public void OmitLookup_UndeclaredField_DiagnosticEmitted_NotInLookup()
    {
        var (ctx, diagnostics) = BuildContextWithDiagnostics("""
            precept Widget
            field A as number default 0
            state Draft initial
            in Draft omit NonExistent
            """);

        diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
        ctx.OmitLookup.Should().NotContain((State: "Draft", Field: "NonExistent"));
    }

    private static CheckContext BuildContext(string preceptText)
    {
        var (ctx, diagnostics) = BuildContextWithDiagnostics(preceptText);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty(because: "the omit lookup scenario should type-check cleanly");

        return ctx;
    }

    private static (CheckContext Context, IReadOnlyList<Diagnostic> Diagnostics) BuildContextWithDiagnostics(string preceptText)
    {
        var tokens = Lexer.Lex(preceptText);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);
        var symbols = Precept.Pipeline.NameBinder.Bind(manifest);
        var ctx = Precept.Pipeline.TypeChecker.CreateContext(manifest, symbols);

        var buildOmitLookup = typeof(Precept.Pipeline.TypeChecker).GetMethod(
            "BuildOmitLookup",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildOmitLookup!.Invoke(null, new object?[] { manifest, ctx });

        var allDiagnostics = manifest.Diagnostics
            .Concat(symbols.Diagnostics)
            .Concat(ctx.Diagnostics)
            .ToList();

        return (ctx, allDiagnostics);
    }
}
