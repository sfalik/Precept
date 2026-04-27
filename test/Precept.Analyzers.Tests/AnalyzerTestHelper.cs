using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Precept.Analyzers.Tests;

/// <summary>
/// Compiles a C# source string, runs the specified analyzer against it, and
/// returns only the diagnostics the analyzer produced (no compiler errors).
/// </summary>
internal static class AnalyzerTestHelper
{
    /// <summary>
    /// Compiles <paramref name="source"/> and runs <typeparamref name="TAnalyzer"/> against it.
    /// Throws <see cref="InvalidOperationException"/> if the source has compiler errors — this
    /// prevents "should be empty" tests from passing vacuously when the source is malformed
    /// and the analyzer never fires.
    /// </summary>
    internal static Task<IReadOnlyList<Diagnostic>> AnalyzeAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
        => AnalyzeAsync<TAnalyzer>(new[] { source });

    /// <summary>
    /// Compiles multiple source strings (each as a separate syntax tree) and runs
    /// <typeparamref name="TAnalyzer"/> against the combined compilation.
    /// Used for cross-catalog analyzers that accumulate data across files.
    /// </summary>
    internal static async Task<IReadOnlyList<Diagnostic>> AnalyzeAsync<TAnalyzer>(params string[] sources)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        var dotnetDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        // System.Runtime.dll handles type-forwarding for common BCL types on .NET Core.
        var systemRuntime = Path.Combine(dotnetDir, "System.Runtime.dll");
        if (File.Exists(systemRuntime))
            references.Add(MetadataReference.CreateFromFile(systemRuntime));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Guard: fail fast if the test source itself doesn't compile. Without this, a typo in
        // test source can cause the analyzer to silently not fire, making "should be empty"
        // assertions pass vacuously.
        var compilerErrors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (compilerErrors.Count > 0)
        {
            var messages = string.Join("\n", compilerErrors.Select(d => $"  {d.Id}: {d.GetMessage()}"));
            throw new InvalidOperationException(
                $"Test source has {compilerErrors.Count} compiler error(s) — fix the source before running the analyzer:\n{messages}");
        }

        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()));

        return (await withAnalyzers.GetAnalyzerDiagnosticsAsync())
            .Where(d => d.Severity != DiagnosticSeverity.Hidden)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToList();
    }
}
