using System.Collections.Generic;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the choice type (Issue #25, Slice 7 — choice portion).
/// Covers: parser acceptance, type-checker validation (C62/C63/C64/C66),
/// runtime membership enforcement, and equality comparison.
/// </summary>
public class PreceptChoiceTypeTests
{
    // ════════════════════════════════════════════════════════════════════
    // PARSER — field declaration
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ChoiceField_BasicDeclaration_Succeeds()
    {
        const string dsl = """
            precept M
            field Status as choice("Draft","Active","Closed") default "Draft"
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().ContainSingle();
        var f = model.Fields[0];
        f.Name.Should().Be("Status");
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.ChoiceValues.Should().BeEquivalentTo(["Draft", "Active", "Closed"],
            opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Parse_ChoiceField_SingleValue_Succeeds()
    {
        const string dsl = """
            precept M
            field State as choice("Only") default "Only"
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields[0].ChoiceValues.Should().ContainSingle().Which.Should().Be("Only");
    }

    [Fact]
    public void Parse_ChoiceField_WithDefault_Succeeds()
    {
        const string dsl = """
            precept M
            field Status as choice("A","B","C") default "A"
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be("A");
        f.ChoiceValues.Should().BeEquivalentTo(["A", "B", "C"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Parse_ChoiceField_WithOrdered_Succeeds()
    {
        const string dsl = """
            precept M
            field Priority as choice("Low","Medium","High") default "Low" ordered
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.IsOrdered.Should().BeTrue();
        f.ChoiceValues.Should().BeEquivalentTo(["Low", "Medium", "High"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Parse_ChoiceField_WithOrderedAndDefault_Succeeds()
    {
        const string dsl = """
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.IsOrdered.Should().BeTrue();
        f.DefaultValue.Should().Be("Low");
    }

    [Fact]
    public void Parse_ChoiceField_Nullable_Succeeds()
    {
        const string dsl = """
            precept M
            field Category as choice("X","Y") nullable
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.IsNullable.Should().BeTrue();
        f.Type.Should().Be(PreceptScalarType.Choice);
    }

    [Fact]
    public void Parse_ChoiceEventArg_Succeeds()
    {
        const string dsl = """
            precept M
            state A initial
            event Assign with Level as choice("Low","High")
            """;

        var model = PreceptParser.Parse(dsl);

        model.Events.Should().ContainSingle();
        var arg = model.Events[0].Args[0];
        arg.Name.Should().Be("Level");
        arg.Type.Should().Be(PreceptScalarType.Choice);
        arg.ChoiceValues.Should().BeEquivalentTo(["Low", "High"], opts => opts.WithStrictOrdering());
    }

    // ════════════════════════════════════════════════════════════════════
    // TYPE CHECKER — C62 (empty set), C63 (duplicates), C64 (bad default), C66 (ordered on non-choice)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypeCheck_ChoiceField_DefaultInSet_NoError()
    {
        const string dsl = """
            precept M
            field Status as choice("A","B") default "A"
            state A initial
            state B
            event Go
            from A on Go -> set Status = "B" -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id.StartsWith("C6"));
    }

    [Fact]
    public void TypeCheck_ChoiceField_DefaultNotInSet_EmitsC64()
    {
        // Default value "C" is not in the choice set {"A","B"} → C64
        const string dsl = """
            precept M
            field Status as choice("A","B") default "C"
            state A initial
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C64");
    }

    [Fact]
    public void TypeCheck_ChoiceField_DuplicateValues_EmitsC63()
    {
        // Construct model directly — duplicate values in choice set → C63
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Status", PreceptScalarType.Choice, false,
                ChoiceValues: ["A", "A", "B"])],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C63");
    }

    [Fact]
    public void TypeCheck_ChoiceField_EmptySet_EmitsC62()
    {
        // Empty choice set → C62
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Status", PreceptScalarType.Choice, false,
                ChoiceValues: [])],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C62");
    }

    [Fact]
    public void TypeCheck_OrderedOnChoiceField_NoError()
    {
        // ordered on a choice field is valid
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Priority", PreceptScalarType.Choice, false,
                ChoiceValues: ["Low", "High"], IsOrdered: true)],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id == "C66");
    }

    [Fact]
    public void TypeCheck_OrderedOnNonChoiceField_EmitsC66()
    {
        // ordered on a string field → C66
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A")],
            new PreceptState("A"),
            [],
            [new PreceptField("Name", PreceptScalarType.String, false,
                IsOrdered: true)],
            [], null);

        var result = PreceptCompiler.Validate(model);

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C66");
    }

    // ════════════════════════════════════════════════════════════════════
    // TYPE CHECKER — C65 (ordinal operator on unordered choice)
    //                C67 (cross-field ordinal comparison)
    //
    // These tests are written to FAIL against current behavior.
    // Current: C41 is emitted (or thrown) for all >/</>=/<= on choice fields
    //          regardless of ordered modifier or operand shape.
    // Expected:
    //   C65 — field is choice but missing 'ordered' modifier
    //   C67 — both operands are choice fields (cross-field ordinal has no semantics)
    //   equality (==) between two choice fields remains allowed (string comparison)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypeCheck_UnorderedChoiceField_GreaterThan_EmitsC65()
    {
        // Status has no 'ordered' modifier — ordinal operator should be C65, not C41
        const string dsl = """
            precept M
            field Status as choice("Draft","Active","Closed") default "Draft"
            state S initial
            event Advance
            from S on Advance when Status > "Active" -> no transition
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C65",
            because: "ordinal operator '>' requires the field to be declared 'ordered'");
    }

    [Fact]
    public void TypeCheck_UnorderedChoiceField_LessThan_EmitsC65()
    {
        // '<' on a choice field without 'ordered' → C65
        const string dsl = """
            precept M
            field Priority as choice("Low","Med","High") default "Low"
            state S initial
            event Check
            from S on Check when Priority < "High" -> no transition
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C65",
            because: "ordinal operator '<' requires the field to be declared 'ordered'");
    }

    [Fact]
    public void TypeCheck_OrderedChoice_CrossField_SameChoiceSet_EmitsC67()
    {
        // Both fields are ordered with the same values, but they are different fields.
        // Cross-field ordinal comparison has no defined semantics → C67
        const string dsl = """
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            field Severity as choice("Low","Med","High") default "Low" ordered
            state S initial
            event Compare
            from S on Compare when Priority > Severity -> no transition
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C67",
            because: "ordinal comparison between two choice fields is not defined, even when their value sets are identical");
    }

    [Fact]
    public void TypeCheck_OrderedChoice_CrossField_DifferentChoiceSets_EmitsC67()
    {
        // Priority and Status are ordered choice fields with different value sets.
        // Ordinal rank is field-local — comparing across fields has no meaning → C67
        const string dsl = """
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            field Status as choice("Draft","Active","Closed") default "Draft" ordered
            state S initial
            event Compare
            from S on Compare when Priority > Status -> no transition
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C67",
            because: "ordinal comparison between two choice fields with incompatible value sets is not defined");
    }

    [Fact]
    public void TypeCheck_OrderedChoice_CrossField_EqualityComparison_IsAllowed()
    {
        // == between two ordered choice fields is fine — both are strings at runtime,
        // and equality does not depend on ordinal position
        const string dsl = """
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            field Severity as choice("Low","Med","High") default "Low" ordered
            state S initial
            event Check
            from S on Check when Priority == Severity -> no transition
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().NotContain(d => d.Constraint.Id == "C67",
            because: "equality comparison between two choice fields is valid — it is a string equality check, not ordinal");
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — membership enforcement, equality comparison
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_Choice_AssignValidMember_Succeeds()
    {
        const string dsl = """
            precept M
            field Status as choice("Draft","Active","Closed") default "Draft"
            state S initial
            event Activate with NewStatus as choice("Draft","Active","Closed")
            from S on Activate -> set Status = Activate.NewStatus -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Activate", new Dictionary<string, object?> { ["NewStatus"] = "Active" });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Status"].Should().Be("Active");
    }

    [Fact]
    public void Runtime_Choice_AssignInvalidMember_IsRejected()
    {
        // "Pending" is not in the choice set — assignment should be rejected
        const string dsl = """
            precept M
            field Status as choice("Draft","Active","Closed") default "Draft"
            state S initial
            event Update with NewStatus as string
            from S on Update -> set Status = Update.NewStatus -> no transition
            """;

        // This DSL should compile (type checker doesn't flag runtime-only checking for event.arg → field mismatch in this case)
        // But when fired with an invalid value, TryValidateAssignedValue should reject it
        var result = PreceptParser.Parse(dsl);
        PreceptCompiler.Validate(result).Diagnostics.Should().NotContain(d => d.Constraint.Id == "C39");

        var engine = PreceptCompiler.Compile(result);
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Update", new Dictionary<string, object?> { ["NewStatus"] = "Pending" });

        // The assignment should fail at runtime — either Rejected or ConstraintFailure
        fired.Outcome.Should().NotBe(TransitionOutcome.NoTransition);
        fired.Outcome.Should().NotBe(TransitionOutcome.Transition);
    }

    [Fact]
    public void Runtime_Choice_DefaultValue_IsSetOnCreation()
    {
        const string dsl = """
            precept M
            field Status as choice("A","B","C") default "A"
            state S initial
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();

        inst.InstanceData["Status"].Should().Be("A");
    }

    [Fact]
    public void Runtime_Choice_EqualityComparison_Works()
    {
        const string dsl = """
            precept M
            field Status as choice("Open","Closed") default "Open"
            state S initial
            event Close
            from S on Close when Status == "Open" -> set Status = "Closed" -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Close");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
        fired.UpdatedInstance!.InstanceData["Status"].Should().Be("Closed");
    }

    [Fact]
    public void Runtime_Choice_EventArg_ValidMember_Accepted()
    {
        const string dsl = """
            precept M
            state S initial
            event SetLevel with Level as choice("Low","Med","High")
            from S on SetLevel -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "SetLevel", new Dictionary<string, object?> { ["Level"] = "Med" });

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    [Fact]
    public void Runtime_Choice_EventArg_InvalidMember_IsRejected()
    {
        const string dsl = """
            precept M
            state S initial
            event SetLevel with Level as choice("Low","Med","High")
            from S on SetLevel -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "SetLevel", new Dictionary<string, object?> { ["Level"] = "Critical" });

        // "Critical" is not in the choice set for the event arg → rejected
        fired.Outcome.Should().Be(TransitionOutcome.Rejected);
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — equality / inequality operators on choice fields
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_Choice_InequalityComparison_Works()
    {
        // != on a choice field: guard passes when values differ, fails when equal
        const string dsl = """
            precept M
            field Status as choice("Open","Closed") default "Open"
            state S initial
            event Check
            from S on Check when Status != "Closed" -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();

        // Status is "Open" (default); "Open" != "Closed" → guard matches
        var fired = engine.Fire(inst, "Check");
        fired.Outcome.Should().Be(TransitionOutcome.NoTransition,
            because: "'Open' != 'Closed' is true — guard should match");

        // Change Status to "Closed" via a separate set
        const string setDsl = """
            precept M
            field Status as choice("Open","Closed") default "Open"
            state S initial
            event Close
            event Check
            from S on Close -> set Status = "Closed" -> no transition
            from S on Check when Status != "Closed" -> no transition
            """;
        var engine2 = PreceptCompiler.Compile(PreceptParser.Parse(setDsl));
        var inst2 = engine2.CreateInstance();
        var afterClose = engine2.Fire(inst2, "Close");
        var checked2 = engine2.Fire(afterClose.UpdatedInstance!, "Check");
        checked2.Outcome.Should().Be(TransitionOutcome.Unmatched,
            because: "'Closed' != 'Closed' is false — guard should not match");
    }

    [Fact]
    public void Runtime_Choice_AddValidMember_Succeeds()
    {
        // Adding a valid choice member to a set of choice succeeds and the value is present
        const string dsl = """
            precept M
            field Tags as set of choice("Alpha","Beta","Gamma")
            state S initial
            event Tag
            from S on Tag -> add Tags "Beta" -> no transition
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var inst = engine.CreateInstance();
        var fired = engine.Fire(inst, "Tag");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition,
            because: "'Beta' is a valid member of choice('Alpha','Beta','Gamma')");
        (fired.UpdatedInstance!.InstanceData["Tags"] as System.Collections.Generic.List<object>)
            .Should().NotBeNull()
            .And.Contain("Beta",
            because: "the value should have been added to the collection");
    }

    // ════════════════════════════════════════════════════════════════════
    // RUNTIME — ordinal comparison for ordered choice fields
    //
    // canonical set: choice("Low","Med","High") ordered
    // declaration order:  Low=0, Med=1, High=2
    // alphabetical order: High < Low < Med
    //
    // These two orderings diverge. A correct implementation uses declaration
    // order. A string-comparison implementation produces wrong answers.
    // These tests are written to FAIL against current behavior (string comparison).
    // ════════════════════════════════════════════════════════════════════

    // Helper DSL: Priority starts at "Low", Escalate fires when Priority > threshold
    private static PreceptEngine BuildPriorityEngine() =>
        PreceptCompiler.Compile(PreceptParser.Parse("""
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            state S initial
            event SetPriority with Level as choice("Low","Med","High")
            event EscalateIfAboveLow
            event EscalateIfAboveMed
            from S on SetPriority -> set Priority = SetPriority.Level -> no transition
            from S on EscalateIfAboveLow when Priority > "Low" -> no transition
            from S on EscalateIfAboveMed when Priority > "Med" -> no transition
            """));

    [Fact]
    public void Runtime_OrderedChoice_GreaterThan_MatchesByDeclarationOrder()
    {
        // "High" is at index 2, "Low" is at index 0.
        // Declaration order: High > Low → guard should MATCH.
        // Alphabetical order: "High" < "Low" (H < L) → guard would FAIL (wrong).
        var engine = BuildPriorityEngine();
        var inst = engine.CreateInstance();

        // Set Priority to "High"
        engine.Fire(inst, "SetPriority", new Dictionary<string, object?> { ["Level"] = "High" })
            .UpdatedInstance.Should().NotBeNull();
        var afterSet = engine.Fire(inst, "SetPriority", new Dictionary<string, object?> { ["Level"] = "High" });
        var highInst = afterSet.UpdatedInstance!;

        // Fire guard: Priority > "Low" — "High" is above "Low" in declaration order → should match
        var fired = engine.Fire(highInst, "EscalateIfAboveLow");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition,
            because: "'High' (index 2) > 'Low' (index 0) in declaration order");
    }

    [Fact]
    public void Runtime_OrderedChoice_GreaterThan_DoesNotMatchWhenEqual()
    {
        // Priority == "Low": Low > "Low" is false.
        var engine = BuildPriorityEngine();
        var inst = engine.CreateInstance();
        // Default is already "Low" — fire event directly
        var fired = engine.Fire(inst, "EscalateIfAboveLow");

        fired.Outcome.Should().Be(TransitionOutcome.Unmatched,
            because: "'Low' (index 0) is not > 'Low' (index 0)");
    }

    [Fact]
    public void Runtime_OrderedChoice_GreaterThan_DoesNotMatchLowerRank()
    {
        // Priority = "Low", guard is Priority > "Med".
        // Declaration order: Low (0) is not > Med (1) → guard should NOT match.
        // Alphabetical: "Low" > "Med" (L > M is false anyway — same conclusion by accident).
        // Use Med > High test to expose divergence:
        var engine = PreceptCompiler.Compile(PreceptParser.Parse("""
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            state S initial
            event SetPriority with Level as choice("Low","Med","High")
            event CheckMedAboveHigh
            from S on SetPriority -> set Priority = SetPriority.Level -> no transition
            from S on CheckMedAboveHigh when Priority > "High" -> no transition
            """));

        var inst = engine.CreateInstance();
        var afterSet = engine.Fire(inst, "SetPriority", new Dictionary<string, object?> { ["Level"] = "Med" });

        // Priority = "Med" → Med > "High"?
        // Declaration order: Med (1) is NOT > High (2) → should be Unmatched.
        // Alphabetical: "Med" > "High" (M > H) → would incorrectly match.
        var fired = engine.Fire(afterSet.UpdatedInstance!, "CheckMedAboveHigh");

        fired.Outcome.Should().Be(TransitionOutcome.Unmatched,
            because: "'Med' (index 1) is not > 'High' (index 2) in declaration order — alphabetic comparison gives the wrong answer here");
    }

    [Fact]
    public void Runtime_OrderedChoice_LessThan_MatchesByDeclarationOrder()
    {
        // "Low" < "Med" by declaration order (0 < 1).
        // Alphabetically: "Low" < "Med" (L < M) — same answer by coincidence.
        // Use a set where they diverge: check "Med" < "High" alphabetically "Med" > "High" (M > H).
        var engine = PreceptCompiler.Compile(PreceptParser.Parse("""
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            state S initial
            event SetPriority with Level as choice("Low","Med","High")
            event CheckMedBelowHigh
            from S on SetPriority -> set Priority = SetPriority.Level -> no transition
            from S on CheckMedBelowHigh when Priority < "High" -> no transition
            """));

        var inst = engine.CreateInstance();
        var afterSet = engine.Fire(inst, "SetPriority", new Dictionary<string, object?> { ["Level"] = "Med" });

        // Priority = "Med" → Med < "High"?
        // Declaration order: Med (1) < High (2) → should MATCH.
        // Alphabetical: "Med" > "High" (M > H) → would NOT match (wrong).
        var fired = engine.Fire(afterSet.UpdatedInstance!, "CheckMedBelowHigh");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition,
            because: "'Med' (index 1) < 'High' (index 2) in declaration order");
    }

    [Fact]
    public void Runtime_OrderedChoice_GreaterThanOrEqual_MatchesEqualRank()
    {
        // Priority = "Med", guard Priority >= "Med" → should match (equal rank).
        var engine = PreceptCompiler.Compile(PreceptParser.Parse("""
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            state S initial
            event SetPriority with Level as choice("Low","Med","High")
            event CheckMedOrAbove
            from S on SetPriority -> set Priority = SetPriority.Level -> no transition
            from S on CheckMedOrAbove when Priority >= "Med" -> no transition
            """));

        var inst = engine.CreateInstance();
        var afterSet = engine.Fire(inst, "SetPriority", new Dictionary<string, object?> { ["Level"] = "Med" });
        var fired = engine.Fire(afterSet.UpdatedInstance!, "CheckMedOrAbove");

        fired.Outcome.Should().Be(TransitionOutcome.NoTransition,
            because: "'Med' (index 1) >= 'Med' (index 1)");
    }

    [Fact]
    public void Runtime_OrderedChoice_LessThanOrEqual_MatchesByDeclarationOrder()
    {
        // "Med" <= "High" by declaration order (1 <= 2) → should match.
        // Alphabetically: "Med" > "High" (M > H) → would NOT match (wrong).
        // This case exposes the divergence between alphabetical and declaration order.
        var engine = PreceptCompiler.Compile(PreceptParser.Parse("""
            precept M
            field Priority as choice("Low","Med","High") default "Low" ordered
            state S initial
            event SetPriority with Level as choice("Low","Med","High")
            event CheckMedOrBelow
            from S on SetPriority -> set Priority = SetPriority.Level -> no transition
            from S on CheckMedOrBelow when Priority <= "High" -> no transition
            """));

        var inst = engine.CreateInstance();
        var afterSet = engine.Fire(inst, "SetPriority", new Dictionary<string, object?> { ["Level"] = "Med" });

        // Priority = "Med" → Med <= "High"?
        // Declaration order: Med (1) <= High (2) → should MATCH.
        // Alphabetical: "Med" > "High" (M > H) → would NOT match (wrong).
        var fired = engine.Fire(afterSet.UpdatedInstance!, "CheckMedOrBelow");
        fired.Outcome.Should().Be(TransitionOutcome.NoTransition,
            because: "'Med' (index 1) <= 'High' (index 2) in declaration order");

        // Also verify equal-rank case: Low <= Low → should match
        var checkLow = engine.Fire(inst, "CheckMedOrBelow");
        checkLow.Outcome.Should().Be(TransitionOutcome.NoTransition,
            because: "'Low' (index 0) <= 'High' (index 2) — lower boundary also matches");
    }

    [Fact]
    public void Runtime_OrderedChoice_StoredValueIsStillString()
    {
        // Ordinal comparison must not change how the value is stored.
        // InstanceData["Priority"] must remain the string "High", not an index.
        var engine = BuildPriorityEngine();
        var inst = engine.CreateInstance();
        var afterSet = engine.Fire(inst, "SetPriority", new Dictionary<string, object?> { ["Level"] = "High" });

        afterSet.UpdatedInstance!.InstanceData["Priority"].Should().Be("High");
        afterSet.UpdatedInstance.InstanceData["Priority"].Should().BeOfType<string>();
    }

    // ════════════════════════════════════════════════════════════════════
    // GAP COVERAGE — Issue #25 AC gaps identified in final review
    // ════════════════════════════════════════════════════════════════════

    // Gap 1: set/queue/stack of choice(…) — collection type parsing

    [Fact]
    public void Parse_SetOfChoice_Succeeds()
    {
        const string dsl = """
            precept M
            field Tags as set of choice("Alpha","Beta","Gamma")
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().ContainSingle();
        var col = model.CollectionFields[0];
        col.Name.Should().Be("Tags");
        col.InnerType.Should().Be(PreceptScalarType.Choice);
        col.ChoiceValues.Should().BeEquivalentTo(["Alpha", "Beta", "Gamma"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Parse_QueueOfChoice_Succeeds()
    {
        const string dsl = """
            precept M
            field Queue as queue of choice("X","Y","Z")
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().ContainSingle();
        var col = model.CollectionFields[0];
        col.Name.Should().Be("Queue");
        col.InnerType.Should().Be(PreceptScalarType.Choice);
        col.ChoiceValues.Should().BeEquivalentTo(["X", "Y", "Z"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Parse_StackOfChoice_Succeeds()
    {
        const string dsl = """
            precept M
            field Stack as stack of choice("P","Q","R")
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().ContainSingle();
        var col = model.CollectionFields[0];
        col.Name.Should().Be("Stack");
        col.InnerType.Should().Be(PreceptScalarType.Choice);
        col.ChoiceValues.Should().BeEquivalentTo(["P", "Q", "R"], opts => opts.WithStrictOrdering());
    }

    // Gap 2: set X = "literal not in choice set" → C68

    [Fact]
    public void TypeCheck_SetAssignment_LiteralNotInChoiceSet_EmitsC68()
    {
        // "Invalid" is not in choice("Open","Closed") → C68
        const string dsl = """
            precept M
            field Status as choice("Open","Closed") default "Open"
            state A initial
            state B
            event Go
            from A on Go -> set Status = "Invalid" -> transition B
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C68",
            because: "'Invalid' is not a member of the choice set {\"Open\", \"Closed\"}");
    }

    // Gap 3: add CollectionField "literal not in choice set" → C68

    [Fact]
    public void TypeCheck_AddToChoiceCollection_LiteralNotInSet_EmitsC68()
    {
        // "Unknown" is not in choice("Alpha","Beta") → C68
        const string dsl = """
            precept M
            field Tags as set of choice("Alpha","Beta")
            state A initial
            event Tag
            from A on Tag -> add Tags "Unknown" -> no transition
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().Contain(d => d.Constraint.Id == "C68",
            because: "'Unknown' is not a member of the choice set for the collection");
    }

    // Gap 4: arithmetic on choice values is a compile-time error

    [Fact]
    public void TypeCheck_ArithmeticOnChoiceField_EmitsError()
    {
        // Status is a choice field — arithmetic '+' on it is not valid
        const string dsl = """
            precept M
            field Status as choice("A","B") default "A"
            state S initial
            event Go
            from S on Go when Status + "X" == "B" -> no transition
            """;

        var result = PreceptCompiler.Validate(PreceptParser.Parse(dsl));

        result.Diagnostics.Should().NotBeEmpty(
            because: "arithmetic on a choice field should produce a compile-time diagnostic");
    }

    // Gap 5: ordered modifier works on choice event args

    [Fact]
    public void Parse_ChoiceEventArg_WithOrdered_Succeeds()
    {
        const string dsl = """
            precept M
            state A initial
            event Assign with Level as choice("Low","High") ordered
            """;

        var model = PreceptParser.Parse(dsl);

        model.Events.Should().ContainSingle();
        var arg = model.Events[0].Args[0];
        arg.Name.Should().Be("Level");
        arg.Type.Should().Be(PreceptScalarType.Choice);
        arg.IsOrdered.Should().BeTrue(because: "'ordered' modifier must be recognised on event args");
    }

    // Gap 6: ordered composes correctly with nullable on choice fields

    [Fact]
    public void Parse_ChoiceField_OrderedAndNullable_Succeeds()
    {
        const string dsl = """
            precept M
            field Priority as choice("Low","High") nullable ordered
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Choice);
        f.IsOrdered.Should().BeTrue(because: "'ordered' must be preserved when combined with 'nullable'");
        f.IsNullable.Should().BeTrue(because: "'nullable' must be preserved when combined with 'ordered'");
    }
}
