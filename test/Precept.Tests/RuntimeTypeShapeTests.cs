using FluentAssertions;
using Precept.Language;
using Precept.Runtime;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Shape tests for runtime types added or expanded in Wave 2.
/// Verifies construction and field access for types that have no evaluator
/// exercise yet: EventOutcome.Faulted, FieldSnapshot.ClrType, ConstraintViolation 5-field.
/// </summary>
public class RuntimeTypeShapeTests
{
    // ── EventOutcome.Faulted ─────────────────────────────────────────────────────

    [Fact]
    public void Faulted_CanBeConstructed_WithFaultValue()
    {
        var fault = new Fault(FaultCode.DivisionByZero, nameof(FaultCode.DivisionByZero), "Division by zero.");
        var outcome = new EventOutcome.Faulted(fault);
        outcome.Fault.Should().Be(fault);
    }

    [Fact]
    public void Faulted_IsEventOutcome_AndPatternMatchable()
    {
        var fault = new Fault(FaultCode.TypeMismatch, nameof(FaultCode.TypeMismatch), "Type mismatch.");
        EventOutcome outcome = new EventOutcome.Faulted(fault);

        var matched = outcome switch
        {
            EventOutcome.Faulted f => (FaultCode?)f.Fault.Code,
            _ => null
        };

        matched.Should().Be(FaultCode.TypeMismatch);
    }

    [Fact]
    public void Faulted_TypeHierarchy_IsSealed_UnderAbstractBase()
    {
        typeof(EventOutcome.Faulted).IsSealed.Should().BeTrue();
        typeof(EventOutcome).IsAbstract.Should().BeTrue();
    }

    // ── FieldSnapshot.ClrType ────────────────────────────────────────────────────

    [Fact]
    public void FieldSnapshot_ClrType_CanBeConstructed_WithNonNullType()
    {
        var snapshot = new FieldSnapshot(
            FieldName: "amount",
            Mode: FieldAccessMode.Read,
            FieldType: "decimal",
            IsResolved: true,
            Value: 42.5m,
            ClrType: typeof(decimal));

        snapshot.ClrType.Should().Be(typeof(decimal));
    }

    [Fact]
    public void FieldSnapshot_ClrType_CanBeNull_WhenUnresolved()
    {
        var snapshot = new FieldSnapshot(
            FieldName: "amount",
            Mode: FieldAccessMode.Read,
            FieldType: "decimal",
            IsResolved: false,
            Value: null,
            ClrType: null);

        snapshot.ClrType.Should().BeNull();
        snapshot.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void FieldSnapshot_AllFields_AreAccessible()
    {
        var snapshot = new FieldSnapshot("status", FieldAccessMode.Write, "string", true, "active", typeof(string));

        snapshot.FieldName.Should().Be("status");
        snapshot.Mode.Should().Be(FieldAccessMode.Write);
        snapshot.FieldType.Should().Be("string");
        snapshot.IsResolved.Should().BeTrue();
        snapshot.Value.Should().Be("active");
        snapshot.ClrType.Should().Be(typeof(string));
    }

    // ── ConstraintViolation 5-field shape ────────────────────────────────────────

    private static ConstraintDescriptor MakeDescriptor() =>
        new(
            Kind: ConstraintKind.Invariant,
            ScopeTarget: null,
            ExpressionText: "amount > 0",
            Because: "amount must be positive",
            ReferencedFields: ["amount"],
            HasGuard: false,
            SourceLine: 10);

    [Fact]
    public void ConstraintViolation_CanBeConstructed_WithAllFiveFields()
    {
        var descriptor = MakeDescriptor();
        var violation = new ConstraintViolation(
            Constraint: descriptor,
            Because: "amount must be positive",
            RelevantFields: ["amount"],
            FailingSubexpression: "amount > 0",
            FailingValue: -5m);

        violation.Constraint.Should().BeSameAs(descriptor);
        violation.Because.Should().Be("amount must be positive");
        violation.RelevantFields.Should().ContainSingle().Which.Should().Be("amount");
        violation.FailingSubexpression.Should().Be("amount > 0");
        violation.FailingValue.Should().Be(-5m);
    }

    [Fact]
    public void ConstraintViolation_NullableFields_AcceptNull()
    {
        var descriptor = MakeDescriptor();
        var violation = new ConstraintViolation(
            Constraint: descriptor,
            Because: "amount must be positive",
            RelevantFields: [],
            FailingSubexpression: null,
            FailingValue: null);

        violation.FailingSubexpression.Should().BeNull();
        violation.FailingValue.Should().BeNull();
        violation.RelevantFields.Should().BeEmpty();
    }

    [Fact]
    public void ConstraintViolation_Constraint_ReferencesDescriptorIdentity()
    {
        var descriptor = MakeDescriptor();
        var violation = new ConstraintViolation(descriptor, descriptor.Because, [], null, null);

        violation.Constraint.Kind.Should().Be(ConstraintKind.Invariant);
        violation.Constraint.ExpressionText.Should().Be("amount > 0");
        violation.Constraint.SourceLine.Should().Be(10);
    }
}
