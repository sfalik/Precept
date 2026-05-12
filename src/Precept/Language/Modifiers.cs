using System.Collections.Frozen;

namespace Precept.Language;

/// <summary>
/// Catalog of all declaration-attached modifiers — field constraints, state lifecycle,
/// event modifiers, access modes, and ensure/action anchors. Source of truth for the
/// type checker, graph analyzer, LS completions/hover, and MCP vocabulary.
/// </summary>
public static class Modifiers
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Shared applicability arrays
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly TypeTarget[] ZeroBoundNumericTypes =
    [
        new(TypeKind.Integer), new(TypeKind.Decimal), new(TypeKind.Number),
        new(TypeKind.Money),   new(TypeKind.Quantity),
        new(TypeKind.Price),   new(TypeKind.ExchangeRate),
    ];

    private static readonly TypeTarget[] RangedNumericTypes =
    [
        new(TypeKind.Integer), new(TypeKind.Decimal), new(TypeKind.Number),
        new(TypeKind.Money),   new(TypeKind.Quantity),
        new(TypeKind.Price),
    ];

    private static readonly TypeTarget[] BusinessMagnitudeTypes =
    [
        new(TypeKind.Decimal), new(TypeKind.Money), new(TypeKind.Quantity),
        new(TypeKind.Price),   new(TypeKind.ExchangeRate),
    ];

    private static readonly TypeTarget[] StringOnly = [new(TypeKind.String)];
    private static readonly TypeTarget[] ChoiceOnly = [new(TypeKind.Choice)];

    private static readonly TypeTarget[] CollectionTypes =
    [
        new(TypeKind.Set), new(TypeKind.Queue), new(TypeKind.Stack),
        new(TypeKind.Log), new(TypeKind.LogBy), new(TypeKind.Bag),
        new(TypeKind.List), new(TypeKind.QueueBy), new(TypeKind.Lookup),
    ];

    private static readonly TypeTarget[] StringAndCollectionTypes =
    [
        new(TypeKind.String),
        new(TypeKind.Set), new(TypeKind.Queue), new(TypeKind.Stack),
        new(TypeKind.Log), new(TypeKind.LogBy), new(TypeKind.Bag),
        new(TypeKind.List), new(TypeKind.QueueBy),
    ];

    private static readonly TypeTarget[] AnyType = []; // empty = applies to all types

    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
    {
        // ── Value modifiers ─────────────────────────────────────────────────────
        ModifierKind.Optional => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Optional),
            "Field is nullable; use is set / is not set for presence",
            ModifierCategory.Structural, AnyType,
            HoverDescription: "The field may have no value. Use 'is set' and 'is not set' to test for presence. Absent values appear as null in the API.",
            MutuallyExclusiveWith: [ModifierKind.Notempty]),

        ModifierKind.Ordered => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Ordered),
            "Choice field supports ordinal comparison",
            ModifierCategory.Structural, ChoiceOnly,
            HoverDescription: "Enables comparison operators (< > <= >=) on a choice field, ordered by declaration sequence."),

        ModifierKind.Nonnegative => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Nonnegative),
            "Value ≥ 0",
            ModifierCategory.Structural, ZeroBoundNumericTypes,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.SelfValue(),
                    OperatorKind.GreaterThanOrEqual,
                    new NumericBoundSource.Constant(0m)),
            ],
            HoverDescription: "The field's value must be zero or greater. Enforced on every assignment.",
            DesugarsToRule: true,
            MutuallyExclusiveWith: [ModifierKind.Positive]),

        ModifierKind.Positive => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Positive),
            "Value > 0",
            ModifierCategory.Structural, ZeroBoundNumericTypes,
            Subsumes: [ModifierKind.Nonnegative, ModifierKind.Nonzero],
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.SelfValue(),
                    OperatorKind.GreaterThan,
                    new NumericBoundSource.Constant(0m)),
            ],
            HoverDescription: "The field's value must be strictly greater than zero. Implies nonnegative and nonzero.",
            DesugarsToRule: true,
            MutuallyExclusiveWith: [ModifierKind.Nonnegative]),

        ModifierKind.Nonzero => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Nonzero),
            "Value ≠ 0",
            ModifierCategory.Structural, ZeroBoundNumericTypes,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.SelfValue(),
                    OperatorKind.NotEquals,
                    new NumericBoundSource.Constant(0m)),
            ],
            HoverDescription: "The field's value must not be zero. Allows negative values.",
            DesugarsToRule: true),

        ModifierKind.Notempty => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Notempty),
            "String or collection is non-empty",
            ModifierCategory.Structural, StringAndCollectionTypes,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.Accessor("length"),
                    OperatorKind.GreaterThan,
                    new NumericBoundSource.Constant(0m)),
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.Accessor("count"),
                    OperatorKind.GreaterThan,
                    new NumericBoundSource.Constant(0m)),
            ],
            HoverDescription: "The field must not be empty. For text fields, the string must have at least one character. For collection fields, the collection must have at least one element. Not applicable to lookup fields — lookup entries are defined at design time.",
            DesugarsToRule: true,
            MutuallyExclusiveWith: [ModifierKind.Optional]),

        ModifierKind.Default => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Default),
            "Default value expression",
            ModifierCategory.Structural, AnyType, HasValue: true,
            HoverDescription: "Provides the initial value for the field when the precept is first created."),

        ModifierKind.Min => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Min),
            "Minimum value",
            ModifierCategory.Structural, RangedNumericTypes, HasValue: true,
            BoundCounterpart: ModifierKind.Max,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.SelfValue(),
                    OperatorKind.GreaterThanOrEqual,
                    new NumericBoundSource.DeclarationValue()),
            ],
            HoverDescription: "The field's value must be at least this minimum. Enforced on every assignment.",
            DesugarsToRule: true),

        ModifierKind.Max => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Max),
            "Maximum value",
            ModifierCategory.Structural, RangedNumericTypes, HasValue: true,
            BoundCounterpart: ModifierKind.Min,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.SelfValue(),
                    OperatorKind.LessThanOrEqual,
                    new NumericBoundSource.DeclarationValue()),
            ],
            HoverDescription: "The field's value must be at most this maximum. Enforced on every assignment.",
            DesugarsToRule: true),

        ModifierKind.Minlength => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Minlength),
            "Minimum string length",
            ModifierCategory.Structural, StringOnly, HasValue: true,
            BoundCounterpart: ModifierKind.Maxlength,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.Accessor("length"),
                    OperatorKind.GreaterThanOrEqual,
                    new NumericBoundSource.DeclarationValue()),
            ],
            HoverDescription: "The text field must have at least this many characters.",
            DesugarsToRule: true),

        ModifierKind.Maxlength => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Maxlength),
            "Maximum string length",
            ModifierCategory.Structural, StringOnly, HasValue: true,
            BoundCounterpart: ModifierKind.Minlength,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.Accessor("length"),
                    OperatorKind.LessThanOrEqual,
                    new NumericBoundSource.DeclarationValue()),
            ],
            HoverDescription: "The text field must have at most this many characters.",
            DesugarsToRule: true),

        ModifierKind.Mincount => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Mincount),
            "Minimum collection count",
            ModifierCategory.Structural, CollectionTypes, HasValue: true,
            BoundCounterpart: ModifierKind.Maxcount,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.Accessor("count"),
                    OperatorKind.GreaterThanOrEqual,
                    new NumericBoundSource.DeclarationValue()),
            ],
            HoverDescription: "The collection must have at least this many elements.",
            DesugarsToRule: true),

        ModifierKind.Maxcount => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Maxcount),
            "Maximum collection count",
            ModifierCategory.Structural, CollectionTypes, HasValue: true,
            BoundCounterpart: ModifierKind.Mincount,
            ProofSatisfactions:
            [
                new ProofSatisfaction.Numeric(
                    new SatisfactionProjection.Accessor("count"),
                    OperatorKind.LessThanOrEqual,
                    new NumericBoundSource.DeclarationValue()),
            ],
            HoverDescription: "The collection must have at most this many elements.",
            DesugarsToRule: true),

        ModifierKind.Maxplaces => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Maxplaces),
            "Maximum decimal places",
            ModifierCategory.Structural, BusinessMagnitudeTypes, HasValue: true,
            HoverDescription: "The decimal or business-domain magnitude field (money, quantity, price, exchangerate) must have at most this many digits after the decimal point. For currency amounts, this overrides the currency's default minor-unit precision.",
            DesugarsToRule: true),

        ModifierKind.Writable => new ValueModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Writable),
            "Field is directly editable; read-only by default without this modifier",
            ModifierCategory.Structural, AnyType, ApplicableDeclarationSites: ValueModifierDeclarationSite.FieldDeclaration,
            HoverDescription: "The field is directly editable. Without this modifier, the field is read-only by default. Use 'in State modify Field editable/readonly' to override per state."),

        // ── State modifiers ─────────────────────────────────────────────────────
        ModifierKind.InitialState => new StateModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Initial),
            "The precept starts in this state",
            ModifierCategory.Structural),

        ModifierKind.Terminal => new StateModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Terminal),
            "No outgoing transitions",
            ModifierCategory.Structural, AllowsOutgoing: false),

        ModifierKind.Required => new StateModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Required),
            "All initial→terminal paths visit this state (dominator)",
            ModifierCategory.Structural, RequiresDominator: true),

        ModifierKind.Irreversible => new StateModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Irreversible),
            "No path from this state back to any ancestor",
            ModifierCategory.Structural, PreventsBackEdge: true),

        ModifierKind.Success => new StateModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Success),
            "Success outcome state",
            ModifierCategory.Semantic,
            MutuallyExclusiveWith: [ModifierKind.Warning, ModifierKind.Error]),

        ModifierKind.Warning => new StateModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Warning),
            "Warning outcome state",
            ModifierCategory.Semantic,
            MutuallyExclusiveWith: [ModifierKind.Success, ModifierKind.Error]),

        ModifierKind.Error => new StateModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Error),
            "Error outcome state",
            ModifierCategory.Semantic,
            MutuallyExclusiveWith: [ModifierKind.Success, ModifierKind.Warning]),

        // ── Event modifiers ─────────────────────────────────────────────────────
        ModifierKind.InitialEvent => new EventModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Initial),
            "Auto-fire entry point event",
            ModifierCategory.Structural, GraphAnalysisKind.InitialEventCompatibility),

        // ── Access modifiers ────────────────────────────────────────────────────
        ModifierKind.Write => new AccessModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Editable),
            "Field is present and writable",
            ModifierCategory.Structural, IsPresent: true, IsWritable: true,
            MutuallyExclusiveWith: [ModifierKind.Read, ModifierKind.Omit]),

        ModifierKind.Read => new AccessModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Readonly),
            "Field is present and read-only",
            ModifierCategory.Structural, IsPresent: true, IsWritable: false,
            MutuallyExclusiveWith: [ModifierKind.Write, ModifierKind.Omit]),

        ModifierKind.Omit => new AccessModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Omit),
            "Field is structurally absent",
            ModifierCategory.Structural, IsPresent: false, IsWritable: false,
            MutuallyExclusiveWith: [ModifierKind.Write, ModifierKind.Read]),

        // ── Anchor modifiers ────────────────────────────────────────────────────
        ModifierKind.In => new AnchorModifierMeta(
            kind, Tokens.GetMeta(TokenKind.In),
            "In-state scope anchor",
            ModifierCategory.Structural, AnchorScope.InState),

        ModifierKind.To => new AnchorModifierMeta(
            kind, Tokens.GetMeta(TokenKind.To),
            "On-entry scope anchor",
            ModifierCategory.Structural, AnchorScope.OnEntry),

        ModifierKind.From => new AnchorModifierMeta(
            kind, Tokens.GetMeta(TokenKind.From),
            "On-exit scope anchor",
            ModifierCategory.Structural, AnchorScope.OnExit),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ModifierKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every ModifierMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ModifierMeta> All { get; } =
        Enum.GetValues<ModifierKind>().Select(GetMeta).ToArray();

    // ════════════════════════════════════════════════════════════════════════════
    //  ByValueToken — O(1) lookup: TokenKind → ValueModifierMeta
    //
    //  Mirrors Actions.ByTokenKind and Types.ByToken for parser-facing dispatch.
    //  Only ValueModifierMeta entries appear here — state, event, access, and
    //  anchor modifiers are never looked up by token kind in ParseFieldModifierNodes.
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// O(1) lookup from token kind to value modifier metadata.
    /// Used by <c>ParseFieldModifierNodes</c> to resolve a modifier token to its
    /// <see cref="ValueModifierMeta"/> without a linear scan. Mirrors
    /// <see cref="Language.Actions.ByTokenKind"/> for the modifier domain.
    /// </summary>
    public static FrozenDictionary<TokenKind, ValueModifierMeta> ByValueToken { get; } =
        All.OfType<ValueModifierMeta>()
           .ToFrozenDictionary(m => m.Token.Kind);

    /// <summary>
    /// O(1) lookup from token kind to state modifier metadata.
    /// Used by <c>ParseStateEntryList</c> to resolve a modifier token to its
    /// <see cref="StateModifierMeta"/> without a linear scan. Mirrors
    /// <see cref="ByValueToken"/> for the state modifier domain.
    /// </summary>
    public static FrozenDictionary<TokenKind, StateModifierMeta> ByStateToken { get; } =
        All.OfType<StateModifierMeta>()
           .ToFrozenDictionary(m => m.Token.Kind);

    /// <summary>
    /// O(1) lookup from token kind to access modifier metadata.
    /// Used by the type checker to resolve an access-mode token to its
    /// <see cref="AccessModifierMeta"/> without a hardcoded switch.
    /// Mirrors <see cref="ByValueToken"/> and <see cref="ByStateToken"/>.
    /// </summary>
    public static FrozenDictionary<TokenKind, AccessModifierMeta> ByAccessToken { get; } =
        All.OfType<AccessModifierMeta>()
           .ToFrozenDictionary(m => m.Token.Kind);

    /// <summary>
    /// O(1) lookup from token kind to anchor modifier metadata.
    /// Used by the type checker to resolve a leading anchor token to its
    /// <see cref="AnchorModifierMeta"/> (which carries <see cref="AnchorScope"/>)
    /// without a hardcoded switch.
    /// Mirrors <see cref="ByValueToken"/>, <see cref="ByStateToken"/>, and
    /// <see cref="ByAccessToken"/>.
    /// </summary>
    public static FrozenDictionary<TokenKind, AnchorModifierMeta> ByAnchorToken { get; } =
        All.OfType<AnchorModifierMeta>()
           .ToFrozenDictionary(m => m.Token.Kind);
}
