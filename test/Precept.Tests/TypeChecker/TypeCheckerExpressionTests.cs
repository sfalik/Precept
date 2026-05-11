using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 2 — Scalar Expression Resolution.
/// Covers TypedLiteral (all scalar kinds), TypedFieldRef/ArgRef (scope priority,
/// forward-ref D8), TypedBinaryOp (FindCandidates, widening D11, ErrorType
/// propagation D13), TypedUnaryOp (FindUnary D12), and stub arm contracts.
/// </summary>
public class TypeCheckerExpressionTests
{
    // ── Shared test span ──────────────────────────────────────────────────

    private static readonly SourceSpan TestSpan = new(0, 1, 1, 1, 1, 2);

    // ── Context builder helpers ───────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="CheckContext"/> from a precept DSL string
    /// by running the full pipeline through Pass 1 (symbol population).
    /// </summary>
    private static CheckContext BuildContext(string preceptText)
    {
        var tokens   = Lexer.Lex(preceptText);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);
        var symbols  = Precept.Pipeline.NameBinder.Bind(manifest);
        return Precept.Pipeline.TypeChecker.CreateContext(manifest, symbols);
    }

    /// <summary>Resolves a <see cref="ParsedExpression"/> in the given context.</summary>
    private static TypedExpression Resolve(ParsedExpression expr, CheckContext ctx) =>
        Precept.Pipeline.TypeChecker.ResolveExpression(expr, ctx);

    /// <summary>Minimal context: one integer field, one state.</summary>
    private static CheckContext MinimalContext() => BuildContext("""
        precept Widget
        field Amount as integer
        state Open initial
        """);

    // ════════════════════════════════════════════════════════════════════════
    //  1. TypedLiteral — scalar literal resolution
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StringLiteral_ResolvesToStringTypeKind()
    {
        var expr = new LiteralExpression(TokenKind.StringLiteral, "hello", TestSpan);
        var result = Resolve(expr, MinimalContext());

        result.Should().BeOfType<TypedLiteral>();
        result.ResultType.Should().Be(TypeKind.String);
        ((TypedLiteral)result).Value.Should().Be("hello");
    }

    [Fact]
    public void IntegerLiteral_ResolvesToIntegerTypeKind()
    {
        var expr = new LiteralExpression(TokenKind.NumberLiteral, "42", TestSpan);
        var result = Resolve(expr, MinimalContext());

        result.Should().BeOfType<TypedLiteral>();
        result.ResultType.Should().Be(TypeKind.Integer);
        ((TypedLiteral)result).Value.Should().Be(42L);
    }

    [Fact]
    public void DecimalLiteral_ResolvesToDecimalTypeKind()
    {
        var expr = new LiteralExpression(TokenKind.NumberLiteral, "3.14", TestSpan);
        var result = Resolve(expr, MinimalContext());

        result.Should().BeOfType<TypedLiteral>();
        result.ResultType.Should().Be(TypeKind.Decimal);
        ((TypedLiteral)result).Value.Should().Be(3.14m);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanLiteral_ResolvesToBooleanTypeKind(bool value)
    {
        var kind = value ? TokenKind.True : TokenKind.False;
        var expr = new LiteralExpression(kind, value.ToString(), TestSpan);
        var result = Resolve(expr, MinimalContext());

        result.Should().BeOfType<TypedLiteral>();
        result.ResultType.Should().Be(TypeKind.Boolean);
        ((TypedLiteral)result).Value.Should().Be(value);
    }

    [Fact]
    public void TypedConstantLiteral_ReturnsErrorExpression_Stub()
    {
        var expr = new LiteralExpression(TokenKind.TypedConstant, "'2026-01-01'", TestSpan);
        var result = Resolve(expr, MinimalContext());

        result.Should().BeOfType<TypedErrorExpression>(
            because: "typed constants are Slice 4 stubs");
    }

    [Fact]
    public void TypedConstantStartLiteral_ReturnsErrorExpression_Stub()
    {
        var expr = new LiteralExpression(TokenKind.TypedConstantStart, "'100", TestSpan);
        var result = Resolve(expr, MinimalContext());

        result.Should().BeOfType<TypedErrorExpression>(
            because: "typed constant start is a Slice 4 stub");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  2. TypedFieldRef — identifier resolution
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FieldReference_ResolvesToCorrectTypeKind()
    {
        var ctx = BuildContext("""
            precept Widget
            field Name as string
            state Open initial
            """);

        var expr = new IdentifierExpression("Name", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedFieldRef>();
        var fieldRef = (TypedFieldRef)result;
        fieldRef.ResultType.Should().Be(TypeKind.String);
        fieldRef.FieldName.Should().Be("Name");
    }

    [Fact]
    public void IntegerFieldReference_ResolvesToIntegerTypeKind()
    {
        var ctx = BuildContext("""
            precept Widget
            field Count as integer
            state Open initial
            """);

        var expr = new IdentifierExpression("Count", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedFieldRef>();
        result.ResultType.Should().Be(TypeKind.Integer);
    }

    [Fact]
    public void UnknownIdentifier_EmitsUndeclaredFieldDiagnostic()
    {
        var ctx = MinimalContext();
        var expr = new IdentifierExpression("NoSuchField", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.UndeclaredField.ToString());
    }

    [Fact]
    public void ForwardReference_InPriorFieldsOnlyScope_EmitsDiagnostic()
    {
        var ctx = BuildContext("""
            precept Widget
            field First as integer
            field Second as string
            state Open initial
            """);

        // Simulate PriorFieldsOnly scope at field index 0 (First)
        // Referencing "Second" (index 1) should be a forward reference
        ctx.CurrentScope = FieldScopeMode.PriorFieldsOnly;
        ctx.CurrentFieldIndex = 0;

        var expr = new IdentifierExpression("Second", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.DefaultForwardReference.ToString());
    }

    [Fact]
    public void SameFieldReference_InPriorFieldsOnlyScope_EmitsDiagnostic()
    {
        var ctx = BuildContext("""
            precept Widget
            field First as integer
            field Second as string
            state Open initial
            """);

        // Referencing own field (index 0) when CurrentFieldIndex == 0 should fail (>= check)
        ctx.CurrentScope = FieldScopeMode.PriorFieldsOnly;
        ctx.CurrentFieldIndex = 0;

        var expr = new IdentifierExpression("First", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.DefaultForwardReference.ToString());
    }

    [Fact]
    public void PriorFieldReference_InPriorFieldsOnlyScope_Resolves()
    {
        var ctx = BuildContext("""
            precept Widget
            field First as integer
            field Second as string
            state Open initial
            """);

        // Referencing "First" (index 0) when CurrentFieldIndex == 1 should succeed
        ctx.CurrentScope = FieldScopeMode.PriorFieldsOnly;
        ctx.CurrentFieldIndex = 1;

        var expr = new IdentifierExpression("First", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedFieldRef>();
        result.ResultType.Should().Be(TypeKind.Integer);
    }

    [Fact]
    public void EventArgReference_ResolvesToTypedArgRef()
    {
        var ctx = BuildContext("""
            precept Widget
            field Status as string
            state Open initial
            """);

        // Manually set up event arg context (bypasses pre-existing parser gap with event arg syntax)
        var arg = new TypedArg(
            Name: "Reason",
            EventName: "Submit",
            ResolvedType: TypeKind.String,
            ElementType: null,
            Modifiers: ImmutableArray<ModifierKind>.Empty,
            DefaultExpression: null,
            IsOptional: false,
            Presence: new DeclaredPresenceMeta.Guaranteed(),
            DeclaredQualifiers: ImmutableArray<DeclaredQualifierMeta>.Empty,
            Span: TestSpan);
        ctx.CurrentEventArgs = new Dictionary<string, TypedArg> { ["Reason"] = arg };

        var expr = new IdentifierExpression("Reason", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedArgRef>();
        var argRef = (TypedArgRef)result;
        argRef.ResultType.Should().Be(TypeKind.String);
        argRef.EventName.Should().Be("Submit");
        argRef.ArgName.Should().Be("Reason");
    }

    [Fact]
    public void EventArgShadowsField_WhenBothInScope()
    {
        var ctx = BuildContext("""
            precept Widget
            field Reason as integer
            state Open initial
            """);

        // Manually set up event arg with same name as field (bypasses parser gap)
        var arg = new TypedArg(
            Name: "Reason",
            EventName: "Submit",
            ResolvedType: TypeKind.String,
            ElementType: null,
            Modifiers: ImmutableArray<ModifierKind>.Empty,
            DefaultExpression: null,
            IsOptional: false,
            Presence: new DeclaredPresenceMeta.Guaranteed(),
            DeclaredQualifiers: ImmutableArray<DeclaredQualifierMeta>.Empty,
            Span: TestSpan);
        ctx.CurrentEventArgs = new Dictionary<string, TypedArg> { ["Reason"] = arg };

        var expr = new IdentifierExpression("Reason", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedArgRef>(
            because: "event args have higher priority than fields (D20)");
        result.ResultType.Should().Be(TypeKind.String);
    }

    [Fact]
    public void QuantifierBindingShadowsEventArg()
    {
        var ctx = BuildContext("""
            precept Widget
            field Reason as integer
            state Open initial
            """);

        // Manually set up event arg context
        var arg = new TypedArg(
            Name: "Reason",
            EventName: "Submit",
            ResolvedType: TypeKind.String,
            ElementType: null,
            Modifiers: ImmutableArray<ModifierKind>.Empty,
            DefaultExpression: null,
            IsOptional: false,
            Presence: new DeclaredPresenceMeta.Guaranteed(),
            DeclaredQualifiers: ImmutableArray<DeclaredQualifierMeta>.Empty,
            Span: TestSpan);
        ctx.CurrentEventArgs = new Dictionary<string, TypedArg> { ["Reason"] = arg };

        // Push a quantifier binding that shadows the event arg
        ctx.QuantifierBindings.Push(("Reason", TypeKind.Decimal, false));

        var expr = new IdentifierExpression("Reason", TestSpan);
        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedFieldRef>(
            because: "quantifier bindings are returned as TypedFieldRef (reuses field ref shape)");
        result.ResultType.Should().Be(TypeKind.Decimal);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  3. TypedBinaryOp — binary operator resolution
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StringPlusString_ResolvesToStringConcatenation()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.StringLiteral, "hello", TestSpan);
        var right = new LiteralExpression(TokenKind.StringLiteral, " world", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultType.Should().Be(TypeKind.String);
        binOp.ResolvedOp.Should().Be(OperationKind.StringPlusString);
    }

    [Fact]
    public void IntegerPlusInteger_ResolvesToIntegerAddition()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var right = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultType.Should().Be(TypeKind.Integer);
        binOp.ResolvedOp.Should().Be(OperationKind.IntegerPlusInteger);
    }

    [Fact]
    public void IntegerPlusDecimal_ResolvesViaExactCatalogEntry()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var right = new LiteralExpression(TokenKind.NumberLiteral, "2.5", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultType.Should().Be(TypeKind.Decimal);
        binOp.ResolvedOp.Should().Be(OperationKind.IntegerPlusDecimal);
    }

    [Fact]
    public void DecimalPlusInteger_ResolvesViaBidirectionalLookup()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.NumberLiteral, "2.5", TestSpan);
        var right = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultType.Should().Be(TypeKind.Decimal);
        binOp.ResolvedOp.Should().Be(OperationKind.IntegerPlusDecimal);
    }

    [Fact]
    public void IntegerPlusNumberField_ResolvesToNumber()
    {
        var ctx = BuildContext("""
            precept Widget
            field Score as number
            state Open initial
            """);

        var left  = new LiteralExpression(TokenKind.NumberLiteral, "10", TestSpan);
        var right = new IdentifierExpression("Score", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultType.Should().Be(TypeKind.Number);
        binOp.ResolvedOp.Should().Be(OperationKind.IntegerPlusNumber);
    }

    [Fact]
    public void BooleanPlusInteger_EmitsTypeMismatch()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var right = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.TypeMismatch.ToString());
    }

    [Fact]
    public void StringMinusString_EmitsTypeMismatch()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.StringLiteral, "hello", TestSpan);
        var right = new LiteralExpression(TokenKind.StringLiteral, "world", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Minus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.TypeMismatch.ToString());
    }

    [Fact]
    public void IntegerTimesInteger_ResolvesToMultiplication()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.NumberLiteral, "3", TestSpan);
        var right = new LiteralExpression(TokenKind.NumberLiteral, "4", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Star, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultType.Should().Be(TypeKind.Integer);
        binOp.ResolvedOp.Should().Be(OperationKind.IntegerTimesInteger);
    }

    [Fact]
    public void IntegerDivideInteger_CarriesProofRequirements()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.NumberLiteral, "10", TestSpan);
        var right = new LiteralExpression(TokenKind.NumberLiteral, "3", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Slash, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultType.Should().Be(TypeKind.Integer);
        binOp.ResolvedOp.Should().Be(OperationKind.IntegerDivideInteger);
        binOp.ProofRequirements.Should().NotBeEmpty(
            because: "division carries a non-zero divisor proof requirement");
    }

    [Fact]
    public void DecimalTimesDecimal_ResolvesCorrectly()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.NumberLiteral, "2.5", TestSpan);
        var right = new LiteralExpression(TokenKind.NumberLiteral, "3.0", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Star, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultType.Should().Be(TypeKind.Decimal);
        binOp.ResolvedOp.Should().Be(OperationKind.DecimalTimesDecimal);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  4. ErrorType propagation (D13)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BinaryOp_LeftErrorType_PropagatesError_NoAdditionalDiagnostic()
    {
        var ctx = MinimalContext();
        var left  = new MissingExpression(TestSpan); // produces TypedErrorExpression
        var right = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().HaveCount(1,
            because: "B3: the resolved MissingExpression now emits one lightweight TC diagnostic before D13 propagation");
    }

    [Fact]
    public void BinaryOp_RightErrorType_PropagatesError_NoAdditionalDiagnostic()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var right = new MissingExpression(TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().HaveCount(1,
            because: "B3: the resolved MissingExpression now emits one lightweight TC diagnostic before D13 propagation");
    }

    [Fact]
    public void BinaryOp_BothErrorType_PropagatesError()
    {
        var ctx = MinimalContext();
        var left  = new MissingExpression(TestSpan);
        var right = new MissingExpression(TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().HaveCount(2,
            because: "B3: ResolveBinaryOp resolves both MissingExpression operands before propagating the error");
    }

    [Fact]
    public void UnaryOp_ErrorTypeOperand_PropagatesError_NoAdditionalDiagnostic()
    {
        var ctx = MinimalContext();
        var operand = new MissingExpression(TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Minus, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().HaveCount(1,
            because: "B3: ResolveUnaryOp resolves the MissingExpression operand and emits one lightweight TC diagnostic before propagation");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  5. TypedUnaryOp — unary operator resolution (D12)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UnaryMinus_OnInteger_ResolvesToNegateInteger()
    {
        var ctx = MinimalContext();
        var operand = new LiteralExpression(TokenKind.NumberLiteral, "42", TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Minus, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedUnaryOp>();
        var unOp = (TypedUnaryOp)result;
        unOp.ResultType.Should().Be(TypeKind.Integer);
        unOp.ResolvedOp.Should().Be(OperationKind.NegateInteger);
    }

    [Fact]
    public void UnaryMinus_OnDecimal_ResolvesToNegateDecimal()
    {
        var ctx = MinimalContext();
        var operand = new LiteralExpression(TokenKind.NumberLiteral, "3.14", TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Minus, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedUnaryOp>();
        var unOp = (TypedUnaryOp)result;
        unOp.ResultType.Should().Be(TypeKind.Decimal);
        unOp.ResolvedOp.Should().Be(OperationKind.NegateDecimal);
    }

    [Fact]
    public void UnaryNot_OnBoolean_ResolvesToNotBoolean()
    {
        var ctx = MinimalContext();
        var operand = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Not, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedUnaryOp>();
        var unOp = (TypedUnaryOp)result;
        unOp.ResultType.Should().Be(TypeKind.Boolean);
        unOp.ResolvedOp.Should().Be(OperationKind.NotBoolean);
    }

    [Fact]
    public void UnaryMinus_OnString_EmitsTypeMismatch()
    {
        var ctx = MinimalContext();
        var operand = new LiteralExpression(TokenKind.StringLiteral, "hello", TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Minus, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.TypeMismatch.ToString());
    }

    [Fact]
    public void UnaryNot_OnInteger_EmitsTypeMismatch()
    {
        var ctx = MinimalContext();
        var operand = new LiteralExpression(TokenKind.NumberLiteral, "5", TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Not, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.TypeMismatch.ToString());
    }

    [Fact]
    public void UnaryMinus_OnNumberField_ResolvesToNegateNumber()
    {
        var ctx = BuildContext("""
            precept Widget
            field Score as number
            state Open initial
            """);

        var operand = new IdentifierExpression("Score", TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Minus, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedUnaryOp>();
        var unOp = (TypedUnaryOp)result;
        unOp.ResultType.Should().Be(TypeKind.Number);
        unOp.ResolvedOp.Should().Be(OperationKind.NegateNumber);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  6. Grouped (parenthesized) expression — transparent unwrap
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GroupedExpression_UnwrapsTransparently()
    {
        var ctx = MinimalContext();
        var inner = new LiteralExpression(TokenKind.NumberLiteral, "42", TestSpan);
        var expr  = new GroupedExpression(inner, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedLiteral>();
        result.ResultType.Should().Be(TypeKind.Integer);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  7. MissingExpression sentinel
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MissingExpression_ReturnsErrorExpression_EmitsDiagnostic()
    {
        var ctx = MinimalContext();
        var expr = new MissingExpression(TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle(
                because: "B3: TC emits lightweight diagnostic on MissingExpression to satisfy D26")
            .Which.Code.Should().Be(nameof(DiagnosticCode.TypeMismatch));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  8. Stub arms — no crash, no spurious diagnostics
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConditionalExpression_BooleanCondition_SameTypeBranches_ReturnsTypedConditional()
    {
        var ctx = MinimalContext();
        var cond = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var then = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var expr = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedConditional>();
        result.ResultType.Should().Be(TypeKind.Integer);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void PostfixOperationExpression_NonField_EmitsIsSetOnNonOptional()
    {
        var ctx = MinimalContext();
        var operand = new LiteralExpression(TokenKind.StringLiteral, "hello", TestSpan);
        var expr = new PostfixOperationExpression(operand, false, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(DiagnosticCode.IsSetOnNonOptional));
    }

    [Fact]
    public void QuantifierExpression_NonCollectionTarget_EmitsInvalidQuantifierTarget()
    {
        var ctx = MinimalContext();
        var collection = new IdentifierExpression("Amount", TestSpan);
        var predicate  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var expr = new QuantifierExpression(TokenKind.All, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(DiagnosticCode.InvalidQuantifierTarget));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  9. TypedBinaryOp carries resolved OperationMeta (D11)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypedBinaryOp_CarriesChildExpressions()
    {
        var ctx = MinimalContext();
        var left  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var right = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Plus, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.Left.Should().BeOfType<TypedLiteral>();
        binOp.Right.Should().BeOfType<TypedLiteral>();
    }

    [Fact]
    public void TypedUnaryOp_CarriesChildExpression()
    {
        var ctx = MinimalContext();
        var operand = new LiteralExpression(TokenKind.NumberLiteral, "5", TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Minus, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedUnaryOp>();
        var unOp = (TypedUnaryOp)result;
        unOp.Operand.Should().BeOfType<TypedLiteral>();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  10. Nested expressions — full resolution tree
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NestedBinaryOps_ResolveFullTree()
    {
        // (1 + 2) * 3
        var ctx = MinimalContext();
        var one   = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var two   = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var three = new LiteralExpression(TokenKind.NumberLiteral, "3", TestSpan);

        var add  = new BinaryOperationExpression(one, TokenKind.Plus, two, TestSpan);
        var mult = new BinaryOperationExpression(add, TokenKind.Star, three, TestSpan);

        var result = Resolve(mult, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var outer = (TypedBinaryOp)result;
        outer.ResolvedOp.Should().Be(OperationKind.IntegerTimesInteger);
        outer.Left.Should().BeOfType<TypedBinaryOp>();
        ((TypedBinaryOp)outer.Left).ResolvedOp.Should().Be(OperationKind.IntegerPlusInteger);
    }

    [Fact]
    public void NestedUnaryInBinary_ResolvesCorrectly()
    {
        // (-1) + 2
        var ctx = MinimalContext();
        var one = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var two = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);

        var neg = new UnaryOperationExpression(TokenKind.Minus, one, TestSpan);
        var add = new BinaryOperationExpression(neg, TokenKind.Plus, two, TestSpan);

        var result = Resolve(add, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResolvedOp.Should().Be(OperationKind.IntegerPlusInteger);
        binOp.Left.Should().BeOfType<TypedUnaryOp>();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  11. Field reference recording for LS navigation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FieldReference_RecordsSiteForLS()
    {
        var ctx = BuildContext("""
            precept Widget
            field Name as string
            state Open initial
            """);

        var expr = new IdentifierExpression("Name", TestSpan);
        Resolve(expr, ctx);

        ctx.FieldReferences.Should().ContainSingle(
            r => r.Field.Name == "Name",
            because: "field reference sites should be recorded for LS semantic tokens");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  12. Conditional expressions — ResolveConditional (R3)
    // ════════════════════════════════════════════════════════════════════════

    // ── Happy path ───────────────────────────────────────────────────────

    [Fact]
    public void Conditional_BooleanCondition_ReturnsTypedConditional()
    {
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var then  = new LiteralExpression(TokenKind.NumberLiteral, "10", TestSpan);
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "20", TestSpan);
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedConditional>();
        var typed = (TypedConditional)result;
        typed.Condition.Should().BeOfType<TypedLiteral>();
        typed.ThenBranch.Should().BeOfType<TypedLiteral>();
        typed.ElseBranch.Should().BeOfType<TypedLiteral>();
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Conditional_IntegerBranches_ResultTypeInteger()
    {
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.False, "false", TestSpan);
        var then  = new LiteralExpression(TokenKind.NumberLiteral, "42", TestSpan);
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "99", TestSpan);
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedConditional>();
        result.ResultType.Should().Be(TypeKind.Integer);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Conditional_WideningBranches_ResultTypeWider()
    {
        // Integer widens to Decimal
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var then  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);    // integer
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "2.5", TestSpan);  // decimal
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedConditional>();
        result.ResultType.Should().Be(TypeKind.Decimal);
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Conditional_StringBranches_ResultTypeString()
    {
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var then  = new LiteralExpression(TokenKind.StringLiteral, "yes", TestSpan);
        var @else = new LiteralExpression(TokenKind.StringLiteral, "no", TestSpan);
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedConditional>();
        result.ResultType.Should().Be(TypeKind.String);
        ctx.Diagnostics.Should().BeEmpty();
    }

    // ── Boolean enforcement on condition ─────────────────────────────────

    [Fact]
    public void Conditional_NonBooleanCondition_EmitsTypeMismatch()
    {
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.NumberLiteral, "42", TestSpan);   // integer, not boolean
        var then  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        Resolve(expr, ctx);

        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(DiagnosticCode.TypeMismatch));
    }

    [Fact]
    public void Conditional_NonBooleanCondition_ReturnsTypedErrorExpression()
    {
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.StringLiteral, "not-a-bool", TestSpan);
        var then  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
    }

    // ── D13 error propagation ────────────────────────────────────────────

    [Fact]
    public void Conditional_ConditionIsError_ReturnsErrorNoSecondDiagnostic()
    {
        // Undeclared field in condition → TypedErrorExpression from identifier resolution.
        // ResolveConditional should propagate the error without emitting a second diagnostic.
        var ctx = MinimalContext();
        var cond  = new IdentifierExpression("NoSuchField", TestSpan);
        var then  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        // Only the UndeclaredField diagnostic from identifier resolution — no TypeMismatch from conditional
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void Conditional_ThenBranchIsError_ReturnsError()
    {
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var then  = new IdentifierExpression("Ghost", TestSpan);                      // undeclared → error
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        // Only UndeclaredField from identifier resolution — no branch-type diagnostic
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void Conditional_ElseBranchIsError_ReturnsError()
    {
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var then  = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var @else = new IdentifierExpression("Phantom", TestSpan);                    // undeclared → error
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(DiagnosticCode.UndeclaredField));
    }

    // ── Branch type incompatibility ──────────────────────────────────────

    [Fact]
    public void Conditional_IncompatibleBranchTypes_EmitsTypeMismatch()
    {
        // String and Integer cannot widen to each other → TypeMismatch
        var ctx = MinimalContext();
        var cond  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var then  = new LiteralExpression(TokenKind.StringLiteral, "hello", TestSpan);
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "42", TestSpan);
        var expr  = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(DiagnosticCode.TypeMismatch));
    }

    // ── Integration: conditional in guard ────────────────────────────────

    [Fact]
    public void Conditional_InGuard_ResolvedCorrectly()
    {
        var precept = """
            precept Widget
            field Score as integer default 0
            field Bonus as integer default 0
            state Open initial
            state Closed
            event Close
            from Open on Close when if Score > 0 then Bonus > 10 else Bonus > 5 -> transition Closed
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var row = index.TransitionRows.Single();

        row.Guard.Should().NotBeNull();
        row.Guard.Should().BeOfType<TypedConditional>();
        var conditional = (TypedConditional)row.Guard!;
        conditional.ResultType.Should().Be(TypeKind.Boolean);
    }

    // ── B9: post-resolution assignment type validation ────────────────────

    [Fact]
    public void SetAction_IntegerToQuantityField_EmitsTypeMismatch()
    {
        var precept = """
            precept Widget
            field q as quantity of 'mass' default '0 kg'
            state Open initial
            state Closed
            event Update
            from Open on Update
                -> set q = 5
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.TypeMismatch);
    }

    [Fact]
    public void SetAction_IntegerToMoneyField_EmitsTypeMismatch()
    {
        var precept = """
            precept Widget
            field m as money in 'USD'
            state Open initial
            state Closed
            event Update
            from Open on Update
                -> set m = 42
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.TypeMismatch);
    }

    [Fact]
    public void SetAction_MatchingType_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field n as integer
            state Open initial
            state Closed
            event Update
            from Open on Update
                -> set n = 5
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetAction_WidenedType_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field d as decimal
            state Open initial
            state Closed
            event Update
            from Open on Update
                -> set d = 5
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetAction_MassDimensionToLengthField_EmitsDimensionCategoryMismatch()
    {
        var precept = """
            precept Widget
            field q as quantity of 'length'
            state Open initial
            state Closed
            event E(qq as quantity of 'mass')
            from Open on E
                -> set q = qq
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DimensionCategoryMismatch);
    }

    [Fact]
    public void SetAction_MatchingDimension_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field q as quantity of 'mass'
            state Open initial
            state Closed
            event E(qq as quantity of 'mass')
            from Open on E
                -> set q = qq
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void SetAction_USDToEURField_EmitsQualifierMismatch()
    {
        var precept = """
            precept Widget
            field m as money in 'USD'
            state Open initial
            state Closed
            event E(p as money in 'EUR')
            from Open on E
                -> set m = p
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.QualifierMismatch);
    }

    [Fact]
    public void SetAction_FieldToFieldDimensionMismatch_EmitsDimensionCategoryMismatch()
    {
        var precept = """
            precept Widget
            field q1 as quantity of 'length'
            field q2 as quantity of 'mass'
            state Open initial
            state Closed
            event E
            from Open on E
                -> set q1 = q2
                -> transition Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DimensionCategoryMismatch);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  12. QualifierBinding — ResultQualifier assertion (D11)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BinaryOp_MoneyOperation_ResultQualifierPopulated()
    {
        // D11: money ÷ money triggers multi-candidate qualifier disambiguation.
        // The TypeChecker selects the QualifierMatch.Same entry and maps it
        // to SameQualifierRequired on TypedBinaryOp.ResultQualifier.
        var ctx = BuildContext("""
            precept Invoice
            field Cost as money
            field Revenue as money
            state Open initial
            """);

        var left  = new IdentifierExpression("Cost", TestSpan);
        var right = new IdentifierExpression("Revenue", TestSpan);
        var expr  = new BinaryOperationExpression(left, TokenKind.Slash, right, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedBinaryOp>();
        var binOp = (TypedBinaryOp)result;
        binOp.ResultQualifier.Should().NotBeNull(
            because: "money ÷ money is a qualifier-disambiguated operation (D11)");
        binOp.ResultQualifier.Should().BeOfType<SameQualifierRequired>(
            because: "the TypeChecker defaults to QualifierMatch.Same for multi-candidate operations");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  G2 GAP — QualifierMatch.Different disambiguation path not reachable
    //
    //  The Operations catalog defines two entries with QualifierMatch.Different:
    //    MoneyDivideMoneyCrossCurrency    (money ÷ money → exchangerate)
    //    QuantityDivideQuantityCrossDimension  (quantity ÷ quantity → quantity)
    //
    //  However, DisambiguateCandidates in TypeChecker.cs always returns the
    //  QualifierMatch.Same entry when candidates contain both Same and Different
    //  options. Since both qualifier-disambiguated groups (money÷money,
    //  quantity÷quantity) each contain a Same entry, the Different entry is
    //  never selected. MapQualifierBinding(QualifierMatch.Different) → null is
    //  therefore dead code at the type-checker level.
    //
    //  This path becomes testable when DisambiguateCandidates gains
    //  qualifier-aware selection that can pick the Different candidate (e.g.,
    //  once the ProofEngine wires field-level qualifier tracking through to the
    //  type-checker disambiguation step).
    //
    //  TODO: Add BinaryOp_DifferentQualifierMoney_ResultQualifierReflectsDifferentPath
    //        test once DisambiguateCandidates can select the Different candidate
    //        and MapQualifierBinding(Different) → null is reachable.
    //        See .squad/decisions/inbox/soup-nazi-g1-g4-tests-written.md §G2.
    // ════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════
    //  13. Stub arm — ConditionalExpression (deferred, tested in section 8)
    //
    //  R3 audit: The original 9 stub arms have been reduced to 2 by
    //  implementation of QuantifierExpression, ListLiteralExpression, and
    //  PostfixOperationExpression resolvers. The remaining stubs are:
    //    1. ConditionalExpression (tested above in section 8, full resolver
    //       landing separately via ResolveConditional)
    //    2. _ default arm in Resolve (unreachable — all ParsedExpression
    //       subtypes have explicit match arms)
    //  No additional stub arm tests are needed at this time.
    // ════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════
    //  14. IsSetOnNonOptional — event arg path (G4)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsSet_OnNonOptionalEventArg_EmitsIsSetOnNonOptionalDiagnostic()
    {
        // DiagnosticCode.IsSetOnNonOptional covers three sub-cases:
        //   (a) non-optional field ref   — TypeCheckerStructuralTests.NonOptionalField_IsSet_InGuard_*
        //   (b) non-field/non-arg expr   — PostfixOperationExpression_NonField_EmitsIsSetOnNonOptional (§8)
        //   (c) non-optional event arg   — THIS TEST (R3 gap G4)
        //
        // 'Amount' is declared without 'optional' → TypedArg.IsOptional = false.
        // When the guard 'Submit.Amount is set' is type-checked, ResolvePostfixOp
        // finds the arg in ctx.CurrentEventArgs and emits IsSetOnNonOptional.
        var precept = """
            precept Widget
            field Count as integer default 0
            state Draft initial
            state Done
            event Submit(Amount as decimal)
            from Draft on Submit when Submit.Amount is set -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.IsSetOnNonOptional);
    }
}
