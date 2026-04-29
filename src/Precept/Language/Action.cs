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
    bool         IntoSupported = false,
    ProofRequirement[]? ProofRequirements = null,
    ConstructKind[]?    AllowedIn         = null,
    string?      HoverDescription = null,
    string?      UsageExample     = null,
    string?      SnippetTemplate  = null)
{
    /// <summary>Proof obligations the type checker must verify at call sites.</summary>
    public ProofRequirement[] ProofRequirements { get; } = ProofRequirements ?? [];

    /// <summary>Construct kinds where this action may appear.</summary>
    public ConstructKind[] AllowedIn { get; } = AllowedIn ?? [];
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
}
