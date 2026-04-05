# Expression Compactness

**Research date:** 2026-04-04  
**Author:** George (Runtime Dev)  
**Relevance:** Reducing statement count without losing expressiveness

---

## Formal Concept

PLT calls this **syntactic sugar** and **derived forms** — language constructs that are translatable to more primitive forms (desugaring) without introducing new semantics. The canonical treatment is in TAPL (Pierce): a derived form is any syntactic construct defined by a transformation into the core language. The core stays minimal and provable; the surface stays readable.

Three distinct mechanisms are relevant for Precept:

| Mechanism | What It Does | PLT Name |
|-----------|-------------|----------|
| Syntactic sugar | New notation that desugars to existing core forms | Derived form / macro expansion |
| Default inference | Compiler infers absent clauses from context | Defaulting / implicit elaboration |
| Implicit conjunction | Multiple statementsmerge into one logical check | Constraint lifting |

---

## Examples from Well-Designed Languages

### 1. Kotlin `data class` — Default Inference

```kotlin
data class Point(val x: Int, val y: Int)
```

The compiler derives `equals`, `hashCode`, `copy`, `toString`, `componentN`. None are stated; all are inferred from the declared structure. The user writes the essential, the language fills in the consequential.

**Design principle:** "If there is only one sensible thing to do, do it without being asked." Kotlin's data class is a closed-world assumption: the compiler knows all fields and applies a fixed policy.

### 2. Alloy — Implicit Fact Conjunction

```alloy
pred valid_order[o: Order] {
  o.total > 0
  o.items.count > 0     // implicit && between predicates
  o.paymentToken != none
}
```

Multiple predicate lines are implicitly conjoined — no `&&` needed. This is constraint lifting: each line is a separate assertion; the conjunction is the compiler's business, not the author's. When one fails, the error names the specific failing line, not the whole blob.

**Design principle:** Separate expression of individual constraints from their evaluation strategy (collect-all conjunction).

---

## How This Applies to Precept

### Current state

Precept already uses derived forms:
- `state Open, InProgress, Blocked` → desugars to three separate `StateDecl`s
- `in any assert ...` → desugars to N `StateAssert`s (one per declared state)  
- `event Approve, Reject with Note as string` → desugars to two `EventDecl`s with identical args

These are Precept's existing syntactic sugar inventory.

### Potential expansions

**1. Multi-field invariants** (default inference pattern)

Current:
```precept
invariant MinAmount >= 0 because "Min cannot be negative"
invariant MaxAmount >= 0 because "Max cannot be negative"
invariant MaxAmount >= MinAmount because "Max must exceed min"
```

Derived form opportunity: when fields are declared with a shared `default 0`, the "non-negative" invariant could be a one-liner opt-in, but this requires default inference which risks hiding contract intent. **Verdict: low value, high risk of opacity — skip.**

**2. Implicit `from any assert` for universally-required conditions**

Current: no shorthand for "must hold in all states *and* on all exits."  
`in any` and `from any` are separate statements. In practice, when a team writes:
```precept
in any assert AuditLog.count > 0 because "..."
from any assert AuditLog.count > 0 because "..."
```
...they almost certainly want the same property with different temporal anchors. A `global assert` form (desugars to both) could reduce ceremony. **Feasibility: low-medium parser change; semantic risk: subsumption rule already catches `in` + `to` on the same state, needs extension.**

**3. Transition shorthand for terminal states**

Repeated pattern across samples:
```precept
from Approved on FundLoan -> transition Funded
from Denied on Archive -> transition Archived
```

If a state has only `no transition` or direct single-target exits, the pattern is mechanical. Not currently worth sugar — rows are intentionally self-contained and verbose for AI readability.

### Desugaring boundary constraint

**Critical rule for Precept:** Any derived form must desugar *before* the type-checker runs, so the type-checker, analyzer, and MCP tools operate on fully-expanded models. The language server must resugar errors back to the source location for diagnostics. This is the main implementation cost for new derived forms.

---

## Implementation Cost: **Low to Medium**

- Adding a derived form to the parser = low (expand before model assembly)
- Reattaching source positions for diagnostics = medium (requires span tracking through desugaring)
- Type-checker / analyzer changes = minimal if desugaring is complete

---

## Semantic Risks Specific to Precept

1. **Error attribution**: If `in any assert X` desugars to N individual asserts, which state does the diagnostic point at? The language server must resugar the error position.
2. **Subsumption detection**: The compiler checks for `in` + `to` subsumption on a per-state basis after expansion. Any new derived form that generates both must avoid generating false subsumption errors.
3. **AI readability**: Precept's design principle 12 says AI is a first-class consumer. Syntactic sugar that hides intent from the AI author defeats the purpose. Any sugar must be expressible in the expanded form the AI learns from.

---

## Key References

- Pierce, *Types and Programming Languages*, Ch. 11 (derived forms, desugaring)
- Pombrio & Krishnamurthi, "Resugaring: Lifting Languages through Syntactic Sugar" (Brown PhD 2018) — on error attribution through desugaring
- Felleisen, "On the Expressive Power of Programming Languages" — formal definition of when sugar adds expressive power vs. when it's purely cosmetic
