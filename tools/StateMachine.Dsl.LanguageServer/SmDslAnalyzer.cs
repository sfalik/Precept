using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using StateMachine.Dsl;

namespace StateMachine.Dsl.LanguageServer;

internal sealed class SmDslAnalyzer
{
    private static readonly Regex LineErrorRegex = new("^Line\\s+(?<line>\\d+)\\s*:\\s*(?<message>.+)$", RegexOptions.Compiled);
    private static readonly Regex StateRegex = new("^\\s*state\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex EventRegex = new("^\\s*event\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex DataFieldRegex = new("^\\s*(?:string|number|boolean|null)\\??\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*=\\s*.+)?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventArgRegex = new("^\\s*(?:string|number|boolean|null)\\??\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*=\\s*.+)?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FromOnRegex = new("^\\s*from\\s+(?<from>any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)\\s+on\\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventMemberPrefixRegex = new("(?<event>[A-Za-z_][A-Za-z0-9_]*)\\.$", RegexOptions.Compiled);
    private static readonly Regex SetLineRegex = new("^\\s*set\\s+(?<key>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<expr>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly MethodInfo? ExpressionParseMethod = typeof(DslMachine).Assembly
        .GetType("StateMachine.Dsl.DslExpressionParser", throwOnError: false)
        ?.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

    private readonly ConcurrentDictionary<DocumentUri, string> _documents = new();

    public void SetDocumentText(DocumentUri uri, string text)
        => _documents[uri] = text;

    public void RemoveDocument(DocumentUri uri)
        => _documents.TryRemove(uri, out _);

    public bool TryGetDocumentText(DocumentUri uri, out string text)
        => _documents.TryGetValue(uri, out text!);

    public IReadOnlyList<Diagnostic> GetDiagnostics(DocumentUri uri)
    {
        if (!_documents.TryGetValue(uri, out var text))
            return Array.Empty<Diagnostic>();

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        try
        {
            var machine = StateMachineDslParser.Parse(text);
            DslWorkflowCompiler.Compile(machine);

            var diagnostics = GetSemanticDiagnostics(machine, lines);
            return diagnostics.Count == 0 ? Array.Empty<Diagnostic>() : diagnostics;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            var diagnostic = ToDiagnostic(ex.Message, lines);
            return new[] { diagnostic };
        }
    }

    public IReadOnlyList<CompletionItem> GetCompletions(DocumentUri uri, Position position)
    {
        if (!_documents.TryGetValue(uri, out var text))
            return KeywordItems;

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (position.Line >= lines.Length)
            return KeywordItems;

        var lineText = lines[(int)position.Line];
        var beforeCursor = position.Character <= lineText.Length
            ? lineText[..(int)position.Character]
            : lineText;

        var states = CollectIdentifiers(text, StateRegex);
        var events = CollectIdentifiers(text, EventRegex);
        var dataFields = CollectTopLevelDataFields(lines);
        var eventArgs = CollectEventArgs(lines);
        var currentEvent = FindCurrentEventName(lines, (int)position.Line);

        var eventMemberPrefixMatch = EventMemberPrefixRegex.Match(beforeCursor);
        if (eventMemberPrefixMatch.Success)
        {
            var eventName = eventMemberPrefixMatch.Groups["event"].Value;
            if (eventArgs.TryGetValue(eventName, out var argsForEvent))
                return BuildItems(argsForEvent, CompletionItemKind.Field);
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*(?:if|else\\s+if)\\s+[^\\n]*$", RegexOptions.IgnoreCase))
        {
            var guardItems = BuildGuardCompletions(dataFields, currentEvent, eventArgs);
            return DistinctAndSort(guardItems.Concat(GuardSnippetItems));
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*set\\s+[^=\\n]*$", RegexOptions.IgnoreCase) &&
            !beforeCursor.Contains('=', StringComparison.Ordinal))
            return DistinctAndSort(BuildItems(dataFields, CompletionItemKind.Field).Concat(SetSnippetItems));

        if (Regex.IsMatch(beforeCursor, "^\\s*set\\s+[A-Za-z_][A-Za-z0-9_]*\\s*=\\s*[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildExpressionCompletions(dataFields, currentEvent, eventArgs);

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*\\s+on(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        if (Regex.IsMatch(beforeCursor, "^\\s*from\\s+[^\\n]*$", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(beforeCursor, "\\s+on\\s+", RegexOptions.IgnoreCase))
        {
            var stateItems = BuildItems(states.Append("any"), CompletionItemKind.EnumMember);
            return stateItems.Concat(KeywordItems.Where(item => item.Label == "on")).ToArray();
        }

        if (Regex.IsMatch(beforeCursor, "^\\s*transition(?:\\s+[^\\n]*)?$", RegexOptions.IgnoreCase))
            return DistinctAndSort(BuildItems(states, CompletionItemKind.EnumMember).Concat(TransitionSnippetItems));

        if (Regex.IsMatch(beforeCursor, "^\\s*inspect\\s+[^\\n]*$", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(beforeCursor, "^\\s*fire\\s+[^\\n]*$", RegexOptions.IgnoreCase))
            return BuildItems(events, CompletionItemKind.Event);

        return DistinctAndSort(KeywordItems.Concat(GlobalSnippetItems));
    }

    private static IReadOnlyList<CompletionItem> BuildGuardCompletions(
        IReadOnlyList<string> dataFields,
        string? currentEvent,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(ExpressionOperatorItems);
        items.AddRange(LiteralItems);

        if (!string.IsNullOrWhiteSpace(currentEvent) && eventArgs.TryGetValue(currentEvent, out var argsForEvent) && argsForEvent.Count > 0)
        {
            items.Add(new CompletionItem
            {
                Label = currentEvent + ".",
                Kind = CompletionItemKind.Module
            });

            items.AddRange(BuildItems(argsForEvent.Select(arg => currentEvent + "." + arg), CompletionItemKind.Field));
        }

        return DistinctAndSort(items);
    }

    private static IReadOnlyList<CompletionItem> BuildExpressionCompletions(
        IReadOnlyList<string> dataFields,
        string? currentEvent,
        IReadOnlyDictionary<string, IReadOnlyList<string>> eventArgs)
    {
        var items = new List<CompletionItem>();
        items.AddRange(BuildItems(dataFields, CompletionItemKind.Variable));
        items.AddRange(ExpressionOperatorItems);
        items.AddRange(LiteralItems);

        if (!string.IsNullOrWhiteSpace(currentEvent) && eventArgs.TryGetValue(currentEvent, out var argsForEvent) && argsForEvent.Count > 0)
        {
            items.Add(new CompletionItem
            {
                Label = currentEvent + ".",
                Kind = CompletionItemKind.Module
            });

            items.AddRange(BuildItems(argsForEvent.Select(arg => currentEvent + "." + arg), CompletionItemKind.Field));
        }

        return DistinctAndSort(items);
    }

    private static IReadOnlyList<string> CollectTopLevelDataFields(string[] lines)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        var inEventArgs = false;
        var eventIndent = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                continue;

            var indent = raw.Length - raw.TrimStart().Length;

            if (Regex.IsMatch(trimmed, "^event\\s+[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.IgnoreCase))
            {
                inEventArgs = true;
                eventIndent = indent;
                continue;
            }

            if (inEventArgs && indent > eventIndent)
                continue;

            inEventArgs = false;

            var match = DataFieldRegex.Match(raw);
            if (match.Success)
                fields.Add(match.Groups["name"].Value);
        }

        return fields.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CollectEventArgs(string[] lines)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var currentEvent = string.Empty;
        var currentIndent = 0;
        var currentArgs = new List<string>();
        var inEvent = false;

        void FlushCurrentEvent()
        {
            if (inEvent && !string.IsNullOrWhiteSpace(currentEvent))
                result[currentEvent] = currentArgs.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                continue;

            var eventMatch = EventRegex.Match(raw);
            if (eventMatch.Success)
            {
                FlushCurrentEvent();
                currentEvent = eventMatch.Groups["name"].Value;
                currentIndent = raw.Length - raw.TrimStart().Length;
                currentArgs = new List<string>();
                inEvent = true;
                continue;
            }

            if (!inEvent)
                continue;

            var indent = raw.Length - raw.TrimStart().Length;
            if (indent <= currentIndent)
            {
                FlushCurrentEvent();
                inEvent = false;
                currentEvent = string.Empty;
                currentArgs = new List<string>();
                continue;
            }

            var argMatch = EventArgRegex.Match(raw);
            if (argMatch.Success)
                currentArgs.Add(argMatch.Groups["name"].Value);
        }

        FlushCurrentEvent();
        return result;
    }

    private static string? FindCurrentEventName(string[] lines, int lineIndex)
    {
        var start = Math.Min(Math.Max(lineIndex, 0), lines.Length - 1);
        for (var i = start; i >= 0; i--)
        {
            var match = FromOnRegex.Match(lines[i]);
            if (match.Success)
                return match.Groups["event"].Value;
        }

        return null;
    }

    private static IReadOnlyList<CompletionItem> DistinctAndSort(IEnumerable<CompletionItem> items)
        => items
            .GroupBy(item => item.Label, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(item => item.Label, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<Diagnostic> GetSemanticDiagnostics(DslMachine machine, string[] lines)
    {
        var diagnostics = new List<Diagnostic>();
        var dataFieldKinds = machine.DataFields.ToDictionary(
            field => field.Name,
            MapFieldContractKind,
            StringComparer.Ordinal);

        var eventArgKinds = machine.Events.ToDictionary(
            evt => evt.Name,
            evt => evt.Args.ToDictionary(
                arg => arg.Name,
                MapFieldContractKind,
                StringComparer.Ordinal),
            StringComparer.Ordinal);

        var searchLine = 0;
        foreach (var transition in machine.Transitions)
        {
            var transitionSymbols = BuildSymbolKinds(dataFieldKinds, eventArgKinds, transition.EventName);
            IReadOnlyDictionary<string, StaticValueKind> setSymbols = transitionSymbols;

            if (!string.IsNullOrWhiteSpace(transition.GuardExpression))
            {
                var guardLine = FindGuardLine(lines, ref searchLine, transition.GuardExpression!);
                ValidateExpression(
                    transition.GuardExpression!,
                    guardLine,
                    transitionSymbols,
                    expectedKind: StaticValueKind.Boolean,
                    expectedLabel: "guard expression",
                    diagnostics,
                    lines);

                if (TryParseExpression(transition.GuardExpression!, out var parsedGuard, out _))
                    setSymbols = ApplyNarrowing(parsedGuard!, transitionSymbols, assumeTrue: true);
            }

            foreach (var assignment in transition.SetAssignments)
            {
                var assignmentLine = FindSetLine(lines, ref searchLine, assignment.Key, assignment.ExpressionText);
                if (!dataFieldKinds.TryGetValue(assignment.Key, out var targetKind))
                    continue;

                ValidateExpression(
                    assignment.ExpressionText,
                    assignmentLine,
                    setSymbols,
                    expectedKind: targetKind,
                    expectedLabel: $"set target '{assignment.Key}'",
                    diagnostics,
                    lines);
            }
        }

        foreach (var terminalRule in machine.TerminalRules)
        {
            var terminalSymbols = BuildSymbolKinds(dataFieldKinds, eventArgKinds, terminalRule.EventName);
            IReadOnlyDictionary<string, StaticValueKind> terminalSetSymbols = terminalSymbols;

            if (!string.IsNullOrWhiteSpace(terminalRule.GuardExpression))
            {
                var guardLine = FindGuardLine(lines, ref searchLine, terminalRule.GuardExpression!);
                ValidateExpression(
                    terminalRule.GuardExpression!,
                    guardLine,
                    terminalSymbols,
                    expectedKind: StaticValueKind.Boolean,
                    expectedLabel: "guard expression",
                    diagnostics,
                    lines);

                if (TryParseExpression(terminalRule.GuardExpression!, out var parsedGuard, out _))
                    terminalSetSymbols = ApplyNarrowing(parsedGuard!, terminalSymbols, assumeTrue: true);
            }

            if (terminalRule.SetAssignments is not null)
            {
                foreach (var assignment in terminalRule.SetAssignments)
                {
                    var assignmentLine = FindSetLine(lines, ref searchLine, assignment.Key, assignment.ExpressionText);
                    if (!dataFieldKinds.TryGetValue(assignment.Key, out var targetKind))
                        continue;

                    ValidateExpression(
                        assignment.ExpressionText,
                        assignmentLine,
                        terminalSetSymbols,
                        expectedKind: targetKind,
                        expectedLabel: $"set target '{assignment.Key}'",
                        diagnostics,
                        lines);
                }
            }
        }

        if (machine.Events.Count == 0)
        {
            var machineLine = 0;
            for (var li = 0; li < lines.Length; li++)
            {
                if (lines[li].TrimStart().StartsWith("machine ", StringComparison.Ordinal))
                {
                    machineLine = li;
                    break;
                }
            }

            var lineLength = machineLine < lines.Length ? lines[machineLine].Length : 1;
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Hint,
                Message = "Machine has no events declared. It cannot respond to any input.",
                Source = "state-machine-dsl",
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(machineLine, 0),
                    new Position(machineLine, Math.Max(1, lineLength)))
            });
        }

        return diagnostics;
    }

    private static Dictionary<string, StaticValueKind> BuildSymbolKinds(
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        string eventName)
    {
        var symbols = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal);
        foreach (var pair in dataFieldKinds)
            symbols[pair.Key] = pair.Value;

        if (eventArgKinds.TryGetValue(eventName, out var eventArgs))
        {
            foreach (var pair in eventArgs)
                symbols[$"{eventName}.{pair.Key}"] = pair.Value;
        }

        return symbols;
    }

    private static void ValidateExpression(
        string expressionText,
        int lineIndex,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        StaticValueKind expectedKind,
        string expectedLabel,
        List<Diagnostic> diagnostics,
        string[] lines)
    {
        if (!TryParseExpression(expressionText, out var parsedExpression, out var parseError))
        {
            diagnostics.Add(CreateLineDiagnostic(lines, lineIndex, parseError));
            return;
        }

        if (!TryInferKind(parsedExpression!, symbols, out var actualKind, out var semanticError))
        {
            diagnostics.Add(CreateLineDiagnostic(lines, lineIndex, semanticError));
            return;
        }

        if (!IsAssignable(actualKind, expectedKind))
        {
            diagnostics.Add(CreateLineDiagnostic(
                lines,
                lineIndex,
                $"{expectedLabel} type mismatch: expected {FormatKinds(expectedKind)} but expression produces {FormatKinds(actualKind)}."));
        }
    }

    private static bool TryParseExpression(string expression, out DslExpression? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;

        if (ExpressionParseMethod is null)
        {
            error = "expression analyzer unavailable in language server.";
            return false;
        }

        try
        {
            parsed = ExpressionParseMethod.Invoke(null, [expression]) as DslExpression;
            if (parsed is null)
            {
                error = "expression parse failed.";
                return false;
            }

            return true;
        }
        catch (TargetInvocationException tie)
        {
            error = tie.InnerException?.Message ?? tie.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryInferKind(
        DslExpression expression,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        out StaticValueKind kind,
        out string error)
    {
        kind = StaticValueKind.None;
        error = string.Empty;

        switch (expression)
        {
            case DslLiteralExpression literal:
                kind = MapLiteralKind(literal.Value);
                return true;

            case DslIdentifierExpression identifier:
            {
                var key = identifier.Member is null ? identifier.Name : $"{identifier.Name}.{identifier.Member}";
                if (!symbols.TryGetValue(key, out var symbolKind))
                {
                    error = $"unknown identifier '{key}'.";
                    return false;
                }

                kind = symbolKind;
                return true;
            }

            case DslParenthesizedExpression parenthesized:
                return TryInferKind(parenthesized.Inner, symbols, out kind, out error);

            case DslUnaryExpression unary:
            {
                if (!TryInferKind(unary.Operand, symbols, out var operandKind, out error))
                    return false;

                if (unary.Operator == "!")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Boolean))
                    {
                        error = "operator '!' requires boolean operand.";
                        return false;
                    }

                    kind = StaticValueKind.Boolean;
                    return true;
                }

                if (unary.Operator == "-")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Number))
                    {
                        error = "unary '-' requires numeric operand.";
                        return false;
                    }

                    kind = StaticValueKind.Number;
                    return true;
                }

                error = $"unsupported unary operator '{unary.Operator}'.";
                return false;
            }

            case DslBinaryExpression binary:
            {
                switch (binary.Operator)
                {
                    case "&&":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!IsExactly(leftKind, StaticValueKind.Boolean))
                        {
                            error = "operator '&&' requires boolean operands.";
                            return false;
                        }

                        var rightSymbols = ApplyNarrowing(binary.Left, symbols, assumeTrue: true);
                        if (!TryInferKind(binary.Right, rightSymbols, out var rightKind, out error))
                            return false;

                        if (!IsExactly(rightKind, StaticValueKind.Boolean))
                        {
                            error = "operator '&&' requires boolean operands.";
                            return false;
                        }

                        kind = StaticValueKind.Boolean;
                        return true;
                    }

                    case "||":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!IsExactly(leftKind, StaticValueKind.Boolean))
                        {
                            error = "operator '||' requires boolean operands.";
                            return false;
                        }

                        var rightSymbols = ApplyNarrowing(binary.Left, symbols, assumeTrue: false);
                        if (!TryInferKind(binary.Right, rightSymbols, out var rightKind, out error))
                            return false;

                        if (!IsExactly(rightKind, StaticValueKind.Boolean))
                        {
                            error = "operator '||' requires boolean operands.";
                            return false;
                        }

                        kind = StaticValueKind.Boolean;
                        return true;
                    }

                    case "+":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!TryInferKind(binary.Right, symbols, out var rightKind, out error))
                            return false;

                        var stringCandidate = IsExactly(leftKind, StaticValueKind.String) && IsExactly(rightKind, StaticValueKind.String);
                        var numberCandidate = IsExactly(leftKind, StaticValueKind.Number) && IsExactly(rightKind, StaticValueKind.Number);

                        if (stringCandidate && !numberCandidate)
                        {
                            kind = StaticValueKind.String;
                            return true;
                        }

                        if (numberCandidate && !stringCandidate)
                        {
                            kind = StaticValueKind.Number;
                            return true;
                        }

                        error = "operator '+' requires number+number or string+string.";
                        return false;
                    }

                    case "-":
                    case "*":
                    case "/":
                    case "%":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!TryInferKind(binary.Right, symbols, out var rightKind, out error))
                            return false;

                        if (!IsExactly(leftKind, StaticValueKind.Number) || !IsExactly(rightKind, StaticValueKind.Number))
                        {
                            error = $"operator '{binary.Operator}' requires numeric operands.";
                            return false;
                        }

                        kind = StaticValueKind.Number;
                        return true;
                    }

                    case ">":
                    case ">=":
                    case "<":
                    case "<=":
                    {
                        if (!TryInferKind(binary.Left, symbols, out var leftKind, out error))
                            return false;

                        if (!TryInferKind(binary.Right, symbols, out var rightKind, out error))
                            return false;

                        if (!IsExactly(leftKind, StaticValueKind.Number) || !IsExactly(rightKind, StaticValueKind.Number))
                        {
                            error = $"operator '{binary.Operator}' requires numeric operands.";
                            return false;
                        }

                        kind = StaticValueKind.Boolean;
                        return true;
                    }

                    case "==":
                    case "!=":
                        if (!TryInferKind(binary.Left, symbols, out _, out error))
                            return false;

                        if (!TryInferKind(binary.Right, symbols, out _, out error))
                            return false;

                        kind = StaticValueKind.Boolean;
                        return true;

                    default:
                        error = $"unsupported binary operator '{binary.Operator}'.";
                        return false;
                }
            }

            default:
                error = "unsupported expression node.";
                return false;
        }
    }

    private static IReadOnlyDictionary<string, StaticValueKind> ApplyNarrowing(
        DslExpression expression,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        bool assumeTrue)
    {
        expression = StripParentheses(expression);

        if (expression is DslUnaryExpression { Operator: "!" } unary)
            return ApplyNarrowing(unary.Operand, symbols, !assumeTrue);

        if (expression is DslBinaryExpression binary)
        {
            if (binary.Operator == "&&")
            {
                if (assumeTrue)
                {
                    var leftNarrowed = ApplyNarrowing(binary.Left, symbols, assumeTrue: true);
                    return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: true);
                }

                return symbols;
            }

            if (binary.Operator == "||")
            {
                if (!assumeTrue)
                {
                    var leftNarrowed = ApplyNarrowing(binary.Left, symbols, assumeTrue: false);
                    return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: false);
                }

                return symbols;
            }

            if (binary.Operator is "==" or "!=" &&
                TryApplyNullComparisonNarrowing(binary, symbols, assumeTrue, out var narrowed))
                return narrowed;
        }

        return symbols;
    }

    private static bool TryApplyNullComparisonNarrowing(
        DslBinaryExpression binary,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        bool assumeTrue,
        out IReadOnlyDictionary<string, StaticValueKind> narrowed)
    {
        narrowed = symbols;

        if (!TryGetIdentifierKey(binary.Left, out var leftKey) && !TryGetIdentifierKey(binary.Right, out leftKey))
            return false;

        var leftIsNull = IsNullLiteral(binary.Left);
        var rightIsNull = IsNullLiteral(binary.Right);
        if (!leftIsNull && !rightIsNull)
            return false;

        if (!symbols.TryGetValue(leftKey, out var existingKind))
            return false;

        var expectsNull = binary.Operator switch
        {
            "==" => assumeTrue,
            "!=" => !assumeTrue,
            _ => false
        };

        var updatedKind = expectsNull
            ? StaticValueKind.Null
            : (existingKind & ~StaticValueKind.Null);

        var updated = new Dictionary<string, StaticValueKind>(symbols, StringComparer.Ordinal)
        {
            [leftKey] = updatedKind
        };

        narrowed = updated;
        return true;
    }

    private static bool TryGetIdentifierKey(DslExpression expression, out string key)
    {
        var stripped = StripParentheses(expression);
        if (stripped is DslIdentifierExpression identifier)
        {
            key = identifier.Member is null
                ? identifier.Name
                : $"{identifier.Name}.{identifier.Member}";
            return true;
        }

        key = string.Empty;
        return false;
    }

    private static DslExpression StripParentheses(DslExpression expression)
    {
        while (expression is DslParenthesizedExpression parenthesized)
            expression = parenthesized.Inner;

        return expression;
    }

    private static bool IsNullLiteral(DslExpression expression)
        => StripParentheses(expression) is DslLiteralExpression { Value: null };

    private static bool IsExactly(StaticValueKind kind, StaticValueKind expected)
        => kind == expected;

    private static bool IsAssignable(StaticValueKind actual, StaticValueKind expected)
    {
        var actualNonNull = actual & ~StaticValueKind.Null;
        var expectedNonNull = expected & ~StaticValueKind.Null;

        if (!HasFlag(expected, StaticValueKind.Null) && HasFlag(actual, StaticValueKind.Null))
            return false;

        if ((actualNonNull & ~expectedNonNull) != StaticValueKind.None)
            return false;

        if (actual == StaticValueKind.Null)
            return HasFlag(expected, StaticValueKind.Null);

        return true;
    }

    private static int FindGuardLine(string[] lines, ref int searchLine, string guardExpression)
    {
        for (var i = Math.Max(0, searchLine); i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("if ", StringComparison.Ordinal))
            {
                var candidate = trimmed[3..].Trim();
                if (Normalized(candidate) == Normalized(guardExpression))
                {
                    searchLine = i + 1;
                    return i;
                }
            }

            if (trimmed.StartsWith("else if ", StringComparison.Ordinal))
            {
                var candidate = trimmed[8..].Trim();
                if (Normalized(candidate) == Normalized(guardExpression))
                {
                    searchLine = i + 1;
                    return i;
                }
            }
        }

        return 0;
    }

    private static int FindSetLine(string[] lines, ref int searchLine, string key, string expression)
    {
        for (var i = Math.Max(0, searchLine); i < lines.Length; i++)
        {
            var match = SetLineRegex.Match(lines[i]);
            if (!match.Success)
                continue;

            var candidateKey = match.Groups["key"].Value;
            var candidateExpr = match.Groups["expr"].Value.Trim();
            if (candidateKey == key && Normalized(candidateExpr) == Normalized(expression))
            {
                searchLine = i + 1;
                return i;
            }
        }

        return 0;
    }

    private static string Normalized(string text)
        => Regex.Replace(text.Trim(), "\\s+", " ");

    private static Diagnostic CreateLineDiagnostic(string[] lines, int lineIndex, string message)
    {
        var safeLineIndex = Math.Min(Math.Max(lineIndex, 0), Math.Max(lines.Length - 1, 0));
        var lineLength = safeLineIndex < lines.Length ? lines[safeLineIndex].Length : 1;
        return new Diagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = message,
            Source = "state-machine-dsl",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(safeLineIndex, 0),
                new Position(safeLineIndex, Math.Max(1, lineLength)))
        };
    }

    private static StaticValueKind MapFieldContractKind(DslFieldContract field)
    {
        var kind = field.Type switch
        {
            DslScalarType.String => StaticValueKind.String,
            DslScalarType.Number => StaticValueKind.Number,
            DslScalarType.Boolean => StaticValueKind.Boolean,
            DslScalarType.Null => StaticValueKind.Null,
            _ => StaticValueKind.None
        };

        if (field.IsNullable)
            kind |= StaticValueKind.Null;

        return kind;
    }

    private static StaticValueKind MapLiteralKind(object? value)
        => value switch
        {
            null => StaticValueKind.Null,
            string => StaticValueKind.String,
            bool => StaticValueKind.Boolean,
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => StaticValueKind.Number,
            _ => StaticValueKind.None
        };

    private static bool HasFlag(StaticValueKind kind, StaticValueKind flag)
        => (kind & flag) == flag;

    private static string FormatKinds(StaticValueKind kinds)
    {
        if (kinds == StaticValueKind.None)
            return "none";

        var labels = new List<string>();
        if (HasFlag(kinds, StaticValueKind.String)) labels.Add("string");
        if (HasFlag(kinds, StaticValueKind.Number)) labels.Add("number");
        if (HasFlag(kinds, StaticValueKind.Boolean)) labels.Add("boolean");
        if (HasFlag(kinds, StaticValueKind.Null)) labels.Add("null");
        return string.Join("|", labels);
    }

    private static IReadOnlyList<string> CollectIdentifiers(string text, Regex regex)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var match = regex.Match(line);
            if (match.Success)
                names.Add(match.Groups["name"].Value);
        }

        return names.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static Diagnostic ToDiagnostic(string message, string[] lines)
    {
        var match = LineErrorRegex.Match(message);
        if (match.Success && int.TryParse(match.Groups["line"].Value, out var lineNumber))
        {
            var lineIndex = Math.Max(0, lineNumber - 1);
            var lineLength = lineIndex < lines.Length ? lines[lineIndex].Length : 1;
            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = match.Groups["message"].Value,
                Source = "state-machine-dsl",
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(lineIndex, 0),
                    new Position(lineIndex, Math.Max(1, lineLength)))
            };
        }

        return new Diagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = message,
            Source = "state-machine-dsl",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(0, 0),
                new Position(0, 1))
        };
    }

    private static IReadOnlyList<CompletionItem> BuildItems(IEnumerable<string> labels, CompletionItemKind kind)
        => labels
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .Select(label => new CompletionItem
            {
                Label = label,
                Kind = kind
            })
            .ToArray();

    private static readonly IReadOnlyList<CompletionItem> KeywordItems =
    [
        new CompletionItem { Label = "machine", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "state", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "initial", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "event", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "from", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "on", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "if", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "else if", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "else", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "transition", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "set", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "reject", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "no transition", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "string", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "number", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "boolean", Kind = CompletionItemKind.Keyword },
        new CompletionItem { Label = "null", Kind = CompletionItemKind.Keyword }
    ];

    private static readonly IReadOnlyList<CompletionItem> ExpressionOperatorItems =
    [
        new CompletionItem { Label = "+", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "-", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "*", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "/", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "%", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "==", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "!=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = ">", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = ">=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "<", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "<=", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "&&", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "||", Kind = CompletionItemKind.Operator },
        new CompletionItem { Label = "!", Kind = CompletionItemKind.Operator }
    ];

    private static readonly IReadOnlyList<CompletionItem> LiteralItems =
    [
        new CompletionItem { Label = "true", Kind = CompletionItemKind.Constant },
        new CompletionItem { Label = "false", Kind = CompletionItemKind.Constant },
        new CompletionItem { Label = "null", Kind = CompletionItemKind.Constant }
    ];

    private static readonly IReadOnlyList<CompletionItem> GlobalSnippetItems =
    [
        SnippetItem("from/on block", "from ${1:any} on ${2:Event}\n  ${3:transition ${4:State}}", "DSL block"),
        SnippetItem("if transition", "if ${1:condition}\n  transition ${2:State}", "Guard branch"),
        SnippetItem("else if transition", "else if ${1:condition}\n  transition ${2:State}", "Guard branch"),
        SnippetItem("else reject", "else\n  reject \"${1:Reason}\"", "Fallback branch")
    ];

    private static readonly IReadOnlyList<CompletionItem> GuardSnippetItems =
    [
        SnippetItem("if ... transition", "if ${1:condition}\n  transition ${2:State}", "Guard branch"),
        SnippetItem("if ... no transition", "if ${1:condition}\n  no transition", "Guard branch"),
        SnippetItem("else if ... transition", "else if ${1:condition}\n  transition ${2:State}", "Guard branch"),
        SnippetItem("else ... reject", "else\n  reject \"${1:Reason}\"", "Fallback branch")
    ];

    private static readonly IReadOnlyList<CompletionItem> SetSnippetItems =
    [
        SnippetItem("set assignment", "set ${1:Field} = ${2:value}", "Data assignment")
    ];

    private static readonly IReadOnlyList<CompletionItem> TransitionSnippetItems =
    [
        SnippetItem("transition state", "transition ${1:State}", "Transition"),
        SnippetItem("no transition", "no transition", "Terminal outcome"),
        SnippetItem("reject reason", "reject \"${1:Reason}\"", "Terminal outcome")
    ];

    private static CompletionItem SnippetItem(string label, string snippet, string detail)
        => new()
        {
            Label = label,
            Kind = CompletionItemKind.Snippet,
            InsertText = snippet,
            InsertTextFormat = InsertTextFormat.Snippet,
            Detail = detail
        };

    [Flags]
    private enum StaticValueKind
    {
        None = 0,
        String = 1,
        Number = 2,
        Boolean = 4,
        Null = 8
    }
}
