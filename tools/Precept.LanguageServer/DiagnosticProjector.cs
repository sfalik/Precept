using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;
using OmniSharpDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using OmniSharpDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;
using OmniSharpRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using PreceptSeverity = Precept.Language.Severity;

namespace Precept.LanguageServer;

/// <summary>
/// Projects Precept <see cref="Compilation"/> diagnostics to LSP <see cref="OmniSharpDiagnostic"/> objects.
/// </summary>
internal static class DiagnosticProjector
{
    public static IReadOnlyList<OmniSharpDiagnostic> Project(Compilation compilation) =>
        compilation.Diagnostics.Select(d => new OmniSharpDiagnostic
        {
            Range = ToRange(d.Span),
            Severity = MapSeverity(d.Severity),
            Code = d.Code,
            Source = "precept",
            Message = d.Message,
        }).ToArray();

    internal static OmniSharpRange ToRange(SourceSpan span) => new()
    {
        Start = new Position(
            Math.Max(span.StartLine - 1, 0),
            Math.Max(span.StartColumn - 1, 0)),
        End = new Position(
            Math.Max(span.EndLine - 1, 0),
            Math.Max(span.EndColumn - 1, 0)),
    };

    private static OmniSharpDiagnosticSeverity MapSeverity(PreceptSeverity severity) => severity switch
    {
        PreceptSeverity.Info => OmniSharpDiagnosticSeverity.Information,
        PreceptSeverity.Warning => OmniSharpDiagnosticSeverity.Warning,
        PreceptSeverity.Error => OmniSharpDiagnosticSeverity.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
    };
}
