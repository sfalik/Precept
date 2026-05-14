namespace Precept.Mcp.Dtos;

public sealed record CompileResultDto(
    bool Success,
    int DiagnosticCount,
    CompileDiagnosticDto[] Diagnostics,
    string Summary,
    CompileProofObligationDto[] ProofObligations);

public sealed record CompileDiagnosticDto(
    int Line,
    int Column,
    string Severity,
    string Code,
    string Message);

public sealed record CompileProofObligationDto(
    string Kind,
    string Disposition,
    string? Strategy,
    string? EmittedDiagnostic,
    string Description,
    string? ComputedInterval,
    string? TargetField,
    decimal? DeclaredMin,
    decimal? DeclaredMax);
