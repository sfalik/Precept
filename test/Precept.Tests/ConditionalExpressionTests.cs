using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Tests.Infrastructure;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for conditional expressions: if &lt;condition&gt; then &lt;expr&gt; else &lt;expr&gt;.
/// Covers parser, type checker, and runtime behavior.
/// </summary>
public class ConditionalExpressionTests
{
    // ════════════════════════════════════════════════════════════════════
    // Parser Tests
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_SimpleConditional_StringBranches()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression(
            "if true then \"yes\" else \"no\"");

        expression.Should().BeOfType<PreceptConditionalExpression>();
        var cond = (PreceptConditionalExpression)expression;
        cond.Condition.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)cond.Condition).Value.Should().Be(true);
        cond.ThenBranch.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)cond.ThenBranch).Value.Should().Be("yes");
        cond.ElseBranch.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)cond.ElseBranch).Value.Should().Be("no");
    }

    [Fact]
    public void Parse_ConditionalWithFieldRefAndComparison()
    {
        // Uses a string field so the comparison and identifier work
        var dsl = """
            precept Parser
            field Status as string nullable
            event Advance
            state Red initial
            state Green
            from Red on Advance -> set Status = if Status != null then Status else "default" -> transition Green
            """;

        var model = PreceptParser.Parse(dsl);
        model.TransitionRows.Should().ContainSingle();
        model.TransitionRows![0].SetAssignments.Should().ContainSingle();

        var expr = model.TransitionRows[0].SetAssignments[0].Expression;
        expr.Should().BeOfType<PreceptConditionalExpression>();
        var cond = (PreceptConditionalExpression)expr;
        cond.Condition.Should().BeOfType<PreceptBinaryExpression>();
        ((PreceptBinaryExpression)cond.Condition).Operator.Should().Be("!=");
        cond.ThenBranch.Should().BeOfType<PreceptIdentifierExpression>();
        cond.ElseBranch.Should().BeOfType<PreceptLiteralExpression>();
    }

    [Fact]
    public void Parse_NestedConditionalViaParens()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression(
            "if true then (if false then 1 else 2) else 3");

        expression.Should().BeOfType<PreceptConditionalExpression>();
        var outer = (PreceptConditionalExpression)expression;
        outer.ThenBranch.Should().BeOfType<PreceptParenthesizedExpression>();
        var inner = ((PreceptParenthesizedExpression)outer.ThenBranch).Inner;
        inner.Should().BeOfType<PreceptConditionalExpression>();
        var nested = (PreceptConditionalExpression)inner;
        nested.Condition.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)nested.Condition).Value.Should().Be(false);
    }

    [Fact]
    public void Parse_ConditionalInSetRHS_WithinFullPrecept()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            field Label as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Label = if Active then "on" else "off" -> transition B
            """;

        var model = PreceptParser.Parse(dsl);
        model.TransitionRows.Should().ContainSingle();
        var setExpr = model.TransitionRows![0].SetAssignments[0].Expression;
        setExpr.Should().BeOfType<PreceptConditionalExpression>();
    }

    [Fact]
    public void Parse_ConditionalInInvariant()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            field Score as number default 10
            invariant (if Active then Score > 0 else true) because "active needs score"
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
        diags.Should().BeEmpty();
        model.Should().NotBeNull();
        model!.Invariants.Should().ContainSingle();
    }

    [Fact]
    public void Parse_ConditionalInWhenGuard()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            field Score as number default 10
            state A initial
            state B
            event Go
            from A on Go when (if Active then Score > 5 else true) -> transition B
            from A on Go -> reject "blocked"
            """;

        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
        diags.Should().BeEmpty();
        model.Should().NotBeNull();
    }

    [Fact]
    public void Parse_ConditionalWithArithmeticBranches()
    {
        var dsl = """
            precept Parser
            field Flag as boolean default true
            field X as number default 5
            event Advance
            state Red initial
            state Green
            from Red on Advance -> set X = if Flag then X + 1 else X - 1 -> transition Green
            """;

        var model = PreceptParser.Parse(dsl);
        var expr = model.TransitionRows![0].SetAssignments[0].Expression;
        expr.Should().BeOfType<PreceptConditionalExpression>();
        var cond = (PreceptConditionalExpression)expr;
        cond.ThenBranch.Should().BeOfType<PreceptBinaryExpression>();
        ((PreceptBinaryExpression)cond.ThenBranch).Operator.Should().Be("+");
        cond.ElseBranch.Should().BeOfType<PreceptBinaryExpression>();
        ((PreceptBinaryExpression)cond.ElseBranch).Operator.Should().Be("-");
    }

    [Fact]
    public void Parse_ConditionalWithBooleanBranches()
    {
        var expression = PreceptExpressionTestHelper.ParseFirstSetExpression(
            "if true then true else false");

        expression.Should().BeOfType<PreceptConditionalExpression>();
        var cond = (PreceptConditionalExpression)expression;
        cond.ThenBranch.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)cond.ThenBranch).Value.Should().Be(true);
        cond.ElseBranch.Should().BeOfType<PreceptLiteralExpression>();
        ((PreceptLiteralExpression)cond.ElseBranch).Value.Should().Be(false);
    }

    // ════════════════════════════════════════════════════════════════════
    // Type Checker Tests
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypeCheck_ValidConditional_BooleanCondition_MatchingStringBranches_NoDiagnostics()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            field Label as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Label = if Active then "on" else "off" -> transition B
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_C78_NonBooleanCondition_Number()
    {
        const string dsl = """
            precept Test
            field Label as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Label = if 42 then "a" else "b" -> no transition
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C78");
    }

    [Fact]
    public void TypeCheck_C78_NonBooleanCondition_String()
    {
        const string dsl = """
            precept Test
            field Label as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Label = if "text" then "a" else "b" -> no transition
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C78");
    }

    [Fact]
    public void TypeCheck_C78_NullableBooleanCondition()
    {
        const string dsl = """
            precept Test
            field Flag as boolean nullable
            field Label as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Label = if Flag then "a" else "b" -> no transition
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C78");
    }

    [Fact]
    public void TypeCheck_C79_BranchTypeMismatch_StringVsNumber()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set X = if true then 42 else "text" -> no transition
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C79");
    }

    [Fact]
    public void TypeCheck_C79_BranchTypeMismatch_BooleanVsString()
    {
        const string dsl = """
            precept Test
            field Label as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Label = if true then true else "text" -> no transition
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C79");
    }

    [Fact]
    public void TypeCheck_NullNarrowing_ThenBranchSeesNonNullableField()
    {
        const string dsl = """
            precept Test
            field Name as string nullable
            field Display as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Display = if Name != null then Name else "anonymous" -> transition B
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        // No C42 null-flow violation — Name is narrowed to non-nullable in then-branch
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_NestedConditional_TypeConsistency()
    {
        const string dsl = """
            precept Test
            field A as boolean default true
            field B as boolean default false
            field X as number default 0
            state S initial
            state T
            event Go
            from S on Go -> set X = if A then (if B then 1 else 2) else 3 -> transition T
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_ConditionalWithFunctionCallInBranch()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default true
            field X as number default 5
            field Y as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set Y = if Flag then abs(X) else 0.0 -> transition B
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_IntegerWidensToNumber_InConditionalBranches()
    {
        // abs() returns number, 0 is integer — widening should accept this
        const string dsl = """
            precept Test
            field Flag as boolean default true
            field X as number default 5
            field Y as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set Y = if Flag then abs(X) else 0 -> transition B
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_IntegerWidensToDecimal_InConditionalBranches()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default true
            field Price as decimal default 0
            state A initial
            state B
            event Go
            from A on Go -> set Price = if Flag then Price else 0 -> transition B
            """;

        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TypeCheck_NumberVsDecimal_StillC79Error()
    {
        // number ↔ decimal is NOT lossless — should remain a hard error
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as decimal default 0
            field Flag as boolean default true
            field Result as number default 0
            state A initial
            event Go
            from A on Go -> set Result = if Flag then X else Y -> no transition
            """;


        var result = PreceptTypeChecker.Check(PreceptParser.Parse(dsl));
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C79");
    }

    // ════════════════════════════════════════════════════════════════════
    // Runtime Tests (end-to-end with PreceptEngine)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Fire_SetWithConditional_CorrectDataInInstance()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            field Label as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Label = if Active then "on" else "off" -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Active"] = true, ["Label"] = "" });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Label"].Should().Be("on");
    }

    [Fact]
    public void Fire_Conditional_EvaluatesTrueBranch()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default true
            field Result as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Result = if Flag then "true-path" else "false-path" -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Flag"] = true, ["Result"] = "" });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Result"].Should().Be("true-path");
    }

    [Fact]
    public void Fire_Conditional_EvaluatesFalseBranch()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default true
            field Result as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Result = if Flag then "true-path" else "false-path" -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Flag"] = false, ["Result"] = "" });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Result"].Should().Be("false-path");
    }

    [Fact]
    public void Fire_Conditional_WithComparisonAsCondition()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            field Grade as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Grade = if Score >= 50 then "pass" else "fail" -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Score"] = 75.0, ["Grade"] = "" });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Grade"].Should().Be("pass");
    }

    [Fact]
    public void Fire_NestedConditional_CorrectValue()
    {
        const string dsl = """
            precept Test
            field A as boolean default true
            field B as boolean default false
            field X as number default 0
            state S initial
            state T
            event Go
            from S on Go -> set X = if A then (if B then 10 else 20) else 30 -> transition T
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("S", new Dictionary<string, object?>
        {
            ["A"] = true, ["B"] = false, ["X"] = 0.0
        });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["X"].Should().Be(20.0);
    }

    [Fact]
    public void Fire_InvariantWithConditional_ValidatesCorrectly()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            field Score as number default 10
            invariant (if Active then Score > 0 else true) because "active needs score"
            state A initial
            state B
            event Go with NewScore as number
            from A on Go -> set Score = Go.NewScore -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        // Active = true, Score set to 0 should violate
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Active"] = true, ["Score"] = 10.0 });
        var result = wf.Fire(inst, "Go", new Dictionary<string, object?> { ["NewScore"] = 0.0 });

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Violations.Should().ContainSingle().Which.Message.Should().Be("active needs score");
    }

    [Fact]
    public void Fire_ConditionalWithNullNarrowing_ProducesCorrectValue()
    {
        const string dsl = """
            precept Test
            field Name as string nullable
            field Display as string default ""
            state A initial
            state B
            event Go
            from A on Go -> set Display = if Name != null then Name else "anonymous" -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Name is non-null — should use Name
        var inst1 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Name"] = "Alice", ["Display"] = "" });
        var result1 = wf.Fire(inst1, "Go");
        result1.Outcome.Should().Be(TransitionOutcome.Transition);
        result1.UpdatedInstance!.InstanceData["Display"].Should().Be("Alice");

        // Name is null — should use "anonymous"
        var inst2 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Name"] = null, ["Display"] = "" });
        var result2 = wf.Fire(inst2, "Go");
        result2.Outcome.Should().Be(TransitionOutcome.Transition);
        result2.UpdatedInstance!.InstanceData["Display"].Should().Be("anonymous");
    }

    [Fact]
    public void Fire_Conditional_WithArithmeticBranches()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default true
            field X as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set X = if Flag then X + 10 else X - 5 -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Flag = true, X = 100 → 100 + 10 = 110
        var inst1 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Flag"] = true, ["X"] = 100.0 });
        var result1 = wf.Fire(inst1, "Go");
        result1.Outcome.Should().Be(TransitionOutcome.Transition);
        result1.UpdatedInstance!.InstanceData["X"].Should().Be(110.0);

        // Flag = false, X = 100 → 100 - 5 = 95
        var inst2 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Flag"] = false, ["X"] = 100.0 });
        var result2 = wf.Fire(inst2, "Go");
        result2.Outcome.Should().Be(TransitionOutcome.Transition);
        result2.UpdatedInstance!.InstanceData["X"].Should().Be(95.0);
    }

    [Fact]
    public void Fire_Conditional_InWhenGuard_GatesTransition()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            field Score as number default 0
            state A initial
            state B
            event Go
            from A on Go when (if Active then Score > 50 else true) -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Active = true, Score = 60 → guard is (Score > 50) = true → transitions
        var inst1 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Active"] = true, ["Score"] = 60.0 });
        var result1 = wf.Fire(inst1, "Go");
        result1.Outcome.Should().Be(TransitionOutcome.Transition);

        // Active = true, Score = 30 → guard is (Score > 50) = false → no matching row
        var inst2 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Active"] = true, ["Score"] = 30.0 });
        var result2 = wf.Fire(inst2, "Go");
        result2.Outcome.Should().Be(TransitionOutcome.Unmatched);
    }

    [Fact]
    public void Fire_Conditional_IntegerWidensToNumber_CorrectRuntimeValue()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default true
            field Value as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set Value = if Flag then 42 else 0 -> transition B
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = wf.CreateInstance("A", new Dictionary<string, object?> { ["Flag"] = true, ["Value"] = 0.0 });
        var result = wf.Fire(inst, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Transition);
        result.UpdatedInstance!.InstanceData["Value"].Should().Be(42L);
    }

    // ════════════════════════════════════════════════════════════════════
    // Statement-Level `if` Misuse Detection Tests
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_StatementLevelIf_ProducesRedirectToWhen()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default true
            field X as number default 0
            state A initial
            state B
            event Go
            if Flag -> set X = 1 -> transition B
            """;

        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
        model.Should().BeNull();
        diags.Should().ContainSingle();
        diags[0].Message.Should().Contain("if");
        diags[0].Message.Should().Contain("value expression");
        diags[0].Message.Should().Contain("when");
    }

    [Fact]
    public void Parse_StatementLevelIf_ErrorMessageIsActionable()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            state A initial
            event Go
            if Active -> transition A
            """;

        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
        model.Should().BeNull();
        diags.Should().ContainSingle();
        diags[0].Message.Should().Be(
            "'if' is a value expression, not a statement. To conditionally apply a transition row, use 'when' as a guard: from <State> on <Event> when <Condition> -> ...");
    }
}
