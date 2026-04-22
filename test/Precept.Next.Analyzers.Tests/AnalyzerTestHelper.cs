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
    internal static async Task<IReadOnlyList<Diagnostic>> AnalyzeAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

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
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()));

        return (await withAnalyzers.GetAnalyzerDiagnosticsAsync())
            .Where(d => d.Severity != DiagnosticSeverity.Hidden)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToList();
    }
}
