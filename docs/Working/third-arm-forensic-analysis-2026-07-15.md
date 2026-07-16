# The third arm — forensic analysis

**status:** Draft finding — 2026-07-15. Evidence only. Makes no ruling, proposes no design. Input to the owner ruling named in §6.

**Question asked:** was the "third arm" present in the corpus as of 2026-07-12, or was it introduced later by the draft/posture work of 07-13→07-14?

**Answer:** neither. It has been in the canonical spec since **2026-06-02**, sitting beside its own negation, which was committed **eleven minutes earlier**. Every artifact downstream inherited the contradiction and picked a side without knowing there was a side to pick.

**Method.** Four agents on disjoint slices, each required to cite `file:line`, to write UNCERTAIN rather than guess, and to treat agent-authored `Locked`/`RULED`/`ratified` labels as non-binding. Two were held to a hard cutoff (nothing dated after 2026-07-12; canonical docs read via `git show f1f3a319:` rather than the working tree, which carries uncommitted 07-14/15 edits). Git history and source were exempt from the cutoff — commits are facts, not opinions. One agent did nothing but verify quotes verbatim, because the load-bearing claims had reached the main session secondhand through two layers of agent reports. That agent found two fabrications (§5).

---

## 1. Definitions

- **Arm 1 — prove-or-reject (compile time).** For values the definition *derives*.
- **Arm 2 — govern (runtime, at ingress).** For raw external values *at their own ingress slot* — event arguments, construction inputs, direct field edits — checked as they enter, before anything derives from them.
- **A third arm.** Runtime governance acting on something that is **not** a raw external value at its ingress slot. Canonically: a *post-mutation sweep* that re-checks whole-entity invariants at commit, over state that never passed through the ingress door being used.

**The base case, which appears nowhere in the entire corpus:**

```precept
field Floor as integer editable
field Ceiling as integer editable
rule Floor <= Ceiling
```

Two independently-set fields. Two separate ingress doors. One predicate over both. Fire `SetFloor(50)` when `Ceiling = 10`: at the moment of the check, `Floor` is a raw external value at its slot (arm 2, legitimate) — but **`Ceiling` is not**. It is stored state from a different event, possibly months earlier, possibly itself derived. There is no ingress slot for `Ceiling` anywhere in that operation.

Six documents across six weeks of argument were searched for this rule or its shape. It is worked in none of them.

---

## 2. Provenance — the sweep has two births

### 2.1 The mechanism (innocent, owner-authored, 2026-04-29)

`a52282a8` — `shane <sfalik@hotmail.com>` — *"docs: make evaluation faults explicit in compile-time checking"* — adds spec §3A.4 **"Mutation Atomicity"**:

> All mutations execute on a working copy. Constraints are evaluated against the working copy after all mutations complete… If any constraint fails, the working copy is discarded and the entity's state is unchanged. **An invalid configuration never exists, even transiently. There is no window between mutation and constraint checking where a partially-committed state with violated rules can be observed.**

The working-copy concept itself traces further back, to `6182847c` / `0d2cfc2c` (2026-03-02), introduced for **atomic multi-transform execution** — all-or-nothing assignment with read-your-writes.

**The purpose was atomicity — preventing torn writes.** The stated problem is the *observability window*. The sweep is the mechanism that makes *discard* possible. Governance is not in the frame anywhere in its origin.

### 2.2 The framing (undecided, agent-co-authored, 2026-06-02)

Two commits, eleven minutes apart. **This is the root cause.**

**`5af46537` — spec §0.7, "The Compile-Time and Runtime Guarantee Contract."** Its commit message states its own purpose:

> Settles the long-standing ambiguity over what is guaranteed at compile time vs enforced at runtime — **so it isn't re-debated**.
> governance = runtime enforcement of declared constraints on external input

The paragraph it added, `precept-language-spec.md:268`, verbatim and complete:

> **Governance — enforced at runtime on external input.** Every declared constraint is enforced on every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — **at the moment it enters, before any computation derives from it.** This enforcement is operation-blind… Because mutations execute on a working copy that is discarded if any constraint fails (§3A.4), an invalid configuration never persists — this is prevention, not detection. (Principles 1, 6.)

**The word "sweep" does not appear in §0.7 at all** (`:262-274`). Governance is scoped, in its opening clause, to "on external input." This is a two-arm contract.

**`63e08963`, eleven minutes later — spec §3A.4:1969.** A **+4/−1 line** edit inserting, above the existing atomicity text:

> **This post-mutation sweep is one of two enforcement points.** Externally-sourced values are first validated against their declared constraints at **ingress** — as they enter, before the working copy derives any dependent value (governance, §0.7). The sweep then re-checks every constraint against the completed working copy. **Ingress governs *what enters*; the sweep governs *the result*.** (Restored state is neither: it is trusted as valid at persistence time and is re-governed only by the next operation's sweep — §0.7.)

This is a three-arm contract, and it cites §0.7 while contradicting it.

**Pickaxe confirms singularity:** `git log --all -S'post-mutation sweep' -- docs/language/precept-language-spec.md` → **exactly one commit, `63e08963`**.

### 2.3 How the framing got in — it rode on a different decision

`63e08963`'s subject is **Restore semantics**. Its declared owner decision is:

> Owner decision: hydration must be fast and trusts persisted data as valid at write time — analogous to a direct database read.

The sweep edit is bullet 2 of four, and the message frames it as *naming* something, not deciding it:

> `- spec §3A.4: name the two enforcement points (ingress governs what enters; the post-mutation sweep governs the result); restored state is re-governed only by the next operation's sweep.`

**The causal chain:** trusted-hydration ruled that restored state is *not* re-validated. That opened a hole — what re-governs a stale value? The sweep was the nearest available mechanism, and the commit conscripted it, promoting an atomicity guarantee to a co-equal *governance enforcement point* as scaffolding for a decision about something else.

The plan doc of that day (`docs/Working/Archive/proof-engine-guarantee-contract-plan-2026-06-02.md`) records it only as an assumed backstop inside decision D4:

> **D4 — Restore semantics = trusted hydration (owner, 2026-06-02).** Hydration must be fast and trusts persisted data as valid at write time… the next operation's post-mutation sweep (§3A.4) re-governs a stale value.

**No alternatives considered. No four-leg rationale. No decision of its own.** For a project whose rules require Rationale / Alternatives rejected / Precedent / Tradeoff accepted on every locked decision, this one has none of the four.

**UNCERTAIN and flagged:** whether the owner read the paragraph at commit time. The record shows no separate decision for it; absence of a decision record is not proof of absence of review. Author identity carries no signal — three git identities appear across this history (`shane <sfalik@hotmail.com>`, `sfalik <sfalik@spark1.falik.ca>`, `shane <17600811+sfalik@users.noreply.github.com>`), all plausibly the owner's machines, and both June-2 commits carry agent co-authorship trailers.

---

## 3. The contradiction, stated plainly

| | Spec §0.7 (`:268`) | Spec §3A.4 (`:1969`) |
|---|---|---|
| Committed | `5af46537`, 2026-06-02 | `63e08963`, 2026-06-02, **+11 min** |
| Governance scope | "on external input… at the moment it enters, **before any computation derives from it**" | "**one of two enforcement points**… the sweep governs **the result**" |
| Arms | Two | Three |
| Mentions the sweep | **No** — word absent from all of §0.7 | Yes, names it as co-equal |

Both are live. Both are byte-identical in the working tree and at `f1f3a319` (the 07-12 snapshot). **The canonical spec has asserted both since 2026-06-02.**

**This is the engine of the re-litigation.** An author reading §0.7 writes two arms and is faithful to canon. An author reading §3A.4 writes three arms and is equally faithful to canon. Both pass review against "does this match the corpus." The owner, reading either draft, correctly detects that something is being smuggled — and is told, correctly, that it matches the corpus. It always did. Just not all of it.

The contradiction is conceded once, in a line nobody elevated (`posture-v2-support/ingress-scope-verification-2026-07-14.md:315-317`):

> **§0.7 is incomplete about governance scope** — it describes ingress and cites §3A.4 for the working-copy mechanism, but doesn't explicitly state the sweep is a second check on relational invariants.

---

## 4. What each artifact downstream did with it

### 4.1 The 07-06 ruling — inherited it, and wrote a boundary that doesn't describe it

**v1's four-leg ingress-origin predicate encoded the two-arm reading.** Run `rule Floor <= Ceiling` through it: **Leg 3 fails** (the rule is not the entering slot's own contract), **Leg 4 fails** (a `<=` operator node sits between raw ingress and the check), **Leg 2 fails** (the sweep runs after all mutations complete, not before derivation). v1's own rule: *"If any leg fails, `v` is definition-internal and is **compile-time prove-or-reject**."*

**v1's predicate routes relational invariants to arm 1.** But v1's worked row (`v1:98`) concedes Leg 4 fails and routes to governance anyway — while saying **"enforced post-mutation"** out loud. v1 contradicted itself in adjacent columns of one table, *visibly*.

**Rev 3 resolved that contradiction by widening the rule to match the row.** The Obligation-Role Rule (`ruling:127`) **deletes Leg 3** (same-slot contract) and **Leg 4** (no expression node) — the two legs that excluded relational rules — distributes governance over "each field `C` reads", and replaces v1's "post-mutation" with the heading **"(runtime, at ingress)"**.

**Rev 3 did not remove the third arm. It removed the words that made it detectable.**

**Part A.1 is internally inconsistent in one sentence** (`ruling:127`):
- **trigger:** "whenever a **raw external value** enters that field" → arm 2
- **subject:** "`C` is enforced **on the resulting working copy**" → the sweep

The trigger is ingress; the subject is the entity. Two different arms in one sentence. And A.1 is *lossier* than the real sweep: read literally, a mutation via `set Floor = Base * 2` triggers no ingress occasion for any field the rule reads — so `rule Floor <= Ceiling` would be enforced nowhere at runtime, and (per row 10's own reasoning) nowhere at compile time. The actual sweep has no such hole. The description does.

**The grep is diagnostic.** Across all 359 lines of the ruling:

| term | hits |
|---|---|
| `whole-entity` | **0** |
| `ConstraintsFailed` | **0** |
| `post-mutation` | **1** — `:106`, and scoped *"Governance runs **on external input** via the post-mutation constraint sweep"* |
| `working copy` | 3 — `:25`, `:89`, `:127`, **all three attaching the discard to ingress** |
| `sweep` | 7 lines — `:37`, `:102`, `:106`, `:151`(×2), `:159`, `:182`, `:227` |

**The ruling is written in §0.7's vocabulary while its rows do §3A.4's work.** It cites §3A.4 **six times and always at `:1965`** — the section *heading*. It never cites `:1969`, four lines below, and grep for `1969` / `two enforcement` / `governs the result` / `enforcement point` across the ruling returns **empty**. The canonical spec names two runtime enforcement points; the ruling neither adopts nor argues against that sentence.

**Rows 10/11 route relational invariants to governance.** Row 10 (`:159`), verbatim fragment: *"The identity is enforced by the constraint sweep."* Its obligation column names the shape exactly: *"Invariant over independently-set fields."* Row 11 (`:160`) makes the split doctrinal — the same expression `Deposits - Withdrawals` is prove-or-reject in a `set` and governed in a `rule`: *"Distinguishes declaring a relationship (governed) from computing a value (proven)."*

Row 11 is in direct tension with the ruling's own Challenge-1 correction 120 lines earlier (`:37`: *"a disposition keyed on modifier-vs-rule is incoherent"*; `:144`: *"Every row is spelling-invariant"*). Rows 10/11 are keyed on exactly a spelling distinction — `rule` vs `set` — and the ruling *endorses* it as intended rather than recognizing it as the routing it declared incoherent.

**The chain "row 10 → `ConstraintsFailed` → the sweep" is assembled by a later verification agent, not by the ruling.** The ruling reaches `result-types.md` only through bare line cites and never names the outcome.

### 4.2 The 07-11 papers — the disagreement was live and was never adjudicated

- **Frank:** governance is ingress-only. *"The mechanism is triggered at a boundary crossing"* (`frank:81`). He means it literally — he argues elsewhere (`frank:131`) that a post-computation re-check would be "new machinery" requiring a new `ConstraintKind`.
- **Fable:** two enforcement points, citing canonical spec (`fable:38-44`, quoting `:1969` and `:1354`). Scored **against** Frank: *"Verdict: overstated by textual omission"* (`fable:85`).
- **Frank's paper contradicts Frank's own ruling.** The paper says ingress-only; the ruling (`:102`) relies on the sweep — *"Governance already enforces it: the runtime constraint sweep"* — as the decisive reason to reject pure Model B. Fable caught this at the time (`fable:127`).

Fable conceded only the **derived-write default** (`fable:233`) — which became Decision #1 — and explicitly **held** the sweep on his final list (`fable:200`). **The disagreement went into 07-12 live and came out live.**

### 4.3 The 07-12 ledger — ruled a cell that excludes the question, then ratified both sides of the contradiction

**Decision #1's cell** (`ledger:19`): *"When a **computed value** is written into a field with a declared limit and the compiler can't prove it stays inside."* Frank's fence, qualifier 1 (`frank:21`): *"the value is produced inside the precept, not supplied from outside… **It is not an event argument, not a construction input, not a direct field edit.**"*

Run `rule Floor <= Ceiling`, both `editable`, through it: **qualifier 1 fails explicitly** — both fields are set by direct field edits, the exact phrase excluded. **Qualifier 2 fails** — there is no write target carrying a bound; it is a two-field predicate. The ruling states the same exclusion independently at row 10: *"the rule form has no `set`/`<-`."*

**Decision #1 does not reach free-standing relational invariants.** Three independent artifacts agree. Confidence ~90%.

**The ledger's vocabulary** — mechanical count over the committed file:

| term | ledger | thesis | frank | fable |
|---|---|---|---|---|
| `sweep` | **0** | 1 | 1 | 14 |
| `ingress` | **0** | 10 | 17 | 11 |
| `proof-carrying` | **0** | 16 | 7 | 14 |

**The ledger contains zero occurrences of the entire boundary vocabulary of all three position papers.** The ruled artifact does not speak the language of the question.

**And the fold-in clause ratified the contradiction** (`ledger:24`):

> **Folded into this ruling if you take A** (converged, no separate debate needed): the Hybrid model + **"raw input is governed at the door; anything computed from it is proven" boundary**

That folds in a **two-arm one-liner** *and* the **Hybrid model containing rows 10/11** (three arms). `rule TotalCost == AvgCost * Qty` is neither raw input at a door nor a computed write — it fits neither arm of the one-liner it was folded in beside. **They have not converged. The contradiction was ratified unexamined, under a clause asserting no debate was needed.**

Eight of ten decisions and all six Stage-0b follow-ons are orthogonal to this question. Only #1 (via the fold-in) and #9 (which removed the escape hatch that was both papers' stated co-requisite) touch it, and neither confronts it.

---

## 5. Evidence defects found — do not propagate these

### 5.1 The "21 of 77" figure does not measure what it has been carrying

The ruling states its own method verbatim (`:102`):

> **21 of 77 sample `.precept` files contain product expressions** (re-confirmed first-hand at HEAD — `grep -l '\*' samples/*.precept | wc -l` = 21, total 77)

**That is a count of sample files containing an asterisk character.** Not files with relational invariants. Not files that would be rejected. Every downstream restatement — *"depend on it"* (`ruling:226`), *"depend on nonlinear governance rules"* (`ingress-scope-verification:44`), *"would be **rejected definitions**"* (`third-arm-section2-check:306`) — escalates a punctuation grep into a load-bearing cost argument. **And that argument is the decisive leg of the ruling's rejection of pure Model B** (`:101-102`).

An independent count of *actual* multi-field relational rules (method: strip `because` messages and quoted literals from every `rule` line, extract capitalized identifiers, drop keywords, count rules referencing ≥2 distinct field roots) returned: **132 of 149 rules are multi-field relational, appearing in ~53 of 77 files (~69%)**. That agent flagged its own heuristic as approximate and possibly over-counting guard-coupled rules.

**Neither number is currently trustworthy enough to rule on.** If the expressiveness cost is load-bearing for the decision in §6, it needs a real count first.

### 5.2 A fabricated quotation

`third-arm-section2-check-2026-07-14.md:311-314` presents this as a quotation of `boundary-ruling:§3 Leg 2`:

> *"21/77 sample files use products, which would become illegal ... zero expressiveness loss was non-negotiable"*

**Neither fragment appears in §3 Leg 2** (`ruling:91-103`). No string "which would become illegal"; no string "zero expressiveness loss was non-negotiable". "Zero expressiveness loss" occurs once, at `:226`, in a different section, as a table cell, and does not say anything was "non-negotiable". The model heading it attributes is also wrong — the ruling's is *"Pure Model B — rejected on expressiveness…"*, not *"prove everything or reject."*

This is the second fabricated-citation family in this corpus; `hybrid-model-draft-D-adversarial-review-2026-07-14.md:43-50` found two fabricated sample citations (B6, B8) in Draft D.

### 5.3 Provenance of the owner's own quote

`ingress-scope-verification-2026-07-14.md:24` labels the owner's claim **"verbatim from task description"** — i.e. the doc's own provenance claim is that the words arrived through the *agent's task brief*, not that they were captured from the owner directly. There is no citation to a source message. The position has since been confirmed by the owner directly in session (2026-07-15), so this is moot as to substance — but it is a live example of how owner-attributed text enters this corpus unsourced.

---

## 6. What is actually open

**Not** "should Precept have a third arm." That framing is the identity re-litigation the retrospective (`frank-retrospective-proof-engine-arc-2026-07-13.md:111`) names as the reflex that cost a month.

The open question is one ruling on two sentences in one file:

> **Spec §0.7 (`:268`) says governance is enforced on external input at the moment it enters, before any computation derives from it. Spec §3A.4 (`:1969`) says the post-mutation sweep is one of two enforcement points and governs the result. These contradict. Which one is right?**

Everything else is downstream and reconciles automatically once it is answered. Whichever way it goes, **the other sentence gets deleted** — which is the mechanism that stops the recurrence, because it removes the half of canon that lets the next draft be "faithful to the corpus" while contradicting the ruling.

### What each side costs

**If §0.7 is right (two arms — the sweep is atomicity only, not an enforcement point):**
- `rule Floor <= Ceiling` and every multi-field relational rule must be **proven at compile time across every reachable event ordering**, or the definition is rejected.
- The ruling's rejection of pure Model B loses its decisive leg (`:101-102`) and needs re-argument on other grounds.
- Rows 10/11/16 of the ruling are wrong and must be re-derived. v1's four-leg predicate becomes the correct device.
- The real expressiveness cost is unknown — see §5.1. It must be measured before this is ruled.
- `§3A.4:1969`'s second paragraph is deleted; §3A.4 returns to being about atomicity, which is what it was for from 2026-03-02 to 2026-06-02.

**If §3A.4 is right (three arms — the sweep is a genuine second enforcement point):**
- §0.7 must be corrected to name the sweep, and the "two distinct mechanisms" framing at `:264` reworded — it currently means compile-vs-runtime, and would now have to carry ingress-vs-sweep as well.
- The ruling's one-line boundary (`:134`) is wrong as written and **cannot be quoted as the boundary** — "a raw external value at its own ingress slot is governed" does not describe what rows 10/11 do.
- The Obligation-Role Rule's Part A.1 must be rewritten: its trigger/subject split (§4.1) has to be stated honestly, and its literal reading has a soundness hole.
- The project must accept that a declared relational invariant is enforced at runtime, which is a governance surface the philosophy's `:55` composition seam does not cover.

### What does not bear on it

**The sweep is not built.** `src/Precept/Runtime/Evaluator.cs` — all five operations `throw new NotImplementedException()` (`:79`, `:98`, `:120`, `:131`, `:154`); `:46` states why: *"TODO: implement Fire/Update once the executable model is designed."* `ConstraintsFailed` is declared in three files (`EventOutcome.cs:28`, `UpdateOutcome.cs:15`, `RestoreOutcome.cs:18`) and **constructed nowhere in the repo** — zero call sites, zero tests. No working copy exists in code. There is no implementation to unwind on either side, so build-cost is not an input to this ruling.

---

## 7. Confidence and falsifiers

**High (~90%)** — the §0.7 / §3A.4 contradiction. Both texts quoted verbatim from the working tree and from `f1f3a319`, byte-identical, with commit hashes and timestamps.

**High (~90%)** — Decision #1's fence excludes free-standing relational invariants. Rests on quoted qualifier text (`frank:21`), the ledger's own glossary (`ledger:11`), and the ruling's independent statement of the same exclusion (`ruling:159`). Three independent artifacts agree.

**High** — the ledger's zero-count on sweep/ingress/proof-carrying. Mechanical count over the committed file.

**Medium-high** — the "third arm" framing itself. "Arm" is not the corpus's term. Someone could reasonably hold that ingress and the sweep are two *implementation points* of one *mechanism* (governance) rather than two arms — which may be how the ruling's author was thinking. **This does not change the substance:** the sweep enforces declared constraints, at commit, over whole-entity state including derived and independently-set values, and `:134` says the only governed thing is a raw external value at its own ingress slot. Whether that is a third arm or an under-described second one, `:134` does not describe what rows 10/11 do.

**Medium (~70%)** — the characterization "never confronted" rather than "answered elsewhere and silently carried forward." The counter-reading: `spec:1969` + `ruling:159` already answer it and the ledger had no reason to re-rule. Weighted lower because `ledger:24` ratifies a one-liner that *denies* the sweep — so the ledger did not carry the answer forward, it carried a contradiction forward.

**Would falsify the core finding:**
- A pre-07-12 **owner** statement (not agent-authored) dispositioning free-standing relational invariants — in a commit message, `decision-index.md`, or `bugs.md`. These were not swept exhaustively; this is the largest coverage gap.
- Evidence that "governed at ingress" is a term of art in this corpus meaning "governed at the operation boundary, sweep included." Against this: `frank:131` argues a post-computation re-check is *new machinery requiring a new `ConstraintKind`* — which only makes sense if he means ingress literally.
- A passage in the ruling acknowledging the sweep as a second, non-ingress runtime enforcement point and arguing it is still within `:134`'s two arms. None found across all 359 lines.
- `spec:1969` having been superseded pre-07-12. Checked: `git log -S` returns exactly one commit, `63e08963`, and the text is live at `f1f3a319`.

---

## 8. Unrelated but flagged

An agent reported that a file-read tool result returned an injected `## Exited Plan Mode` block originating from neither the operator nor the harness. It disregarded it and made no edits. Source unlocated. If that text is sitting in a repo document, it is a prompt-injection vector in our own corpus and should be found.

Separately: `docs/hybrid-model.md` and `docs/Working/frank-canonical-capture-recommendation-2026-07-14.md` were present at session start (per the git-status snapshot) and are now absent. Both were untracked and never committed on any branch — no stash, no dangling blob, no backup. `hybrid-model.md`'s content survives as the A/A2 snapshots in `posture-v2-support/`. The capture recommendation does not; the posture replay v2 cites it in five now-dangling places (`:69`, `:89`, `:101`, `:125`, `:219`). Cause unknown; the deletion looks targeted rather than a sweep, since 19 sibling files are untouched.
