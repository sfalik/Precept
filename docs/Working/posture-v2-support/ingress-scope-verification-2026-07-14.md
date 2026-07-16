---
title: "Ingress-Scope Verification — Shane's Governance-Carve-Out Claim"
status: Research/verification — 2026-07-14 (read-only; no files edited)
author: George (Runtime Dev)
task: Independent research/verification per Shane's request — does the ratified design
      treat governance as ingress-only, or does it include a broader post-mutation sweep?
sources-read:
  - docs/compiler-and-runtime-design.md (§1 What Precept Promises, §1.1–§1.2, §11 Runtime)
  - docs/language/precept-language-spec.md §0.7 (:264–:270), §3A.4 (:1965 area), §0.6 (:200–:215)
  - docs/Working/proof-engine-boundary-ruling-2026-07-06.md (§1, §3, §4 Obligation-Role Rule, worked examples rows 1–17)
  - docs/Working/proof-engine-decision-ledger-2026-07-12.md (Decision #1 full text)
  - docs/Working/frank-retrospective-proof-engine-arc-2026-07-13.md (§1–§2)
  - docs/Working/frank-prove-or-reject-position-2026-07-11.md (§1–§2, the "precise scenario in dispute")
  - docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md (§6 divergences, Rec A)
  - docs/runtime/result-types.md (:113–:121, ConstraintsFailed scope)
---

# Ingress-Scope Verification

*Read-only research by George. No edits to any file. All citations verified against source text.*

---

## Shane's Claim (verbatim from task description)

> "I thought the only carve-out, and what I asked to be explicitly called out and described,
> was ingress (editable fields and args) that would remain runtime only. Everything else,
> including **declared relational invariants** (`rule`/`ensure` constraints spanning multiple
> fields, i.e. relational invariants over definition-derived/internal state) should be
> compile-time **prove-or-reject**, full stop — no separate runtime-governance/sweep mechanism
> for those."

---

## Plain answer: Shane's claim does not match the ratified corpus.

**The ratified design includes two governance enforcement points, not one.** Ingress is the
first. A post-mutation constraint sweep is the second. Both are canonical, both are cited in
the ratified documents, and neither is a v2 invention.

**Relational invariants over independently-set fields are governed at runtime via the sweep —
not proved at compile time.** This was an explicit, deliberate design choice. The alternative
(compiling relational invariants to compile-time proof obligations) was considered and rejected
as pure Model B. 21 of 77 sample files depend on nonlinear governance rules that pure Model B
would refuse.

---

## 1. What canon says about the scope of governance

### Spec §0.7 (`:264–:270`) — the two-mechanism contract

> "**Governance — enforced at runtime on external input.** Every declared constraint is
> enforced on every value entering the entity from outside the definition — event arguments,
> construction inputs, direct field edits — at the moment it enters, before any computation
> derives from it. ... Because mutations execute on a working copy that is discarded if any
> constraint fails (§3A.4), an invalid configuration never persists — this is prevention, not
> detection."

This is the governance description Shane is quoting. It names ingress. It also cites `§3A.4`
for the working-copy discard mechanism. But §0.7 does not say ingress is the ONLY enforcement
point — it ends by pointing to §3A.4 for the implementation.

### Spec §3A.4 (`:1965` area) — two enforcement points, stated plainly

> "All mutations execute on a working copy. Constraints are evaluated against the working copy
> after all mutations complete. If every constraint passes, the working copy is promoted to
> become the entity's committed state. If any constraint fails, the working copy is discarded
> and the entity's state is unchanged.
>
> **This post-mutation sweep is one of two enforcement points.** Externally-sourced values are
> first validated against their declared constraints at **ingress** — as they enter, before the
> working copy derives any dependent value (governance, §0.7). The sweep then re-checks every
> constraint against the completed working copy. **Ingress governs *what enters*; the sweep
> governs *the result*.**"

This is explicit. The spec names two enforcement points and distinguishes them by function:
ingress = one incoming value against its own carried constraint, before derivation; sweep = all
whole-entity constraints against the completed post-mutation working copy.

The sweep is not an implementation detail of ingress. The spec presents them as separate
mechanisms with distinct jobs.

### compiler-and-runtime-design.md §1.1 — governance covers relational rules

> "**Constraint governance** — enforced by the **runtime** — and the philosophy leads with
> this one ('governance, not validation'). Every declared constraint is enforced on every
> operation, with no code path that bypasses it: a `rule` invariant, an `ensure` on a state
> or event (the `ConstraintKind` family — invariant, state-resident/entry/exit, event-
> precondition), a field's `min`/`max`, `notEmpty`, or length/count bound, and **any relational
> or comparison rule over the configuration**."

The canonical design doc lists "any relational or comparison rule over the configuration" as
part of constraint governance. This explicitly includes multi-field relational invariants —
not just individual field ingress checks.

### result-types.md (:113–:121) — ConstraintsFailed is the sweep result

> "**`EventOutcome.ConstraintsFailed` scope:** Covers ALL post-fire constraints: global rules,
> state ensures (`in`/`to`/`from`), AND event ensures."

`ConstraintsFailed` is produced by the post-mutation sweep. It covers "global rules, state
ensures, AND event ensures" — all three constraint kinds, not just ingress-checked field
constraints. The boundary ruling (row 10) cites `result-types.md:121` for the governance
disposition of relational invariants precisely because `ConstraintsFailed` is how the sweep
enforces them.

---

## 2. What the boundary ruling says about relational invariants

The boundary ruling was ratified by Decision #1 ("folded into this ruling") on 2026-07-12.
Its worked-example table rows 10 and 11 are the authoritative dispositions:

**Row 10** — standalone nonlinear relational invariant:
> `rule TotalCost == AvgCost * Qty`
> Obligation: Invariant → **Gov** (`result-types.md:121`); `*`-overflow → Parked (D1)
> Disposition: **Governed** (expressible, never refused). "The identity is enforced by the
> **constraint sweep**."

**Row 11** — linear invariant vs. action, side by side:
> `rule Balance == Deposits - Withdrawals` (invariant form) → **Gov**
> `set Balance = Deposits - Withdrawals` (action form) → **P-or-R** vs Balance's bound
> "Distinguishes *declaring a relationship* (governed) from *computing a value* (proven)."

Both rows show the same pattern: a `rule` declaration spanning multiple independently-set
fields → **governed at runtime by the sweep**, not proven at compile time.

The boundary ruling was explicit about WHY the nonlinear case is governed:
> "Not because 'undecidable ⇒ govern' (all constraints are governed). It is governed because
> it is a *constraint* (A.1). Its *undecidability* means it cannot also be discharged as
> vacuous/contradictory and cannot serve as a family-A.2 carrier for other obligations — so
> governance is the *only* enforcement mechanism left for it."

And for the general principle (§3 Leg 2, rejecting pure Model B):
> "Pure Model B — rejected on expressiveness... a nonlinear **invariant** —
> `rule TotalInventoryCost == AverageCost * QuantityOnHand` — is a *governed constraint*, not
> a *fault-prone operation*. Governance already enforces it: the runtime constraint sweep covers
> 'global rules, state ensures … AND event ensures' (`result-types.md:121`), discarding any
> operation that violates the identity. Pure-B would either refuse the rule (needless loss) or
> pretend to prove it (impossible for a genuine product). The cost is not an edge case:
> **21 of 77 sample `.precept` files contain product expressions**..."

---

## 3. The Obligation-Role Rule (ratified boundary)

The Obligation-Role Rule (boundary ruling §4, folded into Decision #1) is the authoritative
boundary statement. It defines two obligation families:

> **A. Every declared constraint `C` — modifier or rule, indistinguishably — generates two
> obligations, always, regardless of decidability or spelling:**
>
> 1. **Governance (runtime, at ingress).** For each field `C` reads, whenever a **raw external
>    value** enters that field ... `C` is enforced on the resulting working copy, which is
>    discarded if `C` fails (`:268`, `§3A.4:1965`). Operation-blind.
> 2. **Premise contribution (compile-time).** `C` contributes its provable fact to the proof
>    engine's interval/relational knowledge (`:203`; `:1138`), to the extent it is decidable.
>
> **B. Every fault-prone operation `O` over a definition-derived value carries one obligation:
> compile-time prove-or-reject** (with the single D1 overflow carve-out).

Family A.1 is governance. It is triggered "for each field `C` reads, whenever a raw external
value enters that field." For a multi-field relational rule like `rule X + Y <= 100`, this
fires when X changes (checking X against `X + Y <= 100`) AND when Y changes. In a single
operation that changes both X and Y simultaneously, the sweep checks the combined final state
— not each individual ingress point in isolation. That is why §3A.4 says the sweep
"re-checks every constraint against the completed working copy."

Family B is prove-or-reject. It applies to fault-prone operations over DERIVED values (values
computed by Precept expressions: assignment RHS, computed fields, aggregates). A relational
`rule` is NOT a family-B obligation — it's a family-A constraint.

The one-line boundary (boundary ruling §4):
> *"A raw external value at its own ingress slot is **governed**; any value a Precept expression
> has derived from it is **proven-or-rejected**..."*

This distinguishes governance (external values) from prove-or-reject (derived values). A
relational invariant over independently-set fields is in the governance bucket — not because
the fields are at ingress in the same operation, but because they're all externally-supplied
(none is derived by a Precept expression).

---

## 4. The "precise scenario in dispute" and what was ratified

Frank's position paper (`frank-prove-or-reject-position-2026-07-11.md`) explicitly fenced the
contested territory:

> "The one disputed case is: **a value computed by the precept's own rules, flowing into a
> plain policy limit, where the compiler can neither prove it stays in-bounds nor prove it
> always breaks.**"

And it explicitly stated what was NOT disputed:

> "**Externally-supplied values entering a bound → GOVERN. Uncontested.** When an event arg
> or a field edit carries `min 5`, governance enforces it at the moment the value crosses the
> contract boundary. ... This is governance's home turf and I have never contested it."

The identity thesis (Fable, the counter-position) proposed governing computed-value bands at
runtime. Shane ratified Decision #1: prove-or-reject for the disputed computed-value case,
rejecting the thesis's GOVERN position for that specific case.

But the GOVERN disposition for relational invariants over independently-set fields was NEVER
the disputed case. It was uncontested from the start. Both Frank and the thesis agreed:
externally-supplied values entering a declared constraint → governed at ingress/sweep.

A relational invariant like `rule TotalCost == AvgCost * Qty` where all three fields are
independently-set is in the "externally-supplied values entering a declared constraint" bucket,
not the "computed value flowing into a policy limit" bucket. The dispute never touched it.

---

## 5. What the Hybrid model specifically covers — Decision #1 folded-in language

Decision #1 (decision ledger, Shane 2026-07-12):

> "✅ RULING (Shane, 2026-07-12): A — prove-or-reject, RATIFIED."
>
> "**Folded into this ruling if you take A** (converged, no separate debate needed): the
> **Hybrid model** + 'raw input is governed at the door; anything computed from it is proven'
> boundary..."

And the Hybrid model definition (boundary ruling §3 table):

> Hybrid: "Total over the fault surface — no fault deferred. **Admitted as a governed
> constraint** (not a deferred fault); a fault-prone *operation* it feeds is still
> prove-or-reject."

The Hybrid model explicitly admits undecidable/nonlinear relational invariants as **governed
constraints**. This is what "admitted as a governed constraint" means in the model definition.

And Decision #1 ratified the Hybrid model as "folded in" — meaning governance of relational
invariants is part of what Shane ratified in Decision #1, even though it was not the disputed
item.

---

## 6. Why the sweep is necessary and not redundant with ingress

For a single-field constraint like `max 100` on an editable field: ingress is sufficient.
When the external value enters, the constraint is checked. Done.

For a multi-field relational constraint like `rule X + Y <= 100` where X and Y are both
editable: ingress alone is insufficient when a single operation changes both X and Y
simultaneously. Ingress for X alone would check `x + current_Y <= 100` using the OLD Y value,
missing the combined violation. The sweep checks the final state with both new values.

Concrete example:
- Current state: `X = 80, Y = 10` (valid: `80 + 10 = 90 ≤ 100`)
- Operation fires event `SetBoth(x=70, y=35)` that assigns both X and Y
- Ingress for X (`x=70`): checks `70 + 10 <= 100` → passes (using old Y)
- Ingress for Y (`y=35`): depends on order — if X already updated, checks `70 + 35 <= 100` →
  fails. But if ingress checks happen with original values, both pass.
- **Sweep**: checks `70 + 35 <= 100` → **fails** → operation discarded

The sweep is not redundant. It enforces the COMBINED STATE after all mutations apply. Spec
§3A.4 is explicit: "There is no window between mutation and constraint checking where a
partially-committed state with violated rules can be observed."

---

## 7. What "prove-or-reject" does and does not cover

**Prove-or-reject covers (family B):**
- Division (divisor non-zero)
- `sqrt`/`pow` non-negativity
- Empty-collection/index access
- A computed value (`set X = expr` or `field X <- expr`) flowing into a declared bound
  (`OutOfRange`, assignment-range impossibility)
- Count/cardinality containment when the count is computed

**Prove-or-reject does NOT cover:**
- Relational invariants expressed as `rule`/`ensure` declarations over independently-set fields
- Single-field constraints on externally-set fields (governed at ingress)
- Nonlinear product/ratio/balance invariants (`rule TotalCost == AvgCost * Qty`) over
  independently-set fields

The boundary is provenance: "A raw external value at its own ingress slot is governed; any
value a Precept expression has derived from it is proven-or-rejected." (Boundary ruling §4.)

A `rule` declaration doesn't derive any value. It declares a relationship that must hold. No
field's value is computed from another by a Precept expression when you write
`rule Balance == Deposits - Withdrawals` with independently-set fields. So there's no
derivation to prove, and the constraint is governed.

---

## 8. The "only carve-out" language in Decision #1 — what it refers to

The decision ledger's ruling #1 says: "overflow stays parked per old D1 (the **one carve-out**)."

"The one carve-out" refers to **representational overflow** (D1: `NumericOverflow`, a computed
value exceeding the type's own representable range). This is the one explicitly named exception
from the live-enforced fault surface.

This phrasing does NOT mean ingress governance is the only runtime mechanism. "The one carve-out"
refers to the single item CARVED OUT of the live prove-or-reject set, not to the scope of
governance. Governance of relational invariants is not a "carve-out from prove-or-reject" —
it was never in prove-or-reject to begin with (it's family A, not family B).

---

## 9. Why Shane's description doesn't match — the most likely source of confusion

Spec §0.7's governance description uses "external input" and "at the moment it enters" language.
This accurately describes ingress. But §0.7's final sentence says: "Because mutations execute
on a working copy that is discarded if any constraint fails (**§3A.4**)..." — pointing to the
full atomicity mechanism.

A reader of §0.7 alone might conclude governance = ingress-only. But §3A.4 adds the second
enforcement point explicitly: "This post-mutation sweep is one of two enforcement points."

The v2 posture doc (§5) correctly presents both mechanisms. The confusion may stem from:

1. **§0.7 is incomplete about governance scope** — it describes ingress and cites §3A.4 for
   the working-copy mechanism, but doesn't explicitly state the sweep is a second check on
   relational invariants.

2. **The dispute was about computed values** — the active disagreement in the arc (that Shane
   resolved with Decision #1) was about whether computed-value writes into non-proof-carrying
   bounds should be governed at runtime or rejected at compile time. Shane chose reject. But
   this dispute NEVER touched relational invariants over independently-set fields; those were
   always in the governed column and were NEVER the subject of the debate.

3. **"Ingress governance" can sound comprehensive** — if you read "governance at ingress" as
   "all governance is at ingress," you'd miss the sweep. But the spec and boundary ruling say
   "governance at ingress AND via the sweep" — two things, not one.

---

## 10. Direct answers to the four verification questions

**Q1: Does canon define governance as ingress-only, or does it define a broader scope that
includes a runtime check on relational invariants beyond ingress?**

**Broader scope.** The spec describes two enforcement points. §0.7 describes ingress; §3A.4
explicitly adds the post-mutation sweep and calls it "one of two enforcement points." The
canonical design doc §1.1 includes "any relational or comparison rule over the configuration"
in constraint governance.

**Q2: If broader — the exact ratified language authorizing it, and what problem it solves.**

**From spec §3A.4 (the canonical spec, owner-committed, not a Working doc):**
> "This post-mutation sweep is one of two enforcement points. Externally-sourced values are
> first validated against their declared constraints at **ingress** — as they enter, before
> the working copy derives any dependent value (governance, §0.7). The sweep then re-checks
> every constraint against the completed working copy. **Ingress governs *what enters*; the
> sweep governs *the result*.**"

**From boundary ruling §4 (row 10), ratified via Decision #1:**
> `rule TotalCost == AvgCost * Qty` → **Governed** via the constraint sweep.

**Problem it solves:** A relational invariant over multiple independently-set fields cannot
be enforced by ingress alone when a single operation changes multiple fields simultaneously.
The sweep checks the combined final state. Without it, a rule like `rule X + Y <= 100` would
not be enforceable if a single event sets both X and Y in one atomic operation.

More broadly: the sweep enforces governance for nonlinear relational invariants (product rules,
ratio identities, balance identities) that cannot be proven at compile time because the fields
are independently-set and no derivation chain exists for the proof engine to follow. These are
21/77 sample files. Governing them via the sweep is explicitly what the Hybrid model does
instead of refusing them (Model B) or silently deferring fault detection (Model A).

**Q3: If Shane's ingress-only claim is correct and v2 overextended — does v2 need correcting?**

**Shane's ingress-only claim is NOT correct per the corpus.** The v2 posture doc accurately
reflects what the spec, boundary ruling, and decision ledger say. If anything, §0.7 slightly
under-describes governance (it names ingress and cites §3A.4, but doesn't call out the sweep
explicitly), and v2's §5 is more accurate than §0.7 alone.

v2 does not need correcting on this point. The spec itself is the question — specifically
whether §0.7 should be expanded to describe the sweep explicitly alongside ingress (this is
the §0.7 doc-sync question in v2 §13, open question #1).

**Q4: Distinguish (a) governance on raw ingress values, (b) post-construction/post-mutation
"whole-entity-state" check, and (c) proof-engine premise contribution.**

| Mechanism | What it is | When it fires | What it checks |
|---|---|---|---|
| (a) Ingress governance | A declared constraint enforced on a raw external value at the moment it enters | At construction (Create), at event-argument binding (Fire), at direct field edit (Update) | The entering value against the field's own carried constraint — before any derivation uses it |
| (b) Post-mutation sweep | All declared constraints checked against the completed working copy after all mutations apply | After all action rows in an operation have executed; before the working copy is promoted | ALL declared constraints (global rules, state ensures, event ensures, field bounds) — the whole-entity relational state |
| (c) Proof-engine premise | A declared constraint's fact contributed to the compile-time proof lattice for discharging prove-or-reject obligations | Compile time, per definition | Whether the constraint is decidable enough to discharge a family-B obligation |

These are three distinct roles for the SAME declared constraint. A single `max 100` on an
editable field participates in all three: (a) checked at ingress when a value enters, (b)
swept with the whole-entity state at commit, (c) contributes its interval to the proof engine
as a family-A.2 premise.

The v2 posture doc's description of "two governance mechanisms" (§5) refers to (a) and (b).
Frank's self-critique H2 correctly noted the tension with §0.7's current language, which
mentions only (a). That tension is NOT resolved by saying governance is only (a); it's an
underdescription in §0.7 that Shane should decide how to fix (v2 §13 open question #1).

---

## 11. Bottom line for Shane

**Your claim — that the only runtime-only carve-out is ingress, and declared relational
invariants should be prove-or-reject at compile time — does not match the ratified corpus.**

The corpus evidence is unambiguous on three levels:

1. **Canonical spec (§3A.4, owner-committed)** explicitly says there are two enforcement
   points, not one. "Ingress governs *what enters*; the sweep governs *the result*." This is
   the spec you own.

2. **Boundary ruling (row 10, row 11, §3 Leg 2)** shows relational invariants over
   independently-set fields as **Governed**, not prove-or-reject. Pure Model B — which would
   require proving these at compile time — was explicitly rejected because 21/77 sample files
   depend on nonlinear governance rules that can't be proven.

3. **Decision #1 (your ruling, 2026-07-12)** folded in the Hybrid model, which admits
   undecidable relational invariants as governed constraints. "The one carve-out" (overflow)
   refers to the D1 overflow park, not to governance scope.

**What "ingress" does and doesn't cover:** Ingress correctly describes the enforcement point
for individual field constraints on externally-set values (event args, construction inputs,
editable fields). It does not describe the enforcement of relational constraints over multiple
independently-set fields when a single operation changes more than one. That's the sweep's job,
and the spec says so.

**What "prove-or-reject" does and doesn't cover:** Prove-or-reject applies to fault-prone
operations over DERIVED values (things computed by Precept expressions). A `rule` declaration
doesn't derive a value — it declares a relationship. Relational invariants over independently-
set fields are governance obligations, not prove-or-reject obligations, because no field's
value is computed from another by a Precept expression.

**If this is not the design you intended:** this would be a significant design pivot. Requiring
the proof engine to prove relational invariants over independently-set fields at compile time
is substantially stronger than the current ratified posture — it is effectively a move toward
pure Model B (total compile-time proof), which was explicitly rejected for expressiveness
reasons (nonlinear invariants can't be proven; rejecting them loses 21/77 sample files). Such
a pivot would need to go through the proper design process.

**What the v2 posture doc gets right:** v2 §5 correctly describes both governance mechanisms.
The open question in v2 §13 (#1) asks whether §0.7 should be expanded to name the sweep
explicitly. That is the right question — the doc-sync direction is §0.7 → expand to match
the ratified design, not v2 → contract to match §0.7's current under-description.

---

*End of verification — George, 2026-07-14. Read-only; no files edited.*
