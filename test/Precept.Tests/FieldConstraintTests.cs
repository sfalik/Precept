using System.Collections.Generic;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for field-level constraints (Issue #13).
/// Covers: parser acceptance, type checker diagnostics (C57/C58/C59),
/// runtime desugaring verification, nullable semantics, and event arg constraint behavior.
///
/// All tests are fully implemented as of Issue #13.
///
/// CatalogDriftTests.cs has the three entries for C57/C58/C59.
/// The `-> no transition` target is safe for compile-phase diagnostics — see the footgun
/// comment in CatalogDriftTests.cs § "noted footgun".
/// </summary>
public class FieldConstraintTests
{
    // ========================================================================================
    // BASELINE TESTS (READY NOW)
    // Verify the desugar destination already works — current invariant expressions that
    // express the same semantics as field-level constraint keywords.  Once issue #13 ships,
    // `field Amount as number nonnegative` must produce identical ConstraintFailure behavior
    // to the invariant forms tested here.
    // ========================================================================================

    [Fact] // READY
    public void Baseline_NonnegativeEquivalentInvariant_SetToNeg1_ProducesConstraintFailure()
    {
        // Desugar target: nonnegative → invariant Amount >= 0
        // Confirms that the runtime ConstraintFailure mechanism works correctly
        // for the semantics that `nonnegative` will desugar to.
        const string dsl = """
            precept M
            field Amount as number default 0
            invariant Amount >= 0 because "Amount must be non-negative"
            state Active initial
            event Set with Value as number
            from Active on Set -> set Amount = Set.Value -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");
        var fire = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = -1d });

        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact] // READY
    public void Baseline_PositiveEquivalentInvariant_SetToZero_ProducesConstraintFailure()
    {
        // Desugar target: positive → invariant Amount > 0
        // 0 is NOT positive — constraint failure expected.
        const string dsl = """
            precept M
            field Amount as number default 1
            invariant Amount > 0 because "Amount must be positive"
            state Active initial
            event Set with Value as number
            from Active on Set -> set Amount = Set.Value -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");
        var fire = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = 0d });

        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact] // READY
    public void Baseline_MinMaxEquivalentInvariant_OutOfRangeValues_ProduceConstraintFailure()
    {
        // Desugar target: min 1 max 10 → invariant Amount >= 1 and Amount <= 10
        // Setting to 0 or 11 should fail.
        const string dsl = """
            precept M
            field Amount as number default 5
            invariant Amount >= 1 because "Amount must be at least 1"
            invariant Amount <= 10 because "Amount must be at most 10"
            state Active initial
            event Set with Value as number
            from Active on Set -> set Amount = Set.Value -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");

        var belowMin = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = 0d });
        belowMin.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);

        var aboveMax = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = 11d });
        aboveMax.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    // ========================================================================================
    // PARSER TESTS — acceptance of new constraint keywords on fields and event args
    // ========================================================================================

    [Fact]
    public void Parse_Nonnegative_OnNumberField_ParsesWithoutError()
    {
        // `nonnegative` is a valid constraint suffix for number fields.
        const string dsl = """
            precept M
            field Amount as number default 0 nonnegative
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_Positive_OnNumberField_ParsesWithoutError()
    {
        // `positive` is a valid constraint suffix for number fields.
        const string dsl = """
            precept M
            field Amount as number default 1 positive
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_MinN_OnNumberField_ParsesWithoutError()
    {
        // `min N` is a valid constraint suffix for number fields.
        const string dsl = """
            precept M
            field Score as number default 0 min 0
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_MaxN_OnNumberField_ParsesWithoutError()
    {
        // `max N` is a valid constraint suffix for number fields.
        const string dsl = """
            precept M
            field Score as number default 0 max 100
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_Notempty_OnStringField_ParsesWithoutError()
    {
        // `notempty` is a valid constraint suffix for string fields.
        const string dsl = """
            precept M
            field Name as string default "x" notempty
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_MinlengthN_OnStringField_ParsesWithoutError()
    {
        const string dsl = """
            precept M
            field Name as string default "example" minlength 1
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_MaxlengthN_OnStringField_ParsesWithoutError()
    {
        const string dsl = """
            precept M
            field Name as string default "" maxlength 200
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_Notempty_OnCollectionField_ParsesWithoutError()
    {
        // `notempty` is a valid constraint suffix for set/queue/stack fields.
        const string dsl = """
            precept M
            field Tags as set of string notempty
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_MincountN_OnCollectionField_ParsesWithoutError()
    {
        const string dsl = """
            precept M
            field Members as set of string mincount 1
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_MaxcountN_OnCollectionField_ParsesWithoutError()
    {
        const string dsl = """
            precept M
            field Tags as set of string maxcount 10
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_ConstraintsOnEventArgs_ParsesWithoutError()
    {
        // Constraint keywords are valid after event arg type declarations.
        const string dsl = """
            precept M
            state Active initial
            event Submit with Name as string notempty maxlength 200
            from Active on Submit -> no transition
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("field Amount as number default 0 nonnegative max 100")]  // nonnegative then max
    [InlineData("field Amount as number default 0 max 100 nonnegative")]  // max then nonnegative (reversed)
    public void Parse_ConstraintOrder_AnyOrderAccepted_ParsesWithoutError(string fieldDecl)
    {
        // Constraint keywords on a field may appear in any order — both sequences are valid.
        var dsl = $"precept M\n{fieldDecl}\nstate Active initial\n";

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_MultipleConstraints_OnSingleField_ParsesWithoutError()
    {
        // Multiple constraints may appear on one field in a single declaration.
        const string dsl = """
            precept M
            field Score as number default 50 nonnegative min 1 max 100
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);

        act.Should().NotThrow();
    }

    // ========================================================================================
    // TYPE CHECKER TESTS — C57 (constraint type mismatch)
    // C57 fires when a constraint keyword is applied to an incompatible field type.
    // ========================================================================================

    [Fact]
    public void Check_Notempty_OnNumberField_ProducesC57()
    {
        // `notempty` is only valid for string and collection fields, not number.
        const string dsl = """
            precept M
            field Amount as number default 0 notempty
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C57");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT057");
    }

    [Fact] // requires Issue #13 type checker
    public void Check_Nonnegative_OnStringField_ProducesC57()
    {
        // `nonnegative` is only valid for number fields, not string.
        const string dsl = """
            precept M
            field Name as string default "x" nonnegative
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C57");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT057");
    }

    [Fact] // requires Issue #13 type checker
    public void Check_MinlengthN_OnNumberField_ProducesC57()
    {
        // `minlength` is only valid for string fields, not number.
        const string dsl = """
            precept M
            field Count as number default 0 minlength 1
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C57");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT057");
    }

    [Fact] // requires Issue #13 type checker
    public void Check_MincountN_OnStringField_ProducesC57()
    {
        // `mincount` is only valid for collection fields, not string.
        const string dsl = """
            precept M
            field Name as string default "x" mincount 1
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C57");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT057");
    }

    // ========================================================================================
    // TYPE CHECKER TESTS — C58 (contradictory or duplicate constraints)
    // C58 fires when two constraints on the same field are mutually impossible or redundant.
    // ========================================================================================

    [Fact]
    public void Check_MinGreaterThanMax_ProducesC58()
    {
        // `min 10 max 5` is impossible: no number satisfies both Amount >= 10 and Amount <= 5.
        const string dsl = """
            precept M
            field Score as number default 0 min 10 max 5
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C58");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT058");
    }

    [Fact]
    public void Check_DuplicateMin_ProducesC58()
    {
        // `min 5 min 10` — two `min` constraints on the same field is a duplicate declaration.
        // The second supersedes the first, but declaring both is an authoring error.
        const string dsl = """
            precept M
            field Score as number default 10 min 5 min 10
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C58");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT058");
    }

    [Fact]
    public void Check_NonnegativeAndPositive_ProducesC58_SubsumedConstraint()
    {
        // SOUP NAZI RECOMMENDATION: `nonnegative positive` should emit C58.
        //
        // Rationale:
        //   `nonnegative` desugars to Amount >= 0
        //   `positive`    desugars to Amount > 0
        //   `positive` is strictly stronger — any value satisfying `positive` automatically
        //   satisfies `nonnegative`. The weaker constraint is dead code: it can never fire
        //   independently of the stronger one. This is the definition of a redundant
        //   (subsumed) constraint.
        //
        // This is analogous to C44 (duplicate state assert) — not contradictory, but
        // the weaker assertion is entirely inert. C58 covers both contradictions AND
        // redundancies.
        //
        // ALTERNATIVE: treat as valid (just optimized away silently). Rejected because
        // silent subsuming is the same class of authoring mistake as duplicate keywords,
        // and Precept's philosophy is to surface these at compile time, not ignore them.
        //
        // Expected C58 message: "constraint 'nonnegative' is subsumed by 'positive'"
        const string dsl = """
            precept M
            field Amount as number default 1 nonnegative positive
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C58");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT058");
    }

    // ========================================================================================
    // TYPE CHECKER TESTS — C59 (default value violates constraint)
    // C59 fires at compile time when the declared default value cannot satisfy the constraint.
    // ========================================================================================

    [Fact]
    public void Check_DefaultNeg1_WithNonnegative_ProducesC59()
    {
        // default -1 does not satisfy nonnegative (Amount >= 0).
        const string dsl = """
            precept M
            field Amount as number default -1 nonnegative
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C59");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT059");
    }

    [Fact]
    public void Check_DefaultZero_WithPositive_ProducesC59()
    {
        // default 0 does not satisfy positive (Amount > 0). Zero is not positive.
        const string dsl = """
            precept M
            field Amount as number default 0 positive
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C59");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT059");
    }

    [Fact]
    public void Check_DefaultEmptyString_WithNotempty_ProducesC59()
    {
        // default "" does not satisfy notempty. An empty string fails the constraint.
        const string dsl = """
            precept M
            field Name as string default "" notempty
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C59");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT059");
    }

    [Fact]
    public void Check_Default5_WithMin10_ProducesC59()
    {
        // default 5 does not satisfy min 10 (Amount >= 10).
        const string dsl = """
            precept M
            field Amount as number default 5 min 10
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().ContainSingle(d => d.Constraint.Id == "C59");
        result.Diagnostics[0].DiagnosticCode.Should().Be("PRECEPT059");
    }

    [Fact]
    public void Check_DefaultZero_WithNonnegative_NoDiagnostic()
    {
        // default 0 satisfies nonnegative (0 >= 0). No C59 should be emitted.
        const string dsl = """
            precept M
            field Amount as number default 0 nonnegative
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Check_Default5_WithMin1Max10_NoDiagnostic()
    {
        // default 5 satisfies both min 1 and max 10. No C59 should be emitted.
        const string dsl = """
            precept M
            field Score as number default 5 min 1 max 10
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    // ========================================================================================
    // NULLABLE SEMANTICS
    // A nullable field with a constraint: null is always valid; the constraint only
    // applies when the field has a concrete value. No C59 for nullable fields regardless
    // of the default (which is implicitly null).
    // ========================================================================================

    [Fact]
    public void Check_NullableNumberField_WithNonnegative_NullableImpliesNoC59()
    {
        // A nullable field's implicit null default does not violate nonnegative.
        // null is structurally valid for a nullable field regardless of constraints.
        const string dsl = """
            precept M
            field Amount as number nullable nonnegative
            state Active initial
            """;

        var result = Check(dsl);

        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Fire_NullableNonnegativeField_NullValue_DoesNotProduceConstraintFailure()
    {
        // When a nullable nonnegative field holds null, no constraint violation occurs.
        // Desugared form: Amount == null or Amount >= 0
        const string dsl = """
            precept M
            field Amount as number nullable nonnegative
            state Active initial
            event Check
            from Active on Check -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Amount"] = null });
        var fire = workflow.Fire(instance, "Check");
        fire.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    // ========================================================================================
    // RUNTIME TESTS — desugaring verification
    // Each constraint must desugar correctly and produce ConstraintFailure on violation.
    // ========================================================================================

    [Fact]
    public void Fire_Nonnegative_SetToNeg1_ProducesConstraintFailure()
    {
        // `nonnegative` constraint on a field desugars to invariant Amount >= 0.
        // Setting Amount = -1 must produce ConstraintFailure.
        const string dsl = """
            precept M
            field Amount as number default 0 nonnegative
            state Active initial
            event Set with Value as number
            from Active on Set -> set Amount = Set.Value -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");
        var fire = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = -1d });
        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact]
    public void Fire_Nonnegative_SetToZero_Succeeds()
    {
        // 0 satisfies nonnegative (0 >= 0). No constraint failure.
        const string dsl = """
            precept M
            field Amount as number default 1 nonnegative
            state Active initial
            event Set with Value as number
            from Active on Set -> set Amount = Set.Value -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");
        var fire = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = 0d });
        (fire.Outcome is TransitionOutcome.NoTransition or TransitionOutcome.Transition).Should().BeTrue();
    }

    [Fact]
    public void Fire_Positive_SetToZero_ProducesConstraintFailure()
    {
        // `positive` desugars to Amount > 0. Zero is not positive.
        const string dsl = """
            precept M
            field Amount as number default 1 positive
            state Active initial
            event Set with Value as number
            from Active on Set -> set Amount = Set.Value -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");
        var fire = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = 0d });
        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact]
    public void Fire_Notempty_OnStringField_SetToEmpty_ProducesConstraintFailure()
    {
        // `notempty` on a string desugars to Name != "" (or equivalent).
        // Setting Name = "" must produce ConstraintFailure.
        const string dsl = """
            precept M
            field Name as string default "x" notempty
            state Active initial
            event Set with Value as string
            from Active on Set -> set Name = Set.Value -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");
        var fire = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = "" });
        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact]
    public void Fire_Maxlength5_OnStringField_SetToToolong_ProducesConstraintFailure()
    {
        // `maxlength 5` desugars to Code.length <= 5.
        // "toolong" is 7 characters — constraint failure expected.
        const string dsl = """
            precept M
            field Code as string default "abc" maxlength 5
            state Active initial
            event Set with Value as string
            from Active on Set -> set Code = Set.Value -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");
        var fire = workflow.Fire(instance, "Set", new Dictionary<string, object?> { ["Value"] = "toolong" });
        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact] // requires Issue #13 runtime (collection desugaring)
    public void Fire_Notempty_OnCollectionField_WhenEmpty_ProducesConstraintFailure()
    {
        // `notempty` on a collection desugars to invariant Tags.count > 0.
        // Start with a non-empty Tags (satisfies the constraint), then remove the last item,
        // making the collection empty. The invariant fires → ConstraintFailure.
        const string dsl = """
            precept M
            field Tags as set of string notempty
            state Active initial
            event Add with Tag as string
            from Active on Add -> add Tags Add.Tag -> no transition
            event Remove with Tag as string
            from Active on Remove -> remove Tags Remove.Tag -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        // Start with one tag (valid: count = 1 > 0)
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Tags"] = new List<object> { "initial" } });
        // Remove the last tag: count drops to 0, violating Tags.count > 0
        var fire = workflow.Fire(instance, "Remove", new Dictionary<string, object?> { ["Tag"] = "initial" });
        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact] // requires Issue #13 runtime (collection desugaring)
    public void Fire_Mincount2_OnCollectionField_Count1_ProducesConstraintFailure()
    {
        // `mincount 2` desugars to invariant Members.count >= 2.
        // Start with 2 members (valid), then remove one → count becomes 1 → ConstraintFailure.
        const string dsl = """
            precept M
            field Members as set of string mincount 2
            state Active initial
            event Remove with Member as string
            from Active on Remove -> remove Members Remove.Member -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        // Start with 2 members (valid: count = 2 >= 2)
        var instance = workflow.CreateInstance("Active", new Dictionary<string, object?> { ["Members"] = new List<object> { "alice", "bob" } });
        // Remove one member: count drops to 1, violating Members.count >= 2
        var fire = workflow.Fire(instance, "Remove", new Dictionary<string, object?> { ["Member"] = "alice" });
        fire.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    [Fact]
    public void Fire_EventArgNotempty_EmptyString_ProducesRejected()
    {
        // `notempty` on an event arg desugars to an on-event assert.
        // When the assert fires, the outcome is Rejected (not ConstraintFailure).
        // This distinguishes event-arg constraint desugaring from field constraint desugaring.
        const string dsl = """
            precept M
            state Active initial
            event Submit with Name as string notempty maxlength 200
            from Active on Submit -> no transition
            """;

        var workflow = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = workflow.CreateInstance("Active");
        var fire = workflow.Fire(instance, "Submit", new Dictionary<string, object?> { ["Name"] = "" });
        fire.Outcome.Should().Be(TransitionOutcome.Rejected);
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
