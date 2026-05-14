using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.LanguageServer.Handlers;
using Precept.Pipeline;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class HoverHandlerIntervalTests
{
    private const string BoundedProvedSource = """
        precept IntervalFieldProved
        field Principal as decimal min 0 max 45000
        field Interest as decimal min 0 max 5000
        field LoanBalance as decimal min 0 max 50000
        state Open initial
        event Accrue
        from Open on Accrue
            -> set LoanBalance = Principal + Interest
            -> no transition
        """;

    private const string BoundedUnresolvedSource = """
        precept IntervalFieldGap
        field Invoice as decimal min 0 max 999999.99
        field Surcharge as decimal min 0 max 100
        state Open initial
        event Apply
        from Open on Apply
            -> set Invoice = Invoice + Surcharge
            -> no transition
        """;

    private const string OptionalBoundedUnresolvedSource = """
        precept IntervalOptionalGap
        field Discount as decimal optional min 0 max 50
        state Open initial
        event Apply(NewDiscount as decimal)
        from Open on Apply
            -> set Discount = Apply.NewDiscount
            -> no transition
        """;

    private const string UnboundedFieldSource = """
        precept IntervalUnboundedField
        field Adjustment as decimal
        state Open initial
        """;

    private const string IntervalNotationSource = """
        precept IntervalNotation
        field Balance as decimal min 0 max 999999999.99
        state Open initial
        event Deposit(Amount as decimal min 0 max 999999999.99)
        from Open on Deposit
            -> set Balance = Deposit.Amount
            -> no transition
        """;

    private const string LowerBoundViolationSource = """
        precept IntervalLowerBoundGap
        field Balance as decimal min 0 max 100
        state Open initial
        event Withdraw(Amount as decimal min 1 max 200)
        from Open on Withdraw
            -> set Balance = Balance - Withdraw.Amount
            -> no transition
        """;

    [Fact]
    public void FieldHover_BoundedField_ProvedInterval_Template41_ShowsProvenBadge()
    {
        var markup = GetHoverMarkdown(BoundedProvedSource, "LoanBalance as decimal");

        markup.Should().Contain("✅ Proven · `LoanBalance` stays within");
        markup.Should().Contain("⚖️ Declared:");
        markup.Should().Contain("Governed by:");
    }

    [Fact]
    public void FieldHover_BoundedField_UnresolvedInterval_Template42_ShowsGapBadge()
    {
        var markup = GetHoverMarkdown(BoundedUnresolvedSource, "Invoice as decimal");

        markup.Should().Contain("⚠️ Gap · `Invoice` assignment may leave");
        markup.Should().Contain("🔬 `Invoice + Surcharge`");
        markup.Should().Contain("upper bound unsafe");
    }

    [Fact]
    public void FieldHover_UnboundedField_Template43_ShowsEnforcedBadge()
    {
        var markup = GetHoverMarkdown(UnboundedFieldSource, "Adjustment as decimal");

        markup.Should().Contain("⚡ Enforced · `Adjustment` · `decimal`");
        markup.Should().NotContain("Result interval");
    }

    [Fact]
    public void ProofExpressionHover_ProvedInterval_Template44_ShowsIntervalSubLine()
    {
        var markup = GetHoverMarkdown(BoundedProvedSource, "Principal + Interest", offset: 10);

        markup.Should().Contain("✅ Proven · `Principal + Interest` result");
        markup.Should().Contain("🔬 Result interval:");
        markup.Should().Contain("fits `LoanBalance`");
    }

    [Fact]
    public void ProofExpressionHover_UnresolvedInterval_Template45_ShowsViolatedBound()
    {
        var compilation = Precept.Compiler.Compile(BoundedUnresolvedSource);
        var withoutOverflowDiagnostic = compilation with
        {
            Diagnostics = compilation.Diagnostics
                .Where(diagnostic => !string.Equals(diagnostic.Code, nameof(Precept.Language.DiagnosticCode.NumericOverflow), System.StringComparison.Ordinal))
                .ToImmutableArray(),
        };

        var markup = GetHoverMarkdown(withoutOverflowDiagnostic, BoundedUnresolvedSource, "Invoice + Surcharge", offset: 10);

        markup.Should().Contain("⚠️ Gap · `Invoice + Surcharge` may leave");
        markup.Should().Contain("🔬 Result interval:");
        markup.Should().Contain("upper bound unsafe");
    }

    [Fact]
    public void FieldHover_OptionalBoundedField_Template46_ShowsCombinedGap()
    {
        var markup = GetHoverMarkdown(OptionalBoundedUnresolvedSource, "Discount as decimal");

        markup.Should().Contain("⚠️ Gap · `Discount` is optional");
        markup.Should().Contain("interval applies only when present");
        markup.Should().Contain("Guard presence before arithmetic");
    }

    [Fact]
    public void DiagnosticSquiggle_NumericOverflow_BeatsFieldHover_RoutingPriority()
    {
        var markup = GetHoverMarkdown(BoundedUnresolvedSource, "Invoice + Surcharge", offset: 8);

        markup.Should().Contain("`PRE0078`");
        markup.Should().Contain("Arithmetic result may overflow declared bounds");
        markup.Should().NotContain("Governed by:");
    }

    [Fact]
    public void FieldHover_NoDiagnostic_RoutesToFieldCard_NotSquiggle()
    {
        var markup = GetHoverMarkdown(BoundedProvedSource, "LoanBalance as decimal");

        markup.Should().Contain("✅ Proven · `LoanBalance`");
        markup.Should().NotContain("`PRE");
    }

    [Fact]
    public void FieldHover_IntervalNotation_UsesSquareBracketDotDotFormat()
    {
        var markup = GetHoverMarkdown(IntervalNotationSource, "Balance as decimal");

        markup.Should().Contain("[0 ..");
        markup.Should().Contain("]");
    }

    [Fact]
    public void FieldHover_IntervalNotation_ThinSpaceThousandsSeparator()
    {
        var markup = GetHoverMarkdown(IntervalNotationSource, "Balance as decimal");

        markup.Should().Contain("\u2009");
        markup.Should().NotContain("999,999,999.99");
    }

    [Fact]
    public void FieldHover_DeclaredBounds_ShowsScalesBadge()
    {
        var markup = GetHoverMarkdown(BoundedProvedSource, "LoanBalance as decimal");

        markup.Should().Contain("⚖️ Declared:");
    }

    [Fact]
    public void FieldHover_InferredInterval_ShowsMicroscopeBadge()
    {
        var markup = GetHoverMarkdown(BoundedProvedSource, "Principal + Interest", offset: 10);

        markup.Should().Contain("🔬 Result interval:");
    }

    [Fact]
    public void FieldHover_V1_ExpandedView_DoesNotAppear()
    {
        var markup = GetHoverMarkdown(BoundedUnresolvedSource, "Invoice as decimal");
        var lineCount = markup.Split('\n').Length;

        lineCount.Should().BeLessThanOrEqualTo(3);
        markup.Should().NotContain("Left operand:");
        markup.Should().NotContain("headroom");
    }

    [Fact]
    public void DiagnosticSquiggle_NumericOverflow_ShowsIntervalLine()
    {
        var markup = GetHoverMarkdown(BoundedUnresolvedSource, "Invoice + Surcharge", offset: 8);

        markup.Should().Contain("`PRE0078`");
        markup.Should().Contain("Result interval");
        markup.Should().Contain("declared");
    }

    [Fact]
    public void DiagnosticSquiggle_NumericOverflow_IdentifiesViolatedBound()
    {
        var upperMarkup = GetHoverMarkdown(BoundedUnresolvedSource, "Invoice + Surcharge", offset: 8);
        var lowerMarkup = GetHoverMarkdown(LowerBoundViolationSource, "Balance - Withdraw.Amount", offset: 8);

        upperMarkup.Should().Contain("Upper bound violated:");
        lowerMarkup.Should().Contain("Lower bound violated:");
    }

    private static string GetHoverMarkdown(string source, string needle, int offset = 0, int occurrence = 1)
    {
        var compilation = Precept.Compiler.Compile(source);
        return GetHoverMarkdown(compilation, source, needle, offset, occurrence);
    }

    private static string GetHoverMarkdown(Compilation compilation, string source, string needle, int offset = 0, int occurrence = 1)
    {
        var hover = HoverHandler.CreateHover(compilation, PositionOf(source, needle, offset, occurrence));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent.Should().NotBeNull();
        return hover.Contents.MarkupContent!.Value;
    }

    private static Position PositionOf(string source, string needle, int offset = 0, int occurrence = 1)
    {
        var index = -1;
        var searchStart = 0;
        for (var i = 0; i < occurrence; i++)
        {
            index = source.IndexOf(needle, searchStart, System.StringComparison.Ordinal);
            index.Should().BeGreaterThanOrEqualTo(0, $"expected to find '{needle}' in test source");
            searchStart = index + needle.Length;
        }

        index += offset;
        var line = 0;
        var character = 0;
        for (var i = 0; i < index; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                character = 0;
                continue;
            }

            if (source[i] != '\r')
            {
                character++;
            }
        }

        return new Position(line, character);
    }
}
