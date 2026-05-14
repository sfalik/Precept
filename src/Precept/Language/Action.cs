using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Pipeline;

namespace Precept.Language;

/// <summary>
/// Metadata for a state-machine action verb.
/// <c>Token</c> is a <see cref="TokenMeta"/> object reference from the Tokens catalog.
/// </summary>
public sealed record ActionMeta(
    ActionKind   Kind,
    TokenMeta    Token,
    string       Description,
    TypeTarget[] ApplicableTo,
    ActionSyntaxShape SyntaxShape,
    bool         ValueRequired = false,
    ProofRequirement[]? ProofRequirements = null,
    ConstructKind[]?    AllowedIn         = null,
    string?      HoverDescription = null,
    string?      UsageExample     = null,
    string?      SnippetTemplate  = null,
    ActionKind?  PrimaryActionKind = null,
    Func<TypedAction, SemanticIndex, ImmutableArray<ProofObligation>>? DynamicObligationGenerator = null)
{
    /// <summary>Proof obligations the type checker must verify at call sites.</summary>
    public ProofRequirement[] ProofRequirements { get; } = ProofRequirements ?? [];

    /// <summary>Construct kinds where this action may appear.</summary>
    public ConstructKind[] AllowedIn { get; } = AllowedIn ?? [];
    
    /// <summary>
    /// Optional delegate to generate dynamic proof obligations based on runtime context.
    /// Used for obligations that depend on field properties (e.g., interval containment depends on field bounds).
    /// Returns an empty array if no dynamic obligations apply.
    /// </summary>
    public Func<TypedAction, SemanticIndex, ImmutableArray<ProofObligation>>? DynamicObligationGenerator { get; } = DynamicObligationGenerator;
}

/// <summary>
/// Logical role of a slot in an action's argument syntax.
/// </summary>
public enum ActionSlotRole
{
    /// <summary>The collection or field being operated on (always first, always present).</summary>
    Target          = 1,
    /// <summary>The value being inserted, added, or assigned.</summary>
    Value           = 2,
    /// <summary>The key in a key-value action (PutKeyValue).</summary>
    Key             = 3,
    /// <summary>The index position (InsertAt, RemoveAtIndex).</summary>
    Index           = 4,
    /// <summary>The destination field in dequeue-into actions (optional).</summary>
    IntoTarget      = 5,
    /// <summary>The priority or ordering key in CollectionValueBy (by expr).</summary>
    OrderingKey     = 6,
    /// <summary>The capture variable in CollectionIntoBy (by expr, optional).</summary>
    OrderingCapture = 7,
}

/// <summary>
/// Describes one positional or keyword-separated argument slot in an action's syntax.
/// </summary>
/// <param name="Role">Logical role of the slot.</param>
/// <param name="PrecedingSeparator">The keyword token that precedes this slot, or <c>null</c> if the slot is positional (no preceding keyword).</param>
/// <param name="IsOptional"><c>true</c> for slots that may be omitted (e.g. the <c>into</c> capture in dequeue).</param>
public sealed record ActionSyntaxSlot(
    ActionSlotRole Role,
    TokenKind? PrecedingSeparator,
    bool IsOptional);

/// <summary>
/// Shape metadata for one <see cref="ActionSyntaxShape"/> value: the ordered list of argument slots
/// and a pre-computed set of the separator tokens those slots introduce.
/// </summary>
public sealed record ActionShapeMeta(
    ActionSyntaxShape Shape,
    ActionSyntaxSlot[] Slots)
{
    /// <summary>
    /// Pre-computed frozen set of every distinct <see cref="TokenKind"/> that appears as a
    /// <see cref="ActionSyntaxSlot.PrecedingSeparator"/> in <see cref="Slots"/>.
    /// Computed once at construction — never recomputed per call.
    /// </summary>
    public System.Collections.Frozen.FrozenSet<TokenKind> SeparatorTokens { get; } =
        Slots
            .Where(s => s.PrecedingSeparator.HasValue)
            .Select(s => s.PrecedingSeparator!.Value)
            .Distinct()
            .ToHashSet()
            .ToFrozenSet();
}

/// <summary>The token consumption pattern for this action's argument syntax.</summary>
public enum ActionSyntaxShape
{
    /// <summary>verb field = expression  (set)</summary>
    AssignValue     = 1,
    /// <summary>verb field expression    (add, remove, enqueue, push)</summary>
    CollectionValue = 2,
    /// <summary>verb field [into field]  (dequeue, pop)</summary>
    CollectionInto  = 3,
    /// <summary>verb field               (clear)</summary>
    FieldOnly       = 4,
    /// <summary>verb field expr by expr  (append-by, enqueue-by)</summary>
    CollectionValueBy   = 5,
    /// <summary>verb field expr at expr  (insert at index)</summary>
    InsertAt            = 6,
    /// <summary>verb field at expr       (remove at index: positional, no element)</summary>
    RemoveAtIndex       = 7,
    /// <summary>verb field key = value   (put: lookup upsert)</summary>
    PutKeyValue         = 8,
    /// <summary>verb field [into field] [by key]  (dequeue-by: optional into + optional routing)</summary>
    CollectionIntoBy    = 9,
}
