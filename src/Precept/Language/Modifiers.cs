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

    private static readonly TypeTarget[] NumericTypes =
    [
        new(TypeKind.Integer), new(TypeKind.Decimal), new(TypeKind.Number),
    ];

    private static readonly TypeTarget[] StringOnly = [new(TypeKind.String)];
    private static readonly TypeTarget[] DecimalOnly = [new(TypeKind.Decimal)];
    private static readonly TypeTarget[] ChoiceOnly = [new(TypeKind.Choice)];

    private static readonly TypeTarget[] CollectionTypes =
    [
        new(TypeKind.Set), new(TypeKind.Queue), new(TypeKind.Stack),
    ];

    private static readonly TypeTarget[] AnyType = []; // empty = applies to all types

    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
    {
        // ── Field modifiers ─────────────────────────────────────────────────────
        ModifierKind.Optional => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Optional),
            "Field is nullable; use is set / is not set for presence",
            ModifierCategory.Structural, AnyType,
            HoverDescription: "The field may have no value. Use 'is set' and 'is not set' to test for presence. Absent values appear as null in the API."),

        ModifierKind.Ordered => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Ordered),
            "Choice field supports ordinal comparison",
            ModifierCategory.Structural, ChoiceOnly,
            HoverDescription: "Enables comparison operators (< > <= >=) on a choice field, ordered by declaration sequence."),

        ModifierKind.Nonnegative => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Nonnegative),
            "Value ≥ 0",
            ModifierCategory.Structural, NumericTypes,
            HoverDescription: "The field's value must be zero or greater. Enforced on every assignment.",
            MutuallyExclusiveWith: [ModifierKind.Positive]),

        ModifierKind.Positive => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Positive),
            "Value > 0",
            ModifierCategory.Structural, NumericTypes,
            Subsumes: [ModifierKind.Nonnegative, ModifierKind.Nonzero],
            HoverDescription: "The field's value must be strictly greater than zero. Implies nonnegative and nonzero.",
            MutuallyExclusiveWith: [ModifierKind.Nonnegative]),

        ModifierKind.Nonzero => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Nonzero),
            "Value ≠ 0",
            ModifierCategory.Structural, NumericTypes,
            HoverDescription: "The field's value must not be zero. Allows negative values."),

        ModifierKind.Notempty => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Notempty),
            "String is non-empty",
            ModifierCategory.Structural, StringOnly,
            HoverDescription: "The text field must not be an empty string. A field with notempty always has visible content."),

        ModifierKind.Default => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Default),
            "Default value expression",
            ModifierCategory.Structural, AnyType, HasValue: true,
            HoverDescription: "Provides the initial value for the field when the precept is first created."),

        ModifierKind.Min => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Min),
            "Minimum value",
            ModifierCategory.Structural, NumericTypes, HasValue: true,
            HoverDescription: "The field's value must be at least this minimum. Enforced on every assignment."),

        ModifierKind.Max => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Max),
            "Maximum value",
            ModifierCategory.Structural, NumericTypes, HasValue: true,
            HoverDescription: "The field's value must be at most this maximum. Enforced on every assignment."),

        ModifierKind.Minlength => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Minlength),
            "Minimum string length",
            ModifierCategory.Structural, StringOnly, HasValue: true,
            HoverDescription: "The text field must have at least this many characters."),

        ModifierKind.Maxlength => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Maxlength),
            "Maximum string length",
            ModifierCategory.Structural, StringOnly, HasValue: true,
            HoverDescription: "The text field must have at most this many characters."),

        ModifierKind.Mincount => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Mincount),
            "Minimum collection count",
            ModifierCategory.Structural, CollectionTypes, HasValue: true,
            HoverDescription: "The collection must have at least this many elements."),

        ModifierKind.Maxcount => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Maxcount),
            "Maximum collection count",
            ModifierCategory.Structural, CollectionTypes, HasValue: true,
            HoverDescription: "The collection must have at most this many elements."),

        ModifierKind.Maxplaces => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Maxplaces),
            "Maximum decimal places",
            ModifierCategory.Structural, DecimalOnly, HasValue: true,
            HoverDescription: "The decimal field must have at most this many digits after the decimal point."),

        ModifierKind.Writable => new FieldModifierMeta(
            kind, Tokens.GetMeta(TokenKind.Writable),
            "Field is directly editable; read-only by default without this modifier",
            ModifierCategory.Structural, AnyType,
            HoverDescription: "The field is directly editable. Without this modifier, the field is read-only by default. Use 'in State write Field' to override per state."),

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
}
