using FluentAssertions;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public sealed class RecoveryHintTests
{
    [Fact]
    public void Proofs_CollectionEmptyOnMutation_RecoveryHint_MatchesCurrentRuntimeBehavior()
    {
        var result = ProofsTool.Proofs();

        var fault = result.RuntimeFaults.Should().ContainSingle(f => f.Code == "CollectionEmptyOnMutation").Subject;
        fault.RecoveryHint.Should().Be("Guard the action with 'when CollectionField.count > 0' in the transition row guard clause, or apply 'notempty' to the collection field declaration");
    }

    [Fact]
    public void Diagnostic_UnguardedCollectionMutation_Hints_MatchCurrentRuntimeBehavior()
    {
        var result = DiagnosticTool.Diagnostic("UnguardedCollectionMutation");

        result.Found.Should().BeTrue();
        result.Diagnostic.Should().NotBeNull();
        result.Diagnostic!.FixHint.Should().Be("Guard the action with 'when CollectionField.count > 0', or apply 'notempty' to the collection field declaration");
        result.Diagnostic.RecoverySteps.Should().Equal(
            "Add 'when CollectionField.count > 0' to the transition row guard before this action",
            "Or apply 'notempty' to the collection field declaration");
    }

    [Fact]
    public void Proofs_UnexpectedNull_RecoveryHint_UsesIsSetSyntax()
    {
        var result = ProofsTool.Proofs();

        var fault = result.RuntimeFaults.Should().ContainSingle(f => f.Code == "UnexpectedNull").Subject;
        fault.RecoveryHint.Should().Be("Add the 'optional' modifier to the field declaration, or guard access with 'when Field is set' before use");
        fault.RecoveryHint.Should().NotContain("!= null");
    }
}
