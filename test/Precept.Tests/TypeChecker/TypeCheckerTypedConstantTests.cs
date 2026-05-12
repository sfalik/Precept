using System;
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

    private static LiteralExpression InterpolatedTypedConstantStart(string value) =>
        new(TokenKind.TypedConstantStart, value, TestSpan);

    private const string InterpolatedTypedConstantEventHandlerPrecept = """
        precept Test

        field q as quantity in 'each'
        field s as string
        field ss as unitofmeasure

        event xyz initial
        on xyz
            -> set s = "blah"
            -> set ss = 'each'
            -> set q = '1 {s}'
        """;

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

    [Fact]
    public void InterpolatedTypedConstant_InEventHandler_EmitsDiagnostic()
    {
        // '1 {s}' where s is string → string rejected in magnitude slot
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(InterpolatedTypedConstantEventHandlerPrecept);

        diagnostics.Should().Contain(d =>
            d.Severity == Severity.Error
            && d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));

        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(action => action.FieldName == "q");
        assign.InputExpression.Should().BeOfType<TypedErrorExpression>();
    }

    [Fact]
    public void InterpolatedTypedConstant_DoesNotCrashLanguageServer()
    {
        Compilation? compilation = null;
        Action act = () => compilation = Compiler.Compile(InterpolatedTypedConstantEventHandlerPrecept);

        act.Should().NotThrow("interpolated typed constants must surface a diagnostic instead of tripping D26");
        compilation.Should().NotBeNull();
        compilation!.HasErrors.Should().BeTrue();
        compilation.Diagnostics.Should().Contain(d =>
            d.Severity == Severity.Error
            && d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstantStart_AsBareLiteral_ReturnsError()
    {
        // TypedConstantStart as bare LiteralExpression is now dead code path — returns TypedErrorExpression
        var ctx = MinimalContext();
        var result = Resolve(InterpolatedTypedConstantStart("1 "), ctx, TypeKind.Quantity);

        result.Should().BeOfType<TypedErrorExpression>();
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

    // ════════════════════════════════════════════════════════════════════════
    //  11. Slice 2 — Interpolated typed constant form grammar matching
    // ════════════════════════════════════════════════════════════════════════

    // ── Structural validity (InvalidInterpolatedTypedConstantForm) ────────

    [Fact]
    public void InterpolatedTypedConstant_ThreeHolesForMoney_StructuralError()
    {
        // '{x} {y} {z}' for money → 3 holes, money has max 2
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field x as integer
            field y as currency
            field z as integer
            event Go initial
            on Go
                -> set m = '{x} {y} {z}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InvalidInterpolatedTypedConstantForm));
    }

    [Fact]
    public void InterpolatedTypedConstant_SlashSeparatorForMoney_StructuralError()
    {
        // '{x}/{y}' for money → slash separator not valid for money
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field x as integer
            field y as currency
            event Go initial
            on Go
                -> set m = '{x}/{y}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InvalidInterpolatedTypedConstantForm));
    }

    [Fact]
    public void InterpolatedTypedConstant_TwoCurrencyTextsForMoney_StructuralError()
    {
        // '{x} USD EUR' for money → no pattern with two currency texts after hole
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field x as integer
            event Go initial
            on Go
                -> set m = '{x} USD EUR'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InvalidInterpolatedTypedConstantForm));
    }

    // ── Interpolation not supported (InterpolationNotSupportedForType) ────

    [Theory]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("instant")]
    [InlineData("datetime")]
    [InlineData("zoneddatetime")]
    [InlineData("timezone")]
    public void InterpolatedTypedConstant_UnsupportedType_EmitsNotSupported(string typeName)
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check($$"""
            precept Test
            field f as {{typeName}}
            field x as integer
            event Go initial
            on Go
                -> set f = '{x}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolationNotSupportedForType));
    }

    // ── Hole type compatibility (InterpolatedTypedConstantHoleTypeMismatch) ──

    [Fact]
    public void InterpolatedTypedConstant_BooleanInMagnitudeSlot_TypeMismatch()
    {
        // '{b} kg' where b is boolean → magnitude slot type mismatch
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field q as quantity in 'kg'
            field b as boolean
            event Go initial
            on Go
                -> set q = '{b} kg'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_QuantityInCurrencySlot_TypeMismatch()
    {
        // '100 {q}' where q is quantity for money → currency slot type mismatch
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field q as quantity in 'kg'
            event Go initial
            on Go
                -> set m = '100 {q}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_QuantityInMagnitudeSlot_TypeMismatch()
    {
        // '{q} USD' where q is quantity → magnitude slot type mismatch
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field q as quantity in 'kg'
            event Go initial
            on Go
                -> set m = '{q} USD'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_DecimalInTemporalMagnitude_TypeMismatch()
    {
        // '{d} days' where d is decimal → temporal magnitude requires integer
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field p as period
            field d as decimal
            event Go initial
            on Go
                -> set p = '{d} days'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    // ── String rejection (InterpolatedTypedConstantHoleTypeMismatch) ──────

    [Fact]
    public void InterpolatedTypedConstant_StringInMagnitudeSlot_Rejected()
    {
        // '{s} kg' where s is string → string not valid in magnitude slot
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field q as quantity in 'kg'
            field s as string
            event Go initial
            on Go
                -> set q = '{s} kg'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_StringInUnitSlot_Rejected()
    {
        // '100 {s}' where s is string for quantity → string not valid in unit slot
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field q as quantity in 'kg'
            field s as string
            event Go initial
            on Go
                -> set q = '100 {s}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_StringInWholeValueSlot_Rejected()
    {
        // '{s}' where s is string for money → string not valid in whole-value slot
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field s as string
            event Go initial
            on Go
                -> set m = '{s}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    // ── Compound-unit interpolation ──────────────────────────────────────

    [Fact]
    public void InterpolatedTypedConstant_CompoundUnit_ValidUnitOfMeasure()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as unitofmeasure
            field a as unitofmeasure
            field b as unitofmeasure
            event Go initial
            on Go
                -> set target = '{a}/{b}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.UnitOfMeasure);
    }

    [Fact]
    public void InterpolatedTypedConstant_IntegerMagnitudeWithCompoundUnit_ValidQuantity()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity
            field n as integer
            field a as unitofmeasure
            field b as unitofmeasure
            event Go initial
            on Go
                -> set target = '{n} {a}/{b}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Quantity);
    }

    [Fact]
    public void InterpolatedTypedConstant_DecimalMagnitudeWithCompoundUnit_ValidQuantity()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity
            field n as decimal
            field a as unitofmeasure
            field b as unitofmeasure
            event Go initial
            on Go
                -> set target = '{n} {a}/{b}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Quantity);
    }

    [Fact]
    public void InterpolatedTypedConstant_QuantityInCompoundUnitSlot_TypeMismatch()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as unitofmeasure
            field q as quantity in 'kg'
            field b as unitofmeasure
            event Go initial
            on Go
                -> set target = '{q}/{b}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_StringInCompoundUnitNumerator_Rejected()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as unitofmeasure
            field s as string
            field b as unitofmeasure
            event Go initial
            on Go
                -> set target = '{s}/{b}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_StringInCompoundUnitDenominator_Rejected()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as unitofmeasure
            field a as unitofmeasure
            field s as string
            event Go initial
            on Go
                -> set target = '{a}/{s}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_IntegerInCompoundUnitSlot_Rejected()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as unitofmeasure
            field n as integer
            field b as unitofmeasure
            event Go initial
            on Go
                -> set target = '{n}/{b}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch));
    }

    [Fact]
    public void InterpolatedTypedConstant_ThreeHoleCompoundUnit_StructuralError()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as unitofmeasure
            field a as unitofmeasure
            field b as unitofmeasure
            field c as unitofmeasure
            event Go initial
            on Go
                -> set target = '{a}/{b}/{c}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InvalidInterpolatedTypedConstantForm));
    }

    [Fact]
    public void InterpolatedTypedConstant_PipeSeparatedCompoundUnit_StructuralError()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as unitofmeasure
            field a as unitofmeasure
            field b as unitofmeasure
            event Go initial
            on Go
                -> set target = '{a}|{b}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.InvalidInterpolatedTypedConstantForm));
    }

    // ── Valid combinations (positive tests) ──────────────────────────────

    [Fact]
    public void InterpolatedTypedConstant_IntegerMagnitudeWithUnit_ValidQuantity()
    {
        // '{n} kg' where n is integer → valid quantity
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field q as quantity in 'kg'
            field n as integer
            event Go initial
            on Go
                -> set q = '{n} kg'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "q");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Quantity);
    }

    [Fact]
    public void InterpolatedTypedConstant_DecimalMagnitudeWithUnit_ValidQuantity()
    {
        // '{n} kg' where n is decimal → valid quantity
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field q as quantity in 'kg'
            field n as decimal
            event Go initial
            on Go
                -> set q = '{n} kg'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "q");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>();
    }

    [Fact]
    public void InterpolatedTypedConstant_MagnitudeAndCurrency_ValidMoney()
    {
        // '{a} {c}' where a is integer, c is currency → valid money
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field a as integer
            field c as currency
            event Go initial
            on Go
                -> set m = '{a} {c}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "m");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Money);
    }

    [Fact]
    public void InterpolatedTypedConstant_PriceWithAllHoles_ValidPrice()
    {
        // '{r} {c}/{u}' where r is decimal, c is currency, u is unitofmeasure → valid price
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field p as price
            field r as decimal
            field c as currency
            field u as unitofmeasure
            event Go initial
            on Go
                -> set p = '{r} {c}/{u}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "p");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Price);
    }

    [Fact]
    public void InterpolatedTypedConstant_ExchangeRateWithFromTo_ValidExchangeRate()
    {
        // '{r} {f}/{t}' where r is decimal, f/t are currency → valid exchangerate
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field x as exchangerate
            field r as decimal
            field f as currency
            field t as currency
            event Go initial
            on Go
                -> set x = '{r} {f}/{t}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "x");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.ExchangeRate);
    }

    [Fact]
    public void InterpolatedTypedConstant_IntegerWithTemporalUnit_ValidPeriod()
    {
        // '{n} days' where n is integer → valid period
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field p as period
            field n as integer
            event Go initial
            on Go
                -> set p = '{n} days'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "p");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Period);
    }

    [Fact]
    public void InterpolatedTypedConstant_CompoundDuration_ValidDuration()
    {
        // '{n} hours + {m} minutes' where n, m are integer → valid compound duration
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field d as duration
            field n as integer
            field m as integer
            event Go initial
            on Go
                -> set d = '{n} hours + {m} minutes'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "d");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Duration);
    }

    // ── Whole-value forms ────────────────────────────────────────────────

    [Fact]
    public void InterpolatedTypedConstant_MoneyWholeValue_Valid()
    {
        // '{m}' where m is money → valid whole-value
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as money
            field m as money
            event Go initial
            on Go
                -> set target = '{m}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Money);
    }

    [Fact]
    public void InterpolatedTypedConstant_QuantityWholeValue_Valid()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity in 'kg'
            field q as quantity in 'kg'
            event Go initial
            on Go
                -> set target = '{q}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>();
    }

    [Fact]
    public void InterpolatedTypedConstant_DurationWholeValue_Valid()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as duration
            field d as duration
            event Go initial
            on Go
                -> set target = '{d}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>();
    }

    [Fact]
    public void InterpolatedTypedConstant_CurrencyWholeValue_Valid()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as currency
            field c as currency
            event Go initial
            on Go
                -> set target = '{c}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>();
    }

    [Fact]
    public void InterpolatedTypedConstant_UnitOfMeasureWholeValue_Valid()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as unitofmeasure
            field u as unitofmeasure
            event Go initial
            on Go
                -> set target = '{u}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>();
    }

    // ── Dimension-unit consistency (DimensionMismatchInUnitSlot) ──────────

    [Fact]
    public void InterpolatedTypedConstant_DimensionMismatch_LengthVsMass_Error()
    {
        // '1 {f1.unit}' where f1 is quantity of 'length', target is quantity of 'mass' → DimensionMismatchInUnitSlot
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity of 'mass'
            field f1 as quantity of 'length'
            event Go initial
            on Go
                -> set target = '1 {f1.unit}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.DimensionMismatchInUnitSlot));
    }

    [Fact]
    public void InterpolatedTypedConstant_DimensionMatch_MassVsMass_NoError()
    {
        // '1 {f1.unit}' where f1 is quantity of 'mass', target is quantity of 'mass' → no error
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity of 'mass'
            field f1 as quantity of 'mass'
            event Go initial
            on Go
                -> set target = '1 {f1.unit}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "target");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>();
    }

    [Fact]
    public void InterpolatedTypedConstant_SourceNoDimension_ConservativeAccept()
    {
        // '1 {f1.unit}' where f1 is quantity (no dimension), target is quantity of 'mass' → no error
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity of 'mass'
            field f1 as quantity
            event Go initial
            on Go
                -> set target = '1 {f1.unit}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
    }

    [Fact]
    public void InterpolatedTypedConstant_TargetNoDimension_NoError()
    {
        // '1 {f1.unit}' where f1 is quantity of 'length', target is quantity (no dimension) → no error
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity
            field f1 as quantity of 'length'
            event Go initial
            on Go
                -> set target = '1 {f1.unit}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
    }

    [Fact]
    public void InterpolatedTypedConstant_UnitQualifierMismatch_KgVsLength_Error()
    {
        // '1 {f1.unit}' where f1 is quantity in 'kg' (dimension=mass), target is quantity of 'length' → DimensionMismatchInUnitSlot
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity of 'length'
            field f1 as quantity in 'kg'
            event Go initial
            on Go
                -> set target = '1 {f1.unit}'
            """);

        diagnostics.Should().Contain(d =>
            d.Code == nameof(DiagnosticCode.DimensionMismatchInUnitSlot));
    }

    [Fact]
    public void InterpolatedTypedConstant_BareUnitOfMeasure_ConservativeAccept()
    {
        // '1 {u}' where u is bare unitofmeasure field, target is quantity of 'mass' → no dimension check
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field target as quantity of 'mass'
            field u as unitofmeasure
            event Go initial
            on Go
                -> set target = '1 {u}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
    }

    // ── Additional form variations ───────────────────────────────────────

    [Fact]
    public void InterpolatedTypedConstant_NumericMagnitudeWithCurrencyHole_ValidMoney()
    {
        // '100 {c}' where c is currency → valid money (M3 form)
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field c as currency
            event Go initial
            on Go
                -> set m = '100 {c}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "m");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Money);
    }

    [Fact]
    public void InterpolatedTypedConstant_MagnitudeWithFixedCurrency_ValidMoney()
    {
        // '{n} USD' where n is integer → valid money (M2 form)
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field m as money
            field n as integer
            event Go initial
            on Go
                -> set m = '{n} USD'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "m");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Money);
    }

    [Fact]
    public void InterpolatedTypedConstant_NumericWithUnitHole_ValidQuantity()
    {
        // '100 {u}' where u is unitofmeasure → valid quantity (Q3 form)
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field q as quantity
            field u as unitofmeasure
            event Go initial
            on Go
                -> set q = '100 {u}'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "q");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Quantity);
    }

    [Fact]
    public void InterpolatedTypedConstant_PriceWithMagnitudeAndFixedCurrencyUnit_Valid()
    {
        // '{n} USD/kg' where n is decimal → valid price (P2 form)
        var (index, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Test
            field p as price
            field n as decimal
            event Go initial
            on Go
                -> set p = '{n} USD/kg'
            """);

        diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
        var assign = index.EventHandlers.Single().Actions.OfType<TypedInputAction>()
            .Single(a => a.FieldName == "p");
        assign.InputExpression.Should().BeOfType<TypedInterpolatedTypedConstant>()
            .Which.ResultType.Should().Be(TypeKind.Price);
    }

    [Fact]
    public void InterpolatedTypedConstant_NoExpectedType_EmitsUnresolved()
    {
        // Interpolated typed constant without expected type context → UnresolvedTypedConstant
        var ctx = MinimalContext();
        var segments = ImmutableArray.Create<InterpolationSegment>(
            new TextSegment("", TestSpan),
            new HoleSegment(new LiteralExpression(TokenKind.Identifier, "Amount", TestSpan), TestSpan),
            new TextSegment("", TestSpan));
        var expr = new InterpolatedTypedConstantExpression(segments, TestSpan);
        var result = Resolve(expr, ctx, expectedType: null);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(DiagnosticCode.UnresolvedTypedConstant.ToString());
    }
}
