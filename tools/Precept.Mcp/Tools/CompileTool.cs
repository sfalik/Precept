using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Language;
using Precept.Mcp.Dtos;
using Precept.Pipeline;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class CompileTool
{
    [McpServerTool(Name = "precept_compile")]
    [Description("Parse, type-check, and analyze a precept definition. Returns compact JSON with `success`, `diagnosticCount`, compact `diagnostics`, `proofObligations`, and a one-line `summary`.")]
    public static CompileResultDto Compile(string text)
    {
        var compilation = Compiler.Compile(text);
        var diagnostics = compilation.Diagnostics.Select(MapDiagnostic).ToArray();
        var proofObligations = compilation.Proof.Obligations.Select(MapProofObligation).ToArray();

        return new CompileResultDto(
            !compilation.HasErrors,
            diagnostics.Length,
            diagnostics,
            BuildSummary(compilation),
            proofObligations);
    }

    private static CompileDiagnosticDto MapDiagnostic(Diagnostic diagnostic)
        => new(
            diagnostic.Span.StartLine,
            diagnostic.Span.StartColumn,
            FormatSeverity(diagnostic.Severity),
            FormatDiagnosticCode(diagnostic),
            diagnostic.Message);

    private static CompileProofObligationDto MapProofObligation(ProofObligation obligation)
    {
        var intervalRequirement = obligation.Requirement as IntervalContainmentProofRequirement;

        return new CompileProofObligationDto(
            obligation.Requirement.Kind.ToString(),
            obligation.Disposition.ToString(),
            obligation.Strategy?.ToString(),
            obligation.EmittedDiagnostic?.ToString(),
            obligation.Requirement.Description,
            obligation.ComputedInterval?.ToString(),
            intervalRequirement?.TargetField,
            intervalRequirement?.DeclaredMin,
            intervalRequirement?.DeclaredMax);
    }

    private static string BuildSummary(Compilation compilation)
    {
        var semantics = compilation.Semantics;
        var name = GetDefinitionName(compilation.ConstructManifest);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unknown";
        }

        var transitions = semantics.TransitionRows.Length + semantics.EventHandlers.Length;
        var typeErrors = compilation.Diagnostics.Count(diagnostic => diagnostic.Stage == DiagnosticStage.Type && diagnostic.Severity == Severity.Error);

        return $"{name}: {semantics.States.Length} states, {semantics.Events.Length} events, {transitions} transitions, {semantics.Rules.Length} rules, {semantics.Ensures.Length} ensures, {typeErrors} type errors.";
    }

    private static string GetDefinitionName(ConstructManifest manifest)
    {
        var header = manifest.Constructs.FirstOrDefault(construct => construct.Meta.Kind == ConstructKind.PreceptHeader);
        var names = header?.GetSlot<IdentifierListSlot>(ConstructSlotKind.IdentifierList)?.Names;

        return names is { Length: > 0 } value
            ? value[0]
            : string.Empty;
    }

    private static string FormatDiagnosticCode(Diagnostic diagnostic)
        => diagnostic.Code.StartsWith("PRE", StringComparison.Ordinal)
            ? diagnostic.Code
            : Enum.TryParse<DiagnosticCode>(diagnostic.Code, out var code)
                ? $"PRE{(int)code:D4}"
                : diagnostic.Code;

    private static string FormatSeverity(Severity severity)
        => severity switch
        {
            Severity.Info => "hint",
            Severity.Warning => "warning",
            Severity.Error => "error",
            _ => severity.ToString().ToLowerInvariant(),
        };
}
