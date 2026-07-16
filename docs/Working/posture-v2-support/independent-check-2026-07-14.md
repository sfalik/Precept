---
title: "Independent Verification Check — Frank's Go-Forward Posture Replay v2"
status: Draft — 2026-07-14 (verification pass against five input sources)
author: George (Runtime Dev)
phase: Phase 5 — Independent Verification
input-sources:
  - docs/Working/posture-v2-support/citation-audit-2026-07-14.md (Fact Checker — 29 ✅, 2 ⚠️)
  - docs/Working/posture-v2-support/coverage-matrix-2026-07-14.md (Soup Nazi — 107 items)
  - docs/Working/posture-v2-support/devils-advocate-2026-07-14.md (DA mode — 5 openings)
  - docs/Working/posture-v2-support/frank-self-critique-2026-07-14.md (Frank — H1–H4, M1–M3)
  - docs/Working/posture-v2-support/blind-reconstruction-2026-07-14.md (George)
artifact-under-review: docs/Working/frank-go-forward-posture-replay-2026-07-14-v2.md
changelog-in-v2: Appendix C1–C17
---

# Independent Verification Check — Posture Replay v2

*Verification pass by George. Methodology: read v2 in full including its C1–C17 changelog
appendix, then check each input source item against v2's actual text — not against changelog
claims. A changelog claim is not automatically verified; what matters is whether the text changed.*

---

## 1. Citation Audit — Verdict: FULLY ADDRESSED

Both ⚠️ findings from the citation audit are fixed in v2.

**⚠️ #1 (Construction-input citation, Fire-only):**
v2 §5, Mechanism 1(a): *"construction inputs specifically: `precept-language-spec.md:268`,
`runtime-api.md:162`/`:180` — citation fix, audit ⚠️ #1."* The fix is in the text, not just
the changelog. The explicit parenthetical citation of `:162`/`:180` alongside `spec:268` is
present. ✅

**⚠️ #2 ("through genuine debate and reversal" as overquote):**
v2 §11: *"(v1 quoted 'through genuine debate and reversal' as if verbatim — that phrase is an
inference, not a source quote; corrected per audit ⚠️ #2. The retrospective supports the swing
at `:81`/`:109`, not that exact wording.)"* De-quoted in the text; line citations `:81`/`:109`
supplied. ✅

All three strengthening opportunities that were labeled optional in the audit (use `spec:1969`
for the ingress/sweep split; use `compiler-and-runtime-design.md`/`proof-engine.md` for
philosophical claims; use the capture doc) are also present in v2: `spec:1969` appears in §5
for the sweep; `compiler-and-runtime-design.md` appears in §1 and §3; `proof-engine.md:2609`
appears in §3; capture doc `:56` appears in §3. ✅

**One minor residual:** The "no GOVERNED without real enforcement" claim in §6 cites
`critique-c3-philosophy-reconciliation.md` F9 rather than a canonical anchor. The audit called
for a canonical anchor here. Non-blocking — the reference is traceable — but noted.

---

## 2. Coverage Matrix — Verdict: SUBSTANTIALLY ADDRESSED (minor items remain by design)

### The 5 contradicted items

All 5 contradicted items from v1 are handled correctly in v2:

| Contradicted item | How v2 handles it | Status |
|---|---|---|
| Boundary R4 (trust construct re-opened) | Escape hatch CLOSED per ledger #9; band-suppression tradeoff explicitly recorded (§9) | ✅ |
| Identity Thesis Rec A (govern non-proof-carrying bands) | Explicitly banned in §10 as "the thesis's counter-position" and "'Govern, don't prove' for derived values" | ✅ |
| Critique C3-F1 (substrate supports governing non-proof-carrying bands) | Ratified prove-or-reject maintained; the counter-position named and foreclosed | ✅ |
| Critique C3-F2 (Error-is-only-interim split) | No-interim posture maintained; §9 explicitly covers deferred items; §12 notes Forcing §4.5 as superseded | ✅ |
| Forcing §4.5 (replace spec:256 with full linear entailment) | v2 §12: "Forcing §4.5 (replace spec:256 with full linear entailment) is **contradicted** by the ratified §1b deferral (§9); named as superseded, not carried." Explicit acknowledgment in text. | ✅ |

### Sample-check of critical/significant dropped items

Checking all items from the coverage matrix's "Critical" priority tier plus a representative
sample from "Significant":

**Critical-tier drops — all restored in v2:**

- **Ledger #6 (spec:223 rescope / three-way verdict):** v2 §7 has a full section. Three-way
  `ProofVerdict` DU (Proven/ProvenViolating(witness)/Unresolved(condition)), spec:223 rescoped
  to violation *reports*, WP verbatim on Unresolved. Text matches the ruling, not just the
  changelog. ✅ (C2)

- **Boundary Q9 (per-obligation PROVEN/GOVERNED, no DEFERRED state):** v2 §7: "Per-obligation
  disposition surface — PROVEN vs GOVERNED, no DEFERRED state." DEFERRED explicitly absent.
  ✅ (C2)

- **Boundary Q11 / worked row 14 (count/cardinality family):** v2 §3 adds: "cardinality /
  count-bound containment — a derived count against `mincount`/`maxcount`/`minlength`/`maxlength`
  (`CountBoundViolation`/`LengthBoundViolation`, `boundary-ruling:25`, worked row 14; spec:258)."
  Count "Reading A" cited. ✅ (C1)

- **Ledger #10 + Stage 0b F5 (dead-row severity / structural severity flips):** v2 §7:
  "proven-always-violating derived write on a reachable row is `ProvenViolating` and is a hard
  Error"; six structural-severity Warning→Error flips named and re-affirmed; F5 dead-end
  disposition = Error. ✅ (C3)

- **Retrospective §4/§5 (anti-spiral process lessons / P0s / parking lines):** v2 §10 bans
  "engineering-wall-as-identity-crisis" and "one more matcher form" explicitly; v2 §11 states
  the three build-sequencing conditions (fix the two P0s, treat Slice 0 like 2c-ii, hold parking
  lines). ✅ (C11/C12)

**Significant-tier drops — representative sample:**

- **Ledger #4 / #5 (aggregates / compute-then-check):** v2 §9 explicitly names both as deferred
  items with pointers; compute-then-check notes computed fields are NOT a full substitute
  (event-arg derivation scope). ✅ (C14)

- **Critique C2-F1..F5 (false-security surface constraints):** v2 §7 "False-security constraint
  on that surface" adds three standing cautions: inline/ambient rendering (not query-only),
  no-bundling, disclosure-first. Marked as "ratified-but-unvalidated legibility hypothesis
  routed to `/design`." ✅ (C13)

- **Critique C3-F4 (number-model blocker under overflow):** v2 §2 parenthetical: "The
  number-model prerequisite under overflow... is a real build blocker, not just a text
  question." ✅ (C8)

- **Stage 0b Decisions A/B/C:** v2 §7 names Decision A (ProofVerdict DU), Decision B (certificate
  format only, no live re-checker), Decision C (CertificateSteps catalog, not ProofStrategy
  enum). ✅ (C2)

- **Critique C3-F9 (no GOVERNED without enforcement):** v2 §6: "A disposition of 'governed' is
  honest only where the sweep/ingress actually enforces the constraint... Where the enforcement
  is not yet built, the honest label is 'designed,' not 'governed'." ✅ (C6)

- **Old gates and build mechanics:** All correctly named in v2 §12 as "deliberately out of
  scope," with pointers to where they live (decision ledger, readiness plan). Not silently
  dropped — explicitly held. ✅ (C15)

### Items remaining dropped or partially addressed after v2

The following items are still absent or incomplete after v2 — all are low severity:

1. **L2 (final-value vs. intermediate containment semantic):** Frank's self-critique listed this
   as LOW ("one sentence in §4"). The spec defines the containment obligation as binding the
   *committed* working-copy value, not every intermediate write (`spec:1354`/`:1967`). v2 §5
   describes the sweep as checking "after the mutation applies" but never states explicitly that
   a transiently out-of-band intermediate commits clean if the final value satisfies the bound.
   **Gap:** one sentence in §5 or §7 would close it — e.g., "The containment obligation binds
   the committed value, not intermediates; `set X = -50 → set X = 100` commits clean if X's
   final value satisfies its bound, consistent with the spec's defined commit semantics
   (`spec:1354`)."

2. **C2-F3 (walk the `min 5` case through the author-visible disposition):** v2 §7 adds the
   false-security section but does not include a concrete walkthrough of what a domain expert
   sees when they write `min 5` on an unbounded field — before and after the proposed inline
   disposition rendering. The audit called for this as a grounding check on the reclassification.
   The gap is legibility/validation evidence, not correctness.

3. **C2-F4 (per-expression / per-literal marker for out-of-fragment residual risk):** v2 §7
   marks the disposition surface as an "unvalidated legibility hypothesis" but does not
   specifically name the per-expression/per-literal marker question for values that fall outside
   every decidable fragment and are inspector-only. The critique asked for either a marker
   proposal or explicit naming of this as an accepted open residual.

4. **DA banned-framing gap #4 ("flatten history into a circle"):** v2 §11 addresses this
   substantively ("Not a circle — a spiral that deposited the governor"), but the §10 banned-
   framings list does not include an explicit "❌ 'We always knew this' / 'nothing materially
   changed'" item. The concern is addressed in prose but not foreclosed by name in the formal
   ban list. Minor.

5. **Ledger #8 "already applied" claim:** v2 §12 states: "Ledger #8 (tighten the one
   `philosophy.md:49` 'wrong answer' clause) is **already applied** in canon
   (`frank-canonical-capture-recommendation-2026-07-14.md`); I do not re-open it." I have not
   independently verified this against the capture doc. If that application is not actually
   present in the capture doc, this would be an incorrect dismissal of a live item.
   **Recommendation:** verify the capture doc contains the ledger #8 edit before signing off.

Items 1–4 above are **non-blocking** — they are legibility/completeness gaps, not correctness
gaps in the posture. Item 5 is a verification gap that should be confirmed before sign-off.

---

## 3. Devil's Advocate — Verdict: ALL 5 OPENINGS CLOSED

Each DA opening was tested against v2's actual text:

**DA #1 (overflow carve-out: deliberate tension unnamed):**
v2 §2: "Name the tension, because it is deliberate and the corpus does not unanimously resolve
it (DA #1, #4)." Two facts stated simultaneously: parking + owner-ruled target-language. Closes
with: "Anyone citing canon to say 'overflow is inside the live guarantee' is reading target-
language as live-language, which #7 forbids." The tension is named as **deliberate owner policy**
over the boundary ruling's reconciliation recommendation. ✅ **CLOSED**

**DA #2 (routing-axis: proof-carrying and obligation-role rivals unnamed):**
v2 §4 opens: "This is the single biggest re-litigation opening in v1 (DA #2)." Uses the formal
Obligation-Role Rule frame. Then explicitly: the proof-carrying/non-proof-carrying framing
"was the fence around the historically-contested subset... never the model's router" — named
and rejected. Three-route alternative: "two live routes, one of them two-mechanism, plus one
out-of-axis disclosed park." Spelling ban preserved. Rival readings named and killed in text,
not just implied. ✅ **CLOSED**

**DA #3 (§1b: open-vs-rejected ambiguity):**
v2 §9: "Two dimensions at once: it is **open in the design space**... **and closed for the
current authorization set**." Explicit; both halves present in the same sentence. ✅ **CLOSED**

**DA #4 ("build-order, not text defect" as owner policy vs. consensus):**
v2 §2: "I record the disagreement as decided, not as absent... over the boundary ruling's own
recommendation to surface/reconcile the text... and over the identity critique that flagged `:53`
as an unresolved present-tense overclaim." Explicitly says "owner policy choice, made *over*
[both sources]." ✅ **CLOSED**

**DA #5 (two-mechanism claim: restored/out-of-contract runtime not scoped):**
v2 §5 opens: "Scope this claim explicitly (DA #5, self-critique H2): the two mechanisms below
govern the **contract path — mutations entering or produced by declared operations**. They are
**not** the whole runtime behavior surface." Restored state and evaluator traps named as "a
**third runtime zone, not a hidden third governance mechanism.**" ✅ **CLOSED**

**DA additional: Banned-framings process errors (5 shapes):**
- Engineering wall → identity crisis: ✅ banned in §10 explicitly
- Prove-or-reject honest on old engine: ✅ banned in §10 explicitly
- D2 under new vocabulary: ✅ banned in §10 explicitly
- "Nothing changed / always knew this": addressed in §11 but ⚠️ not explicitly banned in §10
- Skip engineering-diagnosis artifact: ✅ §10 bans it, §11 names the countermeasure

4 of 5 explicitly in §10's ban list; 1 addressed in §11 prose. Non-blocking.

---

## 4. Frank's Self-Critique (H1–H4, M1–M3) — Verdict: FULLY ADDRESSED (M3 correctly surfaced)

### H1 — "SAME guarantee" overstatement
v2 §6 is a new dedicated section titled "The guarantee, precisely — outcome identical, epistemics
honestly different." The split is explicit: "(a) Outcome guarantee — identical" / "(b) Epistemic
guarantee — honestly different, by design." The Event-B trade is named. "No GOVERNED without
real enforcement" is present. This section also strengthens the anti-Option-B case by making
the distinction load-bearing: governed relational invariants are *not proven at compile time at
all* — precisely why deriving a computed value and wanting runtime amnesty for it fails.
**H1: FULLY RESOLVED.** ✅

### H2 — "Two distinct mechanisms" framing vs. canonical §0.7
v2 §5 scopes the claim to the contract path explicitly. Reconciles with §0.7: the ingress/sweep
split is presented as "an elaboration of how the govern route is delivered," not as a new
competing "two mechanisms" claim. The `:264` "precise rather than magical" citation is no longer
attached to the ingress/sweep split — it's dropped from that context. The §0.7 doc-sync
question is correctly surfaced as an owner-gated open question in §13.
**H2: FULLY RESOLVED.** ✅

### H3 — `set`/`rule` asymmetry and evadability never engaged
v2 §4: "The `set` vs. `rule` question, met head-on (self-critique H3; Fable Claim 9)."
Full three-point answer present in text: (1) provenance/role difference — `set X = e` computes
X, `rule X == e` constrains independently-set X (row 11 reasoning cited); (2) the "evasion"
changes the definition's meaning, not laundering a computed value into a governed one;
(3) concedes the legibility cost and places it on the diagnostic surface + rejection message.
**H3: FULLY RESOLVED.** ✅

### H4 — Band-suppression tradeoff invisible, §6 presented as complete answer
v2 §9, under escape-hatch closure: "The accepted tradeoff, recorded (self-critique H4; `fable-
analysis` Claim 8)." Explicitly names the band-suppression/constraint-deletion dynamic. States:
"§6 does not neutralize band-suppression — §6 lets the engine honor a fact the author *states*;
the band-suppression author has no dischargeable fact short of a real carrier or bounded operand."
Ends: "Naming it is what stops the next person from 'rediscovering' it and reopening govern-by-
default." Text matches; not just changelog.
**H4: FULLY RESOLVED.** ✅

### M1 — Certificate criterion underweighted to a single clause
v2 §8 is a new dedicated section: "The certificate criterion, and the principled stopping rule."
All four admissibility legs stated. "Clause 4 is the principled stopping rule the June effort
lacked." Three governors named together (§1b-proof, clause 4, §6). The "honest only as the
package" condition in §3. "Without these three, this is a research project whose compiler grows
forever; with them, it is a bounded build."
**M1: FULLY RESOLVED.** ✅

### M2 — False-security caveat absent
v2 §7, "The false-security constraint on that surface — an open acceptance criterion, not a
solved problem." Names all three standing cautions. Explicitly states: "I mark this a
**ratified-but-unvalidated legibility hypothesis routed to `/design`** — **not** a closed win."
**M2: FULLY RESOLVED.** ✅

### M3 — Drift with canonical-capture doc (provenance vs. proof-carrying)
v2 §4: "Self-correction on the record: my own `frank-canonical-capture-recommendation-2026-07-14.md`
§1a still calls the boundary 'proof-carrying vs. not'... a **drift I am flagging against my own
doc** (self-critique M3); the ratified router is provenance, and the capture doc must be
corrected to say so before anything is promoted."

v2 §13 open question #2 explicitly surfaces this for Shane's confirmation: "Confirm you want
that correction, and I'll make it in the appropriate phase."

The capture doc itself is **not corrected in v2** — this is correct scoping (phase boundary). The
flagging is present in text, not just the changelog.

**M3: CORRECTLY SURFACED AS AN OWNER-GATED ACTION. Not resolved in v2 because it requires
Shane's confirmation before Frank touches the capture doc.** ✅ (appropriate handling)

**Residual risk of M3:** If Shane's sign-off on v2 does not explicitly confirm M3's correction,
and the capture doc's Step-1 promotion runs before the fix, `soundness-and-coverage.md` will
inherit "the boundary is proof-carrying vs. not" as a canonical framing — which the boundary
ruling explicitly overruled. This is the **most consequential remaining item** even though it is
correctly scoped out of v2.

---

## 5. My Own Blind Reconstruction — Verdict: FULLY COVERED (with improvements)

Checking v2 against the reconstruction's checklist:

| Reconstruction item | v2 coverage | Status |
|---|---|---|
| Hybrid model, total over fault surface | §3 | ✅ |
| `philosophy.md:57` guarantee (both voices) | §2 | ✅ |
| D1 overflow: owner-parked, disclosed, no live guarantee, no doc may claim prevented | §2 | ✅ |
| `OutOfRange` ≠ `NumericOverflow` (different faults) | §2 | ✅ |
| Live-enforced fault surface (all 5 including count family) | §3 | ✅ |
| Routing by provenance, not decidability or spelling | §4 | ✅ |
| Constraint spelling dispositionally inert | §4 | ✅ |
| Decidability is discharge oracle, not router | §4 | ✅ |
| Governance is not deferral; spec:270 Composition | §5 | ✅ |
| Two governance mechanisms (now scoped to contract path) | §5 | ✅ |
| §1b deferred not rejected; two-dimension clarity | §9 | ✅ |
| No escape hatch; band-suppression tradeoff named | §9, §10 | ✅ |
| D2 dissolved | §10 | ✅ |
| Three-way verdict / spec:223 rescope | §7 | ✅ |
| Six structural-severity Warning→Error flips | §7 | ✅ |
| Certificate criterion as first-class posture element | §8 | ✅ |
| "Honest only as the package" condition | §3 | ✅ |
| Principled stopping rule (three governors) | §8, §11 | ✅ |
| ProofVerdict DU (Stage 0b Decision A) | §7 | ✅ |
| Linear relational invariants governed by provenance, not linearity | §3 | ✅ |
| False-security constraint on disposition surface | §7 | ✅ |

v2 also covers items that were **not** in my reconstruction but belong in the posture:

- Explicitly scoping two-mechanism claim to contract path (and naming evaluator traps as third
  zone) — this was DA #5; my reconstruction mentioned both mechanisms but didn't scope them
- Epistemic-vs-outcome guarantee split (H1) — my reconstruction had "governance is not deferral"
  but not the Event-B trade explicitly named as a design choice
- The `set`/`rule` evadability addressed head-on — I noted this as a gap Frank needed to fill;
  it is now filled
- Band-suppression tradeoff explicitly named — I covered the accepted tradeoff from the ledger
  but not the specific band-suppression dynamic

**My reconstruction provided a valid independent checklist; v2 passes all items and adds
substantive content beyond what I covered.**

---

## 6. Overall Verdict

**v2 is ready for Shane's sign-off, with one action item to confirm before promoting the
capture doc.**

### What "ready" means here

The posture is correct, complete on all load-bearing items, and explicitly closes every
re-litigation opening identified by the five input sources. All five DA openings are
foreclosed by name in the text. All four HIGH-severity self-critique findings are resolved in
the text. The critical and significant coverage gaps are either restored or explicitly named
as out-of-scope in §12. Both citation audit ⚠️ findings are corrected in the text. The
document is substantially stronger than v1 — it is the first version that can plausibly be
called "load-bearing enough to stop the loop."

### Remaining gaps (all minor, non-blocking on posture correctness)

| # | Gap | Severity | Resolution path |
|---|---|---|---|
| G1 | **M3 capture-doc correction awaits Shane's confirmation** (§13 open question #2). If the Step-1 promotion runs without the fix, "proof-carrying vs. not" gets canonized as the router. | **Most significant remaining item** | Shane confirms M3 fix in §13 response → Frank corrects capture doc in appropriate phase |
| G2 | **L2: final-value vs. intermediate containment semantic unnamed.** One sentence in §5 or §7 would close ("The obligation binds the committed value, not intermediates — `spec:1354`"). | Minor | Frank adds one sentence to §5 or §7 in a future pass |
| G3 | **§0.7 doc-sync open (§13 question #1).** Canon's Governance paragraph is ingress-only; the sweep is real and governs results but isn't in §0.7. Named as open; awaits owner ruling. | Correctly scoped as owner-gated | Shane rules §13 question #1 |
| G4 | **Ledger #8 "already applied" claim unverified** against the capture doc. v2 §12 says it's done; not confirmed here. | Low | Verify the capture doc contains the philosophy.md:49 "wrong answer" clause edit |
| G5 | **C2-F3 (concrete min-5 walkthrough absent)** from disposition-surface legibility. | Low, legibility only | Frank adds worked example in the `/design` pass for the disposition surface (Thesis Rec G) |
| G6 | **DA banned-framing #4 ("flatten-to-circle") not in the §10 ban list.** Addressed in §11 prose but not formally foreclosed. | Minimal | Add one line to §10 in a future pass |

G1 is the only item that, if not acted on, carries a real risk of harm (canonical framing drift).
G2–G6 are precision debts that do not affect the posture's correctness.

### Sign-off recommendation

**APPROVE v2 with the following explicit conditions:**

1. **Shane confirms M3 (§13 question #2):** Confirm the capture doc's "proof-carrying vs. not"
   framing must be corrected to "provenance" before its Step-1 promotion runs. Frank needs an
   explicit "yes, make that correction" to proceed.

2. **Shane rules §13 questions 1, 3, 4** (§0.7 doc-sync; disposition-surface hypothesis routing;
   certificate-format/OQ1 ruling timing). These are owner-gated; they are correctly flagged and
   held.

3. **Verify ledger #8 is actually applied in the capture doc** before treating it as closed in
   §12.

The posture document itself does not need further revision before sign-off — conditions 1–3 are
actions on related artifacts, not changes to v2's text. Frank may address G2 (L2) and G6
(DA #4) in a future pass but they are not blocking.

---

*End of verification check — George, 2026-07-14.*
