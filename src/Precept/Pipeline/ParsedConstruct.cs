using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Option F output shape: a single parsed construct, identified by its catalog metadata
/// and carrying an ordered array of typed slot values. Generic — no per-construct subtypes.
/// The parser produces ImmutableArray&lt;ParsedConstruct&gt;; all downstream consumers
/// (type checker, graph analyzer, proof engine) read this type.
/// </summary>
public sealed record ParsedConstruct(
    ConstructMeta             Meta,
    ImmutableArray<SlotValue> Slots,
    SourceSpan                Span,
    TokenKind?                LeadingTokenKind = null
)
{
    /// <summary>
    /// Returns the first slot matching the given kind, or null if not found.
    /// </summary>
    public SlotValue? GetSlot(ConstructSlotKind kind)
        => Slots.FirstOrDefault(s => s.Kind == kind);

    /// <summary>
    /// Returns the first slot matching the given kind as the specified type, or null if not found or wrong type.
    /// </summary>
    public T? GetSlot<T>(ConstructSlotKind kind) where T : SlotValue
        => GetSlot(kind) as T;

    /// <summary>
    /// Returns the first slot matching the given kind as the specified type, or throws if missing.
    /// </summary>
    public T GetRequiredSlot<T>(ConstructSlotKind kind) where T : SlotValue
        => GetSlot<T>(kind) ?? throw new InvalidOperationException($"Required slot {kind} missing on {Meta.Kind}");

    /// <summary>
    /// Returns true if a slot of the given kind exists and is of the specified type.
    /// </summary>
    public bool HasSlot<T>(ConstructSlotKind kind) where T : SlotValue
        => GetSlot(kind) is T;

    /// <summary>
    /// Returns all slots of the specified type.
    /// </summary>
    public IEnumerable<T> GetSlots<T>() where T : SlotValue
        => Slots.OfType<T>();

    /// <summary>
    /// Returns true if all required slots are filled without sentinel values.
    /// A construct is incomplete if any required slot contains a missing/malformed sentinel.
    /// </summary>
    public bool IsComplete
    {
        get
        {
            // Check each required slot from the metadata
            foreach (var metaSlot in Meta.Slots)
            {
                if (!metaSlot.IsRequired) continue;

                // Find the corresponding value by kind
                var value = GetSlot(metaSlot.Kind);
                if (value is null) return false;
                if (IsSentinel(value)) return false;
            }
            return true;
        }
    }

    private static bool IsSentinel(SlotValue value) => value switch
    {
        TypeExpressionSlot { TypeRef: MissingTypeReference } => true,
        GuardClauseSlot { Expression: MissingExpression } => true,
        ComputeExpressionSlot { Expression: MissingExpression } => true,
        EnsureClauseSlot { Expression: MissingExpression } => true,
        RuleExpressionSlot { Expression: MissingExpression } => true,
        OutcomeSlot { Outcome: MalformedOutcome } => true,
        _ => false,
    };
}
