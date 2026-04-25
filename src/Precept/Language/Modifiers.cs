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
            kind, TokenKind.Optional,
            "Field is nullable; use is set / is not set for presence",
            ModifierCategory.Structural, AnyType),

        ModifierKind.Ordered => new FieldModifierMeta(
            kind, TokenKind.Ordered,
            "Choice field supports ordinal comparison",
            ModifierCategory.Structural, ChoiceOnly),

        ModifierKind.Nonnegative => new FieldModifierMeta(
            kind, TokenKind.Nonnegative,
            "Value ≥ 0",
            ModifierCategory.Structural, NumericTypes),

        ModifierKind.Positive => new FieldModifierMeta(
            kind, TokenKind.Positive,
            "Value > 0",
            ModifierCategory.Structural, NumericTypes,
            Subsumes: [ModifierKind.Nonnegative, ModifierKind.Nonzero]),

        ModifierKind.Nonzero => new FieldModifierMeta(
            kind, TokenKind.Nonzero,
            "Value ≠ 0",
            ModifierCategory.Structural, NumericTypes),

        ModifierKind.Notempty => new FieldModifierMeta(
            kind, TokenKind.Notempty,
            "String is non-empty",
            ModifierCategory.Structural, StringOnly),

        ModifierKind.Default => new FieldModifierMeta(
            kind, TokenKind.Default,
            "Default value expression",
            ModifierCategory.Structural, AnyType, HasValue: true),

        ModifierKind.Min => new FieldModifierMeta(
            kind, TokenKind.Min,
            "Minimum value",
            ModifierCategory.Structural, NumericTypes, HasValue: true),

        ModifierKind.Max => new FieldModifierMeta(
            kind, TokenKind.Max,
            "Maximum value",
            ModifierCategory.Structural, NumericTypes, HasValue: true),

        ModifierKind.Minlength => new FieldModifierMeta(
            kind, TokenKind.Minlength,
            "Minimum string length",
            ModifierCategory.Structural, StringOnly, HasValue: true),

        ModifierKind.Maxlength => new FieldModifierMeta(
            kind, TokenKind.Maxlength,
            "Maximum string length",
            ModifierCategory.Structural, StringOnly, HasValue: true),

        ModifierKind.Mincount => new FieldModifierMeta(
            kind, TokenKind.Mincount,
            "Minimum collection count",
            ModifierCategory.Structural, CollectionTypes, HasValue: true),

        ModifierKind.Maxcount => new FieldModifierMeta(
            kind, TokenKind.Maxcount,
            "Maximum collection count",
            ModifierCategory.Structural, CollectionTypes, HasValue: true),

        ModifierKind.Maxplaces => new FieldModifierMeta(
            kind, TokenKind.Maxplaces,
            "Maximum decimal places",
            ModifierCategory.Structural, DecimalOnly, HasValue: true),

        // ── State modifiers ─────────────────────────────────────────────────────
        ModifierKind.InitialState => new StateModifierMeta(
            kind, TokenKind.Initial,
            "The precept starts in this state",
            ModifierCategory.Structural),

        ModifierKind.Terminal => new StateModifierMeta(
            kind, TokenKind.Terminal,
            "No outgoing transitions",
            ModifierCategory.Structural, AllowsOutgoing: false),

        ModifierKind.Required => new StateModifierMeta(
            kind, TokenKind.Required,
            "All initial→terminal paths visit this state (dominator)",
            ModifierCategory.Structural, RequiresDominator: true),

        ModifierKind.Irreversible => new StateModifierMeta(
            kind, TokenKind.Irreversible,
            "No path from this state back to any ancestor",
            ModifierCategory.Structural, PreventsBackEdge: true),

        ModifierKind.Success => new StateModifierMeta(
            kind, TokenKind.Success,
            "Success outcome state",
            ModifierCategory.Semantic),

        ModifierKind.Warning => new StateModifierMeta(
            kind, TokenKind.Warning,
            "Warning outcome state",
            ModifierCategory.Semantic),

        ModifierKind.Error => new StateModifierMeta(
            kind, TokenKind.Error,
            "Error outcome state",
            ModifierCategory.Semantic),

        // ── Event modifiers ─────────────────────────────────────────────────────
        ModifierKind.InitialEvent => new EventModifierMeta(
            kind, TokenKind.Initial,
            "Auto-fire entry point event",
            ModifierCategory.Structural, GraphAnalysisKind.None),

        // ── Access modifiers ────────────────────────────────────────────────────
        ModifierKind.Write => new AccessModifierMeta(
            kind, TokenKind.Write,
            "Field is present and writable",
            ModifierCategory.Structural, IsPresent: true, IsWritable: true),

        ModifierKind.Read => new AccessModifierMeta(
            kind, TokenKind.Read,
            "Field is present and read-only",
            ModifierCategory.Structural, IsPresent: true, IsWritable: false),

        ModifierKind.Omit => new AccessModifierMeta(
            kind, TokenKind.Omit,
            "Field is structurally absent",
            ModifierCategory.Structural, IsPresent: false, IsWritable: false),

        // ── Anchor modifiers ────────────────────────────────────────────────────
        ModifierKind.In => new AnchorModifierMeta(
            kind, TokenKind.In,
            "In-state scope anchor",
            ModifierCategory.Structural, AnchorScope.InState),

        ModifierKind.To => new AnchorModifierMeta(
            kind, TokenKind.To,
            "On-entry scope anchor",
            ModifierCategory.Structural, AnchorScope.OnEntry),

        ModifierKind.From => new AnchorModifierMeta(
            kind, TokenKind.From,
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
