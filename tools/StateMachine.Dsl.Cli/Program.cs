using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

            Console.WriteLine($"sm> {line}");
            var exec = ExecuteReplCommand(line, workflow, ref sessionInstance, ref instancePath);
            if (!exec.IsSuccess)
                return ExitScriptFailed;
            if (exec.ShouldExit)
                break;
        }

        return ExitSuccess;
    }

    Console.WriteLine($"Machine: {machine.Name}");
    Console.WriteLine($"State: {sessionInstance.CurrentState}");
    Console.WriteLine("Type 'help' for commands, 'exit' to quit.");

    while (true)
    {
        Console.Write("sm> ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            continue;

        var exec = ExecuteReplCommand(input, workflow, ref sessionInstance, ref instancePath);
        if (!exec.IsSuccess)
            Console.WriteLine("Command failed.");
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

static void PrintUsage()
{
    Console.WriteLine("StateMachine.Dsl.Cli");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dsl <file.sm> --instance <instance.json> [--script <commands.txt>]");
    Console.WriteLine("Notes:");
    Console.WriteLine("  Without --script, starts interactive REPL.");
    Console.WriteLine("  With --script, runs REPL commands from file non-interactively.");
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

    var context = new Dictionary<string, object?>(StringComparer.Ordinal);
    if (root.TryGetProperty("contextSnapshot", out var contextElement) && contextElement.ValueKind == JsonValueKind.Object)
    {
        foreach (var property in contextElement.EnumerateObject())
            context[property.Name] = ToDotNetValue(property.Value);
    }

    return new DslWorkflowInstance(workflowName, currentState, lastEvent, updatedAt, context);
}

static void SaveInstance(string path, DslWorkflowInstance instance)
{
    var envelope = new Dictionary<string, object?>
    {
        ["workflowName"] = instance.WorkflowName,
        ["currentState"] = instance.CurrentState,
        ["lastEvent"] = instance.LastEvent,
        ["updatedAt"] = instance.UpdatedAt,
        ["contextSnapshot"] = instance.ContextSnapshot
    };

    var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    File.WriteAllText(path, json);
}

static IReadOnlyDictionary<string, object?> ParseContext(string? contextOption, string? contextFileOption)
{
    bool hasInlineContext = !string.IsNullOrWhiteSpace(contextOption);
    bool hasContextFile = !string.IsNullOrWhiteSpace(contextFileOption);

    if (hasInlineContext && hasContextFile)
        throw new InvalidOperationException("Use either --context or --context-file, not both.");

    if (!hasInlineContext && !hasContextFile)
        return new Dictionary<string, object?>(StringComparer.Ordinal);

    var contextText = hasContextFile
        ? File.ReadAllText(contextFileOption!)
        : contextOption!;

    using var doc = JsonDocument.Parse(contextText);
    if (doc.RootElement.ValueKind != JsonValueKind.Object)
        throw new InvalidOperationException("--context must be a JSON object or a file containing a JSON object.");

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
    ref string? sessionInstancePath)
{
    var tokens = Tokenize(input);
    if (tokens.Count == 0)
        return ReplExecutionResult.Success();

    var command = tokens[0].ToLowerInvariant();

    if (command is "exit" or "quit")
        return ReplExecutionResult.Exit();

    if (command == "help")
    {
        PrintReplHelp();
        return ReplExecutionResult.Success();
    }

    if (command == "state")
    {
        Console.WriteLine(sessionInstance.CurrentState);
        return ReplExecutionResult.Success();
    }

    if (command == "events")
    {
        foreach (var evt in workflow.Events)
            Console.WriteLine(evt.Name);
        return ReplExecutionResult.Success();
    }

    if (command == "context")
    {
        Console.WriteLine(ToJson(sessionInstance.ContextSnapshot));
        return ReplExecutionResult.Success();
    }

    if (command == "load")
    {
        if (tokens.Count < 2)
        {
            Console.WriteLine("Usage: load <path>");
            return ReplExecutionResult.Failed();
        }

        var loaded = LoadInstance(tokens[1]);
        var compatibility = workflow.CheckCompatibility(loaded);
        if (!compatibility.IsCompatible)
        {
            Console.WriteLine(compatibility.Reason ?? "Instance is not compatible with this workflow.");
            return ReplExecutionResult.Failed();
        }

        sessionInstance = loaded;
        sessionInstancePath = tokens[1];
        Console.WriteLine($"Instance loaded: {sessionInstancePath}");
        return ReplExecutionResult.Success();
    }

    if (command == "save")
    {
        var path = tokens.Count > 1 ? tokens[1] : sessionInstancePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("Usage: save <path>");
            return ReplExecutionResult.Failed();
        }

        SaveInstance(path, sessionInstance with { UpdatedAt = DateTimeOffset.UtcNow });
        sessionInstancePath = path;
        Console.WriteLine($"Instance saved: {path}");
        return ReplExecutionResult.Success();
    }

    if (command is "inspect" or "fire")
    {
        if (tokens.Count < 2)
        {
            Console.WriteLine($"Usage: {command} <EventName> [event-args-json]");
            return ReplExecutionResult.Failed();
        }

        var eventName = tokens[1];
        var commandContext = tokens.Count > 2
            ? ParseContext(tokens[2], null)
            : new Dictionary<string, object?>(StringComparer.Ordinal);

        var effectiveContext = MergeContext(sessionInstance.ContextSnapshot, commandContext);

        if (command == "inspect")
        {
            var inspect = workflow.Inspect(sessionInstance.CurrentState, eventName, effectiveContext);
            Console.WriteLine($"Defined: {inspect.IsDefined}");
            Console.WriteLine($"Accepted: {inspect.IsAccepted}");
            Console.WriteLine($"Target: {inspect.TargetState ?? "<none>"}");
            if (inspect.Reasons.Count > 0)
            {
                Console.WriteLine("Reasons:");
                foreach (var reason in inspect.Reasons)
                    Console.WriteLine($" - {reason}");
            }

            if (!inspect.IsDefined)
                Console.WriteLine("Outcome: NotDefined");
            else if (!inspect.IsAccepted)
                Console.WriteLine("Outcome: Rejected");
        }
        else
        {
            var instanceResult = workflow.Fire(
                sessionInstance with { ContextSnapshot = effectiveContext },
                eventName,
                null);

            Console.WriteLine($"Defined: {instanceResult.IsDefined}");
            Console.WriteLine($"Accepted: {instanceResult.IsAccepted}");
            Console.WriteLine($"NewState: {instanceResult.NewState ?? "<none>"}");
            if (instanceResult.Reasons.Count > 0)
            {
                Console.WriteLine("Reasons:");
                foreach (var reason in instanceResult.Reasons)
                    Console.WriteLine($" - {reason}");
            }

            if (!instanceResult.IsDefined)
                Console.WriteLine("Outcome: NotDefined");
            else if (!instanceResult.IsAccepted)
                Console.WriteLine("Outcome: Rejected");

            if (instanceResult.IsAccepted && instanceResult.UpdatedInstance is not null)
                sessionInstance = instanceResult.UpdatedInstance;
        }

        sessionInstance = sessionInstance with
        {
            ContextSnapshot = new Dictionary<string, object?>(effectiveContext, StringComparer.Ordinal),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return ReplExecutionResult.Success();
    }

    Console.WriteLine($"Unknown REPL command '{tokens[0]}'. Type 'help' for options.");
    return ReplExecutionResult.Failed();
}

static void PrintReplHelp()
{
    Console.WriteLine("REPL commands:");
    Console.WriteLine("  help");
    Console.WriteLine("  state");
    Console.WriteLine("  events");
    Console.WriteLine("  context");
    Console.WriteLine("  inspect <EventName> [event-args-json]");
    Console.WriteLine("  fire <EventName> [event-args-json]");
    Console.WriteLine("    event-args-json is merged with the current instance snapshot for that command");
    Console.WriteLine("  load <path>");
    Console.WriteLine("  save [path]");
    Console.WriteLine("  exit | quit");
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

static Dictionary<string, object?> MergeContext(
    IReadOnlyDictionary<string, object?> baseContext,
    IReadOnlyDictionary<string, object?> overlay)
{
    var merged = new Dictionary<string, object?>(baseContext, StringComparer.Ordinal);
    foreach (var kvp in overlay)
        merged[kvp.Key] = kvp.Value;
    return merged;
}

static string ToJson(IReadOnlyDictionary<string, object?> context)
{
    return JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
}

readonly record struct ReplExecutionResult(bool IsSuccess, bool ShouldExit)
{
    public static ReplExecutionResult Success() => new(true, false);
    public static ReplExecutionResult Failed() => new(false, false);
    public static ReplExecutionResult Exit() => new(true, true);
}
