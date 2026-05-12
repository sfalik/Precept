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
            FieldOccurrence field => MakeHover(CreateFieldMarkdown(compilation, field.Field), token.Span),
            StateOccurrence state => MakeHover(CreateStateMarkdown(compilation, state.State), token.Span),
            EventOccurrence evt => MakeHover(CreateEventMarkdown(compilation, evt.Event), token.Span),
            ArgOccurrence arg => MakeHover(CreateArgumentMarkdown(arg.Arg), token.Span),
            _ => null,
        };

        hover = symbolHover!;
        return symbolHover is not null;
    }

    private static string CreateFieldMarkdown(Compilation compilation, TypedField field)
    {
        var status = field.IsComputed
            ? BuildStatus(
                compilation,
                field.ComputedExpression?.Span ?? field.NameSpan,
                HoverStatusKind.RuntimeChecked,
                "recomputed on every mutation before commit")
            : BuildStatus(
                compilation,
                field.NameSpan,
                HoverStatusKind.RuntimeChecked,
                "enforced on every mutation before commit");

        var lines = new List<string>
        {
            field.IsComputed ? $"**computed field `{EscapeInline(field.Name)}`**" : $"**field `{EscapeInline(field.Name)}`**",
            FormatStatus(status),
            $"Type: {FormatTypeSummary(field.ResolvedType, field.ElementType, field.KeyType, field.DeclaredQualifiers, field.Presence)}",
        };

        if (field.IsComputed && field.ComputedExpression is not null)
        {
            lines.Add($"Computed from: `{EscapeInline(FormatSnippet(compilation, field.ComputedExpression.Span))}`");
        }
        else if (!compilation.Semantics.States.IsEmpty)
        {
            var writeMap = GetFieldWriteMapByState(compilation, field);
            lines.Add($"Writable: {FormatCodeList(writeMap.WritableStates)} · Read-only: {FormatCodeList(writeMap.LockedStates)}");
        }

        lines.Add($"Governed by: {FormatConstraintGovernance(compilation, field.Name)}");
        return string.Join("\n\n", lines);
    }

    private static string CreateStateMarkdown(Compilation compilation, TypedState state)
    {
        var graphState = compilation.Graph.States.FirstOrDefault(candidate => string.Equals(candidate.Name, state.Name, StringComparison.Ordinal));
        var detail = DescribeStateReachability(compilation, state, graphState);
        var statusKind = HasConstructDiagnostics(compilation, state.NameSpan) || graphState is null || !graphState.IsReachable
            ? HoverStatusKind.Unverified
            : HoverStatusKind.ProofVerified;
        var status = new HoverStatusBadge(statusKind, detail);

        var modifiers = state.Modifiers.IsDefaultOrEmpty ? "*none*" : FormatCodeList(state.Modifiers.Select(FormatModifierName));
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
        var terminalReachable = IsTerminalReachable(compilation.Graph, state.Name);
        var titleSuffix = state.Modifiers.IsDefaultOrEmpty
            ? string.Empty
            : $" · {FormatCodeList(state.Modifiers.Select(FormatModifierName))}";

        var lines = new List<string>
        {
            $"**state `{EscapeInline(state.Name)}`**{titleSuffix}",
            FormatStatus(status),
            $"Modifiers: {modifiers}",
            $"Incoming: {FormatCodeList(incoming)}",
            $"Outgoing: {FormatCodeList(outgoing)}",
            $"Writable here: {FormatCodeList(writable)}",
            $"{(terminalReachable ? "Terminal reachable" : "No terminal path")} · active ensures: {activeEnsures.Length}{(unverifiedEnsures > 0 ? $" ({unverifiedEnsures} unverified)" : string.Empty)}",
        };

        return string.Join("\n\n", lines);
    }

    private static string CreateEventMarkdown(Compilation compilation, TypedEvent evt)
    {
        var signature = evt.Args.IsDefaultOrEmpty
            ? EscapeInline(evt.Name)
            : $"{EscapeInline(evt.Name)}({string.Join(", ", evt.Args.Select(FormatArgumentSignaturePart))})";
        if (evt.IsInitial)
        {
            signature = $"initial {signature}";
        }

        var hasEventEnsureGap = GetEnsuresForEvent(compilation, evt.Name)
            .Any(entry => HasUnresolvedConstraint(compilation, entry.Identity));
        var status = evt.IsInitial
            ? BuildStatus(compilation, evt.NameSpan, hasEventEnsureGap ? HoverStatusKind.Unverified : HoverStatusKind.RuntimeChecked, "constructor event (invoked via `CreateInstance`, not `Fire`)")
            : BuildStatus(compilation, evt.NameSpan, hasEventEnsureGap ? HoverStatusKind.Unverified : HoverStatusKind.RuntimeChecked, "args validated before transition");

        var lines = new List<string>
        {
            $"**event `{signature}`**",
            FormatStatus(status),
        };

        if (!evt.IsInitial)
        {
            var handledInStates = compilation.Graph.Events
                .FirstOrDefault(candidate => string.Equals(candidate.Name, evt.Name, StringComparison.Ordinal))?
                .HandledInStates
                ?? ImmutableArray<string>.Empty;
            lines.Add($"Can fire from: {FormatCodeList(handledInStates)}");
        }

        foreach (var arg in evt.Args)
        {
            lines.Add($"Arg: `{EscapeInline(arg.Name)}` is {FormatTypeSummary(arg.ResolvedType, arg.ElementType, null, arg.DeclaredQualifiers, arg.Presence)}");
        }

        return string.Join("\n\n", lines);
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
        var status = BuildConstraintStatus(compilation, rule.Syntax.Span, identity, HoverStatusKind.RuntimeChecked, "enforced after every mutation before commit");
        var title = rule.Guard is null
            ? FormatSnippet(compilation, rule.Condition.Span)
            : $"when {FormatSnippet(compilation, rule.Guard.Span)}: {FormatSnippet(compilation, rule.Condition.Span)}";

        var lines = new List<string>
        {
            $"**rule** `{EscapeInline(title)}`",
        };

        if (TryGetMessageText(rule.Message, out var message))
        {
            lines.Add($"> {EscapeBlockquote(message)}");
        }

        lines.Add(FormatStatus(status));
        lines.Add(rule.Guard is null
            ? "Scope: global — enforced after every mutation"
            : $"Scope: global when `{EscapeInline(FormatSnippet(compilation, rule.Guard.Span))}`");
        lines.Add("If false: the operation is rejected before commit");
        AppendConstraintInfluenceLines(lines, influence);
        return string.Join("\n\n", lines);
    }

    private static string CreateEnsureMarkdown(Compilation compilation, TypedEnsure ensure, EnsureIdentity identity)
    {
        var influence = GetConstraintInfluence(compilation, identity);
        var status = BuildConstraintStatus(compilation, ensure.Syntax.Span, identity, HoverStatusKind.RuntimeChecked, GetEnsureStatusDetail(ensure));
        var title = ensure.Kind switch
        {
            ConstraintKind.StateResident => $"in {ensure.AnchorState} ensure {FormatSnippet(compilation, ensure.Condition.Span)}",
            ConstraintKind.StateEntry => $"to {ensure.AnchorState} ensure {FormatSnippet(compilation, ensure.Condition.Span)}",
            ConstraintKind.StateExit => $"from {ensure.AnchorState} ensure {FormatSnippet(compilation, ensure.Condition.Span)}",
            ConstraintKind.EventPrecondition => $"on {ensure.AnchorEvent} ensure {FormatSnippet(compilation, ensure.Condition.Span)}",
            _ => $"ensure {FormatSnippet(compilation, ensure.Condition.Span)}",
        };

        var lines = new List<string>
        {
            $"**ensure** `{EscapeInline(title)}`",
        };

        if (TryGetMessageText(ensure.Message, out var message))
        {
            lines.Add($"> {EscapeBlockquote(message)}");
        }

        lines.Add(FormatStatus(status));
        lines.Add($"Scope: {GetEnsureScopeLine(ensure)}");
        AppendConstraintInfluenceLines(lines, influence);
        lines.Add($"Violation rejects {GetEnsureViolationTarget(ensure)}");
        return string.Join("\n\n", lines);
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
            lines.Add($"Proof gap: {proofGapCount} unresolved obligation{Pluralize(proofGapCount)} ({string.Join(", ", categories)})");
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
        var status = BuildStatus(compilation, info.Span, HoverStatusKind.ProofVerified, "qualifier compatibility checked at compile time");
        var axisName = GetQualifierAxisName(info.Axis);
        var lines = new List<string>
        {
            $"**qualifier** `{EscapeInline(info.Label)}`",
            FormatStatus(status),
            $"Axis: {axisName}",
            $"Checks: {GetQualifierChecksText(info.Axis)}",
            "Mismatch: incompatible combinations are rejected",
        };

        return string.Join("\n\n", lines);
    }

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

        var accesses = GetAccessDeclarations(compilation);
        ImmutableArray<string> writableStates;
        if (accesses.Length == 0 && field.Modifiers.Contains(ModifierKind.Writable))
        {
            writableStates = states;
        }
        else
        {
            writableStates = accesses
                .Where(access => access.Mode == ModifierKind.Write
                    && access.FieldNames.Contains(field.Name, StringComparer.Ordinal))
                .Select(access => access.StateName)
                .Distinct(StringComparer.Ordinal)
                .ToImmutableArray();
        }

        var writableSet = writableStates.ToImmutableHashSet(StringComparer.Ordinal);
        var lockedStates = states.Where(state => !writableSet.Contains(state)).ToImmutableArray();
        return new FieldWriteMap(writableStates, lockedStates);
    }

    private static ImmutableArray<string> GetWritableFieldsForState(Compilation compilation, string stateName)
    {
        var accesses = GetAccessDeclarations(compilation)
            .Where(access => access.Mode == ModifierKind.Write && string.Equals(access.StateName, stateName, StringComparison.Ordinal))
            .SelectMany(access => access.FieldNames)
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();

        if (!accesses.IsEmpty)
        {
            return accesses;
        }

        return compilation.Semantics.Fields
            .Where(field => field.Modifiers.Contains(ModifierKind.Writable))
            .Select(field => field.Name)
            .ToImmutableArray();
    }

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
        return new AccessDeclarationInfo(access.StateName, fieldNames, access.Mode, access.Syntax.Span, label);
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
            && string.Equals(omit.FieldName, fieldName, StringComparison.Ordinal));

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
                        var resolved = declaredQualifiers.FirstOrDefault(candidate => candidate.Axis == qualifier.Axis);
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

    private static string GetEnsureStatusDetail(TypedEnsure ensure) => ensure.Kind switch
    {
        ConstraintKind.StateResident => "enforced on every mutation before commit",
        ConstraintKind.StateEntry => $"enforced on transitions entering `{EscapeInline(ensure.AnchorState ?? "<state>")}`",
        ConstraintKind.StateExit => $"enforced on transitions leaving `{EscapeInline(ensure.AnchorState ?? "<state>")}`",
        ConstraintKind.EventPrecondition => $"enforced when `{EscapeInline(ensure.AnchorEvent ?? "<event>")}` fires",
        _ => "enforced before commit",
    };

    private static string GetEnsureScopeLine(TypedEnsure ensure) => ensure.Kind switch
    {
        ConstraintKind.StateResident => $"residency (`in {EscapeInline(ensure.AnchorState ?? "<state>")}`)",
        ConstraintKind.StateEntry => $"entry gate (`to {EscapeInline(ensure.AnchorState ?? "<state>")}`)",
        ConstraintKind.StateExit => $"exit gate (`from {EscapeInline(ensure.AnchorState ?? "<state>")}`)",
        ConstraintKind.EventPrecondition => $"event args (`on {EscapeInline(ensure.AnchorEvent ?? "<event>")}`)",
        _ => "ensure",
    };

    private static string GetEnsureViolationTarget(TypedEnsure ensure) => ensure.Kind switch
    {
        ConstraintKind.EventPrecondition => "the event",
        ConstraintKind.StateEntry or ConstraintKind.StateExit => "the transition",
        _ => "the operation",
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

    private static string FormatArgumentSignaturePart(TypedArg arg) =>
        $"{EscapeInline(arg.Name)} as {FormatType(arg.ResolvedType, arg.ElementType)}{FormatQualifierSuffix(arg.DeclaredQualifiers)}";

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
        HoverStatusKind.ProofVerified => $"✅ **Proof verified** — {status.Detail}",
        HoverStatusKind.RuntimeChecked => $"⚡ **Runtime checked** — {status.Detail}",
        _ => $"⚠️ **Unverified** — {status.Detail}",
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

    private sealed record FieldWriteMap(
        ImmutableArray<string> WritableStates,
        ImmutableArray<string> LockedStates);

    private sealed record AccessDeclarationInfo(
        string StateName,
        ImmutableArray<string> FieldNames,
        ModifierKind Mode,
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
