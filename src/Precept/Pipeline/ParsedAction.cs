using Precept.Language;

namespace Precept.Pipeline;

// ── ParsedAction DU ────────────────────────────────────────────────────────────
//
// Option C discriminated union: shape-based sealed subtypes, each carrying ActionKind.
// The type checker dispatches on shape (the sealed subtype); diagnostics/tooling reads Kind.
// Derived from the ActionSyntaxShape enum in the Actions catalog.

/// <summary>
/// Abstract base of the ParsedAction discriminated union.
/// Each subtype carries the ActionKind field (for diagnostics/tooling) plus the
/// operand expressions needed by the type checker.
/// </summary>
public abstract record ParsedAction(ActionKind Kind, SourceSpan Span);

/// <summary>
/// Assignment action: verb field = expression
/// Shape: AssignValue (1)
/// Actions: set
/// </summary>
public sealed record AssignAction(
    ActionKind Kind,
    ParsedExpression Target,
    ParsedExpression Value,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Collection value action: verb field expression
/// Shape: CollectionValue (2)
/// Actions: add, remove, enqueue, push, append
/// </summary>
public sealed record CollectionValueAction(
    ActionKind Kind,
    ParsedExpression Target,
    ParsedExpression Value,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Collection into action: verb field [into field]
/// Shape: CollectionInto (3)
/// Actions: dequeue, pop
/// </summary>
public sealed record CollectionIntoAction(
    ActionKind Kind,
    ParsedExpression Target,
    ParsedExpression? IntoTarget,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Field-only action: verb field
/// Shape: FieldOnly (4)
/// Actions: clear
/// </summary>
public sealed record FieldOnlyAction(
    ActionKind Kind,
    ParsedExpression Target,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Collection value with ordering key action: verb field expr by expr
/// Shape: CollectionValueBy (5)
/// Actions: append (for log-by), enqueue (for queue-by)
/// </summary>
public sealed record CollectionValueByAction(
    ActionKind Kind,
    ParsedExpression Target,
    ParsedExpression Value,
    ParsedExpression OrderingKey,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Insert at index action: verb field expr at expr
/// Shape: InsertAt (6)
/// Actions: insert
/// </summary>
public sealed record InsertAtAction(
    ActionKind Kind,
    ParsedExpression Target,
    ParsedExpression Value,
    ParsedExpression Index,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Remove at index action: verb field at expr
/// Shape: RemoveAtIndex (7)
/// Actions: remove (at index variant)
/// </summary>
public sealed record RemoveAtAction(
    ActionKind Kind,
    ParsedExpression Target,
    ParsedExpression Index,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Put key-value action: verb field key = value
/// Shape: PutKeyValue (8)
/// Actions: put
/// </summary>
public sealed record PutKeyValueAction(
    ActionKind Kind,
    ParsedExpression Target,
    ParsedExpression Key,
    ParsedExpression Value,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Collection into with optional ordering capture: verb field [into field] [by key]
/// Shape: CollectionIntoBy (9)
/// Actions: dequeue (for queue-by)
/// </summary>
public sealed record CollectionIntoByAction(
    ActionKind Kind,
    ParsedExpression Target,
    ParsedExpression? IntoTarget,
    ParsedExpression? OrderingCapture,
    SourceSpan Span)
    : ParsedAction(Kind, Span);

/// <summary>
/// Malformed action sentinel — used when action parsing fails.
/// </summary>
public sealed record MalformedAction(ActionKind Kind, SourceSpan Span)
    : ParsedAction(Kind, Span);
