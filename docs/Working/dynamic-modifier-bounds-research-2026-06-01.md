---
status: Research — 2026-06-01
sources-consulted:
  - "docs/philosophy.md — core commitments; checked the prevention/determinism/totality/static-completeness guarantees and the approximation-honesty line for what a field-reference bound must preserve."
  - "docs/language/precept-language-spec.md §0.1 — the eleven Design Principles; checked 7 (compile-time-first), 9 (mandatory rationale), 10 (totality), 11 (static completeness) for the bound's compile-time obligations."
  - "docs/language/precept-language-spec.md §0.4 — Execution Model Properties; checked no-loops / no-reconverging-flow / expression-purity for whether a constraint can form an evaluation cycle."
  - "docs/language/precept-language-spec.md §0.6 — Proof Engine Design Contract; checked numeric interval reasoning, relational reasoning over multiple fields, soundness-over-completeness, and the unbounded-interval soundness gate."
  - "docs/language/precept-language-spec.md §2.4 — Field Modifiers; checked the explicit 'constraint modifier desugars to rule' statement and the form-vs-decidability claim about proof participation. Directly on point."
  - "docs/language/precept-language-spec.md §3.2 / §3.6 — widening and binary-operator typing; checked numeric lane rules for cross-type bounds."
  - "docs/language/precept-language-spec.md §3.5 — Scope Rules (recently edited); the modifier-value-expression row and the evaluation-model derivation explicitly address field-reference bounds, self-reference, and mutual reference. Directly on point."
  - "docs/language/precept-language-spec.md §3.8 — Modifier validation; checked InvalidModifierBounds / InvalidModifierValue and the modifier-value-validation table."
  - "docs/language/precept-language-spec.md §3A.1 — Constraint Semantics; checked rule scope (field-only) and collect-all enforcement."
  - "docs/language/business-domain-types.md — whole file; checked § Bounds qualification rules (bound interpretation + qualifier compatibility), D6 (locked rejection: conversion factors as typed quantities, no units block), D9 (open-field narrowing), D14 (in as assignment constraint)."
  - "docs/language/catalog-system.md §11 ProofRequirements — the IntervalContainment / Length / Count containment data shapes; checked what a bound is stored as."
  - "docs/language/primitive-types.md — constraint catalogs for integer/decimal/number/string; checked the form of every min/max example."
  - "docs/compiler/proof-engine.md — Strategy 1 (literal), Strategy 3 (guard-in-path), Strategy 4 (flow-narrowing field-to-field relational), Strategy 7 (compositional), the IntervalContainmentProofRequirement record, the normalization boundary, and the GuardRelationImpliesObligation triple table."
  - "docs/compiler/diagnostic-system.md — confirmed InvalidModifierBounds (=34) / InvalidModifierValue (=35) exist; no statement found constraining bound form to literal."
---

## Question

When a constraint modifier's value is a **field reference** rather than a literal — e.g. `field Amount as number min Floor` where `Floor` is another declared field, versus the literal form `field Amount as number min 5` — how should it behave? This research derives, from Precept's own philosophy and canonical specification only, the semantic nature of such a bound, the scope its value expression should have, the compile-time proof obligation(s) it would require, the soundness consequences under each affected principle, and the edge-case dispositions. It draws no conclusions the canon does not ground, and flags every point where the canon is silent or self-conflicting.

## What the canon says

### Philosophy (`docs/philosophy.md`)

The guarantee Precept makes about computation is unconditional and compile-time:

> "The same absoluteness applies to every calculation in the definition. The compiler does not trust that an expression will succeed — it proves it will. … If the compiler cannot prove an expression safe given the declared constraints, it rejects the definition with a specific message identifying what would make safety provable." (`philosophy.md`, "What makes it different" → "Prevention, not detection")

> "A definition that compiles without diagnostics has no unproven evaluation faults, no unreachable business process states, and no structural dead ends." (same section)

Determinism is named as a core commitment:

> "The engine is deterministic — same definition, same data, same outcome. Nothing is hidden." (`philosophy.md`, "What the product does")

### §0.1 Design Principles (`precept-language-spec.md`)

> **7. Compile-time-first static checking.** "The compiler proves what it can, rejects what it can prove invalid, and does not guess. … A precept that compiles without diagnostics has no unproven evaluation faults." (`spec §0.1`, line 103)

> **9. Mandatory rationale.** "Every constraint carries a mandatory reason. … Constraint modifiers (§2.4) are rule shorthand and carry a *generated* rationale, which satisfies this requirement without an authored clause; an author who wants a specific reason writes the constraint as a full `rule … because "…"`." (line 107)

> **10. Totality.** "Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`. … For any expression that *could* fault at runtime … the compiler must either prove safety or emit a diagnostic requiring the author to supply constraints that make safety provable." (line 109)

> **11. Static completeness.** "If a precept compiles without diagnostics, it does not fault at runtime. … Every fault class that the evaluator can produce — type mismatch, division by zero, overflow, empty collection access, constraint range impossibility — is linked to a compiler diagnostic that prevents it." (line 111)

### §0.4 Execution Model Properties

> "**No reconverging flow.** Because there are no loops or branches, there is no join point where two different states must be merged." (line 164)

> "**Expression purity.** Expressions cannot mutate entity state, trigger side effects, or observe anything outside their evaluation context (current field values and event arguments)." (line 169)

### §0.6 Proof Engine Design Contract

Two listed obligations bear directly on field-reference bounds:

> "1. **Numeric interval reasoning.** Field constraints, rules, and guards contribute provable numeric ranges. The proof system tracks these ranges through assignment chains." (line 202)

> "2. **Relational reasoning** over numeric expressions involving multiple fields." (line 203)

The soundness posture:

> "1. **Soundness over completeness.** The proof layer must never claim an expression is safe when it is not. … False negatives (missed proofs) cause author friction … False positives (wrong "safe" claims) cause runtime failures. The language always chooses the safe direction." (line 220)

> "5. **Truth-based diagnostic classification.** Proof outcomes are classified into three categories: *proved dangerous* …, *proved safe* …, and *unresolved* (the compiler cannot determine either). … Diagnostics are classified by proof outcome, not by syntax shape." (line 228)

### §2.4 Field Modifiers — the most direct canon

> "**Constraint modifiers are shorthand for rules.** A constraint modifier — `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces` — desugars to the equivalent `rule` with a generated rationale. `field Qty as number min 5` is shorthand for `field Qty as number` plus `rule Qty >= 5 because "Qty must be at least 5"`." (line 1109)

> "A constraint modifier **participates in compile-time proof identically to the equivalent rule** … `min 5` and `rule X >= 5` are interchangeable to the proof engine. **Proof participation is a function of a constraint's decidability, not its syntactic form: a literal bound is always decidable, and a relational constraint is decidable only insofar as the referenced fields' own bounds make it so — equally true whether written as a modifier or as a `rule`.**" (line 1112)

The modifier grammar table types each bound slot as an *expression*, not a literal:

> "| `min` _Expr_ | value | Minimum value |" … "| `max` _Expr_ | value | Maximum value |" (lines 1126–1132 — every value modifier is `_Expr_`)

### §3.5 Scope Rules — directly addresses field-reference bounds

The modifier-value-expression row of the Expression-scope table:

> "| Modifier value expressions (`min N`, `max N`, etc.) | All field names — a constraint modifier is rule shorthand (§2.4), so its value expression has the same scope as a rule condition: it may reference any field regardless of declaration order, including fields declared later in the precept. **Self-reference is vacuous (`min X` ⟹ `X >= X`) and mutual reference is satisfiable (`A min B` + `B min A` ⟹ `A == B`), not a cycle** — unlike a computed expression (row above), a constraint cannot form an evaluation cycle. |" (line 1326)

The evaluation-model derivation that grounds the three scopes:

> "The three expression scopes track the **evaluation model**, not preference. A *default value* is materialized in declaration order during construction, so it can only see fields already built above it — a forward reference is *impossible*, not merely disallowed. A *computed expression* is derived from the final configuration, so it sees every field except those that would form a dependency cycle: an assignment cycle has no fixed point and cannot be evaluated. A *rule condition* — and a *constraint modifier*, which is rule shorthand (§2.4) — is checked against the complete working copy *after* all mutations, so declaration order is irrelevant and any field is in scope. Because a constraint is a predicate rather than an assignment, it cannot form an evaluation cycle the way a computed expression can: mutual bounds simply conjoin (`A min B` + `B min A` ⟹ `A == B`), a satisfiable constraint, not a structural error." (line 1328)

### §3A.1 Constraint Semantics

> "Rules operate in field scope. They cannot reference event arguments — this ensures reusability across all events." (line 1838)

> "Rules and ensures are evaluated exhaustively — every applicable constraint is checked, and all violations are reported." (line 1873)

### §3.8 Modifier validation

The modifier-value-validation table treats `min > max` and negative-count checks as the only value-level checks; the firing conditions are stated in terms of *values* ("`min` value exceeds `max` value"):

> "| `min` > `max` | `min` value exceeds `max` value on the same field | `InvalidModifierBounds` |" (line 1659)
> "| Negative count/length | … is negative | `InvalidModifierValue` |" (line 1662)

No row in §3.8 states that a bound must be a literal, and none states what `InvalidModifierBounds` does when one or both bounds are field references whose ordering is not statically decidable.

### `business-domain-types.md` — bound interpretation and a locked rejection

The bound-interpretation rule enumerates exactly two bound forms — typed constants and number literals — and says nothing about field references:

> "**Bound expression interpretation.** Typed-constant bounds (`'5 kg'`, `'100 USD'`) are extracted into comparable `decimal` values for the interval-containment proof. Number-literal bounds (`5`, `-100`) are extracted unchanged. The extraction is exact; bounds never round." (line 426)

> "**Qualifier compatibility on the bound.** When the bound carries its own qualifier (typed constants always do), it must match the field's qualifier on the same axis. Mismatch is `BoundsQualifierMismatch` (PRE0134) … The exception is unit-conversion within the same UCUM dimension on `quantity` fields …" (line 428)

> "**Why a structural rule and not a runtime check.** A bound that cannot be evaluated at compile time is an unprovable governance claim. Precept's identity is structural prevention, not runtime detection — if the bound cannot participate in a proof, accepting it would silently weaken the governance guarantee on the field." (line 430)

Locked rejection bearing on the topic (no `units {}` block; conversion factors are data fields):

> "**D6. Entity-scoped conversion factors are typed compound quantities, not bare integers.** … No dedicated `units { }` block syntax. … **Alternatives rejected:** (A) Dedicated `units { }` block …" (lines 1717–1721)

There is **no `## Alternatives rejected` block and no locked Decision in `business-domain-types.md` that addresses or rejects a field-reference bound** on any type.

### `catalog-system.md` §11 / `proof-engine.md` — the bound's stored shape

The interval-containment obligation stores a bound as a nullable `decimal`, not as an expression or a field reference:

> "class IntervalContainmentProofRequirement { Subject : ProofSubject; TargetField : string; DeclaredMin/Max : decimal?; AuthoredMin/Max : decimal? }" (`catalog-system.md`, lines 622–627)

The proof-engine record confirms this and explains the fields:

> "public sealed record IntervalContainmentProofRequirement( … decimal? DeclaredMin, // UCUM base-unit normalized lower bound; null if absent / decimal? DeclaredMax, … decimal? AuthoredMin, // raw authored magnitude (for diagnostic display only) … )" (`proof-engine.md`, lines 580–588)

`LengthContainmentProofRequirement` and `CountContainmentProofRequirement` likewise store `int?` bounds (`catalog-system.md`, lines 628–637).

The relevant proof strategies:

> "**Strategy 1 (Literal):** … The expression site's subject value is a compile-time literal." (`proof-engine.md`, line 702)

> "**Strategy 4 (flow-narrowing):** The guard establishes a *relational invariant between two or more fields* (e.g., `when Quantity > ReorderPoint` establishes `Quantity > ReorderPoint`). Strategy 4 discharges obligations where the proof site involves an expression over both constrained fields and the established relation implies the obligation … Strategy 4 applies only when the guard is a *binary comparison between two non-literal operands* and the obligation is an arithmetic result-range obligation on an expression involving those operands." (line 1397)

The `GuardRelationImpliesObligation` triple table (line 1336+) shows the field-to-field relational reasoning the engine performs today is **scoped to subtraction expressions only** ("Scope: subtraction expressions only (`A - B`). Division is NOT covered", line 1330) and is **driven by `when`-guard relations**, not by modifier-declared field-to-field bounds.

### `primitive-types.md` — every bound example is a literal

> "| Field constraint value: `field Price as decimal min 0.01` → resolves as `decimal`. |" (line 449)
> "`field Priority as integer min 1 max 10`" (line 194); "`field TaxRate as decimal min 0 max 1 maxplaces 4`" (line 226); "`field Latitude as number min -90 max 90`" (line 261).

No `min`/`max` example anywhere in `primitive-types.md` uses a field reference; all are numeric literals.

## Derivations

### D1 — Semantic nature: what does a constraint modifier desugar to, and does proof participation follow from form?

The canon is explicit and unambiguous on both halves. §2.4 (line 1109) states a constraint modifier **desugars to the equivalent `rule` with a generated rationale**: `field Qty as number min 5` ≡ `field Qty as number` + `rule Qty >= 5 because "…"`. By direct substitution of the field-reference form into the same desugaring, `field Amount as number min Floor` ≡ `field Amount as number` + `rule Amount >= Floor because "<generated>"`. Nothing in the desugaring rule restricts the RHS to a literal — §2.4's modifier table types the slot as `_Expr_` (lines 1126–1132), and §3.5 (line 1326) calls it a "value expression" that "may reference any field."

On whether proof participation follows from form, §2.4 line 1112 answers directly and against a form-based reading: **"Proof participation is a function of a constraint's decidability, not its syntactic form: a literal bound is always decidable, and a relational constraint is decidable only insofar as the referenced fields' own bounds make it so — equally true whether written as a modifier or as a `rule`."** This sentence already contemplates a relational (field-referencing) bound and states the decidability test for it.

**Conclusion (canon-grounded):** A field-reference bound is semantically a relational rule (`Amount >= Floor`). Whether it delivers a compile-time proof is governed by the *decidability* of that relation given the referenced field's own declared bounds — not by the fact that the value is a field reference rather than a literal. This is a single defensible answer; the canon states it directly.

### D2 — Scope of the bound's value expression, derived from the evaluation model

§3.5 gives an evaluation-model derivation for each scope (line 1328), so the answer is derivable rather than asserted. The three evaluation models are:

- *Default value* — materialized in declaration order during construction ⟹ can only see earlier fields; forward reference is impossible.
- *Computed expression* — derived from the final configuration ⟹ sees every field except those forming a dependency cycle (an assignment cycle has no fixed point).
- *Rule condition* — checked against the complete working copy *after* all mutations ⟹ declaration order irrelevant, any field in scope.

A constraint modifier desugars to a rule (D1), and a rule is a *predicate* checked after mutation, not an *assignment* that must be materialized. Therefore the constraint-modifier value expression inherits the rule-condition evaluation model: **all field names are in scope, regardless of declaration order, including forward references.** §3.5 line 1326 states exactly this and adds the cycle analysis: because a constraint is a predicate, not an assignment, mutual bounds *conjoin* rather than forming an unsatisfiable fixpoint — `A min B` + `B min A` ⟹ `A == B`, a satisfiable constraint.

One tension to record honestly: §3A.1 line 1838 says "Rules … cannot reference event arguments." The constraint modifier desugars to a *rule*, and the §3.5 modifier-scope row lists only "All field names" — not event args. So a field-reference bound inherits the rule's field-only scope, **not** the wider transition-row scope that includes `EventName.ArgName`. The two statements are consistent (both exclude event args from rules), but a design pass should confirm the intent that a modifier bound, like its parent rule, cannot reference an event arg.

**Conclusion (canon-grounded, single defensible answer):** The bound expression has rule-condition scope — all field names, any declaration order, forward references allowed, event args excluded. This is derived from the evaluation model, and §3.5 line 1326 states it explicitly.

### D3 — Compile-time obligation, and whether the ProofRequirement data model supports it

Under Principles 7/10/11, a precept that compiles without diagnostics has no unproven faults and does not fault at runtime. The desugared rule `Amount >= Floor` becomes a constraint the proof engine must reason about — specifically the interval-containment family that §0.6 item 1 ("field constraints contribute provable numeric ranges") and item 2 ("relational reasoning over numeric expressions involving multiple fields") cover. So the *contract* in §0.6 already names the obligation kind a field-reference bound needs: relational reasoning between `Amount` and `Floor`.

The data-model evidence, however, shows the **current** obligation instance cannot carry a field-reference bound. `IntervalContainmentProofRequirement` stores `DeclaredMin/Max` and `AuthoredMin/Max` as `decimal?` (`catalog-system.md` lines 625–627; `proof-engine.md` lines 580–588). A bound is a scalar magnitude, not a reference to another field. `business-domain-types.md` line 426 confirms the only bound forms the extraction recognizes are typed constants and number literals, both reduced to `decimal`. There is no slot for "the bound is field `Floor`."

Two readings of what this implies are defensible; the canon does not pick between them:

- **Reading A — the data model is a deliberate scope boundary.** `business-domain-types.md` line 430 says "A bound that cannot be evaluated at compile time is an unprovable governance claim … if the bound cannot participate in a proof, accepting it would silently weaken the governance guarantee." A field reference is not a compile-time constant; under this reading the `decimal?` shape encodes a decision that bounds are static magnitudes, and a field-reference bound should be rejected (or routed to a longhand `rule`).
- **Reading B — the data model is an implementation snapshot, not a locked rejection.** §2.4 line 1112 explicitly contemplates "a relational constraint" as a constraint-modifier shape and assigns it a decidability test, and §3.5 line 1326 explicitly contemplates `A min B`. Under this reading the `decimal?`-only obligation is simply the *literal-bound* case that has shipped; a field-reference bound would need a *different* discharge path (the relational one in §0.6 item 2 / Strategy-4-style field-to-field reasoning), and the data model would be extended, not the feature rejected.

The proof engine already performs *some* field-to-field relational reasoning (Strategy 4 / `GuardRelationImpliesObligation`), but the canon shows it is (a) driven by `when`-guards, not by declared modifier bounds, and (b) scoped to subtraction expressions only (`proof-engine.md` line 1330). Neither restriction is stated as a principle; both read as current-implementation scope.

**Conclusion:** §0.6's *contract* names the obligation a field-reference bound needs (relational reasoning between fields). The *current* `IntervalContainmentProofRequirement` shape stores only `decimal?` and cannot represent a field-reference bound; whether that `decimal?` shape is a locked rejection (Reading A) or an unshipped-feature snapshot (Reading B) is **not resolved by the canon** — see Contradictions/gaps and Open questions.

### D4 — Soundness per principle, with the unbounded-interval case

The decidability test from §2.4 line 1112 is the hinge: a relational constraint "is decidable only insofar as the referenced fields' own bounds make it so." Apply this to each affected principle.

**Principle 10 (Totality) / 11 (Static completeness).** These require that any expression which *could* fault is either proven safe or rejected with a diagnostic. A field-reference bound `Amount min Floor` is itself a predicate (no division/overflow inside the comparison `Amount >= Floor`), so it does not directly introduce a fault site. Its soundness impact is *indirect*: it contributes (or fails to contribute) a provable interval for `Amount`, which downstream expressions may depend on for divisor-safety, sqrt-non-negativity, etc.

**The unbounded-interval case.** Suppose `Floor` has no declared bounds (inferred interval is unbounded). Then `Amount >= Floor` proves nothing decidable about `Amount`'s lower bound — `Floor` could be any value. §0.6 item 1 says field constraints "contribute provable numeric ranges"; here the range contributed is the trivial one. The governing rule is the soundness gate the proof engine already applies elsewhere:

> "if any field's resulting interval is empty **(and non-Unbounded — the soundness gate)**, the rule is impossible" (`proof-engine.md`, line 483)

and the §0.6 principle:

> "Soundness over completeness. The proof layer must never claim an expression is safe when it is not. … The language always chooses the safe direction." (line 220)

So if a downstream expression *needs* `Amount`'s lower bound to be proven and the only source is `Amount >= Floor` with `Floor` unbounded, the proof engine must **decline (unresolved → obligation diagnostic)**, not assume. This is consistent with Principle 10's "emit a diagnostic requiring the author to supply constraints that make safety provable." The MEMORY note "unprovable = reject, not skip" aligns: an unbounded bound that leaves a downstream obligation unprovable is a reject/obligation, never a silent pass.

Crucially, the *bound itself* (`Amount >= Floor`) remains a sound, enforceable runtime constraint even when `Floor` is unbounded — the runtime can always evaluate `Amount >= Floor` against concrete data (Principle 1, prevention, still holds at runtime). What an unbounded `Floor` removes is *compile-time* proof leverage, not runtime enforceability. The canon does not anywhere say a bound must be statically decidable to be a *legal* bound; it says (line 1112) decidability governs *proof participation*. These are different claims, and the distinction is the heart of the open design question (see Open questions Q1).

**Principle 3 (Determinism).** A field-reference bound is deterministic: `Amount >= Floor` is a pure expression over current field values (§0.4 expression purity, line 169). No determinism threat.

**Principle 9 (Mandatory rationale).** The generated-rationale mechanism (§0.1 line 107) supplies the reason. A field-reference bound's generated rationale would presumably read "Amount must be at least Floor" — the canon does not specify the generated wording for a non-literal bound, a small gap.

**Conclusion:** No principle is *violated* by admitting a field-reference bound *as a runtime-enforceable constraint*. The single soundness-sensitive point is compile-time proof: when the referenced field is unbounded, the bound contributes no decidable range, and Principles 10/11 require the engine to leave dependent downstream obligations unresolved (emit obligation diagnostics), never to over-prove. Whether the bound is *admitted at all* when it cannot contribute a static range is the unresolved design question, not a soundness question the canon already answers.

### D5 — Edge cases, with canon-grounded disposition

| Edge case | Canon disposition |
|---|---|
| **Self-reference** `X min X` | **Canon explicit:** "Self-reference is vacuous (`min X` ⟹ `X >= X`)" (§3.5 line 1326). Not a cycle, not an error; vacuously true. (Whether a *vacuous-rule* warning à la §0.6 item 8 / PRE0154 should fire is not stated — see gaps.) |
| **Mutual reference** `A min B` + `B min A` | **Canon explicit:** "mutual reference is satisfiable (`A min B` + `B min A` ⟹ `A == B`), not a cycle" (§3.5 lines 1326, 1328). Satisfiable constraint, not a structural error — explicitly contrasted with computed-field cycles. |
| **Bound references a computed field** (`min ComputedFloor`) | **Canon silent.** §3.5 says modifier value expressions have "All field names" in scope; computed fields are fields. But the canon does not state whether a computed field, whose value is derived from the final configuration, can serve as a bound, nor how its interval is inferred for decidability. No statement either way. Flag as gap. |
| **Cross-type bound** (`number` field bounded by a field of a different numeric type) | **Partially grounded.** The desugared rule is `Amount >= Floor`, a comparison; §3.2/§3.6 numeric lane rules govern it. `integer` widens to `decimal`/`number` implicitly (§3.2 line 1223); `decimal` does **not** widen to `number` (line 1229), so `field Amount as number min DecimalFloor` would, under the comparison rules, be a lane-crossing the spec calls "semantically dangerous" and requires a bridge for (`primitive-types.md` line 424). Canon implies such a cross-lane field-reference bound is a type error absent an explicit bridge — but the canon never states this *for the modifier-bound position specifically*. Defensible but not explicit; flag for confirmation. |
| **Unit-bearing quantity as bound** (`quantity` field, bound is a `quantity` field) | **Partially grounded, with a locked adjacent rule.** `business-domain-types.md` line 428: a bound carrying a qualifier "must match the field's qualifier on the same axis," else `BoundsQualifierMismatch` (PRE0134); the only exception is same-dimension UCUM unit conversion on `quantity`. But line 426 ("Bound expression interpretation") only enumerates *typed-constant* and *number-literal* bounds — it does not contemplate a *field-reference* bound that carries a qualifier. So the qualifier-compatibility machinery is specified for literal bounds and **silent on field-reference bounds**. Flag as gap. |
| **Bound references a field not yet assigned a value** | **Canon grounded via evaluation model.** §3.5 line 1328: a rule (hence a constraint modifier) "is checked against the complete working copy *after* all mutations." There is no "not yet assigned" moment at constraint-check time — every field holds either its default or a mutated value when the constraint is evaluated. So this case does not arise as the question frames it; the bound always sees a materialized value. (Default-value materialization order, §3.5 line 1324, is a *default-expression* concern, not a *constraint* concern.) |

## Contradictions and gaps found

1. **Spec contemplates field-reference bounds; the proof data model and bound-interpretation rule do not.** §2.4 line 1112 explicitly names "a relational constraint" as a constraint-modifier shape and §3.5 line 1326 explicitly works through `A min B` and `min X`. But `IntervalContainmentProofRequirement` stores bounds only as `decimal?` (`catalog-system.md` line 625; `proof-engine.md` line 583), and `business-domain-types.md` line 426 enumerates *only* typed-constant and number-literal bound forms ("Typed-constant bounds … Number-literal bounds … extracted unchanged"). The spec's expression-level surface and the proof engine's obligation shape are not reconciled: the spec says a field-reference bound is a legitimate constraint-modifier form, the obligation model has nowhere to put it. This is a genuine spec-vs-implementation tension, quoted on both sides above. It is **not** a locked rejection of field-reference bounds — `business-domain-types.md` contains no `## Alternatives rejected` block addressing them — so per the project's drift-handling posture it reads as an unbuilt/under-specified feature, not a settled "no." (Recorded, not resolved, per the neutrality discipline.)

2. **`InvalidModifierBounds` is undefined for non-decidable orderings.** §3.8 line 1659 fires `InvalidModifierBounds` when "`min` value exceeds `max` value." With field-reference bounds (`min Floor max Ceiling`), the ordering `Floor <= Ceiling` may be statically undecidable (both unbounded). The canon does not say whether the check (a) is skipped, (b) uses the fields' declared intervals (firing only when *provably* `Floor > Ceiling`), or (c) is an error because non-literal bounds can't be ordered at compile time. Silent.

3. **Vacuous-bound classification unstated.** §3.5 line 1326 calls `min X` "vacuous." §0.6 item 8 / PRE0154 is the vacuous-rule detector. The canon does not say whether a vacuous field-reference bound should emit the vacuous-rule diagnostic or be silently accepted. Silent.

4. **Generated rationale wording for non-literal bounds unspecified.** §0.1 line 107 guarantees a generated rationale but only illustrates the literal case ("Qty must be at least 5"). The wording for `min Floor` is unspecified. Minor gap.

5. **Computed-field-as-bound, cross-lane-field-as-bound, and qualifier-bearing-field-as-bound are all unspecified** (see D5 table rows). The qualifier case is the sharpest: `BoundsQualifierMismatch`/PRE0134 (`business-domain-types.md` line 428) is specified for *qualifier-bearing literal* bounds, and the bound-interpretation rule (line 426) never extends to field references — so it is genuinely undefined whether a `quantity in 'kg'` field bounded by another `quantity` field triggers PRE0134, the same-dimension conversion exception, or neither.

6. **No conflict with any locked decision.** I read `business-domain-types.md` in full. The locked decisions (D1–D19) concern type identity, arithmetic, qualifiers, and registries; **none rejects or even mentions a field-reference bound**. D6 rejects a `units {}` block (line 1721) — unrelated. There is no locked "no X" decision a field-reference bound would override. (Stated explicitly so a reviewer need not re-derive it.)

## Open questions for design

These are for a `/lifecycle-2-design` pass, not for this research. Each is framed neutrally with the options the canon leaves open; no option is pre-ranked.

**Q1 — Admission policy when the bound contributes no static range.** When a field-reference bound's referenced field is unbounded (no decidable range), should the bound be:
  - (a) **Admitted** as a runtime-enforceable constraint (sound at runtime; contributes no compile-time interval; downstream obligations that depended on it stay unresolved → obligation diagnostics) — consistent with §2.4 line 1112 treating decidability as governing *proof participation* only, not *legality*; or
  - (b) **Rejected / routed to longhand** — consistent with `business-domain-types.md` line 430 ("if the bound cannot participate in a proof, accepting it would silently weaken the governance guarantee"), which was written for the *qualifier-missing* case but states a general posture.
  The canon supports a reading in both directions (D3 Reading A vs B; D4); this is the central decision.

**Q2 — Proof-obligation data model.** If field-reference bounds are admitted (Q1a), does `IntervalContainmentProofRequirement` gain a field-reference bound variant (e.g. a DU split between a constant bound and a field-reference bound), or do field-reference bounds discharge through the existing relational path (Strategy-4-style field-to-field reasoning) rather than interval-containment? Note the existing relational path is guard-driven and subtraction-only (`proof-engine.md` lines 1330, 1397) — extending it to modifier-declared bounds and to `>=`/`<=` comparisons would be new surface. Catalog-discipline (DU for varying shapes) is relevant here.

**Q3 — `InvalidModifierBounds` semantics for field-reference bounds** (gap 2). Skip / prove-from-declared-intervals / reject?

**Q4 — Vacuous and mutual-bound diagnostics** (gap 3). Does `min X` (vacuous) or `A min B` + `B min A` (⟹ `A == B`) emit any diagnostic, or are both silently accepted as §3.5 line 1326 currently implies?

**Q5 — Cross-type / cross-lane field-reference bounds** (D5, gap 5). Does a field-reference bound of a different numeric lane require the same explicit bridge a comparison requires (`primitive-types.md` line 424), and is that a type error or a coercion at the modifier-bound position?

**Q6 — Qualifier-bearing field-reference bounds on `money`/`quantity`/`price`** (gap 5). Does `BoundsQualifierMismatch`/PRE0134 and the same-dimension conversion exception (`business-domain-types.md` line 428) extend to a field-reference bound, and what does the bound-interpretation rule (line 426) become when the bound is a field rather than a typed constant or literal?

**Q7 — Computed field as a bound** (D5). Admit, and if so how is its interval inferred for decidability — or exclude?

**Q8 — Event-arg exclusion confirmation** (D2). Confirm that a modifier bound, inheriting rule scope, excludes event args (§3A.1 line 1838), so `min SomeEvent.Arg` is out of scope by the same reasoning that keeps rules event-arg-free.
