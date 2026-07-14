---
title: "Frank's Opinion — Spec Correction Ledger Owner Decisions (D1-D4)"
status: Draft — 2026-07-13 (Frank's recommendations; nothing ratified)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
reviews:
  - docs/Working/spec-correction-ledger-2026-07-13.md
context: >
  Shane asked Frank for his opinion on the four owner-decision items (D1-D4)
  in the spec-correction ledger's § "3. Owner decisions" and § "D4" — two
  genuine two-sided forks each with locked text on both sides (D1 sqrt
  widening, D2 choice field comparison), one design-shape fork (D3 fault
  delivery mechanism), and one wording/scope-call (D4 collections doc
  status). The ledger itself frames these neutrally; Shane wanted a real
  recommendation on each, not a restatement of the two-sided framing.
note: >
  This is Frank's raw, verbatim assessment — read-only analysis, no files
  edited by Frank. Recommendations only; D1-D3 touch locked spec text and
  remain Tier-3 (owner sign-off required) per Frank's own escalation
  discipline. D4 needs no new locked decision.
---

# Frank's read on D1–D4

I read the ledger in full, then re-derived each fork against the actual files (`primitive-types.md`, `precept-language-spec.md`, the runtime docs, the proof-engine decision ledger, and the v1 `spec-coverage-audit.md`). The ledger's evidence held up on every citation I checked. My rulings below are real recommendations; where they touch locked text I flag the Tier-3 sign-off you still owe yourself, but I'm not handing you a coin flip on any of them.

---

## D1 — `sqrt(integer)`: **Reading A (silently widen). Confident.**

The barred-side text (`spec:1586`: "`decimal` and `integer` inputs are type errors") is drafting over-reach, and there's a **decisive** reason, not just a stylistic one:

- **There is no explicit `integer→number` bridge function.** The conversion map (`primitive-types.md` §"Complete conversion map"; `spec:1248-1250`) makes `integer→number` *implicit and lossless* ("exact within safe integer range"). `approximate()` is `decimal→number` only (`primitive-types.md:611`). So under Reading B, an author with an integer field has **no clean way to feed it to `sqrt` at all** — they'd have to launder it `integer→decimal` (implicit) then `approximate(decimal)→number`, which is absurd. Reading B strands integers. That alone kills it.
- **Reading A is internally consistent with lane rule 2** (`primitive-types.md:372`: widening applies "in any context," with function-argument position explicitly listed at `spec:1264`). `sqrt` is a function-argument context; the universal rule already covers it.
- **The decimal bar is the *real* rule** and has a genuine rationale (`primitive-types.md:613`: "no .NET `Math.Sqrt` overload for `decimal`" — but more fundamentally `decimal→number` is *lossy* and requires the visible `approximate()` bridge). Integer carries no such lossiness. Lumping integer in with decimal at `spec:1586` was a copy error, not a decision.

**Philosophy check passes:** the *result* type is `number`, so approximation stays visible in the type system (the honesty-about-approximation commitment is about surfacing the approximate lane — `number` output does that). The lossless argument widening hides nothing.

**Weight:** `primitive-types.md` is the self-declared "single source of truth for numeric type conversions" (its own §"Numeric Lane Rules" header). On numeric-lane questions it outranks a one-cell entry in the §3.7 function table. The lane doc and `primitive-types.md:613` already agree with A; only `spec:1586` dissents.

**Coupled mechanical fix (resolves with the ruling):** edit `spec:1586` to bar *decimal only* — "`decimal` inputs are a type error; `integer` widens losslessly to `number`" — matching `primitive-types.md:613`.

**Tradeoff you're accepting:** `sqrt(intField)` silently pulls into the approximate lane with no author ceremony. I judge that fine because the output type announces it. **Tier-3** (edits locked spec text) — but this is a correction of a contradiction, not a design change, so your sign-off here is a formality.

---

## D2 — Choice field-vs-field comparison: **Reading A (spec §3.6 subsequence rule). Moderate confidence, one caveat.**

New fact that reframes this fork — from the v1 audit (`spec-coverage-audit.md:112`, `:884`): **the implementation satisfies neither reading.** It "accepts any two ordered choice fields including disjoint sets and mismatched element types — rank comparison across unrelated domains is semantically meaningless and a soundness-grade false-clean." And critically: "both doc rules predate the current pipeline… **no decision to drop the compatibility check found — gap, not decision.**" So neither doc reflects a deliberate override, and the impl must be *tightened either way*.

Given that, the question is purely "what's the right rule," and I give it to the spec:

- **`spec:1398` §3.6 is the authoritative operator-semantics surface** — the operator matrix is where operator legality is defined. `primitive-types.md:347`'s "even with the same member set — is a compile-time error" is the blunter statement.
- **The subsequence condition is exactly the soundness boundary.** Two fields both declared `choice of ("low","high") ordered` have identical declaration-position rank — comparing them is perfectly sound and genuinely useful (two priority fields on the same scale). `primitive-types.md`'s "never" throws that away for no reason. The spec's "same element type + order-preserving subsequence" permits *precisely* the well-defined cases and rejects the meaningless cross-domain ones the impl currently allows.
- **Philosophy check:** the subsequence gate doesn't hide behavior — you already must know a choice field's declared order to read *any* comparison (`field < "high"` is equally order-dependent). A failed subsequence check is a *compile-time type error* (visible, deterministic). Rank stays declaration-position and inspectable.

**The caveat (my one reservation, and where AI-legibility bites):** the subsequence rule costs real type-checker machinery — computing order-preserving-subsequence relationships between two fields' declared orders — and it's the more subtle mental model for an AI consumer to reason about. If you want to minimize MVP type-checker surface, there's a **defensible third option: adopt the strict subset "identical declared order → comparable, else error"** as an interim, with the full subsequence rule as the documented target. That's sound, covers the common case, and is trivially legible. But I'd rather *not* narrow the spec: commit to the full §3.6 rule as canon and fix `primitive-types.md:347` to match, so we don't have to re-widen later.

**Weight:** spec §3.6 wins over `primitive-types.md:347`. Tighten the impl to the subsequence check regardless of ruling (that's a separate soundness bug — flag it into the conformance track).

**Tradeoff:** more type-checker complexity and a subtler rule for AI consumers, in exchange for not amputating legitimately-sound comparisons. **Tier-3** — this edits locked `primitive-types.md` text and *expands* what the language accepts vs. that doc's current "never," so it genuinely needs your sign-off, not just mine.

---

## D3 — Fault delivery: **Reading B (return `EventOutcome.Faulted`). Strong — this is barely a fork anymore.**

The ledger frames this as balanced, but when I read the two sides against each other the asymmetry is stark:

- **Reading B is fully, recently specified.** `result-types.md` has `Faulted(Fault)` as the 8th variant *in the sealed hierarchy code block* (`:104`), *in the variant table* (`:117`), with a dedicated "**`Faulted` semantics**" paragraph explaining it "surfaces this as a structured outcome variant rather than a raw exception at the runtime boundary," *plus* MCP serialization (`{ "outcome": "Faulted", "fault": {...} }`). `evaluator.md:1402` states flatly "Faults are **never thrown**" and tags the returned-outcome model with a canonical-concern reference — "**CC#12**" (`:1404`) — which is design-constraint language, i.e. a *decision*, not a musing. `runtime-api.md:172` lists `Faulted` as a `Create()` outcome.
- **Reading A is residual prototype language.** The throw-side survives as *two top-of-doc sentences* (`result-types.md:51`, `:72`), one escape-hatch line (`runtime-api.md:885`), and `fault-system.md` — but `fault-system.md` itself **disavows** it: Q1 (`:301`) still calls it open, and its own Deliberate Exclusions say "**`FaultException` type — not yet implemented.**" Q1 even notes "The prototype uses exceptions (`ConstraintViolationException`)" — i.e. the throw framing is inherited from the prototype, not chosen.

**AI-first / architecture grounding (this is where I plant the flag):** the whole `[StaticallyPreventable]` design is a *mirror* — every `FaultCode` links to the `DiagnosticCode` that should have prevented it (`fault-system.md` Cross-References). A fault is defense-in-depth for out-of-contract data (`fault-system.md` Q3; spec §0.7). Modeling it as a **pattern-matchable, serializable outcome variant** is squarely on the "structured output over prose" and "tool surface is architecture" commitments — `precept_fire` returns `{ "outcome": "Faulted", ... }`, which an agent handles in the same exhaustive match as every other outcome. Throwing an exception across the runtime boundary is the *opposite* of AI-legible: it forces every call site into try/catch and puts fault detail outside the structured result. It also aligns with the ratified **Decision A** DU philosophy in the proof ledger ("use discriminated unions for varying shapes; don't paper over with nullable fields").

**Ruling: B.** Execute the ledger's B-sweep — delete the throw/outside-hierarchy language at `result-types.md:51/72/428`, `runtime-api.md:885`, `fault-system.md:8/95/337`, and **close Q1** (`fault-system.md:301`). Note `result-types.md` carries *both* readings today, so it needs an edit either way — B just makes the top-of-doc sentences match the body that already exists.

**Tradeoff:** you lose the "exceptions are simpler for a rare path" ergonomic (Q1's stated pro of the exception option). Accept it — the rare-path argument doesn't outweigh a uniform, serializable, pattern-matchable outcome model on an AI-first product.

**Tier status:** technically Tier-3 (it settles a locked-but-contested runtime shape, and `fault-system.md:301` still calls it open, so I want your explicit "yes, close Q1 as B"). But don't agonize — the docs have already voted B with their feet; you're ratifying, not deciding from scratch.

---

## D4 — `collection-types.md` Status: **Option (b) — add an explicit `| Implementation state |` row. My recommendation, with exact wording.**

Per CLAUDE.md, "Canonical design" = "grounded in the implementation." The doc's Status table (`collection-types.md:9`) is the first thing any reader — human or agent — hits, and it presents the *whole* doc as implementation-grounded while the body (`:801`) says quantifier predicates are "Not yet implemented." Count-bounds/cardinality and `set`/`lookup`/`log`/`queue` genuinely compile today; quantifier predicates don't. The split is real and intended — but it's buried ~800 lines down.

**My AI-legibility standard for a Status field:** a Status field exists to let a reader route *without reading the body*. If an agent must scroll to `:801` to learn that a headline capability isn't built, the Status field has failed its one job. Relying on scattered in-body "Not yet implemented" markers (option a) is exactly the structured-output-first violation I reject elsewhere. The runtime docs already solved this honestly — `runtime-api.md:8` says "Partial stub…". Collections deserve the same treatment.

Keep "Canonical design" for `Doc maturity` (the *design* is locked and grounded — that's true), and **add a sibling row**:

```
| Implementation state | Partial — built & compiling: cardinality/count-bounds (PRE0136), inner-type value modifiers, parser support, set/lookup/log/queue. Not yet built: quantifier predicates (each/any/no) — see § Quantifier Predicates. |
```

That preserves the accurate design-maturity claim while making the build split legible at the top, matches the runtime-doc pattern, and needs no new locked decision (the ledger confirms none governs this field). Option (a) is defensible but fails the "honest to a skimming AI reader" bar, and I won't sign off on the weaker option when the stronger one costs one table row.

---

## Cross-cutting: interactions & readiness-plan bearing (read before you rule)

1. **D1 + D2 = GATE-J** in the proof-engine ledger (`proof-engine-decision-ledger-2026-07-12.md:84`), listed **STILL OPEN, "pick the canonical readings; the fixes follow."** Neither blocks the proof-engine MVP. Ruling them just unblocks the conformance-track sweeps. No sequencing risk.

2. **D3 = GATE-I** (`:82`): **STILL OPEN, "needs your pick before the runtime build, doesn't block the MVP."** So D3 is a **runtime-build precondition, not a proof-engine readiness blocker.** You can rule it now or defer it to the runtime phase — it does *not* gate the current v3 proof-engine work.

3. **D3 has no bearing on the readiness plan's fault-handling *obligations*.** The proof engine's job is to make faults on *contract* data impossible (spec §0.7; `fault-system.md` Q3 — a fault on contract data is a *proof-engine gap*, a bug to fix). Whether the residual out-of-contract fault is *thrown* or *returned* is purely a delivery mechanism downstream of the proof boundary; it doesn't change a single proof obligation, certificate requirement, or structural-severity flip. So ruling D3=B is safe w.r.t. the readiness plan — and in fact *reinforces* its direction: the returned-`Faulted` model is the same DU/structured-output philosophy as the ratified `ProofVerdict` DU (Decision A) and the `[StaticallyPreventable]` mirror the plan already leans on.

4. **⚠️ Naming-collision warning so you don't cross wires:** the proof-engine ledger has its *own* "D1" — **"D1 overflow-park" (GATE-A)**, a settled overflow decision — and **GATE-O** ("∞/NaN, integer-conversion overflow") "sits inside D1's parked lane." That "D1" is **not** this ledger's D1 (sqrt). When you rule this ledger's D1(sqrt)=A, you are **not** touching GATE-A/GATE-O. Two different D1s; keep them separate when you write the ruling down.

---

**Summary of my recommendations:** D1 → **A** (widen; fix `spec:1586` to bar decimal only). D2 → **A** (spec §3.6 subsequence rule is canon; fix `primitive-types.md:347`; tighten the impl either way; interim identical-order-only is an acceptable fallback if MVP type-checker cost bites). D3 → **B** (return `Faulted`; close Q1). D4 → **(b)** (add `Implementation state` row). D1/D2/D3 all touch locked text → **Tier-3 sign-off from you**, but none is a genuine coin flip — the weight is clear on every one.
