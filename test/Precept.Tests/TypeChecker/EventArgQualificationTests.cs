using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Event arguments are accessed only via dotted notation (<c>EventName.ArgName</c>,
/// spec §3.5). A bare identifier names a field; it never resolves to an event arg.
/// A bare reference to an in-scope arg is rejected with
/// <see cref="DiagnosticCode.UnqualifiedEventArgReference"/> (PRE0163); a bare name
/// that collides with a same-named field resolves to the field with no diagnostic.
/// Uses <see cref="Compiler.Compile"/> (full pipeline) — the resolution path under
/// test is the type checker's, which TypeChecker-only helpers also exercise, but the
/// full pipeline confirms end-to-end behavior on real syntax.
/// </summary>
public class EventArgQualificationTests
{
    private static ImmutableArray<Diagnostic> Compile(string source)
        => Compiler.Compile(source).Diagnostics;

    private static bool Has(ImmutableArray<Diagnostic> diagnostics, DiagnosticCode code)
        => diagnostics.Any(d => d.Code == code.ToString());

    [Fact]
    public void BareInScopeArg_NoSameNameField_EmitsUnqualified()
    {
        // 'Amount' is an in-scope arg of Record and no field is named Amount.
        // Bare 'Amount' must be written 'Record.Amount' → PRE0163.
        var diagnostics = Compile("""
            precept Demo
            field Total as integer default 0
            state Open initial
            state Done terminal
            event Record(Amount as integer)
            from Open on Record
                -> set Total = Amount
                -> transition Done
            """);

        Has(diagnostics, DiagnosticCode.UnqualifiedEventArgReference).Should().BeTrue(
            because: "a bare in-scope event-arg reference must be qualified as Record.Amount");
    }

    [Fact]
    public void DottedInScopeArg_Clean()
    {
        // The qualified form is the supported access path — no diagnostic.
        var diagnostics = Compile("""
            precept Demo
            field Total as integer default 0
            state Open initial
            state Done terminal
            event Record(Amount as integer)
            from Open on Record
                -> set Total = Record.Amount
                -> transition Done
            """);

        Has(diagnostics, DiagnosticCode.UnqualifiedEventArgReference).Should().BeFalse(
            because: "Record.Amount is the supported dotted access form");
    }

    [Fact]
    public void BareName_FieldAndArgCollision_ResolvesToField()
    {
        // Field Amount (integer) and arg Amount (string) share a name. Under dotted-only
        // access a bare 'Amount' is the field (integer), so 'set Mirror = Amount' assigns
        // integer→integer cleanly. If the bare name had bound to the string arg instead,
        // this would be a TypeMismatch — its absence proves the field won.
        var diagnostics = Compile("""
            precept Demo
            field Amount as integer default 0
            field Mirror as integer default 0
            state Open initial
            state Done terminal
            event Record(Amount as string)
            from Open on Record
                -> set Mirror = Amount
                -> transition Done
            """);

        Has(diagnostics, DiagnosticCode.UnqualifiedEventArgReference).Should().BeFalse(
            because: "the bare name resolved to the field, so there is nothing to qualify");
        Has(diagnostics, DiagnosticCode.TypeMismatch).Should().BeFalse(
            because: "the bare name bound to field Amount (integer), assigned to Mirror (integer)");
    }

    [Fact]
    public void BareOutOfScopeArg_StillEmitsOutOfScope()
    {
        // 'Amount' belongs to Record, but the row handles Other — the arg is out of scope.
        // This stays PRE0050 (EventArgOutOfScope), not PRE0163: regression guard that the
        // dotted-only change did not collapse the two distinct failure modes.
        var diagnostics = Compile("""
            precept Demo
            field Total as integer default 0
            state Open initial
            state Done terminal
            event Record(Amount as integer)
            event Other
            from Open on Other
                -> set Total = Amount
                -> transition Done
            """);

        Has(diagnostics, DiagnosticCode.EventArgOutOfScope).Should().BeTrue(
            because: "Amount is not in scope under the Other handler — distinct from a bare in-scope reference");
        Has(diagnostics, DiagnosticCode.UnqualifiedEventArgReference).Should().BeFalse(
            because: "an out-of-scope arg is PRE0050, not PRE0163");
    }
}
