using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0011 — Modifiers catalog cross-reference consistency (X19, X21–X23, S16).
///
/// PRECEPT0011a (X21): <c>FieldModifierMeta.Subsumes</c> must not contain the arm's own
///     <c>ModifierKind</c> (a modifier cannot subsume itself).
///
/// PRECEPT0011b (X22): <c>ModifierMeta.MutuallyExclusiveWith</c> must not contain the
///     arm's own <c>ModifierKind</c>.
///
/// PRECEPT0011c (X23): Mutual exclusivity must be symmetric — if arm A lists B in
///     <c>MutuallyExclusiveWith</c>, arm B must list A.
///
/// PRECEPT0011d (S16): <c>Subsumes</c> must not be circular — if A subsumes B, B must
///     not subsume A.
///
/// PRECEPT0011e (X19): <c>Token</c> should use <c>Tokens.GetMeta()</c>, not inline
///     <c>new TokenMeta(...)</c>.
///
/// Scope: Only fires for <c>GetMeta(ModifierKind)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0011ModifiersCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId_SubsumesSelf     = "PRECEPT0011a";
    public const string DiagnosticId_MutexSelf        = "PRECEPT0011b";
    public const string DiagnosticId_MutexAsymmetric   = "PRECEPT0011c";
    public const string DiagnosticId_SubsumesCircular  = "PRECEPT0011d";
    public const string DiagnosticId_InlineToken       = "PRECEPT0011e";

    private static readonly DiagnosticDescriptor SubsumesSelfRule = new(
        DiagnosticId_SubsumesSelf,
        title: "Subsumes must not self-reference",
        messageFormat: "ModifierKind.{0} lists itself in Subsumes — a modifier cannot subsume itself",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MutexSelfRule = new(
        DiagnosticId_MutexSelf,
        title: "MutuallyExclusiveWith must not self-reference",
        messageFormat: "ModifierKind.{0} lists itself in MutuallyExclusiveWith — a modifier cannot be mutually exclusive with itself",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MutexAsymmetricRule = new(
        DiagnosticId_MutexAsymmetric,
        title: "MutuallyExclusiveWith must be symmetric",
        messageFormat: "ModifierKind.{0} lists ModifierKind.{1} in MutuallyExclusiveWith, but {1} does not list {0} — asymmetric exclusivity",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor SubsumesCircularRule = new(
        DiagnosticId_SubsumesCircular,
        title: "Subsumes must not be circular",
        messageFormat: "ModifierKind.{0} subsumes ModifierKind.{1}, but {1} also subsumes {0} — circular subsumption",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InlineTokenRule = new(
        DiagnosticId_InlineToken,
        title: "ModifierMeta Token should use Tokens.GetMeta()",
        messageFormat: "ModifierKind.{0} creates Token inline instead of using Tokens.GetMeta(TokenKind.X)",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(SubsumesSelfRule, MutexSelfRule, MutexAsymmetricRule,
            SubsumesCircularRule, InlineTokenRule);

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

        if (catalogEnumTypeName != "ModifierKind")
            return;

        // Collect data for cross-arm checks.
        var mutexByArm = new Dictionary<string, (HashSet<string> exclusives, Location location)>();
        var subsumesByArm = new Dictionary<string, (HashSet<string> subsumes, Location location)>();

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            // Per-arm checks.
            CheckSubsumesSelfRef(ctx, creation, armCaseName);
            CheckMutexSelfRef(ctx, creation, armCaseName);
            CheckInlineToken(ctx, creation, armCaseName);

            // Collect for cross-arm checks.
            CollectMutexEntries(creation, armCaseName, mutexByArm);
            CollectSubsumesEntries(creation, armCaseName, subsumesByArm);
        }

        // Cross-arm checks.
        CheckMutexSymmetry(ctx, mutexByArm);
        CheckSubsumesCircularity(ctx, subsumesByArm);
    }

    // ── Per-arm checks ──────────────────────────────────────────────────────

    private static void CheckSubsumesSelfRef(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var arg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Subsumes");
        if (arg == null) return;

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(arg))
        {
            var name = CatalogAnalysisHelpers.ResolveEnumFieldName(element);
            if (name != null && name == armCaseName)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    SubsumesSelfRule, element.Syntax.GetLocation(), armCaseName));
            }
        }
    }

    private static void CheckMutexSelfRef(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var arg = CatalogAnalysisHelpers.GetNamedArgument(creation, "MutuallyExclusiveWith");
        if (arg == null) return;

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(arg))
        {
            var name = CatalogAnalysisHelpers.ResolveEnumFieldName(element);
            if (name != null && name == armCaseName)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    MutexSelfRule, element.Syntax.GetLocation(), armCaseName));
            }
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

    // ── Collection helpers ──────────────────────────────────────────────────

    private static void CollectMutexEntries(
        IObjectCreationOperation creation,
        string armCaseName,
        Dictionary<string, (HashSet<string> exclusives, Location location)> map)
    {
        var arg = CatalogAnalysisHelpers.GetNamedArgument(creation, "MutuallyExclusiveWith");
        if (arg == null) return;

        var set = new HashSet<string>();
        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(arg))
        {
            var name = CatalogAnalysisHelpers.ResolveEnumFieldName(element);
            if (name != null)
                set.Add(name);
        }

        if (set.Count > 0)
            map[armCaseName] = (set, creation.Syntax.GetLocation());
    }

    private static void CollectSubsumesEntries(
        IObjectCreationOperation creation,
        string armCaseName,
        Dictionary<string, (HashSet<string> subsumes, Location location)> map)
    {
        var arg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Subsumes");
        if (arg == null) return;

        var set = new HashSet<string>();
        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(arg))
        {
            var name = CatalogAnalysisHelpers.ResolveEnumFieldName(element);
            if (name != null)
                set.Add(name);
        }

        if (set.Count > 0)
            map[armCaseName] = (set, creation.Syntax.GetLocation());
    }

    // ── Cross-arm checks ────────────────────────────────────────────────────

    private static void CheckMutexSymmetry(
        OperationAnalysisContext ctx,
        Dictionary<string, (HashSet<string> exclusives, Location location)> mutexByArm)
    {
        foreach (var kvp in mutexByArm)
        {
            var armA = kvp.Key;
            var exclusivesA = kvp.Value.exclusives;
            var locationA = kvp.Value.location;

            foreach (var armB in exclusivesA)
            {
                if (armB == armA) continue; // Already reported by self-ref check.

                // Check that B lists A.
                if (!mutexByArm.TryGetValue(armB, out var bEntry) ||
                    !bEntry.exclusives.Contains(armA))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        MutexAsymmetricRule, locationA, armA, armB));
                }
            }
        }
    }

    private static void CheckSubsumesCircularity(
        OperationAnalysisContext ctx,
        Dictionary<string, (HashSet<string> subsumes, Location location)> subsumesByArm)
    {
        foreach (var kvp in subsumesByArm)
        {
            var armA = kvp.Key;
            var subsumesA = kvp.Value.subsumes;
            var locationA = kvp.Value.location;

            foreach (var armB in subsumesA)
            {
                if (armB == armA) continue; // Already reported by self-ref check.

                if (subsumesByArm.TryGetValue(armB, out var bEntry) &&
                    bEntry.subsumes.Contains(armA))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        SubsumesCircularRule, locationA, armA, armB));
                }
            }
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
