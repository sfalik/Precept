# Authored expressiveness gaps — what the sample authors annotated as compiler workarounds

**Status**: Draft — 2026-07-20. Evidence input to the matrix's ratification protocol, layer 4 (`docs/Working/obligation-discharge-matrix-2026-07-19.md`, "Respellability is measured, not asserted"). Not a cell file, not canon, not a ratification verdict.

**Read against**: `docs/Working/obligation-discharge-matrix-2026-07-19.md` (rev 7) and `docs/Working/normal-form-draft-2026-07-19.md`.

**Why it exists.** The corpus measurement that reported zero "no licensed respelling" cases reads `samples/` — a folder filtered by a test asserting every file compiles. Programs that would have demonstrated expressiveness loss were rewritten until they passed, so the measurement scores the rewrite as a success. What survives of the original intent is the authors' own comments about why the code has the shape it has. This document sweeps every one of those comments and judges each against the proof-engine definition as now ruled.

---

## Summary

**Sweep**: all 78 `samples/*.precept` files, five independent grep passes over comment lines (proof engine / compiler / narrowing / interval / prove; "for now" / "until" / "does not currently" / "limitation" / "workaround"; guard-and-row vocabulary; "parallel" / "iterating" / "aggregate"; "static" / "derive" / "infer" / "conservative"). The passes saturate — the last two surfaced nothing the first three had missed.

**Counts**:

- **~25 comments name the proof engine, the compiler, or static proof at all.** Most are ordinary design commentary or praise (the engine *does* something for the author). They are listed and excluded in "Excluded" below.
- **9 comments record a code shape chosen because of the compiler.** These are the candidate workarounds.
- **5 are genuine gaps** against the proof surface. The other 4 are excluded with reasons (one is historical — the workaround was already removed when the surface shipped; two are about the expression language, not the proof engine; one is a correctly-rejected construct the author agrees with).

**Classification of the 5 genuine cases**:

| Case | Classification |
|---|---|
| 1. `production-order-tracking.precept:27-29`, `:136-137` — divisions guarded because "the proof engine cannot follow" an unwritten invariant | **CLOSED BY THE DEFINITION** — and, measured at HEAD, already closable today. Regression witness. |
| 2. `shopping-cart.precept:44-47` — bound "expressed as a rule rather than a structural max" | **SOMETHING ELSE: the workaround concealed an unsound program.** The definition closes it by *rejecting* the file. Also the defect in Part 3. |
| 3. `shopping-cart.precept:185-186` — bound checks split into per-check rejection rows | **REAL EXPRESSIVENESS COST** — friction only. Legitimate respelling exists; intent is not lost. The annotation also misdiagnoses which half of the workaround is load-bearing. |
| 4. `inventory-item.precept:135-139` — state-ensure facts not narrowed across a transition body | **SOMETHING ELSE: the annotation is misplaced, but names a real limitation the definition leaves OPEN.** The guard it sits on is correct and required. |
| 5. `statistical-process-control.precept:43-45` — no enforceable `maxlength` on an interpolated string | **NOT A GAP** in the proof engine — an accepted consequence of the §0.7 declared-bound rule, and the author says so. Recorded because it is the corpus's only annotated *accepted* loss. |

**Defect confirmed (Part 3), plus a second, worse one found while reproducing it.**

- **Defect A** — `rule` mints no write-site obligation at HEAD. `max 1000` on a field mints an `IntervalContainment` obligation and rejects; `rule Total <= 1000` mints nothing and compiles clean, *including* on a write of a literal 5000. `shopping-cart.precept` therefore ships two unenforced constraints (`:53`, `:54`), and its `ApplyPromotion` handler can drive `DiscountPercent` to 200 in two events with nothing flagging it.
- **Defect B (more serious)** — a `rule` is *consumed* as a proof premise while nothing establishes or preserves it. `rule PlannedQuantity > 0` discharges a division-by-zero obligation via the `CompositionalConstraint` strategy, so a program that sets `PlannedQuantity = 0` on one row and divides by it on another compiles with **zero diagnostics**. This is a live fail-open on a fault the engine claims to prevent, and it is exactly the "open dependency" the matrix's premise-(d) validity argument names (`obligation-discharge-matrix-2026-07-19.md:193`).

Both are in Part 3, with reproductions.

---

## Method and its limits

Every diagnostic and obligation quoted below is literal output from `Compiler.Compile` at HEAD (branch `spike/Precept-V2-Radical`, working tree at the session's start commit `26ed4d68`), run through a throwaway console host in the scratchpad that prints `Compilation.Diagnostics` and `Compilation.Proof.Obligations` verbatim. **The precept MCP server could not be used**: every `precept_compile` call returned `PRE0149 … FileNotFoundException: Could not load file or assembly 'NodaTime, Version=3.3.3.0'` — a stale-build failure in the MCP host, not a compiler defect. No sample, source, or research file was modified; the modified copies used for measurement live in the scratchpad only.

---

## Part 2 — the genuine cases

### Case 1 — `production-order-tracking.precept`: an invariant the author knew but never declared

**(a) Business intent.** A production order's planned quantity is fixed positive at creation and never changes; yield is `produced × 100 / planned`, so the division is always safe.

**(b) The comment**, `samples/production-order-tracking.precept:27-29`:

> `# Quantity and yield. PlannedQuantity is initialized positive at construction`
> `# and never changes; the proof engine cannot follow that across all transitions,`
> `# so the divisions below are explicitly guarded with `when PlannedQuantity > 0`.`

and `:136-137`:

> `# Batch production — accumulates against planned quantity. The success row`
> `# guards `PlannedQuantity > 0` explicitly so the division below is safe.`

**(c) The workaround and its cost.** Four dedicated rejection rows exist only to carry the `PlannedQuantity <= 0` complement — `:138-139`, `:151-152`, `:220-221`, `:231-232` (8 lines) — and a `PlannedQuantity > 0` conjunct is repeated on six success-row guards (`:142`, `:155`, `:160`, `:222`, `:233`, `:240`). The invariant itself is never written down; `:30` declares only `field PlannedQuantity as integer default 0 nonnegative`, and the positivity fact lives solely in `on Create ensure Create.PlannedQty > 0` (`:111`).

**(d) Does the definition close it? Yes — and it is already closable at HEAD.** Premise class (d) makes every constraint holding in the pre-state available as a fact (`obligation-discharge-matrix-2026-07-19.md:38`), so an author-declared `rule PlannedQuantity > 0` becomes usable at each division site. Measured:

```
=== rule PlannedQuantity > 0, no guard ===
--- diagnostics ---   (none)
kind=Numeric disp=Proved strategy=CompositionalConstraint desc=Divisor must be non-zero
```

Applying the respelling to the real file — add `rule PlannedQuantity > 0`, delete the four rejection rows, drop the six guard conjuncts — compiles with **`diags=0 obligations=16`** and is 8 lines shorter (239 vs 246). Under the definition, the rule's own preservation obligation attaches to its single write site (`:115`, `set PlannedQuantity = Create.PlannedQty`) and discharges from premise class (b) against `on Create ensure Create.PlannedQty > 0`.

**Classification: CLOSED BY THE DEFINITION.** This is a clean regression witness: the respelled file must compile clean, and — unlike today — must do so *soundly*, because the rule will carry its own preservation obligation. Note the sting: the author wrote a comment apologising for the engine when the engine already had the answer; what was missing was the declaration, not the proof power.

---

### Case 2 — `shopping-cart.precept`: a bound demoted to a `rule`

**(a) Business intent.** A cart may not exceed 1,000 total items, and combined promotional discount may not exceed 100 percent.

**(b) The comment**, `samples/shopping-cart.precept:44-47`:

> `# Order metrics. ItemCount and DiscountPercent are computed from lookup`
> `# values whose intervals the proof engine cannot narrow, so the bound is`
> `# expressed as a rule rather than a structural max.`

**(c) The workaround and its cost.** `max` was dropped from the field declarations (`:48`, `:50`) and re-expressed as `rule ItemCount <= 1000` (`:53`) and `rule DiscountPercent <= 100` (`:54`). Cost as the author saw it: two lines swapped for two lines. Actual cost: total loss of enforcement (Part 3).

**The premise is half-right.** Restoring `max 1000` / `max 100` to the two fields makes the file fail with six `NumericOverflow` errors at `:125`, `:132`, `:154`, `:168`, `:205`, `:213`. Three of those (`:125`, `:154`, `:168`) do read `ItemQuantities for …`, and the computed interval really is `[−∞ .. +∞]` — so the author's claim about lookup-value intervals is accurate at HEAD. It is accurate *even when the bound is declared*: `field Q as lookup of string to integer nonnegative max 100` parses and mints its own containment obligation, but a read `Q for K` still yields `[−∞ .. +∞]`. That matches the normal-form draft's own out-of-scope list — "accessor-projected bounds … yield an explicit skipped-obligation record" (`normal-form-draft-2026-07-19.md`, open item 8).

**But three of the six are not lookup reads at all.** `:132` (`ItemCount + AddItem.Quantity`, computed `[… .. 1100]`), `:205` (`DiscountPercent + ApplyPromotion.DiscountPercent`, computed `[… .. 200]`) and `:213` are plain field-plus-arg sums, and they overflow the bound because **the program genuinely admits the violation**. `ApplyPromotion` (`:103`) takes `DiscountPercent as decimal positive max 100`; the handler at `:203-207` has no guard on the sum; two 60-percent promotions leave `DiscountPercent = 120`, violating the file's own `rule DiscountPercent <= 100`.

**(d) Does the definition close it? Yes — by rejecting the file.** Under the ruled definition the rule mints a preservation obligation at `:205`; premise (d) gives `DiscountPercent ≤ 100` and premise (b) gives the arg `≤ 100`; the interval derivation yields `≤ 200`, which does not close `≤ 100`. Correct outcome: reject, with the suggestion schema naming classes (c) and (b). The respelling — a guard `when DiscountPercent + ApplyPromotion.DiscountPercent <= 100` on the success row plus a rejecting sibling — is licensed by N13 guard-fact coverage.

**Classification: SOMETHING ELSE — a workaround that concealed an unsound program.** It is not a band member: the program is not sound-but-unprovable, it is unsound. It converts into two witnesses: the file as written must be rejected once constraint obligations exist, and the guarded respelling must compile clean.

---

### Case 3 — `shopping-cart.precept`: bound checks split into per-check rejection rows

**(a) Business intent.** Reordering a cart line requires a non-empty cart and two in-range indices; each failure gets its own message.

**(b) The comment**, `samples/shopping-cart.precept:185-186`:

> `# Reorder item — each bound check is its own rejection row so the proof engine`
> `# can see the exact guard that enables the success row.`

**(c) The workaround and its cost.** Three rejection rows (`:187-192`, 6 lines) plus a success row whose guard restates all three conditions positively (`:193`: `when ReorderItem.FromIndex < LineItems.count and ReorderItem.ToIndex < LineItems.count and LineItems.count > 0`). Three checks, written twice.

**The annotation misidentifies which half is load-bearing.** Measured on a reduced reproduction: one *combined* rejection row plus an unguarded success row fails with four `IndexBoundsGuard` errors; one combined rejection row plus the *explicit positive guard* on the success row compiles clean, with all five obligations `Proved strategy=GuardInPath`. So the row-splitting was never required — only the duplicated positive guard was. The real cost is the duplication, not the three rows (which are, independently, good diagnostics).

**(d) Does the definition close it? No.** The premise classes are field modifiers, arg constraints, the handler's *own* guard, and pre-state constraints (`obligation-discharge-matrix-2026-07-19.md:38`). Nothing licenses "the negation of an earlier row's guard" as a fact. Row ordering appears in the matrix only as a structural fact kind (`DominancePath`, `:141`), which mints no premises.

**Classification: REAL EXPRESSIVENESS COST — friction, not intent loss.** This is the first band member drawn from authored code rather than a model-derived example. The respelling (restate the conjunction positively on the success row) is legitimate and preserves the business intent exactly; the cost is that a three-part condition is written twice, once negated and once not. It belongs in the sound-but-unprovable band with **respellable: yes**. If the definition ever wants to close it, the shape needed is a premise class for the complement of dominating rows' guards — which would require the row-ordering semantics to be admitted into the proof surface, and is not proposed here.

---

### Case 4 — `inventory-item.precept`: state-ensure facts across a transition body

**(a) Business intent.** A listed product always has a SKU (`in Listed ensure Sku is set`, `:93`), so reading `Sku` while publishing should not need a presence guard.

**(b) The comment**, `samples/inventory-item.precept:136-139`:

> `# Sku presence is guaranteed by the Listed state ensure, but the proof engine`
> `# does not currently narrow state-ensure facts across a transition body. (Event-`
> `# ensure narrowing is in place; state-ensure narrowing across transitions is a`
> `# separate, still-open limitation.) Guard explicitly until that gap closes.`

**(c) The workaround and its cost.** `when Sku is set` on `:140` plus a sibling rejection row at `:144-145` (2 lines).

**(d) The annotation is misplaced at this site.** The transition is `from Unlisted on Publish` — the entity is in `Unlisted` when the body runs, and `in Listed ensure Sku is set` says nothing about `Unlisted`. Removing the guard produces exactly the right error:

```
Error Proof UnprovedPresenceRequirement L142: Cannot prove that 'Sku' is present
  (used on event 'Publish' from state 'Unlisted') — guard with 'when Sku is set',
  initialize it earlier, or make it required
```

The guard is correct, required, and not a workaround. Assuming a target state's ensure inside the body that must *establish* it would be circular.

**The limitation the comment names is nonetheless real, at a different site.** A read *during residency* also fails to consume the residency ensure. Minimal reproduction:

```
state Unlisted initial
state Listed
in Listed ensure Sku is set because "listed items carry a sku"
from Listed on Touch
    -> set Desc = Sku
```

→ `Error Proof UnprovedPresenceRequirement: Cannot prove that 'Sku' is present (used on event 'Touch' from state 'Listed')`.

**Does the definition close that? No — it is explicitly OPEN.** The matrix, `:135`: *"**Open**: whether the residency fact — the entity is in `S` — is itself available as a premise, and under which class, is not ruled."* The constraint-kind ruling brings state-residency ensures onto the proof surface as *obligations* (establishment at every entry, preservation during residency); it does not make the residency fact a *premise*. `inventory-item.precept` has no live instance of the during-residency read, so the corpus contributes no forced case here — only the annotation, pointing at the right question from the wrong line.

**Classification: SOMETHING ELSE — misplaced annotation over a genuine open definitional question.** Recommend it be routed to the owner as evidence that the open residency-premise question has authored demand behind it, not as a band member.

---

### Case 5 — `statistical-process-control.precept`: a bound the author agrees cannot exist

**(b) The comment**, `samples/statistical-process-control.precept:43-45`:

> `# Free-form: assembled by interpolating measured `quantity` values (open-ended`
> `# magnitude + runtime unit), so no enforceable maxlength — the proof engine`
> `# correctly rejects a length cap here (§0.7: no result outside a declared bound).`

**(c)/(d).** No workaround was applied; a `maxlength` was simply not declared, and the author states the rejection is correct. The intent lost — "this string should have a length cap" — is lost to the §0.7 declared-bound rule, not to a proof-power gap: no premise could exist, because the magnitude is genuinely unbounded at compile time.

**Classification: NOT A GAP.** Recorded because it is the corpus's only annotated case of an author *accepting* a loss rather than routing around it, which is a data point about how the guarantee reads to an author.

---

## Excluded — annotations that turned out not to be workarounds

| Location | Why excluded |
|---|---|
| `insurance-claim-adjudication.precept:132-136` | Entry hooks described as a DRY choice; "the proof engine fires the hook on every inbound edge, so the state ensure … is satisfied automatically" — the engine helping, not being worked around. |
| `patient-referral-management.precept:8-10`, `:54-56` | `when … is set` on cross-field date rules. Required because the fields are `optional`; a language rule about presence (`primitive-types.md:574`), not a proof-power limit. The comment frames it as a positive design choice. |
| `non-profit-membership-renewal.precept:72-76` | Same shape — `is set` narrowing on optional fields in mutually-entailing rules. |
| `global-meeting-scheduler.precept:62-64`, `:88-93`, `:241-243` | Three separate *praise* comments: computed fields kept in lockstep, a `~string` set making a `not contains` obligation discharge correctly, and a state ensure removing the need for a row guard. |
| `saas-usage-metering-and-billing.precept:29-33`, `production-shift-downtime.precept:33-38`, `invoice-line-item.precept:9-12` | Computed-field commentary; all positive. |
| `production-shift-downtime.precept:53`, `calibration-management.precept:53`, `saas-subscription-billing.precept:51`, `crosswalk-signal.precept:21-23` | "Rules / state ensures — invariants the proof engine enforces." Design commentary. (At HEAD these claims are false for `rule` — see Defect A — but they are not workarounds.) |
| `contractor-invoice-settlement.precept:15-23`, `:109-110`, `:127`; `equipment-lease-agreement.precept:45-48`, `:206-207`; `event-venue-booking.precept:59-60`, `:209-210` | Guard-narrowing and dimensional-typing commentary; all describe the engine succeeding. |
| `production-order-tracking.precept:193-195` | "The Pass branch splits on whether `ProductionNotes` is already set so the concatenation can be proved safe." The two branches carry different business behaviour (start the note vs. append to it) and the field is genuinely optional before `Completed` (`:82`). Correct code, not a workaround. |
| `event-venue-booking.precept:17-19` | Names a workaround that was **already removed** when qualified inner types in collections shipped. Historical, and the only recorded instance of a surface change retiring a workaround. (It also violates the transient-reference rule — it cites workstream and finding labels in a shipped sample.) |
| `bill-of-materials-management.precept:29-32`; `academic-course-registration.precept:12-15` | Shadow accumulator fields (`TotalComponentUnits`, `TotalCreditHours`) added "so guards can reason about … without iterating." A genuine workaround, but around the **expression language** — there is no aggregate-over-collection expression — not around the proof engine. Out of scope for the matrix; worth its own note if the language surface is ever revisited. |
| `saas-customer-success.precept:58-62` | "Risk-level derivation — compile-time consistency between the level and the count of contributing factors," over three conditional rules. Not a workaround; but the claimed compile-time consistency does not exist at HEAD (Defect A). |

---

## Part 3 — the defects

### Defect A — a `rule` mints no write-site obligation; a `max` does

**Claim under test** (`shopping-cart.precept:44-47`): that expressing a bound as a `rule` rather than a `max` preserves the bound while avoiding the narrowing failure.

**Reproduction — bound as a modifier.** Compiles to an error:

```
precept BoundViaModifier
field Total as integer default 0 nonnegative max 1000
event Add(Amount as integer positive)
on Add
    -> set Total = Total + Add.Amount
```

```
Error Proof NumericOverflow L8: Numeric computation exceeded the representable range on field 'Total'
kind=IntervalContainment disp=Unresolved interval=[−∞ .. +∞] desc=Interval containment: Total must stay within declared bounds [−∞ .. 1000]
kind=Numeric disp=Proved strategy=Literal desc=Default value of 'Total' must satisfy 'nonnegative'
kind=Numeric disp=Proved strategy=Literal desc=Default value of 'Total' must satisfy 'max'
--- counts: diags=1 obligations=3 ---
```

**Reproduction — the same bound as a rule.** Compiles clean:

```
precept BoundViaRule
field Total as integer default 0 nonnegative
rule Total <= 1000 because "Total cannot exceed 1000"
event Add(Amount as integer positive)
on Add
    -> set Total = Total + Add.Amount
```

```
--- diagnostics ---   (none)
kind=Numeric disp=Proved strategy=Literal desc=Default value of 'Total' must satisfy 'nonnegative'
--- counts: diags=0 obligations=1 ---
```

**The rule form is not merely weaker — it is inert.** A write of a literal that violates it is accepted:

```
precept RuleViolatedByLiteral
field Total as integer default 0 nonnegative
rule Total <= 1000 because "Total cannot exceed 1000"
on Bump
    -> set Total = 5000
```

```
--- diagnostics ---   (none)
--- counts: diags=0 obligations=1 ---
```

whereas the identical program with `max 1000` yields `Error Proof NumericOverflow L8` and `IntervalContainment disp=Unresolved interval=[5000 .. 5000]`.

**Root cause.** `src/Precept/Language/ProofRequirement.cs` declares no invariant-preservation requirement subtype; `rule` reaches the proof engine only through the satisfiability scan (`src/Precept/Pipeline/ProofEngine.Satisfiability.cs:361`, `VacuousRule` / `ContradictoryRule`). Constraint obligations are unbuilt — which the matrix already states as committed-but-not-built power. The *defect* is not that the engine lacks the machinery; it is that a shipped sample records, in a comment, that a bound was preserved by a spelling that at HEAD enforces nothing.

**Consequence in the sample.** `shopping-cart.precept:53` and `:54` are unenforced. The reachable violation is concrete: `ApplyPromotion` (`:203-207`) has no guard on `DiscountPercent + ApplyPromotion.DiscountPercent`, and the arg is capped at 100 (`:103`), so two promotions of 60 percent leave `DiscountPercent = 120` against `rule DiscountPercent <= 100`. Restoring `max` to the two fields surfaces this immediately as `NumericOverflow L205` with computed interval `[… .. 200]`.

A second observation from the same run: with `max` restored, the two rules are reported as `VacuousRule … is always true under the declared constraints — it governs nothing`. So the compiler *does* read `rule` as a declared constraint for vacuity analysis while minting nothing for it at write sites — the asymmetry is internal to the engine, not just a missing feature.

### Defect B — a `rule` is consumed as a premise while nothing establishes it (fail-open)

Found while measuring Case 1. The division-by-zero prover accepts `rule PlannedQuantity > 0` as a premise (`strategy=CompositionalConstraint`). Combined with Defect A — the rule carries no preservation obligation — the following compiles with **zero diagnostics**:

```
precept FailOpen
field PlannedQuantity as integer default 1 nonnegative
field YieldPercent as decimal default 0.0 nonnegative maxplaces 2
rule PlannedQuantity > 0 because "plan is positive"
from S on Zero
    -> set PlannedQuantity = 0
    -> no transition
from S on Compute
    -> set YieldPercent = 100.0 / PlannedQuantity
    -> no transition
```

```
--- diagnostics ---   (none)
kind=Numeric disp=Proved strategy=CompositionalConstraint desc=Divisor must be non-zero
kind=Numeric disp=Proved strategy=Literal desc=Default value of 'PlannedQuantity' must satisfy 'nonnegative'
kind=Numeric disp=Proved strategy=Literal desc=Default value of 'YieldPercent' must satisfy 'nonnegative'
--- counts: diags=0 obligations=3 ---
```

A division by zero is reachable, and the compiler certifies the divisor non-zero. This is worse than Defect A: A is a missing check, B is an *affirmative false proof* on a fault the engine claims to prevent.

This is precisely the open dependency the matrix's own validity argument names — "the hypothesis is only as sound as the minting rule is complete … a missed write site voids the hypothesis for every cell that consumes premise (d)" (`obligation-discharge-matrix-2026-07-19.md:193`). At HEAD the minting rule for `rule` is not incomplete; it is empty. Every existing `CompositionalConstraint` discharge in the corpus rests on it.

**Not filed.** Per instruction, nothing was written to `docs/Working/bugs.md` and no sample was edited. Both defects need owner routing; Defect B in particular should be checked against the fail-open ledger before the next slice consumes premise (d).

---

## What this evidence can and cannot support

**It can support**: existence. Three of these cases are proofs by example that the shape exists in authored code — an author writing a Precept file reached a point where the compiler's proof power, not the business domain, determined the code they wrote, and said so in a comment. Case 3 is the first band member the project has that was not model-derived. Cases 1 and 2 are conversion-ready witnesses. Defects A and B are reproducible facts about HEAD, independent of any judgment in this document.

**It cannot support**: prevalence, coverage, or representativeness, in any form.

- These are **documentation samples authored by this project** — written to demonstrate language features, by authors who knew the compiler's internals well enough to name `strategy` behaviour in comments. They are not what a domain expert writes.
- The corpus is **filtered by construction**: `samples/` is gated by a test asserting every file compiles. Programs whose intent had no licensed spelling were rewritten or abandoned before they reached the folder. The absence of a "no licensed respelling" case in this sweep is therefore **not evidence that none exists** — it is evidence that none survived the filter with an annotation attached.
- The annotations are **prose and voluntary**. An author who worked around the compiler without commenting leaves no trace. The sweep found what was written down, not what was done.
- Two of the nine candidate annotations turned out to **misdiagnose their own cause** (Case 3's row-splitting, Case 4's guard placement). Author annotations are testimony, not measurement; each one here had to be re-measured against the compiler, and several did not survive.

**Therefore**: this document must never be cited as corpus coverage for the matrix's ratification layer 4. It is an input to that measurement — a small set of forced cases with judgments attached — and the measurement's non-exhaustiveness stands unchanged.
