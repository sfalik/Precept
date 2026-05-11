using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.NameBinder;

/// <summary>
/// Tests for the NameBinder's topological sort of computed fields and cycle detection,
/// and for the TypeChecker's <c>DefaultForwardReference</c> enforcement on non-computed
/// field defaults.
///
/// Root causes addressed:
///   BUG-030: Computed field forward references rejected — name binder processes fields
///            in declaration order, no topological sort.
/// Related diagnostic codes: <c>CircularComputedField</c>, <c>DefaultForwardReference</c>.
/// </summary>
public sealed class ForwardReferenceTests
{
    // ── Topological sort: computed fields may freely forward-reference ────────────

    [Fact]
    public void ComputedField_SingleForwardReference_CompilesCleanlly()
    {
        // A <- B, B declared after A: topological sort resolves B first
        var compilation = Compile("""
            precept ForwardRef
            field A as number <- B + 1
            field B as number default 0
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "computed fields may reference fields declared later; topological sort resolves them");
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void ComputedField_ChainForwardReference_CompilesCleanly()
    {
        // A <- B + 1, B <- C + 1, C declared last: topological order is C, B, A
        var compilation = Compile("""
            precept ChainForward
            field A as number <- B + 1
            field B as number <- C + 1
            field C as number default 0
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "a chain of forward-referencing computed fields must resolve via topological sort");
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.CircularComputedField));
    }

    [Fact]
    public void ComputedField_BackwardReference_AlsoCompilesCleanly()
    {
        // Standard case: fields declared before their dependents
        var compilation = Compile("""
            precept BackwardRef
            field C as number default 0
            field B as number <- C + 1
            field A as number <- B + 1
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.CircularComputedField));
    }

    [Fact]
    public void ComputedField_MultipleInputsForwardReference_CompilesCleanly()
    {
        // Total <- Price * Quantity, both Price and Quantity declared after Total
        var compilation = Compile("""
            precept MultiForward
            field Total as number <- Price * Quantity
            field Price as number default 0 writable
            field Quantity as integer default 1 writable
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "a computed field may forward-reference multiple fields; topological sort resolves all");
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void ComputedField_DiamondDependency_CompilesCleanly()
    {
        // A <- B + C, B <- D, C <- D, D declared last: diamond-shaped dependency
        var compilation = Compile("""
            precept Diamond
            field A as number <- B + C
            field B as number <- D + 1
            field C as number <- D + 2
            field D as number default 0
            """);

        compilation.HasErrors.Should().BeFalse(
            because: "diamond-shaped computed field dependencies must resolve without errors");
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.CircularComputedField));
    }

    // ── Cycle detection ───────────────────────────────────────────────────────────

    [Fact]
    public void ComputedField_DirectCycle_EmitsCircularComputedField()
    {
        // A <- B + 1, B <- A + 1: direct cycle
        var compilation = Compile("""
            precept DirectCycle
            field A as number <- B + 1
            field B as number <- A + 1
            """);

        compilation.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.CircularComputedField),
            because: "A <- B + 1 and B <- A + 1 form a direct cycle that must be detected");
    }

    [Fact]
    public void ComputedField_SelfReference_EmitsCircularComputedField()
    {
        // A <- A + 1: self-reference is a trivial cycle
        var compilation = Compile("""
            precept SelfRef
            field A as number <- A + 1
            """);

        compilation.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.CircularComputedField),
            because: "a field that references itself is a trivial cycle and must emit CircularComputedField");
    }

    [Fact]
    public void ComputedField_IndirectCycle_EmitsCircularComputedField()
    {
        // A <- B + 1, B <- C + 1, C <- A + 1: indirect 3-node cycle
        var compilation = Compile("""
            precept IndirectCycle
            field A as number <- B + 1
            field B as number <- C + 1
            field C as number <- A + 1
            """);

        compilation.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.CircularComputedField),
            because: "an indirect 3-node cycle must be detected by the topological sort cycle check");
    }

    [Fact]
    public void ComputedField_CyclicMessage_ContainsAllCycleMembers()
    {
        var compilation = Compile("""
            precept CyclicMessage
            field X as number <- Y + 1
            field Y as number <- X + 1
            """);

        var cycleDiag = compilation.Diagnostics.FirstOrDefault(d => d.Code == nameof(DiagnosticCode.CircularComputedField));
        cycleDiag.Should().NotBeNull();
        cycleDiag!.Message.Should().Contain("X", because: "cycle message must name field X");
        cycleDiag.Message.Should().Contain("Y", because: "cycle message must name field Y");
    }

    // ── DefaultForwardReference: non-computed defaults are scope-restricted ───────

    [Fact]
    public void DefaultField_ReferencesLaterField_EmitsDefaultForwardReference()
    {
        // Non-computed 'default' expression must not reference a field declared later
        var compilation = Compile("""
            precept DefaultFwdRef
            field Y as number default X
            field X as number default 0
            """);

        compilation.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DefaultForwardReference),
            because: "'default X' in the Y declaration references X which is declared later — " +
                     "non-computed defaults are restricted to PriorFieldsOnly scope");
    }

    [Fact]
    public void DefaultField_ReferencesPriorField_CompilesCleanly()
    {
        // Non-computed default that references an earlier field is fine
        var compilation = Compile("""
            precept DefaultPriorRef
            field X as number default 0
            field Y as number default X
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DefaultForwardReference),
            because: "'default X' in Y references X declared before Y — this is valid");
    }

    [Fact]
    public void ComputedField_ForwardRef_DoesNotEmitDefaultForwardReference()
    {
        // Computed fields (using '<-') are not subject to the PriorFieldsOnly restriction
        var compilation = Compile("""
            precept ComputedVsDefault
            field Total as number <- Subtotal + 1
            field Subtotal as number default 0
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DefaultForwardReference),
            because: "computed expressions use topological sort, not PriorFieldsOnly scope");
        compilation.HasErrors.Should().BeFalse();
    }

    private static Compilation Compile(string source) => Compiler.Compile(source);
}
