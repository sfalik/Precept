using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class SampleFieldStateRegressionTests
{
    private static readonly string[] FieldStateGuaranteeCodes =
    [
        nameof(DiagnosticCode.OmittedFieldReadInState),
        nameof(DiagnosticCode.OmittedFieldSetInTargetState),
        nameof(DiagnosticCode.RequiredFieldUnassignedOnEntry),
        nameof(DiagnosticCode.MaterializedFieldSelfReference)
    ];

    private static string SamplesRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    public static IEnumerable<object[]> SampleFiles =>
        Directory.GetFiles(SamplesRoot, "*.precept")
            .OrderBy(Path.GetFileName)
            .Select(path => new object[] { Path.GetFileName(path)!, path });

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public void Samples_AllSampleFiles_NoUnexpected_D130_D131_D132_D143(string name, string path)
    {
        var result = Compiler.Compile(File.ReadAllText(path));

        result.Diagnostics
            .Where(d => FieldStateGuaranteeCodes.Contains(d.Code))
            .Should().BeEmpty($"{name} should not emit D130, D131, D132, or D143 diagnostics");
    }

    [Fact]
    public void Samples_InsuranceClaim_CompilesClean()
    {
        var result = Compiler.Compile(File.ReadAllText(Path.Combine(SamplesRoot, "insurance-claim.precept")));

        result.Diagnostics
            .Where(d => FieldStateGuaranteeCodes.Contains(d.Code))
            .Should().BeEmpty("insurance-claim.precept should not emit D130, D131, D132, or D143 diagnostics");
    }
}
