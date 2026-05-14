using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    //  S9 — Diagnostic Emission and FaultSiteLink Production
    // ════════════════════════════════════════════════════════════════════════════

    private static Diagnostic CreateDiagnostic(ProofObligation obligation, SemanticIndex semantics)
    {
        var contextClause = FormatContextClause(obligation.Context);
        var usageSuffix = FormatUsageContextSuffix(obligation.Context);

        switch (obligation.Requirement)
        {
            case NumericProofRequirement numeric when TryCreateCollectionSafetyDiagnostic(obligation, out var collectionDiagnostic):
                return collectionDiagnostic;

            case NumericProofRequirement numeric:
                return Diagnostics.Create(GetNumericRequirementDiagnosticCode(obligation, numeric), obligation.Site.Span,
                    DescribeSubject(numeric.Subject, obligation.Site),
                    contextClause);

            case ModifierRequirement modReq:
                return Diagnostics.Create(DiagnosticCode.UnprovedModifierRequirement, obligation.Site.Span,
                    DescribeSubject(modReq.Subject, obligation.Site),
                    modReq.Required.ToString(),
                    usageSuffix);

            case DimensionProofRequirement dimReq:
                return Diagnostics.Create(DiagnosticCode.UnprovedDimensionRequirement, obligation.Site.Span,
                    DescribeSubject(dimReq.Subject, obligation.Site),
                    FormatPeriodDimension(dimReq.RequiredDimension),
                    usageSuffix);

            case QualifierCompatibilityProofRequirement qcReq:
                (string Label, string QualifierValue) leftOperand;
                (string Label, string QualifierValue) rightOperand;
                if (obligation.Site is TypedBinaryOp qcBin)
                {
                    leftOperand = DescribeQualifiedExpression(qcBin.Left, qcReq.Axis, semantics);
                    rightOperand = DescribeQualifiedExpression(qcBin.Right, qcReq.Axis, semantics);
                }
                else
                {
                    leftOperand = DescribeQualifiedSubject(qcReq.LeftSubject, obligation.Site, qcReq.Axis, semantics);
                    rightOperand = DescribeQualifiedSubject(qcReq.RightSubject, obligation.Site, qcReq.Axis, semantics);
                }

                return Diagnostics.Create(DiagnosticCode.UnprovedQualifierCompatibility, obligation.Site.Span,
                    leftOperand.Label,
                    rightOperand.Label,
                    qcReq.Axis.ToString(),
                    contextClause,
                    leftOperand.QualifierValue,
                    rightOperand.QualifierValue);

            case QualifierChainProofRequirement chainReq:
                var leftExpression = ResolveSubject(chainReq.LeftSubject, obligation.Site);
                var rightExpression = ResolveSubject(chainReq.RightSubject, obligation.Site);
                return Diagnostics.Create(DiagnosticCode.UnprovedQualifierCompatibility, obligation.Site.Span,
                    DescribeExpression(leftExpression),
                    DescribeExpression(rightExpression),
                    $"{chainReq.LeftAxis}↔{chainReq.RightAxis}",
                    contextClause,
                    FormatQualifierValue(leftExpression is null ? null : ResolveQualifierFromExpression(leftExpression, chainReq.LeftAxis, semantics)),
                    FormatQualifierValue(rightExpression is null ? null : ResolveQualifierFromExpression(rightExpression, chainReq.RightAxis, semantics)));

            case PresenceProofRequirement presence:
                return Diagnostics.Create(DiagnosticCode.UnprovedPresenceRequirement, obligation.Site.Span,
                    DescribeSubject(presence.Subject, obligation.Site),
                    usageSuffix);

            case IntervalContainmentProofRequirement intervalReq:
            {
                var computedStr = obligation.ComputedInterval.HasValue
                    ? $" (computed: {obligation.ComputedInterval.Value})"
                    : string.Empty;
                return Diagnostics.Create(DiagnosticCode.NumericOverflow, obligation.Site.Span,
                    intervalReq.TargetField,
                    $"[{intervalReq.DeclaredMin?.ToString() ?? "−∞"} .. {intervalReq.DeclaredMax?.ToString() ?? "+∞"}]{computedStr}");
            }

            case LengthContainmentProofRequirement lengthReq:
            {
                var literalLength = obligation.Site is TypedLiteral { Value: string s } ? s.Length.ToString() : "?";
                var minStr = lengthReq.DeclaredMinLength?.ToString() ?? "0";
                var maxStr = lengthReq.DeclaredMaxLength?.ToString() ?? "∞";
                return Diagnostics.Create(DiagnosticCode.LengthBoundViolation, obligation.Site.Span,
                    literalLength,
                    lengthReq.TargetField,
                    minStr,
                    maxStr);
            }

            case CountContainmentProofRequirement countReq:
            {
                var minStr = countReq.DeclaredMinCount?.ToString() ?? "0";
                var maxStr = countReq.DeclaredMaxCount?.ToString() ?? "∞";
                return Diagnostics.Create(DiagnosticCode.CountBoundViolation, obligation.Site.Span,
                    "?",
                    minStr,
                    maxStr,
                    countReq.TargetField);
            }
        }

        throw new InvalidOperationException($"Unexpected proof requirement type '{obligation.Requirement.GetType().FullName}'.");
    }

    private static bool TryCreateCollectionSafetyDiagnostic(ProofObligation obligation, out Diagnostic diagnostic)
    {
        diagnostic = default!;

        if (!IsCollectionCountRequirement(obligation.Requirement, out _))
            return false;

        switch (obligation.Site)
        {
            case TypedMemberAccess access:
                diagnostic = Diagnostics.Create(
                    DiagnosticCode.UnguardedCollectionAccess,
                    obligation.Site.Span,
                    DescribeExpression(access.Object),
                    access.ResolvedAccessor.Name);
                return true;

            case TypedFieldRef fieldRef:
                diagnostic = Diagnostics.Create(
                    DiagnosticCode.UnguardedCollectionMutation,
                    obligation.Site.Span,
                    fieldRef.FieldName,
                    "this mutation action");
                return true;

            default:
                return false;
        }
    }

    private static bool IsCollectionCountRequirement(
        ProofRequirement requirement,
        out NumericProofRequirement? numericRequirement)
    {
        if (requirement is NumericProofRequirement numeric
            && numeric.Subject is SelfSubject { Accessor: { Name: "count" } }
            && numeric.Comparison == OperatorKind.GreaterThan
            && numeric.Threshold == 0m)
        {
            numericRequirement = numeric;
            return true;
        }

        numericRequirement = null;
        return false;
    }

    private static DiagnosticCode GetNumericRequirementDiagnosticCode(ProofObligation obligation, NumericProofRequirement requirement)
    {
        if (IsCollectionCountRequirement(requirement, out _))
        {
            return obligation.Site switch
            {
                TypedMemberAccess => DiagnosticCode.UnguardedCollectionAccess,
                TypedFieldRef => DiagnosticCode.UnguardedCollectionMutation,
                _ => DiagnosticCode.DivisionByZero,
            };
        }

        return requirement.Comparison == OperatorKind.GreaterThanOrEqual && requirement.Threshold == 0m
            ? DiagnosticCode.SqrtOfNegative
            : DiagnosticCode.DivisionByZero;
    }

    private static string FormatContextDescription(ObligationContext context) => context switch
    {
        TransitionRowContext trc => $"on event '{trc.Row.EventName}' from state '{trc.Row.FromState ?? "*"}'",
        EventHandlerContext ehc => $"event handler '{ehc.Handler.EventName}'",
        StateHookContext shc => $"state hook for '{shc.Hook.StateName}'",
        ConstraintContext cc => cc.Constraint switch
        {
            RuleIdentity ri => $"rule at index {ri.RuleIndex}",
            EnsureIdentity { AnchorName: { } anchorName } => $"ensure for '{anchorName}'",
            EnsureIdentity => "global ensure",
            _ => "constraint"
        },
        FieldExpressionContext fec => $"field '{fec.Field.Name}' computed expression",
        _ => "unknown context"
    };

    private static string FormatUsageContextSuffix(ObligationContext context)
    {
        var usageDescription = FormatUsageContextDescription(context);
        return usageDescription == "here"
            ? " (used here)"
            : $" (used {usageDescription})";
    }

    private static string FormatContextClause(ObligationContext context)
    {
        var usageDescription = FormatUsageContextDescription(context);
        return usageDescription == "here"
            ? string.Empty
            : $" {usageDescription}";
    }

    private static string FormatUsageContextDescription(ObligationContext context) => context switch
    {
        TransitionRowContext trc => $"on event '{trc.Row.EventName}' from state '{trc.Row.FromState ?? "*"}'",
        EventHandlerContext ehc => $"in event handler '{ehc.Handler.EventName}'",
        StateHookContext shc => $"in state hook for '{shc.Hook.StateName}'",
        ConstraintContext cc => cc.Constraint switch
        {
            RuleIdentity ri => $"while evaluating rule at index {ri.RuleIndex}",
            EnsureIdentity { AnchorName: { } anchorName } => $"while evaluating ensure for '{anchorName}'",
            EnsureIdentity => "while evaluating the global ensure",
            _ => "here"
        },
        FieldExpressionContext fec => $"in the computed expression for field '{fec.Field.Name}'",
        _ => "here"
    };

    private static string FormatPeriodDimension(PeriodDimension dimension) => dimension switch
    {
        PeriodDimension.Date => "date",
        PeriodDimension.Time => "time",
        _ => dimension.ToString().ToLowerInvariant(),
    };

    private static FaultSiteLink CreateFaultSiteLink(ProofObligation obligation)
    {
        switch (obligation.Requirement)
        {
            case NumericProofRequirement numeric:
                return CreateFaultSiteLink(obligation, GetNumericRequirementDiagnosticCode(obligation, numeric));
            case ModifierRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedModifierRequirement);
            case DimensionProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedDimensionRequirement);
            case QualifierCompatibilityProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedQualifierCompatibility);
            case QualifierChainProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedQualifierCompatibility);
            case PresenceProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.UnprovedPresenceRequirement);
            case IntervalContainmentProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.NumericOverflow);
            case LengthContainmentProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.LengthBoundViolation);
            case CountContainmentProofRequirement:
                return CreateFaultSiteLink(obligation, DiagnosticCode.CountBoundViolation);
        }

        throw new InvalidOperationException($"Unexpected proof requirement type '{obligation.Requirement.GetType().FullName}'.");
    }

    private static FaultSiteLink CreateFaultSiteLink(ProofObligation obligation, DiagnosticCode diagnosticCode)
    {
        var faultCode = diagnosticCode switch
        {
            DiagnosticCode.DivisionByZero => FaultCode.DivisionByZero,
            DiagnosticCode.SqrtOfNegative => FaultCode.SqrtOfNegative,
            DiagnosticCode.UnguardedCollectionAccess => FaultCode.CollectionEmptyOnAccess,
            DiagnosticCode.UnguardedCollectionMutation => FaultCode.CollectionEmptyOnMutation,
            DiagnosticCode.NumericOverflow => FaultCode.NumericOverflow,
            DiagnosticCode.LengthBoundViolation => FaultCode.LengthBoundViolation,
            DiagnosticCode.CountBoundViolation => FaultCode.CountBoundViolation,
            _ => FaultCode.DivisionByZero // Proof-only obligation families still share the existing conservative runtime backstop.
        };

        return new FaultSiteLink(obligation, faultCode, diagnosticCode, obligation.Site.Span);
    }
}
