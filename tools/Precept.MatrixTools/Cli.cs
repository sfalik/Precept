using System.Linq;
using System.Text.Json;
using Precept.Pipeline;

namespace Precept.MatrixTools;

/// <summary>
/// CLI. Default (single .precept file): compile it and print, for every
/// obligation (explicit rules plus modifier-desugared rules), the establishment
/// WP over the default configuration and the preservation WP through each row's
/// write plan — with guard-match verdicts where the row carries a guard.
/// Subcommands: <c>convert-cells</c> turns a *.cells.json family file into a
/// test manifest; <c>run-manifest</c> executes a manifest through the pipeline
/// and verdicts each row from structured compile results; <c>measure-corpus</c>
/// classifies every rule × write-site obligation in a corpus directory against
/// the written discharge contracts; <c>render-cells</c> regenerates the
/// human-readable cell tables from the cell data.
/// </summary>
public static class Cli
{
    public static int Run(string[] args)
    {
        if (args.Length >= 1 && args[0] == "convert-cells")
            return ConvertCells(args.Skip(1).ToArray());
        if (args.Length >= 1 && args[0] == "run-manifest")
            return RunManifest(args.Skip(1).ToArray());
        if (args.Length >= 1 && args[0] == "measure-corpus")
            return MeasureCorpus(args.Skip(1).ToArray());
        if (args.Length >= 1 && args[0] == "render-cells")
            return RenderCells(args.Skip(1).ToArray());
        if (args.Length >= 1 && args[0] == "validate-cells")
            return ValidateCells(args.Skip(1).ToArray());

        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: precept-matrixtools <file.precept>");
            Console.Error.WriteLine("       precept-matrixtools convert-cells <family.cells.json> [<out.manifest.json>]");
            Console.Error.WriteLine("       precept-matrixtools run-manifest <manifest.json> [<out.results.json>]");
            Console.Error.WriteLine("       precept-matrixtools measure-corpus <corpus-dir> [<out.json> [<out.md>]]");
            Console.Error.WriteLine("       precept-matrixtools render-cells <cells-dir>");
            Console.Error.WriteLine("       precept-matrixtools validate-cells <cells-dir> [<repo-root>]");
            return 2;
        }

        var compilation = Precept.Compiler.Compile(File.ReadAllText(args[0]));
        foreach (var diagnostic in compilation.Diagnostics)
            Console.WriteLine($"[{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");

        var semantics = compilation.Semantics;
        var entries = ObligationEnumerator.Enumerate(semantics);
        var obligations = entries.OfType<ObligationSpec>().ToArray();

        foreach (var skipped in entries.OfType<ObligationSkipped>())
            Console.WriteLine($"[skipped obligation] {skipped.Label}: {skipped.Reason}");

        if (obligations.Length == 0)
        {
            Console.WriteLine("no computable obligations (no rules and no desugaring modifiers in scope).");
            return compilation.HasErrors ? 1 : 0;
        }

        Console.WriteLine();
        Console.WriteLine("── Establishment over the default configuration ──");
        foreach (var obligation in obligations)
            Describe(WpCalculator.ComputeEstablishmentWp(semantics, [], obligation), obligation, guard: null);

        foreach (var row in semantics.EventHandlers.OfType<TypedEventRowSuccess>().Where(r => r.IsConstruction))
        {
            Console.WriteLine();
            Console.WriteLine($"── Establishment through construction row: on {row.EventName} ──");
            foreach (var obligation in obligations)
                Describe(WpCalculator.ComputeEstablishmentWp(semantics, row.Actions, obligation), obligation, row.Guard);
        }

        foreach (var row in semantics.TransitionRows.OfType<TypedTransitionRowSuccess>())
        {
            Console.WriteLine();
            Console.WriteLine($"── Preservation: from {row.FromState ?? "*"} on {row.EventName}"
                + (row.Guard is null ? "" : $" when {WpCalculator.Canonicalize(row.Guard)}") + " ──");
            PrintPreservation(row.Actions, row.Guard, obligations);
        }

        foreach (var row in semantics.EventHandlers.OfType<TypedEventRowSuccess>().Where(r => !r.IsConstruction))
        {
            Console.WriteLine();
            Console.WriteLine($"── Preservation: on {row.EventName}"
                + (row.Guard is null ? "" : $" when {WpCalculator.Canonicalize(row.Guard)}") + " ──");
            PrintPreservation(row.Actions, row.Guard, obligations);
        }

        return compilation.HasErrors ? 1 : 0;
    }

    private static int ConvertCells(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.Error.WriteLine("usage: precept-matrixtools convert-cells <family.cells.json> [<out.manifest.json>]");
            return 2;
        }

        var manifest = CellTestConverter.Convert(args[0]);

        foreach (var skip in manifest.Skips)
            Console.WriteLine($"[skipped] {skip.CellId} ({skip.Item}): {skip.Reason}");
        Console.WriteLine($"{manifest.FamilyId}: {manifest.Rows.Length} rows ("
            + $"{manifest.Rows.Count(r => r.Kind == ManifestRowKind.Base)} base, "
            + $"{manifest.Rows.Count(r => r.Kind == ManifestRowKind.Discharge)} discharge, "
            + $"{manifest.Rows.Count(r => r.Kind == ManifestRowKind.NearMiss)} near-miss), "
            + $"{manifest.Skips.Length} skips");

        var json = CellTestConverter.ToJson(manifest);
        if (args.Length == 2)
        {
            File.WriteAllText(args[1], json);
            Console.WriteLine($"manifest written to {args[1]}");
        }
        else
        {
            Console.WriteLine(json);
        }

        return 0;
    }

    private static int ValidateCells(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.Error.WriteLine("usage: precept-matrixtools validate-cells <cells-dir> [<repo-root>]");
            return 2;
        }

        var repoRoot = args.Length == 2 ? args[1] : Directory.GetCurrentDirectory();
        var report = CellValidator.ValidateDirectory(args[0], repoRoot);

        foreach (var finding in report.Findings)
            Console.WriteLine($"[{finding.Severity}] {finding.Check} — {finding.Location}: {finding.Message}");
        foreach (var skip in report.Skips)
            Console.WriteLine($"[skipped] {skip.Check} — {skip.Location}: {skip.Reason}");

        Console.WriteLine($"{report.Findings.Count(f => f.Severity == CellFindingSeverity.Error)} errors, "
            + $"{report.Findings.Count(f => f.Severity == CellFindingSeverity.Warning)} warnings, "
            + $"{report.Skips.Length} named skips");

        return report.HasErrors ? 1 : 0;
    }

    private static int RenderCells(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: precept-matrixtools render-cells <cells-dir>");
            return 2;
        }

        // Exit 1 (never the usage code 2) on an unrenderable file, and name the file:
        // a generation run that cannot read its own source must fail distinguishably.
        try
        {
            var written = CellProseGenerator.RenderDirectory(args[0]);
            foreach (var path in written)
                Console.WriteLine($"rendered {path}");
            Console.WriteLine($"{written.Length} generated cell tables written under "
                + $"{Path.Combine(args[0], "generated")}");
            return 0;
        }
        catch (Exception e) when (e is JsonException or IOException or InvalidOperationException)
        {
            Console.Error.WriteLine($"render-cells failed in {args[0]}: {e.Message}");
            return 1;
        }
    }

    private static int MeasureCorpus(string[] args)
    {
        if (args.Length is < 1 or > 3)
        {
            Console.Error.WriteLine("usage: precept-matrixtools measure-corpus <corpus-dir> [<out.json> [<out.md>]]");
            return 2;
        }

        var report = CorpusMeasurement.MeasureDirectory(args[0]);

        // "with a rule-shaped entry" is broader than the matrix's "rule-bearing files"
        // (its ratification layer 4 counts the files with explicit rules) — the two
        // numbers are reported under distinct words so neither reads as the other.
        Console.WriteLine($"files: {report.FileCount} measured, {report.RuleBearingFileCount} with a rule-shaped entry, "
            + $"{report.FilesWithExplicitRules} with explicit rules, "
            + $"{report.FilesWithErrorDiagnostics} with error diagnostics");
        Console.WriteLine($"frame-preserved pairs (no obligation minted): {report.FramePreservedPairs}");
        foreach (var (classification, count) in report.ClassificationTotals)
            Console.WriteLine($"  {classification}: {count}");
        Console.WriteLine("out-of-scope reasons:");
        foreach (var (reason, count) in report.OutOfScopeReasonTotals)
            Console.WriteLine($"  {count,5}  {reason}");

        if (args.Length >= 2)
        {
            File.WriteAllText(args[1], CorpusMeasurement.ToJson(report));
            Console.WriteLine($"report written to {args[1]}");
        }
        if (args.Length == 3)
        {
            File.WriteAllText(args[2], CorpusMeasurement.ToMarkdown(report));
            Console.WriteLine($"summary written to {args[2]}");
        }

        return 0;
    }

    private static int RunManifest(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.Error.WriteLine("usage: precept-matrixtools run-manifest <manifest.json> [<out.results.json>]");
            return 2;
        }

        var manifest = CellTestConverter.FromJson(File.ReadAllText(args[0]));
        var results = ManifestRunner.Execute(manifest);

        foreach (var result in results)
        {
            Console.WriteLine($"[{result.Outcome}] {result.RowId}");
            Console.WriteLine($"    {result.Detail}");
        }

        var tally = results
            .GroupBy(r => r.Outcome)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key}={g.Count()}");
        Console.WriteLine($"{manifest.FamilyId}: {results.Length} rows — {string.Join(", ", tally)}");

        if (args.Length == 2)
        {
            File.WriteAllText(args[1], ManifestRunner.ToJson(results));
            Console.WriteLine($"results written to {args[1]}");
        }

        // Fail loudly on real misses and on stale built-status expectations.
        return results.Any(r => r.Outcome is RowOutcome.Fail or RowOutcome.UnexpectedPass) ? 1 : 0;
    }

    private static void PrintPreservation(
        IReadOnlyList<TypedAction> plan,
        TypedExpression? guard,
        IReadOnlyList<ObligationSpec> obligations)
    {
        if (plan.Count == 0)
        {
            Console.WriteLine("  (no writes — every obligation frame-preserves)");
            return;
        }

        foreach (var obligation in obligations)
        {
            var result = WpCalculator.ComputePreservationWp(plan, obligation);

            // Frame detection: a WP identical to the un-substituted obligation means
            // the plan does not touch the rule.
            if (result is WpComputed computed)
            {
                var frame = WpCalculator.ComputePreservationWp([], obligation);
                if (frame is WpComputed f && f.Wp.Key == computed.Wp.Key)
                {
                    Console.WriteLine($"  {obligation.Label}: frame-preserved (write does not touch the rule)");
                    continue;
                }
            }

            Describe(result, obligation, guard);
        }
    }

    private static void Describe(WpResult result, ObligationSpec obligation, TypedExpression? guard)
    {
        switch (result)
        {
            case WpNotSupported notSupported:
                Console.WriteLine($"  {obligation.Label}: NOT SUPPORTED — {notSupported.Reason}");
                break;

            case WpComputed computed:
                var verdicts = "";
                if (guard is not null)
                {
                    bool exact = WpCalculator.GuardMatchesWp(guard, computed.Wp);
                    bool covered = WpCalculator.GuardFactsCoverWp(guard, computed.Wp);
                    verdicts = exact
                        ? "   [guard normal-form-equal to WP]"
                        : covered
                            ? "   [guard facts cover WP]"
                            : "   [guard does not cover WP]";

                    // Conditional-rule WPs are implications; report whether the guard
                    // covers the consequent (whether the activation side discharges —
                    // e.g. by vacuity — is discharge-contract content, not decided here).
                    if (!exact && !covered && computed.Wp is CanonImplies implies
                        && WpCalculator.GuardFactsCoverWp(guard, implies.Consequent))
                    {
                        verdicts = "   [guard facts cover WP consequent; activation side not decided here]";
                    }
                }
                Console.WriteLine($"  {obligation.Label}: WP = {computed.Wp}{verdicts}");
                break;
        }
    }
}
