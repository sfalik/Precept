using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Structural guard for the typed-constant diagnostic-code path. The selection of a
/// typed-constant diagnostic code must read a typed <c>DiagnosticCode</c> carried by the
/// validator, not parse a code name out of a string. A string round-trip
/// (<c>Enum.TryParse&lt;DiagnosticCode&gt;</c>) is typo-silent — an unrecognized name falls
/// through to the generic code with no compile-time signal — so it is forbidden in this path.
/// </summary>
public class TypedConstantCodeMediationStructureTests
{
    private static string RepoRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ExpressionsSourcePath =>
        Path.Combine(RepoRoot, "src", "Precept", "Pipeline", "TypeChecker.Expressions.cs");

    [Fact]
    public void TypedConstantPath_HasNoEnumTryParseRoundTrip()
    {
        File.Exists(ExpressionsSourcePath).Should().BeTrue(
            $"the structural guard must locate the source at {ExpressionsSourcePath}");

        var source = File.ReadAllText(ExpressionsSourcePath);

        source.Should().NotContain("Enum.TryParse<DiagnosticCode>",
            "the typed-constant validator must carry a typed DiagnosticCode rather than a code name " +
            "that is parsed back via a typo-silent string round-trip");
    }
}
