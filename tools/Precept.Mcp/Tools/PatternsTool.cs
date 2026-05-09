using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class PatternsTool
{
    [McpServerTool(Name = "precept_patterns")]
    [Description("Return the Precept pattern catalog: 8 verified common patterns showing how language features combine in real-world definitions, and 3 anti-patterns showing common mistakes with correct alternatives. Call before writing your first precept draft.")]
    public static PatternsDto Patterns()
    {
        var sr = LanguageTool.Language().SyntaxReference;
        return new(sr.CommonPatterns, sr.AntiPatterns);
    }
}
