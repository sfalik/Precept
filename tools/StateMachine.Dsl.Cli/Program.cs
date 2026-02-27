using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Spectre.Console;
using StateMachine.Dsl;

const int ExitSuccess = 0;
const int ExitInvalidUsage = 1;
const int ExitInputFileNotFound = 2;
const int ExitUnhandledError = 4;
const int ExitIncompatibleInstance = 5;
const int ExitScriptFailed = 6;

if (args.Length < 1)
{
    PrintUsage();
    return ExitInvalidUsage;
}

string inputPath = args[0];
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file '{inputPath}' does not exist.");
    return ExitInputFileNotFound;
}

if (!TryParseOutputMode(GetOption(args, "--output"), out var outputMode))
{
    Console.Error.WriteLine("--output must be one of: compact, verbose, json.");
    PrintUsage();
    return ExitInvalidUsage;
}

bool scriptEcho = HasFlag(args, "--echo");
bool noColor = HasFlag(args, "--no-color");
bool forceUnicode = HasFlag(args, "--unicode");
bool forceAscii = HasFlag(args, "--ascii");
if (forceUnicode && forceAscii)
{
    Console.Error.WriteLine("Use only one of --unicode or --ascii.");
    PrintUsage();
    return ExitInvalidUsage;
}

var symbolMode = forceUnicode
    ? SymbolMode.Unicode
    : forceAscii
        ? SymbolMode.Ascii
        : SymbolMode.Auto;
var renderer = new CliRenderer(!noColor && !Console.IsOutputRedirected);

string text = File.ReadAllText(inputPath);

try
{
    var machine = StateMachineDslParser.Parse(text);
    var workflow = DslWorkflowCompiler.Compile(machine);
    string? instancePath = GetOption(args, "--instance");

    if (string.IsNullOrWhiteSpace(instancePath))
    {
        Console.Error.WriteLine("--instance is required.");
        PrintUsage();
        return ExitInvalidUsage;
    }

    var sessionInstance = LoadInstance(instancePath);
    var compatibility = workflow.CheckCompatibility(sessionInstance);
    if (!compatibility.IsCompatible)
    {
        Console.Error.WriteLine(compatibility.Reason ?? "Instance is not compatible with this workflow.");
        return ExitIncompatibleInstance;
    }

    string? scriptPath = GetOption(args, "--script");
    if (!string.IsNullOrWhiteSpace(scriptPath))
    {
        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"Script file '{scriptPath}' does not exist.");
            return ExitInputFileNotFound;
        }

        var lines = File.ReadAllLines(scriptPath);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (scriptEcho)
                renderer.Meta($"sm> {line}");

            var exec = ExecuteReplCommand(line, workflow, ref sessionInstance, ref instancePath, renderer, ref outputMode, ref symbolMode, isInteractive: false);
            if (!exec.IsSuccess)
                return ExitScriptFailed;
            if (exec.ShouldExit)
                break;
        }

        return ExitSuccess;
    }

    renderer.Meta($"Machine: {machine.Name}");
    renderer.Meta($"State: {sessionInstance.CurrentState}");
    renderer.Meta("Type 'help' for commands, 'exit' to quit.");

    while (true)
    {
        renderer.Prompt("sm> ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            continue;

        var exec = ExecuteReplCommand(input, workflow, ref sessionInstance, ref instancePath, renderer, ref outputMode, ref symbolMode, isInteractive: true);
        if (!exec.IsSuccess)
            renderer.Warning("Command failed.");
        if (exec.ShouldExit)
            return ExitSuccess;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return ExitUnhandledError;
}

static string? GetOption(string[] args, string option)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(option, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static bool HasFlag(string[] args, string option)
{
    return args.Any(a => a.Equals(option, StringComparison.OrdinalIgnoreCase));
}

static bool TryParseOutputMode(string? value, out OutputMode mode)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        mode = OutputMode.Compact;
        return true;
    }

    return Enum.TryParse(value, true, out mode);
}

static void PrintUsage()
{
    Console.WriteLine("StateMachine.Dsl.Cli");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dsl <file.sm> --instance <instance.json> [--script <commands.txt>] [--output compact|verbose|json] [--echo] [--no-color] [--unicode|--ascii]");
    Console.WriteLine("Notes:");
    Console.WriteLine("  Without --script, starts interactive REPL.");
    Console.WriteLine("  With --script, runs REPL commands from file non-interactively.");
    Console.WriteLine("  --output controls result formatting (default: compact).");
    Console.WriteLine("  --echo prints script commands while running script mode.");
    Console.WriteLine("  --no-color disables ANSI coloring.");
    Console.WriteLine("  symbols default to auto detection.");
    Console.WriteLine("  --unicode forces Unicode symbols in compact output.");
    Console.WriteLine("  --ascii forces ASCII symbols in compact output.");
    Console.WriteLine("Exit codes:");
    Console.WriteLine($"  {ExitSuccess}: success");
    Console.WriteLine($"  {ExitInvalidUsage}: invalid usage");
    Console.WriteLine($"  {ExitInputFileNotFound}: input file not found");
    Console.WriteLine($"  {ExitUnhandledError}: unhandled error");
    Console.WriteLine($"  {ExitIncompatibleInstance}: incompatible instance/workflow");
    Console.WriteLine($"  {ExitScriptFailed}: script command failed");
}

static DslWorkflowInstance LoadInstance(string path)
{
    if (!File.Exists(path))
        throw new InvalidOperationException($"Instance file '{path}' does not exist.");

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    var root = doc.RootElement;
    if (root.ValueKind != JsonValueKind.Object)
        throw new InvalidOperationException("Instance JSON must be an object.");

    string workflowName = root.GetProperty("workflowName").GetString() ?? string.Empty;
    string currentState = root.GetProperty("currentState").GetString() ?? string.Empty;
    string? lastEvent = root.TryGetProperty("lastEvent", out var lastEventElement) && lastEventElement.ValueKind != JsonValueKind.Null
        ? lastEventElement.GetString()
        : null;

    DateTimeOffset updatedAt = root.TryGetProperty("updatedAt", out var updatedAtElement) && updatedAtElement.ValueKind == JsonValueKind.String
        ? updatedAtElement.GetDateTimeOffset()
        : DateTimeOffset.UtcNow;

    var instanceData = new Dictionary<string, object?>(StringComparer.Ordinal);
    if (root.TryGetProperty("instanceData", out var instanceDataElement) && instanceDataElement.ValueKind == JsonValueKind.Object)
    {
        foreach (var property in instanceDataElement.EnumerateObject())
            instanceData[property.Name] = ToDotNetValue(property.Value);
    }

    return new DslWorkflowInstance(workflowName, currentState, lastEvent, updatedAt, instanceData);
}

static void SaveInstance(string path, DslWorkflowInstance instance)
{
    var envelope = new Dictionary<string, object?>
    {
        ["workflowName"] = instance.WorkflowName,
        ["currentState"] = instance.CurrentState,
        ["lastEvent"] = instance.LastEvent,
        ["updatedAt"] = instance.UpdatedAt,
        ["instanceData"] = instance.InstanceData
    };

    var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    File.WriteAllText(path, json);
}

static IReadOnlyDictionary<string, object?> ParseEventArguments(string? eventArgsOption, string? eventArgsFileOption)
{
    bool hasInlineEventArgs = !string.IsNullOrWhiteSpace(eventArgsOption);
    bool hasEventArgsFile = !string.IsNullOrWhiteSpace(eventArgsFileOption);

    if (hasInlineEventArgs && hasEventArgsFile)
        throw new InvalidOperationException("Use either inline event arguments or an event-args file, not both.");

    if (!hasInlineEventArgs && !hasEventArgsFile)
        return new Dictionary<string, object?>(StringComparer.Ordinal);

    var eventArgsText = hasEventArgsFile
        ? File.ReadAllText(eventArgsFileOption!)
        : eventArgsOption!;

    using var doc = JsonDocument.Parse(eventArgsText);
    if (doc.RootElement.ValueKind != JsonValueKind.Object)
        throw new InvalidOperationException("Event arguments must be a JSON object.");

    var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var property in doc.RootElement.EnumerateObject())
        dictionary[property.Name] = ToDotNetValue(property.Value);

    return dictionary;
}

static object? ToDotNetValue(JsonElement value)
{
    return value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => value.TryGetInt64(out var i) ? i : value.GetDouble(),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Object => value.GetRawText(),
        JsonValueKind.Array => value.GetRawText(),
        _ => value.GetRawText()
    };
}

static ReplExecutionResult ExecuteReplCommand(
    string input,
    DslWorkflowDefinition workflow,
    ref DslWorkflowInstance sessionInstance,
    ref string? sessionInstancePath,
    CliRenderer renderer,
    ref OutputMode outputMode,
    ref SymbolMode symbolMode,
    bool isInteractive)
{
    var tokens = Tokenize(input);
    if (tokens.Count == 0)
        return ReplExecutionResult.Success();

    var command = tokens[0].ToLowerInvariant();

    if (command is "exit" or "quit")
        return ReplExecutionResult.Exit();

    if (command == "help")
    {
        PrintReplHelp(renderer);
        return ReplExecutionResult.Success();
    }

    if (command == "output")
    {
        if (tokens.Count < 2)
        {
            renderer.Info($"output: {outputMode.ToString().ToLowerInvariant()}");
            return ReplExecutionResult.Success();
        }

        if (!TryParseOutputMode(tokens[1], out var mode))
        {
            renderer.Warning("Usage: output <compact|verbose|json>");
            return ReplExecutionResult.Failed();
        }

        outputMode = mode;
        renderer.Success($"Output mode set to {mode.ToString().ToLowerInvariant()}");
        return ReplExecutionResult.Success();
    }

    if (command == "symbols")
    {
        if (tokens.Count < 2)
        {
            var resolvedMode = ResolveSymbolMode(symbolMode);
            renderer.Info($"symbols: {symbolMode.ToString().ToLowerInvariant()} (resolved: {resolvedMode.ToString().ToLowerInvariant()})");
            return ReplExecutionResult.Success();
        }

        if (tokens[1].Equals("test", StringComparison.OrdinalIgnoreCase))
        {
            RenderSymbolsTest(renderer);
            return ReplExecutionResult.Success();
        }

        if (!Enum.TryParse(tokens[1], true, out SymbolMode mode))
        {
            renderer.Warning("Usage: symbols <auto|ascii|unicode|test>");
            return ReplExecutionResult.Failed();
        }

        symbolMode = mode;
        renderer.Success($"Symbols set to {mode.ToString().ToLowerInvariant()}");
        return ReplExecutionResult.Success();
    }

    if (command == "state")
    {
        if (outputMode == OutputMode.Json)
        {
            renderer.Json(new { kind = "state", state = sessionInstance.CurrentState });
            return ReplExecutionResult.Success();
        }

        renderer.Info(sessionInstance.CurrentState);
        return ReplExecutionResult.Success();
    }

    if (command == "events")
    {
        if (outputMode == OutputMode.Json)
        {
            renderer.Json(new { kind = "events", events = workflow.Events.Select(e => e.Name).ToArray() });
            return ReplExecutionResult.Success();
        }

        foreach (var evt in workflow.Events)
            renderer.Info(evt.Name);
        return ReplExecutionResult.Success();
    }

    if (command == "data")
    {
        if (outputMode == OutputMode.Json || !isInteractive)
        {
            renderer.Json(sessionInstance.InstanceData);
        }
        else
        {
            RenderData(sessionInstance.InstanceData, renderer);
        }

        return ReplExecutionResult.Success();
    }

    if (command == "load")
    {
        if (tokens.Count < 2)
        {
            renderer.Warning("Usage: load <path>");
            return ReplExecutionResult.Failed();
        }

        var loaded = LoadInstance(tokens[1]);
        var compatibility = workflow.CheckCompatibility(loaded);
        if (!compatibility.IsCompatible)
        {
            renderer.Error(compatibility.Reason ?? "Instance is not compatible with this workflow.");
            return ReplExecutionResult.Failed();
        }

        sessionInstance = loaded;
        sessionInstancePath = tokens[1];
        renderer.Success($"Instance loaded: {sessionInstancePath}");
        return ReplExecutionResult.Success();
    }

    if (command == "save")
    {
        var path = tokens.Count > 1 ? tokens[1] : sessionInstancePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            renderer.Warning("Usage: save <path>");
            return ReplExecutionResult.Failed();
        }

        SaveInstance(path, sessionInstance with { UpdatedAt = DateTimeOffset.UtcNow });
        sessionInstancePath = path;
        renderer.Success($"Instance saved: {path}");
        return ReplExecutionResult.Success();
    }

    if (command is "inspect" or "fire")
    {
        if (command == "inspect" && tokens.Count < 2)
        {
            var inspections = new List<DslInspectionResult>();
            foreach (var evt in workflow.Events)
                inspections.Add(workflow.Inspect(sessionInstance, evt.Name, null));

            RenderInspectAll(sessionInstance.CurrentState, inspections, outputMode, symbolMode, renderer);
            return ReplExecutionResult.Success();
        }

        if (tokens.Count < 2)
        {
            renderer.Warning($"Usage: {command} <EventName> [event-args-json]");
            return ReplExecutionResult.Failed();
        }

        var eventName = tokens[1];
        IReadOnlyDictionary<string, object?>? eventArgs = null;

        if (tokens.Count > 2)
        {
            eventArgs = ParseEventArguments(tokens[2], null);
        }
        else if (command == "fire" && isInteractive)
        {
            var inspectForPrompt = workflow.Inspect(sessionInstance, eventName);
            if (inspectForPrompt.IsDefined && inspectForPrompt.IsAccepted && inspectForPrompt.RequiredEventArgumentKeys.Count > 0)
            {
                if (!TryPromptForEventArguments(eventName, inspectForPrompt.RequiredEventArgumentKeys, renderer, out eventArgs))
                    return ReplExecutionResult.Failed();
            }
        }

        if (command == "inspect")
        {
            var inspect = workflow.Inspect(sessionInstance, eventName, eventArgs);
            RenderInspect(inspect, outputMode, symbolMode, renderer);
            return ReplExecutionResult.Success();
        }

        var instanceResult = workflow.Fire(sessionInstance, eventName, eventArgs);
        RenderFire(instanceResult, outputMode, symbolMode, renderer);

        if (instanceResult.IsAccepted && instanceResult.UpdatedInstance is not null)
            sessionInstance = instanceResult.UpdatedInstance;

        return ReplExecutionResult.Success();
    }

    renderer.Warning($"Unknown REPL command '{tokens[0]}'. Type 'help' for options.");
    return ReplExecutionResult.Failed();
}

static void RenderInspect(DslInspectionResult inspect, OutputMode mode, SymbolMode symbolMode, CliRenderer renderer)
{
    if (mode == OutputMode.Json)
    {
        renderer.Json(new
        {
            kind = "inspect",
            defined = inspect.IsDefined,
            accepted = inspect.IsAccepted,
            currentState = inspect.CurrentState,
            eventName = inspect.EventName,
            target = inspect.TargetState,
            reasons = inspect.Reasons
        });
        return;
    }

    if (mode == OutputMode.Verbose)
    {
        renderer.Info($"Defined: {inspect.IsDefined}");
        renderer.Info($"Accepted: {inspect.IsAccepted}");
        renderer.Info($"Target: {inspect.TargetState ?? "<none>"}");
        if (inspect.Reasons.Count > 0)
        {
            renderer.Info("Reasons:");
            foreach (var reason in inspect.Reasons)
                renderer.Info($" - {reason}");
        }

        if (!inspect.IsDefined)
            renderer.Warning("Outcome: NotDefined");
        else if (!inspect.IsAccepted)
            renderer.Warning("Outcome: Rejected");

        return;
    }

    if (!inspect.IsDefined)
    {
        var reason = inspect.Reasons.FirstOrDefault() ?? "Not defined.";
        renderer.Error($"{CompactSymbols.Err(symbolMode)} inspect {inspect.EventName}: not defined ({reason})");
        return;
    }

    if (!inspect.IsAccepted)
    {
        var reason = inspect.Reasons.FirstOrDefault() ?? "Rejected.";
        renderer.Warning($"{CompactSymbols.Warn(symbolMode)} inspect {inspect.EventName}: rejected ({reason})");
        return;
    }

    renderer.Success($"{CompactSymbols.Ok(symbolMode)} inspect {inspect.EventName} {CompactSymbols.Arrow(symbolMode)} {inspect.TargetState}");
}

static void RenderInspectAll(
    string currentState,
    IReadOnlyCollection<DslInspectionResult> inspections,
    OutputMode mode,
    SymbolMode symbolMode,
    CliRenderer renderer)
{
    var callable = inspections
        .Where(i => i.IsDefined && i.IsAccepted)
        .OrderBy(i => i.EventName, StringComparer.Ordinal)
        .ToArray();

    if (mode == OutputMode.Json)
    {
        renderer.Json(new
        {
            kind = "inspect-all",
            currentState,
            callableEvents = callable.Select(i => new { name = i.EventName, target = i.TargetState }).ToArray()
        });
        return;
    }

    if (mode == OutputMode.Verbose)
    {
        renderer.Info($"State: {currentState}");
        if (callable.Length == 0)
        {
            renderer.Warning("No callable events.");
            return;
        }

        renderer.Info("Callable events:");
        foreach (var inspect in callable)
            renderer.Info($" - {inspect.EventName} -> {inspect.TargetState}");

        return;
    }

    if (callable.Length == 0)
    {
        renderer.Warning($"{CompactSymbols.Warn(symbolMode)} inspect: no callable events from {currentState}");
        return;
    }

    renderer.Success($"{CompactSymbols.Ok(symbolMode)} inspect: callable events from {currentState}");
    foreach (var inspect in callable)
        renderer.Info($"{inspect.EventName} {CompactSymbols.Arrow(symbolMode)} {inspect.TargetState}");
}

static void RenderFire(DslInstanceFireResult fire, OutputMode mode, SymbolMode symbolMode, CliRenderer renderer)
{
    if (mode == OutputMode.Json)
    {
        renderer.Json(new
        {
            kind = "fire",
            defined = fire.IsDefined,
            accepted = fire.IsAccepted,
            previousState = fire.PreviousState,
            eventName = fire.EventName,
            newState = fire.NewState,
            reasons = fire.Reasons
        });
        return;
    }

    if (mode == OutputMode.Verbose)
    {
        renderer.Info($"Defined: {fire.IsDefined}");
        renderer.Info($"Accepted: {fire.IsAccepted}");
        renderer.Info($"NewState: {fire.NewState ?? "<none>"}");
        if (fire.Reasons.Count > 0)
        {
            renderer.Info("Reasons:");
            foreach (var reason in fire.Reasons)
                renderer.Info($" - {reason}");
        }

        if (!fire.IsDefined)
            renderer.Warning("Outcome: NotDefined");
        else if (!fire.IsAccepted)
            renderer.Warning("Outcome: Rejected");

        return;
    }

    if (!fire.IsDefined)
    {
        var reason = fire.Reasons.FirstOrDefault() ?? "Not defined.";
        renderer.Error($"{CompactSymbols.Err(symbolMode)} fire {fire.EventName}: not defined ({reason})");
        return;
    }

    if (!fire.IsAccepted)
    {
        var reason = fire.Reasons.FirstOrDefault() ?? "Rejected.";
        renderer.Warning($"{CompactSymbols.Warn(symbolMode)} fire {fire.EventName}: rejected ({reason})");
        return;
    }

    renderer.Success($"{CompactSymbols.Ok(symbolMode)} fire {fire.EventName}: {fire.PreviousState} {CompactSymbols.Arrow(symbolMode)} {fire.NewState}");
}

static void RenderData(IReadOnlyDictionary<string, object?> data, CliRenderer renderer)
{
    if (data.Count == 0)
    {
        renderer.Info("(no instance data)");
        return;
    }

    foreach (var entry in data.OrderBy(e => e.Key, StringComparer.Ordinal))
        renderer.Info($"{entry.Key}: {FormatDataValue(entry.Value)}");
}

static string FormatDataValue(object? value)
{
    if (value is null)
        return "<null>";

    if (value is string s)
        return s;

    if (value is bool b)
        return b ? "true" : "false";

    if (value is IFormattable formattable)
        return formattable.ToString(null, CultureInfo.InvariantCulture);

    return value.ToString() ?? string.Empty;
}

static void PrintReplHelp(CliRenderer renderer)
{
    renderer.Info("REPL commands:");
    renderer.Info("  help");
    renderer.Info("  output [compact|verbose|json]");
    renderer.Info("  symbols [auto|ascii|unicode|test]");
    renderer.Info("    test prints a symbol compatibility matrix for your terminal/font");
    renderer.Info("  state");
    renderer.Info("  events");
    renderer.Info("  data");
    renderer.Info("  inspect [EventName] [event-args-json]");
    renderer.Info("    without EventName, inspects all events and lists callable ones");
    renderer.Info("  fire <EventName> [event-args-json]");
    renderer.Info("    when a selected transition transform requires event keys and no event-args-json is provided, REPL prompts per key");
    renderer.Info("    event-args-json is evaluated only for that command and does not mutate instance data");
    renderer.Info("  load <path>");
    renderer.Info("  save [path]");
    renderer.Info("  exit | quit");
}

static List<string> Tokenize(string input)
{
    var result = new List<string>();
    var sb = new StringBuilder();
    bool inQuotes = false;
    char quoteChar = '\0';

    foreach (var ch in input)
    {
        if ((ch == '"' || ch == '\'') && (!inQuotes || ch == quoteChar))
        {
            if (!inQuotes)
            {
                inQuotes = true;
                quoteChar = ch;
            }
            else
            {
                inQuotes = false;
                quoteChar = '\0';
            }

            continue;
        }

        if (char.IsWhiteSpace(ch) && !inQuotes)
        {
            if (sb.Length > 0)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }

            continue;
        }

        sb.Append(ch);
    }

    if (sb.Length > 0)
        result.Add(sb.ToString());

    return result;
}

static bool TryPromptForEventArguments(
    string eventName,
    IReadOnlyList<string> requiredKeys,
    CliRenderer renderer,
    out IReadOnlyDictionary<string, object?>? eventArguments)
{
    renderer.Info($"event args for {eventName}:");
    var values = new Dictionary<string, object?>(StringComparer.Ordinal);

    foreach (var key in requiredKeys)
    {
        renderer.Prompt($"  {key}> ");
        var raw = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(raw))
        {
            renderer.Warning($"fire cancelled: value for '{key}' is required.");
            eventArguments = null;
            return false;
        }

        values[key] = ParseInteractiveArgumentValue(raw);
    }

    eventArguments = values;
    return true;
}

static object? ParseInteractiveArgumentValue(string raw)
{
    var text = raw.Trim();

    if (text.Equals("null", StringComparison.OrdinalIgnoreCase))
        return null;

    if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
        return true;

    if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
        return false;

    if ((text.Length >= 2 && text[0] == '\'' && text[^1] == '\'') ||
        (text.Length >= 2 && text[0] == '"' && text[^1] == '"'))
    {
        return text.Substring(1, text.Length - 2);
    }

    if (long.TryParse(text, out var integer))
        return integer;

    if (double.TryParse(text, out var number))
        return number;

    return text;
}

static void RenderSymbolsTest(CliRenderer renderer)
{
    var rows = new[]
    {
        ("OK", "✔", "U+2714"),
        ("WARN", "⚠", "U+26A0"),
        ("ERR", "✖", "U+2716"),
        ("->", "→", "U+2192"),
        ("*", "•", "U+2022"),
        ("i", "ℹ", "U+2139"),
        ("(ok)", "✅", "U+2705"),
        ("(x)", "❌", "U+274C")
    };

    renderer.Info("Symbol rendering test:");
    renderer.Info("  ASCII   Unicode   Codepoint");
    foreach (var row in rows)
        renderer.Info($"  {row.Item1,-7} {row.Item2,-8} {row.Item3}");
    renderer.Info("Tip: use 'symbols ascii' if Unicode appears as boxes or misaligned glyphs.");
}

static SymbolMode ResolveSymbolMode(SymbolMode configuredMode)
    => SymbolSupport.ResolveSymbolMode(configuredMode);

enum OutputMode
{
    Compact,
    Verbose,
    Json
}

enum SymbolMode
{
    Auto,
    Ascii,
    Unicode
}

readonly record struct ReplExecutionResult(bool IsSuccess, bool ShouldExit)
{
    public static ReplExecutionResult Success() => new(true, false);
    public static ReplExecutionResult Failed() => new(false, false);
    public static ReplExecutionResult Exit() => new(true, true);
}

sealed class CliRenderer
{
    private readonly bool _useColor;

    public CliRenderer(bool useColor)
    {
        _useColor = useColor;
    }

    public void Prompt(string text)
    {
        if (_useColor)
            AnsiConsole.Markup($"[grey]{Markup.Escape(text)}[/]");
        else
            Console.Write(text);
    }

    public void Meta(string text) => WriteStyled(text, "grey");
    public void Info(string text) => WriteStyled(text, "white");
    public void Success(string text) => WriteStyled(text, "green");
    public void Warning(string text) => WriteStyled(text, "yellow");
    public void Error(string text) => WriteStyled(text, "red");

    public void Json(object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        if (_useColor)
            AnsiConsole.MarkupLine($"[deepskyblue1]{Markup.Escape(json)}[/]");
        else
            Console.WriteLine(json);
    }

    private void WriteStyled(string text, string color)
    {
        if (_useColor)
            AnsiConsole.MarkupLine($"[{color}]{Markup.Escape(text)}[/]");
        else
            Console.WriteLine(text);
    }
}

static class CompactSymbols
{
    public static string Ok(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "✔" : "OK";
    public static string Warn(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "⚠" : "WARN";
    public static string Err(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "✖" : "ERR";
    public static string Arrow(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "→" : "->";

    private static SymbolMode Resolve(SymbolMode mode) => SymbolSupport.ResolveSymbolMode(mode);
}

static class SymbolSupport
{
    public static SymbolMode ResolveSymbolMode(SymbolMode configuredMode)
    {
        if (configuredMode != SymbolMode.Auto)
            return configuredMode;

        return SupportsUnicodeSymbols() ? SymbolMode.Unicode : SymbolMode.Ascii;
    }

    public static bool SupportsUnicodeSymbols()
    {
        if (Console.IsOutputRedirected)
            return false;

        if (Console.OutputEncoding.CodePage != 65001)
            return false;

        var wtSession = Environment.GetEnvironmentVariable("WT_SESSION");
        if (!string.IsNullOrWhiteSpace(wtSession))
            return true;

        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        if (termProgram?.Equals("vscode", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var term = Environment.GetEnvironmentVariable("TERM");
        if (!string.IsNullOrWhiteSpace(term))
        {
            var lowered = term.ToLowerInvariant();
            if (lowered.Contains("xterm", StringComparison.Ordinal) ||
                lowered.Contains("vt", StringComparison.Ordinal) ||
                lowered.Contains("screen", StringComparison.Ordinal) ||
                lowered.Contains("tmux", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
