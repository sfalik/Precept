using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Precept;
using Precept.Language;
using Precept.Pipeline;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: runner <manifest.json>");
    return 1;
}

string manifestPath = args[0];
string manifestText = File.ReadAllText(manifestPath);

using JsonDocument doc = JsonDocument.Parse(manifestText);
var results = new JsonArray();

foreach (JsonElement entry in doc.RootElement.EnumerateArray())
{
    string id = entry.GetProperty("id").GetString() ?? "";
    string text = entry.GetProperty("text").GetString() ?? "";

    JsonObject resultObj;
    try
    {
        Compilation c = Compiler.Compile(text);

        var errorCodes = c.Diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Distinct()
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        var allDiagnostics = new JsonArray();
        foreach (Diagnostic d in c.Diagnostics)
        {
            var diagObj = new JsonObject
            {
                ["code"] = d.Code,
                ["severity"] = d.Severity.ToString(),
                ["stage"] = d.Stage.ToString(),
            };
            allDiagnostics.Add(diagObj);
        }

        var errorCodesArr = new JsonArray();
        foreach (string code in errorCodes)
        {
            errorCodesArr.Add(code);
        }

        resultObj = new JsonObject
        {
            ["id"] = id,
            ["hasErrors"] = c.HasErrors,
            ["errorCodes"] = errorCodesArr,
            ["allDiagnostics"] = allDiagnostics,
        };
    }
    catch (Exception ex)
    {
        resultObj = new JsonObject
        {
            ["id"] = id,
            ["hasErrors"] = true,
            ["errorCodes"] = new JsonArray { "RUNNER_CRASH" },
            ["allDiagnostics"] = new JsonArray(),
            ["crashMessage"] = ex.GetType().Name + ": " + ex.Message,
        };
    }

    results.Add(resultObj);
}

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
Console.WriteLine(results.ToJsonString(jsonOptions));

return 0;
