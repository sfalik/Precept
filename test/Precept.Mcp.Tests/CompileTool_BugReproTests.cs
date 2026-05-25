using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

/// <summary>
/// Scenario tests covering the Phase 2 Step 2.2 compile-path robustness sweep.
///
/// Confirms that every bug repro from <c>docs/Working/bugs.md</c> § Active
/// produces a structured diagnostic (or compiles clean for the valid cases),
/// rather than crashing the compiler or returning the raw
/// <c>"An error occurred invoking 'precept_compile'"</c> string.
///
/// Coverage matrix mirrors the Phase 2 plan
/// (<c>docs/Working/compiler-readiness-plan-2026-05-24.md</c> Phase 2 Step 2.2b):
///
/// | Bug              | Source                                                | Expected                              |
/// |------------------|-------------------------------------------------------|---------------------------------------|
/// | F-LANG-SPEC-10   | date default '2026-13-01'                             | InvalidDateValue                      |
/// | F-LANG-SPEC-10   | time default '25:00:00'                               | InvalidTimeValue                      |
/// | F-LANG-SPEC-10   | instant default '2026-99-99T99:99:99Z'                | InvalidInstantFormat                  |
/// | BUG-003          | period default '1 year'                               | clean                                 |
/// | BUG-003          | period default '1 bogus'                              | InvalidTypedConstantContent           |
/// | BUG-008          | duration default '14 days'                            | clean                                 |
/// | BUG-010          | set X = now() + '365 days'                            | clean (or structured diagnostic)      |
/// | BUG-011          | timezone default 'America/New_York'                   | clean                                 |
/// | BUG-011          | time default '09:00'                                  | clean                                 |
/// | BUG-005          | lookup of string to money in 'USD'                    | CollectionInnerTypeError              |
/// </summary>
public class CompileTool_BugReproTests
{
    [Fact]
    public void FLangSpec10_InvalidDate_EmitsInvalidDateValue()
    {
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field D as date default '2026-13-01'\n" +
            "state Draft initial\n" +
            "state Done terminal\n" +
            "event E\n" +
            "from Draft on E -> transition Done\n");

        result.Success.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Code == "PRE0055");
    }

    [Fact]
    public void FLangSpec10_InvalidTime_EmitsInvalidTimeValue()
    {
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field T as time default '25:00:00'\n" +
            "state Draft initial\n" +
            "state Done terminal\n" +
            "event E\n" +
            "from Draft on E -> transition Done\n");

        result.Success.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Code == "PRE0057");
    }

    [Fact]
    public void FLangSpec10_InvalidInstant_EmitsInvalidInstantFormat()
    {
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field I as instant default '2026-99-99T99:99:99Z'\n" +
            "state Draft initial\n" +
            "state Done terminal\n" +
            "event E\n" +
            "from Draft on E -> transition Done\n");

        result.Success.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Code == "PRE0058");
    }

    [Fact]
    public void Bug003_ValidPeriodDefault_CompilesClean()
    {
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field G as period default '1 year'\n" +
            "state Draft initial\n" +
            "state Done terminal\n" +
            "event E\n" +
            "from Draft on E -> transition Done\n");

        result.Success.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Bug003_InvalidPeriodLiteral_EmitsInvalidTypedConstantContent()
    {
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field G as period default '1 bogus'\n" +
            "state Draft initial\n" +
            "state Done terminal\n" +
            "event E\n" +
            "from Draft on E -> transition Done\n");

        result.Success.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Code == "PRE0053");
    }

    [Fact]
    public void Bug008_ValidDurationDefault_CompilesClean()
    {
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field D as duration default '14 days'\n" +
            "state Draft initial\n" +
            "state Done terminal\n" +
            "event E\n" +
            "from Draft on E -> transition Done\n");

        result.Success.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Bug010_NowPlusDuration_ProducesStructuredResponse()
    {
        // BUG-010: the typed-constant inference path picks instant rather than
        // duration for '365 days'. The crash is gone (no raw exception), but a
        // type-inference upgrade is tracked separately in Phase 4.
        // The robustness contract for Phase 2 is "no raw exception"; this
        // assertion validates the structured-diagnostic shape rather than the
        // success outcome.
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field X as instant optional\n" +
            "state Requested initial\n" +
            "state Active terminal\n" +
            "event Create(K as integer) initial\n" +
            "event Activate\n" +
            "on Create -> set X = now()\n" +
            "from Requested on Activate -> set X = now() + '365 days' -> transition Active\n");

        // Robustness guarantee — never the raw transport error.
        result.Diagnostics.Should().AllSatisfy(d => d.Code.Should().StartWith("PRE"));
    }

    [Fact]
    public void Bug011_ValidTimezoneDefault_CompilesClean()
    {
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field Tz as timezone default 'America/New_York'\n" +
            "state Draft initial\n" +
            "state Done terminal\n" +
            "event E\n" +
            "from Draft on E -> transition Done\n");

        result.Success.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Bug011_ValidTimeShortDefault_CompilesClean()
    {
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field T as time default '09:00'\n" +
            "state Draft initial\n" +
            "state Done terminal\n" +
            "event E\n" +
            "from Draft on E -> transition Done\n");

        result.Success.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Bug005_LookupOfMoneyInCurrency_EmitsCollectionInnerTypeError()
    {
        // BUG-005 symptom fix (Step 2.2c): full qualified-inner-type support is
        // deferred to F-LANG-COLL-06 (Phase 4). Until then the parser emits a
        // single PRE0105 instead of cascading three garbled parse diagnostics
        // or crashing the compiler.
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field F as lookup of string to money in 'USD'\n" +
            "state Draft initial terminal\n");

        result.Success.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle(d => d.Code == "PRE0105");
    }

    [Fact]
    public void Bug005_LookupOfQuantityOfDimension_EmitsCollectionInnerTypeError()
    {
        // Symmetric to the currency case — both qualified-inner-type shapes now
        // yield the same single PRE0105 instead of the dimension-cascade noise
        // they produced before Step 2.2c.
        var result = CompileTool.Compile(
            "precept Repro\n" +
            "field F as lookup of string to quantity of 'mass'\n" +
            "state Draft initial terminal\n");

        result.Success.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle(d => d.Code == "PRE0105");
    }

    [Fact]
    public void AllRepros_NeverThrowRawException_AndAlwaysReturnStructuredDtos()
    {
        // Single belt-and-braces guard: even if a future regression returns to
        // raw exceptions on these inputs, the McpToolSafeInvoke wrapper would
        // intercept and return a CompileResultDto carrying the
        // McpToolInternalError diagnostic. This test asserts the structured
        // shape regardless of which path provided it.
        string[] inputs =
        {
            "precept Repro\nfield G as period default '1 bogus'\nstate Draft initial terminal\n",
            "precept Repro\nfield D as date default '2026-13-01'\nstate Draft initial terminal\n",
            "precept Repro\nfield F as lookup of string to money in 'USD'\nstate Draft initial terminal\n",
        };

        foreach (var input in inputs)
        {
            var result = CompileTool.Compile(input);
            result.Should().NotBeNull();
            result.Diagnostics.Should().NotBeNull();
            result.Summary.Should().NotBeNullOrEmpty();
        }
    }
}
