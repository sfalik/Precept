using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 1 of the index-bounds design: TypedMemberAccess.Arguments preserves
/// the resolved arguments from method-call sites (e.g., .at(N), .countof(E)).
/// The Arguments slot is the architectural prerequisite for IndexBoundsProofRequirement
/// discharge — without it, the proof engine cannot reach the index parameter
/// via the standard ParamSubject framework.
/// </summary>
public class MethodCallArgumentsTests
{
    [Fact]
    public void AtAccessor_PreservesIndexArgument_OnTypedMemberAccess()
    {
        var precept = """
            precept Repro
            field Items as list of string
            field Picked as string default ""
            state Open initial

            event Pick(Index as integer)
            from Open on Pick when Pick.Index >= 0 and Pick.Index < Items.count
                -> set Picked = Items.at(Pick.Index)
                -> no transition
            """;

        var (index, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(d => d.Severity == Severity.Error);

        var memberAccessCalls = FindAtAccessorCalls(index);
        memberAccessCalls.Should().NotBeEmpty(because: ".at(Pick.Index) should be resolved to a TypedMemberAccess");

        memberAccessCalls.Should().AllSatisfy(ma =>
        {
            ma.Arguments.IsDefaultOrEmpty.Should().BeFalse(
                because: ".at(N) preserves its index argument N on TypedMemberAccess.Arguments");
            ma.Arguments.Length.Should().Be(1,
                because: ".at takes a single integer index parameter");
            ma.Arguments[0].ResultType.Should().Be(TypeKind.Integer);
        });
    }

    [Fact]
    public void BareMemberAccess_HasEmptyArguments()
    {
        var precept = """
            precept Repro
            field Items as list of string
            field Cnt as integer default 0
            state Open initial

            event Snap
            from Open on Snap when Items.count > 0
                -> set Cnt = Items.count
                -> no transition
            """;

        var (index, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(d => d.Severity == Severity.Error);

        // Bare .count member access — no arguments, slot stays default-empty.
        var countAccesses = FindCountAccessorCalls(index);
        countAccesses.Should().NotBeEmpty();
        countAccesses.Should().AllSatisfy(ma =>
            ma.Arguments.IsDefaultOrEmpty.Should().BeTrue(
                because: "bare member access doesn't populate Arguments"));
    }

    private static System.Collections.Generic.List<TypedMemberAccess> FindAtAccessorCalls(SemanticIndex index)
    {
        var found = new System.Collections.Generic.List<TypedMemberAccess>();
        foreach (var row in index.TransitionRows.OfType<TypedTransitionRowSuccess>())
            foreach (var action in row.Actions)
                CollectMemberAccesses(action, "at", found);
        return found;
    }

    private static System.Collections.Generic.List<TypedMemberAccess> FindCountAccessorCalls(SemanticIndex index)
    {
        var found = new System.Collections.Generic.List<TypedMemberAccess>();
        foreach (var row in index.TransitionRows.OfType<TypedTransitionRowSuccess>())
            foreach (var action in row.Actions)
                CollectMemberAccesses(action, "count", found);
        return found;
    }

    private static void CollectMemberAccesses(TypedAction action, string accessorName, System.Collections.Generic.List<TypedMemberAccess> found)
    {
        if (action is TypedInputAction input)
        {
            CollectFromExpression(input.InputExpression, accessorName, found);
            if (input.SecondaryExpression is not null)
                CollectFromExpression(input.SecondaryExpression, accessorName, found);
        }
    }

    private static void CollectFromExpression(TypedExpression expr, string accessorName, System.Collections.Generic.List<TypedMemberAccess> found)
    {
        switch (expr)
        {
            case TypedMemberAccess ma:
                if (ma.ResolvedAccessor.Name == accessorName)
                    found.Add(ma);
                CollectFromExpression(ma.Object, accessorName, found);
                break;
            case TypedBinaryOp bin:
                CollectFromExpression(bin.Left, accessorName, found);
                CollectFromExpression(bin.Right, accessorName, found);
                break;
        }
    }
}
