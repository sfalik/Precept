using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0019 — Pipeline Coverage Exhaustiveness.
///
/// For every class decorated with <c>[HandlesCatalogExhaustively(typeof(T))]</c>, verifies that
/// each member of enum <c>T</c> has at least one method in that class annotated with
/// <c>[HandlesForm(T.Member)]</c>.
///
/// This is catalog-agnostic: adding a new catalog enum and decorating pipeline classes with the
/// class marker automatically enrolls them in coverage enforcement — no analyzer changes needed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Precept0019PipelineCoverageExhaustiveness : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0019";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Pipeline class must handle every catalog enum member",
        messageFormat: "{0} is missing [HandlesForm] coverage for {1} member(s): {2}",
        category: "Precept.Pipeline",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Classes decorated with [HandlesCatalogExhaustively(typeof(T))] must have at least one " +
            "method annotated with [HandlesForm(T.X)] for every member of enum T. " +
            "Missing members indicate a pipeline gap that will cause runtime failures.");

    private const string ClassMarkerName = "HandlesCatalogExhaustivelyAttribute";
    private const string MethodMarkerName = "HandlesFormAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
    }

    private static void AnalyzeClass(SymbolAnalysisContext ctx)
    {
        var classSymbol = (INamedTypeSymbol)ctx.Symbol;

        // Find all [HandlesCatalogExhaustively(typeof(T))] attributes on this class.
        foreach (var classAttr in classSymbol.GetAttributes())
        {
            if (!IsClassMarker(classAttr))
                continue;

            // Extract the typeof(T) argument — the catalog enum type.
            if (classAttr.ConstructorArguments.Length != 1)
                continue;

            var typeArg = classAttr.ConstructorArguments[0];
            if (typeArg.Value is not INamedTypeSymbol catalogEnum || catalogEnum.TypeKind != TypeKind.Enum)
                continue;

            // Collect all members of the catalog enum.
            var allMembers = catalogEnum.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
                .Select(f => f.Name)
                .ToImmutableHashSet();

            // Collect all [HandlesForm(T.X)] annotations across methods in this class
            // where the enum value's type matches the declared catalog enum.
            var coveredMembers = new HashSet<string>();
            foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                foreach (var methodAttr in member.GetAttributes())
                {
                    if (!IsMethodMarker(methodAttr))
                        continue;

                    if (methodAttr.ConstructorArguments.Length != 1)
                        continue;

                    var arg = methodAttr.ConstructorArguments[0];

                    // The argument is an enum value — check its type matches our catalog enum.
                    if (arg.Type != null && SymbolEqualityComparer.Default.Equals(arg.Type, catalogEnum))
                    {
                        // The value is the enum field name as a constant.
                        var fieldName = catalogEnum.GetMembers()
                            .OfType<IFieldSymbol>()
                            .FirstOrDefault(f => f.HasConstantValue &&
                                                 Equals(f.ConstantValue, arg.Value));
                        if (fieldName != null)
                            coveredMembers.Add(fieldName.Name);
                    }
                }
            }

            // Report missing members.
            var missing = allMembers.Except(coveredMembers).OrderBy(n => n).ToList();
            if (missing.Count > 0)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    classSymbol.Locations.FirstOrDefault(),
                    classSymbol.Name,
                    missing.Count.ToString(),
                    string.Join(", ", missing)));
            }
        }
    }

    private static bool IsClassMarker(AttributeData attr) =>
        attr.AttributeClass?.Name == ClassMarkerName;

    private static bool IsMethodMarker(AttributeData attr) =>
        attr.AttributeClass?.Name == MethodMarkerName;
}
