using FluentAssertions;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

public class Track2PhaseAProofEngineTests
{
    [Fact]
    public void Sqrt_Of_Abs_CompilesClean()
    {
        var precept = """
            precept Widget
            field X as number default 0 writable
            rule sqrt(abs(X)) >= 0 because "abs makes the value nonnegative"
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var ledger = ProofEngine.Prove(index, GraphAnalyzer.Analyze(index));

        ledger.Diagnostics.Should().BeEmpty();
    }
}
