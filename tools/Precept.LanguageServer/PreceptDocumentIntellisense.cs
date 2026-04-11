using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Precept.LanguageServer;

internal static class PreceptDocumentIntellisense
{
    // ── Regex capture group convention ──────────────────────────────────
    // Multi-name declaration regexes (State, Event, Field, CollectionField) use a "rest" capture
    // group for the name list portion. CollectMultiIdentifiers, BuildCollectionKinds,
    // BuildCollectionInnerTypes, and BuildFieldTypeKinds all depend on this group name.
    // When changing a capture group name, audit EVERY consumer — especially fallback-path
    // code that only runs when the parser returns null (e.g. completion tests with cursor
    // positions that create invalid syntax).
    private static readonly Regex PreceptDeclRegex = new("^\\s*precept\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex StateDeclRegex = new("^\\s*state\\s+(?<rest>.+)$", RegexOptions.Compiled);
    private static readonly Regex EventDeclRegex = new("^\\s*event\\s+(?<rest>.+)$", RegexOptions.Compiled);
    private static readonly Regex FieldDeclRegex = new("^\\s*field\\s+(?<rest>(?:[A-Za-z_]\\w*\\s*,\\s*)*[A-Za-z_]\\w*)\\s+as\\s+(?<type>string|number|boolean|integer|decimal)(?:\\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ChoiceFieldDeclRegex = new("^\\s*field\\s+(?<rest>(?:[A-Za-z_]\\w*\\s*,\\s*)*[A-Za-z_]\\w*)\\s+as\\s+choice\\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CollectionFieldDeclRegex = new("^\\s*field\\s+(?<rest>(?:[A-Za-z_]\\w*\\s*,\\s*)*[A-Za-z_]\\w*)\\s+as\\s+(?<kind>set|queue|stack)\\s+of\\s+(?<type>string|number|boolean|integer|decimal|choice)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventWithArgsRegex = new("^\\s*event\\s+(?<names>(?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)\\s+with\\s+(?<args>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IdentifierPattern = new("[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);
    private static readonly HashSet<string> StateKeywords = new(StringComparer.Ordinal) { "initial" };
    private static readonly HashSet<string> EmptyKeywords = new(StringComparer.Ordinal);
    private static readonly Regex TransitionRowRegex = new("^\\s*from\\s+(?<states>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)(?<rest>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex StateClauseRegex = new("^\\s*(?<prep>in|to|from)\\s+(?<states>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+(?<tail>assert|edit|->)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventAssertRegex = new("^\\s*on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)\\s+assert\\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TransitionOutcomeRegex = new("\\btransition\\s+(?<state>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal static PreceptDocumentInfo Analyze(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var (model, _) = PreceptParser.ParseWithDiagnostics(text);

        var states = model?.States.Select(static state => state.Name).ToArray()
            ?? CollectMultiIdentifiers(lines, StateDeclRegex, StateKeywords);
        var events = model?.Events.Select(static evt => evt.Name).ToArray()
            ?? CollectEventNames(lines);
        var fields = model?.Fields.Select(static field => field.Name).ToArray()
            ?? CollectMultiIdentifiers(lines, FieldDeclRegex, EmptyKeywords);
        var collections = model?.CollectionFields.Select(static field => field.Name).ToArray()
            ?? CollectMultiIdentifiers(lines, CollectionFieldDeclRegex, EmptyKeywords);
        var eventArgs = model is not null
            ? model.Events.ToDictionary(
                static evt => evt.Name,
                static evt => (IReadOnlyList<string>)evt.Args.Select(static arg => arg.Name).ToArray(),
                StringComparer.Ordinal)
            : CollectEventArgs(lines);
        var collectionKinds = BuildCollectionKinds(model, lines);
        var collectionInnerTypes = BuildCollectionInnerTypes(model, lines);
        var fieldTypeKinds = BuildFieldTypeKinds(model, lines);
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
            collectionInnerTypes,
            fieldTypeKinds,
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
                var namesGroup = collectionMatch.Groups["rest"];
                foreach (Match nameMatch in IdentifierPattern.Matches(namesGroup.Value))
                {
                    var name = nameMatch.Value;
                    var collection = collectionsByName.TryGetValue(name, out var modelCollection)
                        ? modelCollection
                        : new PreceptCollectionField(name, ParseCollectionKind(collectionMatch.Groups["kind"].Value), ParseScalarType(collectionMatch.Groups["type"].Value));
                    var detail = $"{collection.CollectionKind.ToString().ToLowerInvariant()} of {collection.InnerType.ToString().ToLowerInvariant()}";
                    var nameStart = namesGroup.Index + nameMatch.Index;
                    var symbol = new PreceptDeclaredSymbol(
                        name,
                        PreceptDeclaredSymbolKind.CollectionField,
                        detail,
                        CreateLineRange(lineIndex, line),
                        CreateRange(lineIndex, nameStart, nameStart + name.Length),
                        null,
                        $"```precept\nfield {name} as {detail}\n```\n\nCollection field of type `{detail}`.");
                    collections[name] = symbol;
                    ordered.Add(symbol);
                }
                continue;
            }

            var fieldMatch = FieldDeclRegex.Match(line);
            if (fieldMatch.Success)
            {
                var namesGroup = fieldMatch.Groups["rest"];
                foreach (Match nameMatch in IdentifierPattern.Matches(namesGroup.Value))
                {
                    var name = nameMatch.Value;
                    var detail = fieldsByName.TryGetValue(name, out var field)
                        ? FormatScalarFieldDetail(field)
                        : fieldMatch.Groups["type"].Value.ToLowerInvariant();
                    var nameStart = namesGroup.Index + nameMatch.Index;
                    var symbol = new PreceptDeclaredSymbol(
                        name,
                        PreceptDeclaredSymbolKind.Field,
                        detail,
                        CreateLineRange(lineIndex, line),
                        CreateRange(lineIndex, nameStart, nameStart + name.Length),
                        null,
                        BuildFieldMarkdown(name, fieldsByName.TryGetValue(name, out field) ? field : null));
                    fields[name] = symbol;
                    ordered.Add(symbol);
                }
                continue;
            }

            var choiceFieldMatch = ChoiceFieldDeclRegex.Match(line);
            if (choiceFieldMatch.Success)
            {
                var namesGroup = choiceFieldMatch.Groups["rest"];
                foreach (Match nameMatch in IdentifierPattern.Matches(namesGroup.Value))
                {
                    var name = nameMatch.Value;
                    fieldsByName.TryGetValue(name, out var field);
                    var detail = field is not null ? FormatScalarFieldDetail(field) : "choice";
                    var nameStart = namesGroup.Index + nameMatch.Index;
                    var symbol = new PreceptDeclaredSymbol(
                        name,
                        PreceptDeclaredSymbolKind.Field,
                        detail,
                        CreateLineRange(lineIndex, line),
                        CreateRange(lineIndex, nameStart, nameStart + name.Length),
                        null,
                        BuildFieldMarkdown(name, field));
                    fields[name] = symbol;
                    ordered.Add(symbol);
                }
                continue;
            }

            var stateMatch = StateDeclRegex.Match(line);
            if (stateMatch.Success)
            {
                var rest = stateMatch.Groups["rest"].Value;
                var restStart = stateMatch.Groups["rest"].Index;
                foreach (Match nameMatch in IdentifierPattern.Matches(rest))
                {
                    var name = nameMatch.Value;
                    if (StateKeywords.Contains(name))
                        continue;
                    var isInitial = model is not null && string.Equals(model.InitialState?.Name, name, StringComparison.Ordinal);
                    var nameStart = restStart + nameMatch.Index;
                    var symbol = new PreceptDeclaredSymbol(
                        name,
                        PreceptDeclaredSymbolKind.State,
                        isInitial ? "initial state" : "state",
                        CreateLineRange(lineIndex, line),
                        CreateRange(lineIndex, nameStart, nameStart + name.Length),
                        null,
                        $"```precept\nstate {name}{(isInitial ? " initial" : string.Empty)}\n```\n\n{(isInitial ? "Initial" : "Declared")} state.");
                    states[name] = symbol;
                    ordered.Add(symbol);
                }
                continue;
            }

            var eventMatch = EventDeclRegex.Match(line);
            if (eventMatch.Success)
            {
                var rest = eventMatch.Groups["rest"].Value;
                var restStart = eventMatch.Groups["rest"].Index;

                // Extract event names (stop at "with" keyword)
                var nameMatches = IdentifierPattern.Matches(rest)
                    .Cast<Match>()
                    .TakeWhile(static m => !string.Equals(m.Value, "with", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Track where args start for arg symbol creation
                var withIdx = rest.IndexOf(" with ", StringComparison.OrdinalIgnoreCase);
                var argsSearchStart = withIdx >= 0 ? restStart + withIdx : line.Length;

                foreach (var nameMatch in nameMatches)
                {
                    var name = nameMatch.Value;
                    eventsByName.TryGetValue(name, out var evt);
                    var nameStart = restStart + nameMatch.Index;
                    var symbol = new PreceptDeclaredSymbol(
                        name,
                        PreceptDeclaredSymbolKind.Event,
                        BuildEventDetail(evt),
                        CreateLineRange(lineIndex, line),
                        CreateRange(lineIndex, nameStart, nameStart + name.Length),
                        null,
                        BuildEventMarkdown(name, evt));
                    events[name] = symbol;
                    ordered.Add(symbol);

                    if (evt is not null)
                    {
                        foreach (var arg in evt.Args)
                        {
                            var argIndex = FindArgumentIndex(line, arg.Name, argsSearchStart);
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

        var typeLabel = field.Type == PreceptScalarType.Choice && field.ChoiceValues?.Count > 0
            ? $"choice({string.Join(", ", field.ChoiceValues.Select(v => $"\"{v}\""))})"
            : FormatScalarType(field.Type, field.IsNullable);

        var suffix = new System.Text.StringBuilder();
        if (field.IsNullable) suffix.Append(" nullable");
        if (field.IsOrdered) suffix.Append(" ordered");
        if (field.HasDefaultValue) suffix.Append($" default {field.DefaultValue}");

        var lines = new List<string>
        {
            $"```precept\nfield {name} as {typeLabel}{suffix}\n```",
            string.Empty,
            $"Type: `{typeLabel}`"
        };

        if (field.IsOrdered)
            lines.Add($"Ordered: values compare in declaration order");

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

            var names = IdentifierPattern.Matches(match.Groups["names"].Value)
                .Select(static m => m.Value)
                .ToArray();
            var args = Regex.Matches(match.Groups["args"].Value, "\\b(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s+as\\s+", RegexOptions.IgnoreCase)
                .Select(static value => value.Groups["name"].Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            foreach (var eventName in names)
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

            var kind = ParseCollectionKind(match.Groups["kind"].Value);
            foreach (Match idMatch in IdentifierPattern.Matches(match.Groups["rest"].Value))
                kinds[idMatch.Value] = kind;
        }

        return kinds;
    }

    private static IReadOnlyDictionary<string, PreceptScalarType> BuildCollectionInnerTypes(PreceptDefinition? model, string[] lines)
    {
        if (model is not null)
        {
            return model.CollectionFields.ToDictionary(
                static field => field.Name,
                static field => field.InnerType,
                StringComparer.Ordinal);
        }

        var types = new Dictionary<string, PreceptScalarType>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var match = CollectionFieldDeclRegex.Match(line);
            if (!match.Success)
                continue;

            var innerType = ParseScalarType(match.Groups["type"].Value);
            foreach (Match idMatch in IdentifierPattern.Matches(match.Groups["rest"].Value))
                types[idMatch.Value] = innerType;
        }

        return types;
    }

    private static IReadOnlyDictionary<string, StaticValueKind> BuildFieldTypeKinds(PreceptDefinition? model, string[] lines)
    {
        if (model is not null)
        {
            return model.Fields.ToDictionary(
                static field => field.Name,
                PreceptTypeChecker.MapFieldContractKind,
                StringComparer.Ordinal);
        }

        var kinds = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var match = FieldDeclRegex.Match(line);
            if (!match.Success)
                continue;

            var kind = ParseScalarType(match.Groups["type"].Value) switch
            {
                PreceptScalarType.String => StaticValueKind.String,
                PreceptScalarType.Number => StaticValueKind.Number,
                PreceptScalarType.Boolean => StaticValueKind.Boolean,
                _ => StaticValueKind.None
            };
            foreach (Match idMatch in IdentifierPattern.Matches(match.Groups["rest"].Value))
                kinds[idMatch.Value] = kind;
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

    /// <summary>
    /// Extracts all identifiers from lines matching a regex with a "rest" capture group,
    /// filtering out keywords in the exclude set (e.g. "initial" for state declarations).
    /// </summary>
    private static string[] CollectMultiIdentifiers(string[] lines, Regex regex, HashSet<string> excludeKeywords)
        => lines
            .Select(line => regex.Match(line))
            .Where(static match => match.Success)
            .SelectMany(match => IdentifierPattern.Matches(match.Groups["rest"].Value)
                .Select(static m => m.Value)
                .Where(name => !excludeKeywords.Contains(name)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// Extracts event names from event declaration lines, handling multi-name declarations.
    /// For "event A, B with ..." extracts A and B. For "event C" extracts C.
    /// </summary>
    private static string[] CollectEventNames(string[] lines)
    {
        var names = new List<string>();
        foreach (var line in lines)
        {
            var withMatch = EventWithArgsRegex.Match(line);
            if (withMatch.Success)
            {
                names.AddRange(IdentifierPattern.Matches(withMatch.Groups["names"].Value)
                    .Select(static m => m.Value));
                continue;
            }
            var simpleMatch = EventDeclRegex.Match(line);
            if (simpleMatch.Success)
            {
                names.AddRange(IdentifierPattern.Matches(simpleMatch.Groups["rest"].Value)
                    .Select(static m => m.Value)
                    .TakeWhile(static name => !string.Equals(name, "with", StringComparison.OrdinalIgnoreCase)));
            }
        }
        return names.Distinct(StringComparer.Ordinal).OrderBy(static v => v, StringComparer.Ordinal).ToArray();
    }

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
    IReadOnlyDictionary<string, PreceptScalarType> CollectionInnerTypes,
    IReadOnlyDictionary<string, StaticValueKind> FieldTypeKinds,
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