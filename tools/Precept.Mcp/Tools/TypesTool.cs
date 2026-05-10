using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class TypesTool
{
    [McpServerTool(Name = "precept_types")]
    [Description("Return the Precept type system reference: all types with traits, widening rules, qualifier shapes, and accessors; all value, state, event, access, and anchor modifiers; and the built-in function catalog. Call when you need to declare field types or apply modifiers.")]
    public static TypesDto Types()
    {
        var lang = LanguageTool.Language();
        return new(lang.Types, lang.Modifiers, lang.Functions);
    }
}
