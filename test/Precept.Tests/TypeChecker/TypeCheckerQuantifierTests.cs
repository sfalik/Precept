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
/// Slice 9 — Quantifier + List Literal Resolution.
/// Covers TypedQuantifier (each/any/no binding scope, element TypeKind per D2,
/// scope pop verification), TypedListLiteral (type unification with widening,
/// incompatible type errors), and ErrorType propagation (D13).
/// </summary>
public class TypeCheckerQuantifierTests
{
    // ── Shared test span ──────────────────────────────────────────────────

    private static readonly SourceSpan TestSpan = new(0, 1, 1, 1, 1, 2);

    // ── Context builder helpers ───────────────────────────────────────────

    private static CheckContext BuildContext(string preceptText)
    {
        var tokens   = Lexer.Lex(preceptText);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);
        var symbols  = Precept.Pipeline.NameBinder.Bind(manifest);
        return Precept.Pipeline.TypeChecker.CreateContext(manifest, symbols);
    }

    private static TypedExpression Resolve(ParsedExpression expr, CheckContext ctx) =>
        Precept.Pipeline.TypeChecker.ResolveExpression(expr, ctx);

    /// <summary>Precept with a set-of-string field for quantifier tests.</summary>
    private static CheckContext CollectionContext() => BuildContext("""
        precept Widget
        field Tags as set of string
        field Amount as integer
        state Open initial
        """);

    /// <summary>Precept with a set-of-number field for numeric quantifier tests.</summary>
    private static CheckContext NumericCollectionContext() => BuildContext("""
        precept Widget
        field Scores as set of number
        state Open initial
        """);

    // ════════════════════════════════════════════════════════════════════════
    //  1. TypedQuantifier — happy path
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EachQuantifier_WithSetOfString_ResolvesToBooleanResult()
    {
        var ctx = CollectionContext();
        // each x in Tags (x = "active")
        var collection = new IdentifierExpression("Tags", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("x", TestSpan),
            TokenKind.DoubleEquals,
            new LiteralExpression(TokenKind.StringLiteral, "active", TestSpan),
            TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedQuantifier>();
        var quant = (TypedQuantifier)result;
        quant.ResultType.Should().Be(TypeKind.Boolean);
        quant.BindingName.Should().Be("x");
    }

    [Fact]
    public void AnyQuantifier_WithSetOfNumber_ResolvesToBoolean()
    {
        var ctx = NumericCollectionContext();
        // any item in Scores (item > 100)
        var collection = new IdentifierExpression("Scores", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("item", TestSpan),
            TokenKind.GreaterThan,
            new LiteralExpression(TokenKind.NumberLiteral, "100", TestSpan),
            TestSpan);
        var expr = new QuantifierExpression(TokenKind.Any, "item", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedQuantifier>();
        var quant = (TypedQuantifier)result;
        quant.ResultType.Should().Be(TypeKind.Boolean);
        quant.BindingName.Should().Be("item");
    }

    [Fact]
    public void NoQuantifier_WithSetOfString_ResolvesToBoolean()
    {
        var ctx = CollectionContext();
        // no entry in Tags (entry = "failed")
        var collection = new IdentifierExpression("Tags", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("entry", TestSpan),
            TokenKind.DoubleEquals,
            new LiteralExpression(TokenKind.StringLiteral, "failed", TestSpan),
            TestSpan);
        var expr = new QuantifierExpression(TokenKind.No, "entry", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedQuantifier>();
        result.ResultType.Should().Be(TypeKind.Boolean);
    }

    [Fact]
    public void BindingVar_HasCorrectElementTypeKind()
    {
        var ctx = CollectionContext();
        // each x in Tags (x = "a") — x should bind as String (element type of set of string)
        var collection = new IdentifierExpression("Tags", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("x", TestSpan),
            TokenKind.DoubleEquals,
            new LiteralExpression(TokenKind.StringLiteral, "a", TestSpan),
            TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedQuantifier>();
        var quant = (TypedQuantifier)result;
        quant.BindingType.Should().Be(TypeKind.String,
            because: "element type of 'set of string' is String");
    }

    [Fact]
    public void BindingVar_NumericCollection_HasNumberElementType()
    {
        var ctx = NumericCollectionContext();
        // each s in Scores (s > 0)
        var collection = new IdentifierExpression("Scores", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("s", TestSpan),
            TokenKind.GreaterThan,
            new LiteralExpression(TokenKind.NumberLiteral, "0", TestSpan),
            TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "s", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedQuantifier>();
        ((TypedQuantifier)result).BindingType.Should().Be(TypeKind.Number);
    }

    [Fact]
    public void BindingVar_ShadowsFieldOfSameName()
    {
        var ctx = CollectionContext();
        // Field "Amount" is integer, but binding "Amount" via set-of-string should be String
        var collection = new IdentifierExpression("Tags", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("Amount", TestSpan),
            TokenKind.DoubleEquals,
            new LiteralExpression(TokenKind.StringLiteral, "x", TestSpan),
            TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "Amount", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedQuantifier>(
            because: "binding 'Amount' as String shadows the Integer field 'Amount'");
        var quant = (TypedQuantifier)result;
        quant.BindingType.Should().Be(TypeKind.String);
    }

    [Fact]
    public void BindingVar_ShadowsEventArg()
    {
        var ctx = CollectionContext();
        // Manually set up event arg named "x" with Integer type
        var arg = new TypedArg("x", "Submit", TypeKind.Integer, null,
            ImmutableArray<ModifierKind>.Empty, null, false, TestSpan);
        ctx.CurrentEventArgs = new Dictionary<string, TypedArg> { ["x"] = arg };

        var collection = new IdentifierExpression("Tags", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("x", TestSpan),
            TokenKind.DoubleEquals,
            new LiteralExpression(TokenKind.StringLiteral, "a", TestSpan),
            TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedQuantifier>(
            because: "quantifier binding 'x' shadows the event arg 'x'");
    }

    [Fact]
    public void NestedQuantifier_ResolvesCorrectly()
    {
        // Need two collection fields for nesting
        var ctx = BuildContext("""
            precept Widget
            field Tags as set of string
            field Labels as set of string
            state Open initial
            """);

        // each x in Tags (any y in Labels (y = x))
        var innerCollection = new IdentifierExpression("Labels", TestSpan);
        var innerPredicate = new BinaryOperationExpression(
            new IdentifierExpression("y", TestSpan),
            TokenKind.DoubleEquals,
            new IdentifierExpression("x", TestSpan),
            TestSpan);
        var innerQuantifier = new QuantifierExpression(TokenKind.Any, "y", innerCollection, innerPredicate, TestSpan);

        var outerCollection = new IdentifierExpression("Tags", TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "x", outerCollection, innerQuantifier, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedQuantifier>();
        var quant = (TypedQuantifier)result;
        quant.ResultType.Should().Be(TypeKind.Boolean);
        quant.Predicate.Should().BeOfType<TypedQuantifier>(
            because: "inner quantifier resolves as TypedQuantifier");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  2. TypedQuantifier — error cases
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NonCollectionTarget_EmitsInvalidQuantifierTarget()
    {
        var ctx = CollectionContext();
        // each x in Amount (...) — Amount is integer, not a collection
        var collection = new IdentifierExpression("Amount", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("x", TestSpan),
            TokenKind.GreaterThan,
            new LiteralExpression(TokenKind.NumberLiteral, "0", TestSpan),
            TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.InvalidQuantifierTarget.ToString());
    }

    [Fact]
    public void QuantifierPredicate_NonBoolean_EmitsQuantifierPredicateNotBoolean()
    {
        var ctx = CollectionContext();
        // each x in Tags (x) — x resolves to String, not Boolean
        var collection = new IdentifierExpression("Tags", TestSpan);
        var predicate = new IdentifierExpression("x", TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.QuantifierPredicateNotBoolean.ToString());
    }

    [Fact]
    public void QuantifierTarget_IsErrorExpression_ReturnsError_NoSecondDiagnostic()
    {
        var ctx = CollectionContext();
        // B3: error collection → TypedErrorExpression after one lightweight TC diagnostic
        var collection = new MissingExpression(TestSpan);
        var predicate = new LiteralExpression(TokenKind.True, "true", TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().HaveCount(1,
            because: "B3: ResolveQuantifier resolves the MissingExpression collection first and short-circuits after that single diagnostic");
    }

    [Fact]
    public void QuantifierPredicate_IsErrorExpression_ReturnsError_NoSecondDiagnostic()
    {
        var ctx = CollectionContext();
        var collection = new IdentifierExpression("Tags", TestSpan);
        var predicate = new MissingExpression(TestSpan);
        var expr = new QuantifierExpression(TokenKind.Each, "x", collection, predicate, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().HaveCount(1,
            because: "B3: ResolveQuantifier resolves the MissingExpression predicate and emits one lightweight TC diagnostic before propagation");
    }

    [Fact]
    public void BindingVar_OutOfScope_AfterQuantifier_EmitsUndeclaredField()
    {
        var ctx = CollectionContext();

        // First resolve a quantifier so binding "x" is pushed/popped
        var collection = new IdentifierExpression("Tags", TestSpan);
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("x", TestSpan),
            TokenKind.DoubleEquals,
            new LiteralExpression(TokenKind.StringLiteral, "a", TestSpan),
            TestSpan);
        var quantExpr = new QuantifierExpression(TokenKind.Each, "x", collection, predicate, TestSpan);
        Resolve(quantExpr, ctx);

        // Now try to reference "x" outside the quantifier scope
        var outsideRef = new IdentifierExpression("x", TestSpan);
        var result = Resolve(outsideRef, ctx);

        result.Should().BeOfType<TypedErrorExpression>(
            because: "binding 'x' should be popped after quantifier completes");
        ctx.Diagnostics
            .Should().Contain(d => d.Code == DiagnosticCode.UndeclaredField.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  3. TypedListLiteral — happy path
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IntegerListLiteral_ResolvesWithIntegerElementType()
    {
        var ctx = CollectionContext();
        var elements = ImmutableArray.Create<ParsedExpression>(
            new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan),
            new LiteralExpression(TokenKind.NumberLiteral, "2", TestSpan),
            new LiteralExpression(TokenKind.NumberLiteral, "3", TestSpan));
        var expr = new ListLiteralExpression(elements, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedListLiteral>();
        var list = (TypedListLiteral)result;
        list.ResultType.Should().Be(TypeKind.List);
        list.ElementType.Should().Be(TypeKind.Integer);
        list.Elements.Should().HaveCount(3);
    }

    [Fact]
    public void StringListLiteral_ResolvesWithStringElementType()
    {
        var ctx = CollectionContext();
        var elements = ImmutableArray.Create<ParsedExpression>(
            new LiteralExpression(TokenKind.StringLiteral, "a", TestSpan),
            new LiteralExpression(TokenKind.StringLiteral, "b", TestSpan));
        var expr = new ListLiteralExpression(elements, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedListLiteral>();
        var list = (TypedListLiteral)result;
        list.ResultType.Should().Be(TypeKind.List);
        list.ElementType.Should().Be(TypeKind.String);
        list.Elements.Should().HaveCount(2);
    }

    [Fact]
    public void MixedIntegerDecimal_WidensToDecimal()
    {
        var ctx = CollectionContext();
        // [1, 2.5] — Integer widens to Decimal
        var elements = ImmutableArray.Create<ParsedExpression>(
            new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan),
            new LiteralExpression(TokenKind.NumberLiteral, "2.5", TestSpan));
        var expr = new ListLiteralExpression(elements, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedListLiteral>();
        var list = (TypedListLiteral)result;
        list.ResultType.Should().Be(TypeKind.List);
        list.ElementType.Should().Be(TypeKind.Decimal,
            because: "Integer widens to Decimal via IsAssignable");
    }

    [Fact]
    public void EmptyListLiteral_ResolvesWithErrorElementType()
    {
        var ctx = CollectionContext();
        var expr = new ListLiteralExpression(ImmutableArray<ParsedExpression>.Empty, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedListLiteral>();
        var list = (TypedListLiteral)result;
        list.ResultType.Should().Be(TypeKind.List);
        list.ElementType.Should().Be(TypeKind.Error,
            because: "empty list cannot infer element type");
        list.Elements.Should().BeEmpty();
    }

    [Fact]
    public void SingleElementListLiteral_ResolvesWithThatElementType()
    {
        var ctx = CollectionContext();
        var elements = ImmutableArray.Create<ParsedExpression>(
            new LiteralExpression(TokenKind.True, "true", TestSpan));
        var expr = new ListLiteralExpression(elements, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedListLiteral>();
        var list = (TypedListLiteral)result;
        list.ElementType.Should().Be(TypeKind.Boolean);
        list.Elements.Should().HaveCount(1);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  4. TypedListLiteral — error cases
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IncompatibleElementTypes_EmitsTypeMismatch()
    {
        var ctx = CollectionContext();
        // [1, "hello"] — Integer and String are incompatible
        var elements = ImmutableArray.Create<ParsedExpression>(
            new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan),
            new LiteralExpression(TokenKind.StringLiteral, "hello", TestSpan));
        var expr = new ListLiteralExpression(elements, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.TypeMismatch.ToString());
    }

    [Fact]
    public void ListElement_IsErrorExpression_ReturnsError_NoSecondDiagnostic()
    {
        var ctx = CollectionContext();
        var elements = ImmutableArray.Create<ParsedExpression>(
            new LiteralExpression(TokenKind.NumberLiteral, "1", TestSpan),
            new MissingExpression(TestSpan));
        var expr = new ListLiteralExpression(elements, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().HaveCount(1,
            because: "B3: ResolveListLiteral resolves every element, and the single MissingExpression element emits one lightweight TC diagnostic");
    }

    [Fact]
    public void AllErrorElements_ReturnsError_NoSecondDiagnostic()
    {
        var ctx = CollectionContext();
        var elements = ImmutableArray.Create<ParsedExpression>(
            new MissingExpression(TestSpan),
            new MissingExpression(TestSpan));
        var expr = new ListLiteralExpression(elements, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics.Should().HaveCount(2,
            because: "B3: ResolveListLiteral resolves both MissingExpression elements before returning the propagated error");
    }

    [Fact]
    public void BooleanAndInteger_IncompatibleTypes_EmitsTypeMismatch()
    {
        var ctx = CollectionContext();
        // [true, 42] — Boolean and Integer are incompatible
        var elements = ImmutableArray.Create<ParsedExpression>(
            new LiteralExpression(TokenKind.True, "true", TestSpan),
            new LiteralExpression(TokenKind.NumberLiteral, "42", TestSpan));
        var expr = new ListLiteralExpression(elements, TestSpan);

        var result = Resolve(expr, ctx);

        result.Should().BeOfType<TypedErrorExpression>();
        ctx.Diagnostics
            .Should().ContainSingle(d => d.Code == DiagnosticCode.TypeMismatch.ToString());
    }
}
