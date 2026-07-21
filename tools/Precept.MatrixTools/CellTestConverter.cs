using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Precept.MatrixTools;

// ════════════════════════════════════════════════════════════════════════════
//  CellTestConverter — mechanical cell-to-test conversion for the
//  obligation-discharge cell data.
//
//  Every defined cell carrying a base witness converts into manifest rows:
//    base            → must reject, naming the cell's obligation
//    each discharge  → base + addition must accept via the expected strategy
//    each near-miss  → base + weakened addition must still reject, same obligation
//
//  Two expectations are kept apart on every row and never conflated:
//    Expected — what the definition requires (accept/reject)
//    Head     — what the built engine is expected to do today (must-pass /
//               expected-failure where the cell records the strategy as
//               unbuilt / unstated where the cell records no built status)
//
//  The obligation a row must name is identified mechanically, not by prose:
//  the cell's stored canonical WP key is recomputed from the base program
//  through the WpCalculator, and the matching enumerated obligation supplies
//  the structural key (field, bound kind, bound) the runner matches against
//  proof-ledger obligation records.
//
//  Every cell or witness the converter cannot turn into a runnable row yields
//  a named skip record with the specific reason — never a generic answer,
//  never silence.
// ════════════════════════════════════════════════════════════════════════════

public enum ManifestRowKind { Base, Discharge, NearMiss }

public enum ExpectedOutcome { Accept, Reject }

/// <summary>What the built engine is expected to do with the row today, per the cell data's built-status record.</summary>
public enum HeadExpectation
{
    /// <summary>The machinery exists; the run must match the definitional expectation.</summary>
    MustPass,
    /// <summary>The cell records the expected strategy as unbuilt; a miss is expected and recorded, a hit is flagged.</summary>
    ExpectedFailure,
    /// <summary>The cell states no built status; the run records what happens without claiming pass or failure.</summary>
    Unstated,
}

/// <summary>Which obligation-record contexts the structural key matches against.</summary>
public enum ObligationContextClass
{
    /// <summary>Obligations minted at authored write sites (event-handler and transition rows).</summary>
    WriteSite,
    /// <summary>Obligations minted for a field's declared default (establishment over defaults).</summary>
    FieldDefault,
}

/// <summary>
/// The structural identity of a cell's obligation, matched against proof-ledger
/// obligation records — never against diagnostic prose.
/// </summary>
public sealed record ObligationKey(
    string Field,
    BoundKind Kind,
    decimal Bound,
    string Label,
    ObligationContextClass ContextClass);

public sealed record ManifestRow(
    string Id,
    string CellId,
    ManifestRowKind Kind,
    string Program,
    ExpectedOutcome Expected,
    ObligationKey Obligation,
    HeadExpectation Head,
    string? HeadNote,
    string? ExpectedStrategy,
    string? PairsWith,
    /// <summary>True when the cell states base minimality: every error must come from the proof stage.</summary>
    bool RequireOnlyObligationDiagnostics,
    /// <summary>
    /// The missing-premise classes the cell requires the rejecting diagnostic to name.
    /// Recorded on the row and reported unverified: diagnostics carry no premise-class
    /// structure at HEAD, so the requirement rides in the manifest rather than vanishing.
    /// </summary>
    ImmutableArray<string> ExpectedMissingPremiseClasses = default,
    /// <summary>
    /// True when the cell requires the accepting compile to record its premise list.
    /// Also unverifiable at HEAD — <c>ProofObligation</c> carries no premise list.
    /// </summary>
    bool ExpectedPremiseListRecorded = false,
    /// <summary>
    /// Contract elements this row carries but cannot check at HEAD, each naming what
    /// is missing. A manifest reader must be able to see what went unverified.
    /// </summary>
    ImmutableArray<string> UnverifiableAtHead = default);

/// <summary>A named skip: a cell or witness that yielded no runnable row, with the specific reason.</summary>
public sealed record ManifestSkip(string CellId, string Item, string Reason);

public sealed record TestManifest(
    string FamilyId,
    string DefinitionVersion,
    string SourceFile,
    ImmutableArray<ManifestRow> Rows,
    ImmutableArray<ManifestSkip> Skips);

public static class CellTestConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string ToJson(TestManifest manifest) =>
        JsonSerializer.Serialize(manifest, JsonOptions);

    public static TestManifest FromJson(string json) =>
        JsonSerializer.Deserialize<TestManifest>(json, JsonOptions)
            ?? throw new JsonException("manifest JSON deserialized to null");

    public static TestManifest Convert(string cellsFilePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(cellsFilePath));
        var root = document.RootElement;

        var rows = ImmutableArray.CreateBuilder<ManifestRow>();
        var skips = ImmutableArray.CreateBuilder<ManifestSkip>();

        if (root.TryGetProperty("cells", out var cells) && cells.ValueKind == JsonValueKind.Array)
        {
            foreach (var cell in cells.EnumerateArray())
                ConvertCell(cell, rows, skips);
        }

        return new TestManifest(
            FamilyId: GetString(root, "familyId") ?? "(family without id)",
            DefinitionVersion: GetString(root, "definitionVersion") ?? "(no definition version)",
            SourceFile: Path.GetFileName(cellsFilePath),
            Rows: rows.ToImmutable(),
            Skips: skips.ToImmutable());
    }

    // ── Per-cell conversion ──────────────────────────────────────────────────

    private static void ConvertCell(
        JsonElement cell,
        ImmutableArray<ManifestRow>.Builder rows,
        ImmutableArray<ManifestSkip>.Builder skips)
    {
        var cellId = GetString(cell, "id") ?? "(cell without id)";

        var status = cell.TryGetProperty("disposition", out var disposition)
            ? GetString(disposition, "status")
            : null;
        if (status != "defined")
        {
            skips.Add(new ManifestSkip(cellId, "cell",
                $"disposition is '{status ?? "(absent)"}' — only defined cells state the "
                + "reject/discharge/near-miss triple the conversion needs"));
            return;
        }

        cell.TryGetProperty("witnesses", out var witnesses);
        var baseProgram = witnesses.ValueKind == JsonValueKind.Object
                && witnesses.TryGetProperty("base", out var baseWitness)
            ? GetString(baseWitness, "program")
            : null;
        if (baseProgram is null)
        {
            skips.Add(new ManifestSkip(cellId, "base",
                "no base witness program is recorded (a standing discharge has no rejecting base) — "
                + "no runnable program is stated in the cell data"));
            return;
        }

        var storedKey = cell.TryGetProperty("obligation", out var obligation)
                && obligation.TryGetProperty("wp", out var wp)
            ? GetString(wp, "canonicalKey")
            : null;
        if (storedKey is null)
        {
            skips.Add(new ManifestSkip(cellId, "obligation",
                "no mechanized canonical WP key is recorded (obligation.wp.canonicalKey absent) — "
                + "the cell's obligation cannot be identified among the base program's enumerated obligations"));
            return;
        }

        cell.TryGetProperty("coordinates", out var coordinates);
        var caseShape = GetString(coordinates, "caseShape");
        var family = GetString(coordinates, "obligationFamily");
        var writeSiteCategory = GetString(coordinates, "writeSiteCategory");

        // The manifest matches ledger records by context class, and only two classes
        // are modelled. A cell at any other write site converts to nothing rather than
        // to a row that would fail for the wrong cause.
        ObligationContextClass contextClass;
        switch (writeSiteCategory)
        {
            case "construction-defaults":
                contextClass = ObligationContextClass.FieldDefault;
                break;
            case "handler-set":
                contextClass = ObligationContextClass.WriteSite;
                break;
            default:
                skips.Add(new ManifestSkip(cellId, "coordinates",
                    $"write-site category '{writeSiteCategory ?? "(absent)"}' (case shape "
                    + $"'{caseShape ?? "(absent)"}') mints its obligation in a proof-ledger context the manifest's "
                    + "two context classes (handler write site, field default) do not cover — no row is emitted, "
                    + "rather than a row that would fail for the wrong cause"));
                return;
        }

        var (key, keyFailure) = DeriveObligationKey(baseProgram, storedKey, family, caseShape, contextClass);
        if (key is null)
        {
            skips.Add(new ManifestSkip(cellId, "obligation", keyFailure!));
            return;
        }

        witnesses.TryGetProperty("base", out var baseAgain);
        baseAgain.TryGetProperty("expected", out var baseExpected);

        var requireMinimality = baseExpected.ValueKind == JsonValueKind.Object
            && baseExpected.TryGetProperty("noOtherDiagnostics", out var minimality)
            && minimality.ValueKind == JsonValueKind.True;

        var missingClasses = StringArray(baseExpected, "missingPremiseClasses");
        var unverifiable = ImmutableArray.CreateBuilder<string>();
        if (!missingClasses.IsEmpty)
            unverifiable.Add(
                $"the rejection must name missing premise classes [{string.Join(", ", missingClasses)}]; "
                + "unverified at HEAD — diagnostics carry no premise-class structure to read");

        rows.Add(new ManifestRow(
            Id: $"{cellId}#base",
            CellId: cellId,
            Kind: ManifestRowKind.Base,
            Program: baseProgram,
            Expected: ExpectedOutcome.Reject,
            Obligation: key,
            Head: HeadExpectation.MustPass,
            HeadNote: null,
            ExpectedStrategy: null,
            PairsWith: null,
            RequireOnlyObligationDiagnostics: requireMinimality,
            ExpectedMissingPremiseClasses: missingClasses,
            ExpectedPremiseListRecorded: false,
            UnverifiableAtHead: unverifiable.ToImmutable()));

        if (witnesses.TryGetProperty("discharges", out var discharges)
            && discharges.ValueKind == JsonValueKind.Array)
        {
            foreach (var discharge in discharges.EnumerateArray())
                ConvertDischarge(discharge, cellId, baseProgram, key, rows, skips);
        }

        if (witnesses.TryGetProperty("nearMisses", out var nearMisses)
            && nearMisses.ValueKind == JsonValueKind.Array)
        {
            foreach (var nearMiss in nearMisses.EnumerateArray())
                ConvertNearMiss(nearMiss, cellId, baseProgram, key, rows, skips);
        }
    }

    private static void ConvertDischarge(
        JsonElement discharge,
        string cellId,
        string baseProgram,
        ObligationKey key,
        ImmutableArray<ManifestRow>.Builder rows,
        ImmutableArray<ManifestSkip>.Builder skips)
    {
        var dischargeId = GetString(discharge, "id") ?? "(discharge without id)";
        var addition = GetString(discharge, "addition");
        if (addition is null)
        {
            skips.Add(new ManifestSkip(cellId, $"discharge:{dischargeId}",
                "standing discharge with no addition — there is nothing to apply to the base program"));
            return;
        }

        var (program, applyFailure) = ApplyAddition(baseProgram, discharge, addition);
        if (program is null)
        {
            skips.Add(new ManifestSkip(cellId, $"discharge:{dischargeId}", applyFailure!));
            return;
        }

        var (head, headNote) = GetString(discharge, "builtStatus") switch
        {
            "proven-today" => (HeadExpectation.MustPass, (string?)null),
            "unresolved-today" => (HeadExpectation.ExpectedFailure,
                GetString(discharge, "builtStatusNote") ?? "recorded unresolved-today with no note"),
            null => (HeadExpectation.Unstated, "built status unstated in the cell data"),
            var other => (HeadExpectation.Unstated, $"built status '{other}' is not a recognized value"),
        };

        bool premiseListRecorded = discharge.TryGetProperty("expected", out var dischargeExpected)
            && dischargeExpected.TryGetProperty("premiseListRecorded", out var recorded)
            && recorded.ValueKind == JsonValueKind.True;

        var unverifiable = ImmutableArray.CreateBuilder<string>();
        if (premiseListRecorded)
            unverifiable.Add("the accepting compile must record its premise list; unverified at HEAD — "
                + "the proof-ledger obligation record carries no premise list");

        rows.Add(new ManifestRow(
            Id: $"{cellId}#discharge:{dischargeId}",
            CellId: cellId,
            Kind: ManifestRowKind.Discharge,
            Program: program,
            Expected: ExpectedOutcome.Accept,
            Obligation: key,
            Head: head,
            HeadNote: headNote,
            ExpectedStrategy: GetString(discharge, "strategy"),
            PairsWith: null,
            RequireOnlyObligationDiagnostics: false,
            ExpectedMissingPremiseClasses: [],
            ExpectedPremiseListRecorded: premiseListRecorded,
            UnverifiableAtHead: unverifiable.ToImmutable()));
    }

    private static void ConvertNearMiss(
        JsonElement nearMiss,
        string cellId,
        string baseProgram,
        ObligationKey key,
        ImmutableArray<ManifestRow>.Builder rows,
        ImmutableArray<ManifestSkip>.Builder skips)
    {
        var pairsWith = GetString(nearMiss, "pairsWith") ?? "(unpaired)";
        var addition = GetString(nearMiss, "addition");
        if (addition is null)
        {
            skips.Add(new ManifestSkip(cellId, $"near-miss:{pairsWith}",
                "near-miss states no addition — there is nothing to apply to the base program"));
            return;
        }

        var (program, applyFailure) = ApplyAddition(baseProgram, nearMiss, addition);
        if (program is null)
        {
            skips.Add(new ManifestSkip(cellId, $"near-miss:{pairsWith}", applyFailure!));
            return;
        }

        rows.Add(new ManifestRow(
            Id: $"{cellId}#near-miss:{pairsWith}",
            CellId: cellId,
            Kind: ManifestRowKind.NearMiss,
            Program: program,
            Expected: ExpectedOutcome.Reject,
            Obligation: key,
            Head: HeadExpectation.MustPass,
            HeadNote: null,
            ExpectedStrategy: null,
            PairsWith: pairsWith,
            RequireOnlyObligationDiagnostics: false,
            ExpectedMissingPremiseClasses: [],
            ExpectedPremiseListRecorded: false,
            UnverifiableAtHead: []));
    }

    // ── Obligation-key derivation (stored WP key → enumerated obligation) ────

    private static (ObligationKey? Key, string? Failure) DeriveObligationKey(
        string baseProgram, string storedKey, string? family, string? caseShape,
        ObligationContextClass contextClass)
    {
        var compilation = Compiler.Compile(baseProgram);
        var semantics = compilation.Semantics;
        var entries = ObligationEnumerator.Enumerate(semantics);
        var specs = entries.OfType<ObligationSpec>().ToArray();
        if (specs.Length == 0)
        {
            var skipped = entries.OfType<ObligationSkipped>().ToArray();

            // A fault cell's obligation is catalog-minted at an evaluation site, not
            // derived from a rule — "the program carries no rules" would be a true
            // sentence about the wrong thing.
            if (caseShape == "fault")
                return (null,
                    "fault-family obligations are catalog-declared safety preconditions at evaluation sites; "
                    + "the rule-obligation enumerator mints none of them, so no rule-family key can be derived "
                    + "for this cell");

            return (null, skipped.Length == 0
                ? "the base program carries no rules and no rule-desugaring modifiers — no obligation to key on"
                : "every obligation of the base program is outside the calculator's scope: "
                    + string.Join("; ", skipped.Select(s => $"{s.Label}: {s.Reason}")));
        }

        var matched = MatchSpecByStoredKey(semantics, specs, storedKey, family, out var refusals);
        if (matched is null)
        {
            // A refusal from the calculator is not a disagreement between the cell and
            // the program: the shape is outside what the calculator computes today, and
            // the reason it gives (e.g. the open write-plan decomposition ruling) is the
            // honest record. Only when every shape computed and none matched is the
            // stored key genuinely at odds with the program.
            return (null, refusals.Count > 0
                ? $"the stored canonical WP key '{storedKey}' could not be recomputed: the WpCalculator declined "
                    + $"{refusals.Count} of the base program's write plans — "
                    + string.Join("; ", refusals.Distinct(StringComparer.Ordinal))
                : $"the stored canonical WP key '{storedKey}' does not recompute from any obligation of "
                    + "the base program through the WpCalculator — the cell data and the base program disagree");
        }

        var canon = WpCalculator.Canonicalize(matched.Condition);
        switch (canon)
        {
            case CanonCompare { Left: CanonFieldPre field, Right: CanonNumber bound } compare
                when UpperBoundKind(compare.Op) is BoundKind upper:
                return (new ObligationKey(field.Name, upper, bound.Value, matched.Label, contextClass), null);

            case CanonCompare { Left: CanonNumber bound, Right: CanonFieldPre field } compare
                when LowerBoundKind(compare.Op) is BoundKind lower:
                return (new ObligationKey(field.Name, lower, bound.Value, matched.Label, contextClass), null);

            default:
                return (null,
                    $"the matched obligation '{matched.Label}' is not a single-field constant-bound "
                    + $"comparison (canonical shape: {canon.Key}) — the structural obligation matcher "
                    + "covers field-versus-constant bounds only");
        }
    }

    private static BoundKind? UpperBoundKind(CanonCompareOp op) => op switch
    {
        CanonCompareOp.Le => BoundKind.UpperInclusive,
        CanonCompareOp.Lt => BoundKind.UpperExclusive,
        _ => null,
    };

    private static BoundKind? LowerBoundKind(CanonCompareOp op) => op switch
    {
        CanonCompareOp.Le => BoundKind.LowerInclusive,
        CanonCompareOp.Lt => BoundKind.LowerExclusive,
        _ => null,
    };

    /// <summary>
    /// Finds the enumerated obligation whose WP through the base program's write
    /// plans (preservation) or default configuration (establishment) recomputes
    /// to the cell's stored canonical key.
    /// </summary>
    private static ObligationSpec? MatchSpecByStoredKey(
        Precept.Pipeline.SemanticIndex semantics,
        ObligationSpec[] specs,
        string storedKey,
        string? family,
        out List<string> refusals)
    {
        refusals = [];

        foreach (var spec in specs)
        {
            if (family == "establishment")
            {
                if (WpCalculator.ComputeEstablishmentWp(semantics, [], spec) is WpComputed established
                    && established.Wp.Key == storedKey)
                    return spec;
            }

            foreach (var plan in AllWritePlans(semantics))
            {
                var result = family == "establishment"
                    ? WpCalculator.ComputeEstablishmentWp(semantics, plan, spec)
                    : WpCalculator.ComputePreservationWp(plan, spec);
                switch (result)
                {
                    case WpComputed computed when computed.Wp.Key == storedKey:
                        return spec;
                    case WpNotSupported notSupported:
                        // The calculator's own reason, carried verbatim — never
                        // rewritten into a claim about the data.
                        refusals.Add($"{spec.Label}: {notSupported.Reason}");
                        break;
                }
            }
        }

        return null;
    }

    private static IEnumerable<IReadOnlyList<Precept.Pipeline.TypedAction>> AllWritePlans(
        Precept.Pipeline.SemanticIndex semantics)
    {
        foreach (var row in semantics.EventHandlers.OfType<Precept.Pipeline.TypedEventRowSuccess>())
        {
            if (!row.Actions.IsEmpty)
                yield return row.Actions;
        }
        foreach (var row in semantics.TransitionRows.OfType<Precept.Pipeline.TypedTransitionRowSuccess>())
        {
            if (!row.Actions.IsEmpty)
                yield return row.Actions;
        }
    }

    // ── Textual addition application ─────────────────────────────────────────

    private static (string? Program, string? Failure) ApplyAddition(
        string baseProgram, JsonElement witness, string addition)
    {
        if (!witness.TryGetProperty("application", out var application)
            || application.ValueKind != JsonValueKind.Object)
        {
            return (null, "witness states no application object — where the addition applies is unstated");
        }

        return GetString(application, "locus") switch
        {
            "row-guard" => ApplyRowGuard(baseProgram, application, addition),
            "event-arg-declarations" => ApplyArgDeclarations(baseProgram, application),
            var locus => (null,
                $"application locus '{locus ?? "(absent)"}' has no textual application rule in the converter "
                + "(row-guard and event-arg-declarations are the covered loci)"),
        };
    }

    private static (string? Program, string? Failure) ApplyRowGuard(
        string baseProgram, JsonElement application, string addition)
    {
        var eventName = GetString(application, "eventName");
        if (eventName is null)
            return (null, "row-guard application names no eventName — the target row cannot be located");

        var lines = baseProgram.Split('\n');
        var rowPattern = new Regex($@"(^|\s)on\s+{Regex.Escape(eventName)}(\s|$)");
        var matches = Enumerable.Range(0, lines.Length)
            .Where(i => rowPattern.IsMatch(lines[i]) && lines[i].Contains("->"))
            .ToArray();
        if (matches.Length != 1)
        {
            return (null,
                $"row-guard application for event '{eventName}' matched {matches.Length} rows in the "
                + "base program — exactly one is required for a deterministic insertion");
        }

        var line = lines[matches[0]];
        if (Regex.IsMatch(line, @"\bwhen\b"))
        {
            return (null,
                $"the row for event '{eventName}' already carries a guard — inserting a second is not "
                + "a defined textual application");
        }

        int arrow = line.IndexOf("->", StringComparison.Ordinal);
        lines[matches[0]] = line[..arrow] + addition + " " + line[arrow..];
        return (string.Join('\n', lines), null);
    }

    private static (string? Program, string? Failure) ApplyArgDeclarations(
        string baseProgram, JsonElement application)
    {
        if (!application.TryGetProperty("replacements", out var replacements)
            || replacements.ValueKind != JsonValueKind.Array)
        {
            return (null, "event-arg-declarations application carries no replacements array");
        }

        var lines = baseProgram.Split('\n');
        foreach (var replacement in replacements.EnumerateArray())
        {
            var eventName = GetString(replacement, "eventName");
            var argName = GetString(replacement, "argName");
            var declaration = GetString(replacement, "declaration");
            if (eventName is null || argName is null || declaration is null)
                return (null, "a replacement is missing eventName, argName, or declaration");

            var eventPattern = new Regex($@"\bevent\s+{Regex.Escape(eventName)}\s*\(");
            var eventLines = Enumerable.Range(0, lines.Length)
                .Where(i => eventPattern.IsMatch(lines[i]))
                .ToArray();
            if (eventLines.Length != 1)
            {
                return (null,
                    $"arg-declaration replacement for event '{eventName}' matched {eventLines.Length} "
                    + "declaration lines — exactly one is required");
            }

            var argPattern = new Regex($@"\b{Regex.Escape(argName)}\s+as\s+[^,()]*");
            var line = lines[eventLines[0]];
            var argMatches = argPattern.Matches(line);
            if (argMatches.Count != 1)
            {
                return (null,
                    $"arg '{argName}' of event '{eventName}' matched {argMatches.Count} declaration "
                    + "segments — exactly one is required for a deterministic replacement");
            }

            lines[eventLines[0]] = argPattern.Replace(line, declaration.TrimEnd(), 1);
        }

        return (string.Join('\n', lines), null);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static ImmutableArray<string> StringArray(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String)
                .Select(v => v.GetString()!).ToImmutableArray()
            : [];
}
