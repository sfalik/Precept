using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Static helpers for TypeChecker tests. Runs the full pipeline
/// (Lexer → Parser → NameBinder → TypeChecker) and surfaces the result.
/// No test cases here — used by Slices 1–10.
/// </summary>
internal static class TypeCheckerTestHelpers
{
    // ── Core pipeline runner ──────────────────────────────────────────────

    /// <summary>
    /// Runs the full pipeline on <paramref name="preceptText"/> and returns
    /// the <see cref="SemanticIndex"/> together with all accumulated diagnostics
    /// from every pipeline stage.
    /// </summary>
    public static (SemanticIndex Index, IReadOnlyList<Diagnostic> Diagnostics) Check(string preceptText)
    {
        var tokens   = Lexer.Lex(preceptText);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);
        var symbols  = Precept.Pipeline.NameBinder.Bind(manifest);
        var index    = Precept.Pipeline.TypeChecker.Check(manifest, symbols);

        var allDiagnostics = manifest.Diagnostics
            .Concat(symbols.Diagnostics)
            .Concat(index.Diagnostics)
            .ToList();

        return (index, allDiagnostics);
    }

    // ── Assertion helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Runs the full pipeline on <paramref name="preceptText"/> and asserts that
    /// a diagnostic with code <paramref name="code"/> is present at Error severity.
    /// Returns the <see cref="SemanticIndex"/> for further assertions.
    /// </summary>
    public static SemanticIndex CheckExpectingError(string preceptText, DiagnosticCode code)
    {
        var (index, diagnostics) = Check(preceptText);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().Contain(code.ToString(),
                because: $"expected diagnostic {code} to be emitted at Error severity");

        return index;
    }

    /// <summary>
    /// Runs the full pipeline on <paramref name="preceptText"/> and asserts that
    /// no Error-severity diagnostics were produced. Returns the <see cref="SemanticIndex"/>
    /// for further assertions.
    /// </summary>
    public static SemanticIndex CheckExpectingClean(string preceptText)
    {
        var (index, diagnostics) = Check(preceptText);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Where(d => d.Code != DiagnosticCode.RequiredFieldsNeedInitialEvent.ToString())
            .Should().BeEmpty(because: "expected no Error diagnostics outside dedicated construction validation");

        return index;
    }
}
