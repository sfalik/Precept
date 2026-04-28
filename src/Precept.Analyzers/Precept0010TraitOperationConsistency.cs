using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0010 — Trait↔Operation consistency across Types and Operations catalogs
/// (X14–X17).
///
/// Cross-catalog invariants between <c>Types.GetMeta(TypeKind)</c> and
/// <c>Operations.GetMeta(OperationKind)</c>:
///
/// PRECEPT0010a (X14): Every TypeKind with <c>TypeTrait.EqualityComparable</c> must
///     have <c>Equals</c> and <c>NotEquals</c> same-type binary operations.
///
/// PRECEPT0010b (X15): Every TypeKind with <c>TypeTrait.Orderable</c> must have
///     <c>LessThan</c>, <c>GreaterThan</c>, <c>LessThanOrEqual</c>, and
///     <c>GreaterThanOrEqual</c> same-type binary operations.
///
/// PRECEPT0010c (X16): Every TypeKind with same-type <c>Equals</c>/<c>NotEquals</c>
///     binary operations must have <c>EqualityComparable</c> trait on its TypeMeta.
///
/// PRECEPT0010d (X17): Every TypeKind with same-type ordering binary operations must
///     have <c>Orderable</c> trait on its TypeMeta.
///
/// Scope: Collects data from both <c>GetMeta(TypeKind)</c> and
/// <c>GetMeta(OperationKind)</c> switches in <c>Precept.Language</c>, then
/// cross-checks in a compilation-end action.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0010TraitOperationConsistency : DiagnosticAnalyzer
{
    public const string DiagnosticId_EqTraitMissingOps   = "PRECEPT0010a";
    public const string DiagnosticId_OrdTraitMissingOps  = "PRECEPT0010b";
    public const string DiagnosticId_EqOpsMissingTrait   = "PRECEPT0010c";
    public const string DiagnosticId_OrdOpsMissingTrait  = "PRECEPT0010d";

    private static readonly DiagnosticDescriptor EqTraitMissingOpsRule = new(
        DiagnosticId_EqTraitMissingOps,
        title: "EqualityComparable trait missing equality operations",
        messageFormat: "TypeKind.{0} has EqualityComparable trait but is missing same-type operations: {1}",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    private static readonly DiagnosticDescriptor OrdTraitMissingOpsRule = new(
        DiagnosticId_OrdTraitMissingOps,
        title: "Orderable trait missing ordering operations",
        messageFormat: "TypeKind.{0} has Orderable trait but is missing same-type operations: {1}",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    private static readonly DiagnosticDescriptor EqOpsMissingTraitRule = new(
        DiagnosticId_EqOpsMissingTrait,
        title: "Equality operations without EqualityComparable trait",
        messageFormat: "TypeKind.{0} has same-type Equals/NotEquals operations but lacks EqualityComparable trait on its TypeMeta",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    private static readonly DiagnosticDescriptor OrdOpsMissingTraitRule = new(
        DiagnosticId_OrdOpsMissingTrait,
        title: "Ordering operations without Orderable trait",
        messageFormat: "TypeKind.{0} has same-type ordering operations but lacks Orderable trait on its TypeMeta",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            EqTraitMissingOpsRule, OrdTraitMissingOpsRule,
            EqOpsMissingTraitRule, OrdOpsMissingTraitRule);

    private static readonly HashSet<string> EqualityOps =
        new() { "Equals", "NotEquals" };

    private static readonly HashSet<string> OrderingOps =
        new() { "LessThan", "GreaterThan", "LessThanOrEqual", "GreaterThanOrEqual" };

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationCtx =>
        {
            var compilation = compilationCtx.Compilation;

            // Shared state for cross-catalog checks (thread-safe for concurrent execution).
            var typeData = new ConcurrentDictionary<string, TypeTraitInfo>();
            var opData = new ConcurrentBag<OpInfo>();

            compilationCtx.RegisterOperationAction(ctx =>
            {
                var switchOp = (ISwitchExpressionOperation)ctx.Operation;

                if (!CatalogAnalysisHelpers.TryGetCatalogSwitchKind(
                        switchOp, ctx.ContainingSymbol, out var catalogEnumTypeName))
                    return;

                if (catalogEnumTypeName == "TypeKind")
                    CollectTypeTraits(switchOp, typeData, compilation);
                else if (catalogEnumTypeName == "OperationKind")
                    CollectOperationData(switchOp, opData, compilation);

            }, OperationKind.SwitchExpression);

            compilationCtx.RegisterCompilationEndAction(endCtx =>
                CrossCheck(endCtx, typeData, opData));
        });
    }

    // ── Data collection ─────────────────────────────────────────────────────

    private sealed class TypeTraitInfo
    {
        public bool HasEqualityComparable { get; set; }
        public bool HasOrderable { get; set; }
        public Location Location { get; set; } = Location.None;
    }

    private sealed class OpInfo
    {
        public string TypeKind { get; set; } = "";
        public string OperatorKind { get; set; } = "";
        public Location Location { get; set; } = Location.None;
        /// <summary>
        /// True when the operation's ProofRequirements contains a ModifierRequirement.
        /// Modifier-gated ordering ops are conditionally available — the type legitimately
        /// lacks the Orderable trait until the modifier is present.
        /// </summary>
        public bool IsModifierGated { get; set; }
    }

    private static void CollectTypeTraits(
        ISwitchExpressionOperation switchOp,
        ConcurrentDictionary<string, TypeTraitInfo> typeData,
        Compilation compilation)
    {
        // Resolve the TypeTrait enum type for FlagsEnumContains.
        var typeTraitType = compilation.GetTypeByMetadataName("Precept.Language.TypeTrait");

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            var info = new TypeTraitInfo
            {
                Location = creation.Syntax.GetLocation()
            };

            var traitsArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Traits");
            if (traitsArg != null && typeTraitType != null)
            {
                info.HasEqualityComparable = CatalogAnalysisHelpers.FlagsEnumContains(
                    traitsArg, "EqualityComparable", typeTraitType);
                info.HasOrderable = CatalogAnalysisHelpers.FlagsEnumContains(
                    traitsArg, "Orderable", typeTraitType);
            }

            typeData[armCaseName] = info;
        }
    }

    private static void CollectOperationData(
        ISwitchExpressionOperation switchOp,
        ConcurrentBag<OpInfo> opData,
        Compilation compilation)
    {
        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            var typeName = creation.Type?.Name;
            if (typeName != "BinaryOperationMeta") continue;

            var args = CatalogAnalysisHelpers.GetExplicitArguments(creation);

            var opName = ExtractOperatorKind(args);
            if (opName == null) continue;

            // Only care about comparison operators.
            if (!EqualityOps.Contains(opName) && !OrderingOps.Contains(opName))
                continue;

            var lhsKind = ExtractParameterTypeKind(args, "Lhs", compilation);
            var rhsKind = ExtractParameterTypeKind(args, "Rhs", compilation);

            // Only care about same-type operations (Lhs.Kind == Rhs.Kind).
            if (lhsKind == null || rhsKind == null || lhsKind != rhsKind)
                continue;

            opData.Add(new OpInfo
            {
                TypeKind = lhsKind,
                OperatorKind = opName,
                Location = creation.Syntax.GetLocation(),
                IsModifierGated = HasModifierRequirement(creation),
            });
        }
    }

    // ── Cross-catalog check ─────────────────────────────────────────────────

    private static void CrossCheck(
        CompilationAnalysisContext ctx,
        ConcurrentDictionary<string, TypeTraitInfo> typeData,
        ConcurrentBag<OpInfo> opData)
    {
        // Build a map: TypeKind → set of comparison OperatorKinds from Operations catalog.
        var opsByType = new Dictionary<string, HashSet<string>>();
        foreach (var op in opData)
        {
            if (!opsByType.TryGetValue(op.TypeKind, out var set))
            {
                set = new HashSet<string>();
                opsByType[op.TypeKind] = set;
            }
            set.Add(op.OperatorKind);
        }

        // X14: EqualityComparable → must have Equals + NotEquals ops.
        // X15: Orderable → must have LT + GT + LTE + GTE ops.
        foreach (var kvp in typeData)
        {
            var typeKind = kvp.Key;
            var info = kvp.Value;

            opsByType.TryGetValue(typeKind, out var ops);
            var opsSet = ops ?? new HashSet<string>();

            if (info.HasEqualityComparable)
            {
                var missing = EqualityOps.Where(o => !opsSet.Contains(o)).ToList();
                if (missing.Count > 0)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        EqTraitMissingOpsRule,
                        info.Location,
                        typeKind,
                        string.Join(", ", missing)));
                }
            }

            if (info.HasOrderable)
            {
                var missing = OrderingOps.Where(o => !opsSet.Contains(o)).ToList();
                if (missing.Count > 0)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        OrdTraitMissingOpsRule,
                        info.Location,
                        typeKind,
                        string.Join(", ", missing)));
                }
            }
        }

        // X16: Equals/NotEquals ops → must have EqualityComparable trait.
        // X17: Ordering ops → must have Orderable trait.
        foreach (var kvp in opsByType)
        {
            var typeKind = kvp.Key;
            var ops = kvp.Value;

            typeData.TryGetValue(typeKind, out var info);

            bool hasEqOps = ops.Any(o => EqualityOps.Contains(o));
            bool hasOrdOps = ops.Any(o => OrderingOps.Contains(o));

            if (hasEqOps && (info == null || !info.HasEqualityComparable))
            {
                // Find a representative op location for the diagnostic.
                var repOp = opData.FirstOrDefault(o => o.TypeKind == typeKind &&
                    EqualityOps.Contains(o.OperatorKind));
                var location = repOp?.Location ?? Location.None;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    EqOpsMissingTraitRule, location, typeKind));
            }

            if (hasOrdOps && (info == null || !info.HasOrderable))
            {
                // Suppress when every ordering op is modifier-gated: the operations exist
                // but are conditionally available (e.g., Choice requires ordered modifier).
                // The trait absence is intentional — it should not be advertised on the type.
                bool allModifierGated = opData
                    .Where(o => o.TypeKind == typeKind && OrderingOps.Contains(o.OperatorKind))
                    .All(o => o.IsModifierGated);

                if (!allModifierGated)
                {
                    var repOp = opData.FirstOrDefault(o => o.TypeKind == typeKind &&
                        OrderingOps.Contains(o.OperatorKind) && !o.IsModifierGated);
                    var location = repOp?.Location ?? Location.None;

                    ctx.ReportDiagnostic(Diagnostic.Create(
                        OrdOpsMissingTraitRule, location, typeKind));
                }
            }
        }
    }

    // ── Helpers (reuse patterns from PRECEPT0009) ───────────────────────────

    private static string? ExtractOperatorKind(IReadOnlyDictionary<string, IOperation> args)
    {
        if (!args.TryGetValue("Op", out var op))
            return null;
        return CatalogAnalysisHelpers.ResolveEnumFieldName(op);
    }

    private static string? ExtractParameterTypeKind(
        IReadOnlyDictionary<string, IOperation> args,
        string paramName,
        Compilation compilation)
    {
        if (!args.TryGetValue(paramName, out var paramOp))
            return null;

        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(paramOp);

        if (unwrapped is IObjectCreationOperation paramCreation)
            return ExtractKindFromParameterMeta(paramCreation);

        if (unwrapped is IFieldReferenceOperation fieldRef)
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

    /// <summary>
    /// Returns true if the operation's ProofRequirements collection contains a
    /// ModifierRequirement entry, indicating the operation is only conditionally available.
    /// </summary>
    private static bool HasModifierRequirement(IObjectCreationOperation creation)
    {
        var proofReqArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "ProofRequirements");
        if (proofReqArg == null) return false;

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(proofReqArg))
        {
            var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(element);
            if (unwrapped is IObjectCreationOperation reqCreation &&
                reqCreation.Type?.Name == "ModifierRequirement")
                return true;
        }
        return false;
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
