using System.Collections.Immutable;

namespace Precept.MatrixTools;

// ════════════════════════════════════════════════════════════════════════════
//  CanonExpr — the normal-form expression tree the WP calculator compares on.
//
//  Produced by Canonicalizer from the pipeline's TypedExpression DU
//  (src/Precept/Pipeline/SemanticIndex.cs). Two source expressions are
//  normal-form-equal iff their CanonExpr Keys are identical strings.
//  The normalization rules applied during construction live in Canon.cs.
//
//  Equality contract: compare via Key, not record equality — several nodes
//  carry ImmutableArray members, whose record equality is referential.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Base of the canonical (normal-form) expression DU.</summary>
public abstract record CanonExpr
{
    /// <summary>
    /// Deterministic S-expression key. Two expressions are normal-form-equal
    /// iff their keys are identical.
    /// </summary>
    public string Key => CanonRender.Key(this);

    /// <summary>Human-readable rendering (canonical form, infix).</summary>
    public sealed override string ToString() => CanonRender.Display(this);
}

/// <summary>Numeric literal, integer and decimal unified by value (500 ≡ 500.0).</summary>
public sealed record CanonNumber(decimal Value) : CanonExpr;

/// <summary>Boolean literal.</summary>
public sealed record CanonBool(bool Value) : CanonExpr;

/// <summary>String literal.</summary>
public sealed record CanonString(string Value) : CanonExpr;

/// <summary>
/// The "no value" marker used by establishment WPs for fields the initial
/// write plan does not cover and that declare no default.
/// </summary>
public sealed record CanonUnset : CanonExpr;

/// <summary>A pre-state field read (fields left unwritten by the plan, and all guard reads).</summary>
public sealed record CanonFieldPre(string Name) : CanonExpr;

/// <summary>An event-argument read, keyed by resolved (event, arg) identity.</summary>
public sealed record CanonArg(string EventName, string ArgName) : CanonExpr;

/// <summary>A quantifier-binding reference, alpha-renamed to its nesting depth.</summary>
public sealed record CanonBinding(int Depth) : CanonExpr;

/// <summary>Canonical comparison operators — Gt/Ge are flipped into Lt/Le at construction.</summary>
public enum CanonCompareOp { Lt, Le, Eq, Ne, EqCi, NeCi }

/// <summary>A comparison. Eq/Ne operands are commutation-sorted.</summary>
public sealed record CanonCompare(CanonCompareOp Op, CanonExpr Left, CanonExpr Right) : CanonExpr;

/// <summary>N-ary commutative operator identity.</summary>
public enum CanonNaryOp { Add, Mul, And, Or }

/// <summary>Flattened, operand-sorted n-ary node for +, *, and, or.</summary>
public sealed record CanonNary(CanonNaryOp Op, ImmutableArray<CanonExpr> Operands) : CanonExpr;

/// <summary>Non-commutative binary residue: division, modulo, membership, lookup access.</summary>
public enum CanonBinaryOp { Divide, Modulo, Contains, LookupAccess }

public sealed record CanonBinary(CanonBinaryOp Op, CanonExpr Left, CanonExpr Right) : CanonExpr;

/// <summary>Arithmetic negation of a non-literal operand (literals fold; double negation cancels).</summary>
public sealed record CanonNeg(CanonExpr Operand) : CanonExpr;

/// <summary>Logical negation (double negation cancels; literal operands fold).</summary>
public sealed record CanonNot(CanonExpr Operand) : CanonExpr;

/// <summary>Presence test (<c>is set</c>). <c>is not set</c> is built as CanonNot(CanonIsSet(..)).</summary>
public sealed record CanonIsSet(CanonExpr Operand) : CanonExpr;

/// <summary>
/// Implication node used by WPs of conditional (<c>when</c>-activated) rules:
/// WP = activation' implies body'. Never folded — vacuity-by-activation is
/// discharge-contract content, not calculator normalization.
/// </summary>
public sealed record CanonImplies(CanonExpr Antecedent, CanonExpr Consequent) : CanonExpr;

/// <summary>Conditional (if/then/else) expression.</summary>
public sealed record CanonConditional(CanonExpr Condition, CanonExpr Then, CanonExpr Else) : CanonExpr;

/// <summary>Member access / accessor method call, keyed by catalog accessor name.</summary>
public sealed record CanonMember(CanonExpr Receiver, string Accessor, ImmutableArray<CanonExpr> Arguments) : CanonExpr;

/// <summary>Function call, keyed by resolved catalog function.</summary>
public sealed record CanonCall(string Function, ImmutableArray<CanonExpr> Arguments) : CanonExpr;

/// <summary>Bounded quantifier; the binding is alpha-renamed (CanonBinding) inside the predicate.</summary>
public sealed record CanonQuantifier(CanonExpr Collection, CanonExpr Predicate) : CanonExpr;

/// <summary>
/// Typed constant ('2026-01-01', '100 USD', …), keyed by parsed value when the
/// pipeline produced one, else by raw text. Distinct raw spellings of the same
/// constant value are only equal when the pipeline parses them to the same value.
/// </summary>
public sealed record CanonTypedConst(string TypeTag, string ValueKey) : CanonExpr;

/// <summary>List literal — element order preserved (lists are ordered).</summary>
public sealed record CanonList(ImmutableArray<CanonExpr> Elements) : CanonExpr;

/// <summary>Interpolated string — literal and hole segments in order.</summary>
public sealed record CanonInterp(ImmutableArray<CanonExpr> Segments) : CanonExpr;

/// <summary>
/// Residue of a TypedErrorExpression. All error residues share one key, so any
/// two error-bearing expressions compare "equal" — callers must treat
/// comparisons involving errors as meaningless, not as matches.
/// </summary>
public sealed record CanonError : CanonExpr;
