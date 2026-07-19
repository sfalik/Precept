# Completeness Critique — final check on the want-analysis decision packet

**Status**: Draft analysis - 2026-07-19 (agent-authored; evidence only; rulings remain the owner's)

**What this is.** The last-pass completeness check before `DECISION-PACKET.md` ships to the owner. Method: read the want doc (`docs/Working/what-i-want-2026-07-16.md`), the packet, and the six axis reports; hunt for owner questions answered around, uncited load-bearing claims, promised-but-unrun probes, begged questions, unengaged want-doc sections, and implicit decisions. Findings are ordered most-important first, each with its cheapest closure. Where the packet is genuinely complete, that is said too.

---

## Misses, ordered

### 1. The packet's epistemic root — "80 confirmed, 9 refuted, 1 unverifiable" — has no on-disk evidence record

**What is missing.** The packet's opening claim (`DECISION-PACKET.md:5`) is that every load-bearing claim was "adversarially verified against the repository by independent verifier agents," and every body conclusion leans on a "(confirmed)" tag. But the analysis directory contains only the six axis reports and the packet — no verification ledger, no probe transcripts, no per-claim verdict record. The nine refutations are spot-checkable (each carries counter-evidence inline, §g.1). The **80 "confirmed" labels are bare assertions**: an owner or reviewer cannot distinguish "verified against source" from "asserted by the synthesizer." The corrected measurements the packet cites as verification products (4,059 test attributes; 960-cell collection unit; 4-sample temporal blast radius; the 417-write-site `now()` intersection in R-9) exist nowhere but the packet's own prose.

**Where.** `DECISION-PACKET.md` §preamble and every "(confirmed)" tag; §g is the only evidence surface and covers refutations only.

**Cheapest closure.** Commit a verification ledger beside the packet — one row per claim: claim, verdict, and the specific evidence (file:line, probe output, or command) the verdict rests on. If the evidence lives only in session transcripts, the packet must say that explicitly, so "confirmed" is honestly labeled as re-derivable-from-citations rather than independently auditable.

### 2. Feasibility "yes" is silently scoped to the compiler; the runtime build is enumerated but never sized

**What is missing.** The owner asked "can we build this? one person with AI Agents?" of the want — which includes a defined runtime contract (premises-only evaluation, inspect with provenance, immutable versioning, the load-time certificate gate). The feasibility axis sizes the compiler-side machinery thoroughly (`feasibility.md` §1.3: §1a ~500–700 LOC, §1b ~1,500–2,200, Slice 0 "L", "one more proof-engine's worth"), but the runtime evaluator and its load-gate call site appear only as "cannot exist yet" / "routine once #8 exists" (machinery rows 9 and 13) — enumerated, never priced. The packet's §(a)2 "Yes; yes; no" carries no statement that the yes is sized over compiler machinery only. This is not a re-flag of the runtime-is-unbuilt context the owner already holds — it is the *scope of the answer* to a direct owner question left unstated.

**Where.** `DECISION-PACKET.md` §(a)2; `feasibility.md` §1.3 and Bottom line. §g.4 discloses the runtime was analyzed only at spec level, but that is about analysis coverage, not build sizing.

**Cheapest closure.** One caveat sentence in §(a)2: the one-person assessment covers the compiler + certificate/checker machinery; the runtime build (evaluator, inspect, versioning, load gate) is defined by the want but was not sized by any axis, and the feasibility follow-on (packet §f step 6) should carry it.

### 3. The non-temporal half of G2 — the numeric-conversion overflow family — vanished between the axis report and the packet

**What is missing.** `comprehensiveness.md` G2 covers two fault sub-families: instant-arithmetic overflow **and** "the large-value integer-conversion family on `number`/`decimal` → `integer`" (`floor(1e20)`; `primitive-types.md:658` — "statically preventable when the proof engine can bound the expression range"). The R-6 refutation corrected only the temporal blast radius; the packet's Q11 carries only the temporal kernel, proceed item 14 covers only *non-finite* `number` (NaN/±∞ — a finite 1e20 is outside it), and Q9 covers integer *arithmetic* overflow. The conversion family — bound-the-operand-or-reject on rounding/conversion functions — is in no queue item, no proceed item, and no worklist row. It was never refuted; it was dropped in synthesis. It plausibly discharges via ordinary numeric bounds (fields *can* carry min/max) and may already sit inside the §3.1 `NumericOverflow`/`OutOfRange` enumeration the outline lifts (`docs/compiler/soundness-and-coverage.md` §3.1), but the packet nowhere says so.

**Where.** `DECISION-PACKET.md` §(a)3 (the "downsized" paragraph), Q11, proceed 14 vs `comprehensiveness.md` G2 ¶1 and Consequence ¶.

**Cheapest closure.** One line in proceed item 14 (or outline §9's enumeration lift): the conversion-range family rides with the finiteness flip as a proven-bound obligation — mechanical, premises exist — so it is visibly carried rather than silently gone.

### 4. Comprehensiveness M7's "a prove-or-reject spec must settle" ambiguities dropped entirely; guard-expression fault attachment half-dropped

**What is missing.** `comprehensiveness.md` M7 names two small spec ambiguities a prove-or-reject spec must settle: the `pow` negative-exponent lane scoping (`primitive-types.md:613`, a standing stop-and-fix item) and `maxplaces`-on-division-results precision. Neither appears anywhere in the packet — not in the queue, the proceed list, the worklist, or the outline. Separately, `clarity-dispute-closure.md` Part 4 item 3 lists "guards' own fault obligations" (where a division *inside a guard* attaches) as a must-add vocabulary item; the packet's outline §10 carries "rule-internal fault attachment" but not the guard-position case — a distinct attachment site (guards have no write site and run pre-state).

**Where.** `DECISION-PACKET.md` §(e) §10 and §(d) vs `comprehensiveness.md` M7 and `clarity-dispute-closure.md` Part 4 item 3.

**Cheapest closure.** Add the two M7 items as leads in §(d)'s unverified list (they are canon-ambiguity rows of exactly that kind), and add "guard-expression fault attachment" to outline §10's vocabulary enumeration. No re-run needed.

### 5. v3's surviving deferrals (aggregates, compute-then-check, Presence/KeyPresence witness) were never checked for no-deferral compatibility

**What is missing.** `scope-delta.md` row 23 marks the v3 deferred items "Unchanged — want silent," and the packet inherits this inside "removed: nothing." But under the want, a deferral is only compatible if it is *build-order* deferral (the construct rejects or does not exist at HEAD), never if the construct **compiles today without its obligations proven** — that would be a live fail-open hole of exactly the kind proceed item 4 exists for. No axis probed which kind each of these three is. The same one-probe check that validated the mutation table would settle it.

**Where.** `DECISION-PACKET.md` §(a)1 "Removed: nothing" / §(c)2 vs `scope-delta.md` rows 23 and §2.6.

**Cheapest closure.** One bounded probe (or doc read) per item establishing "surface absent/rejecting today" vs "compiles unproven"; record the three verdicts in a sentence under §(c)2. If any compiles unproven, it joins proceed item 4's fail-open list.

### 6. Proceed item 13 embeds an agent-inferred exception as settled — "construction-overwritten placeholders excepted"

**What is missing.** The want grounds live-default evaluation in "no constructor arg sets it" (`what-i-want-2026-07-16.md:99`) but never states the converse exception or its scope. The canon report invented the wording ("Defaults that a construction event provably overwrites are placeholders and are a designed exception," S15 row), and the packet's §(c)13 carries it inside a no-decision-needed item. The exception's precise scope is genuinely open: overwritten on *every* initial path, or any? What about free-construction idioms (comprehensiveness row 25)? This is a small begged question — the *flip* is settled by the want; the *exception's boundary* is not.

**Where.** `DECISION-PACKET.md` §(c)13; `canon-corrections-and-supersession.md` S15.

**Cheapest closure.** Keep the flip in proceed; mark the exception as inferred-from-`want:99` and move its precise scope ("overwritten on every initial path" as the presumable reading) into the canonical-doc vocabulary items (outline §2, base case). No queue item needed unless the owner disagrees with the presumable reading.

### 7. "Previous attempts" (plural) — the comparison baseline is never stated in the scope answer

**What is missing.** The owner asked whether the want changes scope "vs the previous attempts." §(a)1 answers against v3 only. The other attempts are in fact covered — the meta-plan via `scope-delta.md` §2.7, the 07-11 thesis via `clarity-dispute-closure.md` Part 2, the 06-16 v2 plan via the supersession map — but the packet never says that v3+meta-plan is the chosen baseline or where the earlier attempts' comparisons live. A reader checking the plural could conclude the question was answered narrowly.

**Where.** `DECISION-PACKET.md` §(a)1 preamble.

**Cheapest closure.** One sentence: baseline is v3 (the only live plan) as re-widened by the 07-13 meta-plan (`scope-delta.md` §2.7); the 07-11 thesis comparison is in `clarity-dispute-closure.md` Part 2, and the superseded 06-16 v2 is dispositioned in the supersession map.

---

## What was checked and found complete

To avoid inventing findings, the following were hunted and came back clean:

- **All five owner questions are answered head-on**, none answered around. The comprehensiveness answer engages the owner's "collections, for example" prompt directly; the clarity answer honestly reports the two-yardstick condition rather than overclaiming a single doc; the meta answer names what the owner was missing rather than reassuring.
- **Every want-doc section has axis engagement**, including the ones easiest to skip: the deliberate-omission paragraph (`soundness…md` §Candidates — verified, "no hole"), inapplicability-vs-refusal/Unmatched (`scope-delta.md` row 21, `clarity…md` F6), immutable versioning and non-latest-Version branching (`soundness…md` §Candidates), inspect provenance (canon R4, outline §7), the never-inserts-the-premise rule (scope row 9 quotes it), restore/schema-evolution (Q3), and the expressibility-trade stopping rule (steelman 3, outline §13).
- **Refuted and unverifiable claims are carried, not buried** — every §g.1/g.2 entry has counter-evidence and a surviving-kernel disposition, and each downstream body claim I traced (R-1→Q7 option ii, R-4→Q10 option ii, R-6→Q11, R-7→Q9, R-9→Q14a sizing) was correctly rewritten rather than silently inherited.
- **The closure-condition check (§g.3) honestly fails both legs** and names the exact distance (Q1, Q4, two unswept surfaces, 20 leads) — no redefining of the bar detected.
- **Promised sweeps**: the canon sweep's own coverage gaps (`compiler-and-runtime-design.md` unread; type docs out of scope) are disclosed in the packet at §(d) and §g.3/g.4 rather than papered over. The clarity report's 12-cell probe, the 11-mutation matrix, and the corpus `now()` intersection were all actually run per their reports.
- **Decision-queue completeness against the reports**: every structural gap, soundness finding, and ambiguous dispute in the six reports that requires a ruling maps to a queue item (G1→Q10, G4→Q12, G5→Q13, S1→Q3, S2→Q2, S3→Q5, F12(a)→Q1, D6→Q4, dead rows→Q6, §1b→Q7, checker→Q8, number model→Q9, temporal→Q11) — with the exceptions already listed as misses 3–6 above.

**Net.** The packet is a faithful, honestly-caveated synthesis. The seven misses above are all closable with one committed evidence file (miss 1), three probe/read checks (miss 5), and five one-to-two-sentence edits (misses 2, 3, 4, 6, 7). None invalidates a packet conclusion; miss 1 is the only one that touches the packet's credibility architecture rather than its content.
