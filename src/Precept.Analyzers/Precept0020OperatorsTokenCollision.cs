using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0020 — Operators catalog token collision check.
///
/// PRECEPT0020a: No two <c>SingleTokenOp</c> arms may share the same
///     composite key <c>(Token.Kind, Arity)</c>. Duplicates cause
///     <c>Operators.ByToken</c> FrozenDictionary construction to throw.
///
/// PRECEPT0020b: No two binary <c>SingleTokenOp</c> arms may share the same
///     <c>Token.Kind</c>. Duplicates cause <c>Parser.OperatorPrecedence</c>
///     FrozenDictionary construction to throw.
///
/// Scope: Only fires for <c>GetMeta(OperatorKind)</c> switches in <c>Precept.Language</c>.
/// <c>MultiTokenOp</c> arms are skipped — use PRECEPT0023 for DU shape invariants.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0020OperatorsTokenCollision : DiagnosticAnalyzer
{
    public const string DiagnosticId_ByTokenCollision         = "PRECEPT0020a";
    public const string DiagnosticId_PrecedenceCollision      = "PRECEPT0020b";

    private static readonly DiagnosticDescriptor ByTokenCollisionRule = new(
        DiagnosticId_ByTokenCollision,
        title: "Duplicate (Token.Kind, Arity) key in Operators catalog",
        messageFormat: "OperatorKind.{0} duplicates the key ({1}, {2}) already used by OperatorKind.{3} — Operators.ByToken FrozenDictionary will throw at startup",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Operators catalog indexes SingleTokenOp entries by (Token.Kind, Arity). " +
                     "Duplicate keys cause the ByToken FrozenDictionary to throw at startup.");

    private static readonly DiagnosticDescriptor PrecedenceCollisionRule = new(
        DiagnosticId_PrecedenceCollision,
        title: "Duplicate Token.Kind among binary operators",
        messageFormat: "OperatorKind.{0} duplicates the binary Token.Kind ({1}) already used by OperatorKind.{2} — Parser.OperatorPrecedence FrozenDictionary will throw at startup",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Parser builds a FrozenDictionary keyed by Token.Kind for binary operators. " +
                     "Duplicate Token.Kind values among binary operators cause it to throw at startup.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ByTokenCollisionRule, PrecedenceCollisionRule);

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

        var byTokenKeys = new Dictionary<string, (string armCase, Location location)>();
        var binaryTokenKeys = new Dictionary<string, (string armCase, Location location)>();

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            if (creation.Type?.Name != "SingleTokenOp")
                continue;

            var tokenKindName = ExtractTokenKindFromSingleTokenOp(creation);
            var arityName     = ExtractArityFromSingleTokenOp(creation);

            if (tokenKindName == null || arityName == null)
                continue;

            // PRECEPT0020a: (TokenKind, Arity) composite key must be unique
            var compositeKey = $"{tokenKindName}|{arityName}";
            if (byTokenKeys.TryGetValue(compositeKey, out var existingByToken))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ByTokenCollisionRule,
                    creation.Syntax.GetLocation(),
                    armCaseName, tokenKindName, arityName, existingByToken.armCase));
            }
            else
            {
                byTokenKeys[compositeKey] = (armCaseName, creation.Syntax.GetLocation());
            }

            // PRECEPT0020b: TokenKind must be unique among binary arms
            if (arityName == "Binary")
            {
                if (binaryTokenKeys.TryGetValue(tokenKindName, out var existingBinary))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        PrecedenceCollisionRule,
                        creation.Syntax.GetLocation(),
                        armCaseName, tokenKindName, existingBinary.armCase));
                }
                else
                {
                    binaryTokenKeys[tokenKindName] = (armCaseName, creation.Syntax.GetLocation());
                }
            }
        }
    }

    /// <summary>
    /// Extracts the TokenKind enum field name from the <c>Token</c> argument of a
    /// <c>SingleTokenOp</c> constructor. The Token arg should be an invocation of
    /// <c>Tokens.GetMeta(TokenKind.X)</c> — returns null if the pattern doesn't match
    /// (e.g. inline <c>new TokenMeta(...)</c> — that's PRECEPT0022's job).
    /// </summary>
    private static string? ExtractTokenKindFromSingleTokenOp(IObjectCreationOperation creation)
    {
        var tokenArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Token");
        if (tokenArg == null) return null;

        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(tokenArg);

        if (unwrapped is not IInvocationOperation invocation)
            return null;

        if (invocation.TargetMethod.Name != "GetMeta")
            return null;

        if (invocation.TargetMethod.ContainingType.Name != "Tokens")
            return null;

        var firstArg = invocation.Arguments.FirstOrDefault(a => !a.IsImplicit);
        if (firstArg == null) return null;

        return CatalogAnalysisHelpers.ResolveEnumFieldName(firstArg.Value);
    }

    /// <summary>
    /// Extracts the Arity enum field name from the <c>Arity</c> argument of a
    /// <c>SingleTokenOp</c> constructor.
    /// </summary>
    private static string? ExtractArityFromSingleTokenOp(IObjectCreationOperation creation)
    {
        var arityArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Arity");
        if (arityArg == null) return null;
        return CatalogAnalysisHelpers.ResolveEnumFieldName(arityArg);
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
