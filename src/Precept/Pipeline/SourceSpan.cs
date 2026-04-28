namespace Precept.Pipeline;

/// <summary>
/// A span in the source text. Combines offset/length (for slicing) with
/// line/column (for diagnostics and LSP). Both coordinate systems are carried
/// on every AST node so downstream stages never need the raw source text to
/// emit located diagnostics.
/// </summary>
/// <param name="Offset">0-based character offset from the start of the source string.</param>
/// <param name="Length">Number of characters spanned.</param>
/// <param name="StartLine">1-based line number of the first character.</param>
/// <param name="StartColumn">1-based column number of the first character.</param>
/// <param name="EndLine">1-based line number of the last character (inclusive).</param>
/// <param name="EndColumn">1-based column one past the last character (exclusive, like LSP).</param>
public readonly record struct SourceSpan(
    int Offset,
    int Length,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn)
{
    public static readonly SourceSpan Missing = new(0, 0, 0, 0, 0, 0);

    public int End => Offset + Length;

    public static SourceSpan Covering(SourceSpan first, SourceSpan last) =>
        new(first.Offset, last.End - first.Offset,
            first.StartLine, first.StartColumn,
            last.EndLine, last.EndColumn);
}
