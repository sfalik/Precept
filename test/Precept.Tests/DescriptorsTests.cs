using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept.Language;
using Precept.Runtime;
using Xunit;

namespace Precept.Tests;

public class DescriptorsTests
{
    // ── FieldDescriptor ─────────────────────────────────────────────────────────

    [Fact]
    public void FieldDescriptor_Construction_SetsAllProperties()
    {
        var modifiers = new[] { ModifierKind.Optional, ModifierKind.Notempty };
        var descriptor = new FieldDescriptor(
            Name: "Amount",
            Type: TypeKind.Decimal,
            SlotIndex: 2,
            Modifiers: modifiers,
            DefaultExpression: "0.0",
            IsComputed: false,
            SourceLine: 5);

        descriptor.Name.Should().Be("Amount");
        descriptor.Type.Should().Be(TypeKind.Decimal);
        descriptor.SlotIndex.Should().Be(2);
        descriptor.Modifiers.Should().BeEquivalentTo(modifiers);
        descriptor.DefaultExpression.Should().Be("0.0");
        descriptor.IsComputed.Should().BeFalse();
        descriptor.SourceLine.Should().Be(5);
    }

    [Fact]
    public void FieldDescriptor_RecordEquality_HoldsForSharedListReference()
    {
        // IReadOnlyList<T> fields compare by reference; use the same cached empty array.
        var a = new FieldDescriptor("Total", TypeKind.Number, 0, Array.Empty<ModifierKind>(), null, false, 1);
        var b = new FieldDescriptor("Total", TypeKind.Number, 0, Array.Empty<ModifierKind>(), null, false, 1);
        a.Should().Be(b);
    }

    [Fact]
    public void FieldDescriptor_IsComputed_True_DistinctFromFalse()
    {
        var computed = new FieldDescriptor("TaxAmount", TypeKind.Money, 3, Array.Empty<ModifierKind>(), "Amount * 0.1", true, 10);
        var regular  = new FieldDescriptor("TaxAmount", TypeKind.Money, 3, Array.Empty<ModifierKind>(), "Amount * 0.1", false, 10);

        computed.IsComputed.Should().BeTrue();
        regular.IsComputed.Should().BeFalse();
        computed.Should().NotBe(regular);
    }

    [Fact]
    public void FieldDescriptor_NullDefaultExpression_IsAllowed()
    {
        var descriptor = new FieldDescriptor("Tags", TypeKind.String, 1, Array.Empty<ModifierKind>(), null, false, 3);
        descriptor.DefaultExpression.Should().BeNull();
    }

    // ── StateDescriptor ─────────────────────────────────────────────────────────

    [Fact]
    public void StateDescriptor_Construction_SetsAllProperties()
    {
        var modifiers = new[] { ModifierKind.InitialState };
        var descriptor = new StateDescriptor(Name: "Draft", Modifiers: modifiers, SourceLine: 12);

        descriptor.Name.Should().Be("Draft");
        descriptor.Modifiers.Should().BeEquivalentTo(modifiers);
        descriptor.SourceLine.Should().Be(12);
    }

    [Fact]
    public void StateDescriptor_RecordEquality_HoldsForSharedListReference()
    {
        // IReadOnlyList<T> fields compare by reference; use the same cached empty array.
        var modifiers = new[] { ModifierKind.Terminal };
        var a = new StateDescriptor("Approved", modifiers, 20);
        var b = new StateDescriptor("Approved", modifiers, 20);
        a.Should().Be(b);
    }

    [Fact]
    public void StateDescriptor_WithNoModifiers_IsValid()
    {
        var descriptor = new StateDescriptor("Review", Array.Empty<ModifierKind>(), 8);
        descriptor.Modifiers.Should().BeEmpty();
    }

    // ── ArgDescriptor ───────────────────────────────────────────────────────────

    [Fact]
    public void ArgDescriptor_Construction_SetsAllProperties()
    {
        var descriptor = new ArgDescriptor(
            Name: "Approver",
            Type: TypeKind.String,
            IsOptional: false,
            DefaultExpression: null,
            SourceLine: 15);

        descriptor.Name.Should().Be("Approver");
        descriptor.Type.Should().Be(TypeKind.String);
        descriptor.IsOptional.Should().BeFalse();
        descriptor.DefaultExpression.Should().BeNull();
        descriptor.SourceLine.Should().Be(15);
    }

    [Fact]
    public void ArgDescriptor_Optional_WithDefault_IsValid()
    {
        var descriptor = new ArgDescriptor("Reason", TypeKind.String, true, "\"none\"", 18);
        descriptor.IsOptional.Should().BeTrue();
        descriptor.DefaultExpression.Should().Be("\"none\"");
    }

    [Fact]
    public void ArgDescriptor_RecordEquality_HoldsForSharedListReference()
    {
        // No list fields — positional equality works directly.
        var a = new ArgDescriptor("Amount", TypeKind.Decimal, false, null, 7);
        var b = new ArgDescriptor("Amount", TypeKind.Decimal, false, null, 7);
        a.Should().Be(b);
    }

    // ── EventDescriptor ─────────────────────────────────────────────────────────

    [Fact]
    public void EventDescriptor_WithArgs_SetsAllProperties()
    {
        var args = new[]
        {
            new ArgDescriptor("Approver", TypeKind.String, false, null, 16),
            new ArgDescriptor("Amount",   TypeKind.Decimal, true, "0.0", 17),
        };
        var descriptor = new EventDescriptor(
            Name: "Approve",
            Modifiers: Array.Empty<ModifierKind>(),
            Args: args,
            SourceLine: 15);

        descriptor.Name.Should().Be("Approve");
        descriptor.Args.Should().HaveCount(2);
        descriptor.Args[0].Name.Should().Be("Approver");
        descriptor.Args[1].Name.Should().Be("Amount");
    }

    [Fact]
    public void EventDescriptor_NoArgs_IsValid()
    {
        var descriptor = new EventDescriptor("Submit", Array.Empty<ModifierKind>(), Array.Empty<ArgDescriptor>(), 9);
        descriptor.Args.Should().BeEmpty();
    }

    [Fact]
    public void EventDescriptor_RecordEquality_HoldsForSharedListReference()
    {
        // IReadOnlyList<T> fields compare by reference; use the same cached empty arrays.
        var a = new EventDescriptor("Cancel", Array.Empty<ModifierKind>(), Array.Empty<ArgDescriptor>(), 22);
        var b = new EventDescriptor("Cancel", Array.Empty<ModifierKind>(), Array.Empty<ArgDescriptor>(), 22);
        a.Should().Be(b);
    }

    [Fact]
    public void EventDescriptor_WithInitialEventModifier_IsDistinct()
    {
        var withModifier    = new EventDescriptor("Start", new[] { ModifierKind.InitialEvent }, Array.Empty<ArgDescriptor>(), 5);
        var withoutModifier = new EventDescriptor("Start", Array.Empty<ModifierKind>(), Array.Empty<ArgDescriptor>(), 5);

        withModifier.Should().NotBe(withoutModifier);
    }

    // ── FaultSiteDescriptor ─────────────────────────────────────────────────────

    [Fact]
    public void FaultSiteDescriptor_Construction_SetsAllProperties()
    {
        var descriptor = new FaultSiteDescriptor(
            FaultCode: FaultCode.DivisionByZero,
            PreventedBy: DiagnosticCode.DivisionByZero,
            SourceLine: 42);

        descriptor.FaultCode.Should().Be(FaultCode.DivisionByZero);
        descriptor.PreventedBy.Should().Be(DiagnosticCode.DivisionByZero);
        descriptor.SourceLine.Should().Be(42);
    }

    [Fact]
    public void FaultSiteDescriptor_RecordEquality_HoldsForIdenticalValues()
    {
        var a = new FaultSiteDescriptor(FaultCode.TypeMismatch, DiagnosticCode.TypeMismatch, 10);
        var b = new FaultSiteDescriptor(FaultCode.TypeMismatch, DiagnosticCode.TypeMismatch, 10);
        a.Should().Be(b);
    }

    /// <summary>
    /// For every FaultCode, a FaultSiteDescriptor constructed with PreventedBy equal to the
    /// [StaticallyPreventable] attribute's code should store that exact DiagnosticCode.
    /// This verifies that the descriptor faithfully carries the fault/diagnostic chain contract.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllFaultCodes))]
    public void FaultSiteDescriptor_PreventedBy_MatchesStaticallyPreventableAttribute(FaultCode code)
    {
        var attr = GetPreventableAttribute(code);
        attr.Should().NotBeNull($"FaultCode.{code} must have [StaticallyPreventable] to be usable in a FaultSiteDescriptor");

        var descriptor = new FaultSiteDescriptor(code, attr!.Code, 1);
        descriptor.PreventedBy.Should().Be(attr.Code,
            $"FaultSiteDescriptor.PreventedBy for {code} should match the [StaticallyPreventable] DiagnosticCode");
    }

    [Fact]
    public void FaultSiteDescriptor_DifferentSourceLines_AreNotEqual()
    {
        var a = new FaultSiteDescriptor(FaultCode.NumericOverflow, DiagnosticCode.NumericOverflow, 5);
        var b = new FaultSiteDescriptor(FaultCode.NumericOverflow, DiagnosticCode.NumericOverflow, 6);
        a.Should().NotBe(b);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    public static TheoryData<FaultCode> AllFaultCodes()
    {
        var data = new TheoryData<FaultCode>();
        foreach (var code in Enum.GetValues<FaultCode>())
            data.Add(code);
        return data;
    }

    private static StaticallyPreventableAttribute? GetPreventableAttribute(FaultCode code)
    {
        var member = typeof(FaultCode).GetField(Enum.GetName(code)!)!;
        return member.GetCustomAttribute<StaticallyPreventableAttribute>();
    }
}
