using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0009 — Operations catalog cross-reference consistency (S14).
///
/// PRECEPT0009a (S14): No two <c>BinaryOperationMeta</c> arms may share the same
///     composite key <c>(Op, Lhs.Kind, Rhs.Kind, Match)</c>. Duplicates cause
///     FrozenDictionary construction to throw at startup.
///
/// PRECEPT0009b: No two <c>UnaryOperationMeta</c> arms may share the same
///     composite key <c>(Op, Operand.Kind)</c>.
///
/// Scope: Only fires for <c>GetMeta(OperationKind)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0009OperationsCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId_DupBinaryKey = "PRECEPT0009a";
    public const string DiagnosticId_DupUnaryKey  = "PRECEPT0009b";

    private static readonly DiagnosticDescriptor DupBinaryKeyRule = new(
        DiagnosticId_DupBinaryKey,
        title: "Duplicate binary operation key (Op, Lhs, Rhs, Match)",
        messageFormat: "OperationKind.{0} duplicates the key ({1}, {2}, {3}, {4}) already used by OperationKind.{5} — FrozenDictionary construction will throw",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Operations catalog indexes binary operations by (Op, Lhs.Kind, Rhs.Kind, Match). " +
                     "Duplicate keys cause the FrozenDictionary to throw at startup.");

    private static readonly DiagnosticDescriptor DupUnaryKeyRule = new(
        DiagnosticId_DupUnaryKey,
        title: "Duplicate unary operation key (Op, Operand)",
        messageFormat: "OperationKind.{0} duplicates the key ({1}, {2}) already used by OperationKind.{3} — FrozenDictionary construction will throw",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Operations catalog indexes unary operations by (Op, Operand.Kind). " +
                     "Duplicate keys cause the FrozenDictionary to throw at startup.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DupBinaryKeyRule, DupUnaryKeyRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationCtx =>
        {
            var compilation = compilationCtx.Compilation;
            compilationCtx.RegisterOperationAction(
                ctx => Analyze(ctx, compilation),
                OperationKind.SwitchExpression);
        });
    }

    private static void Analyze(OperationAnalysisContext ctx, Compilation compilation)
    {
        var switchOp = (ISwitchExpressionOperation)ctx.Operation;

        if (!CatalogAnalysisHelpers.TryGetCatalogSwitchKind(
                switchOp, ctx.ContainingSymbol, out var catalogEnumTypeName))
            return;

        if (catalogEnumTypeName != "OperationKind")
            return;

        // Track seen keys for duplicate detection.
        var binaryKeys = new Dictionary<string, (string armCase, Location location)>();
        var unaryKeys = new Dictionary<string, (string armCase, Location location)>();

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            var typeName = creation.Type?.Name;
            if (typeName == null) continue;

            if (typeName == "BinaryOperationMeta")
            {
                CheckBinaryDuplicate(ctx, creation, armCaseName, binaryKeys, compilation);
            }
            else if (typeName == "UnaryOperationMeta")
            {
                CheckUnaryDuplicate(ctx, creation, armCaseName, unaryKeys, compilation);
            }
        }
    }

    /// <summary>
    /// Checks for duplicate binary operation keys: (Op, Lhs.Kind, Rhs.Kind, Match).
    /// </summary>
    private static void CheckBinaryDuplicate(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName,
        Dictionary<string, (string armCase, Location location)> seen,
        Compilation compilation)
    {
        var args = CatalogAnalysisHelpers.GetExplicitArguments(creation);

        var opName = ExtractOperatorKind(args, "Op");
        var lhsKind = ExtractParameterTypeKind(args, "Lhs", compilation);
        var rhsKind = ExtractParameterTypeKind(args, "Rhs", compilation);

        // Match defaults to QualifierMatch.Any if not specified.
        string matchName = "Any";
        if (args.TryGetValue("Match", out var matchOp))
        {
            var resolved = CatalogAnalysisHelpers.ResolveEnumFieldName(matchOp);
            if (resolved != null)
                matchName = resolved;
        }

        if (opName == null || lhsKind == null || rhsKind == null)
            return;

        var key = $"{opName}|{lhsKind}|{rhsKind}|{matchName}";

        if (seen.TryGetValue(key, out var existing))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DupBinaryKeyRule,
                creation.Syntax.GetLocation(),
                armCaseName,
                opName,
                lhsKind,
                rhsKind,
                matchName,
                existing.armCase));
        }
        else
        {
            seen[key] = (armCaseName, creation.Syntax.GetLocation());
        }
    }

    /// <summary>
    /// Checks for duplicate unary operation keys: (Op, Operand.Kind).
    /// </summary>
    private static void CheckUnaryDuplicate(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName,
        Dictionary<string, (string armCase, Location location)> seen,
        Compilation compilation)
    {
        var args = CatalogAnalysisHelpers.GetExplicitArguments(creation);

        var opName = ExtractOperatorKind(args, "Op");
        var operandKind = ExtractParameterTypeKind(args, "Operand", compilation);

        if (opName == null || operandKind == null)
            return;

        var key = $"{opName}|{operandKind}";

        if (seen.TryGetValue(key, out var existing))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DupUnaryKeyRule,
                creation.Syntax.GetLocation(),
                armCaseName,
                opName,
                operandKind,
                existing.armCase));
        }
        else
        {
            seen[key] = (armCaseName, creation.Syntax.GetLocation());
        }
    }

    /// <summary>
    /// Extracts the OperatorKind enum field name from a named argument.
    /// </summary>
    private static string? ExtractOperatorKind(
        IReadOnlyDictionary<string, IOperation> args,
        string paramName)
    {
        if (!args.TryGetValue(paramName, out var op))
            return null;
        return CatalogAnalysisHelpers.ResolveEnumFieldName(op);
    }

    /// <summary>
    /// Extracts the TypeKind from a ParameterMeta constructor argument.
    /// ParameterMeta(TypeKind Kind, string? Name = null) — the first arg is the Kind.
    /// The argument may be a field reference to a shared static (e.g., PInteger).
    /// In that case, we follow the field initializer to find the TypeKind.
    /// </summary>
    private static string? ExtractParameterTypeKind(
        IReadOnlyDictionary<string, IOperation> args,
        string paramName,
        Compilation? compilation = null)
    {
        if (!args.TryGetValue(paramName, out var paramOp))
            return null;

        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(paramOp);

        // Case 1: Inline new ParameterMeta(TypeKind.X) — extract Kind from args.
        if (unwrapped is IObjectCreationOperation paramCreation)
            return ExtractKindFromParameterMeta(paramCreation);

        // Case 2: Field reference to static (e.g., PInteger).
        // Follow the field initializer to find the ParameterMeta creation, then extract Kind.
        if (unwrapped is IFieldReferenceOperation fieldRef && compilation != null)
        {
            var followed = CatalogAnalysisHelpers.FollowFieldInitializer(
                fieldRef, "ParameterMeta", compilation);
            if (followed != null)
                return ExtractKindFromParameterMeta(followed);
        }

        return null;
    }

    private static string? ExtractKindFromParameterMeta(IObjectCreationOperation creation)
    {
        var paramArgs = CatalogAnalysisHelpers.GetExplicitArguments(creation);
        if (paramArgs.TryGetValue("Kind", out var kindOp))
            return CatalogAnalysisHelpers.ResolveEnumFieldName(kindOp);
        return null;
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
