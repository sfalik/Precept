using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;
using PreceptDiagnostic = Precept.Language.Diagnostic;
using PreceptDiagnosticCode = Precept.Language.DiagnosticCode;

namespace Precept.LanguageServer.Handlers;

internal static class RichHoverFactory
{
    internal static bool TryCreateProofHover(Compilation compilation, Position position, Token token, out Hover hover)
    {
        if (TryCreateProofDiagnosticHover(compilation, position, out hover))
        {
            return true;
        }

        if (TryCreateProofExpressionHover(compilation, position, out hover))
        {
            return true;
        }

        hover = null!;
        return false;
    }

    internal static Hover CreateFieldHover(Compilation compilation, TypedField field, SourceSpan span) =>
        MakeHover(CreateFieldMarkdown(compilation, field), span);

    internal static Hover CreateStateHover(Compilation compilation, TypedState state, SourceSpan span) =>
        MakeHover(CreateStateMarkdown(compilation, state), span);

    internal static Hover CreateEventHover(Compilation compilation, TypedEvent evt, SourceSpan span) =>
        MakeHover(CreateEventMarkdown(compilation, evt), span);

    internal static Hover CreateArgumentHover(TypedArg arg, SourceSpan span) =>
        MakeHover(CreateArgumentMarkdown(arg), span);

    internal static bool TryCreateHover(Compilation compilation, Position position, Token token, out Hover hover)
    {
        if (TryCreateRuleHover(compilation, position, out hover))
        {
            return true;
        }

        if (TryCreateEnsureHover(compilation, position, out hover))
        {
            return true;
        }

        // Reject rows are represented inside TransitionRows too, so they must win before
        // the generic transition template sees the same row span.
        if (TryCreateRejectHover(compilation, position, out hover))
        {
            return true;
        }

        if (TryCreateTransitionHover(compilation, position, out hover))
        {
            return true;
        }

        if (TryCreateAccessHover(compilation, position, out hover))
        {
            return true;
        }

        if (TryCreateOmitHover(compilation, position, out hover))
        {
            return true;
        }

        if (TryCreateQualifierHover(compilation, position, out hover))
        {
            return true;
        }

        if (TryCreateSymbolHover(compilation, position, token, out hover))
        {
            return true;
        }

        hover = null!;
        return false;
    }

    private static bool TryCreateRuleHover(Compilation compilation, Position position, out Hover hover)
    {
        var rules = compilation.Semantics.Rules;
        for (var index = 0; index < rules.Length; index++)
        {
            var rule = rules[index];
            if (!SymbolNavigation.Contains(rule.Syntax.Span, position))
            {
                continue;
            }

            hover = MakeHover(CreateRuleMarkdown(compilation, rule, new RuleIdentity(index)), rule.Syntax.Span);
            return true;
        }

        hover = null!;
        return false;
    }

    private static bool TryCreateEnsureHover(Compilation compilation, Position position, out Hover hover)
    {
        var ensures = compilation.Semantics.Ensures;
        for (var index = 0; index < ensures.Length; index++)
        {
            var ensure = ensures[index];
            if (!SymbolNavigation.Contains(ensure.Syntax.Span, position))
            {
                continue;
            }

            hover = MakeHover(
                CreateEnsureMarkdown(compilation, ensure, new EnsureIdentity(ensure.Kind, ensure.AnchorState ?? ensure.AnchorEvent, index)),
                ensure.Syntax.Span);
            return true;
        }

        hover = null!;
        return false;
    }

    private static bool TryCreateRejectHover(Compilation compilation, Position position, out Hover hover)
    {
        foreach (var row in compilation.Semantics.TransitionRows)
        {
            if (row.Outcome != TransitionRowOutcome.Reject || !SymbolNavigation.Contains(row.RowSpan, position))
            {
                continue;
            }

            hover = MakeHover(CreateRejectMarkdown(compilation, row), row.RowSpan);
            return true;
        }

        hover = null!;
        return false;
    }

    private static bool TryCreateTransitionHover(Compilation compilation, Position position, out Hover hover)
    {
        foreach (var row in compilation.Semantics.TransitionRows)
        {
            if (!SymbolNavigation.Contains(row.RowSpan, position))
            {
                continue;
            }

            hover = MakeHover(CreateTransitionMarkdown(compilation, row), row.RowSpan);
            return true;
        }

        hover = null!;
        return false;
    }

    private static bool TryCreateAccessHover(Compilation compilation, Position position, out Hover hover)
    {
        foreach (var access in GetAccessDeclarations(compilation))
        {
            if (!SymbolNavigation.Contains(access.Span, position))
            {
                continue;
            }

            hover = MakeHover(CreateAccessMarkdown(compilation, access), access.Span);
            return true;
        }

        hover = null!;
        return false;
    }

    private static bool TryCreateOmitHover(Compilation compilation, Position position, out Hover hover)
    {
        foreach (var omit in GetOmitDeclarations(compilation))
        {
            if (!SymbolNavigation.Contains(omit.Span, position))
            {
                continue;
            }

            hover = MakeHover(CreateOmitMarkdown(compilation, omit), omit.Span);
            return true;
        }

        hover = null!;
        return false;
    }

    private static bool TryCreateQualifierHover(Compilation compilation, Position position, out Hover hover)
    {
        if (TryFindQualifierAt(compilation, position, out var info))
        {
            hover = MakeHover(CreateQualifierMarkdown(compilation, info), info.Span);
            return true;
        }

        hover = null!;
        return false;
    }

    private static bool TryCreateSymbolHover(Compilation compilation, Position position, Token token, out Hover hover)
    {
        hover = null!;

        if (token.Kind != TokenKind.Identifier || !SymbolNavigation.TryFindOccurrence(compilation, position, out var occurrence))
        {
            return false;
        }

        Hover? symbolHover = occurrence switch
        {
            FieldOccurrence field => CreateFieldHover(compilation, field.Field, token.Span),
            StateOccurrence state => CreateStateHover(compilation, state.State, token.Span),
            EventOccurrence evt => CreateEventHover(compilation, evt.Event, token.Span),
            ArgOccurrence arg => CreateArgumentHover(arg.Arg, token.Span),
            _ => null,
        };

        hover = symbolHover!;
        return symbolHover is not null;
    }

    private static bool TryCreateProofDiagnosticHover(Compilation compilation, Position position, out Hover hover)
    {
        var diagnostics = compilation.Diagnostics
            .Where(diagnostic => diagnostic.Stage == DiagnosticStage.Proof
                && diagnostic.Severity is Severity.Warning or Severity.Error
                && SymbolNavigation.Contains(diagnostic.Span, position))
            .OrderBy(diagnostic => GetSpanWidth(diagnostic.Span))
            .ToImmutableArray();
        if (diagnostics.IsEmpty)
        {
            hover = null!;
            return false;
        }

        var diagnostic = diagnostics[0];
        var obligation = FindProofObligationForDiagnostic(compilation, diagnostic, position);
        var span = obligation?.Site.Span ?? diagnostic.Span;
        hover = MakeHover(CreateProofDiagnosticMarkdown(compilation, diagnostic, obligation), span);
        return true;
    }

    private static bool TryCreateProofExpressionHover(Compilation compilation, Position position, out Hover hover)
    {
        var obligations = compilation.Proof.Obligations
            .Where(obligation => obligation.Site is TypedBinaryOp
                && SymbolNavigation.Contains(obligation.Site.Span, position))
            .OrderBy(obligation => GetSpanWidth(obligation.Site.Span))
            .ToImmutableArray();
        if (obligations.IsEmpty)
        {
            hover = null!;
            return false;
        }

        var site = (TypedBinaryOp)obligations[0].Site;
        var siteObligations = compilation.Proof.Obligations
            .Where(obligation => obligation.Site.Span == site.Span)
            .ToImmutableArray();
        var primary = SelectPrimaryProofObligation(siteObligations);
        if (primary is null)
        {
            hover = null!;
            return false;
        }

        hover = MakeHover(CreateProofExpressionMarkdown(compilation, site, primary), site.Span);
        return true;
    }

    private static ProofObligation? FindProofObligationForDiagnostic(Compilation compilation, PreceptDiagnostic diagnostic, Position position)
    {
        if (Enum.TryParse<PreceptDiagnosticCode>(diagnostic.Code, out var diagnosticCode))
        {
            var fromLinks = compilation.Proof.FaultSiteLinks
                .Where(link => link.DiagnosticCode == diagnosticCode && SymbolNavigation.Contains(link.Site, position))
                .Select(link => link.Obligation)
                .OrderBy(obligation => GetSpanWidth(obligation.Site.Span))
                .FirstOrDefault();
            if (fromLinks is not null)
            {
                return fromLinks;
            }

            return compilation.Proof.Obligations
                .Where(obligation => obligation.EmittedDiagnostic == diagnosticCode && Overlaps(obligation.Site.Span, diagnostic.Span))
                .OrderBy(obligation => GetSpanWidth(obligation.Site.Span))
                .FirstOrDefault();
        }

        return null;
    }

    private static ProofObligation? SelectPrimaryProofObligation(ImmutableArray<ProofObligation> obligations) => obligations
        .OrderByDescending(obligation => obligation.Disposition == ProofDisposition.Unresolved)
        .ThenByDescending(obligation => obligation.Requirement is QualifierCompatibilityProofRequirement or QualifierChainProofRequirement)
        .ThenBy(obligation => GetSpanWidth(obligation.Site.Span))
        .FirstOrDefault();

    private static string CreateProofDiagnosticMarkdown(Compilation compilation, PreceptDiagnostic diagnostic, ProofObligation? obligation)
    {
        if (obligation is null || !Enum.TryParse<PreceptDiagnosticCode>(diagnostic.Code, out var diagnosticCode))
        {
            return CreateGenericProofDiagnosticMarkdown(diagnostic);
        }

        return obligation.Requirement switch
        {
            QualifierCompatibilityProofRequirement qualifierCompatibility => CreateQualifierProofDiagnosticMarkdown(compilation, diagnosticCode, obligation, qualifierCompatibility),
            QualifierChainProofRequirement qualifierChain => CreateQualifierChainProofDiagnosticMarkdown(compilation, diagnosticCode, obligation, qualifierChain),
            PresenceProofRequirement presence => CreatePresenceProofDiagnosticMarkdown(compilation, diagnosticCode, obligation, presence),
            _ => CreateGenericProofDiagnosticMarkdown(compilation, diagnosticCode, diagnostic, obligation),
        };
    }

    private static string CreateProofExpressionMarkdown(Compilation compilation, TypedBinaryOp expression, ProofObligation obligation) => obligation.Requirement switch
    {
        QualifierCompatibilityProofRequirement qualifierCompatibility => CreateQualifierProofExpressionMarkdown(compilation, expression, obligation, qualifierCompatibility),
        QualifierChainProofRequirement qualifierChain => CreateQualifierChainProofExpressionMarkdown(compilation, expression, obligation, qualifierChain),
        _ => CreateGenericProofExpressionMarkdown(compilation, expression, obligation),
    };

    private static string CreateQualifierProofDiagnosticMarkdown(
        Compilation compilation,
        PreceptDiagnosticCode diagnosticCode,
        ProofObligation obligation,
        QualifierCompatibilityProofRequirement requirement)
    {
        var expression = obligation.Site as TypedBinaryOp;
        var lines = new List<string>
        {
            $"⚠️ `{FormatDiagnosticCode(diagnosticCode)}` · {GetQualifierDiagnosticSummary(requirement.Axis)}",
            $"🔬 {FormatProofUse(compilation, obligation.Context)} · `{EscapeInline(DescribeProofSite(compilation, obligation.Site))}`",
            expression is null
                ? EscapePlain(obligation.Requirement.Description)
                : DescribeQualifierGapEvidenceLine(compilation, expression.Left, expression.Right, requirement.Axis),
        };

        return string.Join("\n", lines);
    }

    private static string CreateQualifierChainProofDiagnosticMarkdown(
        Compilation compilation,
        PreceptDiagnosticCode diagnosticCode,
        ProofObligation obligation,
        QualifierChainProofRequirement requirement)
    {
        var expression = obligation.Site as TypedBinaryOp;
        var lines = new List<string>
        {
            $"**{FormatDiagnosticCode(diagnosticCode)} — Cannot prove qualifier compatibility**",
            $"Verdict: Cannot prove the required qualifier chain across this expression",
            $"Context: {DescribeProofContext(compilation, obligation.Context)}",
            $"Expression: `{EscapeInline(DescribeProofSite(compilation, obligation.Site))}`",
            $"Requirement: {DescribeQualifierChainRequirement(requirement)}",
        };

        if (expression is not null)
        {
            AppendQualifierProofEvidenceLines(lines, compilation, expression.Left, expression.Right, requirement.LeftAxis, obligation, includeResultLines: false, leftAxis: requirement.LeftAxis, rightAxis: requirement.RightAxis);
        }

        lines.Add("Status: unresolved");
        lines.Add($"Reason: {ExplainUnresolvedQualifierReason(compilation, expression?.Left, expression?.Right, requirement.LeftAxis)}");
        lines.Add($"Fix: {Diagnostics.GetMeta(diagnosticCode).FixHint}");
        return string.Join("\n\n", lines);
    }

    private static string CreatePresenceProofDiagnosticMarkdown(
        Compilation compilation,
        PreceptDiagnosticCode diagnosticCode,
        ProofObligation obligation,
        PresenceProofRequirement requirement)
    {
        var subjectExpression = ResolveProofSubjectExpression(requirement.Subject, obligation.Site);
        var subject = subjectExpression is null ? DescribeProofSite(compilation, obligation.Site) : DescribeProofSite(compilation, subjectExpression);
        var lines = new List<string>
        {
            $"**{FormatDiagnosticCode(diagnosticCode)} — Cannot prove presence**",
            $"Verdict: Cannot prove `{EscapeInline(subject)}` is present before this access",
            $"Context: {DescribeProofContext(compilation, obligation.Context)}",
            $"Expression: `{EscapeInline(DescribeProofSite(compilation, obligation.Site))}`",
            "Requirement: optional fields must be proven present before access",
            $"Subject: `{EscapeInline(subject)}`",
        };

        if (TryGetPresenceDescription(subjectExpression, compilation.Semantics, out var presence))
        {
            lines.Add($"Declared presence: {presence}");
        }

        lines.Add("Status: unresolved");
        lines.Add("Reason: no guard or earlier assignment proves the field is set on this path");
        lines.Add($"Fix: {Diagnostics.GetMeta(diagnosticCode).FixHint}");
        return string.Join("\n\n", lines);
    }

    private static string CreateGenericProofDiagnosticMarkdown(PreceptDiagnostic diagnostic)
    {
        var title = diagnostic.Message.Replace("'", "`").Replace("\r", string.Empty).Replace("\n", " ");
        return string.Join("\n\n", new[]
        {
            $"**proof diagnostic** `{EscapeInline(diagnostic.Code)}`",
            $"Verdict: {title}",
            "Status: unresolved",
        });
    }

    private static string CreateGenericProofDiagnosticMarkdown(Compilation compilation, PreceptDiagnosticCode diagnosticCode, PreceptDiagnostic diagnostic, ProofObligation obligation)
    {
        return string.Join("\n\n", new[]
        {
            $"**{FormatDiagnosticCode(diagnosticCode)} — {EscapeInline(diagnostic.Message)}**",
            $"Context: {DescribeProofContext(compilation, obligation.Context)}",
            $"Expression: `{EscapeInline(DescribeProofSite(compilation, obligation.Site))}`",
            "Status: unresolved",
            $"Fix: {Diagnostics.GetMeta(diagnosticCode).FixHint}",
        });
    }

    private static string CreateQualifierProofExpressionMarkdown(
        Compilation compilation,
        TypedBinaryOp expression,
        ProofObligation obligation,
        QualifierCompatibilityProofRequirement requirement)
    {
        var resultQualifier = ResolveQualifierFromExpression(obligation.Site, requirement.Axis, compilation.Semantics);
        var lines = obligation.Disposition == ProofDisposition.Proved
            ? new List<string>
            {
                $"✅ Proven · result keeps {FormatProofQualifierValue(resultQualifier, GetQualifierAxisName(requirement.Axis))}",
                $"🔬 `{EscapeInline(DescribeProofSite(compilation, expression))}`",
                CreateQualifierProvenEvidenceLine(compilation, expression.Left, expression.Right, resultQualifier, requirement.Axis),
            }
            : new List<string>
            {
                $"⚠️ Gap · {GetQualifierGapSummary(requirement.Axis)}",
                $"🔬 `{EscapeInline(DescribeProofSite(compilation, expression))}`",
                DescribeQualifierGapEvidenceLine(compilation, expression.Left, expression.Right, requirement.Axis),
            };

        return string.Join("\n", lines);
    }

    private static string CreateQualifierChainProofExpressionMarkdown(
        Compilation compilation,
        TypedBinaryOp expression,
        ProofObligation obligation,
        QualifierChainProofRequirement requirement)
    {
        var lines = new List<string>
        {
            $"**expression** `{EscapeInline(DescribeProofSite(compilation, expression))}`",
            $"Status: {DescribeProofStatus(obligation)}",
            $"Context: {DescribeProofContext(compilation, obligation.Context)}",
            $"Requirement: {DescribeQualifierChainRequirement(requirement)}",
        };

        AppendQualifierProofEvidenceLines(lines, compilation, expression.Left, expression.Right, requirement.LeftAxis, obligation, includeResultLines: false, leftAxis: requirement.LeftAxis, rightAxis: requirement.RightAxis);

        if (obligation.Disposition == ProofDisposition.Proved && obligation.Strategy is { } strategy)
        {
            lines.Add($"Proof strategy: {HumanizeProofStrategy(strategy)}");
        }
        else
        {
            lines.Add($"Reason: {ExplainUnresolvedQualifierReason(compilation, expression.Left, expression.Right, requirement.LeftAxis)}");
            if (obligation.EmittedDiagnostic is { } diagnosticCode)
            {
                lines.Add($"Fix: {Diagnostics.GetMeta(diagnosticCode).FixHint}");
            }
        }

        return string.Join("\n\n", lines);
    }

    private static string CreateGenericProofExpressionMarkdown(Compilation compilation, TypedBinaryOp expression, ProofObligation obligation)
    {
        var lines = new List<string>
        {
            $"**expression** `{EscapeInline(DescribeProofSite(compilation, expression))}`",
            $"Status: {DescribeProofStatus(obligation)}",
            $"Context: {DescribeProofContext(compilation, obligation.Context)}",
            $"Requirement: {obligation.Requirement.Description}",
        };

        if (obligation.Disposition == ProofDisposition.Proved && obligation.Strategy is { } strategy)
        {
            lines.Add($"Proof strategy: {HumanizeProofStrategy(strategy)}");
        }
        else if (obligation.EmittedDiagnostic is { } diagnosticCode)
        {
            lines.Add($"Fix: {Diagnostics.GetMeta(diagnosticCode).FixHint}");
        }

        return string.Join("\n\n", lines);
    }

    private static string GetQualifierDiagnosticSummary(QualifierAxis axis) => axis switch
    {
        QualifierAxis.Currency or QualifierAxis.FromCurrency or QualifierAxis.ToCurrency => "Can't confirm currencies match",
        QualifierAxis.Unit => "Can't confirm units match",
        QualifierAxis.Dimension => "Can't confirm dimensions match",
        QualifierAxis.Timezone => "Can't confirm timezones match",
        QualifierAxis.TemporalDimension => "Can't confirm temporal dimensions match",
        QualifierAxis.TemporalUnit => "Can't confirm temporal units match",
        _ => "Can't confirm qualifiers match",
    };

    private static string GetQualifierGapSummary(QualifierAxis axis) => $"{GetQualifierAxisName(axis)} not proven";

    private static string FormatProofUse(Compilation compilation, ObligationContext context) => context switch
    {
        FieldExpressionContext fieldContext => $"`{EscapeInline(fieldContext.Field.Name)}`",
        EventHandlerContext eventHandlerContext => $"`{EscapeInline(eventHandlerContext.Handler.EventName)}`",
        StateHookContext stateHookContext => $"`{EscapeInline(stateHookContext.Hook.StateName)}`",
        TransitionRowContext transitionContext => $"`{EscapeInline(FormatTransitionHeader(transitionContext.Row))}`",
        _ => EscapePlain(DescribeProofContext(compilation, context)),
    };

    private static string CreateQualifierProvenEvidenceLine(
        Compilation compilation,
        TypedExpression left,
        TypedExpression right,
        DeclaredQualifierMeta? resultQualifier,
        QualifierAxis axis)
    {
        var leftQualifier = ResolveQualifierFromExpression(left, axis, compilation.Semantics);
        var rightQualifier = ResolveQualifierFromExpression(right, axis, compilation.Semantics);
        return $"Left/Right: {FormatProofQualifierValue(leftQualifier ?? rightQualifier, GetQualifierAxisName(axis))} · Result: {FormatProofQualifierValue(resultQualifier, GetQualifierAxisName(axis))}";
    }

    private static string DescribeQualifierGapEvidenceLine(
        Compilation compilation,
        TypedExpression left,
        TypedExpression right,
        QualifierAxis axis)
    {
        var leftQualifier = ResolveQualifierFromExpression(left, axis, compilation.Semantics);
        var rightQualifier = ResolveQualifierFromExpression(right, axis, compilation.Semantics);
        return $"Left `{EscapeInline(DescribeProofSite(compilation, left))}` {DescribeQualifierGapSegment(leftQualifier, rightQualifier, axis)} · right `{EscapeInline(DescribeProofSite(compilation, right))}` {DescribeQualifierGapSegment(rightQualifier, leftQualifier, axis)}";
    }

    private static string DescribeQualifierGapSegment(DeclaredQualifierMeta? qualifier, DeclaredQualifierMeta? counterpart, QualifierAxis axis)
    {
        if (qualifier is not null)
        {
            return $"carries {FormatResolvedQualifierValue(qualifier)}";
        }

        return counterpart is null
            ? $"has no known {GetQualifierAxisName(axis)}"
            : $"has no known {FormatResolvedQualifierValue(counterpart)}";
    }

    private static void AppendQualifierProofEvidenceLines(
        List<string> lines,
        Compilation compilation,
        TypedExpression left,
        TypedExpression right,
        QualifierAxis defaultAxis,
        ProofObligation obligation,
        bool includeResultLines,
        QualifierAxis? leftAxis = null,
        QualifierAxis? rightAxis = null)
    {
        var resolvedLeftAxis = leftAxis ?? defaultAxis;
        var resolvedRightAxis = rightAxis ?? defaultAxis;
        var leftQualifier = ResolveQualifierFromExpression(left, resolvedLeftAxis, compilation.Semantics);
        var rightQualifier = ResolveQualifierFromExpression(right, resolvedRightAxis, compilation.Semantics);

        lines.Add($"Left operand: `{EscapeInline(DescribeProofSite(compilation, left))}`");
        lines.Add($"Left qualifier{(resolvedLeftAxis == defaultAxis ? string.Empty : $" ({GetQualifierDisplayName(resolvedLeftAxis)})")}: {FormatProofQualifierValue(leftQualifier, "not proven at this site")}");
        lines.Add($"Left qualifier source: {DescribeProofQualifierSource(leftQualifier)}");
        lines.Add($"Right operand: `{EscapeInline(DescribeProofSite(compilation, right))}`");
        lines.Add($"Right qualifier{(resolvedRightAxis == defaultAxis ? string.Empty : $" ({GetQualifierDisplayName(resolvedRightAxis)})")}: {FormatProofQualifierValue(rightQualifier, "not proven at this site")}");
        lines.Add($"Right qualifier source: {DescribeProofQualifierSource(rightQualifier)}");

        var proofChainFields = new[]
        {
            leftQualifier?.SourceFieldName,
            rightQualifier?.SourceFieldName,
        }
            .Where(fieldName => !string.IsNullOrWhiteSpace(fieldName))
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();
        if (!proofChainFields.IsEmpty)
        {
            lines.Add($"Proof chain fields: {FormatCodeList(proofChainFields!)}");
        }

        if (includeResultLines)
        {
            var resultQualifier = ResolveQualifierFromExpression(obligation.Site, defaultAxis, compilation.Semantics);
            lines.Add($"Result type: `{EscapeInline(FormatType(obligation.Site.ResultType))}`");
            lines.Add($"Result qualifier: {FormatProofQualifierValue(resultQualifier, "unresolved")}");
        }
    }

    private static string DescribeProofStatus(ProofObligation obligation) => obligation.Disposition switch
    {
        ProofDisposition.Proved => "Proven",
        _ => "Unresolved proof obligation",
    };

    private static string DescribeQualifierChainRequirement(QualifierChainProofRequirement requirement) =>
        $"left {GetQualifierDisplayName(requirement.LeftAxis)} qualifier and right {GetQualifierDisplayName(requirement.RightAxis)} qualifier must resolve compatibly";

    private static string DescribeProofContext(Compilation compilation, ObligationContext context) => context switch
    {
        FieldExpressionContext fieldContext => $"{(fieldContext.Field.IsComputed ? "computed field" : "field")} `{EscapeInline(fieldContext.Field.Name)}`",
        TransitionRowContext transitionContext => $"transition `{EscapeInline(FormatTransitionHeader(transitionContext.Row))}`",
        ConstraintContext constraintContext => DescribeConstraint(compilation, constraintContext.Constraint),
        EventHandlerContext eventHandlerContext => $"event handler `{EscapeInline(eventHandlerContext.Handler.EventName)}`",
        StateHookContext stateHookContext => $"{stateHookContext.Hook.Scope.ToString().ToLowerInvariant()} hook `{EscapeInline(stateHookContext.Hook.StateName)}`",
        _ => "proof context",
    };

    private static string DescribeProofSite(Compilation compilation, TypedExpression expression) => NormalizeCompactSnippet(FormatSnippet(compilation, expression.Span));

    private static string NormalizeCompactSnippet(string value) => value
        .Replace("( ", "(", StringComparison.Ordinal)
        .Replace(" )", ")", StringComparison.Ordinal)
        .Replace("[ ", "[", StringComparison.Ordinal)
        .Replace(" ]", "]", StringComparison.Ordinal)
        .Replace("{ ", "{", StringComparison.Ordinal)
        .Replace(" }", "}", StringComparison.Ordinal)
        .Replace(" . ", ".", StringComparison.Ordinal);

    private static string ExplainUnresolvedQualifierReason(Compilation compilation, TypedExpression? left, TypedExpression? right, QualifierAxis axis)
    {
        if (left is not null && right is not null)
        {
            var leftQualifier = ResolveQualifierFromExpression(left, axis, compilation.Semantics);
            var rightQualifier = ResolveQualifierFromExpression(right, axis, compilation.Semantics);
            if (leftQualifier is not null && rightQualifier is not null && !AreEquivalentQualifiers(leftQualifier, rightQualifier))
            {
                return $"operands resolve to different {GetQualifierAxisName(axis)} qualifiers";
            }

            if (leftQualifier is null && TryDescribeMissingQualifier(left, axis, compilation, out var leftReason))
            {
                return leftReason;
            }

            if (rightQualifier is null && TryDescribeMissingQualifier(right, axis, compilation, out var rightReason))
            {
                return rightReason;
            }
        }

        return "qualifier preservation is not proven here";
    }

    private static bool TryDescribeMissingQualifier(TypedExpression expression, QualifierAxis axis, Compilation compilation, out string reason)
    {
        if (expression is TypedBinaryOp)
        {
            reason = $"qualifier preservation for `{EscapeInline(DescribeProofSite(compilation, expression))}` is not proven here";
            return true;
        }

        if (TryGetFieldFromExpression(expression, compilation.Semantics, out var field)
            && TryGetQualifierUnresolvedReason(field.ResolvedType, [axis], out var fieldReason))
        {
            var trimmed = fieldReason.Replace($"{FormatType(field.ResolvedType)} declaration ", string.Empty, StringComparison.Ordinal);
            reason = $"field `{EscapeInline(field.Name)}` {trimmed}";
            return true;
        }

        if (expression is TypedArgRef argRef)
        {
            reason = $"argument `{EscapeInline(argRef.ArgName)}` has no {GetQualifierAnnotationLabel(axis)} annotation";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryGetPresenceDescription(TypedExpression? subjectExpression, SemanticIndex semantics, out string presence)
    {
        if (subjectExpression is TypedFieldRef fieldRef && semantics.FieldsByName.TryGetValue(fieldRef.FieldName, out var field))
        {
            presence = field.Presence is DeclaredPresenceMeta.Optional ? "optional" : "not nullable";
            return true;
        }

        if (subjectExpression is TypedArgRef argRef)
        {
            var arg = semantics.Events.SelectMany(evt => evt.Args).FirstOrDefault(candidate => string.Equals(candidate.Name, argRef.ArgName, StringComparison.Ordinal));
            if (arg is not null)
            {
                presence = arg.Presence is DeclaredPresenceMeta.Optional ? "optional" : "not nullable";
                return true;
            }
        }

        presence = string.Empty;
        return false;
    }

    private static string FormatDiagnosticCode(PreceptDiagnosticCode code) => $"PRE{(int)code:0000}";

    private static int GetSpanWidth(SourceSpan span) => span.End - span.Offset;

    private static string FormatProofQualifierValue(DeclaredQualifierMeta? qualifier, string unresolvedText) => qualifier is null
        ? unresolvedText
        : FormatResolvedQualifierValue(qualifier);

    private static string DescribeProofQualifierSource(DeclaredQualifierMeta? qualifier)
    {
        if (TryDescribeQualifierSource(qualifier, TypeKind.Error, out var source))
        {
            return source.DisplayText;
        }

        return "unresolved";
    }

    private static string HumanizeProofStrategy(ProofStrategy strategy) => strategy switch
    {
        ProofStrategy.QualifierCompatibility => "same-qualifier propagation",
        ProofStrategy.Literal => "literal proof",
        ProofStrategy.DeclarationAttribute => "declaration attribute",
        ProofStrategy.GuardInPath => "guard in path",
        ProofStrategy.FlowNarrowing => "flow narrowing",
        ProofStrategy.CompositionalConstraint => "compositional constraint",
        _ => strategy.ToString(),
    };

    private static bool AreEquivalentQualifiers(DeclaredQualifierMeta left, DeclaredQualifierMeta right)
    {
        if (!string.IsNullOrWhiteSpace(left.SourceFieldName) && !string.IsNullOrWhiteSpace(right.SourceFieldName))
        {
            return string.Equals(left.SourceFieldName, right.SourceFieldName, StringComparison.Ordinal);
        }

        return string.Equals(GetQualifierRawValue(left), GetQualifierRawValue(right), StringComparison.Ordinal);
    }

    private static TypedExpression? ResolveProofSubjectExpression(ProofSubject subject, TypedExpression site) => subject switch
    {
        ParamSubject paramSubject => ResolveParameterExpression(paramSubject.Parameter, site),
        SelfSubject => site is TypedMemberAccess memberAccess ? memberAccess.Object : site,
        _ => null,
    };

    private static TypedExpression? ResolveParameterExpression(ParameterMeta parameter, TypedExpression site)
    {
        if (site is TypedBinaryOp binary)
        {
            var left = ResolveParameterFromExpression(binary.Left, parameter);
            return left ?? ResolveParameterFromExpression(binary.Right, parameter);
        }

        if (site is TypedMemberAccess memberAccess)
        {
            return ResolveParameterFromExpression(memberAccess.Object, parameter);
        }

        return null;
    }

    private static TypedExpression? ResolveParameterFromExpression(TypedExpression expression, ParameterMeta parameter)
    {
        if (parameter.Kind == expression.ResultType)
        {
            return expression;
        }

        return expression switch
        {
            TypedBinaryOp binary => ResolveParameterFromExpression(binary.Left, parameter) ?? ResolveParameterFromExpression(binary.Right, parameter),
            TypedUnaryOp unary => ResolveParameterFromExpression(unary.Operand, parameter),
            TypedFunctionCall functionCall => functionCall.Arguments.Select(argument => ResolveParameterFromExpression(argument, parameter)).FirstOrDefault(candidate => candidate is not null),
            TypedMemberAccess memberAccess => ResolveParameterFromExpression(memberAccess.Object, parameter),
            _ => null,
        };
    }

    private static bool TryGetFieldFromExpression(TypedExpression expression, SemanticIndex semantics, out TypedField field)
    {
        if (expression is TypedFieldRef fieldRef && semantics.FieldsByName.TryGetValue(fieldRef.FieldName, out field!))
        {
            return true;
        }

        if (expression is TypedMemberAccess { Object: TypedFieldRef owner } && semantics.FieldsByName.TryGetValue(owner.FieldName, out field!))
        {
            return true;
        }

        field = null!;
        return false;
    }

    private static DeclaredQualifierMeta? ResolveQualifierFromExpression(TypedExpression expression, QualifierAxis axis, SemanticIndex semantics)
    {
        switch (expression)
        {
            case TypedFieldRef fieldRef:
                return ResolveFieldQualifier(fieldRef.FieldName, axis, semantics);

            case TypedArgRef { DeclaredQualifiers: { } argQualifiers }:
                return ResolveDeclarationQualifier(expression.ResultType, argQualifiers, axis);

            case TypedTypedConstant { DeclaredQualifiers: { } constantQualifiers }:
                return ResolveDeclarationQualifier(expression.ResultType, constantQualifiers, axis);

            case TypedInterpolatedTypedConstant interpolatedConstant:
                return ResolveQualifierFromInterpolatedConstant(interpolatedConstant, axis);

            case TypedMemberAccess { ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: var returnsAxis }, Object: var owner }
                when returnsAxis != QualifierAxis.None:
                return ResolveQualifierFromExpression(owner, returnsAxis, semantics);

            case TypedMemberAccess memberAccess:
                return ResolveQualifierFromExpression(memberAccess.Object, axis, semantics);

            case TypedBinaryOp binary when binary.ResultQualifier is not null:
                return binary.ResultQualifier switch
                {
                    SameQualifierRequired => ResolveQualifierFromExpression(binary.Left, axis, semantics)
                        ?? ResolveQualifierFromExpression(binary.Right, axis, semantics),
                    QualifiedOperandInherited => ResolveQualifierFromExpression(
                        binary.Left.ResultType == binary.ResultType ? binary.Left : binary.Right,
                        axis,
                        semantics),
                    CurrencyConversionRequired when axis == QualifierAxis.Currency => ResolveQualifierFromExpression(
                        binary.Left.ResultType == TypeKind.ExchangeRate ? binary.Left : binary.Right,
                        QualifierAxis.ToCurrency,
                        semantics),
                    _ => ResolveQualifierFromExpression(binary.Left, axis, semantics)
                        ?? ResolveQualifierFromExpression(binary.Right, axis, semantics),
                };

            default:
                return null;
        }
    }

    private static DeclaredQualifierMeta? ResolveFieldQualifier(string fieldName, QualifierAxis axis, SemanticIndex semantics) =>
        semantics.FieldsByName.TryGetValue(fieldName, out var field)
            ? ResolveDeclarationQualifier(field.ResolvedType, field.DeclaredQualifiers, axis)
            : null;

    private static DeclaredQualifierMeta? ResolveQualifierFromInterpolatedConstant(TypedInterpolatedTypedConstant constant, QualifierAxis axis)
    {
        var targetSlot = axis switch
        {
            QualifierAxis.Currency => InterpolationSlotKind.Currency,
            QualifierAxis.Unit or QualifierAxis.Dimension => InterpolationSlotKind.Unit,
            QualifierAxis.FromCurrency => InterpolationSlotKind.FromCurrency,
            QualifierAxis.ToCurrency => InterpolationSlotKind.ToCurrency,
            _ => (InterpolationSlotKind?)null,
        };
        if (targetSlot is null)
        {
            return null;
        }

        var slot = constant.Slots.FirstOrDefault(candidate => candidate.SlotKind == targetSlot.Value);
        return slot is null ? null : CreateQualifierFromSlotExpression(slot.Expression, axis);
    }

    private static DeclaredQualifierMeta? CreateQualifierFromSlotExpression(TypedExpression expression, QualifierAxis axis)
    {
        var fieldName = expression switch
        {
            TypedFieldRef fieldRef => fieldRef.FieldName,
            TypedArgRef argRef => argRef.ArgName,
            _ => null,
        };
        if (fieldName is null)
        {
            return null;
        }

        return axis switch
        {
            QualifierAxis.Currency => new DeclaredQualifierMeta.Currency($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit($"{{{fieldName}}}", $"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.FromCurrency => new DeclaredQualifierMeta.FromCurrency($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.ToCurrency => new DeclaredQualifierMeta.ToCurrency($"{{{fieldName}}}", SourceFieldName: fieldName),
            _ => null,
        };
    }

    private static ImmutableArray<ProofObligation> GetStoredFieldProofUses(Compilation compilation, TypedField field) =>
        compilation.Proof.Obligations
            .Where(obligation => obligation.Requirement is QualifierCompatibilityProofRequirement or QualifierChainProofRequirement)
            .Where(obligation => ContainsFieldReference(obligation.Site, field.Name))
            .ToImmutableArray();

    private static ImmutableArray<ProofObligation> GetComputedFieldProofObligations(Compilation compilation, TypedField field) =>
        compilation.Proof.Obligations
            .Where(obligation => obligation.Requirement is QualifierCompatibilityProofRequirement or QualifierChainProofRequirement)
            .Where(obligation => obligation.Context is FieldExpressionContext context
                && string.Equals(context.Field.Name, field.Name, StringComparison.Ordinal))
            .ToImmutableArray();

    private static bool TryGetFieldProofGap(Compilation compilation, ImmutableArray<ProofObligation> obligations, out FieldProofGap gap)
    {
        var obligation = obligations
            .Where(candidate => candidate.Disposition == ProofDisposition.Unresolved)
            .OrderBy(candidate => GetSpanWidth(candidate.Site.Span))
            .FirstOrDefault();
        if (obligation is null)
        {
            gap = null!;
            return false;
        }

        gap = new FieldProofGap(
            FormatProofUse(compilation, obligation.Context),
            CreateFieldProofEvidence(compilation, obligation));
        return true;
    }

    private static string CreateFieldProofEvidence(Compilation compilation, ProofObligation obligation) => obligation switch
    {
        { Requirement: QualifierCompatibilityProofRequirement requirement, Site: TypedBinaryOp expression } =>
            DescribeQualifierGapEvidenceLine(compilation, expression.Left, expression.Right, requirement.Axis),
        { Requirement: QualifierChainProofRequirement requirement, Site: TypedBinaryOp expression } =>
            DescribeQualifierGapEvidenceLine(compilation, expression.Left, expression.Right, requirement.LeftAxis),
        _ => EscapePlain(obligation.Requirement.Description),
    };

    private static bool ContainsFieldReference(TypedExpression expression, string fieldName) => expression switch
    {
        TypedFieldRef fieldRef => string.Equals(fieldRef.FieldName, fieldName, StringComparison.Ordinal),
        TypedMemberAccess memberAccess => ContainsFieldReference(memberAccess.Object, fieldName),
        TypedBinaryOp binary => ContainsFieldReference(binary.Left, fieldName) || ContainsFieldReference(binary.Right, fieldName),
        TypedUnaryOp unary => ContainsFieldReference(unary.Operand, fieldName),
        TypedFunctionCall functionCall => functionCall.Arguments.Any(argument => ContainsFieldReference(argument, fieldName)),
        TypedConditional conditional => ContainsFieldReference(conditional.Condition, fieldName)
            || ContainsFieldReference(conditional.ThenBranch, fieldName)
            || ContainsFieldReference(conditional.ElseBranch, fieldName),
        TypedInterpolatedString interpolatedString => interpolatedString.Segments.OfType<TypedHoleSegment>().Any(hole => ContainsFieldReference(hole.Expression, fieldName)),
        TypedInterpolatedTypedConstant interpolatedConstant => interpolatedConstant.Slots.Any(slot => ContainsFieldReference(slot.Expression, fieldName)),
        TypedListLiteral listLiteral => listLiteral.Elements.Any(element => ContainsFieldReference(element, fieldName)),
        TypedPostfixOp postfix => ContainsFieldReference(postfix.Operand, fieldName),
        _ => false,
    };

    private static string FormatFieldIdentityLine(TypedField field)
    {
        if (TryFormatCompactQualifierSummary(field.ResolvedType, field.DeclaredQualifiers, out var qualifierSummary))
        {
            return $"`{EscapeInline(field.Name)}` · ⚖️ {qualifierSummary}";
        }

        return $"`{EscapeInline(field.Name)}` · `{EscapeInline(FormatType(field.ResolvedType, field.ElementType, field.KeyType))}`";
    }

    private static bool TryFormatCompactQualifierSummary(TypeKind ownerType, ImmutableArray<DeclaredQualifierMeta> qualifiers, out string summary)
    {
        var resolved = GetRelevantQualifierAxes(ownerType, qualifiers)
            .Select(axis => ResolveDeclarationQualifier(ownerType, qualifiers, axis))
            .Where(qualifier => qualifier is not null)
            .Cast<DeclaredQualifierMeta>()
            .ToImmutableArray();
        if (resolved.IsEmpty)
        {
            summary = string.Empty;
            return false;
        }

        summary = string.Join(" / ", resolved.Select(FormatCompactQualifierValue));
        return true;
    }

    private static string FormatCompactQualifierValue(DeclaredQualifierMeta qualifier)
    {
        var rawValue = GetQualifierRawValue(qualifier);
        return string.IsNullOrWhiteSpace(rawValue)
            ? "`<unresolved>`"
            : $"`'{EscapeInline(rawValue)}'`";
    }

    private static string FormatConstraintGovernanceSummary(Compilation compilation, string fieldName)
    {
        var influences = compilation.Proof.ConstraintInfluence
            .Where(entry => entry.ReferencedFields.Contains(fieldName, StringComparer.Ordinal))
            .Select(entry => entry.Constraint)
            .Distinct()
            .ToImmutableArray();
        var ruleCount = influences.Count(identity => identity is RuleIdentity);
        var ensureCount = influences.Count(identity => identity is EnsureIdentity);
        var parts = new List<string>();
        if (ruleCount > 0)
        {
            parts.Add(FormatGovernanceCount(ruleCount, "rule"));
        }

        if (ensureCount > 0)
        {
            parts.Add(FormatGovernanceCount(ensureCount, "ensure"));
        }

        return parts.Count == 0 ? "none" : string.Join(" · ", parts);
    }

    private static string FormatGovernanceCount(int count, string noun) => $"{count} {noun}{Pluralize(count)}";

    private static string FormatComputedFieldSourceSummary(Compilation compilation, TypedField field)
    {
        if (field.ComputedExpression is null)
        {
            return "*none*";
        }

        var inputs = new HashSet<string>(StringComparer.Ordinal);
        CollectFieldInputs(field.ComputedExpression, inputs);
        if (inputs.Count == 0)
        {
            return $"`{EscapeInline(FormatSnippet(compilation, field.ComputedExpression.Span))}`";
        }

        return string.Join(" · ", inputs.Select(name => $"`{EscapeInline(name)}`"));
    }

    private static void CollectFieldInputs(TypedExpression expression, HashSet<string> inputs)
    {
        switch (expression)
        {
            case TypedFieldRef fieldRef:
                inputs.Add(fieldRef.FieldName);
                break;
            case TypedMemberAccess memberAccess:
                CollectFieldInputs(memberAccess.Object, inputs);
                break;
            case TypedBinaryOp binary:
                CollectFieldInputs(binary.Left, inputs);
                CollectFieldInputs(binary.Right, inputs);
                break;
            case TypedUnaryOp unary:
                CollectFieldInputs(unary.Operand, inputs);
                break;
            case TypedFunctionCall functionCall:
                foreach (var argument in functionCall.Arguments)
                {
                    CollectFieldInputs(argument, inputs);
                }
                break;
            case TypedConditional conditional:
                CollectFieldInputs(conditional.Condition, inputs);
                CollectFieldInputs(conditional.ThenBranch, inputs);
                CollectFieldInputs(conditional.ElseBranch, inputs);
                break;
            case TypedInterpolatedString interpolatedString:
                foreach (var hole in interpolatedString.Segments.OfType<TypedHoleSegment>())
                {
                    CollectFieldInputs(hole.Expression, inputs);
                }
                break;
            case TypedInterpolatedTypedConstant interpolatedConstant:
                foreach (var slot in interpolatedConstant.Slots)
                {
                    CollectFieldInputs(slot.Expression, inputs);
                }
                break;
            case TypedListLiteral listLiteral:
                foreach (var element in listLiteral.Elements)
                {
                    CollectFieldInputs(element, inputs);
                }
                break;
            case TypedPostfixOp postfix:
                CollectFieldInputs(postfix.Operand, inputs);
                break;
        }
    }

    private static string CreateFieldMarkdown(Compilation compilation, TypedField field)
    {
        var identity = FormatFieldIdentityLine(field);
        var governance = FormatConstraintGovernanceSummary(compilation, field.Name);

        if (!field.IsComputed && TryGetFieldProofGap(compilation, GetStoredFieldProofUses(compilation, field), out var storedGap))
        {
            return string.Join("\n", new[]
            {
                $"⚠️ Gap · {identity}",
                $"🔬 Use: {storedGap.Use}",
                $"Evidence: {storedGap.Evidence}",
            });
        }

        if (field.IsComputed)
        {
            var obligations = GetComputedFieldProofObligations(compilation, field);
            if (TryGetFieldProofGap(compilation, obligations, out var computedGap))
            {
                return string.Join("\n", new[]
                {
                    "⚠️ Gap · recomputed before commit",
                    identity,
                    $"🔬 Use: {computedGap.Use}",
                    $"Evidence: {computedGap.Evidence}",
                });
            }

            var header = obligations.Length > 0
                ? "✅ Proven · recomputed before commit"
                : "⚡ Enforced · recomputed before commit";
            return string.Join("\n", new[]
            {
                header,
                identity,
                $"From: {FormatComputedFieldSourceSummary(compilation, field)} · Governed by: {governance}",
            });
        }

        var lines = new List<string>
        {
            $"⚡ Enforced · {identity}",
        };

        if (!compilation.Semantics.States.IsEmpty)
        {
            var writeMap = GetFieldWriteMapByState(compilation, field);
            if (TryFormatFieldMutabilitySummary(writeMap, out var mutabilitySummary))
            {
                lines.Add(mutabilitySummary);
            }
        }

        lines.Add($"Governed by: {governance}");
        return string.Join("\n", lines);
    }

    private static bool TryFormatFieldMutabilitySummary(FieldWriteMap writeMap, out string summary)
    {
        var parts = new List<string>();
        if (!writeMap.WritableStates.IsDefaultOrEmpty)
        {
            parts.Add($"✏️ {FormatCodeList(writeMap.WritableStates)} (unconditional)");
        }

        if (!writeMap.LockedStates.IsDefaultOrEmpty)
        {
            parts.Add($"🔒 {FormatCodeList(writeMap.LockedStates)}");
        }

        summary = string.Join(" · ", parts);
        return parts.Count > 0;
    }

    private static string CreateStateMarkdown(Compilation compilation, TypedState state)
    {
        var graphState = compilation.Graph.States.FirstOrDefault(candidate => string.Equals(candidate.Name, state.Name, StringComparison.Ordinal));
        var reachabilityDetail = DescribeStateReachability(compilation, state, graphState);
        var incoming = compilation.Graph.Edges
            .Where(edge => string.Equals(edge.ToState, state.Name, StringComparison.Ordinal))
            .Select(edge => edge.EventName)
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();
        var outgoing = compilation.Graph.Edges
            .Where(edge => string.Equals(edge.FromState, state.Name, StringComparison.Ordinal))
            .Select(FormatOutgoingEdge)
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();
        var writable = GetWritableFieldsForState(compilation, state.Name);
        var activeEnsures = GetEnsuresForState(compilation, state.Name);
        var unverifiedEnsures = activeEnsures.Count(entry => HasUnresolvedConstraint(compilation, entry.Identity));
        var edgeProofStatuses = GetEdgeProofStatusesForState(compilation, state.Name);
        var unresolvedEdgeCount = edgeProofStatuses.Count(status => !status.IsProven);
        var terminalReachable = IsTerminalReachable(compilation.Graph, state.Name);
        var statusDetail = graphState is null || !graphState.IsReachable
            ? reachabilityDetail
            : unresolvedEdgeCount > 0
                ? $"{unresolvedEdgeCount} connected edge{Pluralize(unresolvedEdgeCount)} can't be proven"
                : unverifiedEnsures > 0
                    ? $"{unverifiedEnsures} ensure{Pluralize(unverifiedEnsures)} unverified"
                    : reachabilityDetail;
        var statusKind = HasConstructDiagnostics(compilation, state.NameSpan)
            || graphState is null
            || !graphState.IsReachable
            || unresolvedEdgeCount > 0
            || unverifiedEnsures > 0
            ? HoverStatusKind.Unverified
            : HoverStatusKind.ProofVerified;
        var status = new HoverStatusBadge(statusKind, statusDetail);
        var incomingSummary = incoming.IsEmpty ? "none" : FormatCodeList(incoming);
        var outgoingSummary = outgoing.IsEmpty ? "none" : FormatCodeList(outgoing);

        var lines = new List<string>
        {
            FormatStatus(status),
            $"🔁 In: {incomingSummary} · Out: {outgoingSummary}",
            $"✏️ {writable.Length} field{Pluralize(writable.Length)} (unconditional) · 🧭 terminal {(terminalReachable ? "✓" : "✗")} · ⚡ {activeEnsures.Length} ensure{Pluralize(activeEnsures.Length)} ({unverifiedEnsures} ⚠️)",
            CreateStateGraphEdgeProofCard(state, edgeProofStatuses),
        };

        return string.Join("\n\n", lines);
    }

    private static string CreateEventMarkdown(Compilation compilation, TypedEvent evt)
    {
        var hasEventEnsureGap = GetEnsuresForEvent(compilation, evt.Name)
            .Any(entry => HasUnresolvedConstraint(compilation, entry.Identity));
        var header = evt.IsInitial
            ? hasEventEnsureGap
                ? "⚠️ Gap · constructor event"
                : "⚡ Enforced · constructor event"
            : hasEventEnsureGap
                ? "⚠️ Gap · args checked before route"
                : "⚡ Enforced · args checked before route";
        var lines = new List<string>
        {
            header,
        };

        if (!evt.IsInitial)
        {
            var handledInStates = compilation.Graph.Events
                .FirstOrDefault(candidate => string.Equals(candidate.Name, evt.Name, StringComparison.Ordinal))?
                .HandledInStates
                ?? ImmutableArray<string>.Empty;
            lines.Add($"🔁 Fires from: {FormatCodeList(handledInStates)}");
        }

        lines.Add($"Args: {(evt.Args.IsDefaultOrEmpty ? "*none*" : string.Join(" · ", evt.Args.Select(arg => $"`{EscapeInline(FormatArgumentSignaturePart(arg))}`")))}");
        return string.Join("\n", lines);
    }

    private static string CreateArgumentMarkdown(TypedArg arg) => string.Join("\n\n", new[]
    {
        $"**argument `{EscapeInline(arg.Name)}`**",
        $"Event: `{EscapeInline(arg.EventName)}`",
        $"Type: {FormatTypeSummary(arg.ResolvedType, arg.ElementType, null, arg.DeclaredQualifiers, arg.Presence)}",
    });

    private static string CreateRuleMarkdown(Compilation compilation, TypedRule rule, RuleIdentity identity)
    {
        var influence = GetConstraintInfluence(compilation, identity);
        if (TryCreateConstraintProofCard(
            compilation,
            rule.Syntax.Span,
            identity,
            FormatSnippet(compilation, rule.Condition.Span),
            influence,
            out var proofCard))
        {
            return proofCard;
        }

        var lines = new List<string>
        {
            rule.Guard is null
                ? "⚡ Enforced · after every mutation"
                : $"⚡ Enforced · when `{EscapeInline(FormatSnippet(compilation, rule.Guard.Span))}`",
        };

        if (TryGetMessageText(rule.Message, out var message))
        {
            lines.Add($"> {EscapeBlockquote(message)}");
        }

        if (TryFormatConstraintReferenceLine(influence, false, out var referenceLine))
        {
            lines.Add(referenceLine);
        }

        return string.Join("\n", lines);
    }

    private static string CreateEnsureMarkdown(Compilation compilation, TypedEnsure ensure, EnsureIdentity identity)
    {
        var influence = GetConstraintInfluence(compilation, identity);
        if (TryCreateConstraintProofCard(
            compilation,
            ensure.Syntax.Span,
            identity,
            GetEnsureProofLabel(ensure),
            influence,
            out var proofCard))
        {
            return proofCard;
        }

        var lines = new List<string>
        {
            $"⚡ Enforced · {GetEnsureCardDetail(ensure)}",
        };

        if (TryGetMessageText(ensure.Message, out var message))
        {
            lines.Add($"> {EscapeBlockquote(message)}");
        }

        if (TryFormatConstraintReferenceLine(influence, false, out var referenceLine))
        {
            lines.Add(referenceLine);
        }

        return string.Join("\n", lines);
    }

    private static bool TryCreateConstraintProofCard(
        Compilation compilation,
        SourceSpan span,
        ConstraintIdentity identity,
        string label,
        ConstraintInfluenceSummary influence,
        out string markdown)
    {
        var obligations = GetConstraintObligations(compilation, identity);
        var diagnostics = GetDiagnosticsOverlapping(compilation, span, DiagnosticStage.Proof);
        if (obligations.IsEmpty && diagnostics.IsEmpty)
        {
            markdown = string.Empty;
            return false;
        }

        var diagnosticCode = obligations
            .Select(obligation => obligation.EmittedDiagnostic)
            .FirstOrDefault(code => code is not null)
            ?? diagnostics
                .Select(diagnostic => Enum.TryParse<PreceptDiagnosticCode>(diagnostic.Code, out var code) ? code : (PreceptDiagnosticCode?)null)
                .FirstOrDefault(code => code is not null);
        var header = diagnosticCode is { } resolvedCode
            ? $"⚠️ `{FormatDiagnosticCode(resolvedCode)}` · Gap · {EscapeInline(label)}"
            : $"⚠️ Gap · {EscapeInline(label)}";
        var primaryObligation = SelectPrimaryProofObligation(obligations);
        var evidence = primaryObligation is null
            ? EscapePlain(diagnostics[0].Message)
            : CreateFieldProofEvidence(compilation, primaryObligation);
        var lines = new List<string>
        {
            header,
        };

        if (TryFormatConstraintReferenceLine(influence, true, out var referenceLine))
        {
            lines.Add(referenceLine);
        }

        lines.Add($"🔬 {evidence}");
        markdown = string.Join("\n", lines);
        return true;
    }

    private static bool TryFormatConstraintReferenceLine(ConstraintInfluenceSummary influence, bool proofVariant, out string line)
    {
        var parts = new List<string>();
        if (!influence.ReferencedFields.IsDefaultOrEmpty)
        {
            parts.Add($"Fields: {FormatCodeList(influence.ReferencedFields)}");
        }

        if (!influence.ReferencedArgs.IsDefaultOrEmpty)
        {
            parts.Add($"Args: {FormatCodeList(influence.ReferencedArgs.Select(arg => arg.ArgName))}");
        }

        if (parts.Count == 0)
        {
            line = string.Empty;
            return false;
        }

        line = string.Join(" · ", parts);
        if (proofVariant)
        {
            line = $"⚖️ {line}";
        }

        return true;
    }

    private static string CreateTransitionMarkdown(Compilation compilation, TypedTransitionRow row)
    {
        var unresolved = GetTransitionObligations(compilation, row);
        var proofDiagnostics = GetDiagnosticsOverlapping(compilation, row.RowSpan, DiagnosticStage.Proof);
        var reachability = DescribeTransitionReachability(compilation.Graph, row);
        var proofGapCount = Math.Max(unresolved.Length, proofDiagnostics.Length);
        var status = proofGapCount > 0 || HasConstructDiagnostics(compilation, row.RowSpan)
            ? new HoverStatusBadge(HoverStatusKind.Unverified, $"{proofGapCount} unresolved proof obligation{Pluralize(proofGapCount)}")
            : new HoverStatusBadge(HoverStatusKind.ProofVerified, reachability);

        var lines = new List<string>
        {
            $"**transition** `{EscapeInline(FormatTransitionHeader(row))}`",
            FormatStatus(status),
            $"Guard: {(row.Guard is null ? "*none*" : $"`{EscapeInline(FormatSnippet(compilation, row.Guard.Span))}`")}",
            $"Actions: {FormatCodeList(row.Actions.Select(FormatActionSummary))}",
            $"Graph: {reachability}",
        };

        if (proofGapCount > 0)
        {
            var categories = GetProofGapCategories(proofDiagnostics);
            lines.Add($"Gap: {proofGapCount} unresolved obligation{Pluralize(proofGapCount)} ({string.Join(", ", categories)})");
        }

        return string.Join("\n\n", lines);
    }

    private static string CreateRejectMarkdown(Compilation compilation, TypedTransitionRow row)
    {
        var status = BuildStatus(compilation, row.RowSpan, HoverStatusKind.RuntimeChecked, "deliberate business rejection");
        var lines = new List<string>
        {
            $"**reject** `{EscapeInline(FormatRejectHeader(row))}`",
        };

        if (!string.IsNullOrWhiteSpace(row.RejectReason))
        {
            lines.Add($"> {EscapeBlockquote(row.RejectReason!)}");
        }

        lines.Add(FormatStatus(status));
        lines.Add("Result: state unchanged · no field mutations commit");
        return string.Join("\n\n", lines);
    }

    private static string CreateAccessMarkdown(Compilation compilation, AccessDeclarationInfo access)
    {
        var status = BuildStatus(compilation, access.Span, HoverStatusKind.ProofVerified, "write map is structural");
        var sameWriteSetStates = GetAccessDeclarations(compilation)
            .Where(candidate => !string.Equals(candidate.StateName, access.StateName, StringComparison.Ordinal)
                && candidate.Mode == access.Mode
                && candidate.FieldNames.ToHashSet(StringComparer.Ordinal).SetEquals(access.FieldNames))
            .Select(candidate => candidate.StateName)
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();
        var sameSet = sameWriteSetStates.ToImmutableHashSet(StringComparer.Ordinal);
        var lockedStates = compilation.Semantics.States
            .Select(state => state.Name)
            .Where(name => !string.Equals(name, access.StateName, StringComparison.Ordinal) && !sameSet.Contains(name))
            .ToImmutableArray();

        var lines = new List<string>
        {
            $"**access** `{EscapeInline(access.Label)}`",
            FormatStatus(status),
            $"Editable here: {FormatCodeList(access.FieldNames)}",
            $"Same write set in {FormatCodeList(sameWriteSetStates)} · locked in {FormatCodeList(lockedStates)}",
        };

        return string.Join("\n\n", lines);
    }

    private static string CreateOmitMarkdown(Compilation compilation, OmitDeclarationInfo omit)
    {
        var status = BuildStatus(compilation, omit.Span, HoverStatusKind.ProofVerified, $"field is structurally absent in `{EscapeInline(omit.StateName)}`");
        var restoredStates = compilation.Graph.Edges
            .Where(edge => string.Equals(edge.FromState, omit.StateName, StringComparison.Ordinal)
                && edge.ToState is not null
                && !IsFieldOmittedInState(compilation, omit.FieldName, edge.ToState))
            .Select(edge => edge.ToState!)
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();

        var lines = new List<string>
        {
            $"**omit** `{EscapeInline(omit.Label)}`",
            FormatStatus(status),
            $"`{EscapeInline(omit.FieldName)}` does not exist in this state — not readable, not writable",
            $"Restored on transition to: {FormatCodeList(restoredStates)}",
        };

        return string.Join("\n\n", lines);
    }

    private static string CreateQualifierMarkdown(Compilation compilation, QualifierHoverInfo info)
    {
        var effectiveAxis = info.ResolvedQualifier?.Axis ?? info.Axis;
        var lines = new List<string>
        {
            $"⚖️ {CapitalizeFirst(GetQualifierAxisName(effectiveAxis))} · {FormatResolvedQualifierValue(info.ResolvedQualifier)}",
        };

        if (TryDescribeQualifierSource(info.ResolvedQualifier, info.OwnerType, out var source))
        {
            lines.Add(source.IsFieldSource
                ? $"Resolves from {source.DisplayText}"
                : CapitalizeFirst(source.DisplayText));
        }
        else if (TryGetQualifierUnresolvedReason(info.OwnerType, [info.Axis], out var reason))
        {
            lines.Add(CapitalizeFirst(reason));
        }

        lines.Add(GetQualifierMismatchText(effectiveAxis));
        return string.Join("\n", lines);
    }

    private static void AppendFieldQualifierLines(List<string> lines, TypedField field)
    {
        var axes = GetRelevantQualifierAxes(field.ResolvedType, field.DeclaredQualifiers);
        if (axes.IsDefaultOrEmpty)
        {
            return;
        }

        if (!field.DeclaredQualifiers.IsDefaultOrEmpty)
        {
            var label = field.DeclaredQualifiers.Length == 1 ? "Declared qualifier" : "Declared qualifiers";
            lines.Add($"{label}: {string.Join(" · ", field.DeclaredQualifiers.Select(FormatDeclaredQualifier))}");
        }

        var resolved = axes
            .Select(axis => new QualifierResolutionEntry(axis, ResolveDeclarationQualifier(field.ResolvedType, field.DeclaredQualifiers, axis)))
            .ToImmutableArray();
        lines.Add($"{(resolved.Length == 1 ? "Resolved qualifier" : "Resolved qualifiers")}: {FormatResolvedQualifierEntries(resolved)}");

        var sources = resolved
            .Select(entry => CreateQualifierSourceEntry(field.ResolvedType, entry))
            .Where(entry => entry is not null)
            .Cast<string>()
            .ToImmutableArray();
        if (!sources.IsEmpty)
        {
            lines.Add($"{(sources.Length == 1 ? "Qualifier source" : "Qualifier sources")}: {string.Join(" · ", sources)}");
        }

        var missingAxes = resolved.Where(entry => entry.Qualifier is null).Select(entry => entry.Axis).ToImmutableArray();
        if (!missingAxes.IsEmpty && TryGetQualifierUnresolvedReason(field.ResolvedType, missingAxes, out var reason))
        {
            lines.Add($"Reason: {reason}");
        }
    }

    private static string GetQualifierStatusDetail(QualifierHoverInfo info)
    {
        if (TryDescribeQualifierSource(info.ResolvedQualifier, info.OwnerType, out var source))
        {
            return source.IsFieldSource
                ? $"qualifier resolves from {source.DisplayText}"
                : $"qualifier is {source.DisplayText}";
        }

        if (TryGetQualifierUnresolvedReason(info.OwnerType, [info.Axis], out var reason))
        {
            return reason;
        }

        return $"qualifier compatibility checked for `{EscapeInline(FormatType(info.OwnerType))}` at compile time";
    }

    private static ImmutableArray<QualifierAxis> GetRelevantQualifierAxes(TypeKind ownerType, ImmutableArray<DeclaredQualifierMeta> declaredQualifiers)
    {
        if (!declaredQualifiers.IsDefaultOrEmpty)
        {
            return declaredQualifiers
                .Select(qualifier => qualifier.Axis)
                .Distinct()
                .ToImmutableArray();
        }

        var slots = Types.GetMeta(ownerType).QualifierShape?.Slots;
        return slots is null
            ? ImmutableArray<QualifierAxis>.Empty
            : slots.Select(slot => slot.Axis).Distinct().ToImmutableArray();
    }

    private static DeclaredQualifierMeta? ResolveDeclarationQualifier(
        TypeKind ownerType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        QualifierAxis axis)
    {
        foreach (var qualifier in declaredQualifiers)
        {
            if (qualifier.Axis == axis)
            {
                return qualifier;
            }
        }

        if (axis == QualifierAxis.Unit)
        {
            foreach (var qualifier in declaredQualifiers)
            {
                if (qualifier.Axis == QualifierAxis.Dimension)
                {
                    return qualifier;
                }
            }
        }

        if (axis == QualifierAxis.Dimension)
        {
            foreach (var qualifier in declaredQualifiers)
            {
                if (qualifier.Axis == QualifierAxis.TemporalDimension)
                {
                    return qualifier;
                }
            }
        }

        foreach (var qualifier in Types.GetMeta(ownerType).ImpliedQualifiers)
        {
            if (qualifier.Axis == axis)
            {
                return qualifier;
            }
        }

        return null;
    }

    private static string FormatResolvedQualifierEntries(ImmutableArray<QualifierResolutionEntry> resolved)
    {
        if (resolved.Length == 1)
        {
            return FormatResolvedQualifierValue(resolved[0].Qualifier);
        }

        return string.Join(" · ", resolved.Select(entry => $"{GetQualifierDisplayName(entry.Axis)} {FormatResolvedQualifierValue(entry.Qualifier)}"));
    }

    private static string? CreateQualifierSourceEntry(TypeKind ownerType, QualifierResolutionEntry entry)
    {
        if (!TryDescribeQualifierSource(entry.Qualifier, ownerType, out var source))
        {
            return null;
        }

        return $"{GetQualifierDisplayName(entry.Axis)} {source.DisplayText}";
    }

    private static string FormatResolvedQualifierValue(DeclaredQualifierMeta? qualifier)
    {
        if (qualifier is null)
        {
            return "`<unresolved>`";
        }

        var rawValue = GetQualifierRawValue(qualifier);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return "`<unresolved>`";
        }

        if (TrySplitCompoundQualifier(rawValue, out var numerator, out var denominator))
        {
            return $"`'{EscapeInline(rawValue)}'` (numerator: `'{EscapeInline(numerator)}'`, denominator: `'{EscapeInline(denominator)}'`)";
        }

        return $"`'{EscapeInline(rawValue)}'`";
    }

    private static bool TryDescribeQualifierSource(DeclaredQualifierMeta? qualifier, TypeKind ownerType, out QualifierSourceInfo source)
    {
        if (qualifier is null)
        {
            source = null!;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(qualifier.SourceFieldName))
        {
            source = new QualifierSourceInfo($"field `{EscapeInline(qualifier.SourceFieldName!)}`", qualifier.SourceFieldName, true);
            return true;
        }

        var rawValue = GetQualifierRawValue(qualifier);
        if (!string.IsNullOrWhiteSpace(rawValue)
            && rawValue.Length > 2
            && rawValue[0] == '{'
            && rawValue[^1] == '}'
            && rawValue.Count(static ch => ch == '{') == 1
            && rawValue.Count(static ch => ch == '}') == 1)
        {
            var fieldName = SimplifyQualifierResolvedSource(rawValue);
            source = new QualifierSourceInfo($"field `{EscapeInline(fieldName)}`", fieldName, true);
            return true;
        }

        source = qualifier.Origin == QualifierOrigin.Derived
            ? new QualifierSourceInfo("derived from operand qualifiers")
            : new QualifierSourceInfo("declared explicitly on this type");
        return true;
    }

    private static string GetQualifierRawValue(DeclaredQualifierMeta qualifier) => qualifier switch
    {
        DeclaredQualifierMeta.Currency currency => currency.CurrencyCode,
        DeclaredQualifierMeta.FromCurrency currency => currency.CurrencyCode,
        DeclaredQualifierMeta.ToCurrency currency => currency.CurrencyCode,
        DeclaredQualifierMeta.Unit unit => unit.UnitCode,
        DeclaredQualifierMeta.Dimension dimension => dimension.DimensionName,
        DeclaredQualifierMeta.Timezone timezone => timezone.TimezoneId,
        DeclaredQualifierMeta.TemporalDimension dimension => dimension.Value.ToString().ToLowerInvariant(),
        DeclaredQualifierMeta.TemporalUnit unit => unit.UnitName,
        _ => string.Empty,
    };

    private static bool TrySplitCompoundQualifier(string rawValue, out string numerator, out string denominator)
    {
        var slashIndex = rawValue.IndexOf('/');
        if (slashIndex < 0)
        {
            numerator = string.Empty;
            denominator = string.Empty;
            return false;
        }

        numerator = rawValue[..slashIndex].Trim();
        denominator = rawValue[(slashIndex + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(numerator) && !string.IsNullOrWhiteSpace(denominator);
    }

    private static string DescribeQualifierValueShape(DeclaredQualifierMeta? qualifier, QualifierAxis axis)
    {
        if (qualifier is null)
        {
            return "unresolved";
        }

        var rawValue = GetQualifierRawValue(qualifier);
        if (TrySplitCompoundQualifier(rawValue, out var numerator, out var denominator))
        {
            return $"compound {GetQualifierAxisName(axis)} qualifier — numerator `'{EscapeInline(numerator)}'`, denominator `'{EscapeInline(denominator)}'`";
        }

        if (!string.IsNullOrWhiteSpace(qualifier.SourceFieldName)
            || (rawValue.Length > 2 && rawValue[0] == '{' && rawValue[^1] == '}'))
        {
            return $"symbolic {GetQualifierAxisName(axis)} qualifier";
        }

        return $"literal {GetQualifierAxisName(axis)} qualifier";
    }

    private static bool TryGetQualifierUnresolvedReason(TypeKind ownerType, ImmutableArray<QualifierAxis> missingAxes, out string reason)
    {
        if (missingAxes.IsDefaultOrEmpty)
        {
            reason = string.Empty;
            return false;
        }

        if (ownerType == TypeKind.ExchangeRate)
        {
            var hasFrom = missingAxes.Contains(QualifierAxis.FromCurrency);
            var hasTo = missingAxes.Contains(QualifierAxis.ToCurrency);
            reason = hasFrom && hasTo
                ? "exchange rate has no `in ... to ...` annotation"
                : hasFrom
                    ? "exchange rate has no `in ...` annotation"
                    : "exchange rate has no `to ...` annotation";
            return true;
        }

        var missing = missingAxes.Select(GetQualifierAnnotationLabel).Distinct().ToArray();
        reason = missing.Length switch
        {
            1 => $"{FormatType(ownerType)} declaration has no {missing[0]} annotation",
            _ => $"{FormatType(ownerType)} declaration has no {string.Join(" or ", missing)} annotation",
        };
        return true;
    }

    private static string GetQualifierAnnotationLabel(QualifierAxis axis) => axis switch
    {
        QualifierAxis.Currency or QualifierAxis.Unit or QualifierAxis.FromCurrency or QualifierAxis.Timezone or QualifierAxis.TemporalUnit => "`in ...`",
        QualifierAxis.Dimension or QualifierAxis.TemporalDimension => "`of ...`",
        QualifierAxis.ToCurrency => "`to ...`",
        _ => "qualifier",
    };

    private static string SimplifyQualifierResolvedSource(string template)
    {
        if (template.Length > 2
            && template[0] == '{'
            && template[^1] == '}'
            && template.Count(static ch => ch == '{') == 1
            && template.Count(static ch => ch == '}') == 1)
        {
            var inner = template[1..^1];
            if (inner.EndsWith(".dimension", StringComparison.Ordinal))
            {
                return inner[..^".dimension".Length];
            }

            return inner;
        }

        return template;
    }

    private static string GetQualifierAxisName(QualifierHoverInfo info) => info.ResolvedQualifier switch
    {
        DeclaredQualifierMeta.TemporalDimension or DeclaredQualifierMeta.TemporalUnit => GetQualifierAxisName(info.ResolvedQualifier.Axis),
        _ => GetQualifierAxisName(info.Axis),
    };

    private static string GetQualifierChecksText(QualifierHoverInfo info) => info.ResolvedQualifier switch
    {
        DeclaredQualifierMeta.TemporalDimension or DeclaredQualifierMeta.TemporalUnit => GetQualifierChecksText(info.ResolvedQualifier.Axis),
        _ => GetQualifierChecksText(info.Axis),
    };

    private static HoverStatusBadge BuildConstraintStatus(
        Compilation compilation,
        SourceSpan span,
        ConstraintIdentity identity,
        HoverStatusKind defaultKind,
        string defaultDetail)
    {
        var unresolved = GetConstraintObligations(compilation, identity);
        var diagnostics = GetDiagnosticsOverlapping(compilation, span, DiagnosticStage.Proof);
        if (unresolved.Length > 0 || diagnostics.Length > 0 || HasConstructDiagnostics(compilation, span))
        {
            var total = Math.Max(unresolved.Length, diagnostics.Length);
            return new HoverStatusBadge(HoverStatusKind.Unverified, $"{total} unresolved proof obligation{Pluralize(total)}");
        }

        return new HoverStatusBadge(defaultKind, defaultDetail);
    }

    private static HoverStatusBadge BuildStatus(
        Compilation compilation,
        SourceSpan span,
        HoverStatusKind defaultKind,
        string defaultDetail)
    {
        return HasConstructDiagnostics(compilation, span)
            ? new HoverStatusBadge(HoverStatusKind.Unverified, "diagnostics present on this construct")
            : new HoverStatusBadge(defaultKind, defaultDetail);
    }

    private static ConstraintInfluenceSummary GetConstraintInfluence(Compilation compilation, ConstraintIdentity identity)
    {
        var match = compilation.Proof.ConstraintInfluence
            .FirstOrDefault(entry => Equals(entry.Constraint, identity));

        return match is null
            ? new ConstraintInfluenceSummary(ImmutableArray<string>.Empty, ImmutableArray<EventArgReference>.Empty)
            : new ConstraintInfluenceSummary(match.ReferencedFields, match.ReferencedArgs);
    }

    private static ImmutableArray<ProofObligation> GetConstraintObligations(Compilation compilation, ConstraintIdentity identity) =>
        compilation.Proof.Obligations
            .Where(obligation => obligation.Disposition == ProofDisposition.Unresolved
                && obligation.Context is ConstraintContext context
                && Equals(context.Constraint, identity))
            .ToImmutableArray();

    private static bool HasUnresolvedConstraint(Compilation compilation, ConstraintIdentity identity) =>
        GetConstraintObligations(compilation, identity).Length > 0;

    private static ImmutableArray<ProofObligation> GetTransitionObligations(Compilation compilation, TypedTransitionRow row) =>
        compilation.Proof.Obligations
            .Where(obligation => obligation.Disposition == ProofDisposition.Unresolved
                && obligation.Context is TransitionRowContext context
                && context.Row.RowSpan == row.RowSpan)
            .ToImmutableArray();

    private static ImmutableArray<EdgeProofStatus> GetEdgeProofStatusesForState(Compilation compilation, string stateName) =>
        compilation.Graph.EdgeProofStatuses
            .Where(status => string.Equals(status.FromState, stateName, StringComparison.Ordinal)
                || string.Equals(status.ToState, stateName, StringComparison.Ordinal))
            .GroupBy(status => (status.FromState, status.EventName, status.ToState))
            .Select(group => group.First())
            .ToImmutableArray();

    private static string CreateStateGraphEdgeProofCard(TypedState state, ImmutableArray<EdgeProofStatus> edgeProofStatuses)
    {
        var lines = new List<string>
        {
            $"📍 {EscapePlain(state.Name)} graph position",
        };

        var gaps = edgeProofStatuses
            .Where(status => !status.IsProven)
            .ToImmutableArray();

        if (!gaps.IsEmpty)
        {
            lines.AddRange(gaps.Select(status =>
                $"⚠️ Gap · {EscapePlain(status.FromState)} --{EscapePlain(status.EventName)}--> {EscapePlain(status.ToState)} can't be proven"));
            return string.Join("\n\n", lines);
        }

        lines.Add(edgeProofStatuses.All(status => !status.HasObligations)
            ? "✅ Proven · no connected edges carry proof obligations"
            : "✅ Proven · all connected edges satisfy their proof obligations");
        return string.Join("\n\n", lines);
    }

    private static ImmutableArray<(TypedEnsure Ensure, EnsureIdentity Identity)> GetEnsuresForState(Compilation compilation, string stateName) =>
        compilation.Semantics.Ensures
            .Select((ensure, index) => (Ensure: ensure, Identity: new EnsureIdentity(ensure.Kind, ensure.AnchorState ?? ensure.AnchorEvent, index)))
            .Where(entry => string.Equals(entry.Ensure.AnchorState, stateName, StringComparison.Ordinal))
            .ToImmutableArray();

    private static ImmutableArray<(TypedEnsure Ensure, EnsureIdentity Identity)> GetEnsuresForEvent(Compilation compilation, string eventName) =>
        compilation.Semantics.Ensures
            .Select((ensure, index) => (Ensure: ensure, Identity: new EnsureIdentity(ensure.Kind, ensure.AnchorState ?? ensure.AnchorEvent, index)))
            .Where(entry => string.Equals(entry.Ensure.AnchorEvent, eventName, StringComparison.Ordinal))
            .ToImmutableArray();

    private static FieldWriteMap GetFieldWriteMapByState(Compilation compilation, TypedField field)
    {
        var states = compilation.Semantics.States.Select(state => state.Name).ToImmutableArray();
        if (states.IsEmpty)
        {
            return new FieldWriteMap(ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);
        }

        var omittedStates = states
            .Where(state => IsFieldOmittedInState(compilation, field.Name, state))
            .ToImmutableHashSet(StringComparer.Ordinal);
        var accesses = GetAccessDeclarations(compilation);
        ImmutableArray<string> writableStates;
        if (accesses.Length == 0 && field.Modifiers.Contains(ModifierKind.Writable))
        {
            writableStates = states
                .Where(state => !omittedStates.Contains(state))
                .ToImmutableArray();
        }
        else
        {
            writableStates = accesses
                .Where(access => !access.IsGuarded
                    && access.Mode == ModifierKind.Write
                    && access.FieldNames.Contains(field.Name, StringComparer.Ordinal))
                .Select(access => access.StateName)
                .Where(state => !omittedStates.Contains(state))
                .Distinct(StringComparer.Ordinal)
                .ToImmutableArray();
        }

        var writableSet = writableStates.ToImmutableHashSet(StringComparer.Ordinal);
        var lockedStates = states.Where(state => !writableSet.Contains(state)).ToImmutableArray();
        return new FieldWriteMap(writableStates, lockedStates);
    }

    private static ImmutableArray<string> GetWritableFieldsForState(Compilation compilation, string stateName)
    {
        var omittedFields = GetOmittedFieldsForState(compilation, stateName);
        if (omittedFields.Contains("all"))
        {
            return ImmutableArray<string>.Empty;
        }

        var accesses = GetAccessDeclarations(compilation)
            .Where(access => !access.IsGuarded
                && access.Mode == ModifierKind.Write
                && string.Equals(access.StateName, stateName, StringComparison.Ordinal))
            .SelectMany(access => access.FieldNames)
            .Where(fieldName => !omittedFields.Contains(fieldName))
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();

        if (!accesses.IsEmpty)
        {
            return accesses;
        }

        return compilation.Semantics.Fields
            .Where(field => field.Modifiers.Contains(ModifierKind.Writable)
                && !omittedFields.Contains(field.Name))
            .Select(field => field.Name)
            .ToImmutableArray();
    }

    private static ImmutableHashSet<string> GetOmittedFieldsForState(Compilation compilation, string stateName) =>
        GetOmitDeclarations(compilation)
            .Where(omit => string.Equals(omit.StateName, stateName, StringComparison.Ordinal))
            .Select(omit => omit.FieldName)
            .Where(fieldName => !string.IsNullOrWhiteSpace(fieldName))
            .ToImmutableHashSet(StringComparer.Ordinal);

    private static ImmutableArray<AccessDeclarationInfo> GetAccessDeclarations(Compilation compilation) =>
        compilation.Semantics.AccessModes
            .GroupBy(access => access.Syntax.Span)
            .Select(group => CreateAccessDeclarationInfo(compilation, group.First()))
            .ToImmutableArray();

    private static AccessDeclarationInfo CreateAccessDeclarationInfo(Compilation compilation, TypedAccessMode access)
    {
        var tokens = GetTokensInSpan(compilation.Tokens.Tokens, access.Syntax.Span);
        var fieldNames = ExtractFieldNames(tokens, TokenKind.Modify, Tokens.AccessModeKeywords.Contains);
        if (fieldNames.IsEmpty && !string.IsNullOrWhiteSpace(access.FieldName))
        {
            fieldNames = [access.FieldName];
        }

        var label = FormatSnippet(compilation, access.Syntax.Span);
        return new AccessDeclarationInfo(access.StateName, fieldNames, access.Mode, access.Guard is not null, access.Syntax.Span, label);
    }

    private static ImmutableArray<OmitDeclarationInfo> GetOmitDeclarations(Compilation compilation) =>
        compilation.ConstructManifest.ByKind[ConstructKind.OmitDeclaration]
            .Select(construct =>
            {
                var stateName = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget)?.StateName ?? string.Empty;
                var tokens = GetTokensInSpan(compilation.Tokens.Tokens, construct.Span);
                var fieldNames = ExtractFieldNames(tokens, TokenKind.Omit, kind => false);
                var fieldName = fieldNames.FirstOrDefault()
                    ?? construct.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget)?.FieldName
                    ?? string.Empty;
                return new OmitDeclarationInfo(stateName, fieldName, construct.Span, FormatSnippet(compilation, construct.Span));
            })
            .ToImmutableArray();

    private static bool IsFieldOmittedInState(Compilation compilation, string fieldName, string stateName) =>
        GetOmitDeclarations(compilation).Any(omit => string.Equals(omit.StateName, stateName, StringComparison.Ordinal)
            && (string.Equals(omit.FieldName, fieldName, StringComparison.Ordinal)
                || string.Equals(omit.FieldName, "all", StringComparison.Ordinal)));

    private static ImmutableArray<string> ExtractFieldNames(
        ImmutableArray<Token> tokens,
        TokenKind startAfter,
        Func<TokenKind, bool> isEndToken)
    {
        var startIndex = -1;
        for (var index = 0; index < tokens.Length; index++)
        {
            if (tokens[index].Kind == startAfter)
            {
                startIndex = index;
                break;
            }
        }

        if (startIndex < 0)
        {
            return ImmutableArray<string>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<string>();
        for (var index = startIndex + 1; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (isEndToken(token.Kind))
            {
                break;
            }

            if (token.Kind == TokenKind.Identifier)
            {
                builder.Add(token.Text);
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<Token> GetTokensInSpan(ImmutableArray<Token> tokens, SourceSpan span) =>
        tokens
            .Where(token => token.Kind is not TokenKind.NewLine and not TokenKind.EndOfSource && Overlaps(token.Span, span))
            .ToImmutableArray();

    private static string FormatConstraintGovernance(Compilation compilation, string fieldName)
    {
        var entries = compilation.Proof.ConstraintInfluence
            .Where(entry => entry.ReferencedFields.Contains(fieldName, StringComparer.Ordinal))
            .Select(entry => DescribeConstraint(compilation, entry.Constraint))
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();

        return entries.IsEmpty ? "none known at compile time" : string.Join(" · ", entries);
    }

    private static string DescribeConstraint(Compilation compilation, ConstraintIdentity identity) => identity switch
    {
        RuleIdentity rule => DescribeRule(compilation, rule),
        EnsureIdentity ensure => DescribeEnsure(compilation, ensure),
        _ => "constraint",
    };

    private static string DescribeRule(Compilation compilation, RuleIdentity identity)
    {
        if (identity.RuleIndex < 0 || identity.RuleIndex >= compilation.Semantics.Rules.Length)
        {
            return "rule";
        }

        var rule = compilation.Semantics.Rules[identity.RuleIndex];
        return TryGetMessageText(rule.Message, out var message)
            ? $"rule \"{EscapePlain(message)}\""
            : $"rule `{EscapeInline(FormatSnippet(compilation, rule.Condition.Span))}`";
    }

    private static string DescribeEnsure(Compilation compilation, EnsureIdentity identity)
    {
        if (identity.EnsureIndex < 0 || identity.EnsureIndex >= compilation.Semantics.Ensures.Length)
        {
            return "ensure";
        }

        var ensure = compilation.Semantics.Ensures[identity.EnsureIndex];
        var lead = ensure.Kind switch
        {
            ConstraintKind.StateResident => $"in {ensure.AnchorState} ensure",
            ConstraintKind.StateEntry => $"to {ensure.AnchorState} ensure",
            ConstraintKind.StateExit => $"from {ensure.AnchorState} ensure",
            ConstraintKind.EventPrecondition => $"on {ensure.AnchorEvent} ensure",
            _ => "ensure",
        };

        return TryGetMessageText(ensure.Message, out var message)
            ? $"{lead} \"{EscapePlain(message)}\""
            : $"{lead} `{EscapeInline(FormatSnippet(compilation, ensure.Condition.Span))}`";
    }

    private static void AppendConstraintInfluenceLines(List<string> lines, ConstraintInfluenceSummary influence)
    {
        if (!influence.ReferencedFields.IsDefaultOrEmpty)
        {
            lines.Add($"Referenced fields: {FormatCodeList(influence.ReferencedFields)}");
        }

        if (!influence.ReferencedArgs.IsDefaultOrEmpty)
        {
            lines.Add($"Referenced args: {FormatCodeList(influence.ReferencedArgs.Select(arg => arg.ArgName))}");
        }
    }

    private static bool TryFindQualifierAt(Compilation compilation, Position position, out QualifierHoverInfo info)
    {
        foreach (var field in compilation.Semantics.Fields)
        {
            if (TryFindQualifierAt(field.Syntax.GetSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression)?.TypeRef, field.ResolvedType, field.DeclaredQualifiers, compilation, position, out info))
            {
                return true;
            }
        }

        foreach (var evt in compilation.Semantics.Events)
        {
            var args = evt.Syntax.GetSlot<ArgumentListSlot>(ConstructSlotKind.ArgumentList)?.Args ?? ImmutableArray<ArgumentSyntax>.Empty;
            for (var index = 0; index < args.Length && index < evt.Args.Length; index++)
            {
                if (TryFindQualifierAt(args[index].Type, evt.Args[index].ResolvedType, evt.Args[index].DeclaredQualifiers, compilation, position, out info))
                {
                    return true;
                }
            }
        }

        info = null!;
        return false;
    }

    private static bool TryFindQualifierAt(
        ParsedTypeReference? typeRef,
        TypeKind ownerType,
        ImmutableArray<DeclaredQualifierMeta> declaredQualifiers,
        Compilation compilation,
        Position position,
        out QualifierHoverInfo info)
    {
        info = null!;
        if (typeRef is null)
        {
            return false;
        }

        switch (typeRef)
        {
            case QualifiedTypeReference qualified:
                foreach (var qualifier in qualified.Qualifiers)
                {
                    if (SymbolNavigation.Contains(qualifier.ValueSpan, position))
                    {
                        var resolved = ResolveDeclarationQualifier(ownerType, declaredQualifiers, qualifier.Axis);
                        var label = $"{GetPrepositionText(qualifier.Preposition)} {FormatSnippet(compilation, qualifier.ValueSpan)}";
                        info = new QualifierHoverInfo(qualifier.Axis, qualifier.ValueSpan, label, ownerType, resolved);
                        return true;
                    }
                }

                return TryFindQualifierAt(qualified.InnerType, ownerType, declaredQualifiers, compilation, position, out info);

            case CollectionTypeReference collection:
                return TryFindQualifierAt(collection.ElementType, ownerType, declaredQualifiers, compilation, position, out info)
                    || TryFindQualifierAt(collection.KeyType, ownerType, declaredQualifiers, compilation, position, out info);

            default:
                return false;
        }
    }

    private static string GetPrepositionText(TokenKind tokenKind) => Tokens.GetMeta(tokenKind).Text ?? tokenKind.ToString().ToLowerInvariant();

    private static string GetEnsureCardDetail(TypedEnsure ensure) => ensure.Kind switch
    {
        ConstraintKind.StateResident => "residency (always applies)",
        ConstraintKind.StateEntry => $"entry gate · `{EscapeInline(ensure.AnchorState ?? "<state>")}`",
        ConstraintKind.StateExit => $"exit gate · `{EscapeInline(ensure.AnchorState ?? "<state>")}`",
        ConstraintKind.EventPrecondition => $"arg gate · `{EscapeInline(ensure.AnchorEvent ?? "<event>")}`",
        _ => "ensure",
    };

    private static string GetEnsureProofLabel(TypedEnsure ensure) => ensure.Kind switch
    {
        ConstraintKind.StateResident => $"in {EscapeInline(ensure.AnchorState ?? "<state>")}",
        ConstraintKind.StateEntry => $"to {EscapeInline(ensure.AnchorState ?? "<state>")}",
        ConstraintKind.StateExit => $"from {EscapeInline(ensure.AnchorState ?? "<state>")}",
        ConstraintKind.EventPrecondition => $"on {EscapeInline(ensure.AnchorEvent ?? "<event>")}",
        _ => "ensure",
    };

    private static string DescribeStateReachability(Compilation compilation, TypedState state, GraphState? graphState)
    {
        if (graphState is null || !graphState.IsReachable)
        {
            return "unreachable from the initial state";
        }

        if (state.Modifiers.Contains(ModifierKind.Required))
        {
            return "reachable; every initial→terminal path visits here";
        }

        var reachability = compilation.Graph.ProofFacts
            .OfType<ReachabilityFact>()
            .FirstOrDefault(fact => string.Equals(fact.StateName, state.Name, StringComparison.Ordinal));
        if (reachability?.PathFromInitial is { Length: > 1 } path)
        {
            return $"reachable from `{EscapeInline(path[^2])}`";
        }

        return state.Modifiers.Contains(ModifierKind.InitialState)
            ? "initial state"
            : "reachable";
    }

    private static bool IsTerminalReachable(StateGraph graph, string stateName)
    {
        var terminals = graph.States.Where(state => state.IsTerminal).Select(state => state.Name).ToImmutableArray();
        if (terminals.IsEmpty)
        {
            return false;
        }

        var canReachTerminal = new HashSet<string>(terminals, StringComparer.Ordinal);
        var queue = new Queue<string>(terminals);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var predecessor in graph.Edges.Where(edge => string.Equals(edge.ToState, current, StringComparison.Ordinal)).Select(edge => edge.FromState))
            {
                if (canReachTerminal.Add(predecessor))
                {
                    queue.Enqueue(predecessor);
                }
            }
        }

        return canReachTerminal.Contains(stateName);
    }

    private static string DescribeTransitionReachability(StateGraph graph, TypedTransitionRow row)
    {
        var source = row.FromState is null
            ? "wildcard source"
            : graph.ReachableStates.Contains(row.FromState)
                ? "source reachable"
                : $"source `{EscapeInline(row.FromState)}` unreachable";

        var target = row.Outcome switch
        {
            TransitionRowOutcome.Transition when row.TargetState is not null && graph.ReachableStates.Contains(row.TargetState)
                => $"target `{EscapeInline(row.TargetState)}` reachable",
            TransitionRowOutcome.Transition when row.TargetState is not null
                => $"target `{EscapeInline(row.TargetState)}` unreachable",
            TransitionRowOutcome.Reject => "reject outcome",
            _ => "state unchanged",
        };

        return $"{source} · {target}";
    }

    private static string FormatTransitionHeader(TypedTransitionRow row)
    {
        var from = row.FromState ?? "*";
        return row.Outcome switch
        {
            TransitionRowOutcome.Transition => $"from {from} on {row.EventName} -> {row.TargetState}",
            TransitionRowOutcome.Reject => $"from {from} on {row.EventName} -> reject",
            _ => $"from {from} on {row.EventName} -> no transition",
        };
    }

    private static string FormatRejectHeader(TypedTransitionRow row) => $"from {row.FromState ?? "*"} on {row.EventName} -> reject";

    private static ImmutableArray<string> GetProofGapCategories(ImmutableArray<PreceptDiagnostic> diagnostics)
    {
        var categories = diagnostics
            .Select(diagnostic => TryGetDiagnosticCategory(diagnostic, out var category)
                ? FormatCategoryName(category)
                : "proof")
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();

        return categories.IsEmpty ? ["proof"] : categories;
    }

    private static bool TryGetDiagnosticCategory(PreceptDiagnostic diagnostic, out DiagnosticCategory category)
    {
        if (Enum.TryParse<PreceptDiagnosticCode>(diagnostic.Code, out var code))
        {
            category = Diagnostics.GetMeta(code).Category;
            return true;
        }

        category = default;
        return false;
    }

    private static string FormatCategoryName(DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.TypeSystem => "type system",
        DiagnosticCategory.BusinessDomain => "business domain",
        _ => category.ToString().ToLowerInvariant(),
    };

    private static string FormatActionSummary(TypedAction action)
    {
        var verb = Actions.GetMeta(action.Kind).Token.Text ?? action.Kind.ToString().ToLowerInvariant();
        return $"{verb} {action.FieldName}";
    }

    private static string FormatOutgoingEdge(GraphEdge edge) => edge.Outcome switch
    {
        TransitionRowOutcome.Transition => $"{edge.EventName} → {edge.ToState}",
        TransitionRowOutcome.Reject => $"{edge.EventName} → reject",
        _ => $"{edge.EventName} → no transition",
    };

    private static string FormatArgumentSignaturePart(TypedArg arg)
    {
        var type = $"{FormatType(arg.ResolvedType, arg.ElementType)}{FormatQualifierSuffix(arg.DeclaredQualifiers)}";
        if (arg.Presence is DeclaredPresenceMeta.Optional)
        {
            type += "?";
        }

        return $"{EscapeInline(arg.Name)} as {type}";
    }

    private static string FormatTypeSummary(
        TypeKind kind,
        TypeKind? elementType,
        TypeKind? keyType,
        ImmutableArray<DeclaredQualifierMeta> qualifiers,
        DeclaredPresenceMeta presence)
    {
        var parts = new List<string>
        {
            $"`{EscapeInline(FormatType(kind, elementType, keyType))}`",
            presence is DeclaredPresenceMeta.Optional ? "optional" : "not nullable",
        };

        if (!qualifiers.IsDefaultOrEmpty)
        {
            parts.AddRange(qualifiers.Select(FormatDeclaredQualifier));
        }

        return string.Join(" · ", parts);
    }

    private static string FormatQualifierSuffix(ImmutableArray<DeclaredQualifierMeta> qualifiers) =>
        qualifiers.IsDefaultOrEmpty
            ? string.Empty
            : $" {string.Join(" ", qualifiers.Select(qualifier => FormatDeclaredQualifier(qualifier).Trim('`')))}";

    private static string FormatDeclaredQualifier(DeclaredQualifierMeta qualifier) => qualifier switch
    {
        DeclaredQualifierMeta.Currency currency => $"`in {EscapeInline(currency.CurrencyCode)}`",
        DeclaredQualifierMeta.FromCurrency currency => $"`in {EscapeInline(currency.CurrencyCode)}`",
        DeclaredQualifierMeta.ToCurrency currency => $"`to {EscapeInline(currency.CurrencyCode)}`",
        DeclaredQualifierMeta.Unit unit => $"`in {EscapeInline(unit.UnitCode)}`",
        DeclaredQualifierMeta.Dimension dimension => $"`of {EscapeInline(dimension.DimensionName)}`",
        DeclaredQualifierMeta.Timezone timezone => $"`in {EscapeInline(timezone.TimezoneId)}`",
        DeclaredQualifierMeta.TemporalDimension dimension => $"`of {EscapeInline(dimension.Value.ToString().ToLowerInvariant())}`",
        DeclaredQualifierMeta.TemporalUnit unit => $"`in {EscapeInline(unit.UnitName)}`",
        _ => $"`{EscapeInline(qualifier.Axis.ToString())}`",
    };

    private static string GetQualifierAxisName(QualifierAxis axis) => axis switch
    {
        QualifierAxis.Currency => "currency",
        QualifierAxis.Unit => "unit of measure",
        QualifierAxis.Dimension => "physical dimension",
        QualifierAxis.FromCurrency => "source currency",
        QualifierAxis.ToCurrency => "target currency",
        QualifierAxis.Timezone => "timezone",
        QualifierAxis.TemporalDimension => "temporal dimension",
        QualifierAxis.TemporalUnit => "temporal unit",
        _ => "qualifier",
    };

    private static string GetQualifierDisplayName(QualifierAxis axis) => axis switch
    {
        QualifierAxis.Currency => "Currency",
        QualifierAxis.Unit => "Unit",
        QualifierAxis.Dimension => "Dimension",
        QualifierAxis.FromCurrency => "Source currency",
        QualifierAxis.ToCurrency => "Target currency",
        QualifierAxis.Timezone => "Timezone",
        QualifierAxis.TemporalDimension => "Temporal dimension",
        QualifierAxis.TemporalUnit => "Temporal unit",
        _ => "Qualifier",
    };

    private static string GetQualifierChecksText(QualifierAxis axis) => axis switch
    {
        QualifierAxis.Currency => "assignments, comparisons, and arithmetic stay currency-compatible",
        QualifierAxis.Unit => "assignments, comparisons, and arithmetic stay unit-compatible",
        QualifierAxis.Dimension => "assignments, comparisons, and arithmetic stay dimension-compatible",
        QualifierAxis.FromCurrency or QualifierAxis.ToCurrency => "currency-conversion expressions keep source/target currencies aligned",
        QualifierAxis.Timezone => "temporal values keep timezone-sensitive operations aligned",
        QualifierAxis.TemporalDimension or QualifierAxis.TemporalUnit => "temporal arithmetic stays category-compatible",
        _ => "assignments and comparisons stay compatible",
    };

    private static string GetQualifierMismatchText(QualifierAxis axis) => axis switch
    {
        QualifierAxis.Currency or QualifierAxis.FromCurrency or QualifierAxis.ToCurrency => "Mixed currencies aren't allowed",
        QualifierAxis.Unit => "Mixed units aren't allowed",
        QualifierAxis.Dimension => "Mixed physical dimensions aren't allowed",
        QualifierAxis.Timezone => "Mixed timezones aren't allowed",
        QualifierAxis.TemporalDimension => "Mixed temporal dimensions aren't allowed",
        QualifierAxis.TemporalUnit => "Mixed temporal units aren't allowed",
        _ => "Incompatible qualifiers aren't allowed",
    };

    private static string CapitalizeFirst(string value) => string.IsNullOrEmpty(value)
        ? value
        : char.ToUpperInvariant(value[0]) + value[1..];

    private static string FormatModifierName(ModifierKind modifier) => modifier.ToString().ToLowerInvariant();

    private static string FormatType(TypeKind kind, TypeKind? elementType = null, TypeKind? keyType = null)
    {
        var displayName = Types.GetMeta(kind).DisplayName;

        if (kind == TypeKind.Lookup && keyType is { } lookupKeyType && elementType is { } lookupValueType)
        {
            return $"{displayName} of {FormatType(lookupKeyType)} to {FormatType(lookupValueType)}";
        }

        if ((kind == TypeKind.QueueBy || kind == TypeKind.LogBy) && elementType is { } orderedItemType && keyType is { } orderKeyType)
        {
            return $"{displayName} of {FormatType(orderedItemType)} by {FormatType(orderKeyType)}";
        }

        if (elementType is { } itemType && kind is TypeKind.Set or TypeKind.Queue or TypeKind.Stack or TypeKind.Log or TypeKind.Bag or TypeKind.List)
        {
            return $"{displayName} of {FormatType(itemType)}";
        }

        return displayName;
    }

    private static string FormatStatus(HoverStatusBadge status) => status.Kind switch
    {
        HoverStatusKind.ProofVerified => $"✅ Proven · {status.Detail}",
        HoverStatusKind.RuntimeChecked => $"⚡ Enforced · {status.Detail}",
        _ => $"⚠️ Gap · {status.Detail}",
    };

    private static string FormatCodeList(IEnumerable<string> values)
    {
        var array = values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
        return array.Length == 0
            ? "*none*"
            : string.Join(", ", array.Select(value => $"`{EscapeInline(value)}`"));
    }

    private static string FormatSnippet(Compilation compilation, SourceSpan span)
    {
        var tokens = GetTokensInSpan(compilation.Tokens.Tokens, span);
        if (tokens.IsEmpty)
        {
            return string.Empty;
        }

        return string.Join(" ", tokens.Select(GetTokenText)).Trim();
    }

    private static string GetTokenText(Token token) =>
        token.Text
        ?? Tokens.GetMeta(token.Kind).Text
        ?? token.Kind.ToString().ToLowerInvariant();

    private static ImmutableArray<PreceptDiagnostic> GetDiagnosticsOverlapping(
        Compilation compilation,
        SourceSpan span,
        DiagnosticStage? stage = null) =>
        compilation.Diagnostics
            .Where(diagnostic => diagnostic.Severity is Severity.Warning or Severity.Error
                && Overlaps(diagnostic.Span, span)
                && (stage is null || diagnostic.Stage == stage.Value))
            .ToImmutableArray();

    private static bool HasConstructDiagnostics(Compilation compilation, SourceSpan span) =>
        GetDiagnosticsOverlapping(compilation, span).Length > 0;

    private static bool Overlaps(SourceSpan left, SourceSpan right) =>
        left.End > right.Offset && right.End > left.Offset;

    private static bool TryGetMessageText(TypedExpression message, out string text)
    {
        if (message is TypedLiteral { Value: string value } && !string.IsNullOrWhiteSpace(value))
        {
            text = value;
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static Hover MakeHover(string markdown, SourceSpan span) => new()
    {
        Contents = new MarkedStringsOrMarkupContent(new MarkupContent
        {
            Kind = MarkupKind.Markdown,
            Value = markdown,
        }),
        Range = DiagnosticProjector.ToRange(span),
    };

    private static string EscapeInline(string value) => value.Replace("`", "\\`");

    private static string EscapePlain(string value) => value.Replace("\r", string.Empty).Replace("\n", " ").Trim();

    private static string EscapeBlockquote(string value) => EscapePlain(value).Replace("\n", " ");

    private static string Pluralize(int count) => count == 1 ? string.Empty : "s";

    private sealed record HoverStatusBadge(HoverStatusKind Kind, string Detail);

    private enum HoverStatusKind
    {
        ProofVerified,
        RuntimeChecked,
        Unverified,
    }

    private sealed record ConstraintInfluenceSummary(
        ImmutableArray<string> ReferencedFields,
        ImmutableArray<EventArgReference> ReferencedArgs);

    private sealed record QualifierResolutionEntry(
        QualifierAxis Axis,
        DeclaredQualifierMeta? Qualifier);

    private sealed record QualifierSourceInfo(
        string DisplayText,
        string? FieldName = null,
        bool IsFieldSource = false);

    private sealed record FieldProofGap(
        string Use,
        string Evidence);

    private sealed record FieldWriteMap(
        ImmutableArray<string> WritableStates,
        ImmutableArray<string> LockedStates);

    private sealed record AccessDeclarationInfo(
        string StateName,
        ImmutableArray<string> FieldNames,
        ModifierKind Mode,
        bool IsGuarded,
        SourceSpan Span,
        string Label);

    private sealed record OmitDeclarationInfo(
        string StateName,
        string FieldName,
        SourceSpan Span,
        string Label);

    private sealed record QualifierHoverInfo(
        QualifierAxis Axis,
        SourceSpan Span,
        string Label,
        TypeKind OwnerType,
        DeclaredQualifierMeta? ResolvedQualifier);
}
