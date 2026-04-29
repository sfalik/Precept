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
            SetOnly, ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Adds an element to a set field. Has no effect if the element is already present."),

        ActionKind.Remove => new(
            kind, Tokens.GetMeta(TokenKind.Remove),
            "Remove an element from a set",
            SetOnly, ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Removes an element from a set field. Has no effect if the element is not present."),

        ActionKind.Enqueue => new(
            kind, Tokens.GetMeta(TokenKind.Enqueue),
            "Enqueue an element onto a queue",
            QueueOnly, ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Appends an element to the back of a queue field."),

        ActionKind.Dequeue => new(
            kind, Tokens.GetMeta(TokenKind.Dequeue),
            "Dequeue the front element of a queue",
            QueueOnly, ActionSyntaxShape.CollectionInto, IntoSupported: true,
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
            CollectionsAndOptional, ActionSyntaxShape.FieldOnly, AllowedIn: AllActionContexts,
            HoverDescription: "Removes all elements from a collection, or resets an optional field to null."),

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
        All.ToFrozenDictionary(m => m.Token.Kind);
}
