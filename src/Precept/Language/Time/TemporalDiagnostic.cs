namespace Precept.Language;

public sealed record TemporalDiagnostic(
    string Code,
    string Message,
    string? Suggestion,
    TypedConstantErrorKind ErrorKind = TypedConstantErrorKind.Format);
