using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer;

internal static class OutlineSymbolProjector
{
    internal static SymbolInformationOrDocumentSymbolContainer BuildDocumentSymbols(Compilation compilation) =>
        new(Project(compilation)
            .Select(symbol => new SymbolInformationOrDocumentSymbol(new DocumentSymbol
            {
                Name = symbol.Name,
                Kind = symbol.Kind,
                Range = DiagnosticProjector.ToRange(symbol.Range),
                SelectionRange = DiagnosticProjector.ToRange(symbol.SelectionRange),
            })));

    internal static OutlineSymbolProjection[] Project(Compilation compilation) =>
        compilation.ConstructManifest.Constructs
            .Where(construct => construct.Meta.IsOutlineNode && construct.Meta.OutlineSymbolTag is not null)
            .Select(construct =>
            {
                var selectionSpan = GetSelectionSpan(compilation, construct);
                return new OutlineSymbolProjection(
                    ExtractName(construct),
                    Enum.Parse<SymbolKind>(construct.Meta.OutlineSymbolTag!),
                    ExpandRangeToContainSelection(construct.Span, selectionSpan),
                    selectionSpan);
            })
            .ToArray();

    internal static SourceSpan GetSelectionSpan(Compilation compilation, ParsedConstruct construct) =>
        construct.Meta.Kind switch
        {
            ConstructKind.PreceptHeader => construct.GetRequiredSlot<IdentifierListSlot>(ConstructSlotKind.IdentifierList).Span,
            ConstructKind.FieldDeclaration => compilation.Semantics.Fields.FirstOrDefault(field => field.Syntax == construct)?.NameSpan ?? construct.Span,
            ConstructKind.StateDeclaration => compilation.Semantics.States.FirstOrDefault(state => state.Syntax == construct)?.NameSpan ?? construct.Span,
            ConstructKind.EventDeclaration => compilation.Semantics.Events.FirstOrDefault(evt => evt.Syntax == construct)?.NameSpan ?? construct.Span,
            ConstructKind.RuleDeclaration => construct.Span,
            _ => construct.Span,
        };

    internal static SourceSpan ExpandRangeToContainSelection(SourceSpan range, SourceSpan selectionRange)
    {
        if (Contains(range, selectionRange))
        {
            return range;
        }

        var start = range.Offset <= selectionRange.Offset ? range : selectionRange;
        var end = range.End >= selectionRange.End ? range : selectionRange;

        return new SourceSpan(
            Math.Min(range.Offset, selectionRange.Offset),
            Math.Max(range.End, selectionRange.End) - Math.Min(range.Offset, selectionRange.Offset),
            start.StartLine,
            start.StartColumn,
            end.EndLine,
            end.EndColumn);
    }

    private static bool Contains(SourceSpan range, SourceSpan selectionRange) =>
        range.Offset <= selectionRange.Offset && range.End >= selectionRange.End;

    private static string ExtractName(ParsedConstruct construct)
    {
        if (construct.Slots.OfType<IdentifierListSlot>().FirstOrDefault() is { Names.Length: > 0 } identifiers)
        {
            return string.Join(", ", identifiers.Names);
        }

        if (construct.Slots.OfType<StateEntryListSlot>().FirstOrDefault() is { Entries.Length: > 0 } states)
        {
            return string.Join(", ", states.Entries.Select(static entry => entry.Name));
        }

        if (construct.Slots.OfType<EventEntryListSlot>().FirstOrDefault() is { Entries.Length: > 0 } events)
        {
            return string.Join(", ", events.Entries.Select(static entry => entry.Name));
        }

        return construct.Meta.Name;
    }

    internal readonly record struct OutlineSymbolProjection(
        string Name,
        SymbolKind Kind,
        SourceSpan Range,
        SourceSpan SelectionRange);
}
