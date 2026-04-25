namespace Precept.Language;

/// <summary>
/// Metadata for a state-machine action verb.
/// <c>ProofRequirements</c> and <c>AllowedIn</c> are deferred until
/// those types are implemented.
/// </summary>
public sealed record ActionMeta(
    ActionKind   Kind,
    TokenKind    Token,
    string       Description,
    TypeTarget[] ApplicableTo,
    bool         ValueRequired = false,
    bool         IntoSupported = false);
