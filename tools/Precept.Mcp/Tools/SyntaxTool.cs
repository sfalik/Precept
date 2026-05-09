using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class SyntaxTool
{
    [McpServerTool(Name = "precept_syntax")]
    [Description("Return the Precept syntax reference: constructs, action keywords, outcome keywords, operator precedence, and grammar meta-rules. Call when you need to form syntactically correct statements.")]
    public static SyntaxDto Syntax()
    {
        var lang = LanguageTool.Language();
        return new(lang.Constructs, lang.Actions, lang.Outcomes, lang.Operators, lang.SyntaxReference);
    }
}
