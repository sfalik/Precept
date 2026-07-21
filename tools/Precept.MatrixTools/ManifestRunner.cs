using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.MatrixTools;

// ════════════════════════════════════════════════════════════════════════════
//  ManifestRunner — executes a cell-derived test manifest through the full
//  compiler pipeline and verdicts each row from structured compile results:
//  proof-ledger obligation records (requirement subtype, disposition, strategy)
//  and diagnostic codes/stages — never diagnostic message text.
//
//  A row's verdict combines the definitional expectation with the recorded
//  built-at-HEAD expectation:
//    must-pass        → Pass when the run matches the definition, else Fail
//    expected-failure → ExpectedFailure when the run misses as recorded,
//                       UnexpectedPass when it unexpectedly matches (the
//                       machinery may have landed; the manifest is stale)
//    unstated         → Pass when the run matches, else UnstatedObservation
//                       (recorded, not claimed either way)
// ════════════════════════════════════════════════════════════════════════════

public enum RowOutcome
{
    /// <summary>The run matched the definitional expectation.</summary>
    Pass,
    /// <summary>The run missed the definitional expectation, as the manifest records for an unbuilt strategy.</summary>
    ExpectedFailure,
    /// <summary>The run matched the definitional expectation although the manifest recorded the strategy unbuilt.</summary>
    UnexpectedPass,
    /// <summary>The run missed the definitional expectation with no recorded reason — a real failure.</summary>
    Fail,
    /// <summary>The manifest records no built status for the row; the run's result is recorded without a claim.</summary>
    UnstatedObservation,
}

public sealed record RowResult(
    string RowId,
    string CellId,
    ManifestRowKind Kind,
    ExpectedOutcome Expected,
    HeadExpectation Head,
    RowOutcome Outcome,
    bool CompiledClean,
    ImmutableArray<string> ErrorCodes,
    /// <summary>An obligation record matching the row's structural key exists in the proof ledger.</summary>
    bool ObligationMinted,
    /// <summary>A matching obligation record is unresolved (the rejection names the obligation).</summary>
    bool ObligationUnresolved,
    /// <summary>The strategy that proved the matching obligation, when it was proved.</summary>
    string? ProvedStrategy,
    string Detail);

public static class ManifestRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string ToJson(ImmutableArray<RowResult> results) =>
        JsonSerializer.Serialize(results, JsonOptions);

    public static ImmutableArray<RowResult> Execute(TestManifest manifest)
    {
        var results = ImmutableArray.CreateBuilder<RowResult>(manifest.Rows.Length);
        foreach (var row in manifest.Rows)
            results.Add(ExecuteRow(row));
        return results.MoveToImmutable();
    }

    public static RowResult ExecuteRow(ManifestRow row)
    {
        Compilation compilation;
        try
        {
            compilation = Compiler.Compile(row.Program);
        }
        catch (Exception e)
        {
            return new RowResult(
                row.Id, row.CellId, row.Kind, row.Expected, row.Head,
                Outcome: RowOutcome.Fail,
                CompiledClean: false,
                ErrorCodes: ["PIPELINE_CRASH"],
                ObligationMinted: false,
                ObligationUnresolved: false,
                ProvedStrategy: null,
                Detail: $"the pipeline threw instead of compiling: {e.GetType().Name}: {e.Message}");
        }

        var errorCodes = compilation.Diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Distinct()
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToImmutableArray();

        var matched = compilation.Proof.Obligations
            .Where(o => Matches(o, row.Obligation))
            .ToArray();
        bool minted = matched.Length > 0;
        bool unresolved = matched.Any(o => o.Disposition == ProofDisposition.Unresolved);
        var provedStrategy = matched
            .Where(o => o.Disposition == ProofDisposition.Proved && o.Strategy is not null)
            .Select(o => o.Strategy!.Value.ToString())
            .FirstOrDefault();

        bool nonProofErrors = compilation.Diagnostics.Any(
            d => d.Severity == Severity.Error && d.Stage != DiagnosticStage.Proof);

        // A witness program is base plus a well-formed addition. A parse or type error
        // is not a rejection for the target obligation, and must never read as one —
        // otherwise a broken addition passes as a near-miss.
        bool wellFormed = !nonProofErrors;

        // Obligation identity is (constraint, site). The structural key carries no
        // site, so a program with several matching sites cannot support a
        // "same obligation" claim; that is reported, never assumed away.
        var matchedContexts = matched.Select(o => o.Context).Distinct().Count();
        bool siteAmbiguous = matchedContexts > 1;

        bool matchedDefinition;
        string detail;
        if (row.Expected == ExpectedOutcome.Reject)
        {
            // Rejection must name the obligation: a matching ledger record left
            // unresolved, on a well-formed program — and, where the cell states base
            // minimality, no other obligation of the program is left unresolved.
            bool otherUnresolved = compilation.Proof.Obligations.Any(
                o => o.Disposition == ProofDisposition.Unresolved && !Matches(o, row.Obligation));

            matchedDefinition = compilation.HasErrors
                && unresolved
                && wellFormed
                && !siteAmbiguous
                && (!row.RequireOnlyObligationDiagnostics || !otherUnresolved);
            detail = $"expected reject naming {Describe(row.Obligation)}; "
                + $"hasErrors={compilation.HasErrors}, obligation minted={minted}, unresolved={unresolved}, "
                + $"wellFormed={wellFormed}, errorCodes=[{string.Join(", ", errorCodes)}]"
                + (row.RequireOnlyObligationDiagnostics ? $", otherUnresolvedObligations={otherUnresolved}" : "")
                + (siteAmbiguous
                    ? $"; the key matches {matchedContexts} distinct write sites — obligation identity is "
                        + "(constraint, site) and the structural key cannot tell them apart"
                    : "");
        }
        else
        {
            // Acceptance must be clean and carry the proved obligation record,
            // via the expected strategy where the manifest names one.
            matchedDefinition = !compilation.HasErrors
                && minted
                && !unresolved
                && !siteAmbiguous
                && (row.ExpectedStrategy is null
                    || string.Equals(provedStrategy, row.ExpectedStrategy, StringComparison.Ordinal));
            detail = $"expected clean compile proving {Describe(row.Obligation)}"
                + (row.ExpectedStrategy is null ? "" : $" via {row.ExpectedStrategy}")
                + $"; hasErrors={compilation.HasErrors}, obligation minted={minted}, unresolved={unresolved}, "
                + $"provedStrategy={provedStrategy ?? "(none)"}, errorCodes=[{string.Join(", ", errorCodes)}]";
        }

        var outcome = (row.Head, matchedDefinition) switch
        {
            (HeadExpectation.MustPass, true) => RowOutcome.Pass,
            (HeadExpectation.MustPass, false) => RowOutcome.Fail,
            (HeadExpectation.ExpectedFailure, false) => RowOutcome.ExpectedFailure,
            (HeadExpectation.ExpectedFailure, true) => RowOutcome.UnexpectedPass,
            (HeadExpectation.Unstated, true) => RowOutcome.Pass,
            (HeadExpectation.Unstated, false) => RowOutcome.UnstatedObservation,
            _ => RowOutcome.Fail,
        };

        if (row.HeadNote is not null)
            detail += $"; head note: {row.HeadNote}";

        // Contract elements the row carries but HEAD cannot check are reported, not
        // dropped: a reader must see exactly what went unverified.
        if (!row.UnverifiableAtHead.IsDefaultOrEmpty)
            detail += "; unverified at HEAD: " + string.Join("; ", row.UnverifiableAtHead);

        return new RowResult(
            row.Id, row.CellId, row.Kind, row.Expected, row.Head,
            Outcome: outcome,
            CompiledClean: !compilation.HasErrors,
            ErrorCodes: errorCodes,
            ObligationMinted: minted,
            ObligationUnresolved: unresolved,
            ProvedStrategy: provedStrategy,
            Detail: detail);
    }

    // ── Structural obligation matching ───────────────────────────────────────

    /// <summary>
    /// True when the ledger obligation is the one the row's key names: same
    /// governed field, same bound, in a context of the key's class. Matching is
    /// over requirement-record structure, never over description text.
    /// </summary>
    private static bool Matches(ProofObligation obligation, ObligationKey key)
    {
        bool contextMatches = key.ContextClass switch
        {
            ObligationContextClass.WriteSite =>
                obligation.Context is EventHandlerContext or TransitionRowContext,
            ObligationContextClass.FieldDefault =>
                obligation.Context is FieldDefaultContext,
            _ => false,
        };
        if (!contextMatches)
            return false;

        return obligation.Requirement switch
        {
            IntervalContainmentProofRequirement interval when interval.TargetField == key.Field =>
                key.Kind switch
                {
                    BoundKind.UpperInclusive => interval.DeclaredMax == key.Bound,
                    BoundKind.LowerInclusive => interval.DeclaredMin == key.Bound,
                    _ => false,
                },
            NumericProofRequirement numeric when obligation.Context is FieldDefaultContext fieldDefault
                    && fieldDefault.Field.Name == key.Field =>
                numeric.Threshold == key.Bound
                    && key.Kind == (numeric.Comparison switch
                    {
                        OperatorKind.LessThanOrEqual => BoundKind.UpperInclusive,
                        OperatorKind.LessThan => BoundKind.UpperExclusive,
                        OperatorKind.GreaterThanOrEqual => BoundKind.LowerInclusive,
                        OperatorKind.GreaterThan => BoundKind.LowerExclusive,
                        _ => BoundKind.Equal,
                    }),
            _ => false,
        };
    }

    private static string Describe(ObligationKey key) =>
        $"obligation '{key.Label}' ({key.Field} {key.Kind} {key.Bound})";
}
