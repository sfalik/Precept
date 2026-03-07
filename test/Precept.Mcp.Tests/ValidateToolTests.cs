using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class ValidateToolTests
{
    private static string SamplesDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    private static string SamplePath(string fileName) => Path.Combine(SamplesDir, fileName);

    [Fact]
    public void ValidFile_ReturnsSuccess()
    {
        var result = ValidateTool.Run(SamplePath("bugtracker.precept"));

        result.Valid.Should().BeTrue();
        result.MachineName.Should().Be("BugTracker");
        result.StateCount.Should().BeGreaterThan(0);
        result.EventCount.Should().BeGreaterThan(0);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void SyntaxError_ReturnsInvalid()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "precept Test\nfield as number\n");
            var result = ValidateTool.Run(tempFile);

            result.Valid.Should().BeFalse();
            result.Diagnostics.Should().NotBeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompileTimeViolation_ReturnsInvalid()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            // Priority default 0 violates invariant Priority >= 1
            File.WriteAllText(tempFile, """
                precept Test
                field Priority as number default 0
                invariant Priority >= 1 because "Must be positive"
                state Open initial
                """);
            var result = ValidateTool.Run(tempFile);

            result.Valid.Should().BeFalse();
            result.Diagnostics.Should().NotBeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void MissingFile_ReturnsGracefulError()
    {
        var result = ValidateTool.Run(@"C:\nonexistent\file.precept");

        result.Valid.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Message.Should().Contain("File not found");
    }

    [Fact]
    public void EmptyFile_ReturnsInvalid()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "");
            var result = ValidateTool.Run(tempFile);

            result.Valid.Should().BeFalse();
            result.Diagnostics.Should().NotBeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
