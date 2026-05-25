using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;
using Xunit.Abstractions;

namespace Precept.Tests;

/// <summary>
/// Compile every <c>samples/*.precept</c> file and assert zero diagnostics.
/// Strict full-clean guarantee: any diagnostic (warning or error) on any sample
/// fails the test.
///
/// Complementary to <see cref="SampleFieldStateRegressionTests"/>, which checks
/// only the four field-state-guarantee codes (D130/D131/D132/D143). This test
/// covers the entire diagnostic surface.
/// </summary>
public class SampleCompilesCleanTests(ITestOutputHelper output)
{
    // Minimum sample count to detect a silent corpus collapse — if the artifacts-path
    // layout changes and SamplesRoot resolves wrong, Directory.GetFiles returns zero
    // and a Theory with zero rows passes vacuously. Bump this when the corpus grows
    // substantially; current corpus is 75 samples.
    private const int MinimumExpectedSampleCount = 50;

    private static string SamplesRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    public static IEnumerable<object[]> SampleFiles =>
        Directory.GetFiles(SamplesRoot, "*.precept")
                 .OrderBy(Path.GetFileName)
                 .Select(p => new object[] { Path.GetFileName(p), p });

    [Fact]
    public void SampleCorpus_DiscoveryFindsExpectedSampleCount()
    {
        // Guard against a silent failure mode where SamplesRoot resolves wrong and
        // the corpus appears empty. If this fails, fix the relative-path resolution
        // before trusting any Theory result.
        var sampleCount = Directory.GetFiles(SamplesRoot, "*.precept").Length;
        sampleCount.Should().BeGreaterThanOrEqualTo(
            MinimumExpectedSampleCount,
            $"SampleCompilesCleanTests must discover at least {MinimumExpectedSampleCount} samples in {SamplesRoot}");
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public void Sample_CompilesClean(string name, string path)
    {
        var text = File.ReadAllText(path);
        var compilation = Compiler.Compile(text);
        foreach (var d in compilation.Diagnostics.OrderBy(d => d.Code))
            output.WriteLine($"  L{d.Span.StartLine} {d.Code}: {d.Message}");
        compilation.Diagnostics.Should().BeEmpty($"{name} should compile with zero diagnostics");
    }
}
