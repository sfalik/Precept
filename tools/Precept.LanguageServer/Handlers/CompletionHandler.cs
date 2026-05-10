using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

internal sealed class CompletionHandler : ICompletionHandler
{
    private readonly DocumentStore _store;

    public CompletionHandler(DocumentStore store)
    {
        _store = store;
    }

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
            TriggerCharacters = new Container<string>(" ", "'", ".", ">", "~"),
            ResolveProvider = false,
        };

    public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult(new CompletionList([], true));
        }

        return Task.FromResult(GetCompletions(state.Current, request.Position));
    }

    private static CompletionList GetCompletions(Compilation compilation, Position position)
    {
        return SlotContextResolver.GetCursorContext(compilation, position) switch
        {
            SlotContext.TopLevel => new CompletionList(GetTopLevelItems()),
            SlotContext.InTypePosition => new CompletionList(GetTypeItems()),
            SlotContext.InModifierPosition => CreateModifierList(compilation, position),
            SlotContext.InStateTarget => new CompletionList(GetStateItems(compilation)),
            SlotContext.InEventTarget => new CompletionList(GetEventItems(compilation)),
            SlotContext.InFieldTarget => new CompletionList(GetFieldItems(compilation)),
            _ => new CompletionList([], true),
        };
    }

    private static CompletionList CreateModifierList(Compilation compilation, Position position)
    {
        var construct = SlotContextResolver.GetEnclosingConstruct(compilation, position);
        if (construct is null)
        {
            return new CompletionList([], true);
        }

        return new CompletionList(GetModifierItems(construct.Meta.ModifierDomain));
    }

    private static IEnumerable<CompletionItem> GetTopLevelItems() =>
        Precept.Language.Constructs.All
            .Where(meta => meta.AllowedIn.Length == 0)
            .Select(meta => CreateItem(
                label: Precept.Language.Tokens.GetMeta(meta.PrimaryLeadingToken).Text ?? meta.Name,
                detail: meta.Description,
                kind: CompletionItemKind.Keyword));

    private static IEnumerable<CompletionItem> GetTypeItems() =>
        Precept.Language.Types.All
            .Where(meta => meta.Token is not null)
            .Select(meta => CreateItem(
                label: meta.Token!.Text ?? meta.DisplayName,
                detail: meta.Description,
                kind: CompletionItemKind.Class));

    private static IEnumerable<CompletionItem> GetModifierItems(Precept.Language.ModifierDomain domain) =>
        GetModifiers(domain)
            .Select(meta => CreateItem(
                label: meta.Token.Text ?? meta.Kind.ToString().ToLowerInvariant(),
                detail: meta.Description,
                kind: CompletionItemKind.Keyword));

    private static IEnumerable<CompletionItem> GetStateItems(Compilation compilation) =>
        compilation.Semantics.States
            .Select(state => state.Name)
            .Distinct(System.StringComparer.Ordinal)
            .Select(name => CreateItem(name, "State", CompletionItemKind.EnumMember));

    private static IEnumerable<CompletionItem> GetEventItems(Compilation compilation) =>
        compilation.Semantics.Events
            .Select(evt => evt.Name)
            .Distinct(System.StringComparer.Ordinal)
            .Select(name => CreateItem(name, "Event", CompletionItemKind.Event));

    private static IEnumerable<CompletionItem> GetFieldItems(Compilation compilation) =>
        compilation.Semantics.Fields
            .Select(field => field.Name)
            .Distinct(System.StringComparer.Ordinal)
            .Select(name => CreateItem(name, "Field", CompletionItemKind.Field));

    private static IEnumerable<Precept.Language.ModifierMeta> GetModifiers(Precept.Language.ModifierDomain domain) => domain switch
    {
        Precept.Language.ModifierDomain.Field => Precept.Language.Modifiers.All.OfType<Precept.Language.ValueModifierMeta>(),
        Precept.Language.ModifierDomain.State => Precept.Language.Modifiers.All.OfType<Precept.Language.StateModifierMeta>(),
        Precept.Language.ModifierDomain.Event => Precept.Language.Modifiers.All.OfType<Precept.Language.EventModifierMeta>(),
        Precept.Language.ModifierDomain.Access => Precept.Language.Modifiers.All.OfType<Precept.Language.AccessModifierMeta>(),
        Precept.Language.ModifierDomain.Anchor => Precept.Language.Modifiers.All.OfType<Precept.Language.AnchorModifierMeta>(),
        _ => [],
    };

    private static CompletionItem CreateItem(string label, string detail, CompletionItemKind kind) =>
        new()
        {
            Label = label,
            InsertText = label,
            Detail = detail,
            Kind = kind,
        };
}
