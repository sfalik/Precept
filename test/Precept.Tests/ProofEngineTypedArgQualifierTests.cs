using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

public class ProofEngineTypedArgQualifierTests
{
    private static string SamplesRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    private static ProofLedger Prove(string source)
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void TypedArgRefs_Discharge_QualifierChainProofs()
    {
        var ledger = Prove("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' writable
            state Draft initial
            event Receive(UnitCost as price in 'USD' of 'mass', Qty as quantity of 'mass')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is QualifierChainProofRequirement)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void TypedArgRefs_Use_ArgNames_In_QualifierDiagnostics()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' writable
            state Draft initial
            event Receive(UnitCost as price in 'USD' of 'mass', Qty as quantity of 'length')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        var diagnostic = compilation.Diagnostics
            .Single(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));

        diagnostic.Message.Should().Contain("UnitCost");
        diagnostic.Message.Should().Contain("Qty");
        diagnostic.Message.Should().NotContain("<unknown>");
    }

    [Fact]
    public void InventoryItem_Sample_PRE0114_Count_Drops_Below_Baseline()
    {
        var source = File.ReadAllText(Path.Combine(SamplesRoot, "inventory-item.precept"));
        var compilation = Compiler.Compile(source);

        compilation.Diagnostics
            .Count(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility))
            .Should().BeLessThan(73, because: "C4 should reduce the inventory-item PRE0114 baseline");
    }
}
