using System;
using System.Linq;
using FluentAssertions;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public class CompilerEdgeProofStatusTests
{
    [Fact]
    public void Compile_PopulatesEdgeProofStatuses_ForCompiledStateGraph()
    {
        var compilation = Compiler.Compile("""
            precept EdgeProofStatusBasic
            state Draft initial
            state Done terminal
            event Submit
            from Draft on Submit -> transition Done
            """);

        compilation.Graph.EdgeProofStatuses.Should().ContainSingle();
        compilation.Graph.EdgeProofStatuses[0].Should().BeEquivalentTo(new EdgeProofStatus(
            FromState: "Draft",
            EventName: "Submit",
            ToState: "Done",
            HasObligations: false,
            IsProven: true,
            UnresolvedObligationSummaries: []));
    }

    [Fact]
    public void Compile_ProvenTransitionAction_MarksEdgeProofStatusProven()
    {
        var status = GetSingleEdgeProofStatus("""
            precept EdgeProofStatusProven
            field Result as number
            field Numerator as number
            field Denominator as number
            state Draft initial
            state Approved terminal
            event Submit
            from Draft on Submit when Denominator != 0
                -> set Result = Numerator / Denominator
                -> transition Approved
            """);

        status.HasObligations.Should().BeTrue();
        status.IsProven.Should().BeTrue();
        status.UnresolvedObligationSummaries.Should().BeEmpty();
    }

    [Fact]
    public void Compile_UnresolvedTransitionAction_MarksEdgeProofStatusGapAndCarriesSummaries()
    {
        var status = GetSingleEdgeProofStatus("""
            precept EdgeProofStatusGap
            field Result as number
            field Numerator as number
            field Denominator as number
            state Draft initial
            state Approved terminal
            event Submit
            from Draft on Submit
                -> set Result = Numerator / Denominator
                -> transition Approved
            """);

        status.HasObligations.Should().BeTrue();
        status.IsProven.Should().BeFalse();
        status.UnresolvedObligationSummaries.Should().NotBeEmpty();
        status.UnresolvedObligationSummaries.Should().OnlyContain(summary => !string.IsNullOrWhiteSpace(summary));
    }

    [Fact]
    public void Compile_DeduplicatesDuplicateRequirementDescriptions_PerEdge()
    {
        var compilation = Compiler.Compile("""
            precept EdgeProofStatusDedup
            field PrimaryQueue as queue of string
            field SecondaryQueue as queue of string
            state Draft initial
            state Approved terminal
            event Submit
            from Draft on Submit
                -> dequeue PrimaryQueue
                -> dequeue SecondaryQueue
                -> transition Approved
            """);

        compilation.Proof.Obligations.Count(obligation =>
            obligation.Disposition == ProofDisposition.Unresolved
            && obligation.Context is TransitionRowContext
            && obligation.Requirement.Description == "Queue must be non-empty")
            .Should().Be(2);

        var status = compilation.Graph.EdgeProofStatuses.Should().ContainSingle().Subject;
        status.HasObligations.Should().BeTrue();
        status.IsProven.Should().BeFalse();
        status.UnresolvedObligationSummaries.Should().Equal("Queue must be non-empty");
    }

    [Fact]
    public void Compile_WildcardRows_RespectExplicitEdgeOverrideWhenProjectingProofStatus()
    {
        var compilation = Compiler.Compile("""
            precept EdgeProofStatusWildcard
            field Result as number
            field Numerator as number
            field Denominator as number
            state Draft initial
            state Review
            state Done terminal
            event Submit
            from any on Submit
                -> set Result = Numerator / Denominator
                -> transition Done
            from Review on Submit when Denominator != 0
                -> set Result = Numerator / Denominator
                -> transition Done
            """);

        compilation.Graph.EdgeProofStatuses.Should().Contain(status =>
            status.FromState == "Draft"
            && status.EventName == "Submit"
            && status.ToState == "Done"
            && status.IsProven == false);
        compilation.Graph.EdgeProofStatuses.Should().Contain(status =>
            status.FromState == "Review"
            && status.EventName == "Submit"
            && status.ToState == "Done"
            && status.IsProven);
    }

    private static EdgeProofStatus GetSingleEdgeProofStatus(string source)
    {
        var compilation = Compiler.Compile(source);
        return compilation.Graph.EdgeProofStatuses.Single();
    }
}
