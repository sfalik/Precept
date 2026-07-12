using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Precept;

namespace Precept.Bench;

// Compile-latency benchmark. Refreshes the documented ~44ms-full-corpus / ~3ms-worst-file
// figures on current HEAD, to check the interactive (debounced-keystroke) compile budget:
// the language server recompiles a single file per keystroke, so worst-single-file latency
// is the budget that matters. Samples are read once in setup so only Compiler.Compile is timed.
[ShortRunJob]
[MemoryDiagnoser]
public class CompileBench
{
    private string[] _allSources = Array.Empty<string>();
    private string _worstSource = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        string samplesDir = FindSamplesDir();
        _allSources = Array.ConvertAll(Directory.GetFiles(samplesDir, "*.precept"), File.ReadAllText);
        foreach (string source in _allSources)
        {
            if (source.Length > _worstSource.Length)
            {
                _worstSource = source;
            }
        }
    }

    // Full corpus: every sample compiled once (documented ~44ms).
    [Benchmark]
    public int CompileWholeCorpus()
    {
        int diagnostics = 0;
        foreach (string source in _allSources)
        {
            diagnostics += Compiler.Compile(source).Diagnostics.Length;
        }
        return diagnostics;
    }

    // Single largest file — the per-keystroke unit the debounce budget is held against (documented ~3ms).
    [Benchmark]
    public int CompileWorstFile() => Compiler.Compile(_worstSource).Diagnostics.Length;

    private static string FindSamplesDir()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "samples");
            if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.precept").Length > 0)
            {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        throw new DirectoryNotFoundException($"samples/ not found walking up from {AppContext.BaseDirectory}");
    }
}
