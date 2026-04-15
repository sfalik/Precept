using System;
using System.Collections.Generic;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the string .length accessor (Issue #10).
/// Covers: parser, type checker (C56 for nullable-without-guard), runtime value semantics
/// (UTF-16 code unit contract), null guard compound evaluation, rule context,
/// event ensure context, and guard-based routing.
///
/// George's runtime implementation (parser extension, type-checker C56 constraint,
/// evaluator .length dispatch) must land for these tests to compile and pass.
/// </summary>
public class StringAccessorTests
{
    // ========================================================================================
    // PARSER TESTS
    // ========================================================================================

    [Fact]
    public void Parse_StringFieldLength_InGuard_ParsesWithoutError()
    {
        const string dsl = """
            precept M
            field Name as string default "x"
            state A initial
            state B
            event Go
            from A on Go when Name.length >= 2 -> transition B
            from A on Go -> reject "too short"
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_ThreeLevel_EventArgLength_AcceptsForm()
    {
        // Three-level dotted form: Submit.Name.length where Submit = event, Name = arg, .length = accessor.
        const string dsl = """
            precept M
            field Name as string default "x"
            state A initial
            state B
            event Submit with Name as string
            from A on Submit when Submit.Name.length >= 3 -> transition B
            from A on Submit -> reject "too short"
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    // NOTE: Sub-expression (.length on a parenthesized expression) is not applicable.
    // Precept does not support parenthesized sub-expressions as primary identifiers.
    // If this changes in a future language iteration, add:
    //   from A on Go when (Name).length >= 2 -> ...

    // ========================================================================================
    // TYPE CHECKER TESTS
    // ========================================================================================

    [Fact]
    public void Check_Length_OnNonNullableStringField_NoDiagnostic()
    {
        const string dsl = """
            precept M
            field Name as string default "x"
            state A initial
            state B
            event Go
            from A on Go when Name.length >= 2 -> transition B
            from A on Go -> reject "too short"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_ThreeLevel_EventArgLength_NonNullable_NoDiagnostic()
    {
        // Three-level dotted form on a non-nullable string arg: no C56 expected.
        const string dsl = """
            precept M
            field Name as string default "x"
            state A initial
            state B
            event Submit with Name as string
            from A on Submit when Submit.Name.length >= 2 -> transition B
            from A on Submit -> reject "too short"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_Length_OnNumberField_ProducesTypeError()
    {
        const string dsl = """
            precept M
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go when Count.length >= 2 -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        // .length is not valid on a number — type error expected.
        // TODO: pin the specific code (likely C38 or a new code) once George's implementation ships.
        result.HasErrors.Should().BeTrue();
        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void Check_Length_OnBooleanField_ProducesTypeError()
    {
        const string dsl = """
            precept M
            field Active as boolean default false
            state A initial
            state B
            event Go
            from A on Go when Active.length >= 1 -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        // .length is not valid on a boolean — type error expected.
        result.HasErrors.Should().BeTrue();
        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void Check_Length_OnCollectionField_ProducesTypeError_UseCountInstead()
    {
        // Collections expose .count, not .length. Using .length on a set is a type error.
        const string dsl = """
            precept M
            field Tags as set of string
            state A initial
            state B
            event Go
            from A on Go when Tags.length >= 1 -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.HasErrors.Should().BeTrue();
        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void Check_Length_OnNullableStringWithoutGuard_ProducesC56()
    {
        const string dsl = """
            precept M
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name.length >= 2 -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C56");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT056");
    }

    [Fact]
    public void Check_Length_OnNullableString_WithNotEqualNullAndGuard_NarrowsSuccessfully_NoC56()
    {
        // 'Name != null and Name.length >= 2': the != null check narrows Name to non-null
        // before .length is evaluated — no C56 should be emitted.
        const string dsl = """
            precept M
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name != null and Name.length >= 2 -> transition B
            from A on Go -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_Length_OnNullableString_WithEqualNullOrGuard_NarrowsSuccessfully_NoC56()
    {
        // 'Name == null or Name.length <= 500': when Name == null is false, Name is
        // narrowed to non-null before .length is evaluated — no C56 should be emitted.
        const string dsl = """
            precept M
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name == null or Name.length <= 500 -> transition B
            from A on Go -> reject "too long"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_Length_OnNullableStringField_InRuleScope_ProducesC56()
    {
        // Regression guard: C56 must fire in rule scope as well as guard scope.
        // If Name.length symbol is missing from rule symbols, TryInferKind emits C38
        // (unknown identifier) instead of C56 — this test distinguishes the two.
        const string dsl = """
            precept M
            field Name as string nullable
            state A initial
            rule Name.length > 0 because "Name required"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C56");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT056");
    }

    [Fact]
    public void Check_ThreeLevel_NullableEventArg_WithoutGuard_ProducesC56()
    {
        // Submit.Name.length on a nullable string arg without null guard must emit C56.
        const string dsl = """
            precept M
            field Name as string default "x"
            state A initial
            state B
            event Submit with Name as string nullable
            from A on Submit when Submit.Name.length >= 2 -> transition B
            from A on Submit -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Constraint.Id.Should().Be("C56");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT056");
    }

    [Fact]
    public void Check_ThreeLevel_NullableEventArg_WithNullGuard_NoC56()
    {
        // Submit.Name != null and Submit.Name.length >= 2: null guard on event arg narrows
        // before .length is evaluated — C56 must not be emitted.
        const string dsl = """
            precept M
            field Name as string default "x"
            state A initial
            state B
            event Submit with Name as string nullable
            from A on Submit when Submit.Name != null and Submit.Name.length >= 2 -> transition B
            from A on Submit -> reject "blocked"
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    // NOTE: Event args (Submit.Name) are not accessible in rule scope — rules operate
    // exclusively on fields. A three-level dotted form in a rule would produce a parse or
    // type error. No test added here; field-level rule C56 coverage is in
    // Check_Length_OnNullableStringField_InRuleScope_ProducesC56.

    // ========================================================================================
    // RUNTIME TESTS — string value semantics
    // ========================================================================================

    [Theory]
    [InlineData("hello", 5d)]
    [InlineData("", 0d)]
    [InlineData("a", 1d)]
    public void Fire_Set_StringLength_KnownInputs_ReturnsExpectedCodeUnitCount(string input, double expected)
    {
        const string dsl = """
            precept M
            field Name as string default "x"
            field Len as number default 0
            state Active initial
            event Measure
            from Active on Measure -> set Len = Name.length -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Name"] = input, ["Len"] = 0d });
        var fire = workflow.Fire(instance, "Measure");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Len"].Should().Be(expected);
    }

    [Fact]
    public void Fire_Set_EmojiStringLength_Returns2_ValidatesUtf16Semantics()
    {
        // UTF-16: emoji "💀" is a surrogate pair — 2 code units, NOT 1 grapheme cluster.
        // This test verifies the platform-consistent UTF-16 semantics decision for .length.
        const string dsl = """
            precept M
            field Name as string default "x"
            field Len as number default 0
            state Active initial
            event Measure
            from Active on Measure -> set Len = Name.length -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Name"] = "💀", ["Len"] = 0d });
        var fire = workflow.Fire(instance, "Measure");

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        // UTF-16: emoji is 2 code units, validates platform-consistent semantics
        fire.UpdatedInstance!.InstanceData["Len"].Should().Be(2.0);
    }

    // ========================================================================================
    // RUNTIME TESTS — null guard compound evaluation
    // ========================================================================================

    [Fact]
    public void Fire_NullAndGuard_NonNullName_TransitionsWhenLengthSatisfied()
    {
        const string dsl = """
            precept M
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name != null and Name.length > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?> { ["Name"] = "hello" });
        var fire = workflow.Fire(instance, "Go");

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.NewState.Should().Be("B");
    }

    [Fact]
    public void Fire_NullAndGuard_NullName_EvaluatesToFalse_NoNullError()
    {
        // null short-circuits the 'and' — .length is never evaluated, no null-deref error.
        const string dsl = """
            precept M
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name != null and Name.length > 0 -> transition B
            from A on Go -> reject "blocked"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?> { ["Name"] = null });
        var fire = workflow.Fire(instance, "Go");

        fire.Outcome.Should().Be(TransitionOutcome.Rejected);
        fire.Violations.Should().ContainSingle().Which.Message.Should().Contain("blocked");
    }

    [Fact]
    public void Fire_NullOrGuard_NonNullName_TransitionsWhenLengthSatisfied()
    {
        const string dsl = """
            precept M
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name == null or Name.length <= 500 -> transition B
            from A on Go -> reject "too long"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?> { ["Name"] = "hello" });
        var fire = workflow.Fire(instance, "Go");

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.NewState.Should().Be("B");
    }

    [Fact]
    public void Fire_NullOrGuard_NullName_EvaluatesToTrue_NoNullError()
    {
        // Name == null is true → 'or' short-circuits → guard is true, transition fires.
        // .length is never evaluated on the null value.
        const string dsl = """
            precept M
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name == null or Name.length <= 500 -> transition B
            from A on Go -> reject "too long"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A", new Dictionary<string, object?> { ["Name"] = null });
        var fire = workflow.Fire(instance, "Go");

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.NewState.Should().Be("B");
    }

    // ========================================================================================
    // RULE CONTEXT
    // ========================================================================================

    [Fact]
    public void Fire_Rule_LengthCheck_EmptyString_ProducesConstraintFailure()
    {
        // Default "x" satisfies length >= 1 at compile time. Setting Name = "" at runtime
        // violates the rule and produces ConstraintFailure.
        const string dsl = """
            precept M
            field Name as string default "x"
            rule Name.length >= 1 because "Name cannot be empty"
            state Active initial
            event Update with NewName as string
            from Active on Update -> set Name = Update.NewName -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Name"] = "x" });
        var fire = workflow.Fire(instance, "Update", new Dictionary<string, object?> { ["NewName"] = "" });

        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        fire.Violations.Should().ContainSingle().Which.Message.Should().Contain("Name cannot be empty");
    }

    [Fact]
    public void Fire_Rule_LengthCheck_NonEmptyString_Passes()
    {
        const string dsl = """
            precept M
            field Name as string default "x"
            rule Name.length >= 1 because "Name cannot be empty"
            state Active initial
            event Update with NewName as string
            from Active on Update -> set Name = Update.NewName -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Name"] = "x" });
        var fire = workflow.Fire(instance, "Update", new Dictionary<string, object?> { ["NewName"] = "Alice" });

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.UpdatedInstance!.InstanceData["Name"].Should().Be("Alice");
    }

    // ========================================================================================
    // EVENT ENSURE (ON SUBMIT) CONTEXT
    // ========================================================================================

    [Fact]
    public void Fire_EventEnsure_StringLength_TooShort_Rejects()
    {
        // Rule fires after set action changes Name to a too-short value.
        // On-ensure scope is limited to event-arg identifiers; field .length belongs in rule.
        // Three-level event-arg form deferred — see issue #10.
        const string dsl = """
            precept M
            field Name as string default "Alice"
            rule Name.length >= 2 because "Name too short"
            state A initial
            event Update with NewName as string
            from A on Update -> set Name = Update.NewName -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A");
        var fire = workflow.Fire(instance, "Update", new Dictionary<string, object?> { ["NewName"] = "X" });

        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        fire.Violations.Should().ContainSingle().Which.Message.Should().Contain("Name too short");
    }

    [Fact]
    public void Fire_EventEnsure_StringLength_LongEnough_Passes()
    {
        // Uses rule (not on-ensure) — on-ensure scope is limited to event-arg identifiers.
        // Three-level event-arg form deferred — see issue #10.
        const string dsl = """
            precept M
            field Name as string default "Alice"
            rule Name.length >= 2 because "Name too short"
            state A initial
            state B
            event Submit
            from A on Submit -> transition B
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A");
        var fire = workflow.Fire(instance, "Submit", null);

        (fire.Outcome is TransitionOutcome.Transition or TransitionOutcome.NoTransition).Should().BeTrue();
        fire.NewState.Should().Be("B");
    }

    // ========================================================================================
    // GUARD CONTEXT — routing based on length
    // ========================================================================================

    [Fact]
    public void Fire_Guard_StringLength_RoutesLongNoteToDetailedReview()
    {
        // Uses field Note. Three-level event-arg form (Submit.Note.length) deferred — see issue #10.
        const string dsl = """
            precept M
            field Note as string default ""
            state Draft initial
            state DetailedReview
            state StandardReview
            event Submit
            from Draft on Submit when Note.length > 100 -> transition DetailedReview
            from Draft on Submit -> transition StandardReview
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft");
        instance = instance with { InstanceData = new Dictionary<string, object?>(instance.InstanceData) { ["Note"] = new string('x', 101) } };

        var fire = workflow.Fire(instance, "Submit", null);

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.NewState.Should().Be("DetailedReview");
    }

    [Fact]
    public void Fire_Guard_StringLength_RoutesShortNoteToStandardReview()
    {
        // Uses field Note. Three-level event-arg form (Submit.Note.length) deferred — see issue #10.
        const string dsl = """
            precept M
            field Note as string default ""
            state Draft initial
            state DetailedReview
            state StandardReview
            event Submit
            from Draft on Submit when Note.length > 100 -> transition DetailedReview
            from Draft on Submit -> transition StandardReview
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Draft");
        instance = instance with { InstanceData = new Dictionary<string, object?>(instance.InstanceData) { ["Note"] = "Brief note" } };

        var fire = workflow.Fire(instance, "Submit", null);

        fire.Outcome.Should().Be(TransitionOutcome.Transition);
        fire.NewState.Should().Be("StandardReview");
    }

    [Theory]
    [InlineData("Bob", true)]   // length 3, satisfies Submit.Name.length >= 3 → transition
    [InlineData("Bo", false)]   // length 2, does not satisfy → fallback reject
    public void Fire_ThreeLevel_EventArgLength_GuardRouting(string name, bool shouldTransition)
    {
        // Three-level dotted form used as a guard condition routing on event-arg string length.
        const string dsl = """
            precept M
            field Name as string default "x"
            state A initial
            state B
            event Submit with Name as string
            from A on Submit when Submit.Name.length >= 3 -> transition B
            from A on Submit -> reject "too short"
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("A");
        var fire = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["Name"] = name });

        if (shouldTransition)
        {
            fire.Outcome.Should().Be(TransitionOutcome.Transition);
            fire.NewState.Should().Be("B");
        }
        else
        {
            fire.Outcome.Should().Be(TransitionOutcome.Rejected);
        }
    }

    // ========================================================================================
    // REGRESSION — existing .count on collections is unaffected
    // ========================================================================================

    [Fact]
    public void Regression_CollectionCount_ParsesAndCompiles_Unaffected()
    {
        // Verifies .count on collections still works after .length implementation.
        const string dsl = """
            precept M
            field Tags as set of string
            rule Tags.count <= 10 because "Too many tags"
            state A initial
            state B
            event Check
            from A on Check when Tags.count > 0 -> transition B
            from A on Check -> reject "no tags"
            """;

        var act = () => PreceptCompiler.Compile(PreceptParser.Parse(dsl));

        act.Should().NotThrow();
    }

    // ========================================================================================
    // Helper
    // ========================================================================================

    private static TypeCheckResult Check(string dsl)
    {
        var model = PreceptParser.Parse(dsl);
        return PreceptTypeChecker.Check(model);
    }
}
