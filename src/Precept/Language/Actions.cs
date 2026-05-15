using System.Collections.Frozen;
using System.Collections.Immutable;
using Precept.Pipeline;

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
            HoverDescription: "Assigns a value to a field. Works on any scalar, temporal, or business-domain field.",
            SnippetTemplate: "set ${1:Field} = ${2:value}",
            DynamicObligationGenerator: GenerateIntervalContainmentObligations),

        ActionKind.Add => new(
            kind, Tokens.GetMeta(TokenKind.Add),
            "Add an element to a set",
            [new(TypeKind.Set), new(TypeKind.Bag)], ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Adds an element to a set or bag field. Has no effect if the element is already present (for sets).",
            SnippetTemplate: "add ${1:Field} ${2:value}"),

        ActionKind.Remove => new(
            kind, Tokens.GetMeta(TokenKind.Remove),
            "Remove an element from a set",
            [new(TypeKind.Set), new(TypeKind.Bag), new(TypeKind.List), new(TypeKind.Lookup)], ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Removes an element from a set, bag, list, or lookup field. Has no effect if the element is not present.",
            SnippetTemplate: "remove ${1:Field} ${2:value}"),

        ActionKind.Enqueue => new(
            kind, Tokens.GetMeta(TokenKind.Enqueue),
            "Enqueue an element onto a queue",
            QueueOnly, ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Appends an element to the back of a queue field.",
            SnippetTemplate: "enqueue ${1:Field} ${2:value}"),

        ActionKind.Dequeue => new(
            kind, Tokens.GetMeta(TokenKind.Dequeue),
            "Dequeue the front element of a queue",
            [new(TypeKind.Queue), new(TypeKind.QueueBy)], ActionSyntaxShape.CollectionInto,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(Types.CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Queue must be non-empty"),
            ],
            AllowedIn: AllActionContexts,
            HoverDescription: "Removes the front element from a queue. Optionally captures it with 'into'. Requires a non-empty guard.",
            SnippetTemplate: "dequeue ${1:Field}"),

        ActionKind.Push => new(
            kind, Tokens.GetMeta(TokenKind.Push),
            "Push an element onto a stack",
            StackOnly, ActionSyntaxShape.CollectionValue, ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Pushes an element onto the top of a stack field.",
            SnippetTemplate: "push ${1:Field} ${2:value}"),

        ActionKind.Pop => new(
            kind, Tokens.GetMeta(TokenKind.Pop),
            "Pop the top element of a stack",
            StackOnly, ActionSyntaxShape.CollectionInto,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(Types.CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Stack must be non-empty"),
            ],
            AllowedIn: AllActionContexts,
            HoverDescription: "Removes the top element from a stack. Optionally captures it with 'into'. Requires a non-empty guard.",
            SnippetTemplate: "pop ${1:Field}"),

        ActionKind.Clear => new(
            kind, Tokens.GetMeta(TokenKind.Clear),
            "Clear all elements from a collection or reset an optional field",
            ClearApplicable, ActionSyntaxShape.FieldOnly, AllowedIn: AllActionContexts,
            HoverDescription: "Removes all elements from a collection, or resets an optional field to null.",
            SnippetTemplate: "clear ${1:Field}"),

        ActionKind.Append => new(
            kind, Tokens.GetMeta(TokenKind.Append),
            "Append an element to a log or list",
            [new(TypeKind.Log), new(TypeKind.List)],
            ActionSyntaxShape.CollectionValue, ValueRequired: true,
            AllowedIn: AllActionContexts,
            HoverDescription: "Appends an element to the end of a log or list field.",
            SnippetTemplate: "append ${1:Field} ${2:value}"),

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
                new NumericProofRequirement(new SelfSubject(Types.CollectionCountAccessor), OperatorKind.GreaterThanOrEqual, 0m,
                    "Index must be within bounds (0 to count)"),
            ],
            AllowedIn: AllActionContexts,
            HoverDescription: "Inserts an element at a zero-based index in a list field. Requires an index-bounds guard.",
            SnippetTemplate: "insert ${1:Field} ${2:value} at ${3:index}"),

        ActionKind.RemoveAt => new(
            kind, Tokens.GetMeta(TokenKind.Remove),
            "Remove the element at a specific index from a list",
            [new(TypeKind.List)],
            ActionSyntaxShape.RemoveAtIndex,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(Types.CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
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
            HoverDescription: "Inserts or updates a key-value pair in a lookup field.",
            SnippetTemplate: "put ${1:Field} ${2:key} = ${3:value}"),

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
            ActionSyntaxShape.CollectionIntoBy,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(Types.CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "Queue must be non-empty"),
            ],
            AllowedIn: AllActionContexts,
            PrimaryActionKind: ActionKind.Dequeue,
            HoverDescription: "Removes the front (best-ordered) element from a queue-by. Optionally captures element with 'into' and ordering value with 'by'. Requires a non-empty guard."),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ActionKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  Dynamic Obligation Generation Helpers
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates interval containment proof obligations for Set actions on constrained fields.
    /// Returns an empty array if the action's target has no catalog-declared interval bounds.
    /// Also generates length containment obligations for string literal assignments to bounded string fields.
    /// </summary>
    private static ImmutableArray<ProofObligation> GenerateIntervalContainmentObligations(
        TypedAction action,
        SemanticIndex semantics)
    {
        if (action is not TypedInputAction inputAction || inputAction.Kind != ActionKind.Set)
            return [];
        
        if (!semantics.FieldsByName.TryGetValue(inputAction.FieldName, out var targetField))
            return [];

        var obligations = ImmutableArray.CreateBuilder<ProofObligation>();

        // Numeric interval containment
        var (min, max) = ProofEngine.GetFieldBounds(targetField);
        if (min.HasValue || max.HasValue)
        {
            var authoredMin = targetField.DeclaredMin;
            var authoredMax = targetField.DeclaredMax;
            var intervalReq = new IntervalContainmentProofRequirement(
                new SelfSubject(),
                inputAction.FieldName,
                min, max,
                authoredMin, authoredMax,
                $"Interval containment: {inputAction.FieldName} must stay within declared bounds [{(authoredMin ?? min)?.ToString() ?? "−∞"} .. {(authoredMax ?? max)?.ToString() ?? "+∞"}]");

            obligations.Add(new ProofObligation(
                intervalReq,
                inputAction.InputExpression,
                null!, // Will be replaced with proper context in ProofEngine.WalkActions()
                ProofDisposition.Unresolved,
                null,
                null));
        }

        // String length containment (string fields with minlength/maxlength)
        // Only generate for literal string assignments — non-literal assignments leave the
        // runtime to enforce bounds. This avoids false positives for dynamically-provided values.
        if (targetField.ResolvedType == TypeKind.String
            && (targetField.DeclaredMinLength.HasValue || targetField.DeclaredMaxLength.HasValue)
            && inputAction.InputExpression is TypedLiteral { ResultType: TypeKind.String })
        {
            var lengthReq = new LengthContainmentProofRequirement(
                new SelfSubject(),
                inputAction.FieldName,
                targetField.DeclaredMinLength,
                targetField.DeclaredMaxLength,
                $"Length containment: {inputAction.FieldName} must have length in [{targetField.DeclaredMinLength?.ToString() ?? "0"} .. {targetField.DeclaredMaxLength?.ToString() ?? "∞"}]");

            obligations.Add(new ProofObligation(
                lengthReq,
                inputAction.InputExpression,
                null!, // Will be replaced with proper context in ProofEngine.WalkActions()
                ProofDisposition.Unresolved,
                null,
                null));
        }

        return obligations.ToImmutable();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  GetShapeMeta — exhaustive switch over ActionSyntaxShape
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the canonical <see cref="ActionShapeMeta"/> for the given <paramref name="shape"/>.
    /// Covers every <see cref="ActionSyntaxShape"/> member exhaustively.
    /// </summary>
    public static ActionShapeMeta GetShapeMeta(ActionSyntaxShape shape) => shape switch
    {
        // set Field = value
        ActionSyntaxShape.AssignValue => new(shape,
        [
            new(ActionSlotRole.Target,        null,                false),
            new(ActionSlotRole.Value,         TokenKind.Assign,    false),
        ]),

        // add/remove/enqueue/push/append Field value
        ActionSyntaxShape.CollectionValue => new(shape,
        [
            new(ActionSlotRole.Target,        null,                false),
            new(ActionSlotRole.Value,         null,                false),
        ]),

        // dequeue/pop Field [into intoTarget]
        ActionSyntaxShape.CollectionInto => new(shape,
        [
            new(ActionSlotRole.Target,        null,                false),
            new(ActionSlotRole.IntoTarget,    TokenKind.Into,      true),
        ]),

        // clear Field
        ActionSyntaxShape.FieldOnly => new(shape,
        [
            new(ActionSlotRole.Target,        null,                false),
        ]),

        // append-by/enqueue-by Field value by orderingKey
        ActionSyntaxShape.CollectionValueBy => new(shape,
        [
            new(ActionSlotRole.Target,        null,                false),
            new(ActionSlotRole.Value,         null,                false),
            new(ActionSlotRole.OrderingKey,   TokenKind.By,        false),
        ]),

        // insert Field value at index
        ActionSyntaxShape.InsertAt => new(shape,
        [
            new(ActionSlotRole.Target,        null,                false),
            new(ActionSlotRole.Value,         null,                false),
            new(ActionSlotRole.Index,         TokenKind.At,        false),
        ]),

        // remove-at Field at index
        ActionSyntaxShape.RemoveAtIndex => new(shape,
        [
            new(ActionSlotRole.Target,        null,                false),
            new(ActionSlotRole.Index,         TokenKind.At,        false),
        ]),

        // put Field key = value
        ActionSyntaxShape.PutKeyValue => new(shape,
        [
            new(ActionSlotRole.Target,        null,                false),
            new(ActionSlotRole.Key,           null,                false),
            new(ActionSlotRole.Value,         TokenKind.Assign,    false),
        ]),

        // dequeue-by Field [into intoTarget] [by orderingCapture]
        ActionSyntaxShape.CollectionIntoBy => new(shape,
        [
            new(ActionSlotRole.Target,         null,               false),
            new(ActionSlotRole.IntoTarget,     TokenKind.Into,     true),
            new(ActionSlotRole.OrderingCapture, TokenKind.By,      true),
        ]),

        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape,
            $"Unknown ActionSyntaxShape: {shape}"),
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

    public static FrozenDictionary<ActionKind, ActionMeta[]> SecondaryByPrimaryActionKind { get; } =
        All.Where(m => m.PrimaryActionKind is not null)
           .GroupBy(m => m.PrimaryActionKind!.Value)
           .ToFrozenDictionary(g => g.Key, g => g.ToArray());
}
