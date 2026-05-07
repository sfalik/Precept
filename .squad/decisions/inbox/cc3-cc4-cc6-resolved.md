# CC#3, CC#4, CC#6 — Resolved (2026-05-06)

**Decision maker:** Shane  
**Coordinator:** main session  

---

## CC#3 — SemanticIndex Reference-Tracking Collections

**Ruling:** Option A — typed reference arrays on `SemanticIndex`.

Three per-category arrays added:
- `ImmutableArray<FieldReference> FieldReferences`
- `ImmutableArray<StateReference> StateReferences`
- `ImmutableArray<EventReference> EventReferences`

Reference types: `FieldReference(TypedField Field, SourceSpan Site)`, `StateReference(TypedState State, SourceSpan Site)`, `EventReference(TypedEvent Event, SourceSpan Site)`. No general heterogeneous `References` array needed.

**Files updated:** `docs/compiler/type-checker.md §7.1`, `src/Precept/Pipeline/SemanticIndex.cs`, `docs/working/cross-cutting-decisions.md`

---

## CC#4 — Compilation.Tokens Field

**Ruling:** Already resolved by code stub. `Compilation` carries `TokenStream Tokens` (first field). `TokenStream` wraps `ImmutableArray<Token>` + lex diagnostics.

**Files updated:** `docs/working/cross-cutting-decisions.md` (status only)

---

## CC#6 — FaultSiteLink to FaultSiteDescriptor Transformation

**Ruling:** Option A — nullable `FaultSiteAnnotation?` on each opcode.

Key insight: `FaultSiteLink` is only produced for `Unresolved` obligations. Unresolved = compile error. Proof elision is structural absence — proved sites have no annotation, no check, zero runtime overhead. Matches SPARK Ada model.

```csharp
public sealed record FaultSiteAnnotation(
    FaultCode Code,
    DiagnosticCode PreventedBy,
    SourceSpan Site
);
// On Opcode: FaultSiteAnnotation? FaultSite  (null = proven safe)
```

Builder matches `ProofObligation.Site` (TypedExpression) → stamps annotation on resulting opcode. Evaluator checks `op.FaultSite` after dispatch; null = no check.

**Files to update (Frank's work item):** `docs/compiler/proof-engine.md §2 Output Shape`, `docs/runtime/precept-builder.md §Pass 6`, `docs/runtime/evaluator.md §7 fault dispatch`
