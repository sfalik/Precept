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

var outputMode = OutputMode.Compact;

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
var colorTheme = CliColorTheme.GithubDark;
var renderer = new CliRenderer(!noColor && !Console.IsOutputRedirected, colorTheme);

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

            var exec = ExecuteReplCommand(line, workflow, ref sessionInstance, ref instancePath, renderer, ref outputMode, ref symbolMode, ref colorTheme, isInteractive: false);
            if (!exec.IsSuccess)
                return ExitScriptFailed;
            if (exec.ShouldExit)
                break;
        }

        return ExitSuccess;
    }

    if (outputMode != OutputMode.Compact)
    {
        renderer.Warning("Interactive REPL supports compact mode only; using compact.");
        outputMode = OutputMode.Compact;
    }

    renderer.Meta($"Machine: {machine.Name}");
    renderer.Meta($"State: {sessionInstance.CurrentState}");
    renderer.Meta("Type 'help' for commands, 'exit' to quit.");

    while (true)
    {
        renderer.StatePrompt(sessionInstance.CurrentState, symbolMode);
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            continue;

        var exec = ExecuteReplCommand(input, workflow, ref sessionInstance, ref instancePath, renderer, ref outputMode, ref symbolMode, ref colorTheme, isInteractive: true);
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

static void PrintUsage()
{
    Console.WriteLine("StateMachine.Dsl.Cli");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dsl <file.sm> --instance <instance.json> [--script <commands.txt>] [--echo] [--no-color] [--unicode|--ascii]");
    Console.WriteLine("Notes:");
    Console.WriteLine("  Without --script, starts interactive REPL.");
    Console.WriteLine("  With --script, runs REPL commands from file non-interactively.");
    Console.WriteLine("  Script mode emits log-style output lines (INFO/WARN/ERROR).");
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
        JsonValueKind.Object => throw new InvalidOperationException("Only scalar JSON values are supported for instance data and event arguments (object is not supported)."),
        JsonValueKind.Array => throw new InvalidOperationException("Only scalar JSON values are supported for instance data and event arguments (array is not supported)."),
        _ => throw new InvalidOperationException($"Unsupported JSON value kind '{value.ValueKind}' for scalar contract.")
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
    ref CliColorTheme colorTheme,
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

    if (command == "style")
    {
        if (tokens.Count < 2)
        {
            renderer.Warning("Usage: style <preview [all]|theme <name|list>>");
            return ReplExecutionResult.Failed();
        }

        if (tokens[1].Equals("preview", StringComparison.OrdinalIgnoreCase))
        {
            if (tokens.Count > 2 && tokens[2].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                renderer.StylePreviewAll(outputMode, symbolMode);
                return ReplExecutionResult.Success();
            }

            renderer.StylePreview(outputMode, symbolMode);
            return ReplExecutionResult.Success();
        }

        if (tokens[1].Equals("theme", StringComparison.OrdinalIgnoreCase))
        {
            if (tokens.Count < 3 || tokens[2].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                renderer.Info($"themes: {string.Join(", ", CliColorThemes.Tokens)}");
                renderer.Info($"current: {CliColorThemes.ToToken(colorTheme)}");
                return ReplExecutionResult.Success();
            }

            if (!CliColorThemes.TryParse(tokens[2], out var parsedTheme))
            {
                renderer.Warning("Usage: style theme <muted|nord-crisp|tokyo-night|github-dark|solarized-modern|mono-accent|list>");
                return ReplExecutionResult.Failed();
            }

            colorTheme = parsedTheme;
            renderer.SetTheme(colorTheme);
            renderer.Success($"Theme set to {CliColorThemes.ToToken(colorTheme)}");
            return ReplExecutionResult.Success();
        }

        renderer.Warning("Usage: style <preview [all]|theme <name|list>>");
        return ReplExecutionResult.Failed();
    }

    if (command == "state")
    {
        if (isInteractive)
            renderer.StateValue(sessionInstance.CurrentState, symbolMode, isLast: true);
        else
            renderer.LogInfo($"state current={sessionInstance.CurrentState}");
        return ReplExecutionResult.Success();
    }

    if (command == "events")
    {
        var eventColumnWidth = workflow.Events.Count == 0
            ? 0
            : workflow.Events.Max(e => e.Name.Length);

        foreach (var evt in workflow.Events)
        {
            if (isInteractive)
                renderer.EventValue(evt.Name, symbolMode, eventColumnWidth, isLast: evt == workflow.Events[^1]);
            else
                renderer.LogInfo($"event name={evt.Name}");
        }
        return ReplExecutionResult.Success();
    }

    if (command == "data")
    {
        if (!isInteractive)
        {
            if (sessionInstance.InstanceData.Count == 0)
            {
                renderer.LogInfo("data empty");
            }
            else
            {
                foreach (var entry in sessionInstance.InstanceData.OrderBy(e => e.Key, StringComparer.Ordinal))
                    renderer.LogInfo($"data {entry.Key}={FormatDataValue(entry.Value)}");
            }
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

            RenderInspectAll(sessionInstance.CurrentState, inspections, outputMode, symbolMode, renderer, isInteractive);
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
            var requiredEventKeys = workflow.Events
                .FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.Ordinal))?
                .Args
                .Where(a => !a.IsNullable)
                .Select(a => a.Name)
                .ToArray()
                ?? Array.Empty<string>();

            var inspectForPrompt = workflow.Inspect(sessionInstance, eventName);
            if (inspectForPrompt.Outcome != DslOutcomeKind.Undefined && requiredEventKeys.Length > 0)
            {
                if (!TryPromptForEventArguments(eventName, requiredEventKeys, renderer, symbolMode, out eventArgs))
                    return ReplExecutionResult.Failed();
            }
        }

        if (command == "inspect")
        {
            var inspect = workflow.Inspect(sessionInstance, eventName, eventArgs);
            RenderInspect(inspect, outputMode, symbolMode, renderer, isInteractive);
            return ReplExecutionResult.Success();
        }

        var instanceResult = workflow.Fire(sessionInstance, eventName, eventArgs);
        RenderFire(instanceResult, outputMode, symbolMode, renderer, isInteractive);

        if (instanceResult.Outcome == DslOutcomeKind.Enabled && instanceResult.UpdatedInstance is not null)
            sessionInstance = instanceResult.UpdatedInstance;

        return ReplExecutionResult.Success();
    }

    renderer.Warning($"Unknown REPL command '{tokens[0]}'. Type 'help' for options.");
    return ReplExecutionResult.Failed();
}

static void RenderInspect(DslInspectionResult inspect, OutputMode mode, SymbolMode symbolMode, CliRenderer renderer, bool isInteractive)
{
    if (!isInteractive)
    {
        if (inspect.Outcome == DslOutcomeKind.Enabled)
        {
            renderer.LogInfo($"inspect event={inspect.EventName} outcome=enabled target={inspect.TargetState}");
        }
        else if (inspect.Outcome == DslOutcomeKind.Blocked)
        {
            var reason = inspect.Reasons.FirstOrDefault() ?? "Rejected.";
            renderer.LogWarn($"inspect event={inspect.EventName} outcome=blocked reason=\"{reason}\"");
        }
        else
        {
            var reason = inspect.Reasons.FirstOrDefault() ?? "Not defined.";
            renderer.LogWarn($"inspect event={inspect.EventName} outcome=undefined reason=\"{reason}\"");
        }

        return;
    }

    if (mode == OutputMode.Json)
    {
        renderer.Json(new
        {
            kind = "inspect",
            outcome = inspect.Outcome.ToString().ToLowerInvariant(),
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
        renderer.VerboseInspect(inspect);

        return;
    }

    if (inspect.Outcome == DslOutcomeKind.Undefined)
    {
        var reason = inspect.Reasons.FirstOrDefault() ?? "Not defined.";
        var undefinedMessage = DescribeUndefinedOutcome(inspect.EventName, inspect.CurrentState, reason);
        if (isInteractive)
            renderer.Error(renderer.CompactResult(symbolMode, $"{inspect.EventName} {CompactSymbols.Err(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {undefinedMessage}"));
        else
            renderer.Error($"{CompactSymbols.Err(symbolMode)} inspect {inspect.EventName}: {undefinedMessage} ({reason})");
        return;
    }

    if (inspect.Outcome == DslOutcomeKind.Blocked)
    {
        var reason = inspect.Reasons.FirstOrDefault() ?? "Rejected.";
        if (isInteractive)
            renderer.Warning(renderer.CompactResult(symbolMode, $"{inspect.EventName} {CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {reason}"));
        else
            renderer.Warning($"{CompactSymbols.Warn(symbolMode)} inspect {inspect.EventName}: {reason}");
        return;
    }

    if (isInteractive)
        renderer.PreviewEventStateLine(inspect.EventName, inspect.TargetState ?? "<none>", symbolMode, isLast: true);
    else
        renderer.Success($"{CompactSymbols.Ok(symbolMode)} inspect {inspect.EventName} {CompactSymbols.Arrow(symbolMode)} {inspect.TargetState}");
}

static void RenderInspectAll(
    string currentState,
    IReadOnlyCollection<DslInspectionResult> inspections,
    OutputMode mode,
    SymbolMode symbolMode,
    CliRenderer renderer,
    bool isInteractive)
{
    var callable = inspections
        .Where(i => i.Outcome == DslOutcomeKind.Enabled)
        .OrderBy(i => i.EventName, StringComparer.Ordinal)
        .ToArray();

    if (!isInteractive)
    {
        renderer.LogInfo($"inspect-all state={currentState} callable={callable.Length}");
        foreach (var inspect in callable)
            renderer.LogInfo($"inspect event={inspect.EventName} outcome=enabled target={inspect.TargetState}");
        return;
    }

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
        renderer.VerboseInspectAll(currentState, callable);

        return;
    }

    if (callable.Length == 0)
    {
        if (isInteractive)
            renderer.Warning(renderer.CompactResult(symbolMode, $"{CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} no callable events from {currentState}"));
        else
            renderer.Warning($"{CompactSymbols.Warn(symbolMode)} inspect: no callable events from {currentState}");
        return;
    }

    if (!isInteractive)
        renderer.Success($"{CompactSymbols.Ok(symbolMode)} inspect: callable events from {currentState}");
    var eventColumnWidth = callable.Length == 0
        ? 0
        : callable.Max(i => i.EventName.Length);
    foreach (var inspect in callable)
    {
        if (isInteractive)
            renderer.EventStateLine(
                inspect.EventName,
                inspect.TargetState ?? "<none>",
                CompactSymbols.PreviewArrow(symbolMode),
                symbolMode,
                eventColumnWidth,
                isLast: inspect == callable[^1]);
        else
            renderer.Info($"{inspect.EventName} {CompactSymbols.Arrow(symbolMode)} {inspect.TargetState}");
    }
}

static void RenderFire(DslInstanceFireResult fire, OutputMode mode, SymbolMode symbolMode, CliRenderer renderer, bool isInteractive)
{
    if (!isInteractive)
    {
        if (fire.Outcome == DslOutcomeKind.Enabled)
        {
            renderer.LogInfo($"fire event={fire.EventName} outcome=enabled from={fire.PreviousState} to={fire.NewState}");
        }
        else if (fire.Outcome == DslOutcomeKind.Blocked)
        {
            var reason = fire.Reasons.FirstOrDefault() ?? "Rejected.";
            renderer.LogWarn($"fire event={fire.EventName} outcome=blocked reason=\"{reason}\"");
        }
        else
        {
            var reason = fire.Reasons.FirstOrDefault() ?? "Not defined.";
            renderer.LogWarn($"fire event={fire.EventName} outcome=undefined reason=\"{reason}\"");
        }

        return;
    }

    if (mode == OutputMode.Json)
    {
        renderer.Json(new
        {
            kind = "fire",
            outcome = fire.Outcome.ToString().ToLowerInvariant(),
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
        renderer.VerboseFire(fire);

        return;
    }

    if (fire.Outcome == DslOutcomeKind.Undefined)
    {
        var reason = fire.Reasons.FirstOrDefault() ?? "Not defined.";
        var undefinedMessage = DescribeUndefinedOutcome(fire.EventName, fire.PreviousState, reason);
        if (isInteractive)
            renderer.Error(renderer.CompactResult(symbolMode, $"{fire.EventName} {CompactSymbols.Err(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {undefinedMessage}"));
        else
            renderer.Error($"{CompactSymbols.Err(symbolMode)} fire {fire.EventName}: {undefinedMessage} ({reason})");
        return;
    }

    if (fire.Outcome == DslOutcomeKind.Blocked)
    {
        var reason = fire.Reasons.FirstOrDefault() ?? "Rejected.";
        if (isInteractive)
            renderer.Warning(renderer.CompactResult(symbolMode, $"{fire.EventName} {CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {reason}"));
        else
            renderer.Warning($"{CompactSymbols.Warn(symbolMode)} fire {fire.EventName}: blocked ({reason})");
        return;
    }

    if (isInteractive)
        renderer.Success(renderer.CompactResult(symbolMode, $"{fire.EventName} {CompactSymbols.Ok(symbolMode)} {CompactSymbols.Arrow(symbolMode)} {fire.NewState ?? "<none>"}"));
    else
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

static string DescribeUndefinedOutcome(string eventName, string currentState, string reason)
{
    if (reason.StartsWith("Unknown event", StringComparison.OrdinalIgnoreCase))
        return "unknown event";

    return $"no transition from {currentState}";
}

static void PrintReplHelp(CliRenderer renderer)
{
    renderer.Info("REPL commands:");
    renderer.Info("  help");
    renderer.Info("  symbols [auto|ascii|unicode|test]");
    renderer.Info("    test prints a symbol compatibility matrix for your terminal/font");
    renderer.Info("  style preview [all]");
    renderer.Info("    prints a terminal style sample using current output mode (compact/verbose), or all themes");
    renderer.Info("  style theme <name|list>");
    renderer.Info("    set or list available color themes");
    renderer.Info("  state");
    renderer.Info("  events");
    renderer.Info("  data");
    renderer.Info("  inspect [EventName] [event-args-json]");
    renderer.Info("    without EventName, inspects all events and lists callable ones");
    renderer.Info("  fire <EventName> [event-args-json]");
    renderer.Info("    when EventName has required args and no event-args-json is provided, REPL prompts per key");
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
    SymbolMode symbolMode,
    out IReadOnlyDictionary<string, object?>? eventArguments)
{
    var values = new Dictionary<string, object?>(StringComparer.Ordinal);

    foreach (var key in requiredKeys)
    {
        renderer.ArgumentPrompt(key, symbolMode);
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

enum CliColorTheme
{
    Muted,
    NordCrisp,
    TokyoNight,
    GithubDark,
    SolarizedModern,
    MonoAccent
}

readonly record struct CliPalette(
    string Meta,
    string Info,
    string Success,
    string Warning,
    string Error,
    string State,
    string Event,
    string PreviewEvent,
    string PreviewState,
    string Json);

static class CliColorThemes
{
    public static IReadOnlyList<string> Tokens { get; } = new[]
    {
        "muted",
        "nord-crisp",
        "tokyo-night",
        "github-dark",
        "solarized-modern",
        "mono-accent"
    };

    public static bool TryParse(string value, out CliColorTheme theme)
    {
        var normalized = value.Trim().ToLowerInvariant();
        theme = normalized switch
        {
            "muted" => CliColorTheme.Muted,
            "nord-crisp" or "nord" => CliColorTheme.NordCrisp,
            "tokyo-night" or "tokyo" => CliColorTheme.TokyoNight,
            "github-dark" or "github" => CliColorTheme.GithubDark,
            "solarized-modern" or "solarized" => CliColorTheme.SolarizedModern,
            "mono-accent" or "mono" => CliColorTheme.MonoAccent,
            _ => default
        };

        return normalized is "muted" or "nord-crisp" or "nord" or "tokyo-night" or "tokyo" or "github-dark" or "github" or "solarized-modern" or "solarized" or "mono-accent" or "mono";
    }

    public static string ToToken(CliColorTheme theme) => theme switch
    {
        CliColorTheme.Muted => "muted",
        CliColorTheme.NordCrisp => "nord-crisp",
        CliColorTheme.TokyoNight => "tokyo-night",
        CliColorTheme.GithubDark => "github-dark",
        CliColorTheme.SolarizedModern => "solarized-modern",
        CliColorTheme.MonoAccent => "mono-accent",
        _ => "muted"
    };

    public static CliPalette GetPalette(CliColorTheme theme) => theme switch
    {
        CliColorTheme.NordCrisp => new CliPalette("#6B7280", "white", "#A3BE8C", "#EBCB8B", "#BF616A", "#88C0D0", "#B48EAD", "#A3BE8C", "#8FBCBB", "#88C0D0"),
        CliColorTheme.TokyoNight => new CliPalette("#A9B1D6", "white", "#9ECE6A", "#E0AF68", "#F7768E", "#7DCFFF", "#BB9AF7", "#9ECE6A", "#73DACA", "#7DCFFF"),
        CliColorTheme.GithubDark => new CliPalette("#8B949E", "white", "#56D364", "#F2CC60", "#F85149", "#79C0FF", "#D2A8FF", "#56D364", "#7EE787", "#79C0FF"),
        CliColorTheme.SolarizedModern => new CliPalette("#657B83", "white", "#859900", "#B58900", "#DC322F", "#268BD2", "#D33682", "#2AA198", "#6C71C4", "#268BD2"),
        CliColorTheme.MonoAccent => new CliPalette("#6B7280", "#E5E7EB", "#22C55E", "#F59E0B", "#EF4444", "#D1D5DB", "#9CA3AF", "#D1D5DB", "#E5E7EB", "#D1D5DB"),
        _ => new CliPalette("#7A8394", "white", "#A3BE8C", "#EBCB8B", "#BF616A", "#81A1C1", "#B48EAD", "#A38DBE", "#88AFC8", "#81A1C1")
    };
}

readonly record struct ReplExecutionResult(bool IsSuccess, bool ShouldExit)
{
    public static ReplExecutionResult Success() => new(true, false);
    public static ReplExecutionResult Failed() => new(false, false);
    public static ReplExecutionResult Exit() => new(true, true);
}

sealed class CliRenderer
{
    private const string CompactIndent = "  ";
    private CliColorTheme _theme;
    private CliPalette _palette;
    private readonly bool _useColor;

    public CliRenderer(bool useColor, CliColorTheme theme)
    {
        _useColor = useColor;
        _theme = theme;
        _palette = CliColorThemes.GetPalette(theme);
    }

    public void SetTheme(CliColorTheme theme)
    {
        _theme = theme;
        _palette = CliColorThemes.GetPalette(theme);
    }

    public void Prompt(string text)
    {
        if (_useColor)
            AnsiConsole.Markup($"[{_palette.Meta}]{Markup.Escape(text)}[/]");
        else
            Console.Write(text);
    }

    public void StatePrompt(string state, SymbolMode symbolMode)
    {
        var promptArrow = CompactSymbols.Prompt(symbolMode);

        if (_useColor)
        {
            AnsiConsole.Markup($"[{_palette.State}]{Markup.Escape(state)}[/][{_palette.Meta}] {Markup.Escape(promptArrow)} [/] ");
            return;
        }

        Console.Write($"{state} {promptArrow} ");
    }

    public string TagEvent(string eventName) => eventName;

    public string TagState(string stateName) => stateName;

    public string CompactResult(SymbolMode symbolMode, string text) => $"{CompactIndent}{CompactSymbols.BranchEnd(symbolMode)} {text}";

    public void ArgumentPrompt(string key, SymbolMode symbolMode)
    {
        var stem = CompactSymbols.BranchStem(symbolMode);

        if (_useColor)
        {
            AnsiConsole.Markup($"[{_palette.Meta}]{Markup.Escape(CompactIndent)}{Markup.Escape(stem)} [/][{_palette.Event}]{Markup.Escape(key)}[/][{_palette.Meta}]: [/] ");
            return;
        }

        Console.Write($"{CompactIndent}{stem} {key}: ");
    }

    public void EventValue(string eventName, SymbolMode symbolMode, int eventColumnWidth = 0, bool isLast = false)
    {
        var paddedEvent = eventColumnWidth > 0 ? eventName.PadRight(eventColumnWidth) : eventName;
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        WriteStyled($"{CompactIndent}{prefix} {TagEvent(paddedEvent)}", _palette.Event);
    }

    public void StateValue(string stateName, SymbolMode symbolMode, bool isLast = true)
    {
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        WriteStyled($"{CompactIndent}{prefix} {TagState(stateName)}", _palette.State);
    }

    public void EventStateLine(string eventName, string stateName, string connector, SymbolMode symbolMode, int eventColumnWidth = 0, bool isLast = false)
    {
        var paddedEvent = eventColumnWidth > 0 ? eventName.PadRight(eventColumnWidth) : eventName;
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        var eventLabel = $"{CompactIndent}{prefix} {TagEvent(paddedEvent)}";

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.Event}]{Markup.Escape(eventLabel)}[/][{_palette.Meta}] {Markup.Escape(connector)} [/][{_palette.State}]{Markup.Escape(TagState(stateName))}[/]");
            return;
        }

        Console.WriteLine($"{eventLabel} {connector} {TagState(stateName)}");
    }

    public void PreviewEventStateLine(string eventName, string stateName, SymbolMode symbolMode, bool isLast = true)
    {
        var connector = CompactSymbols.PreviewArrow(symbolMode);
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        var eventLabel = $"{CompactIndent}{prefix} {eventName}";

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.PreviewEvent}]{Markup.Escape(eventLabel)}[/][{_palette.Meta}] {Markup.Escape(connector)} [/][{_palette.PreviewState}]{Markup.Escape(TagState(stateName))}[/]");
            return;
        }

        Console.WriteLine($"{eventLabel} {connector} {TagState(stateName)}");
    }

    public void VerboseInspect(DslInspectionResult inspect)
    {
        if (_useColor)
        {
            var table = new Table().RoundedBorder().BorderColor(Color.Grey);
            table.AddColumn(new TableColumn("Field").Centered());
            table.AddColumn("Value");
            table.AddRow("Outcome", inspect.Outcome.ToString());
            table.AddRow("Event", inspect.EventName);
            table.AddRow("Current", inspect.CurrentState);
            table.AddRow("Target", inspect.TargetState ?? "<none>");
            table.AddRow("Reasons", inspect.Reasons.Count == 0 ? "<none>" : string.Join("; ", inspect.Reasons));
            AnsiConsole.Write(table);
            return;
        }

        Info($"Outcome: {inspect.Outcome}");
        Info($"Target: {inspect.TargetState ?? "<none>"}");
        if (inspect.Reasons.Count > 0)
        {
            Info("Reasons:");
            foreach (var reason in inspect.Reasons)
                Info($" - {reason}");
        }

        if (inspect.Outcome != DslOutcomeKind.Enabled)
            Warning($"Outcome: {inspect.Outcome}");
    }

    public void VerboseInspectAll(string currentState, IReadOnlyList<DslInspectionResult> callable)
    {
        if (_useColor)
        {
            var panelTitle = $"Callable from {currentState}";
            if (callable.Count == 0)
            {
                AnsiConsole.Write(new Panel("No callable events.").Header(panelTitle).Border(BoxBorder.Rounded).BorderStyle(new Style(Color.Grey)));
                return;
            }

            var table = new Table().RoundedBorder().BorderColor(Color.Grey);
            table.AddColumn("Event");
            table.AddColumn("Target");
            foreach (var inspect in callable)
                table.AddRow(inspect.EventName, inspect.TargetState ?? "<none>");

            AnsiConsole.Write(new Panel(table).Header(panelTitle).Border(BoxBorder.Rounded).BorderStyle(new Style(Color.Grey)));
            return;
        }

        Info($"State: {currentState}");
        if (callable.Count == 0)
        {
            Warning("No callable events.");
            return;
        }

        Info("Callable events:");
        foreach (var inspect in callable)
            Info($" - {inspect.EventName} -> {inspect.TargetState}");
    }

    public void VerboseFire(DslInstanceFireResult fire)
    {
        if (_useColor)
        {
            var table = new Table().RoundedBorder().BorderColor(Color.Grey);
            table.AddColumn(new TableColumn("Field").Centered());
            table.AddColumn("Value");
            table.AddRow("Outcome", fire.Outcome.ToString());
            table.AddRow("Event", fire.EventName);
            table.AddRow("Previous", fire.PreviousState);
            table.AddRow("New", fire.NewState ?? "<none>");
            table.AddRow("Reasons", fire.Reasons.Count == 0 ? "<none>" : string.Join("; ", fire.Reasons));
            AnsiConsole.Write(table);
            return;
        }

        Info($"Outcome: {fire.Outcome}");
        Info($"NewState: {fire.NewState ?? "<none>"}");
        if (fire.Reasons.Count > 0)
        {
            Info("Reasons:");
            foreach (var reason in fire.Reasons)
                Info($" - {reason}");
        }

        if (fire.Outcome != DslOutcomeKind.Enabled)
            Warning($"Outcome: {fire.Outcome}");
    }

    public void Meta(string text) => WriteStyled(text, _palette.Meta);
    public void Info(string text) => WriteStyled(text, _palette.Info);
    public void Success(string text) => WriteStyled(text, _palette.Success);
    public void Warning(string text) => WriteStyled(text, _palette.Warning);
    public void Error(string text) => WriteStyled(text, _palette.Error);

    public void Json(object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        if (_useColor)
            AnsiConsole.MarkupLine($"[{_palette.Json}]{Markup.Escape(json)}[/]");
        else
            Console.WriteLine(json);
    }

            public void LogInfo(string message) => WriteLog("INFO", message, _palette.Info);
            public void LogWarn(string message) => WriteLog("WARN", message, _palette.Warning);
            public void LogError(string message) => WriteLog("ERROR", message, _palette.Error);

    public void StylePreviewAll(OutputMode outputMode, SymbolMode symbolMode)
    {
        var priorTheme = _theme;
        var priorPalette = _palette;

        foreach (var token in CliColorThemes.Tokens)
        {
            CliColorThemes.TryParse(token, out var theme);
            SetTheme(theme);
            Meta($"Theme: {token}");
            StylePreview(outputMode, symbolMode);
            Meta(string.Empty);
        }

        _theme = priorTheme;
        _palette = priorPalette;
    }

    public void StylePreview(OutputMode outputMode, SymbolMode symbolMode)
    {
        if (outputMode == OutputMode.Verbose)
        {
            StylePreviewVerbose();
            return;
        }

        StylePreviewCompact(symbolMode);
    }

    private void StylePreviewCompact(SymbolMode symbolMode)
    {
        var ok = CompactSymbols.Ok(symbolMode);
        var warn = CompactSymbols.Warn(symbolMode);
        var err = CompactSymbols.Err(symbolMode);
        var pipe = CompactSymbols.Pipe(symbolMode);
        var promptArrow = CompactSymbols.Prompt(symbolMode);
        var previewArrow = CompactSymbols.PreviewArrow(symbolMode);
        var commitArrow = CompactSymbols.Arrow(symbolMode);
        var branchMid = CompactSymbols.BranchMid(symbolMode);
        var branchEnd = CompactSymbols.BranchEnd(symbolMode);
        var branchStem = CompactSymbols.BranchStem(symbolMode);

        Meta("Style preview transcript:");
        if (_useColor)
        {
            AnsiConsole.MarkupLine($"[{_palette.State}]Red[/][{_palette.Meta}] {Markup.Escape(promptArrow)}[/] inspect");
            AnsiConsole.MarkupLine($"[{_palette.PreviewEvent}]{Markup.Escape(CompactIndent)}{Markup.Escape(branchMid)} Advance[/][{_palette.Meta}] {Markup.Escape(previewArrow)} [/][{_palette.PreviewState}]Green[/]");
            AnsiConsole.MarkupLine($"[{_palette.PreviewEvent}]{Markup.Escape(CompactIndent)}{Markup.Escape(branchEnd)} Emergency[/][{_palette.Meta}] {Markup.Escape(previewArrow)} [/][{_palette.PreviewState}]FlashingRed[/]");
            AnsiConsole.MarkupLine($"[{_palette.State}]Red[/][{_palette.Meta}] {Markup.Escape(promptArrow)}[/] fire Advance");
            AnsiConsole.MarkupLine($"[{_palette.Success}]{Markup.Escape(CompactIndent)}{Markup.Escape(branchEnd)} Advance {Markup.Escape(ok)} {Markup.Escape(commitArrow)} Green[/]");
            AnsiConsole.MarkupLine($"[{_palette.State}]Green[/][{_palette.Meta}] {Markup.Escape(promptArrow)}[/] fire Emergency");
            AnsiConsole.MarkupLine($"[{_palette.Meta}]{Markup.Escape(CompactIndent)}{Markup.Escape(branchStem)} [/][{_palette.Event}]Reason[/][{_palette.Meta}]: Accident[/]");
            AnsiConsole.MarkupLine($"[{_palette.Success}]{Markup.Escape(CompactIndent)}{Markup.Escape(branchEnd)} Emergency {Markup.Escape(ok)} {Markup.Escape(commitArrow)} FlashingRed[/]");
            AnsiConsole.MarkupLine($"[{_palette.State}]Red[/][{_palette.Meta}] {Markup.Escape(promptArrow)}[/] fire ClearEmergency");
            AnsiConsole.MarkupLine($"[{_palette.Error}]{Markup.Escape(CompactIndent)}{Markup.Escape(branchEnd)} ClearEmergency {Markup.Escape(err)} {Markup.Escape(pipe)} no transition from Red[/]");
            AnsiConsole.MarkupLine($"[{_palette.State}]Red[/][{_palette.Meta}] {Markup.Escape(promptArrow)}[/] inspect Advance");
            AnsiConsole.MarkupLine($"[{_palette.Warning}]{Markup.Escape(CompactIndent)}{Markup.Escape(branchEnd)} Advance {Markup.Escape(warn)} {Markup.Escape(pipe)} Cars waiting required[/]");
            return;
        }

        Info($"Red {promptArrow} inspect");
        Info($"{CompactIndent}{branchMid} Advance {previewArrow} Green");
        Info($"{CompactIndent}{branchEnd} Emergency {previewArrow} FlashingRed");
        Info($"Red {promptArrow} fire Advance");
        Info($"{CompactIndent}{branchEnd} Advance {ok} {commitArrow} Green");
        Info($"Green {promptArrow} fire Emergency");
        Info($"{CompactIndent}{branchStem} Reason: Accident");
        Info($"{CompactIndent}{branchEnd} Emergency {ok} {commitArrow} FlashingRed");
        Info($"Red {promptArrow} fire ClearEmergency");
        Info($"{CompactIndent}{branchEnd} ClearEmergency {err} {pipe} no transition from Red");
        Info($"Red {promptArrow} inspect Advance");
        Info($"{CompactIndent}{branchEnd} Advance {warn} {pipe} Cars waiting required");
    }

    private void StylePreviewVerbose()
    {
        Meta("Style preview verbose:");

        var inspectRows = new List<DslInspectionResult>
        {
            new DslInspectionResult(
                DslOutcomeKind.Enabled,
                true,
                true,
                "Red",
                "Advance",
                "Green",
                Array.Empty<string>(),
                Array.Empty<string>()),
            new DslInspectionResult(
                DslOutcomeKind.Enabled,
                true,
                true,
                "Red",
                "Emergency",
                "FlashingRed",
                Array.Empty<string>(),
                Array.Empty<string>())
        };

        VerboseInspectAll("Red", inspectRows);

        VerboseInspect(new DslInspectionResult(
            DslOutcomeKind.Blocked,
            true,
            false,
            "Red",
            "Advance",
            null,
            Array.Empty<string>(),
            new[] { "Guard 'CarsWaiting > 0' failed." }));

        VerboseFire(new DslInstanceFireResult(
            DslOutcomeKind.Enabled,
            true,
            true,
            "Red",
            "Advance",
            "Green",
            Array.Empty<string>(),
            null));

        VerboseFire(new DslInstanceFireResult(
            DslOutcomeKind.Undefined,
            false,
            false,
            "FlashingRed",
            "ClearEmergency",
            null,
            new[] { "No transition for 'ClearEmergency' from 'FlashingRed'." },
            null));
    }

    private void WriteStyled(string text, string color)
    {
        if (_useColor)
            AnsiConsole.MarkupLine($"[{color}]{Markup.Escape(text)}[/]");
        else
            Console.WriteLine(text);
    }

    private void WriteLog(string level, string message, string color)
    {
        var line = $"{DateTimeOffset.Now:HH:mm:ss} {level} {message}";
        WriteStyled(line, color);
    }
}

static class CompactSymbols
{
    public static string Prompt(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "›" : ">";
    public static string Pipe(SymbolMode mode) => "|";
    public static string BranchMid(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "├─" : "|-";
    public static string BranchEnd(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "└─" : "\\-";
    public static string BranchStem(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "│" : "|";
    public static string Ok(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "✔" : "OK";
    public static string Warn(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "⚠" : "WARN";
    public static string Err(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "✖" : "ERR";
    public static string Arrow(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "──▶" : "==>";
    public static string PreviewArrow(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "──▷" : "-->";

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
