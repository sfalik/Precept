using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0031 — No value modifier's <c>ApplicableTo</c> may span both a collection kind
/// and a scalar kind.
///
/// A <c>ValueModifierMeta</c> whose <c>ApplicableTo</c> lists at least one collection
/// <c>TypeKind</c> AND at least one non-collection (scalar) <c>TypeKind</c> is a both-axes
/// modifier — the shape <c>notempty</c> carried before it was retargeted to string-only.
/// Such a modifier reintroduces the collection-vs-element axis overlap: the reader cannot
/// tell whether it scopes the collection (cardinality) or each element (a value). Collection
/// cardinality belongs to count-words (<c>mincount</c>/<c>maxcount</c>); element/value
/// constraints belong to scalar modifiers. This analyzer makes the both-axes shape a
/// build-time impossibility.
///
/// <para>
/// An empty <c>ApplicableTo</c> (<c>AnyType</c>) is exempt: <c>optional</c>/<c>default</c>
/// apply to all types as field-level presence/value modifiers, not as a value-bound axis
/// overlap. The collection-kind set is derived from the <c>CollectionTypes</c> array in the
/// same <c>Modifiers</c> class — never a parallel hardcoded list.
/// </para>
///
/// Scope: Only fires for <c>GetMeta(ModifierKind)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Precept0029ModifierAxisDisjoint : DiagnosticAnalyzer
{
    public const string DiagnosticId_BothAxes = "PRECEPT0031";

    private static readonly DiagnosticDescriptor BothAxesRule = new(
        DiagnosticId_BothAxes,
        title: "Value modifier ApplicableTo must not span both a collection and a scalar kind",
        messageFormat:
            "ModifierKind.{0} ApplicableTo spans both a collection kind ({1}) and a scalar kind ({2}) — "
            + "a modifier may name only one axis. Use mincount/maxcount for collection cardinality and a "
            + "scalar modifier for element/value constraints.",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "A value modifier whose ApplicableTo lists both a collection TypeKind and a scalar TypeKind "
            + "is a both-axes modifier (the pre-retarget notempty shape). Collection cardinality belongs to "
            + "mincount/maxcount; element/value constraints belong to scalar modifiers. An empty ApplicableTo "
            + "(AnyType, e.g. optional/default) is exempt.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(BothAxesRule);

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

        // Derive the collection-kind name set from the CollectionTypes array in the same
        // Modifiers class — never a parallel hardcoded list.
        var collectionKinds = ResolveCollectionKindNames(ctx.ContainingSymbol, switchOp.SemanticModel);
        if (collectionKinds == null || collectionKinds.Count == 0)
            return; // Cannot classify without the catalog's collection set; stay silent.

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            // Only ValueModifierMeta carries ApplicableTo.
            if (creation.Type?.Name != "ValueModifierMeta") continue;

            var applicableToArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "ApplicableTo");
            if (applicableToArg == null) continue;

            var kindNames = ResolveTypeTargetKindNames(applicableToArg);

            // Empty ApplicableTo == AnyType: exempt (presence/value modifiers, not an axis overlap).
            if (kindNames.Count == 0) continue;

            string? collectionHit = null;
            string? scalarHit = null;
            foreach (var name in kindNames)
            {
                if (collectionKinds.Contains(name))
                    collectionHit ??= name;
                else
                    scalarHit ??= name;
            }

            if (collectionHit != null && scalarHit != null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    BothAxesRule,
                    applicableToArg.Syntax.GetLocation(),
                    armCaseName, collectionHit, scalarHit));
            }
        }
    }

    /// <summary>
    /// Resolves the set of <c>TypeKind</c> field names listed in the <c>CollectionTypes</c>
    /// array declared in the <c>Modifiers</c> class that contains the <c>GetMeta</c> switch.
    /// Returns null if the field cannot be found or read.
    /// </summary>
    private static HashSet<string>? ResolveCollectionKindNames(
        ISymbol containingSymbol, SemanticModel? semanticModel)
    {
        if (semanticModel == null) return null;

        var owningType = containingSymbol.ContainingType;
        if (owningType == null) return null;

        var field = owningType.GetMembers("CollectionTypes")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (field == null) return null;

        foreach (var syntaxRef in field.DeclaringSyntaxReferences)
        {
            var syntaxNode = syntaxRef.GetSyntax();

            // RS1030: use the operation's SemanticModel rather than Compilation.GetSemanticModel().
            // The CollectionTypes array and the GetMeta switch live in the same Modifiers class
            // (same syntax tree); if the field somehow lives elsewhere we cannot classify and
            // stay silent (no false positives).
            if (syntaxNode.SyntaxTree != semanticModel.SyntaxTree) continue;

            // Walk the declarator's descendants to find the array/collection initializer operation.
            var candidates = new List<Microsoft.CodeAnalysis.SyntaxNode> { syntaxNode };
            candidates.AddRange(syntaxNode.DescendantNodes());

            foreach (var node in candidates)
            {
                var op = semanticModel.GetOperation(node);
                if (op == null) continue;

                var names = ResolveTypeTargetKindNames(op);
                if (names.Count > 0)
                    return names;
            }
        }

        return null;
    }

    /// <summary>
    /// Enumerates a <c>TypeTarget[]</c> expression and resolves each element's first
    /// constructor argument to a <c>TypeKind</c> enum field name. Each element is a
    /// <c>new(TypeKind.X)</c> / <c>new TypeTarget(TypeKind.X)</c> creation; the kind name is
    /// its <c>Kind</c> argument.
    /// </summary>
    private static HashSet<string> ResolveTypeTargetKindNames(IOperation collectionExpr)
    {
        var result = new HashSet<string>();

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(collectionExpr))
        {
            var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(element);
            if (unwrapped is not IObjectCreationOperation targetCreation) continue;

            // The TypeTarget's first (Kind) argument names the TypeKind member.
            var kindArg = targetCreation.Arguments
                .Where(a => !a.IsImplicit)
                .Select(a => a.Value)
                .FirstOrDefault();
            if (kindArg == null) continue;

            var name = CatalogAnalysisHelpers.ResolveEnumFieldName(kindArg);
            if (name != null)
                result.Add(name);
        }

        return result;
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
