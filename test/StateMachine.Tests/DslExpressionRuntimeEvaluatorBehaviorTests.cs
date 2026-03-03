using System;
using System.Collections.Generic;
using FluentAssertions;
using StateMachine.Dsl;
using Xunit;

namespace StateMachine.Tests;

public class DslExpressionRuntimeEvaluatorBehaviorTests
{
    [Theory]
    [InlineData("+", 6d)]
    [InlineData("-", 2d)]
    [InlineData("*", 8d)]
    [InlineData("/", 2d)]
    [InlineData("%", 0d)]
    public void Fire_Transform_NumericOperators_AreApplied(string op, double expected)
    {
        var fire = FireForTransform(
            "number Input\nnumber Output",
            "Output",
            $"Input {op} 2",
            new Dictionary<string, object?>
            {
                ["Input"] = 4d,
                ["Output"] = 0d
            });

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(expected);
    }

    [Theory]
    [InlineData(">", false)]
    [InlineData(">=", true)]
    [InlineData("<", false)]
    [InlineData("<=", true)]
    [InlineData("==", true)]
    [InlineData("!=", false)]
    public void Fire_Transform_ComparisonOperators_AreApplied(string op, bool expected)
    {
        var fire = FireForTransform(
            "number Input\nboolean IsMatch",
            "IsMatch",
            $"Input {op} 4",
            new Dictionary<string, object?>
            {
                ["Input"] = 4d,
                ["IsMatch"] = false
            });

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["IsMatch"].Should().Be(expected);
    }

    [Fact]
    public void Fire_Transform_StringConcat_IsApplied()
    {
        var fire = FireForTransform(
            "string Prefix\nstring Message",
            "Message",
            "Prefix + Go.EventText",
            new Dictionary<string, object?>
            {
                ["Prefix"] = "Reason: ",
                ["Message"] = ""
            },
            new Dictionary<string, object?>
            {
                ["EventText"] = "Accident"
            });

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Message"].Should().Be("Reason: Accident");
    }

    [Fact]
    public void Fire_Transform_MixedStringAndNumberConcat_IsRejected()
    {
        var fire = FireForTransform(
            "string Prefix\nnumber Count\nstring Message",
            "Message",
            "Prefix + Count",
            new Dictionary<string, object?>
            {
                ["Prefix"] = "Count: ",
                ["Count"] = 2d,
                ["Message"] = ""
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '+' requires number+number or string+string", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Transform_UnaryNot_IsApplied()
    {
        var fire = FireForTransform(
            "boolean IsEnabled\nboolean IsDisabled",
            "IsDisabled",
            "!IsEnabled",
            new Dictionary<string, object?>
            {
                ["IsEnabled"] = true,
                ["IsDisabled"] = false
            });

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["IsDisabled"].Should().Be(false);
    }

    [Fact]
    public void Fire_Transform_UnaryMinus_IsApplied()
    {
        var fire = FireForTransform(
            "number Input\nnumber Negated",
            "Negated",
            "-Input",
            new Dictionary<string, object?>
            {
                ["Input"] = 3d,
                ["Negated"] = 0d
            });

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Negated"].Should().Be(-3d);
    }

    [Fact]
    public void Fire_Transform_BooleanAndWithNonBooleanOperand_IsRejected()
    {
        var fire = FireForTransform(
            "boolean Flag\nnumber Count\nboolean Result",
            "Result",
            "Flag && Count",
            new Dictionary<string, object?>
            {
                ["Flag"] = true,
                ["Count"] = 1d,
                ["Result"] = false
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '&&' requires boolean operands", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Transform_UnknownIdentifier_IsRejected()
    {
        var fire = FireForTransform(
            "number Output",
            "Output",
            "Missing + 1",
            new Dictionary<string, object?>
            {
                ["Output"] = 0d
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("data key 'Missing' was not provided", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_Guard_LogicalAnd_ShortCircuits_WhenLeftFalse()
    {
        var inspect = InspectForGuard(
            "Flag && MissingFlag",
            new Dictionary<string, object?>
            {
                ["Flag"] = false
            });

        inspect.IsAccepted.Should().BeFalse();
        inspect.Reasons.Should().ContainSingle("blocked");
    }

    [Fact]
    public void Inspect_Guard_LogicalOr_ShortCircuits_WhenLeftTrue()
    {
        var inspect = InspectForGuard(
            "Flag || MissingFlag",
            new Dictionary<string, object?>
            {
                ["Flag"] = true
            });

        inspect.IsAccepted.Should().BeTrue();
        inspect.TargetState.Should().Be("B");
    }

    [Fact]
    public void Inspect_Guard_LogicalAnd_EvaluatesRight_WhenLeftTrue()
    {
        var inspect = InspectForGuard(
            "Flag && OtherFlag",
            new Dictionary<string, object?>
            {
                ["Flag"] = true,
                ["OtherFlag"] = true
            });

        inspect.IsAccepted.Should().BeTrue();
        inspect.TargetState.Should().Be("B");
    }

    [Fact]
    public void Inspect_Guard_LogicalOr_EvaluatesRight_WhenLeftFalse()
    {
        var inspect = InspectForGuard(
            "Flag || OtherFlag",
            new Dictionary<string, object?>
            {
                ["Flag"] = false,
                ["OtherFlag"] = true
            });

        inspect.IsAccepted.Should().BeTrue();
        inspect.TargetState.Should().Be("B");
    }

    [Fact]
    public void Inspect_Guard_NonBooleanResult_IsRejectedWithConfiguredReason()
    {
        var inspect = InspectForGuard(
            "1 + 1",
            new Dictionary<string, object?>());

        inspect.IsAccepted.Should().BeFalse();
        inspect.Reasons.Should().ContainSingle("blocked");
    }

    [Fact]
    public void Inspect_Guard_MissingIdentifier_WhenEvaluated_IsRejectedWithConfiguredReason()
    {
        var inspect = InspectForGuard(
            "Flag && MissingFlag",
            new Dictionary<string, object?>
            {
                ["Flag"] = true
            });

        inspect.IsAccepted.Should().BeFalse();
        inspect.Reasons.Should().ContainSingle("blocked");
    }

    private static DslInstanceFireResult FireForTransform(
        string declarations,
        string targetField,
        string expression,
        IReadOnlyDictionary<string, object?> instanceData,
        IReadOnlyDictionary<string, object?>? eventArgs = null)
    {
        var dsl = $$"""
            machine Calc
            {{declarations}}
            state A
            state B
            event Go
              number? EventNum
              string? EventText
            from A on Go
              transform {{targetField}} = {{expression}}
              transition B
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", instanceData);
        return workflow.Fire(instance, "Go", eventArgs);
    }

    private static DslInspectionResult InspectForGuard(string guardExpression, IReadOnlyDictionary<string, object?> data)
    {
        var dsl = $$"""
            machine Guards
            boolean Flag
            boolean OtherFlag
            state A
            state B
            event Go
            from A on Go
              if {{guardExpression}}
                transition B
              else
                reject "blocked"
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        return workflow.Inspect("A", "Go", data);
    }
}
