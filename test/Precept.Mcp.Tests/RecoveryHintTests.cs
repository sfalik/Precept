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

        result.Should().Contain("**CollectionEmptyOnMutation**");
        result.Should().Contain("Guard the action with 'when CollectionField.count > 0' in the transition row guard clause, or apply 'notempty' to the collection field declaration");
    }

    [Fact]
    public void Diagnostic_UnguardedCollectionMutation_Hints_MatchCurrentRuntimeBehavior()
    {
        var result = DiagnosticTool.Diagnostic("UnguardedCollectionMutation");

        result.Should().Contain("Guard the action with 'when CollectionField.count > 0', or apply 'notempty' to the collection field declaration");
        result.Should().Contain("Add 'when CollectionField.count > 0' to the transition row guard before this action");
        result.Should().Contain("Or apply 'notempty' to the collection field declaration");
    }

    [Fact]
    public void Proofs_UnexpectedNull_RecoveryHint_UsesIsSetSyntax()
    {
        var result = ProofsTool.Proofs();

        result.Should().Contain("**UnexpectedNull**");
        result.Should().Contain("Add the 'optional' modifier to the field declaration, or guard access with 'when Field is set' before use");
        result.Should().NotContain("!= null");
    }
}