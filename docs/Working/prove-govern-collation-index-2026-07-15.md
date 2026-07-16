# Collation index — the compile-time proof / runtime governance boundary

**status:** Draft collation — 2026-07-15. Source map only. Makes no rulings, settles no forks, proposes no design. Input to a new canonical draft.

> **⚠️ SUPERSEDED IN PART — read the two forensic analyses first:**
> - [`third-arm-forensic-analysis-2026-07-15.md`](third-arm-forensic-analysis-2026-07-15.md) — the ingress/sweep question. Root cause: spec `§0.7:268` and `§3A.4:1969` contradict each other, committed eleven minutes apart on 2026-06-02.
> - [`prove-or-reject-scope-forensic-2026-07-15.md`](prove-or-reject-scope-forensic-2026-07-15.md) — the fault-gate question. Root cause: `spec:266` heads a **containment** claim under a **"Fault prevention"** heading; the corpus migrated from the fault ground to the computed ground on 2026-07-12 and left the fault ground standing, still quotable.
>
> Both findings have the same shape: **canon asserts two incompatible things, so every downstream draft is faithful to one half.** Neither is a decision the owner failed to make; both are contradictions nobody surfaced.
>
> Two claims below are now known to be wrong, and are corrected in place:
> - **F1 was framed as "owner vs. corpus."** It is not. It is **canon vs. canon**: spec `§0.7:268` (governance = external input at ingress, no sweep) and spec `§3A.4:1969` ("the sweep is one of two enforcement points") contradict each other, and were committed **eleven minutes apart on 2026-06-02** (`5af46537`, then `63e08963`). Every draft since has been faithful to one half of a self-contradicting spec. The owner was detecting a real contradiction, not disagreeing with a settled corpus.
> - **The "21 of 77 sample files" figure does not measure what it carries.** Its method, stated in the ruling at `:102`, is `grep -l '\*' samples/*.precept | wc -l` — a count of files containing an **asterisk character**. Every downstream escalation to "depend on it" / "would be rejected definitions" is unsupported by that measurement. Do not cite this number.

**Method.** Three independent agents read disjoint slices of the corpus in full: (1) `posture-v2-support/` + the draft lineage, (2) the ruling + thesis lineage, (3) the retrospective + replay + the canonical landing zone. Each was told to cite `file:line` for every claim, to write UNCERTAIN rather than guess, and — critically — **not to flatten a two-sided disagreement into "resolved" because a later doc asserts it was**. Each was also told that agent-authored labels (`Locked`, `RULED`, `owner decision`) are artifacts of this project's own agents and do not bind the owner. `docs/philosophy.md` is the one genuine owner-locked anchor.

---

## 0. Read this first — what the collation actually found

**The boundary is not settled, and that is why it keeps reopening.** The corpus contains a ruling, a thesis, an adjudication, a ledger of owner rulings, and five drafts. Underneath them sit **five load-bearing questions that were closed by assertion rather than by argument**, plus **one fork between the owner and the corpus that no document addresses**. A new document that restates the ruling will be the fifth artifact to lose the same argument.

**Two files were deleted this session and are unrecoverable.** `docs/hybrid-model.md` and `docs/Working/frank-canonical-capture-recommendation-2026-07-14.md` were untracked, never committed on any branch — no stash, no dangling blob, no backup. `hybrid-model.md`'s content survives as the A/A2 snapshots in `posture-v2-support/`. The capture recommendation survives only as quotes inside other docs; the posture replay v2 cites it in five now-dangling places (`:69`, `:89`, `:101`, `:125`, `:219`).

**Roughly 60% of the new document is already written, uncommitted, in the working tree** — and the two main files contradict each other on the central axis. See §4.

**A prompt-injection artifact was observed.** One agent reported a file-read tool result returning an injected `## Exited Plan Mode` block that came from neither the operator nor the harness. It disregarded it and made no edits. Source unlocated; if it is text sitting in a repo doc, it is an injection vector in our own corpus.

---

## 1. The diagnosis — why six weeks

Two passages explain the recurrence better than anything else in the corpus. They belong at the front of the new document.

**The effort axis** — `band-guarantee-boundary-analysis-2026-07-03.md:33-42`:

> The three poles are arranged along an *effort* axis — how much the compiler tries to prove. An effort axis has no natural stopping point… The escape is to stop defining the boundary by effort and define it instead by a property that has a hard mathematical edge.

**The sharpest single finding in the corpus** — `band-guarantee-boundary-analysis-2026-07-03.md:199`:

> **partial static *coverage* is not partial *guarantee*.** … False security comes from a *fuzzy* boundary… never from a *partial* one.

And `:85-89`: "neither non-trivial pole eliminates false security… The distinguishing property is not which pole has no residual risk — neither does — but which one's residual risk is *legible*."

**The reflex** — `frank-retrospective-proof-engine-arc-2026-07-13.md:111`:

> when a slice hits a wall, the first move must be a *disciplined engineering diagnosis of the wall*… **not** a re-opening of 'what should Precept be.'… **when an engineering boundary gets hard, the reflex here is to question the product's identity instead of strengthening the engine or handing the author a rule to state.** It cost roughly a month.

And `:109`: "it was a **spiral around an engineering boundary the team mistook for an identity crisis**."

**No stopping rule** — `frank-retrospective-proof-engine-arc-2026-07-13.md:121`:

> The June work spiraled precisely because there was no principled 'we do not build this' — every unprovable case generated pressure to either strengthen the engine again or re-litigate the posture, and **nothing said *stop*.**

**The unanimous convergence.** Across the niche packet's red-team, both 07-03 analyses, the ruling, the thesis, Frank, Fable, and the forcing analysis, exactly one thing is agreed by every party: **the guarantee's boundary must be legible per-obligation, and that legibility is a deliverable, not a caveat.** That is the strongest candidate for the new document's spine.

---

## 2. Source map

### 2.1 The ruling + thesis lineage

| Doc | Declared status | What it is |
|---|---|---|
| `compile-time-niche-decision-packet-2026-06-10.md` | Decision packet — NOT a decision | Verdict "partial"; source of the **flag-soundness contract** (`:117`) |
| `compile-time-niche-evidence-inventory-2026-06-10.md` | Evidence appendix | Defect-class inventory, 6 vantages |
| `compile-time-niche-evidence-legs-2026-06-10.md` | Evidence appendix | Five legs: scenarios, cost, external, UX, demand |
| `band-guarantee-boundary-analysis-2026-07-03.md` | Draft analysis; D2 **NOT** owner-ratified | **The effort-axis diagnosis.** Highest-value doc in the lineage |
| `d2-proven-violation-philosophy-analysis-2026-07-03.md` | Draft analysis; "D2 owner-locked" | Scoped by a label that was **wrong** — see §3 rot |
| `proof-engine-boundary-ruling-2026-07-06-v1-superseded.md` | Draft, pending ratification | Hybrid + four-leg ingress-origin predicate |
| `proof-engine-boundary-ruling-2026-07-06.md` | Draft Rev 3, pending ratification | **THE ruling.** Obligation-Role Rule; overflow carved out |
| `prove-govern-classification-and-legibility-2026-07-06.md` | **`Locked 2026-07-06`** | Superseded same-day; carries a dead classifier — worst rot |
| `precept-identity-and-guarantees-thesis-2026-07-11.md` | Draft, pending owner review | **THE thesis.** Independent second opinion; ADJUST-then-ratify |
| `frank-prove-or-reject-position-2026-07-11.md` | Draft position paper — Not Locked | Argues Error on one cell |
| `fable-analysis-of-frank-response-2026-07-11.md` | Draft | Adjudication; **changes position** |
| `prove-or-reject-forcing-and-expressiveness-2026-07-12.md` | Draft, not owner-ratified | **The three-way-split correction.** Dissolves the flagship example |
| `proof-engine-decision-ledger-2026-07-12.md` | `Ruled — 2026-07-12` | The 10 owner-attributed rulings |
| `decision-index.md` | Working index (names the **06-16** plan) | Pointer index; **D2 row is dead but presented as settled** |

### 2.2 The draft lineage (`posture-v2-support/`)

**Lineage, established from `hybrid-model-draft-discards-2026-07-14.md:4` + line-count arithmetic:**

- **A (453 lines) → A2 (568 lines)** — the same document, refined in place. A2 adds examples G/H/I/J and renames the banned-arm example G→K. Driver: owner clarification that "Deposits/Withdrawals is one example among equals," not the centerpiece (`discards:26`). **This file is the one that was deleted.**
- **B (426)** and **C (283)** — *independent parallel* drafts for owner comparison. Neither descends from the other; no cross-reference either way.
- **D (161)** — descends from the *critique of C*, not from C. Frank's four-disposition analysis (`frank-four-disposition-structural-analysis-2026-07-14.md:70`) recommended "restore the four dispositions"; D executes that as a fresh doc.

**Which is winning: none.** A2 is deleted. D — the only draft claiming promotion candidacy — **failed adversarial review with 8 surviving blockers** (`hybrid-model-draft-D-adversarial-review-2026-07-14.md:94`), two of which are *fabricated sample citations* (B6, B8). B and C were never reviewed for promotion; C was found structurally insufficient by two independent passes. No file records an owner ruling on any draft.

### 2.3 The replay / reconciliation layer

- `frank-go-forward-posture-replay-2026-07-14.md` (v1, 88 lines) — posture *assertion*.
- `frank-go-forward-posture-replay-2026-07-14-v2.md` (v2, 274 lines) — posture *defense*. Synthesizes five review inputs; 18-row C1–C18 changelog; §11 anti-spiral; §13 open questions. **The most complete single artifact in the corpus.**
- `v2-completion-reconciliation-2026-07-14.md` — probe-arbitrated. Finding: **0 of 45 ledger items are false positives** (`:137`). Also: "**The v2 *plan* delivered essentially no code**" (`:14`); the real completed body is the pre-plan build-out of ~05-23→06-05 (`:16`).
- `soundness-hole-slice0-fixes-2026-07-14.md` — tactical, self-declared transient.

---

## 3. Open forks — nothing here is settled

Ordered by how much each blocks the new draft.

### F1 — Ingress-only, or ingress + post-mutation sweep? **(canon vs. canon; blocks everything)**

**CORRECTED 2026-07-15 — see `third-arm-forensic-analysis-2026-07-15.md`.** This was originally written as "owner vs. corpus." That framing was wrong and is the reason the fork looked unresolvable.

- **The spec contradicts itself.** `§0.7:268` (`5af46537`, 2026-06-02): "Governance — enforced at runtime **on external input**… at the moment it enters, **before any computation derives from it**." The word "sweep" appears nowhere in §0.7. `§3A.4:1969` (`63e08963`, **same day, +11 minutes**): "**This post-mutation sweep is one of two enforcement points**… the sweep governs *the result*." Both live; both byte-identical at `f1f3a319`.
- **Consequence.** An author reading §0.7 writes two arms and is faithful to canon. An author reading §3A.4 writes three and is equally faithful. Both pass "does this match the corpus." The owner correctly detects a smuggled arm in every draft and is correctly told it matches the corpus. **It always did — just not all of it.**
- **The finding that explains six weeks** (`third-arm-section2-check-2026-07-14.md:409`): "The 'third arm' is not an authoring error in v2 relative to the corpus — **it is the corpus**." True, and incomplete: the corpus also contains its negation.
- **The cost figure is unusable.** The "21 of 77" claim traces to `grep -l '\*' samples/*.precept | wc -l` (`ruling:102`) — files containing an asterisk. An independent count of actual multi-field relational rules returned ~53/77 files (132/149 rules), self-flagged as approximate. **Neither number is trustworthy enough to rule on.** Measure before ruling if cost is load-bearing.
- **Nothing is built.** `Evaluator.cs` is a stub (all five ops `NotImplementedException`); `ConstraintsFailed` is declared in three files and constructed nowhere. Build-cost is not an input either way.

**The ruling collapses to one question on two sentences in one file:** §0.7 or §3A.4 — which is right? Whichever wins, the other sentence is deleted. That deletion is the anti-recurrence mechanism: it removes the half of canon that lets the next draft contradict the ruling while remaining "faithful to the corpus."

### F2 — How broadly is the "no third arm" ban scoped? *(F1 in different clothes)*

Narrow (v2 §10 as written): the ban covers **derived values** — so relational invariants over independently-set fields aren't reached. Broad (owner's apparent intent): any govern-instead-of-prove path is the banned arm. `third-arm-section2-check:186`.

### F3 — Ledger #1's justification is conditional on package items ledger #3/#5 declined **(sharpest internal inconsistency)**

Ledger #1 ratifies prove-or-reject citing "will genuinely push authors to build better precepts" (`proof-engine-decision-ledger-2026-07-12.md:25`). That verdict's own source states it is **conditional on a four-decision package plus one construct**: `prove-or-reject-forcing-and-expressiveness-2026-07-12.md:273` — "**Ratifying prove-or-reject *without* the package means fighting the identity battle over an artificially weak engine — which no side of the debate should accept**." And `:249`: "ratifying the posture over today's interval-plus-depth-1 engine would reject the corpus's best idioms and would deserve the thesis's over-rejection critique."

Ledger **#3 does not authorize the `spec:256` override** (`:51`) and **#5 defers the compute-then-check construct** (`:60`) — which the source calls "the posture's named ergonomic dependency" and states is "**load-bearing for the Q1 verdict**: the probes' 'forces a better precept' conclusions silently assume it away" (`forcing:187`).

**The verdict ledger #1 cites as its justification is a verdict the source conditions on a construct ledger #5 defers.**

### F4 — Ledger #9 closed the escape hatch that both parties made a co-requisite

Frank (`frank-prove-or-reject-position-2026-07-11.md:190`): "There is exactly one argument that moves me to GOVERN for this case… **an explicit, author-visible, opt-in language construct**… Under that construct… I would ratify it."

Fable (`fable-analysis-of-frank-response-2026-07-11.md:233-235`): ratify Error "**conditional on the opt-in-governance construct being routed to `/design` in the same motion**… the disputed case's disposition and the construct are one design decision, not two."

Ledger #9 closes it (`:64`) citing the forcing analysis's no-exception-class finding. That engages the *exception-class* argument. It does **not** engage Fable's actual reason — the **constraint-deletion dynamic** (`fable:151`): "an author who wants to *document* 'Balance shouldn't go negative' but cannot prove it will often not invent bounds — they **delete the band**, losing the proof *and* the governance *and* the documentation… for a domain-integrity product the perverse outcome: fewer declared truths."

### F5 — The routing axis: provenance or proof-carrying? **(live contradiction in uncommitted canon)**

- Ratified router (`ruling:132`): **provenance**. "Routing by effort would make every newly-hard case a deferral candidate… routing by origin cannot slide."
- Ruling Q8 (`:192-193`) **dissolves** the proof-carrying category entirely: "the category never existed once constraint = rule is taken seriously."
- But the thesis and *both* position papers keep proof-carrying as the only boundary that matters — and the "proof-carrying exception" both papers agreed on **is not addressed anywhere in the ledger**.
- **And the uncommitted working tree contradicts itself** — see §4.

### F6 — Two dispositions or four?

Three same-day framings, zero reconciliation (`hybrid-model-draft-D-adversarial-review-2026-07-14.md:72`). A/A2/B/C say two (`draft-A:207`). D + Frank's analysis say four — but `frank-four-disposition-structural-analysis:57`'s claim that "**the ratified corpus established four dispositions**" is **uncited and contradicted twice**. The posture replay v2 says neither: "two active routes, one of them two-mechanism, plus one out-of-axis disclosed park. **That is the exact count.**"

D's attempt to settle this failed review — but on *execution* (uncited contradictions, fabricated examples), not on "four is wrong."

### F7 — The defaults hole

Two independent reviews agree the two-bucket taxonomy **fails on the base case**. `field X as number nonzero default 12` "matches neither of §2.1's three supplied origins nor its three computed forms… **The partition fails on the base case**" (`hybrid-draft-c-devils-advocate-matrix:46-49`).

The decisive variant is a **wrong answer**, not a gap (`frank-four-disposition-structural-analysis:35-41`): with `default 1` against a rule requiring ≥10, "**every `Create` fails at runtime and the entity can never be instantiated, yet the definition compiles.**"

**Scope of this is itself disputed.** Frank: "the model is sound; Draft C's expression of it is insufficient… bounded work" (`:82`). The DA matrix: "**the defaults hole is systemic**" — destabilizes §2.1, §2.4, §3, and §5.1 (`:327`). Unaddressed anywhere: **defaulted event arguments** (Case 20, `:277`).

### F8 — `set X = e` vs. `rule X == e`: provenance, or evadable hypocrisy?

Fable's charge, never defeated (`fable:158`): the same unprovable derived write is "rejected if the target's constraint is a decidable numeric bound; silently governed if it is an undecidable rule; and governed if it is the same predicate written as a state-scoped `ensure`… Worse, it is *evadable*: the author who hits the rejection can demote the invariant to a scoped ensure or rewrite the rule into a form the compiler cannot analyze — **escaping the error by making the definition strictly *less* analyzable.**"

v2 answers it (`:96`): the evasion "**does not launder a computed value into a governed one** — it *redesigns the entity*." Marked resolved by the independent check. But Frank's own ranking calls this "**the most probable re-entry point**" (`frank-self-critique:138`), and the third leg of v2's answer is a concession, not a rebuttal. The answer is a *definitional* move.

### F9 — Band-suppression: overridden, not solved

Fable made the opt-in construct a co-requisite specifically to prevent this. Owner overrode; v2 records the tradeoff (`:176`). But recording ≠ solving: "**§6 does not neutralize band-suppression**" (`independent-check:244`). Named by the corpus as the likeliest thing to be "rediscovered."

### F10 — Overflow: philosophy asserts what isn't built *(decided; two-sided; must be reproduced, not resolved)*

`philosophy.md:53` — "Division by zero, **arithmetic overflow**, empty collection access — these are not risks managed at runtime. They are **compile-time impossibilities**." Same present-tense claim in spec P10 (`:110`) and P11 (`:112`).

The ruling parks overflow (D1): "no live compile-time guarantee"; `decision-index:19` — "**no doc may claim it prevented**."

Ledger #7 rules **leave the text as-is** — "Overflow-prevention **is** the intended end-state… the gap is build-order, not a text defect" (`:62`), and "**Principle 11 stays un-reworded**." This directly contradicts `decision-index:19` and overrides both Frank's R6 and thesis Rec D.

v2's handling is the model to copy (`:38`): "Two source-level facts are simultaneously true and must be stated together." **The new document must reproduce the two-sided statement or it will silently re-import overflow into the enforced guarantee.**

### F11 — Authority inflation in the ledger

The owner ruled on "reject vs. govern." Ledger `:24` folds the ruling's *entire architecture* into #1 as "converged, no separate debate needed" — Hybrid, the origin boundary, Reading N, Q9, GATE-H, D1 — **none individually surfaced for a ruling**. The thesis flags the principle correctly (`thesis:322`): "Independent convergence is genuine argument-strength evidence… It is **not** ratification."

**The new document should re-surface the folded items explicitly rather than inherit them.**

### F12 — Unclosed semantics nobody has addressed

- **PRE0078 per-write vs. per-plan** (`fable:98`): the check "proves a property **stronger than the constraint's defined meaning** — the constraint binds the final copy, not every intermediate assignment." Nothing in the ledger addresses it.
- **`OutOfRange` exits the fault registry even under Error** (`fable:237(c)`) — ledger #6 points the same way but never says the registry moves; `ruling:205` still lists `OutOfRange` in the fault floor.
- **BUG-020 may be a ledger omission** (`v2-completion-reconciliation:152`).
- **BUG-025 / BUG-026 severity** remain owner calls (`:160`).

---

## 4. The working tree — ~60% already written, and self-contradictory

| File | Diff | Contains |
|---|---|---|
| `docs/compiler-and-runtime-design.md` | +71/−4 | New §1.1/§1.2. **The keystone narrative.** The "closed net, not a floor" frame; the 6-step walk; the `ProofVerdict` DU; "Why prove-or-reject, and not a fault-floor" |
| `docs/compiler/soundness-and-coverage.md` | +235/−23 | Part A/Part B restructure. §1.1 rationale; §3 coverage matrix (16 `FaultCode` rows + 12 site rows); **§3.4 the bucket test** |
| `docs/compiler/proof-engine.md` | +2/−0 | "Why this deferral is principled, not cowardly" |
| `docs/README.md`, `docs/compiler/README.md` | +1/−1 each | Routing sync; designates architecture-doc §1 as the keystone |

**🚩 The two main files contradict each other on the router:**

- `compiler-and-runtime-design.md`: routing is "decided by **provenance**, not by whether the rule involves a fault"
- `soundness-and-coverage.md` §1.1: "**Why the boundary is proof-carrying-vs-not, never band-vs-rule.**… **The only line that matters** is whether an operand *carries a constraint the engine can discharge*"

Proof-carrying is the framing the boundary ruling **overruled as the router**. Posture v2 §13.2 (`:229`) asks the owner to authorize correcting the capture doc *before* its Step-1 promotion put exactly this into `soundness-and-coverage.md`: "**or it will canonize an overruled framing**." The promotion appears to have executed anyway; the capture doc was then deleted.

**Fix this before either file is committed**, or the new document lands on top of a self-contradiction — which is the precise mechanism the whole corpus blames for the six weeks.

### The emerging canonical structure (inferable from the diffs)

| Doc | Role |
|---|---|
| `philosophy.md` | Identity — LOCKED, not re-litigated |
| `precept-language-spec.md` §0.7 | The guarantee **statement** (author-facing) |
| `compiler-and-runtime-design.md` §1/§1.1/§1.2 | **The keystone** — guarantee + mechanism walk |
| `soundness-and-coverage.md` §1.1 | The **rationale** (why this shape) |
| `soundness-and-coverage.md` §3 | The **measurable boundary** + bucket test |
| `proof-engine.md` | Per-strategy detail + the certificate criterion |

---

## 5. Canonical anchors — verbatim

`docs/philosophy.md` is unmodified in the working tree; these line numbers are stable.

- **`:49`** — "**Prevention, not detection.** No errors. No bugs. Business logic cannot produce an answer that violates a declared rule."
- **`:51`** — "No operation can produce a result that violates a declared rule — the invalid configuration is not reachable."
- **`:53`** — the overflow overclaim. See F10.
- **`:55`** — **the composition seam**, the load-bearing sentence for the whole split: "The compiler proves a structural fact — that the value carries its constraint — never the value itself; the runtime enforcement is what makes that carried constraint true, **not a second line of defense**."
- **`:57`** — "A definition that compiles without diagnostics has no unproven evaluation faults… **not what bugs you try to catch, but what bugs cannot exist.**"
- **`:61`** — "**Compile-time structural checking.**" *(A distinct commitment from `:49`.)*

**⚠️ `:55` is being over-cited.** Replay v1 (`:49`, `:59`) quotes it to bless governance *generally, including the sweep*. It is specifically about the **composition seam** — an *external* value carrying a constraint for a *downstream fault proof*. It does not speak to whole-entity invariants swept post-mutation. v2 correctly narrows it (`:110`). **The new doc must not reuse v1's stretch.**

### The numbered principles are in the SPEC, not philosophy

`docs/philosophy.md` has **no numbered principles** — it is prose bullets. P1–P11 live in `docs/language/precept-language-spec.md` §0.1 (`:92-112`):

- **P6** (`:102`) = "**Explicit domain meaning over primitive convenience**" — *nothing to do with the compile/runtime split.*
- **P7** (`:104`) = "Compile-time-first static checking… **does not guess**."
- **P10** (`:110`) = "Totality… **Runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable.**"
- **P11** (`:112`) = "Static completeness… **This is the bridge between the compiler and the evaluator: the compiler's job is to make every evaluator error path unreachable.**"

**⚠️ A live mislabel to stop propagating.** `prove-govern-classification-and-legibility-2026-07-06.md:55` labels its P6 row "Governance not validation" — contradicting `spec:102`, and contradicting its own header's claim that "§0.1 is the authority" (`:46`). Its P7 and P10 rows are mislabelled too. **The widespread "P6 = governance, runtime" framing appears to originate here and is wrong.** If the new doc uses P-numbers, it must use §0.1's numbering.

**Possible spec bug (UNCERTAIN):** spec §0.7's Governance paragraph (`:268`) cites "(Principles 1, 6.)" — but P6 is explicit-domain-meaning. Looks like a stale cross-reference, and may be the true origin of the mislabel.

### `spec` §0.7 — the Guarantee Contract the new doc must land against

- `:264` — "Precept's guarantees are delivered by **two distinct mechanisms; keeping them distinct is what makes the guarantee precise rather than magical.**"
- `:266` — "**Fault prevention — established entirely at compile time.**… It **never compiles a fault-prone operation in the hope a runtime check catches it; there is no deferral.** (Principles 7, 10, 11.)"
- `:268` — "**Governance — enforced at runtime on external input.**… at the moment it enters, before any computation derives from it… **this is prevention, not detection.**"
- `:272` — the canonical statement of proof-gap-is-a-bug: "**a fault that fires for contract data is a compiler defect, not an accommodated condition.**"

**Open (v2 §13.1):** §0.7's Governance paragraph describes governance as **ingress only**. The runtime also enforces whole-entity invariants via the sweep (`spec:1969`). Sync §0.7, or leave the sweep in §3A.4? Owner-gated spec surface. **This is F1 showing up as a doc-sync question.**

---

## 6. Reusable prose register

Ranked. Everything below is lift-ready with the caveats noted.

1. **`compiler-and-runtime-design.md` §1 (uncommitted) — "closed net, not a floor."** The best framing in the corpus. "A floor is open above itself… it *fails open by default*, because **the absence of a check is indistinguishable from a passing check**." Pair with its `saas-trial-to-paid.precept` example — a rule with **no fault in it at all** — and: "'Positive monthly fee' and 'no divide-by-zero' are not two different kinds of guarantee to Precept; they are the same closed net over two regions."
2. **`ruling:64-67` — the two-things-called-runtime-checking distinction.** The definition the document needs; short; survived both owner corrections. Governance ≠ deferral.
3. **`band-guarantee-boundary-analysis:33-42` + `:199`** — the effort-axis diagnosis and "partial coverage is not partial guarantee." If the doc is meant to *end* the argument, this explains what the argument **was**.
4. **`ruling:125-134` — the Obligation-Role Rule + the one-line boundary.** ⚠️ Lift **with** Frank's own accepted concession folded in (`frank:180`, `thesis:340`): an undecidable rule contributes no constraint range, so decidability still shapes *which obligations exist*. "Routing by origin cannot slide" is slightly stronger than the model delivers.
5. **`forcing:67-69` + `:82` — the three-way-split correction.** Given full linear reasoning, the debate's flagship example (`set Balance = Deposits − Withdrawals` into `min 0`) "**is *not in the cell at all***." Any doc that settles this boundary without this correction will re-litigate it.
6. **`fable:219` — the failure-mode asymmetry.** The argument that actually decided the crux, stated by the party who **changed position because of it**. Most credible passage in the corpus for the Error side.
7. **`retrospective:111` + `:121`** — the reflex, and "nothing said *stop*" + the three governors. The structural fix.
8. **`draft-A:40` / `draft-A2:40` — prevention has exactly two homes.** Derives the two-region shape from philosophy rather than asserting it: "the rejected definition never builds an engine, and the refused mutation never commits."
9. **`draft-B:26-30` — why a hybrid; both pure alternatives refuted.** The strongest *argumentative* passage, and the thing A/A2 lack: "A domain integrity engine that refuses `rule Width * Height <= BedArea` because interval arithmetic cannot bound a product is not honest rigor; it is expressiveness amputated to flatter the prover." **Directly answers F1's expressiveness cost.**
10. **`draft-A:150-158` — outcome vs. epistemics + the anti-euphemism rule.** "The proven path tells you *this can never go wrong*; the governed path tells you *this will never be allowed to go wrong*." Then: "**there is no governed disposition without real enforcement machinery behind it**… If no machinery enforces it, it is not governed; it is a gap, and gaps are disclosed as gaps." Lift both halves — load-bearing as a pair.
11. **`draft-B:60-62` — the two wrong axes, named and refused** (difficulty, spelling). Contains the sharpest F8 answer: "Rewriting one into the other is not a rephrasing; it is a redesign of the entity."
12. **`v2:123` — outcome-vs-epistemic split incl. the Event-B concession.** "Precept **trades** deductive invariant-preservation for structural runtime enforcement." ⚠️ Its `philosophy.md:59` cite is **wrong** — correct line is `:61`.
13. **`draft-A:385-386` + `draft-B:96-100` — the addressability argument.** **Merge them**: A is more vivid ("Who receives the refusal of `Deposits - Withdrawals`?"); B is more complete (three reasons, including the invisibility argument A omits).
14. **`thesis:284-290` — the three teachable sentences** (Proven / Flagged / Governed), with its own guardrail: "Any marketing collapse of the three into one flowing guarantee is itself a false-security defect."
15. **`thesis:264` — the composition-seam uniqueness claim**, narrow version, with the Event-B concession attached. Survived an adversarial pass that killed the broad version.
16. **`soundness-and-coverage.md` §3.4 (uncommitted) — the bucket test.** Operationalizes anti-re-litigation: "A deferred cell is a ratified decision, not an oversight; re-arguing it without the trigger is the re-litigation this map exists to stop."
17. **`draft-A:427-436` — know the silhouette.** Teaches *recognition* rather than stating a rule — which is what makes a ban survive re-litigation. One test: "**ask the provenance of the value the runtime would act on.**"
18. **`frank:190` + `frank:209` — construct vs. hover-tag.** "A hover tag is not load-bearing in the source; a language construct is." Reusable even though #9 closed the construct — **it is the argument the new doc must answer if #9 stays closed.**
19. **`draft-B:281-283` — the permanent resident.** "**Governed-forever is not a queue position.** It is the correct, final answer for relationships whose subject is external data."
20. **`niche-packet:117` — the flag-soundness contract.** "a flag is always a true universal statement about the definition; silence beyond the fault floor promises nothing."

**Do NOT lift:**
- Draft D's worked examples or reference table — **fabricated / misapplied sample citations** (B6, B8).
- Draft C §5.1 "A rule is always governed" — two independent reviews found it produces a **wrong answer**.
- Replay v1's stretch of `philosophy.md:55` to bless the sweep.
- Anything citing the deleted capture doc without re-anchoring.

---

## 7. Rot register — fix or avoid

| Item | Problem |
|---|---|
| `prove-govern-classification-and-legibility-2026-07-06.md` | Status says **`Locked 2026-07-06`**; superseded the same day; carries a dead classifier that routes the disputed case **opposite** to ledger #1. The only doc in the lineage whose label asserts finality, and its content is dead. **Worst rot.** |
| `decision-index.md:20` (D2) | Presented as one of "four owner decisions… **settled** — cite them, never re-litigate" (`:9`). D2 was declined (`v1:190`), its premise dissolved (`ruling:289`), "SUPERSEDED by #1… D2 as written is dead" (`ledger:78`). Never updated. |
| `decision-index.md:3` | Names the **06-16** plan; `compiler-readiness-plan-2026-07-12.md` exists. GATE-F/G/H marked pending; the ledger folded them into #1/#7. |
| `decision-index.md:19` vs `ledger:62` | D1 says "**no doc may claim it prevented**"; #7 rules "leave the text as-is." Direct contradiction. |
| `d2-proven-violation-philosophy-analysis-2026-07-03.md` | Entire analysis **scoped by a label that was wrong** ("D2 owner-locked", `:5`/`:99`) while `band…:3` says "NOT owner-ratified." Live example of the agent-label problem. |
| `bugs.md:171` (BUG-017) | Titled a `NumericOverflow` hole; the ruling insists "BUG-017 is `OutOfRange`, not `NumericOverflow`" (`:326`) — and that distinction is what keeps it **outside** the D1 park. **If the title stands, a future reader folds a live soundness hole into the park and closes it by filing error.** |
| `ruling:351-352` | §0.1 appendix rows still describe the overflow gap as needing owner reconciliation. Ledger #7 ruled it is not a text defect. Stale. |
| `draft-A:3` / `draft-A2:3` | Declare `Status: Canonical design` **inside `docs/Working/`** — not a valid working-doc status per CLAUDE.md. |
| `v2:123`, `frank-self-critique:33` | Cite `philosophy.md:59` for "Compile-time structural checking." Correct line is **`:61`**. |
| `v2:69,:89,:101,:125,:219` | Five citations to the **deleted** capture doc. Re-anchor or drop before lifting. |
| `prove-govern…:55` | P6/P7/P10 mislabelled vs `spec` §0.1. Likely origin of the "P6 = governance" error. |

---

## 8. What the drafter needs before drafting

**Owner rulings required — the draft cannot honestly proceed without these:**

1. **F1 / F2 — the sweep.** Is the post-mutation sweep arm two of governance, or is it the banned third arm? This has been flagged three times and verified twice; each verification concluded the corpus is on the other side. Ruling it *is* the deliverable — everything else is downstream. If the owner's reading wins, the cost is real and quantified (21/77 samples) and the corpus needs re-derivation, not a new draft on top of it.
2. **F3 — the package.** #1's justification depends on #3 and #5, which are declined/deferred. Either the package is authorized, or #1's stated rationale needs replacing with one that survives without it.
3. **F4 — the co-requisite.** #9's closure doesn't engage the constraint-deletion argument that motivated the co-requisite. Answer it, or reopen the construct.
4. **F5 — the router**, and the uncommitted contradiction in §4. Fix before committing either file.

**Mechanical, no ruling needed:** the rot register (§7), the `philosophy.md:61` cite, the P-number mislabel, the dangling capture-doc citations.

**Structural obligations on the new document:**
- Reproduce F10's two-sided overflow statement. Do not resolve it into one side.
- Carry the legibility-per-obligation spine (§1) — the one thing every party agreed on.
- Answer F8 (`set`/`rule` evadability) — named as the most probable re-entry point.
- Encode a **stopping rule**. The retrospective's core finding is that "nothing said *stop*." A document without one will be re-litigated regardless of how well it argues.
- Re-surface F11's folded items rather than inheriting them by adjacency.
