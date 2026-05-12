namespace Precept.Mcp.Dtos;

public sealed record CompileResultDto(
    bool Success,
    int DiagnosticCount,
    CompileDiagnosticDto[] Diagnostics,
    string Summary);

public sealed record CompileDiagnosticDto(
    int Line,
    int Column,
    string Severity,
    string Code,
    string Message);