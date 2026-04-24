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
        DiagnosticCode.InputTooLarge                 => new(nameof(DiagnosticCode.InputTooLarge),                 DiagnosticStage.Lex,   Severity.Error,   "This definition exceeds the 65,536-character security limit and cannot be processed"),
        DiagnosticCode.UnterminatedStringLiteral      => new(nameof(DiagnosticCode.UnterminatedStringLiteral),      DiagnosticStage.Lex,   Severity.Error,   "Text value opened with \" is missing its closing quote — every \" must have a matching \""),
        DiagnosticCode.UnterminatedTypedConstant       => new(nameof(DiagnosticCode.UnterminatedTypedConstant),       DiagnosticStage.Lex,   Severity.Error,   "Value opened with ' is missing its closing quote — every ' must have a matching '"),
        DiagnosticCode.UnterminatedInterpolation       => new(nameof(DiagnosticCode.UnterminatedInterpolation),       DiagnosticStage.Lex,   Severity.Error,   "The {{ }} section is not closed — add a closing }} on the same line"),
        DiagnosticCode.InvalidCharacter                  => new(nameof(DiagnosticCode.InvalidCharacter),                  DiagnosticStage.Lex,   Severity.Error,   "'{0}' is not a valid character in a precept definition — remove or replace it"),
        DiagnosticCode.UnrecognizedStringEscape          => new(nameof(DiagnosticCode.UnrecognizedStringEscape),          DiagnosticStage.Lex,   Severity.Error,   "'\\{0}' is not a valid escape in a text value — use \\\" for a quote, \\\\ for a backslash, \\n for a newline, or \\t for a tab"),
        DiagnosticCode.UnrecognizedTypedConstantEscape   => new(nameof(DiagnosticCode.UnrecognizedTypedConstantEscape),   DiagnosticStage.Lex,   Severity.Error,   "'\\{0}' is not a valid escape in a single-quoted value — use \\' for a quote, or \\\\ for a backslash"),
        DiagnosticCode.UnescapedBraceInLiteral           => new(nameof(DiagnosticCode.UnescapedBraceInLiteral),           DiagnosticStage.Lex,   Severity.Error,   "Use '}}}}' to include a literal }} in this value — a single }} starts a field reference"),
        DiagnosticCode.ExpectedToken                  => new(nameof(DiagnosticCode.ExpectedToken),                  DiagnosticStage.Parse, Severity.Error,   "Expected {0} here, but found '{1}'"),
        DiagnosticCode.UnexpectedKeyword              => new(nameof(DiagnosticCode.UnexpectedKeyword),              DiagnosticStage.Parse, Severity.Error,   "'{0}' cannot appear inside a {1}"),
        DiagnosticCode.NonAssociativeComparison         => new(nameof(DiagnosticCode.NonAssociativeComparison),         DiagnosticStage.Parse, Severity.Error,   "Comparisons like == and < cannot be chained — {0}"),
        DiagnosticCode.InvalidCallTarget                => new(nameof(DiagnosticCode.InvalidCallTarget),                DiagnosticStage.Parse, Severity.Error,   "Only built-in functions can be called this way — '{0}' is not a function name"),
        DiagnosticCode.UndeclaredField                => new(nameof(DiagnosticCode.UndeclaredField),                DiagnosticStage.Type,  Severity.Error,   "Field '{0}' is not declared"),
        DiagnosticCode.TypeMismatch                   => new(nameof(DiagnosticCode.TypeMismatch),                   DiagnosticStage.Type,  Severity.Error,   "Expected a {0} value here, but got '{1}'"),
        DiagnosticCode.NullInNonNullableContext       => new(nameof(DiagnosticCode.NullInNonNullableContext),       DiagnosticStage.Type,  Severity.Error,   "'{0}' requires a value and cannot be empty here"),
        DiagnosticCode.InvalidMemberAccess            => new(nameof(DiagnosticCode.InvalidMemberAccess),            DiagnosticStage.Type,  Severity.Error,   "'.{0}' is not available on {1} fields"),
        DiagnosticCode.FunctionArityMismatch          => new(nameof(DiagnosticCode.FunctionArityMismatch),          DiagnosticStage.Type,  Severity.Error,   "'{0}' takes {1} inputs, but {2} were provided"),
        DiagnosticCode.FunctionArgConstraintViolation => new(nameof(DiagnosticCode.FunctionArgConstraintViolation), DiagnosticStage.Type,  Severity.Error,   "Value {0} for '{1}' is not valid: {2}"),
        DiagnosticCode.UnreachableState               => new(nameof(DiagnosticCode.UnreachableState),               DiagnosticStage.Graph, Severity.Warning, "State '{0}' is unreachable from initial state '{1}'"),
        DiagnosticCode.UnhandledEvent                 => new(nameof(DiagnosticCode.UnhandledEvent),                 DiagnosticStage.Graph, Severity.Warning, "No transition handles event '{0}' in state '{1}' — firing it will always be rejected"),
        DiagnosticCode.UnsatisfiableGuard             => new(nameof(DiagnosticCode.UnsatisfiableGuard),             DiagnosticStage.Proof, Severity.Warning, "The condition '{0}' on event '{1}' can never be true when {2} — this transition will never fire"),
        DiagnosticCode.DivisionByZero                 => new(nameof(DiagnosticCode.DivisionByZero),                 DiagnosticStage.Proof, Severity.Error,   "Division by zero: '{0}' can be zero when {1}"),
        DiagnosticCode.SqrtOfNegative                 => new(nameof(DiagnosticCode.SqrtOfNegative),                 DiagnosticStage.Proof, Severity.Error,   "sqrt() requires a non-negative value, but '{0}' can be negative when {1}"),
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
