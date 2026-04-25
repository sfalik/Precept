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
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static ActionMeta GetMeta(ActionKind kind) => kind switch
    {
        ActionKind.Set => new(
            kind, TokenKind.Set,
            "Assign a value to a scalar field",
            AnyType, ValueRequired: true),

        ActionKind.Add => new(
            kind, TokenKind.Add,
            "Add an element to a set",
            SetOnly, ValueRequired: true),

        ActionKind.Remove => new(
            kind, TokenKind.Remove,
            "Remove an element from a set",
            SetOnly, ValueRequired: true),

        ActionKind.Enqueue => new(
            kind, TokenKind.Enqueue,
            "Enqueue an element onto a queue",
            QueueOnly, ValueRequired: true),

        ActionKind.Dequeue => new(
            kind, TokenKind.Dequeue,
            "Dequeue the front element of a queue",
            QueueOnly, IntoSupported: true),

        ActionKind.Push => new(
            kind, TokenKind.Push,
            "Push an element onto a stack",
            StackOnly, ValueRequired: true),

        ActionKind.Pop => new(
            kind, TokenKind.Pop,
            "Pop the top element of a stack",
            StackOnly, IntoSupported: true),

        ActionKind.Clear => new(
            kind, TokenKind.Clear,
            "Clear all elements from a collection or reset an optional field",
            CollectionsAndOptional),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ActionKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every ActionMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ActionMeta> All { get; } =
        Enum.GetValues<ActionKind>().Select(GetMeta).ToArray();
}
