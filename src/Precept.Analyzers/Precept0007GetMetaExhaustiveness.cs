using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0007 — Every catalog <c>GetMeta</c> switch expression must have an explicit arm for
/// every member of its discriminant enum. A discard (<c>_</c>) arm does not satisfy this
/// requirement because it silently swallows new enum members added without a corresponding
/// metadata entry — violating the catalog's guarantee of complete coverage.
///
/// Covers invariants S01–S10 (one per catalog: Tokens, Types, Operators, Operations,
/// Modifiers, Functions, Actions, Constructs, Diagnostics, Faults).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0007GetMetaExhaustiveness : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0007";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "GetMeta switch must cover every enum member",
        messageFormat: "GetMeta switch on {0} is missing explicit arm(s) for: {1}",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Catalog GetMeta switches must have an explicit case for every enum member. " +
                     "A discard arm (_) does not count as coverage because it silently absorbs " +
                     "new members added to the enum without a metadata entry.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

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

        // Resolve the enum type from the switch value.
        var inputType = CatalogAnalysisHelpers.UnwrapConversions(switchOp.Value).Type;
        if (inputType is not INamedTypeSymbol enumType || enumType.TypeKind != TypeKind.Enum)
            return;

        // Collect all enum member names.
        var allMembers = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .Select(f => f.Name)
            .ToImmutableHashSet();

        // Collect all explicitly handled cases.
        var coveredCases = new System.Collections.Generic.HashSet<string>();
        foreach (var arm in switchOp.Arms)
        {
            var caseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (caseName != null)
                coveredCases.Add(caseName);
        }

        // Compute missing members.
        var missing = allMembers.Except(coveredCases).OrderBy(n => n).ToList();
        if (missing.Count > 0)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Rule,
                switchOp.Syntax.GetLocation(),
                catalogEnumTypeName,
                string.Join(", ", missing)));
        }
    }
}
