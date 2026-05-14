using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    // ════════════════════════════════════════════════════════════════════════
    //  Slice 11: String Length Containment and Collection Count Containment
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to prove a <see cref="LengthContainmentProofRequirement"/> obligation.
    /// Strategy: if the obligation's site is a string literal, compare its character count
    /// against the declared minlength/maxlength. Non-literal sites cannot be statically resolved.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the literal string satisfies the declared length bounds; <c>false</c>
    /// if the bounds are violated. When <paramref name="site"/> is not a string literal, returns
    /// <c>null</c> (unresolved — caller should leave the obligation in <see cref="ProofDisposition.Unresolved"/>).
    /// </returns>
    internal static bool? TryLengthContainmentProof(LengthContainmentProofRequirement req, TypedExpression site)
    {
        if (site is not TypedLiteral { ResultType: TypeKind.String, Value: string s })
            return null; // non-literal: unresolved

        int length = s.Length;

        if (req.DeclaredMinLength.HasValue && length < req.DeclaredMinLength.Value)
            return false; // too short
        if (req.DeclaredMaxLength.HasValue && length > req.DeclaredMaxLength.Value)
            return false; // too long

        return true;
    }

    /// <summary>
    /// Attempts to prove a <see cref="CountContainmentProofRequirement"/> obligation.
    /// V1: always returns <c>null</c> (unresolved) because collection set assignments
    /// are rejected by the type checker, and add/remove actions do not yet generate obligations.
    /// </summary>
    internal static bool? TryCountContainmentProof(CountContainmentProofRequirement _, TypedExpression __)
        => null;
}
