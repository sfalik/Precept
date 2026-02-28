using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace StateMachine.Tests;

[CollectionDefinition("CliRendering", DisableParallelization = true)]
public sealed class CliRenderingCollectionDefinition;

[Collection("CliRendering")]
public sealed class CliRenderingTests
{
    [Fact]
    public void Fire_Enabled_Renders_Success_Marker_And_Transition()
    {
        var dsl = """
            machine Sample
            state Red
            state Green
            event Advance
            from Red on Advance
                transition Green
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Red",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": {}
            }
            """;

        var result = RunCli(dsl, instance, new[] { "fire Advance", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().MatchRegex(@"Advance\s+.+\s+Green");
    }

    [Fact]
    public void Fire_Blocked_Renders_Warning_Status_Line()
    {
        var dsl = """
            machine Sample
            state Red
            state Green
            event Advance
            data
                Vehicles: number
            from Red on Advance
                if data.Vehicles > 0
                    transition Green
                reject "No vehicles waiting"
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Red",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": { "Vehicles": 0 }
            }
            """;

        var result = RunCli(dsl, instance, new[] { "fire Advance", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("Advance");
        result.Output.Should().Contain("No vehicles waiting");
    }

    [Fact]
    public void Fire_Undefined_From_Current_State_Uses_No_Transition_Wording()
    {
        var dsl = """
            machine Sample
            state Red
            state Flashing
            event Clear
            from Flashing on Clear
                transition Red
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Red",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": {}
            }
            """;

        var result = RunCli(dsl, instance, new[] { "fire Clear", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("Clear");
        result.Output.Should().Contain("no transition from Red");
    }

    [Fact]
    public void Inspect_All_Single_Target_Does_Not_Render_Duplicate_Child_Line()
    {
        var dsl = """
            machine Sample
            state FlashingGreen
            state Green
            state FlashingRed
            event Advance
            event Emergency
                args
                    AuthorizedBy: string
                    Reason: string
            from FlashingGreen on Advance
                transition Green
            from any on Emergency
                if arg.AuthorizedBy != "" && arg.Reason != ""
                    transition FlashingRed
                reject "AuthorizedBy and Reason are required to activate emergency mode"
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "FlashingGreen",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": {}
            }
            """;

        var result = RunCli(dsl, instance, new[] { "inspect", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().MatchRegex(@"Advance\s+.+\s+Green");
        result.Output.Should().NotContain("└─ ──▷ Green");
    }

    [Fact]
    public void Inspect_All_Blocked_With_Reject_Still_Shows_Target_Child_Line()
    {
        var dsl = """
            machine Sample
            state Red
            state FlashingRed
            event Emergency
                args
                    AuthorizedBy: string
                    Reason: string
            from any on Emergency
                if arg.AuthorizedBy != "" && arg.Reason != ""
                    transition FlashingRed
                reject "AuthorizedBy and Reason are required to activate emergency mode"
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Red",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": {}
            }
            """;

        var result = RunCli(dsl, instance, new[] { "inspect", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("Emergency(AuthorizedBy,Reason)");
        result.Output.Should().Contain("AuthorizedBy and Reason a...");
        result.Output.Should().Contain("FlashingRed");
    }

    [Fact]
    public void Inspect_Multi_Target_Ambiguous_Renders_All_Preview_Child_Targets()
    {
        var dsl = """
            machine Sample
            state Source
            state Alpha
            state Beta
            state Gamma
            event Route
                args
                    Decision: string
            data
                Score: number
                Urgency: number
            from Source on Route
                if arg.Decision == "A"
                    transition Alpha
                else if data.Score > 70
                    transition Beta
                else if data.Urgency > 5
                    transition Gamma
                reject "No route"
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Source",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": { "Score": 65, "Urgency": 3 }
            }
            """;

        var result = RunCli(dsl, instance, new[] { "inspect Route", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("Alpha");
        result.Output.Should().Contain("Beta");
        result.Output.Should().Contain("Gamma");
    }

    [Fact]
    public void Inspect_Multi_Target_Resolved_Renders_Reachable_And_Unreachable_Arrows()
    {
        var dsl = """
            machine Sample
            state Paused
            state Beta
            state Gamma
            event Resume
            data
                Score: number
            from Paused on Resume
                if data.Score > 70
                    transition Beta
                else
                    transition Gamma
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Paused",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": { "Score": 90 }
            }
            """;

        var result = RunCli(dsl, instance, new[] { "inspect", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("Resume");
        result.Output.Should().Contain("Beta");
        result.Output.Should().Contain("Gamma");
    }

    [Fact]
    public void Inspect_Long_Status_Text_Is_Truncated_With_Ellipsis()
    {
        var dsl = """
            machine Sample
            state Idle
            state Processing
            event StartPipelineWithExtremelyDescriptiveNameAndMetadata
            data
                IsReady: boolean
            from Idle on StartPipelineWithExtremelyDescriptiveNameAndMetadata
                if data.IsReady
                    transition Processing
                reject "System prerequisites are not satisfied for startup and operator review is required before beginning processing with compliance artifacts and escalation routing"
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Idle",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": { "IsReady": false }
            }
            """;

        var result = RunCli(dsl, instance, new[] { "inspect", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("...");
        result.Output.Should().Contain("StartPipelineWithExtremelyDescriptiveNameAndMetadata");
    }

    [Fact]
    public void Ascii_Mode_Uses_Ascii_Symbols()
    {
        var dsl = """
            machine Sample
            state Red
            state Green
            event Advance
            from Red on Advance
                transition Green
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Red",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": {}
            }
            """;

        var result = RunCli(dsl, instance, new[] { "inspect", "exit" }, "--ascii");

        AssertSucceeded(result);
        result.Output.Should().Contain("\\- Advance --> Green");
        result.Output.Should().Contain("Red >");
    }

    [Fact]
    public void Stdin_Eof_Exits_Repl_Cleanly_Without_Exit_Command()
    {
        var dsl = """
            machine Sample
            state Red
            state Green
            event Advance
            from Red on Advance
                transition Green
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Red",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": {}
            }
            """;

        var result = RunCli(dsl, instance, new[] { "inspect" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("Advance");
        result.Output.Should().Contain("Green");
    }

    [Fact]
    public void StylePreview_Renders_Full_Compact_Scenario_Matrix()
    {
        var dsl = """
            machine Sample
            state Red
            event Advance
            from Red on Advance
                transition Red
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Red",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": {}
            }
            """;

        var result = RunCli(dsl, instance, new[] { "style preview", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("Style preview transcript (compact matrix):");
        result.Output.Should().Contain("Route(Decision)");
        result.Output.Should().Contain("Alpha");
        result.Output.Should().Contain("Beta");
        result.Output.Should().Contain("UnknownEvent");
        result.Output.Should().Contain("no transition from Red");
        result.Output.Should().Contain("AuthorizedBy: Dispatcher");
        result.Output.Should().Contain("...");
    }

    [Fact]
    public void StylePreviewAll_Renders_All_Theme_Headers_And_Matrix_Content()
    {
        var dsl = """
            machine Sample
            state Red
            event Advance
            from Red on Advance
                transition Red
            """;

        var instance = """
            {
              "workflowName": "Sample",
              "currentState": "Red",
              "lastEvent": null,
              "updatedAt": "2026-02-27T00:00:00+00:00",
              "instanceData": {}
            }
            """;

        var result = RunCli(dsl, instance, new[] { "style preview all", "exit" }, "--unicode");

        AssertSucceeded(result);
        result.Output.Should().Contain("Theme: muted");
        result.Output.Should().Contain("Theme: nord-crisp");
        result.Output.Should().Contain("Theme: tokyo-night");
        result.Output.Should().Contain("Theme: github-dark");
        result.Output.Should().Contain("Theme: solarized-modern");
        result.Output.Should().Contain("Theme: mono-accent");
        result.Output.Should().Contain("Theme: dracula");
        result.Output.Should().Contain("Theme: rose-pine");
        result.Output.Should().Contain("Theme: everforest");
        result.Output.Should().Contain("Style preview transcript (compact matrix):");
    }

    private static CliRunResult RunCli(string dsl, string instanceJson, IReadOnlyList<string> commands, string symbolFlag)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "StateMachine.CliRenderingTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var dslPath = Path.Combine(tempDir, "workflow.sm");
            var instancePath = Path.Combine(tempDir, "instance.json");
            File.WriteAllText(dslPath, dsl);
            File.WriteAllText(instancePath, instanceJson);

            var cliAssembly = ResolveCliAssemblyPath();
            var arguments = $"\"{cliAssembly}\" \"{dslPath}\" --instance \"{instancePath}\" {symbolFlag} --no-color";
            var psi = new ProcessStartInfo("dotnet", arguments)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ResolveRepositoryRoot()
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start CLI process.");

            foreach (var command in commands)
                process.StandardInput.WriteLine(command);

            process.StandardInput.Close();

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            process.WaitForExit(120000);

            return new CliRunResult(process.ExitCode, NormalizeNewLines(stdout + stderr));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string ResolveCliAssemblyPath()
    {
        var repoRoot = ResolveRepositoryRoot();
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "StateMachine.Dsl.Cli.dll"),
            Path.Combine(repoRoot, "tools", "StateMachine.Dsl.Cli", "bin", "Debug", "net10.0", "StateMachine.Dsl.Cli.dll"),
            Path.Combine(repoRoot, "tools", "StateMachine.Dsl.Cli", "bin", "Release", "net10.0", "StateMachine.Dsl.Cli.dll")
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
            throw new FileNotFoundException("Could not locate StateMachine.Dsl.Cli.dll.", string.Join(Environment.NewLine, candidates));

        return path;
    }

    private static string ResolveRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var marker = Path.Combine(dir.FullName, "StateMachine.slnx");
            if (File.Exists(marker))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing StateMachine.slnx.");
    }

    private static string NormalizeNewLines(string value)
        => value.Replace("\r\n", "\n");

    private static void AssertSucceeded(CliRunResult result)
        => result.ExitCode.Should().Be(0, result.Output);

    private sealed record CliRunResult(int ExitCode, string Output);
}
