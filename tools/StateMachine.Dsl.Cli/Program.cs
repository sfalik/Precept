using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;
using StateMachine.Dsl;

const int ExitSuccess = 0;
const int ExitInvalidUsage = 1;
const int ExitInputFileNotFound = 2;
const int ExitUnhandledError = 4;
const int ExitIncompatibleInstance = 5;
const int ExitScriptFailed = 6;

string[] replTopLevelCommands =
[
    "help",
    "clear",
    "symbols",
    "style",
    "state",
    "events",
    "data",
    "inspect",
    "fire",
    "load",
    "save",
    "exit",
    "quit"
];

string[] replSymbolsSubcommands =
[
    "auto",
    "ascii",
    "unicode",
    "test"
];

string[] replStyleSubcommands =
[
    "preview",
    "theme"
];

var replHistory = new List<string>();

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
var colorTheme = CliColorTheme.SlateBlueVivid;
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

            var exec = ExecuteReplCommand(line, machine, workflow, ref sessionInstance, ref instancePath, renderer, ref outputMode, ref symbolMode, ref colorTheme, isInteractive: false);
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
        var input = ReadReplInput(renderer, sessionInstance.CurrentState, symbolMode, workflow, replTopLevelCommands, replSymbolsSubcommands, replStyleSubcommands, replHistory);
        if (input is null)
            return ExitSuccess;
        if (string.IsNullOrWhiteSpace(input))
            continue;

        var exec = ExecuteReplCommand(input, machine, workflow, ref sessionInstance, ref instancePath, renderer, ref outputMode, ref symbolMode, ref colorTheme, isInteractive: true);
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
    DslMachine machine,
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

    if (command == "clear")
    {
        renderer.ClearScreen();
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
                renderer.Warning("Usage: style theme <name|list>");
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
            if (isInteractive)
            {
                RenderInspectAllInteractive(sessionInstance, machine, workflow, symbolMode, renderer);
                return ReplExecutionResult.Success();
            }

            var inspections = new List<DslInspectionResult>();
            foreach (var evt in workflow.Events)
                inspections.Add(workflow.Inspect(sessionInstance, evt.Name, null));

            RenderInspectAll(sessionInstance, machine, workflow, inspections, outputMode, symbolMode, renderer, isInteractive);
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
            if (isInteractive)
            {
                RenderInspectInteractive(sessionInstance, machine, workflow, eventName, eventArgs, symbolMode, renderer);
                return ReplExecutionResult.Success();
            }

            var inspect = workflow.Inspect(sessionInstance, eventName, eventArgs);
            RenderInspect(inspect, machine, workflow, outputMode, symbolMode, renderer, isInteractive);
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

static void RenderInspect(
    DslInspectionResult inspect,
    DslMachine machine,
    DslWorkflowDefinition workflow,
    OutputMode mode,
    SymbolMode symbolMode,
    CliRenderer renderer,
    bool isInteractive)
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

    var displayEvent = BuildInspectEventLabel(workflow, inspect.EventName);
    var possibleTargets = GetPossibleTargetStates(machine, inspect.CurrentState, inspect.EventName);
    var hasMultipleTargets = possibleTargets.Count > 1;

    if (inspect.Outcome == DslOutcomeKind.Blocked)
    {
        var reason = inspect.Reasons.FirstOrDefault() ?? "Rejected.";
        if (isInteractive)
            renderer.Warning(renderer.CompactResult(symbolMode, $"{displayEvent} {CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {reason}"));
        else
            renderer.Warning($"{CompactSymbols.Warn(symbolMode)} inspect {inspect.EventName}: {reason}");

        if (isInteractive && hasMultipleTargets)
        {
            for (int i = 0; i < possibleTargets.Count; i++)
                renderer.ChildTargetLine(possibleTargets[i], symbolMode, parentIsLast: true, isLastChild: i == possibleTargets.Count - 1, highlightArrowAsWarning: true);
        }

        return;
    }

    if (isInteractive)
    {
        if (hasMultipleTargets)
        {
            renderer.EventValue(displayEvent, symbolMode, isLast: true);
            for (int i = 0; i < possibleTargets.Count; i++)
                renderer.ChildTargetLine(possibleTargets[i], symbolMode, parentIsLast: true, isLastChild: i == possibleTargets.Count - 1);
        }
        else
        {
            renderer.PreviewEventStateLine(displayEvent, inspect.TargetState ?? "<none>", symbolMode, isLast: true);
        }
    }
    else
        renderer.Success($"{CompactSymbols.Ok(symbolMode)} inspect {inspect.EventName} {CompactSymbols.Arrow(symbolMode)} {inspect.TargetState}");
}

static void RenderInspectInteractive(
    DslWorkflowInstance instance,
    DslMachine machine,
    DslWorkflowDefinition workflow,
    string eventName,
    IReadOnlyDictionary<string, object?>? eventArgs,
    SymbolMode symbolMode,
    CliRenderer renderer)
{
    var preview = AnalyzeInspectPreview(instance, machine, workflow, eventName, eventArgs);

    if (preview.Outcome == DslOutcomeKind.Undefined)
    {
        var undefinedMessage = DescribeUndefinedOutcome(eventName, instance.CurrentState, preview.Reason ?? "Not defined.");
        renderer.EventErrorLine(
            BuildInspectEventLabel(workflow, eventName),
            $"{CompactSymbols.Err(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {undefinedMessage}",
            symbolMode,
            isLast: true);
        return;
    }

    if (preview.Outcome == DslOutcomeKind.Blocked)
    {
        var reason = preview.Reason ?? "Rejected.";
        renderer.EventOutcomeLine(
            preview.DisplayEvent,
            $"{CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {reason}",
            symbolMode,
            isLast: true);

        if (preview.PossibleTargets.Count > 0)
        {
            for (int i = 0; i < preview.PossibleTargets.Count; i++)
                renderer.ChildTargetLine(preview.PossibleTargets[i], symbolMode, parentIsLast: true, isLastChild: i == preview.PossibleTargets.Count - 1, highlightArrowAsWarning: true);
        }

        return;
    }

    if (preview.PossibleTargets.Count > 1)
    {
        renderer.EventValue(preview.DisplayEvent, symbolMode, isLast: true);

        for (int i = 0; i < preview.PossibleTargets.Count; i++)
            renderer.ChildTargetLine(
                preview.PossibleTargets[i],
                symbolMode,
                parentIsLast: true,
                isLastChild: i == preview.PossibleTargets.Count - 1,
                arrow: string.Equals(preview.PossibleTargets[i], preview.TargetState, StringComparison.Ordinal)
                    ? CompactSymbols.PreviewArrow(symbolMode)
                    : CompactSymbols.UnreachableArrow(symbolMode));
        return;
    }

    renderer.PreviewEventStateLine(preview.DisplayEvent, preview.TargetState ?? "<none>", symbolMode, isLast: true);
}

static void RenderInspectAllInteractive(
    DslWorkflowInstance instance,
    DslMachine machine,
    DslWorkflowDefinition workflow,
    SymbolMode symbolMode,
    CliRenderer renderer)
{
    var previews = workflow.Events
        .Select(evt => AnalyzeInspectPreview(instance, machine, workflow, evt.Name, null))
        .Where(p => p.Outcome != DslOutcomeKind.Undefined)
        .OrderBy(p => p.DisplayEvent, StringComparer.Ordinal)
        .ToList();

    if (previews.Count == 0)
    {
        renderer.Warning(renderer.CompactResult(symbolMode, $"{CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} no callable events from {instance.CurrentState}"));
        return;
    }

    const int eventColumnWidth = 0;

    for (int index = 0; index < previews.Count; index++)
    {
        var preview = previews[index];
        var isLast = index == previews.Count - 1;

        if (preview.Outcome == DslOutcomeKind.Blocked)
        {
            var reason = preview.Reason ?? "Rejected.";
            var outcome = $"{CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {reason}";
            renderer.EventOutcomeLine(preview.DisplayEvent, outcome, symbolMode, eventColumnWidth, isLast);
        }
        else if (preview.PossibleTargets.Count > 1)
        {
            renderer.EventValue(preview.DisplayEvent, symbolMode, eventColumnWidth, isLast);
        }
        else
        {
            renderer.EventStateLine(
                preview.DisplayEvent,
                preview.TargetState ?? "<none>",
                CompactSymbols.PreviewArrow(symbolMode),
                symbolMode,
                eventColumnWidth,
                isLast);
        }

        var shouldRenderChildTargets =
            preview.Outcome == DslOutcomeKind.Blocked ||
            preview.PossibleTargets.Count > 1;

        if (shouldRenderChildTargets && preview.PossibleTargets.Count > 0)
        {
            var childTargets = preview.PossibleTargets.ToArray();

            for (int childIndex = 0; childIndex < childTargets.Length; childIndex++)
                renderer.ChildTargetLine(
                    childTargets[childIndex],
                    symbolMode,
                    parentIsLast: isLast,
                    isLastChild: childIndex == childTargets.Length - 1,
                    highlightArrowAsWarning: preview.Outcome == DslOutcomeKind.Blocked,
                    arrow: preview.Outcome == DslOutcomeKind.Enabled && preview.PossibleTargets.Count > 1
                        ? (string.Equals(childTargets[childIndex], preview.TargetState, StringComparison.Ordinal)
                            ? CompactSymbols.PreviewArrow(symbolMode)
                            : CompactSymbols.UnreachableArrow(symbolMode))
                        : CompactSymbols.PreviewArrow(symbolMode));
        }
    }
}

static void RenderInspectAll(
    DslWorkflowInstance instance,
    DslMachine machine,
    DslWorkflowDefinition workflow,
    IReadOnlyCollection<DslInspectionResult> inspections,
    OutputMode mode,
    SymbolMode symbolMode,
    CliRenderer renderer,
    bool isInteractive)
{
    var currentState = instance.CurrentState;

    var callable = inspections
        .Where(i => i.Outcome == DslOutcomeKind.Enabled)
        .OrderBy(i => i.EventName, StringComparer.Ordinal)
        .ToArray();

    var guarded = inspections
        .Where(i => i.Outcome == DslOutcomeKind.Blocked)
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

    if (callable.Length == 0 && guarded.Length == 0)
    {
        if (isInteractive)
            renderer.Warning(renderer.CompactResult(symbolMode, $"{CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} no callable events from {currentState}"));
        else
            renderer.Warning($"{CompactSymbols.Warn(symbolMode)} inspect: no callable events from {currentState}");
        return;
    }

    if (!isInteractive)
        renderer.Success($"{CompactSymbols.Ok(symbolMode)} inspect: callable events from {currentState}");
    var enabledRows = callable
        .Select(i => new
        {
            DisplayEvent = BuildInspectEventLabel(workflow, i.EventName),
            EventName = i.EventName,
            TargetState = i.TargetState ?? "<none>"
        })
        .ToList();

    var guardedRows = guarded
        .Select(i => new
        {
            DisplayEvent = BuildInspectEventLabel(workflow, i.EventName),
            EventName = i.EventName,
            Reason = i.Reasons.FirstOrDefault() ?? "Rejected."
        })
        .ToList();

    var totalRows = enabledRows.Count + guardedRows.Count;
    var eventColumnWidth = totalRows == 0
        ? 0
        : enabledRows.Select(r => r.DisplayEvent)
            .Concat(guardedRows.Select(r => r.DisplayEvent))
            .Max(name => name.Length);

    var rowIndex = 0;
    foreach (var row in enabledRows)
    {
        var possibleTargets = GetPossibleTargetStates(machine, currentState, row.EventName);
        var hasMultipleTargets = possibleTargets.Count > 1;

        if (isInteractive)
        {
            if (hasMultipleTargets)
            {
                var parentIsLast = rowIndex == totalRows - 1;
                renderer.EventValue(
                    row.DisplayEvent,
                    symbolMode,
                    eventColumnWidth,
                    isLast: parentIsLast);

                for (int i = 0; i < possibleTargets.Count; i++)
                    renderer.ChildTargetLine(
                        possibleTargets[i],
                        symbolMode,
                        parentIsLast,
                        isLastChild: i == possibleTargets.Count - 1);
            }
            else
            {
                renderer.EventStateLine(
                    row.DisplayEvent,
                    row.TargetState,
                    CompactSymbols.PreviewArrow(symbolMode),
                    symbolMode,
                    eventColumnWidth,
                    isLast: rowIndex == totalRows - 1);
            }
        }
        else
            renderer.Info($"{row.DisplayEvent} {CompactSymbols.Arrow(symbolMode)} {row.TargetState}");

        rowIndex++;
    }

    foreach (var row in guardedRows)
    {
        var possibleTargets = GetPossibleTargetStates(machine, currentState, row.EventName);
        var hasMultipleTargets = possibleTargets.Count > 1;
        var outcome = $"{CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {row.Reason}";

        if (isInteractive)
        {
            renderer.EventOutcomeLine(
                row.DisplayEvent,
                outcome,
                symbolMode,
                eventColumnWidth,
                isLast: rowIndex == totalRows - 1);

            if (hasMultipleTargets)
            {
                var parentIsLast = rowIndex == totalRows - 1;
                for (int i = 0; i < possibleTargets.Count; i++)
                    renderer.ChildTargetLine(
                        possibleTargets[i],
                        symbolMode,
                        parentIsLast,
                        isLastChild: i == possibleTargets.Count - 1);
            }
        }
        else
            renderer.Warning($"{row.DisplayEvent} {outcome}");

        rowIndex++;
    }
}

static string BuildInspectEventLabel(
    DslWorkflowDefinition workflow,
    string eventName)
{
    var eventDefinition = workflow.Events.FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.Ordinal));
    if (eventDefinition is null)
        return eventName;

    var argNames = eventDefinition.Args
        .Select(a => a.Name)
        .ToArray();

    var label = argNames.Length > 0
        ? $"{eventDefinition.Name}({string.Join(",", argNames)})"
        : eventDefinition.Name;

    return label;
}

static (DslOutcomeKind Outcome, string DisplayEvent, string? TargetState, string? Reason, IReadOnlyList<string> PossibleTargets) AnalyzeInspectPreview(
    DslWorkflowInstance instance,
    DslMachine machine,
    DslWorkflowDefinition workflow,
    string eventName,
    IReadOnlyDictionary<string, object?>? eventArgs)
{
    var displayEvent = BuildInspectEventLabel(workflow, eventName);
    var requiredArgs = GetRequiredEventArgumentKeys(machine, eventName);
    var missingRequiredArgs = requiredArgs
        .Where(arg => eventArgs is null || !eventArgs.ContainsKey(arg))
        .ToArray();

    var usesMissingArgsInGuards = missingRequiredArgs.Any(arg => EventGuardsReferenceArg(machine, instance.CurrentState, eventName, arg));

    if (!usesMissingArgsInGuards)
    {
        var evaluationData = BuildInspectEvaluationData(instance.InstanceData, eventArgs);
        var eagerInspect = workflow.Inspect(instance.CurrentState, eventName, evaluationData);
        var eagerReason = eagerInspect.Reasons.FirstOrDefault();
        var eagerPossibleTargets = GetPossibleTargetStates(machine, instance.CurrentState, eventName);

        return (
            eagerInspect.Outcome,
            displayEvent,
            eagerInspect.TargetState,
            eagerReason,
            eagerPossibleTargets);
    }

    var terminalRule = machine.TerminalRules.FirstOrDefault(r =>
        string.Equals(r.FromState, instance.CurrentState, StringComparison.Ordinal)
        && string.Equals(r.EventName, eventName, StringComparison.Ordinal));

    var possibleTargets = GetPossibleTargetStates(machine, instance.CurrentState, eventName, includeNoTransitionAsCurrent: terminalRule?.Kind == DslTerminalKind.NoTransition);
    var reason = terminalRule?.Kind == DslTerminalKind.Reject
        ? terminalRule.Reason
        : null;

    if (!string.IsNullOrWhiteSpace(reason))
    {
        return (
            DslOutcomeKind.Blocked,
            displayEvent,
            null,
            reason,
            possibleTargets);
    }

    if (possibleTargets.Count == 1)
    {
        return (
            DslOutcomeKind.Enabled,
            displayEvent,
            possibleTargets[0],
            null,
            possibleTargets);
    }

    return (
        DslOutcomeKind.Enabled,
        displayEvent,
        null,
        null,
        possibleTargets);
}

static IReadOnlyList<string> GetRequiredEventArgumentKeys(DslMachine machine, string eventName)
{
    var eventDefinition = machine.Events.FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.Ordinal));
    if (eventDefinition is null)
        return Array.Empty<string>();

    return eventDefinition.Args
        .Where(a => !a.IsNullable)
        .Select(a => a.Name)
        .ToArray();
}

static bool EventGuardsReferenceArg(DslMachine machine, string currentState, string eventName, string argName)
{
    var explicitScoped = $"arg.{argName}";
    var bareIdentifierRegex = new Regex($@"\b{Regex.Escape(argName)}\b", RegexOptions.Compiled);

    return machine.Transitions
        .Where(t => string.Equals(t.FromState, currentState, StringComparison.Ordinal)
            && string.Equals(t.EventName, eventName, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(t.GuardExpression))
        .Any(t =>
        {
            var guard = t.GuardExpression!;
            return guard.Contains(explicitScoped, StringComparison.Ordinal) || bareIdentifierRegex.IsMatch(guard);
        });
}

static IReadOnlyDictionary<string, object?> BuildInspectEvaluationData(
    IReadOnlyDictionary<string, object?> instanceData,
    IReadOnlyDictionary<string, object?>? eventArguments)
{
    var evaluation = new Dictionary<string, object?>(StringComparer.Ordinal);

    foreach (var kvp in instanceData)
    {
        evaluation[$"data.{kvp.Key}"] = kvp.Value;
        if (!evaluation.ContainsKey(kvp.Key))
            evaluation[kvp.Key] = kvp.Value;
    }

    if (eventArguments is not null)
    {
        foreach (var kvp in eventArguments)
        {
            evaluation[$"arg.{kvp.Key}"] = kvp.Value;
            evaluation[kvp.Key] = kvp.Value;
        }
    }

    return evaluation;
}

static IReadOnlyList<string> GetPossibleTargetStates(
    DslMachine machine,
    string currentState,
    string eventName,
    bool includeNoTransitionAsCurrent = false)
{
    var targets = machine.Transitions
        .Where(t => string.Equals(t.FromState, currentState, StringComparison.Ordinal)
            && string.Equals(t.EventName, eventName, StringComparison.Ordinal))
        .Select(t => t.ToState)
        .Distinct(StringComparer.Ordinal)
        .ToList();

    if (includeNoTransitionAsCurrent && !targets.Contains(currentState, StringComparer.Ordinal))
        targets.Add(currentState);

    return targets.ToArray();
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
            renderer.EventErrorLine(
                fire.EventName,
                $"{CompactSymbols.Err(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {undefinedMessage}",
                symbolMode,
                isLast: true);
        else
            renderer.Error($"{CompactSymbols.Err(symbolMode)} fire {fire.EventName}: {undefinedMessage} ({reason})");
        return;
    }

    if (fire.Outcome == DslOutcomeKind.Blocked)
    {
        var reason = fire.Reasons.FirstOrDefault() ?? "Rejected.";
        if (isInteractive)
            renderer.EventOutcomeLine(
                fire.EventName,
                $"{CompactSymbols.Warn(symbolMode)} {CompactSymbols.Pipe(symbolMode)} {reason}",
                symbolMode,
                isLast: true);
        else
            renderer.Warning($"{CompactSymbols.Warn(symbolMode)} fire {fire.EventName}: blocked ({reason})");
        return;
    }

    if (isInteractive)
        renderer.EventSuccessStateLine(
            fire.EventName,
            fire.NewState ?? "<none>",
            symbolMode,
            isLast: true);
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
    renderer.Info("  clear");
    renderer.Info("    clear the terminal screen");
    renderer.Info("  symbols [auto|ascii|unicode|test]");
    renderer.Info("    test prints a symbol compatibility matrix for your terminal/font");
    renderer.Info("  style preview [all]");
    renderer.Info("    prints a terminal style sample using current output mode (compact/verbose), or all themes");
    renderer.Info("  style theme <name|list>");
    renderer.Info("    set or list available color themes");
    renderer.Info("  tab-completion");
    renderer.Info("    tab suggests commands/events/style themes/symbol modes in interactive REPL");
    renderer.Info("  history navigation");
    renderer.Info("    Up/Down arrows browse command history; Right Arrow accepts inline completion");
    renderer.Info("  type-ahead");
    renderer.Info("    while typing, REPL shows inline completion hints for current token");
    renderer.Info("  state");
    renderer.Info("  events");
    renderer.Info("  data");
    renderer.Info("  inspect [EventName] [event-args-json]");
    renderer.Info("    without EventName, inspects all events and lists callable plus guarded ones");
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

static string? ReadReplInput(
    CliRenderer renderer,
    string currentState,
    SymbolMode symbolMode,
    DslWorkflowDefinition workflow,
    IReadOnlyList<string> topLevelCommands,
    IReadOnlyList<string> symbolsSubcommands,
    IReadOnlyList<string> styleSubcommands,
    List<string> history)
{
    if (Console.IsInputRedirected)
        return Console.ReadLine();

    var buffer = new StringBuilder();
    int? historyIndex = null;
    string draftInput = string.Empty;

    RefreshInputLine(renderer, currentState, symbolMode, buffer, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands, showTypeAhead: true);

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            var result = buffer.ToString();
            if (!string.IsNullOrWhiteSpace(result))
            {
                history.Add(result);
                if (history.Count > 200)
                    history.RemoveAt(0);
            }

            return result;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (buffer.Length == 0)
                continue;

            buffer.Length--;
            RefreshInputLine(renderer, currentState, symbolMode, buffer, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands, showTypeAhead: true);
            continue;
        }

        if (key.Key == ConsoleKey.UpArrow)
        {
            if (history.Count == 0)
                continue;

            if (historyIndex is null)
            {
                draftInput = buffer.ToString();
                historyIndex = history.Count - 1;
            }
            else if (historyIndex > 0)
            {
                historyIndex--;
            }

            ReplaceInputBuffer(buffer, history[historyIndex.Value], renderer, currentState, symbolMode, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands);
            continue;
        }

        if (key.Key == ConsoleKey.DownArrow)
        {
            if (historyIndex is null)
                continue;

            if (historyIndex < history.Count - 1)
            {
                historyIndex++;
                ReplaceInputBuffer(buffer, history[historyIndex.Value], renderer, currentState, symbolMode, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands);
            }
            else
            {
                historyIndex = null;
                ReplaceInputBuffer(buffer, draftInput, renderer, currentState, symbolMode, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands);
            }

            continue;
        }

        if (key.Key == ConsoleKey.Tab)
        {
            ApplyTabCompletion(buffer, renderer, currentState, symbolMode, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands);
            RefreshInputLine(renderer, currentState, symbolMode, buffer, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands, showTypeAhead: true);
            continue;
        }

        if (key.Key == ConsoleKey.RightArrow)
        {
            TryAcceptInlineSuggestion(buffer, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands);
            RefreshInputLine(renderer, currentState, symbolMode, buffer, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands, showTypeAhead: true);
            continue;
        }

        if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            Console.WriteLine();
            return null;
        }

        var ch = key.KeyChar;
        if (char.IsControl(ch))
            continue;

        buffer.Append(ch);
        RefreshInputLine(renderer, currentState, symbolMode, buffer, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands, showTypeAhead: true);
        historyIndex = null;
    }
}

static void ReplaceInputBuffer(
    StringBuilder buffer,
    string value,
    CliRenderer renderer,
    string currentState,
    SymbolMode symbolMode,
    DslWorkflowDefinition workflow,
    IReadOnlyList<string> topLevelCommands,
    IReadOnlyList<string> symbolsSubcommands,
    IReadOnlyList<string> styleSubcommands)
{
    buffer.Clear();
    buffer.Append(value);

    RefreshInputLine(renderer, currentState, symbolMode, buffer, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands, showTypeAhead: true);
}

static void RefreshInputLine(
    CliRenderer renderer,
    string currentState,
    SymbolMode symbolMode,
    StringBuilder buffer,
    DslWorkflowDefinition workflow,
    IReadOnlyList<string> topLevelCommands,
    IReadOnlyList<string> symbolsSubcommands,
    IReadOnlyList<string> styleSubcommands,
    bool showTypeAhead)
{
    var value = buffer.ToString();

    Console.Write('\r');
    var width = TryGetConsoleWidth();
    if (width > 0)
        Console.Write(new string(' ', width - 1));
    Console.Write('\r');

    renderer.StatePrompt(currentState, symbolMode);
    Console.Write(value);

    if (!showTypeAhead)
        return;

    var suggestionSuffix = GetTypeAheadSuffix(value, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands);
    if (string.IsNullOrEmpty(suggestionSuffix))
        return;

    renderer.Prompt(suggestionSuffix);
    Console.Write(new string('\b', suggestionSuffix.Length));
}

static int TryGetConsoleWidth()
{
    try
    {
        return Console.BufferWidth;
    }
    catch
    {
        return 120;
    }
}

static void TryAcceptInlineSuggestion(
    StringBuilder buffer,
    DslWorkflowDefinition workflow,
    IReadOnlyList<string> topLevelCommands,
    IReadOnlyList<string> symbolsSubcommands,
    IReadOnlyList<string> styleSubcommands)
{
    var input = buffer.ToString();
    var completions = GetTabCompletions(input, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (completions.Length == 0)
        return;

    var endsWithWhitespace = input.Length > 0 && char.IsWhiteSpace(input[^1]);
    var fragment = endsWithWhitespace ? string.Empty : GetLastTokenFragment(input);

    if (completions.Length == 1)
    {
        var completion = completions[0];
        if (fragment.Length == 0)
        {
            var appendWhole = completion + " ";
            buffer.Append(appendWhole);
            Console.Write(appendWhole);
            return;
        }

        if (completion.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
            ReplaceTrailingFragment(buffer, fragment, completion);

        return;
    }

    if (fragment.Length == 0)
        return;

    var commonPrefix = GetCommonPrefix(completions);
    if (commonPrefix.Length <= fragment.Length)
        return;

    if (!commonPrefix.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
        return;

    ReplaceTrailingFragment(buffer, fragment, commonPrefix);
}

static string GetTypeAheadSuffix(
    string input,
    DslWorkflowDefinition workflow,
    IReadOnlyList<string> topLevelCommands,
    IReadOnlyList<string> symbolsSubcommands,
    IReadOnlyList<string> styleSubcommands)
{
    if (string.IsNullOrWhiteSpace(input))
        return string.Empty;

    var endsWithWhitespace = input.Length > 0 && char.IsWhiteSpace(input[^1]);
    if (endsWithWhitespace)
        return string.Empty;

    var fragment = GetLastTokenFragment(input);
    if (string.IsNullOrEmpty(fragment))
        return string.Empty;

    var completions = GetTabCompletions(input, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (completions.Length == 0)
        return string.Empty;

    if (completions.Length == 1)
    {
        var completion = completions[0];
        if (!completion.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return completion.Length > fragment.Length
            ? completion.Substring(fragment.Length)
            : string.Empty;
    }

    var commonPrefix = GetCommonPrefix(completions);
    if (!commonPrefix.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
        return string.Empty;

    return commonPrefix.Length > fragment.Length
        ? commonPrefix.Substring(fragment.Length)
        : string.Empty;
}

static void ReplaceTrailingFragment(StringBuilder buffer, string fragment, string replacement)
{
    if (fragment.Length > 0)
    {
        for (var index = 0; index < fragment.Length; index++)
            Console.Write("\b");

        Console.Write(new string(' ', fragment.Length));

        for (var index = 0; index < fragment.Length; index++)
            Console.Write("\b");

        buffer.Length -= fragment.Length;
    }

    buffer.Append(replacement);
    Console.Write(replacement);
}

static string GetCommonPrefix(IReadOnlyList<string> values)
{
    if (values.Count == 0)
        return string.Empty;

    var prefix = values[0];
    for (var index = 1; index < values.Count; index++)
    {
        var candidate = values[index];
        var length = Math.Min(prefix.Length, candidate.Length);
        var match = 0;
        while (match < length && char.ToLowerInvariant(prefix[match]) == char.ToLowerInvariant(candidate[match]))
            match++;

        prefix = prefix[..match];
        if (prefix.Length == 0)
            break;
    }

    return prefix;
}

static void ApplyTabCompletion(
    StringBuilder buffer,
    CliRenderer renderer,
    string currentState,
    SymbolMode symbolMode,
    DslWorkflowDefinition workflow,
    IReadOnlyList<string> topLevelCommands,
    IReadOnlyList<string> symbolsSubcommands,
    IReadOnlyList<string> styleSubcommands)
{
    var input = buffer.ToString();
    var completions = GetTabCompletions(input, workflow, topLevelCommands, symbolsSubcommands, styleSubcommands)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (completions.Length == 0)
        return;

    var endsWithWhitespace = input.Length > 0 && char.IsWhiteSpace(input[^1]);
    var fragment = endsWithWhitespace ? string.Empty : GetLastTokenFragment(input);

    if (completions.Length == 1)
    {
        var completion = completions[0];

        if (fragment.Length == 0)
        {
            var append = completion + " ";
            buffer.Append(append);
            Console.Write(append);
            return;
        }

        if (completion.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
            ReplaceTrailingFragment(buffer, fragment, completion);

        if (buffer.Length == 0 || !char.IsWhiteSpace(buffer[^1]))
        {
            buffer.Append(' ');
            Console.Write(' ');
        }

        return;
    }

    Console.WriteLine();
    renderer.Meta($"suggestions: {string.Join(", ", completions)}");
    renderer.StatePrompt(currentState, symbolMode);
    Console.Write(input);
}

static IEnumerable<string> GetTabCompletions(
    string input,
    DslWorkflowDefinition workflow,
    IReadOnlyList<string> topLevelCommands,
    IReadOnlyList<string> symbolsSubcommands,
    IReadOnlyList<string> styleSubcommands)
{
    var endsWithWhitespace = input.Length > 0 && char.IsWhiteSpace(input[^1]);
    var tokens = SplitCompletionTokens(input);

    var activeTokenIndex = endsWithWhitespace
        ? tokens.Count
        : Math.Max(0, tokens.Count - 1);

    var fragment = endsWithWhitespace
        ? string.Empty
        : tokens.Count == 0
            ? string.Empty
            : tokens[^1];

    IEnumerable<string> candidates;

    if (activeTokenIndex == 0)
    {
        candidates = topLevelCommands;
        return candidates.Where(c => c.StartsWith(fragment, StringComparison.OrdinalIgnoreCase));
    }

    var command = tokens[0].ToLowerInvariant();

    candidates = command switch
    {
        "symbols" when activeTokenIndex == 1 => symbolsSubcommands,
        "style" when activeTokenIndex == 1 => styleSubcommands,
        "style" when activeTokenIndex == 2 && tokens.Count > 1 && tokens[1].Equals("preview", StringComparison.OrdinalIgnoreCase)
            => new[] { "all" },
        "style" when activeTokenIndex == 2 && tokens.Count > 1 && tokens[1].Equals("theme", StringComparison.OrdinalIgnoreCase)
            => CliColorThemes.Tokens,
        "inspect" when activeTokenIndex == 1 => workflow.Events.Select(e => e.Name),
        "fire" when activeTokenIndex == 1 => workflow.Events.Select(e => e.Name),
        _ => Array.Empty<string>()
    };

    return candidates.Where(c => c.StartsWith(fragment, StringComparison.OrdinalIgnoreCase));
}

static List<string> SplitCompletionTokens(string input)
{
    return input
        .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
        .ToList();
}

static string GetLastTokenFragment(string input)
{
    if (string.IsNullOrEmpty(input))
        return string.Empty;

    var index = input.Length - 1;
    while (index >= 0 && !char.IsWhiteSpace(input[index]))
        index--;

    return input[(index + 1)..];
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
    MonoAccent,
    Dracula,
    RosePine,
    Everforest,
    CatppuccinMocha,
    OneDarkPro,
    GruvboxDark,
    MaterialOcean,
    NightOwl,
    Palenight,
    Cobalt2,
    AyuMirage,
    HorizonDark,
    KanagawaWave,
    Synthwave84,
    MonokaiPro,
    SepiaSoft,
    ForestNight,
    Iceberg,
    Carbon,
    NeonMint,
    Ember,
    LavenderMist,
    SlateBlue,
    SlateBlueVivid
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
        "mono-accent",
        "muted",
        "nord-crisp",
        "tokyo-night",
        "github-dark",
        "solarized-modern",
        "dracula",
        "rose-pine",
        "everforest",
        "catppuccin-mocha",
        "one-dark-pro",
        "gruvbox-dark",
        "material-ocean",
        "night-owl",
        "palenight",
        "cobalt2",
        "ayu-mirage",
        "horizon-dark",
        "kanagawa-wave",
        "synthwave-84",
        "monokai-pro",
        "sepia-soft",
        "forest-night",
        "iceberg",
        "carbon",
        "neon-mint",
        "ember",
        "lavender-mist",
        "slate-blue",
        "slate-blue-vivid"
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
            "dracula" => CliColorTheme.Dracula,
            "rose-pine" or "rosepine" => CliColorTheme.RosePine,
            "everforest" => CliColorTheme.Everforest,
            "catppuccin-mocha" or "catppuccin" => CliColorTheme.CatppuccinMocha,
            "one-dark-pro" or "onedark" => CliColorTheme.OneDarkPro,
            "gruvbox-dark" or "gruvbox" => CliColorTheme.GruvboxDark,
            "material-ocean" or "material" => CliColorTheme.MaterialOcean,
            "night-owl" => CliColorTheme.NightOwl,
            "palenight" => CliColorTheme.Palenight,
            "cobalt2" => CliColorTheme.Cobalt2,
            "ayu-mirage" or "ayu" => CliColorTheme.AyuMirage,
            "horizon-dark" or "horizon" => CliColorTheme.HorizonDark,
            "kanagawa-wave" or "kanagawa" => CliColorTheme.KanagawaWave,
            "synthwave-84" or "synthwave" => CliColorTheme.Synthwave84,
            "monokai-pro" or "monokai" => CliColorTheme.MonokaiPro,
            "sepia-soft" or "sepia" => CliColorTheme.SepiaSoft,
            "forest-night" => CliColorTheme.ForestNight,
            "iceberg" => CliColorTheme.Iceberg,
            "carbon" => CliColorTheme.Carbon,
            "neon-mint" => CliColorTheme.NeonMint,
            "ember" => CliColorTheme.Ember,
            "lavender-mist" or "lavender" => CliColorTheme.LavenderMist,
            "slate-blue" or "slate" => CliColorTheme.SlateBlue,
            "slate-blue-vivid" or "slate-vivid" => CliColorTheme.SlateBlueVivid,
            _ => default
        };

        return normalized is
            "muted" or
            "nord-crisp" or "nord" or
            "tokyo-night" or "tokyo" or
            "github-dark" or "github" or
            "solarized-modern" or "solarized" or
            "mono-accent" or "mono" or
            "dracula" or
            "rose-pine" or "rosepine" or
            "everforest" or
            "catppuccin-mocha" or "catppuccin" or
            "one-dark-pro" or "onedark" or
            "gruvbox-dark" or "gruvbox" or
            "material-ocean" or "material" or
            "night-owl" or
            "palenight" or
            "cobalt2" or
            "ayu-mirage" or "ayu" or
            "horizon-dark" or "horizon" or
            "kanagawa-wave" or "kanagawa" or
            "synthwave-84" or "synthwave" or
            "monokai-pro" or "monokai" or
            "sepia-soft" or "sepia" or
            "forest-night" or
            "iceberg" or
            "carbon" or
            "neon-mint" or
            "ember" or
            "lavender-mist" or "lavender" or
                "slate-blue" or "slate" or
                "slate-blue-vivid" or "slate-vivid";
    }

    public static string ToToken(CliColorTheme theme) => theme switch
    {
        CliColorTheme.Muted => "muted",
        CliColorTheme.NordCrisp => "nord-crisp",
        CliColorTheme.TokyoNight => "tokyo-night",
        CliColorTheme.GithubDark => "github-dark",
        CliColorTheme.SolarizedModern => "solarized-modern",
        CliColorTheme.MonoAccent => "mono-accent",
        CliColorTheme.Dracula => "dracula",
        CliColorTheme.RosePine => "rose-pine",
        CliColorTheme.Everforest => "everforest",
        CliColorTheme.CatppuccinMocha => "catppuccin-mocha",
        CliColorTheme.OneDarkPro => "one-dark-pro",
        CliColorTheme.GruvboxDark => "gruvbox-dark",
        CliColorTheme.MaterialOcean => "material-ocean",
        CliColorTheme.NightOwl => "night-owl",
        CliColorTheme.Palenight => "palenight",
        CliColorTheme.Cobalt2 => "cobalt2",
        CliColorTheme.AyuMirage => "ayu-mirage",
        CliColorTheme.HorizonDark => "horizon-dark",
        CliColorTheme.KanagawaWave => "kanagawa-wave",
        CliColorTheme.Synthwave84 => "synthwave-84",
        CliColorTheme.MonokaiPro => "monokai-pro",
        CliColorTheme.SepiaSoft => "sepia-soft",
        CliColorTheme.ForestNight => "forest-night",
        CliColorTheme.Iceberg => "iceberg",
        CliColorTheme.Carbon => "carbon",
        CliColorTheme.NeonMint => "neon-mint",
        CliColorTheme.Ember => "ember",
        CliColorTheme.LavenderMist => "lavender-mist",
        CliColorTheme.SlateBlue => "slate-blue",
        CliColorTheme.SlateBlueVivid => "slate-blue-vivid",
        _ => "muted"
    };

    public static CliPalette GetPalette(CliColorTheme theme) => theme switch
    {
        CliColorTheme.NordCrisp => new CliPalette("#6B7280", "white", "#A3BE8C", "#EBCB8B", "#BF616A", "#88C0D0", "#B48EAD", "#A3BE8C", "#8FBCBB", "#88C0D0"),
        CliColorTheme.TokyoNight => new CliPalette("#A9B1D6", "white", "#9ECE6A", "#E0AF68", "#F7768E", "#7DCFFF", "#BB9AF7", "#9ECE6A", "#73DACA", "#7DCFFF"),
        CliColorTheme.GithubDark => new CliPalette("#8B949E", "white", "#56D364", "#F2CC60", "#F85149", "#79C0FF", "#D2A8FF", "#56D364", "#7EE787", "#79C0FF"),
        CliColorTheme.SolarizedModern => new CliPalette("#657B83", "white", "#859900", "#B58900", "#DC322F", "#268BD2", "#D33682", "#2AA198", "#6C71C4", "#268BD2"),
        CliColorTheme.MonoAccent => new CliPalette("#6B7280", "#E5E7EB", "#22C55E", "#EAB308", "#EF4444", "#D1D5DB", "#9CA3AF", "#D1D5DB", "#E5E7EB", "#D1D5DB"),
        CliColorTheme.Dracula => new CliPalette("#6272A4", "#F8F8F2", "#50FA7B", "#F1FA8C", "#FF5555", "#8BE9FD", "#FF79C6", "#50FA7B", "#BD93F9", "#8BE9FD"),
        CliColorTheme.RosePine => new CliPalette("#6E6A86", "#E0DEF4", "#9CCFD8", "#F6C177", "#EB6F92", "#C4A7E7", "#EBBCBA", "#9CCFD8", "#31748F", "#C4A7E7"),
        CliColorTheme.Everforest => new CliPalette("#7A8478", "#D3C6AA", "#A7C080", "#DBBC7F", "#E67E80", "#7FBBB3", "#D699B6", "#A7C080", "#83C092", "#7FBBB3"),
        CliColorTheme.CatppuccinMocha => new CliPalette("#6C7086", "#CDD6F4", "#A6E3A1", "#F9E2AF", "#F38BA8", "#89B4FA", "#CBA6F7", "#A6E3A1", "#94E2D5", "#89B4FA"),
        CliColorTheme.OneDarkPro => new CliPalette("#5C6370", "#ABB2BF", "#98C379", "#E5C07B", "#E06C75", "#61AFEF", "#C678DD", "#98C379", "#56B6C2", "#61AFEF"),
        CliColorTheme.GruvboxDark => new CliPalette("#7C6F64", "#EBDBB2", "#B8BB26", "#FABD2F", "#FB4934", "#83A598", "#D3869B", "#B8BB26", "#8EC07C", "#83A598"),
        CliColorTheme.MaterialOcean => new CliPalette("#717CB4", "#C8D3F5", "#C3E88D", "#FFCB6B", "#F07178", "#82AAFF", "#C792EA", "#C3E88D", "#89DDFF", "#82AAFF"),
        CliColorTheme.NightOwl => new CliPalette("#637777", "#D6DEEB", "#22DA6E", "#E3D18A", "#EF5350", "#82AAFF", "#C792EA", "#22DA6E", "#7FDBCA", "#82AAFF"),
        CliColorTheme.Palenight => new CliPalette("#676E95", "#A6ACCD", "#C3E88D", "#FFCB6B", "#F07178", "#82AAFF", "#C792EA", "#C3E88D", "#89DDFF", "#82AAFF"),
        CliColorTheme.Cobalt2 => new CliPalette("#6F93C6", "#FFFFFF", "#3AD900", "#FFC600", "#FF628C", "#9EFFFF", "#FF9D00", "#3AD900", "#80FFBB", "#9EFFFF"),
        CliColorTheme.AyuMirage => new CliPalette("#5C6773", "#CBCCC6", "#AAD94C", "#FFCC66", "#F28779", "#73D0FF", "#D4BFFF", "#AAD94C", "#95E6CB", "#73D0FF"),
        CliColorTheme.HorizonDark => new CliPalette("#6C6F93", "#D5C1A9", "#29D398", "#FAB795", "#E95678", "#26BBD9", "#EE64AE", "#29D398", "#59E3E3", "#26BBD9"),
        CliColorTheme.KanagawaWave => new CliPalette("#727169", "#DCD7BA", "#98BB6C", "#E6C384", "#E46876", "#7FB4CA", "#957FB8", "#98BB6C", "#7AA89F", "#7FB4CA"),
        CliColorTheme.Synthwave84 => new CliPalette("#7D77A8", "#F4EEE4", "#72F1B8", "#F2E863", "#FE4450", "#36F9F6", "#FF7EDB", "#72F1B8", "#9AE9FF", "#36F9F6"),
        CliColorTheme.MonokaiPro => new CliPalette("#727072", "#FCFCFA", "#A9DC76", "#FFD866", "#FF6188", "#78DCE8", "#AB9DF2", "#A9DC76", "#FC9867", "#78DCE8"),
        CliColorTheme.SepiaSoft => new CliPalette("#8B7E74", "#F2E7D5", "#95B46A", "#D9B86A", "#C26E5A", "#8FAFC7", "#B58FB6", "#95B46A", "#9BC1AA", "#8FAFC7"),
        CliColorTheme.ForestNight => new CliPalette("#6B7A6A", "#D8E2C8", "#8BCF7A", "#DCCB6A", "#E07070", "#7BB6B0", "#A58FD6", "#8BCF7A", "#9ED89C", "#7BB6B0"),
        CliColorTheme.Iceberg => new CliPalette("#6B7089", "#C6C8D1", "#B4BE82", "#E2A478", "#E27878", "#84A0C6", "#A093C7", "#B4BE82", "#89B8C2", "#84A0C6"),
        CliColorTheme.Carbon => new CliPalette("#6F7680", "#E6E8EB", "#42BE65", "#F1C21B", "#FA4D56", "#78A9FF", "#BE95FF", "#42BE65", "#3DDBD9", "#78A9FF"),
        CliColorTheme.NeonMint => new CliPalette("#7B7F8F", "#E8FFF7", "#5CFFB2", "#F7F779", "#FF6B8A", "#7BDFFF", "#D28CFF", "#5CFFB2", "#9CFFD9", "#7BDFFF"),
        CliColorTheme.Ember => new CliPalette("#8A6D5C", "#F5E9DE", "#9FD67A", "#F3C97A", "#FF7A5C", "#8FBCE6", "#C9A0DC", "#9FD67A", "#E9B872", "#8FBCE6"),
        CliColorTheme.LavenderMist => new CliPalette("#7A7891", "#EFEAFF", "#9ED89C", "#F3DE7A", "#F08AA8", "#9CB8FF", "#C9A7FF", "#9ED89C", "#BBD4FF", "#9CB8FF"),
        CliColorTheme.SlateBlue => new CliPalette("#6A7895", "#E3EAF8", "#8CCF9A", "#E7D37A", "#E5858A", "#7EA7E0", "#9F8FD6", "#8CCF9A", "#96C2F2", "#7EA7E0"),
        CliColorTheme.SlateBlueVivid => new CliPalette("#59657A", "white bold", "#1FFF7A", "#FFF12A", "#FF2A57", "#6D7F9B", "#8573A8", "#1FFF7A", "#7F92AF", "#6D7F9B"),
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

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.Meta}]{Markup.Escape(CompactIndent + prefix + " ")}[/][{_palette.Event}]{Markup.Escape(TagEvent(paddedEvent))}[/]");
            return;
        }

        Console.WriteLine($"{CompactIndent}{prefix} {TagEvent(paddedEvent)}");
    }

    public void StateValue(string stateName, SymbolMode symbolMode, bool isLast = true)
    {
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.Meta}]{Markup.Escape(CompactIndent + prefix + " ")}[/][{_palette.State}]{Markup.Escape(TagState(stateName))}[/]");
            return;
        }

        Console.WriteLine($"{CompactIndent}{prefix} {TagState(stateName)}");
    }

    public void EventStateLine(string eventName, string stateName, string connector, SymbolMode symbolMode, int eventColumnWidth = 0, bool isLast = false)
    {
        var paddedEvent = eventColumnWidth > 0 ? eventName.PadRight(eventColumnWidth) : eventName;
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        var structuralPrefix = $"{CompactIndent}{prefix} ";
        var eventLabel = TagEvent(paddedEvent);

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.Meta}]{Markup.Escape(structuralPrefix)}[/][{_palette.Event}]{Markup.Escape(eventLabel)}[/][{ResolveArrowColor(connector, symbolMode)}] {Markup.Escape(connector)} [/][{_palette.State}]{Markup.Escape(TagState(stateName))}[/]");
            return;
        }

        Console.WriteLine($"{structuralPrefix}{eventLabel} {connector} {TagState(stateName)}");
    }

    public void EventSuccessStateLine(string eventName, string stateName, SymbolMode symbolMode, int eventColumnWidth = 0, bool isLast = false)
    {
        var paddedEvent = eventColumnWidth > 0 ? eventName.PadRight(eventColumnWidth) : eventName;
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        var structuralPrefix = $"{CompactIndent}{prefix} ";
        var eventLabel = TagEvent(paddedEvent);
        var ok = CompactSymbols.Ok(symbolMode);
        var connector = CompactSymbols.Arrow(symbolMode);

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.Meta}]{Markup.Escape(structuralPrefix)}[/][{_palette.Event}]{Markup.Escape(eventLabel)}[/][{_palette.Success}] {Markup.Escape(ok)} {Markup.Escape(connector)} [/][{_palette.State}]{Markup.Escape(TagState(stateName))}[/]");
            return;
        }

        Console.WriteLine($"{structuralPrefix}{eventLabel} {ok} {connector} {TagState(stateName)}");
    }

    public void EventOutcomeLine(string eventName, string outcomeText, SymbolMode symbolMode, int eventColumnWidth = 0, bool isLast = false)
    {
        const int promptReserve = 12;
        var paddedEvent = eventColumnWidth > 0 ? eventName.PadRight(eventColumnWidth) : eventName;
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        var structuralPrefix = $"{CompactIndent}{prefix} ";
        var eventLabel = TagEvent(paddedEvent);

        var consoleWidth = GetRenderWidth();
        var availableWidth = Math.Max(1, consoleWidth - structuralPrefix.Length - eventLabel.Length - 1 - promptReserve);
        var renderedOutcome = TruncateWithEllipsis(outcomeText, availableWidth);

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.Meta}]{Markup.Escape(structuralPrefix)}[/][{_palette.Event}]{Markup.Escape(eventLabel)}[/][{_palette.Warning}] {Markup.Escape(renderedOutcome)}[/]");
            return;
        }

        Console.WriteLine($"{structuralPrefix}{eventLabel} {renderedOutcome}");
    }

    public void EventErrorLine(string eventName, string outcomeText, SymbolMode symbolMode, int eventColumnWidth = 0, bool isLast = false)
    {
        const int promptReserve = 12;
        var paddedEvent = eventColumnWidth > 0 ? eventName.PadRight(eventColumnWidth) : eventName;
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        var structuralPrefix = $"{CompactIndent}{prefix} ";
        var eventLabel = TagEvent(paddedEvent);

        var consoleWidth = GetRenderWidth();
        var availableWidth = Math.Max(1, consoleWidth - structuralPrefix.Length - eventLabel.Length - 1 - promptReserve);
        var renderedOutcome = TruncateWithEllipsis(outcomeText, availableWidth);

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.Meta}]{Markup.Escape(structuralPrefix)}[/][{_palette.Event}]{Markup.Escape(eventLabel)}[/][{_palette.Error}] {Markup.Escape(renderedOutcome)}[/]");
            return;
        }

        Console.WriteLine($"{structuralPrefix}{eventLabel} {renderedOutcome}");
    }

    private static string TruncateWithEllipsis(string text, int maxWidth)
    {
        var value = text ?? string.Empty;

        if (maxWidth <= 0 || value.Length <= maxWidth)
            return value;

        if (maxWidth <= 3)
            return new string('.', maxWidth);

        return value.Substring(0, maxWidth - 3) + "...";
    }

    private static int GetRenderWidth()
    {
        var candidates = new List<int>(3);

        if (AnsiConsole.Profile.Width > 0)
            candidates.Add(AnsiConsole.Profile.Width);

        var windowWidth = TryGetConsoleWidth(() => Console.WindowWidth);
        if (windowWidth > 0)
            candidates.Add(windowWidth);

        var bufferWidth = TryGetConsoleWidth(() => Console.BufferWidth);
        if (bufferWidth > 0)
            candidates.Add(bufferWidth);

        if (candidates.Count == 0)
            return 120;

        return Math.Max(40, candidates.Min());
    }

    private static int TryGetConsoleWidth(Func<int> getWidth)
    {
        try
        {
            return getWidth();
        }
        catch (IOException)
        {
            return 0;
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
    }

    public void ChildTargetLine(string stateName, SymbolMode symbolMode, bool parentIsLast, bool isLastChild, string? arrow = null, bool highlightArrowAsWarning = false)
    {
        var connector = parentIsLast
            ? $"{CompactIndent}    "
            : $"{CompactIndent}{CompactSymbols.BranchStem(symbolMode)}   ";
        var prefix = isLastChild ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        var arrowText = arrow ?? CompactSymbols.PreviewArrow(symbolMode);
        var line = $"{connector}{prefix} {arrowText} {TagState(stateName)}";

        if (_useColor)
        {
            var unreachableArrow = CompactSymbols.UnreachableArrow(symbolMode);
            var isUnreachable = string.Equals(arrowText, unreachableArrow, StringComparison.Ordinal);
            var arrowColor = isUnreachable
                ? _palette.Error
                : highlightArrowAsWarning
                    ? _palette.Warning
                    : _palette.Success;
            var stateColor = _palette.State;

            AnsiConsole.MarkupLine(
                $"[{_palette.Meta}]{Markup.Escape(connector + prefix)} [/][{arrowColor}]{Markup.Escape(arrowText)} [/][{stateColor}]{Markup.Escape(TagState(stateName))}[/]");
            return;
        }

        Console.WriteLine(line);
    }

    public void PreviewEventStateLine(string eventName, string stateName, SymbolMode symbolMode, bool isLast = true)
    {
        var connector = CompactSymbols.PreviewArrow(symbolMode);
        var prefix = isLast ? CompactSymbols.BranchEnd(symbolMode) : CompactSymbols.BranchMid(symbolMode);
        var structuralPrefix = $"{CompactIndent}{prefix} ";
        var eventLabel = eventName;

        if (_useColor)
        {
            AnsiConsole.MarkupLine(
                $"[{_palette.Meta}]{Markup.Escape(structuralPrefix)}[/][{_palette.Event}]{Markup.Escape(eventLabel)}[/][{ResolveArrowColor(connector, symbolMode)}] {Markup.Escape(connector)} [/][{_palette.State}]{Markup.Escape(TagState(stateName))}[/]");
            return;
        }

        Console.WriteLine($"{structuralPrefix}{eventLabel} {connector} {TagState(stateName)}");
    }

    private string ResolveArrowColor(string connector, SymbolMode symbolMode)
    {
        if (string.Equals(connector, CompactSymbols.Arrow(symbolMode), StringComparison.Ordinal))
            return _palette.Success;

        if (string.Equals(connector, CompactSymbols.PreviewArrow(symbolMode), StringComparison.Ordinal))
            return _palette.Success;

        if (string.Equals(connector, CompactSymbols.UnreachableArrow(symbolMode), StringComparison.Ordinal))
            return _palette.Error;

        return _palette.Meta;
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

    public void ClearScreen()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

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
        var warn = CompactSymbols.Warn(symbolMode);
        var err = CompactSymbols.Err(symbolMode);
        var pipe = CompactSymbols.Pipe(symbolMode);
        var promptArrow = CompactSymbols.Prompt(symbolMode);
        var branchStem = CompactSymbols.BranchStem(symbolMode);

        void CommandPrompt(string stateName, string command)
        {
            if (_useColor)
            {
                AnsiConsole.MarkupLine(
                    $"[{_palette.State}]{Markup.Escape(stateName)}[/][{_palette.Meta}] {Markup.Escape(promptArrow)}[/] {Markup.Escape(command)}");
                return;
            }

            Console.WriteLine($"{stateName} {promptArrow} {command}");
        }

        void ArgumentLine(string key, string value)
        {
            if (_useColor)
            {
                AnsiConsole.MarkupLine(
                    $"[{_palette.Meta}]{Markup.Escape(CompactIndent)}{Markup.Escape(branchStem)} [/][{_palette.Event}]{Markup.Escape(key)}[/][{_palette.Meta}]: {Markup.Escape(value)}[/]");
                return;
            }

            Console.WriteLine($"{CompactIndent}{branchStem} {key}: {value}");
        }

        Meta("Style preview transcript (compact matrix):");

        CommandPrompt("Red", "inspect");
        EventStateLine("Advance", "Green", CompactSymbols.PreviewArrow(symbolMode), symbolMode, isLast: false);
        EventValue("Route(Decision)", symbolMode, isLast: false);
        ChildTargetLine("Alpha", symbolMode, parentIsLast: false, isLastChild: false, arrow: CompactSymbols.PreviewArrow(symbolMode));
        ChildTargetLine("Beta", symbolMode, parentIsLast: false, isLastChild: true, arrow: CompactSymbols.UnreachableArrow(symbolMode));
        EventOutcomeLine(
            "Emergency(AuthorizedBy,Reason)",
            $"{warn} {pipe} AuthorizedBy and Reason are required to activate emergency mode",
            symbolMode,
            isLast: true);
        ChildTargetLine("FlashingRed", symbolMode, parentIsLast: true, isLastChild: true, arrow: CompactSymbols.PreviewArrow(symbolMode), highlightArrowAsWarning: true);

        CommandPrompt("Red", "inspect UnknownEvent");
        EventErrorLine(
            "UnknownEvent",
            $"{err} {pipe} unknown event",
            symbolMode,
            isLast: true);

        CommandPrompt("Red", "inspect ClearEmergency");
        EventErrorLine(
            "ClearEmergency",
            $"{err} {pipe} no transition from Red",
            symbolMode,
            isLast: true);

        CommandPrompt("Red", "fire Advance");
        EventSuccessStateLine("Advance", "Green", symbolMode, isLast: true);

        CommandPrompt("Green", "fire Advance");
        EventOutcomeLine(
            "Advance",
            $"{warn} {pipe} Operator confirmation is required before advancing from Green while maintenance override is active and pending supervisory review for safety compliance.",
            symbolMode,
            isLast: true);

        CommandPrompt("Green", "fire ClearEmergency");
        EventErrorLine(
            "ClearEmergency",
            $"{err} {pipe} no transition from Green",
            symbolMode,
            isLast: true);

        CommandPrompt("Green", "fire UnknownEvent");
        EventErrorLine(
            "UnknownEvent",
            $"{err} {pipe} unknown event",
            symbolMode,
            isLast: true);

        CommandPrompt("Red", "fire Emergency");
        ArgumentLine("AuthorizedBy", "Dispatcher");
        ArgumentLine("Reason", "Accident");
        EventSuccessStateLine("Emergency", "FlashingRed", symbolMode, isLast: true);
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
    public static string UnreachableArrow(SymbolMode mode) => Resolve(mode) == SymbolMode.Unicode ? "──✕" : "--X";

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
