using System;
using System.Collections.Generic;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for Issue #31: keyword logical operators — and/or/not replacing &&/||/!
/// Covers parsing, precedence, evaluation, null narrowing, and rejection of old symbols.
/// </summary>
public class PreceptKeywordLogicalOperatorTests
{
    // ────────────────────────────────────────────────────────────────────
    // 1. Basic keyword parsing
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Guard_Not_SingleField_ParsesCorrectly()
    {
        const string dsl = """
            precept Test
            field IsPremium as boolean default false
            state A initial
            state B
            event Go
            from A on Go when not IsPremium -> transition B
            from A on Go -> reject "blocked"
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptUnaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("not");
        guard.Operand.Should().BeOfType<PreceptIdentifierExpression>()
            .Which.Name.Should().Be("IsPremium");
    }

    [Fact]
    public void Parse_Guard_AndWithNot_ParsesCorrectly()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            field IsPremium as boolean default false
            state A initial
            state B
            event Go
            from A on Go when Score >= 680 and not IsPremium -> transition B
            from A on Go -> reject "blocked"
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptBinaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("and");
        guard.Right.Should().BeOfType<PreceptUnaryExpression>()
            .Which.Operator.Should().Be("not");
    }

    [Fact]
    public void Parse_Guard_Or_TwoFields_ParsesCorrectly()
    {
        const string dsl = """
            precept Test
            field Amount as number default 0
            field IsExempt as boolean default false
            state A initial
            state B
            event Go
            from A on Go when Amount > 0 or IsExempt -> transition B
            from A on Go -> reject "blocked"
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptBinaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("or");
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. Precedence: not > and > or
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Guard_Precedence_NotBindsTighterThanAnd()
    {
        // "not IsPremium and Score >= 680" should parse as "(not IsPremium) and (Score >= 680)"
        const string dsl = """
            precept Test
            field IsPremium as boolean default false
            field Score as number default 0
            state A initial
            state B
            event Go
            from A on Go when not IsPremium and Score >= 680 -> transition B
            from A on Go -> reject "blocked"
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptBinaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("and");
        guard.Left.Should().BeOfType<PreceptUnaryExpression>()
            .Which.Operator.Should().Be("not");
    }

    [Fact]
    public void Parse_Guard_Precedence_AndBindsTighterThanOr()
    {
        // "not IsPremium and Score >= 680 or HasOverride" should parse as
        // "((not IsPremium) and (Score >= 680)) or HasOverride"
        const string dsl = """
            precept Test
            field IsPremium as boolean default false
            field Score as number default 0
            field HasOverride as boolean default false
            state A initial
            state B
            event Go
            from A on Go when not IsPremium and Score >= 680 or HasOverride -> transition B
            from A on Go -> reject "blocked"
            """;

        var model = PreceptParser.Parse(dsl);

        // Root is "or"
        var root = model.TransitionRows![0].WhenGuard as PreceptBinaryExpression;
        root.Should().NotBeNull();
        root!.Operator.Should().Be("or");

        // Left side is "and"
        var andNode = root.Left as PreceptBinaryExpression;
        andNode.Should().NotBeNull();
        andNode!.Operator.Should().Be("and");

        // Left of "and" is "not"
        andNode.Left.Should().BeOfType<PreceptUnaryExpression>()
            .Which.Operator.Should().Be("not");

        // Right of "or" is HasOverride identifier
        root.Right.Should().BeOfType<PreceptIdentifierExpression>()
            .Which.Name.Should().Be("HasOverride");
    }

    [Fact]
    public void Evaluate_Guard_Precedence_NotAndOr_CorrectEvaluationOrder()
    {
        // "not IsPremium and Score >= 680 or HasOverride"
        // With IsPremium=true, Score=720, HasOverride=false:
        //   not true = false; false and (720>=680=true) = false; false or false = false → no transition
        // With HasOverride=true: false or true = true → transition
        const string dsl = """
            precept Test
            field IsPremium as boolean default false
            field Score as number default 0
            field HasOverride as boolean default false
            state A initial
            state B
            event Go
            from A on Go when not IsPremium and Score >= 680 or HasOverride -> transition B
            from A on Go -> reject "blocked"
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // IsPremium=true, Score=720, HasOverride=false → no transition (not true and true or false = false)
        var inst1 = wf.CreateInstance("A", new Dictionary<string, object?>
            { ["IsPremium"] = true, ["Score"] = 720d, ["HasOverride"] = false });
        wf.Fire(inst1, "Go").Outcome.Should().Be(TransitionOutcome.Rejected);

        // IsPremium=false, Score=720, HasOverride=false → transition (not false and true or false = true)
        var inst2 = wf.CreateInstance("A", new Dictionary<string, object?>
            { ["IsPremium"] = false, ["Score"] = 720d, ["HasOverride"] = false });
        wf.Fire(inst2, "Go").Outcome.Should().Be(TransitionOutcome.Transition);

        // IsPremium=true, Score=720, HasOverride=true → transition (false or true = true)
        var inst3 = wf.CreateInstance("A", new Dictionary<string, object?>
            { ["IsPremium"] = true, ["Score"] = 720d, ["HasOverride"] = true });
        wf.Fire(inst3, "Go").Outcome.Should().Be(TransitionOutcome.Transition);
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. Null narrowing through "not"
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Diagnostics_Guard_NotNullCheck_And_NumericComparison_NarrowsCorrectly()
    {
        // "not (Field == null) and Field > 0" — the "not (null check)" should narrow Field
        // so the RHS sees a non-nullable number. No type errors expected.
        const string dsl = """
            precept Test
            field Amount as number nullable
            state A initial
            state B
            event Go
            from A on Go when not (Amount == null) and Amount > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        // compile must succeed — narrowing through "not" must work
        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().NotThrow();
    }

    [Fact]
    public void Evaluate_Guard_NotNullCheck_And_NumericComparison_BehavesCorrectly()
    {
        const string dsl = """
            precept Test
            field Amount as number nullable
            state A initial
            state B
            event Go
            from A on Go when not (Amount == null) and Amount > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // null → no transition
        var inst1 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Amount"] = null });
        wf.Fire(inst1, "Go").Outcome.Should().Be(TransitionOutcome.Rejected);

        // positive value → transition
        var inst2 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Amount"] = 100d });
        wf.Fire(inst2, "Go").Outcome.Should().Be(TransitionOutcome.Transition);

        // zero → no transition
        var inst3 = wf.CreateInstance("A", new Dictionary<string, object?> { ["Amount"] = 0d });
        wf.Fire(inst3, "Go").Outcome.Should().Be(TransitionOutcome.Rejected);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. != operator is unaffected
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Guard_NotEqual_NumberField_ParsesWithoutError()
    {
        const string dsl = """
            precept Test
            field Score as number default 0
            state A initial
            state B
            event Go
            from A on Go when Score != 0 -> transition B
            from A on Go -> reject "score must be nonzero"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_Guard_NotEqual_StringField_ParsesWithoutError()
    {
        const string dsl = """
            precept Test
            field Status as string default "Inactive"
            state A initial
            state B
            event Go
            from A on Go when Status != "Active" -> transition B
            from A on Go -> reject "blocked"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().NotThrow();
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. Old symbols produce parse errors
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Guard_OldSymbol_And_ProducesParseError()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default false
            field OtherFlag as boolean default false
            state A initial
            state B
            event Go
            from A on Go when Flag && OtherFlag -> transition B
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_Guard_OldSymbol_Or_ProducesParseError()
    {
        const string dsl = """
            precept Test
            field Flag as boolean default false
            field OtherFlag as boolean default false
            state A initial
            state B
            event Go
            from A on Go when Flag || OtherFlag -> transition B
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_Guard_OldSymbol_BareUnaryNot_ProducesParseError()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            state A initial
            state B
            event Go
            from A on Go when !Active -> transition B
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>();
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. Compound expression
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Guard_CompoundExpression_ParsesCorrectly()
    {
        // "not (Status == "Active") and (Score >= 680 or HasOverride)"
        const string dsl = """
            precept Test
            field Status as string default "Pending"
            field Score as number default 0
            field HasOverride as boolean default false
            state A initial
            state B
            event Go
            from A on Go when not (Status == "Active") and (Score >= 680 or HasOverride) -> transition B
            from A on Go -> reject "blocked"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        act.Should().NotThrow();
    }

    [Fact]
    public void Evaluate_Guard_CompoundExpression_BehavesCorrectly()
    {
        const string dsl = """
            precept Test
            field Status as string default "Pending"
            field Score as number default 0
            field HasOverride as boolean default false
            state A initial
            state B
            event Go
            from A on Go when not (Status == "Active") and (Score >= 680 or HasOverride) -> transition B
            from A on Go -> reject "blocked"
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Status="Active" → blocked by "not"
        var inst1 = wf.CreateInstance("A", new Dictionary<string, object?>
            { ["Status"] = "Active", ["Score"] = 720d, ["HasOverride"] = false });
        wf.Fire(inst1, "Go").Outcome.Should().Be(TransitionOutcome.Rejected);

        // Status="Pending", Score=720, HasOverride=false → transition
        var inst2 = wf.CreateInstance("A", new Dictionary<string, object?>
            { ["Status"] = "Pending", ["Score"] = 720d, ["HasOverride"] = false });
        wf.Fire(inst2, "Go").Outcome.Should().Be(TransitionOutcome.Transition);

        // Status="Pending", Score=600, HasOverride=false → blocked (600 < 680, no override)
        var inst3 = wf.CreateInstance("A", new Dictionary<string, object?>
            { ["Status"] = "Pending", ["Score"] = 600d, ["HasOverride"] = false });
        wf.Fire(inst3, "Go").Outcome.Should().Be(TransitionOutcome.Rejected);

        // Status="Pending", Score=600, HasOverride=true → transition (override rescues)
        var inst4 = wf.CreateInstance("A", new Dictionary<string, object?>
            { ["Status"] = "Pending", ["Score"] = 600d, ["HasOverride"] = true });
        wf.Fire(inst4, "Go").Outcome.Should().Be(TransitionOutcome.Transition);
    }

    // ────────────────────────────────────────────────────────────────────
    // 7. Rule context (not just guards)
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Rule_Or_ParsesAndEvaluatesCorrectly()
    {
        const string dsl = """
            precept Test
            field Amount as number default 0
            field IsExempt as boolean default true
            rule Amount > 0 or IsExempt because "Amount must be positive or entity is exempt"
            state Active initial
            in Active edit Amount, IsExempt
            event Update
            from Active on Update -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        // Amount=0, IsExempt=false → constraint fails
        var inst1 = wf.CreateInstance("Active", new Dictionary<string, object?>
            { ["Amount"] = 0d, ["IsExempt"] = false });
        var result1 = wf.Update(inst1, p => p.Set("Amount", 0d));
        result1.IsSuccess.Should().BeFalse();
        result1.Violations.Should().ContainSingle()
            .Which.Message.Should().Contain("Amount must be positive or entity is exempt");

        // Amount=0, IsExempt=true → passes
        var inst2 = wf.CreateInstance("Active", new Dictionary<string, object?>
            { ["Amount"] = 0d, ["IsExempt"] = true });
        var result2 = wf.Update(inst2, p => p.Set("Amount", 0d).Set("IsExempt", true));
        result2.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Rule_And_ParsesAndEvaluatesCorrectly()
    {
        const string dsl = """
            precept Test
            field Min as number default 1
            field Max as number default 10
            rule Min >= 1 and Max <= 100 because "Min must be at least 1 and Max at most 100"
            state Active initial
            in Active edit Min, Max
            event Go
            from Active on Go -> transition Active
            """;

        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        var inst = wf.CreateInstance("Active", new Dictionary<string, object?>
            { ["Min"] = 1d, ["Max"] = 10d });

        // violate Max
        var bad = wf.Update(inst, p => p.Set("Max", 200d));
        bad.IsSuccess.Should().BeFalse();
        bad.Violations.Should().ContainSingle()
            .Which.Message.Should().Contain("Min must be at least 1 and Max at most 100");

        // valid values
        var good = wf.Update(inst, p => p.Set("Min", 5d).Set("Max", 50d));
        good.IsSuccess.Should().BeTrue();
    }
}
