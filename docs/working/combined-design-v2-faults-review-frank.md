# Faults vs Diagnostics in the v2 Combined Design — Frank Review

## Overall thesis

Precept's promise is not merely that runtime faults are *handled*; it is that a successfully constructed `Precept` should not expose in-domain runtime errors at all. The v2 design should therefore treat **Diagnostic** and **Fault** as related but non-symmetric concepts:

- A **Diagnostic** is an authoring-time finding produced by the compiler pipeline against source.
- A **Fault** is a lowered, defense-in-depth classification for an **impossible-path engine breach** that should be unreachable in any valid executable model.

The key discipline is: **every `FaultCode` must correspond to a compiler-owned prevention obligation, but not every runtime outcome has or should have a `FaultCode`.** Normal runtime behavior stays in outcome unions (`Rejected`, constraint-failed, invalid input, access denied, unmatched, etc.), not in faults.

---

## Recommended boundary model

### 1. What a Fault is vs what a Diagnostic is

#### Diagnostic

A diagnostic is:

- emitted during lex / parse / type / graph / proof
- attributed to authored source
- part of `CompilationResult`
- something the author is expected to act on

It answers: **"what is wrong, incomplete, or unprovable in this `.precept` definition?"**

#### Fault

A fault is:

- not an authoring surface
- not a business-rule outcome
- not part of ordinary domain control flow
- the runtime classification of an invariant breach that should already have been made unreachable by compilation + lowering

It answers: **"what impossible evaluator path was reached anyway?"**

That makes fault closer to a cataloged assertion/backstop than to a normal error result.

### 2. Should every runtime fault have a compile-time diagnostic counterpart?

Yes — **every `FaultCode` should have a compile-time diagnostic counterpart in the prevention sense**.

That counterpart is not "the same thing, surfaced twice." It is:

- a catalog-level linkage saying the compiler owns the obligation that should make this runtime path unreachable
- a structural prevention chain used by proof/lowering/evaluator infrastructure

So the rule is:

> For every fault category the runtime can classify, there must exist a compiler diagnostic rule whose successful enforcement would prevent that fault site from entering a valid executable model.

But the inverse is false:

- many diagnostics have no runtime fault counterpart because they are purely authoring-time problems
- many runtime outcomes have no diagnostic counterpart because they are ordinary domain results or caller-input validation, not impossible-path breaches

### 3. Which faults are "compiler bug / impossible in a valid Precept" vs "user-facing rejected outcome"?

The clean line is:

- **All faults are impossible-path engine breaches.**
- **No fault is a user-facing rejected outcome.**

Within faults there are two subfamilies:

1. **Semantic/lowering corruption faults**  
   These indicate the executable model contains something that a valid compilation should never have lowered:
   - `TypeMismatch`
   - `UndeclaredField`
   - `InvalidMemberAccess`
   - `FunctionArityMismatch`

   These read primarily as compiler/lowering/runtime bugs.

2. **Proof-residue backstop faults**  
   These correspond to sites whose safety was supposed to be statically guaranteed, but which still need a runtime backstop:
   - `DivisionByZero`
   - `SqrtOfNegative`
   - `UnexpectedNull`
   - `FunctionArgConstraintViolation`
   - `CollectionEmptyOnAccess`
   - `CollectionEmptyOnMutation`
   - possibly qualifier/range/overflow-style faults if retained

   These read as "proof/prevention failed to close a site that should have been closed."

Neither family is a domain rejection surface.

### 4. Where user-facing rejected outcomes belong

These remain runtime outcomes, not faults:

- `Rejected`
- `Unmatched`
- `EventConstraintsFailed`
- `UpdateConstraintsFailed`
- `RestoreConstraintsFailed`
- `AccessDenied`
- `InvalidArgs`
- `InvalidInput`
- `RestoreInvalidInput`
- `UndefinedEvent`

These are part of honest runtime behavior. They describe what the definition intentionally allows the runtime to report about business rules, operation selection, or host-supplied input.

### 5. How the fault catalog participates in the metadata-driven architecture

The fault catalog belongs in the metadata-driven system because it is still language/domain knowledge:

- it declares the closed set of impossible-path breach categories
- it provides stable identity for lowering, evaluator backstops, MCP/tooling, and test enforcement
- it carries the prevention relationship to diagnostics

But it must participate **as metadata for backstops, not as metadata for normal runtime behavior**.

The healthy architecture is:

1. catalogs declare preventable hazard categories (`Faults`) and author-facing prevention rules (`Diagnostics`)
2. proof attaches obligations to semantic sites
3. lowering keeps only runtime-relevant residue (`FaultSiteDescriptor` + linked `FaultCode`)
4. evaluator uses that residue only if an impossible path is nevertheless reached

This does **not** undermine the no-runtime-errors promise, because the promise is about the valid executable model's intended behavior. The fault catalog exists so the codebase can classify and preserve impossible-path breaches without smuggling them into ordinary domain control flow.

---

## Mapping table (faults vs diagnostics vs runtime outcomes)

| Category | Compile-time surface | Runtime surface | Meaning in a valid executable model |
|---|---|---|---|
| Lex/parse/type/graph/proof authoring defect | `Diagnostic` only | no runtime surface; `Precept` is not constructed | Authoring-time problem; never reaches runtime |
| Proof obligation not discharged | `Diagnostic` only | no runtime surface; `Precept` is not constructed | Compiler blocks executable model until preventable hazard is discharged |
| Semantic/lowering corruption (`TypeMismatch`, `UndeclaredField`, `InvalidMemberAccess`, `FunctionArityMismatch`) | linked `DiagnosticCode` counterpart exists at catalog level | `Fault` only if runtime somehow reaches corrupted site | Compiler/runtime bug or broken lowering invariant |
| Proof-residue breach (`DivisionByZero`, `SqrtOfNegative`, empty collection access/mutation, unexpected null) | linked `DiagnosticCode` counterpart exists at catalog level | `Fault` only if an allegedly prevented site is still reached | Defense-in-depth backstop; should be unreachable in valid Precept execution |
| Business prohibition or rule failure | may have no compile-time issue at all | `Rejected`, `EventConstraintsFailed`, `UpdateConstraintsFailed`, `RestoreConstraintsFailed` | Normal domain-governed outcome |
| Routing / availability result | may have no compile-time issue at all | `Unmatched`, `UndefinedEvent` | Normal runtime selection result |
| Host input / persisted-data mismatch | descriptor/type contracts exist, but not as `.precept` authoring diagnostics for that invocation | `InvalidArgs`, `InvalidInput`, `RestoreInvalidInput`, `AccessDenied` | Normal boundary-validation outcome, not a fault |

### Important interpretation rule

The phrase "every runtime fault has a compile-time diagnostic counterpart" should mean:

- every **fault catalog entry** is paired with a compiler prevention rule

It should **not** mean:

- every runtime outcome has a diagnostic
- every runtime-reported bad result is evidence of a compiler failure
- normal operations can "error" in-domain and get wrapped as faults

---

## Concrete revision directions for the main v2 doc

### 1. Add one explicit boundary paragraph near the proof/lowering/runtime sections

Recommended substance:

> Diagnostics are author-facing compile-time findings against source. Faults are runtime backstops for impossible evaluator paths that should already have been made unreachable before `Precept.From(compilation)` succeeds. Normal runtime behavior — rejection, constraint failure, unmatched routing, invalid caller input, access denial, restore mismatch — is represented by outcome unions, not by faults.

This should appear around:

- §5.6 Proof engine
- §5.8 Lowering boundary
- §5.9 Evaluator

### 2. Tighten the proof section's description of the fault/diagnostic link

Current v2 wording correctly says "`FaultCode` ↔ `DiagnosticCode` linkage remains catalog-owned," but it should add the missing qualifier:

- the linkage is a **prevention/backstop relationship**
- not a claim that faults are part of ordinary runtime semantics

### 3. Tighten the lowering section

The lowering section should say that runtime receives:

- lowered constraint plans for normal domain enforcement
- lowered fault-site residue only for impossible-path backstops

That keeps "constraint enforcement" and "fault backstops" visibly separate.

### 4. Tighten the evaluator section

The evaluator section should explicitly say:

- outcome unions are the full runtime contract for expected domain behavior
- `Fault` is only for invariant breach / impossible-path classification

Current wording is close; it should be made unmistakable.

### 5. Revise the outcome table labels

In §8, keep the final column, but rename it from language like:

- "impossible-path engine failure"

to something less likely to be read as ordinary runtime failure, such as:

- **engine invariant breach (`Fault`)**
- **impossible-path backstop (`Fault`)**

That preserves honesty without implying that normal operations routinely fail internally.

### 6. Add one sentence stating the non-symmetry explicitly

The main v2 doc should say:

> Every `FaultCode` has a compiler-owned diagnostic counterpart in the prevention sense, but many diagnostics have no runtime fault counterpart, and many runtime outcomes are intentionally modeled as normal results rather than faults.

That sentence closes the most likely reader confusion.

### 7. Terminology changes recommended in and around the v2 doc

Prefer:

- **authoring-time diagnostic**
- **runtime outcome**
- **domain rejection**
- **boundary-validation outcome**
- **fault backstop**
- **engine invariant breach**
- **impossible-path site**

Avoid or qualify:

- **runtime failure mode**
- **runtime mirror of diagnostics**
- **fatal runtime error**
- **evaluator failure** (unless clearly restricted to impossible paths)

The dangerous implication to avoid is that valid in-domain operations can normally "crash" and that faults are just another runtime branch. They are not.

---

## Bottom line

The v2 design should present diagnostics and faults as a **prevention/backstop pair**, not as mirrored user-facing error systems. Diagnostics belong to authored source and block executable-model construction; faults belong to impossible-path residue and exist only to classify invariant breaches if the architecture is violated. Everything the domain legitimately rejects at runtime stays in the outcome unions.
