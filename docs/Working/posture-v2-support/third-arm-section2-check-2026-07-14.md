---
title: "Third-Arm §3 Check — Addendum to Ingress-Scope Verification (section ref corrected)"
status: Research/verification — 2026-07-14 (read-only; no files edited; corrected: §2 → §3)
author: George (Runtime Dev)
task: >
  Shane flagged a recurring "third arm injected in section 3" (initially mis-stated as §2;
  corrected by Shane 2026-07-14T20:33). Independent line-by-line analysis of v2 §3, with
  §1/§2 retained as upstream context, cross-checked against §10's ban, and traced against
  the ingress-scope question already in progress. Determines whether this is the SAME issue
  or a distinct third recurrence.
sources-read:
  - docs/Working/frank-go-forward-posture-replay-2026-07-14-v2.md (§1–§6, §10 full text)
  - docs/Working/proof-engine-boundary-ruling-2026-07-06.md (§3, §4 rows 10–11)
  - docs/Working/proof-engine-decision-ledger-2026-07-12.md (Decision #1 text)
  - docs/compiler-and-runtime-design.md (§1.1)
  - docs/language/precept-language-spec.md §0.7 (:264–:270), §3A.4 (:1969)
depends-on: docs/Working/posture-v2-support/ingress-scope-verification-2026-07-14.md
---

# Third-Arm §3 Check — Addendum to Ingress-Scope Verification

*Read-only analysis by George. No edits to any file. Shane corrected the section reference
from §2 to §3 (2026-07-14T20:33); this document reflects that correction. The §1/§2 upstream
context is retained as it remains relevant to how the third arm propagates forward into the
guarantee claim.*

---

## Shane's Flag (original + correction)

> "honestly, i think this is the third time i've noticed a 'third arm' injected in section 2."
> *(Corrected by Shane 2026-07-14T20:33: he meant §3.)*

The banned middle/third option — "govern the undecidable case at runtime instead of prove-or-reject" (Option B) — has recurred specifically in **§3** of v2: "The governing model, and what must be proved — of what." That is the compile-time-guarantee-scope / fault-set section where the explicit claim "A declared relational invariant is GOVERNED, not proven" lives.

Shane's expectation (from the companion ingress-scope brief): the ONLY runtime-only carve-out
is **ingress** — editable fields and arguments. Everything else, including declared relational
invariants, should be **compile-time prove-or-reject**, full stop.

---

## Part 1 — Upstream Context: §1 and §2 Enable the Third Arm

*(Shane's correction: §3 is the primary locus. §1 and §2 are upstream context — they set up
the framing that makes the §3 claim operative. Both are relevant to the full picture.)*

### §1 "What Precept is" — locus of the primary third-arm sentence

> "Precept then makes every state of that data that violates those rules **structurally
> impossible to reach**: at compile time where it can prove impossibility, and **by structural
> runtime enforcement everywhere else**."

**The phrase "by structural runtime enforcement everywhere else" is the first and most direct
statement of the third arm.**

Parsed literally: where Precept *cannot prove impossibility at compile time*, it falls back to
runtime enforcement. "Everywhere else" means "everything not proven." Under this reading, there
are two tiers:

1. Compile-time proof → structural impossibility
2. Everything the compiler cannot prove → runtime enforcement

Tier 2 is the third arm, stated explicitly. It is the exact form of Option B: "govern the
undecidable case at runtime instead of proving/rejecting it."

**What §10 says this arm is called:** "❌ 'Govern, don't prove' (Option B) for derived values."

Note the scope qualifier: "for **derived values**." The §10 ban is explicitly bounded to derived
values. §1's "everywhere else" is NOT bounded to derived values. It covers the entire surface
where compile-time proof is unavailable — including relational invariants over independently-set
fields.

### §2 "What Precept guarantees" — the reinforcing claim

> "If Precept accepts a definition, every value that definition can ever hold satisfies every
> rule declared over it, **enforced structurally on every operation** — unconditionally or under
> precise guard conditions."

**"Enforced structurally on every operation" is the second statement of the same arm, now
framed as the guarantee mechanism.**

Under the corpus model (Hybrid + sweep), this is technically true: the post-mutation sweep runs
on every operation, checking all declared rules. But this phrasing imports the sweep as the
mechanism that delivers "every rule, every operation" — and the sweep IS the runtime fallback
for relational invariants that aren't proven. The phrase implies: even rules the compiler cannot
prove at compile time are enforced, because they are swept at operation time.

Under Shane's stated expectation (ingress-only), this sentence should not be true for relational
invariants that aren't proven. If such a definition is *accepted* but its invariant is only
enforced by the runtime sweep (not proven at compile time), then the guarantee is delivered by
a mechanism Shane says should not exist.

**The third-arm structure in §1–§2, stated precisely:**

| Layer | §1 claim | §2 claim | Mechanism implied |
|---|---|---|---|
| Proven at compile time | "where it can prove impossibility" | (implicit: derived-value faults) | Proof |
| Not proven at compile time | "by structural runtime enforcement everywhere else" | "enforced structurally on every operation" | Sweep (for relational invariants) |

Row 2 is the third arm. It says: what the compiler can't prove, the runtime enforces. That is
Option B, applied to relational invariants over independently-set fields.

---

## Part 2 — Line-by-Line Reading of v2 §3 (Primary Locus)

§3 is titled "The governing model, and what must be proved — of what." It is the compile-time-guarantee-scope / fault-set section. The third arm is not hidden here — it is the explicit organizing claim of the paragraph that closes the fault-surface enumeration.

**§3 line by line — the explicit third-arm paragraph:**

The paragraph begins by establishing what the compiler IS total over:
> "The compiler is **'total over the fault surface'** — *not* 'every declared constraint, full
> stop.' Inverting that — treating the compiler as total over *every* declared constraint —
> is the error to guard against."

This narrowing is load-bearing: by restricting the prove-or-reject obligation to "the fault
surface" (not all declared constraints), it creates the space for the third arm. Relational
invariants are not on the fault surface as defined, so they have no prove-or-reject obligation.
The question then becomes: what happens to them? Option A (Shane's stated intent): REJECTED.
Option B (what §3 says): GOVERNED.

The fault surface is then enumerated: division, sqrt/pow, empty/index, OutOfRange, cardinality.
Relational invariants are absent from this list.

Then comes the explicit third-arm claim:

**§3 — the sentence:**
> "**A declared relational invariant is GOVERNED, not proven** — and the routing axis is
> provenance, not linearity."

This is unambiguous: relational invariants are not proven at compile time; they are governed at
runtime. That is the definition of the third arm (govern what can't be proved, instead of
rejecting) — applied to the relational-invariant case.

**§4 (downstream) — the routing axis operationalization:**
> "Not a derived-value fault obligation → GOVERN it, structurally. Two mechanisms, both
> governance (§5): a value *supplied externally* is enforced at **ingress**; a *declared
> relational invariant over independently-set fields* is enforced by the **post-mutation sweep**."

This is in §4, not §3 — the routing operationalization follows the §3 model claim. But it names
both "govern instead of prove" mechanisms explicitly. The second bullet is the third arm:
"declared relational invariant → enforced by the post-mutation sweep." Govern, don't prove.

**§4 also explicitly tries to foreclose the "three routes" reading:**
> "❌ 'There are three routes (prove / ingress-govern / sweep-govern).' No — there are **two
> routes on the provenance axis** (prove-or-reject vs. govern); *govern* is one route **delivered
> by two mechanisms.**"

Frank is aware of Shane's concern and is defending against the "three routes" interpretation by
collapsing ingress-govern + sweep-govern into one "govern" route. But this does not change the
substance: there are still two runtime enforcement mechanisms, and the second one (the sweep) is
applied to cases where compile-time proof is unavailable — which is Option B's structure.

---

## Part 3 — Cross-Check Against §10's Explicit Ban

### §10's ban language (full):

> "❌ **'Govern, don't prove' (Option B) for derived values.** Shane ratified **A —
> prove-or-reject** and explicitly rejected Option B, *'the thesis's counter-position'*
> (`decision-ledger` #1). *(This is the error that keeps trying to come back — a 'govern the
> undecidable computed case' middle arm — banned precisely because it recurs.)*"

### The critical scope qualifier

**§10's ban is formally scoped to "for derived values."** The third-arm language in §1–§3 is
about relational invariants over **independently-set fields** (not derived values). Therefore:

**§10's ban as written does NOT technically contradict §3/§4's governance claim for relational
invariants.** The document's defense (in §4): "relational invariants are not derived values; §10's
ban doesn't apply to them."

**BUT this is exactly the question Shane is raising.** Shane believes the ban was not intended
to be limited to derived values — it was intended to be: "no runtime fallback for anything that
should be proven or rejected." In Shane's reading:
- If the compiler cannot prove a relational invariant → the definition is **REJECTED**
- The runtime is NOT the fallback for unprouvable relational invariants
- The only runtime mechanism is ingress governance for external values

Under Shane's reading, §3's "A declared relational invariant is GOVERNED, not proven" IS Option
B — applied to a different surface (independently-set fields), but structurally identical: take
what the compiler cannot prove, and govern it at runtime instead of rejecting.

**This is the core conflict.** The corpus defines the ban narrowly (derived values only). Shane
may have intended the ban broadly (any runtime fallback for things that should be proven or
rejected).

---

## Part 4 — Is This the Same Issue as the Ingress-Scope Question?

**Answer: YES. This is the same underlying design-intent conflict. It is not a distinct third
recurrence of a different error.**

The ingress-scope verification (companion doc) established:

> Shane's claim: "the ONLY runtime-only carve-out is ingress — editable fields and args."
> The corpus: the ratified design includes TWO governance mechanisms: ingress AND the
> post-mutation sweep for relational invariants.

The §2 third-arm flag is the same question, viewed from a different angle:
- Ingress-scope question: "is the sweep authorized?"
- §2 third-arm question: "does §2's guarantee depend on the sweep, and is that a third arm?"

Both questions resolve to: **does governance extend beyond ingress to a post-mutation sweep for
relational invariants over independently-set fields?**

The corpus says yes. Shane says no.

**Why Shane sees it as recurring:**
- **First occurrence:** v1 §4(b)/§5 — the original explicit third-arm language Frank's
  self-critique (H2) identified. §10 called it "the exact error the prior version committed in
  its §4(b)/§5 middle arm."
- **Second occurrence:** v1 §3 or early sections — the same claim embedded in the fault-surface
  / governing-model description.
- **Third occurrence (now, Shane's correction to §3):** v2 §3, where "A declared relational
  invariant is GOVERNED, not proven" is explicitly stated as the closing claim of the fault-
  surface enumeration. The third arm moved from §4(b)/§5 in v1 to §3 in v2 — it was not
  removed, only relocated upward in the document.

**What changed between v1 and v2:** v2 tried to address the third-arm problem by:
- Collapsing "ingress-govern + sweep-govern" into one "govern" route in §4
- Making the routing axis "provenance not three-routes"
- Explicitly rejecting the "three routes" reading in §4
- Citing §10's ban

None of that removes the substantive claim. The third arm is not in §4 (routing) in v2 — it
has been pulled upstream into §3 (governing model / fault scope), where it is now the
explicit conclusion of the fault-surface enumeration: "here is the fault surface, and here is
what falls outside it: relational invariants, which are governed." The move from §4(b)/§5
(explicit mechanism description) to §3 (model claim) made it harder to spot but did not
eliminate it.

---

## Part 5 — Exact Sentences to Name, With the Fix Shape

### Primary locus: §3 — the sentence Shane is flagging

**Sentence (exact, closing paragraph of §3's fault-surface enumeration):**
> "**A declared relational invariant is GOVERNED, not proven —** and the routing axis is
> provenance, not linearity." (`boundary-ruling:226`, worked rows 10–11)

**Structure of the third arm this creates.** §3 has just named the fault surface — the set
of things that are prove-or-reject. Relational invariants are absent from it. The sentence then
answers "what happens to them?": GOVERNED. This is the third path between prove and reject:

1. Fault-surface operation over derived value → **prove-or-reject**
2. [Shane's intent]: Relational invariant not on fault surface → **REJECT the definition**
3. [v2 §3]: Relational invariant not on fault surface → **GOVERNED at runtime**

Path 3 is the third arm. It neither proves nor rejects — it defers enforcement to runtime.

**Why §10's written ban doesn't technically cover it.** §10 says "❌ '‘Govern, don’t prove’
(Option B) for **derived values**.'" Relational invariants over independently-set fields are
not derived values. The ban as written does not extend to this surface. But if Shane's intent
was to ban any "govern instead of prove" path (not just derived values), the sentence directly
contradicts that broader intent.

**The fix if Shane's reading is correct.** Remove "is GOVERNED, not proven" and replace with
"is prove-or-reject — the compiler proves the relationship holds from the declared constraints
on the participating fields, or the definition is rejected." This collapses the provenance-vs-
derived-value routing distinction that §4 operationalizes.

### Upstream enabler: §1

**Sentence:** "by structural runtime enforcement **everywhere else**."

"Everywhere else" creates the bucket; §3 fills it with relational invariants. Fix (if Shane's
reading is correct): replace "everywhere else" with an explicit enumeration of the two
authorized enforcement points (compile-time proof; ingress governance for external values).
Relational invariants that fit neither are rejected, not placed in a third bucket.

### Reinforcing guarantee claim: §2

**Sentence:** "enforced structurally on **every operation**."

The absolute guarantee depends on the sweep to be true for relational invariants. Under Shane's
reading, definitions with unprouvable relational invariants are rejected, so the guarantee still
holds — over the narrower set of accepted definitions. The sentence itself doesn't change, but
the mechanism behind it does.

### Still-present downstream: §4

**Sentence:** "a *declared relational invariant over independently-set fields* is enforced by
the **post-mutation sweep**."

Remove if Shane's reading is correct (the sweep is not the mechanism; prove-or-reject replaces it).
---

## Part 6 — What the Fix Would Actually Require (Owner-Gated)

If Shane's ingress-only intent is the correct design, the fix is NOT purely textual. It is a
substantive design change:

**Design change required:**
1. The proof engine must treat relational invariants over independently-set fields as
   **prove-or-reject obligations** (family B in the Obligation-Role frame), not governance
   (family A).
2. `rule TotalInventoryCost == AverageCost * QuantityOnHand` — nonlinear — cannot currently be
   proven by the engine. Under prove-or-reject: the compiler either proves it holds (from the
   declared bounds on AverageCost and QuantityOnHand) or **rejects the definition**.
3. `rule Balance == Deposits - Withdrawals` — linear — could potentially be proven by §1a
   once built. Under prove-or-reject: it either proves or rejects.
4. The 21/77 sample files using product expressions (`boundary-ruling:25`) would be **rejected
   definitions** under prove-or-reject for relational invariants, unless the proof engine can
   prove their invariants. Given that §1a/§2/§3 are MVP-scope-designed-not-built, most would
   be rejected today.

**What the boundary ruling says about this path:** The boundary ruling explicitly tested and
rejected "Pure Model B — prove everything or reject." Rejection reason: *"21/77 sample files
use products, which would become illegal ... zero expressiveness loss was non-negotiable"*
(`boundary-ruling:§3 Leg 2`). The Hybrid model was chosen specifically TO accommodate
relational invariants over independently-set fields without requiring the compiler to prove them.

**What this means:** If Shane's intent is prove-or-reject for relational invariants, this
retroactively undoes the Hybrid model's core accommodation. The boundary ruling would need to
be reopened, not just v2 corrected.

**Owner question:** Did Shane intend prove-or-reject for relational invariants (meaning the
boundary ruling needs to be reopened), or does his "ingress-only" framing refer only to
*computed values* that don't have an ingress door — which is exactly what Decision #1 and
Option B covered? The answer determines whether v2's text is wrong or Shane is retroactively
extending the ban beyond its original scope.

---

## Part 7 — Summary of Findings

### 1. Is the third arm present in §3 of v2?

**YES.** Shane's corrected section reference is precise. Specifically:

- **§3 sentence (primary):** "A declared relational invariant is GOVERNED, not proven" —
  the explicit statement of the third arm as the conclusion of the fault-surface enumeration.
  This sentence creates a third path (governed at runtime) between prove and reject for the
  relational-invariant case.
- **§1 sentence (upstream enabler):** "by structural runtime enforcement everywhere else" —
  the framing that makes the §3 claim operative. Sets up "everywhere else" as the runtime
  fallback bucket that relational invariants then fall into.
- **§2 sentence (guarantee claim):** "enforced structurally on every operation" — the
  absolute guarantee that depends on the sweep to be true for relational invariants.

### 2. Does §2's third-arm language contradict §10's ban?

**Technically no; substantively depends on Shane's scope for the ban.**

§10 bans "Govern, don't prove for **derived values**." The §3/§4 governance language is for
**independently-set fields** (relational invariants), not derived values. The ban as written
does not technically cover this surface.

BUT — if Shane intended the ban to cover any "govern instead of prove" case (not just derived
values), then §3 directly contradicts §10's intent even if §10's written scope is narrower than
Shane's intent.

### 3. Is this the SAME issue as the ingress-scope question?

**YES.** Both are the same design-intent conflict: does governance extend beyond ingress to the
post-mutation sweep for relational invariants over independently-set fields? The ingress-scope
verification asked it from Shane's stated claim; this §2 analysis asks it from the text of v2.
Same question, same answer required, same owner ruling needed.

This is NOT a distinct third recurrence of a new error. It is the same error recurring in a
new location: what was §4(b)/§5 in v1 (explicit sweep description) is now §1–§2 in v2 (the
guarantee framing that requires the sweep to be true). The third arm was not removed — it was
promoted to the top of the document, where the guarantee now depends on it.

### 4. What does Frank need to know?

The "two routes, one of them two-mechanism" framing in §4 does not resolve Shane's concern —
it correctly addresses the "three-way routing" reading, but it does not address the prior
question of whether the sweep mechanism for relational invariants is authorized at all. Shane's
concern is not "you're calling it three routes" — it's "you're including a runtime fallback for
things that should be proven or rejected."

---

## Bottom-Line Answer to Shane

**The third arm you're seeing in §3 is real. Here is precisely what it is:**

The sentence "A declared relational invariant is GOVERNED, not proven" in §3 creates a third
path between prove and reject, specifically for relational invariants over independently-set
fields. The upstream claim "by structural runtime enforcement everywhere else" in §1 set up
the bucket; §3 explicitly places relational invariants into it. This is structurally identical
to Option B — "govern what you can't prove at runtime instead of prove-or-reject" — applied
to the relational-invariant surface rather than the derived-value surface.

**Whether this IS the banned arm** depends on how you scope your own ban:

- **Narrow scope** (§10's written text): "Govern, don't prove for **derived values**." Under
  this scope, relational invariants over independently-set fields are NOT covered by the ban —
  they have no ingress door, and the sweep is the only enforceable mechanism for them. The
  corpus at `boundary-ruling:§3`, `boundary-ruling:160`, `spec:1969`, and
  `compiler-and-runtime-design.md:§1.1` all explicitly support the sweep for this surface.

- **Broad scope** (your stated intent): "the ONLY runtime carve-out is ingress." Under this
  scope, the sweep IS the banned arm — it's a runtime fallback for cases where the compiler
  can't prove the invariant, which is exactly what you said shouldn't exist.

**If your broad scope is the correct intent:** the fix is NOT a text correction — it's a
design change. Relational invariants over independently-set fields become prove-or-reject
obligations, and the compiler must either prove them or reject the definition. This
retroactively reopens the boundary ruling's rejection of Pure Model B, which was explicitly
chosen to avoid rejecting 21/77 sample files that use product expressions.

**The verdict on the document:** v2 accurately reflects the corpus as it was ratified
(Hybrid model, boundary ruling, spec §3A.4). The "third arm" is not an authoring error in v2
relative to the corpus — it is the corpus. Whether the corpus itself needs to be reopened and
revised is the open question that requires your ruling.

---

## Action Required (Owner-Gated)

Before v2 can be finalized, Shane needs to rule on:

> **Did Decision #1's prove-or-reject ratification extend to relational invariants over
> independently-set fields — such that definitions with unprouvable relational invariants must
> be REJECTED, not governed by the post-mutation sweep? Or was the ban scoped to derived-value
> fault obligations only, with the sweep remaining the authorized mechanism for relational
> invariants?**

This is not a document question. It is a design question. The document reflects whichever
answer the corpus currently gives (the sweep is authorized). If the answer changes, the
boundary ruling, spec §3A.4, compiler-and-runtime-design.md §1.1, and 20+ sample files are all
affected — in addition to v2.

---

*George — 2026-07-14. No files edited. All analysis read-only.*
