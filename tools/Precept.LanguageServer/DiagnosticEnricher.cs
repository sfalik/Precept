using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LspDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;
using PreceptDiagnostic = Precept.Language.Diagnostic;
using PreceptDiagnosticCode = Precept.Language.DiagnosticCode;
using PreceptSeverity = Precept.Language.Severity;

namespace Precept.LanguageServer;

internal readonly record struct DiagnosticKey(PreceptDiagnosticCode Code, SourceSpan Span);

internal sealed record SuggestionInfo(string SuggestedName, string OriginalName);

internal static class DiagnosticEnricher
{
    public static (IReadOnlyList<LspDiagnostic> Diagnostics, IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> Suggestions)
        Enrich(Compilation compilation)
    {
        List<LspDiagnostic> diagnostics = [];
        Dictionary<DiagnosticKey, SuggestionInfo> suggestions = [];

        foreach (var diagnostic in compilation.Diagnostics)
        {
            var lsp = ProjectDiagnostic(diagnostic);

            if (TryGetMeta(diagnostic, out var code, out var meta) &&
                meta.SuggestionSources is { Length: > 0 } &&
                ExtractFailingName(diagnostic) is { } name)
            {
                var pool = BuildPool(compilation, meta.SuggestionSources);
                var suggestion = FindBestMatch(pool, name);
                if (suggestion is not null)
                {
                    lsp = lsp with { Message = $"{lsp.Message} — did you mean '{suggestion}'?" };
                    suggestions[new DiagnosticKey(code, diagnostic.Span)] = new SuggestionInfo(suggestion, name);
                }
            }

            diagnostics.Add(lsp);
        }

        return (diagnostics, suggestions);
    }

    private static bool TryGetMeta(PreceptDiagnostic diagnostic, out PreceptDiagnosticCode code, out DiagnosticMeta meta)
    {
        if (Enum.TryParse(diagnostic.Code, out code))
        {
            meta = Diagnostics.GetMeta(code);
            return true;
        }

        meta = null!;
        return false;
    }

    private static IReadOnlyList<string> BuildPool(Compilation compilation, SuggestionSource[] sources)
    {
        List<string> pool = [];

        foreach (var source in sources)
        {
            pool.AddRange(source switch
            {
                SuggestionSource.UserFields => compilation.Semantics.Fields.Select(field => field.Name),
                SuggestionSource.UserStates => compilation.Semantics.States.Select(state => state.Name),
                SuggestionSource.UserEvents => compilation.Semantics.Events.Select(@event => @event.Name),
                SuggestionSource.FunctionCatalog => Functions.All.Select(function => function.Name),
                _ => []
            });
        }

        return pool
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string? FindBestMatch(IReadOnlyList<string> pool, string name)
    {
        string? best = null;
        var bestDistance = int.MaxValue;

        foreach (var candidate in pool)
        {
            var distance = LevenshteinDistance.Compute(name, candidate);
            if (distance == 0)
            {
                return null;
            }

            if (distance > 3)
            {
                continue;
            }

            if (distance < bestDistance ||
                (distance == bestDistance && (best is null || string.Compare(candidate, best, StringComparison.Ordinal) < 0)))
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static string? ExtractFailingName(PreceptDiagnostic diagnostic)
    {
        if (diagnostic.Args.IsDefaultOrEmpty)
        {
            return null;
        }

        var name = diagnostic.Args[0];
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static LspDiagnostic ProjectDiagnostic(PreceptDiagnostic diagnostic) => new()
    {
        Range = DiagnosticProjector.ToRange(diagnostic.Span),
        Severity = MapSeverity(diagnostic.Severity),
        Code = diagnostic.Code,
        Source = "precept",
        Message = diagnostic.Message,
    };

    private static DiagnosticSeverity MapSeverity(PreceptSeverity severity) => severity switch
    {
        PreceptSeverity.Info => LspDiagnosticSeverity.Information,
        PreceptSeverity.Warning => LspDiagnosticSeverity.Warning,
        PreceptSeverity.Error => LspDiagnosticSeverity.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
    };
}
