using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Drift defense for diagnostic sample files.
/// Ensures that sample .precept files exist, parse cleanly, and that
/// proof-family diagnostics (C94–C98) have exercising samples once implemented.
///
/// Dependency checklist — which samples need which codes:
///   C94 (assignment constraint violations) → sample demonstrating assignment-time proof violations
///   C95 (contradictory rule detection)     → sample with mutually exclusive rules the engine flags
///   C97 (vacuous rules)                    → sample with always-true rules the engine flags
///   C98 (dead/tautological guards)         → sample with guards that are always true or always false
///
/// Cross-surface consistency (D9):
///   At least one sample (computed-tax-net.precept when it exists) must show consistent
///   proof data across hover, diagnostics, and MCP precept_compile output.
///   D9 verification depends on Steps 13 + 14 being complete.
///
/// See temp/specs/step-15.md for the full contract and verification gates.
/// </summary>
public class DiagnosticSampleDriftTests
{
    // ════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Precept.slnx")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find repo root (Precept.slnx)");
    }

    private static string SamplesDir => Path.Combine(FindRepoRoot(), "samples");

    private static string[] GetSampleFiles() =>
        Directory.GetFiles(SamplesDir, "*.precept");

    // ════════════════════════════════════════════════════════════════════
    // Test 1: Sample file count guard
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void SampleDirectory_ContainsExpectedFileCount()
    {
        var files = GetSampleFiles();
        files.Length.Should().BeGreaterThanOrEqualTo(25,
            "samples/ should contain at least 25 .precept files (will grow as proof samples land)");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 2: All sample files parse without errors
    // ════════════════════════════════════════════════════════════════════

    public static TheoryData<string> AllSampleFiles()
    {
        var data = new TheoryData<string>();
        var samplesDir = Path.Combine(FindRepoRoot(), "samples");
        foreach (var file in Directory.GetFiles(samplesDir, "*.precept"))
            data.Add(Path.GetFileName(file));
        return data;
    }

    [Theory]
    [MemberData(nameof(AllSampleFiles))]
    public void SampleFile_ParsesWithoutErrors(string fileName)
    {
        var filePath = Path.Combine(SamplesDir, fileName);
        var dsl = File.ReadAllText(filePath);

        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);

        diags.Should().BeEmpty(
            $"sample file '{fileName}' must parse without errors");
        model.Should().NotBeNull(
            $"sample file '{fileName}' must produce a valid model");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 3: All sample files compile without errors
    // ════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(AllSampleFiles))]
    public void SampleFile_CompilesWithoutErrors(string fileName)
    {
        var filePath = Path.Combine(SamplesDir, fileName);
        var dsl = File.ReadAllText(filePath);

        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);

        var errors = validation.Diagnostics
            .Where(d => d.Constraint.Severity == ConstraintSeverity.Error)
            .ToList();
        errors.Should().BeEmpty(
            $"sample file '{fileName}' must compile without errors (warnings OK)");
    }

    // ════════════════════════════════════════════════════════════════════
    // Proof-family diagnostic samples — C94, C95, C97, C98
    //
    // These tests are scaffolded with Skip because the diagnostic codes
    // do not exist yet. They will be implemented in Commit 12
    // (proof-family diagnostics). Once C94–C98 land, remove the Skip
    // attributes and fill in the sample file paths and assertions.
    // ════════════════════════════════════════════════════════════════════

    [Fact(Skip = "Requires C94 implementation — Commit 12")]
    public void C94_AssignmentConstraintViolation_HasExercisingSample()
    {
        // PLAN: Verify that at least one sample file triggers C94
        // (assignment-time constraint violation detected by proof engine).
        // The sample should demonstrate a literal assignment that the engine
        // can statically prove violates a field constraint.
        AssertDiagnosticExercisedBySample("C94");
    }

    [Fact(Skip = "Requires C95 implementation — Commit 12")]
    public void C95_ContradictoryRuleDetection_HasExercisingSample()
    {
        // PLAN: Verify that at least one sample file triggers C95
        // (contradictory rules — two rules that cannot both be satisfied).
        // The sample should have rules like `rule X > 10` and `rule X < 5`.
        AssertDiagnosticExercisedBySample("C95");
    }

    [Fact(Skip = "Requires C97 implementation — Commit 12")]
    public void C97_VacuousRule_HasExercisingSample()
    {
        // PLAN: Verify that at least one sample file triggers C97
        // (vacuous rule — a rule that is always true given the constraints).
        // The sample should have a rule like `rule X >= 0` on a field with
        // `nonnegative` constraint, making the rule vacuously true.
        AssertDiagnosticExercisedBySample("C97");
    }

    [Fact(Skip = "Requires C98 implementation — Commit 12")]
    public void C98_DeadOrTautologicalGuard_HasExercisingSample()
    {
        // PLAN: Verify that at least one sample file triggers C98
        // (dead or tautological guard — a guard whose condition is always
        // true or always false given the proof context).
        AssertDiagnosticExercisedBySample("C98");
    }

    /// <summary>
    /// Shared assertion: at least one sample file must produce a diagnostic
    /// with the given constraint ID when compiled.
    /// </summary>
    private static void AssertDiagnosticExercisedBySample(string constraintId)
    {
        var sampleFiles = GetSampleFiles();
        var found = false;

        foreach (var filePath in sampleFiles)
        {
            var dsl = File.ReadAllText(filePath);
            var model = PreceptParser.Parse(dsl);
            var validation = PreceptCompiler.Validate(model);

            if (validation.Diagnostics.Any(d => d.Constraint.Id == constraintId))
            {
                found = true;
                break;
            }
        }

        found.Should().BeTrue(
            $"at least one sample file in samples/ should exercise diagnostic {constraintId}");
    }

    // ════════════════════════════════════════════════════════════════════
    // Computed-field proof samples — gap coverage (Step 15 spec §2–§4)
    //
    // These verify that the new proof-demonstrating samples exist and
    // compile cleanly once created. Skipped until the samples land.
    // ════════════════════════════════════════════════════════════════════

    [Fact(Skip = "Requires computed-tax-net.precept — Step 15 sample creation")]
    public void ComputedTaxNetSample_ExistsAndCompiles()
    {
        // PLAN: computed-tax-net.precept demonstrates computed-field intermediary
        // proofs (gap 3). Fields like Subtotal, TaxRate, Tax = Subtotal * TaxRate,
        // Net = Subtotal - Tax, where the engine proves Net > 0.
        AssertSampleExistsAndCompiles("computed-tax-net.precept");
    }

    [Fact(Skip = "Requires transitive-ordering.precept — Step 15 sample creation")]
    public void TransitiveOrderingSample_ExistsAndCompiles()
    {
        // PLAN: transitive-ordering.precept demonstrates transitive chain proofs
        // (gap 4). Rules like A > B, B > C → engine proves A > C.
        AssertSampleExistsAndCompiles("transitive-ordering.precept");
    }

    private static void AssertSampleExistsAndCompiles(string fileName)
    {
        var filePath = Path.Combine(SamplesDir, fileName);
        File.Exists(filePath).Should().BeTrue(
            $"sample file '{fileName}' must exist in samples/");

        var dsl = File.ReadAllText(filePath);
        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);

        var errors = validation.Diagnostics
            .Where(d => d.Constraint.Severity == ConstraintSeverity.Error)
            .ToList();
        errors.Should().BeEmpty(
            $"sample file '{fileName}' must compile without errors");
    }
}
