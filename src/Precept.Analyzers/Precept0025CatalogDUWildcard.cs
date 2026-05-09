using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0025 — CatalogDU Wildcard Prohibition.
///
/// A type-pattern switch expression over an abstract record marked with <c>[CatalogDU]</c>
/// must not contain a wildcard/discard arm (<c>_ =></c>) or a catch-all type arm matching
/// the abstract base. Such arms silently absorb new sealed subtypes added to the hierarchy
/// without forcing an explicit branch — the same class of bug that caused diagnostic code 116
/// (<c>UnprovedPresenceRequirement</c>) to be unreachable when <c>PresenceProofRequirement</c>
/// was added to the <c>ProofRequirement</c> DU.
///
/// Covers:
/// <list type="bullet">
/// <item>Discard pattern: <c>_ =></c></item>
/// <item>Declaration pattern over the abstract base: <c>SomeDUBase x =></c></item>
/// <item>Type pattern over the abstract base: <c>SomeDUBase =></c></item>
/// </list>
///
/// Severity: Error. Missing an explicit arm for a new subtype is a correctness defect.
/// Suppressed only in test files (file path contains ".Tests").
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Precept0025CatalogDUWildcard : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0025";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Wildcard arm in switch over [CatalogDU] type silently swallows new subtypes",
        messageFormat: "Wildcard arm _ in switch over [CatalogDU] type '{0}' silently swallows new subtypes — add an explicit arm for each subtype instead",
        category: "Precept.Pipeline",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Type-pattern switch expressions over abstract records marked with [CatalogDU] must " +
            "not contain wildcard/discard arms (_ =>) or catch-all type arms matching the abstract " +
            "base. These arms silently absorb new sealed subtypes added to the hierarchy, bypassing " +
            "exhaustiveness. Add an explicit arm for each concrete subtype instead.");

    private const string CatalogDUAttributeName = "CatalogDUAttribute";

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

        // Suppress in test files — file path contains ".Tests".
        var filePath = switchOp.Syntax.SyntaxTree.FilePath ?? string.Empty;
        if (filePath.IndexOf(".Tests", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return;

        // Resolve the [CatalogDU]-marked abstract base in the switch value's type hierarchy.
        var switchValueType = switchOp.Value.Type;
        var catalogDUBase = FindCatalogDUBase(switchValueType);
        if (catalogDUBase == null)
            return;

        // Report each arm whose pattern is a discard or a catch-all over the abstract base.
        foreach (var arm in switchOp.Arms)
        {
            if (IsProhibitedPattern(arm, catalogDUBase))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    arm.Pattern.Syntax.GetLocation(),
                    catalogDUBase.Name));
            }
        }
    }

    /// <summary>
    /// Walks the type hierarchy of <paramref name="type"/> and returns the first type that
    /// carries <c>[CatalogDU]</c>, or null if none is found. Walking upward allows catching
    /// switches over concrete subtypes when the abstract base carries the attribute.
    /// </summary>
    private static INamedTypeSymbol? FindCatalogDUBase(ITypeSymbol? type)
    {
        var current = type;
        while (current != null)
        {
            if (current is INamedTypeSymbol named && HasCatalogDUAttribute(named))
                return named;
            current = current.BaseType;
        }
        return null;
    }

    private static bool HasCatalogDUAttribute(INamedTypeSymbol type)
    {
        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.Name == CatalogDUAttributeName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the arm's pattern is a prohibited wildcard or catch-all:
    /// <list type="bullet">
    /// <item><see cref="IDiscardPatternOperation"/> — pure <c>_</c> discard.</item>
    /// <item><see cref="IDeclarationPatternOperation"/> whose matched type is the abstract base —
    ///   e.g. <c>CatalogDUBase x =></c>.</item>
    /// <item><see cref="ITypePatternOperation"/> whose matched type is the abstract base —
    ///   e.g. <c>CatalogDUBase =></c>.</item>
    /// </list>
    /// </summary>
    private static bool IsProhibitedPattern(
        ISwitchExpressionArmOperation arm,
        INamedTypeSymbol catalogDUBase)
    {
        switch (arm.Pattern)
        {
            case IDiscardPatternOperation:
                // Pure wildcard: _ =>
                return true;

            case IDeclarationPatternOperation decl:
                // Binding catch-all over the abstract base: CatalogDUBase x =>
                return decl.MatchedType != null &&
                       SymbolEqualityComparer.Default.Equals(decl.MatchedType, catalogDUBase);

            case ITypePatternOperation typePat:
                // Non-binding type pattern over the abstract base: CatalogDUBase =>
                return SymbolEqualityComparer.Default.Equals(typePat.MatchedType, catalogDUBase);

            default:
                return false;
        }
    }
}
