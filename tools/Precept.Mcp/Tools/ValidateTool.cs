using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class ValidateTool
{
    private static readonly Regex LineErrorRegex = new(@"^Line\s+(?<line>\d+)\s*:\s*(?<code>PRECEPT\d+)\s*(?:\[[^\]]+\])?\s*:\s*(?<message>.+)$", RegexOptions.Compiled);

    [McpServerTool(Name = "precept_validate")]
    [Description("Parse and compile a .precept file. Returns structured diagnostics.")]
    public static ValidateResult Run(
        [Description("Path to the .precept file")] string path)
    {
        if (!File.Exists(path))
            return new(false, null, 0, 0, [new DiagnosticDto(0, $"File not found: {path}")]);

        var text = File.ReadAllText(path);
        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(text);

        if (model is null || diagnostics.Count > 0)
        {
            return new(false, null, 0, 0,
                diagnostics.Select(d => new DiagnosticDto(d.Line, d.Message, d.Code)).ToList());
        }

        var validation = PreceptCompiler.Validate(model);
        if (validation.HasErrors)
        {
            return new(false, model.Name, 0, 0,
                validation.Diagnostics
                    .Select(d => new DiagnosticDto(d.Line, d.Message, d.DiagnosticCode))
                    .ToList());
        }

        try
        {
            PreceptCompiler.Compile(model);
            return new(true, model.Name, model.States.Count, model.Events.Count, []);
        }
        catch (Exception ex)
        {
            return new(false, model.Name, 0, 0, [ParseError(ex.Message)]);
        }
    }

    private static DiagnosticDto ParseError(string message)
    {
        var match = LineErrorRegex.Match(message);
        if (match.Success && int.TryParse(match.Groups["line"].Value, out var line))
            return new(line, match.Groups["message"].Value, match.Groups["code"].Value);
        return new(0, message);
    }
}

public sealed record ValidateResult(
    bool Valid,
    string? MachineName,
    int StateCount,
    int EventCount,
    IReadOnlyList<DiagnosticDto> Diagnostics);

public sealed record DiagnosticDto(int Line, string Message, string? Code = null);
