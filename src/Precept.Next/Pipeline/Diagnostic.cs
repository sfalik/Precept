namespace Precept.Pipeline;

public enum DiagnosticStage
{
    Lex,
    Parse,
    Type,
    Graph,
    Proof
}

public enum Severity
{
    Info,
    Warning,
    Error
}

public readonly record struct Diagnostic(
    Severity        Severity,
    DiagnosticStage Stage,
    /// <summary>
    /// The string identifier for this diagnostic. Always equal to <c>nameof(<see cref="DiagnosticCode"/>.XYZ)</c>
    /// for the corresponding enum value — e.g. <c>"UnterminatedStringLiteral"</c>.
    /// Use <see cref="Diagnostics.Create"/> to construct; never set this field directly.
    /// </summary>
    string          Code,
    string          Message,
    SourceSpan      Span
);

