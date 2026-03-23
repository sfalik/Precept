using System;
using System.Collections.Generic;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

public class PreceptExpressionRuntimeEvaluatorBehaviorTests
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

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
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

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["IsMatch"].Should().Be(expected);
    }

    [Fact]
    public void Fire_Set_StringConcat_IsApplied()
    {
        var fire = FireForSet(
            "string Prefix\nstring Suffix\nstring Message",
            "Message",
            "Prefix + Suffix",
            new Dictionary<string, object?>
            {
                ["Prefix"] = "Reason: ",
                ["Suffix"] = "Accident",
                ["Message"] = ""
            });

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Message"].Should().Be("Reason: Accident");
    }

    [Fact]
    public void Fire_Set_MixedStringAndNumberConcat_IsRejected()
    {
        var dsl = BuildDslForSet("string Prefix\nnumber Count\nstring Message", "Message", "Prefix + Count");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT041");
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

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
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

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
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

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(18d);
    }

    [Fact]
    public void Fire_Set_BooleanAndWithNonBooleanOperand_IsRejected()
    {
        var dsl = BuildDslForSet("boolean Flag\nnumber Count\nboolean Result", "Result", "Flag && Count");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT041");
    }

    [Fact]
    public void Fire_Set_BooleanAnd_WithNonBooleanLeftOperand_IsRejected()
    {
        var dsl = BuildDslForSet("number Count\nboolean Flag\nboolean Result", "Result", "Count && Flag");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT041");
    }

    [Fact]
    public void Fire_Set_UnaryOperandResolutionFailure_IsRejected()
    {
        var dsl = BuildDslForSet("number Output", "Output", "-Missing");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT038");
    }

    [Theory]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    [InlineData("%")]
    [InlineData(">=")]
    [InlineData("<")]
    [InlineData("<=")]
    public void Fire_Set_NonNumericOperands_AreRejected(string op)
    {
        var targetField = op is ">=" or "<" or "<=" ? "ResultBool" : "ResultNum";
        var declarations = op is ">=" or "<" or "<="
            ? "string Input\nboolean ResultBool"
            : "string Input\nnumber ResultNum";

        var dsl = BuildDslForSet(declarations, targetField, $"Input {op} 1");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT041");
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

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Output"].Should().Be(expected);
    }

    [Fact]
    public void Fire_Set_UnknownIdentifier_IsRejected()
    {
        var dsl = BuildDslForSet("number Output", "Output", "Missing + 1");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT038");
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

        (inspect.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
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

        (inspect.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
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

        (inspect.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
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

        (inspect.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        inspect.TargetState.Should().Be("B");
    }

    [Fact]
    public void Inspect_Guard_NonBooleanResult_IsRejectedWithConfiguredReason()
    {
        var dsl = BuildDslForGuard("1 + 1");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT039");
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

        (inspect.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeFalse();
        inspect.Reasons.Should().ContainSingle("blocked");
    }

    [Fact]
    public void Fire_Set_UnaryNot_WithNonBooleanOperand_IsRejected()
    {
        var dsl = BuildDslForSet("number Input\nboolean Result", "Result", "!Input");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT040");
    }

    [Fact]
    public void Fire_Set_UnaryMinus_WithNonNumericOperand_IsRejected()
    {
        var dsl = BuildDslForSet("string Input\nnumber Result", "Result", "-Input");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT040");
    }

    [Fact]
    public void Fire_Set_BooleanOr_WithNonBooleanLeftOperand_IsRejected()
    {
        var dsl = BuildDslForSet("number Count\nboolean Flag\nboolean Result", "Result", "Count || Flag");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT041");
    }

    [Fact]
    public void Fire_Set_BooleanOr_WithNonBooleanRightOperand_IsRejected()
    {
        var dsl = BuildDslForSet("boolean Flag\nnumber Count\nboolean Result", "Result", "Flag || Count");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT041");
    }

    [Fact]
    public void Fire_Set_OrderedComparison_WithNonNumericOperand_IsRejected()
    {
        var dsl = BuildDslForSet("string Input\nboolean Result", "Result", "Input > 0");
        var ex = Assert.Throws<InvalidOperationException>(() => PreceptCompiler.Compile(PreceptParser.Parse(dsl)));
        ex.Message.Should().Contain("PRECEPT041");
    }

    private static string BuildDslForSet(string declarations, string targetField, string expression)
    {
        var normalizedDeclarations = ConvertToNewSyntaxFields(declarations);

        return $$"""
            precept Calc
            {{normalizedDeclarations}}
            state A initial
            state B
            event Go with EventNum as number nullable, EventText as string nullable
            from A on Go -> set {{targetField}} = {{expression}} -> transition B
            """;
    }

    private static FireResult FireForSet(
        string declarations,
        string targetField,
        string expression,
        IReadOnlyDictionary<string, object?> instanceData,
        IReadOnlyDictionary<string, object?>? eventArgs = null)
    {
        var dsl = BuildDslForSet(declarations, targetField, expression);
        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", instanceData);
        return workflow.Fire(instance, "Go", eventArgs);
    }

    private static string ConvertToNewSyntaxFields(string declarations)
    {
        var lines = declarations.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var updated = new string[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                updated[i] = line;
                continue;
            }

            // Handle "type? Name" (nullable)
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                updated[i] = line;
                continue;
            }

            var typeToken = parts[0];
            var name = parts.Length >= 2 ? parts[1] : "";
            bool isNullable = typeToken.EndsWith("?", StringComparison.Ordinal);
            var baseType = isNullable ? typeToken[..^1] : typeToken;

            if (baseType is not ("number" or "string" or "boolean"))
            {
                updated[i] = line;
                continue;
            }

            // Check for explicit default: "type Name = value"
            if (parts.Length >= 4 && parts[2] == "=")
            {
                var defaultVal = string.Join(" ", parts[3..]);
                updated[i] = isNullable
                    ? $"field {name} as {baseType} nullable default {defaultVal}"
                    : $"field {name} as {baseType} default {defaultVal}";
            }
            else if (isNullable)
            {
                updated[i] = $"field {name} as {baseType} nullable";
            }
            else
            {
                // Add default for non-nullable
                string defaultLiteral = baseType switch
                {
                    "number" => "0",
                    "boolean" => "false",
                    "string" => "\"\"",
                    _ => "0"
                };
                updated[i] = $"field {name} as {baseType} default {defaultLiteral}";
            }
        }

        return string.Join(Environment.NewLine, updated);
    }

    private static string BuildDslForGuard(string guardExpression)
    {
        return $$"""
            precept Guards
            field Flag as boolean default false
            field OtherFlag as boolean default false
            field MissingFlag as boolean default false
            state A initial
            state B
            event Go
            from A on Go when {{guardExpression}} -> transition B
            from A on Go -> reject "blocked"
            """;
    }

    private static EventInspectionResult InspectForGuard(string guardExpression, IReadOnlyDictionary<string, object?> data)
    {
        var dsl = BuildDslForGuard(guardExpression);

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        return workflow.Inspect(workflow.CreateInstance("A", data), "Go");
    }
}
