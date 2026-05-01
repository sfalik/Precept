using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0013 — Actions catalog cross-reference consistency (X26–X30, X36).
///
/// PRECEPT0013a (X26): <c>ActionMeta.Token</c> should use <c>Tokens.GetMeta()</c>,
///     not inline <c>new TokenMeta(...)</c>.
///
/// PRECEPT0013b (X29): <c>ActionMeta.AllowedIn</c> must not be empty — an action with
///     no allowed construct contexts is meaningless.
///
/// Scope: Only fires for <c>GetMeta(ActionKind)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0013ActionsCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId_InlineToken  = "PRECEPT0013a";
    public const string DiagnosticId_EmptyAllowed = "PRECEPT0013b";

    private static readonly DiagnosticDescriptor InlineTokenRule = new(
        DiagnosticId_InlineToken,
        title: "ActionMeta Token should use Tokens.GetMeta()",
        messageFormat: "ActionKind.{0} creates Token inline instead of using Tokens.GetMeta(TokenKind.X)",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EmptyAllowedRule = new(
        DiagnosticId_EmptyAllowed,
        title: "ActionMeta AllowedIn must not be empty",
        messageFormat: "ActionKind.{0} has an empty AllowedIn — an action with no allowed contexts is meaningless",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(InlineTokenRule, EmptyAllowedRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(
            ctx => Analyze(ctx), OperationKind.SwitchExpression);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var switchOp = (ISwitchExpressionOperation)ctx.Operation;

        if (!CatalogAnalysisHelpers.TryGetCatalogSwitchKind(
                switchOp, ctx.ContainingSymbol, out var catalogEnumTypeName))
            return;

        if (catalogEnumTypeName != "ActionKind")
            return;

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            CheckInlineToken(ctx, creation, armCaseName);
            CheckEmptyAllowedIn(ctx, creation, armCaseName);
        }
    }

    private static void CheckInlineToken(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var tokenArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Token");
        if (tokenArg == null) return;

        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(tokenArg);

        if (unwrapped is IDefaultValueOperation ||
            unwrapped is ILiteralOperation { ConstantValue: { Value: null } })
            return;

        if (unwrapped is IInvocationOperation)
            return;

        if (unwrapped is IObjectCreationOperation)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                InlineTokenRule, tokenArg.Syntax.GetLocation(), armCaseName));
        }
    }

    private static void CheckEmptyAllowedIn(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var arg = CatalogAnalysisHelpers.GetNamedArgument(creation, "AllowedIn");

        // If AllowedIn is not explicitly provided, the default is [] (empty) —
        // that's what we want to flag.
        if (arg == null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                EmptyAllowedRule, creation.Syntax.GetLocation(), armCaseName));
            return;
        }

        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(arg);

        // If it's a field reference (shared array like AllActionContexts), resolve
        // the initializer to check element count. AllowedIn is ConstructKind[], not
        // object creations, so we resolve the field's initializer operation directly
        // rather than using FollowFieldInitializer (which searches for IObjectCreationOperation).
        if (unwrapped is IFieldReferenceOperation fieldRef)
        {
            var field = fieldRef.Field;
            var syntaxRefs = field.DeclaringSyntaxReferences;
            if (syntaxRefs.Length == 0)
                return; // Can't resolve — assume non-empty.

            var syntaxNode = syntaxRefs[0].GetSyntax();

            // RS1030: use the operation's SemanticModel rather than Compilation.GetSemanticModel().
            // If the field declaration lives in a different syntax tree we cannot resolve
            // it from this context; assume non-empty to avoid false positives.
            var semanticModel = ctx.Operation.SemanticModel;
            if (semanticModel == null || syntaxNode.SyntaxTree != semanticModel.SyntaxTree)
                return;

            var model = semanticModel;

            // Walk ALL descendants to find the deepest array/collection operation.
            IOperation? best = null;
            foreach (var descendant in syntaxNode.DescendantNodes())
            {
                var childOp = model.GetOperation(descendant);
                if (childOp == null) continue;

                var childUnwrapped = CatalogAnalysisHelpers.UnwrapConversions(childOp);

                // Prefer an operation that is an array creation or has collection elements.
                if (childUnwrapped is IArrayCreationOperation ||
                    childUnwrapped.Kind == OperationKind.CollectionExpression)
                {
                    best = childUnwrapped;
                    break;
                }

                // Keep any operation as fallback.
                if (best == null)
                    best = childUnwrapped;
            }

            if (best != null)
                unwrapped = best;
            else
                return; // Can't resolve — assume non-empty.
        }

        // Check for empty collection expression or empty array creation.
        if (!CatalogAnalysisHelpers.EnumerateCollectionElements(unwrapped).Any() &&
            !CatalogAnalysisHelpers.CollectionHasSpread(unwrapped))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                EmptyAllowedRule, arg.Syntax.GetLocation(), armCaseName));
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
