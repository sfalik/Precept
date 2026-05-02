using System.Collections.Frozen;

namespace Precept.Language;

/// <summary>
/// Catalog of state-machine action verbs. Source of truth for the type checker
/// (target-type compatibility), parser, LS completions/hover, and MCP vocabulary.
/// </summary>
public static class Actions
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Shared applicability arrays
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly TypeTarget[] AnyType = []; // empty = caller validates

    private static readonly FixedReturnAccessor CollectionCount =
        new FixedReturnAccessor("count", TypeKind.Integer, "Number of elements");

    private static readonly TypeTarget[] SetOnly = [new(TypeKind.Set)];
    private static readonly TypeTarget[] QueueOnly = [new(TypeKind.Queue)];
    private static readonly TypeTarget[] StackOnly = [new(TypeKind.Stack)];
    private static readonly TypeTarget[] BagOnly = [new(TypeKind.Bag)];
    private static readonly TypeTarget[] LogOnly = [new(TypeKind.Log)];
    private static readonly TypeTarget[] LogByOnly = [new(TypeKind.LogBy)];
    private static readonly TypeTarget[] ListOnly = [new(TypeKind.List)];
    private static readonly TypeTarget[] LookupOnly = [new(TypeKind.Lookup)];
    private static readonly TypeTarget[] QueueByOnly = [new(TypeKind.QueueBy)];

    private static readonly TypeTarget[] CollectionsAndOptional =
    [
        new(TypeKind.Set),
        new(TypeKind.Queue),
        new(TypeKind.Stack),
        new ModifiedTypeTarget(null, [ModifierKind.Optional]),
    ];

    // ════════════════════════════════════════════════════════════════════════════
    //  Shared AllowedIn arrays
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly ConstructKind[] EventBodyOnly = [ConstructKind.EventDeclaration];

    private static readonly ConstructKind[] AllActionContexts =
    [
        ConstructKind.EventDeclaration,
        ConstructKind.StateAction,
        ConstructKind.TransitionRow,
    ];

    private static readonly TypeTarget[] ClearApplicable =
    [
        new(TypeKind.Set),
        new(TypeKind.Queue),
        new(TypeKind.Stack),
        new(TypeKind.Bag),
        new(TypeKind.List),
        new(TypeKind.QueueBy),
        new ModifiedTypeTarget(null, [ModifierKind.Optional]),
    ];

    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static ActionMeta GetMeta(ActionKind kind) => kind switch
    {
        ActionKind.Set => new(
            kind, Tokens.GetMeta(TokenKind.Set),
            "Assign a value to a scalar field",
            AnyType, ActionSyntaxShape.AssignValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Assigns a value to a field. Works on any scalar, temporal, or business-domain field."),

        ActionKind.Add => new(
            kind, Tokens.GetMeta(TokenKind.Add),
            "Add an element to a set",
            [new(TypeKind.Set), new(TypeKind.Bag)], ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Adds an element to a set or bag field. Has no effect if the element is already present (for sets)."),

        ActionKind.Remove => new(
            kind, Tokens.GetMeta(TokenKind.Remove),
            "Remove an element from a set",
            [new(TypeKind.Set), new(TypeKind.Bag), new(TypeKind.List), new(TypeKind.Lookup)], ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Removes an element from a set, bag, list, or lookup field. Has no effect if the element is not present."),

        ActionKind.Enqueue => new(
            kind, Tokens.GetMeta(TokenKind.Enqueue),
            "Enqueue an element onto a queue",
            QueueOnly, ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Appends an element to the back of a queue field."),

        ActionKind.Dequeue => new(
            kind, Tokens.GetMeta(TokenKind.Dequeue),
            "Dequeue the front element of a queue",
            [new(TypeKind.Queue), new(TypeKind.QueueBy)], ActionSyntaxShape.CollectionInto, IntoSupported: true,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCount), OperatorKind.GreaterThan, 0m,
                    "Queue must be non-empty"),
            ],
            AllowedIn: AllActionContexts,
            HoverDescription: "Removes the front element from a queue. Optionally captures it with 'into'. Requires a non-empty guard."),

        ActionKind.Push => new(
            kind, Tokens.GetMeta(TokenKind.Push),
            "Push an element onto a stack",
            StackOnly, ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Pushes an element onto the top of a stack field."),

        ActionKind.Pop => new(
            kind, Tokens.GetMeta(TokenKind.Pop),
            "Pop the top element of a stack",
            StackOnly, ActionSyntaxShape.CollectionInto, IntoSupported: true,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCount), OperatorKind.GreaterThan, 0m,
                    "Stack must be non-empty"),
            ],
            AllowedIn: AllActionContexts,
            HoverDescription: "Removes the top element from a stack. Optionally captures it with 'into'. Requires a non-empty guard."),

        ActionKind.Clear => new(
            kind, Tokens.GetMeta(TokenKind.Clear),
            "Clear all elements from a collection or reset an optional field",
            ClearApplicable, ActionSyntaxShape.FieldOnly, AllowedIn: AllActionContexts,
            HoverDescription: "Removes all elements from a collection, or resets an optional field to null."),

        ActionKind.Append => new(
            kind, Tokens.GetMeta(TokenKind.Append),
            "Append an element to a log or list",
            [new(TypeKind.Log), new(TypeKind.List)],
            ActionSyntaxShape.CollectionValue, ValueRequired: true,
            AllowedIn: AllActionContexts,
            HoverDescription: "Appends an element to the end of a log or list field."),

        ActionKind.AppendBy => new(
            kind, Tokens.GetMeta(TokenKind.Append),
            "Append an element with an ordering key to a log-by",
            [new(TypeKind.LogBy)],
            ActionSyntaxShape.CollectionValueBy, ValueRequired: true,
            AllowedIn: AllActionContexts,
            PrimaryActionKind: ActionKind.Append,
            HoverDescription: "Appends an element with an explicit ordering key to a log-by field. Requires 'when not (F contains P)' guard."),

        ActionKind.Insert => new(
            kind, Tokens.GetMeta(TokenKind.Insert),
            "Insert an element at a specific index in a list",
            [new(TypeKind.List)],
            ActionSyntaxShape.InsertAt, ValueRequired: true,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCount), OperatorKind.GreaterThanOrEqual, 0m,
                    "Index must be within bounds (0 to count)"),
            ],
            AllowedIn: AllActionContexts,
            HoverDescription: "Inserts an element at a zero-based index in a list field. Requires an index-bounds guard."),

        ActionKind.RemoveAt => new(
            kind, Tokens.GetMeta(TokenKind.Remove),
            "Remove the element at a specific index from a list",
            [new(TypeKind.List)],
            ActionSyntaxShape.RemoveAtIndex,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCount), OperatorKind.GreaterThan, 0m,
                    "Index must be within bounds"),
            ],
            AllowedIn: AllActionContexts,
            PrimaryActionKind: ActionKind.Remove,
            HoverDescription: "Removes the element at a zero-based index from a list field. Requires an index-bounds guard."),

        ActionKind.Put => new(
            kind, Tokens.GetMeta(TokenKind.Put),
            "Upsert a key-value pair into a lookup",
            [new(TypeKind.Lookup)],
            ActionSyntaxShape.PutKeyValue, ValueRequired: true,
            AllowedIn: AllActionContexts,
            HoverDescription: "Inserts or updates a key-value pair in a lookup field."),

        ActionKind.EnqueueBy => new(
            kind, Tokens.GetMeta(TokenKind.Enqueue),
            "Enqueue an element with an ordering key to a queue-by",
            [new(TypeKind.QueueBy)],
            ActionSyntaxShape.CollectionValueBy, ValueRequired: true,
            AllowedIn: AllActionContexts,
            PrimaryActionKind: ActionKind.Enqueue,
            HoverDescription: "Enqueues an element with an explicit ordering key to a queue-by field."),

        ActionKind.DequeueBy => new(
            kind, Tokens.GetMeta(TokenKind.Dequeue),
            "Dequeue the front element of a queue-by",
            [new(TypeKind.QueueBy)],
            ActionSyntaxShape.CollectionIntoBy, IntoSupported: true,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(CollectionCount), OperatorKind.GreaterThan, 0m,
                    "Queue must be non-empty"),
            ],
            AllowedIn: AllActionContexts,
            PrimaryActionKind: ActionKind.Dequeue,
            HoverDescription: "Removes the front (best-ordered) element from a queue-by. Optionally captures element with 'into' and ordering value with 'by'. Requires a non-empty guard."),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ActionKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every ActionMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ActionMeta> All { get; } =
        Enum.GetValues<ActionKind>().Select(GetMeta).ToArray();

    /// <summary>
    /// O(1) lookup from token kind to action metadata.
    /// Mirrors <see cref="Constructs.ByLeadingToken"/>. Used by the parser to resolve
    /// the current token to an <see cref="ActionMeta"/> without a linear scan.
    /// </summary>
    public static FrozenDictionary<TokenKind, ActionMeta> ByTokenKind { get; } =
        All.Where(m => m.PrimaryActionKind == null)
           .ToFrozenDictionary(m => m.Token.Kind);
}
