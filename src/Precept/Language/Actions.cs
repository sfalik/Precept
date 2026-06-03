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
        new(TypeKind.Lookup),
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
            AnyType, ActionSyntaxShape.AssignValue, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.ReplacesValue,
            ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Assigns a value to a field. Works on any scalar, temporal, or business-domain field.",
            SnippetTemplate: "set ${1:Field} = ${2:value}",
            DynamicObligationGenerator: GenerateIntervalContainmentObligations),

        ActionKind.Add => new(
            kind, Tokens.GetMeta(TokenKind.Add),
            "Add an element to a set",
            [new(TypeKind.Set), new(TypeKind.Bag)], ActionSyntaxShape.CollectionValue, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.Grows,
            ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Adds an element to a set or bag field. Has no effect if the element is already present (for sets).",
            SnippetTemplate: "add ${1:Field} ${2:value}"),

        ActionKind.Remove => new(
            kind, Tokens.GetMeta(TokenKind.Remove),
            "Remove an element from a set",
            [new(TypeKind.Set), new(TypeKind.Bag), new(TypeKind.List), new(TypeKind.Lookup)], ActionSyntaxShape.CollectionValue, ActionWriteSemantics.ClearsContents,
            Effect: ActionEffectClass.Shrinks,
            ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Removes an element from a set, bag, list, or lookup field. Has no effect if the element is not present.",
            SnippetTemplate: "remove ${1:Field} ${2:value}"),

        ActionKind.Enqueue => new(
            kind, Tokens.GetMeta(TokenKind.Enqueue),
            "Enqueue an element onto a queue",
            QueueOnly, ActionSyntaxShape.CollectionValue, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.Grows,
            ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Appends an element to the back of a queue field.",
            SnippetTemplate: "enqueue ${1:Field} ${2:value}"),

        ActionKind.Dequeue => new(
            kind, Tokens.GetMeta(TokenKind.Dequeue),
            "Dequeue the front element of a queue",
            [new(TypeKind.Queue), new(TypeKind.QueueBy)], ActionSyntaxShape.CollectionInto,
            ActionWriteSemantics.ClearsContents,
            Effect: ActionEffectClass.Shrinks,
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
            StackOnly, ActionSyntaxShape.CollectionValue, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.Grows,
            ValueRequired: true, AllowedIn: AllActionContexts,
            HoverDescription: "Pushes an element onto the top of a stack field.",
            SnippetTemplate: "push ${1:Field} ${2:value}"),

        ActionKind.Pop => new(
            kind, Tokens.GetMeta(TokenKind.Pop),
            "Pop the top element of a stack",
            StackOnly, ActionSyntaxShape.CollectionInto,
            ActionWriteSemantics.ClearsContents,
            Effect: ActionEffectClass.Shrinks,
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
            ClearApplicable, ActionSyntaxShape.FieldOnly, ActionWriteSemantics.ClearsContents,
            Effect: ActionEffectClass.Empties,
            AllowedIn: AllActionContexts,
            HoverDescription: "Removes all elements from a collection, or resets an optional field to null.",
            SnippetTemplate: "clear ${1:Field}"),

        ActionKind.Append => new(
            kind, Tokens.GetMeta(TokenKind.Append),
            "Append an element to a log or list",
            [new(TypeKind.Log), new(TypeKind.List)],
            ActionSyntaxShape.CollectionValue, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.Grows,
            ValueRequired: true,
            AllowedIn: AllActionContexts,
            HoverDescription: "Appends an element to the end of a log or list field.",
            SnippetTemplate: "append ${1:Field} ${2:value}"),

        ActionKind.AppendBy => new(
            kind, Tokens.GetMeta(TokenKind.Append),
            "Append an element with an ordering key to a log-by",
            [new(TypeKind.LogBy)],
            ActionSyntaxShape.CollectionValueBy, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.Grows,
            ValueRequired: true,
            ProofRequirements:
            [
                new KeyPresenceProofRequirement(new SelfSubject(), RequireAbsence: true,
                    "Ordering key must not already exist in the log-by (uniqueness)"),
            ],
            AllowedIn: AllActionContexts,
            PrimaryActionKind: ActionKind.Append,
            HoverDescription: "Appends an element with an explicit ordering key to a log-by field. Requires 'when not (F contains P)' guard."),

        ActionKind.Insert => new(
            kind, Tokens.GetMeta(TokenKind.Insert),
            "Insert an element at a specific index in a list",
            [new(TypeKind.List)],
            ActionSyntaxShape.InsertAt, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.Grows,
            ValueRequired: true,
            ProofRequirements:
            [
                new IndexBoundsProofRequirement(new ParamSubject(Types.PCollectionIndex),
                    IndexBoundsMode.AtOrBefore, Types.CollectionCountAccessor,
                    "Insert index must be within bounds [0, count]"),
            ],
            Parameters: [Types.PCollectionIndex],
            // InputSlotRole defaults to Value (the input expression is the inserted value);
            // the index lives on SecondaryExpression via ActionSecondaryRole.Index.
            InputSlotRole: ActionSlotRole.Value,
            AllowedIn: AllActionContexts,
            HoverDescription: "Inserts an element at a zero-based index in a list field. Requires an index-bounds guard.",
            SnippetTemplate: "insert ${1:Field} ${2:value} at ${3:index}"),

        ActionKind.RemoveAt => new(
            kind, Tokens.GetMeta(TokenKind.Remove),
            "Remove the element at a specific index from a list",
            [new(TypeKind.List)],
            ActionSyntaxShape.RemoveAtIndex, ActionWriteSemantics.ClearsContents,
            Effect: ActionEffectClass.Shrinks,
            ProofRequirements:
            [
                new NumericProofRequirement(new SelfSubject(Types.CollectionCountAccessor), OperatorKind.GreaterThan, 0m,
                    "List must be non-empty"),
                new IndexBoundsProofRequirement(new ParamSubject(Types.PCollectionIndex),
                    IndexBoundsMode.StrictlyBefore, Types.CollectionCountAccessor,
                    "Remove index must be within bounds [0, count)"),
            ],
            Parameters: [Types.PCollectionIndex],
            // The input expression IS the index — RemoveAtIndex shape has no value slot.
            InputSlotRole: ActionSlotRole.Index,
            AllowedIn: AllActionContexts,
            PrimaryActionKind: ActionKind.Remove,
            HoverDescription: "Removes the element at a zero-based index from a list field. Requires an index-bounds guard."),

        ActionKind.Put => new(
            kind, Tokens.GetMeta(TokenKind.Put),
            "Upsert a key-value pair into a lookup",
            [new(TypeKind.Lookup)],
            ActionSyntaxShape.PutKeyValue, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.Grows,
            ValueRequired: true,
            AllowedIn: AllActionContexts,
            HoverDescription: "Inserts or updates a key-value pair in a lookup field.",
            SnippetTemplate: "put ${1:Field} ${2:key} = ${3:value}"),

        ActionKind.EnqueueBy => new(
            kind, Tokens.GetMeta(TokenKind.Enqueue),
            "Enqueue an element with an ordering key to a queue-by",
            [new(TypeKind.QueueBy)],
            ActionSyntaxShape.CollectionValueBy, ActionWriteSemantics.EstablishesValue,
            Effect: ActionEffectClass.Grows,
            ValueRequired: true,
            AllowedIn: AllActionContexts,
            PrimaryActionKind: ActionKind.Enqueue,
            HoverDescription: "Enqueues an element with an explicit ordering key to a queue-by field."),

        ActionKind.DequeueBy => new(
            kind, Tokens.GetMeta(TokenKind.Dequeue),
            "Dequeue the front element of a queue-by",
            [new(TypeKind.QueueBy)],
            ActionSyntaxShape.CollectionIntoBy, ActionWriteSemantics.ClearsContents,
            Effect: ActionEffectClass.Shrinks,
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

        // String length containment (string fields with minlength/maxlength).
        // Generated unconditionally for ANY string RHS assigned into a length-bounded field (§0.7
        // prove-or-reject) — the proof engine discharges it against the RHS's static length interval
        // (literal, reference, concat, conditional, length-stable function) and emits when the
        // interval cannot be proven within bounds.
        if (targetField.ResolvedType == TypeKind.String
            && (targetField.DeclaredMinLength.HasValue || targetField.DeclaredMaxLength.HasValue)
            && inputAction.InputExpression.ResultType == TypeKind.String)
        {
            var lengthObligation = BuildLengthContainmentObligation(
                inputAction.FieldName,
                inputAction.InputExpression,
                targetField.DeclaredMinLength,
                targetField.DeclaredMaxLength);
            if (lengthObligation is not null)
                obligations.Add(lengthObligation);
        }

        // NOTE: open-field assignment-qualifier obligations are NOT generated here. They are stamped
        // by the type checker (which owns the authoritative qualifier resolver) onto the action's
        // ProofRequirements — see TypeChecker.Expressions.AssignmentQualifiers and Decision 3 in
        // docs/compiler/proof-engine.md (type checker stamps, proof engine discharges).

        return obligations.ToImmutable();
    }

    /// <summary>
    /// Builds a per-element string length-containment obligation for an element-introducing
    /// action whose target collection declares a string element length bound
    /// (<c>queue of string maxlength 200</c>). Shares <see cref="BuildLengthContainmentObligation"/>
    /// with the scalar <c>set</c> path — the only difference is the bound source (the receiver
    /// field's <c>ElementType.ValueBounds</c> instead of the field's own length modifiers). The
    /// governed-action set is derived from <see cref="ActionMeta"/> (a value-establishing growing
    /// action), so <c>enqueue</c>/<c>add</c>/<c>push</c>/<c>append</c>/<c>insert</c>/<c>put</c> and
    /// their by-keyed variants are covered without a hand-maintained list.
    /// </summary>
    internal static ProofObligation? GenerateElementLengthContainmentObligation(
        TypedInputAction inputAction,
        ActionMeta actionMeta,
        SemanticIndex semantics)
    {
        // Only value-introducing growing actions establish a new element value; shrink/clear/
        // replace actions do not. This is the catalog-declared effect, not an action-kind list.
        if (actionMeta.Effect != ActionEffectClass.Grows
            || actionMeta.WriteSemantics != ActionWriteSemantics.EstablishesValue)
            return null;

        if (inputAction.InputExpression.ResultType != TypeKind.String)
            return null;

        if (!semantics.FieldsByName.TryGetValue(inputAction.FieldName, out var targetField))
            return null;

        if (targetField.ElementType?.ValueBounds is not { IsEmpty: false } bounds)
            return null;

        if (!bounds.DeclaredMinLength.HasValue && !bounds.DeclaredMaxLength.HasValue)
            return null;

        return BuildLengthContainmentObligation(
            inputAction.FieldName,
            inputAction.InputExpression,
            bounds.DeclaredMinLength,
            bounds.DeclaredMaxLength);
    }

    /// <summary>
    /// Constructs a <see cref="LengthContainmentProofRequirement"/> obligation against the given
    /// declared length band. Shared by the scalar <c>set</c> path and the collection element
    /// write-site path; the band is the only parameter that differs (field bound vs element bound).
    /// The context is filled in by <see cref="ProofEngine"/> when the obligation is walked.
    /// </summary>
    private static ProofObligation? BuildLengthContainmentObligation(
        string fieldName,
        TypedExpression site,
        int? declaredMinLength,
        int? declaredMaxLength)
    {
        var lengthReq = new LengthContainmentProofRequirement(
            new SelfSubject(),
            fieldName,
            declaredMinLength,
            declaredMaxLength,
            $"Length containment: {fieldName} must have length in [{declaredMinLength?.ToString() ?? "0"} .. {declaredMaxLength?.ToString() ?? "∞"}]");

        return new ProofObligation(
            lengthReq,
            site,
            null!, // Replaced with the real context in ProofEngine.WalkActions().
            ProofDisposition.Unresolved,
            null,
            null);
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
