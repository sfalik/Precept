using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Language;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class QuickstartTool
{
    [McpServerTool(Name = "precept_quickstart")]
    [Description("Return orientation content for starting a Precept authoring session: product description, core concepts, a guide to all 8 authoring tools, and minimal verified DSL examples.")]
    public static QuickstartDto Quickstart()
        => new(
            QuickstartCatalog.WhatIsPrecept,
            QuickstartCatalog.CoreGuarantee,
            QuickstartCatalog.CoreConcepts.Select(c => new CoreConceptDto(c.Name, c.Summary, c.Example)).ToArray(),
            QuickstartCatalog.ToolGuide.Select(t => new ToolGuideDto(t.ToolName, t.WhenToCall, t.ReturnsSummary)).ToArray(),
            QuickstartCatalog.MinimalExamples.Select(e => new MinimalExampleDto(e.Title, e.Description, e.DslSnippet)).ToArray()
        );
}
