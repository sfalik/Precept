using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// F-LANG-COLL-04 / F-LANG-COLL-09: index-bounds proof obligations on
/// .at(N), insert at N, and remove at N. Per the locked design at
/// docs/Working/index-bounds-proof-design.md, the obligation discharges
/// from author-written `when N >= 0 and N < F.count` guards (or, for
/// inserts, `N <= F.count`), with lower-bound discharge optionally
/// type-derived from a nonnegative-declared index.
/// </summary>
public class IndexBoundsTests
{
    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    [Fact]
    public void AtAccessor_WithExplicitBoundsGuard_Discharges()
    {
        var ledger = Prove("""
            precept Repro
            field Items as list of string
            field Picked as string default ""
            state Open initial

            event Pick(Index as integer)
            from Open on Pick
                when Pick.Index >= 0 and Pick.Index < Items.count
                -> set Picked = Items.at(Pick.Index)
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is IndexBoundsProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(
                because: "the bounds guard `Pick.Index >= 0 and Pick.Index < Items.count` discharges both lower and upper bounds");
    }

    [Fact]
    public void AtAccessor_WithoutGuard_EmitsIndexBoundsObligation()
    {
        var ledger = Prove("""
            precept Repro
            field Items as list of string
            field Picked as string default ""
            state Open initial

            event Pick(Index as integer)
            from Open on Pick when Items.count > 0
                -> set Picked = Items.at(Pick.Index)
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is IndexBoundsProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().NotBeEmpty(
                because: "the non-empty guard alone doesn't prove the index is in range");
    }

    [Fact]
    public void AtAccessor_WithNonnegativeArgAndUpperGuard_Discharges()
    {
        // Lower bound from `nonnegative`-typed event arg; upper from explicit guard.
        var ledger = Prove("""
            precept Repro
            field Items as list of string
            field Picked as string default ""
            state Open initial

            event Pick(Index as integer nonnegative)
            from Open on Pick when Pick.Index < Items.count
                -> set Picked = Items.at(Pick.Index)
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is IndexBoundsProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(
                because: "nonnegative-declared index discharges lower bound; explicit guard handles the upper");
    }

    [Fact]
    public void RemoveAt_WithExplicitBoundsGuard_Discharges()
    {
        var ledger = Prove("""
            precept Repro
            field Items as list of string
            state Open initial

            event Drop(Index as integer)
            from Open on Drop when Drop.Index >= 0 and Drop.Index < Items.count
                -> remove Items at Drop.Index
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is IndexBoundsProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty();
    }

    [Fact]
    public void InsertAt_WithAtOrBeforeGuard_Discharges()
    {
        // Insert at index N accepts N == F.count (append-at-end).
        var ledger = Prove("""
            precept Repro
            field Items as list of string
            state Open initial

            event Add(NewItem as string, Index as integer)
            from Open on Add when Add.Index >= 0 and Add.Index <= Items.count
                -> insert Items Add.NewItem at Add.Index
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is IndexBoundsProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(
                because: "insert accepts N <= count (AtOrBefore mode)");
    }

    [Fact]
    public void InsertAt_WithStrictUpperGuard_AlsoDischarges()
    {
        // Strict `<` implies `<=`, so a strict guard satisfies AtOrBefore.
        var ledger = Prove("""
            precept Repro
            field Items as list of string
            state Open initial

            event Add(NewItem as string, Index as integer)
            from Open on Add when Add.Index >= 0 and Add.Index < Items.count
                -> insert Items Add.NewItem at Add.Index
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is IndexBoundsProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().BeEmpty(
                because: "strict-less-than implies less-than-or-equal for AtOrBefore mode");
    }

    [Fact]
    public void RemoveAt_WithAtOrBeforeGuard_DoesNotDischargeStrictMode()
    {
        // RemoveAt requires `<` (StrictlyBefore). `<=` is not sufficient since
        // index = count is out of bounds for removal.
        var ledger = Prove("""
            precept Repro
            field Items as list of string
            state Open initial

            event Drop(Index as integer)
            from Open on Drop when Drop.Index >= 0 and Drop.Index <= Items.count
                -> remove Items at Drop.Index
                -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is IndexBoundsProofRequirement)
            .Where(o => o.Disposition == ProofDisposition.Unresolved)
            .Should().NotBeEmpty(
                because: "remove-at requires strict less-than; <= count is not sufficient");
    }
}
