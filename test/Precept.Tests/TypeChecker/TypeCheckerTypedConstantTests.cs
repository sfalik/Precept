using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using NodaTime;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 4 — TypedConstants + ContentValidation.
/// Covers TypedTypedConstant resolution via ClosedSetValidation (currency, unit, dimension),
/// NodaTimeValidation (date, time, datetime, period), TypedLiteral fallback for non-typed-constant
/// contexts, and ErrorType propagation for unresolved typed constants.
/// </summary>
public class TypeCheckerTypedConstantTests
{
    // ── Shared test span ──────────────────────────────────────────────────

    private static readonly SourceSpan TestSpan = new(0, 1, 1, 1, 1, 2);

    // ── Helpers ───────────────────────────────────────────────────────────

    private static CheckContext MinimalContext() => BuildContext("""
        precept Widget
        field Amount as integer
        state Open initial
        """);

    private static CheckContext BuildContext(string preceptText)
    {
        var tokens   = Lexer.Lex(preceptText);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);
        var symbols  = Precept.Pipeline.NameBinder.Bind(manifest);
        return Precept.Pipeline.TypeChecker.CreateContext(manifest, symbols);
    }

    private static TypedExpression Resolve(
        ParsedExpression expr,
        CheckContext ctx,
        TypeKind? expectedType = null,
        ImmutableArray<DeclaredQualifierMeta>? qualifiers = null) =>
        Precept.Pipeline.TypeChecker.ResolveExpression(expr, ctx, expectedType, qualifiers);

    private static LiteralExpression TypedConstant(string value) =>
        new(TokenKind.TypedConstant, value, TestSpan);

    // ════════════════════════════════════════════════════════════════════════
    //  1. ClosedSetValidation — Currency
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    public void ValidCurrencyCode_ResolvedAsTypedConstant(string code)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(code), ctx, TypeKind.Currency);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.Currency);
        tc.RawText.Should().Be(code);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void ValidCurrencyCode_CaseInsensitive_ResolvedAsTypedConstant()
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant("usd"), ctx, TypeKind.Currency);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.Currency);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("XYZ")]
    [InlineData("")]
    public void InvalidCurrencyCode_EmitsInvalidTypedConstantContent(string code)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(code), ctx, TypeKind.Currency);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  2. UcumValidation — UnitOfMeasure
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("kg")]
    [InlineData("each")]
    [InlineData("mg/dL")]
    public void ValidUnitCode_ResolvedAsTypedConstant(string unit)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(unit), ctx, TypeKind.UnitOfMeasure);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.UnitOfMeasure);
        tc.RawText.Should().Be(unit);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("bogusunit")]
    [InlineData("")]
    public void InvalidUnitCode_EmitsInvalidTypedConstantContent(string unit)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(unit), ctx, TypeKind.UnitOfMeasure);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    [Fact]
    public void QuantityLiteral_WrongDimension_EmitsInvalidTypedConstantContent()
    {
        var ctx = MinimalContext();
        var qualifiers = ImmutableArray.Create<DeclaredQualifierMeta>(new DeclaredQualifierMeta.Dimension("length"));
        var result = Resolve(TypedConstant("5 kg"), ctx, TypeKind.Quantity, qualifiers);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    [Fact]
    public void QuantityLiteral_MatchingDimension_Succeeds()
    {
        var ctx = MinimalContext();
        var qualifiers = ImmutableArray.Create<DeclaredQualifierMeta>(new DeclaredQualifierMeta.Dimension("mass"));
        var result = Resolve(TypedConstant("5 kg"), ctx, TypeKind.Quantity, qualifiers);

        result.Should().BeOfType<TypedTypedConstant>();
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void QuantityLiteral_NoDeclaredDimension_Succeeds()
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant("5 kg"), ctx, TypeKind.Quantity);

        result.Should().BeOfType<TypedTypedConstant>();
        ctx.Diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  3. ClosedSetValidation — Dimension
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("length")]
    [InlineData("mass")]
    public void ValidDimension_ResolvedAsTypedConstant(string dim)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(dim), ctx, TypeKind.Dimension);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.Dimension);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void InvalidDimension_EmitsInvalidTypedConstantContent()
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant("bogus"), ctx, TypeKind.Dimension);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  4. NodaTimeValidation — Date
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("2024-01-15")]
    [InlineData("2024-12-31")]
    [InlineData("2026-06-01")]
    public void ValidIsoDate_ResolvedAsTypedConstant(string date)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(date), ctx, TypeKind.Date);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.Date);
        tc.RawText.Should().Be(date);
        tc.ParsedValue.Should().BeOfType<LocalDate>();
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2024-13-01")]
    [InlineData("2024-00-15")]
    public void InvalidIsoDate_EmitsInvalidTypedConstantContent(string date)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(date), ctx, TypeKind.Date);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    [Fact]
    public void DateTimeStringAsDate_EmitsInvalidTypedConstantContent()
    {
        var ctx = MinimalContext();
        // An ISO datetime should not parse as a bare date
        var result = Resolve(TypedConstant("2024-01-15T14:30:00"), ctx, TypeKind.Date);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  5. NodaTimeValidation — Time
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("14:30:00")]
    [InlineData("00:00:00")]
    [InlineData("23:59:59")]
    public void ValidIsoTime_ResolvedAsTypedConstant(string time)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(time), ctx, TypeKind.Time);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.Time);
        tc.ParsedValue.Should().BeOfType<LocalTime>();
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("25:00:00")]
    [InlineData("not-a-time")]
    [InlineData("")]
    public void InvalidIsoTime_EmitsInvalidTypedConstantContent(string time)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(time), ctx, TypeKind.Time);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  6. NodaTimeValidation — DateTime
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("2024-01-15T14:30:00")]
    [InlineData("2024-12-31T23:59:59")]
    public void ValidIsoDateTime_ResolvedAsTypedConstant(string dt)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(dt), ctx, TypeKind.DateTime);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.DateTime);
        tc.ParsedValue.Should().BeOfType<LocalDateTime>();
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-a-datetime")]
    [InlineData("2024-13-01T00:00:00")]
    public void InvalidIsoDateTime_EmitsInvalidTypedConstantContent(string dt)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(dt), ctx, TypeKind.DateTime);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  7. NodaTimeValidation — Period
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("P30D")]
    [InlineData("P1Y6M")]
    [InlineData("PT2H30M")]
    public void ValidIsoPeriod_ResolvedAsTypedConstant(string period)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(period), ctx, TypeKind.Period);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.Period);
        tc.ParsedValue.Should().BeOfType<Period>();
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-a-period")]
    [InlineData("30D")]
    public void InvalidIsoPeriod_EmitsInvalidTypedConstantContent(string period)
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant(period), ctx, TypeKind.Period);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.InvalidTypedConstantContent.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  8. TypedLiteral fallback — non-typed-constant contexts
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StringLiteral_InStringContext_ResolvesAsTypedLiteral()
    {
        var ctx = MinimalContext();
        var lit = new LiteralExpression(TokenKind.StringLiteral, "hello world", TestSpan);
        var result = Resolve(lit, ctx);

        result.Should().BeOfType<TypedLiteral>();
        var tl = (TypedLiteral)result;
        tl.ResultType.Should().Be(TypeKind.String);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void NumberLiteral_InNumericContext_ResolvesAsTypedLiteral()
    {
        var ctx = MinimalContext();
        var lit = new LiteralExpression(TokenKind.NumberLiteral, "42", TestSpan);
        var result = Resolve(lit, ctx);

        result.Should().BeOfType<TypedLiteral>();
        var tl = (TypedLiteral)result;
        tl.ResultType.Should().Be(TypeKind.Integer);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void BooleanLiteral_ResolvesAsTypedLiteral()
    {
        var ctx = MinimalContext();
        var lit = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var result = Resolve(lit, ctx);

        result.Should().BeOfType<TypedLiteral>();
        var tl = (TypedLiteral)result;
        tl.ResultType.Should().Be(TypeKind.Boolean);
        ctx.Diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  9. UnresolvedTypedConstant — no expectedType context
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypedConstant_WithoutExpectedType_EmitsUnresolvedTypedConstant()
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant("USD"), ctx, expectedType: null);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.UnresolvedTypedConstant.ToString());
    }

    [Fact]
    public void TypedConstant_WithErrorExpectedType_EmitsUnresolvedTypedConstant()
    {
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant("USD"), ctx, expectedType: TypeKind.Error);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.UnresolvedTypedConstant.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  10. TypedConstant with no ContentValidation — trusted pass-through
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypedConstant_WithTypeHavingNoContentValidation_ResolvedAsTrustedConstant()
    {
        // String type has no ContentValidation — typed constant should pass through as-is
        var ctx = MinimalContext();
        var result = Resolve(TypedConstant("anything"), ctx, TypeKind.String);

        result.Should().BeOfType<TypedTypedConstant>();
        var tc = (TypedTypedConstant)result;
        tc.ResultType.Should().Be(TypeKind.String);
        tc.RawText.Should().Be("anything");
        tc.ParsedValue.Should().Be("anything");
        ctx.Diagnostics.Should().BeEmpty();
    }
}
