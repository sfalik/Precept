using Precept.Pipeline;

namespace Precept.Language;

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

/// <summary>
/// Thematic grouping for a diagnostic — describes WHAT the diagnostic is about,
/// complementing <see cref="DiagnosticStage"/> which describes WHEN it fires.
/// Used by the language server for filtering, documentation generation, and AI grounding.
/// </summary>
public enum DiagnosticCategory
{
    /// <summary>Name resolution — undeclared, duplicate, or out-of-scope identifiers.</summary>
    Naming,
    /// <summary>Type system — type mismatches, invalid member access, modifier violations, qualifier errors.</summary>
    TypeSystem,
    /// <summary>Temporal types — date/time format, timezone, period/duration arithmetic.</summary>
    Temporal,
    /// <summary>Business-domain types — currency, unit, dimension, price arithmetic rules.</summary>
    BusinessDomain,
    /// <summary>Structural grammar and precept-level declarations — missing initial state, circular dependencies, computed field rules.</summary>
    Structure,
    /// <summary>Runtime safety — guards required before collection access or mutation.</summary>
    Safety,
    /// <summary>Proof engine results — unsatisfiable guards, division by zero, sqrt of negative.</summary>
    Proof,
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

