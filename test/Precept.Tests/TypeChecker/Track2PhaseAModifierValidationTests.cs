using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class Track2PhaseAModifierValidationTests
{
    [Fact]
    public void InvalidModifierBounds_MinExceedsMax_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Score as number min 10 max 0
            state Draft initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierBounds);
    }

    [Fact]
    public void InvalidModifierBounds_MinlengthExceedsMaxlength_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string minlength 10 maxlength 3
            state Draft initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierBounds);
    }

    [Fact]
    public void InvalidModifierBounds_MincountExceedsMaxcount_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Items as list of integer mincount 5 maxcount 2
            state Draft initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierBounds);
    }

    [Fact]
    public void InvalidModifierBounds_MoneyTypedConstants_MinExceedsMax_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' min '10 USD' max '5 USD'
            state Draft initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierBounds);
    }

    [Fact]
    public void WritableOnEventArg_RejectedByParserWithExpectedToken()
    {
        var precept = """
            precept Widget
            field Name as string
            state Draft initial
            event Update(NewName as string writable)
            from Draft on Update -> set Name = Update.NewName -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ExpectedToken);
    }
}
