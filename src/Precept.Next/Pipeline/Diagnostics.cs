namespace Precept.Pipeline;

public sealed record DiagnosticMeta(
    string          Code,
    DiagnosticStage Stage,
    Severity        Severity,
    string          MessageTemplate
);

public static class Diagnostics
{
    public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
    {
        DiagnosticCode.UnterminatedStringLiteral      => new(nameof(DiagnosticCode.UnterminatedStringLiteral),      DiagnosticStage.Lex,   Severity.Error,   "Unterminated string literal"),
        DiagnosticCode.InvalidCharacter               => new(nameof(DiagnosticCode.InvalidCharacter),               DiagnosticStage.Lex,   Severity.Error,   "Invalid character '{0}'"),
        DiagnosticCode.ExpectedToken                  => new(nameof(DiagnosticCode.ExpectedToken),                  DiagnosticStage.Parse, Severity.Error,   "Expected '{0}' but found '{1}'"),
        DiagnosticCode.UnexpectedKeyword              => new(nameof(DiagnosticCode.UnexpectedKeyword),              DiagnosticStage.Parse, Severity.Error,   "Unexpected keyword '{0}' inside {1} block"),
        DiagnosticCode.UndeclaredField                => new(nameof(DiagnosticCode.UndeclaredField),                DiagnosticStage.Type,  Severity.Error,   "Field '{0}' is not declared"),
        DiagnosticCode.TypeMismatch                   => new(nameof(DiagnosticCode.TypeMismatch),                   DiagnosticStage.Type,  Severity.Error,   "Type mismatch: expected '{0}', got '{1}'"),
        DiagnosticCode.NullInNonNullableContext       => new(nameof(DiagnosticCode.NullInNonNullableContext),       DiagnosticStage.Type,  Severity.Error,   "Null value used where non-nullable '{0}' is required"),
        DiagnosticCode.InvalidMemberAccess            => new(nameof(DiagnosticCode.InvalidMemberAccess),            DiagnosticStage.Type,  Severity.Error,   "Member accessor '{0}' is not supported on type '{1}'"),
        DiagnosticCode.FunctionArityMismatch          => new(nameof(DiagnosticCode.FunctionArityMismatch),          DiagnosticStage.Type,  Severity.Error,   "Function '{0}' expects {1} arguments, got {2}"),
        DiagnosticCode.FunctionArgConstraintViolation => new(nameof(DiagnosticCode.FunctionArgConstraintViolation), DiagnosticStage.Type,  Severity.Error,   "Argument {0} to '{1}' violates constraint: {2}"),
        DiagnosticCode.UnreachableState               => new(nameof(DiagnosticCode.UnreachableState),               DiagnosticStage.Graph, Severity.Warning, "State '{0}' is unreachable from initial state '{1}'"),
        DiagnosticCode.UnhandledEvent                 => new(nameof(DiagnosticCode.UnhandledEvent),                 DiagnosticStage.Graph, Severity.Warning, "No transition handles event '{0}' in state '{1}'"),
        DiagnosticCode.UnsatisfiableGuard             => new(nameof(DiagnosticCode.UnsatisfiableGuard),             DiagnosticStage.Proof, Severity.Warning, "Guard '{0}' on event '{1}' is provably unsatisfiable when {2}"),
        DiagnosticCode.DivisionByZero                 => new(nameof(DiagnosticCode.DivisionByZero),                 DiagnosticStage.Proof, Severity.Error,   "Division by zero: '{0}' can be zero when {1}"),
        DiagnosticCode.SqrtOfNegative                 => new(nameof(DiagnosticCode.SqrtOfNegative),                 DiagnosticStage.Proof, Severity.Error,   "sqrt() operand '{0}' can be negative when {1}"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static Diagnostic Create(DiagnosticCode code, SourceRange range, params object?[] args)
    {
        var meta = GetMeta(code);
        return new(meta.Severity, meta.Stage, meta.Code, string.Format(meta.MessageTemplate, args), range);
    }

    public static IReadOnlyList<DiagnosticMeta> All { get; } =
        Enum.GetValues<DiagnosticCode>().Select(GetMeta).ToList();
}
