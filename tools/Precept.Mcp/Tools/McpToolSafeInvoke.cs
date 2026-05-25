using Precept.Language;
using Precept.Mcp.Dtos;
using Precept.Pipeline;

namespace Precept.Mcp.Tools;

/// <summary>
/// Backstop wrapper for MCP tool entry points. Any unhandled exception from
/// the tool body is translated into a structured error response so consumers
/// never see the bare ModelContextProtocol "An error occurred invoking '{tool}'."
/// fallback. See Phase 2 Step 2.2a of the Compiler Readiness Plan.
/// </summary>
/// <remarks>
/// For tools that return a domain DTO (currently <see cref="CompileTool"/>),
/// the wrapper synthesizes a <see cref="CompileResultDto"/> carrying a single
/// <see cref="DiagnosticCode.McpToolInternalError"/> diagnostic so downstream
/// consumers see a structured failure shape, not an empty result.
///
/// For formatter tools that return <see cref="string"/>, the wrapper returns
/// a structured error string of the form:
/// <c>Error invoking '{toolName}': {ExceptionType}: {Message}</c>.
/// </remarks>
internal static class McpToolSafeInvoke
{
    /// <summary>
    /// Wraps a formatter-tool body that returns a markdown string.
    /// </summary>
    public static string Invoke(string toolName, Func<string> body)
    {
        try
        {
            return body();
        }
        catch (Exception ex)
        {
            return FormatStringErrorPayload(toolName, ex);
        }
    }

    /// <summary>
    /// Wraps the <see cref="CompileTool"/> body that returns a <see cref="CompileResultDto"/>.
    /// </summary>
    public static CompileResultDto Invoke(string toolName, Func<CompileResultDto> body)
    {
        try
        {
            return body();
        }
        catch (Exception ex)
        {
            return BuildCompileErrorPayload(toolName, ex);
        }
    }

    private static string FormatStringErrorPayload(string toolName, Exception ex)
        => $"Error invoking '{toolName}': {ex.GetType().Name}: {ex.Message}";

    private static CompileResultDto BuildCompileErrorPayload(string toolName, Exception ex)
    {
        var diagnostic = Diagnostics.Create(
            DiagnosticCode.McpToolInternalError,
            SourceSpan.Missing,
            toolName,
            ex.GetType().Name,
            ex.Message);

        var dto = new CompileDiagnosticDto(
            diagnostic.Span.StartLine,
            diagnostic.Span.StartColumn,
            "error",
            FormatDiagnosticCode(diagnostic),
            diagnostic.Message);

        var summary = $"Internal error in MCP tool '{toolName}': {ex.GetType().Name}";
        return new CompileResultDto(
            Success: false,
            DiagnosticCount: 1,
            Diagnostics: [dto],
            Summary: summary,
            ProofObligations: [],
            EventHandlers: []);
    }

    private static string FormatDiagnosticCode(Diagnostic diagnostic)
        => diagnostic.Code.StartsWith("PRE", StringComparison.Ordinal)
            ? diagnostic.Code
            : Enum.TryParse<DiagnosticCode>(diagnostic.Code, out var code)
                ? $"PRE{(int)code:D4}"
                : diagnostic.Code;
}
