namespace Precept.Language;

public static class CurrencyValidator
{
    public static TypedConstantParseResult Validate(string rawText)
    {
        var normalized = rawText.Trim().ToUpperInvariant();
        return CurrencyCatalog.All.TryGetValue(normalized, out var currency)
            ? new TypedConstantParseResult(true, currency, currency.AlphaCode, "ISO 4217 currency code", [])
            : TypedConstantParseResult.Failed(
                "ISO 4217 currency code",
                new TypedConstantDiagnostic("TC010", $"'{rawText}' is not a recognized ISO 4217 currency code."));
    }
}
