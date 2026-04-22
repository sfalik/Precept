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

public readonly record struct SourceRange(
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn
);

public readonly record struct Diagnostic(
    Severity        Severity,
    DiagnosticStage Stage,
    string          Code,
    string          Message,
    SourceRange     Range
);

