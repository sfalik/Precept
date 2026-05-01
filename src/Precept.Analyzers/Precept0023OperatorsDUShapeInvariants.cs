using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0023 — OperatorMeta DU shape invariants.
///
/// PRECEPT0023a: <c>MultiTokenOp.Tokens</c> must have ≥ 2 elements.
///     Single-element multi-token operators are nonsensical.
///
/// PRECEPT0023b: No <c>SingleTokenOp</c> lead token may equal any <c>MultiTokenOp</c>
///     lead token. This causes operator disambiguation ambiguity at parse time.
///
/// PRECEPT0023c: No two <c>MultiTokenOp</c> entries may have the same full token sequence.
///     The <c>ByTokenSequence</c> index uses the full (TokenKind, TokenKind?, TokenKind?) tuple
///     as its key — duplicate full sequences cause a startup throw.
///
/// Scope: Only fires for <c>GetMeta(OperatorKind)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0023OperatorsDUShapeInvariants : DiagnosticAnalyzer
{
    public const string DiagnosticId_TooFewTokens        = "PRECEPT0023a";
    public const string DiagnosticId_SingleMultiCollision = "PRECEPT0023b";
    public const string DiagnosticId_MultiLeadCollision  = "PRECEPT0023c";

    private static readonly DiagnosticDescriptor TooFewTokensRule = new(
        DiagnosticId_TooFewTokens,
        title: "MultiTokenOp must have at least 2 tokens",
        messageFormat: "OperatorKind.{0} has a MultiTokenOp with {1} token(s) — must have at least 2",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "MultiTokenOp.Tokens must contain at least 2 TokenMeta entries. Single-element multi-token operators are nonsensical.");

    private static readonly DiagnosticDescriptor SingleMultiCollisionRule = new(
        DiagnosticId_SingleMultiCollision,
        title: "SingleTokenOp and MultiTokenOp share the same lead token",
        messageFormat: "OperatorKind.{0} (MultiTokenOp) shares lead token '{1}' with OperatorKind.{2} (SingleTokenOp) — parser disambiguation will fail",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "SingleTokenOp and MultiTokenOp must not share the same lead token. This causes operator disambiguation ambiguity at parse time.");

    private static readonly DiagnosticDescriptor MultiLeadCollisionRule = new(
        DiagnosticId_MultiLeadCollision,
        title: "Duplicate full token sequence among MultiTokenOp entries",
        messageFormat: "OperatorKind.{0} duplicates the MultiTokenOp full token sequence '{1}' already used by OperatorKind.{2} — causes ByTokenSequence startup collision",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "No two MultiTokenOp entries may have the same full token sequence. " +
                     "Identical sequences cause a startup throw in ByTokenSequence's dictionary construction.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(TooFewTokensRule, SingleMultiCollisionRule, MultiLeadCollisionRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationCtx =>
        {
            compilationCtx.RegisterOperationAction(
                ctx => Analyze(ctx),
                OperationKind.SwitchExpression);
        });
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var switchOp = (ISwitchExpressionOperation)ctx.Operation;

        if (!CatalogAnalysisHelpers.TryGetCatalogSwitchKind(
                switchOp, ctx.ContainingSymbol, out var catalogEnumTypeName))
            return;

        if (catalogEnumTypeName != "OperatorKind")
            return;

        var singleLeadTokens = new Dictionary<string, (string armCase, Location location)>();
        var multiLeadTokens = new Dictionary<string, (string armCase, Location location)>();
        var multiFullSequences = new Dictionary<string, (string armCase, Location location)>();

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            var typeName = creation.Type?.Name;
            if (typeName == "SingleTokenOp")
            {
                var leadToken = ExtractSingleTokenOpLeadToken(creation);
                if (leadToken != null)
                    singleLeadTokens[leadToken] = (armCaseName, creation.Syntax.GetLocation());
            }
            else if (typeName == "MultiTokenOp")
            {
                var tokensArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Tokens");
                if (tokensArg == null) continue;

                var elements = CatalogAnalysisHelpers.EnumerateCollectionElements(tokensArg).ToList();

                // PRECEPT0023a: MultiTokenOp must have at least 2 tokens
                if (elements.Count < 2)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        TooFewTokensRule,
                        creation.Syntax.GetLocation(),
                        armCaseName, elements.Count));
                    continue;
                }

                // Extract lead token (first element) — recorded for PRECEPT0023b cross-check
                var leadToken = ExtractTokenKindFromGetMeta(elements[0]);
                if (leadToken != null && !multiLeadTokens.ContainsKey(leadToken))
                    multiLeadTokens[leadToken] = (armCaseName, creation.Syntax.GetLocation());

                // PRECEPT0023c: Check for duplicate full token sequences
                var sequenceKey = BuildFullSequenceKey(elements);
                if (sequenceKey != null)
                {
                    if (multiFullSequences.TryGetValue(sequenceKey, out var existingMulti))
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            MultiLeadCollisionRule,
                            creation.Syntax.GetLocation(),
                            armCaseName, sequenceKey, existingMulti.armCase));
                    }
                    else
                    {
                        multiFullSequences[sequenceKey] = (armCaseName, creation.Syntax.GetLocation());
                    }
                }
            }
        }

        // PRECEPT0023b: Check for SingleTokenOp vs MultiTokenOp lead token collisions
        foreach (var kvp in multiLeadTokens)
        {
            var leadToken = kvp.Key;
            var (multiArm, multiLoc) = kvp.Value;

            if (singleLeadTokens.TryGetValue(leadToken, out var singleEntry))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    SingleMultiCollisionRule,
                    multiLoc,
                    multiArm, leadToken, singleEntry.armCase));
            }
        }
    }

    /// <summary>
    /// Extracts the lead TokenKind from a SingleTokenOp by reading its Token argument.
    /// </summary>
    private static string? ExtractSingleTokenOpLeadToken(IObjectCreationOperation creation)
    {
        var tokenArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Token");
        if (tokenArg == null) return null;

        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(tokenArg);
        if (unwrapped is not IInvocationOperation invocation)
            return null;

        if (invocation.TargetMethod.Name != "GetMeta")
            return null;

        var firstArg = invocation.Arguments.FirstOrDefault(a => !a.IsImplicit);
        if (firstArg == null) return null;

        return CatalogAnalysisHelpers.ResolveEnumFieldName(firstArg.Value);
    }

    /// <summary>
    /// Extracts the TokenKind enum field name from a Tokens.GetMeta(TokenKind.X) invocation.
    /// </summary>
    private static string? ExtractTokenKindFromGetMeta(IOperation op)
    {
        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(op);
        if (unwrapped is not IInvocationOperation invocation)
            return null;

        if (invocation.TargetMethod.Name != "GetMeta")
            return null;

        var firstArg = invocation.Arguments.FirstOrDefault(a => !a.IsImplicit);
        if (firstArg == null) return null;

        return CatalogAnalysisHelpers.ResolveEnumFieldName(firstArg.Value);
    }

    /// <summary>
    /// Builds a comma-joined key from all token kinds in a MultiTokenOp element list,
    /// e.g. "Is,Set" or "Is,Not,Set". Returns null if any element cannot be resolved.
    /// </summary>
    private static string? BuildFullSequenceKey(System.Collections.Generic.List<IOperation> elements)
    {
        var parts = new System.Collections.Generic.List<string>(elements.Count);
        foreach (var element in elements)
        {
            var tokenKind = ExtractTokenKindFromGetMeta(element);
            if (tokenKind == null) return null;
            parts.Add(tokenKind);
        }
        return string.Join(",", parts);
    }

    private static IObjectCreationOperation? FindObjectCreation(IOperation op)
    {
        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(op);
        if (unwrapped is IObjectCreationOperation creation)
            return creation;

        foreach (var child in unwrapped.ChildOperations)
        {
            var found = FindObjectCreation(child);
            if (found != null) return found;
        }

        return null;
    }
}
