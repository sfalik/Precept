using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// F-LANG-COLL-05: `append F E by P` on a `log of T by P` requires the ordering key P
/// to not already be present in the log. The proof engine discharges this via a
/// `when not (F contains P)` guard on the enclosing row.
/// </summary>
public class LogByUniquenessTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void AppendBy_WithContainsNegationGuard_DischargesUniqueness()
    {
        var ledger = Prove("""
            precept Repro
            field AuditLog as log of string by integer
            state Open initial

            event Record(Entry as string, Seq as integer)
            from Open on Record
                when not (AuditLog contains Record.Seq)
                -> append AuditLog Record.Entry by Record.Seq
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is KeyPresenceProofRequirement { RequireAbsence: true })
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(
                because: "the `when not (AuditLog contains Seq)` guard discharges the append-uniqueness obligation");
    }

    [Fact]
    public void AppendBy_WithoutGuard_EmitsUniquenessObligation()
    {
        var ledger = Prove("""
            precept Repro
            field AuditLog as log of string by integer
            state Open initial

            event Record(Entry as string, Seq as integer)
            from Open on Record
                -> append AuditLog Entry by Seq
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is KeyPresenceProofRequirement { RequireAbsence: true })
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().NotBeEmpty(
                because: "without a `when not (F contains P)` guard, AppendBy's uniqueness obligation cannot discharge");
    }

    [Fact]
    public void AppendBy_WithUnrelatedGuard_DoesNotDischarge()
    {
        var ledger = Prove("""
            precept Repro
            field AuditLog as log of string by integer
            field MaxSeq as integer default 100
            state Open initial

            event Record(Entry as string, Seq as integer)
            from Open on Record
                when Seq < MaxSeq
                -> append AuditLog Entry by Seq
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is KeyPresenceProofRequirement { RequireAbsence: true })
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().NotBeEmpty(
                because: "a guard unrelated to `contains` doesn't satisfy the uniqueness obligation");
    }
}
