using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using StateMachine.Dsl;

const int ExitSuccess = 0;
const int ExitInvalidUsage = 1;
const int ExitInputFileNotFound = 2;
const int ExitUnknownCommand = 3;
const int ExitUnhandledError = 4;
const int ExitInspectNotDefined = 5;
const int ExitInspectRejected = 6;
const int ExitFireNotDefined = 7;
const int ExitFireRejected = 8;

if (args.Length < 2)
{
    PrintUsage();
    return ExitInvalidUsage;
}

string command = args[0];
string inputPath = args[1];

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

    if (command.Equals("validate", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"OK: '{machine.Name}' parsed successfully.");
        Console.WriteLine($"States: {machine.States.Count}, Events: {machine.Events.Count}, Transitions: {machine.Transitions.Count}");
        return ExitSuccess;
    }

    if (command.Equals("inspect", StringComparison.OrdinalIgnoreCase))
    {
        string? instancePath = GetOption(args, "--instance");
        string? stateOption = GetOption(args, "--state");
        string state = ResolveStateForCommand("inspect", stateOption, instancePath);
        string eventName = GetOption(args, "--event") ?? throw new InvalidOperationException("--event is required for inspect.");
        var context = ParseContext(GetOption(args, "--context"), GetOption(args, "--context-file"));
        var result = instancePath is null
            ? workflow.Inspect(state, eventName, context)
            : workflow.Inspect(LoadInstance(instancePath), eventName, context);

        Console.WriteLine($"Machine: {machine.Name}");
        Console.WriteLine($"State: {result.CurrentState}");
        Console.WriteLine($"Event: {result.EventName}");
        Console.WriteLine($"Defined: {result.IsDefined}");
        Console.WriteLine($"Accepted: {result.IsAccepted}");
        Console.WriteLine($"Target: {result.TargetState ?? "<none>"}");

        if (result.Reasons.Count > 0)
        {
            Console.WriteLine("Reasons:");
            foreach (var reason in result.Reasons)
                Console.WriteLine($" - {reason}");
        }

        if (!result.IsDefined)
            Console.WriteLine("Outcome: NotDefined");
        else if (!result.IsAccepted)
            Console.WriteLine("Outcome: Rejected");

        if (result.IsAccepted)
            return ExitSuccess;

        return result.IsDefined ? ExitInspectRejected : ExitInspectNotDefined;
    }

    if (command.Equals("fire", StringComparison.OrdinalIgnoreCase))
    {
        string? instancePath = GetOption(args, "--instance");
        string? stateOption = GetOption(args, "--state");
        string state = ResolveStateForCommand("fire", stateOption, instancePath);
        string eventName = GetOption(args, "--event") ?? throw new InvalidOperationException("--event is required for fire.");
        var context = ParseContext(GetOption(args, "--context"), GetOption(args, "--context-file"));

        DslFireResult result;
        DslWorkflowInstance? updatedInstance = null;
        if (instancePath is null)
        {
            result = workflow.Fire(state, eventName, context);
        }
        else
        {
            var instance = LoadInstance(instancePath);
            var instanceResult = workflow.Fire(instance, eventName, context);
            result = new DslFireResult(
                instanceResult.IsDefined,
                instanceResult.IsAccepted,
                instanceResult.PreviousState,
                instanceResult.EventName,
                instanceResult.NewState,
                instanceResult.Reasons);
            updatedInstance = instanceResult.UpdatedInstance;
        }

        Console.WriteLine($"Machine: {machine.Name}");
        Console.WriteLine($"Current: {result.CurrentState}");
        Console.WriteLine($"Event: {result.EventName}");
        Console.WriteLine($"Defined: {result.IsDefined}");
        Console.WriteLine($"Accepted: {result.IsAccepted}");
        Console.WriteLine($"NewState: {result.NewState ?? "<none>"}");

        if (result.Reasons.Count > 0)
        {
            Console.WriteLine("Reasons:");
            foreach (var reason in result.Reasons)
                Console.WriteLine($" - {reason}");
        }

        if (!result.IsDefined)
            Console.WriteLine("Outcome: NotDefined");
        else if (!result.IsAccepted)
            Console.WriteLine("Outcome: Rejected");
        else if (updatedInstance is not null)
        {
            string outputPath = GetOption(args, "--out-instance") ?? instancePath!;
            SaveInstance(outputPath, updatedInstance!);
            Console.WriteLine($"Instance saved: {outputPath}");
        }

        if (result.IsAccepted)
            return ExitSuccess;

        return result.IsDefined ? ExitFireRejected : ExitFireNotDefined;
    }

    if (command.Equals("list", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"Machine: {machine.Name}");
        Console.WriteLine("States:");
        foreach (var state in machine.States)
            Console.WriteLine($" - {state}");

        Console.WriteLine("Events:");
        foreach (var evt in machine.Events)
            Console.WriteLine($" - {evt.Name}{(evt.ArgumentType is null ? string.Empty : $"({evt.ArgumentType})")}");

        Console.WriteLine("Transitions:");
        foreach (var t in machine.Transitions)
        {
            var guard = string.IsNullOrWhiteSpace(t.GuardExpression) ? string.Empty : $" when {t.GuardExpression}";
            Console.WriteLine($" - {t.FromState} -> {t.ToState} on {t.EventName}{guard}");
        }

        return ExitSuccess;
    }

    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintUsage();
    return ExitUnknownCommand;
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
    Console.WriteLine("  dsl validate <file.sm>");
    Console.WriteLine("  dsl list <file.sm>");
    Console.WriteLine("  dsl inspect <file.sm> (--state <StateName> | --instance <instance.json>) --event <EventName> [--context <json>] [--context-file <path.json>]");
    Console.WriteLine("  dsl fire <file.sm> (--state <StateName> | --instance <instance.json>) --event <EventName> [--context <json>] [--context-file <path.json>] [--out-instance <path.json>]");
    Console.WriteLine("Exit codes:");
    Console.WriteLine($"  {ExitSuccess}: success");
    Console.WriteLine($"  {ExitInvalidUsage}: invalid usage");
    Console.WriteLine($"  {ExitInputFileNotFound}: input file not found");
    Console.WriteLine($"  {ExitUnknownCommand}: unknown command");
    Console.WriteLine($"  {ExitUnhandledError}: unhandled error");
    Console.WriteLine($"  {ExitInspectNotDefined}: inspect not defined");
    Console.WriteLine($"  {ExitInspectRejected}: inspect rejected");
    Console.WriteLine($"  {ExitFireNotDefined}: fire not defined");
    Console.WriteLine($"  {ExitFireRejected}: fire rejected");
}

static string ResolveStateForCommand(
    string command,
    string? stateOption,
    string? instancePath)
{
    bool hasState = !string.IsNullOrWhiteSpace(stateOption);
    bool hasInstance = !string.IsNullOrWhiteSpace(instancePath);

    if (hasState && hasInstance)
        throw new InvalidOperationException($"Use either --state or --instance for {command}, not both.");

    if (hasState)
        return stateOption!;

    if (hasInstance)
        return LoadInstance(instancePath!).CurrentState;

    throw new InvalidOperationException($"Either --state or --instance is required for {command}.");
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
    string workflowVersion = root.GetProperty("workflowVersion").GetString() ?? string.Empty;
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

    return new DslWorkflowInstance(workflowName, workflowVersion, currentState, lastEvent, updatedAt, context);
}

static void SaveInstance(string path, DslWorkflowInstance instance)
{
    var envelope = new Dictionary<string, object?>
    {
        ["workflowName"] = instance.WorkflowName,
        ["workflowVersion"] = instance.WorkflowVersion,
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
