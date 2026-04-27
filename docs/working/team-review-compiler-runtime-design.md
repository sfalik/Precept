# Team Review: `docs/compiler-and-runtime-design.md`

**Reviewers:** Frank (Lead/Architect), George (Runtime Dev), J. Peterman (DevRel)  
**Date:** 2026-04-27  
**Status:** Findings captured — not yet actioned

Frank and George both independently confirmed two of the same findings (the `Semantics` naming mismatch and the `ConstraintActivation` / `ConstraintKind` gap), which strengthens those. The runtime sections are in excellent shape overall — George confirmed 11 of 13 checks as fully accurate. The biggest issues are in the spec's claims about not-yet-implemented shapes, two internal contradictions, and one invented verb.

---

## I. Alignment

### Critical — Doc contradicts code or makes unreachable claims

**[A-1] §9 prose: "wiring merges diagnostics correctly" — unreachable**

> *"The wiring exists and merges diagnostics correctly, but four of the five stages are still hollow."*

The diagnostic merge path is structurally sound, but `Parser.Parse`, `TypeChecker.Check`, `GraphAnalyzer.Analyze`, and `ProofEngine.Prove` all throw `NotImplementedException`. Calling `Compiler.Compile` on any input crashes at `Parser.Parse` — the merge path is never reached. The sentence should read something like: *"the wiring is structurally correct but unreachable today — the first unimplemented stage crashes the call."* Appendix A correctly marks Parser as "stub," which makes the §9 prose an internal contradiction within the document.

---

**[A-2] §11 taxonomy: `AccessDenied` assigned to wrong category**

The taxonomy lists `AccessDenied` under **"Boundary validation"** alongside `InvalidArgs`, `InvalidInput`, and `RestoreInvalidInput`. The commit-outcomes table correctly places `AccessDenied` under the **"Domain outcome"** column for `Update`. These contradict each other within the same section. The code confirms the table is right: `AccessDenied(string FieldName, FieldAccessMode ActualMode)` in `UpdateOutcome.cs` carries `FieldAccessMode` — it's a field-access rule enforcement, a business-domain outcome, not a boundary validation. Fix the taxonomy.

---

**[A-3] §11: `ConstraintViolation` described with a rich shape that doesn't exist**

> *"the outcome carries... the failing constraint descriptor, the expression text, the evaluated field values at the point of failure (`{ field: value }` pairs), the guard context that scoped the constraint (if guarded), and the specific sub-expression that failed."*

The actual `ConstraintViolation` in `SharedTypes.cs` carries only `ConstraintDescriptor Constraint` and `IReadOnlyList<string> FieldNames`. No evaluated values, no guard context, no sub-expression. A `// TODO: Stub — full shape pending R6` comment makes clear this is intentional future work. The doc should mark this as designed-but-not-yet-implemented, consistent with how it handles other stubs. A contributor implementing the evaluator would build the wrong type.

---

### Significant — Contributor would write wrong code

**[A-4] §9 table: `Semantic` vs `Semantics` — one-character compile error trap**

The §9 prose summary table writes `SemanticIndex Semantic`. The actual `Compilation.cs` property is `Semantics` (plural). The §3 code snippet is correct — it uses `Semantics:` in named-argument form and matches `Compiler.cs` exactly. The discrepancy is isolated to the §9 table. A contributor referencing only that table would write `compilation.Semantic` and get a compile error.

---

**[A-5] `ConstraintActivation` used throughout — type doesn't exist**
*(Frank and George, confirmed independently)*

The doc uses `ConstraintActivation` as a distinct concept in multiple sections (§3 line 214, §8, §10) — described as *"the discriminant distinguishing whether a constraint binds to the current state, the source state, or the target state of a transition."* There is no enum, record, or type by that name in the source. The actual `ConstraintDescriptor` carries `ConstraintKind Kind` (values: `Rule`, `StateEnsureIn`, `StateEnsureTo`, `StateEnsureFrom`, `EventEnsure`), which covers the same semantic ground under a different name.

§10 acknowledges *"ConstraintActivation should be cataloged"* — but doesn't state that the current `ConstraintDescriptor` uses `ConstraintKind` as a provisional stand-in, nor does Appendix A call out the naming discrepancy. This is the **most significant gap for a contributor working on the Precept Builder or evaluator**. Fix: add a sentence clarifying that `ConstraintActivation` is the aspirational cataloged form, and that the current source uses `ConstraintKind` on `ConstraintDescriptor` in its place.

---

### Notable — Present-tense description of unimplemented shapes

**[A-6] §3 / §10: Five of six descriptor types don't exist yet**

The doc lists `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor`, and `FaultSiteDescriptor` as runtime descriptor types in present tense. Only `ConstraintDescriptor` exists in `SharedTypes.cs`. Appendix A action item 1 correctly identifies "Define concrete descriptor types" as future work, but §3 and §10 present them as current reality. The text needs a status hedge: *"these are the designed shapes; currently only `ConstraintDescriptor` is defined — see Appendix A."*

---

**[A-7] §6: `reset` is an invented action verb**

> *"Verbs like `clear`, `reset`..."*

The `ActionKind` enum includes `Clear` but has no `Reset`. `Reset` doesn't exist in the language. Remove it.

---

**[A-8] §12: "No interfaces and no abstract classes" is wrong**

> *"There are no interfaces and no abstract classes: each type has exactly one implementation."*

`EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, `RowEffect`, `ProofSubject`, `ProofRequirement`, `OperationMeta`, `ModifierMeta`, and `TypeAccessor` are all `abstract record` types with multiple sealed subtypes. Qualify the statement: *"no standalone abstract classes or interfaces serving as abstraction boundaries — abstract records serve only as discriminated union bases."*

---

**[A-9] §4: `SourceSpan` presented as a member name, not a type name**

> *"each: `TokenKind`, `Text`, `SourceSpan`"*

`Token` in `Token.cs` has properties `Kind`, `Text`, and `Span` — where `SourceSpan` is the *type* of `Span`, not the property name. A contributor would write `token.SourceSpan` instead of `token.Span`.

---

## II. Approach

*(J. Peterman)*

**[P-1] §12 is misplaced — document's own "How to read" guide is wrong about it**

> *"Sections 11–15 cover the runtime surface, tooling integration..."*

But §12 is **"Type and immutability strategy"** — a cross-cutting architectural section, not a runtime tooling surface. It belongs after §2 or §9 where architectural principles are established. The preamble's navigation is inaccurate.

---

**[P-2] The artifact inventory table interrupts the §3 pipeline story at its best moment**

The §3 pipeline walkthrough builds momentum from source text through `Compiler.Compile` to the `Precept.From()` boundary — then pauses for the artifact inventory table just as the reader expects the story to continue into `Create/Fire/Version`. The table is valuable but belongs in §9 or §10. Let §3 stay a continuous narrative.

---

**[P-3] Snippets teach API use, not architecture**

The `Create`/`Fire` switch statements in §3 teach call patterns, not the architecture. A contributor working on compiler internals doesn't need to see the calling convention — they need to understand *why the pipeline flows the way it does*. Add a sentence bridge before each snippet explaining what the reader should specifically notice, or replace with a smaller architectural note about immutable progression.

---

**[P-4/P-5] Recompile rationale and severance story each repeated across sections**

"Full recompile on every change" is stated twice in §9 then restated in §12. Let §12 own the rationale; §9 should carry a one-sentence summary with a reference.

Similarly, §3 and §10 both state the compile-to-runtime severance story. §3 should state it briefly and send the reader to §10 for the full contract.

---

**[P-6] The "guarantee" framing drifts in §§13–15 into product-positioning language**

§1 establishes: *"Everything in this document... exists to deliver that guarantee."* But §§13–15 drift toward *"AI-first design concern," "primary distribution surfaces," "No other MCP tool in any category provides this depth of preview."* The tooling sections should explain how each surface preserves or exposes the guarantee for contributors building it — not how it differentiates Precept in the market.

---

**[P-7] Too many Innovations callouts; several read as product marketing**

Innovations callouts appear in nearly every section. Several contain language better suited to a README:
- *"No other MCP tool in any category provides this depth of preview."*
- *"No other DSL tooling in this category has this level of surface coherence."*

For an internal contributor doc, the question is "why did we design it this way, and what would go wrong if you designed it differently?" — not "is this novel vs. competitors?" Consider reframing callouts as **Design rationale** notes with that orientation.

---

**[P-8] §11 is materially longer and denser than all surrounding sections**

§11 carries the full runtime surface contract, three-tier constraint model, inspection API, outcome taxonomy, and significant precedent-survey prose. The survey material dilutes the internal contract. Consider trimming survey comparisons in §11 and replacing with a concise operation lifecycle diagram or table showing: `Version.Fire() → Evaluator.Fire() → outcome → new Version`.

---

## III. Innovation Opportunities

*(Frank)*

### High value — not documented at all

**[I-1] §8: Catalog-declared proof obligations**

The `ProofRequirement` discriminated union (five sealed subtypes: `NumericProofRequirement`, `PresenceProofRequirement`, `DimensionProofRequirement`, `QualifierCompatibilityProofRequirement`, `ModifierRequirement`) is genuinely novel. Operations, functions, accessors, and actions declare their safety requirements as catalog metadata. The proof engine reads these — it maintains no hardcoded obligation lists. Adding a new operation with a division-by-zero hazard requires only attaching a `NumericProofRequirement` to its catalog entry; the proof engine automatically generates and discharges the obligation at every call site. **No surveyed DSL-scale system externalizes proof obligations as structured catalog metadata.** Deserves a callout in §8.

---

**[I-2] §8: Roslyn-enforced `FaultCode` ↔ `DiagnosticCode` correspondence**

Every `FaultCode` carries a `[StaticallyPreventable(DiagnosticCode)]` attribute. Roslyn analyzers (PRECEPT0001, PRECEPT0002) enforce that every evaluator failure path routes through a classified `FaultCode`, and every `FaultCode` links to its prevention `DiagnosticCode`. The C# compiler refuses to build if the chain is broken. This makes fault–diagnostic correspondence a **build-time invariant**, not a convention. No surveyed system enforces this property at the host-language compiler level. The document says *"prevention, not detection"* — this is the actual mechanism that makes that claim auditable. Belongs in §8.

---

### Medium value — partially mentioned but significance not drawn out

**[I-3]** `BinaryOperationMeta.BidirectionalLookup` — catalog-declared commutativity. `money * decimal` and `decimal * money` resolve to the same entry. Mention in §2 or §6.

**[I-4]** `TypeAccessor` DU — proof-carrying type member accessors. Accessing `.peek` on a queue requires proving non-empty, declared in the accessor's catalog metadata. The doc mentions `TypeAccessor` but never calls out the proof-obligation shape. Add to §6 or §8.

**[I-5]** `TokenMeta.ValidAfter` arrays — catalog-declared completion context. The LS filters completion candidates by checking the preceding token against predecessor sets declared in the catalog. Mentioned as "completion hints" in §4 but the filtering mechanism is never described.

**[I-6]** `TypeMeta.WidensTo` and `TypeMeta.ImpliedModifiers` — metadata-declared widening rules and implied modifiers (`money → notempty`, `integer → number`). Not mentioned. The type checker reads these from the catalog; there are no hardcoded widening chains. Add to §6.

**[I-7]** `ConstructSlot` — slot validation (required vs. optional, expected order) is catalog-declared, not per-construct parser logic. Partially mentioned but the implication is never drawn out. Clarify in §5.

**[I-8]** `ConstraintKind` discriminant — typed constraint scope identity (`Rule`, `StateEnsureIn`, `StateEnsureTo`, `StateEnsureFrom`, `EventEnsure`) means consumers pattern-match on scope, not parse strings. Worth adding to the "three-tier constraint exposure" callout in §11.

---

## Summary Table

| ID | Finding | Type | Priority |
|---|---|---|---|
| A-1 | §9 "wiring merges diagnostics" is unreachable | Contradiction | High |
| A-2 | `AccessDenied` in wrong taxonomy category | Contradiction | High |
| A-3 | `ConstraintViolation` shape described with non-existent fields | Misdescription | High |
| A-4 | `Semantic` vs `Semantics` in §9 table | Misdescription | Medium |
| A-5 | `ConstraintActivation` type doesn't exist; code uses `ConstraintKind` | Omission | Medium |
| A-6 | Five descriptor types presented as current; only one exists | Omission | Medium |
| A-7 | `reset` action verb invented | Contradiction | Low |
| A-8 | "No abstract classes" claim wrong | Misdescription | Low |
| A-9 | `SourceSpan` is a type name, not a member name | Misdescription | Low |
| P-1 | §12 misplaced per preamble's own navigation map | Structure | Medium |
| P-2 | Artifact inventory interrupts §3 narrative | Narrative | Medium |
| P-3 | §3 snippets teach API use, not architecture | Audience-fit | Low |
| P-4/P-5 | Recompile rationale + severance story each repeated | Redundancy | Low |
| P-6 | Guarantee framing drifts to marketing in §§13–15 | Framing | Medium |
| P-7 | Innovations callouts slip into marketing language | Audience-fit | Medium |
| P-8 | §11 too dense; survey prose dilutes internal contract | Structure | Low |
| I-1 | Catalog-declared proof obligations not documented | Innovation | High value |
| I-2 | Roslyn-enforced fault/diagnostic chain not documented | Innovation | High value |
| I-3 | Catalog-declared commutativity (`BidirectionalLookup`) | Innovation | Medium value |
| I-4 | Proof-carrying `TypeAccessor` shape not called out | Innovation | Medium value |
| I-5 | `TokenMeta.ValidAfter` completion filtering not described | Innovation | Medium value |
| I-6 | `TypeMeta.WidensTo` / `ImpliedModifiers` not mentioned | Innovation | Medium value |
| I-7 | `ConstructSlot` catalog-declared slot validation | Innovation | Medium value |
| I-8 | `ConstraintKind` typed scope identity not in callout | Innovation | Medium value |
