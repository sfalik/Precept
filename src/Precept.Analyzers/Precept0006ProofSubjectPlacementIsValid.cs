using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0006 — ProofSubject type/placement validity.
///
/// Rule 1: PresenceProofRequirement must use SelfSubject (not ParamSubject).
///   Presence proof is about the field itself being non-null. Using ParamSubject
///   incorrectly targets a function parameter instead of the field, making the
///   proof obligation impossible to satisfy.
///
/// Rule 2: SelfSubject with a non-null Accessor must only appear in PresenceProofRequirement,
///   when used in a BinaryOperationMeta, UnaryOperationMeta, or FunctionOverload proof context.
///   An accessor-bearing SelfSubject in NumericProofRequirement/DimensionProofRequirement/etc.
///   is almost always a mistake — the accessor was likely intended to appear in a presence
///   proof for that accessor, not in a numeric constraint.
///
///   Scoping note: Rule 2 only fires inside BinaryOperationMeta/UnaryOperationMeta/FunctionOverload
///   constructions. TypeAccessor.ProofRequirements legitimately use SelfSubject(countAccessor)
///   in NumericProofRequirement to express "the count of this collection must be > 0" — that
///   pattern is correct and is not flagged.
///
/// Scoped to Precept.Language to avoid false positives on unrelated code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0006ProofSubjectPlacementIsValid : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0006";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "ProofSubject type/placement is invalid",
        messageFormat: "{0}",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ProofSubject types must be used in compatible proof requirement contexts. " +
                     "PresenceProofRequirement must use SelfSubject. SelfSubject with a non-null " +
                     "Accessor must only appear in PresenceProofRequirement within operation/function overloads.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.ObjectCreation);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var op = (IObjectCreationOperation)ctx.Operation;

        var typeName = op.Type?.Name;
        var ns = op.Type?.ContainingNamespace;
        if (ns?.Name != "Language" || ns.ContainingNamespace?.Name != "Precept") return;

        // ── Rule 1: PresenceProofRequirement must use SelfSubject, not ParamSubject ──────
        if (typeName == "PresenceProofRequirement")
        {
            if (op.Arguments.Length > 0)
            {
                var subjectValue = UnwrapConversions(op.Arguments[0].Value);
                if (subjectValue is IObjectCreationOperation subjectCreation &&
                    subjectCreation.Type?.Name == "ParamSubject")
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Rule, subjectCreation.Syntax.GetLocation(),
                        "PresenceProofRequirement must use SelfSubject, not ParamSubject — " +
                        "presence proof is about the field itself, not a function parameter"));
                }
            }
            return;
        }

        // ── Rule 2: SelfSubject with non-null Accessor in non-PresenceProofRequirement ──
        // Only applies inside BinaryOperationMeta/UnaryOperationMeta/FunctionOverload context.
        // TypeAccessor.ProofRequirements uses SelfSubject(accessor) in NumericProofRequirement
        // legitimately (count > 0), so that context is excluded.
        bool isOperationProofType = typeName is "NumericProofRequirement"
                                              or "DimensionProofRequirement"
                                              or "QualifierCompatibilityProofRequirement"
                                              or "ModifierRequirement";
        if (!isOperationProofType) return;
        if (!IsInsideOperationOrFunctionOverload(op)) return;

        // Check subject arguments. QualifierCompatibilityProofRequirement has two subjects.
        int subjectCount = typeName == "QualifierCompatibilityProofRequirement" ? 2 : 1;
        for (int i = 0; i < subjectCount && i < op.Arguments.Length; i++)
        {
            var subjectValue = UnwrapConversions(op.Arguments[i].Value);
            if (subjectValue is IObjectCreationOperation selfSubjectCreation &&
                selfSubjectCreation.Type?.Name == "SelfSubject" &&
                HasNonNullAccessor(selfSubjectCreation))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Rule, selfSubjectCreation.Syntax.GetLocation(),
                    $"SelfSubject with Accessor must only appear in PresenceProofRequirement — " +
                    $"using an accessor-bearing SelfSubject in {typeName} within an operation or function overload is likely wrong"));
            }
        }
    }

    private static bool IsInsideOperationOrFunctionOverload(IOperation op)
    {
        var current = op.Parent;
        while (current != null)
        {
            if (current is IObjectCreationOperation creation &&
                creation.Type?.Name is "BinaryOperationMeta" or "UnaryOperationMeta" or "FunctionOverload")
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private static bool HasNonNullAccessor(IObjectCreationOperation selfSubjectOp)
    {
        // SelfSubject(TypeAccessor? Accessor = null) — if no explicit argument, accessor is null.
        if (selfSubjectOp.Arguments.Length == 0) return false;

        var arg = selfSubjectOp.Arguments[0];
        // Implicit default-value arguments (from omitted optional parameters) mean null accessor.
        if (arg.IsImplicit) return false;

        var value = arg.Value;
        // Explicit null literal.
        if (value is ILiteralOperation lit && lit.ConstantValue.HasValue && lit.ConstantValue.Value == null)
            return false;
        // default expression.
        if (value is IDefaultValueOperation) return false;

        return true;
    }

    private static IOperation UnwrapConversions(IOperation op)
    {
        while (op is IConversionOperation conv && conv.IsImplicit)
            op = conv.Operand;
        return op;
    }
}
