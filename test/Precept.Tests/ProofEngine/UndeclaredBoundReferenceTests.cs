using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// A field reference used as a `min`/`max` modifier value is name-resolved like any
/// rule-condition reference (spec §3.5): if it resolves to no declared field it is an
/// undeclared-name error (UndeclaredField, binder-owned per diagnostic-system.md) rather
/// than being silently dropped. A resolved reference does not error here (enforcement of
/// field-reference bounds in proof is a later slice). Uses <see cref="Compiler.Compile"/>
/// (full pipeline) — TypeChecker-only helpers skip the binder/proof stages.
/// </summary>
public class UndeclaredBoundReferenceTests
{
    private static System.Collections.Immutable.ImmutableArray<Diagnostic> Compile(string source)
        => Compiler.Compile(source).Diagnostics;

    [Fact]
    public void ModifierValue_UndeclaredFieldReference_Errors()
    {
        var diagnostics = Compile("""
            precept T
            field Amount as integer min Floor default 0
            """);

        diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UndeclaredField),
            because: "'Floor' is not declared; a min/max value that resolves to no field must be an undeclared-name error, not silently dropped");
    }

    [Fact]
    public void ModifierValue_ResolvedFieldReference_NoUndeclaredError()
    {
        var diagnostics = Compile("""
            precept T
            field Floor as integer min 0 max 50 default 10
            field Amount as integer min Floor default 20
            """);

        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField),
            because: "'Floor' resolves to a declared field; it may be unenforced for now, but must not raise an undeclared-name error");
    }
}
