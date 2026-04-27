using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0008 — Types catalog cross-reference consistency (X01–X08, S13).
///
/// Checks within each <c>GetMeta(TypeKind)</c> switch arm:
///
/// X05: <c>WidensTo</c> must not contain the arm's own <c>TypeKind</c> (a type cannot widen to itself).
/// X06: <c>ImpliedModifiers</c> must not contain duplicates.
/// S13: <c>Token</c> argument should use <c>Tokens.GetMeta(TokenKind.X)</c> — not a bare
///      <c>new TokenMeta(...)</c> — to ensure it references the canonical Tokens catalog entry.
///      Null is allowed (internal types like Error/StateRef have no surface keyword).
///
/// Kind identity (X01) is already covered by PRECEPT0017 (generic across all catalogs).
///
/// Scope: Only fires for <c>GetMeta(TypeKind)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0008TypesCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId_WidensSelf     = "PRECEPT0008a";
    public const string DiagnosticId_DupModifier    = "PRECEPT0008b";
    public const string DiagnosticId_InlineToken     = "PRECEPT0008c";

    private static readonly DiagnosticDescriptor WidensSelfRule = new(
        DiagnosticId_WidensSelf,
        title: "TypeMeta WidensTo must not self-reference",
        messageFormat: "TypeKind.{0} lists itself in WidensTo — a type cannot widen to itself",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A type's WidensTo array lists types it can implicitly convert to. " +
                     "Self-references would create trivial widening cycles.");

    private static readonly DiagnosticDescriptor DupModifierRule = new(
        DiagnosticId_DupModifier,
        title: "TypeMeta ImpliedModifiers must not have duplicates",
        messageFormat: "TypeKind.{0} has duplicate ImpliedModifier '{1}'",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Duplicate entries in ImpliedModifiers are redundant and likely " +
                     "indicate a copy-paste error.");

    private static readonly DiagnosticDescriptor InlineTokenRule = new(
        DiagnosticId_InlineToken,
        title: "TypeMeta Token should use Tokens.GetMeta()",
        messageFormat: "TypeKind.{0} creates Token inline instead of using Tokens.GetMeta(TokenKind.X) — cross-catalog reference should go through the Tokens catalog",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Token references should come from Tokens.GetMeta() to ensure " +
                     "they reference the canonical catalog entry, not an ad-hoc copy.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(WidensSelfRule, DupModifierRule, InlineTokenRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.SwitchExpression);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var switchOp = (ISwitchExpressionOperation)ctx.Operation;

        if (!CatalogAnalysisHelpers.TryGetCatalogSwitchKind(
                switchOp, ctx.ContainingSymbol, out var catalogEnumTypeName))
            return;

        if (catalogEnumTypeName != "TypeKind")
            return;

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            CheckWidensToSelfRef(ctx, creation, armCaseName);
            CheckImpliedModifiersDuplicates(ctx, creation, armCaseName);
            CheckTokenCrossRef(ctx, creation, armCaseName);
        }
    }

    /// <summary>X05: WidensTo must not contain the arm's own TypeKind.</summary>
    private static void CheckWidensToSelfRef(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var widensArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "WidensTo")
                     ?? CatalogAnalysisHelpers.GetInitializerAssignment(creation, "WidensTo");
        if (widensArg == null) return;

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(widensArg))
        {
            var fieldName = CatalogAnalysisHelpers.ResolveEnumFieldName(element);
            if (fieldName != null && fieldName == armCaseName)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WidensSelfRule,
                    element.Syntax.GetLocation(),
                    armCaseName));
            }
        }
    }

    /// <summary>X06: ImpliedModifiers must not have duplicates.</summary>
    private static void CheckImpliedModifiersDuplicates(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var modArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "ImpliedModifiers")
                  ?? CatalogAnalysisHelpers.GetInitializerAssignment(creation, "ImpliedModifiers");
        if (modArg == null) return;

        var seen = new HashSet<string>();
        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(modArg))
        {
            var fieldName = CatalogAnalysisHelpers.ResolveEnumFieldName(element);
            if (fieldName != null && !seen.Add(fieldName))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DupModifierRule,
                    element.Syntax.GetLocation(),
                    armCaseName,
                    fieldName));
            }
        }
    }

    /// <summary>
    /// S13: Token should use Tokens.GetMeta(), not an inline new TokenMeta().
    /// Null is allowed (internal types with no surface keyword).
    /// </summary>
    private static void CheckTokenCrossRef(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var tokenArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Token");
        if (tokenArg == null) return;

        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(tokenArg);

        // Null literal is fine — some types (Error, StateRef) have no token.
        if (unwrapped is IDefaultValueOperation || unwrapped is ILiteralOperation { ConstantValue.Value: null })
            return;

        // A method invocation (Tokens.GetMeta(...)) is the expected pattern.
        if (unwrapped is IInvocationOperation)
            return;

        // Anything else (e.g., new TokenMeta(...)) is an inline creation — warn.
        if (unwrapped is IObjectCreationOperation)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                InlineTokenRule,
                tokenArg.Syntax.GetLocation(),
                armCaseName));
        }
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
