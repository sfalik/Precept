using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0018 — Enum members at value 0 in Precept.* namespaces must be either:
/// (a) named "None" (structural sentinel), (b) in a [Flags] enum, or
/// (c) marked with [AllowZeroDefault]. All other zero-valued members are flagged
/// because default(T) silently routes to them.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0018SemanticEnumZeroSlot : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0018";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Enum member at value 0 in semantic enum",
        messageFormat: "Enum member '{0}' in '{1}' has value 0 — semantic enums must use explicit " +
                       "1-based values so default(T) throws instead of silently routing. " +
                       "Assign '{0} = 1' (and renumber subsequent members) or mark with [AllowZeroDefault] " +
                       "if zero-init is intentional.",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every enum where all members are semantically meaningful must leave the zero " +
                     "slot unnamed. default(T) = (T)0 = unnamed = SwitchExpressionException rather " +
                     "than silent routing to an arbitrary first member. Enums with None = 0, " +
                     "[Flags] enums, and members marked [AllowZeroDefault] are exempt.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeEnum, SymbolKind.NamedType);
    }

    private static void AnalyzeEnum(SymbolAnalysisContext ctx)
    {
        var type = (INamedTypeSymbol)ctx.Symbol;

        if (type.TypeKind != TypeKind.Enum)
            return;

        if (!IsInPreceptNamespace(type))
            return;

        // E1: [Flags] enums are entirely exempt.
        if (type.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "FlagsAttribute" &&
            a.AttributeClass.ContainingNamespace?.ToDisplayString() == "System"))
            return;

        foreach (var member in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.HasConstantValue)
                continue;

            if (System.Convert.ToInt64(member.ConstantValue) != 0L)
                continue;

            // E2: Named "None" — structural sentinel.
            if (member.Name == "None")
                continue;

            // E3: [AllowZeroDefault] attribute.
            if (member.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "AllowZeroDefaultAttribute"))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Rule,
                member.Locations.FirstOrDefault(),
                member.Name,
                type.Name));
        }
    }

    private static bool IsInPreceptNamespace(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace;
        while (ns != null && !ns.IsGlobalNamespace)
        {
            if (ns.Name == "Precept")
                return true;
            ns = ns.ContainingNamespace;
        }
        return false;
    }
}
