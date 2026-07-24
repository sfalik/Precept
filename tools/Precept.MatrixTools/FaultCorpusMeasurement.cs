using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.MatrixTools;

/// <summary>
/// One fault-family proof obligation as HEAD actually minted it while compiling a corpus
/// file: which catalog requirement kind it is, which evaluation context it sits in, whether
/// the engine discharged it, and by which strategy.
/// </summary>
public sealed record FaultCorpusRow(
    string File,
    string RequirementKind,
    string RequirementDescription,
    string ContextClass,
    string ContextDetail,
    string Disposition,
    string? Strategy,
    string? EmittedDiagnostic);

/// <summary>Per-file totals plus the corpus-wide tallies the report renders.</summary>
public sealed record FaultCorpusFileSummary(
    string File,
    int Obligations,
    int Proved,
    int Unresolved,
    int ErrorDiagnostics,
    bool Compiled);

public sealed record FaultCorpusReport(
    DateTime GeneratedUtc,
    string CorpusDirectory,
    int FileCount,
    int FilesWithFaultObligations,
    int FilesWithErrorDiagnostics,
    ImmutableArray<FaultCorpusRow> Rows,
    ImmutableArray<FaultCorpusFileSummary> Files,
    ImmutableArray<(string Kind, int Count)> KindTotals,
    ImmutableArray<(string Kind, int Count)> KindTotalsUnresolved,
    ImmutableArray<(string Context, int Count)> ContextTotals,
    ImmutableArray<(string Strategy, int Count)> StrategyTotals,
    ImmutableArray<string> CatalogKindsNeverExercised,
    ImmutableArray<string> CatalogKindsWithNoCatalogSite);

/// <summary>
/// Measures the fault family against a corpus the way <see cref="CorpusMeasurement"/>
/// measures the rule × write-site family: it compiles every file and reads the proof
/// ledger's obligations, rather than re-deriving anything.
///
/// What this measures and what it cannot: the ledger records the obligations the shipped
/// engine MINTED. A site the engine does not mint at (a guard, an interpolation hole, a
/// declaration position) contributes nothing here, and its absence from these totals is
/// evidence about HEAD's minting, never evidence that the definition owes no obligation
/// there. The corpus is a must-compile filter — every sample is written to compile — so
/// unresolved obligations are near-absent by construction, and agreement between these
/// totals and the cell data is necessary but never sufficient.
/// </summary>
public static class FaultCorpusMeasurement
{
    /// <summary>
    /// The requirement kinds the fault family covers. Every <see cref="ProofRequirementKind"/>
    /// member is included: the report names the ones the corpus never exercises rather than
    /// omitting them, so a reader sees the zero rows.
    /// </summary>
    private static ImmutableArray<string> AllKinds =>
        Enum.GetValues<ProofRequirementKind>().Select(k => k.ToString()).ToImmutableArray();

    public static FaultCorpusReport MeasureDirectory(string directory)
    {
        var files = Directory.GetFiles(directory, "*.precept", SearchOption.AllDirectories)
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .ToArray();

        var rows = ImmutableArray.CreateBuilder<FaultCorpusRow>();
        var summaries = ImmutableArray.CreateBuilder<FaultCorpusFileSummary>();

        foreach (var path in files)
        {
            var name = Path.GetFileName(path);
            Compilation compilation;
            try
            {
                compilation = Precept.Compiler.Compile(File.ReadAllText(path));
            }
            catch (Exception e) when (e is InvalidOperationException or IOException)
            {
                summaries.Add(new FaultCorpusFileSummary(name, 0, 0, 0, 0, Compiled: false));
                continue;
            }

            var obligations = compilation.Proof.Obligations;
            var fileRows = obligations.Select(o => new FaultCorpusRow(
                File: name,
                RequirementKind: o.Requirement.Kind.ToString(),
                RequirementDescription: o.Requirement.Description,
                ContextClass: o.Context.GetType().Name,
                ContextDetail: DescribeContext(o.Context),
                Disposition: o.Disposition.ToString(),
                Strategy: o.Strategy?.ToString(),
                EmittedDiagnostic: o.EmittedDiagnostic?.ToString())).ToArray();

            rows.AddRange(fileRows);
            summaries.Add(new FaultCorpusFileSummary(
                name,
                fileRows.Length,
                fileRows.Count(r => r.Disposition == nameof(ProofDisposition.Proved)),
                fileRows.Count(r => r.Disposition == nameof(ProofDisposition.Unresolved)),
                compilation.Diagnostics.Count(d => d.Severity == Severity.Error),
                Compiled: true));
        }

        var all = rows.ToImmutable();
        var exercised = all.Select(r => r.RequirementKind).ToHashSet(StringComparer.Ordinal);

        return new FaultCorpusReport(
            GeneratedUtc: DateTime.UtcNow,
            CorpusDirectory: directory,
            FileCount: files.Length,
            FilesWithFaultObligations: summaries.Count(s => s.Obligations > 0),
            FilesWithErrorDiagnostics: summaries.Count(s => s.ErrorDiagnostics > 0),
            Rows: all,
            Files: summaries.ToImmutable(),
            KindTotals: Tally(all.Select(r => r.RequirementKind)),
            KindTotalsUnresolved: Tally(all.Where(r => r.Disposition == nameof(ProofDisposition.Unresolved))
                .Select(r => r.RequirementKind)),
            ContextTotals: Tally(all.Select(r => r.ContextClass)),
            StrategyTotals: Tally(all.Select(r => r.Strategy ?? "(none recorded)")),
            CatalogKindsNeverExercised: AllKinds.Where(k => !exercised.Contains(k)).ToImmutableArray(),
            // Pinned by FaultAxisEnumeratorTests: these kinds have zero catalog declaration
            // sites, so every instance is constructed in pipeline code.
            CatalogKindsWithNoCatalogSite: ["Presence", "IntervalContainment", "LengthContainment", "CountContainment", "AssignmentQualifier"]);
    }

    /// <summary>
    /// A finer-grained context label than <see cref="ObligationContext"/>'s runtime type name —
    /// distinguishes a rule condition from a state/event ensure condition (both surface as
    /// <c>ConstraintContext</c>), so the corpus measurement can match against the fault cell
    /// files' evaluation-site-category vocabulary (rule-condition / state-ensure-condition /
    /// event-ensure-condition), which the plain context-class name collapses.
    /// </summary>
    private static string DescribeContext(ObligationContext context) => context switch
    {
        ConstraintContext { Constraint: RuleIdentity } => "ConstraintContext/Rule",
        ConstraintContext { Constraint: EnsureIdentity ensure } =>
            $"ConstraintContext/Ensure:{ensure.Kind}",
        _ => context.GetType().Name,
    };

    private static ImmutableArray<(string, int)> Tally(IEnumerable<string> values) =>
        values.GroupBy(v => v, StringComparer.Ordinal)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(t => t.Item2).ThenBy(t => t.Key, StringComparer.Ordinal)
            .ToImmutableArray();

    public static string ToJson(FaultCorpusReport report) =>
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });

    public static string ToMarkdown(FaultCorpusReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"- Files measured: **{report.FileCount}**; minting at least one proof obligation: **{report.FilesWithFaultObligations}**; with error diagnostics: **{report.FilesWithErrorDiagnostics}**");
        sb.AppendLine($"- Proof obligations minted across the corpus: **{report.Rows.Length}** "
            + $"(proved **{report.Rows.Count(r => r.Disposition == nameof(ProofDisposition.Proved))}**, "
            + $"unresolved **{report.Rows.Count(r => r.Disposition == nameof(ProofDisposition.Unresolved))}**)");
        sb.AppendLine();

        sb.AppendLine("## Requirement kinds exercised");
        sb.AppendLine();
        sb.AppendLine("| Requirement kind | Minted | Unresolved |");
        sb.AppendLine("|---|---|---|");
        var unresolvedByKind = report.KindTotalsUnresolved.ToDictionary(t => t.Kind, t => t.Count, StringComparer.Ordinal);
        foreach (var (kind, count) in report.KindTotals)
            sb.AppendLine($"| {kind} | {count} | {(unresolvedByKind.TryGetValue(kind, out var u) ? u : 0)} |");
        sb.AppendLine();

        sb.AppendLine("## Requirement kinds the corpus never exercises");
        sb.AppendLine();
        if (report.CatalogKindsNeverExercised.Length == 0)
            sb.AppendLine("None — every declared requirement kind is minted somewhere in the corpus.");
        else
            foreach (var kind in report.CatalogKindsNeverExercised)
                sb.AppendLine($"- `{kind}` — zero obligations minted across the corpus. This is a fact about the corpus, not about the definition.");
        sb.AppendLine();

        sb.AppendLine("## Evaluation contexts the minted obligations sit in");
        sb.AppendLine();
        sb.AppendLine("| Obligation context | Count |");
        sb.AppendLine("|---|---|");
        foreach (var (context, count) in report.ContextTotals)
            sb.AppendLine($"| {context} | {count} |");
        sb.AppendLine();

        sb.AppendLine("## Discharge strategies HEAD used");
        sb.AppendLine();
        sb.AppendLine("| Strategy | Count |");
        sb.AppendLine("|---|---|");
        foreach (var (strategy, count) in report.StrategyTotals)
            sb.AppendLine($"| {strategy} | {count} |");
        sb.AppendLine();

        sb.AppendLine("## Per-file rows");
        sb.AppendLine();
        sb.AppendLine("| File | Obligations | Proved | Unresolved | Error diagnostics |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var file in report.Files.Where(f => f.Obligations > 0 || f.ErrorDiagnostics > 0))
            sb.AppendLine($"| {file.File} | {file.Obligations} | {file.Proved} | {file.Unresolved} | {file.ErrorDiagnostics} |");
        sb.AppendLine();

        return sb.ToString();
    }
}
