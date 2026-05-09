namespace Precept.Language;

public readonly record struct UcumParseResult(bool IsValid, UcumParsedUnit? Unit, IReadOnlyList<UcumDiagnostic> Diagnostics)
{
    public static UcumParseResult Success(UcumParsedUnit unit) => new(true, unit, []);

    public static UcumParseResult Failure(params UcumDiagnostic[] diagnostics) => new(false, null, diagnostics);
}
