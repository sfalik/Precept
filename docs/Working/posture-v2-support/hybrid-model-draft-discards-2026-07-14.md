# Hybrid-model draft — discards and unported material (2026-07-14)

> **Status:** Working — preservation record per Shane's "don't throw them away" directive
> **Context:** The now-deleted root-level hybrid-model copy was refined in place (Frank, 2026-07-14): Example A de-centered, four new examples added (G temporal, H quantity/ensure, I collections, J string-length), banned-arm example renamed G→K. Edits were almost entirely additive. This file preserves (1) the exact original phrasings that were replaced in that stale copy, and (2) alternate example concepts from Frank's pre-redirect v2 draft that were not ported into the rewrite now living under `docs/Working/posture-v2-support/`.

## 1. Original phrasings replaced in the now-deleted root-level hybrid-model copy

Each entry: original text verbatim, and a one-line reason for the change.

### 1.1 "How to read" paragraph (doc header)

> Section 6 is the heart of the document: seven worked examples in real `.precept` syntax, each walking through what is proven, what is governed, at which enforcement point each check fires, and why.

**Cut because:** count updated (seven → eleven) and domain breadth named, after the example set was broadened per Shane's diversity clarification.

### 1.2 §5.1 dual-role pointer

> Example A in §6 is the canonical demonstration.

**Cut because:** replaced with "Examples A and H in §6 demonstrate this in two different domains" — the dual-role structure is now shown to be the model's pattern, not one example's.

### 1.3 §6 intro

> Seven examples, each in real `.precept` syntax, each naming the provenance of every field explicitly and walking every obligation to its enforcement point. Example A is the centerpiece: one rule playing both roles at once.

**Cut because:** "Example A is the centerpiece" directly contradicted Shane's clarification that the Deposits/Withdrawals case is one example among equals; replaced with an intro naming the deliberate domain/constraint-kind diversity.

### 1.4 Example A title

> ### A. Flagship — one rule, two roles: the derived value proven by a constraint that is itself governed

**Cut because:** "Flagship" framing removed for the same de-centering reason; retitled "A. One rule, two roles — a derived value proven by a constraint that is itself governed."

### 1.5 Old Example G (now K), Fix 1 walkthrough

> This is Example A — the flagship resolution, and the one that most enriches the contract: …

**Cut because:** "the flagship resolution, and the one that" → "the resolution that" — same de-centering; rest of sentence unchanged.

### 1.6 Cross-reference renumbers (mechanical, no content lost)

- Example A: "(Example G walks that rejection)" → "(Example K walks that rejection)" — banned-arm example renamed after four insertions.
- §7: "(Examples C and D)" → "(Examples C, D, and G, and the event ensure of Example H)" — new governance examples added to the list.

## 2. Unported alternate-example concepts from Frank's pre-redirect v2 draft

Before discovering the other instance's draft on disk, Frank had drafted a v2 with a 10-example set (the draft text itself was lost to session compaction; the concepts are recorded here). Concepts that did NOT make it into the final canonical doc, and why:

1. **SaaS guarded business rule** — `rule PlanMonthlyFee > '0.00 USD' when ConversionStatus == "Converted"` (from `samples/saas-trial-to-paid.precept:49`): a guarded cross-field rule governed at the sweep with an idle premise role. **Not ported because:** final Example B already uses the saas shape for pure ingress, and final Example G demonstrates guarded rules (temporal `when … is set`); a third saas variant added length without a new lesson. Still a good candidate if a "guarded rule over a choice field" example is ever wanted.
2. **Temporal twin: `rule DeliveredAt >= ShippedAt when DeliveredAt is set`** (shipment shape): **Not ported because:** final Example G covers the identical classification (guarded instant-ordering over independently-set optionals) using the `academic-course-registration` shape, which additionally demonstrates a state ensure.
3. **Nonlinear reported-identity: `rule ReportedTotalCost == ReportedUnitCost * ReportedQuantity`** over three externally-reported fields — the "permanently governed, terminal disposition, not a gap" case. **Not ported because:** final Example D (panel area `Width * Height <= MaxArea`) covers the same nonlinear-relational-edge lesson. The equality-over-three-reported-fields variant is slightly stronger as a "permanent disposition" statement (an identity the prover will never own) and could be added to D as a second snippet if desired.
4. **String interpolation containment** — `set DisplayName = "{First} {Last}"` into `maxlength 60`, proven from bounded args (25 + 1 + 30 = 56 ≤ 60). **Not ported because:** interpolation-fed `set` into a `maxlength`-bounded field could not be verified against any sample in `samples/`, and canonical docs must not assert unverified surface. Replaced by final Example J (maxlength-containment rejection of a wider carrier), which teaches string-length as a decidable bound with verifiable syntax. If the interpolation surface is confirmed, the arithmetic-over-lengths proof is a strong future addition to J.
5. **Queue `maxcount` bound** — Frank's v2 planned a `maxcount 3` queue with a `when count < 3` enqueue guard + reject row. **Not ported because:** no sample uses `maxcount` on a queue (samples show `mincount` on construction inputs and `maxlength` on queue element strings); final Example I uses the verified `count > 0` dequeue-guard shape instead, and covers `mincount`-at-ingress from `shopping-cart.precept`.

Nothing else from either draft was removed. All other content from the pre-existing 453-line root-level hybrid-model draft was preserved verbatim in that stale copy before the rewrite moved under `docs/Working/posture-v2-support/`.
