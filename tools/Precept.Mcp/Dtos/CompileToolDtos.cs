namespace Precept.Mcp.Dtos;

public sealed record CompileResultDto(
    bool Success,
    int DiagnosticCount,
    CompileDiagnosticDto[] Diagnostics,
    string Summary,
    CompileProofObligationDto[] ProofObligations,
    CompileEventRowDto[] EventHandlers);

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
    decimal? DeclaredMax,
    decimal? NormalizedDeclaredMin,
    decimal? NormalizedDeclaredMax);

/// <summary>
/// Compact DTO for a single event handler row in <c>precept_compile</c> output.
/// </summary>
/// <param name="EventName">The name of the event this row handles.</param>
/// <param name="IsConstruction">
/// <c>true</c> when this row is a construction row — its event is declared <c>initial</c>
/// and the row runs when the entity is first created.  <c>false</c> for regular event rows.
/// </param>
public sealed record CompileEventRowDto(
    string EventName,
    bool IsConstruction);
