using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace Precept.MatrixTools;

// ════════════════════════════════════════════════════════════════════════════
//  CellProseGenerator — the human-readable cell tables, generated from the data.
//
//  The cell data is the source; this renders it into readable markdown, one
//  file per witness family. Generated files are never hand-edited (the
//  tmLanguage discipline) and a drift test regenerates in memory and compares.
//
//  Two disciplines the renderer holds to:
//
//  * Nothing is silently dropped. Any property the renderer has no rule for is
//    surfaced by name with its verbatim JSON under "Fields with no rendering
//    rule" — a data field the generator does not understand must be visible,
//    never invisible.
//  * The schema owns the vocabulary. Readable axis names come from each
//    coordinate property's schema `title`, and the premise-class legend from
//    the schema's premise-class description. The renderer keeps no parallel
//    copy of either; axis *values* are rendered verbatim.
// ════════════════════════════════════════════════════════════════════════════

public static class CellProseGenerator
{
    /// <summary>
    /// Renders one *.cells.json file to markdown. When <paramref name="schemaPath"/>
    /// is null the schema is resolved the way the validator resolves it: the file's
    /// <c>$schema</c> property relative to the file, falling back to a sibling
    /// <c>cell.schema.json</c>.
    /// </summary>
    public static string RenderFile(string cellsFilePath, string? schemaPath = null)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(cellsFilePath));
        var root = document.RootElement;
        var schema = SchemaProse.Load(CellValidator.ResolveSchemaPath(cellsFilePath, root, schemaPath));
        return Render(root, Path.GetFileName(cellsFilePath), schema);
    }

    /// <summary>
    /// Renders every *.cells.json in <paramref name="cellsDirectory"/> into
    /// <c>&lt;cellsDirectory&gt;/generated/&lt;family&gt;.md</c>. Returns the written paths.
    /// </summary>
    public static ImmutableArray<string> RenderDirectory(string cellsDirectory, string? schemaPath = null)
    {
        var outputDirectory = Path.Combine(cellsDirectory, "generated");
        Directory.CreateDirectory(outputDirectory);

        var written = ImmutableArray.CreateBuilder<string>();
        foreach (var file in Directory.EnumerateFiles(cellsDirectory, "*.cells.json")
                     .OrderBy(f => f, StringComparer.Ordinal))
        {
            string markdown;
            try
            {
                markdown = RenderFile(file, schemaPath);
            }
            catch (Exception e) when (e is JsonException or IOException or InvalidOperationException)
            {
                // Name the file that failed — a generation run must say which source it choked on.
                throw new InvalidOperationException($"{Path.GetFileName(file)}: {e.Message}", e);
            }

            var family = Path.GetFileName(file);
            family = family[..^".cells.json".Length];
            var outputPath = Path.Combine(outputDirectory, family + ".md");
            File.WriteAllText(outputPath, markdown);
            written.Add(outputPath);
        }

        return written.ToImmutable();
    }

    // ── The file ─────────────────────────────────────────────────────────────

    private static string Render(JsonElement root, string sourceFileName, SchemaProse schema)
    {
        var output = new StringBuilder();
        var extras = new List<Extra>();

        output.Append("<!--\n");
        output.Append("GENERATED FILE — do not hand-edit.\n");
        output.Append($"Source: {sourceFileName}\n");
        output.Append("Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells "
            + "docs/Working/obligation-discharge-matrix-2026-07-19-cells\n");
        output.Append("Hand edits will be overwritten. Edit the .cells.json and regenerate.\n");
        output.Append("-->\n\n");

        output.Append($"# {Text(String(root, "title") ?? String(root, "familyId") ?? sourceFileName)}\n\n");
        Line(output, "Family id", String(root, "familyId"));
        Line(output, "Case shape", String(root, "caseShape"));
        Line(output, "Definition version", String(root, "definitionVersion"));
        output.Append('\n');

        Collect(extras, root, "$", "$schema", "familyId", "title", "caseShape", "definitionVersion",
            "citations", "respellability", "cells");

        RenderCitations(output, root, "Where this family's content comes from", extras, "$");

        if (root.TryGetProperty("respellability", out var familyRespellability))
            RenderFamilyRespellability(output, familyRespellability, extras);

        output.Append("## Premise classes — the legend\n\n");
        output.Append(Text(schema.PremiseClassLegend) + "\n\n");

        if (root.TryGetProperty("cells", out var cells) && cells.ValueKind == JsonValueKind.Array)
            foreach (var cell in cells.EnumerateArray())
                RenderCell(output, cell, schema, extras);

        RenderExtras(output, extras);

        return output.ToString();
    }

    private static void RenderFamilyRespellability(StringBuilder output, JsonElement respellability, List<Extra> extras)
    {
        const string scope = "$.respellability";
        output.Append("## Respellability — the family verdict\n\n");

        var verdict = String(respellability, "verdict");
        output.Append($"Can every safe program these rules reject be rewritten into a form they accept? "
            + $"**{Text(verdict ?? "(not stated)")}**\n\n");
        Line(output, "Evidence", String(respellability, "evidence"));
        Line(output, "Corpus measurement", String(respellability, "corpusMeasurement"));
        output.Append('\n');

        if (respellability.TryGetProperty("licensedNonBandExamples", out var examples)
            && examples.ValueKind == JsonValueKind.Array && examples.GetArrayLength() > 0)
        {
            output.Append("Spellings that look rejected but are in fact licensed (kept so the boundary stays findable):\n\n");
            var index = 0;
            foreach (var example in examples.EnumerateArray())
            {
                output.Append($"- `{Text(String(example, "spelling") ?? "(no spelling recorded)")}` — "
                    + $"{Text(String(example, "whyLicensed") ?? "(no reason recorded)")}\n");
                RenderCitationsInline(output, example, "  ", extras, $"{scope}.licensedNonBandExamples[{index}]");
                Collect(extras, example, $"{scope}.licensedNonBandExamples[{index}]",
                    "spelling", "whyLicensed", "citations");
                index++;
            }
            output.Append('\n');
        }

        RenderNotes(output, respellability, label: "Notes on the family verdict");
        RenderCitations(output, respellability, "Sources for this verdict", extras, scope);

        Collect(extras, respellability, scope,
            "verdict", "evidence", "corpusMeasurement", "licensedNonBandExamples", "citations", "notes");
    }

    // ── One cell ─────────────────────────────────────────────────────────────

    private static void RenderCell(StringBuilder output, JsonElement cell, SchemaProse schema, List<Extra> extras)
    {
        var id = String(cell, "id") ?? "(cell without an id)";
        var scope = $"cells[{id}]";

        output.Append($"## {Text(id)} — {Text(String(cell, "label") ?? "(no label)")}\n\n");

        RenderDisposition(output, cell, extras, scope);
        RenderCoordinates(output, cell, schema);
        RenderObligation(output, cell, extras, scope);
        RenderPremiseAvailability(output, cell);
        RenderContract(output, cell, extras, scope);
        RenderSuggestions(output, cell, extras, scope);
        RenderWitnesses(output, cell, extras, scope);
        RenderCellRespellability(output, cell, extras, scope);
        RenderOpenDependencies(output, cell);
        RenderNotes(output, cell, label: "What the sources leave unstated or ambiguous here");
        RenderCitations(output, cell, "What this cell derives from", extras, scope);

        Collect(extras, cell, scope,
            "id", "label", "coordinates", "disposition", "obligation", "applicablePremiseClasses",
            "premiseAvailability", "dischargeContract", "suggestions", "witnesses", "respellability",
            "openDependencies", "citations", "notes");
    }

    private static void RenderDisposition(StringBuilder output, JsonElement cell, List<Extra> extras, string scope)
    {
        if (!cell.TryGetProperty("disposition", out var disposition))
            return;

        output.Append($"Disposition: **{Text(String(disposition, "status") ?? "(not stated)")}**");
        var derivation = String(disposition, "pruningDerivation");
        output.Append(derivation is null ? ".\n\n" : $" — {Text(derivation)}\n\n");

        RenderCitations(output, disposition, "Disposition sources", extras, $"{scope}.disposition");
        Collect(extras, disposition, $"{scope}.disposition", "status", "pruningDerivation", "citations", "notes");
        RenderNotes(output, disposition);
    }

    private static void RenderCoordinates(StringBuilder output, JsonElement cell, SchemaProse schema)
    {
        if (!cell.TryGetProperty("coordinates", out var coordinates) || coordinates.ValueKind != JsonValueKind.Object)
            return;

        output.Append("### Where this cell sits on the axes\n\n");
        output.Append("| Axis | Value |\n|---|---|\n");
        foreach (var axis in coordinates.EnumerateObject())
            output.Append($"| {Text(schema.AxisLabel(axis.Name))} | {Text(Scalar(axis.Value))} |\n");
        output.Append('\n');
    }

    private static void RenderObligation(StringBuilder output, JsonElement cell, List<Extra> extras, string scope)
    {
        if (!cell.TryGetProperty("obligation", out var obligation))
            return;

        output.Append("### What must be proven\n\n");
        Line(output, "Obligation", String(obligation, "statement"));
        output.Append('\n');

        if (obligation.TryGetProperty("wp", out var wp))
        {
            Line(output, "Weakest precondition", String(wp, "statement"));
            Line(output, "Canonical key", String(wp, "canonicalKey"));
            Line(output, "Key pinned by", String(wp, "canonicalKeySource"));
            output.Append('\n');
            Collect(extras, wp, $"{scope}.obligation.wp", "statement", "canonicalKey", "canonicalKeySource");
        }

        if (obligation.TryGetProperty("metavariables", out var metavariables)
            && metavariables.ValueKind == JsonValueKind.Array && metavariables.GetArrayLength() > 0)
        {
            output.Append("The placeholders, and what each stands for:\n\n");
            output.Append("| Placeholder | Stands for |\n|---|---|\n");
            var index = 0;
            foreach (var metavariable in metavariables.EnumerateArray())
            {
                output.Append($"| {Text(String(metavariable, "name") ?? "(unnamed)")} "
                    + $"| {Text(String(metavariable, "standsFor") ?? "(not stated)")} |\n");
                Collect(extras, metavariable, $"{scope}.obligation.metavariables[{index}]", "name", "standsFor");
                index++;
            }
            output.Append('\n');
        }

        RenderCitations(output, obligation, "Matching is done under these normalization rules",
            extras, $"{scope}.obligation", "normalization");

        Collect(extras, obligation, $"{scope}.obligation", "statement", "metavariables", "normalization", "wp");
    }

    private static void RenderPremiseAvailability(StringBuilder output, JsonElement cell)
    {
        var classes = StringArray(cell, "applicablePremiseClasses");
        var availability = String(cell, "premiseAvailability");
        if (classes is null && availability is null)
            return;

        output.Append("### Which premise classes can discharge it\n\n");
        output.Append(classes is { Length: > 0 }
            ? $"Applicable classes: {Text(string.Join(", ", classes.Select(c => $"({c})")))}.\n\n"
            : "Applicable classes: none — no authorable premise is consumed here.\n\n");
        if (availability is not null)
            output.Append(Text(availability) + "\n\n");
    }

    private static void RenderContract(StringBuilder output, JsonElement cell, List<Extra> extras, string scope)
    {
        if (!cell.TryGetProperty("dischargeContract", out var contract) || contract.ValueKind != JsonValueKind.Array)
            return;

        output.Append("### The discharge contract — exactly when this counts as proven\n\n");

        var index = 0;
        foreach (var entry in contract.EnumerateArray())
        {
            var classes = StringArray(entry, "premiseClasses");
            var heading = classes is { Length: > 0 }
                ? string.Join(", ", classes.Select(c => $"({c})"))
                : "no authored premise";
            output.Append($"**Entry {index + 1} — {Text(heading)}**\n\n");
            Bullet(output, "Derivation", String(entry, "derivation"));
            Bullet(output, "Strategy", String(entry, "strategy"));
            Bullet(output, "Capability tier", String(entry, "capabilityTier"));
            var arguments = StringArray(entry, "validityArguments");
            if (arguments is { Length: > 0 })
                Bullet(output, "Validity arguments", string.Join("; ", arguments));
            Bullet(output, "Decision procedure", String(entry, "decisionProcedure"));
            RenderOpenDependencies(output, entry, "  ");
            output.Append('\n');
            RenderCitationsInline(output, entry, "", extras, $"{scope}.dischargeContract[{index}]");
            RenderNotes(output, entry);

            Collect(extras, entry, $"{scope}.dischargeContract[{index}]",
                "premiseClasses", "derivation", "strategy", "capabilityTier", "validityArguments",
                "decisionProcedure", "openDependencies", "citations", "notes");
            index++;
        }
    }

    private static void RenderSuggestions(StringBuilder output, JsonElement cell, List<Extra> extras, string scope)
    {
        if (!cell.TryGetProperty("suggestions", out var suggestions) || suggestions.ValueKind != JsonValueKind.Array)
            return;

        output.Append("### What the failing diagnostic must suggest\n\n");
        if (suggestions.GetArrayLength() == 0)
        {
            output.Append("Nothing — the applicable-class set is empty, so no authorable premise exists to suggest.\n\n");
            return;
        }

        var index = 0;
        foreach (var suggestion in suggestions.EnumerateArray())
        {
            var classes = StringArray(suggestion, "premiseClasses");
            var heading = classes is { Length: > 0 } ? string.Join(", ", classes.Select(c => $"({c})")) : "unclassified";
            output.Append($"- For class {Text(heading)}: {Text(String(suggestion, "schema") ?? "(no schema stated)")}\n");
            RenderCitationsInline(output, suggestion, "  ", extras, $"{scope}.suggestions[{index}]");
            RenderNotes(output, suggestion, "  ");
            Collect(extras, suggestion, $"{scope}.suggestions[{index}]",
                "premiseClasses", "schema", "citations", "notes");
            index++;
        }
        output.Append('\n');
    }

    private static void RenderWitnesses(StringBuilder output, JsonElement cell, List<Extra> extras, string scope)
    {
        if (!cell.TryGetProperty("witnesses", out var witnesses) || witnesses.ValueKind != JsonValueKind.Object)
            return;

        output.Append("### The worked examples\n\n");

        if (witnesses.TryGetProperty("base", out var baseWitness))
        {
            output.Append("**Base — the program the compiler must reject.**\n\n");
            var program = String(baseWitness, "program");
            if (program is not null)
                output.Append("```precept\n" + program.ReplaceLineEndings("\n").TrimEnd('\n') + "\n```\n\n");

            RenderExpected(output, baseWitness);
            RenderProvenance(output, baseWitness, extras, $"{scope}.witnesses.base");
            RenderNotes(output, baseWitness);
            Collect(extras, baseWitness, $"{scope}.witnesses.base", "program", "expected", "provenance", "notes");
        }
        else
        {
            output.Append("**Base.** None recorded — see this cell's notes for why.\n\n");
        }

        if (witnesses.TryGetProperty("discharges", out var discharges)
            && discharges.ValueKind == JsonValueKind.Array && discharges.GetArrayLength() > 0)
        {
            output.Append("**Discharge additions — what makes the base compile.**\n\n");
            var index = 0;
            foreach (var discharge in discharges.EnumerateArray())
            {
                var id = String(discharge, "id") ?? $"discharge {index + 1}";
                output.Append($"*{Text(id)}* — {Text(String(discharge, "kind") ?? "(kind not stated)")}\n\n");
                var addition = String(discharge, "addition");
                Bullet(output, "Addition", addition ?? "none — a standing discharge (nothing authored to weaken, so no near-miss is owed)");
                var classes = StringArray(discharge, "premiseClasses");
                if (classes is { Length: > 0 })
                    Bullet(output, "Premise classes", string.Join(", ", classes.Select(c => $"({c})")));
                Bullet(output, "Derivation", String(discharge, "derivation"));
                Bullet(output, "Strategy", String(discharge, "strategy"));
                Bullet(output, "Provable under the definition", String(discharge, "modelStatus"));
                var builtStatus = String(discharge, "builtStatus");
                var builtNote = String(discharge, "builtStatusNote");
                if (builtStatus is not null)
                    Bullet(output, "What the engine does today",
                        builtNote is null ? builtStatus : $"{builtStatus} ({builtNote})");
                RenderApplication(output, discharge, extras, $"{scope}.witnesses.discharges[{index}]");
                output.Append('\n');
                RenderExpected(output, discharge);
                RenderProvenance(output, discharge, extras, $"{scope}.witnesses.discharges[{index}]");
                RenderNotes(output, discharge);

                Collect(extras, discharge, $"{scope}.witnesses.discharges[{index}]",
                    "id", "kind", "addition", "application", "premiseClasses", "derivation", "strategy",
                    "modelStatus", "builtStatus", "builtStatusNote", "provenance", "expected", "notes");
                index++;
            }
        }

        if (witnesses.TryGetProperty("nearMisses", out var nearMisses)
            && nearMisses.ValueKind == JsonValueKind.Array && nearMisses.GetArrayLength() > 0)
        {
            output.Append("**Near-misses — each addition weakened past sufficiency; the compiler must still reject, "
                + "naming the same obligation.**\n\n");
            output.Append("| Weakens | Weakened addition | Why it still rejects | Required outcome |\n|---|---|---|---|\n");
            var index = 0;
            foreach (var nearMiss in nearMisses.EnumerateArray())
            {
                var expectedText = nearMiss.TryGetProperty("expected", out var expected)
                    ? Expectation(expected)
                    : "(not stated)";
                output.Append($"| {Text(String(nearMiss, "pairsWith") ?? "(not stated)")} "
                    + $"| {Text(String(nearMiss, "addition") ?? "(not stated)")} "
                    + $"| {Text(String(nearMiss, "whyStillRejects") ?? "(not stated)")} "
                    + $"| {Text(expectedText)} |\n");

                if (nearMiss.TryGetProperty("expected", out var nearMissExpected))
                    Collect(extras, nearMissExpected, $"{scope}.witnesses.nearMisses[{index}].expected",
                        "outcome", "sameObligation");
                RenderApplicationCollect(extras, nearMiss, $"{scope}.witnesses.nearMisses[{index}]");
                Collect(extras, nearMiss, $"{scope}.witnesses.nearMisses[{index}]",
                    "pairsWith", "addition", "application", "whyStillRejects", "provenance", "expected", "notes");
                index++;
            }
            output.Append('\n');
        }

        Collect(extras, witnesses, $"{scope}.witnesses", "base", "discharges", "nearMisses");
    }

    private static void RenderCellRespellability(StringBuilder output, JsonElement cell, List<Extra> extras, string scope)
    {
        if (!cell.TryGetProperty("respellability", out var respellability))
            return;

        output.Append("### Respellability\n\n");
        Bullet(output, "Resolution", String(respellability, "resolution"));
        Bullet(output, "Cell verdict", String(respellability, "verdict"));
        Bullet(output, "A representative safe-but-rejected spelling", String(respellability, "bandMember"));
        Bullet(output, "Its licensed respelling", String(respellability, "respelling"));
        output.Append('\n');
        RenderCitationsInline(output, respellability, "", extras, $"{scope}.respellability");
        RenderNotes(output, respellability);

        Collect(extras, respellability, $"{scope}.respellability",
            "resolution", "verdict", "bandMember", "respelling", "citations", "notes");
    }

    // ── Shared pieces ────────────────────────────────────────────────────────

    private static void RenderApplication(StringBuilder output, JsonElement owner, List<Extra> extras, string scope)
    {
        if (!owner.TryGetProperty("application", out var application) || application.ValueKind != JsonValueKind.Object)
            return;

        var locus = String(application, "locus");
        var eventName = String(application, "eventName");
        var description = locus switch
        {
            "row-guard" => $"appended to the guard of the `on {eventName ?? "?"}` row",
            "event-arg-declarations" => "applied by replacing the named event-arg declarations",
            _ => locus ?? "(locus not stated)",
        };
        Bullet(output, "How it applies to the base", description);

        if (application.TryGetProperty("replacements", out var replacements)
            && replacements.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var replacement in replacements.EnumerateArray())
            {
                output.Append($"  - `{Text(String(replacement, "eventName") ?? "?")}."
                    + $"{Text(String(replacement, "argName") ?? "?")}` becomes "
                    + $"`{Text(String(replacement, "declaration") ?? "?")}`\n");
                Collect(extras, replacement, $"{scope}.application.replacements[{index}]",
                    "eventName", "argName", "declaration");
                index++;
            }
        }

        Collect(extras, application, $"{scope}.application", "locus", "eventName", "replacements");
    }

    private static void RenderApplicationCollect(List<Extra> extras, JsonElement owner, string scope)
    {
        if (!owner.TryGetProperty("application", out var application) || application.ValueKind != JsonValueKind.Object)
            return;

        if (application.TryGetProperty("replacements", out var replacements)
            && replacements.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var replacement in replacements.EnumerateArray())
                Collect(extras, replacement, $"{scope}.application.replacements[{index++}]",
                    "eventName", "argName", "declaration");
        }

        Collect(extras, application, $"{scope}.application", "locus", "eventName", "replacements");
    }

    private static void RenderExpected(StringBuilder output, JsonElement owner)
    {
        if (!owner.TryGetProperty("expected", out var expected) || expected.ValueKind != JsonValueKind.Object)
            return;

        output.Append($"Required outcome: {Text(Expectation(expected))}\n\n");
    }

    private static string Expectation(JsonElement expected)
    {
        var parts = new List<string>();
        var outcome = String(expected, "outcome");
        if (outcome is not null)
            parts.Add(outcome);

        var missing = StringArray(expected, "missingPremiseClasses");
        if (missing is { Length: > 0 })
            parts.Add("naming the missing premise classes " + string.Join(", ", missing.Select(c => $"({c})")));

        if (Bool(expected, "noOtherDiagnostics") == true)
            parts.Add("with no other diagnostics");
        if (Bool(expected, "sameObligation") == true)
            parts.Add("naming the same obligation");
        if (Bool(expected, "premiseListRecorded") == true)
            parts.Add("with the premise list recorded");

        return parts.Count == 0 ? "(not stated)" : string.Join(", ", parts);
    }

    private static void RenderProvenance(StringBuilder output, JsonElement owner, List<Extra> extras, string scope)
    {
        if (!owner.TryGetProperty("provenance", out var provenance) || provenance.ValueKind != JsonValueKind.Object)
            return;

        var mark = String(provenance, "mark");
        var detail = new List<string>();
        if (String(provenance, "tool") is { } tool) detail.Add(tool);
        if (String(provenance, "date") is { } date) detail.Add(date);
        if (String(provenance, "head") is { } head) detail.Add($"HEAD {head}");

        output.Append($"Provenance: {Text(mark ?? "(not stated)")}"
            + (detail.Count == 0 ? "" : $" ({Text(string.Join(", ", detail))})")
            + (mark == "live-verified"
                ? " — the run attests the built-status claim, not the required outcome"
                : "")
            + ".\n\n");

        Collect(extras, provenance, $"{scope}.provenance", "mark", "tool", "date", "head", "note");
    }

    private static void RenderOpenDependencies(StringBuilder output, JsonElement owner, string indent = "")
    {
        var dependencies = StringArray(owner, "openDependencies");
        if (dependencies is not { Length: > 0 })
            return;

        output.Append($"{indent}- Open items this answer is load-bearing on: "
            + Text(string.Join(", ", dependencies)) + "\n");
    }

    private static void RenderNotes(StringBuilder output, JsonElement owner, string indent = "", string? label = null)
    {
        var notes = StringArray(owner, "notes");
        if (notes is not { Length: > 0 })
            return;

        if (label is not null)
            output.Append($"**{Text(label)}**\n\n");
        foreach (var note in notes)
            output.Append($"{indent}- {Text(note)}\n");
        output.Append('\n');
    }

    private static void RenderCitations(
        StringBuilder output, JsonElement owner, string heading, List<Extra> extras, string scope,
        string property = "citations")
    {
        if (!owner.TryGetProperty(property, out var citations)
            || citations.ValueKind != JsonValueKind.Array || citations.GetArrayLength() == 0)
            return;

        output.Append($"**{Text(heading)}**\n\n");
        WriteCitationList(output, citations, "", extras, $"{scope}.{property}");
        output.Append('\n');
    }

    private static void RenderCitationsInline(
        StringBuilder output, JsonElement owner, string indent, List<Extra> extras, string scope)
    {
        if (!owner.TryGetProperty("citations", out var citations)
            || citations.ValueKind != JsonValueKind.Array || citations.GetArrayLength() == 0)
            return;

        WriteCitationList(output, citations, indent, extras, $"{scope}.citations");
        output.Append('\n');
    }

    private static void WriteCitationList(
        StringBuilder output, JsonElement citations, string indent, List<Extra> extras, string scope)
    {
        var index = 0;
        foreach (var citation in citations.EnumerateArray())
        {
            var kind = String(citation, "kind");
            var date = String(citation, "date");
            var note = String(citation, "note");
            output.Append($"{indent}- {Text(String(citation, "ref") ?? "(no ref)")}"
                + (kind is null ? "" : $" — {Text(kind)}")
                + (date is null ? "" : $", {Text(date)}")
                + (note is null ? "" : $": {Text(note)}")
                + "\n");
            Collect(extras, citation, $"{scope}[{index}]", "kind", "ref", "date", "note");
            index++;
        }
    }

    // ── Nothing silently dropped ─────────────────────────────────────────────

    private sealed record Extra(string Location, string Name, string Json);

    private static void Collect(List<Extra> extras, JsonElement owner, string scope, params string[] known)
    {
        if (owner.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in owner.EnumerateObject())
            if (!known.Contains(property.Name, StringComparer.Ordinal))
                extras.Add(new Extra(scope, property.Name, property.Value.GetRawText()));
    }

    private static void RenderExtras(StringBuilder output, List<Extra> extras)
    {
        if (extras.Count == 0)
            return;

        output.Append("## Fields with no rendering rule\n\n");
        output.Append("Data the generator has no rendering rule for, surfaced verbatim rather than dropped. "
            + "Each is either a schema addition the generator has not caught up with, or a stray field.\n\n");
        output.Append("| Where | Field | Value |\n|---|---|---|\n");
        foreach (var extra in extras)
            output.Append($"| {Text(extra.Location)} | {Text(extra.Name)} | {Text(Compact(extra.Json))} |\n");
        output.Append('\n');
    }

    private static string Compact(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement,
                new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException)
        {
            return json;
        }
    }

    // ── The schema's own words ───────────────────────────────────────────────

    private sealed class SchemaProse
    {
        private readonly IReadOnlyDictionary<string, string> _axisTitles;

        private SchemaProse(string premiseClassLegend, IReadOnlyDictionary<string, string> axisTitles)
        {
            PremiseClassLegend = premiseClassLegend;
            _axisTitles = axisTitles;
        }

        /// <summary>The premise-class vocabulary, in the schema's own words — never restated here.</summary>
        public string PremiseClassLegend { get; }

        /// <summary>The readable name of a coordinate axis, from the schema's title for it.</summary>
        public string AxisLabel(string axisName) =>
            _axisTitles.TryGetValue(axisName, out var title) ? title : axisName;

        public static SchemaProse Load(string schemaPath)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(schemaPath));
            var defs = document.RootElement.GetProperty("$defs");

            var legend = defs.GetProperty("premiseClass").GetProperty("description").GetString()
                ?? throw new InvalidOperationException(
                    $"the cell schema at {schemaPath} carries no premise-class description to render the legend from");

            var titles = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var arm in defs.GetProperty("coordinates").GetProperty("oneOf").EnumerateArray())
                foreach (var property in arm.GetProperty("properties").EnumerateObject())
                    if (property.Value.TryGetProperty("title", out var title) && title.GetString() is { } text)
                        titles[property.Name] = text;

            return new SchemaProse(legend, titles);
        }
    }

    // ── Small helpers ────────────────────────────────────────────────────────

    private static void Line(StringBuilder output, string label, string? value)
    {
        if (value is not null)
            output.Append($"{Text(label)}: {Text(value)}\n");
    }

    private static void Bullet(StringBuilder output, string label, string? value)
    {
        if (value is not null)
            output.Append($"- {Text(label)}: {Text(value)}\n");
    }

    /// <summary>
    /// Renders text for markdown. Pipes are escaped: an unescaped pipe would split a
    /// table cell, and an escaped pipe renders as a pipe everywhere else too.
    /// </summary>
    private static string Text(string value) =>
        value.Replace("|", "\\|").ReplaceLineEndings(" ").Trim();

    private static string Scalar(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Array => string.Join(", ", element.EnumerateArray().Select(Scalar)),
        JsonValueKind.Object => element.GetRawText(),
        JsonValueKind.Null => "(null)",
        _ => element.GetRawText(),
    };

    private static string? String(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool? Bool(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null,
            }
            : null;

    private static string[]? StringArray(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String)
                .Select(v => v.GetString()!).ToArray()
            : null;
}
