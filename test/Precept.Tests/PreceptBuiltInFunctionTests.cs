using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

public class PreceptBuiltInFunctionTests
{
    // ─── Evaluation: abs ────────────────────────────────────────────

    [Fact]
    public void Eval_Abs_NegativeNumber_ReturnsPositive()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as number default 0",
            "Output", "abs(Input)",
            new Dictionary<string, object?> { ["Input"] = -5.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(5.0);
    }

    [Fact]
    public void Eval_Abs_NegativeInteger_ReturnsPositiveInteger()
    {
        var fire = FireForSet(
            "field Input as integer default 0\nfield Output as integer default 0",
            "Output", "abs(Input)",
            new Dictionary<string, object?> { ["Input"] = -5L, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(5L);
    }

    [Fact]
    public void Eval_Abs_PositiveNumber_ReturnsSameValue()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as number default 0",
            "Output", "abs(Input)",
            new Dictionary<string, object?> { ["Input"] = 5.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(5.0);
    }

    // ─── Evaluation: floor ──────────────────────────────────────────

    [Fact]
    public void Eval_Floor_PositiveFraction_RoundsDown()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as integer default 0",
            "Output", "floor(Input)",
            new Dictionary<string, object?> { ["Input"] = 3.7, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(3L);
    }

    [Fact]
    public void Eval_Floor_NegativeFraction_RoundsTowardNegativeInfinity()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as integer default 0",
            "Output", "floor(Input)",
            new Dictionary<string, object?> { ["Input"] = -3.2, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(-4L);
    }

    [Fact]
    public void Eval_Floor_WholeNumber_ReturnsSameValue()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as integer default 0",
            "Output", "floor(Input)",
            new Dictionary<string, object?> { ["Input"] = 5.0, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(5L);
    }

    // ─── Evaluation: ceil ───────────────────────────────────────────

    [Fact]
    public void Eval_Ceil_PositiveFraction_RoundsUp()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as integer default 0",
            "Output", "ceil(Input)",
            new Dictionary<string, object?> { ["Input"] = 3.2, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(4L);
    }

    [Fact]
    public void Eval_Ceil_NegativeFraction_RoundsTowardPositiveInfinity()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as integer default 0",
            "Output", "ceil(Input)",
            new Dictionary<string, object?> { ["Input"] = -3.7, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(-3L);
    }

    // ─── Evaluation: truncate ───────────────────────────────────────

    [Fact]
    public void Eval_Truncate_PositiveFraction_TruncatesTowardZero()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as integer default 0",
            "Output", "truncate(Input)",
            new Dictionary<string, object?> { ["Input"] = 3.7, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(3L);
    }

    [Fact]
    public void Eval_Truncate_NegativeFraction_TruncatesTowardZero()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as integer default 0",
            "Output", "truncate(Input)",
            new Dictionary<string, object?> { ["Input"] = -3.7, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(-3L);
    }

    // ─── Evaluation: round ──────────────────────────────────────────

    [Fact]
    public void Eval_Round_BankersRounding_HalfToEven_RoundsDown()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as number default 0",
            "Output", "round(Input)",
            new Dictionary<string, object?> { ["Input"] = 2.5, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(2.0);
    }

    [Fact]
    public void Eval_Round_BankersRounding_HalfToEven_RoundsUp()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as number default 0",
            "Output", "round(Input)",
            new Dictionary<string, object?> { ["Input"] = 3.5, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(4.0);
    }

    [Fact]
    public void Eval_Round_BelowHalf_RoundsDown()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as number default 0",
            "Output", "round(Input)",
            new Dictionary<string, object?> { ["Input"] = 2.4, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(2.0);
    }

    [Fact]
    public void Eval_Round_AboveHalf_RoundsUp()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as number default 0",
            "Output", "round(Input)",
            new Dictionary<string, object?> { ["Input"] = 2.6, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(3.0);
    }

    [Fact]
    public void Eval_Round_WithPrecision_ReturnsDecimalResult()
    {
        var fire = FireForSet(
            "field Input as number default 0\nfield Output as decimal default 0",
            "Output", "round(Input, 2)",
            new Dictionary<string, object?> { ["Input"] = 3.14159, ["Output"] = 0m });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(3.14m);
    }

    // ─── Evaluation: min ────────────────────────────────────────────

    [Fact]
    public void Eval_Min_TwoArgs_ReturnsSmallest()
    {
        var fire = FireForSet(
            "field X as number default 0\nfield Output as number default 0",
            "Output", "min(X, 7.0)",
            new Dictionary<string, object?> { ["X"] = 3.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(3.0);
    }

    [Fact]
    public void Eval_Min_ThreeArgs_ReturnsSmallest()
    {
        var fire = FireForSet(
            "field A as number default 0\nfield B as number default 0\nfield C as number default 0\nfield Output as number default 0",
            "Output", "min(A, B, C)",
            new Dictionary<string, object?> { ["A"] = 5.0, ["B"] = 2.0, ["C"] = 8.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(2.0);
    }

    // ─── Evaluation: max ────────────────────────────────────────────

    [Fact]
    public void Eval_Max_TwoArgs_ReturnsLargest()
    {
        var fire = FireForSet(
            "field X as number default 0\nfield Output as number default 0",
            "Output", "max(X, 7.0)",
            new Dictionary<string, object?> { ["X"] = 3.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(7.0);
    }

    [Fact]
    public void Eval_Max_ThreeArgs_ReturnsLargest()
    {
        var fire = FireForSet(
            "field A as number default 0\nfield B as number default 0\nfield C as number default 0\nfield Output as number default 0",
            "Output", "max(A, B, C)",
            new Dictionary<string, object?> { ["A"] = 5.0, ["B"] = 2.0, ["C"] = 8.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(8.0);
    }

    // ─── Evaluation: clamp ──────────────────────────────────────────

    [Fact]
    public void Eval_Clamp_InRange_ReturnsSameValue()
    {
        var fire = FireForSet(
            "field X as number default 0\nfield Output as number default 0",
            "Output", "clamp(X, 1.0, 10.0)",
            new Dictionary<string, object?> { ["X"] = 5.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(5.0);
    }

    [Fact]
    public void Eval_Clamp_BelowMin_ReturnsMin()
    {
        var fire = FireForSet(
            "field X as number default 0\nfield Output as number default 0",
            "Output", "clamp(X, 0.0, 10.0)",
            new Dictionary<string, object?> { ["X"] = -5.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(0.0);
    }

    [Fact]
    public void Eval_Clamp_AboveMax_ReturnsMax()
    {
        var fire = FireForSet(
            "field X as number default 0\nfield Output as number default 0",
            "Output", "clamp(X, 0.0, 10.0)",
            new Dictionary<string, object?> { ["X"] = 15.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(10.0);
    }

    // ─── Evaluation: pow ────────────────────────────────────────────

    [Fact]
    public void Eval_Pow_IntegerBase_ReturnsCorrectPower()
    {
        var fire = FireForSet(
            "field X as integer default 0\nfield Output as integer default 0",
            "Output", "pow(X, 3)",
            new Dictionary<string, object?> { ["X"] = 2L, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(8L);
    }

    [Fact]
    public void Eval_Pow_ZeroExponent_ReturnsOne()
    {
        var fire = FireForSet(
            "field X as integer default 0\nfield Output as integer default 0",
            "Output", "pow(X, 0)",
            new Dictionary<string, object?> { ["X"] = 5L, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(1L);
    }

    [Fact]
    public void Eval_Pow_ZeroBaseZeroExponent_ReturnsOne()
    {
        var fire = FireForSet(
            "field X as integer default 0\nfield Output as integer default 0",
            "Output", "pow(X, 0)",
            new Dictionary<string, object?> { ["X"] = 0L, ["Output"] = 0L });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(1L);
    }

    // ─── Evaluation: sqrt ───────────────────────────────────────────

    [Fact]
    public void Eval_Sqrt_ReturnsSquareRoot()
    {
        var fire = FireForSet(
            "field Input as number nonnegative default 0\nfield Output as number default 0",
            "Output", "sqrt(Input)",
            new Dictionary<string, object?> { ["Input"] = 9.0, ["Output"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(3.0);
    }

    // ─── Parse verification ─────────────────────────────────────────

    [Theory]
    [InlineData("abs(X)")]
    [InlineData("floor(X)")]
    [InlineData("ceil(X)")]
    [InlineData("round(X)")]
    [InlineData("round(X, 2)")]
    [InlineData("truncate(X)")]
    [InlineData("min(X, 5)")]
    [InlineData("max(X, 5)")]
    [InlineData("clamp(X, 0, 10)")]
    [InlineData("pow(X, 2)")]
    public void Parse_NumericFunction_InGuard_Succeeds(string expr)
    {
        var dsl = $"precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when {expr} >= 0 -> transition B\n";
        var model = PreceptParser.Parse(dsl);
        model.Should().NotBeNull();
    }

    [Fact]
    public void Parse_Min_DisambiguatesFromConstraintKeyword()
    {
        var dsl = "precept Test\nfield X as number min 0 default 5\nstate A initial\nstate B\nevent Go\nfrom A on Go when min(X, 3) > 0 -> transition B\n";
        var model = PreceptParser.Parse(dsl);
        model.Should().NotBeNull();
    }

    // ─── Type checker diagnostics ───────────────────────────────────

    [Fact]
    public void TypeChecker_C72_WrongArity_Abs()
    {
        var dsl = "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when abs(X, X) > 0 -> no transition\n";
        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);
        validation.Diagnostics.Should().Contain(d => d.Constraint.Id == "C72");
    }

    [Fact]
    public void TypeChecker_C74_RoundPrecisionNotLiteral()
    {
        var dsl = "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when round(X, X) > 0 -> no transition\n";
        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);
        validation.Diagnostics.Should().Contain(d => d.Constraint.Id == "C74");
    }

    [Fact]
    public void TypeChecker_C75_PowExponentNotInteger()
    {
        var dsl = "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when pow(X, X) > 0 -> no transition\n";
        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);
        validation.Diagnostics.Should().Contain(d => d.Constraint.Id == "C75");
    }

    [Fact]
    public void TypeChecker_C76_SqrtWithoutProof()
    {
        var dsl = "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when sqrt(X) > 0 -> no transition\n";
        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);
        validation.Diagnostics.Should().Contain(d => d.Constraint.Id == "C76");
    }

    [Fact]
    public void TypeChecker_C76_SqrtWithNonneg_NoDiagnostic()
    {
        var dsl = "precept Test\nfield X as number nonnegative default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when sqrt(X) > 0 -> no transition\n";
        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);
        validation.Diagnostics.Where(d => d.Constraint.Id == "C76").Should().BeEmpty();
    }

    [Fact]
    public void TypeChecker_C77_NullableArg()
    {
        var dsl = "precept Test\nfield X as number nullable default null\nstate A initial\nstate B\nevent Go\nfrom A on Go when abs(X) > 0 -> no transition\n";
        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);
        validation.Diagnostics.Should().Contain(d => d.Constraint.Id == "C77");
    }

    // ─── Evaluation: toLower ────────────────────────────────────────

    [Fact]
    public void Eval_ToLower_MixedCase_ReturnsLowerCase()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "toLower(Input)",
            new Dictionary<string, object?> { ["Input"] = "Hello", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("hello");
    }

    [Fact]
    public void Eval_ToLower_AllUpperCase_ReturnsLowerCase()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "toLower(Input)",
            new Dictionary<string, object?> { ["Input"] = "WORLD", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("world");
    }

    [Fact]
    public void Eval_ToLower_EmptyString_ReturnsEmpty()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "toLower(Input)",
            new Dictionary<string, object?> { ["Input"] = "", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("");
    }

    // ─── Evaluation: toUpper ────────────────────────────────────────

    [Fact]
    public void Eval_ToUpper_LowerCase_ReturnsUpperCase()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "toUpper(Input)",
            new Dictionary<string, object?> { ["Input"] = "hello", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("HELLO");
    }

    [Fact]
    public void Eval_ToUpper_EmptyString_ReturnsEmpty()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "toUpper(Input)",
            new Dictionary<string, object?> { ["Input"] = "", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("");
    }

    // ─── Evaluation: trim ───────────────────────────────────────────

    [Fact]
    public void Eval_Trim_LeadingAndTrailingWhitespace_ReturnsTrimmed()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "trim(Input)",
            new Dictionary<string, object?> { ["Input"] = "  hello  ", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("hello");
    }

    [Fact]
    public void Eval_Trim_NoWhitespace_ReturnsSameValue()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "trim(Input)",
            new Dictionary<string, object?> { ["Input"] = "no_spaces", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("no_spaces");
    }

    // ─── Evaluation: startsWith ─────────────────────────────────────

    [Fact]
    public void Eval_StartsWith_MatchingPrefix_AllowsTransition()
    {
        var fire = FireForGuard(
            "field Name as string default \"\"",
            "startsWith(Name, \"Hello\")",
            new Dictionary<string, object?> { ["Name"] = "Hello World" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void Eval_StartsWith_NonMatchingPrefix_NoTransition()
    {
        var fire = FireForGuard(
            "field Name as string default \"\"",
            "startsWith(Name, \"World\")",
            new Dictionary<string, object?> { ["Name"] = "Hello" });

        fire.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    [Fact]
    public void Eval_StartsWith_CaseSensitive_NoTransition()
    {
        var fire = FireForGuard(
            "field Name as string default \"\"",
            "startsWith(Name, \"hello\")",
            new Dictionary<string, object?> { ["Name"] = "Hello" });

        fire.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    // ─── Evaluation: endsWith ───────────────────────────────────────

    [Fact]
    public void Eval_EndsWith_MatchingSuffix_AllowsTransition()
    {
        var fire = FireForGuard(
            "field Name as string default \"\"",
            "endsWith(Name, \"World\")",
            new Dictionary<string, object?> { ["Name"] = "Hello World" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void Eval_EndsWith_NonMatchingSuffix_NoTransition()
    {
        var fire = FireForGuard(
            "field Name as string default \"\"",
            "endsWith(Name, \"World\")",
            new Dictionary<string, object?> { ["Name"] = "Hello" });

        fire.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    // ─── Evaluation: left ───────────────────────────────────────────

    [Fact]
    public void Eval_Left_NormalLength_ReturnsPrefix()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "left(Input, 5)",
            new Dictionary<string, object?> { ["Input"] = "Hello World", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("Hello");
    }

    [Fact]
    public void Eval_Left_CountExceedsLength_ClampsToFullString()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "left(Input, 10)",
            new Dictionary<string, object?> { ["Input"] = "Hi", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("Hi");
    }

    [Fact]
    public void Eval_Left_ZeroCount_ReturnsEmpty()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "left(Input, 0)",
            new Dictionary<string, object?> { ["Input"] = "Hello", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("");
    }

    // ─── Evaluation: right ──────────────────────────────────────────

    [Fact]
    public void Eval_Right_NormalLength_ReturnsSuffix()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "right(Input, 5)",
            new Dictionary<string, object?> { ["Input"] = "Hello World", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("World");
    }

    [Fact]
    public void Eval_Right_CountExceedsLength_ClampsToFullString()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "right(Input, 10)",
            new Dictionary<string, object?> { ["Input"] = "Hi", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("Hi");
    }

    // ─── Evaluation: mid ────────────────────────────────────────────

    [Fact]
    public void Eval_Mid_NormalRange_ReturnsSubstring()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "mid(Input, 7, 5)",
            new Dictionary<string, object?> { ["Input"] = "Hello World", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("World");
    }

    [Fact]
    public void Eval_Mid_FromStart_ReturnsPrefix()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "mid(Input, 1, 3)",
            new Dictionary<string, object?> { ["Input"] = "Hello", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("Hel");
    }

    [Fact]
    public void Eval_Mid_LengthExceedsRemaining_ClampsToEnd()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "mid(Input, 1, 10)",
            new Dictionary<string, object?> { ["Input"] = "Hi", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("Hi");
    }

    [Fact]
    public void Eval_Mid_StartBeyondLength_ReturnsEmpty()
    {
        var fire = FireForSet(
            "field Input as string default \"\"\nfield Output as string default \"\"",
            "Output", "mid(Input, 10, 3)",
            new Dictionary<string, object?> { ["Input"] = "Hello", ["Output"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be("");
    }

    // ─── Parse verification: string functions ───────────────────────

    [Theory]
    [InlineData("toLower(Name)")]
    [InlineData("toUpper(Name)")]
    [InlineData("trim(Name)")]
    [InlineData("startsWith(Name, \"prefix\")")]
    [InlineData("endsWith(Name, \"suffix\")")]
    [InlineData("left(Name, 3)")]
    [InlineData("right(Name, 3)")]
    [InlineData("mid(Name, 1, 5)")]
    public void Parse_StringFunction_InExpression_Succeeds(string expr)
    {
        var dsl = $"precept Test\nfield Name as string default \"test\"\nfield Output as string default \"\"\nstate A initial\nstate B\nevent Go\n";
        if (expr.StartsWith("startsWith") || expr.StartsWith("endsWith"))
            dsl += $"from A on Go when {expr} -> transition B\n";
        else
            dsl += $"from A on Go -> set Output = {expr} -> transition B\n";
        var model = PreceptParser.Parse(dsl);
        model.Should().NotBeNull();
    }

    // ─── Integration tests ──────────────────────────────────────────

    [Fact]
    public void Integration_StringFunctions_ToLowerInGuard()
    {
        var fire = FireForGuard(
            "field Email as string default \"\"",
            "toLower(Email) == \"test@example.com\"",
            new Dictionary<string, object?> { ["Email"] = "TEST@EXAMPLE.COM" });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void Integration_NumericFunctions_AbsInGuard()
    {
        var dsl = $$"""
            precept Calc
            field Amount as number default 0
            field AbsAmount as number default 0
            state Active initial
            state Done
            event Process
            from Active on Process when abs(Amount) > 10 -> set AbsAmount = abs(Amount) -> transition Done
            from Active on Process -> no transition
            """;
        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active",
            new Dictionary<string, object?> { ["Amount"] = -15.0, ["AbsAmount"] = 0.0 });
        var result = workflow.Fire(instance, "Process");
        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["AbsAmount"].Should().Be(15.0);
    }

    [Fact]
    public void Integration_ClampInSetExpression()
    {
        var fire = FireForSet(
            "field Score as number default 0\nfield BoundedScore as number default 0",
            "BoundedScore", "clamp(Score, 0.0, 100.0)",
            new Dictionary<string, object?> { ["Score"] = 150.0, ["BoundedScore"] = 0.0 });

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.UpdatedInstance!.InstanceData["BoundedScore"].Should().Be(100.0);
    }

    // ─── Helpers ────────────────────────────────────────────────────

    private static string BuildDslForSet(string declarations, string targetField, string expression)
    {
        return $$"""
            precept Calc
            {{declarations}}
            state A initial
            state B
            event Go
            from A on Go -> set {{targetField}} = {{expression}} -> transition B
            """;
    }

    private static FireResult FireForSet(
        string declarations,
        string targetField,
        string expression,
        IReadOnlyDictionary<string, object?> instanceData)
    {
        var dsl = BuildDslForSet(declarations, targetField, expression);
        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", instanceData);
        return workflow.Fire(instance, "Go");
    }

    private static FireResult FireForGuard(
        string declarations,
        string guardExpression,
        IReadOnlyDictionary<string, object?> instanceData)
    {
        var dsl = $$"""
            precept Calc
            {{declarations}}
            state A initial
            state B
            event Go
            from A on Go when {{guardExpression}} -> transition B
            from A on Go -> no transition
            """;
        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", instanceData);
        return workflow.Fire(instance, "Go");
    }
}
