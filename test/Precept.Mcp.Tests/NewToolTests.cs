using System.Text;
using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class NewToolTests
{
    [Fact]
    public void Quickstart_DefaultCall_ReturnsMarkdown()
    {
        var result = QuickstartTool.Quickstart();

        result.Should().StartWith("# Precept Quickstart");
        result.Should().Contain("## Core Concepts");
        result.Should().Contain("### Minimal lifecycle");
        result.Should().Contain("precept_quickstart");
    }

    [Fact]
    public void Syntax_DefaultCall_ReturnsMarkdown()
    {
        var result = SyntaxTool.Syntax();

        result.Should().StartWith("# Precept Syntax Reference");
        result.Should().Contain("## Grammar Rules");
        result.Should().Contain("## Constructs");
        result.Should().Contain("## Operators");
        result.Should().Contain("line-oriented");
    }

    [Fact]
    public void Types_DefaultCall_ReturnsMarkdown()
    {
        var result = TypesTool.Types();

        result.Should().StartWith("# Precept Type System");
        result.Should().Contain("## Types");
        result.Should().Contain("## Modifiers");
        result.Should().Contain("## Built-in Functions");
        result.Should().Contain("**money**");
    }

    [Fact]
    public void Types_FunctionScope_ReturnsOnlyFunctions()
    {
        var result = TypesTool.Types("functions");

        result.Should().Contain("Scope: `functions`");
        result.Should().Contain("## Built-in Functions");
        result.Should().Contain("**min**");
        result.Should().NotContain("## Types");
        result.Should().NotContain("## Modifiers");
    }

    [Fact]
    public void Types_ValueModifierScope_ReturnsOnlyValueModifiers()
    {
        var result = TypesTool.Types("modifiers:value");

        result.Should().Contain("Scope: `modifiers:value`");
        result.Should().Contain("### Value Modifiers");
        result.Should().Contain("nonnegative");
        result.Should().NotContain("### State Modifiers");
        result.Should().NotContain("## Built-in Functions");
        result.Should().NotContain("## Types");
    }

    [Fact]
    public void Operations_DefaultCall_ReturnsMarkdown()
    {
        var result = OperationsTool.Operations();

        result.Should().StartWith("# Precept Operations");
        result.Should().Contain("## Available Categories");
        result.Should().Contain("## Matching Operations");
        result.Should().Contain("## Count");
        result.Should().Contain("`Money`");
    }

    [Fact]
    public void Operations_CategoryFilter_IsApplied()
    {
        var result = OperationsTool.Operations("Money");

        result.Should().Contain("Filtered by: `Money`");
        result.Should().Contain("money + money -> money");
        result.Should().NotContain("Prefer the `category` filter");
    }

    [Fact]
    public void Proofs_DefaultCall_ReturnsMarkdown()
    {
        var result = ProofsTool.Proofs();

        result.Should().StartWith("# Precept Proofs and Runtime Faults");
        result.Should().Contain("## Proof Requirements");
        result.Should().Contain("## Runtime Faults");
        result.Should().Contain("**Numeric**");
        result.Should().Contain("**DivisionByZero**");
    }

    [Fact]
    public void LanguageTool_ProofRequirements_IncludesIntervalContainment()
    {
        var result = ProofsTool.Proofs();

        result.Should().Contain("**IntervalContainment**");
    }

    [Fact]
    public void Patterns_DefaultCall_ReturnsMarkdown()
    {
        var result = PatternsTool.Patterns();

        result.Should().StartWith("# Precept Patterns");
        result.Should().Contain("## Common Patterns");
        result.Should().Contain("### Guarded transition");
        result.Should().Contain("### Entry action hook");
        result.Should().Contain("### Cross-cutting event (from any)");
        result.Should().Contain("### Stack and queue operations");
        result.Should().Contain("### Optional-with-fallback assignment");
        result.Should().Contain("### Conditional rule (rule when)");
        result.Should().Contain("### State-scoped editing window");
        result.Should().Contain("### Interpolation in diagnostic strings");
        result.Should().Contain("## Anti-Patterns");
        result.Should().Contain("### Sentinel defaults for not-yet-meaningful fields");
        result.Should().Contain("omit ApprovedAmount");
        result.Should().Contain("### Exhaustive rejection rows");
    }

    [Fact]
    public void Diagnostic_LookupByName_ReturnsMarkdown()
    {
        var result = DiagnosticTool.Diagnostic("UndeclaredField");

        result.Should().StartWith("# Diagnostic UndeclaredField (PRE0017)");
        result.Should().Contain("## Trigger");
        result.Should().Contain("## Recovery Steps");
    }

    [Fact]
    public void Diagnostic_LookupByPreCode_ReturnsSameEntry()
    {
        var result = DiagnosticTool.Diagnostic("PRE0017");

        result.Should().StartWith("# Diagnostic UndeclaredField (PRE0017)");
        result.Should().Contain("## Fix Hint");
    }

    [Fact]
    public void Diagnostic_MissingCode_ReturnsFailureBlock()
    {
        var result = DiagnosticTool.Diagnostic("PRE9999");

        result.Should().StartWith("# Diagnostic Lookup Failed");
        result.Should().Contain("Requested: `PRE9999`");
        result.Should().Contain("UndeclaredField");
    }

    [Fact]
    public void Domains_DefaultCall_ReturnsMarkdown()
    {
        var result = DomainsTool.Domains();

        result.Should().StartWith("# Precept Domain Catalog");
        result.Should().Contain("## Currencies");
        result.Should().Contain("## UCUM Tier-1 Units");
        result.Should().Contain("## UCUM Prefixes");
        result.Should().Contain("## Temporal Units");
        result.Should().Contain("**USD**");
        result.Should().Contain("**kg**");
    }

    [Fact]
    public void Domains_CurrencyScope_ReturnsOnlyCurrencies()
    {
        var result = DomainsTool.Domains("currencies");

        result.Should().Contain("Scope: `currencies`");
        result.Should().Contain("## Currencies");
        result.Should().Contain("**USD**");
        result.Should().NotContain("## UCUM Tier-1 Units");
        result.Should().NotContain("## Temporal Units");
    }

    [Fact]
    public void Domains_TemporalScope_ReturnsOnlyTemporalUnits()
    {
        var result = DomainsTool.Domains("temporal");

        result.Should().Contain("Scope: `temporal`");
        result.Should().Contain("## Temporal Units");
        result.Should().Contain("**day / days**");
        result.Should().NotContain("## Currencies");
        result.Should().NotContain("## UCUM Tier-1 Units");
    }

    [Theory]
    [MemberData(nameof(DefaultCatalogToolOutputs))]
    public void DefaultCatalogToolOutput_IsNonEmpty(string _, string output)
    {
        output.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(DefaultCatalogToolOutputs))]
    public void DefaultCatalogToolOutput_StaysUnderSixtyKilobytes(string name, string output)
    {
        Encoding.UTF8.GetByteCount(output).Should().BeLessThan(60 * 1024, because: $"{name} should stay within the approved context budget");
    }

    public static IEnumerable<object[]> DefaultCatalogToolOutputs()
    {
        yield return ["quickstart", QuickstartTool.Quickstart()];
        yield return ["syntax", SyntaxTool.Syntax()];
        yield return ["types", TypesTool.Types()];
        yield return ["operations", OperationsTool.Operations()];
        yield return ["proofs", ProofsTool.Proofs()];
        yield return ["patterns", PatternsTool.Patterns()];
        yield return ["diagnostic", DiagnosticTool.Diagnostic("UndeclaredField")];
        yield return ["domains", DomainsTool.Domains()];
    }
}
