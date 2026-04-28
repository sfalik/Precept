using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0012 — Functions catalog cross-reference consistency (S15).
///
/// PRECEPT0012a (S15): When two or more <c>FunctionKind</c> arms share the same
///     <c>Name</c> string (e.g. Round and RoundPlaces both map to "round"),
///     every overload across those arms must have a distinct parameter count.
///     Otherwise the type checker cannot disambiguate call sites by arity alone.
///
/// PRECEPT0012b: Each arm's <c>Overloads</c> collection must not be empty —
///     a function with zero overloads is meaningless metadata.
///
/// Scope: Only fires for <c>GetMeta(FunctionKind)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0012FunctionsCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId_ArityCollision = "PRECEPT0012a";
    public const string DiagnosticId_EmptyOverloads = "PRECEPT0012b";

    private static readonly DiagnosticDescriptor ArityCollisionRule = new(
        DiagnosticId_ArityCollision,
        title: "Same-name FunctionKinds must have distinguishable overload arities",
        messageFormat: "FunctionKind.{0} and FunctionKind.{1} both map to \"{2}\" and share overload arity {3} — type checker cannot disambiguate",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When multiple FunctionKind members share the same surface name, the type " +
                     "checker resolves overloads by parameter count. If two arms contribute an " +
                     "overload with the same arity, call sites are ambiguous.");

    private static readonly DiagnosticDescriptor EmptyOverloadsRule = new(
        DiagnosticId_EmptyOverloads,
        title: "FunctionMeta must have at least one overload",
        messageFormat: "FunctionKind.{0} has an empty Overloads collection — every function must have at least one overload",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A function with zero overloads cannot be called and is meaningless metadata.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ArityCollisionRule, EmptyOverloadsRule);

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

        if (catalogEnumTypeName != "FunctionKind")
            return;

        // Collect per-arm data for the cross-arm arity check.
        // Key: Name string; Value: list of (armCase, arities, location).
        var armsByName = new Dictionary<string, List<(string armCase, HashSet<int> arities, Location location)>>();

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            // ── PRECEPT0012b: Empty overloads check ──────────────────────────
            var arities = CheckEmptyOverloads(ctx, creation, armCaseName);

            // ── Collect for PRECEPT0012a cross-arm check ─────────────────────
            var nameStr = ExtractNameString(creation);
            if (nameStr != null && arities != null)
            {
                if (!armsByName.TryGetValue(nameStr, out var list))
                {
                    list = new List<(string, HashSet<int>, Location)>();
                    armsByName[nameStr] = list;
                }
                list.Add((armCaseName, arities, creation.Syntax.GetLocation()));
            }
        }

        // ── PRECEPT0012a: Cross-arm arity collision check ────────────────────
        CheckArityCollisions(ctx, armsByName);
    }

    /// <summary>
    /// Checks that the Overloads collection is non-empty.
    /// Returns the set of overload arities (parameter counts) for cross-arm checks,
    /// or null if overloads couldn't be resolved.
    /// </summary>
    private static HashSet<int>? CheckEmptyOverloads(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var args = CatalogAnalysisHelpers.GetExplicitArguments(creation);

        if (!args.TryGetValue("Overloads", out var overloadsOp))
            return null;

        var arities = new HashSet<int>();
        bool hasAny = false;

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(overloadsOp))
        {
            hasAny = true;
            var arity = CountOverloadParameters(element);
            if (arity >= 0)
                arities.Add(arity);
        }

        // Also check for spreads — if there's a spread, we can't determine emptiness.
        if (!hasAny && !CatalogAnalysisHelpers.CollectionHasSpread(overloadsOp))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                EmptyOverloadsRule,
                overloadsOp.Syntax.GetLocation(),
                armCaseName));
        }

        return arities;
    }

    /// <summary>
    /// Counts parameters in a FunctionOverload constructor call.
    /// FunctionOverload(IReadOnlyList&lt;ParameterMeta&gt; Parameters, TypeKind ReturnType, ...).
    /// The Parameters arg is the first positional arg (a collection expression).
    /// </summary>
    private static int CountOverloadParameters(IOperation overloadOp)
    {
        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(overloadOp);
        if (unwrapped is not IObjectCreationOperation overloadCreation)
            return -1;

        var args = CatalogAnalysisHelpers.GetExplicitArguments(overloadCreation);
        if (!args.TryGetValue("Parameters", out var paramsOp))
            return -1;

        // Count elements in the Parameters collection.
        int count = 0;
        foreach (var _ in CatalogAnalysisHelpers.EnumerateCollectionElements(paramsOp))
            count++;

        return count;
    }

    /// <summary>
    /// Extracts the Name string from a FunctionMeta constructor call.
    /// Name is the second positional parameter.
    /// </summary>
    private static string? ExtractNameString(IObjectCreationOperation creation)
    {
        var args = CatalogAnalysisHelpers.GetExplicitArguments(creation);
        if (!args.TryGetValue("Name", out var nameOp))
            return null;

        return CatalogAnalysisHelpers.ResolveStringConstant(nameOp);
    }

    /// <summary>
    /// For each name group with multiple FunctionKind arms, checks that no two arms
    /// contribute overloads with the same parameter count.
    /// </summary>
    private static void CheckArityCollisions(
        OperationAnalysisContext ctx,
        Dictionary<string, List<(string armCase, HashSet<int> arities, Location location)>> armsByName)
    {
        foreach (var kvp in armsByName)
        {
            var name = kvp.Key;
            var arms = kvp.Value;

            if (arms.Count < 2)
                continue;

            // For each arity, track which arm "owns" it (first seen).
            var arityOwner = new Dictionary<int, (string armCase, Location location)>();

            foreach (var entry in arms)
            {
                var armCase = entry.armCase;
                var arities = entry.arities;
                var location = entry.location;
                foreach (var arity in arities)
                {
                    if (arityOwner.TryGetValue(arity, out var existing))
                    {
                        // Collision: two different arms have the same name and arity.
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            ArityCollisionRule,
                            location,
                            existing.armCase,
                            armCase,
                            name,
                            arity));
                    }
                    else
                    {
                        arityOwner[arity] = (armCase, location);
                    }
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
