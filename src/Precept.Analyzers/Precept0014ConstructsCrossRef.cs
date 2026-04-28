using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0014 — Constructs catalog cross-reference consistency (invariants X31, X32).
///
/// X31: Each <c>ConstructMeta.AllowedIn</c> array must not contain the arm's own
///      <c>ConstructKind</c> (a construct cannot be nested inside itself).
///
/// X32: Two construct arms may not share a <c>ConstructMeta.PrimaryLeadingToken</c> unless their
///      slot sequences diverge — diverging slots give the parser a lookahead disambiguation
///      point. Identical leading token AND identical slot sequences = genuine ambiguity.
///
/// Scope: Only fires for <c>GetMeta(ConstructKind)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0014ConstructsCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId_SelfRef = "PRECEPT0014a";
    public const string DiagnosticId_DupToken = "PRECEPT0014b";

    private static readonly DiagnosticDescriptor SelfRefRule = new(
        DiagnosticId_SelfRef,
        title: "ConstructMeta AllowedIn must not self-reference",
        messageFormat: "ConstructKind.{0} lists itself in AllowedIn — a construct cannot be nested inside itself",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A construct's AllowedIn array declares which other constructs it " +
                     "may appear inside. Self-references would create a circular nesting rule.");

    private static readonly DiagnosticDescriptor DupTokenRule = new(
        DiagnosticId_DupToken,
        title: "ConstructMeta LeadingToken collision with identical slot sequences",
        messageFormat: "ConstructKind.{0} uses TokenKind.{1} as LeadingToken, which is already used by ConstructKind.{2} — ambiguous parse entry point",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd],
        description: "Two constructs with identical leading tokens are only ambiguous when " +
                     "their slot sequences are also identical — the parser has no slot-level " +
                     "lookahead position to distinguish them. Constructs that share a leading " +
                     "token are allowed when their slot sequences diverge.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(SelfRefRule, DupTokenRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationCtx =>
        {
            var compilation = compilationCtx.Compilation;
            var constructArms = new ConcurrentBag<ConstructArmInfo>();

            compilationCtx.RegisterOperationAction(ctx =>
            {
                var switchOp = (ISwitchExpressionOperation)ctx.Operation;

                if (!CatalogAnalysisHelpers.TryGetCatalogSwitchKind(
                        switchOp, ctx.ContainingSymbol, out var catalogEnumTypeName))
                    return;

                if (catalogEnumTypeName != "ConstructKind")
                    return;

                foreach (var arm in switchOp.Arms)
                {
                    var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
                    if (armCaseName == null) continue;

                    var creation = FindObjectCreation(arm.Value);
                    if (creation == null) continue;

                    // ── X31: AllowedIn self-reference check ──────────────────
                    CheckAllowedInSelfRef(ctx, creation, armCaseName);

                    // ── X32: collect for deferred dup-token check ────────────
                    var tokenArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "LeadingToken");
                    if (tokenArg == null) continue;
                    var tokenName = CatalogAnalysisHelpers.ResolveEnumFieldName(tokenArg);
                    if (tokenName == null) continue;

                    constructArms.Add(new ConstructArmInfo
                    {
                        ArmCase = armCaseName,
                        TokenName = tokenName,
                        TokenLocation = tokenArg.Syntax.GetLocation(),
                        SlotKinds = ExtractSlotKinds(creation, compilation),
                    });
                }
            }, OperationKind.SwitchExpression);

            // X32: deferred — needs full set of arms to compare pairs.
            compilationCtx.RegisterCompilationEndAction(endCtx =>
                CheckLeadingTokenAmbiguity(endCtx, constructArms));
        });
    }

    private static void CheckAllowedInSelfRef(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        // Find the AllowedIn argument (positional or named).
        var allowedInArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "AllowedIn");
        if (allowedInArg == null) return;

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(allowedInArg))
        {
            var fieldName = CatalogAnalysisHelpers.ResolveEnumFieldName(element);
            if (fieldName != null && fieldName == armCaseName)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    SelfRefRule,
                    element.Syntax.GetLocation(),
                    armCaseName));
            }
        }
    }

    private static void CheckLeadingTokenAmbiguity(
        CompilationAnalysisContext ctx,
        ConcurrentBag<ConstructArmInfo> arms)
    {
        // Group arms by leading token.
        var byToken = new Dictionary<string, List<ConstructArmInfo>>();
        foreach (var arm in arms)
        {
            if (!byToken.TryGetValue(arm.TokenName, out var list))
            {
                list = new List<ConstructArmInfo>();
                byToken[arm.TokenName] = list;
            }
            list.Add(arm);
        }

        // For each token shared by 2+ constructs, check all pairs.
        // Two constructs are genuinely ambiguous only when their slot sequences are
        // identical — identical slots mean the parser has no lookahead point to diverge.
        foreach (var kvp in byToken)
        {
            var group = kvp.Value;
            if (group.Count < 2) continue;

            for (int i = 0; i < group.Count; i++)
            {
                for (int j = i + 1; j < group.Count; j++)
                {
                    var a = group[i];
                    var b = group[j];

                    if (SlotSequencesIdentical(a.SlotKinds, b.SlotKinds))
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            DupTokenRule,
                            b.TokenLocation,
                            b.ArmCase,
                            kvp.Key,
                            a.ArmCase));
                    }
                    // Diverging slot sequences give the parser a disambiguation point — OK.
                }
            }
        }
    }

    private static bool SlotSequencesIdentical(List<string> a, List<string> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private static List<string> ExtractSlotKinds(
        IObjectCreationOperation creation,
        Compilation compilation)
    {
        var result = new List<string>();
        var slotsArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Slots");
        if (slotsArg == null) return result;

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(slotsArg))
        {
            var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(element);

            IObjectCreationOperation? slotCreation = null;
            if (unwrapped is IObjectCreationOperation directCreation)
                slotCreation = directCreation;
            else if (unwrapped is IFieldReferenceOperation fieldRef)
                slotCreation = CatalogAnalysisHelpers.FollowFieldInitializer(
                    fieldRef, "ConstructSlot", compilation);

            if (slotCreation == null) continue;

            var kindArg = CatalogAnalysisHelpers.GetNamedArgument(slotCreation, "Kind");
            if (kindArg == null) continue;
            var kindName = CatalogAnalysisHelpers.ResolveEnumFieldName(kindArg);
            if (kindName != null) result.Add(kindName);
        }
        return result;
    }

    private sealed class ConstructArmInfo
    {
        public string ArmCase { get; set; } = "";
        public string TokenName { get; set; } = "";
        public Location TokenLocation { get; set; } = Location.None;
        public List<string> SlotKinds { get; set; } = new();
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
