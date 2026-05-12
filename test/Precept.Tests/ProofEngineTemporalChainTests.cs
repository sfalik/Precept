using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

public class ProofEngineTemporalChainTests
{
    private static ProofLedger Prove(string source)
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    private static void AssertSingleChainObligation(ProofLedger ledger, ProofDisposition disposition)
    {
        ledger.Obligations
            .Where(o => o.Requirement is QualifierChainProofRequirement)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(disposition);
    }

    [Theory]
    [InlineData("time", "time")]
    [InlineData("date", "date")]
    public void PriceTimesPeriod_MatchingTemporalDimensions_Proved(string priceDimension, string periodDimension)
    {
        var ledger = Prove($"""
            precept Widget
            field P as price in 'USD' of '{priceDimension}' writable
            field T as period of '{periodDimension}' writable
            field Result as money writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P * T -> no transition
            """);

        AssertSingleChainObligation(ledger, ProofDisposition.Proved);
        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Theory]
    [InlineData("time", "date")]
    [InlineData("date", "time")]
    [InlineData("mass", "date")]
    public void PriceTimesPeriod_MismatchedTemporalDimensions_EmitsDiagnostic(string priceDimension, string periodDimension)
    {
        var ledger = Prove($"""
            precept Widget
            field P as price in 'USD' of '{priceDimension}' writable
            field T as period of '{periodDimension}' writable
            field Result as money writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P * T -> no transition
            """);

        AssertSingleChainObligation(ledger, ProofDisposition.Unresolved);
        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void PriceTimesDuration_TimePrice_Proved()
    {
        var ledger = Prove("""
            precept Widget
            field P as price in 'USD' of 'time' writable
            field D as duration writable
            field Result as money writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P * D -> no transition
            """);

        AssertSingleChainObligation(ledger, ProofDisposition.Proved);
        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Theory]
    [InlineData("date")]
    [InlineData("mass")]
    public void PriceTimesDuration_NonTimePrice_EmitsDiagnostic(string priceDimension)
    {
        var ledger = Prove($"""
            precept Widget
            field P as price in 'USD' of '{priceDimension}' writable
            field D as duration writable
            field Result as money writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P * D -> no transition
            """);

        AssertSingleChainObligation(ledger, ProofDisposition.Unresolved);
        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void BarePriceTimesBarePeriod_ObligationFiresAndCannotDischarge()
    {
        var ledger = Prove("""
            precept Widget
            field P as price writable
            field T as period writable
            field Result as money writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P * T -> no transition
            """);

        AssertSingleChainObligation(ledger, ProofDisposition.Unresolved);
        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void BarePriceTimesDuration_ObligationFiresAndCannotDischarge()
    {
        var ledger = Prove("""
            precept Widget
            field P as price writable
            field D as duration writable
            field Result as money writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P * D -> no transition
            """);

        AssertSingleChainObligation(ledger, ProofDisposition.Unresolved);
        ledger.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void Regression_PriceTimesDecimal_Scaling_Unaffected()
    {
        var ledger = Prove("""
            precept Widget
            field P as price in 'USD' of 'mass' writable
            field Scale as decimal default 2.0 writable
            field Result as price in 'USD' of 'mass' writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P * Scale -> no transition
            """);

        ledger.Obligations.Should().NotContain(o => o.Requirement is QualifierChainProofRequirement);
        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void Regression_PricePlusPrice_QualifierCompatibility_Unaffected()
    {
        var ledger = Prove("""
            precept Widget
            field P1 as price in 'USD' of 'mass' writable
            field P2 as price in 'USD' of 'mass' writable
            field Result as price in 'USD' of 'mass' writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P1 + P2 -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is QualifierCompatibilityProofRequirement)
            .Should().AllSatisfy(o => o.Disposition.Should().Be(ProofDisposition.Proved));
        ledger.Obligations.Should().NotContain(o => o.Requirement is QualifierChainProofRequirement);
    }

    [Fact]
    public void Price_same_qualifiers_proved()
    {
        var ledger = Prove("""
            precept Widget
            field P1 as price in 'USD' of 'mass' writable
            field P2 as price in 'USD' of 'mass' writable
            field Result as price in 'USD' of 'mass' writable
            state Draft initial
            event Submit
            from Draft on Submit -> set Result = P1 + P2 -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is QualifierCompatibilityProofRequirement)
            .Should().AllSatisfy(o => o.Disposition.Should().Be(ProofDisposition.Proved));
        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }
}
