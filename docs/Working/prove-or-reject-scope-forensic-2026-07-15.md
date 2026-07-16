# Prove-or-reject scope — forensic analysis

**status:** Draft finding — 2026-07-15. Evidence only. Makes no ruling, proposes no design. Companion to [`third-arm-forensic-analysis-2026-07-15.md`](third-arm-forensic-analysis-2026-07-15.md).

**Questions asked.** (1) The 07-12 fold-in reads *"raw input is governed at the door; anything computed from it is proven."* Later formulations read *"prove-or-reject is the disposition for every **fault-prone operation**."* Did the scope narrow — did a fault gate creep in? (2) Is prove-or-reject the fault floor wearing a different name?

**Answers.** (1) **No — the fault gate is not a later narrowing. It is the original owner-committed spec language** (`spec:266`, commit `5af46537`, 2026-06-02), and that same sentence enumerates *"no result outside a declared bound."* But the underlying instinct is confirmed in a sharper form: the fault gate is a **fossil** of an abandoned argument, still quotable in three places, and it is a live trapdoor under the one thing that distinguishes the current posture from the fault floor. (2) **No — and the entire difference is one axis: declared-bound containment.** Under the fault-gated reading, that axis falls out and the answer flips to *yes, and slightly weaker than the start*.

**Method.** Three agents, disjoint slices, **documents only** — no source, no catalog, no probes (a deliberate scope choice: the question is what was *proposed*, not what was built). Each required to quote rather than paraphrase any formulation, to cite `file:line`, to write UNCERTAIN rather than guess, to treat agent-authored `Locked`/`RULED`/`ratified` labels as non-binding, and explicitly **not to flatter the premise**. The first agent's verdict contradicts the hypothesis it was given.

---

## 1. The two grounds — the distinction nobody kept

There are two possible arguments for putting declared-bound containment inside prove-or-reject. **They are not the same argument, and only one survives.**

- **Ground (i) — "it's a fault."** Containment belongs because an out-of-band value is a fault.
- **Ground (ii) — "it's computed."** Containment belongs because the value is definition-derived, and the posture proves what the definition derives — fault or not.

**Ground (i) is refuted.** **Ground (ii) is untouched** by anything the fault-floor research established; that research adjudicated *fault membership* and says nothing about whether a prevention-grounded containment obligation is legitimate.

The corpus migrated from (i) to (ii) between 2026-07-11 and 2026-07-12 **and never announced the move.** Ground (i) appears in no artifact after 2026-07-11. It survives only as text.

---

## 2. The fault test, and why containment fails it

**The definition** — `research/architecture/compiler/fault-floor-definition-2026-06-11.md:122`:

> THE FAULT FLOOR — prove-or-reject, total, stable: if the compiler cannot prove a fault absent, the definition is rejected. **A fault is a mid-evaluation abort with no recoverable typed outcome (only the Faulted trap); anything the working-copy discard or ingress refusal handles (ConstraintsFailed/InvalidArgs/Unmatched/Rejected) is governance.**

Independently derived at `research/architecture/compiler/compile-time-guarantee-boundary-2026-06-10.md:41-42`. The test has two prongs; a fault must pass **both**: (a) the operation aborts mid-evaluation with no result; (b) the only landing place is the `Faulted` trap — no typed outcome exists.

| Operation | (a) aborts? | (b) trap only? | Fault? |
|---|---|---|---|
| `x / 0` | Yes — no result; Principle 10 forbids NaN/Infinity | Yes | **PASS** |
| `sqrt(-1)` | Yes — no real result | Yes | **PASS** |
| `.first` on empty | Yes — no element | Yes | **PASS** |
| `field x max 10` + `set x = 40` | **No** — `40` computes; the arithmetic completes | **No** — surfaces as `ConstraintsFailed`, the typed outcome `:122` assigns to governance | **FAIL** |

Containment fails both prongs. There is no can't-compute referent: the out-of-band value exists, is representable, and is comparable — which is *how the check detects it*. **A check that must first compute the value in order to reject it cannot be preventing a can't-compute condition.**

**One honest complication, flagged not buried.** Prong (b) holds against the *designed* runtime, not HEAD. `fault-floor-definition:144` records that `DesugarsToRule` has "zero consumers today; bounds/modifiers are never synthesized into TypedRules" — so today a bound has no sweep to land in. Frank leans on exactly this (`frank-prove-or-reject-position-2026-07-11.md:133`): *"'GOVERN this at runtime' **today** resolves to that last-resort abort = the fault."* But `:20` states the adjudication is against the **designed** runtime, and Fable answers it (`fable-analysis-of-frank-response-2026-07-11.md:50`): the missing wiring "is already a **floor prerequisite under Frank's own position too**… Its absence is a build-sequencing fact, **inadmissible for either side**." That is correct, and it matches the project's standing no-interim-staging rule. Prong (b) is properly evaluated against the design. Containment still fails.

### The adjudication

`fault-floor-definition:76-78`, verbatim:

> **[rule-misclassified-as-fault] OutOfRange (FaultCode 13, PRE0079)**
>
> **Unanimous:** the fault message defines itself as a declared-band violation; **an out-of-band value computes, stores, compares; every runtime lane refuses recoverably (ingress, sweep, Create sweep). No can't-compute referent — remove from registry.** The compile check (literal default vs own bound) SURVIVES as definition incoherence (every Create would fail — PRE0164/PRE0115 genus, a third category), keeping Error severity with no author-visible change. **Note this prover already has flag posture (fail-open on undecidable magnitude — the opposite of its count/length siblings), internal evidence the band family was never floor.**

That last sentence is the sharpest thing in the document: HEAD's own `OutOfRange` prover already fails open on undecidable magnitude — behaving like a flag, not a floor. The implementation testifies against the classification.

Same verdict for `LengthBoundViolation` (`:80-82` — *"the FaultMeta RecoveryHint concedes 'or remove the length bound if runtime validation is sufficient'"*) and `CountBoundViolation` (`:84-86` — *"the code's own comment calls the runtime trap 'a defense-in-depth backstop, not the boundary enforcer'"*), with a red-team caveat that **mincount is proof-carrying** (it discharges the count>0 access-safety obligation) while **maxcount is not**.

**The registry correction** — `:210`:

> REMOVE from the [StaticallyPreventable] registry and the bijective core: **OutOfRange (13), LengthBoundViolation (14), CountBoundViolation (15) — no can't-compute referent**; their compile checks survive under definition-incoherence or flag identities.

**Method behind it, per its own claims** (`:4`, `:20`, `:136`, `:255`): 10 agents — two independent fault adjudications, red-team, convergence critic, 10+ live probes. On this verdict specifically: two independent adjudications converged, the red-team **CONFIRMED** the misclassification, the critic found no reversals, and the doc self-calibrates it **HIGH** — its top tier. Corroborated by `evaluator.md`'s impossible-path table, which `:136` notes "lists exactly the 9 true-fault/TC-owned codes and **omitting all five contested ones** — independent convergent evidence the design always knew the boundary the registry blurred."

---

## 3. Provenance of the fault gate — it is the origin, not a drift

The phrasing *"at every fault-prone operation"* first appears in `docs/language/precept-language-spec.md:266`:

```
5af46537  sfalik  2026-06-02
docs(spec): land the compile-time/runtime guarantee contract (Phase 1)
  Settles the long-standing ambiguity over what is guaranteed at compile time
  vs enforced at runtime — so it isn't re-debated.
```

Verbatim:

> **Fault prevention — established entirely at compile time.** A precept that compiles without diagnostics cannot produce a runtime fault — no division by zero, no overflow, no empty-collection access, **no result outside a declared bound**. The compiler delivers it by discharging, **at every fault-prone operation**, an obligation that each operand *carries* a sufficient constraint… (Principles 7, 10, 11.)

**"Fault-prone operation" and "no result outside a declared bound" are in the same sentence.** There is no earlier, wider formulation for it to have narrowed from. In this corpus "fault-prone operation" functions as a **site predicate** (where obligations get stamped); the four-item list is the **kind enumeration** (what must hold there). Containment is item four.

**This sentence is the whole dispute in miniature: three faults and a containment claim, under a heading that says "Fault."** `fault-floor-definition:199-201` named it, at the time, as the artifact carrying the error:

> **Spec/canon edits baking the misclassification into the guarantee contract** — **Current:** Spec §0.7's fault-prevention sentence includes 'no result outside a declared bound'; **Principle 11 lists 'constraint range impossibility' as a fault class**… **Why: the reclassification cannot be canon while the spec asserts the opposite.**

### The rider never falls off

Every later document using the fault-gated shorthand covers containment explicitly nearby. This was checked exhaustively:

- **`posture-v2-support/hybrid-model-draft-A-frank-subagent-2026-07-14.md:58`** — the line that prompted this analysis. Four lines later, `:62`: *"**A fault-prone site is any place a computed value could fault or land outside a declared limit.**"* Then `:67` lists declared-bound containment and `:68` cardinality/length containment as enforced-set members. **The term is defined to include containment.**
- **`hybrid-model-draft-D-four-dispositions-2026-07-14.md:28`** — the clearest refutation of the gate reading in the corpus: *"The obligation covers **every declared bound or rule constraining that value**, together with the fault-freedom of the expression that produces it. **Faults are one enumerable family within this scope, alongside bound containment**… **The scope is set by the value being computed.**"*
- Draft A2, Draft B (`:68`), Draft C (`:183`), both posture replays, and the blind reconstruction all carry containment explicitly.

**No post-07-06 document occupies the narrow pole.**

### The narrowing that *did* happen — June, and it was argued

`fault-floor-definition-2026-06-11` applied a genuine new test and removed three codes. `compiler-readiness-plan-2026-06-11:35` adopted it; it hardened into D2's band split. The retrospective names this (`frank-retrospective-proof-engine-arc-2026-07-13.md:52`):

> The pendulum's low point is the **06-16 compiler-readiness plan**… **That is fault-floor-only**: prevention moved off compile-time rejection for any band nothing downstream leans on.

and diagnoses the cause: *"prove-or-reject over the weak engine had just proven **unbuildable-feeling** on the corpus's own house style (214 unbounded money fields…), so the posture itself came under suspicion."*

**The 07-06 ruling re-widened, restoring containment.** That is the light the owner recalls seeing on 07-06 — the ruling is what put containment back.

### Owner-attributed evidence runs *toward* narrow, not away

- `d2-proven-violation-philosophy-analysis-2026-07-03.md:19`: *"The owner **'suspected the engine had over-stepped the floor (band-containment rejections that are really rule-preservation)'** and commissioned an exhaustive floor definition."* — **owner pushback toward NARROW.**
- `c27a382b` (owner-shipped, 2026-06-03): count-bound enforcement, Reading A, prove-or-reject — on `maxcount`, which the narrow reading excludes. **Owner behavior toward WIDE.**
- `ruling:43` — the only verbatim owner quote on this axis, and it is about *deferral*, not scope: *"consider if you took 'no deferrals' too far — I meant no deferrals as a safeguard to prevent agents from deferring scope, I didn't mean it explicitly in the context of compiler deferring to runtime."* The ruling's own accounting: **"Net effect on the verdict: none."**

**No owner-attributed text asks to narrow prove-or-reject off containment.** If that pushback happened in conversation it is invisible to a document trace. **This is the primary falsifier for §3.**

---

## 4. The ruling never engaged the research

Greps over `docs/Working/proof-engine-boundary-ruling-2026-07-06.md` (359 lines):

| term | hits |
|---|---|
| `fault-floor` | **0** |
| `misclassif` | **0** |
| `can't-compute` / `can-it-compute` | **0** |
| `registry` | **0** |
| `2026-06-11` / `2026-06-10` | **0** |
| `StaticallyPreventable` | 3 — **all about `NumericOverflow`'s D1 park; none about `OutOfRange`** |

A document that re-derives the entire fault boundary from scratch, supersedes five documents, and carries a §2 "intellectual honesty section" auditing its own v1 for two pillars' worth of error — **never names the research that adjudicated its central category, never runs its test, never acknowledges that its "fault set" contains three codes the project's own highest-confidence research had instructed be removed from the fault registry five days earlier.**

It did not engage and lose. **It did not engage.**

**And its grounding is circular.** `ruling:130` cites `spec:266` and `spec:208` for family B's membership. `spec:266` is the sentence *headed* "Fault prevention" that enumerates "no result outside a declared bound" — i.e. **the ruling proves containment is a fault by citing the sentence the research identified as where the misclassification got written down.** Silently, because it never tells the reader the sentence is contested.

The rigor is real but aimed elsewhere: the ruling exhaustively audits **`NumericOverflow`** — the one band-adjacent code the research said to **KEEP** (`:211` "RETARGET NumericOverflow (12): keep the code"). It carves out the true fault and rides the three misclassified ones through.

---

## 5. Ground (i) was argued once, and lost

**07-11 — the thesis caught the circularity** (`precept-identity-and-guarantees-thesis-2026-07-11.md:328-330`):

> *The ruling contradicts its own substrate on what a fault is.* Family B is defined over 'fault-prone operations,' but the consequence taxonomy the ruling builds on defines a fault as an unrecoverable abort and explicitly assigns anything `ConstraintsFailed` handles to governance (`fault-floor-definition:122`)… **calling its containment a 'fault-prone operation' assumes the conclusion.**

**07-11 — Frank argued ground (i) for the first and only time.** Step 3: *"a computed containment the compiler can't decide **is** an unproven fault."* Step 4: *"`FaultCode.OutOfRange = 13`, carrying `[StaticallyPreventable]`… **The language's own metadata says: an out-of-band computed value is a fault.**"*

**07-11 — Fable defeated it on the text.**
- On Step 3 (`fable:106`): *"The axis in the sentence is *typed outcome vs. abort*… Reading 'recoverable' as 'an external agent can supply a different value' is an **added** criterion the sentence does not contain. Frank charges the thesis with dropping half the sentence; **on the text, it is his reading that adds a half.**"*
- On Step 4 (`fable:126`): *"**The registry entry is the disputed artifact itself.**… Citing the current registry as evidence for its own correctness is **archaeology**."*
- On the reversal (`fable:135`): *"**One artifact's later reversal against a many-angles convergence needs the stronger argument, and the stronger argument here is not the one the reversal cited.**"*

**07-11 — Fable supplied the exit** (`fable:237(c)`), the single most clarifying sentence in the corpus:

> the fault-registry corrections (`fault-floor-definition:210`) remain correct and **orthogonal — OutOfRange exits the fault registry *even under Error*, because the compile check's identity is containment-obligation, not fault-prevention, and the runtime lane is `ConstraintsFailed`, not a trap**; Frank's Step 4 should not be read as defending the registry status quo, which the fault-floor analysis he relies on adjudicated a misclassification.

It separates two questions the ruling had welded:
- **Compile-side disposition** — reject the unprovable containment write? → **Error**.
- **Runtime fault identity** — does an out-of-band value abort with only the trap? → **No** → exits the registry.

These are independent. A compiler may reject what is not a fault — that is what **definition incoherence** already is, and `ruling:205`'s own taxonomy has that row. **The category needed to hold containment-Error-without-fault-identity already existed in the ruling's own table. Nobody put containment in it.**

**Nobody answered `:237(c)`.** No rebuttal exists anywhere. The ledger has zero hits for `registry`.

---

## 6. The undeclared migration to ground (ii)

**07-12 — the owner's ratification contains no fault claim** (`proof-engine-decision-ledger-2026-07-12.md:25`):

> **✅ RULING (Shane, 2026-07-12): A — prove-or-reject, RATIFIED.** Justification: **prove-or-reject is part of Precept's unique value proposition and will genuinely push authors to build better precepts, in line with the philosophy.**

Not can't-compute. Not `philosophy:57`. Not P10 or P11. **A product ground.**

**07-12 — the fold-in is origin-based** (`ledger:24`): *"raw input is governed at the door; **anything computed from it is proven**."* The word "fault" does not appear in the boundary statement.

**07-12 — ledger #6 changes the premise under a clerical label** (`ledger:61`):

> classify bounded-write checks as **containment obligations** with a three-way verdict — **proven** / **proven-violating** (rejected, with a witness configuration) / **unresolved** (rejected, with the weakest precondition printed verbatim). **The unresolved bucket blocks because the definition left a reachable case with no authored disposition**, not because 'it might be broken.' *Text catching up to #1.*

Three independent tells that this is ground (ii), not (i):
1. **The name.** "Containment obligation" is a category *coordinate with* faults, not a member of them. Family B had one obligation kind ("fault-prone operations"); this is a second.
2. **The justification.** *"under **prevention (Principle 1)**"* — not P10 (Totality) or P11 (Static completeness), the two that enumerate faults. The ruling grounded containment on `:266`/P11; #6 re-grounds it on P1 and never mentions a fault.
3. **The verdict shape.** A *witness configuration* is a value that exists and violates — which presupposes the value computes, which is exactly why containment fails the can't-compute test. A fault obligation has no witness.

**"Text catching up to #1" is not housekeeping. It is a change of premise executed under a clerical label** — and it is the move that saves the conclusion. Ground (ii) is *immune* to the fault-floor research; ground (i) is refuted by it. Neither the ledger nor any later doc registers that the ground shifted.

**It landed in canon.** `precept-language-spec.md:223` now reads:

> **Bounded-write checks are different — they are containment *obligations*, not reports**, and carry a three-way verdict… Both non-proven verdicts **block**: **under prevention (Principle 1)**, a reachable write the definition left with no authored disposition is a definition defect, not a 'might be broken' warning.

**The live spec stands on ground (ii). Only the unratified ruling still stands on ground (i)** — self-labeled *"Draft — pending Shane ratification. Not Locked"* with an entirely unchecked ratification checklist (`ruling:307-320`).

### The registry trim was reversed on a named fallacy

Not in the ledger. In a scoping-table cell — `compiler-readiness-plan-2026-07-12-pipeline-evaluation.md:27`:

> | **Slice 1.4 — honest registry** | **Re-judge under three-way: bound violations *are* now statically prevented (rejected).** | **refactor** |

"We reject it, therefore it is a statically-preventable fault." That conflates **compile-side disposition** with **runtime fault identity** — which is exactly and only what `fable:237(c)` was written to prevent, **by name, twelve days earlier**. A three-derivation, red-teamed, HIGH-confidence verdict was overturned in a table cell by a fallacy that had already been refuted in advance. Nobody noticed, because nobody was answering `:237(c)` anymore.

---

## 7. The two philosophy commitments

- **`philosophy.md:51`** — *"every rule declared on the entity's data is enforced on every operation… **No operation can produce a result that violates a declared rule — the invalid configuration is not reachable.**"* — A **containment** claim. Enumerates nothing. Universal over declared rules. **Never asks whether anything faulted.**
- **`philosophy.md:53`** — *"**The same absoluteness applies to** every calculation… The compiler does not trust that an expression will **succeed** — it proves it will. **Division by zero, arithmetic overflow, empty collection access** — these are… compile-time impossibilities."* — A **fault** claim.

**Different subjects** (a result that is *wrong* vs. a calculation that *doesn't succeed*). **Different enumerations** (none vs. three true faults). **And the text marks its own seam**: `:53` opens *"The same absoluteness applies to…"* — the idiom of extending a commitment to a **new domain**. Two paragraphs, two guarantees, joined by an analogy of *force*, not of *kind*.

`set x = 40` into `max 10` **produces a result that violates a declared rule**. `:51` is violated. Nothing faulted.

**A fault gate reaches only `:53`. `:51` demands containment; `:53` does not supply it. That gap is `:51`-shaped, and it is exactly the hole ground (ii) fills.**

*(`:51` could in principle be satisfied by runtime governance instead — the thesis's position, "prevention relocated, not weakened." But `philosophy.md:55` scopes governance to discharging a precondition the compiler **already proved**, and a computed value has no ingress slot. Fable's counter is the commit sweep — see [`third-arm-forensic-analysis-2026-07-15.md`](third-arm-forensic-analysis-2026-07-15.md), which is where that thread continues.)*

**Note P10 and P11 are not symmetric.** P10 (`spec:110`) is purely fault-scoped and does **not** mention containment. P11 (`spec:112`) lists **"constraint range impossibility"** among fault classes — the phrase `fault-floor-definition:200` flagged for amendment. **Ledger #7 declined to amend it** (`:62`: *"Principle 11 stays un-reworded"*) — but ruled on **overflow** grounds, about a code the research **agreed** was a true fault. **The containment half of P11 rode through #7 unexamined, protected by a ruling aimed at a different code.** Third time in this corpus containment survives by proximity to overflow.

---

## 8. Prove-or-reject vs. the fault floor — the delta

**Comparison baseline:** the readiness plan's **10-family floor** (`compiler-readiness-plan-2026-06-11.md:59` — "Was '11-obligation-family' — corrected to 10 by the overflow-park decision"), the floor's terminal form and plan of record. *(Two enumerated floors exist, not three: `fault-floor-definition:124`'s 11 families, and this 10. The 06-10 packet's "Layer 1" is a breach-closure list, not a family enumeration, and is silent on band containment.)*

### Identical

Division by zero · sqrt/pow domain · empty-collection access · empty-collection mutation · index bounds · free-standing `rule` (both roles) · nonlinear/relational `rule` (governed) · `ensure` (governed) · foldable defaults (Error, definition incoherence) · unfoldable defaults (clean, construction sweep) · **representational overflow (parked on both sides)**.

**Nothing in the classical fault surface moved.**

### Reversed — the entire delta

| | Fault floor | Prove-or-reject |
|---|---|---|
| **Declared-bound containment** | `:76`, `:210` — remove from registry; `:177` — proven violations → warning, unprovable → govern | `ruling:25` — prove-or-reject, **Error**, incl. merely-unprovable |
| **String-length containment** | `:82` — *"Length bounds discharge NO fault obligation… exit the floor with **no carries-proof residue**"*; `:181` — unprovable → **no compile diagnostic** | `draft-A:68`, `ruling:225` — Error, *"Already shipping"* |
| **Count containment (maxcount)** | `:185` — *"proven violations → warning, unprovable → **silent**"* | `ruling:163` — Error, *"Owner-ratified scope"* |
| *(mincount)* | Stays — proof-carrying, discharges count>0 access safety | Same |

Plus three consequences of that answer: **the merely-unprovable case blocks** (vs. *"silence beyond the fault floor promises nothing"*, `packet:117`); **structural severity flips to Error** (`ledger:109`) vs. warning-with-earned-promotion; and the **certificate criterion** (`ledger:34`) — orthogonal and genuinely new, replacing the floor-era solver ban.

### Pointing the other way — prove-or-reject may be *weaker*

- **Presence** (floor family 3) and **qualifier/dimensional compatibility** (floor family 8) are enumerated floor families and appear in **no** prove-or-reject enforced-set enumeration. `draft-A:70` disclaims its own list and defers to `docs/compiler/soundness-and-coverage.md` §3.1 as *"the authoritative boundary"* — **which was not read**. **UNCERTAIN. This is the largest coverage gap in this analysis and should be closed before any ruling.**
- **Position totality** (`fault-floor-definition:126` — obligations created at *every* expression position of the 5 expression-bearing DUs; 10 fail-open positions named) and **write-aware tier-2 discharge** (`:128`, the red-team amendment) are named floor minimums, absent from the entire prove-or-reject corpus.
- **Representational overflow** is a floor family under `fault-floor-definition:124` and parked under prove-or-reject — so prove-or-reject is strictly weaker than the **11**-family floor here (though identical to the 10-family plan of record).

### The delta under the fault-gated reading

Subtract containment/length/maxcount; the severity and verdict consequences lose most of their subject matter. Residual: the certificate criterion, engine scope, the closed escape hatch, the structural-severity flips — against a proven set of div/sqrt/empty/index, overflow parked, presence and qualifier unenumerated.

**That is the readiness plan's floor minus two families. Under the narrow reading, the answer is: yes, a round trip — and slightly weaker than the start.**

**This is why the fossil is load-bearing and not cosmetic.** The band lane — the whole delta — survives **only** on ground (ii). Any reader who re-derives the fault gate from `ruling:130`, `spec:266`'s heading, or P11's "constraint range impossibility" will correctly conclude containment is not a fault, drop it from family B, and collapse the posture back to the fault floor. **The gate is a live trapdoor under the one thing that makes the current posture different from where the project started.**

### One genuine crack, conceded by both sides

`draft-A:86`: *"**Decidability shapes which obligations exist, not just how they discharge.** … If a field's only bound is an undecidable constraint… then a computed write into that field generates **no containment obligation on that axis**."* The thesis caught it first (`:340`): *"'routing by origin cannot slide' is slightly stronger than the model delivers."* So a computed value under a nonlinear-only bound is neither proven nor rejected — a narrow-reading-shaped hole *inside* the wide reading. It does not swallow the delta (decidable modifiers are where bounds actually live), but it is not nothing.

---

## 9. What the month bought — honest accounting

| Item | Verdict |
|---|---|
| **Certificate criterion** | **Genuinely new** — and not a guarantee about programs; a guarantee about *proofs* (legible & re-checkable / performant / right-sized / earns its place, `ledger:34`), replacing the spec's outright solver ban. No analog in the floor corpus, whose posture is the ban itself. Frank calls it *"the biggest single deposit of the whole detour"*; on the evidence, agreed. |
| **§1b-is-never-expressibility** | **Genuinely new** — *"the combination of linear inequalities is itself a linear inequality the author can write as one rule"*, which is what makes deferring §1b principled rather than cowardly. |
| **Band-lane reinstatement** | **The actual delta** (§8). |
| **constraint = rule correction** | **New framing that produced the delta.** It killed D2 and therefore reinstated containment. |
| **Obligation-Role Rule** | **New framing**, with a real simplification: it deletes the carries-proof partition (`ruling:193` — *"the category never existed"*). No new guarantee; less machinery. |
| **Three-way verdict** | **New framing + a thin new guarantee.** Reject-the-unprovable already shipped (`c27a382b`); new are the named third bucket, the printed weakest precondition, and the `spec:223` rescope that makes it non-contradictory. |
| **Decidability as oracle-not-router** | **New framing, over-claimed** — conceded down within days (`draft-A:86`). |
| **D1 overflow park** | **Neither** — a subtraction, made *inside* the fault-floor corpus before the arc, inherited unchanged. |
| **Closing the escape hatch** | **Self-generated.** The ruling opened it (R4); the ledger closed it. Net vs. June: unchanged. |

**What the retrospective's own "what's new" list omits: the band-lane reinstatement** — the one item that separates the two postures. Frank's frame treats prove-or-reject as baseline and the band split as deviation, so it reads to him as a correction rather than a gain. From the floor-as-start frame, it is the *only* thing that answers the question.

**And Frank concedes the rest** (`retrospective:81`, `:111`):

> A disciplined engineering read of that amendment yields **~80% of the ratified conclusion**… it was a **spiral, not a straight line**, and **most of its length was avoidable**.

> when an engineering boundary gets hard, the reflex here is to question the product's identity instead of strengthening the engine… **It cost roughly a month.**

Both are compatible and both appear true on the evidence: **the destination moved; the route was several times longer than the move required.**

---

## 10. Corrections to claims made earlier in this session

Recorded because the same failure mode is what this analysis is about.

- **"The fault gate crept in later."** False. It is the original spec language, owner-committed 2026-06-02, in the same sentence as the containment clause.
- **"`draft-A:58` drops containment."** False. `:62` defines "fault-prone site" to include *"or land outside a declared limit"*; `:67-68` enumerate containment inside the enforced set. **The rider never falls off in any document.**
- **"`field x max 10` + `set x = arg.y` (unbounded) → rejected."** False. `ruling:153` row 4 routes it **governed, clean** — `arg.y` is a raw pass-through, not derived. Row 5 is sharper: `v` declared `in 0..1000` into `max 100` is a *statically knowable* violation for `v ∈ (100,1000]` and is **still not proven** — *"it is not re-proven because `v` is raw, not derived."* **The router is derivation, not computation.** `set x = arg.y * 2` is the reject case.
- **"Rules are governed."** True of the design; **not true of HEAD** — see the probe findings in the session record: free-standing rules generate zero obligations at HEAD, and the runtime that would govern them is a stub.

---

## 11. What is open

1. **The fossil.** `spec:266`'s "Fault prevention" heading over a non-fault list; P11's "constraint range impossibility"; `ruling:130`'s fault gate. Ground (i) is refuted and abandoned but still quotable in three places. **Deletion, not decision** — the same shape as the §0.7/§3A.4 fix. Until it goes, every future draft can re-derive the gate faithfully and collapse the band lane.
2. **Fault identity for `OutOfRange` / `LengthBoundViolation` / `CountBoundViolation`.** Answered NO at the corpus's highest confidence; never answered back; reversed in a table cell on a named fallacy. **The disposition is unaffected** (containment writes reject on ground (ii) regardless) — what is unresolved is registry membership and the `[StaticallyPreventable]` claim.
3. **Presence and qualifier.** Floor families absent from every prove-or-reject enumeration. `docs/compiler/soundness-and-coverage.md` §3.1 is the named authority and was not read. **Close this before ruling.**
4. **Position totality and write-aware tier-2 discharge.** Named floor minimums, absent from the prove-or-reject corpus.
5. **The undecidable-bound hole** (`draft-A:86`): a computed value whose only bound is undecidable generates no containment obligation — neither proven nor rejected.

---

## 12. Confidence and falsifiers

**HIGH** — the ruling never engages the fault-floor research (7/7 greps zero; the three `StaticallyPreventable` hits are all `NumericOverflow`).
**HIGH** — containment fails the `:122` test on both prongs.
**HIGH** — `fable:237(c)` was never answered; the ledger contains no registry decision.
**HIGH** — ledger #6 re-grounds on Principle 1, not on any fault principle; the shift is undeclared.
**HIGH** — the chronology and the spec origin (verbatim text + verified owner commit).
**HIGH** — no post-07-06 document drops containment.
**HIGH** — the §8 delta (directly opposed verbatim instructions about the same three diagnostic codes).
**MEDIUM-HIGH** — that ground (i) was never argued before 07-11. Rests on greps plus a read of the ruling's derivation.
**MEDIUM-LOW** — §3's claim that no owner pushback toward NARROW exists. Absence of documented pushback is not proof it didn't happen; conversational pushback is invisible to a document trace.
**MEDIUM** — the floor-stronger direction (§8) — `soundness-and-coverage.md` §3.1 unread.

**Would overturn the core finding:**
1. A pre-07-06 document applying the can't-compute test to containment and concluding it **passes** — then the ruling inherited a contested verdict rather than ignoring one, and "asserted" is too harsh.
2. Any document answering `:237(c)` on the merits — arguing the registry question is *not* orthogonal, or that compile-side Error entails fault identity. One would make `pipeline-evaluation:27` grounded rather than fallacious.
3. An owner statement that "fault" is *defined* by `[StaticallyPreventable]` membership rather than by the can't-compute test — then `:122` is a research proposal the owner declined, and `ruling:130` is consistent. **Note `:122` is itself an agent artifact and binds nobody; the strength of this analysis comes from the corpus's own migration to ground (ii), not from `:122`'s authority.**
4. An owner statement that the 07-12 ratification of "A" was made on the *narrow* reading of "fault" — then the ratification and the ruling it ratified disagree, and the wide reading loses its only owner anchor. The fold-in is explicit against this (`ledger:24`: *"anything computed from it is proven"* — no fault gate in the sentence).
5. `DesugarsToRule` acquiring consumers, or a runtime decision making bound violations non-recoverable. `fault-floor-definition:237` names this as its own falsifier: *"The runtime shipping with a fault-delivery contract that makes band violations non-recoverable (**would re-open the band adjudication**)."* That would vindicate `ruling:130` retroactively, on prong (b).
6. `soundness-and-coverage.md` §3.1 enumerating presence, qualifier, position-totality and write-aware discharge as live obligations → §8's floor-stronger items dissolve and the delta is cleanly one-directional (verdict unchanged, strengthened).
