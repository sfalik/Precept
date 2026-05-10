using System.Collections.Frozen;
using Precept.Pipeline;

namespace Precept.Language;

/// <summary>
/// Classification of outcome forms.
/// </summary>
public enum OutcomeKind
{
    Transition   = 1,   // -> transition StateName
    NoTransition = 2,   // -> no transition
    Reject       = 3,   // -> reject "reason"
}

/// <summary>
/// What argument shape the outcome expects after its leading token.
/// </summary>
public enum OutcomeArgumentKind
{
    /// <summary>No argument — the outcome is complete after the leading token(s).</summary>
    None = 0,

    /// <summary>Required identifier (state name) following the leading token.</summary>
    RequiredIdentifier = 1,

    /// <summary>Required string literal (reject reason) following the leading token.</summary>
    RequiredStringLiteral = 2,

    /// <summary>Compound form — secondary token required before argument (e.g., `no transition`).</summary>
    SecondaryToken = 3,
}

/// <summary>
/// Metadata record for a single outcome form in the Outcomes catalog.
/// Mirrors the pattern established by ActionMeta and ModifierMeta:
/// - Each member has a leading token for dispatch
/// - Each member has an argument shape (none, required identifier, required string literal)
/// - Each member maps to exactly one ParsedOutcome subtype
/// </summary>
/// <param name="Kind">The enum member this record describes.</param>
/// <param name="LeadingToken">The TokenKind that identifies this outcome form after the arrow.</param>
/// <param name="ArgumentKind">What the outcome expects after its leading token.</param>
/// <param name="ParsedSubtype">The ParsedOutcome subtype this form produces.</param>
/// <param name="Description">Human-readable description for tooling (hover, MCP).</param>
/// <param name="Example">Example syntax fragment for documentation.</param>
public sealed record OutcomeMeta(
    OutcomeKind         Kind,
    TokenKind           LeadingToken,
    OutcomeArgumentKind ArgumentKind,
    Type                ParsedSubtype,
    string              Description,
    string              Example,
    string              SerializedKind);

/// <summary>
/// Catalog of outcome forms — the three ways a transition row can conclude.
/// Source of truth for parser dispatch, LS completions, hover, and MCP vocabulary.
/// </summary>
public static class Outcomes
{
    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static OutcomeMeta GetMeta(OutcomeKind kind) => kind switch
    {
        OutcomeKind.Transition => new(
            kind,
            TokenKind.Transition,
            OutcomeArgumentKind.RequiredIdentifier,
            typeof(TransitionOutcome),
            "Transition to a named target state",
            "-> transition Approved",
            "transition"),

        OutcomeKind.NoTransition => new(
            kind,
            TokenKind.No,
            OutcomeArgumentKind.SecondaryToken,  // expects `transition` after `no`
            typeof(NoTransitionOutcome),
            "Explicitly remain in the current state with no transition",
            "-> no transition",
            "no transition"),

        OutcomeKind.Reject => new(
            kind,
            TokenKind.Reject,
            OutcomeArgumentKind.RequiredStringLiteral,
            typeof(RejectOutcome),
            "Reject the event with an explanation message",
            "-> reject \"Approval requires verified documents\"",
            "reject"),

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown OutcomeKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every OutcomeMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<OutcomeMeta> All { get; } =
        Enum.GetValues<OutcomeKind>().Select(GetMeta).ToArray();

    // ════════════════════════════════════════════════════════════════════════════
    //  Derived indexes
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// O(1) lookup from leading token to outcome metadata.
    /// Used by parser to dispatch after consuming the arrow.
    /// </summary>
    public static FrozenDictionary<TokenKind, OutcomeMeta> ByLeadingToken { get; } =
        All.ToFrozenDictionary(m => m.LeadingToken);

    /// <summary>
    /// The set of all tokens that can follow the outcome arrow.
    /// Used for vocabulary recognition and error recovery.
    /// </summary>
    public static FrozenSet<TokenKind> LeadingTokens { get; } =
        All.Select(m => m.LeadingToken).ToFrozenSet();

    /// <summary>
    /// Secondary token required for NoTransition form.
    /// This is a structural constant — no catalog derivation needed
    /// because only one form has a secondary token.
    /// </summary>
    public static TokenKind NoTransitionSecondaryToken => TokenKind.Transition;
}
