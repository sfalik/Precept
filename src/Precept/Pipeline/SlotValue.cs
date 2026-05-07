using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

// ── SlotValue DU ──────────────────────────────────────────────────────────────
//
// Option F discriminated union: one sealed subtype per ConstructSlotKind member.
// 17 kinds, 17 subtypes — one-to-one mirror of the ConstructSlotKind catalog.
// The parser fills slot arrays; downstream consumers pattern-match on subtypes.

/// <summary>
/// Abstract base of the SlotValue discriminated union. Each subtype corresponds
/// to exactly one <see cref="ConstructSlotKind"/> catalog member.
/// </summary>
public abstract record SlotValue(ConstructSlotKind Kind, SourceSpan Span);

/// <summary>One or more user-defined names (e.g. field names, event names).</summary>
public sealed record IdentifierListSlot(ImmutableArray<string> Names, SourceSpan Span)
    : SlotValue(ConstructSlotKind.IdentifierList, Span);

/// <summary>"as TypeKeyword Qualifiers" type annotation.</summary>
public sealed record TypeExpressionSlot(TypeMeta Type, SourceSpan Span)
    : SlotValue(ConstructSlotKind.TypeExpression, Span);

/// <summary>Field modifiers (nonnegative, positive, notempty, etc.).</summary>
public sealed record ModifierListSlot(ImmutableArray<ModifierKind> Modifiers, SourceSpan Span)
    : SlotValue(ConstructSlotKind.ModifierList, Span);

/// <summary>Comma-separated (name modifier*) pairs for state declarations.</summary>
public sealed record StateEntryListSlot(ImmutableArray<(string Name, ImmutableArray<ModifierKind> Modifiers)> Entries, SourceSpan Span)
    : SlotValue(ConstructSlotKind.StateEntryList, Span);

/// <summary>Event parameter list "(name as type, ...)".</summary>
public sealed record ArgumentListSlot(ImmutableArray<(string Name, TypeMeta Type)> Args, SourceSpan Span)
    : SlotValue(ConstructSlotKind.ArgumentList, Span);

/// <summary>"-> expression" computed value.</summary>
public sealed record ComputeExpressionSlot(ParsedExpression Expression, SourceSpan Span)
    : SlotValue(ConstructSlotKind.ComputeExpression, Span);

/// <summary>"when expression" guard clause.</summary>
public sealed record GuardClauseSlot(ParsedExpression Expression, SourceSpan Span)
    : SlotValue(ConstructSlotKind.GuardClause, Span);

/// <summary>"-> action -> action" chain.</summary>
public sealed record ActionChainSlot(ImmutableArray<ActionKind> Actions, SourceSpan Span)
    : SlotValue(ConstructSlotKind.ActionChain, Span);

/// <summary>"-> transition State | -> no transition | -> reject 'reason'" outcome.</summary>
public sealed record OutcomeSlot(ParsedOutcome Outcome, SourceSpan Span)
    : SlotValue(ConstructSlotKind.Outcome, Span);

/// <summary>State name or quantifier (any).</summary>
public sealed record StateTargetSlot(string? StateName, SourceSpan Span)
    : SlotValue(ConstructSlotKind.StateTarget, Span);

/// <summary>Event name (or "initial" marker).</summary>
public sealed record EventTargetSlot(string? EventName, SourceSpan Span)
    : SlotValue(ConstructSlotKind.EventTarget, Span);

/// <summary>"ensure expression because message" clause.</summary>
public sealed record EnsureClauseSlot(ParsedExpression Expression, SourceSpan Span)
    : SlotValue(ConstructSlotKind.EnsureClause, Span);

/// <summary>"because message" clause.</summary>
public sealed record BecauseClauseSlot(string Message, SourceSpan Span)
    : SlotValue(ConstructSlotKind.BecauseClause, Span);

/// <summary>readonly | editable access mode adjective.</summary>
public sealed record AccessModeSlot(TokenKind AccessMode, SourceSpan Span)
    : SlotValue(ConstructSlotKind.AccessModeKeyword, Span);

/// <summary>Field name or "all".</summary>
public sealed record FieldTargetSlot(string? FieldName, SourceSpan Span)
    : SlotValue(ConstructSlotKind.FieldTarget, Span);

/// <summary>The rule's boolean expression (e.g. amount > 0).</summary>
public sealed record RuleExpressionSlot(ParsedExpression Expression, SourceSpan Span)
    : SlotValue(ConstructSlotKind.RuleExpression, Span);

/// <summary>Optional "initial" keyword on event declarations.</summary>
public sealed record InitialMarkerSlot(bool IsPresent, SourceSpan Span)
    : SlotValue(ConstructSlotKind.InitialMarker, Span);
