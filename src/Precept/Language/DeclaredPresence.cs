namespace Precept.Language;

public abstract record DeclaredPresenceMeta(
    string Description,
    ProofSatisfaction[]? ProofSatisfactions = null)
{
    public ProofSatisfaction[] ProofSatisfactions { get; } = ProofSatisfactions ?? [];

    public sealed record Guaranteed()
        : DeclaredPresenceMeta(
            "Value is structurally present on every instance",
            [new ProofSatisfaction.Presence()]);

    public sealed record Optional()
        : DeclaredPresenceMeta(
            "Value may be absent");
}
