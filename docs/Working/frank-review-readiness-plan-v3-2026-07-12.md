---
title: "Frank's Review — Compiler Readiness Plan v3 (3-Doc Set)"
status: Draft — 2026-07-12 (Frank's architect review; nothing ratified — hard gates flagged for owner)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
reviews:
  - docs/Working/compiler-readiness-plan-2026-07-12-pipeline-evaluation.md
  - docs/Working/compiler-readiness-plan-2026-07-12-architecture.md
  - docs/Working/compiler-readiness-plan-2026-07-12-structural-severity.md
context: >
  Companion to the ratified proof-engine-decision-ledger-2026-07-12.md, which directed
  "/plan rebuilds the readiness plan around the MVP core." These three docs are that
  rebuild (v3 of the compiler-readiness-plan lineage). Frank reviewed all three together
  for technical credibility (source citations), consistency with the ratified ledger
  (#1-#10), consistency with his own prior positions in this debate chain, cross-doc
  internal consistency, and overall go/no-go readiness to code against.
note: >
  This is Frank's raw, verbatim architect assessment — read-only analysis, no files
  edited by Frank. Per his own standing discipline, this is his read, not a ratification;
  every flagged gate remains Shane's to confirm or redirect.
---

# Frank's review — v3 readiness plan (3-doc set), 2026-07-12

Shane — I read all three companions against each other, against the ratified ledger, against source, and against my own prior positions in this chain. Bottom line up front: **the engineering is strong and the code citations are unusually trustworthy, but there are three owner-gate problems and one fabricated citation that you need to personally clear before this is greenlit.** Details below, everything cited.

## 1. Technical credibility — spot-check results

I verified every load-bearing *code* citation I could. The hit rate is essentially 100%:

**Architecture doc — code citations all check out:**
- `ProofLedger.cs` binary `ProofDisposition { Proved=1, Unresolved=2 }` + nullable siblings `Strategy?`/`EmittedDiagnostic?`/`ComputedInterval?` on `ProofObligation` — **confirmed** (enum and record verified). The DU-replaces-both framing is accurate.
- `NumericInterval.cs:29` `Empty = new(0m, -1m)` — **confirmed**. The F4 kernel-guard analysis is correct and I re-derived it: for `Empty=[0,-1]` the `Divide` zero-guard `other.Min <= 0m && other.Max >= 0m` (`:97`) evaluates `0<=0 && -1>=0` = **false**, so an empty divisor falls through to four-corner `Min/0` → **throws/garbages**. `Multiply(Empty,[2,3])` = `[-3,0]` non-empty garbage. `Scale` (`:110`) guards only `IsUnbounded`, so it leaks too. `Contains` returns `true` on empty arg (`:127`) — the over-prove vector. `Union` has no empty special-case (`:131-135`). **Every claim in §1.5 is exactly right.**
- Hole 1 (`ProofEngine.cs:1060-1066`): `TryNumericDefaultBoundProof == false ? Unresolved : (Proved, Literal)` — **confirmed**, `null` (undecidable magnitude) stamps `Proved`. Note: the code comment right above it *documents this as intentional* ("proved vacuously per the unresolvable-magnitude limitation"). It's a real fail-open under ratified #1, but it was a deliberate pre-#1 conservatism, not a latent bug — the doc's "hole" framing is fair given #1, just worth knowing it was a documented choice.
- Hole 3 (`BuildNarrowedIntervals` never reads `ReassignedBefore`): **confirmed by grep** — `ReassignedBefore` appears in `ProofEngine.cs`, `.Strategies.cs`, `.QualifierNarrowing.cs`, but **zero** hits in `.Intervals.cs`. Exactly as claimed.
- Relational fold + ⊥-suppression (`Intervals.cs:633-654`, suppression `:650-653`), `Contains(⊥)⇒true` hazard, `NarrowByConstraint`, `CollectTrustedNumericFacts` — all confirmed. The §6 reuse plan is grounded.
- F8 point-binding: field leaf `:92` vs `ExtractArgInterval` `:99` — confirmed; the demote-to-`Unresolved` degradation is sound.
- `ProofRequirement.cs` `AuthoredMin/Max` (display-only), `CountContainment` endpoints, `ProofEngine.Diagnostics.cs:141` obligation-shaped count message — all confirmed.

This is the most citation-faithful doc in the chain — same quality I praised in my mvp-phases review. I'd code against its factual claims without re-verifying.

**Pipeline-evaluation doc — checks out.** The D2/spec:256 walk-backs are correct (see §2). The `DiagnosticStage` entanglement probe (zero analyzer usage, three-file footprint) matches what I'd expect. No credibility problems.

**Severity doc — code citations perfect, doc-to-doc citations have two real errors:**
- **All 18 `Diagnostics.cs` severity rows verified exact** (`:707` UnreachableState Warning, `:720` StructuralSinkState Warning, `:726` DeadEndState Warning, `:747` RequiredState… Warning, `:802` ContradictoryRule Warning, `:814` UnsatisfiableRule Warning, `:733/:740/:869/:879` already Error, etc.). Messages quoted verbatim match.
- **§5a terminal-detection soundness argument fully verified**: `ComputeDeadEnds` (`GraphAnalyzer.cs:489-494`) excludes terminals explicitly; `StructuralSinkState` gated `!IsTerminal` (`:117-119`); `GetStateFlags` catalog-derived `isTerminal |= !AllowsOutgoing` (`:683`) ← `Modifiers.cs` `terminal ⇒ AllowsOutgoing:false`; zero-terminal guard (`:132-149`); `AlwaysRejecting` construction-row per-instance `Severity.Error` override (`:783`). This section is airtight — the precondition analysis is correct.
- `graph-analyzer.md:457` and OQ1 `:776-777` (dead-end intentionally-Warning, resolved 2026-05-07) — **quoted accurately**; this locked stage-doc conflict is real.
- **ERROR — `philosophy.md:150` "an invalid definition cannot produce an engine":** **`philosophy.md` is 104 lines long. Line 150 does not exist, and that phrase appears nowhere in the file** (grepped every variant). This citation is cited **twice** (§2 uninhabitability rationale, §4) as a *load-bearing rationale leg* for flipping `ContradictoryRule`/`UnsatisfiableRule` to Error. The *concept* is defensible from philosophy.md:49/:51's prevention spirit — but the specific quote+line is fabricated. **This must be fixed before it grounds a canonical severity change.**
- **Sloppy — `philosophy.md:421`/`:423`** for `terminal`/`irreversible` definitions: philosophy.md has no such lines. The content is real but lives in **`precept-language-spec.md` ~421-423** (verified — the token table). The doc even annotates "(spec)" next to `philosophy.md:421`, so it's an internally-contradictory mis-file. Cosmetic, but combined with the :150 error it signals the doc's non-code citations weren't checked with the same rigor as its code citations.

## 2. Consistency with the ratified ledger (#1–#10)

**Where it's correct:**
- `ProofVerdict` DU maps #6 (three-way) and #10 correctly: `Proven` / `ProvenViolating(witness)` = #10 dead-**row** = Error / `Unresolved(condition)` = rejected-with-weakest-precondition. Clean mapping.
- Certificate model reflects #2's four-part criterion **on legs 1 and 3** (legible/re-checkable in §1.2-1.3; right-sized via the k≤6 cap and "no search, bounded by expression size" in §1.4). Leg 2 (performant, ~1-3 ms) is only lightly touched ("bookkeeping, not a second pass"). **Leg 4 (justified/earns-its-place) is not explicitly carried per-strategy** — it was a ledger-level gate, and §1a/§2/§3/§6 were folded into the MVP by the ledger, so it's implicitly satisfied, but the doc should say so rather than leave leg 4 silent.
- Pipeline-eval correctly walks back the reversed 2026-06-16 assumptions: D2 band-reclassification "**DEAD as a plan item**" (#1/#6/#10 keep bounds as Error), and the Slice-1.5 canon-amendment "DEAD half" marks the **spec:256 override REVERSED / do-not-inherit** (matches #3's "spec:256 NOT authorized"). This is exactly right and resolves the drift I'd worried about.
- The severity doc's Warning→Error logic **is the same "prevention means Error, not Warning" argument I made for the value-bound case**, extended to process-topology and uninhabitability. Applied consistently. Substantively I agree with the *direction*.

**Where it diverges — the load-bearing problem:** The ratified ledger (committed 16:20, "owner rulings") contains **#1–#10 and §6-scope, and nothing else**. I grepped it. It contains **no** ruling on:
- the `ProofVerdict` **DU** representation (arch "Decision A"),
- the `CertificateSteps` **catalog** (arch "Decision C"),
- **any** of the six structural-severity flips (`UnreachableState`/`StructuralSinkState`/`DeadEndState`/`RequiredStateDoesNotDominateTerminal`/`ContradictoryRule`/`UnsatisfiableRule`).

The ledger's only severity ruling is **#10 — dead-*row* severity** ("a write that provably always violates its limit but sits on a reachable row"), which is the containment *proven-violating* bucket. That is a **different family** from the structural-topology/uninhabitability diagnostics the severity doc flips. #6 mentions "dead rows, contradictions, unreachable states" only as examples of *violation reports* (vs. containment obligations) — it does **not** flip their severity. **So all six severity flips are net-new against the ledger**, yet the doc stamps them "RULED (owner, 2026-07-12)." Same for arch Decisions A and C. See §3.

## 3. Consistency with my own prior positions — and the process flag

The new docs are **consistent** with my substantive conclusions:
- **§1b deferred/unscheduled** (my firm "not now" in `frank-final-review…:31,93`) — honored: arch §1.6 single-hop rail keeps `ExtractFieldInterval` from folding constant rules, spec:256 override not authorized. ✓
- **§5b re-checker deferred** (my "don't build it until the strategy set stops changing," `frank-final-review…:84`) — honored by Decision B's retraction. The "re-checkable in principle, no live checker in MVP" split matches exactly what I argued: the certificate must be *independently checkable in shape* (I contrasted it with an SMT trace precisely because a Farkas-style cert *can* be replayed), but a live replay need not run in the MVP. **No contradiction** — this is my position implemented.
- **Item #5 (computed fields don't close the compute-then-check gap)** — untouched by these docs; ledger #5 already captured my carve-out. Fine.

**But here's the process problem, and it's the same one I caught before.** In my mvp-phases review I wrote (rec #108, and the §1 boundary): *"Everywhere the document's staging language implies a locked-spec question has been resolved 'as part of staging,' rewrite to surface the question as open and owner-gated… the document's largest build silently answers two of the locked-spec questions I already flagged to you… those need to come back to you as the same decisions they always were."*

**These three docs re-commit that exact pattern**, stamping decisions "RULED (owner)" inside the proposing document with no ledger backing:

- **Decision A (DU):** Substantively correct — CLAUDE.md's "DU over nullable fields for varying shapes" *mandates* it. But that makes it a code-architecture call the rules already settle, **not an owner-gated decision at all**. Stamping it "RULED (owner, 2026-07-12)" is harmless over-ceremony. Proceed — just don't cite it as an owner ruling. **Low stakes.**

- **Decision C (new spec-enumerated `CertificateSteps` catalog):** This introduces **new language surface** — a new catalog. Under the Tier-2 pre-design gate I've insisted on, a new catalog owes a `/design` pass + owner conversation. Ledger #2 authorized "a small, spec-enumerated vocabulary" for the *criterion*; it did **not** name a new catalog, define its membership, or resolve C1-vs-C2. And the **ledger's own closing section says the certificate format (§5a) still owes "a design pass… first coding slice."** So marking C "RULED" pre-empts a design pass the ledger explicitly left open. **Medium stakes** — the *membership* of `CertificateSteps` is undefined design surface, not a settled ruling.

- **The six severity flips:** **Highest stakes.** They change the **compile rejection surface** (definitions that compiled clean now reject) and the doc *itself admits* they **override two locked canonical decisions** — `graph-analyzer.md` OQ1 (dead-end=Warning, "Resolved 2026-05-07") and `diagnostic-system.md:180`'s reasoned contradictory/unsatisfiable-stays-Warning rationale. Per my own Tier-3 discipline, overriding a locked canonical decision requires **explicit owner authorization**. The doc does the rigor right (quotes the locked decisions verbatim, frames the override cost) — but the authorization itself ("the owner's ruling authorizes both") is **asserted inside the proposing doc** and is **not in the ledger**. That is precisely the "substantive decision presented as already-settled" pattern I flagged in the legibility-commit case. **You need to personally re-confirm you ruled these six** — the process-topology four have a genuinely strong mandate (philosophy.md:51 verified: "the compiler proves these impossible at definition time"), so those are easy to affirm; the **uninhabitability two are weaker** (they override a specific reasoned rationale *and* lean on the fabricated `philosophy.md:150` cite), so give those two a harder look.

To be clear: I'm not saying you didn't rule these. I'm saying the **authorization trail runs through the documents that propose the changes**, not through the ledger you actually signed — and for surface-changing, canon-overriding decisions, that trail isn't good enough. It's exactly what I asked you to guard against last time.

## 4. Internal contradictions / gaps across the three docs

- **The docs agree where they overlap** on the substance: DeadEndState=Error (arch §1.9/Hole 4 ↔ severity §2), the outcome-neutrality of the Literal→Unresolved relabel once dead-end blocks, every-verdict-carries-a-certificate (arch §1.2 ↔ pipeline-eval §4). Good.
- **Working-tree contradiction (real, and you should see it):** the severity doc's header and §6 say "**docs-only pass: no code severity changes here**" and "**do not apply to canonical docs now**." But `docs/compiler/diagnostic-system.md` is **currently modified-uncommitted**, and the diff **already rewrites `:180`** into the two-axis split — process-topology + uninhabitability block, the rest report — and self-labels it "**the ruled severity model**" with the code as "**tracked drift**," *and* already applies the per-instance-severity correction. So a sibling uncommitted edit has **already done** the canonical-doc change the severity doc says must wait. Net effect right now: **canonical prose says Error, `Diagnostics.cs` still emits Warning** — live doc-vs-code drift, and the severity doc mischaracterizes the current state (its `:180` quotes are accurate to *committed HEAD*, but HEAD is already superseded in your working tree). Decide whether the canonical flip lands now or in the Stage-1 slice, and make the two artifacts agree.
- **Minor sequencing mismatch:** pipeline-eval files the `Literal // vacuously proved` mislabel (`ProofEngine.cs:1181`) as a §5a-riding refactor (Slice 1.4); arch pulls the same mislabel to **Slice 0** as Hole 4 + the §1.9 two-arm split. Compatible, but they name different owning slices. Pick one.
- **Coverage gap:** pipeline-eval bills itself as keep/refactor/remove **for every finding across the whole pipeline**, but it does not disposition the six severity flips at all (defers wholly to the companion), and §1 marks GraphAnalyzer "keep — no dead scaffolding" while the severity doc flips four GraphAnalyzer-emitted diagnostics. Not a contradiction, but no single doc holds the full disposition set.

## 5. Anything alarming / over-claimed

- **"No architecture open decisions remain" (arch header + §3) is over-claimed** — contradicted by the arch doc's own §1.4, which leaves the **Presence/KeyPresence configuration-witness as an owner-choosable open cell** ("defaulting to deferred… unless the owner wants it in-MVP"). Plus the `CertificateSteps` catalog membership and the length-witness plumbing are undefined. "No *open* decisions" is false by the doc's own text.
- **"The owner has ruled the whole family (2026-07-12)" (severity doc)** — asserts certainty on six decisions absent from the ledger; see §3.
- The **fabricated `philosophy.md:150`** grounding a canonical severity flip is the single most alarming item on pure credibility — a canon-overriding decision should not rest on a quote that doesn't exist in the canon.
- No under-addressed soundness hole that I can see — if anything the **soundness section is the best part**: the F4/F7 ⊥-rail ownership analysis (dict-write-time detection as primary, kernel-guard as backstop, and the explicit §1.5-vs-§1.8 conflict resolution) is genuinely careful, and the F6 "we found four by hand, so a systematic sweep is owed" honesty is exactly right. That's not over-claiming certainty; it's the opposite.

## 6. Bottom line — is v3 ready to code against?

**The engineering substrate is ready and trustworthy.** The soundness inventory, the DU design, the §3/§6 reuse plans, and the citation fidelity are all strong — this is a codeable Slice-0 once the gates below clear. My prior positions are honored, not contradicted.

**But do not greenlight before you personally clear these gates, ranked by how load-bearing they are:**

1. **[HARD GATE] Re-confirm you actually ruled the six severity flips.** They change the rejection surface and override two locked canonical decisions (`graph-analyzer.md` OQ1; `diagnostic-system.md:180`), and **none is in the ledger you signed**. Affirm the process-topology four (strong philosophy.md:51 mandate) and, separately and more carefully, the uninhabitability two (`ContradictoryRule`/`UnsatisfiableRule`), which override a reasoned rationale and lean on a bad citation.

2. **[HARD GATE] Fix the fabricated `philosophy.md:150` citation** before it grounds the uninhabitability flip. philosophy.md is 104 lines; the quote doesn't exist. Reground the uninhabitability rationale on real canon (philosophy.md:49/:51, or the actual creation-time precedents PRE0115/PRE0164 which *are* verified Error) — or the flip is resting on air.

3. **[GATE] Decide whether `CertificateSteps` is "ruled" or owes a `/design` pass.** It's new language surface (a new catalog) whose membership is undefined, and the ledger says the certificate format still owes a design pass. Either run the Tier-2 conversation + `/design`, or explicitly fold the catalog into the §5a design slice — don't let "RULED C1" pre-empt the design the ledger reserved.

4. **[RECONCILE] Resolve the working-tree self-contradiction:** `diagnostic-system.md` is already flipped in prose (uncommitted) while the severity doc says "do not apply to canonical docs now" and the code still emits Warning. Pick when the canonical flip lands and make doc, code, and the severity doc agree. Right now you have three artifacts telling three different stories about the same six severities.

5. **[CLEANUP] Downgrade the "no open decisions remain" / "whole family ruled" language** to match reality (§1.4's Presence-witness cell is open; Decision A didn't need an owner ruling; the leg-4 justification and length-witness plumbing are unstated), and fix the `philosophy.md:421/:423`→spec mis-file.

Gates 1 and 2 are the ones that would embarrass us if this shipped as-is — a surface-changing, canon-overriding decision stamped "owner-ruled" without a ledger entry, partly grounded on a quote that isn't in the file it cites. That's the same class of thing you asked me to catch. Everything below gate 3 is cleanup. Clear 1–4 and I'm comfortable calling this the engineering plan of record for the disposition — conditional, as always, on the rulings actually being yours.

*Per my standing discipline: this is my architect's read, not a ratification. The six severity calls, the catalog decision, and the DU stamp remain yours to confirm or redirect.*
