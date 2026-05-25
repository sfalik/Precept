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
    private static string SamplesRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    public static IEnumerable<object[]> SampleFiles =>
        Directory.GetFiles(SamplesRoot, "*.precept")
                 .OrderBy(Path.GetFileName)
                 .Select(p => new object[] { Path.GetFileName(p), p });

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
