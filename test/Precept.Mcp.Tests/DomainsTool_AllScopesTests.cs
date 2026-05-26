using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

/// <summary>
/// Scenario coverage for <see cref="DomainsTool"/>.
///
/// Verifies that <c>precept_domains</c> succeeds for every scope argument
/// and that the markdown payload carries the section markers each scope
/// is supposed to emit.
/// </summary>
public class DomainsTool_AllScopesTests
{
    [Theory]
    [InlineData("currencies", "Currencies")]
    [InlineData("units", "UCUM Tier-1 Units")]
    [InlineData("dimensions", "Dimensions")]
    [InlineData("prefixes", "UCUM Prefixes")]
    [InlineData("temporal", "Temporal Units")]
    public void Domains_KnownScope_ReturnsMarkdownWithExpectedHeader(string scope, string expectedHeader)
    {
        var result = DomainsTool.Domains(scope);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith("# Precept Domain Catalog");
        result.Should().Contain(expectedHeader);
    }

    [Fact]
    public void Domains_NoScope_ReturnsFullCatalog()
    {
        var result = DomainsTool.Domains(null);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith("# Precept Domain Catalog");
        result.Should().Contain("Currencies");
        result.Should().Contain("UCUM Tier-1 Units");
        result.Should().Contain("Dimensions");
        result.Should().Contain("Temporal Units");
    }

    [Fact]
    public void Domains_UnknownScope_ReturnsStructuredErrorPayload()
    {
        var result = DomainsTool.Domains("nonexistent");

        // The formatter handles unknown scopes itself — confirm we never see
        // the raw transport error from the MCP SDK fallback path.
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotStartWith("Error invoking '");
    }
}
