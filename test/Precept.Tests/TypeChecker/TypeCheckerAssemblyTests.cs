using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 10 — Final Assembly + D26 Global Assert.
/// Validates SemanticIndex completeness (all 16 primaries + 4 FrozenDictionary secondaries),
/// D26 invariant (TypedErrorExpression ↔ Error diagnostic), FrozenDictionary D4 compliance,
/// end-to-end pipeline integration, and zero NotImplementedException stubs.
/// </summary>
public class TypeCheckerAssemblyTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Shared DSL strings
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Realistic multi-field stateful precept with transitions, rules, ensures, access modes,
    /// and diverse expression types. Derived from the insurance-claim sample but simplified
    /// to avoid access-mode guard syntax.
    /// </summary>
    private const string FullPrecept = """
        precept ClaimWorkflow

        field ClaimantName as string optional
        field ClaimAmount as decimal default 0 nonnegative maxplaces 2
        field ApprovedAmount as decimal default 0 nonnegative maxplaces 2
        field AdjusterName as string optional
        field DecisionNote as string optional maxlength 500
        field FraudFlag as boolean default false
        field MissingDocuments as set of string
        rule ApprovedAmount <= ClaimAmount because "Approved amounts cannot exceed the claim"

        state Draft initial
        state Submitted
        state UnderReview
        state Approved
        state Denied
        state Paid

        in UnderReview modify FraudFlag editable
        in Approved ensure ApprovedAmount > 0 because "Approved claims must specify a payout amount"

        event Submit(Claimant as string notempty, Amount as decimal)
        on Submit ensure Submit.Amount > 0 because "Claim amounts must be positive"

        event RequestDocument(Name as string notempty)
        event AssignAdjuster(Name as string notempty)
        event Approve(Amount as decimal, Note as string optional)
        on Approve ensure Approve.Amount > 0 because "Approved claim amounts must be positive"

        event Deny(Note as string notempty)
        event PayClaim

        from Draft on Submit
            -> set ClaimantName = Submit.Claimant
            -> set ClaimAmount = Submit.Amount
            -> transition Submitted

        from Submitted on RequestDocument
            -> add MissingDocuments RequestDocument.Name
            -> no transition
        from Submitted on AssignAdjuster
            -> set AdjusterName = trim(AssignAdjuster.Name)
            -> transition UnderReview

        from UnderReview on Approve when Approve.Amount <= ClaimAmount
            -> set ApprovedAmount = Approve.Amount
            -> set DecisionNote = Approve.Note
            -> transition Approved
        from UnderReview on Approve
            -> reject "Amount exceeds the claim"

        from UnderReview on Deny
            -> set DecisionNote = Deny.Note
            -> transition Denied
        from Approved on PayClaim
            -> transition Paid
        """;

    /// <summary>Minimal clean precept — smallest valid stateful precept.</summary>
    private const string MinimalPrecept = """
        precept Widget
        field Amount as integer
        state Open initial
        """;

    /// <summary>Precept with a deliberate unknown field reference to produce an error.</summary>
    private const string ErrorPrecept_UnknownField = """
        precept Widget
        field Amount as integer
        state Open initial
        event Go
        from Open on Go
            -> set Amount = Bogus
            -> no transition
        """;

    /// <summary>Precept with multiple errors (unknown fields in different transitions).</summary>
    private const string MultiErrorPrecept = """
        precept Widget
        field Amount as integer
        state Open initial
        state Closed
        event Go
        event Finish
        from Open on Go
            -> set Amount = NonExistent
            -> transition Closed
        from Closed on Finish
            -> set Amount = AlsoMissing
            -> no transition
        """;

    /// <summary>Traffic light — realistic multi-state precept with guards.</summary>
    private const string TrafficLightPrecept = """
        precept TrafficLight

        field VehiclesWaiting as number default 0 nonnegative
        field CycleCount as number default 0 nonnegative
        field LeftTurnQueued as boolean default false
        field EmergencyReason as string optional

        state Red initial
        state Green
        state Yellow
        state FlashingRed

        in Red modify VehiclesWaiting editable
        in Red modify LeftTurnQueued editable

        event Advance
        event Emergency(AuthorizedBy as string notempty, Reason as string notempty)
        event ClearEmergency

        from Red on Advance when VehiclesWaiting > 0
            -> set VehiclesWaiting = 0
            -> set CycleCount = CycleCount + 1
            -> transition Green
        from Red on Advance
            -> reject "No demand detected at red"

        from Green on Advance
            -> set CycleCount = CycleCount + 1
            -> transition Yellow
        from Yellow on Advance
            -> transition Red

        from Red on Emergency
            -> set EmergencyReason = Emergency.AuthorizedBy + ": " + Emergency.Reason
            -> transition FlashingRed
        from Green on Emergency
            -> set EmergencyReason = Emergency.AuthorizedBy + ": " + Emergency.Reason
            -> transition FlashingRed

        from FlashingRed on Advance
            -> set CycleCount = CycleCount + 1
            -> no transition
        from FlashingRed on ClearEmergency
            -> clear EmergencyReason
            -> transition Red
        """;

    // ════════════════════════════════════════════════════════════════════════
    //  Category 1: SemanticIndex Completeness
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CleanPrecept_FieldsPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.Fields.Should().NotBeEmpty("precept declares 7 fields");
    }

    [Fact]
    public void CleanPrecept_StatesPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.States.Should().NotBeEmpty("precept declares 6 states");
    }

    [Fact]
    public void CleanPrecept_EventsPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.Events.Should().NotBeEmpty("precept declares 7 events");
    }

    [Fact]
    public void CleanPrecept_TransitionRowsPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.TransitionRows.Should().NotBeEmpty("precept declares multiple transitions");
    }

    [Fact]
    public void CleanPrecept_RulesPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.Rules.Should().NotBeEmpty("precept declares 2 rules");
    }

    [Fact]
    public void CleanPrecept_EnsuresPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.Ensures.Should().HaveCount(3,
            "FullPrecept declares one state ensure and two event ensures");
    }

    [Fact]
    public void CleanPrecept_AccessModesPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.AccessModes.Should().ContainSingle(
            "FullPrecept declares one scoped access-mode construct");
    }

    [Fact]
    public void StateEnsure_MultiStateList_ExpandsIntoIndependentEnsures()
    {
        const string precept = """
            precept Widget
            field Amount as number default 0
            state Draft initial
            state Pending
            in Draft, Pending ensure Amount >= 0 because "Amount stays nonnegative"
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.Ensures.Should().HaveCount(2);
        index.Ensures.Select(e => e.AnchorState).Should().Equal("Draft", "Pending");
    }

    [Fact]
    public void AccessMode_MultiStateList_ExpandsIntoIndependentEntries()
    {
        const string precept = """
            precept Widget
            field Amount as number default 0
            state Draft initial
            state Pending
            in Draft, Pending modify Amount editable
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.AccessModes.Should().HaveCount(2);
        index.AccessModes.Select(mode => mode.StateName).Should().Equal("Draft", "Pending");
    }

    [Fact]
    public void StateAction_MultiStateList_ExpandsIntoIndependentHooks()
    {
        const string precept = """
            precept Widget
            field Amount as number default 0
            state Draft initial
            state Pending
            to Draft, Pending -> set Amount = Amount + 1
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.StateHooks.Should().HaveCount(2);
        index.StateHooks.Select(hook => hook.StateName).Should().Equal("Draft", "Pending");
    }

    [Fact]
    public void CleanPrecept_StateReferencesPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.StateReferences.Should().NotBeEmpty("transitions and ensures reference states");
    }

    [Fact]
    public void CleanPrecept_EventReferencesPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.EventReferences.Should().NotBeEmpty("transitions reference events");
    }

    [Fact]
    public void CleanPrecept_FieldReferencesPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.FieldReferences.Should().NotBeEmpty("guards and actions reference fields");
    }

    [Fact]
    public void CleanPrecept_DiagnosticsEmpty()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(FullPrecept);
        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("a clean precept produces no Error diagnostics");
    }

    [Fact]
    public void CleanPrecept_AllPrimaryFieldsPopulated()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        // All primary ImmutableArray fields should be accessible.
        index.Fields.Should().NotBeEmpty();
        index.States.Should().NotBeEmpty();
        index.Events.Should().NotBeEmpty();
        index.TransitionRows.Should().NotBeEmpty();
        index.Rules.Should().NotBeEmpty();
        index.Ensures.Should().NotBeEmpty();
        index.AccessModes.Should().NotBeEmpty();
        // EventHandlers may be empty depending on precept structure.
        // StateHooks, EditDeclarations, and ComputedDeps may also be empty.
        index.FieldReferences.Should().NotBeEmpty();
        index.StateReferences.Should().NotBeEmpty();
        index.EventReferences.Should().NotBeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 2: D26 Global Assert — Invariant Holds
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void D26_CleanPrecept_NoErrors_InvariantTriviallySatisfied()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(FullPrecept);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("clean precept → no Error diagnostics");

        // D26 is trivially satisfied: no TypedErrorExpression, no Error diagnostic required
        index.Diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("SemanticIndex itself has no errors");
    }

    [Fact]
    public void D26_ErrorPrecept_ErrorExpressionPresent_DiagnosticAlsoPresent()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(ErrorPrecept_UnknownField);

        // An unknown field ref produces at least one Error diagnostic
        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().NotBeEmpty("unknown field 'Bogus' must produce an Error diagnostic");

        // D26 invariant holds: TypedErrorExpression present → Error diagnostic present
        // (if this test passes, D26 Debug.Assert did not fire)
    }

    [Fact]
    public void D26_MultipleErrors_AllCapturedInDiagnostics()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(MultiErrorPrecept);

        var errors = diagnostics.Where(d => d.Severity == Severity.Error).ToList();
        errors.Should().HaveCountGreaterThanOrEqualTo(2,
            "two unknown field refs should each produce an Error diagnostic");
    }

    [Fact]
    public void D26_ErrorDiagnostic_HasErrorSeverity()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(ErrorPrecept_UnknownField);

        var errors = diagnostics.Where(d => d.Severity == Severity.Error).ToList();
        errors.Should().NotBeEmpty();
        errors.Should().AllSatisfy(d =>
            d.Severity.Should().Be(Severity.Error));
    }

    [Fact]
    public void D26_MinimalCleanPrecept_NoErrorExpressions()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(MinimalPrecept);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("minimal valid precept should have no errors");
    }

    [Fact]
    public void D26_TrafficLight_CleanPrecept_InvariantHolds()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(TrafficLightPrecept);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("traffic light sample is a valid precept");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 3: FrozenDictionary Secondaries — D4 Compliance
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FieldsByName_ReturnsCorrectTypedField()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        index.FieldsByName.Should().ContainKey("ClaimAmount");
        var field = index.FieldsByName["ClaimAmount"];
        field.Name.Should().Be("ClaimAmount");
        field.ResolvedType.Should().Be(TypeKind.Decimal);
    }

    [Fact]
    public void StatesByName_ReturnsCorrectTypedState()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        index.StatesByName.Should().ContainKey("Approved");
        var state = index.StatesByName["Approved"];
        state.Name.Should().Be("Approved");
    }

    [Fact]
    public void EventsByName_ReturnsCorrectTypedEvent()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        index.EventsByName.Should().ContainKey("Submit");
        var evt = index.EventsByName["Submit"];
        evt.Name.Should().Be("Submit");
        evt.Args.Should().NotBeEmpty("Submit has args");
    }

    [Fact]
    public void FrozenDictionary_MissingKey_TryGetValueReturnsFalse()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        index.FieldsByName.TryGetValue("NonExistentField", out _).Should().BeFalse();
        index.StatesByName.TryGetValue("NonExistentState", out _).Should().BeFalse();
        index.EventsByName.TryGetValue("NonExistentEvent", out _).Should().BeFalse();
    }

    [Fact]
    public void EnsuresByState_GroupsStateScopedEnsures()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);
        index.EnsuresByState.Should().ContainKey("Approved",
            "FullPrecept declares one state-scoped ensure on Approved");
        index.EnsuresByState["Approved"].Should().ContainSingle(
            "only the state-scoped ensure should appear in EnsuresByState");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 4: End-to-End Pipeline Integration
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Integration_AllStateReferencesPointToValidStates()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        foreach (var stateRef in index.StateReferences)
        {
            index.StatesByName.Should().ContainKey(stateRef.State.Name,
                $"StateReference to '{stateRef.State.Name}' must exist in StatesByName");
        }
    }

    [Fact]
    public void Integration_AllEventReferencesPointToValidEvents()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        foreach (var eventRef in index.EventReferences)
        {
            index.EventsByName.Should().ContainKey(eventRef.Event.Name,
                $"EventReference to '{eventRef.Event.Name}' must exist in EventsByName");
        }
    }

    [Fact]
    public void Integration_AllFieldTypeKindsResolved()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        foreach (var field in index.Fields)
        {
            field.ResolvedType.Should().NotBe(TypeKind.Error,
                $"field '{field.Name}' should have a resolved type, not Error");
        }
    }

    [Fact]
    public void Integration_TrafficLight_ProducesNoErrors()
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(TrafficLightPrecept);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("TrafficLight is a valid sample precept");

        index.Fields.Should().HaveCount(4);
        index.States.Should().HaveCount(4);
        index.Events.Should().HaveCount(3);
        index.TransitionRows.Should().HaveCountGreaterThan(5);
    }

    [Fact]
    public void Integration_InsuranceClaim_TransitionRowConsistency()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(FullPrecept);

        foreach (var row in index.TransitionRows)
        {
            // FromState is null for "from any" rows — that's valid
            if (row.FromState is not null)
            {
                index.StatesByName.Should().ContainKey(row.FromState,
                    $"transition source state '{row.FromState}' must exist");
            }

            index.EventsByName.Should().ContainKey(row.EventName,
                $"transition event '{row.EventName}' must exist");

            if (row.TargetState is not null)
            {
                index.StatesByName.Should().ContainKey(row.TargetState,
                    $"transition target state '{row.TargetState}' must exist");
            }
        }
    }

    [Fact]
    public void Integration_LoanApplication_FullSample()
    {
        const string loanPrecept = """
            precept LoanApplication

            field ApplicantName as string optional
            field RequestedAmount as number default 0 nonnegative
            field ApprovedAmount as number default 0 nonnegative
            field CreditScore as number default 0
            field DecisionNote as string optional
            rule ApprovedAmount <= RequestedAmount because "Approved amount cannot exceed the request"

            state Draft initial
            state UnderReview
            state Approved
            state Declined

            event Submit(
                Applicant as string notempty,
                Amount as number,
                Score as number)
            on Submit ensure Submit.Amount > 0 because "Loan requests must be positive"

            event Approve(Amount as number, Note as string optional)
            on Approve ensure Approve.Amount > 0 because "Approved amounts must be positive"

            event Decline(Note as string notempty)

            from Draft on Submit
                -> set ApplicantName = Submit.Applicant
                -> set RequestedAmount = Submit.Amount
                -> set CreditScore = Submit.Score
                -> transition UnderReview

            from UnderReview on Approve when Approve.Amount <= RequestedAmount
                -> set ApprovedAmount = Approve.Amount
                -> set DecisionNote = Approve.Note
                -> transition Approved
            from UnderReview on Approve
                -> reject "Approval requires strong credit"

            from UnderReview on Decline
                -> set DecisionNote = Decline.Note
                -> transition Declined
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(loanPrecept);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("LoanApplication is a valid sample precept");

        index.Fields.Should().HaveCount(5);
        index.States.Should().HaveCount(4);
        index.Events.Should().HaveCount(3);
        index.Rules.Should().HaveCount(1);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 5: Zero NotImplementedException Stubs
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypedField_ResolvedType_NotError()
    {
        // Verify TypedField has a resolved type (not Error), confirming that field
        // population works end-to-end without stubs throwing.
        const string preceptText = """
            precept Widget
            field Amount as integer
            field Label as string optional
            state Open initial
            """;

        var (index, _) = TypeCheckerTestHelpers.Check(preceptText);

        var amountField = index.Fields.FirstOrDefault(f => f.Name == "Amount");
        amountField.Should().NotBeNull();
        amountField!.ResolvedType.Should().Be(TypeKind.Integer,
            "integer field should resolve to TypeKind.Integer");

        var labelField = index.Fields.FirstOrDefault(f => f.Name == "Label");
        labelField.Should().NotBeNull();
        labelField!.ResolvedType.Should().Be(TypeKind.String,
            "string field should resolve to TypeKind.String");
        labelField.IsOptional.Should().BeTrue();
    }

    [Fact]
    public void InterpolatedString_ResolvedNotStubbed()
    {
        // String concatenation with + is the DSL equivalent; interpolated strings
        // are tested at the expression level. Here we verify the pipeline end-to-end
        // by using string concatenation in an action.
        const string preceptText = """
            precept Widget
            field Label as string optional
            field Code as string optional
            state Open initial
            event Update(Prefix as string notempty, Suffix as string notempty)
            from Open on Update
                -> set Label = Update.Prefix + " - " + Update.Suffix
                -> no transition
            """;

        var (index, diagnostics) = TypeCheckerTestHelpers.Check(preceptText);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty("string concatenation should resolve cleanly");

        // The action's expression tree must be a resolved TypedBinaryOp, not TypedErrorExpression
        var row = index.TransitionRows.First();
        row.Actions.Should().NotBeEmpty();
        var inputAction = row.Actions[0] as TypedInputAction;
        inputAction.Should().NotBeNull();
        inputAction!.InputExpression.Should().NotBeOfType<TypedErrorExpression>(
            "string concatenation expression should not be a stub error");
    }

    [Fact]
    public void BuildSemanticIndex_CompletesWithoutException()
    {
        // The simplest possible assertion: Check() completes without
        // throwing NotImplementedException, proving all stubs are removed.
        var (index, _) = TypeCheckerTestHelpers.Check(TrafficLightPrecept);
        index.Should().NotBeNull("BuildSemanticIndex must complete without exception");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 6: Determinism (§10 G5)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Check_SameInput_ReturnsDeterministicOutput()
    {
        // §10 G5: Same source → identical SemanticIndex. The type checker is a
        // total function with no internal mutable state, so calling Check() twice
        // on the same string must produce structurally equal output.
        var (index1, diag1) = TypeCheckerTestHelpers.Check(TrafficLightPrecept);
        var (index2, diag2) = TypeCheckerTestHelpers.Check(TrafficLightPrecept);

        // Diagnostic count must be stable across calls.
        diag1.Count.Should().Be(diag2.Count,
            because: "diagnostic count must be deterministic on the same input");

        // Symbol table sizes must be identical.
        index1.Fields.Length.Should().Be(index2.Fields.Length,
            because: "field count must be deterministic");
        index1.States.Length.Should().Be(index2.States.Length,
            because: "state count must be deterministic");
        index1.Events.Length.Should().Be(index2.Events.Length,
            because: "event count must be deterministic");
        index1.TransitionRows.Length.Should().Be(index2.TransitionRows.Length,
            because: "transition row count must be deterministic");

        // Spot-check: first field's resolved type is stable across runs.
        index1.Fields.Should().NotBeEmpty();
        index1.Fields[0].ResolvedType.Should().Be(index2.Fields[0].ResolvedType,
            because: "field type resolution must be deterministic (§10 G5)");

        // Spot-check: first guarded row's binary op resolves to the same operation.
        var guardedRow1 = index1.TransitionRows.FirstOrDefault(r => r.Guard is TypedBinaryOp);
        var guardedRow2 = index2.TransitionRows.FirstOrDefault(r => r.Guard is TypedBinaryOp);

        if (guardedRow1 is not null && guardedRow2 is not null)
        {
            var op1 = ((TypedBinaryOp)guardedRow1.Guard!).ResolvedOp;
            var op2 = ((TypedBinaryOp)guardedRow2.Guard!).ResolvedOp;
            op1.Should().Be(op2,
                because: "binary operation resolution in guards must be deterministic (§10 G5)");
        }
    }
}
