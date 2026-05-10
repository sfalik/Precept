using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer;

internal static class SymbolNavigation
{
    internal static bool TryFindOccurrence(Compilation compilation, Position position, out SymbolOccurrence occurrence)
    {
        var semantics = compilation.Semantics;

        foreach (var field in semantics.Fields)
        {
            if (Contains(field.NameSpan, position))
            {
                occurrence = new FieldOccurrence(field);
                return true;
            }
        }

        foreach (var state in semantics.States)
        {
            if (Contains(state.NameSpan, position))
            {
                occurrence = new StateOccurrence(state);
                return true;
            }
        }

        foreach (var @event in semantics.Events)
        {
            if (Contains(@event.NameSpan, position))
            {
                occurrence = new EventOccurrence(@event);
                return true;
            }

            foreach (var arg in @event.Args)
            {
                if (Contains(arg.Span, position))
                {
                    occurrence = new ArgOccurrence(arg);
                    return true;
                }
            }
        }

        foreach (var fieldReference in semantics.FieldReferences)
        {
            if (Contains(fieldReference.Site, position))
            {
                occurrence = new FieldOccurrence(fieldReference.Field);
                return true;
            }
        }

        foreach (var stateReference in semantics.StateReferences)
        {
            if (Contains(stateReference.Site, position))
            {
                occurrence = new StateOccurrence(stateReference.State);
                return true;
            }
        }

        foreach (var eventReference in semantics.EventReferences)
        {
            if (Contains(eventReference.Site, position))
            {
                occurrence = new EventOccurrence(eventReference.Event);
                return true;
            }
        }

        foreach (var argReference in semantics.ArgReferences)
        {
            if (Contains(argReference.Site, position))
            {
                occurrence = new ArgOccurrence(argReference.Arg);
                return true;
            }
        }

        occurrence = null!;
        return false;
    }

    internal static ImmutableArray<SourceSpan> GetReferenceSpans(SemanticIndex index, SymbolOccurrence occurrence, bool includeDeclaration)
    {
        IEnumerable<SourceSpan> spans = occurrence switch
        {
            FieldOccurrence field => index.FieldReferences
                .Where(reference => reference.Field == field.Field)
                .Select(reference => reference.Site),
            StateOccurrence state => index.StateReferences
                .Where(reference => reference.State == state.State)
                .Select(reference => reference.Site),
            EventOccurrence @event => index.EventReferences
                .Where(reference => reference.Event == @event.Event)
                .Select(reference => reference.Site),
            ArgOccurrence arg => index.ArgReferences
                .Where(reference => reference.Arg == arg.Arg)
                .Select(reference => reference.Site),
            _ => Enumerable.Empty<SourceSpan>(),
        };

        if (includeDeclaration)
        {
            spans = spans.Prepend(occurrence.DeclarationSpan);
        }

        return spans.ToImmutableArray();
    }

    internal static ImmutableArray<SourceSpan> GetRenameSpans(SemanticIndex index, SymbolOccurrence occurrence)
    {
        var spans = GetReferenceSpans(index, occurrence, includeDeclaration: true);
        return occurrence is ArgOccurrence arg
            ? spans.Select(span => ToArgIdentifierSpan(arg.Arg, span)).ToImmutableArray()
            : spans;
    }

    internal static bool TryGetRenameSpanAtPosition(SemanticIndex index, SymbolOccurrence occurrence, Position position, out SourceSpan span)
    {
        foreach (var candidate in occurrence is ArgOccurrence
                     ? GetRenameSpans(index, occurrence)
                     : GetReferenceSpans(index, occurrence, includeDeclaration: true))
        {
            if (Contains(candidate, position))
            {
                span = candidate;
                return true;
            }
        }

        span = default;
        return false;
    }

    internal static bool Contains(SourceSpan span, Position position)
    {
        var line = position.Line + 1;
        var character = position.Character + 1;

        if (line < span.StartLine || line > span.EndLine)
        {
            return false;
        }

        if (span.StartLine == span.EndLine)
        {
            return character >= span.StartColumn && character < span.EndColumn;
        }

        if (line == span.StartLine)
        {
            return character >= span.StartColumn;
        }

        if (line == span.EndLine)
        {
            return character < span.EndColumn;
        }

        return true;
    }

    private static SourceSpan ToArgIdentifierSpan(TypedArg arg, SourceSpan span)
    {
        if (span.Length == arg.Name.Length)
        {
            return span;
        }

        return span.StartLine == arg.Span.StartLine
               && span.StartColumn == arg.Span.StartColumn
            ? new SourceSpan(
                span.Offset,
                arg.Name.Length,
                span.StartLine,
                span.StartColumn,
                span.StartLine,
                span.StartColumn + arg.Name.Length)
            : new SourceSpan(
                span.End - arg.Name.Length,
                arg.Name.Length,
                span.EndLine,
                span.EndColumn - arg.Name.Length,
                span.EndLine,
                span.EndColumn);
    }
}

internal abstract record SymbolOccurrence
{
    internal abstract SourceSpan DeclarationSpan { get; }
}

internal sealed record FieldOccurrence(TypedField Field) : SymbolOccurrence
{
    internal override SourceSpan DeclarationSpan => Field.NameSpan;
}

internal sealed record StateOccurrence(TypedState State) : SymbolOccurrence
{
    internal override SourceSpan DeclarationSpan => State.NameSpan;
}

internal sealed record EventOccurrence(TypedEvent Event) : SymbolOccurrence
{
    internal override SourceSpan DeclarationSpan => Event.NameSpan;
}

internal sealed record ArgOccurrence(TypedArg Arg) : SymbolOccurrence
{
    internal override SourceSpan DeclarationSpan => Arg.Span;
}
