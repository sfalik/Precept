using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class CurrencyCatalogTests
{
    [Fact]
    public void All_LoadsExpectedCurrencies()
    {
        CurrencyCatalog.All.Count.Should().Be(159);
        CurrencyCatalog.All.Should().ContainKeys("USD", "EUR", "GBP", "JPY");
    }

    [Fact]
    public void All_ExcludesIntentionalNonTransactionalCodes()
    {
        CurrencyCatalog.All.Should().NotContainKey("XAU");
        CurrencyCatalog.All.Should().NotContainKey("XTS");
        CurrencyCatalog.All.Should().NotContainKey("XXX");
    }

    [Fact]
    public void All_LoadsExpectedMinorUnits()
    {
        CurrencyCatalog.All["JPY"].MinorUnit.Should().Be(0);
        CurrencyCatalog.All["BHD"].MinorUnit.Should().Be(3);
        CurrencyCatalog.All["USD"].MinorUnit.Should().Be(2);
    }

    [Fact]
    public void All_LoadsExpectedSymbols()
    {
        CurrencyCatalog.All["USD"].Symbol.Should().Be("$");
        CurrencyCatalog.All["EUR"].Symbol.Should().Be("€");
        CurrencyCatalog.All["JPY"].Symbol.Should().Be("¥");
    }

    [Fact]
    public void All_FallsBackToAlphaCodeWhenSymbolIsAbsent()
    {
        CurrencyCatalog.All["XDR"].Symbol.Should().Be("XDR");
    }
}
