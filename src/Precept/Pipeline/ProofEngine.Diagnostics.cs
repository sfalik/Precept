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

        // Subtype-fixed obligation kinds source their diagnostic code from catalog metadata — the
        // same field CreateFaultSiteLink reads — so the code lives in exactly one place. The arms
        // below keep only message-argument formatting. The genuinely context-determined kinds
        // (Numeric, KeyPresence, and the QualifierChain compound-period override) select their code
        // explicitly because it is a function of the discharge site, not a per-kind constant.
        DiagnosticCode MetaCode() => ProofRequirements.GetMeta(obligation.Requirement.Kind).DiagnosticCode!.Value;

        switch (obligation.Requirement)
        {
            case NumericProofRequirement numeric when TryCreateCollectionSafetyDiagnostic(obligation, out var collectionDiagnostic):
                return collectionDiagnostic;

            case NumericProofRequirement numeric:
                return Diagnostics.Create(GetNumericRequirementDiagnosticCode(obligation, numeric), obligation.Site.Span,
                    DescribeSubject(numeric.Subject, obligation.Site),
                    contextClause);

            case ModifierRequirement modReq:
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    DescribeSubject(modReq.Subject, obligation.Site),
                    modReq.Required.ToString(),
                    usageSuffix);

            case DimensionProofRequirement dimReq:
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    DescribeSubject(dimReq.Subject, obligation.Site),
                    FormatPeriodDimension(dimReq.RequiredDimension),
                    usageSuffix);

            case QualifierCompatibilityProofRequirement qcReq:
                (string Label, string QualifierValue) leftOperand;
                (string Label, string QualifierValue) rightOperand;
                if (obligation.Site is TypedBinaryOp qcBin)
                {
                    leftOperand = DescribeQualifiedExpression(qcBin.Left, qcReq.Axis, semantics, obligation);
                    rightOperand = DescribeQualifiedExpression(qcBin.Right, qcReq.Axis, semantics, obligation);
                }
                else
                {
                    leftOperand = DescribeQualifiedSubject(qcReq.LeftSubject, obligation.Site, qcReq.Axis, semantics, obligation);
                    rightOperand = DescribeQualifiedSubject(qcReq.RightSubject, obligation.Site, qcReq.Axis, semantics, obligation);
                }

                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    leftOperand.Label,
                    rightOperand.Label,
                    qcReq.Axis.ToString(),
                    contextClause,
                    leftOperand.QualifierValue,
                    rightOperand.QualifierValue);

            case QualifierChainProofRequirement chainReq:
                // Composite-period rejection: a period declared by a multi-component basis
                // (`in 'hours + minutes'`) cannot cancel a single-unit denominator. The chain
                // resolves the period's TemporalDimension only for a single basis, so the
                // composite reaches here unproved — emit the precise CompoundPeriodDenominator
                // rather than the generic chain-compatibility diagnostic.
                if (TryCreateCompoundPeriodDenominatorDiagnostic(chainReq, obligation, semantics, out var compoundDiagnostic))
                    return compoundDiagnostic;

                var leftExpression = ResolveSubject(chainReq.LeftSubject, obligation.Site);
                var rightExpression = ResolveSubject(chainReq.RightSubject, obligation.Site);
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    DescribeExpression(leftExpression),
                    DescribeExpression(rightExpression),
                    $"{chainReq.LeftAxis}↔{chainReq.RightAxis}",
                    contextClause,
                    FormatQualifierOrNarrowed(leftExpression, chainReq.LeftAxis, obligation, semantics),
                    FormatQualifierOrNarrowed(rightExpression, chainReq.RightAxis, obligation, semantics));

            case AssignmentQualifierProofRequirement aqReq:
                // PRE0141 (assignment-qualifier compatibility) — re-staged from the type checker to
                // the proof stage: the open-field assignment is discharged by guard-narrowing or
                // surfaces here. The detail clause names what (if anything) the guard narrowed the
                // source to vs. the value the field requires — the narrowed-vs-required signal.
                var aqRequired = ExtractComparableValue(aqReq.TargetQualifier);
                var aqNarrowed = NarrowedValueFromGuard(obligation.Site, aqReq.Axis, obligation, semantics);
                var aqDetail = aqNarrowed is not null
                    ? $"the guard narrows it to '{aqNarrowed}', which does not satisfy the required '{aqRequired}'"
                    : $"no guard narrows it to the required '{aqRequired}'";
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    QualifierAxisLabel(aqReq.Axis),
                    aqReq.TargetFieldName,
                    aqDetail);

            case PresenceProofRequirement presence:
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    DescribeSubject(presence.Subject, obligation.Site),
                    usageSuffix);

            case IntervalContainmentProofRequirement intervalReq:
            {
                var computedStr = obligation.ComputedInterval.HasValue
                    ? $" (computed: {obligation.ComputedInterval.Value})"
                    : string.Empty;
                var displayMin = intervalReq.AuthoredMin ?? intervalReq.DeclaredMin;
                var displayMax = intervalReq.AuthoredMax ?? intervalReq.DeclaredMax;
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    intervalReq.TargetField,
                    $"[{displayMin?.ToString() ?? "−∞"} .. {displayMax?.ToString() ?? "+∞"}]{computedStr}");
            }

            case LengthContainmentProofRequirement lengthReq:
            {
                var literalLength = obligation.Site is TypedLiteral { Value: string s } ? s.Length.ToString() : "?";
                var minStr = lengthReq.DeclaredMinLength?.ToString() ?? "0";
                var maxStr = lengthReq.DeclaredMaxLength?.ToString() ?? "∞";
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    literalLength,
                    lengthReq.TargetField,
                    minStr,
                    maxStr);
            }

            case CountContainmentProofRequirement countReq:
            {
                var minStr = countReq.DeclaredMinCount?.ToString() ?? "0";
                var maxStr = countReq.DeclaredMaxCount?.ToString() ?? "∞";
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    "?",
                    minStr,
                    maxStr,
                    countReq.TargetField);
            }

            case KeyPresenceProofRequirement keyReq:
            {
                var fieldName = obligation.Site is TypedFieldRef fr ? fr.FieldName : "?";
                var code = keyReq.RequireAbsence
                    ? DiagnosticCode.KeyUniquenessGuard
                    : DiagnosticCode.KeyPresenceSafety;
                return Diagnostics.Create(code, obligation.Site.Span,
                    fieldName,
                    "element");
            }

            case IndexBoundsProofRequirement indexReq:
            {
                // Two site shapes — accessor (.at(N)) and action (Insert/RemoveAt).
                // For accessors, the field is access.Object; for actions, Site is the
                // field directly. In both cases, the index expression is recoverable —
                // from access.Arguments for accessors, or by walking the parent context
                // for actions (mirrors the discharge strategy's resolution).
                string fieldName;
                string indexLabel;
                if (obligation.Site is TypedMemberAccess access)
                {
                    fieldName = DescribeExpression(access.Object);
                    indexLabel = access.Arguments.IsDefaultOrEmpty
                        ? "<index>"
                        : DescribeExpression(access.Arguments[0]);
                }
                else if (obligation.Site is TypedFieldRef fr2)
                {
                    fieldName = fr2.FieldName;
                    var indexExpr = FindActionIndexInContext(obligation.Context, fr2.FieldName);
                    indexLabel = indexExpr is null ? "<index>" : DescribeExpression(indexExpr);
                }
                else
                {
                    fieldName = "?";
                    indexLabel = "<index>";
                }
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    fieldName, indexLabel);
            }

            case DimensionalProductProofRequirement:
            {
                // Surface the per-operand unit and the composed dimension name
                // (or vector) so the diagnostic is teachable.
                string leftLabel = "?";
                string rightLabel = "?";
                string productLabel = "?";
                if (obligation.Site is TypedBinaryOp dimBin)
                {
                    leftLabel = DescribeQualifiedExpression(dimBin.Left, QualifierAxis.Unit, semantics).QualifierValue;
                    if (string.IsNullOrWhiteSpace(leftLabel) || leftLabel == "?")
                        leftLabel = DescribeQualifiedExpression(dimBin.Left, QualifierAxis.Dimension, semantics).QualifierValue;
                    rightLabel = DescribeQualifiedExpression(dimBin.Right, QualifierAxis.Unit, semantics).QualifierValue;
                    if (string.IsNullOrWhiteSpace(rightLabel) || rightLabel == "?")
                        rightLabel = DescribeQualifiedExpression(dimBin.Right, QualifierAxis.Dimension, semantics).QualifierValue;
                    productLabel = $"{leftLabel}·{rightLabel}";
                }
                return Diagnostics.Create(MetaCode(), obligation.Site.Span,
                    leftLabel, rightLabel, productLabel);
            }
        }

        throw new InvalidOperationException($"Unexpected proof requirement type '{obligation.Requirement.GetType().FullName}'.");
    }

    /// <summary>
    /// Detects the composite-period denominator case for a qualifier-chain obligation and, if
    /// found, produces the precise <see cref="DiagnosticCode.CompoundPeriodDenominator"/> instead
    /// of the generic chain-compatibility diagnostic. The period is the chain subject whose axis is
    /// <see cref="QualifierAxis.TemporalDimension"/>; the diagnostic fires only when that subject
    /// resolves to a multi-component <see cref="DeclaredQualifierMeta.TemporalUnit"/>. The price
    /// denominator (arg1) is the opposing subject's unit. Single emission — never alongside the
    /// generic code.
    /// </summary>
    private static bool TryCreateCompoundPeriodDenominatorDiagnostic(
        QualifierChainProofRequirement chainReq,
        ProofObligation obligation,
        SemanticIndex semantics,
        out Diagnostic diagnostic)
    {
        diagnostic = default!;

        // Identify which side is the period (the TemporalDimension axis) and which is the price.
        ProofSubject periodSubject;
        ProofSubject denominatorSubject;
        if (chainReq.RightAxis == QualifierAxis.TemporalDimension)
        {
            periodSubject = chainReq.RightSubject;
            denominatorSubject = chainReq.LeftSubject;
        }
        else if (chainReq.LeftAxis == QualifierAxis.TemporalDimension)
        {
            periodSubject = chainReq.LeftSubject;
            denominatorSubject = chainReq.RightSubject;
        }
        else
        {
            return false;
        }

        if (ResolveQualifierOnAxis(periodSubject, QualifierAxis.TemporalUnit, obligation.Site, semantics)
            is not DeclaredQualifierMeta.TemporalUnit { Components.Length: > 1 } compositeUnit)
        {
            return false;
        }

        var compositeBasis = string.Join(" + ", compositeUnit.Components);
        var denominatorQualifier =
            ResolveQualifierOnAxis(denominatorSubject, QualifierAxis.Unit, obligation.Site, semantics)
            ?? ResolveQualifierOnAxis(denominatorSubject, QualifierAxis.Dimension, obligation.Site, semantics);
        var denominatorUnit =
            (denominatorQualifier is not null ? ExtractComparableValue(denominatorQualifier) : null)
            ?? compositeUnit.Components[0];

        diagnostic = Diagnostics.Create(DiagnosticCode.CompoundPeriodDenominator, obligation.Site.Span,
            compositeBasis,
            denominatorUnit);
        return true;
    }

    private static bool TryCreateCollectionSafetyDiagnostic(ProofObligation obligation, out Diagnostic diagnostic)
    {
        diagnostic = default!;

        if (!IsCollectionCountRequirement(obligation.Requirement, out _))
            return false;

        switch (obligation.Site)
        {
            case TypedMemberAccess access:
                // .at() index access → PRE0100 (IndexBoundsGuard) for more specific diagnostics
                if (access.ResolvedAccessor.Name == "at" && access.ResolvedAccessor.ParameterType is not null)
                {
                    diagnostic = Diagnostics.Create(
                        DiagnosticCode.IndexBoundsGuard,
                        obligation.Site.Span,
                        DescribeExpression(access.Object),
                        "index");
                    return true;
                }

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
                TypedMemberAccess { ResolvedAccessor: { Name: "at", ParameterType: not null } } => DiagnosticCode.IndexBoundsGuard,
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
        FieldDefaultContext fdc => $"in the default value of field '{fdc.Field.Name}'",
        ArgDefaultContext adc => $"in the default value of arg '{adc.Arg.EventName}.{adc.Arg.Name}'",
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
        // Numeric obligations have a 1:many diagnostic mapping that requires per-obligation
        // context dispatch — legitimate direct emission per governing policy.
        if (obligation.Requirement is NumericProofRequirement numeric)
            return CreateFaultSiteLink(obligation, GetNumericRequirementDiagnosticCode(obligation, numeric));

        // KeyPresence has a 1:2 mapping (PRE0099 or PRE0101 depending on RequireAbsence)
        if (obligation.Requirement is KeyPresenceProofRequirement keyReq)
        {
            var code = keyReq.RequireAbsence
                ? DiagnosticCode.KeyUniquenessGuard
                : DiagnosticCode.KeyPresenceSafety;
            return CreateFaultSiteLink(obligation, code);
        }

        // All other obligation kinds have a stable 1:1 kind→diagnostic mapping in catalog metadata.
        var meta = ProofRequirements.GetMeta(obligation.Requirement.Kind);
        return CreateFaultSiteLink(obligation, meta.DiagnosticCode!.Value);
    }

    private static FaultSiteLink CreateFaultSiteLink(ProofObligation obligation, DiagnosticCode diagnosticCode)
    {
        // Bijective core: the 1:1 DiagnosticCode→FaultCode rows are the inverse of the
        // [StaticallyPreventable] declarations on FaultCode — derived, not re-listed here.
        var faultCode = StaticallyPreventableMap.TryGetBijectiveFault(diagnosticCode)

            // Collection-safety codes collapse many-to-one onto the shared empty-collection faults:
            // a key-presence/index-bounds failure is, at runtime, an empty-collection access or
            // mutation. This is not a per-member fact the attribute can express.
            ?? diagnosticCode switch
            {
                DiagnosticCode.KeyPresenceSafety => FaultCode.CollectionEmptyOnAccess,
                DiagnosticCode.KeyUniquenessGuard => FaultCode.CollectionEmptyOnMutation,
                DiagnosticCode.IndexBoundsGuard => FaultCode.CollectionEmptyOnAccess,

                // Proof-only obligation families share the conservative runtime backstop: they have
                // no representable runtime fault of their own, so they surface as the generic fault.
                _ => FaultCode.DivisionByZero,
            };

        return new FaultSiteLink(obligation, faultCode, diagnosticCode, obligation.Site.Span);
    }
}
