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
    public void Fire_Set_NumericOperators_AreApplied(string op, double expected)
    {
        var fire = FireForSet(
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
    public void Fire_Set_ComparisonOperators_AreApplied(string op, bool expected)
    {
        var fire = FireForSet(
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
    public void Fire_Set_StringConcat_IsApplied()
    {
        var fire = FireForSet(
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
    public void Fire_Set_MixedStringAndNumberConcat_IsRejected()
    {
        var fire = FireForSet(
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
    public void Fire_Set_UnaryNot_IsApplied()
    {
        var fire = FireForSet(
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
    public void Fire_Set_UnaryMinus_IsApplied()
    {
        var fire = FireForSet(
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
    public void Fire_Set_ParenthesizedExpression_IsApplied()
    {
        var fire = FireForSet(
            "number Input\nnumber Output",
            "Output",
            "(Input + 2) * 3",
            new Dictionary<string, object?>
            {
                ["Input"] = 4d,
                ["Output"] = 0d
            });

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(18d);
    }

    [Fact]
    public void Fire_Set_BooleanAndWithNonBooleanOperand_IsRejected()
    {
        var fire = FireForSet(
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
    public void Fire_Set_BooleanAnd_WithNonBooleanLeftOperand_IsRejected()
    {
        var fire = FireForSet(
            "number Count\nboolean Flag\nboolean Result",
            "Result",
            "Count && Flag",
            new Dictionary<string, object?>
            {
                ["Count"] = 1d,
                ["Flag"] = true,
                ["Result"] = false
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '&&' requires boolean operands", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Set_UnaryOperandResolutionFailure_IsRejected()
    {
        var fire = FireForSet(
            "number Output",
            "Output",
            "-Missing",
            new Dictionary<string, object?>
            {
                ["Output"] = 0d
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("data key 'Missing' was not provided", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("-", "operator '-' requires numeric operands")]
    [InlineData("*", "operator '*' requires numeric operands")]
    [InlineData("/", "operator '/' requires numeric operands")]
    [InlineData("%", "operator '%' requires numeric operands")]
    [InlineData(">=", "operator '>=' requires numeric operands")]
    [InlineData("<", "operator '<' requires numeric operands")]
    [InlineData("<=", "operator '<=' requires numeric operands")]
    public void Fire_Set_NonNumericOperands_AreRejected(string op, string expectedMessage)
    {
        var targetField = op is ">=" or "<" or "<=" ? "ResultBool" : "ResultNum";
        var declarations = op is ">=" or "<" or "<="
            ? "string Input\nboolean ResultBool"
            : "string Input\nnumber ResultNum";

        var fire = FireForSet(
            declarations,
            targetField,
            $"Input {op} 1",
            op is ">=" or "<" or "<="
                ? new Dictionary<string, object?> { ["Input"] = "text", ["ResultBool"] = false }
                : new Dictionary<string, object?> { ["Input"] = "text", ["ResultNum"] = 0d });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData((byte)4, 6d)]
    [InlineData((sbyte)4, 6d)]
    [InlineData((short)4, 6d)]
    [InlineData((ushort)4, 6d)]
    [InlineData((uint)4, 6d)]
    [InlineData((long)4, 6d)]
    [InlineData((ulong)4, 6d)]
    [InlineData((float)4, 6d)]
    public void Fire_Set_NumericRuntimeTypes_AreAccepted(object input, double expected)
    {
        var fire = FireForSet(
            "number Input\nnumber Output",
            "Output",
            "Input + 2",
            new Dictionary<string, object?>
            {
                ["Input"] = input,
                ["Output"] = 0d
            });

        fire.IsAccepted.Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(expected);
    }

    [Fact]
    public void Fire_Set_UnknownIdentifier_IsRejected()
    {
        var fire = FireForSet(
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

    [Fact]
    public void Fire_Set_UnaryNot_WithNonBooleanOperand_IsRejected()
    {
        var fire = FireForSet(
            "number Input\nboolean Result",
            "Result",
            "!Input",
            new Dictionary<string, object?>
            {
                ["Input"] = 1d,
                ["Result"] = false
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '!' requires boolean operand", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Set_UnaryMinus_WithNonNumericOperand_IsRejected()
    {
        var fire = FireForSet(
            "string Input\nnumber Result",
            "Result",
            "-Input",
            new Dictionary<string, object?>
            {
                ["Input"] = "x",
                ["Result"] = 0d
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("unary '-' requires numeric operand", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Set_BooleanOr_WithNonBooleanLeftOperand_IsRejected()
    {
        var fire = FireForSet(
            "number Count\nboolean Flag\nboolean Result",
            "Result",
            "Count || Flag",
            new Dictionary<string, object?>
            {
                ["Count"] = 1d,
                ["Flag"] = true,
                ["Result"] = false
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '||' requires boolean operands", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Set_BooleanOr_WithNonBooleanRightOperand_IsRejected()
    {
        var fire = FireForSet(
            "boolean Flag\nnumber Count\nboolean Result",
            "Result",
            "Flag || Count",
            new Dictionary<string, object?>
            {
                ["Flag"] = false,
                ["Count"] = 1d,
                ["Result"] = false
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '||' requires boolean operands", StringComparison.Ordinal));
    }

    [Fact]
    public void Fire_Set_OrderedComparison_WithNonNumericOperand_IsRejected()
    {
        var fire = FireForSet(
            "string Input\nboolean Result",
            "Result",
            "Input > 0",
            new Dictionary<string, object?>
            {
                ["Input"] = "text",
                ["Result"] = false
            });

        fire.IsAccepted.Should().BeFalse();
        fire.Reasons.Should().ContainSingle(r => r.Contains("operator '>' requires numeric operands", StringComparison.Ordinal));
    }

    private static DslInstanceFireResult FireForSet(
        string declarations,
        string targetField,
        string expression,
        IReadOnlyDictionary<string, object?> instanceData,
        IReadOnlyDictionary<string, object?>? eventArgs = null)
    {
        var normalizedDeclarations = AddDefaultsForNonNullableFields(declarations);

        var dsl = $$"""
            machine Calc
            {{normalizedDeclarations}}
            state A initial
            state B
            event Go
              number? EventNum
              string? EventText
            from A on Go
              set {{targetField}} = {{expression}}
              transition B
            """;

        var workflow = DslWorkflowCompiler.Compile(StateMachineDslParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", instanceData);
        return workflow.Fire(instance, "Go", eventArgs);
    }

    private static string AddDefaultsForNonNullableFields(string declarations)
    {
        var lines = declarations.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var updated = new string[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Contains("=", StringComparison.Ordinal))
            {
                updated[i] = line;
                continue;
            }

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                updated[i] = line;
                continue;
            }

            var typeToken = parts[0];
            if (typeToken.EndsWith("?", StringComparison.Ordinal))
            {
                updated[i] = line;
                continue;
            }

            string? defaultLiteral = typeToken switch
            {
                "number" => "0",
                "boolean" => "false",
                "string" => "\"\"",
                _ => null
            };

            updated[i] = defaultLiteral is null
                ? line
                : $"{line} = {defaultLiteral}";
        }

        return string.Join(Environment.NewLine, updated);
    }

    private static DslInspectionResult InspectForGuard(string guardExpression, IReadOnlyDictionary<string, object?> data)
    {
        var dsl = $$"""
            machine Guards
            boolean Flag = false
            boolean OtherFlag = false
            state A initial
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
