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
/// TEMPORARY: F5 verification pass — compile all 30 sample files and report residual diagnostics.
/// Remove this file after F5 verification is complete.
/// </summary>
public class F5TempVerify(ITestOutputHelper output)
{
    private static string SamplesRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    public static IEnumerable<object[]> SampleFiles =>
        Directory.GetFiles(SamplesRoot, "*.precept")
                 .OrderBy(Path.GetFileName)
                 .Where(p => !string.Equals(Path.GetFileName(p), "inventory-item.precept", StringComparison.OrdinalIgnoreCase))
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
