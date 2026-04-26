namespace Precept.Language;

// ════════════════════════════════════════════════════════════════════════════════
//  Supporting enums
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Three modifier categories from the language vision (issues #58 and #86).
/// </summary>
public enum ModifierCategory
{
    /// <summary>Compile-time-provable properties — lifecycle shape, terminality, one-write. Requires graph analysis.</summary>
    Structural,
    /// <summary>Intent and tooling meaning — success, error, sensitive, audit, deprecated. No graph analysis.</summary>
    Semantic,
    /// <summary>Language-level control over how declarations surface as warnings vs hard invariants.</summary>
    Severity,
}

/// <summary>
/// Maps each event modifier to the graph reasoning the compiler must perform.
/// </summary>
public enum GraphAnalysisKind
{
    None,
    IncomingEdge,
    OutcomeType,
    Reachability,
    /// <summary>
    /// Initial event compatibility: every transition triggered by the initial event
    /// must target the state marked <c>initial</c>.
    /// </summary>
    InitialEventCompatibility,
}

/// <summary>Scope of an anchor modifier (in, to, from).</summary>
public enum AnchorScope
{
    /// <summary>While in this state.</summary>
    InState,
    /// <summary>On entry to a state.</summary>
    OnEntry,
    /// <summary>On exit from a state.</summary>
    OnExit,
}

/// <summary>Disambiguates anchor usage between ensure and state-action contexts.</summary>
public enum AnchorTarget
{
    Ensure,
    StateAction,
}

// ════════════════════════════════════════════════════════════════════════════════
//  TypeTarget — shared supporting type for applicability declarations
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Declares type applicability for a modifier or action. <c>TypeTarget(Kind)</c>
/// matches fields of the given type. See <see cref="ModifiedTypeTarget"/> for
/// modifier-conditioned matching.
/// </summary>
public record TypeTarget(TypeKind Kind);

/// <summary>
/// Matches a field that has the given type AND all listed modifiers.
/// <c>TypeKind = null</c> means "any type." Used in applicability arrays with OR semantics.
/// </summary>
public sealed record ModifiedTypeTarget : TypeTarget
{
    public TypeKind? TypeKindOrNull { get; }
    public ModifierKind[] RequiredModifiers { get; }

    public ModifiedTypeTarget(TypeKind? typeKind, ModifierKind[] requiredModifiers)
        : base(typeKind ?? TypeKind.Error)
    {
        TypeKindOrNull = typeKind;
        RequiredModifiers = requiredModifiers;
    }
}

// ════════════════════════════════════════════════════════════════════════════════
//  ModifierMeta — discriminated union (5 sealed subtypes)
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Base metadata for a declaration-attached modifier. Discriminated union:
/// <see cref="FieldModifierMeta"/>, <see cref="StateModifierMeta"/>,
/// <see cref="EventModifierMeta"/>, <see cref="AccessModifierMeta"/>,
/// <see cref="AnchorModifierMeta"/>.
/// </summary>
public abstract record ModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category)
{
    /// <summary>
    /// Modifiers that are mutually exclusive with this one — at most one member
    /// of the group may appear on a declaration. Empty for most modifiers.
    /// Declared in the catalog; consumers (type checker, LS) must not hardcode groups.
    /// </summary>
    public ModifierKind[] MutuallyExclusiveWith { get; init; } = [];
}

/// <summary>Field constraint modifiers (14 members: optional, ordered, nonnegative, …, maxplaces).</summary>
public sealed record FieldModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    TypeTarget[] ApplicableTo,
    bool HasValue = false,
    ModifierKind[] Subsumes = default!,
    string? HoverDescription = null,
    string? UsageExample = null,
    string? SnippetTemplate = null)
    : ModifierMeta(Kind, Token, Description, Category)
{
    /// <summary>Modifiers this one makes redundant. Empty for most.</summary>
    public ModifierKind[] Subsumes { get; init; } = Subsumes ?? [];
}

/// <summary>State lifecycle modifiers (7 members: initial, terminal, required, irreversible, success, warning, error).</summary>
public sealed record StateModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    bool AllowsOutgoing = true,
    bool RequiresDominator = false,
    bool PreventsBackEdge = false)
    : ModifierMeta(Kind, Token, Description, Category);

/// <summary>Event modifiers (1 v2 member: initial event).</summary>
public sealed record EventModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    GraphAnalysisKind RequiredAnalysis = GraphAnalysisKind.None)
    : ModifierMeta(Kind, Token, Description, Category);

/// <summary>Access mode modifiers (3 members: write, read, omit).</summary>
public sealed record AccessModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    bool IsPresent = true,
    bool IsWritable = true)
    : ModifierMeta(Kind, Token, Description, Category);

/// <summary>Ensure/action anchor modifiers (3 members: in, to, from).</summary>
public sealed record AnchorModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    AnchorScope Scope,
    AnchorTarget Target = AnchorTarget.Ensure)
    : ModifierMeta(Kind, Token, Description, Category);
