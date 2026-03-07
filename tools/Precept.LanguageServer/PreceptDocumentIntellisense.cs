using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Precept.LanguageServer;

internal static class PreceptDocumentIntellisense
{
    private static readonly Regex PreceptDeclRegex = new("^\\s*precept\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex StateDeclRegex = new("^\\s*state\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex EventDeclRegex = new("^\\s*event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex FieldDeclRegex = new("^\\s*field\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+(?<type>string|number|boolean)(?:\\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CollectionFieldDeclRegex = new("^\\s*field\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+(?<kind>set|queue|stack)\\s+of\\s+(?<type>string|number|boolean)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventWithArgsRegex = new("^\\s*event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s+with\\s+(?<args>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TransitionRowRegex = new("^\\s*from\\s+(?<states>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)(?<rest>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex StateClauseRegex = new("^\\s*(?<prep>in|to|from)\\s+(?<states>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+(?<tail>assert|edit|->)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventAssertRegex = new("^\\s*on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)\\s+assert\\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TransitionOutcomeRegex = new("\\btransition\\s+(?<state>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal static PreceptDocumentInfo Analyze(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var (model, _) = PreceptParser.ParseWithDiagnostics(text);

        var states = model?.States.Select(static state => state.Name).ToArray()
            ?? CollectIdentifiers(lines, StateDeclRegex);
        var events = model?.Events.Select(static evt => evt.Name).ToArray()
            ?? CollectIdentifiers(lines, EventDeclRegex);
        var fields = model?.Fields.Select(static field => field.Name).ToArray()
            ?? CollectIdentifiers(lines, FieldDeclRegex);
        var collections = model?.CollectionFields.Select(static field => field.Name).ToArray()
            ?? CollectIdentifiers(lines, CollectionFieldDeclRegex);
        var eventArgs = model is not null
            ? model.Events.ToDictionary(
                static evt => evt.Name,
                static evt => (IReadOnlyList<string>)evt.Args.Select(static arg => arg.Name).ToArray(),
                StringComparer.Ordinal)
            : CollectEventArgs(lines);
        var collectionKinds = BuildCollectionKinds(model, lines);
        var declarations = BuildDeclarations(lines, model);
        var documentSymbols = BuildDocumentSymbols(lines, model, declarations);

        return new PreceptDocumentInfo(
            text,
            lines,
            model,
            states,
            events,
            fields,
            collections,
            eventArgs,
            collectionKinds,
            declarations,
            documentSymbols);
    }

    internal static PreceptResolvedSymbol? ResolveSymbol(PreceptDocumentInfo info, Position position)
    {
        if (position.Line < 0 || position.Line >= info.Lines.Length)
            return null;

        var line = info.Lines[(int)position.Line];
        if (!TryGetIdentifierAtPosition(line, (int)position.Character, out var identifier, out var start, out var end))
            return null;

        var referenceRange = CreateRange((int)position.Line, start, end);

        foreach (var declaration in info.Declarations.All)
        {
            if (declaration.SelectionRange.Start.Line == position.Line &&
                declaration.SelectionRange.Start.Character <= start &&
                declaration.SelectionRange.End.Character >= end)
            {
                return new PreceptResolvedSymbol(declaration, referenceRange);
            }
        }

        if (TryResolveDottedSymbol(info, line, identifier, start, referenceRange, out var dotted))
            return dotted;

        var eventAssertMatch = EventAssertRegex.Match(line);
        if (eventAssertMatch.Success)
        {
            var eventName = eventAssertMatch.Groups["event"].Value;
            if (TryGetEventArg(info, eventName, identifier, out var argDeclaration))
                return new PreceptResolvedSymbol(argDeclaration, referenceRange);

            if (string.Equals(identifier, eventName, StringComparison.Ordinal) && info.Declarations.Events.TryGetValue(identifier, out var eventDeclaration))
                return new PreceptResolvedSymbol(eventDeclaration, referenceRange);
        }

        if (TryResolveStateReference(info, line, identifier, out var stateDeclaration))
            return new PreceptResolvedSymbol(stateDeclaration, referenceRange);

        if (TryResolveEventReference(info, line, identifier, start, out var eventReference))
            return new PreceptResolvedSymbol(eventReference, referenceRange);

        if (info.Declarations.Collections.TryGetValue(identifier, out var collectionDeclaration))
            return new PreceptResolvedSymbol(collectionDeclaration, referenceRange);

        if (info.Declarations.Fields.TryGetValue(identifier, out var fieldDeclaration))
            return new PreceptResolvedSymbol(fieldDeclaration, referenceRange);

        return null;
    }

    internal static Hover? CreateHover(PreceptDocumentInfo info, Position position)
    {
        var resolved = ResolveSymbol(info, position);
        if (resolved is not null)
        {
            var symbol = resolved.Value.Declaration;
            return new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = symbol.Markdown
                }),
                Range = resolved.Value.ReferenceRange
            };
        }

        // Fall back to construct/keyword hover from catalogs
        return CreateKeywordHover(info, position);
    }

    private static Hover? CreateKeywordHover(PreceptDocumentInfo info, Position position)
    {
        if (position.Line < 0 || position.Line >= info.Lines.Length)
            return null;

        var line = info.Lines[(int)position.Line];
        if (!TryGetIdentifierAtPosition(line, (int)position.Character, out var word, out var start, out var end))
            return null;

        // Check if the word matches any construct's leading keyword(s)
        PreceptParser.EnsureInitialized();
        var matchingConstructs = ConstructCatalog.Constructs
            .Where(c => ConstructFormStartsWithKeyword(c.Form, word))
            .ToList();

        if (matchingConstructs.Count > 0)
        {
            var markdown = string.Join("\n\n---\n\n", matchingConstructs.Select(c =>
                $"```precept\n{c.Form}\n```\n\n{c.Description}"));
            return new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = markdown
                }),
                Range = CreateRange((int)position.Line, start, end)
            };
        }

        // Fall back to token keyword description (Tier 1)
        var tokenDict = PreceptTokenMeta.BuildKeywordDictionary();
        if (tokenDict.TryGetValue(word, out var token))
        {
            var description = PreceptTokenMeta.GetDescription(token);
            if (description is not null)
            {
                return new Hover
                {
                    Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                    {
                        Kind = MarkupKind.Markdown,
                        Value = $"`{word}` — {description}"
                    }),
                    Range = CreateRange((int)position.Line, start, end)
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a construct form starts with the given keyword.
    /// Handles forms like "in|to|from ..." where any alternative matches.
    /// </summary>
    private static bool ConstructFormStartsWithKeyword(string form, string keyword)
    {
        // Split form into first token group (may contain | for alternatives)
        var spaceIdx = form.IndexOf(' ');
        var firstGroup = spaceIdx >= 0 ? form[..spaceIdx] : form;
        return firstGroup.Split('|').Any(k =>
            string.Equals(k, keyword, StringComparison.OrdinalIgnoreCase));
    }

    internal static LocationOrLocationLinks CreateDefinition(DocumentUri uri, PreceptDocumentInfo info, Position position)
    {
        var resolved = ResolveSymbol(info, position);
        if (resolved is null)
            return new LocationOrLocationLinks();

        var symbol = resolved.Value.Declaration;
        return new LocationOrLocationLinks(new Location
        {
            Uri = uri,
            Range = symbol.SelectionRange
        });
    }

    internal static SymbolInformationOrDocumentSymbolContainer CreateDocumentSymbols(PreceptDocumentInfo info)
        => new(info.DocumentSymbols.Select(static symbol => new SymbolInformationOrDocumentSymbol(symbol)));

    private static bool TryResolveDottedSymbol(
        PreceptDocumentInfo info,
        string line,
        string identifier,
        int identifierStart,
        Range referenceRange,
        out PreceptResolvedSymbol? resolved)
    {
        resolved = null;

        if (identifierStart <= 0 || line[identifierStart - 1] != '.')
            return false;

        var leftEnd = identifierStart - 2;
        if (leftEnd < 0)
            return false;

        var leftStart = leftEnd;
        while (leftStart >= 0 && IsIdentifierChar(line[leftStart]))
            leftStart--;
        leftStart++;

        if (leftStart > leftEnd)
            return false;

        var leftIdentifier = line[leftStart..(leftEnd + 1)];

        if (TryGetEventArg(info, leftIdentifier, identifier, out var argDeclaration))
        {
            resolved = new PreceptResolvedSymbol(argDeclaration, referenceRange);
            return true;
        }

        if (info.Declarations.Collections.TryGetValue(leftIdentifier, out var collectionDeclaration) &&
            info.CollectionKinds.TryGetValue(leftIdentifier, out var collectionKind) &&
            TryBuildCollectionAccessorSymbol(collectionDeclaration, identifier, collectionKind, out var accessorSymbol))
        {
            resolved = new PreceptResolvedSymbol(accessorSymbol, referenceRange);
            return true;
        }

        return false;
    }

    private static bool TryResolveStateReference(
        PreceptDocumentInfo info,
        string line,
        string identifier,
        out PreceptDeclaredSymbol declaration)
    {
        declaration = default!;

        if (!info.Declarations.States.TryGetValue(identifier, out declaration!))
            return false;

        if (StateDeclRegex.IsMatch(line))
            return true;

        var clauseMatch = StateClauseRegex.Match(line);
        if (clauseMatch.Success && clauseMatch.Groups["states"].Value.Split(',').Any(state => string.Equals(state.Trim(), identifier, StringComparison.Ordinal)))
            return true;

        var transitionMatch = TransitionRowRegex.Match(line);
        if (transitionMatch.Success && transitionMatch.Groups["states"].Value.Split(',').Any(state => string.Equals(state.Trim(), identifier, StringComparison.Ordinal)))
            return true;

        if (TransitionOutcomeRegex.Matches(line).Any(match => string.Equals(match.Groups["state"].Value, identifier, StringComparison.Ordinal)))
            return true;

        return false;
    }

    private static bool TryResolveEventReference(
        PreceptDocumentInfo info,
        string line,
        string identifier,
        int identifierStart,
        out PreceptDeclaredSymbol declaration)
    {
        declaration = default!;

        if (!info.Declarations.Events.TryGetValue(identifier, out declaration!))
            return false;

        if (EventDeclRegex.IsMatch(line))
            return true;

        var transitionMatch = TransitionRowRegex.Match(line);
        if (transitionMatch.Success && string.Equals(transitionMatch.Groups["event"].Value, identifier, StringComparison.Ordinal))
            return true;

        var eventAssertMatch = EventAssertRegex.Match(line);
        if (eventAssertMatch.Success && string.Equals(eventAssertMatch.Groups["event"].Value, identifier, StringComparison.Ordinal))
            return true;

        if (identifierStart < line.Length && line[Math.Min(identifierStart + identifier.Length, line.Length - 1)] == '.')
            return true;

        return false;
    }

    private static bool TryGetEventArg(PreceptDocumentInfo info, string eventName, string argName, out PreceptDeclaredSymbol declaration)
        => info.Declarations.EventArgs.TryGetValue($"{eventName}.{argName}", out declaration!);

    private static bool TryBuildCollectionAccessorSymbol(
        PreceptDeclaredSymbol collectionDeclaration,
        string accessor,
        PreceptCollectionKind kind,
        out PreceptDeclaredSymbol symbol)
    {
        symbol = default!;

        string detail;
        switch (accessor)
        {
            case "count":
                detail = "number";
                break;
            case "min" when kind == PreceptCollectionKind.Set:
            case "max" when kind == PreceptCollectionKind.Set:
                detail = collectionDeclaration.Detail;
                break;
            case "peek" when kind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack:
                detail = collectionDeclaration.Detail;
                break;
            default:
                return false;
        }

        symbol = new PreceptDeclaredSymbol(
            $"{collectionDeclaration.Name}.{accessor}",
            PreceptDeclaredSymbolKind.CollectionAccessor,
            detail,
            collectionDeclaration.Range,
            collectionDeclaration.SelectionRange,
            collectionDeclaration.ContainerName,
            $"```precept\n{collectionDeclaration.Name}.{accessor}\n```\n\nCollection accessor on `{collectionDeclaration.Name}` returning `{detail}`.");
        return true;
    }

    private static PreceptDeclarationIndex BuildDeclarations(string[] lines, PreceptDefinition? model)
    {
        var statesByName = model?.States.ToDictionary(static state => state.Name, StringComparer.Ordinal)
            ?? new Dictionary<string, PreceptState>(StringComparer.Ordinal);
        var eventsByName = model?.Events.ToDictionary(static evt => evt.Name, StringComparer.Ordinal)
            ?? new Dictionary<string, PreceptEvent>(StringComparer.Ordinal);
        var fieldsByName = model?.Fields.ToDictionary(static field => field.Name, StringComparer.Ordinal)
            ?? new Dictionary<string, PreceptField>(StringComparer.Ordinal);
        var collectionsByName = model?.CollectionFields.ToDictionary(static field => field.Name, StringComparer.Ordinal)
            ?? new Dictionary<string, PreceptCollectionField>(StringComparer.Ordinal);

        PreceptDeclaredSymbol? precept = null;
        var states = new Dictionary<string, PreceptDeclaredSymbol>(StringComparer.Ordinal);
        var events = new Dictionary<string, PreceptDeclaredSymbol>(StringComparer.Ordinal);
        var fields = new Dictionary<string, PreceptDeclaredSymbol>(StringComparer.Ordinal);
        var collections = new Dictionary<string, PreceptDeclaredSymbol>(StringComparer.Ordinal);
        var eventArgs = new Dictionary<string, PreceptDeclaredSymbol>(StringComparer.Ordinal);
        var ordered = new List<PreceptDeclaredSymbol>();

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];

            var preceptMatch = PreceptDeclRegex.Match(line);
            if (preceptMatch.Success)
            {
                precept = BuildSymbol(
                    preceptMatch.Groups["name"].Value,
                    PreceptDeclaredSymbolKind.Precept,
                    "precept",
                    lineIndex,
                    line,
                    "name",
                    null,
                    $"```precept\nprecept {preceptMatch.Groups["name"].Value}\n```\n\nPrecept declaration.");
                ordered.Add(precept);
                continue;
            }

            var collectionMatch = CollectionFieldDeclRegex.Match(line);
            if (collectionMatch.Success)
            {
                var name = collectionMatch.Groups["name"].Value;
                var collection = collectionsByName.TryGetValue(name, out var modelCollection)
                    ? modelCollection
                    : new PreceptCollectionField(name, ParseCollectionKind(collectionMatch.Groups["kind"].Value), ParseScalarType(collectionMatch.Groups["type"].Value));
                var detail = $"{collection.CollectionKind.ToString().ToLowerInvariant()} of {collection.InnerType.ToString().ToLowerInvariant()}";
                var symbol = BuildSymbol(
                    name,
                    PreceptDeclaredSymbolKind.CollectionField,
                    detail,
                    lineIndex,
                    line,
                    "name",
                    null,
                    $"```precept\nfield {name} as {detail}\n```\n\nCollection field of type `{detail}`.");
                collections[name] = symbol;
                ordered.Add(symbol);
                continue;
            }

            var fieldMatch = FieldDeclRegex.Match(line);
            if (fieldMatch.Success)
            {
                var name = fieldMatch.Groups["name"].Value;
                var detail = fieldsByName.TryGetValue(name, out var field)
                    ? FormatScalarFieldDetail(field)
                    : fieldMatch.Groups["type"].Value.ToLowerInvariant();
                var symbol = BuildSymbol(
                    name,
                    PreceptDeclaredSymbolKind.Field,
                    detail,
                    lineIndex,
                    line,
                    "name",
                    null,
                    BuildFieldMarkdown(name, fieldsByName.TryGetValue(name, out field) ? field : null));
                fields[name] = symbol;
                ordered.Add(symbol);
                continue;
            }

            var stateMatch = StateDeclRegex.Match(line);
            if (stateMatch.Success)
            {
                var name = stateMatch.Groups["name"].Value;
                var isInitial = model is not null && string.Equals(model.InitialState.Name, name, StringComparison.Ordinal);
                var symbol = BuildSymbol(
                    name,
                    PreceptDeclaredSymbolKind.State,
                    isInitial ? "initial state" : "state",
                    lineIndex,
                    line,
                    "name",
                    null,
                    $"```precept\nstate {name}{(isInitial ? " initial" : string.Empty)}\n```\n\n{(isInitial ? "Initial" : "Declared")} state.");
                states[name] = symbol;
                ordered.Add(symbol);
                continue;
            }

            var eventMatch = EventDeclRegex.Match(line);
            if (eventMatch.Success)
            {
                var name = eventMatch.Groups["name"].Value;
                eventsByName.TryGetValue(name, out var evt);
                var symbol = BuildSymbol(
                    name,
                    PreceptDeclaredSymbolKind.Event,
                    BuildEventDetail(evt),
                    lineIndex,
                    line,
                    "name",
                    null,
                    BuildEventMarkdown(name, evt));
                events[name] = symbol;
                ordered.Add(symbol);

                if (evt is not null)
                {
                    var searchStart = eventMatch.Groups["name"].Index + eventMatch.Groups["name"].Length;
                    foreach (var arg in evt.Args)
                    {
                        var argIndex = FindArgumentIndex(line, arg.Name, searchStart);
                        if (argIndex < 0)
                            continue;

                        var argSymbol = new PreceptDeclaredSymbol(
                            arg.Name,
                            PreceptDeclaredSymbolKind.EventArg,
                            FormatScalarType(arg.Type, arg.IsNullable),
                            CreateLineRange(lineIndex, line),
                            CreateRange(lineIndex, argIndex, argIndex + arg.Name.Length),
                            name,
                            BuildEventArgMarkdown(name, arg));
                        eventArgs[$"{name}.{arg.Name}"] = argSymbol;
                    }
                }
            }
        }

        return new PreceptDeclarationIndex(precept, states, events, fields, collections, eventArgs, ordered);
    }

    private static IReadOnlyList<DocumentSymbol> BuildDocumentSymbols(
        string[] lines,
        PreceptDefinition? model,
        PreceptDeclarationIndex declarations)
    {
        var children = new List<DocumentSymbol>();

        children.AddRange(declarations.Fields.Values.Select(static field => new DocumentSymbol
        {
            Name = field.Name,
            Detail = field.Detail,
            Kind = SymbolKind.Field,
            Range = field.Range,
            SelectionRange = field.SelectionRange
        }));

        children.AddRange(declarations.Collections.Values.Select(static field => new DocumentSymbol
        {
            Name = field.Name,
            Detail = field.Detail,
            Kind = SymbolKind.Array,
            Range = field.Range,
            SelectionRange = field.SelectionRange
        }));

        children.AddRange(declarations.States.Values.Select(static state => new DocumentSymbol
        {
            Name = state.Name,
            Detail = state.Detail,
            Kind = SymbolKind.EnumMember,
            Range = state.Range,
            SelectionRange = state.SelectionRange
        }));

        foreach (var eventDeclaration in declarations.Events.Values)
        {
            var eventChildren = declarations.EventArgs.Values
                .Where(arg => string.Equals(arg.ContainerName, eventDeclaration.Name, StringComparison.Ordinal))
                .Select(static arg => new DocumentSymbol
                {
                    Name = arg.Name,
                    Detail = arg.Detail,
                    Kind = SymbolKind.Field,
                    Range = arg.Range,
                    SelectionRange = arg.SelectionRange
                })
                .ToArray();

            children.Add(new DocumentSymbol
            {
                Name = eventDeclaration.Name,
                Detail = eventDeclaration.Detail,
                Kind = SymbolKind.Event,
                Range = eventDeclaration.Range,
                SelectionRange = eventDeclaration.SelectionRange,
                Children = eventChildren.Length > 0 ? new Container<DocumentSymbol>(eventChildren) : null
            });
        }

        if (model?.Invariants is not null)
        {
            foreach (var invariant in model.Invariants)
            {
                var lineIndex = Math.Max(0, invariant.SourceLine - 1);
                if (lineIndex >= lines.Length)
                    continue;

                children.Add(new DocumentSymbol
                {
                    Name = "invariant",
                    Detail = invariant.ExpressionText,
                    Kind = SymbolKind.Boolean,
                    Range = CreateLineRange(lineIndex, lines[lineIndex]),
                    SelectionRange = CreateLineRange(lineIndex, lines[lineIndex])
                });
            }
        }

        if (declarations.Precept is null)
            return children;

        var rootRange = new Range(new Position(0, 0), new Position(Math.Max(lines.Length - 1, 0), lines.Length == 0 ? 0 : lines[^1].Length));
        return
        [
            new DocumentSymbol
            {
                Name = declarations.Precept.Name,
                Detail = "precept",
                Kind = SymbolKind.Object,
                Range = rootRange,
                SelectionRange = declarations.Precept.SelectionRange,
                Children = new Container<DocumentSymbol>(children)
            }
        ];
    }

    private static PreceptDeclaredSymbol BuildSymbol(
        string name,
        PreceptDeclaredSymbolKind kind,
        string detail,
        int lineIndex,
        string line,
        string groupName,
        string? containerName,
        string markdown)
    {
        var match = kind switch
        {
            PreceptDeclaredSymbolKind.Precept => PreceptDeclRegex.Match(line),
            PreceptDeclaredSymbolKind.State => StateDeclRegex.Match(line),
            PreceptDeclaredSymbolKind.Event => EventDeclRegex.Match(line),
            PreceptDeclaredSymbolKind.Field => FieldDeclRegex.Match(line),
            PreceptDeclaredSymbolKind.CollectionField => CollectionFieldDeclRegex.Match(line),
            _ => throw new InvalidOperationException($"Unsupported declaration kind '{kind}'.")
        };

        var start = match.Groups[groupName].Index;
        return new PreceptDeclaredSymbol(
            name,
            kind,
            detail,
            CreateLineRange(lineIndex, line),
            CreateRange(lineIndex, start, start + name.Length),
            containerName,
            markdown);
    }

    private static string BuildFieldMarkdown(string name, PreceptField? field)
    {
        if (field is null)
            return $"```precept\nfield {name}\n```\n\nField declaration.";

        var lines = new List<string>
        {
            $"```precept\nfield {name} as {FormatScalarType(field.Type, field.IsNullable)}{(field.HasDefaultValue ? $" default {field.DefaultValue}" : string.Empty)}\n```",
            string.Empty,
            $"Type: `{FormatScalarType(field.Type, field.IsNullable)}`"
        };

        if (field.HasDefaultValue)
            lines.Add($"Default: `{field.DefaultValue ?? "null"}`");

        return string.Join("\n", lines);
    }

    private static string BuildEventMarkdown(string name, PreceptEvent? evt)
    {
        if (evt is null || evt.Args.Count == 0)
            return $"```precept\nevent {name}\n```\n\nEvent with no arguments.";

        var args = string.Join(", ", evt.Args.Select(arg => $"{arg.Name}: {FormatScalarType(arg.Type, arg.IsNullable)}{(arg.HasDefaultValue ? $" = {arg.DefaultValue}" : string.Empty)}"));
        return $"```precept\nevent {name}({args})\n```\n\nEvent with {evt.Args.Count} argument(s).";
    }

    private static string BuildEventArgMarkdown(string eventName, PreceptEventArg arg)
        => $"```precept\n{eventName}.{arg.Name}: {FormatScalarType(arg.Type, arg.IsNullable)}\n```\n\nEvent argument for `{eventName}`{(arg.HasDefaultValue ? $". Default: `{arg.DefaultValue ?? "null"}`." : ".")}";

    private static string BuildEventDetail(PreceptEvent? evt)
        => evt is null
            ? "event"
            : evt.Args.Count == 0
                ? "no args"
                : string.Join(", ", evt.Args.Select(arg => $"{arg.Name}: {FormatScalarType(arg.Type, arg.IsNullable)}"));

    private static string FormatScalarFieldDetail(PreceptField field)
    {
        var detail = FormatScalarType(field.Type, field.IsNullable);
        if (field.HasDefaultValue)
            detail += $" = {field.DefaultValue ?? "null"}";
        return detail;
    }

    private static string FormatScalarType(PreceptScalarType type, bool isNullable)
        => isNullable
            ? $"{type.ToString().ToLowerInvariant()}?"
            : type.ToString().ToLowerInvariant();

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CollectEventArgs(string[] lines)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var match = EventWithArgsRegex.Match(line);
            if (!match.Success)
                continue;

            var eventName = match.Groups["name"].Value;
            var args = Regex.Matches(match.Groups["args"].Value, "\\b(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+", RegexOptions.IgnoreCase)
                .Select(static value => value.Groups["name"].Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            result[eventName] = args;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, PreceptCollectionKind> BuildCollectionKinds(PreceptDefinition? model, string[] lines)
    {
        if (model is not null)
        {
            return model.CollectionFields.ToDictionary(
                static field => field.Name,
                static field => field.CollectionKind,
                StringComparer.Ordinal);
        }

        var kinds = new Dictionary<string, PreceptCollectionKind>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var match = CollectionFieldDeclRegex.Match(line);
            if (!match.Success)
                continue;

            kinds[match.Groups["name"].Value] = ParseCollectionKind(match.Groups["kind"].Value);
        }

        return kinds;
    }

    private static string[] CollectIdentifiers(string[] lines, Regex regex)
        => lines
            .Select(line => regex.Match(line))
            .Where(static match => match.Success)
            .Select(static match => match.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

    private static bool TryGetIdentifierAtPosition(string line, int character, out string identifier, out int start, out int end)
    {
        identifier = string.Empty;
        start = 0;
        end = 0;

        if (string.IsNullOrEmpty(line))
            return false;

        var index = Math.Min(Math.Max(character, 0), line.Length - 1);
        if (!IsIdentifierChar(line[index]) && index > 0 && IsIdentifierChar(line[index - 1]))
            index--;

        if (!IsIdentifierChar(line[index]))
            return false;

        start = index;
        while (start > 0 && IsIdentifierChar(line[start - 1]))
            start--;

        end = index + 1;
        while (end < line.Length && IsIdentifierChar(line[end]))
            end++;

        identifier = line[start..end];
        return true;
    }

    private static bool IsIdentifierChar(char value)
        => char.IsLetterOrDigit(value) || value == '_';

    private static int FindArgumentIndex(string line, string argumentName, int searchStart)
    {
        var pattern = $"\\b{Regex.Escape(argumentName)}\\b\\s+as\\b";
        var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
        while (match.Success && match.Index < searchStart)
            match = match.NextMatch();
        return match.Success ? match.Index : -1;
    }

    private static Range CreateRange(int line, int start, int end)
        => new(new Position(line, start), new Position(line, end));

    private static Range CreateLineRange(int line, string text)
        => CreateRange(line, 0, text.Length);

    private static PreceptCollectionKind ParseCollectionKind(string value) => value.ToLowerInvariant() switch
    {
        "set" => PreceptCollectionKind.Set,
        "queue" => PreceptCollectionKind.Queue,
        "stack" => PreceptCollectionKind.Stack,
        _ => throw new InvalidOperationException($"Unknown collection kind '{value}'.")
    };

    private static PreceptScalarType ParseScalarType(string value) => value.ToLowerInvariant() switch
    {
        "string" => PreceptScalarType.String,
        "number" => PreceptScalarType.Number,
        "boolean" => PreceptScalarType.Boolean,
        _ => throw new InvalidOperationException($"Unknown scalar type '{value}'.")
    };
}

internal sealed record PreceptDocumentInfo(
    string Text,
    string[] Lines,
    PreceptDefinition? Model,
    IReadOnlyList<string> States,
    IReadOnlyList<string> Events,
    IReadOnlyList<string> Fields,
    IReadOnlyList<string> CollectionFields,
    IReadOnlyDictionary<string, IReadOnlyList<string>> EventArgs,
    IReadOnlyDictionary<string, PreceptCollectionKind> CollectionKinds,
    PreceptDeclarationIndex Declarations,
    IReadOnlyList<DocumentSymbol> DocumentSymbols);

internal sealed record PreceptDeclarationIndex(
    PreceptDeclaredSymbol? Precept,
    IReadOnlyDictionary<string, PreceptDeclaredSymbol> States,
    IReadOnlyDictionary<string, PreceptDeclaredSymbol> Events,
    IReadOnlyDictionary<string, PreceptDeclaredSymbol> Fields,
    IReadOnlyDictionary<string, PreceptDeclaredSymbol> Collections,
    IReadOnlyDictionary<string, PreceptDeclaredSymbol> EventArgs,
    IReadOnlyList<PreceptDeclaredSymbol> All);

internal sealed record PreceptDeclaredSymbol(
    string Name,
    PreceptDeclaredSymbolKind Kind,
    string Detail,
    Range Range,
    Range SelectionRange,
    string? ContainerName,
    string Markdown);

internal readonly record struct PreceptResolvedSymbol(
    PreceptDeclaredSymbol Declaration,
    Range ReferenceRange);

internal enum PreceptDeclaredSymbolKind
{
    Precept,
    State,
    Event,
    EventArg,
    Field,
    CollectionField,
    CollectionAccessor
}