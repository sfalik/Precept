using System.Text.RegularExpressions;

namespace Precept.Language;

public static class RegexValidator
{
    public static TypedConstantParseResult Validate(string rawText, RegexValidation validation)
    {
        return Regex.IsMatch(rawText, validation.Pattern)
            ? new TypedConstantParseResult(true, rawText, rawText, validation.FormatDescription, [])
            : TypedConstantParseResult.Failed(
                validation.FormatDescription,
                new TypedConstantDiagnostic("TC003", $"Value does not match {validation.FormatDescription}."));
    }
}
