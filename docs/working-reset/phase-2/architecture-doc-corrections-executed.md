# Phase 2 — the rows executed against `docs/compiler-and-runtime-design.md`

Uncommitted working-tree edits, made 2026-07-27. Every row below is one of the `N*` rows that
[`contradiction-register.md`](contradiction-register.md) files against this document — § 1.3 (the
never-swept architecture overview), plus the rows in § 2, § 3, § 4, § 8 and § 9 that name it. Line
numbers in the *After* column are post-edit.

Nothing outside `docs/compiler-and-runtime-design.md` was touched. `docs/language/`, `docs/runtime/`,
`docs/compiler/`, `docs/philosophy.md` and `docs/Working/` are other slices.

The replacement wording is taken from the corrected `docs/language/precept-language-spec.md` §0.6 and
§0.7 as they now stand, and from `docs/Working/what-i-want-2026-07-16.md`. Nothing was invented: where
neither supplies a replacement, the row says so.

## Rows executed

| Row | Line before | Line after | Removed | Written in its place | Grep pattern now returning zero |
|---|---|---|---|---|---|
| N60 | 125 | 125–129 | "The compiler cannot know what value a caller will supply, so this rule is **not proven at compile time** — it is a `ConstraintKind.Invariant` constraint plan **governed at runtime** … where it is *proven* … versus *governed* … is decided by **provenance**, not by whether the rule involves a fault." | The `PlanMonthlyFee` example is kept and rewritten, because the point it illustrates — that Precept guarantees rules with no fault in them — survives. Three paragraphs: (a) the natural conclusion that an externally-supplied field cannot be proven, named and refused, with "where a value came from does not decide whether a rule is proven or governed; a declared premise does"; (b) the rule proven at compile time, with the obligation attaching to *both* writing rows because the rule mentions both fields, closing from a declared premise or else the definition is rejected, and the accepted cost stated (more definitions refused, authors write guards they would not otherwise have written); (c) runtime governance doing the one narrow job of holding the incoming argument to its declared constraint, and *not* re-evaluating the rule against the finished configuration. | `is decided by provenance`, `governed at runtime`: (the phrase survives only at `:145` describing the third mechanism, which is correct) |
| N61 | 135 | 139 | "split only by where the value comes from: a bound on an externally-supplied field is governed at ingress, while the identical bound on a value computed inside the definition is proven at compile time" | "they are not alternatives to each other: every declared rule is proven at compile time, and ingress makes the premises those proofs rest on true of the values that arrive from outside." The one asymmetry that survives is stated as such — a computed value has no ingress door, so for computed values the compile-time proof is the entire mechanism. That is a statement about where a check *could* sit, not about what gets proven, which is what the register asked for. | `split only by where the value comes from` |
| N62 | 149, 165 | 153, 169 | Nothing deleted — **confirmed by reading**, per the row's instruction. The composition seam at `:153` ("the compile-time proof for an externally-sourced operand relies on the operand's *declared* constraint; ingress governance makes that constraint hold on the actual value") carries no implication that an externally-sourced rule is therefore governed rather than proven, so it survives intact. | At `:169` only the trailing sweep clause changed — see N13. | n/a — confirmed by reading |
| N63 | 171 | 175 | Nothing deleted — **confirmed by reading**. "(c) a *computed* value has **no external ingress to govern**" stands as an argument about computed values, which is what the row permits. | Unchanged. | n/a — confirmed by reading |
| N11 | 133 | 137 | "Every declared constraint is enforced on every operation … Externally-sourced values are checked at ingress; mutations run on a working copy discarded whole if any constraint fails (spec §3A.4), so no operation commits a configuration that violates a declared rule." | The governance bullet restated as ingress: every value entering from outside is held to the constraints declared on the slot it enters, at the moment it enters, before any computation derives from it — the field's `min`/`max`, `notEmpty`, a length or count bound, a constraint on the argument. Operation-blind, no bypassing code path. Then explicitly: "It is the field's own declared contract and nothing wider: no second pass re-checks the finished configuration after the mutations complete, because every handler was already proven to preserve every rule." Guards are named as surviving, and named as row selection rather than a re-check of the result. | `discarded whole if any constraint fails` |
| N12 | 147 | 151 | "Mutations run on a working copy that is discarded if any constraint fails (spec §3A.4), so no invalid configuration commits." | Atomicity kept and made unconditional — all mutations take effect or none does, no partial state observable, "promotion is not conditional on a constraint check." Then the reason: "There is no post-mutation pass left to make a pass/fail decision, because every handler was proven at compile time to preserve every rule. No invalid configuration commits because none can be produced." | `so no invalid configuration commits` |
| N13 | 165 | 169 | "enforcing it operation-blind **on a working copy discarded on failure**" | "enforcing it operation-blind and refusing the value at the boundary rather than after the fact". The composition seam around it is untouched (N62). | `on a working copy discarded on failure` |
| N14 | 835 | 828–842 | The per-operation table's "Constraint plans evaluated" column, and the sentence above it ("Every operation evaluates constraints through the same prebuilt plan indexes"). | The column is replaced by **"Ingress validation of supplied values"** — event arguments for `Fire`, the patch's field values for `Update`, construction inputs for `Create`, none for `Restore`. Access-mode checks and row dispatch keep their columns unchanged. Lead sentence becomes "Ingress validation, access-mode checks and row dispatch are the three things an operation does before it executes. No operation re-checks the finished configuration afterwards." Section renamed **Per-operation validation and dispatch matrix**, and the Contents entry at `:70` updated to match. **What the table does not say** is what becomes of the `ConstraintPlanIndex` itself — see "Left unedited" below. | `Constraint plans evaluated` |
| N15 | 861, 883 | 858, 880–882 | "Business outcomes: `Rejected`, `EventConstraintsFailed`, `UpdateConstraintsFailed`, `RestoreConstraintsFailed`", and the same three names in the per-operation outcome table. | Business outcomes reduce to "`Rejected` — a matched row whose disposition is `reject`". A new paragraph states why the removed outcomes cannot come back: "a definition only compiles if every handler was proven to preserve every rule, so no operation can reach the point of committing a configuration that breaks one. An outcome for that case would name something the compiler already made impossible." Outcome table: `Create`/`Fire` domain column is `Rejected`, `Unmatched`; `Update`'s is `AccessDenied`; `Restore`'s is `—`. | `EventConstraintsFailed`, `UpdateConstraintsFailed`, `RestoreConstraintsFailed` |
| N20 | 883 | 882 | The `Restore` row's `RestoreConstraintsFailed` cell. | Folded into N15 above; the cell is now `—`. | `RestoreConstraintsFailed` |
| N16 | 718 | 718 | "`Restore` itself does not reject them … **but the next operation's post-mutation sweep will**, so a stale entity can become unusable rather than un-loadable." | "…and no later operation re-governs them either, because there is no pass over the finished configuration that would. A stale value therefore persists, silently, until something writes over it." Then the consequence stated honestly, in the spec's own terms: the proofs lean on "every rule already held before this operation", which is proven for contract data and *assumed* for data restored under a definition that has since changed. The known-gap/migration-deferred paragraph is unchanged. | `the next operation's post-mutation sweep will` |
| N17 | 767 | 766 | "restored state is trusted as valid at the time it was persisted, and **is re-governed by the next operation's post-mutation sweep** (spec §0.7, owner decision 2026-06-02)." | "…and no later operation re-governs it (spec §0.7)." The trusted-hydration half survives verbatim. | `re-governed by the next operation's post-mutation sweep` |
| N18 / N76 | 907 | 906 | The Precept Innovations bullet: "**Restore with recomputation-first constraint evaluation.** Persisted data is **never trusted** — Restore recomputes computed fields before evaluating constraints, catching stale computed values that would otherwise pass through silently." | Replaced, not merely deleted, with the claim that is actually true and is already stated at `:842`: "**Trusted hydration on `Restore`.** Persisted data is not re-checked on the way back in — it was governed when it was written, so hydration reconstitutes the entity rather than making a second pass over the whole configuration, exactly as a direct database read trusts the rows it loads. `Restore` does recompute computed fields, so a derived value is re-derived from the restored inputs rather than read back as stored." The document no longer contradicts itself at `:842` (N76 discharged). | `Persisted data is never trusted` |
| N36 | 116, 151, 167, 370, 598, 629, 635, 645, 701, 706, 743, 763, 816, 825, 829, 866, 875 | see below | All seventeen sites carrying the fault-backstop model. | Site by site: `:116` → the evaluator "carries no divide-by-zero test of its own. It performs the division on the strength of the proof, and if the proof were wrong nothing downstream would catch it." `:155` (was 151) → the "Out-of-contract data and traps" paragraph becomes "Out-of-contract data": restore and host injection are outside the envelope, "nothing stands behind those cases at evaluation time", and what makes it safe for the evaluator to compute is the proof certificate verified once at load. `:171` (was 167) → walk step 5 becomes "There is nothing standing behind the proof" — no re-tested divisor, no re-checked bound, no overflow guard; the certificate justifies it, and the runtime refuses to govern under a definition whose certificate does not verify. `:370` → the "Fault-site backstops" bullet deleted from what crosses into the executable model. `:601` (was 598) → the runtime receives the certificate, not fault-site residue, and "plants no checks of its own at the sites the proof cleared". `:632` (was 629) → "Precept Builder fault backstops" dropped from the proof engine's consumers. `:638`–`:645` → the chain now reads `catalog metadata → ProofRequirement → ProofObligation → DiagnosticCode ↔ FaultCode` and stops there; the `FaultSiteDescriptor` link and its trailing sentence are gone, and the `DiagnosticCode ↔ FaultCode` pairing is restated as a build-time integrity check over the catalogs, "not a runtime path: nothing is planted at the site, and the evaluator has nothing to fall back on." `:701`, `:743`, `:824` → "fault-site backstops" dropped from the artifact, builder-output and evaluator-input lists. `:706`, `:824` → `Fault` re-described as "engine-invariant breach only". `:745` → "proof-owned fault-site residue" dropped from the catalog-role sentence. `:763` → the "fault backstops" row deleted from the executable-model contract table. `:816`, `:820` → the `FAULT(["Fault backstops"])` node and its edge deleted from the runtime diagram. `:829` (now 828) → `Fault` re-scoped to "a breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator. It is not a fault-prevention check standing behind the proof; the evaluator carries none." `:865`, `:874`, `:903` → the same re-scoping in the outcome taxonomy, its category table and its innovations bullet. | `defense-in-depth`, `backstop`, `FaultSite`, `impossible-path` |
| N39 | 132 | 136 | "division, overflow, empty-collection access, **bound containment** are *illustrations*" | "division, overflow and empty-collection access are *illustrations*", plus a parenthesis re-classifying containment in the spec's own words: a bound is rule shorthand, so keeping a value inside it is rule preservation under the same single constraint mechanism, identical disposition, "but it is not a fault gate and should not be quoted as one." The site-sweep sentence widens from "every site where a computed value could leave its declared limits" to "every site where a value could break a declared rule or leave its declared limits". | `bound containment are \*illustrations\*` |
| N58 | 598, 161 | 601, 655 | "proof is bounded — **four strategies only**, no general SMT solver"; and the innovations bullet "Four strategies only, each a simple predicate function". | Both become "a closed, non-extensible set of strategies, each a simple predicate, and no general SMT solver". **The count is removed rather than reconciled**, with the reason stated inline: the strategy set is enumerated in `proof-engine.md`, "and this document does not restate the count, because the counts stated across the design documents disagree with each other and reconciling them is that document's job." The `:631` cross-reference "strategy set with bounded four-strategy rationale" is reworded the same way. | `four strategies only` |

## Sites corrected that no row enumerates

Each asserted the same model inside a passage a row rewrote, so leaving it would have made the
document contradict itself in the same section.

- **`:106`–`:107`, the two-places enumeration.** The register has no row here, but the second bullet
  read "an **externally-supplied value** crossing an operation boundary — covered at *runtime* by a
  declared constraint the engine enforces on every operation, **on a working copy discarded whole if
  it fails**." It carries the sweep and, with N60 and N61 deleted, would have been the last statement
  of the provenance split left standing — in the document's opening claim, where a reader meets it
  first. Both bullets are restated: the first as "a value the definition itself produces — a computed
  field, or a value a handler writes — that could break a declared rule or land outside a declared
  bound", covered at compile time; the second as a value supplied from outside, "held at *ingress* to
  the constraints declared on the slot it enters … That is what makes the premises the compile-time
  proofs rest on true of the value that actually arrived." The two-places structure and the
  closed-net-not-a-floor argument around it are unchanged.
- **`:127` (now `:131`), the "sharper categorical gap" sentence.** It summarised the split N60 and N61
  delete — "by proof where the value is computed and by governance where it is supplied". Now "by
  proof at compile time, with ingress making the premises those proofs rest on true of the values that
  arrive from outside", and the closing line becomes "they are the same closed net, discharged the
  same way."
- **`:851`, `:899`, `:885`, `:904`, `:976` — the outcome names and inspection claims in prose.** The
  register's N15 covers the taxonomy lists; these are the same names inside sentences. `:848` (was
  851) now contrasts `Unmatched` with `Rejected` alone. `:898` (was 899) lists the outcomes that
  actually carry a `ConstraintViolation` — `Rejected`, `InvalidArgs`, `InvalidInput`. `:884` and the
  two innovations bullets at `:903`/`:975` claimed inspection previews "with full constraint
  evaluation"; that is R4's claim in this document, so inspection is restated as previewing which rows
  match, which guards hold and whether the supplied values pass ingress, with an explicit sentence
  that it does not predict a post-mutation constraint verdict "because no such check runs".
- **`:842`, rule (2) of the two rules.** "`to` ensures … do not participate in `in`-anchor evaluation"
  was phrased as a statement about runtime evaluation. Reworded as the scoping rule it actually is:
  "they scope to entering a state, not to residing in it." Same treatment the spec slice gave S5.
- **Three uses of banned vocabulary** (`CLAUDE.md` § How to write): "the design's spine" at `:6`,
  "this distinction is load-bearing" at `:135`, "the load-bearing condition" at `:175`. Two sit inside
  passages this pass rewrote; the third is a one-clause substitution.

## Left unedited, and why

- **The `ConstraintPlanIndex` question (N10 / N80).** N14's column is gone, but nothing in this
  document now says what the runtime's validation set *is* — whether the constraint-plan index
  survives in reduced form for ingress and guards, or is replaced by a premise list derived from the
  certificate per want:210. The want document and the corrected specification do not answer it, and
  the register files it as the largest open consequence, waiting on the owner. So the matrix states
  only what is settled (ingress validation, access-mode checks, row dispatch) and says nothing about
  the index. `constraint-plan indexes` still appears in the builder-output and evaluator-input lists
  at `:701`, `:743` and `:824`, unedited, because deleting it would assert an answer and keeping it
  described as a commit-time sweep would assert the removed one. **This is the one place in this
  document where the correction is incomplete on purpose.**
- **What inspection returns (N79).** want:211 gives the shape — "no check needed here, proven from
  these premises" — but not the result type, and the register routes the result type to phase 5. The
  prose now says what inspection previews and what it no longer predicts; it does not name a new
  inspection result shape.
- **The strategy count itself (N57 / N58).** This document's "four" is removed, but the reconciliation
  belongs to `docs/compiler/proof-engine.md`, which is another slice and states four different numbers
  internally. Removing the count here reduces the disagreement from four documents to one; it does not
  settle it.

## One contradiction the register did not record

`:106`–`:107` — the "exactly two places a bad configuration could arise" enumeration in § 1, which
carries both the working-copy-discard clause (want:199) and the provenance split (walk item 16) in the
document's opening statement of the guarantee. The register files fifteen sites in this document under
those two families and this is not among them. It is corrected above and recorded here so the register
can be amended.
