namespace Precept.Language;

public sealed record UcumDiagnostic(string Code, string Message, int Start, int Length, string? Suggestion);
