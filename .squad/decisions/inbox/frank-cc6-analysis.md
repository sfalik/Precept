# CC#6 Analysis — FaultSiteLink to FaultSiteDescriptor Transformation

**By:** Frank  
**Date:** 2026-05-06  
**Status:** Recommendation for Shane decision  
**Blocks:** `docs/compiler/proof-engine.md §2`, `docs/runtime/precept-builder.md`, evaluator fault routing

---

## 1. What the Proof Engine Actually Proves

The proof engine produces a `ProofLedger` with two dispositions:

- **Proved** — obligation discharged by one of four bounded strategies (Literal, Modifier, GuardInPath, FlowNarrowing)
- **Unresolved** — no strategy succeeded; a diagnostic is emitted AND a `FaultSiteLink` is produced

**Critical insight:** `FaultSiteLink` is produced *only* for `Unresolved` obligations. The proof engine's contract (§10 Fault Chain Integrity) states:

> Every `FaultSiteLink` has a 1:1 correspondence with an `Unresolved` obligation and a `FaultCode` that carries `[StaticallyPreventable(DiagnosticCode)]`.

This means: **there is no such thing as a "proven-safe fault site" in Precept's model.** A `FaultSiteLink` exists precisely because the obligation *could not* be proven safe. If the proof succeeds, no link is created, no descriptor is planted, no backstop exists at runtime.

The proof engine does NOT produce "this site is safe, skip checking" annotations — it simply doesn't create fault links for proved sites.

---

## 2. What the Backstop Is Actually For

From `proof-engine.md` §11 Decision 2 and `precept-builder.md` §Pass 6:

> `FaultSiteDescriptor` backstops are defense-in-depth — they should **never fire** if proof is correct. They exist for belt-and-suspenders safety, not as a proof continuation mechanism.

And:

> Fault backstops are defense-in-depth. A correctly-proven program never reaches them at runtime.

The backstop catches **compiler bugs and edge cases**, not user-provided data faults. The proof engine's four-strategy set covers the DSL's constrained expression surface. If the type checker emits no errors, the evaluator should never fault (§ Fault–diagnostic correspondence guarantee).

**Therefore:** The backstop is strictly for "the proof said this was safe but something went wrong" — it's a *compiler integrity check*, not a user-data validation path. Constraint violations (which *do* fire at runtime from user data) are handled by the constraint evaluation system, not fault backstops.

---

## 3. Research Findings on Proof-Elision

### SPARK Ada / GNATprove

SPARK proves Absence of Runtime Errors (AoRTE). When GNATprove proves a check, the **GNAT compiler strips the runtime check entirely from the generated code** — no annotation, no skip flag, the code simply isn't emitted. This is safe because:
- SPARK is a strict subset of Ada with no escape hatches in the verified subset
- The proof is sound (backed by CVC5/Z3 with bounded resource limits)
- Unproved checks remain as Ada runtime checks

**Model:** Binary — either prove and strip, or leave the runtime check in place.

### Rust Borrow Checker

Rust's model is even simpler: the borrow checker is a **gate, not an annotation**. Safety is implicit in successful compilation. There are no runtime borrow checks to elide — the type system *prevents* the unsafe code from compiling at all. No concept of "proven safe but still checked at runtime."

### Dafny

Dafny verifies at compile time and **compiles verified programs to target languages (C#, Java, etc.) without runtime assertion checks**. The verification IS the check — no residue crosses into the compiled output. Contrast with `assert` in C which is a runtime check.

### D Language Contracts

D's `in`/`out`/`invariant` contracts are runtime-checked by default but can be compiled out with `-release` flag. This is a *build configuration* choice, not a per-site proof-driven decision. No selective elision based on proof.

### Summary of External Models

| System | Model | Granularity |
|--------|-------|-------------|
| SPARK Ada | Proved → strip check entirely | Per-check |
| Rust | Gate (no runtime check exists to strip) | Compilation unit |
| Dafny | All verified → no runtime residue | Whole program |
| D contracts | Build flag removes all contracts | Build configuration |

**Key pattern:** Systems that do per-site proof-based elision (SPARK) have a *general solver* that can fail on individual checks. Systems with bounded/structural proof (Rust, Precept) use the gate model — you either pass or you don't.

---

## 4. Recommended Approach

### Should proven-safe fault sites elide their runtime backstop?

**This question is already answered by the architecture.** Precept already uses the gate model:

- If proof succeeds → no `FaultSiteLink` → no `FaultSiteDescriptor` → nothing to check at runtime
- If proof fails → `FaultSiteLink` → `FaultSiteDescriptor` planted → backstop fires if reached

There is no "proven-safe site with a runtime check that could be skipped" — that state doesn't exist. The external research confirms Precept's architecture is already doing the right thing for a bounded-strategy proof system.

**Shane's question about "turning OFF runtime fault checking at proven-safe sites" is already what happens.** The proof engine doesn't produce links for proved obligations, so no backstop is planted. The "elision" is structural absence, not a skip flag.

### Which option is right for the unresolved-obligation sites?

**Recommendation: Option A — Opcode annotation**, with the following rationale:

1. **No proof-elision flag needed.** Since backstops only exist for unresolved obligations, every planted descriptor is active. No conditional evaluation logic.

2. **Structural binding.** The builder knows exactly which opcode it's compiling when it encounters an unresolved obligation's expression site. It can stamp the annotation directly — no span-to-opcode lookup table needed.

3. **Zero runtime overhead for proven programs.** A fully-proven precept has zero `FaultSiteDescriptor` entries and zero annotation checks. This is the SPARK model realized through structural absence.

4. **Option B's span-to-opcode map is unnecessary.** The builder compiles expressions to opcodes sequentially. When it processes an expression that has an unresolved obligation, it knows the current opcode offset. No reverse lookup needed.

5. **Option C's inline guard opcodes add runtime overhead to every execution path.** Even for proven programs, `FAULT_CHECK` opcodes would need to be no-ops (or stripped, which is Option A with extra steps).

### Why not a new Option D?

Option A already embodies the correct design when you account for the proof-gate model. No new option is needed. The annotation is present only at unresolved sites, making it inherently proof-elided by construction.

---

## 5. Concrete C# Shape

```csharp
/// <summary>
/// Annotation on an opcode that identifies a defense-in-depth fault backstop.
/// Present ONLY on opcodes compiled from expressions with Unresolved proof obligations.
/// Proved obligations produce no annotation — elision is structural absence.
/// </summary>
public sealed record FaultSiteAnnotation(
    FaultCode Code,                    // Runtime fault to fire if reached
    DiagnosticCode PreventedBy,        // The authoring-time diagnostic that would prevent this
    SourceSpan Site                    // Source location for diagnostics/logging
);

/// <summary>
/// An opcode in an ExecutionPlan. Annotations are nullable — null means proven safe.
/// </summary>
public sealed record Opcode(
    OpcodeKind Kind,
    // ... operands per kind ...
    FaultSiteAnnotation? FaultSite     // Non-null only for unresolved-obligation sites
);
```

**Builder planting pseudocode:**

```csharp
// During expression compilation in the builder:
Opcode CompileExpression(TypedExpression expr, ProofLedger ledger)
{
    var opcode = EmitOpcode(expr);
    
    // Check if this expression site has an unresolved obligation
    var link = ledger.FaultSiteLinks
        .FirstOrDefault(l => l.Obligation.Site == expr);
    
    if (link is not null)
    {
        opcode = opcode with {
            FaultSite = new FaultSiteAnnotation(
                link.FaultCode,
                link.DiagnosticCode,
                link.Site)
        };
    }
    
    return opcode;
}
```

**Evaluator consumption pseudocode:**

```csharp
// During opcode execution:
PreceptValue Execute(Opcode op, EvaluationContext ctx)
{
    var result = Dispatch(op, ctx);
    
    // Defense-in-depth: if this opcode has a fault annotation and the
    // operation actually faulted, produce structured Fault
    if (op.FaultSite is { } annotation && IsFaultCondition(result, op))
    {
        return Fail(annotation.Code, ctx.BuildFaultArgs(annotation));
    }
    
    return result;
}
```

---

## 6. Resolution of the Open Question

The proof-engine.md Open Question at line 226:

> `FaultSiteLink.Site` is still only a `SourceSpan`, while runtime backstops ultimately need a structural binding such as an `ExecutionRow`, constraint descriptor, or opcode offset.

**Answer:** The builder resolves this during compilation. When the builder visits a `TypedExpression` to emit an opcode, it matches against `ProofLedger.FaultSiteLinks` by obligation site identity. The structural binding is the opcode itself — the annotation lives on the opcode, not on a separate lookup structure.

The `FaultSiteLink.Site` (SourceSpan) is used for diagnostic reporting in the annotation. The structural binding is `ProofObligation.Site` (TypedExpression) matched against the expression being compiled.

---

## Summary for Shane

| Question | Answer |
|----------|--------|
| Should proven-safe sites elide backstops? | Already happens — no link, no descriptor, no check |
| Option A, B, or C? | **Option A** — opcode annotation |
| Proof-elision flag needed? | No — structural absence IS the elision |
| Key external precedent | SPARK strips proven checks; Precept's gate model achieves same result architecturally |
| Data structure | `FaultSiteAnnotation?` nullable field on `Opcode` |
| Open Question resolution | Builder matches by `TypedExpression` identity during compilation |

---

## Caveats

1. **`ProofObligation.Site` identity** — The related Open Question at proof-engine.md line 207 about structural identity of `ProofObligation.Site` feeds into this. If `TypedExpression` reference equality isn't stable across pipeline stages, we may need a synthetic obligation ID. Recommend resolving that OQ concurrently.

2. **Constraint backstops vs expression backstops** — The `FaultSiteDescriptor` planting mechanism note (precept-builder.md line 479) mentions placement on `ExecutionRow` or `ConstraintDescriptor`. The opcode annotation handles expression-site faults; constraint-level faults (if any exist at the backstop level rather than the constraint evaluation level) may need a parallel annotation on `ConstraintDescriptor`. Current architecture suggests all constraint evaluation is runtime-dynamic (not backstop territory), so this may be moot.

3. **Flat array alternative** — `Precept.FaultBackstops` as a flat array (the third option mentioned in precept-builder.md) would work for diagnostic enumeration ("what backstops does this precept carry?") but is wrong for execution routing. The annotation-on-opcode model is the right execution contract. A materialized flat array could be derived for tooling/inspection without affecting the execution path.
