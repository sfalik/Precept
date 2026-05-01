using Precept.Language;
using Precept.Pipeline.SyntaxNodes;

namespace Precept.Pipeline;

// CATALOG-DRIVEN IMPLEMENTATION GUIDE
//
// Before writing any `switch` on a *Kind enum or a hand-maintained set/list
// of language members, check whether the catalog already encodes the distinction:
//
//   Modifier applicability/exclusivity → FieldModifierMeta.ApplicableTo, ModifierMeta.MutuallyExclusiveWith
//   Modifier subsumption              → FieldModifierMeta.Subsumes
//   Access-mode semantics             → AccessModifierMeta.IsPresent, IsWritable
//   Anchor scope/target               → AnchorModifierMeta.Scope, Target
//   Function resolution               → Functions.FindByName(name), FunctionMeta.Overloads, FunctionOverload.Match
//   Operator resolution               → Operations.FindUnary(op, type), Operations.FindCandidates(op, lhs, rhs)
//   Type widening / traits            → TypeMeta.WidensTo, Traits, QualifierShape, ImpliedModifiers, Accessors
//   Action legality                   → ActionMeta.ApplicableTo, AllowedIn, SyntaxShape, ValueRequired, IntoSupported
//   Proof obligations                 → BinaryOperationMeta.ProofRequirements, FunctionOverload.ProofRequirements,
//                                       TypeAccessor.ProofRequirements, ActionMeta.ProofRequirements
//
// Switching on a DU subtype is correct — the subtype IS the metadata shape.
// Switching on a catalog member's enum identity to apply per-member behavior is the smell.
//
// See: docs/language/catalog-system.md § TypeChecker-catalog integration pattern

[Precept.HandlesCatalogExhaustively(typeof(ExpressionFormKind))]
public static class TypeChecker
{
    public static SemanticIndex Check(SyntaxTree tree) => throw new NotImplementedException();

    [HandlesForm(ExpressionFormKind.Literal)]
    [HandlesForm(ExpressionFormKind.Identifier)]
    [HandlesForm(ExpressionFormKind.Grouped)]
    [HandlesForm(ExpressionFormKind.BinaryOperation)]
    [HandlesForm(ExpressionFormKind.UnaryOperation)]
    [HandlesForm(ExpressionFormKind.MemberAccess)]
    [HandlesForm(ExpressionFormKind.Conditional)]
    [HandlesForm(ExpressionFormKind.FunctionCall)]
    [HandlesForm(ExpressionFormKind.MethodCall)]
    [HandlesForm(ExpressionFormKind.ListLiteral)]
    [HandlesForm(ExpressionFormKind.PostfixOperation)]
    private static void CheckExpression(Expression expression) =>
        throw new NotImplementedException("TypeChecker expression handling — Phase 3 implementation");
}
