namespace Precept.Pipeline;

// CATALOG-DRIVEN IMPLEMENTATION GUIDE
//
// Proof obligations are declared in catalog metadata — never hardcoded per
// operator/function/accessor/action. Before writing obligation lists:
//
//   Binary operator obligations → BinaryOperationMeta.ProofRequirements
//   Function overload obligations → FunctionOverload.ProofRequirements
//   Type accessor obligations    → TypeAccessor.ProofRequirements
//   Action obligations           → ActionMeta.ProofRequirements
//   Obligation dispatch          → ProofRequirements.GetMeta(kind)
//
// Subject shape (what the obligation applies to) is encoded in the requirement
// instance (ParamSubject, SelfSubject, etc.) — do not hardcode per-requirement
// subject logic.
//
// See: docs/language/catalog-system.md § ProofEngine-catalog integration pattern

public static class ProofEngine
{
    public static ProofLedger Prove(SemanticIndex semantics, StateGraph graph) => throw new NotImplementedException();
}
