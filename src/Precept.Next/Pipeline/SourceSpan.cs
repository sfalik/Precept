namespace Precept.Pipeline;

/// <summary>
/// A span in the source text. Offset is 0-based; Length is in characters.
/// Used on every AST node for diagnostics, hover, go-to-definition, and semantic tokens.
/// </summary>
public readonly record struct SourceSpan(int Offset, int Length)
{
    public static readonly SourceSpan Missing = new(0, 0);

    public int End => Offset + Length;

    public static SourceSpan Covering(SourceSpan first, SourceSpan last) =>
        new(first.Offset, last.End - first.Offset);
}
