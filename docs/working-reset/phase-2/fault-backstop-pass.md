# Phase 2 — the runtime fault-backstop pass

**Run** 2026-07-28 on top of HEAD `b586ab0b`, branch `spike/Precept-V2-Radical-reset`. Nothing here is
committed. No `.precept` file was compiled, and no catalog, sample or compiler behaviour is cited as
evidence. `docs/philosophy.md` was not edited.

**What this pass is.** It executes the nineteen rows the register lists at
[`contradiction-register.md`](contradiction-register.md) `:292–315` — `R9`, `N22`, `N23`, `R10`, `N24`,
`N25`, `R14`, `N26`, `R11`, `R12`, `N27`, `R13`, `N28`, `N29`, `C1`, `C2`, `N30`, `N34`, `N35` — as one
group across all nine documents they touch. The group had been deferred four times because each earlier
pass owned only two or three of the nine.

**The authority is the decision sheet, not the rows.** Where
[`fault-backstop-model.md`](fault-backstop-model.md) and a register row disagreed, the sheet won. Its § 6
says which rows are right as written, which need correcting first, and which describes an edit that must
not be made. Section 4 below records every place a row was followed with a correction and what the
correction was.

**Seventeen documents changed**, 183 lines added and 435 removed. Nine are the documents the rows name;
eight are documents outside them that the sheet's § 3, this pass's own sweep, or the register's two
unassigned `docs/tooling/` rows put in scope. Two tracking documents were also updated —
[`contradiction-register.md`](contradiction-register.md) and
[`compiler-readiness-plan.md`](../compiler-readiness-plan.md); see § 9.

---

## 1. What the ruling means, applied

The owner ruled on 2026-07-28: *"a runtime that does not fault is what I want."* That is now in the locked
philosophy at `philosophy.md:46` — "The engine does not fault" — and `precept-language-spec.md:112`
already carried it in canon's own voice: "Runtime fault checks do not exist."

Two consequences, and the second is the one that is easy to get backwards.

**Nothing constructs a fault.** No annotation is planted at any site, no descriptor carries one, no builder
pass materializes them, no opcode holds one, and the evaluator runs no fault-prevention check. Every
mechanism that did any of that is gone from the documents.

**`Fault`, `FaultCode` and `EventOutcome.Faulted` are not deleted.** They stay declared with no producer,
because `compiler-and-runtime-design.md:826`, `:865` and `:874` are already-corrected canon and say so.
What changed is the *attribution*: `:706` and `:824` named the Evaluator as the producer, and that is what
went. `FaultCode` and `[StaticallyPreventable]` survive as the compiler's own vocabulary — each member
names a class of failure and names the diagnostic responsible for preventing it, checked when the project
is built, with nothing planted at a site and no evaluation-time path.

**Engine defects stay exceptions.** `precept-builder.md` § Builder Invariant Violations and `evaluator.md`
§ No Unclassified Exceptions already said a corrupted model is an unrecoverable exception that should not
be caught. That is now the only account of an engine defect that describes a mechanism, and nothing
competes with it. Whether the evaluator should instead *detect* such a breach and report it as a `Fault`
is left open and given an owner — see § 5.

---

## 2. The nineteen rows

### `R9` — the planting contract

| | |
|---|---|
| Site | `precept-builder.md:426` (the contract), `:428–436` (the `FaultSiteAnnotation` declaration), `:438–453` (the planting pseudocode), `:607` (the "when reached, they fire" paragraph) |
| Before | ":426 stamps `FaultSiteAnnotation` on opcodes from unresolved proof obligations … For each `TypedExpression` being compiled, the builder checks whether `ProofLedger.FaultSiteLinks` contains a link whose `Obligation.Site` matches the expression." Followed by the record declaration and a `CompileExpression` code block that performs the match. |
| After | One paragraph in Pass 4: "**Nothing is stamped on an opcode for a fault.** An obligation the proof engine could not discharge rejects the definition, so no `Compilation` reaching the builder carries an unproven site, and there is nothing for the builder to annotate." |
| Resolution | Deletion. The declaration and the code block went with the two sentences — correcting only the sentences, which is what the row's own site list would have produced, leaves the mechanism standing. |

### `N22` — Pass 6

| | |
|---|---|
| Site | `precept-builder.md:34` (contents entry and anchor), `:115` (pass-list item 6), `:132` (responsibility row), `:154` (LOC itemization), `:283–285` (diagram box title and two of three bullets), `:304` (dependency row), `:591–595` (heading and body) |
| Before | "Pass 6 — Fault Backstop Pass … materialize `Precept.FaultBackstops` from per-opcode `FaultSiteAnnotation` entries; build `ConstraintInfluenceMap`" |
| After | "Pass 6 — Constraint Influence Pass … build `ConstraintInfluenceMap` from `ProofLedger.ConstraintInfluenceEntries`" |
| Resolution | Re-scope. **The pass survives; its name does not.** The name was replaced in all five places it appeared, plus the contents anchor. `ConstraintInfluenceMap` construction — the pass's other half, which `:593` stated in its own words — is untouched. |
| What survived, and why | The pass count stays **six**. `:26`, `:108`, `:232`, `:234`, `:306` and `:980` all say "six passes" or state the order `1 → 2 → 3 → 4 → 5 → 6`; none changed. Verified after editing: `grep -n "six\|Pass 6" docs/runtime/precept-builder.md` returns eight lines, all consistent. |

### `N23` — `Precept.FaultBackstops`

| | |
|---|---|
| Site | `precept-builder.md:224` (the property), `:653` (evaluator's consumed list), `:728` (output-contract invariant); extended to `evaluator.md:317` and `:1443`, which `N23` and `N24` both missed |
| Before | `internal ImmutableArray<FaultSiteDescriptor> FaultBackstops { get; }` on the `Precept` class; "Every `FaultSiteAnnotation` (on opcodes) and `FaultSiteDescriptor` (on `Precept.FaultBackstops`) has a `FaultCode` with a `[StaticallyPreventable(DiagnosticCode)]` attribute" |
| After | Both removed. The evaluator's consumed-surface list and its `Precept` input description now read `SlotLayout`, `TransitionDispatchIndex`, `ConstraintPlanIndex`, `ExecutionRow`, `ExecutionPlan` — the two copies of that list, at `precept-builder.md:653` and `evaluator.md:1443`, changed together. |
| Resolution | Deletion. |

### `R10` — fault backstop routing

| | |
|---|---|
| Site | `evaluator.md:147`, the In-Scope responsibilities table |
| Before | "\| **Fault backstop routing** \| At impossible-path sites: check `opcode.FaultSite` (CC#6 `FaultSiteAnnotation?`); null = no check; non-null → `Faults.Create(annotation, context)` \|" |
| After | Row removed. |
| Resolution | Deletion. |

### `N24` — the CC#6 contract, and CC#12 with it

| | |
|---|---|
| Site | `evaluator.md:436` (the `// ── Fault Backstop ──` banner), `:981–983` (the dispatch check), `:989` (the two-layer paragraph), `:1419–1429` (the annotation-anchored fault-production block), `:1456` (the internal-dependency row), `:2072` (resolved question 10); plus `:338` and `:1433`, which carry the parallel `CC#12` numbering the row never mentions |
| Before | `if (opcode.FaultSite is { } annotation && IsFaultCondition(stack, top, opcode)) return Fail(annotation.Code, …);` inside the dispatch loop, and "**Two-layer defense model (CC#6):** The first line of defense is the compile gate … The second line is the runtime backstop." |
| After | The dispatch check is gone. The paragraph is one sentence: "**The loop runs no fault check.** The type checker and proof engine emit diagnostics for unresolved obligations, and no `Precept` is built until the author fixes them. There is no second pass behind that: an opcode carries no fault annotation to test, and the evaluator just computes." |
| Resolution | Deletion, with one sentence kept. `:989`'s first half was a complete correct sentence on its own and is kept as one, without "the first line of defense" and without the CC#6 label — the sheet's § 4 names it as the canonical replacement wherever a document said the compile gate is the first line of defence. |
| Line numbers | All five of the row's line numbers had drifted by 22 to 24 and were wrong. `:437`→`:436`; `:1005`→`:981–983`; `:1013`→`:989`; `:1443`→`:1419`; `:2094`→`:2072`. `:437` is the `Fail` declaration, which survives — following the row's number would have deleted the wrong line. |
| Structural | The Resolved Design Questions list ran 8, 9, 10, 11, 12, 13. Item 10 was the CC#6 threading question; the remainder are renumbered 8–12, so the list has no hole. |

### `N25` — Impossible-Path Failures, and the section the row named by mistake

| | |
|---|---|
| Site | `evaluator.md:19` (non-negotiable rule), `:56` (contents entry), `:1620` (in-domain table row), `:1624–1638` (heading and nine-row table), `:1640–1652` (the `FaultCode` excerpt), `:1677` (recovery row), `:1683` ("Produces a `Fault` via `Faults.Create` (for impossible paths)") |
| Before | "compiler proofs are **intended to** eliminate runtime fault paths, leaving evaluator faults as defense-in-depth only"; a section headed "Impossible-Path Failures (Defense-in-Depth)" with a nine-row table and an enum excerpt. |
| After | The non-negotiable rule now reads: "**A compiled definition has no fault path at evaluation time** — the compiler proves every fault-prone operation safe or refuses the definition, so the evaluator carries no fault-prevention check to run." The section, its table, its excerpt and its contents entry are gone. |
| Resolution | **Deletion, against the row's own instruction, on the sheet's ruling.** The row says "the section survives re-scoped." That is right about the `Faulted` variant and wrong about the section it names: the table's third column ("Should Be Prevented By") names an author premise or a compiler stage for all nine rows, and none of them describes a compiler or builder defect. |
| What survived, and why | A *different* section survives — **§ Fault Production** (`:1655–1670`), which the row does not mention. It declares `Fail`, states that `Faults.Create` looks up `FaultMeta`, and restates the `Fault` record. Its contents entry at `:57` is kept. What went instead is the duplicate block at `:1419–1429`, which stated the same thing anchored on the annotation; its content survives verbatim in § Fault Production. `:1431`'s "Faults are **never thrown**" is kept and moved into § Fault Production, where it is anchored rather than orphaned. **Corrected 2026-08-11**: the move was right and stands, but the sentence was kept on a test of whether it is true, and nobody asked whether stating it flatly presumes the answer to the producer question this pass was filing as open. It now states the outcome shape conditionally — see [`fault-outcome-wording.md`](fault-outcome-wording.md). |
| Also corrected | `:1708` read "Every `Fault` produced by the evaluator carries a `FaultCode` with a `[StaticallyPreventable]` attribute. If the compiler emits no errors, the evaluator should never fault." The first sentence described a production path that no longer exists; the second hedged with "should never" a guarantee stated absolutely everywhere else. Replaced with a flat statement plus the surviving build-time pairing. |
| Line numbers | `:1644`→`:1624`; `:1648`→`:1628`; `:2179`→`:2157`. |

### `R14` — the division row

| | |
|---|---|
| Site | `evaluator.md:1630` (the row's `:1654` is stale) |
| Before | "\| Division by zero \| `DivisionByZero` \| Guard `when Divisor != 0` or `nonzero` modifier \|" |
| After | Gone with the table. |
| Resolution | Deletion. **The identical reasoning removes all nine rows, not one** — every row in that table names an author premise or a compiler stage in its third column, so no row of it describes an engine defect. Executed as part of `N25`. |

### `N26` — `FaultSiteDescriptor`

| | |
|---|---|
| Site | `descriptor-types.md:8`, `:38`, `:106–110`, `:140`, `:157`, `:172`, `:203`; consumers `precept-builder.md:899`, `:981`, `evaluator.md:2194`; and `glossary.md:78`, outside the nine |
| Before | The record declaration, "planted by builder, read by evaluator backstops"; "uses `FaultSiteDescriptor` for backstop routing"; the Innovation bullet "**`FaultSiteDescriptor` as the proof/runtime link**"; "all 5 descriptor sealed records" |
| After | The declaration and all seven references are gone. "all 5" is now "all 4". The two definitions of "descriptor" that enumerated "fault sites" among what a descriptor represents — `descriptor-types.md:38` and `glossary.md:78` — no longer do. |
| Resolution | Deletion. |
| Structural note | `:172` was one of exactly three bullets under **Innovation**. Removing it is right, and the section is now two bullets — it makes its claim about first-class runtime identity with no proof-related example. |

### `R11` — the divide-by-zero example

| | |
|---|---|
| Site | `fault-system.md:246–261`; the second copy at `diagnostic-system.md:713–730`, which no row covers |
| Before | Step 1 was "Evaluator: `a / b` where `b == 0` → `Fail(FaultCode.DivisionByZero)`", and the five steps ran from the evaluator back to the compiler. |
| After | Both copies re-authored from the compiler side and renumbered to four steps, opening "Take a definition containing `a / b`, where the compiler cannot show `b` is non-zero." Both close on the point the example exists to make: the chain ends at a definition that does not compile, and a definition that does compile carries no divisor test. |
| Resolution | Re-authoring, not a deleted step. With step 1 removed the walkthrough started nowhere. |

### `R12` — the out-of-contract justification

| | |
|---|---|
| Site | `fault-system.md:263` |
| Before | "The chain means `FaultCode.DivisionByZero` is defense-in-depth: it classifies a failure that should never occur in a correctly compiled precept, but must be handled if data arrives from external sources that bypassed compile-time checking." |
| After | Removed, and replaced by the example's own closing paragraph. |
| Resolution | Deletion. Both halves of the justification are removed by the want document — `want:141` for restored data, `want:165` for the check. |

### `N27` — the Defense-in-depth section, and the half of Q3 that stays

| | |
|---|---|
| Site | `fault-system.md:30` and `:35` (two contents entries), `:284–286` (the section), `:322` (the paragraph inside Q3) |
| Before | "### Defense-in-depth, not a primary error path … `Fault` is therefore a defense-in-depth type … reachable only for **out-of-contract** data", restated almost verbatim at `:322`. |
| After | The section and its contents entry are gone. Inside Q3, only `:322` went. |
| Resolution | Deletion of the section; **narrowed** for Q3. |
| What survived, and why | **The assert-versus-fault question stays; the two lines that carried it do not.** Corrected 2026-07-28 — this row originally read "`:320` and `:324` stay", and that is not what happened. `:322` and `:324` were both deleted and the heading was retitled. `:324` — "This affects how aggressively the evaluator should assert vs. gracefully fault. Decision needed at evaluator design time" — was the only place in canon that recorded the question, so deleting Q3 whole would have erased it. It is not erased: the re-written Q3 body ends "Whether the evaluator asserts, faults, or does neither … is undecided", which states the same question against the corrected model. The substance is preserved and arguably better stated; the record was wrong and is fixed here so a reader checking the two sites the sheet named does not conclude the question was dropped. See § 3 item 3. |
| Correction to the row | The row names `:30` for one removed section's contents entry and misses its sibling. `:35` was added. It then turned out to be moot in the direction the sheet expected: Q3's heading survives, so its contents entry survives too — what changed is the heading's title, and the entry was updated to match. |

### `R13` — the three bound-containment members

| | |
|---|---|
| Site | `fault-system.md:149`, `:153`, `:156` |
| Instruction | Remove `OutOfRange`, `LengthBoundViolation` and `CountBoundViolation` from the `FaultCode` enumeration. |
| **Executed as** | **The members were not removed.** This is the one row the sheet marks as describing an edit that should not be made, and this pass followed the sheet. |
| Why | The row's reasoning is that these "come out of the fault enumeration with S11". S11 and its family were executed as *wording* corrections, not removals — N38's resolution is "Keep the `Proof` classification; re-word", and N40's cell says outright that the quoted phrase survives. Removing the three members would break compile-time coverage accounting: `soundness-and-coverage.md:234`, `:235` and `:236` disposition all three as **in**, with `IntervalContainment`, `LengthContainment` and `CountContainment` as their discharging obligation kinds, and `proof-engine.md:1737` names `CountBoundViolation`'s diagnostic as the emitting one for count containment. Those are three live prevention paths. |
| What R13 is really pointing at, and how it was executed | A **quoting rule, not a membership rule.** `precept-language-spec.md:112` says a value straying outside a declared bound "is not on that list" of fault classes, and `compiler-and-runtime-design.md:136` states the rule that holds: containment "is not a fault gate and should not be quoted as one." No document in this pass's scope quotes the three members as evidence that a declared bound is fault-prevented, so no re-wording was needed. The residual tension — three bound-containment members sitting in an enum the spec says bounds are not on — is answered by the quoting rule and needs no owner ruling. |
| Grep | `grep -rn "LengthBoundViolation," docs/runtime/fault-system.md` returns **1**, and must. |

### `N28` — the third enforcement layer

| | |
|---|---|
| Site | `runtime-api.md:670` (heading), `:672` (the sentence under it), `:678` (item 3) |
| Before | "### Three-Layer Enforcement Model / Precept enforces field constraints across three layers: … 3. **Defense-in-depth evaluator faults** — 15 `[StaticallyPreventable]` fault codes exist at evaluation time as a backstop." |
| After | "### Two-Layer Enforcement Model / Precept enforces field constraints across two layers:" with items 1 and 2 unchanged. |
| Resolution | Deletion, with both the heading and the announcing sentence changed in the same edit. The row cites `:660`, which is stale; the item is at `:678`. Removing the item alone would have left a three-layer announcement over two items. |

### `N29` — `Faulted`, re-scoped rather than deleted

| | |
|---|---|
| Site | The row names `runtime-api.md:887` and `result-types.md:72`, `:117`. Executed at `runtime-api.md:171` and `:906`, `result-types.md:51`, `:63`, `:115`, `:129`, `evaluator.md:338`, `fault-system.md:66`, `:95`, `precept-language-spec.md:1963`, and `evaluator.md:1620` |
| Before | "Faults — errors the type checker should have prevented … surface as the single `EventOutcome.Faulted(Fault)` variant"; "an evaluator fault surfaces as the `EventOutcome.Faulted(Fault)` variant — a backstop for impossible-path bugs"; "\| `Faulted` \| Evaluator impossible path … \| Backstop \| Yes \|" |
| After | Every description now reads as a breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator — and says that nothing constructs one today. |
| Resolution | **Re-scope, not deletion.** The variant stays. This is the one row in the family where deleting would have removed something the want document never asked to remove. |
| Two additions the row does not carry | `result-types.md:115`'s `Pipeline stage` cell read `Backstop`, which is not a phase from spec §3A.4 while every other cell in that column is. It is now `—`, because no §3A.4 phase produces the variant. And the same re-scope was needed at eight sites the row never names. |
| The one place the row's logic points the other way | `evaluator.md:1620` is a row in a table headed "All in-domain failures produce structured outcome variants". An engine defect is not an in-domain failure, so that row was **deleted** rather than re-scoped. Saying so explicitly is necessary; otherwise the row's instruction and the table's heading collide. |
| Line numbers | `runtime-api.md:887`→`:906`; `result-types.md:117`→`:115`. |

### `C1` — `FaultSiteLink` and the residue that crossed the boundary

| | |
|---|---|
| Site | `proof-engine.md:11`, `:107`, `:123`, `:136`, `:264`, `:322–335`, `:339–369`, `:426`, `:434`, `:475`, `:483`, `:1918`, `:1925–1940`, `:1946`, `:2211`, `:2219–2230`, `:2254`, `:2306`, `:2350`, `:2357`, `:2720`; plus `precept-builder.md:198`, `:634`, `:967`, `:989`, `soundness-and-coverage.md:269`, `diagnostic-system.md:541` |
| Before | "The Precept Builder consumes these to plant `FaultSiteAnnotation` backstops — defense-in-depth runtime checks for operations that could not be proven safe", the `FaultSiteLink` record, its subsection with the canonical annotation shape and the structural-elision note, the `ProofLedger` field, and the emission code that built the list. |
| After | The type, the field, the subsection, the builder-consumption contract's first item, both pipeline-diagram lines and every emission-code reference are gone. The `Prove` sketch no longer declares `faultSiteLinks`, no longer adds to it, and no longer passes it to the `ProofLedger` constructor. |
| Resolution | Deletion, with **one re-wording that would have broken a numbered list if deleted**: `:107`, key design choice 2 of three, read "only `FaultSiteDescriptor` residue (defense-in-depth backstops) crosses into runtime." Only the middle clause was false; it now reads "nothing from it crosses into runtime." |
| Line numbers | `:346`→`:352`, `:342`→`:341`, `:2210`→`:2211`, `:2435`→`:2452`, `:2667`→`:2674`. |
| Structural | `docs/runtime/precept-builder.md`'s § 8 Builder Proof-Consumption Contract in `proof-engine.md` had four numbered items; item 1 was the descriptor planting. Items 2–4 are renumbered 1–3. The `ProofLedger` record loses a field, and the `ProofLedger` inventory table loses its row. |

### `C2` — belt-and-suspenders

| | |
|---|---|
| Site | `proof-engine.md:2457` (Decision 2's "Defense-in-depth" bullet), `:2460` (Decision 2's "What crosses" first bullet), `:2678` (the same claim in Deliberate Exclusions), and **`:2446`** |
| Before | "`FaultSiteAnnotation` backstops are defense-in-depth — they should never fire if proof is correct. They exist for belt-and-suspenders safety, not as a proof continuation mechanism." |
| After | Bullet removed; "What crosses" now lists constraint execution plans alone; Decision 2's opening reads "Nothing from it crosses into runtime." In Deliberate Exclusions, the "Defense-in-depth backstops already provide runtime safety" reason is removed and the other two stand. |
| Resolution | Deletion, **except at `:2446`, which is a replacement**. |
| The replacement, and why it is not a deletion | `:2446` is item 3 of three under **Trade-off accepted** in Decision 1, a locked decision that `CLAUDE.md` requires to state its accepted trade-off. It read "Defense-in-depth backstops catch any runtime failures", which is now false. It reads: "An obligation no admissible strategy can discharge refuses the definition. That is the harder half of the trade-off and it is stated plainly: a program that is in fact safe, but safe for a reason no strategy can certify, does not compile." That is a harder trade-off than the one previously claimed, which is the honest replacement. **No register row covers this.** |
| What must not be read into the row | It is not licence to delete Decision 2 or the "No Runtime Obligation Checking" exclusion whole. Both carry true statements of the compile-runtime boundary; both have contents entries (`:62`, `:80`) that would be orphaned; and §11's decisions would have read 1, 3, 4, 5. Clause-level edits were made in all three places. |
| Line number | The quoted text is Decision 2's, at `:2457`, not `:2451` as the row's own correction says. |
| Grep | Returns **1**, at `type-checker.md:1133` — see § 6. |

### `N30` — the proof-flow diagram

| | |
|---|---|
| Site | `proof-engine.md:1918` (the "No runtime check needed" branch), `:1925–1940` (four diagram boxes) |
| Before | The Unresolved branch ran `DiagnosticCode → FaultCode → FaultSiteLink → FaultSiteDescriptor (defense-in-depth)`, and the Proved branch ended at "No runtime check needed". |
| After | The Unresolved branch runs `DiagnosticCode → Definition refused`; the Proved branch ends at "Nothing further". The section's lead-in now says the chain connects a catalog-declared requirement to the diagnostic that refuses the definition. Invariant 3 no longer claims a `FaultSiteLink` is produced. |
| Resolution | Deletion of four boxes, not one branch. |
| What survived | § Proof/Fault Chain at `:1900` and **its contents entry at `:40`**. Deleting the entry would have made a surviving section unreachable from the contents. |
| Line numbers | `:1932`→`:1937–1938`, `:1912`→`:1918`. |

### `N34` — the soundness document

| | |
|---|---|
| Site | `soundness-and-coverage.md:113–114`, `:232`, `:266–271` |
| Before | "(The `[StaticallyPreventable]` evaluator traps are defense-in-depth for out-of-contract entry, not an author-facing opt-out.)"; the `QualifierMismatch` row's "*(runtime fault-identity collapse to a single backstop is a separate runtime-only concern — see §3.2)*"; a note headed "Two known items". |
| After | The parenthetical is gone — its point, that there is no escape hatch, survives in the sentences above it at `:109–112`. The `QualifierMismatch` row reads **in** with no parenthetical. The note is one item and is retitled "A message-template defect, not a compile-time coverage gap". |
| Resolution | Deletion of clauses. Item 1 of the note — a diagnostic borrowing the wrong message template — **survives untouched**; it is a compile-time defect with nothing to do with this family. Item 2, the `FaultSiteLink` identity collapse, went with `FaultSiteLink`. |
| Correction to the row's reason | The row says "the coverage matrix encodes the backstop model in its column definitions, so the matrix's own shape changes." **It does not.** §3.1's columns are `FaultCode` / preventing `DiagnosticCode` / discharging obligation kind / disposition — all compile-time. The matrix keeps its shape and all fifteen rows. Only one of the three edits is inside §3.1 at all: `:232` is; `:113` sits above it in "Why there is no author-visible escape hatch", and `:266–271` is in §3.2. |
| Line numbers | `:94`→`:113`, `:199`→`:232`, `:236`→`:266–271`. |

### `N35` — the diagnostic system

| | |
|---|---|
| Site | The row cites `:548`. At HEAD that is the coverage-scanner paragraph — a different sentence. The clause is at `:552`. Extended to `:35`, `:96`, `:103`, `:541`, `:558`, `:563`, `:577–617`, `:632–645`, `:649–657`, `:713–730`, `:767`, `:787` |
| Before | "…and the **conservative backstop for proof-only obligation families that have no representable runtime fault of their own**." |
| After | The clause is gone; the collection-safety many-to-one collapse survives as the one piece of explicit policy, and the sentence now says "one piece" rather than "two". The same clause in `proof-engine.md:2587` was corrected identically. |
| Resolution | Deletion, plus twelve further sites in the same document that no row covered. The largest is `:649–657`, a subsection headed "### Evaluator usage" whose entire content is `if (IsZero(right)) return Fail(FaultCode.DivisionByZero);` — the exact divide-by-zero test `want:165` names. It is removed **with its contents entry at `:35`**. |
| Also in this document | `:558` re-scoped (its first clause, "no runtime errors", is right; its second described the evaluator producing failures); `:563`'s chain diagram loses its "Evaluator fails →" first link; `:96` and `:103` re-scoped; `:767` and `:787` no longer describe the chain's guarantee as covering "every evaluator failure mode"; `:541`'s `CreateFaultSiteLink` sentence rewritten (see below); the divide-by-zero example re-authored with `R11`. |

---

## 3. The seven items the decision sheet does not carry

**1. `precept-language-spec.md:112`.** Read "Every fault class **that the evaluator can produce** — type
mismatch, division by zero, overflow, empty collection access — is linked to a compiler diagnostic that
prevents it." Nothing is producible. The clause is dropped and the pairing survives: "Every fault class
the language admits — …". The same sentence's closing claim, "the compiler's job is to make every
evaluator error path unreachable," now reads "the compiler's job is to leave the evaluator no error path
to take" — the original presupposes paths that exist and are made unreachable.

**2. `fault-system.md:66`, under *Does NOT OWN*.** Read "How the evaluator produces `Fault` values and
returns them as the `EventOutcome.Faulted` variant — see `evaluator.md`". The cross-reference pointed at a
mechanism that no longer exists, so the bullet is **removed rather than re-scoped**. The bullet above it,
which disclaims ownership of the `EventOutcome` hierarchy itself, is kept and lightly re-worded.

**3. `fault-system.md:322`, and what replaces Q3.** `:322` and `:324` are both gone — this paragraph said
`:324` stayed, and it did not; corrected 2026-07-28. Q3 is re-written rather than left as an open fork,
and the question `:324` carried is restated in the new body rather than dropped. The half of the question that was about the author's data is
closed by the ruling and says so. What remains is about the engine's own state: a slot-index or
stack-depth assertion is not a check about a divisor or a bound, and two accounts of that already exist
and do not agree — `precept-builder.md` and `evaluator.md` say a corrupted model is an unrecoverable
exception, while `compiler-and-runtime-design.md` names the evaluator as where such a breach would surface
as a `Fault`. The heading is retitled to match ("Q3 — How far the evaluator asserts about its own state"),
with its contents entry updated. **Phase 11 was named as its owner in the text, and that was wrong** —
corrected 2026-07-28. Phase 11 builds the compiler against red tests phase 10 wrote, and an undecided
design question cannot have a red test written for it; a phase number is also transient material that does
not belong inside a canonical document. The canonical text now states the question and says it belongs
with the work that builds the evaluator, without naming a phase, and the ownership gap is recorded in the
plan under exit condition 6 and § 4.

**4. `diagnostic-system.md:682–690`.** Taught the analyzer chain by walking through adding
`FaultCode.StackUnderflow` — an engine bug, and now the one category of member that can never be added.
Re-authored against a generic member `X` described as "a class of failure some pipeline stage can diagnose
against the author's source text", with the `PRECEPT0001` step dropped (it constrains `Fail()` call sites,
not the addition of a member). A new paragraph states what the chain rules out: a member is admissible
only when a real authoring diagnostic exists for it, which an engine defect cannot have, because there is
no source text an author could change in response to it and therefore nothing to write into the message
template. Whether the enum is closed at the members it has, or `PRECEPT0002` is relaxed for an
engine-defect category, is recorded as undecided and pointed at `fault-system.md` Q3.

**5. `proof-engine.md:337`, the reference-identity invariant.** **Kept**, with its reason re-written. The
invariant — the proof engine MUST NOT use `with { }` on `obligation.Site` or create new `TypedExpression`
instances — still stands. The only reason previously stated for it was the builder's
`ReferenceEqualityComparer` match against `FaultSiteLinks`, which this pass removes, and no document names
another consumer; diagnostic emission needs the span, not the reference. The blockquote now says so
plainly: the reason is unsettled, and whoever writes the certificate contract owns the question of what an
obligation's `Site` has to be able to do, and should either give the invariant a reason or remove it. It
is neither deleted silently nor preserved with a justification nobody holds.

**6. `precept-builder.md` and the certificate gate.** The document owns `Precept.From` and did not know
the load-time gate exists, while `runtime-api.md:254` says the method runs it. Four places described a
method with no gate: § 9 Failure Modes, § 10 Input Contract, the Recovery Path (which told a caller with a
tampered definition to fix compilation errors that do not exist), and the always-valid guarantee at
`:883–889` resting on the `HasErrors` guard alone. **Fixed without deciding the shape**, in the words
`runtime-api.md:260` already uses in canon's own voice:

- A new **§ The Certificate Gate** under § 9, with a contents entry, stating that the gate exists, catches
  what `HasErrors` cannot, is not built, and that its failure shape — exception or structured result,
  whether it names the derivation step that failed to replay, whether verification is cached — is
  unsettled. It adds one thing the shape must satisfy whichever way it goes: `want:177` is unqualified
  that refusals explain themselves, so an unexplained failure is not admissible.
- The Recovery Path now says its four steps do not fit a definition the gate refuses, because such a
  definition carries no diagnostics to examine.
- The Input Contract gains the certificate condition, marked unbuilt.
- The always-valid guarantee now names both conditions the factory imposes and says only one is built.

**7. `runtime-api.md` against the philosophy's new claims.** Checked and **nothing was disturbed**.
`philosophy.md:46` now states whole-or-nothing commit and immutable versioning as a named guarantee, and
all three sites still say what it claims:

- `:291` — "`Version` is an immutable snapshot … Every operation returns a new `Version` — the input is
  never mutated." Matches "The version you held is never altered."
- `:373` — "a working copy the operation completed is promoted unconditionally. The working copy is
  discarded only when the operation refuses before it finishes." Matches "commits wholly or not at all."
- `:548` — "The patch is applied atomically — either all fields commit or none do."

None of this pass's edits to `runtime-api.md` touched any of the three; the edits were at `:171`, `:277`,
`:670`, `:672`, `:678`, `:906` and `:910`.

---

## 4. Sites corrected that no row covers

Beyond the twelve `diagnostic-system.md` sites folded into `N35` and the `:2446` replacement folded into
`C2`:

| Site | What it said | What it says now | Why |
|---|---|---|---|
| `runtime-api.md:277`, `:910` | Migration on schema change "is the caller's responsibility" | Handling data written under an older definition is the post-MVP job of the restore path itself, through migration | `want:141`'s parenthetical says the opposite, and `evaluator.md:1754` and `precept-language-spec.md:1969` are the two already-corrected siblings. A straight `want:141` correction, not a design question. |
| `precept-language-spec.md:1963` | "\| `Faulted` \| Evaluator impossible path / runtime fault. \|" | An engine-invariant breach, with a note that nothing constructs one | The spec was corrected everywhere else; this row was missed. Same edit as `N29`. |
| `compiler-and-runtime-design.md:706`, `:824` | `Fault` produced by the Evaluator | `:706`'s Owner cell reads "— nothing constructs one"; `:824` no longer lists `Fault` as an evaluator output | The brief names this as the attribution change the ruling requires. `:826`, `:865` and `:874` keep the family, and `:826` and `:874` now say plainly that nothing constructs one today. |
| `glossary.md:113` | "Faults are emitted when invariants the compiler couldn't prove are violated at runtime." | A class of failure the compiler prevents, paired with its diagnostic; nothing produces one during evaluation | A one-sentence statement of exactly what `want:171` removes, in the document a reader consults when a term is unclear. |
| `runtime/README.md:37` | "Fault system mirrors diagnostic system … same code namespace conventions, same severity model." | Same, with the mirroring named as a build-time pairing rather than a compile-time/run-time pair | Survives, but the sentence read as a runtime pairing. |
| `contributing/anti-patterns.md:336` | "Only obligations the proof engine genuinely cannot discharge become runtime fault-site backstops (defense-in-depth, not primary error path)." | An obligation the proof engine cannot discharge refuses the definition; adding runtime enforcement would be this anti-pattern rather than the fix for it | Directly contradicted prove-or-reject, in the document that teaches people not to do this. |
| `catalog-system.md:701`, `:702`, and the paragraph under them | "diagnostics are the compile-time face, faults are the runtime face" | A `FaultCode` member names a class of failure; a `DiagnosticCode` member names the rule that prevents it; both halves are checked when the project is built | The sheet lists the Diagnostics ↔ Faults pair as surviving, and it does — but the "runtime face" framing is the removed model. Same correction as everywhere else. |
| `tooling/language-server.md:661` | "Even though a runtime overflow check exists, the hover reflects static proof status. The runtime check is a fallback, not a guarantee — collapsing the distinction would teach users to trust runtime safety nets as proof." | The hover reports static proof status and nothing else, because there is nothing else; `⚠️ Gap` is accurate because the overflow model is in scope and not yet implemented, so an unbounded operand is genuinely unprotected today | **Found by this pass's own sweep before its row was noticed.** It asserted a runtime overflow check exists as a fallback, which is what `want:165` removes. The hover's own conclusion is unchanged and now rests on an accurate reason. It turned out to be register row **`N103`**, one of the two `docs/tooling/` rows the fifth pass recorded and left unassigned; the correction made matches what that row specifies. |
| `tooling/mcp.md:108` | "The declared-bound **value-safety** codes `NumericOverflow` (PRE0078) and `OutOfRange` (PRE0079) are now `Proof`-owned…" | "The declared-bound **containment** codes … These are rule-preservation obligations, not fault gates — a bound is rule shorthand (language spec §0.7)." | Register row **`N104`**, the other unassigned `docs/tooling/` row, and the same quoting rule `R13` states: a document must not cite `OutOfRange` as a value-safety code for a declared bound. Taken here because it is `R13`'s content in a file `R13` did not reach, and because the replacement wording already exists — `N38` made the identical correction at `diagnostic-system.md:162`, which this edit copies. |
| `proof-engine.md:2296`, `:2587`, `diagnostic-system.md:541` | Three sentences using `CreateFaultSiteLink` as evidence that a diagnostic code lives in exactly one catalog field | All three rewritten. `CreateDiagnostic` is now named as the field's only reader, which is what makes the single-siting argument | The argument survives on `CreateDiagnostic` alone; leaving the sentences would have kept a dead function as the evidence for a live claim. |
| `proof-engine.md:2140` | "the proof-site shape determines the projected failure surface … and map to `FaultCode.CollectionEmptyOnAccess`" | The diagnostic selection is stated as such; the fault-class pairing is named as a build-time correspondence, not something the diagnostic carries into runtime | Re-scope. The diagnostic selection is real and survives. |
| `diagnostic-system.md:577–617`, `:632–645` | A second, drifted copy of the `FaultCode` enum with **thirteen** members (stopping at `OutOfRange = 13`, missing `LengthBoundViolation` and `CountBoundViolation`) and mapping `UnexpectedNull` to `NullInNonNullableContext`, plus a matching thirteen-case `GetMeta` copy | Both reduced to a two-member excerpt plus a pointer at `docs/runtime/fault-system.md`, which declares the enum | Pre-existing drift the sheet marked optional. Fixed, because a parallel copy of a catalog's membership is the thing `CLAUDE.md` § Catalog System forbids, and the drift includes a wrong preventing diagnostic. `fault-system.md:125` is the declaring document and says `UnprovedPresenceRequirement`; `soundness-and-coverage.md:226` independently agrees. Ownership settles it without a tally. The teaching value of the excerpt — the attribute shape — is kept. |
| `fault-system.md:262` | Cross-reference to "diagnostic-system.md § FaultCode — The Runtime-to-Compiler Chain" | Repointed at § FaultCode → DiagnosticCode Chain | The named heading does not exist at HEAD. Pre-existing broken reference, fixed while the file was open. |
| `evaluator.md:2157` | Cross-reference to `docs/compiler/proof-engine.md §Fault Backstops` | Repointed at `docs/compiler/diagnostic-system.md § FaultCode → DiagnosticCode Chain` | That heading does not exist at HEAD and never did. The repoint target is where `proof-engine.md:2724` already sends readers. |

---

## 5. What this pass does not decide, and who holds it

Two questions were left open on purpose, both recorded in canon rather than in this file so they do not
disappear when this document is archived.

**Whether anything constructs a `Fault`.** Recorded at `fault-system.md` Q3, `evaluator.md` § Fault
Production, `result-types.md` § `Faulted` semantics, `compiler-and-runtime-design.md:826`, and
`runtime-api.md:171`. Every re-description this pass made is correct under either answer — nothing here
presumes one. **This was recorded as owned by phase 11, and that is wrong; corrected 2026-07-28.** No
phase of the readiness plan has a scope that reaches it, which is a real result rather than a gap to be
papered over with the nearest phase: the plan builds the compiler, phase 11 builds against red tests
phase 10 wrote, and an undecided design question cannot have one. It is on the plan's § 4 for the owner's
word.

**Whether any consumer needs reference identity on `ProofObligation.Site`.** Recorded at `proof-engine.md`
§ ProofObligation. **This pass deleted the invariant's stated reason and then wrote that no reason was ever
given; corrected 2026-07-28.** The reason — reference identity is what tells two structurally identical
expressions in different transition rows apart — is restored, so the invariant is not unexplained. What is
open is the narrower question of whether anything needs the distinction, and **phase 5 owns it**, because
phase 5's stated output includes the specification of the certificate checker.

Three further open questions were already declared open in canon and were **not** resolved: the shape
`InvalidArgs` needs (`result-types.md:123`), what the `ConstraintResult` arrays mean now
(`result-types.md:252`), and where `FaultMeta.RecoveryHint` belongs. The last of these had no written
record at all, so one was added at `fault-system.md` § Right-Sizing: the field was added because a fault
could be reached at evaluation time and an operator would need telling what to do about it; nothing is
reached at evaluation time now, so the hint's only remaining reader is the precept author, and authoring
guidance already lives on the diagnostic. Keeping it, moving it to the diagnostic, and deleting it are all
open, and each changes a catalog and the MCP formatters. The `FaultMeta` record is untouched.

**The disputed "and nothing wider" scope was not touched.** All eight sites keep their wording and their
disputed marking. That question is `N96`, it is owned by phase 5, and the register says it is not to be
executed here.

---

## 6. The vocabulary sweep

Run over the canonical set — `docs/language docs/compiler docs/runtime docs/tooling docs/philosophy.md
docs/compiler-and-runtime-design.md` — after all edits.

| Term | Hits | Disposition |
|---|---|---|
| `FaultSiteAnnotation` | 0 | — |
| `FaultSiteDescriptor` | 0 | — |
| `FaultSiteLink` | 0 | — |
| `FaultBackstops` | 0 | — |
| `Fault Backstop Pass` | 0 | — |
| `CC#6` | 0 | — |
| `CC#12` | 0 | — |
| `IsFaultCondition` | 0 | — |
| `backstop` | 0 | — |
| `defence-in-depth` | 0 | — |
| `impossible-path` / `impossible path` | 0 | — |
| `defensive redundancy` | 0 | — |
| `defense-in-depth` | **1** | Survivor — see below |
| `belt-and-suspenders` | **1** | Survivor — see below |

**The two survivors, with the reason each survives:**

- **`docs/tooling/mcp.md:110`** — "primarily as defense-in-depth against bugs in the tool wrappers or
  transitive dependencies." This is the MCP server's catch-all around its own tool methods, guaranteeing
  that an unexpected throw returns a structured response rather than a raw framework error. It is about
  the MCP process, not about an entity's data or the evaluator, and `want:165` has nothing to say about
  it. The word is unfortunate; the mechanism is legitimate and unrelated.
- **`docs/compiler/type-checker.md:1133`** — "Forward-reference prohibition belt-and-suspenders
  validation", one line in a slice checklist. It describes the type checker validating a prohibition a
  second time, at compile time. Nothing about it is a runtime path. This is the same shape as `N36`: a
  pattern scoped across the whole canonical set while the row it belongs to corrects one document, so it
  can never reach zero and should not.

**A wider sweep was also run** using `search-procedure.md` § 1's own backstop-family pattern
(`backstop|defense-in-depth|defence-in-depth|defensive redundancy|belt-and-suspenders|belt and
braces|fault trap|FaultSite|safety net|last line of defen|redundant check|defensive check|trap the
fault|guard against (the )?(overflow|division|fault)`) over `CANON_WIDE`. It returned seven lines. Two are
the survivors above. One was `language-server.md:661`, corrected and recorded in § 4. The remaining four
are already-corrected canon stating that no such mechanism exists — `precept-language-spec.md:110` and
`:294` ("there are no runtime fault traps"), `proof-engine.md:1686` (register `C4`, "carries no fault trap
at evaluation time"), and `compiler-and-runtime-design.md:714` ("production deployment safety net",
describing a contract digest for host applications, unrelated).

The deferral-family pattern was also run and returned nothing new: every hit is either already-corrected
canon, the rejected fault-floor alternative stated as rejected, or an unrelated use of "fallback" in the
type checker's operator-widening logic.

---

## 7. The nineteen rows' recorded grep patterns, run

Run from the repository root with `CANON="docs/language docs/compiler docs/runtime docs/philosophy.md"`,
which is what `search-procedure.md:27` defines it as.

| Row | Pattern | Count |
|---|---|---|
| `R9` | `grep -rn "FaultSiteAnnotation" $CANON` | **0** |
| `N22` | `grep -rn "Fault Backstop Pass" $CANON` | **0** |
| `N23` | `grep -rn "FaultBackstops" $CANON` | **0** |
| `R10` | `grep -rn "Fault backstop routing" $CANON` | **0** |
| `N24` | `grep -rn "CC#6" $CANON` | **0** |
| `N25` | `grep -rnF "Impossible-Path Failures (Defense-in-Depth)" $CANON` | **0** |
| `R14` | `grep -rn "Impossible-Path Failures" $CANON` | **0** |
| `N26` | `grep -rn "FaultSiteDescriptor" $CANON` | **0** |
| `R11` | `grep -rnF "Fail(FaultCode.DivisionByZero)" $CANON` | **0** |
| `R12` | `grep -rn "bypassed compile-time checking" $CANON` | **0** |
| `N27` | `grep -rn "Defense-in-depth, not a primary error path" $CANON` | **0** |
| `R13` | `grep -rn "LengthBoundViolation," docs/runtime/fault-system.md` | **1** |
| `N28` | `grep -rn "fault codes exist at evaluation time as a backstop" $CANON` | **0** |
| `N29` | `grep -rn "errors the type checker should have prevented" $CANON` | **0** |
| `C1` | `grep -rn "FaultSiteLink" $CANON` | **0** |
| `C2` | `grep -rn "belt-and-suspenders" $CANON` | **1** |
| `N30` | `grep -rn "defense-in-depth" docs/compiler/proof-engine.md` | **0** |
| `N34` | `grep -rn "evaluator traps are defense-in-depth" $CANON` | **0** |
| `N35` | `grep -rn "conservative backstop for proof-only obligation families" $CANON` | **0** |

**The two non-zero counts, by reading the canonical text:**

- **`R13` returns 1, and must.** The pattern matches `fault-system.md:153`, the `LengthBoundViolation,`
  enum member. This row is the one the decision sheet marks as describing an edit that should not be made,
  and its own pattern is therefore a *retention* check, not a removal check. A zero here would mean the
  three bound-containment members had been removed and the compile-time coverage accounting broken.
- **`C2` returns 1, at `type-checker.md:1133`.** Not an incomplete correction. The pattern is scoped
  across the whole canonical set while the row corrects one sentence in `proof-engine.md`, and the
  surviving hit is a type-checker slice-checklist line about validating a forward-reference prohibition a
  second time at compile time. The register already carries this shape at `N36`, where a pattern was
  re-scoped to the file its row owns for exactly this reason. Corrected form:
  `grep -rn "belt-and-suspenders" docs/compiler/proof-engine.md` — **1** at `b586ab0b`, **0** now.

**Two patterns are worth flagging as weak** for whoever maintains the register, though both returned zero:

- `N29`'s pattern quotes a sentence the row itself expects to *survive in re-scoped form*, so zero is the
  right answer only because the re-scope changed the words. It cannot distinguish a correct re-scope from
  a deletion of the variant, which is exactly the mistake the row exists to prevent. The row's own cell
  says it "is verified by reading", and it was.
- `R14`'s pattern is a prefix of `N25`'s, so the two rows share one signal. Both were verified by reading
  the section that no longer exists.

---

## 8. What this pass did not do, and why

- **`docs/philosophy.md` was not edited.** It is locked and was amended today on the owner's approval.
  This section originally added "Nothing found in this pass contradicts it", and the independent review of
  the diff contested that: `philosophy.md:46` states "The engine does not fault" without qualification,
  while this pass left six canonical places recording the engine-defect case as undecided. **The owner
  ruled on 2026-07-28 that the philosophy sentence is aspirational** — *"obviously we can't say that piece
  of software has no bugs. it's aspirational."* `philosophy.md:8` says the same in the document's own
  voice. A bug in Precept is outside the guarantee rather than a case the guarantee has to model, so
  neither the philosophy nor the six notes change. The claim as originally written was untested; the
  conclusion it reached happens to be the one the owner confirmed.
- **`docs/archive/` was not edited.**
- **Nothing was committed.** The tree is left dirty.
- **No sub-agents were spawned.**
- **`docs/tooling/mcp.md:108` and `language-server.md:661` were taken on**, though neither is one of the
  nineteen. Both are register rows `N104` and `N103`, recorded by the fifth pass and left unassigned; both
  are inside this family's subject matter; and both had their replacement wording already settled by a
  row that executed elsewhere. Doing them here rather than leaving them for "a pass that takes
  `docs/tooling/` on deliberately" is the same reasoning that made the nineteen one group.
- **`docs/Working/` was not corrected.** Two files there —
  `Working/posture-v2-support/devils-advocate-2026-07-14.md:56` and `:172` — cite `runtime-api.md`'s
  "Three-Layer Enforcement Model" and its third layer by name, and that heading is now
  "Two-Layer Enforcement Model". `docs/Working/` is in-flight proposal material on a promote-or-archive
  lifecycle, outside the canonical set, and correcting it is not this pass's job. Recorded here so the
  stale citation is not mistaken for a live claim.
- **`docs/tooling/mcp.md:242` and `:269` were not changed.** They are `## Runtime Faults` and
  `## Prevents Fault`, and both are verbatim section headings in the text `precept_proofs` and
  `precept_diagnostic` emit. The first now describes compiler vocabulary rather than runtime behaviour, so
  the heading reads a little stale — but renaming it in the document without renaming it in
  `tools/Precept.Mcp/` would create drift, and renaming it in both is a change to a tool's output
  contract, which is a different decision from this correction. Recorded rather than made.
- **`catalog-system.md:825` was not corrected.** It cites `precept-builder.md §Pass 4` as the canonical
  home of the five-subtype `ConstraintMeta` hierarchy; that hierarchy is documented under Pass 5, the
  Constraint Plan Pass. Pre-existing mis-citation, unrelated to this family, noticed while checking that
  no cross-reference pointed into removed material.
- **`result-types.md:371` was not corrected.** The survey-grounding table's Temporal row says "Faults
  throw (not return)", which contradicts `:51`'s "never thrown" and Q1's resolution in `fault-system.md`.
  Real drift, but it is about throw-versus-return, not about the fault-backstop model, and fixing it is a
  different row than any of the nineteen.
- **`result-types.md:47` and `:56` were not corrected.** Both say eight `EventOutcome` variants; the code
  block at `:95–105` lists seven and omits `Created`, which `runtime-api.md:168` and
  `precept-language-spec.md:1960` both require. Pre-existing, unrelated, and noted here because the
  eight-variant claim also appeared at `evaluator.md:1433`, which this pass removed for other reasons — so
  one of the three copies of a wrong count is now gone as a side effect, and the other two remain.
- **`src/` and `tools/` were not checked, and three documents were left claiming "Implemented" against
  code that disagrees.** Found by both reviews of the diff. `src/Precept/Runtime/Descriptors.cs:60`
  declares `FaultSiteDescriptor`, `src/Precept/Pipeline/ProofLedger.cs` declares `FaultSiteLink` and
  `FaultSiteAnnotation` and carries the `FaultSiteLinks` field, `ProofEngine.Diagnostics.cs:452` builds
  them, and `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs:292` reads the field. `CLAUDE.md`
  requires drift to be fixed in the same pass and reported. The Implementation State and Source Files
  entries were made honest on 2026-07-28 and the removal is tracked in the plan's § 5 —
  see [`fault-backstop-fixes.md`](fault-backstop-fixes.md).
- **No MCP projection inventory was taken.** The decision sheet's own closing note says nobody checked the
  nine documents against `docs/tooling/mcp.md`'s DTOs or `tools/Precept.Mcp/CatalogFormatters.cs`. This
  pass swept `docs/tooling` for the removed vocabulary and found one live contradiction
  (`language-server.md:661`, corrected), but it did not enumerate where `Fault` or `FaultCode` is
  projected through the tooling code. If the projection carries the removed model somewhere, this pass did
  not find it.


---

## 9. Two tracking documents updated

Neither is canonical; both are the instruments that say where phase 2 stands, and both said this work was
unexecuted.

**[`contradiction-register.md`](contradiction-register.md).** The register's own rule at `:41` is that
every change made to a canonical document during phase 2 is reachable from its table. All nineteen cells
in the fault-backstop family read "Not executed. Deferred with the runtime fault-backstop family…"; they
now say the family ran as one pass, name the decision sheet as the authority followed where the sheet and
the cell disagree, and point at this document. **`R13`'s cell is written differently** — its described
edit was ruled against and not carried out, its own pattern is a retention check, and the cell says so
rather than claiming execution. `N103` and `N104` were updated the same way. **No cell carries a commit
hash**, because nothing is committed; each says so. **Added 2026-07-28 after review:** this pass made
roughly fourteen edits that are not rows, and the register's index table at `:43` had no entry for them,
so they were reachable only through the row cells. An index row was added with the Commit cell reading
*uncommitted*; it gets its hash when the commit lands. The family's intro paragraph at `:286` and the
`N36` cell at `:315`, both of which still described the machinery as standing, were corrected too.

**[`compiler-readiness-plan.md`](../compiler-readiness-plan.md).** Four statements were factually wrong
after this pass and are corrected without any condition being declared passed:

- The phase-2 board row: the done count moves from about 111 to about 132, condition 6 now fails on one
  row rather than 22 — and, after the review, on one open item as well — and condition 2 now fails on the
  21 uncommitted rows as well as the 14 unstamped ones.
- The condition-6 paragraph: named the nineteen as having no owning phase. It now records them disposed of
  and uncommitted and leaves `N82` as the one remaining row.

  **Two corrections to these two, made 2026-07-28 after review.** *(a)* "21 executed" is one too many:
  twenty rows were executed by editing canon and `R13` was ruled against and disposed of without an edit.
  Both statements now say so. *(b)* The condition-6 paragraph originally gave "none of the 21 carries a
  commit hash" as the reason the condition is not satisfied, which is condition 2's requirement, not
  condition 6's. Condition 6 is about owners, and it fails on `N82` and on one open item this pass left
  that has no owning phase — see the exit-condition text.
- The condition-3 sentence: said condition 3 "cannot pass while the fault-backstop rows stand." That
  clause is removed; the rest of the sentence, that it holds only as of a stated commit, is unchanged.
- The exit-condition 6 text and the "How this was found" paragraph: both described the family as an open
  deferral. Both now record it executed. The rule those paragraphs state — that no phase closes with a
  deferral that has no owner — is unchanged and stands on its own; it is what got this pass run.

**What was deliberately not touched in the plan**: no condition is marked as passing, no phase status
changed, and the board still says phase 2 is in progress. Whether phase 2 closes is the owner's call and
the commit does not exist yet.

**Historical pass records were not edited.** `fifth-pass-corrections.md`, `fourth-pass-corrections.md`,
`confirmation-run.md`, `recheck-third-pass.md`, `independent-recheck.md`, `register-spot-check.md`,
`compiler-doc-corrections-executed.md` and `runtime-corrections-executed.md` all describe the family as
deferred. Each was true when written and each records the state of a run that happened; rewriting them
would destroy the record of why this took five passes.
