using System.Collections.Generic;
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

    [Fact]
    public void Get_KnownCode_ReturnsEntry()
    {
        var entry = CurrencyCatalog.Get("USD");
        entry.AlphaCode.Should().Be("USD");
    }

    [Fact]
    public void Get_UnknownCode_Throws()
    {
        var act = () => CurrencyCatalog.Get("ZZZ");
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGet_KnownCode_ReturnsTrueWithEntry()
    {
        CurrencyCatalog.TryGet("USD", out var entry).Should().BeTrue();
        entry.Should().NotBeNull();
        entry!.AlphaCode.Should().Be("USD");
    }

    [Fact]
    public void TryGet_UnknownCode_ReturnsFalse()
    {
        CurrencyCatalog.TryGet("ZZZ", out _).Should().BeFalse();
    }

    [Fact]
    public void GetByNumericCode_Usd_ReturnsEntry()
    {
        var entry = CurrencyCatalog.GetByNumericCode(840);
        entry.Should().NotBeNull();
        entry!.AlphaCode.Should().Be("USD");
    }

    [Fact]
    public void GetByNumericCode_Unknown_ReturnsNull()
    {
        CurrencyCatalog.GetByNumericCode(999).Should().BeNull();
    }

    [Fact]
    public void IsValid_KnownCode_ReturnsTrue() =>
        CurrencyCatalog.IsValid("USD").Should().BeTrue();

    [Fact]
    public void IsValid_UnknownCode_ReturnsFalse() =>
        CurrencyCatalog.IsValid("ZZZ").Should().BeFalse();

    [Fact]
    public void DataVersion_IsNonEmpty()
    {
        CurrencyCatalog.DataVersion.Should().NotBeNullOrEmpty();
        CurrencyCatalog.DataVersion.Should().NotBe("unknown");
    }

    [Fact]
    public void Default_IsNull() =>
        CurrencyCatalog.Default.Should().BeNull();
}
