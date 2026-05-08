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
        var arg = new TypedArg("Reason", "Submit", TypeKind.String, null,
            ImmutableArray<ModifierKind>.Empty, null, false, TestSpan);
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
        var arg = new TypedArg("Reason", "Submit", TypeKind.String, null,
            ImmutableArray<ModifierKind>.Empty, null, false, TestSpan);
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
        var arg = new TypedArg("Reason", "Submit", TypeKind.String, null,
            ImmutableArray<ModifierKind>.Empty, null, false, TestSpan);
        ctx.CurrentEventArgs = new Dictionary<string, TypedArg> { ["Reason"] = arg };

        // Push a quantifier binding that shadows the event arg
        ctx.QuantifierBindings.Push(("Reason", TypeKind.Decimal));

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
        ctx.Diagnostics.Should().BeEmpty(
            because: "D13: MissingExpression → no diagnostic, error propagates silently");
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
        ctx.Diagnostics.Should().BeEmpty(
            because: "D13: error from right operand propagates without new diagnostic");
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
        ctx.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void UnaryOp_ErrorTypeOperand_PropagatesError_NoAdditionalDiagnostic()
    {
        var ctx = MinimalContext();
        var operand = new MissingExpression(TestSpan);
        var expr = new UnaryOperationExpression(TokenKind.Minus, operand, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().BeEmpty(
            because: "D13: error from operand propagates without new diagnostic");
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
    public void MissingExpression_ReturnsErrorExpression_NoDiagnostic()
    {
        var ctx = MinimalContext();
        var expr = new MissingExpression(TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().BeEmpty(
            because: "parser already emitted the diagnostic for the missing expression");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  8. Stub arms — no crash, no spurious diagnostics
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConditionalExpression_Stub_ReturnsErrorExpression_NoDiagnostic()
    {
        var ctx = MinimalContext();
        var cond = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var then = new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan);
        var @else = new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan);
        var expr = new ConditionalExpression(cond, then, @else, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().BeEmpty(
            because: "conditional expressions are deferred to later slices");
    }

    [Fact]
    public void PostfixOperationExpression_Stub_ReturnsErrorExpression_NoDiagnostic()
    {
        var ctx = MinimalContext();
        var operand = new LiteralExpression(TokenKind.StringLiteral, "hello", TestSpan);
        var expr = new PostfixOperationExpression(operand, false, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().BeEmpty(
            because: "postfix operations are deferred to later slices");
    }

    [Fact]
    public void QuantifierExpression_Stub_ReturnsErrorExpression_NoDiagnostic()
    {
        var ctx = MinimalContext();
        var collection = new IdentifierExpression("Amount", TestSpan);
        var predicate  = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var expr = new QuantifierExpression(TokenKind.All, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().BeEmpty(
            because: "quantifier expressions are deferred to later slices");
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
}
