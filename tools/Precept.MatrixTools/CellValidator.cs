using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using Precept.Pipeline;

namespace Precept.MatrixTools;

// ════════════════════════════════════════════════════════════════════════════
//  CellValidator — machine verification of the obligation-discharge cell data.
//
//  Runs the checks a JSON schema cannot express over each *.cells.json file:
//  disposition presence from the closed set, citation resolution against the
//  repository, near-miss pairing (one per discharge addition), respellability
//  verdict resolvability (family verdict with per-cell overrides), a decision
//  procedure named per contract entry, axis values from the case shape's
//  vocabularies (read from the cell schema, never duplicated here), the
//  definition-version stamp, and WP recomputation through the WpCalculator
//  where the obligation's shape is in the calculator's scope.
//
//  Every cell the WP check cannot recompute yields a named skip record — the
//  specific reason (e.g. the calculator's multi-write refusal naming the open
//  write-plan decomposition decision), never a generic answer, never silence.
// ════════════════════════════════════════════════════════════════════════════

public enum CellFindingSeverity { Error, Warning }

/// <summary>A defect found in a cell file. Errors block ratification; warnings flag drift (a moved cited line).</summary>
public sealed record CellFinding(CellFindingSeverity Severity, string Check, string Location, string Message);

/// <summary>A named skip: a check that could not run at a location, with the specific reason it could not.</summary>
public sealed record CellSkip(string Check, string Location, string Reason);

/// <summary>The validator's output: findings plus the named skip records.</summary>
public sealed record CellValidationReport(
    ImmutableArray<CellFinding> Findings,
    ImmutableArray<CellSkip> Skips)
{
    public bool HasErrors => Findings.Any(f => f.Severity == CellFindingSeverity.Error);
}

public static class CellValidator
{
    // Check names — stable identifiers in findings and skips.
    private const string CheckFile = "file";
    private const string CheckDefinitionVersion = "definition-version";
    private const string CheckDisposition = "disposition";
    private const string CheckAxis = "axis";
    private const string CheckCitation = "citation";
    private const string CheckNearMiss = "near-miss";
    private const string CheckRespellability = "respellability";
    private const string CheckDecisionProcedure = "decision-procedure";
    private const string CheckWpRecompute = "wp-recompute";
    private const string CheckSchemaArm = "schema-arm";

    /// <summary>Validates every *.cells.json under <paramref name="cellsDirectory"/>.</summary>
    public static CellValidationReport ValidateDirectory(
        string cellsDirectory, string repoRoot, string? schemaPath = null)
    {
        var findings = ImmutableArray.CreateBuilder<CellFinding>();
        var skips = ImmutableArray.CreateBuilder<CellSkip>();

        foreach (var file in Directory.EnumerateFiles(cellsDirectory, "*.cells.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            var report = ValidateFile(file, repoRoot, schemaPath);
            findings.AddRange(report.Findings);
            skips.AddRange(report.Skips);
        }

        return new CellValidationReport(findings.ToImmutable(), skips.ToImmutable());
    }

    /// <summary>
    /// Validates one cell file. When <paramref name="schemaPath"/> is null, the
    /// schema is resolved from the file's <c>$schema</c> property (relative to the
    /// file), falling back to a sibling <c>cell.schema.json</c>.
    /// </summary>
    public static CellValidationReport ValidateFile(
        string cellsFilePath, string repoRoot, string? schemaPath = null)
    {
        var context = new Context(Path.GetFileName(cellsFilePath), repoRoot);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(cellsFilePath));
        }
        catch (JsonException e)
        {
            context.Error(CheckFile, "(file)", $"file is not parseable JSON: {e.Message}");
            return context.Report();
        }

        using (document)
        {
            var root = document.RootElement;

            SchemaVocabulary vocabulary;
            try
            {
                vocabulary = SchemaVocabulary.Load(ResolveSchemaPath(cellsFilePath, root, schemaPath));
            }
            catch (Exception e) when (e is IOException or JsonException or KeyNotFoundException)
            {
                context.Error(CheckFile, "(file)", $"cell schema could not be loaded: {e.Message}");
                return context.Report();
            }

            CheckDefinitionVersionStamp(context, root);
            var fileCaseShape = GetString(root, "caseShape");
            if (fileCaseShape is null || !vocabulary.CaseShapes.Contains(fileCaseShape))
                context.Error(CheckAxis, "(file)",
                    $"file caseShape '{fileCaseShape ?? "(absent)"}' is not in the schema's case-shape vocabulary "
                    + $"[{string.Join(", ", vocabulary.CaseShapes)}]");

            var familyVerdict = CheckFamilyRespellability(context, root, vocabulary);
            WalkCitations(context, root, "$", cellId: null);

            if (root.TryGetProperty("cells", out var cells) && cells.ValueKind == JsonValueKind.Array)
            {
                foreach (var cell in cells.EnumerateArray())
                {
                    var cellId = GetString(cell, "id") ?? "(cell without id)";
                    var location = $"{context.FileName}: cells[{cellId}]";

                    CheckDispositionValue(context, cell, location, vocabulary);
                    CheckSchemaConditionalArms(context, cell, location, vocabulary);
                    CheckAxisValues(context, cell, location, fileCaseShape, vocabulary);
                    CheckNearMissPairing(context, cell, location);
                    CheckCellRespellability(context, cell, location, vocabulary, familyVerdict);
                    CheckDecisionProcedures(context, cell, location);
                    CheckWpRecomputation(context, cell, location);
                }
            }

            return context.Report();
        }
    }

    // ── Definition-version stamp ─────────────────────────────────────────────

    private static void CheckDefinitionVersionStamp(Context context, JsonElement root)
    {
        var version = GetString(root, "definitionVersion");
        if (string.IsNullOrWhiteSpace(version))
            context.Error(CheckDefinitionVersion, "(file)",
                "definitionVersion stamp is missing or blank — certificates and amendments key off it");
    }

    // ── Disposition ──────────────────────────────────────────────────────────

    private static void CheckDispositionValue(
        Context context, JsonElement cell, string location, SchemaVocabulary vocabulary)
    {
        if (!cell.TryGetProperty("disposition", out var disposition)
            || disposition.ValueKind != JsonValueKind.Object)
        {
            context.Error(CheckDisposition, location, "cell carries no disposition — every cell carries exactly one");
            return;
        }

        var status = GetString(disposition, "status");
        if (status is null || !vocabulary.DispositionStatuses.Contains(status))
            context.Error(CheckDisposition, location,
                $"disposition status '{status ?? "(absent)"}' is not in the closed set "
                + $"[{string.Join(", ", vocabulary.DispositionStatuses)}]");
    }

    // ── Axis values (from the schema's per-case-shape coordinate arms) ───────

    private static void CheckAxisValues(
        Context context, JsonElement cell, string location, string? fileCaseShape, SchemaVocabulary vocabulary)
    {
        if (!cell.TryGetProperty("coordinates", out var coordinates)
            || coordinates.ValueKind != JsonValueKind.Object)
        {
            context.Error(CheckAxis, location, "cell carries no coordinates object");
            return;
        }

        var cellCaseShape = GetString(coordinates, "caseShape");
        if (fileCaseShape is not null && cellCaseShape != fileCaseShape)
        {
            context.Error(CheckAxis, location,
                $"cell caseShape '{cellCaseShape ?? "(absent)"}' disagrees with the file's caseShape "
                + $"'{fileCaseShape}' — one file carries one family/case-shape");
            return;
        }

        if (cellCaseShape is null || !vocabulary.CoordinateArms.TryGetValue(cellCaseShape, out var arm))
        {
            context.Error(CheckAxis, location,
                $"caseShape '{cellCaseShape ?? "(absent)"}' selects no coordinate arm in the cell schema");
            return;
        }

        foreach (var required in arm.Required)
        {
            if (!coordinates.TryGetProperty(required, out _))
                context.Error(CheckAxis, location, $"coordinates are missing required axis '{required}'");
        }

        foreach (var property in coordinates.EnumerateObject())
        {
            if (!arm.Properties.TryGetValue(property.Name, out var spec))
            {
                context.Error(CheckAxis, location,
                    $"axis '{property.Name}' is not part of the '{cellCaseShape}' case shape");
                continue;
            }

            if (spec.Allowed is null)
                continue; // free-string axis (vocabulary anchor pending in the schema)

            if (spec.IsArray)
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                {
                    context.Error(CheckAxis, location, $"axis '{property.Name}' must be an array");
                    continue;
                }
                foreach (var item in property.Value.EnumerateArray())
                {
                    var value = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                    if (value is null || !spec.Allowed.Contains(value))
                        context.Error(CheckAxis, location,
                            $"axis '{property.Name}' value '{value ?? item.ToString()}' is not in the vocabulary "
                            + $"[{string.Join(", ", spec.Allowed)}]");
                }
            }
            else
            {
                var value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
                if (value is null || !spec.Allowed.Contains(value))
                    context.Error(CheckAxis, location,
                        $"axis '{property.Name}' value '{value ?? property.Value.ToString()}' is not in the vocabulary "
                        + $"[{string.Join(", ", spec.Allowed)}]");
            }
        }
    }

    // ── Citation resolution ──────────────────────────────────────────────────

    // Recognizes every citation object ({kind, ref}) anywhere in the file, so
    // new nesting positions are covered without enumeration.
    private static void WalkCitations(Context context, JsonElement element, string path, string? cellId)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (GetString(element, "kind") is not null && GetString(element, "ref") is string reference)
                {
                    ResolveCitation(context, reference, CitationLocation(context, path, cellId));
                    return; // citations carry no nested citations
                }
                foreach (var property in element.EnumerateObject())
                    WalkCitations(context, property.Value, $"{path}.{property.Name}", cellId);
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var childCellId = cellId;
                    if (path.EndsWith(".cells", StringComparison.Ordinal))
                        childCellId = GetString(item, "id") ?? $"(cell {index})";
                    WalkCitations(context, item, $"{path}[{index}]", childCellId);
                    index++;
                }
                break;
        }
    }

    private static string CitationLocation(Context context, string path, string? cellId) =>
        cellId is null ? $"{context.FileName}: {path}" : $"{context.FileName}: cells[{cellId}] {path}";

    private static void ResolveCitation(Context context, string reference, string location)
    {
        // Three machine-checkable forms: path, path:line, path § anchor.
        string filePart = reference;
        string? anchor = null;
        int? line = null;

        var anchorSplit = reference.Split(" § ", 2);
        if (anchorSplit.Length == 2)
        {
            filePart = anchorSplit[0].Trim();
            anchor = anchorSplit[1].Trim();
        }
        else
        {
            var lineMatch = Regex.Match(reference, @"^(?<path>.+):(?<line>\d+)$");
            if (lineMatch.Success)
            {
                filePart = lineMatch.Groups["path"].Value;
                line = int.Parse(lineMatch.Groups["line"].Value);
            }
        }

        var fullPath = Path.Combine(context.RepoRoot, filePart);
        if (!File.Exists(fullPath))
        {
            context.Error(CheckCitation, location, $"cited file '{filePart}' does not exist in the repository");
            return;
        }

        if (line is int cited)
        {
            int lineCount = File.ReadLines(fullPath).Count();
            if (cited < 1 || cited > lineCount)
                context.Warning(CheckCitation, location,
                    $"cited line {cited} is outside '{filePart}' ({lineCount} lines) — the cited text has moved");
        }

        if (anchor is not null)
        {
            var headings = File.ReadLines(fullPath)
                .Where(l => Regex.IsMatch(l, @"^#{1,6}\s"))
                .Select(l => l.TrimStart('#').Trim())
                .ToArray();
            int matches = headings.Count(h => h.Contains(anchor, StringComparison.Ordinal));
            if (matches == 0)
                context.Error(CheckCitation, location,
                    $"anchor '§ {anchor}' matches no markdown heading in '{filePart}'");
            else if (matches > 1)
                context.Error(CheckCitation, location,
                    $"anchor '§ {anchor}' matches {matches} markdown headings in '{filePart}' — it must match exactly one");
        }
    }

    // ── Near-miss pairing ────────────────────────────────────────────────────

    private static void CheckNearMissPairing(Context context, JsonElement cell, string location)
    {
        if (!cell.TryGetProperty("witnesses", out var witnesses)
            || witnesses.ValueKind != JsonValueKind.Object)
        {
            // Missing witnesses on a defined cell is a completeness error the schema
            // arms report; here the pairing check simply cannot run, and says so.
            context.Skip(CheckNearMiss, location,
                "the cell carries no witnesses object, so there are no discharge additions to pair near-misses against");
            return;
        }

        var discharges = witnesses.TryGetProperty("discharges", out var d) && d.ValueKind == JsonValueKind.Array
            ? d.EnumerateArray().ToArray()
            : [];
        var nearMisses = witnesses.TryGetProperty("nearMisses", out var n) && n.ValueKind == JsonValueKind.Array
            ? n.EnumerateArray().ToArray()
            : [];

        var dischargeIds = new Dictionary<string, bool>(StringComparer.Ordinal); // id → has an addition to weaken
        foreach (var discharge in discharges)
        {
            var id = GetString(discharge, "id");
            if (id is null)
            {
                context.Error(CheckNearMiss, location, "discharge without an id — near-misses cannot pair against it");
                continue;
            }
            bool hasAddition = discharge.TryGetProperty("addition", out var addition)
                && addition.ValueKind == JsonValueKind.String;

            // A discharge with no addition is the *standing* case, and standing means
            // one thing: the default constant-fold, which has nothing authored to
            // weaken. Any other kind with the addition absent or null would silently
            // drop its must-reject near-miss, so the polarity is checked here.
            if (!hasAddition && GetString(discharge, "kind") is var kind && kind != "default-constant-fold")
                context.Error(CheckNearMiss, location,
                    $"discharge '{id}' records no addition (kind '{kind ?? "(absent)"}'), but only a standing "
                    + "default-constant-fold discharge has nothing to weaken — every other discharge owes an "
                    + "addition and its paired near-miss");

            if (!dischargeIds.TryAdd(id, hasAddition))
                context.Error(CheckNearMiss, location, $"duplicate discharge id '{id}'");
        }

        var pairCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var nearMiss in nearMisses)
        {
            var pairsWith = GetString(nearMiss, "pairsWith");
            if (pairsWith is null || !dischargeIds.ContainsKey(pairsWith))
            {
                context.Error(CheckNearMiss, location,
                    $"near-miss pairs with '{pairsWith ?? "(absent)"}', which names no discharge in this cell");
                continue;
            }
            pairCounts[pairsWith] = pairCounts.GetValueOrDefault(pairsWith) + 1;
        }

        foreach (var (id, hasAddition) in dischargeIds)
        {
            int count = pairCounts.GetValueOrDefault(id);
            if (!hasAddition)
            {
                if (count > 0)
                    context.Error(CheckNearMiss, location,
                        $"standing discharge '{id}' has no addition to weaken, but {count} near-miss(es) pair with it");
            }
            else if (count != 1)
            {
                context.Error(CheckNearMiss, location,
                    $"discharge addition '{id}' has {count} paired near-misses — exactly one is required per addition");
            }
        }
    }

    // ── Respellability resolvability ─────────────────────────────────────────

    private static string? CheckFamilyRespellability(Context context, JsonElement root, SchemaVocabulary vocabulary)
    {
        if (!root.TryGetProperty("respellability", out var family)
            || family.ValueKind != JsonValueKind.Object)
        {
            context.Error(CheckRespellability, "(file)",
                "family-level respellability block is missing — every cell inherits its verdict from it");
            return null;
        }

        var verdict = GetString(family, "verdict");
        if (verdict is null || !vocabulary.RespellabilityVerdicts.Contains(verdict))
        {
            context.Error(CheckRespellability, "(file)",
                $"family respellability verdict '{verdict ?? "(absent)"}' is not in "
                + $"[{string.Join(", ", vocabulary.RespellabilityVerdicts)}]");
            return null;
        }

        return verdict;
    }

    private static void CheckCellRespellability(
        Context context, JsonElement cell, string location, SchemaVocabulary vocabulary, string? familyVerdict)
    {
        bool hasBlock = cell.TryGetProperty("respellability", out var respellability)
            && respellability.ValueKind == JsonValueKind.Object;

        var status = cell.TryGetProperty("disposition", out var disposition)
            ? GetString(disposition, "status")
            : null;
        var caseShape = cell.TryGetProperty("coordinates", out var coordinates)
            ? GetString(coordinates, "caseShape")
            : null;

        if (!hasBlock)
        {
            // Defined non-structural cells must resolve a verdict; the structural
            // case shape has no respellability column (the weaker case shape).
            if (status == "defined" && caseShape != "structural")
                context.Error(CheckRespellability, location,
                    "defined cell carries no respellability block — its verdict is not resolvable");
            return;
        }

        var resolution = GetString(respellability, "resolution");
        string? resolved;
        switch (resolution)
        {
            case "family-inherited":
                resolved = familyVerdict;
                break;

            case "cell-override":
                resolved = GetString(respellability, "overrideVerdict");
                if (resolved is null || !vocabulary.RespellabilityVerdicts.Contains(resolved))
                {
                    context.Error(CheckRespellability, location,
                        "cell-override respellability states no overrideVerdict — the verdict is not resolvable");
                    return;
                }
                break;

            default:
                context.Error(CheckRespellability, location,
                    $"respellability resolution '{resolution ?? "(absent)"}' is neither family-inherited nor cell-override");
                return;
        }

        if (resolved == "yes"
            && GetString(respellability, "bandMember") is not null
            && string.IsNullOrWhiteSpace(GetString(respellability, "respelling")))
        {
            context.Error(CheckRespellability, location,
                "resolved verdict is yes and a bandMember is stated, but no respelling is recorded for it");
        }
    }

    // ── The cell schema's conditional arms, machine-executed ─────────────────
    //
    // No JSON Schema evaluator runs over the cell files, so the schema's
    // defined-cell arms — "a defined cell carries an obligation, a contract with
    // at least one entry, suggestion schemas, witnesses with at least one
    // discharge, a respellability resolution" — would be enforced by nothing but
    // a human's editor. The validator executes them here, over the constructs
    // those arms use (required / properties / minItems / const / enum / type).
    // A construct outside that set yields a named skip: the arm is reported
    // unchecked, never silently passed.

    private static void CheckSchemaConditionalArms(
        Context context, JsonElement cell, string location, SchemaVocabulary vocabulary)
    {
        using var arms = JsonDocument.Parse(vocabulary.ConditionalArmsJson);

        int index = 0;
        foreach (var arm in arms.RootElement.EnumerateArray())
        {
            var armName = $"cell-schema arm {index++}";

            if (!arm.TryGetProperty("if", out var gate) || !arm.TryGetProperty("then", out var consequent))
            {
                context.Skip(CheckSchemaArm, location,
                    $"{armName} is not an if/then arm — the validator executes if/then arms only");
                continue;
            }

            var applies = EvaluateGate(gate, cell, out var unsupportedGate);
            if (unsupportedGate is not null)
            {
                context.Skip(CheckSchemaArm, location,
                    $"{armName}: its condition uses schema keyword '{unsupportedGate}', which this validator does "
                    + "not evaluate — the arm is unchecked at this cell");
                continue;
            }

            if (applies)
                ApplyConsequent(context, consequent, cell, location, armName, "cell");
        }
    }

    /// <summary>Evaluates a schema condition over a value. Sets <paramref name="unsupported"/> for any keyword outside the executed subset.</summary>
    private static bool EvaluateGate(JsonElement schema, JsonElement value, out string? unsupported)
    {
        unsupported = null;
        bool satisfied = true;

        foreach (var keyword in schema.EnumerateObject())
        {
            switch (keyword.Name)
            {
                case "required":
                    foreach (var name in keyword.Value.EnumerateArray())
                        satisfied &= value.ValueKind == JsonValueKind.Object
                            && value.TryGetProperty(name.GetString()!, out _);
                    break;

                case "properties":
                    foreach (var property in keyword.Value.EnumerateObject())
                    {
                        if (value.ValueKind != JsonValueKind.Object
                            || !value.TryGetProperty(property.Name, out var child))
                            continue; // absent property: `required` decides, not `properties`
                        satisfied &= EvaluateGate(property.Value, child, out unsupported);
                        if (unsupported is not null)
                            return false;
                    }
                    break;

                case "const":
                    satisfied &= value.ValueKind == JsonValueKind.String
                        && value.GetString() == keyword.Value.GetString();
                    break;

                case "enum":
                    satisfied &= value.ValueKind == JsonValueKind.String
                        && keyword.Value.EnumerateArray().Any(v => v.GetString() == value.GetString());
                    break;

                case "type":
                    satisfied &= keyword.Value.GetString() switch
                    {
                        "array" => value.ValueKind == JsonValueKind.Array,
                        "object" => value.ValueKind == JsonValueKind.Object,
                        "string" => value.ValueKind == JsonValueKind.String,
                        _ => false,
                    };
                    break;

                case "minItems":
                    satisfied &= value.ValueKind == JsonValueKind.Array
                        && value.GetArrayLength() >= keyword.Value.GetInt32();
                    break;

                default:
                    unsupported = keyword.Name;
                    return false;
            }
        }

        return satisfied;
    }

    /// <summary>Enforces a schema consequent over a value, reporting each violation as a finding.</summary>
    private static void ApplyConsequent(
        Context context, JsonElement schema, JsonElement value, string location, string armName, string path)
    {
        foreach (var keyword in schema.EnumerateObject())
        {
            switch (keyword.Name)
            {
                case "required":
                    foreach (var name in keyword.Value.EnumerateArray())
                    {
                        var property = name.GetString()!;
                        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(property, out _))
                            context.Error(CheckSchemaArm, location,
                                $"{path} is missing required component '{property}' ({armName}) — "
                                + "a defined cell is not complete without it");
                    }
                    break;

                case "properties":
                    foreach (var property in keyword.Value.EnumerateObject())
                        if (value.ValueKind == JsonValueKind.Object
                            && value.TryGetProperty(property.Name, out var child))
                            ApplyConsequent(context, property.Value, child, location, armName,
                                $"{path}.{property.Name}");
                    break;

                case "minItems":
                    int minimum = keyword.Value.GetInt32();
                    if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() < minimum)
                        context.Error(CheckSchemaArm, location,
                            $"{path} carries {(value.ValueKind == JsonValueKind.Array ? value.GetArrayLength() : 0)} "
                            + $"entries; the schema requires at least {minimum} ({armName})");
                    break;

                default:
                    context.Skip(CheckSchemaArm, location,
                        $"{armName}: its consequent uses schema keyword '{keyword.Name}' at {path}, which this "
                        + "validator does not enforce — that requirement is unchecked at this cell");
                    break;
            }
        }
    }

    // ── Decision procedure per contract entry ────────────────────────────────

    private static void CheckDecisionProcedures(Context context, JsonElement cell, string location)
    {
        if (!cell.TryGetProperty("dischargeContract", out var contract)
            || contract.ValueKind != JsonValueKind.Array)
        {
            context.Skip(CheckDecisionProcedure, location,
                "the cell carries no dischargeContract array, so there is no contract entry whose decision "
                + "procedure could be checked");
            return;
        }

        int index = 0;
        foreach (var entry in contract.EnumerateArray())
        {
            if (string.IsNullOrWhiteSpace(GetString(entry, "decisionProcedure")))
                context.Error(CheckDecisionProcedure, location,
                    $"discharge-contract entry {index} names no decision procedure — without one the reject "
                    + "side of the exact contract cannot be tested even in principle");
            index++;
        }
    }

    // ── WP recomputation ─────────────────────────────────────────────────────

    private static void CheckWpRecomputation(Context context, JsonElement cell, string location)
    {
        // The check's domain: defined cells carrying an obligation schema.
        if (!cell.TryGetProperty("disposition", out var disposition)
            || GetString(disposition, "status") != "defined")
            return;
        if (!cell.TryGetProperty("obligation", out var obligation)
            || obligation.ValueKind != JsonValueKind.Object)
        {
            context.Skip(CheckWpRecompute, location,
                "the defined cell carries no obligation object — there is no proof condition to recompute "
                + "(the missing component is reported by the schema-arm check)");
            return;
        }

        var storedKey = obligation.TryGetProperty("wp", out var wp) ? GetString(wp, "canonicalKey") : null;
        if (storedKey is null)
        {
            context.Skip(CheckWpRecompute, location,
                "no mechanized canonical key is recorded (obligation.wp.canonicalKey absent) — nothing to recompute against");
            return;
        }

        var caseShape = cell.TryGetProperty("coordinates", out var coordinates)
            ? GetString(coordinates, "caseShape")
            : null;
        switch (caseShape)
        {
            case "fault":
                context.Skip(CheckWpRecompute, location,
                    "fault-family obligations are catalog-declared safety preconditions at evaluation sites; "
                    + "the WP calculator computes rule-family weakest preconditions only");
                return;
            case "editable-ingress":
                context.Skip(CheckWpRecompute, location,
                    "editable-ingress obligations are the field's modifier-rules plus every mentioning rule at the "
                    + "edit; there is no authored write plan for the single-write calculator to push a WP through");
                return;
        }

        var program = cell.TryGetProperty("witnesses", out var witnesses)
                && witnesses.TryGetProperty("base", out var baseWitness)
            ? GetString(baseWitness, "program")
            : null;
        if (program is null)
        {
            context.Skip(CheckWpRecompute, location,
                "no base witness program is recorded (witnesses.base absent) — a standing discharge has no "
                + "rejecting base, so there is no program to recompute the WP from");
            return;
        }

        // A base program is expected to carry diagnostics — it is the program the
        // compiler must flag — so error diagnostics never block recomputation. The
        // pipeline still produces the semantic index the calculator reads.
        var compilation = Compiler.Compile(program);
        var semantics = compilation.Semantics;
        var entries = ObligationEnumerator.Enumerate(semantics);
        var specs = entries.OfType<ObligationSpec>().ToArray();
        if (specs.Length == 0)
        {
            var skipped = entries.OfType<ObligationSkipped>().ToArray();
            context.Skip(CheckWpRecompute, location,
                skipped.Length == 0
                    ? "the base program carries no rules and no rule-desugaring modifiers — no obligation to recompute"
                    : "every obligation of the base program is outside the calculator's scope: "
                        + string.Join("; ", skipped.Select(s => $"{s.Label}: {s.Reason}")));
            return;
        }

        var family = coordinates.ValueKind == JsonValueKind.Object ? GetString(coordinates, "obligationFamily") : null;
        var writeSite = coordinates.ValueKind == JsonValueKind.Object ? GetString(coordinates, "writeSiteCategory") : null;

        var results = new List<WpResult>();
        if (family == "establishment" && writeSite == "construction-defaults")
        {
            // Establishment over the default configuration alone: no initial write plan.
            foreach (var spec in specs)
                results.Add(WpCalculator.ComputeEstablishmentWp(semantics, [], spec));
        }
        else if (family is "establishment" or "preservation")
        {
            var eventNames = WitnessEventNames(witnesses);
            if (eventNames.Count == 0)
            {
                context.Skip(CheckWpRecompute, location,
                    "no event name is derivable from the cell's witness applications — the write plan to push "
                    + "the WP through cannot be located in the base program");
                return;
            }

            var plans = RowPlans(semantics, eventNames);
            if (plans.Count == 0)
            {
                var diagnostics = compilation.HasErrors
                    ? $" (the program compiled with errors: {string.Join(", ", compilation.Diagnostics.Select(d => d.Code).Distinct())})"
                    : "";
                context.Error(CheckWpRecompute, location,
                    $"the base program has no row for event(s) [{string.Join(", ", eventNames)}] named by the "
                    + "cell's witness applications" + diagnostics);
                return;
            }

            foreach (var plan in plans)
            foreach (var spec in specs)
                results.Add(family == "establishment"
                    ? WpCalculator.ComputeEstablishmentWp(semantics, plan, spec)
                    : WpCalculator.ComputePreservationWp(plan, spec));
        }
        else
        {
            context.Skip(CheckWpRecompute, location,
                $"obligation family '{family ?? "(absent)"}' is not a rule family (establishment/preservation) — "
                + "the calculator computes rule-family weakest preconditions only");
            return;
        }

        var computedKeys = results.OfType<WpComputed>().Select(r => r.Wp.Key).Distinct().ToArray();
        if (computedKeys.Contains(storedKey, StringComparer.Ordinal))
            return; // recomputation confirms the stored key

        var refusals = results.OfType<WpNotSupported>().Select(r => r.Reason).Distinct().ToArray();
        if (computedKeys.Length == 0 && refusals.Length > 0)
        {
            // The calculator declined every candidate; carry its specific reasons forward.
            context.Skip(CheckWpRecompute, location, string.Join("; ", refusals));
            return;
        }

        context.Error(CheckWpRecompute, location,
            $"stored canonical key '{storedKey}' is not among the recomputed WP keys "
            + $"[{string.Join(", ", computedKeys)}]"
            + (refusals.Length > 0 ? $" (declined candidates: {string.Join("; ", refusals)})" : ""));
    }

    /// <summary>Event names the cell's witness applications point at (row-guard events and arg-declaration events).</summary>
    private static IReadOnlyList<string> WitnessEventNames(JsonElement witnesses)
    {
        var names = new List<string>();

        void Collect(JsonElement witness)
        {
            if (!witness.TryGetProperty("application", out var application)
                || application.ValueKind != JsonValueKind.Object)
                return;

            if (GetString(application, "eventName") is string direct && !names.Contains(direct))
                names.Add(direct);

            if (application.TryGetProperty("replacements", out var replacements)
                && replacements.ValueKind == JsonValueKind.Array)
            {
                foreach (var replacement in replacements.EnumerateArray())
                {
                    if (GetString(replacement, "eventName") is string fromArg && !names.Contains(fromArg))
                        names.Add(fromArg);
                }
            }
        }

        foreach (var group in new[] { "discharges", "nearMisses" })
        {
            if (witnesses.TryGetProperty(group, out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var witness in list.EnumerateArray())
                    Collect(witness);
            }
        }

        return names;
    }

    /// <summary>The write plans of every event/transition row handling one of the named events.</summary>
    private static IReadOnlyList<IReadOnlyList<TypedAction>> RowPlans(
        SemanticIndex semantics, IReadOnlyList<string> eventNames)
    {
        var plans = new List<IReadOnlyList<TypedAction>>();

        foreach (var row in semantics.EventHandlers.OfType<TypedEventRowSuccess>())
        {
            if (eventNames.Contains(row.EventName) && !row.Actions.IsEmpty)
                plans.Add(row.Actions);
        }

        foreach (var row in semantics.TransitionRows.OfType<TypedTransitionRowSuccess>())
        {
            if (eventNames.Contains(row.EventName) && !row.Actions.IsEmpty)
                plans.Add(row.Actions);
        }

        return plans;
    }

    // ── Shared plumbing ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the cell schema for a cell file: an explicit path when given, else the
    /// file's <c>$schema</c> property relative to the file, else a sibling
    /// <c>cell.schema.json</c>. Shared with the prose generator so both read one schema.
    /// </summary>
    internal static string ResolveSchemaPath(string cellsFilePath, JsonElement root, string? schemaPath)
    {
        if (schemaPath is not null)
            return schemaPath;

        var directory = Path.GetDirectoryName(Path.GetFullPath(cellsFilePath))!;
        var declared = GetString(root, "$schema");
        if (declared is not null && !declared.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var resolved = Path.GetFullPath(Path.Combine(directory, declared));
            if (File.Exists(resolved))
                return resolved;
        }

        return Path.Combine(directory, "cell.schema.json");
    }

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed class Context(string fileName, string repoRoot)
    {
        private readonly ImmutableArray<CellFinding>.Builder _findings = ImmutableArray.CreateBuilder<CellFinding>();
        private readonly ImmutableArray<CellSkip>.Builder _skips = ImmutableArray.CreateBuilder<CellSkip>();

        public string FileName { get; } = fileName;
        public string RepoRoot { get; } = repoRoot;

        public void Error(string check, string scope, string message) =>
            _findings.Add(new CellFinding(CellFindingSeverity.Error, check, Locate(scope), message));

        public void Warning(string check, string scope, string message) =>
            _findings.Add(new CellFinding(CellFindingSeverity.Warning, check, Locate(scope), message));

        public void Skip(string check, string scope, string reason) =>
            _skips.Add(new CellSkip(check, Locate(scope), reason));

        private string Locate(string scope) =>
            scope.StartsWith(FileName, StringComparison.Ordinal) ? scope : $"{FileName}: {scope}";

        public CellValidationReport Report() =>
            new(_findings.ToImmutable(), _skips.ToImmutable());
    }

    /// <summary>
    /// The closed vocabularies the checks validate against, read from
    /// cell.schema.json — the schema stays the single owner of the enumerations;
    /// this class derives, never duplicates.
    /// </summary>
    private sealed class SchemaVocabulary
    {
        public required IReadOnlySet<string> DispositionStatuses { get; init; }
        public required IReadOnlySet<string> CaseShapes { get; init; }
        public required IReadOnlySet<string> RespellabilityVerdicts { get; init; }
        public required IReadOnlyDictionary<string, CoordinateArm> CoordinateArms { get; init; }

        /// <summary>
        /// The properties a defined cell must carry, read from the schema's own
        /// defined-cell arms (keyed structural / non-structural). The validator
        /// executes this requirement because nothing evaluates the schema itself.
        /// </summary>
        public required ImmutableArray<string> DefinedNonStructuralRequired { get; init; }

        public required ImmutableArray<string> DefinedStructuralRequired { get; init; }

        /// <summary>
        /// The raw JSON of the cell schema's conditional (<c>allOf</c>) arms, kept as
        /// text so the validator can execute them per cell. No JSON Schema evaluator
        /// runs over these files, so the validator executes the arms itself.
        /// </summary>
        public required string ConditionalArmsJson { get; init; }

        public sealed record CoordinateArm(
            ImmutableArray<string> Required,
            IReadOnlyDictionary<string, AxisSpec> Properties);

        /// <summary>One coordinate axis: its allowed values (null = free string), and whether it is an array axis.</summary>
        public sealed record AxisSpec(IReadOnlySet<string>? Allowed, bool IsArray);

        public static SchemaVocabulary Load(string schemaPath)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(schemaPath));
            var defs = document.RootElement.GetProperty("$defs");

            var arms = new Dictionary<string, CoordinateArm>(StringComparer.Ordinal);
            foreach (var arm in defs.GetProperty("coordinates").GetProperty("oneOf").EnumerateArray())
            {
                var properties = new Dictionary<string, AxisSpec>(StringComparer.Ordinal);
                foreach (var property in arm.GetProperty("properties").EnumerateObject())
                    properties[property.Name] = ParseAxis(property.Value);

                var required = arm.TryGetProperty("required", out var requiredList)
                    ? requiredList.EnumerateArray().Select(r => r.GetString()!).ToImmutableArray()
                    : ImmutableArray<string>.Empty;

                var shape = arm.GetProperty("properties").GetProperty("caseShape").GetProperty("const").GetString()!;
                arms[shape] = new CoordinateArm(required, properties);
            }

            var definedNonStructural = ImmutableArray<string>.Empty;
            var definedStructural = ImmutableArray<string>.Empty;
            foreach (var arm in defs.GetProperty("cell").GetProperty("allOf").EnumerateArray())
            {
                if (!arm.TryGetProperty("if", out var condition)
                    || !condition.TryGetProperty("properties", out var conditionProperties))
                    continue;

                var status = conditionProperties.TryGetProperty("disposition", out var dispositionCondition)
                    && dispositionCondition.TryGetProperty("properties", out var dispositionProperties)
                    && dispositionProperties.TryGetProperty("status", out var statusCondition)
                    && statusCondition.TryGetProperty("const", out var statusConst)
                    ? statusConst.GetString()
                    : null;

                var required = arm.TryGetProperty("then", out var then)
                    && then.TryGetProperty("required", out var requiredList)
                    ? requiredList.EnumerateArray().Select(r => r.GetString()!).ToImmutableArray()
                    : ImmutableArray<string>.Empty;

                bool structural =
                    conditionProperties.TryGetProperty("coordinates", out var coordinateCondition)
                    && coordinateCondition.TryGetProperty("properties", out var coordinateProperties)
                    && coordinateProperties.TryGetProperty("caseShape", out var caseShapeCondition)
                    && caseShapeCondition.TryGetProperty("const", out var caseShapeConst)
                    && caseShapeConst.GetString() == "structural";

                if (status == "defined" && !required.IsEmpty)
                {
                    if (structural)
                        definedStructural = required;
                    else
                        definedNonStructural = required;
                }
            }

            return new SchemaVocabulary
            {
                DispositionStatuses = EnumValues(
                    defs.GetProperty("disposition").GetProperty("properties").GetProperty("status")),
                CaseShapes = EnumValues(defs.GetProperty("caseShape")),
                RespellabilityVerdicts = EnumValues(
                    defs.GetProperty("familyRespellability").GetProperty("properties").GetProperty("verdict")),
                CoordinateArms = arms,
                DefinedNonStructuralRequired = definedNonStructural,
                DefinedStructuralRequired = definedStructural,
                ConditionalArmsJson = defs.GetProperty("cell").GetProperty("allOf").GetRawText(),
            };
        }

        private static AxisSpec ParseAxis(JsonElement axis)
        {
            if (axis.TryGetProperty("const", out var constant))
                return new AxisSpec(new HashSet<string>(StringComparer.Ordinal) { constant.GetString()! }, IsArray: false);

            if (axis.TryGetProperty("enum", out _))
                return new AxisSpec(EnumValues(axis), IsArray: false);

            if (axis.TryGetProperty("items", out var items) && items.TryGetProperty("enum", out _))
                return new AxisSpec(EnumValues(items), IsArray: true);

            return new AxisSpec(Allowed: null, IsArray: false); // free-string axis
        }

        private static IReadOnlySet<string> EnumValues(JsonElement schema) =>
            schema.GetProperty("enum").EnumerateArray()
                .Select(v => v.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
    }
}
