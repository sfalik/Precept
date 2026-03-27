using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class ValidateTool
{
    [McpServerTool(Name = "precept_validate")]
    [Description("Parse and compile a .precept file. Returns structured diagnostics.")]
    public static ValidateResult Run(
        [Description("Path to the .precept file")] string path)
    {
        if (!File.Exists(path))
            return new(false, null, 0, 0, [new DiagnosticDto(0, $"File not found: {path}", null, "error")]);

        var result = PreceptCompiler.CompileFromText(File.ReadAllText(path));
        var model = result.Model;

        return new(
            !result.HasErrors,
            model?.Name,
            model?.States.Count ?? 0,
            model?.Events.Count ?? 0,
            result.Diagnostics
                .Select(d => new DiagnosticDto(d.Line, d.Message, d.Code, d.Severity.ToString().ToLowerInvariant()))
                .ToList());
    }
}

public sealed record ValidateResult(
    bool Valid,
    string? MachineName,
    int StateCount,
    int EventCount,
    IReadOnlyList<DiagnosticDto> Diagnostics);

public sealed record DiagnosticDto(int Line, string Message, string? Code = null, string? Severity = null);
